/* ADMIN_YETKILER + ADMIN_ROLLER + ADMIN_ROL_YETKILER — idempotent seed (UTF-8) */
SET NOCOUNT ON;
GO

IF OBJECT_ID(N'dbo.ADMIN_YETKILER', N'U') IS NULL RETURN;

;WITH yetkiler AS (
    SELECT * FROM (VALUES
        (N'admin.dashboard', N'Panel Özeti', N'overview'),
        (N'admin.system_health', N'Sistem Sağlığı', N'system'),
        (N'admin.platform_checkup', N'Platform Checkup', N'system'),
        (N'admin.approval_center', N'Onay Merkezi', N'users'),
        (N'admin.hotels', N'Otel Yönetimi', N'hotels'),
        (N'admin.reviews', N'Değerlendirme Moderasyonu', N'hotels'),
        (N'admin.reservations', N'Rezervasyonlar', N'commerce'),
        (N'admin.unified_reservations', N'Rezervasyonlar (Tek Liste)', N'commerce'),
        (N'admin.company_reservations', N'Firma Rezervasyonları', N'commerce'),
        (N'admin.listing_subscriptions', N'Otel Liste Abonelikleri', N'commerce'),
        (N'admin.platform_packages', N'Platform Paket Satışı', N'commerce'),
        (N'admin.payments', N'Ödemeler', N'commerce'),
        (N'admin.invoices', N'Faturalar', N'commerce'),
        (N'admin.commissions', N'Komisyon Oranları', N'commerce'),
        (N'admin.contracts', N'Sözleşmeler', N'commerce'),
        (N'admin.reports', N'Gelir / Komisyon Raporu', N'commerce'),
        (N'admin.commerce_insight', N'Ticari İçgörü', N'commerce'),
        (N'admin.users', N'Kullanıcılar', N'users'),
        (N'admin.managers', N'Yöneticiler', N'users'),
        (N'admin.platform_officials', N'Platform Yetkilileri', N'users'),
        (N'admin.development_requests', N'Geliştirme Talepleri', N'users'),
        (N'admin.partner_applications', N'Partner Başvuruları', N'users'),
        (N'admin.company_applications', N'Firma Başvuruları', N'users'),
        (N'admin.notifications', N'Bildirimler', N'content'),
        (N'admin.mail_center', N'Mail Merkezi', N'content'),
        (N'admin.email_queue', N'E-posta Kuyruğu', N'content'),
        (N'admin.email_routing', N'E-posta Yönlendirmeleri', N'content'),
        (N'admin.email_templates', N'E-posta Hesapları / Şablonlar', N'content'),
        (N'admin.whatsapp', N'WhatsApp Cloud API', N'content'),
        (N'admin.blog', N'Blog Yönetimi', N'content'),
        (N'admin.faq', N'SSS Yönetimi', N'content'),
        (N'admin.support_articles', N'Destek / Yardım Merkezi', N'content'),
        (N'admin.settings', N'Ayarlar', N'system'),
        (N'admin.settings_monitor', N'Ayar Monitörü', N'system'),
        (N'admin.sitemap', N'Sitemap', N'system'),
        (N'admin.security', N'Güvenlik', N'system'),
        (N'admin.security_events', N'Güvenlik Olayları', N'system'),
        (N'admin.upload_history', N'Upload Geçmişi', N'system'),
        (N'admin.complaints', N'Şikayetler', N'system'),
        (N'admin.backups', N'Yedekleme', N'system'),
        (N'admin.logs', N'Log Kayıtları', N'system'),
        (N'admin.admin_action_logs', N'İşlem Logları', N'system'),
        (N'admin.geo_search_logs', N'Konum Arama Logları', N'system'),
        (N'admin.hotel_coord_changes', N'Otel Koordinat Değişimleri', N'system'),
        (N'admin.rate_limit', N'Rate Limit', N'system')
    ) AS v([YETKI_CODE], [YETKI_NAME], [GROUP_CODE])
)
INSERT INTO [dbo].[ADMIN_YETKILER] ([YETKI_CODE], [YETKI_NAME], [GROUP_CODE], [DESCRIPTION], [ACTIVE])
SELECT y.[YETKI_CODE], y.[YETKI_NAME], y.[GROUP_CODE], NULL, 1
FROM yetkiler y
WHERE NOT EXISTS (SELECT 1 FROM [dbo].[ADMIN_YETKILER] p WHERE p.[YETKI_CODE] = y.[YETKI_CODE]);
GO

IF OBJECT_ID(N'dbo.ADMIN_ROLLER', N'U') IS NULL RETURN;

IF NOT EXISTS (SELECT 1 FROM [dbo].[ADMIN_ROLLER] WHERE [ROL_CODE] = N'platform_admin_full')
BEGIN
    INSERT INTO [dbo].[ADMIN_ROLLER] ([ROL_CODE], [ROL_NAME], [DESCRIPTION], [ACTIVE])
    VALUES (N'platform_admin_full', N'Platform Admin (Tam)', N'Tüm admin panel yetkileri', 1);
END
GO

IF OBJECT_ID(N'dbo.ADMIN_ROL_YETKILER', N'U') IS NULL RETURN;

INSERT INTO [dbo].[ADMIN_ROL_YETKILER] ([ROL_CODE], [YETKI_CODE], [ACTIVE])
SELECT N'platform_admin_full', p.[YETKI_CODE], 1
FROM [dbo].[ADMIN_YETKILER] p
WHERE p.[ACTIVE] = 1
  AND NOT EXISTS (
      SELECT 1 FROM [dbo].[ADMIN_ROL_YETKILER] rp
      WHERE rp.[ROL_CODE] = N'platform_admin_full' AND rp.[YETKI_CODE] = p.[YETKI_CODE]
  );
GO
