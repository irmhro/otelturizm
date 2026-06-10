using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.SqlClient;
using SqlConnection = Microsoft.Data.SqlClient.SqlConnection;
using SqlCommand = Microsoft.Data.SqlClient.SqlCommand;
using SqlTransaction = Microsoft.Data.SqlClient.SqlTransaction;
using SqlException = Microsoft.Data.SqlClient.SqlException;
using otelturizmnew.Models.Email;
using otelturizmnew.Models.Messages;
using otelturizmnew.Models.Oteller;
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
    private readonly ISecureFileService _secureFileService;
    private readonly IHotelPointsService _hotelPointsService;
    private readonly IPaymentCardService _paymentCardService;
    private readonly string _publicBaseUrl;

    public UserPanelService(
        IConfiguration configuration,
        IMessageCenterService messageCenterService,
        IAddressLookupService addressLookupService,
        IEmailQueueService emailQueueService,
        IHotelPricingReadService hotelPricingReadService,
        ISecureFileService secureFileService,
        IHotelPointsService hotelPointsService,
        IPaymentCardService paymentCardService)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection tanimli degil.");
        _messageCenterService = messageCenterService;
        _addressLookupService = addressLookupService;
        _emailQueueService = emailQueueService;
        _hotelPricingReadService = hotelPricingReadService;
        _secureFileService = secureFileService;
        _hotelPointsService = hotelPointsService;
        _paymentCardService = paymentCardService;
        _publicBaseUrl = (configuration["App:PublicBaseUrl"] ?? "https://localhost:7223").TrimEnd('/');
    }

    public async Task<(int TotalReservations, int FavoriteCount, int MessageThreads)> GetNavBadgeCountsAsync(long userId, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        const string sql = @"
            SELECT
                (SELECT COUNT(*) FROM [dbo].[REZERVASYONLAR] WHERE [KULLANICI_ID] = @userId),
                (SELECT COUNT(*) FROM [dbo].[KULLANICI_FAVORI_OTELLER] WHERE [KULLANICI_ID] = @userId AND [AKTIF_MI] = 1),
                (SELECT COUNT(*) FROM [dbo].[MESAJ_KONUSMALARI] WHERE [MISAFIR_KULLANICI_ID] = @userId AND [DURUM] <> 'Arşivlendi');";
        await using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@userId", userId);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return (0, 0, 0);
        }

        return (SafeInt(reader, 0), SafeInt(reader, 1), SafeInt(reader, 2));
    }

    public async Task<(string TierName, int AvailablePoints)> GetLoyaltyTierChipAsync(long userId, CancellationToken cancellationToken = default)
    {
        if (userId <= 0)
        {
            return ("OtelPuan", 0);
        }

        await using var connection = await OpenConnectionAsync(cancellationToken);
        try
        {
            await EnsureLoyaltyAccountAsync(connection, userId, cancellationToken);
            await using var command = new SqlCommand(@"
                SELECT TOP (1) COALESCE(s.[AD], N'Bronz'), COALESCE(h.[KULLANILABILIR_PUAN], 0)
                FROM [dbo].[KULLANICI_SADAKAT_HESAPLARI] h
                LEFT JOIN [dbo].[SADAKAT_SEVIYELERI] s ON s.id = h.[MEVCUT_SEVIYE_ID]
                WHERE h.[KULLANICI_ID] = @userId;", connection);
            command.Parameters.AddWithValue("@userId", userId);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                return (reader.GetString(0), SafeInt(reader, 1));
            }
        }
        catch (SqlException)
        {
            // Sadakat tabloları eksikse statik chip göster.
        }

        return ("OtelPuan", 0);
    }

    public async Task<UserDashboardPageViewModel> GetDashboardAsync(
        long userId,
        string? reservationStatus = null,
        DateOnly? reservationStartDate = null,
        DateOnly? reservationEndDate = null,
        int reservationPage = 1,
        int reservationPageSize = 5,
        string? favoriteSort = null,
        CancellationToken cancellationToken = default)
    {
        var model = new UserDashboardPageViewModel();
        await using var connection = await OpenConnectionAsync(cancellationToken);
        var normalizedStatus = NormalizeDashboardReservationStatus(reservationStatus);
        var normalizedFavoriteSort = NormalizeDashboardFavoriteSort(favoriteSort);
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
                (SELECT COUNT(*) FROM [dbo].[REZERVASYONLAR] WHERE [KULLANICI_ID] = @userId) AS total_count,
                (SELECT COUNT(*) FROM [dbo].[REZERVASYONLAR] WHERE [KULLANICI_ID] = @userId AND [DURUM] <> 'İptal Edildi' AND [CIKIS_TARIHI] >= CAST(GETDATE() AS date)) AS upcoming_count,
                (SELECT COUNT(*) FROM [dbo].[KULLANICI_FAVORI_OTELLER] WHERE [KULLANICI_ID] = @userId AND [AKTIF_MI] = 1) AS favorite_count,
                (SELECT COUNT(*) FROM [dbo].[MESAJ_KONUSMALARI] WHERE [MISAFIR_KULLANICI_ID] = @userId AND [DURUM] <> 'Arşivlendi') AS message_count,
                (SELECT COALESCE(SUM([TOPLAM_TASARRUF]), 0) FROM [dbo].[REZERVASYONLAR] WHERE [KULLANICI_ID] = @userId) AS total_discount;";

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
        model.FavoriteSortFilter = normalizedFavoriteSort;
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
        model.FavoriteHotels = await LoadFavoriteSummariesAsync(connection, userId, 5, normalizedFavoriteSort, cancellationToken);
        return model;
    }

    public async Task<UserReservationsPageViewModel> GetReservationsAsync(
        long userId,
        string? statusFilter = null,
        DateOnly? startDate = null,
        DateOnly? endDate = null,
        int page = 1,
        int pageSize = 5,
        string? searchTerm = null,
        string? sort = null,
        CancellationToken cancellationToken = default)
    {
        var model = new UserReservationsPageViewModel();
        await using var connection = await OpenConnectionAsync(cancellationToken);
        var allReservations = await LoadReservationsAsync(connection, userId, 500, cancellationToken);
        model.TotalCount = allReservations.Count;
        model.UpcomingCount = allReservations.Count(x => x.IsUpcoming && !x.IsCancelled);
        model.PastCount = allReservations.Count(x => !x.IsUpcoming && !x.IsCancelled);
        model.CancelledCount = allReservations.Count(x => x.IsCancelled);

        var filteredList = FilterReservations(
            allReservations,
            statusFilter,
            startDate,
            endDate,
            searchTerm,
            sort,
            out var normalizedStatus,
            out var normalizedSort,
            out var search);

        var safePageSize = pageSize is 5 or 10 or 15 or 20 ? pageSize : 5;
        model.StatusFilter = normalizedStatus;
        model.StartDateText = startDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        model.EndDateText = endDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        model.PageSize = safePageSize;
        model.SearchTerm = search;
        model.SortFilter = normalizedSort;
        model.FilteredCount = filteredList.Count;
        var safePage = page <= 0 ? 1 : page;
        model.Page = Math.Min(safePage, Math.Max(1, model.TotalPages));
        model.Reservations = filteredList
            .Skip((model.Page - 1) * model.PageSize)
            .Take(model.PageSize)
            .ToList();
        return model;
    }

    public async Task<string> ExportReservationsCsvAsync(
        long userId,
        string? statusFilter = null,
        DateOnly? startDate = null,
        DateOnly? endDate = null,
        string? searchTerm = null,
        string? sort = null,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        var allReservations = await LoadReservationsAsync(connection, userId, 5000, cancellationToken);
        var filteredList = FilterReservations(
            allReservations,
            statusFilter,
            startDate,
            endDate,
            searchTerm,
            sort,
            out _,
            out _,
            out _);

        var csv = new StringBuilder();
        csv.AppendLine("Rezervasyon No,Otel,Sehir,Ilce,Oda,Giris,Cikis,Durum,Odeme,Tutar");
        foreach (var item in filteredList)
        {
            csv.Append(EscapeCsv(item.ReservationNo)).Append(',')
                .Append(EscapeCsv(item.HotelName)).Append(',')
                .Append(EscapeCsv(item.City)).Append(',')
                .Append(EscapeCsv(item.District)).Append(',')
                .Append(EscapeCsv(item.RoomName)).Append(',')
                .Append(EscapeCsv(item.CheckInDate.ToString("dd.MM.yyyy", CultureInfo.GetCultureInfo("tr-TR")))).Append(',')
                .Append(EscapeCsv(item.CheckOutDate.ToString("dd.MM.yyyy", CultureInfo.GetCultureInfo("tr-TR")))).Append(',')
                .Append(EscapeCsv(item.StatusText)).Append(',')
                .Append(EscapeCsv(item.PaymentStatus)).Append(',')
                .Append(EscapeCsv(item.TotalText)).AppendLine();
        }

        return csv.ToString();
    }

    private static List<UserReservationCardViewModel> FilterReservations(
        IReadOnlyList<UserReservationCardViewModel> allReservations,
        string? statusFilter,
        DateOnly? startDate,
        DateOnly? endDate,
        string? searchTerm,
        string? sort,
        out string normalizedStatus,
        out string normalizedSort,
        out string search)
    {
        normalizedStatus = NormalizeReservationStatusFilter(statusFilter);
        normalizedSort = NormalizeReservationSort(sort);
        search = (searchTerm ?? string.Empty).Trim();

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
        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchValue = search;
            filtered = filtered.Where(x =>
                x.ReservationNo.Contains(searchValue, StringComparison.OrdinalIgnoreCase)
                || x.HotelName.Contains(searchValue, StringComparison.OrdinalIgnoreCase)
                || x.RoomName.Contains(searchValue, StringComparison.OrdinalIgnoreCase)
                || x.City.Contains(searchValue, StringComparison.OrdinalIgnoreCase)
                || x.District.Contains(searchValue, StringComparison.OrdinalIgnoreCase));
        }

        filtered = normalizedSort switch
        {
            "oldest" => filtered.OrderBy(x => x.CheckInDate).ThenBy(x => x.ReservationId),
            "price_desc" => filtered.OrderByDescending(x => x.TotalAmount).ThenByDescending(x => x.CheckInDate),
            "price_asc" => filtered.OrderBy(x => x.TotalAmount).ThenByDescending(x => x.CheckInDate),
            _ => filtered.OrderByDescending(x => x.CheckInDate).ThenByDescending(x => x.ReservationId)
        };

        return filtered.ToList();
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
            SELECT TOP (1) [DURUM], [GIRIS_TARIHI]
            FROM [dbo].[REZERVASYONLAR]
            WHERE id = @reservationId AND [KULLANICI_ID] = @userId;";

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

        if (!IsWithinUserCancellationWindow(checkInDate.Value))
        {
            return (false, "Rezervasyon giriş saatinden (14:00) en az 24 saat önce iptal edilebilir. Girişe 24 saatten az kaldıysa Mesajlarım üzerinden otele ulaşabilirsiniz.");
        }

        const string updateSql = @"
            UPDATE [dbo].[REZERVASYONLAR]
            SET [DURUM] = 'İptal Edildi',
                [OTEL_ONAY_DURUMU] = 'Reddedildi',
                [IPTAL_NEDENI] = @reason,
                [IPTAL_EDEN] = 'Misafir',
                [IPTAL_TARIHI] = CURRENT_TIMESTAMP
            WHERE id = @reservationId AND [KULLANICI_ID] = @userId;";
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

    public async Task<(bool Success, string Message)> SaveReservationNoteAsync(long userId, UserReservationNoteForm form, CancellationToken cancellationToken = default)
    {
        if (userId <= 0 || form.ReservationId <= 0)
        {
            return (false, "Geçersiz talep.");
        }

        var note = (form.Note ?? string.Empty).Trim();
        if (note.Length > 2000)
        {
            note = note[..2000];
        }

        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = new SqlCommand("""
            UPDATE [dbo].[REZERVASYONLAR]
            SET [MUSTERI_TALEP_NOTU] = NULLIF(@note, ''),
                [GUNCELLENME_TARIHI] = SYSUTCDATETIME()
            WHERE id = @reservationId
              AND [KULLANICI_ID] = @userId;
            """, connection);
        command.Parameters.AddWithValue("@note", note);
        command.Parameters.AddWithValue("@reservationId", form.ReservationId);
        command.Parameters.AddWithValue("@userId", userId);
        var affected = await command.ExecuteNonQueryAsync(cancellationToken);
        return affected > 0 ? (true, "Rezervasyon notu güncellendi.") : (false, "Rezervasyon notu güncellenemedi.");
    }

    public async Task<UserReservationReviewPageViewModel?> GetReservationReviewPageAsync(long userId, long reservationId, CancellationToken cancellationToken = default)
    {
        if (userId <= 0 || reservationId <= 0)
        {
            return null;
        }

        await using var connection = await OpenConnectionAsync(cancellationToken);
        const string sql = @"
            SELECT TOP (1)
                r.id,
                r.[REZERVASYON_NO],
                r.[OTEL_ID],
                o.[OTEL_ADI],
                COALESCE(o.[ILCE], ''),
                COALESCE(o.[SEHIR], ''),
                COALESCE(ot.[ODA_ADI], 'Oda'),
                r.[GIRIS_TARIHI],
                r.[CIKIS_TARIHI],
                r.[DURUM],
                COALESCE(r.[OTEL_ONAY_DURUMU], ''),
                CAST(CASE WHEN EXISTS (
                    SELECT 1 FROM [dbo].[YORUMLAR] y WHERE y.[REZERVASYON_ID] = r.id AND y.[KULLANICI_ID] = @userId
                ) THEN 1 ELSE 0 END AS INT)
            FROM [dbo].[REZERVASYONLAR] r
            INNER JOIN [dbo].[OTELLER] o ON o.id = r.[OTEL_ID]
            LEFT JOIN [dbo].[ODA_TIPLERI] ot ON ot.id = r.[ODA_TIP_ID]
            WHERE r.id = @reservationId AND r.[KULLANICI_ID] = @userId;";
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@userId", userId);
        command.Parameters.AddWithValue("@reservationId", reservationId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        var checkIn = reader.GetDateTime(7);
        var checkOut = reader.GetDateTime(8);
        var status = reader.GetString(9);
        var otelOnay = reader.IsDBNull(10) ? string.Empty : reader.GetString(10);
        var hasReview = SafeInt(reader, 11) != 0;
        var isCancelled = string.Equals(status, "İptal Edildi", StringComparison.OrdinalIgnoreCase);
        var canSubmitReview = CanReservationReceiveReview(status, otelOnay, hasReview, isCancelled, checkOut);
        if (!canSubmitReview)
        {
            return null;
        }

        return new UserReservationReviewPageViewModel
        {
            ReservationId = reader.GetInt64(0),
            ReservationNo = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
            HotelId = reader.GetInt64(2),
            HotelName = reader.GetString(3),
            District = reader.GetString(4),
            City = reader.GetString(5),
            RoomName = reader.GetString(6),
            StayDateText = $"{checkIn:dd MMM yyyy} – {checkOut:dd MMM yyyy}",
            Form = new UserReservationReviewForm { ReservationId = reservationId }
        };
    }

    public async Task<(bool Success, string Message)> SubmitReservationReviewAsync(long userId, UserReservationReviewForm form, CancellationToken cancellationToken = default)
    {
        if (userId <= 0 || form.ReservationId <= 0)
        {
            return (false, "Gecersiz talep.");
        }

        var comment = (form.Comment ?? string.Empty).Trim();
        if (comment.Length < 20)
        {
            return (false, "Yorum metni en az 20 karakter olmalidir.");
        }

        if (form.PuanKonum is < 1 or > 10
            || form.PuanTemizlik is < 1 or > 10
            || form.PuanFiyat is < 1 or > 10
            || form.PuanPersonel is < 1 or > 10
            || form.PuanSessizlik is < 1 or > 10
            || form.PuanUlasim is < 1 or > 10)
        {
            return (false, "Puanlar 1 ile 10 arasinda olmalidir.");
        }

        if (form.SatisfactionLevel is < 1 or > 5)
        {
            return (false, "Genel memnuniyet seciminizi kontrol edin.");
        }

        var profile = (form.TravelProfile ?? string.Empty).Trim();
        if (!AllowedTravelProfiles.Contains(profile))
        {
            return (false, "Seyahat profili seciminizi kontrol edin.");
        }

        var page = await GetReservationReviewPageAsync(userId, form.ReservationId, cancellationToken);
        if (page is null)
        {
            return (false, "Bu rezervasyon icin yorum gonderemezsiniz (otel onayi, giris/tamamlanma durumu veya mevcut yorum).");
        }

        var genel10 = (byte)Math.Clamp(
            (int)Math.Round((form.PuanKonum + form.PuanTemizlik + form.PuanFiyat + form.PuanPersonel + form.PuanSessizlik + form.PuanUlasim) / 6.0, MidpointRounding.AwayFromZero),
            1,
            10);
        var genelLegacy = MapTenToLegacyFive(genel10);
        var temizlikLegacy = MapTenToLegacyFive(form.PuanTemizlik);
        var ulasimLegacy = MapTenToLegacyFive(form.PuanUlasim);

        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            const string insertSql = @"
                INSERT INTO [dbo].[YORUMLAR] (
                    [OTEL_ID], [KULLANICI_ID], [REZERVASYON_ID], [REZERVASYON_NO],
                    [GENEL_PUAN], [TEMIZLIK_PUANI], [KONFOR_PUANI], [KONUM_PUANI], [PERSONEL_PUANI], [FIYAT_PERFORMANS_PUANI],
                    [YORUM_METNI],
                    [ONAY_DURUMU], [ONAY_TARIHI], [DOGRULANMIS_KONAKLAMA], [ANONIM_MI],
                    [SEYAHAT_PROFILI], [MEMNUNIYET_SEVIYESI],
                    [GENEL_PUAN_10], [PUAN_ODA_10], [PUAN_KONUM_10], [PUAN_FIYAT_10], [PUAN_PERSONEL_10],
                    [PUAN_TEMIZLIK_10], [PUAN_SESSIZLIK_10], [PUAN_ULASIM_10],
                    [OLUSTURULMA_TARIHI]
                )
                VALUES (
                    @otelId, @userId, @rezId, @rezNo,
                    @genelLegacy, @temizLegacy, @konforLegacy, @konumLegacy, @personelLegacy, @fiyatLegacy,
                    @comment,
                    N'Onaylandı', SYSUTCDATETIME(), 1, @anon,
                    @profil, @memnuniyet,
                    @genel10, @oda10, @konum10, @fiyat10, @personel10, @temizlik10, @sessizlik10, @ulasim10,
                    SYSUTCDATETIME()
                );";

            await using (var cmd = new SqlCommand(insertSql, connection, (SqlTransaction)transaction))
            {
                cmd.Parameters.AddWithValue("@otelId", page.HotelId);
                cmd.Parameters.AddWithValue("@userId", userId);
                cmd.Parameters.AddWithValue("@rezId", form.ReservationId);
                cmd.Parameters.AddWithValue("@rezNo", page.ReservationNo);
                cmd.Parameters.AddWithValue("@genelLegacy", genelLegacy);
                cmd.Parameters.AddWithValue("@temizLegacy", temizlikLegacy);
                cmd.Parameters.AddWithValue("@konforLegacy", temizlikLegacy);
                cmd.Parameters.AddWithValue("@konumLegacy", ulasimLegacy);
                cmd.Parameters.AddWithValue("@personelLegacy", MapTenToLegacyFive(form.PuanPersonel));
                cmd.Parameters.AddWithValue("@fiyatLegacy", MapTenToLegacyFive(form.PuanFiyat));
                cmd.Parameters.AddWithValue("@comment", comment);
                cmd.Parameters.AddWithValue("@anon", form.Anonymous ? 1 : 0);
                cmd.Parameters.AddWithValue("@profil", profile);
                cmd.Parameters.AddWithValue("@memnuniyet", form.SatisfactionLevel);
                cmd.Parameters.AddWithValue("@genel10", genel10);
                cmd.Parameters.AddWithValue("@oda10", form.PuanTemizlik);
                cmd.Parameters.AddWithValue("@konum10", form.PuanKonum);
                cmd.Parameters.AddWithValue("@fiyat10", form.PuanFiyat);
                cmd.Parameters.AddWithValue("@personel10", form.PuanPersonel);
                cmd.Parameters.AddWithValue("@temizlik10", form.PuanTemizlik);
                cmd.Parameters.AddWithValue("@sessizlik10", form.PuanSessizlik);
                cmd.Parameters.AddWithValue("@ulasim10", form.PuanUlasim);
                await cmd.ExecuteNonQueryAsync(cancellationToken);
            }

            await RefreshHotelAggregatesFromApprovedReviewsAsync(connection, (SqlTransaction)transaction, page.HotelId, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            if (ex is SqlException sql)
            {
                return (false, sql.Number == 547
                    ? "Yorum kaydedilemedi. Veritabani kisitlamasi."
                    : "Yorum kaydedilemedi. Yorumlar rezervasyon no migration'i uygulanmis mi kontrol edin.");
            }

            throw;
        }

        return (true, "Yorumunuz kaydedildi ve yayina alindi. Tesekkur ederiz.");
    }

    public async Task<UserReviewsPageViewModel> GetReviewsAsync(long userId, string? statusFilter = null, string? searchTerm = null, int page = 1, CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        const int pageSize = 5;
        var normalizedStatus = (statusFilter ?? "all").Trim().ToLowerInvariant();
        var search = (searchTerm ?? string.Empty).Trim();
        if (normalizedStatus is not ("all" or "waiting" or "reviewed"))
        {
            normalizedStatus = "all";
        }

        var model = new UserReviewsPageViewModel
        {
            StatusFilter = normalizedStatus,
            SearchTerm = search,
            Page = page,
            PageSize = pageSize
        };

        await using var connection = await OpenConnectionAsync(cancellationToken);
        const string sql = @"
            SELECT
                r.id,
                COALESCE(r.[REZERVASYON_NO], ''),
                r.[OTEL_ID],
                o.[OTEL_ADI],
                o.[OTEL_KODU],
                COALESCE(o.[ILCE], ''),
                COALESCE(o.[SEHIR], ''),
                COALESCE(ot.[ODA_ADI], 'Oda'),
                r.[GIRIS_TARIHI],
                r.[CIKIS_TARIHI],
                r.[DURUM],
                COALESCE(r.[OTEL_ONAY_DURUMU], ''),
                y.id AS yorum_id,
                COALESCE(y.[YORUM_METNI], '') AS [YORUM_METNI],
                y.[OLUSTURULMA_TARIHI],
                y.[GUNCELLENME_TARIHI],
                COALESCE(y.[ONAY_DURUMU], '') AS yorum_durumu,
                COALESCE(CAST(y.[GENEL_PUAN_10] AS DECIMAL(9,2)), CAST(y.[GENEL_PUAN] AS DECIMAL(9,2)) * 2) AS yorum_puani,
                COALESCE(og.[GORSEL_URL], '')
            FROM [dbo].[REZERVASYONLAR] r
            INNER JOIN [dbo].[OTELLER] o ON o.id = r.[OTEL_ID]
            LEFT JOIN [dbo].[ODA_TIPLERI] ot ON ot.id = r.[ODA_TIP_ID]
            LEFT JOIN [dbo].[YORUMLAR] y ON y.[REZERVASYON_ID] = r.id AND y.[KULLANICI_ID] = @userId
            LEFT JOIN (
                SELECT ranked.[OTEL_ID], ranked.[GORSEL_URL]
                FROM (
                    SELECT
                        g.[OTEL_ID],
                        g.[GORSEL_URL],
                        ROW_NUMBER() OVER (
                            PARTITION BY g.[OTEL_ID]
                            ORDER BY COALESCE(g.[KAPAK_FOTOGRAFI_MI], 0) DESC, COALESCE(g.[SIRALAMA], 2147483647) ASC, g.id ASC
                        ) AS rn
                    FROM [dbo].[OTEL_GORSELLERI] g
                    WHERE COALESCE(g.[GORSEL_URL], '') <> ''
                ) ranked
                WHERE ranked.rn = 1
            ) og ON og.[OTEL_ID] = o.id
            WHERE r.[KULLANICI_ID] = @userId
            ORDER BY CASE WHEN y.id IS NULL THEN 0 ELSE 1 END, r.[GIRIS_TARIHI] DESC, r.id DESC;";

        var all = new List<UserReviewReservationRowViewModel>();
        await using (var command = new SqlCommand(sql, connection))
        {
            command.Parameters.AddWithValue("@userId", userId);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var status = reader.GetString(10);
                var otelOnay = reader.GetString(11);
                var isCancelled = string.Equals(status, "İptal Edildi", StringComparison.OrdinalIgnoreCase)
                                  || string.Equals(status, "Reddedildi", StringComparison.OrdinalIgnoreCase);
                var checkIn = reader.GetDateTime(8);
                var checkOut = reader.GetDateTime(9);
                var hasReview = !reader.IsDBNull(12);
                var canWriteReview = CanReservationReceiveReview(status, otelOnay, hasReview, isCancelled, checkOut);
                var createdAt = reader.IsDBNull(14) ? (DateTime?)null : reader.GetDateTime(14);
                var editLimit = createdAt?.AddDays(7);
                var canEdit = hasReview && editLimit.HasValue && DateTime.UtcNow <= editLimit.Value;
                var reviewTone = hasReview ? "reviewed" : canWriteReview ? "waiting" : "locked";
                var imageRaw = reader.IsDBNull(18) ? string.Empty : reader.GetString(18);
                all.Add(new UserReviewReservationRowViewModel
                {
                    ReservationId = reader.GetInt64(0),
                    ReservationNo = reader.GetString(1),
                    HotelId = reader.GetInt64(2),
                    HotelName = reader.GetString(3),
                    HotelSlug = BuildSlug(reader.GetString(3), reader.GetString(4)),
                    HotelImageUrl = string.IsNullOrWhiteSpace(imageRaw) ? null : imageRaw.Trim(),
                    District = reader.GetString(5),
                    City = reader.GetString(6),
                    RoomName = reader.GetString(7),
                    StayDateText = $"{checkIn:dd MMM yyyy} - {checkOut:dd MMM yyyy}",
                    ReservationStatusText = GetStandardReservationStatusText(status, otelOnay),
                    HasReview = hasReview,
                    CanWriteReview = canWriteReview,
                    CanEditReview = canEdit,
                    ReviewId = hasReview ? reader.GetInt64(12) : null,
                    ReviewText = reader.IsDBNull(13) ? string.Empty : reader.GetString(13),
                    ReviewDateText = createdAt.HasValue ? createdAt.Value.ToString("dd.MM.yyyy HH:mm", CultureInfo.GetCultureInfo("tr-TR")) : string.Empty,
                    EditLimitText = editLimit.HasValue ? editLimit.Value.ToString("dd.MM.yyyy HH:mm", CultureInfo.GetCultureInfo("tr-TR")) : string.Empty,
                    ReviewStatusText = hasReview ? "Yorumlandı" : canWriteReview ? "Yorum bekliyor" : "Yorum kapalı",
                    ReviewTone = reviewTone,
                    ReviewScore = reader.IsDBNull(17) ? 0 : decimal.Round(Convert.ToDecimal(reader.GetValue(17), CultureInfo.InvariantCulture), 1)
                });
            }
        }

        var eligible = all.Where(x => x.HasReview || x.CanWriteReview).ToList();
        model.WaitingReviewCount = eligible.Count(x => x.CanWriteReview);
        model.ReviewedCount = eligible.Count(x => x.HasReview);
        model.EligibleCount = eligible.Count;
        var filtered = normalizedStatus switch
        {
            "waiting" => eligible.Where(x => x.CanWriteReview).ToList(),
            "reviewed" => eligible.Where(x => x.HasReview).ToList(),
            _ => eligible
        };

        if (!string.IsNullOrWhiteSpace(search))
        {
            filtered = filtered
                .Where(x => x.HotelName.Contains(search, StringComparison.OrdinalIgnoreCase)
                            || x.District.Contains(search, StringComparison.OrdinalIgnoreCase)
                            || x.City.Contains(search, StringComparison.OrdinalIgnoreCase)
                            || x.ReservationNo.Contains(search, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        model.TotalCount = filtered.Count;
        model.Items = filtered.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return model;
    }

    public async Task<bool> CanUserWriteReviewForReservationAsync(long userId, long reservationId, CancellationToken cancellationToken = default)
    {
        if (userId <= 0 || reservationId <= 0)
        {
            return false;
        }

        await using var connection = await OpenConnectionAsync(cancellationToken);
        const string sql = """
            SELECT r.[DURUM],
                   COALESCE(r.[OTEL_ONAY_DURUMU], ''),
                   CAST(CASE WHEN EXISTS (
                       SELECT 1 FROM [dbo].[YORUMLAR] y WHERE y.[REZERVASYON_ID] = r.id AND y.[KULLANICI_ID] = @userId
                   ) THEN 1 ELSE 0 END AS INT),
                   r.[CIKIS_TARIHI]
            FROM [dbo].[REZERVASYONLAR] r
            WHERE r.id = @reservationId AND r.[KULLANICI_ID] = @userId;
            """;
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@reservationId", reservationId);
        command.Parameters.AddWithValue("@userId", userId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return false;
        }

        var status = reader.GetString(0);
        var otelOnay = reader.GetString(1);
        var hasReview = Convert.ToInt32(reader.GetValue(2), CultureInfo.InvariantCulture) != 0;
        var checkOut = reader.GetDateTime(3);
        var isCancelled = string.Equals(status, "İptal Edildi", StringComparison.OrdinalIgnoreCase)
                          || string.Equals(status, "Reddedildi", StringComparison.OrdinalIgnoreCase);
        return CanReservationReceiveReview(status, otelOnay, hasReview, isCancelled, checkOut);
    }

    public async Task<IReadOnlyList<HotelEligibleReviewStayViewModel>> GetEligibleReviewStaysForHotelAsync(
        long userId,
        long hotelId,
        CancellationToken cancellationToken = default)
    {
        if (userId <= 0 || hotelId <= 0)
        {
            return Array.Empty<HotelEligibleReviewStayViewModel>();
        }

        var list = new List<HotelEligibleReviewStayViewModel>();
        await using var connection = await OpenConnectionAsync(cancellationToken);
        const string sql = """
            SELECT r.id,
                   r.[GIRIS_TARIHI],
                   r.[CIKIS_TARIHI],
                   COALESCE(ot.[ODA_ADI], N'Oda')
            FROM [dbo].[REZERVASYONLAR] r
            LEFT JOIN [dbo].[ODA_TIPLERI] ot ON ot.id = r.[ODA_TIP_ID]
            LEFT JOIN [dbo].[YORUMLAR] y ON y.[REZERVASYON_ID] = r.id AND y.[KULLANICI_ID] = @userId
            WHERE r.[KULLANICI_ID] = @userId
              AND r.[OTEL_ID] = @hotelId
              AND y.id IS NULL
              AND r.[DURUM] NOT IN (N'İptal Edildi', N'Reddedildi')
              AND CAST(r.[CIKIS_TARIHI] AS date) < CAST(SYSUTCDATETIME() AS date)
              AND r.[DURUM] IN (N'Tamamlandı', N'Giriş Yaptı', N'Onaylandı')
              AND (
                  COALESCE(r.[OTEL_ONAY_DURUMU], '') = N'Onaylandı'
                  OR r.[DURUM] IN (N'Tamamlandı', N'Giriş Yaptı', N'Onaylandı')
              )
            ORDER BY r.[CIKIS_TARIHI] DESC, r.id DESC;
            """;
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@userId", userId);
        command.Parameters.AddWithValue("@hotelId", hotelId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var culture = CultureInfo.GetCultureInfo("tr-TR");
        while (await reader.ReadAsync(cancellationToken))
        {
            var checkIn = reader.GetDateTime(1);
            var checkOut = reader.GetDateTime(2);
            list.Add(new HotelEligibleReviewStayViewModel
            {
                ReservationId = reader.GetInt64(0),
                StayDateText = $"{checkIn.ToString("dd MMM yyyy", culture)} - {checkOut.ToString("dd MMM yyyy", culture)}",
                RoomName = reader.GetString(3)
            });
        }

        return list;
    }

    public async Task<(bool Success, string Message)> UpdateReviewAsync(long userId, UserReviewUpdateForm form, CancellationToken cancellationToken = default)
    {
        var comment = (form.Comment ?? string.Empty).Trim();
        if (userId <= 0 || form.ReviewId <= 0 || comment.Length < 20)
        {
            return (false, "Yorum metni en az 20 karakter olmalidir.");
        }

        await using var connection = await OpenConnectionAsync(cancellationToken);
        const string sql = @"
            UPDATE [dbo].[YORUMLAR]
            SET [YORUM_METNI] = @comment,
                [GUNCELLENME_TARIHI] = SYSUTCDATETIME()
            WHERE id = @reviewId
              AND [KULLANICI_ID] = @userId
              AND [OLUSTURULMA_TARIHI] >= DATEADD(DAY, -7, SYSUTCDATETIME());";
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@comment", comment);
        command.Parameters.AddWithValue("@reviewId", form.ReviewId);
        command.Parameters.AddWithValue("@userId", userId);
        var affected = await command.ExecuteNonQueryAsync(cancellationToken);
        return affected > 0
            ? (true, "Yorumunuz guncellendi.")
            : (false, "Yorum guncellenemedi. 7 gunluk duzenleme suresi dolmus olabilir.");
    }

    public async Task<(bool Success, string Message)> DeleteReviewAsync(long userId, UserReviewDeleteForm form, CancellationToken cancellationToken = default)
    {
        if (userId <= 0 || form.ReviewId <= 0)
        {
            return (false, "Gecersiz talep.");
        }

        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            long? hotelId = null;
            const string lookupSql = @"
                SELECT [OTEL_ID]
                FROM [dbo].[YORUMLAR]
                WHERE id = @reviewId
                  AND [KULLANICI_ID] = @userId
                  AND [OLUSTURULMA_TARIHI] >= DATEADD(DAY, -7, SYSUTCDATETIME());";
            await using (var lookup = new SqlCommand(lookupSql, connection, (SqlTransaction)transaction))
            {
                lookup.Parameters.AddWithValue("@reviewId", form.ReviewId);
                lookup.Parameters.AddWithValue("@userId", userId);
                var value = await lookup.ExecuteScalarAsync(cancellationToken);
                if (value is not null && value is not DBNull)
                {
                    hotelId = Convert.ToInt64(value, CultureInfo.InvariantCulture);
                }
            }

            if (!hotelId.HasValue)
            {
                await transaction.RollbackAsync(cancellationToken);
                return (false, "Yorum silinemedi. 7 gunluk silme suresi dolmus olabilir.");
            }

            await using (var delete = new SqlCommand("DELETE FROM [dbo].[YORUMLAR] WHERE id = @reviewId AND [KULLANICI_ID] = @userId;", connection, (SqlTransaction)transaction))
            {
                delete.Parameters.AddWithValue("@reviewId", form.ReviewId);
                delete.Parameters.AddWithValue("@userId", userId);
                await delete.ExecuteNonQueryAsync(cancellationToken);
            }

            await RefreshHotelAggregatesFromApprovedReviewsAsync(connection, (SqlTransaction)transaction, hotelId.Value, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return (true, "Yorumunuz silindi.");
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static readonly HashSet<string> AllowedTravelProfiles = new(StringComparer.OrdinalIgnoreCase)
    {
        "Aileler",
        "Çiftler",
        "Arkadaş grubu",
        "Yalnız gezgin",
        "İş seyahati"
    };

    private static byte MapTenToLegacyFive(int ten) =>
        (byte)Math.Clamp((int)Math.Round(ten / 2.0, MidpointRounding.AwayFromZero), 1, 5);

    private static bool CanReservationReceiveReview(string status, string otelOnay, bool hasReview, bool isCancelled, DateTime checkOut)
    {
        if (isCancelled || hasReview)
        {
            return false;
        }

        if (checkOut.Date >= DateTime.UtcNow.Date)
        {
            return false;
        }

        var statusAllowsReview = string.Equals(status, "Tamamlandı", StringComparison.OrdinalIgnoreCase)
                                 || string.Equals(status, "Giriş Yaptı", StringComparison.OrdinalIgnoreCase)
                                 || string.Equals(status, "Onaylandı", StringComparison.OrdinalIgnoreCase);
        var partnerApproved = string.Equals(otelOnay, "Onaylandı", StringComparison.OrdinalIgnoreCase) || statusAllowsReview;
        return partnerApproved && statusAllowsReview;
    }

    private static async Task RefreshHotelAggregatesFromApprovedReviewsAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        long hotelId,
        CancellationToken cancellationToken)
    {
        const string sql = @"
;WITH agg AS (
    SELECT
        y.[OTEL_ID],
        COUNT(*) AS cnt,
        CAST(ROUND(AVG(CAST(COALESCE(CAST(y.[GENEL_PUAN_10] AS DECIMAL(9, 4)),
            CASE
                WHEN y.[GENEL_PUAN] <= 5 THEN CAST(y.[GENEL_PUAN] AS DECIMAL(9, 4)) * 2
                WHEN y.[GENEL_PUAN] <= 10 THEN CAST(y.[GENEL_PUAN] AS DECIMAL(9, 4))
                ELSE 10
            END) AS DECIMAL(9, 4))), 2) AS DECIMAL(5, 2)) AS avg_genel,
        CAST(ROUND(AVG(CAST(COALESCE(CAST(y.[PUAN_KONUM_10] AS DECIMAL(9, 4)),
            CASE
                WHEN y.[KONUM_PUANI] <= 5 THEN CAST(y.[KONUM_PUANI] AS DECIMAL(9, 4)) * 2
                WHEN y.[KONUM_PUANI] <= 10 THEN CAST(y.[KONUM_PUANI] AS DECIMAL(9, 4))
                ELSE 10
            END) AS DECIMAL(9, 4))), 2) AS DECIMAL(5, 2)) AS avg_konum,
        CAST(ROUND(AVG(CAST(COALESCE(CAST(y.[PUAN_TEMIZLIK_10] AS DECIMAL(9, 4)), CAST(y.[PUAN_ODA_10] AS DECIMAL(9, 4)),
            CASE
                WHEN y.[TEMIZLIK_PUANI] <= 5 THEN CAST(y.[TEMIZLIK_PUANI] AS DECIMAL(9, 4)) * 2
                WHEN y.[TEMIZLIK_PUANI] <= 10 THEN CAST(y.[TEMIZLIK_PUANI] AS DECIMAL(9, 4))
                WHEN y.[KONFOR_PUANI] <= 5 THEN CAST(y.[KONFOR_PUANI] AS DECIMAL(9, 4)) * 2
                WHEN y.[KONFOR_PUANI] <= 10 THEN CAST(y.[KONFOR_PUANI] AS DECIMAL(9, 4))
                ELSE 10
            END) AS DECIMAL(9, 4))), 2) AS DECIMAL(5, 2)) AS avg_konfor,
        CAST(ROUND(AVG(CAST(COALESCE(CAST(y.[PUAN_FIYAT_10] AS DECIMAL(9, 4)),
            CASE
                WHEN y.[FIYAT_PERFORMANS_PUANI] <= 5 THEN CAST(y.[FIYAT_PERFORMANS_PUANI] AS DECIMAL(9, 4)) * 2
                WHEN y.[FIYAT_PERFORMANS_PUANI] <= 10 THEN CAST(y.[FIYAT_PERFORMANS_PUANI] AS DECIMAL(9, 4))
                ELSE 10
            END) AS DECIMAL(9, 4))), 2) AS DECIMAL(5, 2)) AS avg_fp
    FROM [dbo].[YORUMLAR] AS y
    WHERE y.[OTEL_ID] = @hotelId
      AND y.[ONAY_DURUMU] LIKE N'Onaylan%'
    GROUP BY y.[OTEL_ID]
)
UPDATE o
SET
    o.[TOPLAM_YORUM_SAYISI] = agg.cnt,
    o.[ORTALAMA_PUAN] = agg.avg_genel,
    o.[KONUM_PUANI] = agg.avg_konum,
    o.[KONFOR_PUANI] = agg.avg_konfor,
    o.[FIYAT_PERFORMANS_PUANI] = agg.avg_fp
FROM [dbo].[OTELLER] AS o
INNER JOIN agg ON agg.[OTEL_ID] = o.id;";
        await using var cmd = new SqlCommand(sql, connection, transaction);
        cmd.Parameters.AddWithValue("@hotelId", hotelId);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
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
        var hasGeoIds = await ColumnExistsAsync(connection, "KULLANICILAR", "ULKE_ID", cancellationToken);
        var sql = hasGeoIds
            ? @"
            SELECT TOP (1) [AD_SOYAD], [EPOSTA], COALESCE([TELEFON], ''), [TC_KIMLIK_NO], [DOGUM_TARIHI], [CINSIYET], [UYRUK], [ADRES], [SEHIR], ilce, [MAHALLE], [POSTA_KODU],
                   [TERCIH_EDILEN_ODA_TIPI], [YATAK_TERCIHI], [KONUSULAN_DILLER], [SEYAHAT_AMACI], [OZEL_ISTEKLER],
                   [EPOSTA_DOGRULAMA_TARIHI],
                   COALESCE(NULLIF([PROFIL_RESIM_URL], ''), '') AS [PROFIL_RESIM_URL],
                   [ULKE_ID], [IL_ID], [ILCE_ID], [MAHALLE_ID]
            FROM [dbo].[KULLANICILAR]
            WHERE id = @userId;"
            : @"
            SELECT TOP (1) [AD_SOYAD], [EPOSTA], COALESCE([TELEFON], ''), [TC_KIMLIK_NO], [DOGUM_TARIHI], [CINSIYET], [UYRUK], [ADRES], [SEHIR], ilce, [MAHALLE], [POSTA_KODU],
                   [TERCIH_EDILEN_ODA_TIPI], [YATAK_TERCIHI], [KONUSULAN_DILLER], [SEYAHAT_AMACI], [OZEL_ISTEKLER],
                   [EPOSTA_DOGRULAMA_TARIHI],
                   COALESCE(NULLIF([PROFIL_RESIM_URL], ''), '') AS [PROFIL_RESIM_URL]
            FROM [dbo].[KULLANICILAR]
            WHERE id = @userId;";

        var model = new UserProfilePageViewModel();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@userId", userId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            var name = SplitName(reader.GetString(0));
            var emailVerifiedAt = reader.IsDBNull(17) ? (DateTime?)null : reader.GetDateTime(17);
            var profileImageUrl = reader.IsDBNull(18) ? string.Empty : reader.GetString(18);
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
            if (hasGeoIds)
            {
                model.Form.UlkeId = reader.IsDBNull(19) ? null : reader.GetInt64(19);
                model.Form.IlId = reader.IsDBNull(20) ? null : reader.GetInt64(20);
                model.Form.IlceId = reader.IsDBNull(21) ? null : reader.GetInt64(21);
                model.Form.MahalleId = reader.IsDBNull(22) ? null : reader.GetInt64(22);
            }

            model.EmailVerified = emailVerifiedAt.HasValue;
            model.EmailVerifiedAtText = emailVerifiedAt.HasValue
                ? emailVerifiedAt.Value.ToLocalTime().ToString("dd.MM.yyyy HH:mm", CultureInfo.GetCultureInfo("tr-TR"))
                : "Onay bekliyor";

            model.ProfileImageUrl = await ResolveProfileImageUrlAsync(userId, profileImageUrl, cancellationToken);
        }

        model.Countries = (await _addressLookupService.GetCountriesAsync(cancellationToken)).ToList();
        model.PresetAvatarUrls = BuildPresetAvatarUrls();
        model.UploadedProfileAvatars = await LoadUploadedProfileAvatarsAsync(connection, userId, cancellationToken);
        model.RoomPreferenceOptions = BuildRoomPreferenceOptions();
        model.BedPreferenceOptions = BuildBedPreferenceOptions();
        model.SpokenLanguageOptions = BuildSpokenLanguageOptions();
        model.TravelPurposeOptions = BuildTravelPurposeOptions();
        if (hasGeoIds && model.Form.IlId is > 0)
        {
            model.SelectedCountryId = model.Form.UlkeId ?? model.SelectedCountryId;
            model.SelectedProvinceId = model.Form.IlId;
            model.SelectedDistrictId = model.Form.IlceId;
            model.SelectedNeighborhoodId = model.Form.MahalleId;
        }
        else
        {
            var selection = await _addressLookupService.ResolveSelectionAsync(model.Form.City, model.Form.District, model.Form.Neighborhood, model.Form.Nationality, cancellationToken);
            if (selection is not null)
            {
                model.SelectedCountryId = selection.CountryId;
                model.SelectedProvinceId = selection.ProvinceId;
                model.SelectedDistrictId = selection.DistrictId;
                model.SelectedNeighborhoodId = selection.NeighborhoodId;
            }
        }

        if (model.SelectedCountryId is > 0)
        {
            model.Provinces = (await _addressLookupService.GetProvincesAsync(model.SelectedCountryId.Value, cancellationToken)).ToList();
        }
        else
        {
            var defaultCountry = model.Countries.FirstOrDefault(c => c.Iso2.Equals("TR", StringComparison.OrdinalIgnoreCase))
                ?? model.Countries.FirstOrDefault();
            if (defaultCountry is not null)
            {
                model.SelectedCountryId ??= defaultCountry.Id;
                model.Provinces = (await _addressLookupService.GetProvincesAsync(defaultCountry.Id, cancellationToken)).ToList();
            }
            else
            {
                model.Provinces = new List<AddressProvinceOption>();
            }
        }

        return model;
    }

    public async Task<string> GetProfileImageUrlAsync(long userId, CancellationToken cancellationToken = default)
    {
        if (userId <= 0)
        {
            return "/uploads/demo/avatars/avatar-01.svg";
        }

        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = new SqlCommand("""
            SELECT TOP (1) COALESCE(NULLIF([PROFIL_RESIM_URL], ''), '')
            FROM [dbo].[KULLANICILAR]
            WHERE id = @userId;
            """, connection);
        command.Parameters.AddWithValue("@userId", userId);
        var raw = Convert.ToString(await command.ExecuteScalarAsync(cancellationToken), CultureInfo.InvariantCulture) ?? string.Empty;
        return await ResolveProfileImageUrlAsync(userId, raw, cancellationToken);
    }

    private async Task<string> ResolveProfileImageUrlAsync(long userId, string? storedValue, CancellationToken cancellationToken)
    {
        var normalized = (storedValue ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return "/uploads/demo/avatars/avatar-01.svg";
        }

        if (normalized.StartsWith("secure:", StringComparison.OrdinalIgnoreCase)
            && long.TryParse(normalized["secure:".Length..], NumberStyles.Integer, CultureInfo.InvariantCulture, out var fileId)
            && fileId > 0)
        {
            return await _secureFileService.CreateAccessUrlAsync(fileId, userId, "user", cancellationToken);
        }

        return normalized.StartsWith("/", StringComparison.Ordinal) ? normalized : "/uploads/demo/avatars/avatar-01.svg";
    }

    private static List<string> BuildPresetAvatarUrls()
        => new()
        {
            "/uploads/demo/avatars/avatar-01.svg",
            "/uploads/demo/avatars/avatar-02.svg",
            "/uploads/demo/avatars/avatar-03.svg",
            "/uploads/demo/avatars/avatar-04.svg",
            "/uploads/demo/avatars/avatar-05.svg",
            "/uploads/demo/avatars/avatar-06.svg",
            "/uploads/demo/avatars/avatar-07.svg",
            "/uploads/demo/avatars/avatar-08.svg",
            "/uploads/demo/avatars/avatar-09.svg",
            "/uploads/demo/avatars/avatar-10.svg"
        };

    private static List<string> BuildRoomPreferenceOptions()
        => new() { "Fark etmez", "Standart oda", "Deluxe oda", "Suit oda", "Aile odası", "Sessiz kat", "Sigara içilmeyen oda" };

    private static List<string> BuildBedPreferenceOptions()
        => new() { "Fark etmez", "Tek büyük yatak", "İki ayrı yatak", "Çift kişilik yatak", "Aile yatağı", "Ek yatak uygun olsun" };

    private static List<string> BuildSpokenLanguageOptions()
        => new() { "Türkçe", "İngilizce", "Almanca", "Fransızca", "Arapça", "Rusça" };

    private static List<string> BuildTravelPurposeOptions()
        => new() { "İş", "Tatil", "Aile ziyareti", "Sağlık", "Etkinlik", "Karışık" };

    private async Task<List<UserUploadedProfileAvatarViewModel>> LoadUploadedProfileAvatarsAsync(SqlConnection connection, long userId, CancellationToken cancellationToken)
    {
        var list = new List<UserUploadedProfileAvatarViewModel>();
        await using var command = new SqlCommand("""
            SELECT TOP (3)
                   g.id,
                   COALESCE(g.[ORIJINAL_DOSYA_ADI], 'Profil görseli') AS original_name,
                   g.[OLUSTURULMA_TARIHI],
                   CASE WHEN u.[PROFIL_RESIM_URL] = CONCAT('secure:', CONVERT(varchar(30), g.id)) THEN 1 ELSE 0 END AS is_current
            FROM [dbo].[GUVENLI_DOSYA_VARLIKLARI] g
            INNER JOIN [dbo].[KULLANICILAR] u ON u.id = @userId
            WHERE g.[SAHIBI_KULLANICI_ID] = @userId
              AND g.[KATEGORI] = N'profile'
              AND g.[GORSEL_MI] = 1
            ORDER BY g.id DESC;
            """, connection);
        command.Parameters.AddWithValue("@userId", userId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var fileId = Convert.ToInt64(reader.GetValue(0), CultureInfo.InvariantCulture);
            list.Add(new UserUploadedProfileAvatarViewModel
            {
                FileId = fileId,
                OriginalFileName = reader.IsDBNull(1) ? "Profil görseli" : reader.GetString(1),
                UploadedAtText = reader.IsDBNull(2) ? string.Empty : reader.GetDateTime(2).ToLocalTime().ToString("dd.MM.yyyy HH:mm", CultureInfo.GetCultureInfo("tr-TR")),
                IsCurrent = SafeInt(reader, 3) == 1,
                AccessUrl = await _secureFileService.CreateAccessUrlAsync(fileId, userId, "user", cancellationToken)
            });
        }

        return list;
    }

    public async Task<bool> SaveProfileAsync(long userId, UserProfileForm form, CancellationToken cancellationToken = default)
    {
        var email = form.Email?.Trim() ?? string.Empty;
        var phone = form.Phone?.Trim() ?? string.Empty;
        var normalizedPhone = PhoneVerificationService.NormalizePhoneNumber(phone);
        await using var connection = await OpenConnectionAsync(cancellationToken);

        string existingEmail = string.Empty;
        var emailVerified = false;
        await using (var emailSnapshotCommand = new SqlCommand("""
            SELECT TOP (1) COALESCE([EPOSTA], ''), [EPOSTA_DOGRULAMA_TARIHI]
            FROM [dbo].[KULLANICILAR]
            WHERE id = @userId;
            """, connection))
        {
            emailSnapshotCommand.Parameters.AddWithValue("@userId", userId);
            await using var snapshotReader = await emailSnapshotCommand.ExecuteReaderAsync(cancellationToken);
            if (await snapshotReader.ReadAsync(cancellationToken))
            {
                existingEmail = snapshotReader.IsDBNull(0) ? string.Empty : snapshotReader.GetString(0);
                emailVerified = !snapshotReader.IsDBNull(1);
            }
            else
            {
                return false;
            }
        }

        if (emailVerified)
        {
            email = existingEmail;
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            return false;
        }

        await using (var duplicateCheckCommand = new SqlCommand("SELECT COUNT(*) FROM [dbo].[KULLANICILAR] WHERE [EPOSTA] = @email AND id <> @userId;", connection))
        {
            duplicateCheckCommand.Parameters.AddWithValue("@email", email);
            duplicateCheckCommand.Parameters.AddWithValue("@userId", userId);
            var duplicateCount = Convert.ToInt32(await duplicateCheckCommand.ExecuteScalarAsync(cancellationToken) ?? 0, CultureInfo.InvariantCulture);
            if (duplicateCount > 0)
            {
                return false;
            }
        }

        string existingPhone = string.Empty;
        string existingPhoneE164 = string.Empty;
        await using (var snapshotCommand = new SqlCommand("SELECT TOP (1) COALESCE([TELEFON], ''), COALESCE([TELEFON_E164], '') FROM [dbo].[KULLANICILAR] WHERE id = @userId;", connection))
        {
            snapshotCommand.Parameters.AddWithValue("@userId", userId);
            await using var snapshotReader = await snapshotCommand.ExecuteReaderAsync(cancellationToken);
            if (await snapshotReader.ReadAsync(cancellationToken))
            {
                existingPhone = snapshotReader.IsDBNull(0) ? string.Empty : snapshotReader.GetString(0);
                existingPhoneE164 = snapshotReader.IsDBNull(1) ? string.Empty : snapshotReader.GetString(1);
            }
        }

        var phoneChanged = !string.Equals(existingPhone, phone, StringComparison.Ordinal)
            || !string.Equals(existingPhoneE164, normalizedPhone ?? string.Empty, StringComparison.OrdinalIgnoreCase);

        var hasGeoIds = await ColumnExistsAsync(connection, "KULLANICILAR", "ULKE_ID", cancellationToken);
        if (hasGeoIds)
        {
            await SyncGeoTextFromIdsAsync(connection, form, cancellationToken);
        }

        var geoSetSql = hasGeoIds
            ? @",
                    [ULKE_ID] = @ulkeId,
                    [IL_ID] = @ilId,
                    [ILCE_ID] = @ilceId,
                    [MAHALLE_ID] = @mahalleId"
            : string.Empty;

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            await using var command = new SqlCommand($@"
                UPDATE [dbo].[KULLANICILAR]
                SET [AD_SOYAD] = @fullName,
                    [EPOSTA] = @email,
                    [TELEFON] = NULLIF(@phone, ''),
                    [TELEFON_E164] = NULLIF(@phoneE164, ''),
                    [TELEFON_DOGRULAMA_KANALI] = CASE WHEN NULLIF(@phoneE164, '') IS NULL THEN NULL ELSE 'whatsapp' END,
                    [TELEFON_DOGRULAMA_DURUMU] = CASE
                        WHEN NULLIF(@phoneE164, '') IS NULL THEN NULL
                        WHEN @phoneChanged = 1 THEN 'Dogrulanmadi'
                        ELSE [TELEFON_DOGRULAMA_DURUMU]
                    END,
                    [TELEFON_DEGISTIRILME_TARIHI] = CASE WHEN @phoneChanged = 1 THEN SYSUTCDATETIME() ELSE [TELEFON_DEGISTIRILME_TARIHI] END,
                    [TELEFON_DOGRULAMA_TARIHI] = CASE WHEN @phoneChanged = 1 THEN NULL ELSE [TELEFON_DOGRULAMA_TARIHI] END,
                    [TELEFON_SON_SAHIPLIK_TEYIT_TARIHI] = CASE WHEN @phoneChanged = 1 THEN NULL ELSE [TELEFON_SON_SAHIPLIK_TEYIT_TARIHI] END,
                    [TC_KIMLIK_NO] = NULLIF(@identityNumber, ''),
                    [DOGUM_TARIHI] = @birthDate,
                    [CINSIYET] = NULLIF(@gender, ''),
                    [UYRUK] = NULLIF(@nationality, ''),
                    [ADRES] = NULLIF(@address, ''),
                    [SEHIR] = NULLIF(@city, ''),
                    ilce = NULLIF(@district, ''),
                    [MAHALLE] = NULLIF(@neighborhood, ''),
                    [POSTA_KODU] = NULLIF(@postalCode, ''),
                    [TERCIH_EDILEN_ODA_TIPI] = NULLIF(@roomPreference, ''),
                    [YATAK_TERCIHI] = NULLIF(@bedPreference, ''),
                    [KONUSULAN_DILLER] = NULLIF(@spokenLanguages, ''),
                    [SEYAHAT_AMACI] = NULLIF(@travelPurpose, ''),
                    [OZEL_ISTEKLER] = NULLIF(@specialRequests, ''){geoSetSql},
                    [PROFIL_TAMAMLANMA_TARIHI] = CURRENT_TIMESTAMP,
                    [GUNCELLENME_TARIHI] = SYSUTCDATETIME()
                WHERE id = @userId;", connection, (SqlTransaction)transaction);
            command.Parameters.AddWithValue("@fullName", $"{form.FirstName} {form.LastName}".Trim());
            command.Parameters.AddWithValue("@email", email);
            command.Parameters.AddWithValue("@phone", phone);
            command.Parameters.AddWithValue("@phoneE164", string.IsNullOrWhiteSpace(normalizedPhone) ? DBNull.Value : (object)normalizedPhone);
            command.Parameters.AddWithValue("@phoneChanged", phoneChanged ? 1 : 0);
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
            if (hasGeoIds)
            {
                command.Parameters.AddWithValue("@ulkeId", form.UlkeId.HasValue ? form.UlkeId.Value : DBNull.Value);
                command.Parameters.AddWithValue("@ilId", form.IlId.HasValue ? form.IlId.Value : DBNull.Value);
                command.Parameters.AddWithValue("@ilceId", form.IlceId.HasValue ? form.IlceId.Value : DBNull.Value);
                command.Parameters.AddWithValue("@mahalleId", form.MahalleId.HasValue ? form.MahalleId.Value : DBNull.Value);
            }

            command.Parameters.AddWithValue("@userId", userId);
            var affected = await command.ExecuteNonQueryAsync(cancellationToken) > 0;

            if (affected && phoneChanged && (!string.IsNullOrWhiteSpace(existingPhone) || !string.IsNullOrWhiteSpace(existingPhoneE164)))
            {
                await using var historyCommand = new SqlCommand(@"
                    INSERT INTO [dbo].[KULLANICI_TELEFON_GECMISI]
                    (
                        [KULLANICI_ID], [ONCEKI_TELEFON_RAW], [ONCEKI_TELEFON_E164], [YENI_TELEFON_RAW], [YENI_TELEFON_E164],
                        [DOGRULAMA_DURUMU], [DEGISIM_NEDENI], [OLUSTURULMA_TARIHI]
                    )
                    VALUES
                    (
                        @userId, NULLIF(@oldPhone, ''), NULLIF(@oldPhoneE164, ''), NULLIF(@newPhone, ''), NULLIF(@newPhoneE164, ''),
                        @status, N'Profil ekranından [TELEFON] güncellendi', SYSUTCDATETIME()
                    );", connection, (SqlTransaction)transaction);
                historyCommand.Parameters.AddWithValue("@userId", userId);
                historyCommand.Parameters.AddWithValue("@oldPhone", existingPhone);
                historyCommand.Parameters.AddWithValue("@oldPhoneE164", existingPhoneE164);
                historyCommand.Parameters.AddWithValue("@newPhone", phone);
                historyCommand.Parameters.AddWithValue("@newPhoneE164", normalizedPhone ?? string.Empty);
                historyCommand.Parameters.AddWithValue("@status", string.IsNullOrWhiteSpace(existingPhoneE164) ? "Dogrulanmadi" : "DogrulamaGuncellendi");
                await historyCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
            return affected;
        }
        catch (SqlException)
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }
    }

    public async Task<bool> SaveTravelPreferencesAsync(long userId, UserTravelPreferencesForm form, CancellationToken cancellationToken = default)
    {
        if (userId <= 0)
        {
            return false;
        }

        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = new SqlCommand("""
            UPDATE [dbo].[KULLANICILAR]
            SET [TERCIH_EDILEN_ODA_TIPI] = NULLIF(@roomPreference, ''),
                [YATAK_TERCIHI] = NULLIF(@bedPreference, ''),
                [KONUSULAN_DILLER] = NULLIF(@spokenLanguages, ''),
                [SEYAHAT_AMACI] = NULLIF(@travelPurpose, ''),
                [OZEL_ISTEKLER] = NULLIF(@specialRequests, ''),
                [GUNCELLENME_TARIHI] = SYSUTCDATETIME()
            WHERE id = @userId;
            """, connection);
        command.Parameters.AddWithValue("@roomPreference", (form.RoomPreference ?? string.Empty).Trim());
        command.Parameters.AddWithValue("@bedPreference", (form.BedPreference ?? string.Empty).Trim());
        command.Parameters.AddWithValue("@spokenLanguages", (form.SpokenLanguages ?? string.Empty).Trim());
        command.Parameters.AddWithValue("@travelPurpose", (form.TravelPurpose ?? string.Empty).Trim());
        command.Parameters.AddWithValue("@specialRequests", (form.SpecialRequests ?? string.Empty).Trim());
        command.Parameters.AddWithValue("@userId", userId);
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    public async Task<(bool Success, string Message)> RequestEmailUpdateAsync(
        long userId,
        UserEmailUpdateRequestForm form,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default)
    {
        var newEmail = (form.NewEmail ?? string.Empty).Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(newEmail) || !newEmail.Contains('@', StringComparison.Ordinal) || !newEmail.Contains('.', StringComparison.Ordinal))
        {
            return (false, "Geçerli bir e-posta adresi girin.");
        }

        await using var connection = await OpenConnectionAsync(cancellationToken);

        const string snapshotSql = """
            SELECT TOP (1) COALESCE([AD_SOYAD], ''), COALESCE([EPOSTA], ''), [EPOSTA_DOGRULAMA_TARIHI]
            FROM [dbo].[KULLANICILAR]
            WHERE id = @userId;
            """;

        string fullName;
        string currentEmail;
        bool emailVerified;
        await using (var snapshotCommand = new SqlCommand(snapshotSql, connection))
        {
            snapshotCommand.Parameters.AddWithValue("@userId", userId);
            await using var reader = await snapshotCommand.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                return (false, "Kullanıcı bulunamadı.");
            }

            fullName = reader.IsDBNull(0) ? string.Empty : reader.GetString(0);
            currentEmail = reader.IsDBNull(1) ? string.Empty : reader.GetString(1);
            emailVerified = !reader.IsDBNull(2);
        }

        if (!emailVerified)
        {
            return (false, "E-posta değişikliği için önce mevcut e-posta adresinizi onaylamanız gerekir.");
        }

        if (string.Equals(currentEmail.Trim().ToLowerInvariant(), newEmail, StringComparison.OrdinalIgnoreCase))
        {
            return (false, "Yeni e-posta mevcut e-posta ile aynı.");
        }

        await using (var duplicateCheckCommand = new SqlCommand("SELECT COUNT(*) FROM [dbo].[KULLANICILAR] WHERE [EPOSTA] = @email AND id <> @userId;", connection))
        {
            duplicateCheckCommand.Parameters.AddWithValue("@email", newEmail);
            duplicateCheckCommand.Parameters.AddWithValue("@userId", userId);
            var duplicateCount = Convert.ToInt32(await duplicateCheckCommand.ExecuteScalarAsync(cancellationToken) ?? 0, CultureInfo.InvariantCulture);
            if (duplicateCount > 0)
            {
                return (false, "Bu e-posta adresi zaten kullanımda.");
            }
        }

        await using (var rateLimitCommand = new SqlCommand("""
            SELECT TOP (1) [OLUSTURULMA_TARIHI]
            FROM [dbo].[EPOSTA_DOGRULAMA_TOKENLARI]
            WHERE [KULLANICI_ID] = @userId
              AND [KULLANILDI_MI] = 0
            ORDER BY [OLUSTURULMA_TARIHI] DESC;
            """, connection))
        {
            rateLimitCommand.Parameters.AddWithValue("@userId", userId);
            var lastCreated = await rateLimitCommand.ExecuteScalarAsync(cancellationToken);
            if (lastCreated is DateTime lastCreatedAt && lastCreatedAt.ToUniversalTime() > DateTime.UtcNow.AddSeconds(-60))
            {
                return (false, "Yeni kod istemeden önce lütfen 1 dakika bekleyin.");
            }
        }

        var token = CreateSecureToken(48);
        var code = CreateNumericCode(6);
        var verificationLink = $"{_publicBaseUrl}/panel/user/profil-bilgilerim?openEmailUpdate=true";

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            await using (var invalidateCommand = new SqlCommand("""
                UPDATE [dbo].[EPOSTA_DOGRULAMA_TOKENLARI]
                SET [KULLANILDI_MI] = 1,
                    [KULLANILMA_TARIHI] = SYSUTCDATETIME()
                WHERE [KULLANICI_ID] = @userId
                  AND [KULLANILDI_MI] = 0;
                """, connection, (SqlTransaction)transaction))
            {
                invalidateCommand.Parameters.AddWithValue("@userId", userId);
                await invalidateCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            await using (var insertCommand = new SqlCommand("""
                INSERT INTO [dbo].[EPOSTA_DOGRULAMA_TOKENLARI]
                ([KULLANICI_ID], [EPOSTA], [TOKEN], [DOGRULAMA_KODU], [KULLANILDI_MI], [DENEME_SAYISI], [MAKSIMUM_DENEME], [IP_ADRESI], [KULLANICI_ARACISI], [GECERLILIK_SURESI], [OLUSTURULMA_TARIHI])
                VALUES
                (@userId, @email, @token, @code, 0, 0, 5, @ipAddress, @userAgent, DATEADD(MINUTE, 30, SYSUTCDATETIME()), SYSUTCDATETIME());
                """, connection, (SqlTransaction)transaction))
            {
                insertCommand.Parameters.AddWithValue("@userId", userId);
                insertCommand.Parameters.AddWithValue("@email", newEmail);
                insertCommand.Parameters.AddWithValue("@token", token);
                insertCommand.Parameters.AddWithValue("@code", code);
                insertCommand.Parameters.AddWithValue("@ipAddress", (object?)ipAddress ?? DBNull.Value);
                insertCommand.Parameters.AddWithValue("@userAgent", (object?)TrimOrNull(userAgent, 500) ?? DBNull.Value);
                await insertCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            await _emailQueueService.QueueTemplateAsync(
                connection,
                (SqlTransaction)transaction,
                new QueuedEmailTemplateRequest
                {
                    UserId = userId,
                    RecipientEmail = newEmail,
                    TemplateCode = "email_verify",
                    RelatedTable = "KULLANICILAR",
                    RelatedRecordId = userId,
                    Tokens = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["user_first_name"] = FirstNameFromFullName(fullName),
                        ["user_email"] = newEmail,
                        ["registration_date"] = DateTime.Now.ToString("dd.MM.yyyy HH:mm", CultureInfo.GetCultureInfo("tr-TR")),
                        ["verification_link"] = verificationLink,
                        ["verification_code"] = code
                    }
                },
                cancellationToken);

            await transaction.CommitAsync(cancellationToken);
            return (true, "Doğrulama kodu yeni e-posta adresinize gönderildi.");
        }
        catch (SqlException ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            return (false, $"Doğrulama kodu gönderilemedi: {ex.Message}");
        }
    }

    public async Task<(bool Success, string Message)> VerifyEmailUpdateAsync(
        long userId,
        UserEmailUpdateVerifyForm form,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default)
    {
        var newEmail = (form.NewEmail ?? string.Empty).Trim().ToLowerInvariant();
        var code = (form.Code ?? string.Empty).Trim().ToUpperInvariant();
        var token = (form.Token ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(newEmail) || string.IsNullOrWhiteSpace(code))
        {
            return (false, "E-posta ve doğrulama kodu zorunludur.");
        }

        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        const string tokenSql = """
            SELECT TOP (1) id, [GECERLILIK_SURESI], [KULLANILDI_MI], [DENEME_SAYISI], [MAKSIMUM_DENEME], [TOKEN]
            FROM [dbo].[EPOSTA_DOGRULAMA_TOKENLARI]
            WHERE [KULLANICI_ID] = @userId
              AND [EPOSTA] = @email
              AND [DOGRULAMA_KODU] = @code
            ORDER BY [OLUSTURULMA_TARIHI] DESC;
            """;

        long tokenId;
        DateTime expiryUtc;
        bool used;
        int attemptCount;
        int maxAttempt;
        string storedToken;
        await using (var tokenCommand = new SqlCommand(tokenSql, connection, (SqlTransaction)transaction))
        {
            tokenCommand.Parameters.AddWithValue("@userId", userId);
            tokenCommand.Parameters.AddWithValue("@email", newEmail);
            tokenCommand.Parameters.AddWithValue("@code", code);
            await using var reader = await tokenCommand.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                return (false, "Doğrulama kodu hatalı veya bulunamadı.");
            }

            tokenId = reader.GetInt64(0);
            expiryUtc = reader.GetDateTime(1);
            used = !reader.IsDBNull(2) && reader.GetBoolean(2);
            attemptCount = reader.IsDBNull(3) ? 0 : Convert.ToInt32(reader.GetValue(3), CultureInfo.InvariantCulture);
            maxAttempt = reader.IsDBNull(4) ? 5 : Convert.ToInt32(reader.GetValue(4), CultureInfo.InvariantCulture);
            storedToken = reader.IsDBNull(5) ? string.Empty : reader.GetString(5);
        }

        if (!string.IsNullOrWhiteSpace(token) && !string.Equals(token, storedToken, StringComparison.Ordinal))
        {
            await IncrementEmailTokenAttemptAsync(connection, (SqlTransaction)transaction, tokenId, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return (false, "Doğrulama bağlantısı geçersiz.");
        }

        if (used)
        {
            return (false, "Bu doğrulama kodu daha önce kullanılmış.");
        }

        if (expiryUtc <= DateTime.UtcNow)
        {
            return (false, "Doğrulama kodunun süresi dolmuş. Lütfen yeni kod isteyin.");
        }

        if (attemptCount >= maxAttempt)
        {
            return (false, "Bu doğrulama kodu çok fazla denendiği için geçersiz hale geldi.");
        }

        await using (var duplicateCheckCommand = new SqlCommand("SELECT COUNT(*) FROM [dbo].[KULLANICILAR] WHERE [EPOSTA] = @email AND id <> @userId;", connection, (SqlTransaction)transaction))
        {
            duplicateCheckCommand.Parameters.AddWithValue("@email", newEmail);
            duplicateCheckCommand.Parameters.AddWithValue("@userId", userId);
            var duplicateCount = Convert.ToInt32(await duplicateCheckCommand.ExecuteScalarAsync(cancellationToken) ?? 0, CultureInfo.InvariantCulture);
            if (duplicateCount > 0)
            {
                return (false, "Bu e-posta adresi artık kullanımda. Lütfen farklı bir e-posta deneyin.");
            }
        }

        await MarkEmailTokenUsedAsync(connection, (SqlTransaction)transaction, tokenId, cancellationToken);
        await using (var updateUserCommand = new SqlCommand("""
            UPDATE [dbo].[KULLANICILAR]
            SET [EPOSTA] = @email,
                [EPOSTA_DOGRULAMA_TARIHI] = COALESCE([EPOSTA_DOGRULAMA_TARIHI], SYSUTCDATETIME()),
                [EPOSTA_DOGRULAMA_SON_GONDERIM_TARIHI] = SYSUTCDATETIME(),
                [GUNCELLENME_TARIHI] = SYSUTCDATETIME()
            WHERE id = @userId;
            """, connection, (SqlTransaction)transaction))
        {
            updateUserCommand.Parameters.AddWithValue("@userId", userId);
            updateUserCommand.Parameters.AddWithValue("@email", newEmail);
            await updateUserCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
        return (true, "E-posta adresiniz güncellendi ve doğrulandı.");
    }

    public async Task<bool> SaveProfileImageAsync(long userId, string imageUrl, string source, CancellationToken cancellationToken = default)
    {
        var normalizedUrl = (imageUrl ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalizedUrl))
        {
            return false;
        }

        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = new SqlCommand("""
            UPDATE [dbo].[KULLANICILAR]
            SET [PROFIL_RESIM_URL] = @url,
                [PROFIL_RESIM_KAYNAK] = NULLIF(@source, ''),
                [GUNCELLENME_TARIHI] = SYSUTCDATETIME()
            WHERE id = @userId;
            """, connection);
        command.Parameters.AddWithValue("@userId", userId);
        command.Parameters.AddWithValue("@url", normalizedUrl);
        command.Parameters.AddWithValue("@source", (source ?? string.Empty).Trim());
        var saved = await command.ExecuteNonQueryAsync(cancellationToken) > 0;
        if (saved && normalizedUrl.StartsWith("secure:", StringComparison.OrdinalIgnoreCase))
        {
            await TrimProfileImageHistoryAsync(connection, userId, cancellationToken);
        }

        return saved;
    }

    public async Task<bool> DeleteProfileImageAsync(long userId, long fileId, CancellationToken cancellationToken = default)
    {
        if (userId <= 0 || fileId <= 0)
        {
            return false;
        }

        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using (var resetCommand = new SqlCommand("""
            UPDATE [dbo].[KULLANICILAR]
            SET [PROFIL_RESIM_URL] = NULL,
                [PROFIL_RESIM_KAYNAK] = NULL,
                [GUNCELLENME_TARIHI] = SYSUTCDATETIME()
            WHERE id = @userId
              AND [PROFIL_RESIM_URL] = @secureValue;
            """, connection))
        {
            resetCommand.Parameters.AddWithValue("@userId", userId);
            resetCommand.Parameters.AddWithValue("@secureValue", $"secure:{fileId}");
            await resetCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        return await _secureFileService.DeleteOwnedFileAsync(fileId, userId, "profile", cancellationToken);
    }

    private async Task TrimProfileImageHistoryAsync(SqlConnection connection, long userId, CancellationToken cancellationToken)
    {
        var oldFileIds = new List<long>();
        await using (var command = new SqlCommand("""
            SELECT id
            FROM (
                SELECT g.id,
                       ROW_NUMBER() OVER (ORDER BY g.id DESC) AS rn
                FROM [dbo].[GUVENLI_DOSYA_VARLIKLARI] g
                WHERE g.[SAHIBI_KULLANICI_ID] = @userId
                  AND g.[KATEGORI] = N'profile'
                  AND g.[GORSEL_MI] = 1
            ) ranked
            WHERE ranked.rn > 3;
            """, connection))
        {
            command.Parameters.AddWithValue("@userId", userId);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                oldFileIds.Add(Convert.ToInt64(reader.GetValue(0), CultureInfo.InvariantCulture));
            }
        }

        foreach (var oldFileId in oldFileIds)
        {
            await _secureFileService.DeleteOwnedFileAsync(oldFileId, userId, "profile", cancellationToken);
        }
    }

    private static async Task MarkEmailTokenUsedAsync(SqlConnection connection, SqlTransaction transaction, long tokenId, CancellationToken cancellationToken)
    {
        await using var command = new SqlCommand("""
            UPDATE [dbo].[EPOSTA_DOGRULAMA_TOKENLARI]
            SET [KULLANILDI_MI] = 1,
                [KULLANILMA_TARIHI] = SYSUTCDATETIME()
            WHERE id = @tokenId;
            """, connection, transaction);
        command.Parameters.AddWithValue("@tokenId", tokenId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task IncrementEmailTokenAttemptAsync(SqlConnection connection, SqlTransaction transaction, long tokenId, CancellationToken cancellationToken)
    {
        await using var command = new SqlCommand("""
            UPDATE [dbo].[EPOSTA_DOGRULAMA_TOKENLARI]
            SET [DENEME_SAYISI] = COALESCE([DENEME_SAYISI], 0) + 1
            WHERE id = @tokenId;
            """, connection, transaction);
        command.Parameters.AddWithValue("@tokenId", tokenId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static string CreateSecureToken(int byteLength)
    {
        var buffer = RandomNumberGenerator.GetBytes(byteLength);
        return Convert.ToHexString(buffer).ToLowerInvariant();
    }

    private static string CreateNumericCode(int length)
    {
        var max = (int)Math.Pow(10, length);
        var min = (int)Math.Pow(10, length - 1);
        return RandomNumberGenerator.GetInt32(min, max).ToString(CultureInfo.InvariantCulture);
    }

    private static string FirstNameFromFullName(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            return "Misafir";
        }

        return fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault() ?? fullName;
    }

    private static string? TrimOrNull(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }

    public async Task<UserNotificationsPageViewModel> GetNotificationsAsync(long userId, CancellationToken cancellationToken = default)
    {
        var model = new UserNotificationsPageViewModel();
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await EnsureNotificationPreferencesSchemaAsync(connection, cancellationToken);

        await using (var command = new SqlCommand(@"
            SELECT [REZERVASYON_EPOSTA], [REZERVASYON_PUSH], [CHECKIN_HATIRLATMA], [IPTAL_DEGISIM], [KAMPANYA_EPOSTA], [KAMPANYA_SMS], [SISTEM_BILDIRIMI], COALESCE(giris_eposta, 0)
            FROM [dbo].[KULLANICI_BILDIRIM_TERCIHLERI] WHERE [KULLANICI_ID] = @userId;", connection))
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
                    SystemNotifications = SafeBool(reader, 6),
                    LoginEmail = SafeBool(reader, 7)
                };
            }
        }

        await using (var command = new SqlCommand(@"
            SELECT TOP (6) [BASLIK], [MESAJ], [BILDIRIM_TURU], [OLUSTURULMA_TARIHI]
            FROM [dbo].[SISTEM_ICI_BILDIRIMLER]
            WHERE [KULLANICI_ID] = @userId AND [ARSIVLENDI_MI] = 0
            ORDER BY [OLUSTURULMA_TARIHI] DESC, id DESC;", connection))
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
        await EnsureNotificationPreferencesSchemaAsync(connection, cancellationToken);
        await using var command = new SqlCommand(@"
            IF EXISTS (SELECT 1 FROM [dbo].[KULLANICI_BILDIRIM_TERCIHLERI] WHERE [KULLANICI_ID] = @userId)
            BEGIN
                UPDATE [dbo].[KULLANICI_BILDIRIM_TERCIHLERI]
                SET [REZERVASYON_EPOSTA] = @reservationEmail,
                    [REZERVASYON_PUSH] = @reservationPush,
                    [CHECKIN_HATIRLATMA] = @checkInReminder,
                    [IPTAL_DEGISIM] = @cancellationChanges,
                    [KAMPANYA_EPOSTA] = @campaignEmail,
                    [KAMPANYA_SMS] = @campaignSms,
                    [SISTEM_BILDIRIMI] = @systemNotifications,
                    giris_eposta = @loginEmail,
                    [GUNCELLENME_TARIHI] = CURRENT_TIMESTAMP
                WHERE [KULLANICI_ID] = @userId;
            END
            ELSE
            BEGIN
                INSERT INTO [dbo].[KULLANICI_BILDIRIM_TERCIHLERI]
                ([KULLANICI_ID], [REZERVASYON_EPOSTA], [REZERVASYON_PUSH], [CHECKIN_HATIRLATMA], [IPTAL_DEGISIM], [KAMPANYA_EPOSTA], [KAMPANYA_SMS], [SISTEM_BILDIRIMI], giris_eposta, [GUNCELLENME_TARIHI])
                VALUES
                (@userId, @reservationEmail, @reservationPush, @checkInReminder, @cancellationChanges, @campaignEmail, @campaignSms, @systemNotifications, @loginEmail, CURRENT_TIMESTAMP);
            END;", connection);
        command.Parameters.AddWithValue("@userId", userId);
        command.Parameters.AddWithValue("@reservationEmail", form.ReservationEmail ? 1 : 0);
        command.Parameters.AddWithValue("@reservationPush", form.ReservationPush ? 1 : 0);
        command.Parameters.AddWithValue("@checkInReminder", form.CheckInReminder ? 1 : 0);
        command.Parameters.AddWithValue("@cancellationChanges", form.CancellationChanges ? 1 : 0);
        command.Parameters.AddWithValue("@campaignEmail", form.CampaignEmail ? 1 : 0);
        command.Parameters.AddWithValue("@campaignSms", form.CampaignSms ? 1 : 0);
        command.Parameters.AddWithValue("@systemNotifications", form.SystemNotifications ? 1 : 0);
        command.Parameters.AddWithValue("@loginEmail", form.LoginEmail ? 1 : 0);
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    private static async Task EnsureNotificationPreferencesSchemaAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        await using var command = new SqlCommand("""
            IF OBJECT_ID(N'[dbo].[KULLANICI_BILDIRIM_TERCIHLERI]', N'U') IS NOT NULL
               AND COL_LENGTH(N'[dbo].[KULLANICI_BILDIRIM_TERCIHLERI]', N'giris_eposta') IS NULL
            BEGIN
                ALTER TABLE [dbo].[KULLANICI_BILDIRIM_TERCIHLERI]
                ADD giris_eposta bit NOT NULL CONSTRAINT DF_kullanici_bildirim_tercihleri_giris_eposta DEFAULT (0);
            END
            """, connection);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<UserSecurityPageViewModel> GetSecurityAsync(long userId, CancellationToken cancellationToken = default)
    {
        var model = new UserSecurityPageViewModel();
        await using var connection = await OpenConnectionAsync(cancellationToken);

        await using (var command = new SqlCommand("""
            SELECT TOP (1)
                COALESCE([IKI_ASAMALI_DOGRULAMA_AKTIF_MI], 0),
                COALESCE([IKI_ASAMALI_DOGRULAMA_KANALI], 'email'),
                COALESCE([EPOSTA], ''),
                [EPOSTA_DOGRULAMA_TARIHI],
                COALESCE([TELEFON_E164], ''),
                [TELEFON_DOGRULAMA_TARIHI]
            FROM [dbo].[KULLANICILAR]
            WHERE id = @userId;
            """, connection))
        {
            command.Parameters.AddWithValue("@userId", userId);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                model.TwoFactorEnabled = Convert.ToInt32(reader.GetValue(0), CultureInfo.InvariantCulture) == 1;
                model.SelectedTwoFactorChannel = NormalizeTwoFactorChannel(reader.GetString(1));
                model.EmailAddress = reader.GetString(2);
                model.MaskedEmailAddress = MaskEmail(model.EmailAddress);
                model.EmailUsableForTwoFactor = !string.IsNullOrWhiteSpace(model.EmailAddress) && !reader.IsDBNull(3);
                model.PhoneNumber = reader.GetString(4);
                model.MaskedPhoneNumber = MaskPhone(model.PhoneNumber);
                model.PhoneUsableForTwoFactor = !string.IsNullOrWhiteSpace(model.PhoneNumber) && !reader.IsDBNull(5);
            }
        }

        await using var sessionCommand = new SqlCommand(@"
            SELECT TOP (5)
                COALESCE(s.[CIHAZ_ETIKETI], 'Bilinmeyen cihaz') AS cihaz,
                s.[BENI_HATIRLA_TERCIHI],
                s.[TOPLAM_OTURUM_SURESI_SANIYE],
                s.[SON_AKTIVITE_TARIHI],
                l.[IP_ADRESI],
                l.[GIRIS_TARIHI]
            FROM [dbo].[KULLANICI_OTURUM_ISTATISTIKLERI] s
            OUTER APPLY
            (
                SELECT TOP (1) [IP_ADRESI], [GIRIS_TARIHI]
                FROM [dbo].[KULLANICI_GIRIS_LOGLARI]
                WHERE [KULLANICI_ID] = s.[KULLANICI_ID]
                  AND [HESAP_TIPI] = 'user'
                ORDER BY [GIRIS_TARIHI] DESC
            ) l
            WHERE s.[KULLANICI_ID] = @userId
              AND s.[HESAP_TIPI] = 'user'
            ORDER BY s.[SON_AKTIVITE_TARIHI] DESC, s.id DESC;", connection);
        sessionCommand.Parameters.AddWithValue("@userId", userId);
        await using var sessionReader = await sessionCommand.ExecuteReaderAsync(cancellationToken);
        while (await sessionReader.ReadAsync(cancellationToken))
        {
            var duration = sessionReader.IsDBNull(2) ? 0L : sessionReader.GetInt64(2);
            var lastActive = sessionReader.IsDBNull(3) ? (DateTime?)null : sessionReader.GetDateTime(3).ToLocalTime();
            var ip = sessionReader.IsDBNull(4) ? "—" : sessionReader.GetString(4);
            var loginAtLocal = sessionReader.IsDBNull(5) ? (DateTime?)null : sessionReader.GetDateTime(5).ToLocalTime();
            var openMinutes = loginAtLocal.HasValue ? (int)Math.Max(0, Math.Round((DateTime.Now - loginAtLocal.Value).TotalMinutes)) : 0;
            model.Sessions.Add(new UserSessionRowViewModel
            {
                DeviceLabel = sessionReader.GetString(0),
                IpAddress = ip,
                LoginAtText = loginAtLocal.HasValue ? $"{loginAtLocal.Value:dd.MM.yyyy HH:mm}" : "—",
                OpenMinutes = openMinutes,
                RememberText = SafeBool(sessionReader, 1) ? "Beni hatırla açık" : "Standart oturum",
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
            UPDATE [dbo].[KULLANICILAR]
            SET [SIFRE] = CONVERT(varchar(64), HASHBYTES('SHA2_256', @newPassword), 2)
            WHERE id = @userId
              AND [SIFRE] = CONVERT(varchar(64), HASHBYTES('SHA2_256', @currentPassword), 2);", connection);
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
        var channel = NormalizeTwoFactorChannel(form.Channel);
        if (form.Enabled)
        {
            await using var validationCommand = new SqlCommand("""
                SELECT TOP (1)
                    COALESCE([EPOSTA], ''),
                    [EPOSTA_DOGRULAMA_TARIHI],
                    COALESCE([TELEFON_E164], ''),
                    [TELEFON_DOGRULAMA_TARIHI]
                FROM [dbo].[KULLANICILAR]
                WHERE id = @userId;
                """, connection);
            validationCommand.Parameters.AddWithValue("@userId", userId);
            await using var validationReader = await validationCommand.ExecuteReaderAsync(cancellationToken);
            if (await validationReader.ReadAsync(cancellationToken))
            {
                var email = validationReader.GetString(0);
                var emailVerified = !validationReader.IsDBNull(1);
                var phone = validationReader.GetString(2);
                var phoneVerified = !validationReader.IsDBNull(3);

                if (channel == "email" && (string.IsNullOrWhiteSpace(email) || !emailVerified))
                {
                    return false;
                }

                if (channel == "whatsapp" && (string.IsNullOrWhiteSpace(phone) || !phoneVerified))
                {
                    return false;
                }
            }
        }

        await using var command = new SqlCommand("""
            UPDATE [dbo].[KULLANICILAR]
            SET [IKI_ASAMALI_DOGRULAMA_AKTIF_MI] = @enabled,
                [IKI_ASAMALI_DOGRULAMA_KANALI] = @channel
            WHERE id = @userId;
            """, connection);
        command.Parameters.AddWithValue("@enabled", form.Enabled ? 1 : 0);
        command.Parameters.AddWithValue("@channel", channel);
        command.Parameters.AddWithValue("@userId", userId);
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    private static string NormalizeTwoFactorChannel(string? channel)
    {
        var normalized = (channel ?? string.Empty).Trim().ToLowerInvariant();
        return normalized switch
        {
            "telefon" => "whatsapp",
            "phone" => "whatsapp",
            "whatsapp" => "whatsapp",
            _ => "email"
        };
    }

    private static async Task SyncGeoTextFromIdsAsync(SqlConnection connection, UserProfileForm form, CancellationToken cancellationToken)
    {
        if (form.IlId is > 0 && string.IsNullOrWhiteSpace(form.City))
        {
            await using var ilCommand = new SqlCommand("SELECT TOP (1) [IL_ADI] FROM [dbo].[ILLER] WHERE [ID] = @id;", connection);
            ilCommand.Parameters.AddWithValue("@id", form.IlId.Value);
            form.City = Convert.ToString(await ilCommand.ExecuteScalarAsync(cancellationToken), CultureInfo.InvariantCulture)?.Trim();
        }

        if (form.IlceId is > 0 && string.IsNullOrWhiteSpace(form.District))
        {
            await using var ilceCommand = new SqlCommand("SELECT TOP (1) [ILCE_ADI] FROM [dbo].[ILCELER] WHERE [ID] = @id;", connection);
            ilceCommand.Parameters.AddWithValue("@id", form.IlceId.Value);
            form.District = Convert.ToString(await ilceCommand.ExecuteScalarAsync(cancellationToken), CultureInfo.InvariantCulture)?.Trim();
        }

        if (form.MahalleId is > 0 && string.IsNullOrWhiteSpace(form.Neighborhood))
        {
            await using var mahalleCommand = new SqlCommand("SELECT TOP (1) [MAHALLE_ADI] FROM [dbo].[MAHALLELER] WHERE [ID] = @id;", connection);
            mahalleCommand.Parameters.AddWithValue("@id", form.MahalleId.Value);
            form.Neighborhood = Convert.ToString(await mahalleCommand.ExecuteScalarAsync(cancellationToken), CultureInfo.InvariantCulture)?.Trim();
        }
    }

    private static string MaskEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@', StringComparison.Ordinal))
        {
            return "—";
        }

        var parts = email.Split('@', 2);
        var local = parts[0];
        var domain = parts[1];
        if (local.Length <= 2)
        {
            return $"{local[0]}***@{domain}";
        }

        return $"{local[..2]}***@{domain}";
    }

    private static string MaskPhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
        {
            return "—";
        }

        var digits = new string(phone.Where(char.IsDigit).ToArray());
        if (digits.Length < 4)
        {
            return phone;
        }

        return $"*** *** {digits[^4..]}";
    }

    public Task<UserPaymentMethodsPageViewModel> GetPaymentMethodsAsync(long userId, CancellationToken cancellationToken = default)
        => _paymentCardService.GetPaymentMethodsPageAsync(userId, cancellationToken);

    public Task<(bool Success, string Message)> SavePaymentMethodAsync(long userId, UserPaymentMethodForm form, CancellationToken cancellationToken = default)
        => _paymentCardService.SavePaymentMethodAsync(userId, form, cancellationToken);

    public Task<bool> DeletePaymentMethodAsync(long userId, long paymentMethodId, CancellationToken cancellationToken = default)
        => _paymentCardService.DeletePaymentMethodAsync(userId, paymentMethodId, cancellationToken);

    public Task<bool> SetDefaultPaymentMethodAsync(long userId, long paymentMethodId, CancellationToken cancellationToken = default)
        => _paymentCardService.SetDefaultPaymentMethodAsync(userId, paymentMethodId, cancellationToken);

    public async Task<(bool Success, string Message)> SaveBillingInfoAsync(long userId, UserBillingForm form, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(form.InvoiceName))
        {
            return (false, "Fatura unvani zorunludur.");
        }

        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = new SqlCommand(@"
            UPDATE [dbo].[KULLANICILAR]
            SET [AD_SOYAD] = @invoiceName,
                [ADRES] = @addressLine,
                ilce = @district,
                [SEHIR] = @city
            WHERE id = @userId;", connection);
        command.Parameters.AddWithValue("@invoiceName", form.InvoiceName.Trim());
        command.Parameters.AddWithValue("@addressLine", string.IsNullOrWhiteSpace(form.AddressLine) ? DBNull.Value : (object)form.AddressLine.Trim());
        command.Parameters.AddWithValue("@district", string.IsNullOrWhiteSpace(form.District) ? DBNull.Value : (object)form.District.Trim());
        command.Parameters.AddWithValue("@city", string.IsNullOrWhiteSpace(form.City) ? DBNull.Value : (object)form.City.Trim());
        command.Parameters.AddWithValue("@userId", userId);

        var affected = await command.ExecuteNonQueryAsync(cancellationToken);
        return affected > 0
            ? (true, "Fatura bilgileri guncellendi.")
            : (false, "Fatura bilgileri kaydedilemedi.");
    }

    public async Task<UserLoyaltyPageViewModel> GetLoyaltyAsync(long userId, CancellationToken cancellationToken = default)
    {
        var model = new UserLoyaltyPageViewModel();
        await using var connection = await OpenConnectionAsync(cancellationToken);

        try
        {
            await EnsureLoyaltyAccountAsync(connection, userId, cancellationToken);
        }
        catch (SqlException)
        {
            // Sadakat hesabı oluşturulamazsa varsayılan özetle devam et.
        }

        var alertHotelIds = new List<long>();

        await using (var summaryCommand = new SqlCommand(@"
            SELECT
                COALESCE(u.[AD_SOYAD], 'Misafir') AS [AD_SOYAD],
                COALESCE(h.[TOPLAM_PUAN], 0) AS [TOPLAM_PUAN],
                COALESCE(h.[KULLANILABILIR_PUAN], 0) AS [KULLANILABILIR_PUAN],
                COALESCE(h.[BU_YIL_KAZANILAN_PUAN], 0) AS [BU_YIL_KAZANILAN_PUAN],
                COALESCE(h.[BU_YIL_KULLANILAN_PUAN], 0) AS [BU_YIL_KULLANILAN_PUAN],
                COALESCE(h.[PUAN_GECERLILIK_TARIHI], DATEADD(DAY, 365, CAST(GETDATE() AS date))) AS [PUAN_GECERLILIK_TARIHI],
                COALESCE(ct.ad, 'Bronz') AS mevcut_seviye_adi,
                COALESCE(ct.[KOD], 'B') AS mevcut_seviye_kodu,
                COALESCE(ct.[AVANTAJLAR_METIN], 'Yuzde 5 indirim|Hos geldin puani') AS [AVANTAJLAR_METIN],
                COALESCE(nt.ad, '') AS sonraki_seviye_adi,
                CASE
                    WHEN COALESCE(nt.[MINIMUM_PUAN], COALESCE(h.[KULLANILABILIR_PUAN], 0)) - COALESCE(h.[KULLANILABILIR_PUAN], 0) > 0
                        THEN COALESCE(nt.[MINIMUM_PUAN], COALESCE(h.[KULLANILABILIR_PUAN], 0)) - COALESCE(h.[KULLANILABILIR_PUAN], 0)
                    ELSE 0
                END AS kalan_puan,
                CASE
                    WHEN COALESCE(nt.[MINIMUM_PUAN], 0) <= 0 THEN 100
                    ELSE CASE
                        WHEN ROUND((COALESCE(h.[KULLANILABILIR_PUAN], 0) / nt.[MINIMUM_PUAN]) * 100, 0) > 100 THEN 100
                        ELSE ROUND((COALESCE(h.[KULLANILABILIR_PUAN], 0) / nt.[MINIMUM_PUAN]) * 100, 0)
                    END
                END AS ilerleme_yuzdesi
            FROM [dbo].[KULLANICILAR] u
            LEFT JOIN [dbo].[KULLANICI_SADAKAT_HESAPLARI] h ON h.[KULLANICI_ID] = u.id
            LEFT JOIN [dbo].[SADAKAT_SEVIYELERI] ct ON ct.id = h.[MEVCUT_SEVIYE_ID]
            LEFT JOIN [dbo].[SADAKAT_SEVIYELERI] nt ON nt.id = h.[SONRAKI_SEVIYE_ID]
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

        try
        {
            model.Tiers = await LoadLoyaltyTiersAsync(connection, model.CurrentTierCode, cancellationToken);
            model.PointTransactions = await LoadLoyaltyTransactionsAsync(connection, userId, cancellationToken);
            model.Rewards = await LoadLoyaltyRewardsAsync(connection, model.AvailablePoints, cancellationToken);
        }
        catch (SqlException)
        {
            // Sadakat tabloları eksikse temel özetle devam et.
        }

        try
        {
            model.Badges = await LoadUserBadgesAsync(connection, userId, cancellationToken);
        }
        catch (SqlException)
        {
        }

        try
        {
            await EnsurePassportCitiesAsync(connection, userId, cancellationToken);
            model.PassportCities = await LoadPassportCitiesAsync(connection, userId, cancellationToken);
        }
        catch (SqlException)
        {
        }

        try
        {
            model.RecentReservationCities = await LoadRecentReservationCitiesAsync(connection, userId, 5, cancellationToken);
            model.TravelPlans = await LoadTravelPlansAsync(connection, userId, cancellationToken);
            model.Offers = await LoadOffersAsync(connection, userId, cancellationToken);
            model.BudgetPlans = await LoadBudgetPlansAsync(connection, userId, cancellationToken);
            model.PriceAlerts = await LoadPriceAlertsAsync(connection, userId, alertHotelIds, cancellationToken);
        }
        catch (SqlException)
        {
        }

        if (alertHotelIds.Count > 0)
        {
            try
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
            catch (SqlException)
            {
            }
        }

        try
        {
            model.Recommendations = await LoadRecommendationsAsync(connection, cancellationToken);
        }
        catch (SqlException)
        {
        }

        model.BudgetPlanForm.DestinationCity = model.BudgetPlans.FirstOrDefault()?.DestinationText ?? string.Empty;
        model.BudgetPlanForm.TargetBudget = model.BudgetPlans.FirstOrDefault() is { } budget
            && TryParseCurrency(budget.BudgetText, out var budgetValue)
            ? budgetValue
            : null;
        model.TravelPlanForm.DestinationCity = model.PassportCities.FirstOrDefault(static item => !item.IsVisited)?.CityName ?? string.Empty;

        try
        {
            var hotelBalances = await _hotelPointsService.GetUserHotelBalancesAsync(userId, cancellationToken);
            model.HotelPointBalances = hotelBalances.Select(static balance => new UserHotelPointsBalanceViewModel
            {
                HotelId = balance.HotelId,
                HotelName = balance.HotelName,
                HotelCity = balance.HotelCity,
                TotalEarned = balance.TotalEarned,
                AvailablePoints = balance.AvailablePoints,
                UsedPoints = balance.UsedPoints,
                DiscountPercent = balance.DiscountPercent,
                LastEarnedText = balance.LastEarnedText,
                StayCount = balance.StayCount,
                LastStayText = balance.LastStayText,
                RecentMovements = balance.RecentMovements.Select(static movement => new UserHotelPointMovementViewModel
                {
                    DateText = movement.DateText,
                    Title = movement.Title,
                    Description = movement.Description,
                    PointChange = movement.PointChange,
                    PointChangeText = movement.PointChangeText
                }).ToList()
            }).ToList();
        }
        catch (SqlException)
        {
            model.HotelPointBalances = new List<UserHotelPointsBalanceViewModel>();
        }

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
            INSERT INTO [dbo].[KULLANICI_BUTCE_PLANLARI]
            ([KULLANICI_ID], [HEDEF_SEHIR], [HEDEF_BUTCE], [GECE_SAYISI], [KISI_SAYISI], [PARA_BIRIMI], [NOTLAR], [OLUSTURULMA_TARIHI], [GUNCELLENME_TARIHI])
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
            INSERT INTO [dbo].[KULLANICI_SEYAHAT_PLANLARI]
            ([OLUSTURAN_KULLANICI_ID], [PLAN_KODU], [PLAN_ADI], [HEDEF_SEHIR], [BASLANGIC_TARIHI], [BITIS_TARIHI], [BUTCE_TUTARI], [PARA_BIRIMI], [DAVET_KODU], [DURUM], [OLUSTURULMA_TARIHI], [GUNCELLENME_TARIHI])
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

    public async Task<(bool Success, string Message)> RedeemRewardAsync(long userId, UserLoyaltyRedeemForm form, CancellationToken cancellationToken = default)
    {
        if (form.RewardId <= 0)
        {
            return (false, "Gecerli bir odul seciniz.");
        }

        await using var connection = await OpenConnectionAsync(cancellationToken);
        await EnsureLoyaltyAccountAsync(connection, userId, cancellationToken);

        string rewardTitle;
        int requiredPoints;
        await using (var rewardCommand = new SqlCommand(@"
            SELECT TOP (1) [AD], [GEREKLI_PUAN]
            FROM [dbo].[SADAKAT_ODULLERI]
            WHERE id = @rewardId AND [AKTIF_MI] = 1;", connection))
        {
            rewardCommand.Parameters.AddWithValue("@rewardId", form.RewardId);
            await using var reader = await rewardCommand.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                return (false, "Odul bulunamadi veya su an kullanilamiyor.");
            }

            rewardTitle = reader.GetString(0);
            requiredPoints = SafeInt(reader, 1);
        }

        long accountId;
        int availablePoints;
        await using (var accountCommand = new SqlCommand(@"
            SELECT TOP (1) id, [KULLANILABILIR_PUAN]
            FROM [dbo].[KULLANICI_SADAKAT_HESAPLARI]
            WHERE [KULLANICI_ID] = @userId;", connection))
        {
            accountCommand.Parameters.AddWithValue("@userId", userId);
            await using var reader = await accountCommand.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                return (false, "Sadakat hesabi bulunamadi.");
            }

            accountId = reader.GetInt64(0);
            availablePoints = SafeInt(reader, 1);
        }

        if (availablePoints < requiredPoints)
        {
            return (false, "Bu odul icin yeterli puaniniz bulunmuyor.");
        }

        var newBalance = availablePoints - requiredPoints;
        await using var insertCommand = new SqlCommand(@"
            INSERT INTO [dbo].[KULLANICI_PUAN_HAREKETLERI]
            ([KULLANICI_ID], [SADAKAT_HESAP_ID], [HAREKET_TIPI], [BASLIK], [ACIKLAMA], [PUAN_DEGISIM], [PUAN_BAKIYE_SONRASI], [DURUM], [ISLEM_TARIHI], [OLUSTURULMA_TARIHI])
            VALUES
            (@userId, @accountId, N'OdulKullanim', @title, @description, @delta, @newBalance, N'Tamamlandi', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);", connection);
        insertCommand.Parameters.AddWithValue("@userId", userId);
        insertCommand.Parameters.AddWithValue("@accountId", accountId);
        insertCommand.Parameters.AddWithValue("@title", rewardTitle);
        insertCommand.Parameters.AddWithValue("@description", $"Odul katalogu · #{form.RewardId}");
        insertCommand.Parameters.AddWithValue("@delta", -requiredPoints);
        insertCommand.Parameters.AddWithValue("@newBalance", newBalance);
        await insertCommand.ExecuteNonQueryAsync(cancellationToken);

        await EnsureLoyaltyAccountAsync(connection, userId, cancellationToken);
        return (true, $"{rewardTitle} odulu basariyla kullanildi. {requiredPoints:N0} puan dusuldu.");
    }

    private async Task<SqlConnection> OpenConnectionAsync(CancellationToken cancellationToken)
    {
        var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }

    private async Task EnsureLoyaltyAccountAsync(SqlConnection connection, long userId, CancellationToken cancellationToken)
    {
        const string existsSql = "SELECT COUNT(*) FROM [dbo].[KULLANICI_SADAKAT_HESAPLARI] WHERE [KULLANICI_ID] = @userId;";
        await using (var existsCommand = new SqlCommand(existsSql, connection))
        {
            existsCommand.Parameters.AddWithValue("@userId", userId);
            var exists = Convert.ToInt32(await existsCommand.ExecuteScalarAsync(cancellationToken) ?? 0, CultureInfo.InvariantCulture);
            if (exists == 0)
            {
                var bronzeTierId = await ResolveTierIdAsync(connection, "BRONZE", cancellationToken);
                var silverTierId = await ResolveTierIdAsync(connection, "SILVER", cancellationToken);
                await using var insertCommand = new SqlCommand(@"
                    INSERT INTO [dbo].[KULLANICI_SADAKAT_HESAPLARI]
                    ([KULLANICI_ID], [TOPLAM_PUAN], [KULLANILABILIR_PUAN], [BU_YIL_KAZANILAN_PUAN], [BU_YIL_KULLANILAN_PUAN], [MEVCUT_SEVIYE_ID], [SONRAKI_SEVIYE_ID], [PUAN_GECERLILIK_TARIHI], [OLUSTURULMA_TARIHI], [GUNCELLENME_TARIHI])
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
            SET h.[TOPLAM_PUAN] = agg.kazanilan,
                h.[KULLANILABILIR_PUAN] = CASE WHEN agg.kazanilan - agg.kullanilan > 0 THEN agg.kazanilan - agg.kullanilan ELSE 0 END,
                h.[BU_YIL_KAZANILAN_PUAN] = yearly.yil_kazanilan,
                h.[BU_YIL_KULLANILAN_PUAN] = yearly.yil_kullanilan,
                h.[MEVCUT_SEVIYE_ID] = COALESCE(current_tier.id, h.[MEVCUT_SEVIYE_ID]),
                h.[SONRAKI_SEVIYE_ID] = next_tier.id,
                h.[SON_SEVIYE_GUNCELLEME_TARIHI] = CURRENT_TIMESTAMP,
                h.[GUNCELLENME_TARIHI] = CURRENT_TIMESTAMP
            FROM [dbo].[KULLANICI_SADAKAT_HESAPLARI] h
            CROSS APPLY (
                SELECT
                    COALESCE(SUM(CASE WHEN p.[PUAN_DEGISIM] > 0 THEN p.[PUAN_DEGISIM] ELSE 0 END), 0) AS kazanilan,
                    COALESCE(ABS(SUM(CASE WHEN p.[PUAN_DEGISIM] < 0 THEN p.[PUAN_DEGISIM] ELSE 0 END)), 0) AS kullanilan
                FROM [dbo].[KULLANICI_PUAN_HAREKETLERI] p
                WHERE p.[KULLANICI_ID] = @userId
                  AND COALESCE(p.[DURUM], 'Tamamlandi') <> 'Iptal'
            ) agg
            CROSS APPLY (
                SELECT
                    COALESCE(SUM(CASE WHEN y.[PUAN_DEGISIM] > 0 THEN y.[PUAN_DEGISIM] ELSE 0 END), 0) AS yil_kazanilan,
                    COALESCE(ABS(SUM(CASE WHEN y.[PUAN_DEGISIM] < 0 THEN y.[PUAN_DEGISIM] ELSE 0 END)), 0) AS yil_kullanilan
                FROM [dbo].[KULLANICI_PUAN_HAREKETLERI] y
                WHERE y.[KULLANICI_ID] = @userId
                  AND YEAR(COALESCE(y.[ISLEM_TARIHI], CURRENT_TIMESTAMP)) = YEAR(CAST(GETDATE() AS date))
            ) yearly
            OUTER APPLY (
                SELECT TOP (1) s.id
                FROM [dbo].[SADAKAT_SEVIYELERI] s
                WHERE agg.kazanilan - agg.kullanilan >= s.[MINIMUM_PUAN]
                  AND (s.[MAXIMUM_PUAN] IS NULL OR agg.kazanilan - agg.kullanilan <= s.[MAXIMUM_PUAN])
                ORDER BY s.[MINIMUM_PUAN] DESC
            ) current_tier
            OUTER APPLY (
                SELECT TOP (1) s2.id
                FROM [dbo].[SADAKAT_SEVIYELERI] s2
                WHERE s2.[MINIMUM_PUAN] > agg.kazanilan - agg.kullanilan
                ORDER BY s2.[MINIMUM_PUAN] ASC
            ) next_tier
            WHERE h.[KULLANICI_ID] = @userId;", connection);
        syncCommand.Parameters.AddWithValue("@userId", userId);
        await syncCommand.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task<long> ResolveTierIdAsync(SqlConnection connection, string code, CancellationToken cancellationToken)
    {
        await using var command = new SqlCommand("SELECT TOP (1) id FROM [dbo].[SADAKAT_SEVIYELERI] WHERE kod = @code;", connection);
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
            SELECT id, kod, ad, [MINIMUM_PUAN], [MAXIMUM_PUAN], COALESCE([AVANTAJLAR_METIN], '')
            FROM [dbo].[SADAKAT_SEVIYELERI]
            WHERE [AKTIF_MI] = 1
            ORDER BY [SIRA_NO] ASC, [MINIMUM_PUAN] ASC;", connection);
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
            SELECT CONVERT(varchar(10), COALESCE([ISLEM_TARIHI], [OLUSTURULMA_TARIHI]), 104) AS [TARIH],
                   COALESCE([HAREKET_TIPI], 'Bilgi'),
                   COALESCE([BASLIK], 'Puan hareketi'),
                   COALESCE([ACIKLAMA], ''),
                   COALESCE([PUAN_DEGISIM], 0),
                   COALESCE([DURUM], 'Tamamlandi')
            FROM [dbo].[KULLANICI_PUAN_HAREKETLERI]
            WHERE [KULLANICI_ID] = @userId
            ORDER BY COALESCE([ISLEM_TARIHI], [OLUSTURULMA_TARIHI]) DESC, id DESC
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
            SELECT TOP (8) id, ad, [ACIKLAMA], [GEREKLI_PUAN], COALESCE(ikon, 'fas fa-gift'), COALESCE(ton, 'primary')
            FROM [dbo].[SADAKAT_ODULLERI]
            WHERE [AKTIF_MI] = 1
            ORDER BY [GEREKLI_PUAN] ASC, id ASC;", connection);
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
            SELECT TOP (8) r.ad, COALESCE(r.[IKON], 'fas fa-award'), COALESCE(kr.[DURUM], 'Kilitli'), COALESCE(kr.[ILERLEME_DEGERI], 0), COALESCE(r.[HEDEF_DEGER], 1)
            FROM [dbo].[ROZET_TANIMLARI] r
            LEFT JOIN [dbo].[KULLANICI_ROZETLERI] kr ON kr.[ROZET_ID] = r.id AND kr.[KULLANICI_ID] = @userId
            WHERE r.[AKTIF_MI] = 1
            ORDER BY r.[SIRALAMA] ASC, r.id ASC;", connection);
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
            SELECT TOP (8) [SEHIR], COALESCE(ulke, 'Türkiye'), [TOPLAM_KONAKLAMA_SAYISI], [ILK_KONAKLAMA_TARIHI], [SON_KONAKLAMA_TARIHI]
            FROM [dbo].[KULLANICI_DIJITAL_PASAPORTLARI]
            WHERE [KULLANICI_ID] = @userId
            ORDER BY [SON_KONAKLAMA_TARIHI] DESC, [TOPLAM_KONAKLAMA_SAYISI] DESC;", connection);
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

    private static async Task EnsurePassportCitiesAsync(SqlConnection connection, long userId, CancellationToken cancellationToken)
    {
        const string sql = """
            MERGE kullanici_dijital_pasaportlari AS target
            USING
            (
                SELECT
                    @userId AS [KULLANICI_ID],
                    LTRIM(RTRIM(COALESCE(o.[SEHIR], ''))) AS [SEHIR],
                    CAST(MIN(r.[GIRIS_TARIHI]) AS date) AS [ILK_KONAKLAMA_TARIHI],
                    CAST(MAX(r.[CIKIS_TARIHI]) AS date) AS [SON_KONAKLAMA_TARIHI],
                    COUNT(*) AS [TOPLAM_KONAKLAMA_SAYISI]
                FROM [dbo].[REZERVASYONLAR] r
                INNER JOIN [dbo].[OTELLER] o ON o.id = r.[OTEL_ID]
                WHERE r.[KULLANICI_ID] = @userId
                  AND COALESCE(NULLIF(o.[SEHIR], ''), '') <> ''
                  AND COALESCE(NULLIF(r.[DURUM], ''), '') <> 'İptal Edildi'
                GROUP BY LTRIM(RTRIM(COALESCE(o.[SEHIR], '')))
            ) AS source
            ON target.[KULLANICI_ID] = source.[KULLANICI_ID]
               AND target.[SEHIR] = source.[SEHIR]
            WHEN MATCHED THEN
                UPDATE SET
                    [ILK_KONAKLAMA_TARIHI] = CASE
                        WHEN target.[ILK_KONAKLAMA_TARIHI] IS NULL THEN source.[ILK_KONAKLAMA_TARIHI]
                        WHEN source.[ILK_KONAKLAMA_TARIHI] < target.[ILK_KONAKLAMA_TARIHI] THEN source.[ILK_KONAKLAMA_TARIHI]
                        ELSE target.[ILK_KONAKLAMA_TARIHI]
                    END,
                    [SON_KONAKLAMA_TARIHI] = CASE
                        WHEN target.[SON_KONAKLAMA_TARIHI] IS NULL THEN source.[SON_KONAKLAMA_TARIHI]
                        WHEN source.[SON_KONAKLAMA_TARIHI] > target.[SON_KONAKLAMA_TARIHI] THEN source.[SON_KONAKLAMA_TARIHI]
                        ELSE target.[SON_KONAKLAMA_TARIHI]
                    END,
                    [TOPLAM_KONAKLAMA_SAYISI] = source.[TOPLAM_KONAKLAMA_SAYISI],
                    [GUNCELLENME_TARIHI] = SYSUTCDATETIME()
            WHEN NOT MATCHED THEN
                INSERT ([KULLANICI_ID], [SEHIR], ulke, [ILK_KONAKLAMA_TARIHI], [SON_KONAKLAMA_TARIHI], [TOPLAM_KONAKLAMA_SAYISI], [OLUSTURULMA_TARIHI], [GUNCELLENME_TARIHI])
                VALUES (source.[KULLANICI_ID], source.[SEHIR], N'Türkiye', source.[ILK_KONAKLAMA_TARIHI], source.[SON_KONAKLAMA_TARIHI], source.[TOPLAM_KONAKLAMA_SAYISI], SYSUTCDATETIME(), SYSUTCDATETIME());
            """;

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@userId", userId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<List<UserLoyaltyRecentCityViewModel>> LoadRecentReservationCitiesAsync(SqlConnection connection, long userId, int take, CancellationToken cancellationToken)
    {
        var list = new List<UserLoyaltyRecentCityViewModel>();
        await using var command = new SqlCommand("""
            SELECT TOP (@take)
                   COALESCE(NULLIF(o.[SEHIR], ''), '') AS [SEHIR],
                   r.[GIRIS_TARIHI]
            FROM [dbo].[REZERVASYONLAR] r
            INNER JOIN [dbo].[OTELLER] o ON o.id = r.[OTEL_ID]
            WHERE r.[KULLANICI_ID] = @userId
              AND COALESCE(NULLIF(o.[SEHIR], ''), '') <> ''
            ORDER BY r.[GIRIS_TARIHI] DESC, r.id DESC;
            """, connection);
        command.Parameters.AddWithValue("@userId", userId);
        command.Parameters.AddWithValue("@take", Math.Clamp(take, 1, 10));
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var city = reader.GetString(0);
            var checkIn = reader.IsDBNull(1) ? (DateTime?)null : reader.GetDateTime(1);
            list.Add(new UserLoyaltyRecentCityViewModel
            {
                CityName = city,
                DateText = checkIn.HasValue ? checkIn.Value.ToString("dd MMM yyyy", CultureInfo.GetCultureInfo("tr-TR")) : "—"
            });
        }
        return list;
    }

    private async Task<List<UserLoyaltyTravelPlanViewModel>> LoadTravelPlansAsync(SqlConnection connection, long userId, CancellationToken cancellationToken)
    {
        var list = new List<UserLoyaltyTravelPlanViewModel>();
        await using var command = new SqlCommand(@"
            SELECT TOP (5) p.id, p.[PLAN_ADI], p.[HEDEF_SEHIR], p.[BASLANGIC_TARIHI], p.[BITIS_TARIHI], COALESCE(p.[BUTCE_TUTARI], 0), COALESCE(p.[DURUM], 'Taslak'),
                   COALESCE(COUNT(ps.id), 0) AS secim_sayisi,
                   MAX(p.[GUNCELLENME_TARIHI]) AS son_guncellenme_tarihi
            FROM [dbo].[KULLANICI_SEYAHAT_PLANLARI] p
            LEFT JOIN [dbo].[KULLANICI_SEYAHAT_PLAN_OTEL_SECIMLERI] ps ON ps.[PLAN_ID] = p.id
            WHERE p.[OLUSTURAN_KULLANICI_ID] = @userId
            GROUP BY p.id, p.[PLAN_ADI], p.[HEDEF_SEHIR], p.[BASLANGIC_TARIHI], p.[BITIS_TARIHI], p.[BUTCE_TUTARI], p.[DURUM]
            ORDER BY MAX(p.[GUNCELLENME_TARIHI]) DESC, p.id DESC;", connection);
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
            SELECT TOP (4) [BASLIK], [ACIKLAMA], [KAMPANYA_KODU], COALESCE([BUTON_URL], '/oteller'), [GECERLILIK_BITIS]
            FROM [dbo].[KULLANICI_OZEL_TEKLIFLERI]
            WHERE [AKTIF_MI] = 1
              AND ([KULLANICI_ID] = @userId OR [KULLANICI_ID] IS NULL)
              AND CAST(GETDATE() AS date) BETWEEN [GECERLILIK_BASLANGIC] AND [GECERLILIK_BITIS]
            ORDER BY CASE WHEN [KULLANICI_ID] = @userId THEN 0 ELSE 1 END, [GECERLILIK_BITIS] ASC, id DESC
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
            SELECT TOP (4) [HEDEF_SEHIR], [HEDEF_BUTCE], [GECE_SAYISI], [KISI_SAYISI]
            FROM [dbo].[KULLANICI_BUTCE_PLANLARI]
            WHERE [KULLANICI_ID] = @userId
            ORDER BY [GUNCELLENME_TARIHI] DESC, id DESC;", connection);
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
            SELECT TOP (4) a.[OTEL_ID], o.[OTEL_ADI], a.[HEDEF_MAKSIMUM_FIYAT]
            FROM [dbo].[KULLANICI_FAVORI_FIYAT_ALARMLARI] a
            INNER JOIN [dbo].[OTELLER] o ON o.id = a.[OTEL_ID]
            WHERE a.[KULLANICI_ID] = @userId
              AND COALESCE(a.[AKTIF_MI], 1) = 1
            ORDER BY a.[GUNCELLENME_TARIHI] DESC, a.[ID] DESC;", connection);
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
            SELECT TOP (4) o.[OTEL_ADI], o.[OTEL_KODU], COALESCE(o.[ILCE], ''), COALESCE(o.[SEHIR], ''), COALESCE(o.[ORTALAMA_PUAN], 0), COALESCE(og.[GORSEL_URL], '')
            FROM [dbo].[OTELLER] o
            LEFT JOIN [dbo].[OTEL_GORSELLERI] og ON og.[OTEL_ID] = o.id AND (og.[KAPAK_FOTOGRAFI_MI] = 1 OR og.[SIRALAMA] = 1)
            WHERE o.[YAYIN_DURUMU] = 'Yayında'
              AND o.[ONAY_DURUMU] = 'Onaylandı'
            ORDER BY COALESCE(o.[ORTALAMA_PUAN], 0) DESC, COALESCE(o.[TOPLAM_YORUM_SAYISI], 0) DESC, o.id DESC;", connection);
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
            SELECT TOP (@take)
                   r.id,
                   r.[OTEL_ID],
                   r.[REZERVASYON_NO],
                   o.[OTEL_ADI],
                   o.[OTEL_KODU],
                   COALESCE(o.[ILCE], ''),
                   COALESCE(o.[SEHIR], ''),
                   COALESCE(ot.[ODA_ADI], 'Oda'),
                   COALESCE(NULLIF(r.[MISAFIR_AD_SOYAD], ''), ''),
                   COALESCE(NULLIF(r.[MISAFIR_EPOSTA], ''), ''),
                   COALESCE(NULLIF(r.[MISAFIR_TELEFON], ''), ''),
                   r.[GIRIS_TARIHI],
                   r.[CIKIS_TARIHI],
                   r.[YETISKIN_SAYISI],
                   r.[COCUK_SAYISI],
                   r.[TOPLAM_TUTAR],
                   r.[DURUM],
                   COALESCE(og.[GORSEL_URL], ''),
                   COALESCE(r.[OTEL_ONAY_DURUMU], ''),
                   COALESCE(NULLIF(r.[IPTAL_NEDENI], ''), '') AS [IPTAL_NEDENI],
                   r.[IPTAL_TARIHI],
                   COALESCE(NULLIF(r.[ODEME_DURUMU], ''), '') AS [ODEME_DURUMU],
                   COALESCE(NULLIF(r.[ODEME_YONTEMI], ''), '') AS [ODEME_YONTEMI],
                   COALESCE(NULLIF(r.[KAYNAK], ''), '') AS [KAYNAK],
                   COALESCE(NULLIF(r.[REZERVASYON_KANALI], ''), '') AS [REZERVASYON_KANALI],
                   r.[OLUSTURULMA_TARIHI],
                   COALESCE(NULLIF(r.[MISAFIR_NOTU], ''), '') AS [MISAFIR_NOTU],
                   COALESCE(NULLIF(r.[MUSTERI_TALEP_NOTU], ''), '') AS [MUSTERI_TALEP_NOTU],
                   CAST(CASE WHEN EXISTS (
                       SELECT 1 FROM [dbo].[YORUMLAR] y
                       WHERE y.[REZERVASYON_ID] = r.id AND y.[KULLANICI_ID] = @userId
                   ) THEN 1 ELSE 0 END AS INT) AS has_review
            FROM [dbo].[REZERVASYONLAR] r
            INNER JOIN [dbo].[OTELLER] o ON o.id = r.[OTEL_ID]
            LEFT JOIN [dbo].[ODA_TIPLERI] ot ON ot.id = r.[ODA_TIP_ID]
            LEFT JOIN (
                SELECT ranked.[OTEL_ID], ranked.[GORSEL_URL]
                FROM (
                    SELECT
                        g.[OTEL_ID],
                        g.[GORSEL_URL],
                        ROW_NUMBER() OVER (
                            PARTITION BY g.[OTEL_ID]
                            ORDER BY COALESCE(g.[KAPAK_FOTOGRAFI_MI], 0) DESC, COALESCE(g.[SIRALAMA], 2147483647) ASC, g.id ASC
                        ) AS rn
                    FROM [dbo].[OTEL_GORSELLERI] g
                    WHERE COALESCE(g.[GORSEL_URL], '') <> ''
                ) ranked
                WHERE ranked.rn = 1
            ) og ON og.[OTEL_ID] = o.id
            WHERE r.[KULLANICI_ID] = @userId
            ORDER BY r.[GIRIS_TARIHI] DESC, r.id DESC;";
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@userId", userId);
        command.Parameters.AddWithValue("@take", take);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var tr = CultureInfo.GetCultureInfo("tr-TR");
        while (await reader.ReadAsync(cancellationToken))
        {
            var checkIn = reader.GetDateTime(11);
            var checkOut = reader.GetDateTime(12);
            var status = reader.GetString(16);
            var isCancelled = string.Equals(status, "İptal Edildi", StringComparison.OrdinalIgnoreCase);
            var isUpcoming = checkOut >= DateTime.Today;
            var adultCount = SafeInt(reader, 13);
            var childCount = SafeInt(reader, 14);
            var otelOnay = reader.IsDBNull(18) ? string.Empty : reader.GetString(18);
            var hasReview = SafeInt(reader, 28) != 0;
            var canSubmitReview = CanReservationReceiveReview(status, otelOnay, hasReview, isCancelled, checkOut);
            var canCancel = CanUserCancelReservation(status, otelOnay, checkIn, checkOut);
            list.Add(new UserReservationCardViewModel
            {
                ReservationId = reader.GetInt64(0),
                HotelId = reader.GetInt64(1),
                ReservationNo = reader.GetString(2),
                HotelName = reader.GetString(3),
                HotelSlug = BuildSlug(reader.GetString(3), reader.GetString(4)),
                District = reader.GetString(5),
                City = reader.GetString(6),
                RoomName = reader.GetString(7),
                GuestName = reader.GetString(8),
                GuestEmail = reader.GetString(9),
                GuestPhone = reader.GetString(10),
                StayDateText = $"{checkIn:dd MMM} - {checkOut:dd MMM yyyy}",
                CheckInDate = DateOnly.FromDateTime(checkIn),
                CheckOutDate = DateOnly.FromDateTime(checkOut),
                GuestText = $"{adultCount} Yetiskin" + (childCount > 0 ? $", {childCount} Cocuk" : string.Empty),
                MealOrRoomText = reader.GetString(7),
                StatusText = GetStandardReservationStatusText(status, otelOnay),
                StatusTone = GetReservationStatusTone(status, otelOnay, checkIn),
                SubNote = BuildReservationNote(status, checkIn, checkOut),
                SubNoteTone = isCancelled ? "danger" : isUpcoming ? "info" : "success",
                TotalAmount = SafeDecimal(reader, 15),
                TotalText = FormatMoney(SafeDecimal(reader, 15)),
                ImageUrl = string.IsNullOrWhiteSpace(reader.GetString(17)) ? null : reader.GetString(17),
                CanCancel = canCancel,
                IsUpcoming = isUpcoming,
                IsCancelled = isCancelled,
                CancellationReason = reader.IsDBNull(19) || string.IsNullOrWhiteSpace(reader.GetString(19)) ? null : reader.GetString(19),
                CancellationTimeText = reader.IsDBNull(20) ? null : reader.GetDateTime(20).ToString("dd.MM.yyyy HH:mm", tr),
                PaymentStatus = reader.GetString(21),
                PaymentMethod = reader.GetString(22),
                Source = $"{reader.GetString(23)} / {reader.GetString(24)}",
                CreatedAtText = reader.IsDBNull(25) ? string.Empty : reader.GetDateTime(25).ToLocalTime().ToString("dd.MM.yyyy HH:mm", tr),
                GuestNote = reader.GetString(26),
                RequestNote = reader.GetString(27),
                OtelOnayDurumu = otelOnay,
                CanSubmitReview = canSubmitReview
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
            SELECT r.id,
                   r.[OTEL_ID],
                   r.[REZERVASYON_NO],
                   o.[OTEL_ADI],
                   o.[OTEL_KODU],
                   COALESCE(o.[ILCE], ''),
                   COALESCE(o.[SEHIR], ''),
                   COALESCE(ot.[ODA_ADI], 'Oda'),
                   r.[GIRIS_TARIHI],
                   r.[CIKIS_TARIHI],
                   r.[YETISKIN_SAYISI],
                   r.[COCUK_SAYISI],
                   r.[TOPLAM_TUTAR],
                   r.[DURUM],
                   COALESCE(og.[GORSEL_URL], ''),
                   COALESCE(r.[OTEL_ONAY_DURUMU], ''),
                   COALESCE(NULLIF(r.[IPTAL_NEDENI], ''), '') AS [IPTAL_NEDENI],
                   r.[IPTAL_TARIHI],
                   CAST(CASE WHEN EXISTS (
                       SELECT 1 FROM [dbo].[YORUMLAR] y
                       WHERE y.[REZERVASYON_ID] = r.id AND y.[KULLANICI_ID] = @userId
                   ) THEN 1 ELSE 0 END AS INT) AS has_review
            FROM [dbo].[REZERVASYONLAR] r
            INNER JOIN [dbo].[OTELLER] o ON o.id = r.[OTEL_ID]
            LEFT JOIN [dbo].[ODA_TIPLERI] ot ON ot.id = r.[ODA_TIP_ID]
            LEFT JOIN (
                SELECT ranked.[OTEL_ID], ranked.[GORSEL_URL]
                FROM (
                    SELECT
                        g.[OTEL_ID],
                        g.[GORSEL_URL],
                        ROW_NUMBER() OVER (
                            PARTITION BY g.[OTEL_ID]
                            ORDER BY COALESCE(g.[KAPAK_FOTOGRAFI_MI], 0) DESC, COALESCE(g.[SIRALAMA], 2147483647) ASC, g.id ASC
                        ) AS rn
                    FROM [dbo].[OTEL_GORSELLERI] g
                    WHERE COALESCE(g.[GORSEL_URL], '') <> ''
                ) ranked
                WHERE ranked.rn = 1
            ) og ON og.[OTEL_ID] = o.id
            WHERE r.[KULLANICI_ID] = @userId
              AND (@statusFilter = 'all'
                   OR (@statusFilter = 'completed' AND r.[DURUM] IN ('Onaylandı', 'Tamamlandı'))
                   OR (@statusFilter = 'pending' AND (COALESCE(r.[OTEL_ONAY_DURUMU], '') = 'Beklemede' OR r.[DURUM] IN ('Onay Bekliyor', 'Değişiklik Bekliyor')))
                   OR (@statusFilter = 'rejected' AND (COALESCE(r.[OTEL_ONAY_DURUMU], '') = 'Reddedildi' OR r.[DURUM] IN ('İptal Edildi', 'Reddedildi'))))
              AND (@startDate IS NULL OR CAST(r.[GIRIS_TARIHI] AS date) >= @startDate)
              AND (@endDate IS NULL OR CAST(r.[GIRIS_TARIHI] AS date) <= @endDate)
            ORDER BY r.[GIRIS_TARIHI] DESC, r.id DESC
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
            var checkIn = reader.GetDateTime(8);
            var checkOut = reader.GetDateTime(9);
            var status = reader.GetString(13);
            var isCancelled = string.Equals(status, "İptal Edildi", StringComparison.OrdinalIgnoreCase);
            var isUpcoming = checkOut >= DateTime.Today;
            var adultCount = SafeInt(reader, 10);
            var childCount = SafeInt(reader, 11);
            var otelOnay = reader.IsDBNull(15) ? string.Empty : reader.GetString(15);
            var hasReview = SafeInt(reader, 18) != 0;
            var canSubmitReview = CanReservationReceiveReview(status, otelOnay, hasReview, isCancelled, checkOut);
            var canCancel = CanUserCancelReservation(status, otelOnay, checkIn, checkOut);
            list.Add(new UserReservationCardViewModel
            {
                ReservationId = reader.GetInt64(0),
                HotelId = reader.GetInt64(1),
                ReservationNo = reader.GetString(2),
                HotelName = reader.GetString(3),
                HotelSlug = BuildSlug(reader.GetString(3), reader.GetString(4)),
                District = reader.GetString(5),
                City = reader.GetString(6),
                RoomName = reader.GetString(7),
                StayDateText = $"{checkIn:dd MMM} - {checkOut:dd MMM yyyy}",
                CheckInDate = DateOnly.FromDateTime(checkIn),
                CheckOutDate = DateOnly.FromDateTime(checkOut),
                GuestText = $"{adultCount} Yetiskin" + (childCount > 0 ? $", {childCount} Cocuk" : string.Empty),
                MealOrRoomText = reader.GetString(7),
                StatusText = GetStandardReservationStatusText(status, otelOnay),
                StatusTone = GetReservationStatusTone(status, otelOnay, checkIn),
                SubNote = BuildReservationNote(status, checkIn, checkOut),
                SubNoteTone = isCancelled ? "danger" : isUpcoming ? "info" : "success",
                TotalText = FormatMoney(SafeDecimal(reader, 12)),
                ImageUrl = string.IsNullOrWhiteSpace(reader.GetString(14)) ? null : reader.GetString(14),
                CanCancel = canCancel,
                IsUpcoming = isUpcoming,
                IsCancelled = isCancelled,
                CancellationReason = reader.IsDBNull(16) || string.IsNullOrWhiteSpace(reader.GetString(16)) ? null : reader.GetString(16),
                CancellationTimeText = reader.IsDBNull(17) ? null : reader.GetDateTime(17).ToString("dd.MM.yyyy HH:mm", CultureInfo.GetCultureInfo("tr-TR")),
                OtelOnayDurumu = otelOnay,
                CanSubmitReview = canSubmitReview
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
            FROM [dbo].[REZERVASYONLAR] r
            WHERE r.[KULLANICI_ID] = @userId
              AND (@statusFilter = 'all'
                   OR (@statusFilter = 'completed' AND r.[DURUM] IN ('Onaylandı', 'Tamamlandı'))
                   OR (@statusFilter = 'pending' AND (COALESCE(r.[OTEL_ONAY_DURUMU], '') = 'Beklemede' OR r.[DURUM] IN ('Onay Bekliyor', 'Değişiklik Bekliyor')))
                   OR (@statusFilter = 'rejected' AND (COALESCE(r.[OTEL_ONAY_DURUMU], '') = 'Reddedildi' OR r.[DURUM] IN ('İptal Edildi', 'Reddedildi'))))
              AND (@startDate IS NULL OR CAST(r.[GIRIS_TARIHI] AS date) >= @startDate)
              AND (@endDate IS NULL OR CAST(r.[GIRIS_TARIHI] AS date) <= @endDate);";
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
        return normalized is "completed" or "pending" or "rejected" ? normalized : "all";
    }

    private static string NormalizeDashboardFavoriteSort(string? sort)
    {
        var normalized = (sort ?? string.Empty).Trim().ToLowerInvariant();
        return normalized is "oldest" or "frequent" ? normalized : "newest";
    }

    private async Task<List<UserFavoriteSummaryViewModel>> LoadFavoriteSummariesAsync(SqlConnection connection, long userId, int take, string sort, CancellationToken cancellationToken)
    {
        var list = new List<UserFavoriteSummaryViewModel>();
        const string sql = @"
            SELECT o.[OTEL_ADI],
                   COALESCE(o.[OTEL_KODU], ''),
                   COALESCE(o.[ILCE], ''),
                   COALESCE(o.[SEHIR], ''),
                   COALESCE(o.[ORTALAMA_PUAN], 0),
                   COALESCE(img.[GORSEL_URL], ''),
                   COALESCE(res.reservation_count, 0),
                   uf.[SON_ISLEM_TARIHI]
            FROM [dbo].[KULLANICI_FAVORI_OTELLER] uf
            INNER JOIN [dbo].[OTELLER] o ON o.id = uf.[OTEL_ID]
            OUTER APPLY (
                SELECT TOP (1) og.[GORSEL_URL]
                FROM [dbo].[OTEL_GORSELLERI] og
                WHERE og.[OTEL_ID] = o.id
                ORDER BY CASE WHEN og.[KAPAK_FOTOGRAFI_MI] = 1 THEN 0 ELSE 1 END, og.[SIRALAMA] ASC, og.id ASC
            ) img
            OUTER APPLY (
                SELECT COUNT(*) AS reservation_count
                FROM [dbo].[REZERVASYONLAR] r
                WHERE r.[KULLANICI_ID] = @userId AND r.[OTEL_ID] = o.id
            ) res
            WHERE uf.[KULLANICI_ID] = @userId AND uf.[AKTIF_MI] = 1
            ORDER BY
                CASE WHEN @sort = 'frequent' THEN COALESCE(res.reservation_count, 0) END DESC,
                CASE WHEN @sort = 'oldest' THEN uf.[SON_ISLEM_TARIHI] END ASC,
                CASE WHEN @sort = 'newest' OR @sort = 'frequent' THEN uf.[SON_ISLEM_TARIHI] END DESC,
                uf.id DESC
            OFFSET 0 ROWS FETCH NEXT @take ROWS ONLY;";
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@userId", userId);
        command.Parameters.AddWithValue("@take", take);
        command.Parameters.AddWithValue("@sort", sort);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var reservationCount = SafeInt(reader, 6);
            list.Add(new UserFavoriteSummaryViewModel
            {
                HotelName = reader.GetString(0),
                HotelSlug = BuildSlug(reader.GetString(0), reader.GetString(1)),
                District = reader.GetString(2),
                City = reader.GetString(3),
                RatingText = $"{SafeDecimal(reader, 4):0.0} Puan",
                ImageUrl = string.IsNullOrWhiteSpace(reader.GetString(5)) ? null : reader.GetString(5),
                ReservationCountText = reservationCount > 0 ? $"{reservationCount} rezervasyon" : "Rezervasyon yok",
                AddedDateText = reader.IsDBNull(7) ? string.Empty : reader.GetDateTime(7).ToString("dd.MM.yyyy", CultureInfo.GetCultureInfo("tr-TR"))
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
        => GetStandardReservationStatusText(status, hotelApprovalStatus) switch
        {
            "Reddedildi" => "danger",
            "Tamamlandı" => "completed",
            "Onaylandı" when checkIn <= DateTime.Today.AddDays(2) => "info",
            "Onaylandı" => "ok",
            _ => "wait"
        };

    private static string GetStandardReservationStatusText(string status, string hotelApprovalStatus)
    {
        var normalizedStatus = NormalizeTurkishStatus(status);
        var normalizedApproval = NormalizeTurkishStatus(hotelApprovalStatus);
        if (normalizedApproval == "REDDEDILDI"
            || normalizedStatus is "IPTAL EDILDI" or "REDDEDILDI")
        {
            return "Reddedildi";
        }

        if (normalizedStatus is "TAMAMLANDI" or "GIRIS YAPTI")
        {
            return "Tamamlandı";
        }

        if (normalizedStatus == "ONAYLANDI")
        {
            return "Onaylandı";
        }

        if (normalizedApproval == "BEKLEMEDE"
            || normalizedStatus is "ONAY BEKLIYOR" or "DEGISIKLIK BEKLIYOR" or "")
        {
            return "Bekliyor";
        }

        return "Bekliyor";
    }

    private static bool IsWithinUserCancellationWindow(DateTime checkIn)
    {
        var checkInMoment = checkIn.Date.AddHours(14);
        return DateTime.Now.AddHours(24) < checkInMoment;
    }

    private static bool CanUserCancelReservation(string status, string hotelApprovalStatus, DateTime checkIn, DateTime checkOut)
    {
        if (checkOut.Date < DateTime.Today)
        {
            return false;
        }

        if (string.Equals(status, "İptal Edildi", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!IsWithinUserCancellationWindow(checkIn))
        {
            return false;
        }

        var displayStatus = GetStandardReservationStatusText(status, hotelApprovalStatus);
        if (displayStatus is "Reddedildi" or "Tamamlandı")
        {
            return false;
        }

        return true;
    }

    private static string BuildReservationNote(string status, DateTime checkIn, DateTime checkOut)
    {
        var normalizedStatus = NormalizeTurkishStatus(status);
        if (normalizedStatus == "IPTAL EDILDI") return "Rezervasyon iptal edildi.";
        if (normalizedStatus is "TAMAMLANDI" or "GIRIS YAPTI") return "Konaklama tamamlandı.";
        if (normalizedStatus == "ONAYLANDI" && checkIn <= DateTime.Today.AddDays(2) && checkOut >= DateTime.Today) return "Check-in tarihi yaklaşıyor.";
        if (normalizedStatus == "ONAYLANDI") return "Rezervasyon onaylı ve konaklama planı hazır.";
        return "Rezervasyon otel onay sürecinde.";
    }

    private static string NormalizeTurkishStatus(string? value)
        => (value ?? string.Empty).Trim()
            .Replace('ı', 'i')
            .Replace('ş', 's')
            .Replace('ğ', 'g')
            .Replace('ü', 'u')
            .Replace('ö', 'o')
            .Replace('ç', 'c')
            .ToUpperInvariant()
            .Replace('İ', 'I')
            .Replace('Ş', 'S')
            .Replace('Ğ', 'G')
            .Replace('Ü', 'U')
            .Replace('Ö', 'O')
            .Replace('Ç', 'C');

    private static string NormalizeReservationStatusFilter(string? statusFilter)
    {
        var value = (statusFilter ?? string.Empty).Trim().ToLowerInvariant();
        return value is "upcoming" or "past" or "cancelled" ? value : "all";
    }

    private static string NormalizeReservationSort(string? sort)
    {
        var value = (sort ?? string.Empty).Trim().ToLowerInvariant();
        return value is "oldest" or "price_desc" or "price_asc" ? value : "newest";
    }

    private async Task<ReservationCancellationSnapshot?> LoadReservationCancellationSnapshotAsync(SqlConnection connection, long userId, long reservationId, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT TOP (1) r.id,
                   r.[OTEL_ID],
                   r.[REZERVASYON_NO],
                   COALESCE(NULLIF(r.[MISAFIR_AD_SOYAD], ''), 'Misafir') AS [MISAFIR_AD_SOYAD],
                   r.[GIRIS_TARIHI],
                   r.[CIKIS_TARIHI],
                   COALESCE(r.[TOPLAM_TUTAR], 0) AS [TOPLAM_TUTAR],
                   COALESCE(NULLIF(ot.[ODA_ADI], ''), 'Oda') AS [ODA_ADI],
                   COALESCE(NULLIF(o.[OTEL_ADI], ''), 'Otel') AS [OTEL_ADI]
            FROM [dbo].[REZERVASYONLAR] r
            LEFT JOIN [dbo].[ODA_TIPLERI] ot ON ot.id = r.[ODA_TIP_ID]
            LEFT JOIN [dbo].[OTELLER] o ON o.id = r.[OTEL_ID]
            WHERE r.id = @reservationId
              AND r.[KULLANICI_ID] = @userId;";
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
            SELECT TOP (1) COALESCE(o.[KULLANICI_ID], oks.[KULLANICI_ID], 1),
                   COALESCE(u.[EPOSTA], o.[SATIS_KONTAK_EPOSTA], o.[EPOSTA], 'partner@otelturizm.com'),
                   COALESCE(u.[AD_SOYAD], o.[SATIS_KONTAK_ADI], 'Partner Yetkilisi')
            FROM [dbo].[OTELLER] o
            LEFT JOIN [dbo].[OTEL_KULLANICI_SAHIPLIKLERI] oks ON oks.[OTEL_ID] = o.id AND oks.[AKTIF_MI] = 1
            LEFT JOIN [dbo].[KULLANICILAR] u ON u.id = COALESCE(o.[KULLANICI_ID], oks.[KULLANICI_ID])
            WHERE o.id = @hotelId
            ORDER BY oks.[ANA_SORUMLU_MU] DESC, oks.id ASC;";
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@hotelId", hotelId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            return (reader.IsDBNull(0) ? 1L : reader.GetInt64(0), reader.GetString(1), reader.GetString(2));
        }

        return (1L, "partner@otelturizm.com", "Partner Yetkilisi");
    }

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
        return Convert.ToInt32(result ?? 0, CultureInfo.InvariantCulture) > 0;
    }

    public async Task<UserInvoicesPageViewModel> GetInvoicesAsync(long userId, CancellationToken cancellationToken = default)
    {
        var model = new UserInvoicesPageViewModel();
        await using var connection = await OpenConnectionAsync(cancellationToken);

        const string tableCheckSql = @"
            SELECT
                CASE WHEN OBJECT_ID(N'dbo.REZERVASYON_FATURALARI', N'U') IS NOT NULL THEN 1 ELSE 0 END,
                CASE WHEN OBJECT_ID(N'dbo.FATURALAR', N'U') IS NOT NULL THEN 1 ELSE 0 END;";
        await using (var tableCmd = new SqlCommand(tableCheckSql, connection))
        {
            await using var tableReader = await tableCmd.ExecuteReaderAsync(cancellationToken);
            if (!await tableReader.ReadAsync(cancellationToken))
            {
                model.TableMissing = true;
                return model;
            }

            var hasGuestInvoiceTable = tableReader.GetInt32(0) == 1;
            if (!hasGuestInvoiceTable)
            {
                model.TableMissing = true;
                return model;
            }
        }

        const string sql = @"
            SELECT TOP (120)
                r.id,
                COALESCE(NULLIF(r.[REZERVASYON_NO], ''), CAST(r.id AS nvarchar(30))) AS [REZERVASYON_NO],
                COALESCE(o.[OTEL_ADI], N'Otel') AS [OTEL_ADI],
                r.[GIRIS_TARIHI],
                r.[CIKIS_TARIHI],
                COALESCE(r.[TOPLAM_TUTAR], 0) AS [TOPLAM_TUTAR],
                COALESCE(r.[DURUM], N'') AS [DURUM],
                rf.[GUVENLI_DOSYA_ID],
                rf.[OLUSTURULMA_TARIHI],
                rf.[MIME_TIPI]
            FROM [dbo].[REZERVASYONLAR] r
            INNER JOIN [dbo].[OTELLER] o ON o.id = r.[OTEL_ID]
            LEFT JOIN [dbo].[REZERVASYON_FATURALARI] rf ON rf.[REZERVASYON_ID] = r.id
            WHERE r.[KULLANICI_ID] = @userId
              AND COALESCE(r.[DURUM], '') IN (N'Tamamlandı', N'Onaylandı', N'Konaklama Tamamlandı')
            ORDER BY CASE WHEN rf.[ID] IS NULL THEN 0 ELSE 1 END ASC, r.[CIKIS_TARIHI] DESC, r.id DESC;";

        await using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@userId", userId);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            long? fileId = reader.IsDBNull(7) ? null : Convert.ToInt64(reader.GetValue(7), CultureInfo.InvariantCulture);
            string? downloadUrl = null;
            if (fileId.HasValue && fileId.Value > 0)
            {
                downloadUrl = await _secureFileService.CreateAccessUrlAsync(fileId.Value, userId, "user", cancellationToken);
            }

            var status = reader.IsDBNull(6) ? string.Empty : reader.GetString(6);
            var checkOut = reader.GetDateTime(4);
            model.Rows.Add(new UserInvoiceRowViewModel
            {
                ReservationId = reader.GetInt64(0),
                ReservationNo = reader.GetString(1),
                HotelName = reader.GetString(2),
                StayText = $"{reader.GetDateTime(3):dd.MM.yyyy} – {checkOut:dd.MM.yyyy}",
                TotalText = FormatMoney(SafeDecimal(reader, 5)),
                StatusText = status,
                HasInvoice = fileId.HasValue && fileId.Value > 0,
                DownloadUrl = downloadUrl,
                UploadedAtText = reader.IsDBNull(8) ? null : reader.GetDateTime(8).ToString("dd.MM.yyyy HH:mm", CultureInfo.GetCultureInfo("tr-TR")),
                MimeType = reader.IsDBNull(9) ? null : reader.GetString(9),
                CheckOutDate = checkOut
            });
        }

        return model;
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

    private static string EscapeCsv(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var normalized = value.Replace("\"", "\"\"", StringComparison.Ordinal);
        return $"\"{normalized}\"";
    }
}

