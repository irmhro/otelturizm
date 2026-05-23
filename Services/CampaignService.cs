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
    private const string PublishStatusSql = "LOWER(REPLACE(LTRIM(RTRIM(o.[YAYIN_DURUMU])), NCHAR(0x0131), N'i')) = N'yayinda'";
    private const string ApprovalStatusSql = "LOWER(REPLACE(LTRIM(RTRIM(o.[ONAY_DURUMU])), NCHAR(0x0131), N'i')) IN (N'onaylandi', N'onaylanmis', N'onayli')";
    private const string CampaignParticipationSql = "LOWER(REPLACE(LTRIM(RTRIM(ko.[KATILIM_DURUMU])), NCHAR(0x0131), N'i')) IN (N'aktif', N'onaylandi', N'onaylanmis')";

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

        var sql = $@"
            SELECT
                k.id,
                k.[KAMPANYA_KODU],
                k.[KAMPANYA_ADI],
                COALESCE(NULLIF(k.[TUR], ''), N'Genel') AS tur,
                k.[SEO_SLUG],
                k.[SAYFA_URL],
                COALESCE(NULLIF(k.[KISA_ACIKLAMA], ''), LEFT(k.[KAMPANYA_ACIKLAMASI], 220)) AS [KISA_ACIKLAMA],
                k.[BASLANGIC_TARIHI],
                k.[BITIS_TARIHI],
                k.[KAMPANYA_ETIKETI],
                k.[PROMO_BADGE],
                COALESCE(NULLIF(k.[KAMPANYA_RENK_KODU], ''), '#003B95') AS [KAMPANYA_RENK_KODU],
                COALESCE(NULLIF(k.[KART_GORSELI], ''), NULLIF(k.[HERO_GORSELI], ''), NULLIF(k.[BANNER_GORSELI], '')) AS [GORSEL_URL],
                COALESCE(k.[ONE_CIKAN_KAMPANYA], 0) AS [ONE_CIKAN_KAMPANYA],
                (
                    SELECT COUNT(*)
                    FROM [dbo].[KAMPANYA_OTELLER] ko
                    JOIN [dbo].[OTELLER] o ON o.id = ko.[OTEL_ID]
                    WHERE ko.[KAMPANYA_ID] = k.id
                      AND {CampaignParticipationSql}
                      AND {PublishStatusSql}
                      AND {ApprovalStatusSql}
                ) AS hotel_count
            FROM [dbo].[KAMPANYALAR] k
            WHERE k.[AKTIF_MI] = 1
              AND (
                    k.[GORUNURLUK_DURUMU] IS NULL
                    OR LTRIM(RTRIM(k.[GORUNURLUK_DURUMU])) = ''
                    OR LOWER(REPLACE(LTRIM(RTRIM(k.[GORUNURLUK_DURUMU])), N'ı', N'i')) IN (N'yayinda', N'yayında')
                  )
              AND (
                    @preset = ''
                    OR k.[KAMPANYA_ETIKETI] LIKE '%' + @preset + '%'
                    OR k.[PROMO_BADGE] LIKE '%' + @preset + '%'
                    OR k.[KAMPANYA_ADI] LIKE '%' + @preset + '%'
                    OR k.[KAMPANYA_KODU] LIKE '%' + @preset + '%'
                  )
            ORDER BY k.[ONE_CIKAN_KAMPANYA] DESC, k.[AKTIF_SAYFA_VITRINI] DESC, k.[SIRALAMA] ASC, k.id ASC;";

        await using var command = CreateCommand(connection, sql);
        AddParameter(command, "@preset", (preset ?? string.Empty).Trim());
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var campaignType = reader.IsDBNull(3) ? "Genel" : reader.GetString(3);
            var slug = reader.IsDBNull(4) ? string.Empty : reader.GetString(4);
            var url = reader.IsDBNull(5) ? string.Empty : reader.GetString(5);
            var campaignUrl = NormalizeCampaignPath(
                !string.IsNullOrWhiteSpace(url)
                    ? url
                    : $"/kampanyalar/{slug}");

            model.Campaigns.Add(new CampaignCardViewModel
            {
                CampaignId = reader.GetInt64(0),
                CampaignCode = reader.GetString(1),
                CampaignName = reader.GetString(2),
                CampaignType = campaignType,
                Slug = slug,
                Url = campaignUrl,
                ShortDescription = reader.IsDBNull(6) ? "Aktif kampanya detaylarini inceleyin." : reader.GetString(6),
                DateText = BuildDateText(reader.GetDateTime(7), reader.GetDateTime(8)),
                StartDateText = reader.GetDateTime(7).ToString("dd.MM.yyyy", CultureInfo.GetCultureInfo("tr-TR")),
                EndDateText = reader.GetDateTime(8).ToString("dd.MM.yyyy", CultureInfo.GetCultureInfo("tr-TR")),
                BadgeText = reader.IsDBNull(9) ? null : reader.GetString(9),
                PromoBadge = reader.IsDBNull(10) ? null : reader.GetString(10),
                ColorCode = reader.GetString(11),
                HeroImageUrl = NormalizeImageUrl(reader.IsDBNull(12) ? null : reader.GetString(12)),
                IsFeatured = !reader.IsDBNull(13) && Convert.ToInt32(reader.GetValue(13), CultureInfo.InvariantCulture) == 1,
                HotelCount = reader.IsDBNull(14) ? 0 : Convert.ToInt32(reader.GetValue(14), CultureInfo.InvariantCulture)
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
                k.[KAMPANYA_KODU],
                k.[KAMPANYA_ADI],
                k.[SEO_SLUG],
                COALESCE(NULLIF(k.[CANONICAL_URL], ''), NULLIF(k.[SAYFA_URL], ''), CONCAT('/kampanyalar/', k.[SEO_SLUG])) AS [CANONICAL_URL],
                COALESCE(NULLIF(k.[KISA_ACIKLAMA], ''), LEFT(k.[KAMPANYA_ACIKLAMASI], 220)) AS [KISA_ACIKLAMA],
                COALESCE(NULLIF(k.[DETAY_ACIKLAMA], ''), k.[KAMPANYA_ACIKLAMASI]) AS [DETAY_ACIKLAMA],
                COALESCE(NULLIF(k.[LISTELEME_BASLIGI], ''), k.[KAMPANYA_ADI]) AS [LISTELEME_BASLIGI],
                COALESCE(NULLIF(k.[LISTELEME_ACIKLAMASI], ''), COALESCE(NULLIF(k.[KISA_ACIKLAMA], ''), LEFT(k.[KAMPANYA_ACIKLAMASI], 220))) AS [LISTELEME_ACIKLAMASI],
                k.[BASLANGIC_TARIHI],
                k.[BITIS_TARIHI],
                k.[KULLANIM_KOSULLARI],
                k.[KAMPANYA_ETIKETI],
                k.[PROMO_BADGE],
                COALESCE(NULLIF(k.[HERO_GORSELI], ''), NULLIF(k.[BANNER_GORSELI], ''), NULLIF(k.[KART_GORSELI], '')) AS [HERO_GORSELI],
                COALESCE(NULLIF(k.[KART_GORSELI], ''), NULLIF(k.[HERO_GORSELI], ''), NULLIF(k.[BANNER_GORSELI], '')) AS [KART_GORSELI],
                COALESCE(NULLIF(k.[KAMPANYA_RENK_KODU], ''), '#003B95') AS [KAMPANYA_RENK_KODU],
                COALESCE(k.[ONE_CIKAN_KAMPANYA], 0) AS [ONE_CIKAN_KAMPANYA]
            FROM [dbo].[KAMPANYALAR] k
            WHERE k.[AKTIF_MI] = 1
              AND (
                    k.[GORUNURLUK_DURUMU] IS NULL
                    OR LTRIM(RTRIM(k.[GORUNURLUK_DURUMU])) = ''
                    OR LOWER(REPLACE(LTRIM(RTRIM(k.[GORUNURLUK_DURUMU])), N'ı', N'i')) IN (N'yayinda', N'yayında')
                  )
              AND (
                    k.[SEO_SLUG] = @slug
                    OR k.[SAYFA_URL] = @slug
                    OR k.[SAYFA_URL] LIKE '%' + @slashSlug
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
                    IsFeatured = !reader.IsDBNull(17) && Convert.ToInt32(reader.GetValue(17), CultureInfo.InvariantCulture) == 1
                };
            }
        }

        if (model is null)
        {
            return null;
        }

        var hotelsSql = $@"
            SELECT
                o.id,
                o.[OTEL_KODU],
                o.[OTEL_ADI],
                o.[SEHIR],
                o.[ILCE],
                COALESCE(o.[ORTALAMA_PUAN], 0) AS [ORTALAMA_PUAN],
                COALESCE(o.[TOPLAM_YORUM_SAYISI], 0) AS [TOPLAM_YORUM_SAYISI],
                COALESCE(ko.[OZEL_KAMPANYALI_FIYAT], pf.baslangic_fiyat) AS baslangic_fiyat,
                COALESCE(NULLIF(o.[KISA_ACIKLAMA], ''), @campaignName) AS [KISA_ACIKLAMA],
                COALESCE(NULLIF(o.[KAPAK_FOTOGRAFI], ''), NULLIF(og.[GORSEL_URL], '')) AS [GORSEL_URL],
                COALESCE(o.[ONE_CIKAN_OTEL], 0) AS [ONE_CIKAN_OTEL],
                COALESCE(NULLIF(ko.[KAMPANYA_ETIKETI], ''), NULLIF(k.[KAMPANYA_ETIKETI], '')) AS [KAMPANYA_ETIKETI],
                oz.[OZELLIKLER]
            FROM [dbo].[KAMPANYA_OTELLER] ko
            JOIN [dbo].[KAMPANYALAR] k ON k.id = ko.[KAMPANYA_ID]
            JOIN [dbo].[OTELLER] o ON o.id = ko.[OTEL_ID]
            LEFT JOIN (
                SELECT
                    ot.[OTEL_ID],
                    MIN(
                        CASE
                            WHEN ofm.[KAPALI_SATIS] = 1 THEN NULL
                            WHEN (COALESCE(ofm.[TOPLAM_ODA_SAYISI], ot.[TOPLAM_ODA_SAYISI]) - COALESCE(ofm.[SATILAN_ODA_SAYISI], 0) - COALESCE(ofm.[BLOKE_ODA_SAYISI], 0)) <= 0 THEN NULL
                            WHEN ofm.[GECELIK_FIYAT] IS NULL OR ofm.[GECELIK_FIYAT] <= 0 THEN NULL
                            WHEN ofm.[INDIRIMLI_FIYAT] IS NOT NULL
                                 AND ofm.[INDIRIMLI_FIYAT] > 0
                                 AND ofm.[INDIRIMLI_FIYAT] < ofm.[GECELIK_FIYAT]
                                THEN ofm.[INDIRIMLI_FIYAT]
                            ELSE ofm.[GECELIK_FIYAT]
                        END
                    ) AS baslangic_fiyat
                FROM [dbo].[ODA_TIPLERI] ot
                LEFT JOIN [dbo].[ODA_FIYAT_MUSAITLIK] ofm ON ofm.[ODA_TIP_ID] = ot.id
                    AND ofm.[OTEL_ID] = ot.[OTEL_ID]
                    AND ofm.[TARIH] BETWEEN CAST(SYSUTCDATETIME() AS date) AND DATEADD(DAY, 45, CAST(SYSUTCDATETIME() AS date))
                WHERE ot.[AKTIF_MI] = 1
                GROUP BY ot.[OTEL_ID]
            ) pf ON pf.[OTEL_ID] = o.id
            LEFT JOIN (
                SELECT g1.[OTEL_ID], g1.[GORSEL_URL]
                FROM (
                    SELECT
                        g.[OTEL_ID],
                        g.[GORSEL_URL],
                        ROW_NUMBER() OVER (PARTITION BY g.[OTEL_ID] ORDER BY g.[KAPAK_FOTOGRAFI_MI] DESC, g.[ONE_CIKAN] DESC, g.[SIRALAMA] ASC) AS rn
                    FROM [dbo].[OTEL_GORSELLERI] g
                    WHERE g.[ONAY_DURUMU] = 'Onaylandı'
                      AND g.[GORSEL_URL] NOT LIKE '/uploads/logo/%'
                ) g1
                WHERE g1.rn = 1
            ) og ON og.[OTEL_ID] = o.id
            LEFT JOIN (
                SELECT
                    oi.[OTEL_ID],
                    STRING_AGG(oo.[OZELLIK_ADI], '||') WITHIN GROUP (ORDER BY oo.[ONE_CIKAN_OZELLIK] DESC, oo.[SIRALAMA] ASC) AS [OZELLIKLER]
                FROM [dbo].[OTEL_OZELLIK_ILISKILERI] oi
                JOIN [dbo].[OTEL_OZELLIKLERI] oo ON oo.id = oi.[OZELLIK_ID] AND oo.[AKTIF_MI] = 1
                GROUP BY oi.[OTEL_ID]
            ) oz ON oz.[OTEL_ID] = o.id
            WHERE ko.[KAMPANYA_ID] = @campaignId
              AND {CampaignParticipationSql}
              AND SYSUTCDATETIME() BETWEEN ko.[BASLANGIC_TARIHI] AND ko.[BITIS_TARIHI]
              AND {PublishStatusSql}
              AND {ApprovalStatusSql}
            ORDER BY ko.[ONE_CIKAN] DESC, ko.[SIRALAMA] ASC, o.[ONE_CIKAN_OTEL] DESC, o.[ORTALAMA_PUAN] DESC, o.id DESC;";

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
                    IsFeatured = !reader.IsDBNull(10) && Convert.ToInt32(reader.GetValue(10), CultureInfo.InvariantCulture) == 1,
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
