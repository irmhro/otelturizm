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
using otelturizmnew.Services.Abstractions;

namespace otelturizmnew.Services;

public class FirmaService : IFirmaService
{
    private readonly string _connectionString;
    private readonly IMessageCenterService _messageCenterService;
    private readonly IEmailQueueService _emailQueueService;

    public FirmaService(IConfiguration configuration, IMessageCenterService messageCenterService, IEmailQueueService emailQueueService)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection tanimli degil.");
        _messageCenterService = messageCenterService;
        _emailQueueService = emailQueueService;
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
                (SELECT COUNT(*) FROM firmalar WHERE aktif_mi = 1 AND onay_durumu = 'Onaylandı') AS active_companies,
                (SELECT COUNT(DISTINCT otel_id) FROM firma_ozel_fiyatlar WHERE aktif_mi = 1) AS contracted_hotels,
                (SELECT COALESCE(MAX(indirim_orani), 0) FROM firma_ozel_fiyatlar WHERE aktif_mi = 1) AS max_discount;";

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
            SELECT TOP (6) ot.id, ot.otel_adi, ot.sehir, od.standart_gecelik_fiyat, foz.ozel_fiyat, foz.indirim_orani, foz.minimum_oda_sayisi
            FROM firma_ozel_fiyatlar foz
            INNER JOIN oteller ot ON ot.id = foz.otel_id
            LEFT JOIN oda_tipleri od ON od.id = foz.oda_tip_id
            WHERE foz.aktif_mi = 1
            ORDER BY foz.indirim_orani DESC, foz.ozel_fiyat ASC;";

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
                (SELECT COUNT(*) FROM users u WHERE u.firma_id = @firmaId AND u.rol LIKE 'firma_%' AND u.hesap_durumu = 1) AS employee_count,
                (SELECT COUNT(*) FROM firma_ozel_fiyatlar f WHERE f.firma_id = @firmaId AND f.aktif_mi = 1) AS deal_count,
                (SELECT COUNT(*) FROM rezervasyonlar r WHERE r.firma_id = @firmaId) AS reservation_count,
                (SELECT COUNT(*) FROM rezervasyonlar r WHERE r.firma_id = @firmaId AND r.firma_onay_durumu = 'Beklemede') AS pending_approval_count;";

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

