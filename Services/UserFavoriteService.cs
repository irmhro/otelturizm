using System.Globalization;
using Microsoft.Data.SqlClient;
using SqlConnection = Microsoft.Data.SqlClient.SqlConnection;
using SqlCommand = Microsoft.Data.SqlClient.SqlCommand;
using SqlTransaction = Microsoft.Data.SqlClient.SqlTransaction;
using SqlException = Microsoft.Data.SqlClient.SqlException;
using otelturizmnew.Models.Oteller;
using otelturizmnew.Models.Paneller.User;
using otelturizmnew.Services.Abstractions;

namespace otelturizmnew.Services;

public class UserFavoriteService : IUserFavoriteService
{
    private readonly IConfiguration _configuration;
    private readonly IHotelPricingReadService _hotelPricingReadService;
    private static readonly CultureInfo TrCulture = CultureInfo.GetCultureInfo("tr-TR");

    public UserFavoriteService(IConfiguration configuration, IHotelPricingReadService hotelPricingReadService)
    {
        _configuration = configuration;
        _hotelPricingReadService = hotelPricingReadService;
    }

    public async Task<HashSet<long>> GetFavoriteHotelIdsAsync(long userId, IEnumerable<long> hotelIds, CancellationToken cancellationToken = default)
    {
        var ids = hotelIds.Where(id => id > 0).Distinct().ToArray();
        if (userId <= 0 || ids.Length == 0)
        {
            return new HashSet<long>();
        }

        var connectionString = _configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return new HashSet<long>();
        }

        var parameters = string.Join(", ", ids.Select((_, index) => $"@hotelId{index}"));
        var sql = $"SELECT otel_id FROM user_favori_oteller WHERE user_id = @userId AND otel_id IN ({parameters}) AND COALESCE(aktif_mi, 1) = 1;";

