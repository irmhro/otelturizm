using Microsoft.Data.SqlClient;
using SqlConnection = Microsoft.Data.SqlClient.SqlConnection;
using SqlCommand = Microsoft.Data.SqlClient.SqlCommand;
using SqlTransaction = Microsoft.Data.SqlClient.SqlTransaction;
using SqlException = Microsoft.Data.SqlClient.SqlException;
using otelturizmnew.Models.Anasayfa;
using otelturizmnew.Models.Oteller;
using otelturizmnew.Pricing;
using otelturizmnew.Services.Abstractions;
using otelturizmnew.Utils;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace otelturizmnew.Services;

public class HotelService : IHotelService
{
    private readonly IConfiguration _configuration;
    private readonly IHotelPricingReadService _hotelPricingReadService;
    private readonly ISmartRouteService _smartRouteService;
    private readonly ICacheSingleFlight _cache;
    private readonly ILogger<HotelService> _logger;

    private const string PublishStatusSql = "LOWER(REPLACE(LTRIM(RTRIM(o.yayin_durumu)), NCHAR(0x0131), N'i')) = N'yayinda'";
    private const string ApprovalStatusSql = "LOWER(REPLACE(LTRIM(RTRIM(o.onay_durumu)), NCHAR(0x0131), N'i')) IN (N'onaylandi', N'onaylanmis', N'onayli')";

