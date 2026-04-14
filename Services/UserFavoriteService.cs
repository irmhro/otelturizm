using System.Globalization;
using MySqlConnector;
using otelturizmnew.Models.Oteller;
using otelturizmnew.Models.Paneller.User;
using otelturizmnew.Services.Abstractions;

namespace otelturizmnew.Services;

public class UserFavoriteService : IUserFavoriteService
{
    private readonly IConfiguration _configuration;

    public UserFavoriteService(IConfiguration configuration)
    {
        _configuration = configuration;
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

        await using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new MySqlCommand(sql, connection);
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

        await using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new MySqlCommand(sql, connection);
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
                IFNULL(o.ortalama_puan, 0) AS ortalama_puan,
                IFNULL(o.toplam_yorum_sayisi, 0) AS toplam_yorum_sayisi,
                COALESCE(NULLIF(o.kapak_fotografi, ''), NULLIF(og.gorsel_url, '')) AS gorsel_url,
                pf.baslangic_fiyat
            FROM user_favori_oteller f
            JOIN oteller o ON o.id = f.otel_id
            LEFT JOIN (
                SELECT g.otel_id,
                       SUBSTRING_INDEX(GROUP_CONCAT(g.gorsel_url ORDER BY g.kapak_fotografi_mi DESC, g.one_cikan DESC, g.siralama ASC SEPARATOR '||'),'||',1) AS gorsel_url
                FROM otel_gorselleri g
                WHERE g.onay_durumu = 'Onaylandı'
                GROUP BY g.otel_id
            ) og ON og.otel_id = o.id
            LEFT JOIN (
                SELECT ot.otel_id, MIN(ot.standart_gecelik_fiyat) AS baslangic_fiyat
                FROM oda_tipleri ot
                WHERE ot.aktif_mi = 1
                GROUP BY ot.otel_id
            ) pf ON pf.otel_id = o.id
            WHERE f.user_id = @userId
              AND COALESCE(f.aktif_mi, 1) = 1
              AND o.yayin_durumu = 'Yayında'
              AND o.onay_durumu = 'Onaylandı'
            ORDER BY f.olusturulma_tarihi DESC, f.id DESC;";

        await using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new MySqlCommand(sql, connection);
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
                AddedDateText = $"{createdAt.ToString("dd MMMM yyyy", culture)} tarihinde kaydedildi"
            });
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

        await using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        if (!await HotelExistsAsync(connection, hotelId, cancellationToken))
        {
            return new HotelFavoriteToggleResponse { Success = false, Message = "Favorilere eklenecek otel bulunamadı." };
        }

        const string selectSql = @"SELECT id, COALESCE(aktif_mi, 1) AS aktif_mi FROM user_favori_oteller WHERE user_id = @userId AND otel_id = @hotelId LIMIT 1;";
        await using var selectCommand = new MySqlCommand(selectSql, connection);
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

            await using var insertCommand = new MySqlCommand(insertSql, connection);
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

        await using var updateCommand = new MySqlCommand(updateSql, connection);
        updateCommand.Parameters.AddWithValue("@id", recordId.Value);
        updateCommand.Parameters.AddWithValue("@sourcePage", DbValue(normalizedSourcePage));
        updateCommand.Parameters.AddWithValue("@sourceUrl", DbValue(normalizedSourceUrl));
        updateCommand.Parameters.AddWithValue("@deviceType", DbValue(normalizedDeviceType));
        updateCommand.Parameters.AddWithValue("@ipAddress", DbValue(normalizedIp));
        updateCommand.Parameters.AddWithValue("@isFavorite", isActive ? 0 : 1);
        await updateCommand.ExecuteNonQueryAsync(cancellationToken);

        return new HotelFavoriteToggleResponse
        {
            Success = true,
            IsFavorite = !isActive,
            Message = isActive ? "Otel favorilerinizden çıkarıldı." : "Otel favorilerinize yeniden eklendi."
        };
    }

    private static async Task<bool> HotelExistsAsync(MySqlConnection connection, long hotelId, CancellationToken cancellationToken)
    {
        const string sql = @"SELECT COUNT(*) FROM oteller WHERE id = @hotelId AND yayin_durumu = 'Yayında' AND onay_durumu = 'Onaylandı';";
        await using var command = new MySqlCommand(sql, connection);
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
