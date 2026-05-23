SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.guvenli_dosya_varliklari', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[guvenli_dosya_varliklari]
    (
        [id] bigint IDENTITY(1,1) NOT NULL,
        [baglam_tablo] nvarchar(100) NOT NULL,
        [baglam_kayit_id] bigint NOT NULL,
        [sahibi_kullanici_id] bigint NULL,
        [sahibi_firma_id] bigint NULL,
        [kategori] nvarchar(50) NOT NULL,
        [gorunurluk_kapsami] nvarchar(255) NOT NULL,
        [orijinal_dosya_adi] nvarchar(255) NOT NULL,
        [depolanan_dosya_adi] nvarchar(255) NOT NULL,
        [depolama_yolu] nvarchar(500) NOT NULL,
        [mime_tipi] nvarchar(150) NOT NULL,
        [dosya_uzantisi] nvarchar(20) NULL,
        [dosya_boyutu] bigint CONSTRAINT [DF__guvenli_d__dosya__4B7734FF] DEFAULT ((0)) NOT NULL,
        [sha256_ozeti] nchar(64) NULL,
        [gorsel_mi] bit CONSTRAINT [DF__guvenli_d__gorse__4C6B5938] DEFAULT ((0)) NOT NULL,
        [aktif_mi] bit CONSTRAINT [DF__guvenli_d__aktif__4D5F7D71] DEFAULT ((1)) NOT NULL,
        [silinme_tarihi] datetime2(0) NULL,
        [olusturulma_tarihi] datetime2(0) CONSTRAINT [DF__guvenli_d__olust__4E53A1AA] DEFAULT (sysutcdatetime()) NOT NULL,
        CONSTRAINT [PK_guvenli_dosya_varliklari] PRIMARY KEY CLUSTERED ([id] ASC)
    );
END
GO

-- Missing column repair blocks for existing databases.
IF COL_LENGTH(N'dbo.guvenli_dosya_varliklari', N'id') IS NULL
BEGIN
    ALTER TABLE [dbo].[guvenli_dosya_varliklari] ADD [id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.guvenli_dosya_varliklari', N'baglam_tablo') IS NULL
BEGIN
    ALTER TABLE [dbo].[guvenli_dosya_varliklari] ADD [baglam_tablo] nvarchar(100) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.guvenli_dosya_varliklari', N'baglam_kayit_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[guvenli_dosya_varliklari] ADD [baglam_kayit_id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.guvenli_dosya_varliklari', N'sahibi_kullanici_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[guvenli_dosya_varliklari] ADD [sahibi_kullanici_id] bigint NULL;
END
GO
IF COL_LENGTH(N'dbo.guvenli_dosya_varliklari', N'sahibi_firma_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[guvenli_dosya_varliklari] ADD [sahibi_firma_id] bigint NULL;
END
GO
IF COL_LENGTH(N'dbo.guvenli_dosya_varliklari', N'kategori') IS NULL
BEGIN
    ALTER TABLE [dbo].[guvenli_dosya_varliklari] ADD [kategori] nvarchar(50) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.guvenli_dosya_varliklari', N'gorunurluk_kapsami') IS NULL
BEGIN
    ALTER TABLE [dbo].[guvenli_dosya_varliklari] ADD [gorunurluk_kapsami] nvarchar(255) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.guvenli_dosya_varliklari', N'orijinal_dosya_adi') IS NULL
BEGIN
    ALTER TABLE [dbo].[guvenli_dosya_varliklari] ADD [orijinal_dosya_adi] nvarchar(255) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.guvenli_dosya_varliklari', N'depolanan_dosya_adi') IS NULL
BEGIN
    ALTER TABLE [dbo].[guvenli_dosya_varliklari] ADD [depolanan_dosya_adi] nvarchar(255) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.guvenli_dosya_varliklari', N'depolama_yolu') IS NULL
BEGIN
    ALTER TABLE [dbo].[guvenli_dosya_varliklari] ADD [depolama_yolu] nvarchar(500) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.guvenli_dosya_varliklari', N'mime_tipi') IS NULL
BEGIN
    ALTER TABLE [dbo].[guvenli_dosya_varliklari] ADD [mime_tipi] nvarchar(150) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.guvenli_dosya_varliklari', N'dosya_uzantisi') IS NULL
BEGIN
    ALTER TABLE [dbo].[guvenli_dosya_varliklari] ADD [dosya_uzantisi] nvarchar(20) NULL;
END
GO
IF COL_LENGTH(N'dbo.guvenli_dosya_varliklari', N'dosya_boyutu') IS NULL
BEGIN
    ALTER TABLE [dbo].[guvenli_dosya_varliklari] ADD [dosya_boyutu] bigint DEFAULT ((0)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.guvenli_dosya_varliklari', N'sha256_ozeti') IS NULL
BEGIN
    ALTER TABLE [dbo].[guvenli_dosya_varliklari] ADD [sha256_ozeti] nchar(64) NULL;
END
GO
IF COL_LENGTH(N'dbo.guvenli_dosya_varliklari', N'gorsel_mi') IS NULL
BEGIN
    ALTER TABLE [dbo].[guvenli_dosya_varliklari] ADD [gorsel_mi] bit DEFAULT ((0)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.guvenli_dosya_varliklari', N'aktif_mi') IS NULL
BEGIN
    ALTER TABLE [dbo].[guvenli_dosya_varliklari] ADD [aktif_mi] bit DEFAULT ((1)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.guvenli_dosya_varliklari', N'silinme_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[guvenli_dosya_varliklari] ADD [silinme_tarihi] datetime2(0) NULL;
END
GO
IF COL_LENGTH(N'dbo.guvenli_dosya_varliklari', N'olusturulma_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[guvenli_dosya_varliklari] ADD [olusturulma_tarihi] datetime2(0) DEFAULT (sysutcdatetime()) NOT NULL;
END
GO