    public HotelService(IConfiguration configuration, IHotelPricingReadService hotelPricingReadService, ISmartRouteService smartRouteService, ICacheSingleFlight cache, ILogger<HotelService> logger)
    {
        _configuration = configuration;
        _hotelPricingReadService = hotelPricingReadService;
        _smartRouteService = smartRouteService;
        _cache = cache;
        _logger = logger;
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

        const decimal homepageVatPercent = 10m;
        const decimal homepageAccommodationPercent = 2m;

        var hasDiscountTable = await HotelTableExistsAsync(connection, "fiyat_indirimleri", cancellationToken);
        var discountSelect = hasDiscountTable
            ? """
                COALESCE(fi.indirim_adi, '') AS indirim_adi,
                COALESCE(fi.kisa_aciklama, '') AS indirim_aciklama,
                COALESCE(fi.gorsel_url, '') AS indirim_gorsel_url,
                """
            : """
                '' AS indirim_adi,
                '' AS indirim_aciklama,
                '' AS indirim_gorsel_url,
                """;
        var discountJoin = hasDiscountTable
            ? "LEFT JOIN fiyat_indirimleri fi ON fi.id = pf.indirim_id AND fi.aktif_mi = 1"
            : string.Empty;

        var hotelSql = $"""
            SELECT TOP (30)
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
                pf.indirim_id,
                {discountSelect}
                oz.ozellikler
            FROM oteller o
            LEFT JOIN (
                SELECT
                    hotel_prices.otel_id,
                    hotel_prices.effective_price AS baslangic_fiyat,
                    hotel_prices.base_price AS min_normal_fiyat,
                    hotel_prices.discount_price AS min_indirimli_fiyat,
                    hotel_prices.discount_id AS indirim_id
                FROM (
                    SELECT
                        ot.otel_id,
                        best.effective_price,
                        best.base_price,
                        best.discount_price,
                        best.discount_id,
                        ROW_NUMBER() OVER (
                            PARTITION BY ot.otel_id
                            ORDER BY
                                CASE WHEN best.effective_price IS NULL THEN 1 ELSE 0 END,
                                best.effective_price ASC,
                                ot.id ASC
                        ) AS rn
                    FROM oda_tipleri ot
                    OUTER APPLY (
                        SELECT TOP (1)
                            CASE
                                WHEN ofm.kapali_satis = 1 THEN NULL
                                WHEN (COALESCE(ofm.toplam_oda_sayisi, ot.toplam_oda_sayisi) - COALESCE(ofm.satilan_oda_sayisi, 0) - COALESCE(ofm.bloke_oda_sayisi, 0)) <= 0 THEN NULL
                                WHEN ofm.gecelik_fiyat IS NULL OR ofm.gecelik_fiyat <= 0 THEN NULL
                                WHEN ofm.indirimli_fiyat IS NOT NULL AND ofm.indirimli_fiyat > 0 AND ofm.indirimli_fiyat < ofm.gecelik_fiyat THEN ofm.indirimli_fiyat
                                ELSE ofm.gecelik_fiyat
                            END AS effective_price,
                            CASE
                                WHEN ofm.kapali_satis = 1 THEN NULL
                                WHEN (COALESCE(ofm.toplam_oda_sayisi, ot.toplam_oda_sayisi) - COALESCE(ofm.satilan_oda_sayisi, 0) - COALESCE(ofm.bloke_oda_sayisi, 0)) <= 0 THEN NULL
                                WHEN ofm.gecelik_fiyat IS NULL OR ofm.gecelik_fiyat <= 0 THEN NULL
                                ELSE ofm.gecelik_fiyat
                            END AS base_price,
                            CASE
                                WHEN ofm.kapali_satis = 1 THEN NULL
                                WHEN (COALESCE(ofm.toplam_oda_sayisi, ot.toplam_oda_sayisi) - COALESCE(ofm.satilan_oda_sayisi, 0) - COALESCE(ofm.bloke_oda_sayisi, 0)) <= 0 THEN NULL
                                WHEN ofm.gecelik_fiyat IS NULL OR ofm.gecelik_fiyat <= 0 THEN NULL
                                WHEN ofm.indirimli_fiyat IS NULL OR ofm.indirimli_fiyat <= 0 THEN NULL
                                WHEN ofm.indirimli_fiyat >= ofm.gecelik_fiyat THEN NULL
                                ELSE ofm.indirimli_fiyat
                            END AS discount_price,
                            ofm.kampanya_id AS discount_id
                        FROM oda_fiyat_musaitlik ofm
                        WHERE ofm.oda_tip_id = ot.id
                          AND ofm.otel_id = ot.otel_id
                          AND ofm.tarih = CAST(SYSUTCDATETIME() AS date)
                        ORDER BY
                            CASE
                                WHEN ofm.kapali_satis = 1 THEN 1 ELSE 0
                            END ASC,
                            CASE
                                WHEN (COALESCE(ofm.toplam_oda_sayisi, ot.toplam_oda_sayisi) - COALESCE(ofm.satilan_oda_sayisi, 0) - COALESCE(ofm.bloke_oda_sayisi, 0)) <= 0 THEN 1 ELSE 0
                            END ASC,
                            CASE
                                WHEN ofm.gecelik_fiyat IS NULL OR ofm.gecelik_fiyat <= 0 THEN 1 ELSE 0
                            END ASC,
                            CASE
                                WHEN ofm.indirimli_fiyat IS NOT NULL AND ofm.indirimli_fiyat > 0 AND ofm.indirimli_fiyat < ofm.gecelik_fiyat THEN ofm.indirimli_fiyat
                                ELSE ofm.gecelik_fiyat
                            END ASC,
                            ofm.id ASC
                    ) best
                    WHERE ot.aktif_mi = 1
                ) hotel_prices
                WHERE hotel_prices.rn = 1
            ) pf ON pf.otel_id = o.id
            {discountJoin}
            LEFT JOIN (
                SELECT g1.otel_id, g1.gorsel_url
                FROM (
                    SELECT
                        g.otel_id,
                        g.gorsel_url,
                        ROW_NUMBER() OVER (PARTITION BY g.otel_id ORDER BY g.kapak_fotografi_mi DESC, g.one_cikan DESC, g.siralama ASC) AS rn
                    FROM otel_gorselleri g
                    WHERE g.onay_durumu IN (N'Onaylandı', N'Onaylandi', N'OnaylandÄ±', N'Onaylanmış', N'Onaylanmis', N'Onayli')
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
            WHERE {PublishStatusSql}
              AND {ApprovalStatusSql}
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
            var hotelId = reader.GetInt64(reader.GetOrdinal("id"));
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
            var discountId = reader.IsDBNull(reader.GetOrdinal("indirim_id"))
                ? (long?)null
                : reader.GetInt64(reader.GetOrdinal("indirim_id"));
            var discountName = reader.IsDBNull(reader.GetOrdinal("indirim_adi")) ? string.Empty : reader.GetString(reader.GetOrdinal("indirim_adi"));
            var discountDesc = reader.IsDBNull(reader.GetOrdinal("indirim_aciklama")) ? string.Empty : reader.GetString(reader.GetOrdinal("indirim_aciklama"));
            var discountImageUrl = reader.IsDBNull(reader.GetOrdinal("indirim_gorsel_url")) ? string.Empty : reader.GetString(reader.GetOrdinal("indirim_gorsel_url"));
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

            decimal? guestStartingPrice = startingPrice.HasValue && startingPrice.Value > 0m
                ? decimal.Round(InclusiveNightlyPricing.StoredNetToGuestDisplay(startingPrice.Value, homepageVatPercent, homepageAccommodationPercent), 0)
                : null;
            decimal? guestOriginalPrice = originalPrice.HasValue && originalPrice.Value > 0m
                ? decimal.Round(InclusiveNightlyPricing.StoredNetToGuestDisplay(originalPrice.Value, homepageVatPercent, homepageAccommodationPercent), 0)
                : null;
            decimal? guestDiscountedPrice = discountedPrice.HasValue && discountedPrice.Value > 0m
                ? decimal.Round(InclusiveNightlyPricing.StoredNetToGuestDisplay(discountedPrice.Value, homepageVatPercent, homepageAccommodationPercent), 0)
                : null;

            byte? starCount = null;
            var starOrd = reader.GetOrdinal("yildiz_sayisi");
            if (!reader.IsDBNull(starOrd))
            {
                var rawStar = Convert.ToInt32(reader.GetValue(starOrd), CultureInfo.InvariantCulture);
                if (rawStar is > 0 and <= 7)
                {
                    starCount = (byte)rawStar;
                }
            }

            hotels.Add(new HomeHotelCardViewModel
            {
                Id = hotelId,
                HotelCode = reader.GetString(reader.GetOrdinal("otel_kodu")),
                Name = reader.GetString(reader.GetOrdinal("OTEL_ADI")),
                City = reader.GetString(reader.GetOrdinal("sehir")),
                District = reader.GetString(reader.GetOrdinal("ilce")),
                LocationText = $"{reader.GetString(reader.GetOrdinal("ilce"))}, {reader.GetString(reader.GetOrdinal("sehir"))}",
                Rating = rating,
                RatingText = BuildRatingText(rating),
                ReviewCount = reviewCount,
                StartingPrice = guestStartingPrice,
                OriginalPrice = guestOriginalPrice,
                DiscountedPrice = guestDiscountedPrice,
                DiscountPercent = discountPercent,
                HasDiscount = hasDiscount,
                DiscountId = hasDiscount ? discountId : null,
                DiscountName = hasDiscount && !string.IsNullOrWhiteSpace(discountName) ? discountName : null,
                DiscountShortDescription = hasDiscount && !string.IsNullOrWhiteSpace(discountDesc) ? discountDesc : null,
                DiscountImageUrl = hasDiscount && !string.IsNullOrWhiteSpace(discountImageUrl) ? NormalizeImageUrl(discountImageUrl) : null,
                PriceText = hasDiscount && guestDiscountedPrice.HasValue
                    ? $"TRY {guestDiscountedPrice.Value:N0}"
                    : guestStartingPrice.HasValue ? $"TRY {guestStartingPrice.Value:N0}" : "Teklif Al",
                PriceNote = guestStartingPrice.HasValue ? "Vergiler dahil" : "Musait fiyat bilgisi bulunamadi",
                ImageUrl = NormalizeHotelImageUrl(hotelId, imageUrl),
                DetailSlug = BuildSlug(reader.GetString(reader.GetOrdinal("OTEL_ADI")), reader.GetString(reader.GetOrdinal("otel_kodu"))),
                Amenities = amenities,
                Tags = tags,
                IsSmartPrice = isFeatured || isRecommended || (startingPrice.HasValue && startingPrice.Value <= 3500m),
                IsFeatured = isFeatured,
                IsRecommended = isRecommended,
                StarCount = starCount
            });
        }

        await reader.CloseAsync();

        if (hotels.Count > 0)
        {
            await PopulateHomeHotelGalleriesAsync(connection, hotels, cancellationToken);
        }

        var destinationSql = $"""
            WITH published AS (
                SELECT
                    o.id,
                    LTRIM(RTRIM(o.sehir)) AS sehir,
                    LTRIM(RTRIM(o.ilce)) AS ilce,
                    LTRIM(RTRIM(o.otel_adi)) AS otel_adi,
                    COALESCE(NULLIF(LTRIM(RTRIM(o.kapak_fotografi)), N''), N'') AS image_url,
                    COALESCE(o.olusturulma_tarihi, CAST('19000101' AS datetime2)) AS olusturulma_tarihi
                FROM oteller o
                WHERE {PublishStatusSql}
                  AND {ApprovalStatusSql}
                  AND NULLIF(LTRIM(RTRIM(o.sehir)), N'') IS NOT NULL
            ),
            city_totals AS (
                SELECT sehir, COUNT(*) AS hotel_count
                FROM published
                GROUP BY sehir
            ),
            ranked_cities AS (
                SELECT
                    sehir,
                    hotel_count,
                    ROW_NUMBER() OVER (ORDER BY hotel_count DESC, sehir) AS city_rank
                FROM city_totals
            ),
            top_cities AS (
                SELECT sehir, hotel_count
                FROM ranked_cities
                WHERE city_rank <= 6
            ),
            hotel_rank AS (
                SELECT
                    p.sehir,
                    p.id,
                    p.otel_adi,
                    p.image_url,
                    ROW_NUMBER() OVER (
                        PARTITION BY p.sehir
                        ORDER BY p.olusturulma_tarihi DESC, p.id DESC
                    ) AS rn
                FROM published p
                INNER JOIN top_cities tc ON tc.sehir = p.sehir
            ),
            lead_hotel AS (
                SELECT sehir, id AS lead_hotel_id, otel_adi AS lead_hotel, image_url
                FROM hotel_rank
                WHERE rn = 1
            ),
            recent_hotel_names AS (
                SELECT
                    hr.sehir,
                    STRING_AGG(hr.otel_adi, N'|') WITHIN GROUP (ORDER BY hr.rn) AS hotels_text
                FROM hotel_rank hr
                WHERE hr.rn <= 3
                GROUP BY hr.sehir
            )
            SELECT
                tc.sehir,
                tc.hotel_count,
                COALESCE(rhn.hotels_text, N'') AS recent_hotels,
                lh.lead_hotel_id,
                COALESCE(lh.lead_hotel, N'') AS lead_hotel,
                COALESCE(lh.image_url, N'') AS image_url
            FROM top_cities tc
            LEFT JOIN recent_hotel_names rhn ON rhn.sehir = tc.sehir
            LEFT JOIN lead_hotel lh ON lh.sehir = tc.sehir
            ORDER BY tc.hotel_count DESC, tc.sehir;
            """;

        {
            await using var destinationCommand = new SqlCommand(destinationSql, connection);
            await using var destinationReader = await destinationCommand.ExecuteReaderAsync(cancellationToken);
            while (await destinationReader.ReadAsync(cancellationToken))
            {
                var city = destinationReader.GetString(destinationReader.GetOrdinal("sehir"));
                var hotelCount = Convert.ToInt32(destinationReader.GetValue(destinationReader.GetOrdinal("hotel_count")), CultureInfo.InvariantCulture);
                var recentHotelsRaw = destinationReader.IsDBNull(destinationReader.GetOrdinal("recent_hotels"))
                    ? string.Empty
                    : destinationReader.GetString(destinationReader.GetOrdinal("recent_hotels"));
                var leadHotel = destinationReader.IsDBNull(destinationReader.GetOrdinal("lead_hotel"))
                    ? string.Empty
                    : destinationReader.GetString(destinationReader.GetOrdinal("lead_hotel"));
                var imageUrl = destinationReader.IsDBNull(destinationReader.GetOrdinal("image_url"))
                    ? string.Empty
                    : destinationReader.GetString(destinationReader.GetOrdinal("image_url"));
                var leadHotelIdOrd = destinationReader.GetOrdinal("lead_hotel_id");
                long leadHotelId = 0;
                if (!destinationReader.IsDBNull(leadHotelIdOrd))
                {
                    leadHotelId = destinationReader.GetInt64(leadHotelIdOrd);
                }

                var recentHotelNames = recentHotelsRaw
                    .Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Take(3)
                    .ToList();
                if (recentHotelNames.Count == 0 && !string.IsNullOrWhiteSpace(leadHotel))
                {
                    recentHotelNames.Add(leadHotel.Trim());
                }

                destinations.Add(new HomeDestinationCardViewModel
                {
                    City = city,
                    HotelCount = hotelCount,
                    RecentHotelNames = recentHotelNames,
                    ImageUrl = leadHotelId > 0
                        ? NormalizeHotelImageUrl(leadHotelId, imageUrl)
                        : NormalizeImageUrl(imageUrl),
                    ListingUrl = $"/oteller?q={Uri.EscapeDataString(city)}"
                });
            }
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

        const string campaignSql = """
            SELECT TOP (12)
                k.kampanya_adi,
                k.seo_slug,
                COALESCE(NULLIF(k.listeleme_aciklamasi, ''), NULLIF(k.kisa_aciklama, ''), LEFT(k.kampanya_aciklamasi, 180)) AS slogan,
                COALESCE(NULLIF(k.hero_gorseli, ''), NULLIF(k.kart_gorseli, ''), NULLIF(k.banner_gorseli, ''), NULLIF(k.mobil_gorsel, '')) AS gorsel_url,
                COALESCE(NULLIF(k.promo_badge, ''), NULLIF(k.kampanya_etiketi, ''), N'Aktif Kampanya') AS badge_text
            FROM kampanyalar k
            WHERE k.aktif_mi = 1
              AND k.gorunurluk_durumu = N'Yayında'
              AND COALESCE(NULLIF(k.hero_gorseli, ''), NULLIF(k.kart_gorseli, ''), NULLIF(k.banner_gorseli, ''), NULLIF(k.mobil_gorsel, '')) IS NOT NULL
            ORDER BY k.one_cikan_kampanya DESC, k.aktif_sayfa_vitrini DESC, k.siralama ASC, k.id ASC;
            """;

        {
            await using var campaignCommand = new SqlCommand(campaignSql, connection);
            await using var campaignReader = await campaignCommand.ExecuteReaderAsync(cancellationToken);
            while (await campaignReader.ReadAsync(cancellationToken))
            {
                var campaignSlug = campaignReader.IsDBNull(1) ? string.Empty : campaignReader.GetString(1);
                var imageUrl = campaignReader.IsDBNull(3) ? string.Empty : campaignReader.GetString(3);
                if (string.IsNullOrWhiteSpace(campaignSlug) || string.IsNullOrWhiteSpace(imageUrl))
                {
                    continue;
                }

                model.CampaignSlides.Add(new HomeCampaignSlideViewModel
                {
                    CampaignName = campaignReader.GetString(0),
                    Slogan = campaignReader.IsDBNull(2)
                        ? "Seçili otellerde güncel fiyat ve kampanya avantajı sizi bekliyor."
                        : campaignReader.GetString(2),
                    ImageUrl = NormalizeImageUrl(imageUrl),
                    Slug = campaignSlug,
                    TargetUrl = $"/oteller?kampanya={Uri.EscapeDataString(campaignSlug)}",
                    DetailUrl = $"/kampanyalar/{Uri.EscapeDataString(campaignSlug)}",
                    BadgeText = campaignReader.IsDBNull(4) ? "Aktif Kampanya" : campaignReader.GetString(4)
                });
            }
        }

        model.PopularDestinations = destinations;

        await ApplyAdminHomepageSectionsAsync(connection, model, hotels, homepageVatPercent, homepageAccommodationPercent, hasDiscountTable, cancellationToken);

        return model;
    }

    public static string ResolveListingCampaignTag(string? etiket, string? filter)
    {
        var raw = !string.IsNullOrWhiteSpace(etiket) ? etiket : filter;
        if (string.IsNullOrWhiteSpace(raw))
        {
            return string.Empty;
        }

        return NormalizeCampaignTag(SearchTextNormalizer.Normalize(raw));
    }

    public async Task<HotelListingPageViewModel> GetHotelListingPageAsync(string? searchTerm, string? campaignTag = null, string? campaignSlug = null, int page = 1, string? contextualSearchBoostNormalized = null, decimal? minPrice = null, decimal? maxPrice = null, long? ilceId = null, long? sehirId = null, CancellationToken cancellationToken = default)
    {
        var normalizedSearch = string.IsNullOrWhiteSpace(searchTerm) ? string.Empty : searchTerm.Trim().ToLowerInvariant();
        var normalizedTag = string.IsNullOrWhiteSpace(campaignTag) ? string.Empty : campaignTag.Trim().ToLowerInvariant();
        var normalizedSlug = string.IsNullOrWhiteSpace(campaignSlug) ? string.Empty : campaignSlug.Trim().ToLowerInvariant();
        var safePage = Math.Max(1, page);
        var ctxNorm = string.IsNullOrWhiteSpace(contextualSearchBoostNormalized)
            ? "-"
            : NormalizeSearchKeyword(contextualSearchBoostNormalized.Trim()).ToLowerInvariant();
        var minKey = minPrice.HasValue && minPrice.Value > 0m ? minPrice.Value.ToString("0.##", CultureInfo.InvariantCulture) : "-";
        var maxKey = maxPrice.HasValue && maxPrice.Value > 0m ? maxPrice.Value.ToString("0.##", CultureInfo.InvariantCulture) : "-";
        var ilceKey = ilceId is > 0 ? ilceId.Value.ToString(CultureInfo.InvariantCulture) : "-";
        var sehirKey = sehirId is > 0 ? sehirId.Value.ToString(CultureInfo.InvariantCulture) : "-";

        var cacheKey = $"hotel-listing:v6:{normalizedSearch}:{normalizedTag}:{normalizedSlug}:p{safePage}:ctx:{ctxNorm}:min:{minKey}:max:{maxKey}:ilce:{ilceKey}:sehir:{sehirKey}";
        var cached = await _cache.GetOrCreateAsync(
            cacheKey,
            async ct => await GetHotelListingPageForSqlServerAsync(searchTerm, campaignTag, campaignSlug, safePage, contextualSearchBoostNormalized, minPrice, maxPrice, ct, true, ilceId, sehirId),
            absoluteExpirationRelativeToNow: TimeSpan.FromSeconds(45),
            slidingExpiration: TimeSpan.FromSeconds(15),
            cancellationToken: cancellationToken);

        return CloneHotelListing(cached ?? new HotelListingPageViewModel());
    }

    private static HotelListingPageViewModel CloneHotelListing(HotelListingPageViewModel src)
    {
        return new HotelListingPageViewModel
        {
            City = src.City,
            SearchTerm = src.SearchTerm,
            SearchLabel = src.SearchLabel,
            CampaignSlug = src.CampaignSlug,
            ActiveTag = src.ActiveTag,
            CurrentPage = src.CurrentPage,
            PageSize = src.PageSize,
            TotalPages = src.TotalPages,
            CampaignTitle = src.CampaignTitle,
            CampaignDescription = src.CampaignDescription,
            TotalCount = src.TotalCount,
            MinPrice = src.MinPrice,
            MaxPrice = src.MaxPrice,
            ActiveMinPrice = src.ActiveMinPrice,
            ActiveMaxPrice = src.ActiveMaxPrice,
            Cities = new List<string>(src.Cities),
            Districts = new List<string>(src.Districts),
            Neighborhoods = new List<string>(src.Neighborhoods),
            StarOptions = new List<int>(src.StarOptions),
            PropertyTypes = new List<string>(src.PropertyTypes),
            Campaigns = src.Campaigns.Select(x => new HotelListingCampaignFilterViewModel
            {
                Slug = x.Slug,
                Name = x.Name,
                HotelCount = x.HotelCount,
                IsActive = x.IsActive
            }).ToList(),
            SmartRoutes = src.SmartRoutes.Select(x => new SmartRouteFilterViewModel
            {
                Id = x.Id,
                Slug = x.Slug,
                DisplayName = x.DisplayName,
                Hashtag = x.Hashtag,
                SearchText = x.SearchText,
                ColorClass = x.ColorClass,
                HotelCount = x.HotelCount
            }).ToList(),
            QuickLinks = src.QuickLinks.Select(x => new HotelListingQuickLinkViewModel
            {
                Title = x.Title,
                Subtitle = x.Subtitle,
                IconClass = x.IconClass,
                Url = x.Url,
                IsActive = x.IsActive
            }).ToList(),
            MapHotels = src.MapHotels.Select(h => new HotelListingCardViewModel
            {
                Id = h.Id,
                HotelCode = h.HotelCode,
                Name = h.Name,
                PropertyType = h.PropertyType,
                StarCount = h.StarCount,
                City = h.City,
                District = h.District,
                Neighborhood = h.Neighborhood,
                Latitude = h.Latitude,
                Longitude = h.Longitude,
                Rating = h.Rating,
                RatingText = h.RatingText,
                ReviewCount = h.ReviewCount,
                IsFeatured = h.IsFeatured,
                ImageUrl = h.ImageUrl,
                GalleryImages = new List<string>(h.GalleryImages),
                StartingPrice = h.StartingPrice,
                OriginalPrice = h.OriginalPrice,
                DiscountedPrice = h.DiscountedPrice,
                DiscountPercent = h.DiscountPercent,
                HasDiscount = h.HasDiscount,
                DiscountName = h.DiscountName,
                DiscountShortDescription = h.DiscountShortDescription,
                DiscountImageUrl = h.DiscountImageUrl,
                PriceNote = h.PriceNote,
                Amenities = new List<string>(h.Amenities),
                AmenityItems = h.AmenityItems.Select(a => new HotelAmenityViewModel
                {
                    Name = a.Name,
                    IconClass = a.IconClass
                }).ToList(),
                SmartRouteSlugs = new List<string>(h.SmartRouteSlugs),
                Tags = new List<string>(h.Tags),
                CampaignNames = new List<string>(h.CampaignNames),
                CampaignSlugs = new List<string>(h.CampaignSlugs),
                CampaignBadgeText = h.CampaignBadgeText,
                CampaignInfoText = h.CampaignInfoText,
                Summary = h.Summary,
                Slug = h.Slug,
                IsFavorite = false,
                DiscountId = h.DiscountId,
                HasAvailabilityToday = h.HasAvailabilityToday,
                ListingLeadRoomName = h.ListingLeadRoomName
            }).ToList(),
            Hotels = src.Hotels.Select(h => new HotelListingCardViewModel
            {
                Id = h.Id,
                HotelCode = h.HotelCode,
                Name = h.Name,
                PropertyType = h.PropertyType,
                StarCount = h.StarCount,
                City = h.City,
                District = h.District,
                Neighborhood = h.Neighborhood,
                Latitude = h.Latitude,
                Longitude = h.Longitude,
                Rating = h.Rating,
                RatingText = h.RatingText,
                ReviewCount = h.ReviewCount,
                IsFeatured = h.IsFeatured,
                ImageUrl = h.ImageUrl,
                GalleryImages = new List<string>(h.GalleryImages),
                StartingPrice = h.StartingPrice,
                OriginalPrice = h.OriginalPrice,
                DiscountedPrice = h.DiscountedPrice,
                DiscountPercent = h.DiscountPercent,
                HasDiscount = h.HasDiscount,
                DiscountName = h.DiscountName,
                DiscountShortDescription = h.DiscountShortDescription,
                DiscountImageUrl = h.DiscountImageUrl,
                PriceNote = h.PriceNote,
                Amenities = new List<string>(h.Amenities),
                AmenityItems = h.AmenityItems.Select(a => new HotelAmenityViewModel
                {
                    Name = a.Name,
                    IconClass = a.IconClass
                }).ToList(),
                SmartRouteSlugs = new List<string>(h.SmartRouteSlugs),
                Tags = new List<string>(h.Tags),
                CampaignNames = new List<string>(h.CampaignNames),
                CampaignSlugs = new List<string>(h.CampaignSlugs),
                CampaignBadgeText = h.CampaignBadgeText,
                CampaignInfoText = h.CampaignInfoText,
                Summary = h.Summary,
                Slug = h.Slug,
                // kullanıcıya göre set edilir (controller ApplyFavoriteStatesAsync)
                IsFavorite = false,
                DiscountId = h.DiscountId,
                HasAvailabilityToday = h.HasAvailabilityToday,
                ListingLeadRoomName = h.ListingLeadRoomName
            }).ToList()
        };
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

        var sql = $"""
            SELECT TOP (150) suggestion_value, suggestion_label, suggestion_type, suggestion_hotel_code
            FROM (
                SELECT DISTINCT o.sehir AS suggestion_value, o.sehir AS suggestion_label, 'Sehir' AS suggestion_type, CAST('' AS nvarchar(50)) AS suggestion_hotel_code
                FROM oteller o
                WHERE {PublishStatusSql} AND {ApprovalStatusSql}

                UNION

                SELECT DISTINCT o.ilce AS suggestion_value, CONCAT(o.ilce, ' / ', o.sehir) AS suggestion_label, 'Ilce' AS suggestion_type, CAST('' AS nvarchar(50)) AS suggestion_hotel_code
                FROM oteller o
                WHERE {PublishStatusSql} AND {ApprovalStatusSql}

                UNION

                SELECT DISTINCT o.mahalle AS suggestion_value, CONCAT(o.mahalle, ' / ', o.ilce, ' / ', o.sehir) AS suggestion_label, 'Mahalle' AS suggestion_type, CAST('' AS nvarchar(50)) AS suggestion_hotel_code
                FROM oteller o
                WHERE o.yayin_durumu = N'Yayında'
                  AND o.onay_durumu IN (N'Onaylandı', N'Onaylandi', N'OnaylandÄ±', N'Onaylanmış', N'Onaylanmis', N'Onayli')
                  AND o.mahalle IS NOT NULL
                  AND o.mahalle <> ''

                UNION

                SELECT DISTINCT o.otel_adi AS suggestion_value, CONCAT(o.otel_adi, ' / ', o.ilce, ' / ', o.sehir) AS suggestion_label, 'Otel' AS suggestion_type, COALESCE(o.otel_kodu, '') AS suggestion_hotel_code
                FROM oteller o
                WHERE o.yayin_durumu = N'Yayında' AND o.onay_durumu IN (N'Onaylandı', N'Onaylandi', N'OnaylandÄ±', N'Onaylanmış', N'Onaylanmis', N'Onayli')
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
                Type = reader.GetString(2),
                Slug = string.Equals(reader.GetString(2), "Otel", StringComparison.OrdinalIgnoreCase)
                    ? BuildSlug(reader.GetString(0), reader.IsDBNull(3) ? string.Empty : reader.GetString(3))
                    : string.Empty
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

        // Global normalize:
        // - URL/slug benzeri karakterleri sadeleştir
        // - Unicode diakritikleri (é, ü, ñ, å...) kaldır
        // - Boşlukları tekilleştir
        var seed = NormalizeRouteSegment(value)
            .Replace('-', ' ')
            .Trim();

        // Unicode normalize + diakritik temizleme
        var formD = seed.Normalize(NormalizationForm.FormD);
        Span<char> buffer = stackalloc char[Math.Min(formD.Length, 256)];
        var idx = 0;
        for (var i = 0; i < formD.Length; i++)
        {
            var ch = formD[i];
            var category = CharUnicodeInfo.GetUnicodeCategory(ch);
            if (category == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            // Çok görülen global eşleştirmeler
            // (DB tarafı collate/FTS ile güçlendirilir; burada arama anahtarını tutarlı yapıyoruz.)
            if (ch == 'ß') { AppendChars(buffer, ref idx, "ss"); continue; }
            if (ch == 'Æ' || ch == 'æ') { AppendChars(buffer, ref idx, "ae"); continue; }
            if (ch == 'Œ' || ch == 'œ') { AppendChars(buffer, ref idx, "oe"); continue; }
            if (ch == 'Ø' || ch == 'ø') { AppendChars(buffer, ref idx, "o"); continue; }
            if (ch == 'Ł' || ch == 'ł') { AppendChars(buffer, ref idx, "l"); continue; }
            if (ch == 'Đ' || ch == 'đ') { AppendChars(buffer, ref idx, "d"); continue; }
            if (ch == 'Þ' || ch == 'þ') { AppendChars(buffer, ref idx, "th"); continue; }

            // Diğerleri: harf/rakam/boşluk/temel ayrımlar
            if (char.IsLetterOrDigit(ch))
            {
                if (idx < buffer.Length) buffer[idx++] = char.ToLowerInvariant(ch);
                continue;
            }

            if (char.IsWhiteSpace(ch))
            {
                if (idx < buffer.Length) buffer[idx++] = ' ';
                continue;
            }

            // Noktalama vb. -> boşluk
            if (idx < buffer.Length) buffer[idx++] = ' ';
        }

        var normalized = (idx == 0 ? string.Empty : new string(buffer[..idx]))
            .Normalize(NormalizationForm.FormC)
            .Trim();

        while (normalized.Contains("  ", StringComparison.Ordinal))
        {
            normalized = normalized.Replace("  ", " ", StringComparison.Ordinal);
        }

        return normalized;
    }

    private static void AppendChars(Span<char> buffer, ref int idx, string value)
    {
        for (var i = 0; i < value.Length; i++)
        {
            if (idx >= buffer.Length) return;
            buffer[idx++] = value[i];
        }
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

    private static async Task<bool> ColumnExistsAsync(SqlConnection connection, string tableName, string columnName, CancellationToken cancellationToken)
    {
        await using var command = new SqlCommand("SELECT COL_LENGTH(@tableName, @columnName);", connection);
        command.Parameters.AddWithValue("@tableName", tableName);
        command.Parameters.AddWithValue("@columnName", columnName);
        var scalar = await command.ExecuteScalarAsync(cancellationToken);
        return scalar is not null && scalar != DBNull.Value;
    }

    private static async Task<bool> HasFullTextIndexAsync(SqlConnection connection, string tableName, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT CASE WHEN
                FULLTEXTSERVICEPROPERTY('IsFullTextInstalled') = 1
                AND EXISTS (
                    SELECT 1
                    FROM sys.fulltext_indexes fti
                    INNER JOIN sys.fulltext_index_columns fic ON fic.object_id = fti.object_id
                    WHERE fti.object_id = OBJECT_ID(@tableName)
                )
            THEN 1 ELSE 0 END;
            """;
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@tableName", tableName);
        var scalar = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(scalar ?? 0) == 1;
    }

    private static async Task<bool> HasFullTextIndexedColumnAsync(SqlConnection connection, string tableName, string columnName, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT CASE WHEN
                FULLTEXTSERVICEPROPERTY('IsFullTextInstalled') = 1
                AND EXISTS (
                    SELECT 1
                    FROM sys.fulltext_index_columns fic
                    WHERE fic.object_id = OBJECT_ID(@tableName)
                      AND fic.column_id = COLUMNPROPERTY(OBJECT_ID(@tableName), @columnName, 'ColumnId')
                )
            THEN 1 ELSE 0 END;
            """;

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@tableName", tableName);
        command.Parameters.AddWithValue("@columnName", columnName);
        var scalar = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(scalar ?? 0) == 1;
    }

    private static string BuildFullTextPrefixQuery(string normalizedKeyword)
    {
        // CONTAINS için basit prefix araması: "foo*" AND "bar*"
        // Güvenlik: tırnak karakterlerini temizleyip sadece token bazlı çalış.
        if (string.IsNullOrWhiteSpace(normalizedKeyword))
        {
            return string.Empty;
        }

        var tokens = normalizedKeyword
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(t => t.Replace("\"", string.Empty, StringComparison.Ordinal))
            .Where(t => t.Length >= 2)
            .Take(5)
            .ToList();

        if (tokens.Count == 0)
        {
            return string.Empty;
        }

        return string.Join(" AND ", tokens.Select(t => $"\"{t}*\""));
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

    public async Task<HotelDetailPageViewModel?> GetHotelDetailPageAsync(string slug, HotelDetailLoadOptions? options = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            return null;
        }

        if (options is { HasFilters: true })
        {
            var fresh = await GetHotelDetailPageForSqlServerAsync(slug, options, cancellationToken);
            return fresh is null ? null : CloneHotelDetail(fresh);
        }

        var cacheKey = $"hotel-detail:v1:{slug.Trim().ToLowerInvariant()}";
        var cached = await _cache.GetOrCreateAsync(
            cacheKey,
            async ct => await GetHotelDetailPageForSqlServerAsync(slug, null, ct),
            absoluteExpirationRelativeToNow: TimeSpan.FromMinutes(2),
            slidingExpiration: TimeSpan.FromSeconds(30),
            cancellationToken: cancellationToken);

        return cached is null ? null : CloneHotelDetail(cached);
    }

    private static HotelDetailPageViewModel CloneHotelDetail(HotelDetailPageViewModel src)
    {
        return new HotelDetailPageViewModel
        {
            Id = src.Id,
            Slug = src.Slug,
            HotelCode = src.HotelCode,
            Name = src.Name,
            City = src.City,
            District = src.District,
            Address = src.Address,
            ShortDescription = src.ShortDescription,
            LongDescription = src.LongDescription,
            LocationDescription = src.LocationDescription,
            StarCount = src.StarCount,
            Rating = src.Rating,
            RatingText = src.RatingText,
            ReviewCount = src.ReviewCount,
            ReviewLocationScore = src.ReviewLocationScore,
            ReviewRoomScore = src.ReviewRoomScore,
            ReviewComfortScore = src.ReviewComfortScore,
            ReviewValueScore = src.ReviewValueScore,
            ReviewStaffScore = src.ReviewStaffScore,
            CheckInTime = src.CheckInTime,
            CheckOutTime = src.CheckOutTime,
            Latitude = src.Latitude,
            Longitude = src.Longitude,
            LowestRoomPrice = src.LowestRoomPrice,
            TaxDisplayVatPercent = src.TaxDisplayVatPercent,
            TaxDisplayAccommodationPercent = src.TaxDisplayAccommodationPercent,
            MainImageUrl = src.MainImageUrl,
            IsFavorite = src.IsFavorite,
            IsLoggedInUser = false,
            HasCompletedReservationAtHotel = false,
            ConversationInfoMessage = string.Empty,
            ShouldResumeDraftOnLoad = false,
            ReservationForm = new(),
            ActiveDraft = null,
            ProfilePrompt = new(),
            GalleryImages = new List<string>(src.GalleryImages),
            Amenities = src.Amenities.Select(x => new HotelAmenityViewModel
            {
                Name = x.Name,
                IconClass = x.IconClass
            }).ToList(),
            Rooms = src.Rooms.Select(r => new HotelRoomViewModel
            {
                RoomTypeId = r.RoomTypeId,
                Name = r.Name,
                Specs = r.Specs,
                BedType = r.BedType,
                SquareMeter = r.SquareMeter,
                DetailDescription = r.DetailDescription,
                Price = r.Price,
                BasePrice = r.BasePrice,
                DiscountPrice = r.DiscountPrice,
                DiscountId = r.DiscountId,
                DiscountName = r.DiscountName,
                DiscountShortDescription = r.DiscountShortDescription,
                DiscountImageUrl = r.DiscountImageUrl,
                MaxGuestCount = r.MaxGuestCount,
                MaxAdultCount = r.MaxAdultCount,
                MaxChildCount = r.MaxChildCount,
                ImageUrl = r.ImageUrl,
                GalleryImages = new List<string>(r.GalleryImages),
                Features = r.Features.Select(f => new HotelRoomFeatureViewModel
                {
                    Name = f.Name,
                    IconClass = f.IconClass
                }).ToList(),
                CancellationText = r.CancellationText
            }).ToList(),
            Reviews = src.Reviews.Select(x => new HotelReviewViewModel
            {
                Avatar = x.Avatar,
                Name = x.Name,
                DateText = x.DateText,
                Score = x.Score,
                Text = x.Text,
                TravelProfile = x.TravelProfile,
                SatisfactionLabel = x.SatisfactionLabel,
                ReservationNoTail = x.ReservationNoTail
            }).ToList(),
            SimilarHotels = src.SimilarHotels.Select(x => new HotelSimilarCardViewModel
            {
                Name = x.Name,
                PriceText = x.PriceText,
                RatingText = x.RatingText,
                Slug = x.Slug,
                ImageUrl = x.ImageUrl
            }).ToList(),
            Conditions = src.Conditions is null
                ? null
                : new HotelDetailConditionsViewModel
                {
                    CancellationSummary = src.Conditions.CancellationSummary,
                    CancellationDetail = src.Conditions.CancellationDetail,
                    FreeCancellationHours = src.Conditions.FreeCancellationHours,
                    SmokingPolicy = src.Conditions.SmokingPolicy,
                    PetPolicy = src.Conditions.PetPolicy,
                    ChildPolicy = src.Conditions.ChildPolicy,
                    PrepaymentRequired = src.Conditions.PrepaymentRequired,
                    PrepaymentPercent = src.Conditions.PrepaymentPercent,
                    CardPaymentAccepted = src.Conditions.CardPaymentAccepted
                },
            Weather = src.Weather,
            ActiveViewerBand = src.ActiveViewerBand,
            LivePresenceCount = src.LivePresenceCount,
            IntentSegmentLabel = src.IntentSegmentLabel,
            TourismDocumentNo = src.TourismDocumentNo,
            TourismDocumentType = src.TourismDocumentType
        };
    }

    private async Task<HotelListingPageViewModel> GetHotelListingPageForSqlServerAsync(string? searchTerm, string? campaignTag, string? campaignSlug, int page, string? contextualCityHint, decimal? minPrice, decimal? maxPrice, CancellationToken cancellationToken, bool allowFuzzyFallback = true, long? ilceId = null, long? sehirId = null)
    {
        const decimal listingVatPercent = 10m;
        const decimal listingAccommodationPercent = 2m;

        var normalizedSearchTerm = string.IsNullOrWhiteSpace(searchTerm) ? string.Empty : searchTerm.Trim();
        var normalizedSearchKeyword = NormalizeSearchKeyword(normalizedSearchTerm);
        var contextBoost = string.IsNullOrWhiteSpace(contextualCityHint) ? string.Empty : NormalizeSearchKeyword(contextualCityHint.Trim());
        var normalizedCampaignSlug = NormalizeCampaignSlug(campaignSlug);
        var normalizedTag = NormalizeCampaignTag(campaignTag);
        var displayLabel = string.IsNullOrWhiteSpace(normalizedSearchTerm) ? "Tüm bölgeler" : normalizedSearchTerm;
        var model = new HotelListingPageViewModel
        {
            City = displayLabel,
            SearchTerm = normalizedSearchTerm,
            SearchLabel = displayLabel,
            CampaignSlug = normalizedCampaignSlug,
            ActiveTag = normalizedTag,
            CurrentPage = Math.Max(page, 1),
            PageSize = 15
        };

        var connectionString = GetConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return model;
        }

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        var hasNormalizedColumns = await ColumnExistsAsync(connection, "[dbo].[OTELLER]", "SEHIR_NORMALIZED", cancellationToken);
        var normalizedCitySql = hasNormalizedColumns ? "o.sehir_normalized" : BuildSearchNormalizationSql("o.sehir");
        var normalizedDistrictSql = hasNormalizedColumns ? "o.ilce_normalized" : BuildSearchNormalizationSql("o.ilce");
        var normalizedNeighborhoodSql = hasNormalizedColumns ? "o.mahalle_normalized" : BuildSearchNormalizationSql("COALESCE(o.mahalle, '')");
        var normalizedHotelNameSql = hasNormalizedColumns ? "o.otel_adi_normalized" : BuildSearchNormalizationSql("o.otel_adi");
        var normalizedCompositeSql = hasNormalizedColumns ? "o.konum_normalized" : BuildSearchNormalizationSql("CONCAT(o.mahalle, ' ', o.ilce, ' ', o.sehir)");

        var hasDiscountTable = await HotelTableExistsAsync(connection, "fiyat_indirimleri", cancellationToken);
        var discountSelect = hasDiscountTable
            ? """
                COALESCE(fi.indirim_adi, '') AS indirim_adi,
                COALESCE(fi.kisa_aciklama, '') AS indirim_aciklama,
                COALESCE(fi.gorsel_url, '') AS indirim_gorsel_url,
                """
            : """
                '' AS indirim_adi,
                '' AS indirim_aciklama,
                '' AS indirim_gorsel_url,
                """;
        var discountJoin = hasDiscountTable
            ? "LEFT JOIN fiyat_indirimleri fi ON fi.id = pf.indirim_id AND fi.aktif_mi = 1"
            : string.Empty;

        var hasSubscriptionTable = await HotelTableExistsAsync(connection, "OTEL_LISTE_ABONELIKLERI", cancellationToken);
        var hasScopeNormColumn = hasSubscriptionTable
            && await ColumnExistsAsync(connection, "[dbo].[OTEL_LISTE_ABONELIKLERI]", "KAPSAM_DEGERI_NORMALIZE", cancellationToken);
        var scopeNormSql = hasScopeNormColumn
            ? "a.[KAPSAM_DEGERI_NORMALIZE]"
            : BuildSearchNormalizationSql("a.[KAPSAM_DEGERI]");
        var subscriptionApplySql = hasSubscriptionTable
            ? $"""
                OUTER APPLY (
                    SELECT TOP (1)
                        a.HEDEF_SIRA AS pin_rank
                    FROM [dbo].[OTEL_LISTE_ABONELIKLERI] a
                    WHERE a.OTEL_ID = o.id
                      AND a.DURUM = N'Onaylandı'
                      AND SYSUTCDATETIME() BETWEEN a.BASLANGIC_UTC AND a.BITIS_UTC
                      AND (
                            @searchTermNormalized <> ''
                            AND (
                                ({normalizedCitySql} = @searchTermNormalized AND a.KAPSAM_TIPI = N'IL' AND {scopeNormSql} = @searchTermNormalized)
                                OR ({normalizedDistrictSql} = @searchTermNormalized AND a.KAPSAM_TIPI = N'ILCE' AND {scopeNormSql} = @searchTermNormalized)
                                OR ({normalizedNeighborhoodSql} = @searchTermNormalized AND a.KAPSAM_TIPI = N'MAHALLE' AND {scopeNormSql} = @searchTermNormalized)
                            )
                          )
                    ORDER BY a.HEDEF_SIRA ASC, a.BITIS_UTC DESC, a.ID DESC
                ) subs
                """
            : "OUTER APPLY (SELECT CAST(NULL AS int) AS pin_rank) subs";

        var ftsQuery = BuildFullTextPrefixQuery(normalizedSearchKeyword);
        var hasFts = !string.IsNullOrWhiteSpace(ftsQuery) && await HasFullTextIndexAsync(connection, "[dbo].[OTELLER]", cancellationToken);
        var hasFtsHotelNameIndexed = hasFts && await HasFullTextIndexedColumnAsync(connection, "[dbo].[OTELLER]", "OTEL_ADI", cancellationToken);
        var useFts = hasFtsHotelNameIndexed;
        var hasFtsSearchText = useFts
            && await ColumnExistsAsync(connection, "[dbo].[OTELLER]", "FTS_SEARCH_TEXT", cancellationToken)
            && await HasFullTextIndexedColumnAsync(connection, "[dbo].[OTELLER]", "FTS_SEARCH_TEXT", cancellationToken);

        // SQL Server compile-time doğrulama yapar: CONTAINS / olmayan kolonlar, parametre ile "devre dışı" bırakılsa bile hata üretir.
        // Bu yüzden FTS yoksa CONTAINS parçalarını sorgudan tamamen çıkarıyoruz.
        var ftsWhereSql = useFts
            ? (hasFtsSearchText
                ? "CONTAINS(o.fts_search_text, @ftsQuery)"
                : "CONTAINS(o.otel_adi, @ftsQuery)")
            : string.Empty;
        var searchWhereSql = useFts
            ? $"""
                (
                    @searchTerm = ''
                    OR {normalizedCitySql} = @searchTermNormalized
                    OR {normalizedDistrictSql} = @searchTermNormalized
                    OR {normalizedNeighborhoodSql} = @searchTermNormalized
                    OR ({ftsWhereSql})
                )
                """
            : $"""
                (
                    @searchTerm = ''
                    OR {normalizedCitySql} = @searchTermNormalized
                    OR {normalizedDistrictSql} = @searchTermNormalized
                    OR {normalizedNeighborhoodSql} = @searchTermNormalized
                    OR {normalizedHotelNameSql} LIKE '%' + @searchTermNormalized + '%'
                    OR {normalizedCompositeSql} LIKE '%' + @searchTermNormalized + '%'
                )
                """;

        var locationWhereSql = ilceId is > 0
            ? "o.ilce_id = @ilceId"
            : sehirId is > 0
                ? "o.sehir_id = @sehirId"
                : searchWhereSql;

        var sql = $"""
            SELECT
                o.id,
                o.otel_kodu,
                o.otel_adi,
                o.otel_turu,
                o.yildiz_sayisi,
                o.sehir,
                o.ilce,
                COALESCE(o.mahalle, '') AS mahalle,
                o.enlem,
                o.boylam,
                COALESCE(o.ortalama_puan, 0) AS ortalama_puan,
                COALESCE(o.toplam_yorum_sayisi, 0) AS toplam_yorum_sayisi,
                COALESCE(o.kisa_aciklama, '') AS kisa_aciklama,
                COALESCE(o.one_cikan_otel, 0) AS one_cikan_otel,
                COALESCE(NULLIF(o.kapak_fotografi, ''), NULLIF(og.gorsel_url, '')) AS gorsel_url,
                COALESCE(og3.gorsel_listesi, '') AS gorsel_listesi,
                pf.baslangic_fiyat,
                COALESCE(NULLIF(LTRIM(RTRIM(pf.listing_room_adi)), ''), '') AS listing_room_adi,
                COALESCE(NULLIF(LTRIM(RTRIM(pf.listing_max_room_adi)), ''), '') AS listing_max_room_adi,
                pf.max_baslangic_fiyat,
                pf.min_normal_fiyat,
                pf.min_indirimli_fiyat,
                pf.indirim_id,
                {discountSelect}
                oz.ozellikler,
                COALESCE(kc.kampanya_adlari, '') AS kampanya_adlari,
                COALESCE(kc.kampanya_sluglari, '') AS kampanya_sluglari,
                COALESCE(kc.kampanya_badgetext, '') AS kampanya_badgetext,
                COALESCE(av.has_open_today, 0) AS has_open_today,
                COALESCE(ok.has_free_cancel, 0) AS has_free_cancel
            FROM oteller o
            LEFT JOIN (
                SELECT
                    r_min.otel_id,
                    r_min.effective_price AS baslangic_fiyat,
                    r_min.listing_room_adi,
                    r_min.base_price AS min_normal_fiyat,
                    r_min.discount_price AS min_indirimli_fiyat,
                    r_min.discount_id AS indirim_id,
                    r_max.listing_room_adi AS listing_max_room_adi,
                    r_max.effective_price AS max_baslangic_fiyat
                FROM (
                    SELECT
                        ot.otel_id,
                        ot.oda_adi AS listing_room_adi,
                        best.effective_price,
                        best.base_price,
                        best.discount_price,
                        best.discount_id,
                        ROW_NUMBER() OVER (
                            PARTITION BY ot.otel_id
                            ORDER BY
                                CASE WHEN best.effective_price IS NULL THEN 1 ELSE 0 END,
                                best.effective_price ASC,
                                ot.id ASC
                        ) AS rn_min,
                        ROW_NUMBER() OVER (
                            PARTITION BY ot.otel_id
                            ORDER BY
                                CASE WHEN best.effective_price IS NULL THEN 1 ELSE 0 END,
                                best.effective_price DESC,
                                ot.id ASC
                        ) AS rn_max
                    FROM oda_tipleri ot
                    OUTER APPLY (
                        SELECT TOP (1)
                            CASE
                                WHEN ofm.kapali_satis = 1 THEN NULL
                                WHEN (COALESCE(ofm.toplam_oda_sayisi, ot.toplam_oda_sayisi) - COALESCE(ofm.satilan_oda_sayisi, 0) - COALESCE(ofm.bloke_oda_sayisi, 0)) <= 0 THEN NULL
                                WHEN ofm.gecelik_fiyat IS NULL OR ofm.gecelik_fiyat <= 0 THEN NULL
                                WHEN ofm.indirimli_fiyat IS NOT NULL AND ofm.indirimli_fiyat > 0 AND ofm.indirimli_fiyat < ofm.gecelik_fiyat THEN ofm.indirimli_fiyat
                                ELSE ofm.gecelik_fiyat
                            END AS effective_price,
                            CASE
                                WHEN ofm.kapali_satis = 1 THEN NULL
                                WHEN (COALESCE(ofm.toplam_oda_sayisi, ot.toplam_oda_sayisi) - COALESCE(ofm.satilan_oda_sayisi, 0) - COALESCE(ofm.bloke_oda_sayisi, 0)) <= 0 THEN NULL
                                WHEN ofm.gecelik_fiyat IS NULL OR ofm.gecelik_fiyat <= 0 THEN NULL
                                ELSE ofm.gecelik_fiyat
                            END AS base_price,
                            CASE
                                WHEN ofm.kapali_satis = 1 THEN NULL
                                WHEN (COALESCE(ofm.toplam_oda_sayisi, ot.toplam_oda_sayisi) - COALESCE(ofm.satilan_oda_sayisi, 0) - COALESCE(ofm.bloke_oda_sayisi, 0)) <= 0 THEN NULL
                                WHEN ofm.gecelik_fiyat IS NULL OR ofm.gecelik_fiyat <= 0 THEN NULL
                                WHEN ofm.indirimli_fiyat IS NULL OR ofm.indirimli_fiyat <= 0 THEN NULL
                                WHEN ofm.indirimli_fiyat >= ofm.gecelik_fiyat THEN NULL
                                ELSE ofm.indirimli_fiyat
                            END AS discount_price,
                            ofm.kampanya_id AS discount_id
                        FROM oda_fiyat_musaitlik ofm
                        WHERE ofm.oda_tip_id = ot.id
                          AND ofm.otel_id = ot.otel_id
                          AND ofm.tarih = CAST(SYSUTCDATETIME() AS date)
                        ORDER BY
                            CASE
                                WHEN ofm.kapali_satis = 1 THEN 1 ELSE 0
                            END ASC,
                            CASE
                                WHEN (COALESCE(ofm.toplam_oda_sayisi, ot.toplam_oda_sayisi) - COALESCE(ofm.satilan_oda_sayisi, 0) - COALESCE(ofm.bloke_oda_sayisi, 0)) <= 0 THEN 1 ELSE 0
                            END ASC,
                            CASE
                                WHEN ofm.gecelik_fiyat IS NULL OR ofm.gecelik_fiyat <= 0 THEN 1 ELSE 0
                            END ASC,
                            CASE
                                WHEN ofm.indirimli_fiyat IS NOT NULL AND ofm.indirimli_fiyat > 0 AND ofm.indirimli_fiyat < ofm.gecelik_fiyat THEN ofm.indirimli_fiyat
                                ELSE ofm.gecelik_fiyat
                            END ASC,
                            ofm.id ASC
                    ) best
                    WHERE ot.aktif_mi = 1
                ) r_min
                LEFT JOIN (
                    SELECT
                        ot.otel_id,
                        ot.oda_adi AS listing_room_adi,
                        best.effective_price,
                        ROW_NUMBER() OVER (
                            PARTITION BY ot.otel_id
                            ORDER BY
                                CASE WHEN best.effective_price IS NULL THEN 1 ELSE 0 END,
                                best.effective_price DESC,
                                ot.id ASC
                        ) AS rn_max
                    FROM oda_tipleri ot
                    OUTER APPLY (
                        SELECT TOP (1)
                            CASE
                                WHEN ofm.kapali_satis = 1 THEN NULL
                                WHEN (COALESCE(ofm.toplam_oda_sayisi, ot.toplam_oda_sayisi) - COALESCE(ofm.satilan_oda_sayisi, 0) - COALESCE(ofm.bloke_oda_sayisi, 0)) <= 0 THEN NULL
                                WHEN ofm.gecelik_fiyat IS NULL OR ofm.gecelik_fiyat <= 0 THEN NULL
                                WHEN ofm.indirimli_fiyat IS NOT NULL AND ofm.indirimli_fiyat > 0 AND ofm.indirimli_fiyat < ofm.gecelik_fiyat THEN ofm.indirimli_fiyat
                                ELSE ofm.gecelik_fiyat
                            END AS effective_price
                        FROM oda_fiyat_musaitlik ofm
                        WHERE ofm.oda_tip_id = ot.id
                          AND ofm.otel_id = ot.otel_id
                          AND ofm.tarih = CAST(SYSUTCDATETIME() AS date)
                        ORDER BY
                            CASE WHEN ofm.kapali_satis = 1 THEN 1 ELSE 0 END ASC,
                            CASE WHEN (COALESCE(ofm.toplam_oda_sayisi, ot.toplam_oda_sayisi) - COALESCE(ofm.satilan_oda_sayisi, 0) - COALESCE(ofm.bloke_oda_sayisi, 0)) <= 0 THEN 1 ELSE 0 END ASC,
                            CASE WHEN ofm.gecelik_fiyat IS NULL OR ofm.gecelik_fiyat <= 0 THEN 1 ELSE 0 END ASC,
                            CASE WHEN ofm.indirimli_fiyat IS NOT NULL AND ofm.indirimli_fiyat > 0 AND ofm.indirimli_fiyat < ofm.gecelik_fiyat THEN ofm.indirimli_fiyat ELSE ofm.gecelik_fiyat END DESC,
                            ofm.id ASC
                    ) best
                    WHERE ot.aktif_mi = 1
                ) r_max ON r_max.otel_id = r_min.otel_id AND r_max.rn_max = 1 AND r_max.effective_price IS NOT NULL
                WHERE r_min.rn_min = 1
            ) pf ON pf.otel_id = o.id
            {discountJoin}
            LEFT JOIN (
                SELECT g1.otel_id, g1.gorsel_url
                FROM (
                    SELECT
                        g.otel_id,
                        g.gorsel_url,
                        ROW_NUMBER() OVER (PARTITION BY g.otel_id ORDER BY g.kapak_fotografi_mi DESC, g.one_cikan DESC, g.siralama ASC) AS rn
                    FROM otel_gorselleri g
                    WHERE g.onay_durumu IN (N'Onaylandı', N'Onaylandi', N'OnaylandÄ±', N'Onaylanmış', N'Onaylanmis', N'Onayli')
                      AND g.gorsel_url NOT LIKE '/uploads/logo/%'
                ) g1
                WHERE g1.rn = 1
            ) og ON og.otel_id = o.id
            LEFT JOIN (
                SELECT g.otel_id,
                       STRING_AGG(g.gorsel_url, '||') WITHIN GROUP (ORDER BY g.rn) AS gorsel_listesi
                FROM (
                    SELECT
                        g0.otel_id,
                        g0.gorsel_url,
                        ROW_NUMBER() OVER (PARTITION BY g0.otel_id ORDER BY g0.kapak_fotografi_mi DESC, g0.one_cikan DESC, g0.siralama ASC, g0.id ASC) AS rn
                    FROM otel_gorselleri g0
                    WHERE g0.onay_durumu IN (N'Onaylandı', N'Onaylandi', N'OnaylandÄ±', N'Onaylanmış', N'Onaylanmis', N'Onayli')
                      AND g0.gorsel_url NOT LIKE '/uploads/logo/%'
                ) g
                WHERE g.rn <= 3
                GROUP BY g.otel_id
            ) og3 ON og3.otel_id = o.id
            LEFT JOIN (
                SELECT
                    c.otel_id,
                    STRING_AGG(c.kampanya_adi, '||') WITHIN GROUP (ORDER BY c.kampanya_adi) AS kampanya_adlari,
                    STRING_AGG(c.seo_slug, '||') WITHIN GROUP (ORDER BY c.kampanya_adi) AS kampanya_sluglari,
                    MAX(COALESCE(NULLIF(c.kampanya_etiketi, ''), NULLIF(c.promo_badge, ''), c.kampanya_adi)) AS kampanya_badgetext
                FROM (
                    SELECT DISTINCT
                        ko.otel_id,
                        k.kampanya_adi,
                        k.seo_slug,
                        k.kampanya_etiketi,
                        k.promo_badge
                    FROM kampanya_oteller ko
                    JOIN kampanyalar k ON k.id = ko.kampanya_id
                    WHERE ko.katilim_durumu = N'Aktif'
                      AND k.aktif_mi = 1
                      AND (
                            k.gorunurluk_durumu IS NULL
                            OR LTRIM(RTRIM(k.gorunurluk_durumu)) = N''
                            OR LOWER(REPLACE(LTRIM(RTRIM(k.gorunurluk_durumu)), N'ı', N'i')) IN (N'yayinda', N'yayında')
                          )
                ) c
                GROUP BY c.otel_id
            ) kc ON kc.otel_id = o.id
            LEFT JOIN (
                SELECT
                    oi.otel_id,
                    STRING_AGG(CONCAT(oo.ozellik_adi, '::', COALESCE(oo.ozellik_ikon, 'fa-circle-check')), '||')
                        WITHIN GROUP (ORDER BY oo.one_cikan_ozellik DESC, oo.siralama ASC) AS ozellikler
                FROM otel_ozellik_iliskileri oi
                JOIN otel_ozellikleri oo ON oo.id = oi.ozellik_id AND oo.aktif_mi = 1
                GROUP BY oi.otel_id
            ) oz ON oz.otel_id = o.id
            OUTER APPLY (
                SELECT TOP (1) 1 AS has_open_today
                FROM oda_tipleri ot2
                INNER JOIN oda_fiyat_musaitlik ofm2
                    ON ofm2.oda_tip_id = ot2.id
                   AND ofm2.otel_id = ot2.otel_id
                WHERE ot2.otel_id = o.id
                  AND ot2.aktif_mi = 1
                  AND ofm2.tarih = CAST(SYSUTCDATETIME() AS date)
                  AND ofm2.kapali_satis = 0
                  AND (COALESCE(ofm2.toplam_oda_sayisi, ot2.toplam_oda_sayisi) - COALESCE(ofm2.satilan_oda_sayisi, 0) - COALESCE(ofm2.bloke_oda_sayisi, 0)) > 0
                  AND COALESCE(ofm2.gecelik_fiyat, 0) > 0
            ) av
            LEFT JOIN (
                SELECT
                    k.otel_id,
                    MAX(CASE WHEN COALESCE(k.ucretsiz_iptal_suresi, 0) >= 1 THEN 1 ELSE 0 END) AS has_free_cancel
                FROM otel_kosullari k
                GROUP BY k.otel_id
            ) ok ON ok.otel_id = o.id
            {subscriptionApplySql}
            WHERE {PublishStatusSql}
              AND {ApprovalStatusSql}
              AND (
                    @campaignSlug = ''
                    OR EXISTS (
                        SELECT 1
                        FROM kampanya_oteller ko
                        JOIN kampanyalar k ON k.id = ko.kampanya_id
                        WHERE ko.otel_id = o.id
                          AND ko.katilim_durumu = N'Aktif'
                          AND k.aktif_mi = 1
                          AND (
                                k.gorunurluk_durumu IS NULL
                                OR LTRIM(RTRIM(k.gorunurluk_durumu)) = N''
                                OR LOWER(REPLACE(LTRIM(RTRIM(k.gorunurluk_durumu)), N'ı', N'i')) IN (N'yayinda', N'yayında')
                              )
                          AND k.seo_slug = @campaignSlug
                    )
                  )
              AND {locationWhereSql}
            ORDER BY
                CASE WHEN subs.pin_rank IS NULL THEN 1 ELSE 0 END,
                subs.pin_rank ASC,
                CASE WHEN @contextBoost <> '' AND {normalizedCitySql} = @contextBoost THEN 0 ELSE 1 END,
                CASE
                    WHEN @searchTermNormalized = '' THEN 99
                    WHEN {normalizedHotelNameSql} = @searchTermNormalized THEN 0
                    WHEN {normalizedHotelNameSql} LIKE @searchTermNormalized + '%' THEN 1
                    WHEN {normalizedDistrictSql} = @searchTermNormalized THEN 2
                    WHEN {normalizedNeighborhoodSql} = @searchTermNormalized THEN 3
                    WHEN {normalizedCitySql} = @searchTermNormalized THEN 4
                    WHEN {normalizedDistrictSql} LIKE @searchTermNormalized + '%' THEN 5
                    WHEN {normalizedNeighborhoodSql} LIKE @searchTermNormalized + '%' THEN 6
                    WHEN {normalizedCitySql} LIKE @searchTermNormalized + '%' THEN 7
                    ELSE 8
                END,
                CASE WHEN o.one_cikan_otel = 1 THEN 0 ELSE 1 END,
                CASE WHEN o.ortalama_puan > 0 THEN 0 ELSE 1 END,
                o.ortalama_puan DESC,
                o.toplam_yorum_sayisi DESC,
                o.populerlik_sirasi DESC,
                (
                    (CAST(COALESCE(o.ortalama_puan, 0) AS FLOAT) / 10.0) * 0.45
                    + (LOG(1.0 + CAST(COALESCE(o.toplam_yorum_sayisi, 0) AS FLOAT)) / 12.0) * 0.35
                    + (
                        CASE
                            WHEN pf.baslangic_fiyat IS NOT NULL AND pf.baslangic_fiyat > 0
                                THEN CAST(9000.0 AS FLOAT) / (9000.0 + CAST(pf.baslangic_fiyat AS FLOAT))
                            ELSE 0.0
                        END
                      ) * 0.20
                ) DESC,
                o.id DESC;
            """;

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@searchTerm", normalizedSearchTerm);
        command.Parameters.AddWithValue("@searchTermNormalized", normalizedSearchKeyword);
        command.Parameters.AddWithValue("@contextBoost", contextBoost);
        command.Parameters.AddWithValue("@ftsQuery", useFts ? ftsQuery : (object)DBNull.Value);
        command.Parameters.AddWithValue("@campaignSlug", normalizedCampaignSlug);
        command.Parameters.AddWithValue("@ilceId", ilceId is > 0 ? ilceId.Value : DBNull.Value);
        command.Parameters.AddWithValue("@sehirId", sehirId is > 0 ? sehirId.Value : DBNull.Value);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var id = reader.GetInt64(reader.GetOrdinal("id"));
            var hotelCode = reader.GetString(reader.GetOrdinal("otel_kodu"));
            var name = reader.GetString(reader.GetOrdinal("OTEL_ADI"));
            var hotelCity = reader.GetString(reader.GetOrdinal("sehir"));
            var district = reader.GetString(reader.GetOrdinal("ilce"));
            var neighborhood = reader.GetString(reader.GetOrdinal("mahalle"));
            var hotelType = reader.GetString(reader.GetOrdinal("otel_turu"));
            var lat = reader.IsDBNull(reader.GetOrdinal("enlem")) ? (decimal?)null : reader.GetDecimal(reader.GetOrdinal("enlem"));
            var lon = reader.IsDBNull(reader.GetOrdinal("boylam")) ? (decimal?)null : reader.GetDecimal(reader.GetOrdinal("boylam"));
            var rating = reader.GetDecimal(reader.GetOrdinal("ortalama_puan"));
            var reviewCount = ReadInt(reader, "toplam_yorum_sayisi");
            var summary = reader.GetString(reader.GetOrdinal("kisa_aciklama"));
            var isFeatured = ReadFlag(reader, "one_cikan_otel");
            var imageUrl = reader.IsDBNull(reader.GetOrdinal("gorsel_url")) ? string.Empty : reader.GetString(reader.GetOrdinal("gorsel_url"));
            var galleryRaw = reader.IsDBNull(reader.GetOrdinal("gorsel_listesi")) ? string.Empty : reader.GetString(reader.GetOrdinal("gorsel_listesi"));
            var galleryImages = string.IsNullOrWhiteSpace(galleryRaw)
                ? new List<string>()
                : galleryRaw
                    .Split("||", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(x => NormalizeHotelImageUrl(id, x))
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Take(3)
                    .ToList();
            var storedStartingPrice = reader.IsDBNull(reader.GetOrdinal("baslangic_fiyat")) ? (decimal?)null : reader.GetDecimal(reader.GetOrdinal("baslangic_fiyat"));
            var startingPrice = storedStartingPrice.HasValue
                ? decimal.Round(InclusiveNightlyPricing.StoredNetToGuestDisplay(storedStartingPrice.Value, listingVatPercent, listingAccommodationPercent), 0)
                : (decimal?)null;
            var storedNormalPrice = reader.IsDBNull(reader.GetOrdinal("min_normal_fiyat")) ? (decimal?)null : reader.GetDecimal(reader.GetOrdinal("min_normal_fiyat"));
            var storedDiscountPrice = reader.IsDBNull(reader.GetOrdinal("min_indirimli_fiyat")) ? (decimal?)null : reader.GetDecimal(reader.GetOrdinal("min_indirimli_fiyat"));
            var discountId = reader.IsDBNull(reader.GetOrdinal("indirim_id")) ? (long?)null : reader.GetInt64(reader.GetOrdinal("indirim_id"));
            var discountName = reader.IsDBNull(reader.GetOrdinal("indirim_adi")) ? string.Empty : reader.GetString(reader.GetOrdinal("indirim_adi"));
            var discountDesc = reader.IsDBNull(reader.GetOrdinal("indirim_aciklama")) ? string.Empty : reader.GetString(reader.GetOrdinal("indirim_aciklama"));
            var discountImageUrl = reader.IsDBNull(reader.GetOrdinal("indirim_gorsel_url")) ? string.Empty : reader.GetString(reader.GetOrdinal("indirim_gorsel_url"));
            var rawAmenities = reader.IsDBNull(reader.GetOrdinal("ozellikler")) ? string.Empty : reader.GetString(reader.GetOrdinal("ozellikler"));
            var campaignNames = reader.IsDBNull(reader.GetOrdinal("kampanya_adlari"))
                ? new List<string>()
                : reader.GetString(reader.GetOrdinal("kampanya_adlari"))
                    .Split("||", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
            var campaignSlugs = reader.IsDBNull(reader.GetOrdinal("kampanya_sluglari"))
                ? new List<string>()
                : reader.GetString(reader.GetOrdinal("kampanya_sluglari"))
                    .Split("||", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
            var campaignBadgeText = reader.IsDBNull(reader.GetOrdinal("kampanya_badgetext"))
                ? string.Empty
                : NormalizeTurkishText(reader.GetString(reader.GetOrdinal("kampanya_badgetext")));
            var hasOpenToday = !reader.IsDBNull(reader.GetOrdinal("has_open_today"))
                && Convert.ToInt32(reader.GetValue(reader.GetOrdinal("has_open_today")), CultureInfo.InvariantCulture) == 1;
            var listingRoomAdiOrdinal = reader.GetOrdinal("listing_room_adi");
            var listingRoomAdiRaw = reader.IsDBNull(listingRoomAdiOrdinal) ? string.Empty : reader.GetString(listingRoomAdiOrdinal);
            var listingLeadRoomName = string.IsNullOrWhiteSpace(listingRoomAdiRaw) ? null : listingRoomAdiRaw.Trim();
            var listingMaxRoomAdiOrdinal = reader.GetOrdinal("listing_max_room_adi");
            var listingMaxRoomAdiRaw = reader.IsDBNull(listingMaxRoomAdiOrdinal) ? string.Empty : reader.GetString(listingMaxRoomAdiOrdinal);
            var listingMaxRoomName = string.IsNullOrWhiteSpace(listingMaxRoomAdiRaw) ? null : listingMaxRoomAdiRaw.Trim();
            decimal? listingMaxRoomPrice = null;
            var maxBaslangicOrdinal = reader.GetOrdinal("max_baslangic_fiyat");
            if (!reader.IsDBNull(maxBaslangicOrdinal))
            {
                var storedMaxPrice = reader.GetDecimal(maxBaslangicOrdinal);
                if (storedMaxPrice > 0m)
                {
                    listingMaxRoomPrice = decimal.Round(
                        InclusiveNightlyPricing.StoredNetToGuestDisplay(storedMaxPrice, listingVatPercent, listingAccommodationPercent),
                        0);
                }
            }

            var amenityItems = rawAmenities
                .Split("||", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(ParseAmenity)
                .Where(x => !string.IsNullOrWhiteSpace(x.Label))
                .GroupBy(x => x.Label, StringComparer.OrdinalIgnoreCase)
                .Select(x => x.First())
                .ToList();
            var amenities = amenityItems.Select(x => x.Label).ToList();

            var tags = BuildTags(isFeatured, rating, reviewCount, startingPrice);
            foreach (var campaignName in campaignNames.Take(2))
            {
                if (!tags.Contains(campaignName, StringComparer.OrdinalIgnoreCase))
                {
                    tags.Add(campaignName);
                }
            }

            decimal? originalPrice = storedNormalPrice.HasValue && storedNormalPrice.Value > 0m
                ? decimal.Round(InclusiveNightlyPricing.StoredNetToGuestDisplay(storedNormalPrice.Value, listingVatPercent, listingAccommodationPercent), 0)
                : null;
            decimal? discountedPrice = storedDiscountPrice.HasValue && storedDiscountPrice.Value > 0m
                ? decimal.Round(InclusiveNightlyPricing.StoredNetToGuestDisplay(storedDiscountPrice.Value, listingVatPercent, listingAccommodationPercent), 0)
                : null;
            var hasDiscount = originalPrice.HasValue
                && discountedPrice.HasValue
                && discountedPrice.Value > 0m
                && discountedPrice.Value < originalPrice.Value;
            var discountPercent = hasDiscount
                ? Math.Clamp((int)Math.Round(((originalPrice!.Value - discountedPrice!.Value) / originalPrice.Value) * 100m, MidpointRounding.AwayFromZero), 1, 95)
                : 0;
            var campaignInfoText = BuildCampaignInfoText(campaignNames);

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
                Neighborhood = neighborhood,
                Latitude = lat,
                Longitude = lon,
                Rating = rating,
                RatingText = BuildRatingText(rating),
                ReviewCount = reviewCount,
                StartingPrice = startingPrice,
                OriginalPrice = hasDiscount ? originalPrice : null,
                DiscountedPrice = hasDiscount ? discountedPrice : null,
                DiscountPercent = discountPercent,
                HasDiscount = hasDiscount,
                DiscountId = hasDiscount ? discountId : null,
                DiscountName = hasDiscount && !string.IsNullOrWhiteSpace(discountName) ? discountName : null,
                DiscountShortDescription = hasDiscount && !string.IsNullOrWhiteSpace(discountDesc) ? discountDesc : null,
                DiscountImageUrl = hasDiscount && !string.IsNullOrWhiteSpace(discountImageUrl) ? NormalizeImageUrl(discountImageUrl) : null,
                PriceNote = startingPrice.HasValue ? "Vergiler dahil" : "Musait fiyat bilgisi bulunamadi",
                ImageUrl = NormalizeHotelImageUrl(id, imageUrl),
                GalleryImages = galleryImages,
                IsFeatured = isFeatured,
                Amenities = amenities,
                AmenityItems = amenityItems.Select(x => new HotelAmenityViewModel
                {
                    Name = x.Label,
                    IconClass = x.IconClass
                }).ToList(),
                Tags = tags,
                CampaignNames = campaignNames.Select(NormalizeTurkishText).ToList(),
                CampaignSlugs = campaignSlugs,
                CampaignBadgeText = string.IsNullOrWhiteSpace(campaignBadgeText)
                    ? (campaignNames.Count > 0 ? NormalizeTurkishText(campaignNames[0]) : string.Empty)
                    : campaignBadgeText,
                CampaignInfoText = campaignInfoText,
                Summary = string.IsNullOrWhiteSpace(summary)
                    ? "Sehir konaklamasi, esnek rezervasyon ve mobil uyumlu deneyim icin yayindaki tesis."
                    : summary,
                HasAvailabilityToday = hasOpenToday,
                ListingLeadRoomName = listingLeadRoomName,
                ListingMaxRoomName = listingMaxRoomName,
                ListingMaxRoomPrice = listingMaxRoomPrice,
                HasFreeCancellation = ReadFlag(reader, "has_free_cancel")
            });
        }

        await reader.DisposeAsync();

        await _smartRouteService.EnrichListingCardsAsync(model.Hotels, cancellationToken);

        model.Hotels = ApplyCampaignFilter(model.Hotels, model.ActiveTag).ToList();
        var filteredHotels = model.Hotels.ToList();

        if (filteredHotels.Count == 0 && !string.IsNullOrWhiteSpace(normalizedSearchKeyword))
        {
            _logger.LogInformation("NULL_SEARCH term={SearchTerm} campaignSlug={CampaignSlug}", normalizedSearchTerm, normalizedCampaignSlug);
        }

        filteredHotels = ApplySponsorPinning(filteredHotels, normalizedSearchKeyword);
        model.MinPrice = filteredHotels.Where(x => x.StartingPrice.HasValue && x.StartingPrice.Value > 0m).Select(x => x.StartingPrice!.Value).DefaultIfEmpty(0).Min();
        model.MaxPrice = filteredHotels.Where(x => x.StartingPrice.HasValue && x.StartingPrice.Value > 0m).Select(x => x.StartingPrice!.Value).DefaultIfEmpty(0).Max();
        model.ActiveMinPrice = minPrice is > 0m ? minPrice : null;
        model.ActiveMaxPrice = maxPrice is > 0m ? maxPrice : null;
        filteredHotels = ApplyListingPriceFilter(filteredHotels, minPrice, maxPrice);
        model.TotalCount = filteredHotels.Count;
        model.Cities = filteredHotels.Select(x => x.City).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToList();
        model.Districts = filteredHotels.Select(x => x.District).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToList();
        model.Neighborhoods = filteredHotels.Select(x => x.Neighborhood).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToList();
        model.PropertyTypes = filteredHotels.Select(x => x.PropertyType).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToList();
        model.StarOptions = new List<int> { 5, 4, 3, 2, 1 };
        model.Campaigns = await LoadListingCampaignFiltersAsync(connection, model.CampaignSlug, cancellationToken);
        model.SmartRoutes = await _smartRouteService.GetListingFiltersAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(model.CampaignSlug))
        {
            var campaignMeta = await GetCampaignMetaFromDatabaseAsync(connection, model.CampaignSlug, cancellationToken);
            model.CampaignTitle = campaignMeta.Title;
            model.CampaignDescription = campaignMeta.Description;
        }
        else
        {
            var campaignMeta = GetCampaignMeta(model.ActiveTag);
            model.CampaignTitle = campaignMeta.Title;
            model.CampaignDescription = campaignMeta.Description;
        }

        model.MapHotels = filteredHotels.ToList();
        model.QuickLinks = BuildListingQuickLinks(displayLabel, model.ActiveTag);
        model.TotalPages = model.TotalCount <= 0
            ? 1
            : (int)Math.Ceiling(model.TotalCount / (decimal)model.PageSize);
        model.CurrentPage = Math.Min(model.CurrentPage, model.TotalPages);
        model.Hotels = filteredHotels
            .Skip((model.CurrentPage - 1) * model.PageSize)
            .Take(model.PageSize)
            .ToList();

        if (allowFuzzyFallback
            && filteredHotels.Count == 0
            && ilceId is not > 0
            && sehirId is not > 0
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
                    var fallbackModel = await GetHotelListingPageForSqlServerAsync(fallbackValue, campaignTag, campaignSlug, 1, contextualCityHint, minPrice, maxPrice, cancellationToken, false);
                    fallbackModel.SearchTerm = normalizedSearchTerm;
                    fallbackModel.SearchLabel = $"{normalizedSearchTerm} için {bestMatch.Label}";
                    fallbackModel.City = fallbackModel.SearchLabel;
                    return fallbackModel;
                }
            }
        }

        return model;
    }

