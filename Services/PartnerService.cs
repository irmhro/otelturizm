using System.Globalization;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using MySqlConnector;
using otelturizmnew.Models.Paneller.Partner;
using otelturizmnew.Services.Abstractions;

namespace otelturizmnew.Services;

public class PartnerService : IPartnerService
{
    private readonly string _connectionString;
    private readonly IWebHostEnvironment _environment;
    private readonly IImageStorageService _imageStorageService;

    public PartnerService(IConfiguration configuration, IWebHostEnvironment environment, IImageStorageService imageStorageService)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection tanimli degil.");
        _environment = environment;
        _imageStorageService = imageStorageService;
    }

    public async Task<PartnerDashboardViewModel> GetDashboardAsync(long userId, long? hotelId = null, CancellationToken cancellationToken = default)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var context = await BuildContextAsync(connection, userId, hotelId, "Dashboard", "Otel, fiyat, rezervasyon ve yorum operasyonlarini tek ekranda yonetin.", "dashboard", cancellationToken);
        var model = new PartnerDashboardViewModel { Shell = context.Shell };

        const string metricsSql = @"
            SELECT
                (SELECT COUNT(*) FROM otel_kullanici_sahiplikleri oks WHERE oks.user_id = @userId AND oks.aktif_mi = 1) AS managed_hotels,
                (SELECT COUNT(*) FROM rezervasyonlar r WHERE r.otel_id = @hotelId) AS total_reservations,
                (SELECT COALESCE(SUM(r.toplam_tutar), 0) FROM rezervasyonlar r WHERE r.otel_id = @hotelId AND r.durum IN ('Onaylandı','Tamamlandı')) AS total_revenue,
                (SELECT COALESCE(AVG(y.genel_puan), 0) FROM yorumlar y WHERE y.otel_id = @hotelId AND y.onay_durumu = 'Onaylandı') AS average_score;";

        await using (var command = new MySqlCommand(metricsSql, connection))
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

        const string trendSql = @"
            SELECT DATE_FORMAT(r.olusturulma_tarihi, '%b') AS ay, COUNT(*) AS rezervasyon_adedi, COALESCE(SUM(r.toplam_tutar), 0) AS gelir
            FROM rezervasyonlar r
            WHERE r.otel_id = @hotelId
              AND r.olusturulma_tarihi >= DATE_SUB(CURDATE(), INTERVAL 5 MONTH)
            GROUP BY YEAR(r.olusturulma_tarihi), MONTH(r.olusturulma_tarihi), DATE_FORMAT(r.olusturulma_tarihi, '%b')
            ORDER BY YEAR(r.olusturulma_tarihi), MONTH(r.olusturulma_tarihi);";

        var trendRows = new List<PartnerRevenuePointViewModel>();
        await using (var trendCommand = new MySqlCommand(trendSql, connection))
        {
            trendCommand.Parameters.AddWithValue("@hotelId", context.SelectedHotel.HotelId);
            await using var trendReader = await trendCommand.ExecuteReaderAsync(cancellationToken);
            while (await trendReader.ReadAsync(cancellationToken))
            {
                trendRows.Add(new PartnerRevenuePointViewModel
                {
                    Label = trendReader.GetString(0),
                    ReservationCount = SafeInt(trendReader, 1),
                    RevenueAmount = SafeDecimal(trendReader, 2)
                });
            }
        }

        var maxRevenue = Math.Max(1m, trendRows.Count == 0 ? 0m : trendRows.Max(static row => row.RevenueAmount));
        foreach (var item in trendRows)
        {
            item.HeightPercent = Math.Max(16, (int)Math.Round(item.RevenueAmount * 100m / maxRevenue));
            model.RevenueTrend.Add(item);
        }

        model.UpcomingReservations = await LoadReservationsAsync(connection, context.SelectedHotel.HotelId, context.SelectedHotel.HotelName, 8, cancellationToken);
        model.InventoryAlerts = await LoadInventoryAlertsAsync(connection, context.SelectedHotel.HotelId, cancellationToken);
        model.RecentReviews = await LoadReviewsAsync(connection, context.SelectedHotel.HotelId, 4, cancellationToken);
        model.QuickActions = new List<PartnerQuickActionViewModel>
        {
            new() { Title = "Yeni fiyat guncelle", Description = "Takvim uzerinden gunluk veya toplu fiyat aksiyonu acin.", IconClass = "fa-calendar-days", Url = $"/panel/partner/takvim-fiyatlar?otelId={context.SelectedHotel.HotelId}", ToneClass = "info" },
            new() { Title = "Oda tiplerini yonet", Description = "Oda ekle, duzenle veya pasife al.", IconClass = "fa-bed", Url = $"/panel/partner/oda-yonetimi?otelId={context.SelectedHotel.HotelId}", ToneClass = "success" },
            new() { Title = "Galeri guncelle", Description = "Yeni gorsel yukle veya kapak secimini degistir.", IconClass = "fa-images", Url = $"/panel/partner/fotograflar?otelId={context.SelectedHotel.HotelId}#fotograf-yukle", ToneClass = "warning" },
            new() { Title = "Destek talebi ac", Description = "Operasyon, odeme veya teknik sorunlar icin aninda talep olustur.", IconClass = "fa-headset", Url = $"/panel/partner/724-destek?otelId={context.SelectedHotel.HotelId}", ToneClass = "danger" }
        };
        return model;
    }

    public async Task<PartnerReservationsPageViewModel> GetReservationsAsync(long userId, long? hotelId = null, CancellationToken cancellationToken = default)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var context = await BuildContextAsync(connection, userId, hotelId, "Rezervasyonlar", "Rezervasyon akislarini, durum gecislerini ve odeme hareketlerini canli verilerle izleyin.", "reservations", cancellationToken);
        var model = new PartnerReservationsPageViewModel { Shell = context.Shell };

        const string summarySql = @"
            SELECT
                COUNT(*) AS total_count,
                SUM(CASE WHEN durum IN ('Onay Bekliyor','Değişiklik Bekliyor') THEN 1 ELSE 0 END) AS pending_count,
                SUM(CASE WHEN durum = 'Onaylandı' THEN 1 ELSE 0 END) AS approved_count,
                SUM(CASE WHEN durum = 'İptal Edildi' THEN 1 ELSE 0 END) AS cancelled_count
            FROM rezervasyonlar
            WHERE otel_id = @hotelId;";

        await using (var summaryCommand = new MySqlCommand(summarySql, connection))
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

        model.Reservations = await LoadReservationsAsync(connection, context.SelectedHotel.HotelId, context.SelectedHotel.HotelName, 100, cancellationToken);
        model.StatusForm = new PartnerReservationStatusRequest { HotelId = context.SelectedHotel.HotelId };
        model.MessageForm = new PartnerGuestMessageRequest { HotelId = context.SelectedHotel.HotelId };
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
            "reject" => "İptal Edildi",
            "pending" => "Onay Bekliyor",
            _ => "Onay Bekliyor"
        };

        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await EnsureHotelAccessAsync(connection, userId, request.HotelId, cancellationToken);

        const string sql = @"
            UPDATE rezervasyonlar
            SET durum = @status
            WHERE id = @reservationId
              AND otel_id = @hotelId;";

        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@status", status);
        command.Parameters.AddWithValue("@reservationId", request.ReservationId);
        command.Parameters.AddWithValue("@hotelId", request.HotelId);
        var affectedRows = await command.ExecuteNonQueryAsync(cancellationToken);

        return affectedRows > 0
            ? (true, $"Rezervasyon durumu '{status}' olarak guncellendi.")
            : (false, "Rezervasyon bulunamadi veya guncellenemedi.");
    }

    public async Task<(bool Success, string Message)> SendGuestMessageAsync(long userId, PartnerGuestMessageRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return (false, "Mesaj alani bos birakilamaz.");
        }

        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await EnsureHotelAccessAsync(connection, userId, request.HotelId, cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            const string reservationSql = @"
                SELECT user_id, rezervasyon_no
                FROM rezervasyonlar
                WHERE id = @reservationId AND otel_id = @hotelId
                LIMIT 1;";

            long guestUserId;
            string reservationNo;
            await using (var reservationCommand = new MySqlCommand(reservationSql, connection, (MySqlTransaction)transaction))
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
            }

            const string createConversationSql = @"
                INSERT INTO mesaj_konusmalari
                (konusma_kodu, rezervasyon_id, otel_id, misafir_kullanici_id, otel_yetkilisi_kullanici_id, konu_basligi, konu_kategorisi, durum, oncelik, son_mesaj_tarihi, son_mesaj_gonderen, son_mesaj_onizleme, otel_okunmamis_sayisi, misafir_okunmamis_sayisi)
                VALUES
                (@conversationCode, @reservationId, @hotelId, @guestUserId, @userId, @subject, 'Rezervasyon', 'Açık', 'Normal', NOW(), 'Otel', @messagePreview, 0, 1);
                SELECT LAST_INSERT_ID();";

            await using var createConversationCommand = new MySqlCommand(createConversationSql, connection, (MySqlTransaction)transaction);
            createConversationCommand.Parameters.AddWithValue("@conversationCode", $"KNM-{DateTime.UtcNow:yyyyMMddHHmmss}-{Random.Shared.Next(100, 999)}");
            createConversationCommand.Parameters.AddWithValue("@reservationId", request.ReservationId);
            createConversationCommand.Parameters.AddWithValue("@hotelId", request.HotelId);
            createConversationCommand.Parameters.AddWithValue("@guestUserId", guestUserId);
            createConversationCommand.Parameters.AddWithValue("@userId", userId);
            createConversationCommand.Parameters.AddWithValue("@subject", string.IsNullOrWhiteSpace(request.Subject) ? $"Rezervasyon {reservationNo}" : request.Subject.Trim());
            createConversationCommand.Parameters.AddWithValue("@messagePreview", TruncateText(request.Message.Trim(), 100));
            var conversationId = Convert.ToInt64(await createConversationCommand.ExecuteScalarAsync(cancellationToken), CultureInfo.InvariantCulture);

            const string insertMessageSql = @"
                INSERT INTO mesajlar
                (konusma_id, gonderen_turu, gonderen_kullanici_id, gonderen_otel_id, mesaj_metni, mesaj_tipi, okundu_mu, durum, gonderim_tarihi)
                VALUES
                (@conversationId, 'Otel', @userId, @hotelId, @message, 'Metin', 0, 'Gönderildi', NOW());";

            await using (var insertMessageCommand = new MySqlCommand(insertMessageSql, connection, (MySqlTransaction)transaction))
            {
                insertMessageCommand.Parameters.AddWithValue("@conversationId", conversationId);
                insertMessageCommand.Parameters.AddWithValue("@userId", userId);
                insertMessageCommand.Parameters.AddWithValue("@hotelId", request.HotelId);
                insertMessageCommand.Parameters.AddWithValue("@message", request.Message.Trim());
                await insertMessageCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
            return (true, "Misafire mesaj gonderildi.");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            return (false, $"Misafire mesaj gonderilemedi: {ex.Message}");
        }
    }

    public async Task<string> ExportReservationsCsvAsync(long userId, long? hotelId = null, CancellationToken cancellationToken = default)
    {
        var model = await GetReservationsAsync(userId, hotelId, cancellationToken);
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
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var context = await BuildContextAsync(connection, userId, hotelId, "Takvim ve Fiyatlar", "Gunluk fiyat, kampanya ve musaitlik kurallarini oda bazli takvim uzerinden yonetin.", "pricing", cancellationToken);
        var rooms = await GetRoomSummariesAsync(connection, context.SelectedHotel.HotelId, cancellationToken);
        var monthStart = ParseMonthStart(month);
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);

        var selectedRoomId = roomId.HasValue && rooms.Any(static item => item.RoomId > 0) && rooms.Any(item => item.RoomId == roomId.Value)
            ? roomId.Value
            : rooms.FirstOrDefault(static item => item.IsActive)?.RoomId ?? rooms.FirstOrDefault()?.RoomId;

        var pricingEntries = await LoadPricingMonthEntriesAsync(connection, context.SelectedHotel.HotelId, monthStart, monthEnd, cancellationToken);
        var campaigns = await LoadActiveCampaignOptionsAsync(connection, context.SelectedHotel.HotelId, cancellationToken);
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
            PreviousMonthKey = monthStart.AddMonths(-1).ToString("yyyy-MM", CultureInfo.InvariantCulture),
            NextMonthKey = monthStart.AddMonths(1).ToString("yyyy-MM", CultureInfo.InvariantCulture),
            Rooms = rooms,
            SummaryCards = BuildPricingSummaryCards(rooms, pricingEntries, selectedRoomId),
            CalendarDays = selectedRoomId.HasValue
                ? BuildPricingCalendarDays(rooms, pricingEntries, selectedRoomId.Value, monthStart)
                : new List<PartnerPricingDayViewModel>(),
            AvailableCampaigns = campaigns,
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

    public async Task<PartnerCampaignsPageViewModel> GetCampaignsAsync(long userId, long? hotelId = null, CancellationToken cancellationToken = default)
    {
        await using var connection = new MySqlConnection(_connectionString);
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
            || request.CampaignId.HasValue
            || !string.IsNullOrWhiteSpace(request.CampaignLabel)
            || !string.IsNullOrWhiteSpace(request.PriceNote);

        if (!hasAnyUpdate)
        {
            return (false, "En az bir fiyat, stok veya satis kuralini guncellemelisiniz.");
        }

        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        var hotel = await EnsureHotelAccessAsync(connection, userId, request.HotelId, cancellationToken);
        var campaign = await ResolveCampaignAsync(connection, request.CampaignId, cancellationToken);
        if (request.CampaignId.HasValue && request.CampaignId.Value > 0 && campaign is null)
        {
            return (false, "Secilen kampanya bulunamadi veya aktif degil.");
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
        await using (var roomCommand = new MySqlCommand(resolvedRoomSql, connection))
        {
            roomCommand.Parameters.AddWithValue("@hotelId", hotel.HotelId);
            roomCommand.Parameters.AddWithValue("@hasFilter", roomIdList.Count > 0 ? 1 : 0);
            await using var roomReader = await roomCommand.ExecuteReaderAsync(cancellationToken);
            while (await roomReader.ReadAsync(cancellationToken))
            {
                rooms.Add(new RoomPricingSeed(
                    roomReader.GetInt64(0),
                    roomReader.GetInt16(1),
                    roomReader.GetDecimal(2)));
            }
        }

        if (rooms.Count == 0)
        {
            return (false, "Guncellenecek oda tipi bulunamadi.");
        }

        var existingEntries = await LoadPricingEntriesForRangeAsync(
            connection,
            rooms.Select(static item => item.RoomId).ToList(),
            DateOnly.FromDateTime(request.DateFrom.Date),
            DateOnly.FromDateTime(request.DateTo.Date),
            cancellationToken);

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            foreach (var room in rooms)
            {
                for (var date = request.DateFrom.Date; date <= request.DateTo.Date; date = date.AddDays(1))
                {
                    var dateOnly = DateOnly.FromDateTime(date);
                    existingEntries.TryGetValue((room.RoomId, dateOnly), out var existing);

                    var basePrice = request.BasePrice ?? existing?.BasePrice ?? room.BasePrice;
                    var discountPrice = request.ClearDiscountPrice
                        ? null
                        : request.DiscountPrice ?? existing?.DiscountPrice;
                    var totalRooms = request.TotalRooms ?? existing?.TotalRooms ?? room.TotalRooms;
                    var minStay = request.MinStay ?? existing?.MinStay ?? (byte)1;
                    var maxStay = request.MaxStay ?? existing?.MaxStay ?? (short)30;
                    var isClosed = saleAction switch
                    {
                        "open" => false,
                        "close" => true,
                        _ => existing?.IsClosed ?? false
                    };
                    var campaignId = campaign?.CampaignId ?? existing?.CampaignId;
                    var campaignLabel = !string.IsNullOrWhiteSpace(request.CampaignLabel)
                        ? request.CampaignLabel.Trim()
                        : campaign?.DisplayLabel ?? existing?.CampaignLabel;
                    var priceNote = !string.IsNullOrWhiteSpace(request.PriceNote)
                        ? request.PriceNote.Trim()
                        : existing?.PriceNote;

                    const string upsertSql = @"
                        INSERT INTO oda_fiyat_musaitlik
                        (oda_tip_id, tarih, gecelik_fiyat, indirimli_fiyat, kampanya_id, toplam_oda_sayisi, satilan_oda_sayisi, bloke_oda_sayisi, minimum_geceleme, maksimum_geceleme, kapali_satis, sadece_gunubirlik, kampanya_etiketi, fiyat_notu, guncelleyen_kullanici_id)
                        VALUES
                        (@roomId, @date, @basePrice, @discountPrice, @campaignId, @stock, 0, 0, @minStay, @maxStay, @closeSale, 0, @campaignLabel, @priceNote, @updatedBy)
                        ON DUPLICATE KEY UPDATE
                            gecelik_fiyat = VALUES(gecelik_fiyat),
                            indirimli_fiyat = VALUES(indirimli_fiyat),
                            kampanya_id = VALUES(kampanya_id),
                            toplam_oda_sayisi = VALUES(toplam_oda_sayisi),
                            minimum_geceleme = VALUES(minimum_geceleme),
                            maksimum_geceleme = VALUES(maksimum_geceleme),
                            kapali_satis = VALUES(kapali_satis),
                            kampanya_etiketi = VALUES(kampanya_etiketi),
                            fiyat_notu = VALUES(fiyat_notu),
                            guncelleyen_kullanici_id = VALUES(guncelleyen_kullanici_id),
                            guncellenme_tarihi = CURRENT_TIMESTAMP;";

                    await using var upsertCommand = new MySqlCommand(upsertSql, connection, (MySqlTransaction)transaction);
                    upsertCommand.Parameters.AddWithValue("@roomId", room.RoomId);
                    upsertCommand.Parameters.AddWithValue("@date", date);
                    upsertCommand.Parameters.AddWithValue("@basePrice", basePrice);
                    upsertCommand.Parameters.AddWithValue("@discountPrice", discountPrice.HasValue ? discountPrice.Value : DBNull.Value);
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

            if (campaign is not null)
            {
                await UpsertCampaignHotelParticipationAsync(
                    connection,
                    (MySqlTransaction)transaction,
                    hotel,
                    campaign,
                    userId,
                    request.DateFrom.Date,
                    request.DateTo.Date,
                    cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
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
            CampaignId = request.CampaignId,
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

        await using var connection = new MySqlConnection(_connectionString);
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
            INSERT INTO kampanya_oteller
            (kampanya_id, otel_id, partner_id, katilim_durumu, katilim_kaynagi, baslangic_tarihi, bitis_tarihi, ozel_indirim_orani, ozel_indirim_tutari, ozel_kampanyali_fiyat, kampanya_etiketi, landing_url, partner_notu, one_cikan, siralama, partner_onay_tarihi, olusturan_kullanici_id, guncelleyen_kullanici_id)
            VALUES
            (@campaignId, @hotelId, @partnerId, 'Aktif', 'Partner', @startDate, @endDate, @discountRate, @discountAmount, @campaignPrice, @campaignLabel, @landingUrl, @partnerNote, @featured, @sortOrder, NOW(), @userId, @userId)
            ON DUPLICATE KEY UPDATE
                partner_id = VALUES(partner_id),
                katilim_durumu = 'Aktif',
                katilim_kaynagi = 'Partner',
                baslangic_tarihi = VALUES(baslangic_tarihi),
                bitis_tarihi = VALUES(bitis_tarihi),
                ozel_indirim_orani = VALUES(ozel_indirim_orani),
                ozel_indirim_tutari = VALUES(ozel_indirim_tutari),
                ozel_kampanyali_fiyat = VALUES(ozel_kampanyali_fiyat),
                kampanya_etiketi = VALUES(kampanya_etiketi),
                landing_url = VALUES(landing_url),
                partner_notu = VALUES(partner_notu),
                one_cikan = VALUES(one_cikan),
                siralama = VALUES(siralama),
                partner_onay_tarihi = NOW(),
                guncelleyen_kullanici_id = VALUES(guncelleyen_kullanici_id),
                guncellenme_tarihi = CURRENT_TIMESTAMP;";

        await using var command = new MySqlCommand(sql, connection);
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
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var hotel = await EnsureHotelAccessAsync(connection, userId, hotelId, cancellationToken);

        const string sql = @"
            UPDATE kampanya_oteller
            SET katilim_durumu = 'Pasif',
                bitis_tarihi = CASE WHEN bitis_tarihi > NOW() THEN NOW() ELSE bitis_tarihi END,
                guncelleyen_kullanici_id = @userId,
                guncellenme_tarihi = CURRENT_TIMESTAMP
            WHERE kampanya_id = @campaignId
              AND otel_id = @hotelId;";

        await using var command = new MySqlCommand(sql, connection);
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
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var context = await BuildContextAsync(connection, userId, hotelId, "Oda Yonetimi", "Oda tiplerini, kapasiteyi, gorselleri ve baz fiyatlari yonetin.", "rooms", cancellationToken);
        return new PartnerRoomManagementPageViewModel
        {
            Shell = context.Shell,
            SelectedHotelId = context.SelectedHotel.HotelId,
            Rooms = await GetRoomSummariesAsync(connection, context.SelectedHotel.HotelId, cancellationToken),
            InventoryRows = await LoadRoomInventoryRowsAsync(connection, context.SelectedHotel.HotelId, cancellationToken),
            Form = roomId.HasValue
                ? await LoadRoomFormAsync(connection, context.SelectedHotel.HotelId, roomId.Value, cancellationToken)
                : new PartnerRoomUpsertRequest { HotelId = context.SelectedHotel.HotelId }
        };
    }

    public async Task<(bool Success, string Message)> UpsertRoomAsync(long userId, PartnerRoomUpsertRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.RoomName) || request.BasePrice <= 0)
        {
            return (false, "Oda adi ve taban fiyat zorunludur.");
        }

        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        var hotel = await EnsureHotelAccessAsync(connection, userId, request.HotelId, cancellationToken);

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

            await using var updateCommand = new MySqlCommand(updateSql, connection);
            BindRoomCommand(updateCommand, request, hotel, featuresJson);
            updateCommand.Parameters.AddWithValue("@roomId", request.RoomId.Value);
            await updateCommand.ExecuteNonQueryAsync(cancellationToken);
            return (true, "Oda tipi guncellendi.");
        }

        const string insertSql = @"
            INSERT INTO oda_tipleri
            (otel_id, oda_tip_kodu, oda_adi, oda_kategorisi, maksimum_kisi_sayisi, maksimum_yetiskin_sayisi, maksimum_cocuk_sayisi, yatak_tipi, oda_metrekare, manzara_tipi, standart_gecelik_fiyat, toplam_oda_sayisi, kapak_fotografi, ozellikler, aktif_mi)
            VALUES
            (@hotelId, @roomCode, @roomName, @roomCategory, @maxPeople, @maxAdults, @maxChildren, @bedType, @roomSize, @viewType, @basePrice, @totalRooms, @coverPhoto, @features, @active);";

        await using var insertCommand = new MySqlCommand(insertSql, connection);
        BindRoomCommand(insertCommand, request, hotel, featuresJson);
        insertCommand.Parameters.AddWithValue("@roomCode", BuildRoomCode(hotel.HotelId));
        await insertCommand.ExecuteNonQueryAsync(cancellationToken);
        return (true, "Yeni oda tipi eklendi.");
    }

    public async Task<(bool Success, string Message)> DeleteRoomAsync(long userId, long hotelId, long roomId, CancellationToken cancellationToken = default)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        var hotel = await EnsureHotelAccessAsync(connection, userId, hotelId, cancellationToken);

        const string sql = @"
            UPDATE oda_tipleri
            SET aktif_mi = 0
            WHERE id = @roomId AND otel_id = @hotelId;";

        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@roomId", roomId);
        command.Parameters.AddWithValue("@hotelId", hotel.HotelId);
        var affectedRows = await command.ExecuteNonQueryAsync(cancellationToken);
        return affectedRows > 0
            ? (true, "Oda tipi pasife alindi.")
            : (false, "Silinecek oda tipi bulunamadi.");
    }

    public async Task<PartnerHotelInfoPageViewModel> GetHotelInfoAsync(long userId, long? hotelId = null, CancellationToken cancellationToken = default)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var context = await BuildContextAsync(connection, userId, hotelId, "Otel Bilgileri", "Referans paneldeki alanlarin tamami veritabani kolonlari ile yonetilir.", "hotel-info", cancellationToken);
        var model = new PartnerHotelInfoPageViewModel
        {
            Shell = context.Shell,
            Form = await LoadHotelInfoFormAsync(connection, context.SelectedHotel.HotelId, cancellationToken)
        };

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

        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        var hotel = await EnsureHotelAccessAsync(connection, userId, request.HotelId, cancellationToken);

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
        const string updateSql = @"
                UPDATE oteller
                SET otel_adi = @hotelName,
                    otel_turu = @hotelType,
                    turizm_belge_no = @tourismDocumentNo,
                    turizm_belge_turu = @tourismDocumentType,
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
                    faks = @fax,
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
                    varsayilan_komisyon_orani = @defaultCommissionRate,
                    depozito_tutari = @depositAmount,
                    depozito_iade_suresi = @depositReturnDays,
                    konusulan_diller = @spokenLanguages,
                    video_url = @videoUrl,
                    sanal_tur_url = @virtualTourUrl,
                    guncellenme_tarihi = NOW()
                WHERE id = @hotelId;";

            await using (var updateCommand = new MySqlCommand(updateSql, connection, (MySqlTransaction)transaction))
            {
                updateCommand.Parameters.AddWithValue("@hotelId", hotel.HotelId);
                updateCommand.Parameters.AddWithValue("@hotelName", request.HotelName.Trim());
                updateCommand.Parameters.AddWithValue("@hotelType", request.HotelType);
                updateCommand.Parameters.AddWithValue("@tourismDocumentNo", (object?)request.TourismDocumentNo ?? DBNull.Value);
                updateCommand.Parameters.AddWithValue("@tourismDocumentType", string.IsNullOrWhiteSpace(request.TourismDocumentType) ? DBNull.Value : request.TourismDocumentType);
                updateCommand.Parameters.AddWithValue("@shortDescription", (object?)request.ShortDescription ?? DBNull.Value);
                updateCommand.Parameters.AddWithValue("@description", (object?)request.Description ?? DBNull.Value);
                updateCommand.Parameters.AddWithValue("@address", (object?)request.Address ?? DBNull.Value);
                updateCommand.Parameters.AddWithValue("@city", (object?)request.City ?? DBNull.Value);
                updateCommand.Parameters.AddWithValue("@district", (object?)request.District ?? DBNull.Value);
                updateCommand.Parameters.AddWithValue("@neighborhood", (object?)request.Neighborhood ?? DBNull.Value);
                updateCommand.Parameters.AddWithValue("@postalCode", (object?)request.PostalCode ?? DBNull.Value);
                updateCommand.Parameters.AddWithValue("@locationDescription", (object?)request.LocationDescription ?? DBNull.Value);
                updateCommand.Parameters.AddWithValue("@latitude", request.Latitude.HasValue ? request.Latitude.Value : DBNull.Value);
                updateCommand.Parameters.AddWithValue("@longitude", request.Longitude.HasValue ? request.Longitude.Value : DBNull.Value);
                updateCommand.Parameters.AddWithValue("@website", (object?)request.Website ?? DBNull.Value);
                updateCommand.Parameters.AddWithValue("@contactEmail", (object?)request.ContactEmail ?? DBNull.Value);
                updateCommand.Parameters.AddWithValue("@hotelPhone", (object?)request.HotelPhone ?? DBNull.Value);
                updateCommand.Parameters.AddWithValue("@hotelPhone2", (object?)request.HotelPhone2 ?? DBNull.Value);
                updateCommand.Parameters.AddWithValue("@fax", (object?)request.Fax ?? DBNull.Value);
                updateCommand.Parameters.AddWithValue("@checkIn", string.IsNullOrWhiteSpace(request.CheckInTime) ? DBNull.Value : TimeSpan.Parse(request.CheckInTime));
                updateCommand.Parameters.AddWithValue("@checkOut", string.IsNullOrWhiteSpace(request.CheckOutTime) ? DBNull.Value : TimeSpan.Parse(request.CheckOutTime));
                updateCommand.Parameters.AddWithValue("@lateCheckoutAvailable", request.LateCheckoutAvailable ? 1 : 0);
                updateCommand.Parameters.AddWithValue("@lateCheckoutFee", request.LateCheckoutFee.HasValue ? request.LateCheckoutFee.Value : DBNull.Value);
                updateCommand.Parameters.AddWithValue("@earlyCheckinAvailable", request.EarlyCheckinAvailable ? 1 : 0);
                updateCommand.Parameters.AddWithValue("@earlyCheckinFee", request.EarlyCheckinFee.HasValue ? request.EarlyCheckinFee.Value : DBNull.Value);
                updateCommand.Parameters.AddWithValue("@minStay", request.MinStay);
                updateCommand.Parameters.AddWithValue("@maxStay", request.MaxStay);
                updateCommand.Parameters.AddWithValue("@starCount", request.StarCount.HasValue ? request.StarCount.Value : DBNull.Value);
                updateCommand.Parameters.AddWithValue("@totalRoomCount", request.TotalRoomCount);
                updateCommand.Parameters.AddWithValue("@totalBedCapacity", request.TotalBedCapacity.HasValue ? request.TotalBedCapacity.Value : DBNull.Value);
                updateCommand.Parameters.AddWithValue("@floorCount", request.FloorCount.HasValue ? request.FloorCount.Value : DBNull.Value);
                updateCommand.Parameters.AddWithValue("@elevatorAvailable", request.ElevatorAvailable ? 1 : 0);
                updateCommand.Parameters.AddWithValue("@elevatorCount", request.ElevatorCount);
                updateCommand.Parameters.AddWithValue("@defaultCommissionRate", request.DefaultCommissionRate);
                updateCommand.Parameters.AddWithValue("@depositAmount", request.DepositAmount.HasValue ? request.DepositAmount.Value : DBNull.Value);
                updateCommand.Parameters.AddWithValue("@depositReturnDays", request.DepositReturnDays.HasValue ? request.DepositReturnDays.Value : DBNull.Value);
                updateCommand.Parameters.AddWithValue("@spokenLanguages", string.IsNullOrWhiteSpace(request.SpokenLanguages) ? DBNull.Value : request.SpokenLanguages);
                updateCommand.Parameters.AddWithValue("@videoUrl", (object?)request.VideoUrl ?? DBNull.Value);
                updateCommand.Parameters.AddWithValue("@virtualTourUrl", (object?)request.VirtualTourUrl ?? DBNull.Value);
                await updateCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            await using (var deleteCommand = new MySqlCommand("DELETE FROM otel_ozellik_iliskileri WHERE otel_id = @hotelId;", connection, (MySqlTransaction)transaction))
            {
                deleteCommand.Parameters.AddWithValue("@hotelId", hotel.HotelId);
                await deleteCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            foreach (var amenityId in request.SelectedAmenityIds.Distinct())
            {
                await using var insertAmenity = new MySqlCommand("INSERT INTO otel_ozellik_iliskileri (otel_id, ozellik_id) VALUES (@hotelId, @amenityId);", connection, (MySqlTransaction)transaction);
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

    public async Task<PartnerPhotosPageViewModel> GetPhotosAsync(long userId, long? hotelId = null, long? photoId = null, CancellationToken cancellationToken = default)
    {
        await using var connection = new MySqlConnection(_connectionString);
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

        const string summarySql = @"
            SELECT COUNT(*) AS total_count,
                   SUM(CASE WHEN kapak_fotografi_mi = 1 THEN 1 ELSE 0 END) AS cover_count,
                   SUM(CASE WHEN onay_durumu = 'Onaylandı' THEN 1 ELSE 0 END) AS approved_count,
                   SUM(CASE WHEN one_cikan = 1 THEN 1 ELSE 0 END) AS featured_count
            FROM otel_gorselleri
            WHERE otel_id = @hotelId;";

        await using (var summaryCommand = new MySqlCommand(summarySql, connection))
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

        await using var photoCommand = new MySqlCommand(photoSql, connection);
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
                SortText = $"Sira {photoReader.GetInt16(4)}",
                Description = photoReader.IsDBNull(5) ? null : photoReader.GetString(5),
                DisplayOrder = photoReader.GetFieldValue<ushort>(4),
                IsCover = photoReader.GetBoolean(6),
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

        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        var hotel = await EnsureHotelAccessAsync(connection, userId, request.HotelId, cancellationToken);

        var targetDirectory = Path.Combine(_environment.WebRootPath, "uploads", "hotels", "partner", hotel.HotelId.ToString(CultureInfo.InvariantCulture));
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
                var relativePath = $"/uploads/hotels/partner/{hotel.HotelId}/{fileName}";
                const string insertSql = @"
                    INSERT INTO otel_gorselleri
                    (otel_id, gorsel_url, gorsel_turu, baslik, aciklama, kapak_fotografi_mi, one_cikan, siralama, boyut_kb, onay_durumu, yukleyen_kullanici_id)
                    VALUES
                    (@hotelId, @url, @photoType, @title, @description, @isCover, @featured, @sortOrder, @sizeKb, 'Onaylandı', @userId);";

                await using var insertCommand = new MySqlCommand(insertSql, connection, (MySqlTransaction)transaction);
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
                    await using var resetCoverCommand = new MySqlCommand("UPDATE otel_gorselleri SET kapak_fotografi_mi = 0 WHERE otel_id = @hotelId AND gorsel_url <> @url;", connection, (MySqlTransaction)transaction);
                    resetCoverCommand.Parameters.AddWithValue("@hotelId", hotel.HotelId);
                    resetCoverCommand.Parameters.AddWithValue("@url", relativePath);
                    await resetCoverCommand.ExecuteNonQueryAsync(cancellationToken);

                    await using var updateHotelCommand = new MySqlCommand("UPDATE oteller SET kapak_fotografi = @coverUrl WHERE id = @hotelId;", connection, (MySqlTransaction)transaction);
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
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        var hotel = await EnsureHotelAccessAsync(connection, userId, hotelId, cancellationToken);

        const string selectSql = "SELECT gorsel_url FROM otel_gorselleri WHERE id = @photoId AND otel_id = @hotelId LIMIT 1;";
        await using var selectCommand = new MySqlCommand(selectSql, connection);
        selectCommand.Parameters.AddWithValue("@photoId", photoId);
        selectCommand.Parameters.AddWithValue("@hotelId", hotel.HotelId);
        var url = await selectCommand.ExecuteScalarAsync(cancellationToken) as string;
        if (string.IsNullOrWhiteSpace(url))
        {
            return (false, "Kapak yapilacak fotograf bulunamadi.");
        }

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        await using (var updatePhotos = new MySqlCommand("UPDATE otel_gorselleri SET kapak_fotografi_mi = CASE WHEN id = @photoId THEN 1 ELSE 0 END WHERE otel_id = @hotelId;", connection, (MySqlTransaction)transaction))
        {
            updatePhotos.Parameters.AddWithValue("@photoId", photoId);
            updatePhotos.Parameters.AddWithValue("@hotelId", hotel.HotelId);
            await updatePhotos.ExecuteNonQueryAsync(cancellationToken);
        }

        await using (var updateHotel = new MySqlCommand("UPDATE oteller SET kapak_fotografi = @coverUrl WHERE id = @hotelId;", connection, (MySqlTransaction)transaction))
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

        await using var connection = new MySqlConnection(_connectionString);
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

        await using var command = new MySqlCommand(sql, connection);
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
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        var hotel = await EnsureHotelAccessAsync(connection, userId, hotelId, cancellationToken);

        const string selectSql = "SELECT gorsel_url, kapak_fotografi_mi FROM otel_gorselleri WHERE id = @photoId AND otel_id = @hotelId LIMIT 1;";
        string? relativePath = null;
        var wasCover = false;
        await using (var selectCommand = new MySqlCommand(selectSql, connection))
        {
            selectCommand.Parameters.AddWithValue("@photoId", photoId);
            selectCommand.Parameters.AddWithValue("@hotelId", hotel.HotelId);
            await using var reader = await selectCommand.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                relativePath = reader.GetString(0);
                wasCover = reader.GetBoolean(1);
            }
        }

        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return (false, "Silinecek fotograf bulunamadi.");
        }

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        await using (var deleteCommand = new MySqlCommand("DELETE FROM otel_gorselleri WHERE id = @photoId AND otel_id = @hotelId;", connection, (MySqlTransaction)transaction))
        {
            deleteCommand.Parameters.AddWithValue("@photoId", photoId);
            deleteCommand.Parameters.AddWithValue("@hotelId", hotel.HotelId);
            await deleteCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        if (wasCover)
        {
            await using var clearHotel = new MySqlCommand("UPDATE oteller SET kapak_fotografi = NULL WHERE id = @hotelId;", connection, (MySqlTransaction)transaction);
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

        await using var connection = new MySqlConnection(_connectionString);
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

        await using (var selectCommand = new MySqlCommand(sql, connection))
        {
            selectCommand.Parameters.AddWithValue("@hotelId", hotel.HotelId);
            await using var reader = await selectCommand.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                actualIds.Add(reader.GetInt64(0));
                anyCover |= reader.GetBoolean(2);
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
            await using (var deleteCommand = new MySqlCommand(deleteSql, connection, (MySqlTransaction)transaction))
            {
                deleteCommand.Parameters.AddWithValue("@hotelId", hotel.HotelId);
                await deleteCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            if (anyCover)
            {
                const string nextCoverSql = @"
                    SELECT gorsel_url
                    FROM otel_gorselleri
                    WHERE otel_id = @hotelId
                    ORDER BY one_cikan DESC, siralama ASC, id ASC
                    LIMIT 1;";

                string? nextCoverUrl = null;
                await using (var coverCommand = new MySqlCommand(nextCoverSql, connection, (MySqlTransaction)transaction))
                {
                    coverCommand.Parameters.AddWithValue("@hotelId", hotel.HotelId);
                    nextCoverUrl = await coverCommand.ExecuteScalarAsync(cancellationToken) as string;
                }

                await using (var resetCovers = new MySqlCommand("UPDATE otel_gorselleri SET kapak_fotografi_mi = 0 WHERE otel_id = @hotelId;", connection, (MySqlTransaction)transaction))
                {
                    resetCovers.Parameters.AddWithValue("@hotelId", hotel.HotelId);
                    await resetCovers.ExecuteNonQueryAsync(cancellationToken);
                }

                if (!string.IsNullOrWhiteSpace(nextCoverUrl))
                {
                    await using var promoteCover = new MySqlCommand("UPDATE otel_gorselleri SET kapak_fotografi_mi = 1 WHERE otel_id = @hotelId AND gorsel_url = @url;", connection, (MySqlTransaction)transaction);
                    promoteCover.Parameters.AddWithValue("@hotelId", hotel.HotelId);
                    promoteCover.Parameters.AddWithValue("@url", nextCoverUrl);
                    await promoteCover.ExecuteNonQueryAsync(cancellationToken);
                }

                await using var updateHotel = new MySqlCommand("UPDATE oteller SET kapak_fotografi = @coverUrl WHERE id = @hotelId;", connection, (MySqlTransaction)transaction);
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
        var dashboard = await GetDashboardAsync(userId, hotelId, cancellationToken);
        dashboard.Shell.PanelTitle = "Performans";
        dashboard.Shell.PanelSubtitle = "Gelir, doluluk, yorum ve rakip verilerini secili tesis bazinda izleyin.";
        dashboard.Shell.ActiveSectionKey = "performance";

        await using var connection = new MySqlConnection(_connectionString);
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

        await using var connection = new MySqlConnection(_connectionString);
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

            await using var updateCommand = new MySqlCommand(updateSql, connection);
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

        await using var insertCommand = new MySqlCommand(insertSql, connection);
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

    public async Task<PartnerReviewsPageViewModel> GetReviewsAsync(long userId, long? hotelId = null, CancellationToken cancellationToken = default)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        var context = await BuildContextAsync(connection, userId, hotelId, "Degerlendirmeler", "Misafir yorumlarini, yanitsiz kayitlari ve hizmet iyilestirme sinyallerini yonetin.", "reviews", cancellationToken);
        var model = new PartnerReviewsPageViewModel
        {
            Shell = context.Shell,
            ReplyForm = new PartnerReviewReplyRequest { HotelId = context.SelectedHotel.HotelId },
            Reviews = await LoadReviewsAsync(connection, context.SelectedHotel.HotelId, 100, cancellationToken)
        };
        const string summarySql = @"
            SELECT COUNT(*) AS total_count,
                   SUM(CASE WHEN COALESCE(otel_yaniti,'') = '' THEN 1 ELSE 0 END) AS unanswered_count,
                   SUM(CASE WHEN onay_durumu = 'Beklemede' THEN 1 ELSE 0 END) AS pending_count,
                   COALESCE(AVG(genel_puan), 0) AS average_score
            FROM yorumlar
            WHERE otel_id = @hotelId;";

        await using (var summaryCommand = new MySqlCommand(summarySql, connection))
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

        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await EnsureHotelAccessAsync(connection, userId, request.HotelId, cancellationToken);

        const string sql = @"
            UPDATE yorumlar
            SET otel_yaniti = @responseText,
                otel_yaniti_tarihi = NOW(),
                yanitlayan_kullanici_id = @userId
            WHERE id = @reviewId AND otel_id = @hotelId;";

        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@responseText", request.ResponseText.Trim());
        command.Parameters.AddWithValue("@userId", userId);
        command.Parameters.AddWithValue("@reviewId", request.ReviewId);
        command.Parameters.AddWithValue("@hotelId", request.HotelId);
        await command.ExecuteNonQueryAsync(cancellationToken);
        return (true, "Yorum yaniti kaydedildi.");
    }

    public async Task<(bool Success, string Message)> ReportReviewAsync(long userId, PartnerReviewReportRequest request, CancellationToken cancellationToken = default)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await EnsureHotelAccessAsync(connection, userId, request.HotelId, cancellationToken);

        const string sql = @"
            UPDATE yorumlar
            SET onay_durumu = 'Beklemede'
            WHERE id = @reviewId AND otel_id = @hotelId;";

        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@reviewId", request.ReviewId);
        command.Parameters.AddWithValue("@hotelId", request.HotelId);
        var affectedRows = await command.ExecuteNonQueryAsync(cancellationToken);

        return affectedRows > 0
            ? (true, "Yorum raporlanmak uzere beklemeye alindi.")
            : (false, "Yorum bulunamadi.");
    }

    public async Task<PartnerFinancePageViewModel> GetFinanceAsync(long userId, long? hotelId = null, CancellationToken cancellationToken = default)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        var context = await BuildContextAsync(connection, userId, hotelId, "Finans", "Tahsilat, komisyon ve otele net odenecek akislari secili tesis bazinda izleyin.", "finance", cancellationToken);
        var model = new PartnerFinancePageViewModel { Shell = context.Shell };

        const string summarySql = @"
            SELECT
                COALESCE(SUM(r.toplam_tutar), 0) AS gross_revenue,
                COALESCE(SUM(r.komisyon_tutari), 0) AS commission_total,
                COALESCE(SUM(r.otele_odenecek_tutar), 0) AS payout_total,
                SUM(CASE WHEN r.odeme_durumu IN ('Beklemede','Ön Ödeme Alındı') THEN 1 ELSE 0 END) AS pending_payments
            FROM rezervasyonlar r
            WHERE r.otel_id = @hotelId;";

        await using (var summaryCommand = new MySqlCommand(summarySql, connection))
        {
            summaryCommand.Parameters.AddWithValue("@hotelId", context.SelectedHotel.HotelId);
            await using var reader = await summaryCommand.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                model.SummaryCards.Add(new PartnerStatCardViewModel { Label = "Brut Ciro", Value = FormatMoney(SafeDecimal(reader, 0)), Description = "Tum rezervasyon toplam tutari", IconClass = "fa-money-bill-wave", ToneClass = "info" });
                model.SummaryCards.Add(new PartnerStatCardViewModel { Label = "Platform Komisyonu", Value = FormatMoney(SafeDecimal(reader, 1)), Description = "Rezervasyon bazli kesilen komisyon", IconClass = "fa-percent", ToneClass = "warning" });
                model.SummaryCards.Add(new PartnerStatCardViewModel { Label = "Net Odeme", Value = FormatMoney(SafeDecimal(reader, 2)), Description = "Otele aktarilacak toplu bakiye", IconClass = "fa-building-columns", ToneClass = "success" });
                model.SummaryCards.Add(new PartnerStatCardViewModel { Label = "Bekleyen Odeme", Value = SafeInt(reader, 3).ToString(), Description = "Odeme durumu kapanmamis islem", IconClass = "fa-hourglass-half", ToneClass = "danger" });
            }
        }

        const string transactionsSql = @"
            SELECT rezervasyon_no, COALESCE(odeme_tarihi, olusturulma_tarihi) AS hareket_tarihi, odeme_durumu, otele_odenecek_tutar, misafir_ad_soyad
            FROM rezervasyonlar
            WHERE otel_id = @hotelId
            ORDER BY COALESCE(odeme_tarihi, olusturulma_tarihi) DESC, id DESC
            LIMIT 12;";

        await using (var transactionsCommand = new MySqlCommand(transactionsSql, connection))
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
                    DetailText = reader.IsDBNull(4) ? "Misafir bilgisi yok" : reader.GetString(4)
                });
            }
        }

        model.PayoutNote = "Komisyon ve net odeme hesaplari rezervasyon anindaki kayitli komisyon oranlari uzerinden uretilir.";
        return model;
    }

    public async Task<(bool Success, string Message)> SaveBankInfoAsync(long userId, PartnerBankInfoForm request, CancellationToken cancellationToken = default)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await EnsureHotelAccessAsync(connection, userId, request.HotelId, cancellationToken);

        if (string.IsNullOrWhiteSpace(request.BankName) || string.IsNullOrWhiteSpace(request.Iban) || string.IsNullOrWhiteSpace(request.AccountHolderName))
        {
            return (false, "Banka, IBAN ve hesap sahibi alanlari zorunludur.");
        }

        const string sql = @"
            INSERT INTO otel_odeme_bilgileri
            (otel_id, banka_adi, sube_adi, iban, hesap_sahibi, para_birimi, guncellenme_tarihi)
            VALUES
            (@hotelId, @bankName, @branchName, @iban, @accountHolder, @currency, NOW())
            ON DUPLICATE KEY UPDATE
                banka_adi = VALUES(banka_adi),
                sube_adi = VALUES(sube_adi),
                iban = VALUES(iban),
                hesap_sahibi = VALUES(hesap_sahibi),
                para_birimi = VALUES(para_birimi),
                guncellenme_tarihi = NOW();";

        try
        {
            await using var command = new MySqlCommand(sql, connection);
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
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await EnsureHotelAccessAsync(connection, userId, hotelId, cancellationToken);

        const string sql = @"
            SELECT fatura_no, fatura_tarihi, fatura_turu, fatura_durumu, genel_toplam, para_birimi,
                   fatura_alici_unvan, fatura_alici_adres, fatura_kesen_unvan, fatura_kesen_vergi_no, fatura_pdf_yolu
            FROM faturalar
            WHERE id = @invoiceId AND otel_id = @hotelId
            LIMIT 1;";

        await using var command = new MySqlCommand(sql, connection);
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

    public async Task<PartnerPreferencesPageViewModel> GetPreferencesAsync(long userId, long? hotelId = null, CancellationToken cancellationToken = default)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        var context = await BuildContextAsync(connection, userId, hotelId, "Tercihler", "Bildirim, cihaz ve panel davranislarini kullanici bazinda yonetin.", "preferences", cancellationToken);
        var model = new PartnerPreferencesPageViewModel
        {
            Shell = context.Shell,
            Form = new PartnerPreferencesForm { HotelId = context.SelectedHotel.HotelId, DefaultHotelId = context.SelectedHotel.HotelId }
        };

        const string sql = @"
            SELECT varsayilan_otel_id, dil, para_birimi, zaman_dilimi, takvim_gorunumu,
                   email_bildirimleri, sms_bildirimleri, push_bildirimleri, masaustu_bildirimleri,
                   yeni_rezervasyon_bildirimi, iptal_bildirimi, odeme_bildirimi, yorum_bildirimi,
                   otomatik_fiyat_onerileri, otomatik_kapali_gun_uyarisi, cihazi_hatirla
            FROM partner_panel_tercihleri
            WHERE kullanici_id = @userId
            LIMIT 1;";

        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@userId", userId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            model.Form = new PartnerPreferencesForm
            {
                HotelId = context.SelectedHotel.HotelId,
                DefaultHotelId = reader.IsDBNull(0) ? context.SelectedHotel.HotelId : reader.GetInt64(0),
                Language = reader.IsDBNull(1) ? "tr" : reader.GetString(1),
                Currency = reader.IsDBNull(2) ? "TRY" : reader.GetString(2),
                Timezone = reader.IsDBNull(3) ? "Europe/Istanbul" : reader.GetString(3),
                CalendarView = reader.IsDBNull(4) ? "Aylik" : reader.GetString(4),
                EmailNotifications = !reader.IsDBNull(5) && reader.GetBoolean(5),
                SmsNotifications = !reader.IsDBNull(6) && reader.GetBoolean(6),
                PushNotifications = !reader.IsDBNull(7) && reader.GetBoolean(7),
                DesktopNotifications = !reader.IsDBNull(8) && reader.GetBoolean(8),
                NewReservationNotifications = !reader.IsDBNull(9) && reader.GetBoolean(9),
                CancellationNotifications = !reader.IsDBNull(10) && reader.GetBoolean(10),
                PaymentNotifications = !reader.IsDBNull(11) && reader.GetBoolean(11),
                ReviewNotifications = !reader.IsDBNull(12) && reader.GetBoolean(12),
                AutoPriceSuggestions = !reader.IsDBNull(13) && reader.GetBoolean(13),
                AutoCloseoutWarnings = !reader.IsDBNull(14) && reader.GetBoolean(14),
                RememberDevice = !reader.IsDBNull(15) && reader.GetBoolean(15)
            };
        }

        return model;
    }

    public async Task<(bool Success, string Message)> SavePreferencesAsync(long userId, PartnerPreferencesForm request, CancellationToken cancellationToken = default)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        var hotel = await EnsureHotelAccessAsync(connection, userId, request.HotelId, cancellationToken);

        const string sql = @"
            INSERT INTO partner_panel_tercihleri
            (kullanici_id, partner_id, varsayilan_otel_id, dil, para_birimi, zaman_dilimi, takvim_gorunumu,
             email_bildirimleri, sms_bildirimleri, push_bildirimleri, masaustu_bildirimleri,
             yeni_rezervasyon_bildirimi, iptal_bildirimi, odeme_bildirimi, yorum_bildirimi,
             otomatik_fiyat_onerileri, otomatik_kapali_gun_uyarisi, cihazi_hatirla)
            VALUES
            (@userId, @partnerId, @defaultHotelId, @language, @currency, @timezone, @calendarView,
             @email, @sms, @push, @desktop, @newReservation, @cancellation, @payment, @review, @autoPrice, @autoCloseout, @remember)
            ON DUPLICATE KEY UPDATE
             varsayilan_otel_id = VALUES(varsayilan_otel_id),
             dil = VALUES(dil),
             para_birimi = VALUES(para_birimi),
             zaman_dilimi = VALUES(zaman_dilimi),
             takvim_gorunumu = VALUES(takvim_gorunumu),
             email_bildirimleri = VALUES(email_bildirimleri),
             sms_bildirimleri = VALUES(sms_bildirimleri),
             push_bildirimleri = VALUES(push_bildirimleri),
             masaustu_bildirimleri = VALUES(masaustu_bildirimleri),
             yeni_rezervasyon_bildirimi = VALUES(yeni_rezervasyon_bildirimi),
             iptal_bildirimi = VALUES(iptal_bildirimi),
             odeme_bildirimi = VALUES(odeme_bildirimi),
             yorum_bildirimi = VALUES(yorum_bildirimi),
             otomatik_fiyat_onerileri = VALUES(otomatik_fiyat_onerileri),
             otomatik_kapali_gun_uyarisi = VALUES(otomatik_kapali_gun_uyarisi),
             cihazi_hatirla = VALUES(cihazi_hatirla),
             guncellenme_tarihi = NOW();";

        await using var saveCommand = new MySqlCommand(sql, connection);
        saveCommand.Parameters.AddWithValue("@userId", userId);
        saveCommand.Parameters.AddWithValue("@partnerId", hotel.PartnerId);
        saveCommand.Parameters.AddWithValue("@defaultHotelId", request.DefaultHotelId ?? hotel.HotelId);
        saveCommand.Parameters.AddWithValue("@language", request.Language);
        saveCommand.Parameters.AddWithValue("@currency", request.Currency);
        saveCommand.Parameters.AddWithValue("@timezone", request.Timezone);
        saveCommand.Parameters.AddWithValue("@calendarView", request.CalendarView);
        saveCommand.Parameters.AddWithValue("@email", request.EmailNotifications ? 1 : 0);
        saveCommand.Parameters.AddWithValue("@sms", request.SmsNotifications ? 1 : 0);
        saveCommand.Parameters.AddWithValue("@push", request.PushNotifications ? 1 : 0);
        saveCommand.Parameters.AddWithValue("@desktop", request.DesktopNotifications ? 1 : 0);
        saveCommand.Parameters.AddWithValue("@newReservation", request.NewReservationNotifications ? 1 : 0);
        saveCommand.Parameters.AddWithValue("@cancellation", request.CancellationNotifications ? 1 : 0);
        saveCommand.Parameters.AddWithValue("@payment", request.PaymentNotifications ? 1 : 0);
        saveCommand.Parameters.AddWithValue("@review", request.ReviewNotifications ? 1 : 0);
        saveCommand.Parameters.AddWithValue("@autoPrice", request.AutoPriceSuggestions ? 1 : 0);
        saveCommand.Parameters.AddWithValue("@autoCloseout", request.AutoCloseoutWarnings ? 1 : 0);
        saveCommand.Parameters.AddWithValue("@remember", request.RememberDevice ? 1 : 0);
        await saveCommand.ExecuteNonQueryAsync(cancellationToken);
        return (true, "Partner panel tercihleri kaydedildi.");
    }

    public async Task<PartnerSupportPageViewModel> GetSupportAsync(long userId, long? hotelId = null, CancellationToken cancellationToken = default)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        var context = await BuildContextAsync(connection, userId, hotelId, "7/24 Destek", "Canli destek, bilgi bankasi ve partner operasyon taleplerini tek merkezde toplayin.", "support", cancellationToken);
        var model = new PartnerSupportPageViewModel
        {
            Shell = context.Shell,
            CreateTicketForm = new PartnerSupportCreateTicketRequest { HotelId = context.SelectedHotel.HotelId }
        };

        const string summarySql = @"
            SELECT
                COUNT(*) AS total_count,
                SUM(CASE WHEN durum IN ('Acik','Partner Yaniti Bekleniyor','Inceleniyor') THEN 1 ELSE 0 END) AS open_count,
                SUM(CASE WHEN oncelik = 'Kritik' THEN 1 ELSE 0 END) AS critical_count,
                SUM(CASE WHEN durum = 'Cozuldu' THEN 1 ELSE 0 END) AS resolved_count
            FROM partner_destek_talepleri
            WHERE partner_id = @partnerId;";

        await using (var summaryCommand = new MySqlCommand(summarySql, connection))
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
            SELECT talep_no, konu, kategori, oncelik, durum, son_mesaj_tarihi, id
            FROM partner_destek_talepleri
            WHERE partner_id = @partnerId
            ORDER BY son_mesaj_tarihi DESC, id DESC
            LIMIT 10;";

        await using (var ticketCommand = new MySqlCommand(ticketSql, connection))
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

        const string articleSql = @"
            SELECT baslik, COALESCE(ozet, ''), CONCAT('/yardim-merkezi#', seo_slug) AS url
            FROM destek_makaleleri
            WHERE durum = 1
              AND yardim_merkezinde_goster = 1
            ORDER BY one_cikan_mi DESC, siralama ASC, id DESC
            LIMIT 6;";

        await using (var articleCommand = new MySqlCommand(articleSql, connection))
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

        await using (var channelCommand = new MySqlCommand(channelSql, connection))
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

    public async Task<(bool Success, string Message)> CreateSupportTicketAsync(long userId, PartnerSupportCreateTicketRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Subject) || string.IsNullOrWhiteSpace(request.Message))
        {
            return (false, "Talep konusu ve mesaj zorunludur.");
        }

        await using var connection = new MySqlConnection(_connectionString);
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
                (@partnerId, @userId, @hotelId, @ticketNo, @subject, @category, @priority, 'Acik', NOW());
                SELECT LAST_INSERT_ID();";

            long ticketId;
            await using (var ticketCommand = new MySqlCommand(ticketSql, connection, (MySqlTransaction)transaction))
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

            await using (var messageCommand = new MySqlCommand(messageSql, connection, (MySqlTransaction)transaction))
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

    private async Task<PartnerContext> BuildContextAsync(MySqlConnection connection, long userId, long? hotelId, string title, string subtitle, string activeSectionKey, CancellationToken cancellationToken)
    {
        var hotels = await GetManagedHotelsAsync(connection, userId, cancellationToken);
        if (hotels.Count == 0)
        {
            throw new InvalidOperationException("Bu kullanici icin yetkili otel bulunamadi.");
        }

        var selectedHotel = hotelId.HasValue
            ? hotels.FirstOrDefault(item => item.HotelId == hotelId.Value) ?? hotels[0]
            : hotels[0];

        var shell = await BuildShellAsync(connection, userId, selectedHotel, hotels, title, subtitle, activeSectionKey, cancellationToken);
        return new PartnerContext(selectedHotel, shell);
    }

    private async Task<PartnerShellViewModel> BuildShellAsync(MySqlConnection connection, long userId, PartnerHotelContext selectedHotel, IReadOnlyList<PartnerHotelContext> hotels, string title, string subtitle, string activeSectionKey, CancellationToken cancellationToken)
    {
        const string userSql = "SELECT ad_soyad, eposta, rol FROM users WHERE id = @userId LIMIT 1;";
        string fullName = "Partner Kullanici";
        string email = string.Empty;
        string role = "partner_staff";

        await using (var userCommand = new MySqlCommand(userSql, connection))
        {
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

        const string shellMetricsSql = @"
            SELECT
                (SELECT COUNT(*) FROM rezervasyonlar r WHERE r.otel_id = @hotelId AND r.durum IN ('Onay Bekliyor','Değişiklik Bekliyor')) AS pending_reservations,
                (SELECT COUNT(*) FROM partner_destek_talepleri pdt WHERE pdt.partner_id = @partnerId AND pdt.durum IN ('Acik','Partner Yaniti Bekleniyor','Inceleniyor')) AS open_support_tickets,
                (SELECT COUNT(*)
                 FROM oda_fiyat_musaitlik ofm
                 INNER JOIN oda_tipleri ot ON ot.id = ofm.oda_tip_id
                 WHERE ot.otel_id = @hotelId
                   AND ofm.tarih BETWEEN CURDATE() AND DATE_ADD(CURDATE(), INTERVAL 14 DAY)
                   AND ((ofm.toplam_oda_sayisi - ofm.satilan_oda_sayisi - ofm.bloke_oda_sayisi) <= 2 OR ofm.kapali_satis = 1)) AS low_stock_alerts,
                (SELECT COUNT(*) FROM yorumlar y WHERE y.otel_id = @hotelId AND COALESCE(y.otel_yaniti, '') = '') AS unanswered_reviews;";

        await using (var shellCommand = new MySqlCommand(shellMetricsSql, connection))
        {
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
            ManagedHotels = hotels.Select(item => new PartnerHotelSwitchItemViewModel
            {
                HotelId = item.HotelId,
                HotelCode = item.HotelCode,
                HotelName = item.HotelName,
                CityLabel = item.CityLabel,
                IsPrimary = item.IsPrimary,
                IsSelected = item.HotelId == selectedHotel.HotelId
            }).ToList()
        };

        return shell;
    }

    private async Task<List<PartnerHotelContext>> GetManagedHotelsAsync(MySqlConnection connection, long userId, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT DISTINCT o.id, o.partner_id, o.otel_kodu, o.otel_adi, CONCAT(o.ilce, ', ', o.sehir) AS city_label, oks.ana_sorumlu_mu
            FROM otel_kullanici_sahiplikleri oks
            INNER JOIN oteller o ON o.id = oks.otel_id
            WHERE oks.user_id = @userId AND oks.aktif_mi = 1
            ORDER BY oks.ana_sorumlu_mu DESC, o.id ASC;";

        var hotels = new List<PartnerHotelContext>();
        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@userId", userId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            hotels.Add(new PartnerHotelContext(
                reader.GetInt64(0),
                reader.GetInt64(1),
                reader.GetString(2),
                reader.GetString(3),
                "Otel",
                reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                !reader.IsDBNull(5) && reader.GetBoolean(5)));
        }

        return hotels;
    }

    private async Task<PartnerHotelContext> EnsureHotelAccessAsync(MySqlConnection connection, long userId, long hotelId, CancellationToken cancellationToken)
    {
        var hotels = await GetManagedHotelsAsync(connection, userId, cancellationToken);
        var hotel = hotels.FirstOrDefault(item => item.HotelId == hotelId);
        if (hotel is null)
        {
            throw new InvalidOperationException("Bu otel icin yetkiniz bulunmuyor.");
        }

        return hotel;
    }

    private async Task<List<PartnerRoomSummaryViewModel>> GetRoomSummariesAsync(MySqlConnection connection, long hotelId, CancellationToken cancellationToken)
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
                AND ofm.tarih BETWEEN CURDATE() AND DATE_ADD(CURDATE(), INTERVAL 60 DAY)
                AND ofm.indirimli_fiyat IS NOT NULL
            WHERE ot.otel_id = @hotelId
            GROUP BY ot.id, ot.oda_adi, ot.oda_kategorisi, ot.maksimum_yetiskin_sayisi, ot.maksimum_cocuk_sayisi, ot.toplam_oda_sayisi, ot.standart_gecelik_fiyat, ot.kapak_fotografi, ot.aktif_mi
            ORDER BY ot.aktif_mi DESC, ot.siralama ASC, ot.id ASC;";

        var rooms = new List<PartnerRoomSummaryViewModel>();
        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@hotelId", hotelId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var discountPrice = reader.IsDBNull(9) ? (decimal?)null : reader.GetDecimal(9);
            rooms.Add(new PartnerRoomSummaryViewModel
            {
                RoomId = reader.GetInt64(0),
                RoomName = reader.GetString(1),
                Category = reader.GetString(2),
                CapacityText = $"{SafeInt(reader, 3)} yetiskin / {SafeInt(reader, 4)} cocuk",
                StockText = $"{SafeInt(reader, 5)} oda",
                BasePriceText = FormatMoney(SafeDecimal(reader, 6)),
                DiscountPriceText = discountPrice.HasValue ? FormatMoney(discountPrice.Value) : "-",
                CoverPhotoUrl = reader.IsDBNull(7) ? null : reader.GetString(7),
                IsActive = reader.GetBoolean(8)
            });
        }

        return rooms;
    }

    private async Task<Dictionary<(long RoomId, DateOnly Date), PricingCalendarEntry>> LoadPricingMonthEntriesAsync(MySqlConnection connection, long hotelId, DateOnly monthStart, DateOnly monthEnd, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT ofm.oda_tip_id,
                   ofm.tarih,
                   ofm.gecelik_fiyat,
                   ofm.indirimli_fiyat,
                   ofm.kampanya_id,
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
              AND ofm.tarih BETWEEN @startDate AND @endDate;";

        var entries = new Dictionary<(long RoomId, DateOnly Date), PricingCalendarEntry>();
        await using var command = new MySqlCommand(sql, connection);
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
                reader.IsDBNull(5) ? null : reader.GetInt16(5),
                reader.IsDBNull(6) ? (short)0 : reader.GetInt16(6),
                reader.IsDBNull(7) ? (short)0 : reader.GetInt16(7),
                reader.IsDBNull(8) ? null : reader.GetByte(8),
                reader.IsDBNull(9) ? null : reader.GetInt16(9),
                !reader.IsDBNull(10) && reader.GetBoolean(10),
                reader.IsDBNull(11) ? null : reader.GetString(11),
                reader.IsDBNull(12) ? null : reader.GetString(12));
        }

        return entries;
    }

    private async Task<Dictionary<(long RoomId, DateOnly Date), PricingCalendarEntry>> LoadPricingEntriesForRangeAsync(MySqlConnection connection, IReadOnlyCollection<long> roomIds, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken)
    {
        if (roomIds.Count == 0)
        {
            return new Dictionary<(long RoomId, DateOnly Date), PricingCalendarEntry>();
        }

        var sql = $@"
            SELECT oda_tip_id,
                   tarih,
                   gecelik_fiyat,
                   indirimli_fiyat,
                   kampanya_id,
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
              AND tarih BETWEEN @startDate AND @endDate;";

        var entries = new Dictionary<(long RoomId, DateOnly Date), PricingCalendarEntry>();
        await using var command = new MySqlCommand(sql, connection);
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
                reader.IsDBNull(5) ? null : reader.GetInt16(5),
                reader.IsDBNull(6) ? (short)0 : reader.GetInt16(6),
                reader.IsDBNull(7) ? (short)0 : reader.GetInt16(7),
                reader.IsDBNull(8) ? null : reader.GetByte(8),
                reader.IsDBNull(9) ? null : reader.GetInt16(9),
                !reader.IsDBNull(10) && reader.GetBoolean(10),
                reader.IsDBNull(11) ? null : reader.GetString(11),
                reader.IsDBNull(12) ? null : reader.GetString(12));
        }

        return entries;
    }

    private static List<PartnerStatCardViewModel> BuildPricingSummaryCards(
        IReadOnlyCollection<PartnerRoomSummaryViewModel> rooms,
        IReadOnlyDictionary<(long RoomId, DateOnly Date), PricingCalendarEntry> pricingEntries,
        long? selectedRoomId)
    {
        var filteredEntries = selectedRoomId.HasValue
            ? pricingEntries.Values.Where(item => item.RoomId == selectedRoomId.Value).ToList()
            : pricingEntries.Values.ToList();

        var activeRoomCount = rooms.Count(static item => item.IsActive);
        var campaignDayCount = filteredEntries.Count(static item => item.DiscountPrice.HasValue && item.DiscountPrice.Value > 0m && item.DiscountPrice.Value < item.BasePrice);
        var closedDayCount = filteredEntries.Count(static item => item.IsClosed);
        var averagePrice = filteredEntries.Count > 0
            ? filteredEntries.Average(static item => item.DiscountPrice ?? item.BasePrice)
            : rooms.FirstOrDefault(item => !selectedRoomId.HasValue || item.RoomId == selectedRoomId.Value) is { } selectedRoom
                ? ParseMoney(selectedRoom.BasePriceText)
                : 0m;

        return new List<PartnerStatCardViewModel>
        {
            new() { Label = "Aktif Oda Tipi", Value = activeRoomCount.ToString(CultureInfo.InvariantCulture), Description = "Takvimde yonetilen aktif oda tipleri", IconClass = "fa-bed", ToneClass = "info" },
            new() { Label = "Kampanyali Gun", Value = campaignDayCount.ToString(CultureInfo.InvariantCulture), Description = "Indirimli fiyat tanimlanmis gun sayisi", IconClass = "fa-tags", ToneClass = "success" },
            new() { Label = "Kapali Gun", Value = closedDayCount.ToString(CultureInfo.InvariantCulture), Description = "Satisa kapatilan takvim gunleri", IconClass = "fa-lock", ToneClass = "danger" },
            new() { Label = "Ortalama Fiyat", Value = FormatMoney(averagePrice), Description = "Secili ay icin efektif satis fiyati ortalamasi", IconClass = "fa-money-bill-wave", ToneClass = "warning" }
        };
    }

    private static List<PartnerPricingDayViewModel> BuildPricingCalendarDays(
        IReadOnlyCollection<PartnerRoomSummaryViewModel> rooms,
        IReadOnlyDictionary<(long RoomId, DateOnly Date), PricingCalendarEntry> pricingEntries,
        long roomId,
        DateOnly monthStart)
    {
        var room = rooms.FirstOrDefault(item => item.RoomId == roomId);
        if (room is null)
        {
            return new List<PartnerPricingDayViewModel>();
        }

        var defaultBasePrice = ParseMoney(room.BasePriceText);
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
            var basePrice = entry?.BasePrice ?? defaultBasePrice;
            var discountPrice = entry?.DiscountPrice;
            var totalRooms = entry?.TotalRooms ?? defaultStock;
            var soldRooms = entry?.SoldRooms ?? 0;
            var blockedRooms = entry?.BlockedRooms ?? 0;
            var availableRooms = Math.Max(0, totalRooms - soldRooms - blockedRooms);
            var isClosed = entry?.IsClosed ?? false;
            var effectivePrice = discountPrice ?? basePrice;
            var hasDiscount = discountPrice.HasValue && discountPrice.Value > 0m && discountPrice.Value < basePrice;

            items.Add(new PartnerPricingDayViewModel
            {
                Date = date,
                DayLabel = date.Day.ToString("00", CultureInfo.InvariantCulture),
                WeekdayLabel = date.ToDateTime(TimeOnly.MinValue).ToString("ddd", culture),
                PriceText = FormatMoney(effectivePrice),
                BasePriceText = FormatMoney(basePrice),
                DiscountPriceText = hasDiscount ? FormatMoney(discountPrice!.Value) : "-",
                BasePriceAmount = basePrice,
                DiscountPriceAmount = discountPrice,
                TotalRooms = Convert.ToInt16(totalRooms, CultureInfo.InvariantCulture),
                SoldRooms = soldRooms,
                BlockedRooms = blockedRooms,
                MinStay = entry?.MinStay ?? (byte)1,
                MaxStay = entry?.MaxStay ?? (short)30,
                CampaignId = entry?.CampaignId,
                AvailabilityText = isClosed ? "Satisa kapali" : $"{availableRooms} oda musait",
                SoldText = soldRooms > 0 ? $"{soldRooms} satildi" : "Henuz satis yok",
                StatusText = isClosed ? "Kapali" : availableRooms == 0 ? "Dolu" : availableRooms <= 2 ? "Son odalar" : "Satista",
                CampaignLabel = entry?.CampaignLabel,
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

    private async Task<List<PartnerCampaignOptionViewModel>> LoadActiveCampaignOptionsAsync(MySqlConnection connection, long hotelId, CancellationToken cancellationToken)
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
              AND bitis_tarihi >= NOW()
              AND COALESCE(partner_katilim_acik, 1) = 1
              AND (partner_katilim_baslangic IS NULL OR partner_katilim_baslangic <= NOW())
              AND (partner_katilim_bitis IS NULL OR partner_katilim_bitis >= NOW())
            ORDER BY one_cikan_kampanya DESC, baslangic_tarihi ASC, id ASC
            LIMIT 50;";

        var items = new List<PartnerCampaignOptionViewModel>();
        await using var command = new MySqlCommand(sql, connection);
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
                BadgeText = reader.GetBoolean(8) ? "Öne Çıkan" : (reader.IsDBNull(12) ? null : reader.GetString(12)),
                Description = reader.IsDBNull(9) ? null : reader.GetString(9),
                PromoBadge = reader.IsDBNull(10) ? null : reader.GetString(10),
                ColorCode = reader.IsDBNull(11) ? "#003B95" : reader.GetString(11),
                IsHighlighted = reader.GetBoolean(8)
            });
        }

        return items;
    }

    private async Task<List<PartnerCampaignParticipationViewModel>> LoadJoinedCampaignsAsync(MySqlConnection connection, long hotelId, CancellationToken cancellationToken)
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
        await using var command = new MySqlCommand(sql, connection);
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
                IsFeatured = !reader.IsDBNull(9) && reader.GetBoolean(9),
                DiscountText = discountText
            });
        }

        return items;
    }

    private async Task<CampaignSelection?> ResolveCampaignAsync(MySqlConnection connection, long? campaignId, CancellationToken cancellationToken)
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
              AND bitis_tarihi >= NOW()
              AND COALESCE(partner_katilim_acik, 1) = 1
              AND (partner_katilim_baslangic IS NULL OR partner_katilim_baslangic <= NOW())
              AND (partner_katilim_bitis IS NULL OR partner_katilim_bitis >= NOW())
            LIMIT 1;";

        await using var command = new MySqlCommand(sql, connection);
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

    private async Task UpsertCampaignHotelParticipationAsync(
        MySqlConnection connection,
        MySqlTransaction transaction,
        PartnerHotelContext hotel,
        CampaignSelection campaign,
        long userId,
        DateTime dateFrom,
        DateTime dateTo,
        CancellationToken cancellationToken)
    {
        if (!await TableExistsAsync(connection, "kampanya_oteller", cancellationToken))
        {
            return;
        }

        const string sql = @"
            INSERT INTO kampanya_oteller
            (kampanya_id, otel_id, partner_id, katilim_durumu, baslangic_tarihi, bitis_tarihi, olusturan_kullanici_id, guncelleyen_kullanici_id)
            VALUES
            (@campaignId, @hotelId, @partnerId, 'Aktif', @startDate, @endDate, @userId, @userId)
            ON DUPLICATE KEY UPDATE
                partner_id = VALUES(partner_id),
                katilim_durumu = 'Aktif',
                baslangic_tarihi = LEAST(COALESCE(baslangic_tarihi, VALUES(baslangic_tarihi)), VALUES(baslangic_tarihi)),
                bitis_tarihi = GREATEST(COALESCE(bitis_tarihi, VALUES(bitis_tarihi)), VALUES(bitis_tarihi)),
                guncelleyen_kullanici_id = VALUES(guncelleyen_kullanici_id),
                guncellenme_tarihi = CURRENT_TIMESTAMP;";

        await using var command = new MySqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("@campaignId", campaign.CampaignId);
        command.Parameters.AddWithValue("@hotelId", hotel.HotelId);
        command.Parameters.AddWithValue("@partnerId", hotel.PartnerId);
        command.Parameters.AddWithValue("@startDate", dateFrom.Date < campaign.StartDate ? campaign.StartDate : dateFrom.Date);
        command.Parameters.AddWithValue("@endDate", dateTo.Date > campaign.EndDate ? campaign.EndDate : dateTo.Date);
        command.Parameters.AddWithValue("@userId", userId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<bool> TableExistsAsync(MySqlConnection connection, string tableName, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT COUNT(*)
            FROM information_schema.TABLES
            WHERE TABLE_SCHEMA = DATABASE()
              AND TABLE_NAME = @tableName;";

        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@tableName", tableName);
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

    private static int GetMondayBasedIndex(DayOfWeek dayOfWeek)
        => ((int)dayOfWeek + 6) % 7;

    private async Task<PartnerHotelInfoForm> LoadHotelInfoFormAsync(MySqlConnection connection, long hotelId, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT otel_adi, otel_turu, turizm_belge_no, turizm_belge_turu, kisa_aciklama, uzun_aciklama, tam_adres, sehir, ilce, mahalle, posta_kodu,
                   konum_aciklamasi, enlem, boylam, web_sitesi, eposta, telefon_1, telefon_2, faks,
                   check_in_saati, check_out_saati, gec_check_out_mumkun_mu, gec_check_out_ucreti, erken_check_in_mumkun_mu, erken_check_in_ucreti,
                   minimum_konaklama_gecesi, maksimum_konaklama_gecesi, yildiz_sayisi, toplam_oda_sayisi, toplam_yatak_kapasitesi, kat_sayisi,
                   asansor_var_mi, asansor_sayisi, varsayilan_komisyon_orani, depozito_tutari, depozito_iade_suresi, konusulan_diller, video_url, sanal_tur_url
            FROM oteller
            WHERE id = @hotelId
            LIMIT 1;";

        var model = new PartnerHotelInfoForm { HotelId = hotelId };
        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@hotelId", hotelId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            model.HotelName = reader.GetString(0);
            model.HotelType = reader.GetString(1);
            model.TourismDocumentNo = reader.IsDBNull(2) ? null : reader.GetString(2);
            model.TourismDocumentType = reader.IsDBNull(3) ? null : reader.GetString(3);
            model.ShortDescription = reader.IsDBNull(4) ? null : reader.GetString(4);
            model.Description = reader.IsDBNull(5) ? null : reader.GetString(5);
            model.Address = reader.IsDBNull(6) ? null : reader.GetString(6);
            model.City = reader.IsDBNull(7) ? null : reader.GetString(7);
            model.District = reader.IsDBNull(8) ? null : reader.GetString(8);
            model.Neighborhood = reader.IsDBNull(9) ? null : reader.GetString(9);
            model.PostalCode = reader.IsDBNull(10) ? null : reader.GetString(10);
            model.LocationDescription = reader.IsDBNull(11) ? null : reader.GetString(11);
            model.Latitude = reader.IsDBNull(12) ? null : reader.GetDecimal(12);
            model.Longitude = reader.IsDBNull(13) ? null : reader.GetDecimal(13);
            model.Website = reader.IsDBNull(14) ? null : reader.GetString(14);
            model.ContactEmail = reader.IsDBNull(15) ? null : reader.GetString(15);
            model.HotelPhone = reader.IsDBNull(16) ? null : reader.GetString(16);
            model.HotelPhone2 = reader.IsDBNull(17) ? null : reader.GetString(17);
            model.Fax = reader.IsDBNull(18) ? null : reader.GetString(18);
            model.CheckInTime = reader.IsDBNull(19) ? null : reader.GetTimeSpan(19).ToString(@"hh\:mm");
            model.CheckOutTime = reader.IsDBNull(20) ? null : reader.GetTimeSpan(20).ToString(@"hh\:mm");
            model.LateCheckoutAvailable = !reader.IsDBNull(21) && reader.GetBoolean(21);
            model.LateCheckoutFee = reader.IsDBNull(22) ? null : reader.GetDecimal(22);
            model.EarlyCheckinAvailable = !reader.IsDBNull(23) && reader.GetBoolean(23);
            model.EarlyCheckinFee = reader.IsDBNull(24) ? null : reader.GetDecimal(24);
            model.MinStay = reader.IsDBNull(25) ? (byte)1 : reader.GetByte(25);
            model.MaxStay = reader.IsDBNull(26) ? (short)30 : reader.GetInt16(26);
            model.StarCount = reader.IsDBNull(27) ? null : reader.GetByte(27);
            model.TotalRoomCount = reader.IsDBNull(28) ? (short)0 : reader.GetInt16(28);
            model.TotalBedCapacity = reader.IsDBNull(29) ? null : reader.GetInt16(29);
            model.FloorCount = reader.IsDBNull(30) ? null : reader.GetByte(30);
            model.ElevatorAvailable = !reader.IsDBNull(31) && reader.GetBoolean(31);
            model.ElevatorCount = reader.IsDBNull(32) ? (byte)0 : reader.GetByte(32);
            model.DefaultCommissionRate = reader.IsDBNull(33) ? 0m : reader.GetDecimal(33);
            model.DepositAmount = reader.IsDBNull(34) ? null : reader.GetDecimal(34);
            model.DepositReturnDays = reader.IsDBNull(35) ? null : reader.GetByte(35);
            model.SpokenLanguages = reader.IsDBNull(36) ? null : reader.GetString(36);
            model.VideoUrl = reader.IsDBNull(37) ? null : reader.GetString(37);
            model.VirtualTourUrl = reader.IsDBNull(38) ? null : reader.GetString(38);
        }

        return model;
    }

    private async Task<List<PartnerAmenityOptionViewModel>> LoadAmenityOptionsAsync(MySqlConnection connection, long hotelId, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT o.id, o.ozellik_adi, COALESCE(o.ozellik_ikon, 'fa-circle-check') AS ikon,
                   CASE WHEN oi.otel_id IS NULL THEN 0 ELSE 1 END AS secili
            FROM otel_ozellikleri o
            LEFT JOIN otel_ozellik_iliskileri oi ON oi.ozellik_id = o.id AND oi.otel_id = @hotelId
            WHERE o.aktif_mi = 1
            ORDER BY o.one_cikan_ozellik DESC, o.siralama ASC, o.id ASC;";

        var items = new List<PartnerAmenityOptionViewModel>();
        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@hotelId", hotelId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new PartnerAmenityOptionViewModel
            {
                AmenityId = reader.GetInt64(0),
                Name = reader.GetString(1),
                IconClass = reader.GetString(2),
                IsSelected = reader.GetInt32(3) == 1
            });
        }

        return items;
    }

    private async Task<List<PartnerRoomInventoryRowViewModel>> LoadRoomInventoryRowsAsync(MySqlConnection connection, long hotelId, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT
                ot.id,
                ot.oda_adi,
                ot.toplam_oda_sayisi,
                GREATEST(ot.toplam_oda_sayisi - COALESCE(MAX(ofm.satilan_oda_sayisi + ofm.bloke_oda_sayisi), 0), 0) AS satilabilir_oda,
                SUM(CASE WHEN COALESCE(ofm.kapali_satis, 0) = 1 THEN 1 ELSE 0 END) AS bakim_gunu,
                COALESCE(MIN(COALESCE(ofm.indirimli_fiyat, ofm.gecelik_fiyat)), ot.standart_gecelik_fiyat) AS minimum_fiyat,
                COALESCE(MAX(COALESCE(ofm.indirimli_fiyat, ofm.gecelik_fiyat)), ot.standart_gecelik_fiyat) AS maksimum_fiyat,
                ot.aktif_mi
            FROM oda_tipleri ot
            LEFT JOIN oda_fiyat_musaitlik ofm
                ON ofm.oda_tip_id = ot.id
               AND ofm.tarih BETWEEN CURDATE() AND DATE_ADD(CURDATE(), INTERVAL 30 DAY)
            WHERE ot.otel_id = @hotelId
            GROUP BY ot.id, ot.oda_adi, ot.toplam_oda_sayisi, ot.standart_gecelik_fiyat, ot.aktif_mi
            ORDER BY ot.aktif_mi DESC, ot.siralama ASC, ot.id ASC;";

        var items = new List<PartnerRoomInventoryRowViewModel>();
        await using var command = new MySqlCommand(sql, connection);
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
                MinPriceText = FormatMoney(SafeDecimal(reader, 5)),
                MaxPriceText = FormatMoney(SafeDecimal(reader, 6)),
                IsActive = reader.GetBoolean(7)
            });
        }

        return items;
    }

    private async Task<PartnerRoomUpsertRequest> LoadRoomFormAsync(MySqlConnection connection, long hotelId, long roomId, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT id, oda_adi, oda_kategorisi, yatak_tipi, manzara_tipi, oda_metrekare,
                   maksimum_yetiskin_sayisi, maksimum_cocuk_sayisi, bebek_ucretsiz_mi,
                   toplam_oda_sayisi, standart_gecelik_fiyat, kapak_fotografi, ozellikler, aktif_mi
            FROM oda_tipleri
            WHERE otel_id = @hotelId AND id = @roomId
            LIMIT 1;";

        var model = new PartnerRoomUpsertRequest { HotelId = hotelId };
        await using var command = new MySqlCommand(sql, connection);
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
            model.MaxAdults = reader.IsDBNull(6) ? (byte)2 : reader.GetByte(6);
            model.MaxChildren = reader.IsDBNull(7) ? (byte)0 : reader.GetByte(7);
            model.MaxBabies = !reader.IsDBNull(8) && reader.GetBoolean(8) ? (byte)1 : (byte)0;
            model.TotalRooms = reader.IsDBNull(9) ? (short)1 : reader.GetInt16(9);
            model.BasePrice = reader.IsDBNull(10) ? 0m : reader.GetDecimal(10);
            model.CoverPhotoPath = reader.IsDBNull(11) ? null : reader.GetString(11);
            model.IsActive = !reader.IsDBNull(13) && reader.GetBoolean(13);

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

    private async Task<PartnerPhotoEditForm> LoadPhotoEditFormAsync(MySqlConnection connection, long hotelId, long photoId, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT id, gorsel_turu, baslik, aciklama, siralama, one_cikan
            FROM otel_gorselleri
            WHERE otel_id = @hotelId AND id = @photoId
            LIMIT 1;";

        var model = new PartnerPhotoEditForm { HotelId = hotelId };
        await using var command = new MySqlCommand(sql, connection);
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
            model.MarkAsFeatured = !reader.IsDBNull(5) && reader.GetBoolean(5);
        }

        return model;
    }

    private async Task<List<PartnerCompetitorRowViewModel>> LoadCompetitorsAsync(MySqlConnection connection, long hotelId, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT id, rakip_otel_adi, rakip_sehir, rakip_ilce, analiz_tarihi, ortalama_gecelik_fiyat, tahmini_doluluk_orani, kaynak_url, notlar
            FROM otel_rakip_analizi
            WHERE otel_id = @hotelId
            ORDER BY analiz_tarihi DESC, id DESC
            LIMIT 20;";

        var items = new List<PartnerCompetitorRowViewModel>();
        await using var command = new MySqlCommand(sql, connection);
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

    private static void BindRoomCommand(MySqlCommand command, PartnerRoomUpsertRequest request, PartnerHotelContext hotel, string? featuresJson)
    {
        command.Parameters.AddWithValue("@hotelId", hotel.HotelId);
        command.Parameters.AddWithValue("@roomName", request.RoomName.Trim());
        command.Parameters.AddWithValue("@roomCategory", request.RoomCategory);
        command.Parameters.AddWithValue("@maxPeople", Math.Max(request.MaxAdults + request.MaxChildren + request.MaxBabies, (byte)1));
        command.Parameters.AddWithValue("@maxAdults", request.MaxAdults);
        command.Parameters.AddWithValue("@maxChildren", request.MaxChildren);
        command.Parameters.AddWithValue("@babyFree", request.MaxBabies > 0 ? 1 : 0);
        command.Parameters.AddWithValue("@bedType", (object?)request.BedType ?? DBNull.Value);
        command.Parameters.AddWithValue("@roomSize", request.RoomSize.HasValue ? request.RoomSize.Value : DBNull.Value);
        command.Parameters.AddWithValue("@viewType", (object?)request.ViewType ?? DBNull.Value);
        command.Parameters.AddWithValue("@basePrice", request.BasePrice);
        command.Parameters.AddWithValue("@totalRooms", request.TotalRooms);
        command.Parameters.AddWithValue("@coverPhoto", (object?)request.CoverPhotoPath ?? DBNull.Value);
        command.Parameters.AddWithValue("@features", string.IsNullOrWhiteSpace(featuresJson) ? DBNull.Value : featuresJson);
        command.Parameters.AddWithValue("@active", request.IsActive ? 1 : 0);
    }

    private static void BindCompetitorCommand(MySqlCommand command, PartnerCompetitorUpsertRequest request, long hotelId)
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

    private async Task<List<PartnerReservationRowViewModel>> LoadReservationsAsync(MySqlConnection connection, long hotelId, string hotelName, int take, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT id, rezervasyon_no, misafir_ad_soyad, giris_tarihi, cikis_tarihi, durum, odeme_durumu, toplam_tutar, olusturulma_tarihi
            FROM rezervasyonlar
            WHERE otel_id = @hotelId
            ORDER BY olusturulma_tarihi DESC, id DESC
            LIMIT @take;";

        var items = new List<PartnerReservationRowViewModel>();
        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@hotelId", hotelId);
        command.Parameters.AddWithValue("@take", take);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new PartnerReservationRowViewModel
            {
                ReservationId = reader.GetInt64(0),
                ReservationNo = reader.GetString(1),
                HotelName = hotelName,
                GuestName = reader.GetString(2),
                StayText = FormatStay(reader.GetDateTime(3), reader.GetDateTime(4)),
                StatusLabel = reader.GetString(5),
                PaymentStatusLabel = reader.GetString(6),
                TotalText = FormatMoney(SafeDecimal(reader, 7)),
                CreatedText = FormatDateTime(reader.IsDBNull(8) ? null : reader.GetDateTime(8))
            });
        }

        return items;
    }

    private async Task<List<PartnerInventoryAlertViewModel>> LoadInventoryAlertsAsync(MySqlConnection connection, long hotelId, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT ot.id, ot.oda_adi, ofm.tarih,
                   (ofm.toplam_oda_sayisi - ofm.satilan_oda_sayisi - ofm.bloke_oda_sayisi) AS kalan,
                   ofm.kapali_satis
            FROM oda_fiyat_musaitlik ofm
            INNER JOIN oda_tipleri ot ON ot.id = ofm.oda_tip_id
            WHERE ot.otel_id = @hotelId
              AND ofm.tarih BETWEEN CURDATE() AND DATE_ADD(CURDATE(), INTERVAL 14 DAY)
              AND ((ofm.toplam_oda_sayisi - ofm.satilan_oda_sayisi - ofm.bloke_oda_sayisi) <= 2 OR ofm.kapali_satis = 1)
            ORDER BY ofm.kapali_satis DESC, kalan ASC, ofm.tarih ASC
            LIMIT 8;";

        var alerts = new List<PartnerInventoryAlertViewModel>();
        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@hotelId", hotelId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var isClosed = !reader.IsDBNull(4) && reader.GetBoolean(4);
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

    private async Task<List<PartnerReviewRowViewModel>> LoadReviewsAsync(MySqlConnection connection, long hotelId, int take, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT y.id, COALESCE(u.ad_soyad, 'Misafir'), COALESCE(y.yorum_basligi, 'Yorum'), y.genel_puan, y.onay_durumu, y.olusturulma_tarihi, y.yorum_metni, y.otel_yaniti
            FROM yorumlar y
            LEFT JOIN users u ON u.id = y.kullanici_id
            WHERE y.otel_id = @hotelId
            ORDER BY y.olusturulma_tarihi DESC
            LIMIT @take;";

        var items = new List<PartnerReviewRowViewModel>();
        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@hotelId", hotelId);
        command.Parameters.AddWithValue("@take", take);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new PartnerReviewRowViewModel
            {
                ReviewId = reader.GetInt64(0),
                GuestName = reader.GetString(1),
                Title = reader.GetString(2),
                ScoreText = $"{reader.GetByte(3)} / 5",
                StatusText = reader.GetString(4),
                CreatedText = FormatDateTime(reader.IsDBNull(5) ? null : reader.GetDateTime(5)),
                Comment = reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
                ResponseText = reader.IsDBNull(7) ? null : reader.GetString(7)
            });
        }

        return items;
    }

    private static int SafeInt(MySqlDataReader reader, int ordinal)
        => reader.IsDBNull(ordinal) ? 0 : Convert.ToInt32(reader.GetValue(ordinal), CultureInfo.InvariantCulture);

    private static decimal SafeDecimal(MySqlDataReader reader, int ordinal)
        => reader.IsDBNull(ordinal) ? 0m : Convert.ToDecimal(reader.GetValue(ordinal), CultureInfo.InvariantCulture);

    private static string FormatMoney(decimal value)
        => string.Format(CultureInfo.GetCultureInfo("tr-TR"), "{0:C0}", value);

    private static string FormatStay(DateTime checkIn, DateTime checkOut)
        => $"{checkIn:dd.MM.yyyy} - {checkOut:dd.MM.yyyy}";

    private static string FormatDateTime(DateTime? value)
        => value.HasValue ? value.Value.ToString("dd.MM.yyyy HH:mm", CultureInfo.GetCultureInfo("tr-TR")) : "-";

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
    private sealed record PartnerContext(PartnerHotelContext SelectedHotel, PartnerShellViewModel Shell);
    private sealed record PartnerHotelContext(long HotelId, long PartnerId, string HotelCode, string HotelName, string HotelType, string CityLabel, bool IsPrimary);
}
