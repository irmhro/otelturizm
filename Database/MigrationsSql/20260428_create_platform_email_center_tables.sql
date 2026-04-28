SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;

IF OBJECT_ID(N'dbo.platform_email_hesaplari', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.platform_email_hesaplari
    (
        id BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        hesap_kodu NVARCHAR(80) NOT NULL,
        hesap_adi NVARCHAR(180) NOT NULL,
        email_adresi NVARCHAR(320) NOT NULL,
        gelen_protokol NVARCHAR(20) NOT NULL CONSTRAINT DF_platform_email_hesaplari_gelen_protokol DEFAULT N'IMAP',
        gelen_sunucu NVARCHAR(255) NOT NULL,
        gelen_port INT NOT NULL,
        gelen_ssl BIT NOT NULL CONSTRAINT DF_platform_email_hesaplari_gelen_ssl DEFAULT 1,
        giden_sunucu NVARCHAR(255) NOT NULL,
        giden_port INT NOT NULL,
        giden_guvenlik_tipi NVARCHAR(40) NOT NULL CONSTRAINT DF_platform_email_hesaplari_giden_guvenlik DEFAULT N'SSL/TLS',
        kullanici_adi NVARCHAR(320) NOT NULL,
        sifre_sifreli NVARCHAR(MAX) NOT NULL,
        aktif_mi BIT NOT NULL CONSTRAINT DF_platform_email_hesaplari_aktif DEFAULT 1,
        varsayilan_gonderen_mi BIT NOT NULL CONSTRAINT DF_platform_email_hesaplari_varsayilan DEFAULT 0,
        son_senkron_tarihi DATETIME2 NULL,
        son_hata_mesaji NVARCHAR(1000) NULL,
        olusturulma_tarihi DATETIME2 NOT NULL CONSTRAINT DF_platform_email_hesaplari_olusturulma DEFAULT SYSUTCDATETIME(),
        guncellenme_tarihi DATETIME2 NOT NULL CONSTRAINT DF_platform_email_hesaplari_guncellenme DEFAULT SYSUTCDATETIME()
    );

    CREATE UNIQUE INDEX UX_platform_email_hesaplari_hesap_kodu ON dbo.platform_email_hesaplari(hesap_kodu);
    CREATE UNIQUE INDEX UX_platform_email_hesaplari_email_adresi ON dbo.platform_email_hesaplari(email_adresi);
END

IF OBJECT_ID(N'dbo.platform_email_mesajlari', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.platform_email_mesajlari
    (
        id BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        hesap_id BIGINT NOT NULL,
        yon NVARCHAR(20) NOT NULL CONSTRAINT DF_platform_email_mesajlari_yon DEFAULT N'Gelen',
        klasor NVARCHAR(120) NOT NULL CONSTRAINT DF_platform_email_mesajlari_klasor DEFAULT N'INBOX',
        uid_degeri NVARCHAR(120) NULL,
        internet_message_id NVARCHAR(500) NULL,
        konu NVARCHAR(500) NULL,
        gonderen NVARCHAR(500) NULL,
        alicilar NVARCHAR(MAX) NULL,
        cc NVARCHAR(MAX) NULL,
        tarih_utc DATETIME2 NULL,
        ozet NVARCHAR(1200) NULL,
        html_icerik NVARCHAR(MAX) NULL,
        text_icerik NVARCHAR(MAX) NULL,
        okunmus_mu BIT NOT NULL CONSTRAINT DF_platform_email_mesajlari_okunmus DEFAULT 0,
        spam_mi BIT NOT NULL CONSTRAINT DF_platform_email_mesajlari_spam DEFAULT 0,
        ilgili_bildirim_log_id BIGINT NULL,
        ham_basliklar NVARCHAR(MAX) NULL,
        senkron_tarihi DATETIME2 NOT NULL CONSTRAINT DF_platform_email_mesajlari_senkron DEFAULT SYSUTCDATETIME(),
        olusturulma_tarihi DATETIME2 NOT NULL CONSTRAINT DF_platform_email_mesajlari_olusturulma DEFAULT SYSUTCDATETIME(),
        guncellenme_tarihi DATETIME2 NOT NULL CONSTRAINT DF_platform_email_mesajlari_guncellenme DEFAULT SYSUTCDATETIME()
    );

    ALTER TABLE dbo.platform_email_mesajlari
        ADD CONSTRAINT FK_platform_email_mesajlari_hesap
        FOREIGN KEY (hesap_id) REFERENCES dbo.platform_email_hesaplari(id);

    CREATE UNIQUE INDEX UX_platform_email_mesajlari_hesap_uid
        ON dbo.platform_email_mesajlari(hesap_id, yon, klasor, uid_degeri)
        WHERE uid_degeri IS NOT NULL;

    CREATE INDEX IX_platform_email_mesajlari_hesap_tarih
        ON dbo.platform_email_mesajlari(hesap_id, tarih_utc DESC);
END
