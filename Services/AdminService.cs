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
    private readonly ISecureFileService _secureFileService;

    private static readonly string[] PartnerRequiredDocumentTypes =
    [
        "Vergi Levhasi",
        "Ticaret Sicil Gazetesi",
        "Imza Sirkuleri",
        "IBAN Belgesi",
        "Turizm Belgesi",
        "Kimlik Belgesi"
    ];

    public AdminService(
        IConfiguration configuration,
        IDataProtectionProvider dataProtectionProvider,
        IEmailQueueService emailQueueService,
        IAdminRbacService adminRbacService,
        IHttpContextAccessor httpContextAccessor,
        CommerceMetricsAccumulator commerceMetrics,
        IGrowthGovernanceService growthGovernance,
        HealthCheckService healthCheckService,
        ISecureFileService secureFileService)
    {
        _configuration = configuration;
        _mailAccountProtector = dataProtectionProvider.CreateProtector("Admin.PlatformMailAccounts.v1");
        _emailQueueService = emailQueueService;
        _adminRbacService = adminRbacService;
        _httpContextAccessor = httpContextAccessor;
        _commerceMetrics = commerceMetrics;
        _growthGovernance = growthGovernance;
        _healthCheckService = healthCheckService;
        _secureFileService = secureFileService;
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
                (SELECT COUNT(*) FROM [dbo].[OTELLER]) AS total_hotels,
                (SELECT COUNT(*) FROM [dbo].[REZERVASYONLAR]) AS total_reservations,
                (SELECT COUNT(*) FROM [dbo].[REZERVASYONLAR] WHERE [DURUM] = 'İptal Edildi') AS cancelled_reservations,
                (SELECT COALESCE(SUM(COALESCE([TOPLAM_TUTAR],0)),0) FROM [dbo].[REZERVASYONLAR] WHERE COALESCE([DURUM],'') <> 'İptal Edildi') AS gross_revenue,
                (SELECT COALESCE(SUM(COALESCE([KOMISYON_TUTARI],0)),0) FROM [dbo].[KOMISYON_MUHASEBE_KAYITLARI]) AS total_commission,
                (SELECT COUNT(*) FROM [dbo].[ODEME_ISLEMLERI] WHERE [ODEME_DURUMU] IN ('Başarılı','Geri Ödendi','Kısmi Geri Ödendi')) AS successful_payments,
                (SELECT COUNT(*) FROM [dbo].[KULLANICILAR] WHERE rol = 'admin') AS admin_count,
                (SELECT COUNT(*) FROM [dbo].[PARTNER_DETAYLARI] WHERE [ONAY_DURUMU] = 'Beklemede') AS pending_partner_count,
                (SELECT COUNT(*) FROM [dbo].[FIRMALAR] WHERE COALESCE([ONAY_DURUMU], 'Beklemede') = 'Beklemede') AS pending_company_count,
                (SELECT COUNT(*) FROM [dbo].[OTELLER] WHERE [YAYIN_DURUMU] = 'Yayında' AND [ONAY_DURUMU] = 'Onaylandı') AS active_hotel_count,
                (SELECT COUNT(*) FROM [dbo].[OTELLER] WHERE COALESCE([ONAY_DURUMU], '') = 'Beklemede') AS pending_hotel_count;";

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
                model.PendingApprovalsQueueCount = shell.PendingPartnerApplications + shell.PendingCompanyApplications + SafeInt(reader, 10);
            }
        }

        const string revenue30Sql = @"
            SELECT
                COALESCE(SUM(COALESCE(r.[TOPLAM_TUTAR], 0)), 0) AS gmv_30d,
                COALESCE(SUM(COALESCE(r.[KOMISYON_TUTARI], 0)), 0) AS commission_30d,
                COUNT(*) AS reservation_count_30d
            FROM [dbo].[REZERVASYONLAR] r
            WHERE COALESCE(r.[DURUM], N'') <> N'İptal Edildi'
              AND r.[OLUSTURULMA_TARIHI] >= DATEADD(day, -30, CAST(GETDATE() AS date));";

        await using (var revenue30Command = new SqlCommand(revenue30Sql, connection))
        await using (var revenue30Reader = await revenue30Command.ExecuteReaderAsync(cancellationToken))
        {
            if (await revenue30Reader.ReadAsync(cancellationToken))
            {
                model.Revenue30DayMetrics.Add(new AdminMetricCardViewModel
                {
                    Label = "Platform GMV (30 gün)",
                    Value = $"{SafeDecimal(revenue30Reader, 0):N0} TL",
                    TrendText = "İptal hariç rezervasyon cirosu",
                    IconClass = "fa-chart-line",
                    ToneClass = "success"
                });
                model.Revenue30DayMetrics.Add(new AdminMetricCardViewModel
                {
                    Label = "Platform komisyon (30 gün)",
                    Value = $"{SafeDecimal(revenue30Reader, 1):N0} TL",
                    TrendText = "Rezervasyon snapshot komisyonu",
                    IconClass = "fa-percent",
                    ToneClass = "warning"
                });
                model.Revenue30DayMetrics.Add(new AdminMetricCardViewModel
                {
                    Label = "Rezervasyon (30 gün)",
                    Value = SafeInt(revenue30Reader, 2).ToString(),
                    TrendText = "Son 30 günde oluşturulan kayıtlar",
                    IconClass = "fa-calendar-check",
                    ToneClass = "info"
                });
            }
        }

        const string chartSql = @"
            SELECT FORMAT([OLUSTURULMA_TARIHI], 'MMM', 'tr-TR') AS ay, COUNT(*) AS adet
            FROM [dbo].[REZERVASYONLAR]
            WHERE [OLUSTURULMA_TARIHI] >= DATEADD(MONTH, -5, DATEFROMPARTS(YEAR(GETDATE()), MONTH(GETDATE()), 1))
            GROUP BY YEAR([OLUSTURULMA_TARIHI]), MONTH([OLUSTURULMA_TARIHI]), FORMAT([OLUSTURULMA_TARIHI], 'MMM', 'tr-TR')
            ORDER BY YEAR([OLUSTURULMA_TARIHI]), MONTH([OLUSTURULMA_TARIHI]);";

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
                SELECT 'Partner basvurusu' AS [BASLIK],
                       CONCAT(p.[FIRMA_UNVANI], ' · ', p.[ONAY_DURUMU]) AS [ALT_BASLIK],
                       p.[OLUSTURULMA_TARIHI] AS zaman
                FROM [dbo].[PARTNER_DETAYLARI] p
                UNION ALL
                SELECT 'Admin islemi',
                       CONCAT(a.[HEDEF_TABLO], ' · ', a.[ISLEM_TURU]),
                       a.[ISLEM_TARIHI]
                FROM [dbo].[ADMIN_ISLEM_LOGLARI] a
                UNION ALL
                SELECT 'Sistem hatasi',
                       CONCAT(s.[HATA_SEVIYESI], ' · ', LEFT(s.[HATA_MESAJI], 70)),
                       s.[OLUSMA_TARIHI]
                FROM [dbo].[SISTEM_HATA_LOGLARI] s
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
                o.[OTEL_ADI],
                CONCAT(o.[ILCE], ', ', o.[SEHIR]) AS sehir_label,
                o.[YAYIN_DURUMU],
                o.[ORTALAMA_PUAN],
                COUNT(r.id) AS rezervasyon_adedi
            FROM [dbo].[OTELLER] o
            LEFT JOIN [dbo].[REZERVASYONLAR] r ON r.[OTEL_ID] = o.id
            GROUP BY o.id, o.[OTEL_ADI], o.[ILCE], o.[SEHIR], o.[YAYIN_DURUMU], o.[ORTALAMA_PUAN]
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
                o.[OTEL_ADI],
                CONCAT(o.[ILCE], ', ', o.[SEHIR]) AS sehir_label,
                o.[YAYIN_DURUMU],
                COALESCE(resStats.res_count, 0) AS res_count,
                COALESCE(resStats.cancel_count, 0) AS cancel_count,
                COALESCE(resStats.gross_revenue, 0) AS gross_revenue,
                COALESCE(commStats.commission_amount, 0) AS commission_amount,
                COALESCE(o.[TOPLAM_YORUM_SAYISI], 0) AS review_count,
                COALESCE(o.[ORTALAMA_PUAN], 0) AS avg_score
            FROM [dbo].[OTELLER] o
            OUTER APPLY
            (
                SELECT
                    COUNT(*) AS res_count,
                    SUM(CASE WHEN r.[DURUM] = 'İptal Edildi' THEN 1 ELSE 0 END) AS cancel_count,
                    SUM(CASE WHEN COALESCE(r.[DURUM],'') <> 'İptal Edildi' THEN COALESCE(r.[TOPLAM_TUTAR],0) ELSE 0 END) AS gross_revenue
                FROM [dbo].[REZERVASYONLAR] r
                WHERE r.[OTEL_ID] = o.id
            ) resStats
            OUTER APPLY
            (
                SELECT SUM(COALESCE(k.[KOMISYON_TUTARI],0)) AS commission_amount
                FROM [dbo].[KOMISYON_MUHASEBE_KAYITLARI] k
                WHERE k.[OTEL_ID] = o.id
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

        var countSql = $"SELECT COUNT(*) FROM [dbo].[ADMIN_ISLEM_LOGLARI] a {whereSql};";
        await using (var countCmd = new SqlCommand(countSql, connection))
        {
            countCmd.Parameters.AddRange(prms.ToArray());
            model.Total = Convert.ToInt32(await countCmd.ExecuteScalarAsync(cancellationToken) ?? 0, CultureInfo.InvariantCulture);
        }

        var offset = (safePage - 1) * safePageSize;
        var listSql = $"""
            SELECT a.id, a.[ADMIN_KULLANICI_ID], a.[ISLEM_TURU], a.[HEDEF_TABLO], a.[HEDEF_KAYIT_ID], a.[ACIKLAMA], a.[IP_ADRESI], a.[ISLEM_TARIHI]
            FROM [dbo].[ADMIN_ISLEM_LOGLARI] a
            {whereSql}
            ORDER BY a.[ISLEM_TARIHI] {sort}, a.id {sort}
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

        var hasNextAttempt = await ColumnExistsAsync(connection, "[dbo].[BILDIRIM_LOGLARI]", "sonraki_deneme_utc", cancellationToken);
        var hasMaxAttemptsColumn = await ColumnExistsAsync(connection, "[dbo].[BILDIRIM_LOGLARI]", "maksimum_deneme", cancellationToken);
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

        var countSql = $"SELECT COUNT(*) FROM [dbo].[BILDIRIM_LOGLARI] b {whereSql};";
        await using (var countCmd = new SqlCommand(countSql, connection))
        {
            countCmd.Parameters.AddRange(prms.ToArray());
            model.Total = Convert.ToInt32(await countCmd.ExecuteScalarAsync(cancellationToken) ?? 0, CultureInfo.InvariantCulture);
        }

        var offset = (safePage - 1) * safePageSize;
        var listSql = hasNextAttempt
            ? $"""
                SELECT b.id, COALESCE(b.[KULLANICI_ID], 0), COALESCE(b.[ALICI_EPOSTA],''), COALESCE(b.[KONU],''),
                       COALESCE(b.[DURUM],''), COALESCE(b.[SAGLAYICI_MESAJ_ID],''), COALESCE(b.[GONDERME_DENEMESI],0), {maxAttemptsSql},
                       COALESCE(b.[OLUSTURULMA_TARIHI], SYSUTCDATETIME()) AS created_at,
                       b.sonraki_deneme_utc,
                       COALESCE(b.[HATA_MESAJI],'')
                FROM [dbo].[BILDIRIM_LOGLARI] b
                {whereSql}
                ORDER BY b.id DESC
                OFFSET @offset ROWS FETCH NEXT @take ROWS ONLY;
                """
            : $"""
                SELECT b.id, COALESCE(b.[KULLANICI_ID], 0), COALESCE(b.[ALICI_EPOSTA],''), COALESCE(b.[KONU],''),
                       COALESCE(b.[DURUM],''), COALESCE(b.[SAGLAYICI_MESAJ_ID],''), COALESCE(b.[GONDERME_DENEMESI],0), {maxAttemptsSql},
                       COALESCE(b.[OLUSTURULMA_TARIHI], SYSUTCDATETIME()) AS created_at,
                       COALESCE(b.[HATA_MESAJI],'')
                FROM [dbo].[BILDIRIM_LOGLARI] b
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
                [SERVIS_KODU],
                [SERVIS_ADI],
                COALESCE([GONDEREN_AD], N''),
                COALESCE([GONDEREN_EPOSTA], N''),
                [YANITLA_EPOSTA],
                COALESCE([SAGLAYICI], N''),
                COALESCE([SMTP_HOST], N''),
                COALESCE([SMTP_PORT], 0),
                COALESCE([GUVENLIK_TIPI], N''),
                COALESCE([AKTIF_MI], 0),
                COALESCE([VARSAYILAN_MI], 0),
                COALESCE([TEST_MODU], 0),
                [SON_BASARILI_TEST_TARIHI],
                [SON_HATA_TARIHI],
                [SON_HATA_MESAJI]
            FROM [dbo].[EPOSTA_SERVISLERI]
            ORDER BY [VARSAYILAN_MI] DESC, [AKTIF_MI] DESC, id ASC;
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
                COALESCE([SABLON_KODU], N''),
                COALESCE([SABLON_ADI], N''),
                COALESCE(dil, N'tr'),
                COALESCE(konu, N''),
                COALESCE([ICERIK], N'')
            FROM [dbo].[BILDIRIM_SABLONLARI]
            WHERE tur = N'E-posta'
              AND COALESCE([AKTIF_MI], 1) = 1
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
                SUM(CASE WHEN [DURUM] = N'Beklemede' THEN 1 ELSE 0 END) AS beklemede,
                SUM(CASE WHEN [DURUM] IN (N'Gönderildi', N'SMTP Kabul', N'Dosyaya Yazıldı') THEN 1 ELSE 0 END) AS smtp_kabul,
                SUM(CASE WHEN [DURUM] = N'Başarısız' THEN 1 ELSE 0 END) AS basarisiz
            FROM [dbo].[BILDIRIM_LOGLARI]
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
                UPDATE [dbo].[PLATFORM_EPOSTA_HESAPLARI]
                SET [HESAP_KODU] = @code,
                    [HESAP_ADI] = @name,
                    [EPOSTA_ADRESI] = @email,
                    [GELEN_PROTOKOL] = @protocol,
                    [GELEN_SUNUCU] = @incomingHost,
                    [GELEN_PORT] = @incomingPort,
                    [GELEN_SSL] = @incomingSsl,
                    [GIDEN_SUNUCU] = @outgoingHost,
                    [GIDEN_PORT] = @outgoingPort,
                    [GIDEN_GUVENLIK_TIPI] = @outgoingSecurity,
                    [KULLANICI_ADI] = @username,
                    [SIFRE_SIFRELI] = @password,
                    [AKTIF_MI] = @active,
                    [VARSAYILAN_GONDEREN_MI] = @defaultSender,
                    [GUNCELLENME_TARIHI] = SYSUTCDATETIME()
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
                INSERT INTO [dbo].[PLATFORM_EPOSTA_HESAPLARI]
                (
                    [HESAP_KODU], [HESAP_ADI], [EPOSTA_ADRESI], [GELEN_PROTOKOL], [GELEN_SUNUCU], [GELEN_PORT], [GELEN_SSL],
                    [GIDEN_SUNUCU], [GIDEN_PORT], [GIDEN_GUVENLIK_TIPI], [KULLANICI_ADI], [SIFRE_SIFRELI], [AKTIF_MI], [VARSAYILAN_GONDEREN_MI]
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
        const string deleteMessagesSql = "DELETE FROM [dbo].[PLATFORM_EPOSTA_MESAJLARI] WHERE [HESAP_ID] = @id;";
        await using (var deleteMessagesCmd = new SqlCommand(deleteMessagesSql, connection))
        {
            deleteMessagesCmd.Parameters.AddWithValue("@id", accountId);
            await deleteMessagesCmd.ExecuteNonQueryAsync(cancellationToken);
        }

        await DeleteEmailServiceByAccountIdAsync(connection, accountId, cancellationToken);

        const string deleteAccountSql = "DELETE FROM [dbo].[PLATFORM_EPOSTA_HESAPLARI] WHERE id = @id;";
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
            SELECT [SABLON_KODU], [SABLON_ADI]
            FROM [dbo].[BILDIRIM_SABLONLARI]
            WHERE tur = N'E-posta'
              AND [AKTIF_MI] = 1
            ORDER BY [SABLON_KODU] ASC;
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
                RelatedTable = "KULLANICILAR",
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
                id, [HESAP_KODU], [HESAP_ADI], [EPOSTA_ADRESI], [GELEN_PROTOKOL], [GELEN_SUNUCU], [GELEN_PORT], [GELEN_SSL],
                [GIDEN_SUNUCU], [GIDEN_PORT], [GIDEN_GUVENLIK_TIPI], [AKTIF_MI], [VARSAYILAN_GONDEREN_MI], [SON_SENKRON_TARIHI], [SON_HATA_MESAJI]
            FROM [dbo].[PLATFORM_EPOSTA_HESAPLARI]
            ORDER BY [VARSAYILAN_GONDEREN_MI] DESC, [EPOSTA_ADRESI] ASC;
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
            SELECT TOP (1) COALESCE([GONDEREN_EPOSTA], N'')
            FROM [dbo].[EPOSTA_SERVISLERI]
            WHERE [AKTIF_MI] = 1
            ORDER BY [VARSAYILAN_MI] DESC, id ASC;
            """;
        await using var command = new SqlCommand(sql, connection);
        return Convert.ToString(await command.ExecuteScalarAsync(cancellationToken), CultureInfo.InvariantCulture) ?? string.Empty;
    }

    private async Task<List<AdminIncomingEmailRowViewModel>> LoadIncomingEmailsAsync(SqlConnection connection, long? accountId, CancellationToken cancellationToken)
    {
        var sql = """
            SELECT TOP (80)
                m.id, m.[HESAP_ID], h.[EPOSTA_ADRESI], COALESCE(m.[KLASOR], N'INBOX'), COALESCE(m.[GONDEREN], N''),
                COALESCE(m.[KONU], N''), COALESCE(m.[OZET], N''), m.[INTERNET_MESAJ_KIMLIGI], m.[TARIH_UTC],
                COALESCE(m.[OKUNMUS_MU], 0), COALESCE(m.[SPAM_MI], 0)
            FROM [dbo].[PLATFORM_EPOSTA_MESAJLARI] m
            INNER JOIN [dbo].[PLATFORM_EPOSTA_HESAPLARI] h ON h.id = m.[HESAP_ID]
            WHERE m.[YON] = N'Gelen'
              AND (@accountId IS NULL OR m.[HESAP_ID] = @accountId)
            ORDER BY COALESCE(m.[TARIH_UTC], m.[OLUSTURULMA_TARIHI]) DESC, m.id DESC;
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
        var hasSenderOverrideColumn = await TableColumnExistsAsync(connection, "dbo", "BILDIRIM_LOGLARI", "GONDEREN_EPOSTA_OVERRIDE", cancellationToken);
        var sql = hasSenderOverrideColumn
            ? """
                SELECT TOP (80)
                    b.id,
                    b.[KULLANICI_ID],
                    COALESCE(b.[ALICI_EPOSTA], N''),
                    COALESCE(b.[KONU], N''),
                    COALESCE(b.[DURUM], N''),
                    b.[SAGLAYICI_MESAJ_ID],
                    b.[GONDERIM_TARIHI],
                    COALESCE(b.[OLUSTURULMA_TARIHI], SYSUTCDATETIME()),
                    COALESCE(b.[GONDEREN_EPOSTA_OVERRIDE], N'')
                FROM [dbo].[BILDIRIM_LOGLARI] b
                WHERE b.[TUR] = N'E-posta'
                ORDER BY COALESCE(b.[GONDERIM_TARIHI], b.[OLUSTURULMA_TARIHI]) DESC, b.id DESC;
                """
            : """
                SELECT TOP (80)
                    b.id,
                    b.[KULLANICI_ID],
                    COALESCE(b.[ALICI_EPOSTA], N''),
                    COALESCE(b.[KONU], N''),
                    COALESCE(b.[DURUM], N''),
                    b.[SAGLAYICI_MESAJ_ID],
                    b.[GONDERIM_TARIHI],
                    COALESCE(b.[OLUSTURULMA_TARIHI], SYSUTCDATETIME()),
                    N''
                FROM [dbo].[BILDIRIM_LOGLARI] b
                WHERE b.[TUR] = N'E-posta'
                ORDER BY COALESCE(b.[GONDERIM_TARIHI], b.[OLUSTURULMA_TARIHI]) DESC, b.id DESC;
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
        const string sql = "SELECT [SIFRE_SIFRELI] FROM [dbo].[PLATFORM_EPOSTA_HESAPLARI] WHERE id = @id;";
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
            UPDATE [dbo].[PLATFORM_EPOSTA_HESAPLARI]
            SET [VARSAYILAN_GONDEREN_MI] = 0,
                [GUNCELLENME_TARIHI] = SYSUTCDATETIME()
            WHERE [EPOSTA_ADRESI] <> @email
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
            MERGE [dbo].[EPOSTA_SERVISLERI] AS target
            USING (
                SELECT
                    @serviceCode AS [SERVIS_KODU],
                    @serviceName AS [SERVIS_ADI],
                    N'SMTP' AS [SAGLAYICI],
                    @isDefault AS [VARSAYILAN_MI],
                    @isActive AS [AKTIF_MI],
                    @senderName AS [GONDEREN_AD],
                    @senderEmail AS [GONDEREN_EPOSTA],
                    @replyTo AS [YANITLA_EPOSTA],
                    @smtpHost AS [SMTP_HOST],
                    @smtpPort AS [SMTP_PORT],
                    @smtpUsername AS [SMTP_KULLANICI_ADI],
                    @smtpPassword AS [SMTP_SIFRE],
                    CAST(0 AS bit) AS [SIFRE_SIFRELENMIS_MI],
                    @securityType AS [GUVENLIK_TIPI],
                    CAST(45 AS smallint) AS [BAGLANTI_ZAMAN_ASIMI_SANIYE],
                    CAST(0 AS bit) AS [TEST_MODU],
                    @metadata AS [METADATA]
            ) AS src
            ON target.[SERVIS_KODU] = src.[SERVIS_KODU]
            WHEN MATCHED THEN
                UPDATE SET
                    [SERVIS_ADI] = src.[SERVIS_ADI],
                    [SAGLAYICI] = src.[SAGLAYICI],
                    [VARSAYILAN_MI] = src.[VARSAYILAN_MI],
                    [AKTIF_MI] = src.[AKTIF_MI],
                    [GONDEREN_AD] = src.[GONDEREN_AD],
                    [GONDEREN_EPOSTA] = src.[GONDEREN_EPOSTA],
                    [YANITLA_EPOSTA] = src.[YANITLA_EPOSTA],
                    [SMTP_HOST] = src.[SMTP_HOST],
                    [SMTP_PORT] = src.[SMTP_PORT],
                    [SMTP_KULLANICI_ADI] = src.[SMTP_KULLANICI_ADI],
                    [SMTP_SIFRE] = src.[SMTP_SIFRE],
                    [SIFRE_SIFRELENMIS_MI] = src.[SIFRE_SIFRELENMIS_MI],
                    [GUVENLIK_TIPI] = src.[GUVENLIK_TIPI],
                    [BAGLANTI_ZAMAN_ASIMI_SANIYE] = src.[BAGLANTI_ZAMAN_ASIMI_SANIYE],
                    [TEST_MODU] = src.[TEST_MODU],
                    [METADATA] = src.[METADATA],
                    [GUNCELLENME_TARIHI] = SYSUTCDATETIME()
            WHEN NOT MATCHED THEN
                INSERT
                (
                    [SERVIS_KODU], [SERVIS_ADI], [SAGLAYICI], [VARSAYILAN_MI], [AKTIF_MI], [GONDEREN_AD], [GONDEREN_EPOSTA],
                    [YANITLA_EPOSTA], [SMTP_HOST], [SMTP_PORT], [SMTP_KULLANICI_ADI], [SMTP_SIFRE], [SIFRE_SIFRELENMIS_MI],
                    [GUVENLIK_TIPI], [BAGLANTI_ZAMAN_ASIMI_SANIYE], [TEST_MODU], [METADATA]
                )
                VALUES
                (
                    src.[SERVIS_KODU], src.[SERVIS_ADI], src.[SAGLAYICI], src.[VARSAYILAN_MI], src.[AKTIF_MI], src.[GONDEREN_AD], src.[GONDEREN_EPOSTA],
                    src.[YANITLA_EPOSTA], src.[SMTP_HOST], src.[SMTP_PORT], src.[SMTP_KULLANICI_ADI], src.[SMTP_SIFRE], src.[SIFRE_SIFRELENMIS_MI],
                    src.[GUVENLIK_TIPI], src.[BAGLANTI_ZAMAN_ASIMI_SANIYE], src.[TEST_MODU], src.[METADATA]
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
            FROM [dbo].[EPOSTA_SERVISLERI] s
            INNER JOIN [dbo].[PLATFORM_EPOSTA_HESAPLARI] h ON LOWER(h.[HESAP_KODU]) = LOWER(REPLACE(s.[SERVIS_KODU], N'platform_', N''))
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
            ["hotel_link"] = "https://otelturizm.com/hotel/216-eagle-palace",
            ["favorite_link"] = "https://otelturizm.com/kullanici/favoriler",
            ["reservation_link"] = "https://otelturizm.com/kullanici/rezervasyonlarim",
            ["message_link"] = "https://otelturizm.com/kullanici/mesajlar"
        };
    }

    private async Task<PlatformMailAccountEntity?> LoadMailAccountEntityAsync(SqlConnection connection, long accountId, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT TOP (1)
                id, [HESAP_KODU], [HESAP_ADI], [EPOSTA_ADRESI], [GELEN_PROTOKOL], [GELEN_SUNUCU], [GELEN_PORT], [GELEN_SSL],
                [GIDEN_SUNUCU], [GIDEN_PORT], [GIDEN_GUVENLIK_TIPI], [KULLANICI_ADI], [SIFRE_SIFRELI], [AKTIF_MI]
            FROM [dbo].[PLATFORM_EPOSTA_HESAPLARI]
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
            FROM [dbo].[PLATFORM_EPOSTA_MESAJLARI]
            WHERE [HESAP_ID] = @accountId
              AND yon = N'Gelen'
              AND [KLASOR] = @folder
              AND [UID_DEGERI] = @uid;
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
                UPDATE [dbo].[PLATFORM_EPOSTA_MESAJLARI]
                SET konu = @subject,
                    [GONDEREN] = @from,
                    [ALICILAR] = @to,
                    cc = @cc,
                    [TARIH_UTC] = @dateUtc,
                    ozet = @summary,
                    [HTML_ICERIK] = @htmlBody,
                    [METIN_ICERIK] = @textBody,
                    [OKUNMUS_MU] = 1,
                    [HAM_BASLIKLAR] = @headers,
                    [GUNCELLENME_TARIHI] = SYSUTCDATETIME(),
                    [SENKRON_TARIHI] = SYSUTCDATETIME()
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
            INSERT INTO [dbo].[PLATFORM_EPOSTA_MESAJLARI]
            (
                [HESAP_ID], yon, [KLASOR], [UID_DEGERI], [INTERNET_MESAJ_KIMLIGI], konu, [GONDEREN], [ALICILAR], cc,
                [TARIH_UTC], ozet, [HTML_ICERIK], [METIN_ICERIK], [OKUNMUS_MU], [SPAM_MI], [HAM_BASLIKLAR]
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
            UPDATE [dbo].[PLATFORM_EPOSTA_HESAPLARI]
            SET [SON_SENKRON_TARIHI] = SYSUTCDATETIME(),
                [SON_HATA_MESAJI] = NULL,
                [GUNCELLENME_TARIHI] = SYSUTCDATETIME()
            WHERE id = @id;
            """;
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@id", accountId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task UpdateMailAccountSyncErrorAsync(SqlConnection connection, long accountId, string errorMessage, CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE [dbo].[PLATFORM_EPOSTA_HESAPLARI]
            SET [SON_HATA_MESAJI] = @error,
                [GUNCELLENME_TARIHI] = SYSUTCDATETIME()
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
            INSERT INTO [dbo].[ADMIN_ISLEM_LOGLARI] ([ADMIN_KULLANICI_ID], [ISLEM_TURU], [HEDEF_TABLO], [HEDEF_KAYIT_ID], [ACIKLAMA], [IP_ADRESI], [ISLEM_TARIHI])
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

        var hasNextAttempt = await ColumnExistsAsync(connection, "[dbo].[BILDIRIM_LOGLARI]", "sonraki_deneme_utc", cancellationToken);
        var sql = hasNextAttempt
            ? """
                UPDATE [dbo].[BILDIRIM_LOGLARI]
                SET [DURUM] = N'Beklemede',
                    [HATA_MESAJI] = NULL,
                    [HATA_KODU] = NULL,
                    sonraki_deneme_utc = NULL
                WHERE id = @id;
                """
            : """
                UPDATE [dbo].[BILDIRIM_LOGLARI]
                SET [DURUM] = N'Beklemede',
                    [HATA_MESAJI] = NULL,
                    [HATA_KODU] = NULL
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

        var hasNextAttempt = await ColumnExistsAsync(connection, "[dbo].[BILDIRIM_LOGLARI]", "sonraki_deneme_utc", cancellationToken);
        var sql = hasNextAttempt
            ? """
                UPDATE [dbo].[BILDIRIM_LOGLARI]
                SET [DURUM] = N'Beklemede',
                    [HATA_MESAJI] = NULL,
                    [HATA_KODU] = NULL,
                    sonraki_deneme_utc = NULL
                WHERE tur = N'E-posta'
                  AND [DURUM] = N'Başarısız';
                """
            : """
                UPDATE [dbo].[BILDIRIM_LOGLARI]
                SET [DURUM] = N'Beklemede',
                    [HATA_MESAJI] = NULL,
                    [HATA_KODU] = NULL
                WHERE tur = N'E-posta'
                  AND [DURUM] = N'Başarısız';
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
            UPDATE [dbo].[BILDIRIM_LOGLARI]
            SET [DURUM] = N'Başarısız'
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
            FROM [dbo].[REZERVASYONLAR] r
            LEFT JOIN [dbo].[OTELLER] o ON o.id = r.[OTEL_ID]
            LEFT JOIN [dbo].[KULLANICILAR] u ON u.id = r.[KULLANICI_ID]
            LEFT JOIN [dbo].[FIRMALAR] f ON f.id = r.[FIRMA_ID]
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
                   COALESCE(o.[OTEL_ADI],'-') AS [OTEL_ADI],
                   COALESCE(u.[AD_SOYAD],'') AS musteri_adi,
                   COALESCE(u.[EPOSTA],'') AS musteri_eposta,
                   COALESCE(f.[FIRMA_ADI],'') AS [FIRMA_ADI],
                   COALESCE(r.[DURUM],'') AS [DURUM],
                   COALESCE(r.[TOPLAM_TUTAR],0) AS [TOPLAM_TUTAR],
                   COALESCE(r.[PARA_BIRIMI],'TRY') AS [PARA_BIRIMI],
                   COALESCE(r.[OLUSTURULMA_TARIHI], SYSUTCDATETIME()) AS created_at,
                   COALESCE(r.[KOMISYON_TUTARI], 0) AS [KOMISYON_TUTARI],
                   CASE
                       WHEN r.[FIRMA_ID] IS NOT NULL THEN N'Firma'
                       WHEN COALESCE(r.[SATIS_TEMSILCISI_ID], 0) > 0 THEN N'Satış'
                       ELSE N'Bireysel'
                   END AS [KAYNAK]
            FROM [dbo].[REZERVASYONLAR] r
            LEFT JOIN [dbo].[OTELLER] o ON o.id = r.[OTEL_ID]
            LEFT JOIN [dbo].[KULLANICILAR] u ON u.id = r.[KULLANICI_ID]
            LEFT JOIN [dbo].[FIRMALAR] f ON f.id = r.[FIRMA_ID]
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

        if (!await TableExistsAsync(connection, "ODEME_ISLEMLERI", cancellationToken))
        {
            return model;
        }

        model.SummaryCards.AddRange(await LoadAdminPaymentSummaryAsync(connection, cancellationToken));
        model.StatusOptions.AddRange(await LoadDistinctAdminOptionAsync(connection, "ODEME_ISLEMLERI", "ODEME_DURUMU", cancellationToken));
        model.TypeOptions.AddRange(await LoadDistinctAdminOptionAsync(connection, "ODEME_ISLEMLERI", "ODEME_TURU", cancellationToken));

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
            FROM [dbo].[ODEME_ISLEMLERI] p
            LEFT JOIN [dbo].[REZERVASYONLAR] r ON r.id = p.[REZERVASYON_ID]
            LEFT JOIN [dbo].[OTELLER] o ON o.id = p.[OTEL_ID]
            LEFT JOIN [dbo].[KULLANICILAR] u ON u.id = p.[KULLANICI_ID]
            {whereSql};
            """, connection))
        {
            BindPaymentFilters(countCmd, model);
            model.Total = Convert.ToInt32(await countCmd.ExecuteScalarAsync(cancellationToken) ?? 0, CultureInfo.InvariantCulture);
        }

        await using var cmd = new SqlCommand($"""
            SELECT p.id,
                   COALESCE(p.[ISLEM_NO], N'-'),
                   COALESCE(r.[REZERVASYON_NO], N'-'),
                   COALESCE(o.[OTEL_ADI], N'-'),
                   COALESCE(u.[AD_SOYAD], N'-'),
                   COALESCE(p.[ODEME_TURU], N'-'),
                   COALESCE(p.[ODEME_YONTEMI], N'-'),
                   COALESCE(p.[ODEME_DURUMU], N'-'),
                   COALESCE(p.[TUTAR], 0),
                   COALESCE(p.[KOMISYON_TUTARI], 0),
                   COALESCE(p.[VERGI_TUTARI], 0),
                   COALESCE(p.[TOPLAM_TAHSILAT], 0),
                   COALESCE(p.[PARA_BIRIMI], N'TRY'),
                   COALESCE(p.[ODEME_SAGLAYICI], N'-'),
                   COALESCE(p.[RISK_PUANI], 0),
                   COALESCE(p.[MANUEL_ONAY_GEREKTIRIR], 0),
                   p.[ODEME_BASLANGIC_TARIHI],
                   p.[ODEME_TAMAMLANMA_TARIHI]
            FROM [dbo].[ODEME_ISLEMLERI] p
            LEFT JOIN [dbo].[REZERVASYONLAR] r ON r.id = p.[REZERVASYON_ID]
            LEFT JOIN [dbo].[OTELLER] o ON o.id = p.[OTEL_ID]
            LEFT JOIN [dbo].[KULLANICILAR] u ON u.id = p.[KULLANICI_ID]
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

        if (!await TableExistsAsync(connection, "FATURALAR", cancellationToken))
        {
            return model;
        }

        model.SummaryCards.AddRange(await LoadAdminInvoiceSummaryAsync(connection, cancellationToken));
        model.StatusOptions.AddRange(await LoadDistinctAdminOptionAsync(connection, "FATURALAR", "FATURA_DURUMU", cancellationToken));
        model.TypeOptions.AddRange(await LoadDistinctAdminOptionAsync(connection, "FATURALAR", "FATURA_TURU", cancellationToken));

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
            FROM [dbo].[FATURALAR] f
            LEFT JOIN [dbo].[OTELLER] o ON o.id = f.[OTEL_ID]
            LEFT JOIN [dbo].[REZERVASYONLAR] r ON r.id = f.[REZERVASYON_ID]
            {whereSql};
            """, connection))
        {
            BindInvoiceFilters(countCmd, model);
            model.Total = Convert.ToInt32(await countCmd.ExecuteScalarAsync(cancellationToken) ?? 0, CultureInfo.InvariantCulture);
        }

        await using var cmd = new SqlCommand($"""
            SELECT f.id,
                   COALESCE(f.[FATURA_NO], N'-'),
                   COALESCE(f.[FATURA_TURU], N'-'),
                   COALESCE(o.[OTEL_ADI], N'-'),
                   COALESCE(f.[FATURA_ALICI_UNVAN], u.[AD_SOYAD], firma.[FIRMA_ADI], N'-'),
                   COALESCE(f.[FATURA_ALICI_EPOSTA], u.[EPOSTA], N''),
                   COALESCE(f.[FATURA_DURUMU], N'Taslak'),
                   COALESCE(f.[E_FATURA_DURUMU], N'-'),
                   COALESCE(f.[ARA_TOPLAM], 0),
                   COALESCE(f.[KDV_TUTARI], 0),
                   COALESCE(f.[KONAKLAMA_VERGISI_TUTARI], 0),
                   COALESCE(f.[GENEL_TOPLAM], 0),
                   COALESCE(f.[PARA_BIRIMI], N'TRY'),
                   f.[FATURA_TARIHI],
                   f.[VADE_TARIHI],
                   f.[ODEME_TARIHI],
                   COALESCE(f.[FATURA_PDF_YOLU], N'')
            FROM [dbo].[FATURALAR] f
            LEFT JOIN [dbo].[OTELLER] o ON o.id = f.[OTEL_ID]
            LEFT JOIN [dbo].[REZERVASYONLAR] r ON r.id = f.[REZERVASYON_ID]
            LEFT JOIN [dbo].[KULLANICILAR] u ON u.id = f.[KULLANICI_ID]
            LEFT JOIN [dbo].[FIRMALAR] firma ON firma.id = f.[FIRMA_ID]
            {whereSql}
            ORDER BY COALESCE(f.[FATURA_TARIHI], CAST(f.[OLUSTURULMA_TARIHI] AS date)) DESC, f.id DESC
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
            ("Toplam İşlem", "SELECT COUNT(*) FROM [dbo].[ODEME_ISLEMLERI]", "Tüm ödeme hareketleri", "info", "fa-credit-card"),
            ("Başarılı", "SELECT COUNT(*) FROM [dbo].[ODEME_ISLEMLERI] WHERE [ODEME_DURUMU] = N'Başarılı'", "Tamamlanan tahsilatlar", "success", "fa-circle-check"),
            ("Riskli", "SELECT COUNT(*) FROM [dbo].[ODEME_ISLEMLERI] WHERE COALESCE([RISK_PUANI],0) >= 70 OR COALESCE([MANUEL_ONAY_GEREKTIRIR],0)=1", "Risk/manuel onay bekleyenler", "warning", "fa-shield-halved"),
            ("İade Edilen", "SELECT COALESCE(SUM(COALESCE([IADE_EDILEN_TUTAR],0)),0) FROM [dbo].[ODEME_ISLEMLERI]", "Toplam iade tutarı", "danger", "fa-rotate-left")
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
            ("Toplam Fatura", "SELECT COUNT(*) FROM [dbo].[FATURALAR]", "Sistemdeki tüm fatura kayıtları", "info", "fa-file-invoice"),
            ("Kesildi", "SELECT COUNT(*) FROM [dbo].[FATURALAR] WHERE [FATURA_DURUMU] = N'Kesildi'", "Aktif kesilmiş faturalar", "success", "fa-file-circle-check"),
            ("Bekleyen", "SELECT COUNT(*) FROM [dbo].[FATURALAR] WHERE COALESCE([FATURA_DURUMU],N'Taslak') IN (N'Taslak',N'Beklemede')", "Hazırlık/onay bekleyenler", "warning", "fa-file-pen"),
            ("Ciro", "SELECT COALESCE(SUM(COALESCE([GENEL_TOPLAM],0)),0) FROM [dbo].[FATURALAR] WHERE COALESCE([FATURA_DURUMU],N'') <> N'İptal Edildi'", "İptal dışı toplam", "success", "fa-chart-line")
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
                    DATEFROMPARTS(YEAR(r.[GIRIS_TARIHI]), MONTH(r.[GIRIS_TARIHI]), 1) AS report_month,
                    o.id AS hotel_id,
                    COALESCE(o.[OTEL_ADI], N'-') AS hotel_name,
                    CONCAT(COALESCE(o.[ILCE], N'-'), N', ', COALESCE(o.[SEHIR], N'-')) AS city_label,
                    COUNT(*) AS reservation_count,
                    SUM(CASE WHEN COALESCE(r.[DURUM], N'') IN (N'Tamamlandı', N'Giriş Yaptı', N'Onaylandı') THEN 1 ELSE 0 END) AS completed_count,
                    SUM(CASE WHEN COALESCE(r.[DURUM], N'') LIKE N'%İptal%' OR COALESCE(r.[DURUM], N'') LIKE N'%Iptal%' THEN 1 ELSE 0 END) AS cancelled_count,
                    COALESCE(SUM(CASE WHEN COALESCE(r.[DURUM], N'') NOT LIKE N'%İptal%' AND COALESCE(r.[DURUM], N'') NOT LIKE N'%Iptal%' THEN COALESCE(r.[TOPLAM_TUTAR],0) ELSE 0 END),0) AS gross_revenue,
                    COALESCE(SUM(CASE WHEN COALESCE(r.[DURUM], N'') NOT LIKE N'%İptal%' AND COALESCE(r.[DURUM], N'') NOT LIKE N'%Iptal%' THEN COALESCE(r.[KOMISYON_TUTARI],0) ELSE 0 END),0) AS gross_commission,
                    COALESCE(SUM(CASE WHEN COALESCE(r.[DURUM], N'') NOT LIKE N'%İptal%' AND COALESCE(r.[DURUM], N'') NOT LIKE N'%Iptal%' THEN COALESCE(r.[PLATFORM_NET_KOMISYON_TUTARI],0) ELSE 0 END),0) AS net_commission,
                    COALESCE(SUM(COALESCE(r.[KONAKLAMA_VERGISI_TUTARI],0)),0) AS accommodation_tax,
                    COALESCE(SUM(COALESCE(r.[KDV_TUTARI],0)),0) AS kdv_amount
                FROM [dbo].[REZERVASYONLAR] r
                INNER JOIN [dbo].[OTELLER] o ON o.id = r.[OTEL_ID]
                WHERE r.[GIRIS_TARIHI] >= @fromDate
                  AND r.[GIRIS_TARIHI] <= @toDate
                  AND (@hotelId IS NULL OR o.id = @hotelId)
                GROUP BY DATEFROMPARTS(YEAR(r.[GIRIS_TARIHI]), MONTH(r.[GIRIS_TARIHI]), 1), o.id, o.[OTEL_ADI], o.[ILCE], o.[SEHIR]
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

    public async Task<AdminRevenueCommandCenterPageViewModel> GetRevenueCommandCenterAsync(string fullName, string email, string userRole, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var model = new AdminRevenueCommandCenterPageViewModel
        {
            Shell = await GetShellAsync(connection, "Gelir Komuta Merkezi", "Son 30 gün platform GMV, komisyon, rezervasyon trendi ve otel liderleri.", fullName, email, userRole, cancellationToken)
        };

        const string revenue30Sql = @"
            SELECT
                COALESCE(SUM(COALESCE(r.[TOPLAM_TUTAR], 0)), 0) AS gmv_30d,
                COALESCE(SUM(COALESCE(r.[KOMISYON_TUTARI], 0)), 0) AS commission_30d,
                COUNT(*) AS reservation_count_30d
            FROM [dbo].[REZERVASYONLAR] r
            WHERE COALESCE(r.[DURUM], N'') <> N'İptal Edildi'
              AND r.[OLUSTURULMA_TARIHI] >= DATEADD(day, -30, CAST(GETDATE() AS date));";

        const string cancel30Sql = @"
            SELECT COUNT(*)
            FROM [dbo].[REZERVASYONLAR] r
            WHERE COALESCE(r.[DURUM], N'') = N'İptal Edildi'
              AND r.[OLUSTURULMA_TARIHI] >= DATEADD(day, -30, CAST(GETDATE() AS date));";

        decimal gmv30 = 0m;
        decimal commission30 = 0m;
        var reservationCount30 = 0;
        var cancelledCount30 = 0;

        await using (var revenue30Command = new SqlCommand(revenue30Sql, connection))
        await using (var revenue30Reader = await revenue30Command.ExecuteReaderAsync(cancellationToken))
        {
            if (await revenue30Reader.ReadAsync(cancellationToken))
            {
                gmv30 = SafeDecimal(revenue30Reader, 0);
                commission30 = SafeDecimal(revenue30Reader, 1);
                reservationCount30 = SafeInt(revenue30Reader, 2);
            }
        }

        await using (var cancel30Command = new SqlCommand(cancel30Sql, connection))
        {
            cancelledCount30 = Convert.ToInt32(await cancel30Command.ExecuteScalarAsync(cancellationToken) ?? 0, CultureInfo.InvariantCulture);
        }

        var tr = CultureInfo.GetCultureInfo("tr-TR");
        var createdTotal30 = reservationCount30 + cancelledCount30;
        var cancelRate = createdTotal30 <= 0 ? 0m : Math.Round(cancelledCount30 * 100m / createdTotal30, 1, MidpointRounding.AwayFromZero);

        model.SummaryCards.Add(new AdminSummaryCardViewModel
        {
            Label = "Platform GMV (30 gün)",
            Value = $"{gmv30:N0} TL",
            Description = "İptal hariç rezervasyon cirosu",
            ToneClass = "success",
            IconClass = "fa-chart-line"
        });
        model.SummaryCards.Add(new AdminSummaryCardViewModel
        {
            Label = "Platform komisyon (30 gün)",
            Value = $"{commission30:N0} TL",
            Description = "Rezervasyon snapshot komisyonu",
            ToneClass = "warning",
            IconClass = "fa-percent"
        });
        model.SummaryCards.Add(new AdminSummaryCardViewModel
        {
            Label = "Rezervasyon (30 gün)",
            Value = reservationCount30.ToString("N0", tr),
            Description = "Son 30 günde oluşturulan kayıtlar",
            ToneClass = "info",
            IconClass = "fa-calendar-check"
        });
        model.SummaryCards.Add(new AdminSummaryCardViewModel
        {
            Label = "İptal (30 gün)",
            Value = $"{cancelledCount30:N0} · %{cancelRate:0.#}",
            Description = "İptal adedi ve oran",
            ToneClass = "danger",
            IconClass = "fa-ban"
        });

        const string dailyTrendSql = @"
            SELECT
                CAST(r.[OLUSTURULMA_TARIHI] AS date) AS gun,
                COALESCE(SUM(CASE WHEN COALESCE(r.[DURUM], N'') <> N'İptal Edildi' THEN COALESCE(r.[TOPLAM_TUTAR], 0) ELSE 0 END), 0) AS gmv,
                COUNT(*) AS adet
            FROM [dbo].[REZERVASYONLAR] r
            WHERE r.[OLUSTURULMA_TARIHI] >= DATEADD(day, -30, CAST(GETDATE() AS date))
            GROUP BY CAST(r.[OLUSTURULMA_TARIHI] AS date)
            ORDER BY gun;";

        var dailyRows = new Dictionary<DateTime, (decimal Gmv, int Count)>();
        await using (var dailyCommand = new SqlCommand(dailyTrendSql, connection))
        await using (var dailyReader = await dailyCommand.ExecuteReaderAsync(cancellationToken))
        {
            while (await dailyReader.ReadAsync(cancellationToken))
            {
                var day = dailyReader.GetDateTime(0).Date;
                dailyRows[day] = (SafeDecimal(dailyReader, 1), SafeInt(dailyReader, 2));
            }
        }

        var trendStart = DateTime.Today.AddDays(-29);
        var gmvSeries = new List<(string Label, decimal Value)>();
        var countSeries = new List<(string Label, int Value)>();
        for (var day = trendStart; day <= DateTime.Today; day = day.AddDays(1))
        {
            dailyRows.TryGetValue(day, out var row);
            gmvSeries.Add((day.ToString("dd MMM", tr), row.Gmv));
            countSeries.Add((day.ToString("dd MMM", tr), row.Count));
        }

        var maxGmv = Math.Max(gmvSeries.Count == 0 ? 0m : gmvSeries.Max(static item => item.Value), 1m);
        foreach (var row in gmvSeries)
        {
            model.GmvDailyTrend.Add(new AdminChartBarViewModel
            {
                Label = row.Label,
                Value = (int)Math.Round(row.Value, MidpointRounding.AwayFromZero),
                HeightPercent = Math.Max(8, (int)Math.Round(row.Value * 100m / maxGmv, MidpointRounding.AwayFromZero))
            });
        }

        var maxCount = Math.Max(countSeries.Count == 0 ? 0 : countSeries.Max(static item => item.Value), 1);
        foreach (var row in countSeries)
        {
            model.ReservationDailyTrend.Add(new AdminChartBarViewModel
            {
                Label = row.Label,
                Value = row.Value,
                HeightPercent = Math.Max(8, (int)Math.Round(row.Value * 100m / maxCount, MidpointRounding.AwayFromZero))
            });
        }

        const string topHotelsSql = @"
            SELECT TOP (12)
                o.[ID],
                COALESCE(o.[OTEL_ADI], N'-') AS hotel_name,
                CONCAT(COALESCE(o.[ILCE], N'-'), N', ', COALESCE(o.[SEHIR], N'-')) AS city_label,
                COUNT(*) AS reservation_count,
                SUM(CASE WHEN COALESCE(r.[DURUM], N'') = N'İptal Edildi' THEN 1 ELSE 0 END) AS cancelled_count,
                COALESCE(SUM(CASE WHEN COALESCE(r.[DURUM], N'') <> N'İptal Edildi' THEN COALESCE(r.[TOPLAM_TUTAR], 0) ELSE 0 END), 0) AS gmv,
                COALESCE(SUM(CASE WHEN COALESCE(r.[DURUM], N'') <> N'İptal Edildi' THEN COALESCE(r.[KOMISYON_TUTARI], 0) ELSE 0 END), 0) AS commission
            FROM [dbo].[REZERVASYONLAR] r
            INNER JOIN [dbo].[OTELLER] o ON o.[ID] = r.[OTEL_ID]
            WHERE r.[OLUSTURULMA_TARIHI] >= DATEADD(day, -30, CAST(GETDATE() AS date))
            GROUP BY o.[ID], o.[OTEL_ADI], o.[ILCE], o.[SEHIR]
            ORDER BY gmv DESC, reservation_count DESC;";

        await using (var topHotelsCommand = new SqlCommand(topHotelsSql, connection))
        await using (var topHotelsReader = await topHotelsCommand.ExecuteReaderAsync(cancellationToken))
        {
            while (await topHotelsReader.ReadAsync(cancellationToken))
            {
                model.TopHotels.Add(new AdminRevenueHotelLeaderRowViewModel
                {
                    HotelId = topHotelsReader.GetInt64(0),
                    HotelName = topHotelsReader.GetString(1),
                    CityLabel = topHotelsReader.GetString(2),
                    ReservationCount = SafeInt(topHotelsReader, 3),
                    CancelledCount = SafeInt(topHotelsReader, 4),
                    Gmv = SafeDecimal(topHotelsReader, 5),
                    Commission = SafeDecimal(topHotelsReader, 6)
                });
            }
        }

        model.PackageTablesReady = await TableExistsAsync(connection, "PARTNER_PAKET_BASVURULARI", cancellationToken);
        if (model.PackageTablesReady)
        {
            const string packageSql = @"
                SELECT
                    COUNT(*) AS total_30d,
                    SUM(CASE WHEN k.[KOD] = N'5651' THEN 1 ELSE 0 END) AS kod_5651,
                    SUM(CASE WHEN k.[KOD] = N'5661' THEN 1 ELSE 0 END) AS kod_5661
                FROM [dbo].[PARTNER_PAKET_BASVURULARI] b
                INNER JOIN [dbo].[PLATFORM_PAKETLER] p ON p.[ID] = b.[PAKET_ID]
                INNER JOIN [dbo].[PLATFORM_PAKET_KATEGORILERI] k ON k.[ID] = p.[KATEGORI_ID]
                WHERE b.[OLUSTURULMA_UTC] >= DATEADD(day, -30, SYSUTCDATETIME());";

            await using var packageCommand = new SqlCommand(packageSql, connection);
            await using var packageReader = await packageCommand.ExecuteReaderAsync(cancellationToken);
            if (await packageReader.ReadAsync(cancellationToken))
            {
                model.PackageApplications30d = SafeInt(packageReader, 0);
                model.Package5651Applications30d = SafeInt(packageReader, 1);
                model.Package5661Applications30d = SafeInt(packageReader, 2);
            }

            model.SummaryCards.Add(new AdminSummaryCardViewModel
            {
                Label = "5651/5661 başvuru (30 gün)",
                Value = model.PackageApplications30d.ToString("N0", tr),
                Description = $"5651: {model.Package5651Applications30d:N0} · 5661: {model.Package5661Applications30d:N0}",
                ToneClass = "primary",
                IconClass = "fa-shield-halved"
            });
        }

        return model;
    }

    private static async Task<bool> TableExistsAsync(SqlConnection connection, string tableName, CancellationToken cancellationToken)
    {
        await using var cmd = new SqlCommand("SELECT CASE WHEN OBJECT_ID(@fullName, N'U') IS NULL THEN 0 ELSE 1 END;", connection);
        cmd.Parameters.AddWithValue("@fullName", $"[dbo].[{tableName}]");
        var exists = Convert.ToInt32(await cmd.ExecuteScalarAsync(cancellationToken) ?? 0, CultureInfo.InvariantCulture);
        return exists == 1;
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
        const string sql = "SELECT TOP (500) id, [OTEL_ADI] FROM [dbo].[OTELLER] ORDER BY [OTEL_ADI];";
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
                COALESCE(SUM(CASE WHEN COALESCE([DURUM],N'') NOT LIKE N'%İptal%' AND COALESCE([DURUM],N'') NOT LIKE N'%Iptal%' THEN COALESCE([TOPLAM_TUTAR],0) ELSE 0 END),0) AS gross_revenue,
                COALESCE(SUM(CASE WHEN COALESCE([DURUM],N'') NOT LIKE N'%İptal%' AND COALESCE([DURUM],N'') NOT LIKE N'%Iptal%' THEN COALESCE([KOMISYON_TUTARI],0) ELSE 0 END),0) AS gross_commission,
                COALESCE(SUM(CASE WHEN COALESCE([DURUM],N'') NOT LIKE N'%İptal%' AND COALESCE([DURUM],N'') NOT LIKE N'%Iptal%' THEN COALESCE([PLATFORM_NET_KOMISYON_TUTARI],0) ELSE 0 END),0) AS net_commission
            FROM [dbo].[REZERVASYONLAR]
            WHERE [GIRIS_TARIHI] >= @fromDate
              AND [GIRIS_TARIHI] <= @toDate
              AND (@hotelId IS NULL OR [OTEL_ID] = @hotelId);
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

        if (!await TableExistsAsync(connection, "API_LOGLARI", cancellationToken))
        {
            return model;
        }

        var since = DateTime.UtcNow.AddHours(-model.WindowHours);
        const string sql = """
            SELECT TOP (200)
                COALESCE([ENDPOINT],'') AS [ENDPOINT],
                COALESCE([HTTP_METHOD],'') AS method,
                SUM(CASE WHEN response_status = 429 THEN 1 ELSE 0 END) AS count_429,
                COUNT(*) AS count_total
            FROM [dbo].[API_LOGLARI]
            WHERE [BASLANGIC_TARIHI] >= @since
            GROUP BY [ENDPOINT], [HTTP_METHOD]
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
        if (await TableExistsAsync(connection, "EPOSTA_SERVISLERI", cancellationToken))
        {
            await using var cmd = new SqlCommand("""
                SELECT TOP (1) COALESCE([SAGLAYICI],''), COALESCE([AKTIF_MI], 0)
                FROM [dbo].[EPOSTA_SERVISLERI]
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
            Value = (await CountIfTableExistsAsync(connection, "BILDIRIM_LOGLARI", "COALESCE([DURUM],'') IN (N'Beklemede',N'Kuyrukta',N'Basarisiz',N'Başarısız')", cancellationToken)).ToString(CultureInfo.InvariantCulture),
            Description = "Bekleyen veya hatalı bildirim işleri",
            ToneClass = "info",
            IconClass = "fas fa-envelope-open-text"
        });
        model.SummaryCards.Add(new AdminSummaryCardViewModel
        {
            Label = "Aktif E-posta Servisi",
            Value = (await CountIfTableExistsAsync(connection, "EPOSTA_SERVISLERI", "COALESCE([AKTIF_MI],0)=1", cancellationToken)).ToString(CultureInfo.InvariantCulture),
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
            ("E-posta servisleri", "EPOSTA_SERVISLERI", "/admin/mail-merkezi"),
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
            SELECT COUNT(*), COALESCE(SUM(COALESCE([TOPLAM_TUTAR],0)),0)
            FROM [dbo].[REZERVASYONLAR]
            WHERE COALESCE([OLUSTURULMA_TARIHI], SYSUTCDATETIME()) >= @since
              AND COALESCE([DURUM],'') <> N'İptal Edildi';
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

        if (await TableExistsAsync(connection, "ODA_FIYAT_MUSAITLIK", cancellationToken))
        {
            const string invSql = """
                SELECT TOP (40)
                    COALESCE(o.[OTEL_ADI],'') AS [OTEL_ADI],
                    COALESCE(ot.[ODA_ADI],'') AS [ODA_ADI],
                    ofm.[TARIH],
                    (COALESCE(ofm.[TOPLAM_ODA_SAYISI],0) - COALESCE(ofm.[SATILAN_ODA_SAYISI],0) - COALESCE(ofm.[BLOKE_ODA_SAYISI],0)) AS kalan
                FROM [dbo].[ODA_FIYAT_MUSAITLIK] ofm
                JOIN [dbo].[OTELLER] o ON o.id = ofm.[OTEL_ID]
                JOIN [dbo].[ODA_TIPLERI] ot ON ot.id = ofm.[ODA_TIP_ID]
                WHERE ofm.[TARIH] >= CAST(SYSUTCDATETIME() AS date)
                  AND (COALESCE(ofm.[TOPLAM_ODA_SAYISI],0) - COALESCE(ofm.[SATILAN_ODA_SAYISI],0) - COALESCE(ofm.[BLOKE_ODA_SAYISI],0)) BETWEEN 1 AND 3
                ORDER BY kalan ASC, ofm.[TARIH] ASC;
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
            const string firstHotelSql = "SELECT TOP (1) id FROM [dbo].[OTELLER] ORDER BY id ASC;";
            await using var fhCmd = new SqlCommand(firstHotelSql, connection);
            var scalar = await fhCmd.ExecuteScalarAsync(cancellationToken);
            if (scalar is not null && scalar != DBNull.Value)
            {
                hid = Convert.ToInt64(scalar, CultureInfo.InvariantCulture);
                model.PriceHistoryHotelId = hid;
            }
        }

        if (hid > 0 && await TableExistsAsync(connection, "ODA_FIYAT_MUSAITLIK", cancellationToken))
        {
            const string priceSql = """
                SELECT TOP (60)
                    COALESCE(ot.[ODA_ADI],'') AS [ODA_ADI],
                    ofm.[TARIH],
                    COALESCE(ofm.[GECELIK_FIYAT],0) AS gecelik,
                    ofm.[INDIRIMLI_FIYAT]
                FROM [dbo].[ODA_FIYAT_MUSAITLIK] ofm
                JOIN [dbo].[ODA_TIPLERI] ot ON ot.id = ofm.[ODA_TIP_ID]
                WHERE ofm.[OTEL_ID] = @hotelId
                ORDER BY ofm.[TARIH] DESC, ofm.id DESC;
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

        if (await TableExistsAsync(connection, "REZERVASYONLAR_ARSIV", cancellationToken))
        {
            await using var aCmd = new SqlCommand("SELECT COUNT(*) FROM [dbo].[REZERVASYONLAR_ARSIV];", connection);
            var ac = await aCmd.ExecuteScalarAsync(cancellationToken);
            model.ArchivedReservationSampleCount = ac is null || ac == DBNull.Value
                ? 0
                : Convert.ToInt32(ac, CultureInfo.InvariantCulture);
            model.ArchiveHint = "Arşiv tablosu mevcut.";
        }
        else
        {
            model.ArchiveHint = "REZERVASYONLAR_ARSIV tablosu henüz oluşturulmadı (105_REZERVASYONLAR_ARSIV.sql migration ile).";
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
                FORMAT(DATEFROMPARTS(YEAR(r.[GIRIS_TARIHI]), MONTH(r.[GIRIS_TARIHI]), 1), 'yyyy-MM', 'en-US') AS ay,
                o.id,
                o.[OTEL_ADI],
                COALESCE(o.[SEHIR], '') AS [SEHIR],
                COALESCE(o.[ILCE], '') AS ilce,
                COUNT(*) AS rezervasyon,
                COALESCE(SUM(COALESCE(r.[TOPLAM_TUTAR],0)),0) AS ciro,
                COALESCE(SUM(COALESCE(r.[KOMISYON_TUTARI],0)),0) AS brut_komisyon,
                COALESCE(SUM(COALESCE(r.[PLATFORM_NET_KOMISYON_TUTARI],0)),0) AS net_komisyon
            FROM [dbo].[REZERVASYONLAR] r
            INNER JOIN [dbo].[OTELLER] o ON o.id = r.[OTEL_ID]
            WHERE COALESCE(r.[DURUM],'') <> N'İptal Edildi'
              AND r.[GIRIS_TARIHI] >= DATEADD(month, -6, DATEFROMPARTS(YEAR(GETDATE()), MONTH(GETDATE()), 1))
            GROUP BY DATEFROMPARTS(YEAR(r.[GIRIS_TARIHI]), MONTH(r.[GIRIS_TARIHI]), 1), o.id, o.[OTEL_ADI], o.[SEHIR], o.[ILCE]
            ORDER BY DATEFROMPARTS(YEAR(r.[GIRIS_TARIHI]), MONTH(r.[GIRIS_TARIHI]), 1) DESC, ciro DESC;
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
            SELECT TOP (1) [SAGLAYICI], [GONDEREN_EPOSTA], [TEST_MODU], [AKTIF_MI]
            FROM [dbo].[EPOSTA_SERVISLERI]
            WHERE [AKTIF_MI] = 1
            ORDER BY [VARSAYILAN_MI] DESC, id ASC;";
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
                model.Checks.Add(new AdminSystemHealthCheckItemViewModel { Title = "E-posta Servisi", StatusText = "YOK", ToneClass = "danger", Detail = "Aktif EPOSTA_SERVISLERI kaydı bulunamadı." });
            }
        }

        // Queue stats: bildirim_loglari (email)
        const string queueSql = @"
            SELECT
                SUM(CASE WHEN tur='E-posta' AND [DURUM]='Beklemede' THEN 1 ELSE 0 END) AS email_pending,
                SUM(CASE WHEN tur='E-posta' AND [DURUM]='Başarısız' THEN 1 ELSE 0 END) AS email_failed,
                MIN(CASE WHEN tur='E-posta' AND [DURUM]='Beklemede' THEN [OLUSTURULMA_TARIHI] ELSE NULL END) AS email_oldest_pending,
                SUM(CASE WHEN tur='Sistem İçi' AND [DURUM]='Beklemede' THEN 1 ELSE 0 END) AS system_pending
            FROM [dbo].[BILDIRIM_LOGLARI];";
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
                (SELECT COUNT(*) FROM [dbo].[FIRMALAR] WHERE COALESCE([ONAY_DURUMU],'Beklemede')='Beklemede') AS pending_company,
                (SELECT COUNT(*) FROM [dbo].[PARTNER_DETAYLARI] WHERE [ONAY_DURUMU]='Beklemede') AS pending_partner,
                (SELECT COUNT(*) FROM [dbo].[YORUMLAR] WHERE [ONAY_DURUMU]='Beklemede') AS pending_reviews,
                (SELECT COUNT(*) FROM [dbo].[REZERVASYONLAR] WHERE [DURUM]='Onay Bekliyor') AS pending_reservations,
                (SELECT COUNT(*) FROM [dbo].[SISTEM_HATA_LOGLARI] WHERE [HATA_SEVIYESI] IN ('CRITICAL','ALERT','EMERGENCY') AND [COZULDU_MU]=0) AS critical_errors;";
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
            IF OBJECT_ID(N'[dbo].[FIRMA_ODA_FIYAT_MUSAITLIK]', N'U') IS NULL
            BEGIN
                SELECT 0 AS exists_flag, 0 AS row_count;
            END
            ELSE
            BEGIN
                SELECT 1 AS exists_flag, (SELECT COUNT(*) FROM [dbo].[FIRMA_ODA_FIYAT_MUSAITLIK]) AS row_count;
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
            IF OBJECT_ID(N'[dbo].[OTEL_LISTE_ABONELIKLERI]', N'U') IS NULL
            BEGIN
                SELECT 0 AS exists_flag, 0 AS row_count;
            END
            ELSE
            BEGIN
                SELECT 1 AS exists_flag, (SELECT COUNT(*) FROM [dbo].[OTEL_LISTE_ABONELIKLERI]) AS row_count;
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
            SELECT p.id, p.[KULLANICI_ID], o.id AS hotel_id, p.[FIRMA_UNVANI], COALESCE(o.[OTEL_ADI], p.[FIRMA_UNVANI]),
                   p.[YETKILI_AD_SOYAD], p.[YETKILI_EPOSTA], p.[VERGI_NUMARASI], p.[ONAY_DURUMU], p.[OLUSTURULMA_TARIHI],
                   p.[ONAY_TARIHI], u.[EPOSTA_DOGRULAMA_TARIHI],
                   CASE
                        WHEN COL_LENGTH('partner_detaylari', 'eposta_giris_onayi_verildi_mi') IS NULL THEN 0
                        ELSE COALESCE(p.[EPOSTA_GIRIS_ONAYI_VERILDI_MI], 0)
                   END AS email_login_approved,
                   (SELECT COUNT(*) FROM [dbo].[PARTNER_BASVURU_EVRAKLARI] ped WHERE ped.[PARTNER_ID] = p.id) AS document_count,
                   COALESCE(p.[RED_NEDENI], '')
            FROM [dbo].[PARTNER_DETAYLARI] p
            INNER JOIN [dbo].[KULLANICILAR] u ON u.id = p.[KULLANICI_ID]
            LEFT JOIN [dbo].[OTELLER] o ON o.[PARTNER_ID] = p.id
            ORDER BY
                CASE p.[ONAY_DURUMU]
                    WHEN 'Beklemede' THEN 0
                    WHEN 'Reddedildi' THEN 1
                    WHEN 'Askida' THEN 2
                    ELSE 3
                END,
                p.[OLUSTURULMA_TARIHI] DESC;";

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

    public async Task<AdminPartnerApplicationDetailPageViewModel?> GetPartnerApplicationDetailAsync(long partnerId, string fullName, string email, string userRole, long adminUserId, CancellationToken cancellationToken = default)
    {
        if (partnerId <= 0)
        {
            return null;
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        const string sql = @"
            SELECT TOP (1)
                p.id, p.[KULLANICI_ID], o.id,
                p.[FIRMA_UNVANI], p.[FIRMA_TURU], COALESCE(o.[OTEL_ADI], p.[FIRMA_UNVANI]),
                p.[YETKILI_AD_SOYAD], COALESCE(p.[YETKILI_GOREV], N''),
                p.[YETKILI_EPOSTA], p.[YETKILI_TELEFON],
                p.[VERGI_DAIRESI], p.[VERGI_NUMARASI], p.[YETKILI_TC_NO],
                p.[FATURA_ADRESI], p.[FATURA_IL], p.[FATURA_ILCE],
                p.[BANKA_ADI], COALESCE(p.[BANKA_SUBESI], N''), p.[IBAN],
                COALESCE(p.[WEB_SITESI], N''), COALESCE(p.[ACIKLAMA], N''),
                p.[ONAY_DURUMU], p.[OLUSTURULMA_TARIHI], p.[ONAY_TARIHI],
                u.[EPOSTA_DOGRULAMA_TARIHI],
                CASE WHEN COL_LENGTH('partner_detaylari', 'eposta_giris_onayi_verildi_mi') IS NULL THEN 0 ELSE COALESCE(p.[EPOSTA_GIRIS_ONAYI_VERILDI_MI], 0) END,
                COALESCE(p.[RED_NEDENI], N''),
                COALESCE(o.[VARSAYILAN_KOMISYON_ORANI], 15),
                COALESCE(o.[ONAY_DURUMU], N'Beklemede'),
                COALESCE(o.[YAYIN_DURUMU], N'Taslak')
            FROM [dbo].[PARTNER_DETAYLARI] p
            INNER JOIN [dbo].[KULLANICILAR] u ON u.id = p.[KULLANICI_ID]
            LEFT JOIN [dbo].[OTELLER] o ON o.[PARTNER_ID] = p.id
            WHERE p.id = @partnerId
            ORDER BY o.id DESC;";

        AdminPartnerApplicationDetailViewModel? detail = null;
        await using (var command = new SqlCommand(sql, connection))
        {
            command.Parameters.AddWithValue("@partnerId", partnerId);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                return null;
            }

            var status = reader.GetString(22);
            detail = new AdminPartnerApplicationDetailViewModel
            {
                PartnerId = reader.GetInt64(0),
                UserId = reader.GetInt64(1),
                HotelId = reader.IsDBNull(2) ? null : reader.GetInt64(2),
                CompanyName = reader.GetString(3),
                CompanyType = reader.GetString(4),
                HotelName = reader.GetString(5),
                ContactName = reader.GetString(6),
                ContactTitle = reader.GetString(7),
                Email = reader.GetString(8),
                Phone = reader.GetString(9),
                TaxOffice = reader.GetString(10),
                TaxNumber = reader.GetString(11),
                ContactTcNo = reader.GetString(12),
                Address = reader.GetString(13),
                City = reader.GetString(14),
                District = reader.GetString(15),
                BankName = reader.GetString(16),
                BankBranch = EmptyToNull(reader.GetString(17)),
                Iban = reader.GetString(18),
                Website = EmptyToNull(reader.GetString(19)),
                Description = EmptyToNull(reader.GetString(20)),
                StatusText = status,
                StatusToneClass = status switch
                {
                    "Onaylandi" => "success",
                    "Reddedildi" => "danger",
                    "Askida" => "warning",
                    _ => "info"
                },
                RegistrationDateText = reader.GetDateTime(23).ToString("dd.MM.yyyy HH:mm", CultureInfo.GetCultureInfo("tr-TR")),
                ApprovalDateText = reader.IsDBNull(24) ? null : reader.GetDateTime(24).ToString("dd.MM.yyyy HH:mm", CultureInfo.GetCultureInfo("tr-TR")),
                EmailVerified = !reader.IsDBNull(25),
                EmailLoginApproved = SafeInt(reader, 26) == 1,
                ReviewNote = EmptyToNull(reader.GetString(27)),
                CurrentCommissionRate = reader.GetDecimal(28),
                HotelApprovalStatus = reader.GetString(29),
                HotelPublishStatus = reader.GetString(30)
            };
        }

        detail.AvailableDocumentTypes = PartnerRequiredDocumentTypes.ToList();
        detail.ActiveMissingDocumentTypes = await LoadActiveMissingDocumentTypesAsync(connection, partnerId, cancellationToken);

        if (await TableExistsAsync(connection, "partner_basvuru_evraklari", cancellationToken))
        {
            const string docsSql = @"
                SELECT ped.id, ped.[GUVENLI_DOSYA_ID], ped.[EVRAK_TIPI], COALESCE(ped.[BELGE_BASLIGI], ped.[EVRAK_TIPI]),
                       COALESCE(gfv.[ORIJINAL_DOSYA_ADI], N'Belge'), ped.[DURUM], ped.[OLUSTURULMA_TARIHI], COALESCE(ped.[RED_NEDENI], N'')
                FROM [dbo].[PARTNER_BASVURU_EVRAKLARI] ped
                INNER JOIN [dbo].[GUVENLI_DOSYA_VARLIKLARI] gfv ON gfv.id = ped.[GUVENLI_DOSYA_ID]
                WHERE ped.[PARTNER_ID] = @partnerId
                ORDER BY ped.[OLUSTURULMA_TARIHI] DESC;";

            await using var docsCommand = new SqlCommand(docsSql, connection);
            docsCommand.Parameters.AddWithValue("@partnerId", partnerId);
            await using var docsReader = await docsCommand.ExecuteReaderAsync(cancellationToken);
            while (await docsReader.ReadAsync(cancellationToken))
            {
                var fileId = docsReader.GetInt64(1);
                var docStatus = docsReader.GetString(5);
                detail.Documents.Add(new AdminPartnerDocumentItemViewModel
                {
                    DocumentId = docsReader.GetInt64(0),
                    DocumentType = docsReader.GetString(2),
                    Title = docsReader.GetString(3),
                    FileName = docsReader.GetString(4),
                    StatusText = docStatus,
                    StatusToneClass = MapPartnerDocumentTone(docStatus),
                    UploadedAtText = docsReader.GetDateTime(6).ToString("dd.MM.yyyy HH:mm", CultureInfo.GetCultureInfo("tr-TR")),
                    ReviewNote = EmptyToNull(docsReader.GetString(7)),
                    AccessUrl = await _secureFileService.CreateAccessUrlAsync(fileId, adminUserId, "admin", cancellationToken)
                });
            }
        }

        detail.Checklist = BuildPartnerDocumentChecklist(detail.Documents);

        return new AdminPartnerApplicationDetailPageViewModel
        {
            Shell = await GetShellAsync(connection, "Partner Basvuru Detayi", detail.CompanyName, fullName, email, userRole, cancellationToken),
            Application = detail
        };
    }

    private static async Task<List<string>> LoadActiveMissingDocumentTypesAsync(SqlConnection connection, long partnerId, CancellationToken cancellationToken)
    {
        var items = new List<string>();
        if (!await TableExistsAsync(connection, "partner_eksik_evrak_talepleri", cancellationToken))
        {
            return items;
        }

        const string sql = @"
            SELECT [EVRAK_TIPI]
            FROM [dbo].[PARTNER_EKSIK_EVRAK_TALEPLERI]
            WHERE [PARTNER_ID] = @partnerId AND [AKTIF_MI] = 1
            ORDER BY [EVRAK_TIPI];";

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@partnerId", partnerId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(reader.GetString(0));
        }

        return items;
    }

    private static async Task SaveMissingDocumentRequestsAsync(SqlConnection connection, SqlTransaction transaction, long partnerId, long adminUserId, IEnumerable<string> documentTypes, string? note, CancellationToken cancellationToken)
    {
        if (!await TableExistsAsync(connection, "partner_eksik_evrak_talepleri", cancellationToken, transaction))
        {
            return;
        }

        const string deactivateSql = @"
            UPDATE [dbo].[PARTNER_EKSIK_EVRAK_TALEPLERI]
            SET [AKTIF_MI] = 0
            WHERE [PARTNER_ID] = @partnerId AND [AKTIF_MI] = 1;";

        await using (var deactivate = new SqlCommand(deactivateSql, connection, transaction))
        {
            deactivate.Parameters.AddWithValue("@partnerId", partnerId);
            await deactivate.ExecuteNonQueryAsync(cancellationToken);
        }

        const string insertSql = @"
            INSERT INTO [dbo].[PARTNER_EKSIK_EVRAK_TALEPLERI]
            ([PARTNER_ID], [EVRAK_TIPI], [AKTIF_MI], [ADMIN_NOTU], [OLUSTURAN_ADMIN_ID], [OLUSTURULMA_TARIHI])
            VALUES (@partnerId, @docType, 1, @note, @adminId, SYSUTCDATETIME());";

        foreach (var docType in documentTypes.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(docType))
            {
                continue;
            }

            await using var insert = new SqlCommand(insertSql, connection, transaction);
            insert.Parameters.AddWithValue("@partnerId", partnerId);
            insert.Parameters.AddWithValue("@docType", docType.Trim());
            insert.Parameters.AddWithValue("@note", (object?)EmptyToNull(note) ?? DBNull.Value);
            insert.Parameters.AddWithValue("@adminId", adminUserId);
            await insert.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    private static async Task ClearMissingDocumentRequestsAsync(SqlConnection connection, SqlTransaction transaction, long partnerId, CancellationToken cancellationToken)
    {
        if (!await TableExistsAsync(connection, "partner_eksik_evrak_talepleri", cancellationToken, transaction))
        {
            return;
        }

        const string sql = @"
            UPDATE [dbo].[PARTNER_EKSIK_EVRAK_TALEPLERI]
            SET [AKTIF_MI] = 0, [TAMAMLANDI_TARIHI] = COALESCE([TAMAMLANDI_TARIHI], SYSUTCDATETIME())
            WHERE [PARTNER_ID] = @partnerId AND [AKTIF_MI] = 1;";

        await using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("@partnerId", partnerId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<AdminPartnerDocumentsPageViewModel> GetPartnerDocumentsReviewQueueAsync(string fullName, string email, string userRole, string? statusFilter, long adminUserId, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var model = new AdminPartnerDocumentsPageViewModel
        {
            StatusFilter = statusFilter ?? string.Empty,
            Shell = await GetShellAsync(connection, "Partner Evrak İnceleme", "Zorunlu evrak checklist, yükleme durumu ve admin onay kuyruğu.", fullName, email, userRole, cancellationToken)
        };

        var summaryDefinitions = new[]
        {
            ("Toplam Evrak", "SELECT COUNT(*) FROM [dbo].[PARTNER_BASVURU_EVRAKLARI]", "Yüklenen tüm belgeler", "info", "fa-folder-open"),
            ("İnceleme Bekleyen", "SELECT COUNT(*) FROM [dbo].[PARTNER_BASVURU_EVRAKLARI] WHERE COALESCE([DURUM], N'Beklemede') = N'Beklemede'", "Admin kararı bekleyen", "warning", "fa-hourglass-half"),
            ("Onaylı", "SELECT COUNT(*) FROM [dbo].[PARTNER_BASVURU_EVRAKLARI] WHERE COALESCE([DURUM], N'') IN (N'Onaylandi', N'Onaylandı')", "Uygun bulunan belgeler", "success", "fa-circle-check"),
            ("Reddedilen", "SELECT COUNT(*) FROM [dbo].[PARTNER_BASVURU_EVRAKLARI] WHERE COALESCE([DURUM], N'') = N'Reddedildi'", "Eksik/hatalı belgeler", "danger", "fa-circle-xmark")
        };

        foreach (var (label, sql, description, tone, icon) in summaryDefinitions)
        {
            await using var command = new SqlCommand(sql, connection);
            var raw = await command.ExecuteScalarAsync(cancellationToken);
            model.SummaryCards.Add(new AdminSummaryCardViewModel
            {
                Label = label,
                Value = FormatScalar(raw),
                Description = description,
                ToneClass = tone,
                IconClass = icon
            });
        }

        if (!await TableExistsAsync(connection, "partner_basvuru_evraklari", cancellationToken))
        {
            return model;
        }

        const string partnersSql = @"
            SELECT p.id, o.id AS hotel_id, p.[FIRMA_UNVANI], COALESCE(o.[OTEL_ADI], p.[FIRMA_UNVANI]),
                   COALESCE(p.[ONAY_DURUMU], N'Beklemede')
            FROM [dbo].[PARTNER_DETAYLARI] p
            LEFT JOIN [dbo].[OTELLER] o ON o.[PARTNER_ID] = p.id
            WHERE EXISTS (
                SELECT 1 FROM [dbo].[PARTNER_BASVURU_EVRAKLARI] ped
                WHERE ped.[PARTNER_ID] = p.id
                  AND (@statusFilter = N'' OR COALESCE(ped.[DURUM], N'Beklemede') = @statusFilter)
            )
               OR (
                    @statusFilter = N''
                    AND COALESCE(p.[ONAY_DURUMU], N'Beklemede') IN (N'Beklemede', N'Askida')
                    AND (
                        SELECT COUNT(*) FROM [dbo].[PARTNER_BASVURU_EVRAKLARI] ped2 WHERE ped2.[PARTNER_ID] = p.id
                    ) < @requiredCount
               )
            ORDER BY
                CASE COALESCE(p.[ONAY_DURUMU], N'Beklemede') WHEN N'Beklemede' THEN 0 WHEN N'Askida' THEN 1 ELSE 2 END,
                p.[OLUSTURULMA_TARIHI] DESC;";

        var partners = new List<(long PartnerId, long? HotelId, string Company, string Hotel, string Status)>();
        await using (var partnersCommand = new SqlCommand(partnersSql, connection))
        {
            partnersCommand.Parameters.AddWithValue("@statusFilter", model.StatusFilter);
            partnersCommand.Parameters.AddWithValue("@requiredCount", PartnerRequiredDocumentTypes.Length);
            await using var reader = await partnersCommand.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                partners.Add((reader.GetInt64(0), reader.IsDBNull(1) ? null : reader.GetInt64(1), reader.GetString(2), reader.GetString(3), reader.GetString(4)));
            }
        }

        foreach (var partner in partners)
        {
            var documents = new List<AdminPartnerDocumentItemViewModel>();
            const string docsSql = @"
                SELECT ped.id, ped.[GUVENLI_DOSYA_ID], ped.[EVRAK_TIPI], COALESCE(ped.[BELGE_BASLIGI], ped.[EVRAK_TIPI]),
                       COALESCE(gfv.[ORIJINAL_DOSYA_ADI], N'Belge'), ped.[DURUM], ped.[OLUSTURULMA_TARIHI], COALESCE(ped.[RED_NEDENI], N'')
                FROM [dbo].[PARTNER_BASVURU_EVRAKLARI] ped
                INNER JOIN [dbo].[GUVENLI_DOSYA_VARLIKLARI] gfv ON gfv.id = ped.[GUVENLI_DOSYA_ID]
                WHERE ped.[PARTNER_ID] = @partnerId
                  AND (@statusFilter = N'' OR COALESCE(ped.[DURUM], N'Beklemede') = @statusFilter)
                ORDER BY ped.[OLUSTURULMA_TARIHI] DESC;";

            await using var docsCommand = new SqlCommand(docsSql, connection);
            docsCommand.Parameters.AddWithValue("@partnerId", partner.PartnerId);
            docsCommand.Parameters.AddWithValue("@statusFilter", model.StatusFilter);
            await using var docsReader = await docsCommand.ExecuteReaderAsync(cancellationToken);
            while (await docsReader.ReadAsync(cancellationToken))
            {
                var fileId = docsReader.GetInt64(1);
                var status = docsReader.GetString(5);
                documents.Add(new AdminPartnerDocumentItemViewModel
                {
                    DocumentId = docsReader.GetInt64(0),
                    DocumentType = docsReader.GetString(2),
                    Title = docsReader.GetString(3),
                    FileName = docsReader.GetString(4),
                    StatusText = status,
                    StatusToneClass = MapPartnerDocumentTone(status),
                    UploadedAtText = docsReader.GetDateTime(6).ToString("dd.MM.yyyy HH:mm", CultureInfo.GetCultureInfo("tr-TR")),
                    ReviewNote = EmptyToNull(docsReader.GetString(7)),
                    AccessUrl = await _secureFileService.CreateAccessUrlAsync(fileId, adminUserId, "admin", cancellationToken)
                });
            }

            var checklist = BuildPartnerDocumentChecklist(documents);
            var pendingReview = documents.Count(d => string.Equals(d.StatusText, "Beklemede", StringComparison.OrdinalIgnoreCase));
            var approved = documents.Count(d => string.Equals(d.StatusText, "Onaylandi", StringComparison.OrdinalIgnoreCase) || string.Equals(d.StatusText, "Onaylandı", StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(model.StatusFilter) && documents.Count == 0)
            {
                continue;
            }

            model.Queue.Add(new AdminPartnerDocumentQueueRowViewModel
            {
                PartnerId = partner.PartnerId,
                HotelId = partner.HotelId,
                CompanyName = partner.Company,
                HotelName = partner.Hotel,
                PartnerStatus = partner.Status,
                PartnerStatusTone = MapPartnerDocumentTone(partner.Status),
                RequiredDocumentCount = PartnerRequiredDocumentTypes.Length,
                UploadedDocumentCount = documents.Count,
                PendingReviewCount = pendingReview,
                ApprovedDocumentCount = approved,
                Checklist = checklist,
                Documents = documents
            });
        }

        return model;
    }

    public async Task<(bool Success, string Message)> ReviewPartnerDocumentAsync(long adminUserId, AdminPartnerDocumentReviewRequest request, CancellationToken cancellationToken = default)
    {
        if (request.DocumentId <= 0)
        {
            return (false, "Geçersiz evrak kaydı.");
        }

        var status = request.TargetStatus?.Trim() ?? string.Empty;
        if (!string.Equals(status, "Onaylandi", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(status, "Onaylandı", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(status, "Reddedildi", StringComparison.OrdinalIgnoreCase))
        {
            return (false, "Geçersiz hedef durum.");
        }

        if (string.Equals(status, "Reddedildi", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(request.Note))
        {
            return (false, "Red gerekçesi zorunludur.");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        const string sql = @"
            UPDATE [dbo].[PARTNER_BASVURU_EVRAKLARI]
            SET [DURUM] = @status,
                [RED_NEDENI] = @note,
                [INCELEYEN_ADMIN_ID] = @adminId,
                [INCELENME_TARIHI] = SYSUTCDATETIME(),
                [GUNCELLENME_TARIHI] = SYSUTCDATETIME()
            WHERE [ID] = @documentId AND [PARTNER_ID] = @partnerId;";

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@status", string.Equals(status, "Onaylandı", StringComparison.OrdinalIgnoreCase) ? "Onaylandi" : status);
        command.Parameters.AddWithValue("@note", (object?)EmptyToNull(request.Note) ?? DBNull.Value);
        command.Parameters.AddWithValue("@adminId", adminUserId);
        command.Parameters.AddWithValue("@documentId", request.DocumentId);
        command.Parameters.AddWithValue("@partnerId", request.PartnerId);
        var affected = await command.ExecuteNonQueryAsync(cancellationToken);
        return affected > 0
            ? (true, "Evrak inceleme kararı kaydedildi.")
            : (false, "Evrak bulunamadı veya güncellenemedi.");
    }

    private static List<AdminPartnerDocumentChecklistItemViewModel> BuildPartnerDocumentChecklist(IReadOnlyList<AdminPartnerDocumentItemViewModel> documents)
    {
        var checklist = new List<AdminPartnerDocumentChecklistItemViewModel>();
        foreach (var requiredType in PartnerRequiredDocumentTypes)
        {
            var match = documents
                .Where(d => string.Equals(d.DocumentType, requiredType, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(d => d.UploadedAtText)
                .FirstOrDefault();

            if (match is null)
            {
                checklist.Add(new AdminPartnerDocumentChecklistItemViewModel
                {
                    DocumentType = requiredType,
                    StatusText = "Eksik",
                    ToneClass = "danger"
                });
                continue;
            }

            var tone = MapPartnerDocumentTone(match.StatusText);
            var statusText = match.StatusText switch
            {
                var s when string.Equals(s, "Onaylandi", StringComparison.OrdinalIgnoreCase) || string.Equals(s, "Onaylandı", StringComparison.OrdinalIgnoreCase) => "Onaylı",
                var s when string.Equals(s, "Reddedildi", StringComparison.OrdinalIgnoreCase) => "Reddedildi",
                _ => "İncelemede"
            };

            checklist.Add(new AdminPartnerDocumentChecklistItemViewModel
            {
                DocumentType = requiredType,
                StatusText = statusText,
                ToneClass = tone
            });
        }

        return checklist;
    }

    private static string MapPartnerDocumentTone(string status) => status switch
    {
        var s when string.Equals(s, "Onaylandi", StringComparison.OrdinalIgnoreCase) || string.Equals(s, "Onaylandı", StringComparison.OrdinalIgnoreCase) => "success",
        var s when string.Equals(s, "Reddedildi", StringComparison.OrdinalIgnoreCase) => "danger",
        var s when string.Equals(s, "Beklemede", StringComparison.OrdinalIgnoreCase) => "warning",
        _ => "info"
    };

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
                f.[FIRMA_ADI],
                COALESCE(f.[VERGI_NO],''),
                COALESCE(f.[YETKILI_AD_SOYAD],''),
                COALESCE(f.[YETKILI_EPOSTA], f.[FIRMA_EPOSTA], ''),
                COALESCE(f.[YETKILI_TELEFON], f.[FIRMA_TELEFON], ''),
                COALESCE(f.[ONAY_DURUMU],'Beklemede'),
                f.[OLUSTURULMA_TARIHI]
            FROM [dbo].[FIRMALAR] f
            ORDER BY
                CASE COALESCE(f.[ONAY_DURUMU],'Beklemede')
                    WHEN 'Beklemede' THEN 0
                    WHEN 'Askıda' THEN 1
                    WHEN 'Reddedildi' THEN 2
                    ELSE 3
                END,
                f.[OLUSTURULMA_TARIHI] DESC;";

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
                (SELECT COUNT(*) FROM [dbo].[OTELLER]),
                (SELECT COUNT(*) FROM [dbo].[OTELLER] WHERE COALESCE([ONAY_DURUMU],'Beklemede') <> 'Onaylandi' OR COALESCE([YAYIN_DURUMU],'Kapali') <> 'Yayinda'),
                (SELECT COUNT(*) FROM [dbo].[PARTNER_DETAYLARI] WHERE COALESCE([ONAY_DURUMU],'Beklemede') = 'Beklemede'),
                (SELECT COUNT(*) FROM [dbo].[FIRMALAR] WHERE COALESCE([ONAY_DURUMU],'Beklemede') = 'Beklemede'),
                (SELECT COUNT(*) FROM [dbo].[REZERVASYONLAR]),
                (SELECT COALESCE(SUM(COALESCE([TOPLAM_TUTAR],0)),0) FROM [dbo].[REZERVASYONLAR] WHERE COALESCE([DURUM],'') <> N'İptal Edildi'),
                (SELECT COALESCE(SUM(COALESCE([KOMISYON_TUTARI],0)),0) FROM [dbo].[KOMISYON_MUHASEBE_KAYITLARI]),
                (SELECT COUNT(*) FROM [dbo].[FATURALAR] WHERE COALESCE([FATURA_DURUMU],'Taslak') IN ('Taslak','Beklemede'));";

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
                SELECT N'Partner' AS type_name, p.id AS entity_id, p.[FIRMA_UNVANI] AS title,
                       CONCAT(COALESCE(o.[OTEL_ADI], N'Otel bağlantısı yok'), N' · ', COALESCE(p.[YETKILI_EPOSTA], N'')) AS detail,
                       COALESCE(p.[ONAY_DURUMU], N'Beklemede') AS status_text,
                       COALESCE(p.[OLUSTURULMA_TARIHI], SYSUTCDATETIME()) AS created_at,
                       CONCAT(N'/admin/partner-evraklari?partnerId=', p.id) AS action_url
                FROM [dbo].[PARTNER_DETAYLARI] p
                LEFT JOIN [dbo].[OTELLER] o ON o.[PARTNER_ID] = p.id
                WHERE COALESCE(p.[ONAY_DURUMU], N'Beklemede') <> N'Onaylandi'
                UNION ALL
                SELECT N'Firma', f.id, f.[FIRMA_ADI],
                       CONCAT(COALESCE(f.[YETKILI_EPOSTA], f.[FIRMA_EPOSTA], N''), N' · ', COALESCE(f.[VERGI_NO], N'')),
                       COALESCE(f.[ONAY_DURUMU], N'Beklemede'),
                       COALESCE(f.[OLUSTURULMA_TARIHI], SYSUTCDATETIME()),
                       N'/admin/firma-basvurulari'
                FROM [dbo].[FIRMALAR] f
                WHERE COALESCE(f.[ONAY_DURUMU], N'Beklemede') <> N'Onaylandı'
                UNION ALL
                SELECT N'Otel', o.id, o.[OTEL_ADI],
                       CONCAT(COALESCE(o.[ILCE], N''), N', ', COALESCE(o.[SEHIR], N''), N' · ', COALESCE(p.[FIRMA_UNVANI], N'Partner yok')),
                       CONCAT(COALESCE(o.[ONAY_DURUMU], N'Beklemede'), N' / ', COALESCE(o.[YAYIN_DURUMU], N'Kapali')),
                       COALESCE(o.[OLUSTURULMA_TARIHI], SYSUTCDATETIME()),
                       CONCAT(N'/admin/otel-detay/', o.id)
                FROM [dbo].[OTELLER] o
                LEFT JOIN [dbo].[PARTNER_DETAYLARI] p ON p.id = o.[PARTNER_ID]
                WHERE COALESCE(o.[ONAY_DURUMU], N'Beklemede') <> N'Onaylandi' OR COALESCE(o.[YAYIN_DURUMU], N'Kapali') <> N'Yayinda'
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
                o.id, o.[OTEL_ADI], COALESCE(p.[FIRMA_UNVANI], N'Partner yok'),
                CONCAT(COALESCE(o.[ILCE], N''), N', ', COALESCE(o.[SEHIR], N'')),
                COALESCE(o.[ONAY_DURUMU], N'Beklemede'), COALESCE(o.[YAYIN_DURUMU], N'Kapali'),
                COALESCE(o.[VARSAYILAN_KOMISYON_ORANI], 0),
                COALESCE(SUM(CASE WHEN r.[OLUSTURULMA_TARIHI] >= @monthStart AND COALESCE(r.[DURUM],'') <> N'İptal Edildi' THEN COALESCE(r.[TOPLAM_TUTAR],0) ELSE 0 END),0),
                COALESCE(SUM(CASE WHEN k.[KAYIT_TARIHI] >= @monthStart THEN COALESCE(k.[KOMISYON_TUTARI],0) ELSE 0 END),0)
            FROM [dbo].[OTELLER] o
            LEFT JOIN [dbo].[PARTNER_DETAYLARI] p ON p.id = o.[PARTNER_ID]
            LEFT JOIN [dbo].[REZERVASYONLAR] r ON r.[OTEL_ID] = o.id
            LEFT JOIN [dbo].[KOMISYON_MUHASEBE_KAYITLARI] k ON k.[OTEL_ID] = o.id
            GROUP BY o.id, o.[OTEL_ADI], p.[FIRMA_UNVANI], o.[ILCE], o.[SEHIR], o.[ONAY_DURUMU], o.[YAYIN_DURUMU], o.[VARSAYILAN_KOMISYON_ORANI]
            ORDER BY CASE WHEN COALESCE(o.[ONAY_DURUMU],'Beklemede') <> 'Onaylandi' OR COALESCE(o.[YAYIN_DURUMU],'Kapali') <> 'Yayinda' THEN 0 ELSE 1 END, o.id DESC;";

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
                f.id, COALESCE(f.[FATURA_NO], CONCAT(N'Taslak-', f.id)), COALESCE(f.[FATURA_TURU], N'Konaklama'),
                COALESCE(o.[OTEL_ADI], N'-'), COALESCE(f.[FATURA_ALICI_UNVAN], u.[AD_SOYAD], N'-'),
                COALESCE(f.[FATURA_DURUMU], N'Taslak'), COALESCE(f.[GENEL_TOPLAM], 0), COALESCE(f.[FATURA_TARIHI], f.[OLUSTURULMA_TARIHI])
            FROM [dbo].[FATURALAR] f
            LEFT JOIN [dbo].[OTELLER] o ON o.id = f.[OTEL_ID]
            LEFT JOIN [dbo].[KULLANICILAR] u ON u.id = f.[KULLANICI_ID]
            ORDER BY COALESCE(f.[FATURA_TARIHI], f.[OLUSTURULMA_TARIHI]) DESC, f.id DESC;";

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
            const string prevSql = "SELECT TOP (1) COALESCE([ONAY_DURUMU],'Beklemede'), COALESCE([FIRMA_ADI],'') FROM [dbo].[FIRMALAR] WHERE id = @id;";
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
                UPDATE [dbo].[FIRMALAR]
                SET [ONAY_DURUMU] = @status,
                    [GUNCELLENME_TARIHI] = SYSUTCDATETIME()
                WHERE id = @id;";
            await using (var updateCmd = new SqlCommand(updateSql, connection, (SqlTransaction)tx))
            {
                updateCmd.Parameters.AddWithValue("@status", target);
                updateCmd.Parameters.AddWithValue("@id", request.CompanyId);
                await updateCmd.ExecuteNonQueryAsync(cancellationToken);
            }

            // Firma başvuru hareketi (tablo varsa)
            const string existsSql = "SELECT CASE WHEN OBJECT_ID(N'[dbo].[FIRMA_BASVURU_HAREKETLERI]', N'U') IS NULL THEN 0 ELSE 1 END;";
            var exists = false;
            await using (var existsCmd = new SqlCommand(existsSql, connection, (SqlTransaction)tx))
            {
                exists = Convert.ToInt32(await existsCmd.ExecuteScalarAsync(cancellationToken) ?? 0, CultureInfo.InvariantCulture) == 1;
            }

            if (exists)
            {
                const string insertSql = @"
                    INSERT INTO [dbo].[FIRMA_BASVURU_HAREKETLERI]
                    ([FIRMA_ID], [ONCEKI_DURUM], [YENI_DURUM], [HAREKET_TIPI], [ACIKLAMA], [ISLEM_YAPAN_KULLANICI_ID], [ISLEM_KAYNAGI], [IP_ADRESI], [OLUSTURULMA_TARIHI])
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
            "Beklemede" => "Beklemede",
            _ => string.Empty
        };

        if (string.IsNullOrWhiteSpace(targetStatus))
        {
            return (false, "Gecersiz partner basvuru durumu secildi.");
        }

        if (string.Equals(targetStatus, "Onaylandi", StringComparison.OrdinalIgnoreCase))
        {
            if (!request.CommissionRate.HasValue || request.CommissionRate.Value <= 0 || request.CommissionRate.Value > 100)
            {
                return (false, "Onay icin gecerli bir komisyon orani (0-100) girmelisiniz.");
            }
        }

        if (string.Equals(targetStatus, "Askida", StringComparison.OrdinalIgnoreCase))
        {
            var missing = request.MissingDocumentTypes?
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList() ?? [];
            if (missing.Count == 0)
            {
                return (false, "Askiya almak icin en az bir eksik evrak tipi secmelisiniz.");
            }
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            const string readSql = """
                SELECT TOP (1) [KULLANICI_ID], [ONAY_DURUMU]
                FROM [dbo].[PARTNER_DETAYLARI]
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
                UPDATE [dbo].[PARTNER_DETAYLARI]
                SET [ONAY_DURUMU] = @targetStatus,
                    [ONAY_TARIHI] = CASE WHEN @targetStatus = 'Onaylandi' THEN SYSUTCDATETIME() ELSE [ONAY_TARIHI] END,
                    [ONAYLAYAN_ADMIN_ID] = @adminUserId,
                    [RED_NEDENI] = @note,
                    [GUNCELLENME_TARIHI] = SYSUTCDATETIME()
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
                UPDATE [dbo].[OTELLER]
                SET [ONAY_DURUMU] = CASE
                        WHEN @targetStatus = 'Onaylandi' THEN N'Onaylandı'
                        WHEN @targetStatus = 'Reddedildi' THEN N'Reddedildi'
                        ELSE N'Beklemede'
                    END,
                    [YAYIN_DURUMU] = CASE
                        WHEN @targetStatus = 'Onaylandi' THEN N'Yayında'
                        WHEN @targetStatus = 'Askida' THEN N'Askıda'
                        WHEN @targetStatus = 'Reddedildi' THEN N'Taslak'
                        ELSE [YAYIN_DURUMU]
                    END,
                    [VARSAYILAN_KOMISYON_ORANI] = CASE
                        WHEN @targetStatus = 'Onaylandi' THEN @commissionRate
                        ELSE [VARSAYILAN_KOMISYON_ORANI]
                    END,
                    [ONAY_TARIHI] = CASE WHEN @targetStatus = 'Onaylandi' THEN SYSUTCDATETIME() ELSE [ONAY_TARIHI] END
                WHERE [PARTNER_ID] = @partnerId;";

            await using (var hotelUpdateCommand = new SqlCommand(hotelUpdateSql, connection, (SqlTransaction)transaction))
            {
                hotelUpdateCommand.Parameters.AddWithValue("@targetStatus", targetStatus);
                hotelUpdateCommand.Parameters.AddWithValue("@partnerId", request.PartnerId);
                hotelUpdateCommand.Parameters.AddWithValue("@commissionRate", request.CommissionRate ?? 15m);
                await hotelUpdateCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            if (string.Equals(targetStatus, "Askida", StringComparison.OrdinalIgnoreCase))
            {
                await SaveMissingDocumentRequestsAsync(
                    connection,
                    (SqlTransaction)transaction,
                    request.PartnerId,
                    adminUserId,
                    request.MissingDocumentTypes!,
                    request.Note,
                    cancellationToken);
            }
            else
            {
                await ClearMissingDocumentRequestsAsync(connection, (SqlTransaction)transaction, request.PartnerId, cancellationToken);
            }

            if (await TableExistsAsync(connection, "PARTNER_BASVURU_HAREKETLERI", cancellationToken, (SqlTransaction?)transaction))
            {
                const string historySql = @"
                    INSERT INTO [dbo].[PARTNER_BASVURU_HAREKETLERI]
                    ([PARTNER_ID], [ONCEKI_DURUM], [YENI_DURUM], [ISLEM_TIPI], [ACIKLAMA], [ISLEM_YAPAN_KULLANICI_ID], [OLUSTURULMA_TARIHI])
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
                UPDATE [dbo].[KULLANICILAR]
                SET [HESAP_DURUMU] = CASE WHEN @targetStatus = 'Kara Liste' THEN 0 ELSE [HESAP_DURUMU] END
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
                "Onaylandi" => "Partner basvurusu onaylandi ve otel yayina alindi.",
                "Reddedildi" => "Partner basvurusu reddedildi.",
                "Askida" => "Partner basvurusu askiya alindi. Eksik evrak talepleri partner paneline iletildi.",
                "Beklemede" => "Partner basvurusu tekrar inceleme kuyruguna alindi.",
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
                UPDATE [dbo].[PARTNER_DETAYLARI]
                SET [EPOSTA_GIRIS_ONAYI_VERILDI_MI] = @approved,
                    [EPOSTA_GIRIS_ONAY_TARIHI] = CASE WHEN @approved = 1 THEN SYSUTCDATETIME() ELSE NULL END,
                    [EPOSTA_GIRIS_ONAYLAYAN_ADMIN_ID] = CASE WHEN @approved = 1 THEN @adminUserId ELSE NULL END,
                    [GUNCELLENME_TARIHI] = SYSUTCDATETIME()
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

            if (await TableExistsAsync(connection, "PARTNER_BASVURU_HAREKETLERI", cancellationToken, (SqlTransaction?)tx))
            {
                const string historySql = """
                    INSERT INTO [dbo].[PARTNER_BASVURU_HAREKETLERI]
                    ([PARTNER_ID], [ONCEKI_DURUM], [YENI_DURUM], [ISLEM_TIPI], [ACIKLAMA], [ISLEM_YAPAN_KULLANICI_ID], [OLUSTURULMA_TARIHI])
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
        var hasTable = await TableExistsAsync(connection, "OTEL_LISTE_ABONELIKLERI", cancellationToken);
        if (!hasTable)
        {
            model.SummaryCards.Add(new AdminSummaryCardViewModel { Label = "Abonelik Tablosu", Value = "YOK", Description = "Migration uygulanmamış.", ToneClass = "danger", IconClass = "fa-triangle-exclamation" });
            return model;
        }

        var summary = new (string Label, string Sql, string Tone, string Icon, string Desc)[]
        {
            ("Toplam", "SELECT COUNT(*) FROM [dbo].[OTEL_LISTE_ABONELIKLERI]", "info", "fa-crown", "Tüm talepler"),
            ("Bekleyen", "SELECT COUNT(*) FROM [dbo].[OTEL_LISTE_ABONELIKLERI] WHERE [DURUM] = N'Beklemede'", "warning", "fa-hourglass-half", "Admin onayı bekliyor"),
            ("Aktif", "SELECT COUNT(*) FROM [dbo].[OTEL_LISTE_ABONELIKLERI] WHERE [DURUM] = N'Onaylandı' AND SYSUTCDATETIME() BETWEEN [BASLANGIC_UTC] AND bitis_utc", "success", "fa-circle-check", "Şu anda pin uygulanıyor"),
            ("Süresi Dolan", "SELECT COUNT(*) FROM [dbo].[OTEL_LISTE_ABONELIKLERI] WHERE [DURUM] = N'Onaylandı' AND [BITIS_UTC] < SYSUTCDATETIME()", "secondary", "fa-clock", "Bitişi geçenler")
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
                a.[OTEL_ID],
                COALESCE(o.[OTEL_ADI],'-') AS [OTEL_ADI],
                CONCAT(COALESCE(o.[ILCE],''), ', ', COALESCE(o.[SEHIR],'')) AS city_text,
                COALESCE(a.[KAPSAM_TIPI],'-'),
                COALESCE(a.[KAPSAM_DEGERI],'-'),
                COALESCE(a.[HEDEF_SIRA],0),
                COALESCE(a.[DURUM],'-'),
                a.[BASLANGIC_UTC],
                a.[BITIS_UTC],
                COALESCE(u.[EPOSTA],'') AS partner_email,
                COALESCE(a.[PARTNER_NOTU],'')
            FROM [dbo].[OTEL_LISTE_ABONELIKLERI] a
            LEFT JOIN [dbo].[OTELLER] o ON o.id = a.[OTEL_ID]
            LEFT JOIN [dbo].[KULLANICILAR] u ON u.id = a.[TALEP_EDEN_KULLANICI_ID]
            ORDER BY a.[OLUSTURULMA_TARIHI] DESC, a.id DESC;";

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
        if (!await TableExistsAsync(connection, "OTEL_LISTE_ABONELIKLERI", cancellationToken))
        {
            return (false, "Abonelik tablosu bulunamadı.");
        }

        await using var tx = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            const string readSql = @"
                SELECT TOP (1)
                    [OTEL_ID], [KAPSAM_TIPI], [KAPSAM_DEGERI_NORMALIZE], [HEDEF_SIRA], [BASLANGIC_UTC], [BITIS_UTC], [DURUM]
                FROM [dbo].[OTEL_LISTE_ABONELIKLERI]
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
                    FROM [dbo].[OTEL_LISTE_ABONELIKLERI]
                    WHERE id <> @id
                      AND [DURUM] = N'Onaylandı'
                      AND [KAPSAM_TIPI] = @scopeType
                      AND [KAPSAM_DEGERI_NORMALIZE] = @scopeNorm
                      AND [HEDEF_SIRA] = @rank
                      AND (
                          (@startUtc < [BITIS_UTC]) AND (@endUtc > [BASLANGIC_UTC])
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
                UPDATE [dbo].[OTEL_LISTE_ABONELIKLERI]
                SET [DURUM] = @status,
                    [ONAYLAYAN_ADMIN_KULLANICI_ID] = @adminId,
                    [ADMIN_NOTU] = @note,
                    [ONAY_TARIHI] = CASE WHEN @status = N'Onaylandı' THEN SYSUTCDATETIME() ELSE [ONAY_TARIHI] END
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

    public async Task<AdminCommissionManagementPageViewModel> GetCommissionManagementAsync(string fullName, string email, string userRole, long? hotelId = null, DateTime? dateFrom = null, DateTime? dateTo = null, string? city = null, string? district = null, string? neighborhood = null, string? paymentStatus = null, int pageSize = 50, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var model = new AdminCommissionManagementPageViewModel
        {
            Shell = await GetShellAsync(connection, "Komisyon ve Vergi Ayarlari", "Otel bazli komisyon, KDV ve konaklama vergisi kurallarini tarih bazli yonetin.", fullName, email, userRole, cancellationToken)
        };
        model.DateFrom = dateFrom?.Date;
        model.DateTo = dateTo?.Date;
        model.City = city?.Trim() ?? string.Empty;
        model.District = district?.Trim() ?? string.Empty;
        model.Neighborhood = neighborhood?.Trim() ?? string.Empty;
        model.PaymentStatus = paymentStatus?.Trim() ?? string.Empty;
        model.PageSize = Math.Clamp(pageSize <= 0 ? 50 : pageSize, 50, 500);

        const string hotelsSql = @"
            SELECT o.id, o.[OTEL_ADI], o.[OTEL_KODU], CONCAT(o.[ILCE], ', ', o.[SEHIR]) AS sehir_label
            FROM [dbo].[OTELLER] o
            ORDER BY o.[OTEL_ADI] ASC;";

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
                (SELECT COUNT(*) FROM [dbo].[KOMISYON_VERGILER]) AS total_rule_count,
                (SELECT COUNT(DISTINCT [OTEL_ID]) FROM [dbo].[KOMISYON_VERGILER] WHERE [AKTIF_MI] = 1) AS active_hotel_count,
                (SELECT COALESCE(AVG([KOMISYON_ORANI]), 0) FROM [dbo].[KOMISYON_VERGILER] WHERE [AKTIF_MI] = 1) AS avg_commission_rate,
                (
                    SELECT COALESCE(SUM(COALESCE([KDV_ORANI], 0) + COALESCE([KONAKLAMA_VERGISI_ORANI], 0)), 0)
                    FROM [dbo].[KOMISYON_VERGILER]
                    WHERE [AKTIF_MI] = 1
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
                kv.[OTEL_ID],
                o.[OTEL_ADI],
                o.[OTEL_KODU],
                CONCAT(o.[ILCE], ', ', o.[SEHIR]) AS sehir_label,
                kv.[BASLANGIC_TARIHI],
                kv.[BITIS_TARIHI],
                kv.[KOMISYON_ORANI],
                kv.[KOMISYON_GELIR_VERGISI_ORANI],
                kv.[KDV_ORANI],
                kv.[KONAKLAMA_VERGISI_ORANI],
                kv.[AKTIF_MI],
                kv.[ACIKLAMA]
            FROM [dbo].[KOMISYON_VERGILER] kv
            INNER JOIN [dbo].[OTELLER] o ON o.id = kv.[OTEL_ID]
            WHERE (@hotelId IS NULL OR kv.[OTEL_ID] = @hotelId)
            ORDER BY kv.[OTEL_ID] ASC, kv.[BASLANGIC_TARIHI] DESC, kv.id DESC;";

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
            SELECT TOP (@take)
                o.id,
                o.[OTEL_ADI],
                COALESCE(reservationStats.gross_revenue, 0) AS gross_revenue,
                COALESCE(commissionStats.total_commission, 0) AS total_commission,
                COALESCE(commissionStats.paid_commission, 0) AS paid_commission,
                COALESCE(reservationStats.reservation_count, 0) AS reservation_count,
                COALESCE(reservationStats.completed_reservation_count, 0) AS completed_reservation_count,
                COALESCE(reservationStats.platform_net_commission_total, 0) AS platform_net_commission_total
            FROM [dbo].[OTELLER] o
            OUTER APPLY
            (
                SELECT
                    SUM(COALESCE(r.[TOPLAM_TUTAR], 0)) AS gross_revenue,
                    COUNT(*) AS reservation_count,
                    SUM(CASE WHEN COALESCE(r.[DURUM], '') = N'Tamamlandı' THEN 1 ELSE 0 END) AS completed_reservation_count,
                    SUM(COALESCE(r.[PLATFORM_NET_KOMISYON_TUTARI], 0)) AS platform_net_commission_total
                FROM [dbo].[REZERVASYONLAR] r
                WHERE r.[OTEL_ID] = o.id
                  AND COALESCE(r.[DURUM], '') <> N'İptal Edildi'
                  AND (@dateFrom IS NULL OR CAST(r.[GIRIS_TARIHI] AS date) >= CAST(@dateFrom AS date))
                  AND (@dateTo IS NULL OR CAST(r.[CIKIS_TARIHI] AS date) <= CAST(@dateTo AS date))
            ) reservationStats
            OUTER APPLY
            (
                SELECT
                    SUM(COALESCE(k.[KOMISYON_TUTARI], 0)) AS total_commission,
                    SUM(CASE WHEN COALESCE(k.[OTELE_ODEME_DURUMU], N'') = N'Ödendi' THEN COALESCE(k.[KOMISYON_TUTARI], 0) ELSE 0 END) AS paid_commission
                FROM [dbo].[KOMISYON_MUHASEBE_KAYITLARI] k
                WHERE k.[OTEL_ID] = o.id
                  AND (@dateFrom IS NULL OR CAST(k.[KAYIT_TARIHI] AS date) >= CAST(@dateFrom AS date))
                  AND (@dateTo IS NULL OR CAST(k.[KAYIT_TARIHI] AS date) <= CAST(@dateTo AS date))
            ) commissionStats
            WHERE (@hotelId IS NULL OR o.id = @hotelId)
              AND (@city = N'' OR COALESCE(o.[SEHIR], N'') LIKE N'%' + @city + N'%')
              AND (@district = N'' OR COALESCE(o.[ILCE], N'') LIKE N'%' + @district + N'%')
              AND (@neighborhood = N'' OR COALESCE(o.[MAHALLE], N'') LIKE N'%' + @neighborhood + N'%')
              AND (COALESCE(reservationStats.gross_revenue, 0) > 0 OR COALESCE(commissionStats.total_commission, 0) > 0)
              AND (
                    @paymentStatus = N''
                    OR (@paymentStatus = N'Ödendi' AND COALESCE(commissionStats.total_commission, 0) > 0 AND COALESCE(commissionStats.paid_commission, 0) >= COALESCE(commissionStats.total_commission, 0))
                    OR (@paymentStatus = N'Ödenmedi' AND COALESCE(commissionStats.total_commission, 0) > COALESCE(commissionStats.paid_commission, 0))
                  )
            ORDER BY COALESCE(reservationStats.gross_revenue, 0) DESC, o.id DESC;";

        await using (var financeCommand = new SqlCommand(financeSql, connection))
        {
            financeCommand.Parameters.AddWithValue("@take", model.PageSize);
            financeCommand.Parameters.AddWithValue("@hotelId", hotelId.HasValue ? hotelId.Value : DBNull.Value);
            financeCommand.Parameters.AddWithValue("@dateFrom", dateFrom.HasValue ? dateFrom.Value.Date : DBNull.Value);
            financeCommand.Parameters.AddWithValue("@dateTo", dateTo.HasValue ? dateTo.Value.Date : DBNull.Value);
            financeCommand.Parameters.AddWithValue("@city", model.City);
            financeCommand.Parameters.AddWithValue("@district", model.District);
            financeCommand.Parameters.AddWithValue("@neighborhood", model.Neighborhood);
            financeCommand.Parameters.AddWithValue("@paymentStatus", model.PaymentStatus);
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
            IF EXISTS (SELECT 1 FROM [dbo].[KOMISYON_VERGILER] WHERE id = @ruleId)
            BEGIN
                UPDATE [dbo].[KOMISYON_VERGILER]
                SET [OTEL_ID] = @hotelId,
                    [BASLANGIC_TARIHI] = @startDate,
                    [BITIS_TARIHI] = @endDate,
                    [KOMISYON_ORANI] = @commissionRate,
                    [KOMISYON_GELIR_VERGISI_ORANI] = @commissionIncomeTaxRate,
                    [KDV_ORANI] = @vatRate,
                    [KONAKLAMA_VERGISI_ORANI] = @accommodationTaxRate,
                    [PARA_BIRIMI] = @currency,
                    [AKTIF_MI] = 1,
                    [ACIKLAMA] = @note,
                    [GUNCELLEYEN_KULLANICI_ID] = @adminUserId,
                    [GUNCELLENME_TARIHI] = SYSUTCDATETIME()
                WHERE id = @ruleId;
            END
            ELSE
            BEGIN
                INSERT INTO [dbo].[KOMISYON_VERGILER]
                (
                    [OTEL_ID], [BASLANGIC_TARIHI], [BITIS_TARIHI], [KOMISYON_ORANI], [KOMISYON_GELIR_VERGISI_ORANI],
                    [KDV_ORANI], [KONAKLAMA_VERGISI_ORANI], [PARA_BIRIMI], [AKTIF_MI], [ACIKLAMA], [OLUSTURAN_KULLANICI_ID], [GUNCELLEYEN_KULLANICI_ID]
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
                SELECT id, [AD_SOYAD], [UNVAN], [EPOSTA], [ACIKLAMA], COALESCE([AVATAR_URL], N''), COALESCE([SIRALAMA], 0), COALESCE([AKTIF_MI], 1)
                FROM [dbo].[PLATFORM_EKIP_UYELERI]
                ORDER BY COALESCE([SIRALAMA], 0) ASC, id ASC;";
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
            IF OBJECT_ID(N'[dbo].[PLATFORM_EKIP_UYELERI]', N'U') IS NULL
            BEGIN
                RAISERROR('platform_ekip_uyeleri tablosu yok.', 16, 1);
                RETURN;
            END

            IF (@id IS NOT NULL AND EXISTS (SELECT 1 FROM [dbo].[PLATFORM_EKIP_UYELERI] WHERE id = @id))
            BEGIN
                UPDATE [dbo].[PLATFORM_EKIP_UYELERI]
                SET [AD_SOYAD] = @name,
                    [UNVAN] = @title,
                    [EPOSTA] = @email,
                    [ACIKLAMA] = @desc,
                    [AVATAR_URL] = COALESCE(NULLIF(@avatarUrl, N''), [AVATAR_URL]),
                    [SIRALAMA] = @orderNo,
                    [AKTIF_MI] = @active,
                    [GUNCELLENME_TARIHI] = SYSUTCDATETIME()
                WHERE id = @id;
            END
            ELSE
            BEGIN
                INSERT INTO [dbo].[PLATFORM_EKIP_UYELERI]([AD_SOYAD], [UNVAN], [EPOSTA], [ACIKLAMA], [AVATAR_URL], [SIRALAMA], [AKTIF_MI], [OLUSTURULMA_TARIHI])
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
            await using var cmd = new SqlCommand("DELETE FROM [dbo].[PLATFORM_EKIP_UYELERI] WHERE id=@id;", connection);
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
                (SELECT COUNT(*) FROM [dbo].[PARTNER_DETAYLARI] WHERE [ONAY_DURUMU] = 'Beklemede') AS pending_partner_applications,
                (SELECT COUNT(*) FROM [dbo].[FIRMALAR] WHERE COALESCE([ONAY_DURUMU], 'Beklemede') = 'Beklemede') AS pending_company_applications,
                (SELECT COUNT(*) FROM [dbo].[SISTEM_ICI_BILDIRIMLER] WHERE [OKUNDU_MU] = 0) AS unread_notifications,
                (SELECT COUNT(*) FROM [dbo].[SISTEM_HATA_LOGLARI] WHERE [HATA_SEVIYESI] IN ('CRITICAL','ALERT','EMERGENCY') AND [COZULDU_MU] = 0) AS critical_logs,
                (SELECT COUNT(*) FROM [dbo].[YORUMLAR] WHERE [ONAY_DURUMU] = 'Beklemede') AS pending_reviews;";

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
        if (AdminNavigationCatalog.TryGetSectionMeta(sectionKey, out var catalogMeta))
        {
            return (catalogMeta.Title, catalogMeta.Subtitle, catalogMeta.Columns, catalogMeta.EmptyMessage, null);
        }

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
            "reports" => ("Gelir / Komisyon Raporu", "Otel bazında aylık rezervasyon adedi, ciro ve komisyon toplamlarını izleyin.", new[] { "Ay", "Otel", "Rezervasyon", "Ciro", "Brüt Komisyon", "Net Komisyon" }, "Rapor kaydı bulunamadı.", "Kaynak tablo: [dbo].[REZERVASYONLAR] (komisyon snapshot)"),
            "campaigns" => ("Kampanyalar", "Kampanya performansini ve yayindaki indirim kurallarini izleyin.", new[] { "Kampanya", "Tur", "Baslangic", "Bitis", "Aktif", "Kullanim" }, "Kampanya bulunamadi.", null),
            "notifications" => ("Bildirimler", "Panel ici bildirimler ve sablon akislarini yonetin.", new[] { "Baslik", "Tur", "Onem", "Okundu", "Arsiv", "Olusturma" }, "Bildirim bulunamadi.", null),
            "settings" => ("Ayarlar", "Genel ayarlar icin veritabani karsiligi olan ayar tablolarini bir sonraki migration fazinda kuracagiz.", Array.Empty<string>(), "Ayar kaydi icin ayar tablolari gerekiyor.", "Bu ekran mevcut migration setinde karsiligi olmayan yeni tablo ailesi gerektiriyor."),
            "security" => ("Guvenlik", "Guvenlik paneli icin oturum, IP, 2FA ve audit yapisini genisletecegiz.", Array.Empty<string>(), "Guvenlik paneli migration fazinda detaylandirilacak.", "Mevcut tablolar log verir, ancak referans guvenlik ekrani icin ek yapilar gerekiyor."),
            "blog" => ("Blog Yonetimi", "Blog modulu icin yeni tablo ve medya baglantilari olusturulacak.", Array.Empty<string>(), "Blog icin veritabani tablolari henuz eklenmedi.", "Bu ekran icin blog kategori, yazi, etiket ve medya migration'lari acilacak."),
            "email-templates" => ("E-posta Sablonlari", "Mesaj ve bildirim sablonlarini veritabani uzerinden yonetin.", new[] { "Sablon", "Kategori", "Dil", "Aktif", "Sistem Geneli", "Konu" }, "Sablon kaydi bulunamadi.", null),
            "faq" => ("SSS Yonetimi", "SSS kategori ve soru/cevap akisini veritabani kayitlari ile yonetin.", new[] { "Kategori", "Soru", "One Cikan", "Aktif", "Olusturma" }, "SSS kaydi bulunamadi.", null),
            "complaints" => ("Sikayetler", "Sikayet ve itiraz yonetimi icin yeni tablo ailesi planlanacak.", Array.Empty<string>(), "Sikayet modulu tablolari henuz eklenmedi.", "Yorum raporlari var; ancak referanstaki sikayet modulu icin ayri veri modeli gerekiyor."),
            "logs" => ("Log Kayitlari", "Admin islem, sistem hata ve API loglarini merkezi olarak izleyin.", new[] { "Hedef", "Islem", "IP", "Tarih", "Kaynak", "Not" }, "Log kaydi bulunamadi.", null),
            "geo-search-logs" => ("Konum & Bölge Arama Logları", "Kullanıcının konumla arama yaptığı kayıtları; arama metni/bölgesi, yarıçap ve görünen oteller ile izleyin.", new[] { "Tarih", "Kaynak", "Arama Metni", "Arama Bölgesi", "Enlem", "Boylam", "Yarıçap(km)", "Görünen", "IP", "Cihaz" }, "Konum arama logu bulunamadı.", "Kaynak tablo: [dbo].[KULLANICI_KONUM_LOGLARI] (web/mobil arama istihbaratı)."),
            "hotel-coordinate-changes" => ("Otel Koordinat Değişimleri", "Otel enlem/boylam değişikliklerini admin bazlı audit trail ile takip edin.", new[] { "Tarih", "Admin", "Otel", "Önceki", "Yeni", "IP", "Not" }, "Koordinat değişim kaydı bulunamadı.", "Kaynak tablo: [dbo].[OTEL_KOORDINAT_DEGISIM_LOGLARI]"),
            "backups" => ("Yedekleme", "Yedekleme operasyonu icin snapshot kaydi ve dosya metadata tablolarini ekleyecegiz.", Array.Empty<string>(), "Yedekleme kaydi henuz bulunmuyor.", "Referans yedekleme ekrani icin yeni migration gerekir."),
            "countries" => ("Ülkeler", "Platform adres hiyerarşisinin üst düzey ülke kayıtlarını izleyin.", new[] { "Ülke", "ISO2", "ISO3", "Para Birimi", "Varsayılan", "Durum" }, "Ülke kaydı bulunamadı.", null),
            "roles" => ("Roller", "Platform kullanıcı rollerini departman ve seviye bilgisiyle yönetin.", new[] { "Rol Kodu", "Rol Adı", "Departman", "Seviye", "Varsayılan", "Açıklama" }, "Rol kaydı bulunamadı.", null),
            "admin-rbac-roles" => ("Admin Panel Rolleri", "Admin panel RBAC rol tanımlarını ve yetki kapsamlarını izleyin.", new[] { "Rol Kodu", "Rol Adı", "Açıklama", "Durum" }, "Admin rol kaydı bulunamadı.", null),
            "companies" => ("Firmalar", "B2B firma profillerini onay durumu ve rezervasyon hacmiyle izleyin.", new[] { "Firma", "Onay", "Kullanıcı", "Rezervasyon", "Kayıt" }, "Firma kaydı bulunamadı.", null),
            "platform-db-stats" => ("Veritabanı İstatistikleri", "Tablo satır sayıları ve platform veri hacmi özetini izleyin.", new[] { "Tablo", "Satır Sayısı", "Şema" }, "Tablo istatistiği bulunamadı.", "Kaynak: sys.tables + sys.partitions"),
            _ => ("Admin Panel", "Bu admin bolumu icin veritabani baglantisi hazirlaniyor.", Array.Empty<string>(), "Veri bulunamadi.", null)
        };
    }

    private static IEnumerable<(string Label, string Sql, string Description, string ToneClass, string IconClass)> GetSummaryDefinitions(string sectionKey)
    {
        return sectionKey switch
        {
            "users" =>
            [
                ("Toplam Kullanici", "SELECT COUNT(*) FROM [dbo].[KULLANICILAR]", "Tum hesaplar", "info", "fa-users"),
                ("Aktif Kullanici", "SELECT COUNT(*) FROM [dbo].[KULLANICILAR] WHERE [HESAP_DURUMU] = 1", "Giris yapabilen hesaplar", "success", "fa-circle-check"),
                ("Onaysiz E-posta", "SELECT COUNT(*) FROM [dbo].[KULLANICILAR] WHERE [EPOSTA_DOGRULAMA_TARIHI] IS NULL", "E-posta dogrulamasi bekleyenler", "warning", "fa-envelope-circle-check"),
                ("Pasif Kullanici", "SELECT COUNT(*) FROM [dbo].[KULLANICILAR] WHERE COALESCE([HESAP_DURUMU], 0) = 0", "Panele veya siteye erisemeyen hesaplar", "danger", "fa-user-slash")
            ],
            "managers" =>
            [
                ("Yonetici", "SELECT COUNT(*) FROM [dbo].[KULLANICILAR] WHERE rol = 'admin'", "Admin rolundeki kullanicilar", "danger", "fa-user-tie"),
                ("Departman", "SELECT COUNT(*) FROM [dbo].[DEPARTMANLAR]", "Organizasyon birimleri", "info", "fa-sitemap"),
                ("Rol", "SELECT COUNT(*) FROM [dbo].[ROLLER]", "Sistem rolleri", "warning", "fa-key"),
                ("Rol Atamasi", "SELECT COUNT(*) FROM [dbo].[KULLANICI_ROLLERI]", "Aktif veya gecmis rol kayitlari", "success", "fa-user-check")
            ],
            "hotels" =>
            [
                ("Toplam Otel", "SELECT COUNT(*) FROM [dbo].[OTELLER]", "Tum tesis kayitlari", "info", "fa-hotel"),
                ("Yayinda", "SELECT COUNT(*) FROM [dbo].[OTELLER] WHERE [YAYIN_DURUMU] = 'Yayında'", "Canli satistaki tesisler", "success", "fa-tower-broadcast"),
                ("Bekleyen Onay", "SELECT COUNT(*) FROM [dbo].[OTELLER] WHERE [ONAY_DURUMU] = 'Beklemede'", "Inceleme bekleyen tesisler", "warning", "fa-hourglass-half"),
                ("Oda Tipi", "SELECT COUNT(*) FROM [dbo].[ODA_TIPLERI]", "Toplam oda tipi sayisi", "danger", "fa-bed")
            ],
            "reservations" =>
            [
                ("Toplam Rezervasyon", "SELECT COUNT(*) FROM [dbo].[REZERVASYONLAR]", "Tum rezervasyon kayitlari", "info", "fa-calendar-check"),
                ("Onay Bekliyor", "SELECT COUNT(*) FROM [dbo].[REZERVASYONLAR] WHERE [DURUM] = 'Onay Bekliyor'", "Islem bekleyen rezervasyonlar", "warning", "fa-clock"),
                ("Tamamlandi", "SELECT COUNT(*) FROM [dbo].[REZERVASYONLAR] WHERE [DURUM] = 'Tamamlandı'", "Konaklamasi biten rezervasyonlar", "success", "fa-circle-check"),
                ("Iptal", "SELECT COUNT(*) FROM [dbo].[REZERVASYONLAR] WHERE [DURUM] = 'İptal Edildi'", "Iptal edilenler", "danger", "fa-ban")
            ],
            "payments" =>
            [
                ("Odeme Islemi", "SELECT COUNT(*) FROM [dbo].[ODEME_ISLEMLERI]", "Tum odeme hareketleri", "info", "fa-credit-card"),
                ("Basarili", "SELECT COUNT(*) FROM [dbo].[ODEME_ISLEMLERI] WHERE [ODEME_DURUMU] = 'Başarılı'", "Tamamlanan tahsilatlar", "success", "fa-circle-check"),
                ("Basarisiz", "SELECT COUNT(*) FROM [dbo].[ODEME_ISLEMLERI] WHERE [ODEME_DURUMU] = 'Başarısız'", "Reddedilen islemler", "danger", "fa-circle-xmark"),
                ("Askida/Bekleyen", "SELECT COUNT(*) FROM [dbo].[ODEME_ISLEMLERI] WHERE [ODEME_DURUMU] IN ('Beklemede','İşleniyor','Askıda')", "Inceleme veya islem bekleyenler", "warning", "fa-hourglass-half")
            ],
            "invoices" =>
            [
                ("Toplam Fatura", "SELECT COUNT(*) FROM [dbo].[FATURALAR]", "Sistemdeki tum fatura kayitlari", "info", "fa-file-invoice"),
                ("Kesildi", "SELECT COUNT(*) FROM [dbo].[FATURALAR] WHERE [FATURA_DURUMU] = 'Kesildi'", "Aktif kesilmis faturalar", "success", "fa-file-circle-check"),
                ("Taslak", "SELECT COUNT(*) FROM [dbo].[FATURALAR] WHERE [FATURA_DURUMU] = 'Taslak'", "Hazirlik asamasindakiler", "warning", "fa-file-pen"),
                ("Iptal", "SELECT COUNT(*) FROM [dbo].[FATURALAR] WHERE [FATURA_DURUMU] = 'İptal Edildi'", "Iptal edilen faturalar", "danger", "fa-file-circle-xmark")
            ],
            "commissions" =>
            [
                ("Komisyon Kaydi", "SELECT COUNT(*) FROM [dbo].[KOMISYON_MUHASEBE_KAYITLARI]", "Muhasebe donem kayitlari", "info", "fa-percent"),
                ("Beklemede", "SELECT COUNT(*) FROM [dbo].[KOMISYON_MUHASEBE_KAYITLARI] WHERE [OTELE_ODEME_DURUMU] = 'Beklemede'", "Otele odeme bekleyenler", "warning", "fa-wallet"),
                ("Odendi", "SELECT COUNT(*) FROM [dbo].[KOMISYON_MUHASEBE_KAYITLARI] WHERE [OTELE_ODEME_DURUMU] = 'Ödendi'", "Kapatilan odemeler", "success", "fa-money-bill-transfer"),
                ("Itirazli", "SELECT COUNT(*) FROM [dbo].[KOMISYON_MUHASEBE_KAYITLARI] WHERE [ITIRAZ_VAR_MI] = 1", "Mutabakat itirazli kayitlar", "danger", "fa-scale-balanced")
            ],
            "reports" =>
            [
                ("30 Gün Ciro", "SELECT COALESCE(SUM(COALESCE(r.[TOPLAM_TUTAR],0)),0) FROM [dbo].[REZERVASYONLAR] r WHERE COALESCE(r.[DURUM],'') <> 'İptal Edildi' AND r.[GIRIS_TARIHI] >= DATEADD(day, -30, CAST(GETDATE() AS date))", "İptal hariç toplam", "success", "fa-money-bill-wave"),
                ("30 Gün Brüt Komisyon", "SELECT COALESCE(SUM(COALESCE(r.[KOMISYON_TUTARI],0)),0) FROM [dbo].[REZERVASYONLAR] r WHERE COALESCE(r.[DURUM],'') <> 'İptal Edildi' AND r.[GIRIS_TARIHI] >= DATEADD(day, -30, CAST(GETDATE() AS date))", "Tahakkuk eden brüt", "info", "fa-percent"),
                ("30 Gün Net Komisyon", "SELECT COALESCE(SUM(COALESCE(r.[PLATFORM_NET_KOMISYON_TUTARI],0)),0) FROM [dbo].[REZERVASYONLAR] r WHERE COALESCE(r.[DURUM],'') <> 'İptal Edildi' AND r.[GIRIS_TARIHI] >= DATEADD(day, -30, CAST(GETDATE() AS date))", "Platform net", "primary", "fa-coins"),
                ("Bu Ay Rezervasyon", "SELECT COUNT(*) FROM [dbo].[REZERVASYONLAR] r WHERE COALESCE(r.[DURUM],'') <> 'İptal Edildi' AND r.[GIRIS_TARIHI] >= DATEFROMPARTS(YEAR(GETDATE()), MONTH(GETDATE()), 1)", "Ay başlangıcından bugüne", "warning", "fa-calendar-check")
            ],
            "company-reservations" =>
            [
                ("Firma Rezervasyonu", "SELECT COUNT(*) FROM [dbo].[REZERVASYONLAR] WHERE [FIRMA_ID] IS NOT NULL", "Firma bağlı tüm kayıtlar", "info", "fa-briefcase"),
                ("Onay Bekleyen", "SELECT COUNT(*) FROM [dbo].[REZERVASYONLAR] WHERE [FIRMA_ID] IS NOT NULL AND [FIRMA_ONAY_DURUMU] = 'Beklemede'", "Firma onay akışında", "warning", "fa-hourglass-half"),
                ("İptal", "SELECT COUNT(*) FROM [dbo].[REZERVASYONLAR] WHERE [FIRMA_ID] IS NOT NULL AND [DURUM] = 'İptal Edildi'", "İptal edilenler", "danger", "fa-ban"),
                ("Toplam Tutar", "SELECT COALESCE(SUM(COALESCE([TOPLAM_TUTAR],0)),0) FROM [dbo].[REZERVASYONLAR] WHERE [FIRMA_ID] IS NOT NULL AND COALESCE([DURUM],'') <> 'İptal Edildi'", "İptal hariç ciro", "success", "fa-money-bill-wave")
            ],
            "partner-applications" =>
            [
                ("Toplam Partner", "SELECT COUNT(*) FROM [dbo].[PARTNER_DETAYLARI]", "Tum partner hesaplari", "info", "fa-handshake-angle"),
                ("Beklemede", "SELECT COUNT(*) FROM [dbo].[PARTNER_DETAYLARI] WHERE [ONAY_DURUMU] = 'Beklemede'", "Inceleme bekleyen basvurular", "warning", "fa-hourglass-half"),
                ("Onaylandi", "SELECT COUNT(*) FROM [dbo].[PARTNER_DETAYLARI] WHERE [ONAY_DURUMU] = 'Onaylandi'", "Aktif partner hesaplari", "success", "fa-circle-check"),
                ("Reddedildi", "SELECT COUNT(*) FROM [dbo].[PARTNER_DETAYLARI] WHERE [ONAY_DURUMU] = 'Reddedildi'", "Reddedilen kayitlar", "danger", "fa-circle-xmark")
            ],
            "company-applications" =>
            [
                ("Toplam Firma", "SELECT COUNT(*) FROM [dbo].[FIRMALAR]", "Tum firma profilleri", "info", "fa-building"),
                ("Beklemede", "SELECT COUNT(*) FROM [dbo].[FIRMALAR] WHERE COALESCE([ONAY_DURUMU],'Beklemede') = 'Beklemede'", "Onay bekleyen firmalar", "warning", "fa-hourglass-half"),
                ("Onaylandi", "SELECT COUNT(*) FROM [dbo].[FIRMALAR] WHERE COALESCE([ONAY_DURUMU],'') = 'Onaylandı'", "Aktif firma hesaplari", "success", "fa-circle-check"),
                ("Firma Rezervasyonu", "SELECT COUNT(*) FROM [dbo].[REZERVASYONLAR] WHERE [FIRMA_ID] IS NOT NULL", "Firma baglantili rezervasyonlar", "danger", "fa-briefcase")
            ],
            "platform-officials" =>
            [
                ("Yetkili Hesap", "SELECT COUNT(*) FROM [dbo].[KULLANICILAR] WHERE rol IN ('admin','superadmin')", "Admin ve superadmin kullanicilar", "info", "fa-user-shield"),
                ("Aktif Yetkili", "SELECT COUNT(*) FROM [dbo].[KULLANICILAR] WHERE rol IN ('admin','superadmin') AND [HESAP_DURUMU] = 1", "Panele erisebilen yetkililer", "success", "fa-user-check"),
                ("Departman Kaydi", "SELECT COUNT(*) FROM [dbo].[KULLANICI_DEPARTMAN]", "Yetkili departman baglantilari", "warning", "fa-sitemap"),
                ("Rol Kaydi", "SELECT COUNT(*) FROM [dbo].[KULLANICI_ROLLERI]", "Rol atama kayitlari", "danger", "fa-key")
            ],
            "active-hotels" =>
            [
                ("Yayinda", "SELECT COUNT(*) FROM [dbo].[OTELLER] WHERE [YAYIN_DURUMU] = 'Yayında' AND [ONAY_DURUMU] = 'Onaylandı'", "Yayinda ve onayli oteller", "success", "fa-tower-broadcast"),
                ("Toplam Oda Tipi", "SELECT COUNT(*) FROM [dbo].[ODA_TIPLERI] ot INNER JOIN [dbo].[OTELLER] o ON o.id = ot.[OTEL_ID] WHERE o.[YAYIN_DURUMU] = 'Yayında' AND o.[ONAY_DURUMU] = 'Onaylandı'", "Acik otellerdeki oda tipleri", "info", "fa-bed"),
                ("Toplam Rezervasyon", "SELECT COUNT(*) FROM [dbo].[REZERVASYONLAR] r INNER JOIN [dbo].[OTELLER] o ON o.id = r.[OTEL_ID] WHERE o.[YAYIN_DURUMU] = 'Yayında' AND o.[ONAY_DURUMU] = 'Onaylandı'", "Acik otellere gelen rezervasyonlar", "warning", "fa-calendar-check"),
                ("Toplam Gelir", "SELECT COALESCE(SUM(COALESCE(r.[TOPLAM_TUTAR],0)),0) FROM [dbo].[REZERVASYONLAR] r INNER JOIN [dbo].[OTELLER] o ON o.id = r.[OTEL_ID] WHERE o.[YAYIN_DURUMU] = 'Yayında' AND o.[ONAY_DURUMU] = 'Onaylandı' AND COALESCE(r.[DURUM],'') <> 'İptal Edildi'", "Iptal disi rezervasyon gelirleri", "danger", "fa-money-bill-wave")
            ],
            "pending-hotels" =>
            [
                ("Bekleyen Onay", "SELECT COUNT(*) FROM [dbo].[OTELLER] WHERE [ONAY_DURUMU] = 'Beklemede'", "Onay bekleyen tesisler", "warning", "fa-hourglass-half"),
                ("Taslak Yayin", "SELECT COUNT(*) FROM [dbo].[OTELLER] WHERE [YAYIN_DURUMU] <> 'Yayında'", "Yayina alinmamis tesisler", "info", "fa-file-pen"),
                ("Partner Basvuru Bekliyor", "SELECT COUNT(*) FROM [dbo].[PARTNER_DETAYLARI] WHERE [ONAY_DURUMU] = 'Beklemede'", "Partner adiminda bekleyenler", "danger", "fa-user-clock"),
                ("Eksik Medya", "SELECT COUNT(*) FROM [dbo].[OTELLER] o WHERE NOT EXISTS (SELECT 1 FROM [dbo].[OTEL_GORSELLERI] g WHERE g.[OTEL_ID] = o.id)", "Gorsel yuklenmemis oteller", "success", "fa-image")
            ],
            "reviews" =>
            [
                ("Toplam Yorum", "SELECT COUNT(*) FROM [dbo].[YORUMLAR]", "Tesis yorumlari", "info", "fa-star"),
                ("Beklemede", "SELECT COUNT(*) FROM [dbo].[YORUMLAR] WHERE [ONAY_DURUMU] = 'Beklemede'", "Moderasyon bekleyenler", "warning", "fa-hourglass-half"),
                ("Onaylandi", "SELECT COUNT(*) FROM [dbo].[YORUMLAR] WHERE [ONAY_DURUMU] = 'Onaylandı'", "Yayinda olan yorumlar", "success", "fa-thumbs-up"),
                ("Raporlandi", "SELECT COUNT(*) FROM [dbo].[YORUMLAR] WHERE [RAPOR_SAYISI] > 0", "Incelenmesi gerekenler", "danger", "fa-flag")
            ],
            "campaigns" =>
            [
                ("Kampanya", "SELECT COUNT(*) FROM [dbo].[KAMPANYALAR]", "Tum kampanya kayitlari", "info", "fa-bullhorn"),
                ("Aktif", "SELECT COUNT(*) FROM [dbo].[KAMPANYALAR] WHERE [AKTIF_MI] = 1", "Yayinda kampanyalar", "success", "fa-badge-percent"),
                ("One Cikan", "SELECT COUNT(*) FROM [dbo].[KAMPANYALAR] WHERE [ONE_CIKAN_KAMPANYA] = 1", "Ana sayfa on plana cikacak kampanyalar", "warning", "fa-fire"),
                ("Toplam Kullanim", "SELECT COALESCE(SUM([KULLANILAN_ADET]),0) FROM [dbo].[KAMPANYALAR]", "Kampanya kullanim adedi", "danger", "fa-chart-column")
            ],
            "notifications" =>
            [
                ("Sistem Bildirimi", "SELECT COUNT(*) FROM [dbo].[SISTEM_ICI_BILDIRIMLER]", "Tum panel bildirimleri", "info", "fa-bell"),
                ("Okunmamis", "SELECT COUNT(*) FROM [dbo].[SISTEM_ICI_BILDIRIMLER] WHERE [OKUNDU_MU] = 0", "Henuz gorulmeyen bildirimler", "warning", "fa-envelope-open-text"),
                ("Bildirim Sablonu", "SELECT COUNT(*) FROM [dbo].[BILDIRIM_SABLONLARI]", "Push/SMS/mail sablonlari", "success", "fa-file-lines"),
                ("Mesaj Sablonu", "SELECT COUNT(*) FROM [dbo].[MESAJ_SABLONLARI]", "Operasyonel mesaj sablonlari", "danger", "fa-comments")
            ],
            "logs" =>
            [
                ("Admin Islem Logu", "SELECT COUNT(*) FROM [dbo].[ADMIN_ISLEM_LOGLARI]", "Yonetici aksiyon kayitlari", "info", "fa-clipboard-list"),
                ("Sistem Hata", "SELECT COUNT(*) FROM [dbo].[SISTEM_HATA_LOGLARI]", "Uygulama hata kayitlari", "danger", "fa-bug"),
                ("API Logu", "SELECT COUNT(*) FROM [dbo].[API_LOGLARI]", "API erisim loglari", "warning", "fa-cloud-arrow-up"),
                ("Kullanici Aktivitesi", "SELECT COUNT(*) FROM [dbo].[KULLANICI_AKTIVITE_LOGLARI]", "Oturum ve hareket gecmisi", "success", "fa-user-clock")
            ],
            "geo-search-logs" =>
            [
                ("Toplam Log", "SELECT COUNT(*) FROM [dbo].[KULLANICI_KONUM_LOGLARI]", "Konumla arama log kayitlari", "info", "fa-location-dot"),
                ("Bugün", "SELECT COUNT(*) FROM [dbo].[KULLANICI_KONUM_LOGLARI] WHERE CAST([KAYIT_TARIHI] AS date) = CAST(SYSUTCDATETIME() AS date)", "Bugün üretilen kayıtlar", "success", "fa-calendar-day"),
                ("Konum Araması", "SELECT COUNT(*) FROM [dbo].[KULLANICI_KONUM_LOGLARI] WHERE COALESCE([ARAMA_BOLGESI],'') <> ''", "Arama bölgesi dolu kayıtlar", "warning", "fa-map-location-dot"),
                ("Yarıçaplı", "SELECT COUNT(*) FROM [dbo].[KULLANICI_KONUM_LOGLARI] WHERE [YARICAP_KM] IS NOT NULL", "Yarıçap parametreli kayıtlar", "danger", "fa-circle-nodes")
            ],
            "hotel-coordinate-changes" =>
            [
                ("Toplam Değişim", "SELECT COUNT(*) FROM [dbo].[OTEL_KOORDINAT_DEGISIM_LOGLARI]", "Koordinat değişim kayıtları", "info", "fa-route"),
                ("Bugün", "SELECT COUNT(*) FROM [dbo].[OTEL_KOORDINAT_DEGISIM_LOGLARI] WHERE CAST([KAYIT_TARIHI] AS date) = CAST(SYSUTCDATETIME() AS date)", "Bugün yapılan değişimler", "success", "fa-calendar-day"),
                ("Farklı IP", "SELECT COUNT(DISTINCT [IP_ADRESI]) FROM [dbo].[OTEL_KOORDINAT_DEGISIM_LOGLARI]", "Değişim yapılan IP çeşitliliği", "warning", "fa-globe"),
                ("Farklı Otel", "SELECT COUNT(DISTINCT [OTEL_ID]) FROM [dbo].[OTEL_KOORDINAT_DEGISIM_LOGLARI]", "Etkilenen otel sayısı", "danger", "fa-hotel")
            ],
            "email-templates" =>
            [
                ("Mesaj Sablonu", "SELECT COUNT(*) FROM [dbo].[MESAJ_SABLONLARI]", "Mail/mesaj sablon seti", "info", "fa-envelope"),
                ("Bildirim Sablonu", "SELECT COUNT(*) FROM [dbo].[BILDIRIM_SABLONLARI]", "Push/SMS/system ici sablonlar", "warning", "fa-paper-plane"),
                ("Aktif Mesaj", "SELECT COUNT(*) FROM [dbo].[MESAJ_SABLONLARI] WHERE [AKTIF_MI] = 1", "Kullanilan mail sablonlari", "success", "fa-circle-check"),
                ("Aktif Bildirim", "SELECT COUNT(*) FROM [dbo].[BILDIRIM_SABLONLARI] WHERE [AKTIF_MI] = 1", "Yayinda bildirim sablonlari", "danger", "fa-bell-concierge")
            ],
            "faq" =>
            [
                ("SSS Kategorisi", "SELECT COUNT(*) FROM [dbo].[SSS_KATEGORILERI] WHERE [AKTIF_MI] = 1", "Aktif destek kategorileri", "info", "fa-layer-group"),
                ("Toplam Soru", "SELECT COUNT(*) FROM [dbo].[SSS_SORULARI]", "Tum soru ve cevap kayitlari", "warning", "fa-circle-question"),
                ("One Cikan", "SELECT COUNT(*) FROM [dbo].[SSS_SORULARI] WHERE [ONE_CIKAN_MI] = 1", "Ana akista vurgulanan sorular", "success", "fa-fire"),
                ("Aktif", "SELECT COUNT(*) FROM [dbo].[SSS_SORULARI] WHERE [AKTIF_MI] = 1", "Yayinda olan soru/cevaplar", "danger", "fa-circle-check")
            ],
            "countries" =>
            [
                ("Ülke", "SELECT COUNT(*) FROM [dbo].[ULKELER]", "Tüm ülke kayıtları", "info", "fa-globe"),
                ("Aktif", "SELECT COUNT(*) FROM [dbo].[ULKELER] WHERE [AKTIF_MI] = 1", "Kullanımda", "success", "fa-circle-check"),
                ("Varsayılan", "SELECT COUNT(*) FROM [dbo].[ULKELER] WHERE [VARSAYILAN_ULKE] = 1", "Varsayılan ülke", "warning", "fa-flag"),
                ("İl Bağlantısı", "SELECT COUNT(DISTINCT [ULKE_ID]) FROM [dbo].[ILLER]", "İli olan ülke", "danger", "fa-map")
            ],
            "roles" =>
            [
                ("Rol", "SELECT COUNT(*) FROM [dbo].[ROLLER]", "Platform rolleri", "info", "fa-key"),
                ("Varsayılan", "SELECT COUNT(*) FROM [dbo].[ROLLER] WHERE [VARSAYILAN_MI] = 1", "Varsayılan rol", "success", "fa-star"),
                ("Departman", "SELECT COUNT(DISTINCT [DEPARTMAN]) FROM [dbo].[ROLLER] WHERE [DEPARTMAN] IS NOT NULL", "Departman çeşidi", "warning", "fa-sitemap"),
                ("Rol Ataması", "SELECT COUNT(*) FROM [dbo].[KULLANICI_ROLLERI]", "Kullanıcı-rol eşlemesi", "danger", "fa-user-check")
            ],
            "admin-rbac-roles" =>
            [
                ("Admin Rol", "SELECT COUNT(*) FROM [dbo].[ADMIN_ROLLER]", "Panel rolleri", "info", "fa-user-shield"),
                ("Aktif Rol", "SELECT COUNT(*) FROM [dbo].[ADMIN_ROLLER] WHERE [ACTIVE] = 1", "Aktif tanımlar", "success", "fa-circle-check"),
                ("Yetki", "SELECT COUNT(*) FROM [dbo].[ADMIN_YETKILER] WHERE [ACTIVE] = 1", "Tanımlı yetkiler", "warning", "fa-lock"),
                ("Rol-Yetki", "SELECT COUNT(*) FROM [dbo].[ADMIN_ROL_YETKILER] WHERE [ACTIVE] = 1", "Eşleşmeler", "danger", "fa-link")
            ],
            "companies" =>
            [
                ("Firma", "SELECT COUNT(*) FROM [dbo].[FIRMALAR]", "Tüm firmalar", "info", "fa-building"),
                ("Onay Bekleyen", "SELECT COUNT(*) FROM [dbo].[FIRMALAR] WHERE COALESCE([ONAY_DURUMU],'Beklemede') = 'Beklemede'", "İnceleme bekleyen", "warning", "fa-hourglass-half"),
                ("Onaylı", "SELECT COUNT(*) FROM [dbo].[FIRMALAR] WHERE COALESCE([ONAY_DURUMU],'') = 'Onaylandı'", "Aktif firmalar", "success", "fa-circle-check"),
                ("Firma Rezervasyonu", "SELECT COUNT(*) FROM [dbo].[REZERVASYONLAR] WHERE [FIRMA_ID] IS NOT NULL", "B2B rezervasyon", "danger", "fa-briefcase")
            ],
            "platform-db-stats" =>
            [
                ("Tablo", "SELECT COUNT(*) FROM sys.tables WHERE is_ms_shipped = 0", "Kullanıcı tabloları", "info", "fa-table"),
                ("Toplam Satır", "SELECT COALESCE(SUM(p.[rows]),0) FROM sys.tables t INNER JOIN sys.partitions p ON t.[object_id]=p.[object_id] WHERE t.is_ms_shipped=0 AND p.index_id IN (0,1)", "Yaklaşık satır", "success", "fa-database"),
                ("Otel", "SELECT COUNT(*) FROM [dbo].[OTELLER]", "Otel kaydı", "warning", "fa-hotel"),
                ("Rezervasyon", "SELECT COUNT(*) FROM [dbo].[REZERVASYONLAR]", "Rezervasyon", "danger", "fa-calendar-check")
            ],
            _ => []
        };
    }

    private static string GetTableSql(string sectionKey)
    {
        if (AdminNavigationCatalog.TryGetSectionMeta(sectionKey, out var catalogMeta))
        {
            return catalogMeta.ListSql;
        }

        return sectionKey switch
        {
            "users" => @"SELECT TOP (40)
                                CAST(u.id AS nvarchar(30)),
                                COALESCE(NULLIF(u.[AD_SOYAD], ''), '-'),
                                COALESCE(NULLIF(u.[EPOSTA], ''), '-'),
                                COALESCE(NULLIF(u.[TELEFON], ''), NULLIF(u.[TELEFON_E164], ''), '-'),
                                CASE
                                    WHEN reservationStats.reservation_count >= 10 OR reservationStats.total_spent >= 100000 THEN 'Gold'
                                    WHEN reservationStats.reservation_count >= 4 OR reservationStats.total_spent >= 30000 THEN 'Silver'
                                    ELSE 'Bronze'
                                END,
                                CAST(reservationStats.reservation_count AS nvarchar(20)),
                                FORMAT(reservationStats.loyalty_points, 'N0', 'tr-TR'),
                                CASE
                                    WHEN COALESCE(u.[HESAP_DURUMU], 0) = 0 THEN 'Pasif'
                                    WHEN u.[EPOSTA_DOGRULAMA_TARIHI] IS NULL THEN 'Onaysiz'
                                    ELSE 'Aktif'
                                END,
                                COALESCE(NULLIF(u.[ROL], ''), 'user'),
                                FORMAT(u.[OLUSTURULMA_TARIHI], 'dd.MM.yyyy', 'tr-TR')
                         FROM [dbo].[KULLANICILAR] u
                         OUTER APPLY
                         (
                             SELECT
                                 COUNT(r.id) AS reservation_count,
                                 COALESCE(SUM(COALESCE(r.[TOPLAM_TUTAR], 0)), 0) AS total_spent,
                                 CAST(ROUND(COALESCE(SUM(COALESCE(r.[TOPLAM_TUTAR], 0)), 0) / 12.5, 0) AS int) AS loyalty_points
                             FROM [dbo].[REZERVASYONLAR] r
                             WHERE r.[KULLANICI_ID] = u.id
                               AND COALESCE(r.[DURUM], '') <> 'İptal Edildi'
                         ) reservationStats
                         ORDER BY u.id DESC;",
            "managers" => @"SELECT TOP (12) u.[AD_SOYAD], u.[EPOSTA], COALESCE(d.[DEPARTMAN_ADI], '-'), COALESCE(r.[ROL_ADI], u.[ROL]), COALESCE(FORMAT(u.[SON_GIRIS_TARIHI], 'dd.MM.yyyy HH:mm', 'tr-TR'), '-') FROM [dbo].[KULLANICILAR] u LEFT JOIN [dbo].[KULLANICI_DEPARTMAN] kd ON kd.[KULLANICI_ID] = u.id LEFT JOIN [dbo].[DEPARTMANLAR] d ON d.id = kd.[DEPARTMAN_ID] LEFT JOIN [dbo].[KULLANICI_ROLLERI] kr ON kr.[KULLANICI_ID] = u.id AND (kr.[BITIS_TARIHI] IS NULL OR kr.[BITIS_TARIHI] > SYSUTCDATETIME()) LEFT JOIN [dbo].[ROLLER] r ON r.id = kr.[ROL_ID] WHERE u.[ROL] = 'admin' ORDER BY u.id DESC;",
            "hotels" => @"SELECT TOP (12) [OTEL_ADI], CONCAT(ilce, ', ', [SEHIR]), [OTEL_TURU], [YAYIN_DURUMU], [ONAY_DURUMU], FORMAT([ORTALAMA_PUAN], '0.0', 'tr-TR') FROM [dbo].[OTELLER] ORDER BY id DESC;",
            "reservations" => @"SELECT TOP (12) [REZERVASYON_NO], [MISAFIR_AD_SOYAD], FORMAT([GIRIS_TARIHI], 'dd.MM.yyyy', 'tr-TR'), FORMAT([CIKIS_TARIHI], 'dd.MM.yyyy', 'tr-TR'), [DURUM], FORMAT([TOPLAM_TUTAR], 'N0', 'tr-TR') FROM [dbo].[REZERVASYONLAR] ORDER BY id DESC;",
            "payments" => @"SELECT TOP (12) [ISLEM_NO], [ODEME_TURU], [ODEME_DURUMU], [ODEME_YONTEMI], FORMAT([TOPLAM_TAHSILAT], 'N0', 'tr-TR'), FORMAT([ODEME_BASLANGIC_TARIHI], 'dd.MM.yyyy HH:mm', 'tr-TR') FROM [dbo].[ODEME_ISLEMLERI] ORDER BY id DESC;",
            "invoices" => @"SELECT TOP (12) [FATURA_NO], FORMAT([FATURA_TARIHI], 'dd.MM.yyyy', 'tr-TR'), [FATURA_TURU], [FATURA_DURUMU], FORMAT([GENEL_TOPLAM], 'N0', 'tr-TR'), [PARA_BIRIMI] FROM [dbo].[FATURALAR] ORDER BY id DESC;",
            "commissions" => @"SELECT TOP (12) [KAYIT_NO], [DONEM], o.[OTEL_ADI], FORMAT([KOMISYON_TUTARI], 'N0', 'tr-TR'), [OTELE_ODEME_DURUMU], [MUTABAKAT_DURUMU] FROM [dbo].[KOMISYON_MUHASEBE_KAYITLARI] k LEFT JOIN [dbo].[OTELLER] o ON o.id = k.[OTEL_ID] ORDER BY k.id DESC;",
            "reports" => @"SELECT TOP (240)
                                FORMAT(DATEFROMPARTS(YEAR(r.[GIRIS_TARIHI]), MONTH(r.[GIRIS_TARIHI]), 1), 'yyyy-MM', 'en-US') AS ay,
                                o.[OTEL_ADI] AS otel,
                                CAST(COUNT(*) AS nvarchar(20)) AS rezervasyon,
                                FORMAT(COALESCE(SUM(COALESCE(r.[TOPLAM_TUTAR],0)),0), 'N0', 'tr-TR') AS ciro,
                                FORMAT(COALESCE(SUM(COALESCE(r.[KOMISYON_TUTARI],0)),0), 'N0', 'tr-TR') AS brut_komisyon,
                                FORMAT(COALESCE(SUM(COALESCE(r.[PLATFORM_NET_KOMISYON_TUTARI],0)),0), 'N0', 'tr-TR') AS net_komisyon
                         FROM [dbo].[REZERVASYONLAR] r
                         INNER JOIN [dbo].[OTELLER] o ON o.id = r.[OTEL_ID]
                         WHERE COALESCE(r.[DURUM],'') <> 'İptal Edildi'
                           AND r.[GIRIS_TARIHI] >= DATEADD(month, -6, DATEFROMPARTS(YEAR(GETDATE()), MONTH(GETDATE()), 1))
                         GROUP BY DATEFROMPARTS(YEAR(r.[GIRIS_TARIHI]), MONTH(r.[GIRIS_TARIHI]), 1), o.[OTEL_ADI]
                         ORDER BY DATEFROMPARTS(YEAR(r.[GIRIS_TARIHI]), MONTH(r.[GIRIS_TARIHI]), 1) DESC, COALESCE(SUM(COALESCE(r.[TOPLAM_TUTAR],0)),0) DESC;",
            "company-reservations" => @"SELECT TOP (120)
                                            r.[REZERVASYON_NO],
                                            COALESCE(f.[FIRMA_ADI],'-') AS firma,
                                            COALESCE(u.[AD_SOYAD], '-') AS personel,
                                            o.[OTEL_ADI],
                                            CONCAT(o.[ILCE], ', ', o.[SEHIR]) AS konum,
                                            FORMAT(r.[GIRIS_TARIHI], 'dd.MM.yyyy', 'tr-TR'),
                                            FORMAT(r.[CIKIS_TARIHI], 'dd.MM.yyyy', 'tr-TR'),
                                            COALESCE(r.[DURUM],'-'),
                                            COALESCE(r.[FIRMA_ONAY_DURUMU],'-'),
                                            FORMAT(COALESCE(r.[TOPLAM_TUTAR],0), 'N0', 'tr-TR')
                                         FROM [dbo].[REZERVASYONLAR] r
                                         INNER JOIN [dbo].[OTELLER] o ON o.id = r.[OTEL_ID]
                                         LEFT JOIN [dbo].[FIRMALAR] f ON f.id = r.[FIRMA_ID]
                                         LEFT JOIN [dbo].[KULLANICILAR] u ON u.id = r.[FIRMA_CALISAN_ID]
                                         WHERE r.[FIRMA_ID] IS NOT NULL
                                         ORDER BY r.[OLUSTURULMA_TARIHI] DESC, r.id DESC;",
            "partner-applications" => @"SELECT TOP (12) [FIRMA_UNVANI], [YETKILI_AD_SOYAD], [YETKILI_EPOSTA], [VERGI_NUMARASI], [ONAY_DURUMU], FORMAT([OLUSTURULMA_TARIHI], 'dd.MM.yyyy', 'tr-TR') FROM [dbo].[PARTNER_DETAYLARI] ORDER BY id DESC;",
            "company-applications" => @"SELECT TOP (20) f.[FIRMA_ADI], COALESCE(f.[ONAY_DURUMU], 'Beklemede'),
                                                (SELECT COUNT(*) FROM [dbo].[KULLANICILAR] u WHERE u.[FIRMA_ID] = f.id AND u.[ROL] LIKE 'firma_%'),
                                                (SELECT COUNT(*) FROM [dbo].[REZERVASYONLAR] r WHERE r.[FIRMA_ID] = f.id),
                                                FORMAT(f.[OLUSTURULMA_TARIHI], 'dd.MM.yyyy', 'tr-TR')
                                         FROM [dbo].[FIRMALAR] f
                                         ORDER BY f.id DESC;",
            "platform-officials" => @"SELECT TOP (20) u.[AD_SOYAD], u.[EPOSTA], COALESCE(NULLIF(u.[ROL], ''), 'admin'),
                                               CASE WHEN COALESCE(u.[HESAP_DURUMU], 0) = 1 THEN 'Aktif' ELSE 'Pasif' END,
                                               COALESCE(FORMAT(u.[SON_GIRIS_TARIHI], 'dd.MM.yyyy HH:mm', 'tr-TR'), '-'),
                                               FORMAT(u.[OLUSTURULMA_TARIHI], 'dd.MM.yyyy', 'tr-TR')
                                        FROM [dbo].[KULLANICILAR] u
                                        WHERE u.[ROL] IN ('admin', 'superadmin')
                                        ORDER BY COALESCE(u.[SON_GIRIS_TARIHI], u.[OLUSTURULMA_TARIHI]) DESC;",
            "active-hotels" => @"SELECT TOP (20)
                                        o.[OTEL_ADI],
                                        CONCAT(o.[ILCE], ', ', o.[SEHIR]),
                                        FORMAT(COALESCE(o.[ORTALAMA_PUAN], 0), '0.0', 'tr-TR'),
                                        (SELECT COUNT(*) FROM [dbo].[REZERVASYONLAR] r WHERE r.[OTEL_ID] = o.id),
                                        FORMAT(COALESCE((SELECT SUM(COALESCE(r.[TOPLAM_TUTAR],0)) FROM [dbo].[REZERVASYONLAR] r WHERE r.[OTEL_ID] = o.id AND COALESCE(r.[DURUM],'') <> 'İptal Edildi'),0), 'N0', 'tr-TR'),
                                        FORMAT(COALESCE(o.[GUNCELLENME_TARIHI], o.[OLUSTURULMA_TARIHI]), 'dd.MM.yyyy HH:mm', 'tr-TR')
                                     FROM [dbo].[OTELLER] o
                                     WHERE o.[YAYIN_DURUMU] = 'Yayında' AND o.[ONAY_DURUMU] = 'Onaylandı'
                                     ORDER BY COALESCE(o.[ORTALAMA_PUAN], 0) DESC, o.id DESC;",
            "pending-hotels" => @"SELECT TOP (20)
                                         o.[OTEL_ADI],
                                         CONCAT(o.[ILCE], ', ', o.[SEHIR]),
                                         COALESCE(o.[ONAY_DURUMU], '-'),
                                         COALESCE(o.[YAYIN_DURUMU], '-'),
                                         FORMAT(o.[OLUSTURULMA_TARIHI], 'dd.MM.yyyy', 'tr-TR'),
                                         FORMAT(COALESCE(o.[GUNCELLENME_TARIHI], o.[OLUSTURULMA_TARIHI]), 'dd.MM.yyyy HH:mm', 'tr-TR')
                                  FROM [dbo].[OTELLER] o
                                  WHERE COALESCE(o.[ONAY_DURUMU], '') = 'Beklemede'
                                     OR COALESCE(o.[YAYIN_DURUMU], '') <> 'Yayında'
                                  ORDER BY o.[OLUSTURULMA_TARIHI] DESC;",
            "reviews" => @"SELECT TOP (12) COALESCE([YORUM_BASLIGI], 'Basliksiz'), [GENEL_PUAN], [ONAY_DURUMU], [RAPOR_SAYISI], [DOGRULANMIS_KONAKLAMA], FORMAT([OLUSTURULMA_TARIHI], 'dd.MM.yyyy', 'tr-TR') FROM [dbo].[YORUMLAR] ORDER BY id DESC;",
            "campaigns" => @"SELECT TOP (12) [KAMPANYA_ADI], tur, FORMAT([BASLANGIC_TARIHI], 'dd.MM.yyyy', 'tr-TR'), FORMAT([BITIS_TARIHI], 'dd.MM.yyyy', 'tr-TR'), [AKTIF_MI], [KULLANILAN_ADET] FROM [dbo].[KAMPANYALAR] ORDER BY id DESC;",
            "notifications" => @"SELECT TOP (12) [BASLIK], [BILDIRIM_TURU], [ONEM_DERECESI], [OKUNDU_MU], [ARSIVLENDI_MI], FORMAT([OLUSTURULMA_TARIHI], 'dd.MM.yyyy HH:mm', 'tr-TR') FROM [dbo].[SISTEM_ICI_BILDIRIMLER] ORDER BY id DESC;",
            "logs" => @"SELECT TOP (6) [HEDEF_TABLO], [ISLEM_TURU], [IP_ADRESI], FORMAT([ISLEM_TARIHI], 'dd.MM.yyyy HH:mm', 'tr-TR'), 'Admin Islem', '' FROM [dbo].[ADMIN_ISLEM_LOGLARI] ORDER BY id DESC;",
            "geo-search-logs" => @"SELECT TOP (80)
                                        FORMAT([KAYIT_TARIHI], 'dd.MM.yyyy HH:mm', 'tr-TR') AS [TARIH],
                                        COALESCE([KAYNAK],'-') AS [KAYNAK],
                                        COALESCE([ARAMA_METNI],'-') AS [ARAMA_METNI],
                                        COALESCE([ARAMA_BOLGESI],'-') AS [ARAMA_BOLGESI],
                                        FORMAT([ENLEM], '0.0000000', 'en-US') AS [ENLEM],
                                        FORMAT([BOYLAM], '0.0000000', 'en-US') AS [BOYLAM],
                                        COALESCE(CAST([YARICAP_KM] AS nvarchar(20)),'-') AS [YARICAP_KM],
                                        COALESCE(CAST([GORUNEN_OTEL_SAYISI] AS nvarchar(20)),'-') AS [GORUNEN_OTEL_SAYISI],
                                        COALESCE([IP_ADRESI],'-') AS ip,
                                        COALESCE([CIHAZ_TIPI],'-') AS cihaz
                                     FROM [dbo].[KULLANICI_KONUM_LOGLARI]
                                     ORDER BY [KAYIT_TARIHI] DESC;",
            "hotel-coordinate-changes" => @"SELECT TOP (120)
                                                FORMAT([KAYIT_TARIHI], 'dd.MM.yyyy HH:mm', 'tr-TR'),
                                                COALESCE([ADMIN_AD_SOYAD], CONCAT('Admin#', [ADMIN_KULLANICI_ID])),
                                                COALESCE([OTEL_ADI], CONCAT('Otel#', [OTEL_ID])),
                                                CONCAT(FORMAT([ONCEKI_ENLEM], '0.0000000', 'en-US'), ', ', FORMAT([ONCEKI_BOYLAM], '0.0000000', 'en-US')),
                                                CONCAT(FORMAT([YENI_ENLEM], '0.0000000', 'en-US'), ', ', FORMAT([YENI_BOYLAM], '0.0000000', 'en-US')),
                                                COALESCE([IP_ADRESI], '-'),
                                                COALESCE([NOTLAR], '-')
                                           FROM [dbo].[OTEL_KOORDINAT_DEGISIM_LOGLARI]
                                           ORDER BY [KAYIT_TARIHI] DESC;",
            "email-templates" => @"SELECT TOP (12) [SABLON_ADI], [KATEGORI], dil, [AKTIF_MI], [SISTEM_GENELI_MI], [KONU_BASLIGI] FROM [dbo].[MESAJ_SABLONLARI] ORDER BY id DESC;",
            "faq" => @"SELECT TOP (20) k.[KATEGORI_ADI], s.[SORU], s.[ONE_CIKAN_MI], s.[AKTIF_MI], FORMAT(s.[OLUSTURULMA_TARIHI], 'dd.MM.yyyy', 'tr-TR') FROM [dbo].[SSS_SORULARI] s INNER JOIN [dbo].[SSS_KATEGORILERI] k ON k.id = s.[SSS_KATEGORI_ID] ORDER BY k.[SIRALAMA], s.[SIRALAMA], s.id;",
            "countries" => @"SELECT TOP (80) [ULKE_ADI], COALESCE([ISO2_KODU],'-'), COALESCE([ISO3_KODU],'-'), COALESCE([PARA_BIRIMI_KODU],'-'), CASE WHEN [VARSAYILAN_ULKE]=1 THEN N'Evet' ELSE N'Hayır' END, CASE WHEN [AKTIF_MI]=1 THEN N'Aktif' ELSE N'Pasif' END FROM [dbo].[ULKELER] ORDER BY [VARSAYILAN_ULKE] DESC, [ULKE_ADI];",
            "roles" => @"SELECT TOP (80) [ROL_KODU], [ROL_ADI], COALESCE([DEPARTMAN],'-'), CAST(COALESCE([SEVIYE],0) AS nvarchar(10)), CASE WHEN [VARSAYILAN_MI]=1 THEN N'Evet' ELSE N'Hayır' END, COALESCE([ACIKLAMA],'-') FROM [dbo].[ROLLER] ORDER BY [SEVIYE], [ROL_ADI];",
            "admin-rbac-roles" => @"SELECT TOP (40) [ROL_CODE], [ROL_NAME], COALESCE([DESCRIPTION],'-'), CASE WHEN [ACTIVE]=1 THEN N'Aktif' ELSE N'Pasif' END FROM [dbo].[ADMIN_ROLLER] ORDER BY [ROL_CODE];",
            "companies" => @"SELECT TOP (80) f.[FIRMA_ADI], COALESCE(f.[ONAY_DURUMU], 'Beklemede'),
                                    (SELECT COUNT(*) FROM [dbo].[KULLANICILAR] u WHERE u.[FIRMA_ID] = f.id AND u.[ROL] LIKE 'firma_%'),
                                    (SELECT COUNT(*) FROM [dbo].[REZERVASYONLAR] r WHERE r.[FIRMA_ID] = f.id),
                                    FORMAT(f.[OLUSTURULMA_TARIHI], 'dd.MM.yyyy', 'tr-TR')
                             FROM [dbo].[FIRMALAR] f ORDER BY f.id DESC;",
            "platform-db-stats" => @"SELECT TOP (60) t.[name], CAST(SUM(p.[rows]) AS nvarchar(30)), SCHEMA_NAME(t.[schema_id])
                                     FROM sys.tables t
                                     INNER JOIN sys.partitions p ON t.[object_id] = p.[object_id]
                                     WHERE t.is_ms_shipped = 0 AND p.index_id IN (0, 1)
                                     GROUP BY t.[name], t.[schema_id]
                                     ORDER BY SUM(p.[rows]) DESC;",
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
            const string getSql = @"SELECT TOP (1) [OTEL_ID] FROM [dbo].[YORUMLAR] WHERE id = @id;";
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
UPDATE [dbo].[YORUMLAR]
SET [ONAY_DURUMU] = @status,
    [ONAYLAYAN_ADMIN_ID] = @adminId,
    [ONAY_TARIHI] = CASE WHEN @status LIKE N'Onaylan%' THEN SYSUTCDATETIME() ELSE [ONAY_TARIHI] END,
    [RED_NEDENI] = CASE WHEN @status <> N'Onaylandı' THEN @note ELSE NULL END,
    [GUNCELLENME_TARIHI] = SYSUTCDATETIME()
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
            const string getSql = @"SELECT TOP (1) [OTEL_ID] FROM [dbo].[YORUMLAR] WHERE id = @id;";
            long hotelId;
            await using (var getCmd = new SqlCommand(getSql, connection, (SqlTransaction)tx))
            {
                getCmd.Parameters.AddWithValue("@id", form.ReviewId);
                var obj = await getCmd.ExecuteScalarAsync(cancellationToken);
                hotelId = obj is null || obj is DBNull ? 0 : Convert.ToInt64(obj, CultureInfo.InvariantCulture);
            }

            const string delSql = @"DELETE FROM [dbo].[YORUMLAR] WHERE id = @id;";
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
INSERT INTO [dbo].[SISTEM_ICI_BILDIRIMLER](
    [KULLANICI_ID], [BILDIRIM_TURU], [BASLIK], [MESAJ], ikon, renk,
    [AKSIYON_URL], [AKSIYON_METNI], [ONEM_DERECESI], [ILGILI_TABLO], [ILGILI_KAYIT_ID]
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
IF EXISTS (SELECT 1 FROM [dbo].[BLOCKYORUMKELIME] WHERE [KELIME] = @w)
BEGIN
    UPDATE [dbo].[BLOCKYORUMKELIME]
    SET [AKTIF_MI] = 1,
        [ACIKLAMA] = COALESCE(NULLIF(@d,''), [ACIKLAMA]),
        [EKLEYEN_ADMIN_ID] = COALESCE(@adminId, [EKLEYEN_ADMIN_ID]),
        [GUNCELLENME_TARIHI] = SYSUTCDATETIME()
    WHERE [KELIME] = @w;
END
ELSE
BEGIN
    INSERT INTO [dbo].[BLOCKYORUMKELIME]([KELIME], [AKTIF_MI], [ACIKLAMA], [EKLEYEN_ADMIN_ID])
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
UPDATE [dbo].[BLOCKYORUMKELIME]
SET [AKTIF_MI] = @active,
    [EKLEYEN_ADMIN_ID] = COALESCE([EKLEYEN_ADMIN_ID], @adminId),
    [GUNCELLENME_TARIHI] = SYSUTCDATETIME()
WHERE [ID] = @id;";
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
        y.[OTEL_ID],
        COUNT(*) AS cnt,
        CAST(ROUND(AVG(CAST(COALESCE(CAST(y.[GENEL_PUAN_10] AS DECIMAL(9, 4)),
            CASE
                WHEN y.[GENEL_PUAN] <= 5 THEN CAST(y.[GENEL_PUAN] AS DECIMAL(9, 4)) * 2
                WHEN y.[GENEL_PUAN] <= 10 THEN CAST(y.[GENEL_PUAN] AS DECIMAL(9, 4))
                ELSE 10
            END) AS DECIMAL(9, 4))), 2) AS DECIMAL(5, 2)) AS avg_genel
    FROM [dbo].[YORUMLAR] AS y
    WHERE y.[OTEL_ID] = @hotelId
      AND y.[ONAY_DURUMU] LIKE N'Onaylan%'
    GROUP BY y.[OTEL_ID]
)
UPDATE o
SET
    o.[TOPLAM_YORUM_SAYISI] = agg.cnt,
    o.[ORTALAMA_PUAN] = agg.avg_genel
FROM [dbo].[OTELLER] AS o
INNER JOIN agg ON agg.[OTEL_ID] = o.id;";
        await using var cmd = new SqlCommand(sql, connection, transaction);
        cmd.Parameters.AddWithValue("@hotelId", hotelId);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<List<AdminBlockedWordRowViewModel>> LoadBlockedWordsAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        var items = new List<AdminBlockedWordRowViewModel>();
        if (!await TableExistsAsync(connection, "BLOCKYORUMKELIME", cancellationToken))
        {
            return items;
        }

        const string sql = @"
SELECT TOP (200) [ID], [KELIME], [AKTIF_MI], [ACIKLAMA], [OLUSTURULMA_TARIHI]
FROM [dbo].[BLOCKYORUMKELIME]
ORDER BY [AKTIF_MI] DESC, [ID] DESC;";
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
        if (!await TableExistsAsync(connection, "YORUM_KALDIRMA_TALEPLERI", cancellationToken))
        {
            return items;
        }

        const string sql = @"
SELECT TOP (40)
    t.[ID],
    t.[YORUM_ID],
    COALESCE(t.[OTEL_ID], 0) AS [OTEL_ID],
    COALESCE(o.[OTEL_ADI], '') AS [OTEL_ADI],
    t.[PARTNER_KULLANICI_ID],
    COALESCE(pu.[EPOSTA], '') AS partner_eposta,
    COALESCE(t.[DURUM], 'Beklemede') AS [DURUM],
    COALESCE(t.[SEBEP], '') AS sebep,
    t.[OLUSTURULMA_TARIHI]
FROM [dbo].[YORUM_KALDIRMA_TALEPLERI] t
LEFT JOIN [dbo].[OTELLER] o ON o.[ID] = t.[OTEL_ID]
LEFT JOIN [dbo].[KULLANICILAR] pu ON pu.[ID] = t.[PARTNER_KULLANICI_ID]
ORDER BY CASE WHEN COALESCE(t.[DURUM],'') = 'Beklemede' THEN 0 ELSE 1 END, t.[OLUSTURULMA_TARIHI] DESC;";
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
    y.[OTEL_ID],
    COALESCE(o.[OTEL_ADI],'') AS [OTEL_ADI],
    COALESCE(o.[SEHIR],'') AS [SEHIR],
    COALESCE(o.[ILCE],'') AS ilce,
    y.[KULLANICI_ID],
    COALESCE(u.[AD_SOYAD], u.[EPOSTA], 'Kullanıcı') AS kullanici,
    COALESCE(y.[GENEL_PUAN], 0) AS puan,
    COALESCE(y.[ONAY_DURUMU], 'Beklemede') AS [DURUM],
    COALESCE(y.[RAPOR_SAYISI], 0) AS rapor,
    y.[OLUSTURULMA_TARIHI],
    COALESCE(y.[YORUM_METNI], '') AS yorum
FROM [dbo].[YORUMLAR] y
LEFT JOIN [dbo].[OTELLER] o ON o.id = y.[OTEL_ID]
LEFT JOIN [dbo].[KULLANICILAR] u ON u.id = y.[KULLANICI_ID]
WHERE (@q IS NULL OR (COALESCE(y.[YORUM_METNI],'') LIKE N'%' + @q + N'%' OR COALESCE(o.[OTEL_ADI],'') LIKE N'%' + @q + N'%' OR COALESCE(u.[AD_SOYAD],'') LIKE N'%' + @q + N'%' OR COALESCE(u.[EPOSTA],'') LIKE N'%' + @q + N'%'))
  AND (@city IS NULL OR COALESCE(o.[SEHIR],'') LIKE N'%' + @city + N'%')
  AND (@hotel IS NULL OR COALESCE(o.[OTEL_ADI],'') LIKE N'%' + @hotel + N'%')
ORDER BY y.[OLUSTURULMA_TARIHI] DESC, y.id DESC;";

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

        var sql = $"SELECT COUNT(*) FROM [dbo].[{tableName}] WHERE {whereClause};";
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
            "admin_routing_notice" => "Admin yönlendirme",
            "admin_partner_basvuru" => "Partner başvurusu",
            "admin_firma_basvuru" => "Firma başvurusu",
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

    public async Task<AdminCommissionCollectionPageViewModel> GetCommissionCollectionLedgerAsync(string fullName, string email, string userRole, AdminCommissionCollectionFilter filter, CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeCommissionCollectionFilter(filter);
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var model = new AdminCommissionCollectionPageViewModel
        {
            Shell = await GetShellAsync(connection, "Komisyon Tahsilat Merkezi", "Otel ve donem bazinda platform komisyon tahsilatini filtreleyin, sirlayin ve toplu isaretleyin.", fullName, email, userRole, cancellationToken),
            Filter = normalized
        };

        var (whereSql, parameters) = BuildCommissionCollectionWhere(normalized);
        var orderSql = BuildCommissionCollectionOrder(normalized.SortBy, normalized.SortDir);
        var offset = (normalized.Page - 1) * normalized.PageSize;

        var countSql = $@"
            SELECT COUNT(*) FROM (
                SELECT
                    CASE
                        WHEN SUM(CASE WHEN COALESCE(k.[PLATFORM_TAHSILAT_DURUMU], N'') = N'Itiraz' THEN 1 ELSE 0 END) > 0 THEN N'Itiraz'
                        WHEN COALESCE(SUM(k.[KOMISYON_TUTARI]), 0) > 0
                             AND COALESCE(SUM(CASE WHEN COALESCE(k.[PLATFORM_TAHSILAT_DURUMU], N'Bekliyor') = N'TahsilEdildi' THEN k.[KOMISYON_TUTARI] ELSE 0 END), 0) >= COALESCE(SUM(k.[KOMISYON_TUTARI]), 0)
                            THEN N'TahsilEdildi'
                        WHEN COALESCE(SUM(CASE WHEN COALESCE(k.[PLATFORM_TAHSILAT_DURUMU], N'Bekliyor') = N'TahsilEdildi' THEN k.[KOMISYON_TUTARI] ELSE 0 END), 0) > 0 THEN N'Kismi'
                        ELSE N'Bekliyor'
                    END AS tahsilat_ozet
                FROM [dbo].[KOMISYON_MUHASEBE_KAYITLARI] k
                INNER JOIN [dbo].[OTELLER] o ON o.[ID] = k.[OTEL_ID]
                {whereSql}
                GROUP BY o.[ID], k.[DONEM]
            ) agg
            WHERE (@tahsilatStatus = N'' OR agg.[tahsilat_ozet] = @tahsilatStatus);";

        await using (var countCmd = new SqlCommand(countSql, connection))
        {
            BindCommissionCollectionParameters(countCmd, parameters);
            model.Total = Convert.ToInt32(await countCmd.ExecuteScalarAsync(cancellationToken) ?? 0, CultureInfo.InvariantCulture);
        }

        var listSql = $@"
            SELECT
                agg.[hotel_id],
                agg.[otel_kodu],
                agg.[otel_adi],
                agg.[sehir],
                agg.[ilce],
                agg.[mahalle],
                agg.[ilce_id],
                agg.[partner_id],
                agg.[donem],
                agg.[rez_adet],
                agg.[brut_komisyon],
                agg.[tahsil_edilen],
                agg.[bekleyen],
                agg.[tahsilat_ozet],
                agg.[odeme_ozet]
            FROM (
                SELECT
                    o.[ID] AS hotel_id,
                    COALESCE(o.[OTEL_KODU], N'') AS otel_kodu,
                    COALESCE(o.[OTEL_ADI], N'') AS otel_adi,
                    COALESCE(o.[SEHIR], N'') AS sehir,
                    COALESCE(o.[ILCE], N'') AS ilce,
                    COALESCE(o.[MAHALLE], N'') AS mahalle,
                    o.[ILCE_ID] AS ilce_id,
                    k.[PARTNER_ID] AS partner_id,
                    k.[DONEM] AS donem,
                    COUNT(*) AS rez_adet,
                    COALESCE(SUM(k.[KOMISYON_TUTARI]), 0) AS brut_komisyon,
                    COALESCE(SUM(CASE WHEN COALESCE(k.[PLATFORM_TAHSILAT_DURUMU], N'Bekliyor') = N'TahsilEdildi' THEN k.[KOMISYON_TUTARI] ELSE 0 END), 0) AS tahsil_edilen,
                    COALESCE(SUM(CASE WHEN COALESCE(k.[PLATFORM_TAHSILAT_DURUMU], N'Bekliyor') <> N'TahsilEdildi' THEN k.[KOMISYON_TUTARI] ELSE 0 END), 0) AS bekleyen,
                    CASE
                        WHEN SUM(CASE WHEN COALESCE(k.[PLATFORM_TAHSILAT_DURUMU], N'') = N'Itiraz' THEN 1 ELSE 0 END) > 0 THEN N'Itiraz'
                        WHEN COALESCE(SUM(k.[KOMISYON_TUTARI]), 0) > 0
                             AND COALESCE(SUM(CASE WHEN COALESCE(k.[PLATFORM_TAHSILAT_DURUMU], N'Bekliyor') = N'TahsilEdildi' THEN k.[KOMISYON_TUTARI] ELSE 0 END), 0) >= COALESCE(SUM(k.[KOMISYON_TUTARI]), 0)
                            THEN N'TahsilEdildi'
                        WHEN COALESCE(SUM(CASE WHEN COALESCE(k.[PLATFORM_TAHSILAT_DURUMU], N'Bekliyor') = N'TahsilEdildi' THEN k.[KOMISYON_TUTARI] ELSE 0 END), 0) > 0 THEN N'Kismi'
                        ELSE N'Bekliyor'
                    END AS tahsilat_ozet,
                    CASE
                        WHEN SUM(CASE WHEN COALESCE(k.[OTELE_ODEME_DURUMU], N'') = N'Ödendi' THEN 1 ELSE 0 END) = COUNT(*) THEN N'Ödendi'
                        WHEN SUM(CASE WHEN COALESCE(k.[OTELE_ODEME_DURUMU], N'') = N'Ödendi' THEN 1 ELSE 0 END) = 0 THEN N'Ödenmedi'
                        ELSE N'Kısmi'
                    END AS odeme_ozet
                FROM [dbo].[KOMISYON_MUHASEBE_KAYITLARI] k
                INNER JOIN [dbo].[OTELLER] o ON o.[ID] = k.[OTEL_ID]
                {whereSql}
                GROUP BY o.[ID], o.[OTEL_KODU], o.[OTEL_ADI], o.[SEHIR], o.[ILCE], o.[MAHALLE], o.[ILCE_ID], k.[DONEM], k.[PARTNER_ID]
            ) agg
            WHERE (@tahsilatStatus = N'' OR agg.[tahsilat_ozet] = @tahsilatStatus)
            ORDER BY {orderSql}
            OFFSET @offset ROWS FETCH NEXT @take ROWS ONLY;";

        await using (var listCmd = new SqlCommand(listSql, connection))
        {
            BindCommissionCollectionParameters(listCmd, parameters);
            listCmd.Parameters.AddWithValue("@offset", offset);
            listCmd.Parameters.AddWithValue("@take", normalized.PageSize);
            await using var reader = await listCmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                model.Rows.Add(new AdminCommissionCollectionRowViewModel
                {
                    HotelId = reader.GetInt64(0),
                    HotelCode = reader.GetString(1),
                    HotelName = reader.GetString(2),
                    City = reader.GetString(3),
                    District = reader.GetString(4),
                    Neighborhood = reader.GetString(5),
                    IlceId = reader.IsDBNull(6) ? null : reader.GetInt64(6),
                    PartnerId = reader.GetInt64(7),
                    Donem = reader.GetString(8),
                    ReservationCount = SafeInt(reader, 9),
                    GrossCommission = SafeDecimal(reader, 10),
                    CollectedAmount = SafeDecimal(reader, 11),
                    PendingAmount = SafeDecimal(reader, 12),
                    TahsilatStatus = reader.GetString(13),
                    PaymentStatusSummary = reader.GetString(14)
                });
            }
        }

        var totalsSql = $@"
            SELECT
                COALESCE(SUM(agg.[rez_adet]), 0),
                COALESCE(SUM(agg.[brut_komisyon]), 0),
                COALESCE(SUM(agg.[tahsil_edilen]), 0),
                COALESCE(SUM(agg.[bekleyen]), 0)
            FROM (
                SELECT
                    COUNT(*) AS rez_adet,
                    COALESCE(SUM(k.[KOMISYON_TUTARI]), 0) AS brut_komisyon,
                    COALESCE(SUM(CASE WHEN COALESCE(k.[PLATFORM_TAHSILAT_DURUMU], N'Bekliyor') = N'TahsilEdildi' THEN k.[KOMISYON_TUTARI] ELSE 0 END), 0) AS tahsil_edilen,
                    COALESCE(SUM(CASE WHEN COALESCE(k.[PLATFORM_TAHSILAT_DURUMU], N'Bekliyor') <> N'TahsilEdildi' THEN k.[KOMISYON_TUTARI] ELSE 0 END), 0) AS bekleyen,
                    CASE
                        WHEN SUM(CASE WHEN COALESCE(k.[PLATFORM_TAHSILAT_DURUMU], N'') = N'Itiraz' THEN 1 ELSE 0 END) > 0 THEN N'Itiraz'
                        WHEN COALESCE(SUM(k.[KOMISYON_TUTARI]), 0) > 0
                             AND COALESCE(SUM(CASE WHEN COALESCE(k.[PLATFORM_TAHSILAT_DURUMU], N'Bekliyor') = N'TahsilEdildi' THEN k.[KOMISYON_TUTARI] ELSE 0 END), 0) >= COALESCE(SUM(k.[KOMISYON_TUTARI]), 0)
                            THEN N'TahsilEdildi'
                        WHEN COALESCE(SUM(CASE WHEN COALESCE(k.[PLATFORM_TAHSILAT_DURUMU], N'Bekliyor') = N'TahsilEdildi' THEN k.[KOMISYON_TUTARI] ELSE 0 END), 0) > 0 THEN N'Kismi'
                        ELSE N'Bekliyor'
                    END AS tahsilat_ozet
                FROM [dbo].[KOMISYON_MUHASEBE_KAYITLARI] k
                INNER JOIN [dbo].[OTELLER] o ON o.[ID] = k.[OTEL_ID]
                {whereSql}
                GROUP BY o.[ID], k.[DONEM]
            ) agg
            WHERE (@tahsilatStatus = N'' OR agg.[tahsilat_ozet] = @tahsilatStatus);";

        await using (var totalsCmd = new SqlCommand(totalsSql, connection))
        {
            BindCommissionCollectionParameters(totalsCmd, parameters);
            await using var reader = await totalsCmd.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                model.Totals = new AdminCommissionCollectionTotalsViewModel
                {
                    ReservationTotal = SafeInt(reader, 0),
                    TotalCommission = SafeDecimal(reader, 1),
                    CollectedTotal = SafeDecimal(reader, 2),
                    PendingTotal = SafeDecimal(reader, 3)
                };
            }
        }

        return model;
    }

    public async Task<string> ExportCommissionCollectionCsvAsync(AdminCommissionCollectionFilter filter, CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeCommissionCollectionFilter(filter);
        normalized.Page = 1;
        normalized.PageSize = 10000;

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var (whereSql, parameters) = BuildCommissionCollectionWhere(normalized);
        var orderSql = BuildCommissionCollectionOrder(normalized.SortBy, normalized.SortDir);

        var sql = $@"
            SELECT
                agg.[donem],
                agg.[otel_kodu],
                agg.[otel_adi],
                agg.[sehir],
                agg.[ilce],
                agg.[mahalle],
                agg.[rez_adet],
                agg.[brut_komisyon],
                agg.[tahsil_edilen],
                agg.[bekleyen],
                agg.[tahsilat_ozet],
                agg.[odeme_ozet],
                agg.[partner_id]
            FROM (
                SELECT
                    k.[DONEM] AS donem,
                    COALESCE(o.[OTEL_KODU], N'') AS otel_kodu,
                    COALESCE(o.[OTEL_ADI], N'') AS otel_adi,
                    COALESCE(o.[SEHIR], N'') AS sehir,
                    COALESCE(o.[ILCE], N'') AS ilce,
                    COALESCE(o.[MAHALLE], N'') AS mahalle,
                    k.[PARTNER_ID] AS partner_id,
                    COUNT(*) AS rez_adet,
                    COALESCE(SUM(k.[KOMISYON_TUTARI]), 0) AS brut_komisyon,
                    COALESCE(SUM(CASE WHEN COALESCE(k.[PLATFORM_TAHSILAT_DURUMU], N'Bekliyor') = N'TahsilEdildi' THEN k.[KOMISYON_TUTARI] ELSE 0 END), 0) AS tahsil_edilen,
                    COALESCE(SUM(CASE WHEN COALESCE(k.[PLATFORM_TAHSILAT_DURUMU], N'Bekliyor') <> N'TahsilEdildi' THEN k.[KOMISYON_TUTARI] ELSE 0 END), 0) AS bekleyen,
                    CASE
                        WHEN SUM(CASE WHEN COALESCE(k.[PLATFORM_TAHSILAT_DURUMU], N'') = N'Itiraz' THEN 1 ELSE 0 END) > 0 THEN N'Itiraz'
                        WHEN COALESCE(SUM(k.[KOMISYON_TUTARI]), 0) > 0
                             AND COALESCE(SUM(CASE WHEN COALESCE(k.[PLATFORM_TAHSILAT_DURUMU], N'Bekliyor') = N'TahsilEdildi' THEN k.[KOMISYON_TUTARI] ELSE 0 END), 0) >= COALESCE(SUM(k.[KOMISYON_TUTARI]), 0)
                            THEN N'TahsilEdildi'
                        WHEN COALESCE(SUM(CASE WHEN COALESCE(k.[PLATFORM_TAHSILAT_DURUMU], N'Bekliyor') = N'TahsilEdildi' THEN k.[KOMISYON_TUTARI] ELSE 0 END), 0) > 0 THEN N'Kismi'
                        ELSE N'Bekliyor'
                    END AS tahsilat_ozet,
                    CASE
                        WHEN SUM(CASE WHEN COALESCE(k.[OTELE_ODEME_DURUMU], N'') = N'Ödendi' THEN 1 ELSE 0 END) = COUNT(*) THEN N'Ödendi'
                        WHEN SUM(CASE WHEN COALESCE(k.[OTELE_ODEME_DURUMU], N'') = N'Ödendi' THEN 1 ELSE 0 END) = 0 THEN N'Ödenmedi'
                        ELSE N'Kısmi'
                    END AS odeme_ozet
                FROM [dbo].[KOMISYON_MUHASEBE_KAYITLARI] k
                INNER JOIN [dbo].[OTELLER] o ON o.[ID] = k.[OTEL_ID]
                {whereSql}
                GROUP BY o.[ID], o.[OTEL_KODU], o.[OTEL_ADI], o.[SEHIR], o.[ILCE], o.[MAHALLE], k.[DONEM], k.[PARTNER_ID]
            ) agg
            WHERE (@tahsilatStatus = N'' OR agg.[tahsilat_ozet] = @tahsilatStatus)
            ORDER BY {orderSql};";

        var sb = new StringBuilder();
        var inv = CultureInfo.InvariantCulture;
        sb.AppendLine("donem,otel_kodu,otel_adi,sehir,ilce,mahalle,rezervasyon,brut_komisyon,tahsil_edilen,bekleyen,tahsilat_durumu,otele_odeme,partner_id");

        await using var cmd = new SqlCommand(sql, connection);
        BindCommissionCollectionParameters(cmd, parameters);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            sb.Append(Csv(reader.GetString(0))).Append(',')
              .Append(Csv(reader.GetString(1))).Append(',')
              .Append(Csv(reader.GetString(2))).Append(',')
              .Append(Csv(reader.GetString(3))).Append(',')
              .Append(Csv(reader.GetString(4))).Append(',')
              .Append(Csv(reader.GetString(5))).Append(',')
              .Append(SafeInt(reader, 6).ToString(inv)).Append(',')
              .Append(SafeDecimal(reader, 7).ToString("0.##", inv)).Append(',')
              .Append(SafeDecimal(reader, 8).ToString("0.##", inv)).Append(',')
              .Append(SafeDecimal(reader, 9).ToString("0.##", inv)).Append(',')
              .Append(Csv(reader.GetString(10))).Append(',')
              .Append(Csv(reader.GetString(11))).Append(',')
              .Append(reader.GetInt64(12).ToString(inv))
              .AppendLine();
        }

        return sb.ToString();
    }

    public async Task<(bool Success, string Message, int UpdatedCount)> MarkCommissionCollectionPaidAsync(long adminUserId, AdminCommissionCollectionMarkPaidForm request, CancellationToken cancellationToken = default)
    {
        if (request.HotelIds.Count == 0 || request.Donems.Count == 0 || request.HotelIds.Count != request.Donems.Count)
        {
            return (false, "Tahsilat isareti icin en az bir otel-donem secilmelidir.", 0);
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        var updated = 0;
        var reference = string.IsNullOrWhiteSpace(request.Reference) ? $"ADMIN-{DateTime.UtcNow:yyyyMMddHHmmss}" : request.Reference.Trim();

        const string sql = @"
            UPDATE [dbo].[KOMISYON_MUHASEBE_KAYITLARI]
            SET [PLATFORM_TAHSILAT_DURUMU] = N'TahsilEdildi',
                [PLATFORM_TAHSILAT_TARIHI] = CAST(GETDATE() AS date),
                [PLATFORM_TAHSILAT_REFERANSI] = @reference,
                [PLATFORM_TAHSILAT_NOTU] = @note,
                [GUNCELLENME_TARIHI] = SYSUTCDATETIME()
            WHERE [OTEL_ID] = @hotelId
              AND [DONEM] = @donem
              AND COALESCE([PLATFORM_TAHSILAT_DURUMU], N'Bekliyor') <> N'TahsilEdildi';";

        for (var i = 0; i < request.HotelIds.Count; i++)
        {
            var hotelId = request.HotelIds[i];
            var donem = request.Donems[i]?.Trim() ?? string.Empty;
            if (hotelId <= 0 || string.IsNullOrWhiteSpace(donem))
            {
                continue;
            }

            await using var cmd = new SqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@hotelId", hotelId);
            cmd.Parameters.AddWithValue("@donem", donem);
            cmd.Parameters.AddWithValue("@reference", reference);
            cmd.Parameters.AddWithValue("@note", (object?)request.Note ?? DBNull.Value);
            updated += await cmd.ExecuteNonQueryAsync(cancellationToken);
        }

        if (updated <= 0)
        {
            return (false, "Guncellenecek kayit bulunamadi veya zaten tahsil edilmis.", 0);
        }

        await TryLogAdminActionAsync(connection, adminUserId, "commission_collection_mark_paid", "komisyon_muhasebe_kayitlari", string.Join(';', request.HotelIds.Zip(request.Donems, (h, d) => $"{h}:{d}")), $"Tahsil edildi ({updated} kayit). Ref: {reference}", cancellationToken);
        return (true, $"{updated} komisyon kaydi tahsil edildi olarak isaretlendi.", updated);
    }

    private static AdminCommissionCollectionFilter NormalizeCommissionCollectionFilter(AdminCommissionCollectionFilter filter)
    {
        filter ??= new AdminCommissionCollectionFilter();
        filter.Donem = filter.Donem?.Trim() ?? string.Empty;
        filter.City = filter.City?.Trim() ?? string.Empty;
        filter.District = filter.District?.Trim() ?? string.Empty;
        filter.Neighborhood = filter.Neighborhood?.Trim() ?? string.Empty;
        filter.TahsilatStatus = filter.TahsilatStatus?.Trim() ?? string.Empty;
        filter.PaymentStatus = filter.PaymentStatus?.Trim() ?? string.Empty;
        filter.SortBy = string.IsNullOrWhiteSpace(filter.SortBy) ? "commission" : filter.SortBy.Trim().ToLowerInvariant();
        filter.SortDir = string.Equals(filter.SortDir, "asc", StringComparison.OrdinalIgnoreCase) ? "asc" : "desc";
        filter.Page = Math.Max(1, filter.Page <= 0 ? 1 : filter.Page);
        filter.PageSize = Math.Clamp(filter.PageSize <= 0 ? 50 : filter.PageSize, 50, 500);
        return filter;
    }

    private static (string WhereSql, Dictionary<string, object?> Parameters) BuildCommissionCollectionWhere(AdminCommissionCollectionFilter filter)
    {
        var clauses = new List<string> { "WHERE 1=1" };
        var parameters = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["@donem"] = filter.Donem,
            ["@city"] = filter.City,
            ["@district"] = filter.District,
            ["@neighborhood"] = filter.Neighborhood,
            ["@tahsilatStatus"] = filter.TahsilatStatus,
            ["@paymentStatus"] = filter.PaymentStatus
        };

        if (!string.IsNullOrWhiteSpace(filter.Donem))
        {
            clauses.Add("AND k.[DONEM] = @donem");
        }

        if (!string.IsNullOrWhiteSpace(filter.City))
        {
            clauses.Add("AND COALESCE(o.[SEHIR], N'') LIKE N'%' + @city + N'%'");
        }

        if (!string.IsNullOrWhiteSpace(filter.District))
        {
            clauses.Add("AND COALESCE(o.[ILCE], N'') LIKE N'%' + @district + N'%'");
        }

        if (!string.IsNullOrWhiteSpace(filter.Neighborhood))
        {
            clauses.Add("AND COALESCE(o.[MAHALLE], N'') LIKE N'%' + @neighborhood + N'%'");
        }

        if (filter.IlceId.HasValue)
        {
            clauses.Add("AND o.[ILCE_ID] = @ilceId");
            parameters["@ilceId"] = filter.IlceId.Value;
        }

        if (filter.HotelId.HasValue)
        {
            clauses.Add("AND o.[ID] = @hotelId");
            parameters["@hotelId"] = filter.HotelId.Value;
        }

        if (filter.PartnerId.HasValue)
        {
            clauses.Add("AND k.[PARTNER_ID] = @partnerId");
            parameters["@partnerId"] = filter.PartnerId.Value;
        }

        if (string.Equals(filter.PaymentStatus, "Ödendi", StringComparison.OrdinalIgnoreCase))
        {
            clauses.Add("AND COALESCE(k.[OTELE_ODEME_DURUMU], N'') = N'Ödendi'");
        }
        else if (string.Equals(filter.PaymentStatus, "Ödenmedi", StringComparison.OrdinalIgnoreCase))
        {
            clauses.Add("AND COALESCE(k.[OTELE_ODEME_DURUMU], N'') <> N'Ödendi'");
        }

        return (string.Join(Environment.NewLine + "                ", clauses), parameters);
    }

    private static string BuildCommissionCollectionOrder(string sortBy, string sortDir)
    {
        var column = sortBy switch
        {
            "hotel" => "agg.[otel_adi]",
            "district" => "agg.[ilce]",
            "donem" => "agg.[donem]",
            "rez" => "agg.[rez_adet]",
            "collected" => "agg.[tahsil_edilen]",
            "pending" => "agg.[bekleyen]",
            _ => "agg.[brut_komisyon]"
        };
        var dir = sortDir == "asc" ? "ASC" : "DESC";
        return $"{column} {dir}, agg.[otel_adi] ASC";
    }

    private static void BindCommissionCollectionParameters(SqlCommand command, Dictionary<string, object?> parameters)
    {
        foreach (var pair in parameters)
        {
            command.Parameters.AddWithValue(pair.Key, pair.Value ?? DBNull.Value);
        }
    }

    private static string? EmptyToNull(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

