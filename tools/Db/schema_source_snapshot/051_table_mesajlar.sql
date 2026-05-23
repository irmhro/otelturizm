SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.mesajlar', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[mesajlar]
    (
        [id] bigint IDENTITY(1,1) NOT NULL,
        [konusma_id] bigint NOT NULL,
        [gonderen_turu] nvarchar(255) NOT NULL,
        [gonderen_kullanici_id] bigint NULL,
        [gonderen_otel_id] bigint NULL,
        [gonderen_firma_id] bigint NULL,
        [gonderen_firma_kullanici_id] bigint NULL,
        [mesaj_metni] nvarchar(max) NOT NULL,
        [mesaj_tipi] nvarchar(255) NULL,
        [medya_urls] nvarchar(max) NULL,
        [medya_tipleri] nvarchar(max) NULL,
        [ozel_teklif_var_mi] bit CONSTRAINT [DF__mesajlar__ozel_t__467D75B8] DEFAULT ((0)) NULL,
        [teklif_tutari] decimal(10,2) NULL,
        [teklif_para_birimi] nvarchar(3) NULL,
        [teklif_gecerlilik_suresi] datetime2(0) NULL,
        [teklif_durumu] nvarchar(255) NULL,
        [teklif_kabul_tarihi] datetime2(0) NULL,
        [okundu_mu] bit CONSTRAINT [DF__mesajlar__okundu__477199F1] DEFAULT ((0)) NULL,
        [okunma_tarihi] datetime2(0) NULL,
        [durum] nvarchar(255) NULL,
        [ip_adresi] nvarchar(45) NULL,
        [cihaz_bilgisi] nvarchar(255) NULL,
        [gonderim_tarihi] datetime2(0) CONSTRAINT [DF__mesajlar__gonder__4865BE2A] DEFAULT (sysutcdatetime()) NULL,
        [duzenlenme_tarihi] datetime2(0) NULL,
        [duzenlendi_mi] bit CONSTRAINT [DF__mesajlar__duzenl__4959E263] DEFAULT ((0)) NOT NULL,
        [duzenleyen_kullanici_id] bigint NULL,
        [silinme_tarihi] datetime2(0) NULL,
        [misafir_gizlendi_mi] bit CONSTRAINT [DF__mesajlar__misafi__4A4E069C] DEFAULT ((0)) NOT NULL,
        [firma_gizlendi_mi] bit CONSTRAINT [DF__mesajlar__firma___4B422AD5] DEFAULT ((0)) NOT NULL,
        [otel_gizlendi_mi] bit CONSTRAINT [DF__mesajlar__otel_g__4C364F0E] DEFAULT ((0)) NOT NULL,
        [silinme_nedeni] nvarchar(255) NULL,
        [silinme_gorunum_metni] nvarchar(255) NULL,
        CONSTRAINT [PK_mesajlar] PRIMARY KEY CLUSTERED ([id] ASC)
    );
END
GO