    private static List<HotelListingCardViewModel> ApplyListingPriceFilter(
        List<HotelListingCardViewModel> hotels,
        decimal? minPrice,
        decimal? maxPrice)
    {
        if (hotels.Count == 0)
        {
            return hotels;
        }

        var hasMin = minPrice is > 0m;
        var hasMax = maxPrice is > 0m;
        if (!hasMin && !hasMax)
        {
            return hotels;
        }

        return hotels.Where(hotel =>
        {
            var price = hotel.StartingPrice ?? 0m;
            if (price <= 0m)
            {
                return false;
            }

            if (hasMin && price < minPrice!.Value)
            {
                return false;
            }

            if (hasMax && price > maxPrice!.Value)
            {
                return false;
            }

            return true;
        }).ToList();
    }

    private static List<HotelListingCardViewModel> ApplySponsorPinning(List<HotelListingCardViewModel> hotels, string regionKey)
    {
        if (hotels.Count == 0)
        {
            return hotels;
        }

        regionKey ??= string.Empty;
        var sponsors = hotels.Where(x => x.IsFeatured).ToList();
        if (sponsors.Count > 0)
        {
            DateTime localNow;
            try
            {
                var tz = TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time");
                localNow = TimeZoneInfo.ConvertTime(DateTime.UtcNow, tz);
            }
            catch
            {
                localNow = DateTime.UtcNow;
            }

            var periodKey = localNow.ToString("yyyyMMddHH", CultureInfo.InvariantCulture);
            var seed = $"{regionKey}|{periodKey}";

            int StableScore(long hotelId)
            {
                var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(seed + "|" + hotelId.ToString(CultureInfo.InvariantCulture)));
                return BitConverter.ToInt32(bytes, 0);
            }

            var topSponsors = sponsors
                .OrderBy(x => StableScore(x.Id))
                .Take(3)
                .ToList();

            var pinnedIds = topSponsors.Select(x => x.Id).ToHashSet();
            var rest = hotels.Where(x => !pinnedIds.Contains(x.Id)).ToList();
            return topSponsors.Concat(rest).ToList();
        }

