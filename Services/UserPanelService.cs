using System.Globalization;
using MySqlConnector;
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

    public UserPanelService(
        IConfiguration configuration,
        IMessageCenterService messageCenterService,
        IAddressLookupService addressLookupService,
        IEmailQueueService emailQueueService)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection tanimli degil.");
        _messageCenterService = messageCenterService;
        _addressLookupService = addressLookupService;
        _emailQueueService = emailQueueService;
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
                (SELECT COUNT(*) FROM rezervasyonlar WHERE kullanici_id = @userId AND durum <> 'İptal Edildi' AND cikis_tarihi >= CURDATE()) AS upcoming_count,
                (SELECT COUNT(*) FROM user_favori_oteller WHERE user_id = @userId AND aktif_mi = 1) AS favorite_count,
                (SELECT COUNT(*) FROM mesaj_konusmalari WHERE misafir_kullanici_id = @userId AND durum <> 'Arşivlendi') AS message_count,
                (SELECT COALESCE(SUM(toplam_tasarruf), 0) FROM rezervasyonlar WHERE kullanici_id = @userId) AS total_discount;";

        await using (var command = new MySqlCommand(summarySql, connection))
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

    public async Task<UserReservationsPageViewModel> GetReservationsAsync(long userId, CancellationToken cancellationToken = default)
    {
        var model = new UserReservationsPageViewModel();
        await using var connection = await OpenConnectionAsync(cancellationToken);
        model.Reservations = await LoadReservationsAsync(connection, userId, 100, cancellationToken);
        model.UpcomingCount = model.Reservations.Count(x => x.IsUpcoming && !x.IsCancelled);
        model.PastCount = model.Reservations.Count(x => !x.IsUpcoming && !x.IsCancelled);
        model.CancelledCount = model.Reservations.Count(x => x.IsCancelled);
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
            SELECT durum, giris_tarihi
            FROM rezervasyonlar
            WHERE id = @reservationId AND kullanici_id = @userId
            LIMIT 1;";

        string? currentStatus = null;
        DateTime? checkInDate = null;
        await using (var selectCommand = new MySqlCommand(selectSql, connection))
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
                iptal_tarihi = NOW()
            WHERE id = @reservationId AND kullanici_id = @userId;";
        await using var updateCommand = new MySqlCommand(updateSql, connection);
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
            SELECT ad_soyad, eposta, COALESCE(telefon, ''), tc_kimlik_no, dogum_tarihi, cinsiyet, uyruk, adres, sehir, ilce, mahalle, posta_kodu,
                   tercih_edilen_oda_tipi, yatak_tercihi, konusulan_diller, seyahat_amaci, ozel_istekler
            FROM users
            WHERE id = @userId
            LIMIT 1;";

        var model = new UserProfilePageViewModel();
        await using var command = new MySqlCommand(sql, connection);
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
        await using (var duplicateCheckCommand = new MySqlCommand("SELECT COUNT(*) FROM users WHERE eposta = @email AND id <> @userId;", connection))
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
            await using var command = new MySqlCommand(@"
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
        catch (MySqlException)
        {
            return false;
        }
    }

    public async Task<UserNotificationsPageViewModel> GetNotificationsAsync(long userId, CancellationToken cancellationToken = default)
    {
        var model = new UserNotificationsPageViewModel();
        await using var connection = await OpenConnectionAsync(cancellationToken);

        await using (var command = new MySqlCommand(@"
            SELECT rezervasyon_eposta, rezervasyon_push, checkin_hatirlatma, iptal_degisim, kampanya_eposta, kampanya_sms, sistem_bildirimi
            FROM kullanici_bildirim_tercihleri WHERE kullanici_id = @userId LIMIT 1;", connection))
        {
            command.Parameters.AddWithValue("@userId", userId);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                model.Form = new UserNotificationPreferencesForm
                {
                    ReservationEmail = reader.GetBoolean(0),
                    ReservationPush = reader.GetBoolean(1),
                    CheckInReminder = reader.GetBoolean(2),
                    CancellationChanges = reader.GetBoolean(3),
                    CampaignEmail = reader.GetBoolean(4),
                    CampaignSms = reader.GetBoolean(5),
                    SystemNotifications = reader.GetBoolean(6)
                };
            }
        }

        await using (var command = new MySqlCommand(@"
            SELECT baslik, mesaj, bildirim_turu, olusturulma_tarihi
            FROM sistem_ici_bildirimler
            WHERE kullanici_id = @userId AND arsivlendi_mi = 0
            ORDER BY olusturulma_tarihi DESC, id DESC LIMIT 6;", connection))
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
        await using var command = new MySqlCommand(@"
            INSERT INTO kullanici_bildirim_tercihleri
            (kullanici_id, rezervasyon_eposta, rezervasyon_push, checkin_hatirlatma, iptal_degisim, kampanya_eposta, kampanya_sms, sistem_bildirimi)
            VALUES
            (@userId, @reservationEmail, @reservationPush, @checkInReminder, @cancellationChanges, @campaignEmail, @campaignSms, @systemNotifications)
            ON DUPLICATE KEY UPDATE
                rezervasyon_eposta = VALUES(rezervasyon_eposta),
                rezervasyon_push = VALUES(rezervasyon_push),
                checkin_hatirlatma = VALUES(checkin_hatirlatma),
                iptal_degisim = VALUES(iptal_degisim),
                kampanya_eposta = VALUES(kampanya_eposta),
                kampanya_sms = VALUES(kampanya_sms),
                sistem_bildirimi = VALUES(sistem_bildirimi),
                guncellenme_tarihi = CURRENT_TIMESTAMP;", connection);
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

        await using (var command = new MySqlCommand("SELECT iki_asamali_dogrulama_aktif_mi FROM users WHERE id = @userId LIMIT 1;", connection))
        {
            command.Parameters.AddWithValue("@userId", userId);
            var scalar = await command.ExecuteScalarAsync(cancellationToken);
            model.TwoFactorEnabled = scalar is not null && scalar != DBNull.Value && Convert.ToInt32(scalar, CultureInfo.InvariantCulture) == 1;
        }

        await using var sessionCommand = new MySqlCommand(@"
            SELECT COALESCE(cihaz_etiketi, 'Bilinmeyen cihaz'), beni_hatirla_tercihi, toplam_oturum_suresi_saniye, son_aktivite_tarihi
            FROM kullanici_oturum_istatistikleri
            WHERE kullanici_id = @userId AND hesap_tipi = 'user'
            ORDER BY son_aktivite_tarihi DESC, id DESC LIMIT 8;", connection);
        sessionCommand.Parameters.AddWithValue("@userId", userId);
        await using var reader = await sessionCommand.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var duration = reader.IsDBNull(2) ? 0L : reader.GetInt64(2);
            var lastActive = reader.IsDBNull(3) ? (DateTime?)null : reader.GetDateTime(3).ToLocalTime();
            model.Sessions.Add(new UserSessionRowViewModel
            {
                DeviceLabel = reader.GetString(0),
                RememberText = !reader.IsDBNull(1) && reader.GetBoolean(1) ? "Beni hatırla açık" : "Standart oturum",
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
        await using var command = new MySqlCommand(@"
            UPDATE users SET sifre = SHA2(@newPassword, 256)
            WHERE id = @userId AND sifre = SHA2(@currentPassword, 256);", connection);
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
        await using var command = new MySqlCommand("UPDATE users SET iki_asamali_dogrulama_aktif_mi = @enabled WHERE id = @userId;", connection);
        command.Parameters.AddWithValue("@enabled", form.Enabled ? 1 : 0);
        command.Parameters.AddWithValue("@userId", userId);
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    public async Task<UserPaymentMethodsPageViewModel> GetPaymentMethodsAsync(long userId, CancellationToken cancellationToken = default)
    {
        var model = new UserPaymentMethodsPageViewModel();
        await using var connection = await OpenConnectionAsync(cancellationToken);

        await using (var command = new MySqlCommand(@"
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
                    DetailText = $"Son kullanim {reader.GetInt32(4):00}/{reader.GetInt32(5)}" + (!reader.IsDBNull(6) && reader.GetBoolean(6) ? " · Varsayilan kart" : string.Empty),
                    IsDefault = !reader.IsDBNull(6) && reader.GetBoolean(6)
                });
            }
        }

        await using var billingCommand = new MySqlCommand(@"
            SELECT ad_soyad, CONCAT_WS(', ', NULLIF(adres, ''), NULLIF(ilce, ''), NULLIF(sehir, '')) AS full_address, eposta
            FROM users WHERE id = @userId LIMIT 1;", connection);
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
            await using var clear = new MySqlCommand("UPDATE kullanici_odeme_yontemleri SET varsayilan_mi = 0 WHERE kullanici_id = @userId;", connection, transaction);
            clear.Parameters.AddWithValue("@userId", userId);
            await clear.ExecuteNonQueryAsync(cancellationToken);
        }

        await using var command = new MySqlCommand(@"
            INSERT INTO kullanici_odeme_yontemleri
            (kullanici_id, kart_etiketi, kart_sahibi, marka, son_dort_hane, son_kullanim_ay, son_kullanim_yil, varsayilan_mi, aktif_mi)
            VALUES
            (@userId, @label, @holder, @brand, @lastFour, @month, @year, @isDefault, 1);", connection, transaction);
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
        await using var command = new MySqlCommand("UPDATE kullanici_odeme_yontemleri SET aktif_mi = 0, varsayilan_mi = 0 WHERE id = @paymentMethodId AND kullanici_id = @userId;", connection);
        command.Parameters.AddWithValue("@paymentMethodId", paymentMethodId);
        command.Parameters.AddWithValue("@userId", userId);
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    private async Task<MySqlConnection> OpenConnectionAsync(CancellationToken cancellationToken)
    {
        var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }

    private async Task<List<UserReservationCardViewModel>> LoadReservationsAsync(MySqlConnection connection, long userId, int take, CancellationToken cancellationToken)
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
            LEFT JOIN otel_gorselleri og ON og.otel_id = o.id AND (og.kapak_fotografi_mi = 1 OR og.siralama = 1)
            WHERE r.kullanici_id = @userId
            ORDER BY r.giris_tarihi DESC, r.id DESC
            LIMIT @take;";
        await using var command = new MySqlCommand(sql, connection);
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
                GuestText = $"{reader.GetInt32(9)} Yetiskin" + (reader.GetInt32(10) > 0 ? $", {reader.GetInt32(10)} Cocuk" : string.Empty),
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
        MySqlConnection connection,
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
            LEFT JOIN otel_gorselleri og ON og.otel_id = o.id AND (og.kapak_fotografi_mi = 1 OR og.siralama = 1)
            WHERE r.kullanici_id = @userId
              AND (@statusFilter = 'all'
                   OR (@statusFilter = 'approved' AND r.durum = 'Onaylandı')
                   OR (@statusFilter = 'pending' AND (COALESCE(r.otel_onay_durumu, '') = 'Beklemede' OR r.durum = 'Onay Bekliyor')))
              AND (@startDate IS NULL OR DATE(r.giris_tarihi) >= @startDate)
              AND (@endDate IS NULL OR DATE(r.giris_tarihi) <= @endDate)
            ORDER BY r.giris_tarihi DESC, r.id DESC
            LIMIT @offset, @pageSize;";
        await using var command = new MySqlCommand(sql, connection);
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
                GuestText = $"{reader.GetInt32(9)} Yetiskin" + (reader.GetInt32(10) > 0 ? $", {reader.GetInt32(10)} Cocuk" : string.Empty),
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
        MySqlConnection connection,
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
              AND (@startDate IS NULL OR DATE(r.giris_tarihi) >= @startDate)
              AND (@endDate IS NULL OR DATE(r.giris_tarihi) <= @endDate);";
        await using var command = new MySqlCommand(sql, connection);
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

    private async Task<List<UserFavoriteSummaryViewModel>> LoadFavoriteSummariesAsync(MySqlConnection connection, long userId, int take, CancellationToken cancellationToken)
    {
        var list = new List<UserFavoriteSummaryViewModel>();
        const string sql = @"
            SELECT o.otel_adi, COALESCE(o.ilce, ''), COALESCE(o.sehir, ''), COALESCE(o.ortalama_puan, 0), COALESCE(og.gorsel_url, '')
            FROM user_favori_oteller uf
            INNER JOIN oteller o ON o.id = uf.otel_id
            LEFT JOIN otel_gorselleri og ON og.otel_id = o.id AND (og.kapak_fotografi_mi = 1 OR og.siralama = 1)
            WHERE uf.user_id = @userId AND uf.aktif_mi = 1
            ORDER BY uf.son_islem_tarihi DESC, uf.id DESC
            LIMIT @take;";
        await using var command = new MySqlCommand(sql, connection);
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

    private static int SafeInt(MySqlDataReader reader, int ordinal)
        => reader.IsDBNull(ordinal) ? 0 : Convert.ToInt32(reader.GetValue(ordinal), CultureInfo.InvariantCulture);

    private static decimal SafeDecimal(MySqlDataReader reader, int ordinal)
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

    private async Task<ReservationCancellationSnapshot?> LoadReservationCancellationSnapshotAsync(MySqlConnection connection, long userId, long reservationId, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT r.id,
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
              AND r.kullanici_id = @userId
            LIMIT 1;";
        await using var command = new MySqlCommand(sql, connection);
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

    private static async Task<(long UserId, string Email, string ManagerName)> ResolvePartnerRecipientAsync(MySqlConnection connection, long hotelId, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT COALESCE(o.user_id, oks.user_id, 1),
                   COALESCE(u.eposta, o.satis_kontak_eposta, o.eposta, 'partner@otelturizm.com'),
                   COALESCE(u.ad_soyad, o.satis_kontak_adi, 'Partner Yetkilisi')
            FROM oteller o
            LEFT JOIN otel_kullanici_sahiplikleri oks ON oks.otel_id = o.id AND oks.aktif_mi = 1
            LEFT JOIN users u ON u.id = COALESCE(o.user_id, oks.user_id)
            WHERE o.id = @hotelId
            ORDER BY oks.ana_sorumlu_mu DESC, oks.id ASC
            LIMIT 1;";
        await using var command = new MySqlCommand(sql, connection);
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
