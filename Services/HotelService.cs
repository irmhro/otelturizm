using Microsoft.Data.SqlClient;
using SqlConnection = Microsoft.Data.SqlClient.SqlConnection;
using SqlCommand = Microsoft.Data.SqlClient.SqlCommand;
using SqlTransaction = Microsoft.Data.SqlClient.SqlTransaction;
using SqlException = Microsoft.Data.SqlClient.SqlException;
using otelturizmnew.Models.Anasayfa;
using otelturizmnew.Models.Oteller;
using otelturizmnew.Services.Abstractions;
using System.Globalization;
using System.Text.Json;
using System.Text;

namespace otelturizmnew.Services;

public class HotelService : IHotelService
{
    private readonly IConfiguration _configuration;
    private readonly IHotelPricingReadService _hotelPricingReadService;

    public HotelService(IConfiguration configuration, IHotelPricingReadService hotelPricingReadService)
    {
        _configuration = configuration;
        _hotelPricingReadService = hotelPricingReadService;
    }

    // Simple local weather placeholder — replace with real API integration (OpenWeatherMap, etc.)
    private (string icon, string temp) DetermineWeatherForLocation(string city, string district)
    {
        // For now return a neutral sunny icon and a placeholder temperature.
        // In production, call a weather API and map conditions to FontAwesome icons.
        return ("fa-sun", "20°C");
    }

    public async Task<AnasayfaViewModel> GetHomepageAsync(CancellationToken cancellationToken = default)
    {
        return await GetHomepageForSqlServerAsync(cancellationToken);
    }

    private async Task<AnasayfaViewModel> GetHomepageForSqlServerAsync(CancellationToken cancellationToken)
    {
        var connectionString = GetConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return new AnasayfaViewModel();
        }

        var model = new AnasayfaViewModel();
        var hotels = new List<HomeHotelCardViewModel>();
        var destinations = new List<HomeDestinationCardViewModel>();

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        const string hotelSql = """
            SELECT TOP (18)
                o.id,
                o.otel_kodu,
                o.otel_adi,
                o.sehir,
                o.ilce,
                o.otel_turu,
                COALESCE(o.ortalama_puan, 0) AS ortalama_puan,
                COALESCE(o.toplam_yorum_sayisi, 0) AS toplam_yorum_sayisi,
                COALESCE(o.one_cikan_otel, 0) AS one_cikan_otel,
                COALESCE(o.tavsiye_edilen_otel, 0) AS tavsiye_edilen_otel,
                COALESCE(o.kisa_aciklama, '') AS kisa_aciklama,
                o.yildiz_sayisi,
                COALESCE(NULLIF(o.kapak_fotografi, ''), NULLIF(og.gorsel_url, '')) AS gorsel_url,
                pf.baslangic_fiyat,
                pf.min_normal_fiyat,
                pf.min_indirimli_fiyat,
                oz.ozellikler
            FROM oteller o
            LEFT JOIN (
                SELECT
                    ot.otel_id,
                    MIN(
                        CASE
                            WHEN ofm.kapali_satis = 1 THEN NULL
                            WHEN (COALESCE(ofm.toplam_oda_sayisi, ot.toplam_oda_sayisi) - COALESCE(ofm.satilan_oda_sayisi, 0) - COALESCE(ofm.bloke_oda_sayisi, 0)) <= 0 THEN NULL
                            WHEN ofm.gecelik_fiyat IS NULL OR ofm.gecelik_fiyat <= 0 THEN NULL
                            WHEN ofm.kampanya_id IS NOT NULL
                                 AND ofm.kampanya_id > 0
                                 AND ofm.indirimli_fiyat IS NOT NULL
                                 AND ofm.indirimli_fiyat > 0
                                 AND ofm.indirimli_fiyat < ofm.gecelik_fiyat
                                THEN ofm.indirimli_fiyat
                            ELSE ofm.gecelik_fiyat
                        END
                    ) AS baslangic_fiyat,
                    MIN(
                        CASE
                            WHEN ofm.kapali_satis = 1 THEN NULL
                            WHEN (COALESCE(ofm.toplam_oda_sayisi, ot.toplam_oda_sayisi) - COALESCE(ofm.satilan_oda_sayisi, 0) - COALESCE(ofm.bloke_oda_sayisi, 0)) <= 0 THEN NULL
                            WHEN ofm.gecelik_fiyat IS NULL OR ofm.gecelik_fiyat <= 0 THEN NULL
                            ELSE ofm.gecelik_fiyat
                        END
                    ) AS min_normal_fiyat,
                    MIN(
                        CASE
                            WHEN ofm.kapali_satis = 1 THEN NULL
                            WHEN (COALESCE(ofm.toplam_oda_sayisi, ot.toplam_oda_sayisi) - COALESCE(ofm.satilan_oda_sayisi, 0) - COALESCE(ofm.bloke_oda_sayisi, 0)) <= 0 THEN NULL
                            WHEN ofm.kampanya_id IS NULL OR ofm.kampanya_id <= 0 THEN NULL
                            WHEN ofm.gecelik_fiyat IS NULL OR ofm.gecelik_fiyat <= 0 THEN NULL
                            WHEN ofm.indirimli_fiyat IS NULL OR ofm.indirimli_fiyat <= 0 THEN NULL
                            WHEN ofm.indirimli_fiyat >= ofm.gecelik_fiyat THEN NULL
                            ELSE ofm.indirimli_fiyat
                        END
                    ) AS min_indirimli_fiyat
                FROM oda_tipleri ot
                LEFT JOIN oda_fiyat_musaitlik ofm ON ofm.oda_tip_id = ot.id
                    AND ofm.otel_id = ot.otel_id
                    AND ofm.tarih BETWEEN CAST(SYSUTCDATETIME() AS date) AND DATEADD(DAY, 120, CAST(SYSUTCDATETIME() AS date))
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
                    WHERE g.onay_durumu LIKE N'Onaylan%'
                      AND g.gorsel_url NOT LIKE '/uploads/logo/%'
                ) g1
                WHERE g1.rn = 1
            ) og ON og.otel_id = o.id
            LEFT JOIN (
                SELECT
                    oi.otel_id,
                    STRING_AGG(CONCAT(oo.ozellik_adi, '::', COALESCE(oo.ozellik_ikon, 'fa-circle-check')), '||')
                        WITHIN GROUP (ORDER BY oo.one_cikan_ozellik DESC, oo.siralama ASC) AS ozellikler
                FROM otel_ozellik_iliskileri oi
                JOIN otel_ozellikleri oo
                    ON oo.id = oi.ozellik_id
                   AND oo.aktif_mi = 1
                GROUP BY oi.otel_id
            ) oz ON oz.otel_id = o.id
            WHERE o.yayin_durumu = N'Yayında'
              AND o.onay_durumu = N'Onaylandı'
            ORDER BY
                CASE WHEN o.one_cikan_otel = 1 THEN 0 ELSE 1 END,
                CASE WHEN o.tavsiye_edilen_otel = 1 THEN 0 ELSE 1 END,
                CASE WHEN o.ortalama_puan > 0 THEN 0 ELSE 1 END,
                o.ortalama_puan DESC,
                o.toplam_yorum_sayisi DESC,
                o.populerlik_sirasi DESC,
                o.id DESC;
            """;

