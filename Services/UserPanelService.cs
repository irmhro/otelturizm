using System.Globalization;
using Microsoft.Data.SqlClient;
using SqlConnection = Microsoft.Data.SqlClient.SqlConnection;
using SqlCommand = Microsoft.Data.SqlClient.SqlCommand;
using SqlTransaction = Microsoft.Data.SqlClient.SqlTransaction;
using SqlException = Microsoft.Data.SqlClient.SqlException;
using otelturizmnew.Models.Email;
using otelturizmnew.Models.Messages;
using otelturizmnew.Models.Paneller.User;
using otelturizmnew.Services.Abstractions;

namespace otelturizmnew.Services;

public class UserPanelService : IUserPanelService
{
    private readonly string _connectionString;
    private readonly IMessageCenterService _messageCenterService;
    private readonly IAddressLookupService _addressLookupService;
    private readonly IEmailQueueService _emailQueueService;
    private readonly IHotelPricingReadService _hotelPricingReadService;

    public UserPanelService(
        IConfiguration configuration,
        IMessageCenterService messageCenterService,
        IAddressLookupService addressLookupService,
        IEmailQueueService emailQueueService,
        IHotelPricingReadService hotelPricingReadService)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection tanimli degil.");
        _messageCenterService = messageCenterService;
        _addressLookupService = addressLookupService;
        _emailQueueService = emailQueueService;
        _hotelPricingReadService = hotelPricingReadService;
    }

    public async Task<UserDashboardPageViewModel> GetDashboardAsync(
        long userId,
        string? reservationStatus = null,
        DateOnly? reservationStartDate = null,
        DateOnly? reservationEndDate = null,
        int reservationPage = 1,
        int reservationPageSize = 5,
        CancellationToken cancellationToken = default)
    {
        var model = new UserDashboardPageViewModel();
        await using var connection = await OpenConnectionAsync(cancellationToken);
        var normalizedStatus = NormalizeDashboardReservationStatus(reservationStatus);
        var normalizedPageSize = reservationPageSize is 5 or 10 or 15 or 20 ? reservationPageSize : 5;
        var normalizedPage = reservationPage <= 0 ? 1 : reservationPage;
        var startDate = reservationStartDate;
        var endDate = reservationEndDate;
        if (startDate.HasValue && endDate.HasValue && endDate.Value < startDate.Value)
        {
            (startDate, endDate) = (endDate, startDate);
        }

        const string summarySql = @"
            SELECT
                (SELECT COUNT(*) FROM rezervasyonlar WHERE kullanici_id = @userId) AS total_count,
                (SELECT COUNT(*) FROM rezervasyonlar WHERE kullanici_id = @userId AND durum <> 'İptal Edildi' AND cikis_tarihi >= CAST(GETDATE() AS date)) AS upcoming_count,
                (SELECT COUNT(*) FROM user_favori_oteller WHERE user_id = @userId AND aktif_mi = 1) AS favorite_count,
                (SELECT COUNT(*) FROM mesaj_konusmalari WHERE misafir_kullanici_id = @userId AND durum <> 'Arşivlendi') AS message_count,
                (SELECT COALESCE(SUM(toplam_tasarruf), 0) FROM rezervasyonlar WHERE kullanici_id = @userId) AS total_discount;";

        await using (var command = new SqlCommand(summarySql, connection))
        {
            command.Parameters.AddWithValue("@userId", userId);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                model.TotalReservationCount = SafeInt(reader, 0);
                model.UpcomingReservationCount = SafeInt(reader, 1);
                model.FavoriteCount = SafeInt(reader, 2);
                model.MessageCount = SafeInt(reader, 3);
                model.DiscountText = FormatMoney(SafeDecimal(reader, 4));
            }
        }

        model.ReservationStatusFilter = normalizedStatus;
        model.ReservationStartDateText = startDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        model.ReservationEndDateText = endDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        model.ReservationPageSize = normalizedPageSize;
        model.ReservationTotalCount = await CountDashboardReservationsAsync(connection, userId, normalizedStatus, startDate, endDate, cancellationToken);
        model.ReservationPage = Math.Min(normalizedPage, Math.Max(1, model.ReservationTotalPages));
        model.RecentReservations = await LoadDashboardReservationsAsync(
            connection,
            userId,
            normalizedStatus,
            startDate,
            endDate,
            model.ReservationPage,
            model.ReservationPageSize,
            cancellationToken);
        model.FavoriteHotels = await LoadFavoriteSummariesAsync(connection, userId, 3, cancellationToken);
        return model;
    }

    public async Task<UserReservationsPageViewModel> GetReservationsAsync(
        long userId,
        string? statusFilter = null,
        DateOnly? startDate = null,
        DateOnly? endDate = null,
        int page = 1,
        int pageSize = 5,
        CancellationToken cancellationToken = default)
    {
        var model = new UserReservationsPageViewModel();
        await using var connection = await OpenConnectionAsync(cancellationToken);
        var allReservations = await LoadReservationsAsync(connection, userId, 500, cancellationToken);
        model.TotalCount = allReservations.Count;
        model.UpcomingCount = allReservations.Count(x => x.IsUpcoming && !x.IsCancelled);
        model.PastCount = allReservations.Count(x => !x.IsUpcoming && !x.IsCancelled);
        model.CancelledCount = allReservations.Count(x => x.IsCancelled);

        var normalizedStatus = NormalizeReservationStatusFilter(statusFilter);
        var safePageSize = pageSize is 5 or 10 or 15 ? pageSize : 5;
        var safePage = page <= 0 ? 1 : page;
        if (startDate.HasValue && endDate.HasValue && endDate.Value < startDate.Value)
        {
            (startDate, endDate) = (endDate, startDate);
        }

        IEnumerable<UserReservationCardViewModel> filtered = allReservations;
        filtered = normalizedStatus switch
        {
            "upcoming" => filtered.Where(x => x.IsUpcoming && !x.IsCancelled),
            "past" => filtered.Where(x => !x.IsUpcoming && !x.IsCancelled),
            "cancelled" => filtered.Where(x => x.IsCancelled),
            _ => filtered
        };

        if (startDate.HasValue)
        {
            filtered = filtered.Where(x => x.CheckInDate >= startDate.Value);
        }
        if (endDate.HasValue)
        {
            filtered = filtered.Where(x => x.CheckInDate <= endDate.Value);
        }

        var filteredList = filtered.ToList();
        model.StatusFilter = normalizedStatus;
        model.StartDateText = startDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        model.EndDateText = endDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        model.PageSize = safePageSize;
        model.FilteredCount = filteredList.Count;
        model.Page = Math.Min(safePage, Math.Max(1, model.TotalPages));
        model.Reservations = filteredList
            .Skip((model.Page - 1) * model.PageSize)
            .Take(model.PageSize)
            .ToList();
        return model;
    }

    public async Task<(bool Success, string Message)> CancelReservationAsync(long userId, long reservationId, string cancellationReason, CancellationToken cancellationToken = default)
    {
        if (reservationId <= 0)
        {
            return (false, "Gecerli bir rezervasyon seciniz.");
        }

        var reason = (cancellationReason ?? string.Empty).Trim();
        if (reason.Length < 10)
        {
            return (false, "Iptal nedeni zorunludur ve en az 10 karakter olmalidir.");
        }

        await using var connection = await OpenConnectionAsync(cancellationToken);
        const string selectSql = @"
            SELECT TOP (1) durum, giris_tarihi
            FROM rezervasyonlar
            WHERE id = @reservationId AND kullanici_id = @userId;";

        string? currentStatus = null;
        DateTime? checkInDate = null;
        await using (var selectCommand = new SqlCommand(selectSql, connection))
        {
            selectCommand.Parameters.AddWithValue("@reservationId", reservationId);
            selectCommand.Parameters.AddWithValue("@userId", userId);
            await using var reader = await selectCommand.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                currentStatus = reader.GetString(0);
                checkInDate = reader.GetDateTime(1);
            }
        }

        if (string.IsNullOrWhiteSpace(currentStatus) || !checkInDate.HasValue)
        {
            return (false, "Rezervasyon bulunamadi.");
        }

        if (string.Equals(currentStatus, "İptal Edildi", StringComparison.OrdinalIgnoreCase))
        {
            return (false, "Bu rezervasyon zaten iptal edildi.");
        }

        if (checkInDate.Value.Date <= DateTime.Today)
        {
            return (false, "Check-in tarihi gelen veya gecen rezervasyonlar panelden iptal edilemez. Sadece otel rezervasyonunuzu iptal edebilir, firma ile iletisim icin Mesajlarim alanina geciniz.");
        }

        const string updateSql = @"
            UPDATE rezervasyonlar
            SET durum = 'İptal Edildi',
                otel_onay_durumu = 'Reddedildi',
                iptal_nedeni = @reason,
                iptal_eden = 'Misafir',
                iptal_tarihi = CURRENT_TIMESTAMP
            WHERE id = @reservationId AND kullanici_id = @userId;";
        await using var updateCommand = new SqlCommand(updateSql, connection);
        updateCommand.Parameters.AddWithValue("@reservationId", reservationId);
        updateCommand.Parameters.AddWithValue("@userId", userId);
        updateCommand.Parameters.AddWithValue("@reason", reason);
        var affected = await updateCommand.ExecuteNonQueryAsync(cancellationToken);
        if (affected <= 0)
        {
            return (false, "Rezervasyon iptal edilemedi.");
        }

        var snapshot = await LoadReservationCancellationSnapshotAsync(connection, userId, reservationId, cancellationToken);
        if (snapshot is not null)
        {
            var partner = await ResolvePartnerRecipientAsync(connection, snapshot.HotelId, cancellationToken);
            if (!string.IsNullOrWhiteSpace(partner.Email))
            {
                await _emailQueueService.QueueTemplateAsync(connection, null, new QueuedEmailTemplateRequest
                {
                    UserId = partner.UserId,
                    RecipientEmail = partner.Email,
                    TemplateCode = "reservation_cancelled_partner",
                    RelatedTable = "rezervasyonlar",
                    RelatedRecordId = reservationId,
                    Tokens = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["hotel_manager_name"] = partner.ManagerName,
                        ["hotel_name"] = snapshot.HotelName,
                        ["booking_reference"] = snapshot.ReservationNo,
                        ["guest_full_name"] = snapshot.GuestName,
                        ["check_in_date"] = snapshot.CheckIn.ToString("dd MMM yyyy", CultureInfo.GetCultureInfo("tr-TR")),
                        ["check_out_date"] = snapshot.CheckOut.ToString("dd MMM yyyy", CultureInfo.GetCultureInfo("tr-TR")),
                        ["room_type_name"] = snapshot.RoomName,
                        ["total_price"] = snapshot.TotalAmount.ToString("N2", CultureInfo.GetCultureInfo("tr-TR")),
                        ["cancel_reason"] = reason
                    }
                }, cancellationToken);
            }
        }

        return (true, "Rezervasyonunuz iptal edildi.");
    }

    public async Task<UserMessagesPageViewModel> GetMessagesAsync(long userId, long? conversationId, CancellationToken cancellationToken = default)
    {
        var inbox = await _messageCenterService.GetUserInboxAsync(userId, conversationId, cancellationToken);
        return new UserMessagesPageViewModel
        {
            Threads = inbox.Threads,
            SelectedConversationId = inbox.SelectedConversationId,
            SelectedTitle = inbox.SelectedTitle,
            SelectedSubtitle = inbox.SelectedSubtitle,
            Messages = inbox.Messages
        };
    }

    public Task<(bool Success, string Message)> SendMessageAsync(long userId, MessageSendRequest form, IReadOnlyList<IFormFile>? attachments, HttpContext httpContext, CancellationToken cancellationToken = default)
        => _messageCenterService.SendFromUserAsync(userId, form, attachments, httpContext, cancellationToken);

    public Task<(bool Success, string Message)> DeleteMessageAsync(long userId, MessageDeleteRequest form, CancellationToken cancellationToken = default)
        => Task.FromResult((false, "Mesaj silme islemi kullanici panelinde devre disi."));

    public async Task<UserProfilePageViewModel> GetProfileAsync(long userId, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        const string sql = @"
            SELECT TOP (1) ad_soyad, eposta, COALESCE(telefon, ''), tc_kimlik_no, dogum_tarihi, cinsiyet, uyruk, adres, sehir, ilce, mahalle, posta_kodu,
                   tercih_edilen_oda_tipi, yatak_tercihi, konusulan_diller, seyahat_amaci, ozel_istekler
            FROM users
            WHERE id = @userId;";

        var model = new UserProfilePageViewModel();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@userId", userId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            var name = SplitName(reader.GetString(0));
            model.Form = new UserProfileForm
            {
                FirstName = name.FirstName,
                LastName = name.LastName,
                Email = reader.GetString(1),
                Phone = reader.GetString(2),
                IdentityNumber = reader.IsDBNull(3) ? null : reader.GetString(3),
                BirthDateText = reader.IsDBNull(4) ? null : reader.GetDateTime(4).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                Gender = reader.IsDBNull(5) ? null : reader.GetString(5),
                Nationality = reader.IsDBNull(6) ? null : reader.GetString(6),
                Address = reader.IsDBNull(7) ? null : reader.GetString(7),
                City = reader.IsDBNull(8) ? null : reader.GetString(8),
                District = reader.IsDBNull(9) ? null : reader.GetString(9),
                Neighborhood = reader.IsDBNull(10) ? null : reader.GetString(10),
                PostalCode = reader.IsDBNull(11) ? null : reader.GetString(11),
                RoomPreference = reader.IsDBNull(12) ? null : reader.GetString(12),
                BedPreference = reader.IsDBNull(13) ? null : reader.GetString(13),
                SpokenLanguages = reader.IsDBNull(14) ? null : reader.GetString(14),
                TravelPurpose = reader.IsDBNull(15) ? null : reader.GetString(15),
                SpecialRequests = reader.IsDBNull(16) ? null : reader.GetString(16)
            };
        }

        model.Countries = (await _addressLookupService.GetCountriesAsync(cancellationToken)).ToList();
        model.Provinces = (await _addressLookupService.GetProvincesAsync(cancellationToken)).ToList();
        var selection = await _addressLookupService.ResolveSelectionAsync(model.Form.City, model.Form.District, model.Form.Neighborhood, model.Form.Nationality, cancellationToken);
        if (selection is not null)
        {
            model.SelectedCountryId = selection.CountryId;
            model.SelectedProvinceId = selection.ProvinceId;
            model.SelectedDistrictId = selection.DistrictId;
            model.SelectedNeighborhoodId = selection.NeighborhoodId;
        }

        return model;
    }

    public async Task<bool> SaveProfileAsync(long userId, UserProfileForm form, CancellationToken cancellationToken = default)
    {
        var email = form.Email?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(email))
        {
            return false;
        }

        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using (var duplicateCheckCommand = new SqlCommand("SELECT COUNT(*) FROM users WHERE eposta = @email AND id <> @userId;", connection))
        {
            duplicateCheckCommand.Parameters.AddWithValue("@email", email);
            duplicateCheckCommand.Parameters.AddWithValue("@userId", userId);
            var duplicateCount = Convert.ToInt32(await duplicateCheckCommand.ExecuteScalarAsync(cancellationToken) ?? 0, CultureInfo.InvariantCulture);
            if (duplicateCount > 0)
            {
                return false;
            }
        }

        try
        {
            await using var command = new SqlCommand(@"
                UPDATE users
                SET ad_soyad = @fullName,
                    eposta = @email,
                    telefon = NULLIF(@phone, ''),
                    tc_kimlik_no = NULLIF(@identityNumber, ''),
                    dogum_tarihi = @birthDate,
                    cinsiyet = NULLIF(@gender, ''),
                    uyruk = NULLIF(@nationality, ''),
                    adres = NULLIF(@address, ''),
                    sehir = NULLIF(@city, ''),
                    ilce = NULLIF(@district, ''),
                    mahalle = NULLIF(@neighborhood, ''),
                    posta_kodu = NULLIF(@postalCode, ''),
                    tercih_edilen_oda_tipi = NULLIF(@roomPreference, ''),
                    yatak_tercihi = NULLIF(@bedPreference, ''),
                    konusulan_diller = NULLIF(@spokenLanguages, ''),
                    seyahat_amaci = NULLIF(@travelPurpose, ''),
                    ozel_istekler = NULLIF(@specialRequests, ''),
                    profil_tamamlanma_tarihi = CURRENT_TIMESTAMP
                WHERE id = @userId;", connection);
            command.Parameters.AddWithValue("@fullName", $"{form.FirstName} {form.LastName}".Trim());
            command.Parameters.AddWithValue("@email", email);
            command.Parameters.AddWithValue("@phone", form.Phone?.Trim() ?? string.Empty);
            command.Parameters.AddWithValue("@identityNumber", form.IdentityNumber?.Trim() ?? string.Empty);
            command.Parameters.AddWithValue("@birthDate", DateTime.TryParse(form.BirthDateText, out var birthDate) ? birthDate : DBNull.Value);
            command.Parameters.AddWithValue("@gender", form.Gender?.Trim() ?? string.Empty);
            command.Parameters.AddWithValue("@nationality", form.Nationality?.Trim() ?? string.Empty);
            command.Parameters.AddWithValue("@address", form.Address?.Trim() ?? string.Empty);
            command.Parameters.AddWithValue("@city", form.City?.Trim() ?? string.Empty);
            command.Parameters.AddWithValue("@district", form.District?.Trim() ?? string.Empty);
            command.Parameters.AddWithValue("@neighborhood", form.Neighborhood?.Trim() ?? string.Empty);
            command.Parameters.AddWithValue("@postalCode", form.PostalCode?.Trim() ?? string.Empty);
            command.Parameters.AddWithValue("@roomPreference", form.RoomPreference?.Trim() ?? string.Empty);
            command.Parameters.AddWithValue("@bedPreference", form.BedPreference?.Trim() ?? string.Empty);
            command.Parameters.AddWithValue("@spokenLanguages", form.SpokenLanguages?.Trim() ?? string.Empty);
            command.Parameters.AddWithValue("@travelPurpose", form.TravelPurpose?.Trim() ?? string.Empty);
            command.Parameters.AddWithValue("@specialRequests", form.SpecialRequests?.Trim() ?? string.Empty);
            command.Parameters.AddWithValue("@userId", userId);
            return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
        }
        catch (SqlException)
        {
            return false;
        }
    }

    public async Task<UserNotificationsPageViewModel> GetNotificationsAsync(long userId, CancellationToken cancellationToken = default)
    {
        var model = new UserNotificationsPageViewModel();
        await using var connection = await OpenConnectionAsync(cancellationToken);

        await using (var command = new SqlCommand(@"
            SELECT rezervasyon_eposta, rezervasyon_push, checkin_hatirlatma, iptal_degisim, kampanya_eposta, kampanya_sms, sistem_bildirimi
            FROM kullanici_bildirim_tercihleri WHERE kullanici_id = @userId;", connection))
        {
            command.Parameters.AddWithValue("@userId", userId);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                model.Form = new UserNotificationPreferencesForm
                {
                    ReservationEmail = SafeBool(reader, 0),
                    ReservationPush = SafeBool(reader, 1),
                    CheckInReminder = SafeBool(reader, 2),
                    CancellationChanges = SafeBool(reader, 3),
                    CampaignEmail = SafeBool(reader, 4),
                    CampaignSms = SafeBool(reader, 5),
                    SystemNotifications = SafeBool(reader, 6)
                };
            }
        }

        await using (var command = new SqlCommand(@"
            SELECT TOP (6) baslik, mesaj, bildirim_turu, olusturulma_tarihi
            FROM sistem_ici_bildirimler
            WHERE kullanici_id = @userId AND arsivlendi_mi = 0
            ORDER BY olusturulma_tarihi DESC, id DESC;", connection))
        {
            command.Parameters.AddWithValue("@userId", userId);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                model.RecentNotifications.Add(new UserNotificationItemViewModel
                {
                    Title = reader.GetString(0),
                    Message = reader.GetString(1),
                    TypeText = reader.GetString(2),
                    TimeText = reader.GetDateTime(3).ToLocalTime().ToString("dd MMM HH:mm", CultureInfo.GetCultureInfo("tr-TR"))
                });
            }
        }

        return model;
    }

    public async Task<bool> SaveNotificationsAsync(long userId, UserNotificationPreferencesForm form, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = new SqlCommand(@"
            IF EXISTS (SELECT 1 FROM kullanici_bildirim_tercihleri WHERE kullanici_id = @userId)
            BEGIN
                UPDATE kullanici_bildirim_tercihleri
                SET rezervasyon_eposta = @reservationEmail,
                    rezervasyon_push = @reservationPush,
                    checkin_hatirlatma = @checkInReminder,
                    iptal_degisim = @cancellationChanges,
                    kampanya_eposta = @campaignEmail,
                    kampanya_sms = @campaignSms,
                    sistem_bildirimi = @systemNotifications,
                    guncellenme_tarihi = CURRENT_TIMESTAMP
                WHERE kullanici_id = @userId;
            END
            ELSE
            BEGIN
                INSERT INTO kullanici_bildirim_tercihleri
                (kullanici_id, rezervasyon_eposta, rezervasyon_push, checkin_hatirlatma, iptal_degisim, kampanya_eposta, kampanya_sms, sistem_bildirimi, guncellenme_tarihi)
                VALUES
                (@userId, @reservationEmail, @reservationPush, @checkInReminder, @cancellationChanges, @campaignEmail, @campaignSms, @systemNotifications, CURRENT_TIMESTAMP);
            END;", connection);
        command.Parameters.AddWithValue("@userId", userId);
        command.Parameters.AddWithValue("@reservationEmail", form.ReservationEmail ? 1 : 0);
        command.Parameters.AddWithValue("@reservationPush", form.ReservationPush ? 1 : 0);
        command.Parameters.AddWithValue("@checkInReminder", form.CheckInReminder ? 1 : 0);
        command.Parameters.AddWithValue("@cancellationChanges", form.CancellationChanges ? 1 : 0);
        command.Parameters.AddWithValue("@campaignEmail", form.CampaignEmail ? 1 : 0);
        command.Parameters.AddWithValue("@campaignSms", form.CampaignSms ? 1 : 0);
        command.Parameters.AddWithValue("@systemNotifications", form.SystemNotifications ? 1 : 0);
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    public async Task<UserSecurityPageViewModel> GetSecurityAsync(long userId, CancellationToken cancellationToken = default)
    {
        var model = new UserSecurityPageViewModel();
        await using var connection = await OpenConnectionAsync(cancellationToken);

        await using (var command = new SqlCommand("SELECT TOP (1) iki_asamali_dogrulama_aktif_mi FROM users WHERE id = @userId;", connection))
        {
            command.Parameters.AddWithValue("@userId", userId);
            var scalar = await command.ExecuteScalarAsync(cancellationToken);
            model.TwoFactorEnabled = scalar is not null && scalar != DBNull.Value && Convert.ToInt32(scalar, CultureInfo.InvariantCulture) == 1;
        }

        await using var sessionCommand = new SqlCommand(@"
            SELECT TOP (8) COALESCE(cihaz_etiketi, 'Bilinmeyen cihaz'), beni_hatirla_tercihi, toplam_oturum_suresi_saniye, son_aktivite_tarihi
            FROM kullanici_oturum_istatistikleri
            WHERE kullanici_id = @userId AND hesap_tipi = 'user'
            ORDER BY son_aktivite_tarihi DESC, id DESC;", connection);
        sessionCommand.Parameters.AddWithValue("@userId", userId);
        await using var reader = await sessionCommand.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var duration = reader.IsDBNull(2) ? 0L : reader.GetInt64(2);
            var lastActive = reader.IsDBNull(3) ? (DateTime?)null : reader.GetDateTime(3).ToLocalTime();
            model.Sessions.Add(new UserSessionRowViewModel
            {
                DeviceLabel = reader.GetString(0),
                RememberText = SafeBool(reader, 1) ? "Beni hatırla açık" : "Standart oturum",
                ActivityText = lastActive.HasValue ? $"{lastActive.Value:dd.MM.yyyy HH:mm} · {Math.Max(1, duration / 60)} dk toplam" : "Henüz aktivite yok"
            });
        }

        return model;
    }

    public async Task<(bool Success, string Message)> ChangePasswordAsync(long userId, UserChangePasswordForm form, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(form.CurrentPassword) || string.IsNullOrWhiteSpace(form.NewPassword))
        {
            return (false, "Mevcut şifre ve yeni şifre zorunludur.");
        }
        if (!string.Equals(form.NewPassword, form.ConfirmPassword, StringComparison.Ordinal))
        {
            return (false, "Yeni şifre tekrarı eşleşmiyor.");
        }
        if (!IsPasswordPolicyValid(form.NewPassword))
        {
            return (false, "Yeni sifre en az 6 karakter olmali ve en az 1 harf ile 1 rakam icermelidir.");
        }

        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = new SqlCommand(@"
            UPDATE users
            SET sifre = CONVERT(varchar(64), HASHBYTES('SHA2_256', @newPassword), 2)
            WHERE id = @userId
              AND sifre = CONVERT(varchar(64), HASHBYTES('SHA2_256', @currentPassword), 2);", connection);
        command.Parameters.AddWithValue("@newPassword", form.NewPassword);
        command.Parameters.AddWithValue("@currentPassword", form.CurrentPassword);
        command.Parameters.AddWithValue("@userId", userId);
        var affected = await command.ExecuteNonQueryAsync(cancellationToken);
        return affected > 0 ? (true, "Şifren güncellendi.") : (false, "Mevcut şifre doğrulanamadı.");
    }

    private static bool IsPasswordPolicyValid(string? password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
        {
            return false;
        }

        var hasLetter = password.Any(char.IsLetter);
        var hasDigit = password.Any(char.IsDigit);
        return hasLetter && hasDigit;
    }

    public async Task<bool> SaveTwoFactorAsync(long userId, UserTwoFactorForm form, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = new SqlCommand("UPDATE users SET iki_asamali_dogrulama_aktif_mi = @enabled WHERE id = @userId;", connection);
        command.Parameters.AddWithValue("@enabled", form.Enabled ? 1 : 0);
        command.Parameters.AddWithValue("@userId", userId);
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    public async Task<UserPaymentMethodsPageViewModel> GetPaymentMethodsAsync(long userId, CancellationToken cancellationToken = default)
    {
        var model = new UserPaymentMethodsPageViewModel();
        await using var connection = await OpenConnectionAsync(cancellationToken);

        await using (var command = new SqlCommand(@"
            SELECT id, kart_etiketi, marka, son_dort_hane, son_kullanim_ay, son_kullanim_yil, varsayilan_mi
            FROM kullanici_odeme_yontemleri
            WHERE kullanici_id = @userId AND aktif_mi = 1
            ORDER BY varsayilan_mi DESC, id DESC;", connection))
        {
            command.Parameters.AddWithValue("@userId", userId);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                model.Methods.Add(new UserPaymentMethodRowViewModel
                {
                    PaymentMethodId = reader.GetInt64(0),
                    Label = $"{reader.GetString(1)} · {reader.GetString(2)} •••• {reader.GetString(3)}",
                    DetailText = $"Son kullanim {SafeInt(reader, 4):00}/{SafeInt(reader, 5)}" + (SafeBool(reader, 6) ? " · Varsayilan kart" : string.Empty),
                    IsDefault = SafeBool(reader, 6)
                });
            }
        }

        await using var billingCommand = new SqlCommand(@"
            SELECT TOP (1) ad_soyad,
                   LTRIM(RTRIM(CONCAT(
                       COALESCE(NULLIF(adres, ''), ''),
                       CASE WHEN COALESCE(NULLIF(ilce, ''), '') <> '' AND COALESCE(NULLIF(adres, ''), '') <> '' THEN ', ' ELSE '' END,
                       COALESCE(NULLIF(ilce, ''), ''),
                       CASE WHEN COALESCE(NULLIF(sehir, ''), '') <> '' AND (COALESCE(NULLIF(adres, ''), '') <> '' OR COALESCE(NULLIF(ilce, ''), '') <> '') THEN ', ' ELSE '' END,
                       COALESCE(NULLIF(sehir, ''), '')
                   ))) AS full_address,
                   eposta
            FROM users
            WHERE id = @userId;", connection);
        billingCommand.Parameters.AddWithValue("@userId", userId);
        await using var billingReader = await billingCommand.ExecuteReaderAsync(cancellationToken);
        if (await billingReader.ReadAsync(cancellationToken))
        {
            model.Billing = new UserBillingSummaryViewModel
            {
                InvoiceName = billingReader.GetString(0),
                Address = billingReader.IsDBNull(1) ? "Adres bilgisi eklenmedi" : billingReader.GetString(1),
                Email = billingReader.GetString(2)
            };
        }

        return model;
    }

    public async Task<bool> SavePaymentMethodAsync(long userId, UserPaymentMethodForm form, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(form.CardLabel) || string.IsNullOrWhiteSpace(form.CardHolder) || string.IsNullOrWhiteSpace(form.LastFourDigits))
        {
            return false;
        }

        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        if (form.SetAsDefault)
        {
            await using var clear = new SqlCommand("UPDATE kullanici_odeme_yontemleri SET varsayilan_mi = 0 WHERE kullanici_id = @userId;", connection, (SqlTransaction)transaction);
            clear.Parameters.AddWithValue("@userId", userId);
            await clear.ExecuteNonQueryAsync(cancellationToken);
        }

        await using var command = new SqlCommand(@"
            INSERT INTO kullanici_odeme_yontemleri
            (kullanici_id, kart_etiketi, kart_sahibi, marka, son_dort_hane, son_kullanim_ay, son_kullanim_yil, varsayilan_mi, aktif_mi)
            VALUES
            (@userId, @label, @holder, @brand, @lastFour, @month, @year, @isDefault, 1);", connection, (SqlTransaction)transaction);
        command.Parameters.AddWithValue("@userId", userId);
        command.Parameters.AddWithValue("@label", form.CardLabel.Trim());
        command.Parameters.AddWithValue("@holder", form.CardHolder.Trim());
        command.Parameters.AddWithValue("@brand", form.Brand.Trim());
        command.Parameters.AddWithValue("@lastFour", form.LastFourDigits.Trim());
        command.Parameters.AddWithValue("@month", form.ExpiryMonth);
        command.Parameters.AddWithValue("@year", form.ExpiryYear);
        command.Parameters.AddWithValue("@isDefault", form.SetAsDefault ? 1 : 0);
        var affected = await command.ExecuteNonQueryAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return affected > 0;
    }

    public async Task<bool> DeletePaymentMethodAsync(long userId, long paymentMethodId, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = new SqlCommand("UPDATE kullanici_odeme_yontemleri SET aktif_mi = 0, varsayilan_mi = 0 WHERE id = @paymentMethodId AND kullanici_id = @userId;", connection);
        command.Parameters.AddWithValue("@paymentMethodId", paymentMethodId);
        command.Parameters.AddWithValue("@userId", userId);
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    public async Task<UserLoyaltyPageViewModel> GetLoyaltyAsync(long userId, CancellationToken cancellationToken = default)
    {
        var model = new UserLoyaltyPageViewModel();
        await using var connection = await OpenConnectionAsync(cancellationToken);

        await EnsureLoyaltyAccountAsync(connection, userId, cancellationToken);

        var alertHotelIds = new List<long>();

        await using (var summaryCommand = new SqlCommand(@"
            SELECT
                COALESCE(u.ad_soyad, 'Misafir') AS ad_soyad,
                COALESCE(h.toplam_puan, 0) AS toplam_puan,
                COALESCE(h.kullanilabilir_puan, 0) AS kullanilabilir_puan,
                COALESCE(h.bu_yil_kazanilan_puan, 0) AS bu_yil_kazanilan_puan,
                COALESCE(h.bu_yil_kullanilan_puan, 0) AS bu_yil_kullanilan_puan,
                COALESCE(h.puan_gecerlilik_tarihi, DATEADD(DAY, 365, CAST(GETDATE() AS date))) AS puan_gecerlilik_tarihi,
                COALESCE(ct.ad, 'Bronz') AS mevcut_seviye_adi,
                COALESCE(ct.kod, 'B') AS mevcut_seviye_kodu,
                COALESCE(ct.avantajlar_metin, 'Yuzde 5 indirim|Hos geldin puani') AS avantajlar_metin,
                COALESCE(nt.ad, '') AS sonraki_seviye_adi,
                CASE
                    WHEN COALESCE(nt.minimum_puan, COALESCE(h.kullanilabilir_puan, 0)) - COALESCE(h.kullanilabilir_puan, 0) > 0
                        THEN COALESCE(nt.minimum_puan, COALESCE(h.kullanilabilir_puan, 0)) - COALESCE(h.kullanilabilir_puan, 0)
                    ELSE 0
                END AS kalan_puan,
                CASE
                    WHEN COALESCE(nt.minimum_puan, 0) <= 0 THEN 100
                    ELSE CASE
                        WHEN ROUND((COALESCE(h.kullanilabilir_puan, 0) / nt.minimum_puan) * 100, 0) > 100 THEN 100
                        ELSE ROUND((COALESCE(h.kullanilabilir_puan, 0) / nt.minimum_puan) * 100, 0)
                    END
                END AS ilerleme_yuzdesi
            FROM users u
            LEFT JOIN kullanici_sadakat_hesaplari h ON h.kullanici_id = u.id
            LEFT JOIN sadakat_seviyeleri ct ON ct.id = h.mevcut_seviye_id
            LEFT JOIN sadakat_seviyeleri nt ON nt.id = h.sonraki_seviye_id
            WHERE u.id = @userId
            ;", connection))
        {
            summaryCommand.Parameters.AddWithValue("@userId", userId);
            await using var reader = await summaryCommand.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                model.UserDisplayName = reader.GetString(0);
                model.TotalPoints = SafeInt(reader, 1);
                model.AvailablePoints = SafeInt(reader, 2);
                model.CurrentYearEarnedPoints = SafeInt(reader, 3);
                model.CurrentYearSpentPoints = SafeInt(reader, 4);
                model.PointsExpiryText = reader.GetDateTime(5).ToString("dd MMM yyyy", CultureInfo.GetCultureInfo("tr-TR"));
                model.CurrentTierName = reader.GetString(6);
                model.CurrentTierCode = reader.GetString(7);
                model.CurrentTierSummary = $"Avantajlar aktif. {model.CurrentTierName} seviyesindesiniz.";
                model.NextTierName = reader.IsDBNull(9) || string.IsNullOrWhiteSpace(reader.GetString(9)) ? null : reader.GetString(9);
                model.PointsToNextTier = SafeInt(reader, 10);
                model.ProgressPercent = SafeInt(reader, 11);
                model.CurrentTierCssClass = ResolveTierCssClass(model.CurrentTierCode);
                model.CurrentTierIconClass = ResolveTierIcon(model.CurrentTierCode);
                model.Benefits = ParseBenefits(reader.GetString(8), model.CurrentTierName);
            }
        }

        model.Tiers = await LoadLoyaltyTiersAsync(connection, model.CurrentTierCode, cancellationToken);
        model.PointTransactions = await LoadLoyaltyTransactionsAsync(connection, userId, cancellationToken);
        model.Rewards = await LoadLoyaltyRewardsAsync(connection, model.AvailablePoints, cancellationToken);
        model.Badges = await LoadUserBadgesAsync(connection, userId, cancellationToken);
        model.PassportCities = await LoadPassportCitiesAsync(connection, userId, cancellationToken);
        model.TravelPlans = await LoadTravelPlansAsync(connection, userId, cancellationToken);
        model.Offers = await LoadOffersAsync(connection, userId, cancellationToken);
        model.BudgetPlans = await LoadBudgetPlansAsync(connection, userId, cancellationToken);
        model.PriceAlerts = await LoadPriceAlertsAsync(connection, userId, alertHotelIds, cancellationToken);

        if (alertHotelIds.Count > 0)
        {
            var start = DateOnly.FromDateTime(DateTime.Today);
            var priceMap = await _hotelPricingReadService.GetHotelEffectivePriceMapAsync(alertHotelIds.Distinct().ToList(), start, start.AddDays(60), cancellationToken);
            foreach (var alert in model.PriceAlerts)
            {
                if (priceMap.TryGetValue(alert.HotelId, out var amount) && amount > 0)
                {
                    alert.CurrentPriceText = FormatMoney(amount);
                    alert.IsTriggered = TryParseCurrency(alert.TargetPriceText, out var targetPrice) && amount <= targetPrice;
                }
            }
        }

        model.Recommendations = await LoadRecommendationsAsync(connection, cancellationToken);
        model.BudgetPlanForm.DestinationCity = model.BudgetPlans.FirstOrDefault()?.DestinationText ?? string.Empty;
        model.BudgetPlanForm.TargetBudget = model.BudgetPlans.FirstOrDefault() is { } budget
            && TryParseCurrency(budget.BudgetText, out var budgetValue)
            ? budgetValue
            : null;
        model.TravelPlanForm.DestinationCity = model.PassportCities.FirstOrDefault(static item => !item.IsVisited)?.CityName ?? string.Empty;

        return model;
    }

    public async Task<(bool Success, string Message)> SaveBudgetPlanAsync(long userId, UserLoyaltyBudgetPlanForm form, CancellationToken cancellationToken = default)
    {
        var destination = (form.DestinationCity ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(destination))
        {
            return (false, "Butce planlayici icin hedef sehir zorunludur.");
        }

        if (!form.TargetBudget.HasValue || form.TargetBudget.Value <= 0)
        {
            return (false, "Hedef butce alanina gecerli bir tutar giriniz.");
        }

        var nightCount = form.NightCount <= 0 ? 2 : form.NightCount;
        var travelerCount = form.TravelerCount <= 0 ? 2 : form.TravelerCount;

        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = new SqlCommand(@"
            INSERT INTO kullanici_butce_planlari
            (kullanici_id, hedef_sehir, hedef_butce, gece_sayisi, kisi_sayisi, para_birimi, notlar, olusturulma_tarihi, guncellenme_tarihi)
            VALUES
            (@userId, @city, @budget, @nightCount, @travelerCount, 'TRY', @note, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);", connection);
        command.Parameters.AddWithValue("@userId", userId);
        command.Parameters.AddWithValue("@city", destination);
        command.Parameters.AddWithValue("@budget", form.TargetBudget.Value);
        command.Parameters.AddWithValue("@nightCount", nightCount);
        command.Parameters.AddWithValue("@travelerCount", travelerCount);
        command.Parameters.AddWithValue("@note", $"Web panelden olusturuldu · {DateTime.Now:dd.MM.yyyy HH:mm}");
        await command.ExecuteNonQueryAsync(cancellationToken);

        return (true, "Butce planiniz kaydedildi. Size uygun oteller aninda listelenecek.");
    }

    public async Task<(bool Success, string Message)> SaveTravelPlanAsync(long userId, UserLoyaltyTravelPlanForm form, CancellationToken cancellationToken = default)
    {
        var planName = (form.PlanName ?? string.Empty).Trim();
        var destination = (form.DestinationCity ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(planName) || string.IsNullOrWhiteSpace(destination))
        {
            return (false, "Plan adi ve hedef sehir zorunludur.");
        }

        if (!DateOnly.TryParseExact(form.StartDateText, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var startDate))
        {
            return (false, "Seyahat planinin baslangic tarihi zorunludur.");
        }

        if (!DateOnly.TryParseExact(form.EndDateText, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var endDate))
        {
            return (false, "Seyahat planinin bitis tarihi zorunludur.");
        }

        if (endDate < startDate)
        {
            return (false, "Bitis tarihi baslangic tarihinden once olamaz.");
        }

        await using var connection = await OpenConnectionAsync(cancellationToken);
        var planCode = $"PLAN-{userId:D4}-{DateTime.UtcNow:ddHHmmss}";
        var inviteCode = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
        await using var command = new SqlCommand(@"
            INSERT INTO kullanici_seyahat_planlari
            (olusturan_kullanici_id, plan_kodu, plan_adi, hedef_sehir, baslangic_tarihi, bitis_tarihi, butce_tutari, para_birimi, davet_kodu, durum, olusturulma_tarihi, guncellenme_tarihi)
            VALUES
            (@userId, @planCode, @planName, @city, @startDate, @endDate, @budget, 'TRY', @inviteCode, 'Taslak', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);", connection);
        command.Parameters.AddWithValue("@userId", userId);
        command.Parameters.AddWithValue("@planCode", planCode);
        command.Parameters.AddWithValue("@planName", planName);
        command.Parameters.AddWithValue("@city", destination);
        command.Parameters.AddWithValue("@startDate", startDate.ToDateTime(TimeOnly.MinValue));
        command.Parameters.AddWithValue("@endDate", endDate.ToDateTime(TimeOnly.MinValue));
        command.Parameters.AddWithValue("@budget", form.BudgetAmount ?? 0m);
        command.Parameters.AddWithValue("@inviteCode", inviteCode);
        await command.ExecuteNonQueryAsync(cancellationToken);

        return (true, "Seyahat planiniz kaydedildi. Artik otelleri ortak plana ekleyebilirsiniz.");
    }

    private async Task<SqlConnection> OpenConnectionAsync(CancellationToken cancellationToken)
    {
        var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }

    private async Task EnsureLoyaltyAccountAsync(SqlConnection connection, long userId, CancellationToken cancellationToken)
    {
        const string existsSql = "SELECT COUNT(*) FROM kullanici_sadakat_hesaplari WHERE kullanici_id = @userId;";
        await using (var existsCommand = new SqlCommand(existsSql, connection))
        {
            existsCommand.Parameters.AddWithValue("@userId", userId);
            var exists = Convert.ToInt32(await existsCommand.ExecuteScalarAsync(cancellationToken) ?? 0, CultureInfo.InvariantCulture);
            if (exists == 0)
            {
                var bronzeTierId = await ResolveTierIdAsync(connection, "BRONZE", cancellationToken);
                var silverTierId = await ResolveTierIdAsync(connection, "SILVER", cancellationToken);
                await using var insertCommand = new SqlCommand(@"
                    INSERT INTO kullanici_sadakat_hesaplari
                    (kullanici_id, toplam_puan, kullanilabilir_puan, bu_yil_kazanilan_puan, bu_yil_kullanilan_puan, mevcut_seviye_id, sonraki_seviye_id, puan_gecerlilik_tarihi, olusturulma_tarihi, guncellenme_tarihi)
                    VALUES
                    (@userId, 0, 0, 0, 0, @currentTierId, @nextTierId, DATEADD(DAY, 365, CAST(GETDATE() AS date)), CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);", connection);
                insertCommand.Parameters.AddWithValue("@userId", userId);
                insertCommand.Parameters.AddWithValue("@currentTierId", bronzeTierId);
                insertCommand.Parameters.AddWithValue("@nextTierId", silverTierId);
                await insertCommand.ExecuteNonQueryAsync(cancellationToken);
            }
        }

        await using var syncCommand = new SqlCommand(@"
            UPDATE h
            SET h.toplam_puan = agg.kazanilan,
                h.kullanilabilir_puan = CASE WHEN agg.kazanilan - agg.kullanilan > 0 THEN agg.kazanilan - agg.kullanilan ELSE 0 END,
                h.bu_yil_kazanilan_puan = yearly.yil_kazanilan,
                h.bu_yil_kullanilan_puan = yearly.yil_kullanilan,
                h.mevcut_seviye_id = COALESCE(current_tier.id, h.mevcut_seviye_id),
                h.sonraki_seviye_id = next_tier.id,
                h.son_seviye_guncelleme_tarihi = CURRENT_TIMESTAMP,
                h.guncellenme_tarihi = CURRENT_TIMESTAMP
            FROM kullanici_sadakat_hesaplari h
            CROSS APPLY (
                SELECT
                    COALESCE(SUM(CASE WHEN p.puan_degisim > 0 THEN p.puan_degisim ELSE 0 END), 0) AS kazanilan,
                    COALESCE(ABS(SUM(CASE WHEN p.puan_degisim < 0 THEN p.puan_degisim ELSE 0 END)), 0) AS kullanilan
                FROM kullanici_puan_hareketleri p
                WHERE p.kullanici_id = @userId
                  AND COALESCE(p.durum, 'Tamamlandi') <> 'Iptal'
            ) agg
            CROSS APPLY (
                SELECT
                    COALESCE(SUM(CASE WHEN y.puan_degisim > 0 THEN y.puan_degisim ELSE 0 END), 0) AS yil_kazanilan,
                    COALESCE(ABS(SUM(CASE WHEN y.puan_degisim < 0 THEN y.puan_degisim ELSE 0 END)), 0) AS yil_kullanilan
                FROM kullanici_puan_hareketleri y
                WHERE y.kullanici_id = @userId
                  AND YEAR(COALESCE(y.islem_tarihi, CURRENT_TIMESTAMP)) = YEAR(CAST(GETDATE() AS date))
            ) yearly
            OUTER APPLY (
                SELECT TOP (1) s.id
                FROM sadakat_seviyeleri s
                WHERE agg.kazanilan - agg.kullanilan >= s.minimum_puan
                  AND (s.maximum_puan IS NULL OR agg.kazanilan - agg.kullanilan <= s.maximum_puan)
                ORDER BY s.minimum_puan DESC
            ) current_tier
            OUTER APPLY (
                SELECT TOP (1) s2.id
                FROM sadakat_seviyeleri s2
                WHERE s2.minimum_puan > agg.kazanilan - agg.kullanilan
                ORDER BY s2.minimum_puan ASC
            ) next_tier
            WHERE h.kullanici_id = @userId;", connection);
        syncCommand.Parameters.AddWithValue("@userId", userId);
        await syncCommand.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task<long> ResolveTierIdAsync(SqlConnection connection, string code, CancellationToken cancellationToken)
    {
        await using var command = new SqlCommand("SELECT TOP (1) id FROM sadakat_seviyeleri WHERE kod = @code;", connection);
        command.Parameters.AddWithValue("@code", code);
        var value = await command.ExecuteScalarAsync(cancellationToken);
        return value is null ? 0L : Convert.ToInt64(value, CultureInfo.InvariantCulture);
    }

    private static List<UserLoyaltyBenefitViewModel> ParseBenefits(string rawText, string tierName)
    {
        var items = (rawText ?? string.Empty)
            .Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select((item, index) => new UserLoyaltyBenefitViewModel
            {
                Title = item,
                Description = $"{tierName} uyelik avantajiniz aktif.",
                IsUnlocked = true,
                IconClass = index switch
                {
                    0 => "fas fa-percent",
                    1 => "fas fa-clock",
                    2 => "fas fa-gift",
                    3 => "fas fa-headset",
                    _ => "fas fa-check-circle"
                },
                Tone = index switch
                {
                    0 => "primary",
                    1 => "warning",
                    2 => "success",
                    3 => "info",
                    _ => "primary"
                }
            })
            .ToList();

        return items.Count > 0
            ? items
            : new List<UserLoyaltyBenefitViewModel>
            {
                new()
                {
                    Title = "Hos geldin puani",
                    Description = "Sadakat hesabiniz hazir. Ilk rezervasyonunuz ile puan kazanmaya baslayin.",
                    IsUnlocked = true,
                    IconClass = "fas fa-star",
                    Tone = "primary"
                }
            };
    }

    private async Task<List<UserLoyaltyTierViewModel>> LoadLoyaltyTiersAsync(SqlConnection connection, string currentTierCode, CancellationToken cancellationToken)
    {
        var list = new List<UserLoyaltyTierViewModel>();
        await using var command = new SqlCommand(@"
            SELECT id, kod, ad, minimum_puan, maximum_puan, COALESCE(avantajlar_metin, '')
            FROM sadakat_seviyeleri
            WHERE aktif_mi = 1
            ORDER BY sira_no ASC, minimum_puan ASC;", connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var min = SafeInt(reader, 3);
            var max = reader.IsDBNull(4) ? (int?)null : SafeInt(reader, 4);
            var code = reader.GetString(1);
            list.Add(new UserLoyaltyTierViewModel
            {
                TierId = reader.GetInt64(0),
                Code = code,
                Name = reader.GetString(2),
                MinimumPoints = min,
                MaximumPoints = max,
                RangeText = max.HasValue ? $"{min:N0} - {max.Value:N0} Puan" : $"{min:N0}+ Puan",
                BenefitSummary = reader.GetString(5),
                CssClass = ResolveTierCssClass(code),
                IsCurrent = string.Equals(code, currentTierCode, StringComparison.OrdinalIgnoreCase)
            });
        }

        return list;
    }

    private async Task<List<UserLoyaltyPointTransactionViewModel>> LoadLoyaltyTransactionsAsync(SqlConnection connection, long userId, CancellationToken cancellationToken)
    {
        var list = new List<UserLoyaltyPointTransactionViewModel>();
        await using var command = new SqlCommand(@"
            SELECT CONVERT(varchar(10), COALESCE(islem_tarihi, olusturulma_tarihi), 104) AS tarih,
                   COALESCE(hareket_tipi, 'Bilgi'),
                   COALESCE(baslik, 'Puan hareketi'),
                   COALESCE(aciklama, ''),
                   COALESCE(puan_degisim, 0),
                   COALESCE(durum, 'Tamamlandi')
            FROM kullanici_puan_hareketleri
            WHERE kullanici_id = @userId
            ORDER BY COALESCE(islem_tarihi, olusturulma_tarihi) DESC, id DESC
            OFFSET 0 ROWS FETCH NEXT 10 ROWS ONLY;", connection);
        command.Parameters.AddWithValue("@userId", userId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var delta = SafeInt(reader, 4);
            var status = reader.GetString(5);
            list.Add(new UserLoyaltyPointTransactionViewModel
            {
                DateText = reader.GetString(0),
                TypeText = reader.GetString(1),
                Title = reader.GetString(2),
                Description = reader.GetString(3),
                PointChange = delta,
                PointChangeText = delta >= 0 ? $"+{delta:N0}" : delta.ToString("N0", CultureInfo.GetCultureInfo("tr-TR")),
                StatusText = status,
                StatusTone = string.Equals(status, "Beklemede", StringComparison.OrdinalIgnoreCase) ? "pending" : "completed"
            });
        }

        return list;
    }

    private async Task<List<UserLoyaltyRewardViewModel>> LoadLoyaltyRewardsAsync(SqlConnection connection, int availablePoints, CancellationToken cancellationToken)
    {
        var list = new List<UserLoyaltyRewardViewModel>();
        await using var command = new SqlCommand(@"
            SELECT TOP (8) id, ad, aciklama, gerekli_puan, COALESCE(ikon, 'fas fa-gift'), COALESCE(ton, 'primary')
            FROM sadakat_odulleri
            WHERE aktif_mi = 1
            ORDER BY gerekli_puan ASC, id ASC;", connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var required = SafeInt(reader, 3);
            list.Add(new UserLoyaltyRewardViewModel
            {
                RewardId = reader.GetInt64(0),
                Title = reader.GetString(1),
                Description = reader.GetString(2),
                RequiredPoints = required,
                IsAvailable = availablePoints >= required,
                IconClass = reader.GetString(4),
                Tone = reader.GetString(5)
            });
        }

        return list;
    }

    private async Task<List<UserLoyaltyBadgeViewModel>> LoadUserBadgesAsync(SqlConnection connection, long userId, CancellationToken cancellationToken)
    {
        var list = new List<UserLoyaltyBadgeViewModel>();
        await using var command = new SqlCommand(@"
            SELECT TOP (8) r.ad, COALESCE(r.ikon, 'fas fa-award'), COALESCE(kr.durum, 'Kilitli'), COALESCE(kr.ilerleme_degeri, 0), COALESCE(r.hedef_deger, 1)
            FROM rozet_tanimlari r
            LEFT JOIN kullanici_rozetleri kr ON kr.rozet_id = r.id AND kr.kullanici_id = @userId
            WHERE r.aktif_mi = 1
            ORDER BY r.siralama ASC, r.id ASC;", connection);
        command.Parameters.AddWithValue("@userId", userId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var progress = SafeInt(reader, 3);
            var target = Math.Max(1, SafeInt(reader, 4));
            var earned = string.Equals(reader.GetString(2), "Kazanildi", StringComparison.OrdinalIgnoreCase);
            list.Add(new UserLoyaltyBadgeViewModel
            {
                Title = reader.GetString(0),
                IconClass = reader.GetString(1),
                IsEarned = earned,
                ProgressText = earned ? "Kazanildi" : $"{progress}/{target}"
            });
        }

        return list;
    }

    private async Task<List<UserLoyaltyPassportCityViewModel>> LoadPassportCitiesAsync(SqlConnection connection, long userId, CancellationToken cancellationToken)
    {
        var list = new List<UserLoyaltyPassportCityViewModel>();
        await using var command = new SqlCommand(@"
            SELECT TOP (8) sehir, COALESCE(ulke, 'Türkiye'), toplam_konaklama_sayisi, ilk_konaklama_tarihi, son_konaklama_tarihi
            FROM kullanici_dijital_pasaportlari
            WHERE kullanici_id = @userId
            ORDER BY son_konaklama_tarihi DESC, toplam_konaklama_sayisi DESC;", connection);
        command.Parameters.AddWithValue("@userId", userId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var firstVisit = reader.IsDBNull(3) ? (DateTime?)null : reader.GetDateTime(3);
            var lastVisit = reader.IsDBNull(4) ? (DateTime?)null : reader.GetDateTime(4);
            list.Add(new UserLoyaltyPassportCityViewModel
            {
                CityName = reader.GetString(0),
                CountryName = reader.GetString(1),
                IsVisited = true,
                VisitText = firstVisit.HasValue && lastVisit.HasValue
                    ? $"{firstVisit.Value:dd.MM.yyyy} · {lastVisit.Value:dd.MM.yyyy}"
                    : $"{SafeInt(reader, 2)} konaklama"
            });
        }

        return list;
    }

    private async Task<List<UserLoyaltyTravelPlanViewModel>> LoadTravelPlansAsync(SqlConnection connection, long userId, CancellationToken cancellationToken)
    {
        var list = new List<UserLoyaltyTravelPlanViewModel>();
        await using var command = new SqlCommand(@"
            SELECT TOP (5) p.id, p.plan_adi, p.hedef_sehir, p.baslangic_tarihi, p.bitis_tarihi, COALESCE(p.butce_tutari, 0), COALESCE(p.durum, 'Taslak'),
                   COALESCE(COUNT(ps.id), 0) AS secim_sayisi
            FROM kullanici_seyahat_planlari p
            LEFT JOIN kullanici_seyahat_plan_otel_secimleri ps ON ps.plan_id = p.id
            WHERE p.olusturan_kullanici_id = @userId
            GROUP BY p.id, p.plan_adi, p.hedef_sehir, p.baslangic_tarihi, p.bitis_tarihi, p.butce_tutari, p.durum
            ORDER BY p.guncellenme_tarihi DESC, p.id DESC;", connection);
        command.Parameters.AddWithValue("@userId", userId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var startDate = reader.IsDBNull(3) ? (DateTime?)null : reader.GetDateTime(3);
            var endDate = reader.IsDBNull(4) ? (DateTime?)null : reader.GetDateTime(4);
            list.Add(new UserLoyaltyTravelPlanViewModel
            {
                PlanId = reader.GetInt64(0),
                PlanName = reader.GetString(1),
                DestinationText = reader.GetString(2),
                DateText = startDate.HasValue && endDate.HasValue ? $"{startDate.Value:dd MMM} - {endDate.Value:dd MMM yyyy}" : "Tarih secimi bekleniyor",
                BudgetText = FormatMoney(SafeDecimal(reader, 5)),
                StatusText = reader.GetString(6),
                VoteSummary = $"{SafeInt(reader, 7)} otel secenegi"
            });
        }

        return list;
    }

    private async Task<List<UserLoyaltyOfferViewModel>> LoadOffersAsync(SqlConnection connection, long userId, CancellationToken cancellationToken)
    {
        var list = new List<UserLoyaltyOfferViewModel>();
        await using var command = new SqlCommand(@"
            SELECT TOP (4) baslik, aciklama, kampanya_kodu, COALESCE(buton_url, '/oteller'), gecerlilik_bitis
            FROM kullanici_ozel_teklifleri
            WHERE aktif_mi = 1
              AND (kullanici_id = @userId OR kullanici_id IS NULL)
              AND CAST(GETDATE() AS date) BETWEEN gecerlilik_baslangic AND gecerlilik_bitis
            ORDER BY CASE WHEN kullanici_id = @userId THEN 0 ELSE 1 END, gecerlilik_bitis ASC, id DESC
            ;", connection);
        command.Parameters.AddWithValue("@userId", userId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            list.Add(new UserLoyaltyOfferViewModel
            {
                Title = reader.GetString(0),
                Description = reader.GetString(1),
                Code = reader.GetString(2),
                ActionUrl = reader.GetString(3),
                ValidityText = $"Son gun {reader.GetDateTime(4):dd MMM yyyy}"
            });
        }

        return list;
    }

    private async Task<List<UserLoyaltyBudgetPlanViewModel>> LoadBudgetPlansAsync(SqlConnection connection, long userId, CancellationToken cancellationToken)
    {
        var list = new List<UserLoyaltyBudgetPlanViewModel>();
        await using var command = new SqlCommand(@"
            SELECT TOP (4) hedef_sehir, hedef_butce, gece_sayisi, kisi_sayisi
            FROM kullanici_butce_planlari
            WHERE kullanici_id = @userId
            ORDER BY guncellenme_tarihi DESC, id DESC;", connection);
        command.Parameters.AddWithValue("@userId", userId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var nightCount = SafeInt(reader, 2);
            var travelerCount = SafeInt(reader, 3);
            list.Add(new UserLoyaltyBudgetPlanViewModel
            {
                DestinationText = reader.GetString(0),
                BudgetText = FormatMoney(SafeDecimal(reader, 1)),
                TravelerText = $"{nightCount} gece · {travelerCount} kisi",
                SuggestionText = travelerCount > 2 ? "Aile dostu ve baglantili odalari filtreleyin." : "Sehir otelleri ve hafta sonu firsatlari uygun gorunuyor."
            });
        }

        return list;
    }

    private async Task<List<UserLoyaltyPriceAlertViewModel>> LoadPriceAlertsAsync(SqlConnection connection, long userId, List<long> alertHotelIds, CancellationToken cancellationToken)
    {
        var list = new List<UserLoyaltyPriceAlertViewModel>();
        await using var command = new SqlCommand(@"
            SELECT TOP (4) a.otel_id, o.otel_adi, a.hedef_maksimum_fiyat
            FROM user_favorite_price_alerts a
            INNER JOIN oteller o ON o.id = a.otel_id
            WHERE a.user_id = @userId
              AND COALESCE(a.aktif_mi, 1) = 1
            ORDER BY a.guncellenme_tarihi DESC, a.id DESC;", connection);
        command.Parameters.AddWithValue("@userId", userId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var hotelId = reader.GetInt64(0);
            alertHotelIds.Add(hotelId);
            list.Add(new UserLoyaltyPriceAlertViewModel
            {
                HotelId = hotelId,
                HotelName = reader.GetString(1),
                CurrentPriceText = "Hesaplaniyor",
                TargetPriceText = FormatMoney(SafeDecimal(reader, 2))
            });
        }

        return list;
    }

    private async Task<List<UserLoyaltyRecommendationViewModel>> LoadRecommendationsAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        var list = new List<UserLoyaltyRecommendationViewModel>();
        await using var command = new SqlCommand(@"
            SELECT TOP (4) o.otel_adi, o.otel_kodu, COALESCE(o.ilce, ''), COALESCE(o.sehir, ''), COALESCE(o.ortalama_puan, 0), COALESCE(og.gorsel_url, '')
            FROM oteller o
            LEFT JOIN otel_gorselleri og ON og.otel_id = o.id AND (og.kapak_fotografi_mi = 1 OR og.siralama = 1)
            WHERE o.yayin_durumu = 'Yayında'
              AND o.onay_durumu = 'Onaylandı'
            ORDER BY COALESCE(o.ortalama_puan, 0) DESC, COALESCE(o.toplam_yorum_sayisi, 0) DESC, o.id DESC;", connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var hotelName = reader.GetString(0);
            var hotelCode = reader.GetString(1);
            list.Add(new UserLoyaltyRecommendationViewModel
            {
                HotelName = hotelName,
                DistrictText = $"{reader.GetString(2)} / {reader.GetString(3)}",
                ImageUrl = string.IsNullOrWhiteSpace(reader.GetString(5)) ? "/uploads/logo/logo.png" : reader.GetString(5),
                RatingText = SafeDecimal(reader, 4) > 0 ? $"{SafeDecimal(reader, 4):0.0} puan" : "Yeni liste",
                Url = $"/oteller/{BuildSlug(hotelName, hotelCode)}"
            });
        }

        return list;
    }

    private static string ResolveTierCssClass(string tierCode)
        => tierCode.ToUpperInvariant() switch
        {
            "S" or "SILVER" => "silver",
            "G" or "GOLD" => "gold",
            "P" or "PLATINUM" => "platinum",
            _ => "bronze"
        };

    private static string ResolveTierIcon(string tierCode)
        => tierCode.ToUpperInvariant() switch
        {
            "S" or "SILVER" => "fas fa-medal",
            "G" or "GOLD" => "fas fa-crown",
            "P" or "PLATINUM" => "fas fa-gem",
            _ => "fas fa-star"
        };

    private static bool TryParseCurrency(string? value, out decimal amount)
    {
        amount = 0m;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var normalized = value.Replace("₺", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("TRY", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Trim();

        return decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.GetCultureInfo("tr-TR"), out amount)
               || decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out amount);
    }

    private async Task<List<UserReservationCardViewModel>> LoadReservationsAsync(SqlConnection connection, long userId, int take, CancellationToken cancellationToken)
    {
        var list = new List<UserReservationCardViewModel>();
        const string sql = @"
            SELECT TOP (@take) r.id, r.rezervasyon_no, o.otel_adi, o.otel_kodu, COALESCE(o.ilce, ''), COALESCE(o.sehir, ''),
                   COALESCE(ot.oda_adi, 'Oda'), r.giris_tarihi, r.cikis_tarihi, r.yetiskin_sayisi, r.cocuk_sayisi,
                   r.toplam_tutar, r.durum, COALESCE(og.gorsel_url, ''), COALESCE(r.otel_onay_durumu, ''),
                   COALESCE(NULLIF(r.iptal_nedeni, ''), '') AS iptal_nedeni,
                   r.iptal_tarihi
            FROM rezervasyonlar r
            INNER JOIN oteller o ON o.id = r.otel_id
            LEFT JOIN oda_tipleri ot ON ot.id = r.oda_tip_id
            LEFT JOIN (
                SELECT ranked.otel_id, ranked.gorsel_url
                FROM (
                    SELECT
                        g.otel_id,
                        g.gorsel_url,
                        ROW_NUMBER() OVER (
                            PARTITION BY g.otel_id
                            ORDER BY COALESCE(g.kapak_fotografi_mi, 0) DESC, COALESCE(g.siralama, 2147483647) ASC, g.id ASC
                        ) AS rn
                    FROM otel_gorselleri g
                    WHERE COALESCE(g.gorsel_url, '') <> ''
                ) ranked
                WHERE ranked.rn = 1
            ) og ON og.otel_id = o.id
            WHERE r.kullanici_id = @userId
            ORDER BY r.giris_tarihi DESC, r.id DESC;";
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@userId", userId);
        command.Parameters.AddWithValue("@take", take);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var checkIn = reader.GetDateTime(7);
            var checkOut = reader.GetDateTime(8);
            var status = reader.GetString(12);
            var isCancelled = string.Equals(status, "İptal Edildi", StringComparison.OrdinalIgnoreCase);
            var isUpcoming = checkOut >= DateTime.Today;
            var adultCount = SafeInt(reader, 9);
            var childCount = SafeInt(reader, 10);
            list.Add(new UserReservationCardViewModel
            {
                ReservationId = reader.GetInt64(0),
                ReservationNo = reader.GetString(1),
                HotelName = reader.GetString(2),
                HotelSlug = BuildSlug(reader.GetString(2), reader.GetString(3)),
                District = reader.GetString(4),
                City = reader.GetString(5),
                RoomName = reader.GetString(6),
                StayDateText = $"{checkIn:dd MMM} - {checkOut:dd MMM yyyy}",
                CheckInDate = DateOnly.FromDateTime(checkIn),
                CheckOutDate = DateOnly.FromDateTime(checkOut),
                GuestText = $"{adultCount} Yetiskin" + (childCount > 0 ? $", {childCount} Cocuk" : string.Empty),
                MealOrRoomText = reader.GetString(6),
                StatusText = status,
                StatusTone = GetReservationStatusTone(status, reader.GetString(14), checkIn),
                SubNote = BuildReservationNote(status, checkIn, checkOut),
                SubNoteTone = isCancelled ? "danger" : isUpcoming ? "info" : "success",
                TotalText = FormatMoney(SafeDecimal(reader, 11)),
                ImageUrl = string.IsNullOrWhiteSpace(reader.GetString(13)) ? null : reader.GetString(13),
                CanCancel = isUpcoming && !isCancelled,
                IsUpcoming = isUpcoming,
                IsCancelled = isCancelled,
                CancellationReason = reader.IsDBNull(15) || string.IsNullOrWhiteSpace(reader.GetString(15)) ? null : reader.GetString(15),
                CancellationTimeText = reader.IsDBNull(16) ? null : reader.GetDateTime(16).ToString("dd.MM.yyyy HH:mm", CultureInfo.GetCultureInfo("tr-TR"))
            });
        }
        return list;
    }

    private async Task<List<UserReservationCardViewModel>> LoadDashboardReservationsAsync(
        SqlConnection connection,
        long userId,
        string statusFilter,
        DateOnly? startDate,
        DateOnly? endDate,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var list = new List<UserReservationCardViewModel>();
        const string sql = @"
            SELECT r.id, r.rezervasyon_no, o.otel_adi, o.otel_kodu, COALESCE(o.ilce, ''), COALESCE(o.sehir, ''),
                   COALESCE(ot.oda_adi, 'Oda'), r.giris_tarihi, r.cikis_tarihi, r.yetiskin_sayisi, r.cocuk_sayisi,
                   r.toplam_tutar, r.durum, COALESCE(og.gorsel_url, ''), COALESCE(r.otel_onay_durumu, ''),
                   COALESCE(NULLIF(r.iptal_nedeni, ''), '') AS iptal_nedeni,
                   r.iptal_tarihi
            FROM rezervasyonlar r
            INNER JOIN oteller o ON o.id = r.otel_id
            LEFT JOIN oda_tipleri ot ON ot.id = r.oda_tip_id
            LEFT JOIN (
                SELECT ranked.otel_id, ranked.gorsel_url
                FROM (
                    SELECT
                        g.otel_id,
                        g.gorsel_url,
                        ROW_NUMBER() OVER (
                            PARTITION BY g.otel_id
                            ORDER BY COALESCE(g.kapak_fotografi_mi, 0) DESC, COALESCE(g.siralama, 2147483647) ASC, g.id ASC
                        ) AS rn
                    FROM otel_gorselleri g
                    WHERE COALESCE(g.gorsel_url, '') <> ''
                ) ranked
                WHERE ranked.rn = 1
            ) og ON og.otel_id = o.id
            WHERE r.kullanici_id = @userId
              AND (@statusFilter = 'all'
                   OR (@statusFilter = 'approved' AND r.durum = 'Onaylandı')
                   OR (@statusFilter = 'pending' AND (COALESCE(r.otel_onay_durumu, '') = 'Beklemede' OR r.durum = 'Onay Bekliyor')))
              AND (@startDate IS NULL OR CAST(r.giris_tarihi AS date) >= @startDate)
              AND (@endDate IS NULL OR CAST(r.giris_tarihi AS date) <= @endDate)
            ORDER BY r.giris_tarihi DESC, r.id DESC
            OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY;";
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@userId", userId);
        command.Parameters.AddWithValue("@statusFilter", statusFilter);
        command.Parameters.AddWithValue("@startDate", startDate.HasValue ? startDate.Value.ToDateTime(TimeOnly.MinValue) : DBNull.Value);
        command.Parameters.AddWithValue("@endDate", endDate.HasValue ? endDate.Value.ToDateTime(TimeOnly.MinValue) : DBNull.Value);
        command.Parameters.AddWithValue("@offset", (page - 1) * pageSize);
        command.Parameters.AddWithValue("@pageSize", pageSize);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var checkIn = reader.GetDateTime(7);
            var checkOut = reader.GetDateTime(8);
            var status = reader.GetString(12);
            var isCancelled = string.Equals(status, "İptal Edildi", StringComparison.OrdinalIgnoreCase);
            var isUpcoming = checkOut >= DateTime.Today;
            var adultCount = SafeInt(reader, 9);
            var childCount = SafeInt(reader, 10);
            list.Add(new UserReservationCardViewModel
            {
                ReservationId = reader.GetInt64(0),
                ReservationNo = reader.GetString(1),
                HotelName = reader.GetString(2),
                HotelSlug = BuildSlug(reader.GetString(2), reader.GetString(3)),
                District = reader.GetString(4),
                City = reader.GetString(5),
                RoomName = reader.GetString(6),
                StayDateText = $"{checkIn:dd MMM} - {checkOut:dd MMM yyyy}",
                CheckInDate = DateOnly.FromDateTime(checkIn),
                CheckOutDate = DateOnly.FromDateTime(checkOut),
                GuestText = $"{adultCount} Yetiskin" + (childCount > 0 ? $", {childCount} Cocuk" : string.Empty),
                MealOrRoomText = reader.GetString(6),
                StatusText = status,
                StatusTone = GetReservationStatusTone(status, reader.GetString(14), checkIn),
                SubNote = BuildReservationNote(status, checkIn, checkOut),
                SubNoteTone = isCancelled ? "danger" : isUpcoming ? "info" : "success",
                TotalText = FormatMoney(SafeDecimal(reader, 11)),
                ImageUrl = string.IsNullOrWhiteSpace(reader.GetString(13)) ? null : reader.GetString(13),
                CanCancel = isUpcoming && !isCancelled,
                IsUpcoming = isUpcoming,
                IsCancelled = isCancelled,
                CancellationReason = reader.IsDBNull(15) || string.IsNullOrWhiteSpace(reader.GetString(15)) ? null : reader.GetString(15),
                CancellationTimeText = reader.IsDBNull(16) ? null : reader.GetDateTime(16).ToString("dd.MM.yyyy HH:mm", CultureInfo.GetCultureInfo("tr-TR"))
            });
        }
        return list;
    }

    private static async Task<int> CountDashboardReservationsAsync(
        SqlConnection connection,
        long userId,
        string statusFilter,
        DateOnly? startDate,
        DateOnly? endDate,
        CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT COUNT(*)
            FROM rezervasyonlar r
            WHERE r.kullanici_id = @userId
              AND (@statusFilter = 'all'
                   OR (@statusFilter = 'approved' AND r.durum = 'Onaylandı')
                   OR (@statusFilter = 'pending' AND (COALESCE(r.otel_onay_durumu, '') = 'Beklemede' OR r.durum = 'Onay Bekliyor')))
              AND (@startDate IS NULL OR CAST(r.giris_tarihi AS date) >= @startDate)
              AND (@endDate IS NULL OR CAST(r.giris_tarihi AS date) <= @endDate);";
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@userId", userId);
        command.Parameters.AddWithValue("@statusFilter", statusFilter);
        command.Parameters.AddWithValue("@startDate", startDate.HasValue ? startDate.Value.ToDateTime(TimeOnly.MinValue) : DBNull.Value);
        command.Parameters.AddWithValue("@endDate", endDate.HasValue ? endDate.Value.ToDateTime(TimeOnly.MinValue) : DBNull.Value);
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken) ?? 0, CultureInfo.InvariantCulture);
    }

    private static string NormalizeDashboardReservationStatus(string? status)
    {
        var normalized = (status ?? string.Empty).Trim().ToLowerInvariant();
        return normalized is "approved" or "pending" ? normalized : "all";
    }

    private async Task<List<UserFavoriteSummaryViewModel>> LoadFavoriteSummariesAsync(SqlConnection connection, long userId, int take, CancellationToken cancellationToken)
    {
        var list = new List<UserFavoriteSummaryViewModel>();
        const string sql = @"
            SELECT o.otel_adi, COALESCE(o.ilce, ''), COALESCE(o.sehir, ''), COALESCE(o.ortalama_puan, 0), COALESCE(og.gorsel_url, '')
            FROM user_favori_oteller uf
            INNER JOIN oteller o ON o.id = uf.otel_id
            LEFT JOIN otel_gorselleri og ON og.otel_id = o.id AND (og.kapak_fotografi_mi = 1 OR og.siralama = 1)
            WHERE uf.user_id = @userId AND uf.aktif_mi = 1
            ORDER BY uf.son_islem_tarihi DESC, uf.id DESC
            OFFSET 0 ROWS FETCH NEXT @take ROWS ONLY;";
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@userId", userId);
        command.Parameters.AddWithValue("@take", take);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            list.Add(new UserFavoriteSummaryViewModel
            {
                HotelName = reader.GetString(0),
                District = reader.GetString(1),
                City = reader.GetString(2),
                RatingText = $"{SafeDecimal(reader, 3):0.0} Puan",
                ImageUrl = string.IsNullOrWhiteSpace(reader.GetString(4)) ? null : reader.GetString(4)
            });
        }
        return list;
    }

    private static (string FirstName, string LastName) SplitName(string fullName)
    {
        var parts = (fullName ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return (string.Empty, string.Empty);
        if (parts.Length == 1) return (parts[0], string.Empty);
        return (parts[0], string.Join(' ', parts.Skip(1)));
    }

    private static string BuildAvatar(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return "OT";
        var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length == 1 ? input[..Math.Min(2, input.Length)].ToUpperInvariant() : $"{char.ToUpperInvariant(parts[0][0])}{char.ToUpperInvariant(parts[1][0])}";
    }

    private static string BuildSlug(string hotelName, string hotelCode)
    {
        var source = string.IsNullOrWhiteSpace(hotelName) ? hotelCode : hotelName;
        if (string.IsNullOrWhiteSpace(source))
        {
            return "otel";
        }

        var buffer = new List<char>(source.Length);
        foreach (var ch in source.ToLowerInvariant())
        {
            buffer.Add(ch switch
            {
                'ı' => 'i',
                'ğ' => 'g',
                'ü' => 'u',
                'ş' => 's',
                'ö' => 'o',
                'ç' => 'c',
                '&' => '-',
                _ when char.IsLetterOrDigit(ch) => ch,
                _ => '-'
            });
        }

        var slug = new string(buffer.ToArray()).Trim('-');
        while (slug.Contains("--", StringComparison.Ordinal))
        {
            slug = slug.Replace("--", "-", StringComparison.Ordinal);
        }

        return string.IsNullOrWhiteSpace(slug) ? hotelCode.ToLowerInvariant() : slug;
    }

    private static int SafeInt(SqlDataReader reader, int ordinal)
        => reader.IsDBNull(ordinal) ? 0 : Convert.ToInt32(reader.GetValue(ordinal), CultureInfo.InvariantCulture);

    private static bool SafeBool(SqlDataReader reader, int ordinal)
        => !reader.IsDBNull(ordinal) && Convert.ToInt32(reader.GetValue(ordinal), CultureInfo.InvariantCulture) == 1;

    private static decimal SafeDecimal(SqlDataReader reader, int ordinal)
        => reader.IsDBNull(ordinal) ? 0m : reader.GetDecimal(ordinal);

    private static string FormatMoney(decimal amount)
        => $"₺{amount:N0}";

    private static string GetReservationStatusTone(string status, string hotelApprovalStatus, DateTime checkIn)
        => status switch
        {
            "İptal Edildi" => "danger",
            "Onaylandı" when checkIn <= DateTime.Today.AddDays(2) => "info",
            "Onaylandı" => "ok",
            _ when string.Equals(hotelApprovalStatus, "Beklemede", StringComparison.OrdinalIgnoreCase) => "wait",
            _ => "wait"
        };

    private static string BuildReservationNote(string status, DateTime checkIn, DateTime checkOut)
    {
        if (string.Equals(status, "İptal Edildi", StringComparison.OrdinalIgnoreCase)) return "Rezervasyon iptal edildi.";
        if (string.Equals(status, "Onaylandı", StringComparison.OrdinalIgnoreCase) && checkIn <= DateTime.Today.AddDays(2) && checkOut >= DateTime.Today) return "Check-in tarihi yaklaşıyor.";
        if (string.Equals(status, "Onaylandı", StringComparison.OrdinalIgnoreCase)) return "Rezervasyon onaylı ve konaklama planı hazır.";
        return "Rezervasyon otel onay sürecinde.";
    }

    private static string NormalizeReservationStatusFilter(string? statusFilter)
    {
        var value = (statusFilter ?? string.Empty).Trim().ToLowerInvariant();
        return value is "upcoming" or "past" or "cancelled" ? value : "all";
    }

    private async Task<ReservationCancellationSnapshot?> LoadReservationCancellationSnapshotAsync(SqlConnection connection, long userId, long reservationId, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT TOP (1) r.id,
                   r.otel_id,
                   r.rezervasyon_no,
                   COALESCE(NULLIF(r.misafir_ad_soyad, ''), 'Misafir') AS misafir_ad_soyad,
                   r.giris_tarihi,
                   r.cikis_tarihi,
                   COALESCE(r.toplam_tutar, 0) AS toplam_tutar,
                   COALESCE(NULLIF(ot.oda_adi, ''), 'Oda') AS oda_adi,
                   COALESCE(NULLIF(o.otel_adi, ''), 'Otel') AS otel_adi
            FROM rezervasyonlar r
            LEFT JOIN oda_tipleri ot ON ot.id = r.oda_tip_id
            LEFT JOIN oteller o ON o.id = r.otel_id
            WHERE r.id = @reservationId
              AND r.kullanici_id = @userId;";
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@reservationId", reservationId);
        command.Parameters.AddWithValue("@userId", userId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new ReservationCancellationSnapshot(
            reader.GetInt64(0),
            reader.GetInt64(1),
            reader.GetString(2),
            reader.GetString(3),
            reader.GetDateTime(4),
            reader.GetDateTime(5),
            SafeDecimal(reader, 6),
            reader.GetString(7),
            reader.GetString(8));
    }

    private static async Task<(long UserId, string Email, string ManagerName)> ResolvePartnerRecipientAsync(SqlConnection connection, long hotelId, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT TOP (1) COALESCE(o.user_id, oks.user_id, 1),
                   COALESCE(u.eposta, o.satis_kontak_eposta, o.eposta, 'partner@otelturizm.com'),
                   COALESCE(u.ad_soyad, o.satis_kontak_adi, 'Partner Yetkilisi')
            FROM oteller o
            LEFT JOIN otel_kullanici_sahiplikleri oks ON oks.otel_id = o.id AND oks.aktif_mi = 1
            LEFT JOIN users u ON u.id = COALESCE(o.user_id, oks.user_id)
            WHERE o.id = @hotelId
            ORDER BY oks.ana_sorumlu_mu DESC, oks.id ASC;";
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@hotelId", hotelId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            return (reader.IsDBNull(0) ? 1L : reader.GetInt64(0), reader.GetString(1), reader.GetString(2));
        }

        return (1L, "partner@otelturizm.com", "Partner Yetkilisi");
    }

    private sealed record ReservationCancellationSnapshot(
        long ReservationId,
        long HotelId,
        string ReservationNo,
        string GuestName,
        DateTime CheckIn,
        DateTime CheckOut,
        decimal TotalAmount,
        string RoomName,
        string HotelName);
}
