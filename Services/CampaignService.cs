using System.Globalization;
using System.Data.Common;
using Microsoft.Data.SqlClient;
using SqlConnection = Microsoft.Data.SqlClient.SqlConnection;
using SqlCommand = Microsoft.Data.SqlClient.SqlCommand;
using SqlTransaction = Microsoft.Data.SqlClient.SqlTransaction;
using SqlException = Microsoft.Data.SqlClient.SqlException;
using otelturizmnew.Models.Kampanyalar;
using otelturizmnew.Models.Oteller;
using otelturizmnew.Services.Abstractions;

namespace otelturizmnew.Services;

public class CampaignService : ICampaignService
{
    private readonly string _connectionString;

    public CampaignService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection tanimli degil.");
    }

    public async Task<CampaignListingPageViewModel> GetCampaignListingPageAsync(string? preset = null, CancellationToken cancellationToken = default)
    {
        var model = new CampaignListingPageViewModel();

        await using var connection = await CreateOpenConnectionAsync(cancellationToken);

        const string sql = @"
            SELECT
                k.id,
                k.kampanya_kodu,
                k.kampanya_adi,
                k.seo_slug,
                k.sayfa_url,
                COALESCE(NULLIF(k.kisa_aciklama, ''), LEFT(k.kampanya_aciklamasi, 220)) AS kisa_aciklama,
                k.baslangic_tarihi,
                k.bitis_tarihi,
                k.kampanya_etiketi,
                k.promo_badge,
                COALESCE(NULLIF(k.kampanya_renk_kodu, ''), '#003B95') AS kampanya_renk_kodu,
                COALESCE(NULLIF(k.kart_gorseli, ''), NULLIF(k.hero_gorseli, ''), NULLIF(k.banner_gorseli, '')) AS gorsel_url,
                COALESCE(k.one_cikan_kampanya, 0) AS one_cikan_kampanya,
                (
                    SELECT COUNT(*)
                    FROM kampanya_oteller ko
                    JOIN oteller o ON o.id = ko.otel_id
                    WHERE ko.kampanya_id = k.id
                      AND ko.katilim_durumu = 'Aktif'
                      AND SYSUTCDATETIME() BETWEEN ko.baslangic_tarihi AND ko.bitis_tarihi
                      AND o.yayin_durumu = 'Yayında'
                      AND o.onay_durumu = 'Onaylandı'
                ) AS hotel_count
            FROM kampanyalar k
            WHERE k.aktif_mi = 1
              AND k.gorunurluk_durumu = 'Yayında'
              AND SYSUTCDATETIME() BETWEEN k.baslangic_tarihi AND k.bitis_tarihi
              AND (
                    @preset = ''
                    OR k.kampanya_etiketi LIKE '%' + @preset + '%'
                    OR k.promo_badge LIKE '%' + @preset + '%'
                    OR k.kampanya_adi LIKE '%' + @preset + '%'
                    OR k.kampanya_kodu LIKE '%' + @preset + '%'
                  )
            ORDER BY k.one_cikan_kampanya DESC, k.aktif_sayfa_vitrini DESC, k.siralama ASC, k.id ASC;";

        await using var command = CreateCommand(connection, sql);
        AddParameter(command, "@preset", (preset ?? string.Empty).Trim());
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var slug = reader.IsDBNull(3) ? string.Empty : reader.GetString(3);
            var url = reader.IsDBNull(4) ? string.Empty : reader.GetString(4);
            var campaignUrl = NormalizeCampaignPath(
                !string.IsNullOrWhiteSpace(url)
                    ? url
                    : $"/kampanyalar/{slug}");

            model.Campaigns.Add(new CampaignCardViewModel
            {
                CampaignId = reader.GetInt64(0),
                CampaignCode = reader.GetString(1),
                CampaignName = reader.GetString(2),
                Slug = slug,
                Url = campaignUrl,
                ShortDescription = reader.IsDBNull(5) ? "Aktif kampanya detaylarini inceleyin." : reader.GetString(5),
                DateText = BuildDateText(reader.GetDateTime(6), reader.GetDateTime(7)),
                BadgeText = reader.IsDBNull(8) ? null : reader.GetString(8),
                PromoBadge = reader.IsDBNull(9) ? null : reader.GetString(9),
                ColorCode = reader.GetString(10),
                HeroImageUrl = NormalizeImageUrl(reader.IsDBNull(11) ? null : reader.GetString(11)),
                IsFeatured = !reader.IsDBNull(12) && reader.GetBoolean(12),
                HotelCount = reader.IsDBNull(13) ? 0 : Convert.ToInt32(reader.GetValue(13), CultureInfo.InvariantCulture)
            });
        }

        return model;
    }

    public async Task<CampaignDetailPageViewModel?> GetCampaignDetailPageAsync(
        string slug,
        string? q = null,
        string? city = null,
        string? district = null,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            return null;
        }

        await using var connection = await CreateOpenConnectionAsync(cancellationToken);

        const string campaignSql = @"
            SELECT
                k.id,
                k.kampanya_kodu,
                k.kampanya_adi,
                k.seo_slug,
                COALESCE(NULLIF(k.canonical_url, ''), NULLIF(k.sayfa_url, ''), CONCAT('/kampanyalar/', k.seo_slug)) AS canonical_url,
                COALESCE(NULLIF(k.kisa_aciklama, ''), LEFT(k.kampanya_aciklamasi, 220)) AS kisa_aciklama,
                COALESCE(NULLIF(k.detay_aciklama, ''), k.kampanya_aciklamasi) AS detay_aciklama,
                COALESCE(NULLIF(k.listeleme_basligi, ''), k.kampanya_adi) AS listeleme_basligi,
                COALESCE(NULLIF(k.listeleme_aciklamasi, ''), COALESCE(NULLIF(k.kisa_aciklama, ''), LEFT(k.kampanya_aciklamasi, 220))) AS listeleme_aciklamasi,
                k.baslangic_tarihi,
                k.bitis_tarihi,
                k.kullanim_kosullari,
                k.kampanya_etiketi,
                k.promo_badge,
                COALESCE(NULLIF(k.hero_gorseli, ''), NULLIF(k.banner_gorseli, ''), NULLIF(k.kart_gorseli, '')) AS hero_gorseli,
                COALESCE(NULLIF(k.kart_gorseli, ''), NULLIF(k.hero_gorseli, ''), NULLIF(k.banner_gorseli, '')) AS kart_gorseli,
                COALESCE(NULLIF(k.kampanya_renk_kodu, ''), '#003B95') AS kampanya_renk_kodu,
                COALESCE(k.one_cikan_kampanya, 0) AS one_cikan_kampanya
            FROM kampanyalar k
            WHERE k.aktif_mi = 1
              AND k.gorunurluk_durumu = 'Yayında'
              AND SYSUTCDATETIME() BETWEEN k.baslangic_tarihi AND k.bitis_tarihi
              AND (
                    k.seo_slug = @slug
                    OR k.sayfa_url = @slug
                    OR k.sayfa_url LIKE '%' + @slashSlug
                  )
            ORDER BY k.id
            OFFSET 0 ROWS FETCH NEXT 1 ROWS ONLY;";

        CampaignDetailPageViewModel? model = null;

        await using (var command = CreateCommand(connection, campaignSql))
        {
            AddParameter(command, "@slug", slug.Trim());
            AddParameter(command, "@slashSlug", $"%/{slug.Trim()}");
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                model = new CampaignDetailPageViewModel
                {
                    CampaignId = reader.GetInt64(0),
                    CampaignCode = reader.GetString(1),
                    CampaignName = reader.GetString(2),
                    Slug = reader.GetString(3),
                    CanonicalUrl = NormalizeCampaignPath(reader.GetString(4)),
                    ShortDescription = reader.GetString(5),
                    LongDescription = reader.GetString(6),
                    ListingTitle = reader.GetString(7),
                    ListingDescription = reader.GetString(8),
                    DateText = BuildDateText(reader.GetDateTime(9), reader.GetDateTime(10)),
                    Terms = reader.IsDBNull(11) ? null : reader.GetString(11),
                    BadgeText = reader.IsDBNull(12) ? null : reader.GetString(12),
                    PromoBadge = reader.IsDBNull(13) ? null : reader.GetString(13),
                    HeroImageUrl = NormalizeImageUrl(reader.IsDBNull(14) ? null : reader.GetString(14)),
                    CardImageUrl = NormalizeImageUrl(reader.IsDBNull(15) ? null : reader.GetString(15)),
                    ColorCode = reader.GetString(16),
                    IsFeatured = !reader.IsDBNull(17) && reader.GetBoolean(17)
                };
            }
        }

        if (model is null)
        {
            return null;
        }

        const string hotelsSql = @"
            SELECT
                o.id,
                o.otel_kodu,
                o.otel_adi,
                o.sehir,
                o.ilce,
                COALESCE(o.ortalama_puan, 0) AS ortalama_puan,
                COALESCE(o.toplam_yorum_sayisi, 0) AS toplam_yorum_sayisi,
                COALESCE(ko.ozel_kampanyali_fiyat, pf.baslangic_fiyat) AS baslangic_fiyat,
                COALESCE(NULLIF(o.kisa_aciklama, ''), @campaignName) AS kisa_aciklama,
                COALESCE(NULLIF(o.kapak_fotografi, ''), NULLIF(og.gorsel_url, '')) AS gorsel_url,
                COALESCE(o.one_cikan_otel, 0) AS one_cikan_otel,
                COALESCE(NULLIF(ko.kampanya_etiketi, ''), NULLIF(k.kampanya_etiketi, '')) AS kampanya_etiketi,
                oz.ozellikler
            FROM kampanya_oteller ko
            JOIN kampanyalar k ON k.id = ko.kampanya_id
            JOIN oteller o ON o.id = ko.otel_id
            LEFT JOIN (
                SELECT
                    ot.otel_id,
                    MIN(
                        CASE
                            WHEN ofm.kapali_satis = 1 THEN NULL
                            WHEN (COALESCE(ofm.toplam_oda_sayisi, ot.toplam_oda_sayisi) - COALESCE(ofm.satilan_oda_sayisi, 0) - COALESCE(ofm.bloke_oda_sayisi, 0)) <= 0 THEN NULL
                            WHEN ofm.gecelik_fiyat IS NULL OR ofm.gecelik_fiyat <= 0 THEN NULL
                            WHEN ofm.indirimli_fiyat IS NOT NULL
                                 AND ofm.indirimli_fiyat > 0
                                 AND ofm.indirimli_fiyat < ofm.gecelik_fiyat
                                THEN ofm.indirimli_fiyat
                            ELSE ofm.gecelik_fiyat
                        END
                    ) AS baslangic_fiyat
                FROM oda_tipleri ot
                LEFT JOIN oda_fiyat_musaitlik ofm ON ofm.oda_tip_id = ot.id
                    AND ofm.otel_id = ot.otel_id
                    AND ofm.tarih BETWEEN CAST(SYSUTCDATETIME() AS date) AND DATEADD(DAY, 45, CAST(SYSUTCDATETIME() AS date))
                WHERE ot.aktif_mi = 1
                GROUP BY ot.otel_id
            ) pf ON pf.otel_id = o.id
            LEFT JOIN (
                SELECT g1.otel_id, g1.gorsel_url
                FROM (
                    SELECT
                        g.otel_id,
                        g.gorsel_url,
                        ROW_NUMBER() OVER (PARTITION BY g.otel_id ORDER BY g.kapak_fotografi_mi DESC, g.one_cikan DESC, g.siralama ASC) AS rn
                    FROM otel_gorselleri g
                    WHERE g.onay_durumu = 'Onaylandı'
                      AND g.gorsel_url NOT LIKE '/uploads/logo/%'
                ) g1
                WHERE g1.rn = 1
            ) og ON og.otel_id = o.id
            LEFT JOIN (
                SELECT
                    oi.otel_id,
                    STRING_AGG(oo.ozellik_adi, '||') WITHIN GROUP (ORDER BY oo.one_cikan_ozellik DESC, oo.siralama ASC) AS ozellikler
                FROM otel_ozellik_iliskileri oi
                JOIN otel_ozellikleri oo ON oo.id = oi.ozellik_id AND oo.aktif_mi = 1
                GROUP BY oi.otel_id
            ) oz ON oz.otel_id = o.id
            WHERE ko.kampanya_id = @campaignId
              AND ko.katilim_durumu = 'Aktif'
              AND SYSUTCDATETIME() BETWEEN ko.baslangic_tarihi AND ko.bitis_tarihi
              AND o.yayin_durumu = 'Yayında'
              AND o.onay_durumu = 'Onaylandı'
            ORDER BY ko.one_cikan DESC, ko.siralama ASC, o.one_cikan_otel DESC, o.ortalama_puan DESC, o.id DESC;";

        await using (var command = CreateCommand(connection, hotelsSql))
        {
            AddParameter(command, "@campaignId", model.CampaignId);
            AddParameter(command, "@campaignName", model.CampaignName);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var hotelTags = new List<string>();
                if (!reader.IsDBNull(11))
                {
                    hotelTags.Add(reader.GetString(11));
                }

                var amenities = reader.IsDBNull(12)
                    ? new List<string>()
                    : reader.GetString(12)
                        .Split("||", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .Take(3)
                        .ToList();

                if (amenities.Count == 0)
                {
                    amenities.AddRange(new[] { "Ücretsiz WiFi", "Merkezi Konum", "7/24 Resepsiyon" });
                }

                var rating = reader.GetDecimal(5);
                var price = reader.IsDBNull(7) ? (decimal?)null : reader.GetDecimal(7);

                model.Hotels.Add(new HotelListingCardViewModel
                {
                    Id = reader.GetInt64(0),
                    HotelCode = reader.GetString(1),
                    Name = reader.GetString(2),
                    Slug = BuildSlug(reader.GetString(2), reader.GetString(1)),
                    City = reader.GetString(3),
                    District = reader.GetString(4),
                    Rating = rating,
                    RatingText = BuildRatingText(rating),
                ReviewCount = reader.IsDBNull(6) ? 0 : Convert.ToInt32(reader.GetValue(6), CultureInfo.InvariantCulture),
                    StartingPrice = price,
                    PriceNote = price.HasValue ? "Kampanyalı · günlük (vergi öncesi)" : "Teklif al",
                    Summary = reader.GetString(8),
                    ImageUrl = NormalizeImageUrl(reader.IsDBNull(9) ? null : reader.GetString(9)),
                    IsFeatured = !reader.IsDBNull(10) && reader.GetBoolean(10),
                    Amenities = amenities,
                    Tags = hotelTags
                });
            }
        }

        var query = (q ?? string.Empty).Trim();
        var cityFilter = (city ?? string.Empty).Trim();
        var districtFilter = (district ?? string.Empty).Trim();
        if (!string.IsNullOrWhiteSpace(query) || !string.IsNullOrWhiteSpace(cityFilter) || !string.IsNullOrWhiteSpace(districtFilter) || minPrice.HasValue || maxPrice.HasValue)
        {
            var normalizedQuery = query.ToLowerInvariant();
            model.Hotels = model.Hotels
                .Where(h =>
                {
                    if (!string.IsNullOrWhiteSpace(normalizedQuery))
                    {
                        var hay = $"{h.Name} {h.City} {h.District} {h.HotelCode}".ToLowerInvariant();
                        if (!hay.Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase))
                        {
                            return false;
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(cityFilter) && !string.Equals(h.City ?? string.Empty, cityFilter, StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }

                    if (!string.IsNullOrWhiteSpace(districtFilter) && !string.Equals(h.District ?? string.Empty, districtFilter, StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }

                    var price = h.StartingPrice;
                    if (minPrice.HasValue && price.HasValue && price.Value < minPrice.Value)
                    {
                        return false;
                    }
                    if (maxPrice.HasValue && price.HasValue && price.Value > maxPrice.Value)
                    {
                        return false;
                    }

                    return true;
                })
                .ToList();
        }

        model.HotelCount = model.Hotels.Count;
        return model;
    }

    private static string BuildDateText(DateTime start, DateTime end)
        => $"{start:dd MMMM yyyy} - {end:dd MMMM yyyy}";

    private static string NormalizeImageUrl(string? imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            return string.Empty;
        }

        if (imageUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || imageUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            || imageUrl.StartsWith("/", StringComparison.OrdinalIgnoreCase))
        {
            return imageUrl;
        }

        return "/" + imageUrl.TrimStart('~', '/').Replace("\\", "/");
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

    private static string BuildRatingText(decimal rating)
    {
        if (rating >= 4.5m) return "Muhteşem";
        if (rating >= 4.0m) return "Fevkalade";
        if (rating >= 3.5m) return "Çok İyi";
        if (rating > 0) return "İyi";
        return "Yorum Bekleniyor";
    }

    private static string NormalizeCampaignPath(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalized = value.Trim();
        if (Uri.TryCreate(normalized, UriKind.Absolute, out var absoluteUri))
        {
            var absolutePath = absoluteUri.AbsolutePath;
            return string.IsNullOrWhiteSpace(absolutePath)
                ? string.Empty
                : absolutePath;
        }

        if (normalized.StartsWith("/", StringComparison.Ordinal))
        {
            return normalized;
        }

        return "/" + normalized.TrimStart('/');
    }

    private async Task<DbConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken)
    {
        DbConnection connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }

    private static DbCommand CreateCommand(DbConnection connection, string sql)
    {
        var command = connection.CreateCommand();
        command.CommandText = sql;
        return command;
    }

    private static void AddParameter(DbCommand command, string name, object? value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value ?? DBNull.Value;
        command.Parameters.Add(parameter);
    }
}
