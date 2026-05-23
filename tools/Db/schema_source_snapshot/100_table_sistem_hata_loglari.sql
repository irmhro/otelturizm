SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.sistem_hata_loglari', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[sistem_hata_loglari]
    (
        [id] bigint IDENTITY(1,1) NOT NULL,
        [hata_seviyesi] nvarchar(255) NOT NULL,
        [hata_kodu] nvarchar(20) NULL,
        [hata_mesaji] nvarchar(max) NOT NULL,
        [hata_detayi] nvarchar(max) NULL,
        [dosya_yolu] nvarchar(500) NULL,
        [satir_no] int NULL,
        [fonksiyon_adi] nvarchar(100) NULL,
        [sinif_adi] nvarchar(100) NULL,
        [url] nvarchar(2000) NULL,
        [http_method] nvarchar(10) NULL,
        [ip_adresi] nvarchar(45) NULL,
        [user_agent] nvarchar(max) NULL,
        [referer] nvarchar(2000) NULL,
        [kullanici_id] bigint NULL,
        [session_id] nvarchar(100) NULL,
        [request_id] nvarchar(36) NULL,
        [request_verisi] nvarchar(max) NULL,
        [response_verisi] nvarchar(max) NULL,
        [ek_bilgiler] nvarchar(max) NULL,
        [olusma_tarihi] datetime2(0) CONSTRAINT [DF__sistem_ha__olusm__25A691D2] DEFAULT (sysutcdatetime()) NULL,
        [cozuldu_mu] bit CONSTRAINT [DF__sistem_ha__cozul__269AB60B] DEFAULT ((0)) NULL,
        [cozulme_tarihi] datetime2(0) NULL,
        [cozen_admin_id] bigint NULL,
        [cozum_notu] nvarchar(max) NULL,
        CONSTRAINT [PK_sistem_hata_loglari] PRIMARY KEY CLUSTERED ([id] ASC)
    );
END
GO

