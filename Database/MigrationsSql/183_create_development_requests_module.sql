SET NOCOUNT ON;

IF OBJECT_ID(N'dbo.gelistirme_talepleri', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.gelistirme_talepleri
    (
        id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_gelistirme_talepleri PRIMARY KEY,
        ana_talep_id BIGINT NULL,
        cevap_talep_id BIGINT NULL,
        kayit_tipi NVARCHAR(40) NOT NULL CONSTRAINT DF_gelistirme_talepleri_kayit_tipi DEFAULT N'talep',
        kaynak_rol NVARCHAR(40) NOT NULL CONSTRAINT DF_gelistirme_talepleri_kaynak_rol DEFAULT N'developer',
        olusturan_kullanici_id BIGINT NOT NULL,
        atanan_gelistirici_id BIGINT NULL,
        baslik NVARCHAR(220) NULL,
        aciklama NVARCHAR(MAX) NULL,
        oncelik NVARCHAR(30) NOT NULL CONSTRAINT DF_gelistirme_talepleri_oncelik DEFAULT N'Orta',
        durum NVARCHAR(40) NOT NULL CONSTRAINT DF_gelistirme_talepleri_durum DEFAULT N'Yeni',
        planlanan_baslangic_tarihi DATE NULL,
        hedef_bitis_tarihi DATE NULL,
        tamamlanma_tarihi DATETIME2 NULL,
        gorsel_url NVARCHAR(500) NULL,
        silindi_mi BIT NOT NULL CONSTRAINT DF_gelistirme_talepleri_silindi DEFAULT 0,
        son_hareket_tarihi DATETIME2 NOT NULL CONSTRAINT DF_gelistirme_talepleri_son_hareket DEFAULT SYSUTCDATETIME(),
        olusturulma_tarihi DATETIME2 NOT NULL CONSTRAINT DF_gelistirme_talepleri_olusturulma DEFAULT SYSUTCDATETIME(),
        guncellenme_tarihi DATETIME2 NOT NULL CONSTRAINT DF_gelistirme_talepleri_guncellenme DEFAULT SYSUTCDATETIME()
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_gelistirme_talepleri_ana')
BEGIN
    ALTER TABLE dbo.gelistirme_talepleri
    ADD CONSTRAINT FK_gelistirme_talepleri_ana FOREIGN KEY (ana_talep_id) REFERENCES dbo.gelistirme_talepleri(id);
END;

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_gelistirme_talepleri_cevap')
BEGIN
    ALTER TABLE dbo.gelistirme_talepleri
    ADD CONSTRAINT FK_gelistirme_talepleri_cevap FOREIGN KEY (cevap_talep_id) REFERENCES dbo.gelistirme_talepleri(id);
END;

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_gelistirme_talepleri_olusturan')
BEGIN
    ALTER TABLE dbo.gelistirme_talepleri
    ADD CONSTRAINT FK_gelistirme_talepleri_olusturan FOREIGN KEY (olusturan_kullanici_id) REFERENCES dbo.users(id);
END;

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_gelistirme_talepleri_atanan')
BEGIN
    ALTER TABLE dbo.gelistirme_talepleri
    ADD CONSTRAINT FK_gelistirme_talepleri_atanan FOREIGN KEY (atanan_gelistirici_id) REFERENCES dbo.users(id);
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_gelistirme_talepleri_ana_talep_id' AND object_id = OBJECT_ID(N'dbo.gelistirme_talepleri'))
BEGIN
    CREATE INDEX IX_gelistirme_talepleri_ana_talep_id ON dbo.gelistirme_talepleri(ana_talep_id, silindi_mi, olusturulma_tarihi DESC);
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_gelistirme_talepleri_durum' AND object_id = OBJECT_ID(N'dbo.gelistirme_talepleri'))
BEGIN
    CREATE INDEX IX_gelistirme_talepleri_durum ON dbo.gelistirme_talepleri(durum, oncelik, son_hareket_tarihi DESC);
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_gelistirme_talepleri_olusturan' AND object_id = OBJECT_ID(N'dbo.gelistirme_talepleri'))
BEGIN
    CREATE INDEX IX_gelistirme_talepleri_olusturan ON dbo.gelistirme_talepleri(olusturan_kullanici_id, silindi_mi, olusturulma_tarihi DESC);
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_gelistirme_talepleri_atanan' AND object_id = OBJECT_ID(N'dbo.gelistirme_talepleri'))
BEGIN
    CREATE INDEX IX_gelistirme_talepleri_atanan ON dbo.gelistirme_talepleri(atanan_gelistirici_id, silindi_mi, son_hareket_tarihi DESC);
END;

IF NOT EXISTS (SELECT 1 FROM dbo.roller WHERE rol_kodu = N'developer')
BEGIN
    INSERT INTO dbo.roller
    (
        rol_kodu, rol_adi, departman, seviye, ust_rol_id, varsayilan_mi, aciklama, olusturulma_tarihi
    )
    VALUES
    (
        N'developer', N'Developer', N'Teknoloji', 72, NULL, 0, N'Gelistirme talebi olusturan ve takip eden teknik kullanici rolu.', SYSUTCDATETIME()
    );
END;

DECLARE @developerRoleId SMALLINT;
SELECT TOP (1) @developerRoleId = id FROM dbo.roller WHERE rol_kodu = N'developer';

DECLARE @developerUserId BIGINT;
SELECT TOP (1) @developerUserId = id FROM dbo.users WHERE eposta = N'devoloper.orhan@otelturizm.com';

IF @developerUserId IS NULL
BEGIN
    INSERT INTO dbo.users
    (
        ad_soyad, eposta, telefon, sifre, rol, departman, gorev_unvani,
        iki_asamali_dogrulama_aktif_mi, email_dogrulama_tarihi,
        hesap_durumu, kayit_kaynagi, dil_tercihi, para_birimi, ulke,
        iki_asamali_dogrulama_kanali, olusturulma_tarihi, guncellenme_tarihi
    )
    VALUES
    (
        N'Orhan Developer',
        N'devoloper.orhan@otelturizm.com',
        N'05320009988',
        LOWER(CONVERT(VARCHAR(64), HASHBYTES('SHA2_256', CONVERT(nvarchar(max), N'1585')), 2)),
        N'developer',
        N'Teknoloji',
        N'Full Stack Developer',
        0,
        SYSUTCDATETIME(),
        1,
        N'manual-seed',
        N'tr',
        N'TRY',
        N'Turkiye',
        N'email',
        SYSUTCDATETIME(),
        SYSUTCDATETIME()
    );

    SET @developerUserId = SCOPE_IDENTITY();
END;
ELSE
BEGIN
    UPDATE dbo.users
    SET ad_soyad = N'Orhan Developer',
        telefon = COALESCE(NULLIF(telefon, N''), N'05320009988'),
        sifre = LOWER(CONVERT(VARCHAR(64), HASHBYTES('SHA2_256', CONVERT(nvarchar(max), N'1585')), 2)),
        rol = N'developer',
        departman = COALESCE(NULLIF(departman, N''), N'Teknoloji'),
        gorev_unvani = COALESCE(NULLIF(gorev_unvani, N''), N'Full Stack Developer'),
        iki_asamali_dogrulama_kanali = COALESCE(NULLIF(iki_asamali_dogrulama_kanali, N''), N'email'),
        email_dogrulama_tarihi = COALESCE(email_dogrulama_tarihi, SYSUTCDATETIME()),
        hesap_durumu = 1,
        guncellenme_tarihi = SYSUTCDATETIME()
    WHERE id = @developerUserId;
END;

IF @developerUserId IS NOT NULL
   AND @developerRoleId IS NOT NULL
   AND NOT EXISTS
   (
       SELECT 1
       FROM dbo.kullanici_rolleri
       WHERE kullanici_id = @developerUserId
         AND rol_id = @developerRoleId
         AND bitis_tarihi IS NULL
   )
BEGIN
    INSERT INTO dbo.kullanici_rolleri
    (
        kullanici_id, rol_id, atayan_kullanici_id, atama_tarihi, bitis_tarihi
    )
    VALUES
    (
        @developerUserId, @developerRoleId, @developerUserId, SYSUTCDATETIME(), NULL
    );
END;

IF OBJECT_ID(N'schema_migrations', N'U') IS NOT NULL
AND NOT EXISTS (SELECT 1 FROM schema_migrations WHERE script_name = N'183_create_development_requests_module.sql')
BEGIN
    INSERT INTO schema_migrations (script_name, checksum, applied_at)
    VALUES (N'183_create_development_requests_module.sql', N'manual-update', SYSUTCDATETIME());
END;
