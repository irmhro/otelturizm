using System.Globalization;
using System.Security.Claims;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Data.SqlClient;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Net.Pop3;
using MailKit.Search;
using SqlConnection = Microsoft.Data.SqlClient.SqlConnection;
using SqlCommand = Microsoft.Data.SqlClient.SqlCommand;
using SqlTransaction = Microsoft.Data.SqlClient.SqlTransaction;
using SqlException = Microsoft.Data.SqlClient.SqlException;
using otelturizmnew.Models.Paneller.Admin;
using otelturizmnew.Services.Abstractions;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace otelturizmnew.Services;

public class AdminService : IAdminService
{
    private readonly string _connectionString;
    private readonly IConfiguration _configuration;
    private readonly IDataProtector _mailAccountProtector;
    private readonly IEmailQueueService _emailQueueService;
    private readonly IAdminRbacService _adminRbacService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly CommerceMetricsAccumulator _commerceMetrics;
    private readonly IGrowthGovernanceService _growthGovernance;
    private readonly HealthCheckService _healthCheckService;

    public AdminService(
        IConfiguration configuration,
        IDataProtectionProvider dataProtectionProvider,
        IEmailQueueService emailQueueService,
        IAdminRbacService adminRbacService,
        IHttpContextAccessor httpContextAccessor,
        CommerceMetricsAccumulator commerceMetrics,
        IGrowthGovernanceService growthGovernance,
        HealthCheckService healthCheckService)
    {
        _configuration = configuration;
        _mailAccountProtector = dataProtectionProvider.CreateProtector("Admin.PlatformMailAccounts.v1");
        _emailQueueService = emailQueueService;
        _adminRbacService = adminRbacService;
        _httpContextAccessor = httpContextAccessor;
        _commerceMetrics = commerceMetrics;
        _growthGovernance = growthGovernance;
        _healthCheckService = healthCheckService;
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

    public async Task<AdminActionLogsPageViewModel> GetAdminActionLogsAsync(string fullName, string email, string userRole, AdminActionLogFilter filter, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var shell = await GetShellAsync(connection, "Admin İşlem Logları", "Kritik admin aksiyonlarını filtreleyin ve dışa aktarın.", fullName, email, userRole, cancellationToken);
        var model = new AdminActionLogsPageViewModel { Shell = shell, Filter = filter ?? new AdminActionLogFilter() };

        var safePage = Math.Max(1, model.Filter.Page);
        var safePageSize = Math.Clamp(model.Filter.PageSize <= 0 ? 50 : model.Filter.PageSize, 10, 200);
        model.Filter.Page = safePage;
        model.Filter.PageSize = safePageSize;

        var where = new List<string>();
        var prms = new List<SqlParameter>();

        if (model.Filter.AdminUserId.HasValue && model.Filter.AdminUserId.Value > 0)
        {
            where.Add("a.admin_kullanici_id = @adminId");
            prms.Add(new SqlParameter("@adminId", model.Filter.AdminUserId.Value));
        }
        if (!string.IsNullOrWhiteSpace(model.Filter.ActionType))
        {
            where.Add("LOWER(a.islem_turu) = LOWER(@type)");
            prms.Add(new SqlParameter("@type", model.Filter.ActionType.Trim()));
        }
        if (!string.IsNullOrWhiteSpace(model.Filter.TargetTable))
        {
            where.Add("LOWER(a.hedef_tablo) = LOWER(@table)");
            prms.Add(new SqlParameter("@table", model.Filter.TargetTable.Trim()));
        }
        if (!string.IsNullOrWhiteSpace(model.Filter.Query))
        {
            where.Add("(a.aciklama LIKE @q OR a.hedef_kayit_id LIKE @q OR a.ip_adresi LIKE @q)");
            prms.Add(new SqlParameter("@q", "%" + model.Filter.Query.Trim() + "%"));
        }
        if (model.Filter.FromUtc.HasValue)
        {
            where.Add("a.islem_tarihi >= @fromUtc");
            prms.Add(new SqlParameter("@fromUtc", model.Filter.FromUtc.Value.UtcDateTime));
        }
        if (model.Filter.ToUtc.HasValue)
        {
            where.Add("a.islem_tarihi <= @toUtc");
            prms.Add(new SqlParameter("@toUtc", model.Filter.ToUtc.Value.UtcDateTime));
        }

        var whereSql = where.Count == 0 ? string.Empty : "WHERE " + string.Join(" AND ", where);
        var sort = string.Equals(model.Filter.Sort, "date_asc", StringComparison.OrdinalIgnoreCase) ? "ASC" : "DESC";

        var countSql = $"SELECT COUNT(*) FROM admin_islem_loglari a {whereSql};";
        await using (var countCmd = new SqlCommand(countSql, connection))
        {
            countCmd.Parameters.AddRange(prms.ToArray());
            model.Total = Convert.ToInt32(await countCmd.ExecuteScalarAsync(cancellationToken) ?? 0, CultureInfo.InvariantCulture);
        }

        var offset = (safePage - 1) * safePageSize;
        var listSql = $"""
            SELECT a.id, a.admin_kullanici_id, a.islem_turu, a.hedef_tablo, a.hedef_kayit_id, a.aciklama, a.ip_adresi, a.islem_tarihi
            FROM admin_islem_loglari a
            {whereSql}
            ORDER BY a.islem_tarihi {sort}, a.id {sort}
            OFFSET @offset ROWS FETCH NEXT @take ROWS ONLY;
            """;

        await using (var cmd = new SqlCommand(listSql, connection))
        {
            cmd.Parameters.AddRange(prms.ToArray());
            cmd.Parameters.AddWithValue("@offset", offset);
            cmd.Parameters.AddWithValue("@take", safePageSize);
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                model.Rows.Add(new AdminActionLogRowViewModel
                {
                    Id = reader.GetInt64(0),
                    AdminUserId = reader.GetInt64(1),
                    ActionType = reader.IsDBNull(2) ? "-" : reader.GetString(2),
                    TargetTable = reader.IsDBNull(3) ? "-" : reader.GetString(3),
                    TargetId = reader.IsDBNull(4) ? null : reader.GetString(4),
                    Note = reader.IsDBNull(5) ? null : reader.GetString(5),
                    IpAddress = reader.IsDBNull(6) ? null : reader.GetString(6),
                    CreatedAtUtc = new DateTimeOffset(reader.GetDateTime(7), TimeSpan.Zero)
                });
            }
        }

        return model;
    }

    public async Task<string> ExportAdminActionLogsCsvAsync(AdminActionLogFilter filter, CancellationToken cancellationToken = default)
    {
        // CSV içerik üretir (controller File() döndürür)
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var f = filter ?? new AdminActionLogFilter();
        f.Page = 1;
        f.PageSize = 5000; // export upper bound

        var model = await GetAdminActionLogsAsync("-", "-", "admin", f, cancellationToken);
        var sb = new StringBuilder();
        sb.AppendLine("id,admin_user_id,action_type,target_table,target_id,ip,created_at_utc,note");
        foreach (var r in model.Rows)
        {
            sb.Append(r.Id).Append(',')
              .Append(r.AdminUserId).Append(',')
              .Append(Csv(r.ActionType)).Append(',')
              .Append(Csv(r.TargetTable)).Append(',')
              .Append(Csv(r.TargetId)).Append(',')
              .Append(Csv(r.IpAddress)).Append(',')
              .Append(r.CreatedAtUtc.UtcDateTime.ToString("O", CultureInfo.InvariantCulture)).Append(',')
              .Append(Csv(r.Note))
              .AppendLine();
        }
        return sb.ToString();
    }

    public async Task<AdminEmailQueuePageViewModel> GetEmailQueueAsync(string fullName, string email, string userRole, AdminEmailQueueFilter filter, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var shell = await GetShellAsync(connection, "E-posta Kuyruğu", "Bekleyen e-postaları inceleyin, yeniden deneyin ve kuyruk sağlığını izleyin.", fullName, email, userRole, cancellationToken);
        var model = new AdminEmailQueuePageViewModel { Shell = shell, Filter = filter ?? new AdminEmailQueueFilter() };

        var safePage = Math.Max(1, model.Filter.Page);
        var safePageSize = Math.Clamp(model.Filter.PageSize <= 0 ? 50 : model.Filter.PageSize, 10, 200);
        model.Filter.Page = safePage;
        model.Filter.PageSize = safePageSize;

        var hasNextAttempt = await ColumnExistsAsync(connection, "dbo.bildirim_loglari", "sonraki_deneme_utc", cancellationToken);
        var hasMaxAttemptsColumn = await ColumnExistsAsync(connection, "dbo.bildirim_loglari", "maksimum_deneme", cancellationToken);
        var maxAttemptsSql = hasMaxAttemptsColumn ? "COALESCE(b.maksimum_deneme,3)" : "3";

        var where = new List<string> { "b.tur = N'E-posta'" };
        var prms = new List<SqlParameter>();
        if (!string.IsNullOrWhiteSpace(model.Filter.Status))
        {
            where.Add("LOWER(b.durum) = LOWER(@status)");
            prms.Add(new SqlParameter("@status", model.Filter.Status.Trim()));
        }
        if (!string.IsNullOrWhiteSpace(model.Filter.Query))
        {
            where.Add("(b.alici_eposta LIKE @q OR b.konu LIKE @q)");
            prms.Add(new SqlParameter("@q", "%" + model.Filter.Query.Trim() + "%"));
        }

        var whereSql = "WHERE " + string.Join(" AND ", where);

        var countSql = $"SELECT COUNT(*) FROM bildirim_loglari b {whereSql};";
        await using (var countCmd = new SqlCommand(countSql, connection))
        {
            countCmd.Parameters.AddRange(prms.ToArray());
            model.Total = Convert.ToInt32(await countCmd.ExecuteScalarAsync(cancellationToken) ?? 0, CultureInfo.InvariantCulture);
        }

        var offset = (safePage - 1) * safePageSize;
        var listSql = hasNextAttempt
            ? $"""
                SELECT b.id, COALESCE(b.kullanici_id, 0), COALESCE(b.alici_eposta,''), COALESCE(b.konu,''),
                       COALESCE(b.durum,''), COALESCE(b.saglayici_mesaj_id,''), COALESCE(b.gonderme_denemesi,0), {maxAttemptsSql},
                       COALESCE(b.olusturulma_tarihi, SYSUTCDATETIME()) AS created_at,
                       b.sonraki_deneme_utc,
                       COALESCE(b.hata_mesaji,'')
                FROM bildirim_loglari b
                {whereSql}
                ORDER BY b.id DESC
                OFFSET @offset ROWS FETCH NEXT @take ROWS ONLY;
                """
            : $"""
                SELECT b.id, COALESCE(b.kullanici_id, 0), COALESCE(b.alici_eposta,''), COALESCE(b.konu,''),
                       COALESCE(b.durum,''), COALESCE(b.saglayici_mesaj_id,''), COALESCE(b.gonderme_denemesi,0), {maxAttemptsSql},
                       COALESCE(b.olusturulma_tarihi, SYSUTCDATETIME()) AS created_at,
                       COALESCE(b.hata_mesaji,'')
                FROM bildirim_loglari b
                {whereSql}
                ORDER BY b.id DESC
                OFFSET @offset ROWS FETCH NEXT @take ROWS ONLY;
                """;

        await using (var cmd = new SqlCommand(listSql, connection))
        {
            cmd.Parameters.AddRange(prms.ToArray());
            cmd.Parameters.AddWithValue("@offset", offset);
            cmd.Parameters.AddWithValue("@take", safePageSize);
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var row = new AdminEmailQueueRowViewModel
                {
                    Id = reader.GetInt64(0),
                    UserId = reader.GetInt64(1),
                    RecipientEmail = reader.GetString(2),
                    Subject = reader.GetString(3),
                    Status = reader.GetString(4),
                    ProviderMessageId = reader.IsDBNull(5) ? null : reader.GetString(5),
                    AttemptCount = reader.GetInt32(6),
                    MaxAttempts = reader.GetInt32(7),
                    CreatedAtUtc = new DateTimeOffset(reader.GetDateTime(8), TimeSpan.Zero),
                    LastError = hasNextAttempt ? reader.GetString(10) : reader.GetString(9)
                };
                if (hasNextAttempt)
                {
                    row.NextAttemptUtc = reader.IsDBNull(9) ? null : new DateTimeOffset(reader.GetDateTime(9), TimeSpan.Zero);
                }
                model.Rows.Add(row);
            }
        }

