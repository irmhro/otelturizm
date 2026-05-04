using System.Globalization;
using Microsoft.Data.SqlClient;
using otelturizmnew.Models.Paneller.Departman;
using otelturizmnew.Services.Abstractions;

namespace otelturizmnew.Services;

public sealed class DepartmentPanelService : IDepartmentPanelService
{
    private static readonly DepartmentPanelNavItemViewModel[] DepartmentDefinitions =
    {
        new() { Key = "kullanici", Label = "Kullanıcı", IconClass = "fa-users" },
        new() { Key = "partner", Label = "Partner", IconClass = "fa-hotel" },
        new() { Key = "firma", Label = "Firma", IconClass = "fa-building" },
        new() { Key = "satis", Label = "Satış", IconClass = "fa-handshake" },
        new() { Key = "muhasebe", Label = "Muhasebe", IconClass = "fa-file-invoice-dollar" },
        new() { Key = "destek", Label = "Destek", IconClass = "fa-headset" }
    };

    private readonly string _connectionString;

    public DepartmentPanelService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection tanımlı değil.");
    }

    public async Task<DepartmentDashboardPageViewModel> GetDashboardAsync(string departmentKey, string fullName, string email, string role, CancellationToken cancellationToken = default)
    {
        var key = NormalizeDepartmentKey(departmentKey, role);
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var shell = BuildShell(key, fullName, email, role);
        var model = new DepartmentDashboardPageViewModel { Shell = shell };
        await FillDepartmentDataAsync(connection, model, key, cancellationToken);
        FillProtocols(model, key);
        return model;
    }

    private static DepartmentPanelShellViewModel BuildShell(string key, string fullName, string email, string role)
    {
        var active = DepartmentDefinitions.FirstOrDefault(x => x.Key == key) ?? DepartmentDefinitions[0];
        return new DepartmentPanelShellViewModel
        {
            FullName = fullName,
            Email = email,
            Role = role,
            ActiveDepartment = active.Key,
            PanelTitle = $"{active.Label} Departmanı",
            PanelSubtitle = "Departman görevleri, onay kuyrukları ve operasyon KPI'ları tek panelde izlenir.",
            Departments = DepartmentDefinitions
                .Select(x => new DepartmentPanelNavItemViewModel
                {
                    Key = x.Key,
                    Label = x.Label,
                    IconClass = x.IconClass,
                    IsActive = x.Key == active.Key
                })
                .ToList()
        };
    }

    private static string NormalizeDepartmentKey(string? requested, string? role)
    {
        var raw = (requested ?? string.Empty).Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(raw) || raw == "dashboard")
        {
            raw = (role ?? string.Empty).Trim().ToLowerInvariant();
        }

        raw = raw.Replace("departman_", string.Empty).Replace("department_", string.Empty);
        return DepartmentDefinitions.Any(x => x.Key == raw) ? raw : "kullanici";
    }

    private static async Task FillDepartmentDataAsync(SqlConnection connection, DepartmentDashboardPageViewModel model, string key, CancellationToken cancellationToken)
    {
        switch (key)
        {
            case "partner":
                await FillPartnerDepartmentAsync(connection, model, cancellationToken);
                break;
            case "firma":
                await FillCompanyDepartmentAsync(connection, model, cancellationToken);
                break;
            case "satis":
                await FillSalesDepartmentAsync(connection, model, cancellationToken);
                break;
            case "muhasebe":
                await FillAccountingDepartmentAsync(connection, model, cancellationToken);
                break;
            case "destek":
                await FillSupportDepartmentAsync(connection, model, cancellationToken);
                break;
            default:
                await FillUserDepartmentAsync(connection, model, cancellationToken);
                break;
        }
    }

    private static async Task FillUserDepartmentAsync(SqlConnection connection, DepartmentDashboardPageViewModel model, CancellationToken cancellationToken)
    {
        await AddMetricAsync(connection, model, "Toplam Kullanıcı", "SELECT COUNT(*) FROM users", "Kayıtlı kullanıcı hesabı", "info", "fa-users", cancellationToken);
        await AddMetricAsync(connection, model, "Bugün Giriş", "SELECT COUNT(*) FROM kullanici_giris_loglari WHERE giris_tarihi >= CONVERT(date, SYSUTCDATETIME())", "Günlük oturum hareketi", "success", "fa-right-to-bracket", cancellationToken);
        await AddMetricAsync(connection, model, "Onaysız Yorum", "SELECT COUNT(*) FROM yorumlar WHERE COALESCE(onay_durumu,'Beklemede')='Beklemede'", "Yayın bekleyen yorumlar", "warning", "fa-star-half-stroke", cancellationToken);
        model.WorkItems.Add(new() { Type = "Yorum", Title = "Misafir değerlendirmeleri", Detail = "Bekleyen yorumları kontrol et ve yayın standardına göre onayla.", StatusText = "Bekliyor", ToneClass = "warning", ActionUrl = "/admin/degerlendirmeler" });
    }

    private static async Task FillPartnerDepartmentAsync(SqlConnection connection, DepartmentDashboardPageViewModel model, CancellationToken cancellationToken)
    {
        await AddMetricAsync(connection, model, "Partner Başvurusu", "SELECT COUNT(*) FROM partner_detaylari WHERE COALESCE(onay_durumu,'Beklemede')='Beklemede'", "Admin evrak/onay bekleyen partnerler", "warning", "fa-file-signature", cancellationToken);
        await AddMetricAsync(connection, model, "Yayın Bekleyen Otel", "SELECT COUNT(*) FROM oteller WHERE COALESCE(onay_durumu,'Beklemede') <> 'Onaylandi' OR COALESCE(yayin_durumu,'Kapali') <> 'Yayinda'", "Onay/yayın kararı bekleyen oteller", "danger", "fa-circle-pause", cancellationToken);
        await AddMetricAsync(connection, model, "Toplam Otel", "SELECT COUNT(*) FROM oteller", "Partner otel portföyü", "info", "fa-hotel", cancellationToken);
        model.WorkItems.Add(new() { Type = "Partner", Title = "Partner başvuruları", Detail = "Evrak, e-posta giriş onayı ve otel yayın bağlantısını kontrol et.", StatusText = "İnceleme", ToneClass = "warning", ActionUrl = "/admin/partner-basvurulari" });
        model.WorkItems.Add(new() { Type = "Otel", Title = "Otel yayın/onay", Detail = "Admin onayı olmayan otellerin listelenmesini engelle ve yayına açma kararını ver.", StatusText = "Kritik", ToneClass = "danger", ActionUrl = "/admin/onay-merkezi" });
    }

    private static async Task FillCompanyDepartmentAsync(SqlConnection connection, DepartmentDashboardPageViewModel model, CancellationToken cancellationToken)
    {
        await AddMetricAsync(connection, model, "Firma Başvurusu", "SELECT COUNT(*) FROM firmalar WHERE COALESCE(onay_durumu,'Beklemede')='Beklemede'", "Kurumsal onay kuyruğu", "warning", "fa-building", cancellationToken);
        await AddMetricAsync(connection, model, "Firma Rezervasyonu", "SELECT COUNT(*) FROM rezervasyonlar WHERE firma_id IS NOT NULL", "Firma kaynaklı konaklama talebi", "success", "fa-calendar-check", cancellationToken);
        await AddMetricAsync(connection, model, "Firma Faturası", "SELECT COUNT(*) FROM faturalar WHERE firma_id IS NOT NULL", "Firma panelinde görünecek faturalar", "info", "fa-receipt", cancellationToken);
        model.WorkItems.Add(new() { Type = "Firma", Title = "Firma başvuruları", Detail = "Vergi no, sicil, MERSİS ve yetkili bilgisi kontrol edilecek.", StatusText = "Bekliyor", ToneClass = "warning", ActionUrl = "/admin/firma-basvurulari" });
    }

    private static async Task FillSalesDepartmentAsync(SqlConnection connection, DepartmentDashboardPageViewModel model, CancellationToken cancellationToken)
    {
        await AddMetricAsync(connection, model, "Satış Müşterisi", "SELECT COUNT(*) FROM satis_musterileri", "Satış müşteri havuzu", "info", "fa-address-book", cancellationToken);
        await AddMetricAsync(connection, model, "Satış Rezervasyonu", "SELECT COUNT(*) FROM rezervasyonlar WHERE satis_temsilcisi_id IS NOT NULL", "Satış temsilcisi bağlantılı kayıt", "success", "fa-handshake", cancellationToken);
        await AddMetricAsync(connection, model, "Satış Cirosu", "SELECT COALESCE(SUM(COALESCE(toplam_tutar,0)),0) FROM rezervasyonlar WHERE satis_temsilcisi_id IS NOT NULL", "Satış kaynaklı toplam ciro", "success", "fa-chart-line", cancellationToken, true);
        model.WorkItems.Add(new() { Type = "Satış", Title = "Satış rezervasyonları", Detail = "Müşteri, otel ve rezervasyon ciro hareketlerini takip et.", StatusText = "Aktif", ToneClass = "success", ActionUrl = "/panel/satis/rezervasyonlarim" });
    }

    private static async Task FillAccountingDepartmentAsync(SqlConnection connection, DepartmentDashboardPageViewModel model, CancellationToken cancellationToken)
    {
        await AddMetricAsync(connection, model, "Toplam Komisyon", "SELECT COALESCE(SUM(COALESCE(komisyon_tutari,0)),0) FROM komisyon_muhasebe_kayitlari", "Tahakkuk eden komisyon", "success", "fa-percent", cancellationToken, true);
        await AddMetricAsync(connection, model, "Bekleyen Fatura", "SELECT COUNT(*) FROM faturalar WHERE COALESCE(fatura_durumu,'Taslak') IN ('Taslak','Beklemede')", "Onay/yükleme bekleyen faturalar", "warning", "fa-file-invoice", cancellationToken);
        await AddMetricAsync(connection, model, "Mutabakat İtirazı", "SELECT COUNT(*) FROM komisyon_muhasebe_kayitlari WHERE COALESCE(itiraz_var_mi,0)=1", "Çözüm bekleyen muhasebe kayıtları", "danger", "fa-triangle-exclamation", cancellationToken);
        model.WorkItems.Add(new() { Type = "Muhasebe", Title = "Komisyon ve fatura", Detail = "Otel komisyon oranları, vergi ve fatura yükleme süreçlerini kontrol et.", StatusText = "Kontrol", ToneClass = "warning", ActionUrl = "/admin/komisyonlar" });
    }

    private static async Task FillSupportDepartmentAsync(SqlConnection connection, DepartmentDashboardPageViewModel model, CancellationToken cancellationToken)
    {
        await AddMetricAsync(connection, model, "Partner Destek", "SELECT COUNT(*) FROM partner_destek_talepleri WHERE COALESCE(durum,'Acik') <> 'Kapali'", "Açık partner destek talepleri", "warning", "fa-headset", cancellationToken);
        await AddMetricAsync(connection, model, "Mesaj Konuşması", "SELECT COUNT(*) FROM mesaj_konusmalari", "Platform mesaj merkezi", "info", "fa-comments", cancellationToken);
        await AddMetricAsync(connection, model, "Sistem Hatası", "SELECT COUNT(*) FROM sistem_hata_loglari WHERE COALESCE(cozuldu_mu,0)=0", "Çözülmemiş hata kaydı", "danger", "fa-bug", cancellationToken);
        model.WorkItems.Add(new() { Type = "Destek", Title = "Açık destek talepleri", Detail = "Partner, firma ve kullanıcı destek işlerini SLA mantığında takip et.", StatusText = "Açık", ToneClass = "warning", ActionUrl = "/admin/destek-makaleleri" });
    }

    private static async Task AddMetricAsync(SqlConnection connection, DepartmentDashboardPageViewModel model, string label, string sql, string description, string tone, string icon, CancellationToken cancellationToken, bool money = false)
    {
        object? raw;
        try
        {
            await using var command = new SqlCommand(sql, connection);
            raw = await command.ExecuteScalarAsync(cancellationToken);
        }
        catch (SqlException)
        {
            raw = 0;
            description = $"{description} - tablo/sutun hazırlığı bekliyor";
            tone = "muted";
        }

        var value = money
            ? $"{Convert.ToDecimal(raw ?? 0, CultureInfo.InvariantCulture).ToString("N0", CultureInfo.GetCultureInfo("tr-TR"))} TL"
            : Convert.ToInt32(raw ?? 0, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture);
        model.Metrics.Add(new() { Label = label, Value = value, Description = description, ToneClass = tone, IconClass = icon });
    }

    private static void FillProtocols(DepartmentDashboardPageViewModel model, string key)
    {
        model.Protocols.Add(new() { Title = "Tablo standardı", Detail = "Listeleme olan her departman sayfası kart değil tablo mantığıyla tasarlanacak.", StatusText = "Zorunlu" });
        model.Protocols.Add(new() { Title = "Aksiyon standardı", Detail = "Butonlar ikon-only değil yazılı olacak; İncele, Onayla, Reddet, Askıya Al, Fatura gibi net aksiyonlar kullanılacak.", StatusText = "Aktif" });
        model.Protocols.Add(new() { Title = $"{model.Shell.PanelTitle} iş protokolü", Detail = key switch
        {
            "partner" => "Partner evrak, otel onay/yayın, komisyon ve fatura akışı admin kararıyla ilerler.",
            "firma" => "Firma başvuru, personel, rezervasyon ve fatura süreçleri admin/firma/partner izleriyle takip edilir.",
            "muhasebe" => "Komisyon, vergi, mutabakat ve fatura dosyaları güvenli dosya standardıyla yönetilir.",
            "destek" => "Destek talepleri SLA, konuşma ve çözüm notu ile kayıt altına alınır.",
            "satis" => "Satış ciro, müşteri ve rezervasyon verileri temsilci bazlı raporlanır.",
            _ => "Kullanıcı rezervasyon, yorum, bildirim ve profil güvenliği kayıt altına alınır."
        }, StatusText = "Planlandı" });
    }
}
