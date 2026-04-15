using System.Globalization;
using MySqlConnector;
using otelturizmnew.Models.Email;
using otelturizmnew.Models.Paneller.Satis;
using otelturizmnew.Services.Abstractions;

namespace otelturizmnew.Services;

public class SalesService : ISalesService
{
    private readonly string _connectionString;
    private readonly IEmailQueueService _emailQueueService;

    public SalesService(IConfiguration configuration, IEmailQueueService emailQueueService)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection tanimli degil.");
        _emailQueueService = emailQueueService;
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
              AND olusturulma_tarihi >= DATE_FORMAT(CURDATE(), '%Y-%m-01')
              AND olusturulma_tarihi < DATE_ADD(DATE_FORMAT(CURDATE(), '%Y-%m-01'), INTERVAL 1 MONTH);";
        await using (var command = new MySqlCommand(sql, connection))
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
        model.RecentReservations = await LoadReservationsAsync(connection, userId, 6, cancellationToken);
        return model;
    }

    public async Task<SalesCreateReservationPageViewModel> GetCreateReservationAsync(long userId, long? hotelId = null, long? roomTypeId = null, string? searchTerm = null, string? city = null, string? district = null, string? neighborhood = null, decimal? minPrice = null, decimal? maxPrice = null, decimal? minimumRating = null, int? minimumReviewCount = null, string? feature = null, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        var shell = await BuildShellAsync(connection, userId, "create", "Yeni Rezervasyon Oluştur", "İl, ilçe, fiyat ve özellik filtreleriyle oteli hızlıca bulun; müşteriye en uygun rezervasyonu aynı ekrandan tamamlayın.", cancellationToken);
        var hotels = await LoadHotelOptionsAsync(connection, cancellationToken);
        var selectedHotelId = hotelId ?? hotels.FirstOrDefault()?.Value ?? 0;
        var roomTypes = selectedHotelId > 0 ? await LoadRoomTypeOptionsAsync(connection, selectedHotelId, cancellationToken) : new List<SalesSelectOption>();
        var selectedRoomTypeId = roomTypeId ?? roomTypes.FirstOrDefault()?.Value ?? 0;

        var model = new SalesCreateReservationPageViewModel
        {
            Shell = shell,
            Form = new SalesReservationCreateModel
            {
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
            HotelSearchResults = await LoadHotelSearchResultsAsync(connection, searchTerm, city, district, neighborhood, minPrice, maxPrice, minimumRating, minimumReviewCount, feature, cancellationToken),
            AvailableRooms = selectedHotelId > 0 ? await LoadRoomOptionsAsync(connection, selectedHotelId, DateOnly.FromDateTime(DateTime.Today.AddDays(2)), DateOnly.FromDateTime(DateTime.Today.AddDays(4)), cancellationToken) : new List<SalesRoomOptionViewModel>()
        };

        if (selectedRoomTypeId > 0)
        {
            model.Summary = await BuildPriceSummaryAsync(connection, selectedRoomTypeId, model.Form.CheckInDate, model.Form.CheckOutDate, model.Form.RoomCount, cancellationToken);
        }

        return model;
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

    public async Task<SalesAvailabilityPageViewModel> GetAvailabilityAsync(long userId, long? hotelId = null, long? roomTypeId = null, DateOnly? month = null, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        var shell = await BuildShellAsync(connection, userId, "availability", "Müsaitlik Takvimi", "Otel ve oda seçerek günlük fiyat ile müsaitliği anlık görün.", cancellationToken);
        var hotels = await LoadHotelOptionsAsync(connection, cancellationToken);
        var selectedHotelId = hotelId ?? hotels.FirstOrDefault()?.Value ?? 0;
        var roomTypes = selectedHotelId > 0 ? await LoadRoomTypeOptionsAsync(connection, selectedHotelId, cancellationToken) : new List<SalesSelectOption>();
        var selectedRoomTypeId = roomTypeId ?? roomTypes.FirstOrDefault()?.Value ?? 0;
        var targetMonth = month ?? DateOnly.FromDateTime(DateTime.Today);

        return new SalesAvailabilityPageViewModel
        {
            Shell = shell,
            SelectedHotelId = selectedHotelId,
            SelectedRoomTypeId = selectedRoomTypeId,
            SelectedMonth = new DateOnly(targetMonth.Year, targetMonth.Month, 1),
            Hotels = hotels,
            RoomTypes = roomTypes,
            Days = selectedRoomTypeId > 0 ? await LoadAvailabilityDaysAsync(connection, selectedRoomTypeId, new DateOnly(targetMonth.Year, targetMonth.Month, 1), cancellationToken) : new List<SalesAvailabilityDayViewModel>()
        };
    }

    public async Task<SalesReservationsPageViewModel> GetReservationsAsync(long userId, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        return new SalesReservationsPageViewModel
        {
            Shell = await BuildShellAsync(connection, userId, "reservations", "Rezervasyonlarım", "Satış panelinden açtığınız rezervasyonları durum ve komisyon bilgileriyle takip edin.", cancellationToken),
            Reservations = await LoadReservationsAsync(connection, userId, 150, cancellationToken)
        };
    }

    public async Task<SalesReportsPageViewModel> GetReportsAsync(long userId, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        var model = new SalesReportsPageViewModel
        {
            Shell = await BuildShellAsync(connection, userId, "reports", "Raporlar", "Aylık rezervasyon hacmi, ciro ve komisyon verisini satış performansı olarak görün.", cancellationToken)
        };

        const string sql = @"
            SELECT COALESCE(SUM(toplam_tutar),0), COALESCE(SUM(komisyon_tutari),0), COUNT(*)
            FROM rezervasyonlar
            WHERE satis_temsilcisi_id = @userId
              AND olusturulma_tarihi >= DATE_FORMAT(CURDATE(), '%Y-%m-01')
              AND olusturulma_tarihi < DATE_ADD(DATE_FORMAT(CURDATE(), '%Y-%m-01'), INTERVAL 1 MONTH);";
        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@userId", userId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            model.MonthlyRevenue = ReadDecimal(reader, 0);
            model.MonthlyCommission = ReadDecimal(reader, 1);
            model.MonthlyReservationCount = ReadInt(reader, 2);
        }
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
        if (string.IsNullOrWhiteSpace(model.CustomerFullName) || string.IsNullOrWhiteSpace(model.CustomerEmail) || string.IsNullOrWhiteSpace(model.CustomerPhone))
        {
            return (false, "Müşteri adı, e-posta ve telefon zorunludur.", null);
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
        var hotelInfo = await GetHotelSummaryAsync(connection, model.HotelId, cancellationToken);
        var roomName = await GetRoomNameAsync(connection, model.RoomTypeId, model.HotelId, cancellationToken);
        var summary = await BuildPriceSummaryAsync(connection, model.RoomTypeId, model.CheckInDate, model.CheckOutDate, model.RoomCount, cancellationToken);
        var partnerRecipient = await ResolvePartnerRecipientAsync(connection, model.HotelId, cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            var salesCustomerId = await EnsureSalesCustomerAsync(connection, transaction, userId, model, cancellationToken);
            var publicUserId = await EnsurePublicCustomerUserAsync(connection, transaction, model, cancellationToken);
            var reservationNo = await GenerateReservationNoAsync(connection, transaction, cancellationToken);

            const string insertSql = @"
                INSERT INTO rezervasyonlar
                (
                    rezervasyon_no, otel_id, oda_tip_id, kullanici_id, satis_temsilcisi_id, satis_musteri_id,
                    misafir_ad_soyad, misafir_eposta, misafir_telefon, misafir_notu, misafir_sehir, misafir_ilce, misafir_mahalle, misafir_adres,
                    giris_tarihi, cikis_tarihi, yetiskin_sayisi, cocuk_sayisi, oda_sayisi,
                    gecelik_fiyat, toplam_oda_tutari, vergi_tutari, toplam_tutar,
                    komisyon_orani, durum, odeme_durumu, otel_onay_durumu, firma_onay_durumu,
                    kaynak, rezervasyon_kanali, musteri_talep_notu, ozel_istekler
                )
                VALUES
                (
                    @reservationNo, @hotelId, @roomTypeId, @userId, @salesUserId, @salesCustomerId,
                    @fullName, @email, @phone, @note, @city, @district, @neighborhood, @address,
                    @checkIn, @checkOut, @adultCount, @childCount, @roomCount,
                    @nightlyPrice, @roomTotal, @taxAmount, @totalAmount,
                    @commissionRate, 'Onay Bekliyor', 'Beklemede', 'Beklemede', 'Onay Gerekmiyor',
                    'Telefon', 'Satış Paneli', @note, @note
                );
                SELECT LAST_INSERT_ID();";
            long reservationId;
            await using (var command = new MySqlCommand(insertSql, connection, (MySqlTransaction)transaction))
            {
                command.Parameters.AddWithValue("@reservationNo", reservationNo);
                command.Parameters.AddWithValue("@hotelId", model.HotelId);
                command.Parameters.AddWithValue("@roomTypeId", model.RoomTypeId);
                command.Parameters.AddWithValue("@userId", publicUserId);
                command.Parameters.AddWithValue("@salesUserId", userId);
                command.Parameters.AddWithValue("@salesCustomerId", salesCustomerId);
                command.Parameters.AddWithValue("@fullName", model.CustomerFullName.Trim());
                command.Parameters.AddWithValue("@email", model.CustomerEmail.Trim());
                command.Parameters.AddWithValue("@phone", model.CustomerPhone.Trim());
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
            await using (var command = new MySqlCommand(updateCustomerSql, connection, (MySqlTransaction)transaction))
            {
                command.Parameters.AddWithValue("@totalAmount", summary.TotalAmount);
                command.Parameters.AddWithValue("@checkIn", model.CheckInDate.ToDateTime(TimeOnly.MinValue));
                command.Parameters.AddWithValue("@summary", $"{hotelInfo.HotelName} · {model.CheckInDate:dd.MM} - {model.CheckOutDate:dd.MM} · {model.RoomCount} oda");
                command.Parameters.AddWithValue("@customerId", salesCustomerId);
                await command.ExecuteNonQueryAsync(cancellationToken);
            }

            await _emailQueueService.QueueTemplateAsync(connection, (MySqlTransaction)transaction, new QueuedEmailTemplateRequest
            {
                UserId = publicUserId,
                RecipientEmail = model.CustomerEmail.Trim(),
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
            await _emailQueueService.QueueTemplateAsync(connection, (MySqlTransaction)transaction, new QueuedEmailTemplateRequest
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
                    ["guest_email"] = model.CustomerEmail.Trim(),
                    ["guest_phone"] = model.CustomerPhone.Trim(),
                    ["total_price"] = summary.TotalAmount.ToString("N2", CultureInfo.GetCultureInfo("tr-TR")),
                    ["check_in_date"] = model.CheckInDate.ToString("dd MMM yyyy", CultureInfo.GetCultureInfo("tr-TR")),
                    ["check_out_date"] = model.CheckOutDate.ToString("dd MMM yyyy", CultureInfo.GetCultureInfo("tr-TR")),
                    ["room_type_name"] = roomName,
                    ["room_count"] = model.RoomCount.ToString(CultureInfo.InvariantCulture)
                }
            }, cancellationToken);

            await transaction.CommitAsync(cancellationToken);
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
        if (string.IsNullOrWhiteSpace(model.FullName) || string.IsNullOrWhiteSpace(model.Email) || string.IsNullOrWhiteSpace(model.Phone))
        {
            return (false, "Ad soyad, e-posta ve telefon zorunludur.");
        }

        await using var connection = await OpenConnectionAsync(cancellationToken);
        await EnsureSalesUserAsync(connection, userId, cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            var reservationModel = new SalesReservationCreateModel
            {
                CustomerFullName = model.FullName,
                CustomerEmail = model.Email,
                CustomerPhone = model.Phone,
                CustomerCity = model.City,
                DemandNote = model.Note
            };

            var customerId = await EnsureSalesCustomerAsync(connection, transaction, userId, reservationModel, cancellationToken, model.MembershipLevel);
            if (!string.IsNullOrWhiteSpace(model.Note))
            {
                const string noteSql = "INSERT INTO satis_musteri_notlari (satis_musteri_id, sales_user_id, not_basligi, not_icerigi) VALUES (@customerId, @userId, 'Müşteri Notu', @note);";
                await using var noteCommand = new MySqlCommand(noteSql, connection, (MySqlTransaction)transaction);
                noteCommand.Parameters.AddWithValue("@customerId", customerId);
                noteCommand.Parameters.AddWithValue("@userId", userId);
                noteCommand.Parameters.AddWithValue("@note", model.Note);
                await noteCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            await EnsurePublicCustomerUserAsync(connection, transaction, reservationModel, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return (true, "Müşteri kaydı oluşturuldu.");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            return (false, $"Müşteri kaydı sırasında hata oluştu: {ex.Message}");
        }
    }

    private async Task<SalesPanelShellViewModel> BuildShellAsync(MySqlConnection connection, long userId, string activeSectionKey, string title, string subtitle, CancellationToken cancellationToken)
    {
        await EnsureSalesUserAsync(connection, userId, cancellationToken);
        const string sql = @"
            SELECT
                u.ad_soyad, u.eposta, u.rol, COALESCE(u.satis_ekibi,'Satış Ekibi'),
                COALESCE(u.gunluk_satis_hedefi,0), COALESCE(u.aylik_satis_hedefi,0),
                COALESCE((SELECT COUNT(*) FROM rezervasyonlar r WHERE r.satis_temsilcisi_id = u.id AND DATE(r.olusturulma_tarihi) = CURDATE()),0),
                COALESCE((SELECT SUM(r.toplam_tutar) FROM rezervasyonlar r WHERE r.satis_temsilcisi_id = u.id AND DATE(r.olusturulma_tarihi) = CURDATE()),0),
                COALESCE((SELECT COUNT(*) FROM rezervasyonlar r WHERE r.satis_temsilcisi_id = u.id AND YEAR(r.olusturulma_tarihi)=YEAR(CURDATE()) AND MONTH(r.olusturulma_tarihi)=MONTH(CURDATE())),0)
            FROM users u WHERE u.id = @userId LIMIT 1;";
        var shell = new SalesPanelShellViewModel();
        await using (var command = new MySqlCommand(sql, connection))
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

    private async Task EnsureSalesUserAsync(MySqlConnection connection, long userId, CancellationToken cancellationToken)
    {
        const string sql = "SELECT COUNT(*) FROM users WHERE id = @userId AND rol IN ('sales_admin','sales_agent') AND hesap_durumu = 1;";
        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@userId", userId);
        if (Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken), CultureInfo.InvariantCulture) == 0)
        {
            throw new InvalidOperationException("Satış paneli yetkisi bulunamadı.");
        }
    }

    private async Task<int> LoadRankingAsync(MySqlConnection connection, long userId, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT siralama FROM (
                SELECT satis_temsilcisi_id, DENSE_RANK() OVER (ORDER BY COALESCE(SUM(toplam_tutar),0) DESC) AS siralama
                FROM rezervasyonlar
                WHERE satis_temsilcisi_id IS NOT NULL
                  AND olusturulma_tarihi >= DATE_FORMAT(CURDATE(), '%Y-%m-01')
                  AND olusturulma_tarihi < DATE_ADD(DATE_FORMAT(CURDATE(), '%Y-%m-01'), INTERVAL 1 MONTH)
                GROUP BY satis_temsilcisi_id
            ) t WHERE satis_temsilcisi_id = @userId LIMIT 1;";
        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@userId", userId);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is null or DBNull ? 0 : Convert.ToInt32(result, CultureInfo.InvariantCulture);
    }

    private async Task<List<SalesReservationListItemViewModel>> LoadReservationsAsync(MySqlConnection connection, long userId, int limit, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT r.id, r.rezervasyon_no, o.otel_adi, r.misafir_ad_soyad,
                   DATE_FORMAT(r.giris_tarihi, '%d.%m.%Y'), DATE_FORMAT(r.cikis_tarihi, '%d.%m.%Y'),
                   r.gece_sayisi, ot.oda_adi, r.durum, r.otel_onay_durumu, r.toplam_tutar, r.komisyon_tutari
            FROM rezervasyonlar r
            INNER JOIN oteller o ON o.id = r.otel_id
            INNER JOIN oda_tipleri ot ON ot.id = r.oda_tip_id
            WHERE r.satis_temsilcisi_id = @userId
            ORDER BY r.olusturulma_tarihi DESC
            LIMIT @limit;";
        var items = new List<SalesReservationListItemViewModel>();
        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@userId", userId);
        command.Parameters.AddWithValue("@limit", limit);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new SalesReservationListItemViewModel
            {
                ReservationId = reader.GetInt64(0),
                ReservationNo = reader.GetString(1),
                HotelName = reader.GetString(2),
                CustomerName = reader.GetString(3),
                StayText = $"{reader.GetString(4)} - {reader.GetString(5)} · {ReadInt(reader, 6)} gece",
                RoomName = reader.GetString(7),
                StatusText = reader.GetString(8),
                ApprovalText = reader.GetString(9),
                TotalText = FormatMoney(ReadDecimal(reader, 10)),
                CommissionText = FormatMoney(ReadDecimal(reader, 11))
            });
        }
        return items;
    }

    private async Task<List<SalesCustomerCardViewModel>> LoadCustomersAsync(MySqlConnection connection, string? search, int limit, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT id, musteri_kodu, ad_soyad, COALESCE(eposta,''), COALESCE(telefon,''), uyelik_seviyesi,
                   toplam_rezervasyon_sayisi, son_rezervasyon_tarihi, COALESCE(son_talep_ozeti,'')
            FROM satis_musterileri
            WHERE (@search IS NULL OR ad_soyad LIKE CONCAT('%', @search, '%') OR eposta LIKE CONCAT('%', @search, '%') OR telefon LIKE CONCAT('%', @search, '%'))
            ORDER BY guncellenme_tarihi DESC, olusturulma_tarihi DESC
            LIMIT @limit;";
        var items = new List<SalesCustomerCardViewModel>();
        await using var command = new MySqlCommand(sql, connection);
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
                LastRequestSummary = reader.GetString(8)
            });
        }
        return items;
    }

    private async Task<List<SalesSelectOption>> LoadHotelOptionsAsync(MySqlConnection connection, CancellationToken cancellationToken)
        => await LoadOptionsAsync(connection, "SELECT id, otel_adi FROM oteller WHERE yayin_durumu IN ('Yayında','Bakımda') AND onay_durumu = 'Onaylandı' ORDER BY one_cikan_otel DESC, populerlik_sirasi DESC, otel_adi;", null, cancellationToken);

    private async Task<List<SalesSelectOption>> LoadCitiesAsync(MySqlConnection connection, CancellationToken cancellationToken)
        => await LoadOptionsAsync(connection, "SELECT MIN(id), sehir FROM oteller GROUP BY sehir ORDER BY sehir;", null, cancellationToken);

    private async Task<List<SalesSelectOption>> LoadDistrictsAsync(MySqlConnection connection, string? city, CancellationToken cancellationToken)
        => await LoadOptionsAsync(connection, "SELECT MIN(id), ilce FROM oteller WHERE (@city IS NULL OR sehir = @city) GROUP BY ilce ORDER BY ilce;", cmd => cmd.Parameters.AddWithValue("@city", string.IsNullOrWhiteSpace(city) ? DBNull.Value : city.Trim()), cancellationToken);

    private async Task<List<SalesSelectOption>> LoadRoomTypeOptionsAsync(MySqlConnection connection, long hotelId, CancellationToken cancellationToken)
        => await LoadOptionsAsync(connection, "SELECT id, oda_adi FROM oda_tipleri WHERE otel_id = @hotelId AND aktif_mi = 1 ORDER BY standart_gecelik_fiyat ASC, oda_adi;", cmd => cmd.Parameters.AddWithValue("@hotelId", hotelId), cancellationToken);

    private async Task<List<SalesRoomOptionViewModel>> LoadRoomOptionsAsync(MySqlConnection connection, long hotelId, DateOnly checkIn, DateOnly checkOut, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT o.id, o.oda_adi, o.maksimum_kisi_sayisi, o.toplam_oda_sayisi,
                   COALESCE(AVG(COALESCE(ofm.indirimli_fiyat, ofm.gecelik_fiyat)), o.standart_gecelik_fiyat) AS fiyat,
                   COALESCE(MAX(ofm.toplam_oda_sayisi - ofm.satilan_oda_sayisi - ofm.bloke_oda_sayisi), o.toplam_oda_sayisi) AS stok
            FROM oda_tipleri o
            LEFT JOIN oda_fiyat_musaitlik ofm ON ofm.oda_tip_id = o.id AND ofm.tarih >= @checkIn AND ofm.tarih < @checkOut
            WHERE o.otel_id = @hotelId AND o.aktif_mi = 1
            GROUP BY o.id, o.oda_adi, o.maksimum_kisi_sayisi, o.toplam_oda_sayisi, o.standart_gecelik_fiyat
            ORDER BY fiyat ASC;";
        var items = new List<SalesRoomOptionViewModel>();
        await using var command = new MySqlCommand(sql, connection);
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
                FeaturesText = "Hızlı rezervasyon için uygun"
            });
        }
        return items;
    }

    private async Task<SalesReservationPriceSummaryViewModel> BuildPriceSummaryAsync(MySqlConnection connection, long roomTypeId, DateOnly checkIn, DateOnly checkOut, int roomCount, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT COALESCE(AVG(COALESCE(ofm.indirimli_fiyat, ofm.gecelik_fiyat)), ot.standart_gecelik_fiyat)
            FROM oda_tipleri ot
            LEFT JOIN oda_fiyat_musaitlik ofm ON ofm.oda_tip_id = ot.id AND ofm.tarih >= @checkIn AND ofm.tarih < @checkOut
            WHERE ot.id = @roomTypeId;";
        await using var command = new MySqlCommand(sql, connection);
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

    private async Task<List<SalesHotelSearchCardViewModel>> LoadHotelSearchResultsAsync(MySqlConnection connection, string? searchTerm, string? city, string? district, string? neighborhood, decimal? minPrice, decimal? maxPrice, decimal? minimumRating, int? minimumReviewCount, string? feature, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT o.id, o.otel_adi, o.sehir, o.ilce, o.tam_adres, COALESCE(o.rezervasyon_telefonu, o.telefon_1, ''),
                   o.ortalama_puan, o.toplam_yorum_sayisi,
                   COALESCE((SELECT MIN(standart_gecelik_fiyat) FROM oda_tipleri WHERE otel_id = o.id),0),
                   (SELECT COUNT(*) FROM rezervasyonlar r WHERE r.otel_id = o.id AND DATE(r.olusturulma_tarihi) = CURDATE()),
                   COALESCE((SELECT GROUP_CONCAT(oo.ozellik_adi ORDER BY oo.siralama ASC SEPARATOR ', ') FROM otel_ozellik_iliskileri il INNER JOIN otel_ozellikleri oo ON oo.id = il.ozellik_id WHERE il.otel_id = o.id LIMIT 3),'')
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
            ORDER BY o.one_cikan_otel DESC, o.ortalama_puan DESC, o.toplam_yorum_sayisi DESC
            LIMIT 12;";
        var items = new List<SalesHotelSearchCardViewModel>();
        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@searchTerm", string.IsNullOrWhiteSpace(searchTerm) ? DBNull.Value : searchTerm.Trim());
        command.Parameters.AddWithValue("@city", string.IsNullOrWhiteSpace(city) ? DBNull.Value : city.Trim());
        command.Parameters.AddWithValue("@district", string.IsNullOrWhiteSpace(district) ? DBNull.Value : district.Trim());
        command.Parameters.AddWithValue("@neighborhood", string.IsNullOrWhiteSpace(neighborhood) ? DBNull.Value : neighborhood.Trim());
        command.Parameters.AddWithValue("@minimumRating", minimumRating.HasValue ? minimumRating.Value : DBNull.Value);
        command.Parameters.AddWithValue("@minimumReviewCount", minimumReviewCount.HasValue ? minimumReviewCount.Value : DBNull.Value);
        command.Parameters.AddWithValue("@feature", string.IsNullOrWhiteSpace(feature) ? DBNull.Value : feature.Trim());
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
                FeatureBadges = SplitFeatures(reader.GetString(10))
            });
        }
        return items;
    }

    private async Task<List<SalesAvailabilityDayViewModel>> LoadAvailabilityDaysAsync(MySqlConnection connection, long roomTypeId, DateOnly monthStart, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT tarih, COALESCE(indirimli_fiyat, gecelik_fiyat), toplam_oda_sayisi, satilan_oda_sayisi, bloke_oda_sayisi
            FROM oda_fiyat_musaitlik
            WHERE oda_tip_id = @roomTypeId
              AND tarih >= @monthStart
              AND tarih < DATE_ADD(@monthStart, INTERVAL 1 MONTH)
            ORDER BY tarih;";
        var items = new List<SalesAvailabilityDayViewModel>();
        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@roomTypeId", roomTypeId);
        command.Parameters.AddWithValue("@monthStart", monthStart.ToDateTime(TimeOnly.MinValue));
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var total = ReadInt(reader, 2);
            var available = total - ReadInt(reader, 3) - ReadInt(reader, 4);
            items.Add(new SalesAvailabilityDayViewModel
            {
                Date = DateOnly.FromDateTime(reader.GetDateTime(0)),
                IsAvailable = available > 0,
                PriceText = FormatMoney(ReadDecimal(reader, 1))
            });
        }
        return items;
    }

    private async Task<List<SalesHotelGuideItemViewModel>> LoadHotelGuideAsync(MySqlConnection connection, string? search, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT o.id, o.otel_adi, COALESCE(o.rezervasyon_telefonu, o.telefon_1, ''),
                   COALESCE(o.satis_kontak_adi,''), COALESCE(o.satis_kontak_telefonu,''), COALESCE(o.satis_kontak_eposta,''),
                   o.tam_adres, COALESCE(o.satis_notlari,''),
                   (SELECT COUNT(*) FROM rezervasyonlar r WHERE r.otel_id = o.id AND DATE(r.olusturulma_tarihi) = CURDATE())
            FROM oteller o
            WHERE o.yayin_durumu IN ('Yayında','Bakımda')
              AND o.onay_durumu = 'Onaylandı'
              AND (@search IS NULL OR o.otel_adi LIKE CONCAT('%', @search, '%') OR o.sehir LIKE CONCAT('%', @search, '%') OR o.ilce LIKE CONCAT('%', @search, '%'))
            ORDER BY 9 DESC, o.one_cikan_otel DESC, o.otel_adi;";
        var items = new List<SalesHotelGuideItemViewModel>();
        await using var command = new MySqlCommand(sql, connection);
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

    private async Task<long> EnsureSalesCustomerAsync(MySqlConnection connection, MySqlTransaction transaction, long userId, SalesReservationCreateModel model, CancellationToken cancellationToken, string? membershipLevel = null)
    {
        const string findSql = "SELECT id FROM satis_musterileri WHERE (eposta = @email AND @email <> '') OR (telefon = @phone AND @phone <> '') LIMIT 1;";
        await using (var findCommand = new MySqlCommand(findSql, connection, transaction))
        {
            findCommand.Parameters.AddWithValue("@email", model.CustomerEmail.Trim());
            findCommand.Parameters.AddWithValue("@phone", model.CustomerPhone.Trim());
            var existing = await findCommand.ExecuteScalarAsync(cancellationToken);
            if (existing is not null and not DBNull) return Convert.ToInt64(existing, CultureInfo.InvariantCulture);
        }

        await using var seqCommand = new MySqlCommand("SELECT COUNT(*) + 1 FROM satis_musterileri;", connection, transaction);
        var seq = Convert.ToInt32(await seqCommand.ExecuteScalarAsync(cancellationToken), CultureInfo.InvariantCulture);
        var code = $"SATMUST-{seq:0000}";
        const string insertSql = @"
            INSERT INTO satis_musterileri
            (musteri_kodu, ad_soyad, eposta, telefon, sehir, ilce, mahalle, adres, uyelik_seviyesi, son_talep_ozeti, notlar, olusturan_sales_user_id)
            VALUES
            (@code, @fullName, @email, @phone, @city, @district, @neighborhood, @address, @membership, @summary, @notes, @userId);
            SELECT LAST_INSERT_ID();";
        await using var insertCommand = new MySqlCommand(insertSql, connection, transaction);
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

    private async Task<long> EnsurePublicCustomerUserAsync(MySqlConnection connection, MySqlTransaction transaction, SalesReservationCreateModel model, CancellationToken cancellationToken)
    {
        const string findSql = "SELECT id FROM users WHERE eposta = @email OR (telefon = @phone AND @phone <> '') ORDER BY id LIMIT 1;";
        await using (var findCommand = new MySqlCommand(findSql, connection, transaction))
        {
            findCommand.Parameters.AddWithValue("@email", model.CustomerEmail.Trim());
            findCommand.Parameters.AddWithValue("@phone", model.CustomerPhone.Trim());
            var existing = await findCommand.ExecuteScalarAsync(cancellationToken);
            if (existing is not null and not DBNull) return Convert.ToInt64(existing, CultureInfo.InvariantCulture);
        }

        const string insertSql = @"
            INSERT INTO users
            (ad_soyad, eposta, telefon, sehir, ilce, mahalle, adres, sifre, rol, hesap_durumu, dil_tercihi, para_birimi, ulke)
            VALUES
            (@fullName, @email, @phone, @city, @district, @neighborhood, @address, SHA2('1585', 256), 'user', 1, 'tr', 'TRY', 'Türkiye');
            SELECT LAST_INSERT_ID();";
        await using var insertCommand = new MySqlCommand(insertSql, connection, transaction);
        insertCommand.Parameters.AddWithValue("@fullName", model.CustomerFullName.Trim());
        insertCommand.Parameters.AddWithValue("@email", model.CustomerEmail.Trim());
        insertCommand.Parameters.AddWithValue("@phone", model.CustomerPhone.Trim());
        insertCommand.Parameters.AddWithValue("@city", string.IsNullOrWhiteSpace(model.CustomerCity) ? DBNull.Value : model.CustomerCity.Trim());
        insertCommand.Parameters.AddWithValue("@district", string.IsNullOrWhiteSpace(model.CustomerDistrict) ? DBNull.Value : model.CustomerDistrict.Trim());
        insertCommand.Parameters.AddWithValue("@neighborhood", string.IsNullOrWhiteSpace(model.CustomerNeighborhood) ? DBNull.Value : model.CustomerNeighborhood.Trim());
        insertCommand.Parameters.AddWithValue("@address", string.IsNullOrWhiteSpace(model.CustomerAddress) ? DBNull.Value : model.CustomerAddress.Trim());
        return Convert.ToInt64(await insertCommand.ExecuteScalarAsync(cancellationToken), CultureInfo.InvariantCulture);
    }

    private async Task<string> GenerateReservationNoAsync(MySqlConnection connection, MySqlTransaction transaction, CancellationToken cancellationToken)
    {
        await using var command = new MySqlCommand("SELECT COUNT(*) + 1 FROM rezervasyonlar WHERE DATE(olusturulma_tarihi) = CURDATE();", connection, transaction);
        var seq = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken), CultureInfo.InvariantCulture);
        return $"SAT-{DateTime.Now:yyyyMMdd}-{seq:0000}";
    }

    private async Task<(string HotelName, decimal CommissionRate)> GetHotelSummaryAsync(MySqlConnection connection, long hotelId, CancellationToken cancellationToken)
    {
        await using var command = new MySqlCommand("SELECT otel_adi, varsayilan_komisyon_orani FROM oteller WHERE id = @hotelId LIMIT 1;", connection);
        command.Parameters.AddWithValue("@hotelId", hotelId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) throw new InvalidOperationException("Otel bulunamadı.");
        return (reader.GetString(0), ReadDecimal(reader, 1));
    }

    private async Task<string> GetRoomNameAsync(MySqlConnection connection, long roomTypeId, long hotelId, CancellationToken cancellationToken)
    {
        await using var command = new MySqlCommand("SELECT oda_adi FROM oda_tipleri WHERE id = @roomTypeId AND otel_id = @hotelId LIMIT 1;", connection);
        command.Parameters.AddWithValue("@roomTypeId", roomTypeId);
        command.Parameters.AddWithValue("@hotelId", hotelId);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        if (result is null or DBNull) throw new InvalidOperationException("Oda tipi bulunamadı.");
        return Convert.ToString(result, CultureInfo.InvariantCulture) ?? "Oda";
    }

    private async Task<(long UserId, string Email)> ResolvePartnerRecipientAsync(MySqlConnection connection, long hotelId, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT COALESCE(o.user_id, oks.user_id, 1), COALESCE(o.satis_kontak_eposta, u.eposta, o.eposta, 'partner@otelturizm.com')
            FROM oteller o
            LEFT JOIN otel_kullanici_sahiplikleri oks ON oks.otel_id = o.id AND oks.aktif_mi = 1
            LEFT JOIN users u ON u.id = COALESCE(o.user_id, oks.user_id)
            WHERE o.id = @hotelId
            ORDER BY oks.ana_sorumlu_mu DESC, oks.id ASC
            LIMIT 1;";
        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@hotelId", hotelId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken)) return (reader.IsDBNull(0) ? 1L : reader.GetInt64(0), reader.GetString(1));
        return (1, "partner@otelturizm.com");
    }

    private static async Task<List<SalesSelectOption>> LoadOptionsAsync(MySqlConnection connection, string sql, Action<MySqlCommand>? configure, CancellationToken cancellationToken)
    {
        var items = new List<SalesSelectOption>();
        await using var command = new MySqlCommand(sql, connection);
        configure?.Invoke(command);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken)) items.Add(new SalesSelectOption { Value = reader.GetInt64(0), Label = reader.GetString(1) });
        return items;
    }

    private static decimal ReadDecimal(MySqlDataReader reader, int index) => reader.IsDBNull(index) ? 0m : Convert.ToDecimal(reader.GetValue(index), CultureInfo.InvariantCulture);
    private static int ReadInt(MySqlDataReader reader, int index) => reader.IsDBNull(index) ? 0 : Convert.ToInt32(reader.GetValue(index), CultureInfo.InvariantCulture);
    private static string FormatMoney(decimal value) => value.ToString("'₺'#,##0.##", CultureInfo.GetCultureInfo("tr-TR"));
    private static List<string> SplitFeatures(string raw) => string.IsNullOrWhiteSpace(raw) ? new List<string>() : raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Take(3).ToList();
    private static string SplitFirstName(string fullName) => string.IsNullOrWhiteSpace(fullName) ? "Misafir" : fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "Misafir";

    private async Task<MySqlConnection> OpenConnectionAsync(CancellationToken cancellationToken)
    {
        var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }
}


