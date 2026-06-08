namespace otelturizmnew.Models.Paneller.Admin;

/// <summary>
/// Admin sidebar ve RenderSectionAsync RBAC eşlemesinin tek kaynağı.
/// </summary>
public static class AdminNavigationCatalog
{
    public sealed record AdminNavItem(
        string Action,
        string Label,
        string Permission,
        string? SectionKey = null,
        string[]? ActiveActions = null,
        string? BadgeKey = null,
        string[]? Tables = null);

    public sealed record AdminNavGroup(
        string Label,
        string MenuTitle,
        string IconClass,
        string IconTone,
        IReadOnlyList<AdminNavItem> Items);

    public sealed record AdminSectionMeta(
        string SectionKey,
        string Title,
        string Subtitle,
        string[] Columns,
        string EmptyMessage,
        string ListSql,
        string Permission);

    private static readonly Dictionary<string, AdminSectionMeta> SectionMetaByKey =
        BuildSectionMeta().ToDictionary(x => x.SectionKey, StringComparer.OrdinalIgnoreCase);

    private static readonly Dictionary<string, string> PermissionBySectionKey =
        SectionMetaByKey.Values.ToDictionary(x => x.SectionKey, x => x.Permission, StringComparer.OrdinalIgnoreCase);

    public static IReadOnlyList<AdminNavGroup> GetGroups() => Groups;

    public static string? GetPermissionForSectionKey(string sectionKey)
    {
        if (string.IsNullOrWhiteSpace(sectionKey))
        {
            return null;
        }

        return PermissionBySectionKey.TryGetValue(sectionKey, out var permission) ? permission : null;
    }

    public static bool TryGetSectionMeta(string sectionKey, out AdminSectionMeta meta)
        => SectionMetaByKey.TryGetValue(sectionKey, out meta!);

    public static IEnumerable<string> GetAllMappedTables()
        => Groups.SelectMany(g => g.Items).SelectMany(i => i.Tables ?? Array.Empty<string>()).Distinct(StringComparer.OrdinalIgnoreCase);

