SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.kullanici_telefon_gecmisi', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[kullanici_telefon_gecmisi]
    (
        [id] bigint IDENTITY(1,1) NOT NULL,
        [kullanici_id] bigint NOT NULL,
        [onceki_telefon_raw] nvarchar(32) NULL,
        [onceki_telefon_e164] nvarchar(32) NULL,
        [yeni_telefon_raw] nvarchar(32) NULL,
        [yeni_telefon_e164] nvarchar(32) NULL,
        [dogrulama_durumu] nvarchar(40) NULL,
        [degisim_nedeni] nvarchar(255) NULL,
        [olusturulma_tarihi] datetime2(7) CONSTRAINT [DF_kullanici_telefon_gecmisi_created] DEFAULT (sysutcdatetime()) NOT NULL,
        CONSTRAINT [PK__kullanic__3213E83F677BD769] PRIMARY KEY CLUSTERED ([id] ASC)
    );
END
GO

-- Missing column repair blocks for existing databases.
IF COL_LENGTH(N'dbo.kullanici_telefon_gecmisi', N'id') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_telefon_gecmisi] ADD [id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.kullanici_telefon_gecmisi', N'kullanici_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_telefon_gecmisi] ADD [kullanici_id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.kullanici_telefon_gecmisi', N'onceki_telefon_raw') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_telefon_gecmisi] ADD [onceki_telefon_raw] nvarchar(32) NULL;
END
GO
IF COL_LENGTH(N'dbo.kullanici_telefon_gecmisi', N'onceki_telefon_e164') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_telefon_gecmisi] ADD [onceki_telefon_e164] nvarchar(32) NULL;
END
GO
IF COL_LENGTH(N'dbo.kullanici_telefon_gecmisi', N'yeni_telefon_raw') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_telefon_gecmisi] ADD [yeni_telefon_raw] nvarchar(32) NULL;
END
GO
IF COL_LENGTH(N'dbo.kullanici_telefon_gecmisi', N'yeni_telefon_e164') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_telefon_gecmisi] ADD [yeni_telefon_e164] nvarchar(32) NULL;
END
GO
IF COL_LENGTH(N'dbo.kullanici_telefon_gecmisi', N'dogrulama_durumu') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_telefon_gecmisi] ADD [dogrulama_durumu] nvarchar(40) NULL;
END
GO
IF COL_LENGTH(N'dbo.kullanici_telefon_gecmisi', N'degisim_nedeni') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_telefon_gecmisi] ADD [degisim_nedeni] nvarchar(255) NULL;
END
GO
IF COL_LENGTH(N'dbo.kullanici_telefon_gecmisi', N'olusturulma_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_telefon_gecmisi] ADD [olusturulma_tarihi] datetime2(7) DEFAULT (sysutcdatetime()) NOT NULL;
END
GO