        model.HighlightDeals = await LoadDealsAsync(connection, context.FirmaId, 4, null, null, null, cancellationToken);
        model.FeaturedEmployees = await LoadEmployeesAsync(connection, context.FirmaId, 4, cancellationToken);
        model.RecentReservations = await LoadReservationsAsync(connection, context.FirmaId, 6, cancellationToken);
        return model;
    }

    public async Task<FirmaDealsPageViewModel> GetDealsAsync(long userId, string? city = null, int? minRoomCount = null, string? search = null, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        var context = await BuildContextAsync(connection, userId, "Firma Fiyatları", "Otellerin firmanız için tanımladığı özel kurumsal fiyatları canlı takip edin.", "deals", cancellationToken);
        var deals = await LoadDealsAsync(connection, context.FirmaId, 100, city, minRoomCount, search, cancellationToken);
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
            Filter = new FirmaDealsFilterModel { City = city, MinRoomCount = minRoomCount, Search = search },
            AvailableCities = await LoadDealCitiesAsync(connection, context.FirmaId, cancellationToken),
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
            SELECT id, otel_adi, CONCAT(COALESCE(ilce, N''), CASE WHEN COALESCE(ilce, N'') <> '' THEN N', ' ELSE N'' END, COALESCE(sehir, N'')) AS city_text
            FROM dbo.oteller
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
                SELECT value AS otel_id
                FROM OPENJSON(@ids)
            ),
            corp AS (
                SELECT f.otel_id, f.oda_tip_id,
                       MIN(CASE WHEN f.firma_gecelik_fiyat > 0 THEN f.firma_gecelik_fiyat ELSE NULL END) AS corp_price,
                       MIN(f.tarih) AS min_date,
                       MAX(f.tarih) AS max_date
                FROM dbo.firma_oda_fiyat_musaitlik f
                INNER JOIN sel s ON s.otel_id = f.otel_id
                WHERE f.firma_id = @firmaId
                  AND f.aktif_mi = 1
                  AND f.kapali_satis = 0
                GROUP BY f.otel_id, f.oda_tip_id
            ),
            std AS (
                SELECT ofm.otel_id, ofm.oda_tip_id,
                       MIN(
                            CASE
                                WHEN ofm.indirimli_fiyat IS NOT NULL AND ofm.indirimli_fiyat > 0 AND ofm.indirimli_fiyat < ofm.gecelik_fiyat THEN ofm.indirimli_fiyat
                                ELSE ofm.gecelik_fiyat
                            END
                       ) AS std_price
                FROM dbo.oda_fiyat_musaitlik ofm
                INNER JOIN sel s ON s.otel_id = ofm.otel_id
                GROUP BY ofm.otel_id, ofm.oda_tip_id
            )
            SELECT c.otel_id, c.oda_tip_id, COALESCE(od.oda_adi, N'Oda') AS room_name,
                   COALESCE(c.corp_price, 0) AS corp_price,
                   COALESCE(s.std_price, 0) AS std_price,
                   c.min_date, c.max_date
            FROM corp c
            LEFT JOIN std s ON s.otel_id = c.otel_id AND s.oda_tip_id = c.oda_tip_id
            LEFT JOIN dbo.oda_tipleri od ON od.id = c.oda_tip_id
            ORDER BY c.otel_id ASC, corp_price ASC;";

        var culture = CultureInfo.GetCultureInfo("tr-TR");
        await using (var cmd = new SqlCommand(compareSql, connection))
        {
            cmd.Parameters.AddWithValue("@ids", JsonSerializer.Serialize(normalized));
            cmd.Parameters.AddWithValue("@firmaId", context.FirmaId);
            await using var r = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await r.ReadAsync(cancellationToken))
            {
                var corpPrice = SafeDecimal(r, 3);
                var stdPrice = SafeDecimal(r, 4);
                string discountText = "-";
                if (stdPrice > 0m && corpPrice > 0m && corpPrice < stdPrice)
                {
                    var pct = Math.Clamp((int)Math.Round(((stdPrice - corpPrice) / stdPrice) * 100m, MidpointRounding.AwayFromZero), 1, 95);
                    discountText = $"%{pct}";
                }

                string validityText;
                if (!r.IsDBNull(5) && !r.IsDBNull(6))
                {
                    validityText = $"{r.GetDateTime(5):dd.MM.yyyy} - {r.GetDateTime(6):dd.MM.yyyy}";
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

    public async Task<FirmaCreateReservationPageViewModel> GetCreateReservationAsync(long userId, long? hotelId = null, long? roomTypeId = null, string? search = null, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        var context = await BuildContextAsync(connection, userId, "Yeni Rezervasyon", "Firma fiyatı ile standart fiyatı karşılaştırıp tek ekranda rezervasyon açın.", "create-reservation", cancellationToken);

        var model = new FirmaCreateReservationPageViewModel { Shell = context.Shell };
        model.Form.HotelId = hotelId.GetValueOrDefault();
        model.Form.RoomTypeId = roomTypeId.GetValueOrDefault();
        model.HotelSearch = search;

        model.Employees = (await LoadEmployeesAsync(connection, context.FirmaId, 200, cancellationToken))
            .Select(static item => new FirmaEmployeeOptionViewModel
            {
                UserId = item.UserId,
                FullName = item.FullName,
                Email = item.Email,
                Department = item.Department
            })
            .ToList();

        // Hotels: approved/published + optional search (otel/sehir/ilce/mahalle)
        const string hotelsSql = @"
            SELECT TOP (300) id, CONCAT(otel_adi, ' · ', ilce, ', ', sehir)
            FROM oteller
            WHERE onay_durumu = 'Onaylandı'
              AND yayin_durumu IN ('Yayında','Bakımda')
              AND (@q IS NULL OR @q = '' OR otel_adi LIKE '%' + @q + '%' OR sehir LIKE '%' + @q + '%' OR ilce LIKE '%' + @q + '%' OR mahalle LIKE '%' + @q + '%')
            ORDER BY one_cikan_otel DESC, otel_adi ASC;";
        await using (var cmd = new SqlCommand(hotelsSql, connection))
        await using (var reader = await cmd.ExecuteReaderAsync(cancellationToken))
        {
            cmd.Parameters.AddWithValue("@q", string.IsNullOrWhiteSpace(search) ? (object)DBNull.Value : search.Trim());
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
            SELECT id, oda_adi
            FROM oda_tipleri
            WHERE otel_id = @hotelId AND aktif_mi = 1
            ORDER BY oda_adi;";
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

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        var context = await BuildContextAsync(connection, userId, string.Empty, string.Empty, "create-reservation", cancellationToken);

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
                INSERT INTO rezervasyonlar
                (
                    rezervasyon_no, otel_id, oda_tip_id, kullanici_id,
                    firma_id, firma_calisan_id,
                    misafir_ad_soyad, misafir_eposta, misafir_telefon, misafir_notu,
                    giris_tarihi, cikis_tarihi, yetiskin_sayisi, cocuk_sayisi, oda_sayisi,
                    gecelik_fiyat, toplam_oda_tutari, vergi_tutari, toplam_tutar,
                    durum, rezervasyon_durumu_id, odeme_durumu, otel_onay_durumu, firma_onay_durumu,
                    kaynak, rezervasyon_kanali, musteri_talep_notu
                )
                VALUES
                (
                    @reservationNo, @hotelId, @roomTypeId, @userId,
                    @firmaId, @firmaEmployeeId,
                    @guestName, @guestEmail, @guestPhone, @note,
                    @checkIn, @checkOut, @adultCount, @childCount, @roomCount,
                    @nightlyPrice, @roomTotal, 0, @totalAmount,
                    'Onay Bekliyor', (SELECT TOP (1) id FROM dbo.rezervasyon_durum_tanimlari WHERE kod = N'OnayBekliyor'), 'Beklemede', 'Beklemede', 'Beklemede',
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
            return (true, $"Firma rezervasyonu oluşturuldu: {reservationNo}", reservationId);
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(cancellationToken);
            return (false, "Rezervasyon oluşturulamadı: " + ex.Message, null);
        }
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
            SELECT d.tarih,
                   f.firma_gecelik_fiyat,
                   f.kapali_satis,
                   CASE
                       WHEN ofm.indirimli_fiyat IS NOT NULL AND ofm.indirimli_fiyat > 0 AND ofm.indirimli_fiyat < ofm.gecelik_fiyat THEN ofm.indirimli_fiyat
                       ELSE ofm.gecelik_fiyat
                   END AS base_price
            FROM
            (
                SELECT DATEADD(DAY, v.number, @startDate) AS tarih
                FROM master..spt_values v
                WHERE v.type = 'P' AND v.number BETWEEN 0 AND DATEDIFF(DAY, @startDate, @endDate)
            ) d
            LEFT JOIN firma_oda_fiyat_musaitlik f
                ON f.firma_id = @firmaId AND f.otel_id=@hotelId AND f.oda_tip_id=@roomTypeId AND f.tarih = d.tarih
            LEFT JOIN oda_fiyat_musaitlik ofm
                ON ofm.otel_id=@hotelId AND ofm.oda_tip_id=@roomTypeId AND ofm.tarih = d.tarih
            ORDER BY d.tarih;";

        await using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@startDate", start.ToDateTime(TimeOnly.MinValue));
        cmd.Parameters.AddWithValue("@endDate", end.ToDateTime(TimeOnly.MinValue));
        cmd.Parameters.AddWithValue("@firmaId", firmaId);
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
            await using var cmd = new SqlCommand("SELECT TOP (1) COALESCE(NULLIF(ad_soyad,''),'Firma Personeli') FROM users WHERE id=@id;", connection, tx);
            cmd.Parameters.AddWithValue("@id", employeeUserId.Value);
            var raw = await cmd.ExecuteScalarAsync(cancellationToken);
            if (raw is not null and not DBNull) return Convert.ToString(raw, CultureInfo.InvariantCulture) ?? "Firma Personeli";
        }
        await using var firmCmd = new SqlCommand("SELECT TOP (1) COALESCE(NULLIF(firma_adi,''),'Firma') FROM firmalar WHERE id=@id;", connection, tx);
        firmCmd.Parameters.AddWithValue("@id", firmaId);
        var firmRaw = await firmCmd.ExecuteScalarAsync(cancellationToken);
        return firmRaw is null or DBNull ? "Firma" : Convert.ToString(firmRaw, CultureInfo.InvariantCulture) ?? "Firma";
    }

    private static async Task<(string? Email, string? CompanyName)> LoadCompanyContactAsync(SqlConnection connection, SqlTransaction tx, long firmaId, CancellationToken cancellationToken)
    {
        await using var cmd = new SqlCommand("SELECT TOP (1) COALESCE(NULLIF(firma_eposta,''), NULLIF(yetkili_eposta,''), NULL), COALESCE(NULLIF(firma_adi,''),NULL) FROM firmalar WHERE id=@id;", connection, tx);
        cmd.Parameters.AddWithValue("@id", firmaId);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) return (null, null);
        var email = reader.IsDBNull(0) ? null : reader.GetString(0);
        var name = reader.IsDBNull(1) ? null : reader.GetString(1);
        return (email, name);
    }

    private static async Task<string?> ResolveEmployeeEmailAsync(SqlConnection connection, SqlTransaction tx, long userId, CancellationToken cancellationToken)
    {
        await using var cmd = new SqlCommand("SELECT TOP (1) COALESCE(NULLIF(eposta,''), NULL) FROM users WHERE id=@id;", connection, tx);
        cmd.Parameters.AddWithValue("@id", userId);
        var raw = await cmd.ExecuteScalarAsync(cancellationToken);
        return raw is null or DBNull ? null : Convert.ToString(raw, CultureInfo.InvariantCulture);
    }

    private static async Task<string> LoadHotelNameAsync(SqlConnection connection, SqlTransaction tx, long hotelId, CancellationToken cancellationToken)
    {
        await using var cmd = new SqlCommand("SELECT TOP (1) otel_adi FROM oteller WHERE id=@id;", connection, tx);
        cmd.Parameters.AddWithValue("@id", hotelId);
        var raw = await cmd.ExecuteScalarAsync(cancellationToken);
        return raw is null or DBNull ? "Otel" : Convert.ToString(raw, CultureInfo.InvariantCulture) ?? "Otel";
    }

    private static async Task<(long UserId, string Email)> ResolvePartnerRecipientAsync(SqlConnection connection, SqlTransaction tx, long hotelId, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT TOP (1) COALESCE(o.user_id, oks.user_id, 1), COALESCE(o.satis_kontak_eposta, u.eposta, o.eposta, 'partner@otelturizm.com')
            FROM oteller o
            LEFT JOIN otel_kullanici_sahiplikleri oks ON oks.otel_id = o.id AND oks.aktif_mi = 1
            LEFT JOIN users u ON u.id = COALESCE(o.user_id, oks.user_id)
            WHERE o.id = @hotelId
            ORDER BY oks.ana_sorumlu_mu DESC, oks.id ASC;";
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
        await using var cmd = new SqlCommand("SELECT COUNT(*) + 1 FROM rezervasyonlar WHERE CAST(olusturulma_tarihi AS date) = CAST(GETDATE() AS date);", connection);
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

    public async Task<FirmaEmployeesPageViewModel> GetEmployeesAsync(long userId, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        var context = await BuildContextAsync(connection, userId, "Çalışanlar", "Firma kullanıcılarını, departmanlarını ve harcama yetkilerini yönetin.", "employees", cancellationToken);
        var employees = await LoadEmployeesAsync(connection, context.FirmaId, 200, cancellationToken);
        return new FirmaEmployeesPageViewModel
        {
            Shell = context.Shell,
            Employees = employees,
            TravelingEmployeeCount = employees.Count(static x => x.ReservationCountText != "0"),
            AverageLimitText = employees.Count == 0
                ? "₺0"
                : FormatMoney(ParseMoneyAverage(employees.Select(static x => x.LimitText)))
        };
    }

    public async Task<FirmaLimitsPageViewModel> GetLimitsAsync(long userId, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        var context = await BuildContextAsync(connection, userId, "Limitler & Onaylar", "Departman ve çalışan bazlı harcama limitleri ile onay akışlarını yönetin.", "limits", cancellationToken);
        var model = new FirmaLimitsPageViewModel { Shell = context.Shell };

        const string sql = @"
            SELECT id,
                   CASE WHEN kullanici_id IS NOT NULL THEN CONCAT('Kullanıcı · ', COALESCE((SELECT TOP (1) ad_soyad FROM users u WHERE u.id = fhl.kullanici_id), 'Kayıt'))
                        WHEN departman IS NOT NULL THEN CONCAT('Departman · ', departman)
                        ELSE 'Firma Geneli' END AS scope_text,
                   gecelik_limit, rezervasyon_basi_limit, aylik_limit, onay_gereksinimi
            FROM firma_harcama_limitleri fhl
            WHERE firma_id = @firmaId AND aktif_mi = 1
            ORDER BY kullanici_id IS NULL DESC, departman ASC, id ASC;";

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

    public async Task<FirmaInvoicesPageViewModel> GetInvoicesAsync(long userId, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        var context = await BuildContextAsync(connection, userId, "Faturalar", "Kurumsal rezervasyonlara ait fatura kayıtlarını takip edin.", "invoices", cancellationToken);
        var model = new FirmaInvoicesPageViewModel { Shell = context.Shell };

        const string sql = @"
            SELECT TOP (100) fatura_no, fatura_tarihi, fatura_turu, fatura_alici_unvan, genel_toplam, fatura_durumu
            FROM faturalar
            WHERE firma_id = @firmaId
            ORDER BY fatura_tarihi DESC, id DESC;";

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
            SELECT FORMAT(olusturulma_tarihi, 'MMM', 'tr-TR') AS ay, COALESCE(SUM(toplam_tutar), 0) AS toplam, COUNT(*) AS rezervasyon_sayisi
            FROM rezervasyonlar
            WHERE firma_id = @firmaId
              AND olusturulma_tarihi >= DATEADD(MONTH, -5, CAST(SYSUTCDATETIME() AS date))
            GROUP BY YEAR(olusturulma_tarihi), MONTH(olusturulma_tarihi), FORMAT(olusturulma_tarihi, 'MMM', 'tr-TR')
            ORDER BY YEAR(olusturulma_tarihi), MONTH(olusturulma_tarihi);";

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
            SELECT o.otel_adi, CONCAT(o.ilce, ', ', o.sehir) AS city_text, COUNT(*) AS rezervasyon_sayisi,
                   COALESCE(SUM(r.toplam_tutar), 0) AS toplam, COALESCE(SUM(r.toplam_tasarruf), 0) AS tasarruf
            FROM rezervasyonlar r
            INNER JOIN oteller o ON o.id = r.otel_id
            WHERE r.firma_id = @firmaId
            GROUP BY o.id, o.otel_adi, o.ilce, o.sehir
            ORDER BY toplam DESC, rezervasyon_sayisi DESC;";

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

        const string existsSql = "SELECT COUNT(*) FROM users WHERE eposta = @email;";
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
                    INSERT INTO users
                    (
                        ad_soyad, eposta, telefon, telefon_e164, telefon_dogrulama_kanali, telefon_dogrulama_durumu, sifre, rol, firma_id, departman, gorev_unvani,
                        harcama_limiti, onay_gereksinimi, personel_kodu, firma_yonetici_mi,
                        hesap_durumu, dil_tercihi, para_birimi, ulke, olusturulma_tarihi
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
                    INSERT INTO firma_harcama_limitleri
                    (
                        firma_id, kullanici_id, departman, gecelik_limit, onay_gereksinimi, aktif_mi, olusturulma_tarihi
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
                const string validateUserSql = "SELECT COUNT(*) FROM users WHERE id = @userId AND firma_id = @firmaId;";
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
                FROM firma_harcama_limitleri
                WHERE firma_id = @firmaId
                  AND aktif_mi = 1
                  AND ((@userId IS NOT NULL AND kullanici_id = @userId)
                    OR (@userId IS NULL AND @department IS NOT NULL AND departman = @department)
                    OR (@userId IS NULL AND @department IS NULL AND kullanici_id IS NULL AND departman IS NULL))
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
                    UPDATE firma_harcama_limitleri
                    SET departman = @department,
                        kullanici_id = @userId,
                        gecelik_limit = @nightlyLimit,
                        rezervasyon_basi_limit = @reservationLimit,
                        aylik_limit = @monthlyLimit,
                        onay_gereksinimi = @approvalRequired,
                        aktif_mi = 1
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
                    INSERT INTO firma_harcama_limitleri
                    (
                        firma_id, departman, kullanici_id, gecelik_limit, rezervasyon_basi_limit, aylik_limit,
                        onay_gereksinimi, aktif_mi, olusturulma_tarihi
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
                    UPDATE users
                    SET harcama_limiti = @nightlyLimit,
                        onay_gereksinimi = @approvalRequired
                    WHERE id = @userId AND firma_id = @firmaId;";
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
            UPDATE rezervasyonlar
            SET firma_onay_durumu = @approvalStatus,
                firma_onaylayan_kullanici_id = @userId,
                firma_onay_tarihi = CURRENT_TIMESTAMP,
                durum = @reservationStatus,
                iptal_nedeni = @cancelReason
            WHERE id = @reservationId
              AND firma_id = @firmaId
              AND firma_onay_durumu = 'Beklemede';";

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
            SELECT f.id, f.firma_adi, COALESCE(f.onay_durumu, 'Beklemede') AS onay_durumu,
                   u.ad_soyad, u.eposta, u.rol,
                   (SELECT COUNT(*) FROM users fu WHERE fu.firma_id = f.id AND fu.rol LIKE 'firma_%' AND fu.hesap_durumu = 1) AS employee_count,
                   (SELECT COUNT(*) FROM firma_ozel_fiyatlar ff WHERE ff.firma_id = f.id AND ff.aktif_mi = 1) AS deal_count,
                   (SELECT COUNT(*) FROM rezervasyonlar r WHERE r.firma_id = f.id AND r.firma_onay_durumu = 'Beklemede') AS pending_count
            FROM users u
            INNER JOIN firmalar f ON f.id = u.firma_id
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
            SELECT DISTINCT ot.sehir
            FROM dbo.firma_oda_fiyat_musaitlik f
            INNER JOIN dbo.oteller ot ON ot.id = f.otel_id
            WHERE f.firma_id = @firmaId
              AND f.aktif_mi = 1
              AND ot.sehir IS NOT NULL
              AND ot.sehir <> ''
            ORDER BY ot.sehir;";

        var cities = new List<string>();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@firmaId", firmaId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            cities.Add(reader.GetString(0));
        }

        return cities;
    }

    private async Task<List<FirmaPanelDealRowViewModel>> LoadDealsAsync(SqlConnection connection, long firmaId, int take, string? city = null, int? minRoomCount = null, string? search = null, CancellationToken cancellationToken = default)
    {
        // Firma paneli "Kurumsal Otel Fiyatları": partnerin firma_oda_fiyat_musaitlik tablosuna girdiği
        // fiyatları özetleyerek gösterir. Burada amaç; firmaya özel fiyat varsa onu, yoksa standart fiyatı
        // kıyaslayıp rezervasyon akışına yönlendirmektir.
        const string sql = @"
            SELECT TOP (@take)
                   ROW_NUMBER() OVER (ORDER BY ot.otel_adi ASC, od.oda_adi ASC) AS deal_id,
                   ot.id AS hotel_id,
                   ot.otel_adi,
                   COALESCE(od.oda_adi, N'') AS oda_adi,
                   CONCAT(COALESCE(ot.ilce, N''), CASE WHEN COALESCE(ot.ilce, N'') <> '' THEN N', ' ELSE N'' END, COALESCE(ot.sehir, N'')) AS city_text,
                   COALESCE(std.base_price, 0) AS standard_price,
                   COALESCE(corp.corp_price, 0) AS corporate_price,
                   COALESCE(corp.min_date, CAST(NULL AS date)) AS min_date,
                   COALESCE(corp.max_date, CAST(NULL AS date)) AS max_date
            FROM (
                SELECT DISTINCT f.otel_id, f.oda_tip_id
                FROM dbo.firma_oda_fiyat_musaitlik f
                WHERE f.firma_id = @firmaId
                  AND f.aktif_mi = 1
                  AND f.kapali_satis = 0
            ) x
            INNER JOIN dbo.oteller ot ON ot.id = x.otel_id
            LEFT JOIN dbo.oda_tipleri od ON od.id = x.oda_tip_id
            OUTER APPLY (
                SELECT
                    MIN(CASE WHEN f.firma_gecelik_fiyat > 0 THEN f.firma_gecelik_fiyat ELSE NULL END) AS corp_price,
                    MIN(f.tarih) AS min_date,
                    MAX(f.tarih) AS max_date
                FROM dbo.firma_oda_fiyat_musaitlik f
                WHERE f.firma_id = @firmaId
                  AND f.aktif_mi = 1
                  AND f.kapali_satis = 0
                  AND f.otel_id = x.otel_id
                  AND f.oda_tip_id = x.oda_tip_id
            ) corp
            OUTER APPLY (
                SELECT
                    MIN(
                        CASE
                            WHEN ofm.indirimli_fiyat IS NOT NULL AND ofm.indirimli_fiyat > 0 AND ofm.indirimli_fiyat < ofm.gecelik_fiyat THEN ofm.indirimli_fiyat
                            ELSE ofm.gecelik_fiyat
                        END
                    ) AS base_price
                FROM dbo.oda_fiyat_musaitlik ofm
                WHERE ofm.otel_id = x.otel_id
                  AND ofm.oda_tip_id = x.oda_tip_id
            ) std
            WHERE (@city IS NULL OR ot.sehir = @city)
              AND (@search IS NULL OR ot.otel_adi LIKE '%' + @search + '%' OR ot.sehir LIKE '%' + @search + '%' OR ot.ilce LIKE '%' + @search + '%' OR ot.mahalle LIKE '%' + @search + '%')
            ORDER BY ot.one_cikan_otel DESC, ot.otel_adi ASC, od.oda_adi ASC;";

        var items = new List<FirmaPanelDealRowViewModel>();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@firmaId", firmaId);
        command.Parameters.AddWithValue("@take", take);
        command.Parameters.AddWithValue("@city", string.IsNullOrWhiteSpace(city) ? DBNull.Value : (object)city);
        command.Parameters.AddWithValue("@minRoomCount", minRoomCount.HasValue ? (object)minRoomCount.Value : DBNull.Value);
        command.Parameters.AddWithValue("@search", string.IsNullOrWhiteSpace(search) ? DBNull.Value : (object)search.Trim());
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var standardPrice = SafeDecimal(reader, 4);
            var corporatePrice = SafeDecimal(reader, 5);
            items.Add(new FirmaPanelDealRowViewModel
            {
                DealId = reader.GetInt64(0),
                HotelId = reader.GetInt64(1),
                HotelName = reader.GetString(2),
                RoomName = reader.IsDBNull(3) || string.IsNullOrWhiteSpace(reader.GetString(3)) ? "Oda" : reader.GetString(3),
                CityText = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                StandardPriceText = FormatMoney(standardPrice),
                CorporatePriceText = FormatMoney(corporatePrice),
                DiscountText = standardPrice > 0m && corporatePrice > 0m && corporatePrice < standardPrice
                    ? $"%{Math.Clamp((int)Math.Round(((standardPrice - corporatePrice) / standardPrice) * 100m, MidpointRounding.AwayFromZero), 1, 95)}"
                    : "-",
                MinimumRoomText = "Kurumsal",
                ValidityText = reader.IsDBNull(7) || reader.IsDBNull(8)
                    ? "Tarih aralığı tanımlı"
                    : $"{reader.GetDateTime(7):dd.MM.yyyy} - {reader.GetDateTime(8):dd.MM.yyyy}",
                SavingsText = FormatMoney(Math.Max(0m, standardPrice - corporatePrice))
            });
        }
        return items;
    }

    private async Task<List<FirmaPanelEmployeeRowViewModel>> LoadEmployeesAsync(SqlConnection connection, long firmaId, int take, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT u.id, u.ad_soyad, COALESCE(u.departman, 'Tanımsız'), COALESCE(u.gorev_unvani, u.rol), u.eposta,
                   u.harcama_limiti, u.onay_gereksinimi, u.rol, u.firma_yonetici_mi,
                   u.telefon_dogrulama_tarihi, u.telefon_son_sahiplik_teyit_tarihi, COALESCE(u.telefon_dogrulama_durumu, ''),
                   COUNT(r.id) AS rezervasyon_sayisi, COALESCE(SUM(r.toplam_tutar), 0) AS harcama_toplami
            FROM users u
            LEFT JOIN rezervasyonlar r ON r.firma_calisan_id = u.id
            WHERE u.firma_id = @firmaId AND u.rol LIKE 'firma_%'
            GROUP BY u.id, u.ad_soyad, u.departman, u.gorev_unvani, u.eposta, u.harcama_limiti, u.onay_gereksinimi, u.rol, u.firma_yonetici_mi,
                     u.telefon_dogrulama_tarihi, u.telefon_son_sahiplik_teyit_tarihi, u.telefon_dogrulama_durumu
            ORDER BY u.firma_yonetici_mi DESC, u.ad_soyad ASC
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
            SELECT r.id, r.rezervasyon_no, COALESCE(u.ad_soyad, r.misafir_ad_soyad) AS employee_name, o.otel_adi,
                   CONCAT(o.ilce, ', ', o.sehir) AS city_text,
                   r.giris_tarihi, r.cikis_tarihi, r.durum, r.firma_onay_durumu, r.toplam_tutar
            FROM rezervasyonlar r
            INNER JOIN oteller o ON o.id = r.otel_id
            LEFT JOIN users u ON u.id = r.firma_calisan_id
            WHERE r.firma_id = @firmaId
            ORDER BY r.olusturulma_tarihi DESC, r.id DESC
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
            SELECT r.id, r.rezervasyon_no, COALESCE(u.ad_soyad, r.misafir_ad_soyad) AS employee_name, o.otel_adi,
                   CONCAT(o.ilce, ', ', o.sehir) AS city_text,
                   r.giris_tarihi, r.cikis_tarihi, r.durum, r.firma_onay_durumu, r.toplam_tutar
            FROM rezervasyonlar r
            INNER JOIN oteller o ON o.id = r.otel_id
            LEFT JOIN users u ON u.id = r.firma_calisan_id
            WHERE r.firma_id = @firmaId AND r.firma_onay_durumu = 'Beklemede'
            ORDER BY r.olusturulma_tarihi DESC, r.id DESC
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

    private static string FormatMoney(decimal value)
        => string.Format(CultureInfo.GetCultureInfo("tr-TR"), "{0:C0}", value);

    private static string GetInitials(string fullName)
        => string.Concat(fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(2).Select(static x => x[0])).ToUpperInvariant();

    private sealed record FirmaContext(long FirmaId, FirmaPanelShellViewModel Shell);
}