    private static readonly IReadOnlyList<AdminNavGroup> Groups =
    [
        new("Konumlar", "Adres Hiyerarşisi", "fa-map-location-dot", "ic-core",
        [
            new("Countries", "Ülkeler", "admin.locations", "countries", ["Countries"], null, ["ULKELER"]),
            new("LocationsCities", "İller", "admin.locations", null, ["LocationsCities"], null, ["ILLER"]),
            new("LocationsDistricts", "İlçeler", "admin.locations", null, ["LocationsDistricts"], null, ["ILCELER"]),
            new("LocationsNeighborhoods", "Mahalleler", "admin.locations", null, ["LocationsNeighborhoods"], null, ["MAHALLELER"])
        ]),
        new("Otel & Envanter", "Tesis Yönetimi", "fa-hotel", "ic-inv",
        [
            new("Hotels", "Oteller (CRUD)", "admin.hotels", "hotels", ["Hotels", "HotelDetail"], null, ["OTELLER"]),
            new("ActiveHotels", "Açık Oteller", "admin.hotels", "active-hotels", ["ActiveHotels"], null, ["OTELLER"]),
            new("PendingHotels", "Onay Bekleyen Oteller", "admin.hotels", "pending-hotels", ["PendingHotels"], null, ["OTELLER"]),
            new("OtelTablosu", "Oteller Tablosu", "admin.hotels", "hotels", ["OtelTablosu"], null, ["OTELLER"]),
            new("OdaTipleri", "Oda Tipleri", "admin.hotels", "oda-tipleri", ["OdaTipleri"], null, ["ODA_TIPLERI"]),
            new("OtelGorselleri", "Otel Görselleri", "admin.hotels", "otel-gorselleri", ["OtelGorselleri"], null, ["OTEL_GORSELLERI"]),
            new("OdaGorselleri", "Oda Görselleri", "admin.hotels", "oda-gorselleri", ["OdaGorselleri"], null, ["ODA_GORSELLERI"]),
            new("OtelOzellikleri", "Otel Özellikleri", "admin.hotels", "otel-ozellikleri", ["OtelOzellikleri"], null, ["OTEL_OZELLIKLERI", "OTEL_OZELLIK_ILISKILERI", "OTEL_OZELLIK_KATEGORILERI"]),
            new("Campaigns", "Kampanyalar", "admin.hotels", "campaigns", ["Campaigns"], null, ["KAMPANYALAR", "KAMPANYA_OTELLER"]),
            new("Reviews", "Değerlendirmeler", "admin.reviews", "reviews", ["Reviews"], "PendingReviews", ["YORUMLAR", "YORUM_KALDIRMA_TALEPLERI"]),
            new("ListingSubscriptions", "Liste Abonelikleri", "admin.listing_subscriptions", "otel-liste-abonelikleri", ["ListingSubscriptions"], null, ["OTEL_LISTE_ABONELIKLERI"]),
            new("HotelCoordinateChanges", "Koordinat Değişimleri", "admin.hotel_coord_changes", "hotel-coordinate-changes", ["HotelCoordinateChanges"], null, ["OTEL_KOORDINAT_DEGISIM_LOGLARI"])
        ]),
        new("Yardım & SSS", "Destek İçerikleri", "fa-circle-question", "ic-set",
        [
            new("SssKategorileri", "SSS Kategorileri", "admin.faq", "sss-kategorileri", ["SssKategorileri"], null, ["SSS_KATEGORILERI"]),
            new("SssSorulari", "SSS Soruları", "admin.faq", "sss-sorulari", ["SssSorulari"], null, ["SSS_SORULARI"]),
            new("Faq", "SSS (Birleşik Görünüm)", "admin.faq", "faq", ["Faq"], null, ["SSS_KATEGORILERI", "SSS_SORULARI"]),
            new("YardimIcerikleri", "Yardım Merkezi İçerikleri", "admin.support_articles", "yardim-icerikleri", ["YardimIcerikleri"], null, ["YARDIM_MERKEZI_ICERIKLER"]),
            new("YardimKategoriDetay", "Yardım Kategori Detayları", "admin.support_articles", "yardim-kategori-detay", ["YardimKategoriDetay"], null, ["YARDIM_MERKEZI_KATEGORI_DETAYLARI"]),
            new("YardimKategoriSss", "Yardım Kategori SSS", "admin.support_articles", "yardim-kategori-sss", ["YardimKategoriSss"], null, ["YARDIM_MERKEZI_KATEGORI_SSS"]),
            new("DestekKanallari", "Destek Kanalları", "admin.support_articles", "destek-kanallari", ["DestekKanallari"], null, ["DESTEK_KANALLARI"]),
            new("DestekKategorileri", "Destek Kategorileri", "admin.support_articles", "destek-kategorileri", ["DestekKategorileri"], null, ["DESTEK_KATEGORILERI"]),
            new("SupportArticles", "Destek Makaleleri", "admin.support_articles", "destek-makaleleri", ["SupportArticles"], null, ["DESTEK_MAKALELERI"]),
            new("HelpCenter", "Yardım Merkezi Yönetim", "admin.support_articles", null, ["HelpCenter"], null, ["YARDIM_MERKEZI_ICERIKLER", "YARDIM_MERKEZI_KATEGORI_DETAYLARI", "YARDIM_MERKEZI_KATEGORI_SSS", "DESTEK_KATEGORILERI"])
        ]),
        new("Partner", "Partner Ağı", "fa-handshake-angle", "ic-chan",
        [
            new("PartnerApplications", "Partner Başvuruları", "admin.partner_applications", "partner-applications", ["PartnerApplications", "PartnerApplicationDetail"], "PendingPartnerApplications", ["PARTNER_DETAYLARI"]),
            new("PartnerDocuments", "Partner Evrakları", "admin.partner_applications", "partner-evraklari", ["PartnerDocuments"], null, ["PARTNER_BASVURU_EVRAKLARI", "PARTNER_BASVURU_HAREKETLERI", "PARTNER_EKSIK_EVRAK_TALEPLERI"]),
            new("PartnerDestekTalepleri", "Partner Destek Talepleri", "admin.partner_applications", "partner-destek-talepleri", ["PartnerDestekTalepleri"], null, ["PARTNER_DESTEK_TALEPLERI", "PARTNER_DESTEK_MESAJLARI"]),
            new("PartnerPanelTercihleri", "Partner Panel Tercihleri", "admin.partner_applications", "partner-panel-tercihleri", ["PartnerPanelTercihleri"], null, ["PARTNER_PANEL_TERCIHLERI"])
        ]),
        new("Firmalar", "Kurumsal & B2B", "fa-building", "ic-crm",
        [
            new("Companies", "Firmalar", "admin.companies", "companies", ["Companies"], null, ["FIRMALAR"]),
            new("CompanyApplications", "Firma Başvuruları", "admin.company_applications", "company-applications", ["CompanyApplications"], "PendingCompanyApplications", ["FIRMALAR", "FIRMA_BASVURU_HAREKETLERI"]),
            new("CompanyReservations", "Firma Rezervasyonları", "admin.company_reservations", "company-reservations", ["CompanyReservations"], null, ["REZERVASYONLAR", "FIRMA_REZERVASYONLARI", "FIRMA_CALISANLARI", "FIRMA_HARCAMA_LIMITLERI", "FIRMA_ODA_FIYAT_MUSAITLIK"])
        ]),
        new("Rezervasyon & Satış", "Satış Operasyonu", "fa-calendar-check", "ic-book",
        [
            new("UnifiedReservations", "Tüm Rezervasyonlar", "admin.unified_reservations", null, ["UnifiedReservations"], null, ["REZERVASYONLAR"]),
            new("Reservations", "Rezervasyon Listesi", "admin.reservations", "reservations", ["Reservations"], null, ["REZERVASYONLAR", "REZERVASYON_TASLAKLARI", "REZERVASYON_ODEME_KALEMLERI", "REZERVASYON_FATURALARI", "REZERVASYON_DURUM_TANIMLARI", "SEPET_BLOKAJLARI"])
        ]),
        new("Finans & Komisyon", "Gelir & Tahsilat", "fa-coins", "ic-fin",
        [
            new("RevenueCommandCenter", "Gelir Komuta Merkezi", "admin.reports", null, ["RevenueCommandCenter"], null, ["REZERVASYONLAR", "KOMISYON_MUHASEBE_KAYITLARI"]),
            new("Reports", "Gelir / Komisyon Raporu", "admin.reports", "reports", ["Reports"], null, ["REZERVASYONLAR", "KOMISYON_MUHASEBE_KAYITLARI"]),
            new("Commissions", "Komisyon Oranları", "admin.commissions", "commissions", ["Commissions"], null, ["KOMISYON_MUHASEBE_KAYITLARI", "KOMISYON_VERGILER"]),
            new("CommissionCollection", "Komisyon Tahsilat", "admin.commissions", null, ["CommissionCollection"], null, ["KOMISYON_MUHASEBE_KAYITLARI"]),
            new("Invoices", "Faturalar", "admin.invoices", "invoices", ["Invoices"], null, ["FATURALAR"]),
            new("Payments", "Ödemeler", "admin.payments", "payments", ["Payments"], null, ["ODEME_ISLEMLERI", "ODEME_YONTEMI_TANIMLARI", "ODEME_DURUMU_TANIMLARI", "BASARISIZ_ODEME_DENEMELERI"]),
            new("Contracts", "Sözleşmeler", "admin.contracts", null, ["Contracts"], null, ["SOZLESMELER", "SOZLESME_KABULLERI"]),
            new("CommerceInsight", "Ticari İçgörü", "admin.commerce_insight", null, ["CommerceInsight"], null, ["OTEL_RAKIP_ANALIZI"]),
            new("PlatformPackages", "Platform Paketleri", "admin.platform_packages", null, ["PlatformPackages"], null, ["PLATFORM_PAKETLER", "PLATFORM_PAKET_KATEGORILERI", "PARTNER_PAKET_BASVURULARI", "OTEL_UYUM_DURUMLARI"])
        ]),
        new("Platform Yönetimi", "Operasyon Merkezi", "fa-heart-pulse", "ic-core",
        [
            new("Dashboard", "Genel Bakış", "admin.dashboard", null, ["Dashboard"], null, ["OTELLER", "REZERVASYONLAR", "KULLANICILAR"]),
            new("SystemHealth", "Sistem Sağlığı", "admin.system_health", null, ["SystemHealth", "SlowSqlMonitor"], null, ["SISTEM_HATA_LOGLARI", "API_LOGLARI"]),
            new("PlatformCheckup", "Platform Checkup", "admin.platform_checkup", null, ["PlatformCheckup"], null, []),
            new("ApprovalCenter", "Onay Merkezi", "admin.approval_center", null, ["ApprovalCenter"], "PendingApprovals", ["OTELLER", "PARTNER_DETAYLARI", "FIRMALAR"]),
            new("PlatformDbStats", "Veritabanı İstatistikleri", "admin.platform_stats", "platform-db-stats", ["PlatformDbStats"], null, []),
            new("EmailQueue", "E-posta Kuyruğu", "admin.email_queue", null, ["EmailQueue"], null, ["PLATFORM_EPOSTA_MESAJLARI"]),
            new("GeoSearchLogs", "Konum Arama Logları", "admin.geo_search_logs", "geo-search-logs", ["GeoSearchLogs"], null, ["KULLANICI_KONUM_LOGLARI"]),
            new("RateLimitStats", "Rate Limit", "admin.rate_limit", null, ["RateLimitStats"], null, []),
            new("AdminActionLogs", "İşlem Logları", "admin.admin_action_logs", null, ["AdminActionLogs"], null, ["ADMIN_ISLEM_LOGLARI"]),
            new("Logs", "Log Kayıtları", "admin.logs", "logs", ["Logs"], "CriticalLogs", ["ADMIN_ISLEM_LOGLARI", "SISTEM_HATA_LOGLARI", "API_LOGLARI", "KULLANICI_AKTIVITE_LOGLARI", "KULLANICI_GIRIS_LOGLARI"]),
            new("UploadHistory", "Upload Geçmişi", "admin.upload_history", null, ["UploadHistory"], null, ["GUVENLI_DOSYA_VARLIKLARI"]),
            new("SecurityEvents", "Güvenlik Olayları", "admin.security_events", null, ["SecurityEvents"], null, ["BLOCKYORUMKELIME"])
        ]),
        new("Kullanıcı & CRM", "Hesap Yönetimi", "fa-users", "ic-crm",
        [
            new("Users", "Kullanıcılar (B2C)", "admin.users", "users", ["Users"], null, ["KULLANICILAR", "KULLANICILAR_ARSIV_YEDEK"]),
            new("Managers", "Yönetici Hesapları", "admin.managers", "managers", ["Managers"], null, ["KULLANICILAR", "DEPARTMANLAR", "KULLANICI_DEPARTMAN"]),
            new("PlatformOfficials", "Platform Yetkilileri", "admin.platform_officials", "platform-officials", ["PlatformOfficials"], null, ["KULLANICILAR"]),
            new("AdminRoles", "Roller", "admin.roles", "roles", ["AdminRoles"], null, ["ROLLER", "ROL_YETKILERI", "KULLANICI_ROLLERI"]),
            new("AdminRbacRoles", "Admin Panel Rolleri", "admin.roles", "admin-rbac-roles", ["AdminRbacRoles"], null, ["ADMIN_ROLLER", "ADMIN_YETKILER", "ADMIN_ROL_YETKILER", "ADMIN_KULLANICI_ROLLER"]),
            new("DevelopmentRequests", "Geliştirme Talepleri", "admin.development_requests", null, ["DevelopmentRequests"], null, ["GELISTIRME_TALEPLERI", "DEVELOPER_BILDIRIMLERI"]),
            new("SadakatSeviyeleri", "Sadakat Seviyeleri", "admin.users", "sadakat-seviyeleri", ["SadakatSeviyeleri"], null, ["SADAKAT_SEVIYELERI", "SADAKAT_ODULLERI", "KULLANICI_SADAKAT_HESAPLARI", "KULLANICI_PUAN_HAREKETLERI"]),
            new("KullaniciFavoriler", "Favoriler & Alarmlar", "admin.users", "kullanici-favoriler", ["KullaniciFavoriler"], null, ["KULLANICI_FAVORI_OTELLER", "KULLANICI_FAVORI_FIYAT_ALARMLARI", "KULLANICI_FAVORI_FIYAT_ALARM_ISLERI"]),
            new("SeyahatPlanlari", "Seyahat Planları", "admin.users", "seyahat-planlari", ["SeyahatPlanlari"], null, ["KULLANICI_SEYAHAT_PLANLARI", "KULLANICI_SEYAHAT_PLAN_OTEL_SECIMLERI", "KULLANICI_BUTCE_PLANLARI"])
        ]),
        new("İletişim & E-posta", "Mesajlaşma", "fa-envelope", "ic-set",
        [
            new("MailCenter", "Mail Merkezi", "admin.mail_center", null, ["MailCenter"], null, ["PLATFORM_EPOSTA_HESAPLARI"]),
            new("EmailRouting", "E-posta Yönlendirmeleri", "admin.email_routing", null, ["EmailRouting"], null, ["ADMIN_EPOSTA_YONLENDIRME"]),
            new("EmailTemplates", "E-posta Şablonları", "admin.email_templates", "email-templates", ["EmailTemplates"], null, ["MESAJ_SABLONLARI", "BILDIRIM_SABLONLARI", "EPOSTA_SERVISLERI"]),
            new("WhatsAppCloudApi", "WhatsApp Cloud API", "admin.whatsapp", null, ["WhatsAppCloudApi"], null, ["WHATSAPP_CLOUD_API_AYARLARI", "WHATSAPP_MESAJ_LOGLARI"]),
            new("MesajMerkezi", "Mesaj Merkezi", "admin.notifications", "mesaj-merkezi", ["MesajMerkezi"], null, ["MESAJ_KONUSMALARI", "MESAJLAR", "MESAJ_DOSYALARI", "DIS_KUTU_MESAJLARI"]),
            new("Notifications", "Bildirimler", "admin.notifications", "notifications", ["Notifications"], "UnreadNotifications", ["SISTEM_ICI_BILDIRIMLER", "BILDIRIM_LOGLARI", "KULLANICI_BILDIRIM_TERCIHLERI", "KULLANICI_BILDIRIM_CIHAZLARI", "PANEL_HEADER_BILDIRI_OKUMALARI"])
        ]),
        new("Sistem Konfigürasyonu", "Ayarlar & İçerik", "fa-sliders", "ic-set",
        [
            new("Settings", "Genel Ayarlar", "admin.settings", "settings", ["Settings"], null, ["TEMA_PANEL", "OZEL_GUNLER"]),
            new("SettingsMonitor", "Ayar Monitörü", "admin.settings_monitor", null, ["SettingsMonitor"], null, ["SISTEM_DIYAGRAMLARI"]),
            new("Security", "Güvenlik Politikaları", "admin.security", "security", ["Security"], null, []),
            new("Sitemap", "Sitemap XML", "admin.sitemap", null, ["Sitemap"], null, []),
            new("Blog", "Blog Yönetimi", "admin.blog", "blog", ["Blog"], null, []),
            new("Team", "Platform Ekibi", "admin.notifications", null, ["Team"], null, ["PLATFORM_EKIP_UYELERI"]),
            new("Complaints", "Şikayet Yönetimi", "admin.complaints", "complaints", ["Complaints"], null, []),
            new("Backups", "Yedekleme", "admin.backups", "backups", ["Backups"], null, []),
            new("SatisMusterileri", "Satış Müşterileri", "admin.users", "satis-musterileri", ["SatisMusterileri"], null, ["SATIS_MUSTERILERI", "SATIS_MUSTERI_NOTLARI"]),
            new("RozetTanimlari", "Rozet Tanımları", "admin.users", "rozet-tanimlari", ["RozetTanimlari"], null, ["ROZET_TANIMLARI", "KULLANICI_ROZETLERI"])
        ])
    ];

