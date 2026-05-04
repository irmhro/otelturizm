SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.bildirim_loglari', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[bildirim_loglari]
    (
        [id] bigint IDENTITY(1,1) NOT NULL,
        [kullanici_id] bigint NOT NULL,
        [bildirim_sablon_id] smallint NULL,
        [tur] nvarchar(255) NOT NULL,
        [alici_eposta] nvarchar(100) NULL,
        [alici_telefon] nvarchar(20) NULL,
        [cihaz_token] nvarchar(255) NULL,
        [konu] nvarchar(200) NULL,
        [icerik] nvarchar(max) NOT NULL,
        [gonderilen_icerik] nvarchar(max) NULL,
        [durum] nvarchar(255) NULL,
        [saglayici] nvarchar(255) NULL,
        [saglayici_mesaj_id] nvarchar(100) NULL,
        [hata_kodu] nvarchar(20) NULL,
        [hata_mesaji] nvarchar(500) NULL,
        [gonderme_denemesi] tinyint CONSTRAINT [DF__bildirim___gonde__7A672E12] DEFAULT ((1)) NULL,
        [maksimum_deneme] tinyint CONSTRAINT [DF__bildirim___maksi__7B5B524B] DEFAULT ((3)) NULL,
        [gonderim_tarihi] datetime2(0) NULL,
        [okunma_tarihi] datetime2(0) NULL,
        [olusturulma_tarihi] datetime2(0) CONSTRAINT [DF__bildirim___olust__7C4F7684] DEFAULT (sysutcdatetime()) NULL,
        [ilgili_tablo] nvarchar(50) NULL,
        [ilgili_kayit_id] bigint NULL,
        [guncellenme_tarihi] datetime2(7) NULL,
        [ekler_json] nvarchar(max) NULL,
        [email_servis_kodu] nvarchar(80) NULL,
        [gonderen_eposta_override] nvarchar(320) NULL,
        CONSTRAINT [PK_bildirim_loglari] PRIMARY KEY CLUSTERED ([id] ASC)
    );
END
GO

