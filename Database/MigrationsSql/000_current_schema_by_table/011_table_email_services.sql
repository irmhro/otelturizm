SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.email_services', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[email_services]
    (
        [id] smallint IDENTITY(1,1) NOT NULL,
        [servis_kodu] nvarchar(50) NOT NULL,
        [servis_adi] nvarchar(100) NOT NULL,
        [saglayici] nvarchar(255) NOT NULL,
        [varsayilan_mi] bit CONSTRAINT [DF__email_ser__varsa__1F98B2C1] DEFAULT ((0)) NOT NULL,
        [aktif_mi] bit CONSTRAINT [DF__email_ser__aktif__208CD6FA] DEFAULT ((1)) NOT NULL,
        [gonderen_ad] nvarchar(120) NOT NULL,
        [gonderen_eposta] nvarchar(150) NOT NULL,
        [yanitla_eposta] nvarchar(150) NULL,
        [smtp_host] nvarchar(255) NULL,
        [smtp_port] smallint CONSTRAINT [DF__email_ser__smtp___2180FB33] DEFAULT ((587)) NOT NULL,
        [smtp_kullanici_adi] nvarchar(255) NULL,
        [smtp_sifre] nvarchar(max) NULL,
        [sifre_sifrelenmis_mi] bit CONSTRAINT [DF__email_ser__sifre__22751F6C] DEFAULT ((0)) NOT NULL,
        [guvenlik_tipi] nvarchar(255) NOT NULL,
        [api_base_url] nvarchar(255) NULL,
        [api_anahtari] nvarchar(max) NULL,
        [api_secret] nvarchar(max) NULL,
        [baglanti_zaman_asimi_saniye] smallint CONSTRAINT [DF__email_ser__bagla__236943A5] DEFAULT ((30)) NOT NULL,
        [gonderim_limiti_dakika] int CONSTRAINT [DF__email_ser__gonde__245D67DE] DEFAULT ((60)) NOT NULL,
        [gonderim_limiti_saat] int CONSTRAINT [DF__email_ser__gonde__25518C17] DEFAULT ((1000)) NOT NULL,
        [gonderim_limiti_gun] int CONSTRAINT [DF__email_ser__gonde__2645B050] DEFAULT ((5000)) NOT NULL,
        [test_modu] bit CONSTRAINT [DF__email_ser__test___2739D489] DEFAULT ((1)) NOT NULL,
        [hata_esigi] smallint CONSTRAINT [DF__email_ser__hata___282DF8C2] DEFAULT ((10)) NOT NULL,
        [son_basarili_test_tarihi] datetime2(0) NULL,
        [son_hata_tarihi] datetime2(0) NULL,
        [son_hata_mesaji] nvarchar(500) NULL,
        [metadata] nvarchar(max) NULL,
        [olusturulma_tarihi] datetime2(0) CONSTRAINT [DF__email_ser__olust__29221CFB] DEFAULT (sysutcdatetime()) NOT NULL,
        [guncellenme_tarihi] datetime2(0) NULL,
        CONSTRAINT [PK_email_services] PRIMARY KEY CLUSTERED ([id] ASC)
    );
END
GO

-- Missing column repair blocks for existing databases.
IF COL_LENGTH(N'dbo.email_services', N'id') IS NULL
BEGIN
    ALTER TABLE [dbo].[email_services] ADD [id] smallint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.email_services', N'servis_kodu') IS NULL
BEGIN
    ALTER TABLE [dbo].[email_services] ADD [servis_kodu] nvarchar(50) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.email_services', N'servis_adi') IS NULL
BEGIN
    ALTER TABLE [dbo].[email_services] ADD [servis_adi] nvarchar(100) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.email_services', N'saglayici') IS NULL
BEGIN
    ALTER TABLE [dbo].[email_services] ADD [saglayici] nvarchar(255) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.email_services', N'varsayilan_mi') IS NULL