        return model;
    }

    public async Task<AdminEmailSettingsPageViewModel> GetEmailSettingsPageAsync(string fullName, string email, string userRole, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var shell = await GetShellAsync(connection, "E-posta Hesapları", "Gönderici hesapları, kuyruk durumu ve form-şablon eşleşmelerini tek ekranda yönetin.", fullName, email, userRole, cancellationToken);
        var model = new AdminEmailSettingsPageViewModel { Shell = shell };

        const string accountSql = """
            SELECT
                servis_kodu,
                servis_adi,
                COALESCE(gonderen_ad, N''),
                COALESCE(gonderen_eposta, N''),
                yanitla_eposta,
                COALESCE(saglayici, N''),
                COALESCE(smtp_host, N''),
                COALESCE(smtp_port, 0),
                COALESCE(guvenlik_tipi, N''),
                COALESCE(aktif_mi, 0),
                COALESCE(varsayilan_mi, 0),
                COALESCE(test_modu, 0),
                son_basarili_test_tarihi,
                son_hata_tarihi,
                son_hata_mesaji
            FROM email_services
            ORDER BY varsayilan_mi DESC, aktif_mi DESC, id ASC;
            """;

        await using (var command = new SqlCommand(accountSql, connection))
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                var row = new AdminEmailAccountRowViewModel
                {
                    ServiceCode = reader.GetString(0),
                    ServiceName = reader.GetString(1),
                    SenderName = reader.GetString(2),
                    SenderEmail = reader.GetString(3),
                    ReplyToEmail = reader.IsDBNull(4) ? null : reader.GetString(4),
                    Provider = reader.GetString(5),
                    Host = reader.GetString(6),
                    Port = Convert.ToInt32(reader.GetValue(7), CultureInfo.InvariantCulture),
                    SecurityType = reader.GetString(8),
                    IsActive = SafeBool(reader, 9),
                    IsDefault = SafeBool(reader, 10),
                    TestMode = SafeBool(reader, 11),
                    LastSuccessUtc = reader.IsDBNull(12) ? null : new DateTimeOffset(reader.GetDateTime(12), TimeSpan.Zero),
                    LastErrorUtc = reader.IsDBNull(13) ? null : new DateTimeOffset(reader.GetDateTime(13), TimeSpan.Zero),
                    LastErrorMessage = reader.IsDBNull(14) ? null : reader.GetString(14)
                };
                model.Accounts.Add(row);
            }
        }

        var activeAccount = model.Accounts.FirstOrDefault(x => x.IsActive && x.IsDefault)
            ?? model.Accounts.FirstOrDefault(x => x.IsActive)
            ?? model.Accounts.FirstOrDefault();
        model.ActiveSenderEmail = activeAccount?.SenderEmail ?? string.Empty;
        model.ActiveServiceCode = activeAccount?.ServiceCode ?? string.Empty;

        const string templateSql = """
            SELECT
                COALESCE(sablon_kodu, N''),
                COALESCE(sablon_adi, N''),
                COALESCE(dil, N'tr'),
                COALESCE(konu, N''),
                COALESCE(icerik, N'')
            FROM bildirim_sablonlari
            WHERE tur = N'E-posta'
              AND COALESCE(aktif_mi, 1) = 1
            ORDER BY id ASC;
            """;

        await using (var templateCommand = new SqlCommand(templateSql, connection))
        await using (var templateReader = await templateCommand.ExecuteReaderAsync(cancellationToken))
        {
            while (await templateReader.ReadAsync(cancellationToken))
            {
                var templateCode = templateReader.GetString(0);
                var preferredSender = ResolvePreferredSenderEmail(templateCode);
                var matchingAccount = model.Accounts.FirstOrDefault(x => string.Equals(x.SenderEmail, preferredSender, StringComparison.OrdinalIgnoreCase));
                model.Templates.Add(new AdminEmailTemplateBindingRowViewModel
                {
                    TemplateCode = templateCode,
                    TemplateName = templateReader.GetString(1),
                    Language = templateReader.GetString(2),
                    Subject = NormalizeBrokenTurkish(templateReader.GetString(3)),
                    ViewPath = templateReader.GetString(4),
                    TriggerArea = ResolveTriggerArea(templateCode),
                    IntendedSenderEmail = preferredSender,
                    ActualSenderEmail = matchingAccount?.IsActive == true ? matchingAccount.SenderEmail : model.ActiveSenderEmail,
                    UsesFallbackSender = matchingAccount is null || !matchingAccount.IsActive
                });
            }
        }

        const string queueSql = """
            SELECT
                SUM(CASE WHEN durum = N'Beklemede' THEN 1 ELSE 0 END) AS beklemede,
                SUM(CASE WHEN durum IN (N'Gönderildi', N'SMTP Kabul', N'Dosyaya Yazıldı') THEN 1 ELSE 0 END) AS smtp_kabul,
                SUM(CASE WHEN durum = N'Başarısız' THEN 1 ELSE 0 END) AS basarisiz
            FROM bildirim_loglari
            WHERE tur = N'E-posta';
            """;
        await using (var queueCommand = new SqlCommand(queueSql, connection))
        await using (var queueReader = await queueCommand.ExecuteReaderAsync(cancellationToken))
        {
            if (await queueReader.ReadAsync(cancellationToken))
            {
                model.PendingCount = SafeInt(queueReader, 0);
                model.AcceptedCount = SafeInt(queueReader, 1);
                model.FailedCount = SafeInt(queueReader, 2);
            }
        }

        return model;
    }

    public async Task<AdminMailCenterPageViewModel> GetMailCenterAsync(string fullName, string email, string userRole, long? accountId, bool syncInbox, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var shell = await GetShellAsync(connection, "Mail Merkezi", "Platform e-posta hesaplarını, gelen kutusunu ve giden posta akışını tek ekranda yönetin.", fullName, email, userRole, cancellationToken);
        var model = new AdminMailCenterPageViewModel { Shell = shell };

        model.Accounts = await LoadMailAccountsAsync(connection, cancellationToken);
        model.ActiveSenderEmail = await LoadActiveSenderEmailAsync(connection, cancellationToken);
        model.SelectedAccountId = accountId ?? model.Accounts.FirstOrDefault()?.Id;

        if (syncInbox && model.SelectedAccountId.HasValue)
        {
            await SyncMailAccountInternalAsync(connection, model.SelectedAccountId.Value, cancellationToken);
            model.Accounts = await LoadMailAccountsAsync(connection, cancellationToken);
        }

        if (model.SelectedAccountId.HasValue)
        {
            var selected = model.Accounts.FirstOrDefault(x => x.Id == model.SelectedAccountId.Value);
            if (selected is not null)
            {
                model.Form = new AdminMailAccountForm
                {
                    Id = selected.Id,
                    AccountCode = selected.AccountCode,
                    AccountName = selected.AccountName,
                    EmailAddress = selected.EmailAddress,
                    IncomingProtocol = selected.IncomingProtocol,
                    IncomingHost = selected.IncomingHost,
                    IncomingPort = selected.IncomingPort,
                    IncomingUseSsl = selected.IncomingUseSsl,
                    OutgoingHost = selected.OutgoingHost,
                    OutgoingPort = selected.OutgoingPort,
                    OutgoingSecurityType = selected.OutgoingSecurityType,
                    Username = selected.EmailAddress,
                    IsActive = selected.IsActive,
                    IsDefaultSender = selected.IsDefaultSender
                };
            }
        }

        model.Incoming = await LoadIncomingEmailsAsync(connection, model.SelectedAccountId, cancellationToken);
        model.Outgoing = await LoadOutgoingEmailsAsync(connection, cancellationToken);
        model.TotalIncoming = model.Incoming.Count;
        model.TotalOutgoing = model.Outgoing.Count;
        return model;
    }

    public async Task<(bool Success, string Message)> SaveMailAccountAsync(long adminUserId, AdminMailAccountForm form, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(form.EmailAddress) || string.IsNullOrWhiteSpace(form.Username) || string.IsNullOrWhiteSpace(form.AccountCode))
        {
            return (false, "Hesap kodu, e-posta ve kullanıcı adı zorunludur.");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        var existingEncryptedPassword = form.Id.HasValue
            ? await GetStoredMailPasswordAsync(connection, form.Id.Value, cancellationToken)
            : null;
        var storedPassword = !string.IsNullOrWhiteSpace(form.Password)
            ? ProtectMailSecret(form.Password.Trim())
            : existingEncryptedPassword;

        if (string.IsNullOrWhiteSpace(storedPassword))
        {
            return (false, "Şifre zorunludur.");
        }

        if (form.Id.HasValue)
        {
            const string updateSql = """
                UPDATE dbo.platform_email_hesaplari
                SET hesap_kodu = @code,
                    hesap_adi = @name,
                    email_adresi = @email,
                    gelen_protokol = @protocol,
                    gelen_sunucu = @incomingHost,
                    gelen_port = @incomingPort,
                    gelen_ssl = @incomingSsl,
                    giden_sunucu = @outgoingHost,
                    giden_port = @outgoingPort,
                    giden_guvenlik_tipi = @outgoingSecurity,
                    kullanici_adi = @username,
                    sifre_sifreli = @password,
                    aktif_mi = @active,
                    varsayilan_gonderen_mi = @defaultSender,
                    guncellenme_tarihi = SYSUTCDATETIME()
                WHERE id = @id;
                """;
            await using var updateCmd = new SqlCommand(updateSql, connection);
            BindMailAccountParameters(updateCmd, form, storedPassword);
            updateCmd.Parameters.AddWithValue("@id", form.Id.Value);
            await updateCmd.ExecuteNonQueryAsync(cancellationToken);
        }
        else
        {
            const string insertSql = """
                INSERT INTO dbo.platform_email_hesaplari
                (
                    hesap_kodu, hesap_adi, email_adresi, gelen_protokol, gelen_sunucu, gelen_port, gelen_ssl,
                    giden_sunucu, giden_port, giden_guvenlik_tipi, kullanici_adi, sifre_sifreli, aktif_mi, varsayilan_gonderen_mi
                )
                VALUES
                (
                    @code, @name, @email, @protocol, @incomingHost, @incomingPort, @incomingSsl,
                    @outgoingHost, @outgoingPort, @outgoingSecurity, @username, @password, @active, @defaultSender
                );
                """;
            await using var insertCmd = new SqlCommand(insertSql, connection);
            BindMailAccountParameters(insertCmd, form, storedPassword);
            await insertCmd.ExecuteNonQueryAsync(cancellationToken);
        }

        if (form.IsDefaultSender)
        {
            await ClearOtherDefaultMailAccountsAsync(connection, form.Id, form.EmailAddress, cancellationToken);
        }

        await UpsertEmailServiceFromMailAccountAsync(connection, form, storedPassword, cancellationToken);

        await TryLogAdminActionAsync(connection, adminUserId, "mail_account_save", "platform_email_hesaplari", form.Id?.ToString(CultureInfo.InvariantCulture) ?? form.EmailAddress, $"{form.EmailAddress} kaydedildi.", cancellationToken);
        return (true, "Mail hesabı kaydedildi.");
    }

    public async Task<(bool Success, string Message)> DeleteMailAccountAsync(long adminUserId, long accountId, CancellationToken cancellationToken = default)
    {
        if (accountId <= 0)
        {
            return (false, "Geçersiz hesap.");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        const string deleteMessagesSql = "DELETE FROM dbo.platform_email_mesajlari WHERE hesap_id = @id;";
        await using (var deleteMessagesCmd = new SqlCommand(deleteMessagesSql, connection))
        {
            deleteMessagesCmd.Parameters.AddWithValue("@id", accountId);
            await deleteMessagesCmd.ExecuteNonQueryAsync(cancellationToken);
        }

        await DeleteEmailServiceByAccountIdAsync(connection, accountId, cancellationToken);

        const string deleteAccountSql = "DELETE FROM dbo.platform_email_hesaplari WHERE id = @id;";
        await using var deleteAccountCmd = new SqlCommand(deleteAccountSql, connection);
        deleteAccountCmd.Parameters.AddWithValue("@id", accountId);
        var affected = await deleteAccountCmd.ExecuteNonQueryAsync(cancellationToken);

        if (affected <= 0)
        {
            return (false, "Hesap bulunamadı.");
        }

        await TryLogAdminActionAsync(connection, adminUserId, "mail_account_delete", "platform_email_hesaplari", accountId.ToString(CultureInfo.InvariantCulture), "Mail hesabı silindi.", cancellationToken);
        return (true, "Mail hesabı silindi.");
    }

    public async Task<(bool Success, string Message, int ImportedCount)> SyncMailAccountAsync(long adminUserId, long accountId, CancellationToken cancellationToken = default)
    {
        if (accountId <= 0)
        {
            return (false, "Geçersiz hesap.", 0);
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        try
        {
            var imported = await SyncMailAccountInternalAsync(connection, accountId, cancellationToken);
            await TryLogAdminActionAsync(connection, adminUserId, "mail_account_sync", "platform_email_hesaplari", accountId.ToString(CultureInfo.InvariantCulture), $"İçe alınan mesaj: {imported}", cancellationToken);
            return (true, "Mail hesabı senkronize edildi.", imported);
        }
        catch (Exception ex)
        {
            await UpdateMailAccountSyncErrorAsync(connection, accountId, ex.Message, cancellationToken);
            return (false, $"Senkron başarısız: {ex.Message}", 0);
        }
    }

    public async Task<(bool Success, string Message, int QueuedCount)> QueueTemplateTestBatchAsync(long adminUserId, string recipientEmail, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(recipientEmail))
        {
            return (false, "Test alıcı e-posta zorunludur.", 0);
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        const string sql = """
            SELECT sablon_kodu, sablon_adi
            FROM bildirim_sablonlari
            WHERE tur = N'E-posta'
              AND aktif_mi = 1
            ORDER BY sablon_kodu ASC;
            """;

        var templateRows = new List<(string Code, string Name)>();
        await using (var command = new SqlCommand(sql, connection))
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                templateRows.Add((reader.GetString(0), reader.IsDBNull(1) ? reader.GetString(0) : reader.GetString(1)));
            }
        }

        if (templateRows.Count == 0)
        {
            return (false, "Aktif e-posta şablonu bulunamadı.", 0);
        }

        var queuedCount = 0;
        foreach (var template in templateRows)
        {
            await _emailQueueService.QueueTemplateAsync(connection, null, new Models.Email.QueuedEmailTemplateRequest
            {
                UserId = adminUserId,
                RecipientEmail = recipientEmail.Trim(),
                TemplateCode = template.Code,
                SubjectOverride = $"[TEST] {template.Name} ({template.Code})",
                ServiceCodeOverride = ResolvePreferredServiceCode(template.Code),
                SenderEmailOverride = ResolvePreferredSenderEmail(template.Code),
                RelatedTable = "users",
                RelatedRecordId = adminUserId,
                Tokens = BuildTemplateTestTokens(template.Code, recipientEmail)
            }, cancellationToken);
            queuedCount++;
        }

        await TryLogAdminActionAsync(connection, adminUserId, "mail_template_test_batch", "bildirim_sablonlari", recipientEmail.Trim(), $"{queuedCount} şablon test kuyruğuna bırakıldı.", cancellationToken);
        return (true, $"{queuedCount} şablon test kuyruğuna bırakıldı.", queuedCount);
    }

    private async Task<List<AdminMailAccountRowViewModel>> LoadMailAccountsAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                id, hesap_kodu, hesap_adi, email_adresi, gelen_protokol, gelen_sunucu, gelen_port, gelen_ssl,
                giden_sunucu, giden_port, giden_guvenlik_tipi, aktif_mi, varsayilan_gonderen_mi, son_senkron_tarihi, son_hata_mesaji
            FROM dbo.platform_email_hesaplari
            ORDER BY varsayilan_gonderen_mi DESC, email_adresi ASC;
            """;
        var rows = new List<AdminMailAccountRowViewModel>();
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(new AdminMailAccountRowViewModel
            {
                Id = reader.GetInt64(0),
                AccountCode = reader.GetString(1),
                AccountName = reader.GetString(2),
                EmailAddress = reader.GetString(3),
                IncomingProtocol = reader.GetString(4),
                IncomingHost = reader.GetString(5),
                IncomingPort = reader.GetInt32(6),
                IncomingUseSsl = reader.GetBoolean(7),
                OutgoingHost = reader.GetString(8),
                OutgoingPort = reader.GetInt32(9),
                OutgoingSecurityType = reader.GetString(10),
                IsActive = reader.GetBoolean(11),
                IsDefaultSender = reader.GetBoolean(12),
                LastSyncUtc = reader.IsDBNull(13) ? null : new DateTimeOffset(reader.GetDateTime(13), TimeSpan.Zero),
                LastError = reader.IsDBNull(14) ? null : reader.GetString(14)
            });
        }

        return rows;
    }

    private async Task<string> LoadActiveSenderEmailAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT TOP (1) COALESCE(gonderen_eposta, N'')
            FROM dbo.email_services
            WHERE aktif_mi = 1
            ORDER BY varsayilan_mi DESC, id ASC;
            """;
        await using var command = new SqlCommand(sql, connection);
        return Convert.ToString(await command.ExecuteScalarAsync(cancellationToken), CultureInfo.InvariantCulture) ?? string.Empty;
    }

    private async Task<List<AdminIncomingEmailRowViewModel>> LoadIncomingEmailsAsync(SqlConnection connection, long? accountId, CancellationToken cancellationToken)
    {
        var sql = """
            SELECT TOP (80)
                m.id, m.hesap_id, h.email_adresi, COALESCE(m.klasor, N'INBOX'), COALESCE(m.gonderen, N''),
                COALESCE(m.konu, N''), COALESCE(m.ozet, N''), m.internet_message_id, m.tarih_utc,
                COALESCE(m.okunmus_mu, 0), COALESCE(m.spam_mi, 0)
            FROM dbo.platform_email_mesajlari m
            INNER JOIN dbo.platform_email_hesaplari h ON h.id = m.hesap_id
            WHERE m.yon = N'Gelen'
              AND (@accountId IS NULL OR m.hesap_id = @accountId)
            ORDER BY COALESCE(m.tarih_utc, m.olusturulma_tarihi) DESC, m.id DESC;
            """;

        var rows = new List<AdminIncomingEmailRowViewModel>();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@accountId", (object?)accountId ?? DBNull.Value);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(new AdminIncomingEmailRowViewModel
            {
                Id = reader.GetInt64(0),
                AccountId = reader.GetInt64(1),
                AccountEmail = reader.GetString(2),
                FolderName = reader.GetString(3),
                From = reader.GetString(4),
                Subject = reader.GetString(5),
                Summary = reader.GetString(6),
                InternetMessageId = reader.IsDBNull(7) ? null : reader.GetString(7),
                ReceivedAtUtc = reader.IsDBNull(8) ? null : new DateTimeOffset(reader.GetDateTime(8), TimeSpan.Zero),
                IsRead = reader.GetBoolean(9),
                IsSpam = reader.GetBoolean(10)
            });
        }

        return rows;
    }

    private async Task<List<AdminOutgoingEmailRowViewModel>> LoadOutgoingEmailsAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        var hasSenderOverrideColumn = await TableColumnExistsAsync(connection, "dbo", "bildirim_loglari", "gonderen_eposta_override", cancellationToken);
        var sql = hasSenderOverrideColumn
            ? """
                SELECT TOP (80)
                    b.id,
                    b.kullanici_id,
                    COALESCE(b.alici_eposta, N''),
                    COALESCE(b.konu, N''),
                    COALESCE(b.durum, N''),
                    b.saglayici_mesaj_id,
                    b.gonderim_tarihi,
                    COALESCE(b.olusturulma_tarihi, SYSUTCDATETIME()),
                    COALESCE(b.gonderen_eposta_override, N'')
                FROM dbo.bildirim_loglari b
                WHERE b.tur = N'E-posta'
                ORDER BY COALESCE(b.gonderim_tarihi, b.olusturulma_tarihi) DESC, b.id DESC;
                """
            : """
                SELECT TOP (80)
                    b.id,
                    b.kullanici_id,
                    COALESCE(b.alici_eposta, N''),
                    COALESCE(b.konu, N''),
                    COALESCE(b.durum, N''),
                    b.saglayici_mesaj_id,
                    b.gonderim_tarihi,
                    COALESCE(b.olusturulma_tarihi, SYSUTCDATETIME()),
                    N''
                FROM dbo.bildirim_loglari b
                WHERE b.tur = N'E-posta'
                ORDER BY COALESCE(b.gonderim_tarihi, b.olusturulma_tarihi) DESC, b.id DESC;
                """;
        var senderEmail = await LoadActiveSenderEmailAsync(connection, cancellationToken);
        var rows = new List<AdminOutgoingEmailRowViewModel>();
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(new AdminOutgoingEmailRowViewModel
            {
                Id = reader.GetInt64(0),
                UserId = reader.IsDBNull(1) ? null : reader.GetInt64(1),
                RecipientEmail = reader.GetString(2),
                Subject = reader.GetString(3),
                Status = reader.GetString(4),
                ProviderMessageId = reader.IsDBNull(5) ? null : reader.GetString(5),
                SentAtUtc = reader.IsDBNull(6) ? null : new DateTimeOffset(reader.GetDateTime(6), TimeSpan.Zero),
                CreatedAtUtc = new DateTimeOffset(reader.GetDateTime(7), TimeSpan.Zero),
                SenderEmail = reader.IsDBNull(8) || string.IsNullOrWhiteSpace(reader.GetString(8)) ? senderEmail : reader.GetString(8)
            });
        }

        return rows;
    }

    private async Task<string?> GetStoredMailPasswordAsync(SqlConnection connection, long accountId, CancellationToken cancellationToken)
    {
        const string sql = "SELECT sifre_sifreli FROM dbo.platform_email_hesaplari WHERE id = @id;";
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@id", accountId);
        var scalar = await command.ExecuteScalarAsync(cancellationToken);
        return scalar as string;
    }

    private static void BindMailAccountParameters(SqlCommand command, AdminMailAccountForm form, string storedPassword)
    {
        command.Parameters.AddWithValue("@code", form.AccountCode.Trim());
        command.Parameters.AddWithValue("@name", form.AccountName.Trim());
        command.Parameters.AddWithValue("@email", form.EmailAddress.Trim());
        command.Parameters.AddWithValue("@protocol", form.IncomingProtocol.Trim().ToUpperInvariant());
        command.Parameters.AddWithValue("@incomingHost", NormalizeTransportHost(form.IncomingHost));
        command.Parameters.AddWithValue("@incomingPort", form.IncomingPort);
        command.Parameters.AddWithValue("@incomingSsl", form.IncomingUseSsl);
        command.Parameters.AddWithValue("@outgoingHost", NormalizeTransportHost(form.OutgoingHost));
        command.Parameters.AddWithValue("@outgoingPort", form.OutgoingPort);
        command.Parameters.AddWithValue("@outgoingSecurity", form.OutgoingSecurityType.Trim());
        command.Parameters.AddWithValue("@username", form.Username.Trim());
        command.Parameters.AddWithValue("@password", storedPassword);
        command.Parameters.AddWithValue("@active", form.IsActive);
        command.Parameters.AddWithValue("@defaultSender", form.IsDefaultSender);
    }

    private async Task ClearOtherDefaultMailAccountsAsync(SqlConnection connection, long? currentId, string currentEmail, CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE dbo.platform_email_hesaplari
            SET varsayilan_gonderen_mi = 0,
                guncellenme_tarihi = SYSUTCDATETIME()
            WHERE email_adresi <> @email
              AND (@id IS NULL OR id <> @id);
            """;
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@email", currentEmail);
        command.Parameters.AddWithValue("@id", (object?)currentId ?? DBNull.Value);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task UpsertEmailServiceFromMailAccountAsync(SqlConnection connection, AdminMailAccountForm form, string storedPassword, CancellationToken cancellationToken)
    {
        const string sql = """
            MERGE dbo.email_services AS target
            USING (
                SELECT
                    @serviceCode AS servis_kodu,
                    @serviceName AS servis_adi,
                    N'SMTP' AS saglayici,
                    @isDefault AS varsayilan_mi,
                    @isActive AS aktif_mi,
                    @senderName AS gonderen_ad,
                    @senderEmail AS gonderen_eposta,
                    @replyTo AS yanitla_eposta,
                    @smtpHost AS smtp_host,
                    @smtpPort AS smtp_port,
                    @smtpUsername AS smtp_kullanici_adi,
                    @smtpPassword AS smtp_sifre,
                    CAST(0 AS bit) AS sifre_sifrelenmis_mi,
                    @securityType AS guvenlik_tipi,
                    CAST(45 AS smallint) AS baglanti_zaman_asimi_saniye,
                    CAST(0 AS bit) AS test_modu,
                    @metadata AS metadata
            ) AS src
            ON target.servis_kodu = src.servis_kodu
            WHEN MATCHED THEN
                UPDATE SET
                    servis_adi = src.servis_adi,
                    saglayici = src.saglayici,
                    varsayilan_mi = src.varsayilan_mi,
                    aktif_mi = src.aktif_mi,
                    gonderen_ad = src.gonderen_ad,
                    gonderen_eposta = src.gonderen_eposta,
                    yanitla_eposta = src.yanitla_eposta,
                    smtp_host = src.smtp_host,
                    smtp_port = src.smtp_port,
                    smtp_kullanici_adi = src.smtp_kullanici_adi,
                    smtp_sifre = src.smtp_sifre,
                    sifre_sifrelenmis_mi = src.sifre_sifrelenmis_mi,
                    guvenlik_tipi = src.guvenlik_tipi,
                    baglanti_zaman_asimi_saniye = src.baglanti_zaman_asimi_saniye,
                    test_modu = src.test_modu,
                    metadata = src.metadata,
                    guncellenme_tarihi = SYSUTCDATETIME()
            WHEN NOT MATCHED THEN
                INSERT
                (
                    servis_kodu, servis_adi, saglayici, varsayilan_mi, aktif_mi, gonderen_ad, gonderen_eposta,
                    yanitla_eposta, smtp_host, smtp_port, smtp_kullanici_adi, smtp_sifre, sifre_sifrelenmis_mi,
                    guvenlik_tipi, baglanti_zaman_asimi_saniye, test_modu, metadata
                )
                VALUES
                (
                    src.servis_kodu, src.servis_adi, src.saglayici, src.varsayilan_mi, src.aktif_mi, src.gonderen_ad, src.gonderen_eposta,
                    src.yanitla_eposta, src.smtp_host, src.smtp_port, src.smtp_kullanici_adi, src.smtp_sifre, src.sifre_sifrelenmis_mi,
                    src.guvenlik_tipi, src.baglanti_zaman_asimi_saniye, src.test_modu, src.metadata
                );
            """;

        var metadata = JsonSerializer.Serialize(new
        {
            transport_mode = "smtp",
            incoming_protocol = form.IncomingProtocol.Trim().ToUpperInvariant(),
            incoming_host = NormalizeTransportHost(form.IncomingHost),
            incoming_port = form.IncomingPort,
            incoming_ssl = form.IncomingUseSsl
        });

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@serviceCode", ResolvePreferredServiceCodeByAccount(form.AccountCode));
        command.Parameters.AddWithValue("@serviceName", form.AccountName.Trim());
        command.Parameters.AddWithValue("@isDefault", form.IsDefaultSender);
        command.Parameters.AddWithValue("@isActive", form.IsActive);
        command.Parameters.AddWithValue("@senderName", form.AccountName.Trim());
        command.Parameters.AddWithValue("@senderEmail", form.EmailAddress.Trim());
        command.Parameters.AddWithValue("@replyTo", form.EmailAddress.Trim());
        command.Parameters.AddWithValue("@smtpHost", NormalizeTransportHost(form.OutgoingHost));
        command.Parameters.AddWithValue("@smtpPort", form.OutgoingPort);
        command.Parameters.AddWithValue("@smtpUsername", form.Username.Trim());
        command.Parameters.AddWithValue("@smtpPassword", UnprotectMailSecret(storedPassword));
        command.Parameters.AddWithValue("@securityType", form.OutgoingSecurityType.Trim());
        command.Parameters.AddWithValue("@metadata", metadata);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task DeleteEmailServiceByAccountIdAsync(SqlConnection connection, long accountId, CancellationToken cancellationToken)
    {
        const string sql = """
            DELETE s
            FROM dbo.email_services s
            INNER JOIN dbo.platform_email_hesaplari h ON LOWER(h.hesap_kodu) = LOWER(REPLACE(s.servis_kodu, N'platform_', N''))
            WHERE h.id = @id;
            """;
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@id", accountId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<bool> TableColumnExistsAsync(SqlConnection connection, string schemaName, string tableName, string columnName, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT COUNT(*)
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = @schemaName
              AND TABLE_NAME = @tableName
              AND COLUMN_NAME = @columnName;
            """;
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@schemaName", schemaName);
        command.Parameters.AddWithValue("@tableName", tableName);
        command.Parameters.AddWithValue("@columnName", columnName);
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken), CultureInfo.InvariantCulture) > 0;
    }

    private static string NormalizeTransportHost(string host)
    {
        var value = (host ?? string.Empty).Trim();
        if (value.Equals("mail.otelturizm.com", StringComparison.OrdinalIgnoreCase))
        {
            return "umay.muvhost.com";
        }

        return value;
    }

    private static string ResolvePreferredServiceCode(string templateCode)
    {
        var sender = ResolvePreferredSenderEmail(templateCode);
        return ResolvePreferredServiceCodeByAccount(sender.Split('@')[0]);
    }

    private static string ResolvePreferredServiceCodeByAccount(string accountCodeOrEmailPrefix)
    {
        var normalized = (accountCodeOrEmailPrefix ?? string.Empty).Trim().ToLowerInvariant();
        normalized = normalized.Replace("@otelturizm.com", string.Empty, StringComparison.OrdinalIgnoreCase);
        return $"platform_{normalized}";
    }

    private static Dictionary<string, string> BuildTemplateTestTokens(string templateCode, string recipientEmail)
    {
        var now = DateTimeOffset.Now;
        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["lang"] = "tr",
            ["user_first_name"] = "İrem",
            ["user_email"] = recipientEmail.Trim(),
            ["registration_date"] = now.ToString("dd.MM.yyyy HH:mm"),
            ["verification_link"] = "https://otelturizm.com/eposta-dogrula",
            ["verification_code"] = "654321",
            ["reset_link"] = "https://otelturizm.com/sifremi-unuttum",
            ["request_ip"] = "127.0.0.1",
            ["login_time"] = now.ToString("dd.MM.yyyy HH:mm"),
            ["hotel_name"] = "216 EAGLE PALACE",
            ["hotel_manager_name"] = "Kurumsal Yetkili",
            ["company_name"] = "Otelturizm Kurumsal",
            ["booking_reference"] = "OTL-TEST-20260428",
            ["booking_status"] = "Onay Bekliyor",
            ["reservation_status"] = "Onaylandı",
            ["check_in_date"] = now.AddDays(7).ToString("dd.MM.yyyy"),
            ["check_out_date"] = now.AddDays(9).ToString("dd.MM.yyyy"),
            ["guest_name"] = "İrem Test",
            ["guest_email"] = recipientEmail.Trim(),
            ["hotel_city"] = "İstanbul",
            ["total_price"] = "4.250 TL",
            ["tutar"] = "4.250",
            ["ad_soyad"] = "İrem Test",
            ["otel_adi"] = "216 EAGLE PALACE",
            ["rezervasyon_no"] = "OTL-TEST-20260428",
            ["message_subject"] = "Test bilgilendirme mesajı",
            ["message_body"] = "Bu içerik e-posta şablon testinde otomatik oluşturuldu.",
            ["rejection_reason"] = "Bu, canlı template doğrulama test mesajıdır.",
            ["contract_bundle_title"] = "Test sözleşme paketi",
            ["recipient_name"] = "İrem Test",
            ["module_label"] = "Admin platform",
            ["contract_sections_html"] = "<ul><li>Mesafeli satış sözleşmesi</li><li>KVKK aydınlatma metni</li><li>İptal ve iade koşulları</li></ul>",
            ["primary_contract_url"] = "https://otelturizm.com/gelisim",
            ["base_url"] = "https://otelturizm.com",
            ["checked_at"] = now.ToString("dd.MM.yyyy HH:mm:ss"),
            ["ok_count"] = "42",
            ["bad_count"] = "0",
            ["total_count"] = "42",
            ["bad_list"] = "-",
            ["price_drop_amount"] = "750 TL",
            ["discount_rate"] = "%12",
            ["hotel_link"] = "https://otelturizm.com/oteller/216-eagle-palace",
            ["favorite_link"] = "https://otelturizm.com/kullanici/favoriler",
            ["reservation_link"] = "https://otelturizm.com/kullanici/rezervasyonlarim",
            ["message_link"] = "https://otelturizm.com/kullanici/mesajlar"
        };
    }

    private async Task<PlatformMailAccountEntity?> LoadMailAccountEntityAsync(SqlConnection connection, long accountId, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT TOP (1)
                id, hesap_kodu, hesap_adi, email_adresi, gelen_protokol, gelen_sunucu, gelen_port, gelen_ssl,
                giden_sunucu, giden_port, giden_guvenlik_tipi, kullanici_adi, sifre_sifreli, aktif_mi
            FROM dbo.platform_email_hesaplari
            WHERE id = @id;
            """;
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@id", accountId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new PlatformMailAccountEntity
        {
            Id = reader.GetInt64(0),
            AccountCode = reader.GetString(1),
            AccountName = reader.GetString(2),
            EmailAddress = reader.GetString(3),
            IncomingProtocol = reader.GetString(4),
            IncomingHost = reader.GetString(5),
            IncomingPort = reader.GetInt32(6),
            IncomingUseSsl = reader.GetBoolean(7),
            OutgoingHost = reader.GetString(8),
            OutgoingPort = reader.GetInt32(9),
            OutgoingSecurityType = reader.GetString(10),
            Username = reader.GetString(11),
            StoredPassword = reader.GetString(12),
            IsActive = reader.GetBoolean(13)
        };
    }

    private async Task<int> SyncMailAccountInternalAsync(SqlConnection connection, long accountId, CancellationToken cancellationToken)
    {
        var account = await LoadMailAccountEntityAsync(connection, accountId, cancellationToken);
        if (account is null)
        {
            throw new InvalidOperationException("Mail hesabı bulunamadı.");
        }

        var password = UnprotectMailSecret(account.StoredPassword);
        var imported = account.IncomingProtocol.Equals("POP3", StringComparison.OrdinalIgnoreCase)
            ? await SyncPop3Async(connection, account, password, cancellationToken)
            : await SyncImapAsync(connection, account, password, cancellationToken);

        await UpdateMailAccountSyncSuccessAsync(connection, accountId, cancellationToken);
        return imported;
    }

    private async Task<int> SyncImapAsync(SqlConnection connection, PlatformMailAccountEntity account, string password, CancellationToken cancellationToken)
    {
        using var client = new ImapClient();
        await client.ConnectAsync(account.IncomingHost, account.IncomingPort, account.IncomingUseSsl, cancellationToken);
        await client.AuthenticateAsync(account.Username, password, cancellationToken);
        var inbox = client.Inbox ?? throw new InvalidOperationException("IMAP inbox bulunamadı.");
        await inbox.OpenAsync(FolderAccess.ReadOnly, cancellationToken);

        var uids = await inbox.SearchAsync(SearchQuery.All, cancellationToken) ?? Array.Empty<UniqueId>();
        var imported = 0;
        foreach (var uid in uids.TakeLast(40))
        {
            var message = await inbox.GetMessageAsync(uid, cancellationToken);
            if (await UpsertIncomingMessageAsync(connection, account, "INBOX", uid.ToString(), message, cancellationToken))
            {
                imported++;
            }
        }

        await client.DisconnectAsync(true, cancellationToken);
        return imported;
    }

    private async Task<int> SyncPop3Async(SqlConnection connection, PlatformMailAccountEntity account, string password, CancellationToken cancellationToken)
    {
        using var client = new Pop3Client();
        await client.ConnectAsync(account.IncomingHost, account.IncomingPort, account.IncomingUseSsl, cancellationToken);
        await client.AuthenticateAsync(account.Username, password, cancellationToken);

        var count = client.Count;
        var start = Math.Max(0, count - 40);
        var imported = 0;
        for (var i = start; i < count; i++)
        {
            var message = await client.GetMessageAsync(i, cancellationToken);
            if (await UpsertIncomingMessageAsync(connection, account, "INBOX", $"POP3-{i}", message, cancellationToken))
            {
                imported++;
            }
        }

        await client.DisconnectAsync(true, cancellationToken);
        return imported;
    }

    private async Task<bool> UpsertIncomingMessageAsync(SqlConnection connection, PlatformMailAccountEntity account, string folderName, string uidValue, MimeKit.MimeMessage message, CancellationToken cancellationToken)
    {
        const string existsSql = """
            SELECT TOP (1) id
            FROM dbo.platform_email_mesajlari
            WHERE hesap_id = @accountId
              AND yon = N'Gelen'
              AND klasor = @folder
              AND uid_degeri = @uid;
            """;
        await using var existsCommand = new SqlCommand(existsSql, connection);
        existsCommand.Parameters.AddWithValue("@accountId", account.Id);
        existsCommand.Parameters.AddWithValue("@folder", folderName);
        existsCommand.Parameters.AddWithValue("@uid", uidValue);
        var existingId = await existsCommand.ExecuteScalarAsync(cancellationToken);

        var from = string.Join("; ", message.From.Mailboxes.Select(x => $"{x.Name} <{x.Address}>".Trim()));
        var to = string.Join("; ", message.To.Mailboxes.Select(x => $"{x.Name} <{x.Address}>".Trim()));
        var cc = string.Join("; ", message.Cc.Mailboxes.Select(x => $"{x.Name} <{x.Address}>".Trim()));
        var summary = BuildMailSummary(message.TextBody, message.HtmlBody);
        var headers = string.Join(Environment.NewLine, message.Headers.Select(x => $"{x.Field}: {x.Value}"));

        if (existingId is not null && existingId != DBNull.Value)
        {
            const string updateSql = """
                UPDATE dbo.platform_email_mesajlari
                SET konu = @subject,
                    gonderen = @from,
                    alicilar = @to,
                    cc = @cc,
                    tarih_utc = @dateUtc,
                    ozet = @summary,
                    html_icerik = @htmlBody,
                    text_icerik = @textBody,
                    okunmus_mu = 1,
                    ham_basliklar = @headers,
                    guncellenme_tarihi = SYSUTCDATETIME(),
                    senkron_tarihi = SYSUTCDATETIME()
                WHERE id = @id;
                """;
            await using var updateCommand = new SqlCommand(updateSql, connection);
            updateCommand.Parameters.AddWithValue("@subject", (object?)message.Subject ?? DBNull.Value);
            updateCommand.Parameters.AddWithValue("@from", (object?)from ?? DBNull.Value);
            updateCommand.Parameters.AddWithValue("@to", (object?)to ?? DBNull.Value);
            updateCommand.Parameters.AddWithValue("@cc", (object?)cc ?? DBNull.Value);
            updateCommand.Parameters.AddWithValue("@dateUtc", message.Date != DateTimeOffset.MinValue ? message.Date.UtcDateTime : DBNull.Value);
            updateCommand.Parameters.AddWithValue("@summary", (object?)summary ?? DBNull.Value);
            updateCommand.Parameters.AddWithValue("@htmlBody", (object?)message.HtmlBody ?? DBNull.Value);
            updateCommand.Parameters.AddWithValue("@textBody", (object?)message.TextBody ?? DBNull.Value);
            updateCommand.Parameters.AddWithValue("@headers", headers);
            updateCommand.Parameters.AddWithValue("@id", Convert.ToInt64(existingId, CultureInfo.InvariantCulture));
            await updateCommand.ExecuteNonQueryAsync(cancellationToken);
            return false;
        }

        const string insertSql = """
            INSERT INTO dbo.platform_email_mesajlari
            (
                hesap_id, yon, klasor, uid_degeri, internet_message_id, konu, gonderen, alicilar, cc,
                tarih_utc, ozet, html_icerik, text_icerik, okunmus_mu, spam_mi, ham_basliklar
            )
            VALUES
            (
                @accountId, N'Gelen', @folder, @uid, @internetMessageId, @subject, @from, @to, @cc,
                @dateUtc, @summary, @htmlBody, @textBody, 1, 0, @headers
            );
            """;
        await using var insertCommand = new SqlCommand(insertSql, connection);
        insertCommand.Parameters.AddWithValue("@accountId", account.Id);
        insertCommand.Parameters.AddWithValue("@folder", folderName);
        insertCommand.Parameters.AddWithValue("@uid", uidValue);
        insertCommand.Parameters.AddWithValue("@internetMessageId", (object?)message.MessageId ?? DBNull.Value);
        insertCommand.Parameters.AddWithValue("@subject", (object?)message.Subject ?? DBNull.Value);
        insertCommand.Parameters.AddWithValue("@from", (object?)from ?? DBNull.Value);
        insertCommand.Parameters.AddWithValue("@to", (object?)to ?? DBNull.Value);
        insertCommand.Parameters.AddWithValue("@cc", (object?)cc ?? DBNull.Value);
        insertCommand.Parameters.AddWithValue("@dateUtc", message.Date != DateTimeOffset.MinValue ? message.Date.UtcDateTime : DBNull.Value);
        insertCommand.Parameters.AddWithValue("@summary", (object?)summary ?? DBNull.Value);
        insertCommand.Parameters.AddWithValue("@htmlBody", (object?)message.HtmlBody ?? DBNull.Value);
        insertCommand.Parameters.AddWithValue("@textBody", (object?)message.TextBody ?? DBNull.Value);
        insertCommand.Parameters.AddWithValue("@headers", headers);
        await insertCommand.ExecuteNonQueryAsync(cancellationToken);
        return true;
    }

    private string ProtectMailSecret(string rawSecret)
        => _mailAccountProtector.Protect(rawSecret);

    private string UnprotectMailSecret(string storedSecret)
    {
        try
        {
            return _mailAccountProtector.Unprotect(storedSecret);
        }
        catch
        {
            return storedSecret;
        }
    }

    private static string BuildMailSummary(string? textBody, string? htmlBody)
    {
        var text = !string.IsNullOrWhiteSpace(textBody) ? textBody : htmlBody ?? string.Empty;
        text = Regex.Replace(text, "<.*?>", " ");
        text = Regex.Replace(text, @"\s+", " ").Trim();
        return text.Length <= 600 ? text : text[..600];
    }

    private static async Task UpdateMailAccountSyncSuccessAsync(SqlConnection connection, long accountId, CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE dbo.platform_email_hesaplari
            SET son_senkron_tarihi = SYSUTCDATETIME(),
                son_hata_mesaji = NULL,
                guncellenme_tarihi = SYSUTCDATETIME()
            WHERE id = @id;
            """;
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@id", accountId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task UpdateMailAccountSyncErrorAsync(SqlConnection connection, long accountId, string errorMessage, CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE dbo.platform_email_hesaplari
            SET son_hata_mesaji = @error,
                guncellenme_tarihi = SYSUTCDATETIME()
            WHERE id = @id;
            """;
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@id", accountId);
        command.Parameters.AddWithValue("@error", errorMessage.Length <= 1000 ? errorMessage : errorMessage[..1000]);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task TryLogAdminActionAsync(SqlConnection connection, long adminUserId, string actionType, string targetTable, string? targetId, string? note, CancellationToken cancellationToken)
    {
        if (adminUserId <= 0)
        {
            return;
        }

        const string sql = """
            INSERT INTO dbo.admin_islem_loglari (admin_kullanici_id, islem_turu, hedef_tablo, hedef_kayit_id, aciklama, ip_adresi, islem_tarihi)
            VALUES (@adminUserId, @actionType, @targetTable, @targetId, @note, NULL, SYSUTCDATETIME());
            """;
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@adminUserId", adminUserId);
        command.Parameters.AddWithValue("@actionType", actionType);
        command.Parameters.AddWithValue("@targetTable", targetTable);
        command.Parameters.AddWithValue("@targetId", (object?)targetId ?? DBNull.Value);
        command.Parameters.AddWithValue("@note", (object?)note ?? DBNull.Value);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private sealed class PlatformMailAccountEntity
    {
        public long Id { get; init; }
        public string AccountCode { get; init; } = string.Empty;
        public string AccountName { get; init; } = string.Empty;
        public string EmailAddress { get; init; } = string.Empty;
        public string IncomingProtocol { get; init; } = "IMAP";
        public string IncomingHost { get; init; } = string.Empty;
        public int IncomingPort { get; init; }
        public bool IncomingUseSsl { get; init; }
        public string OutgoingHost { get; init; } = string.Empty;
        public int OutgoingPort { get; init; }
        public string OutgoingSecurityType { get; init; } = string.Empty;
        public string Username { get; init; } = string.Empty;
        public string StoredPassword { get; init; } = string.Empty;
        public bool IsActive { get; init; }
    }

    public async Task<(bool Success, string Message)> ForceRetryEmailAsync(long adminUserId, long queueId, string reason, CancellationToken cancellationToken = default)
    {
        if (queueId <= 0) return (false, "Geçersiz kuyruk kaydı.");
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var hasNextAttempt = await ColumnExistsAsync(connection, "dbo.bildirim_loglari", "sonraki_deneme_utc", cancellationToken);
        var sql = hasNextAttempt
            ? """
                UPDATE bildirim_loglari
                SET durum = N'Beklemede',
                    hata_mesaji = NULL,
                    hata_kodu = NULL,
                    sonraki_deneme_utc = NULL
                WHERE id = @id;
                """
            : """
                UPDATE bildirim_loglari
                SET durum = N'Beklemede',
                    hata_mesaji = NULL,
                    hata_kodu = NULL
                WHERE id = @id;
                """;

        await using (var cmd = new SqlCommand(sql, connection))
        {
            cmd.Parameters.AddWithValue("@id", queueId);
            var affected = await cmd.ExecuteNonQueryAsync(cancellationToken);
            if (affected <= 0) return (false, "Kayıt bulunamadı.");
        }

        return (true, "E-posta yeniden denemeye alındı.");
    }

    public async Task<(bool Success, string Message, int RetriedCount)> RetryAllFailedEmailsAsync(long adminUserId, string reason, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var hasNextAttempt = await ColumnExistsAsync(connection, "dbo.bildirim_loglari", "sonraki_deneme_utc", cancellationToken);
        var sql = hasNextAttempt
            ? """
                UPDATE bildirim_loglari
                SET durum = N'Beklemede',
                    hata_mesaji = NULL,
                    hata_kodu = NULL,
                    sonraki_deneme_utc = NULL
                WHERE tur = N'E-posta'
                  AND durum = N'Başarısız';
                """
            : """
                UPDATE bildirim_loglari
                SET durum = N'Beklemede',
                    hata_mesaji = NULL,
                    hata_kodu = NULL
                WHERE tur = N'E-posta'
                  AND durum = N'Başarısız';
                """;

        await using var command = new SqlCommand(sql, connection);
        var affected = await command.ExecuteNonQueryAsync(cancellationToken);
        return (true, affected > 0 ? $"{affected} başarısız e-posta yeniden kuyruğa alındı." : "Yeniden denenecek başarısız e-posta bulunamadı.", affected);
    }

    public async Task<(bool Success, string Message)> MarkEmailFailedAsync(long adminUserId, long queueId, string reason, CancellationToken cancellationToken = default)
    {
        if (queueId <= 0) return (false, "Geçersiz kuyruk kaydı.");
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using (var cmd = new SqlCommand("""
            UPDATE bildirim_loglari
            SET durum = N'Başarısız'
            WHERE id = @id;
            """, connection))
        {
            cmd.Parameters.AddWithValue("@id", queueId);
            var affected = await cmd.ExecuteNonQueryAsync(cancellationToken);
            if (affected <= 0) return (false, "Kayıt bulunamadı.");
        }

        return (true, "E-posta başarısız olarak işaretlendi.");
    }

    public async Task<AdminUnifiedReservationsPageViewModel> GetUnifiedReservationsAsync(string fullName, string email, string userRole, string? q, string? status, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var shell = await GetShellAsync(connection, "Rezervasyonlar (Tek Liste)", "Tüm rezervasyonları tek ekranda takip edin.", fullName, email, userRole, cancellationToken);
        var model = new AdminUnifiedReservationsPageViewModel
        {
            Shell = shell,
            Query = q,
            Status = status,
            Page = Math.Max(1, page <= 0 ? 1 : page),
            PageSize = Math.Clamp(pageSize <= 0 ? 50 : pageSize, 10, 200)
        };

        var where = new List<string>();
        var prms = new List<SqlParameter>();
        if (!string.IsNullOrWhiteSpace(model.Status))
        {
            where.Add("LOWER(r.durum) = LOWER(@status)");
            prms.Add(new SqlParameter("@status", model.Status.Trim()));
        }
        if (!string.IsNullOrWhiteSpace(model.Query))
        {
            where.Add("(o.otel_adi LIKE @q OR u.ad_soyad LIKE @q OR u.eposta LIKE @q OR f.firma_adi LIKE @q)");
            prms.Add(new SqlParameter("@q", "%" + model.Query.Trim() + "%"));
        }
        var whereSql = where.Count == 0 ? string.Empty : "WHERE " + string.Join(" AND ", where);

        var countSql = $"""
            SELECT COUNT(*)
            FROM rezervasyonlar r
            LEFT JOIN oteller o ON o.id = r.otel_id
            LEFT JOIN users u ON u.id = r.kullanici_id
            LEFT JOIN firmalar f ON f.id = r.firma_id
            {whereSql};
            """;
        await using (var countCmd = new SqlCommand(countSql, connection))
        {
            countCmd.Parameters.AddRange(prms.ToArray());
            model.Total = Convert.ToInt32(await countCmd.ExecuteScalarAsync(cancellationToken) ?? 0, CultureInfo.InvariantCulture);
        }

        var offset = (model.Page - 1) * model.PageSize;
        var listSql = $"""
            SELECT r.id,
                   COALESCE(o.otel_adi,'-') AS otel_adi,
                   COALESCE(u.ad_soyad,'') AS musteri_adi,
                   COALESCE(u.eposta,'') AS musteri_eposta,
                   COALESCE(f.firma_adi,'') AS firma_adi,
                   COALESCE(r.durum,'') AS durum,
                   COALESCE(r.toplam_tutar,0) AS toplam_tutar,
                   COALESCE(r.para_birimi,'TRY') AS para_birimi,
                   COALESCE(r.olusturulma_tarihi, SYSUTCDATETIME()) AS created_at,
                   COALESCE(r.komisyon_tutari, 0) AS komisyon_tutari,
                   CASE
                       WHEN r.firma_id IS NOT NULL THEN N'Firma'
                       WHEN COALESCE(r.satis_temsilcisi_id, 0) > 0 THEN N'Satış'
                       ELSE N'Bireysel'
                   END AS kaynak
            FROM rezervasyonlar r
            LEFT JOIN oteller o ON o.id = r.otel_id
            LEFT JOIN users u ON u.id = r.kullanici_id
            LEFT JOIN firmalar f ON f.id = r.firma_id
            {whereSql}
            ORDER BY r.id DESC
            OFFSET @offset ROWS FETCH NEXT @take ROWS ONLY;
            """;

        await using (var cmd = new SqlCommand(listSql, connection))
        {
            cmd.Parameters.AddRange(prms.ToArray());
            cmd.Parameters.AddWithValue("@offset", offset);
            cmd.Parameters.AddWithValue("@take", model.PageSize);
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                model.Rows.Add(new AdminUnifiedReservationRowViewModel
                {
                    ReservationId = reader.GetInt64(0),
                    HotelName = reader.GetString(1),
                    CustomerName = reader.GetString(2),
                    CustomerEmail = reader.GetString(3),
                    CompanyName = reader.GetString(4),
                    Status = reader.GetString(5),
                    TotalAmount = reader.GetDecimal(6),
                    Currency = reader.GetString(7),
                    CreatedAtUtc = new DateTimeOffset(reader.GetDateTime(8), TimeSpan.Zero),
                    CommissionAmount = reader.GetDecimal(9),
                    SourceText = reader.GetString(10)
                });
            }
        }

        return model;
    }

    public async Task<AdminPaymentsPageViewModel> GetPaymentsAsync(string fullName, string email, string userRole, string? q, string? status, string? paymentType, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var shell = await GetShellAsync(connection, "Ödemeler", "Tahsilat, iade, risk ve manuel onay gerektiren ödeme kayıtlarını yönetin.", fullName, email, userRole, cancellationToken);
        var model = new AdminPaymentsPageViewModel
        {
            Shell = shell,
            Query = q,
            Status = status,
            PaymentType = paymentType,
            Page = Math.Max(1, page <= 0 ? 1 : page),
            PageSize = Math.Clamp(pageSize <= 0 ? 25 : pageSize, 10, 200)
        };

        if (!await TableExistsAsync(connection, "odeme_islemleri", cancellationToken))
        {
            return model;
        }

        model.SummaryCards.AddRange(await LoadAdminPaymentSummaryAsync(connection, cancellationToken));
        model.StatusOptions.AddRange(await LoadDistinctAdminOptionAsync(connection, "odeme_islemleri", "odeme_durumu", cancellationToken));
        model.TypeOptions.AddRange(await LoadDistinctAdminOptionAsync(connection, "odeme_islemleri", "odeme_turu", cancellationToken));

        var where = new List<string>();
        if (!string.IsNullOrWhiteSpace(model.Status)) where.Add("LOWER(p.odeme_durumu) = LOWER(@status)");
        if (!string.IsNullOrWhiteSpace(model.PaymentType)) where.Add("LOWER(p.odeme_turu) = LOWER(@paymentType)");
        if (!string.IsNullOrWhiteSpace(model.Query))
        {
            where.Add("(p.islem_no LIKE @q OR p.saglayici_islem_no LIKE @q OR r.rezervasyon_no LIKE @q OR o.otel_adi LIKE @q OR u.ad_soyad LIKE @q OR u.eposta LIKE @q)");
        }

        var whereSql = where.Count == 0 ? string.Empty : "WHERE " + string.Join(" AND ", where);
        await using (var countCmd = new SqlCommand($"""
            SELECT COUNT(*)
            FROM odeme_islemleri p
            LEFT JOIN rezervasyonlar r ON r.id = p.rezervasyon_id
            LEFT JOIN oteller o ON o.id = p.otel_id
            LEFT JOIN users u ON u.id = p.kullanici_id
            {whereSql};
            """, connection))
        {
            BindPaymentFilters(countCmd, model);
            model.Total = Convert.ToInt32(await countCmd.ExecuteScalarAsync(cancellationToken) ?? 0, CultureInfo.InvariantCulture);
        }

        await using var cmd = new SqlCommand($"""
            SELECT p.id,
                   COALESCE(p.islem_no, N'-'),
                   COALESCE(r.rezervasyon_no, N'-'),
                   COALESCE(o.otel_adi, N'-'),
                   COALESCE(u.ad_soyad, N'-'),
                   COALESCE(p.odeme_turu, N'-'),
                   COALESCE(p.odeme_yontemi, N'-'),
                   COALESCE(p.odeme_durumu, N'-'),
                   COALESCE(p.tutar, 0),
                   COALESCE(p.komisyon_tutari, 0),
                   COALESCE(p.vergi_tutari, 0),
                   COALESCE(p.toplam_tahsilat, 0),
                   COALESCE(p.para_birimi, N'TRY'),
                   COALESCE(p.odeme_saglayici, N'-'),
                   COALESCE(p.risk_puani, 0),
                   COALESCE(p.manuel_onay_gerektirir, 0),
                   p.odeme_baslangic_tarihi,
                   p.odeme_tamamlanma_tarihi
            FROM odeme_islemleri p
            LEFT JOIN rezervasyonlar r ON r.id = p.rezervasyon_id
            LEFT JOIN oteller o ON o.id = p.otel_id
            LEFT JOIN users u ON u.id = p.kullanici_id
            {whereSql}
            ORDER BY p.id DESC
            OFFSET @offset ROWS FETCH NEXT @take ROWS ONLY;
            """, connection);
        BindPaymentFilters(cmd, model);
        cmd.Parameters.AddWithValue("@offset", (model.Page - 1) * model.PageSize);
        cmd.Parameters.AddWithValue("@take", model.PageSize);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            model.Rows.Add(new AdminPaymentRowViewModel
            {
                PaymentId = reader.GetInt64(0),
                TransactionNo = reader.GetString(1),
                ReservationNo = reader.GetString(2),
                HotelName = reader.GetString(3),
                CustomerName = reader.GetString(4),
                PaymentType = reader.GetString(5),
                PaymentMethod = reader.GetString(6),
                Status = reader.GetString(7),
                Amount = SafeDecimal(reader, 8),
                CommissionAmount = SafeDecimal(reader, 9),
                TaxAmount = SafeDecimal(reader, 10),
                TotalCollected = SafeDecimal(reader, 11),
                Currency = reader.GetString(12),
                Provider = reader.GetString(13),
                RiskScore = SafeInt(reader, 14),
                ManualApprovalRequired = SafeBool(reader, 15),
                StartedAtUtc = reader.IsDBNull(16) ? null : new DateTimeOffset(reader.GetDateTime(16), TimeSpan.Zero),
                CompletedAtUtc = reader.IsDBNull(17) ? null : new DateTimeOffset(reader.GetDateTime(17), TimeSpan.Zero)
            });
        }

        return model;
    }

    public async Task<AdminInvoicesPageViewModel> GetInvoicesAsync(string fullName, string email, string userRole, string? q, string? status, string? invoiceType, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var shell = await GetShellAsync(connection, "Faturalar", "Partner, firma ve kullanıcı konaklama faturalarını takip edin.", fullName, email, userRole, cancellationToken);
        var model = new AdminInvoicesPageViewModel
        {
            Shell = shell,
            Query = q,
            Status = status,
            InvoiceType = invoiceType,
            Page = Math.Max(1, page <= 0 ? 1 : page),
            PageSize = Math.Clamp(pageSize <= 0 ? 25 : pageSize, 10, 200)
        };

        if (!await TableExistsAsync(connection, "faturalar", cancellationToken))
        {
            return model;
        }

        model.SummaryCards.AddRange(await LoadAdminInvoiceSummaryAsync(connection, cancellationToken));
        model.StatusOptions.AddRange(await LoadDistinctAdminOptionAsync(connection, "faturalar", "fatura_durumu", cancellationToken));
        model.TypeOptions.AddRange(await LoadDistinctAdminOptionAsync(connection, "faturalar", "fatura_turu", cancellationToken));

        var where = new List<string>();
        if (!string.IsNullOrWhiteSpace(model.Status)) where.Add("LOWER(f.fatura_durumu) = LOWER(@status)");
        if (!string.IsNullOrWhiteSpace(model.InvoiceType)) where.Add("LOWER(f.fatura_turu) = LOWER(@invoiceType)");
        if (!string.IsNullOrWhiteSpace(model.Query))
        {
            where.Add("(f.fatura_no LIKE @q OR f.fatura_alici_unvan LIKE @q OR f.fatura_alici_eposta LIKE @q OR o.otel_adi LIKE @q OR r.rezervasyon_no LIKE @q)");
        }

        var whereSql = where.Count == 0 ? string.Empty : "WHERE " + string.Join(" AND ", where);
        await using (var countCmd = new SqlCommand($"""
            SELECT COUNT(*)
            FROM faturalar f
            LEFT JOIN oteller o ON o.id = f.otel_id
            LEFT JOIN rezervasyonlar r ON r.id = f.rezervasyon_id
            {whereSql};
            """, connection))
        {
            BindInvoiceFilters(countCmd, model);
            model.Total = Convert.ToInt32(await countCmd.ExecuteScalarAsync(cancellationToken) ?? 0, CultureInfo.InvariantCulture);
        }

        await using var cmd = new SqlCommand($"""
            SELECT f.id,
                   COALESCE(f.fatura_no, N'-'),
                   COALESCE(f.fatura_turu, N'-'),
                   COALESCE(o.otel_adi, N'-'),
                   COALESCE(f.fatura_alici_unvan, u.ad_soyad, firma.firma_adi, N'-'),
                   COALESCE(f.fatura_alici_eposta, u.eposta, N''),
                   COALESCE(f.fatura_durumu, N'Taslak'),
                   COALESCE(f.e_fatura_durumu, N'-'),
                   COALESCE(f.ara_toplam, 0),
                   COALESCE(f.kdv_tutari, 0),
                   COALESCE(f.konaklama_vergisi_tutari, 0),
                   COALESCE(f.genel_toplam, 0),
                   COALESCE(f.para_birimi, N'TRY'),
                   f.fatura_tarihi,
                   f.vade_tarihi,
                   f.odeme_tarihi,
                   COALESCE(f.fatura_pdf_yolu, N'')
            FROM faturalar f
            LEFT JOIN oteller o ON o.id = f.otel_id
            LEFT JOIN rezervasyonlar r ON r.id = f.rezervasyon_id
            LEFT JOIN users u ON u.id = f.kullanici_id
            LEFT JOIN firmalar firma ON firma.id = f.firma_id
            {whereSql}
            ORDER BY COALESCE(f.fatura_tarihi, CAST(f.olusturulma_tarihi AS date)) DESC, f.id DESC
            OFFSET @offset ROWS FETCH NEXT @take ROWS ONLY;
            """, connection);
        BindInvoiceFilters(cmd, model);
        cmd.Parameters.AddWithValue("@offset", (model.Page - 1) * model.PageSize);
        cmd.Parameters.AddWithValue("@take", model.PageSize);
        await using var invoiceReader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await invoiceReader.ReadAsync(cancellationToken))
        {
            model.Rows.Add(new AdminInvoiceRowViewModel
            {
                InvoiceId = invoiceReader.GetInt64(0),
                InvoiceNo = invoiceReader.GetString(1),
                InvoiceType = invoiceReader.GetString(2),
                HotelName = invoiceReader.GetString(3),
                BuyerTitle = invoiceReader.GetString(4),
                BuyerEmail = invoiceReader.GetString(5),
                Status = invoiceReader.GetString(6),
                EInvoiceStatus = invoiceReader.GetString(7),
                SubTotal = SafeDecimal(invoiceReader, 8),
                TaxAmount = SafeDecimal(invoiceReader, 9),
                AccommodationTaxAmount = SafeDecimal(invoiceReader, 10),
                GrandTotal = SafeDecimal(invoiceReader, 11),
                Currency = invoiceReader.GetString(12),
                InvoiceDate = invoiceReader.IsDBNull(13) ? null : invoiceReader.GetDateTime(13),
                DueDate = invoiceReader.IsDBNull(14) ? null : invoiceReader.GetDateTime(14),
                PaymentDate = invoiceReader.IsDBNull(15) ? null : invoiceReader.GetDateTime(15),
                PdfPath = invoiceReader.GetString(16)
            });
        }

        return model;
    }

    private static void BindPaymentFilters(SqlCommand command, AdminPaymentsPageViewModel model)
    {
        if (!string.IsNullOrWhiteSpace(model.Query)) command.Parameters.AddWithValue("@q", "%" + model.Query.Trim() + "%");
        if (!string.IsNullOrWhiteSpace(model.Status)) command.Parameters.AddWithValue("@status", model.Status.Trim());
        if (!string.IsNullOrWhiteSpace(model.PaymentType)) command.Parameters.AddWithValue("@paymentType", model.PaymentType.Trim());
    }

    private static void BindInvoiceFilters(SqlCommand command, AdminInvoicesPageViewModel model)
    {
        if (!string.IsNullOrWhiteSpace(model.Query)) command.Parameters.AddWithValue("@q", "%" + model.Query.Trim() + "%");
        if (!string.IsNullOrWhiteSpace(model.Status)) command.Parameters.AddWithValue("@status", model.Status.Trim());
        if (!string.IsNullOrWhiteSpace(model.InvoiceType)) command.Parameters.AddWithValue("@invoiceType", model.InvoiceType.Trim());
    }

    private static async Task<List<AdminSummaryCardViewModel>> LoadAdminPaymentSummaryAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        var cards = new List<AdminSummaryCardViewModel>();
        var definitions = new[]
        {
            ("Toplam İşlem", "SELECT COUNT(*) FROM odeme_islemleri", "Tüm ödeme hareketleri", "info", "fa-credit-card"),
            ("Başarılı", "SELECT COUNT(*) FROM odeme_islemleri WHERE odeme_durumu = N'Başarılı'", "Tamamlanan tahsilatlar", "success", "fa-circle-check"),
            ("Riskli", "SELECT COUNT(*) FROM odeme_islemleri WHERE COALESCE(risk_puani,0) >= 70 OR COALESCE(manuel_onay_gerektirir,0)=1", "Risk/manuel onay bekleyenler", "warning", "fa-shield-halved"),
            ("İade Edilen", "SELECT COALESCE(SUM(COALESCE(iade_edilen_tutar,0)),0) FROM odeme_islemleri", "Toplam iade tutarı", "danger", "fa-rotate-left")
        };

        foreach (var item in definitions)
        {
            await using var cmd = new SqlCommand(item.Item2, connection);
            cards.Add(new AdminSummaryCardViewModel
            {
                Label = item.Item1,
                Value = FormatScalar(await cmd.ExecuteScalarAsync(cancellationToken)),
                Description = item.Item3,
                ToneClass = item.Item4,
                IconClass = item.Item5
            });
        }

        return cards;
    }

    private static async Task<List<AdminSummaryCardViewModel>> LoadAdminInvoiceSummaryAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        var cards = new List<AdminSummaryCardViewModel>();
        var definitions = new[]
        {
            ("Toplam Fatura", "SELECT COUNT(*) FROM faturalar", "Sistemdeki tüm fatura kayıtları", "info", "fa-file-invoice"),
            ("Kesildi", "SELECT COUNT(*) FROM faturalar WHERE fatura_durumu = N'Kesildi'", "Aktif kesilmiş faturalar", "success", "fa-file-circle-check"),
            ("Bekleyen", "SELECT COUNT(*) FROM faturalar WHERE COALESCE(fatura_durumu,N'Taslak') IN (N'Taslak',N'Beklemede')", "Hazırlık/onay bekleyenler", "warning", "fa-file-pen"),
            ("Ciro", "SELECT COALESCE(SUM(COALESCE(genel_toplam,0)),0) FROM faturalar WHERE COALESCE(fatura_durumu,N'') <> N'İptal Edildi'", "İptal dışı toplam", "success", "fa-chart-line")
        };

        foreach (var item in definitions)
        {
            await using var cmd = new SqlCommand(item.Item2, connection);
            cards.Add(new AdminSummaryCardViewModel
            {
                Label = item.Item1,
                Value = FormatScalar(await cmd.ExecuteScalarAsync(cancellationToken)),
                Description = item.Item3,
                ToneClass = item.Item4,
                IconClass = item.Item5
            });
        }

        return cards;
    }

    private static async Task<List<string>> LoadDistinctAdminOptionAsync(SqlConnection connection, string tableName, string columnName, CancellationToken cancellationToken)
    {
        var result = new List<string>();
        var safeTable = tableName.Replace("]", string.Empty, StringComparison.Ordinal);
        var safeColumn = columnName.Replace("]", string.Empty, StringComparison.Ordinal);
        await using var cmd = new SqlCommand($"""
            SELECT DISTINCT TOP (50) COALESCE(NULLIF([{safeColumn}], N''), N'')
            FROM [{safeTable}]
            WHERE COALESCE(NULLIF([{safeColumn}], N''), N'') <> N''
            ORDER BY 1;
            """, connection);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(reader.GetString(0));
        }

        return result;
    }

    public async Task<AdminReportsPageViewModel> GetReportsAsync(string fullName, string email, string userRole, long? hotelId, DateTime? dateFrom, DateTime? dateTo, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var firstDay = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(-11);
        var model = new AdminReportsPageViewModel
        {
            Shell = await GetShellAsync(connection, "Gelir / Komisyon Raporu", "Otel bazlı aylık ciro, komisyon, vergi ve iptal etkisini izleyin.", fullName, email, userRole, cancellationToken),
            HotelId = hotelId is > 0 ? hotelId : null,
            DateFrom = dateFrom?.Date ?? firstDay,
            DateTo = dateTo?.Date ?? DateTime.Today,
            Page = Math.Max(1, page <= 0 ? 1 : page),
            PageSize = Math.Clamp(pageSize <= 0 ? 25 : pageSize, 10, 200)
        };

        model.HotelOptions.AddRange(await LoadReportHotelOptionsAsync(connection, cancellationToken));
        model.SummaryCards.AddRange(await LoadAdminReportSummaryAsync(connection, model, cancellationToken));

        const string groupSql = """
            FROM
            (
                SELECT
                    DATEFROMPARTS(YEAR(r.giris_tarihi), MONTH(r.giris_tarihi), 1) AS report_month,
                    o.id AS hotel_id,
                    COALESCE(o.otel_adi, N'-') AS hotel_name,
                    CONCAT(COALESCE(o.ilce, N'-'), N', ', COALESCE(o.sehir, N'-')) AS city_label,
                    COUNT(*) AS reservation_count,
                    SUM(CASE WHEN COALESCE(r.durum, N'') IN (N'Tamamlandı', N'Giriş Yaptı', N'Onaylandı') THEN 1 ELSE 0 END) AS completed_count,
                    SUM(CASE WHEN COALESCE(r.durum, N'') LIKE N'%İptal%' OR COALESCE(r.durum, N'') LIKE N'%Iptal%' THEN 1 ELSE 0 END) AS cancelled_count,
                    COALESCE(SUM(CASE WHEN COALESCE(r.durum, N'') NOT LIKE N'%İptal%' AND COALESCE(r.durum, N'') NOT LIKE N'%Iptal%' THEN COALESCE(r.toplam_tutar,0) ELSE 0 END),0) AS gross_revenue,
                    COALESCE(SUM(CASE WHEN COALESCE(r.durum, N'') NOT LIKE N'%İptal%' AND COALESCE(r.durum, N'') NOT LIKE N'%Iptal%' THEN COALESCE(r.komisyon_tutari,0) ELSE 0 END),0) AS gross_commission,
                    COALESCE(SUM(CASE WHEN COALESCE(r.durum, N'') NOT LIKE N'%İptal%' AND COALESCE(r.durum, N'') NOT LIKE N'%Iptal%' THEN COALESCE(r.platform_net_komisyon_tutari,0) ELSE 0 END),0) AS net_commission,
                    COALESCE(SUM(COALESCE(r.konaklama_vergisi_tutari,0)),0) AS accommodation_tax,
                    COALESCE(SUM(COALESCE(r.kdv_tutari,0)),0) AS kdv_amount
                FROM rezervasyonlar r
                INNER JOIN oteller o ON o.id = r.otel_id
                WHERE r.giris_tarihi >= @fromDate
                  AND r.giris_tarihi <= @toDate
                  AND (@hotelId IS NULL OR o.id = @hotelId)
                GROUP BY DATEFROMPARTS(YEAR(r.giris_tarihi), MONTH(r.giris_tarihi), 1), o.id, o.otel_adi, o.ilce, o.sehir
            ) grouped
            """;

        await using (var countCmd = new SqlCommand($"SELECT COUNT(*) {groupSql};", connection))
        {
            BindReportFilters(countCmd, model);
            model.Total = Convert.ToInt32(await countCmd.ExecuteScalarAsync(cancellationToken) ?? 0, CultureInfo.InvariantCulture);
        }

        await using var cmd = new SqlCommand($"""
            SELECT report_month, hotel_id, hotel_name, city_label, reservation_count, completed_count, cancelled_count,
                   gross_revenue, gross_commission, net_commission, accommodation_tax, kdv_amount
            {groupSql}
            ORDER BY report_month DESC, gross_revenue DESC
            OFFSET @offset ROWS FETCH NEXT @take ROWS ONLY;
            """, connection);
        BindReportFilters(cmd, model);
        cmd.Parameters.AddWithValue("@offset", (model.Page - 1) * model.PageSize);
        cmd.Parameters.AddWithValue("@take", model.PageSize);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            model.Rows.Add(new AdminRevenueReportRowViewModel
            {
                MonthText = reader.GetDateTime(0).ToString("yyyy-MM", CultureInfo.InvariantCulture),
                HotelId = reader.GetInt64(1),
                HotelName = reader.GetString(2),
                CityLabel = reader.GetString(3),
                ReservationCount = SafeInt(reader, 4),
                CompletedCount = SafeInt(reader, 5),
                CancelledCount = SafeInt(reader, 6),
                GrossRevenue = SafeDecimal(reader, 7),
                GrossCommission = SafeDecimal(reader, 8),
                NetCommission = SafeDecimal(reader, 9),
                AccommodationTax = SafeDecimal(reader, 10),
                KdvAmount = SafeDecimal(reader, 11)
            });
        }

        return model;
    }

    private static void BindReportFilters(SqlCommand command, AdminReportsPageViewModel model)
    {
        command.Parameters.AddWithValue("@fromDate", model.DateFrom?.Date ?? DateTime.Today.AddMonths(-12));
        command.Parameters.AddWithValue("@toDate", model.DateTo?.Date ?? DateTime.Today);
        command.Parameters.AddWithValue("@hotelId", model.HotelId.HasValue ? model.HotelId.Value : DBNull.Value);
    }

    private static async Task<List<AdminReportHotelOptionViewModel>> LoadReportHotelOptionsAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        var options = new List<AdminReportHotelOptionViewModel>();
        const string sql = "SELECT TOP (500) id, otel_adi FROM oteller ORDER BY otel_adi;";
        await using var cmd = new SqlCommand(sql, connection);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            options.Add(new AdminReportHotelOptionViewModel { HotelId = reader.GetInt64(0), HotelName = reader.GetString(1) });
        }

        return options;
    }

    private static async Task<List<AdminSummaryCardViewModel>> LoadAdminReportSummaryAsync(SqlConnection connection, AdminReportsPageViewModel model, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                COUNT(*) AS reservation_count,
                COALESCE(SUM(CASE WHEN COALESCE(durum,N'') NOT LIKE N'%İptal%' AND COALESCE(durum,N'') NOT LIKE N'%Iptal%' THEN COALESCE(toplam_tutar,0) ELSE 0 END),0) AS gross_revenue,
                COALESCE(SUM(CASE WHEN COALESCE(durum,N'') NOT LIKE N'%İptal%' AND COALESCE(durum,N'') NOT LIKE N'%Iptal%' THEN COALESCE(komisyon_tutari,0) ELSE 0 END),0) AS gross_commission,
                COALESCE(SUM(CASE WHEN COALESCE(durum,N'') NOT LIKE N'%İptal%' AND COALESCE(durum,N'') NOT LIKE N'%Iptal%' THEN COALESCE(platform_net_komisyon_tutari,0) ELSE 0 END),0) AS net_commission
            FROM rezervasyonlar
            WHERE giris_tarihi >= @fromDate
              AND giris_tarihi <= @toDate
              AND (@hotelId IS NULL OR otel_id = @hotelId);
            """;
        await using var cmd = new SqlCommand(sql, connection);
        BindReportFilters(cmd, model);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return new List<AdminSummaryCardViewModel>();
        }

        return new List<AdminSummaryCardViewModel>
        {
            new() { Label = "Rezervasyon", Value = SafeInt(reader, 0).ToString(CultureInfo.InvariantCulture), Description = "Filtre kapsamındaki kayıt", ToneClass = "info", IconClass = "fa-calendar-check" },
            new() { Label = "Ciro", Value = FormatScalar(SafeDecimal(reader, 1)), Description = "İptal dışı toplam", ToneClass = "success", IconClass = "fa-money-bill-wave" },
            new() { Label = "Brüt Komisyon", Value = FormatScalar(SafeDecimal(reader, 2)), Description = "Tahakkuk eden", ToneClass = "warning", IconClass = "fa-percent" },
            new() { Label = "Net Komisyon", Value = FormatScalar(SafeDecimal(reader, 3)), Description = "Platform net", ToneClass = "success", IconClass = "fa-coins" }
        };
    }

    public async Task<AdminRateLimitStatsPageViewModel> GetRateLimitStatsAsync(string fullName, string email, string userRole, int windowHours = 24, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var shell = await GetShellAsync(connection, "Rate Limit İstatistikleri", "429 yanıtlarını endpoint bazında izleyin.", fullName, email, userRole, cancellationToken);
        var model = new AdminRateLimitStatsPageViewModel { Shell = shell, WindowHours = Math.Clamp(windowHours <= 0 ? 24 : windowHours, 1, 168) };

        if (!await TableExistsAsync(connection, "api_loglari", cancellationToken))
        {
            return model;
        }

        var since = DateTime.UtcNow.AddHours(-model.WindowHours);
        const string sql = """
            SELECT TOP (200)
                COALESCE(endpoint,'') AS endpoint,
                COALESCE(http_method,'') AS method,
                SUM(CASE WHEN response_status = 429 THEN 1 ELSE 0 END) AS count_429,
                COUNT(*) AS count_total
            FROM api_loglari
            WHERE baslangic_tarihi >= @since
            GROUP BY endpoint, http_method
            ORDER BY count_429 DESC, count_total DESC;
            """;
        await using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@since", since);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            model.Rows.Add(new AdminRateLimitEndpointStatViewModel
            {
                Endpoint = reader.IsDBNull(0) ? "-" : reader.GetString(0),
                Method = reader.IsDBNull(1) ? "-" : reader.GetString(1),
                Count429 = SafeInt(reader, 2),
                CountTotal = SafeInt(reader, 3)
            });
        }
        return model;
    }

    public async Task<AdminSettingsMonitorPageViewModel> GetSettingsMonitorAsync(string fullName, string email, string userRole, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var shell = await GetShellAsync(connection, "Kritik Ayarlar (Read-only)", "Prod öncesi kritik config değerlerini tek ekranda izleyin.", fullName, email, userRole, cancellationToken);
        var model = new AdminSettingsMonitorPageViewModel { Shell = shell };

        string? Get(string key) => _configuration[key];
        model.Items["App:PublicBaseUrl"] = Get("App:PublicBaseUrl");
        model.Items["App:DefaultTimeZone"] = Get("App:DefaultTimeZone");
        model.Items["Uploads:OrphanCleanupEnabled"] = Get("Uploads:OrphanCleanupEnabled");
        model.Items["Uploads:OrphanCleanupMinAgeHours"] = Get("Uploads:OrphanCleanupMinAgeHours");
        model.Items["Security:Csp:Enforce"] = Get("Security:Csp:Enforce");

        // Email provider (DB)
        if (await TableExistsAsync(connection, "email_services", cancellationToken))
        {
            await using var cmd = new SqlCommand("""
                SELECT TOP (1) COALESCE(saglayici,''), COALESCE(aktif_mi, 0)
                FROM email_services
                ORDER BY id DESC;
                """, connection);
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                model.Items["EmailProvider:Provider"] = reader.IsDBNull(0) ? "-" : reader.GetString(0);
                model.Items["EmailProvider:Active"] = reader.IsDBNull(1) ? "0" : (reader.GetInt32(1) == 1 ? "1" : "0");
            }
        }

        return model;
    }

    public async Task<AdminPlatformCheckupPageViewModel> GetPlatformCheckupAsync(string fullName, string email, string userRole, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var shell = await GetShellAsync(connection, "Platform Checkup", "Yetki, onay, satış, içerik, e-posta, log ve kurulum eksiklerini tek ekranda izleyin.", fullName, email, userRole, cancellationToken);
        var model = new AdminPlatformCheckupPageViewModel { Shell = shell };

        model.SummaryCards.Add(new AdminSummaryCardViewModel
        {
            Label = "Bekleyen Partner",
            Value = shell.PendingPartnerApplications.ToString(CultureInfo.InvariantCulture),
            Description = "Admin onayı bekleyen tesis başvuruları",
            ToneClass = shell.PendingPartnerApplications > 0 ? "warning" : "success",
            IconClass = "fas fa-hotel"
        });
        model.SummaryCards.Add(new AdminSummaryCardViewModel
        {
            Label = "Bekleyen Firma",
            Value = shell.PendingCompanyApplications.ToString(CultureInfo.InvariantCulture),
            Description = "Kurumsal hesap onay akışı",
            ToneClass = shell.PendingCompanyApplications > 0 ? "warning" : "success",
            IconClass = "fas fa-building"
        });
        model.SummaryCards.Add(new AdminSummaryCardViewModel
        {
            Label = "E-posta Kuyruğu",
            Value = (await CountIfTableExistsAsync(connection, "bildirim_loglari", "COALESCE(durum,'') IN (N'Beklemede',N'Kuyrukta',N'Basarisiz',N'Başarısız')", cancellationToken)).ToString(CultureInfo.InvariantCulture),
            Description = "Bekleyen veya hatalı bildirim işleri",
            ToneClass = "info",
            IconClass = "fas fa-envelope-open-text"
        });
        model.SummaryCards.Add(new AdminSummaryCardViewModel
        {
            Label = "Aktif E-posta Servisi",
            Value = (await CountIfTableExistsAsync(connection, "email_services", "COALESCE(aktif_mi,0)=1", cancellationToken)).ToString(CultureInfo.InvariantCulture),
            Description = "Canlı gönderime açık SMTP hesapları",
            ToneClass = "success",
            IconClass = "fas fa-server"
        });

        model.Groups.Add(await BuildTableCheckGroupAsync(connection, "Yetki ve Kullanıcı", "Rol, yetki, kullanıcı ve admin işlem izleri.", "primary", cancellationToken,
            ("Kullanıcılar", "users", "/admin/kullanicilar"),
            ("Yöneticiler", "users", "/admin/yoneticiler"),
            ("Yetki tanımları", "yetkiler", "/admin/platform-yetkilileri"),
            ("Rol yetkileri", "rol_yetkileri", "/admin/platform-yetkilileri"),
            ("Admin işlem logları", "admin_islem_loglari", "/admin/islem-loglari")));

        model.Groups.Add(await BuildTableCheckGroupAsync(connection, "Ticari Operasyon", "Rezervasyon, ödeme, komisyon, satış ekibi ve ciro takibi.", "success", cancellationToken,
            ("Rezervasyonlar", "rezervasyonlar", "/admin/rezervasyonlar-tek-liste"),
            ("Ödeme işlemleri", "odeme_islemleri", "/admin/odemeler"),
            ("Komisyon kayıtları", "komisyon_muhasebe_kayitlari", "/admin/komisyonlar"),
            ("Firma rezervasyonları", "firma_rezervasyonlari", "/admin/firma-rezervasyonlari"),
            ("Satış müşteri havuzu", "satis_musterileri", "/satis/musteri-yonetimi")));

        model.Groups.Add(await BuildTableCheckGroupAsync(connection, "Otel, Oda ve Fiyat", "Otel içerikleri, odalar, özellikler, fiyat takvimi ve görsel kayıtları.", "azure", cancellationToken,
            ("Oteller", "oteller", "/admin/oteller"),
            ("Oda tipleri", "oda_tipleri", "/admin/oteller"),
            ("Oda özellikleri", "oda_ozellikleri", "/admin/oteller"),
            ("Fiyat/müsaitlik takvimi", "oda_fiyat_musaitlik", "/admin/ticari-icgoru"),
            ("Otel fotoğrafları", "otel_gorselleri", "/admin/oteller"),
            ("Oda fotoğrafları", "oda_gorselleri", "/admin/oteller")));

        model.Groups.Add(await BuildTableCheckGroupAsync(connection, "Başvuru ve Onay", "Partner/firma onboarding, evrak ve admin karar kayıtları.", "warning", cancellationToken,
            ("Partner detayları", "partner_detaylari", "/admin/partner-basvurulari"),
            ("Partner evrakları", "partner_basvuru_evraklari", "/admin/partner-basvurulari"),
            ("Partner başvuru hareketleri", "partner_basvuru_hareketleri", "/admin/partner-basvurulari"),
            ("Firmalar", "firmalar", "/admin/firma-basvurulari"),
            ("Firma çalışanları", "firma_calisanlari", "/firma/calisanlar")));

        model.Groups.Add(await BuildTableCheckGroupAsync(connection, "E-posta, Log ve Sağlık", "Kuyruk, şablon, servis, sistem logları ve health izleme.", "danger", cancellationToken,
            ("E-posta servisleri", "email_services", "/admin/mail-merkezi"),
            ("Bildirim kuyruğu", "bildirim_loglari", "/admin/email-kuyruk"),
            ("Bildirim şablonları", "bildirim_sablonlari", "/admin/eposta-sablonlari"),
            ("Sistem hata logları", "sistem_hata_loglari", "/admin/loglar"),
            ("API logları", "api_loglari", "/admin/rate-limit")));

        model.Groups.Add(await BuildTableCheckGroupAsync(connection, "Konum ve Kurulum", "İl, ilçe, mahalle, geo arama ve kurulum verileri.", "info", cancellationToken,
            ("İller", "iller", "/admin/ayarlar"),
            ("İlçeler", "ilceler", "/admin/ayarlar"),
            ("Mahalleler", "mahalleler", "/admin/ayarlar"),
            ("Konum arama logları", "kullanici_konum_loglari", "/admin/konum-arama-loglari"),
            ("Otel koordinat değişimleri", "otel_koordinat_degisim_loglari", "/admin/otel-koordinat-degisimleri")));

        model.Roadmap.AddRange(new[]
        {
            new AdminPlatformRoadmapItemViewModel { Phase = "1", Scope = "Admin temel yönetim", Status = "Devam ediyor", Detail = "Dashboard, tek rezervasyon listesi, komisyon, partner başvuru ve platform checkup Tabler yapısına alındı." },
            new AdminPlatformRoadmapItemViewModel { Phase = "2", Scope = "Yetki ve satış ekibi", Status = "Sırada", Detail = "Rol/yetki matrisi, satış ciroları, kullanıcı rezervasyon adetleri ve işlem logları tekil sayfalara ayrılacak." },
            new AdminPlatformRoadmapItemViewModel { Phase = "3", Scope = "Otel/oda/fiyat/görsel", Status = "Sırada", Detail = "Otel detay, oda, fiyat, WEBP görsel ve güvenli silme yönetimi admin/partner ortak standarda bağlanacak." },
            new AdminPlatformRoadmapItemViewModel { Phase = "4", Scope = "E-posta ve sistem sağlığı", Status = "Sırada", Detail = "Canlı SMTP servisleri, kuyruk retry, şablon, health ve ayar izleme tek akışta tamamlanacak." },
            new AdminPlatformRoadmapItemViewModel { Phase = "5", Scope = "Firma/partner tamamlama", Status = "Planlandı", Detail = "Yarım kalan firma ve partner sayfaları aynı sayfa-adı CS/CSS/CSHTML sözleşmesiyle tamamlanacak." }
        });

        return model;
    }

    public async Task<AdminCommerceInsightPageViewModel> GetCommerceInsightPageAsync(
        string fullName,
        string email,
        string userRole,
        long priceHistoryHotelId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var shell = await GetShellAsync(connection, "Ticari İçgörü & Vitals", "Growth, Web Vitals özetleri ve stok/fiyat örnekleri.", fullName, email, userRole, cancellationToken);
        var model = new AdminCommerceInsightPageViewModel
        {
            Shell = shell,
            KillSwitchConfig = _configuration.GetValue("Growth:KillSwitchAll", false),
            KillSwitchEmergency = _growthGovernance.EmergencyKillSwitchActive,
            PriceHistoryHotelId = priceHistoryHotelId > 0 ? priceHistoryHotelId : 0
        };

        foreach (var row in _commerceMetrics.SnapshotRum(40))
        {
            model.RumRows.Add(new AdminCommerceRumRowViewModel
            {
                RouteMetric = row.RouteMetric,
                Avg = row.Avg,
                Count = row.Count,
                Min = row.Min,
                Max = row.Max
            });
        }

        foreach (var kv in _commerceMetrics.SnapshotGrowthKinds())
        {
            model.GrowthKinds[kv.Key] = kv.Value;
        }

        var since = DateTime.UtcNow.AddDays(-7);
        const string kpiSql = """
            SELECT COUNT(*), COALESCE(SUM(COALESCE(toplam_tutar,0)),0)
            FROM rezervasyonlar
            WHERE COALESCE(olusturulma_tarihi, SYSUTCDATETIME()) >= @since
              AND COALESCE(durum,'') <> N'İptal Edildi';
            """;
        await using (var kpiCmd = new SqlCommand(kpiSql, connection))
        {
            kpiCmd.Parameters.AddWithValue("@since", since);
            await using var r = await kpiCmd.ExecuteReaderAsync(cancellationToken);
            if (await r.ReadAsync(cancellationToken))
            {
                model.ReservationsLast7Days = SafeInt(r, 0);
                model.RevenueLast7Days = SafeDecimal(r, 1);
            }
        }

        if (await TableExistsAsync(connection, "oda_fiyat_musaitlik", cancellationToken))
        {
            const string invSql = """
                SELECT TOP (40)
                    COALESCE(o.otel_adi,'') AS otel_adi,
                    COALESCE(ot.oda_adi,'') AS oda_adi,
                    ofm.tarih,
                    (COALESCE(ofm.toplam_oda_sayisi,0) - COALESCE(ofm.satilan_oda_sayisi,0) - COALESCE(ofm.bloke_oda_sayisi,0)) AS kalan
                FROM oda_fiyat_musaitlik ofm
                JOIN oteller o ON o.id = ofm.otel_id
                JOIN oda_tipleri ot ON ot.id = ofm.oda_tip_id
                WHERE ofm.tarih >= CAST(SYSUTCDATETIME() AS date)
                  AND (COALESCE(ofm.toplam_oda_sayisi,0) - COALESCE(ofm.satilan_oda_sayisi,0) - COALESCE(ofm.bloke_oda_sayisi,0)) BETWEEN 1 AND 3
                ORDER BY kalan ASC, ofm.tarih ASC;
                """;
            await using var invCmd = new SqlCommand(invSql, connection);
            await using var invReader = await invCmd.ExecuteReaderAsync(cancellationToken);
            while (await invReader.ReadAsync(cancellationToken))
            {
                model.InventoryRows.Add(new AdminCommerceInventoryRowViewModel
                {
                    HotelName = invReader.IsDBNull(0) ? "-" : invReader.GetString(0),
                    RoomName = invReader.IsDBNull(1) ? "-" : invReader.GetString(1),
                    Date = DateOnly.FromDateTime(invReader.GetDateTime(2)),
                    Remaining = SafeInt(invReader, 3)
                });
            }
        }

        var hid = model.PriceHistoryHotelId;
        if (hid <= 0)
        {
            const string firstHotelSql = "SELECT TOP (1) id FROM oteller ORDER BY id ASC;";
            await using var fhCmd = new SqlCommand(firstHotelSql, connection);
            var scalar = await fhCmd.ExecuteScalarAsync(cancellationToken);
            if (scalar is not null && scalar != DBNull.Value)
            {
                hid = Convert.ToInt64(scalar, CultureInfo.InvariantCulture);
                model.PriceHistoryHotelId = hid;
            }
        }

        if (hid > 0 && await TableExistsAsync(connection, "oda_fiyat_musaitlik", cancellationToken))
        {
            const string priceSql = """
                SELECT TOP (60)
                    COALESCE(ot.oda_adi,'') AS oda_adi,
                    ofm.tarih,
                    COALESCE(ofm.gecelik_fiyat,0) AS gecelik,
                    ofm.indirimli_fiyat
                FROM oda_fiyat_musaitlik ofm
                JOIN oda_tipleri ot ON ot.id = ofm.oda_tip_id
                WHERE ofm.otel_id = @hotelId
                ORDER BY ofm.tarih DESC, ofm.id DESC;
                """;
            await using var pCmd = new SqlCommand(priceSql, connection);
            pCmd.Parameters.AddWithValue("@hotelId", hid);
            await using var pReader = await pCmd.ExecuteReaderAsync(cancellationToken);
            while (await pReader.ReadAsync(cancellationToken))
            {
                model.PriceSampleRows.Add(new AdminCommercePriceRowViewModel
                {
                    RoomName = pReader.IsDBNull(0) ? "-" : pReader.GetString(0),
                    Date = DateOnly.FromDateTime(pReader.GetDateTime(1)),
                    BasePrice = pReader.IsDBNull(2) ? 0m : pReader.GetDecimal(2),
                    DiscountPrice = pReader.IsDBNull(3) ? null : pReader.GetDecimal(3)
                });
            }
        }

        if (await TableExistsAsync(connection, "rezervasyonlar_archive", cancellationToken))
        {
            await using var aCmd = new SqlCommand("SELECT COUNT(*) FROM rezervasyonlar_archive;", connection);
            var ac = await aCmd.ExecuteScalarAsync(cancellationToken);
            model.ArchivedReservationSampleCount = ac is null || ac == DBNull.Value
                ? 0
                : Convert.ToInt32(ac, CultureInfo.InvariantCulture);
            model.ArchiveHint = "Arşiv tablosu mevcut.";
        }
        else
        {
            model.ArchiveHint = "rezervasyonlar_archive tablosu henüz oluşturulmadı (migration ile).";
        }

        return model;
    }

    private static string Csv(string? value)
    {
        var v = value ?? string.Empty;
        v = v.Replace("\"", "\"\"", StringComparison.Ordinal);
        return $"\"{v}\"";
    }

    public async Task<string> ExportMonthlyHotelRevenueCommissionCsvAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        const string sql = """
            SELECT
                FORMAT(DATEFROMPARTS(YEAR(r.giris_tarihi), MONTH(r.giris_tarihi), 1), 'yyyy-MM', 'en-US') AS ay,
                o.id,
                o.otel_adi,
                COALESCE(o.sehir, '') AS sehir,
                COALESCE(o.ilce, '') AS ilce,
                COUNT(*) AS rezervasyon,
                COALESCE(SUM(COALESCE(r.toplam_tutar,0)),0) AS ciro,
                COALESCE(SUM(COALESCE(r.komisyon_tutari,0)),0) AS brut_komisyon,
                COALESCE(SUM(COALESCE(r.platform_net_komisyon_tutari,0)),0) AS net_komisyon
            FROM rezervasyonlar r
            INNER JOIN oteller o ON o.id = r.otel_id
            WHERE COALESCE(r.durum,'') <> N'İptal Edildi'
              AND r.giris_tarihi >= DATEADD(month, -6, DATEFROMPARTS(YEAR(GETDATE()), MONTH(GETDATE()), 1))
            GROUP BY DATEFROMPARTS(YEAR(r.giris_tarihi), MONTH(r.giris_tarihi), 1), o.id, o.otel_adi, o.sehir, o.ilce
            ORDER BY DATEFROMPARTS(YEAR(r.giris_tarihi), MONTH(r.giris_tarihi), 1) DESC, ciro DESC;
            """;

        var sb = new StringBuilder();
        var inv = CultureInfo.InvariantCulture;
        string CsvDec(SqlDataReader rdr, int ord)
        {
            var d = rdr.IsDBNull(ord) ? 0m : rdr.GetDecimal(ord);
            return Csv(d.ToString("0.##", inv));
        }

        sb.AppendLine(string.Join(';', new[]
        {
            Csv("ay"), Csv("otel_id"), Csv("otel_adi"), Csv("sehir"), Csv("ilce"),
            Csv("rezervasyon_adedi"), Csv("ciro_tl"), Csv("brut_komisyon_tl"), Csv("net_komisyon_tl")
        }));

        await using var cmd = new SqlCommand(sql, connection);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            sb.AppendLine(string.Join(';', new[]
            {
                Csv(reader.IsDBNull(0) ? string.Empty : reader.GetString(0)),
                Csv(reader.IsDBNull(1) ? string.Empty : reader.GetInt64(1).ToString(inv)),
                Csv(reader.IsDBNull(2) ? string.Empty : reader.GetString(2)),
                Csv(reader.IsDBNull(3) ? string.Empty : reader.GetString(3)),
                Csv(reader.IsDBNull(4) ? string.Empty : reader.GetString(4)),
                Csv(SafeInt(reader, 5).ToString(inv)),
                CsvDec(reader, 6),
                CsvDec(reader, 7),
                CsvDec(reader, 8)
            }));
        }

        return sb.ToString();
    }

    private static async Task<bool> ColumnExistsAsync(SqlConnection connection, string tableName, string columnName, CancellationToken cancellationToken)
    {
        await using var command = new SqlCommand("SELECT COL_LENGTH(@tableName, @columnName);", connection);
        command.Parameters.AddWithValue("@tableName", tableName);
        command.Parameters.AddWithValue("@columnName", columnName);
        var scalar = await command.ExecuteScalarAsync(cancellationToken);
        return scalar is not null && scalar != DBNull.Value;
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
        const string firmPricingSql = """
            IF OBJECT_ID(N'dbo.firma_oda_fiyat_musaitlik', N'U') IS NULL
            BEGIN
                SELECT 0 AS exists_flag, 0 AS row_count;
            END
            ELSE
            BEGIN
                SELECT 1 AS exists_flag, (SELECT COUNT(*) FROM dbo.firma_oda_fiyat_musaitlik) AS row_count;
            END
            """;
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
        const string listingSubSql = """
            IF OBJECT_ID(N'dbo.otel_liste_abonelikleri', N'U') IS NULL
            BEGIN
                SELECT 0 AS exists_flag, 0 AS row_count;
            END
            ELSE
            BEGIN
                SELECT 1 AS exists_flag, (SELECT COUNT(*) FROM dbo.otel_liste_abonelikleri) AS row_count;
            END
            """;
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

        try
        {
            var report = await _healthCheckService.CheckHealthAsync(cancellationToken);
            model.PlatformHealthAggregateStatus = report.Status.ToString();
            model.PlatformHealthTotalDurationMs = report.TotalDuration.TotalMilliseconds;
            foreach (var entry in report.Entries.OrderBy(static e => e.Key, StringComparer.OrdinalIgnoreCase))
            {
                var r = entry.Value;
                model.PlatformHealthProbes.Add(new AdminPlatformHealthProbeViewModel
                {
                    Name = entry.Key,
                    Status = r.Status.ToString(),
                    DurationMs = r.Duration.TotalMilliseconds,
                    Detail = string.IsNullOrWhiteSpace(r.Description)
                        ? r.Exception?.Message
                        : r.Description
                });
            }
        }
        catch (Exception ex)
        {
            model.PlatformHealthError = ex.Message;
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
                   CASE
                        WHEN COL_LENGTH('partner_detaylari', 'eposta_giris_onayi_verildi_mi') IS NULL THEN 0
                        ELSE COALESCE(p.eposta_giris_onayi_verildi_mi, 0)
                   END AS email_login_approved,
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
                EmailLoginApproved = SafeInt(reader, 12) == 1,
                DocumentCount = SafeInt(reader, 13),
                ReviewNote = reader.IsDBNull(14) ? null : reader.GetString(14)
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

    public async Task<AdminApprovalCenterPageViewModel> GetApprovalCenterAsync(string fullName, string email, string userRole, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var model = new AdminApprovalCenterPageViewModel
        {
            Shell = await GetShellAsync(connection, "Onay Merkezi", "Partner, firma, otel, evrak, komisyon ve fatura onaylarını tek merkezde yönetin.", fullName, email, userRole, cancellationToken)
        };

        const string summarySql = @"
            SELECT
                (SELECT COUNT(*) FROM oteller),
                (SELECT COUNT(*) FROM oteller WHERE COALESCE(onay_durumu,'Beklemede') <> 'Onaylandi' OR COALESCE(yayin_durumu,'Kapali') <> 'Yayinda'),
                (SELECT COUNT(*) FROM partner_detaylari WHERE COALESCE(onay_durumu,'Beklemede') = 'Beklemede'),
                (SELECT COUNT(*) FROM firmalar WHERE COALESCE(onay_durumu,'Beklemede') = 'Beklemede'),
                (SELECT COUNT(*) FROM rezervasyonlar),
                (SELECT COALESCE(SUM(COALESCE(toplam_tutar,0)),0) FROM rezervasyonlar WHERE COALESCE(durum,'') <> N'İptal Edildi'),
                (SELECT COALESCE(SUM(COALESCE(komisyon_tutari,0)),0) FROM komisyon_muhasebe_kayitlari),
                (SELECT COUNT(*) FROM faturalar WHERE COALESCE(fatura_durumu,'Taslak') IN ('Taslak','Beklemede'));";

        await using (var command = new SqlCommand(summarySql, connection))
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            if (await reader.ReadAsync(cancellationToken))
            {
                model.SummaryCards.Add(new AdminSummaryCardViewModel { Label = "Toplam Otel", Value = SafeInt(reader, 0).ToString(CultureInfo.InvariantCulture), Description = "Platformdaki tüm oteller", ToneClass = "info", IconClass = "fa-hotel" });
                model.SummaryCards.Add(new AdminSummaryCardViewModel { Label = "Onay/Yayın Bekleyen", Value = SafeInt(reader, 1).ToString(CultureInfo.InvariantCulture), Description = "Admin onayı veya yayın kararı bekleyen oteller", ToneClass = "warning", IconClass = "fa-circle-pause" });
                model.SummaryCards.Add(new AdminSummaryCardViewModel { Label = "Partner Başvuru", Value = SafeInt(reader, 2).ToString(CultureInfo.InvariantCulture), Description = "Evrak/onay bekleyen partnerler", ToneClass = "warning", IconClass = "fa-file-signature" });
                model.SummaryCards.Add(new AdminSummaryCardViewModel { Label = "Firma Başvuru", Value = SafeInt(reader, 3).ToString(CultureInfo.InvariantCulture), Description = "Kurumsal hesap onayı bekleyenler", ToneClass = "warning", IconClass = "fa-building" });
                model.SummaryCards.Add(new AdminSummaryCardViewModel { Label = "Rezervasyon", Value = SafeInt(reader, 4).ToString(CultureInfo.InvariantCulture), Description = "Tüm zamanlar rezervasyon adedi", ToneClass = "success", IconClass = "fa-calendar-check" });
                model.SummaryCards.Add(new AdminSummaryCardViewModel { Label = "Ciro", Value = $"{SafeDecimal(reader, 5).ToString("N0", CultureInfo.GetCultureInfo("tr-TR"))} TL", Description = "İptal dışı toplam rezervasyon cirosu", ToneClass = "success", IconClass = "fa-chart-line" });
                model.SummaryCards.Add(new AdminSummaryCardViewModel { Label = "Komisyon", Value = $"{SafeDecimal(reader, 6).ToString("N0", CultureInfo.GetCultureInfo("tr-TR"))} TL", Description = "Muhasebe komisyon kaydı", ToneClass = "primary", IconClass = "fa-percent" });
                model.SummaryCards.Add(new AdminSummaryCardViewModel { Label = "Fatura Bekleyen", Value = SafeInt(reader, 7).ToString(CultureInfo.InvariantCulture), Description = "Taslak/beklemede fatura kayıtları", ToneClass = "danger", IconClass = "fa-receipt" });
            }
        }

        const string approvalsSql = @"
            SELECT TOP (150) type_name, entity_id, title, detail, status_text, created_at, action_url
            FROM (
                SELECT N'Partner' AS type_name, p.id AS entity_id, p.firma_unvani AS title,
                       CONCAT(COALESCE(o.otel_adi, N'Otel bağlantısı yok'), N' · ', COALESCE(p.yetkili_eposta, N'')) AS detail,
                       COALESCE(p.onay_durumu, N'Beklemede') AS status_text,
                       COALESCE(p.olusturulma_tarihi, SYSUTCDATETIME()) AS created_at,
                       N'/admin/partner-basvurulari' AS action_url
                FROM partner_detaylari p
                LEFT JOIN oteller o ON o.partner_id = p.id
                WHERE COALESCE(p.onay_durumu, N'Beklemede') <> N'Onaylandi'
                UNION ALL
                SELECT N'Firma', f.id, f.firma_adi,
                       CONCAT(COALESCE(f.yetkili_eposta, f.firma_eposta, N''), N' · ', COALESCE(f.vergi_no, N'')),
                       COALESCE(f.onay_durumu, N'Beklemede'),
                       COALESCE(f.olusturulma_tarihi, SYSUTCDATETIME()),
                       N'/admin/firma-basvurulari'
                FROM firmalar f
                WHERE COALESCE(f.onay_durumu, N'Beklemede') <> N'Onaylandı'
                UNION ALL
                SELECT N'Otel', o.id, o.otel_adi,
                       CONCAT(COALESCE(o.ilce, N''), N', ', COALESCE(o.sehir, N''), N' · ', COALESCE(p.firma_unvani, N'Partner yok')),
                       CONCAT(COALESCE(o.onay_durumu, N'Beklemede'), N' / ', COALESCE(o.yayin_durumu, N'Kapali')),
                       COALESCE(o.olusturulma_tarihi, SYSUTCDATETIME()),
                       CONCAT(N'/admin/otel-detay/', o.id)
                FROM oteller o
                LEFT JOIN partner_detaylari p ON p.id = o.partner_id
                WHERE COALESCE(o.onay_durumu, N'Beklemede') <> N'Onaylandi' OR COALESCE(o.yayin_durumu, N'Kapali') <> N'Yayinda'
            ) x
            ORDER BY created_at DESC;";

        await using (var command = new SqlCommand(approvalsSql, connection))
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                var status = reader.GetString(4);
                model.PendingApprovals.Add(new AdminApprovalTaskRowViewModel
                {
                    Type = reader.GetString(0),
                    EntityId = reader.GetInt64(1),
                    Title = reader.GetString(2),
                    Detail = reader.GetString(3),
                    StatusText = status,
                    ToneClass = ResolveApprovalTone(status),
                    CreatedAtText = reader.GetDateTime(5).ToString("dd.MM.yyyy HH:mm", CultureInfo.GetCultureInfo("tr-TR")),
                    ActionUrl = reader.GetString(6)
                });
            }
        }

        var monthStart = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        const string hotelsSql = @"
            SELECT TOP (100)
                o.id, o.otel_adi, COALESCE(p.firma_unvani, N'Partner yok'),
                CONCAT(COALESCE(o.ilce, N''), N', ', COALESCE(o.sehir, N'')),
                COALESCE(o.onay_durumu, N'Beklemede'), COALESCE(o.yayin_durumu, N'Kapali'),
                COALESCE(o.varsayilan_komisyon_orani, 0),
                COALESCE(SUM(CASE WHEN r.olusturulma_tarihi >= @monthStart AND COALESCE(r.durum,'') <> N'İptal Edildi' THEN COALESCE(r.toplam_tutar,0) ELSE 0 END),0),
                COALESCE(SUM(CASE WHEN k.kayit_tarihi >= @monthStart THEN COALESCE(k.komisyon_tutari,0) ELSE 0 END),0)
            FROM oteller o
            LEFT JOIN partner_detaylari p ON p.id = o.partner_id
            LEFT JOIN rezervasyonlar r ON r.otel_id = o.id
            LEFT JOIN komisyon_muhasebe_kayitlari k ON k.otel_id = o.id
            GROUP BY o.id, o.otel_adi, p.firma_unvani, o.ilce, o.sehir, o.onay_durumu, o.yayin_durumu, o.varsayilan_komisyon_orani
            ORDER BY CASE WHEN COALESCE(o.onay_durumu,'Beklemede') <> 'Onaylandi' OR COALESCE(o.yayin_durumu,'Kapali') <> 'Yayinda' THEN 0 ELSE 1 END, o.id DESC;";

        await using (var command = new SqlCommand(hotelsSql, connection))
        {
            command.Parameters.AddWithValue("@monthStart", monthStart);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var approval = reader.GetString(4);
                var publish = reader.GetString(5);
                model.Hotels.Add(new AdminApprovalHotelRowViewModel
                {
                    HotelId = reader.GetInt64(0),
                    HotelName = reader.GetString(1),
                    PartnerName = reader.GetString(2),
                    CityLabel = reader.GetString(3),
                    ApprovalStatus = approval,
                    PublishStatus = publish,
                    ToneClass = ResolveApprovalTone($"{approval} {publish}"),
                    CommissionRate = SafeDecimal(reader, 6),
                    MonthRevenue = SafeDecimal(reader, 7),
                    MonthCommission = SafeDecimal(reader, 8)
                });
            }
        }

        const string invoicesSql = @"
            SELECT TOP (50)
                f.id, COALESCE(f.fatura_no, CONCAT(N'Taslak-', f.id)), COALESCE(f.fatura_turu, N'Konaklama'),
                COALESCE(o.otel_adi, N'-'), COALESCE(f.fatura_alici_unvan, u.ad_soyad, N'-'),
                COALESCE(f.fatura_durumu, N'Taslak'), COALESCE(f.genel_toplam, 0), COALESCE(f.fatura_tarihi, f.olusturulma_tarihi)
            FROM faturalar f
            LEFT JOIN oteller o ON o.id = f.otel_id
            LEFT JOIN users u ON u.id = f.kullanici_id
            ORDER BY COALESCE(f.fatura_tarihi, f.olusturulma_tarihi) DESC, f.id DESC;";

        await using (var command = new SqlCommand(invoicesSql, connection))
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                var status = reader.GetString(5);
                model.Invoices.Add(new AdminApprovalInvoiceRowViewModel
                {
                    InvoiceId = reader.GetInt64(0),
                    InvoiceNo = reader.GetString(1),
                    InvoiceType = reader.GetString(2),
                    HotelName = reader.GetString(3),
                    BuyerTitle = reader.GetString(4),
                    StatusText = status,
                    ToneClass = ResolveApprovalTone(status),
                    TotalAmount = SafeDecimal(reader, 6),
                    DateText = reader.GetDateTime(7).ToString("dd.MM.yyyy", CultureInfo.GetCultureInfo("tr-TR"))
                });
            }
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

    public async Task<(bool Success, string Message)> SetPartnerEmailLoginApprovalAsync(long adminUserId, AdminPartnerEmailLoginApprovalRequest request, CancellationToken cancellationToken = default)
    {
        if (request.PartnerId <= 0)
        {
            return (false, "Partner basvurusu bulunamadi.");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var tx = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            const string sql = """
                UPDATE partner_detaylari
                SET eposta_giris_onayi_verildi_mi = @approved,
                    eposta_giris_onay_tarihi = CASE WHEN @approved = 1 THEN SYSUTCDATETIME() ELSE NULL END,
                    eposta_giris_onaylayan_admin_id = CASE WHEN @approved = 1 THEN @adminUserId ELSE NULL END,
                    guncellenme_tarihi = SYSUTCDATETIME()
                WHERE id = @partnerId;
                """;

            await using (var cmd = new SqlCommand(sql, connection, (SqlTransaction)tx))
            {
                cmd.Parameters.AddWithValue("@approved", request.Approved ? 1 : 0);
                cmd.Parameters.AddWithValue("@adminUserId", adminUserId);
                cmd.Parameters.AddWithValue("@partnerId", request.PartnerId);
                var affected = await cmd.ExecuteNonQueryAsync(cancellationToken);
                if (affected <= 0)
                {
                    await tx.RollbackAsync(cancellationToken);
                    return (false, "Partner basvurusu bulunamadi.");
                }
            }

            if (await TableExistsAsync(connection, "partner_basvuru_hareketleri", cancellationToken, (SqlTransaction?)tx))
            {
                const string historySql = """
                    INSERT INTO partner_basvuru_hareketleri
                    (partner_id, onceki_durum, yeni_durum, islem_tipi, aciklama, islem_yapan_kullanici_id, olusturulma_tarihi)
                    VALUES
                    (@partnerId, NULL, NULL, 'AdminPartnerEpostaGirisOnayi', @note, @adminUserId, SYSUTCDATETIME());
                    """;
                await using var history = new SqlCommand(historySql, connection, (SqlTransaction)tx);
                history.Parameters.AddWithValue("@partnerId", request.PartnerId);
                history.Parameters.AddWithValue("@adminUserId", adminUserId);
                history.Parameters.AddWithValue("@note", string.IsNullOrWhiteSpace(request.Note)
                    ? (request.Approved ? "Admin partner e-posta giris onayi verdi." : "Admin partner e-posta giris onayini geri cekti.")
                    : request.Note.Trim());
                await history.ExecuteNonQueryAsync(cancellationToken);
            }

            await tx.CommitAsync(cancellationToken);
            return (true, request.Approved ? "Partner e-posta giris onayi verildi." : "Partner e-posta giris onayi kaldirildi.");
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(cancellationToken);
            return (false, $"E-posta giris onayi guncellenemedi: {ex.Message}");
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

    public async Task<AdminCommissionManagementPageViewModel> GetCommissionManagementAsync(string fullName, string email, string userRole, long? hotelId = null, DateTime? dateFrom = null, DateTime? dateTo = null, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var model = new AdminCommissionManagementPageViewModel
        {
            Shell = await GetShellAsync(connection, "Komisyon ve Vergi Ayarlari", "Otel bazli komisyon, KDV ve konaklama vergisi kurallarini tarih bazli yonetin.", fullName, email, userRole, cancellationToken)
        };
        model.DateFrom = dateFrom?.Date;
        model.DateTo = dateTo?.Date;

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
            SELECT TOP (15)
                o.id,
                o.otel_adi,
                COALESCE(reservationStats.gross_revenue, 0) AS gross_revenue,
                COALESCE(commissionStats.total_commission, 0) AS total_commission,
                COALESCE(commissionStats.paid_commission, 0) AS paid_commission,
                COALESCE(reservationStats.reservation_count, 0) AS reservation_count,
                COALESCE(reservationStats.completed_reservation_count, 0) AS completed_reservation_count,
                COALESCE(reservationStats.platform_net_commission_total, 0) AS platform_net_commission_total
            FROM oteller o
            OUTER APPLY
            (
                SELECT
                    SUM(COALESCE(r.toplam_tutar, 0)) AS gross_revenue,
                    COUNT(*) AS reservation_count,
                    SUM(CASE WHEN COALESCE(r.durum, '') = N'Tamamlandı' THEN 1 ELSE 0 END) AS completed_reservation_count,
                    SUM(COALESCE(r.platform_net_komisyon_tutari, 0)) AS platform_net_commission_total
                FROM rezervasyonlar r
                WHERE r.otel_id = o.id
                  AND COALESCE(r.durum, '') <> 'İptal Edildi'
                  AND (@dateFrom IS NULL OR CAST(r.giris_tarihi AS date) >= CAST(@dateFrom AS date))
                  AND (@dateTo IS NULL OR CAST(r.cikis_tarihi AS date) <= CAST(@dateTo AS date))
            ) reservationStats
            OUTER APPLY
            (
                SELECT
                    SUM(COALESCE(k.komisyon_tutari, 0)) AS total_commission,
                    SUM(CASE WHEN COALESCE(k.otele_odeme_durumu, '') = 'Ödendi' THEN COALESCE(k.komisyon_tutari, 0) ELSE 0 END) AS paid_commission
                FROM komisyon_muhasebe_kayitlari k
                WHERE k.otel_id = o.id
                  AND (@dateFrom IS NULL OR CAST(k.kayit_tarihi AS date) >= CAST(@dateFrom AS date))
                  AND (@dateTo IS NULL OR CAST(k.kayit_tarihi AS date) <= CAST(@dateTo AS date))
            ) commissionStats
            WHERE (@hotelId IS NULL OR o.id = @hotelId)
              AND (COALESCE(reservationStats.gross_revenue, 0) > 0 OR COALESCE(commissionStats.total_commission, 0) > 0)
            ORDER BY COALESCE(reservationStats.gross_revenue, 0) DESC, o.id DESC;";

        await using (var financeCommand = new SqlCommand(financeSql, connection))
        {
            financeCommand.Parameters.AddWithValue("@hotelId", hotelId.HasValue ? hotelId.Value : DBNull.Value);
            financeCommand.Parameters.AddWithValue("@dateFrom", dateFrom.HasValue ? dateFrom.Value.Date : DBNull.Value);
            financeCommand.Parameters.AddWithValue("@dateTo", dateTo.HasValue ? dateTo.Value.Date : DBNull.Value);
            await using var financeReader = await financeCommand.ExecuteReaderAsync(cancellationToken);
            while (await financeReader.ReadAsync(cancellationToken))
            {
                model.HotelFinanceRows.Add(new AdminHotelCommissionFinanceRowViewModel
                {
                    HotelId = financeReader.GetInt64(0),
                    HotelName = financeReader.GetString(1),
                    GrossRevenue = SafeDecimal(financeReader, 2),
                    TotalCommission = SafeDecimal(financeReader, 3),
                    PaidCommission = SafeDecimal(financeReader, 4),
                    ReservationCount = SafeInt(financeReader, 5),
                    CompletedReservationCount = SafeInt(financeReader, 6),
                    PlatformNetCommission = SafeDecimal(financeReader, 7)
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

    public async Task<AdminPlatformTeamPageViewModel> GetPlatformTeamAsync(string fullName, string email, string userRole, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var model = new AdminPlatformTeamPageViewModel
        {
            Shell = await GetShellAsync(connection, "Ekibimiz Yönetimi", "Yardım Merkezi'ndeki Ekibimiz kartlarını yönetin: ekle, düzenle, sırala, aktif/pasif.", fullName, email, userRole, cancellationToken)
        };

        try
        {
            const string sql = @"
                SELECT id, ad_soyad, unvan, eposta, aciklama, COALESCE(avatar_url, N''), COALESCE(siralama, 0), COALESCE(aktif_mi, 1)
                FROM dbo.platform_ekip_uyeleri
                ORDER BY COALESCE(siralama, 0) ASC, id ASC;";
            await using var cmd = new SqlCommand(sql, connection);
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var name = reader.GetString(1);
                var avatar = reader.IsDBNull(5) ? string.Empty : reader.GetString(5);
                if (string.IsNullOrWhiteSpace(avatar))
                {
                    var q = Uri.EscapeDataString(name);
                    avatar = $"https://ui-avatars.com/api/?name={q}&size=160&background=0b57d0&color=ffffff&bold=true&format=png";
                }

                model.Members.Add(new AdminPlatformTeamRowViewModel
                {
                    Id = reader.GetInt64(0),
                    Name = name,
                    Title = reader.GetString(2),
                    Email = reader.GetString(3),
                    Description = reader.IsDBNull(4) ? null : reader.GetString(4),
                    AvatarUrl = avatar,
                    OrderNo = SafeInt(reader, 6),
                    IsActive = SafeBool(reader, 7)
                });
            }
        }
        catch (SqlException ex) when (IsMissingTableOrColumn(ex))
        {
            // tablo migration ile gelecek; boş liste göster
        }

        return model;
    }

    public async Task<(bool Success, string Message)> SavePlatformTeamMemberAsync(long adminUserId, AdminPlatformTeamForm form, string? avatarUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(form.Name) || string.IsNullOrWhiteSpace(form.Title) || string.IsNullOrWhiteSpace(form.Email))
        {
            return (false, "Ad Soyad, Ünvan ve E-posta zorunludur.");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        const string sql = @"
            IF OBJECT_ID(N'dbo.platform_ekip_uyeleri', N'U') IS NULL
            BEGIN
                RAISERROR('platform_ekip_uyeleri tablosu yok.', 16, 1);
                RETURN;
            END

            IF (@id IS NOT NULL AND EXISTS (SELECT 1 FROM dbo.platform_ekip_uyeleri WHERE id = @id))
            BEGIN
                UPDATE dbo.platform_ekip_uyeleri
                SET ad_soyad = @name,
                    unvan = @title,
                    eposta = @email,
                    aciklama = @desc,
                    avatar_url = COALESCE(NULLIF(@avatarUrl, N''), avatar_url),
                    siralama = @orderNo,
                    aktif_mi = @active,
                    guncellenme_tarihi = SYSUTCDATETIME()
                WHERE id = @id;
            END
            ELSE
            BEGIN
                INSERT INTO dbo.platform_ekip_uyeleri(ad_soyad, unvan, eposta, aciklama, avatar_url, siralama, aktif_mi, olusturulma_tarihi)
                VALUES(@name, @title, @email, @desc, @avatarUrl, @orderNo, @active, SYSUTCDATETIME());
            END";

        try
        {
            await using var cmd = new SqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@id", form.Id.HasValue ? form.Id.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@name", form.Name.Trim());
            cmd.Parameters.AddWithValue("@title", form.Title.Trim());
            cmd.Parameters.AddWithValue("@email", form.Email.Trim());
            cmd.Parameters.AddWithValue("@desc", string.IsNullOrWhiteSpace(form.Description) ? DBNull.Value : form.Description.Trim());
            cmd.Parameters.AddWithValue("@avatarUrl", string.IsNullOrWhiteSpace(avatarUrl) ? DBNull.Value : avatarUrl.Trim());
            cmd.Parameters.AddWithValue("@orderNo", form.OrderNo);
            cmd.Parameters.AddWithValue("@active", form.IsActive ? 1 : 0);
            await cmd.ExecuteNonQueryAsync(cancellationToken);
            return (true, form.Id.HasValue ? "Ekip üyesi güncellendi." : "Ekip üyesi eklendi.");
        }
        catch (Exception ex)
        {
            return (false, "Kaydedilemedi: " + ex.Message);
        }
    }

    public async Task<(bool Success, string Message)> DeletePlatformTeamMemberAsync(long adminUserId, long id, CancellationToken cancellationToken = default)
    {
        if (id <= 0) return (false, "Geçersiz kayıt.");
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        try
        {
            await using var cmd = new SqlCommand("DELETE FROM dbo.platform_ekip_uyeleri WHERE id=@id;", connection);
            cmd.Parameters.AddWithValue("@id", id);
            var affected = await cmd.ExecuteNonQueryAsync(cancellationToken);
            return affected > 0 ? (true, "Kayıt silindi.") : (false, "Kayıt bulunamadı.");
        }
        catch (SqlException ex) when (IsMissingTableOrColumn(ex))
        {
            return (false, "Tablo bulunamadı. Migration uygulanmalı.");
        }
        catch (Exception ex)
        {
            return (false, "Silinemedi: " + ex.Message);
        }
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

        // RBAC izin seti (menü görünürlüğü için)
        try
        {
            var rawUserId = _httpContextAccessor.HttpContext?.User?.FindFirstValue(otelturizmnew.Constants.AuthClaimTypes.UserId)
                            ?? _httpContextAccessor.HttpContext?.User?.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (long.TryParse(rawUserId, out var adminUserId) && adminUserId > 0)
            {
                shell.Permissions = await _adminRbacService.GetPermissionsAsync(adminUserId, userRole, cancellationToken);
            }
        }
        catch
        {
            // izin seti yüklenemezse boş bırak (controller endpoint guard devrede)
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
            "reports" => ("Gelir / Komisyon Raporu", "Otel bazında aylık rezervasyon adedi, ciro ve komisyon toplamlarını izleyin.", new[] { "Ay", "Otel", "Rezervasyon", "Ciro", "Brüt Komisyon", "Net Komisyon" }, "Rapor kaydı bulunamadı.", "Kaynak tablo: dbo.rezervasyonlar (komisyon snapshot)"),
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
            "reports" =>
            [
                ("30 Gün Ciro", "SELECT COALESCE(SUM(COALESCE(r.toplam_tutar,0)),0) FROM rezervasyonlar r WHERE COALESCE(r.durum,'') <> 'İptal Edildi' AND r.giris_tarihi >= DATEADD(day, -30, CAST(GETDATE() AS date))", "İptal hariç toplam", "success", "fa-money-bill-wave"),
                ("30 Gün Brüt Komisyon", "SELECT COALESCE(SUM(COALESCE(r.komisyon_tutari,0)),0) FROM rezervasyonlar r WHERE COALESCE(r.durum,'') <> 'İptal Edildi' AND r.giris_tarihi >= DATEADD(day, -30, CAST(GETDATE() AS date))", "Tahakkuk eden brüt", "info", "fa-percent"),
                ("30 Gün Net Komisyon", "SELECT COALESCE(SUM(COALESCE(r.platform_net_komisyon_tutari,0)),0) FROM rezervasyonlar r WHERE COALESCE(r.durum,'') <> 'İptal Edildi' AND r.giris_tarihi >= DATEADD(day, -30, CAST(GETDATE() AS date))", "Platform net", "primary", "fa-coins"),
                ("Bu Ay Rezervasyon", "SELECT COUNT(*) FROM rezervasyonlar r WHERE COALESCE(r.durum,'') <> 'İptal Edildi' AND r.giris_tarihi >= DATEFROMPARTS(YEAR(GETDATE()), MONTH(GETDATE()), 1)", "Ay başlangıcından bugüne", "warning", "fa-calendar-check")
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
            "reports" => @"SELECT TOP (240)
                                FORMAT(DATEFROMPARTS(YEAR(r.giris_tarihi), MONTH(r.giris_tarihi), 1), 'yyyy-MM', 'en-US') AS ay,
                                o.otel_adi AS otel,
                                CAST(COUNT(*) AS nvarchar(20)) AS rezervasyon,
                                FORMAT(COALESCE(SUM(COALESCE(r.toplam_tutar,0)),0), 'N0', 'tr-TR') AS ciro,
                                FORMAT(COALESCE(SUM(COALESCE(r.komisyon_tutari,0)),0), 'N0', 'tr-TR') AS brut_komisyon,
                                FORMAT(COALESCE(SUM(COALESCE(r.platform_net_komisyon_tutari,0)),0), 'N0', 'tr-TR') AS net_komisyon
                         FROM rezervasyonlar r
                         INNER JOIN oteller o ON o.id = r.otel_id
                         WHERE COALESCE(r.durum,'') <> 'İptal Edildi'
                           AND r.giris_tarihi >= DATEADD(month, -6, DATEFROMPARTS(YEAR(GETDATE()), MONTH(GETDATE()), 1))
                         GROUP BY DATEFROMPARTS(YEAR(r.giris_tarihi), MONTH(r.giris_tarihi), 1), o.otel_adi
                         ORDER BY DATEFROMPARTS(YEAR(r.giris_tarihi), MONTH(r.giris_tarihi), 1) DESC, COALESCE(SUM(COALESCE(r.toplam_tutar,0)),0) DESC;",
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

    public async Task<AdminReviewModerationPageViewModel> GetReviewModerationPageAsync(
        string fullName,
        string email,
        string userRole,
        string? q,
        string? city,
        string? hotel,
        int take,
        CancellationToken cancellationToken = default)
    {
        var shellPage = await GetSectionPageAsync("reviews", fullName, email, userRole, cancellationToken);
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var model = new AdminReviewModerationPageViewModel
        {
            Shell = shellPage.Shell,
            Q = string.IsNullOrWhiteSpace(q) ? null : q.Trim(),
            City = string.IsNullOrWhiteSpace(city) ? null : city.Trim(),
            Hotel = string.IsNullOrWhiteSpace(hotel) ? null : hotel.Trim(),
            Take = take is <= 0 or > 200 ? 20 : take
        };

        model.BlockedWords = await LoadBlockedWordsAsync(connection, cancellationToken);
        model.TakedownRequests = await LoadReviewTakedownRequestsAsync(connection, cancellationToken);
        model.Reviews = await LoadReviewsForModerationAsync(connection, model.Q, model.City, model.Hotel, model.Take, cancellationToken);
        return model;
    }

    public async Task<(bool Success, string Message)> ApplyReviewModerationActionAsync(long adminUserId, AdminReviewModerationActionForm form, CancellationToken cancellationToken = default)
    {
        var action = (form.Action ?? string.Empty).Trim().ToLowerInvariant();
        if (form.ReviewId <= 0)
        {
            return (false, "Yorum kaydı bulunamadı.");
        }
        if (action is not ("approve" or "unpublish" or "reject"))
        {
            return (false, "Geçersiz işlem.");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var tx = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            const string getSql = @"SELECT TOP (1) otel_id FROM yorumlar WHERE id = @id;";
            long hotelId;
            await using (var getCmd = new SqlCommand(getSql, connection, (SqlTransaction)tx))
            {
                getCmd.Parameters.AddWithValue("@id", form.ReviewId);
                var obj = await getCmd.ExecuteScalarAsync(cancellationToken);
                if (obj is null || obj is DBNull)
                {
                    return (false, "Yorum kaydı bulunamadı.");
                }
                hotelId = Convert.ToInt64(obj, CultureInfo.InvariantCulture);
            }

            var status = action switch
            {
                "approve" => "Onaylandı",
                "unpublish" => "Kaldırıldı",
                "reject" => "Reddedildi",
                _ => "Beklemede"
            };

            const string updSql = @"
UPDATE yorumlar
SET onay_durumu = @status,
    onaylayan_admin_id = @adminId,
    onay_tarihi = CASE WHEN @status LIKE N'Onaylan%' THEN SYSUTCDATETIME() ELSE onay_tarihi END,
    red_nedeni = CASE WHEN @status <> N'Onaylandı' THEN @note ELSE NULL END,
    guncellenme_tarihi = SYSUTCDATETIME()
WHERE id = @id;";
            await using (var updCmd = new SqlCommand(updSql, connection, (SqlTransaction)tx))
            {
                updCmd.Parameters.AddWithValue("@id", form.ReviewId);
                updCmd.Parameters.AddWithValue("@adminId", adminUserId);
                updCmd.Parameters.AddWithValue("@status", status);
                updCmd.Parameters.AddWithValue("@note", string.IsNullOrWhiteSpace(form.Note) ? (object)DBNull.Value : form.Note.Trim());
                await updCmd.ExecuteNonQueryAsync(cancellationToken);
            }

            if (hotelId > 0)
            {
                await RefreshHotelAggregatesFromApprovedReviewsAsync(connection, (SqlTransaction)tx, hotelId, cancellationToken);
            }

            await tx.CommitAsync(cancellationToken);
            return (true, action == "approve" ? "Yorum yayına alındı." : action == "unpublish" ? "Yorum yayından kaldırıldı." : "Yorum reddedildi.");
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(cancellationToken);
            return (false, $"İşlem başarısız: {ex.Message}");
        }
    }

    public async Task<(bool Success, string Message)> DeleteReviewAsAdminAsync(long adminUserId, AdminReviewDeleteForm form, CancellationToken cancellationToken = default)
    {
        if (form.ReviewId <= 0)
        {
            return (false, "Yorum kaydı bulunamadı.");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var tx = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            const string getSql = @"SELECT TOP (1) otel_id FROM yorumlar WHERE id = @id;";
            long hotelId;
            await using (var getCmd = new SqlCommand(getSql, connection, (SqlTransaction)tx))
            {
                getCmd.Parameters.AddWithValue("@id", form.ReviewId);
                var obj = await getCmd.ExecuteScalarAsync(cancellationToken);
                hotelId = obj is null || obj is DBNull ? 0 : Convert.ToInt64(obj, CultureInfo.InvariantCulture);
            }

            const string delSql = @"DELETE FROM yorumlar WHERE id = @id;";
            await using (var delCmd = new SqlCommand(delSql, connection, (SqlTransaction)tx))
            {
                delCmd.Parameters.AddWithValue("@id", form.ReviewId);
                var affected = await delCmd.ExecuteNonQueryAsync(cancellationToken);
                if (affected <= 0)
                {
                    return (false, "Yorum kaydı bulunamadı.");
                }
            }

            if (hotelId > 0)
            {
                await RefreshHotelAggregatesFromApprovedReviewsAsync(connection, (SqlTransaction)tx, hotelId, cancellationToken);
            }

            await tx.CommitAsync(cancellationToken);
            _ = adminUserId;
            return (true, "Yorum silindi.");
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(cancellationToken);
            return (false, $"Silme başarısız: {ex.Message}");
        }
    }

    public async Task<(bool Success, string Message)> NotifyReviewViolationAsync(long adminUserId, AdminReviewViolationNotifyForm form, CancellationToken cancellationToken = default)
    {
        if (form.UserId <= 0 || form.ReviewId <= 0)
        {
            return (false, "Geçersiz istek.");
        }

        var summary = string.IsNullOrWhiteSpace(form.RuleSummary)
            ? "Yorum içeriği topluluk kurallarına aykırı olabilir."
            : form.RuleSummary!.Trim();
        var note = string.IsNullOrWhiteSpace(form.AdminNote) ? null : form.AdminNote!.Trim();

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        const string sql = @"
INSERT INTO sistem_ici_bildirimler(
    kullanici_id, bildirim_turu, baslik, mesaj, ikon, renk,
    aksiyon_url, aksiyon_metni, onem_derecesi, ilgili_tablo, ilgili_kayit_id
)
VALUES(
    @userId, N'ReviewViolation', N'Yorum ihlali bildirimi',
    @msg, N'fa-shield-halved', N'danger',
    N'/panel/user/yorumlarim', N'Yorumlarım', N'High', N'yorumlar', @reviewId
);";
        var msg = note is null ? summary : $"{summary}\n\nNot: {note}";
        await using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@userId", form.UserId);
        cmd.Parameters.AddWithValue("@reviewId", form.ReviewId);
        cmd.Parameters.AddWithValue("@msg", msg);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
        _ = adminUserId;
        return (true, "Kullanıcıya ihlal bildirimi gönderildi.");
    }

    public async Task<(bool Success, string Message)> AddBlockedWordAsync(long adminUserId, AdminBlockedWordAddForm form, CancellationToken cancellationToken = default)
    {
        var word = (form.Word ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(word))
        {
            return (false, "Kelime boş olamaz.");
        }
        if (word.Length > 120)
        {
            return (false, "Kelime çok uzun.");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        const string sql = @"
IF EXISTS (SELECT 1 FROM dbo.blockyorumkelime WHERE kelime = @w)
BEGIN
    UPDATE dbo.blockyorumkelime
    SET aktif_mi = 1,
        aciklama = COALESCE(NULLIF(@d,''), aciklama),
        ekleyen_admin_id = COALESCE(@adminId, ekleyen_admin_id),
        guncellenme_tarihi = SYSUTCDATETIME()
    WHERE kelime = @w;
END
ELSE
BEGIN
    INSERT INTO dbo.blockyorumkelime(kelime, aktif_mi, aciklama, ekleyen_admin_id)
    VALUES(@w, 1, NULLIF(@d,''), @adminId);
END";
        await using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@w", word);
        cmd.Parameters.AddWithValue("@d", (object?)(form.Description ?? string.Empty) ?? string.Empty);
        cmd.Parameters.AddWithValue("@adminId", adminUserId);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
        return (true, "Yasaklı kelime kaydedildi.");
    }

    public async Task<(bool Success, string Message)> ToggleBlockedWordAsync(long adminUserId, AdminBlockedWordToggleForm form, CancellationToken cancellationToken = default)
    {
        if (form.Id <= 0)
        {
            return (false, "Kayıt bulunamadı.");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        const string sql = @"
UPDATE dbo.blockyorumkelime
SET aktif_mi = @active,
    ekleyen_admin_id = COALESCE(ekleyen_admin_id, @adminId),
    guncellenme_tarihi = SYSUTCDATETIME()
WHERE id = @id;";
        await using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@id", form.Id);
        cmd.Parameters.AddWithValue("@active", form.Active ? 1 : 0);
        cmd.Parameters.AddWithValue("@adminId", adminUserId);
        var affected = await cmd.ExecuteNonQueryAsync(cancellationToken);
        return affected > 0 ? (true, "Güncellendi.") : (false, "Kayıt bulunamadı.");
    }

    private static async Task RefreshHotelAggregatesFromApprovedReviewsAsync(SqlConnection connection, SqlTransaction transaction, long hotelId, CancellationToken cancellationToken)
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
            END) AS DECIMAL(9, 4))), 2) AS DECIMAL(5, 2)) AS avg_genel
    FROM yorumlar AS y
    WHERE y.otel_id = @hotelId
      AND y.onay_durumu LIKE N'Onaylan%'
    GROUP BY y.otel_id
)
UPDATE o
SET
    o.toplam_yorum_sayisi = agg.cnt,
    o.ortalama_puan = agg.avg_genel
FROM oteller AS o
INNER JOIN agg ON agg.otel_id = o.id;";
        await using var cmd = new SqlCommand(sql, connection, transaction);
        cmd.Parameters.AddWithValue("@hotelId", hotelId);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<List<AdminBlockedWordRowViewModel>> LoadBlockedWordsAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        var items = new List<AdminBlockedWordRowViewModel>();
        if (!await TableExistsAsync(connection, "blockyorumkelime", cancellationToken))
        {
            return items;
        }

        const string sql = @"
SELECT TOP (200) id, kelime, aktif_mi, aciklama, olusturulma_tarihi
FROM dbo.blockyorumkelime
ORDER BY aktif_mi DESC, id DESC;";
        await using var cmd = new SqlCommand(sql, connection);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new AdminBlockedWordRowViewModel
            {
                Id = reader.GetInt64(0),
                Word = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                Active = !reader.IsDBNull(2) && Convert.ToInt32(reader.GetValue(2), CultureInfo.InvariantCulture) == 1,
                Description = reader.IsDBNull(3) ? null : reader.GetString(3),
                CreatedText = reader.IsDBNull(4) ? "-" : reader.GetDateTime(4).ToString("dd.MM.yyyy HH:mm", CultureInfo.GetCultureInfo("tr-TR"))
            });
        }
        return items;
    }

    private static async Task<List<AdminReviewTakedownRequestRowViewModel>> LoadReviewTakedownRequestsAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        var items = new List<AdminReviewTakedownRequestRowViewModel>();
        if (!await TableExistsAsync(connection, "yorum_kaldirma_talepleri", cancellationToken))
        {
            return items;
        }

        const string sql = @"
SELECT TOP (40)
    t.id,
    t.yorum_id,
    COALESCE(t.otel_id, 0) AS otel_id,
    COALESCE(o.otel_adi, '') AS otel_adi,
    t.partner_kullanici_id,
    COALESCE(pu.eposta, '') AS partner_eposta,
    COALESCE(t.durum, 'Beklemede') AS durum,
    COALESCE(t.sebep, '') AS sebep,
    t.olusturulma_tarihi
FROM dbo.yorum_kaldirma_talepleri t
LEFT JOIN oteller o ON o.id = t.otel_id
LEFT JOIN users pu ON pu.id = t.partner_kullanici_id
ORDER BY CASE WHEN COALESCE(t.durum,'') = 'Beklemede' THEN 0 ELSE 1 END, t.olusturulma_tarihi DESC;";
        await using var cmd = new SqlCommand(sql, connection);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new AdminReviewTakedownRequestRowViewModel
            {
                RequestId = reader.GetInt64(0),
                ReviewId = reader.GetInt64(1),
                HotelId = Convert.ToInt64(reader.GetValue(2), CultureInfo.InvariantCulture),
                HotelName = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                PartnerUserId = reader.IsDBNull(4) ? 0 : reader.GetInt64(4),
                PartnerEmail = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
                StatusText = reader.IsDBNull(6) ? "Beklemede" : reader.GetString(6),
                Reason = reader.IsDBNull(7) ? null : reader.GetString(7),
                CreatedText = reader.IsDBNull(8) ? "-" : reader.GetDateTime(8).ToString("dd.MM.yyyy HH:mm", CultureInfo.GetCultureInfo("tr-TR"))
            });
        }
        return items;
    }

    private static async Task<List<AdminReviewModerationRowViewModel>> LoadReviewsForModerationAsync(SqlConnection connection, string? q, string? city, string? hotel, int take, CancellationToken cancellationToken)
    {
        var items = new List<AdminReviewModerationRowViewModel>();

        const string sql = @"
SELECT TOP (@take)
    y.id,
    y.otel_id,
    COALESCE(o.otel_adi,'') AS otel_adi,
    COALESCE(o.sehir,'') AS sehir,
    COALESCE(o.ilce,'') AS ilce,
    y.kullanici_id,
    COALESCE(u.ad_soyad, u.eposta, 'Kullanıcı') AS kullanici,
    COALESCE(y.genel_puan, 0) AS puan,
    COALESCE(y.onay_durumu, 'Beklemede') AS durum,
    COALESCE(y.rapor_sayisi, 0) AS rapor,
    y.olusturulma_tarihi,
    COALESCE(y.yorum_metni, '') AS yorum
FROM yorumlar y
LEFT JOIN oteller o ON o.id = y.otel_id
LEFT JOIN users u ON u.id = y.kullanici_id
WHERE (@q IS NULL OR (COALESCE(y.yorum_metni,'') LIKE N'%' + @q + N'%' OR COALESCE(o.otel_adi,'') LIKE N'%' + @q + N'%' OR COALESCE(u.ad_soyad,'') LIKE N'%' + @q + N'%' OR COALESCE(u.eposta,'') LIKE N'%' + @q + N'%'))
  AND (@city IS NULL OR COALESCE(o.sehir,'') LIKE N'%' + @city + N'%')
  AND (@hotel IS NULL OR COALESCE(o.otel_adi,'') LIKE N'%' + @hotel + N'%')
ORDER BY y.olusturulma_tarihi DESC, y.id DESC;";

        await using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@take", take);
        cmd.Parameters.AddWithValue("@q", string.IsNullOrWhiteSpace(q) ? (object)DBNull.Value : q);
        cmd.Parameters.AddWithValue("@city", string.IsNullOrWhiteSpace(city) ? (object)DBNull.Value : city);
        cmd.Parameters.AddWithValue("@hotel", string.IsNullOrWhiteSpace(hotel) ? (object)DBNull.Value : hotel);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var comment = reader.IsDBNull(11) ? string.Empty : reader.GetString(11);
            var snippet = comment.Length <= 220 ? comment : comment.Substring(0, 220) + "…";
            items.Add(new AdminReviewModerationRowViewModel
            {
                ReviewId = reader.GetInt64(0),
                HotelId = reader.IsDBNull(1) ? 0 : reader.GetInt64(1),
                HotelName = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                City = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                District = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                UserId = reader.IsDBNull(5) ? 0 : reader.GetInt64(5),
                UserName = reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
                Score = reader.IsDBNull(7) ? (byte)0 : Convert.ToByte(reader.GetValue(7), CultureInfo.InvariantCulture),
                StatusText = reader.IsDBNull(8) ? "Beklemede" : reader.GetString(8),
                ReportCount = reader.IsDBNull(9) ? (short)0 : Convert.ToInt16(reader.GetValue(9), CultureInfo.InvariantCulture),
                CreatedText = reader.IsDBNull(10) ? "-" : reader.GetDateTime(10).ToString("dd.MM.yyyy HH:mm", CultureInfo.GetCultureInfo("tr-TR")),
                CommentSnippet = snippet
            });
        }

        return items;
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

    private static string ResolveApprovalTone(string? status)
    {
        var normalized = (status ?? string.Empty).Trim().ToLowerInvariant();
        if (normalized.Contains("redded") || normalized.Contains("iptal"))
        {
            return "danger";
        }

        if (normalized.Contains("ask") || normalized.Contains("bekle") || normalized.Contains("kapali") || normalized.Contains("kapalı"))
        {
            return "warning";
        }

        if (normalized.Contains("onay") || normalized.Contains("yayin") || normalized.Contains("yayın") || normalized.Contains("odendi") || normalized.Contains("ödendi"))
        {
            return "success";
        }

        return "info";
    }

    private static bool IsMissingTableOrColumn(SqlException ex)
    {
        var msg = ex.Message ?? string.Empty;
        return msg.Contains("Invalid object name", StringComparison.OrdinalIgnoreCase)
               || msg.Contains("Invalid column name", StringComparison.OrdinalIgnoreCase);
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

    private static async Task<int> CountIfTableExistsAsync(SqlConnection connection, string tableName, string whereClause, CancellationToken cancellationToken)
    {
        if (!await TableExistsAsync(connection, tableName, cancellationToken))
        {
            return 0;
        }

        var sql = $"SELECT COUNT(*) FROM dbo.{tableName} WHERE {whereClause};";
        await using var command = new SqlCommand(sql, connection);
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken), CultureInfo.InvariantCulture);
    }

    private static async Task<AdminPlatformCheckupGroupViewModel> BuildTableCheckGroupAsync(
        SqlConnection connection,
        string title,
        string description,
        string toneClass,
        CancellationToken cancellationToken,
        params (string Label, string TableName, string ActionUrl)[] checks)
    {
        var group = new AdminPlatformCheckupGroupViewModel
        {
            Title = title,
            Description = description,
            ToneClass = toneClass
        };

        foreach (var check in checks)
        {
            var exists = await TableExistsAsync(connection, check.TableName, cancellationToken);
            group.Items.Add(new AdminPlatformCheckupItemViewModel
            {
                Label = check.Label,
                StatusText = exists ? "Hazır" : "Eksik",
                Detail = exists ? $"dbo.{check.TableName} şeması mevcut." : $"dbo.{check.TableName} bulunamadı; kurulum/migration planına alınmalı.",
                ToneClass = exists ? "success" : "warning",
                ActionUrl = check.ActionUrl
            });
        }

        return group;
    }

    private static string ResolvePreferredSenderEmail(string templateCode)
    {
        var code = (templateCode ?? string.Empty).Trim().ToLowerInvariant();
        return code switch
        {
            "login_2fa_email" => "guvenlik@otelturizm.com",
            "email_verify" => "guvenlik@otelturizm.com",
            "password_reset" => "guvenlik@otelturizm.com",
            "reservation_received_customer" => "rezervasyon@otelturizm.com",
            "reservation_confirmed_customer" => "rezervasyon@otelturizm.com",
            "reservation_new_partner" => "rezervasyon@otelturizm.com",
            "reservation_rejected_customer" => "rezervasyon@otelturizm.com",
            "reservation_guest_message" => "rezervasyon@otelturizm.com",
            "reservation_cancelled_partner" => "rezervasyon@otelturizm.com",
            "firma_reservation_created_company" => "rezervasyon@otelturizm.com",
            "firma_reservation_created_partner" => "rezervasyon@otelturizm.com",
            "favorite_price_alert_match" => "bildiri@otelturizm.com",
            "contract_delivery" => "bilgi@otelturizm.com",
            "system_health_link_report" => "bildiri@otelturizm.com",
            _ => "info@otelturizm.com"
        };
    }

    private static string ResolveTriggerArea(string templateCode)
    {
        var code = (templateCode ?? string.Empty).Trim().ToLowerInvariant();
        return code switch
        {
            "login_2fa_email" => "Giriş / 2FA",
            "email_verify" => "Kayıt / e-posta doğrulama",
            "password_reset" => "Şifre sıfırlama",
            "reservation_received_customer" => "Rezervasyon oluşturma",
            "reservation_confirmed_customer" => "Rezervasyon onayı",
            "reservation_new_partner" => "Partner yeni rezervasyon",
            "reservation_rejected_customer" => "Rezervasyon reddi",
            "reservation_guest_message" => "Rezervasyon mesajlaşma",
            "reservation_cancelled_partner" => "Misafir iptali",
            "favorite_price_alert_match" => "Favori fiyat alarmı",
            "contract_delivery" => "Sözleşme ve KVKK",
            "firma_reservation_created_company" => "Kurumsal rezervasyon / firma",
            "firma_reservation_created_partner" => "Kurumsal rezervasyon / partner",
            "system_health_link_report" => "Admin / sistem sağlığı",
            _ => "Genel sistem akışı"
        };
    }

    private static string NormalizeBrokenTurkish(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        return text
            .Replace("Ä±", "ı", StringComparison.Ordinal)
            .Replace("Ä°", "İ", StringComparison.Ordinal)
            .Replace("ÅŸ", "ş", StringComparison.Ordinal)
            .Replace("Åž", "Ş", StringComparison.Ordinal)
            .Replace("Ã¼", "ü", StringComparison.Ordinal)
            .Replace("Ãœ", "Ü", StringComparison.Ordinal)
            .Replace("Ã¶", "ö", StringComparison.Ordinal)
            .Replace("Ã–", "Ö", StringComparison.Ordinal)
            .Replace("Ã§", "ç", StringComparison.Ordinal)
            .Replace("Ã‡", "Ç", StringComparison.Ordinal)
            .Replace("ÄŸ", "ğ", StringComparison.Ordinal)
            .Replace("Äž", "Ğ", StringComparison.Ordinal);
    }

    public async Task<AdminShellViewModel> GetShellForEmailRoutingAsync(string fullName, string email, string userRole, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        return await GetShellAsync(
            connection,
            "E-posta yönlendirmeleri",
            "Partner/firma kayıtları, rezervasyon, ödeme, şikayet ve sistem bildirimleri için olay bazında hedef adresleri buradan yönetin.",
            fullName,
            email,
            userRole,
            cancellationToken);
    }
}

