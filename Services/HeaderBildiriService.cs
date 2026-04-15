using MySqlConnector;
using otelturizmnew.Models.Paneller.Common;
using otelturizmnew.Services.Abstractions;

namespace otelturizmnew.Services;

public class HeaderBildiriService : IHeaderBildiriService
{
    private readonly string _connectionString;

    public HeaderBildiriService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection tanimli degil.");
    }

    public async Task<HeaderBildiriViewModel> GetForPanelAsync(string panelKey, long userId, CancellationToken cancellationToken = default)
    {
        var safeKey = NormalizePanelKey(panelKey);
        var model = new HeaderBildiriViewModel
        {
            PanelKey = safeKey,
            PanelLabel = ResolvePanelLabel(safeKey)
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
                Url = "#",
                IsPlaceholder = true,
                IsRead = true
            });
        }
        else
        {
            await ApplyReadStateAsync(connection, safeKey, userId, model, cancellationToken);
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
                    INSERT INTO panel_header_bildiri_okumalari
                    (panel_kodu, kullanici_id, bildiri_anahtari, okundu_mi, okundu_tarihi, guncellenme_tarihi)
                    VALUES
                    (@panelKey, @userId, @itemKey, 1, UTC_TIMESTAMP(), CURRENT_TIMESTAMP)
                    ON DUPLICATE KEY UPDATE
                        okundu_mi = VALUES(okundu_mi),
                        okundu_tarihi = VALUES(okundu_tarihi),
                        guncellenme_tarihi = CURRENT_TIMESTAMP;";
                await using var command = new MySqlCommand(upsertSql, connection, (MySqlTransaction)transaction);
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

    private static void Add(HeaderBildiriViewModel model, string key, string icon, string title, string description, string tone, string timeLabel, string url)
    {
        model.Items.Add(new HeaderBildiriItemViewModel
        {
            ItemKey = key,
            IconClass = icon,
            Title = title,
            Description = description,
            Tone = tone,
            TimeLabel = timeLabel,
            Url = string.IsNullOrWhiteSpace(url) ? "#" : url
        });
    }

    private async Task FillUserItemsAsync(MySqlConnection connection, long userId, HeaderBildiriViewModel model, CancellationToken cancellationToken)
    {
        const string approvedSql = @"
            SELECT r.id, r.rezervasyon_no, COALESCE(o.otel_adi, 'Otel'), COALESCE(r.otel_onay_tarihi, r.guncellenme_tarihi, r.olusturulma_tarihi)
            FROM rezervasyonlar r
            LEFT JOIN oteller o ON o.id = r.otel_id
            WHERE r.kullanici_id = @userId
              AND (r.durum = 'Onaylandı' OR COALESCE(r.otel_onay_durumu, '') = 'Onaylandı')
            ORDER BY COALESCE(r.otel_onay_tarihi, r.guncellenme_tarihi, r.olusturulma_tarihi) DESC
            LIMIT 1;";

        await using (var command = new MySqlCommand(approvedSql, connection))
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

        const string birthdaySql = "SELECT COALESCE(ad_soyad, ''), dogum_tarihi FROM users WHERE id = @userId LIMIT 1;";
        await using (var command = new MySqlCommand(birthdaySql, connection))
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

        const string reservationCountSql = "SELECT COUNT(*) FROM rezervasyonlar WHERE kullanici_id = @userId;";
        await using (var command = new MySqlCommand(reservationCountSql, connection))
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
            SELECT COALESCE(SUM(misafir_okunmamis_sayisi), 0)
            FROM mesaj_konusmalari
            WHERE misafir_kullanici_id = @userId
              AND durum <> 'Arşivlendi';";
        await using (var command = new MySqlCommand(unreadSql, connection))
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

    private async Task FillPartnerItemsAsync(MySqlConnection connection, long userId, HeaderBildiriViewModel model, CancellationToken cancellationToken)
    {
        const string pendingSql = @"
            SELECT COUNT(*)
            FROM rezervasyonlar r
            INNER JOIN otel_kullanici_sahiplikleri oks ON oks.otel_id = r.otel_id
            WHERE oks.user_id = @userId
              AND oks.aktif_mi = 1
              AND r.durum IN ('Onay Bekliyor', 'Değişiklik Bekliyor');";
        await using (var command = new MySqlCommand(pendingSql, connection))
        {
            command.Parameters.AddWithValue("@userId", userId);
            var pendingCount = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken) ?? 0);
            if (pendingCount > 0)
            {
                Add(
                    model,
                    BuildItemKey("partner-pending-count", pendingCount.ToString()),
                    "fa-hourglass-half",
                    "Yeni rezervasyon talebi",
                    $"{pendingCount} rezervasyon talebi onay bekliyor.",
                    "warning",
                    "Bugun",
                    "/panel/partner/rezervasyonlar");
            }
        }

        const string latestPendingSql = @"
            SELECT r.rezervasyon_no, COALESCE(o.otel_adi, 'Otel'), r.olusturulma_tarihi
            FROM rezervasyonlar r
            INNER JOIN otel_kullanici_sahiplikleri oks ON oks.otel_id = r.otel_id
            LEFT JOIN oteller o ON o.id = r.otel_id
            WHERE oks.user_id = @userId
              AND oks.aktif_mi = 1
              AND r.durum IN ('Onay Bekliyor', 'Değişiklik Bekliyor')
            ORDER BY r.olusturulma_tarihi DESC
            LIMIT 1;";
        await using (var command = new MySqlCommand(latestPendingSql, connection))
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
                    "/panel/partner/rezervasyonlar");
            }
        }

        const string unreadSql = @"
            SELECT COALESCE(SUM(mk.otel_okunmamis_sayisi), 0)
            FROM mesaj_konusmalari mk
            INNER JOIN otel_kullanici_sahiplikleri oks ON oks.otel_id = mk.otel_id
            WHERE oks.user_id = @userId
              AND oks.aktif_mi = 1
              AND mk.durum <> 'Arşivlendi';";
        await using (var command = new MySqlCommand(unreadSql, connection))
        {
            command.Parameters.AddWithValue("@userId", userId);
            var unreadCount = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken) ?? 0);
            if (unreadCount > 0)
            {
                Add(
                    model,
                    BuildItemKey("partner-unread-messages", unreadCount.ToString()),
                    "fa-comments",
                    "Misafir mesaji bekliyor",
                    $"{unreadCount} okunmamis misafir mesaji mevcut.",
                    "danger",
                    "Simdi",
                    "/panel/partner/rezervasyonlar#partner-reservation-chat");
            }
        }

        const string cancellationSql = @"
            SELECT r.rezervasyon_no,
                   COALESCE(o.otel_adi, 'Otel'),
                   COALESCE(NULLIF(r.iptal_nedeni, ''), 'Misafir rezervasyonu iptal etti.'),
                   COALESCE(r.iptal_tarihi, r.guncellenme_tarihi, r.olusturulma_tarihi)
            FROM rezervasyonlar r
            INNER JOIN otel_kullanici_sahiplikleri oks ON oks.otel_id = r.otel_id
            LEFT JOIN oteller o ON o.id = r.otel_id
            WHERE oks.user_id = @userId
              AND oks.aktif_mi = 1
              AND r.durum = 'İptal Edildi'
              AND COALESCE(r.iptal_eden, '') = 'Misafir'
            ORDER BY COALESCE(r.iptal_tarihi, r.guncellenme_tarihi, r.olusturulma_tarihi) DESC
            LIMIT 1;";
        await using (var command = new MySqlCommand(cancellationSql, connection))
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
                    "/panel/partner/rezervasyonlar");
            }
        }
    }

    private async Task FillFirmaItemsAsync(MySqlConnection connection, long userId, HeaderBildiriViewModel model, CancellationToken cancellationToken)
    {
        const string firmaSql = "SELECT COALESCE(firma_id, 0) FROM users WHERE id = @userId LIMIT 1;";
        long firmaId;
        await using (var command = new MySqlCommand(firmaSql, connection))
        {
            command.Parameters.AddWithValue("@userId", userId);
            firmaId = Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken) ?? 0L);
        }

        if (firmaId <= 0)
        {
            return;
        }

        const string pendingApprovalSql = "SELECT COUNT(*) FROM rezervasyonlar WHERE firma_id = @firmaId AND COALESCE(firma_onay_durumu, '') = 'Beklemede';";
        await using (var command = new MySqlCommand(pendingApprovalSql, connection))
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
            SELECT COALESCE(SUM(firma_okunmamis_sayisi), 0)
            FROM mesaj_konusmalari
            WHERE firma_id = @firmaId
              AND durum <> 'Arşivlendi';";
        await using (var command = new MySqlCommand(unreadSql, connection))
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

        const string todaySql = "SELECT COUNT(*) FROM rezervasyonlar WHERE firma_id = @firmaId AND DATE(olusturulma_tarihi) = CURDATE();";
        await using (var command = new MySqlCommand(todaySql, connection))
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

    private async Task FillSalesItemsAsync(MySqlConnection connection, long userId, HeaderBildiriViewModel model, CancellationToken cancellationToken)
    {
        const string todaySql = "SELECT COUNT(*) FROM rezervasyonlar WHERE satis_temsilcisi_id = @userId AND DATE(olusturulma_tarihi) = CURDATE();";
        await using (var command = new MySqlCommand(todaySql, connection))
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
            SELECT COALESCE(SUM(toplam_tutar), 0)
            FROM rezervasyonlar
            WHERE satis_temsilcisi_id = @userId
              AND olusturulma_tarihi >= DATE_FORMAT(CURDATE(), '%Y-%m-01')
              AND olusturulma_tarihi < DATE_ADD(DATE_FORMAT(CURDATE(), '%Y-%m-01'), INTERVAL 1 MONTH);";
        await using (var command = new MySqlCommand(monthRevenueSql, connection))
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

        const string pendingSql = "SELECT COUNT(*) FROM rezervasyonlar WHERE satis_temsilcisi_id = @userId AND durum = 'Onay Bekliyor';";
        await using (var command = new MySqlCommand(pendingSql, connection))
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

    private async Task<MySqlConnection> OpenConnectionAsync(CancellationToken cancellationToken)
    {
        var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }

    private static string BuildItemKey(string prefix, string uniquePart)
        => $"{prefix}:{uniquePart}".ToLowerInvariant();

    private static async Task ApplyReadStateAsync(MySqlConnection connection, string panelKey, long userId, HeaderBildiriViewModel model, CancellationToken cancellationToken)
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
            SELECT bildiri_anahtari, COALESCE(okundu_mi, 0)
            FROM panel_header_bildiri_okumalari
            WHERE panel_kodu = @panelKey
              AND kullanici_id = @userId
              AND bildiri_anahtari IN ({placeholders});";
        var readMap = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        await using (var command = new MySqlCommand(sql, connection))
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
