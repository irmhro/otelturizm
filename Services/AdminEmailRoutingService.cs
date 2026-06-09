using System.Globalization;
using System.Net;
using System.Text;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using otelturizmnew.Models.Email;
using otelturizmnew.Models.Paneller.Admin;
using otelturizmnew.Services.Abstractions;

namespace otelturizmnew.Services;

public class AdminEmailRoutingService : IAdminEmailRoutingService
{
    private readonly string _connectionString;
    private readonly IConfiguration _configuration;
    private readonly IEmailQueueService _emailQueueService;
    private readonly IAdminService _adminService;
    private readonly ILogger<AdminEmailRoutingService> _logger;

    public AdminEmailRoutingService(
        IConfiguration configuration,
        IEmailQueueService emailQueueService,
        IAdminService adminService,
        ILogger<AdminEmailRoutingService> logger)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection tanimli degil.");
        _configuration = configuration;
        _emailQueueService = emailQueueService;
        _adminService = adminService;
        _logger = logger;
    }

    private string PublicBase => (_configuration["App:PublicBaseUrl"] ?? "https://localhost:7223").TrimEnd('/');

    private string FallbackEmail => (_configuration["Admin:NotificationFallbackEmail"] ?? "irmhro0@gmail.com").Trim();

    public async Task<AdminEmailRoutingPageViewModel> GetPageAsync(string fullName, string email, string userRole, CancellationToken cancellationToken = default)
    {
        var shell = await _adminService.GetShellForEmailRoutingAsync(fullName, email, userRole, cancellationToken);
        var model = new AdminEmailRoutingPageViewModel { Shell = shell };

        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            const string sql = @"
                SELECT [OLAY_KODU], [BASLIK], [ACIKLAMA], [HEDEF_EPOSTALAR], [AKTIF_MI]
                FROM [dbo].[ADMIN_EPOSTA_YONLENDIRME]
                ORDER BY [BASLIK];";

            await using var cmd = new SqlCommand(sql, connection);
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var code = reader.GetString(0);
                model.Rows.Add(new AdminEmailRoutingRowEditModel
                {
                    EventCode = code,
                    Title = reader.GetString(1),
                    Description = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                    EmailsCsv = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                    Active = !reader.IsDBNull(4) && reader.GetBoolean(4),
                    DeepLinkHint = RoutingHints.TryGetDeepLink(code, PublicBase)
                });
            }
        }
        catch (SqlException ex) when (IsMissingTable(ex))
        {
            model.Shell.PanelSubtitle = "Veritabanında ADMIN_EPOSTA_YONLENDIRME tablosu bulunamadı. Database/MigrationsSql/tablo/migrationlar/001_ADMIN_EPOSTA_YONLENDIRME.sql dosyasını çalıştırın.";
        }

        return model;
    }

    public async Task<(bool Success, string Message)> SaveAsync(long adminUserId, AdminEmailRoutingSaveForm form, CancellationToken cancellationToken = default)
    {
        if (form.Rows is null || form.Rows.Count == 0)
        {
            return (false, "Kaydedilecek satır yok.");
        }

        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            await using var tx = await connection.BeginTransactionAsync(cancellationToken);

            const string upd = @"
                UPDATE [dbo].[ADMIN_EPOSTA_YONLENDIRME]
                SET [HEDEF_EPOSTALAR] = @emails,
                    [AKTIF_MI] = @active,
                    [GUNCELLENME_UTC] = SYSUTCDATETIME()
                WHERE [OLAY_KODU] = @code;";

            foreach (var row in form.Rows)
            {
                if (string.IsNullOrWhiteSpace(row.EventCode))
                {
                    continue;
                }

                await using var cmd = new SqlCommand(upd, connection, (SqlTransaction)tx);
                cmd.Parameters.AddWithValue("@code", row.EventCode.Trim());
                cmd.Parameters.AddWithValue("@emails", (object?)NormalizeEmails(row.EmailsCsv) ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@active", row.Active);
                var n = await cmd.ExecuteNonQueryAsync(cancellationToken);
                if (n == 0)
                {
                    await tx.RollbackAsync(cancellationToken);
                    return (false, $"Bilinmeyen olay kodu: {row.EventCode}");
                }
            }

            await tx.CommitAsync(cancellationToken);
            _logger.LogInformation("Admin {AdminUserId} e-posta yönlendirmelerini güncelledi.", adminUserId);
            return (true, "E-posta yönlendirmeleri güncellendi.");
        }
        catch (SqlException ex) when (IsMissingTable(ex))
        {
            return (false, "Tablo henüz oluşturulmamış. Önce SQL migration'ı çalıştırın.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "E-posta yönlendirme kaydı başarısız.");
            return (false, $"Kayıt sırasında hata: {ex.Message}");
        }
    }

    public Task NotifyPartnerRegistrationAsync(
        long applicantUserId,
        long partnerId,
        long hotelId,
        string hotelName,
        string companyName,
        string contactName,
        string applicantEmail,
        string phone,
        string city,
        string district,
        CancellationToken cancellationToken = default)
        => QueueAdminEventAsync(
            "partner_kayit",
            applicantUserId,
            "[Otelturizm] Yeni partner başvurusu",
            "Partner kaydı",
            "Web üzerinden yeni bir partner / taslak otel başvurusu tamamlandı. E-posta doğrulaması ve admin incelemesi beklenir.",
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Tesis adı"] = hotelName,
                ["Şirket ünvanı"] = companyName,
                ["Yetkili"] = contactName,
                ["E-posta"] = applicantEmail,
                ["Telefon"] = phone,
                ["İl / ilçe"] = $"{city} / {district}",
                ["Partner ID"] = partnerId.ToString(CultureInfo.InvariantCulture),
                ["Otel ID"] = hotelId.ToString(CultureInfo.InvariantCulture),
                ["Kullanıcı ID"] = applicantUserId.ToString(CultureInfo.InvariantCulture)
            },
            $"{PublicBase}/admin/partner-basvurulari",
            "Partner başvurularını aç",
            "partners",
            partnerId,
            cancellationToken);

    public Task NotifyFirmaRegistrationAsync(
        long applicantUserId,
        long firmaId,
        string companyName,
        string contactName,
        string applicantEmail,
        string phone,
        string city,
        CancellationToken cancellationToken = default)
        => QueueAdminEventAsync(
            "firma_kayit",
            applicantUserId,
            "[Otelturizm] Yeni firma başvurusu",
            "Firma başvurusu",
            "Web üzerinden yeni bir kurumsal firma başvurusu alındı. Başvuru incelemesi için admin panelini kullanın.",
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Firma"] = companyName,
                ["Yetkili"] = contactName,
                ["E-posta"] = applicantEmail,
                ["Telefon"] = phone,
                ["Şehir"] = city,
                ["Firma ID"] = firmaId.ToString(CultureInfo.InvariantCulture),
                ["Kullanıcı ID"] = applicantUserId.ToString(CultureInfo.InvariantCulture)
            },
            $"{PublicBase}/admin/firma-basvurulari",
            "Firma başvurularını aç",
            "firmalar",
            firmaId,
            cancellationToken);

    private async Task QueueAdminEventAsync(
        string eventCode,
        long actorUserId,
        string subject,
        string title,
        string intro,
        Dictionary<string, string> detailPairs,
        string primaryUrl,
        string primaryLabel,
        string? relatedTable,
        long? relatedRecordId,
        CancellationToken cancellationToken)
    {
        try
        {
            var recipients = await ResolveRecipientsAsync(eventCode, cancellationToken);
            if (recipients.Count == 0)
            {
                return;
            }

            var detailHtml = BuildDetailTable(detailPairs);
            var tokens = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["email_subject"] = subject,
                ["badge"] = "Yönetim bildirimi",
                ["title"] = title,
                ["intro"] = intro,
                ["detail_html"] = detailHtml,
                ["primary_url"] = primaryUrl,
                ["primary_label"] = primaryLabel,
                ["event_code"] = eventCode,
                ["occurred_at"] = DateTime.Now.ToString("dd.MM.yyyy HH:mm", CultureInfo.GetCultureInfo("tr-TR"))
            };

            var templateCode = ResolveTemplateCode(eventCode);

            foreach (var to in recipients)
            {
                await _emailQueueService.QueueTemplateAsync(new QueuedEmailTemplateRequest
                {
                    UserId = actorUserId,
                    RecipientEmail = to,
                    TemplateCode = templateCode,
                    SubjectOverride = subject,
                    RelatedTable = relatedTable,
                    RelatedRecordId = relatedRecordId,
                    Tokens = tokens
                }, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Admin olay e-postası kuyruğa alınamadı: {EventCode}", eventCode);
        }
    }

    private async Task<List<string>> ResolveRecipientsAsync(string eventCode, CancellationToken cancellationToken)
    {
        var list = new List<string>();
        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            const string sql = @"
                SELECT [HEDEF_EPOSTALAR], [AKTIF_MI]
                FROM [dbo].[ADMIN_EPOSTA_YONLENDIRME]
                WHERE [OLAY_KODU] = @c;";

            await using var cmd = new SqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@c", eventCode);
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                if (!reader.IsDBNull(1) && !reader.GetBoolean(1))
                {
                    return list;
                }

                var raw = reader.IsDBNull(0) ? string.Empty : reader.GetString(0);
                list.AddRange(ParseEmails(raw));
            }
        }
        catch (SqlException ex) when (IsMissingTable(ex))
        {
            _logger.LogWarning("ADMIN_EPOSTA_YONLENDIRME tablosu yok; fallback e-posta kullanılacak.");
        }

        if (list.Count == 0 && !string.IsNullOrWhiteSpace(FallbackEmail))
        {
            list.Add(FallbackEmail);
        }

        return list.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static List<string> ParseEmails(string? csv)
    {
        if (string.IsNullOrWhiteSpace(csv))
        {
            return new List<string>();
        }

        var parts = csv.Split(new[] { ',', ';', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return parts.Where(static p => p.Contains('@', StringComparison.Ordinal)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static string? NormalizeEmails(string? csv)
    {
        var emails = ParseEmails(csv);
        return emails.Count == 0 ? string.Empty : string.Join(",", emails);
    }

    private static string BuildDetailTable(Dictionary<string, string> pairs)
    {
        var sb = new StringBuilder();
        sb.Append("<table style=\"width:100%;border-collapse:collapse;font-size:14px;\">");
        foreach (var kv in pairs)
        {
            sb.Append("<tr>");
            sb.Append("<td style=\"padding:6px 10px 6px 0;color:#64748b;vertical-align:top;width:38%;\">")
                .Append(WebUtility.HtmlEncode(kv.Key))
                .Append("</td><td style=\"padding:6px 0;\"><strong>")
                .Append(WebUtility.HtmlEncode(kv.Value))
                .Append("</strong></td></tr>");
        }

        sb.Append("</table>");
        return sb.ToString();
    }

    private static string ResolveTemplateCode(string eventCode)
    {
        return (eventCode ?? string.Empty).Trim().ToLowerInvariant() switch
        {
            "partner_kayit" => "admin_partner_basvuru",
            "firma_kayit" => "admin_firma_basvuru",
            _ => "admin_routing_notice"
        };
    }

    private static bool IsMissingTable(SqlException ex)
    {
        var m = ex.Message ?? string.Empty;
        return m.Contains("Invalid object name", StringComparison.OrdinalIgnoreCase)
               || m.Contains("Could not find stored procedure", StringComparison.OrdinalIgnoreCase);
    }

    private static class RoutingHints
    {
        public static string TryGetDeepLink(string code, string origin)
        {
            return code switch
            {
                "partner_kayit" => $"{origin}/admin/partner-basvurulari",
                "firma_kayit" => $"{origin}/admin/firma-basvurulari",
                "kullanici_kayit" => $"{origin}/admin/kullanicilar",
                "rezervasyon_yonetim" => $"{origin}/admin/rezervasyonlar",
                "odeme_uyari" => $"{origin}/admin/odemeler",
                "sikayet" => $"{origin}/admin/sikayetler",
                "gelistirme_talebi" => $"{origin}/admin/gelistirme-talepleri",
                "destek_talebi" => $"{origin}/yardim-merkezi",
                "bildirim_kritik" => $"{origin}/admin/sistem-sagligi",
                "firma_limit" => $"{origin}/admin/firma-rezervasyonlari",
                "partner_evrak" => $"{origin}/admin/partner-basvurulari",
                "kvkk_basvuru" => $"{origin}/admin/ayarlar",
                _ => origin + "/admin/dashboard"
            };
        }
    }
}
