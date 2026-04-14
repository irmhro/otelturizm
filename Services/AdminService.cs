using Microsoft.Extensions.Configuration;
using MySqlConnector;
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
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var shell = await GetShellAsync(connection, "Dashboard", "Panel genel operasyon durumunu ve kritik metrikleri canli verilerle takip edin.", fullName, email, userRole, cancellationToken);
        var model = new AdminDashboardViewModel { Shell = shell };

        const string metricsSql = @"
            SELECT
                (SELECT COUNT(*) FROM oteller) AS total_hotels,
                (SELECT COUNT(*) FROM rezervasyonlar) AS total_reservations,
                (SELECT COUNT(*) FROM odeme_islemleri WHERE odeme_durumu IN ('Başarılı','Geri Ödendi','Kısmi Geri Ödendi')) AS successful_payments,
                (SELECT COUNT(*) FROM users WHERE rol = 'admin') AS admin_count;";

        await using (var command = new MySqlCommand(metricsSql, connection))
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            if (await reader.ReadAsync(cancellationToken))
            {
                model.Metrics.Add(new AdminMetricCardViewModel { Label = "Toplam Otel", Value = SafeInt(reader, 0).ToString(), TrendText = "Yayin, taslak ve bakim tum oteller", IconClass = "fa-hotel", ToneClass = "info" });
                model.Metrics.Add(new AdminMetricCardViewModel { Label = "Toplam Rezervasyon", Value = SafeInt(reader, 1).ToString(), TrendText = "Tum rezervasyon kayitlari", IconClass = "fa-calendar-check", ToneClass = "success" });
                model.Metrics.Add(new AdminMetricCardViewModel { Label = "Basarili Odeme", Value = SafeInt(reader, 2).ToString(), TrendText = "Tahsilat ve iade dahil tamamlanan islemler", IconClass = "fa-credit-card", ToneClass = "warning" });
                model.Metrics.Add(new AdminMetricCardViewModel { Label = "Admin Kullanici", Value = SafeInt(reader, 3).ToString(), TrendText = "Yonetsel yetkili aktif hesaplar", IconClass = "fa-user-shield", ToneClass = "danger" });
            }
        }

        const string chartSql = @"
            SELECT DATE_FORMAT(olusturulma_tarihi, '%b') AS ay, COUNT(*) AS adet
            FROM rezervasyonlar
            WHERE olusturulma_tarihi >= DATE_SUB(CURDATE(), INTERVAL 5 MONTH)
            GROUP BY YEAR(olusturulma_tarihi), MONTH(olusturulma_tarihi), DATE_FORMAT(olusturulma_tarihi, '%b')
            ORDER BY YEAR(olusturulma_tarihi), MONTH(olusturulma_tarihi);";

        var chartRows = new List<(string Label, int Value)>();
        await using (var chartCommand = new MySqlCommand(chartSql, connection))
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
            ORDER BY zaman DESC
            LIMIT 6;";

        await using (var activityCommand = new MySqlCommand(activitySql, connection))
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
            SELECT
                o.otel_adi,
                CONCAT(o.ilce, ', ', o.sehir) AS sehir_label,
                o.yayin_durumu,
                o.ortalama_puan,
                COUNT(r.id) AS rezervasyon_adedi
            FROM oteller o
            LEFT JOIN rezervasyonlar r ON r.otel_id = o.id
            GROUP BY o.id, o.otel_adi, o.ilce, o.sehir, o.yayin_durumu, o.ortalama_puan
            ORDER BY rezervasyon_adedi DESC, o.id DESC
            LIMIT 6;";

        await using (var hotelsCommand = new MySqlCommand(hotelsSql, connection))
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

        return model;
    }

    public async Task<AdminSectionPageViewModel> GetSectionPageAsync(string sectionKey, string fullName, string email, string userRole, CancellationToken cancellationToken = default)
    {
        await using var connection = new MySqlConnection(_connectionString);
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

    private async Task<AdminShellViewModel> GetShellAsync(MySqlConnection connection, string title, string subtitle, string fullName, string email, string userRole, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT
                (SELECT COUNT(*) FROM partner_detaylari WHERE onay_durumu = 'Beklemede') AS pending_partner_applications,
                (SELECT COUNT(*) FROM sistem_ici_bildirimler WHERE okundu_mu = 0) AS unread_notifications,
                (SELECT COUNT(*) FROM sistem_hata_loglari WHERE hata_seviyesi IN ('CRITICAL','ALERT','EMERGENCY') AND cozuldu_mu = 0) AS critical_logs,
                (SELECT COUNT(*) FROM yorumlar WHERE onay_durumu = 'Beklemede') AS pending_reviews;";

        await using var command = new MySqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var shell = new AdminShellViewModel { FullName = fullName, Email = email, UserRole = userRole, PanelTitle = title, PanelSubtitle = subtitle };
        if (await reader.ReadAsync(cancellationToken))
        {
            shell.PendingPartnerApplications = SafeInt(reader, 0);
            shell.UnreadNotifications = SafeInt(reader, 1);
            shell.CriticalLogs = SafeInt(reader, 2);
            shell.PendingReviews = SafeInt(reader, 3);
        }

        return shell;
    }

    private static async Task FillSummaryCardsAsync(MySqlConnection connection, AdminSectionPageViewModel model, string sectionKey, CancellationToken cancellationToken)
    {
        var cards = GetSummaryDefinitions(sectionKey);
        foreach (var card in cards)
        {
            await using var command = new MySqlCommand(card.Sql, connection);
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

    private static async Task FillTableAsync(MySqlConnection connection, AdminSectionPageViewModel model, string sectionKey, CancellationToken cancellationToken)
    {
        var sql = GetTableSql(sectionKey);
        if (string.IsNullOrWhiteSpace(sql))
        {
            return;
        }

        await using var command = new MySqlCommand(sql, connection);
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
            "users" => ("Kullanicilar", "Tum kullanici tiplerini, rollerini ve hesap durumlarini veritabani kayitlari ile yonetin.", new[] { "Kullanici", "E-posta", "Rol", "Durum", "Kayit" }, "Kullanici kaydi bulunamadi.", null),
            "managers" => ("Yoneticiler", "Admin ve ekip kullanicilarini departman ve rol dagilimi ile izleyin.", new[] { "Ad Soyad", "E-posta", "Departman", "Rol", "Son Giris" }, "Yonetici kaydi bulunamadi.", null),
            "hotels" => ("Oteller", "Otel, yayin ve onay durumlarini tek ekranda izleyin.", new[] { "Otel", "Konum", "Tur", "Yayin", "Onay", "Puan" }, "Otel kaydi bulunamadi.", null),
            "hotel-detail" => ("Otel Detay", "Referans admin otel detay ekranini, secili otelin tum panel verileri ile baglayacagiz.", Array.Empty<string>(), "Detay ekrani icin otel secimi gerekiyor.", "Bu ekran sonraki adimda secili otel bazli detay verilerle ayri servisle baglanacak."),
            "reservations" => ("Rezervasyonlar", "Rezervasyon hareketlerini dogrudan yerel veritabanindan izleyin.", new[] { "Rez. No", "Misafir", "Giris", "Cikis", "Durum", "Tutar" }, "Rezervasyon bulunamadi.", null),
            "payments" => ("Odemeler", "Tahsilat, iade ve risk durumlarini odeme tablosu uzerinden yonetin.", new[] { "Islem No", "Tur", "Durum", "Yontem", "Tahsilat", "Tarih" }, "Odeme kaydi bulunamadi.", null),
            "invoices" => ("Faturalar", "Platform ve otel faturalarini veritabani kayitlari ile izleyin.", new[] { "Fatura No", "Tarih", "Tur", "Durum", "Toplam", "PB" }, "Fatura kaydi bulunamadi.", null),
            "commissions" => ("Komisyonlar", "Komisyon muhasebe ve mutabakat durumlarini takip edin.", new[] { "Kayit No", "Donem", "Otel", "Komisyon", "Odeme Durumu", "Mutabakat" }, "Komisyon kaydi bulunamadi.", null),
            "partner-applications" => ("Partner Basvurulari", "Partner onboarding surecini ve onay akisini yonetin.", new[] { "Firma", "Yetkili", "E-posta", "Vergi No", "Durum", "Kayit" }, "Partner basvurusu bulunamadi.", null),
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
                ("Admin", "SELECT COUNT(*) FROM users WHERE rol = 'admin'", "Yonetim kullanicilari", "danger", "fa-user-shield"),
                ("Partner", "SELECT COUNT(*) FROM users WHERE rol LIKE 'partner_%'", "Partner yoneten hesaplar", "warning", "fa-building"),
                ("Aktif Hesap", "SELECT COUNT(*) FROM users WHERE hesap_durumu = 1", "Giris yapabilen hesaplar", "success", "fa-circle-check")
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
            "partner-applications" =>
            [
                ("Toplam Partner", "SELECT COUNT(*) FROM partner_detaylari", "Tum partner hesaplari", "info", "fa-handshake-angle"),
                ("Beklemede", "SELECT COUNT(*) FROM partner_detaylari WHERE onay_durumu = 'Beklemede'", "Inceleme bekleyen basvurular", "warning", "fa-hourglass-half"),
                ("Onaylandi", "SELECT COUNT(*) FROM partner_detaylari WHERE onay_durumu = 'Onaylandi'", "Aktif partner hesaplari", "success", "fa-circle-check"),
                ("Reddedildi", "SELECT COUNT(*) FROM partner_detaylari WHERE onay_durumu = 'Reddedildi'", "Reddedilen kayitlar", "danger", "fa-circle-xmark")
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
            "users" => @"SELECT ad_soyad, eposta, rol, hesap_durumu, DATE_FORMAT(olusturulma_tarihi, '%d.%m.%Y') FROM users ORDER BY id DESC LIMIT 12;",
            "managers" => @"SELECT u.ad_soyad, u.eposta, COALESCE(d.departman_adi, '-'), COALESCE(r.rol_adi, u.rol), COALESCE(DATE_FORMAT(u.son_giris_tarihi, '%d.%m.%Y %H:%i'), '-') FROM users u LEFT JOIN kullanici_departman kd ON kd.kullanici_id = u.id LEFT JOIN departmanlar d ON d.id = kd.departman_id LEFT JOIN kullanici_rolleri kr ON kr.kullanici_id = u.id AND (kr.bitis_tarihi IS NULL OR kr.bitis_tarihi > NOW()) LEFT JOIN roller r ON r.id = kr.rol_id WHERE u.rol = 'admin' ORDER BY u.id DESC LIMIT 12;",
            "hotels" => @"SELECT otel_adi, CONCAT(ilce, ', ', sehir), otel_turu, yayin_durumu, onay_durumu, FORMAT(ortalama_puan,1) FROM oteller ORDER BY id DESC LIMIT 12;",
            "reservations" => @"SELECT rezervasyon_no, misafir_ad_soyad, DATE_FORMAT(giris_tarihi, '%d.%m.%Y'), DATE_FORMAT(cikis_tarihi, '%d.%m.%Y'), durum, FORMAT(toplam_tutar,0) FROM rezervasyonlar ORDER BY id DESC LIMIT 12;",
            "payments" => @"SELECT islem_no, odeme_turu, odeme_durumu, odeme_yontemi, FORMAT(toplam_tahsilat,0), DATE_FORMAT(odeme_baslangic_tarihi, '%d.%m.%Y %H:%i') FROM odeme_islemleri ORDER BY id DESC LIMIT 12;",
            "invoices" => @"SELECT fatura_no, DATE_FORMAT(fatura_tarihi, '%d.%m.%Y'), fatura_turu, fatura_durumu, FORMAT(genel_toplam,0), para_birimi FROM faturalar ORDER BY id DESC LIMIT 12;",
            "commissions" => @"SELECT kayit_no, donem, o.otel_adi, FORMAT(komisyon_tutari,0), otele_odeme_durumu, mutabakat_durumu FROM komisyon_muhasebe_kayitlari k LEFT JOIN oteller o ON o.id = k.otel_id ORDER BY k.id DESC LIMIT 12;",
            "partner-applications" => @"SELECT firma_unvani, yetkili_ad_soyad, yetkili_eposta, vergi_numarasi, onay_durumu, DATE_FORMAT(olusturulma_tarihi, '%d.%m.%Y') FROM partner_detaylari ORDER BY id DESC LIMIT 12;",
            "reviews" => @"SELECT COALESCE(yorum_basligi, 'Basliksiz'), genel_puan, onay_durumu, rapor_sayisi, dogrulanmis_konaklama, DATE_FORMAT(olusturulma_tarihi, '%d.%m.%Y') FROM yorumlar ORDER BY id DESC LIMIT 12;",
            "campaigns" => @"SELECT kampanya_adi, tur, DATE_FORMAT(baslangic_tarihi, '%d.%m.%Y'), DATE_FORMAT(bitis_tarihi, '%d.%m.%Y'), aktif_mi, kullanilan_adet FROM kampanyalar ORDER BY id DESC LIMIT 12;",
            "notifications" => @"SELECT baslik, bildirim_turu, onem_derecesi, okundu_mu, arsivlendi_mi, DATE_FORMAT(olusturulma_tarihi, '%d.%m.%Y %H:%i') FROM sistem_ici_bildirimler ORDER BY id DESC LIMIT 12;",
            "logs" => @"SELECT hedef_tablo, islem_turu, ip_adresi, DATE_FORMAT(islem_tarihi, '%d.%m.%Y %H:%i'), 'Admin Islem', '' FROM admin_islem_loglari ORDER BY id DESC LIMIT 6;",
            "email-templates" => @"SELECT sablon_adi, kategori, dil, aktif_mi, sistem_geneli_mi, konu_basligi FROM mesaj_sablonlari ORDER BY id DESC LIMIT 12;",
            "faq" => @"SELECT k.kategori_adi, s.soru, s.one_cikan_mi, s.aktif_mi, DATE_FORMAT(s.olusturulma_tarihi, '%d.%m.%Y') FROM sss_sorulari s INNER JOIN sss_kategorileri k ON k.id = s.sss_kategori_id ORDER BY k.siralama, s.siralama, s.id LIMIT 20;",
            _ => string.Empty
        };
    }

    private static int SafeInt(MySqlDataReader reader, int ordinal)
    {
        return reader.IsDBNull(ordinal) ? 0 : Convert.ToInt32(reader.GetValue(ordinal));
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
}

