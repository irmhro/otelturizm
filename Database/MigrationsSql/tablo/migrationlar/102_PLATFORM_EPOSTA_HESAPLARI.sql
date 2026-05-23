-- Tablo: dbo.PLATFORM_EPOSTA_HESAPLARI
IF OBJECT_ID(N'dbo.PLATFORM_EPOSTA_HESAPLARI', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[PLATFORM_EPOSTA_HESAPLARI] (
        [ID] bigint IDENTITY(1,1) NOT NULL,
        [HESAP_KODU] nvarchar(80) NOT NULL,
        [HESAP_ADI] nvarchar(180) NOT NULL,
        [EPOSTA_ADRESI] nvarchar(320) NOT NULL,
        [GELEN_PROTOKOL] nvarchar(20) NOT NULL CONSTRAINT [DF_platform_email_hesaplari_gelen_protokol] DEFAULT (N'IMAP'),
        [GELEN_SUNUCU] nvarchar(255) NOT NULL,
        [GELEN_PORT] int NOT NULL,
        [GELEN_SSL] bit NOT NULL CONSTRAINT [DF_platform_email_hesaplari_gelen_ssl] DEFAULT ((1)),
        [GIDEN_SUNUCU] nvarchar(255) NOT NULL,
        [GIDEN_PORT] int NOT NULL,
        [GIDEN_GUVENLIK_TIPI] nvarchar(40) NOT NULL CONSTRAINT [DF_platform_email_hesaplari_giden_guvenlik] DEFAULT (N'SSL/TLS'),
        [KULLANICI_ADI] nvarchar(320) NOT NULL,
        [SIFRE_SIFRELI] nvarchar(max) NOT NULL,
        [AKTIF_MI] bit NOT NULL CONSTRAINT [DF_platform_email_hesaplari_aktif] DEFAULT ((1)),
        [VARSAYILAN_GONDEREN_MI] bit NOT NULL CONSTRAINT [DF_platform_email_hesaplari_varsayilan] DEFAULT ((0)),
        [SON_SENKRON_TARIHI] datetime2(7) NULL,
        [SON_HATA_MESAJI] nvarchar(1000) NULL,
        [OLUSTURULMA_TARIHI] datetime2(7) NOT NULL CONSTRAINT [DF_platform_email_hesaplari_olusturulma] DEFAULT (sysutcdatetime()),
        [GUNCELLENME_TARIHI] datetime2(7) NOT NULL CONSTRAINT [DF_platform_email_hesaplari_guncellenme] DEFAULT (sysutcdatetime()),
        CONSTRAINT [PK_PLATFORM_EPOSTA_HESAPLARI] PRIMARY KEY CLUSTERED ([ID] ASC)
    );
END
