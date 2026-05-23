SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.sozlesmeler', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[sozlesmeler]
    (
        [id] bigint IDENTITY(1,1) NOT NULL,
        [hedef_kitle] nvarchar(30) NOT NULL,
        [sozlesme_tipi] nvarchar(30) NOT NULL,
        [baslik] nvarchar(200) NOT NULL,
        [alt_baslik] nvarchar(300) NULL,
        [slug] nvarchar(200) NOT NULL,
        [ozet_html] nvarchar(max) NULL,
        [icerik_html] nvarchar(max) NOT NULL,
        [gorsel_url] nvarchar(500) NULL,
        [sozlesme_linki] nvarchar(500) NULL,
        [versiyon_no] int CONSTRAINT [DF_sozlesmeler_versiyon] DEFAULT ((1)) NOT NULL,
        [baslangic_tarihi] datetime2(7) CONSTRAINT [DF_sozlesmeler_baslangic] DEFAULT (sysutcdatetime()) NOT NULL,
        [bitis_tarihi] datetime2(7) NULL,
        [kabul_gerektirir_mi] bit CONSTRAINT [DF_sozlesmeler_kabul] DEFAULT ((1)) NOT NULL,
        [email_dogrulamada_gonder] bit CONSTRAINT [DF_sozlesmeler_email] DEFAULT ((1)) NOT NULL,
        [yenileme_gerekir_mi] bit CONSTRAINT [DF_sozlesmeler_yenileme] DEFAULT ((0)) NOT NULL,
        [yenileme_periyodu_gun] int NULL,
        [aktif_mi] bit CONSTRAINT [DF_sozlesmeler_aktif] DEFAULT ((1)) NOT NULL,
        [notlar] nvarchar(1000) NULL,
        [olusturan_kullanici_id] bigint NULL,
        [guncelleyen_kullanici_id] bigint NULL,
        [olusturulma_tarihi] datetime2(7) CONSTRAINT [DF_sozlesmeler_olustur] DEFAULT (sysutcdatetime()) NOT NULL,
        [guncellenme_tarihi] datetime2(7) CONSTRAINT [DF_sozlesmeler_guncel] DEFAULT (sysutcdatetime()) NOT NULL,
        CONSTRAINT [PK__sozlesme__3213E83F8ACB874B] PRIMARY KEY CLUSTERED ([id] ASC)
    );
END
GO

-- Missing column repair blocks for existing databases.
IF COL_LENGTH(N'dbo.sozlesmeler', N'id') IS NULL
BEGIN
    ALTER TABLE [dbo].[sozlesmeler] ADD [id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.sozlesmeler', N'hedef_kitle') IS NULL
BEGIN
    ALTER TABLE [dbo].[sozlesmeler] ADD [hedef_kitle] nvarchar(30) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.sozlesmeler', N'sozlesme_tipi') IS NULL
BEGIN
    ALTER TABLE [dbo].[sozlesmeler] ADD [sozlesme_tipi] nvarchar(30) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.sozlesmeler', N'baslik') IS NULL
BEGIN
    ALTER TABLE [dbo].[sozlesmeler] ADD [baslik] nvarchar(200) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.sozlesmeler', N'alt_baslik') IS NULL
BEGIN
    ALTER TABLE [dbo].[sozlesmeler] ADD [alt_baslik] nvarchar(300) NULL;
END
GO
IF COL_LENGTH(N'dbo.sozlesmeler', N'slug') IS NULL
BEGIN
    ALTER TABLE [dbo].[sozlesmeler] ADD [slug] nvarchar(200) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.sozlesmeler', N'ozet_html') IS NULL
BEGIN
    ALTER TABLE [dbo].[sozlesmeler] ADD [ozet_html] nvarchar(max) NULL;
END
GO
IF COL_LENGTH(N'dbo.sozlesmeler', N'icerik_html') IS NULL
BEGIN
    ALTER TABLE [dbo].[sozlesmeler] ADD [icerik_html] nvarchar(max) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.sozlesmeler', N'gorsel_url') IS NULL
BEGIN
    ALTER TABLE [dbo].[sozlesmeler] ADD [gorsel_url] nvarchar(500) NULL;
END
GO
IF COL_LENGTH(N'dbo.sozlesmeler', N'sozlesme_linki') IS NULL
BEGIN
    ALTER TABLE [dbo].[sozlesmeler] ADD [sozlesme_linki] nvarchar(500) NULL;
END
GO
IF COL_LENGTH(N'dbo.sozlesmeler', N'versiyon_no') IS NULL
BEGIN
    ALTER TABLE [dbo].[sozlesmeler] ADD [versiyon_no] int DEFAULT ((1)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.sozlesmeler', N'baslangic_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[sozlesmeler] ADD [baslangic_tarihi] datetime2(7) DEFAULT (sysutcdatetime()) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.sozlesmeler', N'bitis_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[sozlesmeler] ADD [bitis_tarihi] datetime2(7) NULL;
END
GO
IF COL_LENGTH(N'dbo.sozlesmeler', N'kabul_gerektirir_mi') IS NULL
BEGIN
    ALTER TABLE [dbo].[sozlesmeler] ADD [kabul_gerektirir_mi] bit DEFAULT ((1)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.sozlesmeler', N'email_dogrulamada_gonder') IS NULL
BEGIN
    ALTER TABLE [dbo].[sozlesmeler] ADD [email_dogrulamada_gonder] bit DEFAULT ((1)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.sozlesmeler', N'yenileme_gerekir_mi') IS NULL
BEGIN
    ALTER TABLE [dbo].[sozlesmeler] ADD [yenileme_gerekir_mi] bit DEFAULT ((0)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.sozlesmeler', N'yenileme_periyodu_gun') IS NULL
BEGIN
    ALTER TABLE [dbo].[sozlesmeler] ADD [yenileme_periyodu_gun] int NULL;
END
GO
IF COL_LENGTH(N'dbo.sozlesmeler', N'aktif_mi') IS NULL
BEGIN
    ALTER TABLE [dbo].[sozlesmeler] ADD [aktif_mi] bit DEFAULT ((1)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.sozlesmeler', N'notlar') IS NULL
BEGIN
    ALTER TABLE [dbo].[sozlesmeler] ADD [notlar] nvarchar(1000) NULL;
END
GO
IF COL_LENGTH(N'dbo.sozlesmeler', N'olusturan_kullanici_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[sozlesmeler] ADD [olusturan_kullanici_id] bigint NULL;
END
GO
IF COL_LENGTH(N'dbo.sozlesmeler', N'guncelleyen_kullanici_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[sozlesmeler] ADD [guncelleyen_kullanici_id] bigint NULL;
END
GO
IF COL_LENGTH(N'dbo.sozlesmeler', N'olusturulma_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[sozlesmeler] ADD [olusturulma_tarihi] datetime2(7) DEFAULT (sysutcdatetime()) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.sozlesmeler', N'guncellenme_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[sozlesmeler] ADD [guncellenme_tarihi] datetime2(7) DEFAULT (sysutcdatetime()) NOT NULL;
END
GO