        // No sponsors in this region: ensure the most-starred hotel appears first.
        var best = hotels
            .OrderByDescending(x => x.StarCount ?? 0)
            .ThenByDescending(x => x.Rating)
            .ThenByDescending(x => x.ReviewCount)
            .ThenByDescending(x => x.Id)
            .FirstOrDefault();

        if (best is null)
        {
            return hotels;
        }

        var bestId = best.Id;
        return new[] { best }.Concat(hotels.Where(x => x.Id != bestId)).ToList();
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

        var hasNormalizedColumns = await ColumnExistsAsync(connection, "[dbo].[OTELLER]", "SEHIR_NORMALIZED", cancellationToken);
        var normalizedCitySql = hasNormalizedColumns ? "o.sehir_normalized" : BuildSearchNormalizationSql("o.sehir");
        var normalizedDistrictSql = hasNormalizedColumns ? "o.ilce_normalized" : BuildSearchNormalizationSql("o.ilce");
        var normalizedNeighborhoodSql = hasNormalizedColumns ? "o.mahalle_normalized" : BuildSearchNormalizationSql("o.mahalle");
        var normalizedHotelNameSql = hasNormalizedColumns ? "o.otel_adi_normalized" : BuildSearchNormalizationSql("o.otel_adi");

        var sql = $"""
            SELECT TOP (8) suggestion_value, suggestion_label, suggestion_type, suggestion_hotel_code
            FROM (
                SELECT DISTINCT o.sehir AS suggestion_value, o.sehir AS suggestion_label, 'Sehir' AS suggestion_type, 1 AS sort_order, CAST('' AS nvarchar(50)) AS suggestion_hotel_code
                FROM oteller o
                WHERE o.yayin_durumu = N'Yayında'
              AND o.onay_durumu IN (N'Onaylandı', N'Onaylandi', N'OnaylandÄ±', N'Onaylanmış', N'Onaylanmis', N'Onayli')
                  AND {normalizedCitySql} LIKE @queryNormalized + '%'

                UNION

                SELECT DISTINCT o.ilce AS suggestion_value, CONCAT(o.ilce, ' / ', o.sehir) AS suggestion_label, 'Ilce' AS suggestion_type, 2 AS sort_order, CAST('' AS nvarchar(50)) AS suggestion_hotel_code
                FROM oteller o
                WHERE o.yayin_durumu = N'Yayında'
              AND o.onay_durumu IN (N'Onaylandı', N'Onaylandi', N'OnaylandÄ±', N'Onaylanmış', N'Onaylanmis', N'Onayli')
                  AND {normalizedDistrictSql} LIKE @queryNormalized + '%'

                UNION

                SELECT DISTINCT o.mahalle AS suggestion_value, CONCAT(o.mahalle, ' / ', o.ilce, ' / ', o.sehir) AS suggestion_label, 'Mahalle' AS suggestion_type, 3 AS sort_order, CAST('' AS nvarchar(50)) AS suggestion_hotel_code
                FROM oteller o
                WHERE o.yayin_durumu = N'Yayında'
              AND o.onay_durumu IN (N'Onaylandı', N'Onaylandi', N'OnaylandÄ±', N'Onaylanmış', N'Onaylanmis', N'Onayli')
                  AND o.mahalle IS NOT NULL
                  AND o.mahalle <> ''
                  AND {normalizedNeighborhoodSql} LIKE @queryNormalized + '%'

                UNION

                SELECT DISTINCT o.otel_adi AS suggestion_value, CONCAT(o.otel_adi, ' / ', o.ilce, ' / ', o.sehir) AS suggestion_label, 'Otel' AS suggestion_type, 4 AS sort_order, COALESCE(o.otel_kodu, '') AS suggestion_hotel_code
                FROM oteller o
                WHERE o.yayin_durumu = N'Yayında'
              AND o.onay_durumu IN (N'Onaylandı', N'Onaylandi', N'OnaylandÄ±', N'Onaylanmış', N'Onaylanmis', N'Onayli')
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
                Type = reader.GetString(2),
                Slug = string.Equals(reader.GetString(2), "Otel", StringComparison.OrdinalIgnoreCase)
                    ? BuildSlug(reader.GetString(0), reader.IsDBNull(3) ? string.Empty : reader.GetString(3))
                    : string.Empty
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

    private async Task<HotelDetailPageViewModel?> GetHotelDetailPageForSqlServerAsync(string slug, HotelDetailLoadOptions? options, CancellationToken cancellationToken)
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
                o.ulke_id,
                o.sehir_id,
                o.ilce_id,
                o.mahalle_id,
                COALESCE(NULLIF(o.video_url, ''), '') AS video_url,
                COALESCE(NULLIF(o.kapak_fotografi, ''), '') AS gorsel_url,
                COALESCE(NULLIF(LTRIM(RTRIM(o.turizm_belge_no)), ''), '') AS turizm_belge_no,
                COALESCE(NULLIF(LTRIM(RTRIM(o.turizm_belge_turu)), ''), '') AS turizm_belge_turu
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
                Name = reader.GetString(reader.GetOrdinal("OTEL_ADI")),
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
                UlkeId = reader.IsDBNull(reader.GetOrdinal("ulke_id")) ? null : reader.GetInt64(reader.GetOrdinal("ulke_id")),
                SehirId = reader.IsDBNull(reader.GetOrdinal("sehir_id")) ? null : reader.GetInt64(reader.GetOrdinal("sehir_id")),
                IlceId = reader.IsDBNull(reader.GetOrdinal("ilce_id")) ? null : reader.GetInt64(reader.GetOrdinal("ilce_id")),
                MahalleId = reader.IsDBNull(reader.GetOrdinal("mahalle_id")) ? null : reader.GetInt64(reader.GetOrdinal("mahalle_id")),
                VideoUrl = NormalizeMediaUrl(reader.GetString(reader.GetOrdinal("video_url"))),
                MainImageUrl = NormalizeHotelImageUrl(reader.GetInt64(reader.GetOrdinal("id")), reader.GetString(reader.GetOrdinal("gorsel_url"))),
                TourismDocumentNo = NullIfWhiteSpace(reader.GetString(reader.GetOrdinal("turizm_belge_no"))),
                TourismDocumentType = NullIfWhiteSpace(reader.GetString(reader.GetOrdinal("turizm_belge_turu")))
            };
        }

        ApplyDetailLoadOptions(model, options);
        await LoadHotelConditionsAsync(connection, model, cancellationToken);

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
                model.GalleryImages.Add(NormalizeHotelImageUrl(model.Id, galleryReader.GetString(0)));
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
            SELECT oo.ozellik_adi, COALESCE(oo.ozellik_ikon, 'fa-circle-check') AS ozellik_ikon
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
                var amenityName = NormalizeTurkishText(amenitiesReader.GetString(0));
                var amenityIcon = amenitiesReader.GetString(1);
                model.Amenities.Add(new HotelAmenityViewModel
                {
                    Name = NormalizeAmenityLabel(amenityName),
                    IconClass = NormalizeAmenityIcon(amenityIcon, amenityName)
                });
            }
        }

        const string hotelCampaignSql = """
            SELECT TOP (10)
                COALESCE(NULLIF(ko.kampanya_etiketi, ''), k.kampanya_adi) AS kampanya_adi,
                COALESCE(NULLIF(k.seo_slug, ''), NULLIF(k.sayfa_url, ''), k.kampanya_kodu) AS kampanya_slug
            FROM kampanya_oteller ko
            JOIN kampanyalar k
              ON k.id = ko.kampanya_id
            WHERE ko.otel_id = @hotelId
              AND ko.katilim_durumu = N'Aktif'
              AND (k.aktif_mi = 1 OR k.aktif_mi IS NULL)
              AND SYSUTCDATETIME() >= ko.baslangic_tarihi
              AND SYSUTCDATETIME() <= ko.bitis_tarihi
            ORDER BY ko.one_cikan DESC, ko.siralama ASC, k.siralama ASC, k.id DESC;
            """;

        await using (var campaignCommand = new SqlCommand(hotelCampaignSql, connection))
        {
            campaignCommand.Parameters.AddWithValue("@hotelId", model.Id);
            await using var campaignReader = await campaignCommand.ExecuteReaderAsync(cancellationToken);
            while (await campaignReader.ReadAsync(cancellationToken))
            {
                var name = NormalizeTurkishText(campaignReader.IsDBNull(0) ? string.Empty : campaignReader.GetString(0));
                var campaignSlugValue = campaignReader.IsDBNull(1) ? string.Empty : campaignReader.GetString(1);
                if (string.IsNullOrWhiteSpace(name)) continue;
                if (model.CampaignNames.Any(x => string.Equals(x, name, StringComparison.OrdinalIgnoreCase))) continue;
                model.CampaignNames.Add(name.Trim());
                model.CampaignSlugs.Add((campaignSlugValue ?? string.Empty).Trim());
            }
        }

        const string roomFeaturesSql = """
            SELECT
                rfo.oda_tip_id,
                ro.ozellik_adi,
                COALESCE(ro.ozellik_ikon, 'fa-circle-check') AS ozellik_ikon
            FROM oda_tipi_ozellikleri rfo
            JOIN oda_ozellikleri ro
                ON ro.id = rfo.ozellik_id
               AND ro.aktif_mi = 1
            WHERE rfo.oda_tip_id IN (SELECT id FROM oda_tipleri WHERE otel_id = @hotelId AND aktif_mi = 1)
            ORDER BY rfo.oda_tip_id ASC, ro.siralama ASC, ro.id ASC;
            """;
        var roomFeatureMap = new Dictionary<long, List<HotelRoomFeatureViewModel>>();
        await using (var roomFeaturesCommand = new SqlCommand(roomFeaturesSql, connection))
        {
            roomFeaturesCommand.Parameters.AddWithValue("@hotelId", model.Id);
            await using var roomFeaturesReader = await roomFeaturesCommand.ExecuteReaderAsync(cancellationToken);
            while (await roomFeaturesReader.ReadAsync(cancellationToken))
            {
                var roomId = roomFeaturesReader.GetInt64(0);
                var featureName = NormalizeTurkishText(roomFeaturesReader.IsDBNull(1) ? string.Empty : roomFeaturesReader.GetString(1));
                var featureIcon = NormalizeFeatureIcon(roomFeaturesReader.IsDBNull(2) ? "fa-circle-check" : roomFeaturesReader.GetString(2), featureName);

                if (string.IsNullOrWhiteSpace(featureName))
                {
                    continue;
                }

                if (!roomFeatureMap.TryGetValue(roomId, out var features))
                {
                    features = new List<HotelRoomFeatureViewModel>();
                    roomFeatureMap[roomId] = features;
                }

                if (features.Any(x => string.Equals(x.Name, featureName, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                features.Add(new HotelRoomFeatureViewModel
                {
                    Name = featureName,
                    IconClass = featureIcon
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
                var coverPhoto = NormalizeRoomImageUrl(model.Id, roomId, roomsReader.IsDBNull(8) ? string.Empty : roomsReader.GetString(8));
                var galleryJson = roomsReader.IsDBNull(9) ? string.Empty : roomsReader.GetString(9);
                var roomGalleryImages = ParseImageList(galleryJson)
                    .Select(x => NormalizeRoomImageUrl(model.Id, roomId, x))
                    .Where(static item => !string.IsNullOrWhiteSpace(item))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
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
                    BedType = bedType,
                    SquareMeter = squareMeter,
                    DetailDescription = BuildRoomDetailDescription(roomName, bedType, squareMeter, maxGuests, maxAdults, maxChildren),
                    Price = roomPrice,
                    MaxGuestCount = maxGuests,
                    MaxAdultCount = maxAdults,
                    MaxChildCount = maxChildren,
                    ImageUrl = roomGalleryImages.FirstOrDefault(),
                    GalleryImages = roomGalleryImages,
                    Features = roomFeatureMap.TryGetValue(roomId, out var roomFeatures)
                        ? roomFeatures
                        : new List<HotelRoomFeatureViewModel>(),
                    CancellationText = "Ücretsiz iptal"
                });
            }
        }

        var hotelTaxDisplay = await LoadHotelDisplayTaxPercentsAsync(connection, model.Id, cancellationToken);
        model.TaxDisplayVatPercent = hotelTaxDisplay.VatPercent;
        model.TaxDisplayAccommodationPercent = hotelTaxDisplay.AccommodationPercent;

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
                    var imageUrl = NormalizeRoomImageUrl(model.Id, roomId, roomGalleryReader.IsDBNull(1) ? string.Empty : roomGalleryReader.GetString(1));
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

            var roomDiscountIds = new HashSet<long>();
            foreach (var room in model.Rooms)
            {
                if (roomPriceMap.TryGetValue(room.RoomTypeId, out var effectivePrice) && effectivePrice > 0m)
                {
                    room.Price = effectivePrice;
                }

                // For the room cards: also expose base/discount for "bugün" vitrininde.
                var breakdown = await _hotelPricingReadService.GetRoomNightlyBreakdownAsync(room.RoomTypeId, today, today.AddDays(1), cancellationToken);
                var first = breakdown.Count > 0 ? breakdown[0] : null;
                if (first is not null)
                {
                    room.BasePrice = first.BasePrice > 0m ? first.BasePrice : null;
                    room.DiscountPrice = first.IsDiscounted && first.DiscountPrice.HasValue && first.DiscountPrice.Value > 0m
                        ? first.DiscountPrice.Value
                        : null;
                    room.DiscountId = first.IsDiscounted ? first.DiscountId : null;
                    if (room.DiscountId.HasValue && room.DiscountId.Value > 0)
                    {
                        roomDiscountIds.Add(room.DiscountId.Value);
                    }
                }
            }

            if (roomDiscountIds.Count > 0)
            {
                var discountMap = await LoadDiscountMetaMapAsync(connection, roomDiscountIds.ToList(), cancellationToken);
                foreach (var room in model.Rooms)
                {
                    if (room.DiscountId.HasValue && discountMap.TryGetValue(room.DiscountId.Value, out var meta))
                    {
                        room.DiscountName = meta.Name;
                        room.DiscountShortDescription = meta.ShortDescription;
                        room.DiscountImageUrl = meta.ImageUrl;
                    }
                }
            }

            var lowestRoomPrice = model.Rooms.Where(static item => item.Price > 0m).Select(static item => item.Price).DefaultIfEmpty(model.LowestRoomPrice).Min();
            if (lowestRoomPrice > 0m)
            {
                model.LowestRoomPrice = lowestRoomPrice;
            }

            const string roomPolicySql = """
                SELECT ot.id,
                       COALESCE(NULLIF(ofm.iptal_politikasi_override, ''), N'Ücretsiz iptal') AS iptal_politikasi
                FROM oda_tipleri ot
                OUTER APPLY (
                    SELECT TOP (1) ofm.iptal_politikasi_override
                    FROM oda_fiyat_musaitlik ofm
                    WHERE ofm.oda_tip_id = ot.id
                      AND ofm.otel_id = ot.otel_id
                      AND ofm.tarih >= CAST(SYSUTCDATETIME() AS date)
                      AND ofm.iptal_politikasi_override IS NOT NULL
                      AND LTRIM(RTRIM(ofm.iptal_politikasi_override)) <> ''
                    ORDER BY ofm.tarih ASC, ofm.id ASC
                ) ofm
                WHERE ot.otel_id = @hotelId
                  AND ot.aktif_mi = 1;
                """;

            var roomPolicyMap = new Dictionary<long, string>();
            await using (var roomPolicyCommand = new SqlCommand(roomPolicySql, connection))
            {
                roomPolicyCommand.Parameters.AddWithValue("@hotelId", model.Id);
                await using var roomPolicyReader = await roomPolicyCommand.ExecuteReaderAsync(cancellationToken);
                while (await roomPolicyReader.ReadAsync(cancellationToken))
                {
                    var roomId = roomPolicyReader.GetInt64(0);
                    var text = roomPolicyReader.IsDBNull(1) ? "Ücretsiz iptal" : NormalizeTurkishText(roomPolicyReader.GetString(1));
                    roomPolicyMap[roomId] = string.IsNullOrWhiteSpace(text) ? "Ücretsiz iptal" : text;
                }
            }

            foreach (var room in model.Rooms)
            {
                if (roomPolicyMap.TryGetValue(room.RoomTypeId, out var policyText))
                {
                    room.CancellationText = policyText;
                }
                else if (!string.IsNullOrWhiteSpace(model.Conditions?.CancellationSummary))
                {
                    room.CancellationText = model.Conditions.CancellationSummary;
                }
            }
        }

        // Otel detay: seçili check-in tarihine göre satışa kapalı odaları gösterme.
        // Not: ReservationForm default olarak "bugün/yarın" gelir.
        var availabilityDate = model.ReservationForm.CheckInDate;
        model.AvailabilityCheckDate = availabilityDate;
        if (model.Rooms.Count > 0)
        {
            const string closedRoomsSql = """
                SELECT DISTINCT ofm.oda_tip_id
                FROM oda_fiyat_musaitlik ofm
                WHERE ofm.otel_id = @hotelId
                  AND ofm.tarih = @date
                  AND ofm.kapali_satis = 1;
                """;
            var closedRoomIds = new HashSet<long>();
            await using (var closedCommand = new SqlCommand(closedRoomsSql, connection))
            {
                closedCommand.Parameters.AddWithValue("@hotelId", model.Id);
                closedCommand.Parameters.AddWithValue("@date", availabilityDate.ToDateTime(TimeOnly.MinValue));
                await using var closedReader = await closedCommand.ExecuteReaderAsync(cancellationToken);
                while (await closedReader.ReadAsync(cancellationToken))
                {
                    closedRoomIds.Add(closedReader.GetInt64(0));
                }
            }

            if (closedRoomIds.Count > 0)
            {
                model.Rooms = model.Rooms.Where(r => !closedRoomIds.Contains(r.RoomTypeId)).ToList();
            }

            var hasAnyReservableRoom = model.Rooms.Any(r => r.Price > 0m);
            if (model.Rooms.Count == 0 || !hasAnyReservableRoom)
            {
                model.HasNoOpenRoomsForSelectedDate = true;
                model.NoOpenRoomsMessage = $"Bu otelde {availabilityDate:dd.MM.yyyy} için satışa açık boş oda bulunmuyor.";
            }

            // (Pasif) Email altyapısı için 1 haftalık kapanış raporu hazırla.
            // Gönderim bu aşamada kapalı; sadece rapor text'i üretilir.
            if (model.HasNoOpenRoomsForSelectedDate)
            {
                var rangeStart = availabilityDate;
                var rangeEnd = availabilityDate.AddDays(6);
                const string reportSql = """
                    SELECT
                        ot.oda_adi,
                        ofm.tarih,
                        ofm.kapali_satis
                    FROM oda_tipleri ot
                    INNER JOIN oda_fiyat_musaitlik ofm
                        ON ofm.oda_tip_id = ot.id
                       AND ofm.otel_id = ot.otel_id
                    WHERE ot.otel_id = @hotelId
                      AND ot.aktif_mi = 1
                      AND ofm.tarih BETWEEN @startDate AND @endDate
                    ORDER BY ofm.tarih ASC, ot.siralama ASC, ot.id ASC;
                    """;

                var lines = new List<string>();
                await using (var reportCommand = new SqlCommand(reportSql, connection))
                {
                    reportCommand.Parameters.AddWithValue("@hotelId", model.Id);
                    reportCommand.Parameters.AddWithValue("@startDate", rangeStart.ToDateTime(TimeOnly.MinValue));
                    reportCommand.Parameters.AddWithValue("@endDate", rangeEnd.ToDateTime(TimeOnly.MinValue));
                    await using var reportReader = await reportCommand.ExecuteReaderAsync(cancellationToken);
                    while (await reportReader.ReadAsync(cancellationToken))
                    {
                        var roomName = NormalizeTurkishText(reportReader.IsDBNull(0) ? string.Empty : reportReader.GetString(0));
                        var date = DateOnly.FromDateTime(reportReader.GetDateTime(1));
                        var isClosed = !reportReader.IsDBNull(2) && reportReader.GetBoolean(2);
                        if (!string.IsNullOrWhiteSpace(roomName) && isClosed)
                        {
                            lines.Add($"{date:dd.MM.yyyy} · {roomName} · satışa kapalı");
                        }
                    }
                }

                if (lines.Count > 0)
                {
                    model.InventoryClosureWeekReport = string.Join("\n", lines.Distinct(StringComparer.OrdinalIgnoreCase));
                }
            }

            // Seçilen oda tipi filtre sonrası yoksa rezervasyon formunu stabilize et.
            if (model.ReservationForm.RoomTypeId > 0 && model.Rooms.All(x => x.RoomTypeId != model.ReservationForm.RoomTypeId))
            {
                var fallback = model.Rooms.FirstOrDefault(x => x.Price > 0m) ?? model.Rooms.FirstOrDefault();
                model.ReservationForm.RoomTypeId = fallback?.RoomTypeId ?? 0;
            }
        }

        const string reviewStatsSql = """
            SELECT
                COUNT(*) AS review_count,
                COALESCE(AVG(CAST(COALESCE(CAST(y.genel_puan_10 AS DECIMAL(9, 3)),
                    CASE
                        WHEN y.genel_puan <= 5 THEN CAST(y.genel_puan AS DECIMAL(9, 3)) * 2
                        WHEN y.genel_puan <= 10 THEN CAST(y.genel_puan AS DECIMAL(9, 3))
                        ELSE 10
                    END) AS DECIMAL(9, 3))), 0) AS avg_genel,
                COALESCE(AVG(CAST(COALESCE(CAST(y.puan_konum_10 AS DECIMAL(9, 3)),
                    CASE
                        WHEN y.konum_puani <= 5 THEN CAST(y.konum_puani AS DECIMAL(9, 3)) * 2
                        WHEN y.konum_puani <= 10 THEN CAST(y.konum_puani AS DECIMAL(9, 3))
                        ELSE 10
                    END) AS DECIMAL(9, 3))), 0) AS avg_konum,
                COALESCE(AVG(CAST(COALESCE(CAST(y.puan_temizlik_10 AS DECIMAL(9, 3)), CAST(y.puan_oda_10 AS DECIMAL(9, 3)),
                    CASE
                        WHEN y.temizlik_puani <= 5 THEN CAST(y.temizlik_puani AS DECIMAL(9, 3)) * 2
                        WHEN y.temizlik_puani <= 10 THEN CAST(y.temizlik_puani AS DECIMAL(9, 3))
                        WHEN y.konfor_puani <= 5 THEN CAST(y.konfor_puani AS DECIMAL(9, 3)) * 2
                        WHEN y.konfor_puani <= 10 THEN CAST(y.konfor_puani AS DECIMAL(9, 3))
                        ELSE 10
                    END) AS DECIMAL(9, 3))), 0) AS avg_temizlik,
                COALESCE(AVG(CAST(COALESCE(CAST(y.puan_fiyat_10 AS DECIMAL(9, 3)),
                    CASE
                        WHEN y.fiyat_performans_puani <= 5 THEN CAST(y.fiyat_performans_puani AS DECIMAL(9, 3)) * 2
                        WHEN y.fiyat_performans_puani <= 10 THEN CAST(y.fiyat_performans_puani AS DECIMAL(9, 3))
                        ELSE 10
                    END) AS DECIMAL(9, 3))), 0) AS avg_fp,
                COALESCE(AVG(CAST(COALESCE(CAST(y.puan_personel_10 AS DECIMAL(9, 3)),
                    CASE
                        WHEN y.personel_puani <= 5 THEN CAST(y.personel_puani AS DECIMAL(9, 3)) * 2
                        WHEN y.personel_puani <= 10 THEN CAST(y.personel_puani AS DECIMAL(9, 3))
                        ELSE 10
                    END) AS DECIMAL(9, 3))), 0) AS avg_personel,
                COALESCE(AVG(CAST(COALESCE(CAST(y.puan_sessizlik_10 AS DECIMAL(9, 3)), CAST(y.genel_puan_10 AS DECIMAL(9, 3)),
                    CASE
                        WHEN y.genel_puan <= 5 THEN CAST(y.genel_puan AS DECIMAL(9, 3)) * 2
                        WHEN y.genel_puan <= 10 THEN CAST(y.genel_puan AS DECIMAL(9, 3))
                        ELSE 10
                    END) AS DECIMAL(9, 3))), 0) AS avg_sessizlik,
                COALESCE(AVG(CAST(COALESCE(CAST(y.puan_ulasim_10 AS DECIMAL(9, 3)), CAST(y.puan_konum_10 AS DECIMAL(9, 3)),
                    CASE
                        WHEN y.konum_puani <= 5 THEN CAST(y.konum_puani AS DECIMAL(9, 3)) * 2
                        WHEN y.konum_puani <= 10 THEN CAST(y.konum_puani AS DECIMAL(9, 3))
                        ELSE 10
                    END) AS DECIMAL(9, 3))), 0) AS avg_ulasim
            FROM yorumlar AS y
            WHERE y.otel_id = @hotelId
              AND y.onay_durumu LIKE N'Onaylan%';
            """;

        await using (var statsCommand = new SqlCommand(reviewStatsSql, connection))
        {
            statsCommand.Parameters.AddWithValue("@hotelId", model.Id);
            await using var statsReader = await statsCommand.ExecuteReaderAsync(cancellationToken);
            if (await statsReader.ReadAsync(cancellationToken))
            {
                var approvedCount = Convert.ToInt32(statsReader.GetValue(0), CultureInfo.InvariantCulture);
                if (approvedCount > 0)
                {
                    model.ReviewCount = approvedCount;
                    model.Rating = ClampDisplayTen(decimal.Round(
                        Convert.ToDecimal(statsReader.GetValue(1), CultureInfo.InvariantCulture),
                        1,
                        MidpointRounding.AwayFromZero));
                    model.RatingText = BuildGuestRatingSummaryText(model.Rating);
                    model.ReviewLocationScore = ClampDisplayTen(decimal.Round(
                        Convert.ToDecimal(statsReader.GetValue(2), CultureInfo.InvariantCulture),
                        1,
                        MidpointRounding.AwayFromZero));
                    model.ReviewCleaningScore = ClampDisplayTen(decimal.Round(
                        Convert.ToDecimal(statsReader.GetValue(3), CultureInfo.InvariantCulture),
                        1,
                        MidpointRounding.AwayFromZero));
                    model.ReviewRoomScore = model.ReviewCleaningScore;
                    model.ReviewComfortScore = model.ReviewCleaningScore;
                    model.ReviewValueScore = ClampDisplayTen(decimal.Round(
                        Convert.ToDecimal(statsReader.GetValue(4), CultureInfo.InvariantCulture),
                        1,
                        MidpointRounding.AwayFromZero));
                    model.ReviewStaffScore = ClampDisplayTen(decimal.Round(
                        Convert.ToDecimal(statsReader.GetValue(5), CultureInfo.InvariantCulture),
                        1,
                        MidpointRounding.AwayFromZero));
                    model.ReviewQuietnessScore = ClampDisplayTen(decimal.Round(
                        Convert.ToDecimal(statsReader.GetValue(6), CultureInfo.InvariantCulture),
                        1,
                        MidpointRounding.AwayFromZero));
                    model.ReviewTransportScore = ClampDisplayTen(decimal.Round(
                        Convert.ToDecimal(statsReader.GetValue(7), CultureInfo.InvariantCulture),
                        1,
                        MidpointRounding.AwayFromZero));
                }
                else
                {
                    model.ReviewCount = 0;
                    model.Rating = 0m;
                    model.RatingText = BuildGuestRatingSummaryText(0m);
                    model.ReviewLocationScore = 0m;
                    model.ReviewCleaningScore = 0m;
                    model.ReviewRoomScore = 0m;
                    model.ReviewComfortScore = 0m;
                    model.ReviewValueScore = 0m;
                    model.ReviewStaffScore = 0m;
                    model.ReviewQuietnessScore = 0m;
                    model.ReviewTransportScore = 0m;
                }
            }
        }

        const string reviewsSql = """
            SELECT TOP (60)
                CASE WHEN y.anonim_mi = 1 THEN N'Misafir' ELSE COALESCE(u.[AD_SOYAD], N'Misafir') END AS ad_soyad,
                y.genel_puan,
                y.genel_puan_10,
                y.yorum_metni,
                y.olusturulma_tarihi,
                COALESCE(NULLIF(y.seyahat_profili, ''), '') AS seyahat_profili,
                y.memnuniyet_seviyesi,
                COALESCE(NULLIF(r.rezervasyon_no, ''), CAST(r.id AS nvarchar(30))) AS rezervasyon_no
            FROM yorumlar y
            LEFT JOIN rezervasyonlar r ON r.id = y.rezervasyon_id
            LEFT JOIN [dbo].[KULLANICILAR] u ON u.id = y.kullanici_id
            WHERE y.otel_id = @hotelId
              AND y.onay_durumu LIKE N'Onaylan%'
            ORDER BY y.olusturulma_tarihi DESC;
            """;
        await using (var reviewsCommand = new SqlCommand(reviewsSql, connection))
        {
            reviewsCommand.Parameters.AddWithValue("@hotelId", model.Id);
            var blockedWords = await LoadBlockedReviewWordsAsync(connection, cancellationToken);
            await using var reviewsReader = await reviewsCommand.ExecuteReaderAsync(cancellationToken);
            while (await reviewsReader.ReadAsync(cancellationToken))
            {
                var reviewName = reviewsReader.IsDBNull(0) ? "Misafir" : reviewsReader.GetString(0);
                decimal reviewScore;
                if (!reviewsReader.IsDBNull(2))
                {
                    reviewScore = ClampDisplayTen(Convert.ToDecimal(reviewsReader.GetValue(2), CultureInfo.InvariantCulture));
                }
                else
                {
                    var rawScore = Convert.ToDecimal(reviewsReader.GetValue(1), CultureInfo.InvariantCulture);
                    reviewScore = ClampDisplayTen(NormalizeStoredRatingToDisplayTen(rawScore));
                }

                var reviewDate = reviewsReader.GetDateTime(4);
                var travel = reviewsReader.IsDBNull(5) ? null : reviewsReader.GetString(5);
                var memRaw = reviewsReader.IsDBNull(6) ? null : reviewsReader.GetValue(6);
                int? memLevel = memRaw is null || memRaw is DBNull ? null : Convert.ToInt32(memRaw, CultureInfo.InvariantCulture);
                var reservationNo = reviewsReader.IsDBNull(7) ? null : reviewsReader.GetString(7);
                var rawText = reviewsReader.IsDBNull(3) ? string.Empty : reviewsReader.GetString(3);
                model.Reviews.Add(new HotelReviewViewModel
                {
                    Avatar = BuildAvatar(reviewName),
                    Name = reviewName,
                    DateText = reviewDate.ToString("dd MMMM yyyy", new CultureInfo("tr-TR")),
                    Score = decimal.Round(reviewScore, 1, MidpointRounding.AwayFromZero),
                    Text = ReviewTextFilter.MaskBlockedWords(rawText, blockedWords),
                    TravelProfile = string.IsNullOrWhiteSpace(travel) ? null : travel,
                    SatisfactionLabel = MemnuniyetEtiketi(memLevel),
                    ReservationNoTail = string.IsNullOrWhiteSpace(reservationNo)
                        ? null
                        : reservationNo.Length <= 3 ? reservationNo : reservationNo[^3..]
                });
            }
        }

        if (!string.IsNullOrWhiteSpace(model.City))
        {
            const string similarHotelsSql = """
                SELECT TOP (6)
                    o.id,
                    o.otel_kodu,
                    o.otel_adi,
                    COALESCE(NULLIF(o.kapak_fotografi, ''), '') AS kapak_url,
                    COALESCE(o.ortalama_puan, 0) AS ortalama_puan
                FROM oteller o
                WHERE o.yayin_durumu = N'Yayında'
                  AND o.onay_durumu IN (N'Onaylandı', N'Onaylandi', N'OnaylandÄ±', N'Onaylanmış', N'Onaylanmis', N'Onayli')
                  AND o.sehir = @city
                  AND o.id <> @hotelId
                ORDER BY COALESCE(o.ortalama_puan, 0) DESC, COALESCE(o.toplam_yorum_sayisi, 0) DESC, o.id DESC;
                """;

            var similarRows = new List<(long Id, string Code, string Name, string Img, decimal Rating)>();
            await using (var similarCmd = new SqlCommand(similarHotelsSql, connection))
            {
                similarCmd.Parameters.AddWithValue("@hotelId", model.Id);
                similarCmd.Parameters.AddWithValue("@city", model.City);
                await using var sr = await similarCmd.ExecuteReaderAsync(cancellationToken);
                while (await sr.ReadAsync(cancellationToken))
                {
                    similarRows.Add((
                        sr.GetInt64(0),
                        sr.GetString(1),
                        sr.GetString(2),
                        sr.IsDBNull(3) ? string.Empty : sr.GetString(3),
                        sr.GetDecimal(4)));
                }
            }

            foreach (var row in similarRows)
            {
                var storedNet = await _hotelPricingReadService.GetHotelEffectivePriceAsync(row.Id, today, today, cancellationToken);
                var guestPrice = storedNet.HasValue && storedNet.Value > 0m
                    ? model.GuestInclusiveNightlyFromStoredNet(storedNet.Value)
                    : 0m;
                model.SimilarHotels.Add(new HotelSimilarCardViewModel
                {
                    Name = row.Name,
                    Slug = BuildSlug(row.Name, row.Code),
                    ImageUrl = NormalizeImageUrl(row.Img),
                    RatingText = BuildRatingText(row.Rating),
                    PriceText = guestPrice > 0m
                        ? $"₺{guestPrice.ToString("N0", CultureInfo.GetCultureInfo("tr-TR"))} / gece"
                        : "Fiyat icin tarih secin"
                });
            }
        }

        return model;
    }

    private static async Task<Dictionary<long, (string Name, string? ShortDescription, string? ImageUrl)>> LoadDiscountMetaMapAsync(
        SqlConnection connection,
        IReadOnlyCollection<long> discountIds,
        CancellationToken cancellationToken)
    {
        if (!await HotelTableExistsAsync(connection, "fiyat_indirimleri", cancellationToken))
        {
            return new Dictionary<long, (string, string?, string?)>();
        }

        var ids = discountIds.Where(static id => id > 0).Distinct().ToList();
        if (ids.Count == 0)
        {
            return new Dictionary<long, (string, string?, string?)>();
        }

        var parameters = string.Join(", ", ids.Select((_, index) => $"@did{index}"));
        var sql = $@"
            SELECT id,
                   indirim_adi,
                   COALESCE(kisa_aciklama, '') AS kisa_aciklama,
                   COALESCE(gorsel_url, '') AS gorsel_url
            FROM fiyat_indirimleri
            WHERE aktif_mi = 1
              AND id IN ({parameters});";

        var map = new Dictionary<long, (string Name, string? ShortDescription, string? ImageUrl)>();
        await using var command = new SqlCommand(sql, connection);
        for (var i = 0; i < ids.Count; i++)
        {
            command.Parameters.AddWithValue($"@did{i}", ids[i]);
        }

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var id = reader.GetInt64(0);
            var name = reader.IsDBNull(1) ? string.Empty : reader.GetString(1);
            var desc = reader.IsDBNull(2) ? string.Empty : reader.GetString(2);
            var img = reader.IsDBNull(3) ? string.Empty : reader.GetString(3);
            map[id] = (name, string.IsNullOrWhiteSpace(desc) ? null : NormalizeTurkishText(desc), string.IsNullOrWhiteSpace(img) ? null : NormalizeImageUrl(img));
        }

        return map;
    }

    private static string BuildRoomDetailDescription(string roomName, string bedType, ushort? squareMeter, byte maxGuests, byte maxAdults, byte maxChildren)
    {
        var meterText = squareMeter.HasValue
            ? $"{squareMeter.Value} m2 ferah kullanım alanı"
            : "konforlu kullanım alanı";

        var childText = maxChildren > 0
            ? $" {maxChildren} çocuk kapasitesi de sunar."
            : string.Empty;

        return $"{roomName}, {bedType.ToLowerInvariant()} düzeni ve {meterText} ile öne çıkar. Oda toplamda {maxGuests} misafire kadar uygundur; en fazla {maxAdults} yetişkin kabul eder.{childText}";
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

    private static string NormalizeMediaUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return string.Empty;
        }

        var trimmed = url.Trim();
        if (trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return trimmed;
        }

        // Relative: treat as local route or file under wwwroot.
        return "/" + trimmed.TrimStart('~', '/', '\\').Replace("\\", "/");
    }

    private static string NormalizeHotelImageUrl(long hotelId, string imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            return string.Empty;
        }

        var trimmed = imageUrl.Trim();
        if (trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("/", StringComparison.OrdinalIgnoreCase))
        {
            return trimmed;
        }

        var normalized = trimmed.TrimStart('~', '/', '\\').Replace("\\", "/");
        if (normalized.StartsWith("uploads/", StringComparison.OrdinalIgnoreCase)
            || normalized.StartsWith("assets/", StringComparison.OrdinalIgnoreCase))
        {
            return "/" + normalized;
        }

        if (normalized.Contains("/", StringComparison.Ordinal))
        {
            return "/" + normalized;
        }

        // Demo/test verilerinde sadece dosya adi saklanmis olabiliyor.
        return MediaStoragePaths.HotelImagesUrl(hotelId, normalized);
    }

    private static string NormalizeRoomImageUrl(long hotelId, long roomId, string imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            return string.Empty;
        }

        var trimmed = imageUrl.Trim();
        if (trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("/", StringComparison.OrdinalIgnoreCase))
        {
            return trimmed;
        }

        var normalized = trimmed.TrimStart('~', '/', '\\').Replace("\\", "/");
        if (normalized.StartsWith("uploads/", StringComparison.OrdinalIgnoreCase)
            || normalized.StartsWith("assets/", StringComparison.OrdinalIgnoreCase))
        {
            return "/" + normalized;
        }

        if (normalized.Contains("/", StringComparison.Ordinal))
        {
            return "/" + normalized;
        }

        return MediaStoragePaths.RoomImagesUrl(hotelId, roomId, normalized);
    }

    private static string? NullIfWhiteSpace(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string NormalizeTurkishText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var text = value.Trim();
        if (!LooksLikeMojibake(text))
        {
            return text;
        }

        try
        {
            var latinBytes = Encoding.GetEncoding(1252).GetBytes(text);
            var fixedText = Encoding.UTF8.GetString(latinBytes).Trim();
            return string.IsNullOrWhiteSpace(fixedText) ? text : fixedText;
        }
        catch
        {
            return text;
        }
    }

    private static bool LooksLikeMojibake(string value)
        => value.Contains('Ã')
           || value.Contains('Å')
           || value.Contains('Ä')
           || value.Contains('Ð')
           || value.Contains('Þ');

    private static string BuildCampaignInfoText(IEnumerable<string> campaignNames)
    {
        var names = campaignNames
            .Where(static x => !string.IsNullOrWhiteSpace(x))
            .Select(NormalizeTurkishText)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(3)
            .ToList();

        return names.Count switch
        {
            0 => string.Empty,
            1 => $"{names[0]} kampanyası bu fiyat için aktif.",
            _ => $"{string.Join(", ", names)} kampanyaları bu otelde aktif."
        };
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

    private static void ApplyDetailLoadOptions(HotelDetailPageViewModel model, HotelDetailLoadOptions? options)
    {
        if (options is null)
        {
            return;
        }

        if (options.CheckIn is { } checkIn)
        {
            model.ReservationForm.CheckInDate = checkIn;
            if (options.CheckOut is null || options.CheckOut <= checkIn)
            {
                model.ReservationForm.CheckOutDate = checkIn.AddDays(1);
            }
        }

        if (options.CheckOut is { } checkOut && checkOut > model.ReservationForm.CheckInDate)
        {
            model.ReservationForm.CheckOutDate = checkOut;
        }

        if (options.RoomTypeId is > 0)
        {
            model.ReservationForm.RoomTypeId = options.RoomTypeId.Value;
        }
    }

    private static async Task LoadHotelConditionsAsync(SqlConnection connection, HotelDetailPageViewModel model, CancellationToken cancellationToken)
    {
        if (!await HotelTableExistsAsync(connection, "otel_kosullari", cancellationToken))
        {
            return;
        }

        const string sql = """
            SELECT TOP (1)
                COALESCE(iptal_politikasi_ozet, '') AS iptal_ozet,
                COALESCE(detayli_iptal_kosullari, '') AS iptal_detay,
                ucretsiz_iptal_suresi,
                COALESCE(sigara_politikasi, '') AS sigara,
                COALESCE(evcil_hayvan_politikasi, '') AS evcil,
                COALESCE(cocuk_kabul_yas_araligi, '') AS cocuk,
                COALESCE(on_odeme_gerekli_mi, 0) AS on_odeme,
                COALESCE(on_odeme_orani, 0) AS on_odeme_orani,
                COALESCE(kredi_karti_ile_odeme_kabul, 1) AS kart_kabul
            FROM otel_kosullari
            WHERE otel_id = @hotelId;
            """;

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@hotelId", model.Id);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return;
        }

        byte? freeHours = null;
        if (!reader.IsDBNull(reader.GetOrdinal("ucretsiz_iptal_suresi")))
        {
            var raw = Convert.ToInt32(reader.GetValue(reader.GetOrdinal("ucretsiz_iptal_suresi")), CultureInfo.InvariantCulture);
            if (raw is > 0 and <= 255)
            {
                freeHours = (byte)raw;
            }
        }

        model.Conditions = new HotelDetailConditionsViewModel
        {
            CancellationSummary = NormalizeTurkishText(reader.GetString(reader.GetOrdinal("iptal_ozet"))),
            CancellationDetail = NormalizeTurkishText(reader.GetString(reader.GetOrdinal("iptal_detay"))),
            FreeCancellationHours = freeHours,
            SmokingPolicy = NormalizeTurkishText(reader.GetString(reader.GetOrdinal("sigara"))),
            PetPolicy = NormalizeTurkishText(reader.GetString(reader.GetOrdinal("evcil"))),
            ChildPolicy = NormalizeTurkishText(reader.GetString(reader.GetOrdinal("cocuk"))),
            PrepaymentRequired = ReadFlag(reader, "on_odeme"),
            PrepaymentPercent = reader.IsDBNull(reader.GetOrdinal("on_odeme_orani"))
                ? 0m
                : reader.GetDecimal(reader.GetOrdinal("on_odeme_orani")),
            CardPaymentAccepted = ReadFlag(reader, "kart_kabul")
        };
    }

    private async Task PopulateHomeHotelGalleriesAsync(
        SqlConnection connection,
        List<HomeHotelCardViewModel> hotels,
        CancellationToken cancellationToken)
    {
        if (hotels.Count == 0)
        {
            return;
        }

        var hotelIds = hotels.Select(h => h.Id).Distinct().ToList();
        var galleries = new Dictionary<long, List<string>>();

        const int batchSize = 50;
        for (var offset = 0; offset < hotelIds.Count; offset += batchSize)
        {
            var batch = hotelIds.Skip(offset).Take(batchSize).ToList();
            var paramNames = batch.Select((_, i) => $"@hid{i}").ToList();
            var sql = $"""
                SELECT g.otel_id, g.gorsel_url
                FROM (
                    SELECT
                        g0.otel_id,
                        g0.gorsel_url,
                        ROW_NUMBER() OVER (
                            PARTITION BY g0.otel_id
                            ORDER BY g0.kapak_fotografi_mi DESC, g0.one_cikan DESC, g0.siralama ASC, g0.id ASC
                        ) AS rn
                    FROM otel_gorselleri g0
                    WHERE g0.otel_id IN ({string.Join(", ", paramNames)})
                      AND g0.onay_durumu IN (N'Onaylandı', N'Onaylandi', N'OnaylandÄ±', N'Onaylanmış', N'Onaylanmis', N'Onayli')
                      AND g0.gorsel_url NOT LIKE '/uploads/logo/%'
                ) g
                WHERE g.rn <= 4
                ORDER BY g.otel_id, g.rn;
                """;

            await using var command = new SqlCommand(sql, connection);
            for (var i = 0; i < batch.Count; i++)
            {
                command.Parameters.AddWithValue($"@hid{i}", batch[i]);
            }

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var id = reader.GetInt64(0);
                var url = reader.IsDBNull(1) ? string.Empty : reader.GetString(1);
                if (string.IsNullOrWhiteSpace(url))
                {
                    continue;
                }

                if (!galleries.TryGetValue(id, out var list))
                {
                    list = new List<string>();
                    galleries[id] = list;
                }

                var normalized = NormalizeHotelImageUrl(id, url);
                if (!string.IsNullOrWhiteSpace(normalized))
                {
                    list.Add(normalized);
                }
            }
        }

        foreach (var hotel in hotels)
        {
            if (galleries.TryGetValue(hotel.Id, out var urls) && urls.Count > 0)
            {
                hotel.GalleryImageUrls = urls;
            }
            else if (!string.IsNullOrWhiteSpace(hotel.ImageUrl))
            {
                hotel.GalleryImageUrls = new List<string> { hotel.ImageUrl };
            }
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
            // UX karari: "Yuksek Puanli" etiketi kartta gosterilmiyor.
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

    private static IEnumerable<HomeHotelCardViewModel> ApplyHomepageCampaignFilter(IEnumerable<HomeHotelCardViewModel> hotels, string activeTag)
    {
        var hotelList = hotels.ToList();
        if (string.IsNullOrWhiteSpace(activeTag))
        {
            return hotelList;
        }

        IEnumerable<HomeHotelCardViewModel> Filter(Func<HomeHotelCardViewModel, bool> predicate, Func<IEnumerable<HomeHotelCardViewModel>, IOrderedEnumerable<HomeHotelCardViewModel>>? orderBy = null)
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
            "havuzlu-oteller" => Filter(x => x.Amenities.Any(a => a.Label.Contains("Havuz", StringComparison.OrdinalIgnoreCase))
                                              || x.Tags.Any(t => t.Contains("Havuz", StringComparison.OrdinalIgnoreCase))),
            "evcil-hayvan-dostu" => Filter(x => x.Tags.Any(t => t.Contains("evcil", StringComparison.OrdinalIgnoreCase))
                                              || x.Amenities.Any(a => a.Label.Contains("evcil", StringComparison.OrdinalIgnoreCase))),
            "kampanyaya-dahil-oteller" => Filter(x => x.IsFeatured || x.HasDiscount).Take(12),
            "hafta-sonu-firsatlari" => Filter(x => x.IsRecommended || x.IsFeatured || x.Tags.Any(t => t.Contains("Hafta", StringComparison.OrdinalIgnoreCase))).Take(12),
            "butceme-uygun-oteller" => Filter(x => (x.StartingPrice ?? decimal.MaxValue) <= 4000m, items => items.OrderBy(x => x.StartingPrice ?? decimal.MaxValue)).Take(12),
            "ultra-luks" => Filter(x => (x.StartingPrice ?? 0m) >= 5500m || x.Rating >= 8.5m, items => items.OrderByDescending(x => x.StartingPrice ?? 0m)).Take(12),
            "ay-sonu-ozel" => Filter(x => x.IsFeatured || (x.StartingPrice ?? decimal.MaxValue) <= 4200m).Take(12),
            "flash-indirim" => Filter(x => (x.StartingPrice ?? decimal.MaxValue) <= 3500m || x.HasDiscount, items => items.OrderBy(x => x.StartingPrice ?? decimal.MaxValue)).Take(12),
            "erken-rezervasyon" => Filter(x => x.ReviewCount > 0, items => items.OrderByDescending(x => x.Rating)).Take(12),
            "akilli-fiyat" => Filter(x => x.IsSmartPrice || (x.StartingPrice ?? decimal.MaxValue) <= 3600m, items => items.OrderBy(x => x.StartingPrice ?? decimal.MaxValue)).Take(12),
            _ => hotelList
        };
    }

    private static IEnumerable<HotelListingCardViewModel> ApplyCampaignFilter(IEnumerable<HotelListingCardViewModel> hotels, string activeTag)
    {
        var hotelList = hotels.ToList();

        if (string.IsNullOrWhiteSpace(activeTag))
        {
            return hotelList;
        }

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
            "kahvalti-dahil" => Filter(x => HotelHasMealAmenity(x, "kahvalti dahil", "kahvaltı dahil")),
            "ogle-yemegi-dahil" => Filter(x => HotelHasMealAmenity(x, "ogle yemegi", "öğle yemegi", "ogle")),
            "aksam-yemegi-dahil" => Filter(x => HotelHasMealAmenity(x, "aksam yemegi", "akşam yemegi", "aksam")),
            _ => hotelList
        };
    }

    private static bool HotelHasMealAmenity(HotelListingCardViewModel hotel, params string[] markers)
    {
        if (markers.Length == 0)
        {
            return false;
        }

        return (hotel.Amenities ?? new List<string>()).Any(amenity =>
                markers.Any(marker => amenity.Contains(marker, StringComparison.OrdinalIgnoreCase)))
            || (hotel.Tags ?? new List<string>()).Any(tag =>
                markers.Any(marker => tag.Contains(marker, StringComparison.OrdinalIgnoreCase)));
    }

    private static string NormalizeCampaignTag(string? campaignTag)
    {
        if (string.IsNullOrWhiteSpace(campaignTag))
        {
            return string.Empty;
        }

        return campaignTag.Trim().ToLowerInvariant() switch
        {
            "budget" => "butceme-uygun-oteller",
            "discount" => "flash-indirim",
            "weekend" => "hafta-sonu-firsatlari",
            "pool" => "havuzlu-oteller",
            "campaign" => "kampanyaya-dahil-oteller",
            "butcene-uygun" => "butceme-uygun-oteller",
            "haftasonu-firsatlari" => "hafta-sonu-firsatlari",
            "kampanyali-oteller" => "kampanyaya-dahil-oteller",
            "yildiz-yagmuru" => "ultra-luks",
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
            "butik" => "butik-oteller",
            "kahvalti-dahil" => "kahvalti-dahil",
            "kahvalti" => "kahvalti-dahil",
            "breakfast" => "kahvalti-dahil",
            "ogle-yemegi-dahil" => "ogle-yemegi-dahil",
            "ogle-yemegi" => "ogle-yemegi-dahil",
            "lunch" => "ogle-yemegi-dahil",
            "aksam-yemegi-dahil" => "aksam-yemegi-dahil",
            "aksam-yemegi" => "aksam-yemegi-dahil",
            "dinner" => "aksam-yemegi-dahil",
            _ => string.Empty
        };
    }

    private static string NormalizeCampaignSlug(string? campaignSlug)
    {
        if (string.IsNullOrWhiteSpace(campaignSlug))
        {
            return string.Empty;
        }

        return campaignSlug.Trim().ToLowerInvariant();
    }

    private static async Task<List<HotelListingCampaignFilterViewModel>> LoadListingCampaignFiltersAsync(SqlConnection connection, string activeCampaignSlug, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                k.seo_slug,
                COALESCE(NULLIF(k.kampanya_adi, ''), NULLIF(k.listeleme_basligi, ''), N'Kampanya') AS kampanya_adi,
                COUNT(DISTINCT ko.otel_id) AS otel_adedi,
                MAX(CASE WHEN k.one_cikan_kampanya = 1 THEN 1 ELSE 0 END) AS one_cikan,
                MIN(COALESCE(k.siralama, 9999)) AS min_siralama
            FROM kampanyalar k
            JOIN kampanya_oteller ko ON ko.kampanya_id = k.id
            JOIN oteller o ON o.id = ko.otel_id
            WHERE k.aktif_mi = 1
              AND ko.katilim_durumu = N'Aktif'
              AND o.yayin_durumu = N'Yayında'
              AND o.onay_durumu IN (N'Onaylandı', N'Onaylandi', N'OnaylandÄ±', N'Onaylanmış', N'Onaylanmis', N'Onayli')
              AND (
                    k.gorunurluk_durumu IS NULL
                    OR LTRIM(RTRIM(k.gorunurluk_durumu)) = N''
                    OR LOWER(REPLACE(LTRIM(RTRIM(k.gorunurluk_durumu)), N'ı', N'i')) IN (N'yayinda', N'yayında')
                  )
              AND NULLIF(LTRIM(RTRIM(k.seo_slug)), N'') IS NOT NULL
            GROUP BY k.seo_slug, COALESCE(NULLIF(k.kampanya_adi, ''), NULLIF(k.listeleme_basligi, ''), N'Kampanya')
            HAVING COUNT(DISTINCT ko.otel_id) > 0
            ORDER BY one_cikan DESC, min_siralama ASC, otel_adedi DESC, kampanya_adi ASC;
            """;

