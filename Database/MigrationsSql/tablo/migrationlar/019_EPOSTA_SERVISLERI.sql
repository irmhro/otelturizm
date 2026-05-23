-- Tablo: dbo.EPOSTA_SERVISLERI
IF OBJECT_ID(N'dbo.EPOSTA_SERVISLERI', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[EPOSTA_SERVISLERI] (
        [ID] smallint IDENTITY(1,1) NOT NULL,
        [SERVIS_KODU] nvarchar(50) NOT NULL,
        [SERVIS_ADI] nvarchar(100) NOT NULL,
        [SAGLAYICI] nvarchar(255) NOT NULL,
        [VARSAYILAN_MI] bit NOT NULL CONSTRAINT [DF__email_ser__varsa__1F98B2C1] DEFAULT ((0)),
        [AKTIF_MI] bit NOT NULL CONSTRAINT [DF__email_ser__aktif__208CD6FA] DEFAULT ((1)),
        [GONDEREN_AD] nvarchar(120) NOT NULL,
        [GONDEREN_EPOSTA] nvarchar(150) NOT NULL,
        [YANITLA_EPOSTA] nvarchar(150) NULL,
        [SMTP_HOST] nvarchar(255) NULL,
        [SMTP_PORT] smallint NOT NULL CONSTRAINT [DF__email_ser__smtp___2180FB33] DEFAULT ((587)),
        [SMTP_KULLANICI_ADI] nvarchar(255) NULL,
        [SMTP_SIFRE] nvarchar(max) NULL,
        [SIFRE_SIFRELENMIS_MI] bit NOT NULL CONSTRAINT [DF__email_ser__sifre__22751F6C] DEFAULT ((0)),
        [GUVENLIK_TIPI] nvarchar(255) NOT NULL,
        [API_BASE_URL] nvarchar(255) NULL,
        [API_ANAHTARI] nvarchar(max) NULL,
        [API_SECRET] nvarchar(max) NULL,
        [BAGLANTI_ZAMAN_ASIMI_SANIYE] smallint NOT NULL CONSTRAINT [DF__email_ser__bagla__236943A5] DEFAULT ((30)),
        [GONDERIM_LIMITI_DAKIKA] int NOT NULL CONSTRAINT [DF__email_ser__gonde__245D67DE] DEFAULT ((60)),
        [GONDERIM_LIMITI_SAAT] int NOT NULL CONSTRAINT [DF__email_ser__gonde__25518C17] DEFAULT ((1000)),
        [GONDERIM_LIMITI_GUN] int NOT NULL CONSTRAINT [DF__email_ser__gonde__2645B050] DEFAULT ((5000)),
        [TEST_MODU] bit NOT NULL CONSTRAINT [DF__email_ser__test___2739D489] DEFAULT ((1)),
        [HATA_ESIGI] smallint NOT NULL CONSTRAINT [DF__email_ser__hata___282DF8C2] DEFAULT ((10)),
        [SON_BASARILI_TEST_TARIHI] datetime2(0) NULL,
        [SON_HATA_TARIHI] datetime2(0) NULL,
        [SON_HATA_MESAJI] nvarchar(500) NULL,
        [METADATA] nvarchar(max) NULL,
        [OLUSTURULMA_TARIHI] datetime2(0) NOT NULL CONSTRAINT [DF__email_ser__olust__29221CFB] DEFAULT (sysutcdatetime()),
        [GUNCELLENME_TARIHI] datetime2(0) NULL,
        CONSTRAINT [PK_EPOSTA_SERVISLERI] PRIMARY KEY CLUSTERED ([ID] ASC)
    );
END
