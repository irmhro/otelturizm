IF OBJECT_ID(N'dbo.developer_bildirimleri', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.developer_bildirimleri
    (
        id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_developer_bildirimleri PRIMARY KEY,
        bildirim_kodu UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_developer_bildirimleri_kod DEFAULT NEWID(),
        kaynak_panel NVARCHAR(50) NOT NULL,
        kaynak_rol NVARCHAR(50) NULL,
        kullanici_id BIGINT NULL,
        kullanici_eposta NVARCHAR(256) NULL,
        ad_soyad NVARCHAR(200) NULL,
        bildirim_turu NVARCHAR(60) NOT NULL,
        baslik NVARCHAR(220) NOT NULL,
        icerik NVARCHAR(MAX) NOT NULL,
        sayfa_url NVARCHAR(1000) NULL,
        ip_adresi NVARCHAR(80) NULL,
        user_agent NVARCHAR(1000) NULL,
        ekran_bilgisi NVARCHAR(120) NULL,
        cihaz_bilgisi NVARCHAR(500) NULL,
        gorsel_url NVARCHAR(1000) NULL,
        durum NVARCHAR(60) NOT NULL CONSTRAINT DF_developer_bildirimleri_durum DEFAULT N'Yeni',
        oncelik NVARCHAR(40) NOT NULL CONSTRAINT DF_developer_bildirimleri_oncelik DEFAULT N'Orta',
        email_kuyruga_alindi_mi BIT NOT NULL CONSTRAINT DF_developer_bildirimleri_email DEFAULT 0,
        admin_notu NVARCHAR(MAX) NULL,
        olusturulma_tarihi DATETIME2(7) NOT NULL CONSTRAINT DF_developer_bildirimleri_olusturma DEFAULT SYSUTCDATETIME(),
        guncellenme_tarihi DATETIME2(7) NOT NULL CONSTRAINT DF_developer_bildirimleri_guncelleme DEFAULT SYSUTCDATETIME()
    );

    CREATE INDEX IX_developer_bildirimleri_panel_tarih ON dbo.developer_bildirimleri(kaynak_panel, olusturulma_tarihi DESC);
    CREATE INDEX IX_developer_bildirimleri_durum ON dbo.developer_bildirimleri(durum, olusturulma_tarihi DESC);
    CREATE INDEX IX_developer_bildirimleri_kullanici ON dbo.developer_bildirimleri(kullanici_id, olusturulma_tarihi DESC);
END;

IF NOT EXISTS (
    SELECT 1 FROM dbo.bildirim_sablonlari
    WHERE sablon_kodu = N'developer_feedback' AND tur = N'E-posta' AND dil = N'tr'
)
BEGIN
    INSERT INTO dbo.bildirim_sablonlari
    (sablon_kodu, sablon_adi, tur, dil, konu, baslik, icerik, degiskenler, aktif_mi, olusturulma_tarihi)
    VALUES
    (
        N'developer_feedback',
        N'Beta Geri Bildirim',
        N'E-posta',
        N'tr',
        N'[BETA BİLDİRİM] {{title}}',
        N'Beta Geri Bildirim',
        N'Views/Email/tr/Developer Bildirim.cshtml',
        N'feedback_id,panel_key,feedback_type,title,content,page_url,user_full_name,user_email,account_type,ip_address,user_agent,viewport,device_info,image_url,created_at',
        1,
        SYSUTCDATETIME()
    );
END
ELSE
BEGIN
    UPDATE dbo.bildirim_sablonlari
    SET konu = N'[BETA BİLDİRİM] {{title}}',
        baslik = N'Beta Geri Bildirim',
        icerik = N'Views/Email/tr/Developer Bildirim.cshtml',
        aktif_mi = 1
    WHERE sablon_kodu = N'developer_feedback' AND tur = N'E-posta' AND dil = N'tr';
END;