-- Missing column repair blocks for existing databases.
IF COL_LENGTH(N'dbo.sistem_hata_loglari', N'id') IS NULL
BEGIN
    ALTER TABLE [dbo].[sistem_hata_loglari] ADD [id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.sistem_hata_loglari', N'hata_seviyesi') IS NULL
BEGIN
    ALTER TABLE [dbo].[sistem_hata_loglari] ADD [hata_seviyesi] nvarchar(255) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.sistem_hata_loglari', N'hata_kodu') IS NULL
BEGIN
    ALTER TABLE [dbo].[sistem_hata_loglari] ADD [hata_kodu] nvarchar(20) NULL;
END
GO
IF COL_LENGTH(N'dbo.sistem_hata_loglari', N'hata_mesaji') IS NULL
BEGIN
    ALTER TABLE [dbo].[sistem_hata_loglari] ADD [hata_mesaji] nvarchar(max) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.sistem_hata_loglari', N'hata_detayi') IS NULL
BEGIN
    ALTER TABLE [dbo].[sistem_hata_loglari] ADD [hata_detayi] nvarchar(max) NULL;
END
GO
IF COL_LENGTH(N'dbo.sistem_hata_loglari', N'dosya_yolu') IS NULL
BEGIN
    ALTER TABLE [dbo].[sistem_hata_loglari] ADD [dosya_yolu] nvarchar(500) NULL;
END
GO
IF COL_LENGTH(N'dbo.sistem_hata_loglari', N'satir_no') IS NULL
BEGIN
    ALTER TABLE [dbo].[sistem_hata_loglari] ADD [satir_no] int NULL;
END
GO
IF COL_LENGTH(N'dbo.sistem_hata_loglari', N'fonksiyon_adi') IS NULL
BEGIN
    ALTER TABLE [dbo].[sistem_hata_loglari] ADD [fonksiyon_adi] nvarchar(100) NULL;
END
GO
IF COL_LENGTH(N'dbo.sistem_hata_loglari', N'sinif_adi') IS NULL
BEGIN
    ALTER TABLE [dbo].[sistem_hata_loglari] ADD [sinif_adi] nvarchar(100) NULL;
END
GO
IF COL_LENGTH(N'dbo.sistem_hata_loglari', N'url') IS NULL
BEGIN
    ALTER TABLE [dbo].[sistem_hata_loglari] ADD [url] nvarchar(2000) NULL;
END
GO
IF COL_LENGTH(N'dbo.sistem_hata_loglari', N'http_method') IS NULL
BEGIN
    ALTER TABLE [dbo].[sistem_hata_loglari] ADD [http_method] nvarchar(10) NULL;
END
GO
IF COL_LENGTH(N'dbo.sistem_hata_loglari', N'ip_adresi') IS NULL
BEGIN
    ALTER TABLE [dbo].[sistem_hata_loglari] ADD [ip_adresi] nvarchar(45) NULL;
END
GO
IF COL_LENGTH(N'dbo.sistem_hata_loglari', N'user_agent') IS NULL
BEGIN
    ALTER TABLE [dbo].[sistem_hata_loglari] ADD [user_agent] nvarchar(max) NULL;
END
GO
IF COL_LENGTH(N'dbo.sistem_hata_loglari', N'referer') IS NULL
BEGIN
    ALTER TABLE [dbo].[sistem_hata_loglari] ADD [referer] nvarchar(2000) NULL;
END
GO
IF COL_LENGTH(N'dbo.sistem_hata_loglari', N'kullanici_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[sistem_hata_loglari] ADD [kullanici_id] bigint NULL;
END
GO
IF COL_LENGTH(N'dbo.sistem_hata_loglari', N'session_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[sistem_hata_loglari] ADD [session_id] nvarchar(100) NULL;
END
GO
IF COL_LENGTH(N'dbo.sistem_hata_loglari', N'request_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[sistem_hata_loglari] ADD [request_id] nvarchar(36) NULL;
END
GO
IF COL_LENGTH(N'dbo.sistem_hata_loglari', N'request_verisi') IS NULL
BEGIN
    ALTER TABLE [dbo].[sistem_hata_loglari] ADD [request_verisi] nvarchar(max) NULL;
END
GO
IF COL_LENGTH(N'dbo.sistem_hata_loglari', N'response_verisi') IS NULL
BEGIN
    ALTER TABLE [dbo].[sistem_hata_loglari] ADD [response_verisi] nvarchar(max) NULL;
END
GO
IF COL_LENGTH(N'dbo.sistem_hata_loglari', N'ek_bilgiler') IS NULL
BEGIN
    ALTER TABLE [dbo].[sistem_hata_loglari] ADD [ek_bilgiler] nvarchar(max) NULL;
END
GO
IF COL_LENGTH(N'dbo.sistem_hata_loglari', N'olusma_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[sistem_hata_loglari] ADD [olusma_tarihi] datetime2(0) DEFAULT (sysutcdatetime()) NULL;
END
GO
IF COL_LENGTH(N'dbo.sistem_hata_loglari', N'cozuldu_mu') IS NULL
BEGIN
    ALTER TABLE [dbo].[sistem_hata_loglari] ADD [cozuldu_mu] bit DEFAULT ((0)) NULL;
END
GO
IF COL_LENGTH(N'dbo.sistem_hata_loglari', N'cozulme_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[sistem_hata_loglari] ADD [cozulme_tarihi] datetime2(0) NULL;
END
GO
IF COL_LENGTH(N'dbo.sistem_hata_loglari', N'cozen_admin_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[sistem_hata_loglari] ADD [cozen_admin_id] bigint NULL;
END
GO
IF COL_LENGTH(N'dbo.sistem_hata_loglari', N'cozum_notu') IS NULL
BEGIN
    ALTER TABLE [dbo].[sistem_hata_loglari] ADD [cozum_notu] nvarchar(max) NULL;
END
GO
