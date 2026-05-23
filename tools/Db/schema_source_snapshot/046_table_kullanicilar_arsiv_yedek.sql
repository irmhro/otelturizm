SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.kullanicilar_arsiv_yedek', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[kullanicilar_arsiv_yedek]
    (
        [id] bigint CONSTRAINT [DF__kullanicilar__id__32767D0B] DEFAULT ((0)) NOT NULL,
        [ad_soyad] nvarchar(100) NOT NULL,
        [eposta] nvarchar(100) NOT NULL,
        [telefon] nvarchar(20) NULL,
        [sifre] nvarchar(255) NOT NULL,
        [profil_fotografi] nvarchar(255) NULL,
        [email_dogrulama_tarihi] datetime2(0) NULL,
        [telefon_dogrulama_tarihi] datetime2(0) NULL,
        [son_giris_tarihi] datetime2(0) NULL,
        [son_giris_ip] nvarchar(45) NULL,
        [hesap_durumu] tinyint CONSTRAINT [DF__kullanici__hesap__336AA144] DEFAULT ((1)) NOT NULL,
        [dil_tercihi] nvarchar(5) NULL,
        [para_birimi] nvarchar(3) NULL,
        [ulke] nvarchar(50) NULL,
        [olusturulma_tarihi] datetime2(0) CONSTRAINT [DF__kullanici__olust__345EC57D] DEFAULT (sysutcdatetime()) NULL,
        [guncellenme_tarihi] datetime2(0) NULL
    );
END
GO

-- Missing column repair blocks for existing databases.
IF COL_LENGTH(N'dbo.kullanicilar_arsiv_yedek', N'id') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanicilar_arsiv_yedek] ADD [id] bigint DEFAULT ((0)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.kullanicilar_arsiv_yedek', N'ad_soyad') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanicilar_arsiv_yedek] ADD [ad_soyad] nvarchar(100) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.kullanicilar_arsiv_yedek', N'eposta') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanicilar_arsiv_yedek] ADD [eposta] nvarchar(100) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.kullanicilar_arsiv_yedek', N'telefon') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanicilar_arsiv_yedek] ADD [telefon] nvarchar(20) NULL;
END
GO
IF COL_LENGTH(N'dbo.kullanicilar_arsiv_yedek', N'sifre') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanicilar_arsiv_yedek] ADD [sifre] nvarchar(255) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.kullanicilar_arsiv_yedek', N'profil_fotografi') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanicilar_arsiv_yedek] ADD [profil_fotografi] nvarchar(255) NULL;
END
GO
IF COL_LENGTH(N'dbo.kullanicilar_arsiv_yedek', N'email_dogrulama_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanicilar_arsiv_yedek] ADD [email_dogrulama_tarihi] datetime2(0) NULL;
END
GO
IF COL_LENGTH(N'dbo.kullanicilar_arsiv_yedek', N'telefon_dogrulama_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanicilar_arsiv_yedek] ADD [telefon_dogrulama_tarihi] datetime2(0) NULL;
END
GO
IF COL_LENGTH(N'dbo.kullanicilar_arsiv_yedek', N'son_giris_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanicilar_arsiv_yedek] ADD [son_giris_tarihi] datetime2(0) NULL;
END
GO
IF COL_LENGTH(N'dbo.kullanicilar_arsiv_yedek', N'son_giris_ip') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanicilar_arsiv_yedek] ADD [son_giris_ip] nvarchar(45) NULL;
END
GO
IF COL_LENGTH(N'dbo.kullanicilar_arsiv_yedek', N'hesap_durumu') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanicilar_arsiv_yedek] ADD [hesap_durumu] tinyint DEFAULT ((1)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.kullanicilar_arsiv_yedek', N'dil_tercihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanicilar_arsiv_yedek] ADD [dil_tercihi] nvarchar(5) NULL;
END
GO
IF COL_LENGTH(N'dbo.kullanicilar_arsiv_yedek', N'para_birimi') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanicilar_arsiv_yedek] ADD [para_birimi] nvarchar(3) NULL;
END
GO
IF COL_LENGTH(N'dbo.kullanicilar_arsiv_yedek', N'ulke') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanicilar_arsiv_yedek] ADD [ulke] nvarchar(50) NULL;
END
GO
IF COL_LENGTH(N'dbo.kullanicilar_arsiv_yedek', N'olusturulma_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanicilar_arsiv_yedek] ADD [olusturulma_tarihi] datetime2(0) DEFAULT (sysutcdatetime()) NULL;
END
GO
IF COL_LENGTH(N'dbo.kullanicilar_arsiv_yedek', N'guncellenme_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanicilar_arsiv_yedek] ADD [guncellenme_tarihi] datetime2(0) NULL;
END
GO
