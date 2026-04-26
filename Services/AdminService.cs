using System.Globalization;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;
using SqlConnection = Microsoft.Data.SqlClient.SqlConnection;
using SqlCommand = Microsoft.Data.SqlClient.SqlCommand;
using SqlTransaction = Microsoft.Data.SqlClient.SqlTransaction;
using SqlException = Microsoft.Data.SqlClient.SqlException;
using otelturizmnew.Models.Paneller.Admin;
using otelturizmnew.Services.Abstractions;

namespace otelturizmnew.Services;

public class AdminService : IAdminService
{
    private readonly string _connectionString;

    public AdminService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection tanimli degil.");
    }

    public async Task<AdminDashboardViewModel> GetDashboardAsync(string fullName, string email, string userRole, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var shell = await GetShellAsync(connection, "Dashboard", "Panel genel operasyon durumunu ve kritik metrikleri canli verilerle takip edin.", fullName, email, userRole, cancellationToken);
        var model = new AdminDashboardViewModel { Shell = shell };

        const string metricsSql = @"
            SELECT
                (SELECT COUNT(*) FROM oteller) AS total_hotels,
                (SELECT COUNT(*) FROM rezervasyonlar) AS total_reservations,
                (SELECT COUNT(*) FROM rezervasyonlar WHERE durum = 'İptal Edildi') AS cancelled_reservations,
                (SELECT COALESCE(SUM(COALESCE(toplam_tutar,0)),0) FROM rezervasyonlar WHERE COALESCE(durum,'') <> 'İptal Edildi') AS gross_revenue,
                (SELECT COALESCE(SUM(COALESCE(komisyon_tutari,0)),0) FROM komisyon_muhasebe_kayitlari) AS total_commission,
                (SELECT COUNT(*) FROM odeme_islemleri WHERE odeme_durumu IN ('Başarılı','Geri Ödendi','Kısmi Geri Ödendi')) AS successful_payments,
                (SELECT COUNT(*) FROM users WHERE rol = 'admin') AS admin_count,
                (SELECT COUNT(*) FROM partner_detaylari WHERE onay_durumu = 'Beklemede') AS pending_partner_count,
                (SELECT COUNT(*) FROM firmalar WHERE COALESCE(onay_durumu, 'Beklemede') = 'Beklemede') AS pending_company_count,
                (SELECT COUNT(*) FROM oteller WHERE yayin_durumu = 'Yayında' AND onay_durumu = 'Onaylandı') AS active_hotel_count,
                (SELECT COUNT(*) FROM oteller WHERE COALESCE(onay_durumu, '') = 'Beklemede') AS pending_hotel_count;";

        await using (var command = new SqlCommand(metricsSql, connection))
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            if (await reader.ReadAsync(cancellationToken))
            {
                model.Metrics.Add(new AdminMetricCardViewModel { Label = "Toplam Otel", Value = SafeInt(reader, 0).ToString(), TrendText = "Yayin, taslak ve bakim tum oteller", IconClass = "fa-hotel", ToneClass = "info" });
                model.Metrics.Add(new AdminMetricCardViewModel { Label = "Toplam Rezervasyon", Value = SafeInt(reader, 1).ToString(), TrendText = "Tum rezervasyon kayitlari", IconClass = "fa-calendar-check", ToneClass = "success" });
                model.Metrics.Add(new AdminMetricCardViewModel { Label = "İptal", Value = SafeInt(reader, 2).ToString(), TrendText = "İptal edilen rezervasyonlar", IconClass = "fa-ban", ToneClass = "danger" });
                model.Metrics.Add(new AdminMetricCardViewModel { Label = "Toplam Ciro", Value = $"{SafeDecimal(reader, 3):N0} TL", TrendText = "İptal hariç toplam tutar", IconClass = "fa-money-bill-wave", ToneClass = "warning" });
                model.Metrics.Add(new AdminMetricCardViewModel { Label = "Komisyon", Value = $"{SafeDecimal(reader, 4):N0} TL", TrendText = "Komisyon muhasebe toplamı", IconClass = "fa-percent", ToneClass = "info" });
                model.Metrics.Add(new AdminMetricCardViewModel { Label = "Basarili Odeme", Value = SafeInt(reader, 5).ToString(), TrendText = "Tahsilat ve iade dahil tamamlanan islemler", IconClass = "fa-credit-card", ToneClass = "success" });
                model.Metrics.Add(new AdminMetricCardViewModel { Label = "Bekleyen Partner", Value = SafeInt(reader, 7).ToString(), TrendText = "Onay bekleyen partner basvurulari", IconClass = "fa-handshake", ToneClass = "warning" });
                model.Metrics.Add(new AdminMetricCardViewModel { Label = "Bekleyen Firma", Value = SafeInt(reader, 8).ToString(), TrendText = "Onay bekleyen firma basvurulari", IconClass = "fa-building", ToneClass = "info" });
                model.Metrics.Add(new AdminMetricCardViewModel { Label = "Açık Otel", Value = SafeInt(reader, 9).ToString(), TrendText = "Yayinda ve onayli tesisler", IconClass = "fa-tower-broadcast", ToneClass = "success" });
                model.Metrics.Add(new AdminMetricCardViewModel { Label = "Bekleyen Otel", Value = SafeInt(reader, 10).ToString(), TrendText = "Onay/yayin aksiyonunda bekleyen tesisler", IconClass = "fa-hourglass-half", ToneClass = "danger" });
            }
        }

        const string chartSql = @"
            SELECT FORMAT(olusturulma_tarihi, 'MMM', 'tr-TR') AS ay, COUNT(*) AS adet
            FROM rezervasyonlar
            WHERE olusturulma_tarihi >= DATEADD(MONTH, -5, DATEFROMPARTS(YEAR(GETDATE()), MONTH(GETDATE()), 1))
            GROUP BY YEAR(olusturulma_tarihi), MONTH(olusturulma_tarihi), FORMAT(olusturulma_tarihi, 'MMM', 'tr-TR')
            ORDER BY YEAR(olusturulma_tarihi), MONTH(olusturulma_tarihi);";

        var chartRows = new List<(string Label, int Value)>();
        await using (var chartCommand = new SqlCommand(chartSql, connection))
        await using (var chartReader = await chartCommand.ExecuteReaderAsync(cancellationToken))
        {
            while (await chartReader.ReadAsync(cancellationToken))
            {
                chartRows.Add((chartReader.GetString(0), SafeInt(chartReader, 1)));
            }
        }

        var maxChart = Math.Max(chartRows.Count == 0 ? 0 : chartRows.Max(static item => item.Value), 1);
        foreach (var row in chartRows)
        {
            model.ReservationChart.Add(new AdminChartBarViewModel
            {
                Label = row.Label,
                Value = row.Value,
                HeightPercent = Math.Max(12, (int)Math.Round(row.Value * 100m / maxChart))
            });
        }

        const string activitySql = @"
            SELECT TOP (6) *
            FROM
            (
                SELECT 'Partner basvurusu' AS baslik,
                       CONCAT(p.firma_unvani, ' · ', p.onay_durumu) AS alt_baslik,
                       p.olusturulma_tarihi AS zaman
                FROM partner_detaylari p
                UNION ALL
                SELECT 'Admin islemi',
                       CONCAT(a.hedef_tablo, ' · ', a.islem_turu),
                       a.islem_tarihi
                FROM admin_islem_loglari a
                UNION ALL
                SELECT 'Sistem hatasi',
                       CONCAT(s.hata_seviyesi, ' · ', LEFT(s.hata_mesaji, 70)),
                       s.olusma_tarihi
                FROM sistem_hata_loglari s
            ) activity_feed
            ORDER BY zaman DESC;";

        await using (var activityCommand = new SqlCommand(activitySql, connection))
        await using (var activityReader = await activityCommand.ExecuteReaderAsync(cancellationToken))
        {
            while (await activityReader.ReadAsync(cancellationToken))
            {
                var title = activityReader.GetString(0);
                var tone = title.Contains("hata", StringComparison.OrdinalIgnoreCase)
                    ? "danger"
                    : title.Contains("Partner", StringComparison.OrdinalIgnoreCase) ? "warning" : "info";

                model.Activities.Add(new AdminActivityViewModel
                {
                    Title = title,
                    Subtitle = activityReader.GetString(1),
                    TimeText = FormatRelative(activityReader.IsDBNull(2) ? null : activityReader.GetDateTime(2)),
                    IconClass = title.Contains("hata", StringComparison.OrdinalIgnoreCase) ? "fa-triangle-exclamation" : title.Contains("Admin", StringComparison.OrdinalIgnoreCase) ? "fa-user-gear" : "fa-file-signature",
                    ToneClass = tone
                });
            }
        }

        const string hotelsSql = @"
            SELECT TOP (6)
                o.otel_adi,
                CONCAT(o.ilce, ', ', o.sehir) AS sehir_label,
                o.yayin_durumu,
                o.ortalama_puan,
                COUNT(r.id) AS rezervasyon_adedi
            FROM oteller o
            LEFT JOIN rezervasyonlar r ON r.otel_id = o.id
            GROUP BY o.id, o.otel_adi, o.ilce, o.sehir, o.yayin_durumu, o.ortalama_puan
            ORDER BY rezervasyon_adedi DESC, o.id DESC;";

        await using (var hotelsCommand = new SqlCommand(hotelsSql, connection))
        await using (var hotelsReader = await hotelsCommand.ExecuteReaderAsync(cancellationToken))
        {
            while (await hotelsReader.ReadAsync(cancellationToken))
            {
                var status = hotelsReader.GetString(2);
                model.HighlightHotels.Add(new AdminDashboardHotelRowViewModel
                {
                    HotelName = hotelsReader.GetString(0),
                    CityLabel = hotelsReader.GetString(1),
                    StatusLabel = status,
                    StatusToneClass = MapStatusTone(status),
                    ScoreText = hotelsReader.IsDBNull(3) ? "-" : hotelsReader.GetDecimal(3).ToString("0.0"),
                    ReservationText = SafeInt(hotelsReader, 4).ToString()
                });
            }
        }

        const string hotelKpiSql = @"
            SELECT TOP (30)
                o.id,
                o.otel_adi,
                CONCAT(o.ilce, ', ', o.sehir) AS sehir_label,
                o.yayin_durumu,
                COALESCE(resStats.res_count, 0) AS res_count,
                COALESCE(resStats.cancel_count, 0) AS cancel_count,
                COALESCE(resStats.gross_revenue, 0) AS gross_revenue,
                COALESCE(commStats.commission_amount, 0) AS commission_amount,
                COALESCE(o.toplam_yorum_sayisi, 0) AS review_count,
                COALESCE(o.ortalama_puan, 0) AS avg_score
            FROM oteller o
            OUTER APPLY
            (
                SELECT
                    COUNT(*) AS res_count,
                    SUM(CASE WHEN r.durum = 'İptal Edildi' THEN 1 ELSE 0 END) AS cancel_count,
                    SUM(CASE WHEN COALESCE(r.durum,'') <> 'İptal Edildi' THEN COALESCE(r.toplam_tutar,0) ELSE 0 END) AS gross_revenue
                FROM rezervasyonlar r
                WHERE r.otel_id = o.id
            ) resStats
            OUTER APPLY
            (
                SELECT SUM(COALESCE(k.komisyon_tutari,0)) AS commission_amount
                FROM komisyon_muhasebe_kayitlari k
                WHERE k.otel_id = o.id
            ) commStats
            WHERE COALESCE(resStats.res_count, 0) > 0 OR COALESCE(commStats.commission_amount, 0) > 0
            ORDER BY COALESCE(resStats.gross_revenue, 0) DESC, COALESCE(resStats.res_count, 0) DESC, o.id DESC;";

        await using (var kpiCommand = new SqlCommand(hotelKpiSql, connection))
        await using (var kpiReader = await kpiCommand.ExecuteReaderAsync(cancellationToken))
        {
            while (await kpiReader.ReadAsync(cancellationToken))
            {
                model.HotelKpis.Add(new AdminDashboardHotelKpiRowViewModel
                {
                    HotelId = kpiReader.GetInt64(0),
                    HotelName = kpiReader.GetString(1),
                    CityLabel = kpiReader.IsDBNull(2) ? "-" : kpiReader.GetString(2),
                    PublishStatus = kpiReader.IsDBNull(3) ? "-" : kpiReader.GetString(3),
                    ReservationCount = SafeInt(kpiReader, 4),
                    CancelledCount = SafeInt(kpiReader, 5),
                    GrossRevenue = SafeDecimal(kpiReader, 6),
                    CommissionAmount = SafeDecimal(kpiReader, 7),
                    ReviewCount = SafeInt(kpiReader, 8),
                    AverageScore = SafeDecimal(kpiReader, 9)
                });
            }
        }

        return model;
    }

    public async Task<AdminSectionPageViewModel> GetSectionPageAsync(string sectionKey, string fullName, string email, string userRole, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var config = GetSectionConfig(sectionKey);
        var model = new AdminSectionPageViewModel
        {
            SectionKey = sectionKey,
            Shell = await GetShellAsync(connection, config.Title, config.Subtitle, fullName, email, userRole, cancellationToken),
            EmptyStateMessage = config.EmptyMessage,
            InfoNote = config.InfoNote
        };

        model.Columns.AddRange(config.Columns.Select(static column => new AdminTableColumnViewModel { Label = column }));

        await FillSummaryCardsAsync(connection, model, sectionKey, cancellationToken);
        await FillTableAsync(connection, model, sectionKey, cancellationToken);

        return model;
    }

    public async Task<AdminSystemHealthPageViewModel> GetSystemHealthAsync(string fullName, string email, string userRole, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var shell = await GetShellAsync(connection, "Sistem Sağlığı", "E-posta kuyruğu, başvurular, kritik hatalar ve servis konfigürasyonlarını tek ekrandan doğrulayın.", fullName, email, userRole, cancellationToken);
        var model = new AdminSystemHealthPageViewModel { Shell = shell };

        // DB check
        try
        {
            await using var ping = new SqlCommand("SELECT 1;", connection);
            await ping.ExecuteScalarAsync(cancellationToken);
            model.Checks.Add(new AdminSystemHealthCheckItemViewModel { Title = "Veritabanı Bağlantısı", StatusText = "OK", ToneClass = "success", Detail = "SQL bağlantısı çalışıyor." });
        }
        catch (Exception ex)
        {
            model.Checks.Add(new AdminSystemHealthCheckItemViewModel { Title = "Veritabanı Bağlantısı", StatusText = "HATA", ToneClass = "danger", Detail = ex.Message });
        }

        // SMTP / email service config
        const string smtpSql = @"
            SELECT TOP (1) saglayici, gonderen_eposta, test_modu, aktif_mi
            FROM email_services
            WHERE aktif_mi = 1
            ORDER BY varsayilan_mi DESC, id ASC;";
        await using (var smtpCmd = new SqlCommand(smtpSql, connection))
        await using (var smtpReader = await smtpCmd.ExecuteReaderAsync(cancellationToken))
        {
            if (await smtpReader.ReadAsync(cancellationToken))
            {
                var provider = smtpReader.IsDBNull(0) ? "-" : smtpReader.GetString(0);
                var sender = smtpReader.IsDBNull(1) ? "-" : smtpReader.GetString(1);
                var testMode = !smtpReader.IsDBNull(2) && smtpReader.GetBoolean(2);
                model.Checks.Add(new AdminSystemHealthCheckItemViewModel
                {
                    Title = "E-posta Servisi",
                    StatusText = testMode ? "TEST MODU" : "AKTİF",
                    ToneClass = testMode ? "warning" : "success",
                    Detail = $"{provider} · Gönderen: {sender}"
                });
            }
            else
            {
                model.Checks.Add(new AdminSystemHealthCheckItemViewModel { Title = "E-posta Servisi", StatusText = "YOK", ToneClass = "danger", Detail = "Aktif email_services kaydı bulunamadı." });
            }
        }

        // Queue stats: bildirim_loglari (email)
        const string queueSql = @"
            SELECT
                SUM(CASE WHEN tur='E-posta' AND durum='Beklemede' THEN 1 ELSE 0 END) AS email_pending,
                SUM(CASE WHEN tur='E-posta' AND durum='Başarısız' THEN 1 ELSE 0 END) AS email_failed,
                MIN(CASE WHEN tur='E-posta' AND durum='Beklemede' THEN olusturulma_tarihi ELSE NULL END) AS email_oldest_pending,
                SUM(CASE WHEN tur='Sistem İçi' AND durum='Beklemede' THEN 1 ELSE 0 END) AS system_pending
            FROM bildirim_loglari;";
        await using (var queueCmd = new SqlCommand(queueSql, connection))
        await using (var queueReader = await queueCmd.ExecuteReaderAsync(cancellationToken))
        {
            if (await queueReader.ReadAsync(cancellationToken))
            {
                var emailPending = SafeInt(queueReader, 0);
                var emailFailed = SafeInt(queueReader, 1);
                var oldest = queueReader.IsDBNull(2) ? (DateTime?)null : queueReader.GetDateTime(2);
                model.Queues.Add(new AdminSystemHealthQueueRowViewModel
                {
                    QueueName = "E-posta Kuyruğu",
                    PendingCount = emailPending,
                    FailedCount = emailFailed,
                    OldestPendingText = oldest.HasValue ? oldest.Value.ToString("dd.MM.yyyy HH:mm", CultureInfo.GetCultureInfo("tr-TR")) : "-",
                    Note = "Kaynak: bildirim_loglari (tur='E-posta')"
                });
            }
        }

        // Critical operational counts
        const string opsSql = @"
            SELECT
                (SELECT COUNT(*) FROM firmalar WHERE COALESCE(onay_durumu,'Beklemede')='Beklemede') AS pending_company,
                (SELECT COUNT(*) FROM partner_detaylari WHERE onay_durumu='Beklemede') AS pending_partner,
                (SELECT COUNT(*) FROM yorumlar WHERE onay_durumu='Beklemede') AS pending_reviews,
                (SELECT COUNT(*) FROM rezervasyonlar WHERE durum='Onay Bekliyor') AS pending_reservations,
                (SELECT COUNT(*) FROM sistem_hata_loglari WHERE hata_seviyesi IN ('CRITICAL','ALERT','EMERGENCY') AND cozuldu_mu=0) AS critical_errors;";
        await using (var opsCmd = new SqlCommand(opsSql, connection))
        await using (var opsReader = await opsCmd.ExecuteReaderAsync(cancellationToken))
        {
            if (await opsReader.ReadAsync(cancellationToken))
            {
                model.Checks.Add(new AdminSystemHealthCheckItemViewModel
                {
                    Title = "Bekleyen Firma Başvurusu",
                    StatusText = SafeInt(opsReader, 0).ToString(),
                    ToneClass = SafeInt(opsReader, 0) > 0 ? "warning" : "success",
                    Detail = "Admin onay akışına düşmesi beklenir."
                });
                model.Checks.Add(new AdminSystemHealthCheckItemViewModel
                {
                    Title = "Bekleyen Partner Başvurusu",
                    StatusText = SafeInt(opsReader, 1).ToString(),
                    ToneClass = SafeInt(opsReader, 1) > 0 ? "warning" : "success",
                    Detail = "Partner onboarding kuyruğu."
                });
                model.Checks.Add(new AdminSystemHealthCheckItemViewModel
                {
                    Title = "Bekleyen Yorum",
                    StatusText = SafeInt(opsReader, 2).ToString(),
                    ToneClass = SafeInt(opsReader, 2) > 0 ? "warning" : "success",
                    Detail = "Moderasyon bekleyen yorumlar."
                });
                model.Checks.Add(new AdminSystemHealthCheckItemViewModel
                {
                    Title = "Onay Bekleyen Rezervasyon",
                    StatusText = SafeInt(opsReader, 3).ToString(),
                    ToneClass = SafeInt(opsReader, 3) > 0 ? "warning" : "success",
                    Detail = "Operasyon bekleyen rezervasyonlar."
                });
                model.Checks.Add(new AdminSystemHealthCheckItemViewModel
                {
                    Title = "Kritik Sistem Hatası",
                    StatusText = SafeInt(opsReader, 4).ToString(),
                    ToneClass = SafeInt(opsReader, 4) > 0 ? "danger" : "success",
                    Detail = "Kaynak: sistem_hata_loglari"
                });
            }
        }

        // Corporate pricing table existence & volume
        const string firmPricingSql = @"
            SELECT
                CASE WHEN OBJECT_ID(N'dbo.firma_oda_fiyat_musaitlik', N'U') IS NULL THEN 0 ELSE 1 END AS exists_flag,
                CASE WHEN OBJECT_ID(N'dbo.firma_oda_fiyat_musaitlik', N'U') IS NULL THEN 0 ELSE (SELECT COUNT(*) FROM firma_oda_fiyat_musaitlik) END AS row_count;";
        await using (var firmPricingCmd = new SqlCommand(firmPricingSql, connection))
        await using (var firmPricingReader = await firmPricingCmd.ExecuteReaderAsync(cancellationToken))
        {
            if (await firmPricingReader.ReadAsync(cancellationToken))
            {
                var exists = SafeInt(firmPricingReader, 0) == 1;
                var count = SafeInt(firmPricingReader, 1);
                model.Checks.Add(new AdminSystemHealthCheckItemViewModel
                {
                    Title = "Firma Günlük Fiyat Tablosu",
                    StatusText = exists ? "VAR" : "YOK",
                    ToneClass = exists ? "success" : "danger",
                    Detail = exists ? $"firma_oda_fiyat_musaitlik satır: {count:N0}" : "Migration henüz DB'ye uygulanmamış."
                });
            }
        }

        // Hotel listing subscriptions table existence & volume
        const string listingSubSql = @"
            SELECT
                CASE WHEN OBJECT_ID(N'dbo.otel_liste_abonelikleri', N'U') IS NULL THEN 0 ELSE 1 END AS exists_flag,
                CASE WHEN OBJECT_ID(N'dbo.otel_liste_abonelikleri', N'U') IS NULL THEN 0 ELSE (SELECT COUNT(*) FROM otel_liste_abonelikleri) END AS row_count;";
        await using (var listingSubCmd = new SqlCommand(listingSubSql, connection))
        await using (var listingSubReader = await listingSubCmd.ExecuteReaderAsync(cancellationToken))
        {
            if (await listingSubReader.ReadAsync(cancellationToken))
            {
                var exists = SafeInt(listingSubReader, 0) == 1;
                var count = SafeInt(listingSubReader, 1);
                model.Checks.Add(new AdminSystemHealthCheckItemViewModel
                {
                    Title = "Otel Liste Abonelikleri Tablosu",
                    StatusText = exists ? "VAR" : "YOK",
                    ToneClass = exists ? "success" : "danger",
                    Detail = exists ? $"otel_liste_abonelikleri satır: {count:N0}" : "Migration henüz DB'ye uygulanmamış."
                });
            }
        }

        // Panel design tokens asset
        try
        {
            var physical = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "assets", "css", "paneller", "panel-tokens.css");
            var exists = File.Exists(physical);
            model.Checks.Add(new AdminSystemHealthCheckItemViewModel
            {
                Title = "Panel Design Tokens",
                StatusText = exists ? "OK" : "YOK",
                ToneClass = exists ? "success" : "danger",
                Detail = exists ? "panel-tokens.css bulundu ve tüm panel layout'larına bağlandı." : "wwwroot/assets/css/paneller/panel-tokens.css bulunamadı."
            });
        }
        catch (Exception ex)
        {
            model.Checks.Add(new AdminSystemHealthCheckItemViewModel
            {
                Title = "Panel Design Tokens",
                StatusText = "HATA",
                ToneClass = "danger",
                Detail = ex.Message
            });
        }

        return model;
    }

    public async Task<AdminPartnerApplicationsPageViewModel> GetPartnerApplicationsAsync(string fullName, string email, string userRole, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var model = new AdminPartnerApplicationsPageViewModel
        {
            Shell = await GetShellAsync(connection, "Partner Basvurulari", "Partner onboarding, e-posta dogrulama ve admin onay akislarini yonetin.", fullName, email, userRole, cancellationToken)
        };

        var summaryDefinitions = GetSummaryDefinitions("partner-applications").ToList();
        model.SummaryCards.AddRange(summaryDefinitions.Select(static item => new AdminSummaryCardViewModel
        {
            Label = item.Label,
            Description = item.Description,
            ToneClass = item.ToneClass,
            IconClass = item.IconClass
        }));

        for (var i = 0; i < model.SummaryCards.Count; i++)
        {
            await using var command = new SqlCommand(summaryDefinitions[i].Sql, connection);
            var raw = await command.ExecuteScalarAsync(cancellationToken);
            model.SummaryCards[i].Value = FormatScalar(raw);
        }

        const string sql = @"
            SELECT p.id, p.kullanici_id, o.id AS hotel_id, p.firma_unvani, COALESCE(o.otel_adi, p.firma_unvani),
                   p.yetkili_ad_soyad, p.yetkili_eposta, p.vergi_numarasi, p.onay_durumu, p.olusturulma_tarihi,
                   p.onay_tarihi, u.email_dogrulama_tarihi,
                   (SELECT COUNT(*) FROM partner_basvuru_evraklari ped WHERE ped.partner_id = p.id) AS document_count,
                   COALESCE(p.red_nedeni, '')
            FROM partner_detaylari p
            INNER JOIN users u ON u.id = p.kullanici_id
            LEFT JOIN oteller o ON o.partner_id = p.id
            ORDER BY
                CASE p.onay_durumu
                    WHEN 'Beklemede' THEN 0
                    WHEN 'Reddedildi' THEN 1
                    WHEN 'Askida' THEN 2
                    ELSE 3
                END,
                p.olusturulma_tarihi DESC;";

        await using var listCommand = new SqlCommand(sql, connection);
        await using var reader = await listCommand.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var status = reader.GetString(8);
            model.Applications.Add(new AdminPartnerApplicationRowViewModel
            {
                PartnerId = reader.GetInt64(0),
                UserId = reader.GetInt64(1),
                HotelId = reader.IsDBNull(2) ? null : reader.GetInt64(2),
                CompanyName = reader.GetString(3),
                HotelName = reader.GetString(4),
                ContactName = reader.GetString(5),
                Email = reader.GetString(6),
                TaxNumber = reader.GetString(7),
                StatusText = status,
                StatusToneClass = status switch
                {
                    "Onaylandi" => "success",
                    "Reddedildi" => "danger",
                    "Askida" => "warning",
                    _ => "info"
                },
                RegistrationDateText = reader.GetDateTime(9).ToString("dd.MM.yyyy HH:mm", CultureInfo.GetCultureInfo("tr-TR")),
                ApprovalDateText = reader.IsDBNull(10) ? null : reader.GetDateTime(10).ToString("dd.MM.yyyy HH:mm", CultureInfo.GetCultureInfo("tr-TR")),
                EmailVerified = !reader.IsDBNull(11),
                DocumentCount = SafeInt(reader, 12),
                ReviewNote = reader.IsDBNull(13) ? null : reader.GetString(13)
            });
        }

        return model;
    }

    public async Task<AdminCompanyApplicationsPageViewModel> GetCompanyApplicationsAsync(string fullName, string email, string userRole, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var model = new AdminCompanyApplicationsPageViewModel
        {
            Shell = await GetShellAsync(connection, "Firma Başvuruları", "Firma onboarding ve kurumsal hesap onay sürecini yönetin.", fullName, email, userRole, cancellationToken)
        };

        var summary = GetSummaryDefinitions("company-applications").ToList();
        model.SummaryCards.AddRange(summary.Select(static item => new AdminSummaryCardViewModel
        {
            Label = item.Label,
            Description = item.Description,
            ToneClass = item.ToneClass,
            IconClass = item.IconClass
        }));
        for (var i = 0; i < model.SummaryCards.Count; i++)
        {
            await using var cmd = new SqlCommand(summary[i].Sql, connection);
            model.SummaryCards[i].Value = FormatScalar(await cmd.ExecuteScalarAsync(cancellationToken));
        }

        const string sql = @"
            SELECT TOP (200)
                f.id,
                f.firma_adi,
                COALESCE(f.vergi_no,''),
                COALESCE(f.yetkili_ad_soyad,''),
                COALESCE(f.yetkili_eposta, f.firma_eposta, ''),
                COALESCE(f.yetkili_telefon, f.firma_telefon, ''),
                COALESCE(f.onay_durumu,'Beklemede'),
                f.olusturulma_tarihi
            FROM firmalar f
            ORDER BY
                CASE COALESCE(f.onay_durumu,'Beklemede')
                    WHEN 'Beklemede' THEN 0
                    WHEN 'Askıda' THEN 1
                    WHEN 'Reddedildi' THEN 2
                    ELSE 3
                END,
                f.olusturulma_tarihi DESC;";

        await using var listCmd = new SqlCommand(sql, connection);
        await using var reader = await listCmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var status = reader.GetString(6);
            model.Applications.Add(new AdminCompanyApplicationRowViewModel
            {
                CompanyId = reader.GetInt64(0),
                CompanyName = reader.GetString(1),
                TaxNo = reader.GetString(2),
                ContactName = reader.GetString(3),
                ContactEmail = reader.GetString(4),
                ContactPhone = reader.GetString(5),
                StatusText = status,
                StatusToneClass = status switch
                {
                    "Onaylandı" => "success",
                    "Reddedildi" => "danger",
                    "Askıda" => "warning",
                    _ => "info"
                },
                CreatedAtText = reader.GetDateTime(7).ToString("dd.MM.yyyy HH:mm", CultureInfo.GetCultureInfo("tr-TR"))
            });
        }

        return model;
    }

    public async Task<(bool Success, string Message)> ReviewCompanyApplicationAsync(long adminUserId, AdminCompanyApplicationDecisionRequest request, CancellationToken cancellationToken = default)
    {
        if (request.CompanyId <= 0)
        {
            return (false, "Firma seçilmelidir.");
        }
        if (string.IsNullOrWhiteSpace(request.TargetStatus))
        {
            return (false, "Hedef durum seçilmelidir.");
        }

        var target = request.TargetStatus.Trim();
        var allowed = target is "Beklemede" or "Onaylandı" or "Reddedildi" or "Askıda";
        if (!allowed)
        {
            return (false, "Geçersiz durum.");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var tx = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            const string prevSql = "SELECT TOP (1) COALESCE(onay_durumu,'Beklemede'), COALESCE(firma_adi,'') FROM firmalar WHERE id = @id;";
            string previous;
            string companyName;
            await using (var prevCmd = new SqlCommand(prevSql, connection, (SqlTransaction)tx))
            {
                prevCmd.Parameters.AddWithValue("@id", request.CompanyId);
                await using var r = await prevCmd.ExecuteReaderAsync(cancellationToken);
                if (!await r.ReadAsync(cancellationToken))
                {
                    return (false, "Firma bulunamadı.");
                }
                previous = r.GetString(0);
                companyName = r.GetString(1);
            }

            const string updateSql = @"
                UPDATE firmalar
                SET onay_durumu = @status,
                    guncellenme_tarihi = SYSUTCDATETIME()
                WHERE id = @id;";
            await using (var updateCmd = new SqlCommand(updateSql, connection, (SqlTransaction)tx))
            {
                updateCmd.Parameters.AddWithValue("@status", target);
                updateCmd.Parameters.AddWithValue("@id", request.CompanyId);
                await updateCmd.ExecuteNonQueryAsync(cancellationToken);
            }

            // Firma başvuru hareketi (tablo varsa)
            const string existsSql = "SELECT CASE WHEN OBJECT_ID(N'dbo.firma_basvuru_hareketleri', N'U') IS NULL THEN 0 ELSE 1 END;";
            var exists = false;
            await using (var existsCmd = new SqlCommand(existsSql, connection, (SqlTransaction)tx))
            {
                exists = Convert.ToInt32(await existsCmd.ExecuteScalarAsync(cancellationToken) ?? 0, CultureInfo.InvariantCulture) == 1;
            }

            if (exists)
            {
                const string insertSql = @"
                    INSERT INTO firma_basvuru_hareketleri
                    (firma_id, onceki_durum, yeni_durum, hareket_tipi, aciklama, islem_yapan_kullanici_id, islem_kaynagi, ip_adresi, olusturulma_tarihi)
                    VALUES
                    (@firmaId, @prev, @next, @type, @desc, @adminId, 'admin', NULL, SYSUTCDATETIME());";
                await using var ins = new SqlCommand(insertSql, connection, (SqlTransaction)tx);
                ins.Parameters.AddWithValue("@firmaId", request.CompanyId);
                ins.Parameters.AddWithValue("@prev", previous);
                ins.Parameters.AddWithValue("@next", target);
                ins.Parameters.AddWithValue("@type", target == "Onaylandı" ? "Onaylandi" : target == "Reddedildi" ? "Reddedildi" : target == "Askıda" ? "Askida" : "Incelemeye Alindi");
                ins.Parameters.AddWithValue("@desc", string.IsNullOrWhiteSpace(request.Note) ? "Admin firma başvurusunu güncelledi." : request.Note.Trim());
                ins.Parameters.AddWithValue("@adminId", adminUserId);
                await ins.ExecuteNonQueryAsync(cancellationToken);
            }

            await tx.CommitAsync(cancellationToken);
            return (true, $"{companyName} durumu güncellendi: {previous} → {target}");
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(cancellationToken);
            return (false, $"Firma güncellenemedi: {ex.Message}");
        }
    }

    public async Task<(bool Success, string Message)> ReviewPartnerApplicationAsync(long adminUserId, AdminPartnerApplicationDecisionRequest request, CancellationToken cancellationToken = default)
    {
        if (request.PartnerId <= 0)
        {
            return (false, "Guncellenecek partner basvurusu bulunamadi.");
        }

        var targetStatus = request.TargetStatus switch
        {
            "Onaylandi" => "Onaylandi",
            "Reddedildi" => "Reddedildi",
            "Askida" => "Askida",
            _ => string.Empty
        };

        if (string.IsNullOrWhiteSpace(targetStatus))
        {
            return (false, "Gecersiz partner basvuru durumu secildi.");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            const string readSql = """
                SELECT TOP (1) kullanici_id, onay_durumu
                FROM partner_detaylari
                WHERE id = @partnerId;
                """;

            long userId;
            string currentStatus;
            await using (var readCommand = new SqlCommand(readSql, connection, (SqlTransaction)transaction))
            {
                readCommand.Parameters.AddWithValue("@partnerId", request.PartnerId);
                await using var reader = await readCommand.ExecuteReaderAsync(cancellationToken);
                if (!await reader.ReadAsync(cancellationToken))
                {
                    return (false, "Partner basvurusu bulunamadi.");
                }

                userId = reader.GetInt64(0);
                currentStatus = reader.GetString(1);
            }

            const string updateSql = @"
                UPDATE partner_detaylari
                SET onay_durumu = @targetStatus,
                    onay_tarihi = CASE WHEN @targetStatus = 'Onaylandi' THEN SYSUTCDATETIME() ELSE onay_tarihi END,
                    onaylayan_admin_id = @adminUserId,
                    red_nedeni = @note,
                    guncellenme_tarihi = SYSUTCDATETIME()
                WHERE id = @partnerId;";

            await using (var updateCommand = new SqlCommand(updateSql, connection, (SqlTransaction)transaction))
            {
                updateCommand.Parameters.AddWithValue("@targetStatus", targetStatus);
                updateCommand.Parameters.AddWithValue("@adminUserId", adminUserId);
                updateCommand.Parameters.AddWithValue("@note", string.IsNullOrWhiteSpace(request.Note) ? DBNull.Value : request.Note.Trim());
                updateCommand.Parameters.AddWithValue("@partnerId", request.PartnerId);
                await updateCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            const string hotelUpdateSql = @"
                UPDATE oteller
                SET onay_durumu = CASE
                        WHEN @targetStatus = 'Onaylandi' THEN 'Onaylandı'
                        WHEN @targetStatus = 'Reddedildi' THEN 'Reddedildi'
                        ELSE 'Beklemede'
                    END,
                    yayin_durumu = CASE
                        WHEN @targetStatus = 'Askida' THEN 'Askıda'
                        WHEN @targetStatus = 'Reddedildi' THEN 'Taslak'
                        ELSE yayin_durumu
                    END,
                    onay_tarihi = CASE WHEN @targetStatus = 'Onaylandi' THEN SYSUTCDATETIME() ELSE onay_tarihi END
                WHERE partner_id = @partnerId;";

            await using (var hotelUpdateCommand = new SqlCommand(hotelUpdateSql, connection, (SqlTransaction)transaction))
            {
                hotelUpdateCommand.Parameters.AddWithValue("@targetStatus", targetStatus);
                hotelUpdateCommand.Parameters.AddWithValue("@partnerId", request.PartnerId);
                await hotelUpdateCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            if (await TableExistsAsync(connection, "partner_basvuru_hareketleri", cancellationToken, (SqlTransaction?)transaction))
            {
                const string historySql = @"
                    INSERT INTO partner_basvuru_hareketleri
                    (partner_id, onceki_durum, yeni_durum, islem_tipi, aciklama, islem_yapan_kullanici_id, olusturulma_tarihi)
                    VALUES
                    (@partnerId, @currentStatus, @targetStatus, 'AdminPartnerBasvuruKarari', @note, @adminUserId, SYSUTCDATETIME());";

                await using var historyCommand = new SqlCommand(historySql, connection, (SqlTransaction)transaction);
                historyCommand.Parameters.AddWithValue("@partnerId", request.PartnerId);
                historyCommand.Parameters.AddWithValue("@currentStatus", currentStatus);
                historyCommand.Parameters.AddWithValue("@targetStatus", targetStatus);
                historyCommand.Parameters.AddWithValue("@note", string.IsNullOrWhiteSpace(request.Note) ? "Admin partner basvurusunu guncelledi." : request.Note.Trim());
                historyCommand.Parameters.AddWithValue("@adminUserId", adminUserId);
                await historyCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            const string userSql = """
                UPDATE users
                SET hesap_durumu = CASE WHEN @targetStatus = 'Kara Liste' THEN 0 ELSE hesap_durumu END
                WHERE id = @userId;
                """;

            await using (var userCommand = new SqlCommand(userSql, connection, (SqlTransaction)transaction))
            {
                userCommand.Parameters.AddWithValue("@targetStatus", targetStatus);
                userCommand.Parameters.AddWithValue("@userId", userId);
                await userCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
            return (true, targetStatus switch
            {
                "Onaylandi" => "Partner basvurusu onaylandi. Partner artik yayin oncesi son icerik adimlarini tamamlayabilir.",
                "Reddedildi" => "Partner basvurusu reddedildi.",
                "Askida" => "Partner basvurusu askiya alindi.",
                _ => "Partner basvurusu guncellendi."
            });
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<AdminListingSubscriptionsPageViewModel> GetListingSubscriptionsAsync(string fullName, string email, string userRole, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var model = new AdminListingSubscriptionsPageViewModel
        {
            Shell = await GetShellAsync(connection, "Otel Liste Abonelikleri", "İl/ilçe/mahalle bazlı 1-2-3 sabit çıkma abonelik taleplerini yönetin.", fullName, email, userRole, cancellationToken)
        };

        // Summary
        var hasTable = await TableExistsAsync(connection, "otel_liste_abonelikleri", cancellationToken);
        if (!hasTable)
        {
            model.SummaryCards.Add(new AdminSummaryCardViewModel { Label = "Abonelik Tablosu", Value = "YOK", Description = "Migration uygulanmamış.", ToneClass = "danger", IconClass = "fa-triangle-exclamation" });
            return model;
        }

        var summary = new (string Label, string Sql, string Tone, string Icon, string Desc)[]
        {
            ("Toplam", "SELECT COUNT(*) FROM otel_liste_abonelikleri", "info", "fa-crown", "Tüm talepler"),
            ("Bekleyen", "SELECT COUNT(*) FROM otel_liste_abonelikleri WHERE durum = N'Beklemede'", "warning", "fa-hourglass-half", "Admin onayı bekliyor"),
            ("Aktif", "SELECT COUNT(*) FROM otel_liste_abonelikleri WHERE durum = N'Onaylandı' AND SYSUTCDATETIME() BETWEEN baslangic_utc AND bitis_utc", "success", "fa-circle-check", "Şu anda pin uygulanıyor"),
            ("Süresi Dolan", "SELECT COUNT(*) FROM otel_liste_abonelikleri WHERE durum = N'Onaylandı' AND bitis_utc < SYSUTCDATETIME()", "secondary", "fa-clock", "Bitişi geçenler")
        };
        foreach (var item in summary)
        {
            await using var cmd = new SqlCommand(item.Sql, connection);
            var raw = await cmd.ExecuteScalarAsync(cancellationToken);
            model.SummaryCards.Add(new AdminSummaryCardViewModel { Label = item.Label, Value = FormatScalar(raw), Description = item.Desc, ToneClass = item.Tone, IconClass = item.Icon });
        }

        const string sql = @"
            SELECT TOP (200)
                a.id,
                a.otel_id,
                COALESCE(o.otel_adi,'-') AS otel_adi,
                CONCAT(COALESCE(o.ilce,''), ', ', COALESCE(o.sehir,'')) AS city_text,
                COALESCE(a.kapsam_tipi,'-'),
                COALESCE(a.kapsam_degeri,'-'),
                COALESCE(a.hedef_sira,0),
                COALESCE(a.durum,'-'),
                a.baslangic_utc,
                a.bitis_utc,
                COALESCE(u.eposta,'') AS partner_email,
                COALESCE(a.partner_notu,'')
            FROM otel_liste_abonelikleri a
            LEFT JOIN oteller o ON o.id = a.otel_id
            LEFT JOIN users u ON u.id = a.talep_eden_user_id
            ORDER BY a.olusturulma_tarihi DESC, a.id DESC;";

        await using var listCmd = new SqlCommand(sql, connection);
        await using var reader = await listCmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var status = reader.IsDBNull(7) ? "-" : reader.GetString(7);
            var tone = status switch
            {
                "Onaylandı" => "success",
                "Beklemede" => "warning",
                "Reddedildi" => "danger",
                "Askıda" => "secondary",
                _ => "info"
            };

            var start = reader.IsDBNull(8) ? (DateTime?)null : reader.GetDateTime(8);
            var end = reader.IsDBNull(9) ? (DateTime?)null : reader.GetDateTime(9);
            model.Rows.Add(new AdminListingSubscriptionRowViewModel
            {
                SubscriptionId = Convert.ToInt64(reader.GetValue(0), CultureInfo.InvariantCulture),
                HotelId = Convert.ToInt64(reader.GetValue(1), CultureInfo.InvariantCulture),
                HotelName = reader.IsDBNull(2) ? "-" : reader.GetString(2),
                CityText = reader.IsDBNull(3) ? "-" : reader.GetString(3),
                ScopeType = reader.IsDBNull(4) ? "-" : reader.GetString(4),
                ScopeValue = reader.IsDBNull(5) ? "-" : reader.GetString(5),
                Rank = reader.IsDBNull(6) ? 0 : Convert.ToInt32(reader.GetValue(6), CultureInfo.InvariantCulture),
                StatusText = status,
                StatusToneClass = tone,
                StartText = start.HasValue ? start.Value.ToString("dd.MM.yyyy", CultureInfo.GetCultureInfo("tr-TR")) : "-",
                EndText = end.HasValue ? end.Value.ToString("dd.MM.yyyy", CultureInfo.GetCultureInfo("tr-TR")) : "-",
                PartnerEmail = reader.IsDBNull(10) ? string.Empty : reader.GetString(10),
                PartnerNote = reader.IsDBNull(11) ? string.Empty : reader.GetString(11)
            });
        }

        return model;
    }

    public async Task<(bool Success, string Message)> ReviewListingSubscriptionAsync(long adminUserId, AdminListingSubscriptionDecisionRequest request, CancellationToken cancellationToken = default)
    {
        if (request.SubscriptionId <= 0)
        {
            return (false, "Abonelik kaydı bulunamadı.");
        }

        var action = (request.Action ?? string.Empty).Trim().ToLowerInvariant();
        var targetStatus = action switch
        {
            "approve" => "Onaylandı",
            "reject" => "Reddedildi",
            "suspend" => "Askıda",
            "cancel" => "İptal",
            _ => string.Empty
        };
        if (string.IsNullOrWhiteSpace(targetStatus))
        {
            return (false, "Geçersiz işlem.");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        if (!await TableExistsAsync(connection, "otel_liste_abonelikleri", cancellationToken))
        {
            return (false, "Abonelik tablosu bulunamadı.");
        }

        await using var tx = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            const string readSql = @"
                SELECT TOP (1)
                    otel_id, kapsam_tipi, kapsam_degeri_normalized, hedef_sira, baslangic_utc, bitis_utc, durum
                FROM otel_liste_abonelikleri
                WHERE id = @id;";
            long hotelId;
            string scopeType;
            string scopeNorm;
            int rank;
            DateTime startUtc;
            DateTime endUtc;
            string currentStatus;
            await using (var read = new SqlCommand(readSql, connection, (SqlTransaction)tx))
            {
                read.Parameters.AddWithValue("@id", request.SubscriptionId);
                await using var r = await read.ExecuteReaderAsync(cancellationToken);
                if (!await r.ReadAsync(cancellationToken))
                {
                    return (false, "Abonelik kaydı bulunamadı.");
                }
                hotelId = Convert.ToInt64(r.GetValue(0), CultureInfo.InvariantCulture);
                scopeType = r.IsDBNull(1) ? string.Empty : r.GetString(1);
                scopeNorm = r.IsDBNull(2) ? string.Empty : r.GetString(2);
                rank = r.IsDBNull(3) ? 0 : Convert.ToInt32(r.GetValue(3), CultureInfo.InvariantCulture);
                startUtc = r.GetDateTime(4);
                endUtc = r.GetDateTime(5);
                currentStatus = r.IsDBNull(6) ? "-" : r.GetString(6);
            }

            if (targetStatus == "Onaylandı")
            {
                const string conflictSql = @"
                    SELECT COUNT(*)
                    FROM otel_liste_abonelikleri
                    WHERE id <> @id
                      AND durum = N'Onaylandı'
                      AND kapsam_tipi = @scopeType
                      AND kapsam_degeri_normalized = @scopeNorm
                      AND hedef_sira = @rank
                      AND (
                          (@startUtc < bitis_utc) AND (@endUtc > baslangic_utc)
                      );";
                await using var conflict = new SqlCommand(conflictSql, connection, (SqlTransaction)tx);
                conflict.Parameters.AddWithValue("@id", request.SubscriptionId);
                conflict.Parameters.AddWithValue("@scopeType", scopeType);
                conflict.Parameters.AddWithValue("@scopeNorm", scopeNorm);
                conflict.Parameters.AddWithValue("@rank", rank);
                conflict.Parameters.AddWithValue("@startUtc", startUtc);
                conflict.Parameters.AddWithValue("@endUtc", endUtc);
                var conflicts = Convert.ToInt32(await conflict.ExecuteScalarAsync(cancellationToken) ?? 0, CultureInfo.InvariantCulture);
                if (conflicts > 0)
                {
                    return (false, "Bu kapsam ve sıra için aynı tarihlerde aktif bir abonelik zaten var.");
                }
            }

            const string updateSql = @"
                UPDATE otel_liste_abonelikleri
                SET durum = @status,
                    onaylayan_admin_user_id = @adminId,
                    admin_notu = @note,
                    onay_tarihi = CASE WHEN @status = N'Onaylandı' THEN SYSUTCDATETIME() ELSE onay_tarihi END
                WHERE id = @id;";
            await using (var upd = new SqlCommand(updateSql, connection, (SqlTransaction)tx))
            {
                upd.Parameters.AddWithValue("@status", targetStatus);
                upd.Parameters.AddWithValue("@adminId", adminUserId);
                upd.Parameters.AddWithValue("@note", string.IsNullOrWhiteSpace(request.AdminNote) ? (object)DBNull.Value : request.AdminNote.Trim());
                upd.Parameters.AddWithValue("@id", request.SubscriptionId);
                await upd.ExecuteNonQueryAsync(cancellationToken);
            }

            await tx.CommitAsync(cancellationToken);
            return (true, $"Abonelik güncellendi: {currentStatus} → {targetStatus}");
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(cancellationToken);
            return (false, "Abonelik güncellenemedi: " + ex.Message);
        }
    }

    public async Task<AdminCommissionManagementPageViewModel> GetCommissionManagementAsync(string fullName, string email, string userRole, long? hotelId = null, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var model = new AdminCommissionManagementPageViewModel
        {
            Shell = await GetShellAsync(connection, "Komisyon ve Vergi Ayarlari", "Otel bazli komisyon, KDV ve konaklama vergisi kurallarini tarih bazli yonetin.", fullName, email, userRole, cancellationToken)
        };

        const string hotelsSql = @"
            SELECT o.id, o.otel_adi, o.otel_kodu, CONCAT(o.ilce, ', ', o.sehir) AS sehir_label
            FROM oteller o
            ORDER BY o.otel_adi ASC;";

        await using (var hotelCommand = new SqlCommand(hotelsSql, connection))
        await using (var hotelReader = await hotelCommand.ExecuteReaderAsync(cancellationToken))
        {
            while (await hotelReader.ReadAsync(cancellationToken))
            {
                model.Hotels.Add(new AdminCommissionHotelOptionViewModel
                {
                    HotelId = hotelReader.GetInt64(0),
                    HotelName = hotelReader.GetString(1),
                    HotelCode = hotelReader.IsDBNull(2) ? string.Empty : hotelReader.GetString(2),
                    CityLabel = hotelReader.IsDBNull(3) ? string.Empty : hotelReader.GetString(3),
                    IsSelected = hotelId.HasValue && hotelId.Value == hotelReader.GetInt64(0)
                });
            }
        }

        model.Form.HotelId = hotelId ?? model.Hotels.FirstOrDefault()?.HotelId ?? 0;

        const string summarySql = @"
            SELECT
                (SELECT COUNT(*) FROM komisyon_vergiler) AS total_rule_count,
                (SELECT COUNT(DISTINCT otel_id) FROM komisyon_vergiler WHERE aktif_mi = 1) AS active_hotel_count,
                (SELECT COALESCE(AVG(komisyon_orani), 0) FROM komisyon_vergiler WHERE aktif_mi = 1) AS avg_commission_rate,
                (
                    SELECT COALESCE(SUM(COALESCE(kdv_orani, 0) + COALESCE(konaklama_vergisi_orani, 0)), 0)
                    FROM komisyon_vergiler
                    WHERE aktif_mi = 1
                ) AS total_tax_rate_sum;";

        await using (var summaryCommand = new SqlCommand(summarySql, connection))
        await using (var summaryReader = await summaryCommand.ExecuteReaderAsync(cancellationToken))
        {
            if (await summaryReader.ReadAsync(cancellationToken))
            {
                model.SummaryCards.Add(new AdminSummaryCardViewModel { Label = "Toplam Kural", Value = SafeInt(summaryReader, 0).ToString(), Description = "Tarih bazli komisyon ve vergi setleri", ToneClass = "info", IconClass = "fa-layer-group" });
                model.SummaryCards.Add(new AdminSummaryCardViewModel { Label = "Aktif Otel", Value = SafeInt(summaryReader, 1).ToString(), Description = "En az bir aktif kural tanimli oteller", ToneClass = "success", IconClass = "fa-hotel" });
                model.SummaryCards.Add(new AdminSummaryCardViewModel { Label = "Ort. Komisyon", Value = $"{SafeDecimal(summaryReader, 2):0.##}%", Description = "Aktif kurallarin ortalama komisyon orani", ToneClass = "warning", IconClass = "fa-percent" });
                model.SummaryCards.Add(new AdminSummaryCardViewModel { Label = "Toplam Vergi Oranlari", Value = $"{SafeDecimal(summaryReader, 3):0.##}%", Description = "KDV + konaklama vergisi toplamlari", ToneClass = "danger", IconClass = "fa-receipt" });
            }
        }

        const string rulesSql = @"
            SELECT TOP (100)
                kv.id,
                kv.otel_id,
                o.otel_adi,
                o.otel_kodu,
                CONCAT(o.ilce, ', ', o.sehir) AS sehir_label,
                kv.baslangic_tarihi,
                kv.bitis_tarihi,
                kv.komisyon_orani,
                kv.komisyon_gelir_vergisi_orani,
                kv.kdv_orani,
                kv.konaklama_vergisi_orani,
                kv.aktif_mi,
                kv.aciklama
            FROM komisyon_vergiler kv
            INNER JOIN oteller o ON o.id = kv.otel_id
            WHERE (@hotelId IS NULL OR kv.otel_id = @hotelId)
            ORDER BY kv.otel_id ASC, kv.baslangic_tarihi DESC, kv.id DESC;";

        await using (var rulesCommand = new SqlCommand(rulesSql, connection))
        {
            rulesCommand.Parameters.AddWithValue("@hotelId", hotelId.HasValue ? hotelId.Value : DBNull.Value);
            await using var reader = await rulesCommand.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var commissionRate = SafeDecimal(reader, 7);
                var commissionIncomeTaxRate = SafeDecimal(reader, 8);
                var vatRate = SafeDecimal(reader, 9);
                var accommodationTaxRate = SafeDecimal(reader, 10);
                var grossCommissionAmount = Math.Round(3500m * commissionRate / 100m, 2, MidpointRounding.AwayFromZero);
                var netCommissionAmount = grossCommissionAmount - Math.Round(grossCommissionAmount * commissionIncomeTaxRate / 100m, 2, MidpointRounding.AwayFromZero);

                model.Rules.Add(new AdminCommissionRuleRowViewModel
                {
                    RuleId = reader.GetInt64(0),
                    HotelId = reader.GetInt64(1),
                    HotelName = reader.GetString(2),
                    HotelCode = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                    CityLabel = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                    DateRangeText = reader.IsDBNull(6)
                        ? $"{reader.GetDateTime(5):dd.MM.yyyy} - Acik Uclu"
                        : $"{reader.GetDateTime(5):dd.MM.yyyy} - {reader.GetDateTime(6):dd.MM.yyyy}",
                    CommissionText = $"%{commissionRate:0.##} komisyon / %{commissionIncomeTaxRate:0.##} gelir vergisi",
                    TaxText = $"KDV %{vatRate:0.##} + Konaklama %{accommodationTaxRate:0.##}",
                    NetText = $"{grossCommissionAmount:0.##} brüt / {netCommissionAmount:0.##} net",
                    IsActive = SafeBool(reader, 11),
                    Note = reader.IsDBNull(12) ? null : reader.GetString(12)
                });
            }
        }

        if (hotelId.HasValue)
        {
            var selectedRule = model.Rules.FirstOrDefault(static item => item.IsActive);
            if (selectedRule is not null)
            {
                model.Form.HotelId = selectedRule.HotelId;
            }
        }

        const string financeSql = @"
            SELECT TOP (20)
                o.id,
                o.otel_adi,
                COALESCE(reservationStats.gross_revenue, 0) AS gross_revenue,
                COALESCE(commissionStats.total_commission, 0) AS total_commission,
                COALESCE(commissionStats.paid_commission, 0) AS paid_commission
            FROM oteller o
            OUTER APPLY
            (
                SELECT SUM(COALESCE(r.toplam_tutar, 0)) AS gross_revenue
                FROM rezervasyonlar r
                WHERE r.otel_id = o.id
                  AND COALESCE(r.durum, '') <> 'İptal Edildi'
            ) reservationStats
            OUTER APPLY
            (
                SELECT
                    SUM(COALESCE(k.komisyon_tutari, 0)) AS total_commission,
                    SUM(CASE WHEN COALESCE(k.otele_odeme_durumu, '') = 'Ödendi' THEN COALESCE(k.komisyon_tutari, 0) ELSE 0 END) AS paid_commission
                FROM komisyon_muhasebe_kayitlari k
                WHERE k.otel_id = o.id
            ) commissionStats
            WHERE (@hotelId IS NULL OR o.id = @hotelId)
              AND (COALESCE(reservationStats.gross_revenue, 0) > 0 OR COALESCE(commissionStats.total_commission, 0) > 0)
            ORDER BY COALESCE(reservationStats.gross_revenue, 0) DESC, o.id DESC;";

        await using (var financeCommand = new SqlCommand(financeSql, connection))
        {
            financeCommand.Parameters.AddWithValue("@hotelId", hotelId.HasValue ? hotelId.Value : DBNull.Value);
            await using var financeReader = await financeCommand.ExecuteReaderAsync(cancellationToken);
            while (await financeReader.ReadAsync(cancellationToken))
            {
                model.HotelFinanceRows.Add(new AdminHotelCommissionFinanceRowViewModel
                {
                    HotelId = financeReader.GetInt64(0),
                    HotelName = financeReader.GetString(1),
                    GrossRevenue = SafeDecimal(financeReader, 2),
                    TotalCommission = SafeDecimal(financeReader, 3),
                    PaidCommission = SafeDecimal(financeReader, 4)
                });
            }
        }

        return model;
    }

    public async Task<(bool Success, string Message)> SaveCommissionRuleAsync(long adminUserId, AdminCommissionRuleForm request, CancellationToken cancellationToken = default)
    {
        if (request.HotelId <= 0)
        {
            return (false, "Komisyon tanimi icin otel secilmelidir.");
        }

        if (request.EndDate.HasValue && request.EndDate.Value.Date < request.StartDate.Date)
        {
            return (false, "Bitis tarihi baslangic tarihinden once olamaz.");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        const string sql = @"
            IF EXISTS (SELECT 1 FROM komisyon_vergiler WHERE id = @ruleId)
            BEGIN
                UPDATE komisyon_vergiler
                SET otel_id = @hotelId,
                    baslangic_tarihi = @startDate,
                    bitis_tarihi = @endDate,
                    komisyon_orani = @commissionRate,
                    komisyon_gelir_vergisi_orani = @commissionIncomeTaxRate,
                    kdv_orani = @vatRate,
                    konaklama_vergisi_orani = @accommodationTaxRate,
                    para_birimi = @currency,
                    aktif_mi = 1,
                    aciklama = @note,
                    guncelleyen_kullanici_id = @adminUserId,
                    guncellenme_tarihi = SYSUTCDATETIME()
                WHERE id = @ruleId;
            END
            ELSE
            BEGIN
                INSERT INTO komisyon_vergiler
                (
                    otel_id, baslangic_tarihi, bitis_tarihi, komisyon_orani, komisyon_gelir_vergisi_orani,
                    kdv_orani, konaklama_vergisi_orani, para_birimi, aktif_mi, aciklama, olusturan_kullanici_id, guncelleyen_kullanici_id
                )
                VALUES
                (
                    @hotelId, @startDate, @endDate, @commissionRate, @commissionIncomeTaxRate,
                    @vatRate, @accommodationTaxRate, @currency, 1, @note, @adminUserId, @adminUserId
                );
            END;";

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@ruleId", request.RuleId.HasValue ? request.RuleId.Value : DBNull.Value);
        command.Parameters.AddWithValue("@hotelId", request.HotelId);
        command.Parameters.AddWithValue("@startDate", request.StartDate.Date);
        command.Parameters.AddWithValue("@endDate", request.EndDate.HasValue ? request.EndDate.Value.Date : DBNull.Value);
        command.Parameters.AddWithValue("@commissionRate", request.CommissionRate);
        command.Parameters.AddWithValue("@commissionIncomeTaxRate", request.CommissionIncomeTaxRate);
        command.Parameters.AddWithValue("@vatRate", request.VatRate);
        command.Parameters.AddWithValue("@accommodationTaxRate", request.AccommodationTaxRate);
        command.Parameters.AddWithValue("@currency", string.IsNullOrWhiteSpace(request.Currency) ? "TRY" : request.Currency.Trim().ToUpperInvariant());
        command.Parameters.AddWithValue("@note", string.IsNullOrWhiteSpace(request.Note) ? DBNull.Value : request.Note.Trim());
        command.Parameters.AddWithValue("@adminUserId", adminUserId);
        await command.ExecuteNonQueryAsync(cancellationToken);

        return (true, "Komisyon ve vergi kurali kaydedildi.");
    }

    private async Task<AdminShellViewModel> GetShellAsync(SqlConnection connection, string title, string subtitle, string fullName, string email, string userRole, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT
                (SELECT COUNT(*) FROM partner_detaylari WHERE onay_durumu = 'Beklemede') AS pending_partner_applications,
                (SELECT COUNT(*) FROM firmalar WHERE COALESCE(onay_durumu, 'Beklemede') = 'Beklemede') AS pending_company_applications,
                (SELECT COUNT(*) FROM sistem_ici_bildirimler WHERE okundu_mu = 0) AS unread_notifications,
                (SELECT COUNT(*) FROM sistem_hata_loglari WHERE hata_seviyesi IN ('CRITICAL','ALERT','EMERGENCY') AND cozuldu_mu = 0) AS critical_logs,
                (SELECT COUNT(*) FROM yorumlar WHERE onay_durumu = 'Beklemede') AS pending_reviews;";

        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var shell = new AdminShellViewModel { FullName = fullName, Email = email, UserRole = userRole, PanelTitle = title, PanelSubtitle = subtitle };
        if (await reader.ReadAsync(cancellationToken))
        {
            shell.PendingPartnerApplications = SafeInt(reader, 0);
            shell.PendingCompanyApplications = SafeInt(reader, 1);
            shell.UnreadNotifications = SafeInt(reader, 2);
            shell.CriticalLogs = SafeInt(reader, 3);
            shell.PendingReviews = SafeInt(reader, 4);
        }

        return shell;
    }

    private static async Task FillSummaryCardsAsync(SqlConnection connection, AdminSectionPageViewModel model, string sectionKey, CancellationToken cancellationToken)
    {
        var cards = GetSummaryDefinitions(sectionKey);
        foreach (var card in cards)
        {
            await using var command = new SqlCommand(card.Sql, connection);
            var rawValue = await command.ExecuteScalarAsync(cancellationToken);
            model.SummaryCards.Add(new AdminSummaryCardViewModel
            {
                Label = card.Label,
                Value = FormatScalar(rawValue),
                Description = card.Description,
                ToneClass = card.ToneClass,
                IconClass = card.IconClass
            });
        }
    }

    private static async Task FillTableAsync(SqlConnection connection, AdminSectionPageViewModel model, string sectionKey, CancellationToken cancellationToken)
    {
        var sql = GetTableSql(sectionKey);
        if (string.IsNullOrWhiteSpace(sql))
        {
            return;
        }

        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var row = new List<string>();
            for (var i = 0; i < reader.FieldCount; i++)
            {
                row.Add(reader.IsDBNull(i) ? "-" : reader.GetValue(i)?.ToString() ?? "-");
            }

            model.Rows.Add(row);
        }
    }

    private static (string Title, string Subtitle, string[] Columns, string EmptyMessage, string? InfoNote) GetSectionConfig(string sectionKey)
    {
        return sectionKey switch
        {
            "users" => ("Kullanicilar", "Tum kullanici tiplerini, rollerini ve hesap durumlarini veritabani kayitlari ile yonetin.", new[] { "Kullanici", "E-posta", "Telefon", "Uyelik", "Rezervasyon", "Puan", "Durum", "Islem" }, "Kullanici kaydi bulunamadi.", null),
            "managers" => ("Yoneticiler", "Admin ve ekip kullanicilarini departman ve rol dagilimi ile izleyin.", new[] { "Ad Soyad", "E-posta", "Departman", "Rol", "Son Giris" }, "Yonetici kaydi bulunamadi.", null),
            "hotels" => ("Oteller", "Otel, yayin ve onay durumlarini tek ekranda izleyin.", new[] { "Otel", "Konum", "Tur", "Yayin", "Onay", "Puan" }, "Otel kaydi bulunamadi.", null),
            "hotel-detail" => ("Otel Detay", "Referans admin otel detay ekranini, secili otelin tum panel verileri ile baglayacagiz.", Array.Empty<string>(), "Detay ekrani icin otel secimi gerekiyor.", "Bu ekran sonraki adimda secili otel bazli detay verilerle ayri servisle baglanacak."),
            "reservations" => ("Rezervasyonlar", "Rezervasyon hareketlerini dogrudan yerel veritabanindan izleyin.", new[] { "Rez. No", "Misafir", "Giris", "Cikis", "Durum", "Tutar" }, "Rezervasyon bulunamadi.", null),
            "payments" => ("Odemeler", "Tahsilat, iade ve risk durumlarini odeme tablosu uzerinden yonetin.", new[] { "Islem No", "Tur", "Durum", "Yontem", "Tahsilat", "Tarih" }, "Odeme kaydi bulunamadi.", null),
            "invoices" => ("Faturalar", "Platform ve otel faturalarini veritabani kayitlari ile izleyin.", new[] { "Fatura No", "Tarih", "Tur", "Durum", "Toplam", "PB" }, "Fatura kaydi bulunamadi.", null),
            "commissions" => ("Komisyonlar", "Komisyon muhasebe ve mutabakat durumlarini takip edin.", new[] { "Kayit No", "Donem", "Otel", "Komisyon", "Odeme Durumu", "Mutabakat" }, "Komisyon kaydi bulunamadi.", null),
            "company-reservations" => ("Firma Rezervasyonları", "Firma ve personel bazlı rezervasyon kayıtlarını izleyin; otel/şehir/ilçe/mahalle filtreleriyle kontrol edin.", new[] { "Rez. No", "Firma", "Personel", "Otel", "Konum", "Giriş", "Çıkış", "Durum", "Firma Onayı", "Tutar" }, "Firma rezervasyonu bulunamadı.", null),
            "partner-applications" => ("Partner Basvurulari", "Partner onboarding surecini ve onay akisini yonetin.", new[] { "Firma", "Yetkili", "E-posta", "Vergi No", "Durum", "Kayit" }, "Partner basvurusu bulunamadi.", null),
            "company-applications" => ("Firma Basvurulari", "Firma onboarding durumlarini ve rezervasyon onay akislarini izleyin.", new[] { "Firma", "Onay", "Firma Kullanicisi", "Rezervasyon", "Kayit" }, "Firma basvurusu bulunamadi.", null),
            "platform-officials" => ("Platform Yetkilileri", "Admin ve superadmin hesaplarin durumunu ve erisim izlerini yonetin.", new[] { "Ad Soyad", "E-posta", "Rol", "Durum", "Son Giris", "Kayit" }, "Platform yetkilisi bulunamadi.", null),
            "active-hotels" => ("Acik Oteller", "Yayinda ve onayli otelleri operasyonel performans ile izleyin.", new[] { "Otel", "Konum", "Puan", "Rezervasyon", "Gelir", "Guncelleme" }, "Acik otel bulunamadi.", null),
            "pending-hotels" => ("Bekleyen Oteller", "Onay veya yayin bekleyen tesisleri hizli aksiyon listesi olarak yonetin.", new[] { "Otel", "Konum", "Onay", "Yayin", "Olusturma", "Son Guncelleme" }, "Bekleyen otel bulunamadi.", null),
            "reviews" => ("Degerlendirmeler", "Yorum moderasyonu, raporlanan yorumlar ve dogrulanmis konaklama kayitlarini yonetin.", new[] { "Baslik", "Puan", "Durum", "Rapor", "Dogrulama", "Tarih" }, "Yorum kaydi bulunamadi.", null),
            "reports" => ("Raporlar", "Rapor ekranini mevcut operasyon verileri uzerinden kurgulayacagiz.", Array.Empty<string>(), "Rapor veri matrisi bir sonraki fazda kurulur.", "Bu ekran icin rapor snapshot / export altyapisi migration ile eklenecek."),
            "campaigns" => ("Kampanyalar", "Kampanya performansini ve yayindaki indirim kurallarini izleyin.", new[] { "Kampanya", "Tur", "Baslangic", "Bitis", "Aktif", "Kullanim" }, "Kampanya bulunamadi.", null),
            "notifications" => ("Bildirimler", "Panel ici bildirimler ve sablon akislarini yonetin.", new[] { "Baslik", "Tur", "Onem", "Okundu", "Arsiv", "Olusturma" }, "Bildirim bulunamadi.", null),
            "settings" => ("Ayarlar", "Genel ayarlar icin veritabani karsiligi olan ayar tablolarini bir sonraki migration fazinda kuracagiz.", Array.Empty<string>(), "Ayar kaydi icin ayar tablolari gerekiyor.", "Bu ekran mevcut migration setinde karsiligi olmayan yeni tablo ailesi gerektiriyor."),
            "security" => ("Guvenlik", "Guvenlik paneli icin oturum, IP, 2FA ve audit yapisini genisletecegiz.", Array.Empty<string>(), "Guvenlik paneli migration fazinda detaylandirilacak.", "Mevcut tablolar log verir, ancak referans guvenlik ekrani icin ek yapilar gerekiyor."),
            "blog" => ("Blog Yonetimi", "Blog modulu icin yeni tablo ve medya baglantilari olusturulacak.", Array.Empty<string>(), "Blog icin veritabani tablolari henuz eklenmedi.", "Bu ekran icin blog kategori, yazi, etiket ve medya migration'lari acilacak."),
            "email-templates" => ("E-posta Sablonlari", "Mesaj ve bildirim sablonlarini veritabani uzerinden yonetin.", new[] { "Sablon", "Kategori", "Dil", "Aktif", "Sistem Geneli", "Konu" }, "Sablon kaydi bulunamadi.", null),
            "faq" => ("SSS Yonetimi", "SSS kategori ve soru/cevap akisini veritabani kayitlari ile yonetin.", new[] { "Kategori", "Soru", "One Cikan", "Aktif", "Olusturma" }, "SSS kaydi bulunamadi.", null),
            "complaints" => ("Sikayetler", "Sikayet ve itiraz yonetimi icin yeni tablo ailesi planlanacak.", Array.Empty<string>(), "Sikayet modulu tablolari henuz eklenmedi.", "Yorum raporlari var; ancak referanstaki sikayet modulu icin ayri veri modeli gerekiyor."),
            "logs" => ("Log Kayitlari", "Admin islem, sistem hata ve API loglarini merkezi olarak izleyin.", new[] { "Hedef", "Islem", "IP", "Tarih", "Kaynak", "Not" }, "Log kaydi bulunamadi.", null),
            "geo-search-logs" => ("Konum & Bölge Arama Logları", "Kullanıcının konumla arama yaptığı kayıtları; arama metni/bölgesi, yarıçap ve görünen oteller ile izleyin.", new[] { "Tarih", "Kaynak", "Arama Metni", "Arama Bölgesi", "Enlem", "Boylam", "Yarıçap(km)", "Görünen", "IP", "Cihaz" }, "Konum arama logu bulunamadı.", "Kaynak tablo: dbo.kullanici_konum_loglari (web/mobil arama istihbaratı)."),
            "hotel-coordinate-changes" => ("Otel Koordinat Değişimleri", "Otel enlem/boylam değişikliklerini admin bazlı audit trail ile takip edin.", new[] { "Tarih", "Admin", "Otel", "Önceki", "Yeni", "IP", "Not" }, "Koordinat değişim kaydı bulunamadı.", "Kaynak tablo: dbo.otel_koordinat_degisim_loglari"),
            "backups" => ("Yedekleme", "Yedekleme operasyonu icin snapshot kaydi ve dosya metadata tablolarini ekleyecegiz.", Array.Empty<string>(), "Yedekleme kaydi henuz bulunmuyor.", "Referans yedekleme ekrani icin yeni migration gerekir."),
            _ => ("Admin Panel", "Bu admin bolumu icin veritabani baglantisi hazirlaniyor.", Array.Empty<string>(), "Veri bulunamadi.", null)
        };
    }

    private static IEnumerable<(string Label, string Sql, string Description, string ToneClass, string IconClass)> GetSummaryDefinitions(string sectionKey)
    {
        return sectionKey switch
        {
            "users" =>
            [
                ("Toplam Kullanici", "SELECT COUNT(*) FROM users", "Tum hesaplar", "info", "fa-users"),
                ("Aktif Kullanici", "SELECT COUNT(*) FROM users WHERE hesap_durumu = 1", "Giris yapabilen hesaplar", "success", "fa-circle-check"),
                ("Onaysiz E-posta", "SELECT COUNT(*) FROM users WHERE email_dogrulama_tarihi IS NULL", "E-posta dogrulamasi bekleyenler", "warning", "fa-envelope-circle-check"),
                ("Pasif Kullanici", "SELECT COUNT(*) FROM users WHERE COALESCE(hesap_durumu, 0) = 0", "Panele veya siteye erisemeyen hesaplar", "danger", "fa-user-slash")
            ],
            "managers" =>
            [
                ("Yonetici", "SELECT COUNT(*) FROM users WHERE rol = 'admin'", "Admin rolundeki kullanicilar", "danger", "fa-user-tie"),
                ("Departman", "SELECT COUNT(*) FROM departmanlar", "Organizasyon birimleri", "info", "fa-sitemap"),
                ("Rol", "SELECT COUNT(*) FROM roller", "Sistem rolleri", "warning", "fa-key"),
                ("Rol Atamasi", "SELECT COUNT(*) FROM kullanici_rolleri", "Aktif veya gecmis rol kayitlari", "success", "fa-user-check")
            ],
            "hotels" =>
            [
                ("Toplam Otel", "SELECT COUNT(*) FROM oteller", "Tum tesis kayitlari", "info", "fa-hotel"),
                ("Yayinda", "SELECT COUNT(*) FROM oteller WHERE yayin_durumu = 'Yayında'", "Canli satistaki tesisler", "success", "fa-tower-broadcast"),
                ("Bekleyen Onay", "SELECT COUNT(*) FROM oteller WHERE onay_durumu = 'Beklemede'", "Inceleme bekleyen tesisler", "warning", "fa-hourglass-half"),
                ("Oda Tipi", "SELECT COUNT(*) FROM oda_tipleri", "Toplam oda tipi sayisi", "danger", "fa-bed")
            ],
            "reservations" =>
            [
                ("Toplam Rezervasyon", "SELECT COUNT(*) FROM rezervasyonlar", "Tum rezervasyon kayitlari", "info", "fa-calendar-check"),
                ("Onay Bekliyor", "SELECT COUNT(*) FROM rezervasyonlar WHERE durum = 'Onay Bekliyor'", "Islem bekleyen rezervasyonlar", "warning", "fa-clock"),
                ("Tamamlandi", "SELECT COUNT(*) FROM rezervasyonlar WHERE durum = 'Tamamlandı'", "Konaklamasi biten rezervasyonlar", "success", "fa-circle-check"),
                ("Iptal", "SELECT COUNT(*) FROM rezervasyonlar WHERE durum = 'İptal Edildi'", "Iptal edilenler", "danger", "fa-ban")
            ],
            "payments" =>
            [
                ("Odeme Islemi", "SELECT COUNT(*) FROM odeme_islemleri", "Tum odeme hareketleri", "info", "fa-credit-card"),
                ("Basarili", "SELECT COUNT(*) FROM odeme_islemleri WHERE odeme_durumu = 'Başarılı'", "Tamamlanan tahsilatlar", "success", "fa-circle-check"),
                ("Basarisiz", "SELECT COUNT(*) FROM odeme_islemleri WHERE odeme_durumu = 'Başarısız'", "Reddedilen islemler", "danger", "fa-circle-xmark"),
                ("Askida/Bekleyen", "SELECT COUNT(*) FROM odeme_islemleri WHERE odeme_durumu IN ('Beklemede','İşleniyor','Askıda')", "Inceleme veya islem bekleyenler", "warning", "fa-hourglass-half")
            ],
            "invoices" =>
            [
                ("Toplam Fatura", "SELECT COUNT(*) FROM faturalar", "Sistemdeki tum fatura kayitlari", "info", "fa-file-invoice"),
                ("Kesildi", "SELECT COUNT(*) FROM faturalar WHERE fatura_durumu = 'Kesildi'", "Aktif kesilmis faturalar", "success", "fa-file-circle-check"),
                ("Taslak", "SELECT COUNT(*) FROM faturalar WHERE fatura_durumu = 'Taslak'", "Hazirlik asamasindakiler", "warning", "fa-file-pen"),
                ("Iptal", "SELECT COUNT(*) FROM faturalar WHERE fatura_durumu = 'İptal Edildi'", "Iptal edilen faturalar", "danger", "fa-file-circle-xmark")
            ],
            "commissions" =>
            [
                ("Komisyon Kaydi", "SELECT COUNT(*) FROM komisyon_muhasebe_kayitlari", "Muhasebe donem kayitlari", "info", "fa-percent"),
                ("Beklemede", "SELECT COUNT(*) FROM komisyon_muhasebe_kayitlari WHERE otele_odeme_durumu = 'Beklemede'", "Otele odeme bekleyenler", "warning", "fa-wallet"),
                ("Odendi", "SELECT COUNT(*) FROM komisyon_muhasebe_kayitlari WHERE otele_odeme_durumu = 'Ödendi'", "Kapatilan odemeler", "success", "fa-money-bill-transfer"),
                ("Itirazli", "SELECT COUNT(*) FROM komisyon_muhasebe_kayitlari WHERE itiraz_var_mi = 1", "Mutabakat itirazli kayitlar", "danger", "fa-scale-balanced")
            ],
            "company-reservations" =>
            [
                ("Firma Rezervasyonu", "SELECT COUNT(*) FROM rezervasyonlar WHERE firma_id IS NOT NULL", "Firma bağlı tüm kayıtlar", "info", "fa-briefcase"),
                ("Onay Bekleyen", "SELECT COUNT(*) FROM rezervasyonlar WHERE firma_id IS NOT NULL AND firma_onay_durumu = 'Beklemede'", "Firma onay akışında", "warning", "fa-hourglass-half"),
                ("İptal", "SELECT COUNT(*) FROM rezervasyonlar WHERE firma_id IS NOT NULL AND durum = 'İptal Edildi'", "İptal edilenler", "danger", "fa-ban"),
                ("Toplam Tutar", "SELECT COALESCE(SUM(COALESCE(toplam_tutar,0)),0) FROM rezervasyonlar WHERE firma_id IS NOT NULL AND COALESCE(durum,'') <> 'İptal Edildi'", "İptal hariç ciro", "success", "fa-money-bill-wave")
            ],
            "partner-applications" =>
            [
                ("Toplam Partner", "SELECT COUNT(*) FROM partner_detaylari", "Tum partner hesaplari", "info", "fa-handshake-angle"),
                ("Beklemede", "SELECT COUNT(*) FROM partner_detaylari WHERE onay_durumu = 'Beklemede'", "Inceleme bekleyen basvurular", "warning", "fa-hourglass-half"),
                ("Onaylandi", "SELECT COUNT(*) FROM partner_detaylari WHERE onay_durumu = 'Onaylandi'", "Aktif partner hesaplari", "success", "fa-circle-check"),
                ("Reddedildi", "SELECT COUNT(*) FROM partner_detaylari WHERE onay_durumu = 'Reddedildi'", "Reddedilen kayitlar", "danger", "fa-circle-xmark")
            ],
            "company-applications" =>
            [
                ("Toplam Firma", "SELECT COUNT(*) FROM firmalar", "Tum firma profilleri", "info", "fa-building"),
                ("Beklemede", "SELECT COUNT(*) FROM firmalar WHERE COALESCE(onay_durumu,'Beklemede') = 'Beklemede'", "Onay bekleyen firmalar", "warning", "fa-hourglass-half"),
                ("Onaylandi", "SELECT COUNT(*) FROM firmalar WHERE COALESCE(onay_durumu,'') = 'Onaylandı'", "Aktif firma hesaplari", "success", "fa-circle-check"),
                ("Firma Rezervasyonu", "SELECT COUNT(*) FROM rezervasyonlar WHERE firma_id IS NOT NULL", "Firma baglantili rezervasyonlar", "danger", "fa-briefcase")
            ],
            "platform-officials" =>
            [
                ("Yetkili Hesap", "SELECT COUNT(*) FROM users WHERE rol IN ('admin','superadmin')", "Admin ve superadmin kullanicilar", "info", "fa-user-shield"),
                ("Aktif Yetkili", "SELECT COUNT(*) FROM users WHERE rol IN ('admin','superadmin') AND hesap_durumu = 1", "Panele erisebilen yetkililer", "success", "fa-user-check"),
                ("Departman Kaydi", "SELECT COUNT(*) FROM kullanici_departman", "Yetkili departman baglantilari", "warning", "fa-sitemap"),
                ("Rol Kaydi", "SELECT COUNT(*) FROM kullanici_rolleri", "Rol atama kayitlari", "danger", "fa-key")
            ],
            "active-hotels" =>
            [
                ("Yayinda", "SELECT COUNT(*) FROM oteller WHERE yayin_durumu = 'Yayında' AND onay_durumu = 'Onaylandı'", "Yayinda ve onayli oteller", "success", "fa-tower-broadcast"),
                ("Toplam Oda Tipi", "SELECT COUNT(*) FROM oda_tipleri ot INNER JOIN oteller o ON o.id = ot.otel_id WHERE o.yayin_durumu = 'Yayında' AND o.onay_durumu = 'Onaylandı'", "Acik otellerdeki oda tipleri", "info", "fa-bed"),
                ("Toplam Rezervasyon", "SELECT COUNT(*) FROM rezervasyonlar r INNER JOIN oteller o ON o.id = r.otel_id WHERE o.yayin_durumu = 'Yayında' AND o.onay_durumu = 'Onaylandı'", "Acik otellere gelen rezervasyonlar", "warning", "fa-calendar-check"),
                ("Toplam Gelir", "SELECT COALESCE(SUM(COALESCE(r.toplam_tutar,0)),0) FROM rezervasyonlar r INNER JOIN oteller o ON o.id = r.otel_id WHERE o.yayin_durumu = 'Yayında' AND o.onay_durumu = 'Onaylandı' AND COALESCE(r.durum,'') <> 'İptal Edildi'", "Iptal disi rezervasyon gelirleri", "danger", "fa-money-bill-wave")
            ],
            "pending-hotels" =>
            [
                ("Bekleyen Onay", "SELECT COUNT(*) FROM oteller WHERE onay_durumu = 'Beklemede'", "Onay bekleyen tesisler", "warning", "fa-hourglass-half"),
                ("Taslak Yayin", "SELECT COUNT(*) FROM oteller WHERE yayin_durumu <> 'Yayında'", "Yayina alinmamis tesisler", "info", "fa-file-pen"),
                ("Partner Basvuru Bekliyor", "SELECT COUNT(*) FROM partner_detaylari WHERE onay_durumu = 'Beklemede'", "Partner adiminda bekleyenler", "danger", "fa-user-clock"),
                ("Eksik Medya", "SELECT COUNT(*) FROM oteller o WHERE NOT EXISTS (SELECT 1 FROM otel_gorselleri g WHERE g.otel_id = o.id)", "Gorsel yuklenmemis oteller", "success", "fa-image")
            ],
            "reviews" =>
            [
                ("Toplam Yorum", "SELECT COUNT(*) FROM yorumlar", "Tesis yorumlari", "info", "fa-star"),
                ("Beklemede", "SELECT COUNT(*) FROM yorumlar WHERE onay_durumu = 'Beklemede'", "Moderasyon bekleyenler", "warning", "fa-hourglass-half"),
                ("Onaylandi", "SELECT COUNT(*) FROM yorumlar WHERE onay_durumu = 'Onaylandı'", "Yayinda olan yorumlar", "success", "fa-thumbs-up"),
                ("Raporlandi", "SELECT COUNT(*) FROM yorumlar WHERE rapor_sayisi > 0", "Incelenmesi gerekenler", "danger", "fa-flag")
            ],
            "campaigns" =>
            [
                ("Kampanya", "SELECT COUNT(*) FROM kampanyalar", "Tum kampanya kayitlari", "info", "fa-bullhorn"),
                ("Aktif", "SELECT COUNT(*) FROM kampanyalar WHERE aktif_mi = 1", "Yayinda kampanyalar", "success", "fa-badge-percent"),
                ("One Cikan", "SELECT COUNT(*) FROM kampanyalar WHERE one_cikan_kampanya = 1", "Ana sayfa on plana cikacak kampanyalar", "warning", "fa-fire"),
                ("Toplam Kullanim", "SELECT COALESCE(SUM(kullanilan_adet),0) FROM kampanyalar", "Kampanya kullanim adedi", "danger", "fa-chart-column")
            ],
            "notifications" =>
            [
                ("Sistem Bildirimi", "SELECT COUNT(*) FROM sistem_ici_bildirimler", "Tum panel bildirimleri", "info", "fa-bell"),
                ("Okunmamis", "SELECT COUNT(*) FROM sistem_ici_bildirimler WHERE okundu_mu = 0", "Henuz gorulmeyen bildirimler", "warning", "fa-envelope-open-text"),
                ("Bildirim Sablonu", "SELECT COUNT(*) FROM bildirim_sablonlari", "Push/SMS/mail sablonlari", "success", "fa-file-lines"),
                ("Mesaj Sablonu", "SELECT COUNT(*) FROM mesaj_sablonlari", "Operasyonel mesaj sablonlari", "danger", "fa-comments")
            ],
            "logs" =>
            [
                ("Admin Islem Logu", "SELECT COUNT(*) FROM admin_islem_loglari", "Yonetici aksiyon kayitlari", "info", "fa-clipboard-list"),
                ("Sistem Hata", "SELECT COUNT(*) FROM sistem_hata_loglari", "Uygulama hata kayitlari", "danger", "fa-bug"),
                ("API Logu", "SELECT COUNT(*) FROM api_loglari", "API erisim loglari", "warning", "fa-cloud-arrow-up"),
                ("Kullanici Aktivitesi", "SELECT COUNT(*) FROM kullanici_aktivite_loglari", "Oturum ve hareket gecmisi", "success", "fa-user-clock")
            ],
            "geo-search-logs" =>
            [
                ("Toplam Log", "SELECT COUNT(*) FROM kullanici_konum_loglari", "Konumla arama log kayitlari", "info", "fa-location-dot"),
                ("Bugün", "SELECT COUNT(*) FROM kullanici_konum_loglari WHERE CAST(kayit_tarihi AS date) = CAST(SYSUTCDATETIME() AS date)", "Bugün üretilen kayıtlar", "success", "fa-calendar-day"),
                ("Konum Araması", "SELECT COUNT(*) FROM kullanici_konum_loglari WHERE COALESCE(arama_bolgesi,'') <> ''", "Arama bölgesi dolu kayıtlar", "warning", "fa-map-location-dot"),
                ("Yarıçaplı", "SELECT COUNT(*) FROM kullanici_konum_loglari WHERE yaricap_km IS NOT NULL", "Yarıçap parametreli kayıtlar", "danger", "fa-circle-nodes")
            ],
            "hotel-coordinate-changes" =>
            [
                ("Toplam Değişim", "SELECT COUNT(*) FROM otel_koordinat_degisim_loglari", "Koordinat değişim kayıtları", "info", "fa-route"),
                ("Bugün", "SELECT COUNT(*) FROM otel_koordinat_degisim_loglari WHERE CAST(kayit_tarihi AS date) = CAST(SYSUTCDATETIME() AS date)", "Bugün yapılan değişimler", "success", "fa-calendar-day"),
                ("Farklı IP", "SELECT COUNT(DISTINCT ip_adresi) FROM otel_koordinat_degisim_loglari", "Değişim yapılan IP çeşitliliği", "warning", "fa-globe"),
                ("Farklı Otel", "SELECT COUNT(DISTINCT otel_id) FROM otel_koordinat_degisim_loglari", "Etkilenen otel sayısı", "danger", "fa-hotel")
            ],
            "email-templates" =>
            [
                ("Mesaj Sablonu", "SELECT COUNT(*) FROM mesaj_sablonlari", "Mail/mesaj sablon seti", "info", "fa-envelope"),
                ("Bildirim Sablonu", "SELECT COUNT(*) FROM bildirim_sablonlari", "Push/SMS/system ici sablonlar", "warning", "fa-paper-plane"),
                ("Aktif Mesaj", "SELECT COUNT(*) FROM mesaj_sablonlari WHERE aktif_mi = 1", "Kullanilan mail sablonlari", "success", "fa-circle-check"),
                ("Aktif Bildirim", "SELECT COUNT(*) FROM bildirim_sablonlari WHERE aktif_mi = 1", "Yayinda bildirim sablonlari", "danger", "fa-bell-concierge")
            ],
            "faq" =>
            [
                ("SSS Kategorisi", "SELECT COUNT(*) FROM sss_kategorileri WHERE aktif_mi = 1", "Aktif destek kategorileri", "info", "fa-layer-group"),
                ("Toplam Soru", "SELECT COUNT(*) FROM sss_sorulari", "Tum soru ve cevap kayitlari", "warning", "fa-circle-question"),
                ("One Cikan", "SELECT COUNT(*) FROM sss_sorulari WHERE one_cikan_mi = 1", "Ana akista vurgulanan sorular", "success", "fa-fire"),
                ("Aktif", "SELECT COUNT(*) FROM sss_sorulari WHERE aktif_mi = 1", "Yayinda olan soru/cevaplar", "danger", "fa-circle-check")
            ],
            _ => []
        };
    }

    private static string GetTableSql(string sectionKey)
    {
        return sectionKey switch
        {
            "users" => @"SELECT TOP (40)
                                CAST(u.id AS nvarchar(30)),
                                COALESCE(NULLIF(u.ad_soyad, ''), '-'),
                                COALESCE(NULLIF(u.eposta, ''), '-'),
                                COALESCE(NULLIF(u.telefon, ''), NULLIF(u.telefon_e164, ''), '-'),
                                CASE
                                    WHEN reservationStats.reservation_count >= 10 OR reservationStats.total_spent >= 100000 THEN 'Gold'
                                    WHEN reservationStats.reservation_count >= 4 OR reservationStats.total_spent >= 30000 THEN 'Silver'
                                    ELSE 'Bronze'
                                END,
                                CAST(reservationStats.reservation_count AS nvarchar(20)),
                                FORMAT(reservationStats.loyalty_points, 'N0', 'tr-TR'),
                                CASE
                                    WHEN COALESCE(u.hesap_durumu, 0) = 0 THEN 'Pasif'
                                    WHEN u.email_dogrulama_tarihi IS NULL THEN 'Onaysiz'
                                    ELSE 'Aktif'
                                END,
                                COALESCE(NULLIF(u.rol, ''), 'user'),
                                FORMAT(u.olusturulma_tarihi, 'dd.MM.yyyy', 'tr-TR')
                         FROM users u
                         OUTER APPLY
                         (
                             SELECT
                                 COUNT(r.id) AS reservation_count,
                                 COALESCE(SUM(COALESCE(r.toplam_tutar, 0)), 0) AS total_spent,
                                 CAST(ROUND(COALESCE(SUM(COALESCE(r.toplam_tutar, 0)), 0) / 12.5, 0) AS int) AS loyalty_points
                             FROM rezervasyonlar r
                             WHERE r.kullanici_id = u.id
                               AND COALESCE(r.durum, '') <> 'İptal Edildi'
                         ) reservationStats
                         ORDER BY u.id DESC;",
            "managers" => @"SELECT TOP (12) u.ad_soyad, u.eposta, COALESCE(d.departman_adi, '-'), COALESCE(r.rol_adi, u.rol), COALESCE(FORMAT(u.son_giris_tarihi, 'dd.MM.yyyy HH:mm', 'tr-TR'), '-') FROM users u LEFT JOIN kullanici_departman kd ON kd.kullanici_id = u.id LEFT JOIN departmanlar d ON d.id = kd.departman_id LEFT JOIN kullanici_rolleri kr ON kr.kullanici_id = u.id AND (kr.bitis_tarihi IS NULL OR kr.bitis_tarihi > SYSUTCDATETIME()) LEFT JOIN roller r ON r.id = kr.rol_id WHERE u.rol = 'admin' ORDER BY u.id DESC;",
            "hotels" => @"SELECT TOP (12) otel_adi, CONCAT(ilce, ', ', sehir), otel_turu, yayin_durumu, onay_durumu, FORMAT(ortalama_puan, '0.0', 'tr-TR') FROM oteller ORDER BY id DESC;",
            "reservations" => @"SELECT TOP (12) rezervasyon_no, misafir_ad_soyad, FORMAT(giris_tarihi, 'dd.MM.yyyy', 'tr-TR'), FORMAT(cikis_tarihi, 'dd.MM.yyyy', 'tr-TR'), durum, FORMAT(toplam_tutar, 'N0', 'tr-TR') FROM rezervasyonlar ORDER BY id DESC;",
            "payments" => @"SELECT TOP (12) islem_no, odeme_turu, odeme_durumu, odeme_yontemi, FORMAT(toplam_tahsilat, 'N0', 'tr-TR'), FORMAT(odeme_baslangic_tarihi, 'dd.MM.yyyy HH:mm', 'tr-TR') FROM odeme_islemleri ORDER BY id DESC;",
            "invoices" => @"SELECT TOP (12) fatura_no, FORMAT(fatura_tarihi, 'dd.MM.yyyy', 'tr-TR'), fatura_turu, fatura_durumu, FORMAT(genel_toplam, 'N0', 'tr-TR'), para_birimi FROM faturalar ORDER BY id DESC;",
            "commissions" => @"SELECT TOP (12) kayit_no, donem, o.otel_adi, FORMAT(komisyon_tutari, 'N0', 'tr-TR'), otele_odeme_durumu, mutabakat_durumu FROM komisyon_muhasebe_kayitlari k LEFT JOIN oteller o ON o.id = k.otel_id ORDER BY k.id DESC;",
            "company-reservations" => @"SELECT TOP (120)
                                            r.rezervasyon_no,
                                            COALESCE(f.firma_adi,'-') AS firma,
                                            COALESCE(u.ad_soyad, '-') AS personel,
                                            o.otel_adi,
                                            CONCAT(o.ilce, ', ', o.sehir) AS konum,
                                            FORMAT(r.giris_tarihi, 'dd.MM.yyyy', 'tr-TR'),
                                            FORMAT(r.cikis_tarihi, 'dd.MM.yyyy', 'tr-TR'),
                                            COALESCE(r.durum,'-'),
                                            COALESCE(r.firma_onay_durumu,'-'),
                                            FORMAT(COALESCE(r.toplam_tutar,0), 'N0', 'tr-TR')
                                         FROM rezervasyonlar r
                                         INNER JOIN oteller o ON o.id = r.otel_id
                                         LEFT JOIN firmalar f ON f.id = r.firma_id
                                         LEFT JOIN users u ON u.id = r.firma_calisan_id
                                         WHERE r.firma_id IS NOT NULL
                                         ORDER BY r.olusturulma_tarihi DESC, r.id DESC;",
            "partner-applications" => @"SELECT TOP (12) firma_unvani, yetkili_ad_soyad, yetkili_eposta, vergi_numarasi, onay_durumu, FORMAT(olusturulma_tarihi, 'dd.MM.yyyy', 'tr-TR') FROM partner_detaylari ORDER BY id DESC;",
            "company-applications" => @"SELECT TOP (20) f.firma_adi, COALESCE(f.onay_durumu, 'Beklemede'),
                                                (SELECT COUNT(*) FROM users u WHERE u.firma_id = f.id AND u.rol LIKE 'firma_%'),
                                                (SELECT COUNT(*) FROM rezervasyonlar r WHERE r.firma_id = f.id),
                                                FORMAT(f.olusturulma_tarihi, 'dd.MM.yyyy', 'tr-TR')
                                         FROM firmalar f
                                         ORDER BY f.id DESC;",
            "platform-officials" => @"SELECT TOP (20) u.ad_soyad, u.eposta, COALESCE(NULLIF(u.rol, ''), 'admin'),
                                               CASE WHEN COALESCE(u.hesap_durumu, 0) = 1 THEN 'Aktif' ELSE 'Pasif' END,
                                               COALESCE(FORMAT(u.son_giris_tarihi, 'dd.MM.yyyy HH:mm', 'tr-TR'), '-'),
                                               FORMAT(u.olusturulma_tarihi, 'dd.MM.yyyy', 'tr-TR')
                                        FROM users u
                                        WHERE u.rol IN ('admin', 'superadmin')
                                        ORDER BY COALESCE(u.son_giris_tarihi, u.olusturulma_tarihi) DESC;",
            "active-hotels" => @"SELECT TOP (20)
                                        o.otel_adi,
                                        CONCAT(o.ilce, ', ', o.sehir),
                                        FORMAT(COALESCE(o.ortalama_puan, 0), '0.0', 'tr-TR'),
                                        (SELECT COUNT(*) FROM rezervasyonlar r WHERE r.otel_id = o.id),
                                        FORMAT(COALESCE((SELECT SUM(COALESCE(r.toplam_tutar,0)) FROM rezervasyonlar r WHERE r.otel_id = o.id AND COALESCE(r.durum,'') <> 'İptal Edildi'),0), 'N0', 'tr-TR'),
                                        FORMAT(COALESCE(o.guncellenme_tarihi, o.olusturulma_tarihi), 'dd.MM.yyyy HH:mm', 'tr-TR')
                                     FROM oteller o
                                     WHERE o.yayin_durumu = 'Yayında' AND o.onay_durumu = 'Onaylandı'
                                     ORDER BY COALESCE(o.ortalama_puan, 0) DESC, o.id DESC;",
            "pending-hotels" => @"SELECT TOP (20)
                                         o.otel_adi,
                                         CONCAT(o.ilce, ', ', o.sehir),
                                         COALESCE(o.onay_durumu, '-'),
                                         COALESCE(o.yayin_durumu, '-'),
                                         FORMAT(o.olusturulma_tarihi, 'dd.MM.yyyy', 'tr-TR'),
                                         FORMAT(COALESCE(o.guncellenme_tarihi, o.olusturulma_tarihi), 'dd.MM.yyyy HH:mm', 'tr-TR')
                                  FROM oteller o
                                  WHERE COALESCE(o.onay_durumu, '') = 'Beklemede'
                                     OR COALESCE(o.yayin_durumu, '') <> 'Yayında'
                                  ORDER BY o.olusturulma_tarihi DESC;",
            "reviews" => @"SELECT TOP (12) COALESCE(yorum_basligi, 'Basliksiz'), genel_puan, onay_durumu, rapor_sayisi, dogrulanmis_konaklama, FORMAT(olusturulma_tarihi, 'dd.MM.yyyy', 'tr-TR') FROM yorumlar ORDER BY id DESC;",
            "campaigns" => @"SELECT TOP (12) kampanya_adi, tur, FORMAT(baslangic_tarihi, 'dd.MM.yyyy', 'tr-TR'), FORMAT(bitis_tarihi, 'dd.MM.yyyy', 'tr-TR'), aktif_mi, kullanilan_adet FROM kampanyalar ORDER BY id DESC;",
            "notifications" => @"SELECT TOP (12) baslik, bildirim_turu, onem_derecesi, okundu_mu, arsivlendi_mi, FORMAT(olusturulma_tarihi, 'dd.MM.yyyy HH:mm', 'tr-TR') FROM sistem_ici_bildirimler ORDER BY id DESC;",
            "logs" => @"SELECT TOP (6) hedef_tablo, islem_turu, ip_adresi, FORMAT(islem_tarihi, 'dd.MM.yyyy HH:mm', 'tr-TR'), 'Admin Islem', '' FROM admin_islem_loglari ORDER BY id DESC;",
            "geo-search-logs" => @"SELECT TOP (80)
                                        FORMAT(kayit_tarihi, 'dd.MM.yyyy HH:mm', 'tr-TR') AS tarih,
                                        COALESCE(kaynak,'-') AS kaynak,
                                        COALESCE(arama_metni,'-') AS arama_metni,
                                        COALESCE(arama_bolgesi,'-') AS arama_bolgesi,
                                        FORMAT(enlem, '0.0000000', 'en-US') AS enlem,
                                        FORMAT(boylam, '0.0000000', 'en-US') AS boylam,
                                        COALESCE(CAST(yaricap_km AS nvarchar(20)),'-') AS yaricap_km,
                                        COALESCE(CAST(gorunen_otel_sayisi AS nvarchar(20)),'-') AS gorunen_otel_sayisi,
                                        COALESCE(ip_adresi,'-') AS ip,
                                        COALESCE(cihaz_tipi,'-') AS cihaz
                                     FROM kullanici_konum_loglari
                                     ORDER BY kayit_tarihi DESC;",
            "hotel-coordinate-changes" => @"SELECT TOP (120)
                                                FORMAT(kayit_tarihi, 'dd.MM.yyyy HH:mm', 'tr-TR'),
                                                COALESCE(admin_ad_soyad, CONCAT('Admin#', admin_kullanici_id)),
                                                COALESCE(otel_adi, CONCAT('Otel#', otel_id)),
                                                CONCAT(FORMAT(onceki_enlem, '0.0000000', 'en-US'), ', ', FORMAT(onceki_boylam, '0.0000000', 'en-US')),
                                                CONCAT(FORMAT(yeni_enlem, '0.0000000', 'en-US'), ', ', FORMAT(yeni_boylam, '0.0000000', 'en-US')),
                                                COALESCE(ip_adresi, '-'),
                                                COALESCE(notlar, '-')
                                           FROM otel_koordinat_degisim_loglari
                                           ORDER BY kayit_tarihi DESC;",
            "email-templates" => @"SELECT TOP (12) sablon_adi, kategori, dil, aktif_mi, sistem_geneli_mi, konu_basligi FROM mesaj_sablonlari ORDER BY id DESC;",
            "faq" => @"SELECT TOP (20) k.kategori_adi, s.soru, s.one_cikan_mi, s.aktif_mi, FORMAT(s.olusturulma_tarihi, 'dd.MM.yyyy', 'tr-TR') FROM sss_sorulari s INNER JOIN sss_kategorileri k ON k.id = s.sss_kategori_id ORDER BY k.siralama, s.siralama, s.id;",
            _ => string.Empty
        };
    }

    private static int SafeInt(SqlDataReader reader, int ordinal)
    {
        return reader.IsDBNull(ordinal) ? 0 : Convert.ToInt32(reader.GetValue(ordinal));
    }

    private static bool SafeBool(SqlDataReader reader, int ordinal)
    {
        return !reader.IsDBNull(ordinal) && Convert.ToInt32(reader.GetValue(ordinal), CultureInfo.InvariantCulture) == 1;
    }

    private static decimal SafeDecimal(SqlDataReader reader, int ordinal)
    {
        return reader.IsDBNull(ordinal) ? 0m : Convert.ToDecimal(reader.GetValue(ordinal), CultureInfo.InvariantCulture);
    }

    private static string FormatScalar(object? value)
    {
        return value switch
        {
            null or DBNull => "0",
            decimal number => number.ToString("0.##"),
            double number => number.ToString("0.##"),
            float number => number.ToString("0.##"),
            _ => value?.ToString() ?? "0"
        };
    }

    private static string FormatRelative(DateTime? value)
    {
        if (!value.HasValue)
        {
            return "Zaman bilgisi yok";
        }

        var diff = DateTime.Now - value.Value;
        if (diff.TotalMinutes < 1) return "Az once";
        if (diff.TotalHours < 1) return $"{Math.Max(1, (int)diff.TotalMinutes)} dk once";
        if (diff.TotalDays < 1) return $"{Math.Max(1, (int)diff.TotalHours)} saat once";
        return $"{Math.Max(1, (int)diff.TotalDays)} gun once";
    }

    private static string MapStatusTone(string status)
    {
        return status switch
        {
            "Yayında" or "Onaylandı" => "success",
            "Bakımda" or "Beklemede" => "warning",
            "Kapatıldı" or "Reddedildi" => "danger",
            _ => "info"
        };
    }

    private static async Task<bool> TableExistsAsync(
        SqlConnection connection,
        string tableName,
        CancellationToken cancellationToken,
        SqlTransaction? transaction = null)
    {
        const string sql = """
            SELECT COUNT(*)
            FROM INFORMATION_SCHEMA.TABLES
            WHERE TABLE_SCHEMA = 'dbo'
              AND TABLE_NAME = @tableName;
            """;

        await using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("@tableName", tableName);
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken), CultureInfo.InvariantCulture) > 0;
    }
}

