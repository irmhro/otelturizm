SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.kampanya_oteller', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[kampanya_oteller]
    (
        [id] bigint IDENTITY(1,1) NOT NULL,
        [kampanya_id] bigint NOT NULL,
        [otel_id] bigint NOT NULL,
        [partner_id] bigint NULL,
        [katilim_durumu] nvarchar(255) NOT NULL,
        [katilim_kaynagi] nvarchar(255) NOT NULL,
        [baslangic_tarihi] datetime2(0) NOT NULL,
        [bitis_tarihi] datetime2(0) NOT NULL,
        [ozel_indirim_orani] decimal(5,2) NULL,
        [ozel_indirim_tutari] decimal(10,2) NULL,
        [ozel_kampanyali_fiyat] decimal(10,2) NULL,
        [kampanya_etiketi] nvarchar(120) NULL,
        [landing_url] nvarchar(500) NULL,
        [partner_notu] nvarchar(max) NULL,
        [one_cikan] bit CONSTRAINT [DF__kampanya___one_c__51300E55] DEFAULT ((0)) NOT NULL,
        [siralama] int CONSTRAINT [DF__kampanya___siral__5224328E] DEFAULT ((0)) NOT NULL,
        [admin_onay_tarihi] datetime2(0) NULL,
        [partner_onay_tarihi] datetime2(0) NULL,
        [olusturan_kullanici_id] bigint NULL,
        [guncelleyen_kullanici_id] bigint NULL,
        [olusturulma_tarihi] datetime2(0) CONSTRAINT [DF__kampanya___olust__531856C7] DEFAULT (sysutcdatetime()) NOT NULL,
        [guncellenme_tarihi] datetime2(0) NULL,
        CONSTRAINT [PK_kampanya_oteller] PRIMARY KEY CLUSTERED ([id] ASC)
    );
END
GO

-- Missing column repair blocks for existing databases.
IF COL_LENGTH(N'dbo.kampanya_oteller', N'id') IS NULL
BEGIN
    ALTER TABLE [dbo].[kampanya_oteller] ADD [id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.kampanya_oteller', N'kampanya_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[kampanya_oteller] ADD [kampanya_id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.kampanya_oteller', N'otel_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[kampanya_oteller] ADD [otel_id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.kampanya_oteller', N'partner_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[kampanya_oteller] ADD [partner_id] bigint NULL;
END
GO
IF COL_LENGTH(N'dbo.kampanya_oteller', N'katilim_durumu') IS NULL
BEGIN
    ALTER TABLE [dbo].[kampanya_oteller] ADD [katilim_durumu] nvarchar(255) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.kampanya_oteller', N'katilim_kaynagi') IS NULL
BEGIN
    ALTER TABLE [dbo].[kampanya_oteller] ADD [katilim_kaynagi] nvarchar(255) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.kampanya_oteller', N'baslangic_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[kampanya_oteller] ADD [baslangic_tarihi] datetime2(0) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.kampanya_oteller', N'bitis_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[kampanya_oteller] ADD [bitis_tarihi] datetime2(0) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.kampanya_oteller', N'ozel_indirim_orani') IS NULL
BEGIN
    ALTER TABLE [dbo].[kampanya_oteller] ADD [ozel_indirim_orani] decimal(5,2) NULL;
END
GO
IF COL_LENGTH(N'dbo.kampanya_oteller', N'ozel_indirim_tutari') IS NULL
BEGIN
    ALTER TABLE [dbo].[kampanya_oteller] ADD [ozel_indirim_tutari] decimal(10,2) NULL;
END
GO
IF COL_LENGTH(N'dbo.kampanya_oteller', N'ozel_kampanyali_fiyat') IS NULL
BEGIN
    ALTER TABLE [dbo].[kampanya_oteller] ADD [ozel_kampanyali_fiyat] decimal(10,2) NULL;
END
GO
IF COL_LENGTH(N'dbo.kampanya_oteller', N'kampanya_etiketi') IS NULL
BEGIN
    ALTER TABLE [dbo].[kampanya_oteller] ADD [kampanya_etiketi] nvarchar(120) NULL;
END
GO
IF COL_LENGTH(N'dbo.kampanya_oteller', N'landing_url') IS NULL
BEGIN
    ALTER TABLE [dbo].[kampanya_oteller] ADD [landing_url] nvarchar(500) NULL;
END
GO
IF COL_LENGTH(N'dbo.kampanya_oteller', N'partner_notu') IS NULL
BEGIN
    ALTER TABLE [dbo].[kampanya_oteller] ADD [partner_notu] nvarchar(max) NULL;
END
GO
IF COL_LENGTH(N'dbo.kampanya_oteller', N'one_cikan') IS NULL
BEGIN
    ALTER TABLE [dbo].[kampanya_oteller] ADD [one_cikan] bit DEFAULT ((0)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.kampanya_oteller', N'siralama') IS NULL
BEGIN
    ALTER TABLE [dbo].[kampanya_oteller] ADD [siralama] int DEFAULT ((0)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.kampanya_oteller', N'admin_onay_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[kampanya_oteller] ADD [admin_onay_tarihi] datetime2(0) NULL;
END
GO
IF COL_LENGTH(N'dbo.kampanya_oteller', N'partner_onay_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[kampanya_oteller] ADD [partner_onay_tarihi] datetime2(0) NULL;
END
GO
IF COL_LENGTH(N'dbo.kampanya_oteller', N'olusturan_kullanici_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[kampanya_oteller] ADD [olusturan_kullanici_id] bigint NULL;
END
GO
IF COL_LENGTH(N'dbo.kampanya_oteller', N'guncelleyen_kullanici_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[kampanya_oteller] ADD [guncelleyen_kullanici_id] bigint NULL;
END
GO
IF COL_LENGTH(N'dbo.kampanya_oteller', N'olusturulma_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[kampanya_oteller] ADD [olusturulma_tarihi] datetime2(0) DEFAULT (sysutcdatetime()) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.kampanya_oteller', N'guncellenme_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[kampanya_oteller] ADD [guncellenme_tarihi] datetime2(0) NULL;
END
GO
