SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.kullanici_puan_hareketleri', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[kullanici_puan_hareketleri]
    (
        [id] bigint IDENTITY(1,1) NOT NULL,
        [kullanici_id] bigint NOT NULL,
        [sadakat_hesap_id] bigint NULL,
        [rezervasyon_id] bigint NULL,
        [hareket_tipi] nvarchar(60) NOT NULL,
        [baslik] nvarchar(180) NOT NULL,
        [aciklama] nvarchar(500) NULL,
        [puan_degisim] int NOT NULL,
        [puan_bakiye_sonrasi] int NULL,
        [durum] nvarchar(30) NOT NULL,
        [islem_tarihi] datetime2(0) CONSTRAINT [DF__kullanici__islem__18B6AB08] DEFAULT (sysutcdatetime()) NOT NULL,
        [gecerlilik_tarihi] datetime2(0) NULL,
        [olusturulma_tarihi] datetime2(0) CONSTRAINT [DF__kullanici__olust__19AACF41] DEFAULT (sysutcdatetime()) NOT NULL,
        CONSTRAINT [PK_kullanici_puan_hareketleri] PRIMARY KEY CLUSTERED ([id] ASC)
    );
END
GO

-- Missing column repair blocks for existing databases.
IF COL_LENGTH(N'dbo.kullanici_puan_hareketleri', N'id') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_puan_hareketleri] ADD [id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.kullanici_puan_hareketleri', N'kullanici_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_puan_hareketleri] ADD [kullanici_id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.kullanici_puan_hareketleri', N'sadakat_hesap_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_puan_hareketleri] ADD [sadakat_hesap_id] bigint NULL;
END
GO
IF COL_LENGTH(N'dbo.kullanici_puan_hareketleri', N'rezervasyon_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_puan_hareketleri] ADD [rezervasyon_id] bigint NULL;
END
GO
IF COL_LENGTH(N'dbo.kullanici_puan_hareketleri', N'hareket_tipi') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_puan_hareketleri] ADD [hareket_tipi] nvarchar(60) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.kullanici_puan_hareketleri', N'baslik') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_puan_hareketleri] ADD [baslik] nvarchar(180) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.kullanici_puan_hareketleri', N'aciklama') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_puan_hareketleri] ADD [aciklama] nvarchar(500) NULL;
END
GO
IF COL_LENGTH(N'dbo.kullanici_puan_hareketleri', N'puan_degisim') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_puan_hareketleri] ADD [puan_degisim] int NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.kullanici_puan_hareketleri', N'puan_bakiye_sonrasi') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_puan_hareketleri] ADD [puan_bakiye_sonrasi] int NULL;
END
GO
IF COL_LENGTH(N'dbo.kullanici_puan_hareketleri', N'durum') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_puan_hareketleri] ADD [durum] nvarchar(30) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.kullanici_puan_hareketleri', N'islem_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_puan_hareketleri] ADD [islem_tarihi] datetime2(0) DEFAULT (sysutcdatetime()) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.kullanici_puan_hareketleri', N'gecerlilik_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_puan_hareketleri] ADD [gecerlilik_tarihi] datetime2(0) NULL;
END
GO
IF COL_LENGTH(N'dbo.kullanici_puan_hareketleri', N'olusturulma_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_puan_hareketleri] ADD [olusturulma_tarihi] datetime2(0) DEFAULT (sysutcdatetime()) NOT NULL;
END
GO
