using System.Globalization;
using System.Data.Common;
using Microsoft.Data.SqlClient;
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
        DbConnection connection,
        DbTransaction? transaction,
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
            INSERT INTO [dbo].[KULLANICI_FAVORI_FIYAT_ALARM_ISLERI]
            ([OTEL_ID], [TARIH_BASLANGIC], [TARIH_BITIS], [TETIKLEYEN_KULLANICI_ID], [DURUM], [SON_ISLENEN_ALARM_ID], [ISLENEN_KAYIT_SAYISI], [DENEME_SAYISI], [PLANLI_CALISMA_TARIHI], [OLUSTURULMA_TARIHI], [GUNCELLENME_TARIHI])
            VALUES
            (@hotelId, @startDate, @endDate, @triggeredByUserId, 'Pending', 0, 0, 0, SYSUTCDATETIME(), SYSUTCDATETIME(), SYSUTCDATETIME());";
        await using var command = new SqlCommand(insertSql, (SqlConnection)connection, (SqlTransaction?)transaction);
        command.Parameters.AddWithValue("@hotelId", hotelId);
        command.Parameters.AddWithValue("@startDate", startDate.Date);
        command.Parameters.AddWithValue("@endDate", endDate.Date);
        command.Parameters.AddWithValue("@triggeredByUserId", triggeredByUserId > 0 ? triggeredByUserId : 0);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task ProcessPendingJobsAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
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

    private async Task ProcessSingleJobAsync(SqlConnection connection, PriceAlertJobRow job, CancellationToken cancellationToken)
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
                RelatedTable = "KULLANICI_FAVORI_FIYAT_ALARMLARI",
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
                UPDATE [dbo].[KULLANICI_FAVORI_FIYAT_ALARMLARI]
                SET [SON_TETIKLENEN_TARIH] = SYSUTCDATETIME(),
                    [SON_TETIKLENEN_FIYAT] = @price,
                    [GUNCELLENME_TARIHI] = CURRENT_TIMESTAMP
                WHERE id = @alertId;";
            await using var touchAlertCommand = new SqlCommand(touchAlertSql, connection);
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
            UPDATE [dbo].[KULLANICI_FAVORI_FIYAT_ALARM_ISLERI]
            SET [DURUM] = 'Pending',
                [SON_ISLENEN_ALARM_ID] = @cursor,
                [ISLENEN_KAYIT_SAYISI] = [ISLENEN_KAYIT_SAYISI] + @processedCount,
                [HATA_MESAJI] = NULL,
                [PLANLI_CALISMA_TARIHI] = DATEADD(SECOND, 2, SYSUTCDATETIME()),
                [GUNCELLENME_TARIHI] = CURRENT_TIMESTAMP
            WHERE id = @jobId;";
        await using var requeueCommand = new SqlCommand(requeueSql, connection);
        requeueCommand.Parameters.AddWithValue("@jobId", job.JobId);
        requeueCommand.Parameters.AddWithValue("@cursor", lastCursor);
        requeueCommand.Parameters.AddWithValue("@processedCount", processedCount);
        await requeueCommand.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<List<PriceAlertJobRow>> LoadCandidateJobsAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT TOP (8) id, [OTEL_ID], [TARIH_BASLANGIC], [TARIH_BITIS], [SON_ISLENEN_ALARM_ID], [DENEME_SAYISI]
            FROM [dbo].[KULLANICI_FAVORI_FIYAT_ALARM_ISLERI]
            WHERE [PLANLI_CALISMA_TARIHI] <= SYSUTCDATETIME()
              AND (
                    [DURUM] = 'Pending'
                    OR ([DURUM] = 'Processing' AND [GUNCELLENME_TARIHI] <= DATEADD(MINUTE, -5, SYSUTCDATETIME()))
                  )
            ORDER BY id ASC;";

        var items = new List<PriceAlertJobRow>();
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new PriceAlertJobRow(
                reader.GetInt64(0),
                reader.GetInt64(1),
                reader.GetDateTime(2),
                reader.GetDateTime(3),
                reader.IsDBNull(4) ? 0L : reader.GetInt64(4),
                    reader.IsDBNull(5) ? 0 : Convert.ToInt32(reader.GetValue(5), CultureInfo.InvariantCulture)));
        }

        return items;
    }

    private static async Task<bool> TryClaimJobAsync(SqlConnection connection, long jobId, CancellationToken cancellationToken)
    {
        const string sql = @"
            UPDATE [dbo].[KULLANICI_FAVORI_FIYAT_ALARM_ISLERI]
            SET [DURUM] = 'Processing',
                [DENEME_SAYISI] = [DENEME_SAYISI] + 1,
                [GUNCELLENME_TARIHI] = CURRENT_TIMESTAMP
            WHERE id = @jobId
              AND (
                    [DURUM] = 'Pending'
                    OR ([DURUM] = 'Processing' AND [GUNCELLENME_TARIHI] <= DATEADD(MINUTE, -5, SYSUTCDATETIME()))
                  );";
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@jobId", jobId);
        var affected = await command.ExecuteNonQueryAsync(cancellationToken);
        return affected > 0;
    }

    private static async Task<List<PriceAlertMatchRow>> LoadMatchingAlertsAsync(SqlConnection connection, PriceAlertJobRow job, int batchSize, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT TOP (@batchSize)
                a.id AS alert_id,
                a.[KULLANICI_ID],
                COALESCE(NULLIF(u.[EPOSTA], ''), '') AS user_email,
                COALESCE(NULLIF(u.[AD_SOYAD], ''), 'Degerli misafirimiz') AS user_name,
                COALESCE(NULLIF(o.[OTEL_ADI], ''), 'Otel') AS hotel_name,
                COALESCE(NULLIF(o.[OTEL_KODU], ''), 'otel') AS hotel_code,
                a.[HEDEF_MAKSIMUM_FIYAT],
                MIN(COALESCE(NULLIF(ofm.[INDIRIMLI_FIYAT], 0), NULLIF(ofm.[GECELIK_FIYAT], 0))) AS matched_price,
                MIN(ofm.[TARIH]) AS matched_date
            FROM [dbo].[KULLANICI_FAVORI_FIYAT_ALARMLARI] a
            INNER JOIN [dbo].[KULLANICI_FAVORI_OTELLER] f ON f.[KULLANICI_ID] = a.[KULLANICI_ID] AND f.[OTEL_ID] = a.[OTEL_ID] AND COALESCE(f.[AKTIF_MI], 1) = 1
            INNER JOIN [dbo].[KULLANICILAR] u ON u.id = a.[KULLANICI_ID]
            INNER JOIN [dbo].[OTELLER] o ON o.id = a.[OTEL_ID]
            INNER JOIN [dbo].[ODA_TIPLERI] ot ON ot.[OTEL_ID] = a.[OTEL_ID] AND ot.[AKTIF_MI] = 1
            INNER JOIN [dbo].[ODA_FIYAT_MUSAITLIK] ofm ON ofm.[ODA_TIP_ID] = ot.id AND ofm.[OTEL_ID] = a.[OTEL_ID]
            WHERE a.[OTEL_ID] = @hotelId
              AND COALESCE(a.[AKTIF_MI], 1) = 1
              AND a.id > @cursor
              AND ofm.[TARIH] BETWEEN
                    (CASE WHEN CAST(a.[BASLANGIC_TARIHI] AS date) > CAST(@jobStart AS date) THEN CAST(a.[BASLANGIC_TARIHI] AS date) ELSE CAST(@jobStart AS date) END)
                    AND
                    (CASE WHEN CAST(a.[BITIS_TARIHI] AS date) < CAST(@jobEnd AS date) THEN CAST(a.[BITIS_TARIHI] AS date) ELSE CAST(@jobEnd AS date) END)
              AND COALESCE(ofm.[KAPALI_SATIS], 0) = 0
              AND (COALESCE(ofm.[TOPLAM_ODA_SAYISI], ot.[TOPLAM_ODA_SAYISI]) - COALESCE(ofm.[SATILAN_ODA_SAYISI], 0) - COALESCE(ofm.[BLOKE_ODA_SAYISI], 0)) > 0
              AND COALESCE(NULLIF(ofm.[INDIRIMLI_FIYAT], 0), NULLIF(ofm.[GECELIK_FIYAT], 0), 999999999.99) <= a.[HEDEF_MAKSIMUM_FIYAT]
              AND (a.[SON_TETIKLENEN_TARIH] IS NULL OR a.[SON_TETIKLENEN_TARIH] <= DATEADD(HOUR, -6, SYSUTCDATETIME()))
            GROUP BY a.id, a.[KULLANICI_ID], u.[EPOSTA], u.[AD_SOYAD], o.[OTEL_ADI], o.[OTEL_KODU], a.[HEDEF_MAKSIMUM_FIYAT]
            ORDER BY a.id ASC;";

        var matches = new List<PriceAlertMatchRow>();
        await using var command = new SqlCommand(sql, connection);
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

    private static async Task MarkCompletedAsync(SqlConnection connection, long jobId, long cursor, int processedCount, CancellationToken cancellationToken)
    {
        const string sql = @"
            UPDATE [dbo].[KULLANICI_FAVORI_FIYAT_ALARM_ISLERI]
            SET [DURUM] = 'Completed',
                [SON_ISLENEN_ALARM_ID] = @cursor,
                [ISLENEN_KAYIT_SAYISI] = [ISLENEN_KAYIT_SAYISI] + @processedCount,
                [HATA_MESAJI] = NULL,
                [GUNCELLENME_TARIHI] = CURRENT_TIMESTAMP
            WHERE id = @jobId;";
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@jobId", jobId);
        command.Parameters.AddWithValue("@cursor", cursor);
        command.Parameters.AddWithValue("@processedCount", processedCount);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task MarkFailedAsync(SqlConnection connection, long jobId, string errorMessage, CancellationToken cancellationToken)
    {
        const string sql = @"
            UPDATE [dbo].[KULLANICI_FAVORI_FIYAT_ALARM_ISLERI]
            SET [DURUM] = 'Pending',
                [PLANLI_CALISMA_TARIHI] = DATEADD(
                    SECOND,
                    CASE
                        WHEN POWER(CAST(2 AS bigint), [DENEME_SAYISI]) > 300 THEN 300
                        ELSE CAST(POWER(CAST(2 AS bigint), [DENEME_SAYISI]) AS int)
                    END,
                    SYSUTCDATETIME()),
                [HATA_MESAJI] = @error,
                [GUNCELLENME_TARIHI] = CURRENT_TIMESTAMP
            WHERE id = @jobId;";
        await using var command = new SqlCommand(sql, connection);
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

        return $"/hotel/{slug}";
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
