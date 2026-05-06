/*
  Admin RBAC (Role-Based Access Control)
  - admin_roles
  - admin_permissions
  - admin_role_permissions
  - admin_user_roles

  Notlar:
  - Script idempotent (IF OBJECT_ID / COL_LENGTH guard).
  - Mevcut "user_role=admin" kullanan sistemle uyum için uygulama tarafında fallback vardır.
*/

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.admin_roles', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.admin_roles
    (
        role_code NVARCHAR(64) NOT NULL CONSTRAINT PK_admin_roles PRIMARY KEY,
        role_name NVARCHAR(128) NOT NULL,
        description NVARCHAR(256) NULL,
        active BIT NOT NULL CONSTRAINT DF_admin_roles_active DEFAULT (1),
        created_utc DATETIME2(0) NOT NULL CONSTRAINT DF_admin_roles_created DEFAULT (SYSUTCDATETIME())
    );
END
GO

IF OBJECT_ID(N'dbo.admin_permissions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.admin_permissions
    (
        permission_code NVARCHAR(64) NOT NULL CONSTRAINT PK_admin_permissions PRIMARY KEY,
        permission_name NVARCHAR(128) NOT NULL,
        group_code NVARCHAR(64) NOT NULL CONSTRAINT DF_admin_permissions_group DEFAULT (N'general'),
        description NVARCHAR(256) NULL,
        active BIT NOT NULL CONSTRAINT DF_admin_permissions_active DEFAULT (1),
        created_utc DATETIME2(0) NOT NULL CONSTRAINT DF_admin_permissions_created DEFAULT (SYSUTCDATETIME())
    );
END
GO

IF OBJECT_ID(N'dbo.admin_role_permissions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.admin_role_permissions
    (
        role_code NVARCHAR(64) NOT NULL,
        permission_code NVARCHAR(64) NOT NULL,
        active BIT NOT NULL CONSTRAINT DF_admin_role_permissions_active DEFAULT (1),
        created_utc DATETIME2(0) NOT NULL CONSTRAINT DF_admin_role_permissions_created DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_admin_role_permissions PRIMARY KEY (role_code, permission_code),
        CONSTRAINT FK_admin_rp_role FOREIGN KEY (role_code) REFERENCES dbo.admin_roles(role_code),
        CONSTRAINT FK_admin_rp_perm FOREIGN KEY (permission_code) REFERENCES dbo.admin_permissions(permission_code)
    );
END
GO

IF OBJECT_ID(N'dbo.admin_user_roles', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.admin_user_roles
    (
        admin_user_id BIGINT NOT NULL,
        role_code NVARCHAR(64) NOT NULL,
        active BIT NOT NULL CONSTRAINT DF_admin_user_roles_active DEFAULT (1),
        created_utc DATETIME2(0) NOT NULL CONSTRAINT DF_admin_user_roles_created DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_admin_user_roles PRIMARY KEY (admin_user_id, role_code),
        CONSTRAINT FK_admin_ur_role FOREIGN KEY (role_code) REFERENCES dbo.admin_roles(role_code)
    );
END
GO

-- Seed roles
MERGE dbo.admin_roles AS t
USING (VALUES
    (N'superadmin', N'Süper Admin', N'Tüm yetkiler'),
    (N'content',    N'İçerik',      N'Blog/SSS/Destek içerikleri'),
    (N'support',    N'Destek',      N'Destek talepleri ve şikayetler'),
    (N'finance',    N'Finans',      N'Ödeme/fatura/komisyon/raporlar'),
    (N'ops',        N'Operasyon',   N'Rezervasyon/otel operasyonları'),
    (N'security',   N'Güvenlik',    N'Loglar, güvenlik olayları, ayarlar')
) AS s(role_code, role_name, description)
ON t.role_code = s.role_code
WHEN MATCHED THEN UPDATE SET role_name = s.role_name, description = s.description, active = 1
WHEN NOT MATCHED THEN INSERT(role_code, role_name, description, active) VALUES(s.role_code, s.role_name, s.description, 1);
GO

-- Seed permissions (menu + endpoint grupları)
MERGE dbo.admin_permissions AS t
USING (VALUES
    (N'admin.dashboard',              N'Panel Özeti',                 N'general',  N'KPI ve özet ekranı'),
    (N'admin.system_health',          N'Sistem Sağlığı',              N'system',   N'Sistem metrikleri ve kontroller'),
    (N'admin.platform_checkup',       N'Platform Checkup',            N'system',   N'Kontrol listeleri'),
    (N'admin.approval_center',        N'Onay Merkezi',                N'ops',      N'Onay süreçleri'),
    (N'admin.hotels',                 N'Oteller',                     N'ops',      N'Otel yönetimi'),
    (N'admin.reviews',                N'Değerlendirmeler',            N'ops',      N'Review moderasyonu'),
    (N'admin.reservations',           N'Rezervasyonlar',              N'ops',      N'Rezervasyon listeleri'),
    (N'admin.unified_reservations',   N'Rezervasyonlar (Tek Liste)',  N'ops',      N'Tek birleşik liste'),
    (N'admin.company_reservations',   N'Firma Rezervasyonları',       N'ops',      N'Kurumsal rezervasyonlar'),
    (N'admin.payments',               N'Ödemeler',                    N'finance',  N'Ödeme kayıtları'),
    (N'admin.invoices',               N'Faturalar',                   N'finance',  N'Fatura kayıtları'),
    (N'admin.commissions',            N'Komisyonlar',                 N'finance',  N'Komisyon kuralları ve özet'),
    (N'admin.contracts',              N'Sözleşmeler',                 N'finance',  N'Sözleşme yönetimi'),
    (N'admin.reports',                N'Gelir/Komisyon Raporu',       N'finance',  N'Rapor ekranları'),
    (N'admin.commerce_insight',       N'Ticari içgörü',               N'finance',  N'Growth/insight ekranı'),
    (N'admin.users',                  N'Kullanıcılar',                N'users',    N'Üye yönetimi'),
    (N'admin.managers',               N'Yöneticiler',                 N'users',    N'Yönetici yönetimi'),
    (N'admin.platform_officials',     N'Platform Yetkilileri',        N'users',    N'Platform ekip listesi'),
    (N'admin.development_requests',   N'Geliştirme Talepleri',        N'content',  N'Geliştirme backlog'),
    (N'admin.partner_applications',   N'Partner Başvuruları',         N'ops',      N'Partner başvuruları'),
    (N'admin.company_applications',   N'Firma Başvuruları',           N'ops',      N'Firma başvuruları'),
    (N'admin.notifications',          N'Bildirimler',                 N'content',  N'Sistem içi bildirimler'),
    (N'admin.mail_center',            N'Mail Merkezi',                N'content',  N'IMAP/SMTP hesapları'),
    (N'admin.email_queue',            N'E-posta Kuyruğu',             N'system',   N'E-posta kuyruk yönetimi'),
    (N'admin.email_routing',          N'E-posta Yönlendirmeleri',     N'content',  N'Olay bazlı yönlendirmeler'),
    (N'admin.email_templates',        N'E-posta Hesapları',           N'content',  N'Servise bağlı şablon/hesaplar'),
    (N'admin.whatsapp',               N'WhatsApp Cloud API',          N'content',  N'WhatsApp ayarları'),
    (N'admin.blog',                   N'Blog Yönetimi',               N'content',  N'Blog içerikleri'),
    (N'admin.faq',                    N'SSS Yönetimi',                N'content',  N'SSS içerikleri'),
    (N'admin.support_articles',       N'Destek Makaleleri',           N'content',  N'Yardım merkezi makaleleri'),
    (N'admin.settings',               N'Ayarlar',                     N'system',   N'Platform ayarları'),
    (N'admin.settings_monitor',       N'Ayar Monitörü',               N'system',   N'Ayar drift izleme'),
    (N'admin.security',               N'Güvenlik',                    N'security', N'Güvenlik ekranı'),
    (N'admin.security_events',        N'Güvenlik Olayları',           N'security', N'Log event okuma'),
    (N'admin.upload_history',         N'Upload Geçmişi',              N'security', N'Upload audit okuma'),
    (N'admin.complaints',             N'Şikayetler',                  N'support',  N'Şikayet yönetimi'),
    (N'admin.backups',                N'Yedekleme',                   N'system',   N'Yedek ekranı'),
    (N'admin.logs',                   N'Log Kayıtları',               N'security', N'Uygulama logları'),
    (N'admin.admin_action_logs',      N'İşlem Logları',               N'security', N'Admin audit logları'),
    (N'admin.rate_limit',             N'Rate Limit',                  N'system',   N'Rate limit istatistikleri'),
    (N'admin.geo_search_logs',        N'Konum Arama Logları',         N'security', N'Geo arama logları'),
    (N'admin.hotel_coord_changes',    N'Otel Koordinat Değişimleri',  N'security', N'Koordinat değişim logları'),
    (N'admin.sitemap',                N'Sitemap XML',                 N'system',   N'Sitemap yönetimi'),
    (N'admin.listing_subscriptions',  N'Otel Liste Abonelikleri',     N'ops',      N'Liste abonelik onay ve görüntüleme')
) AS s(permission_code, permission_name, group_code, description)
ON t.permission_code = s.permission_code
WHEN MATCHED THEN UPDATE SET permission_name = s.permission_name, group_code = s.group_code, description = s.description, active = 1
WHEN NOT MATCHED THEN INSERT(permission_code, permission_name, group_code, description, active) VALUES(s.permission_code, s.permission_name, s.group_code, s.description, 1);
GO

-- Seed role-permission maps (başlangıç)
-- superadmin: hepsi
INSERT INTO dbo.admin_role_permissions(role_code, permission_code, active)
SELECT 'superadmin', p.permission_code, 1
FROM dbo.admin_permissions p
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.admin_role_permissions rp WHERE rp.role_code = 'superadmin' AND rp.permission_code = p.permission_code
);
GO

-- finance
INSERT INTO dbo.admin_role_permissions(role_code, permission_code, active)
SELECT 'finance', p.permission_code, 1
FROM dbo.admin_permissions p
WHERE p.group_code IN (N'finance', N'general')
  AND NOT EXISTS (
      SELECT 1 FROM dbo.admin_role_permissions rp WHERE rp.role_code = 'finance' AND rp.permission_code = p.permission_code
  );
GO

-- ops
INSERT INTO dbo.admin_role_permissions(role_code, permission_code, active)
SELECT 'ops', p.permission_code, 1
FROM dbo.admin_permissions p
WHERE p.group_code IN (N'ops', N'general')
  AND NOT EXISTS (
      SELECT 1 FROM dbo.admin_role_permissions rp WHERE rp.role_code = 'ops' AND rp.permission_code = p.permission_code
  );
GO

-- content
INSERT INTO dbo.admin_role_permissions(role_code, permission_code, active)
SELECT 'content', p.permission_code, 1
FROM dbo.admin_permissions p
WHERE p.group_code IN (N'content', N'general')
  AND NOT EXISTS (
      SELECT 1 FROM dbo.admin_role_permissions rp WHERE rp.role_code = 'content' AND rp.permission_code = p.permission_code
  );
GO

-- support
INSERT INTO dbo.admin_role_permissions(role_code, permission_code, active)
SELECT 'support', p.permission_code, 1
FROM dbo.admin_permissions p
WHERE p.group_code IN (N'support', N'general')
  AND NOT EXISTS (
      SELECT 1 FROM dbo.admin_role_permissions rp WHERE rp.role_code = 'support' AND rp.permission_code = p.permission_code
  );
GO

-- security
INSERT INTO dbo.admin_role_permissions(role_code, permission_code, active)
SELECT 'security', p.permission_code, 1
FROM dbo.admin_permissions p
WHERE p.group_code IN (N'security', N'system', N'general')
  AND NOT EXISTS (
      SELECT 1 FROM dbo.admin_role_permissions rp WHERE rp.role_code = 'security' AND rp.permission_code = p.permission_code
  );
GO

