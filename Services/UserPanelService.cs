using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
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
    private readonly string _publicBaseUrl;

    public UserPanelService(
        IConfiguration configuration,
        IMessageCenterService messageCenterService,
        IAddressLookupService addressLookupService,
        IEmailQueueService emailQueueService,
        IHotelPricingReadService hotelPricingReadService,
        ISecureFileService secureFileService)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection tanimli degil.");
        _messageCenterService = messageCenterService;
        _addressLookupService = addressLookupService;
        _emailQueueService = emailQueueService;
        _hotelPricingReadService = hotelPricingReadService;
        _secureFileService = secureFileService;
        _publicBaseUrl = (configuration["App:PublicBaseUrl"] ?? "https://localhost:7223").TrimEnd('/');
    }

    public async Task<(int TotalReservations, int FavoriteCount, int MessageThreads)> GetNavBadgeCountsAsync(long userId, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        const string sql = @"
            SELECT
                (SELECT COUNT(*) FROM rezervasyonlar WHERE kullanici_id = @userId),
                (SELECT COUNT(*) FROM user_favori_oteller WHERE user_id = @userId AND aktif_mi = 1),
                (SELECT COUNT(*) FROM mesaj_konusmalari WHERE misafir_kullanici_id = @userId AND durum <> 'Arşivlendi');";
        await using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@userId", userId);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return (0, 0, 0);
        }

        return (SafeInt(reader, 0), SafeInt(reader, 1), SafeInt(reader, 2));
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

        var normalizedStatus = NormalizeReservationStatusFilter(statusFilter);
        var normalizedSort = NormalizeReservationSort(sort);
        var search = (searchTerm ?? string.Empty).Trim();
        var safePageSize = pageSize is 5 or 10 or 15 or 20 ? pageSize : 5;
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
        if (!string.IsNullOrWhiteSpace(search))
        {
            filtered = filtered.Where(x =>
                x.ReservationNo.Contains(search, StringComparison.OrdinalIgnoreCase)
                || x.HotelName.Contains(search, StringComparison.OrdinalIgnoreCase)
                || x.RoomName.Contains(search, StringComparison.OrdinalIgnoreCase)
                || x.City.Contains(search, StringComparison.OrdinalIgnoreCase)
                || x.District.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        filtered = normalizedSort switch
        {
            "oldest" => filtered.OrderBy(x => x.CheckInDate).ThenBy(x => x.ReservationId),
            "price_desc" => filtered.OrderByDescending(x => x.TotalAmount).ThenByDescending(x => x.CheckInDate),
            "price_asc" => filtered.OrderBy(x => x.TotalAmount).ThenByDescending(x => x.CheckInDate),
            _ => filtered.OrderByDescending(x => x.CheckInDate).ThenByDescending(x => x.ReservationId)
        };

        var filteredList = filtered.ToList();
        model.StatusFilter = normalizedStatus;
        model.StartDateText = startDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        model.EndDateText = endDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        model.PageSize = safePageSize;
        model.SearchTerm = search;
        model.SortFilter = normalizedSort;
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
            UPDATE rezervasyonlar
            SET musteri_talep_notu = NULLIF(@note, ''),
                guncellenme_tarihi = SYSUTCDATETIME()
            WHERE id = @reservationId
              AND kullanici_id = @userId;
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
                r.rezervasyon_no,
                r.otel_id,
                o.otel_adi,
                COALESCE(o.ilce, ''),
                COALESCE(o.sehir, ''),
                COALESCE(ot.oda_adi, 'Oda'),
                r.giris_tarihi,
                r.cikis_tarihi,
                r.durum,
                COALESCE(r.otel_onay_durumu, ''),
                CAST(CASE WHEN EXISTS (
                    SELECT 1 FROM yorumlar y WHERE y.rezervasyon_id = r.id AND y.kullanici_id = @userId
                ) THEN 1 ELSE 0 END AS INT)
            FROM rezervasyonlar r
            INNER JOIN oteller o ON o.id = r.otel_id
            LEFT JOIN oda_tipleri ot ON ot.id = r.oda_tip_id
            WHERE r.id = @reservationId AND r.kullanici_id = @userId;";
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
        var canSubmitReview = CanReservationReceiveReview(status, otelOnay, hasReview, isCancelled);
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
                INSERT INTO yorumlar (
                    otel_id, kullanici_id, rezervasyon_id, rezervasyon_no,
                    genel_puan, temizlik_puani, konfor_puani, konum_puani, personel_puani, fiyat_performans_puani,
                    yorum_metni,
                    onay_durumu, onay_tarihi, dogrulanmis_konaklama, anonim_mi,
                    seyahat_profili, memnuniyet_seviyesi,
                    genel_puan_10, puan_oda_10, puan_konum_10, puan_fiyat_10, puan_personel_10,
                    puan_temizlik_10, puan_sessizlik_10, puan_ulasim_10,
                    olusturulma_tarihi
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
                COALESCE(r.rezervasyon_no, ''),
                r.otel_id,
                o.otel_adi,
                o.otel_kodu,
                COALESCE(o.ilce, ''),
                COALESCE(o.sehir, ''),
                COALESCE(ot.oda_adi, 'Oda'),
                r.giris_tarihi,
                r.cikis_tarihi,
                r.durum,
                COALESCE(r.otel_onay_durumu, ''),
                y.id AS yorum_id,
                COALESCE(y.yorum_metni, '') AS yorum_metni,
                y.olusturulma_tarihi,
                y.guncellenme_tarihi,
                COALESCE(y.onay_durumu, '') AS yorum_durumu,
                COALESCE(CAST(y.genel_puan_10 AS DECIMAL(9,2)), CAST(y.genel_puan AS DECIMAL(9,2)) * 2) AS yorum_puani,
                COALESCE(og.gorsel_url, '')
            FROM rezervasyonlar r
            INNER JOIN oteller o ON o.id = r.otel_id
            LEFT JOIN oda_tipleri ot ON ot.id = r.oda_tip_id
            LEFT JOIN yorumlar y ON y.rezervasyon_id = r.id AND y.kullanici_id = @userId
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
            ORDER BY CASE WHEN y.id IS NULL THEN 0 ELSE 1 END, r.giris_tarihi DESC, r.id DESC;";

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
                var hasReview = !reader.IsDBNull(12);
                var canWriteReview = CanReservationReceiveReview(status, otelOnay, hasReview, isCancelled);
                var createdAt = reader.IsDBNull(14) ? (DateTime?)null : reader.GetDateTime(14);
                var editLimit = createdAt?.AddDays(7);
                var canEdit = hasReview && editLimit.HasValue && DateTime.UtcNow <= editLimit.Value;
                var checkIn = reader.GetDateTime(8);
                var checkOut = reader.GetDateTime(9);
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

        model.WaitingReviewCount = all.Count(x => x.CanWriteReview);
        model.ReviewedCount = all.Count(x => x.HasReview);
        var filtered = normalizedStatus switch
        {
            "waiting" => all.Where(x => x.CanWriteReview).ToList(),
            "reviewed" => all.Where(x => x.HasReview).ToList(),
            _ => all
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
            SELECT r.durum,
                   COALESCE(r.otel_onay_durumu, ''),
                   CAST(CASE WHEN EXISTS (
                       SELECT 1 FROM yorumlar y WHERE y.rezervasyon_id = r.id AND y.kullanici_id = @userId
                   ) THEN 1 ELSE 0 END AS INT)
            FROM rezervasyonlar r
            WHERE r.id = @reservationId AND r.kullanici_id = @userId;
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
        var isCancelled = string.Equals(status, "İptal Edildi", StringComparison.OrdinalIgnoreCase)
                          || string.Equals(status, "Reddedildi", StringComparison.OrdinalIgnoreCase);
        return CanReservationReceiveReview(status, otelOnay, hasReview, isCancelled);
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
                   r.giris_tarihi,
                   r.cikis_tarihi,
                   COALESCE(ot.oda_adi, N'Oda')
            FROM rezervasyonlar r
            LEFT JOIN oda_tipleri ot ON ot.id = r.oda_tip_id
            LEFT JOIN yorumlar y ON y.rezervasyon_id = r.id AND y.kullanici_id = @userId
            WHERE r.kullanici_id = @userId
              AND r.otel_id = @hotelId
              AND y.id IS NULL
              AND r.durum NOT IN (N'İptal Edildi', N'Reddedildi')
              AND CAST(r.cikis_tarihi AS date) < CAST(SYSUTCDATETIME() AS date)
              AND r.durum IN (N'Tamamlandı', N'Giriş Yaptı', N'Onaylandı')
              AND (
                  COALESCE(r.otel_onay_durumu, '') = N'Onaylandı'
                  OR r.durum IN (N'Tamamlandı', N'Giriş Yaptı', N'Onaylandı')
              )
            ORDER BY r.cikis_tarihi DESC, r.id DESC;
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
            UPDATE yorumlar
            SET yorum_metni = @comment,
                guncellenme_tarihi = SYSUTCDATETIME()
            WHERE id = @reviewId
              AND kullanici_id = @userId
              AND olusturulma_tarihi >= DATEADD(DAY, -7, SYSUTCDATETIME());";
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
                SELECT otel_id
                FROM yorumlar
                WHERE id = @reviewId
                  AND kullanici_id = @userId
                  AND olusturulma_tarihi >= DATEADD(DAY, -7, SYSUTCDATETIME());";
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

            await using (var delete = new SqlCommand("DELETE FROM yorumlar WHERE id = @reviewId AND kullanici_id = @userId;", connection, (SqlTransaction)transaction))
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

    private static bool CanReservationReceiveReview(string status, string otelOnay, bool hasReview, bool isCancelled)
    {
        if (isCancelled || hasReview)
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
        y.otel_id,
        COUNT(*) AS cnt,
        CAST(ROUND(AVG(CAST(COALESCE(CAST(y.genel_puan_10 AS DECIMAL(9, 4)),
            CASE
                WHEN y.genel_puan <= 5 THEN CAST(y.genel_puan AS DECIMAL(9, 4)) * 2
                WHEN y.genel_puan <= 10 THEN CAST(y.genel_puan AS DECIMAL(9, 4))
                ELSE 10
            END) AS DECIMAL(9, 4))), 2) AS DECIMAL(5, 2)) AS avg_genel,
        CAST(ROUND(AVG(CAST(COALESCE(CAST(y.puan_konum_10 AS DECIMAL(9, 4)),
            CASE
                WHEN y.konum_puani <= 5 THEN CAST(y.konum_puani AS DECIMAL(9, 4)) * 2
                WHEN y.konum_puani <= 10 THEN CAST(y.konum_puani AS DECIMAL(9, 4))
                ELSE 10
            END) AS DECIMAL(9, 4))), 2) AS DECIMAL(5, 2)) AS avg_konum,
        CAST(ROUND(AVG(CAST(COALESCE(CAST(y.puan_temizlik_10 AS DECIMAL(9, 4)), CAST(y.puan_oda_10 AS DECIMAL(9, 4)),
            CASE
                WHEN y.temizlik_puani <= 5 THEN CAST(y.temizlik_puani AS DECIMAL(9, 4)) * 2
                WHEN y.temizlik_puani <= 10 THEN CAST(y.temizlik_puani AS DECIMAL(9, 4))
                WHEN y.konfor_puani <= 5 THEN CAST(y.konfor_puani AS DECIMAL(9, 4)) * 2
                WHEN y.konfor_puani <= 10 THEN CAST(y.konfor_puani AS DECIMAL(9, 4))
                ELSE 10
            END) AS DECIMAL(9, 4))), 2) AS DECIMAL(5, 2)) AS avg_konfor,
        CAST(ROUND(AVG(CAST(COALESCE(CAST(y.puan_fiyat_10 AS DECIMAL(9, 4)),
            CASE
                WHEN y.fiyat_performans_puani <= 5 THEN CAST(y.fiyat_performans_puani AS DECIMAL(9, 4)) * 2
                WHEN y.fiyat_performans_puani <= 10 THEN CAST(y.fiyat_performans_puani AS DECIMAL(9, 4))
                ELSE 10
            END) AS DECIMAL(9, 4))), 2) AS DECIMAL(5, 2)) AS avg_fp
    FROM yorumlar AS y
    WHERE y.otel_id = @hotelId
      AND y.onay_durumu LIKE N'Onaylan%'
    GROUP BY y.otel_id
)
UPDATE o
SET
    o.toplam_yorum_sayisi = agg.cnt,
    o.ortalama_puan = agg.avg_genel,
    o.konum_puani = agg.avg_konum,
    o.konfor_puani = agg.avg_konfor,
    o.fiyat_performans_puani = agg.avg_fp
FROM oteller AS o
INNER JOIN agg ON agg.otel_id = o.id;";
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
        const string sql = @"
            SELECT TOP (1) ad_soyad, eposta, COALESCE(telefon, ''), tc_kimlik_no, dogum_tarihi, cinsiyet, uyruk, adres, sehir, ilce, mahalle, posta_kodu,
                   tercih_edilen_oda_tipi, yatak_tercihi, konusulan_diller, seyahat_amaci, ozel_istekler,
                   email_dogrulama_tarihi,
                   COALESCE(NULLIF(profil_resim_url, ''), '') AS profil_resim_url
            FROM users
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

            model.EmailVerified = emailVerifiedAt.HasValue;
            model.EmailVerifiedAtText = emailVerifiedAt.HasValue
                ? emailVerifiedAt.Value.ToLocalTime().ToString("dd.MM.yyyy HH:mm", CultureInfo.GetCultureInfo("tr-TR"))
                : "Onay bekliyor";

            model.ProfileImageUrl = await ResolveProfileImageUrlAsync(userId, profileImageUrl, cancellationToken);
        }

        model.Countries = (await _addressLookupService.GetCountriesAsync(cancellationToken)).ToList();
        model.Provinces = (await _addressLookupService.GetProvincesAsync(cancellationToken)).ToList();
        model.PresetAvatarUrls = BuildPresetAvatarUrls();
        model.UploadedProfileAvatars = await LoadUploadedProfileAvatarsAsync(connection, userId, cancellationToken);
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

    public async Task<string> GetProfileImageUrlAsync(long userId, CancellationToken cancellationToken = default)
    {
        if (userId <= 0)
        {
            return "/uploads/demo/avatars/avatar-01.svg";
        }

        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = new SqlCommand("""
            SELECT TOP (1) COALESCE(NULLIF(profil_resim_url, ''), '')
            FROM users
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

    private async Task<List<UserUploadedProfileAvatarViewModel>> LoadUploadedProfileAvatarsAsync(SqlConnection connection, long userId, CancellationToken cancellationToken)
    {
        var list = new List<UserUploadedProfileAvatarViewModel>();
        await using var command = new SqlCommand("""
            SELECT TOP (3)
                   g.id,
                   COALESCE(g.orijinal_dosya_adi, 'Profil görseli') AS original_name,
                   g.olusturulma_tarihi,
                   CASE WHEN u.profil_resim_url = CONCAT('secure:', CONVERT(varchar(30), g.id)) THEN 1 ELSE 0 END AS is_current
            FROM guvenli_dosya_varliklari g
            INNER JOIN users u ON u.id = @userId
            WHERE g.sahibi_kullanici_id = @userId
              AND g.kategori = N'profile'
              AND g.gorsel_mi = 1
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
            SELECT TOP (1) COALESCE(eposta, ''), email_dogrulama_tarihi
            FROM users
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

        string existingPhone = string.Empty;
        string existingPhoneE164 = string.Empty;
        await using (var snapshotCommand = new SqlCommand("SELECT TOP (1) COALESCE(telefon, ''), COALESCE(telefon_e164, '') FROM users WHERE id = @userId;", connection))
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

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            await using var command = new SqlCommand(@"
                UPDATE users
                SET ad_soyad = @fullName,
                    eposta = @email,
                    telefon = NULLIF(@phone, ''),
                    telefon_e164 = NULLIF(@phoneE164, ''),
                    telefon_dogrulama_kanali = CASE WHEN NULLIF(@phoneE164, '') IS NULL THEN NULL ELSE 'whatsapp' END,
                    telefon_dogrulama_durumu = CASE
                        WHEN NULLIF(@phoneE164, '') IS NULL THEN NULL
                        WHEN @phoneChanged = 1 THEN 'Dogrulanmadi'
                        ELSE telefon_dogrulama_durumu
                    END,
                    telefon_degistirilme_tarihi = CASE WHEN @phoneChanged = 1 THEN SYSUTCDATETIME() ELSE telefon_degistirilme_tarihi END,
                    telefon_dogrulama_tarihi = CASE WHEN @phoneChanged = 1 THEN NULL ELSE telefon_dogrulama_tarihi END,
                    telefon_son_sahiplik_teyit_tarihi = CASE WHEN @phoneChanged = 1 THEN NULL ELSE telefon_son_sahiplik_teyit_tarihi END,
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
                    profil_tamamlanma_tarihi = CURRENT_TIMESTAMP,
                    guncellenme_tarihi = SYSUTCDATETIME()
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
            command.Parameters.AddWithValue("@userId", userId);
            var affected = await command.ExecuteNonQueryAsync(cancellationToken) > 0;

            if (affected && phoneChanged && (!string.IsNullOrWhiteSpace(existingPhone) || !string.IsNullOrWhiteSpace(existingPhoneE164)))
            {
                await using var historyCommand = new SqlCommand(@"
                    INSERT INTO kullanici_telefon_gecmisi
                    (
                        kullanici_id, onceki_telefon_raw, onceki_telefon_e164, yeni_telefon_raw, yeni_telefon_e164,
                        dogrulama_durumu, degisim_nedeni, olusturulma_tarihi
                    )
                    VALUES
                    (
                        @userId, NULLIF(@oldPhone, ''), NULLIF(@oldPhoneE164, ''), NULLIF(@newPhone, ''), NULLIF(@newPhoneE164, ''),
                        @status, N'Profil ekranından telefon güncellendi', SYSUTCDATETIME()
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
            UPDATE users
            SET tercih_edilen_oda_tipi = NULLIF(@roomPreference, ''),
                yatak_tercihi = NULLIF(@bedPreference, ''),
                konusulan_diller = NULLIF(@spokenLanguages, ''),
                seyahat_amaci = NULLIF(@travelPurpose, ''),
                ozel_istekler = NULLIF(@specialRequests, ''),
                guncellenme_tarihi = SYSUTCDATETIME()
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
            SELECT TOP (1) COALESCE(ad_soyad, ''), COALESCE(eposta, ''), email_dogrulama_tarihi
            FROM users
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

        await using (var duplicateCheckCommand = new SqlCommand("SELECT COUNT(*) FROM users WHERE eposta = @email AND id <> @userId;", connection))
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
            SELECT TOP (1) olusturulma_tarihi
            FROM email_dogrulama_tokenlari
            WHERE kullanici_id = @userId
              AND kullanildi_mi = 0
            ORDER BY olusturulma_tarihi DESC;
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
                UPDATE email_dogrulama_tokenlari
                SET kullanildi_mi = 1,
                    kullanilma_tarihi = SYSUTCDATETIME()
                WHERE kullanici_id = @userId
                  AND kullanildi_mi = 0;
                """, connection, (SqlTransaction)transaction))
            {
                invalidateCommand.Parameters.AddWithValue("@userId", userId);
                await invalidateCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            await using (var insertCommand = new SqlCommand("""
                INSERT INTO email_dogrulama_tokenlari
                (kullanici_id, eposta, token, dogrulama_kodu, kullanildi_mi, deneme_sayisi, maksimum_deneme, ip_adresi, user_agent, gecerlilik_suresi, olusturulma_tarihi)
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
                    RelatedTable = "users",
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
            SELECT TOP (1) id, gecerlilik_suresi, kullanildi_mi, deneme_sayisi, maksimum_deneme, token
            FROM email_dogrulama_tokenlari
            WHERE kullanici_id = @userId
              AND eposta = @email
              AND dogrulama_kodu = @code
            ORDER BY olusturulma_tarihi DESC;
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

        await using (var duplicateCheckCommand = new SqlCommand("SELECT COUNT(*) FROM users WHERE eposta = @email AND id <> @userId;", connection, (SqlTransaction)transaction))
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
            UPDATE users
            SET eposta = @email,
                email_dogrulama_tarihi = COALESCE(email_dogrulama_tarihi, SYSUTCDATETIME()),
                email_dogrulama_son_gonderim_tarihi = SYSUTCDATETIME(),
                guncellenme_tarihi = SYSUTCDATETIME()
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
            UPDATE users
            SET profil_resim_url = @url,
                profil_resim_kaynak = NULLIF(@source, ''),
                guncellenme_tarihi = SYSUTCDATETIME()
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
            UPDATE users
            SET profil_resim_url = NULL,
                profil_resim_kaynak = NULL,
                guncellenme_tarihi = SYSUTCDATETIME()
            WHERE id = @userId
              AND profil_resim_url = @secureValue;
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
                FROM guvenli_dosya_varliklari g
                WHERE g.sahibi_kullanici_id = @userId
                  AND g.kategori = N'profile'
                  AND g.gorsel_mi = 1
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
            UPDATE email_dogrulama_tokenlari
            SET kullanildi_mi = 1,
                kullanilma_tarihi = SYSUTCDATETIME()
            WHERE id = @tokenId;
            """, connection, transaction);
        command.Parameters.AddWithValue("@tokenId", tokenId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task IncrementEmailTokenAttemptAsync(SqlConnection connection, SqlTransaction transaction, long tokenId, CancellationToken cancellationToken)
    {
        await using var command = new SqlCommand("""
            UPDATE email_dogrulama_tokenlari
            SET deneme_sayisi = COALESCE(deneme_sayisi, 0) + 1
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
            SELECT rezervasyon_eposta, rezervasyon_push, checkin_hatirlatma, iptal_degisim, kampanya_eposta, kampanya_sms, sistem_bildirimi, COALESCE(giris_eposta, 0)
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
                    SystemNotifications = SafeBool(reader, 6),
                    LoginEmail = SafeBool(reader, 7)
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
        await EnsureNotificationPreferencesSchemaAsync(connection, cancellationToken);
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
                    giris_eposta = @loginEmail,
                    guncellenme_tarihi = CURRENT_TIMESTAMP
                WHERE kullanici_id = @userId;
            END
            ELSE
            BEGIN
                INSERT INTO kullanici_bildirim_tercihleri
                (kullanici_id, rezervasyon_eposta, rezervasyon_push, checkin_hatirlatma, iptal_degisim, kampanya_eposta, kampanya_sms, sistem_bildirimi, giris_eposta, guncellenme_tarihi)
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
            IF OBJECT_ID(N'dbo.kullanici_bildirim_tercihleri', N'U') IS NOT NULL
               AND COL_LENGTH(N'dbo.kullanici_bildirim_tercihleri', N'giris_eposta') IS NULL
            BEGIN
                ALTER TABLE dbo.kullanici_bildirim_tercihleri
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
                COALESCE(iki_asamali_dogrulama_aktif_mi, 0),
                COALESCE(iki_asamali_dogrulama_kanali, 'email'),
                COALESCE(eposta, ''),
                email_dogrulama_tarihi,
                COALESCE(telefon_e164, ''),
                telefon_dogrulama_tarihi
            FROM users
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
                COALESCE(s.cihaz_etiketi, 'Bilinmeyen cihaz') AS cihaz,
                s.beni_hatirla_tercihi,
                s.toplam_oturum_suresi_saniye,
                s.son_aktivite_tarihi,
                l.ip_adresi,
                l.giris_tarihi
            FROM kullanici_oturum_istatistikleri s
            OUTER APPLY
            (
                SELECT TOP (1) ip_adresi, giris_tarihi
                FROM kullanici_giris_loglari
                WHERE kullanici_id = s.kullanici_id
                  AND hesap_tipi = 'user'
                ORDER BY giris_tarihi DESC
            ) l
            WHERE s.kullanici_id = @userId
              AND s.hesap_tipi = 'user'
            ORDER BY s.son_aktivite_tarihi DESC, s.id DESC;", connection);
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
        var channel = NormalizeTwoFactorChannel(form.Channel);
        if (form.Enabled)
        {
            await using var validationCommand = new SqlCommand("""
                SELECT TOP (1)
                    COALESCE(eposta, ''),
                    email_dogrulama_tarihi,
                    COALESCE(telefon_e164, ''),
                    telefon_dogrulama_tarihi
                FROM users
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
            UPDATE users
            SET iki_asamali_dogrulama_aktif_mi = @enabled,
                iki_asamali_dogrulama_kanali = @channel
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
        await EnsurePassportCitiesAsync(connection, userId, cancellationToken);
        model.PassportCities = await LoadPassportCitiesAsync(connection, userId, cancellationToken);
        model.RecentReservationCities = await LoadRecentReservationCitiesAsync(connection, userId, 5, cancellationToken);
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

    private static async Task EnsurePassportCitiesAsync(SqlConnection connection, long userId, CancellationToken cancellationToken)
    {
        const string sql = """
            MERGE kullanici_dijital_pasaportlari AS target
            USING
            (
                SELECT
                    @userId AS kullanici_id,
                    LTRIM(RTRIM(COALESCE(o.sehir, ''))) AS sehir,
                    CAST(MIN(r.giris_tarihi) AS date) AS ilk_konaklama_tarihi,
                    CAST(MAX(r.cikis_tarihi) AS date) AS son_konaklama_tarihi,
                    COUNT(*) AS toplam_konaklama_sayisi
                FROM rezervasyonlar r
                INNER JOIN oteller o ON o.id = r.otel_id
                WHERE r.kullanici_id = @userId
                  AND COALESCE(NULLIF(o.sehir, ''), '') <> ''
                  AND COALESCE(NULLIF(r.durum, ''), '') <> 'İptal Edildi'
                GROUP BY LTRIM(RTRIM(COALESCE(o.sehir, '')))
            ) AS source
            ON target.kullanici_id = source.kullanici_id
               AND target.sehir = source.sehir
            WHEN MATCHED THEN
                UPDATE SET
                    ilk_konaklama_tarihi = CASE
                        WHEN target.ilk_konaklama_tarihi IS NULL THEN source.ilk_konaklama_tarihi
                        WHEN source.ilk_konaklama_tarihi < target.ilk_konaklama_tarihi THEN source.ilk_konaklama_tarihi
                        ELSE target.ilk_konaklama_tarihi
                    END,
                    son_konaklama_tarihi = CASE
                        WHEN target.son_konaklama_tarihi IS NULL THEN source.son_konaklama_tarihi
                        WHEN source.son_konaklama_tarihi > target.son_konaklama_tarihi THEN source.son_konaklama_tarihi
                        ELSE target.son_konaklama_tarihi
                    END,
                    toplam_konaklama_sayisi = source.toplam_konaklama_sayisi,
                    guncellenme_tarihi = SYSUTCDATETIME()
            WHEN NOT MATCHED THEN
                INSERT (kullanici_id, sehir, ulke, ilk_konaklama_tarihi, son_konaklama_tarihi, toplam_konaklama_sayisi, olusturulma_tarihi, guncellenme_tarihi)
                VALUES (source.kullanici_id, source.sehir, N'Türkiye', source.ilk_konaklama_tarihi, source.son_konaklama_tarihi, source.toplam_konaklama_sayisi, SYSUTCDATETIME(), SYSUTCDATETIME());
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
                   COALESCE(NULLIF(o.sehir, ''), '') AS sehir,
                   r.giris_tarihi
            FROM rezervasyonlar r
            INNER JOIN oteller o ON o.id = r.otel_id
            WHERE r.kullanici_id = @userId
              AND COALESCE(NULLIF(o.sehir, ''), '') <> ''
            ORDER BY r.giris_tarihi DESC, r.id DESC;
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
            SELECT TOP (5) p.id, p.plan_adi, p.hedef_sehir, p.baslangic_tarihi, p.bitis_tarihi, COALESCE(p.butce_tutari, 0), COALESCE(p.durum, 'Taslak'),
                   COALESCE(COUNT(ps.id), 0) AS secim_sayisi,
                   MAX(p.guncellenme_tarihi) AS son_guncellenme_tarihi
            FROM kullanici_seyahat_planlari p
            LEFT JOIN kullanici_seyahat_plan_otel_secimleri ps ON ps.plan_id = p.id
            WHERE p.olusturan_kullanici_id = @userId
            GROUP BY p.id, p.plan_adi, p.hedef_sehir, p.baslangic_tarihi, p.bitis_tarihi, p.butce_tutari, p.durum
            ORDER BY MAX(p.guncellenme_tarihi) DESC, p.id DESC;", connection);
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
            SELECT TOP (@take)
                   r.id,
                   r.otel_id,
                   r.rezervasyon_no,
                   o.otel_adi,
                   o.otel_kodu,
                   COALESCE(o.ilce, ''),
                   COALESCE(o.sehir, ''),
                   COALESCE(ot.oda_adi, 'Oda'),
                   COALESCE(NULLIF(r.misafir_ad_soyad, ''), ''),
                   COALESCE(NULLIF(r.misafir_eposta, ''), ''),
                   COALESCE(NULLIF(r.misafir_telefon, ''), ''),
                   r.giris_tarihi,
                   r.cikis_tarihi,
                   r.yetiskin_sayisi,
                   r.cocuk_sayisi,
                   r.toplam_tutar,
                   r.durum,
                   COALESCE(og.gorsel_url, ''),
                   COALESCE(r.otel_onay_durumu, ''),
                   COALESCE(NULLIF(r.iptal_nedeni, ''), '') AS iptal_nedeni,
                   r.iptal_tarihi,
                   COALESCE(NULLIF(r.odeme_durumu, ''), '') AS odeme_durumu,
                   COALESCE(NULLIF(r.odeme_yontemi, ''), '') AS odeme_yontemi,
                   COALESCE(NULLIF(r.kaynak, ''), '') AS kaynak,
                   COALESCE(NULLIF(r.rezervasyon_kanali, ''), '') AS rezervasyon_kanali,
                   r.olusturulma_tarihi,
                   COALESCE(NULLIF(r.misafir_notu, ''), '') AS misafir_notu,
                   COALESCE(NULLIF(r.musteri_talep_notu, ''), '') AS musteri_talep_notu,
                   CAST(CASE WHEN EXISTS (
                       SELECT 1 FROM yorumlar y
                       WHERE y.rezervasyon_id = r.id AND y.kullanici_id = @userId
                   ) THEN 1 ELSE 0 END AS INT) AS has_review
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
            var canSubmitReview = CanReservationReceiveReview(status, otelOnay, hasReview, isCancelled);
            var canCancel = CanUserCancelReservation(status, otelOnay, checkOut);
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
                   r.otel_id,
                   r.rezervasyon_no,
                   o.otel_adi,
                   o.otel_kodu,
                   COALESCE(o.ilce, ''),
                   COALESCE(o.sehir, ''),
                   COALESCE(ot.oda_adi, 'Oda'),
                   r.giris_tarihi,
                   r.cikis_tarihi,
                   r.yetiskin_sayisi,
                   r.cocuk_sayisi,
                   r.toplam_tutar,
                   r.durum,
                   COALESCE(og.gorsel_url, ''),
                   COALESCE(r.otel_onay_durumu, ''),
                   COALESCE(NULLIF(r.iptal_nedeni, ''), '') AS iptal_nedeni,
                   r.iptal_tarihi,
                   CAST(CASE WHEN EXISTS (
                       SELECT 1 FROM yorumlar y
                       WHERE y.rezervasyon_id = r.id AND y.kullanici_id = @userId
                   ) THEN 1 ELSE 0 END AS INT) AS has_review
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
                   OR (@statusFilter = 'completed' AND r.durum IN ('Onaylandı', 'Tamamlandı'))
                   OR (@statusFilter = 'pending' AND (COALESCE(r.otel_onay_durumu, '') = 'Beklemede' OR r.durum IN ('Onay Bekliyor', 'Değişiklik Bekliyor')))
                   OR (@statusFilter = 'rejected' AND (COALESCE(r.otel_onay_durumu, '') = 'Reddedildi' OR r.durum IN ('İptal Edildi', 'Reddedildi'))))
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
            var checkIn = reader.GetDateTime(8);
            var checkOut = reader.GetDateTime(9);
            var status = reader.GetString(13);
            var isCancelled = string.Equals(status, "İptal Edildi", StringComparison.OrdinalIgnoreCase);
            var isUpcoming = checkOut >= DateTime.Today;
            var adultCount = SafeInt(reader, 10);
            var childCount = SafeInt(reader, 11);
            var otelOnay = reader.IsDBNull(15) ? string.Empty : reader.GetString(15);
            var hasReview = SafeInt(reader, 18) != 0;
            var canSubmitReview = CanReservationReceiveReview(status, otelOnay, hasReview, isCancelled);
            var canCancel = CanUserCancelReservation(status, otelOnay, checkOut);
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
            FROM rezervasyonlar r
            WHERE r.kullanici_id = @userId
              AND (@statusFilter = 'all'
                   OR (@statusFilter = 'completed' AND r.durum IN ('Onaylandı', 'Tamamlandı'))
                   OR (@statusFilter = 'pending' AND (COALESCE(r.otel_onay_durumu, '') = 'Beklemede' OR r.durum IN ('Onay Bekliyor', 'Değişiklik Bekliyor')))
                   OR (@statusFilter = 'rejected' AND (COALESCE(r.otel_onay_durumu, '') = 'Reddedildi' OR r.durum IN ('İptal Edildi', 'Reddedildi'))))
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
            SELECT o.otel_adi,
                   COALESCE(o.otel_kodu, ''),
                   COALESCE(o.ilce, ''),
                   COALESCE(o.sehir, ''),
                   COALESCE(o.ortalama_puan, 0),
                   COALESCE(img.gorsel_url, ''),
                   COALESCE(res.reservation_count, 0),
                   uf.son_islem_tarihi
            FROM user_favori_oteller uf
            INNER JOIN oteller o ON o.id = uf.otel_id
            OUTER APPLY (
                SELECT TOP (1) og.gorsel_url
                FROM otel_gorselleri og
                WHERE og.otel_id = o.id
                ORDER BY CASE WHEN og.kapak_fotografi_mi = 1 THEN 0 ELSE 1 END, og.siralama ASC, og.id ASC
            ) img
            OUTER APPLY (
                SELECT COUNT(*) AS reservation_count
                FROM rezervasyonlar r
                WHERE r.kullanici_id = @userId AND r.otel_id = o.id
            ) res
            WHERE uf.user_id = @userId AND uf.aktif_mi = 1
            ORDER BY
                CASE WHEN @sort = 'frequent' THEN COALESCE(res.reservation_count, 0) END DESC,
                CASE WHEN @sort = 'oldest' THEN uf.son_islem_tarihi END ASC,
                CASE WHEN @sort = 'newest' OR @sort = 'frequent' THEN uf.son_islem_tarihi END DESC,
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
        => status switch
        {
            "İptal Edildi" => "danger",
            "Reddedildi" => "danger",
            "Tamamlandı" => "completed",
            "Giriş Yaptı" => "completed",
            "Onaylandı" when checkIn <= DateTime.Today.AddDays(2) => "info",
            "Onaylandı" => "ok",
            "Onay Bekliyor" => "wait",
            "Değişiklik Bekliyor" => "wait",
            _ when string.Equals(hotelApprovalStatus, "Beklemede", StringComparison.OrdinalIgnoreCase) => "wait",
            _ when string.Equals(hotelApprovalStatus, "Reddedildi", StringComparison.OrdinalIgnoreCase) => "danger",
            _ => "wait"
        };

    private static string GetStandardReservationStatusText(string status, string hotelApprovalStatus)
    {
        if (string.Equals(hotelApprovalStatus, "Reddedildi", StringComparison.OrdinalIgnoreCase)
            || string.Equals(status, "İptal Edildi", StringComparison.OrdinalIgnoreCase)
            || string.Equals(status, "Reddedildi", StringComparison.OrdinalIgnoreCase))
        {
            return "Reddedildi";
        }

        if (string.Equals(status, "Tamamlandı", StringComparison.OrdinalIgnoreCase)
            || string.Equals(status, "Giriş Yaptı", StringComparison.OrdinalIgnoreCase)
            || string.Equals(status, "Onaylandı", StringComparison.OrdinalIgnoreCase))
        {
            return "Tamamlandı";
        }

        if (string.Equals(hotelApprovalStatus, "Beklemede", StringComparison.OrdinalIgnoreCase)
            || string.Equals(status, "Onay Bekliyor", StringComparison.OrdinalIgnoreCase)
            || string.Equals(status, "Değişiklik Bekliyor", StringComparison.OrdinalIgnoreCase))
        {
            return "Bekliyor";
        }

        return "Tamamlandı";
    }

    private static bool CanUserCancelReservation(string status, string hotelApprovalStatus, DateTime checkOut)
    {
        if (checkOut < DateTime.Today)
        {
            return false;
        }

        if (string.Equals(hotelApprovalStatus, "Reddedildi", StringComparison.OrdinalIgnoreCase)
            || string.Equals(status, "İptal Edildi", StringComparison.OrdinalIgnoreCase)
            || string.Equals(status, "Reddedildi", StringComparison.OrdinalIgnoreCase)
            || string.Equals(status, "Tamamlandı", StringComparison.OrdinalIgnoreCase)
            || string.Equals(status, "Giriş Yaptı", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return true;
    }

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

    private static string NormalizeReservationSort(string? sort)
    {
        var value = (sort ?? string.Empty).Trim().ToLowerInvariant();
        return value is "oldest" or "price_desc" or "price_asc" ? value : "newest";
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