        var result = new HashSet<long>();

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@userId", userId);
        for (var i = 0; i < ids.Length; i++)
        {
            command.Parameters.AddWithValue($"@hotelId{i}", ids[i]);
        }

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(reader.GetInt64(0));
        }

        return result;
    }

    public async Task<int> GetFavoriteCountAsync(long userId, CancellationToken cancellationToken = default)
    {
        if (userId <= 0)
        {
            return 0;
        }

        var connectionString = _configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return 0;
        }

        const string sql = @"SELECT COUNT(*) FROM user_favori_oteller WHERE user_id = @userId AND COALESCE(aktif_mi, 1) = 1;";

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@userId", userId);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result, CultureInfo.InvariantCulture);
    }

    public async Task<UserFavoritesPageViewModel> GetFavoritesPageAsync(long userId, CancellationToken cancellationToken = default)
    {
        var model = new UserFavoritesPageViewModel();
        if (userId <= 0)
        {
            return model;
        }

        var connectionString = _configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return model;
        }

        const string sql = @"
            SELECT
                f.otel_id,
                f.olusturulma_tarihi,
                o.otel_kodu,
                o.otel_adi,
                o.sehir,
                o.ilce,
                COALESCE(o.ortalama_puan, 0) AS ortalama_puan,
                COALESCE(o.toplam_yorum_sayisi, 0) AS toplam_yorum_sayisi,
                COALESCE(NULLIF(o.kapak_fotografi, ''), NULLIF(og.gorsel_url, '')) AS gorsel_url,
                pf.baslangic_fiyat,
                a.hedef_maksimum_fiyat,
                a.baslangic_tarihi AS alert_baslangic_tarihi,
                a.bitis_tarihi AS alert_bitis_tarihi,
                a.son_tetiklenen_tarih AS alert_son_tetiklenen_tarih,
                COALESCE(a.aktif_mi, 0) AS alert_aktif_mi,
                (
                    SELECT COUNT(*)
                    FROM rezervasyonlar r
                    WHERE r.kullanici_id = @userId
                      AND r.otel_id = f.otel_id
                      AND r.durum <> 'İptal Edildi'
                      AND CAST(r.cikis_tarihi AS date) < CAST(SYSUTCDATETIME() AS date)
                ) AS past_stay_count
            FROM user_favori_oteller f
            JOIN oteller o ON o.id = f.otel_id
            LEFT JOIN (
                SELECT g1.otel_id, g1.gorsel_url
                FROM (
                    SELECT
                        g.otel_id,
                        g.gorsel_url,
                        ROW_NUMBER() OVER (PARTITION BY g.otel_id ORDER BY g.kapak_fotografi_mi DESC, g.one_cikan DESC, g.siralama ASC, g.id ASC) AS rn
                    FROM otel_gorselleri g
                    WHERE g.onay_durumu = 'Onaylandı'
                ) g1
                WHERE g1.rn = 1
            ) og ON og.otel_id = o.id
            LEFT JOIN (
                SELECT ot.otel_id,
                       MIN(
                           COALESCE(
                               CASE
                                   WHEN ofm.kapali_satis = 1 THEN NULL
                                   WHEN (COALESCE(ofm.toplam_oda_sayisi, ot.toplam_oda_sayisi) - COALESCE(ofm.satilan_oda_sayisi, 0) - COALESCE(ofm.bloke_oda_sayisi, 0)) <= 0 THEN NULL
                                   ELSE COALESCE(NULLIF(ofm.indirimli_fiyat, 0), NULLIF(ofm.gecelik_fiyat, 0))
                               END,
                               NULLIF(ot.standart_gecelik_fiyat, 0)
                           )
                       ) AS baslangic_fiyat
                FROM oda_tipleri ot
                LEFT JOIN oda_fiyat_musaitlik ofm ON ofm.oda_tip_id = ot.id
                    AND ofm.otel_id = ot.otel_id
                    AND ofm.tarih BETWEEN CAST(SYSUTCDATETIME() AS date) AND DATEADD(DAY, 120, CAST(SYSUTCDATETIME() AS date))
                WHERE ot.aktif_mi = 1
                GROUP BY ot.otel_id
            ) pf ON pf.otel_id = o.id
            LEFT JOIN user_favorite_price_alerts a
                ON a.user_id = f.user_id
               AND a.otel_id = f.otel_id
               AND COALESCE(a.aktif_mi, 1) = 1
            WHERE f.user_id = @userId
              AND COALESCE(f.aktif_mi, 1) = 1
              AND o.yayin_durumu = 'Yayında'
              AND o.onay_durumu = 'Onaylandı'
            ORDER BY f.olusturulma_tarihi DESC, f.id DESC;";

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@userId", userId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var culture = new CultureInfo("tr-TR");

        while (await reader.ReadAsync(cancellationToken))
        {
            var rating = reader.GetDecimal(reader.GetOrdinal("ortalama_puan"));
            var reviewCount = reader.GetInt32(reader.GetOrdinal("toplam_yorum_sayisi"));
            var priceOrdinal = reader.GetOrdinal("baslangic_fiyat");
            var price = reader.IsDBNull(priceOrdinal) ? (decimal?)null : reader.GetDecimal(priceOrdinal);
            var createdAt = reader.GetDateTime(reader.GetOrdinal("olusturulma_tarihi"));
            var hotelCode = reader.GetString(reader.GetOrdinal("otel_kodu"));
            var hotelName = reader.GetString(reader.GetOrdinal("otel_adi"));
            var alertActive = !reader.IsDBNull(reader.GetOrdinal("alert_aktif_mi")) && reader.GetBoolean(reader.GetOrdinal("alert_aktif_mi"));
            decimal? alertTarget = null;
            if (!reader.IsDBNull(reader.GetOrdinal("hedef_maksimum_fiyat")))
            {
                alertTarget = reader.GetDecimal(reader.GetOrdinal("hedef_maksimum_fiyat"));
            }
            DateTime? alertStart = reader.IsDBNull(reader.GetOrdinal("alert_baslangic_tarihi")) ? null : reader.GetDateTime(reader.GetOrdinal("alert_baslangic_tarihi"));
            DateTime? alertEnd = reader.IsDBNull(reader.GetOrdinal("alert_bitis_tarihi")) ? null : reader.GetDateTime(reader.GetOrdinal("alert_bitis_tarihi"));
            DateTime? lastTriggered = reader.IsDBNull(reader.GetOrdinal("alert_son_tetiklenen_tarih")) ? null : reader.GetDateTime(reader.GetOrdinal("alert_son_tetiklenen_tarih"));

            model.Hotels.Add(new UserFavoriteHotelCardViewModel
            {
                HotelId = reader.GetInt64(reader.GetOrdinal("otel_id")),
                HotelCode = hotelCode,
                Name = hotelName,
                Slug = BuildSlug(hotelName, hotelCode),
                City = reader.GetString(reader.GetOrdinal("sehir")),
                District = reader.GetString(reader.GetOrdinal("ilce")),
                ImageUrl = NormalizeImageUrl(reader.IsDBNull(reader.GetOrdinal("gorsel_url")) ? string.Empty : reader.GetString(reader.GetOrdinal("gorsel_url"))),
                Rating = rating,
                ReviewCount = reviewCount,
                StartingPrice = price,
                PriceText = price.HasValue ? $"TRY {price.Value:N0}" : "Teklif Al",
                RatingText = rating > 0 ? (rating >= 9 ? "Olağanüstü" : rating >= 8 ? "Çok İyi" : "İyi") : "Yorum Bekleniyor",
                AddedDateText = $"{createdAt.ToString("dd MMMM yyyy", culture)} tarihinde kaydedildi",
                PastStayCount = reader.GetInt32(reader.GetOrdinal("past_stay_count")),
                PriceAlertEnabled = alertActive,
                PriceAlertTargetAmount = alertTarget,
                PriceAlertTargetText = alertTarget.HasValue ? $"TRY {alertTarget.Value:N0}" : null,
                PriceAlertDateRangeText = alertStart.HasValue && alertEnd.HasValue ? $"{alertStart.Value:dd.MM.yyyy} - {alertEnd.Value:dd.MM.yyyy}" : null,
                PriceAlertLastTriggeredText = lastTriggered.HasValue ? $"{lastTriggered.Value:dd.MM.yyyy HH:mm}" : null,
                PriceAlertStartDateValue = alertStart?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                PriceAlertEndDateValue = alertEnd?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
            });
        }

        if (model.Hotels.Count > 0)
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            var endDate = today.AddDays(120);
            var priceMap = await _hotelPricingReadService.GetHotelEffectivePriceMapAsync(
                model.Hotels.Select(static item => item.HotelId).ToList(),
                today,
                endDate,
                cancellationToken);

            foreach (var hotel in model.Hotels)
            {
                if (!priceMap.TryGetValue(hotel.HotelId, out var effectivePrice) || effectivePrice <= 0m)
                {
                    continue;
                }

                hotel.StartingPrice = effectivePrice;
                hotel.PriceText = $"TRY {effectivePrice:N0}";
            }
        }

        model.FavoriteCount = model.Hotels.Count;
        return model;
    }

    public async Task<HotelFavoriteToggleResponse> ToggleFavoriteAsync(long userId, long hotelId, string sourcePage, string sourceUrl, string? deviceType, string? ipAddress, CancellationToken cancellationToken = default)
    {
        if (userId <= 0 || hotelId <= 0)
        {
            return new HotelFavoriteToggleResponse { Success = false, Message = "Favori işlemi için geçerli kullanıcı ve otel bilgisi gereklidir." };
        }

        var connectionString = _configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return new HotelFavoriteToggleResponse { Success = false, Message = "Veritabanı bağlantısı bulunamadı." };
        }

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        if (!await HotelExistsAsync(connection, hotelId, cancellationToken))
        {
            return new HotelFavoriteToggleResponse { Success = false, Message = "Favorilere eklenecek otel bulunamadı." };
        }

        const string selectSql = @"SELECT TOP (1) id, COALESCE(aktif_mi, 1) AS aktif_mi FROM user_favori_oteller WHERE user_id = @userId AND otel_id = @hotelId ORDER BY id DESC;";
        await using var selectCommand = new SqlCommand(selectSql, connection);
        selectCommand.Parameters.AddWithValue("@userId", userId);
        selectCommand.Parameters.AddWithValue("@hotelId", hotelId);

        long? recordId = null;
        var isActive = false;
        await using (var reader = await selectCommand.ExecuteReaderAsync(cancellationToken))
        {
            if (await reader.ReadAsync(cancellationToken))
            {
                recordId = reader.GetInt64(reader.GetOrdinal("id"));
                isActive = reader.GetBoolean(reader.GetOrdinal("aktif_mi"));
            }
        }

        var normalizedSourcePage = SafeTrim(sourcePage, 100);
        var normalizedSourceUrl = SafeTrim(sourceUrl, 500);
        var normalizedDeviceType = SafeTrim(deviceType, 50);
        var normalizedIp = SafeTrim(ipAddress, 45);

        if (!recordId.HasValue)
        {
            const string insertSql = @"
                INSERT INTO user_favori_oteller
                (user_id, otel_id, kaynak_sayfa, kaynak_url, cihaz_tipi, ip_adresi, aktif_mi, olusturulma_tarihi, son_islem_tarihi)
                VALUES
                (@userId, @hotelId, @sourcePage, @sourceUrl, @deviceType, @ipAddress, 1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);";

            await using var insertCommand = new SqlCommand(insertSql, connection);
            insertCommand.Parameters.AddWithValue("@userId", userId);
            insertCommand.Parameters.AddWithValue("@hotelId", hotelId);
            insertCommand.Parameters.AddWithValue("@sourcePage", DbValue(normalizedSourcePage));
            insertCommand.Parameters.AddWithValue("@sourceUrl", DbValue(normalizedSourceUrl));
            insertCommand.Parameters.AddWithValue("@deviceType", DbValue(normalizedDeviceType));
            insertCommand.Parameters.AddWithValue("@ipAddress", DbValue(normalizedIp));
            await insertCommand.ExecuteNonQueryAsync(cancellationToken);

            return new HotelFavoriteToggleResponse { Success = true, IsFavorite = true, Message = "Otel favorilerinize eklendi." };
        }

        const string updateSql = @"
            UPDATE user_favori_oteller
            SET kaynak_sayfa = @sourcePage,
                kaynak_url = @sourceUrl,
                cihaz_tipi = @deviceType,
                ip_adresi = @ipAddress,
                aktif_mi = @isFavorite,
                kaldirilma_tarihi = CASE WHEN @isFavorite = 1 THEN NULL ELSE CURRENT_TIMESTAMP END,
                son_islem_tarihi = CURRENT_TIMESTAMP
            WHERE id = @id;";

        await using var updateCommand = new SqlCommand(updateSql, connection);
        updateCommand.Parameters.AddWithValue("@id", recordId.Value);
        updateCommand.Parameters.AddWithValue("@sourcePage", DbValue(normalizedSourcePage));
        updateCommand.Parameters.AddWithValue("@sourceUrl", DbValue(normalizedSourceUrl));
        updateCommand.Parameters.AddWithValue("@deviceType", DbValue(normalizedDeviceType));
        updateCommand.Parameters.AddWithValue("@ipAddress", DbValue(normalizedIp));
        updateCommand.Parameters.AddWithValue("@isFavorite", isActive ? 0 : 1);
        await updateCommand.ExecuteNonQueryAsync(cancellationToken);

        if (isActive)
        {
            const string disableAlertSql = @"
                UPDATE user_favorite_price_alerts
                SET aktif_mi = 0,
                    guncellenme_tarihi = CURRENT_TIMESTAMP
                WHERE user_id = @userId
                  AND otel_id = @hotelId;";
            await using var disableAlertCommand = new SqlCommand(disableAlertSql, connection);
            disableAlertCommand.Parameters.AddWithValue("@userId", userId);
            disableAlertCommand.Parameters.AddWithValue("@hotelId", hotelId);
            await disableAlertCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        return new HotelFavoriteToggleResponse
        {
            Success = true,
            IsFavorite = !isActive,
            Message = isActive ? "Otel favorilerinizden çıkarıldı." : "Otel favorilerinize yeniden eklendi."
        };
    }

    public async Task<(bool Success, string Message)> SavePriceAlertAsync(long userId, UserFavoritePriceAlertForm form, CancellationToken cancellationToken = default)
    {
        if (userId <= 0 || form.HotelId <= 0)
        {
            return (false, "Fiyat alarmi icin gecersiz kullanici veya otel secimi.");
        }

        var connectionString = _configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return (false, "Veritabani baglantisi bulunamadi.");
        }

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        const string favoriteCheckSql = @"
            SELECT COUNT(*)
            FROM user_favori_oteller
            WHERE user_id = @userId
              AND otel_id = @hotelId
              AND COALESCE(aktif_mi, 1) = 1;";
        await using (var favoriteCommand = new SqlCommand(favoriteCheckSql, connection))
        {
            favoriteCommand.Parameters.AddWithValue("@userId", userId);
            favoriteCommand.Parameters.AddWithValue("@hotelId", form.HotelId);
            var favoriteCount = Convert.ToInt32(await favoriteCommand.ExecuteScalarAsync(cancellationToken) ?? 0, CultureInfo.InvariantCulture);
            if (favoriteCount == 0)
            {
                return (false, "Fiyat alarmi sadece favorilere eklenmis oteller icin acilabilir.");
            }
        }

        if (!form.Enabled)
        {
            const string disableSql = @"
                UPDATE user_favorite_price_alerts
                SET aktif_mi = 0,
                    guncellenme_tarihi = CURRENT_TIMESTAMP
                WHERE user_id = @userId
                  AND otel_id = @hotelId;";
            await using var disableCommand = new SqlCommand(disableSql, connection);
            disableCommand.Parameters.AddWithValue("@userId", userId);
            disableCommand.Parameters.AddWithValue("@hotelId", form.HotelId);
            await disableCommand.ExecuteNonQueryAsync(cancellationToken);
            return (true, "Fiyat alarmi kapatildi.");
        }

        if (!TryParsePrice(form.TargetPriceText, out var targetPrice) || targetPrice <= 0)
        {
            return (false, "Hedef fiyat alanina gecerli bir tutar giriniz.");
        }

        if (!DateOnly.TryParseExact(form.StartDateText, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var startDate))
        {
            return (false, "Baslangic tarihi zorunludur.");
        }

        if (!DateOnly.TryParseExact(form.EndDateText, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var endDate))
        {
            return (false, "Bitis tarihi zorunludur.");
        }

        if (endDate < startDate)
        {
            return (false, "Bitis tarihi baslangic tarihinden once olamaz.");
        }

        if (startDate < DateOnly.FromDateTime(DateTime.Today))
        {
            return (false, "Baslangic tarihi bugunden once olamaz.");
        }

        if (endDate.DayNumber - startDate.DayNumber > 365)
        {
            return (false, "Fiyat alarmi icin maksimum 365 gunluk tarih araligi secilebilir.");
        }

        const string upsertSql = @"
            MERGE user_favorite_price_alerts AS target
            USING (SELECT @userId AS user_id, @hotelId AS otel_id) AS source
            ON target.user_id = source.user_id AND target.otel_id = source.otel_id
            WHEN MATCHED THEN
                UPDATE SET
                    hedef_maksimum_fiyat = @targetPrice,
                    baslangic_tarihi = @startDate,
                    bitis_tarihi = @endDate,
                    aktif_mi = 1,
                    guncellenme_tarihi = CURRENT_TIMESTAMP
            WHEN NOT MATCHED THEN
                INSERT (user_id, otel_id, hedef_maksimum_fiyat, baslangic_tarihi, bitis_tarihi, aktif_mi, olusturulma_tarihi, guncellenme_tarihi)
                VALUES (@userId, @hotelId, @targetPrice, @startDate, @endDate, 1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);";
        await using var command = new SqlCommand(upsertSql, connection);
        command.Parameters.AddWithValue("@userId", userId);
        command.Parameters.AddWithValue("@hotelId", form.HotelId);
        command.Parameters.AddWithValue("@targetPrice", targetPrice);
        command.Parameters.AddWithValue("@startDate", startDate.ToDateTime(TimeOnly.MinValue));
        command.Parameters.AddWithValue("@endDate", endDate.ToDateTime(TimeOnly.MinValue));
        await command.ExecuteNonQueryAsync(cancellationToken);

        return (true, "Fiyat alarmi aktif edildi. Belirlediginiz aralikta fiyat uygunsa e-posta ile bilgilendirileceksiniz.");
    }

    public async Task<(bool Success, string Message)> DeletePriceAlertAsync(long userId, long hotelId, CancellationToken cancellationToken = default)
    {
        if (userId <= 0 || hotelId <= 0)
        {
            return (false, "Fiyat alarmi silme icin gecersiz kullanici veya otel secimi.");
        }

        var connectionString = _configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return (false, "Veritabani baglantisi bulunamadi.");
        }

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        const string deleteSql = @"
            DELETE FROM user_favorite_price_alerts
            WHERE user_id = @userId
              AND otel_id = @hotelId;";
        await using var command = new SqlCommand(deleteSql, connection);
        command.Parameters.AddWithValue("@userId", userId);
        command.Parameters.AddWithValue("@hotelId", hotelId);
        var affectedRows = await command.ExecuteNonQueryAsync(cancellationToken);

        return affectedRows > 0
            ? (true, "Fiyat alarmi veritabanindan silindi.")
            : (false, "Silinecek aktif fiyat alarmi bulunamadi.");
    }

    private static async Task<bool> HotelExistsAsync(SqlConnection connection, long hotelId, CancellationToken cancellationToken)
    {
        const string sql = @"SELECT COUNT(*) FROM oteller WHERE id = @hotelId AND yayin_durumu = 'Yayında' AND onay_durumu = 'Onaylandı';";
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@hotelId", hotelId);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result, CultureInfo.InvariantCulture) > 0;
    }

    private static object DbValue(string? value) => string.IsNullOrWhiteSpace(value) ? DBNull.Value : value.Trim();

    private static string SafeTrim(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }

    private static bool TryParsePrice(string? value, out decimal price)
    {
        price = 0m;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var normalized = value.Trim().Replace("TRY", string.Empty, StringComparison.OrdinalIgnoreCase).Replace("₺", string.Empty).Trim();
        return decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out price)
               || decimal.TryParse(normalized, NumberStyles.Number, TrCulture, out price);
    }

    private static string NormalizeImageUrl(string? imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            return string.Empty;
        }

        return imageUrl.StartsWith("/", StringComparison.Ordinal) ? imageUrl : "/" + imageUrl.TrimStart('/');
    }

    private static string BuildSlug(string hotelName, string hotelCode)
    {
        var baseText = string.IsNullOrWhiteSpace(hotelName) ? hotelCode : hotelName;
        var normalized = baseText.ToLowerInvariant()
            .Replace("ı", "i")
            .Replace("ğ", "g")
            .Replace("ü", "u")
            .Replace("ş", "s")
            .Replace("ö", "o")
            .Replace("ç", "c");

        var buffer = new List<char>(normalized.Length);
        var lastDash = false;
        foreach (var character in normalized)
        {
            if (char.IsLetterOrDigit(character))
            {
                buffer.Add(character);
                lastDash = false;
            }
            else if (!lastDash)
            {
                buffer.Add('-');
                lastDash = true;
            }
        }

        var slug = new string(buffer.ToArray()).Trim('-');
        return string.IsNullOrWhiteSpace(slug) ? hotelCode.ToLowerInvariant() : slug;
    }
}