        var campaigns = new List<HotelListingCampaignFilterViewModel>();
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var slug = reader.IsDBNull(0) ? string.Empty : reader.GetString(0);
            if (string.IsNullOrWhiteSpace(slug))
            {
                continue;
            }

            campaigns.Add(new HotelListingCampaignFilterViewModel
            {
                Slug = NormalizeCampaignSlug(slug),
                Name = NormalizeTurkishText(reader.IsDBNull(1) ? "Kampanya" : reader.GetString(1)),
                HotelCount = reader.IsDBNull(2) ? 0 : Convert.ToInt32(reader.GetValue(2), CultureInfo.InvariantCulture),
                IsActive = string.Equals(NormalizeCampaignSlug(slug), activeCampaignSlug, StringComparison.OrdinalIgnoreCase)
            });
        }

        return campaigns;
    }

    private static async Task<(string Title, string Description)> GetCampaignMetaFromDatabaseAsync(SqlConnection connection, string campaignSlug, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT TOP (1)
                COALESCE(NULLIF(k.listeleme_basligi, ''), NULLIF(k.kampanya_adi, ''), N'Kampanyali Oteller') AS baslik,
                COALESCE(NULLIF(k.listeleme_aciklamasi, ''), NULLIF(k.kisa_aciklama, ''), LEFT(k.kampanya_aciklamasi, 220), N'Secili kampanyaya dahil yayindaki otelleri listeleyin.') AS aciklama
            FROM kampanyalar k
            WHERE k.aktif_mi = 1
              AND (
                    k.gorunurluk_durumu IS NULL
                    OR LTRIM(RTRIM(k.gorunurluk_durumu)) = N''
                    OR LOWER(REPLACE(LTRIM(RTRIM(k.gorunurluk_durumu)), N'ı', N'i')) IN (N'yayinda', N'yayında')
                  )
              AND k.seo_slug = @campaignSlug;
            """;

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@campaignSlug", campaignSlug);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            return (reader.GetString(0), reader.GetString(1));
        }

        return ("Tum Oteller", "Veritabanindaki yayinda ve onayli tum tesisleri listeleyin, filtreleyin ve karsilastirin.");
    }

    private static (string Title, string Description) GetCampaignMeta(string activeTag)
    {
        if (string.IsNullOrWhiteSpace(activeTag))
        {
            return ("Tum Oteller", "Veritabanindaki yayinda ve onayli tum tesisleri listeleyin, filtreleyin ve karsilastirin.");
        }

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
            "hafta-sonu-firsatlari" => ("Hafta Sonu Fırsatları", "Kısa konaklamalar için rezervasyona hazır, kullanıcıların en çok baktığı hafta sonu otelleri."),
            "butceme-uygun-oteller" => ("Bütçeme Uygun Oteller", "Daha erişilebilir fiyat bandındaki otelleri hızlıca filtreleyin ve karşılaştırın."),
            "ultra-luks" => ("Ultra Lüks Seçkisi", "Yüksek segment, premium deneyim ve güçlü puan kombinasyonu sunan seçili tesisler."),
            "ay-sonu-ozel" => ("Ay Sonu Özel", "Ay sonu kampanyaları ve hızlı dönüş sağlayan fiyat avantajları burada toplanır."),
            "flash-indirim" => ("Flash İndirim", "Anlık indirimli, fiyat avantajı güçlü ve hızlı rezervasyona uygun oteller."),
            "erken-rezervasyon" => ("Erken Rezervasyon", "Ön planlama yapan kullanıcılar için güçlü fiyat dengesi sunan oteller."),
            "akilli-fiyat" => ("Akıllı Fiyat", "Fiyat-performans oranı güçlü otelleri algoritmik sıralama ile gösteriyoruz."),
            "kahvalti-dahil" => ("Kahvaltı Dahil Oteller", "Kahvaltısı konaklamaya dahil olan tesisleri listeleyin."),
            "ogle-yemegi-dahil" => ("Öğle Yemeği Sunan Oteller", "Öğle yemeği hizmeti sunan tesisleri keşfedin."),
            "aksam-yemegi-dahil" => ("Akşam Yemeği Sunan Oteller", "Akşam yemeği hizmeti sunan tesisleri keşfedin."),
            _ => ("Tum Oteller", "Veritabanindaki yayinda ve onayli tum tesisleri listeleyin, filtreleyin ve karsilastirin.")
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
            // UX karari: "Yuksek Puanli" etiketi anasayfa kartinda gosterilmiyor.
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

    /// <summary>
    /// Vitrin puani her zaman 0-10 araliginda tutulur (DB bozulmasi veya cift olcekten koruma).
    /// </summary>
    private static decimal ClampDisplayTen(decimal value)
        => decimal.Round(Math.Clamp(value, 0m, 10m), 1, MidpointRounding.AwayFromZero);

    private static string? MemnuniyetEtiketi(int? level)
        => level switch
        {
            1 => "Cok kotu",
            2 => "Kotu",
            3 => "Idare eder",
            4 => "Iyi",
            5 => "Cok iyi",
            _ => null
        };

    /// <summary>
    /// 1-5 sema: *2 ile 2-10 vitrin; 6-10 araligi dogrudan on uzerinden kabul edilir (canli veri uyumu).
    /// </summary>
    private static decimal NormalizeStoredRatingToDisplayTen(decimal stored)
    {
        if (stored <= 0m)
        {
            return 0m;
        }

        if (stored <= 5m)
        {
            return ClampDisplayTen(stored * 2m);
        }

        return ClampDisplayTen(stored);
    }

    /// <summary>Ozet metni; vitrin puani 2-10 olcegi.</summary>
    private static string BuildGuestRatingSummaryText(decimal ratingOutOfTen)
    {
        if (ratingOutOfTen <= 0m)
        {
            return "Yorum Bekleniyor";
        }

        if (ratingOutOfTen >= 9m)
        {
            return "Muhtesem";
        }

        if (ratingOutOfTen >= 8m)
        {
            return "Fevkalade";
        }

        if (ratingOutOfTen >= 7m)
        {
            return "Cok Iyi";
        }

        return "Iyi";
    }

    private static async Task<IReadOnlyList<string>> LoadBlockedReviewWordsAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        var result = new List<string>();
        if (!await HotelTableExistsAsync(connection, "blockyorumkelime", cancellationToken))
        {
            return result;
        }

        const string sql = @"SELECT kelime FROM [dbo].[BLOCKYORUMKELIME] WHERE [AKTIF_MI] = 1 ORDER BY [ID] DESC;";
        await using var cmd = new SqlCommand(sql, connection);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            if (!reader.IsDBNull(0))
            {
                var w = reader.GetString(0)?.Trim();
                if (!string.IsNullOrWhiteSpace(w))
                {
                    result.Add(w);
                }
            }
        }
        return result;
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
        var label = NormalizeTurkishText(parts[0]);
        var icon = parts.Length > 1 ? parts[1] : "fa-circle-check";

        return new HomeAmenityViewModel
        {
            Label = NormalizeAmenityLabel(label),
            IconClass = NormalizeAmenityIcon(icon, label)
        };
    }

    private static string NormalizeAmenityLabel(string label)
    {
        label = NormalizeTurkishText(label);
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
        var normalized = NormalizeFeatureIcon(icon, label);
        return label switch
        {
            "Ücretsiz WiFi" => "fa-wifi",
            "Açık Yüzme Havuzu" => "fa-water-ladder",
            "Kapalı Yüzme Havuzu" => "fa-water-ladder",
            "SPA ve Sağlık Merkezi" => "fa-spa",
            _ => normalized
        };
    }

    private static string NormalizeFeatureIcon(string icon, string label)
    {
        var cleanedIcon = (icon ?? string.Empty).Trim();
        var normalizedLabel = NormalizeTurkishText(label);
        return cleanedIcon switch
        {
            "fa-swimming-pool" => "fa-water-ladder",
            "fa-hot-tub" => "fa-spa",
            "fa-water" when normalizedLabel.Contains("Havuz", StringComparison.OrdinalIgnoreCase) => "fa-water-ladder",
            "fa-parking" => "fa-square-parking",
            "fa-dog" => "fa-paw",
            "fa-glass-cheers" => "fa-martini-glass-citrus",
            "fa-cocktail" => "fa-martini-glass-citrus",
            "fa-coffee" => "fa-mug-hot",
            "fa-child" => "fa-children",
            "fa-tshirt" => "fa-shirt",
            "fa-desk" => "fa-table",
            "fa-glass" => "fa-wine-bottle",
            "fa-refrigerator" => "fa-temperature-low",
            "fa-microwave" => "fa-kitchen-set",
            "fa-hands" => "fa-hand-sparkles",
            _ => label switch
            {
                "Ücretsiz WiFi" => "fa-wifi",
                "Açık Yüzme Havuzu" => "fa-water-ladder",
                "Kapalı Yüzme Havuzu" => "fa-water-ladder",
                "SPA ve Sağlık Merkezi" => "fa-spa",
                "Wi-Fi" => "fa-wifi",
                "Minibar" => "fa-wine-bottle",
                "Fön Makinası" => "fa-wind",
                "Saç Kurutma Makinesi" => "fa-wind",
                _ when normalizedLabel.Contains("WiFi", StringComparison.OrdinalIgnoreCase)
                    || normalizedLabel.Contains("Wifi", StringComparison.OrdinalIgnoreCase)
                    || normalizedLabel.Contains("İnternet", StringComparison.OrdinalIgnoreCase) => "fa-wifi",
                _ when normalizedLabel.Contains("Duş", StringComparison.OrdinalIgnoreCase)
                    || normalizedLabel.Contains("Banyo", StringComparison.OrdinalIgnoreCase)
                    || normalizedLabel.Contains("Küvet", StringComparison.OrdinalIgnoreCase)
                    || normalizedLabel.Contains("Jakuzi", StringComparison.OrdinalIgnoreCase)
                    || normalizedLabel.Contains("Havlu", StringComparison.OrdinalIgnoreCase) => "fa-bath",
                _ when normalizedLabel.Contains("Saç Kurutma", StringComparison.OrdinalIgnoreCase)
                    || normalizedLabel.Contains("Fön", StringComparison.OrdinalIgnoreCase)
                    || normalizedLabel.Contains("Fon", StringComparison.OrdinalIgnoreCase) => "fa-wind",
                _ when normalizedLabel.Contains("Bakım", StringComparison.OrdinalIgnoreCase)
                    || normalizedLabel.Contains("Kozmetik", StringComparison.OrdinalIgnoreCase)
                    || normalizedLabel.Contains("Sabun", StringComparison.OrdinalIgnoreCase) => "fa-pump-soap",
                _ when normalizedLabel.Contains("Gardırop", StringComparison.OrdinalIgnoreCase)
                    || normalizedLabel.Contains("Dolap", StringComparison.OrdinalIgnoreCase)
                    || normalizedLabel.Contains("Kıyafet", StringComparison.OrdinalIgnoreCase) => "fa-shirt",
                _ when normalizedLabel.Contains("Yatak", StringComparison.OrdinalIgnoreCase)
                    || normalizedLabel.Contains("Kanepe", StringComparison.OrdinalIgnoreCase)
                    || normalizedLabel.Contains("Koltuk", StringComparison.OrdinalIgnoreCase) => "fa-bed",
                _ when normalizedLabel.Contains("Mutfak", StringComparison.OrdinalIgnoreCase)
                    || normalizedLabel.Contains("Mikrodalga", StringComparison.OrdinalIgnoreCase)
                    || normalizedLabel.Contains("Ocak", StringComparison.OrdinalIgnoreCase)
                    || normalizedLabel.Contains("Fırın", StringComparison.OrdinalIgnoreCase) => "fa-kitchen-set",
                _ when normalizedLabel.Contains("Kettle", StringComparison.OrdinalIgnoreCase)
                    || normalizedLabel.Contains("Kahve", StringComparison.OrdinalIgnoreCase)
                    || normalizedLabel.Contains("Çay", StringComparison.OrdinalIgnoreCase) => "fa-mug-hot",
                _ => string.IsNullOrWhiteSpace(cleanedIcon) ? "fa-circle-check" : cleanedIcon
            }
        };
    }

    private static async Task<(long Id, string Slug)?> ResolveHotelIdentityBySlugAsync(SqlConnection connection, string slug, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT id, otel_kodu, otel_adi
            FROM oteller
            WHERE yayin_durumu = N'Yayında'
              AND onay_durumu IN (N'Onaylandı', N'Onaylandi', N'OnaylandÄ±', N'Onaylanmış', N'Onaylanmis', N'Onayli');
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
              AND onay_durumu IN (N'Onaylandı', N'Onaylandi', N'OnaylandÄ±', N'Onaylanmış', N'Onaylanmis', N'Onayli');
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

    private readonly record struct HotelDisplayTaxPercents(decimal VatPercent, decimal AccommodationPercent);

    private async Task ApplyAdminHomepageSectionsAsync(
        SqlConnection connection,
        AnasayfaViewModel model,
        List<HomeHotelCardViewModel> hotels,
        decimal homepageVatPercent,
        decimal homepageAccommodationPercent,
        bool hasDiscountTable,
        CancellationToken cancellationToken)
    {
        if (!await HotelTableExistsAsync(connection, "ANASAYFA_OTEL_BOLUMLERI", cancellationToken))
        {
            model.FeaturedRouteHotels = model.PopularHotels.Take(4).ToList();
            model.AdminHomepageSections.Add(BuildFallbackFeaturedRouteSection(model));
            return;
        }

        var sections = new List<(long Id, string Code, string Title, string? Subtitle, int SortOrder)>();
        const string sectionSql = @"
            SELECT b.[ID], b.[BOLUM_KODU], b.[BASLIK], b.[ALT_BASLIK], b.[SIRALAMA]
            FROM [dbo].[ANASAYFA_OTEL_BOLUMLERI] b
            WHERE b.[AKTIF_MI] = 1
            ORDER BY b.[SIRALAMA], b.[ID];";

        await using (var sectionCommand = new SqlCommand(sectionSql, connection))
        await using (var sectionReader = await sectionCommand.ExecuteReaderAsync(cancellationToken))
        {
            while (await sectionReader.ReadAsync(cancellationToken))
            {
                sections.Add((
                    sectionReader.GetInt64(0),
                    sectionReader.GetString(1),
                    sectionReader.GetString(2),
                    sectionReader.IsDBNull(3) ? null : sectionReader.GetString(3),
                    sectionReader.GetInt32(4)));
            }
        }

        var entriesBySection = new Dictionary<long, List<long>>();
        const string entriesSql = @"
            SELECT k.[BOLUM_ID], k.[OTEL_ID]
            FROM [dbo].[ANASAYFA_OTEL_KAYITLARI] k
            WHERE k.[AKTIF_MI] = 1
            ORDER BY k.[BOLUM_ID], k.[SIRALAMA], k.[ID];";

        await using (var entriesCommand = new SqlCommand(entriesSql, connection))
        await using (var entriesReader = await entriesCommand.ExecuteReaderAsync(cancellationToken))
        {
            while (await entriesReader.ReadAsync(cancellationToken))
            {
                var sectionId = entriesReader.GetInt64(0);
                var hotelId = entriesReader.GetInt64(1);
                if (!entriesBySection.TryGetValue(sectionId, out var list))
                {
                    list = new List<long>();
                    entriesBySection[sectionId] = list;
                }

                list.Add(hotelId);
            }
        }

        if (sections.Count == 0 && entriesBySection.Count == 0)
        {
            model.FeaturedRouteHotels = model.PopularHotels.Take(4).ToList();
            model.AdminHomepageSections.Add(BuildFallbackFeaturedRouteSection(model));
            return;
        }

        var hotelById = hotels.ToDictionary(x => x.Id);
        var missingIds = entriesBySection.Values
            .SelectMany(x => x)
            .Distinct()
            .Where(id => !hotelById.ContainsKey(id))
            .ToList();

        if (missingIds.Count > 0)
        {
            var extraHotels = await LoadSupplementalHomeHotelCardsAsync(
                connection,
                missingIds,
                homepageVatPercent,
                homepageAccommodationPercent,
                hasDiscountTable,
                cancellationToken);
            foreach (var card in extraHotels)
            {
                hotelById[card.Id] = card;
                hotels.Add(card);
            }

            if (extraHotels.Count > 0)
            {
                await PopulateHomeHotelGalleriesAsync(connection, extraHotels, cancellationToken);
            }
        }

        List<HomeHotelCardViewModel> ResolveHotels(IReadOnlyList<long> ids)
        {
            var resolved = new List<HomeHotelCardViewModel>();
            foreach (var id in ids)
            {
                if (hotelById.TryGetValue(id, out var card))
                {
                    resolved.Add(card);
                }
            }

            return resolved;
        }

        foreach (var section in sections)
        {
            var isFeaturedRoute = string.Equals(section.Code, "ozel-rotalar", StringComparison.OrdinalIgnoreCase);
            var hasEntries = entriesBySection.TryGetValue(section.Id, out var hotelIds) && hotelIds.Count > 0;
            List<HomeHotelCardViewModel> sectionHotels;

            if (hasEntries)
            {
                sectionHotels = ResolveHotels(hotelIds!);
            }
            else if (isFeaturedRoute)
            {
                sectionHotels = model.PopularHotels.Take(4).ToList();
            }
            else
            {
                continue;
            }

            if (sectionHotels.Count == 0 && !isFeaturedRoute)
            {
                continue;
            }

            var sectionKey = string.Equals(section.Code, "custom", StringComparison.OrdinalIgnoreCase)
                ? $"custom-{section.Id}"
                : section.Code;
            var adminSection = new AdminHomepageSectionViewModel
            {
                Key = sectionKey,
                Title = section.Title,
                Subtitle = section.Subtitle,
                Hotels = sectionHotels,
                SortOrder = section.SortOrder,
                IsFeaturedRoute = isFeaturedRoute,
                AllowEmptyFallback = isFeaturedRoute,
                SeeAllUrl = ResolveHomepageSectionSeeAllUrl(section.Code)
            };
            model.AdminHomepageSections.Add(adminSection);

            if (isFeaturedRoute)
            {
                model.FeaturedRouteHotels = sectionHotels;
                continue;
            }

            model.CustomHomepageSections.Add(new HomeCategorySectionViewModel
            {
                Key = sectionKey,
                Etiket = section.Subtitle ?? string.Empty,
                Title = section.Title,
                Hotels = sectionHotels
            });
        }

        if (model.FeaturedRouteHotels.Count == 0)
        {
            model.FeaturedRouteHotels = model.PopularHotels.Take(4).ToList();
            if (model.AdminHomepageSections.All(x => !x.IsFeaturedRoute))
            {
                model.AdminHomepageSections.Insert(0, BuildFallbackFeaturedRouteSection(model));
            }
        }
    }

    private static AdminHomepageSectionViewModel BuildFallbackFeaturedRouteSection(AnasayfaViewModel model)
    {
        var featuredHotels = model.FeaturedRouteHotels.Count > 0
            ? model.FeaturedRouteHotels
            : model.PopularHotels.Take(4).ToList();

        return new AdminHomepageSectionViewModel
        {
            Key = "ozel-rotalar",
            Title = "Seçilen Özel Rotalar",
            Hotels = featuredHotels,
            SortOrder = 10,
            IsFeaturedRoute = true,
            AllowEmptyFallback = true,
            SeeAllUrl = "/oteller"
        };
    }

    private static string ResolveHomepageSectionSeeAllUrl(string sectionCode)
    {
        if (string.Equals(sectionCode, "ozel-rotalar", StringComparison.OrdinalIgnoreCase))
        {
            return "/oteller";
        }

        if (string.Equals(sectionCode, "custom", StringComparison.OrdinalIgnoreCase))
        {
            return "/oteller";
        }

        return $"/oteller?etiket={Uri.EscapeDataString(sectionCode)}";
    }

    private async Task<List<HomeHotelCardViewModel>> LoadSupplementalHomeHotelCardsAsync(
        SqlConnection connection,
        IReadOnlyList<long> hotelIds,
        decimal homepageVatPercent,
        decimal homepageAccommodationPercent,
        bool hasDiscountTable,
        CancellationToken cancellationToken)
    {
        var cards = new List<HomeHotelCardViewModel>();
        if (hotelIds.Count == 0)
        {
            return cards;
        }

        var idCsv = string.Join(",", hotelIds);
        var discountSelect = hasDiscountTable
            ? """
                COALESCE(fi.indirim_adi, '') AS indirim_adi,
                COALESCE(fi.kisa_aciklama, '') AS indirim_aciklama,
                COALESCE(fi.gorsel_url, '') AS indirim_gorsel_url,
                """
            : """
                '' AS indirim_adi,
                '' AS indirim_aciklama,
                '' AS indirim_gorsel_url,
                """;
        var discountJoin = hasDiscountTable
            ? "LEFT JOIN fiyat_indirimleri fi ON fi.id = pf.indirim_id AND fi.aktif_mi = 1"
            : string.Empty;

        var hotelSql = $"""
            SELECT
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
                pf.indirim_id,
                {discountSelect}
                oz.ozellikler
            FROM oteller o
            LEFT JOIN (
                SELECT
                    hotel_prices.otel_id,
                    hotel_prices.effective_price AS baslangic_fiyat,
                    hotel_prices.base_price AS min_normal_fiyat,
                    hotel_prices.discount_price AS min_indirimli_fiyat,
                    hotel_prices.discount_id AS indirim_id
                FROM (
                    SELECT
                        ot.otel_id,
                        best.effective_price,
                        best.base_price,
                        best.discount_price,
                        best.discount_id,
                        ROW_NUMBER() OVER (
                            PARTITION BY ot.otel_id
                            ORDER BY
                                CASE WHEN best.effective_price IS NULL THEN 1 ELSE 0 END,
                                best.effective_price ASC,
                                ot.id ASC
                        ) AS rn
                    FROM oda_tipleri ot
                    OUTER APPLY (
                        SELECT TOP (1)
                            CASE
                                WHEN ofm.kapali_satis = 1 THEN NULL
                                WHEN (COALESCE(ofm.toplam_oda_sayisi, ot.toplam_oda_sayisi) - COALESCE(ofm.satilan_oda_sayisi, 0) - COALESCE(ofm.bloke_oda_sayisi, 0)) <= 0 THEN NULL
                                WHEN ofm.gecelik_fiyat IS NULL OR ofm.gecelik_fiyat <= 0 THEN NULL
                                WHEN ofm.indirimli_fiyat IS NOT NULL AND ofm.indirimli_fiyat > 0 AND ofm.indirimli_fiyat < ofm.gecelik_fiyat THEN ofm.indirimli_fiyat
                                ELSE ofm.gecelik_fiyat
                            END AS effective_price,
                            CASE
                                WHEN ofm.kapali_satis = 1 THEN NULL
                                WHEN (COALESCE(ofm.toplam_oda_sayisi, ot.toplam_oda_sayisi) - COALESCE(ofm.satilan_oda_sayisi, 0) - COALESCE(ofm.bloke_oda_sayisi, 0)) <= 0 THEN NULL
                                WHEN ofm.gecelik_fiyat IS NULL OR ofm.gecelik_fiyat <= 0 THEN NULL
                                ELSE ofm.gecelik_fiyat
                            END AS base_price,
                            CASE
                                WHEN ofm.kapali_satis = 1 THEN NULL
                                WHEN (COALESCE(ofm.toplam_oda_sayisi, ot.toplam_oda_sayisi) - COALESCE(ofm.satilan_oda_sayisi, 0) - COALESCE(ofm.bloke_oda_sayisi, 0)) <= 0 THEN NULL
                                WHEN ofm.gecelik_fiyat IS NULL OR ofm.gecelik_fiyat <= 0 THEN NULL
                                WHEN ofm.indirimli_fiyat IS NULL OR ofm.indirimli_fiyat <= 0 THEN NULL
                                WHEN ofm.indirimli_fiyat >= ofm.gecelik_fiyat THEN NULL
                                ELSE ofm.indirimli_fiyat
                            END AS discount_price,
                            ofm.kampanya_id AS discount_id
                        FROM oda_fiyat_musaitlik ofm
                        WHERE ofm.oda_tip_id = ot.id
                          AND ofm.otel_id = ot.otel_id
                          AND ofm.tarih = CAST(SYSUTCDATETIME() AS date)
                        ORDER BY
                            CASE WHEN ofm.kapali_satis = 1 THEN 1 ELSE 0 END ASC,
                            CASE WHEN (COALESCE(ofm.toplam_oda_sayisi, ot.toplam_oda_sayisi) - COALESCE(ofm.satilan_oda_sayisi, 0) - COALESCE(ofm.bloke_oda_sayisi, 0)) <= 0 THEN 1 ELSE 0 END ASC,
                            CASE WHEN ofm.gecelik_fiyat IS NULL OR ofm.gecelik_fiyat <= 0 THEN 1 ELSE 0 END ASC,
                            CASE
                                WHEN ofm.indirimli_fiyat IS NOT NULL AND ofm.indirimli_fiyat > 0 AND ofm.indirimli_fiyat < ofm.gecelik_fiyat THEN ofm.indirimli_fiyat
                                ELSE ofm.gecelik_fiyat
                            END ASC,
                            ofm.id ASC
                    ) best
                    WHERE ot.aktif_mi = 1
                ) hotel_prices
                WHERE hotel_prices.rn = 1
            ) pf ON pf.otel_id = o.id
            {discountJoin}
            LEFT JOIN (
                SELECT g1.otel_id, g1.gorsel_url
                FROM (
                    SELECT
                        g.otel_id,
                        g.gorsel_url,
                        ROW_NUMBER() OVER (PARTITION BY g.otel_id ORDER BY g.kapak_fotografi_mi DESC, g.one_cikan DESC, g.siralama ASC) AS rn
                    FROM otel_gorselleri g
                    WHERE g.onay_durumu IN (N'Onaylandı', N'Onaylandi', N'OnaylandÄ±', N'Onaylanmış', N'Onaylanmis', N'Onayli')
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
            WHERE o.id IN ({idCsv})
              AND {PublishStatusSql}
              AND {ApprovalStatusSql};
            """;

        await using var command = new SqlCommand(hotelSql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var hotelId = reader.GetInt64(reader.GetOrdinal("id"));
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
            var discountId = reader.IsDBNull(reader.GetOrdinal("indirim_id"))
                ? (long?)null
                : reader.GetInt64(reader.GetOrdinal("indirim_id"));
            var discountName = reader.IsDBNull(reader.GetOrdinal("indirim_adi")) ? string.Empty : reader.GetString(reader.GetOrdinal("indirim_adi"));
            var discountDesc = reader.IsDBNull(reader.GetOrdinal("indirim_aciklama")) ? string.Empty : reader.GetString(reader.GetOrdinal("indirim_aciklama"));
            var discountImageUrl = reader.IsDBNull(reader.GetOrdinal("indirim_gorsel_url")) ? string.Empty : reader.GetString(reader.GetOrdinal("indirim_gorsel_url"));
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

            decimal? guestStartingPrice = startingPrice.HasValue && startingPrice.Value > 0m
                ? decimal.Round(InclusiveNightlyPricing.StoredNetToGuestDisplay(startingPrice.Value, homepageVatPercent, homepageAccommodationPercent), 0)
                : null;
            decimal? guestOriginalPrice = originalPrice.HasValue && originalPrice.Value > 0m
                ? decimal.Round(InclusiveNightlyPricing.StoredNetToGuestDisplay(originalPrice.Value, homepageVatPercent, homepageAccommodationPercent), 0)
                : null;
            decimal? guestDiscountedPrice = discountedPrice.HasValue && discountedPrice.Value > 0m
                ? decimal.Round(InclusiveNightlyPricing.StoredNetToGuestDisplay(discountedPrice.Value, homepageVatPercent, homepageAccommodationPercent), 0)
                : null;

            byte? starCount = null;
            var starOrd = reader.GetOrdinal("yildiz_sayisi");
            if (!reader.IsDBNull(starOrd))
            {
                var rawStar = Convert.ToInt32(reader.GetValue(starOrd), CultureInfo.InvariantCulture);
                if (rawStar is > 0 and <= 7)
                {
                    starCount = (byte)rawStar;
                }
            }

            cards.Add(new HomeHotelCardViewModel
            {
                Id = hotelId,
                HotelCode = reader.GetString(reader.GetOrdinal("otel_kodu")),
                Name = reader.GetString(reader.GetOrdinal("OTEL_ADI")),
                City = reader.GetString(reader.GetOrdinal("sehir")),
                District = reader.GetString(reader.GetOrdinal("ilce")),
                LocationText = $"{reader.GetString(reader.GetOrdinal("ilce"))}, {reader.GetString(reader.GetOrdinal("sehir"))}",
                Rating = rating,
                RatingText = BuildRatingText(rating),
                ReviewCount = reviewCount,
                StartingPrice = guestStartingPrice,
                OriginalPrice = guestOriginalPrice,
                DiscountedPrice = guestDiscountedPrice,
                DiscountPercent = discountPercent,
                HasDiscount = hasDiscount,
                DiscountId = hasDiscount ? discountId : null,
                DiscountName = hasDiscount && !string.IsNullOrWhiteSpace(discountName) ? discountName : null,
                DiscountShortDescription = hasDiscount && !string.IsNullOrWhiteSpace(discountDesc) ? discountDesc : null,
                DiscountImageUrl = hasDiscount && !string.IsNullOrWhiteSpace(discountImageUrl) ? NormalizeImageUrl(discountImageUrl) : null,
                PriceText = hasDiscount && guestDiscountedPrice.HasValue
                    ? $"TRY {guestDiscountedPrice.Value:N0}"
                    : guestStartingPrice.HasValue ? $"TRY {guestStartingPrice.Value:N0}" : "Teklif Al",
                PriceNote = guestStartingPrice.HasValue ? "Vergiler dahil" : "Musait fiyat bilgisi bulunamadi",
                ImageUrl = NormalizeHotelImageUrl(hotelId, imageUrl),
                DetailSlug = BuildSlug(reader.GetString(reader.GetOrdinal("OTEL_ADI")), reader.GetString(reader.GetOrdinal("otel_kodu"))),
                Amenities = amenities,
                Tags = tags,
                IsSmartPrice = isFeatured || isRecommended || (startingPrice.HasValue && startingPrice.Value <= 3500m),
                IsFeatured = isFeatured,
                IsRecommended = isRecommended,
                StarCount = starCount
            });
        }

        return cards;
    }

    private static async Task<bool> HotelTableExistsAsync(SqlConnection connection, string tableName, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT COUNT(*)
            FROM information_schema.TABLES
            WHERE TABLE_CATALOG = DB_NAME()
              AND TABLE_NAME = @tableName;
            """;
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@tableName", tableName);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result, CultureInfo.InvariantCulture) > 0;
    }

    private static async Task<HotelDisplayTaxPercents> LoadHotelDisplayTaxPercentsAsync(SqlConnection connection, long hotelId, CancellationToken cancellationToken)
    {
        if (!await HotelTableExistsAsync(connection, "komisyon_vergiler", cancellationToken))
        {
            return new HotelDisplayTaxPercents(10m, 2m);
        }

        const string sql = """
            SELECT TOP (1)
                COALESCE(kv.kdv_orani, 10),
                COALESCE(kv.konaklama_vergisi_orani, 2)
            FROM komisyon_vergiler kv
            WHERE kv.otel_id = @hotelId
              AND kv.aktif_mi = 1
              AND kv.baslangic_tarihi <= @effectiveDate
              AND (kv.bitis_tarihi IS NULL OR kv.bitis_tarihi >= @effectiveDate)
            ORDER BY kv.baslangic_tarihi DESC, kv.id DESC;
            """;

        var effectiveDate = DateOnly.FromDateTime(DateTime.Today).ToDateTime(TimeOnly.MinValue);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@hotelId", hotelId);
        command.Parameters.AddWithValue("@effectiveDate", effectiveDate);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            return new HotelDisplayTaxPercents(reader.GetDecimal(0), reader.GetDecimal(1));
        }

        return new HotelDisplayTaxPercents(10m, 2m);
    }
}




