using System.Globalization;
using MySqlConnector;
using otelturizmnew.Models.Email;
using otelturizmnew.Services.Abstractions;

namespace otelturizmnew.Services;

public sealed class FavoritePriceAlertService : IFavoritePriceAlertService
{
    private readonly string _connectionString;
    private readonly IEmailQueueService _emailQueueService;
    private readonly ILogger<FavoritePriceAlertService> _logger;

    public FavoritePriceAlertService(IConfiguration configuration, IEmailQueueService emailQueueService, ILogger<FavoritePriceAlertService> logger)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection tanimli degil.");
        _emailQueueService = emailQueueService;
        _logger = logger;
    }

    public async Task QueuePriceRecheckJobAsync(
        MySqlConnection connection,
        MySqlTransaction? transaction,
        long hotelId,
        DateTime startDate,
        DateTime endDate,
        long triggeredByUserId,
        CancellationToken cancellationToken = default)
    {
        if (hotelId <= 0 || endDate.Date < startDate.Date)
        {
            return;
        }

        const string insertSql = @"
            INSERT INTO user_favorite_price_alert_jobs
            (otel_id, tarih_baslangic, tarih_bitis, tetikleyen_kullanici_id, durum, son_islenen_alert_id, islenen_kayit_sayisi, deneme_sayisi, planli_calisma_tarihi, olusturulma_tarihi, guncellenme_tarihi)
            VALUES
            (@hotelId, @startDate, @endDate, @triggeredByUserId, 'Pending', 0, 0, 0, UTC_TIMESTAMP(), UTC_TIMESTAMP(), UTC_TIMESTAMP());";
        await using var command = new MySqlCommand(insertSql, connection, transaction);
        command.Parameters.AddWithValue("@hotelId", hotelId);
        command.Parameters.AddWithValue("@startDate", startDate.Date);
        command.Parameters.AddWithValue("@endDate", endDate.Date);
        command.Parameters.AddWithValue("@triggeredByUserId", triggeredByUserId > 0 ? triggeredByUserId : 0);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task ProcessPendingJobsAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var jobs = await LoadCandidateJobsAsync(connection, cancellationToken);
        foreach (var job in jobs)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var claimed = await TryClaimJobAsync(connection, job.JobId, cancellationToken);
            if (!claimed)
            {
                continue;
            }

            try
            {
                await ProcessSingleJobAsync(connection, job, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fiyat alarmi islenirken hata olustu. JobId: {JobId}, HotelId: {HotelId}", job.JobId, job.HotelId);
                await MarkFailedAsync(connection, job.JobId, ex.Message, cancellationToken);
            }
        }
    }

    private async Task ProcessSingleJobAsync(MySqlConnection connection, PriceAlertJobRow job, CancellationToken cancellationToken)
    {
        const int batchSize = 250;
        var matches = await LoadMatchingAlertsAsync(connection, job, batchSize, cancellationToken);
        if (matches.Count == 0)
        {
            await MarkCompletedAsync(connection, job.JobId, job.LastAlertCursor, 0, cancellationToken);
            return;
        }

        var processedCount = 0;
        long lastCursor = job.LastAlertCursor;
        foreach (var match in matches)
        {
            if (string.IsNullOrWhiteSpace(match.Email))
            {
                lastCursor = match.AlertId;
                continue;
            }

            await _emailQueueService.QueueTemplateAsync(connection, null, new QueuedEmailTemplateRequest
            {
                UserId = match.UserId,
                RecipientEmail = match.Email,
                TemplateCode = "favorite_price_alert_match",
                RelatedTable = "user_favorite_price_alerts",
                RelatedRecordId = match.AlertId,
                Tokens = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["user_first_name"] = SplitFirstName(match.FullName),
                    ["hotel_name"] = match.HotelName,
                    ["target_price"] = match.TargetPrice.ToString("N2", CultureInfo.GetCultureInfo("tr-TR")),
                    ["matched_price"] = match.MatchedPrice.ToString("N2", CultureInfo.GetCultureInfo("tr-TR")),
                    ["matched_date"] = match.MatchedDate.ToString("dd MMM yyyy", CultureInfo.GetCultureInfo("tr-TR")),
                    ["favorites_link"] = "/panel/user/favorilerim",
                    ["hotel_link"] = match.HotelSlug
                }
            }, cancellationToken);

            const string touchAlertSql = @"
                UPDATE user_favorite_price_alerts
                SET son_tetiklenen_tarih = UTC_TIMESTAMP(),
                    son_tetiklenen_fiyat = @price,
                    guncellenme_tarihi = CURRENT_TIMESTAMP
                WHERE id = @alertId;";
            await using var touchAlertCommand = new MySqlCommand(touchAlertSql, connection);
            touchAlertCommand.Parameters.AddWithValue("@alertId", match.AlertId);
            touchAlertCommand.Parameters.AddWithValue("@price", match.MatchedPrice);
            await touchAlertCommand.ExecuteNonQueryAsync(cancellationToken);

            processedCount++;
            lastCursor = match.AlertId;
        }

        if (matches.Count < batchSize)
        {
            await MarkCompletedAsync(connection, job.JobId, lastCursor, processedCount, cancellationToken);
            return;
        }

        const string requeueSql = @"
            UPDATE user_favorite_price_alert_jobs
            SET durum = 'Pending',
                son_islenen_alert_id = @cursor,
                islenen_kayit_sayisi = islenen_kayit_sayisi + @processedCount,
                hata_mesaji = NULL,
                planli_calisma_tarihi = DATE_ADD(UTC_TIMESTAMP(), INTERVAL 2 SECOND),
                guncellenme_tarihi = CURRENT_TIMESTAMP
            WHERE id = @jobId;";
        await using var requeueCommand = new MySqlCommand(requeueSql, connection);
        requeueCommand.Parameters.AddWithValue("@jobId", job.JobId);
        requeueCommand.Parameters.AddWithValue("@cursor", lastCursor);
        requeueCommand.Parameters.AddWithValue("@processedCount", processedCount);
        await requeueCommand.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<List<PriceAlertJobRow>> LoadCandidateJobsAsync(MySqlConnection connection, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT id, otel_id, tarih_baslangic, tarih_bitis, son_islenen_alert_id, deneme_sayisi
            FROM user_favorite_price_alert_jobs
            WHERE planli_calisma_tarihi <= UTC_TIMESTAMP()
              AND (
                    durum = 'Pending'
                    OR (durum = 'Processing' AND guncellenme_tarihi <= DATE_SUB(UTC_TIMESTAMP(), INTERVAL 5 MINUTE))
                  )
            ORDER BY id ASC
            LIMIT 8;";

        var items = new List<PriceAlertJobRow>();
        await using var command = new MySqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new PriceAlertJobRow(
                reader.GetInt64(0),
                reader.GetInt64(1),
                reader.GetDateTime(2),
                reader.GetDateTime(3),
                reader.IsDBNull(4) ? 0L : reader.GetInt64(4),
                reader.IsDBNull(5) ? 0 : reader.GetInt32(5)));
        }

        return items;
    }

    private static async Task<bool> TryClaimJobAsync(MySqlConnection connection, long jobId, CancellationToken cancellationToken)
    {
        const string sql = @"
            UPDATE user_favorite_price_alert_jobs
            SET durum = 'Processing',
                deneme_sayisi = deneme_sayisi + 1,
                guncellenme_tarihi = CURRENT_TIMESTAMP
            WHERE id = @jobId
              AND (
                    durum = 'Pending'
                    OR (durum = 'Processing' AND guncellenme_tarihi <= DATE_SUB(UTC_TIMESTAMP(), INTERVAL 5 MINUTE))
                  );";
        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@jobId", jobId);
        var affected = await command.ExecuteNonQueryAsync(cancellationToken);
        return affected > 0;
    }

    private static async Task<List<PriceAlertMatchRow>> LoadMatchingAlertsAsync(MySqlConnection connection, PriceAlertJobRow job, int batchSize, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT
                a.id AS alert_id,
                a.user_id,
                COALESCE(NULLIF(u.eposta, ''), '') AS user_email,
                COALESCE(NULLIF(u.ad_soyad, ''), 'Degerli misafirimiz') AS user_name,
                COALESCE(NULLIF(o.otel_adi, ''), 'Otel') AS hotel_name,
                COALESCE(NULLIF(o.otel_kodu, ''), 'otel') AS hotel_code,
                a.hedef_maksimum_fiyat,
                MIN(COALESCE(NULLIF(ofm.indirimli_fiyat, 0), NULLIF(ofm.gecelik_fiyat, 0))) AS matched_price,
                MIN(ofm.tarih) AS matched_date
            FROM user_favorite_price_alerts a
            INNER JOIN user_favori_oteller f ON f.user_id = a.user_id AND f.otel_id = a.otel_id AND COALESCE(f.aktif_mi, 1) = 1
            INNER JOIN users u ON u.id = a.user_id
            INNER JOIN oteller o ON o.id = a.otel_id
            INNER JOIN oda_tipleri ot ON ot.otel_id = a.otel_id AND ot.aktif_mi = 1
            INNER JOIN oda_fiyat_musaitlik ofm ON ofm.oda_tip_id = ot.id
            WHERE a.otel_id = @hotelId
              AND COALESCE(a.aktif_mi, 1) = 1
              AND a.id > @cursor
              AND ofm.tarih BETWEEN GREATEST(DATE(a.baslangic_tarihi), DATE(@jobStart)) AND LEAST(DATE(a.bitis_tarihi), DATE(@jobEnd))
              AND COALESCE(ofm.kapali_satis, 0) = 0
              AND (COALESCE(ofm.toplam_oda_sayisi, ot.toplam_oda_sayisi) - COALESCE(ofm.satilan_oda_sayisi, 0) - COALESCE(ofm.bloke_oda_sayisi, 0)) > 0
              AND COALESCE(NULLIF(ofm.indirimli_fiyat, 0), NULLIF(ofm.gecelik_fiyat, 0), 999999999.99) <= a.hedef_maksimum_fiyat
              AND (a.son_tetiklenen_tarih IS NULL OR a.son_tetiklenen_tarih <= DATE_SUB(UTC_TIMESTAMP(), INTERVAL 6 HOUR))
            GROUP BY a.id, a.user_id, u.eposta, u.ad_soyad, o.otel_adi, o.otel_kodu, a.hedef_maksimum_fiyat
            ORDER BY a.id ASC
            LIMIT @batchSize;";

        var matches = new List<PriceAlertMatchRow>();
        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@hotelId", job.HotelId);
        command.Parameters.AddWithValue("@cursor", job.LastAlertCursor);
        command.Parameters.AddWithValue("@jobStart", job.StartDate.Date);
        command.Parameters.AddWithValue("@jobEnd", job.EndDate.Date);
        command.Parameters.AddWithValue("@batchSize", batchSize);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var hotelName = reader.GetString(4);
            var hotelCode = reader.GetString(5);
            matches.Add(new PriceAlertMatchRow(
                reader.GetInt64(0),
                reader.GetInt64(1),
                reader.GetString(2),
                reader.GetString(3),
                hotelName,
                BuildHotelLink(hotelName, hotelCode),
                reader.GetDecimal(6),
                reader.GetDecimal(7),
                reader.GetDateTime(8)));
        }

        return matches;
    }

    private static async Task MarkCompletedAsync(MySqlConnection connection, long jobId, long cursor, int processedCount, CancellationToken cancellationToken)
    {
        const string sql = @"
            UPDATE user_favorite_price_alert_jobs
            SET durum = 'Completed',
                son_islenen_alert_id = @cursor,
                islenen_kayit_sayisi = islenen_kayit_sayisi + @processedCount,
                hata_mesaji = NULL,
                guncellenme_tarihi = CURRENT_TIMESTAMP
            WHERE id = @jobId;";
        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@jobId", jobId);
        command.Parameters.AddWithValue("@cursor", cursor);
        command.Parameters.AddWithValue("@processedCount", processedCount);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task MarkFailedAsync(MySqlConnection connection, long jobId, string errorMessage, CancellationToken cancellationToken)
    {
        const string sql = @"
            UPDATE user_favorite_price_alert_jobs
            SET durum = 'Pending',
                planli_calisma_tarihi = DATE_ADD(UTC_TIMESTAMP(), INTERVAL LEAST(300, POW(2, deneme_sayisi)) SECOND),
                hata_mesaji = @error,
                guncellenme_tarihi = CURRENT_TIMESTAMP
            WHERE id = @jobId;";
        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@jobId", jobId);
        command.Parameters.AddWithValue("@error", errorMessage.Length > 500 ? errorMessage[..500] : errorMessage);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static string SplitFirstName(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            return "Misafir";
        }

        var tokens = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return tokens.Length == 0 ? "Misafir" : tokens[0];
    }

    private static string BuildHotelLink(string hotelName, string hotelCode)
    {
        var source = string.IsNullOrWhiteSpace(hotelName) ? hotelCode : hotelName;
        var normalized = source.ToLowerInvariant()
            .Replace("ı", "i")
            .Replace("ğ", "g")
            .Replace("ü", "u")
            .Replace("ş", "s")
            .Replace("ö", "o")
            .Replace("ç", "c");

        var chars = new List<char>(normalized.Length);
        var lastDash = false;
        foreach (var ch in normalized)
        {
            if (char.IsLetterOrDigit(ch))
            {
                chars.Add(ch);
                lastDash = false;
            }
            else if (!lastDash)
            {
                chars.Add('-');
                lastDash = true;
            }
        }

        var slug = new string(chars.ToArray()).Trim('-');
        if (string.IsNullOrWhiteSpace(slug))
        {
            slug = hotelCode.ToLowerInvariant();
        }

        return $"/oteller/{slug}";
    }

    private sealed record PriceAlertJobRow(
        long JobId,
        long HotelId,
        DateTime StartDate,
        DateTime EndDate,
        long LastAlertCursor,
        int AttemptCount);

    private sealed record PriceAlertMatchRow(
        long AlertId,
        long UserId,
        string Email,
        string FullName,
        string HotelName,
        string HotelSlug,
        decimal TargetPrice,
        decimal MatchedPrice,
        DateTime MatchedDate);
}