-- Missing column repair blocks for existing databases.
IF COL_LENGTH(N'dbo.bildirim_loglari', N'id') IS NULL
BEGIN
    ALTER TABLE [dbo].[bildirim_loglari] ADD [id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.bildirim_loglari', N'kullanici_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[bildirim_loglari] ADD [kullanici_id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.bildirim_loglari', N'bildirim_sablon_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[bildirim_loglari] ADD [bildirim_sablon_id] smallint NULL;
END
GO
IF COL_LENGTH(N'dbo.bildirim_loglari', N'tur') IS NULL
BEGIN
    ALTER TABLE [dbo].[bildirim_loglari] ADD [tur] nvarchar(255) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.bildirim_loglari', N'alici_eposta') IS NULL
BEGIN
    ALTER TABLE [dbo].[bildirim_loglari] ADD [alici_eposta] nvarchar(100) NULL;
END
GO
IF COL_LENGTH(N'dbo.bildirim_loglari', N'alici_telefon') IS NULL
BEGIN
    ALTER TABLE [dbo].[bildirim_loglari] ADD [alici_telefon] nvarchar(20) NULL;
END
GO
IF COL_LENGTH(N'dbo.bildirim_loglari', N'cihaz_token') IS NULL
BEGIN
    ALTER TABLE [dbo].[bildirim_loglari] ADD [cihaz_token] nvarchar(255) NULL;
END
GO
IF COL_LENGTH(N'dbo.bildirim_loglari', N'konu') IS NULL
BEGIN
    ALTER TABLE [dbo].[bildirim_loglari] ADD [konu] nvarchar(200) NULL;
END
GO
IF COL_LENGTH(N'dbo.bildirim_loglari', N'icerik') IS NULL
BEGIN
    ALTER TABLE [dbo].[bildirim_loglari] ADD [icerik] nvarchar(max) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.bildirim_loglari', N'gonderilen_icerik') IS NULL
BEGIN
    ALTER TABLE [dbo].[bildirim_loglari] ADD [gonderilen_icerik] nvarchar(max) NULL;
END
GO
IF COL_LENGTH(N'dbo.bildirim_loglari', N'durum') IS NULL
BEGIN
    ALTER TABLE [dbo].[bildirim_loglari] ADD [durum] nvarchar(255) NULL;
END
GO
IF COL_LENGTH(N'dbo.bildirim_loglari', N'saglayici') IS NULL
BEGIN
    ALTER TABLE [dbo].[bildirim_loglari] ADD [saglayici] nvarchar(255) NULL;
END
GO
IF COL_LENGTH(N'dbo.bildirim_loglari', N'saglayici_mesaj_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[bildirim_loglari] ADD [saglayici_mesaj_id] nvarchar(100) NULL;
END
GO
IF COL_LENGTH(N'dbo.bildirim_loglari', N'hata_kodu') IS NULL
BEGIN
    ALTER TABLE [dbo].[bildirim_loglari] ADD [hata_kodu] nvarchar(20) NULL;
END
GO
IF COL_LENGTH(N'dbo.bildirim_loglari', N'hata_mesaji') IS NULL
BEGIN
    ALTER TABLE [dbo].[bildirim_loglari] ADD [hata_mesaji] nvarchar(500) NULL;
END
GO
IF COL_LENGTH(N'dbo.bildirim_loglari', N'gonderme_denemesi') IS NULL
BEGIN
    ALTER TABLE [dbo].[bildirim_loglari] ADD [gonderme_denemesi] tinyint DEFAULT ((1)) NULL;
END
GO
IF COL_LENGTH(N'dbo.bildirim_loglari', N'maksimum_deneme') IS NULL
BEGIN
    ALTER TABLE [dbo].[bildirim_loglari] ADD [maksimum_deneme] tinyint DEFAULT ((3)) NULL;
END
GO
IF COL_LENGTH(N'dbo.bildirim_loglari', N'gonderim_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[bildirim_loglari] ADD [gonderim_tarihi] datetime2(0) NULL;
END
GO
IF COL_LENGTH(N'dbo.bildirim_loglari', N'okunma_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[bildirim_loglari] ADD [okunma_tarihi] datetime2(0) NULL;
END
GO
IF COL_LENGTH(N'dbo.bildirim_loglari', N'olusturulma_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[bildirim_loglari] ADD [olusturulma_tarihi] datetime2(0) DEFAULT (sysutcdatetime()) NULL;
END
GO
IF COL_LENGTH(N'dbo.bildirim_loglari', N'ilgili_tablo') IS NULL
BEGIN
    ALTER TABLE [dbo].[bildirim_loglari] ADD [ilgili_tablo] nvarchar(50) NULL;
END
GO
IF COL_LENGTH(N'dbo.bildirim_loglari', N'ilgili_kayit_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[bildirim_loglari] ADD [ilgili_kayit_id] bigint NULL;
END
GO
IF COL_LENGTH(N'dbo.bildirim_loglari', N'guncellenme_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[bildirim_loglari] ADD [guncellenme_tarihi] datetime2(7) NULL;
END
GO
IF COL_LENGTH(N'dbo.bildirim_loglari', N'ekler_json') IS NULL
BEGIN
    ALTER TABLE [dbo].[bildirim_loglari] ADD [ekler_json] nvarchar(max) NULL;
END
GO
IF COL_LENGTH(N'dbo.bildirim_loglari', N'email_servis_kodu') IS NULL
BEGIN
    ALTER TABLE [dbo].[bildirim_loglari] ADD [email_servis_kodu] nvarchar(80) NULL;
END
GO
IF COL_LENGTH(N'dbo.bildirim_loglari', N'gonderen_eposta_override') IS NULL
BEGIN
    ALTER TABLE [dbo].[bildirim_loglari] ADD [gonderen_eposta_override] nvarchar(320) NULL;
END
GO
