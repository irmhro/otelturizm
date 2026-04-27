using System.Globalization;
using System.Linq;
using Microsoft.Data.SqlClient;
using SqlConnection = Microsoft.Data.SqlClient.SqlConnection;
using SqlCommand = Microsoft.Data.SqlClient.SqlCommand;
using SqlTransaction = Microsoft.Data.SqlClient.SqlTransaction;
using SqlException = Microsoft.Data.SqlClient.SqlException;
using otelturizmnew.Models.Email;
using otelturizmnew.Models.Paneller.Satis;
using otelturizmnew.Models.Reservations;
using otelturizmnew.Services.Abstractions;

namespace otelturizmnew.Services;

public class SalesService : ISalesService
{
    private readonly string _connectionString;
    private readonly IEmailQueueService _emailQueueService;
    private readonly ILogger<SalesService> _logger;

    public SalesService(IConfiguration configuration, IEmailQueueService emailQueueService, ILogger<SalesService> logger)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection tanimli degil.");
        _emailQueueService = emailQueueService;
        _logger = logger;
    }

    public async Task<SalesDashboardPageViewModel> GetDashboardAsync(long userId, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        var shell = await BuildShellAsync(connection, userId, "dashboard", "Satış Dashboard", "Çağrıdan gelen talepleri, hızlı rezervasyonları ve günlük performansı tek ekranda yönetin.", cancellationToken);
        var model = new SalesDashboardPageViewModel { Shell = shell };

        model.SummaryCards.Add(new SalesStatCardViewModel { Label = "Bugünkü Rezervasyon", Value = shell.TodayReservationCount.ToString(CultureInfo.InvariantCulture), Description = "Satış panelinden açılan kayıtlar", IconClass = "fa-check-circle", ToneClass = "success" });
        model.SummaryCards.Add(new SalesStatCardViewModel { Label = "Bugünkü Ciro", Value = FormatMoney(shell.TodayRevenue), Description = "Bugün oluşturulan rezervasyon toplamı", IconClass = "fa-money-bill-wave", ToneClass = "warning" });
        model.SummaryCards.Add(new SalesStatCardViewModel { Label = "Aylık Rezervasyon", Value = shell.MonthlyReservationCount.ToString(CultureInfo.InvariantCulture), Description = "Ay içi toplam satış", IconClass = "fa-calendar-week", ToneClass = "info" });
        model.SummaryCards.Add(new SalesStatCardViewModel { Label = "Ekip Sıralaması", Value = shell.Ranking <= 0 ? "-" : $"#{shell.Ranking}", Description = "Aylık ciroya göre", IconClass = "fa-trophy", ToneClass = "danger" });

        const string sql = @"
            SELECT COALESCE(SUM(toplam_tutar),0), COUNT(*)
            FROM rezervasyonlar
            WHERE satis_temsilcisi_id = @userId
              AND olusturulma_tarihi >= DATEFROMPARTS(YEAR(GETDATE()), MONTH(GETDATE()), 1)
              AND olusturulma_tarihi < DATEADD(MONTH, 1, DATEFROMPARTS(YEAR(GETDATE()), MONTH(GETDATE()), 1));";
        await using (var command = new SqlCommand(sql, connection))
        {
            command.Parameters.AddWithValue("@userId", userId);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                model.MonthlyAchievedRevenue = ReadDecimal(reader, 0);
                model.MonthlyReservationCount = ReadInt(reader, 1);
            }
        }

        model.MonthlyRemainingRevenue = Math.Max(0m, shell.MonthlyTarget - model.MonthlyAchievedRevenue);
        model.MonthlyProgressPercent = shell.MonthlyTarget <= 0 ? 0 : (int)Math.Min(100m, Math.Round(model.MonthlyAchievedRevenue * 100m / shell.MonthlyTarget, MidpointRounding.AwayFromZero));
        model.RemainingReservationCount = Math.Max(0, 10 - model.MonthlyReservationCount);
        model.RecentReservations = await LoadReservationsAsync(connection, userId, new SalesReservationsFilterViewModel { Page = 1, PageSize = 6 }, cancellationToken);
        return model;
    }

    public async Task<SalesCreateReservationPageViewModel> GetCreateReservationAsync(long userId, long? hotelId = null, long? roomTypeId = null, long? customerId = null, string? searchTerm = null, string? city = null, string? district = null, string? neighborhood = null, decimal? minPrice = null, decimal? maxPrice = null, decimal? minimumRating = null, int? minimumReviewCount = null, string? feature = null, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        var shell = await BuildShellAsync(connection, userId, "create", "Yeni Rezervasyon Oluştur", "İl, ilçe, fiyat ve özellik filtreleriyle oteli hızlıca bulun; müşteriye en uygun rezervasyonu aynı ekrandan tamamlayın.", cancellationToken);
        var customerPrefill = customerId.HasValue ? await LoadCustomerPrefillAsync(connection, customerId.Value, cancellationToken) : null;
        var hasAssistantSearch = HasHotelAssistantSearch(searchTerm, city, district, neighborhood, minPrice, maxPrice, minimumRating, minimumReviewCount, feature);
        var hotels = hasAssistantSearch || hotelId.HasValue
            ? await LoadHotelOptionsAsync(connection, searchTerm, city, district, neighborhood, cancellationToken)
            : new List<SalesSelectOption>();
        if (hotelId.HasValue && hotelId.Value > 0 && hotels.All(x => x.Value != hotelId.Value))
        {
            var selectedHotelOption = await LoadHotelOptionByIdAsync(connection, hotelId.Value, cancellationToken);
            if (selectedHotelOption is not null)
            {
                hotels.Insert(0, selectedHotelOption);
            }
        }
        var selectedHotelId = hotelId ?? hotels.FirstOrDefault()?.Value ?? 0;
        var roomTypes = selectedHotelId > 0 ? await LoadRoomTypeOptionsAsync(connection, selectedHotelId, cancellationToken) : new List<SalesSelectOption>();
        var selectedRoomTypeId = roomTypeId ?? roomTypes.FirstOrDefault()?.Value ?? 0;

        var model = new SalesCreateReservationPageViewModel
        {
            Shell = shell,
            Form = new SalesReservationCreateModel
            {
                CustomerId = customerPrefill?.CustomerId,
                CustomerFullName = customerPrefill?.FullName ?? string.Empty,
                CustomerEmail = customerPrefill?.Email ?? string.Empty,
                CustomerPhone = customerPrefill?.Phone ?? string.Empty,
                CustomerCity = customerPrefill?.City,
                CustomerDistrict = customerPrefill?.District,
                CustomerNeighborhood = customerPrefill?.Neighborhood,
                CustomerAddress = customerPrefill?.Address,
                HotelId = selectedHotelId,
                RoomTypeId = selectedRoomTypeId,
                SearchTerm = searchTerm,
                SearchCity = city,
                SearchDistrict = district,
                SearchNeighborhood = neighborhood,
                SearchMinPrice = minPrice,
                SearchMaxPrice = maxPrice,
                SearchMinimumRating = minimumRating,
                SearchMinimumReviewCount = minimumReviewCount,
                SearchFeature = feature
            },
            Hotels = hotels,
            Cities = await LoadCitiesAsync(connection, cancellationToken),
            Districts = await LoadDistrictsAsync(connection, city, cancellationToken),
            RoomTypes = roomTypes,
            Customers = await LoadCustomersAsync(connection, null, 8, cancellationToken),
            HotelSearchResults = hasAssistantSearch
                ? await LoadHotelSearchResultsAsync(connection, searchTerm, city, district, neighborhood, minPrice, maxPrice, minimumRating, minimumReviewCount, feature, 12, cancellationToken)
                : new List<SalesHotelSearchCardViewModel>(),
            AvailableRooms = selectedHotelId > 0 ? await LoadRoomOptionsAsync(connection, selectedHotelId, DateOnly.FromDateTime(DateTime.Today.AddDays(2)), DateOnly.FromDateTime(DateTime.Today.AddDays(4)), cancellationToken) : new List<SalesRoomOptionViewModel>(),
            SelectedHotelSummary = await BuildSelectedHotelSummaryAsync(connection, selectedHotelId, cancellationToken),
            HasAssistantSearch = hasAssistantSearch
        };

        if (selectedRoomTypeId > 0)
        {
            model.Summary = await BuildPriceSummaryAsync(connection, selectedRoomTypeId, model.Form.CheckInDate, model.Form.CheckOutDate, model.Form.RoomCount, cancellationToken);
        }

        return model;
    }

    public async Task<IReadOnlyList<SalesHotelSearchCardViewModel>> SearchHotelsForAssistantAsync(
        long userId,
        string? searchTerm = null,
        string? city = null,
        string? district = null,
        string? neighborhood = null,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        decimal? minimumRating = null,
        int? minimumReviewCount = null,
        string? feature = null,
        int resultLimit = 8,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await EnsureSalesUserAsync(connection, userId, cancellationToken);
        var safeLimit = Math.Clamp(resultLimit, 1, 20);
        return await LoadHotelSearchResultsAsync(connection, searchTerm, city, district, neighborhood, minPrice, maxPrice, minimumRating, minimumReviewCount, feature, safeLimit, cancellationToken);
    }

    public async Task<SalesCustomersPageViewModel> GetCustomersAsync(long userId, string? search = null, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        return new SalesCustomersPageViewModel
        {
            Shell = await BuildShellAsync(connection, userId, "customers", "Müşteri Yönetimi", "Arayan misafirleri, son talepleri ve rezervasyon geçmişini tek ekranda yönetin.", cancellationToken),
            Search = search,
            Customers = await LoadCustomersAsync(connection, search, 100, cancellationToken)
        };
    }

    public async Task<SalesAvailabilityPageViewModel> GetAvailabilityAsync(long userId, long? hotelId = null, long? roomTypeId = null, string? search = null, DateOnly? month = null, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        var shell = await BuildShellAsync(connection, userId, "availability", "Müsaitlik Takvimi", "Telefon görüşmesinde oteli ada veya bölgeye göre bulup aylık fiyatı ve müsaitliği tek ekranda görün.", cancellationToken);
        var hotels = await LoadHotelOptionsAsync(connection, search, null, null, null, cancellationToken);
        var selectedHotelId = hotelId ?? hotels.FirstOrDefault()?.Value ?? 0;
        var roomTypes = selectedHotelId > 0 ? await LoadRoomTypeOptionsAsync(connection, selectedHotelId, cancellationToken) : new List<SalesSelectOption>();
        var selectedRoomTypeId = roomTypeId ?? roomTypes.FirstOrDefault()?.Value ?? 0;
        var targetMonth = month ?? DateOnly.FromDateTime(DateTime.Today);
        var monthAnchor = new DateOnly(targetMonth.Year, targetMonth.Month, 1);

        return new SalesAvailabilityPageViewModel
        {
            Shell = shell,
            Search = search,
            SelectedHotelId = selectedHotelId,
            SelectedRoomTypeId = selectedRoomTypeId,
            SelectedMonth = monthAnchor,
            Hotels = hotels,
            RoomTypes = roomTypes,
            Days = selectedRoomTypeId > 0 ? await LoadAvailabilityDaysAsync(connection, selectedRoomTypeId, monthAnchor, cancellationToken) : new List<SalesAvailabilityDayViewModel>(),
            SelectedHotelLabel = hotels.FirstOrDefault(x => x.Value == selectedHotelId)?.Label ?? "Otel seçin",
            SelectedRoomLabel = roomTypes.FirstOrDefault(x => x.Value == selectedRoomTypeId)?.Label ?? "Oda seçin",
            PreviousMonthQuery = BuildMonthQuery(selectedHotelId, selectedRoomTypeId, search, monthAnchor.AddMonths(-1)),
            NextMonthQuery = BuildMonthQuery(selectedHotelId, selectedRoomTypeId, search, monthAnchor.AddMonths(1))
        };
    }

    public async Task<SalesReservationsPageViewModel> GetReservationsAsync(long userId, SalesReservationsFilterViewModel filters, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        NormalizeReservationFilters(filters);

        var model = new SalesReservationsPageViewModel
        {
            Shell = await BuildShellAsync(connection, userId, "reservations", "Rezervasyonlarım", "Durum, tarih ve müşteri bazlı filtreleyin; görüşme sırasında kayıtları anında bulun.", cancellationToken),
            Filters = filters,
            Reservations = await LoadReservationsAsync(connection, userId, filters, cancellationToken),
            Pagination = await LoadReservationsPaginationAsync(connection, userId, filters, cancellationToken),
            Summary = await LoadReservationSummaryAsync(connection, userId, filters, cancellationToken)
        };

        return model;
    }

    public async Task<SalesReportsPageViewModel> GetReportsAsync(long userId, int year, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        var safePage = page <= 0 ? 1 : page;
        var safePageSize = pageSize is 10 or 20 or 30 ? pageSize : 10;
        var model = new SalesReportsPageViewModel
        {
            Shell = await BuildShellAsync(connection, userId, "reports", "Raporlar", "Aylık rezervasyon performansınızı, onay/iptal oranınızı ve kazandırdığınız ciroyu görün.", cancellationToken),
            SelectedYear = year,
            Pagination = new SalesPaginationViewModel { Page = safePage, PageSize = safePageSize }
        };

        const string sql = @"
            SELECT COALESCE(SUM(toplam_tutar),0), COALESCE(SUM(komisyon_tutari),0), COUNT(*),
                   SUM(CASE WHEN durum = 'Onaylandı' THEN 1 ELSE 0 END),
                   SUM(CASE WHEN durum = 'İptal Edildi' THEN 1 ELSE 0 END)
            FROM rezervasyonlar
            WHERE satis_temsilcisi_id = @userId
              AND YEAR(olusturulma_tarihi) = @year;";
        await using (var command = new SqlCommand(sql, connection))
        {
            command.Parameters.AddWithValue("@userId", userId);
            command.Parameters.AddWithValue("@year", year);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                model.MonthlyRevenue = ReadDecimal(reader, 0);
                model.MonthlyCommission = ReadDecimal(reader, 1);
                model.MonthlyReservationCount = ReadInt(reader, 2);
                model.MonthlyApprovedCount = ReadInt(reader, 3);
                model.MonthlyCancelledCount = ReadInt(reader, 4);
            }
        }

        model.MonthlyBreakdown = await LoadMonthlyPerformanceAsync(connection, userId, year, safePage, safePageSize, cancellationToken);
        model.Pagination.TotalCount = await CountMonthlyPerformanceRowsAsync(connection, userId, year, cancellationToken);
        return model;
    }

    public async Task<SalesHotelGuidePageViewModel> GetHotelGuideAsync(long userId, string? search = null, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        return new SalesHotelGuidePageViewModel
        {
            Shell = await BuildShellAsync(connection, userId, "hotels", "Otel Rehberi", "Telefon, adres, satış kontağı ve günlük talep bilgisini müşteriye aktarılabilir formatta görün.", cancellationToken),
            Search = search,
            Hotels = await LoadHotelGuideAsync(connection, search, cancellationToken)
        };
    }

    public async Task<(bool Success, string Message, long? ReservationId)> CreateReservationAsync(long userId, SalesReservationCreateModel model, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(model.CustomerFullName) || string.IsNullOrWhiteSpace(model.CustomerPhone))
        {
            return (false, "Müşteri adı ve telefon zorunludur. E-posta yoksa boş bırakabilirsiniz; sistem PDF çıktısı üretir.", null);
        }
        if (model.HotelId <= 0 || model.RoomTypeId <= 0)
        {
            return (false, "Otel ve oda tipi seçilmelidir.", null);
        }
        if (model.CheckOutDate <= model.CheckInDate)
        {
            return (false, "Çıkış tarihi giriş tarihinden sonra olmalıdır.", null);
        }

        await using var connection = await OpenConnectionAsync(cancellationToken);
        await EnsureSalesUserAsync(connection, userId, cancellationToken);

        var customerEmailRaw = (model.CustomerEmail ?? string.Empty).Trim();
        var customerPhoneRaw = (model.CustomerPhone ?? string.Empty).Trim();
        var customerEmailForUser = string.IsNullOrWhiteSpace(customerEmailRaw)
            ? BuildPlaceholderEmail(customerPhoneRaw)
            : customerEmailRaw;

        var hotelInfo = await GetHotelSummaryAsync(connection, model.HotelId, cancellationToken);
        var roomName = await GetRoomNameAsync(connection, model.RoomTypeId, model.HotelId, cancellationToken);
        var summary = await BuildPriceSummaryAsync(connection, model.RoomTypeId, model.CheckInDate, model.CheckOutDate, model.RoomCount, cancellationToken);
        var partnerRecipient = await ResolvePartnerRecipientAsync(connection, model.HotelId, cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            var salesCustomerId = await EnsureSalesCustomerAsync(connection, (SqlTransaction)transaction, userId, model, cancellationToken);
            var publicUserId = await EnsurePublicCustomerUserAsync(connection, (SqlTransaction)transaction, model, customerEmailForUser, cancellationToken);
            var reservationNo = await GenerateReservationNoAsync(connection, (SqlTransaction)transaction, cancellationToken);

            var insertSql = $@"
                INSERT INTO rezervasyonlar
                (
                    rezervasyon_no, otel_id, oda_tip_id, kullanici_id, satis_temsilcisi_id, satis_musteri_id,
                    misafir_ad_soyad, misafir_eposta, misafir_telefon, misafir_notu, misafir_sehir, misafir_ilce, misafir_mahalle, misafir_adres,
                    giris_tarihi, cikis_tarihi, yetiskin_sayisi, cocuk_sayisi, oda_sayisi,
                    gecelik_fiyat, toplam_oda_tutari, vergi_tutari, toplam_tutar,
                    komisyon_orani, durum, rezervasyon_durumu_id, odeme_durumu, otel_onay_durumu, firma_onay_durumu,
                    kaynak, rezervasyon_kanali, musteri_talep_notu, ozel_istekler
                )
                VALUES
                (
                    @reservationNo, @hotelId, @roomTypeId, @userId, @salesUserId, @salesCustomerId,
                    @fullName, @email, @phone, @note, @city, @district, @neighborhood, @address,
                    @checkIn, @checkOut, @adultCount, @childCount, @roomCount,
                    @nightlyPrice, @roomTotal, @taxAmount, @totalAmount,
                    @commissionRate, 'Onay Bekliyor', (SELECT TOP (1) id FROM dbo.rezervasyon_durum_tanimlari WHERE kod = N'{RezervasyonDurumKodlari.OnayBekliyor}'), 'Beklemede', 'Beklemede', 'Onay Gerekmiyor',
                    'Telefon', 'Satış Paneli', @note, @note
                );
                SELECT CAST(SCOPE_IDENTITY() AS bigint);";
            long reservationId;
            await using (var command = new SqlCommand(insertSql, connection, (SqlTransaction)transaction))
            {
                command.Parameters.AddWithValue("@reservationNo", reservationNo);
                command.Parameters.AddWithValue("@hotelId", model.HotelId);
                command.Parameters.AddWithValue("@roomTypeId", model.RoomTypeId);
                command.Parameters.AddWithValue("@userId", publicUserId);
                command.Parameters.AddWithValue("@salesUserId", userId);
                command.Parameters.AddWithValue("@salesCustomerId", salesCustomerId);
                command.Parameters.AddWithValue("@fullName", model.CustomerFullName.Trim());
                command.Parameters.AddWithValue("@email", customerEmailRaw);
                command.Parameters.AddWithValue("@phone", customerPhoneRaw);
                command.Parameters.AddWithValue("@note", model.DemandNote ?? string.Empty);
                command.Parameters.AddWithValue("@city", string.IsNullOrWhiteSpace(model.CustomerCity) ? DBNull.Value : model.CustomerCity.Trim());
                command.Parameters.AddWithValue("@district", string.IsNullOrWhiteSpace(model.CustomerDistrict) ? DBNull.Value : model.CustomerDistrict.Trim());
                command.Parameters.AddWithValue("@neighborhood", string.IsNullOrWhiteSpace(model.CustomerNeighborhood) ? DBNull.Value : model.CustomerNeighborhood.Trim());
                command.Parameters.AddWithValue("@address", string.IsNullOrWhiteSpace(model.CustomerAddress) ? DBNull.Value : model.CustomerAddress.Trim());
                command.Parameters.AddWithValue("@checkIn", model.CheckInDate.ToDateTime(TimeOnly.MinValue));
                command.Parameters.AddWithValue("@checkOut", model.CheckOutDate.ToDateTime(TimeOnly.MinValue));
                command.Parameters.AddWithValue("@adultCount", model.AdultCount);
                command.Parameters.AddWithValue("@childCount", model.ChildCount);
                command.Parameters.AddWithValue("@roomCount", model.RoomCount);
                command.Parameters.AddWithValue("@nightlyPrice", summary.BaseNightlyAmount);
                command.Parameters.AddWithValue("@roomTotal", summary.RoomTotalAmount);
                command.Parameters.AddWithValue("@taxAmount", summary.TaxAmount);
                command.Parameters.AddWithValue("@totalAmount", summary.TotalAmount);
                command.Parameters.AddWithValue("@commissionRate", hotelInfo.CommissionRate);
                reservationId = Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken), CultureInfo.InvariantCulture);
            }

            const string updateCustomerSql = @"
                UPDATE satis_musterileri
                SET toplam_rezervasyon_sayisi = toplam_rezervasyon_sayisi + 1,
                    toplam_harcama = toplam_harcama + @totalAmount,
                    son_rezervasyon_tarihi = @checkIn,
                    son_talep_ozeti = @summary
                WHERE id = @customerId;";
            await using (var command = new SqlCommand(updateCustomerSql, connection, (SqlTransaction)transaction))
            {
                command.Parameters.AddWithValue("@totalAmount", summary.TotalAmount);
                command.Parameters.AddWithValue("@checkIn", model.CheckInDate.ToDateTime(TimeOnly.MinValue));
                command.Parameters.AddWithValue("@summary", $"{hotelInfo.HotelName} · {model.CheckInDate:dd.MM} - {model.CheckOutDate:dd.MM} · {model.RoomCount} oda");
                command.Parameters.AddWithValue("@customerId", salesCustomerId);
                await command.ExecuteNonQueryAsync(cancellationToken);
            }

            if (!string.IsNullOrWhiteSpace(customerEmailRaw))
            {
                await _emailQueueService.QueueTemplateAsync(connection, (SqlTransaction)transaction, new QueuedEmailTemplateRequest
                {
                    UserId = publicUserId,
                    RecipientEmail = customerEmailRaw,
                    TemplateCode = "reservation_received_customer",
                    RelatedTable = "rezervasyonlar",
                    RelatedRecordId = reservationId,
                    Tokens = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["user_first_name"] = SplitFirstName(model.CustomerFullName),
                        ["booking_reference"] = reservationNo,
                        ["hotel_name"] = hotelInfo.HotelName,
                        ["check_in_date"] = model.CheckInDate.ToString("dd MMM yyyy", CultureInfo.GetCultureInfo("tr-TR")),
                        ["check_out_date"] = model.CheckOutDate.ToString("dd MMM yyyy", CultureInfo.GetCultureInfo("tr-TR")),
                        ["total_price"] = summary.TotalAmount.ToString("N2", CultureInfo.GetCultureInfo("tr-TR")),
                        ["room_type_name"] = roomName,
                        ["booking_details_link"] = "/panel/user/rezervasyonlarim",
                        ["hotel_address"] = hotelInfo.HotelName
                    }
                }, cancellationToken);
            }
            await _emailQueueService.QueueTemplateAsync(connection, (SqlTransaction)transaction, new QueuedEmailTemplateRequest
            {
                UserId = partnerRecipient.UserId,
                RecipientEmail = partnerRecipient.Email,
                TemplateCode = "reservation_new_partner",
                RelatedTable = "rezervasyonlar",
                RelatedRecordId = reservationId,
                Tokens = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["hotel_manager_name"] = "Partner Yetkilisi",
                    ["hotel_name"] = hotelInfo.HotelName,
                    ["booking_reference"] = reservationNo,
                    ["guest_full_name"] = model.CustomerFullName.Trim(),
                    ["guest_email"] = customerEmailRaw,
                    ["guest_phone"] = customerPhoneRaw,
                    ["total_price"] = summary.TotalAmount.ToString("N2", CultureInfo.GetCultureInfo("tr-TR")),
                    ["check_in_date"] = model.CheckInDate.ToString("dd MMM yyyy", CultureInfo.GetCultureInfo("tr-TR")),
                    ["check_out_date"] = model.CheckOutDate.ToString("dd MMM yyyy", CultureInfo.GetCultureInfo("tr-TR")),
                    ["room_type_name"] = roomName,
                    ["room_count"] = model.RoomCount.ToString(CultureInfo.InvariantCulture)
                }
            }, cancellationToken);

            await transaction.CommitAsync(cancellationToken);
            _logger.LogInformation(
                "RESERVATION_AUDIT create source=sales salesUserId={SalesUserId} reservationId={ReservationId} reservationNo={ReservationNo} hotelId={HotelId} roomTypeId={RoomTypeId} total={Total} rooms={Rooms}",
                userId,
                reservationId,
                reservationNo,
                model.HotelId,
                model.RoomTypeId,
                summary.TotalAmount,
                model.RoomCount);
            return (true, $"Rezervasyon başarıyla oluşturuldu: {reservationNo}", reservationId);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            return (false, $"Rezervasyon oluşturulurken hata oluştu: {ex.Message}", null);
        }
    }

    public async Task<(bool Success, string Message)> CreateCustomerAsync(long userId, SalesCustomerCreateModel model, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(model.FullName) || string.IsNullOrWhiteSpace(model.Phone))
        {
            return (false, "Ad soyad ve telefon zorunludur. E-posta yoksa boş bırakabilirsiniz.");
        }

        await using var connection = await OpenConnectionAsync(cancellationToken);
        await EnsureSalesUserAsync(connection, userId, cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            var reservationModel = new SalesReservationCreateModel
            {
                CustomerFullName = model.FullName,
                CustomerEmail = model.Email ?? string.Empty,
                CustomerPhone = model.Phone,
                CustomerCity = model.City,
                DemandNote = model.Note
            };

            var customerId = await EnsureSalesCustomerAsync(connection, (SqlTransaction)transaction, userId, reservationModel, cancellationToken, model.MembershipLevel);
            if (!string.IsNullOrWhiteSpace(model.Note))
            {
                const string noteSql = "INSERT INTO satis_musteri_notlari (satis_musteri_id, sales_user_id, not_basligi, not_icerigi) VALUES (@customerId, @userId, 'Müşteri Notu', @note);";
                await using var noteCommand = new SqlCommand(noteSql, connection, (SqlTransaction)transaction);
                noteCommand.Parameters.AddWithValue("@customerId", customerId);
                noteCommand.Parameters.AddWithValue("@userId", userId);
                noteCommand.Parameters.AddWithValue("@note", model.Note);
                await noteCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            var emailForUser = string.IsNullOrWhiteSpace(reservationModel.CustomerEmail)
                ? BuildPlaceholderEmail(reservationModel.CustomerPhone)
                : reservationModel.CustomerEmail.Trim();
            await EnsurePublicCustomerUserAsync(connection, (SqlTransaction)transaction, reservationModel, emailForUser, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return (true, "Müşteri kaydı oluşturuldu.");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            return (false, $"Müşteri kaydı sırasında hata oluştu: {ex.Message}");
        }
    }

    public async Task<SalesReservationPdfDataViewModel?> GetReservationPdfDataAsync(long userId, long reservationId, CancellationToken cancellationToken = default)
    {
        if (reservationId <= 0) return null;
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await EnsureSalesUserAsync(connection, userId, cancellationToken);

        const string sql = @"
            SELECT
                r.id, r.rezervasyon_no, o.otel_adi, COALESCE(o.rezervasyon_telefonu, o.telefon_1, ''),
                ot.oda_adi,
                CAST(r.giris_tarihi AS date), CAST(r.cikis_tarihi AS date),
                r.yetiskin_sayisi, r.cocuk_sayisi, r.oda_sayisi,
                COALESCE(r.misafir_ad_soyad,''), COALESCE(r.misafir_eposta,''), COALESCE(r.misafir_telefon,''),
                COALESCE(r.gecelik_fiyat,0), COALESCE(r.toplam_oda_tutari,0), COALESCE(r.vergi_tutari,0), COALESCE(r.toplam_tutar,0),
                FORMAT(r.olusturulma_tarihi, 'dd.MM.yyyy HH:mm', 'tr-TR')
            FROM rezervasyonlar r
            INNER JOIN oteller o ON o.id = r.otel_id
            INNER JOIN oda_tipleri ot ON ot.id = r.oda_tip_id
            WHERE r.id = @reservationId
              AND r.satis_temsilcisi_id = @userId;";

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@reservationId", reservationId);
        command.Parameters.AddWithValue("@userId", userId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) return null;

        var checkIn = DateOnly.FromDateTime(reader.GetDateTime(5));
        var checkOut = DateOnly.FromDateTime(reader.GetDateTime(6));
        var nightCount = Math.Max(1, checkOut.DayNumber - checkIn.DayNumber);

        return new SalesReservationPdfDataViewModel
        {
            ReservationId = reader.GetInt64(0),
            ReservationNo = reader.GetString(1),
            HotelName = reader.GetString(2),
            HotelPhone = reader.GetString(3),
            RoomName = reader.GetString(4),
            CheckInDate = checkIn,
            CheckOutDate = checkOut,
            NightCount = nightCount,
            AdultCount = ReadInt(reader, 7),
            ChildCount = ReadInt(reader, 8),
            RoomCount = ReadInt(reader, 9),
            GuestFullName = reader.GetString(10),
            GuestEmail = reader.GetString(11),
            GuestPhone = reader.GetString(12),
            NightlyPrice = ReadDecimal(reader, 13),
            RoomTotal = ReadDecimal(reader, 14),
            TaxAmount = ReadDecimal(reader, 15),
            TotalAmount = ReadDecimal(reader, 16),
            CreatedAtText = reader.GetString(17)
        };
    }

    private async Task<SalesPanelShellViewModel> BuildShellAsync(SqlConnection connection, long userId, string activeSectionKey, string title, string subtitle, CancellationToken cancellationToken)
    {
        await EnsureSalesUserAsync(connection, userId, cancellationToken);
        const string sql = @"
            SELECT
                u.ad_soyad, u.eposta, u.rol, COALESCE(u.satis_ekibi,'Satış Ekibi'),
                COALESCE(u.gunluk_satis_hedefi,0), COALESCE(u.aylik_satis_hedefi,0),
                COALESCE((SELECT COUNT(*) FROM rezervasyonlar r WHERE r.satis_temsilcisi_id = u.id AND CAST(r.olusturulma_tarihi AS date) = CAST(GETDATE() AS date)),0),
                COALESCE((SELECT SUM(r.toplam_tutar) FROM rezervasyonlar r WHERE r.satis_temsilcisi_id = u.id AND CAST(r.olusturulma_tarihi AS date) = CAST(GETDATE() AS date)),0),
                COALESCE((SELECT COUNT(*) FROM rezervasyonlar r WHERE r.satis_temsilcisi_id = u.id AND YEAR(r.olusturulma_tarihi)=YEAR(GETDATE()) AND MONTH(r.olusturulma_tarihi)=MONTH(GETDATE())),0)
            FROM users u WHERE u.id = @userId;";
        var shell = new SalesPanelShellViewModel();
        await using (var command = new SqlCommand(sql, connection))
        {
            command.Parameters.AddWithValue("@userId", userId);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                shell.UserId = userId;
                shell.FullName = reader.GetString(0);
                shell.Email = reader.GetString(1);
                shell.UserRole = reader.GetString(2);
                shell.TeamName = reader.GetString(3);
                shell.DailyTarget = ReadDecimal(reader, 4);
                shell.MonthlyTarget = ReadDecimal(reader, 5);
                shell.TodayReservationCount = ReadInt(reader, 6);
                shell.TodayRevenue = ReadDecimal(reader, 7);
                shell.MonthlyReservationCount = ReadInt(reader, 8);
            }
        }
        shell.Ranking = await LoadRankingAsync(connection, userId, cancellationToken);
        shell.ActiveSectionKey = activeSectionKey;
        shell.PanelTitle = title;
        shell.PanelSubtitle = subtitle;
        return shell;
    }

    private async Task EnsureSalesUserAsync(SqlConnection connection, long userId, CancellationToken cancellationToken)
    {
        const string sql = "SELECT COUNT(*) FROM users WHERE id = @userId AND rol IN ('sales_admin','sales_agent') AND hesap_durumu = 1;";
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@userId", userId);
        if (Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken), CultureInfo.InvariantCulture) == 0)
        {
            throw new InvalidOperationException("Satış paneli yetkisi bulunamadı.");
        }
    }

    private async Task<int> LoadRankingAsync(SqlConnection connection, long userId, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT siralama FROM (
                SELECT satis_temsilcisi_id, DENSE_RANK() OVER (ORDER BY COALESCE(SUM(toplam_tutar),0) DESC) AS siralama
                FROM rezervasyonlar
                WHERE satis_temsilcisi_id IS NOT NULL
                  AND olusturulma_tarihi >= DATEFROMPARTS(YEAR(GETDATE()), MONTH(GETDATE()), 1)
                  AND olusturulma_tarihi < DATEADD(MONTH, 1, DATEFROMPARTS(YEAR(GETDATE()), MONTH(GETDATE()), 1))
                GROUP BY satis_temsilcisi_id
            ) t WHERE satis_temsilcisi_id = @userId;";
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@userId", userId);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is null or DBNull ? 0 : Convert.ToInt32(result, CultureInfo.InvariantCulture);
    }

    private async Task<List<SalesReservationListItemViewModel>> LoadReservationsAsync(SqlConnection connection, long userId, SalesReservationsFilterViewModel filters, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT r.id, r.rezervasyon_no, o.otel_adi, r.misafir_ad_soyad, COALESCE(r.misafir_eposta,''), COALESCE(r.misafir_telefon,''),
                   FORMAT(r.giris_tarihi, 'dd.MM.yyyy', 'tr-TR'), FORMAT(r.cikis_tarihi, 'dd.MM.yyyy', 'tr-TR'),
                   r.gece_sayisi, ot.oda_adi, r.durum, r.otel_onay_durumu, r.toplam_tutar, r.komisyon_tutari,
                   FORMAT(r.olusturulma_tarihi, 'dd.MM.yyyy HH:mm', 'tr-TR'), COALESCE(r.rezervasyon_kanali,''), COALESCE(r.musteri_talep_notu,'')
            FROM rezervasyonlar r
            INNER JOIN oteller o ON o.id = r.otel_id
            INNER JOIN oda_tipleri ot ON ot.id = r.oda_tip_id
            WHERE r.satis_temsilcisi_id = @userId
              AND (@search IS NULL OR r.rezervasyon_no LIKE CONCAT('%', @search, '%') OR o.otel_adi LIKE CONCAT('%', @search, '%') OR r.misafir_ad_soyad LIKE CONCAT('%', @search, '%') OR r.misafir_telefon LIKE CONCAT('%', @search, '%') OR r.misafir_eposta LIKE CONCAT('%', @search, '%'))
              AND (@status IS NULL OR r.durum = @status)
              AND (@approval IS NULL OR r.otel_onay_durumu = @approval)
              AND (@startDate IS NULL OR r.giris_tarihi >= @startDate)
              AND (@endDate IS NULL OR r.cikis_tarihi <= @endDate)
            ORDER BY r.olusturulma_tarihi DESC
            OFFSET @offset ROWS FETCH NEXT @limit ROWS ONLY;";
        var items = new List<SalesReservationListItemViewModel>();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@userId", userId);
        command.Parameters.AddWithValue("@search", string.IsNullOrWhiteSpace(filters.Search) ? DBNull.Value : filters.Search.Trim());
        command.Parameters.AddWithValue("@status", string.IsNullOrWhiteSpace(filters.Status) ? DBNull.Value : filters.Status.Trim());
        command.Parameters.AddWithValue("@approval", string.IsNullOrWhiteSpace(filters.Approval) ? DBNull.Value : filters.Approval.Trim());
        command.Parameters.AddWithValue("@startDate", filters.StartDate.HasValue ? filters.StartDate.Value.ToDateTime(TimeOnly.MinValue) : DBNull.Value);
        command.Parameters.AddWithValue("@endDate", filters.EndDate.HasValue ? filters.EndDate.Value.ToDateTime(TimeOnly.MinValue) : DBNull.Value);
        command.Parameters.AddWithValue("@offset", (filters.Page - 1) * filters.PageSize);
        command.Parameters.AddWithValue("@limit", filters.PageSize);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new SalesReservationListItemViewModel
            {
                ReservationId = reader.GetInt64(0),
                ReservationNo = reader.GetString(1),
                HotelName = reader.GetString(2),
                CustomerName = reader.GetString(3),
                CustomerEmail = reader.GetString(4),
                CustomerPhone = reader.GetString(5),
                StayText = $"{reader.GetString(6)} - {reader.GetString(7)} · {ReadInt(reader, 8)} gece",
                RoomName = reader.GetString(9),
                StatusText = reader.GetString(10),
                ApprovalText = reader.GetString(11),
                TotalText = FormatMoney(ReadDecimal(reader, 12)),
                CommissionText = FormatMoney(ReadDecimal(reader, 13)),
                CreatedAtText = reader.GetString(14),
                ChannelText = reader.GetString(15),
                DemandNote = reader.GetString(16)
            });
        }
        return items;
    }

    private async Task<List<SalesCustomerCardViewModel>> LoadCustomersAsync(SqlConnection connection, string? search, int limit, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT TOP (@limit) id, musteri_kodu, ad_soyad, COALESCE(eposta,''), COALESCE(telefon,''), uyelik_seviyesi,
                   toplam_rezervasyon_sayisi, son_rezervasyon_tarihi, COALESCE(son_talep_ozeti,''), toplam_harcama,
                   CONCAT_WS(', ', NULLIF(mahalle,''), NULLIF(ilce,''), NULLIF(sehir,'')),
                   COALESCE(sehir,''), COALESCE(ilce,''), COALESCE(mahalle,''), COALESCE(adres,'')
            FROM satis_musterileri
            WHERE (@search IS NULL OR ad_soyad LIKE CONCAT('%', @search, '%') OR eposta LIKE CONCAT('%', @search, '%') OR telefon LIKE CONCAT('%', @search, '%'))
            ORDER BY toplam_rezervasyon_sayisi DESC, guncellenme_tarihi DESC, olusturulma_tarihi DESC;";
        var items = new List<SalesCustomerCardViewModel>();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@search", string.IsNullOrWhiteSpace(search) ? DBNull.Value : search.Trim());
        command.Parameters.AddWithValue("@limit", limit);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new SalesCustomerCardViewModel
            {
                CustomerId = reader.GetInt64(0),
                CustomerCode = reader.GetString(1),
                FullName = reader.GetString(2),
                Email = reader.GetString(3),
                Phone = reader.GetString(4),
                MembershipLevel = reader.GetString(5),
                ReservationCountText = $"{ReadInt(reader, 6)} rezervasyon",
                LastStayText = reader.IsDBNull(7) ? "Henüz rezervasyon yok" : reader.GetDateTime(7).ToString("dd.MM.yyyy", CultureInfo.InvariantCulture),
                LastRequestSummary = reader.GetString(8),
                TotalSpendText = FormatMoney(ReadDecimal(reader, 9)),
                LocationText = reader.IsDBNull(10) ? "Konum bilgisi yok" : reader.GetString(10),
                City = reader.GetString(11),
                District = reader.GetString(12),
                Neighborhood = reader.GetString(13),
                Address = reader.GetString(14)
            });
        }
        return items;
    }

    private async Task<List<SalesSelectOption>> LoadHotelOptionsAsync(SqlConnection connection, string? search, string? city, string? district, string? neighborhood, CancellationToken cancellationToken)
        => await LoadOptionsAsync(connection, @"
            SELECT TOP (250)
                id,
                CONCAT(
                    otel_adi,
                    CASE
                        WHEN NULLIF(TRIM(CONCAT_WS(', ', NULLIF(ilce,''), NULLIF(sehir,''))), '') IS NULL THEN ''
                        ELSE CONCAT(' · ', TRIM(CONCAT_WS(', ', NULLIF(ilce,''), NULLIF(sehir,''))))
                    END
                ) AS etiket
            FROM oteller
            WHERE yayin_durumu IN ('Yayında','Bakımda')
              AND onay_durumu = 'Onaylandı'
              AND (@search IS NULL OR otel_adi LIKE CONCAT('%', @search, '%') OR sehir LIKE CONCAT('%', @search, '%') OR ilce LIKE CONCAT('%', @search, '%') OR tam_adres LIKE CONCAT('%', @search, '%'))
              AND (@city IS NULL OR sehir = @city)
              AND (@district IS NULL OR ilce = @district)
              AND (@neighborhood IS NULL OR tam_adres LIKE CONCAT('%', @neighborhood, '%'))
            ORDER BY
                CASE WHEN @search IS NOT NULL AND otel_adi LIKE CONCAT(@search, '%') THEN 0 ELSE 1 END,
                one_cikan_otel DESC,
                populerlik_sirasi DESC,
                ortalama_puan DESC,
                toplam_yorum_sayisi DESC,
                otel_adi;",
            cmd =>
            {
                cmd.Parameters.AddWithValue("@search", string.IsNullOrWhiteSpace(search) ? DBNull.Value : search.Trim());
                cmd.Parameters.AddWithValue("@city", string.IsNullOrWhiteSpace(city) ? DBNull.Value : city.Trim());
                cmd.Parameters.AddWithValue("@district", string.IsNullOrWhiteSpace(district) ? DBNull.Value : district.Trim());
                cmd.Parameters.AddWithValue("@neighborhood", string.IsNullOrWhiteSpace(neighborhood) ? DBNull.Value : neighborhood.Trim());
            },
            cancellationToken);

    private async Task<List<SalesSelectOption>> LoadCitiesAsync(SqlConnection connection, CancellationToken cancellationToken)
        => await LoadOptionsAsync(connection, "SELECT MIN(id), sehir FROM oteller GROUP BY sehir ORDER BY sehir;", null, cancellationToken);

    private async Task<List<SalesSelectOption>> LoadDistrictsAsync(SqlConnection connection, string? city, CancellationToken cancellationToken)
        => await LoadOptionsAsync(connection, "SELECT MIN(id), ilce FROM oteller WHERE (@city IS NULL OR sehir = @city) GROUP BY ilce ORDER BY ilce;", cmd => cmd.Parameters.AddWithValue("@city", string.IsNullOrWhiteSpace(city) ? DBNull.Value : city.Trim()), cancellationToken);

    private async Task<List<SalesSelectOption>> LoadRoomTypeOptionsAsync(SqlConnection connection, long hotelId, CancellationToken cancellationToken)
        => await LoadOptionsAsync(connection, "SELECT id, oda_adi FROM oda_tipleri WHERE otel_id = @hotelId AND aktif_mi = 1 ORDER BY standart_gecelik_fiyat ASC, oda_adi;", cmd => cmd.Parameters.AddWithValue("@hotelId", hotelId), cancellationToken);

    private async Task<List<SalesRoomOptionViewModel>> LoadRoomOptionsAsync(SqlConnection connection, long hotelId, DateOnly checkIn, DateOnly checkOut, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT o.id, o.oda_adi, o.maksimum_kisi_sayisi, o.toplam_oda_sayisi,
                   COALESCE(AVG(COALESCE(ofm.indirimli_fiyat, ofm.gecelik_fiyat)), o.standart_gecelik_fiyat) AS fiyat,
                   COALESCE(MAX(ofm.toplam_oda_sayisi - ofm.satilan_oda_sayisi - ofm.bloke_oda_sayisi), o.toplam_oda_sayisi) AS stok,
                   COALESCE(feature_data.ozellikler, '')
            FROM oda_tipleri o
            LEFT JOIN oda_fiyat_musaitlik ofm ON ofm.oda_tip_id = o.id
                AND ofm.otel_id = o.otel_id
                AND ofm.tarih >= @checkIn
                AND ofm.tarih < @checkOut
            OUTER APPLY (
                SELECT STRING_AGG(feature_rows.ozellik_adi, ', ') WITHIN GROUP (ORDER BY feature_rows.siralama ASC) AS ozellikler
                FROM (
                    SELECT DISTINCT oo.ozellik_adi, oo.siralama
                    FROM oda_tipi_ozellikleri oto
                    INNER JOIN oda_ozellikleri oo ON oo.id = oto.ozellik_id AND oo.aktif_mi = 1
                    WHERE oto.oda_tip_id = o.id
                ) feature_rows
            ) feature_data
            WHERE o.otel_id = @hotelId AND o.aktif_mi = 1
            GROUP BY o.id, o.oda_adi, o.maksimum_kisi_sayisi, o.toplam_oda_sayisi, o.standart_gecelik_fiyat
            ORDER BY fiyat ASC;";
        var items = new List<SalesRoomOptionViewModel>();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@hotelId", hotelId);
        command.Parameters.AddWithValue("@checkIn", checkIn.ToDateTime(TimeOnly.MinValue));
        command.Parameters.AddWithValue("@checkOut", checkOut.ToDateTime(TimeOnly.MinValue));
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var stock = ReadInt(reader, 5);
            items.Add(new SalesRoomOptionViewModel
            {
                RoomTypeId = reader.GetInt64(0),
                RoomName = reader.GetString(1),
                CapacityText = $"{ReadInt(reader, 2)} kişiye kadar",
                StockText = stock > 0 ? $"{stock} oda müsait" : "Müsaitlik sınırlı",
                PriceText = $"{FormatMoney(ReadDecimal(reader, 4))} / gece",
                IsAvailable = stock > 0,
                FeaturesText = reader.IsDBNull(6) || string.IsNullOrWhiteSpace(reader.GetString(6))
                    ? "Hızlı rezervasyon için uygun"
                    : string.Join(" · ", SplitFeatures(reader.GetString(6)).Take(4))
            });
        }
        return items;
    }

    private async Task<SalesReservationPriceSummaryViewModel> BuildPriceSummaryAsync(SqlConnection connection, long roomTypeId, DateOnly checkIn, DateOnly checkOut, int roomCount, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT COALESCE(AVG(COALESCE(ofm.indirimli_fiyat, ofm.gecelik_fiyat)), ot.standart_gecelik_fiyat)
            FROM oda_tipleri ot
            LEFT JOIN oda_fiyat_musaitlik ofm ON ofm.oda_tip_id = ot.id
                AND ofm.otel_id = ot.otel_id
                AND ofm.tarih >= @checkIn
                AND ofm.tarih < @checkOut
            WHERE ot.id = @roomTypeId;";
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@checkIn", checkIn.ToDateTime(TimeOnly.MinValue));
        command.Parameters.AddWithValue("@checkOut", checkOut.ToDateTime(TimeOnly.MinValue));
        command.Parameters.AddWithValue("@roomTypeId", roomTypeId);
        var nightly = Convert.ToDecimal(await command.ExecuteScalarAsync(cancellationToken) ?? 0m, CultureInfo.InvariantCulture);
        if (nightly <= 0) nightly = 1000m;
        var nights = Math.Max(1, checkOut.DayNumber - checkIn.DayNumber);
        var roomTotal = nightly * nights * Math.Max(1, roomCount);
        var tax = Math.Round(roomTotal * 0.08m, 2, MidpointRounding.AwayFromZero);
        var total = roomTotal + tax;
        return new SalesReservationPriceSummaryViewModel
        {
            BaseNightlyAmount = nightly,
            RoomTotalAmount = roomTotal,
            TaxAmount = tax,
            TotalAmount = total,
            BaseAmountText = FormatMoney(roomTotal),
            TaxAmountText = FormatMoney(tax),
            TotalAmountText = FormatMoney(total)
        };
    }

    private async Task<List<SalesHotelSearchCardViewModel>> LoadHotelSearchResultsAsync(SqlConnection connection, string? searchTerm, string? city, string? district, string? neighborhood, decimal? minPrice, decimal? maxPrice, decimal? minimumRating, int? minimumReviewCount, string? feature, int resultLimit, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT o.id, o.otel_adi, o.sehir, o.ilce, o.tam_adres, COALESCE(o.rezervasyon_telefonu, o.telefon_1, ''),
                   o.ortalama_puan, o.toplam_yorum_sayisi,
                   COALESCE((SELECT MIN(standart_gecelik_fiyat) FROM oda_tipleri WHERE otel_id = o.id),0),
                   (SELECT COUNT(*) FROM rezervasyonlar r WHERE r.otel_id = o.id AND CAST(r.olusturulma_tarihi AS date) = CAST(GETDATE() AS date)),
                   COALESCE((
                       SELECT STRING_AGG(feature_rows.ozellik_adi, ', ') WITHIN GROUP (ORDER BY feature_rows.siralama ASC)
                       FROM (
                           SELECT TOP (3) oo.ozellik_adi, oo.siralama
                           FROM otel_ozellik_iliskileri il
                           INNER JOIN otel_ozellikleri oo ON oo.id = il.ozellik_id
                           WHERE il.otel_id = o.id
                           ORDER BY oo.siralama ASC
                       ) feature_rows
                   ),'')
            FROM oteller o
            WHERE o.yayin_durumu IN ('Yayında','Bakımda')
              AND o.onay_durumu = 'Onaylandı'
              AND (@searchTerm IS NULL OR o.otel_adi LIKE CONCAT('%', @searchTerm, '%') OR o.sehir LIKE CONCAT('%', @searchTerm, '%') OR o.ilce LIKE CONCAT('%', @searchTerm, '%') OR o.tam_adres LIKE CONCAT('%', @searchTerm, '%'))
              AND (@city IS NULL OR o.sehir = @city)
              AND (@district IS NULL OR o.ilce = @district)
              AND (@neighborhood IS NULL OR o.tam_adres LIKE CONCAT('%', @neighborhood, '%'))
              AND (@minimumRating IS NULL OR o.ortalama_puan >= @minimumRating)
              AND (@minimumReviewCount IS NULL OR o.toplam_yorum_sayisi >= @minimumReviewCount)
              AND (@feature IS NULL OR EXISTS (SELECT 1 FROM otel_ozellik_iliskileri il INNER JOIN otel_ozellikleri oo ON oo.id = il.ozellik_id WHERE il.otel_id = o.id AND oo.ozellik_adi LIKE CONCAT('%', @feature, '%')))
            ORDER BY
                CASE WHEN @searchTerm IS NOT NULL AND o.otel_adi LIKE CONCAT(@searchTerm, '%') THEN 0 ELSE 1 END,
                CASE WHEN @district IS NOT NULL AND o.ilce = @district THEN 0 ELSE 1 END,
                CASE WHEN @city IS NOT NULL AND o.sehir = @city THEN 0 ELSE 1 END,
                o.one_cikan_otel DESC, o.ortalama_puan DESC, o.toplam_yorum_sayisi DESC
            OFFSET 0 ROWS FETCH NEXT @resultLimit ROWS ONLY;";
        var items = new List<SalesHotelSearchCardViewModel>();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@searchTerm", string.IsNullOrWhiteSpace(searchTerm) ? DBNull.Value : searchTerm.Trim());
        command.Parameters.AddWithValue("@city", string.IsNullOrWhiteSpace(city) ? DBNull.Value : city.Trim());
        command.Parameters.AddWithValue("@district", string.IsNullOrWhiteSpace(district) ? DBNull.Value : district.Trim());
        command.Parameters.AddWithValue("@neighborhood", string.IsNullOrWhiteSpace(neighborhood) ? DBNull.Value : neighborhood.Trim());
        command.Parameters.AddWithValue("@minimumRating", minimumRating.HasValue ? minimumRating.Value : DBNull.Value);
        command.Parameters.AddWithValue("@minimumReviewCount", minimumReviewCount.HasValue ? minimumReviewCount.Value : DBNull.Value);
        command.Parameters.AddWithValue("@feature", string.IsNullOrWhiteSpace(feature) ? DBNull.Value : feature.Trim());
        command.Parameters.AddWithValue("@resultLimit", Math.Clamp(resultLimit, 1, 50));
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var price = ReadDecimal(reader, 8);
            if (minPrice.HasValue && price < minPrice.Value) continue;
            if (maxPrice.HasValue && price > maxPrice.Value) continue;
            items.Add(new SalesHotelSearchCardViewModel
            {
                HotelId = reader.GetInt64(0),
                HotelName = reader.GetString(1),
                City = reader.GetString(2),
                District = reader.GetString(3),
                Address = reader.GetString(4),
                Phone = reader.GetString(5),
                RatingText = ReadDecimal(reader, 6).ToString("0.0", CultureInfo.InvariantCulture),
                ReviewCountText = $"{ReadInt(reader, 7)} yorum",
                PriceText = price > 0 ? $"{FormatMoney(price)} / gece" : "Fiyat bekleniyor",
                TodayDemandText = $"{ReadInt(reader, 9)} kişi bugün tercih etti",
                LocationText = string.Join(" · ", new[] { reader.GetString(3), reader.GetString(2) }.Where(static x => !string.IsNullOrWhiteSpace(x))),
                FeatureBadges = SplitFeatures(reader.GetString(10))
            });
        }
        return items;
    }

    private static bool HasHotelAssistantSearch(
        string? searchTerm,
        string? city,
        string? district,
        string? neighborhood,
        decimal? minPrice,
        decimal? maxPrice,
        decimal? minimumRating,
        int? minimumReviewCount,
        string? feature)
        => !string.IsNullOrWhiteSpace(searchTerm)
           || !string.IsNullOrWhiteSpace(city)
           || !string.IsNullOrWhiteSpace(district)
           || !string.IsNullOrWhiteSpace(neighborhood)
           || !string.IsNullOrWhiteSpace(feature)
           || minPrice.HasValue
           || maxPrice.HasValue
           || minimumRating.HasValue
           || minimumReviewCount.HasValue;

    private static async Task<SalesSelectOption?> LoadHotelOptionByIdAsync(SqlConnection connection, long hotelId, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT id,
                   CONCAT(
                       otel_adi,
                       CASE
                           WHEN NULLIF(TRIM(CONCAT_WS(', ', NULLIF(ilce,''), NULLIF(sehir,''))), '') IS NULL THEN ''
                           ELSE CONCAT(' · ', TRIM(CONCAT_WS(', ', NULLIF(ilce,''), NULLIF(sehir,''))))
                       END
                   ) AS etiket
            FROM oteller
            WHERE id = @hotelId;";
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@hotelId", hotelId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new SalesSelectOption
        {
            Value = reader.GetInt64(0),
            Label = reader.GetString(1)
        };
    }

    private async Task<List<SalesAvailabilityDayViewModel>> LoadAvailabilityDaysAsync(SqlConnection connection, long roomTypeId, DateOnly monthStart, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT ofm.tarih, ofm.gecelik_fiyat, ofm.indirimli_fiyat, ofm.toplam_oda_sayisi, ofm.satilan_oda_sayisi, ofm.bloke_oda_sayisi
            FROM oda_fiyat_musaitlik ofm
            INNER JOIN oda_tipleri ot ON ot.id = ofm.oda_tip_id
            WHERE ofm.oda_tip_id = @roomTypeId
              AND ofm.otel_id = ot.otel_id
              AND ofm.tarih >= @monthStart
              AND ofm.tarih < DATEADD(MONTH, 1, @monthStart)
            ORDER BY ofm.tarih;";
        var items = new List<SalesAvailabilityDayViewModel>();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@roomTypeId", roomTypeId);
        command.Parameters.AddWithValue("@monthStart", monthStart.ToDateTime(TimeOnly.MinValue));
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var total = ReadInt(reader, 3);
            var available = total - ReadInt(reader, 4) - ReadInt(reader, 5);
            var basePrice = ReadDecimal(reader, 1);
            var campaignPrice = ReadDecimal(reader, 2);
            items.Add(new SalesAvailabilityDayViewModel
            {
                Date = DateOnly.FromDateTime(reader.GetDateTime(0)),
                IsAvailable = available > 0,
                PriceText = FormatMoney(basePrice),
                CampaignPriceText = campaignPrice > 0 && campaignPrice != basePrice ? FormatMoney(campaignPrice) : string.Empty,
                StockText = $"{Math.Max(0, available)} oda",
                SoldOutText = available > 0 ? "Müsait" : "Kapalı / Dolu"
            });
        }
        return items;
    }

    private async Task<List<SalesHotelGuideItemViewModel>> LoadHotelGuideAsync(SqlConnection connection, string? search, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT o.id, o.otel_adi, COALESCE(o.rezervasyon_telefonu, o.telefon_1, ''),
                   COALESCE(o.satis_kontak_adi,''), COALESCE(o.satis_kontak_telefonu,''), COALESCE(o.satis_kontak_eposta,''),
                   o.tam_adres, COALESCE(o.satis_notlari,''),
                   (SELECT COUNT(*) FROM rezervasyonlar r WHERE r.otel_id = o.id AND CAST(r.olusturulma_tarihi AS date) = CAST(GETDATE() AS date))
            FROM oteller o
            WHERE o.yayin_durumu IN ('Yayında','Bakımda')
              AND o.onay_durumu = 'Onaylandı'
              AND (@search IS NULL OR o.otel_adi LIKE CONCAT('%', @search, '%') OR o.sehir LIKE CONCAT('%', @search, '%') OR o.ilce LIKE CONCAT('%', @search, '%'))
            ORDER BY 9 DESC, o.one_cikan_otel DESC, o.otel_adi;";
        var items = new List<SalesHotelGuideItemViewModel>();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@search", string.IsNullOrWhiteSpace(search) ? DBNull.Value : search.Trim());
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new SalesHotelGuideItemViewModel
            {
                HotelId = reader.GetInt64(0),
                HotelName = reader.GetString(1),
                Phone = reader.GetString(2),
                SalesContactName = reader.GetString(3),
                SalesContactPhone = reader.GetString(4),
                SalesContactEmail = reader.GetString(5),
                Address = reader.GetString(6),
                Note = reader.GetString(7),
                PreferredCountText = $"{ReadInt(reader, 8)} kişi bugün tercih etti"
            });
        }
        return items;
    }

    private async Task<long> EnsureSalesCustomerAsync(SqlConnection connection, SqlTransaction transaction, long userId, SalesReservationCreateModel model, CancellationToken cancellationToken, string? membershipLevel = null)
    {
        const string findSql = "SELECT TOP (1) id FROM satis_musterileri WHERE (eposta = @email AND @email <> '') OR (telefon = @phone AND @phone <> '');";
        await using (var findCommand = new SqlCommand(findSql, connection, (SqlTransaction)transaction))
        {
            findCommand.Parameters.AddWithValue("@email", model.CustomerEmail.Trim());
            findCommand.Parameters.AddWithValue("@phone", model.CustomerPhone.Trim());
            var existing = await findCommand.ExecuteScalarAsync(cancellationToken);
            if (existing is not null and not DBNull) return Convert.ToInt64(existing, CultureInfo.InvariantCulture);
        }

        await using var seqCommand = new SqlCommand("SELECT COUNT(*) + 1 FROM satis_musterileri;", connection, (SqlTransaction)transaction);
        var seq = Convert.ToInt32(await seqCommand.ExecuteScalarAsync(cancellationToken), CultureInfo.InvariantCulture);
        var code = $"SATMUST-{seq:0000}";
        const string insertSql = @"
            INSERT INTO satis_musterileri
            (musteri_kodu, ad_soyad, eposta, telefon, sehir, ilce, mahalle, adres, uyelik_seviyesi, son_talep_ozeti, notlar, olusturan_sales_user_id)
            VALUES
            (@code, @fullName, @email, @phone, @city, @district, @neighborhood, @address, @membership, @summary, @notes, @userId);
            SELECT CAST(SCOPE_IDENTITY() AS bigint);";
        await using var insertCommand = new SqlCommand(insertSql, connection, (SqlTransaction)transaction);
        insertCommand.Parameters.AddWithValue("@code", code);
        insertCommand.Parameters.AddWithValue("@fullName", model.CustomerFullName.Trim());
        insertCommand.Parameters.AddWithValue("@email", model.CustomerEmail.Trim());
        insertCommand.Parameters.AddWithValue("@phone", model.CustomerPhone.Trim());
        insertCommand.Parameters.AddWithValue("@city", string.IsNullOrWhiteSpace(model.CustomerCity) ? DBNull.Value : model.CustomerCity.Trim());
        insertCommand.Parameters.AddWithValue("@district", string.IsNullOrWhiteSpace(model.CustomerDistrict) ? DBNull.Value : model.CustomerDistrict.Trim());
        insertCommand.Parameters.AddWithValue("@neighborhood", string.IsNullOrWhiteSpace(model.CustomerNeighborhood) ? DBNull.Value : model.CustomerNeighborhood.Trim());
        insertCommand.Parameters.AddWithValue("@address", string.IsNullOrWhiteSpace(model.CustomerAddress) ? DBNull.Value : model.CustomerAddress.Trim());
        insertCommand.Parameters.AddWithValue("@membership", string.IsNullOrWhiteSpace(membershipLevel) ? "Standart" : membershipLevel);
        insertCommand.Parameters.AddWithValue("@summary", model.DemandNote ?? string.Empty);
        insertCommand.Parameters.AddWithValue("@notes", model.DemandNote ?? string.Empty);
        insertCommand.Parameters.AddWithValue("@userId", userId);
        return Convert.ToInt64(await insertCommand.ExecuteScalarAsync(cancellationToken), CultureInfo.InvariantCulture);
    }

    private async Task<long> EnsurePublicCustomerUserAsync(SqlConnection connection, SqlTransaction transaction, SalesReservationCreateModel model, string emailForUser, CancellationToken cancellationToken)
    {
        const string findSql = "SELECT TOP (1) id FROM users WHERE eposta = @email OR (telefon = @phone AND @phone <> '') ORDER BY id;";
        await using (var findCommand = new SqlCommand(findSql, connection, (SqlTransaction)transaction))
        {
            findCommand.Parameters.AddWithValue("@email", emailForUser.Trim());
            findCommand.Parameters.AddWithValue("@phone", (model.CustomerPhone ?? string.Empty).Trim());
            var existing = await findCommand.ExecuteScalarAsync(cancellationToken);
            if (existing is not null and not DBNull) return Convert.ToInt64(existing, CultureInfo.InvariantCulture);
        }

        const string insertSql = @"
            INSERT INTO users
            (ad_soyad, eposta, telefon, sehir, ilce, mahalle, adres, sifre, rol, hesap_durumu, dil_tercihi, para_birimi, ulke)
            VALUES
            (@fullName, @email, @phone, @city, @district, @neighborhood, @address, CONVERT(varchar(64), HASHBYTES('SHA2_256', '1585'), 2), 'user', 1, 'tr', 'TRY', N'Türkiye');
            SELECT CAST(SCOPE_IDENTITY() AS bigint);";
        await using var insertCommand = new SqlCommand(insertSql, connection, (SqlTransaction)transaction);
        insertCommand.Parameters.AddWithValue("@fullName", model.CustomerFullName.Trim());
        insertCommand.Parameters.AddWithValue("@email", emailForUser.Trim());
        insertCommand.Parameters.AddWithValue("@phone", (model.CustomerPhone ?? string.Empty).Trim());
        insertCommand.Parameters.AddWithValue("@city", string.IsNullOrWhiteSpace(model.CustomerCity) ? DBNull.Value : model.CustomerCity.Trim());
        insertCommand.Parameters.AddWithValue("@district", string.IsNullOrWhiteSpace(model.CustomerDistrict) ? DBNull.Value : model.CustomerDistrict.Trim());
        insertCommand.Parameters.AddWithValue("@neighborhood", string.IsNullOrWhiteSpace(model.CustomerNeighborhood) ? DBNull.Value : model.CustomerNeighborhood.Trim());
        insertCommand.Parameters.AddWithValue("@address", string.IsNullOrWhiteSpace(model.CustomerAddress) ? DBNull.Value : model.CustomerAddress.Trim());
        return Convert.ToInt64(await insertCommand.ExecuteScalarAsync(cancellationToken), CultureInfo.InvariantCulture);
    }

    private static string BuildPlaceholderEmail(string phone)
    {
        var digits = new string((phone ?? string.Empty).Where(char.IsDigit).ToArray());
        if (string.IsNullOrWhiteSpace(digits)) digits = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);
        if (digits.Length > 25) digits = digits[^25..];
        return $"noemail_{digits}@guest.otelturizm.local";
    }

    private async Task<string> GenerateReservationNoAsync(SqlConnection connection, SqlTransaction transaction, CancellationToken cancellationToken)
    {
        await using var command = new SqlCommand("SELECT COUNT(*) + 1 FROM rezervasyonlar WHERE CAST(olusturulma_tarihi AS date) = CAST(GETDATE() AS date);", connection, (SqlTransaction)transaction);
        var seq = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken), CultureInfo.InvariantCulture);
        return $"SAT-{DateTime.Now:yyyyMMdd}-{seq:0000}";
    }

    private async Task<(string HotelName, decimal CommissionRate)> GetHotelSummaryAsync(SqlConnection connection, long hotelId, CancellationToken cancellationToken)
    {
        await using var command = new SqlCommand("SELECT TOP (1) otel_adi, varsayilan_komisyon_orani FROM oteller WHERE id = @hotelId;", connection);
        command.Parameters.AddWithValue("@hotelId", hotelId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) throw new InvalidOperationException("Otel bulunamadı.");
        return (reader.GetString(0), ReadDecimal(reader, 1));
    }

    private async Task<string> GetRoomNameAsync(SqlConnection connection, long roomTypeId, long hotelId, CancellationToken cancellationToken)
    {
        await using var command = new SqlCommand("SELECT TOP (1) oda_adi FROM oda_tipleri WHERE id = @roomTypeId AND otel_id = @hotelId;", connection);
        command.Parameters.AddWithValue("@roomTypeId", roomTypeId);
        command.Parameters.AddWithValue("@hotelId", hotelId);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        if (result is null or DBNull) throw new InvalidOperationException("Oda tipi bulunamadı.");
        return Convert.ToString(result, CultureInfo.InvariantCulture) ?? "Oda";
    }

    private async Task<(long UserId, string Email)> ResolvePartnerRecipientAsync(SqlConnection connection, long hotelId, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT TOP (1) COALESCE(o.user_id, oks.user_id, 1), COALESCE(o.satis_kontak_eposta, u.eposta, o.eposta, 'partner@otelturizm.com')
            FROM oteller o
            LEFT JOIN otel_kullanici_sahiplikleri oks ON oks.otel_id = o.id AND oks.aktif_mi = 1
            LEFT JOIN users u ON u.id = COALESCE(o.user_id, oks.user_id)
            WHERE o.id = @hotelId
            ORDER BY oks.ana_sorumlu_mu DESC, oks.id ASC;";
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@hotelId", hotelId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken)) return (reader.IsDBNull(0) ? 1L : reader.GetInt64(0), reader.GetString(1));
        return (1, "partner@otelturizm.com");
    }

    private static async Task<List<SalesSelectOption>> LoadOptionsAsync(SqlConnection connection, string sql, Action<SqlCommand>? configure, CancellationToken cancellationToken)
    {
        var items = new List<SalesSelectOption>();
        await using var command = new SqlCommand(sql, connection);
        configure?.Invoke(command);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken)) items.Add(new SalesSelectOption { Value = reader.GetInt64(0), Label = reader.GetString(1) });
        return items;
    }

    private async Task<SalesCustomerCardViewModel?> LoadCustomerPrefillAsync(SqlConnection connection, long customerId, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT id, musteri_kodu, ad_soyad, COALESCE(eposta,''), COALESCE(telefon,''), COALESCE(uyelik_seviyesi,'Standart'),
                   toplam_rezervasyon_sayisi, son_rezervasyon_tarihi, COALESCE(son_talep_ozeti,''), toplam_harcama,
                   COALESCE(sehir,''), COALESCE(ilce,''), COALESCE(mahalle,''), COALESCE(adres,'')
            FROM satis_musterileri
            WHERE id = @customerId;";
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@customerId", customerId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new SalesCustomerCardViewModel
        {
            CustomerId = reader.GetInt64(0),
            CustomerCode = reader.GetString(1),
            FullName = reader.GetString(2),
            Email = reader.GetString(3),
            Phone = reader.GetString(4),
            MembershipLevel = reader.GetString(5),
            ReservationCountText = $"{ReadInt(reader, 6)} rezervasyon",
            LastStayText = reader.IsDBNull(7) ? "Henüz rezervasyon yok" : reader.GetDateTime(7).ToString("dd.MM.yyyy", CultureInfo.InvariantCulture),
            LastRequestSummary = reader.GetString(8),
            TotalSpendText = FormatMoney(ReadDecimal(reader, 9)),
            LocationText = string.Join(", ", new[] { reader.GetString(12), reader.GetString(11), reader.GetString(10) }.Where(static x => !string.IsNullOrWhiteSpace(x))),
            City = reader.GetString(10),
            District = reader.GetString(11),
            Neighborhood = reader.GetString(12),
            Address = reader.GetString(13)
        };
    }

    private async Task<string> BuildSelectedHotelSummaryAsync(SqlConnection connection, long hotelId, CancellationToken cancellationToken)
    {
        if (hotelId <= 0)
        {
            return "Henüz otel seçilmedi. Operatörler otel adı, il, ilçe veya mahalle ile hızlı arama yapabilir.";
        }

        const string sql = @"
            SELECT otel_adi, COALESCE(ilce,''), COALESCE(sehir,''), COALESCE(ortalama_puan,0), COALESCE(toplam_yorum_sayisi,0),
                   COALESCE(rezervasyon_telefonu, telefon_1, '')
            FROM oteller
            WHERE id = @hotelId;";
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@hotelId", hotelId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return "Otel bilgisi bulunamadı.";
        }

        var location = string.Join(", ", new[] { reader.GetString(1), reader.GetString(2) }.Where(static x => !string.IsNullOrWhiteSpace(x)));
        return $"{reader.GetString(0)} · {location} · {ReadDecimal(reader, 3):0.0} puan · {ReadInt(reader, 4)} yorum · {reader.GetString(5)}";
    }

    private static string BuildMonthQuery(long? hotelId, long? roomTypeId, string? search, DateOnly monthAnchor)
    {
        var queryParts = new List<string>
        {
            $"year={monthAnchor.Year}",
            $"month={monthAnchor.Month}"
        };
        if (hotelId.HasValue && hotelId.Value > 0) queryParts.Add($"hotelId={hotelId.Value}");
        if (roomTypeId.HasValue && roomTypeId.Value > 0) queryParts.Add($"roomTypeId={roomTypeId.Value}");
        if (!string.IsNullOrWhiteSpace(search)) queryParts.Add($"search={Uri.EscapeDataString(search.Trim())}");
        return string.Join("&", queryParts);
    }

    private static void NormalizeReservationFilters(SalesReservationsFilterViewModel filters)
    {
        filters.Page = filters.Page <= 0 ? 1 : filters.Page;
        filters.PageSize = filters.PageSize is 10 or 20 or 30 ? filters.PageSize : 10;
        filters.Search = string.IsNullOrWhiteSpace(filters.Search) ? null : filters.Search.Trim();
        filters.Status = string.IsNullOrWhiteSpace(filters.Status) ? null : filters.Status.Trim();
        filters.Approval = string.IsNullOrWhiteSpace(filters.Approval) ? null : filters.Approval.Trim();

        if (filters.StartDate.HasValue && filters.EndDate.HasValue && filters.EndDate < filters.StartDate)
        {
            (filters.StartDate, filters.EndDate) = (filters.EndDate, filters.StartDate);
        }
    }

    private async Task<SalesPaginationViewModel> LoadReservationsPaginationAsync(SqlConnection connection, long userId, SalesReservationsFilterViewModel filters, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT COUNT(*)
            FROM rezervasyonlar r
            INNER JOIN oteller o ON o.id = r.otel_id
            WHERE r.satis_temsilcisi_id = @userId
              AND (@search IS NULL OR r.rezervasyon_no LIKE CONCAT('%', @search, '%') OR o.otel_adi LIKE CONCAT('%', @search, '%') OR r.misafir_ad_soyad LIKE CONCAT('%', @search, '%') OR r.misafir_telefon LIKE CONCAT('%', @search, '%') OR r.misafir_eposta LIKE CONCAT('%', @search, '%'))
              AND (@status IS NULL OR r.durum = @status)
              AND (@approval IS NULL OR r.otel_onay_durumu = @approval)
              AND (@startDate IS NULL OR r.giris_tarihi >= @startDate)
              AND (@endDate IS NULL OR r.cikis_tarihi <= @endDate);";
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@userId", userId);
        command.Parameters.AddWithValue("@search", filters.Search is null ? DBNull.Value : filters.Search);
        command.Parameters.AddWithValue("@status", filters.Status is null ? DBNull.Value : filters.Status);
        command.Parameters.AddWithValue("@approval", filters.Approval is null ? DBNull.Value : filters.Approval);
        command.Parameters.AddWithValue("@startDate", filters.StartDate.HasValue ? filters.StartDate.Value.ToDateTime(TimeOnly.MinValue) : DBNull.Value);
        command.Parameters.AddWithValue("@endDate", filters.EndDate.HasValue ? filters.EndDate.Value.ToDateTime(TimeOnly.MinValue) : DBNull.Value);

        return new SalesPaginationViewModel
        {
            Page = filters.Page,
            PageSize = filters.PageSize,
            TotalCount = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken) ?? 0, CultureInfo.InvariantCulture)
        };
    }

    private async Task<SalesReservationSummaryViewModel> LoadReservationSummaryAsync(SqlConnection connection, long userId, SalesReservationsFilterViewModel filters, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT COUNT(*),
                   SUM(CASE WHEN r.otel_onay_durumu = 'Onaylandı' OR r.durum = 'Onaylandı' THEN 1 ELSE 0 END),
                   SUM(CASE WHEN r.durum = 'İptal Edildi' THEN 1 ELSE 0 END),
                   COALESCE(SUM(r.toplam_tutar), 0)
            FROM rezervasyonlar r
            INNER JOIN oteller o ON o.id = r.otel_id
            WHERE r.satis_temsilcisi_id = @userId
              AND (@search IS NULL OR r.rezervasyon_no LIKE CONCAT('%', @search, '%') OR o.otel_adi LIKE CONCAT('%', @search, '%') OR r.misafir_ad_soyad LIKE CONCAT('%', @search, '%') OR r.misafir_telefon LIKE CONCAT('%', @search, '%') OR r.misafir_eposta LIKE CONCAT('%', @search, '%'))
              AND (@status IS NULL OR r.durum = @status)
              AND (@approval IS NULL OR r.otel_onay_durumu = @approval)
              AND (@startDate IS NULL OR r.giris_tarihi >= @startDate)
              AND (@endDate IS NULL OR r.cikis_tarihi <= @endDate);";
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@userId", userId);
        command.Parameters.AddWithValue("@search", filters.Search is null ? DBNull.Value : filters.Search);
        command.Parameters.AddWithValue("@status", filters.Status is null ? DBNull.Value : filters.Status);
        command.Parameters.AddWithValue("@approval", filters.Approval is null ? DBNull.Value : filters.Approval);
        command.Parameters.AddWithValue("@startDate", filters.StartDate.HasValue ? filters.StartDate.Value.ToDateTime(TimeOnly.MinValue) : DBNull.Value);
        command.Parameters.AddWithValue("@endDate", filters.EndDate.HasValue ? filters.EndDate.Value.ToDateTime(TimeOnly.MinValue) : DBNull.Value);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return new SalesReservationSummaryViewModel();
        }

        return new SalesReservationSummaryViewModel
        {
            TotalCount = ReadInt(reader, 0),
            ApprovedCount = ReadInt(reader, 1),
            CancelledCount = ReadInt(reader, 2),
            TotalRevenueText = FormatMoney(ReadDecimal(reader, 3))
        };
    }

    private async Task<List<SalesMonthlyPerformanceItemViewModel>> LoadMonthlyPerformanceAsync(SqlConnection connection, long userId, int year, int page, int pageSize, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT MONTH(olusturulma_tarihi) AS ay,
                   COUNT(*) AS rezervasyon_sayisi,
                   SUM(CASE WHEN durum = 'Onaylandı' OR otel_onay_durumu = 'Onaylandı' THEN 1 ELSE 0 END) AS onaylanan,
                   SUM(CASE WHEN durum = 'İptal Edildi' THEN 1 ELSE 0 END) AS iptal_edilen,
                   COALESCE(SUM(toplam_tutar), 0) AS toplam_tutar
            FROM rezervasyonlar
            WHERE satis_temsilcisi_id = @userId
              AND YEAR(olusturulma_tarihi) = @year
            GROUP BY MONTH(olusturulma_tarihi)
            ORDER BY ay DESC
            OFFSET @offset ROWS FETCH NEXT @limit ROWS ONLY;";
        var items = new List<SalesMonthlyPerformanceItemViewModel>();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@userId", userId);
        command.Parameters.AddWithValue("@year", year);
        command.Parameters.AddWithValue("@offset", (page - 1) * pageSize);
        command.Parameters.AddWithValue("@limit", pageSize);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var month = ReadInt(reader, 0);
            items.Add(new SalesMonthlyPerformanceItemViewModel
            {
                PeriodLabel = new DateTime(year, Math.Max(1, month), 1).ToString("MMMM yyyy", CultureInfo.GetCultureInfo("tr-TR")),
                ReservationCount = ReadInt(reader, 1),
                ApprovedCount = ReadInt(reader, 2),
                CancelledCount = ReadInt(reader, 3),
                RevenueText = FormatMoney(ReadDecimal(reader, 4))
            });
        }
        return items;
    }

    private async Task<int> CountMonthlyPerformanceRowsAsync(SqlConnection connection, long userId, int year, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT COUNT(*)
            FROM (
                SELECT MONTH(olusturulma_tarihi) AS ay
                FROM rezervasyonlar
                WHERE satis_temsilcisi_id = @userId
                  AND YEAR(olusturulma_tarihi) = @year
                GROUP BY MONTH(olusturulma_tarihi)
            ) aylik;";
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@userId", userId);
        command.Parameters.AddWithValue("@year", year);
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken) ?? 0, CultureInfo.InvariantCulture);
    }

    private static decimal ReadDecimal(SqlDataReader reader, int index) => reader.IsDBNull(index) ? 0m : Convert.ToDecimal(reader.GetValue(index), CultureInfo.InvariantCulture);
    private static int ReadInt(SqlDataReader reader, int index) => reader.IsDBNull(index) ? 0 : Convert.ToInt32(reader.GetValue(index), CultureInfo.InvariantCulture);
    private static string FormatMoney(decimal value) => value.ToString("'₺'#,##0.##", CultureInfo.GetCultureInfo("tr-TR"));
    private static List<string> SplitFeatures(string raw) => string.IsNullOrWhiteSpace(raw) ? new List<string>() : raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Take(3).ToList();
    private static string SplitFirstName(string fullName) => string.IsNullOrWhiteSpace(fullName) ? "Misafir" : fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "Misafir";

    private async Task<SqlConnection> OpenConnectionAsync(CancellationToken cancellationToken)
    {
        var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }
}


