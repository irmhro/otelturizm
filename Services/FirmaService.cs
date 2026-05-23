using System.Globalization;
using Microsoft.Data.SqlClient;
using SqlConnection = Microsoft.Data.SqlClient.SqlConnection;
using SqlCommand = Microsoft.Data.SqlClient.SqlCommand;
using SqlTransaction = Microsoft.Data.SqlClient.SqlTransaction;
using SqlException = Microsoft.Data.SqlClient.SqlException;
using otelturizmnew.Models.Firma;
using otelturizmnew.Models.Messages;
using otelturizmnew.Models.Paneller.Firma;
using System.Text.Json;
using otelturizmnew.Models.Reservations;
using otelturizmnew.Services.Abstractions;

namespace otelturizmnew.Services;

public class FirmaService : IFirmaService
{
    private readonly string _connectionString;
    private readonly IMessageCenterService _messageCenterService;
    private readonly IEmailQueueService _emailQueueService;
    private readonly ILogger<FirmaService> _logger;

    public FirmaService(IConfiguration configuration, IMessageCenterService messageCenterService, IEmailQueueService emailQueueService, ILogger<FirmaService> logger)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection tanimli degil.");
        _messageCenterService = messageCenterService;
        _emailQueueService = emailQueueService;
        _logger = logger;
    }

    public async Task<FirmaLandingPageViewModel> GetLandingPageAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var model = new FirmaLandingPageViewModel
        {
            HeroDescription = "5 ve üzeri oda ihtiyacınızda, otellerin firmalar için özel belirlediği indirimli fiyatlardan yararlanın. Tek fatura, çalışan limitleri ve detaylı raporlarla seyahat operasyonunuzu kontrol edin."
        };

        const string summarySql = @"
            SELECT
                (SELECT COUNT(*) FROM [dbo].[FIRMALAR] WHERE [AKTIF_MI] = 1 AND [ONAY_DURUMU] = 'Onaylandı') AS active_companies,
                (SELECT COUNT(DISTINCT [OTEL_ID]) FROM [dbo].[FIRMA_ODA_FIYAT_MUSAITLIK] WHERE [AKTIF_MI] = 1 AND [KAPALI_SATIS] = 0) AS contracted_hotels,
                (
                    SELECT COALESCE(MAX(
                        CASE
                            WHEN std.base_price > 0 AND f.[FIRMA_GECELIK_FIYAT] > 0 AND f.[FIRMA_GECELIK_FIYAT] < std.base_price
                            THEN ((std.base_price - f.[FIRMA_GECELIK_FIYAT]) / std.base_price) * 100
                            ELSE 0
                        END
                    ), 0)
                    FROM [dbo].[FIRMA_ODA_FIYAT_MUSAITLIK] f
                    OUTER APPLY (
                        SELECT TOP (1)
                            CASE
                                WHEN ofm.[INDIRIMLI_FIYAT] IS NOT NULL AND ofm.[INDIRIMLI_FIYAT] > 0 AND ofm.[INDIRIMLI_FIYAT] < ofm.[GECELIK_FIYAT] THEN ofm.[INDIRIMLI_FIYAT]
                                ELSE ofm.[GECELIK_FIYAT]
                            END AS base_price
                        FROM [dbo].[ODA_FIYAT_MUSAITLIK] ofm
                        WHERE ofm.[OTEL_ID] = f.[OTEL_ID]
                          AND ofm.[ODA_TIP_ID] = f.[ODA_TIP_ID]
                          AND ofm.[TARIH] = f.[TARIH]
                    ) std
                    WHERE f.[AKTIF_MI] = 1 AND f.[KAPALI_SATIS] = 0
                ) AS max_discount;";

        await using (var summaryCommand = new SqlCommand(summarySql, connection))
        await using (var reader = await summaryCommand.ExecuteReaderAsync(cancellationToken))
        {
            if (await reader.ReadAsync(cancellationToken))
            {
                model.ActiveCompanyCount = SafeInt(reader, 0);
                model.ContractedHotelCount = SafeInt(reader, 1);
                model.MaxDiscountRate = SafeDecimal(reader, 2);
            }
        }

        model.HeroStats = new List<FirmaLandingStatViewModel>
        {
            new() { Value = model.ContractedHotelCount.ToString("N0", CultureInfo.GetCultureInfo("tr-TR")), Label = "Anlaşmalı Otel" },
            new() { Value = $"%{model.MaxDiscountRate:0}", Label = "İndirim Tavanı" },
            new() { Value = model.ActiveCompanyCount.ToString("N0", CultureInfo.GetCultureInfo("tr-TR")), Label = "Kayıtlı Firma" }
        };

        const string dealsSql = @"
            WITH deals AS (
                SELECT
                    f.[OTEL_ID],
                    f.[ODA_TIP_ID],
                    MIN(CASE WHEN f.[FIRMA_GECELIK_FIYAT] > 0 THEN f.[FIRMA_GECELIK_FIYAT] ELSE NULL END) AS corporate_price,
                    MIN(COALESCE(std.base_price, 0)) AS standard_price
                FROM [dbo].[FIRMA_ODA_FIYAT_MUSAITLIK] f
                OUTER APPLY (
                    SELECT TOP (1)
                        CASE
                            WHEN ofm.[INDIRIMLI_FIYAT] IS NOT NULL AND ofm.[INDIRIMLI_FIYAT] > 0 AND ofm.[INDIRIMLI_FIYAT] < ofm.[GECELIK_FIYAT] THEN ofm.[INDIRIMLI_FIYAT]
                            ELSE ofm.[GECELIK_FIYAT]
                        END AS base_price
                    FROM [dbo].[ODA_FIYAT_MUSAITLIK] ofm
                    WHERE ofm.[OTEL_ID] = f.[OTEL_ID]
                      AND ofm.[ODA_TIP_ID] = f.[ODA_TIP_ID]
                      AND ofm.[TARIH] = f.[TARIH]
                ) std
                WHERE f.[AKTIF_MI] = 1 AND f.[KAPALI_SATIS] = 0
                GROUP BY f.[OTEL_ID], f.[ODA_TIP_ID]
            )
            SELECT TOP (6) ot.id, ot.[OTEL_ADI], ot.[SEHIR], d.standard_price, d.corporate_price,
                   CASE
                       WHEN d.standard_price > 0 AND d.corporate_price > 0 AND d.corporate_price < d.standard_price
                       THEN ((d.standard_price - d.corporate_price) / d.standard_price) * 100
                       ELSE 0
                   END AS discount_rate,
                   1 AS minimum_room_count
            FROM deals d
            INNER JOIN [dbo].[OTELLER] ot ON ot.id = d.[OTEL_ID]
            ORDER BY discount_rate DESC, d.corporate_price ASC;";

        await using (var dealsCommand = new SqlCommand(dealsSql, connection))
        await using (var reader = await dealsCommand.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                var standard = SafeDecimal(reader, 3);
                var corporate = SafeDecimal(reader, 4);
                model.FeaturedDeals.Add(new FirmaLandingDealViewModel
                {
                    HotelId = reader.GetInt64(0),
                    HotelName = reader.GetString(1),
                    City = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                    StandardPriceText = FormatMoney(standard),
                    CorporatePriceText = FormatMoney(corporate),
                    DiscountRateText = $"%{SafeDecimal(reader, 5):0}",
                    MinRoomText = $"{SafeInt(reader, 6)} oda",
                    SavingsText = FormatMoney(Math.Max(0, standard - corporate))
                });
            }
        }

        model.PricingTiers = new List<FirmaLandingTierViewModel>
        {
            new() { Name = "Operasyon Paketi", Range = "Minimum 2+ Oda", DiscountText = "Örnek %20 - %18", ExampleText = "Kısa iş seyahatleri için oranlar otel planlamasına göre değişir; panelde şeffaf olarak gösterilir." },
            new() { Name = "Büyüme Paketi", Range = "Minimum 5+ Oda", DiscountText = "Örnek %18 - %15", ExampleText = "Düzenli ekip konaklamasında nihai oranlar sezon ve otelin kurumsal planına göre belirlenir.", Highlighted = true },
            new() { Name = "Kurumsal Plus", Range = "Minimum 10+ Oda", DiscountText = "Örnek %15 - %10", ExampleText = "Yoğun kullanımda nihai indirim, otelin onayladığı özel kurumsal teklife göre netleşir." }
        };

        model.Benefits = new List<FirmaLandingBenefitViewModel>
        {
            new() { Title = "Firma bazlı özel fiyatlar", Description = "Her otelin firmalara özel minimum oda ve indirim tanımı canlı veritabanından okunur.", IconClass = "fa-tags" },
            new() { Title = "Çalışan limit yönetimi", Description = "Departman ve kullanıcı bazlı harcama limitlerini firma panelinden kontrol edin.", IconClass = "fa-users-gear" },
            new() { Title = "Tek merkezden fatura takibi", Description = "Kurumsal faturaları ve rezervasyon finans hareketlerini tek panelde görün.", IconClass = "fa-file-invoice" },
            new() { Title = "Harcama ve otel bazlı rapor", Description = "Aylık toplam spend ve otel performansını karşılaştırmalı izleyin.", IconClass = "fa-chart-column" }
        };

        return model;
    }

    public async Task<FirmaDashboardPageViewModel> GetDashboardAsync(long userId, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        var context = await BuildContextAsync(connection, userId, "Dashboard", "Kurumsal fiyatları, çalışanları ve seyahat bütçesini tek panelden yönetin.", "dashboard", cancellationToken);

        var model = new FirmaDashboardPageViewModel { Shell = context.Shell };

        const string statsSql = @"
            SELECT
                (SELECT COUNT(*) FROM [dbo].[KULLANICILAR] u WHERE u.[FIRMA_ID] = @firmaId AND u.[ROL] LIKE 'firma_%' AND u.[HESAP_DURUMU] = 1) AS employee_count,
                (SELECT COUNT(DISTINCT CONCAT(f.[OTEL_ID], ':', f.[ODA_TIP_ID])) FROM [dbo].[FIRMA_ODA_FIYAT_MUSAITLIK] f WHERE f.[AKTIF_MI] = 1 AND f.[KAPALI_SATIS] = 0) AS deal_count,
                (SELECT COUNT(*) FROM [dbo].[REZERVASYONLAR] r WHERE r.[FIRMA_ID] = @firmaId) AS reservation_count,
                (SELECT COUNT(*) FROM [dbo].[REZERVASYONLAR] r WHERE r.[FIRMA_ID] = @firmaId AND r.[FIRMA_ONAY_DURUMU] = 'Beklemede') AS pending_approval_count;";

        await using (var command = new SqlCommand(statsSql, connection))
        {
            command.Parameters.AddWithValue("@firmaId", context.FirmaId);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                model.SummaryCards.Add(new FirmaPanelStatCardViewModel { Label = "Çalışan", Value = SafeInt(reader, 0).ToString(), Description = "Aktif firma kullanıcıları", IconClass = "fa-users", ToneClass = "info" });
                model.SummaryCards.Add(new FirmaPanelStatCardViewModel { Label = "Özel Fiyat", Value = SafeInt(reader, 1).ToString(), Description = "Kurumsal anlaşmalı fiyat kartı", IconClass = "fa-tags", ToneClass = "success" });
                model.SummaryCards.Add(new FirmaPanelStatCardViewModel { Label = "Rezervasyon", Value = SafeInt(reader, 2).ToString(), Description = "Firma adına açılmış kayıtlar", IconClass = "fa-calendar-check", ToneClass = "warning" });
                model.SummaryCards.Add(new FirmaPanelStatCardViewModel { Label = "Onay Bekleyen", Value = SafeInt(reader, 3).ToString(), Description = "Firma onay akışında bekleyen işlem", IconClass = "fa-hourglass-half", ToneClass = "danger" });
            }
        }

        model.HighlightDeals = await LoadDealsAsync(connection, context.FirmaId, 4, null, null, null, null, null, cancellationToken);
        model.FeaturedEmployees = await LoadEmployeesAsync(connection, context.FirmaId, 4, cancellationToken);
        model.RecentReservations = await LoadReservationsAsync(connection, context.FirmaId, 6, cancellationToken);
        await LoadDashboardExtrasAsync(connection, context.FirmaId, model, cancellationToken);
        return model;
    }

    public async Task<FirmaDealsPageViewModel> GetDealsAsync(long userId, string? city = null, string? district = null, string? neighborhood = null, int? minRoomCount = null, string? search = null, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        var context = await BuildContextAsync(connection, userId, "Firma Fiyatları", "Otellerin firmanız için tanımladığı özel kurumsal fiyatları canlı takip edin.", "deals", cancellationToken);
        var deals = await LoadDealsAsync(connection, context.FirmaId, 100, city, district, neighborhood, minRoomCount, search, cancellationToken);
        var hotelOptions = deals
            .GroupBy(x => x.HotelId)
            .Select(x => new FirmaDealHotelOptionViewModel
            {
                HotelId = x.Key,
                Label = $"{x.First().HotelName} · {x.First().CityText}"
            })
            .OrderBy(x => x.Label, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new FirmaDealsPageViewModel
        {
            Shell = context.Shell,
            Deals = deals,
            Filter = new FirmaDealsFilterModel { City = city, District = district, Neighborhood = neighborhood, MinRoomCount = minRoomCount, Search = search },
            AvailableCities = await LoadDealCitiesAsync(connection, context.FirmaId, cancellationToken),
            AvailableDistricts = await LoadDealDistrictsAsync(connection, context.FirmaId, city, cancellationToken),
            AvailableNeighborhoods = await LoadDealNeighborhoodsAsync(connection, context.FirmaId, city, district, cancellationToken),
            HotelOptions = hotelOptions
        };
    }

    public async Task<FirmaDealsComparePageViewModel> GetDealsCompareAsync(long userId, IReadOnlyList<long> hotelIds, int roomCount, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        var context = await BuildContextAsync(connection, userId, "Fiyat Karşılaştır", "Seçtiğiniz otellerdeki kurumsal fiyatları yan yana karşılaştırın.", "deals", cancellationToken);

        var normalized = (hotelIds ?? Array.Empty<long>())
            .Where(x => x > 0)
            .Distinct()
            .Take(3)
            .ToList();

        var model = new FirmaDealsComparePageViewModel
        {
            Shell = context.Shell,
            RoomCount = Math.Clamp(roomCount, 1, 50),
            Hint = "2-3 otel seçerek oda tiplerine göre kurumsal fiyatları kıyaslayabilirsiniz."
        };

        if (normalized.Count < 2)
        {
            model.Hint = "Karşılaştırma için en az 2 otel seçmelisiniz.";
            return model;
        }

        // Hotels header info
        const string hotelsSql = @"
            SELECT id, [OTEL_ADI], CONCAT(COALESCE(ilce, N''), CASE WHEN COALESCE(ilce, N'') <> '' THEN N', ' ELSE N'' END, COALESCE([SEHIR], N'')) AS city_text
            FROM [dbo].[OTELLER]
            WHERE id IN (SELECT value FROM OPENJSON(@ids));";
        await using (var cmd = new SqlCommand(hotelsSql, connection))
        {
            cmd.Parameters.AddWithValue("@ids", JsonSerializer.Serialize(normalized));
            await using var r = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await r.ReadAsync(cancellationToken))
            {
                model.Hotels.Add(new FirmaDealsCompareHotelViewModel
                {
                    HotelId = r.GetInt64(0),
                    HotelName = r.GetString(1),
                    CityText = r.IsDBNull(2) ? string.Empty : r.GetString(2)
                });
            }
        }

        // Compare rows: corp min nightly vs standard min nightly per hotel+roomType
        const string compareSql = @"
            WITH sel AS (
                SELECT value AS [OTEL_ID]
                FROM OPENJSON(@ids)
            ),
            corp AS (
                SELECT f.[OTEL_ID], f.[ODA_TIP_ID],
                       MIN(CASE WHEN f.[FIRMA_GECELIK_FIYAT] > 0 THEN f.[FIRMA_GECELIK_FIYAT] ELSE NULL END) AS corp_price,
                       MIN(f.[TARIH]) AS min_date,
                       MAX(f.[TARIH]) AS max_date
                FROM [dbo].[FIRMA_ODA_FIYAT_MUSAITLIK] f
                INNER JOIN sel s ON s.[OTEL_ID] = f.[OTEL_ID]
                WHERE f.[AKTIF_MI] = 1
                  AND f.[KAPALI_SATIS] = 0
                GROUP BY f.[OTEL_ID], f.[ODA_TIP_ID]
            ),
            std AS (
                SELECT ofm.[OTEL_ID], ofm.[ODA_TIP_ID],
                       MIN(
                            CASE
                                WHEN ofm.[INDIRIMLI_FIYAT] IS NOT NULL AND ofm.[INDIRIMLI_FIYAT] > 0 AND ofm.[INDIRIMLI_FIYAT] < ofm.[GECELIK_FIYAT] THEN ofm.[INDIRIMLI_FIYAT]
                                ELSE ofm.[GECELIK_FIYAT]
                            END
                       ) AS std_price
                FROM [dbo].[ODA_FIYAT_MUSAITLIK] ofm
                INNER JOIN sel s ON s.[OTEL_ID] = ofm.[OTEL_ID]
                GROUP BY ofm.[OTEL_ID], ofm.[ODA_TIP_ID]
            )
            SELECT c.[OTEL_ID], c.[ODA_TIP_ID], COALESCE(od.[ODA_ADI], N'Oda') AS room_name,
                   COALESCE(od.[MAKSIMUM_KISI_SAYISI], 0) AS max_guest,
                   COALESCE(od.[MAKSIMUM_YETISKIN_SAYISI], 0) AS max_adult,
                   COALESCE(od.[MAKSIMUM_COCUK_SAYISI], 0) AS max_child,
                   COALESCE(c.corp_price, 0) AS corp_price,
                   COALESCE(s.std_price, 0) AS std_price,
                   c.min_date, c.max_date
            FROM corp c
            LEFT JOIN std s ON s.[OTEL_ID] = c.[OTEL_ID] AND s.[ODA_TIP_ID] = c.[ODA_TIP_ID]
            LEFT JOIN [dbo].[ODA_TIPLERI] od ON od.id = c.[ODA_TIP_ID]
            ORDER BY c.[OTEL_ID] ASC, corp_price ASC;";

        var culture = CultureInfo.GetCultureInfo("tr-TR");
        await using (var cmd = new SqlCommand(compareSql, connection))
        {
            cmd.Parameters.AddWithValue("@ids", JsonSerializer.Serialize(normalized));
            await using var r = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await r.ReadAsync(cancellationToken))
            {
                var maxGuest = SafeInt(r, 3);
                var maxAdult = SafeInt(r, 4);
                var maxChild = SafeInt(r, 5);
                var corpPrice = SafeDecimal(r, 6);
                var stdPrice = SafeDecimal(r, 7);
                string discountText = "-";
                if (stdPrice > 0m && corpPrice > 0m && corpPrice < stdPrice)
                {
                    var pct = Math.Clamp((int)Math.Round(((stdPrice - corpPrice) / stdPrice) * 100m, MidpointRounding.AwayFromZero), 1, 95);
                    discountText = $"%{pct}";
                }

                string validityText;
                if (!r.IsDBNull(8) && !r.IsDBNull(9))
                {
                    validityText = $"{r.GetDateTime(8):dd.MM.yyyy} - {r.GetDateTime(9):dd.MM.yyyy}";
                }
                else
                {
                    validityText = "Tanımlı";
                }

                model.Rows.Add(new FirmaDealsCompareRowViewModel
                {
                    HotelId = r.GetInt64(0),
                    RoomTypeId = r.GetInt64(1),
                    RoomName = r.GetString(2),
                    CapacityText = maxGuest > 0 ? $"{maxGuest} kişi (Y{Math.Max(0, maxAdult)} / Ç{Math.Max(0, maxChild)})" : "-",
                    CorporateNightlyText = corpPrice > 0m ? corpPrice.ToString("N2", culture) : "-",
                    StandardNightlyText = stdPrice > 0m ? stdPrice.ToString("N2", culture) : "-",
                    DiscountText = discountText,
                    ValidityText = validityText
                });
            }
        }

        return model;
    }

    public async Task<FirmaReservationsPageViewModel> GetReservationsAsync(long userId, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        var context = await BuildContextAsync(connection, userId, "Rezervasyonlar", "Firma adına oluşturulan tüm konaklama kayıtlarını görün.", "reservations", cancellationToken);
        return new FirmaReservationsPageViewModel { Shell = context.Shell, Reservations = await LoadReservationsAsync(connection, context.FirmaId, 200, cancellationToken) };
    }

    public async Task<FirmaCreateReservationPageViewModel> GetCreateReservationAsync(
        long userId,
        long? hotelId = null,
        long? roomTypeId = null,
        string? search = null,
        DateOnly? checkIn = null,
        DateOnly? checkOut = null,
        int? roomCount = null,
        int? adultCount = null,
        int? childCount = null,
        long? employeeUserId = null,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        var context = await BuildContextAsync(connection, userId, "Yeni Rezervasyon", "Firma fiyatı ile standart fiyatı karşılaştırıp tek ekranda rezervasyon açın.", "create-reservation", cancellationToken);

        var model = new FirmaCreateReservationPageViewModel { Shell = context.Shell };
        model.Form.HotelId = hotelId.GetValueOrDefault();
        model.Form.RoomTypeId = roomTypeId.GetValueOrDefault();
        model.HotelSearch = search;
        if (checkIn.HasValue)
        {
            model.Form.CheckInDate = checkIn.Value;
        }
        if (checkOut.HasValue && checkOut.Value > model.Form.CheckInDate)
        {
            model.Form.CheckOutDate = checkOut.Value;
        }
        if (roomCount.HasValue && roomCount.Value > 0)
        {
            model.Form.RoomCount = Math.Clamp(roomCount.Value, 1, 50);
        }
        if (adultCount.HasValue && adultCount.Value > 0)
        {
            model.Form.AdultCount = adultCount.Value;
        }
        if (childCount.HasValue && childCount.Value >= 0)
        {
            model.Form.ChildCount = childCount.Value;
        }
        if (employeeUserId.HasValue && employeeUserId.Value > 0)
        {
            model.Form.EmployeeUserId = employeeUserId.Value;
        }

        model.Employees = (await LoadEmployeesAsync(connection, context.FirmaId, 200, cancellationToken))
            .Select(item => new FirmaEmployeeOptionViewModel
            {
                UserId = item.UserId,
                FullName = item.FullName,
                Email = item.Email,
                Department = item.Department,
                IsSelected = model.Form.EmployeeUserId.HasValue && model.Form.EmployeeUserId.Value == item.UserId
            })
            .ToList();

        // Hotels: approved/published + optional search (otel/sehir/ilce/mahalle)
        const string hotelsSql = @"
            SELECT TOP (300) id, CONCAT([OTEL_ADI], ' · ', ilce, ', ', [SEHIR])
            FROM [dbo].[OTELLER]
            WHERE [ONAY_DURUMU] = 'Onaylandı'
              AND [YAYIN_DURUMU] IN ('Yayında','Bakımda')
              AND (@q IS NULL OR @q = '' OR [OTEL_ADI] LIKE '%' + @q + '%' OR [SEHIR] LIKE '%' + @q + '%' OR ilce LIKE '%' + @q + '%' OR [MAHALLE] LIKE '%' + @q + '%')
            ORDER BY [ONE_CIKAN_OTEL] DESC, [OTEL_ADI] ASC;";
        await using (var cmd = new SqlCommand(hotelsSql, connection))
        {
            cmd.Parameters.AddWithValue("@q", string.IsNullOrWhiteSpace(search) ? (object)DBNull.Value : search.Trim());
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                model.Hotels.Add(new FirmaSelectOption { Value = reader.GetInt64(0), Label = reader.GetString(1) });
            }
        }

        if (model.Form.HotelId <= 0)
        {
            model.Form.HotelId = model.Hotels.FirstOrDefault()?.Value ?? 0;
        }

        // Rooms for selected hotel
        const string roomsSql = @"
            SELECT id, [ODA_ADI]
            FROM [dbo].[ODA_TIPLERI]
            WHERE [OTEL_ID] = @hotelId AND [AKTIF_MI] = 1
            ORDER BY [ODA_ADI];";
        await using (var roomCmd = new SqlCommand(roomsSql, connection))
        {
            roomCmd.Parameters.AddWithValue("@hotelId", model.Form.HotelId);
            await using var roomReader = await roomCmd.ExecuteReaderAsync(cancellationToken);
            while (await roomReader.ReadAsync(cancellationToken))
            {
                model.RoomTypes.Add(new FirmaSelectOption { Value = roomReader.GetInt64(0), Label = roomReader.GetString(1) });
            }
        }

        if (model.Form.RoomTypeId <= 0)
        {
            model.Form.RoomTypeId = model.RoomTypes.FirstOrDefault()?.Value ?? 0;
        }

        model.GuestPolicy = await LoadGuestPolicyAsync(connection, model.Form.RoomTypeId, model.Form.RoomCount, cancellationToken);
        model.Compare = await BuildPriceCompareAsync(connection, context.FirmaId, model.Form, cancellationToken);
        return model;
    }

    public async Task<(bool Success, string Message, long? ReservationId)> CreateReservationAsync(long userId, FirmaReservationCreateModel model, CancellationToken cancellationToken = default)
    {
        if (model.HotelId <= 0 || model.RoomTypeId <= 0)
        {
            return (false, "Otel ve oda tipi seçilmelidir.", null);
        }
        if (model.CheckOutDate <= model.CheckInDate)
        {
            return (false, "Çıkış tarihi giriş tarihinden sonra olmalıdır.", null);
        }
        if (model.RoomCount <= 0)
        {
            return (false, "Oda sayısı 1 veya daha büyük olmalıdır.", null);
        }
        if (model.AdultCount <= 0)
        {
            return (false, "En az 1 yetişkin girilmelidir.", null);
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        var context = await BuildContextAsync(connection, userId, string.Empty, string.Empty, "create-reservation", cancellationToken);

        var guestPolicy = await LoadGuestPolicyAsync(connection, model.RoomTypeId, model.RoomCount, cancellationToken);
        var guestValidation = ValidateGuestCounts(model, guestPolicy);
        if (!string.IsNullOrWhiteSpace(guestValidation))
        {
            return (false, guestValidation, null);
        }

        if (model.EmployeeUserId.HasValue && model.EmployeeUserId.Value > 0)
        {
            var employeeOk = await EmployeeBelongsToFirmaAsync(connection, context.FirmaId, model.EmployeeUserId.Value, cancellationToken);
            if (!employeeOk)
            {
                return (false, "Seçilen personel bu firmaya ait değil.", null);
            }
        }

        var compare = await BuildPriceCompareAsync(connection, context.FirmaId, model, cancellationToken);
        if (compare.CompanyTotal <= 0m)
        {
            return (false, "Fiyat hesaplanamadı. Tarih/oda seçimlerini kontrol edin.", null);
        }

        var reservationNo = await GenerateFirmaReservationNoAsync(connection, cancellationToken);

        await using var tx = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            // partner recipient email
            var partnerRecipient = await ResolvePartnerRecipientAsync(connection, (SqlTransaction)tx, model.HotelId, cancellationToken);

            const string insertSql = @"
                INSERT INTO [dbo].[REZERVASYONLAR]
                (
                    [REZERVASYON_NO], [OTEL_ID], [ODA_TIP_ID], [KULLANICI_ID],
                    [FIRMA_ID], [FIRMA_CALISAN_ID],
                    [MISAFIR_AD_SOYAD], [MISAFIR_EPOSTA], [MISAFIR_TELEFON], [MISAFIR_NOTU],
                    [GIRIS_TARIHI], [CIKIS_TARIHI], [YETISKIN_SAYISI], [COCUK_SAYISI], [ODA_SAYISI],
                    [GECELIK_FIYAT], [TOPLAM_ODA_TUTARI], [VERGI_TUTARI], [TOPLAM_TUTAR],
                    [DURUM], [REZERVASYON_DURUMU_ID], [ODEME_DURUMU], [OTEL_ONAY_DURUMU], [FIRMA_ONAY_DURUMU],
                    [KAYNAK], [REZERVASYON_KANALI], [MUSTERI_TALEP_NOTU]
                )
                VALUES
                (
                    @reservationNo, @hotelId, @roomTypeId, @userId,
                    @firmaId, @firmaEmployeeId,
                    @guestName, @guestEmail, @guestPhone, @note,
                    @checkIn, @checkOut, @adultCount, @childCount, @roomCount,
                    @nightlyPrice, @roomTotal, 0, @totalAmount,
                    'Onay Bekliyor', (SELECT TOP (1) id FROM [dbo].[REZERVASYON_DURUM_TANIMLARI] WHERE kod = N'{RezervasyonDurumKodlari.OnayBekliyor}'), 'Beklemede', 'Beklemede', 'Beklemede',
                    'Firma', 'Firma Paneli', @note
                );
                SELECT CAST(SCOPE_IDENTITY() AS bigint);";

            var guestName = await ResolveGuestNameAsync(connection, (SqlTransaction)tx, context.FirmaId, model.EmployeeUserId, cancellationToken);
            var (companyEmail, companyName) = await LoadCompanyContactAsync(connection, (SqlTransaction)tx, context.FirmaId, cancellationToken);
            var employeeEmail = model.EmployeeUserId.HasValue ? await ResolveEmployeeEmailAsync(connection, (SqlTransaction)tx, model.EmployeeUserId.Value, cancellationToken) : null;

            var guestEmail = string.IsNullOrWhiteSpace(employeeEmail) ? (companyEmail ?? string.Empty) : employeeEmail!;
            long reservationId;
            await using (var cmd = new SqlCommand(insertSql, connection, (SqlTransaction)tx))
            {
                cmd.Parameters.AddWithValue("@reservationNo", reservationNo);
                cmd.Parameters.AddWithValue("@hotelId", model.HotelId);
                cmd.Parameters.AddWithValue("@roomTypeId", model.RoomTypeId);
                cmd.Parameters.AddWithValue("@userId", userId);
                cmd.Parameters.AddWithValue("@firmaId", context.FirmaId);
                cmd.Parameters.AddWithValue("@firmaEmployeeId", model.EmployeeUserId.HasValue ? model.EmployeeUserId.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("@guestName", guestName);
                cmd.Parameters.AddWithValue("@guestEmail", guestEmail);
                cmd.Parameters.AddWithValue("@guestPhone", DBNull.Value);
                cmd.Parameters.AddWithValue("@note", string.IsNullOrWhiteSpace(model.Note) ? DBNull.Value : model.Note.Trim());
                cmd.Parameters.AddWithValue("@checkIn", model.CheckInDate.ToDateTime(TimeOnly.MinValue));
                cmd.Parameters.AddWithValue("@checkOut", model.CheckOutDate.ToDateTime(TimeOnly.MinValue));
                cmd.Parameters.AddWithValue("@adultCount", model.AdultCount);
                cmd.Parameters.AddWithValue("@childCount", model.ChildCount);
                cmd.Parameters.AddWithValue("@roomCount", model.RoomCount);
                cmd.Parameters.AddWithValue("@nightlyPrice", compare.CompanyTotal / Math.Max(1, (model.CheckOutDate.DayNumber - model.CheckInDate.DayNumber) * model.RoomCount));
                cmd.Parameters.AddWithValue("@roomTotal", compare.CompanyTotal);
                cmd.Parameters.AddWithValue("@totalAmount", compare.CompanyTotal);
                reservationId = Convert.ToInt64(await cmd.ExecuteScalarAsync(cancellationToken), CultureInfo.InvariantCulture);
            }

            // Email queue: company + employees + partner
            var tokenMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["booking_reference"] = reservationNo,
                ["hotel_name"] = await LoadHotelNameAsync(connection, (SqlTransaction)tx, model.HotelId, cancellationToken),
                ["check_in_date"] = model.CheckInDate.ToString("dd MMM yyyy", CultureInfo.GetCultureInfo("tr-TR")),
                ["check_out_date"] = model.CheckOutDate.ToString("dd MMM yyyy", CultureInfo.GetCultureInfo("tr-TR")),
                ["total_price"] = compare.CompanyTotal.ToString("N2", CultureInfo.GetCultureInfo("tr-TR")),
                ["company_name"] = companyName ?? "Firma"
            };

            if (!string.IsNullOrWhiteSpace(companyEmail))
            {
                await _emailQueueService.QueueTemplateAsync(connection, (SqlTransaction)tx, new otelturizmnew.Models.Email.QueuedEmailTemplateRequest
                {
                    UserId = userId,
                    RecipientEmail = companyEmail,
                    TemplateCode = "firma_reservation_created_company",
                    RelatedTable = "rezervasyonlar",
                    RelatedRecordId = reservationId,
                    Tokens = tokenMap
                }, cancellationToken);
            }

            foreach (var emailItem in SplitEmails(model.EmployeeEmailsCsv).Distinct(StringComparer.OrdinalIgnoreCase))
            {
                await _emailQueueService.QueueTemplateAsync(connection, (SqlTransaction)tx, new otelturizmnew.Models.Email.QueuedEmailTemplateRequest
                {
                    UserId = userId,
                    RecipientEmail = emailItem,
                    TemplateCode = "firma_reservation_created_company",
                    RelatedTable = "rezervasyonlar",
                    RelatedRecordId = reservationId,
                    Tokens = tokenMap
                }, cancellationToken);
            }

            await _emailQueueService.QueueTemplateAsync(connection, (SqlTransaction)tx, new otelturizmnew.Models.Email.QueuedEmailTemplateRequest
            {
                UserId = partnerRecipient.UserId,
                RecipientEmail = partnerRecipient.Email,
                TemplateCode = "firma_reservation_created_partner",
                RelatedTable = "rezervasyonlar",
                RelatedRecordId = reservationId,
                Tokens = new Dictionary<string, string>(tokenMap, StringComparer.OrdinalIgnoreCase)
                {
                    ["hotel_manager_name"] = "Partner Yetkilisi"
                }
            }, cancellationToken);

            await tx.CommitAsync(cancellationToken);
            _logger.LogInformation(
                "RESERVATION_AUDIT create source=firma userId={UserId} firmaId={FirmaId} reservationId={ReservationId} reservationNo={ReservationNo} hotelId={HotelId} roomTypeId={RoomTypeId} total={Total} rooms={Rooms}",
                userId,
                context.FirmaId,
                reservationId,
                reservationNo,
                model.HotelId,
                model.RoomTypeId,
                compare.CompanyTotal,
                model.RoomCount);
            return (true, $"Firma rezervasyonu oluşturuldu: {reservationNo}", reservationId);
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(cancellationToken);
            return (false, "Rezervasyon oluşturulamadı: " + ex.Message, null);
        }
    }

    private static async Task<FirmaReservationGuestPolicyViewModel> LoadGuestPolicyAsync(SqlConnection connection, long roomTypeId, int roomCount, CancellationToken cancellationToken)
    {
        var policy = new FirmaReservationGuestPolicyViewModel { RoomCount = Math.Max(1, roomCount) };
        if (roomTypeId <= 0)
        {
            policy.HintText = "Oda tipi seçildiğinde kapasite kuralları gösterilir.";
            return policy;
        }

        const string sql = @"
            SELECT COALESCE([MAKSIMUM_KISI_SAYISI], 0),
                   COALESCE([MAKSIMUM_YETISKIN_SAYISI], 0),
                   COALESCE([MAKSIMUM_COCUK_SAYISI], 0)
            FROM [dbo].[ODA_TIPLERI]
            WHERE id = @roomTypeId AND [AKTIF_MI] = 1;";
        await using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@roomTypeId", roomTypeId);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            policy.HintText = "Seçilen oda tipi için kapasite bilgisi bulunamadı.";
            return policy;
        }

        policy.MaxGuestPerRoom = reader.GetInt32(0);
        policy.MaxAdultPerRoom = reader.GetInt32(1);
        policy.MaxChildPerRoom = reader.GetInt32(2);
        policy.HasCapacityLimit = policy.MaxGuestPerRoom > 0;
        if (!policy.HasCapacityLimit)
        {
            policy.HintText = "Bu oda tipi için üst misafir limiti tanımlı değil; otel onayı sırasında kontrol edilir.";
            return policy;
        }

        var adultPart = policy.MaxAdultPerRoom > 0 ? $"en fazla {policy.MaxAdultPerRoom} yetişkin" : "yetişkin";
        var childPart = policy.MaxChildPerRoom > 0 ? $", {policy.MaxChildPerRoom} çocuk" : string.Empty;
        policy.HintText = $"Oda başına {policy.MaxGuestPerRoom} kişi ({adultPart}{childPart}). {policy.RoomCount} oda için toplam en fazla {policy.TotalMaxGuests} misafir.";
        return policy;
    }

    private static string? ValidateGuestCounts(FirmaReservationCreateModel model, FirmaReservationGuestPolicyViewModel policy)
    {
        if (!policy.HasCapacityLimit)
        {
            return null;
        }

        var totalGuests = model.AdultCount + model.ChildCount;
        if (totalGuests > policy.TotalMaxGuests)
        {
            return $"Toplam misafir sayısı ({totalGuests}) seçilen {policy.RoomCount} oda için üst sınırı ({policy.TotalMaxGuests}) aşıyor.";
        }

        if (policy.MaxAdultPerRoom > 0 && model.AdultCount > policy.MaxAdultPerRoom * Math.Max(1, model.RoomCount))
        {
            return $"Yetişkin sayısı oda kapasitesini aşıyor (oda başına en fazla {policy.MaxAdultPerRoom}).";
        }

        if (policy.MaxChildPerRoom > 0 && model.ChildCount > policy.MaxChildPerRoom * Math.Max(1, model.RoomCount))
        {
            return $"Çocuk sayısı oda kapasitesini aşıyor (oda başına en fazla {policy.MaxChildPerRoom}).";
        }

        return null;
    }

    private static async Task<bool> EmployeeBelongsToFirmaAsync(SqlConnection connection, long firmaId, long employeeUserId, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT TOP (1) 1
            FROM [dbo].[KULLANICILAR]
            WHERE id = @userId AND [FIRMA_ID] = @firmaId AND [ROL] LIKE 'firma_%';";
        await using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@userId", employeeUserId);
        cmd.Parameters.AddWithValue("@firmaId", firmaId);
        var raw = await cmd.ExecuteScalarAsync(cancellationToken);
        return raw is not null and not DBNull;
    }

    private async Task<FirmaReservationPriceCompareViewModel> BuildPriceCompareAsync(SqlConnection connection, long firmaId, FirmaReservationCreateModel model, CancellationToken cancellationToken)
    {
        var culture = CultureInfo.GetCultureInfo("tr-TR");
        var nightCount = Math.Max(1, model.CheckOutDate.DayNumber - model.CheckInDate.DayNumber);
        var start = model.CheckInDate;
        var end = model.CheckOutDate.AddDays(-1);
        decimal standardSum = 0m;
        decimal companySum = 0m;
        var hasCompanyAny = false;

        // Query both firm daily override + base ofm effective
        const string sql = @"
            SELECT d.[TARIH],
                   f.[FIRMA_GECELIK_FIYAT],
                   f.[KAPALI_SATIS],
                   CASE
                       WHEN ofm.[INDIRIMLI_FIYAT] IS NOT NULL AND ofm.[INDIRIMLI_FIYAT] > 0 AND ofm.[INDIRIMLI_FIYAT] < ofm.[GECELIK_FIYAT] THEN ofm.[INDIRIMLI_FIYAT]
                       ELSE ofm.[GECELIK_FIYAT]
                   END AS base_price
            FROM
            (
                SELECT DATEADD(DAY, v.number, @startDate) AS [TARIH]
                FROM master..spt_values v
                WHERE v.type = 'P' AND v.number BETWEEN 0 AND DATEDIFF(DAY, @startDate, @endDate)
            ) d
            LEFT JOIN [dbo].[FIRMA_ODA_FIYAT_MUSAITLIK] f
                ON f.[OTEL_ID]=@hotelId AND f.[ODA_TIP_ID]=@roomTypeId AND f.[TARIH] = d.[TARIH]
            LEFT JOIN [dbo].[ODA_FIYAT_MUSAITLIK] ofm
                ON ofm.[OTEL_ID]=@hotelId AND ofm.[ODA_TIP_ID]=@roomTypeId AND ofm.[TARIH] = d.[TARIH]
            ORDER BY d.[TARIH];";

        await using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@startDate", start.ToDateTime(TimeOnly.MinValue));
        cmd.Parameters.AddWithValue("@endDate", end.ToDateTime(TimeOnly.MinValue));
        cmd.Parameters.AddWithValue("@hotelId", model.HotelId);
        cmd.Parameters.AddWithValue("@roomTypeId", model.RoomTypeId);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var firmPrice = reader.IsDBNull(1) ? (decimal?)null : reader.GetDecimal(1);
            var firmClosed = !reader.IsDBNull(2) && reader.GetBoolean(2);
            var basePrice = reader.IsDBNull(3) ? 0m : reader.GetDecimal(3);
            if (basePrice > 0m)
            {
                standardSum += basePrice;
            }

            if (!firmClosed && firmPrice.HasValue && firmPrice.Value > 0m)
            {
                companySum += firmPrice.Value;
                hasCompanyAny = true;
            }
            else
            {
                companySum += basePrice;
            }
        }

        standardSum *= Math.Max(1, model.RoomCount);
        companySum *= Math.Max(1, model.RoomCount);

        return new FirmaReservationPriceCompareViewModel
        {
            StandardTotal = standardSum,
            CompanyTotal = companySum,
            StandardTotalText = standardSum <= 0 ? "-" : standardSum.ToString("N0", culture) + " ₺",
            CompanyTotalText = companySum <= 0 ? "-" : companySum.ToString("N0", culture) + " ₺",
            SavingsText = (standardSum - companySum) <= 0 ? "0 ₺" : (standardSum - companySum).ToString("N0", culture) + " ₺",
            NightCountText = $"{nightCount} gece · {model.RoomCount} oda",
            HasCompanyPrice = hasCompanyAny,
            Note = hasCompanyAny ? "Firma günlük fiyatları uygulandı." : "Firma günlük fiyatı yoksa standart fiyat baz alındı."
        };
    }

    private static IEnumerable<string> SplitEmails(string? csv)
    {
        if (string.IsNullOrWhiteSpace(csv)) yield break;
        foreach (var item in csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (item.Contains('@')) yield return item;
        }
    }

    private static async Task<string> ResolveGuestNameAsync(SqlConnection connection, SqlTransaction tx, long firmaId, long? employeeUserId, CancellationToken cancellationToken)
    {
        if (employeeUserId.HasValue && employeeUserId.Value > 0)
        {
            await using var cmd = new SqlCommand("SELECT TOP (1) COALESCE(NULLIF([AD_SOYAD],''),'Firma Personeli') FROM [dbo].[KULLANICILAR] WHERE id=@id;", connection, tx);
            cmd.Parameters.AddWithValue("@id", employeeUserId.Value);
            var raw = await cmd.ExecuteScalarAsync(cancellationToken);
            if (raw is not null and not DBNull) return Convert.ToString(raw, CultureInfo.InvariantCulture) ?? "Firma Personeli";
        }
        await using var firmCmd = new SqlCommand("SELECT TOP (1) COALESCE(NULLIF([FIRMA_ADI],''),'Firma') FROM [dbo].[FIRMALAR] WHERE id=@id;", connection, tx);
        firmCmd.Parameters.AddWithValue("@id", firmaId);
        var firmRaw = await firmCmd.ExecuteScalarAsync(cancellationToken);
        return firmRaw is null or DBNull ? "Firma" : Convert.ToString(firmRaw, CultureInfo.InvariantCulture) ?? "Firma";
    }

    private static async Task<(string? Email, string? CompanyName)> LoadCompanyContactAsync(SqlConnection connection, SqlTransaction tx, long firmaId, CancellationToken cancellationToken)
    {
        await using var cmd = new SqlCommand("SELECT TOP (1) COALESCE(NULLIF([FIRMA_EPOSTA],''), NULLIF([YETKILI_EPOSTA],''), NULL), COALESCE(NULLIF([FIRMA_ADI],''),NULL) FROM [dbo].[FIRMALAR] WHERE id=@id;", connection, tx);
        cmd.Parameters.AddWithValue("@id", firmaId);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) return (null, null);
        var email = reader.IsDBNull(0) ? null : reader.GetString(0);
        var name = reader.IsDBNull(1) ? null : reader.GetString(1);
        return (email, name);
    }

    private static async Task<string?> ResolveEmployeeEmailAsync(SqlConnection connection, SqlTransaction tx, long userId, CancellationToken cancellationToken)
    {
        await using var cmd = new SqlCommand("SELECT TOP (1) COALESCE(NULLIF([EPOSTA],''), NULL) FROM [dbo].[KULLANICILAR] WHERE id=@id;", connection, tx);
        cmd.Parameters.AddWithValue("@id", userId);
        var raw = await cmd.ExecuteScalarAsync(cancellationToken);
        return raw is null or DBNull ? null : Convert.ToString(raw, CultureInfo.InvariantCulture);
    }

    private static async Task<string> LoadHotelNameAsync(SqlConnection connection, SqlTransaction tx, long hotelId, CancellationToken cancellationToken)
    {
        await using var cmd = new SqlCommand("SELECT TOP (1) [OTEL_ADI] FROM [dbo].[OTELLER] WHERE id=@id;", connection, tx);
        cmd.Parameters.AddWithValue("@id", hotelId);
        var raw = await cmd.ExecuteScalarAsync(cancellationToken);
        return raw is null or DBNull ? "Otel" : Convert.ToString(raw, CultureInfo.InvariantCulture) ?? "Otel";
    }

    private static async Task<(long UserId, string Email)> ResolvePartnerRecipientAsync(SqlConnection connection, SqlTransaction tx, long hotelId, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT TOP (1) COALESCE(o.[KULLANICI_ID], oks.[KULLANICI_ID], 1), COALESCE(o.[SATIS_KONTAK_EPOSTA], u.[EPOSTA], o.[EPOSTA], 'partner@otelturizm.com')
            FROM [dbo].[OTELLER] o
            LEFT JOIN [dbo].[OTEL_KULLANICI_SAHIPLIKLERI] oks ON oks.[OTEL_ID] = o.id AND oks.[AKTIF_MI] = 1
            LEFT JOIN [dbo].[KULLANICILAR] u ON u.id = COALESCE(o.[KULLANICI_ID], oks.[KULLANICI_ID])
            WHERE o.id = @hotelId
            ORDER BY oks.[ANA_SORUMLU_MU] DESC, oks.id ASC;";
        await using var cmd = new SqlCommand(sql, connection, tx);
        cmd.Parameters.AddWithValue("@hotelId", hotelId);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            return (reader.IsDBNull(0) ? 1L : reader.GetInt64(0), reader.GetString(1));
        }
        return (1, "partner@otelturizm.com");
    }

    private static async Task<string> GenerateFirmaReservationNoAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        await using var cmd = new SqlCommand("SELECT COUNT(*) + 1 FROM [dbo].[REZERVASYONLAR] WHERE CAST([OLUSTURULMA_TARIHI] AS date) = CAST(GETDATE() AS date);", connection);
        var seq = Convert.ToInt32(await cmd.ExecuteScalarAsync(cancellationToken) ?? 0, CultureInfo.InvariantCulture);
        return $"FRM-{DateTime.Now:yyyyMMdd}-{seq:0000}";
    }

    public async Task<FirmaMessagesPageViewModel> GetMessagesAsync(long userId, long? conversationId, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        var context = await BuildContextAsync(connection, userId, "Mesajlar", "Kullanıcılarla güvenli şekilde yazışın, ek ve belge paylaşın.", "messages", cancellationToken);
        var inbox = await _messageCenterService.GetFirmaInboxAsync(userId, conversationId, cancellationToken);
        return new FirmaMessagesPageViewModel
        {
            Shell = context.Shell,
            Threads = inbox.Threads,
            SelectedConversationId = inbox.SelectedConversationId,
            SelectedTitle = inbox.SelectedTitle,
            SelectedSubtitle = inbox.SelectedSubtitle,
            Messages = inbox.Messages
        };
    }

    public async Task<FirmaEmployeesPageViewModel> GetEmployeesAsync(
        long userId,
        string? q = null,
        string? departman = null,
        int? page = null,
        int? pageSize = null,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        var context = await BuildContextAsync(connection, userId, "Çalışanlar", "Firma kullanıcılarını, departmanlarını ve harcama yetkilerini yönetin.", "employees", cancellationToken);
        var resolvedPageSize = ResolveEmployeesPageSize(pageSize);
        var resolvedPage = ResolveEmployeesPage(page);
        var offset = (resolvedPage - 1) * resolvedPageSize;

        var departments = await LoadEmployeeDepartmentsAsync(connection, context.FirmaId, cancellationToken);
        var (employees, totalCount) = await LoadEmployeesPagedAsync(connection, context.FirmaId, q, departman, offset, resolvedPageSize, cancellationToken);
        var totalPages = totalCount <= 0 ? 1 : (int)Math.Ceiling(totalCount / (double)resolvedPageSize);
        resolvedPage = Math.Clamp(resolvedPage, 1, Math.Max(1, totalPages));

        return new FirmaEmployeesPageViewModel
        {
            Shell = context.Shell,
            Employees = employees,
            TotalCount = totalCount,
            CurrentPage = resolvedPage,
            PageSize = resolvedPageSize,
            TotalPages = totalPages,
            SearchTerm = string.IsNullOrWhiteSpace(q) ? null : q.Trim(),
            DepartmentFilter = string.IsNullOrWhiteSpace(departman) ? null : departman.Trim(),
            Departments = departments,
            TravelingEmployeeCount = employees.Count(static x => x.ReservationCountText != "0"),
            AverageLimitText = employees.Count == 0
                ? "₺0"
                : FormatMoney(ParseMoneyAverage(employees.Select(static x => x.LimitText)))
        };
    }

    private static int ResolveEmployeesPageSize(int? pageSize)
    {
        return pageSize switch
        {
            25 => 25,
            35 => 35,
            45 => 45,
            _ => 25
        };
    }

    private static int ResolveEmployeesPage(int? page)
    {
        if (!page.HasValue) return 1;
        if (page.Value < 1) return 1;
        return Math.Min(page.Value, 10_000);
    }

    private async Task<List<string>> LoadEmployeeDepartmentsAsync(SqlConnection connection, long firmaId, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT DISTINCT LTRIM(RTRIM(COALESCE([DEPARTMAN], N'Tanımsız'))) AS [DEPARTMAN]
            FROM [dbo].[KULLANICILAR]
            WHERE [FIRMA_ID] = @firmaId AND rol LIKE 'firma_%'
            ORDER BY LTRIM(RTRIM(COALESCE([DEPARTMAN], N'Tanımsız'))) ASC;";

        var items = new List<string>();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@firmaId", firmaId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            if (reader.IsDBNull(0)) continue;
            var dep = reader.GetString(0);
            if (string.IsNullOrWhiteSpace(dep)) continue;
            items.Add(dep);
        }
        return items;
    }

    private async Task<(List<FirmaPanelEmployeeRowViewModel> Items, int TotalCount)> LoadEmployeesPagedAsync(
        SqlConnection connection,
        long firmaId,
        string? q,
        string? departman,
        int offset,
        int take,
        CancellationToken cancellationToken)
    {
        const string sql = @"
            DECLARE @qLike nvarchar(250) = NULL;
            IF (@q IS NOT NULL AND LTRIM(RTRIM(@q)) <> N'') SET @qLike = N'%' + LTRIM(RTRIM(@q)) + N'%';

            WITH base_users AS (
                SELECT u.id
                FROM [dbo].[KULLANICILAR] u
                WHERE u.[FIRMA_ID] = @firmaId
                  AND u.[ROL] LIKE 'firma_%'
                  AND (@qLike IS NULL OR u.[AD_SOYAD] LIKE @qLike OR u.[EPOSTA] LIKE @qLike OR COALESCE(u.[DEPARTMAN], N'Tanımsız') LIKE @qLike)
                  AND (@departman IS NULL OR LTRIM(RTRIM(@departman)) = N'' OR LTRIM(RTRIM(COALESCE(u.[DEPARTMAN], N'Tanımsız'))) = LTRIM(RTRIM(@departman)))
            )
            SELECT u.id, u.[AD_SOYAD], COALESCE(u.[DEPARTMAN], N'Tanımsız'), COALESCE(u.[GOREV_UNVANI], u.[ROL]), u.[EPOSTA],
                   u.[HARCAMA_LIMITI], u.[ONAY_GEREKSINIMI], u.[ROL], u.[FIRMA_YONETICI_MI],
                   u.[TELEFON_DOGRULAMA_TARIHI], u.[TELEFON_SON_SAHIPLIK_TEYIT_TARIHI], COALESCE(u.[TELEFON_DOGRULAMA_DURUMU], N''),
                   COUNT(r.id) AS [REZERVASYON_SAYISI], COALESCE(SUM(r.[TOPLAM_TUTAR]), 0) AS harcama_toplami,
                   (SELECT COUNT(*) FROM base_users) AS total_count
            FROM [dbo].[KULLANICILAR] u
            INNER JOIN base_users bu ON bu.id = u.id
            LEFT JOIN [dbo].[REZERVASYONLAR] r ON r.[FIRMA_CALISAN_ID] = u.id
            GROUP BY u.id, u.[AD_SOYAD], u.[DEPARTMAN], u.[GOREV_UNVANI], u.[EPOSTA], u.[HARCAMA_LIMITI], u.[ONAY_GEREKSINIMI], u.[ROL], u.[FIRMA_YONETICI_MI],
                     u.[TELEFON_DOGRULAMA_TARIHI], u.[TELEFON_SON_SAHIPLIK_TEYIT_TARIHI], u.[TELEFON_DOGRULAMA_DURUMU]
            ORDER BY u.[FIRMA_YONETICI_MI] DESC, u.[AD_SOYAD] ASC
            OFFSET @offset ROWS FETCH NEXT @take ROWS ONLY;";

        var items = new List<FirmaPanelEmployeeRowViewModel>();
        var totalCount = 0;

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@firmaId", firmaId);
        command.Parameters.AddWithValue("@q", (object?)q ?? DBNull.Value);
        command.Parameters.AddWithValue("@departman", (object?)departman ?? DBNull.Value);
        command.Parameters.AddWithValue("@offset", Math.Max(0, offset));
        command.Parameters.AddWithValue("@take", Math.Max(1, take));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var fullName = reader.GetString(1);
            var verifiedAt = reader.IsDBNull(9) ? (DateTime?)null : reader.GetDateTime(9);
            var ownershipAt = reader.IsDBNull(10) ? (DateTime?)null : reader.GetDateTime(10);
            var phoneStatus = reader.IsDBNull(11) ? string.Empty : reader.GetString(11);
            var isPhoneVerified = verifiedAt.HasValue
                && (!ownershipAt.HasValue || ownershipAt.Value >= DateTime.UtcNow.AddDays(-180));

            if (totalCount == 0) totalCount = SafeInt(reader, 14);

            items.Add(new FirmaPanelEmployeeRowViewModel
            {
                UserId = reader.GetInt64(0),
                FullName = fullName,
                Department = reader.GetString(2),
                Title = reader.GetString(3),
                Email = reader.GetString(4),
                LimitText = reader.IsDBNull(5) ? "-" : FormatMoney(reader.GetDecimal(5)),
                ApprovalText = SafeBool(reader, 6) ? "Onaylı akış" : "Serbest rezervasyon",
                Initials = GetInitials(fullName),
                RoleText = GetRoleLabel(reader.IsDBNull(7) ? string.Empty : reader.GetString(7)),
                IsManager = SafeBool(reader, 8),
                IsPhoneVerified = isPhoneVerified,
                PhoneVerificationText = isPhoneVerified
                    ? $"Telefon doğrulandı · {verifiedAt!.Value.ToLocalTime():dd.MM.yyyy}"
                    : string.Equals(phoneStatus, "Beklemede", StringComparison.OrdinalIgnoreCase)
                        ? "Telefon doğrulaması bekleniyor"
                        : "Telefon doğrulanmadı",
                PhoneVerificationToneClass = isPhoneVerified ? "success" : string.Equals(phoneStatus, "Beklemede", StringComparison.OrdinalIgnoreCase) ? "warning" : "secondary",
                ReservationCountText = SafeInt(reader, 12).ToString(CultureInfo.InvariantCulture),
                SpendText = FormatMoney(SafeDecimal(reader, 13))
            });
        }

        return (items, totalCount);
    }

    public async Task<FirmaLimitsPageViewModel> GetLimitsAsync(long userId, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        var context = await BuildContextAsync(connection, userId, "Limitler & Onaylar", "Departman ve çalışan bazlı harcama limitleri ile onay akışlarını yönetin.", "limits", cancellationToken);
        var model = new FirmaLimitsPageViewModel { Shell = context.Shell };

        try
        {
            const string sql = @"
                SELECT id,
                       CASE WHEN [KULLANICI_ID] IS NOT NULL THEN CONCAT('Kullanıcı · ', COALESCE((SELECT TOP (1) [AD_SOYAD] FROM [dbo].[KULLANICILAR] u WHERE u.id = fhl.[KULLANICI_ID]), 'Kayıt'))
                            WHEN [DEPARTMAN] IS NOT NULL THEN CONCAT('Departman · ', [DEPARTMAN])
                            ELSE 'Firma Geneli' END AS scope_text,
                       [GECELIK_LIMIT], [REZERVASYON_BASI_LIMIT], [AYLIK_LIMIT], [ONAY_GEREKSINIMI]
                FROM [dbo].[FIRMA_HARCAMA_LIMITLERI] fhl
                WHERE [FIRMA_ID] = @firmaId AND [AKTIF_MI] = 1
                ORDER BY [KULLANICI_ID] IS NULL DESC, [DEPARTMAN] ASC, id ASC;";

            await using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@firmaId", context.FirmaId);
                await using var reader = await command.ExecuteReaderAsync(cancellationToken);
                while (await reader.ReadAsync(cancellationToken))
                {
                    model.Limits.Add(new FirmaPanelLimitRowViewModel
                    {
                        LimitId = reader.GetInt64(0),
                        ScopeText = reader.GetString(1),
                        NightlyLimitText = reader.IsDBNull(2) ? "-" : FormatMoney(reader.GetDecimal(2)),
                        ReservationLimitText = reader.IsDBNull(3) ? "-" : FormatMoney(reader.GetDecimal(3)),
                        MonthlyLimitText = reader.IsDBNull(4) ? "-" : FormatMoney(reader.GetDecimal(4)),
                        ApprovalText = SafeBool(reader, 5) ? "Onay gerekli" : "Otomatik onay"
                    });
                }
            }

            model.PendingApprovals = await LoadPendingApprovalsAsync(connection, context.FirmaId, cancellationToken);
            model.Employees = await LoadEmployeesAsync(connection, context.FirmaId, 200, cancellationToken);
            return model;
        }
        catch (SqlException ex) when (IsMissingTableOrColumn(ex))
        {
            model.Shell.ErrorMessage = "Limitler & Onaylar modülü için veritabanı şeması eksik görünüyor. Local DB'de firma panel migration'larını çalıştırdıktan sonra bu sayfa tam olarak açılacaktır.";
            model.PendingApprovals = new List<FirmaPanelReservationRowViewModel>();
            model.Employees = await LoadEmployeesAsync(connection, context.FirmaId, 200, cancellationToken);
            return model;
        }
    }

    public async Task<FirmaInvoicesPageViewModel> GetInvoicesAsync(long userId, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        var context = await BuildContextAsync(connection, userId, "Faturalar", "Kurumsal rezervasyonlara ait fatura kayıtlarını takip edin.", "invoices", cancellationToken);
        var model = new FirmaInvoicesPageViewModel { Shell = context.Shell };

        const string sql = @"
            SELECT TOP (100) [FATURA_NO], [FATURA_TARIHI], [FATURA_TURU], [FATURA_ALICI_UNVAN], [GENEL_TOPLAM], [FATURA_DURUMU]
            FROM [dbo].[FATURALAR]
            WHERE [FIRMA_ID] = @firmaId
            ORDER BY [FATURA_TARIHI] DESC, id DESC;";

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@firmaId", context.FirmaId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            model.Invoices.Add(new FirmaPanelInvoiceRowViewModel
            {
                InvoiceNo = reader.GetString(0),
                InvoiceDateText = reader.GetDateTime(1).ToString("dd.MM.yyyy", CultureInfo.GetCultureInfo("tr-TR")),
                InvoiceType = reader.GetString(2),
                RecipientName = reader.GetString(3),
                TotalText = FormatMoney(SafeDecimal(reader, 4)),
                StatusText = reader.IsDBNull(5) ? string.Empty : reader.GetString(5)
            });
        }

        return model;
    }

    public async Task<FirmaSpendingReportsPageViewModel> GetSpendingReportsAsync(long userId, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        var context = await BuildContextAsync(connection, userId, "Harcama Raporları", "Aylık kurumsal spend ve rezervasyon adetlerini izleyin.", "spending", cancellationToken);
        var model = new FirmaSpendingReportsPageViewModel { Shell = context.Shell };

        const string sql = @"
            SELECT FORMAT([OLUSTURULMA_TARIHI], 'MMM', 'tr-TR') AS ay, COALESCE(SUM([TOPLAM_TUTAR]), 0) AS toplam, COUNT(*) AS [REZERVASYON_SAYISI]
            FROM [dbo].[REZERVASYONLAR]
            WHERE [FIRMA_ID] = @firmaId
              AND [OLUSTURULMA_TARIHI] >= DATEADD(MONTH, -5, CAST(SYSUTCDATETIME() AS date))
            GROUP BY YEAR([OLUSTURULMA_TARIHI]), MONTH([OLUSTURULMA_TARIHI]), FORMAT([OLUSTURULMA_TARIHI], 'MMM', 'tr-TR')
            ORDER BY YEAR([OLUSTURULMA_TARIHI]), MONTH([OLUSTURULMA_TARIHI]);";

        await using (var command = new SqlCommand(sql, connection))
        {
            command.Parameters.AddWithValue("@firmaId", context.FirmaId);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                model.MonthlySpend.Add(new FirmaMonthlySpendRowViewModel
                {
                    Label = reader.GetString(0),
                    Amount = SafeDecimal(reader, 1),
                    ReservationCount = SafeInt(reader, 2)
                });
            }
        }

        var maxAmount = Math.Max(1m, model.MonthlySpend.Count == 0 ? 0m : model.MonthlySpend.Max(x => x.Amount));
        foreach (var item in model.MonthlySpend)
        {
            item.HeightPercent = Math.Max(16, (int)Math.Round(item.Amount * 100m / maxAmount));
        }

        model.TotalSpendText = FormatMoney(model.MonthlySpend.Sum(x => x.Amount));
        return model;
    }

    public async Task<FirmaHotelReportsPageViewModel> GetHotelReportsAsync(long userId, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        var context = await BuildContextAsync(connection, userId, "Otel Bazlı Rapor", "Hangi otelde ne kadar rezervasyon ve tasarruf oluştuğunu görün.", "hotels", cancellationToken);
        var model = new FirmaHotelReportsPageViewModel { Shell = context.Shell };

        const string sql = @"
            SELECT o.[OTEL_ADI], CONCAT(o.[ILCE], ', ', o.[SEHIR]) AS city_text, COUNT(*) AS [REZERVASYON_SAYISI],
                   COALESCE(SUM(r.[TOPLAM_TUTAR]), 0) AS toplam, COALESCE(SUM(r.[TOPLAM_TASARRUF]), 0) AS tasarruf
            FROM [dbo].[REZERVASYONLAR] r
            INNER JOIN [dbo].[OTELLER] o ON o.id = r.[OTEL_ID]
            WHERE r.[FIRMA_ID] = @firmaId
            GROUP BY o.id, o.[OTEL_ADI], o.[ILCE], o.[SEHIR]
            ORDER BY toplam DESC, [REZERVASYON_SAYISI] DESC;";

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@firmaId", context.FirmaId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            model.HotelReports.Add(new FirmaHotelReportRowViewModel
            {
                HotelName = reader.GetString(0),
                CityText = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                ReservationCount = SafeInt(reader, 2),
                GrossAmountText = FormatMoney(SafeDecimal(reader, 3)),
                SavingsText = FormatMoney(SafeDecimal(reader, 4))
            });
        }

        return model;
    }

    public async Task<(bool Success, string Message)> CreateEmployeeAsync(long userId, FirmaEmployeeCreateModel model, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(model.FullName) || string.IsNullOrWhiteSpace(model.Email))
        {
            return (false, "Çalışan adı ve e-posta zorunludur.");
        }

        var normalizedEmail = model.Email.Trim().ToLowerInvariant();
        var role = NormalizeFirmaRole(model.Role);

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        var context = await BuildContextAsync(connection, userId, string.Empty, string.Empty, "employees", cancellationToken);

        const string existsSql = "SELECT COUNT(*) FROM [dbo].[KULLANICILAR] WHERE [EPOSTA] = @email;";
        await using (var existsCommand = new SqlCommand(existsSql, connection))
        {
            existsCommand.Parameters.AddWithValue("@email", normalizedEmail);
            var existingCount = Convert.ToInt32(await existsCommand.ExecuteScalarAsync(cancellationToken), CultureInfo.InvariantCulture);
            if (existingCount > 0)
            {
                return (false, "Bu e-posta adresi zaten kullanılıyor.");
            }
        }

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
                const string insertUserSql = @"
                    INSERT INTO [dbo].[KULLANICILAR]
                    (
                        [AD_SOYAD], [EPOSTA], [TELEFON], [TELEFON_E164], [TELEFON_DOGRULAMA_KANALI], [TELEFON_DOGRULAMA_DURUMU], [SIFRE], rol, [FIRMA_ID], [DEPARTMAN], [GOREV_UNVANI],
                        [HARCAMA_LIMITI], [ONAY_GEREKSINIMI], [PERSONEL_KODU], [FIRMA_YONETICI_MI],
                        [HESAP_DURUMU], [DIL_TERCIHI], [PARA_BIRIMI], ulke, [OLUSTURULMA_TARIHI]
                    )
                    VALUES
                    (
                        @fullName, @email, @phone, @phoneE164, @phoneVerificationChannel, @phoneVerificationStatus, LOWER(CONVERT(VARCHAR(64), HASHBYTES('SHA2_256', CONVERT(nvarchar(max), '1585')), 2)), @role, @firmaId, @department, @title,
                        @nightlyLimit, @approvalRequired, @personelCode, @isManager,
                        1, 'tr', 'TRY', 'Türkiye', CURRENT_TIMESTAMP
                    );
                    SELECT CAST(SCOPE_IDENTITY() AS bigint);";

            long createdUserId;
            await using (var insertUserCommand = new SqlCommand(insertUserSql, connection, (SqlTransaction)transaction))
            {
                var normalizedPhone = PhoneVerificationService.NormalizePhoneNumber(model.Phone);
                insertUserCommand.Parameters.AddWithValue("@fullName", model.FullName.Trim());
                insertUserCommand.Parameters.AddWithValue("@email", normalizedEmail);
                insertUserCommand.Parameters.AddWithValue("@phone", string.IsNullOrWhiteSpace(model.Phone) ? DBNull.Value : (object)model.Phone.Trim());
                insertUserCommand.Parameters.AddWithValue("@phoneE164", string.IsNullOrWhiteSpace(normalizedPhone) ? DBNull.Value : (object)normalizedPhone);
                insertUserCommand.Parameters.AddWithValue("@phoneVerificationChannel", string.IsNullOrWhiteSpace(normalizedPhone) ? DBNull.Value : (object)"whatsapp");
                insertUserCommand.Parameters.AddWithValue("@phoneVerificationStatus", string.IsNullOrWhiteSpace(normalizedPhone) ? DBNull.Value : (object)"Dogrulanmadi");
                insertUserCommand.Parameters.AddWithValue("@role", role);
                insertUserCommand.Parameters.AddWithValue("@firmaId", context.FirmaId);
                insertUserCommand.Parameters.AddWithValue("@department", string.IsNullOrWhiteSpace(model.Department) ? DBNull.Value : (object)model.Department.Trim());
                insertUserCommand.Parameters.AddWithValue("@title", string.IsNullOrWhiteSpace(model.Title) ? DBNull.Value : (object)model.Title.Trim());
                insertUserCommand.Parameters.AddWithValue("@nightlyLimit", model.NightlyLimit.HasValue ? (object)model.NightlyLimit.Value : DBNull.Value);
                insertUserCommand.Parameters.AddWithValue("@approvalRequired", model.ApprovalRequired ? 1 : 0);
                insertUserCommand.Parameters.AddWithValue("@personelCode", BuildPersonelCode(context.FirmaId));
                insertUserCommand.Parameters.AddWithValue("@isManager", role is "firma_admin" or "firma_manager" ? 1 : 0);
                createdUserId = Convert.ToInt64(await insertUserCommand.ExecuteScalarAsync(cancellationToken), CultureInfo.InvariantCulture);
            }

            if (model.NightlyLimit.HasValue || model.ApprovalRequired)
            {
                const string limitSql = @"
                    INSERT INTO [dbo].[FIRMA_HARCAMA_LIMITLERI]
                    (
                        [FIRMA_ID], [KULLANICI_ID], [DEPARTMAN], [GECELIK_LIMIT], [ONAY_GEREKSINIMI], [AKTIF_MI], [OLUSTURULMA_TARIHI]
                    )
                    VALUES
                    (
                        @firmaId, @userId, @department, @nightlyLimit, @approvalRequired, 1, CURRENT_TIMESTAMP
                    );";

                await using var limitCommand = new SqlCommand(limitSql, connection, (SqlTransaction)transaction);
                limitCommand.Parameters.AddWithValue("@firmaId", context.FirmaId);
                limitCommand.Parameters.AddWithValue("@userId", createdUserId);
                limitCommand.Parameters.AddWithValue("@department", string.IsNullOrWhiteSpace(model.Department) ? DBNull.Value : (object)model.Department.Trim());
                limitCommand.Parameters.AddWithValue("@nightlyLimit", model.NightlyLimit.HasValue ? (object)model.NightlyLimit.Value : DBNull.Value);
                limitCommand.Parameters.AddWithValue("@approvalRequired", model.ApprovalRequired ? 1 : 0);
                await limitCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
            return (true, $"Çalışan oluşturuldu. İlk giriş şifresi: 1585 · E-posta: {normalizedEmail}");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            return (false, $"Çalışan ekleme sırasında hata oluştu: {ex.Message}");
        }
    }

    public Task<(bool Success, string Message)> SendMessageAsync(long userId, MessageSendRequest form, IReadOnlyList<IFormFile>? attachments, HttpContext httpContext, CancellationToken cancellationToken = default)
        => _messageCenterService.SendFromFirmaAsync(userId, form, attachments, httpContext, cancellationToken);

    public Task<(bool Success, string Message)> DeleteMessageAsync(long userId, MessageDeleteRequest form, CancellationToken cancellationToken = default)
        => _messageCenterService.DeleteForFirmaAsync(userId, form, cancellationToken);

    public async Task<(bool Success, string Message)> UpsertLimitAsync(long userId, FirmaLimitUpsertModel model, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        var context = await BuildContextAsync(connection, userId, string.Empty, string.Empty, "limits", cancellationToken);

        if (model.UserId is null && string.IsNullOrWhiteSpace(model.Department) && model.MonthlyLimit is null && model.NightlyLimit is null && model.ReservationLimit is null)
        {
            return (false, "Limit kaydı için en az bir kapsam veya tutar alanı girilmelidir.");
        }

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            if (model.UserId.HasValue)
            {
                const string validateUserSql = "SELECT COUNT(*) FROM [dbo].[KULLANICILAR] WHERE id = @userId AND [FIRMA_ID] = @firmaId;";
                await using var validateCommand = new SqlCommand(validateUserSql, connection, (SqlTransaction)transaction);
                validateCommand.Parameters.AddWithValue("@userId", model.UserId.Value);
                validateCommand.Parameters.AddWithValue("@firmaId", context.FirmaId);
                var exists = Convert.ToInt32(await validateCommand.ExecuteScalarAsync(cancellationToken), CultureInfo.InvariantCulture) > 0;
                if (!exists)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return (false, "Seçilen çalışan bu firmaya bağlı değil.");
                }
            }

            const string findSql = @"
                SELECT id
                FROM [dbo].[FIRMA_HARCAMA_LIMITLERI]
                WHERE [FIRMA_ID] = @firmaId
                  AND [AKTIF_MI] = 1
                  AND ((@userId IS NOT NULL AND [KULLANICI_ID] = @userId)
                    OR (@userId IS NULL AND @department IS NOT NULL AND [DEPARTMAN] = @department)
                    OR (@userId IS NULL AND @department IS NULL AND [KULLANICI_ID] IS NULL AND [DEPARTMAN] IS NULL))
                ORDER BY id DESC
                OFFSET 0 ROWS FETCH NEXT 1 ROWS ONLY;";

            long? existingId = null;
            await using (var findCommand = new SqlCommand(findSql, connection, (SqlTransaction)transaction))
            {
                findCommand.Parameters.AddWithValue("@firmaId", context.FirmaId);
                findCommand.Parameters.AddWithValue("@userId", model.UserId.HasValue ? (object)model.UserId.Value : DBNull.Value);
                findCommand.Parameters.AddWithValue("@department", string.IsNullOrWhiteSpace(model.Department) ? DBNull.Value : (object)model.Department.Trim());
                var result = await findCommand.ExecuteScalarAsync(cancellationToken);
                if (result is not null)
                {
                    existingId = Convert.ToInt64(result, CultureInfo.InvariantCulture);
                }
            }

            if (existingId.HasValue)
            {
                const string updateSql = @"
                    UPDATE [dbo].[FIRMA_HARCAMA_LIMITLERI]
                    SET [DEPARTMAN] = @department,
                        [KULLANICI_ID] = @userId,
                        [GECELIK_LIMIT] = @nightlyLimit,
                        [REZERVASYON_BASI_LIMIT] = @reservationLimit,
                        [AYLIK_LIMIT] = @monthlyLimit,
                        [ONAY_GEREKSINIMI] = @approvalRequired,
                        [AKTIF_MI] = 1
                    WHERE id = @id;";

                await using var updateCommand = new SqlCommand(updateSql, connection, (SqlTransaction)transaction);
                updateCommand.Parameters.AddWithValue("@id", existingId.Value);
                updateCommand.Parameters.AddWithValue("@department", string.IsNullOrWhiteSpace(model.Department) ? DBNull.Value : (object)model.Department.Trim());
                updateCommand.Parameters.AddWithValue("@userId", model.UserId.HasValue ? (object)model.UserId.Value : DBNull.Value);
                updateCommand.Parameters.AddWithValue("@nightlyLimit", model.NightlyLimit.HasValue ? (object)model.NightlyLimit.Value : DBNull.Value);
                updateCommand.Parameters.AddWithValue("@reservationLimit", model.ReservationLimit.HasValue ? (object)model.ReservationLimit.Value : DBNull.Value);
                updateCommand.Parameters.AddWithValue("@monthlyLimit", model.MonthlyLimit.HasValue ? (object)model.MonthlyLimit.Value : DBNull.Value);
                updateCommand.Parameters.AddWithValue("@approvalRequired", model.ApprovalRequired ? 1 : 0);
                await updateCommand.ExecuteNonQueryAsync(cancellationToken);
            }
            else
            {
                const string insertSql = @"
                    INSERT INTO [dbo].[FIRMA_HARCAMA_LIMITLERI]
                    (
                        [FIRMA_ID], [DEPARTMAN], [KULLANICI_ID], [GECELIK_LIMIT], [REZERVASYON_BASI_LIMIT], [AYLIK_LIMIT],
                        [ONAY_GEREKSINIMI], [AKTIF_MI], [OLUSTURULMA_TARIHI]
                    )
                    VALUES
                    (
                        @firmaId, @department, @userId, @nightlyLimit, @reservationLimit, @monthlyLimit,
                        @approvalRequired, 1, CURRENT_TIMESTAMP
                    );";

                await using var insertCommand = new SqlCommand(insertSql, connection, (SqlTransaction)transaction);
                insertCommand.Parameters.AddWithValue("@firmaId", context.FirmaId);
                insertCommand.Parameters.AddWithValue("@department", string.IsNullOrWhiteSpace(model.Department) ? DBNull.Value : (object)model.Department.Trim());
                insertCommand.Parameters.AddWithValue("@userId", model.UserId.HasValue ? (object)model.UserId.Value : DBNull.Value);
                insertCommand.Parameters.AddWithValue("@nightlyLimit", model.NightlyLimit.HasValue ? (object)model.NightlyLimit.Value : DBNull.Value);
                insertCommand.Parameters.AddWithValue("@reservationLimit", model.ReservationLimit.HasValue ? (object)model.ReservationLimit.Value : DBNull.Value);
                insertCommand.Parameters.AddWithValue("@monthlyLimit", model.MonthlyLimit.HasValue ? (object)model.MonthlyLimit.Value : DBNull.Value);
                insertCommand.Parameters.AddWithValue("@approvalRequired", model.ApprovalRequired ? 1 : 0);
                await insertCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            if (model.UserId.HasValue)
            {
                const string userUpdateSql = @"
                    UPDATE [dbo].[KULLANICILAR]
                    SET [HARCAMA_LIMITI] = @nightlyLimit,
                        [ONAY_GEREKSINIMI] = @approvalRequired
                    WHERE id = @userId AND [FIRMA_ID] = @firmaId;";
                await using var userUpdateCommand = new SqlCommand(userUpdateSql, connection, (SqlTransaction)transaction);
                userUpdateCommand.Parameters.AddWithValue("@nightlyLimit", model.NightlyLimit.HasValue ? (object)model.NightlyLimit.Value : DBNull.Value);
                userUpdateCommand.Parameters.AddWithValue("@approvalRequired", model.ApprovalRequired ? 1 : 0);
                userUpdateCommand.Parameters.AddWithValue("@userId", model.UserId.Value);
                userUpdateCommand.Parameters.AddWithValue("@firmaId", context.FirmaId);
                await userUpdateCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
            return (true, "Limit ve onay ayarı kaydedildi.");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            return (false, $"Limit kaydı sırasında hata oluştu: {ex.Message}");
        }
    }

    public async Task<(bool Success, string Message)> UpdateReservationApprovalAsync(long userId, FirmaReservationDecisionModel model, CancellationToken cancellationToken = default)
    {
        if (model.ReservationId <= 0)
        {
            return (false, "Geçerli bir rezervasyon seçilmedi.");
        }

        var isApprove = string.Equals(model.Decision, "approve", StringComparison.OrdinalIgnoreCase);
        var isReject = string.Equals(model.Decision, "reject", StringComparison.OrdinalIgnoreCase);
        if (!isApprove && !isReject)
        {
            return (false, "Geçerli bir onay kararı seçilmedi.");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        var context = await BuildContextAsync(connection, userId, string.Empty, string.Empty, "limits", cancellationToken);

        const string updateSql = @"
            UPDATE [dbo].[REZERVASYONLAR]
            SET [FIRMA_ONAY_DURUMU] = @approvalStatus,
                [FIRMA_ONAYLAYAN_KULLANICI_ID] = @userId,
                [FIRMA_ONAY_TARIHI] = CURRENT_TIMESTAMP,
                [DURUM] = @reservationStatus,
                [IPTAL_NEDENI] = @cancelReason
            WHERE id = @reservationId
              AND [FIRMA_ID] = @firmaId
              AND [FIRMA_ONAY_DURUMU] = 'Beklemede';";

        await using var command = new SqlCommand(updateSql, connection);
        command.Parameters.AddWithValue("@approvalStatus", isApprove ? "Onaylandı" : "Reddedildi");
        command.Parameters.AddWithValue("@reservationStatus", isApprove ? "Onaylandı" : "İptal Edildi");
        command.Parameters.AddWithValue("@cancelReason", isReject ? (object)"Firma paneli üzerinden rezervasyon reddedildi." : DBNull.Value);
        command.Parameters.AddWithValue("@userId", userId);
        command.Parameters.AddWithValue("@reservationId", model.ReservationId);
        command.Parameters.AddWithValue("@firmaId", context.FirmaId);

        var affected = await command.ExecuteNonQueryAsync(cancellationToken);
        return affected > 0
            ? (true, isApprove ? "Rezervasyon firma tarafından onaylandı." : "Rezervasyon firma tarafından reddedildi.")
            : (false, "Rezervasyon bulunamadı veya daha önce işlem görmüş.");
    }

    private async Task<FirmaContext> BuildContextAsync(SqlConnection connection, long userId, string title, string subtitle, string activeSectionKey, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT f.id, f.[FIRMA_ADI], COALESCE(f.[ONAY_DURUMU], 'Beklemede') AS [ONAY_DURUMU],
                   u.[AD_SOYAD], u.[EPOSTA], u.[ROL],
                   (SELECT COUNT(*) FROM [dbo].[KULLANICILAR] fu WHERE fu.[FIRMA_ID] = f.id AND fu.[ROL] LIKE 'firma_%' AND fu.[HESAP_DURUMU] = 1) AS employee_count,
                   (SELECT COUNT(DISTINCT CONCAT(ff.[OTEL_ID], ':', ff.[ODA_TIP_ID])) FROM [dbo].[FIRMA_ODA_FIYAT_MUSAITLIK] ff WHERE ff.[AKTIF_MI] = 1 AND ff.[KAPALI_SATIS] = 0) AS deal_count,
                   (SELECT COUNT(*) FROM [dbo].[REZERVASYONLAR] r WHERE r.[FIRMA_ID] = f.id AND r.[FIRMA_ONAY_DURUMU] = 'Beklemede') AS pending_count
            FROM [dbo].[KULLANICILAR] u
            INNER JOIN [dbo].[FIRMALAR] f ON f.id = u.[FIRMA_ID]
            WHERE u.id = @userId
            ORDER BY u.id
            OFFSET 0 ROWS FETCH NEXT 1 ROWS ONLY;";

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@userId", userId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            throw new InvalidOperationException("Firma paneli için aktif firma bağlantısı bulunamadı.");
        }

        var shell = new FirmaPanelShellViewModel
        {
            UserId = userId,
            FirmaId = reader.GetInt64(0),
            CompanyName = reader.GetString(1),
            CompanyBadgeText = reader.IsDBNull(2) ? "Beklemede" : reader.GetString(2),
            FullName = reader.GetString(3),
            Email = reader.GetString(4),
            UserRole = reader.GetString(5),
            EmployeeCount = SafeInt(reader, 6),
            DealCount = SafeInt(reader, 7),
            PendingApprovalCount = SafeInt(reader, 8),
            PanelTitle = title,
            PanelSubtitle = subtitle,
            ActiveSectionKey = activeSectionKey
        };

        return new FirmaContext(shell.FirmaId, shell);
    }

    private static string NormalizeFirmaRole(string? role)
        => role?.Trim() switch
        {
            "firma_admin" => "firma_admin",
            "firma_manager" => "firma_manager",
            _ => "firma_staff"
        };

    private static string BuildPersonelCode(long firmaId)
        => $"FRM{firmaId:D3}-{DateTime.UtcNow:HHmmss}";

    private static string GetRoleLabel(string role)
        => role switch
        {
            "firma_admin" => "Firma Yöneticisi",
            "firma_manager" => "Departman Yöneticisi",
            "firma_staff" => "Firma Personeli",
            _ => role
        };

    private static async Task<List<string>> LoadDealCitiesAsync(SqlConnection connection, long firmaId, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT DISTINCT ot.[SEHIR]
            FROM [dbo].[FIRMA_ODA_FIYAT_MUSAITLIK] f
            INNER JOIN [dbo].[OTELLER] ot ON ot.id = f.[OTEL_ID]
            WHERE f.[AKTIF_MI] = 1
              AND ot.[SEHIR] IS NOT NULL
              AND ot.[SEHIR] <> ''
            ORDER BY ot.[SEHIR];";

        var cities = new List<string>();
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            cities.Add(reader.GetString(0));
        }

        return cities;
    }

    private static async Task<List<string>> LoadDealDistrictsAsync(SqlConnection connection, long firmaId, string? city, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT DISTINCT ot.[ILCE]
            FROM [dbo].[FIRMA_ODA_FIYAT_MUSAITLIK] f
            INNER JOIN [dbo].[OTELLER] ot ON ot.id = f.[OTEL_ID]
            WHERE f.[AKTIF_MI] = 1
              AND (@city IS NULL OR ot.[SEHIR] = @city)
              AND ot.[ILCE] IS NOT NULL
              AND ot.[ILCE] <> ''
            ORDER BY ot.[ILCE];";

        var items = new List<string>();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@city", string.IsNullOrWhiteSpace(city) ? DBNull.Value : (object)city);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(reader.GetString(0));
        }

        return items;
    }

    private static async Task<List<string>> LoadDealNeighborhoodsAsync(SqlConnection connection, long firmaId, string? city, string? district, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT DISTINCT ot.[MAHALLE]
            FROM [dbo].[FIRMA_ODA_FIYAT_MUSAITLIK] f
            INNER JOIN [dbo].[OTELLER] ot ON ot.id = f.[OTEL_ID]
            WHERE f.[AKTIF_MI] = 1
              AND (@city IS NULL OR ot.[SEHIR] = @city)
              AND (@district IS NULL OR ot.[ILCE] = @district)
              AND ot.[MAHALLE] IS NOT NULL
              AND ot.[MAHALLE] <> ''
            ORDER BY ot.[MAHALLE];";

        var items = new List<string>();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@city", string.IsNullOrWhiteSpace(city) ? DBNull.Value : (object)city);
        command.Parameters.AddWithValue("@district", string.IsNullOrWhiteSpace(district) ? DBNull.Value : (object)district);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(reader.GetString(0));
        }

        return items;
    }

    private async Task<List<FirmaPanelDealRowViewModel>> LoadDealsAsync(SqlConnection connection, long firmaId, int take, string? city = null, string? district = null, string? neighborhood = null, int? minRoomCount = null, string? search = null, CancellationToken cancellationToken = default)
    {
        // Firma paneli "Kurumsal Otel Fiyatları": partnerin firma_oda_fiyat_musaitlik tablosuna girdiği
        // fiyatları özetleyerek gösterir. Burada amaç; firmaya özel fiyat varsa onu, yoksa standart fiyatı
        // kıyaslayıp rezervasyon akışına yönlendirmektir.
        const string sql = @"
            SELECT TOP (@take)
                   ROW_NUMBER() OVER (ORDER BY ot.[OTEL_ADI] ASC, od.[ODA_ADI] ASC) AS deal_id,
                   ot.id AS hotel_id,
                   x.[ODA_TIP_ID] AS room_type_id,
                   ot.[OTEL_ADI],
                   COALESCE(od.[ODA_ADI], N'') AS [ODA_ADI],
                   CONCAT(COALESCE(ot.[ILCE], N''), CASE WHEN COALESCE(ot.[ILCE], N'') <> '' THEN N', ' ELSE N'' END, COALESCE(ot.[SEHIR], N'')) AS city_text,
                   COALESCE(od.[MAKSIMUM_KISI_SAYISI], 0) AS max_guest,
                   COALESCE(od.[MAKSIMUM_YETISKIN_SAYISI], 0) AS max_adult,
                   COALESCE(od.[MAKSIMUM_COCUK_SAYISI], 0) AS max_child,
                   COALESCE(od.[TOPLAM_ODA_SAYISI], 0) AS total_room_count,
                   COALESCE(std.base_price, 0) AS standard_price,
                   COALESCE(corp.corp_price, 0) AS corporate_price,
                   COALESCE(corp.min_date, CAST(NULL AS date)) AS min_date,
                   COALESCE(corp.max_date, CAST(NULL AS date)) AS max_date
            FROM (
                SELECT DISTINCT f.[OTEL_ID], f.[ODA_TIP_ID]
                FROM [dbo].[FIRMA_ODA_FIYAT_MUSAITLIK] f
                WHERE f.[AKTIF_MI] = 1
                  AND f.[KAPALI_SATIS] = 0
            ) x
            INNER JOIN [dbo].[OTELLER] ot ON ot.id = x.[OTEL_ID]
            LEFT JOIN [dbo].[ODA_TIPLERI] od ON od.id = x.[ODA_TIP_ID]
            OUTER APPLY (
                SELECT
                    MIN(CASE WHEN f.[FIRMA_GECELIK_FIYAT] > 0 THEN f.[FIRMA_GECELIK_FIYAT] ELSE NULL END) AS corp_price,
                    MIN(f.[TARIH]) AS min_date,
                    MAX(f.[TARIH]) AS max_date
                FROM [dbo].[FIRMA_ODA_FIYAT_MUSAITLIK] f
                WHERE f.[AKTIF_MI] = 1
                  AND f.[KAPALI_SATIS] = 0
                  AND f.[OTEL_ID] = x.[OTEL_ID]
                  AND f.[ODA_TIP_ID] = x.[ODA_TIP_ID]
            ) corp
            OUTER APPLY (
                SELECT
                    MIN(
                        CASE
                            WHEN ofm.[INDIRIMLI_FIYAT] IS NOT NULL AND ofm.[INDIRIMLI_FIYAT] > 0 AND ofm.[INDIRIMLI_FIYAT] < ofm.[GECELIK_FIYAT] THEN ofm.[INDIRIMLI_FIYAT]
                            ELSE ofm.[GECELIK_FIYAT]
                        END
                    ) AS base_price
                FROM [dbo].[ODA_FIYAT_MUSAITLIK] ofm
                WHERE ofm.[OTEL_ID] = x.[OTEL_ID]
                  AND ofm.[ODA_TIP_ID] = x.[ODA_TIP_ID]
            ) std
            WHERE (@city IS NULL OR ot.[SEHIR] = @city)
              AND (@district IS NULL OR ot.[ILCE] = @district)
              AND (@neighborhood IS NULL OR ot.[MAHALLE] = @neighborhood)
              AND (@minRoomCount IS NULL OR COALESCE(od.[TOPLAM_ODA_SAYISI], 0) >= @minRoomCount)
              AND (@search IS NULL OR ot.[OTEL_ADI] LIKE '%' + @search + '%' OR ot.[SEHIR] LIKE '%' + @search + '%' OR ot.[ILCE] LIKE '%' + @search + '%' OR ot.[MAHALLE] LIKE '%' + @search + '%')
            ORDER BY ot.[ONE_CIKAN_OTEL] DESC, ot.[OTEL_ADI] ASC, od.[ODA_ADI] ASC;";

        var items = new List<FirmaPanelDealRowViewModel>();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@take", take);
        command.Parameters.AddWithValue("@city", string.IsNullOrWhiteSpace(city) ? DBNull.Value : (object)city);
        command.Parameters.AddWithValue("@district", string.IsNullOrWhiteSpace(district) ? DBNull.Value : (object)district);
        command.Parameters.AddWithValue("@neighborhood", string.IsNullOrWhiteSpace(neighborhood) ? DBNull.Value : (object)neighborhood);
        command.Parameters.AddWithValue("@minRoomCount", minRoomCount.HasValue ? (object)minRoomCount.Value : DBNull.Value);
        command.Parameters.AddWithValue("@search", string.IsNullOrWhiteSpace(search) ? DBNull.Value : (object)search.Trim());
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var maxGuest = SafeInt(reader, 6);
            var maxAdult = SafeInt(reader, 7);
            var maxChild = SafeInt(reader, 8);
            var totalRoomCount = SafeInt(reader, 9);
            var standardPrice = SafeDecimal(reader, 10);
            var corporatePrice = SafeDecimal(reader, 11);
            var capacityText = maxGuest > 0
                ? $"{maxGuest} kişi (Y{Math.Max(0, maxAdult)} / Ç{Math.Max(0, maxChild)})"
                : "-";
            var stockText = totalRoomCount > 0 ? $"{totalRoomCount} oda" : "-";

            items.Add(new FirmaPanelDealRowViewModel
            {
                DealId = reader.GetInt64(0),
                HotelId = reader.GetInt64(1),
                RoomTypeId = reader.GetInt64(2),
                HotelName = reader.GetString(3),
                RoomName = reader.IsDBNull(4) || string.IsNullOrWhiteSpace(reader.GetString(4)) ? "Oda" : reader.GetString(4),
                CityText = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
                CapacityText = capacityText,
                StockText = stockText,
                StandardPriceText = FormatMoney(standardPrice),
                CorporatePriceText = FormatMoney(corporatePrice),
                DiscountText = standardPrice > 0m && corporatePrice > 0m && corporatePrice < standardPrice
                    ? $"%{Math.Clamp((int)Math.Round(((standardPrice - corporatePrice) / standardPrice) * 100m, MidpointRounding.AwayFromZero), 1, 95)}"
                    : "-",
                MinimumRoomText = "Kurumsal",
                ValidityText = reader.IsDBNull(12) || reader.IsDBNull(13)
                    ? "Tarih aralığı tanımlı"
                    : $"{reader.GetDateTime(12):dd.MM.yyyy} - {reader.GetDateTime(13):dd.MM.yyyy}",
                SavingsText = FormatMoney(Math.Max(0m, standardPrice - corporatePrice))
            });
        }
        return items;
    }

    private async Task<List<FirmaPanelEmployeeRowViewModel>> LoadEmployeesAsync(SqlConnection connection, long firmaId, int take, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT u.id, u.[AD_SOYAD], COALESCE(u.[DEPARTMAN], 'Tanımsız'), COALESCE(u.[GOREV_UNVANI], u.[ROL]), u.[EPOSTA],
                   u.[HARCAMA_LIMITI], u.[ONAY_GEREKSINIMI], u.[ROL], u.[FIRMA_YONETICI_MI],
                   u.[TELEFON_DOGRULAMA_TARIHI], u.[TELEFON_SON_SAHIPLIK_TEYIT_TARIHI], COALESCE(u.[TELEFON_DOGRULAMA_DURUMU], ''),
                   COUNT(r.id) AS [REZERVASYON_SAYISI], COALESCE(SUM(r.[TOPLAM_TUTAR]), 0) AS harcama_toplami
            FROM [dbo].[KULLANICILAR] u
            LEFT JOIN [dbo].[REZERVASYONLAR] r ON r.[FIRMA_CALISAN_ID] = u.id
            WHERE u.[FIRMA_ID] = @firmaId AND u.[ROL] LIKE 'firma_%'
            GROUP BY u.id, u.[AD_SOYAD], u.[DEPARTMAN], u.[GOREV_UNVANI], u.[EPOSTA], u.[HARCAMA_LIMITI], u.[ONAY_GEREKSINIMI], u.[ROL], u.[FIRMA_YONETICI_MI],
                     u.[TELEFON_DOGRULAMA_TARIHI], u.[TELEFON_SON_SAHIPLIK_TEYIT_TARIHI], u.[TELEFON_DOGRULAMA_DURUMU]
            ORDER BY u.[FIRMA_YONETICI_MI] DESC, u.[AD_SOYAD] ASC
            OFFSET 0 ROWS FETCH NEXT @take ROWS ONLY;";

        var items = new List<FirmaPanelEmployeeRowViewModel>();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@firmaId", firmaId);
        command.Parameters.AddWithValue("@take", take);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var fullName = reader.GetString(1);
            var verifiedAt = reader.IsDBNull(9) ? (DateTime?)null : reader.GetDateTime(9);
            var ownershipAt = reader.IsDBNull(10) ? (DateTime?)null : reader.GetDateTime(10);
            var phoneStatus = reader.IsDBNull(11) ? string.Empty : reader.GetString(11);
            var isPhoneVerified = verifiedAt.HasValue
                && (!ownershipAt.HasValue || ownershipAt.Value >= DateTime.UtcNow.AddDays(-180));
            items.Add(new FirmaPanelEmployeeRowViewModel
            {
                UserId = reader.GetInt64(0),
                FullName = fullName,
                Department = reader.GetString(2),
                Title = reader.GetString(3),
                Email = reader.GetString(4),
                LimitText = reader.IsDBNull(5) ? "-" : FormatMoney(reader.GetDecimal(5)),
                ApprovalText = SafeBool(reader, 6) ? "Onaylı akış" : "Serbest rezervasyon",
                Initials = GetInitials(fullName),
                RoleText = GetRoleLabel(reader.IsDBNull(7) ? string.Empty : reader.GetString(7)),
                IsManager = SafeBool(reader, 8),
                IsPhoneVerified = isPhoneVerified,
                PhoneVerificationText = isPhoneVerified
                    ? $"Telefon doğrulandı · {verifiedAt!.Value.ToLocalTime():dd.MM.yyyy}"
                    : string.Equals(phoneStatus, "Beklemede", StringComparison.OrdinalIgnoreCase)
                        ? "Telefon doğrulaması bekleniyor"
                        : "Telefon doğrulanmadı",
                PhoneVerificationToneClass = isPhoneVerified ? "success" : string.Equals(phoneStatus, "Beklemede", StringComparison.OrdinalIgnoreCase) ? "warning" : "secondary",
                ReservationCountText = SafeInt(reader, 12).ToString(CultureInfo.InvariantCulture),
                SpendText = FormatMoney(SafeDecimal(reader, 13))
            });
        }
        return items;
    }

    private async Task<List<FirmaPanelReservationRowViewModel>> LoadReservationsAsync(SqlConnection connection, long firmaId, int take, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT r.id, r.[REZERVASYON_NO], COALESCE(u.[AD_SOYAD], r.[MISAFIR_AD_SOYAD]) AS employee_name, o.[OTEL_ADI],
                   CONCAT(o.[ILCE], ', ', o.[SEHIR]) AS city_text,
                   r.[GIRIS_TARIHI], r.[CIKIS_TARIHI], r.[DURUM], r.[FIRMA_ONAY_DURUMU], r.[TOPLAM_TUTAR]
            FROM [dbo].[REZERVASYONLAR] r
            INNER JOIN [dbo].[OTELLER] o ON o.id = r.[OTEL_ID]
            LEFT JOIN [dbo].[KULLANICILAR] u ON u.id = r.[FIRMA_CALISAN_ID]
            WHERE r.[FIRMA_ID] = @firmaId
            ORDER BY r.[OLUSTURULMA_TARIHI] DESC, r.id DESC
            OFFSET 0 ROWS FETCH NEXT @take ROWS ONLY;";

        var items = new List<FirmaPanelReservationRowViewModel>();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@firmaId", firmaId);
        command.Parameters.AddWithValue("@take", take);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new FirmaPanelReservationRowViewModel
            {
                ReservationId = reader.GetInt64(0),
                ReservationNo = reader.GetString(1),
                EmployeeName = reader.GetString(2),
                HotelName = reader.GetString(3),
                HotelCityText = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                StayText = $"{reader.GetDateTime(5):dd.MM.yyyy} - {reader.GetDateTime(6):dd.MM.yyyy}",
                StatusText = reader.GetString(7),
                ApprovalText = reader.IsDBNull(8) ? "-" : reader.GetString(8),
                TotalText = FormatMoney(SafeDecimal(reader, 9)),
                CanApprove = string.Equals(reader.IsDBNull(8) ? null : reader.GetString(8), "Beklemede", StringComparison.OrdinalIgnoreCase)
            });
        }
        return items;
    }

    private async Task<List<FirmaPanelReservationRowViewModel>> LoadPendingApprovalsAsync(SqlConnection connection, long firmaId, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT r.id, r.[REZERVASYON_NO], COALESCE(u.[AD_SOYAD], r.[MISAFIR_AD_SOYAD]) AS employee_name, o.[OTEL_ADI],
                   CONCAT(o.[ILCE], ', ', o.[SEHIR]) AS city_text,
                   r.[GIRIS_TARIHI], r.[CIKIS_TARIHI], r.[DURUM], r.[FIRMA_ONAY_DURUMU], r.[TOPLAM_TUTAR]
            FROM [dbo].[REZERVASYONLAR] r
            INNER JOIN [dbo].[OTELLER] o ON o.id = r.[OTEL_ID]
            LEFT JOIN [dbo].[KULLANICILAR] u ON u.id = r.[FIRMA_CALISAN_ID]
            WHERE r.[FIRMA_ID] = @firmaId AND r.[FIRMA_ONAY_DURUMU] = 'Beklemede'
            ORDER BY r.[OLUSTURULMA_TARIHI] DESC, r.id DESC
            OFFSET 0 ROWS FETCH NEXT 20 ROWS ONLY;";

        var items = new List<FirmaPanelReservationRowViewModel>();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@firmaId", firmaId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new FirmaPanelReservationRowViewModel
            {
                ReservationId = reader.GetInt64(0),
                ReservationNo = reader.GetString(1),
                EmployeeName = reader.GetString(2),
                HotelName = reader.GetString(3),
                HotelCityText = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                StayText = $"{reader.GetDateTime(5):dd.MM.yyyy} - {reader.GetDateTime(6):dd.MM.yyyy}",
                StatusText = reader.GetString(7),
                ApprovalText = reader.IsDBNull(8) ? "-" : reader.GetString(8),
                TotalText = FormatMoney(SafeDecimal(reader, 9)),
                CanApprove = true
            });
        }
        return items;
    }

    private async Task LoadDashboardExtrasAsync(SqlConnection connection, long firmaId, FirmaDashboardPageViewModel model, CancellationToken cancellationToken)
    {
        var tr = CultureInfo.GetCultureInfo("tr-TR");
        model.CurrentMonthLabel = DateTime.Now.ToString("MMMM yyyy", tr);

        decimal monthSpend = 0m;
        decimal monthSave = 0m;

        const string monthSql = @"
            SELECT
                COALESCE(SUM([TOPLAM_TUTAR]), 0),
                COALESCE(SUM([TOPLAM_TASARRUF]), 0),
                COUNT(*)
            FROM [dbo].[REZERVASYONLAR]
            WHERE [FIRMA_ID] = @firmaId
              AND [OLUSTURULMA_TARIHI] >= DATEFROMPARTS(YEAR(GETDATE()), MONTH(GETDATE()), 1)
              AND [OLUSTURULMA_TARIHI] < DATEADD(MONTH, 1, DATEFROMPARTS(YEAR(GETDATE()), MONTH(GETDATE()), 1));";

        await using (var cmd = new SqlCommand(monthSql, connection))
        {
            cmd.Parameters.AddWithValue("@firmaId", firmaId);
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                monthSpend = SafeDecimal(reader, 0);
                monthSave = SafeDecimal(reader, 1);
                model.MonthReservationCount = SafeInt(reader, 2);
            }
        }

        model.MonthSpendTotalText = FormatMoney(monthSpend);
        model.MonthSavingsText = FormatMoney(monthSave);

        const string trendSql = @"
            SELECT FORMAT([OLUSTURULMA_TARIHI], 'MMM', 'tr-TR') AS ay, COALESCE(SUM([TOPLAM_TUTAR]), 0) AS toplam, COUNT(*) AS [REZERVASYON_SAYISI]
            FROM [dbo].[REZERVASYONLAR]
            WHERE [FIRMA_ID] = @firmaId
              AND [OLUSTURULMA_TARIHI] >= DATEADD(MONTH, -5, CAST(SYSUTCDATETIME() AS date))
            GROUP BY YEAR([OLUSTURULMA_TARIHI]), MONTH([OLUSTURULMA_TARIHI]), FORMAT([OLUSTURULMA_TARIHI], 'MMM', 'tr-TR')
            ORDER BY YEAR([OLUSTURULMA_TARIHI]), MONTH([OLUSTURULMA_TARIHI]);";

        await using (var trendCmd = new SqlCommand(trendSql, connection))
        {
            trendCmd.Parameters.AddWithValue("@firmaId", firmaId);
            await using var reader = await trendCmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                model.SpendTrend.Add(new FirmaMonthlySpendRowViewModel
                {
                    Label = reader.GetString(0),
                    Amount = SafeDecimal(reader, 1),
                    ReservationCount = SafeInt(reader, 2)
                });
            }
        }

        var maxAmount = Math.Max(1m, model.SpendTrend.Count == 0 ? 0m : model.SpendTrend.Max(static x => x.Amount));
        foreach (var item in model.SpendTrend)
        {
            item.HeightPercent = Math.Max(16, (int)Math.Round(item.Amount * 100m / maxAmount));
        }

        try
        {
            const string limitSql = @"
                SELECT TOP (1) [AYLIK_LIMIT]
                FROM [dbo].[FIRMA_HARCAMA_LIMITLERI]
                WHERE [FIRMA_ID] = @firmaId AND [AKTIF_MI] = 1
                  AND [KULLANICI_ID] IS NULL AND [DEPARTMAN] IS NULL AND [AYLIK_LIMIT] IS NOT NULL
                ORDER BY id ASC;";

            await using var limCmd = new SqlCommand(limitSql, connection);
            limCmd.Parameters.AddWithValue("@firmaId", firmaId);
            var limObj = await limCmd.ExecuteScalarAsync(cancellationToken);
            if (limObj is not null && limObj != DBNull.Value)
            {
                var lim = Convert.ToDecimal(limObj, CultureInfo.InvariantCulture);
                if (lim > 0m)
                {
                    if (monthSpend > lim)
                    {
                        model.LimitAlerts.Add(
                            $"Bu ay kurumsal harcamanız ({FormatMoney(monthSpend)}), firma geneli aylık limitinizi ({FormatMoney(lim)}) aştı. Limitler & Onaylar üzerinden güncelleyebilirsiniz.");
                    }
                    else if (monthSpend >= lim * 0.9m)
                    {
                        model.LimitAlerts.Add(
                            $"Bu ay harcama ({FormatMoney(monthSpend)}), aylık limitinize ({FormatMoney(lim)}) yaklaştı (%90 üzeri).");
                    }
                }
            }
        }
        catch (SqlException ex) when (IsMissingTableOrColumn(ex))
        {
            // Şema yoksa sessiz
        }
    }

    private static decimal ParseMoneyAverage(IEnumerable<string> moneyValues)
    {
        var values = moneyValues
            .Where(static value => !string.IsNullOrWhiteSpace(value) && value != "-")
            .Select(static value =>
            {
                var cleaned = value.Replace("₺", string.Empty, StringComparison.Ordinal)
                    .Replace(".", string.Empty, StringComparison.Ordinal)
                    .Replace(",", ".", StringComparison.Ordinal)
                    .Trim();
                return decimal.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed) ? parsed : 0m;
            })
            .Where(static value => value > 0m)
            .ToList();

        return values.Count == 0 ? 0m : values.Average();
    }

    private static int SafeInt(SqlDataReader reader, int ordinal)
        => reader.IsDBNull(ordinal) ? 0 : Convert.ToInt32(reader.GetValue(ordinal), CultureInfo.InvariantCulture);

    private static bool SafeBool(SqlDataReader reader, int ordinal)
        => !reader.IsDBNull(ordinal) && Convert.ToInt32(reader.GetValue(ordinal), CultureInfo.InvariantCulture) == 1;

    private static decimal SafeDecimal(SqlDataReader reader, int ordinal)
        => reader.IsDBNull(ordinal) ? 0m : Convert.ToDecimal(reader.GetValue(ordinal), CultureInfo.InvariantCulture);

    private static bool IsMissingTableOrColumn(SqlException ex)
    {
        var msg = ex.Message ?? string.Empty;
        return msg.Contains("Invalid object name", StringComparison.OrdinalIgnoreCase)
               || msg.Contains("Invalid column name", StringComparison.OrdinalIgnoreCase);
    }

    private static string FormatMoney(decimal value)
        => string.Format(CultureInfo.GetCultureInfo("tr-TR"), "{0:C0}", value);

    private static string GetInitials(string fullName)
        => string.Concat(fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(2).Select(static x => x[0])).ToUpperInvariant();

    private sealed record FirmaContext(long FirmaId, FirmaPanelShellViewModel Shell);
}