        await using var command = new SqlCommand(hotelSql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var isFeatured = ReadFlag(reader, "one_cikan_otel");
            var isRecommended = ReadFlag(reader, "tavsiye_edilen_otel");
            var rating = reader.GetDecimal(reader.GetOrdinal("ortalama_puan"));
            var reviewCount = ReadInt(reader, "toplam_yorum_sayisi");
            var imageUrl = reader.IsDBNull(reader.GetOrdinal("gorsel_url"))
                ? string.Empty
                : reader.GetString(reader.GetOrdinal("gorsel_url"));
            var startingPrice = reader.IsDBNull(reader.GetOrdinal("baslangic_fiyat"))
                ? (decimal?)null
                : reader.GetDecimal(reader.GetOrdinal("baslangic_fiyat"));
            var minNormalPrice = reader.IsDBNull(reader.GetOrdinal("min_normal_fiyat"))
                ? (decimal?)null
                : reader.GetDecimal(reader.GetOrdinal("min_normal_fiyat"));
            var minDiscountPrice = reader.IsDBNull(reader.GetOrdinal("min_indirimli_fiyat"))
                ? (decimal?)null
                : reader.GetDecimal(reader.GetOrdinal("min_indirimli_fiyat"));
            var rawAmenities = reader.IsDBNull(reader.GetOrdinal("ozellikler"))
                ? string.Empty
                : reader.GetString(reader.GetOrdinal("ozellikler"));
            var amenities = rawAmenities
                .Split("||", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(ParseAmenity)
                .GroupBy(x => x.Label, StringComparer.OrdinalIgnoreCase)
                .Select(x => x.First())
                .Take(3)
                .ToList();

            if (amenities.Count == 0)
            {
                amenities.AddRange(new[]
                {
                    new HomeAmenityViewModel { Label = "WiFi", IconClass = "fa-wifi" },
                    new HomeAmenityViewModel { Label = "Havuz", IconClass = "fa-water-ladder" },
                    new HomeAmenityViewModel { Label = "Spa", IconClass = "fa-spa" }
                });
            }

            var tags = BuildHomepageTags(isFeatured, isRecommended, rating, startingPrice);
            var referencePrice = minNormalPrice ?? startingPrice;
            var hasDiscount = minDiscountPrice.HasValue
                && minDiscountPrice.Value > 0m
                && referencePrice.HasValue
                && referencePrice.Value > 0m
                && minDiscountPrice.Value < referencePrice.Value;
            var originalPrice = hasDiscount ? decimal.Round(referencePrice!.Value, 0) : (decimal?)null;
            var discountedPrice = hasDiscount ? decimal.Round(minDiscountPrice!.Value, 0) : (decimal?)null;
            var discountPercent = hasDiscount
                ? Math.Clamp((int)Math.Round(((originalPrice!.Value - discountedPrice!.Value) / originalPrice.Value) * 100m, MidpointRounding.AwayFromZero), 1, 95)
                : 0;

            hotels.Add(new HomeHotelCardViewModel
            {
                Id = reader.GetInt64(reader.GetOrdinal("id")),
                HotelCode = reader.GetString(reader.GetOrdinal("otel_kodu")),
                Name = reader.GetString(reader.GetOrdinal("otel_adi")),
                City = reader.GetString(reader.GetOrdinal("sehir")),
                District = reader.GetString(reader.GetOrdinal("ilce")),
                LocationText = $"{reader.GetString(reader.GetOrdinal("ilce"))}, {reader.GetString(reader.GetOrdinal("sehir"))}",
                Rating = rating,
                RatingText = BuildRatingText(rating),
                ReviewCount = reviewCount,
                StartingPrice = startingPrice,
                OriginalPrice = originalPrice,
                DiscountedPrice = discountedPrice,
                DiscountPercent = discountPercent,
                HasDiscount = hasDiscount,
                PriceText = hasDiscount && discountedPrice.HasValue
                    ? $"TRY {discountedPrice.Value:N0}"
                    : startingPrice.HasValue ? $"TRY {startingPrice.Value:N0}" : "Teklif Al",
                PriceNote = startingPrice.HasValue ? "Baslayan fiyatlarla, gecelik" : "Musait fiyat bilgisi bulunamadi",
                ImageUrl = NormalizeImageUrl(imageUrl),
                DetailSlug = BuildSlug(reader.GetString(reader.GetOrdinal("otel_adi")), reader.GetString(reader.GetOrdinal("otel_kodu"))),
                Amenities = amenities,
                Tags = tags,
                IsSmartPrice = isFeatured || isRecommended || (startingPrice.HasValue && startingPrice.Value <= 3500m)
            });
        }

        await reader.CloseAsync();

        const string destinationSql = """
            WITH ranked_destinations AS (
                SELECT
                    o.sehir,
                    o.ilce,
                    COUNT(*) OVER (PARTITION BY o.sehir, o.ilce) AS hotel_count,
                    COALESCE(NULLIF(o.kapak_fotografi, ''), '') AS image_url,
                    o.otel_adi AS lead_hotel,
                    MAX(COALESCE(o.one_cikan_otel, 0)) OVER (PARTITION BY o.sehir, o.ilce) AS max_featured,
                    MAX(COALESCE(o.ortalama_puan, 0)) OVER (PARTITION BY o.sehir, o.ilce) AS max_rating,
                    ROW_NUMBER() OVER (
                        PARTITION BY o.sehir, o.ilce
                        ORDER BY COALESCE(o.one_cikan_otel, 0) DESC, COALESCE(o.ortalama_puan, 0) DESC, COALESCE(o.populerlik_sirasi, 0) DESC, o.id DESC
                    ) AS rn
                FROM oteller o
                WHERE o.yayin_durumu = N'Yayında'
                  AND o.onay_durumu = N'Onaylandı'
            )
            SELECT TOP (6)
                sehir,
                ilce,
                hotel_count,
                lead_hotel,
                image_url
            FROM ranked_destinations
            WHERE rn = 1
            ORDER BY hotel_count DESC, max_featured DESC, max_rating DESC;
            """;

        await using var destinationCommand = new SqlCommand(destinationSql, connection);
        await using var destinationReader = await destinationCommand.ExecuteReaderAsync(cancellationToken);
        while (await destinationReader.ReadAsync(cancellationToken))
        {
            var city = destinationReader.GetString(destinationReader.GetOrdinal("sehir"));
            var district = destinationReader.GetString(destinationReader.GetOrdinal("ilce"));
            var hotelCount = destinationReader.GetInt32(destinationReader.GetOrdinal("hotel_count"));
            var leadHotel = destinationReader.IsDBNull(destinationReader.GetOrdinal("lead_hotel"))
                ? string.Empty
                : destinationReader.GetString(destinationReader.GetOrdinal("lead_hotel"));
            var imageUrl = destinationReader.IsDBNull(destinationReader.GetOrdinal("image_url"))
                ? string.Empty
                : destinationReader.GetString(destinationReader.GetOrdinal("image_url"));

            destinations.Add(new HomeDestinationCardViewModel
            {
                City = city,
                District = district,
                HotelCount = hotelCount,
                LeadText = string.IsNullOrWhiteSpace(leadHotel)
                    ? $"{district} bolgesinde secili oteller"
                    : $"{leadHotel} ve benzeri tesisler",
                ImageUrl = NormalizeImageUrl(imageUrl),
                ListingUrl = $"/oteller/{NormalizeRouteSegment(city)}"
            });
        }

        model.PopularHotels = hotels.Take(8).ToList();
        model.WeekendHotels = hotels
            .Where(x => x.City.Equals("Istanbul", StringComparison.OrdinalIgnoreCase) || x.City.Equals("İstanbul", StringComparison.OrdinalIgnoreCase))
            .Take(8)
            .ToList();

        if (model.WeekendHotels.Count == 0)
        {
            model.WeekendHotels = hotels.Skip(2).Take(8).ToList();
        }

        model.HeroSlides = hotels
            .Where(x => !string.IsNullOrWhiteSpace(x.ImageUrl))
            .Take(4)
            .Select(x => new HomeHeroSlideViewModel
            {
                Title = x.Name,
                Subtitle = x.LocationText,
                ImageUrl = x.ImageUrl,
                DetailSlug = x.DetailSlug,
                WeatherIcon = x.WeatherIcon,
                Temperature = x.Temperature
            })
            .ToList();

        if (model.HeroSlides.Count == 0)
        {
            model.HeroSlides = model.PopularHotels.Take(3).Select(x => new HomeHeroSlideViewModel
            {
                Title = x.Name,
                Subtitle = x.LocationText,
                ImageUrl = x.ImageUrl,
                DetailSlug = x.DetailSlug
            }).ToList();
        }

        model.PopularDestinations = destinations;
        return model;
    }

    public async Task<HotelListingPageViewModel> GetHotelListingPageAsync(string? searchTerm, string? campaignTag = null, CancellationToken cancellationToken = default)
    {
        return await GetHotelListingPageForSqlServerAsync(searchTerm, campaignTag, cancellationToken);
    }

    public async Task<List<HotelSearchSuggestionViewModel>> GetSearchSuggestionsAsync(string query, CancellationToken cancellationToken = default)
    {
        return await GetSearchSuggestionsForSqlServerAsync(query, cancellationToken);
    }

    private async Task<List<(HotelSearchSuggestionViewModel Item, int Score)>> LoadFuzzySearchCandidatesAsync(SqlConnection connection, string normalizedQueryKeyword, CancellationToken cancellationToken)
    {
        var candidates = new List<HotelSearchSuggestionViewModel>();
        if (string.IsNullOrWhiteSpace(normalizedQueryKeyword))
        {
            return new List<(HotelSearchSuggestionViewModel Item, int Score)>();
        }

        const string sql = """
            SELECT TOP (150) suggestion_value, suggestion_label, suggestion_type
            FROM (
                SELECT DISTINCT o.sehir AS suggestion_value, o.sehir AS suggestion_label, 'Sehir' AS suggestion_type
                FROM oteller o
                WHERE o.yayin_durumu = N'Yayında' AND o.onay_durumu = N'Onaylandı'

                UNION

                SELECT DISTINCT o.ilce AS suggestion_value, CONCAT(o.ilce, ' / ', o.sehir) AS suggestion_label, 'Ilce' AS suggestion_type
                FROM oteller o
                WHERE o.yayin_durumu = N'Yayında' AND o.onay_durumu = N'Onaylandı'

                UNION

                SELECT DISTINCT o.mahalle AS suggestion_value, CONCAT(o.mahalle, ' / ', o.ilce, ' / ', o.sehir) AS suggestion_label, 'Mahalle' AS suggestion_type
                FROM oteller o
                WHERE o.yayin_durumu = N'Yayında'
                  AND o.onay_durumu = N'Onaylandı'
                  AND o.mahalle IS NOT NULL
                  AND o.mahalle <> ''

                UNION

                SELECT DISTINCT o.otel_adi AS suggestion_value, CONCAT(o.otel_adi, ' / ', o.ilce, ' / ', o.sehir) AS suggestion_label, 'Otel' AS suggestion_type
                FROM oteller o
                WHERE o.yayin_durumu = N'Yayında' AND o.onay_durumu = N'Onaylandı'
            ) AS suggestions;
            """;

        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            candidates.Add(new HotelSearchSuggestionViewModel
            {
                Value = reader.GetString(0),
                Label = reader.GetString(1),
                Type = reader.GetString(2)
            });
        }