    private static IEnumerable<AdminSectionMeta> BuildSectionMeta()
    {
        yield return Meta("countries", "Ülkeler", "Platform adres hiyerarşisinin ülke kayıtları.", ["Ülke", "ISO2", "ISO3", "Para Birimi", "Varsayılan", "Durum"], "Ülke kaydı bulunamadı.", "admin.locations",
            @"SELECT TOP (80) [ULKE_ADI], COALESCE([ISO2_KODU],'-'), COALESCE([ISO3_KODU],'-'), COALESCE([PARA_BIRIMI_KODU],'-'), CASE WHEN [VARSAYILAN_ULKE]=1 THEN N'Evet' ELSE N'Hayır' END, CASE WHEN [AKTIF_MI]=1 THEN N'Aktif' ELSE N'Pasif' END FROM [dbo].[ULKELER] ORDER BY [VARSAYILAN_ULKE] DESC, [ULKE_ADI];");

        yield return Meta("hotels", "Oteller Tablosu", "OTELLER tablosu kayıtlarını listeleyin.", ["Otel", "Konum", "Tür", "Yayın", "Onay", "Puan"], "Otel kaydı bulunamadı.", "admin.hotels",
            @"SELECT TOP (40) [OTEL_ADI], CONCAT([ILCE], ', ', [SEHIR]), [OTEL_TURU], [YAYIN_DURUMU], [ONAY_DURUMU], FORMAT([ORTALAMA_PUAN], '0.0', 'tr-TR') FROM [dbo].[OTELLER] ORDER BY id DESC;");

        yield return Meta("sss-kategorileri", "SSS Kategorileri", "SSS_KATEGORILERI tablosu.", ["Kategori", "Slug", "İkon", "Sıra", "Aktif", "Güncelleme"], "Kategori bulunamadı.", "admin.faq",
            @"SELECT TOP (80) [KATEGORI_ADI], [SEO_SLUG], [IKON], CAST([SIRALAMA] AS nvarchar(10)), CASE WHEN [AKTIF_MI]=1 THEN N'Aktif' ELSE N'Pasif' END, FORMAT([GUNCELLENME_TARIHI], 'dd.MM.yyyy', 'tr-TR') FROM [dbo].[SSS_KATEGORILERI] ORDER BY [SIRALAMA], id;");

        yield return Meta("sss-sorulari", "SSS Soruları", "SSS_SORULARI tablosu.", ["Soru", "Öne Çıkan", "Aktif", "Sıra", "Oluşturma"], "Soru bulunamadı.", "admin.faq",
            @"SELECT TOP (80) LEFT([SORU], 120), CASE WHEN [ONE_CIKAN_MI]=1 THEN N'Evet' ELSE N'Hayır' END, CASE WHEN [AKTIF_MI]=1 THEN N'Aktif' ELSE N'Pasif' END, CAST([SIRALAMA] AS nvarchar(10)), FORMAT([OLUSTURULMA_TARIHI], 'dd.MM.yyyy', 'tr-TR') FROM [dbo].[SSS_SORULARI] ORDER BY [SIRALAMA], id DESC;");

        yield return Meta("yardim-icerikleri", "Yardım Merkezi İçerikleri", "YARDIM_MERKEZI_ICERIKLER tablosu.", ["Tür", "Başlık", "Slug", "Öne Çıkan", "Aktif", "Sıra"], "İçerik bulunamadı.", "admin.support_articles",
            @"SELECT TOP (80) [ICERIK_TURU], [BASLIK], [SEO_SLUG], CASE WHEN [ONE_CIKAN_MI]=1 THEN N'Evet' ELSE N'Hayır' END, CASE WHEN [AKTIF_MI]=1 THEN N'Aktif' ELSE N'Pasif' END, CAST([SIRALAMA] AS nvarchar(10)) FROM [dbo].[YARDIM_MERKEZI_ICERIKLER] ORDER BY [SIRALAMA], id DESC;");

        yield return Meta("yardim-kategori-detay", "Yardım Kategori Detayları", "YARDIM_MERKEZI_KATEGORI_DETAYLARI tablosu.", ["Kategori ID", "Hero Başlık", "Aktif", "Güncelleme"], "Detay kaydı bulunamadı.", "admin.support_articles",
            @"SELECT TOP (80) CAST([DESTEK_KATEGORI_ID] AS nvarchar(20)), COALESCE([HERO_BASLIK],'-'), CASE WHEN [AKTIF_MI]=1 THEN N'Aktif' ELSE N'Pasif' END, FORMAT(COALESCE([GUNCELLENME_TARIHI],[OLUSTURULMA_TARIHI]), 'dd.MM.yyyy', 'tr-TR') FROM [dbo].[YARDIM_MERKEZI_KATEGORI_DETAYLARI] ORDER BY id DESC;");

        yield return Meta("yardim-kategori-sss", "Yardım Kategori SSS", "YARDIM_MERKEZI_KATEGORI_SSS tablosu.", ["Kategori ID", "Soru", "Aktif", "Sıra"], "Kayıt bulunamadı.", "admin.support_articles",
            @"SELECT TOP (80) CAST([DESTEK_KATEGORI_ID] AS nvarchar(20)), LEFT([SORU], 120), CASE WHEN [AKTIF_MI]=1 THEN N'Aktif' ELSE N'Pasif' END, CAST([SIRALAMA] AS nvarchar(10)) FROM [dbo].[YARDIM_MERKEZI_KATEGORI_SSS] ORDER BY [SIRALAMA], id DESC;");

        yield return Meta("destek-kanallari", "Destek Kanalları", "DESTEK_KANALLARI tablosu.", ["Kanal", "Tür", "Buton", "URL", "Aktif", "Sıra"], "Kanal bulunamadı.", "admin.support_articles",
            @"SELECT TOP (80) [KANAL_ADI], [KANAL_TURU], [BUTON_METIN], LEFT([BAGLANTI_URL], 60), CASE WHEN [AKTIF_MI]=1 THEN N'Aktif' ELSE N'Pasif' END, CAST([SIRALAMA] AS nvarchar(10)) FROM [dbo].[DESTEK_KANALLARI] ORDER BY [SIRALAMA], id;");

        yield return Meta("destek-kategorileri", "Destek Kategorileri", "DESTEK_KATEGORILERI tablosu.", ["Kategori", "Slug", "Aktif", "Sıra"], "Kategori bulunamadı.", "admin.support_articles",
            @"SELECT TOP (80) [KATEGORI_ADI], [SEO_SLUG], CASE WHEN [AKTIF_MI]=1 THEN N'Aktif' ELSE N'Pasif' END, CAST([SIRALAMA] AS nvarchar(10)) FROM [dbo].[DESTEK_KATEGORILERI] ORDER BY [SIRALAMA], id;");

        yield return Meta("destek-makaleleri", "Destek Makaleleri Tablosu", "DESTEK_MAKALELERI tablosu.", ["Başlık", "Slug", "Durum", "Yardım Merkezi", "Güncelleme"], "Makale bulunamadı.", "admin.support_articles",
            @"SELECT TOP (80) [BASLIK], [SEO_SLUG], COALESCE([DURUM],'-'), CASE WHEN [YARDIM_MERKEZINDE_GOSTER]=1 THEN N'Evet' ELSE N'Hayır' END, FORMAT(COALESCE([GUNCELLENME_TARIHI],[OLUSTURULMA_TARIHI]), 'dd.MM.yyyy', 'tr-TR') FROM [dbo].[DESTEK_MAKALELERI] ORDER BY id DESC;");

        yield return Meta("roles", "Roller", "Platform rolleri.", ["Rol Kodu", "Rol Adı", "Departman", "Seviye", "Varsayılan", "Açıklama"], "Rol kaydı bulunamadı.", "admin.roles",
            @"SELECT TOP (80) [ROL_KODU], [ROL_ADI], COALESCE([DEPARTMAN],'-'), CAST(COALESCE([SEVIYE],0) AS nvarchar(10)), CASE WHEN [VARSAYILAN_MI]=1 THEN N'Evet' ELSE N'Hayır' END, COALESCE([ACIKLAMA],'-') FROM [dbo].[ROLLER] ORDER BY [SEVIYE], [ROL_ADI];");

        yield return Meta("admin-rbac-roles", "Admin Panel Rolleri", "ADMIN_ROLLER tablosu.", ["Rol Kodu", "Rol Adı", "Açıklama", "Durum"], "Admin rol kaydı bulunamadı.", "admin.roles",
            @"SELECT TOP (40) [ROL_CODE], [ROL_NAME], COALESCE([DESCRIPTION],'-'), CASE WHEN [ACTIVE]=1 THEN N'Aktif' ELSE N'Pasif' END FROM [dbo].[ADMIN_ROLLER] ORDER BY [ROL_CODE];");

        yield return Meta("companies", "Firmalar", "FIRMALAR tablosu.", ["Firma", "Onay", "Kullanıcı", "Rezervasyon", "Kayıt"], "Firma kaydı bulunamadı.", "admin.companies",
            @"SELECT TOP (80) f.[FIRMA_ADI], COALESCE(f.[ONAY_DURUMU], 'Beklemede'), (SELECT COUNT(*) FROM [dbo].[KULLANICILAR] u WHERE u.[FIRMA_ID] = f.id AND u.[ROL] LIKE 'firma_%'), (SELECT COUNT(*) FROM [dbo].[REZERVASYONLAR] r WHERE r.[FIRMA_ID] = f.id), FORMAT(f.[OLUSTURULMA_TARIHI], 'dd.MM.yyyy', 'tr-TR') FROM [dbo].[FIRMALAR] f ORDER BY f.id DESC;");

        yield return Meta("platform-db-stats", "Veritabanı İstatistikleri", "Tablo satır sayıları.", ["Tablo", "Satır Sayısı", "Şema"], "Tablo istatistiği bulunamadı.", "admin.platform_stats",
            @"SELECT TOP (60) t.[name], CAST(SUM(p.[rows]) AS nvarchar(30)), SCHEMA_NAME(t.[schema_id]) FROM sys.tables t INNER JOIN sys.partitions p ON t.[object_id] = p.[object_id] WHERE t.is_ms_shipped = 0 AND p.index_id IN (0, 1) GROUP BY t.[name], t.[schema_id] ORDER BY SUM(p.[rows]) DESC;");

        yield return Meta("oda-tipleri", "Oda Tipleri", "ODA_TIPLERI tablosu.", ["Otel ID", "Oda Adı", "Kapasite", "Aktif", "Güncelleme"], "Oda tipi bulunamadı.", "admin.hotels",
            @"SELECT TOP (80) CAST([OTEL_ID] AS nvarchar(20)), [ODA_ADI], CAST(COALESCE([MAKSIMUM_KISI],0) AS nvarchar(10)), CASE WHEN [AKTIF_MI]=1 THEN N'Aktif' ELSE N'Pasif' END, FORMAT(COALESCE([GUNCELLENME_TARIHI],[OLUSTURULMA_TARIHI]), 'dd.MM.yyyy', 'tr-TR') FROM [dbo].[ODA_TIPLERI] ORDER BY id DESC;");

        yield return Meta("otel-gorselleri", "Otel Görselleri", "OTEL_GORSELLERI tablosu.", ["Otel ID", "Tür", "Sıra", "Kapak", "Yol"], "Görsel bulunamadı.", "admin.hotels",
            @"SELECT TOP (80) CAST([OTEL_ID] AS nvarchar(20)), COALESCE([FOTO_TURU],'-'), CAST(COALESCE([SIRALAMA],0) AS nvarchar(10)), CASE WHEN [KAPAK_FOTOGRAFI]=1 THEN N'Evet' ELSE N'Hayır' END, LEFT(COALESCE([DOSYA_YOLU],'-'), 80) FROM [dbo].[OTEL_GORSELLERI] ORDER BY id DESC;");

        yield return Meta("oda-gorselleri", "Oda Görselleri", "ODA_GORSELLERI tablosu.", ["Oda Tip ID", "Sıra", "Kapak", "Yol"], "Görsel bulunamadı.", "admin.hotels",
            @"SELECT TOP (80) CAST([ODA_TIP_ID] AS nvarchar(20)), CAST(COALESCE([SIRALAMA],0) AS nvarchar(10)), CASE WHEN [KAPAK_FOTOGRAFI]=1 THEN N'Evet' ELSE N'Hayır' END, LEFT(COALESCE([DOSYA_YOLU],'-'), 80) FROM [dbo].[ODA_GORSELLERI] ORDER BY id DESC;");

        yield return Meta("otel-ozellikleri", "Otel Özellik İlişkileri", "OTEL_OZELLIK_ILISKILERI tablosu.", ["Otel ID", "Özellik ID", "Ücretli", "Fiyat"], "Kayıt bulunamadı.", "admin.hotels",
            @"SELECT TOP (80) CAST([OTEL_ID] AS nvarchar(20)), CAST([OTEL_OZELLIK_ID] AS nvarchar(20)), CASE WHEN [EK_UCRETLI_MI]=1 THEN N'Evet' ELSE N'Hayır' END, COALESCE(FORMAT([EK_UCRET_TUTARI], 'N0', 'tr-TR'),'-') FROM [dbo].[OTEL_OZELLIK_ILISKILERI] ORDER BY id DESC;");

        yield return Meta("otel-liste-abonelikleri", "Otel Liste Abonelikleri", "OTEL_LISTE_ABONELIKLERI tablosu.", ["Otel ID", "Plan", "Durum", "Bitiş"], "Abonelik bulunamadı.", "admin.listing_subscriptions",
            @"SELECT TOP (80) CAST([OTEL_ID] AS nvarchar(20)), COALESCE([PLAN_ADI],'-'), COALESCE([DURUM],'-'), FORMAT(COALESCE([BITIS_TARIHI], [BASLANGIC_TARIHI]), 'dd.MM.yyyy', 'tr-TR') FROM [dbo].[OTEL_LISTE_ABONELIKLERI] ORDER BY id DESC;");

        yield return Meta("partner-evraklari", "Partner Başvuru Evrakları", "PARTNER_BASVURU_EVRAKLARI tablosu.", ["Partner ID", "Evrak", "Durum", "Tarih"], "Evrak bulunamadı.", "admin.partner_applications",
            @"SELECT TOP (80) CAST([PARTNER_ID] AS nvarchar(20)), COALESCE([EVRAK_TURU],'-'), COALESCE([ONAY_DURUMU],'-'), FORMAT(COALESCE([YUKLEME_TARIHI], [OLUSTURULMA_TARIHI]), 'dd.MM.yyyy', 'tr-TR') FROM [dbo].[PARTNER_BASVURU_EVRAKLARI] ORDER BY id DESC;");

        yield return Meta("partner-destek-talepleri", "Partner Destek Talepleri", "PARTNER_DESTEK_TALEPLERI tablosu.", ["Partner ID", "Konu", "Durum", "Öncelik", "Tarih"], "Talep bulunamadı.", "admin.partner_applications",
            @"SELECT TOP (80) CAST([PARTNER_ID] AS nvarchar(20)), LEFT(COALESCE([KONU],'-'), 80), COALESCE([DURUM],'-'), COALESCE([ONCELIK],'-'), FORMAT([OLUSTURULMA_TARIHI], 'dd.MM.yyyy', 'tr-TR') FROM [dbo].[PARTNER_DESTEK_TALEPLERI] ORDER BY id DESC;");

        yield return Meta("partner-panel-tercihleri", "Partner Panel Tercihleri", "PARTNER_PANEL_TERCIHLERI tablosu.", ["Partner ID", "Anahtar", "Değer"], "Tercih bulunamadı.", "admin.partner_applications",
            @"SELECT TOP (80) CAST([PARTNER_ID] AS nvarchar(20)), COALESCE([TERCIH_ANAHTARI],'-'), LEFT(COALESCE(CAST([TERCIH_DEGERI] AS nvarchar(200)),'-'), 120) FROM [dbo].[PARTNER_PANEL_TERCIHLERI] ORDER BY id DESC;");

        yield return Meta("sadakat-seviyeleri", "Sadakat Seviyeleri", "SADAKAT_SEVIYELERI tablosu.", ["Seviye", "Min Puan", "Max Puan", "Aktif"], "Seviye bulunamadı.", "admin.users",
            @"SELECT TOP (40) COALESCE([SEVIYE_ADI],'-'), CAST(COALESCE([MIN_PUAN],0) AS nvarchar(20)), CAST(COALESCE([MAX_PUAN],0) AS nvarchar(20)), CASE WHEN [AKTIF_MI]=1 THEN N'Aktif' ELSE N'Pasif' END FROM [dbo].[SADAKAT_SEVIYELERI] ORDER BY [MIN_PUAN];");

        yield return Meta("kullanici-favoriler", "Kullanıcı Favori Oteller", "KULLANICI_FAVORI_OTELLER tablosu.", ["Kullanıcı ID", "Otel ID", "Tarih"], "Favori bulunamadı.", "admin.users",
            @"SELECT TOP (80) CAST([KULLANICI_ID] AS nvarchar(20)), CAST([OTEL_ID] AS nvarchar(20)), FORMAT([OLUSTURULMA_TARIHI], 'dd.MM.yyyy', 'tr-TR') FROM [dbo].[KULLANICI_FAVORI_OTELLER] ORDER BY id DESC;");

        yield return Meta("seyahat-planlari", "Seyahat Planları", "KULLANICI_SEYAHAT_PLANLARI tablosu.", ["Kullanıcı ID", "Plan Adı", "Durum", "Tarih"], "Plan bulunamadı.", "admin.users",
            @"SELECT TOP (80) CAST([KULLANICI_ID] AS nvarchar(20)), LEFT(COALESCE([PLAN_ADI],'-'), 80), COALESCE([DURUM],'-'), FORMAT([OLUSTURULMA_TARIHI], 'dd.MM.yyyy', 'tr-TR') FROM [dbo].[KULLANICI_SEYAHAT_PLANLARI] ORDER BY id DESC;");

        yield return Meta("mesaj-merkezi", "Mesaj Konuşmaları", "MESAJ_KONUSMALARI tablosu.", ["Konu", "Durum", "Kaynak", "Tarih"], "Konuşma bulunamadı.", "admin.notifications",
            @"SELECT TOP (80) LEFT(COALESCE([KONU],'-'), 80), COALESCE([DURUM],'-'), COALESCE([KAYNAK],'-'), FORMAT([OLUSTURULMA_TARIHI], 'dd.MM.yyyy', 'tr-TR') FROM [dbo].[MESAJ_KONUSMALARI] ORDER BY id DESC;");

        yield return Meta("satis-musterileri", "Satış Müşterileri", "SATIS_MUSTERILERI tablosu.", ["Ad", "E-posta", "Telefon", "Durum"], "Müşteri bulunamadı.", "admin.users",
            @"SELECT TOP (80) COALESCE([AD_SOYAD],'-'), COALESCE([EPOSTA],'-'), COALESCE([TELEFON],'-'), COALESCE([DURUM],'-') FROM [dbo].[SATIS_MUSTERILERI] ORDER BY id DESC;");

        yield return Meta("rozet-tanimlari", "Rozet Tanımları", "ROZET_TANIMLARI tablosu.", ["Kod", "Ad", "Aktif"], "Rozet bulunamadı.", "admin.users",
            @"SELECT TOP (40) COALESCE([ROZET_KODU],'-'), COALESCE([ROZET_ADI],'-'), CASE WHEN [AKTIF_MI]=1 THEN N'Aktif' ELSE N'Pasif' END FROM [dbo].[ROZET_TANIMLARI] ORDER BY id;");
    }

    private static AdminSectionMeta Meta(string key, string title, string subtitle, string[] columns, string empty, string permission, string sql)
        => new(key, title, subtitle, columns, empty, sql, permission);
}