BEGIN
    ALTER TABLE [dbo].[email_services] ADD [varsayilan_mi] bit DEFAULT ((0)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.email_services', N'aktif_mi') IS NULL
BEGIN
    ALTER TABLE [dbo].[email_services] ADD [aktif_mi] bit DEFAULT ((1)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.email_services', N'gonderen_ad') IS NULL
BEGIN
    ALTER TABLE [dbo].[email_services] ADD [gonderen_ad] nvarchar(120) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.email_services', N'gonderen_eposta') IS NULL
BEGIN
    ALTER TABLE [dbo].[email_services] ADD [gonderen_eposta] nvarchar(150) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.email_services', N'yanitla_eposta') IS NULL
BEGIN
    ALTER TABLE [dbo].[email_services] ADD [yanitla_eposta] nvarchar(150) NULL;
END
GO
IF COL_LENGTH(N'dbo.email_services', N'smtp_host') IS NULL
BEGIN
    ALTER TABLE [dbo].[email_services] ADD [smtp_host] nvarchar(255) NULL;
END
GO
IF COL_LENGTH(N'dbo.email_services', N'smtp_port') IS NULL
BEGIN
    ALTER TABLE [dbo].[email_services] ADD [smtp_port] smallint DEFAULT ((587)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.email_services', N'smtp_kullanici_adi') IS NULL
BEGIN
    ALTER TABLE [dbo].[email_services] ADD [smtp_kullanici_adi] nvarchar(255) NULL;
END
GO
IF COL_LENGTH(N'dbo.email_services', N'smtp_sifre') IS NULL
BEGIN
    ALTER TABLE [dbo].[email_services] ADD [smtp_sifre] nvarchar(max) NULL;
END
GO
IF COL_LENGTH(N'dbo.email_services', N'sifre_sifrelenmis_mi') IS NULL
BEGIN
    ALTER TABLE [dbo].[email_services] ADD [sifre_sifrelenmis_mi] bit DEFAULT ((0)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.email_services', N'guvenlik_tipi') IS NULL
BEGIN
    ALTER TABLE [dbo].[email_services] ADD [guvenlik_tipi] nvarchar(255) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.email_services', N'api_base_url') IS NULL
BEGIN
    ALTER TABLE [dbo].[email_services] ADD [api_base_url] nvarchar(255) NULL;
END
GO
IF COL_LENGTH(N'dbo.email_services', N'api_anahtari') IS NULL
BEGIN
    ALTER TABLE [dbo].[email_services] ADD [api_anahtari] nvarchar(max) NULL;
END
GO
IF COL_LENGTH(N'dbo.email_services', N'api_secret') IS NULL
BEGIN
    ALTER TABLE [dbo].[email_services] ADD [api_secret] nvarchar(max) NULL;
END
GO
IF COL_LENGTH(N'dbo.email_services', N'baglanti_zaman_asimi_saniye') IS NULL
BEGIN
    ALTER TABLE [dbo].[email_services] ADD [baglanti_zaman_asimi_saniye] smallint DEFAULT ((30)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.email_services', N'gonderim_limiti_dakika') IS NULL
BEGIN
    ALTER TABLE [dbo].[email_services] ADD [gonderim_limiti_dakika] int DEFAULT ((60)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.email_services', N'gonderim_limiti_saat') IS NULL
BEGIN
    ALTER TABLE [dbo].[email_services] ADD [gonderim_limiti_saat] int DEFAULT ((1000)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.email_services', N'gonderim_limiti_gun') IS NULL
BEGIN
    ALTER TABLE [dbo].[email_services] ADD [gonderim_limiti_gun] int DEFAULT ((5000)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.email_services', N'test_modu') IS NULL
BEGIN
    ALTER TABLE [dbo].[email_services] ADD [test_modu] bit DEFAULT ((1)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.email_services', N'hata_esigi') IS NULL
BEGIN
    ALTER TABLE [dbo].[email_services] ADD [hata_esigi] smallint DEFAULT ((10)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.email_services', N'son_basarili_test_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[email_services] ADD [son_basarili_test_tarihi] datetime2(0) NULL;
END
GO
IF COL_LENGTH(N'dbo.email_services', N'son_hata_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[email_services] ADD [son_hata_tarihi] datetime2(0) NULL;
END
GO
IF COL_LENGTH(N'dbo.email_services', N'son_hata_mesaji') IS NULL
BEGIN
    ALTER TABLE [dbo].[email_services] ADD [son_hata_mesaji] nvarchar(500) NULL;
END
GO
IF COL_LENGTH(N'dbo.email_services', N'metadata') IS NULL
BEGIN
    ALTER TABLE [dbo].[email_services] ADD [metadata] nvarchar(max) NULL;
END
GO
IF COL_LENGTH(N'dbo.email_services', N'olusturulma_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[email_services] ADD [olusturulma_tarihi] datetime2(0) DEFAULT (sysutcdatetime()) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.email_services', N'guncellenme_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[email_services] ADD [guncellenme_tarihi] datetime2(0) NULL;
END
GO
