using Microsoft.Data.SqlClient;
using otelturizmnew.Models;
using otelturizmnew.Models.Paneller.Common;
using otelturizmnew.Services.Abstractions;
using System.Globalization;

namespace otelturizmnew.Services;

public class HeaderBildiriService : IHeaderBildiriService
{
    private readonly string _connectionString;
    private readonly IHotelCompletenessService _hotelCompletenessService;

    public HeaderBildiriService(IConfiguration configuration, IHotelCompletenessService hotelCompletenessService)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection tanimli degil.");
        _hotelCompletenessService = hotelCompletenessService;
    }

    public async Task<HeaderBildiriViewModel> GetForPanelAsync(string panelKey, long userId, int? maxItems = null, CancellationToken cancellationToken = default)
    {
        var safeKey = NormalizePanelKey(panelKey);
        var model = new HeaderBildiriViewModel
        {
            PanelKey = safeKey,
            PanelLabel = ResolvePanelLabel(safeKey),
            InboxUrl = ResolveInboxUrl(safeKey)
        };

        if (userId <= 0)
        {
            return model;
        }

        await using var connection = await OpenConnectionAsync(cancellationToken);
        switch (safeKey)
        {
            case PanelHeaderAudience.Partner:
                await FillPartnerItemsAsync(connection, userId, model, cancellationToken);
                break;
            case PanelHeaderAudience.Firma:
                await FillFirmaItemsAsync(connection, userId, model, cancellationToken);
                break;
            case PanelHeaderAudience.Sales:
                await FillSalesItemsAsync(connection, userId, model, cancellationToken);
                break;
            default:
                await FillUserItemsAsync(connection, userId, model, cancellationToken);
                break;
        }

        if (model.Items.Count == 0)
        {
            model.Items.Add(new HeaderBildiriItemViewModel
            {
                ItemKey = "placeholder",
                IconClass = "fa-sparkles",
                Title = "Paneliniz hazir",
                Description = "Yeni bildirim olustugunda bu alanda otomatik gosterilir.",
                Tone = "info",
                TimeLabel = "Bugun",
                AbsoluteTimeLabel = "Bugun",
                Url = "#",
                IsPlaceholder = true,
                IsRead = true,
                EventTimeUtc = DateTime.UtcNow
            });
        }
        else
        {
            await ApplyReadStateAsync(connection, safeKey, userId, model, cancellationToken);
            FinalizeItems(model, maxItems);
        }

        return model;
    }

    public async Task MarkAsReadAsync(string panelKey, long userId, IReadOnlyCollection<string> itemKeys, CancellationToken cancellationToken = default)
    {
        if (userId <= 0 || itemKeys.Count == 0)
        {
            return;
        }

        var normalizedPanel = NormalizePanelKey(panelKey);
        var normalizedKeys = itemKeys
            .Where(static key => !string.IsNullOrWhiteSpace(key))
            .Select(static key => key.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (normalizedKeys.Count == 0)
        {
            return;
        }

        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            foreach (var key in normalizedKeys)
            {
                const string upsertSql = @"
                    MERGE panel_header_bildiri_okumalari AS target
                    USING (SELECT @panelKey AS [PANEL_KODU], @userId AS [KULLANICI_ID], @itemKey AS [BILDIRI_ANAHTARI]) AS source
                    ON target.[PANEL_KODU] = source.[PANEL_KODU]
                       AND target.[KULLANICI_ID] = source.[KULLANICI_ID]
                       AND target.[BILDIRI_ANAHTARI] = source.[BILDIRI_ANAHTARI]
                    WHEN MATCHED THEN
                        UPDATE SET
                            [OKUNDU_MI] = 1,
                            [OKUNDU_TARIHI] = SYSUTCDATETIME(),
                            [GUNCELLENME_TARIHI] = CURRENT_TIMESTAMP
                    WHEN NOT MATCHED THEN
                        INSERT ([PANEL_KODU], [KULLANICI_ID], [BILDIRI_ANAHTARI], [OKUNDU_MI], [OKUNDU_TARIHI], [GUNCELLENME_TARIHI])
                        VALUES (@panelKey, @userId, @itemKey, 1, SYSUTCDATETIME(), CURRENT_TIMESTAMP);";
                await using var command = new SqlCommand(upsertSql, connection, (SqlTransaction)transaction);
                command.Parameters.AddWithValue("@panelKey", normalizedPanel);
                command.Parameters.AddWithValue("@userId", userId);
                command.Parameters.AddWithValue("@itemKey", key);
                await command.ExecuteNonQueryAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task ClearAllAsync(string panelKey, long userId, CancellationToken cancellationToken = default)
    {
        if (userId <= 0)
        {
            return;
        }

        var normalizedPanel = NormalizePanelKey(panelKey);
        var model = await GetForPanelAsync(normalizedPanel, userId, maxItems: null, cancellationToken);
        var itemKeys = model.Items
            .Where(static item => !item.IsPlaceholder && !string.IsNullOrWhiteSpace(item.ItemKey))
            .Select(static item => item.ItemKey)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (itemKeys.Count > 0)
        {
            await MarkAsReadAsync(normalizedPanel, userId, itemKeys, cancellationToken);
        }

        if (!string.Equals(normalizedPanel, PanelHeaderAudience.User, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = new SqlCommand(@"
            UPDATE [dbo].[SISTEM_ICI_BILDIRIMLER]
            SET [OKUNDU_MU] = 1,
                [OKUNMA_TARIHI] = SYSUTCDATETIME(),
                [ARSIVLENDI_MI] = 1
            WHERE [KULLANICI_ID] = @userId
              AND COALESCE([ARSIVLENDI_MI], 0) = 0;", connection);
        command.Parameters.AddWithValue("@userId", userId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static void FinalizeItems(HeaderBildiriViewModel model, int? maxItems)
    {
        var realItems = model.Items
            .Where(static item => !item.IsPlaceholder)
            .OrderByDescending(static item => item.EventTimeUtc ?? DateTime.MinValue)
            .ThenBy(static item => item.ItemKey, StringComparer.OrdinalIgnoreCase)
            .ToList();

        model.AllItemsCount = realItems.Count;
        model.UnreadCount = realItems.Count(static item => !item.IsRead);
        if (maxItems is > 0 && realItems.Count > maxItems.Value)
        {
            model.Items = realItems.Take(maxItems.Value).ToList();
            return;
        }

        model.Items = realItems;
    }

    private static string ResolveInboxUrl(string panelKey)
    {
        return panelKey switch
        {
            PanelHeaderAudience.Partner => "/panel/partner",
            PanelHeaderAudience.Firma => "/panel/firma",
            PanelHeaderAudience.Sales => "/panel/satis/rezervasyonlarim",
            _ => "/panel/user/bildirimlerim"
        };
    }

    private static string NormalizePanelKey(string? panelKey)
    {
        if (string.Equals(panelKey, PanelHeaderAudience.Partner, StringComparison.OrdinalIgnoreCase)) return PanelHeaderAudience.Partner;
        if (string.Equals(panelKey, PanelHeaderAudience.Firma, StringComparison.OrdinalIgnoreCase)) return PanelHeaderAudience.Firma;
        if (string.Equals(panelKey, PanelHeaderAudience.Sales, StringComparison.OrdinalIgnoreCase)) return PanelHeaderAudience.Sales;
        return PanelHeaderAudience.User;
    }

    private static string ResolvePanelLabel(string panelKey)
    {
        return panelKey switch
        {
            PanelHeaderAudience.Partner => "Partner",
            PanelHeaderAudience.Firma => "Firma",
            PanelHeaderAudience.Sales => "Satis",
            _ => "Kullanici"
        };
    }

    private static string RelativeTime(DateTime? valueUtc)
    {
        if (!valueUtc.HasValue)
        {
            return "Bugun";
        }

        var diff = DateTime.UtcNow - valueUtc.Value.ToUniversalTime();
        if (diff.TotalMinutes < 60) return $"{Math.Max(1, (int)diff.TotalMinutes)} dk once";
        if (diff.TotalHours < 24) return $"{(int)diff.TotalHours} saat once";
        return $"{(int)diff.TotalDays} gun once";
    }

    private static string ResolveAbsoluteTimeLabel(DateTime? valueUtc, string fallback)
    {
        if (!valueUtc.HasValue)
        {
            return string.IsNullOrWhiteSpace(fallback) ? "Zaman bilgisi yok" : fallback;
        }

        var value = valueUtc.Value;
        var local = value.Kind == DateTimeKind.Utc ? value.ToLocalTime() : value;
        return local.ToString("dd.MM.yyyy HH:mm", CultureInfo.GetCultureInfo("tr-TR"));
    }

    private static void Add(
        HeaderBildiriViewModel model,
        string key,
        string icon,
        string title,
        string description,
        string tone,
        string timeLabel,
        string url,
        DateTime? eventTimeUtc = null)
    {
        model.Items.Add(new HeaderBildiriItemViewModel
        {
            ItemKey = key,
            IconClass = icon,
            Title = title,
            Description = description,
            Tone = tone,
            TimeLabel = timeLabel,
            AbsoluteTimeLabel = ResolveAbsoluteTimeLabel(eventTimeUtc, timeLabel),
            Url = string.IsNullOrWhiteSpace(url) ? "#" : url,
            EventTimeUtc = eventTimeUtc ?? DateTime.UtcNow
        });
    }

    private async Task FillUserItemsAsync(SqlConnection connection, long userId, HeaderBildiriViewModel model, CancellationToken cancellationToken)
    {
        const string approvedSql = @"
            SELECT r.id, r.[REZERVASYON_NO], COALESCE(o.[OTEL_ADI], 'Otel'), COALESCE(r.[OTEL_ONAY_TARIHI], r.[GUNCELLENME_TARIHI], r.[OLUSTURULMA_TARIHI])
            FROM [dbo].[REZERVASYONLAR] r
            LEFT JOIN [dbo].[OTELLER] o ON o.id = r.[OTEL_ID]
            WHERE r.[KULLANICI_ID] = @userId
              AND (r.[DURUM] = 'Onaylandı' OR COALESCE(r.[OTEL_ONAY_DURUMU], '') = 'Onaylandı')
            ORDER BY COALESCE(r.[OTEL_ONAY_TARIHI], r.[GUNCELLENME_TARIHI], r.[OLUSTURULMA_TARIHI]) DESC
            OFFSET 0 ROWS FETCH NEXT 1 ROWS ONLY;";

        await using (var command = new SqlCommand(approvedSql, connection))
        {
            command.Parameters.AddWithValue("@userId", userId);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                var reservationNo = reader.IsDBNull(1) ? "-" : reader.GetString(1);
                var hotelName = reader.IsDBNull(2) ? "Otel" : reader.GetString(2);
                DateTime? timeUtc = reader.IsDBNull(3) ? null : reader.GetDateTime(3);
                Add(
                    model,
                    BuildItemKey("user-approved", reservationNo),
                    "fa-circle-check",
                    "Rezervasyon onaylandi",
                    $"{hotelName} icin {reservationNo} numarali rezervasyonunuz onaylandi.",
                    "success",
                    RelativeTime(timeUtc),
                    "/panel/user/rezervasyonlarim");
            }
        }

        const string birthdaySql = "SELECT TOP (1) COALESCE([AD_SOYAD], ''), [DOGUM_TARIHI] FROM [dbo].[KULLANICILAR] WHERE id = @userId;";
        await using (var command = new SqlCommand(birthdaySql, connection))
        {
            command.Parameters.AddWithValue("@userId", userId);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken) && !reader.IsDBNull(1))
            {
                var fullName = reader.IsDBNull(0) ? "Degerli misafirimiz" : reader.GetString(0);
                var birthDate = reader.GetDateTime(1).Date;
                var today = DateTime.Today;
                if (birthDate.Month == today.Month && birthDate.Day == today.Day)
                {
                    Add(
                        model,
                        BuildItemKey("user-birthday", fullName),
                        "fa-cake-candles",
                        "Dogum gununuz kutlu olsun",
                        $"{fullName}, saglikli ve mutlu bir yas diliyoruz.",
                        "vip",
                        "Bugun",
                        "/panel/user/profil-bilgilerim");
                }
            }
        }

        const string reservationCountSql = "SELECT COUNT(*) FROM [dbo].[REZERVASYONLAR] WHERE [KULLANICI_ID] = @userId;";
        await using (var command = new SqlCommand(reservationCountSql, connection))
        {
            command.Parameters.AddWithValue("@userId", userId);
            var count = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken) ?? 0);
            if (count == 1)
            {
                Add(
                    model,
                    BuildItemKey("user-first-reservation", userId.ToString()),
                    "fa-heart",
                    "Ilk rezervasyon tesekkuru",
                    "Bizleri tercih ettiginiz icin tesekkur ederiz. Konaklamanizla ilgili tum sorularinizi firmaya iletebilirsiniz.",
                    "info",
                    "Yeni",
                    "/panel/user/rezervasyonlarim");
            }
        }

        const string unreadSql = @"
            SELECT COALESCE(SUM([MISAFIR_OKUNMAMIS_SAYISI]), 0)
            FROM [dbo].[MESAJ_KONUSMALARI]
            WHERE [MISAFIR_KULLANICI_ID] = @userId
              AND [DURUM] <> 'Arşivlendi';";

        const string invoiceSql = @"
            IF OBJECT_ID(N'[dbo].[REZERVASYON_FATURALARI]', N'U') IS NULL
            BEGIN
                RETURN;
            END

            SELECT TOP (3)
                rf.id,
                COALESCE(NULLIF(r.[REZERVASYON_NO], ''), CAST(r.id AS nvarchar(30))) AS [REZERVASYON_NO],
                COALESCE(o.[OTEL_ADI], N'Otel') AS [OTEL_ADI],
                rf.[OLUSTURULMA_TARIHI]
            FROM [dbo].[REZERVASYON_FATURALARI] rf
            INNER JOIN [dbo].[REZERVASYONLAR] r ON r.id = rf.[REZERVASYON_ID]
            INNER JOIN [dbo].[OTELLER] o ON o.id = rf.[OTEL_ID]
            WHERE r.[KULLANICI_ID] = @userId
              AND rf.[GUVENLI_DOSYA_ID] IS NOT NULL
            ORDER BY rf.[OLUSTURULMA_TARIHI] DESC;";

        await using (var invoiceCommand = new SqlCommand(invoiceSql, connection))
        {
            invoiceCommand.Parameters.AddWithValue("@userId", userId);
            await using var reader = await invoiceCommand.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var invoiceId = reader.GetInt64(0);
                var reservationNo = reader.GetString(1);
                var hotelName = reader.GetString(2);
                var uploadedAt = reader.GetDateTime(3);
                Add(
                    model,
                    BuildItemKey("user-invoice", invoiceId.ToString(CultureInfo.InvariantCulture)),
                    "fa-file-invoice",
                    "Faturaniz yuklendi",
                    $"{hotelName} konaklamaniz ({reservationNo}) icin fatura indirilebilir.",
                    "success",
                    RelativeTime(uploadedAt),
                    "/panel/user/faturalarim",
                    uploadedAt);
            }
        }

        await using (var command = new SqlCommand(unreadSql, connection))
        {
            command.Parameters.AddWithValue("@userId", userId);
            var unreadCount = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken) ?? 0);
            if (unreadCount > 0)
            {
                Add(
                    model,
                    BuildItemKey("user-unread-messages", unreadCount.ToString()),
                    "fa-envelope-open-text",
                    "Yeni mesajlariniz var",
                    $"{unreadCount} adet okunmamis mesajiniz sizi bekliyor.",
                    "warning",
                    "Simdi",
                    "/panel/user/mesajlarim");
            }
        }
    }

    private async Task FillPartnerItemsAsync(SqlConnection connection, long userId, HeaderBildiriViewModel model, CancellationToken cancellationToken)
    {
        const string pendingSql = @"
            SELECT COUNT(*),
                   MAX(COALESCE(r.[GUNCELLENME_TARIHI], r.[OLUSTURULMA_TARIHI]))
            FROM [dbo].[REZERVASYONLAR] r
            INNER JOIN [dbo].[OTEL_KULLANICI_SAHIPLIKLERI] oks ON oks.[OTEL_ID] = r.[OTEL_ID]
            WHERE oks.[KULLANICI_ID] = @userId
              AND oks.[AKTIF_MI] = 1
              AND r.[DURUM] IN ('Onay Bekliyor', 'Değişiklik Bekliyor');";
        await using (var command = new SqlCommand(pendingSql, connection))
        {
            command.Parameters.AddWithValue("@userId", userId);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                var pendingCount = reader.IsDBNull(0) ? 0 : Convert.ToInt32(reader.GetValue(0), CultureInfo.InvariantCulture);
                if (pendingCount > 0)
                {
                    DateTime? timeUtc = reader.IsDBNull(1) ? null : reader.GetDateTime(1);
                    Add(
                        model,
                        BuildItemKey("partner-pending-count", pendingCount.ToString()),
                        "fa-hourglass-half",
                        "Yeni rezervasyon talebi",
                        $"{pendingCount} rezervasyon talebi onay bekliyor.",
                        "warning",
                        RelativeTime(timeUtc),
                        "/panel/partner/rezervasyonlar",
                        timeUtc);
                }
            }
        }

        const string latestPendingSql = @"
            SELECT r.[REZERVASYON_NO], COALESCE(o.[OTEL_ADI], 'Otel'), r.[OLUSTURULMA_TARIHI]
            FROM [dbo].[REZERVASYONLAR] r
            INNER JOIN [dbo].[OTEL_KULLANICI_SAHIPLIKLERI] oks ON oks.[OTEL_ID] = r.[OTEL_ID]
            LEFT JOIN [dbo].[OTELLER] o ON o.id = r.[OTEL_ID]
            WHERE oks.[KULLANICI_ID] = @userId
              AND oks.[AKTIF_MI] = 1
              AND r.[DURUM] IN ('Onay Bekliyor', 'Değişiklik Bekliyor')
            ORDER BY r.[OLUSTURULMA_TARIHI] DESC
            OFFSET 0 ROWS FETCH NEXT 1 ROWS ONLY;";
        await using (var command = new SqlCommand(latestPendingSql, connection))
        {
            command.Parameters.AddWithValue("@userId", userId);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                var reservationNo = reader.IsDBNull(0) ? "-" : reader.GetString(0);
                var hotelName = reader.IsDBNull(1) ? "Otel" : reader.GetString(1);
                DateTime? timeUtc = reader.IsDBNull(2) ? null : reader.GetDateTime(2);
                Add(
                    model,
                    BuildItemKey("partner-latest-pending", reservationNo),
                    "fa-hotel",
                    "Rezervasyon panelde hazir",
                    $"{hotelName} icin {reservationNo} rezervasyonunu onayla veya reddet.",
                    "info",
                    RelativeTime(timeUtc),
                    "/panel/partner/rezervasyonlar",
                    timeUtc);
            }
        }

        const string unreadSql = @"
            SELECT COALESCE(SUM(mk.[OTEL_OKUNMAMIS_SAYISI]), 0),
                   MAX(mk.[GUNCELLENME_TARIHI])
            FROM [dbo].[MESAJ_KONUSMALARI] mk
            INNER JOIN [dbo].[OTEL_KULLANICI_SAHIPLIKLERI] oks ON oks.[OTEL_ID] = mk.[OTEL_ID]
            WHERE oks.[KULLANICI_ID] = @userId
              AND oks.[AKTIF_MI] = 1
              AND mk.[DURUM] <> 'Arşivlendi';";
        await using (var command = new SqlCommand(unreadSql, connection))
        {
            command.Parameters.AddWithValue("@userId", userId);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                var unreadCount = reader.IsDBNull(0) ? 0 : Convert.ToInt32(reader.GetValue(0), CultureInfo.InvariantCulture);
                if (unreadCount > 0)
                {
                    DateTime? timeUtc = reader.IsDBNull(1) ? null : reader.GetDateTime(1);
                    Add(
                        model,
                        BuildItemKey("partner-unread-messages", unreadCount.ToString()),
                        "fa-comments",
                        "Misafir mesaji bekliyor",
                        $"{unreadCount} okunmamis misafir mesaji mevcut.",
                        "danger",
                        RelativeTime(timeUtc),
                        "/panel/partner/rezervasyonlar#partner-reservation-chat",
                        timeUtc);
                }
            }
        }

        const string cancellationSql = @"
            SELECT r.[REZERVASYON_NO],
                   COALESCE(o.[OTEL_ADI], 'Otel'),
                   COALESCE(NULLIF(r.[IPTAL_NEDENI], ''), 'Misafir rezervasyonu iptal etti.'),
                   COALESCE(r.[IPTAL_TARIHI], r.[GUNCELLENME_TARIHI], r.[OLUSTURULMA_TARIHI])
            FROM [dbo].[REZERVASYONLAR] r
            INNER JOIN [dbo].[OTEL_KULLANICI_SAHIPLIKLERI] oks ON oks.[OTEL_ID] = r.[OTEL_ID]
            LEFT JOIN [dbo].[OTELLER] o ON o.id = r.[OTEL_ID]
            WHERE oks.[KULLANICI_ID] = @userId
              AND oks.[AKTIF_MI] = 1
              AND r.[DURUM] = 'İptal Edildi'
              AND COALESCE(r.[IPTAL_EDEN], '') = 'Misafir'
            ORDER BY COALESCE(r.[IPTAL_TARIHI], r.[GUNCELLENME_TARIHI], r.[OLUSTURULMA_TARIHI]) DESC
            OFFSET 0 ROWS FETCH NEXT 1 ROWS ONLY;";
        await using (var command = new SqlCommand(cancellationSql, connection))
        {
            command.Parameters.AddWithValue("@userId", userId);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                var reservationNo = reader.IsDBNull(0) ? "-" : reader.GetString(0);
                var hotelName = reader.IsDBNull(1) ? "Otel" : reader.GetString(1);
                var cancelReason = reader.IsDBNull(2) ? "Misafir rezervasyonu iptal etti." : reader.GetString(2);
                DateTime? timeUtc = reader.IsDBNull(3) ? null : reader.GetDateTime(3);
                Add(
                    model,
                    BuildItemKey("partner-cancellation", reservationNo),
                    "fa-ban",
                    "Misafir iptal talebi",
                    $"{hotelName} icin {reservationNo} rezervasyonu iptal edildi. Sebep: {cancelReason}",
                    "danger",
                    RelativeTime(timeUtc),
                    "/panel/partner/rezervasyonlar",
                    timeUtc);
            }
        }

        const string missingInvoiceSql = @"
            IF OBJECT_ID(N'[dbo].[REZERVASYON_FATURALARI]', N'U') IS NULL
            BEGIN
                SELECT 0 AS missing_count, NULL AS last_time;
                RETURN;
            END

            SELECT
                COUNT(*) AS missing_count,
                MAX(COALESCE(r.[GUNCELLENME_TARIHI], r.[CHECK_OUT_TARIHI], r.[CIKIS_TARIHI], r.[OLUSTURULMA_TARIHI])) AS last_time
            FROM [dbo].[REZERVASYONLAR] r
            INNER JOIN [dbo].[OTEL_KULLANICI_SAHIPLIKLERI] oks ON oks.[OTEL_ID] = r.[OTEL_ID]
            LEFT JOIN [dbo].[REZERVASYON_FATURALARI] rf ON rf.[REZERVASYON_ID] = r.id
            WHERE oks.[KULLANICI_ID] = @userId
              AND oks.[AKTIF_MI] = 1
              AND COALESCE(r.[DURUM], '') = N'Tamamlandı'
              AND rf.id IS NULL;";
        await using (var command = new SqlCommand(missingInvoiceSql, connection))
        {
            command.Parameters.AddWithValue("@userId", userId);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                var missingCount = reader.IsDBNull(0) ? 0 : Convert.ToInt32(reader.GetValue(0), CultureInfo.InvariantCulture);
                if (missingCount > 0)
                {
                    DateTime? timeUtc = reader.IsDBNull(1) ? null : reader.GetDateTime(1);
                    Add(
                        model,
                        BuildItemKey("partner-missing-invoices", missingCount.ToString()),
                        "fa-file-invoice",
                        "Eksik misafir faturasi",
                        $"{missingCount} tamamlanmis rezervasyon icin fatura yuklenmemis gorunuyor. Lutfen fatura yukleyin.",
                        "warning",
                        RelativeTime(timeUtc),
                        "/panel/partner/finans/misafir-faturalari",
                        timeUtc);
                }
            }
        }

        var completenessItems = new List<PartnerHotelCompletenessViewModel>();
        try
        {
            completenessItems = await _hotelCompletenessService.GetPartnerManagedHotelsCompletenessAsync(userId, cancellationToken);
        }
        catch (SqlException)
        {
            // Canlı şema geride kaldığında header bildirimleri patlamasın.
        }

        foreach (var hotel in completenessItems.Where(x => x.MissingCount > 0))
        {
            var topMissing = hotel.MissingItems.FirstOrDefault();
            var fixUrl = topMissing?.FixUrl ?? $"/panel/partner?otelId={hotel.HotelId}";
            var description = hotel.MissingCount == 1
                ? $"{hotel.HotelName}: {topMissing?.Label ?? "Eksik alan"} tamamlanmali."
                : $"{hotel.HotelName}: {hotel.MissingCount} eksik alan var (%{hotel.CompletenessScore} tamamlanma).";
            Add(
                model,
                BuildItemKey("partner-hotel-completeness", $"{hotel.HotelId}:{hotel.MissingCount}"),
                topMissing?.IconClass ?? "fa-triangle-exclamation",
                "Tesis profili eksik",
                description,
                hotel.CriticalMissingCount > 0 ? "danger" : "warning",
                "Simdi",
                fixUrl,
                DateTime.UtcNow);
        }
    }

    private async Task FillFirmaItemsAsync(SqlConnection connection, long userId, HeaderBildiriViewModel model, CancellationToken cancellationToken)
    {
        const string firmaSql = "SELECT TOP (1) COALESCE([FIRMA_ID], 0) FROM [dbo].[KULLANICILAR] WHERE id = @userId;";
        long firmaId;
        await using (var command = new SqlCommand(firmaSql, connection))
        {
            command.Parameters.AddWithValue("@userId", userId);
            firmaId = Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken) ?? 0L);
        }

        if (firmaId <= 0)
        {
            return;
        }

        const string pendingApprovalSql = "SELECT COUNT(*) FROM [dbo].[REZERVASYONLAR] WHERE [FIRMA_ID] = @firmaId AND COALESCE([FIRMA_ONAY_DURUMU], '') = 'Beklemede';";
        await using (var command = new SqlCommand(pendingApprovalSql, connection))
        {
            command.Parameters.AddWithValue("@firmaId", firmaId);
            var pendingCount = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken) ?? 0);
            if (pendingCount > 0)
            {
                Add(
                    model,
                    BuildItemKey("firma-pending-approval", pendingCount.ToString()),
                    "fa-clipboard-check",
                    "Firma onayi bekleyen rezervasyonlar",
                    $"{pendingCount} rezervasyon firma onayinizi bekliyor.",
                    "warning",
                    "Bugun",
                    "/panel/firma/limitler-onaylar");
            }
        }

        const string unreadSql = @"
            SELECT COALESCE(SUM([FIRMA_OKUNMAMIS_SAYISI]), 0)
            FROM [dbo].[MESAJ_KONUSMALARI]
            WHERE [FIRMA_ID] = @firmaId
              AND [DURUM] <> 'Arşivlendi';";
        await using (var command = new SqlCommand(unreadSql, connection))
        {
            command.Parameters.AddWithValue("@firmaId", firmaId);
            var unreadCount = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken) ?? 0);
            if (unreadCount > 0)
            {
                Add(
                    model,
                    BuildItemKey("firma-unread-messages", unreadCount.ToString()),
                    "fa-building-user",
                    "Calisan mesajlari var",
                    $"{unreadCount} okunmamis firma mesaji gorunuyor.",
                    "info",
                    "Simdi",
                    "/panel/firma/mesajlar");
            }
        }

        const string todaySql = "SELECT COUNT(*) FROM [dbo].[REZERVASYONLAR] WHERE [FIRMA_ID] = @firmaId AND CAST([OLUSTURULMA_TARIHI] AS date) = CAST(SYSUTCDATETIME() AS date);";
        await using (var command = new SqlCommand(todaySql, connection))
        {
            command.Parameters.AddWithValue("@firmaId", firmaId);
            var todayCount = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken) ?? 0);
            if (todayCount > 0)
            {
                Add(
                    model,
                    BuildItemKey("firma-today-reservation", todayCount.ToString()),
                    "fa-briefcase",
                    "Bugunku kurumsal rezervasyonlar",
                    $"Bugun firmaniz adina {todayCount} rezervasyon olusturuldu.",
                    "success",
                    "Bugun",
                    "/panel/firma/rezervasyonlar");
            }
        }
    }

    private async Task FillSalesItemsAsync(SqlConnection connection, long userId, HeaderBildiriViewModel model, CancellationToken cancellationToken)
    {
        const string todaySql = "SELECT COUNT(*) FROM [dbo].[REZERVASYONLAR] WHERE [SATIS_TEMSILCISI_ID] = @userId AND CAST([OLUSTURULMA_TARIHI] AS date) = CAST(SYSUTCDATETIME() AS date);";
        await using (var command = new SqlCommand(todaySql, connection))
        {
            command.Parameters.AddWithValue("@userId", userId);
            var todayCount = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken) ?? 0);
            if (todayCount > 0)
            {
                Add(
                    model,
                    BuildItemKey("sales-today-reservation", todayCount.ToString()),
                    "fa-bolt",
                    "Bugunku satis performansi",
                    $"Bugun {todayCount} rezervasyon olusturdunuz.",
                    "success",
                    "Bugun",
                    "/panel/satis/rezervasyonlarim");
            }
        }

        const string monthRevenueSql = @"
            SELECT COALESCE(SUM([TOPLAM_TUTAR]), 0)
            FROM [dbo].[REZERVASYONLAR]
            WHERE [SATIS_TEMSILCISI_ID] = @userId
              AND [OLUSTURULMA_TARIHI] >= DATEFROMPARTS(YEAR(SYSUTCDATETIME()), MONTH(SYSUTCDATETIME()), 1)
              AND [OLUSTURULMA_TARIHI] < DATEADD(MONTH, 1, DATEFROMPARTS(YEAR(SYSUTCDATETIME()), MONTH(SYSUTCDATETIME()), 1));";
        await using (var command = new SqlCommand(monthRevenueSql, connection))
        {
            command.Parameters.AddWithValue("@userId", userId);
            var monthRevenue = Convert.ToDecimal(await command.ExecuteScalarAsync(cancellationToken) ?? 0m);
            if (monthRevenue > 0)
            {
                Add(
                    model,
                    BuildItemKey("sales-month-revenue", monthRevenue.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture)),
                    "fa-money-bill-trend-up",
                    "Aylik gelir takibi",
                    $"Bu ay {monthRevenue:N0} TL ciroya ulastiniz.",
                    "vip",
                    "Bu ay",
                    "/panel/satis/raporlar");
            }
        }

        const string pendingSql = "SELECT COUNT(*) FROM [dbo].[REZERVASYONLAR] WHERE [SATIS_TEMSILCISI_ID] = @userId AND [DURUM] = 'Onay Bekliyor';";
        await using (var command = new SqlCommand(pendingSql, connection))
        {
            command.Parameters.AddWithValue("@userId", userId);
            var pendingCount = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken) ?? 0);
            if (pendingCount > 0)
            {
                Add(
                    model,
                    BuildItemKey("sales-pending", pendingCount.ToString()),
                    "fa-list-check",
                    "Onay bekleyen kayitlar",
                    $"{pendingCount} rezervasyonunuz partner onayi bekliyor.",
                    "warning",
                    "Guncel",
                    "/panel/satis/rezervasyonlarim");
            }
        }
    }

    private async Task<SqlConnection> OpenConnectionAsync(CancellationToken cancellationToken)
    {
        var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }

    private static string BuildItemKey(string prefix, string uniquePart)
        => $"{prefix}:{uniquePart}".ToLowerInvariant();

    private static async Task ApplyReadStateAsync(SqlConnection connection, string panelKey, long userId, HeaderBildiriViewModel model, CancellationToken cancellationToken)
    {
        var keys = model.Items
            .Where(static item => !item.IsPlaceholder && !string.IsNullOrWhiteSpace(item.ItemKey))
            .Select(static item => item.ItemKey)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (keys.Count == 0)
        {
            return;
        }

        var placeholders = string.Join(", ", keys.Select((_, index) => $"@key{index}"));
        var sql = $@"
            SELECT [BILDIRI_ANAHTARI], COALESCE([OKUNDU_MI], 0)
            FROM [dbo].[PANEL_HEADER_BILDIRI_OKUMALARI]
            WHERE [PANEL_KODU] = @panelKey
              AND [KULLANICI_ID] = @userId
              AND [BILDIRI_ANAHTARI] IN ({placeholders});";
        var readMap = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        await using (var command = new SqlCommand(sql, connection))
        {
            command.Parameters.AddWithValue("@panelKey", panelKey);
            command.Parameters.AddWithValue("@userId", userId);
            for (var i = 0; i < keys.Count; i++)
            {
                command.Parameters.AddWithValue($"@key{i}", keys[i]);
            }

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var key = reader.IsDBNull(0) ? string.Empty : reader.GetString(0);
                if (string.IsNullOrWhiteSpace(key))
                {
                    continue;
                }

                readMap[key] = !reader.IsDBNull(1) && reader.GetBoolean(1);
            }
        }

        foreach (var item in model.Items)
        {
            if (item.IsPlaceholder || string.IsNullOrWhiteSpace(item.ItemKey))
            {
                item.IsRead = true;
                continue;
            }

            item.IsRead = readMap.TryGetValue(item.ItemKey, out var isRead) && isRead;
        }
    }
}