-- Missing column repair blocks for existing databases.
IF COL_LENGTH(N'dbo.mesajlar', N'id') IS NULL
BEGIN
    ALTER TABLE [dbo].[mesajlar] ADD [id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.mesajlar', N'konusma_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[mesajlar] ADD [konusma_id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.mesajlar', N'gonderen_turu') IS NULL
BEGIN
    ALTER TABLE [dbo].[mesajlar] ADD [gonderen_turu] nvarchar(255) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.mesajlar', N'gonderen_kullanici_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[mesajlar] ADD [gonderen_kullanici_id] bigint NULL;
END
GO
IF COL_LENGTH(N'dbo.mesajlar', N'gonderen_otel_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[mesajlar] ADD [gonderen_otel_id] bigint NULL;
END
GO
IF COL_LENGTH(N'dbo.mesajlar', N'gonderen_firma_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[mesajlar] ADD [gonderen_firma_id] bigint NULL;
END
GO
IF COL_LENGTH(N'dbo.mesajlar', N'gonderen_firma_kullanici_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[mesajlar] ADD [gonderen_firma_kullanici_id] bigint NULL;
END
GO
IF COL_LENGTH(N'dbo.mesajlar', N'mesaj_metni') IS NULL
BEGIN
    ALTER TABLE [dbo].[mesajlar] ADD [mesaj_metni] nvarchar(max) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.mesajlar', N'mesaj_tipi') IS NULL
BEGIN
    ALTER TABLE [dbo].[mesajlar] ADD [mesaj_tipi] nvarchar(255) NULL;
END
GO
IF COL_LENGTH(N'dbo.mesajlar', N'medya_urls') IS NULL
BEGIN
    ALTER TABLE [dbo].[mesajlar] ADD [medya_urls] nvarchar(max) NULL;
END
GO
IF COL_LENGTH(N'dbo.mesajlar', N'medya_tipleri') IS NULL
BEGIN
    ALTER TABLE [dbo].[mesajlar] ADD [medya_tipleri] nvarchar(max) NULL;
END
GO
IF COL_LENGTH(N'dbo.mesajlar', N'ozel_teklif_var_mi') IS NULL
BEGIN
    ALTER TABLE [dbo].[mesajlar] ADD [ozel_teklif_var_mi] bit DEFAULT ((0)) NULL;
END
GO
IF COL_LENGTH(N'dbo.mesajlar', N'teklif_tutari') IS NULL
BEGIN
    ALTER TABLE [dbo].[mesajlar] ADD [teklif_tutari] decimal(10,2) NULL;
END
GO
IF COL_LENGTH(N'dbo.mesajlar', N'teklif_para_birimi') IS NULL
BEGIN
    ALTER TABLE [dbo].[mesajlar] ADD [teklif_para_birimi] nvarchar(3) NULL;
END
GO
IF COL_LENGTH(N'dbo.mesajlar', N'teklif_gecerlilik_suresi') IS NULL
BEGIN
    ALTER TABLE [dbo].[mesajlar] ADD [teklif_gecerlilik_suresi] datetime2(0) NULL;
END
GO
IF COL_LENGTH(N'dbo.mesajlar', N'teklif_durumu') IS NULL
BEGIN
    ALTER TABLE [dbo].[mesajlar] ADD [teklif_durumu] nvarchar(255) NULL;
END
GO
IF COL_LENGTH(N'dbo.mesajlar', N'teklif_kabul_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[mesajlar] ADD [teklif_kabul_tarihi] datetime2(0) NULL;
END
GO
IF COL_LENGTH(N'dbo.mesajlar', N'okundu_mu') IS NULL
BEGIN
    ALTER TABLE [dbo].[mesajlar] ADD [okundu_mu] bit DEFAULT ((0)) NULL;
END
GO
IF COL_LENGTH(N'dbo.mesajlar', N'okunma_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[mesajlar] ADD [okunma_tarihi] datetime2(0) NULL;
END
GO
IF COL_LENGTH(N'dbo.mesajlar', N'durum') IS NULL
BEGIN
    ALTER TABLE [dbo].[mesajlar] ADD [durum] nvarchar(255) NULL;
END
GO
IF COL_LENGTH(N'dbo.mesajlar', N'ip_adresi') IS NULL
BEGIN
    ALTER TABLE [dbo].[mesajlar] ADD [ip_adresi] nvarchar(45) NULL;
END
GO
IF COL_LENGTH(N'dbo.mesajlar', N'cihaz_bilgisi') IS NULL
BEGIN
    ALTER TABLE [dbo].[mesajlar] ADD [cihaz_bilgisi] nvarchar(255) NULL;
END
GO
IF COL_LENGTH(N'dbo.mesajlar', N'gonderim_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[mesajlar] ADD [gonderim_tarihi] datetime2(0) DEFAULT (sysutcdatetime()) NULL;
END
GO
IF COL_LENGTH(N'dbo.mesajlar', N'duzenlenme_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[mesajlar] ADD [duzenlenme_tarihi] datetime2(0) NULL;
END
GO
IF COL_LENGTH(N'dbo.mesajlar', N'duzenlendi_mi') IS NULL
BEGIN
    ALTER TABLE [dbo].[mesajlar] ADD [duzenlendi_mi] bit DEFAULT ((0)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.mesajlar', N'duzenleyen_kullanici_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[mesajlar] ADD [duzenleyen_kullanici_id] bigint NULL;
END
GO
IF COL_LENGTH(N'dbo.mesajlar', N'silinme_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[mesajlar] ADD [silinme_tarihi] datetime2(0) NULL;
END
GO
IF COL_LENGTH(N'dbo.mesajlar', N'misafir_gizlendi_mi') IS NULL
BEGIN
    ALTER TABLE [dbo].[mesajlar] ADD [misafir_gizlendi_mi] bit DEFAULT ((0)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.mesajlar', N'firma_gizlendi_mi') IS NULL
BEGIN
    ALTER TABLE [dbo].[mesajlar] ADD [firma_gizlendi_mi] bit DEFAULT ((0)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.mesajlar', N'otel_gizlendi_mi') IS NULL
BEGIN
    ALTER TABLE [dbo].[mesajlar] ADD [otel_gizlendi_mi] bit DEFAULT ((0)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.mesajlar', N'silinme_nedeni') IS NULL
BEGIN
    ALTER TABLE [dbo].[mesajlar] ADD [silinme_nedeni] nvarchar(255) NULL;
END
GO
IF COL_LENGTH(N'dbo.mesajlar', N'silinme_gorunum_metni') IS NULL
BEGIN
    ALTER TABLE [dbo].[mesajlar] ADD [silinme_gorunum_metni] nvarchar(255) NULL;
END
GO
