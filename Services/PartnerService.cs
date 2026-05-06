using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.SqlClient;
using SqlConnection = Microsoft.Data.SqlClient.SqlConnection;
using SqlCommand = Microsoft.Data.SqlClient.SqlCommand;
using SqlTransaction = Microsoft.Data.SqlClient.SqlTransaction;
using SqlException = Microsoft.Data.SqlClient.SqlException;
using otelturizmnew.Models.Email;
using otelturizmnew.Models.Messages;
using otelturizmnew.Models.Paneller.Partner;
using otelturizmnew.Pricing;
using otelturizmnew.Services.Abstractions;
using otelturizmnew.Utils;

namespace otelturizmnew.Services;

public class PartnerService : IPartnerService
{
    private readonly string _connectionString;
    private readonly string _publicBaseUrl;
    private readonly IWebHostEnvironment _environment;
    private readonly IImageStorageService _imageStorageService;
    private readonly IEmailQueueService _emailQueueService;
    private readonly IFavoritePriceAlertService _favoritePriceAlertService;
    private readonly ISecureFileService _secureFileService;
    private readonly ISlowSqlTracker _slowSql;
    private readonly ILogger<PartnerService> _logger;

    public PartnerService(
        IConfiguration configuration,
        IWebHostEnvironment environment,
        IImageStorageService imageStorageService,
        IEmailQueueService emailQueueService,
        IFavoritePriceAlertService favoritePriceAlertService,
        ISecureFileService secureFileService,
        ISlowSqlTracker slowSql,
        ILogger<PartnerService> logger)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection tanimli degil.");
        _publicBaseUrl = (configuration["App:PublicBaseUrl"] ?? "https://otelturizm.com").TrimEnd('/');
        _environment = environment;
        _imageStorageService = imageStorageService;
        _emailQueueService = emailQueueService;
        _favoritePriceAlertService = favoritePriceAlertService;
        _secureFileService = secureFileService;
        _slowSql = slowSql;
        _logger = logger;
    }

    public async Task<PartnerDashboardViewModel> GetDashboardAsync(long userId, long? hotelId = null, DateTime? dateFrom = null, DateTime? dateTo = null, string? status = null, string? paymentMethod = null, int pageSize = 7, long? conversationId = null, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var context = await BuildContextAsync(connection, userId, hotelId, "Dashboard", "Otel, fiyat, rezervasyon ve yorum operasyonlarini tek ekranda yonetin.", "dashboard", cancellationToken);
        var normalizedStatus = NormalizeReservationStatusFilter(status);
        var normalizedPaymentMethod = NormalizeReservationPaymentFilter(paymentMethod);
        var normalizedPageSize = Math.Clamp(pageSize <= 0 ? 7 : pageSize, 7, 30);
        var model = new PartnerDashboardViewModel
        {
            Shell = context.Shell,
            DashboardPageSize = normalizedPageSize,
            Filters = new PartnerReservationFilterViewModel
            {
                DateFrom = dateFrom?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                DateTo = dateTo?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                Status = normalizedStatus,
                PaymentMethod = normalizedPaymentMethod,
                Page = 1,
                PageSize = normalizedPageSize
            }
        };

        const string metricsSql = @"
            SELECT
                (SELECT COUNT(*) FROM otel_kullanici_sahiplikleri oks WHERE oks.user_id = @userId AND oks.aktif_mi = 1) AS managed_hotels,
                (SELECT COUNT(*) FROM rezervasyonlar r WHERE r.otel_id = @hotelId) AS total_reservations,
                (SELECT COALESCE(SUM(r.toplam_tutar), 0) FROM rezervasyonlar r WHERE r.otel_id = @hotelId AND r.durum IN ('Onaylandı','Tamamlandı')) AS total_revenue,
                (SELECT COALESCE(AVG(y.genel_puan), 0) FROM yorumlar y WHERE y.otel_id = @hotelId AND y.onay_durumu = 'Onaylandı') AS average_score;";

        await using (var command = new SqlCommand(metricsSql, connection))
        {
            command.Parameters.AddWithValue("@userId", userId);
            command.Parameters.AddWithValue("@hotelId", context.SelectedHotel.HotelId);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                model.SummaryCards.Add(new PartnerStatCardViewModel { Label = "Yonetilen Oteller", Value = SafeInt(reader, 0).ToString(), Description = "Aktif sahiplik kayitlari", IconClass = "fa-hotel", ToneClass = "info" });
                model.SummaryCards.Add(new PartnerStatCardViewModel { Label = "Toplam Rezervasyon", Value = SafeInt(reader, 1).ToString(), Description = "Secili otelin tum rezervasyonlari", IconClass = "fa-calendar-check", ToneClass = "success" });
                model.SummaryCards.Add(new PartnerStatCardViewModel { Label = "Brut Gelir", Value = FormatMoney(SafeDecimal(reader, 2)), Description = "Onayli ve tamamlanan rezervasyonlar", IconClass = "fa-money-bill-wave", ToneClass = "warning" });
                model.SummaryCards.Add(new PartnerStatCardViewModel { Label = "Ortalama Puan", Value = SafeDecimal(reader, 3).ToString("0.0", CultureInfo.InvariantCulture), Description = "Onayli yorum ortalamasi", IconClass = "fa-star", ToneClass = "danger" });
            }
        }

        const string widgetSql = @"
            SELECT
                COUNT(*) AS total_reservations,
                SUM(CASE WHEN r.durum = N'İptal Edildi' THEN 1 ELSE 0 END) AS cancelled_reservations,
                SUM(CASE WHEN r.firma_id IS NOT NULL THEN 1 ELSE 0 END) AS corporate_reservations,
                (SELECT COUNT(DISTINCT ofm.tarih)
                 FROM oda_fiyat_musaitlik ofm
                 WHERE ofm.otel_id = @hotelId
                   AND ofm.tarih >= CONVERT(date, GETDATE())
                   AND ofm.tarih < DATEADD(day, 30, CONVERT(date, GETDATE()))
                   AND ofm.indirimli_fiyat IS NOT NULL
                   AND ofm.indirimli_fiyat > 0
                   AND ofm.indirimli_fiyat < ofm.gecelik_fiyat) AS discounted_days_30,
                (SELECT COUNT(*)
                 FROM kampanya_oteller ko
                 WHERE ko.otel_id = @hotelId
                   AND ko.katilim_durumu = N'Aktif'
                   AND SYSUTCDATETIME() >= ko.baslangic_tarihi
                   AND SYSUTCDATETIME() <= ko.bitis_tarihi) AS active_campaigns
            FROM rezervasyonlar r
            WHERE r.otel_id = @hotelId;";

        await using (var widgetCommand = new SqlCommand(widgetSql, connection))
        {
            widgetCommand.Parameters.AddWithValue("@hotelId", context.SelectedHotel.HotelId);
            await using var widgetReader = await widgetCommand.ExecuteReaderAsync(cancellationToken);
            if (await widgetReader.ReadAsync(cancellationToken))
            {
                var total = SafeInt(widgetReader, 0);
                var cancelled = SafeInt(widgetReader, 1);
                var corporate = SafeInt(widgetReader, 2);
                var discountedDays = SafeInt(widgetReader, 3);
                var activeCampaigns = SafeInt(widgetReader, 4);

                model.WidgetCards.Add(new PartnerStatCardViewModel { Label = "Toplam Rezervasyon", Value = total.ToString(), Description = "Tüm zamanlar", IconClass = "fa-calendar-check", ToneClass = "primary" });
                model.WidgetCards.Add(new PartnerStatCardViewModel { Label = "İptal Rezervasyon", Value = cancelled.ToString(), Description = "Otel iptalleri", IconClass = "fa-ban", ToneClass = "danger" });
                model.WidgetCards.Add(new PartnerStatCardViewModel { Label = "Firma Rezervasyonu", Value = corporate.ToString(), Description = "Kurumsal kaynak", IconClass = "fa-building", ToneClass = "info" });
                model.WidgetCards.Add(new PartnerStatCardViewModel { Label = "İndirimli Gün", Value = discountedDays.ToString(), Description = "Önümüzdeki 30 gün", IconClass = "fa-tags", ToneClass = "success" });
                model.WidgetCards.Add(new PartnerStatCardViewModel { Label = "Kampanya Katılımı", Value = activeCampaigns.ToString(), Description = "Aktif kampanya", IconClass = "fa-bullhorn", ToneClass = "warning" });
                model.WidgetCards.Add(new PartnerStatCardViewModel { Label = "Aboneliklerim", Value = "—", Description = "Geliştirilecek", IconClass = "fa-bell", ToneClass = "secondary" });
            }
        }

        const string trendSql = @"
            SELECT YEAR(r.olusturulma_tarihi) AS yil,
                   MONTH(r.olusturulma_tarihi) AS ay,
                   COUNT(*) AS rezervasyon_adedi,
                   COALESCE(SUM(r.toplam_tutar), 0) AS gelir
            FROM rezervasyonlar r
            WHERE r.otel_id = @hotelId
              AND r.olusturulma_tarihi >= DATEADD(MONTH, -5, DATEFROMPARTS(YEAR(GETDATE()), MONTH(GETDATE()), 1))
            GROUP BY YEAR(r.olusturulma_tarihi), MONTH(r.olusturulma_tarihi)
            ORDER BY YEAR(r.olusturulma_tarihi), MONTH(r.olusturulma_tarihi);";

        var trendMap = new Dictionary<(int Year, int Month), (int ReservationCount, decimal RevenueAmount)>();
        await using (var trendCommand = new SqlCommand(trendSql, connection))
        {
            trendCommand.Parameters.AddWithValue("@hotelId", context.SelectedHotel.HotelId);
            await using var trendReader = await trendCommand.ExecuteReaderAsync(cancellationToken);
            while (await trendReader.ReadAsync(cancellationToken))
            {
                var year = SafeInt(trendReader, 0);
                var monthNumber = SafeInt(trendReader, 1);
                trendMap[(year, monthNumber)] = (SafeInt(trendReader, 2), SafeDecimal(trendReader, 3));
            }
        }

        var trendRows = new List<PartnerRevenuePointViewModel>();
        var monthCulture = CultureInfo.GetCultureInfo("tr-TR");
        var currentMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        for (var i = 5; i >= 0; i--)
        {
            var monthDate = currentMonth.AddMonths(-i);
            var key = (monthDate.Year, monthDate.Month);
            var hasValue = trendMap.TryGetValue(key, out var value);
            trendRows.Add(new PartnerRevenuePointViewModel
            {
                Label = monthDate.ToString("MMM", monthCulture),
                ReservationCount = hasValue ? value.ReservationCount : 0,
                RevenueAmount = hasValue ? value.RevenueAmount : 0m
            });
        }

        var maxRevenue = trendRows.Max(static row => row.RevenueAmount);
        var maxReservation = trendRows.Max(static row => row.ReservationCount);
        foreach (var item in trendRows)
        {
            if (maxRevenue > 0m)
            {
                item.HeightPercent = item.RevenueAmount <= 0m
                    ? 6
                    : Math.Max(16, (int)Math.Round(item.RevenueAmount * 100m / maxRevenue));
            }
            else if (maxReservation > 0)
            {
                item.HeightPercent = item.ReservationCount <= 0
                    ? 6
                    : Math.Max(16, (int)Math.Round(item.ReservationCount * 100d / maxReservation));
            }
            else
            {
                item.HeightPercent = 6;
            }

            model.RevenueTrend.Add(item);
        }

        var upcomingReservations = await LoadReservationsAsync(
            connection,
            context.SelectedHotel.HotelId,
            context.SelectedHotel.HotelName,
            dateFrom,
            dateTo,
            normalizedStatus,
            normalizedPaymentMethod,
            1,
            normalizedPageSize,
            cancellationToken);
        model.UpcomingReservations = upcomingReservations.Items;
        model.DashboardReservationTotalCount = upcomingReservations.TotalCount;

        var recentReservations = await LoadReservationsAsync(
            connection,
            context.SelectedHotel.HotelId,
            context.SelectedHotel.HotelName,
            null,
            null,
            "all",
            "all",
            1,
            7,
            cancellationToken);
        model.RecentReservations = recentReservations.Items;
        const string dashboardPolicySql = @"
            SELECT
                (
                    SELECT COUNT(*)
                    FROM rezervasyonlar r
                    WHERE r.otel_id = @hotelId
                      AND COALESCE(r.otel_onay_durumu, '') = 'Reddedildi'
                      AND COALESCE(r.otel_onay_tarihi, r.guncellenme_tarihi, r.olusturulma_tarihi) >= DATEADD(DAY, -30, GETDATE())
                ) AS reject_count_30,
                CASE
                    WHEN o.partner_ceza_bitis_tarihi IS NOT NULL AND o.partner_ceza_bitis_tarihi > GETDATE() THEN 1
                    ELSE 0
                END AS ceza_aktif,
                o.partner_ceza_bitis_tarihi
            FROM oteller o
            WHERE o.id = @hotelId;";
        await using (var policyCommand = new SqlCommand(dashboardPolicySql, connection))
        {
            policyCommand.Parameters.AddWithValue("@hotelId", context.SelectedHotel.HotelId);
            await using var policyReader = await policyCommand.ExecuteReaderAsync(cancellationToken);
            if (await policyReader.ReadAsync(cancellationToken))
            {
                model.RejectCountLast30Days = SafeInt(policyReader, 0);
                model.PenaltyActive = SafeBool(policyReader, 1);
                if (!policyReader.IsDBNull(2))
                {
                    model.PenaltyEndText = policyReader.GetDateTime(2).ToString("dd.MM.yyyy HH:mm", CultureInfo.GetCultureInfo("tr-TR"));
                }
            }
        }
        var conversations = await LoadReservationConversationsAsync(connection, context.SelectedHotel.HotelId, cancellationToken);
        var selectedConversationId = conversationId ?? conversations.FirstOrDefault()?.ConversationId;
        foreach (var item in conversations)
        {
            item.IsSelected = item.ConversationId == selectedConversationId;
        }

        model.SelectedConversationId = selectedConversationId;
        model.Conversations = conversations;
        if (selectedConversationId.HasValue)
        {
            model.ConversationMessages = await LoadConversationMessagesAsync(connection, context.SelectedHotel.HotelId, selectedConversationId.Value, cancellationToken);
            await MarkConversationAsReadAsync(connection, context.SelectedHotel.HotelId, selectedConversationId.Value, cancellationToken);
        }
        model.MessageForm = new PartnerGuestMessageRequest
        {
            HotelId = context.SelectedHotel.HotelId,
            ConversationId = model.SelectedConversationId,
            ReservationId = model.Conversations.FirstOrDefault(static item => item.IsSelected)?.ReservationId ?? 0
        };
        model.QuickActions = new List<PartnerQuickActionViewModel>
        {
            new() { Title = "Yeni fiyat guncelle", Description = "Takvim uzerinden gunluk veya toplu fiyat aksiyonu acin.", IconClass = "fa-calendar-days", Url = $"/panel/partner/takvim-fiyatlar?otelId={context.SelectedHotel.HotelId}", ToneClass = "info" },
            new() { Title = "Oda tiplerini yonet", Description = "Oda ekle, duzenle veya bagli verilerle birlikte sil.", IconClass = "fa-bed", Url = $"/panel/partner/oda-yonetimi?otelId={context.SelectedHotel.HotelId}", ToneClass = "success" },
            new() { Title = "Galeri guncelle", Description = "Yeni gorsel yukle veya kapak secimini degistir.", IconClass = "fa-images", Url = $"/panel/partner/fotograflar?otelId={context.SelectedHotel.HotelId}#fotograf-yukle", ToneClass = "warning" },
            new() { Title = "Destek talebi ac", Description = "Operasyon, odeme veya teknik sorunlar icin aninda talep olustur.", IconClass = "fa-headset", Url = $"/panel/partner/724-destek?otelId={context.SelectedHotel.HotelId}", ToneClass = "danger" }
        };
        return model;
    }

    public async Task<PartnerReservationsPageViewModel> GetReservationsAsync(
        long userId,
        long? hotelId = null,
        DateTime? dateFrom = null,
        DateTime? dateTo = null,
        string? status = null,
        string? paymentMethod = null,
        int page = 1,
        int pageSize = 10,
        long? conversationId = null,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var normalizedStatus = NormalizeReservationStatusFilter(status);
        var normalizedPaymentMethod = NormalizeReservationPaymentFilter(paymentMethod);
        var normalizedPageSize = pageSize is 10 or 20 or 30 ? pageSize : 7;
        var normalizedPage = page < 1 ? 1 : page;

        var context = await BuildContextAsync(connection, userId, hotelId, "Rezervasyonlar", "Rezervasyon akislarini, durum gecislerini ve odeme hareketlerini canli verilerle izleyin.", "reservations", cancellationToken);
        var model = new PartnerReservationsPageViewModel
        {
            Shell = context.Shell,
            Filters = new PartnerReservationFilterViewModel
            {
                DateFrom = dateFrom?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                DateTo = dateTo?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                Status = normalizedStatus,
                PaymentMethod = normalizedPaymentMethod,
                Page = normalizedPage,
                PageSize = normalizedPageSize
            },
            CurrentPage = normalizedPage,
            PageSize = normalizedPageSize
        };

        const string summarySql = @"
            SELECT
                COUNT(*) AS total_count,
                SUM(CASE WHEN durum IN ('Onay Bekliyor','Değişiklik Bekliyor') THEN 1 ELSE 0 END) AS pending_count,
                SUM(CASE WHEN durum = 'Onaylandı' THEN 1 ELSE 0 END) AS approved_count,
                SUM(CASE WHEN durum = 'İptal Edildi' THEN 1 ELSE 0 END) AS cancelled_count
            FROM rezervasyonlar
            WHERE otel_id = @hotelId;";

        await using (var summaryCommand = new SqlCommand(summarySql, connection))
        {
            summaryCommand.Parameters.AddWithValue("@hotelId", context.SelectedHotel.HotelId);
            await using var reader = await summaryCommand.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                model.SummaryCards.Add(new PartnerStatCardViewModel { Label = "Toplam", Value = SafeInt(reader, 0).ToString(), Description = "Tum rezervasyonlar", IconClass = "fa-calendar-check", ToneClass = "info" });
                model.SummaryCards.Add(new PartnerStatCardViewModel { Label = "Bekleyen", Value = SafeInt(reader, 1).ToString(), Description = "Onay veya degisiklik bekleyen", IconClass = "fa-hourglass-half", ToneClass = "warning" });
                model.SummaryCards.Add(new PartnerStatCardViewModel { Label = "Onayli", Value = SafeInt(reader, 2).ToString(), Description = "Aktif konaklama akisindaki kayitlar", IconClass = "fa-circle-check", ToneClass = "success" });
                model.SummaryCards.Add(new PartnerStatCardViewModel { Label = "Iptal", Value = SafeInt(reader, 3).ToString(), Description = "Iptal edilen rezervasyonlar", IconClass = "fa-ban", ToneClass = "danger" });
            }
        }

        var reservationResult = await LoadReservationsAsync(
            connection,
            context.SelectedHotel.HotelId,
            context.SelectedHotel.HotelName,
            dateFrom?.Date,
            dateTo?.Date,
            normalizedStatus,
            normalizedPaymentMethod,
            normalizedPage,
            normalizedPageSize,
            cancellationToken);
        model.Reservations = reservationResult.Items;
        model.TotalCount = reservationResult.TotalCount;
        model.TotalPages = Math.Max(1, (int)Math.Ceiling(reservationResult.TotalCount / (double)normalizedPageSize));
        if (model.CurrentPage > model.TotalPages)
        {
            model.CurrentPage = model.TotalPages;
            model.Filters.Page = model.TotalPages;
        }
        await EnsurePartnerPenaltyColumnAsync(connection, cancellationToken);

        const string policySql = @"
            SELECT
                (
                    SELECT COUNT(*)
                    FROM rezervasyonlar r
                    WHERE r.otel_id = @hotelId
                      AND COALESCE(r.otel_onay_durumu, '') = 'Reddedildi'
                      AND COALESCE(r.otel_onay_tarihi, r.guncellenme_tarihi, r.olusturulma_tarihi) >= DATEADD(DAY, -30, GETDATE())
                ) AS reject_count_30,
                CASE
                    WHEN o.partner_ceza_bitis_tarihi IS NOT NULL AND o.partner_ceza_bitis_tarihi > GETDATE() THEN 1
                    ELSE 0
                END AS ceza_aktif,
                o.partner_ceza_bitis_tarihi
            FROM oteller o
            WHERE o.id = @hotelId
            ";
        await using (var policyCommand = new SqlCommand(policySql, connection))
        {
            policyCommand.Parameters.AddWithValue("@hotelId", context.SelectedHotel.HotelId);
            await using var policyReader = await policyCommand.ExecuteReaderAsync(cancellationToken);
            if (await policyReader.ReadAsync(cancellationToken))
            {
                model.RejectCountLast30Days = SafeInt(policyReader, 0);
                model.PenaltyActive = SafeBool(policyReader, 1);
                if (!policyReader.IsDBNull(2))
                {
                    model.PenaltyEndText = policyReader.GetDateTime(2).ToString("dd.MM.yyyy HH:mm", CultureInfo.GetCultureInfo("tr-TR"));
                }
            }
        }

        var conversations = await LoadReservationConversationsAsync(connection, context.SelectedHotel.HotelId, cancellationToken);
        if (conversations.Count > 0)
        {
            var selectedConversationId = conversationId.HasValue && conversations.Any(item => item.ConversationId == conversationId.Value)
                ? conversationId.Value
                : conversations.First().ConversationId;
            model.SelectedConversationId = selectedConversationId;
            foreach (var item in conversations)
            {
                item.IsSelected = item.ConversationId == selectedConversationId;
            }

            model.Conversations = conversations;
            model.ConversationMessages = await LoadConversationMessagesAsync(connection, context.SelectedHotel.HotelId, selectedConversationId, cancellationToken);
            await MarkConversationAsReadAsync(connection, context.SelectedHotel.HotelId, selectedConversationId, cancellationToken);
        }

        model.StatusForm = new PartnerReservationStatusRequest { HotelId = context.SelectedHotel.HotelId };
        model.MessageForm = new PartnerGuestMessageRequest
        {
            HotelId = context.SelectedHotel.HotelId,
            ConversationId = model.SelectedConversationId,
            ReservationId = model.Conversations.FirstOrDefault(static item => item.IsSelected)?.ReservationId ?? 0
        };
        return model;
    }

    public async Task<(bool Success, string Message)> UpdateReservationStatusAsync(long userId, PartnerReservationStatusRequest request, CancellationToken cancellationToken = default)
    {
        if (request.ReservationId <= 0)
        {
            return (false, "Gecerli bir rezervasyon seciniz.");
        }

        var normalizedAction = (request.ActionType ?? string.Empty).Trim().ToLowerInvariant();
        var status = normalizedAction switch
        {
            "approve" => "Onaylandı",
            "checkin" => "Tamamlandı",
            "reject" => "İptal Edildi",
            "pending" => "Onay Bekliyor",
            _ => "Onay Bekliyor"
        };
        var hotelApprovalStatus = normalizedAction switch
        {
            "approve" => "Onaylandı",
            "checkin" => "Onaylandı",
            "reject" => "Reddedildi",
            _ => "Beklemede"
        };
        var reason = string.IsNullOrWhiteSpace(request.Reason)
            ? normalizedAction == "reject"
                ? "Partner panelinden reddedildi."
                : null
            : request.Reason.Trim();
        if (normalizedAction == "reject" && (reason?.Length ?? 0) < 15)
        {
            return (false, "Red sebebi en az 15 karakter olmalidir. Lutfen misafir icin aciklayici bir sebep yazin.");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await EnsurePartnerPenaltyColumnAsync(connection, cancellationToken);
        await EnsureHotelAccessAsync(connection, userId, request.HotelId, cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            const string reservationInfoSql = @"
                SELECT TOP (1) r.kullanici_id,
                       r.rezervasyon_no,
                       COALESCE(NULLIF(r.misafir_ad_soyad, ''), 'Misafir') AS misafir_ad_soyad,
                       COALESCE(NULLIF(r.misafir_eposta, ''), '') AS misafir_eposta,
                       r.giris_tarihi,
                       r.cikis_tarihi,
                       COALESCE(r.toplam_tutar, 0) AS toplam_tutar,
                       COALESCE(ot.oda_adi, '') AS oda_adi,
                       COALESCE(o.otel_adi, 'Otel') AS otel_adi
                FROM rezervasyonlar r
                LEFT JOIN oda_tipleri ot ON ot.id = r.oda_tip_id
                LEFT JOIN oteller o ON o.id = r.otel_id
                WHERE r.id = @reservationId
                  AND r.otel_id = @hotelId;";
            ReservationEmailSnapshot? reservationSnapshot = null;
            await using (var snapshotCommand = new SqlCommand(reservationInfoSql, connection, (SqlTransaction)transaction))
            {
                snapshotCommand.Parameters.AddWithValue("@reservationId", request.ReservationId);
                snapshotCommand.Parameters.AddWithValue("@hotelId", request.HotelId);
                await using var snapshotReader = await snapshotCommand.ExecuteReaderAsync(cancellationToken);
                if (await snapshotReader.ReadAsync(cancellationToken))
                {
                    reservationSnapshot = new ReservationEmailSnapshot(
                        snapshotReader.IsDBNull(0) ? 0 : snapshotReader.GetInt64(0),
                        snapshotReader.GetString(1),
                        snapshotReader.GetString(2),
                        snapshotReader.GetString(3),
                        snapshotReader.GetDateTime(4),
                        snapshotReader.GetDateTime(5),
                        SafeDecimal(snapshotReader, 6),
                        snapshotReader.IsDBNull(7) ? string.Empty : snapshotReader.GetString(7),
                        snapshotReader.IsDBNull(8) ? "Otel" : snapshotReader.GetString(8));
                }
            }

            if (reservationSnapshot is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return (false, "Rezervasyon bulunamadi veya guncellenemedi.");
            }

            const string sql = @"
                UPDATE rezervasyonlar
                SET durum = @status,
                    otel_onay_durumu = @hotelApprovalStatus,
                    otel_onay_tarihi = CASE
                        WHEN @hotelApprovalStatus IN ('Onaylandı', 'Reddedildi') THEN GETDATE()
                        ELSE NULL
                    END,
                    otel_red_nedeni = CASE
                        WHEN @hotelApprovalStatus = 'Reddedildi' THEN @reason
                        ELSE NULL
                    END,
                    iptal_tarihi = CASE
                        WHEN @status = 'İptal Edildi' THEN GETDATE()
                        ELSE NULL
                    END,
                    iptal_nedeni = CASE
                        WHEN @status = 'İptal Edildi' THEN @reason
                        ELSE NULL
                    END,
                    iptal_eden = CASE
                        WHEN @status = 'İptal Edildi' THEN 'Otel'
                        ELSE NULL
                    END,
                    guncellenme_tarihi = GETDATE()
                WHERE id = @reservationId
                  AND otel_id = @hotelId;";

            await using var command = new SqlCommand(sql, connection, (SqlTransaction)transaction);
            command.Parameters.AddWithValue("@status", status);
            command.Parameters.AddWithValue("@hotelApprovalStatus", hotelApprovalStatus);
            command.Parameters.AddWithValue("@reason", (object?)reason ?? DBNull.Value);
            command.Parameters.AddWithValue("@reservationId", request.ReservationId);
            command.Parameters.AddWithValue("@hotelId", request.HotelId);
            var affectedRows = await command.ExecuteNonQueryAsync(cancellationToken);
            if (affectedRows <= 0)
            {
                await transaction.RollbackAsync(cancellationToken);
                return (false, "Rezervasyon bulunamadi veya guncellenemedi.");
            }

            if (normalizedAction == "checkin")
            {
                await ApplyPartnerCheckInPaymentSettlementAsync(connection, (SqlTransaction)transaction, request.HotelId, request.ReservationId, cancellationToken);
                await UpsertCommissionAccountingAfterCheckInAsync(connection, (SqlTransaction)transaction, request.HotelId, request.ReservationId, cancellationToken);
            }

            if (!string.IsNullOrWhiteSpace(reservationSnapshot.GuestEmail))
            {
                var tokens = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["user_first_name"] = SplitFirstName(reservationSnapshot.GuestName),
                    ["booking_reference"] = reservationSnapshot.ReservationNo,
                    ["hotel_name"] = reservationSnapshot.HotelName,
                    ["check_in_date"] = reservationSnapshot.CheckInDate.ToString("dd MMM yyyy", CultureInfo.GetCultureInfo("tr-TR")),
                    ["check_out_date"] = reservationSnapshot.CheckOutDate.ToString("dd MMM yyyy", CultureInfo.GetCultureInfo("tr-TR")),
                    ["total_price"] = reservationSnapshot.TotalAmount.ToString("N2", CultureInfo.GetCultureInfo("tr-TR")),
                    ["room_type_name"] = string.IsNullOrWhiteSpace(reservationSnapshot.RoomName) ? "Oda bilgisi" : reservationSnapshot.RoomName,
                    ["rejection_reason"] = string.IsNullOrWhiteSpace(reason) ? "Belirtilmedi." : reason
                };
                var templateCode = normalizedAction == "approve" ? "reservation_confirmed_customer" : normalizedAction == "reject" ? "reservation_rejected_customer" : string.Empty;
                if (!string.IsNullOrWhiteSpace(templateCode))
                {
                    await _emailQueueService.QueueTemplateAsync(connection, (SqlTransaction)transaction, new QueuedEmailTemplateRequest
                    {
                        UserId = reservationSnapshot.GuestUserId,
                        RecipientEmail = reservationSnapshot.GuestEmail,
                        TemplateCode = templateCode,
                        RelatedTable = "rezervasyonlar",
                        RelatedRecordId = request.ReservationId,
                        Tokens = tokens
                    }, cancellationToken);
                }
            }

            var appendedPolicyMessage = string.Empty;
            if (normalizedAction == "reject")
            {
                const string rejectCountSql = @"
                    SELECT COUNT(*)
                    FROM rezervasyonlar
                    WHERE otel_id = @hotelId
                      AND COALESCE(otel_onay_durumu, '') = 'Reddedildi'
                      AND COALESCE(otel_onay_tarihi, guncellenme_tarihi, olusturulma_tarihi) >= DATEADD(DAY, -30, GETDATE());";
                int rejectCount;
                await using (var rejectCountCommand = new SqlCommand(rejectCountSql, connection, (SqlTransaction)transaction))
                {
                    rejectCountCommand.Parameters.AddWithValue("@hotelId", request.HotelId);
                    var rejectScalar = await rejectCountCommand.ExecuteScalarAsync(cancellationToken);
                    rejectCount = Convert.ToInt32(rejectScalar ?? 0, CultureInfo.InvariantCulture);
                }

                if (rejectCount > 5)
                {
                    appendedPolicyMessage = " Son 30 gunde 5+ red goruldu; kontenjan ve operasyon sureclerini yeniden yapilandirmaniz onerilir.";
                }
                if (rejectCount >= 3)
                {
                    const string lockHotelSql = @"
                        UPDATE oteller
                        SET yayin_durumu = 'Kapatıldı',
                            partner_ceza_bitis_tarihi = DATEADD(DAY, 3, GETDATE())
                        WHERE id = @hotelId;";
                    await using var lockHotelCommand = new SqlCommand(lockHotelSql, connection, (SqlTransaction)transaction);
                    lockHotelCommand.Parameters.AddWithValue("@hotelId", request.HotelId);
                    await lockHotelCommand.ExecuteNonQueryAsync(cancellationToken);
                    appendedPolicyMessage += " Son 30 gunde 3 red esigi asildigi icin tesis 3 gun sureyle listeden otomatik kapatildi.";
                }
            }

            await transaction.CommitAsync(cancellationToken);
            var messageStatus = normalizedAction == "checkin" ? "Giris yapti" : status;
            return (true, $"Rezervasyon durumu '{messageStatus}' olarak guncellendi.{appendedPolicyMessage}");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            return (false, $"Rezervasyon guncellenemedi: {ex.Message}");
        }
    }

    public async Task<(bool Success, string Message)> SendGuestMessageAsync(long userId, PartnerGuestMessageRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return (false, "Mesaj alani bos birakilamaz.");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await EnsureHotelAccessAsync(connection, userId, request.HotelId, cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            const string reservationSql = @"
                SELECT TOP (1) kullanici_id, rezervasyon_no, COALESCE(NULLIF(misafir_eposta, ''), ''), COALESCE(NULLIF(misafir_ad_soyad, ''), 'Misafir')
                FROM rezervasyonlar
                WHERE id = @reservationId AND otel_id = @hotelId;";

            long guestUserId;
            string reservationNo;
            string guestEmail;
            string guestName;
            await using (var reservationCommand = new SqlCommand(reservationSql, connection, (SqlTransaction)transaction))
            {
                reservationCommand.Parameters.AddWithValue("@reservationId", request.ReservationId);
                reservationCommand.Parameters.AddWithValue("@hotelId", request.HotelId);
                await using var reservationReader = await reservationCommand.ExecuteReaderAsync(cancellationToken);
                if (!await reservationReader.ReadAsync(cancellationToken))
                {
                    return (false, "Rezervasyon bulunamadi.");
                }

                guestUserId = reservationReader.IsDBNull(0) ? 0 : reservationReader.GetInt64(0);
                reservationNo = reservationReader.IsDBNull(1) ? "-" : reservationReader.GetString(1);
                guestEmail = reservationReader.IsDBNull(2) ? string.Empty : reservationReader.GetString(2);
                guestName = reservationReader.IsDBNull(3) ? "Misafir" : reservationReader.GetString(3);
            }

            long conversationId = 0;
            const string conversationLookupSql = @"
                SELECT TOP (1) id
                FROM mesaj_konusmalari
                WHERE rezervasyon_id = @reservationId
                  AND otel_id = @hotelId
                ORDER BY id DESC;";
            await using (var conversationLookupCommand = new SqlCommand(conversationLookupSql, connection, (SqlTransaction)transaction))
            {
                conversationLookupCommand.Parameters.AddWithValue("@reservationId", request.ReservationId);
                conversationLookupCommand.Parameters.AddWithValue("@hotelId", request.HotelId);
                var existingConversation = await conversationLookupCommand.ExecuteScalarAsync(cancellationToken);
                if (existingConversation is not null && existingConversation != DBNull.Value)
                {
                    conversationId = Convert.ToInt64(existingConversation, CultureInfo.InvariantCulture);
                }
            }

            if (conversationId <= 0)
            {
                const string createConversationSql = @"
                    INSERT INTO mesaj_konusmalari
                    (konusma_kodu, rezervasyon_id, otel_id, misafir_kullanici_id, otel_yetkilisi_kullanici_id, konu_basligi, konusma_turu, konu_kategorisi, durum, oncelik, son_mesaj_tarihi, son_mesaj_gonderen, son_mesaj_onizleme, otel_okunmamis_sayisi, misafir_okunmamis_sayisi)
                    VALUES
                    (@conversationCode, @reservationId, @hotelId, @guestUserId, @userId, @subject, 'Otel', 'Rezervasyon', 'Açık', 'Normal', GETDATE(), 'Otel', @messagePreview, 0, 1);
                    SELECT CAST(SCOPE_IDENTITY() AS bigint);";

                await using var createConversationCommand = new SqlCommand(createConversationSql, connection, (SqlTransaction)transaction);
                createConversationCommand.Parameters.AddWithValue("@conversationCode", $"KNM-{DateTime.UtcNow:yyyyMMddHHmmss}-{Random.Shared.Next(100, 999)}");
                createConversationCommand.Parameters.AddWithValue("@reservationId", request.ReservationId);
                createConversationCommand.Parameters.AddWithValue("@hotelId", request.HotelId);
                createConversationCommand.Parameters.AddWithValue("@guestUserId", guestUserId > 0 ? guestUserId : DBNull.Value);
                createConversationCommand.Parameters.AddWithValue("@userId", userId);
                createConversationCommand.Parameters.AddWithValue("@subject", string.IsNullOrWhiteSpace(request.Subject) ? $"Rezervasyon {reservationNo}" : request.Subject.Trim());
                createConversationCommand.Parameters.AddWithValue("@messagePreview", TruncateText(request.Message.Trim(), 100));
                var newConversationId = await createConversationCommand.ExecuteScalarAsync(cancellationToken);
                conversationId = Convert.ToInt64(newConversationId ?? 0L, CultureInfo.InvariantCulture);
            }

            const string insertMessageSql = @"
                INSERT INTO mesajlar
                (konusma_id, gonderen_turu, gonderen_kullanici_id, gonderen_otel_id, mesaj_metni, mesaj_tipi, okundu_mu, durum, gonderim_tarihi)
                VALUES
                (@conversationId, 'Otel', @userId, @hotelId, @message, 'Metin', 0, 'Gönderildi', GETDATE());";

            await using (var insertMessageCommand = new SqlCommand(insertMessageSql, connection, (SqlTransaction)transaction))
            {
                insertMessageCommand.Parameters.AddWithValue("@conversationId", conversationId);
                insertMessageCommand.Parameters.AddWithValue("@userId", userId);
                insertMessageCommand.Parameters.AddWithValue("@hotelId", request.HotelId);
                insertMessageCommand.Parameters.AddWithValue("@message", request.Message.Trim());
                await insertMessageCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            const string updateConversationSql = @"
                UPDATE mesaj_konusmalari
                SET son_mesaj_tarihi = GETDATE(),
                    son_mesaj_gonderen = 'Otel',
                    son_mesaj_onizleme = @messagePreview,
                    misafir_okunmamis_sayisi = misafir_okunmamis_sayisi + 1,
                    otel_okunmamis_sayisi = 0,
                    durum = 'Açık'
                WHERE id = @conversationId;";
            await using (var updateConversationCommand = new SqlCommand(updateConversationSql, connection, (SqlTransaction)transaction))
            {
                updateConversationCommand.Parameters.AddWithValue("@conversationId", conversationId);
                updateConversationCommand.Parameters.AddWithValue("@messagePreview", TruncateText(request.Message.Trim(), 100));
                await updateConversationCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            if (!string.IsNullOrWhiteSpace(guestEmail))
            {
                await _emailQueueService.QueueTemplateAsync(connection, (SqlTransaction)transaction, new QueuedEmailTemplateRequest
                {
                    UserId = guestUserId,
                    RecipientEmail = guestEmail,
                    TemplateCode = "reservation_guest_message",
                    RelatedTable = "rezervasyonlar",
                    RelatedRecordId = request.ReservationId,
                    Tokens = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["user_first_name"] = SplitFirstName(guestName),
                        ["booking_reference"] = reservationNo,
                        ["message_subject"] = string.IsNullOrWhiteSpace(request.Subject) ? $"Rezervasyon {reservationNo}" : request.Subject.Trim(),
                        ["message_text"] = request.Message.Trim()
                    }
                }, cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
            return (true, "Misafire mesaj gonderildi ve e-posta bildirimi kuyruga alindi.");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            return (false, $"Misafire mesaj gonderilemedi: {ex.Message}");
        }
    }

    public async Task<(bool Success, string Message)> MarkReservationPaymentCompletedAsync(long userId, long hotelId, long reservationId, CancellationToken cancellationToken = default)
    {
        if (hotelId <= 0 || reservationId <= 0)
        {
            return (false, "Geçersiz istek.");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await EnsureHotelAccessAsync(connection, userId, hotelId, cancellationToken);
        await using var tx = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            const string readSql = @"
                SELECT TOP (1)
                    COALESCE(durum, N'') AS durum,
                    COALESCE(odeme_durumu, N'') AS odeme_durumu,
                    COALESCE(toplam_tutar, 0) AS toplam_tutar,
                    COALESCE(tahsil_edilen_tutar, 0) AS tahsil_edilen_tutar,
                    COALESCE(kapida_odeme_tutari, 0) AS kapida_odeme_tutari,
                    COALESCE(kapida_odeme_durumu, N'') AS kapida_odeme_durumu,
                    COALESCE(online_odeme_tutari, 0) AS online_odeme_tutari,
                    COALESCE(online_odeme_durumu, N'') AS online_odeme_durumu,
                    COALESCE(havale_eft_bekleyen_tutari, 0) AS havale_bekleyen,
                    COALESCE(odeme_yontemi, N'') AS odeme_yontemi
                FROM dbo.rezervasyonlar
                WHERE id=@rid AND otel_id=@hid;";

            string durum;
            string odemeDurumu;
            decimal toplam;
            decimal tahsil;
            decimal kapidaTutar;
            string kapidaDurum;
            decimal onlineTutar;
            string onlineDurum;
            decimal havaleBekleyen;
            string odemeYontemi;

            await using (var cmd = new SqlCommand(readSql, connection, (SqlTransaction)tx))
            {
                cmd.Parameters.AddWithValue("@rid", reservationId);
                cmd.Parameters.AddWithValue("@hid", hotelId);
                await using var r = await cmd.ExecuteReaderAsync(cancellationToken);
                if (!await r.ReadAsync(cancellationToken))
                {
                    await tx.RollbackAsync(cancellationToken);
                    return (false, "Rezervasyon bulunamadı.");
                }

                durum = r.GetString(0);
                odemeDurumu = r.GetString(1);
                toplam = SafeDecimal(r, 2);
                tahsil = SafeDecimal(r, 3);
                kapidaTutar = SafeDecimal(r, 4);
                kapidaDurum = r.IsDBNull(5) ? string.Empty : r.GetString(5);
                onlineTutar = SafeDecimal(r, 6);
                onlineDurum = r.IsDBNull(7) ? string.Empty : r.GetString(7);
                havaleBekleyen = SafeDecimal(r, 8);
                odemeYontemi = r.IsDBNull(9) ? string.Empty : r.GetString(9);
            }

            // Sadece “giriş yaptı / tamamlandı” rezervasyonlarda
            if (!string.Equals(durum, "Tamamlandı", StringComparison.OrdinalIgnoreCase))
            {
                await tx.RollbackAsync(cancellationToken);
                return (false, "Ödeme tamamlandı işlemi sadece giriş yapılmış (Tamamlandı) rezervasyonlarda kullanılabilir.");
            }

            if (string.Equals(odemeDurumu, "Tamamlandı", StringComparison.OrdinalIgnoreCase))
            {
                await tx.RollbackAsync(cancellationToken);
                return (true, "Ödeme zaten tamamlanmış.");
            }

            var newTahsil = toplam;

            const string updateSql = @"
                UPDATE dbo.rezervasyonlar
                SET odeme_durumu = N'Tamamlandı',
                    tahsil_edilen_tutar = @tahsil,
                    kalan_tahsil_edilecek_tutar = 0,
                    havale_eft_bekleyen_tutari = 0,
                    kapida_odeme_durumu = CASE WHEN COALESCE(kapida_odeme_tutari,0) > 0 THEN N'Ödendi' ELSE kapida_odeme_durumu END,
                    online_odeme_durumu = CASE WHEN COALESCE(online_odeme_tutari,0) > 0 THEN N'Tamamlandı' ELSE online_odeme_durumu END,
                    odeme_tarihi = COALESCE(odeme_tarihi, SYSUTCDATETIME()),
                    guncellenme_tarihi = SYSUTCDATETIME()
                WHERE id=@rid AND otel_id=@hid;";

            await using (var cmd = new SqlCommand(updateSql, connection, (SqlTransaction)tx))
            {
                cmd.Parameters.AddWithValue("@tahsil", newTahsil);
                cmd.Parameters.AddWithValue("@rid", reservationId);
                cmd.Parameters.AddWithValue("@hid", hotelId);
                await cmd.ExecuteNonQueryAsync(cancellationToken);
            }

            // Ödeme kalemleri varsa hepsini TAMAMLANDI’ye çek
            if (await TableExistsAsync(connection, "rezervasyon_odeme_kalemleri", (SqlTransaction)tx, cancellationToken))
            {
                const string linesSql = @"
                    UPDATE k
                    SET k.odeme_durumu_id = d.id,
                        k.tahsil_edilen_tutar = k.tutar
                    FROM dbo.rezervasyon_odeme_kalemleri k
                    INNER JOIN dbo.odeme_durumu_tanimlari d ON d.kod = N'TAMAMLANDI'
                    WHERE k.rezervasyon_id = @rid
                      AND k.odeme_durumu_id <> d.id;";
                await using var cmd = new SqlCommand(linesSql, connection, (SqlTransaction)tx);
                cmd.Parameters.AddWithValue("@rid", reservationId);
                await cmd.ExecuteNonQueryAsync(cancellationToken);
            }

            // Ödeme işlemleri tablosu varsa kayıt at (opsiyonel)
            if (await TableExistsAsync(connection, "odeme_islemleri", (SqlTransaction)tx, cancellationToken))
            {
                const string insertPaySql = @"
                    INSERT INTO dbo.odeme_islemleri
                    (rezervasyon_id, odeme_turu, odeme_durumu, odeme_yontemi, toplam_tahsilat, odeme_baslangic_tarihi, odeme_tamamlanma_tarihi)
                    VALUES
                    (@rid, N'Rezervasyon', N'Başarılı', NULLIF(@yontem, N''), @tutar, SYSUTCDATETIME(), SYSUTCDATETIME());";
                await using var cmd = new SqlCommand(insertPaySql, connection, (SqlTransaction)tx);
                cmd.Parameters.AddWithValue("@rid", reservationId);
                cmd.Parameters.AddWithValue("@yontem", odemeYontemi);
                cmd.Parameters.AddWithValue("@tutar", toplam);
                await cmd.ExecuteNonQueryAsync(cancellationToken);
            }

            await tx.CommitAsync(cancellationToken);
            return (true, "Ödeme tamamlandı olarak işaretlendi.");
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(cancellationToken);
            return (false, "Ödeme tamamlandı işlemi başarısız: " + ex.Message);
        }
    }

    public async Task<PartnerGuestMessagesPageViewModel> GetGuestMessagesAsync(long userId, long? hotelId = null, long? conversationId = null, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var context = await BuildContextAsync(connection, userId, hotelId, "Misafir Mesajları", "Rezervasyon bazlı misafir yazışmalarını yönetin.", "reservations", cancellationToken);
        var model = new PartnerGuestMessagesPageViewModel
        {
            Shell = context.Shell,
            SelectedHotelId = context.SelectedHotel.HotelId
        };

        var conversations = await LoadReservationConversationsAsync(connection, context.SelectedHotel.HotelId, cancellationToken);
        var selectedConversationId = conversationId ?? conversations.FirstOrDefault()?.ConversationId;
        foreach (var item in conversations)
        {
            item.IsSelected = item.ConversationId == selectedConversationId;
        }

        model.SelectedConversationId = selectedConversationId;
        model.Conversations = conversations;
        if (selectedConversationId.HasValue)
        {
            model.ConversationMessages = await LoadConversationMessagesAsync(connection, context.SelectedHotel.HotelId, selectedConversationId.Value, cancellationToken);
            await MarkConversationAsReadAsync(connection, context.SelectedHotel.HotelId, selectedConversationId.Value, cancellationToken);
        }

        model.MessageForm = new PartnerGuestMessageRequest
        {
            HotelId = context.SelectedHotel.HotelId,
            ConversationId = selectedConversationId,
            ReservationId = conversations.FirstOrDefault(static item => item.IsSelected)?.ReservationId ?? 0,
            ReturnUrl = $"/panel/partner/rezervasyonlar/misafir-mesajlari?otelId={context.SelectedHotel.HotelId}&conversationId={selectedConversationId}"
        };
        return model;
    }

    public async Task<PartnerReservationCalendarPageViewModel> GetReservationCalendarAsync(long userId, long? hotelId = null, DateTime? dateFrom = null, DateTime? dateTo = null, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var context = await BuildContextAsync(connection, userId, hotelId, "Rezervasyon Takvimi", "Gün bazlı giriş/çıkış ve doluluk özetini izleyin.", "reservations", cancellationToken);
        var start = (dateFrom ?? DateTime.Today).Date;
        var end = (dateTo ?? DateTime.Today.AddDays(30)).Date;
        if (end < start) (start, end) = (end, start);
        if ((end - start).TotalDays > 62) end = start.AddDays(62);

        var model = new PartnerReservationCalendarPageViewModel
        {
            Shell = context.Shell,
            SelectedHotelId = context.SelectedHotel.HotelId,
            DateFrom = start.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            DateTo = end.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            RangeLabel = $"{start:dd.MM.yyyy} - {end:dd.MM.yyyy}"
        };

        const string dayAggSql = @"
            ;WITH dates AS (
                SELECT CAST(@start AS date) AS d
                UNION ALL
                SELECT DATEADD(day, 1, d) FROM dates WHERE d < CAST(@end AS date)
            )
            SELECT d AS tarih,
                   (SELECT COUNT(*) FROM rezervasyonlar r WHERE r.otel_id = @hotelId AND r.durum NOT IN (N'İptal', N'Red') AND r.giris_tarihi = d) AS checkin_count,
                   (SELECT COUNT(*) FROM rezervasyonlar r WHERE r.otel_id = @hotelId AND r.durum NOT IN (N'İptal', N'Red') AND r.cikis_tarihi = d) AS checkout_count,
                   (SELECT COUNT(*) FROM rezervasyonlar r WHERE r.otel_id = @hotelId AND r.durum NOT IN (N'İptal', N'Red')
                        AND r.giris_tarihi <= d AND r.cikis_tarihi > d) AS inhouse_count,
                   (SELECT COUNT(*) FROM rezervasyonlar r WHERE r.otel_id = @hotelId AND r.durum = N'İptal' AND CAST(r.iptal_tarihi AS date) = d) AS cancelled_count
            FROM dates
            OPTION (MAXRECURSION 400);";

        await using (var command = new SqlCommand(dayAggSql, connection))
        {
            command.Parameters.AddWithValue("@hotelId", context.SelectedHotel.HotelId);
            command.Parameters.AddWithValue("@start", start);
            command.Parameters.AddWithValue("@end", end);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var d = reader.GetDateTime(0).Date;
                model.Days.Add(new PartnerReservationCalendarDayRowViewModel
                {
                    Date = DateOnly.FromDateTime(d),
                    DayLabel = d.ToString("dd.MM", CultureInfo.GetCultureInfo("tr-TR")),
                    WeekdayLabel = CultureInfo.GetCultureInfo("tr-TR").DateTimeFormat.GetAbbreviatedDayName(d.DayOfWeek),
                    CheckinCount = SafeInt(reader, 1),
                    CheckoutCount = SafeInt(reader, 2),
                    InhouseCount = SafeInt(reader, 3),
                    CancelledCount = SafeInt(reader, 4)
                });
            }
        }

        PartnerReservationRowViewModel MapReservationRow(SqlDataReader reader)
        {
            var checkIn = reader.GetDateTime(5);
            var checkOut = reader.GetDateTime(6);
            var nights = reader.IsDBNull(20) ? (short)Math.Max(1, (checkOut.Date - checkIn.Date).Days) : Convert.ToInt16(reader.GetValue(20), CultureInfo.InvariantCulture);

            return new PartnerReservationRowViewModel
            {
                ReservationId = reader.GetInt64(0),
                ReservationNo = reader.IsDBNull(1) ? "-" : reader.GetString(1),
                GuestName = reader.IsDBNull(2) ? "-" : reader.GetString(2),
                GuestEmail = reader.IsDBNull(3) ? "-" : reader.GetString(3),
                GuestPhone = reader.IsDBNull(4) ? "-" : reader.GetString(4),
                StayText = $"{checkIn:dd.MM.yyyy} - {checkOut:dd.MM.yyyy}",
                StatusLabel = reader.IsDBNull(7) ? "-" : reader.GetString(7),
                PaymentStatusLabel = reader.IsDBNull(8) ? "-" : reader.GetString(8),
                TotalText = reader.IsDBNull(9) ? "-" : reader.GetDecimal(9).ToString("C0", CultureInfo.GetCultureInfo("tr-TR")),
                CreatedText = reader.IsDBNull(10) ? "-" : reader.GetDateTime(10).ToString("dd.MM.yyyy HH:mm", CultureInfo.GetCultureInfo("tr-TR")),
                HotelName = reader.IsDBNull(11) ? string.Empty : reader.GetString(11),
                RoomName = reader.IsDBNull(12) ? null : reader.GetString(12),
                PaymentMethodLabel = reader.IsDBNull(13) ? null : reader.GetString(13),
                GuestNote = reader.IsDBNull(14) ? null : reader.GetString(14),
                RequestNote = reader.IsDBNull(15) ? null : reader.GetString(15),
                CancellationReason = reader.IsDBNull(16) ? null : reader.GetString(16),
                AdultCount = reader.IsDBNull(17) ? (byte)0 : reader.GetByte(17),
                ChildCount = reader.IsDBNull(18) ? (byte)0 : reader.GetByte(18),
                NightCount = nights
            };
        }

        const string upcomingSql = @"
            SELECT TOP 10 r.id, r.rezervasyon_no, r.misafir_ad_soyad, r.misafir_eposta, r.misafir_telefon,
                   r.giris_tarihi, r.cikis_tarihi, COALESCE(r.durum, N''), COALESCE(r.odeme_durumu, N''), COALESCE(r.toplam_tutar, 0), COALESCE(r.olusturulma_tarihi, SYSUTCDATETIME()),
                   COALESCE(ot.otel_adi, N''), COALESCE(od.oda_adi, N''), COALESCE(r.odeme_yontemi, N''), r.misafir_notu, r.musteri_talep_notu, r.iptal_nedeni,
                   COALESCE(r.yetiskin_sayisi, 0), COALESCE(r.cocuk_sayisi, 0), COALESCE(r.gece_sayisi, DATEDIFF(day, r.giris_tarihi, r.cikis_tarihi))
            FROM rezervasyonlar r
            LEFT JOIN oteller ot ON ot.id = r.otel_id
            LEFT JOIN oda_tipleri od ON od.id = r.oda_tip_id
            WHERE r.otel_id = @hotelId AND r.durum NOT IN (N'İptal', N'Red') AND r.giris_tarihi BETWEEN @start AND @end
            ORDER BY r.giris_tarihi ASC, r.id DESC;";

        await using (var command = new SqlCommand(upcomingSql, connection))
        {
            command.Parameters.AddWithValue("@hotelId", context.SelectedHotel.HotelId);
            command.Parameters.AddWithValue("@start", start);
            command.Parameters.AddWithValue("@end", end);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                model.UpcomingCheckins.Add(MapReservationRow(reader));
            }
        }

        const string upcomingOutSql = @"
            SELECT TOP 10 r.id, r.rezervasyon_no, r.misafir_ad_soyad, r.misafir_eposta, r.misafir_telefon,
                   r.giris_tarihi, r.cikis_tarihi, COALESCE(r.durum, N''), COALESCE(r.odeme_durumu, N''), COALESCE(r.toplam_tutar, 0), COALESCE(r.olusturulma_tarihi, SYSUTCDATETIME()),
                   COALESCE(ot.otel_adi, N''), COALESCE(od.oda_adi, N''), COALESCE(r.odeme_yontemi, N''), r.misafir_notu, r.musteri_talep_notu, r.iptal_nedeni,
                   COALESCE(r.yetiskin_sayisi, 0), COALESCE(r.cocuk_sayisi, 0), COALESCE(r.gece_sayisi, DATEDIFF(day, r.giris_tarihi, r.cikis_tarihi))
            FROM rezervasyonlar r
            LEFT JOIN oteller ot ON ot.id = r.otel_id
            LEFT JOIN oda_tipleri od ON od.id = r.oda_tip_id
            WHERE r.otel_id = @hotelId AND r.durum NOT IN (N'İptal', N'Red') AND r.cikis_tarihi BETWEEN @start AND @end
            ORDER BY r.cikis_tarihi ASC, r.id DESC;";

        await using (var command = new SqlCommand(upcomingOutSql, connection))
        {
            command.Parameters.AddWithValue("@hotelId", context.SelectedHotel.HotelId);
            command.Parameters.AddWithValue("@start", start);
            command.Parameters.AddWithValue("@end", end);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                model.UpcomingCheckouts.Add(MapReservationRow(reader));
            }
        }

        return model;
    }

    public async Task<PartnerCancellationNoShowPageViewModel> GetCancellationNoShowAsync(long userId, long? hotelId = null, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var context = await BuildContextAsync(connection, userId, hotelId, "İptal & No-show", "İptal politikası ve son 30 gün iptal/red özetini görüntüleyin.", "reservations", cancellationToken);
        var model = new PartnerCancellationNoShowPageViewModel
        {
            Shell = context.Shell,
            SelectedHotelId = context.SelectedHotel.HotelId
        };

        const string policySql = @"
            SELECT iptal_politikasi_ozet, detayli_iptal_kosullari, ucretsiz_iptal_suresi, gec_iptal_ceza_orani, no_show_ceza_orani
            FROM otel_kosullari WHERE otel_id = @hotelId;";

        await using (var command = new SqlCommand(policySql, connection))
        {
            command.Parameters.AddWithValue("@hotelId", context.SelectedHotel.HotelId);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                model.CancellationSummary = reader.IsDBNull(0) ? null : reader.GetString(0);
                model.CancellationDetails = reader.IsDBNull(1) ? null : reader.GetString(1);
                model.FreeCancellationDays = reader.IsDBNull(2) ? null : reader.GetByte(2);
                model.LateCancellationPenaltyPercent = reader.IsDBNull(3) ? null : reader.GetDecimal(3);
                model.NoShowPenaltyPercent = reader.IsDBNull(4) ? null : reader.GetDecimal(4);
            }
        }

        const string metricsSql = @"
            SELECT
                SUM(CASE WHEN r.durum = N'İptal' THEN 1 ELSE 0 END) AS cancel_count,
                SUM(CASE WHEN r.durum = N'Red' THEN 1 ELSE 0 END) AS reject_count
            FROM rezervasyonlar r
            WHERE r.otel_id = @hotelId
              AND r.olusturulma_tarihi >= DATEADD(day, -30, SYSUTCDATETIME());";

        await using (var command = new SqlCommand(metricsSql, connection))
        {
            command.Parameters.AddWithValue("@hotelId", context.SelectedHotel.HotelId);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                model.CancelCountLast30Days = SafeInt(reader, 0);
                model.RejectCountLast30Days = SafeInt(reader, 1);
            }
        }

        const string topReasonsSql = @"
            SELECT TOP 8 COALESCE(NULLIF(LTRIM(RTRIM(iptal_nedeni)), ''), N'Belirtilmedi') AS reason, COUNT(*) AS cnt
            FROM rezervasyonlar
            WHERE otel_id = @hotelId AND durum = N'İptal'
              AND olusturulma_tarihi >= DATEADD(day, -30, SYSUTCDATETIME())
            GROUP BY COALESCE(NULLIF(LTRIM(RTRIM(iptal_nedeni)), ''), N'Belirtilmedi')
            ORDER BY cnt DESC;";

        await using (var command = new SqlCommand(topReasonsSql, connection))
        {
            command.Parameters.AddWithValue("@hotelId", context.SelectedHotel.HotelId);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var reason = reader.IsDBNull(0) ? "Belirtilmedi" : reader.GetString(0);
                var cnt = SafeInt(reader, 1);
                model.TopCancellationReasons.Add((reason, cnt));
            }
        }

        return model;
    }

    public async Task<PartnerPaymentStatusesPageViewModel> GetPaymentStatusesAsync(long userId, long? hotelId = null, DateTime? dateFrom = null, DateTime? dateTo = null, string? paymentStatus = null, string? paymentMethod = null, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var context = await BuildContextAsync(connection, userId, hotelId, "Ödeme Durumları", "Rezervasyon tahsilat ve ödeme işlemlerini izleyin.", "reservations", cancellationToken);
        var start = dateFrom?.Date;
        var end = dateTo?.Date;

        var model = new PartnerPaymentStatusesPageViewModel
        {
            Shell = context.Shell,
            SelectedHotelId = context.SelectedHotel.HotelId,
            DateFrom = start?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            DateTo = end?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            PaymentStatus = string.IsNullOrWhiteSpace(paymentStatus) ? "all" : paymentStatus!,
            PaymentMethod = string.IsNullOrWhiteSpace(paymentMethod) ? "all" : paymentMethod!
        };

        const string sql = @"
            SELECT TOP 200
                r.id,
                r.rezervasyon_no,
                r.misafir_ad_soyad,
                r.giris_tarihi,
                r.cikis_tarihi,
                COALESCE(r.durum, N'') AS durum,
                COALESCE(r.odeme_durumu, N'') AS odeme_durumu,
                COALESCE(r.odeme_yontemi, N'') AS odeme_yontemi,
                COALESCE(r.toplam_tutar, 0) AS toplam_tutar,
                COALESCE(r.tahsil_edilen_tutar, 0) AS tahsil_edilen_tutar,
                COALESCE(r.kalan_tahsil_edilecek_tutar, 0) AS kalan_tutar,
                (SELECT TOP 1 COALESCE(odeme_tamamlanma_tarihi, odeme_baslangic_tarihi) FROM odeme_islemleri oi WHERE oi.rezervasyon_id = r.id ORDER BY COALESCE(oi.odeme_tamamlanma_tarihi, oi.odeme_baslangic_tarihi) DESC) AS last_payment_time
            FROM rezervasyonlar r
            WHERE r.otel_id = @hotelId
              AND (@status = N'all' OR COALESCE(r.odeme_durumu, N'') = @status)
              AND (@method = N'all' OR COALESCE(r.odeme_yontemi, N'') = @method)
              AND (@start IS NULL OR r.olusturulma_tarihi >= @start)
              AND (@end IS NULL OR r.olusturulma_tarihi < DATEADD(day, 1, @end))
            ORDER BY r.olusturulma_tarihi DESC, r.id DESC;";

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@hotelId", context.SelectedHotel.HotelId);
        command.Parameters.AddWithValue("@status", model.PaymentStatus);
        command.Parameters.AddWithValue("@method", model.PaymentMethod);
        command.Parameters.AddWithValue("@start", (object?)start ?? DBNull.Value);
        command.Parameters.AddWithValue("@end", (object?)end ?? DBNull.Value);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var total = reader.GetDecimal(8);
            var collected = reader.GetDecimal(9);
            var remaining = reader.GetDecimal(10);
            var lastPaymentTime = reader.IsDBNull(11) ? (DateTime?)null : reader.GetDateTime(11);
            var payStatus = reader.IsDBNull(6) ? string.Empty : reader.GetString(6);
            var tone = payStatus.Contains("Bek", StringComparison.OrdinalIgnoreCase) ? "warning"
                : payStatus.Contains("Tamam", StringComparison.OrdinalIgnoreCase) ? "success"
                : payStatus.Contains("İptal", StringComparison.OrdinalIgnoreCase) ? "danger"
                : "secondary";

            model.Rows.Add(new PartnerPaymentStatusRowViewModel
            {
                ReservationId = reader.GetInt64(0),
                ReservationNo = reader.IsDBNull(1) ? "-" : reader.GetString(1),
                GuestName = reader.IsDBNull(2) ? "-" : reader.GetString(2),
                StayText = $"{reader.GetDateTime(3):dd.MM.yyyy} - {reader.GetDateTime(4):dd.MM.yyyy}",
                StatusLabel = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
                PaymentStatusLabel = payStatus,
                PaymentMethodLabel = reader.IsDBNull(7) ? string.Empty : reader.GetString(7),
                TotalText = total.ToString("C0", CultureInfo.GetCultureInfo("tr-TR")),
                CollectedText = collected.ToString("C0", CultureInfo.GetCultureInfo("tr-TR")),
                RemainingText = remaining.ToString("C0", CultureInfo.GetCultureInfo("tr-TR")),
                LastPaymentTimeText = lastPaymentTime.HasValue ? lastPaymentTime.Value.ToString("dd.MM.yyyy HH:mm", CultureInfo.GetCultureInfo("tr-TR")) : "-",
                ToneClass = tone
            });
        }

        return model;
    }

    public async Task<PartnerCompanyReservationsPageViewModel> GetCompanyReservationsAsync(long userId, long? hotelId = null, DateTime? dateFrom = null, DateTime? dateTo = null, long? companyId = null, string? status = null, string? dateRangeMode = null, bool completedStaysOnly = false, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var context = await BuildContextAsync(connection, userId, hotelId, "Firma Rezervasyonları", "Kurumsal (firma) rezervasyonlarını filtreleyip izleyin; konaklama tarihine veya tamamlanan konaklamaya göre daraltın.", "company-reservations", cancellationToken);
        var start = dateFrom?.Date;
        var end = dateTo?.Date;
        var statusFilter = string.IsNullOrWhiteSpace(status) ? "all" : status!;
        var rangeMode = string.Equals(dateRangeMode, "stay", StringComparison.OrdinalIgnoreCase) ? "stay" : "creation";

        var model = new PartnerCompanyReservationsPageViewModel
        {
            Shell = context.Shell,
            SelectedHotelId = context.SelectedHotel.HotelId,
            DateFrom = start?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            DateTo = end?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            DateRangeMode = rangeMode,
            CompletedStaysOnly = completedStaysOnly,
            CompanyId = companyId,
            Status = statusFilter
        };

        const string companySql = @"
            SELECT DISTINCT f.id, f.firma_adi
            FROM rezervasyonlar r
            INNER JOIN firmalar f ON f.id = r.firma_id
            WHERE r.otel_id = @hotelId AND r.firma_id IS NOT NULL
            ORDER BY f.firma_adi ASC;";

        await using (var cmd = new SqlCommand(companySql, connection))
        {
            cmd.Parameters.AddWithValue("@hotelId", context.SelectedHotel.HotelId);
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                model.CompanyOptions.Add(new PartnerCompanyOptionViewModel
                {
                    CompanyId = reader.GetInt64(0),
                    CompanyName = reader.IsDBNull(1) ? "-" : reader.GetString(1),
                    StatusText = string.Empty,
                    IsSelected = companyId.HasValue && companyId.Value == reader.GetInt64(0)
                });
            }
        }

        const string sql = @"
            SELECT TOP 250
                r.id,
                r.rezervasyon_no,
                COALESCE(f.firma_adi, N'') AS firma_adi,
                COALESCE(r.misafir_ad_soyad, N'') AS misafir,
                r.giris_tarihi,
                r.cikis_tarihi,
                COALESCE(r.durum, N'') AS durum,
                COALESCE(r.firma_onay_durumu, N'') AS firma_onay,
                COALESCE(r.toplam_tutar, 0) AS toplam,
                COALESCE(r.olusturulma_tarihi, SYSUTCDATETIME()) AS olusturma
            FROM rezervasyonlar r
            INNER JOIN firmalar f ON f.id = r.firma_id
            WHERE r.otel_id = @hotelId AND r.firma_id IS NOT NULL
              AND (@companyId IS NULL OR r.firma_id = @companyId)
              AND (@status = N'all' OR COALESCE(r.durum, N'') = @status)
              AND (
                  (@rangeMode = N'creation'
                   AND (@start IS NULL OR r.olusturulma_tarihi >= @start)
                   AND (@end IS NULL OR r.olusturulma_tarihi < DATEADD(day, 1, @end)))
                  OR (@rangeMode = N'stay'
                   AND (
                        (@start IS NULL AND @end IS NULL)
                        OR (@start IS NOT NULL AND @end IS NOT NULL
                            AND r.cikis_tarihi > @start AND r.giris_tarihi < DATEADD(day, 1, @end))
                        OR (@start IS NOT NULL AND @end IS NULL AND r.cikis_tarihi > @start)
                        OR (@start IS NULL AND @end IS NOT NULL AND r.giris_tarihi < DATEADD(day, 1, @end))
                   ))
              )
              AND (@completedOnly = 0 OR (
                    CAST(r.cikis_tarihi AS date) < CAST(SYSUTCDATETIME() AS date)
                    AND COALESCE(r.durum, N'') NOT IN (N'İptal Edildi', N'İptal', N'Reddedildi')
                  ))
            ORDER BY r.olusturulma_tarihi DESC, r.id DESC;";

        await using (var cmd = new SqlCommand(sql, connection))
        {
            cmd.Parameters.AddWithValue("@hotelId", context.SelectedHotel.HotelId);
            cmd.Parameters.AddWithValue("@companyId", (object?)companyId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@status", statusFilter);
            cmd.Parameters.AddWithValue("@start", (object?)start ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@end", (object?)end ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@rangeMode", rangeMode);
            cmd.Parameters.AddWithValue("@completedOnly", completedStaysOnly ? 1 : 0);
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var checkIn = reader.GetDateTime(4);
                var checkOut = reader.GetDateTime(5);
                var created = reader.GetDateTime(9);

                model.Rows.Add(new PartnerCompanyReservationRowViewModel
                {
                    ReservationId = reader.GetInt64(0),
                    ReservationNo = reader.IsDBNull(1) ? "-" : reader.GetString(1),
                    CompanyName = reader.IsDBNull(2) ? "-" : reader.GetString(2),
                    GuestName = reader.IsDBNull(3) ? "-" : reader.GetString(3),
                    StayText = $"{checkIn:dd.MM.yyyy} - {checkOut:dd.MM.yyyy}",
                    StatusLabel = reader.IsDBNull(6) ? "-" : reader.GetString(6),
                    CompanyApprovalStatus = reader.IsDBNull(7) ? "-" : reader.GetString(7),
                    TotalText = reader.IsDBNull(8) ? "-" : reader.GetDecimal(8).ToString("C0", CultureInfo.GetCultureInfo("tr-TR")),
                    CreatedText = created.ToString("dd.MM.yyyy HH:mm", CultureInfo.GetCultureInfo("tr-TR"))
                });
            }
        }

        return model;
    }

    public async Task<PartnerCompanyAnalyticsPageViewModel> GetCompanyAnalyticsAsync(long userId, long? hotelId = null, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var context = await BuildContextAsync(connection, userId, hotelId, "Firma Analizleri", "Firma rezervasyon trendlerini ve gelir dağılımını izleyin.", "company-pricing", cancellationToken);
        var model = new PartnerCompanyAnalyticsPageViewModel
        {
            Shell = context.Shell,
            SelectedHotelId = context.SelectedHotel.HotelId,
            RangeLabel = "Son 90 gün"
        };

        const string sql = @"
            SELECT TOP 50
                f.id,
                COALESCE(f.firma_adi, N'') AS firma_adi,
                COUNT(*) AS cnt,
                COALESCE(SUM(r.toplam_tutar), 0) AS revenue,
                COALESCE(AVG(r.toplam_tutar), 0) AS avg_ticket,
                MAX(COALESCE(r.olusturulma_tarihi, SYSUTCDATETIME())) AS last_time
            FROM rezervasyonlar r
            INNER JOIN firmalar f ON f.id = r.firma_id
            WHERE r.otel_id = @hotelId AND r.firma_id IS NOT NULL
              AND r.olusturulma_tarihi >= DATEADD(day, -90, SYSUTCDATETIME())
              AND COALESCE(r.durum, N'') NOT IN (N'Red')
            GROUP BY f.id, f.firma_adi
            ORDER BY revenue DESC, cnt DESC;";

        await using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@hotelId", context.SelectedHotel.HotelId);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var revenue = reader.GetDecimal(3);
            var avgTicket = reader.GetDecimal(4);
            var lastTime = reader.GetDateTime(5);
            model.Rows.Add(new PartnerCompanyAnalyticsRowViewModel
            {
                CompanyId = reader.GetInt64(0),
                CompanyName = reader.IsDBNull(1) ? "-" : reader.GetString(1),
                ReservationCount = SafeInt(reader, 2),
                RevenueText = revenue.ToString("C0", CultureInfo.GetCultureInfo("tr-TR")),
                AvgTicketText = avgTicket.ToString("C0", CultureInfo.GetCultureInfo("tr-TR")),
                LastReservationText = lastTime.ToString("dd.MM.yyyy HH:mm", CultureInfo.GetCultureInfo("tr-TR"))
            });
        }
        return model;
    }

    public async Task<PartnerCompanyRequestsPageViewModel> GetCompanyRequestsAsync(long userId, long? hotelId = null, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var context = await BuildContextAsync(connection, userId, hotelId, "İlişkili Kurumsal Firmalar", "Bu otelle rezervasyon veya kurumsal oda fiyatı üzerinden bağlantılı firmaların kart özetleri. Platform genelindeki kurumsal üyelik başvuru onayı yalnızca yönetim panelindedir.", "company-pricing", cancellationToken);
        var model = new PartnerCompanyRequestsPageViewModel
        {
            Shell = context.Shell,
            SelectedHotelId = context.SelectedHotel.HotelId
        };

        var hotelPk = context.SelectedHotel.HotelId;

        const string companySql = @"
            SELECT TOP 100
                f.id, f.firma_kodu, f.firma_adi, f.firma_turu, f.onay_durumu, COALESCE(f.basvuru_tarihi, f.olusturulma_tarihi) AS created_at,
                COALESCE(NULLIF(f.yetkili_ad_soyad, ''), N'-') AS contact_name,
                COALESCE(NULLIF(f.yetkili_unvani, ''), N'') AS contact_title,
                f.yetkili_eposta, f.yetkili_telefon
            FROM firmalar f
            WHERE f.id IN (
                SELECT DISTINCT r.firma_id
                FROM dbo.rezervasyonlar r
                WHERE r.otel_id = @hotelId AND r.firma_id IS NOT NULL
                UNION
                SELECT DISTINCT ofm.firma_id
                FROM dbo.firma_oda_fiyat_musaitlik ofm
                WHERE ofm.otel_id = @hotelId AND ofm.firma_id IS NOT NULL
            )
            ORDER BY COALESCE(f.basvuru_tarihi, f.olusturulma_tarihi) DESC, f.id DESC;";

        await using (var cmd = new SqlCommand(companySql, connection))
        {
            cmd.Parameters.AddWithValue("@hotelId", hotelPk);
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var created = reader.IsDBNull(5) ? (DateTime?)null : reader.GetDateTime(5);
                var contactName = reader.IsDBNull(6) ? "-" : reader.GetString(6);
                var contactTitle = reader.IsDBNull(7) ? "" : reader.GetString(7);
                var contact = string.IsNullOrWhiteSpace(contactTitle) ? contactName : $"{contactName} • {contactTitle}";
                model.Companies.Add(new PartnerCompanyRequestRowViewModel
                {
                    CompanyId = reader.GetInt64(0),
                    Code = reader.IsDBNull(1) ? "-" : reader.GetString(1),
                    Name = reader.IsDBNull(2) ? "-" : reader.GetString(2),
                    Type = reader.IsDBNull(3) ? "-" : reader.GetString(3),
                    Status = reader.IsDBNull(4) ? "-" : reader.GetString(4),
                    CreatedText = created.HasValue ? created.Value.ToString("dd.MM.yyyy HH:mm", CultureInfo.GetCultureInfo("tr-TR")) : "-",
                    ContactText = contact,
                    Email = reader.IsDBNull(8) ? null : reader.GetString(8),
                    Phone = reader.IsDBNull(9) ? null : reader.GetString(9)
                });
            }
        }

        if (await TableExistsAsync(connection, "firma_basvuru_hareketleri", cancellationToken))
        {
            const string activitySql = @"
                SELECT TOP 80 h.id, h.firma_id, COALESCE(f.firma_adi, N'') AS firma_adi,
                       COALESCE(h.hareket_tipi, N'') AS hareket_tipi,
                       CONCAT(COALESCE(h.onceki_durum, N''), CASE WHEN h.onceki_durum IS NULL THEN N'' ELSE N' → ' END, COALESCE(h.yeni_durum, N'')) AS durum,
                       COALESCE(h.olusturulma_tarihi, SYSUTCDATETIME()) AS created_at,
                       h.aciklama
                FROM firma_basvuru_hareketleri h
                LEFT JOIN firmalar f ON f.id = h.firma_id
                WHERE EXISTS (
                    SELECT 1 FROM dbo.rezervasyonlar r
                    WHERE r.firma_id = h.firma_id AND r.otel_id = @hotelId)
                   OR EXISTS (
                    SELECT 1 FROM dbo.firma_oda_fiyat_musaitlik ofm
                    WHERE ofm.firma_id = h.firma_id AND ofm.otel_id = @hotelId)
                ORDER BY h.olusturulma_tarihi DESC, h.id DESC;";

            await using var cmd = new SqlCommand(activitySql, connection);
            cmd.Parameters.AddWithValue("@hotelId", hotelPk);
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var created = reader.GetDateTime(5);
                model.Activities.Add(new PartnerCompanyRequestActivityRowViewModel
                {
                    ActivityId = reader.GetInt64(0),
                    CompanyId = reader.GetInt64(1),
                    CompanyName = reader.IsDBNull(2) ? "-" : reader.GetString(2),
                    Type = reader.IsDBNull(3) ? "-" : reader.GetString(3),
                    StatusText = reader.IsDBNull(4) ? "-" : reader.GetString(4),
                    TimeText = created.ToString("dd.MM.yyyy HH:mm", CultureInfo.GetCultureInfo("tr-TR")),
                    Note = reader.IsDBNull(6) ? null : reader.GetString(6)
                });
            }
        }

        return model;
    }

    public async Task<string> ExportReservationsCsvAsync(long userId, long? hotelId = null, CancellationToken cancellationToken = default)
    {
        var model = await GetReservationsAsync(userId, hotelId, cancellationToken: cancellationToken);
        var csv = new StringBuilder();
        csv.AppendLine("Rezervasyon No,Misafir,E-posta,Telefon,Oda,Konaklama,Durum,Odeme Durumu,Tutar,Olusturma Tarihi");

        foreach (var item in model.Reservations)
        {
            csv.Append(EscapeCsv(item.ReservationNo)).Append(',');
            csv.Append(EscapeCsv(item.GuestName)).Append(',');
            csv.Append(EscapeCsv(item.GuestEmail)).Append(',');
            csv.Append(EscapeCsv(item.GuestPhone)).Append(',');
            csv.Append(EscapeCsv(item.RoomName ?? string.Empty)).Append(',');
            csv.Append(EscapeCsv(item.StayText)).Append(',');
            csv.Append(EscapeCsv(item.StatusLabel)).Append(',');
            csv.Append(EscapeCsv(item.PaymentStatusLabel)).Append(',');
            csv.Append(EscapeCsv(item.TotalText)).Append(',');
            csv.Append(EscapeCsv(item.CreatedText)).AppendLine();
        }

        return csv.ToString();
    }

    public async Task<PartnerPricingPageViewModel> GetPricingAsync(long userId, long? hotelId = null, long? roomId = null, string? month = null, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var context = await BuildContextAsync(connection, userId, hotelId, "Takvim ve Fiyatlar", "Gunluk fiyat, indirim ve musaitlik kurallarini oda bazli takvim uzerinden yonetin.", "pricing", cancellationToken);
        var inclusiveTax = await LoadInclusiveTaxPercentsAsync(connection, context.SelectedHotel.HotelId, cancellationToken);
        var rooms = await GetRoomSummariesAsync(connection, context.SelectedHotel.HotelId, inclusiveTax, cancellationToken);
        var monthStart = ParseMonthStart(month);
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);

        var selectedRoomId = roomId.HasValue && rooms.Any(static item => item.RoomId > 0) && rooms.Any(item => item.RoomId == roomId.Value)
            ? roomId.Value
            : rooms.FirstOrDefault(static item => item.IsActive)?.RoomId ?? rooms.FirstOrDefault()?.RoomId;

        var pricingEntries = await LoadPricingMonthEntriesAsync(connection, context.SelectedHotel.HotelId, monthStart, monthEnd, cancellationToken);
        await EnrichRoomSummariesForPricingMonthAsync(connection, context.SelectedHotel.HotelId, rooms, pricingEntries, monthStart, monthEnd, cancellationToken);
        var discounts = await LoadActiveDiscountOptionsAsync(connection, cancellationToken);
        var defaultRangeStart = monthStart < DateOnly.FromDateTime(DateTime.Today) ? DateOnly.FromDateTime(DateTime.Today) : monthStart;
        if (defaultRangeStart > monthEnd)
        {
            defaultRangeStart = monthStart;
        }

        var defaultRangeEnd = defaultRangeStart.AddDays(6);
        if (defaultRangeEnd > monthEnd)
        {
            defaultRangeEnd = monthEnd;
        }

        return new PartnerPricingPageViewModel
        {
            Shell = context.Shell,
            SelectedHotelId = context.SelectedHotel.HotelId,
            SelectedRoomId = selectedRoomId,
            MonthKey = monthStart.ToString("yyyy-MM", CultureInfo.InvariantCulture),
            MonthLabel = monthStart.ToDateTime(TimeOnly.MinValue).ToString("MMMM yyyy", CultureInfo.GetCultureInfo("tr-TR")),
            PreviousMonthKey = ClampPricingMonth(monthStart.AddMonths(-1)).ToString("yyyy-MM", CultureInfo.InvariantCulture),
            NextMonthKey = ClampPricingMonth(monthStart.AddMonths(1)).ToString("yyyy-MM", CultureInfo.InvariantCulture),
            MonthOptions = BuildMonthOptions(monthStart),
            Rooms = rooms,
            SummaryCards = BuildPricingSummaryCards(rooms, pricingEntries, selectedRoomId, inclusiveTax.VatPercent, inclusiveTax.AccommodationPercent),
            CalendarDays = selectedRoomId.HasValue
                ? BuildPricingCalendarDays(rooms, pricingEntries, selectedRoomId.Value, monthStart, inclusiveTax.VatPercent, inclusiveTax.AccommodationPercent)
                : new List<PartnerPricingDayViewModel>(),
            AvailableDiscounts = discounts,
            BulkForm = new PartnerBulkPricingUpdateRequest
            {
                HotelId = context.SelectedHotel.HotelId,
                ViewRoomId = selectedRoomId,
                ViewMonth = monthStart.ToString("yyyy-MM", CultureInfo.InvariantCulture),
                SelectedRoomIds = selectedRoomId.HasValue ? new List<long> { selectedRoomId.Value } : new List<long>(),
                DateFrom = defaultRangeStart.ToDateTime(TimeOnly.MinValue),
                DateTo = defaultRangeEnd.ToDateTime(TimeOnly.MinValue),
                MinStay = null,
                MaxStay = null,
                SaleStatusAction = "keep"
            },
            DailyForm = new PartnerDailyPricingUpdateRequest
            {
                HotelId = context.SelectedHotel.HotelId,
                RoomId = selectedRoomId ?? 0,
                ViewMonth = monthStart.ToString("yyyy-MM", CultureInfo.InvariantCulture),
                Date = defaultRangeStart.ToDateTime(TimeOnly.MinValue),
                SaleStatusAction = "keep"
            }
        };
    }

    private async Task<List<PartnerDiscountOptionViewModel>> LoadActiveDiscountOptionsAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        var result = new List<PartnerDiscountOptionViewModel>();
        if (!await TableExistsAsync(connection, "fiyat_indirimleri", cancellationToken))
        {
            return result;
        }

        const string sql = @"
            SELECT id,
                   indirim_adi,
                   COALESCE(kisa_aciklama, '') AS kisa_aciklama,
                   COALESCE(gorsel_url, '') AS gorsel_url,
                   COALESCE(ikon_class, '') AS ikon_class,
                   COALESCE(renk_kodu, '') AS renk_kodu
            FROM fiyat_indirimleri
            WHERE aktif_mi = 1
            ORDER BY siralama ASC, id ASC;";

        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var id = reader.GetInt64(0);
            result.Add(new PartnerDiscountOptionViewModel
            {
                DiscountId = id,
                DiscountName = NormalizeTurkishText(reader.IsDBNull(1) ? string.Empty : reader.GetString(1)),
                ShortDescription = reader.IsDBNull(2) ? null : NormalizeTurkishText(reader.GetString(2)),
                ImageUrl = reader.IsDBNull(3) ? null : reader.GetString(3),
                IconClass = reader.IsDBNull(4) ? null : reader.GetString(4),
                ColorCode = reader.IsDBNull(5) ? null : reader.GetString(5)
            });
        }

        return result;
    }

    public async Task<PartnerCampaignsPageViewModel> GetCampaignsAsync(long userId, long? hotelId = null, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var context = await BuildContextAsync(connection, userId, hotelId, "Kampanyalar", "Aktif kampanyalara katilin, otel vitrininizi kampanya tarihleriyle yonetin.", "campaigns", cancellationToken);
        var availableCampaigns = await LoadActiveCampaignOptionsAsync(connection, context.SelectedHotel.HotelId, cancellationToken);
        var joinedCampaigns = await LoadJoinedCampaignsAsync(connection, context.SelectedHotel.HotelId, cancellationToken);

        return new PartnerCampaignsPageViewModel
        {
            Shell = context.Shell,
            SelectedHotelId = context.SelectedHotel.HotelId,
            AvailableCampaigns = availableCampaigns,
            JoinedCampaigns = joinedCampaigns,
            SummaryCards = new List<PartnerStatCardViewModel>
            {
                new() { Label = "Aktif Kampanya", Value = joinedCampaigns.Count(x => x.StatusText == "Aktif").ToString(CultureInfo.InvariantCulture), Description = "Secili otelin yayinlanan kampanya katilimlari", IconClass = "fa-bullhorn", ToneClass = "success" },
                new() { Label = "Uygun Kampanya", Value = availableCampaigns.Count.ToString(CultureInfo.InvariantCulture), Description = "Partner katilimina acik kampanyalar", IconClass = "fa-rocket", ToneClass = "info" },
                new() { Label = "One Cikan", Value = joinedCampaigns.Count(x => x.IsFeatured).ToString(CultureInfo.InvariantCulture), Description = "Vitrinde one cikarilan kampanyalar", IconClass = "fa-star", ToneClass = "warning" },
                new() { Label = "Toplam Kayit", Value = joinedCampaigns.Count.ToString(CultureInfo.InvariantCulture), Description = "Bu otele ait tum kampanya kayitlari", IconClass = "fa-layer-group", ToneClass = "danger" }
            },
            JoinForm = new PartnerCampaignJoinRequest
            {
                HotelId = context.SelectedHotel.HotelId,
                SortOrder = joinedCampaigns.Count * 10 + 10
            }
        };
    }

    public async Task<(bool Success, string Message)> ApplyBulkPricingAsync(long userId, PartnerBulkPricingUpdateRequest request, CancellationToken cancellationToken = default)
    {
        if (request.DateFrom.Date > request.DateTo.Date)
        {
            return (false, "Baslangic tarihi bitis tarihinden buyuk olamaz.");
        }

        var pricingWindowError = ValidatePricingWindow(request.DateFrom.Date, request.DateTo.Date);
        if (pricingWindowError is not null)
        {
            return (false, pricingWindowError);
        }

        // Kural: indirimli fiyat giriliyorsa indirim secimi zorunlu.
        if (!request.ClearDiscountPrice && request.DiscountPrice.HasValue)
        {
            if (!request.DiscountId.HasValue || request.DiscountId.Value <= 0)
            {
                return (false, "İndirimli fiyat girmek için indirim seçmelisiniz.");
            }
        }

        var selectedRoomIds = request.SelectedRoomIds
            .Where(static item => item > 0)
            .Distinct()
            .ToList();

        if (selectedRoomIds.Count == 0 && request.RoomId.HasValue && request.RoomId.Value > 0)
        {
            selectedRoomIds.Add(request.RoomId.Value);
        }

        if (selectedRoomIds.Count == 0 && request.ViewRoomId.HasValue && request.ViewRoomId.Value > 0)
        {
            selectedRoomIds.Add(request.ViewRoomId.Value);
        }

        var saleAction = (request.SaleStatusAction ?? string.Empty).Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(saleAction))
        {
            saleAction = request.OpenSale ? "open" : request.CloseSale ? "close" : "keep";
        }

        var hasAnyUpdate = request.BasePrice.HasValue
            || request.DiscountPrice.HasValue
            || request.ClearDiscountPrice
            || request.TotalRooms.HasValue
            || request.MinStay.HasValue
            || request.MaxStay.HasValue
            || saleAction is "open" or "close"
            || request.DiscountId.HasValue
            || !string.IsNullOrWhiteSpace(request.CampaignLabel)
            || !string.IsNullOrWhiteSpace(request.PriceNote);

        if (!hasAnyUpdate)
        {
            return (false, "En az bir fiyat, stok veya satis kuralini guncellemelisiniz.");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        var hotel = await EnsureHotelAccessAsync(connection, userId, request.HotelId, cancellationToken);
        var inclusiveTax = await LoadInclusiveTaxPercentsAsync(connection, hotel.HotelId, cancellationToken);
        var discount = await ResolveDiscountAsync(connection, request.DiscountId, cancellationToken);
        if (request.DiscountId.HasValue && request.DiscountId.Value > 0 && discount is null)
        {
            return (false, "Seçilen indirim bulunamadı veya aktif değil.");
        }

        const string roomSql = @"
            SELECT id, toplam_oda_sayisi, standart_gecelik_fiyat
            FROM oda_tipleri
            WHERE otel_id = @hotelId
              AND aktif_mi = 1
              AND (@hasFilter = 0 OR id IN ({ROOM_IDS}))
            ORDER BY id ASC;";

        var roomIdList = selectedRoomIds.Count == 0
            ? new List<long>()
            : selectedRoomIds;

        var rooms = new List<RoomPricingSeed>();
        var resolvedRoomSql = roomSql.Replace("{ROOM_IDS}", roomIdList.Count == 0 ? "0" : string.Join(",", roomIdList));
        await using (var roomCommand = new SqlCommand(resolvedRoomSql, connection))
        {
            roomCommand.Parameters.AddWithValue("@hotelId", hotel.HotelId);
            roomCommand.Parameters.AddWithValue("@hasFilter", roomIdList.Count > 0 ? 1 : 0);
            await using var roomReader = await roomCommand.ExecuteReaderAsync(cancellationToken);
            while (await roomReader.ReadAsync(cancellationToken))
            {
                rooms.Add(new RoomPricingSeed(
                    roomReader.GetInt64(0),
                    SafeShort(roomReader, 1),
                    roomReader.GetDecimal(2)));
            }
        }

        if (rooms.Count == 0)
        {
            return (false, "Guncellenecek oda tipi bulunamadi.");
        }

        var existingEntries = await LoadPricingEntriesForRangeAsync(
            connection,
            hotel.HotelId,
            rooms.Select(static item => item.RoomId).ToList(),
            DateOnly.FromDateTime(request.DateFrom.Date),
            DateOnly.FromDateTime(request.DateTo.Date),
            cancellationToken);

        var hasDiscountIdColumn = await ColumnExistsAsync(connection, "oda_fiyat_musaitlik", "indirim_id", cancellationToken);

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            foreach (var room in rooms)
            {
                for (var date = request.DateFrom.Date; date <= request.DateTo.Date; date = date.AddDays(1))
                {
                    var dateOnly = DateOnly.FromDateTime(date);
                    existingEntries.TryGetValue((room.RoomId, dateOnly), out var existing);

                    decimal basePrice;
                    if (request.BasePrice.HasValue)
                    {
                        basePrice = InclusiveNightlyPricing.PartnerGrossEntryToStoredNet(request.BasePrice.Value, inclusiveTax.VatPercent, inclusiveTax.AccommodationPercent);
                    }
                    else
                    {
                        basePrice = existing?.BasePrice ?? room.BasePrice;
                    }

                    decimal? discountPrice;
                    if (request.ClearDiscountPrice)
                    {
                        discountPrice = null;
                    }
                    else if (request.DiscountPrice.HasValue)
                    {
                        discountPrice = InclusiveNightlyPricing.PartnerGrossEntryToStoredNet(request.DiscountPrice.Value, inclusiveTax.VatPercent, inclusiveTax.AccommodationPercent);
                    }
                    else
                    {
                        discountPrice = existing?.DiscountPrice;
                    }
                    var totalRooms = request.TotalRooms ?? existing?.TotalRooms ?? room.TotalRooms;
                    var minStay = request.MinStay ?? existing?.MinStay ?? (byte)1;
                    var maxStay = request.MaxStay ?? existing?.MaxStay ?? (short)30;
                    var isClosed = saleAction switch
                    {
                        "open" => false,
                        "close" => true,
                        _ => existing?.IsClosed ?? false
                    };
                    var discountId = discount?.DiscountId ?? existing?.DiscountId;
                    var campaignId = hasDiscountIdColumn ? existing?.CampaignId : null;
                    var campaignLabel = !string.IsNullOrWhiteSpace(request.CampaignLabel)
                        ? request.CampaignLabel.Trim()
                        : existing?.CampaignLabel;
                    if (string.IsNullOrWhiteSpace(campaignLabel) && discount is not null && !string.IsNullOrWhiteSpace(discount.DiscountName))
                    {
                        campaignLabel = discount.DiscountName;
                    }
                    var priceNote = !string.IsNullOrWhiteSpace(request.PriceNote)
                        ? request.PriceNote.Trim()
                        : existing?.PriceNote;

                    var upsertSql = hasDiscountIdColumn
                        ? @"
                        IF EXISTS (SELECT 1 FROM oda_fiyat_musaitlik WHERE otel_id = @hotelId AND oda_tip_id = @roomId AND tarih = @date)
                        BEGIN
                            UPDATE oda_fiyat_musaitlik
                            SET gecelik_fiyat = @basePrice,
                                indirimli_fiyat = @discountPrice,
                                indirim_id = @discountId,
                                kampanya_id = @campaignId,
                                toplam_oda_sayisi = @stock,
                                minimum_geceleme = @minStay,
                                maksimum_geceleme = @maxStay,
                                kapali_satis = @closeSale,
                                kampanya_etiketi = @campaignLabel,
                                fiyat_notu = @priceNote,
                                guncelleyen_kullanici_id = @updatedBy,
                                guncellenme_tarihi = CURRENT_TIMESTAMP
                            WHERE otel_id = @hotelId
                              AND oda_tip_id = @roomId
                              AND tarih = @date;
                        END
                        ELSE
                        BEGIN
                            INSERT INTO oda_fiyat_musaitlik
                            (otel_id, oda_tip_id, tarih, gecelik_fiyat, indirimli_fiyat, indirim_id, kampanya_id, toplam_oda_sayisi, satilan_oda_sayisi, bloke_oda_sayisi, minimum_geceleme, maksimum_geceleme, kapali_satis, sadece_gunubirlik, kampanya_etiketi, fiyat_notu, guncelleyen_kullanici_id)
                            VALUES
                            (@hotelId, @roomId, @date, @basePrice, @discountPrice, @discountId, @campaignId, @stock, 0, 0, @minStay, @maxStay, @closeSale, 0, @campaignLabel, @priceNote, @updatedBy);
                        END;"
                        : @"
                        IF EXISTS (SELECT 1 FROM oda_fiyat_musaitlik WHERE otel_id = @hotelId AND oda_tip_id = @roomId AND tarih = @date)
                        BEGIN
                            UPDATE oda_fiyat_musaitlik
                            SET gecelik_fiyat = @basePrice,
                                indirimli_fiyat = @discountPrice,
                                kampanya_id = @discountId,
                                toplam_oda_sayisi = @stock,
                                minimum_geceleme = @minStay,
                                maksimum_geceleme = @maxStay,
                                kapali_satis = @closeSale,
                                kampanya_etiketi = @campaignLabel,
                                fiyat_notu = @priceNote,
                                guncelleyen_kullanici_id = @updatedBy,
                                guncellenme_tarihi = CURRENT_TIMESTAMP
                            WHERE otel_id = @hotelId
                              AND oda_tip_id = @roomId
                              AND tarih = @date;
                        END
                        ELSE
                        BEGIN
                            INSERT INTO oda_fiyat_musaitlik
                            (otel_id, oda_tip_id, tarih, gecelik_fiyat, indirimli_fiyat, kampanya_id, toplam_oda_sayisi, satilan_oda_sayisi, bloke_oda_sayisi, minimum_geceleme, maksimum_geceleme, kapali_satis, sadece_gunubirlik, kampanya_etiketi, fiyat_notu, guncelleyen_kullanici_id)
                            VALUES
                            (@hotelId, @roomId, @date, @basePrice, @discountPrice, @discountId, @stock, 0, 0, @minStay, @maxStay, @closeSale, 0, @campaignLabel, @priceNote, @updatedBy);
                        END;";

                    await using var upsertCommand = new SqlCommand(upsertSql, connection, (SqlTransaction)transaction);
                    upsertCommand.Parameters.AddWithValue("@hotelId", hotel.HotelId);
                    upsertCommand.Parameters.AddWithValue("@roomId", room.RoomId);
                    upsertCommand.Parameters.AddWithValue("@date", date);
                    upsertCommand.Parameters.AddWithValue("@basePrice", basePrice);
                    upsertCommand.Parameters.AddWithValue("@discountPrice", discountPrice.HasValue ? discountPrice.Value : DBNull.Value);
                    upsertCommand.Parameters.AddWithValue("@discountId", discountId.HasValue ? discountId.Value : DBNull.Value);
                    upsertCommand.Parameters.AddWithValue("@campaignId", campaignId.HasValue ? campaignId.Value : DBNull.Value);
                    upsertCommand.Parameters.AddWithValue("@stock", totalRooms);
                    upsertCommand.Parameters.AddWithValue("@minStay", minStay);
                    upsertCommand.Parameters.AddWithValue("@maxStay", maxStay);
                    upsertCommand.Parameters.AddWithValue("@closeSale", isClosed ? 1 : 0);
                    upsertCommand.Parameters.AddWithValue("@campaignLabel", string.IsNullOrWhiteSpace(campaignLabel) ? DBNull.Value : campaignLabel);
                    upsertCommand.Parameters.AddWithValue("@priceNote", string.IsNullOrWhiteSpace(priceNote) ? DBNull.Value : priceNote);
                    upsertCommand.Parameters.AddWithValue("@updatedBy", userId);
                    await upsertCommand.ExecuteNonQueryAsync(cancellationToken);
                }
            }

            await _favoritePriceAlertService.QueuePriceRecheckJobAsync(
                connection,
                (SqlTransaction)transaction,
                hotel.HotelId,
                request.DateFrom.Date,
                request.DateTo.Date,
                userId,
                cancellationToken);

            await transaction.CommitAsync(cancellationToken);
            _logger.LogInformation(
                "PRICING_AUDIT_TRAIL hotelId={HotelId} userId={UserId} roomTypeCount={RoomTypes} dateFrom={From:o} dateTo={To:o}",
                hotel.HotelId,
                userId,
                rooms.Count,
                request.DateFrom,
                request.DateTo);
            return (true, $"{rooms.Count} oda tipi icin takvim ve fiyat kurallari guncellendi.");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            return (false, $"Toplu fiyat kaydi sirasinda hata olustu: {ex.Message}");
        }
    }

    public async Task<(bool Success, string Message)> ApplyDailyPricingAsync(long userId, PartnerDailyPricingUpdateRequest request, CancellationToken cancellationToken = default)
    {
        var mappedRequest = new PartnerBulkPricingUpdateRequest
        {
            HotelId = request.HotelId,
            RoomId = request.RoomId,
            ViewRoomId = request.RoomId,
            ViewMonth = request.ViewMonth,
            SelectedRoomIds = new List<long> { request.RoomId },
            DateFrom = request.Date.Date,
            DateTo = request.Date.Date,
            BasePrice = request.BasePrice,
            DiscountPrice = request.DiscountPrice,
            ClearDiscountPrice = request.ClearDiscountPrice,
            TotalRooms = request.TotalRooms,
            MinStay = request.MinStay,
            MaxStay = request.MaxStay,
            SaleStatusAction = request.SaleStatusAction,
            DiscountId = request.DiscountId,
            PriceNote = request.PriceNote
        };

        return await ApplyBulkPricingAsync(userId, mappedRequest, cancellationToken);
    }

    public async Task<(bool Success, string Message)> JoinCampaignAsync(long userId, PartnerCampaignJoinRequest request, CancellationToken cancellationToken = default)
    {
        if (request.CampaignId <= 0 || request.HotelId <= 0)
        {
            return (false, "Kampanya ve otel secimi zorunludur.");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var hotel = await EnsureHotelAccessAsync(connection, userId, request.HotelId, cancellationToken);
        var campaign = await ResolveCampaignAsync(connection, request.CampaignId, cancellationToken);
        if (campaign is null)
        {
            return (false, "Secilen kampanya aktif degil veya partner katilimina uygun degil.");
        }

        var startDate = request.StartDate?.Date ?? campaign.StartDate;
        var endDate = request.EndDate?.Date ?? campaign.EndDate;

        if (startDate < campaign.StartDate)
        {
            startDate = campaign.StartDate;
        }

        if (endDate > campaign.EndDate)
        {
            endDate = campaign.EndDate;
        }

        if (startDate > endDate)
        {
            return (false, "Katilim tarih araligi kampanya tarihleri icinde kalmalidir.");
        }

        if (!await TableExistsAsync(connection, "kampanya_oteller", cancellationToken))
        {
            return (false, "Kampanya otel iliski tablosu bulunamadi.");
        }

        const string sql = @"
            IF EXISTS (SELECT 1 FROM kampanya_oteller WHERE kampanya_id = @campaignId AND otel_id = @hotelId)
            BEGIN
                UPDATE kampanya_oteller
                SET partner_id = @partnerId,
                    katilim_durumu = 'Aktif',
                    katilim_kaynagi = 'Partner',
                    baslangic_tarihi = @startDate,
                    bitis_tarihi = @endDate,
                    ozel_indirim_orani = @discountRate,
                    ozel_indirim_tutari = @discountAmount,
                    ozel_kampanyali_fiyat = @campaignPrice,
                    kampanya_etiketi = @campaignLabel,
                    landing_url = @landingUrl,
                    partner_notu = @partnerNote,
                    one_cikan = @featured,
                    siralama = @sortOrder,
                    partner_onay_tarihi = GETDATE(),
                    guncelleyen_kullanici_id = @userId,
                    guncellenme_tarihi = CURRENT_TIMESTAMP
                WHERE kampanya_id = @campaignId
                  AND otel_id = @hotelId;
            END
            ELSE
            BEGIN
                INSERT INTO kampanya_oteller
                (kampanya_id, otel_id, partner_id, katilim_durumu, katilim_kaynagi, baslangic_tarihi, bitis_tarihi, ozel_indirim_orani, ozel_indirim_tutari, ozel_kampanyali_fiyat, kampanya_etiketi, landing_url, partner_notu, one_cikan, siralama, partner_onay_tarihi, olusturan_kullanici_id, guncelleyen_kullanici_id)
                VALUES
                (@campaignId, @hotelId, @partnerId, 'Aktif', 'Partner', @startDate, @endDate, @discountRate, @discountAmount, @campaignPrice, @campaignLabel, @landingUrl, @partnerNote, @featured, @sortOrder, GETDATE(), @userId, @userId);
            END;";

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@campaignId", campaign.CampaignId);
        command.Parameters.AddWithValue("@hotelId", hotel.HotelId);
        command.Parameters.AddWithValue("@partnerId", hotel.PartnerId);
        command.Parameters.AddWithValue("@startDate", startDate);
        command.Parameters.AddWithValue("@endDate", endDate);
        command.Parameters.AddWithValue("@discountRate", request.CustomDiscountRate.HasValue ? request.CustomDiscountRate.Value : DBNull.Value);
        command.Parameters.AddWithValue("@discountAmount", request.CustomDiscountAmount.HasValue ? request.CustomDiscountAmount.Value : DBNull.Value);
        command.Parameters.AddWithValue("@campaignPrice", request.CustomCampaignPrice.HasValue ? request.CustomCampaignPrice.Value : DBNull.Value);
        command.Parameters.AddWithValue("@campaignLabel", string.IsNullOrWhiteSpace(request.CampaignLabel) ? campaign.DisplayLabel : request.CampaignLabel.Trim());
        command.Parameters.AddWithValue("@landingUrl", string.IsNullOrWhiteSpace(request.LandingUrl) ? DBNull.Value : request.LandingUrl.Trim());
        command.Parameters.AddWithValue("@partnerNote", string.IsNullOrWhiteSpace(request.PartnerNote) ? DBNull.Value : request.PartnerNote.Trim());
        command.Parameters.AddWithValue("@featured", request.Featured ? 1 : 0);
        command.Parameters.AddWithValue("@sortOrder", request.SortOrder);
        command.Parameters.AddWithValue("@userId", userId);
        await command.ExecuteNonQueryAsync(cancellationToken);

        return (true, $"{campaign.CampaignName} kampanyası bu otel için kaydedildi.");
    }

    public async Task<(bool Success, string Message)> LeaveCampaignAsync(long userId, long hotelId, long campaignId, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var hotel = await EnsureHotelAccessAsync(connection, userId, hotelId, cancellationToken);

        const string sql = @"
            UPDATE kampanya_oteller
            SET katilim_durumu = 'Pasif',
                bitis_tarihi = CASE WHEN bitis_tarihi > GETDATE() THEN GETDATE() ELSE bitis_tarihi END,
                guncelleyen_kullanici_id = @userId,
                guncellenme_tarihi = CURRENT_TIMESTAMP
            WHERE kampanya_id = @campaignId
              AND otel_id = @hotelId;";

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@campaignId", campaignId);
        command.Parameters.AddWithValue("@hotelId", hotel.HotelId);
        command.Parameters.AddWithValue("@userId", userId);
        var affected = await command.ExecuteNonQueryAsync(cancellationToken);
        return affected > 0
            ? (true, "Kampanya katilimi pasife alindi.")
            : (false, "Pasife alinacak kampanya kaydi bulunamadi.");
    }

    public async Task<PartnerRoomManagementPageViewModel> GetRoomsAsync(long userId, long? hotelId = null, long? roomId = null, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var context = await BuildContextAsync(connection, userId, hotelId, "Oda Yonetimi", "Oda tiplerini, kapasiteyi, gorselleri ve baz fiyatlari yonetin.", "rooms", cancellationToken);
        var inclusiveTax = await LoadInclusiveTaxPercentsAsync(connection, context.SelectedHotel.HotelId, cancellationToken);
        var rooms = await GetRoomSummariesAsync(connection, context.SelectedHotel.HotelId, inclusiveTax, cancellationToken);
        var selectedRoomId = roomId.HasValue && rooms.Any(item => item.RoomId == roomId.Value)
            ? roomId.Value
            : rooms.FirstOrDefault()?.RoomId;

        return new PartnerRoomManagementPageViewModel
        {
            Shell = context.Shell,
            SelectedHotelId = context.SelectedHotel.HotelId,
            SelectedRoomId = selectedRoomId,
            Rooms = rooms,
            InventoryRows = await LoadRoomInventoryRowsAsync(connection, context.SelectedHotel.HotelId, inclusiveTax, cancellationToken),
            SelectedRoomPhotos = selectedRoomId.HasValue
                ? await LoadRoomPhotosAsync(connection, context.SelectedHotel.HotelId, selectedRoomId.Value, cancellationToken)
                : new List<PartnerRoomPhotoCardViewModel>(),
            AvailableRoomFeatures = await LoadActiveRoomFeaturesAsync(connection, cancellationToken),
            SelectedRoomFeatureIds = selectedRoomId.HasValue
                ? await LoadSelectedRoomFeatureIdsAsync(connection, selectedRoomId.Value, cancellationToken)
                : new List<long>(),
            Form = selectedRoomId.HasValue && roomId.HasValue
                ? await LoadRoomFormAsync(connection, context.SelectedHotel.HotelId, selectedRoomId.Value, cancellationToken)
                : new PartnerRoomUpsertRequest { HotelId = context.SelectedHotel.HotelId },
            PhotoUploadForm = new PartnerRoomPhotoUploadRequest
            {
                HotelId = context.SelectedHotel.HotelId,
                RoomId = selectedRoomId ?? 0
            }
        };
    }

    public async Task<(bool Success, string Message)> UpsertRoomAsync(long userId, PartnerRoomUpsertRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.RoomName) || request.BasePrice <= 0)
            {
                return (false, "Oda adi ve taban fiyat zorunludur.");
            }

            if (request.TotalRooms <= 0)
            {
                return (false, "Toplam oda sayisi en az 1 olmalidir.");
            }

            if (request.MaxAdults < 1)
            {
                return (false, "Maksimum yetişkin sayısı en az 1 olmalıdır.");
            }

            var totalCapacity = request.MaxAdults + request.MaxChildren + request.MaxBabies;
            if (totalCapacity <= 0)
            {
                return (false, "Oda kişi kapasitesi en az 1 olmalıdır.");
            }

            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            var hotel = await EnsureHotelAccessAsync(connection, userId, request.HotelId, cancellationToken);
            var inclusiveTax = await LoadInclusiveTaxPercentsAsync(connection, request.HotelId, cancellationToken);
            var storedNetBasePrice = InclusiveNightlyPricing.PartnerGrossEntryToStoredNet(request.BasePrice, inclusiveTax.VatPercent, inclusiveTax.AccommodationPercent);

            var featuresJson = string.IsNullOrWhiteSpace(request.RoomFeaturesText)
                ? null
                : JsonSerializer.Serialize(request.RoomFeaturesText.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
            if (request.RoomId.HasValue)
            {
                const string updateSql = @"
                UPDATE oda_tipleri
                SET oda_adi = @roomName,
                    oda_kategorisi = @roomCategory,
                    maksimum_kisi_sayisi = @maxPeople,
                    maksimum_yetiskin_sayisi = @maxAdults,
                    maksimum_cocuk_sayisi = @maxChildren,
                    bebek_ucretsiz_mi = @babyFree,
                    yatak_tipi = @bedType,
                    oda_metrekare = @roomSize,
                    manzara_tipi = @viewType,
                    standart_gecelik_fiyat = @basePrice,
                    toplam_oda_sayisi = @totalRooms,
                    kapak_fotografi = COALESCE(@coverPhoto, kapak_fotografi),
                    ozellikler = @features,
                    aktif_mi = @active
                WHERE id = @roomId AND otel_id = @hotelId;";

                await using var updateCommand = new SqlCommand(updateSql, connection);
                BindRoomCommand(updateCommand, request, hotel, featuresJson, storedNetBasePrice);
                updateCommand.Parameters.AddWithValue("@roomId", request.RoomId.Value);
                await updateCommand.ExecuteNonQueryAsync(cancellationToken);
                await SyncRoomFeatureRelationsAsync(connection, request.RoomId.Value, request.SelectedFeatureIds, cancellationToken);
                await SyncHotelRoomCountAsync(connection, hotel.HotelId, cancellationToken);
                return (true, "Oda tipi guncellendi.");
            }

            const string insertSql = @"
            INSERT INTO oda_tipleri
            (otel_id, oda_tip_kodu, oda_adi, oda_kategorisi, maksimum_kisi_sayisi, maksimum_yetiskin_sayisi, maksimum_cocuk_sayisi, yatak_tipi, oda_metrekare, manzara_tipi, standart_gecelik_fiyat, toplam_oda_sayisi, kapak_fotografi, ozellikler, aktif_mi)
            VALUES
            (@hotelId, @roomCode, @roomName, @roomCategory, @maxPeople, @maxAdults, @maxChildren, @bedType, @roomSize, @viewType, @basePrice, @totalRooms, @coverPhoto, @features, @active);
            SELECT CAST(SCOPE_IDENTITY() AS bigint);";

            await using var insertCommand = new SqlCommand(insertSql, connection);
            BindRoomCommand(insertCommand, request, hotel, featuresJson, storedNetBasePrice);
            insertCommand.Parameters.AddWithValue("@roomCode", BuildRoomCode(hotel.HotelId));
            var createdRoomIdRaw = await insertCommand.ExecuteScalarAsync(cancellationToken);
            var createdRoomId = Convert.ToInt64(createdRoomIdRaw ?? 0L, CultureInfo.InvariantCulture);
            if (createdRoomId > 0)
            {
                await SyncRoomFeatureRelationsAsync(connection, createdRoomId, request.SelectedFeatureIds, cancellationToken);
            }

            await SyncHotelRoomCountAsync(connection, hotel.HotelId, cancellationToken);
            return (true, "Yeni oda tipi eklendi.");
        }
        catch (InvalidOperationException ex)
        {
            return (false, ex.Message);
        }
        catch (SqlException ex)
        {
            return (false, $"Veritabanı hatası: {ex.Message}");
        }
        catch (Exception ex)
        {
            return (false, $"Beklenmeyen hata: {ex.Message}");
        }
    }

    public async Task<(bool Success, string Message)> DeleteRoomAsync(long userId, long hotelId, long roomId, CancellationToken cancellationToken = default)
    {
        if (roomId <= 0)
        {
            return (false, "Silinecek oda tipi secimi gecersiz.");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        var hotel = await EnsureHotelAccessAsync(connection, userId, hotelId, cancellationToken);

        const string roomSql = @"
            SELECT TOP (1) id
            FROM oda_tipleri
            WHERE id = @roomId
              AND otel_id = @hotelId;";
        await using (var roomCommand = new SqlCommand(roomSql, connection))
        {
            roomCommand.Parameters.AddWithValue("@roomId", roomId);
            roomCommand.Parameters.AddWithValue("@hotelId", hotel.HotelId);
            var exists = await roomCommand.ExecuteScalarAsync(cancellationToken);
            if (exists is null)
            {
                return (false, "Silinecek oda tipi bulunamadi.");
            }
        }

        var photoPaths = new List<string>();
        if (await TableExistsAsync(connection, "oda_gorselleri", cancellationToken))
        {
            const string photoSql = @"
                SELECT gorsel_url
                FROM oda_gorselleri
                WHERE oda_tip_id = @roomId;";
            await using var photoCommand = new SqlCommand(photoSql, connection);
            photoCommand.Parameters.AddWithValue("@roomId", roomId);
            await using var reader = await photoCommand.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                if (reader.IsDBNull(0))
                {
                    continue;
                }

                var relativePath = reader.GetString(0).TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
                photoPaths.Add(Path.Combine(_environment.WebRootPath, relativePath));
            }
        }

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            async Task DeleteByRoomAsync(string tableName)
            {
                var deleteSql = $@"
                    DELETE FROM {tableName}
                    WHERE oda_tip_id = @roomId;";
                await using var deleteCommand = new SqlCommand(deleteSql, connection, (SqlTransaction)transaction);
                deleteCommand.Parameters.AddWithValue("@roomId", roomId);
                await deleteCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            if (await TableExistsAsync(connection, "oda_fiyat_musaitlik", (SqlTransaction)transaction, cancellationToken))
            {
                await DeleteByRoomAsync("oda_fiyat_musaitlik");
            }

            if (await TableExistsAsync(connection, "oda_tipi_ozellikleri", (SqlTransaction)transaction, cancellationToken))
            {
                await DeleteByRoomAsync("oda_tipi_ozellikleri");
            }

            if (await TableExistsAsync(connection, "oda_gorselleri", (SqlTransaction)transaction, cancellationToken))
            {
                await DeleteByRoomAsync("oda_gorselleri");
            }

            if (await TableExistsAsync(connection, "sepet_blokajlari", (SqlTransaction)transaction, cancellationToken))
            {
                await DeleteByRoomAsync("sepet_blokajlari");
            }

            if (await TableExistsAsync(connection, "rezervasyon_taslaklari", (SqlTransaction)transaction, cancellationToken))
            {
                const string clearDraftRoomSql = @"
                    UPDATE rezervasyon_taslaklari
                    SET oda_tip_id = NULL
                    WHERE oda_tip_id = @roomId;";
                await using var clearDraftRoomCommand = new SqlCommand(clearDraftRoomSql, connection, (SqlTransaction)transaction);
                clearDraftRoomCommand.Parameters.AddWithValue("@roomId", roomId);
                await clearDraftRoomCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            const string deleteRoomSql = @"
                DELETE FROM oda_tipleri
                WHERE id = @roomId
                  AND otel_id = @hotelId;";
            await using var deleteRoomCommand = new SqlCommand(deleteRoomSql, connection, (SqlTransaction)transaction);
            deleteRoomCommand.Parameters.AddWithValue("@roomId", roomId);
            deleteRoomCommand.Parameters.AddWithValue("@hotelId", hotel.HotelId);
            var deletedRoomCount = await deleteRoomCommand.ExecuteNonQueryAsync(cancellationToken);
            if (deletedRoomCount <= 0)
            {
                await transaction.RollbackAsync(cancellationToken);
                return (false, "Silinecek oda tipi bulunamadi.");
            }

            const string syncTotalRoomSql = @"
                UPDATE oteller o
                SET o.toplam_oda_sayisi = COALESCE(
                    (
                        SELECT SUM(CASE WHEN COALESCE(ot.aktif_mi, 1) = 1 THEN COALESCE(ot.toplam_oda_sayisi, 0) ELSE 0 END)
                        FROM oda_tipleri ot
                        WHERE ot.otel_id = o.id
                    ),
                    0
                )
                WHERE o.id = @hotelId;";
            await using var syncRoomCountCommand = new SqlCommand(syncTotalRoomSql, connection, (SqlTransaction)transaction);
            syncRoomCountCommand.Parameters.AddWithValue("@hotelId", hotel.HotelId);
            await syncRoomCountCommand.ExecuteNonQueryAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);
        }
        catch (SqlException ex) when (ex.Number == 1451)
        {
            await transaction.RollbackAsync(cancellationToken);
            return (false, "Bu oda tipine bagli aktif rezervasyon kayitlari oldugu icin silinemedi. Once rezervasyon baglantilarini temizleyin.");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            return (false, $"Oda tipi silinirken hata olustu: {ex.Message}");
        }

        foreach (var physicalPath in photoPaths.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            await _imageStorageService.DeleteAsync(physicalPath, cancellationToken);
        }

        return (true, "Oda tipi ve bagli fiyat/gorsel/kural verileri temizlenerek silindi.");
    }

    public async Task<(bool Success, string Message)> UploadRoomPhotosAsync(long userId, PartnerRoomPhotoUploadRequest request, CancellationToken cancellationToken = default)
    {
        if (request.RoomId <= 0)
        {
            return (false, "Önce görsel yüklenecek oda tipini seçiniz.");
        }

        if (request.Files is null || request.Files.Count == 0)
        {
            return (false, "En az bir oda görseli seçmelisiniz.");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        var hotel = await EnsureHotelAccessAsync(connection, userId, request.HotelId, cancellationToken);
        var room = await EnsureRoomBelongsToHotelAsync(connection, hotel.HotelId, request.RoomId, cancellationToken);
        var targetDirectory = MediaStoragePaths.RoomImagesDirectory(_environment.WebRootPath, request.HotelId, request.RoomId);
        Directory.CreateDirectory(targetDirectory);

        var savedPhysicalPaths = new List<string>();
        var displayOrder = request.DisplayOrder;
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            var shouldMakeCover = request.MakeCover;
            foreach (var file in request.Files.Where(static item => item.Length > 0))
            {
                var storedImage = await _imageStorageService.SaveAsWebpAsync(file, targetDirectory, $"partner-oda-{room.RoomId}", cancellationToken);
                var relativePath = MediaStoragePaths.RoomImagesUrl(request.HotelId, request.RoomId, storedImage.FileName);
                savedPhysicalPaths.Add(Path.Combine(targetDirectory, storedImage.FileName));

                const string insertSql = @"
                    INSERT INTO oda_gorselleri
                    (oda_tip_id, gorsel_url, baslik, aciklama, kapak_fotografi_mi, siralama, boyut_kb, onay_durumu, yukleyen_kullanici_id, olusturulma_tarihi)
                    VALUES
                    (@roomId, @url, @title, @description, @isCover, @sortOrder, @sizeKb, 'Onaylandı', @userId, CURRENT_TIMESTAMP);";
                await using var insertCommand = new SqlCommand(insertSql, connection, (SqlTransaction)transaction);
                insertCommand.Parameters.AddWithValue("@roomId", request.RoomId);
                insertCommand.Parameters.AddWithValue("@url", relativePath);
                insertCommand.Parameters.AddWithValue("@title", string.IsNullOrWhiteSpace(request.Title) ? room.RoomName : request.Title.Trim());
                insertCommand.Parameters.AddWithValue("@description", DbValue(request.Description));
                insertCommand.Parameters.AddWithValue("@isCover", shouldMakeCover ? 1 : 0);
                insertCommand.Parameters.AddWithValue("@sortOrder", displayOrder);
                insertCommand.Parameters.AddWithValue("@sizeKb", Math.Max(1, (int)Math.Round(storedImage.FileSizeBytes / 1024m)));
                insertCommand.Parameters.AddWithValue("@userId", userId);
                await insertCommand.ExecuteNonQueryAsync(cancellationToken);

                if (shouldMakeCover)
                {
                    await UpdateRoomCoverSelectionAsync(connection, (SqlTransaction)transaction, request.RoomId, relativePath, cancellationToken);
                    shouldMakeCover = false;
                }

                displayOrder++;
            }

            await transaction.CommitAsync(cancellationToken);
            return (true, $"{savedPhysicalPaths.Count} oda görseli yüklendi.");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            foreach (var path in savedPhysicalPaths)
            {
                await _imageStorageService.DeleteAsync(path, cancellationToken);
            }

            return (false, $"Oda görselleri yüklenirken hata oluştu: {ex.Message}");
        }
    }

    public async Task<(bool Success, string Message)> SetRoomCoverAsync(long userId, long hotelId, long roomId, long photoId, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await EnsureHotelAccessAsync(connection, userId, hotelId, cancellationToken);
        await EnsureRoomBelongsToHotelAsync(connection, hotelId, roomId, cancellationToken);

        const string selectSql = @"
            SELECT TOP (1) gorsel_url
            FROM oda_gorselleri
            WHERE id = @photoId
              AND oda_tip_id = @roomId;";
        await using var selectCommand = new SqlCommand(selectSql, connection);
        selectCommand.Parameters.AddWithValue("@photoId", photoId);
        selectCommand.Parameters.AddWithValue("@roomId", roomId);
        var url = await selectCommand.ExecuteScalarAsync(cancellationToken) as string;
        if (string.IsNullOrWhiteSpace(url))
        {
            return (false, "Kapak yapılacak oda görseli bulunamadı.");
        }

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        await UpdateRoomCoverSelectionAsync(connection, (SqlTransaction)transaction, roomId, url, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return (true, "Oda vitrin görseli güncellendi.");
    }

    public async Task<(bool Success, string Message)> DeleteRoomPhotoAsync(long userId, long hotelId, long roomId, long photoId, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await EnsureHotelAccessAsync(connection, userId, hotelId, cancellationToken);
        await EnsureRoomBelongsToHotelAsync(connection, hotelId, roomId, cancellationToken);

        string? relativePath = null;
        var wasCover = false;
        const string selectSql = @"
            SELECT TOP (1) gorsel_url, kapak_fotografi_mi
            FROM oda_gorselleri
            WHERE id = @photoId
              AND oda_tip_id = @roomId;";
        await using (var selectCommand = new SqlCommand(selectSql, connection))
        {
            selectCommand.Parameters.AddWithValue("@photoId", photoId);
            selectCommand.Parameters.AddWithValue("@roomId", roomId);
            await using var reader = await selectCommand.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                relativePath = reader.IsDBNull(0) ? null : reader.GetString(0);
                wasCover = SafeBool(reader, 1);
            }
        }

        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return (false, "Silinecek oda görseli bulunamadı.");
        }

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        await using (var deleteCommand = new SqlCommand("DELETE FROM oda_gorselleri WHERE id = @photoId AND oda_tip_id = @roomId;", connection, (SqlTransaction)transaction))
        {
            deleteCommand.Parameters.AddWithValue("@photoId", photoId);
            deleteCommand.Parameters.AddWithValue("@roomId", roomId);
            await deleteCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        if (wasCover)
        {
            await PromoteNextRoomCoverAsync(connection, (SqlTransaction)transaction, roomId, cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
        var physicalPath = Path.Combine(_environment.WebRootPath, relativePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        await _imageStorageService.DeleteAsync(physicalPath, cancellationToken);
        return (true, "Oda görseli silindi.");
    }

    public async Task<PartnerHotelInfoPageViewModel> GetHotelInfoAsync(long userId, long? hotelId = null, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var context = await BuildContextAsync(connection, userId, hotelId, "Otel Bilgileri", "Referans paneldeki alanlarin tamami veritabani kolonlari ile yonetilir.", "hotel-info", cancellationToken);
        var model = new PartnerHotelInfoPageViewModel
        {
            Shell = context.Shell,
            Form = await LoadHotelInfoFormAsync(connection, context.SelectedHotel.HotelId, cancellationToken)
        };

        model.HotelTypes = await LoadHotelTypeOptionsAsync(connection, cancellationToken);
        model.AvailableAmenities = await LoadAmenityOptionsAsync(connection, context.SelectedHotel.HotelId, cancellationToken);
        model.Form.SelectedAmenityIds = model.AvailableAmenities.Where(static item => item.IsSelected).Select(static item => item.AmenityId).ToList();
        return model;
    }

    public async Task<(bool Success, string Message)> UpdateHotelInfoAsync(long userId, PartnerHotelInfoForm request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.HotelName))
        {
            return (false, "Otel adi bos birakilamaz.");
        }

        var numericValidation = ValidateHotelInfoNumbers(request);
        if (!string.IsNullOrWhiteSpace(numericValidation))
        {
            return (false, numericValidation);
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        var hotel = await EnsureHotelAccessAsync(connection, userId, request.HotelId, cancellationToken);

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
        const string updateSql = @"
                UPDATE oteller
                SET otel_adi = @hotelName,
                    otel_turu = @hotelType,
                    otel_tipi_id = @hotelTypeId,
                    turizm_belge_no = @tourismDocumentNo,
                    turizm_belge_turu = @tourismDocumentType,
                    ulke = @country,
                    kisa_aciklama = @shortDescription,
                    uzun_aciklama = @description,
                    tam_adres = @address,
                    sehir = @city,
                    ilce = @district,
                    mahalle = @neighborhood,
                    posta_kodu = @postalCode,
                    konum_aciklamasi = @locationDescription,
                    enlem = @latitude,
                    boylam = @longitude,
                    web_sitesi = @website,
                    eposta = @contactEmail,
                    telefon_1 = @hotelPhone,
                    telefon_2 = @hotelPhone2,
                    rezervasyon_telefonu = @reservationPhone,
                    faks = @fax,
                    satis_kontak_adi = @salesContactName,
                    satis_kontak_telefonu = @salesContactPhone,
                    satis_kontak_eposta = @salesContactEmail,
                    satis_notlari = @salesNotes,
                    check_in_saati = @checkIn,
                    check_out_saati = @checkOut,
                    gec_check_out_mumkun_mu = @lateCheckoutAvailable,
                    gec_check_out_ucreti = @lateCheckoutFee,
                    erken_check_in_mumkun_mu = @earlyCheckinAvailable,
                    erken_check_in_ucreti = @earlyCheckinFee,
                    minimum_konaklama_gecesi = @minStay,
                    maksimum_konaklama_gecesi = @maxStay,
                    yildiz_sayisi = @starCount,
                    toplam_oda_sayisi = @totalRoomCount,
                    toplam_yatak_kapasitesi = @totalBedCapacity,
                    kat_sayisi = @floorCount,
                    asansor_var_mi = @elevatorAvailable,
                    asansor_sayisi = @elevatorCount,
                    konusulan_diller = @spokenLanguages,
                    video_url = @videoUrl,
                    sanal_tur_url = @virtualTourUrl,
                    guncellenme_tarihi = GETDATE()
                WHERE id = @hotelId;";

            await using (var updateCommand = new SqlCommand(updateSql, connection, (SqlTransaction)transaction))
            {
                updateCommand.Parameters.AddWithValue("@hotelId", hotel.HotelId);
                updateCommand.Parameters.AddWithValue("@hotelName", request.HotelName.Trim());
                var hotelType = await ResolvePartnerHotelTypeAsync(connection, request.HotelTypeId, (SqlTransaction)transaction, cancellationToken);
                updateCommand.Parameters.AddWithValue("@hotelType", hotelType.Name);
                updateCommand.Parameters.AddWithValue("@hotelTypeId", hotelType.Id);
                updateCommand.Parameters.AddWithValue("@tourismDocumentNo", (object?)request.TourismDocumentNo ?? DBNull.Value);
                updateCommand.Parameters.AddWithValue("@tourismDocumentType", string.IsNullOrWhiteSpace(request.TourismDocumentType) ? DBNull.Value : request.TourismDocumentType);
                updateCommand.Parameters.AddWithValue("@country", string.IsNullOrWhiteSpace(request.Country) ? "Türkiye" : request.Country.Trim());
                updateCommand.Parameters.AddWithValue("@shortDescription", (object?)request.ShortDescription ?? DBNull.Value);
                updateCommand.Parameters.AddWithValue("@description", (object?)request.Description ?? DBNull.Value);
                updateCommand.Parameters.AddWithValue("@address", (object?)request.Address ?? DBNull.Value);
                updateCommand.Parameters.AddWithValue("@city", (object?)request.City ?? DBNull.Value);
                updateCommand.Parameters.AddWithValue("@district", (object?)request.District ?? DBNull.Value);
                updateCommand.Parameters.AddWithValue("@neighborhood", (object?)request.Neighborhood ?? DBNull.Value);
                updateCommand.Parameters.AddWithValue("@postalCode", (object?)request.PostalCode ?? DBNull.Value);
                updateCommand.Parameters.AddWithValue("@locationDescription", (object?)request.LocationDescription ?? DBNull.Value);
                updateCommand.Parameters.AddWithValue("@latitude", request.Latitude.HasValue ? decimal.Round(request.Latitude.Value, 8) : DBNull.Value);
                updateCommand.Parameters.AddWithValue("@longitude", request.Longitude.HasValue ? decimal.Round(request.Longitude.Value, 8) : DBNull.Value);
                updateCommand.Parameters.AddWithValue("@website", (object?)request.Website ?? DBNull.Value);
                updateCommand.Parameters.AddWithValue("@contactEmail", (object?)request.ContactEmail ?? DBNull.Value);
                updateCommand.Parameters.AddWithValue("@hotelPhone", (object?)request.HotelPhone ?? DBNull.Value);
                updateCommand.Parameters.AddWithValue("@hotelPhone2", (object?)request.HotelPhone2 ?? DBNull.Value);
                updateCommand.Parameters.AddWithValue("@reservationPhone", (object?)request.ReservationPhone ?? DBNull.Value);
                updateCommand.Parameters.AddWithValue("@fax", (object?)request.Fax ?? DBNull.Value);
                updateCommand.Parameters.AddWithValue("@salesContactName", (object?)request.SalesContactName ?? DBNull.Value);
                updateCommand.Parameters.AddWithValue("@salesContactPhone", (object?)request.SalesContactPhone ?? DBNull.Value);
                updateCommand.Parameters.AddWithValue("@salesContactEmail", (object?)request.SalesContactEmail ?? DBNull.Value);
                updateCommand.Parameters.AddWithValue("@salesNotes", (object?)request.SalesNotes ?? DBNull.Value);
                updateCommand.Parameters.AddWithValue("@checkIn", string.IsNullOrWhiteSpace(request.CheckInTime) ? DBNull.Value : TimeSpan.Parse(request.CheckInTime));
                updateCommand.Parameters.AddWithValue("@checkOut", string.IsNullOrWhiteSpace(request.CheckOutTime) ? DBNull.Value : TimeSpan.Parse(request.CheckOutTime));
                updateCommand.Parameters.AddWithValue("@lateCheckoutAvailable", request.LateCheckoutAvailable ? 1 : 0);
                updateCommand.Parameters.AddWithValue("@lateCheckoutFee", request.LateCheckoutFee.HasValue ? decimal.Round(request.LateCheckoutFee.Value, 2) : DBNull.Value);
                updateCommand.Parameters.AddWithValue("@earlyCheckinAvailable", request.EarlyCheckinAvailable ? 1 : 0);
                updateCommand.Parameters.AddWithValue("@earlyCheckinFee", request.EarlyCheckinFee.HasValue ? decimal.Round(request.EarlyCheckinFee.Value, 2) : DBNull.Value);
                updateCommand.Parameters.AddWithValue("@minStay", request.MinStay);
                updateCommand.Parameters.AddWithValue("@maxStay", request.MaxStay);
                updateCommand.Parameters.AddWithValue("@starCount", request.StarCount.HasValue ? request.StarCount.Value : DBNull.Value);
                updateCommand.Parameters.AddWithValue("@totalRoomCount", request.TotalRoomCount);
                updateCommand.Parameters.AddWithValue("@totalBedCapacity", request.TotalBedCapacity.HasValue ? request.TotalBedCapacity.Value : DBNull.Value);
                updateCommand.Parameters.AddWithValue("@floorCount", request.FloorCount.HasValue ? request.FloorCount.Value : DBNull.Value);
                updateCommand.Parameters.AddWithValue("@elevatorAvailable", request.ElevatorAvailable ? 1 : 0);
                updateCommand.Parameters.AddWithValue("@elevatorCount", request.ElevatorCount);
                updateCommand.Parameters.AddWithValue("@spokenLanguages", string.IsNullOrWhiteSpace(request.SpokenLanguages) ? DBNull.Value : request.SpokenLanguages);
                updateCommand.Parameters.AddWithValue("@videoUrl", (object?)request.VideoUrl ?? DBNull.Value);
                updateCommand.Parameters.AddWithValue("@virtualTourUrl", (object?)request.VirtualTourUrl ?? DBNull.Value);
                await updateCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            await using (var deleteCommand = new SqlCommand("DELETE FROM otel_ozellik_iliskileri WHERE otel_id = @hotelId;", connection, (SqlTransaction)transaction))
            {
                deleteCommand.Parameters.AddWithValue("@hotelId", hotel.HotelId);
                await deleteCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            foreach (var amenityId in request.SelectedAmenityIds.Distinct())
            {
                await using var insertAmenity = new SqlCommand("INSERT INTO otel_ozellik_iliskileri (otel_id, ozellik_id) VALUES (@hotelId, @amenityId);", connection, (SqlTransaction)transaction);
                insertAmenity.Parameters.AddWithValue("@hotelId", hotel.HotelId);
                insertAmenity.Parameters.AddWithValue("@amenityId", amenityId);
                await insertAmenity.ExecuteNonQueryAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
            return (true, "Otel bilgileri guncellendi.");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            return (false, $"Otel bilgileri kaydedilemedi: {ex.Message}");
        }
    }

    public async Task<(bool Success, string Message)> UpdateHotelAmenitiesAsync(long userId, PartnerHotelAmenitiesUpdateRequest request, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        var hotel = await EnsureHotelAccessAsync(connection, userId, request.HotelId, cancellationToken);

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            await using (var deleteCommand = new SqlCommand("DELETE FROM otel_ozellik_iliskileri WHERE otel_id = @hotelId;", connection, (SqlTransaction)transaction))
            {
                deleteCommand.Parameters.AddWithValue("@hotelId", hotel.HotelId);
                await deleteCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            foreach (var amenityId in (request.SelectedAmenityIds ?? new List<long>()).Distinct())
            {
                await using var insertAmenity = new SqlCommand("INSERT INTO otel_ozellik_iliskileri (otel_id, ozellik_id) VALUES (@hotelId, @amenityId);", connection, (SqlTransaction)transaction);
                insertAmenity.Parameters.AddWithValue("@hotelId", hotel.HotelId);
                insertAmenity.Parameters.AddWithValue("@amenityId", amenityId);
                await insertAmenity.ExecuteNonQueryAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
            return (true, "Tesis özellikleri güncellendi.");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            return (false, $"Tesis özellikleri kaydedilemedi: {ex.Message}");
        }
    }

    public async Task<(bool Success, string Message)> UpdateHotelLocationAsync(long userId, PartnerHotelLocationUpdateRequest request, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        var hotel = await EnsureHotelAccessAsync(connection, userId, request.HotelId, cancellationToken);

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            await ResolveLocationNamesAsync(connection, request, cancellationToken);
            const string sql = @"
                UPDATE oteller
                SET ulke = @country,
                    sehir = @city,
                    ilce = @district,
                    mahalle = @neighborhood,
                    posta_kodu = @postalCode,
                    tam_adres = @address,
                    konum_aciklamasi = @locationDescription,
                    enlem = @latitude,
                    boylam = @longitude,
                    guncellenme_tarihi = GETDATE()
                WHERE id = @hotelId;";

            await using (var command = new SqlCommand(sql, connection, (SqlTransaction)transaction))
            {
                command.Parameters.AddWithValue("@hotelId", hotel.HotelId);
                command.Parameters.AddWithValue("@country", string.IsNullOrWhiteSpace(request.Country) ? "Türkiye" : request.Country.Trim());
                command.Parameters.AddWithValue("@city", string.IsNullOrWhiteSpace(request.City) ? DBNull.Value : request.City.Trim());
                command.Parameters.AddWithValue("@district", string.IsNullOrWhiteSpace(request.District) ? DBNull.Value : request.District.Trim());
                command.Parameters.AddWithValue("@neighborhood", string.IsNullOrWhiteSpace(request.Neighborhood) ? DBNull.Value : request.Neighborhood.Trim());
                command.Parameters.AddWithValue("@postalCode", string.IsNullOrWhiteSpace(request.PostalCode) ? DBNull.Value : request.PostalCode.Trim());
                command.Parameters.AddWithValue("@address", string.IsNullOrWhiteSpace(request.Address) ? DBNull.Value : request.Address.Trim());
                command.Parameters.AddWithValue("@locationDescription", string.IsNullOrWhiteSpace(request.LocationDescription) ? DBNull.Value : request.LocationDescription.Trim());
                command.Parameters.AddWithValue("@latitude", request.Latitude.HasValue ? decimal.Round(request.Latitude.Value, 8) : DBNull.Value);
                command.Parameters.AddWithValue("@longitude", request.Longitude.HasValue ? decimal.Round(request.Longitude.Value, 8) : DBNull.Value);
                await command.ExecuteNonQueryAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
            return (true, "Konum bilgileri güncellendi.");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            return (false, $"Konum bilgileri kaydedilemedi: {ex.Message}");
        }
    }

    public async Task<PartnerHotelLocationPageViewModel> GetHotelLocationAsync(long userId, long? hotelId = null, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var context = await BuildContextAsync(connection, userId, hotelId, "Konum ve Harita", "Adres, koordinat ve konum açıklaması otel detay ve harita aramalarında kullanılır.", "hotel-info", cancellationToken);
        var info = await LoadHotelInfoFormAsync(connection, context.SelectedHotel.HotelId, cancellationToken);

        var cities = await LoadCityOptionsAsync(connection, cancellationToken);
        var selectedCityId = ResolveOptionIdByName(cities, info.City);
        var districts = selectedCityId.HasValue ? await LoadDistrictOptionsAsync(connection, selectedCityId.Value, cancellationToken) : new List<PartnerLocationOptionViewModel>();
        var selectedDistrictId = ResolveOptionIdByName(districts, info.District);
        var neighborhoods = selectedDistrictId.HasValue ? await LoadNeighborhoodOptionsAsync(connection, selectedDistrictId.Value, cancellationToken) : new List<PartnerLocationOptionViewModel>();
        var selectedNeighborhoodId = ResolveOptionIdByName(neighborhoods, info.Neighborhood);

        return new PartnerHotelLocationPageViewModel
        {
            Shell = context.Shell,
            Form = info,
            Cities = cities,
            Districts = districts,
            Neighborhoods = neighborhoods,
            SelectedCityId = selectedCityId,
            SelectedDistrictId = selectedDistrictId,
            SelectedNeighborhoodId = selectedNeighborhoodId
        };
    }

    public async Task<List<PartnerLocationOptionViewModel>> GetCityOptionsAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        return await LoadCityOptionsAsync(connection, cancellationToken);
    }

    public async Task<List<PartnerLocationOptionViewModel>> GetDistrictOptionsAsync(long cityId, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        return await LoadDistrictOptionsAsync(connection, cityId, cancellationToken);
    }

    public async Task<List<PartnerLocationOptionViewModel>> GetNeighborhoodOptionsAsync(long districtId, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        return await LoadNeighborhoodOptionsAsync(connection, districtId, cancellationToken);
    }

    private static long? ResolveOptionIdByName(List<PartnerLocationOptionViewModel> options, string? name)
    {
        if (options.Count == 0 || string.IsNullOrWhiteSpace(name)) return null;
        var normalized = NormalizeLocationName(name);
        foreach (var row in options)
        {
            if (NormalizeLocationName(row.Name) == normalized) return row.Id;
        }
        return null;
    }

    private static string NormalizeLocationName(string value)
    {
        return value.Trim().Replace("İ", "I", StringComparison.OrdinalIgnoreCase).Replace("ı", "i", StringComparison.OrdinalIgnoreCase).ToLowerInvariant();
    }

    private static async Task ResolveLocationNamesAsync(SqlConnection connection, PartnerHotelLocationUpdateRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.City) && request.CityId.HasValue)
        {
            await using var cmd = new SqlCommand("SELECT TOP (1) il_adi FROM iller WHERE id = @id;", connection);
            cmd.Parameters.AddWithValue("@id", request.CityId.Value);
            var value = await cmd.ExecuteScalarAsync(cancellationToken);
            if (value is string s && !string.IsNullOrWhiteSpace(s)) request.City = s.Trim();
        }

        if (string.IsNullOrWhiteSpace(request.District) && request.DistrictId.HasValue)
        {
            await using var cmd = new SqlCommand("SELECT TOP (1) ilce_adi FROM ilceler WHERE id = @id;", connection);
            cmd.Parameters.AddWithValue("@id", request.DistrictId.Value);
            var value = await cmd.ExecuteScalarAsync(cancellationToken);
            if (value is string s && !string.IsNullOrWhiteSpace(s)) request.District = s.Trim();
        }

        if (string.IsNullOrWhiteSpace(request.Neighborhood) && request.NeighborhoodId.HasValue)
        {
            await using var cmd = new SqlCommand("SELECT TOP (1) mahalle_adi FROM mahalleler WHERE id = @id;", connection);
            cmd.Parameters.AddWithValue("@id", request.NeighborhoodId.Value);
            var value = await cmd.ExecuteScalarAsync(cancellationToken);
            if (value is string s && !string.IsNullOrWhiteSpace(s)) request.Neighborhood = s.Trim();
        }
    }

    private static async Task<List<PartnerLocationOptionViewModel>> LoadCityOptionsAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        var rows = new List<PartnerLocationOptionViewModel>();
        if (!await TableExistsAsync(connection, "iller", cancellationToken)) return rows;

        const string sql = "SELECT id, il_adi FROM iller WHERE aktif_mi = 1 ORDER BY il_adi;";
        await using var cmd = new SqlCommand(sql, connection);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(new PartnerLocationOptionViewModel
            {
                Id = reader.IsDBNull(0) ? 0 : reader.GetInt64(0),
                Name = reader.IsDBNull(1) ? string.Empty : reader.GetString(1)
            });
        }
        rows.RemoveAll(x => x.Id <= 0 || string.IsNullOrWhiteSpace(x.Name));
        return rows;
    }

    private static async Task<List<PartnerLocationOptionViewModel>> LoadDistrictOptionsAsync(SqlConnection connection, long cityId, CancellationToken cancellationToken)
    {
        var rows = new List<PartnerLocationOptionViewModel>();
        if (cityId <= 0) return rows;
        if (!await TableExistsAsync(connection, "ilceler", cancellationToken)) return rows;

        const string sql = "SELECT id, ilce_adi FROM ilceler WHERE aktif_mi = 1 AND il_id = @cityId ORDER BY ilce_adi;";
        await using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@cityId", cityId);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(new PartnerLocationOptionViewModel
            {
                Id = reader.IsDBNull(0) ? 0 : reader.GetInt64(0),
                Name = reader.IsDBNull(1) ? string.Empty : reader.GetString(1)
            });
        }
        rows.RemoveAll(x => x.Id <= 0 || string.IsNullOrWhiteSpace(x.Name));
        return rows;
    }

    private static async Task<List<PartnerLocationOptionViewModel>> LoadNeighborhoodOptionsAsync(SqlConnection connection, long districtId, CancellationToken cancellationToken)
    {
        var rows = new List<PartnerLocationOptionViewModel>();
        if (districtId <= 0) return rows;
        if (!await TableExistsAsync(connection, "mahalleler", cancellationToken)) return rows;

        const string sql = "SELECT id, mahalle_adi FROM mahalleler WHERE aktif_mi = 1 AND ilce_id = @districtId ORDER BY mahalle_adi;";
        await using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@districtId", districtId);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(new PartnerLocationOptionViewModel
            {
                Id = reader.IsDBNull(0) ? 0 : reader.GetInt64(0),
                Name = reader.IsDBNull(1) ? string.Empty : reader.GetString(1)
            });
        }
        rows.RemoveAll(x => x.Id <= 0 || string.IsNullOrWhiteSpace(x.Name));
        return rows;
    }

    public async Task<PartnerHotelPoliciesPageViewModel> GetHotelPoliciesAsync(long userId, long? hotelId = null, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var context = await BuildContextAsync(connection, userId, hotelId, "Otel Koşulları", "Misafire gösterilen kuralları ve ödeme/iptal politikalarını yönetin.", "hotel-info", cancellationToken);
        var form = await LoadHotelPoliciesFormAsync(connection, context.SelectedHotel.HotelId, cancellationToken);
        var policyOptions = await LoadHotelPolicyDictionaryAsync(connection, context.SelectedHotel.HotelId, cancellationToken);

        var model = new PartnerHotelPoliciesPageViewModel
        {
            Shell = context.Shell,
            SelectedHotelId = context.SelectedHotel.HotelId,
            Form = form,
            PolicyOptions = policyOptions
        };
        return model;
    }

    public async Task<(bool Success, string Message)> UpdateHotelPoliciesAsync(long userId, PartnerHotelPoliciesForm request, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        var hotel = await EnsureHotelAccessAsync(connection, userId, request.HotelId, cancellationToken);

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            await UpsertHotelPolicySelectionsAsync(connection, (SqlTransaction)transaction, hotel.HotelId, request.SelectedPolicyIds, cancellationToken);

            const string upsertSql = @"
                IF EXISTS (SELECT 1 FROM otel_kosullari WHERE otel_id = @hotelId)
                BEGIN
                    UPDATE otel_kosullari
                    SET sigara_politikasi = @smoking,
                        evcil_hayvan_politikasi = @petPolicy,
                        evcil_hayvan_ucreti = @petFee,
                        evcil_hayvan_depozitosu = @petDeposit,
                        parti_etkinlik_izin = @partyAllowed,
                        sessizlik_saatleri_baslangic = @quietStart,
                        sessizlik_saatleri_bitis = @quietEnd,
                        minimum_yas_siniri = @minAge,
                        sadece_yetiskinlere_mi = @adultsOnly,
                        cocuk_kabul_yas_araligi = @childRange,
                        bebek_karyolasi_var_mi = @cribAvailable,
                        bebek_karyolasi_ucreti = @cribFee,
                        ekstra_yatak_var_mi = @extraBed,
                        ekstra_yatak_ucreti = @extraBedFee,
                        maksimum_cocuk_sayisi = @maxChildren,
                        on_odeme_gerekli_mi = @prepayRequired,
                        on_odeme_orani = @prepayPercent,
                        kalan_odeme_zamani = @remainingPayTime,
                        kredi_karti_ile_odeme_kabul = @ccAccepted,
                        nakit_odeme_kabul = @cashAccepted,
                        kabul_edilen_kartlar = @acceptedCards,
                        iptal_politikasi_ozet = @cancelSummary,
                        detayli_iptal_kosullari = @cancelDetails,
                        ucretsiz_iptal_suresi = @freeCancelDays,
                        gec_iptal_ceza_orani = @lateCancelPenalty,
                        no_show_ceza_orani = @noShowPenalty,
                        hasar_depozitosu_tutari = @damageDeposit,
                        hasar_depozitosu_aciklamasi = @damageDepositDesc,
                        disaridan_yiyecek_icecek_serbest_mi = @outsideFoodAllowed,
                        ziyaretci_kabul_edilir_mi = @visitorAllowed,
                        ziyaretci_saati_baslangic = @visitorStart,
                        ziyaretci_saati_bitis = @visitorEnd,
                        ozel_kosullar = @specialConditions,
                        guncellenme_tarihi = GETDATE()
                    WHERE otel_id = @hotelId;
                END
                ELSE
                BEGIN
                    INSERT INTO otel_kosullari
                    (otel_id, sigara_politikasi, evcil_hayvan_politikasi, evcil_hayvan_ucreti, evcil_hayvan_depozitosu, parti_etkinlik_izin,
                     sessizlik_saatleri_baslangic, sessizlik_saatleri_bitis, minimum_yas_siniri, sadece_yetiskinlere_mi, cocuk_kabul_yas_araligi,
                     bebek_karyolasi_var_mi, bebek_karyolasi_ucreti, ekstra_yatak_var_mi, ekstra_yatak_ucreti, maksimum_cocuk_sayisi,
                     on_odeme_gerekli_mi, on_odeme_orani, kalan_odeme_zamani, kredi_karti_ile_odeme_kabul, nakit_odeme_kabul, kabul_edilen_kartlar,
                     iptal_politikasi_ozet, detayli_iptal_kosullari, ucretsiz_iptal_suresi, gec_iptal_ceza_orani, no_show_ceza_orani,
                     hasar_depozitosu_tutari, hasar_depozitosu_aciklamasi, disaridan_yiyecek_icecek_serbest_mi, ziyaretci_kabul_edilir_mi,
                     ziyaretci_saati_baslangic, ziyaretci_saati_bitis, ozel_kosullar, guncellenme_tarihi)
                    VALUES
                    (@hotelId, @smoking, @petPolicy, @petFee, @petDeposit, @partyAllowed,
                     @quietStart, @quietEnd, @minAge, @adultsOnly, @childRange,
                     @cribAvailable, @cribFee, @extraBed, @extraBedFee, @maxChildren,
                     @prepayRequired, @prepayPercent, @remainingPayTime, @ccAccepted, @cashAccepted, @acceptedCards,
                     @cancelSummary, @cancelDetails, @freeCancelDays, @lateCancelPenalty, @noShowPenalty,
                     @damageDeposit, @damageDepositDesc, @outsideFoodAllowed, @visitorAllowed,
                     @visitorStart, @visitorEnd, @specialConditions, GETDATE());
                END";

            await using var command = new SqlCommand(upsertSql, connection, (SqlTransaction)transaction);
            command.Parameters.AddWithValue("@hotelId", hotel.HotelId);
            command.Parameters.AddWithValue("@smoking", (object?)request.SmokingPolicy ?? DBNull.Value);
            command.Parameters.AddWithValue("@petPolicy", (object?)request.PetPolicy ?? DBNull.Value);
            command.Parameters.AddWithValue("@petFee", request.PetFee.HasValue ? decimal.Round(request.PetFee.Value, 2) : DBNull.Value);
            command.Parameters.AddWithValue("@petDeposit", request.PetDeposit.HasValue ? decimal.Round(request.PetDeposit.Value, 2) : DBNull.Value);
            command.Parameters.AddWithValue("@partyAllowed", request.PartyAllowed ? 1 : 0);
            command.Parameters.AddWithValue("@quietStart", ParseTimeOrNull(request.QuietHoursStart) ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@quietEnd", ParseTimeOrNull(request.QuietHoursEnd) ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@minAge", request.MinimumAgeLimit.HasValue ? request.MinimumAgeLimit.Value : DBNull.Value);
            command.Parameters.AddWithValue("@adultsOnly", request.AdultsOnly ? 1 : 0);
            command.Parameters.AddWithValue("@childRange", (object?)request.ChildAgeRange ?? DBNull.Value);
            command.Parameters.AddWithValue("@cribAvailable", request.BabyCribAvailable ? 1 : 0);
            command.Parameters.AddWithValue("@cribFee", request.BabyCribFee.HasValue ? decimal.Round(request.BabyCribFee.Value, 2) : DBNull.Value);
            command.Parameters.AddWithValue("@extraBed", request.ExtraBedAvailable ? 1 : 0);
            command.Parameters.AddWithValue("@extraBedFee", request.ExtraBedFee.HasValue ? decimal.Round(request.ExtraBedFee.Value, 2) : DBNull.Value);
            command.Parameters.AddWithValue("@maxChildren", request.MaxChildren.HasValue ? request.MaxChildren.Value : DBNull.Value);
            command.Parameters.AddWithValue("@prepayRequired", request.PrepaymentRequired ? 1 : 0);
            command.Parameters.AddWithValue("@prepayPercent", request.PrepaymentPercent.HasValue ? decimal.Round(request.PrepaymentPercent.Value, 2) : DBNull.Value);
            command.Parameters.AddWithValue("@remainingPayTime", (object?)request.RemainingPaymentTimeText ?? DBNull.Value);
            command.Parameters.AddWithValue("@ccAccepted", request.CreditCardPaymentAccepted ? 1 : 0);
            command.Parameters.AddWithValue("@cashAccepted", request.CashPaymentAccepted ? 1 : 0);
            command.Parameters.AddWithValue("@acceptedCards", (object?)request.AcceptedCardsText ?? DBNull.Value);
            command.Parameters.AddWithValue("@cancelSummary", (object?)request.CancellationSummary ?? DBNull.Value);
            command.Parameters.AddWithValue("@cancelDetails", (object?)request.CancellationDetails ?? DBNull.Value);
            command.Parameters.AddWithValue("@freeCancelDays", request.FreeCancellationDays.HasValue ? request.FreeCancellationDays.Value : DBNull.Value);
            command.Parameters.AddWithValue("@lateCancelPenalty", request.LateCancellationPenaltyPercent.HasValue ? decimal.Round(request.LateCancellationPenaltyPercent.Value, 2) : DBNull.Value);
            command.Parameters.AddWithValue("@noShowPenalty", request.NoShowPenaltyPercent.HasValue ? decimal.Round(request.NoShowPenaltyPercent.Value, 2) : DBNull.Value);
            command.Parameters.AddWithValue("@damageDeposit", request.DamageDepositAmount.HasValue ? decimal.Round(request.DamageDepositAmount.Value, 2) : DBNull.Value);
            command.Parameters.AddWithValue("@damageDepositDesc", (object?)request.DamageDepositDescription ?? DBNull.Value);
            command.Parameters.AddWithValue("@outsideFoodAllowed", request.OutsideFoodAllowed ? 1 : 0);
            command.Parameters.AddWithValue("@visitorAllowed", request.VisitorAllowed ? 1 : 0);
            command.Parameters.AddWithValue("@visitorStart", ParseTimeOrNull(request.VisitorHoursStart) ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@visitorEnd", ParseTimeOrNull(request.VisitorHoursEnd) ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@specialConditions", (object?)request.SpecialConditions ?? DBNull.Value);

            await command.ExecuteNonQueryAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return (true, "Otel koşulları güncellendi.");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            return (false, $"Otel koşulları kaydedilemedi: {ex.Message}");
        }
    }

    private async Task<List<PartnerHotelPolicyOptionViewModel>> LoadHotelPolicyDictionaryAsync(SqlConnection connection, long hotelId, CancellationToken cancellationToken)
    {
        var rows = new List<PartnerHotelPolicyOptionViewModel>();
        if (!await TableExistsAsync(connection, "otel_kosul_sozlugu", cancellationToken) || !await TableExistsAsync(connection, "otel_kosul_secimleri", cancellationToken))
        {
            return rows;
        }

        var selected = new HashSet<long>();
        await using (var cmdSelected = new SqlCommand("SELECT kosul_id FROM otel_kosul_secimleri WHERE otel_id = @hotelId;", connection))
        {
            cmdSelected.Parameters.AddWithValue("@hotelId", hotelId);
            await using var r = await cmdSelected.ExecuteReaderAsync(cancellationToken);
            while (await r.ReadAsync(cancellationToken))
            {
                if (!r.IsDBNull(0)) selected.Add(r.GetInt64(0));
            }
        }

        const string sql = @"
            SELECT id, COALESCE(kategori, N'Genel') AS kategori, kosul_adi, aciklama, siralama
            FROM otel_kosul_sozlugu
            WHERE aktif_mi = 1
            ORDER BY kategori, siralama, kosul_adi;";

        await using var cmd = new SqlCommand(sql, connection);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var id = reader.IsDBNull(0) ? 0 : reader.GetInt64(0);
            if (id <= 0) continue;
            rows.Add(new PartnerHotelPolicyOptionViewModel
            {
                PolicyId = id,
                Category = reader.IsDBNull(1) ? "Genel" : reader.GetString(1),
                Name = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                Description = reader.IsDBNull(3) ? null : reader.GetString(3),
                Order = reader.IsDBNull(4) ? (short)100 : reader.GetInt16(4),
                IsSelected = selected.Contains(id)
            });
        }
        return rows;
    }

    private static async Task UpsertHotelPolicySelectionsAsync(SqlConnection connection, SqlTransaction transaction, long hotelId, List<long>? selectedPolicyIds, CancellationToken cancellationToken)
    {
        if (!await TableExistsAsync(connection, "otel_kosul_secimleri", cancellationToken))
        {
            return;
        }

        await using (var deleteCmd = new SqlCommand("DELETE FROM otel_kosul_secimleri WHERE otel_id = @hotelId;", connection, transaction))
        {
            deleteCmd.Parameters.AddWithValue("@hotelId", hotelId);
            await deleteCmd.ExecuteNonQueryAsync(cancellationToken);
        }

        foreach (var policyId in (selectedPolicyIds ?? new List<long>()).Distinct())
        {
            if (policyId <= 0) continue;
            await using var ins = new SqlCommand("INSERT INTO otel_kosul_secimleri (otel_id, kosul_id) VALUES (@hotelId, @policyId);", connection, transaction);
            ins.Parameters.AddWithValue("@hotelId", hotelId);
            ins.Parameters.AddWithValue("@policyId", policyId);
            await ins.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    public async Task<PartnerRoomFeaturesPageViewModel> GetRoomFeaturesAsync(long userId, long? hotelId = null, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var context = await BuildContextAsync(connection, userId, hotelId, "Oda Özellikleri", "Oda tiplerinde seçilebilir özellik listesini yönetin.", "rooms", cancellationToken);
        var model = new PartnerRoomFeaturesPageViewModel
        {
            Shell = context.Shell,
            SelectedHotelId = context.SelectedHotel.HotelId,
            AddForm = new PartnerRoomFeatureAddRequest { HotelId = context.SelectedHotel.HotelId }
        };

        const string sql = @"
            SELECT id, kategori, ozellik_adi, ozellik_ikon, siralama, aktif_mi
            FROM oda_ozellikleri
            ORDER BY kategori ASC, siralama ASC, ozellik_adi ASC;";

        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            model.Features.Add(new PartnerRoomFeatureRowViewModel
            {
                FeatureId = Convert.ToInt16(reader.GetValue(0), CultureInfo.InvariantCulture),
                Category = reader.IsDBNull(1) ? "Genel" : reader.GetString(1),
                Name = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                IconClass = reader.IsDBNull(3) ? null : reader.GetString(3),
                Order = reader.IsDBNull(4) ? (short)0 : Convert.ToInt16(reader.GetValue(4), CultureInfo.InvariantCulture),
                IsActive = !reader.IsDBNull(5) && reader.GetBoolean(5)
            });
        }

        return model;
    }

    public async Task<(bool Success, string Message)> AddRoomFeatureAsync(long userId, PartnerRoomFeatureAddRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return (false, "Özellik adı boş olamaz.");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await EnsureHotelAccessAsync(connection, userId, request.HotelId, cancellationToken);

        const string sql = @"
            INSERT INTO oda_ozellikleri (kategori, ozellik_adi, ozellik_ikon, siralama, aktif_mi)
            VALUES (@category, @name, @icon, @order, 1);";

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@category", string.IsNullOrWhiteSpace(request.Category) ? "Genel" : request.Category.Trim());
        command.Parameters.AddWithValue("@name", request.Name.Trim());
        command.Parameters.AddWithValue("@icon", string.IsNullOrWhiteSpace(request.IconClass) ? DBNull.Value : request.IconClass.Trim());
        command.Parameters.AddWithValue("@order", request.Order);
        await command.ExecuteNonQueryAsync(cancellationToken);

        return (true, "Oda özelliği eklendi.");
    }

    public async Task<(bool Success, string Message)> ToggleRoomFeatureAsync(long userId, PartnerRoomFeatureToggleRequest request, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await EnsureHotelAccessAsync(connection, userId, request.HotelId, cancellationToken);

        const string sql = "UPDATE oda_ozellikleri SET aktif_mi = @active WHERE id = @id;";
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@active", request.IsActive ? 1 : 0);
        command.Parameters.AddWithValue("@id", request.FeatureId);
        await command.ExecuteNonQueryAsync(cancellationToken);
        return (true, request.IsActive ? "Oda özelliği aktif edildi." : "Oda özelliği pasife alındı.");
    }

    private async Task<PartnerHotelPoliciesForm> LoadHotelPoliciesFormAsync(SqlConnection connection, long hotelId, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT sigara_politikasi, evcil_hayvan_politikasi, evcil_hayvan_ucreti, evcil_hayvan_depozitosu,
                   parti_etkinlik_izin, sessizlik_saatleri_baslangic, sessizlik_saatleri_bitis, minimum_yas_siniri,
                   sadece_yetiskinlere_mi, cocuk_kabul_yas_araligi, bebek_karyolasi_var_mi, bebek_karyolasi_ucreti,
                   ekstra_yatak_var_mi, ekstra_yatak_ucreti, maksimum_cocuk_sayisi,
                   on_odeme_gerekli_mi, on_odeme_orani, kalan_odeme_zamani, kredi_karti_ile_odeme_kabul, nakit_odeme_kabul, kabul_edilen_kartlar,
                   iptal_politikasi_ozet, detayli_iptal_kosullari, ucretsiz_iptal_suresi, gec_iptal_ceza_orani, no_show_ceza_orani,
                   hasar_depozitosu_tutari, hasar_depozitosu_aciklamasi, disaridan_yiyecek_icecek_serbest_mi, ziyaretci_kabul_edilir_mi,
                   ziyaretci_saati_baslangic, ziyaretci_saati_bitis, ozel_kosullar
            FROM otel_kosullari
            WHERE otel_id = @hotelId;";

        var form = new PartnerHotelPoliciesForm { HotelId = hotelId };
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@hotelId", hotelId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return form;
        }

        form.SmokingPolicy = reader.IsDBNull(0) ? null : reader.GetString(0);
        form.PetPolicy = reader.IsDBNull(1) ? null : reader.GetString(1);
        form.PetFee = reader.IsDBNull(2) ? null : reader.GetDecimal(2);
        form.PetDeposit = reader.IsDBNull(3) ? null : reader.GetDecimal(3);
        form.PartyAllowed = !reader.IsDBNull(4) && reader.GetBoolean(4);
        form.QuietHoursStart = reader.IsDBNull(5) ? null : reader.GetTimeSpan(5).ToString(@"hh\:mm", CultureInfo.InvariantCulture);
        form.QuietHoursEnd = reader.IsDBNull(6) ? null : reader.GetTimeSpan(6).ToString(@"hh\:mm", CultureInfo.InvariantCulture);
        form.MinimumAgeLimit = reader.IsDBNull(7) ? null : reader.GetByte(7);
        form.AdultsOnly = !reader.IsDBNull(8) && reader.GetBoolean(8);
        form.ChildAgeRange = reader.IsDBNull(9) ? null : reader.GetString(9);
        form.BabyCribAvailable = !reader.IsDBNull(10) && reader.GetBoolean(10);
        form.BabyCribFee = reader.IsDBNull(11) ? null : reader.GetDecimal(11);
        form.ExtraBedAvailable = !reader.IsDBNull(12) && reader.GetBoolean(12);
        form.ExtraBedFee = reader.IsDBNull(13) ? null : reader.GetDecimal(13);
        form.MaxChildren = reader.IsDBNull(14) ? null : reader.GetByte(14);
        form.PrepaymentRequired = reader.IsDBNull(15) ? form.PrepaymentRequired : reader.GetBoolean(15);
        form.PrepaymentPercent = reader.IsDBNull(16) ? form.PrepaymentPercent : reader.GetDecimal(16);
        form.RemainingPaymentTimeText = reader.IsDBNull(17) ? null : reader.GetString(17);
        form.CreditCardPaymentAccepted = reader.IsDBNull(18) ? form.CreditCardPaymentAccepted : reader.GetBoolean(18);
        form.CashPaymentAccepted = !reader.IsDBNull(19) && reader.GetBoolean(19);
        form.AcceptedCardsText = reader.IsDBNull(20) ? null : reader.GetString(20);
        form.CancellationSummary = reader.IsDBNull(21) ? null : reader.GetString(21);
        form.CancellationDetails = reader.IsDBNull(22) ? null : reader.GetString(22);
        form.FreeCancellationDays = reader.IsDBNull(23) ? null : reader.GetByte(23);
        form.LateCancellationPenaltyPercent = reader.IsDBNull(24) ? null : reader.GetDecimal(24);
        form.NoShowPenaltyPercent = reader.IsDBNull(25) ? null : reader.GetDecimal(25);
        form.DamageDepositAmount = reader.IsDBNull(26) ? null : reader.GetDecimal(26);
        form.DamageDepositDescription = reader.IsDBNull(27) ? null : reader.GetString(27);
        form.OutsideFoodAllowed = reader.IsDBNull(28) ? form.OutsideFoodAllowed : reader.GetBoolean(28);
        form.VisitorAllowed = !reader.IsDBNull(29) && reader.GetBoolean(29);
        form.VisitorHoursStart = reader.IsDBNull(30) ? null : reader.GetTimeSpan(30).ToString(@"hh\:mm", CultureInfo.InvariantCulture);
        form.VisitorHoursEnd = reader.IsDBNull(31) ? null : reader.GetTimeSpan(31).ToString(@"hh\:mm", CultureInfo.InvariantCulture);
        form.SpecialConditions = reader.IsDBNull(32) ? null : reader.GetString(32);
        return form;
    }

    private static TimeSpan? ParseTimeOrNull(string? raw)
    {
        var value = (raw ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(value)) return null;
        return TimeSpan.TryParse(value, CultureInfo.InvariantCulture, out var ts) ? ts : null;
    }

    public async Task<PartnerPhotosPageViewModel> GetPhotosAsync(long userId, long? hotelId = null, long? photoId = null, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var context = await BuildContextAsync(connection, userId, hotelId, "Fotograflar", "Galeri, kapak gorseli ve onayli medya akislarini yonetin.", "photos", cancellationToken);
        var model = new PartnerPhotosPageViewModel
        {
            Shell = context.Shell,
            SelectedHotelId = context.SelectedHotel.HotelId,
            UploadForm = new PartnerPhotoUploadRequest { HotelId = context.SelectedHotel.HotelId },
            EditForm = photoId.HasValue
                ? await LoadPhotoEditFormAsync(connection, context.SelectedHotel.HotelId, photoId.Value, cancellationToken)
                : new PartnerPhotoEditForm { HotelId = context.SelectedHotel.HotelId }
        };

        const string roomsSql = @"
            SELECT id, oda_adi, kapak_fotografi, toplam_oda_sayisi, aktif_mi
            FROM oda_tipleri
            WHERE otel_id = @hotelId
            ORDER BY aktif_mi DESC, id DESC;";
        await using (var cmd = new SqlCommand(roomsSql, connection))
        {
            cmd.Parameters.AddWithValue("@hotelId", context.SelectedHotel.HotelId);
            await using var r = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await r.ReadAsync(cancellationToken))
            {
                model.Rooms.Add(new PartnerPhotoRoomLinkViewModel
                {
                    RoomId = r.GetInt64(0),
                    RoomName = r.IsDBNull(1) ? "Oda" : r.GetString(1),
                    CoverPhotoUrl = r.IsDBNull(2) ? null : r.GetString(2),
                    TotalRooms = r.IsDBNull(3) ? (short)0 : Convert.ToInt16(r.GetValue(3), CultureInfo.InvariantCulture),
                    IsActive = !r.IsDBNull(4) && r.GetBoolean(4)
                });
            }
        }

        const string summarySql = @"
            SELECT COUNT(*) AS total_count,
                   SUM(CASE WHEN kapak_fotografi_mi = 1 THEN 1 ELSE 0 END) AS cover_count,
                   SUM(CASE WHEN onay_durumu = 'Onaylandı' THEN 1 ELSE 0 END) AS approved_count,
                   SUM(CASE WHEN one_cikan = 1 THEN 1 ELSE 0 END) AS featured_count
            FROM otel_gorselleri
            WHERE otel_id = @hotelId;";

        await using (var summaryCommand = new SqlCommand(summarySql, connection))
        {
            summaryCommand.Parameters.AddWithValue("@hotelId", context.SelectedHotel.HotelId);
            await using var reader = await summaryCommand.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                model.SummaryCards.Add(new PartnerStatCardViewModel { Label = "Toplam Gorsel", Value = SafeInt(reader, 0).ToString(), Description = "Otel galerisindeki tum medya", IconClass = "fa-images", ToneClass = "info" });
                model.SummaryCards.Add(new PartnerStatCardViewModel { Label = "Kapak", Value = SafeInt(reader, 1).ToString(), Description = "Aktif kapak fotograflari", IconClass = "fa-image-portrait", ToneClass = "success" });
                model.SummaryCards.Add(new PartnerStatCardViewModel { Label = "Onayli", Value = SafeInt(reader, 2).ToString(), Description = "Panelde yayina hazir medya", IconClass = "fa-circle-check", ToneClass = "warning" });
                model.SummaryCards.Add(new PartnerStatCardViewModel { Label = "One Cikan", Value = SafeInt(reader, 3).ToString(), Description = "Vurgulu slider gorselleri", IconClass = "fa-star", ToneClass = "danger" });
            }
        }

        const string photoSql = @"
            SELECT id, gorsel_url, COALESCE(baslik, 'Gorsel'), gorsel_turu, siralama, COALESCE(aciklama, ''), kapak_fotografi_mi, onay_durumu
            FROM otel_gorselleri
            WHERE otel_id = @hotelId
            ORDER BY kapak_fotografi_mi DESC, siralama ASC, id DESC;";

        await using var photoCommand = new SqlCommand(photoSql, connection);
        photoCommand.Parameters.AddWithValue("@hotelId", context.SelectedHotel.HotelId);
        await using var photoReader = await photoCommand.ExecuteReaderAsync(cancellationToken);
        while (await photoReader.ReadAsync(cancellationToken))
        {
            model.Photos.Add(new PartnerPhotoCardViewModel
            {
                PhotoId = photoReader.GetInt64(0),
                Url = photoReader.GetString(1),
                Title = photoReader.GetString(2),
                Type = photoReader.GetString(3),
                SortText = $"Sira {SafeShort(photoReader, 4)}",
                Description = photoReader.IsDBNull(5) ? null : photoReader.GetString(5),
                DisplayOrder = Convert.ToUInt16(photoReader.GetValue(4), CultureInfo.InvariantCulture),
                IsCover = SafeBool(photoReader, 6),
                IsApproved = string.Equals(photoReader.GetString(7), "Onaylandı", StringComparison.OrdinalIgnoreCase)
            });
        }

        return model;
    }
    public async Task<(bool Success, string Message)> UploadPhotosAsync(long userId, PartnerPhotoUploadRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Files is null || request.Files.Count == 0)
        {
            return (false, "Yuklemek icin en az bir gorsel secmelisiniz.");
        }

        if (request.Files.Count > 100)
        {
            return (false, "Tek seferde en fazla 100 gorsel yukleyebilirsiniz.");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        var hotel = await EnsureHotelAccessAsync(connection, userId, request.HotelId, cancellationToken);

        var targetDirectory = MediaStoragePaths.HotelImagesDirectory(_environment.WebRootPath, hotel.HotelId);
        Directory.CreateDirectory(targetDirectory);
        var savedPhysicalPaths = new List<string>();
        var displayOrder = request.DisplayOrder;

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            var shouldMakeCover = request.MakeCover;
            foreach (var file in request.Files.Where(static item => item.Length > 0))
            {
                var storedImage = await _imageStorageService.SaveAsWebpAsync(file, targetDirectory, $"otel-{hotel.HotelId}", cancellationToken);
                var fileName = storedImage.FileName;
                savedPhysicalPaths.Add(Path.Combine(targetDirectory, fileName));
                var relativePath = MediaStoragePaths.HotelImagesUrl(hotel.HotelId, fileName);
                const string insertSql = @"
                    INSERT INTO otel_gorselleri
                    (otel_id, gorsel_url, gorsel_turu, baslik, aciklama, kapak_fotografi_mi, one_cikan, siralama, boyut_kb, onay_durumu, yukleyen_kullanici_id)
                    VALUES
                    (@hotelId, @url, @photoType, @title, @description, @isCover, @featured, @sortOrder, @sizeKb, 'Onaylandı', @userId);";

                await using var insertCommand = new SqlCommand(insertSql, connection, (SqlTransaction)transaction);
                insertCommand.Parameters.AddWithValue("@hotelId", hotel.HotelId);
                insertCommand.Parameters.AddWithValue("@url", relativePath);
                insertCommand.Parameters.AddWithValue("@photoType", request.PhotoType);
                insertCommand.Parameters.AddWithValue("@title", string.IsNullOrWhiteSpace(request.Title) ? hotel.HotelName : request.Title.Trim());
                insertCommand.Parameters.AddWithValue("@description", string.IsNullOrWhiteSpace(request.Description) ? DBNull.Value : request.Description.Trim());
                insertCommand.Parameters.AddWithValue("@isCover", shouldMakeCover ? 1 : 0);
                insertCommand.Parameters.AddWithValue("@featured", shouldMakeCover ? 1 : 0);
                insertCommand.Parameters.AddWithValue("@sortOrder", displayOrder);
                insertCommand.Parameters.AddWithValue("@sizeKb", Math.Max(1, (int)Math.Round(storedImage.FileSizeBytes / 1024m)));
                insertCommand.Parameters.AddWithValue("@userId", userId);
                await insertCommand.ExecuteNonQueryAsync(cancellationToken);

                if (shouldMakeCover)
                {
                    await using var resetCoverCommand = new SqlCommand("UPDATE otel_gorselleri SET kapak_fotografi_mi = 0 WHERE otel_id = @hotelId AND gorsel_url <> @url;", connection, (SqlTransaction)transaction);
                    resetCoverCommand.Parameters.AddWithValue("@hotelId", hotel.HotelId);
                    resetCoverCommand.Parameters.AddWithValue("@url", relativePath);
                    await resetCoverCommand.ExecuteNonQueryAsync(cancellationToken);

                    await using var updateHotelCommand = new SqlCommand("UPDATE oteller SET kapak_fotografi = @coverUrl WHERE id = @hotelId;", connection, (SqlTransaction)transaction);
                    updateHotelCommand.Parameters.AddWithValue("@coverUrl", relativePath);
                    updateHotelCommand.Parameters.AddWithValue("@hotelId", hotel.HotelId);
                    await updateHotelCommand.ExecuteNonQueryAsync(cancellationToken);
                    shouldMakeCover = false;
                }

                displayOrder++;
            }

            await transaction.CommitAsync(cancellationToken);
            return (true, $"{savedPhysicalPaths.Count} fotograf galeriye eklendi.");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            foreach (var physicalPath in savedPhysicalPaths)
            {
                await _imageStorageService.DeleteAsync(physicalPath, cancellationToken);
            }
            return (false, $"Fotograf yukleme sirasinda hata olustu: {ex.Message}");
        }
    }

    public async Task<(bool Success, string Message)> SetCoverPhotoAsync(long userId, long hotelId, long photoId, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        var hotel = await EnsureHotelAccessAsync(connection, userId, hotelId, cancellationToken);

        const string selectSql = "SELECT TOP (1) gorsel_url FROM otel_gorselleri WHERE id = @photoId AND otel_id = @hotelId;";
        await using var selectCommand = new SqlCommand(selectSql, connection);
        selectCommand.Parameters.AddWithValue("@photoId", photoId);
        selectCommand.Parameters.AddWithValue("@hotelId", hotel.HotelId);
        var url = await selectCommand.ExecuteScalarAsync(cancellationToken) as string;
        if (string.IsNullOrWhiteSpace(url))
        {
            return (false, "Kapak yapilacak fotograf bulunamadi.");
        }

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        await using (var updatePhotos = new SqlCommand("UPDATE otel_gorselleri SET kapak_fotografi_mi = CASE WHEN id = @photoId THEN 1 ELSE 0 END WHERE otel_id = @hotelId;", connection, (SqlTransaction)transaction))
        {
            updatePhotos.Parameters.AddWithValue("@photoId", photoId);
            updatePhotos.Parameters.AddWithValue("@hotelId", hotel.HotelId);
            await updatePhotos.ExecuteNonQueryAsync(cancellationToken);
        }

        await using (var updateHotel = new SqlCommand("UPDATE oteller SET kapak_fotografi = @coverUrl WHERE id = @hotelId;", connection, (SqlTransaction)transaction))
        {
            updateHotel.Parameters.AddWithValue("@coverUrl", url);
            updateHotel.Parameters.AddWithValue("@hotelId", hotel.HotelId);
            await updateHotel.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
        return (true, "Kapak fotografi guncellendi.");
    }

    public async Task<(bool Success, string Message)> UpdatePhotoAsync(long userId, PartnerPhotoEditForm request, CancellationToken cancellationToken = default)
    {
        if (!request.PhotoId.HasValue)
        {
            return (false, "Duzenlenecek fotograf secilmedi.");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        var hotel = await EnsureHotelAccessAsync(connection, userId, request.HotelId, cancellationToken);

        const string sql = @"
            UPDATE otel_gorselleri
            SET gorsel_turu = @photoType,
                baslik = @title,
                aciklama = @description,
                siralama = @displayOrder,
                one_cikan = @featured
            WHERE id = @photoId AND otel_id = @hotelId;";

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@photoId", request.PhotoId.Value);
        command.Parameters.AddWithValue("@hotelId", hotel.HotelId);
        command.Parameters.AddWithValue("@photoType", request.PhotoType);
        command.Parameters.AddWithValue("@title", string.IsNullOrWhiteSpace(request.Title) ? hotel.HotelName : request.Title.Trim());
        command.Parameters.AddWithValue("@description", string.IsNullOrWhiteSpace(request.Description) ? DBNull.Value : request.Description.Trim());
        command.Parameters.AddWithValue("@displayOrder", request.DisplayOrder);
        command.Parameters.AddWithValue("@featured", request.MarkAsFeatured ? 1 : 0);
        var affectedRows = await command.ExecuteNonQueryAsync(cancellationToken);
        return affectedRows > 0
            ? (true, "Fotograf bilgileri guncellendi.")
            : (false, "Guncellenecek fotograf bulunamadi.");
    }

    public async Task<(bool Success, string Message)> DeletePhotoAsync(long userId, long hotelId, long photoId, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        var hotel = await EnsureHotelAccessAsync(connection, userId, hotelId, cancellationToken);

        const string selectSql = "SELECT TOP (1) gorsel_url, kapak_fotografi_mi FROM otel_gorselleri WHERE id = @photoId AND otel_id = @hotelId;";
        string? relativePath = null;
        var wasCover = false;
        await using (var selectCommand = new SqlCommand(selectSql, connection))
        {
            selectCommand.Parameters.AddWithValue("@photoId", photoId);
            selectCommand.Parameters.AddWithValue("@hotelId", hotel.HotelId);
            await using var reader = await selectCommand.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                relativePath = reader.GetString(0);
                wasCover = SafeBool(reader, 1);
            }
        }

        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return (false, "Silinecek fotograf bulunamadi.");
        }

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        await using (var deleteCommand = new SqlCommand("DELETE FROM otel_gorselleri WHERE id = @photoId AND otel_id = @hotelId;", connection, (SqlTransaction)transaction))
        {
            deleteCommand.Parameters.AddWithValue("@photoId", photoId);
            deleteCommand.Parameters.AddWithValue("@hotelId", hotel.HotelId);
            await deleteCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        if (wasCover)
        {
            await using var clearHotel = new SqlCommand("UPDATE oteller SET kapak_fotografi = NULL WHERE id = @hotelId;", connection, (SqlTransaction)transaction);
            clearHotel.Parameters.AddWithValue("@hotelId", hotel.HotelId);
            await clearHotel.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);

        var normalizedPath = relativePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var physicalPath = Path.Combine(_environment.WebRootPath, normalizedPath);
        await _imageStorageService.DeleteAsync(physicalPath, cancellationToken);

        return (true, "Fotograf galeriden kaldirildi.");
    }

    public async Task<(bool Success, string Message)> BulkDeletePhotosAsync(long userId, PartnerPhotoBulkDeleteRequest request, CancellationToken cancellationToken = default)
    {
        var selectedIds = request.PhotoIds
            .Where(static item => item > 0)
            .Distinct()
            .ToList();

        if (request.HotelId <= 0 || selectedIds.Count == 0)
        {
            return (false, "Toplu silme icin en az bir gorsel secmelisiniz.");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        var hotel = await EnsureHotelAccessAsync(connection, userId, request.HotelId, cancellationToken);

        var sql = $@"
            SELECT id, gorsel_url, kapak_fotografi_mi
            FROM otel_gorselleri
            WHERE otel_id = @hotelId
              AND id IN ({string.Join(",", selectedIds)});";

        var physicalPaths = new List<string>();
        var anyCover = false;
        var actualIds = new List<long>();

        await using (var selectCommand = new SqlCommand(sql, connection))
        {
            selectCommand.Parameters.AddWithValue("@hotelId", hotel.HotelId);
            await using var reader = await selectCommand.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                actualIds.Add(reader.GetInt64(0));
                anyCover |= SafeBool(reader, 2);
                var normalizedPath = reader.GetString(1).TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
                physicalPaths.Add(Path.Combine(_environment.WebRootPath, normalizedPath));
            }
        }

        if (actualIds.Count == 0)
        {
            return (false, "Secilen gorseller bulunamadi.");
        }

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            var deleteSql = $@"DELETE FROM otel_gorselleri WHERE otel_id = @hotelId AND id IN ({string.Join(",", actualIds)});";
            await using (var deleteCommand = new SqlCommand(deleteSql, connection, (SqlTransaction)transaction))
            {
                deleteCommand.Parameters.AddWithValue("@hotelId", hotel.HotelId);
                await deleteCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            if (anyCover)
            {
                const string nextCoverSql = @"
                    SELECT TOP (1) gorsel_url
                    FROM otel_gorselleri
                    WHERE otel_id = @hotelId
                    ORDER BY one_cikan DESC, siralama ASC, id ASC;";

                string? nextCoverUrl = null;
                await using (var coverCommand = new SqlCommand(nextCoverSql, connection, (SqlTransaction)transaction))
                {
                    coverCommand.Parameters.AddWithValue("@hotelId", hotel.HotelId);
                    nextCoverUrl = await coverCommand.ExecuteScalarAsync(cancellationToken) as string;
                }

                await using (var resetCovers = new SqlCommand("UPDATE otel_gorselleri SET kapak_fotografi_mi = 0 WHERE otel_id = @hotelId;", connection, (SqlTransaction)transaction))
                {
                    resetCovers.Parameters.AddWithValue("@hotelId", hotel.HotelId);
                    await resetCovers.ExecuteNonQueryAsync(cancellationToken);
                }

                if (!string.IsNullOrWhiteSpace(nextCoverUrl))
                {
                    await using var promoteCover = new SqlCommand("UPDATE otel_gorselleri SET kapak_fotografi_mi = 1 WHERE otel_id = @hotelId AND gorsel_url = @url;", connection, (SqlTransaction)transaction);
                    promoteCover.Parameters.AddWithValue("@hotelId", hotel.HotelId);
                    promoteCover.Parameters.AddWithValue("@url", nextCoverUrl);
                    await promoteCover.ExecuteNonQueryAsync(cancellationToken);
                }

                await using var updateHotel = new SqlCommand("UPDATE oteller SET kapak_fotografi = @coverUrl WHERE id = @hotelId;", connection, (SqlTransaction)transaction);
                updateHotel.Parameters.AddWithValue("@hotelId", hotel.HotelId);
                updateHotel.Parameters.AddWithValue("@coverUrl", string.IsNullOrWhiteSpace(nextCoverUrl) ? DBNull.Value : nextCoverUrl);
                await updateHotel.ExecuteNonQueryAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);

            foreach (var physicalPath in physicalPaths)
            {
                await _imageStorageService.DeleteAsync(physicalPath, cancellationToken);
            }

            return (true, $"{actualIds.Count} gorsel galeriden kaldirildi.");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            return (false, $"Toplu gorsel silme tamamlanamadi: {ex.Message}");
        }
    }

    public async Task<PartnerPerformancePageViewModel> GetPerformanceAsync(long userId, long? hotelId = null, CancellationToken cancellationToken = default)
    {
        var dashboard = await GetDashboardAsync(userId, hotelId, cancellationToken: cancellationToken);
        dashboard.Shell.PanelTitle = "Performans";
        dashboard.Shell.PanelSubtitle = "Gelir, doluluk, yorum ve rakip verilerini secili tesis bazinda izleyin.";
        dashboard.Shell.ActiveSectionKey = "performance";

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        var hotel = await EnsureHotelAccessAsync(connection, userId, dashboard.Shell.SelectedHotelId ?? 0, cancellationToken);

        return new PartnerPerformancePageViewModel
        {
            Shell = dashboard.Shell,
            SummaryCards = dashboard.SummaryCards,
            RevenueTrend = dashboard.RevenueTrend,
            InfoNote = "Bu ekran secili otele ait rezervasyon, yorum, istatistik ve rakip analiz verilerini birlestirir. Rezervasyon verisi olmayan tesislerde kartlar sifira dusebilir.",
            Competitors = await LoadCompetitorsAsync(connection, hotel.HotelId, cancellationToken),
            CompetitorForm = new PartnerCompetitorUpsertRequest
            {
                HotelId = hotel.HotelId,
                AnalysisDate = DateTime.Today
            }
        };
    }

    public async Task<(bool Success, string Message)> SaveCompetitorAsync(long userId, PartnerCompetitorUpsertRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.CompetitorHotelName))
        {
            return (false, "Rakip otel adi zorunludur.");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        var hotel = await EnsureHotelAccessAsync(connection, userId, request.HotelId, cancellationToken);

        if (request.CompetitorId.HasValue)
        {
            const string updateSql = @"
                UPDATE otel_rakip_analizi
                SET rakip_otel_adi = @hotelName,
                    rakip_sehir = @city,
                    rakip_ilce = @district,
                    analiz_tarihi = @analysisDate,
                    ortalama_gecelik_fiyat = @averagePrice,
                    tahmini_doluluk_orani = @occupancyRate,
                    kaynak_url = @sourceUrl,
                    notlar = @notes
                WHERE id = @competitorId AND otel_id = @hotelId;";

            await using var updateCommand = new SqlCommand(updateSql, connection);
            BindCompetitorCommand(updateCommand, request, hotel.HotelId);
            updateCommand.Parameters.AddWithValue("@competitorId", request.CompetitorId.Value);
            var affectedRows = await updateCommand.ExecuteNonQueryAsync(cancellationToken);
            return affectedRows > 0
                ? (true, "Rakip kaydi guncellendi.")
                : (false, "Guncellenecek rakip kaydi bulunamadi.");
        }

        const string insertSql = @"
            INSERT INTO otel_rakip_analizi
            (otel_id, rakip_otel_adi, rakip_sehir, rakip_ilce, analiz_tarihi, ortalama_gecelik_fiyat, tahmini_doluluk_orani, kaynak_url, notlar)
            VALUES
            (@hotelId, @hotelName, @city, @district, @analysisDate, @averagePrice, @occupancyRate, @sourceUrl, @notes);";

        await using var insertCommand = new SqlCommand(insertSql, connection);
        BindCompetitorCommand(insertCommand, request, hotel.HotelId);
        await insertCommand.ExecuteNonQueryAsync(cancellationToken);
        return (true, "Rakip analizi kaydi eklendi.");
    }

    public async Task<string> ExportPerformanceCsvAsync(long userId, long? hotelId = null, CancellationToken cancellationToken = default)
    {
        var model = await GetPerformanceAsync(userId, hotelId, cancellationToken);
        var lines = new List<string>
        {
            "Bolum,Ad,Deger,Aciklama"
        };

        foreach (var card in model.SummaryCards)
        {
            lines.Add($"Ozet,{EscapeCsv(card.Label)},{EscapeCsv(card.Value)},{EscapeCsv(card.Description)}");
        }

        foreach (var point in model.RevenueTrend)
        {
            lines.Add($"Trend,{EscapeCsv(point.Label)},{point.RevenueAmount.ToString(CultureInfo.InvariantCulture)},{point.ReservationCount} rezervasyon");
        }

        foreach (var competitor in model.Competitors)
        {
            lines.Add($"Rakip,{EscapeCsv(competitor.HotelName)},{EscapeCsv(competitor.AveragePriceText)},{EscapeCsv(competitor.LocationText)}");
        }

        return string.Join(Environment.NewLine, lines);
    }

    public async Task<PartnerReviewsPageViewModel> GetReviewsAsync(long userId, long? hotelId = null, string? status = null, string? replyState = null, int page = 1, int pageSize = 7, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        var context = await BuildContextAsync(connection, userId, hotelId, "Degerlendirmeler", "Misafir yorumlarini, yanitsiz kayitlari ve hizmet iyilestirme sinyallerini yonetin.", "reviews", cancellationToken);
        var normalizedStatus = NormalizeTurkishText(status);
        var normalizedReplyState = (replyState ?? string.Empty).Trim().ToLowerInvariant();
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 5, 25);

        var model = new PartnerReviewsPageViewModel
        {
            Shell = context.Shell,
            ReplyForm = new PartnerReviewReplyRequest { HotelId = context.SelectedHotel.HotelId },
            StatusFilter = normalizedStatus,
            ReplyStateFilter = normalizedReplyState,
            Page = page,
            PageSize = pageSize
        };
        model.TotalCount = await CountReviewsAsync(connection, context.SelectedHotel.HotelId, normalizedStatus, normalizedReplyState, cancellationToken);
        model.Reviews = await LoadReviewsAsync(connection, context.SelectedHotel.HotelId, normalizedStatus, normalizedReplyState, page, pageSize, cancellationToken);
        const string summarySql = @"
            SELECT COUNT(*) AS total_count,
                   SUM(CASE WHEN COALESCE(otel_yaniti,'') = '' THEN 1 ELSE 0 END) AS unanswered_count,
                   SUM(CASE WHEN onay_durumu = 'Beklemede' THEN 1 ELSE 0 END) AS pending_count,
                   COALESCE(AVG(genel_puan), 0) AS average_score
            FROM yorumlar
            WHERE otel_id = @hotelId;";

        await using (var summaryCommand = new SqlCommand(summarySql, connection))
        {
            summaryCommand.Parameters.AddWithValue("@hotelId", context.SelectedHotel.HotelId);
            await using var reader = await summaryCommand.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                model.SummaryCards.Add(new PartnerStatCardViewModel { Label = "Toplam Yorum", Value = SafeInt(reader, 0).ToString(), Description = "Tum misafir geri bildirimleri", IconClass = "fa-comments", ToneClass = "info" });
                model.SummaryCards.Add(new PartnerStatCardViewModel { Label = "Yanitsiz", Value = SafeInt(reader, 1).ToString(), Description = "Cevap bekleyen yorumlar", IconClass = "fa-reply", ToneClass = "warning" });
                model.SummaryCards.Add(new PartnerStatCardViewModel { Label = "Beklemede", Value = SafeInt(reader, 2).ToString(), Description = "Inceleme veya moderasyon bekleyen", IconClass = "fa-hourglass-half", ToneClass = "danger" });
                model.SummaryCards.Add(new PartnerStatCardViewModel { Label = "Ortalama Puan", Value = SafeDecimal(reader, 3).ToString("0.0", CultureInfo.InvariantCulture), Description = "Genel misafir memnuniyeti", IconClass = "fa-star", ToneClass = "success" });
            }
        }

        return model;
    }

    public async Task<(bool Success, string Message)> ReplyToReviewAsync(long userId, PartnerReviewReplyRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.ResponseText))
        {
            return (false, "Yorum yaniti bos birakilamaz.");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await EnsureHotelAccessAsync(connection, userId, request.HotelId, cancellationToken);

        const string sql = @"
            UPDATE yorumlar
            SET otel_yaniti = @responseText,
                otel_yaniti_tarihi = GETDATE(),
                yanitlayan_kullanici_id = @userId
            WHERE id = @reviewId AND otel_id = @hotelId;";

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@responseText", request.ResponseText.Trim());
        command.Parameters.AddWithValue("@userId", userId);
        command.Parameters.AddWithValue("@reviewId", request.ReviewId);
        command.Parameters.AddWithValue("@hotelId", request.HotelId);
        await command.ExecuteNonQueryAsync(cancellationToken);
        return (true, "Yorum yaniti kaydedildi.");
    }

    public async Task<(bool Success, string Message)> ReportReviewAsync(long userId, PartnerReviewReportRequest request, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await EnsureHotelAccessAsync(connection, userId, request.HotelId, cancellationToken);

        const string sql = @"
            UPDATE yorumlar
            SET onay_durumu = 'Beklemede'
            WHERE id = @reviewId AND otel_id = @hotelId;";

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@reviewId", request.ReviewId);
        command.Parameters.AddWithValue("@hotelId", request.HotelId);
        var affectedRows = await command.ExecuteNonQueryAsync(cancellationToken);

        return affectedRows > 0
            ? (true, "Yorum raporlanmak uzere beklemeye alindi.")
            : (false, "Yorum bulunamadi.");
    }

    public async Task<(bool Success, string Message)> RequestReviewTakedownAsync(long userId, long hotelId, long reviewId, string? reason, CancellationToken cancellationToken = default)
    {
        if (hotelId <= 0 || reviewId <= 0)
        {
            return (false, "Geçersiz istek.");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await EnsureHotelAccessAsync(connection, userId, hotelId, cancellationToken);

        if (!await TableExistsAsync(connection, "yorum_kaldirma_talepleri", cancellationToken))
        {
            return (false, "Kaldırma talebi modülü için tablo bulunamadı. Migration çalıştırın.");
        }

        const string sql = @"
DECLARE @otelId bigint = (SELECT TOP (1) otel_id FROM yorumlar WHERE id = @reviewId);
IF (@otelId IS NULL OR @otelId <> @hotelId)
BEGIN
    SELECT 0;
    RETURN;
END

IF EXISTS (SELECT 1 FROM dbo.yorum_kaldirma_talepleri WHERE yorum_id = @reviewId AND durum = N'Beklemede')
BEGIN
    UPDATE dbo.yorum_kaldirma_talepleri
    SET sebep = COALESCE(NULLIF(@reason, ''), sebep),
        guncellenme_tarihi = SYSUTCDATETIME()
    WHERE yorum_id = @reviewId AND durum = N'Beklemede';
    SELECT 1;
END
ELSE
BEGIN
    INSERT INTO dbo.yorum_kaldirma_talepleri(yorum_id, otel_id, partner_kullanici_id, sebep, durum)
    VALUES(@reviewId, @hotelId, @userId, NULLIF(@reason, ''), N'Beklemede');
    SELECT 1;
END";

        await using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@reviewId", reviewId);
        cmd.Parameters.AddWithValue("@hotelId", hotelId);
        cmd.Parameters.AddWithValue("@userId", userId);
        cmd.Parameters.AddWithValue("@reason", (object?)reason?.Trim() ?? DBNull.Value);
        var result = await cmd.ExecuteScalarAsync(cancellationToken);
        var ok = result is not null && result is not DBNull && Convert.ToInt32(result, CultureInfo.InvariantCulture) == 1;
        return ok ? (true, "Kaldırma talebi admin ekibine iletildi.") : (false, "Yorum bulunamadı.");
    }

    public async Task<PartnerFinancePageViewModel> GetFinanceAsync(long userId, long? hotelId = null, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        var context = await BuildContextAsync(connection, userId, hotelId, "Finans", "Tahsilat, komisyon ve otele net odenecek akislari secili tesis bazinda izleyin.", "finance", cancellationToken);
        var model = new PartnerFinancePageViewModel { Shell = context.Shell };

        const string summarySql = @"
            SELECT
                COALESCE(SUM(r.toplam_tutar), 0) AS gross_revenue,
                COALESCE(SUM(r.komisyon_tutari), 0) AS commission_total,
                COALESCE(SUM(r.platform_net_komisyon_tutari), 0) AS platform_net_commission_total,
                COALESCE(SUM(r.otele_odenecek_tutar), 0) AS payout_total,
                COALESCE(SUM(r.kdv_tutari), 0) AS vat_total,
                COALESCE(SUM(r.konaklama_vergisi_tutari), 0) AS accommodation_tax_total,
                SUM(CASE WHEN r.odeme_durumu IN ('Beklemede','Ön Ödeme Alındı') THEN 1 ELSE 0 END) AS pending_payments
            FROM rezervasyonlar r
            WHERE r.otel_id = @hotelId;";

        await using (var summaryCommand = new SqlCommand(summarySql, connection))
        {
            summaryCommand.Parameters.AddWithValue("@hotelId", context.SelectedHotel.HotelId);
            await using var reader = await summaryCommand.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                model.SummaryCards.Add(new PartnerStatCardViewModel { Label = "Brut Ciro", Value = FormatMoney(SafeDecimal(reader, 0)), Description = "Tum rezervasyon toplam tutari", IconClass = "fa-money-bill-wave", ToneClass = "info" });
                model.SummaryCards.Add(new PartnerStatCardViewModel { Label = "Platform Komisyonu", Value = FormatMoney(SafeDecimal(reader, 1)), Description = "Rezervasyon bazli kesilen komisyon", IconClass = "fa-percent", ToneClass = "warning" });
                model.SummaryCards.Add(new PartnerStatCardViewModel { Label = "Platform Net Komisyon", Value = FormatMoney(SafeDecimal(reader, 2)), Description = "Komisyon gelir vergisi sonrasi net platform tutari", IconClass = "fa-chart-pie", ToneClass = "danger" });
                model.SummaryCards.Add(new PartnerStatCardViewModel { Label = "Net Odeme", Value = FormatMoney(SafeDecimal(reader, 3)), Description = "Otele aktarilacak toplu bakiye", IconClass = "fa-building-columns", ToneClass = "success" });
                model.SummaryCards.Add(new PartnerStatCardViewModel { Label = "KDV", Value = FormatMoney(SafeDecimal(reader, 4)), Description = "Misafire yansitilan KDV toplami", IconClass = "fa-file-invoice-dollar", ToneClass = "warning" });
                model.SummaryCards.Add(new PartnerStatCardViewModel { Label = "Konaklama Vergisi", Value = FormatMoney(SafeDecimal(reader, 5)), Description = "Misafire yansitilan konaklama vergisi", IconClass = "fa-receipt", ToneClass = "info" });
                model.SummaryCards.Add(new PartnerStatCardViewModel { Label = "Bekleyen Odeme", Value = SafeInt(reader, 6).ToString(), Description = "Odeme durumu kapanmamis islem", IconClass = "fa-hourglass-half", ToneClass = "danger" });
            }
        }

        const string activeRuleSql = @"
            SELECT TOP (1)
                kv.baslangic_tarihi,
                kv.bitis_tarihi,
                kv.komisyon_orani,
                kv.komisyon_gelir_vergisi_orani,
                kv.kdv_orani,
                kv.konaklama_vergisi_orani
            FROM komisyon_vergiler kv
            WHERE kv.otel_id = @hotelId
              AND kv.aktif_mi = 1
            ORDER BY
                CASE
                    WHEN kv.baslangic_tarihi <= CAST(GETDATE() AS date)
                         AND (kv.bitis_tarihi IS NULL OR kv.bitis_tarihi >= CAST(GETDATE() AS date)) THEN 0
                    ELSE 1
                END,
                kv.baslangic_tarihi DESC,
                kv.id DESC;";

        await using (var ruleCommand = new SqlCommand(activeRuleSql, connection))
        {
            ruleCommand.Parameters.AddWithValue("@hotelId", context.SelectedHotel.HotelId);
            await using var reader = await ruleCommand.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                var startDate = reader.GetDateTime(0);
                var endDateText = reader.IsDBNull(1) ? "Acik uclu" : reader.GetDateTime(1).ToString("dd.MM.yyyy", CultureInfo.GetCultureInfo("tr-TR"));
                var commissionRate = SafeDecimal(reader, 2);
                var commissionIncomeTaxRate = SafeDecimal(reader, 3);
                var vatRate = SafeDecimal(reader, 4);
                var accommodationTaxRate = SafeDecimal(reader, 5);
                model.ActiveRuleDateText = $"{startDate:dd.MM.yyyy} - {endDateText}";
                model.ActiveRuleCommissionText = $"%{commissionRate:0.##} komisyon / %{commissionIncomeTaxRate:0.##} gelir vergisi";
                model.ActiveRuleTaxText = $"KDV %{vatRate:0.##} + Konaklama Vergisi %{accommodationTaxRate:0.##}";
            }
        }

        const string taxSummarySql = @"
            SELECT
                COALESCE(AVG(r.kdv_orani), 0) AS avg_vat_rate,
                COALESCE(SUM(r.kdv_tutari), 0) AS vat_amount,
                COALESCE(AVG(r.konaklama_vergisi_orani), 0) AS avg_accommodation_rate,
                COALESCE(SUM(r.konaklama_vergisi_tutari), 0) AS accommodation_amount,
                COALESCE(AVG(r.komisyon_gelir_vergisi_orani), 0) AS avg_commission_income_tax_rate,
                COALESCE(SUM(r.komisyon_gelir_vergisi_tutari), 0) AS commission_income_tax_amount
            FROM rezervasyonlar r
            WHERE r.otel_id = @hotelId;";

        await using (var taxCommand = new SqlCommand(taxSummarySql, connection))
        {
            taxCommand.Parameters.AddWithValue("@hotelId", context.SelectedHotel.HotelId);
            await using var reader = await taxCommand.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                model.TaxRows.Add(new PartnerFinanceTaxRowViewModel
                {
                    Label = "KDV",
                    RateText = $"%{SafeDecimal(reader, 0):0.##}",
                    AmountText = FormatMoney(SafeDecimal(reader, 1)),
                    Description = "Rezervasyonlarin net oda tutari uzerinden hesaplanan KDV toplami"
                });
                model.TaxRows.Add(new PartnerFinanceTaxRowViewModel
                {
                    Label = "Konaklama Vergisi",
                    RateText = $"%{SafeDecimal(reader, 2):0.##}",
                    AmountText = FormatMoney(SafeDecimal(reader, 3)),
                    Description = "Misafir toplamina eklenen konaklama vergisi toplami"
                });
                model.TaxRows.Add(new PartnerFinanceTaxRowViewModel
                {
                    Label = "Komisyon Gelir Vergisi",
                    RateText = $"%{SafeDecimal(reader, 4):0.##}",
                    AmountText = FormatMoney(SafeDecimal(reader, 5)),
                    Description = "Platform komisyonundan ayrilan gelir vergisi snapshot toplami"
                });
            }
        }

        const string transactionsSql = @"
            SELECT TOP (12)
                rezervasyon_no,
                COALESCE(odeme_tarihi, olusturulma_tarihi) AS hareket_tarihi,
                odeme_durumu,
                otele_odenecek_tutar,
                misafir_ad_soyad,
                toplam_tutar,
                komisyon_tutari,
                toplam_vergi_tutari
            FROM rezervasyonlar
            WHERE otel_id = @hotelId
            ORDER BY COALESCE(odeme_tarihi, olusturulma_tarihi) DESC, id DESC;";

        await using (var transactionsCommand = new SqlCommand(transactionsSql, connection))
        {
            transactionsCommand.Parameters.AddWithValue("@hotelId", context.SelectedHotel.HotelId);
            await using var reader = await transactionsCommand.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                model.Transactions.Add(new PartnerFinanceTransactionViewModel
                {
                    Label = reader.GetString(0),
                    DateText = FormatDateTime(reader.IsDBNull(1) ? null : reader.GetDateTime(1)),
                    StatusText = reader.IsDBNull(2) ? "Beklemede" : reader.GetString(2),
                    AmountText = FormatMoney(SafeDecimal(reader, 3)),
                    DetailText = $"{(reader.IsDBNull(4) ? "Misafir bilgisi yok" : reader.GetString(4))} | Brut {FormatMoney(SafeDecimal(reader, 5))} | Komisyon {FormatMoney(SafeDecimal(reader, 6))} | Vergi {FormatMoney(SafeDecimal(reader, 7))}"
                });
            }
        }

        model.PayoutNote = "Komisyon ve vergi hesaplari rezervasyon aninda aktif olan komisyon_vergiler kuralindan snapshot olarak alinir. Sonradan oran degisse bile eski rezervasyon finans kayitlari korunur.";
        return model;
    }

    public async Task<(bool Success, string Message)> SaveBankInfoAsync(long userId, PartnerBankInfoForm request, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await EnsureHotelAccessAsync(connection, userId, request.HotelId, cancellationToken);

        if (string.IsNullOrWhiteSpace(request.BankName) || string.IsNullOrWhiteSpace(request.Iban) || string.IsNullOrWhiteSpace(request.AccountHolderName))
        {
            return (false, "Banka, IBAN ve hesap sahibi alanlari zorunludur.");
        }

        const string sql = @"
            IF EXISTS (SELECT 1 FROM otel_odeme_bilgileri WHERE otel_id = @hotelId)
            BEGIN
                UPDATE otel_odeme_bilgileri
                SET banka_adi = @bankName,
                    sube_adi = @branchName,
                    iban = @iban,
                    hesap_sahibi = @accountHolder,
                    para_birimi = @currency,
                    guncellenme_tarihi = GETDATE()
                WHERE otel_id = @hotelId;
            END
            ELSE
            BEGIN
                INSERT INTO otel_odeme_bilgileri
                (otel_id, banka_adi, sube_adi, iban, hesap_sahibi, para_birimi, guncellenme_tarihi)
                VALUES
                (@hotelId, @bankName, @branchName, @iban, @accountHolder, @currency, GETDATE());
            END;";

        try
        {
            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@hotelId", request.HotelId);
            command.Parameters.AddWithValue("@bankName", request.BankName.Trim());
            command.Parameters.AddWithValue("@branchName", string.IsNullOrWhiteSpace(request.BranchName) ? DBNull.Value : request.BranchName.Trim());
            command.Parameters.AddWithValue("@iban", request.Iban.Trim());
            command.Parameters.AddWithValue("@accountHolder", request.AccountHolderName.Trim());
            command.Parameters.AddWithValue("@currency", string.IsNullOrWhiteSpace(request.Currency) ? "TRY" : request.Currency.Trim().ToUpperInvariant());
            await command.ExecuteNonQueryAsync(cancellationToken);
            return (true, "Banka bilgileri kaydedildi.");
        }
        catch
        {
            return (false, "Banka bilgileri kaydedilemedi. Tablo yapisini kontrol ediniz.");
        }
    }

    public async Task<string> ExportFinanceCsvAsync(long userId, long? hotelId = null, CancellationToken cancellationToken = default)
    {
        var model = await GetFinanceAsync(userId, hotelId, cancellationToken);
        var csv = new StringBuilder();
        csv.AppendLine("Bolum,Alan,Deger");

        foreach (var card in model.SummaryCards)
        {
            csv.Append(EscapeCsv("Ozet")).Append(',')
                .Append(EscapeCsv(card.Label)).Append(',')
                .Append(EscapeCsv(card.Value)).AppendLine();
        }

        foreach (var item in model.Transactions)
        {
            csv.Append(EscapeCsv("Hareket")).Append(',')
                .Append(EscapeCsv(item.Label)).Append(',')
                .Append(EscapeCsv($"{item.DateText} | {item.AmountText} | {item.StatusText}")).AppendLine();
        }

        foreach (var invoice in model.Invoices)
        {
            csv.Append(EscapeCsv("Fatura")).Append(',')
                .Append(EscapeCsv(invoice.InvoiceNo)).Append(',')
                .Append(EscapeCsv($"{invoice.InvoiceDateText} | {invoice.TotalText} | {invoice.InvoiceStatusText}")).AppendLine();
        }

        return csv.ToString();
    }

    public async Task<(byte[] Content, string ContentType, string FileName)?> DownloadInvoiceAsync(long userId, long hotelId, long invoiceId, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await EnsureHotelAccessAsync(connection, userId, hotelId, cancellationToken);

        const string sql = @"
            SELECT TOP (1) fatura_no, fatura_tarihi, fatura_turu, fatura_durumu, genel_toplam, para_birimi,
                   fatura_alici_unvan, fatura_alici_adres, fatura_kesen_unvan, fatura_kesen_vergi_no, fatura_pdf_yolu
            FROM faturalar
            WHERE id = @invoiceId AND otel_id = @hotelId;";

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@invoiceId", invoiceId);
        command.Parameters.AddWithValue("@hotelId", hotelId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        var invoiceNo = reader.GetString(0);
        var pdfPath = reader.IsDBNull(10) ? null : reader.GetString(10);
        if (!string.IsNullOrWhiteSpace(pdfPath))
        {
            var normalizedPath = pdfPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            var physicalPath = Path.Combine(_environment.WebRootPath, normalizedPath);
            if (File.Exists(physicalPath))
            {
                return (await File.ReadAllBytesAsync(physicalPath, cancellationToken), "application/pdf", $"{invoiceNo}.pdf");
            }
        }

        var invoiceHtml = BuildInvoiceHtml(
            invoiceNo,
            reader.IsDBNull(1) ? null : reader.GetDateTime(1),
            reader.IsDBNull(2) ? null : reader.GetString(2),
            reader.IsDBNull(3) ? null : reader.GetString(3),
            SafeDecimal(reader, 4),
            reader.IsDBNull(5) ? "TRY" : reader.GetString(5),
            reader.IsDBNull(6) ? "Misafir" : reader.GetString(6),
            reader.IsDBNull(7) ? "-" : reader.GetString(7),
            reader.IsDBNull(8) ? "Otelturizm" : reader.GetString(8),
            reader.IsDBNull(9) ? "-" : reader.GetString(9));

        return (Encoding.UTF8.GetBytes(invoiceHtml), "text/html; charset=utf-8", $"{invoiceNo}.html");
    }

    public async Task<PartnerReservationGuestInvoicesPageViewModel> GetGuestInvoicesAsync(long userId, long? hotelId = null, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        var context = await BuildContextAsync(connection, userId, hotelId, "Misafir Faturaları", "Konaklaması tamamlanan rezervasyonların fatura yükleme takibini yapın.", "finance", cancellationToken);

        var model = new PartnerReservationGuestInvoicesPageViewModel { Shell = context.Shell };

        if (!await TableExistsAsync(connection, "rezervasyon_faturalari", cancellationToken))
        {
            model.Rows.Add(new PartnerReservationGuestInvoiceRowViewModel
            {
                ReservationNo = "-",
                GuestName = "Tablo eksik",
                StayText = "Migration uygulanmalı: rezervasyon_faturalari",
                TotalText = "-",
                HasInvoice = false
            });
            return model;
        }

        const string sql = @"
            SELECT TOP (120)
                r.id,
                COALESCE(NULLIF(r.rezervasyon_no, ''), CAST(r.id AS nvarchar(30))) AS rezervasyon_no,
                COALESCE(NULLIF(r.misafir_ad_soyad, ''), 'Misafir') AS misafir_ad_soyad,
                r.giris_tarihi,
                r.cikis_tarihi,
                COALESCE(r.toplam_tutar, 0) AS toplam_tutar,
                rf.guvenli_dosya_id,
                rf.olusturulma_tarihi
            FROM dbo.rezervasyonlar r
            LEFT JOIN dbo.rezervasyon_faturalari rf ON rf.rezervasyon_id = r.id
            WHERE r.otel_id = @hotelId
              AND COALESCE(r.durum, '') = N'Tamamlandı'
            ORDER BY CASE WHEN rf.id IS NULL THEN 0 ELSE 1 END ASC, r.cikis_tarihi DESC, r.id DESC;";

        await using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@hotelId", context.SelectedHotel.HotelId);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            long? fileId = reader.IsDBNull(6) ? null : Convert.ToInt64(reader.GetValue(6), CultureInfo.InvariantCulture);
            string? downloadUrl = null;
            if (fileId.HasValue && fileId.Value > 0)
            {
                downloadUrl = await _secureFileService.CreateAccessUrlAsync(fileId.Value, userId, "partner", cancellationToken);
            }

            model.Rows.Add(new PartnerReservationGuestInvoiceRowViewModel
            {
                ReservationId = reader.GetInt64(0),
                ReservationNo = reader.GetString(1),
                GuestName = reader.GetString(2),
                StayText = $"{reader.GetDateTime(3):dd.MM.yyyy} - {reader.GetDateTime(4):dd.MM.yyyy}",
                TotalText = FormatMoney(SafeDecimal(reader, 5)),
                HasInvoice = fileId.HasValue && fileId.Value > 0,
                SecureFileId = fileId,
                DownloadUrl = downloadUrl,
                UploadedAtText = reader.IsDBNull(7) ? null : reader.GetDateTime(7).ToString("dd.MM.yyyy HH:mm", CultureInfo.GetCultureInfo("tr-TR"))
            });
        }

        return model;
    }

    public async Task<(bool Success, string Message)> SaveGuestInvoiceAsync(long userId, long hotelId, long reservationId, long secureFileId, string? fileName, string? mimeType, CancellationToken cancellationToken = default)
    {
        if (hotelId <= 0 || reservationId <= 0 || secureFileId <= 0)
        {
            return (false, "Geçersiz istek.");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await EnsureHotelAccessAsync(connection, userId, hotelId, cancellationToken);

        if (!await TableExistsAsync(connection, "rezervasyon_faturalari", cancellationToken))
        {
            return (false, "Fatura tablosu bulunamadı. Migration uygulanmalı.");
        }

        const string ensureCompletedSql = @"
            SELECT COUNT(*)
            FROM dbo.rezervasyonlar
            WHERE id = @reservationId
              AND otel_id = @hotelId
              AND COALESCE(durum, '') = N'Tamamlandı';";

        await using (var ensureCmd = new SqlCommand(ensureCompletedSql, connection))
        {
            ensureCmd.Parameters.AddWithValue("@reservationId", reservationId);
            ensureCmd.Parameters.AddWithValue("@hotelId", hotelId);
            var ok = Convert.ToInt32(await ensureCmd.ExecuteScalarAsync(cancellationToken) ?? 0, CultureInfo.InvariantCulture) > 0;
            if (!ok)
            {
                return (false, "Sadece tamamlanan rezervasyonlar için fatura yükleyebilirsiniz.");
            }
        }

        const string upsertSql = @"
            IF EXISTS (SELECT 1 FROM dbo.rezervasyon_faturalari WHERE rezervasyon_id = @reservationId)
            BEGIN
                UPDATE dbo.rezervasyon_faturalari
                SET guvenli_dosya_id = @fileId,
                    dosya_adi = @fileName,
                    mime_tipi = @mimeType,
                    yukleyen_kullanici_id = @userId,
                    olusturulma_tarihi = SYSUTCDATETIME()
                WHERE rezervasyon_id = @reservationId;
            END
            ELSE
            BEGIN
                INSERT INTO dbo.rezervasyon_faturalari
                (rezervasyon_id, otel_id, yukleyen_kullanici_id, guvenli_dosya_id, dosya_adi, mime_tipi, olusturulma_tarihi)
                VALUES
                (@reservationId, @hotelId, @userId, @fileId, @fileName, @mimeType, SYSUTCDATETIME());
            END";

        await using var cmd = new SqlCommand(upsertSql, connection);
        cmd.Parameters.AddWithValue("@reservationId", reservationId);
        cmd.Parameters.AddWithValue("@hotelId", hotelId);
        cmd.Parameters.AddWithValue("@userId", userId);
        cmd.Parameters.AddWithValue("@fileId", secureFileId);
        cmd.Parameters.AddWithValue("@fileName", string.IsNullOrWhiteSpace(fileName) ? DBNull.Value : fileName.Trim());
        cmd.Parameters.AddWithValue("@mimeType", string.IsNullOrWhiteSpace(mimeType) ? DBNull.Value : mimeType.Trim());
        await cmd.ExecuteNonQueryAsync(cancellationToken);
        return (true, "Fatura yüklendi ve rezervasyona işlendi.");
    }

    public async Task<PartnerApplicationPageViewModel> GetApplicationAsync(long userId, long? hotelId = null, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        var context = await BuildContextAsync(connection, userId, hotelId, "Basvuru ve Evraklar", "Partner onay surecini, evraklarini ve yayin durumunu bu alandan yonetin.", "preferences", cancellationToken);

        // Not: Tek sorguda oteller'e LEFT JOIN + ORDER BY, büyük veride tarama/sıralama yapıp timeout'a neden olabiliyor.
        // Partner/user verisini küçük bir sorguda, seçili otel bilgisini ayrı hedefli sorguda alıyoruz.
        const string partnerSql = @"
            SELECT TOP (1)
                p.id,
                p.firma_unvani,
                p.firma_turu,
                p.yetkili_ad_soyad,
                COALESCE(p.yetkili_gorev, '') AS yetkili_gorev,
                p.yetkili_eposta,
                p.yetkili_telefon,
                p.vergi_dairesi,
                p.vergi_numarasi,
                p.yetkili_tc_no,
                p.fatura_adresi,
                p.fatura_il,
                p.fatura_ilce,
                p.banka_adi,
                COALESCE(p.banka_subesi, '') AS banka_subesi,
                p.iban,
                COALESCE(p.web_sitesi, '') AS web_sitesi,
                COALESCE(p.aciklama, '') AS aciklama,
                p.onay_durumu,
                p.olusturulma_tarihi,
                p.onay_tarihi,
                COALESCE(p.red_nedeni, '') AS red_nedeni,
                p.otel_tipi_id,
                u.email_dogrulama_tarihi
            FROM partner_detaylari p
            INNER JOIN users u ON u.id = p.kullanici_id
            WHERE p.kullanici_id = @userId;";

        const string hotelSql = @"
            SELECT TOP (1)
                o.otel_adi,
                COALESCE(o.mahalle, '') AS mahalle,
                o.otel_tipi_id,
                COALESCE(ht.tip_adi, o.otel_turu, N'Otel') AS otel_tipi
            FROM oteller o
            LEFT JOIN otel_tipleri ht ON ht.id = o.otel_tipi_id
            WHERE o.id = @hotelId;";

        var model = new PartnerApplicationPageViewModel { Shell = context.Shell };
        model.HotelTypes = await LoadHotelTypeOptionsAsync(connection, cancellationToken);
        long partnerId;
        string companyName;
        string companyType;
        string contactName;
        string contactTitle;
        string email;
        string phone;
        string taxOffice;
        string taxNumber;
        string contactTcNo;
        string address;
        string city;
        string district;
        string bankName;
        string bankBranch;
        string iban;
        string website;
        string description;
        string statusText;
        DateTime registrationDate;
        DateTime? approvalDate;
        string rejectionReason;
        int? partnerHotelTypeId;
        bool emailVerified;

        await using (var command = new SqlCommand(partnerSql, connection))
        {
            command.Parameters.AddWithValue("@userId", userId);
            command.CommandTimeout = 60;
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                throw new InvalidOperationException("Partner basvuru kaydi bulunamadi.");
            }

            partnerId = reader.GetInt64(reader.GetOrdinal("id"));
            companyName = reader.GetString(reader.GetOrdinal("firma_unvani"));
            companyType = reader.GetString(reader.GetOrdinal("firma_turu"));
            contactName = reader.GetString(reader.GetOrdinal("yetkili_ad_soyad"));
            contactTitle = reader.GetString(reader.GetOrdinal("yetkili_gorev"));
            email = reader.GetString(reader.GetOrdinal("yetkili_eposta"));
            phone = reader.GetString(reader.GetOrdinal("yetkili_telefon"));
            taxOffice = reader.GetString(reader.GetOrdinal("vergi_dairesi"));
            taxNumber = reader.GetString(reader.GetOrdinal("vergi_numarasi"));
            contactTcNo = reader.GetString(reader.GetOrdinal("yetkili_tc_no"));
            address = reader.GetString(reader.GetOrdinal("fatura_adresi"));
            city = reader.GetString(reader.GetOrdinal("fatura_il"));
            district = reader.GetString(reader.GetOrdinal("fatura_ilce"));
            bankName = reader.GetString(reader.GetOrdinal("banka_adi"));
            bankBranch = reader.GetString(reader.GetOrdinal("banka_subesi"));
            iban = reader.GetString(reader.GetOrdinal("iban"));
            website = reader.GetString(reader.GetOrdinal("web_sitesi"));
            description = reader.GetString(reader.GetOrdinal("aciklama"));
            statusText = reader.GetString(reader.GetOrdinal("onay_durumu"));
            registrationDate = reader.GetDateTime(reader.GetOrdinal("olusturulma_tarihi"));
            approvalDate = reader.IsDBNull(reader.GetOrdinal("onay_tarihi")) ? null : reader.GetDateTime(reader.GetOrdinal("onay_tarihi"));
            rejectionReason = reader.GetString(reader.GetOrdinal("red_nedeni"));
            partnerHotelTypeId = reader.IsDBNull(reader.GetOrdinal("otel_tipi_id")) ? null : reader.GetInt32(reader.GetOrdinal("otel_tipi_id"));
            emailVerified = !reader.IsDBNull(reader.GetOrdinal("email_dogrulama_tarihi"));

            model.Status = new PartnerApplicationStatusViewModel
            {
                PartnerId = partnerId,
                CompanyName = companyName,
                HotelName = context.SelectedHotel.HotelName,
                StatusText = statusText,
                StatusToneClass = MapPartnerApprovalTone(statusText),
                RegistrationDateText = FormatDate(registrationDate),
                ApprovalDateText = approvalDate.HasValue ? FormatDate(approvalDate.Value) : null,
                RejectionReason = string.IsNullOrWhiteSpace(rejectionReason) ? null : EmptyToNull(rejectionReason),
                EmailVerified = emailVerified,
                CanPublish = string.Equals(statusText, "Onaylandi", StringComparison.OrdinalIgnoreCase),
                PublicationHint = string.Equals(statusText, "Onaylandi", StringComparison.OrdinalIgnoreCase)
                    ? "Admin onayi tamamlandi. Tesisinizi yayina almak icin icerik ve fiyat alanlarini tamamlayin."
                    : "Admin onayi tamamlanana kadar tesisiniz kamuya acik listelerde ve aramalarda gorunmez."
            };

            string hotelName = context.SelectedHotel.HotelName;
            string? neighborhood = null;
            int? hotelTypeId = null;
            string hotelType = "Otel";
            if (context.SelectedHotel.HotelId > 0)
            {
                await using var hotelCommand = new SqlCommand(hotelSql, connection);
                hotelCommand.Parameters.AddWithValue("@hotelId", context.SelectedHotel.HotelId);
                hotelCommand.CommandTimeout = 60;
                await using var hotelReader = await hotelCommand.ExecuteReaderAsync(cancellationToken);
                if (await hotelReader.ReadAsync(cancellationToken))
                {
                    hotelName = hotelReader.IsDBNull(hotelReader.GetOrdinal("otel_adi")) ? hotelName : hotelReader.GetString(hotelReader.GetOrdinal("otel_adi"));
                    neighborhood = hotelReader.GetString(hotelReader.GetOrdinal("mahalle"));
                    hotelTypeId = hotelReader.IsDBNull(hotelReader.GetOrdinal("otel_tipi_id")) ? null : hotelReader.GetInt32(hotelReader.GetOrdinal("otel_tipi_id"));
                    hotelType = hotelReader.GetString(hotelReader.GetOrdinal("otel_tipi"));
                }
            }

            model.Form = new PartnerApplicationProfileForm
            {
                HotelId = context.SelectedHotel.HotelId,
                PartnerId = partnerId,
                CompanyName = companyName,
                CompanyType = companyType,
                ContactName = contactName,
                ContactTitle = contactTitle,
                Email = email,
                Phone = phone,
                TaxOffice = taxOffice,
                TaxNumber = taxNumber,
                ContactTcNo = contactTcNo,
                Address = address,
                City = city,
                District = district,
                Neighborhood = string.IsNullOrWhiteSpace(neighborhood) ? null : EmptyToNull(neighborhood),
                BankName = bankName,
                BankBranch = string.IsNullOrWhiteSpace(bankBranch) ? null : EmptyToNull(bankBranch),
                Iban = iban,
                Website = string.IsNullOrWhiteSpace(website) ? null : EmptyToNull(website),
                Description = string.IsNullOrWhiteSpace(description) ? null : EmptyToNull(description),
                HotelName = string.IsNullOrWhiteSpace(hotelName) ? context.SelectedHotel.HotelName : hotelName,
                HotelTypeId = hotelTypeId ?? partnerHotelTypeId,
                HotelType = string.IsNullOrWhiteSpace(hotelType) ? "Otel" : hotelType
            };
        }

        model.UploadForm = new PartnerApplicationDocumentUploadForm
        {
            HotelId = model.Form.HotelId,
            PartnerId = model.Form.PartnerId
        };
        model.Documents = await LoadPartnerApplicationDocumentsAsync(connection, userId, model.Form.PartnerId, cancellationToken);
        return model;
    }

    public async Task<(bool Success, string Message)> SaveApplicationAsync(long userId, PartnerApplicationProfileForm request, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        var hotel = await EnsureHotelAccessAsync(connection, userId, request.HotelId, cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var hotelType = await ResolvePartnerHotelTypeAsync(connection, request.HotelTypeId, (SqlTransaction)transaction, cancellationToken);
            const string partnerSql = @"
                UPDATE partner_detaylari
                SET firma_unvani = @companyName,
                    firma_turu = @companyType,
                    yetkili_ad_soyad = @contactName,
                    yetkili_gorev = @contactTitle,
                    yetkili_eposta = @email,
                    yetkili_telefon = @phone,
                    vergi_dairesi = @taxOffice,
                    vergi_numarasi = @taxNumber,
                    yetkili_tc_no = @contactTcNo,
                    fatura_adresi = @address,
                    fatura_il = @city,
                    fatura_ilce = @district,
                    banka_adi = @bankName,
                    banka_subesi = @bankBranch,
                    iban = @iban,
                    hesap_sahibi_adi = @contactName,
                    web_sitesi = @website,
                    aciklama = @description,
                    otel_tipi_id = @hotelTypeId,
                    guncellenme_tarihi = GETDATE()
                WHERE id = @partnerId
                  AND kullanici_id = @userId;";

            await using (var command = new SqlCommand(partnerSql, connection, (SqlTransaction)transaction))
            {
                BindPartnerApplicationParameters(command, request, userId);
                command.Parameters.AddWithValue("@hotelTypeId", hotelType.Id);
                await command.ExecuteNonQueryAsync(cancellationToken);
            }

            const string hotelSql = @"
                UPDATE oteller
                SET otel_adi = @hotelName,
                    otel_tipi_id = @hotelTypeId,
                    otel_turu = @hotelType,
                    sehir = @city,
                    ilce = @district,
                    mahalle = @neighborhood,
                    tam_adres = @address,
                    telefon_1 = @phone,
                    eposta = @email,
                    web_sitesi = @website,
                    rezervasyon_telefonu = @phone,
                    satis_kontak_adi = @contactName,
                    satis_kontak_telefonu = @phone,
                    satis_kontak_eposta = @email,
                    kisa_aciklama = CONCAT(@hotelName, ' için partner onboarding bilgileri güncellendi.'),
                    guncellenme_tarihi = GETDATE()
                WHERE id = @hotelId
                  AND partner_id = @partnerId;";

            await using (var command = new SqlCommand(hotelSql, connection, (SqlTransaction)transaction))
            {
                BindPartnerApplicationParameters(command, request, userId);
                command.Parameters.AddWithValue("@hotelTypeId", hotelType.Id);
                command.Parameters.AddWithValue("@hotelType", hotelType.Name);
                await command.ExecuteNonQueryAsync(cancellationToken);
            }

            if (await TableExistsAsync(connection, "partner_basvuru_hareketleri", (SqlTransaction)transaction, cancellationToken))
            {
                const string historySql = @"
                    INSERT INTO partner_basvuru_hareketleri
                    (partner_id, onceki_durum, yeni_durum, islem_tipi, aciklama, islem_yapan_kullanici_id, olusturulma_tarihi)
                    SELECT id, onay_durumu, onay_durumu, 'PartnerBilgileriGuncellendi',
                           'Partner onboarding bilgileri partner panelinden güncellendi.', @userId, GETDATE()
                    FROM partner_detaylari
                    WHERE id = @partnerId;";

                await using var historyCommand = new SqlCommand(historySql, connection, (SqlTransaction)transaction);
                historyCommand.Parameters.AddWithValue("@partnerId", request.PartnerId);
                historyCommand.Parameters.AddWithValue("@userId", userId);
                await historyCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
            return (true, "Partner basvuru ve evrak bilgileri guncellendi.");
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<(bool Success, string Message)> UploadApplicationDocumentAsync(long userId, PartnerApplicationDocumentUploadForm request, CancellationToken cancellationToken = default)
    {
        if (request.File is null || request.File.Length <= 0)
        {
            return (false, "Yuklenecek bir dosya secmelisiniz.");
        }

        var ext = Path.GetExtension(request.File.FileName ?? string.Empty).ToLowerInvariant();
        var allowedExt = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".pdf", ".jpg", ".jpeg", ".png", ".webp" };
        if (string.IsNullOrWhiteSpace(ext) || !allowedExt.Contains(ext))
        {
            return (false, "Sadece PDF veya görsel dosyaları (JPG/PNG/WEBP) yükleyebilirsiniz.");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        var hotel = await EnsureHotelAccessAsync(connection, userId, request.HotelId, cancellationToken);
        if (hotel.PartnerId != request.PartnerId)
        {
            return (false, "Bu basvuru icin yetkiniz bulunmuyor.");
        }

        var stored = await _secureFileService.SaveAsync(request.File, new SecureFileSaveRequest
        {
            ContextTable = "partner_basvuru_evraklari",
            ContextId = request.PartnerId,
            OwnerUserId = userId,
            Category = "partner-application",
            VisibilityScope = "private",
            HotelId = request.HotelId
        }, cancellationToken);

        const string sql = @"
            INSERT INTO partner_basvuru_evraklari
            (
                partner_id, guvenli_dosya_id, evrak_tipi, belge_basligi, durum, yukleyen_kullanici_id, olusturulma_tarihi
            )
            VALUES
            (
                @partnerId, @fileId, @documentType, @title, 'Beklemede', @userId, GETDATE()
            );";

        await using (var command = new SqlCommand(sql, connection))
        {
            command.Parameters.AddWithValue("@partnerId", request.PartnerId);
            command.Parameters.AddWithValue("@fileId", stored.FileId);
            command.Parameters.AddWithValue("@documentType", request.DocumentType);
            command.Parameters.AddWithValue("@title", string.IsNullOrWhiteSpace(request.DocumentTitle) ? request.DocumentType : request.DocumentTitle.Trim());
            command.Parameters.AddWithValue("@userId", userId);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        return (true, "Basvuru evragi guvenli alana yuklendi.");
    }

    public async Task<(bool Success, string Message)> DeleteApplicationDocumentAsync(long userId, long hotelId, long documentId, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        var hotel = await EnsureHotelAccessAsync(connection, userId, hotelId, cancellationToken);

        const string sql = @"
            DELETE ped
            FROM partner_basvuru_evraklari ped
            INNER JOIN partner_detaylari pd ON pd.id = ped.partner_id
            WHERE ped.id = @documentId
              AND pd.id = @partnerId
              AND pd.kullanici_id = @userId;";

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@documentId", documentId);
        command.Parameters.AddWithValue("@partnerId", hotel.PartnerId);
        command.Parameters.AddWithValue("@userId", userId);
        var affected = await command.ExecuteNonQueryAsync(cancellationToken);
        return affected > 0
            ? (true, "Basvuru evragi listeden kaldirildi.")
            : (false, "Silinecek evrak bulunamadi.");
    }

    public async Task<PartnerSupportPageViewModel> GetSupportAsync(long userId, long? hotelId = null, long? ticketId = null, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        var context = await BuildContextAsync(connection, userId, hotelId, "7/24 Destek", "Canli destek, bilgi bankasi ve partner operasyon taleplerini tek merkezde toplayin.", "support", cancellationToken);
        var model = new PartnerSupportPageViewModel
        {
            Shell = context.Shell,
            CreateTicketForm = new PartnerSupportCreateTicketRequest { HotelId = context.SelectedHotel.HotelId },
            SendMessageForm = new PartnerSupportSendMessageRequest { HotelId = context.SelectedHotel.HotelId }
        };

        const string summarySql = @"
            SELECT
                COUNT(*) AS total_count,
                SUM(CASE WHEN durum IN ('Acik','Partner Yaniti Bekleniyor','Inceleniyor') THEN 1 ELSE 0 END) AS open_count,
                SUM(CASE WHEN oncelik = 'Kritik' THEN 1 ELSE 0 END) AS critical_count,
                SUM(CASE WHEN durum = 'Cozuldu' THEN 1 ELSE 0 END) AS resolved_count
            FROM partner_destek_talepleri
            WHERE partner_id = @partnerId;";

        await using (var summaryCommand = new SqlCommand(summarySql, connection))
        {
            summaryCommand.Parameters.AddWithValue("@partnerId", context.SelectedHotel.PartnerId);
            await using var reader = await summaryCommand.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                model.SummaryCards.Add(new PartnerStatCardViewModel { Label = "Toplam Talep", Value = SafeInt(reader, 0).ToString(), Description = "Tum destek gecmisi", IconClass = "fa-life-ring", ToneClass = "info" });
                model.SummaryCards.Add(new PartnerStatCardViewModel { Label = "Acik Talep", Value = SafeInt(reader, 1).ToString(), Description = "Ekip aksiyon bekleyen talepler", IconClass = "fa-headset", ToneClass = "warning" });
                model.SummaryCards.Add(new PartnerStatCardViewModel { Label = "Kritik", Value = SafeInt(reader, 2).ToString(), Description = "Onceligi yuksek destek kayitlari", IconClass = "fa-bolt", ToneClass = "danger" });
                model.SummaryCards.Add(new PartnerStatCardViewModel { Label = "Cozulen", Value = SafeInt(reader, 3).ToString(), Description = "Tamamlanan talepler", IconClass = "fa-circle-check", ToneClass = "success" });
            }
        }

        const string ticketSql = @"
            SELECT TOP (50) talep_no, konu, kategori, oncelik, durum, son_mesaj_tarihi, id, olusturulma_tarihi
            FROM partner_destek_talepleri
            WHERE partner_id = @partnerId
            ORDER BY son_mesaj_tarihi DESC, id DESC;";

        await using (var ticketCommand = new SqlCommand(ticketSql, connection))
        {
            ticketCommand.Parameters.AddWithValue("@partnerId", context.SelectedHotel.PartnerId);
            await using var reader = await ticketCommand.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                model.Tickets.Add(new PartnerSupportTicketViewModel
                {
                    TicketNo = reader.GetString(0),
                    Subject = reader.GetString(1),
                    Category = reader.GetString(2),
                    Priority = reader.GetString(3),
                    Status = reader.GetString(4),
                    UpdatedText = FormatDateTime(reader.IsDBNull(5) ? null : reader.GetDateTime(5)),
                    TicketId = reader.GetInt64(6)
                });
            }
        }

        // seçili talep: query parametre varsa onu kullan; yoksa ilk talep
        model.SelectedTicketId = (ticketId.HasValue && ticketId.Value > 0) ? ticketId.Value : model.Tickets.FirstOrDefault()?.TicketId;
        if (model.SelectedTicketId.HasValue && model.SelectedTicketId.Value > 0)
        {
            var selectedId = model.SelectedTicketId.Value;
            foreach (var t in model.Tickets)
            {
                t.IsSelected = t.TicketId == selectedId;
            }

            const string detailSql = @"
                SELECT TOP (1) id, talep_no, konu, kategori, COALESCE(oncelik,'Normal'), COALESCE(durum,'Acik'),
                               olusturulma_tarihi, son_mesaj_tarihi
                FROM partner_destek_talepleri
                WHERE partner_id = @partnerId AND id = @ticketId;";
            await using (var cmd = new SqlCommand(detailSql, connection))
            {
                cmd.Parameters.AddWithValue("@partnerId", context.SelectedHotel.PartnerId);
                cmd.Parameters.AddWithValue("@ticketId", selectedId);
                await using var r = await cmd.ExecuteReaderAsync(cancellationToken);
                if (await r.ReadAsync(cancellationToken))
                {
                    model.SelectedTicket = new PartnerSupportTicketDetailViewModel
                    {
                        TicketId = r.GetInt64(0),
                        TicketNo = r.GetString(1),
                        Subject = r.GetString(2),
                        Category = r.GetString(3),
                        Priority = r.GetString(4),
                        Status = r.GetString(5),
                        CreatedText = FormatDateTime(r.IsDBNull(6) ? null : r.GetDateTime(6)),
                        UpdatedText = FormatDateTime(r.IsDBNull(7) ? null : r.GetDateTime(7))
                    };
                }
            }

            model.SendMessageForm.TicketId = selectedId;

            const string messagesSql = @"
                SELECT TOP (200)
                    id,
                    COALESCE(gonderen_tipi,'Partner') AS sender_type,
                    COALESCE(mesaj,'') AS body,
                    ek_dosya_yolu,
                    olusturulma_tarihi
                FROM partner_destek_mesajlari
                WHERE talep_id = @ticketId
                ORDER BY id ASC;";
            await using (var msgCmd = new SqlCommand(messagesSql, connection))
            {
                msgCmd.Parameters.AddWithValue("@ticketId", selectedId);
                await using var mr = await msgCmd.ExecuteReaderAsync(cancellationToken);
                var tr = CultureInfo.GetCultureInfo("tr-TR");
                while (await mr.ReadAsync(cancellationToken))
                {
                    var senderType = mr.IsDBNull(1) ? "Partner" : mr.GetString(1);
                    var isPartner = string.Equals(senderType, "Partner", StringComparison.OrdinalIgnoreCase);
                    var created = mr.IsDBNull(4) ? (DateTime?)null : mr.GetDateTime(4);
                    var createdText = created.HasValue ? created.Value.ToLocalTime().ToString("dd.MM.yyyy HH:mm", tr) : "—";
                    var attach = mr.IsDBNull(3) ? null : mr.GetString(3);
                    model.Messages.Add(new PartnerSupportMessageViewModel
                    {
                        MessageId = mr.GetInt64(0),
                        SenderLabel = isPartner ? "Siz" : "Destek",
                        Body = mr.IsDBNull(2) ? string.Empty : mr.GetString(2),
                        TimeText = createdText,
                        IsFromPartner = isPartner,
                        AttachmentUrl = string.IsNullOrWhiteSpace(attach) ? null : attach
                    });
                }
            }
        }

        const string articleSql = @"
            SELECT TOP (6) baslik, COALESCE(ozet, ''), CONCAT('/yardim-merkezi#', seo_slug) AS url
            FROM destek_makaleleri
            WHERE durum = 1
              AND yardim_merkezinde_goster = 1
            ORDER BY one_cikan_mi DESC, siralama ASC, id DESC;";

        await using (var articleCommand = new SqlCommand(articleSql, connection))
        await using (var articleReader = await articleCommand.ExecuteReaderAsync(cancellationToken))
        {
            while (await articleReader.ReadAsync(cancellationToken))
            {
                model.KnowledgeBaseArticles.Add(new PartnerKnowledgeBaseArticleViewModel
                {
                    Title = articleReader.GetString(0),
                    Summary = articleReader.IsDBNull(1) ? string.Empty : articleReader.GetString(1),
                    Url = articleReader.IsDBNull(2) ? "/yardim-merkezi" : articleReader.GetString(2)
                });
            }
        }

        const string channelSql = @"
            SELECT kanal_adi, COALESCE(ek_bilgi, buton_metin), aciklama, ikon
            FROM destek_kanallari
            WHERE aktif_mi = 1
            ORDER BY siralama ASC, id ASC;";

        await using (var channelCommand = new SqlCommand(channelSql, connection))
        await using (var channelReader = await channelCommand.ExecuteReaderAsync(cancellationToken))
        {
            while (await channelReader.ReadAsync(cancellationToken))
            {
                model.Channels.Add(new PartnerSupportChannelViewModel
                {
                    Name = channelReader.GetString(0),
                    Value = channelReader.IsDBNull(1) ? string.Empty : channelReader.GetString(1),
                    Description = channelReader.IsDBNull(2) ? string.Empty : channelReader.GetString(2),
                    IconClass = channelReader.IsDBNull(3) ? "fa-headset" : channelReader.GetString(3)
                });
            }
        }

        return model;
    }

    public async Task<(bool Success, string Message)> SendSupportMessageAsync(long userId, PartnerSupportSendMessageRequest request, CancellationToken cancellationToken = default)
    {
        if (request.HotelId <= 0 || request.TicketId <= 0)
        {
            return (false, "Geçersiz istek.");
        }
        if (string.IsNullOrWhiteSpace(request.Message) && (request.File is null || request.File.Length <= 0))
        {
            return (false, "Mesaj veya dosya eklemelisiniz.");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        var hotel = await EnsureHotelAccessAsync(connection, userId, request.HotelId, cancellationToken);

        // ticket belongs to partner?
        const string ensureSql = "SELECT COUNT(*) FROM partner_destek_talepleri WHERE id=@id AND partner_id=@partnerId;";
        await using (var ensureCmd = new SqlCommand(ensureSql, connection))
        {
            ensureCmd.Parameters.AddWithValue("@id", request.TicketId);
            ensureCmd.Parameters.AddWithValue("@partnerId", hotel.PartnerId);
            var ok = Convert.ToInt32(await ensureCmd.ExecuteScalarAsync(cancellationToken) ?? 0, CultureInfo.InvariantCulture) > 0;
            if (!ok) return (false, "Bu talep için yetkiniz yok.");
        }

        string? attachmentUrl = null;
        if (request.File is not null && request.File.Length > 0)
        {
            var ext = Path.GetExtension(request.File.FileName ?? string.Empty);
            var safeExt = string.IsNullOrWhiteSpace(ext) ? ".bin" : ext[..Math.Min(ext.Length, 10)];
            var stamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
            var fileName = $"destek-{request.TicketId}-{stamp}{safeExt}";
            var relative = $"/uploads/support/{hotel.HotelId}/{request.TicketId}/{fileName}";
            var physical = Path.Combine(_environment.WebRootPath, "uploads", "support", hotel.HotelId.ToString(CultureInfo.InvariantCulture), request.TicketId.ToString(CultureInfo.InvariantCulture));
            Directory.CreateDirectory(physical);
            var fullPath = Path.Combine(physical, fileName);
            await using (var stream = File.Create(fullPath))
            {
                await request.File.CopyToAsync(stream, cancellationToken);
            }
            attachmentUrl = relative.Replace("\\", "/", StringComparison.Ordinal);
        }

        await using var tx = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            const string insertSql = @"
                INSERT INTO partner_destek_mesajlari
                (talep_id, gonderen_kullanici_id, gonderen_tipi, mesaj, ek_dosya_yolu, okundu_mu)
                VALUES
                (@ticketId, @userId, 'Partner', @message, @filePath, 1);";
            await using (var cmd = new SqlCommand(insertSql, connection, (SqlTransaction)tx))
            {
                cmd.Parameters.AddWithValue("@ticketId", request.TicketId);
                cmd.Parameters.AddWithValue("@userId", userId);
                cmd.Parameters.AddWithValue("@message", string.IsNullOrWhiteSpace(request.Message) ? string.Empty : request.Message.Trim());
                cmd.Parameters.AddWithValue("@filePath", (object?)attachmentUrl ?? DBNull.Value);
                await cmd.ExecuteNonQueryAsync(cancellationToken);
            }

            const string updateSql = @"
                UPDATE partner_destek_talepleri
                SET son_mesaj_tarihi = GETDATE(),
                    durum = CASE WHEN durum IN ('Cozuldu','Kapatildi') THEN 'Acik' ELSE durum END
                WHERE id = @ticketId AND partner_id = @partnerId;";
            await using (var up = new SqlCommand(updateSql, connection, (SqlTransaction)tx))
            {
                up.Parameters.AddWithValue("@ticketId", request.TicketId);
                up.Parameters.AddWithValue("@partnerId", hotel.PartnerId);
                await up.ExecuteNonQueryAsync(cancellationToken);
            }

            await tx.CommitAsync(cancellationToken);
            return (true, "Mesaj gönderildi.");
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(cancellationToken);
            return (false, "Mesaj gönderilemedi: " + ex.Message);
        }
    }

    public async Task<(bool Success, string Message)> CloseSupportTicketAsync(long userId, long hotelId, long ticketId, CancellationToken cancellationToken = default)
    {
        if (hotelId <= 0 || ticketId <= 0) return (false, "Geçersiz istek.");
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        var hotel = await EnsureHotelAccessAsync(connection, userId, hotelId, cancellationToken);

        const string sql = @"
            UPDATE partner_destek_talepleri
            SET durum = 'Cozuldu',
                guncellenme_tarihi = SYSUTCDATETIME()
            WHERE id = @ticketId AND partner_id = @partnerId;";
        await using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@ticketId", ticketId);
        cmd.Parameters.AddWithValue("@partnerId", hotel.PartnerId);
        var affected = await cmd.ExecuteNonQueryAsync(cancellationToken);
        return affected > 0 ? (true, "Talep çözüldü olarak işaretlendi.") : (false, "Talep bulunamadı.");
    }

    public async Task<PartnerCompanyPricingPageViewModel> GetCompanyPricingAsync(long userId, long? hotelId = null, long? companyId = null, long? roomId = null, string? month = null, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var context = await BuildContextAsync(connection, userId, hotelId, "Firma Fiyatları", "Firmalara özel günlük fiyatlarla kurumsal satış hacmini artırın.", "company-pricing", cancellationToken);
        var selectedHotelId = context.SelectedHotel.HotelId;

        var monthAnchor = ParseMonthAnchor(month);
        var monthStart = new DateOnly(monthAnchor.Year, monthAnchor.Month, 1);
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);

        var model = new PartnerCompanyPricingPageViewModel
        {
            Shell = context.Shell,
            HotelId = selectedHotelId,
            MonthAnchor = monthStart
        };

        // Yeni model: partner firma bazlı fiyat vermez; kurumsal fiyat listesi otel+oda+tarih bazındadır.
        model.SelectedCompanyId = 0;
        model.SelectedCompanyName = "Kurumsal";

        // Rooms
        const string roomSql = @"
            SELECT id, oda_adi
            FROM oda_tipleri
            WHERE otel_id = @hotelId AND aktif_mi = 1
            ORDER BY oda_adi;";
        await using (var roomCmd = new SqlCommand(roomSql, connection))
        {
            roomCmd.Parameters.AddWithValue("@hotelId", selectedHotelId);
            await using var roomReader = await otelturizmnew.Utils.SqlTiming.ExecuteReaderAsync(
                roomCmd,
                _slowSql,
                _logger,
                scope: "PartnerService.GetCompanyPricing.Rooms",
                slowMsThreshold: 250,
                cancellationToken);
            while (await roomReader.ReadAsync(cancellationToken))
            {
                var id = roomReader.GetInt64(0);
                model.Rooms.Add(new PartnerCompanyRoomOptionViewModel
                {
                    RoomTypeId = id,
                    RoomName = roomReader.GetString(1),
                    IsSelected = roomId.HasValue && roomId.Value == id
                });
            }
        }

        var selectedRoomId = roomId ?? model.Rooms.FirstOrDefault()?.RoomTypeId;
        model.SelectedRoomId = selectedRoomId;

        model.BulkForm = new PartnerCompanyBulkPricingUpdateRequest
        {
            HotelId = selectedHotelId,
            CompanyId = 0,
            RoomTypeId = selectedRoomId ?? 0,
            StartDate = monthStart,
            EndDate = monthEnd,
            CompanyNightlyPrice = 0m,
            CloseSales = false
        };

        model.PreviousMonthQuery = $"/panel/partner/firma-fiyatlari?otelId={selectedHotelId}&roomId={selectedRoomId}&month={ClampPricingMonth(monthStart.AddMonths(-1)):yyyy-MM}";
        model.NextMonthQuery = $"/panel/partner/firma-fiyatlari?otelId={selectedHotelId}&roomId={selectedRoomId}&month={ClampPricingMonth(monthStart.AddMonths(1)):yyyy-MM}";

        var safeRoomId = selectedRoomId.GetValueOrDefault();
        if (safeRoomId <= 0)
        {
            return model;
        }

        if (!await TableExistsAsync(connection, "firma_oda_fiyat_musaitlik", cancellationToken))
        {
            model.Warning =
                "Firma fiyat tablosu (dbo.firma_oda_fiyat_musaitlik) veritabanında bulunamadı. " +
                "Bu sayfa firma fiyatlarını kaydedemez/okuyamaz. " +
                "SQL Server için migration dosyası: Database/MigrationsSql/20260426_sqlserver_create_table_firma_oda_fiyat_musaitlik.sql";
            return model;
        }

        // Load firm prices map
        var firmPrices = new Dictionary<DateOnly, (decimal Price, bool Closed, int? MinNight, int? MaxNight)>();
        const string firmSql = @"
            SELECT tarih, firma_gecelik_fiyat, kapali_satis, minimum_geceleme, maksimum_geceleme
            FROM firma_oda_fiyat_musaitlik
            WHERE otel_id = @hotelId
              AND oda_tip_id = @roomTypeId
              AND tarih BETWEEN @startDate AND @endDate;";
        await using (var firmCmd = new SqlCommand(firmSql, connection))
        {
            firmCmd.Parameters.AddWithValue("@hotelId", selectedHotelId);
            firmCmd.Parameters.AddWithValue("@roomTypeId", safeRoomId);
            firmCmd.Parameters.AddWithValue("@startDate", monthStart.ToDateTime(TimeOnly.MinValue));
            firmCmd.Parameters.AddWithValue("@endDate", monthEnd.ToDateTime(TimeOnly.MinValue));
            await using var firmReader = await otelturizmnew.Utils.SqlTiming.ExecuteReaderAsync(
                firmCmd,
                _slowSql,
                _logger,
                scope: "PartnerService.GetCompanyPricing.FirmPrices",
                slowMsThreshold: 250,
                cancellationToken);
            while (await firmReader.ReadAsync(cancellationToken))
            {
                var date = DateOnly.FromDateTime(firmReader.GetDateTime(0));
                var price = firmReader.GetDecimal(1);
                var closed = !firmReader.IsDBNull(2) && firmReader.GetBoolean(2);
                var minNight = firmReader.IsDBNull(3) ? (int?)null : Convert.ToInt32(firmReader.GetValue(3), CultureInfo.InvariantCulture);
                var maxNight = firmReader.IsDBNull(4) ? (int?)null : Convert.ToInt32(firmReader.GetValue(4), CultureInfo.InvariantCulture);
                firmPrices[date] = (price, closed, minNight, maxNight);
            }
        }

        // Load base prices map (oda_fiyat_musaitlik)
        var basePrices = new Dictionary<DateOnly, decimal>();
        const string baseSql = @"
            SELECT tarih,
                   CASE
                       WHEN indirimli_fiyat IS NOT NULL AND indirimli_fiyat > 0 AND indirimli_fiyat < gecelik_fiyat THEN indirimli_fiyat
                       ELSE gecelik_fiyat
                   END AS effective_price
            FROM oda_fiyat_musaitlik
            WHERE otel_id = @hotelId
              AND oda_tip_id = @roomTypeId
              AND tarih BETWEEN @startDate AND @endDate;";
        await using (var baseCmd = new SqlCommand(baseSql, connection))
        {
            baseCmd.Parameters.AddWithValue("@hotelId", selectedHotelId);
            baseCmd.Parameters.AddWithValue("@roomTypeId", safeRoomId);
            baseCmd.Parameters.AddWithValue("@startDate", monthStart.ToDateTime(TimeOnly.MinValue));
            baseCmd.Parameters.AddWithValue("@endDate", monthEnd.ToDateTime(TimeOnly.MinValue));
            await using var baseReader = await otelturizmnew.Utils.SqlTiming.ExecuteReaderAsync(
                baseCmd,
                _slowSql,
                _logger,
                scope: "PartnerService.GetCompanyPricing.BasePrices",
                slowMsThreshold: 250,
                cancellationToken);
            while (await baseReader.ReadAsync(cancellationToken))
            {
                var date = DateOnly.FromDateTime(baseReader.GetDateTime(0));
                var price = baseReader.IsDBNull(1) ? 0m : baseReader.GetDecimal(1);
                if (price > 0m) basePrices[date] = price;
            }
        }

        var culture = CultureInfo.GetCultureInfo("tr-TR");
        for (var day = monthStart; day <= monthEnd; day = day.AddDays(1))
        {
            var firmHas = firmPrices.TryGetValue(day, out var fp);
            var baseHas = basePrices.TryGetValue(day, out var bp);
            var isClosed = firmHas && fp.Closed;
            var nightRuleText = firmHas && (fp.MinNight.HasValue || fp.MaxNight.HasValue)
                ? $"{(fp.MinNight.HasValue ? $"Min {fp.MinNight}" : "Min yok")} / {(fp.MaxNight.HasValue ? $"Maks {fp.MaxNight}" : "Maks yok")} gece"
                : "Koşul yok";
            model.Days.Add(new PartnerCompanyPricingDayViewModel
            {
                Date = day,
                DateText = day.ToString("dd MMM", culture),
                CompanyPrice = firmHas ? fp.Price : null,
                BasePrice = baseHas ? bp : null,
                MinimumNights = firmHas ? fp.MinNight : null,
                MaximumNights = firmHas ? fp.MaxNight : null,
                IsClosed = isClosed,
                CompanyPriceText = firmHas ? fp.Price.ToString("N0", culture) + " ₺" : "-",
                BasePriceText = baseHas ? bp.ToString("N0", culture) + " ₺" : "-",
                NightRuleText = nightRuleText
            });
        }

        return model;
    }

    public async Task<(bool Success, string Message)> ApplyCompanyBulkPricingAsync(long userId, PartnerCompanyBulkPricingUpdateRequest request, CancellationToken cancellationToken = default)
    {
        if (request.HotelId <= 0)
        {
            return (false, "Otel seçilmelidir.");
        }
        if (request.EndDate < request.StartDate)
        {
            return (false, "Bitiş tarihi başlangıçtan önce olamaz.");
        }
        var pricingWindowError = ValidatePricingWindow(
            request.StartDate.ToDateTime(TimeOnly.MinValue),
            request.EndDate.ToDateTime(TimeOnly.MinValue));
        if (pricingWindowError is not null)
        {
            return (false, pricingWindowError);
        }
        if (request.CompanyNightlyPrice <= 0)
        {
            return (false, "Firma gecelik fiyatı 0'dan büyük olmalıdır.");
        }
        if (request.MinimumNights.HasValue && request.MinimumNights.Value < 1)
        {
            return (false, "Minimum geceleme en az 1 olmalıdır.");
        }
        if (request.MaximumNights.HasValue && request.MaximumNights.Value < 1)
        {
            return (false, "Maksimum geceleme en az 1 olmalıdır.");
        }
        if (request.MinimumNights.HasValue && request.MaximumNights.HasValue && request.MaximumNights.Value < request.MinimumNights.Value)
        {
            return (false, "Maksimum geceleme minimum gecelemeden küçük olamaz.");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await EnsurePartnerHotelAccessAsync(connection, userId, request.HotelId, cancellationToken);

        if (!await TableExistsAsync(connection, "firma_oda_fiyat_musaitlik", cancellationToken))
        {
            return (false,
                "Firma fiyat tablosu (dbo.firma_oda_fiyat_musaitlik) veritabanında bulunamadı. " +
                "SQL Server migration uygulanmadığı için kaydedilemedi. " +
                "Dosya: Database/MigrationsSql/20260426_sqlserver_create_table_firma_oda_fiyat_musaitlik.sql");
        }

        await using var tx = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            var targetRoomIds = (request.RoomTypeIds is { Count: > 0 }
                    ? request.RoomTypeIds
                    : new List<long> { request.RoomTypeId })
                .Where(static x => x > 0)
                .Distinct()
                .ToList();

            if (targetRoomIds.Count == 0)
            {
                return (false, "En az bir oda tipi seçilmelidir.");
            }

            for (var date = request.StartDate; date <= request.EndDate; date = date.AddDays(1))
            {
                foreach (var roomTypeId in targetRoomIds)
                {
                    const string upsertSql = @"
                        IF EXISTS (SELECT 1 FROM firma_oda_fiyat_musaitlik WHERE otel_id=@hotelId AND oda_tip_id=@roomTypeId AND tarih=@date)
                        BEGIN
                            UPDATE firma_oda_fiyat_musaitlik
                            SET firma_gecelik_fiyat=@price,
                                minimum_geceleme=@minNight,
                                maksimum_geceleme=@maxNight,
                                kapali_satis=@closed,
                                fiyat_notu=@note,
                                guncelleyen_kullanici_id=@userId,
                                guncellenme_tarihi=SYSUTCDATETIME()
                            WHERE otel_id=@hotelId AND oda_tip_id=@roomTypeId AND tarih=@date;
                        END
                        ELSE
                        BEGIN
                            INSERT INTO firma_oda_fiyat_musaitlik
                            (otel_id, oda_tip_id, tarih, firma_gecelik_fiyat, minimum_geceleme, maksimum_geceleme, kapali_satis, fiyat_notu, guncelleyen_kullanici_id)
                            VALUES
                            (@hotelId, @roomTypeId, @date, @price, @minNight, @maxNight, @closed, @note, @userId);
                        END";
                    await using var cmd = new SqlCommand(upsertSql, connection, (SqlTransaction)tx);
                    cmd.Parameters.AddWithValue("@hotelId", request.HotelId);
                    cmd.Parameters.AddWithValue("@roomTypeId", roomTypeId);
                    cmd.Parameters.AddWithValue("@date", date.ToDateTime(TimeOnly.MinValue));
                    cmd.Parameters.AddWithValue("@price", request.CompanyNightlyPrice);
                    cmd.Parameters.AddWithValue("@minNight", request.MinimumNights.HasValue ? request.MinimumNights.Value : (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@maxNight", request.MaximumNights.HasValue ? request.MaximumNights.Value : (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@closed", request.CloseSales ? 1 : 0);
                    cmd.Parameters.AddWithValue("@note", string.IsNullOrWhiteSpace(request.Note) ? DBNull.Value : request.Note.Trim());
                    cmd.Parameters.AddWithValue("@userId", userId);
                    await otelturizmnew.Utils.SqlTiming.ExecuteNonQueryAsync(
                        cmd,
                        _slowSql,
                        _logger,
                        scope: "PartnerService.ApplyCompanyBulkPricing.Upsert",
                        slowMsThreshold: 250,
                        cancellationToken);
                }
            }

            await tx.CommitAsync(cancellationToken);
            return (true, "Firma fiyatları kaydedildi.");
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(cancellationToken);
            return (false, "Firma fiyatları kaydedilemedi: " + ex.Message);
        }
    }

    public async Task<PartnerListingSubscriptionsPageViewModel> GetListingSubscriptionsAsync(long userId, long? hotelId = null, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var context = await BuildContextAsync(
            connection,
            userId,
            hotelId,
            "Aboneliklerim",
            "İl/ilçe/mahalle aramalarında 1-2-3 sabit çıkmak için abonelik taleplerinizi yönetin.",
            "ListingSubscriptions",
            cancellationToken);

        var model = new PartnerListingSubscriptionsPageViewModel
        {
            Shell = context.Shell,
            SelectedHotelId = context.SelectedHotel.HotelId,
            SelectedHotelName = context.SelectedHotel.HotelName
        };

        model.Form.HotelId = context.SelectedHotel.HotelId;
        model.Form.ScopeType = "IL";
        model.Form.DesiredRank = 1;
        model.Form.DayCount = 7;

        if (!await TableExistsAsync(connection, "otel_liste_abonelikleri", cancellationToken))
        {
            return model;
        }

        const string sql = @"
            SELECT TOP (120)
                a.id,
                a.kapsam_tipi,
                a.kapsam_degeri,
                a.hedef_sira,
                a.durum,
                a.baslangic_utc,
                a.bitis_utc,
                COALESCE(a.partner_notu,'')
            FROM otel_liste_abonelikleri a
            WHERE a.otel_id = @hotelId
            ORDER BY a.olusturulma_tarihi DESC, a.id DESC;";

        await using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@hotelId", context.SelectedHotel.HotelId);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var start = reader.IsDBNull(5) ? (DateTime?)null : reader.GetDateTime(5);
            var end = reader.IsDBNull(6) ? (DateTime?)null : reader.GetDateTime(6);
            model.Items.Add(new PartnerListingSubscriptionRowViewModel
            {
                Id = Convert.ToInt64(reader.GetValue(0), CultureInfo.InvariantCulture),
                ScopeType = reader.IsDBNull(1) ? "-" : reader.GetString(1),
                ScopeValue = reader.IsDBNull(2) ? "-" : reader.GetString(2),
                DesiredRank = reader.IsDBNull(3) ? 0 : Convert.ToInt32(reader.GetValue(3), CultureInfo.InvariantCulture),
                Status = reader.IsDBNull(4) ? "-" : reader.GetString(4),
                StartText = start.HasValue ? start.Value.ToString("dd.MM.yyyy", CultureInfo.GetCultureInfo("tr-TR")) : "-",
                EndText = end.HasValue ? end.Value.ToString("dd.MM.yyyy", CultureInfo.GetCultureInfo("tr-TR")) : "-",
                Note = reader.IsDBNull(7) ? string.Empty : reader.GetString(7)
            });
        }

        return model;
    }

    public async Task<PartnerLocationInsightsPageViewModel> GetLocationInsightsAsync(long userId, long? hotelId = null, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var context = await BuildContextAsync(
            connection,
            userId,
            hotelId,
            "Konum İçgörüleri",
            "Rezervasyonlardan şehir ve ülke dağılımı ile günlük istatistik özetlerini izleyin.",
            "location-insights",
            cancellationToken);

        var model = new PartnerLocationInsightsPageViewModel
        {
            Shell = context.Shell,
            SelectedHotelId = context.SelectedHotel.HotelId,
            InfoNote = "Son 12 ayda oluşturulan ve iptal/red edilmeyen rezervasyonlar üzerinden misafir şehir dağılımı hesaplanır. Eksik adres bilgisi “Bilinmiyor” olarak gruplanır. Günlük istatistikler otel_istatistikleri tablosundan okunur."
        };

        const string hotelLocSql = @"
            SELECT COALESCE(sehir, N''), COALESCE(ilce, N'')
            FROM oteller
            WHERE id = @hotelId;";
        await using (var hcmd = new SqlCommand(hotelLocSql, connection))
        {
            hcmd.Parameters.AddWithValue("@hotelId", context.SelectedHotel.HotelId);
            await using var reader = await hcmd.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                var sehir = reader.IsDBNull(0) ? string.Empty : reader.GetString(0);
                var ilce = reader.IsDBNull(1) ? string.Empty : reader.GetString(1);
                if (string.IsNullOrWhiteSpace(sehir) && string.IsNullOrWhiteSpace(ilce))
                {
                    model.HotelLocationLabel = "—";
                }
                else if (string.IsNullOrWhiteSpace(ilce))
                {
                    model.HotelLocationLabel = sehir.Trim();
                }
                else if (string.IsNullOrWhiteSpace(sehir))
                {
                    model.HotelLocationLabel = ilce.Trim();
                }
                else
                {
                    model.HotelLocationLabel = $"{sehir.Trim()} / {ilce.Trim()}";
                }
            }
        }

        if (!await TableExistsAsync(connection, "rezervasyonlar", cancellationToken))
        {
            model.SummaryCards.Add(new PartnerStatCardViewModel { Label = "Veri", Value = "0", Description = "Rezervasyon tablosu bulunamadı", IconClass = "fa-map-location-dot", ToneClass = "warning" });
            return model;
        }

        const string cancelExclude = @"
            COALESCE(r.durum, N'') NOT IN (N'İptal', N'İptal Edildi', N'Red', N'Reddedildi')";

        var totalWithGeoSql = $@"
            SELECT COUNT(*)
            FROM rezervasyonlar r
            WHERE r.otel_id = @hotelId
              AND r.olusturulma_tarihi >= DATEADD(month, -12, SYSUTCDATETIME())
              AND {cancelExclude};";

        var totalCount = 0;
        await using (var tcmd = new SqlCommand(totalWithGeoSql, connection))
        {
            tcmd.Parameters.AddWithValue("@hotelId", context.SelectedHotel.HotelId);
            var obj = await tcmd.ExecuteScalarAsync(cancellationToken);
            totalCount = obj is null || obj == DBNull.Value ? 0 : Convert.ToInt32(obj, CultureInfo.InvariantCulture);
        }

        const string citySql = $@"
            SELECT TOP (40)
                COALESCE(NULLIF(LTRIM(RTRIM(r.misafir_sehir)), N''), N'Bilinmiyor') AS sehir,
                COUNT(*) AS cnt
            FROM rezervasyonlar r
            WHERE r.otel_id = @hotelId
              AND r.olusturulma_tarihi >= DATEADD(month, -12, SYSUTCDATETIME())
              AND {cancelExclude}
            GROUP BY COALESCE(NULLIF(LTRIM(RTRIM(r.misafir_sehir)), N''), N'Bilinmiyor')
            ORDER BY COUNT(*) DESC;";

        await using (var cityCmd = new SqlCommand(citySql, connection))
        {
            cityCmd.Parameters.AddWithValue("@hotelId", context.SelectedHotel.HotelId);
            await using var reader = await cityCmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var city = reader.IsDBNull(0) ? "Bilinmiyor" : reader.GetString(0);
                var cnt = reader.IsDBNull(1) ? 0 : Convert.ToInt32(reader.GetValue(1), CultureInfo.InvariantCulture);
                var pct = totalCount > 0 ? Math.Round(100m * cnt / totalCount, 1, MidpointRounding.AwayFromZero) : 0m;
                model.GuestCityRows.Add(new PartnerGuestCityInsightRowViewModel
                {
                    CityLabel = city,
                    ReservationCount = cnt,
                    SharePercent = pct
                });
            }
        }

        const string countrySql = $@"
            SELECT TOP (25)
                COALESCE(NULLIF(LTRIM(RTRIM(r.misafir_ulke)), N''), N'Bilinmiyor') AS ulke,
                COUNT(*) AS cnt
            FROM rezervasyonlar r
            WHERE r.otel_id = @hotelId
              AND r.olusturulma_tarihi >= DATEADD(month, -12, SYSUTCDATETIME())
              AND {cancelExclude}
            GROUP BY COALESCE(NULLIF(LTRIM(RTRIM(r.misafir_ulke)), N''), N'Bilinmiyor')
            ORDER BY COUNT(*) DESC;";

        await using (var countryCmd = new SqlCommand(countrySql, connection))
        {
            countryCmd.Parameters.AddWithValue("@hotelId", context.SelectedHotel.HotelId);
            await using var reader = await countryCmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var ulke = reader.IsDBNull(0) ? "Bilinmiyor" : reader.GetString(0);
                var cnt = reader.IsDBNull(1) ? 0 : Convert.ToInt32(reader.GetValue(1), CultureInfo.InvariantCulture);
                var pct = totalCount > 0 ? Math.Round(100m * cnt / totalCount, 1, MidpointRounding.AwayFromZero) : 0m;
                model.GuestCountryRows.Add(new PartnerGuestCountryInsightRowViewModel
                {
                    CountryLabel = ulke,
                    ReservationCount = cnt,
                    SharePercent = pct
                });
            }
        }

        var distinctCities = model.GuestCityRows.Count;
        var topCity = model.GuestCityRows.FirstOrDefault()?.CityLabel ?? "—";
        var topShare = model.GuestCityRows.FirstOrDefault()?.SharePercent ?? 0m;

        model.SummaryCards.Add(new PartnerStatCardViewModel { Label = "Tesis konumu", Value = model.HotelLocationLabel, Description = "Kayıtlı adres", IconClass = "fa-hotel", ToneClass = "info" });
        model.SummaryCards.Add(new PartnerStatCardViewModel { Label = "Analiz rezervasyon", Value = totalCount.ToString(CultureInfo.InvariantCulture), Description = "Son 12 ay (iptal/red hariç)", IconClass = "fa-calendar-check", ToneClass = "success" });
        model.SummaryCards.Add(new PartnerStatCardViewModel { Label = "Şehir çeşitliliği", Value = distinctCities.ToString(CultureInfo.InvariantCulture), Description = "Farklı şehir grubu", IconClass = "fa-city", ToneClass = "warning" });
        model.SummaryCards.Add(new PartnerStatCardViewModel { Label = "En yoğun şehir", Value = topCity, Description = totalCount > 0 ? $"%{topShare.ToString("0.#", CultureInfo.InvariantCulture)} pay" : "Veri yok", IconClass = "fa-chart-pie", ToneClass = "danger" });

        if (await TableExistsAsync(connection, "otel_istatistikleri", cancellationToken))
        {
            const string dailySql = @"
                SELECT TOP (90)
                    istatistik_tarihi,
                    COALESCE(rezervasyon_sayisi, 0),
                    doluluk_orani,
                    net_gelir
                FROM otel_istatistikleri
                WHERE otel_id = @hotelId
                ORDER BY istatistik_tarihi DESC;";

            await using var dcmd = new SqlCommand(dailySql, connection);
            dcmd.Parameters.AddWithValue("@hotelId", context.SelectedHotel.HotelId);
            await using var reader = await dcmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var d = reader.GetDateTime(0);
                model.DailyStatRows.Add(new PartnerDailyHotelStatRowViewModel
                {
                    DateText = d.ToString("dd.MM.yyyy", CultureInfo.GetCultureInfo("tr-TR")),
                    Reservations = reader.IsDBNull(1) ? 0 : Convert.ToInt32(reader.GetValue(1), CultureInfo.InvariantCulture),
                    OccupancyPercent = reader.IsDBNull(2) ? null : SafeDecimal(reader, 2),
                    NetRevenue = reader.IsDBNull(3) ? null : SafeDecimal(reader, 3)
                });
            }
        }

        return model;
    }

    public async Task<PartnerFavoriteGuestsPageViewModel> GetFavoriteGuestsAsync(long userId, long? hotelId = null, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var context = await BuildContextAsync(
            connection,
            userId,
            hotelId,
            "Favori Misafirler",
            "Otelinizi favorilerine ekleyen kayıtlı kullanıcıları izleyin.",
            "favorite-guests",
            cancellationToken);

        var model = new PartnerFavoriteGuestsPageViewModel
        {
            Shell = context.Shell,
            SelectedHotelId = context.SelectedHotel.HotelId,
            PrivacyNote = "E-posta adresleri KVKK kapsamında maskelenmiştir. Detaylı iletişim için misafir rezervasyon kayıtlarınızı kullanın."
        };

        if (!await TableExistsAsync(connection, "user_favori_oteller", cancellationToken)
            || !await TableExistsAsync(connection, "users", cancellationToken))
        {
            model.SummaryCards.Add(new PartnerStatCardViewModel { Label = "Favori", Value = "0", Description = "Tablo bulunamadı", IconClass = "fa-heart", ToneClass = "warning" });
            return model;
        }

        var hasRemoved = await ColumnExistsAsync(connection, "user_favori_oteller", "kaldirilma_tarihi", cancellationToken);
        var removedClause = hasRemoved ? " AND uf.kaldirilma_tarihi IS NULL " : string.Empty;

        var countSql = $@"
            SELECT COUNT(*),
                   SUM(CASE WHEN uf.olusturulma_tarihi >= DATEADD(day, -7, SYSUTCDATETIME()) THEN 1 ELSE 0 END)
            FROM user_favori_oteller uf
            WHERE uf.otel_id = @hotelId {removedClause};";

        var totalFav = 0;
        var last7 = 0;
        await using (var ccmd = new SqlCommand(countSql, connection))
        {
            ccmd.Parameters.AddWithValue("@hotelId", context.SelectedHotel.HotelId);
            await using var reader = await ccmd.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                totalFav = reader.IsDBNull(0) ? 0 : Convert.ToInt32(reader.GetValue(0), CultureInfo.InvariantCulture);
                last7 = reader.IsDBNull(1) ? 0 : Convert.ToInt32(reader.GetValue(1), CultureInfo.InvariantCulture);
            }
        }

        model.SummaryCards.Add(new PartnerStatCardViewModel { Label = "Toplam favori", Value = totalFav.ToString(CultureInfo.InvariantCulture), Description = "Aktif favori kayıtları", IconClass = "fa-heart", ToneClass = "success" });
        model.SummaryCards.Add(new PartnerStatCardViewModel { Label = "Son 7 gün", Value = last7.ToString(CultureInfo.InvariantCulture), Description = "Yeni eklenenler", IconClass = "fa-clock", ToneClass = "info" });
        model.SummaryCards.Add(new PartnerStatCardViewModel { Label = "Kaynak çeşitliliği", Value = "—", Description = "Sayfa ve cihaz sütunlarından izleyin", IconClass = "fa-mobile-screen", ToneClass = "warning" });

        var listSql = $@"
            SELECT TOP (200)
                uf.id,
                uf.user_id,
                COALESCE(u.ad_soyad, N'Misafir'),
                COALESCE(u.eposta, N''),
                COALESCE(u.sehir, N''),
                COALESCE(uf.kaynak_sayfa, N''),
                COALESCE(uf.cihaz_tipi, N''),
                uf.olusturulma_tarihi
            FROM user_favori_oteller uf
            LEFT JOIN users u ON u.id = uf.user_id
            WHERE uf.otel_id = @hotelId {removedClause}
            ORDER BY uf.olusturulma_tarihi DESC;";

        await using (var lcmd = new SqlCommand(listSql, connection))
        {
            lcmd.Parameters.AddWithValue("@hotelId", context.SelectedHotel.HotelId);
            await using var reader = await lcmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var added = reader.IsDBNull(7) ? DateTime.UtcNow : reader.GetDateTime(7);
                model.Rows.Add(new PartnerFavoriteGuestRowViewModel
                {
                    FavoriteLinkId = Convert.ToInt64(reader.GetValue(0), CultureInfo.InvariantCulture),
                    UserId = Convert.ToInt64(reader.GetValue(1), CultureInfo.InvariantCulture),
                    DisplayName = reader.IsDBNull(2) ? "Misafir" : reader.GetString(2),
                    MaskedEmail = MaskEmail(reader.IsDBNull(3) ? null : reader.GetString(3)),
                    UserCity = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                    SourcePage = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
                    DeviceType = reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
                    AddedText = added.ToString("dd.MM.yyyy HH:mm", CultureInfo.GetCultureInfo("tr-TR"))
                });
            }
        }

        return model;
    }

    public async Task<PartnerMarketingEventsPageViewModel> GetMarketingEventsAsync(long userId, long? hotelId = null, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var context = await BuildContextAsync(
            connection,
            userId,
            hotelId,
            "Kampanya ve Katılım Takvimi",
            "Kampanya eşleşmelerinizi tarih ve durum ile yönetin; operasyon notlarını kaydedin.",
            "marketing-events",
            cancellationToken);

        var model = new PartnerMarketingEventsPageViewModel
        {
            Shell = context.Shell,
            SelectedHotelId = context.SelectedHotel.HotelId,
            IntroNote = "Bu ekran kampanya_oteller kayıtlarınızı listeler. Katılım bir kampanya zaman penceresidir; yeni kampanyaya giriş için Kampanyalar menüsünü kullanın."
        };

        if (!await TableExistsAsync(connection, "kampanya_oteller", cancellationToken)
            || !await TableExistsAsync(connection, "kampanyalar", cancellationToken))
        {
            model.SummaryCards.Add(new PartnerStatCardViewModel { Label = "Kayıt", Value = "0", Description = "Kampanya tabloları eksik", IconClass = "fa-calendar-days", ToneClass = "warning" });
            return model;
        }

        const string sql = @"
            SELECT
                ko.id,
                k.id,
                k.kampanya_adi,
                k.kampanya_kodu,
                COALESCE(ko.katilim_durumu, N''),
                ko.baslangic_tarihi,
                ko.bitis_tarihi,
                COALESCE(ko.one_cikan, 0),
                COALESCE(ko.landing_url, N''),
                COALESCE(ko.partner_notu, N'')
            FROM dbo.kampanya_oteller ko
            INNER JOIN dbo.kampanyalar k ON k.id = ko.kampanya_id
            WHERE ko.otel_id = @hotelId
            ORDER BY ko.baslangic_tarihi DESC, ko.id DESC;";

        var activeCount = 0;
        var featuredCount = 0;

        await using (var cmd = new SqlCommand(sql, connection))
        {
            cmd.Parameters.AddWithValue("@hotelId", context.SelectedHotel.HotelId);
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var status = reader.IsDBNull(4) ? string.Empty : reader.GetString(4);
                var oneCikan = !reader.IsDBNull(7) && Convert.ToBoolean(reader.GetValue(7), CultureInfo.InvariantCulture);
                if (string.Equals(status, "Aktif", StringComparison.OrdinalIgnoreCase))
                {
                    activeCount++;
                }

                if (oneCikan)
                {
                    featuredCount++;
                }

                var start = reader.IsDBNull(5) ? DateTime.UtcNow : reader.GetDateTime(5);
                var end = reader.IsDBNull(6) ? DateTime.UtcNow : reader.GetDateTime(6);
                model.Participations.Add(new PartnerCampaignParticipationRowViewModel
                {
                    CampaignHotelLinkId = Convert.ToInt64(reader.GetValue(0), CultureInfo.InvariantCulture),
                    CampaignId = Convert.ToInt64(reader.GetValue(1), CultureInfo.InvariantCulture),
                    CampaignName = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                    CampaignCode = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                    ParticipationStatus = status,
                    WindowStartText = start.ToString("dd.MM.yyyy HH:mm", CultureInfo.GetCultureInfo("tr-TR")),
                    WindowEndText = end.ToString("dd.MM.yyyy HH:mm", CultureInfo.GetCultureInfo("tr-TR")),
                    IsFeatured = oneCikan,
                    LandingUrl = reader.IsDBNull(8) || string.IsNullOrWhiteSpace(reader.GetString(8)) ? null : reader.GetString(8),
                    PartnerNote = reader.IsDBNull(9) ? null : reader.GetString(9)
                });
            }
        }

        model.SummaryCards.Add(new PartnerStatCardViewModel { Label = "Toplam eşleşme", Value = model.Participations.Count.ToString(CultureInfo.InvariantCulture), Description = "kampanya_oteller kayıtları", IconClass = "fa-link", ToneClass = "info" });
        model.SummaryCards.Add(new PartnerStatCardViewModel { Label = "Aktif katılım", Value = activeCount.ToString(CultureInfo.InvariantCulture), Description = "Durumu Aktif", IconClass = "fa-check-circle", ToneClass = "success" });
        model.SummaryCards.Add(new PartnerStatCardViewModel { Label = "Öne çıkan", Value = featuredCount.ToString(CultureInfo.InvariantCulture), Description = "Vitrin işaretli", IconClass = "fa-star", ToneClass = "warning" });

        return model;
    }

    public async Task<(bool Success, string Message)> SaveCampaignParticipationNoteAsync(long userId, PartnerCampaignParticipationNoteRequest request, CancellationToken cancellationToken = default)
    {
        if (request.HotelId <= 0 || request.CampaignHotelLinkId <= 0)
        {
            return (false, "Geçersiz kayıt.");
        }

        var note = (request.PartnerNote ?? string.Empty).Trim();
        if (note.Length > 4000)
        {
            return (false, "Not en fazla 4000 karakter olabilir.");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await EnsureHotelAccessAsync(connection, userId, request.HotelId, cancellationToken);

        if (!await TableExistsAsync(connection, "kampanya_oteller", cancellationToken))
        {
            return (false, "Kampanya tablosu bulunamadı.");
        }

        const string sql = @"
            UPDATE dbo.kampanya_oteller
            SET partner_notu = @note,
                guncellenme_tarihi = SYSUTCDATETIME(),
                guncelleyen_kullanici_id = @userId
            WHERE id = @linkId AND otel_id = @hotelId;";

        await using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@note", string.IsNullOrEmpty(note) ? (object)DBNull.Value : note);
        cmd.Parameters.AddWithValue("@userId", userId);
        cmd.Parameters.AddWithValue("@linkId", request.CampaignHotelLinkId);
        cmd.Parameters.AddWithValue("@hotelId", request.HotelId);

        var n = await cmd.ExecuteNonQueryAsync(cancellationToken);
        return n > 0
            ? (true, "Kampanya notu kaydedildi.")
            : (false, "Kayıt güncellenemedi veya yetkiniz yok.");
    }

    public async Task<PartnerSettingsPageViewModel> GetPartnerSettingsPageAsync(long userId, long? hotelId = null, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        var context = await BuildContextAsync(
            connection,
            userId,
            hotelId,
            "Ayarlar",
            "Panel teması, görünüm ve ilgili modüllere hızlı erişim.",
            "settings",
            cancellationToken);
        return new PartnerSettingsPageViewModel { Shell = context.Shell };
    }

    public async Task<PartnerPreferencesPageViewModel> GetPartnerPreferencesAsync(long userId, long? hotelId = null, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        var context = await BuildContextAsync(connection, userId, hotelId, "Tercihler", "Panel ve bildirim tercihlerinizi yönetin.", "settings", cancellationToken);

        var model = new PartnerPreferencesPageViewModel
        {
            Shell = context.Shell,
            Form = new PartnerPreferencesForm
            {
                HotelId = context.SelectedHotel.HotelId,
                DefaultHotelId = context.SelectedHotel.HotelId
            }
        };

        if (!await TableExistsAsync(connection, "partner_panel_tercihleri", cancellationToken))
        {
            return model;
        }

        const string sql = @"
            SELECT TOP (1)
                COALESCE(dil, N'tr'),
                COALESCE(para_birimi, N'TRY'),
                COALESCE(zaman_dilimi, N'Europe/Istanbul'),
                COALESCE(takvim_gorunumu, N'Aylik'),
                COALESCE(email_bildirimleri, 1),
                COALESCE(sms_bildirimleri, 0),
                COALESCE(push_bildirimleri, 1),
                COALESCE(masaustu_bildirimleri, 1),
                COALESCE(yeni_rezervasyon_bildirimi, 1),
                COALESCE(iptal_bildirimi, 1),
                COALESCE(odeme_bildirimi, 1),
                COALESCE(yorum_bildirimi, 1),
                COALESCE(otomatik_fiyat_onerisi, 1),
                COALESCE(otomatik_closeout_uyarisi, 1),
                COALESCE(cihaz_hatirla, 1),
                varsayilan_otel_id
            FROM dbo.partner_panel_tercihleri
            WHERE kullanici_id = @userId
            ORDER BY id DESC;";

        await using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@userId", userId);
        await using var r = await cmd.ExecuteReaderAsync(cancellationToken);
        if (await r.ReadAsync(cancellationToken))
        {
            model.Form.Language = r.IsDBNull(0) ? "tr" : r.GetString(0);
            model.Form.Currency = r.IsDBNull(1) ? "TRY" : r.GetString(1);
            model.Form.Timezone = r.IsDBNull(2) ? "Europe/Istanbul" : r.GetString(2);
            model.Form.CalendarView = r.IsDBNull(3) ? "Aylik" : r.GetString(3);
            model.Form.EmailNotifications = !r.IsDBNull(4) && r.GetBoolean(4);
            model.Form.SmsNotifications = !r.IsDBNull(5) && r.GetBoolean(5);
            model.Form.PushNotifications = !r.IsDBNull(6) && r.GetBoolean(6);
            model.Form.DesktopNotifications = !r.IsDBNull(7) && r.GetBoolean(7);
            model.Form.NewReservationNotifications = !r.IsDBNull(8) && r.GetBoolean(8);
            model.Form.CancellationNotifications = !r.IsDBNull(9) && r.GetBoolean(9);
            model.Form.PaymentNotifications = !r.IsDBNull(10) && r.GetBoolean(10);
            model.Form.ReviewNotifications = !r.IsDBNull(11) && r.GetBoolean(11);
            model.Form.AutoPriceSuggestions = !r.IsDBNull(12) && r.GetBoolean(12);
            model.Form.AutoCloseoutWarnings = !r.IsDBNull(13) && r.GetBoolean(13);
            model.Form.RememberDevice = !r.IsDBNull(14) && r.GetBoolean(14);
            model.Form.DefaultHotelId = r.IsDBNull(15) ? context.SelectedHotel.HotelId : r.GetInt64(15);
        }

        return model;
    }

    public async Task<(bool Success, string Message)> SavePartnerPreferencesAsync(long userId, PartnerPreferencesForm form, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await EnsureHotelAccessAsync(connection, userId, form.HotelId, cancellationToken);

        if (!await TableExistsAsync(connection, "partner_panel_tercihleri", cancellationToken))
        {
            return (false, "Tercih tablosu veritabanında bulunamadı. Migration uygulanmamış olabilir.");
        }

        var lang = string.IsNullOrWhiteSpace(form.Language) ? "tr" : form.Language.Trim().ToLowerInvariant();
        var currency = string.IsNullOrWhiteSpace(form.Currency) ? "TRY" : form.Currency.Trim().ToUpperInvariant();
        var tz = string.IsNullOrWhiteSpace(form.Timezone) ? "Europe/Istanbul" : form.Timezone.Trim();
        var view = string.IsNullOrWhiteSpace(form.CalendarView) ? "Aylik" : form.CalendarView.Trim();

        // partner_id for FK
        long partnerId = 0;
        if (await TableExistsAsync(connection, "partner_detaylari", cancellationToken))
        {
            await using var pcmd = new SqlCommand("SELECT TOP (1) id FROM partner_detaylari WHERE kullanici_id=@userId ORDER BY id DESC;", connection);
            pcmd.Parameters.AddWithValue("@userId", userId);
            var obj = await pcmd.ExecuteScalarAsync(cancellationToken);
            partnerId = obj is null || obj is DBNull ? 0 : Convert.ToInt64(obj, CultureInfo.InvariantCulture);
        }

        const string sql = @"
            IF EXISTS (SELECT 1 FROM dbo.partner_panel_tercihleri WHERE kullanici_id = @userId)
            BEGIN
                UPDATE dbo.partner_panel_tercihleri
                SET varsayilan_otel_id = @defaultHotelId,
                    partner_id = @partnerId,
                    dil = @lang,
                    para_birimi = @currency,
                    zaman_dilimi = @tz,
                    takvim_gorunumu = @view,
                    email_bildirimleri = @email,
                    sms_bildirimleri = @sms,
                    push_bildirimleri = @push,
                    masaustu_bildirimleri = @desktop,
                    yeni_rezervasyon_bildirimi = @newRes,
                    iptal_bildirimi = @cancel,
                    odeme_bildirimi = @payment,
                    yorum_bildirimi = @review,
                    otomatik_fiyat_onerisi = @autoPrice,
                    otomatik_closeout_uyarisi = @autoCloseout,
                    cihaz_hatirla = @remember,
                    guncellenme_tarihi = SYSUTCDATETIME()
                WHERE kullanici_id = @userId;
            END
            ELSE
            BEGIN
                INSERT INTO dbo.partner_panel_tercihleri
                (kullanici_id, partner_id, varsayilan_otel_id, dil, para_birimi, zaman_dilimi, takvim_gorunumu,
                 email_bildirimleri, sms_bildirimleri, push_bildirimleri, masaustu_bildirimleri,
                 yeni_rezervasyon_bildirimi, iptal_bildirimi, odeme_bildirimi, yorum_bildirimi,
                 otomatik_fiyat_onerisi, otomatik_closeout_uyarisi, cihaz_hatirla, olusturulma_tarihi, guncellenme_tarihi)
                VALUES
                (@userId, @partnerId, @defaultHotelId, @lang, @currency, @tz, @view,
                 @email, @sms, @push, @desktop,
                 @newRes, @cancel, @payment, @review,
                 @autoPrice, @autoCloseout, @remember, SYSUTCDATETIME(), SYSUTCDATETIME());
            END";

        await using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@userId", userId);
        cmd.Parameters.AddWithValue("@partnerId", partnerId > 0 ? partnerId : DBNull.Value);
        cmd.Parameters.AddWithValue("@defaultHotelId", form.DefaultHotelId.HasValue && form.DefaultHotelId.Value > 0 ? form.DefaultHotelId.Value : form.HotelId);
        cmd.Parameters.AddWithValue("@lang", lang);
        cmd.Parameters.AddWithValue("@currency", currency);
        cmd.Parameters.AddWithValue("@tz", tz);
        cmd.Parameters.AddWithValue("@view", view);
        cmd.Parameters.AddWithValue("@email", form.EmailNotifications ? 1 : 0);
        cmd.Parameters.AddWithValue("@sms", form.SmsNotifications ? 1 : 0);
        cmd.Parameters.AddWithValue("@push", form.PushNotifications ? 1 : 0);
        cmd.Parameters.AddWithValue("@desktop", form.DesktopNotifications ? 1 : 0);
        cmd.Parameters.AddWithValue("@newRes", form.NewReservationNotifications ? 1 : 0);
        cmd.Parameters.AddWithValue("@cancel", form.CancellationNotifications ? 1 : 0);
        cmd.Parameters.AddWithValue("@payment", form.PaymentNotifications ? 1 : 0);
        cmd.Parameters.AddWithValue("@review", form.ReviewNotifications ? 1 : 0);
        cmd.Parameters.AddWithValue("@autoPrice", form.AutoPriceSuggestions ? 1 : 0);
        cmd.Parameters.AddWithValue("@autoCloseout", form.AutoCloseoutWarnings ? 1 : 0);
        cmd.Parameters.AddWithValue("@remember", form.RememberDevice ? 1 : 0);
        await cmd.ExecuteNonQueryAsync(cancellationToken);

        return (true, "Tercihler kaydedildi.");
    }

    public async Task<PartnerAccountInfoPageViewModel> GetAccountInfoAsync(long userId, long? hotelId = null, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        var context = await BuildContextAsync(
            connection,
            userId,
            hotelId,
            "Hesap Bilgileri",
            "Kullanıcı profili ve partner kurumsal kaydınızın veri tabanı özeti.",
            "account",
            cancellationToken);

        var model = new PartnerAccountInfoPageViewModel { Shell = context.Shell };
        model.UpdateForm.HotelId = context.SelectedHotel.HotelId;

        if (!await TableExistsAsync(connection, "users", cancellationToken))
        {
            model.UserSection.AccountStatusText = "users tablosu bulunamadı";
            return model;
        }

        const string userSql = @"
            SELECT TOP (1)
                u.id,
                COALESCE(u.ad_soyad, N''),
                COALESCE(u.eposta, N''),
                COALESCE(u.telefon, N''),
                COALESCE(u.rol, N''),
                COALESCE(u.dil_tercihi, N''),
                u.olusturulma_tarihi,
                u.son_giris_tarihi,
                u.hesap_durumu,
                u.email_dogrulama_tarihi,
                u.telefon_dogrulama_tarihi,
                u.kvkk_onay_tarihi,
                u.pazarlama_izni,
                COALESCE(u.profil_fotografi, N'')
            FROM dbo.users u
            WHERE u.id = @userId;";

        await using (var cmd = new SqlCommand(userSql, connection))
        {
            cmd.Parameters.AddWithValue("@userId", userId);
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                throw new InvalidOperationException("Kullanıcı kaydı bulunamadı.");
            }

            var reg = reader.IsDBNull(6) ? DateTime.UtcNow : reader.GetDateTime(6);
            var lastLogin = reader.IsDBNull(7) ? (DateTime?)null : reader.GetDateTime(7);
            var hesapDurumu = reader.IsDBNull(8) ? (byte?)null : Convert.ToByte(reader.GetValue(8), CultureInfo.InvariantCulture);

            model.UserSection = new PartnerAccountUserSectionViewModel
            {
                UserId = Convert.ToInt64(reader.GetValue(0), CultureInfo.InvariantCulture),
                FullName = reader.GetString(1),
                Email = reader.GetString(2),
                Phone = reader.GetString(3),
                Role = reader.GetString(4),
                Language = string.IsNullOrWhiteSpace(reader.GetString(5)) ? "—" : reader.GetString(5),
                RegistrationText = FormatDate(reg),
                LastLoginText = lastLogin.HasValue ? FormatDate(lastLogin.Value) : "—",
                AccountStatusText = MapUserAccountStatus(hesapDurumu),
                EmailVerificationText = reader.IsDBNull(9) ? "Doğrulanmadı" : FormatDate(reader.GetDateTime(9)),
                PhoneVerificationText = reader.IsDBNull(10) ? "Doğrulanmadı" : FormatDate(reader.GetDateTime(10)),
                KvkkText = reader.IsDBNull(11) ? "—" : FormatDate(reader.GetDateTime(11)),
                MarketingConsentText = reader.IsDBNull(12) ? "—" : (SafeBool(reader, 12) ? "Açık" : "Kapalı"),
                ProfilePhotoPath = string.IsNullOrWhiteSpace(reader.GetString(13)) ? null : reader.GetString(13)
            };
        }

        if (!await TableExistsAsync(connection, "partner_detaylari", cancellationToken))
        {
            return model;
        }

        const string partnerSql = @"
            SELECT TOP (1)
                p.id,
                COALESCE(p.firma_unvani, N''),
                COALESCE(p.firma_turu, N''),
                COALESCE(p.vergi_dairesi, N''),
                COALESCE(p.vergi_numarasi, N''),
                COALESCE(p.yetkili_ad_soyad, N''),
                COALESCE(p.yetkili_eposta, N''),
                COALESCE(p.yetkili_telefon, N''),
                COALESCE(p.fatura_adresi, N''),
                COALESCE(p.fatura_il, N''),
                COALESCE(p.fatura_ilce, N''),
                COALESCE(p.banka_adi, N''),
                COALESCE(p.banka_subesi, N''),
                COALESCE(p.iban, N''),
                COALESCE(p.hesap_sahibi_adi, N''),
                COALESCE(p.onay_durumu, N''),
                p.olusturulma_tarihi,
                p.onay_tarihi,
                COALESCE(p.red_nedeni, N''),
                COALESCE(p.sozlesme_no, N''),
                p.sozlesme_baslangic_tarihi,
                p.sozlesme_bitis_tarihi,
                COALESCE(p.aktif_mi, 1)
            FROM dbo.partner_detaylari p
            WHERE p.kullanici_id = @userId
            ORDER BY p.id DESC;";

        await using (var pcmd = new SqlCommand(partnerSql, connection))
        {
            pcmd.Parameters.AddWithValue("@userId", userId);
            await using var reader = await pcmd.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                return model;
            }

            var preg = reader.IsDBNull(16) ? DateTime.UtcNow : reader.GetDateTime(16);
            DateTime? appr = reader.IsDBNull(17) ? null : reader.GetDateTime(17);
            var rej = reader.IsDBNull(18) ? string.Empty : reader.GetString(18);
            var contractNo = reader.IsDBNull(19) ? string.Empty : reader.GetString(19);
            var contractStart = reader.IsDBNull(20) ? (DateTime?)null : reader.GetDateTime(20);
            var contractEnd = reader.IsDBNull(21) ? (DateTime?)null : reader.GetDateTime(21);

            model.PartnerSection = new PartnerAccountPartnerSectionViewModel
            {
                PartnerId = Convert.ToInt64(reader.GetValue(0), CultureInfo.InvariantCulture),
                CompanyName = reader.GetString(1),
                CompanyType = reader.GetString(2),
                TaxOffice = reader.GetString(3),
                TaxNumber = reader.GetString(4),
                AuthorizedName = reader.GetString(5),
                AuthorizedEmail = reader.GetString(6),
                AuthorizedPhone = reader.GetString(7),
                BillingAddress = reader.GetString(8),
                BillingCity = reader.GetString(9),
                BillingDistrict = reader.GetString(10),
                BankName = reader.GetString(11),
                BankBranch = reader.GetString(12),
                Iban = reader.GetString(13),
                AccountHolderName = reader.GetString(14),
                ApprovalStatus = reader.GetString(15),
                RegistrationText = FormatDate(preg),
                ApprovalDateText = appr.HasValue ? FormatDate(appr.Value) : null,
                RejectionReason = string.IsNullOrWhiteSpace(rej) ? null : rej,
                ContractNo = string.IsNullOrWhiteSpace(contractNo) ? null : contractNo,
                ContractStartText = contractStart.HasValue ? contractStart.Value.ToString("dd.MM.yyyy", CultureInfo.GetCultureInfo("tr-TR")) : null,
                ContractEndText = contractEnd.HasValue ? contractEnd.Value.ToString("dd.MM.yyyy", CultureInfo.GetCultureInfo("tr-TR")) : null,
                PartnerActive = SafeBool(reader, 22)
            };
        }

        if (model.PartnerSection is not null)
        {
            model.UpdateForm.CompanyName = model.PartnerSection.CompanyName;
            model.UpdateForm.CompanyType = model.PartnerSection.CompanyType;
            model.UpdateForm.TaxOffice = model.PartnerSection.TaxOffice;
            model.UpdateForm.TaxNumber = model.PartnerSection.TaxNumber;
            model.UpdateForm.AuthorizedName = model.PartnerSection.AuthorizedName;
            model.UpdateForm.AuthorizedEmail = model.PartnerSection.AuthorizedEmail;
            model.UpdateForm.AuthorizedPhone = model.PartnerSection.AuthorizedPhone;
            model.UpdateForm.BillingAddress = model.PartnerSection.BillingAddress;
            model.UpdateForm.BillingCity = model.PartnerSection.BillingCity;
            model.UpdateForm.BillingDistrict = model.PartnerSection.BillingDistrict;
            model.UpdateForm.BankName = model.PartnerSection.BankName;
            model.UpdateForm.BankBranch = model.PartnerSection.BankBranch;
            model.UpdateForm.Iban = model.PartnerSection.Iban;
            model.UpdateForm.AccountHolderName = model.PartnerSection.AccountHolderName;
        }

        return model;
    }

    public async Task<(bool Success, string Message)> SaveAccountInfoAsync(long userId, PartnerAccountInfoUpdateForm form, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        _ = await BuildContextAsync(
            connection,
            userId,
            form.HotelId,
            "Hesap Bilgileri",
            "Kullanıcı profili ve partner kurumsal kaydınızın veri tabanı özeti.",
            "account",
            cancellationToken);

        if (!await TableExistsAsync(connection, "partner_detaylari", cancellationToken))
        {
            return (false, "Partner kurumsal kaydı için veritabanı tablosu eksik (partner_detaylari).");
        }

        static string Norm(string? v) => string.IsNullOrWhiteSpace(v) ? string.Empty : v.Trim();

        var iban = Norm(form.Iban).Replace(" ", string.Empty, StringComparison.OrdinalIgnoreCase).ToUpperInvariant();
        if (!string.IsNullOrWhiteSpace(iban) && iban.Length < 14)
        {
            return (false, "IBAN çok kısa görünüyor.");
        }

        const string sql = @"
            UPDATE dbo.partner_detaylari
            SET firma_unvani = @companyName,
                firma_turu = @companyType,
                vergi_dairesi = @taxOffice,
                vergi_numarasi = @taxNumber,
                yetkili_ad_soyad = @authorizedName,
                yetkili_eposta = @authorizedEmail,
                yetkili_telefon = @authorizedPhone,
                fatura_adresi = @billingAddress,
                fatura_il = @billingCity,
                fatura_ilce = @billingDistrict,
                banka_adi = @bankName,
                banka_subesi = @bankBranch,
                iban = @iban,
                hesap_sahibi_adi = @accountHolderName
            WHERE kullanici_id = @userId;";

        await using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@userId", userId);
        cmd.Parameters.AddWithValue("@companyName", Norm(form.CompanyName));
        cmd.Parameters.AddWithValue("@companyType", Norm(form.CompanyType));
        cmd.Parameters.AddWithValue("@taxOffice", Norm(form.TaxOffice));
        cmd.Parameters.AddWithValue("@taxNumber", Norm(form.TaxNumber));
        cmd.Parameters.AddWithValue("@authorizedName", Norm(form.AuthorizedName));
        cmd.Parameters.AddWithValue("@authorizedEmail", Norm(form.AuthorizedEmail));
        cmd.Parameters.AddWithValue("@authorizedPhone", Norm(form.AuthorizedPhone));
        cmd.Parameters.AddWithValue("@billingAddress", Norm(form.BillingAddress));
        cmd.Parameters.AddWithValue("@billingCity", Norm(form.BillingCity));
        cmd.Parameters.AddWithValue("@billingDistrict", Norm(form.BillingDistrict));
        cmd.Parameters.AddWithValue("@bankName", Norm(form.BankName));
        cmd.Parameters.AddWithValue("@bankBranch", Norm(form.BankBranch));
        cmd.Parameters.AddWithValue("@iban", iban);
        cmd.Parameters.AddWithValue("@accountHolderName", Norm(form.AccountHolderName));

        var n = await cmd.ExecuteNonQueryAsync(cancellationToken);
        return n > 0 ? (true, "Kurumsal bilgiler güncellendi.") : (false, "Güncellenecek partner kaydı bulunamadı.");
    }

    private static string MapUserAccountStatus(byte? status)
    {
        if (!status.HasValue) return "—";
        return status.Value switch
        {
            1 => "Aktif",
            0 => "Pasif",
            2 => "Askıda",
            _ => status.Value.ToString(CultureInfo.InvariantCulture)
        };
    }

    private static string MaskEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return "—";
        }

        var at = email.IndexOf('@');
        if (at <= 0)
        {
            return "***";
        }

        var local = email[..at];
        var domain = email[(at + 1)..];
        var show = local.Length <= 1 ? "*" : local[..1] + "***";
        return $"{show}@{domain}";
    }

    public async Task<(bool Success, string Message)> CreateListingSubscriptionAsync(long userId, PartnerListingSubscriptionCreateRequest request, CancellationToken cancellationToken = default)
    {
        if (request.HotelId <= 0)
        {
            return (false, "Otel seçimi zorunludur.");
        }

        var scopeType = (request.ScopeType ?? string.Empty).Trim().ToUpperInvariant();
        if (scopeType is not ("IL" or "ILCE" or "MAHALLE"))
        {
            return (false, "Kapsam tipi geçersiz (IL/ILCE/MAHALLE).");
        }

        if (request.DesiredRank is < 1 or > 3)
        {
            return (false, "Hedef sıra 1-3 arasında olmalıdır.");
        }

        var dayCount = Math.Clamp(request.DayCount, 1, 30);
        var scopeValue = (request.ScopeValue ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(scopeValue))
        {
            return (false, "Kapsam değeri zorunludur.");
        }

        var normalized = SearchNormalize.Keyword(scopeValue);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return (false, "Kapsam değeri normalize edilemedi.");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await EnsureHotelAccessAsync(connection, userId, request.HotelId, cancellationToken);

        if (!await TableExistsAsync(connection, "otel_liste_abonelikleri", cancellationToken))
        {
            return (false, "Abonelik tablosu bulunamadı. Migration uygulanmamış olabilir.");
        }

        var now = DateTime.UtcNow;
        var startUtc = now;
        var endUtc = now.AddDays(dayCount);

        const string sql = @"
            INSERT INTO otel_liste_abonelikleri
            (otel_id, kapsam_tipi, kapsam_degeri, kapsam_degeri_normalized, hedef_sira, baslangic_utc, bitis_utc, durum, talep_eden_user_id, partner_notu, olusturulma_tarihi)
            VALUES
            (@hotelId, @scopeType, @scopeValue, @scopeNorm, @rank, @startUtc, @endUtc, N'Beklemede', @userId, @note, SYSUTCDATETIME());";

        await using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@hotelId", request.HotelId);
        cmd.Parameters.AddWithValue("@scopeType", scopeType);
        cmd.Parameters.AddWithValue("@scopeValue", scopeValue);
        cmd.Parameters.AddWithValue("@scopeNorm", normalized);
        cmd.Parameters.AddWithValue("@rank", request.DesiredRank);
        cmd.Parameters.AddWithValue("@startUtc", startUtc);
        cmd.Parameters.AddWithValue("@endUtc", endUtc);
        cmd.Parameters.AddWithValue("@userId", userId);
        cmd.Parameters.AddWithValue("@note", string.IsNullOrWhiteSpace(request.PartnerNote) ? (object)DBNull.Value : request.PartnerNote.Trim());
        await cmd.ExecuteNonQueryAsync(cancellationToken);

        return (true, $"Abonelik talebiniz alındı. Kapsam: {scopeValue} · Sıra: {request.DesiredRank} · Süre: {dayCount} gün");
    }

    private static DateOnly ParseMonthAnchor(string? month)
    {
        if (!string.IsNullOrWhiteSpace(month) && DateTime.TryParse(month + "-01", out var dt))
        {
            return ClampPricingMonth(new DateOnly(dt.Year, dt.Month, 1));
        }
        return ClampPricingMonth(new DateOnly(DateTime.Today.Year, DateTime.Today.Month, 1));
    }

    private static async Task EnsurePartnerHotelAccessAsync(SqlConnection connection, long userId, long hotelId, CancellationToken cancellationToken)
    {
        const string sql = "SELECT COUNT(*) FROM otel_kullanici_sahiplikleri WHERE user_id=@userId AND otel_id=@hotelId AND aktif_mi=1;";
        await using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@userId", userId);
        cmd.Parameters.AddWithValue("@hotelId", hotelId);
        var count = Convert.ToInt32(await cmd.ExecuteScalarAsync(cancellationToken) ?? 0, CultureInfo.InvariantCulture);
        if (count <= 0) throw new InvalidOperationException("Bu otel için yetkiniz yok.");
    }

    public async Task<(bool Success, string Message)> CreateSupportTicketAsync(long userId, PartnerSupportCreateTicketRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Subject) || string.IsNullOrWhiteSpace(request.Message))
        {
            return (false, "Talep konusu ve mesaj zorunludur.");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        var hotel = await EnsureHotelAccessAsync(connection, userId, request.HotelId, cancellationToken);

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            var ticketNo = $"DSTK-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}";
            const string ticketSql = @"
                INSERT INTO partner_destek_talepleri
                (partner_id, kullanici_id, otel_id, talep_no, konu, kategori, oncelik, durum, son_mesaj_tarihi)
                VALUES
                (@partnerId, @userId, @hotelId, @ticketNo, @subject, @category, @priority, 'Acik', GETDATE());
                SELECT CAST(SCOPE_IDENTITY() AS bigint);";

            long ticketId;
            await using (var ticketCommand = new SqlCommand(ticketSql, connection, (SqlTransaction)transaction))
            {
                ticketCommand.Parameters.AddWithValue("@partnerId", hotel.PartnerId);
                ticketCommand.Parameters.AddWithValue("@userId", userId);
                ticketCommand.Parameters.AddWithValue("@hotelId", hotel.HotelId);
                ticketCommand.Parameters.AddWithValue("@ticketNo", ticketNo);
                ticketCommand.Parameters.AddWithValue("@subject", request.Subject.Trim());
                ticketCommand.Parameters.AddWithValue("@category", request.Category);
                ticketCommand.Parameters.AddWithValue("@priority", request.Priority);
                ticketId = Convert.ToInt64(await ticketCommand.ExecuteScalarAsync(cancellationToken), CultureInfo.InvariantCulture);
            }

            const string messageSql = @"
                INSERT INTO partner_destek_mesajlari
                (talep_id, gonderen_kullanici_id, gonderen_tipi, mesaj, okundu_mu)
                VALUES
                (@ticketId, @userId, 'Partner', @message, 1);";

            await using (var messageCommand = new SqlCommand(messageSql, connection, (SqlTransaction)transaction))
            {
                messageCommand.Parameters.AddWithValue("@ticketId", ticketId);
                messageCommand.Parameters.AddWithValue("@userId", userId);
                messageCommand.Parameters.AddWithValue("@message", request.Message.Trim());
                await messageCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
            return (true, "Destek talebi olusturuldu.");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            return (false, $"Destek talebi acilamadi: {ex.Message}");
        }
    }

    public async Task<(bool Success, string Message)> SaveThemeAsync(long userId, long? hotelId, string scope, PanelThemeViewModel theme, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        // hotelId seçili değilse bile user bazlı kayıt yapılabilir.
        long partnerId = 0;
        if (hotelId.HasValue && hotelId.Value > 0)
        {
            var context = await BuildContextAsync(connection, userId, hotelId, "Tema", "Tema ayarları", "theme", cancellationToken);
            partnerId = context.SelectedHotel.PartnerId;
        }

        var normalizedScope = (scope ?? string.Empty).Trim().ToLowerInvariant();
        if (normalizedScope is not ("partner" or "user")) normalizedScope = "user";

        try
        {
            if (!await TableExistsAsync(connection, "tema_panel", cancellationToken))
            {
                return (false, "Tema tablosu veritabanında bulunamadı. Migration uygulanmamış olabilir.");
            }
            var normalizedTheme = new PanelThemeViewModel
            {
                BsTheme = string.IsNullOrWhiteSpace(theme.BsTheme) ? "light" : theme.BsTheme.Trim().ToLowerInvariant(),
                PrimaryHex = string.IsNullOrWhiteSpace(theme.PrimaryHex) ? null : theme.PrimaryHex.Trim(),
                AccentHex = string.IsNullOrWhiteSpace(theme.AccentHex) ? null : theme.AccentHex.Trim(),
                SidebarBgHex = string.IsNullOrWhiteSpace(theme.SidebarBgHex) ? BuildDefaultSidebarHex(theme.PrimaryHex, theme.BsTheme) : theme.SidebarBgHex.Trim(),
                RadiusScale = theme.RadiusScale,
                FontFamily = string.IsNullOrWhiteSpace(theme.FontFamily) ? null : theme.FontFamily.Trim(),
                Rtl = theme.Rtl
            };

            await UpsertThemeRecordAsync(connection, "user", userId, normalizedTheme, cancellationToken);
            if (normalizedScope == "partner" && partnerId > 0) await UpsertThemeRecordAsync(connection, "partner", partnerId, normalizedTheme, cancellationToken);

            return (true, "Tema ayarları kaydedildi.");
        }
        catch (SqlException ex) when (ex.Number == 208)
        {
            return (false, "Tema tablosu bulunamadı. Migration uygulanmamış olabilir.");
        }
        catch (Exception ex)
        {
            return (false, $"Tema kaydedilemedi: {ex.Message}");
        }
    }

    public async Task<PartnerFacilityUsersPageViewModel> GetFacilityUsersAsync(long userId, long? hotelId = null, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var context = await BuildContextAsync(connection, userId, hotelId, "Tesis Kullanıcıları", "Bu tesisi yönetecek kullanıcıları ekleyin ve sürelerini yönetin.", "facility-users", cancellationToken);
        var model = new PartnerFacilityUsersPageViewModel
        {
            Shell = context.Shell,
            HotelId = context.SelectedHotel.HotelId,
            HotelName = context.SelectedHotel.HotelName
        };
        model.InviteForm.HotelId = context.SelectedHotel.HotelId;

        if (!await TableExistsAsync(connection, "partner_tesis_kullanicilari", cancellationToken))
        {
            model.Warning = "Tesis kullanıcıları tablosu veritabanında bulunamadı. Migration uygulanmamış olabilir.";
            return model;
        }

        const string sql = @"
            SELECT TOP (200)
                ptk.id,
                ptk.kullanici_id,
                COALESCE(u.ad_soyad,'') AS ad_soyad,
                COALESCE(u.eposta,'') AS eposta,
                CASE WHEN u.email_dogrulama_tarihi IS NULL THEN 0 ELSE 1 END AS email_ok,
                COALESCE(ptk.durum,'Beklemede') AS durum,
                ptk.baslangic_tarihi,
                ptk.bitis_tarihi,
                ptk.onay_tarihi,
                ptk.davet_gonderim_tarihi
            FROM dbo.partner_tesis_kullanicilari ptk
            INNER JOIN dbo.users u ON u.id = ptk.kullanici_id
            WHERE ptk.otel_id = @hotelId
              AND ptk.aktif_mi = 1
            ORDER BY ptk.olusturulma_tarihi DESC, ptk.id DESC;";

        var tr = CultureInfo.GetCultureInfo("tr-TR");
        await using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@hotelId", context.SelectedHotel.HotelId);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            model.Users.Add(new PartnerFacilityUserRowViewModel
            {
                Id = reader.GetInt64(0),
                UserId = reader.GetInt64(1),
                FullName = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                Email = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                EmailVerified = SafeBool(reader, 4),
                Status = reader.IsDBNull(5) ? "Beklemede" : reader.GetString(5),
                StartDateText = reader.IsDBNull(6) ? null : DateOnly.FromDateTime(reader.GetDateTime(6)).ToString("dd.MM.yyyy", tr),
                EndDateText = reader.IsDBNull(7) ? null : DateOnly.FromDateTime(reader.GetDateTime(7)).ToString("dd.MM.yyyy", tr),
                ApprovedAtText = reader.IsDBNull(8) ? null : reader.GetDateTime(8).ToString("dd.MM.yyyy HH:mm", tr),
                InviteSentAtText = reader.IsDBNull(9) ? null : reader.GetDateTime(9).ToString("dd.MM.yyyy HH:mm", tr)
            });
        }

        return model;
    }

    public async Task<(bool Success, string Message)> InviteFacilityUserAsync(long userId, PartnerFacilityUserInviteRequest request, CancellationToken cancellationToken = default)
    {
        if (request.HotelId <= 0) return (false, "Otel seçilmelidir.");
        if (string.IsNullOrWhiteSpace(request.Email)) return (false, "E-posta zorunludur.");
        if (request.EndDate.HasValue && request.StartDate.HasValue && request.EndDate.Value < request.StartDate.Value)
        {
            return (false, "Bitiş tarihi başlangıçtan önce olamaz.");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        var hotel = await EnsureHotelAccessAsync(connection, userId, request.HotelId, cancellationToken);

        if (!await TableExistsAsync(connection, "partner_tesis_kullanicilari", cancellationToken))
        {
            return (false, "Tesis kullanıcıları tablosu veritabanında bulunamadı. Migration uygulanmamış olabilir.");
        }

        var email = request.Email.Trim();
        long invitedUserId = 0;
        string invitedName = "Kullanıcı";
        bool invitedEmailVerified = false;

        await using (var findCmd = new SqlCommand(@"
            SELECT TOP (1) id, COALESCE(ad_soyad,''), email_dogrulama_tarihi
            FROM dbo.users
            WHERE LOWER(eposta) = LOWER(@email);", connection))
        {
            findCmd.Parameters.AddWithValue("@email", email);
            await using var r = await findCmd.ExecuteReaderAsync(cancellationToken);
            if (await r.ReadAsync(cancellationToken))
            {
                invitedUserId = r.GetInt64(0);
                invitedName = r.IsDBNull(1) ? invitedName : r.GetString(1);
                invitedEmailVerified = !r.IsDBNull(2);
            }
        }

        if (invitedUserId <= 0)
        {
            return (false, "Bu e-posta ile kayıtlı bir kullanıcı bulunamadı. Önce üyelik oluşturulmalıdır.");
        }
        if (!invitedEmailVerified)
        {
            return (false, "Bu kullanıcı e-posta doğrulamasını tamamlamadığı için eklenemez. Önce e-posta doğrulaması tamamlanmalıdır.");
        }

        // create assignment + invite token
        var token = CreateSecureToken(32);
        var expires = DateTime.UtcNow.AddHours(48);
        var approvalLink = $"{_publicBaseUrl}/panel/partner/tesis-kullanici-onay?token={Uri.EscapeDataString(token)}";

        // inviter name
        var inviterName = await GetUserFullNameAsync(connection, userId, cancellationToken) ?? "Partner";

        await using var tx = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            // if exists active for same hotel/user -> refresh token
            const string upsertSql = @"
                IF EXISTS (SELECT 1 FROM dbo.partner_tesis_kullanicilari WHERE otel_id=@hotelId AND kullanici_id=@userId AND aktif_mi=1)
                BEGIN
                    UPDATE dbo.partner_tesis_kullanicilari
                    SET durum = N'Beklemede',
                        baslangic_tarihi = @startDate,
                        bitis_tarihi = @endDate,
                        davet_token = @token,
                        davet_gonderim_tarihi = SYSUTCDATETIME(),
                        davet_son_gecerlilik = @expires,
                        onay_tarihi = NULL,
                        guncellenme_tarihi = SYSUTCDATETIME()
                    WHERE otel_id=@hotelId AND kullanici_id=@userId AND aktif_mi=1;
                END
                ELSE
                BEGIN
                    INSERT INTO dbo.partner_tesis_kullanicilari
                    (otel_id, kullanici_id, durum, baslangic_tarihi, bitis_tarihi, davet_token, davet_gonderim_tarihi, davet_son_gecerlilik, ekleyen_kullanici_id, aktif_mi, olusturulma_tarihi)
                    VALUES
                    (@hotelId, @userId, N'Beklemede', @startDate, @endDate, @token, SYSUTCDATETIME(), @expires, @addedBy, 1, SYSUTCDATETIME());
                END";
            await using (var cmd = new SqlCommand(upsertSql, connection, (SqlTransaction)tx))
            {
                cmd.Parameters.AddWithValue("@hotelId", hotel.HotelId);
                cmd.Parameters.AddWithValue("@userId", invitedUserId);
                cmd.Parameters.AddWithValue("@token", token);
                cmd.Parameters.AddWithValue("@expires", expires);
                cmd.Parameters.AddWithValue("@addedBy", userId);
                cmd.Parameters.AddWithValue("@startDate", request.StartDate.HasValue ? request.StartDate.Value.ToDateTime(TimeOnly.MinValue) : (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@endDate", request.EndDate.HasValue ? request.EndDate.Value.ToDateTime(TimeOnly.MinValue) : (object)DBNull.Value);
                await cmd.ExecuteNonQueryAsync(cancellationToken);
            }

            var tr = CultureInfo.GetCultureInfo("tr-TR");
            await _emailQueueService.QueueTemplateAsync(
                connection,
                (SqlTransaction)tx,
                new QueuedEmailTemplateRequest
                {
                    UserId = invitedUserId,
                    RecipientEmail = email,
                    TemplateCode = "partner_facility_user_invite",
                    RelatedTable = "partner_tesis_kullanicilari",
                    RelatedRecordId = null,
                    Tokens = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["invited_full_name"] = invitedName,
                        ["invited_email"] = email,
                        ["hotel_name"] = hotel.HotelName,
                        ["inviter_name"] = inviterName,
                        ["approval_link"] = approvalLink,
                        ["start_date"] = request.StartDate.HasValue ? request.StartDate.Value.ToString("dd.MM.yyyy", tr) : "",
                        ["end_date"] = request.EndDate.HasValue ? request.EndDate.Value.ToString("dd.MM.yyyy", tr) : ""
                    }
                },
                cancellationToken);

            await tx.CommitAsync(cancellationToken);
            return (true, "Davet e-postası gönderildi. Kullanıcı onayladıktan sonra erişim aktif olur.");
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(cancellationToken);
            return (false, "Davet gönderilemedi: " + ex.Message);
        }
    }

    public async Task<(bool Success, string Message)> RevokeFacilityUserAsync(long userId, PartnerFacilityUserRevokeRequest request, CancellationToken cancellationToken = default)
    {
        if (request.HotelId <= 0 || request.AssignmentId <= 0) return (false, "Geçersiz istek.");

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await EnsurePartnerHotelAccessAsync(connection, userId, request.HotelId, cancellationToken);

        if (!await TableExistsAsync(connection, "partner_tesis_kullanicilari", cancellationToken))
        {
            return (false, "Tesis kullanıcıları tablosu veritabanında bulunamadı.");
        }

        const string sql = @"
            UPDATE dbo.partner_tesis_kullanicilari
            SET aktif_mi = 0,
                durum = N'Iptal',
                iptal_eden_kullanici_id = @revoker,
                iptal_tarihi = SYSUTCDATETIME(),
                iptal_nedeni = @reason,
                guncellenme_tarihi = SYSUTCDATETIME()
            WHERE id = @id AND otel_id = @hotelId;";
        await using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@id", request.AssignmentId);
        cmd.Parameters.AddWithValue("@hotelId", request.HotelId);
        cmd.Parameters.AddWithValue("@revoker", userId);
        cmd.Parameters.AddWithValue("@reason", string.IsNullOrWhiteSpace(request.Reason) ? DBNull.Value : request.Reason.Trim());
        var affected = await cmd.ExecuteNonQueryAsync(cancellationToken);
        return affected > 0 ? (true, "Tesis kullanıcı erişimi iptal edildi.") : (false, "Kayıt bulunamadı.");
    }

    public async Task<(bool Success, string Message)> ApproveFacilityInviteAsync(long userId, string token, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token)) return (false, "Geçersiz token.");

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        if (!await TableExistsAsync(connection, "partner_tesis_kullanicilari", cancellationToken))
        {
            return (false, "Tesis kullanıcıları tablosu veritabanında bulunamadı.");
        }

        // email must be verified
        var emailVerified = false;
        await using (var cmd = new SqlCommand("SELECT TOP (1) email_dogrulama_tarihi FROM dbo.users WHERE id=@id;", connection))
        {
            cmd.Parameters.AddWithValue("@id", userId);
            var obj = await cmd.ExecuteScalarAsync(cancellationToken);
            emailVerified = obj is not null && obj is not DBNull;
        }
        if (!emailVerified)
        {
            return (false, "E-posta doğrulamanız tamamlanmadan davet onaylanamaz.");
        }

        const string sql = @"
            UPDATE dbo.partner_tesis_kullanicilari
            SET durum = N'Aktif',
                onay_tarihi = SYSUTCDATETIME(),
                davet_token = NULL,
                davet_son_gecerlilik = NULL,
                guncellenme_tarihi = SYSUTCDATETIME()
            WHERE kullanici_id = @userId
              AND davet_token = @token
              AND aktif_mi = 1
              AND (davet_son_gecerlilik IS NULL OR davet_son_gecerlilik > SYSUTCDATETIME());";

        await using var update = new SqlCommand(sql, connection);
        update.Parameters.AddWithValue("@userId", userId);
        update.Parameters.AddWithValue("@token", token.Trim());
        var affected = await update.ExecuteNonQueryAsync(cancellationToken);
        return affected > 0 ? (true, "Davet onaylandı. Artık tesis panelini yönetebilirsiniz.") : (false, "Davet bulunamadı veya süresi dolmuş.");
    }

    private static string CreateSecureToken(int byteLength)
    {
        var buffer = RandomNumberGenerator.GetBytes(byteLength);
        return Convert.ToHexString(buffer).ToLowerInvariant();
    }

    private static async Task<string?> GetUserFullNameAsync(SqlConnection connection, long userId, CancellationToken cancellationToken)
    {
        await using var cmd = new SqlCommand("SELECT TOP (1) COALESCE(ad_soyad,'') FROM dbo.users WHERE id=@id;", connection);
        cmd.Parameters.AddWithValue("@id", userId);
        var obj = await cmd.ExecuteScalarAsync(cancellationToken);
        var s = obj?.ToString();
        return string.IsNullOrWhiteSpace(s) ? null : s;
    }

    private async Task<PartnerContext> BuildContextAsync(SqlConnection connection, long userId, long? hotelId, string title, string subtitle, string activeSectionKey, CancellationToken cancellationToken)
    {
        await ReopenExpiredPenaltyHotelsAsync(connection, cancellationToken);
        var hotels = await GetManagedHotelsAsync(connection, userId, cancellationToken);
        if (hotels.Count == 0)
        {
            // Partner paneli: bazı kullanıcılar (özellikle demo / kurulum aşaması) için henüz otel yetkisi tanımlı olmayabilir.
            // Bu durumda sistemin patlaması yerine mümkünse demo oteli otomatik ilişkilendir, değilse controller tarafında uyarı ekranı gösterilecek.
            await TryAutoAssignDefaultHotelForKnownUsersAsync(connection, userId, cancellationToken);
            hotels = await GetManagedHotelsAsync(connection, userId, cancellationToken);
            if (hotels.Count == 0)
            {
                throw new InvalidOperationException("Bu kullanici icin yetkili otel bulunamadi.");
            }
        }

        var selectedHotel = hotelId.HasValue
            ? hotels.FirstOrDefault(item => item.HotelId == hotelId.Value) ?? hotels[0]
            : hotels[0];

        var shell = await BuildShellAsync(connection, userId, selectedHotel, hotels, title, subtitle, activeSectionKey, cancellationToken);
        return new PartnerContext(selectedHotel, shell);
    }

    private static async Task TryAutoAssignDefaultHotelForKnownUsersAsync(SqlConnection connection, long userId, CancellationToken cancellationToken)
    {
        if (!await TableExistsAsync(connection, "users", cancellationToken)
            || !await TableExistsAsync(connection, "oteller", cancellationToken)
            || !await TableExistsAsync(connection, "otel_kullanici_sahiplikleri", cancellationToken))
        {
            return;
        }

        const string emailSql = "SELECT TOP (1) eposta FROM users WHERE id = @userId;";
        string email = string.Empty;
        await using (var emailCommand = new SqlCommand(emailSql, connection))
        {
            emailCommand.CommandTimeout = 30;
            emailCommand.Parameters.AddWithValue("@userId", userId);
            var emailObj = await emailCommand.ExecuteScalarAsync(cancellationToken);
            email = emailObj?.ToString() ?? string.Empty;
        }

        // İstek: kurumsal@otelturizm.com için bir otel tanımla.
        if (!string.Equals(email?.Trim(), "kurumsal@otelturizm.com", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        // İstek: 216-eagle-palace otelini bu e-postaya bağla.
        // Partner panel, otel_kullanici_sahiplikleri üzerinden çalışıyor. PartnerId zorunlu olduğu için partner_id NULL olmayan bir oteli seçiyoruz.
        const string pickHotelSql = @"
            SELECT TOP (1) o.id, o.partner_id
            FROM oteller o
            WHERE o.partner_id IS NOT NULL
              AND (
                  o.otel_kodu = @hotelCode
                  OR REPLACE(REPLACE(LOWER(o.otel_kodu), '_', '-'), ' ', '-') = @hotelCodeSlug
                  OR LOWER(o.otel_adi) = @hotelName
                  OR LOWER(o.eposta) = @email
                  OR (@hotelCode IS NULL)
              )
            ORDER BY
                CASE
                    WHEN o.otel_kodu = @hotelCode THEN 0
                    WHEN REPLACE(REPLACE(LOWER(o.otel_kodu), '_', '-'), ' ', '-') = @hotelCodeSlug THEN 1
                    WHEN LOWER(o.otel_adi) = @hotelName THEN 2
                    WHEN LOWER(o.eposta) = @email THEN 3
                    ELSE 4
                END,
                o.id ASC;";

        long hotelId = 0;
        long partnerId = 0;
        await using (var pickCommand = new SqlCommand(pickHotelSql, connection))
        {
            pickCommand.CommandTimeout = 30;
            pickCommand.Parameters.AddWithValue("@hotelCode", "216-eagle-palace");
            pickCommand.Parameters.AddWithValue("@hotelCodeSlug", "216-eagle-palace");
            pickCommand.Parameters.AddWithValue("@hotelName", "216 eagle palace");
            pickCommand.Parameters.AddWithValue("@email", "kurumsal@otelturizm.com");
            await using var reader = await pickCommand.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                hotelId = Convert.ToInt64(reader.GetValue(0), CultureInfo.InvariantCulture);
                partnerId = Convert.ToInt64(reader.GetValue(1), CultureInfo.InvariantCulture);
            }
        }

        if (hotelId <= 0 || partnerId <= 0)
        {
            return;
        }

        // Zaten tanımlıysa tekrar ekleme.
        const string existsSql = @"
            SELECT COUNT(*)
            FROM otel_kullanici_sahiplikleri
            WHERE otel_id = @hotelId AND user_id = @userId AND aktif_mi = 1;";
        await using (var existsCommand = new SqlCommand(existsSql, connection))
        {
            existsCommand.CommandTimeout = 30;
            existsCommand.Parameters.AddWithValue("@hotelId", hotelId);
            existsCommand.Parameters.AddWithValue("@userId", userId);
            var exists = Convert.ToInt32(await existsCommand.ExecuteScalarAsync(cancellationToken) ?? 0) > 0;
            if (exists) return;
        }

        const string insertSql = @"
            INSERT INTO otel_kullanici_sahiplikleri
            (otel_id, user_id, partner_id, rol, ana_sorumlu_mu, aktif_mi, olusturulma_tarihi)
            VALUES
            (@hotelId, @userId, @partnerId, 'owner', 1, 1, SYSUTCDATETIME());";

        await using var insertCommand = new SqlCommand(insertSql, connection);
        insertCommand.CommandTimeout = 30;
        insertCommand.Parameters.AddWithValue("@hotelId", hotelId);
        insertCommand.Parameters.AddWithValue("@userId", userId);
        insertCommand.Parameters.AddWithValue("@partnerId", partnerId);
        await insertCommand.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task<PartnerShellViewModel> BuildShellAsync(SqlConnection connection, long userId, PartnerHotelContext selectedHotel, IReadOnlyList<PartnerHotelContext> hotels, string title, string subtitle, string activeSectionKey, CancellationToken cancellationToken)
    {
        const string userSql = "SELECT TOP (1) ad_soyad, eposta, rol FROM users WHERE id = @userId;";
        string fullName = "Partner Kullanici";
        string email = string.Empty;
        string role = "partner_staff";

        await using (var userCommand = new SqlCommand(userSql, connection))
        {
            userCommand.CommandTimeout = 30;
            userCommand.Parameters.AddWithValue("@userId", userId);
            await using var reader = await userCommand.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                fullName = reader.IsDBNull(0) ? fullName : reader.GetString(0);
                email = reader.IsDBNull(1) ? email : reader.GetString(1);
                role = reader.IsDBNull(2) ? role : reader.GetString(2);
            }
        }

        var pendingReservations = 0;
        var openSupportTickets = 0;
        var lowStockAlerts = 0;
        var unansweredReviews = 0;
        var favoriteCount = 0;

        const string shellMetricsSql = @"
            SELECT
                (SELECT COUNT(*) FROM rezervasyonlar r WHERE r.otel_id = @hotelId AND r.durum IN ('Onay Bekliyor','Değişiklik Bekliyor')) AS pending_reservations,
                (SELECT COUNT(*) FROM partner_destek_talepleri pdt WHERE pdt.partner_id = @partnerId AND pdt.durum IN ('Acik','Partner Yaniti Bekleniyor','Inceleniyor')) AS open_support_tickets,
                (SELECT COUNT(*)
                 FROM oda_fiyat_musaitlik ofm
                 INNER JOIN oda_tipleri ot ON ot.id = ofm.oda_tip_id
                 WHERE ot.otel_id = @hotelId
                   AND ofm.otel_id = @hotelId
                   AND ofm.tarih BETWEEN CAST(GETDATE() AS date) AND DATEADD(DAY, 14, CAST(GETDATE() AS date))
                   AND ((ofm.toplam_oda_sayisi - ofm.satilan_oda_sayisi - ofm.bloke_oda_sayisi) <= 2 OR ofm.kapali_satis = 1)) AS low_stock_alerts,
                (SELECT COUNT(*) FROM yorumlar y WHERE y.otel_id = @hotelId AND COALESCE(y.otel_yaniti, '') = '') AS unanswered_reviews;";

        await using (var shellCommand = new SqlCommand(shellMetricsSql, connection))
        {
            shellCommand.CommandTimeout = 60;
            shellCommand.Parameters.AddWithValue("@hotelId", selectedHotel.HotelId);
            shellCommand.Parameters.AddWithValue("@partnerId", selectedHotel.PartnerId);
            await using var reader = await shellCommand.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                pendingReservations = SafeInt(reader, 0);
                openSupportTickets = SafeInt(reader, 1);
                lowStockAlerts = SafeInt(reader, 2);
                unansweredReviews = SafeInt(reader, 3);
            }
        }

        // Partner panelindeki favori sayacı kalıcı olarak doğru olsun diye,
        // oteller.favori_sayisi gibi "cache" alanlara bağlı kalmayıp gerçek kaynaktan hesaplıyoruz.
        const string favoriteCountSql = @"
            SELECT COUNT(DISTINCT uf.user_id)
            FROM user_favori_oteller uf
            WHERE uf.otel_id = @hotelId
              AND COALESCE(uf.aktif_mi, 1) = 1
              AND uf.kaldirilma_tarihi IS NULL;";
        await using (var favoriteCommand = new SqlCommand(favoriteCountSql, connection))
        {
            favoriteCommand.Parameters.AddWithValue("@hotelId", selectedHotel.HotelId);
            var rawFavorite = await favoriteCommand.ExecuteScalarAsync(cancellationToken);
            favoriteCount = rawFavorite is null || rawFavorite == DBNull.Value ? 0 : Convert.ToInt32(rawFavorite, CultureInfo.InvariantCulture);
        }

        // Eğer DB'de favori_sayisi kolonu varsa, uyumlu kalsın diye (sessizce) senkronize et.
        if (await ColumnExistsAsync(connection, "oteller", "favori_sayisi", cancellationToken))
        {
            await using var syncCommand = new SqlCommand("UPDATE oteller SET favori_sayisi = @count WHERE id = @hotelId;", connection);
            syncCommand.Parameters.AddWithValue("@count", favoriteCount);
            syncCommand.Parameters.AddWithValue("@hotelId", selectedHotel.HotelId);
            await syncCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        var shell = new PartnerShellViewModel
        {
            UserId = userId,
            PartnerId = selectedHotel.PartnerId,
            FullName = fullName,
            Email = email,
            UserRole = role,
            PanelTitle = title,
            PanelSubtitle = subtitle,
            ActiveSectionKey = activeSectionKey,
            SelectedHotelId = selectedHotel.HotelId,
            SelectedHotelName = selectedHotel.HotelName,
            PendingReservations = pendingReservations,
            OpenSupportTickets = openSupportTickets,
            LowStockAlerts = lowStockAlerts,
            UnansweredReviews = unansweredReviews,
            FavoriteCount = favoriteCount,
            FavoriteSummaryText = $"{favoriteCount.ToString(CultureInfo.InvariantCulture)} kişi sizi favorisine ekledi.",
            ManagedHotels = hotels.Select(item => new PartnerHotelSwitchItemViewModel
            {
                HotelId = item.HotelId,
                HotelCode = item.HotelCode,
                HotelName = item.HotelName,
                CityLabel = item.CityLabel,
                IsPrimary = item.IsPrimary,
                IsSelected = item.HotelId == selectedHotel.HotelId
            }).ToList(),
            Theme = await LoadPanelThemeAsync(connection, userId, selectedHotel.PartnerId, cancellationToken)
        };

        return shell;
    }

    private static async Task<PanelThemeViewModel> LoadPanelThemeAsync(SqlConnection connection, long userId, long partnerId, CancellationToken cancellationToken)
    {
        // Fallback sırası:
        // 1) kullanıcı teması (hedef_tur='user', hedef_id=userId)
        // 2) partner teması (hedef_tur='partner', hedef_id=partnerId)
        const string sql = @"
            SELECT TOP (1)
                bs_theme,
                primary_hex,
                accent_hex,
                sidebar_bg_hex,
                radius_scale,
                density,
                font_family,
                layout_mode,
                rtl
            FROM dbo.tema_panel
            WHERE aktif_mi = 1
              AND (
                    (hedef_tur = N'user' AND hedef_id = @userId)
                    OR (hedef_tur = N'partner' AND hedef_id = @partnerId)
                  )
            ORDER BY CASE WHEN hedef_tur = N'user' THEN 0 ELSE 1 END, guncellenme_tarihi DESC, id DESC;";

        try
        {
            await using var command = new SqlCommand(sql, connection);
            command.CommandTimeout = 30;
            command.Parameters.AddWithValue("@partnerId", partnerId);
            command.Parameters.AddWithValue("@userId", userId);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                return new PanelThemeViewModel
                {
                    BsTheme = reader.IsDBNull(0) ? "light" : reader.GetString(0),
                    PrimaryHex = reader.IsDBNull(1) ? null : reader.GetString(1),
                    AccentHex = reader.IsDBNull(2) ? null : reader.GetString(2),
                    SidebarBgHex = reader.IsDBNull(3) ? null : reader.GetString(3),
                    RadiusScale = reader.IsDBNull(4) ? null : reader.GetDecimal(4),
                    Density = reader.IsDBNull(5) ? null : reader.GetString(5),
                    FontFamily = reader.IsDBNull(6) ? null : reader.GetString(6),
                    LayoutMode = reader.IsDBNull(7) ? null : reader.GetString(7),
                    Rtl = !reader.IsDBNull(8) && reader.GetBoolean(8)
                };
            }
        }
        catch (SqlException ex) when (ex.Number == 208) // Invalid object name (tema_panel yok)
        {
            // Migration henüz uygulanmadıysa sessiz fallback.
        }

        return new PanelThemeViewModel { BsTheme = "light" };
    }

    private static async Task UpsertThemeRecordAsync(SqlConnection connection, string targetType, long targetId, PanelThemeViewModel theme, CancellationToken cancellationToken)
    {
        const string sql = @"
            SET QUOTED_IDENTIFIER ON;
            SET ANSI_NULLS ON;
            IF EXISTS (SELECT 1 FROM dbo.tema_panel WHERE hedef_tur = @targetType AND hedef_id = @targetId)
            BEGIN
                UPDATE dbo.tema_panel
                SET bs_theme = @bsTheme,
                    primary_hex = NULLIF(@primaryHex, ''),
                    accent_hex = NULLIF(@accentHex, ''),
                    sidebar_bg_hex = NULLIF(@sidebarBgHex, ''),
                    radius_scale = @radiusScale,
                    font_family = NULLIF(@fontFamily, ''),
                    rtl = @rtl,
                    aktif_mi = 1,
                    guncellenme_tarihi = SYSUTCDATETIME()
                WHERE hedef_tur = @targetType AND hedef_id = @targetId;
            END
            ELSE
            BEGIN
                INSERT INTO dbo.tema_panel
                (hedef_tur, hedef_id, bs_theme, primary_hex, accent_hex, sidebar_bg_hex, radius_scale, font_family, rtl, aktif_mi, olusturulma_tarihi, guncellenme_tarihi)
                VALUES
                (@targetType, @targetId, @bsTheme, NULLIF(@primaryHex, ''), NULLIF(@accentHex, ''), NULLIF(@sidebarBgHex, ''), @radiusScale, NULLIF(@fontFamily, ''), @rtl, 1, SYSUTCDATETIME(), SYSUTCDATETIME());
            END;";

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@targetType", targetType);
        command.Parameters.AddWithValue("@targetId", targetId);
        command.Parameters.AddWithValue("@bsTheme", theme.BsTheme);
        command.Parameters.AddWithValue("@primaryHex", (object?)theme.PrimaryHex ?? string.Empty);
        command.Parameters.AddWithValue("@accentHex", (object?)theme.AccentHex ?? string.Empty);
        command.Parameters.AddWithValue("@sidebarBgHex", (object?)theme.SidebarBgHex ?? string.Empty);
        command.Parameters.AddWithValue("@radiusScale", theme.RadiusScale.HasValue ? theme.RadiusScale.Value : (object)DBNull.Value);
        command.Parameters.AddWithValue("@fontFamily", (object?)theme.FontFamily ?? string.Empty);
        command.Parameters.AddWithValue("@rtl", theme.Rtl ? 1 : 0);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static string BuildDefaultSidebarHex(string? primaryHex, string? bsTheme)
    {
        if (string.Equals(bsTheme?.Trim(), "dark", StringComparison.OrdinalIgnoreCase))
        {
            return "#0f1d36";
        }

        return string.IsNullOrWhiteSpace(primaryHex) ? "#16325c" : primaryHex.Trim();
    }

    private static async Task ReopenExpiredPenaltyHotelsAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        const string sql = @"
            UPDATE oteller
            SET yayin_durumu = 'Yayında',
                partner_ceza_bitis_tarihi = NULL
            WHERE partner_ceza_bitis_tarihi IS NOT NULL
              AND partner_ceza_bitis_tarihi <= GETDATE()
              AND yayin_durumu = 'Kapatıldı';";
        try
        {
            await using var command = new SqlCommand(sql, connection);
            command.CommandTimeout = 30;
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (SqlException ex) when (IsUnknownColumnError(ex, "partner_ceza_bitis_tarihi"))
        {
            await EnsurePartnerPenaltyColumnAsync(connection, cancellationToken);
            await using var retryCommand = new SqlCommand(sql, connection);
            retryCommand.CommandTimeout = 30;
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
            WHERE TABLE_NAME = 'oteller'
              AND COLUMN_NAME = 'partner_ceza_bitis_tarihi';";
        await using var existsCommand = new SqlCommand(existsSql, connection);
        var exists = Convert.ToInt32(await existsCommand.ExecuteScalarAsync(cancellationToken) ?? 0) > 0;
        if (!exists)
        {
            const string alterSql = "ALTER TABLE oteller ADD partner_ceza_bitis_tarihi DATETIME NULL;";
            await using var alterCommand = new SqlCommand(alterSql, connection);
            await alterCommand.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    private async Task<List<PartnerHotelContext>> GetManagedHotelsAsync(SqlConnection connection, long userId, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT DISTINCT o.id, o.partner_id, o.otel_kodu, o.otel_adi, CONCAT(o.ilce, ', ', o.sehir) AS city_label, oks.ana_sorumlu_mu
            FROM otel_kullanici_sahiplikleri oks
            INNER JOIN oteller o ON o.id = oks.otel_id
            WHERE oks.user_id = @userId AND oks.aktif_mi = 1
            ORDER BY oks.ana_sorumlu_mu DESC, o.id ASC;";

        var hotels = new List<PartnerHotelContext>();
        await using var command = new SqlCommand(sql, connection);
        command.CommandTimeout = 30;
        command.Parameters.AddWithValue("@userId", userId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            hotels.Add(new PartnerHotelContext(
                Convert.ToInt64(reader.GetValue(0), CultureInfo.InvariantCulture),
                Convert.ToInt64(reader.GetValue(1), CultureInfo.InvariantCulture),
                reader.GetString(2),
                reader.GetString(3),
                "Otel",
                reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                SafeBool(reader, 5)));
        }

        return hotels;
    }

    private async Task<PartnerHotelContext> EnsureHotelAccessAsync(SqlConnection connection, long userId, long hotelId, CancellationToken cancellationToken)
    {
        var hotels = await GetManagedHotelsAsync(connection, userId, cancellationToken);
        var hotel = hotels.FirstOrDefault(item => item.HotelId == hotelId);
        if (hotel is null)
        {
            throw new InvalidOperationException("Bu otel icin yetkiniz bulunmuyor.");
        }

        return hotel;
    }

    private readonly record struct InclusiveTaxPercents(decimal VatPercent, decimal AccommodationPercent);

    private async Task<InclusiveTaxPercents> LoadInclusiveTaxPercentsAsync(SqlConnection connection, long hotelId, CancellationToken cancellationToken)
    {
        if (!await TableExistsAsync(connection, "komisyon_vergiler", cancellationToken))
        {
            return new InclusiveTaxPercents(10m, 2m);
        }

        const string sql = @"
            SELECT TOP (1)
                COALESCE(kv.kdv_orani, 10),
                COALESCE(kv.konaklama_vergisi_orani, 2)
            FROM komisyon_vergiler kv
            WHERE kv.otel_id = @hotelId
              AND kv.aktif_mi = 1
              AND kv.baslangic_tarihi <= @effectiveDate
              AND (kv.bitis_tarihi IS NULL OR kv.bitis_tarihi >= @effectiveDate)
            ORDER BY kv.baslangic_tarihi DESC, kv.id DESC;";

        var effectiveDate = DateOnly.FromDateTime(DateTime.Today).ToDateTime(TimeOnly.MinValue);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@hotelId", hotelId);
        command.Parameters.AddWithValue("@effectiveDate", effectiveDate);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            return new InclusiveTaxPercents(reader.GetDecimal(0), reader.GetDecimal(1));
        }

        return new InclusiveTaxPercents(10m, 2m);
    }

    private async Task<List<PartnerRoomSummaryViewModel>> GetRoomSummariesAsync(SqlConnection connection, long hotelId, InclusiveTaxPercents tax, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT ot.id,
                   ot.oda_adi,
                   ot.oda_kategorisi,
                   ot.maksimum_yetiskin_sayisi,
                   ot.maksimum_cocuk_sayisi,
                   ot.toplam_oda_sayisi,
                   ot.standart_gecelik_fiyat,
                   ot.kapak_fotografi,
                   ot.aktif_mi,
                   MIN(ofm.indirimli_fiyat) AS min_discount_price
            FROM oda_tipleri ot
            LEFT JOIN oda_fiyat_musaitlik ofm ON ofm.oda_tip_id = ot.id
                AND ofm.otel_id = ot.otel_id
                AND ofm.tarih BETWEEN CAST(GETDATE() AS date) AND DATEADD(DAY, 60, CAST(GETDATE() AS date))
                AND ofm.indirimli_fiyat IS NOT NULL
            WHERE ot.otel_id = @hotelId
            GROUP BY ot.id, ot.oda_adi, ot.oda_kategorisi, ot.maksimum_yetiskin_sayisi, ot.maksimum_cocuk_sayisi, ot.toplam_oda_sayisi, ot.standart_gecelik_fiyat, ot.kapak_fotografi, ot.aktif_mi, ot.siralama
            ORDER BY ot.aktif_mi DESC, ot.siralama ASC, ot.id ASC;";

        var rooms = new List<PartnerRoomSummaryViewModel>();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@hotelId", hotelId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var storedNet = SafeDecimal(reader, 6);
            var discountNet = reader.IsDBNull(9) ? (decimal?)null : reader.GetDecimal(9);
            rooms.Add(new PartnerRoomSummaryViewModel
            {
                RoomId = reader.GetInt64(0),
                StandardNightlyStoredNet = storedNet,
                RoomName = reader.GetString(1),
                Category = reader.GetString(2),
                CapacityText = $"{SafeInt(reader, 3)} yetiskin / {SafeInt(reader, 4)} cocuk",
                StockText = $"{SafeInt(reader, 5)} oda",
                BasePriceText = FormatMoney(InclusiveNightlyPricing.StoredNetToPartnerDisplay(storedNet, tax.VatPercent, tax.AccommodationPercent)),
                DiscountPriceText = discountNet.HasValue
                    ? FormatMoney(InclusiveNightlyPricing.StoredNetToPartnerDisplay(discountNet.Value, tax.VatPercent, tax.AccommodationPercent))
                    : "-",
                CoverPhotoUrl = reader.IsDBNull(7) ? null : reader.GetString(7),
                IsActive = SafeBool(reader, 8)
            });
        }

        return rooms;
    }

    private async Task<Dictionary<(long RoomId, DateOnly Date), PricingCalendarEntry>> LoadPricingMonthEntriesAsync(SqlConnection connection, long hotelId, DateOnly monthStart, DateOnly monthEnd, CancellationToken cancellationToken)
    {
        var hasDiscountIdColumn = await ColumnExistsAsync(connection, "oda_fiyat_musaitlik", "indirim_id", cancellationToken);
        var discountIdSelect = hasDiscountIdColumn ? "ofm.indirim_id" : "ofm.kampanya_id";
        var campaignIdSelect = hasDiscountIdColumn ? "ofm.kampanya_id" : "CAST(NULL AS bigint)";

        var sql = $@"
            SELECT ofm.oda_tip_id,
                   ofm.tarih,
                   ofm.gecelik_fiyat,
                   ofm.indirimli_fiyat,
                   {discountIdSelect} AS indirim_id,
                   {campaignIdSelect} AS kampanya_id,
                   ofm.toplam_oda_sayisi,
                   ofm.satilan_oda_sayisi,
                   ofm.bloke_oda_sayisi,
                   ofm.minimum_geceleme,
                   ofm.maksimum_geceleme,
                   ofm.kapali_satis,
                   ofm.kampanya_etiketi,
                   ofm.fiyat_notu
            FROM oda_fiyat_musaitlik ofm
            INNER JOIN oda_tipleri ot ON ot.id = ofm.oda_tip_id
            WHERE ot.otel_id = @hotelId
              AND ofm.otel_id = @hotelId
              AND ofm.tarih BETWEEN @startDate AND @endDate;";

        var entries = new Dictionary<(long RoomId, DateOnly Date), PricingCalendarEntry>();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@hotelId", hotelId);
        command.Parameters.AddWithValue("@startDate", monthStart.ToDateTime(TimeOnly.MinValue));
        command.Parameters.AddWithValue("@endDate", monthEnd.ToDateTime(TimeOnly.MinValue));
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var date = DateOnly.FromDateTime(reader.GetDateTime(1));
            entries[(reader.GetInt64(0), date)] = new PricingCalendarEntry(
                reader.GetInt64(0),
                date,
                SafeDecimal(reader, 2),
                reader.IsDBNull(3) ? null : reader.GetDecimal(3),
                reader.IsDBNull(4) ? null : reader.GetInt64(4),
                reader.IsDBNull(5) ? null : reader.GetInt64(5),
                reader.IsDBNull(6) ? null : SafeShort(reader, 6),
                reader.IsDBNull(7) ? (short)0 : SafeShort(reader, 7),
                reader.IsDBNull(8) ? (short)0 : SafeShort(reader, 8),
                reader.IsDBNull(9) ? null : SafeByte(reader, 9),
                reader.IsDBNull(10) ? null : SafeShort(reader, 10),
                SafeBool(reader, 11),
                reader.IsDBNull(12) ? null : NormalizeTurkishText(reader.GetString(12)),
                reader.IsDBNull(13) ? null : NormalizeTurkishText(reader.GetString(13)));
        }

        return entries;
    }

    private static async Task EnrichRoomSummariesForPricingMonthAsync(
        SqlConnection connection,
        long hotelId,
        IReadOnlyList<PartnerRoomSummaryViewModel> rooms,
        IReadOnlyDictionary<(long RoomId, DateOnly Date), PricingCalendarEntry> pricingEntries,
        DateOnly monthStart,
        DateOnly monthEnd,
        CancellationToken cancellationToken)
    {
        var tones = new[] { "tone-1", "tone-2", "tone-3", "tone-4", "tone-5", "tone-6" };
        for (var i = 0; i < rooms.Count; i++)
        {
            rooms[i].ToneClass = tones[i % tones.Length];
            rooms[i].MonthlyDiscountDayCount = pricingEntries.Values.Count(item =>
                item.RoomId == rooms[i].RoomId
                && item.DiscountPrice.HasValue
                && item.DiscountPrice.Value > 0m
                && item.DiscountPrice.Value < item.BasePrice);
        }

        if (rooms.Count == 0)
        {
            return;
        }

        const string sql = @"
            SELECT r.oda_tip_id, COUNT(*) AS reservation_count
            FROM rezervasyonlar r
            WHERE r.otel_id = @hotelId
              AND r.oda_tip_id IS NOT NULL
              AND CAST(r.giris_tarihi AS date) >= @monthStart
              AND CAST(r.giris_tarihi AS date) <= @monthEnd
              AND r.durum <> N'İptal Edildi'
            GROUP BY r.oda_tip_id;";

        var map = rooms.ToDictionary(static item => item.RoomId);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@hotelId", hotelId);
        command.Parameters.AddWithValue("@monthStart", monthStart.ToDateTime(TimeOnly.MinValue));
        command.Parameters.AddWithValue("@monthEnd", monthEnd.ToDateTime(TimeOnly.MinValue));
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var roomId = reader.GetInt64(0);
            if (map.TryGetValue(roomId, out var room))
            {
                room.MonthlyReservationCount = SafeInt(reader, 1);
            }
        }
    }

    private async Task<Dictionary<(long RoomId, DateOnly Date), PricingCalendarEntry>> LoadPricingEntriesForRangeAsync(SqlConnection connection, long hotelId, IReadOnlyCollection<long> roomIds, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken)
    {
        if (roomIds.Count == 0)
        {
            return new Dictionary<(long RoomId, DateOnly Date), PricingCalendarEntry>();
        }

        var hasDiscountIdColumn = await ColumnExistsAsync(connection, "oda_fiyat_musaitlik", "indirim_id", cancellationToken);
        var discountIdSelect = hasDiscountIdColumn ? "indirim_id" : "kampanya_id";
        var campaignIdSelect = hasDiscountIdColumn ? "kampanya_id" : "CAST(NULL AS bigint)";

        var sql = $@"
            SELECT oda_tip_id,
                   tarih,
                   gecelik_fiyat,
                   indirimli_fiyat,
                   {discountIdSelect} AS indirim_id,
                   {campaignIdSelect} AS kampanya_id,
                   toplam_oda_sayisi,
                   satilan_oda_sayisi,
                   bloke_oda_sayisi,
                   minimum_geceleme,
                   maksimum_geceleme,
                   kapali_satis,
                   kampanya_etiketi,
                   fiyat_notu
            FROM oda_fiyat_musaitlik
            WHERE oda_tip_id IN ({string.Join(",", roomIds)})
              AND otel_id = @hotelId
              AND tarih BETWEEN @startDate AND @endDate;";

        var entries = new Dictionary<(long RoomId, DateOnly Date), PricingCalendarEntry>();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@hotelId", hotelId);
        command.Parameters.AddWithValue("@startDate", startDate.ToDateTime(TimeOnly.MinValue));
        command.Parameters.AddWithValue("@endDate", endDate.ToDateTime(TimeOnly.MinValue));
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var date = DateOnly.FromDateTime(reader.GetDateTime(1));
            entries[(reader.GetInt64(0), date)] = new PricingCalendarEntry(
                reader.GetInt64(0),
                date,
                SafeDecimal(reader, 2),
                reader.IsDBNull(3) ? null : reader.GetDecimal(3),
                reader.IsDBNull(4) ? null : reader.GetInt64(4),
                reader.IsDBNull(5) ? null : reader.GetInt64(5),
                reader.IsDBNull(6) ? null : SafeShort(reader, 6),
                reader.IsDBNull(7) ? (short)0 : SafeShort(reader, 7),
                reader.IsDBNull(8) ? (short)0 : SafeShort(reader, 8),
                reader.IsDBNull(9) ? null : SafeByte(reader, 9),
                reader.IsDBNull(10) ? null : SafeShort(reader, 10),
                SafeBool(reader, 11),
                reader.IsDBNull(12) ? null : NormalizeTurkishText(reader.GetString(12)),
                reader.IsDBNull(13) ? null : NormalizeTurkishText(reader.GetString(13)));
        }

        return entries;
    }

    private static List<PartnerStatCardViewModel> BuildPricingSummaryCards(
        IReadOnlyCollection<PartnerRoomSummaryViewModel> rooms,
        IReadOnlyDictionary<(long RoomId, DateOnly Date), PricingCalendarEntry> pricingEntries,
        long? selectedRoomId,
        decimal vatPercent,
        decimal accommodationPercent)
    {
        var filteredEntries = selectedRoomId.HasValue
            ? pricingEntries.Values.Where(item => item.RoomId == selectedRoomId.Value).ToList()
            : pricingEntries.Values.ToList();

        var activeRoomCount = rooms.Count(static item => item.IsActive);
        var campaignDayCount = filteredEntries.Count(static item => item.DiscountPrice.HasValue && item.DiscountPrice.Value > 0m && item.DiscountPrice.Value < item.BasePrice);
        var closedDayCount = filteredEntries.Count(static item => item.IsClosed);
        decimal averageNet;
        if (filteredEntries.Count > 0)
        {
            averageNet = filteredEntries.Average(static item => item.DiscountPrice ?? item.BasePrice);
        }
        else if (rooms.FirstOrDefault(item => !selectedRoomId.HasValue || item.RoomId == selectedRoomId.Value) is { } selectedRoom)
        {
            averageNet = selectedRoom.StandardNightlyStoredNet;
        }
        else
        {
            averageNet = 0m;
        }

        var averageDisplay = InclusiveNightlyPricing.StoredNetToPartnerDisplay(averageNet, vatPercent, accommodationPercent);

        return new List<PartnerStatCardViewModel>
        {
            new() { Label = "Aktif Oda Tipi", Value = activeRoomCount.ToString(CultureInfo.InvariantCulture), Description = "Takvimde yonetilen aktif oda tipleri", IconClass = "fa-bed", ToneClass = "info" },
            new() { Label = "Kampanyali Gun", Value = campaignDayCount.ToString(CultureInfo.InvariantCulture), Description = "Indirimli fiyat tanimlanmis gun sayisi", IconClass = "fa-tags", ToneClass = "success" },
            new() { Label = "Kapali Gun", Value = closedDayCount.ToString(CultureInfo.InvariantCulture), Description = "Satisa kapatilan takvim gunleri", IconClass = "fa-lock", ToneClass = "danger" },
            new() { Label = "Ortalama Fiyat", Value = FormatMoney(averageDisplay), Description = "Secili ay icin vergi dahil efektif gece fiyati ortalamasi", IconClass = "fa-money-bill-wave", ToneClass = "warning" }
        };
    }

    private static List<PartnerPricingDayViewModel> BuildPricingCalendarDays(
        IReadOnlyCollection<PartnerRoomSummaryViewModel> rooms,
        IReadOnlyDictionary<(long RoomId, DateOnly Date), PricingCalendarEntry> pricingEntries,
        long roomId,
        DateOnly monthStart,
        decimal vatPercent,
        decimal accommodationPercent)
    {
        var room = rooms.FirstOrDefault(item => item.RoomId == roomId);
        if (room is null)
        {
            return new List<PartnerPricingDayViewModel>();
        }

        var defaultBaseNet = room.StandardNightlyStoredNet;
        var defaultStock = ParseStock(room.StockText);
        var culture = CultureInfo.GetCultureInfo("tr-TR");
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);
        var firstOffset = GetMondayBasedIndex(monthStart.DayOfWeek);
        var calendarStart = monthStart.AddDays(-firstOffset);
        var lastOffset = 6 - GetMondayBasedIndex(monthEnd.DayOfWeek);
        var calendarEnd = monthEnd.AddDays(lastOffset);
        var today = DateOnly.FromDateTime(DateTime.Today);

        var items = new List<PartnerPricingDayViewModel>();
        for (var date = calendarStart; date <= calendarEnd; date = date.AddDays(1))
        {
            pricingEntries.TryGetValue((roomId, date), out var entry);
            var baseNet = entry?.BasePrice ?? defaultBaseNet;
            var discountNet = entry?.DiscountPrice;
            var totalRooms = entry?.TotalRooms ?? defaultStock;
            var soldRooms = entry?.SoldRooms ?? 0;
            var blockedRooms = entry?.BlockedRooms ?? 0;
            var availableRooms = Math.Max(0, totalRooms - soldRooms - blockedRooms);
            var isClosed = entry?.IsClosed ?? false;
            var effectiveNet = discountNet ?? baseNet;
            var hasDiscount = discountNet.HasValue && discountNet.Value > 0m && discountNet.Value < baseNet;
            var effectiveDisplay = InclusiveNightlyPricing.StoredNetToPartnerDisplay(effectiveNet, vatPercent, accommodationPercent);
            var baseDisplay = InclusiveNightlyPricing.StoredNetToPartnerDisplay(baseNet, vatPercent, accommodationPercent);
            var discountDisplay = discountNet.HasValue
                ? InclusiveNightlyPricing.StoredNetToPartnerDisplay(discountNet.Value, vatPercent, accommodationPercent)
                : (decimal?)null;
            var discountPercent = hasDiscount && baseNet > 0m
                ? Math.Round((baseNet - discountNet!.Value) / baseNet * 100m, 0, MidpointRounding.AwayFromZero)
                : 0m;

            items.Add(new PartnerPricingDayViewModel
            {
                Date = date,
                DayLabel = date.Day.ToString("00", CultureInfo.InvariantCulture),
                WeekdayLabel = date.ToDateTime(TimeOnly.MinValue).ToString("ddd", culture),
                PriceText = FormatMoney(effectiveDisplay),
                BasePriceText = FormatMoney(baseDisplay),
                DiscountPriceText = hasDiscount && discountDisplay.HasValue ? FormatMoney(discountDisplay.Value) : "-",
                BasePriceAmount = baseDisplay,
                DiscountPriceAmount = discountDisplay,
                TotalRooms = Convert.ToInt16(totalRooms, CultureInfo.InvariantCulture),
                SoldRooms = soldRooms,
                BlockedRooms = blockedRooms,
                MinStay = entry?.MinStay ?? (byte)1,
                MaxStay = entry?.MaxStay ?? (short)30,
                DiscountId = entry?.DiscountId,
                AvailabilityText = isClosed ? "Satisa kapali" : $"{availableRooms} oda musait",
                SoldText = soldRooms > 0 ? $"{soldRooms} satildi" : "Henuz satis yok",
                StatusText = isClosed ? "Kapali" : availableRooms == 0 ? "Dolu" : availableRooms <= 2 ? "Son odalar" : "Satista",
                CampaignLabel = entry?.CampaignLabel,
                DiscountPercentText = hasDiscount ? $"%{discountPercent:0} indirim" : null,
                ToneClass = isClosed ? "closed" : hasDiscount ? "discount" : availableRooms == 0 ? "closed" : "open",
                PriceNote = entry?.PriceNote,
                IsClosed = isClosed,
                IsHighlighted = hasDiscount || isClosed || availableRooms <= 2,
                IsToday = date == today,
                IsCurrentMonth = date.Month == monthStart.Month && date.Year == monthStart.Year,
                HasDiscount = hasDiscount
            });
        }

        return items;
    }

    private async Task<List<PartnerCampaignOptionViewModel>> LoadActiveCampaignOptionsAsync(SqlConnection connection, long hotelId, CancellationToken cancellationToken)
    {
        var tableExists = await TableExistsAsync(connection, "kampanyalar", cancellationToken);
        if (!tableExists)
        {
            return new List<PartnerCampaignOptionViewModel>();
        }

        const string sql = @"
            SELECT id,
                   kampanya_kodu,
                   kampanya_adi,
                   tur,
                   indirim_orani,
                   indirim_tutari,
                   baslangic_tarihi,
                   bitis_tarihi,
                   COALESCE(one_cikan_kampanya, 0) AS one_cikan_kampanya,
                   COALESCE(NULLIF(kisa_aciklama, ''), LEFT(kampanya_aciklamasi, 160)) AS kisa_aciklama,
                   promo_badge,
                   COALESCE(NULLIF(kampanya_renk_kodu, ''), '#003B95') AS kampanya_renk_kodu,
                   kampanya_etiketi
            FROM kampanyalar
            WHERE aktif_mi = 1
              AND gorunurluk_durumu IN ('Yayında', 'Zamanlanmış')
              AND bitis_tarihi >= GETDATE()
              AND COALESCE(partner_katilim_acik, 1) = 1
              AND (partner_katilim_baslangic IS NULL OR partner_katilim_baslangic <= GETDATE())
              AND (partner_katilim_bitis IS NULL OR partner_katilim_bitis >= GETDATE())
            ORDER BY one_cikan_kampanya DESC, baslangic_tarihi ASC, id ASC
            OFFSET 0 ROWS FETCH NEXT 50 ROWS ONLY;";

        var items = new List<PartnerCampaignOptionViewModel>();
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var discountText = reader.IsDBNull(4) && reader.IsDBNull(5)
                ? "Kampanya aktif"
                : !reader.IsDBNull(4)
                    ? $"%{Convert.ToDecimal(reader.GetValue(4), CultureInfo.InvariantCulture):0.##} indirim"
                    : $"{FormatMoney(Convert.ToDecimal(reader.GetValue(5), CultureInfo.InvariantCulture))} sabit indirim";

            items.Add(new PartnerCampaignOptionViewModel
            {
                CampaignId = reader.GetInt64(0),
                CampaignCode = reader.GetString(1),
                CampaignName = reader.GetString(2),
                CampaignType = reader.GetString(3),
                DiscountText = discountText,
                DateText = $"{reader.GetDateTime(6):dd.MM.yyyy} - {reader.GetDateTime(7):dd.MM.yyyy}",
                BadgeText = SafeBool(reader, 8) ? "Öne Çıkan" : (reader.IsDBNull(12) ? null : reader.GetString(12)),
                Description = reader.IsDBNull(9) ? null : reader.GetString(9),
                PromoBadge = reader.IsDBNull(10) ? null : reader.GetString(10),
                ColorCode = reader.IsDBNull(11) ? "#003B95" : reader.GetString(11),
                IsHighlighted = SafeBool(reader, 8)
            });
        }

        return items;
    }

    private async Task<List<PartnerCampaignParticipationViewModel>> LoadJoinedCampaignsAsync(SqlConnection connection, long hotelId, CancellationToken cancellationToken)
    {
        if (!await TableExistsAsync(connection, "kampanya_oteller", cancellationToken)
            || !await TableExistsAsync(connection, "kampanyalar", cancellationToken))
        {
            return new List<PartnerCampaignParticipationViewModel>();
        }

        const string sql = @"
            SELECT
                ko.id,
                ko.kampanya_id,
                k.kampanya_kodu,
                k.kampanya_adi,
                ko.katilim_durumu,
                ko.baslangic_tarihi,
                ko.bitis_tarihi,
                ko.kampanya_etiketi,
                ko.partner_notu,
                ko.one_cikan,
                ko.ozel_indirim_orani,
                ko.ozel_indirim_tutari,
                ko.ozel_kampanyali_fiyat
            FROM kampanya_oteller ko
            INNER JOIN kampanyalar k ON k.id = ko.kampanya_id
            WHERE ko.otel_id = @hotelId
            ORDER BY
                CASE ko.katilim_durumu
                    WHEN 'Aktif' THEN 0
                    WHEN 'Beklemede' THEN 1
                    WHEN 'Pasif' THEN 2
                    WHEN 'Sona Erdi' THEN 3
                    ELSE 4
                END,
                ko.one_cikan DESC,
                ko.siralama ASC,
                ko.id DESC;";

        var items = new List<PartnerCampaignParticipationViewModel>();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@hotelId", hotelId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            string? discountText = null;
            if (!reader.IsDBNull(12))
            {
                discountText = $"{FormatMoney(reader.GetDecimal(12))} kampanyalı fiyat";
            }
            else if (!reader.IsDBNull(10))
            {
                discountText = $"%{reader.GetDecimal(10):0.##} özel indirim";
            }
            else if (!reader.IsDBNull(11))
            {
                discountText = $"{FormatMoney(reader.GetDecimal(11))} sabit indirim";
            }

            items.Add(new PartnerCampaignParticipationViewModel
            {
                ParticipationId = reader.GetInt64(0),
                CampaignId = reader.GetInt64(1),
                CampaignCode = reader.GetString(2),
                CampaignName = reader.GetString(3),
                StatusText = reader.GetString(4),
                DateText = $"{reader.GetDateTime(5):dd.MM.yyyy} - {reader.GetDateTime(6):dd.MM.yyyy}",
                CampaignLabel = reader.IsDBNull(7) ? null : reader.GetString(7),
                PartnerNote = reader.IsDBNull(8) ? null : reader.GetString(8),
                IsFeatured = SafeBool(reader, 9),
                DiscountText = discountText
            });
        }

        return items;
    }

    private async Task<CampaignSelection?> ResolveCampaignAsync(SqlConnection connection, long? campaignId, CancellationToken cancellationToken)
    {
        if (!campaignId.HasValue || campaignId.Value <= 0)
        {
            return null;
        }

        if (!await TableExistsAsync(connection, "kampanyalar", cancellationToken))
        {
            return null;
        }

        const string sql = @"
            SELECT id, kampanya_adi, kampanya_kodu, baslangic_tarihi, bitis_tarihi
            FROM kampanyalar
            WHERE id = @campaignId
              AND aktif_mi = 1
              AND gorunurluk_durumu IN ('Yayında', 'Zamanlanmış')
              AND bitis_tarihi >= GETDATE()
              AND COALESCE(partner_katilim_acik, 1) = 1
              AND (partner_katilim_baslangic IS NULL OR partner_katilim_baslangic <= GETDATE())
              AND (partner_katilim_bitis IS NULL OR partner_katilim_bitis >= GETDATE());";

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@campaignId", campaignId.Value);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new CampaignSelection(
            reader.GetInt64(0),
            reader.GetString(1),
            reader.GetString(2),
            $"{reader.GetString(1)}",
            reader.GetDateTime(3).Date,
            reader.GetDateTime(4).Date);
    }

    private async Task<DiscountSelection?> ResolveDiscountAsync(SqlConnection connection, long? discountId, CancellationToken cancellationToken)
    {
        if (!discountId.HasValue || discountId.Value <= 0)
        {
            return null;
        }

        if (!await TableExistsAsync(connection, "fiyat_indirimleri", cancellationToken))
        {
            return null;
        }

        const string sql = @"
            SELECT TOP (1) id, indirim_adi
            FROM fiyat_indirimleri
            WHERE id = @discountId
              AND aktif_mi = 1;";

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@discountId", discountId.Value);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            return new DiscountSelection(reader.GetInt64(0), NormalizeTurkishText(reader.IsDBNull(1) ? string.Empty : reader.GetString(1)));
        }

        return null;
    }

    private async Task UpsertCampaignHotelParticipationAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        PartnerHotelContext hotel,
        CampaignSelection campaign,
        long userId,
        DateTime dateFrom,
        DateTime dateTo,
        CancellationToken cancellationToken)
    {
        if (!await TableExistsAsync(connection, "kampanya_oteller", transaction, cancellationToken))
        {
            return;
        }

        const string sql = @"
            IF EXISTS (SELECT 1 FROM kampanya_oteller WHERE kampanya_id = @campaignId AND otel_id = @hotelId)
            BEGIN
                UPDATE kampanya_oteller
                SET partner_id = @partnerId,
                    katilim_durumu = 'Aktif',
                    baslangic_tarihi = CASE
                        WHEN baslangic_tarihi IS NULL OR @startDate < baslangic_tarihi THEN @startDate
                        ELSE baslangic_tarihi
                    END,
                    bitis_tarihi = CASE
                        WHEN bitis_tarihi IS NULL OR @endDate > bitis_tarihi THEN @endDate
                        ELSE bitis_tarihi
                    END,
                    guncelleyen_kullanici_id = @userId,
                    guncellenme_tarihi = CURRENT_TIMESTAMP
                WHERE kampanya_id = @campaignId
                  AND otel_id = @hotelId;
            END
            ELSE
            BEGIN
                INSERT INTO kampanya_oteller
                (kampanya_id, otel_id, partner_id, katilim_durumu, baslangic_tarihi, bitis_tarihi, olusturan_kullanici_id, guncelleyen_kullanici_id)
                VALUES
                (@campaignId, @hotelId, @partnerId, 'Aktif', @startDate, @endDate, @userId, @userId);
            END;";

        await using var command = transaction is null
            ? new SqlCommand(sql, connection)
            : new SqlCommand(sql, connection, transaction!);
        command.Parameters.AddWithValue("@campaignId", campaign.CampaignId);
        command.Parameters.AddWithValue("@hotelId", hotel.HotelId);
        command.Parameters.AddWithValue("@partnerId", hotel.PartnerId);
        command.Parameters.AddWithValue("@startDate", dateFrom.Date < campaign.StartDate ? campaign.StartDate : dateFrom.Date);
        command.Parameters.AddWithValue("@endDate", dateTo.Date > campaign.EndDate ? campaign.EndDate : dateTo.Date);
        command.Parameters.AddWithValue("@userId", userId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task ExecuteDeleteByRoomAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        string tableName,
        long roomId,
        CancellationToken cancellationToken)
    {
        var sql = $@"
            DELETE FROM {tableName}
            WHERE oda_tip_id = @roomId;";
        await using var command = transaction is null
            ? new SqlCommand(sql, connection)
            : new SqlCommand(sql, connection, transaction!);
        command.Parameters.AddWithValue("@roomId", roomId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<bool> TableExistsAsync(SqlConnection connection, string tableName, CancellationToken cancellationToken)
        => await TableExistsAsync(connection, tableName, null, cancellationToken);

    private static async Task<bool> TableExistsAsync(SqlConnection connection, string tableName, SqlTransaction? transaction, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT COUNT(*)
            FROM information_schema.TABLES
            WHERE TABLE_CATALOG = DB_NAME()
              AND TABLE_NAME = @tableName;";

        await using var command = transaction is null
            ? new SqlCommand(sql, connection)
            : new SqlCommand(sql, connection, transaction!);
        command.Parameters.AddWithValue("@tableName", tableName);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result, CultureInfo.InvariantCulture) > 0;
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
        return Convert.ToInt32(result, CultureInfo.InvariantCulture) > 0;
    }

    private static DateOnly ParseMonthStart(string? month)
    {
        if (!string.IsNullOrWhiteSpace(month)
            && DateOnly.TryParseExact($"{month}-01", "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
        {
            return new DateOnly(parsed.Year, parsed.Month, 1);
        }

        var today = DateOnly.FromDateTime(DateTime.Today);
        return new DateOnly(today.Year, today.Month, 1);
    }

    private static List<PartnerMonthOptionViewModel> BuildMonthOptions(DateOnly selectedMonth)
    {
        var culture = CultureInfo.GetCultureInfo("tr-TR");
        var currentMonth = new DateOnly(DateTime.Today.Year, DateTime.Today.Month, 1);
        var start = currentMonth.AddMonths(-12);
        var end = ClampPricingMonth(DateOnly.FromDateTime(DateTime.Today.AddDays(365)));
        var items = new List<PartnerMonthOptionViewModel>();
        for (var month = start; month <= end; month = month.AddMonths(1))
        {
            items.Add(new PartnerMonthOptionViewModel
            {
                MonthKey = month.ToString("yyyy-MM", CultureInfo.InvariantCulture),
                Label = month.ToDateTime(TimeOnly.MinValue).ToString("MMMM yyyy", culture),
                IsCurrent = month.Year == selectedMonth.Year && month.Month == selectedMonth.Month
            });
        }

        return items;
    }

    private static string? ValidatePricingWindow(DateTime startDate, DateTime endDate)
    {
        var maxDate = DateTime.Today.AddDays(365).Date;
        if (startDate.Date > maxDate || endDate.Date > maxDate)
        {
            return $"365 günden sonrası için fiyat ve müsaitlik girişi yapılamaz. En son izin verilen tarih {maxDate:dd.MM.yyyy}.";
        }

        return null;
    }

    private static DateOnly ClampPricingMonth(DateOnly month)
    {
        var minMonth = new DateOnly(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(-12);
        var maxDate = DateOnly.FromDateTime(DateTime.Today.AddDays(365));
        var maxMonth = new DateOnly(maxDate.Year, maxDate.Month, 1);
        if (month < minMonth) return minMonth;
        if (month > maxMonth) return maxMonth;
        return month;
    }

    private static int GetMondayBasedIndex(DayOfWeek dayOfWeek)
        => ((int)dayOfWeek + 6) % 7;

    private static string? ValidateHotelInfoNumbers(PartnerHotelInfoForm request)
    {
        if (request.Latitude.HasValue && (request.Latitude.Value < -90m || request.Latitude.Value > 90m))
        {
            return "Enlem değeri -90 ile 90 arasında olmalıdır.";
        }

        if (request.Longitude.HasValue && (request.Longitude.Value < -180m || request.Longitude.Value > 180m))
        {
            return "Boylam değeri -180 ile 180 arasında olmalıdır.";
        }

        if (request.StarCount.HasValue && request.StarCount.Value > 5)
        {
            return "Yıldız sayısı 0 ile 5 arasında olmalıdır.";
        }

        if (request.LateCheckoutFee.HasValue && request.LateCheckoutFee.Value > 99999999.99m)
        {
            return "Geç çıkış ücreti çok yüksek. Lütfen geçerli bir tutar girin.";
        }

        if (request.EarlyCheckinFee.HasValue && request.EarlyCheckinFee.Value > 99999999.99m)
        {
            return "Erken giriş ücreti çok yüksek. Lütfen geçerli bir tutar girin.";
        }

        if (request.TotalRoomCount < 0 || request.TotalRoomCount > short.MaxValue)
        {
            return "Toplam oda sayısı geçerli aralıkta olmalıdır.";
        }

        if (request.TotalBedCapacity.HasValue && (request.TotalBedCapacity.Value < 0 || request.TotalBedCapacity.Value > short.MaxValue))
        {
            return "Yatak kapasitesi geçerli aralıkta olmalıdır.";
        }

        if (request.FloorCount.HasValue && request.FloorCount.Value > byte.MaxValue)
        {
            return "Kat sayısı geçerli aralıkta olmalıdır.";
        }

        if (request.ElevatorCount > byte.MaxValue)
        {
            return "Asansör sayısı geçerli aralıkta olmalıdır.";
        }

        if (request.MinStay < 1)
        {
            return "Minimum gece en az 1 olmalıdır.";
        }

        if (request.MaxStay < request.MinStay)
        {
            return "Maksimum gece minimum geceden küçük olamaz.";
        }

        return null;
    }

    private async Task<PartnerHotelInfoForm> LoadHotelInfoFormAsync(SqlConnection connection, long hotelId, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT otel_adi, otel_turu, otel_tipi_id, turizm_belge_no, turizm_belge_turu, kisa_aciklama, uzun_aciklama, tam_adres, sehir, ilce, mahalle, posta_kodu,
                   konum_aciklamasi, enlem, boylam, web_sitesi, eposta, telefon_1, telefon_2, faks,
                   check_in_saati, check_out_saati, gec_check_out_mumkun_mu, gec_check_out_ucreti, erken_check_in_mumkun_mu, erken_check_in_ucreti,
                   minimum_konaklama_gecesi, maksimum_konaklama_gecesi, yildiz_sayisi, toplam_oda_sayisi, toplam_yatak_kapasitesi, kat_sayisi,
                   asansor_var_mi, asansor_sayisi, varsayilan_komisyon_orani, depozito_tutari, depozito_iade_suresi, konusulan_diller, video_url, sanal_tur_url,
                   ulke, rezervasyon_telefonu, satis_kontak_adi, satis_kontak_telefonu, satis_kontak_eposta, satis_notlari,
                   komisyon_turu, komisyon_hesaplama_tipi, odeme_vadesi, odeme_yontemi, fatura_kesim_turu,
                   yayin_durumu, onay_durumu, ortalama_puan, toplam_yorum_sayisi, olusturulma_tarihi, guncellenme_tarihi
            FROM oteller
            WHERE id = @hotelId;";

        var model = new PartnerHotelInfoForm { HotelId = hotelId };
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@hotelId", hotelId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            model.HotelName = reader.GetString(0);
            model.HotelType = reader.GetString(1);
            model.HotelTypeId = reader.IsDBNull(2) ? null : reader.GetInt32(2);
            model.TourismDocumentNo = reader.IsDBNull(3) ? null : reader.GetString(3);
            model.TourismDocumentType = reader.IsDBNull(4) ? null : reader.GetString(4);
            model.ShortDescription = reader.IsDBNull(5) ? null : reader.GetString(5);
            model.Description = reader.IsDBNull(6) ? null : reader.GetString(6);
            model.Address = reader.IsDBNull(7) ? null : reader.GetString(7);
            model.City = reader.IsDBNull(8) ? null : reader.GetString(8);
            model.District = reader.IsDBNull(9) ? null : reader.GetString(9);
            model.Neighborhood = reader.IsDBNull(10) ? null : reader.GetString(10);
            model.PostalCode = reader.IsDBNull(11) ? null : reader.GetString(11);
            model.LocationDescription = reader.IsDBNull(12) ? null : reader.GetString(12);
            model.Latitude = reader.IsDBNull(13) ? null : reader.GetDecimal(13);
            model.Longitude = reader.IsDBNull(14) ? null : reader.GetDecimal(14);
            model.Website = reader.IsDBNull(15) ? null : reader.GetString(15);
            model.ContactEmail = reader.IsDBNull(16) ? null : reader.GetString(16);
            model.HotelPhone = reader.IsDBNull(17) ? null : reader.GetString(17);
            model.HotelPhone2 = reader.IsDBNull(18) ? null : reader.GetString(18);
            model.Fax = reader.IsDBNull(19) ? null : reader.GetString(19);
            model.CheckInTime = reader.IsDBNull(20) ? null : reader.GetTimeSpan(20).ToString(@"hh\:mm");
            model.CheckOutTime = reader.IsDBNull(21) ? null : reader.GetTimeSpan(21).ToString(@"hh\:mm");
            model.LateCheckoutAvailable = SafeBool(reader, 22);
            model.LateCheckoutFee = reader.IsDBNull(23) ? null : reader.GetDecimal(23);
            model.EarlyCheckinAvailable = SafeBool(reader, 24);
            model.EarlyCheckinFee = reader.IsDBNull(25) ? null : reader.GetDecimal(25);
            model.MinStay = reader.IsDBNull(26) ? (byte)1 : SafeByte(reader, 26);
            model.MaxStay = reader.IsDBNull(27) ? (short)30 : SafeShort(reader, 27);
            model.StarCount = reader.IsDBNull(28) ? null : SafeByte(reader, 28);
            model.TotalRoomCount = reader.IsDBNull(29) ? (short)0 : SafeShort(reader, 29);
            model.TotalBedCapacity = reader.IsDBNull(30) ? null : SafeShort(reader, 30);
            model.FloorCount = reader.IsDBNull(31) ? null : SafeByte(reader, 31);
            model.ElevatorAvailable = SafeBool(reader, 32);
            model.ElevatorCount = reader.IsDBNull(33) ? (byte)0 : SafeByte(reader, 33);
            model.DefaultCommissionRate = reader.IsDBNull(34) ? 0m : reader.GetDecimal(34);
            model.DepositAmount = reader.IsDBNull(35) ? null : reader.GetDecimal(35);
            model.DepositReturnDays = reader.IsDBNull(36) ? null : SafeByte(reader, 36);
            model.SpokenLanguages = reader.IsDBNull(37) ? null : reader.GetString(37);
            model.VideoUrl = reader.IsDBNull(38) ? null : reader.GetString(38);
            model.VirtualTourUrl = reader.IsDBNull(39) ? null : reader.GetString(39);
            model.Country = reader.IsDBNull(40) ? null : reader.GetString(40);
            model.ReservationPhone = reader.IsDBNull(41) ? null : reader.GetString(41);
            model.SalesContactName = reader.IsDBNull(42) ? null : reader.GetString(42);
            model.SalesContactPhone = reader.IsDBNull(43) ? null : reader.GetString(43);
            model.SalesContactEmail = reader.IsDBNull(44) ? null : reader.GetString(44);
            model.SalesNotes = reader.IsDBNull(45) ? null : reader.GetString(45);
            model.CommissionType = reader.IsDBNull(46) ? null : reader.GetString(46);
            model.CommissionCalculationType = reader.IsDBNull(47) ? null : reader.GetString(47);
            model.PaymentTerm = reader.IsDBNull(48) ? null : reader.GetString(48);
            model.PaymentMethod = reader.IsDBNull(49) ? null : reader.GetString(49);
            model.InvoiceIssueType = reader.IsDBNull(50) ? null : reader.GetString(50);
            model.PublishStatus = reader.IsDBNull(51) ? null : reader.GetString(51);
            model.ApprovalStatus = reader.IsDBNull(52) ? null : reader.GetString(52);
            model.AverageScore = reader.IsDBNull(53) ? null : reader.GetDecimal(53);
            model.TotalReviewCount = reader.IsDBNull(54) ? 0 : SafeInt(reader, 54);
            model.CreatedAt = reader.IsDBNull(55) ? null : reader.GetDateTime(55);
            model.UpdatedAt = reader.IsDBNull(56) ? null : reader.GetDateTime(56);
        }

        return model;
    }

    private static async Task<List<PartnerHotelTypeOptionViewModel>> LoadHotelTypeOptionsAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT id, kod, tip_adi, COALESCE(ikon_class, N'fa-hotel')
            FROM dbo.otel_tipleri
            WHERE aktif_mi = 1
            ORDER BY siralama, tip_adi;";

        var items = new List<PartnerHotelTypeOptionViewModel>();
        await using var command = new SqlCommand(sql, connection);
        command.CommandTimeout = 30;
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new PartnerHotelTypeOptionViewModel
            {
                HotelTypeId = reader.GetInt32(0),
                Code = reader.GetString(1),
                Name = reader.GetString(2),
                IconClass = reader.GetString(3)
            });
        }

        return items;
    }

    private static async Task<(int Id, string Name)> ResolvePartnerHotelTypeAsync(SqlConnection connection, int? hotelTypeId, SqlTransaction transaction, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT TOP (1) id, tip_adi
            FROM dbo.otel_tipleri
            WHERE aktif_mi = 1
              AND id = COALESCE(@hotelTypeId, (SELECT TOP (1) id FROM dbo.otel_tipleri WHERE kod = N'otel'))
            ORDER BY siralama;";

        await using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("@hotelTypeId", hotelTypeId.HasValue ? hotelTypeId.Value : DBNull.Value);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            return (reader.GetInt32(0), reader.GetString(1));
        }

        throw new InvalidOperationException("Gecerli otel tipi bulunamadi.");
    }

    private async Task<List<PartnerAmenityOptionViewModel>> LoadAmenityOptionsAsync(SqlConnection connection, long hotelId, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT o.id, o.ozellik_adi, COALESCE(o.ozellik_ikon, 'fa-circle-check') AS ikon,
                   COALESCE(k.kategori_adi, N'Genel') AS kategori_adi,
                   CASE WHEN oi.otel_id IS NULL THEN 0 ELSE 1 END AS secili
            FROM otel_ozellikleri o
            LEFT JOIN otel_ozellik_kategorileri k ON k.id = o.kategori_id
            LEFT JOIN otel_ozellik_iliskileri oi ON oi.ozellik_id = o.id AND oi.otel_id = @hotelId
            WHERE o.aktif_mi = 1
            ORDER BY COALESCE(k.siralama, 999), o.one_cikan_ozellik DESC, o.siralama ASC, o.id ASC;";

        var items = new List<PartnerAmenityOptionViewModel>();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@hotelId", hotelId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new PartnerAmenityOptionViewModel
            {
                AmenityId = Convert.ToInt64(reader.GetValue(0), CultureInfo.InvariantCulture),
                Name = reader.GetString(1),
                IconClass = reader.GetString(2),
                CategoryName = reader.GetString(3),
                IsSelected = SafeInt(reader, 4) == 1
            });
        }

        return items;
    }

    private async Task<List<PartnerRoomInventoryRowViewModel>> LoadRoomInventoryRowsAsync(SqlConnection connection, long hotelId, InclusiveTaxPercents tax, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT
                ot.id,
                ot.oda_adi,
                ot.toplam_oda_sayisi,
                CASE
                    WHEN (ot.toplam_oda_sayisi - COALESCE(MAX(ofm.satilan_oda_sayisi + ofm.bloke_oda_sayisi), 0)) > 0
                        THEN (ot.toplam_oda_sayisi - COALESCE(MAX(ofm.satilan_oda_sayisi + ofm.bloke_oda_sayisi), 0))
                    ELSE 0
                END AS satilabilir_oda,
                SUM(CASE WHEN COALESCE(ofm.kapali_satis, 0) = 1 THEN 1 ELSE 0 END) AS bakim_gunu,
                COALESCE(MIN(COALESCE(ofm.indirimli_fiyat, ofm.gecelik_fiyat)), ot.standart_gecelik_fiyat) AS minimum_fiyat,
                COALESCE(MAX(COALESCE(ofm.indirimli_fiyat, ofm.gecelik_fiyat)), ot.standart_gecelik_fiyat) AS maksimum_fiyat,
                ot.aktif_mi
            FROM oda_tipleri ot
            LEFT JOIN oda_fiyat_musaitlik ofm
                ON ofm.oda_tip_id = ot.id
               AND ofm.otel_id = ot.otel_id
               AND ofm.tarih BETWEEN CAST(GETDATE() AS date) AND DATEADD(DAY, 30, CAST(GETDATE() AS date))
            WHERE ot.otel_id = @hotelId
            GROUP BY ot.id, ot.oda_adi, ot.toplam_oda_sayisi, ot.standart_gecelik_fiyat, ot.aktif_mi, ot.siralama
            ORDER BY ot.aktif_mi DESC, ot.siralama ASC, ot.id ASC;";

        var items = new List<PartnerRoomInventoryRowViewModel>();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@hotelId", hotelId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new PartnerRoomInventoryRowViewModel
            {
                RoomId = reader.GetInt64(0),
                RoomName = reader.GetString(1),
                TotalRooms = Convert.ToInt16(reader.GetValue(2), CultureInfo.InvariantCulture),
                SellableRooms = Convert.ToInt16(reader.GetValue(3), CultureInfo.InvariantCulture),
                MaintenanceRooms = Convert.ToInt16(reader.GetValue(4), CultureInfo.InvariantCulture),
                MinPriceText = FormatMoney(InclusiveNightlyPricing.StoredNetToPartnerDisplay(SafeDecimal(reader, 5), tax.VatPercent, tax.AccommodationPercent)),
                MaxPriceText = FormatMoney(InclusiveNightlyPricing.StoredNetToPartnerDisplay(SafeDecimal(reader, 6), tax.VatPercent, tax.AccommodationPercent)),
                IsActive = SafeBool(reader, 7)
            });
        }

        return items;
    }

    private async Task<PartnerRoomUpsertRequest> LoadRoomFormAsync(SqlConnection connection, long hotelId, long roomId, CancellationToken cancellationToken)
    {
        var inclusiveTax = await LoadInclusiveTaxPercentsAsync(connection, hotelId, cancellationToken);

        const string sql = @"
            SELECT id, oda_adi, oda_kategorisi, yatak_tipi, manzara_tipi, oda_metrekare,
                   maksimum_yetiskin_sayisi, maksimum_cocuk_sayisi, bebek_ucretsiz_mi,
                   toplam_oda_sayisi, standart_gecelik_fiyat, kapak_fotografi, ozellikler, aktif_mi
            FROM oda_tipleri
            WHERE otel_id = @hotelId AND id = @roomId;";

        var model = new PartnerRoomUpsertRequest { HotelId = hotelId };
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@hotelId", hotelId);
        command.Parameters.AddWithValue("@roomId", roomId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            model.RoomId = reader.GetInt64(0);
            model.RoomName = reader.GetString(1);
            model.RoomCategory = reader.GetString(2);
            model.BedType = reader.IsDBNull(3) ? null : reader.GetString(3);
            model.ViewType = reader.IsDBNull(4) ? null : reader.GetString(4);
            model.RoomSize = reader.IsDBNull(5) ? null : Convert.ToInt16(reader.GetValue(5), CultureInfo.InvariantCulture);
            model.MaxAdults = reader.IsDBNull(6) ? (byte)2 : (byte)Math.Max(1, (int)SafeByte(reader, 6));
            model.MaxChildren = reader.IsDBNull(7) ? (byte)0 : SafeByte(reader, 7);
            model.MaxBabies = SafeBool(reader, 8) ? (byte)1 : (byte)0;
            model.TotalRooms = reader.IsDBNull(9) ? (short)1 : SafeShort(reader, 9);
            var storedNet = reader.IsDBNull(10) ? 0m : reader.GetDecimal(10);
            model.BasePrice = storedNet <= 0m
                ? 0m
                : InclusiveNightlyPricing.StoredNetToPartnerDisplay(storedNet, inclusiveTax.VatPercent, inclusiveTax.AccommodationPercent);
            model.CoverPhotoPath = reader.IsDBNull(11) ? null : reader.GetString(11);
            model.IsActive = SafeBool(reader, 13);

            if (!reader.IsDBNull(12))
            {
                try
                {
                    var features = JsonSerializer.Deserialize<List<string>>(reader.GetString(12));
                    model.RoomFeaturesText = features is null ? null : string.Join(", ", features);
                }
                catch (JsonException)
                {
                    model.RoomFeaturesText = null;
                }
            }
        }

        return model;
    }

    private async Task<PartnerPhotoEditForm> LoadPhotoEditFormAsync(SqlConnection connection, long hotelId, long photoId, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT id, gorsel_turu, baslik, aciklama, siralama, one_cikan
            FROM otel_gorselleri
            WHERE otel_id = @hotelId AND id = @photoId;";

        var model = new PartnerPhotoEditForm { HotelId = hotelId };
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@hotelId", hotelId);
        command.Parameters.AddWithValue("@photoId", photoId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            model.PhotoId = reader.GetInt64(0);
            model.PhotoType = reader.GetString(1);
            model.Title = reader.IsDBNull(2) ? null : reader.GetString(2);
            model.Description = reader.IsDBNull(3) ? null : reader.GetString(3);
            model.DisplayOrder = Convert.ToUInt16(reader.GetValue(4), CultureInfo.InvariantCulture);
            model.MarkAsFeatured = SafeBool(reader, 5);
        }

        return model;
    }

    private async Task<List<PartnerRoomPhotoCardViewModel>> LoadRoomPhotosAsync(SqlConnection connection, long hotelId, long roomId, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT og.id,
                   og.gorsel_url,
                   COALESCE(NULLIF(og.baslik, ''), ot.oda_adi) AS baslik,
                   og.aciklama,
                   og.siralama,
                   og.kapak_fotografi_mi,
                   og.onay_durumu
            FROM oda_gorselleri og
            INNER JOIN oda_tipleri ot ON ot.id = og.oda_tip_id
            WHERE og.oda_tip_id = @roomId
              AND ot.otel_id = @hotelId
            ORDER BY og.kapak_fotografi_mi DESC, og.siralama ASC, og.id ASC;";

        var items = new List<PartnerRoomPhotoCardViewModel>();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@roomId", roomId);
        command.Parameters.AddWithValue("@hotelId", hotelId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new PartnerRoomPhotoCardViewModel
            {
                PhotoId = reader.GetInt64(0),
                Url = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                Title = reader.IsDBNull(2) ? "Oda Görseli" : reader.GetString(2),
                Description = reader.IsDBNull(3) ? null : reader.GetString(3),
                DisplayOrder = Convert.ToUInt16(reader.GetValue(4), CultureInfo.InvariantCulture),
                IsCover = SafeBool(reader, 5),
                IsApproved = string.Equals(reader.IsDBNull(6) ? "Beklemede" : reader.GetString(6), "Onaylandı", StringComparison.OrdinalIgnoreCase)
            });
        }

        return items;
    }

    private async Task<(long RoomId, string RoomName)> EnsureRoomBelongsToHotelAsync(SqlConnection connection, long hotelId, long roomId, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT id, oda_adi
            FROM oda_tipleri
            WHERE id = @roomId
              AND otel_id = @hotelId;";

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@roomId", roomId);
        command.Parameters.AddWithValue("@hotelId", hotelId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            throw new InvalidOperationException("Seçilen oda tipi bu otele ait değil veya bulunamadı.");
        }

        return (reader.GetInt64(0), reader.GetString(1));
    }

    private static async Task SyncHotelRoomCountAsync(SqlConnection connection, long hotelId, CancellationToken cancellationToken)
    {
        const string sql = @"
            UPDATE oteller o
            SET o.toplam_oda_sayisi = COALESCE(
                (
                    SELECT SUM(CASE WHEN COALESCE(ot.aktif_mi, 1) = 1 THEN COALESCE(ot.toplam_oda_sayisi, 0) ELSE 0 END)
                    FROM oda_tipleri ot
                    WHERE ot.otel_id = o.id
                ),
                0
            )
            WHERE o.id = @hotelId;";

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@hotelId", hotelId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task SyncRoomFeatureRelationsAsync(SqlConnection connection, long roomId, List<long>? selectedFeatureIds, CancellationToken cancellationToken)
    {
        if (!await TableExistsAsync(connection, "oda_tipi_ozellikleri", cancellationToken)
            || !await TableExistsAsync(connection, "oda_ozellikleri", cancellationToken))
        {
            return;
        }

        var featureIds = (selectedFeatureIds ?? new List<long>())
            .Where(static id => id > 0)
            .Distinct()
            .ToList();

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        await using (var clearCommand = new SqlCommand("DELETE FROM oda_tipi_ozellikleri WHERE oda_tip_id = @roomId;", connection, (SqlTransaction)transaction))
        {
            clearCommand.Parameters.AddWithValue("@roomId", roomId);
            await clearCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        foreach (var featureId in featureIds)
        {
            const string selectSql = "SELECT TOP (1) id FROM oda_ozellikleri WHERE id = @featureId AND aktif_mi = 1;";
            await using var selectCommand = new SqlCommand(selectSql, connection, (SqlTransaction)transaction);
            selectCommand.Parameters.AddWithValue("@featureId", featureId);
            var existingId = await selectCommand.ExecuteScalarAsync(cancellationToken);
            if (existingId is null)
            {
                continue;
            }

            const string relationSql = @"
                IF EXISTS (SELECT 1 FROM oda_tipi_ozellikleri WHERE oda_tip_id = @roomId AND ozellik_id = @featureId)
                    UPDATE oda_tipi_ozellikleri SET miktar = 1 WHERE oda_tip_id = @roomId AND ozellik_id = @featureId
                ELSE
                    INSERT INTO oda_tipi_ozellikleri (oda_tip_id, ozellik_id, miktar) VALUES (@roomId, @featureId, 1);";
            await using var relationCommand = new SqlCommand(relationSql, connection, (SqlTransaction)transaction);
            relationCommand.Parameters.AddWithValue("@roomId", roomId);
            relationCommand.Parameters.AddWithValue("@featureId", featureId);
            await relationCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }

    private static async Task<List<PartnerRoomFeatureRowViewModel>> LoadActiveRoomFeaturesAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        var rows = new List<PartnerRoomFeatureRowViewModel>();
        if (!await TableExistsAsync(connection, "oda_ozellikleri", cancellationToken))
        {
            return rows;
        }

        const string sql = @"
            SELECT id, COALESCE(kategori, N'Genel') AS kategori, ozellik_adi, ozellik_ikon, siralama, aktif_mi
            FROM oda_ozellikleri
            WHERE aktif_mi = 1
            ORDER BY kategori, siralama, ozellik_adi;";
        await using var cmd = new SqlCommand(sql, connection);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(new PartnerRoomFeatureRowViewModel
            {
                FeatureId = reader.IsDBNull(0) ? (short)0 : Convert.ToInt16(reader.GetValue(0), CultureInfo.InvariantCulture),
                Category = reader.IsDBNull(1) ? "Genel" : reader.GetString(1),
                Name = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                IconClass = reader.IsDBNull(3) ? null : reader.GetString(3),
                Order = reader.IsDBNull(4) ? (short)100 : reader.GetInt16(4),
                IsActive = !reader.IsDBNull(5) && reader.GetBoolean(5)
            });
        }
        rows.RemoveAll(x => x.FeatureId <= 0 || string.IsNullOrWhiteSpace(x.Name));
        return rows;
    }

    private static async Task<List<long>> LoadSelectedRoomFeatureIdsAsync(SqlConnection connection, long roomId, CancellationToken cancellationToken)
    {
        var rows = new List<long>();
        if (roomId <= 0) return rows;
        if (!await TableExistsAsync(connection, "oda_tipi_ozellikleri", cancellationToken))
        {
            return rows;
        }

        const string sql = "SELECT ozellik_id FROM oda_tipi_ozellikleri WHERE oda_tip_id = @roomId;";
        await using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@roomId", roomId);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            if (!reader.IsDBNull(0))
            {
                rows.Add(Convert.ToInt64(reader.GetValue(0), CultureInfo.InvariantCulture));
            }
        }
        return rows.Distinct().Where(x => x > 0).ToList();
    }

    private static async Task UpdateRoomCoverSelectionAsync(SqlConnection connection, SqlTransaction transaction, long roomId, string coverUrl, CancellationToken cancellationToken)
    {
        await using (var updatePhotos = new SqlCommand("UPDATE oda_gorselleri SET kapak_fotografi_mi = CASE WHEN gorsel_url = @coverUrl THEN 1 ELSE 0 END WHERE oda_tip_id = @roomId;", connection, (SqlTransaction)transaction))
        {
            updatePhotos.Parameters.AddWithValue("@coverUrl", coverUrl);
            updatePhotos.Parameters.AddWithValue("@roomId", roomId);
            await updatePhotos.ExecuteNonQueryAsync(cancellationToken);
        }

        await using (var updateRoom = new SqlCommand("UPDATE oda_tipleri SET kapak_fotografi = @coverUrl WHERE id = @roomId;", connection, (SqlTransaction)transaction))
        {
            updateRoom.Parameters.AddWithValue("@coverUrl", coverUrl);
            updateRoom.Parameters.AddWithValue("@roomId", roomId);
            await updateRoom.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    private static async Task PromoteNextRoomCoverAsync(SqlConnection connection, SqlTransaction transaction, long roomId, CancellationToken cancellationToken)
    {
        const string selectNextSql = "SELECT TOP (1) id, gorsel_url FROM oda_gorselleri WHERE oda_tip_id = @roomId ORDER BY siralama, id;";
        await using var selectCommand = new SqlCommand(selectNextSql, connection, (SqlTransaction)transaction);
        selectCommand.Parameters.AddWithValue("@roomId", roomId);
        await using var reader = await selectCommand.ExecuteReaderAsync(cancellationToken);

        long? nextPhotoId = null;
        string? nextUrl = null;
        if (await reader.ReadAsync(cancellationToken))
        {
            nextPhotoId = reader.GetInt64(0);
            nextUrl = reader.IsDBNull(1) ? null : reader.GetString(1);
        }

        await reader.CloseAsync();

        if (!nextPhotoId.HasValue || string.IsNullOrWhiteSpace(nextUrl))
        {
            await using var clearRoom = new SqlCommand("UPDATE oda_tipleri SET kapak_fotografi = NULL WHERE id = @roomId;", connection, (SqlTransaction)transaction);
            clearRoom.Parameters.AddWithValue("@roomId", roomId);
            await clearRoom.ExecuteNonQueryAsync(cancellationToken);
            return;
        }

        await using (var updatePhotos = new SqlCommand("UPDATE oda_gorselleri SET kapak_fotografi_mi = CASE WHEN id = @photoId THEN 1 ELSE 0 END WHERE oda_tip_id = @roomId;", connection, (SqlTransaction)transaction))
        {
            updatePhotos.Parameters.AddWithValue("@photoId", nextPhotoId.Value);
            updatePhotos.Parameters.AddWithValue("@roomId", roomId);
            await updatePhotos.ExecuteNonQueryAsync(cancellationToken);
        }

        await using (var updateRoom = new SqlCommand("UPDATE oda_tipleri SET kapak_fotografi = @coverUrl WHERE id = @roomId;", connection, (SqlTransaction)transaction))
        {
            updateRoom.Parameters.AddWithValue("@coverUrl", nextUrl);
            updateRoom.Parameters.AddWithValue("@roomId", roomId);
            await updateRoom.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    private static object DbValue(string? value)
        => string.IsNullOrWhiteSpace(value) ? DBNull.Value : value.Trim();

    private async Task<List<PartnerCompetitorRowViewModel>> LoadCompetitorsAsync(SqlConnection connection, long hotelId, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT TOP (20) id, rakip_otel_adi, rakip_sehir, rakip_ilce, analiz_tarihi, ortalama_gecelik_fiyat, tahmini_doluluk_orani, kaynak_url, notlar
            FROM otel_rakip_analizi
            WHERE otel_id = @hotelId
            ORDER BY analiz_tarihi DESC, id DESC;";

        var items = new List<PartnerCompetitorRowViewModel>();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@hotelId", hotelId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var city = reader.IsDBNull(2) ? string.Empty : reader.GetString(2);
            var district = reader.IsDBNull(3) ? string.Empty : reader.GetString(3);
            var location = string.Join(" / ", new[] { district, city }.Where(static item => !string.IsNullOrWhiteSpace(item)));
            var occupancy = reader.IsDBNull(6)
                ? "-"
                : $"{Convert.ToDecimal(reader.GetValue(6), CultureInfo.InvariantCulture):0.##}%";

            items.Add(new PartnerCompetitorRowViewModel
            {
                CompetitorId = reader.GetInt64(0),
                HotelName = reader.GetString(1),
                LocationText = string.IsNullOrWhiteSpace(location) ? "Konum girilmedi" : location,
                AnalysisDateText = reader.GetDateTime(4).ToString("dd.MM.yyyy", CultureInfo.GetCultureInfo("tr-TR")),
                AveragePriceText = reader.IsDBNull(5) ? "-" : FormatMoney(reader.GetDecimal(5)),
                OccupancyText = occupancy,
                SourceUrl = reader.IsDBNull(7) ? null : reader.GetString(7),
                Notes = reader.IsDBNull(8) ? null : reader.GetString(8)
            });
        }

        return items;
    }

    private static void BindRoomCommand(SqlCommand command, PartnerRoomUpsertRequest request, PartnerHotelContext hotel, string? featuresJson, decimal storedNetBasePrice)
    {
        var safeMaxAdults = (byte)Math.Max(1, (int)request.MaxAdults);
        var safeMaxChildren = request.MaxChildren;
        var safeMaxBabies = request.MaxBabies;
        var totalCapacity = Math.Max(1, (int)safeMaxAdults + safeMaxChildren + safeMaxBabies);

        command.Parameters.AddWithValue("@hotelId", hotel.HotelId);
        command.Parameters.AddWithValue("@roomName", request.RoomName.Trim());
        command.Parameters.AddWithValue("@roomCategory", request.RoomCategory);
        command.Parameters.AddWithValue("@maxPeople", totalCapacity);
        command.Parameters.AddWithValue("@maxAdults", safeMaxAdults);
        command.Parameters.AddWithValue("@maxChildren", safeMaxChildren);
        command.Parameters.AddWithValue("@babyFree", safeMaxBabies > 0 ? 1 : 0);
        command.Parameters.AddWithValue("@bedType", (object?)request.BedType ?? DBNull.Value);
        command.Parameters.AddWithValue("@roomSize", request.RoomSize.HasValue ? request.RoomSize.Value : DBNull.Value);
        command.Parameters.AddWithValue("@viewType", (object?)request.ViewType ?? DBNull.Value);
        command.Parameters.AddWithValue("@basePrice", storedNetBasePrice);
        command.Parameters.AddWithValue("@totalRooms", request.TotalRooms);
        command.Parameters.AddWithValue("@coverPhoto", (object?)request.CoverPhotoPath ?? DBNull.Value);
        command.Parameters.AddWithValue("@features", string.IsNullOrWhiteSpace(featuresJson) ? DBNull.Value : featuresJson);
        command.Parameters.AddWithValue("@active", request.IsActive ? 1 : 0);
    }

    private static void BindCompetitorCommand(SqlCommand command, PartnerCompetitorUpsertRequest request, long hotelId)
    {
        command.Parameters.AddWithValue("@hotelId", hotelId);
        command.Parameters.AddWithValue("@hotelName", request.CompetitorHotelName.Trim());
        command.Parameters.AddWithValue("@city", string.IsNullOrWhiteSpace(request.CompetitorCity) ? DBNull.Value : request.CompetitorCity.Trim());
        command.Parameters.AddWithValue("@district", string.IsNullOrWhiteSpace(request.CompetitorDistrict) ? DBNull.Value : request.CompetitorDistrict.Trim());
        command.Parameters.AddWithValue("@analysisDate", request.AnalysisDate.Date);
        command.Parameters.AddWithValue("@averagePrice", request.AverageNightlyPrice.HasValue ? request.AverageNightlyPrice.Value : DBNull.Value);
        command.Parameters.AddWithValue("@occupancyRate", request.EstimatedOccupancyRate.HasValue ? request.EstimatedOccupancyRate.Value : DBNull.Value);
        command.Parameters.AddWithValue("@sourceUrl", string.IsNullOrWhiteSpace(request.SourceUrl) ? DBNull.Value : request.SourceUrl.Trim());
        command.Parameters.AddWithValue("@notes", string.IsNullOrWhiteSpace(request.Notes) ? DBNull.Value : request.Notes.Trim());
    }

    private async Task<(List<PartnerReservationRowViewModel> Items, int TotalCount)> LoadReservationsAsync(
        SqlConnection connection,
        long hotelId,
        string hotelName,
        DateTime? dateFrom,
        DateTime? dateTo,
        string statusFilter,
        string paymentMethodFilter,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        const string countSql = @"
            SELECT COUNT(*)
            FROM rezervasyonlar r
            WHERE r.otel_id = @hotelId
              AND (@dateFrom IS NULL OR CAST(r.giris_tarihi AS date) >= CAST(@dateFrom AS date))
              AND (@dateTo IS NULL OR CAST(r.cikis_tarihi AS date) <= CAST(@dateTo AS date))
              AND (
                    @statusFilter = 'all'
                    OR (@statusFilter = 'pending' AND r.durum IN ('Onay Bekliyor','Değişiklik Bekliyor'))
                    OR (@statusFilter = 'approved' AND r.durum = 'Onaylandı')
                    OR (@statusFilter = 'completed' AND r.durum = 'Tamamlandı')
                    OR (@statusFilter = 'rejected' AND (r.durum = 'İptal Edildi' OR COALESCE(r.otel_onay_durumu, '') = 'Reddedildi'))
                  )
              AND (
                    @paymentMethodFilter = 'all'
                    OR (@paymentMethodFilter = 'card' AND COALESCE(r.odeme_yontemi, '') IN ('Kredi Kartı','Sanal POS'))
                    OR (@paymentMethodFilter = 'cash' AND COALESCE(r.odeme_yontemi, '') IN ('Kapıda Ödeme','Nakit'))
                    OR (@paymentMethodFilter = 'transfer' AND (
                            COALESCE(r.odeme_yontemi, '') IN ('Havale/EFT','Banka Havalesi','Banka Havalesi/EFT')
                            OR EXISTS (
                                SELECT 1 FROM dbo.rezervasyon_odeme_kalemleri k
                                INNER JOIN dbo.odeme_yontemi_tanimlari oy ON oy.id = k.odeme_yontemi_id AND oy.kod IN (N'HAVALE_EFT', N'BANKA_HAVALESI')
                                WHERE k.rezervasyon_id = r.id)))
                    OR (@paymentMethodFilter = 'mixed' AND COALESCE(r.odeme_yontemi, '') = N'Karma Ödeme')
                  );";
        var totalCount = 0;
        await using (var countCommand = new SqlCommand(countSql, connection))
        {
            countCommand.Parameters.AddWithValue("@hotelId", hotelId);
            countCommand.Parameters.AddWithValue("@dateFrom", dateFrom.HasValue ? dateFrom.Value : DBNull.Value);
            countCommand.Parameters.AddWithValue("@dateTo", dateTo.HasValue ? dateTo.Value : DBNull.Value);
            countCommand.Parameters.AddWithValue("@statusFilter", statusFilter);
            countCommand.Parameters.AddWithValue("@paymentMethodFilter", paymentMethodFilter);
            var scalar = await countCommand.ExecuteScalarAsync(cancellationToken);
            totalCount = Convert.ToInt32(scalar ?? 0, CultureInfo.InvariantCulture);
        }

        if (totalCount <= 0)
        {
            return (new List<PartnerReservationRowViewModel>(), 0);
        }

        const string sql = @"
            SELECT
                r.id,
                r.rezervasyon_no,
                COALESCE(NULLIF(r.misafir_ad_soyad, ''), 'Misafir') AS misafir_ad_soyad,
                COALESCE(NULLIF(r.misafir_eposta, ''), '') AS misafir_eposta,
                COALESCE(NULLIF(r.misafir_telefon, ''), '') AS misafir_telefon,
                r.giris_tarihi,
                r.cikis_tarihi,
                r.durum,
                r.odeme_durumu,
                r.toplam_tutar,
                r.olusturulma_tarihi,
                COALESCE(ot.oda_adi, '') AS oda_adi,
                COALESCE(r.odeme_yontemi, '') AS odeme_yontemi,
                COALESCE(r.kaynak, COALESCE(r.rezervasyon_kanali, 'Web')) AS kaynak,
                COALESCE(NULLIF(r.misafir_notu, ''), '') AS misafir_notu,
                COALESCE(NULLIF(r.musteri_talep_notu, ''), '') AS musteri_talep_notu,
                COALESCE(NULLIF(r.iptal_nedeni, ''), '') AS iptal_nedeni,
                r.iptal_tarihi,
                COALESCE(r.yetiskin_sayisi, 0) AS yetiskin_sayisi,
                COALESCE(r.cocuk_sayisi, 0) AS cocuk_sayisi,
                DATEDIFF(DAY, r.giris_tarihi, r.cikis_tarihi) AS gece_sayisi,
                COALESCE(r.otel_onay_durumu, 'Beklemede') AS otel_onay_durumu,
                r.kullanici_id,
                (
                    SELECT COUNT(*)
                    FROM dbo.rezervasyon_odeme_kalemleri k
                    WHERE k.rezervasyon_id = r.id
                ) AS odeme_kalem_sayisi,
                (
                    SELECT STRING_AGG(CAST(y.ad AS nvarchar(max)) + N': ' + CAST(k.tutar AS nvarchar(40)) + N' TL', N' | ')
                    FROM dbo.rezervasyon_odeme_kalemleri k
                    INNER JOIN dbo.odeme_yontemi_tanimlari y ON y.id = k.odeme_yontemi_id
                    WHERE k.rezervasyon_id = r.id
                ) AS odeme_kalem_ozet
            FROM rezervasyonlar r
            LEFT JOIN oda_tipleri ot ON ot.id = r.oda_tip_id
            WHERE r.otel_id = @hotelId
              AND (@dateFrom IS NULL OR CAST(r.giris_tarihi AS date) >= CAST(@dateFrom AS date))
              AND (@dateTo IS NULL OR CAST(r.cikis_tarihi AS date) <= CAST(@dateTo AS date))
              AND (
                    @statusFilter = 'all'
                    OR (@statusFilter = 'pending' AND r.durum IN ('Onay Bekliyor','Değişiklik Bekliyor'))
                    OR (@statusFilter = 'approved' AND r.durum = 'Onaylandı')
                    OR (@statusFilter = 'completed' AND r.durum = 'Tamamlandı')
                    OR (@statusFilter = 'rejected' AND (r.durum = 'İptal Edildi' OR COALESCE(r.otel_onay_durumu, '') = 'Reddedildi'))
                  )
              AND (
                    @paymentMethodFilter = 'all'
                    OR (@paymentMethodFilter = 'card' AND COALESCE(r.odeme_yontemi, '') IN ('Kredi Kartı','Sanal POS'))
                    OR (@paymentMethodFilter = 'cash' AND COALESCE(r.odeme_yontemi, '') IN ('Kapıda Ödeme','Nakit'))
                    OR (@paymentMethodFilter = 'transfer' AND (
                            COALESCE(r.odeme_yontemi, '') IN ('Havale/EFT','Banka Havalesi','Banka Havalesi/EFT')
                            OR EXISTS (
                                SELECT 1 FROM dbo.rezervasyon_odeme_kalemleri k
                                INNER JOIN dbo.odeme_yontemi_tanimlari oy ON oy.id = k.odeme_yontemi_id AND oy.kod IN (N'HAVALE_EFT', N'BANKA_HAVALESI')
                                WHERE k.rezervasyon_id = r.id)))
                    OR (@paymentMethodFilter = 'mixed' AND COALESCE(r.odeme_yontemi, '') = N'Karma Ödeme')
                  )
            ORDER BY r.olusturulma_tarihi DESC, r.id DESC
            OFFSET @offset ROWS FETCH NEXT @limit ROWS ONLY;";

        var items = new List<PartnerReservationRowViewModel>();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@hotelId", hotelId);
        command.Parameters.AddWithValue("@dateFrom", dateFrom.HasValue ? dateFrom.Value : DBNull.Value);
        command.Parameters.AddWithValue("@dateTo", dateTo.HasValue ? dateTo.Value : DBNull.Value);
        command.Parameters.AddWithValue("@statusFilter", statusFilter);
        command.Parameters.AddWithValue("@paymentMethodFilter", paymentMethodFilter);
        command.Parameters.AddWithValue("@limit", pageSize);
        command.Parameters.AddWithValue("@offset", (page - 1) * pageSize);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var reservationStatus = reader.IsDBNull(7) ? "Onay Bekliyor" : reader.GetString(7);
            var hotelApprovalStatus = reader.IsDBNull(21) ? "Beklemede" : reader.GetString(21);
            var canApprove = (string.Equals(reservationStatus, "Onay Bekliyor", StringComparison.OrdinalIgnoreCase)
                              || string.Equals(reservationStatus, "Değişiklik Bekliyor", StringComparison.OrdinalIgnoreCase)
                              || string.Equals(hotelApprovalStatus, "Beklemede", StringComparison.OrdinalIgnoreCase))
                             && !string.Equals(reservationStatus, "İptal Edildi", StringComparison.OrdinalIgnoreCase)
                             && !string.Equals(reservationStatus, "Tamamlandı", StringComparison.OrdinalIgnoreCase);

            var canReject = !string.Equals(reservationStatus, "İptal Edildi", StringComparison.OrdinalIgnoreCase)
                            && !string.Equals(reservationStatus, "Tamamlandı", StringComparison.OrdinalIgnoreCase)
                            && !string.Equals(hotelApprovalStatus, "Reddedildi", StringComparison.OrdinalIgnoreCase);
            var canCheckIn = string.Equals(reservationStatus, "Onaylandı", StringComparison.OrdinalIgnoreCase)
                             && string.Equals(hotelApprovalStatus, "Onaylandı", StringComparison.OrdinalIgnoreCase);
            var canMarkPaymentCompleted = string.Equals(reservationStatus, "Tamamlandı", StringComparison.OrdinalIgnoreCase)
                                          && !string.Equals(reader.IsDBNull(8) ? string.Empty : reader.GetString(8), "Tamamlandı", StringComparison.OrdinalIgnoreCase);

            items.Add(new PartnerReservationRowViewModel
            {
                ReservationId = reader.GetInt64(0),
                ReservationNo = reader.GetString(1),
                HotelName = hotelName,
                GuestName = reader.GetString(2),
                GuestEmail = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                GuestPhone = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                StayText = FormatStay(reader.GetDateTime(5), reader.GetDateTime(6)),
                StatusLabel = GetPartnerReservationDisplayStatus(reservationStatus, hotelApprovalStatus),
                PaymentStatusLabel = reader.IsDBNull(8) ? "Beklemede" : reader.GetString(8),
                TotalText = FormatMoney(SafeDecimal(reader, 9)),
                CreatedText = FormatDateTime(reader.IsDBNull(10) ? null : reader.GetDateTime(10)),
                RoomName = reader.IsDBNull(11) ? null : reader.GetString(11),
                PaymentMethodLabel = reader.IsDBNull(12) || string.IsNullOrWhiteSpace(reader.GetString(12)) ? null : reader.GetString(12),
                SourceLabel = reader.IsDBNull(13) || string.IsNullOrWhiteSpace(reader.GetString(13)) ? null : reader.GetString(13),
                GuestNote = reader.IsDBNull(14) ? null : reader.GetString(14),
                RequestNote = reader.IsDBNull(15) ? null : reader.GetString(15),
                CancellationReason = reader.IsDBNull(16) || string.IsNullOrWhiteSpace(reader.GetString(16)) ? null : reader.GetString(16),
                CancellationTimeText = reader.IsDBNull(17) ? null : reader.GetDateTime(17).ToString("dd.MM.yyyy HH:mm", CultureInfo.GetCultureInfo("tr-TR")),
                AdultCount = reader.IsDBNull(18) ? (byte)0 : SafeByte(reader, 18),
                ChildCount = reader.IsDBNull(19) ? (byte)0 : SafeByte(reader, 19),
                NightCount = reader.IsDBNull(20) ? (short)0 : Convert.ToInt16(reader.GetValue(20), CultureInfo.InvariantCulture),
                CanApprove = canApprove,
                CanReject = canReject,
                CanCheckIn = canCheckIn,
                CanMarkPaymentCompleted = canMarkPaymentCompleted,
                CanMessageGuest = !reader.IsDBNull(22) && Convert.ToInt64(reader.GetValue(22), CultureInfo.InvariantCulture) > 0,
                PaymentLineCount = reader.IsDBNull(23) ? 0 : Convert.ToInt32(reader.GetValue(23), CultureInfo.InvariantCulture),
                PaymentMixSummary = reader.IsDBNull(24) ? null : reader.GetString(24),
                StatusTone = GetPartnerReservationStatusTone(reservationStatus, hotelApprovalStatus)
            });
        }

        return (items, totalCount);
    }

    private static async Task<List<PartnerConversationSummaryViewModel>> LoadReservationConversationsAsync(SqlConnection connection, long hotelId, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT
                mk.id,
                COALESCE(mk.rezervasyon_id, 0) AS rezervasyon_id,
                COALESCE(NULLIF(r.rezervasyon_no, ''), '-') AS rezervasyon_no,
                COALESCE(NULLIF(r.misafir_ad_soyad, ''), 'Misafir') AS misafir_ad_soyad,
                COALESCE(NULLIF(mk.konu_basligi, ''), 'Rezervasyon mesaji') AS konu_basligi,
                COALESCE(NULLIF(mk.son_mesaj_onizleme, ''), 'Mesaj bulunmuyor') AS son_mesaj_onizleme,
                COALESCE(mk.son_mesaj_tarihi, mk.olusturulma_tarihi) AS son_mesaj_tarihi,
                COALESCE(mk.otel_okunmamis_sayisi, 0) AS otel_okunmamis_sayisi
            FROM mesaj_konusmalari mk
            LEFT JOIN rezervasyonlar r ON r.id = mk.rezervasyon_id
            WHERE mk.otel_id = @hotelId
              AND mk.durum <> 'Arşivlendi'
            ORDER BY COALESCE(mk.otel_okunmamis_sayisi, 0) DESC, COALESCE(mk.son_mesaj_tarihi, mk.olusturulma_tarihi) DESC
            OFFSET 0 ROWS FETCH NEXT 40 ROWS ONLY;";

        var items = new List<PartnerConversationSummaryViewModel>();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@hotelId", hotelId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new PartnerConversationSummaryViewModel
            {
                ConversationId = reader.GetInt64(0),
                ReservationId = Convert.ToInt64(reader.GetValue(1), CultureInfo.InvariantCulture),
                ReservationNo = reader.GetString(2),
                GuestName = reader.GetString(3),
                Subject = reader.GetString(4),
                LastMessagePreview = reader.GetString(5),
                LastMessageTimeText = FormatDateTime(reader.GetDateTime(6)),
                UnreadCount = SafeInt(reader, 7)
            });
        }

        return items;
    }

    private static async Task<List<PartnerConversationMessageViewModel>> LoadConversationMessagesAsync(SqlConnection connection, long hotelId, long conversationId, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT
                m.id,
                m.gonderen_turu,
                COALESCE(NULLIF(m.mesaj_metni, ''), '-') AS mesaj_metni,
                COALESCE(m.gonderim_tarihi, m.duzenlenme_tarihi) AS mesaj_tarihi
            FROM mesajlar m
            INNER JOIN mesaj_konusmalari mk ON mk.id = m.konusma_id
            WHERE m.konusma_id = @conversationId
              AND mk.otel_id = @hotelId
              AND COALESCE(m.durum, '') <> 'Silindi'
            ORDER BY m.gonderim_tarihi ASC, m.id ASC
            OFFSET 0 ROWS FETCH NEXT 250 ROWS ONLY;";

        var items = new List<PartnerConversationMessageViewModel>();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@conversationId", conversationId);
        command.Parameters.AddWithValue("@hotelId", hotelId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var senderType = reader.IsDBNull(1) ? string.Empty : reader.GetString(1);
            var isFromHotel = string.Equals(senderType, "Otel", StringComparison.OrdinalIgnoreCase);
            items.Add(new PartnerConversationMessageViewModel
            {
                MessageId = reader.GetInt64(0),
                SenderLabel = isFromHotel ? "Partner" : string.Equals(senderType, "Misafir", StringComparison.OrdinalIgnoreCase) ? "Misafir" : senderType,
                Body = reader.GetString(2),
                TimeText = FormatDateTime(reader.GetDateTime(3)),
                IsFromHotel = isFromHotel
            });
        }

        return items;
    }

    private static async Task MarkConversationAsReadAsync(SqlConnection connection, long hotelId, long conversationId, CancellationToken cancellationToken)
    {
        const string sql = @"
            UPDATE mesaj_konusmalari
            SET otel_okunmamis_sayisi = 0,
                otel_son_okuma_tarihi = GETUTCDATE(),
                guncellenme_tarihi = CURRENT_TIMESTAMP
            WHERE id = @conversationId
              AND otel_id = @hotelId;";
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@conversationId", conversationId);
        command.Parameters.AddWithValue("@hotelId", hotelId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task<List<PartnerInventoryAlertViewModel>> LoadInventoryAlertsAsync(SqlConnection connection, long hotelId, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT ot.id, ot.oda_adi, ofm.tarih,
                   (ofm.toplam_oda_sayisi - ofm.satilan_oda_sayisi - ofm.bloke_oda_sayisi) AS kalan,
                   ofm.kapali_satis
            FROM oda_fiyat_musaitlik ofm
            INNER JOIN oda_tipleri ot ON ot.id = ofm.oda_tip_id
            WHERE ot.otel_id = @hotelId
              AND ofm.otel_id = @hotelId
              AND ofm.tarih BETWEEN CAST(GETDATE() AS date) AND DATEADD(DAY, 14, CAST(GETDATE() AS date))
              AND ((ofm.toplam_oda_sayisi - ofm.satilan_oda_sayisi - ofm.bloke_oda_sayisi) <= 2 OR ofm.kapali_satis = 1)
            ORDER BY ofm.kapali_satis DESC, kalan ASC, ofm.tarih ASC
            OFFSET 0 ROWS FETCH NEXT 8 ROWS ONLY;";

        var alerts = new List<PartnerInventoryAlertViewModel>();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@hotelId", hotelId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var isClosed = SafeBool(reader, 4);
            var remaining = SafeInt(reader, 3);
            alerts.Add(new PartnerInventoryAlertViewModel
            {
                RoomId = reader.GetInt64(0),
                RoomName = reader.GetString(1),
                DateText = FormatDateTime(reader.GetDateTime(2)).Split(' ')[0],
                AvailabilityText = isClosed ? "Satisa kapali" : $"{remaining} oda kaldi",
                ToneClass = isClosed ? "danger" : remaining == 0 ? "danger" : "warning"
            });
        }

        return alerts;
    }

    private static void AddReviewFilterParameters(SqlCommand command, long hotelId, string? status, string? replyState)
    {
        command.Parameters.AddWithValue("@hotelId", hotelId);
        command.Parameters.AddWithValue("@status", string.IsNullOrWhiteSpace(status) ? string.Empty : status.Trim());
        command.Parameters.AddWithValue("@replyState", string.IsNullOrWhiteSpace(replyState) ? string.Empty : replyState.Trim().ToLowerInvariant());
    }

    private async Task<int> CountReviewsAsync(SqlConnection connection, long hotelId, string? status, string? replyState, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT COUNT(1)
            FROM yorumlar y
            WHERE y.otel_id = @hotelId
              AND (@status = N'' OR y.onay_durumu = @status)
              AND (
                    @replyState = N''
                    OR (@replyState = N'answered' AND COALESCE(y.otel_yaniti, N'') <> N'')
                    OR (@replyState = N'unanswered' AND COALESCE(y.otel_yaniti, N'') = N'')
                  );";

        await using var command = new SqlCommand(sql, connection);
        AddReviewFilterParameters(command, hotelId, status, replyState);
        var value = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(value ?? 0, CultureInfo.InvariantCulture);
    }

    private async Task<List<PartnerReviewRowViewModel>> LoadReviewsAsync(SqlConnection connection, long hotelId, string? status, string? replyState, int page, int pageSize, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT y.id, COALESCE(u.ad_soyad, 'Misafir'), COALESCE(y.yorum_basligi, 'Yorum'), y.genel_puan, y.onay_durumu, y.olusturulma_tarihi, y.yorum_metni, y.otel_yaniti
            FROM yorumlar y
            LEFT JOIN users u ON u.id = y.kullanici_id
            WHERE y.otel_id = @hotelId
              AND (@status = N'' OR y.onay_durumu = @status)
              AND (
                    @replyState = N''
                    OR (@replyState = N'answered' AND COALESCE(y.otel_yaniti, N'') <> N'')
                    OR (@replyState = N'unanswered' AND COALESCE(y.otel_yaniti, N'') = N'')
                  )
            ORDER BY y.olusturulma_tarihi DESC
            OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY;";

        var items = new List<PartnerReviewRowViewModel>();
        var blockedWords = await LoadBlockedReviewWordsAsync(connection, cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        AddReviewFilterParameters(command, hotelId, status, replyState);
        command.Parameters.AddWithValue("@offset", Math.Max(0, page - 1) * pageSize);
        command.Parameters.AddWithValue("@pageSize", pageSize);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var rawComment = reader.IsDBNull(6) ? string.Empty : NormalizeTurkishText(reader.GetString(6));
            items.Add(new PartnerReviewRowViewModel
            {
                ReviewId = reader.GetInt64(0),
                GuestName = NormalizeTurkishText(reader.GetString(1)),
                Title = NormalizeTurkishText(reader.GetString(2)),
                ScoreText = $"{SafeByte(reader, 3)} / 5",
                StatusText = NormalizeTurkishText(reader.GetString(4)),
                CreatedText = FormatDateTime(reader.IsDBNull(5) ? null : reader.GetDateTime(5)),
                CreatedAtUtc = reader.IsDBNull(5) ? null : reader.GetDateTime(5),
                Comment = ReviewTextFilter.MaskBlockedWords(rawComment, blockedWords),
                ResponseText = reader.IsDBNull(7) ? null : NormalizeTurkishText(reader.GetString(7))
            });
        }

        return items;
    }

    private static async Task<IReadOnlyList<string>> LoadBlockedReviewWordsAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        var result = new List<string>();
        if (!await TableExistsAsync(connection, "blockyorumkelime", cancellationToken))
        {
            return result;
        }

        const string sql = @"SELECT kelime FROM dbo.blockyorumkelime WHERE aktif_mi = 1 ORDER BY id DESC;";
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
            var utf8Text = Encoding.UTF8.GetString(latinBytes);
            return string.IsNullOrWhiteSpace(utf8Text) ? text : utf8Text;
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

    private static int SafeInt(SqlDataReader reader, int ordinal)
        => reader.IsDBNull(ordinal) ? 0 : Convert.ToInt32(reader.GetValue(ordinal), CultureInfo.InvariantCulture);

    private static short SafeShort(SqlDataReader reader, int ordinal)
        => reader.IsDBNull(ordinal) ? (short)0 : Convert.ToInt16(reader.GetValue(ordinal), CultureInfo.InvariantCulture);

    private static byte SafeByte(SqlDataReader reader, int ordinal)
        => reader.IsDBNull(ordinal) ? (byte)0 : Convert.ToByte(reader.GetValue(ordinal), CultureInfo.InvariantCulture);

    private static bool SafeBool(SqlDataReader reader, int ordinal)
        => !reader.IsDBNull(ordinal) && Convert.ToInt32(reader.GetValue(ordinal), CultureInfo.InvariantCulture) == 1;

    private static decimal SafeDecimal(SqlDataReader reader, int ordinal)
        => reader.IsDBNull(ordinal) ? 0m : Convert.ToDecimal(reader.GetValue(ordinal), CultureInfo.InvariantCulture);

    private static string FormatMoney(decimal value)
        => string.Format(CultureInfo.GetCultureInfo("tr-TR"), "{0:C0}", value);

    private static string FormatStay(DateTime checkIn, DateTime checkOut)
        => $"{checkIn:dd.MM.yyyy} - {checkOut:dd.MM.yyyy}";

    private static string FormatDateTime(DateTime? value)
        => value.HasValue ? value.Value.ToString("dd.MM.yyyy HH:mm", CultureInfo.GetCultureInfo("tr-TR")) : "-";

    /// <summary>Kapıda tahsil tutarı varsa ve henüz ödendi olarak işaretlenmemişse girişte tahsil kapatılır.</summary>
    private static bool IsKapidaCollectionOutstanding(decimal kapidaTutar, string? kapidaDurumu)
    {
        if (kapidaTutar <= 0.009m)
        {
            return false;
        }

        var d = (kapidaDurumu ?? string.Empty).Trim();
        if (d.Equals("Uygulanmiyor", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (d.Equals("Ödendi", StringComparison.OrdinalIgnoreCase)
            || d.Equals("Odendi", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return true;
    }

    private static string ResolveAggregateOdemeDurumuAfterCheckIn(decimal toplamTutar, decimal newTahsilEdilen, string? currentOdemeDurumu)
    {
        var kalan = Math.Max(0m, toplamTutar - newTahsilEdilen);
        if (kalan <= 0.05m)
        {
            return "Tamamlandı";
        }

        if (newTahsilEdilen > 0.05m)
        {
            return "Kısmen Ödendi";
        }

        return string.IsNullOrWhiteSpace(currentOdemeDurumu) ? "Beklemede" : currentOdemeDurumu!;
    }

    private async Task ApplyPartnerCheckInPaymentSettlementAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        long hotelId,
        long reservationId,
        CancellationToken cancellationToken)
    {
        const string readSql = @"
            SELECT
                COALESCE(r.kapida_odeme_tutari, 0),
                COALESCE(r.kapida_odeme_durumu, N''),
                COALESCE(r.toplam_tutar, 0),
                COALESCE(r.tahsil_edilen_tutar, 0),
                COALESCE(r.odeme_durumu, N'')
            FROM dbo.rezervasyonlar r
            WHERE r.id = @reservationId
              AND r.otel_id = @hotelId;";

        decimal kapidaTutar;
        string kapidaDurumu;
        decimal toplamTutar;
        decimal tahsilEdilen;
        string currentOdeme;

        await using (var cmd = new SqlCommand(readSql, connection, transaction))
        {
            cmd.Parameters.AddWithValue("@reservationId", reservationId);
            cmd.Parameters.AddWithValue("@hotelId", hotelId);
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                return;
            }

            kapidaTutar = SafeDecimal(reader, 0);
            kapidaDurumu = reader.IsDBNull(1) ? string.Empty : reader.GetString(1);
            toplamTutar = SafeDecimal(reader, 2);
            tahsilEdilen = SafeDecimal(reader, 3);
            currentOdeme = reader.IsDBNull(4) ? string.Empty : reader.GetString(4);
        }

        if (!IsKapidaCollectionOutstanding(kapidaTutar, kapidaDurumu))
        {
            return;
        }

        var newTahsil = tahsilEdilen + kapidaTutar;
        var newKalan = Math.Max(0m, toplamTutar - newTahsil);
        var newAggregate = ResolveAggregateOdemeDurumuAfterCheckIn(toplamTutar, newTahsil, currentOdeme);

        const string updateSql = @"
            UPDATE dbo.rezervasyonlar
            SET
                kapida_odeme_durumu = CASE WHEN COALESCE(kapida_odeme_tutari, 0) > 0 THEN N'Ödendi' ELSE kapida_odeme_durumu END,
                tahsil_edilen_tutar = @newTahsil,
                kalan_tahsil_edilecek_tutar = @newKalan,
                odeme_durumu = @newAggregate,
                odeme_tarihi = CASE
                    WHEN @newAggregate = N'Tamamlandı' AND odeme_tarihi IS NULL THEN SYSUTCDATETIME()
                    ELSE odeme_tarihi
                END,
                guncellenme_tarihi = SYSUTCDATETIME()
            WHERE id = @reservationId
              AND otel_id = @hotelId;";

        await using (var updateCmd = new SqlCommand(updateSql, connection, transaction))
        {
            updateCmd.Parameters.AddWithValue("@newTahsil", newTahsil);
            updateCmd.Parameters.AddWithValue("@newKalan", newKalan);
            updateCmd.Parameters.AddWithValue("@newAggregate", newAggregate);
            updateCmd.Parameters.AddWithValue("@reservationId", reservationId);
            updateCmd.Parameters.AddWithValue("@hotelId", hotelId);
            await updateCmd.ExecuteNonQueryAsync(cancellationToken);
        }

        if (!await TableExistsAsync(connection, "rezervasyon_odeme_kalemleri", transaction, cancellationToken))
        {
            return;
        }

        const string linesSql = @"
            UPDATE k
            SET k.odeme_durumu_id = tam.id,
                k.tahsil_edilen_tutar = k.tutar
            FROM dbo.rezervasyon_odeme_kalemleri k
            INNER JOIN dbo.odeme_yontemi_tanimlari y ON y.id = k.odeme_yontemi_id
            INNER JOIN dbo.odeme_durumu_tanimlari tam ON tam.kod = N'TAMAMLANDI'
            WHERE k.rezervasyon_id = @reservationId
              AND y.kod IN (N'KAPIDA_ODEME', N'NAKIT')
              AND k.odeme_durumu_id <> tam.id;";

        await using (var linesCmd = new SqlCommand(linesSql, connection, transaction))
        {
            linesCmd.Parameters.AddWithValue("@reservationId", reservationId);
            await linesCmd.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    private async Task UpsertCommissionAccountingAfterCheckInAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        long hotelId,
        long reservationId,
        CancellationToken cancellationToken)
    {
        if (!await TableExistsAsync(connection, "komisyon_muhasebe_kayitlari", transaction, cancellationToken))
        {
            return;
        }

        const string snapshotSql = @"
            SELECT TOP (1)
                COALESCE(r.toplam_tutar, 0) AS toplam_tutar,
                COALESCE(r.komisyon_orani, 0) AS komisyon_orani,
                COALESCE(r.komisyon_tutari, 0) AS komisyon_tutari,
                COALESCE(r.otele_odenecek_tutar, 0) AS otele_odenecek_tutar,
                COALESCE(r.giris_tarihi, CAST(GETDATE() AS date)) AS giris_tarihi,
                COALESCE(NULLIF(r.rezervasyon_no, ''), CAST(r.id AS nvarchar(30))) AS rezervasyon_no,
                COALESCE(o.partner_id, 0) AS partner_id
            FROM dbo.rezervasyonlar r
            INNER JOIN dbo.oteller o ON o.id = r.otel_id
            WHERE r.id = @reservationId
              AND r.otel_id = @hotelId;";

        decimal toplamTutar;
        decimal komisyonOrani;
        decimal komisyonTutari;
        decimal netOteleOdenecek;
        DateTime girisTarihi;
        string rezervasyonNo;
        long partnerId;

        await using (var cmd = new SqlCommand(snapshotSql, connection, transaction))
        {
            cmd.Parameters.AddWithValue("@reservationId", reservationId);
            cmd.Parameters.AddWithValue("@hotelId", hotelId);
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                return;
            }

            toplamTutar = SafeDecimal(reader, 0);
            komisyonOrani = SafeDecimal(reader, 1);
            komisyonTutari = SafeDecimal(reader, 2);
            netOteleOdenecek = SafeDecimal(reader, 3);
            girisTarihi = reader.GetDateTime(4);
            rezervasyonNo = reader.GetString(5);
            partnerId = reader.IsDBNull(6) ? 0 : Convert.ToInt64(reader.GetValue(6), CultureInfo.InvariantCulture);
        }

        if (partnerId <= 0)
        {
            // Otel -> partner bağlanmamışsa muhasebe kaydı açma; admin bunu ayrıca düzeltmeli.
            return;
        }

        var donem = girisTarihi.ToString("yyyy-MM", CultureInfo.InvariantCulture);
        var kayitNo = $"KOM-{reservationId}";

        const string upsertSql = @"
            IF EXISTS (SELECT 1 FROM dbo.komisyon_muhasebe_kayitlari WHERE rezervasyon_id = @reservationId)
            BEGIN
                UPDATE dbo.komisyon_muhasebe_kayitlari
                SET
                    otel_id = @hotelId,
                    partner_id = @partnerId,
                    kayit_tarihi = COALESCE(kayit_tarihi, CAST(SYSUTCDATETIME() AS date)),
                    donem = COALESCE(NULLIF(donem, ''), @donem),
                    toplam_rezervasyon_tutari = COALESCE(@toplamTutar, toplam_rezervasyon_tutari),
                    komisyon_orani = COALESCE(@komisyonOrani, komisyon_orani),
                    komisyon_tutari = COALESCE(@komisyonTutari, komisyon_tutari),
                    net_otele_odenecek = COALESCE(@netOteleOdenecek, net_otele_odenecek),
                    otele_odeme_durumu = COALESCE(NULLIF(otele_odeme_durumu, ''), N'Beklemede'),
                    mutabakat_durumu = COALESCE(NULLIF(mutabakat_durumu, ''), N'Beklemede'),
                    guncellenme_tarihi = SYSUTCDATETIME()
                WHERE rezervasyon_id = @reservationId;
            END
            ELSE
            BEGIN
                INSERT INTO dbo.komisyon_muhasebe_kayitlari
                (
                    kayit_no, kayit_tarihi, donem,
                    rezervasyon_id, otel_id, partner_id,
                    toplam_rezervasyon_tutari, komisyon_orani, komisyon_tutari, ek_kesintiler, net_otele_odenecek,
                    otele_odeme_durumu, mutabakat_durumu, olusturulma_tarihi
                )
                VALUES
                (
                    @kayitNo, CAST(SYSUTCDATETIME() AS date), @donem,
                    @reservationId, @hotelId, @partnerId,
                    @toplamTutar, @komisyonOrani, @komisyonTutari, 0, @netOteleOdenecek,
                    N'Beklemede', N'Beklemede', SYSUTCDATETIME()
                );
            END";

        await using var upsert = new SqlCommand(upsertSql, connection, transaction);
        upsert.Parameters.AddWithValue("@kayitNo", kayitNo);
        upsert.Parameters.AddWithValue("@donem", donem);
        upsert.Parameters.AddWithValue("@reservationId", reservationId);
        upsert.Parameters.AddWithValue("@hotelId", hotelId);
        upsert.Parameters.AddWithValue("@partnerId", partnerId);
        upsert.Parameters.AddWithValue("@toplamTutar", toplamTutar);
        upsert.Parameters.AddWithValue("@komisyonOrani", komisyonOrani);
        upsert.Parameters.AddWithValue("@komisyonTutari", komisyonTutari);
        upsert.Parameters.AddWithValue("@netOteleOdenecek", netOteleOdenecek);
        await upsert.ExecuteNonQueryAsync(cancellationToken);
    }

    private static string GetPartnerReservationDisplayStatus(string status, string hotelApprovalStatus)
    {
        if (string.Equals(status, "Tamamlandı", StringComparison.OrdinalIgnoreCase)) return "Giriş Yaptı";
        if (string.Equals(status, "İptal Edildi", StringComparison.OrdinalIgnoreCase) || string.Equals(hotelApprovalStatus, "Reddedildi", StringComparison.OrdinalIgnoreCase)) return "İptal";
        if (string.Equals(status, "Onaylandı", StringComparison.OrdinalIgnoreCase) || string.Equals(hotelApprovalStatus, "Onaylandı", StringComparison.OrdinalIgnoreCase)) return "Onaylandı";
        return "Bekliyor";
    }

    private static string GetPartnerReservationStatusTone(string status, string hotelApprovalStatus)
    {
        if (string.Equals(status, "Tamamlandı", StringComparison.OrdinalIgnoreCase)) return "checked-in";
        if (string.Equals(status, "İptal Edildi", StringComparison.OrdinalIgnoreCase) || string.Equals(hotelApprovalStatus, "Reddedildi", StringComparison.OrdinalIgnoreCase)) return "cancelled";
        if (string.Equals(status, "Onaylandı", StringComparison.OrdinalIgnoreCase) || string.Equals(hotelApprovalStatus, "Onaylandı", StringComparison.OrdinalIgnoreCase)) return "approved";
        return "pending";
    }

    private static string SplitFirstName(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            return "Misafir";
        }

        return fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "Misafir";
    }

    private static string NormalizeReservationStatusFilter(string? status)
    {
        var normalized = (status ?? string.Empty).Trim().ToLowerInvariant();
        return normalized switch
        {
            "pending" => "pending",
            "approved" => "approved",
            "completed" => "completed",
            "rejected" => "rejected",
            _ => "all"
        };
    }

    private static string NormalizeReservationPaymentFilter(string? paymentMethod)
    {
        var normalized = (paymentMethod ?? string.Empty).Trim().ToLowerInvariant();
        return normalized switch
        {
            "card" => "card",
            "cash" => "cash",
            "transfer" => "transfer",
            "mixed" => "mixed",
            _ => "all"
        };
    }

    private static decimal ParseMoney(string value)
    {
        var cleaned = value
            .Replace("₺", string.Empty, StringComparison.Ordinal)
            .Replace(".", string.Empty, StringComparison.Ordinal)
            .Replace(",", ".", StringComparison.Ordinal)
            .Trim();

        return decimal.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture, out var amount) ? amount : 0m;
    }

    private static int ParseStock(string value)
    {
        var digits = new string(value.Where(char.IsDigit).ToArray());
        return int.TryParse(digits, NumberStyles.Integer, CultureInfo.InvariantCulture, out var stock) ? stock : 0;
    }

    private static string GuessRoomFeatureIcon(string featureName)
    {
        var normalized = (featureName ?? string.Empty).ToLowerInvariant();

        if (normalized.Contains("smart", StringComparison.Ordinal) || normalized.Contains("tv", StringComparison.Ordinal))
        {
            return "fa-tv";
        }

        if (normalized.Contains("jakuzi", StringComparison.Ordinal) || normalized.Contains("spa", StringComparison.Ordinal))
        {
            return "fa-hot-tub-person";
        }

        if (normalized.Contains("kettle", StringComparison.Ordinal) || normalized.Contains("kahve", StringComparison.Ordinal) || normalized.Contains("çay", StringComparison.Ordinal) || normalized.Contains("cay", StringComparison.Ordinal))
        {
            return "fa-mug-hot";
        }

        if (normalized.Contains("havlu", StringComparison.Ordinal) || normalized.Contains("banyo", StringComparison.Ordinal))
        {
            return "fa-bath";
        }

        if (normalized.Contains("wifi", StringComparison.Ordinal) || normalized.Contains("internet", StringComparison.Ordinal))
        {
            return "fa-wifi";
        }

        if (normalized.Contains("balkon", StringComparison.Ordinal) || normalized.Contains("teras", StringComparison.Ordinal))
        {
            return "fa-door-open";
        }

        if (normalized.Contains("manzara", StringComparison.Ordinal) || normalized.Contains("deniz", StringComparison.Ordinal) || normalized.Contains("sehir", StringComparison.Ordinal) || normalized.Contains("şehir", StringComparison.Ordinal))
        {
            return "fa-water";
        }

        if (normalized.Contains("mini bar", StringComparison.Ordinal) || normalized.Contains("buzdolabi", StringComparison.Ordinal) || normalized.Contains("buzdolabı", StringComparison.Ordinal))
        {
            return "fa-champagne-glasses";
        }

        if (normalized.Contains("klima", StringComparison.Ordinal))
        {
            return "fa-wind";
        }

        return "fa-circle-check";
    }

    private static string BuildRoomCode(long hotelId)
        => $"RM{hotelId:D4}{DateTime.UtcNow:MMddHHmmss}";

    private static string TruncateText(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        if (maxLength <= 0 || value.Length <= maxLength)
        {
            return value;
        }

        return value[..maxLength].TrimEnd();
    }

    private static string FormatDate(DateTime value)
        => value.ToString("dd.MM.yyyy HH:mm", CultureInfo.GetCultureInfo("tr-TR"));

    private static string? EmptyToNull(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string MapPartnerApprovalTone(string status)
    {
        return status switch
        {
            "Onaylandi" => "success",
            "Reddedildi" => "danger",
            "Askida" or "Kara Liste" => "danger",
            _ => "warning"
        };
    }

    private static void BindPartnerApplicationParameters(SqlCommand command, PartnerApplicationProfileForm request, long userId)
    {
        command.Parameters.AddWithValue("@partnerId", request.PartnerId);
        command.Parameters.AddWithValue("@hotelId", request.HotelId);
        command.Parameters.AddWithValue("@userId", userId);
        command.Parameters.AddWithValue("@companyName", request.CompanyName.Trim());
        command.Parameters.AddWithValue("@companyType", request.CompanyType.Trim());
        command.Parameters.AddWithValue("@hotelName", request.HotelName.Trim());
        command.Parameters.AddWithValue("@contactName", request.ContactName.Trim());
        command.Parameters.AddWithValue("@contactTitle", (object?)EmptyToNull(request.ContactTitle) ?? DBNull.Value);
        command.Parameters.AddWithValue("@email", request.Email.Trim().ToLowerInvariant());
        command.Parameters.AddWithValue("@phone", request.Phone.Trim());
        command.Parameters.AddWithValue("@taxOffice", request.TaxOffice.Trim());
        command.Parameters.AddWithValue("@taxNumber", request.TaxNumber.Trim());
        command.Parameters.AddWithValue("@contactTcNo", request.ContactTcNo.Trim());
        command.Parameters.AddWithValue("@address", request.Address.Trim());
        command.Parameters.AddWithValue("@city", request.City.Trim());
        command.Parameters.AddWithValue("@district", request.District.Trim());
        command.Parameters.AddWithValue("@neighborhood", (object?)EmptyToNull(request.Neighborhood) ?? DBNull.Value);
        command.Parameters.AddWithValue("@bankName", request.BankName.Trim());
        command.Parameters.AddWithValue("@bankBranch", (object?)EmptyToNull(request.BankBranch) ?? DBNull.Value);
        command.Parameters.AddWithValue("@iban", request.Iban.Trim().Replace(" ", string.Empty, StringComparison.Ordinal));
        command.Parameters.AddWithValue("@website", (object?)EmptyToNull(request.Website) ?? DBNull.Value);
        command.Parameters.AddWithValue("@description", (object?)EmptyToNull(request.Description) ?? DBNull.Value);
    }

    private async Task<List<PartnerApplicationDocumentViewModel>> LoadPartnerApplicationDocumentsAsync(SqlConnection connection, long userId, long partnerId, CancellationToken cancellationToken)
    {
        var documents = new List<PartnerApplicationDocumentViewModel>();
        if (!await TableExistsAsync(connection, "partner_basvuru_evraklari", cancellationToken))
        {
            return documents;
        }

        const string sql = @"
            SELECT ped.id, ped.guvenli_dosya_id, ped.evrak_tipi, COALESCE(ped.belge_basligi, ped.evrak_tipi),
                   COALESCE(gfv.orijinal_dosya_adi, 'Belge'), ped.durum, ped.olusturulma_tarihi, COALESCE(ped.red_nedeni, '')
            FROM partner_basvuru_evraklari ped
            INNER JOIN guvenli_dosya_varliklari gfv ON gfv.id = ped.guvenli_dosya_id
            WHERE ped.partner_id = @partnerId
            ORDER BY ped.olusturulma_tarihi DESC;";

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@partnerId", partnerId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var fileId = reader.GetInt64(1);
            documents.Add(new PartnerApplicationDocumentViewModel
            {
                DocumentId = reader.GetInt64(0),
                SecureFileId = fileId,
                DocumentType = reader.GetString(2),
                Title = reader.GetString(3),
                FileName = reader.GetString(4),
                StatusText = reader.GetString(5),
                StatusToneClass = MapPartnerApprovalTone(reader.GetString(5)),
                UploadedAtText = FormatDate(reader.GetDateTime(6)),
                ReviewNote = EmptyToNull(reader.GetString(7)),
                AccessUrl = await _secureFileService.CreateAccessUrlAsync(fileId, userId, "partner", cancellationToken)
            });
        }

        return documents;
    }

    private static string BuildInvoiceHtml(
        string invoiceNo,
        DateTime? invoiceDate,
        string? invoiceType,
        string? invoiceStatus,
        decimal totalAmount,
        string currency,
        string recipientName,
        string recipientAddress,
        string issuerName,
        string issuerTaxNo)
    {
        static string Safe(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "-";
            }

            return value
                .Replace("&", "&amp;", StringComparison.Ordinal)
                .Replace("<", "&lt;", StringComparison.Ordinal)
                .Replace(">", "&gt;", StringComparison.Ordinal)
                .Replace("\"", "&quot;", StringComparison.Ordinal);
        }

        var formattedDate = invoiceDate?.ToString("dd.MM.yyyy", CultureInfo.GetCultureInfo("tr-TR")) ?? "-";
        var formattedTotal = string.Format(CultureInfo.GetCultureInfo("tr-TR"), "{0:N2} {1}", totalAmount, string.IsNullOrWhiteSpace(currency) ? "TRY" : currency);

        return $$"""
<!doctype html>
<html lang="tr">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>{{Safe(invoiceNo)}} Fatura</title>
    <style>
        body { font-family: Arial, sans-serif; margin: 24px; color: #1f2937; }
        .card { border: 1px solid #e5e7eb; border-radius: 12px; padding: 20px; max-width: 760px; }
        h1 { margin: 0 0 16px; font-size: 22px; }
        .grid { display: grid; grid-template-columns: 180px 1fr; gap: 8px 12px; }
        .label { font-weight: 700; color: #374151; }
        .value { color: #111827; }
        .total { margin-top: 18px; padding-top: 12px; border-top: 1px dashed #d1d5db; font-size: 20px; font-weight: 700; color: #0b4f9f; }
    </style>
</head>
<body>
    <div class="card">
        <h1>Fatura {{Safe(invoiceNo)}}</h1>
        <div class="grid">
            <div class="label">Tarih</div><div class="value">{{formattedDate}}</div>
            <div class="label">Tur</div><div class="value">{{Safe(invoiceType)}}</div>
            <div class="label">Durum</div><div class="value">{{Safe(invoiceStatus)}}</div>
            <div class="label">Alici</div><div class="value">{{Safe(recipientName)}}</div>
            <div class="label">Alici Adres</div><div class="value">{{Safe(recipientAddress)}}</div>
            <div class="label">Duzenleyen</div><div class="value">{{Safe(issuerName)}}</div>
            <div class="label">Vergi No</div><div class="value">{{Safe(issuerTaxNo)}}</div>
        </div>
        <div class="total">Toplam: {{formattedTotal}}</div>
    </div>
</body>
</html>
""";
    }

    private static string EscapeCsv(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var normalized = value.Replace("\"", "\"\"", StringComparison.Ordinal);
        return $"\"{normalized}\"";
    }

    private sealed record RoomPricingSeed(long RoomId, short TotalRooms, decimal BasePrice);

    private sealed record PricingCalendarEntry(
        long RoomId,
        DateOnly Date,
        decimal BasePrice,
        decimal? DiscountPrice,
        long? DiscountId,
        long? CampaignId,
        short? TotalRooms,
        short SoldRooms,
        short BlockedRooms,
        byte? MinStay,
        short? MaxStay,
        bool IsClosed,
        string? CampaignLabel,
        string? PriceNote);

    private sealed record CampaignSelection(long CampaignId, string CampaignName, string CampaignCode, string DisplayLabel, DateTime StartDate, DateTime EndDate);
    private sealed record DiscountSelection(long DiscountId, string DiscountName);
    private sealed record PartnerContext(PartnerHotelContext SelectedHotel, PartnerShellViewModel Shell);
    private sealed record PartnerHotelContext(long HotelId, long PartnerId, string HotelCode, string HotelName, string HotelType, string CityLabel, bool IsPrimary);
    private sealed record ReservationEmailSnapshot(long GuestUserId, string ReservationNo, string GuestName, string GuestEmail, DateTime CheckInDate, DateTime CheckOutDate, decimal TotalAmount, string RoomName, string HotelName);
}