        return candidates
            .Select(item => (Item: item, Score: ComputeSuggestionScore(normalizedQueryKeyword, NormalizeSearchKeyword(item.Value))))
            .Where(x => x.Score >= 55)
            .ToList();
    }

    private static string NormalizeSearchKeyword(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalized = NormalizeRouteSegment(value)
            .Replace('-', ' ')
            .Trim();

        while (normalized.Contains("  ", StringComparison.Ordinal))
        {
            normalized = normalized.Replace("  ", " ", StringComparison.Ordinal);
        }

        return normalized;
    }

    private static string BuildSearchNormalizationSql(string fieldExpression)
    {
        var expression = $"LOWER(COALESCE({fieldExpression}, ''))";
        foreach (var replacement in new (string From, string To)[]
                 {
                     ("İ", "i"),
                     ("I", "i"),
                     ("ı", "i"),
                     ("Ç", "c"),
                     ("ç", "c"),
                     ("Ğ", "g"),
                     ("ğ", "g"),
                     ("Ö", "o"),
                     ("ö", "o"),
                     ("Ş", "s"),
                     ("ş", "s"),
                     ("Ü", "u"),
                     ("ü", "u")
                 })
        {
            expression = $"REPLACE({expression}, '{replacement.From}', '{replacement.To}')";
        }

        return expression;
    }

    private static int ComputeSuggestionScore(string query, string candidate)
    {
        if (string.IsNullOrWhiteSpace(query) || string.IsNullOrWhiteSpace(candidate))
        {
            return 0;
        }

        if (string.Equals(query, candidate, StringComparison.OrdinalIgnoreCase))
        {
            return 100;
        }

        if (candidate.StartsWith(query, StringComparison.OrdinalIgnoreCase))
        {
            return 94;
        }

        if (candidate.Contains(query, StringComparison.OrdinalIgnoreCase))
        {
            return 86;
        }

        if (candidate.Split(' ', StringSplitOptions.RemoveEmptyEntries).Any(token => token.StartsWith(query, StringComparison.OrdinalIgnoreCase)))
        {
            return 82;
        }

        var distance = ComputeLevenshteinDistance(query, candidate);
        var maxLength = Math.Max(query.Length, candidate.Length);
        if (maxLength == 0)
        {
            return 0;
        }

        var similarity = 1d - (double)distance / maxLength;
        return similarity >= 0.45d
            ? (int)Math.Round(similarity * 100d)
            : 0;
    }

    private static int ComputeLevenshteinDistance(string source, string target)
    {
        var costs = new int[target.Length + 1];
        for (var j = 0; j <= target.Length; j++)
        {
            costs[j] = j;
        }

        for (var i = 1; i <= source.Length; i++)
        {
            var previousDiagonal = costs[0];
            costs[0] = i;
            for (var j = 1; j <= target.Length; j++)
            {
                var previousAbove = costs[j];
                var substitutionCost = source[i - 1] == target[j - 1] ? 0 : 1;
                costs[j] = Math.Min(
                    Math.Min(costs[j] + 1, costs[j - 1] + 1),
                    previousDiagonal + substitutionCost);
                previousDiagonal = previousAbove;
            }
        }

        return costs[target.Length];
    }

    public async Task<HotelDetailPageViewModel?> GetHotelDetailPageAsync(string slug, CancellationToken cancellationToken = default)
    {
        return await GetHotelDetailPageForSqlServerAsync(slug, cancellationToken);
    }

    private async Task<HotelListingPageViewModel> GetHotelListingPageForSqlServerAsync(string? searchTerm, string? campaignTag, CancellationToken cancellationToken, bool allowFuzzyFallback = true)
    {
        var normalizedSearchTerm = string.IsNullOrWhiteSpace(searchTerm) ? string.Empty : searchTerm.Trim();
        var normalizedSearchKeyword = NormalizeSearchKeyword(normalizedSearchTerm);
        var displayLabel = string.IsNullOrWhiteSpace(normalizedSearchTerm) ? "Tüm bölgeler" : normalizedSearchTerm;
        var model = new HotelListingPageViewModel
        {
            City = displayLabel,
            SearchTerm = normalizedSearchTerm,
            SearchLabel = displayLabel
        };

        var connectionString = GetConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return model;
        }

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        var normalizedCitySql = BuildSearchNormalizationSql("o.sehir");
        var normalizedDistrictSql = BuildSearchNormalizationSql("o.ilce");
        var normalizedNeighborhoodSql = BuildSearchNormalizationSql("COALESCE(o.mahalle, '')");
        var normalizedHotelNameSql = BuildSearchNormalizationSql("o.otel_adi");
        var normalizedCompositeSql = BuildSearchNormalizationSql("CONCAT(o.mahalle, ' ', o.ilce, ' ', o.sehir)");

        var sql = $"""
            SELECT
                o.id,
                o.otel_kodu,
                o.otel_adi,
                o.otel_turu,
                o.yildiz_sayisi,
                o.sehir,
                o.ilce,
                COALESCE(o.ortalama_puan, 0) AS ortalama_puan,
                COALESCE(o.toplam_yorum_sayisi, 0) AS toplam_yorum_sayisi,
                COALESCE(o.kisa_aciklama, '') AS kisa_aciklama,
                COALESCE(o.one_cikan_otel, 0) AS one_cikan_otel,
                COALESCE(NULLIF(o.kapak_fotografi, ''), NULLIF(og.gorsel_url, '')) AS gorsel_url,
                pf.baslangic_fiyat,
                oz.ozellikler
            FROM oteller o
            LEFT JOIN (
                SELECT
                    ot.otel_id,
                    MIN(
                        CASE
                            WHEN ofm.kapali_satis = 1 THEN NULL
                            WHEN (COALESCE(ofm.toplam_oda_sayisi, ot.toplam_oda_sayisi) - COALESCE(ofm.satilan_oda_sayisi, 0) - COALESCE(ofm.bloke_oda_sayisi, 0)) <= 0 THEN NULL
                            WHEN ofm.gecelik_fiyat IS NULL OR ofm.gecelik_fiyat <= 0 THEN NULL
                            WHEN ofm.kampanya_id IS NOT NULL
                                 AND ofm.kampanya_id > 0
                                 AND ofm.indirimli_fiyat IS NOT NULL
                                 AND ofm.indirimli_fiyat > 0
                                 AND ofm.indirimli_fiyat < ofm.gecelik_fiyat
                                THEN ofm.indirimli_fiyat
                            ELSE ofm.gecelik_fiyat
                        END
                    ) AS baslangic_fiyat
                FROM oda_tipleri ot
                LEFT JOIN oda_fiyat_musaitlik ofm ON ofm.oda_tip_id = ot.id
                    AND ofm.otel_id = ot.otel_id
                    AND ofm.tarih BETWEEN CAST(SYSUTCDATETIME() AS date) AND DATEADD(DAY, 120, CAST(SYSUTCDATETIME() AS date))
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
                    WHERE g.onay_durumu LIKE N'Onaylan%'
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
            WHERE o.yayin_durumu = N'Yayında'
              AND o.onay_durumu = N'Onaylandı'
              AND (
                    @searchTerm = ''
                    OR {normalizedCitySql} = @searchTermNormalized
                    OR {normalizedDistrictSql} = @searchTermNormalized
                    OR {normalizedNeighborhoodSql} = @searchTermNormalized
                    OR {normalizedHotelNameSql} LIKE '%' + @searchTermNormalized + '%'
                    OR {normalizedCompositeSql} LIKE '%' + @searchTermNormalized + '%'
                  )
            ORDER BY
                CASE
                    WHEN {normalizedHotelNameSql} = @searchTermNormalized THEN 0
                    WHEN {normalizedDistrictSql} = @searchTermNormalized THEN 1
                    WHEN {normalizedNeighborhoodSql} = @searchTermNormalized THEN 2
                    WHEN {normalizedCitySql} = @searchTermNormalized THEN 3
                    ELSE 4
                END,
                CASE WHEN o.one_cikan_otel = 1 THEN 0 ELSE 1 END,
                CASE WHEN o.ortalama_puan > 0 THEN 0 ELSE 1 END,
                o.ortalama_puan DESC,
                o.toplam_yorum_sayisi DESC,
                o.populerlik_sirasi DESC,
                o.id DESC;
            """;

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@searchTerm", normalizedSearchTerm);
        command.Parameters.AddWithValue("@searchTermNormalized", normalizedSearchKeyword);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var id = reader.GetInt64(reader.GetOrdinal("id"));
            var hotelCode = reader.GetString(reader.GetOrdinal("otel_kodu"));
            var name = reader.GetString(reader.GetOrdinal("otel_adi"));
            var hotelCity = reader.GetString(reader.GetOrdinal("sehir"));
            var district = reader.GetString(reader.GetOrdinal("ilce"));
            var hotelType = reader.GetString(reader.GetOrdinal("otel_turu"));
            var rating = reader.GetDecimal(reader.GetOrdinal("ortalama_puan"));
            var reviewCount = ReadInt(reader, "toplam_yorum_sayisi");
            var summary = reader.GetString(reader.GetOrdinal("kisa_aciklama"));
            var isFeatured = ReadFlag(reader, "one_cikan_otel");
            var imageUrl = reader.IsDBNull(reader.GetOrdinal("gorsel_url")) ? string.Empty : reader.GetString(reader.GetOrdinal("gorsel_url"));
            var startingPrice = reader.IsDBNull(reader.GetOrdinal("baslangic_fiyat")) ? (decimal?)null : reader.GetDecimal(reader.GetOrdinal("baslangic_fiyat"));
            var rawAmenities = reader.IsDBNull(reader.GetOrdinal("ozellikler")) ? string.Empty : reader.GetString(reader.GetOrdinal("ozellikler"));

            var amenities = rawAmenities
                .Split("||", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(3)
                .ToList();
            if (amenities.Count == 0)
            {
                amenities.AddRange(new[] { "24 Saat Resepsiyon", "Ucretsiz WiFi", "Restoran" });
            }

            model.Hotels.Add(new HotelListingCardViewModel
            {
                Id = id,
                HotelCode = hotelCode,
                PropertyType = hotelType,
                StarCount = reader.IsDBNull(reader.GetOrdinal("yildiz_sayisi")) ? null : reader.GetByte(reader.GetOrdinal("yildiz_sayisi")),
                Name = name,
                Slug = BuildSlug(name, hotelCode),
                City = hotelCity,
                District = district,
                Rating = rating,
                RatingText = BuildRatingText(rating),
                ReviewCount = reviewCount,
                StartingPrice = startingPrice,
                PriceNote = startingPrice.HasValue ? "Baslayan fiyatlarla, gecelik" : "Musait fiyat bilgisi bulunamadi",
                ImageUrl = NormalizeImageUrl(imageUrl),
                IsFeatured = isFeatured,
                Amenities = amenities,
                Tags = BuildTags(isFeatured, rating, reviewCount, startingPrice),
                Summary = string.IsNullOrWhiteSpace(summary)
                    ? "Sehir konaklamasi, esnek rezervasyon ve mobil uyumlu deneyim icin yayindaki tesis."
                    : summary
            });
        }

        await reader.DisposeAsync();

        model.ActiveTag = NormalizeCampaignTag(campaignTag);
        model.Hotels = ApplyCampaignFilter(model.Hotels, model.ActiveTag).ToList();
        model.TotalCount = model.Hotels.Count;
        model.MinPrice = model.Hotels.Where(x => x.StartingPrice.HasValue).Select(x => x.StartingPrice!.Value).DefaultIfEmpty(0).Min();
        model.MaxPrice = model.Hotels.Where(x => x.StartingPrice.HasValue).Select(x => x.StartingPrice!.Value).DefaultIfEmpty(0).Max();
        model.Districts = model.Hotels.Select(x => x.District).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToList();
        model.StarOptions = new List<int> { 5, 4, 3, 2, 1 };
        var campaignMeta = GetCampaignMeta(model.ActiveTag);
        model.CampaignTitle = campaignMeta.Title;
        model.CampaignDescription = campaignMeta.Description;
        model.QuickLinks = BuildListingQuickLinks(displayLabel, model.ActiveTag);

        if (allowFuzzyFallback
            && model.Hotels.Count == 0
            && !string.IsNullOrWhiteSpace(normalizedSearchKeyword))
        {
            var fuzzyMatches = await LoadFuzzySearchCandidatesAsync(connection, normalizedSearchKeyword, cancellationToken);
            var bestMatch = fuzzyMatches
                .OrderByDescending(x => x.Score)
                .ThenBy(x => x.Item.Type)
                .ThenBy(x => x.Item.Label, StringComparer.OrdinalIgnoreCase)
                .Select(x => x.Item)
                .FirstOrDefault();

            if (bestMatch is not null)
            {
                var fallbackValue = bestMatch.Value;
                var fallbackKeyword = NormalizeSearchKeyword(fallbackValue);
                if (!string.Equals(fallbackKeyword, normalizedSearchKeyword, StringComparison.OrdinalIgnoreCase))
                {
                    var fallbackModel = await GetHotelListingPageForSqlServerAsync(fallbackValue, campaignTag, cancellationToken, false);
                    fallbackModel.SearchTerm = normalizedSearchTerm;
                    fallbackModel.SearchLabel = $"{normalizedSearchTerm} için {bestMatch.Label}";
                    fallbackModel.City = fallbackModel.SearchLabel;
                    return fallbackModel;
                }
            }
        }

        return model;
    }

    private async Task<List<HotelSearchSuggestionViewModel>> GetSearchSuggestionsForSqlServerAsync(string query, CancellationToken cancellationToken)
    {
        var normalizedQuery = string.IsNullOrWhiteSpace(query) ? string.Empty : query.Trim();
        var normalizedQueryKeyword = NormalizeSearchKeyword(normalizedQuery);
        var result = new List<HotelSearchSuggestionViewModel>();
        if (normalizedQuery.Length < 2)
        {
            return result;
        }

        var connectionString = GetConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return result;
        }

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        var normalizedCitySql = BuildSearchNormalizationSql("o.sehir");
        var normalizedDistrictSql = BuildSearchNormalizationSql("o.ilce");
        var normalizedNeighborhoodSql = BuildSearchNormalizationSql("o.mahalle");
        var normalizedHotelNameSql = BuildSearchNormalizationSql("o.otel_adi");

        var sql = $"""
            SELECT TOP (8) suggestion_value, suggestion_label, suggestion_type
            FROM (
                SELECT DISTINCT o.sehir AS suggestion_value, o.sehir AS suggestion_label, 'Sehir' AS suggestion_type, 1 AS sort_order
                FROM oteller o
                WHERE o.yayin_durumu = N'Yayında'
                  AND o.onay_durumu = N'Onaylandı'
                  AND {normalizedCitySql} LIKE @queryNormalized + '%'

                UNION

                SELECT DISTINCT o.ilce AS suggestion_value, CONCAT(o.ilce, ' / ', o.sehir) AS suggestion_label, 'Ilce' AS suggestion_type, 2 AS sort_order
                FROM oteller o
                WHERE o.yayin_durumu = N'Yayında'
                  AND o.onay_durumu = N'Onaylandı'
                  AND {normalizedDistrictSql} LIKE @queryNormalized + '%'

                UNION

                SELECT DISTINCT o.mahalle AS suggestion_value, CONCAT(o.mahalle, ' / ', o.ilce, ' / ', o.sehir) AS suggestion_label, 'Mahalle' AS suggestion_type, 3 AS sort_order
                FROM oteller o
                WHERE o.yayin_durumu = N'Yayında'
                  AND o.onay_durumu = N'Onaylandı'
                  AND o.mahalle IS NOT NULL
                  AND o.mahalle <> ''
                  AND {normalizedNeighborhoodSql} LIKE @queryNormalized + '%'

                UNION

                SELECT DISTINCT o.otel_adi AS suggestion_value, CONCAT(o.otel_adi, ' / ', o.ilce, ' / ', o.sehir) AS suggestion_label, 'Otel' AS suggestion_type, 4 AS sort_order
                FROM oteller o
                WHERE o.yayin_durumu = N'Yayında'
                  AND o.onay_durumu = N'Onaylandı'
                  AND {normalizedHotelNameSql} LIKE '%' + @queryNormalized + '%'
            ) suggestions
            ORDER BY sort_order, suggestion_label;
            """;

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@queryNormalized", normalizedQueryKeyword);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(new HotelSearchSuggestionViewModel
            {
                Value = reader.GetString(0),
                Label = reader.GetString(1),
                Type = reader.GetString(2)
            });
        }

        await reader.DisposeAsync();

        if (result.Count < 8 && !string.IsNullOrWhiteSpace(normalizedQueryKeyword))
        {
            var fuzzyMatches = await LoadFuzzySearchCandidatesAsync(connection, normalizedQueryKeyword, cancellationToken);
            foreach (var fuzzyMatch in fuzzyMatches
                         .OrderByDescending(x => x.Score)
                         .ThenBy(x => x.Item.Type)
                         .ThenBy(x => x.Item.Label, StringComparer.OrdinalIgnoreCase))
            {
                if (result.Any(existing =>
                        string.Equals(existing.Type, fuzzyMatch.Item.Type, StringComparison.OrdinalIgnoreCase)
                        && string.Equals(existing.Value, fuzzyMatch.Item.Value, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                result.Add(fuzzyMatch.Item);
                if (result.Count >= 8)
                {
                    break;
                }
            }
        }

        return result;
    }

    private async Task<HotelDetailPageViewModel?> GetHotelDetailPageForSqlServerAsync(string slug, CancellationToken cancellationToken)
    {
        var connectionString = GetConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return null;
        }

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        var hotelIdentity = await ResolveHotelIdentityBySlugSqlServerAsync(connection, slug, cancellationToken);
        if (hotelIdentity is null)
        {
            return null;
        }

        const string detailSql = """
            SELECT TOP (1)
                o.id,
                o.otel_kodu,
                o.otel_adi,
                o.yildiz_sayisi,
                o.sehir,
                o.ilce,
                o.tam_adres,
                COALESCE(o.kisa_aciklama, '') AS kisa_aciklama,
                COALESCE(o.uzun_aciklama, '') AS uzun_aciklama,
                COALESCE(o.konum_aciklamasi, '') AS konum_aciklamasi,
                COALESCE(o.ortalama_puan, 0) AS ortalama_puan,
                COALESCE(o.toplam_yorum_sayisi, 0) AS toplam_yorum_sayisi,
                o.check_in_saati,
                o.check_out_saati,
                o.enlem,
                o.boylam,
                COALESCE(NULLIF(o.kapak_fotografi, ''), '') AS gorsel_url
            FROM oteller o
            WHERE o.id = @hotelId;
            """;

        var model = new HotelDetailPageViewModel();
        await using (var detailCommand = new SqlCommand(detailSql, connection))
        {
            detailCommand.Parameters.AddWithValue("@hotelId", hotelIdentity.Value.Id);
            await using var reader = await detailCommand.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                return null;
            }

            model = new HotelDetailPageViewModel
            {
                Id = reader.GetInt64(reader.GetOrdinal("id")),
                Slug = hotelIdentity.Value.Slug,
                HotelCode = reader.GetString(reader.GetOrdinal("otel_kodu")),
                Name = reader.GetString(reader.GetOrdinal("otel_adi")),
                City = reader.GetString(reader.GetOrdinal("sehir")),
                District = reader.GetString(reader.GetOrdinal("ilce")),
                Address = reader.IsDBNull(reader.GetOrdinal("tam_adres")) ? string.Empty : reader.GetString(reader.GetOrdinal("tam_adres")),
                ShortDescription = reader.GetString(reader.GetOrdinal("kisa_aciklama")),
                LongDescription = reader.GetString(reader.GetOrdinal("uzun_aciklama")),
                LocationDescription = reader.GetString(reader.GetOrdinal("konum_aciklamasi")),
                StarCount = reader.IsDBNull(reader.GetOrdinal("yildiz_sayisi")) ? (byte?)null : Convert.ToByte(reader.GetValue(reader.GetOrdinal("yildiz_sayisi")), CultureInfo.InvariantCulture),
                Rating = reader.GetDecimal(reader.GetOrdinal("ortalama_puan")),
                RatingText = BuildRatingText(reader.GetDecimal(reader.GetOrdinal("ortalama_puan"))),
                ReviewCount = ReadInt(reader, "toplam_yorum_sayisi"),
                CheckInTime = reader.IsDBNull(reader.GetOrdinal("check_in_saati")) ? null : reader.GetTimeSpan(reader.GetOrdinal("check_in_saati")),
                CheckOutTime = reader.IsDBNull(reader.GetOrdinal("check_out_saati")) ? null : reader.GetTimeSpan(reader.GetOrdinal("check_out_saati")),
                Latitude = reader.IsDBNull(reader.GetOrdinal("enlem")) ? null : reader.GetDecimal(reader.GetOrdinal("enlem")),
                Longitude = reader.IsDBNull(reader.GetOrdinal("boylam")) ? null : reader.GetDecimal(reader.GetOrdinal("boylam")),
                MainImageUrl = NormalizeImageUrl(reader.GetString(reader.GetOrdinal("gorsel_url")))
            };
        }

        var today = DateOnly.FromDateTime(DateTime.Today);
        var detailPrice = await _hotelPricingReadService.GetHotelEffectivePriceAsync(model.Id, today, today, cancellationToken);
        model.LowestRoomPrice = detailPrice.GetValueOrDefault(0m);

        const string gallerySql = """
            SELECT gorsel_url
            FROM otel_gorselleri
            WHERE otel_id = @hotelId
              AND onay_durumu LIKE N'Onaylan%'
              AND gorsel_url NOT LIKE '/uploads/logo/%'
            ORDER BY kapak_fotografi_mi DESC, one_cikan DESC, siralama ASC, id ASC;
            """;
        await using (var galleryCommand = new SqlCommand(gallerySql, connection))
        {
            galleryCommand.Parameters.AddWithValue("@hotelId", model.Id);
            await using var galleryReader = await galleryCommand.ExecuteReaderAsync(cancellationToken);
            while (await galleryReader.ReadAsync(cancellationToken))
            {
                model.GalleryImages.Add(NormalizeImageUrl(galleryReader.GetString(0)));
            }
        }

        if (model.GalleryImages.Count == 0 && !string.IsNullOrWhiteSpace(model.MainImageUrl))
        {
            model.GalleryImages.Add(model.MainImageUrl);
        }
        if (string.IsNullOrWhiteSpace(model.MainImageUrl) && model.GalleryImages.Count > 0)
        {
            model.MainImageUrl = model.GalleryImages[0];
        }

        const string amenitiesSql = """
            SELECT TOP (6) oo.ozellik_adi, COALESCE(oo.ozellik_ikon, 'fa-circle-check') AS ozellik_ikon
            FROM otel_ozellik_iliskileri oi
            JOIN otel_ozellikleri oo ON oo.id = oi.ozellik_id AND oo.aktif_mi = 1
            WHERE oi.otel_id = @hotelId
            ORDER BY oo.one_cikan_ozellik DESC, oo.siralama ASC, oo.id ASC;
            """;
        await using (var amenitiesCommand = new SqlCommand(amenitiesSql, connection))
        {
            amenitiesCommand.Parameters.AddWithValue("@hotelId", model.Id);
            await using var amenitiesReader = await amenitiesCommand.ExecuteReaderAsync(cancellationToken);
            while (await amenitiesReader.ReadAsync(cancellationToken))
            {
                var amenityName = amenitiesReader.GetString(0);
                var amenityIcon = amenitiesReader.GetString(1);
                model.Amenities.Add(new HotelAmenityViewModel
                {
                    Name = NormalizeAmenityLabel(amenityName),
                    IconClass = NormalizeAmenityIcon(amenityIcon, amenityName)
                });
            }
        }

        const string roomsSql = """
            SELECT
                ot.id,
                ot.oda_adi,
                ot.maksimum_kisi_sayisi,
                ot.maksimum_yetiskin_sayisi,
                COALESCE(ot.maksimum_cocuk_sayisi, 0),
                ot.yatak_tipi,
                ot.oda_metrekare,
                COALESCE(ot.standart_gecelik_fiyat, 0) AS standart_gecelik_fiyat,
                COALESCE(ot.kapak_fotografi, '') AS kapak_fotografi,
                COALESCE(ot.galeri, '') AS galeri
            FROM oda_tipleri ot
            WHERE ot.otel_id = @hotelId
              AND ot.aktif_mi = 1
            ORDER BY ot.siralama ASC, ot.id ASC;
            """;
        await using (var roomsCommand = new SqlCommand(roomsSql, connection))
        {
            roomsCommand.Parameters.AddWithValue("@hotelId", model.Id);
            await using var roomsReader = await roomsCommand.ExecuteReaderAsync(cancellationToken);
            while (await roomsReader.ReadAsync(cancellationToken))
            {
                var roomId = roomsReader.GetInt64(0);
                var roomName = roomsReader.GetString(1);
                var maxGuests = Convert.ToByte(roomsReader.GetValue(2), CultureInfo.InvariantCulture);
                var maxAdults = Convert.ToByte(roomsReader.GetValue(3), CultureInfo.InvariantCulture);
                var maxChildren = Convert.ToByte(roomsReader.GetValue(4), CultureInfo.InvariantCulture);
                var bedType = roomsReader.IsDBNull(5) ? "Yatak bilgisi yok" : roomsReader.GetString(5);
                ushort? squareMeter = roomsReader.IsDBNull(6) ? null : Convert.ToUInt16(roomsReader.GetValue(6), CultureInfo.InvariantCulture);
                var roomPrice = roomsReader.GetDecimal(7);
                var coverPhoto = NormalizeImageUrl(roomsReader.IsDBNull(8) ? string.Empty : roomsReader.GetString(8));
                var galleryJson = roomsReader.IsDBNull(9) ? string.Empty : roomsReader.GetString(9);
                var roomGalleryImages = ParseImageList(galleryJson);
                if (!string.IsNullOrWhiteSpace(coverPhoto))
                {
                    roomGalleryImages.Insert(0, coverPhoto);
                    roomGalleryImages = roomGalleryImages
                        .Where(static item => !string.IsNullOrWhiteSpace(item))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList();
                }

                model.Rooms.Add(new HotelRoomViewModel
                {
                    RoomTypeId = roomId,
                    Name = roomName,
                    Specs = $"{bedType} · {(squareMeter.HasValue ? $"{squareMeter.Value} m2" : "Metrekare bilgisi bekleniyor")} · Max {maxGuests} Kisi",
                    Price = roomPrice,
                    MaxGuestCount = maxGuests,
                    MaxAdultCount = maxAdults,
                    MaxChildCount = maxChildren,
                    ImageUrl = roomGalleryImages.FirstOrDefault(),
                    GalleryImages = roomGalleryImages,
                    Features = new List<HotelRoomFeatureViewModel>(),
                    CancellationText = "Ucretsiz iptal"
                });
            }
        }

        if (model.Rooms.Count > 0)
        {
            const string roomGallerySql = """
                SELECT oda_tip_id, gorsel_url
                FROM oda_gorselleri
                WHERE oda_tip_id IN (SELECT id FROM oda_tipleri WHERE otel_id = @hotelId AND aktif_mi = 1)
                  AND onay_durumu LIKE N'Onaylan%'
                ORDER BY oda_tip_id ASC, kapak_fotografi_mi DESC, siralama ASC, id ASC;
                """;
            var roomGalleryMap = new Dictionary<long, List<string>>();
            await using (var roomGalleryCommand = new SqlCommand(roomGallerySql, connection))
            {
                roomGalleryCommand.Parameters.AddWithValue("@hotelId", model.Id);
                await using var roomGalleryReader = await roomGalleryCommand.ExecuteReaderAsync(cancellationToken);
                while (await roomGalleryReader.ReadAsync(cancellationToken))
                {
                    var roomId = roomGalleryReader.GetInt64(0);
                    var imageUrl = NormalizeImageUrl(roomGalleryReader.IsDBNull(1) ? string.Empty : roomGalleryReader.GetString(1));
                    if (string.IsNullOrWhiteSpace(imageUrl))
                    {
                        continue;
                    }

                    if (!roomGalleryMap.TryGetValue(roomId, out var imageList))
                    {
                        imageList = new List<string>();
                        roomGalleryMap[roomId] = imageList;
                    }

                    if (!imageList.Contains(imageUrl, StringComparer.OrdinalIgnoreCase))
                    {
                        imageList.Add(imageUrl);
                    }
                }
            }

            foreach (var room in model.Rooms)
            {
                if (roomGalleryMap.TryGetValue(room.RoomTypeId, out var galleryImages))
                {
                    room.GalleryImages = room.GalleryImages
                        .Concat(galleryImages)
                        .Where(static item => !string.IsNullOrWhiteSpace(item))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList();
                }

                if (string.IsNullOrWhiteSpace(room.ImageUrl))
                {
                    room.ImageUrl = room.GalleryImages.FirstOrDefault();
                }
            }
        }

        if (model.Rooms.Count > 0)
        {
            var roomPriceMap = await _hotelPricingReadService.GetRoomAverageNightlyPriceMapAsync(
                model.Rooms.Select(static item => item.RoomTypeId).Where(static id => id > 0).ToList(),
                today,
                today.AddDays(1),
                cancellationToken);

            foreach (var room in model.Rooms)
            {
                if (roomPriceMap.TryGetValue(room.RoomTypeId, out var effectivePrice) && effectivePrice > 0m)
                {
                    room.Price = effectivePrice;
                }
            }

            var lowestRoomPrice = model.Rooms.Where(static item => item.Price > 0m).Select(static item => item.Price).DefaultIfEmpty(model.LowestRoomPrice).Min();
            if (lowestRoomPrice > 0m)
            {
                model.LowestRoomPrice = lowestRoomPrice;
            }
        }

        const string reviewsSql = """
            SELECT TOP (6)
                CASE WHEN y.anonim_mi = 1 THEN 'Misafir' ELSE u.ad_soyad END AS ad_soyad,
                y.genel_puan,
                y.yorum_metni,
                y.olusturulma_tarihi
            FROM yorumlar y
            LEFT JOIN users u ON u.id = y.kullanici_id
            WHERE y.otel_id = @hotelId
              AND y.onay_durumu = N'Onaylandı'
            ORDER BY y.olusturulma_tarihi DESC;
            """;
        await using (var reviewsCommand = new SqlCommand(reviewsSql, connection))
        {
            reviewsCommand.Parameters.AddWithValue("@hotelId", model.Id);
            await using var reviewsReader = await reviewsCommand.ExecuteReaderAsync(cancellationToken);
            while (await reviewsReader.ReadAsync(cancellationToken))
            {
                var reviewName = reviewsReader.IsDBNull(0) ? "Misafir" : reviewsReader.GetString(0);
                var reviewScore = Convert.ToDecimal(reviewsReader.GetValue(1), CultureInfo.InvariantCulture) * 2m;
                var reviewDate = reviewsReader.GetDateTime(3);
                model.Reviews.Add(new HotelReviewViewModel
                {
                    Avatar = BuildAvatar(reviewName),
                    Name = reviewName,
                    DateText = reviewDate.ToString("dd MMMM yyyy", new CultureInfo("tr-TR")),
                    Score = reviewScore,
                    Text = reviewsReader.IsDBNull(2) ? string.Empty : reviewsReader.GetString(2)
                });
            }
        }

        return model;
    }

    private string? GetConnectionString()
    {
        return _configuration.GetConnectionString("DefaultConnection");
    }

    private static bool ShouldUseSqlServer(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return false;
        }

        return connectionString.Contains("Trusted_Connection", StringComparison.OrdinalIgnoreCase)
               || connectionString.Contains("TrustServerCertificate", StringComparison.OrdinalIgnoreCase)
               || connectionString.Contains("(localdb)", StringComparison.OrdinalIgnoreCase)
               || connectionString.Contains("Initial Catalog", StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeImageUrl(string imageUrl)
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

    private static List<string> ParseImageList(string? galleryJson)
    {
        if (string.IsNullOrWhiteSpace(galleryJson))
        {
            return new List<string>();
        }

        try
        {
            var images = JsonSerializer.Deserialize<List<string>>(galleryJson);
            return images?
                .Select(NormalizeImageUrl)
                .Where(static item => !string.IsNullOrWhiteSpace(item))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList()
                ?? new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }

    private static bool ReadFlag(SqlDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        if (reader.IsDBNull(ordinal))
        {
            return false;
        }

        var rawValue = reader.GetValue(ordinal);
        return rawValue switch
        {
            bool boolValue => boolValue,
            byte byteValue => byteValue != 0,
            short shortValue => shortValue != 0,
            int intValue => intValue != 0,
            long longValue => longValue != 0,
            string stringValue => string.Equals(stringValue, "1", StringComparison.OrdinalIgnoreCase)
                                  || string.Equals(stringValue, "true", StringComparison.OrdinalIgnoreCase)
                                  || string.Equals(stringValue, "evet", StringComparison.OrdinalIgnoreCase),
            _ => Convert.ToInt32(rawValue, CultureInfo.InvariantCulture) != 0
        };
    }

    private static int ReadInt(SqlDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        if (reader.IsDBNull(ordinal))
        {
            return 0;
        }

        return Convert.ToInt32(reader.GetValue(ordinal), CultureInfo.InvariantCulture);
    }

    private static List<string> BuildTags(bool isFeatured, decimal rating, int reviewCount, decimal? startingPrice)
    {
        var tags = new List<string>();

        if (isFeatured)
        {
            tags.Add("One Cikan Otel");
        }

        if (rating >= 4.5m)
        {
            tags.Add("Yuksek Puanli");
        }

        if (reviewCount == 0)
        {
            tags.Add("Yeni Listeleme");
        }

        if (startingPrice.HasValue && startingPrice.Value <= 3500m)
        {
            tags.Add("Uygun Fiyat");
        }

        if (tags.Count == 0)
        {
            tags.Add("Rezervasyona Hazir");
        }

        return tags.Take(3).ToList();
    }

    private static async Task ReopenExpiredPenaltyHotelsAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        const string sql = @"
            UPDATE oteller
            SET yayin_durumu = N'Yayında',
                partner_ceza_bitis_tarihi = NULL
            WHERE partner_ceza_bitis_tarihi IS NOT NULL
              AND partner_ceza_bitis_tarihi <= SYSUTCDATETIME()
              AND yayin_durumu = N'Kapatıldı';";
        try
        {
            await using var command = new SqlCommand(sql, connection);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (SqlException ex) when (IsUnknownColumnError(ex, "partner_ceza_bitis_tarihi"))
        {
            await EnsurePartnerPenaltyColumnAsync(connection, cancellationToken);
            await using var retryCommand = new SqlCommand(sql, connection);
            await retryCommand.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    private static bool IsUnknownColumnError(SqlException ex, string columnName)
        => ex.Number == 207 && ex.Message.Contains(columnName, StringComparison.OrdinalIgnoreCase);

    private static async Task EnsurePartnerPenaltyColumnAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        const string existsSql = @"
            SELECT COUNT(*)
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = 'dbo'
              AND TABLE_NAME = 'oteller'
              AND COLUMN_NAME = 'partner_ceza_bitis_tarihi';";
        await using var existsCommand = new SqlCommand(existsSql, connection);
        var exists = Convert.ToInt32(await existsCommand.ExecuteScalarAsync(cancellationToken) ?? 0) > 0;
        if (!exists)
        {
            const string alterSql = "ALTER TABLE oteller ADD partner_ceza_bitis_tarihi DATETIME2 NULL;";
            await using var alterCommand = new SqlCommand(alterSql, connection);
            await alterCommand.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    private static IEnumerable<HotelListingCardViewModel> ApplyCampaignFilter(IEnumerable<HotelListingCardViewModel> hotels, string activeTag)
    {
        var hotelList = hotels.ToList();

        IEnumerable<HotelListingCardViewModel> Filter(Func<HotelListingCardViewModel, bool> predicate, Func<IEnumerable<HotelListingCardViewModel>, IOrderedEnumerable<HotelListingCardViewModel>>? orderBy = null)
        {
            var filtered = hotelList.Where(predicate);
            if (!filtered.Any())
            {
                filtered = hotelList;
            }

            return orderBy is null ? filtered : orderBy(filtered);
        }

        return activeTag switch
        {
            "havuzlu-oteller" => Filter(
                    x => x.Amenities.Any(a => a.Contains("Havuz", StringComparison.OrdinalIgnoreCase))
                      || x.Tags.Any(t => t.Contains("Havuz", StringComparison.OrdinalIgnoreCase))),
            "evcil-hayvan-dostu" => Filter(
                    x => x.Summary.Contains("evcil", StringComparison.OrdinalIgnoreCase)
                      || x.Tags.Any(t => t.Contains("evcil", StringComparison.OrdinalIgnoreCase))
                      || x.Amenities.Any(a => a.Contains("evcil", StringComparison.OrdinalIgnoreCase))),
            "butik-oteller" => Filter(
                    x => x.PropertyType.Contains("Butik", StringComparison.OrdinalIgnoreCase)
                      || x.Name.Contains("Butik", StringComparison.OrdinalIgnoreCase)),
            "bungalov" => Filter(
                    x => x.PropertyType.Contains("Bungalov", StringComparison.OrdinalIgnoreCase)
                      || x.Name.Contains("Bungalov", StringComparison.OrdinalIgnoreCase)),
            "dag-evi" => Filter(
                    x => x.PropertyType.Contains("Dağ", StringComparison.OrdinalIgnoreCase)
                      || x.PropertyType.Contains("Dag", StringComparison.OrdinalIgnoreCase)
                      || x.Name.Contains("Dağ", StringComparison.OrdinalIgnoreCase)
                      || x.Name.Contains("Dag", StringComparison.OrdinalIgnoreCase)),
            "aile-dostu" => Filter(
                    x => x.Summary.Contains("aile", StringComparison.OrdinalIgnoreCase)
                      || x.Tags.Any(t => t.Contains("aile", StringComparison.OrdinalIgnoreCase))
                      || x.Amenities.Any(a => a.Contains("aile", StringComparison.OrdinalIgnoreCase))),
            "spa-wellness" => Filter(
                    x => x.Amenities.Any(a => a.Contains("Spa", StringComparison.OrdinalIgnoreCase))
                      || x.Amenities.Any(a => a.Contains("Wellness", StringComparison.OrdinalIgnoreCase))
                      || x.Tags.Any(t => t.Contains("Spa", StringComparison.OrdinalIgnoreCase))),
            "en-iyi-fiyat" => Filter(
                    _ => true,
                    items => items.OrderBy(x => x.StartingPrice ?? decimal.MaxValue).ThenByDescending(x => x.Rating))
                .Take(12),
            "kampanyaya-dahil-oteller" => Filter(x => x.IsFeatured || x.Tags.Any(tag => tag.Contains("Puanli", StringComparison.OrdinalIgnoreCase)))
                .Take(12),
            "hafta-sonu-firsatlari" => Filter(x => x.Tags.Any(tag => tag.Contains("Hazir", StringComparison.OrdinalIgnoreCase))
                                                || x.Tags.Any(tag => tag.Contains("Puanli", StringComparison.OrdinalIgnoreCase))
                                                || x.IsFeatured)
                .Take(12),
            "butceme-uygun-oteller" => Filter(
                    x => (x.StartingPrice ?? decimal.MaxValue) <= 4000m,
                    items => items.OrderBy(x => x.StartingPrice ?? decimal.MaxValue))
                .Take(12),
            "ultra-luks" => Filter(
                    x => (x.StartingPrice ?? 0m) >= 5500m || x.Tags.Any(tag => tag.Contains("Puanli", StringComparison.OrdinalIgnoreCase)),
                    items => items.OrderByDescending(x => x.StartingPrice ?? 0m).ThenByDescending(x => x.Rating))
                .Take(12),
            "ay-sonu-ozel" => Filter(x => x.IsFeatured || (x.StartingPrice ?? decimal.MaxValue) <= 4200m)
                .Take(12),
            "flash-indirim" => Filter(
                    x => (x.StartingPrice ?? decimal.MaxValue) <= 3500m,
                    items => items.OrderBy(x => x.StartingPrice ?? decimal.MaxValue))
                .Take(12),
            "erken-rezervasyon" => Filter(
                    x => x.ReviewCount > 0,
                    items => items.OrderByDescending(x => x.Rating).ThenBy(x => x.StartingPrice ?? decimal.MaxValue))
                .Take(12),
            "akilli-fiyat" => Filter(
                    x => x.Tags.Any(tag => tag.Contains("Fiyat", StringComparison.OrdinalIgnoreCase)) || (x.StartingPrice ?? decimal.MaxValue) <= 3600m,
                    items => items.OrderBy(x => x.StartingPrice ?? decimal.MaxValue))
                .Take(12),
            _ => hotelList.Take(12)
        };
    }

    private static string NormalizeCampaignTag(string? campaignTag)
    {
        if (string.IsNullOrWhiteSpace(campaignTag))
        {
            return "havuzlu-oteller";
        }

        return campaignTag.Trim().ToLowerInvariant() switch
        {
            "havuzlu-oteller" => "havuzlu-oteller",
            "evcil-hayvan-dostu" => "evcil-hayvan-dostu",
            "butik-oteller" => "butik-oteller",
            "bungalov" => "bungalov",
            "dag-evi" => "dag-evi",
            "aile-dostu" => "aile-dostu",
            "spa-wellness" => "spa-wellness",
            "ultra-luks" => "ultra-luks",
            "en-iyi-fiyat" => "en-iyi-fiyat",
            "kampanyaya-dahil-oteller" => "kampanyaya-dahil-oteller",
            "hafta-sonu-firsatlari" => "hafta-sonu-firsatlari",
            "butceme-uygun-oteller" => "butceme-uygun-oteller",
            "ay-sonu-ozel" => "ay-sonu-ozel",
            "flash-indirim" => "flash-indirim",
            "erken-rezervasyon" => "erken-rezervasyon",
            "akilli-fiyat" => "akilli-fiyat",
            _ => "havuzlu-oteller"
        };
    }

    private static (string Title, string Description) GetCampaignMeta(string activeTag)
    {
        return activeTag switch
        {
            "havuzlu-oteller" => ("Havuzlu Oteller", "Havuz keyfini öne çıkaran tesisleri filtrelenmiş şekilde keşfedin."),
            "evcil-hayvan-dostu" => ("Evcil Hayvan Dostu", "Patili dostlarınızla rahat konaklama sunan seçenekler burada."),
            "butik-oteller" => ("Butik Oteller", "Kompakt, karakterli ve deneyim odaklı butik tesis seçkisi."),
            "bungalov" => ("Bungalov Oteller", "Doğaya yakın, sakin ve özel alan sunan bungalov konaklamalar."),
            "dag-evi" => ("Dağ Evi Otelleri", "Yüksek lokasyonlarda manzara ve huzur odaklı dağ evi seçenekleri."),
            "aile-dostu" => ("Aile Dostu Oteller", "Çocuklu ailelerin ihtiyaçlarına uygun, dengeli konaklama seçenekleri."),
            "spa-wellness" => ("Spa & Wellness", "Dinlenme ve bakım odaklı spa/wellness deneyimi sunan oteller."),
            "kampanyaya-dahil-oteller" => ("Kampanyaya Dahil Oteller", "Yayınlanan kampanya ve görünürlük avantajı taşıyan otelleri tek ekranda görün."),
            "hafta-sonu-firsatlari" => ("Hafta Sonu Fırsatları", "Kısa kaçamaklar için rezervasyona hazır, kullanıcıların en çok baktığı hafta sonu otelleri."),
            "butceme-uygun-oteller" => ("Bütçeme Uygun Oteller", "Daha erişilebilir fiyat bandındaki otelleri hızlıca filtreleyin ve karşılaştırın."),
            "ultra-luks" => ("Ultra Lüks Seçkisi", "Yüksek segment, premium deneyim ve güçlü puan kombinasyonu sunan seçili tesisler."),
            "ay-sonu-ozel" => ("Ay Sonu Özel", "Ay sonu kampanyaları ve hızlı dönüş sağlayan fiyat avantajları burada toplanır."),
            "flash-indirim" => ("Flash İndirim", "Anlık indirimli, fiyat avantajı güçlü ve hızlı rezervasyona uygun oteller."),
            "erken-rezervasyon" => ("Erken Rezervasyon", "Ön planlama yapan kullanıcılar için güçlü fiyat dengesi sunan oteller."),
            "akilli-fiyat" => ("Akıllı Fiyat", "Fiyat-performans oranı güçlü otelleri algoritmik sıralama ile gösteriyoruz."),
            _ => ("Otel Kategorileri", "Kurumsal ve stilistik kart yapısıyla kategori bazlı otel keşfi yapın.")
        };
    }

    private static List<HotelListingQuickLinkViewModel> BuildListingQuickLinks(string city, string activeTag)
    {
        var baseUrl = string.IsNullOrWhiteSpace(city) || string.Equals(city, "Tüm bölgeler", StringComparison.OrdinalIgnoreCase)
            ? "/oteller"
            : $"/oteller?q={Uri.EscapeDataString(city)}";
        var tagSeparator = baseUrl.Contains('?', StringComparison.Ordinal) ? "&" : "?";

        return new List<HotelListingQuickLinkViewModel>
        {
            new()
            {
                Title = "Havuzlu Oteller",
                Subtitle = "Havuz keyfini yaşayın",
                IconClass = "fa-water-ladder",
                Url = $"{baseUrl}{tagSeparator}etiket=havuzlu-oteller",
                IsActive = activeTag == "havuzlu-oteller"
            },
            new()
            {
                Title = "Evcil Hayvan Dostu",
                Subtitle = "Patili misafirlere uygun",
                IconClass = "fa-paw",
                Url = $"{baseUrl}{tagSeparator}etiket=evcil-hayvan-dostu",
                IsActive = activeTag == "evcil-hayvan-dostu"
            },
            new()
            {
                Title = "Butik",
                Subtitle = "Özel ve karakterli konaklama",
                IconClass = "fa-hotel",
                Url = $"{baseUrl}{tagSeparator}etiket=butik-oteller",
                IsActive = activeTag == "butik-oteller"
            },
            new()
            {
                Title = "Bungalov",
                Subtitle = "Doğayla iç içe deneyim",
                IconClass = "fa-tree",
                Url = $"{baseUrl}{tagSeparator}etiket=bungalov",
                IsActive = activeTag == "bungalov"
            },
            new()
            {
                Title = "Dağ Evi",
                Subtitle = "Manzara ve huzur odaklı",
                IconClass = "fa-mountain",
                Url = $"{baseUrl}{tagSeparator}etiket=dag-evi",
                IsActive = activeTag == "dag-evi"
            },
            new()
            {
                Title = "Aile Dostu",
                Subtitle = "Aile konforu öncelikli",
                IconClass = "fa-people-roof",
                Url = $"{baseUrl}{tagSeparator}etiket=aile-dostu",
                IsActive = activeTag == "aile-dostu"
            },
            new()
            {
                Title = "Spa & Wellness",
                Subtitle = "Dinlenme ve yenilenme",
                IconClass = "fa-spa",
                Url = $"{baseUrl}{tagSeparator}etiket=spa-wellness",
                IsActive = activeTag == "spa-wellness"
            },
            new()
            {
                Title = "Ultra Lüks",
                Subtitle = "Premium segment seçkisi",
                IconClass = "fa-gem",
                Url = $"{baseUrl}{tagSeparator}etiket=ultra-luks",
                IsActive = activeTag == "ultra-luks"
            }
        };
    }

    private static List<string> BuildHomepageTags(bool isFeatured, bool isRecommended, decimal rating, decimal? startingPrice)
    {
        var tags = new List<string>();

        if (isFeatured)
        {
            tags.Add("One Cikan");
        }

        if (isRecommended)
        {
            tags.Add("Hafta Sonu Secimi");
        }

        if (rating >= 4.5m)
        {
            tags.Add("Yuksek Puanli");
        }

        if (startingPrice.HasValue && startingPrice.Value <= 3500m)
        {
            tags.Add("Akilli Fiyat");
        }

        if (tags.Count == 0)
        {
            tags.Add("Rezervasyona Hazir");
        }

        return tags.Take(2).ToList();
    }

    private static string BuildRatingText(decimal rating)
    {
        if (rating >= 4.5m) return "Muhtesem";
        if (rating >= 4.0m) return "Fevkalade";
        if (rating >= 3.5m) return "Cok Iyi";
        if (rating > 0) return "Iyi";
        return "Yorum Bekleniyor";
    }

    private static string BuildSlug(string name, string hotelCode)
    {
        var seed = string.IsNullOrWhiteSpace(name) ? hotelCode : name;
        var normalized = NormalizeRouteSegment(seed);

        var chars = normalized
            .Select(c => char.IsLetterOrDigit(c) ? c : '-')
            .ToArray();

        var slug = new string(chars);
        while (slug.Contains("--", StringComparison.Ordinal))
        {
            slug = slug.Replace("--", "-", StringComparison.Ordinal);
        }

        return slug.Trim('-');
    }

    private static string NormalizeRouteSegment(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalized = value
            .Trim()
            .Replace("İ", "I", StringComparison.Ordinal)
            .Replace("I", "i", StringComparison.Ordinal)
            .Replace("ı", "i", StringComparison.Ordinal)
            .Replace("Ğ", "G", StringComparison.Ordinal)
            .Replace("ğ", "g", StringComparison.Ordinal)
            .Replace("Ü", "U", StringComparison.Ordinal)
            .Replace("ü", "u", StringComparison.Ordinal)
            .Replace("Ş", "S", StringComparison.Ordinal)
            .Replace("ş", "s", StringComparison.Ordinal)
            .Replace("Ö", "O", StringComparison.Ordinal)
            .Replace("ö", "o", StringComparison.Ordinal)
            .Replace("Ç", "C", StringComparison.Ordinal)
            .Replace("ç", "c", StringComparison.Ordinal)
            .ToLowerInvariant()
            .Normalize(NormalizationForm.FormD);

        var sb = new System.Text.StringBuilder();
        foreach (var ch in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
            {
                sb.Append(ch);
            }
        }

        return sb.ToString().Normalize(NormalizationForm.FormC);
    }

    private static HomeAmenityViewModel ParseAmenity(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return new HomeAmenityViewModel();
        }

        var parts = raw.Split("::", StringSplitOptions.TrimEntries);
        var label = parts[0];
        var icon = parts.Length > 1 ? parts[1] : "fa-circle-check";

        return new HomeAmenityViewModel
        {
            Label = NormalizeAmenityLabel(label),
            IconClass = NormalizeAmenityIcon(icon, label)
        };
    }

    private static string NormalizeAmenityLabel(string label)
    {
        return label switch
        {
            "Ücretsiz WiFi" => "WiFi",
            "Açık Yüzme Havuzu" => "Havuz",
            "Kapalı Yüzme Havuzu" => "Havuz",
            "SPA ve Sağlık Merkezi" => "Spa",
            "Açık Büfe Kahvaltı" => "Kahvalti",
            _ => label
                .Replace("Ü", "U", StringComparison.Ordinal)
                .Replace("ü", "u", StringComparison.Ordinal)
                .Replace("Ş", "S", StringComparison.Ordinal)
                .Replace("ş", "s", StringComparison.Ordinal)
                .Replace("İ", "I", StringComparison.Ordinal)
                .Replace("ı", "i", StringComparison.Ordinal)
                .Replace("Ç", "C", StringComparison.Ordinal)
                .Replace("ç", "c", StringComparison.Ordinal)
                .Replace("Ö", "O", StringComparison.Ordinal)
                .Replace("ö", "o", StringComparison.Ordinal)
                .Replace("Ğ", "G", StringComparison.Ordinal)
                .Replace("ğ", "g", StringComparison.Ordinal)
        };
    }

    private static string NormalizeAmenityIcon(string icon, string label)
    {
        return icon switch
        {
            "fa-swimming-pool" => "fa-water-ladder",
            "fa-hot-tub" => "fa-spa",
            _ => label switch
            {
                "Ücretsiz WiFi" => "fa-wifi",
                "Açık Yüzme Havuzu" => "fa-water-ladder",
                "Kapalı Yüzme Havuzu" => "fa-water-ladder",
                "SPA ve Sağlık Merkezi" => "fa-spa",
                _ => icon
            }
        };
    }

    private static async Task<(long Id, string Slug)?> ResolveHotelIdentityBySlugAsync(SqlConnection connection, string slug, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT id, otel_kodu, otel_adi
            FROM oteller
            WHERE yayin_durumu = N'Yayında'
              AND onay_durumu = N'Onaylandı';
            """;

        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var id = reader.GetInt64(0);
            var hotelCode = reader.GetString(1);
            var hotelName = reader.GetString(2);
            var generatedSlug = BuildSlug(hotelName, hotelCode);
            if (string.Equals(generatedSlug, slug, StringComparison.OrdinalIgnoreCase))
            {
                return (id, generatedSlug);
            }
        }

        return null;
    }

    private static async Task<(long Id, string Slug)?> ResolveHotelIdentityBySlugSqlServerAsync(SqlConnection connection, string slug, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT id, otel_kodu, otel_adi
            FROM oteller
            WHERE yayin_durumu = N'Yayında'
              AND onay_durumu = N'Onaylandı';
            """;

        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var id = reader.GetInt64(0);
            var hotelCode = reader.GetString(1);
            var hotelName = reader.GetString(2);
            var generatedSlug = BuildSlug(hotelName, hotelCode);
            if (string.Equals(generatedSlug, slug, StringComparison.OrdinalIgnoreCase))
            {
                return (id, generatedSlug);
            }
        }

        return null;
    }

    private static string BuildAvatar(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            return "OT";
        }

        var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 1)
        {
            return parts[0].Length >= 2 ? parts[0][..2].ToUpperInvariant() : parts[0].ToUpperInvariant();
        }

        return string.Concat(parts.Take(2).Select(x => char.ToUpperInvariant(x[0])));
    }
}




