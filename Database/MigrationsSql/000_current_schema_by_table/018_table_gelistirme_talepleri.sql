SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.gelistirme_talepleri', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[gelistirme_talepleri]
    (
        [id] bigint IDENTITY(1,1) NOT NULL,
        [ana_talep_id] bigint NULL,
        [cevap_talep_id] bigint NULL,
        [kayit_tipi] nvarchar(40) CONSTRAINT [DF_gelistirme_talepleri_kayit_tipi] DEFAULT (N'talep') NOT NULL,
        [kaynak_rol] nvarchar(40) CONSTRAINT [DF_gelistirme_talepleri_kaynak_rol] DEFAULT (N'developer') NOT NULL,
        [olusturan_kullanici_id] bigint NOT NULL,
        [atanan_gelistirici_id] bigint NULL,
        [baslik] nvarchar(220) NULL,
        [aciklama] nvarchar(max) NULL,
        [oncelik] nvarchar(30) CONSTRAINT [DF_gelistirme_talepleri_oncelik] DEFAULT (N'Orta') NOT NULL,
        [durum] nvarchar(40) CONSTRAINT [DF_gelistirme_talepleri_durum] DEFAULT (N'Yeni') NOT NULL,
        [planlanan_baslangic_tarihi] date NULL,
        [hedef_bitis_tarihi] date NULL,
        [tamamlanma_tarihi] datetime2(7) NULL,
        [gorsel_url] nvarchar(500) NULL,
        [silindi_mi] bit CONSTRAINT [DF_gelistirme_talepleri_silindi] DEFAULT ((0)) NOT NULL,
        [son_hareket_tarihi] datetime2(7) CONSTRAINT [DF_gelistirme_talepleri_son_hareket] DEFAULT (sysutcdatetime()) NOT NULL,
        [olusturulma_tarihi] datetime2(7) CONSTRAINT [DF_gelistirme_talepleri_olusturulma] DEFAULT (sysutcdatetime()) NOT NULL,
        [guncellenme_tarihi] datetime2(7) CONSTRAINT [DF_gelistirme_talepleri_guncellenme] DEFAULT (sysutcdatetime()) NOT NULL,
        CONSTRAINT [PK_gelistirme_talepleri] PRIMARY KEY CLUSTERED ([id] ASC)
    );
END
GO

-- Missing column repair blocks for existing databases.
IF COL_LENGTH(N'dbo.gelistirme_talepleri', N'id') IS NULL
BEGIN
    ALTER TABLE [dbo].[gelistirme_talepleri] ADD [id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.gelistirme_talepleri', N'ana_talep_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[gelistirme_talepleri] ADD [ana_talep_id] bigint NULL;
END
GO
IF COL_LENGTH(N'dbo.gelistirme_talepleri', N'cevap_talep_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[gelistirme_talepleri] ADD [cevap_talep_id] bigint NULL;
END
GO
IF COL_LENGTH(N'dbo.gelistirme_talepleri', N'kayit_tipi') IS NULL
BEGIN
    ALTER TABLE [dbo].[gelistirme_talepleri] ADD [kayit_tipi] nvarchar(40) DEFAULT (N'talep') NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.gelistirme_talepleri', N'kaynak_rol') IS NULL
BEGIN
    ALTER TABLE [dbo].[gelistirme_talepleri] ADD [kaynak_rol] nvarchar(40) DEFAULT (N'developer') NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.gelistirme_talepleri', N'olusturan_kullanici_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[gelistirme_talepleri] ADD [olusturan_kullanici_id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.gelistirme_talepleri', N'atanan_gelistirici_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[gelistirme_talepleri] ADD [atanan_gelistirici_id] bigint NULL;
END
GO
IF COL_LENGTH(N'dbo.gelistirme_talepleri', N'baslik') IS NULL
BEGIN
    ALTER TABLE [dbo].[gelistirme_talepleri] ADD [baslik] nvarchar(220) NULL;
END
GO
IF COL_LENGTH(N'dbo.gelistirme_talepleri', N'aciklama') IS NULL
BEGIN
    ALTER TABLE [dbo].[gelistirme_talepleri] ADD [aciklama] nvarchar(max) NULL;
END
GO
IF COL_LENGTH(N'dbo.gelistirme_talepleri', N'oncelik') IS NULL
BEGIN
    ALTER TABLE [dbo].[gelistirme_talepleri] ADD [oncelik] nvarchar(30) DEFAULT (N'Orta') NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.gelistirme_talepleri', N'durum') IS NULL
BEGIN
    ALTER TABLE [dbo].[gelistirme_talepleri] ADD [durum] nvarchar(40) DEFAULT (N'Yeni') NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.gelistirme_talepleri', N'planlanan_baslangic_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[gelistirme_talepleri] ADD [planlanan_baslangic_tarihi] date NULL;
END
GO
IF COL_LENGTH(N'dbo.gelistirme_talepleri', N'hedef_bitis_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[gelistirme_talepleri] ADD [hedef_bitis_tarihi] date NULL;
END
GO
IF COL_LENGTH(N'dbo.gelistirme_talepleri', N'tamamlanma_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[gelistirme_talepleri] ADD [tamamlanma_tarihi] datetime2(7) NULL;
END
GO
IF COL_LENGTH(N'dbo.gelistirme_talepleri', N'gorsel_url') IS NULL
BEGIN
    ALTER TABLE [dbo].[gelistirme_talepleri] ADD [gorsel_url] nvarchar(500) NULL;
END
GO
IF COL_LENGTH(N'dbo.gelistirme_talepleri', N'silindi_mi') IS NULL
BEGIN
    ALTER TABLE [dbo].[gelistirme_talepleri] ADD [silindi_mi] bit DEFAULT ((0)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.gelistirme_talepleri', N'son_hareket_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[gelistirme_talepleri] ADD [son_hareket_tarihi] datetime2(7) DEFAULT (sysutcdatetime()) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.gelistirme_talepleri', N'olusturulma_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[gelistirme_talepleri] ADD [olusturulma_tarihi] datetime2(7) DEFAULT (sysutcdatetime()) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.gelistirme_talepleri', N'guncellenme_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[gelistirme_talepleri] ADD [guncellenme_tarihi] datetime2(7) DEFAULT (sysutcdatetime()) NOT NULL;
END
GO
