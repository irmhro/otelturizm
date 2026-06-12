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
        var sql = $"SELECT [OTEL_ID] FROM [dbo].[KULLANICI_FAVORI_OTELLER] WHERE [KULLANICI_ID] = @userId AND [OTEL_ID] IN ({parameters}) AND COALESCE([AKTIF_MI], 1) = 1 AND [KALDIRILMA_TARIHI] IS NULL;";

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

        const string sql = @"SELECT COUNT(*) FROM [dbo].[KULLANICI_FAVORI_OTELLER] WHERE [KULLANICI_ID] = @userId AND COALESCE([AKTIF_MI], 1) = 1 AND [KALDIRILMA_TARIHI] IS NULL;";

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@userId", userId);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result, CultureInfo.InvariantCulture);
    }

    public async Task<UserFavoritesPageViewModel> GetFavoritesPageAsync(long userId, string? searchTerm = null, string? sort = null, int page = 1, CancellationToken cancellationToken = default)
    {
        var normalizedSearch = (searchTerm ?? string.Empty).Trim();
        var normalizedSort = NormalizeFavoriteSort(sort);
        var model = new UserFavoritesPageViewModel
        {
            Page = Math.Max(1, page),
            PageSize = 7,
            SearchTerm = normalizedSearch,
            Sort = normalizedSort
        };
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
                f.[OTEL_ID],
                f.[OLUSTURULMA_TARIHI],
                o.[OTEL_KODU],
                o.[OTEL_ADI],
                o.[SEHIR],
                o.[ILCE],
                COALESCE(o.[ONAY_DURUMU], '') AS [ONAY_DURUMU],
                COALESCE(o.[YAYIN_DURUMU], '') AS [YAYIN_DURUMU],
                COALESCE(o.[ORTALAMA_PUAN], 0) AS [ORTALAMA_PUAN],
                COALESCE(o.[TOPLAM_YORUM_SAYISI], 0) AS [TOPLAM_YORUM_SAYISI],
                COALESCE(NULLIF(o.[KAPAK_FOTOGRAFI], ''), NULLIF(og.[GORSEL_URL], '')) AS [GORSEL_URL],
                pf.baslangic_fiyat,
                a.[HEDEF_MAKSIMUM_FIYAT],
                a.[BASLANGIC_TARIHI] AS alert_baslangic_tarihi,
                a.[BITIS_TARIHI] AS alert_bitis_tarihi,
                a.[SON_TETIKLENEN_TARIH] AS alert_son_tetiklenen_tarih,
                COALESCE(a.[AKTIF_MI], 0) AS alert_aktif_mi,
                (
                    SELECT COUNT(*)
                    FROM [dbo].[REZERVASYONLAR] r
                    WHERE r.[KULLANICI_ID] = @userId
                      AND r.[OTEL_ID] = f.[OTEL_ID]
                      AND r.[DURUM] <> 'İptal Edildi'
                      AND CAST(r.[CIKIS_TARIHI] AS date) < CAST(SYSUTCDATETIME() AS date)
                ) AS past_stay_count,
                (
                    SELECT COUNT(*)
                    FROM [dbo].[REZERVASYONLAR] r
                    WHERE r.[KULLANICI_ID] = @userId
                      AND r.[OTEL_ID] = f.[OTEL_ID]
                      AND r.[DURUM] <> N'İptal Edildi'
                ) AS reservation_count,
                (
                    SELECT COUNT(*)
                    FROM [dbo].[YORUMLAR] y
                    INNER JOIN [dbo].[REZERVASYONLAR] r ON r.id = y.[REZERVASYON_ID]
                    WHERE y.[KULLANICI_ID] = @userId
                      AND r.[OTEL_ID] = f.[OTEL_ID]
                ) AS user_review_count,
                (
                    SELECT COUNT(*)
                    FROM [dbo].[REZERVASYONLAR] r
                    WHERE r.[KULLANICI_ID] = @userId
                      AND r.[OTEL_ID] = f.[OTEL_ID]
                      AND r.[DURUM] <> N'İptal Edildi'
                      AND CAST(r.[CIKIS_TARIHI] AS date) < CAST(SYSUTCDATETIME() AS date)
                      AND NOT EXISTS (
                          SELECT 1
                          FROM [dbo].[YORUMLAR] y
                          WHERE y.[REZERVASYON_ID] = r.id
                            AND y.[KULLANICI_ID] = @userId
                      )
                ) AS pending_review_count,
                (
                    SELECT CAST(ROUND(AVG(CAST(COALESCE(
                        CAST(y.[GENEL_PUAN_10] AS DECIMAL(9, 4)),
                        CASE
                            WHEN y.[GENEL_PUAN] <= 5 THEN CAST(y.[GENEL_PUAN] AS DECIMAL(9, 4)) * 2
                            WHEN y.[GENEL_PUAN] <= 10 THEN CAST(y.[GENEL_PUAN] AS DECIMAL(9, 4))
                            ELSE 10
                        END
                    ) AS DECIMAL(9, 4))), 1) AS DECIMAL(5, 1))
                    FROM [dbo].[YORUMLAR] y
                    INNER JOIN [dbo].[REZERVASYONLAR] r ON r.id = y.[REZERVASYON_ID]
                    WHERE y.[KULLANICI_ID] = @userId
                      AND r.[OTEL_ID] = f.[OTEL_ID]
                ) AS user_average_rating,
                (
                    SELECT MAX(r.[OLUSTURULMA_TARIHI])
                    FROM [dbo].[REZERVASYONLAR] r
                    WHERE r.[KULLANICI_ID] = @userId
                      AND r.[OTEL_ID] = f.[OTEL_ID]
                      AND r.[DURUM] <> N'İptal Edildi'
                ) AS last_reservation_date,
                (
                    SELECT TOP (1) r.id
                    FROM [dbo].[REZERVASYONLAR] r
                    LEFT JOIN [dbo].[YORUMLAR] y ON y.[REZERVASYON_ID] = r.id AND y.[KULLANICI_ID] = @userId
                    WHERE r.[KULLANICI_ID] = @userId
                      AND r.[OTEL_ID] = f.[OTEL_ID]
                      AND y.id IS NULL
                      AND r.[DURUM] NOT IN (N'İptal Edildi', N'Reddedildi')
                      AND CAST(r.[CIKIS_TARIHI] AS date) < CAST(SYSUTCDATETIME() AS date)
                      AND r.[DURUM] IN (N'Tamamlandı', N'Giriş Yaptı', N'Onaylandı')
                      AND (
                          COALESCE(r.[OTEL_ONAY_DURUMU], '') = N'Onaylandı'
                          OR r.[DURUM] IN (N'Tamamlandı', N'Giriş Yaptı', N'Onaylandı')
                      )
                    ORDER BY r.[CIKIS_TARIHI] DESC, r.id DESC
                ) AS first_eligible_review_reservation_id
            FROM [dbo].[KULLANICI_FAVORI_OTELLER] f
            JOIN [dbo].[OTELLER] o ON o.id = f.[OTEL_ID]
            LEFT JOIN (
                SELECT g1.[OTEL_ID], g1.[GORSEL_URL]
                FROM (
                    SELECT
                        g.[OTEL_ID],
                        g.[GORSEL_URL],
                        ROW_NUMBER() OVER (PARTITION BY g.[OTEL_ID] ORDER BY g.[KAPAK_FOTOGRAFI_MI] DESC, g.[ONE_CIKAN] DESC, g.[SIRALAMA] ASC, g.id ASC) AS rn
                    FROM [dbo].[OTEL_GORSELLERI] g
                    WHERE g.[ONAY_DURUMU] = 'Onaylandı'
                ) g1
                WHERE g1.rn = 1
            ) og ON og.[OTEL_ID] = o.id
            LEFT JOIN (
                SELECT ot.[OTEL_ID],
                       MIN(
                           COALESCE(
                               CASE
                                   WHEN ofm.[KAPALI_SATIS] = 1 THEN NULL
                                   WHEN (COALESCE(ofm.[TOPLAM_ODA_SAYISI], ot.[TOPLAM_ODA_SAYISI]) - COALESCE(ofm.[SATILAN_ODA_SAYISI], 0) - COALESCE(ofm.[BLOKE_ODA_SAYISI], 0)) <= 0 THEN NULL
                                   ELSE COALESCE(NULLIF(ofm.[INDIRIMLI_FIYAT], 0), NULLIF(ofm.[GECELIK_FIYAT], 0))
                               END,
                               NULLIF(ot.[STANDART_GECELIK_FIYAT], 0)
                           )
                       ) AS baslangic_fiyat
                FROM [dbo].[ODA_TIPLERI] ot
                LEFT JOIN [dbo].[ODA_FIYAT_MUSAITLIK] ofm ON ofm.[ODA_TIP_ID] = ot.id
                    AND ofm.[OTEL_ID] = ot.[OTEL_ID]
                    AND ofm.[TARIH] BETWEEN CAST(SYSUTCDATETIME() AS date) AND DATEADD(DAY, 120, CAST(SYSUTCDATETIME() AS date))
                WHERE ot.[AKTIF_MI] = 1
                GROUP BY ot.[OTEL_ID]
            ) pf ON pf.[OTEL_ID] = o.id
            LEFT JOIN [dbo].[KULLANICI_FAVORI_FIYAT_ALARMLARI] a
                ON a.[KULLANICI_ID] = f.[KULLANICI_ID]
               AND a.[OTEL_ID] = f.[OTEL_ID]
               AND COALESCE(a.[AKTIF_MI], 1) = 1
            WHERE f.[KULLANICI_ID] = @userId
              AND COALESCE(f.[AKTIF_MI], 1) = 1
            ORDER BY f.[OLUSTURULMA_TARIHI] DESC, f.id DESC;";

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@userId", userId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var culture = new CultureInfo("tr-TR");

        while (await reader.ReadAsync(cancellationToken))
        {
            var rating = reader.GetDecimal(reader.GetOrdinal("ortalama_puan"));
            var reviewCount = Convert.ToInt32(reader.GetValue(reader.GetOrdinal("toplam_yorum_sayisi")), CultureInfo.InvariantCulture);
            var priceOrdinal = reader.GetOrdinal("baslangic_fiyat");
            var price = reader.IsDBNull(priceOrdinal) ? (decimal?)null : reader.GetDecimal(priceOrdinal);
            var createdAt = reader.GetDateTime(reader.GetOrdinal("olusturulma_tarihi"));
            var hotelCode = reader.IsDBNull(reader.GetOrdinal("otel_kodu")) ? string.Empty : reader.GetString(reader.GetOrdinal("otel_kodu"));
            var hotelName = reader.IsDBNull(reader.GetOrdinal("otel_adi")) ? "Otel" : reader.GetString(reader.GetOrdinal("otel_adi"));
            var approvalStatus = reader.IsDBNull(reader.GetOrdinal("onay_durumu")) ? string.Empty : reader.GetString(reader.GetOrdinal("onay_durumu"));
            var publishStatus = reader.IsDBNull(reader.GetOrdinal("yayin_durumu")) ? string.Empty : reader.GetString(reader.GetOrdinal("yayin_durumu"));
            var alertActive = false;
            var alertActiveOrdinal = reader.GetOrdinal("alert_aktif_mi");
            if (!reader.IsDBNull(alertActiveOrdinal))
            {
                alertActive = Convert.ToInt32(reader.GetValue(alertActiveOrdinal), CultureInfo.InvariantCulture) == 1;
            }
            decimal? alertTarget = null;
            if (!reader.IsDBNull(reader.GetOrdinal("hedef_maksimum_fiyat")))
            {
                alertTarget = reader.GetDecimal(reader.GetOrdinal("hedef_maksimum_fiyat"));
            }
            DateTime? alertStart = reader.IsDBNull(reader.GetOrdinal("alert_baslangic_tarihi")) ? null : reader.GetDateTime(reader.GetOrdinal("alert_baslangic_tarihi"));
            DateTime? alertEnd = reader.IsDBNull(reader.GetOrdinal("alert_bitis_tarihi")) ? null : reader.GetDateTime(reader.GetOrdinal("alert_bitis_tarihi"));
            DateTime? lastTriggered = reader.IsDBNull(reader.GetOrdinal("alert_son_tetiklenen_tarih")) ? null : reader.GetDateTime(reader.GetOrdinal("alert_son_tetiklenen_tarih"));
            DateTime? lastReservationDate = reader.IsDBNull(reader.GetOrdinal("last_reservation_date")) ? null : reader.GetDateTime(reader.GetOrdinal("last_reservation_date"));
            var reservationCount = Convert.ToInt32(reader.GetValue(reader.GetOrdinal("reservation_count")), CultureInfo.InvariantCulture);
            var reviewGivenCount = Convert.ToInt32(reader.GetValue(reader.GetOrdinal("user_review_count")), CultureInfo.InvariantCulture);
            var reviewPendingCount = Convert.ToInt32(reader.GetValue(reader.GetOrdinal("pending_review_count")), CultureInfo.InvariantCulture);
            var firstEligibleRevOrd = reader.GetOrdinal("first_eligible_review_reservation_id");
            long? firstEligibleReviewReservationId = reader.IsDBNull(firstEligibleRevOrd)
                ? null
                : reader.GetInt64(firstEligibleRevOrd);
            var averageRatingOrdinal = reader.GetOrdinal("user_average_rating");
            var userAverageRating = reader.IsDBNull(averageRatingOrdinal) ? 0m : reader.GetDecimal(averageRatingOrdinal);

            model.Hotels.Add(new UserFavoriteHotelCardViewModel
            {
                HotelId = reader.GetInt64(reader.GetOrdinal("otel_id")),
                HotelCode = hotelCode,
                Name = hotelName,
                Slug = BuildSlug(hotelName, hotelCode),
                City = reader.IsDBNull(reader.GetOrdinal("sehir")) ? string.Empty : reader.GetString(reader.GetOrdinal("sehir")),
                District = reader.IsDBNull(reader.GetOrdinal("ilce")) ? string.Empty : reader.GetString(reader.GetOrdinal("ilce")),
                AvailabilityNote = BuildAvailabilityNote(approvalStatus, publishStatus),
                ImageUrl = NormalizeImageUrl(reader.IsDBNull(reader.GetOrdinal("gorsel_url")) ? string.Empty : reader.GetString(reader.GetOrdinal("gorsel_url"))),
                Rating = rating,
                ReviewCount = reviewCount,
                StartingPrice = price,
                PriceText = price.HasValue ? $"TRY {price.Value:N0}" : "Teklif Al",
                RatingText = rating > 0 ? (rating >= 9 ? "Olağanüstü" : rating >= 8 ? "Çok İyi" : "İyi") : "Yorum Bekleniyor",
                FavoriteAddedAt = createdAt,
                AddedDateText = $"{createdAt.ToString("dd MMMM yyyy", culture)} tarihinde kaydedildi",
                LastReservationDate = lastReservationDate,
                LastReservationDateText = lastReservationDate.HasValue ? $"{lastReservationDate.Value.ToString("dd MMMM yyyy", culture)} son rezervasyon" : "Henüz rezervasyon yok",
                PastStayCount = Convert.ToInt32(reader.GetValue(reader.GetOrdinal("past_stay_count")), CultureInfo.InvariantCulture),
                ReservationCount = reservationCount,
                ReviewGivenCount = reviewGivenCount,
                ReviewPendingCount = reviewPendingCount,
                FirstEligibleReviewReservationId = firstEligibleReviewReservationId,
                UserAverageRating = userAverageRating,
                ReservationCountText = reservationCount > 0 ? $"{reservationCount} rezervasyon" : "Rezervasyon yok",
                ReviewGivenText = reviewGivenCount > 0 ? $"{reviewGivenCount} yorum verdiniz" : "Henüz yorum yok",
                ReviewPendingText = reviewPendingCount > 0 ? $"{reviewPendingCount} konaklama yorum bekliyor" : "Yorum bekleyen konaklama yok",
                UserAverageRatingText = userAverageRating > 0 ? $"{userAverageRating:0.0}/10" : "-",
                PriceAlertEnabled = alertActive,
                PriceAlertTargetAmount = alertTarget,
                PriceAlertTargetText = alertTarget.HasValue ? $"TRY {alertTarget.Value:N0}" : null,
                PriceAlertDateRangeText = alertStart.HasValue && alertEnd.HasValue ? $"{alertStart.Value:dd.MM.yyyy} - {alertEnd.Value:dd.MM.yyyy}" : null,
                PriceAlertLastTriggeredText = lastTriggered.HasValue ? $"{lastTriggered.Value:dd.MM.yyyy HH:mm}" : null,
                PriceAlertStartDateValue = alertStart?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                PriceAlertEndDateValue = alertEnd?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
            });
        }

        model.FavoriteCount = model.Hotels.Count;
        await EnrichFavoriteSmartRoutesAsync(connection, model.Hotels, cancellationToken);
        model.RouteSearchHints = model.Hotels
            .SelectMany(h => h.SmartRouteLabels)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .Take(30)
            .ToList();
        if (!string.IsNullOrWhiteSpace(normalizedSearch))
        {
            model.Hotels = model.Hotels
                .Where(h => MatchesFavoriteSearch(h, normalizedSearch))
                .ToList();
        }

        model.Hotels = normalizedSort switch
        {
            "most-reserved" => model.Hotels
                .OrderByDescending(h => h.ReservationCount)
                .ThenByDescending(h => h.LastReservationDate ?? DateTime.MinValue)
                .ThenBy(h => h.Name)
                .ToList(),
            "highest-rating" => model.Hotels
                .OrderByDescending(h => h.Rating)
                .ThenByDescending(h => h.ReviewCount)
                .ThenBy(h => h.Name)
                .ToList(),
            "review-waiting" => model.Hotels
                .OrderByDescending(h => h.ReviewPendingCount)
                .ThenByDescending(h => h.LastReservationDate ?? DateTime.MinValue)
                .ThenBy(h => h.Name)
                .ToList(),
            "newest-favorite" => model.Hotels
                .OrderByDescending(h => h.FavoriteAddedAt)
                .ThenBy(h => h.Name)
                .ToList(),
            _ => model.Hotels
                .OrderByDescending(h => h.LastReservationDate ?? DateTime.MinValue)
                .ThenByDescending(h => h.FavoriteAddedAt)
                .ThenBy(h => h.Name)
                .ToList()
        };

        model.TotalCount = model.Hotels.Count;
        model.TotalPages = Math.Max(1, (int)Math.Ceiling(model.TotalCount / (double)model.PageSize));
        model.Page = Math.Min(model.Page, model.TotalPages);
        model.Hotels = model.Hotels
            .Skip((model.Page - 1) * model.PageSize)
            .Take(model.PageSize)
            .ToList();

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
                    if (hotel.StartingPrice is > 0m)
                    {
                        hotel.NightlyPrice = hotel.StartingPrice;
                        hotel.NightlyPriceText = $"TRY {hotel.StartingPrice.Value:N0}";
                        hotel.PriceText = hotel.NightlyPriceText;
                    }
                    else
                    {
                        hotel.NightlyPriceText = "Teklif Al";
                    }

                    hotel.PriceDateText = $"{today:dd MMMM yyyy} itibariyle";
                    continue;
                }

                hotel.StartingPrice = effectivePrice;
                hotel.NightlyPrice = effectivePrice;
                hotel.NightlyPriceText = $"TRY {effectivePrice:N0}";
                hotel.PriceText = hotel.NightlyPriceText;
                hotel.PriceDateText = $"{today:dd MMMM yyyy} itibariyle";
            }
        }

        return model;
    }

    private static string NormalizeFavoriteSort(string? sort)
        => (sort ?? string.Empty).Trim().ToLowerInvariant() switch
        {
            "most-reserved" => "most-reserved",
            "highest-rating" => "highest-rating",
            "review-waiting" => "review-waiting",
            "newest-favorite" => "newest-favorite",
            _ => "latest-reservation"
        };

    private static bool MatchesFavoriteSearch(UserFavoriteHotelCardViewModel hotel, string searchTerm)
    {
        var term = searchTerm.Trim();
        if (term.Length == 0)
        {
            return true;
        }

        if (ContainsIgnoreCase(hotel.Name, term)
            || ContainsIgnoreCase(hotel.City, term)
            || ContainsIgnoreCase(hotel.District, term))
        {
            return true;
        }

        foreach (var label in hotel.SmartRouteLabels)
        {
            if (ContainsIgnoreCase(label, term))
            {
                return true;
            }
        }

        return false;
    }

    private static bool ContainsIgnoreCase(string? source, string term)
        => !string.IsNullOrWhiteSpace(source)
           && source.Contains(term, StringComparison.OrdinalIgnoreCase);

    private async Task EnrichFavoriteSmartRoutesAsync(
        SqlConnection connection,
        List<UserFavoriteHotelCardViewModel> hotels,
        CancellationToken cancellationToken)
    {
        if (hotels.Count == 0)
        {
            return;
        }

        if (!await TableExistsAsync(connection, "AKILLI_ROTA_OTELLER", cancellationToken)
            || !await TableExistsAsync(connection, "AKILLI_ROTA", cancellationToken))
        {
            return;
        }

        var hotelIds = hotels.Select(h => h.HotelId).Distinct().ToArray();
        var parameters = string.Join(", ", hotelIds.Select((_, index) => $"@hotelId{index}"));
        var sql = $@"
            SELECT
                aro.[OTEL_ID],
                ar.[ETIKET_ADI],
                COALESCE(ar.[HASHTAG], '') AS [HASHTAG],
                ar.[ETIKET_KODU],
                COALESCE(ar.[ARAMA_METNI], '') AS [ARAMA_METNI]
            FROM [dbo].[AKILLI_ROTA_OTELLER] aro
            INNER JOIN [dbo].[AKILLI_ROTA] ar ON ar.[ID] = aro.[AKILLI_ROTA_ID]
            WHERE aro.[OTEL_ID] IN ({parameters})
              AND aro.[AKTIF_MI] = 1
              AND (aro.[BITIS_TARIHI] IS NULL OR aro.[BITIS_TARIHI] > SYSUTCDATETIME())
              AND ar.[AKTIF_MI] = 1
            ORDER BY ar.[SIRA_NO], ar.[ETIKET_ADI];";

        var routeMap = hotels.ToDictionary(h => h.HotelId, _ => new List<string>());

        await using var command = new SqlCommand(sql, connection);
        for (var i = 0; i < hotelIds.Length; i++)
        {
            command.Parameters.AddWithValue($"@hotelId{i}", hotelIds[i]);
        }

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var hotelId = reader.GetInt64(0);
            if (!routeMap.TryGetValue(hotelId, out var labels))
            {
                continue;
            }

            var displayName = reader.IsDBNull(1) ? string.Empty : reader.GetString(1).Trim();
            var hashtag = reader.IsDBNull(2) ? string.Empty : reader.GetString(2).Trim().TrimStart('#');
            var slug = reader.IsDBNull(3) ? string.Empty : reader.GetString(3).Trim();
            var searchText = reader.IsDBNull(4) ? string.Empty : reader.GetString(4).Trim();

            AddRouteLabel(labels, displayName);
            AddRouteLabel(labels, hashtag);
            AddRouteLabel(labels, slug);
            AddRouteLabel(labels, slug.Replace('-', ' '));
            foreach (var part in searchText.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                AddRouteLabel(labels, part);
            }
        }

        foreach (var hotel in hotels)
        {
            if (routeMap.TryGetValue(hotel.HotelId, out var labels))
            {
                hotel.SmartRouteLabels = labels;
            }
        }
    }

    private static void AddRouteLabel(List<string> labels, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        var trimmed = value.Trim();
        if (labels.Any(existing => string.Equals(existing, trimmed, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        labels.Add(trimmed);
    }

    private static async Task<bool> TableExistsAsync(SqlConnection connection, string tableName, CancellationToken cancellationToken)
    {
        const string sql = "SELECT CASE WHEN OBJECT_ID(@tableName, 'U') IS NOT NULL THEN 1 ELSE 0 END;";
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@tableName", $"dbo.{tableName}");
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result, CultureInfo.InvariantCulture) == 1;
    }

    public async Task<OtelFavoriToggleYanit> ToggleFavoriteAsync(long userId, long hotelId, string sourcePage, string sourceUrl, string? deviceType, string? ipAddress, CancellationToken cancellationToken = default)
    {
        if (userId <= 0 || hotelId <= 0)
        {
            return new OtelFavoriToggleYanit { Success = false, Message = "Favori işlemi için geçerli kullanıcı ve otel bilgisi gereklidir." };
        }

        var connectionString = _configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return new OtelFavoriToggleYanit { Success = false, Message = "Veritabanı bağlantısı bulunamadı." };
        }

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        if (!await HotelExistsAsync(connection, hotelId, cancellationToken))
        {
            return new OtelFavoriToggleYanit { Success = false, Message = "Favorilere eklenecek otel bulunamadı." };
        }

        var hasHotelFavoriteCounter = await ColumnExistsAsync(connection, "oteller", "favori_sayisi", cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        try
        {

        const string selectSql = @"
            SELECT TOP (1)
                id,
                CASE
                    WHEN COALESCE(CONVERT(int, [AKTIF_MI]), 1) = 1 THEN 1
                    ELSE 0
                END AS [AKTIF_MI]
            FROM [dbo].[KULLANICI_FAVORI_OTELLER]
            WHERE [KULLANICI_ID] = @userId
              AND [OTEL_ID] = @hotelId
            ORDER BY id DESC;";
        await using var selectCommand = new SqlCommand(selectSql, connection, (SqlTransaction)transaction);
        selectCommand.Parameters.AddWithValue("@userId", userId);
        selectCommand.Parameters.AddWithValue("@hotelId", hotelId);

        long? recordId = null;
        var isActive = false;
        await using (var reader = await selectCommand.ExecuteReaderAsync(cancellationToken))
        {
            if (await reader.ReadAsync(cancellationToken))
            {
                recordId = reader.GetInt64(reader.GetOrdinal("id"));
                isActive = Convert.ToInt32(reader.GetValue(reader.GetOrdinal("aktif_mi")), CultureInfo.InvariantCulture) == 1;
            }
        }

        var normalizedSourcePage = SafeTrim(sourcePage, 100);
        var normalizedSourceUrl = SafeTrim(sourceUrl, 500);
        var normalizedDeviceType = SafeTrim(deviceType, 50);
        var normalizedIp = SafeTrim(ipAddress, 45);

        if (!recordId.HasValue)
        {
            const string insertSql = @"
                INSERT INTO [dbo].[KULLANICI_FAVORI_OTELLER]
                ([KULLANICI_ID], [OTEL_ID], [KAYNAK_SAYFA], [KAYNAK_URL], [CIHAZ_TIPI], [IP_ADRESI], [AKTIF_MI], [OLUSTURULMA_TARIHI], [SON_ISLEM_TARIHI])
                VALUES
                (@userId, @hotelId, @sourcePage, @sourceUrl, @deviceType, @ipAddress, 1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);";

            await using var insertCommand = new SqlCommand(insertSql, connection, (SqlTransaction)transaction);
            insertCommand.Parameters.AddWithValue("@userId", userId);
            insertCommand.Parameters.AddWithValue("@hotelId", hotelId);
            insertCommand.Parameters.AddWithValue("@sourcePage", DbValue(normalizedSourcePage));
            insertCommand.Parameters.AddWithValue("@sourceUrl", DbValue(normalizedSourceUrl));
            insertCommand.Parameters.AddWithValue("@deviceType", DbValue(normalizedDeviceType));
            insertCommand.Parameters.AddWithValue("@ipAddress", DbValue(normalizedIp));
            await insertCommand.ExecuteNonQueryAsync(cancellationToken);
            if (hasHotelFavoriteCounter) await RefreshHotelFavoriteCounterAsync(connection, (SqlTransaction)transaction, hotelId, cancellationToken);

            await transaction.CommitAsync(cancellationToken);
            return new OtelFavoriToggleYanit { Success = true, IsFavorite = true, Message = "Otel favorilerinize eklendi." };
        }

        const string updateSql = @"
            UPDATE [dbo].[KULLANICI_FAVORI_OTELLER]
            SET [KAYNAK_SAYFA] = @sourcePage,
                [KAYNAK_URL] = @sourceUrl,
                [CIHAZ_TIPI] = @deviceType,
                [IP_ADRESI] = @ipAddress,
                [AKTIF_MI] = @isFavorite,
                [KALDIRILMA_TARIHI] = CASE WHEN @isFavorite = 1 THEN NULL ELSE CURRENT_TIMESTAMP END,
                [SON_ISLEM_TARIHI] = CURRENT_TIMESTAMP
            WHERE id = @id;";

        await using var updateCommand = new SqlCommand(updateSql, connection, (SqlTransaction)transaction);
        updateCommand.Parameters.AddWithValue("@id", recordId.Value);
        updateCommand.Parameters.AddWithValue("@sourcePage", DbValue(normalizedSourcePage));
        updateCommand.Parameters.AddWithValue("@sourceUrl", DbValue(normalizedSourceUrl));
        updateCommand.Parameters.AddWithValue("@deviceType", DbValue(normalizedDeviceType));
        updateCommand.Parameters.AddWithValue("@ipAddress", DbValue(normalizedIp));
        updateCommand.Parameters.AddWithValue("@isFavorite", isActive ? 0 : 1);
        await updateCommand.ExecuteNonQueryAsync(cancellationToken);
        if (hasHotelFavoriteCounter) await RefreshHotelFavoriteCounterAsync(connection, (SqlTransaction)transaction, hotelId, cancellationToken);

        if (isActive)
        {
            const string disableAlertSql = @"
                UPDATE [dbo].[KULLANICI_FAVORI_FIYAT_ALARMLARI]
                SET [AKTIF_MI] = 0,
                    [GUNCELLENME_TARIHI] = CURRENT_TIMESTAMP
                WHERE [KULLANICI_ID] = @userId
                  AND [OTEL_ID] = @hotelId;";
            await using var disableAlertCommand = new SqlCommand(disableAlertSql, connection, (SqlTransaction)transaction);
            disableAlertCommand.Parameters.AddWithValue("@userId", userId);
            disableAlertCommand.Parameters.AddWithValue("@hotelId", hotelId);
            await disableAlertCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
        return new OtelFavoriToggleYanit
        {
            Success = true,
            IsFavorite = !isActive,
            Message = isActive ? "Otel favorilerinizden çıkarıldı." : "Otel favorilerinize yeniden eklendi."
        };
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
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
            FROM [dbo].[KULLANICI_FAVORI_OTELLER]
            WHERE [KULLANICI_ID] = @userId
              AND [OTEL_ID] = @hotelId
              AND COALESCE([AKTIF_MI], 1) = 1;";
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
                UPDATE [dbo].[KULLANICI_FAVORI_FIYAT_ALARMLARI]
                SET [AKTIF_MI] = 0,
                    [GUNCELLENME_TARIHI] = CURRENT_TIMESTAMP
                WHERE [KULLANICI_ID] = @userId
                  AND [OTEL_ID] = @hotelId;";
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
            MERGE [dbo].[KULLANICI_FAVORI_FIYAT_ALARMLARI] AS target
            USING (SELECT @userId AS [KULLANICI_ID], @hotelId AS [OTEL_ID]) AS source
            ON target.[KULLANICI_ID] = source.[KULLANICI_ID] AND target.[OTEL_ID] = source.[OTEL_ID]
            WHEN MATCHED THEN
                UPDATE SET
                    [HEDEF_MAKSIMUM_FIYAT] = @targetPrice,
                    [BASLANGIC_TARIHI] = @startDate,
                    [BITIS_TARIHI] = @endDate,
                    [AKTIF_MI] = 1,
                    [GUNCELLENME_TARIHI] = CURRENT_TIMESTAMP
            WHEN NOT MATCHED THEN
                INSERT ([KULLANICI_ID], [OTEL_ID], [HEDEF_MAKSIMUM_FIYAT], [BASLANGIC_TARIHI], [BITIS_TARIHI], [AKTIF_MI], [OLUSTURULMA_TARIHI], [GUNCELLENME_TARIHI])
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
            DELETE FROM [dbo].[KULLANICI_FAVORI_FIYAT_ALARMLARI]
            WHERE [KULLANICI_ID] = @userId
              AND [OTEL_ID] = @hotelId;";
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
        const string sql = @"SELECT COUNT(*) FROM [dbo].[OTELLER] WHERE id = @hotelId;";
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@hotelId", hotelId);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result, CultureInfo.InvariantCulture) > 0;
    }

    private static string? BuildAvailabilityNote(string? approvalStatus, string? publishStatus)
    {
        var normalizedApproval = (approvalStatus ?? string.Empty).Trim();
        var normalizedPublish = (publishStatus ?? string.Empty).Trim();

        if (string.Equals(normalizedApproval, "Onaylandı", StringComparison.OrdinalIgnoreCase)
            && string.Equals(normalizedPublish, "Yayında", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (!string.Equals(normalizedApproval, "Onaylandı", StringComparison.OrdinalIgnoreCase))
        {
            return "Bu otel şu anda onay sürecinde. Favorinizde kalır, yayına alındığında tekrar inceleyebilirsiniz.";
        }

        if (!string.Equals(normalizedPublish, "Yayında", StringComparison.OrdinalIgnoreCase))
        {
            return "Bu otel şu anda yayında değil. Favorinizde saklanmaya devam eder.";
        }

        return null;
    }

    private static object DbValue(string? value) => string.IsNullOrWhiteSpace(value) ? DBNull.Value : value.Trim();

    private static async Task<bool> ColumnExistsAsync(SqlConnection connection, string tableName, string columnName, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT COUNT(*)
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_CATALOG = DB_NAME()
              AND TABLE_NAME = @tableName
              AND COLUMN_NAME = @columnName;";

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@tableName", tableName);
        command.Parameters.AddWithValue("@columnName", columnName);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result, CultureInfo.InvariantCulture) > 0;
    }

    private static async Task RefreshHotelFavoriteCounterAsync(SqlConnection connection, SqlTransaction transaction, long hotelId, CancellationToken cancellationToken)
    {
        const string sql = @"
            UPDATE o
            SET o.[FAVORI_SAYISI] = src.toplam
            FROM [dbo].[OTELLER] o
            CROSS APPLY
            (
                SELECT COUNT(DISTINCT uf.[KULLANICI_ID]) AS toplam
                FROM [dbo].[KULLANICI_FAVORI_OTELLER] uf
                WHERE uf.[OTEL_ID] = @hotelId
                  AND COALESCE(uf.[AKTIF_MI], 1) = 1
                  AND uf.[KALDIRILMA_TARIHI] IS NULL
            ) src
            WHERE o.id = @hotelId;";

        await using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("@hotelId", hotelId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

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
