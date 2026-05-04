SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.yorumlar', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[yorumlar]
    (
        [id] bigint IDENTITY(1,1) NOT NULL,
        [otel_id] bigint NOT NULL,
        [kullanici_id] bigint NOT NULL,
        [rezervasyon_id] bigint NULL,
        [genel_puan] tinyint NOT NULL,
        [temizlik_puani] tinyint NOT NULL,
        [konfor_puani] tinyint NOT NULL,
        [konum_puani] tinyint NOT NULL,
        [personel_puani] tinyint NOT NULL,
        [fiyat_performans_puani] tinyint NOT NULL,
        [yorum_basligi] nvarchar(200) NULL,
        [yorum_metni] nvarchar(max) NOT NULL,
        [olumlu_yanlar] nvarchar(max) NULL,
        [olumsuz_yanlar] nvarchar(max) NULL,
        [konaklama_tarihi] date NULL,
        [konaklama_turu] nvarchar(255) NULL,
        [kaldigi_oda_tipi] nvarchar(100) NULL,
        [gece_sayisi] tinyint NULL,
        [dogrulanmis_konaklama] bit CONSTRAINT [DF__yorumlar__dogrul__68687968] DEFAULT ((0)) NULL,
        [onay_durumu] nvarchar(255) NULL,
        [onaylayan_admin_id] bigint NULL,
        [onay_tarihi] datetime2(0) NULL,
        [red_nedeni] nvarchar(500) NULL,
        [faydali_oy_sayisi] int CONSTRAINT [DF__yorumlar__faydal__695C9DA1] DEFAULT ((0)) NULL,
        [faydasiz_oy_sayisi] int CONSTRAINT [DF__yorumlar__faydas__6A50C1DA] DEFAULT ((0)) NULL,
        [rapor_sayisi] smallint CONSTRAINT [DF__yorumlar__rapor___6B44E613] DEFAULT ((0)) NULL,
        [otel_yaniti] nvarchar(max) NULL,
        [otel_yaniti_tarihi] datetime2(0) NULL,
        [yanitlayan_kullanici_id] bigint NULL,
        [yorum_gorselleri] nvarchar(max) NULL,
        [anonim_mi] bit CONSTRAINT [DF__yorumlar__anonim__6C390A4C] DEFAULT ((0)) NULL,
        [olusturulma_tarihi] datetime2(0) CONSTRAINT [DF__yorumlar__olustu__6D2D2E85] DEFAULT (sysutcdatetime()) NULL,
        [guncellenme_tarihi] datetime2(0) NULL,
        [seyahat_profili] nvarchar(40) NULL,
        [memnuniyet_seviyesi] tinyint NULL,
        [genel_puan_10] tinyint NULL,
        [puan_oda_10] tinyint NULL,
        [puan_konum_10] tinyint NULL,
        [puan_fiyat_10] tinyint NULL,
        [puan_personel_10] tinyint NULL,
        [puan_temizlik_10] tinyint NULL,
        [puan_sessizlik_10] tinyint NULL,
        [puan_ulasim_10] tinyint NULL,
        CONSTRAINT [PK_yorumlar] PRIMARY KEY CLUSTERED ([id] ASC)
    );
END
GO

-- Missing column repair blocks for existing databases.
IF COL_LENGTH(N'dbo.yorumlar', N'id') IS NULL
BEGIN
    ALTER TABLE [dbo].[yorumlar] ADD [id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.yorumlar', N'otel_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[yorumlar] ADD [otel_id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.yorumlar', N'kullanici_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[yorumlar] ADD [kullanici_id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.yorumlar', N'rezervasyon_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[yorumlar] ADD [rezervasyon_id] bigint NULL;
END
GO
IF COL_LENGTH(N'dbo.yorumlar', N'genel_puan') IS NULL
BEGIN
    ALTER TABLE [dbo].[yorumlar] ADD [genel_puan] tinyint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.yorumlar', N'temizlik_puani') IS NULL
BEGIN
    ALTER TABLE [dbo].[yorumlar] ADD [temizlik_puani] tinyint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.yorumlar', N'konfor_puani') IS NULL
BEGIN
    ALTER TABLE [dbo].[yorumlar] ADD [konfor_puani] tinyint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.yorumlar', N'konum_puani') IS NULL
BEGIN
    ALTER TABLE [dbo].[yorumlar] ADD [konum_puani] tinyint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.yorumlar', N'personel_puani') IS NULL
BEGIN
    ALTER TABLE [dbo].[yorumlar] ADD [personel_puani] tinyint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.yorumlar', N'fiyat_performans_puani') IS NULL
BEGIN
    ALTER TABLE [dbo].[yorumlar] ADD [fiyat_performans_puani] tinyint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.yorumlar', N'yorum_basligi') IS NULL
BEGIN
    ALTER TABLE [dbo].[yorumlar] ADD [yorum_basligi] nvarchar(200) NULL;
END
GO
IF COL_LENGTH(N'dbo.yorumlar', N'yorum_metni') IS NULL
BEGIN
    ALTER TABLE [dbo].[yorumlar] ADD [yorum_metni] nvarchar(max) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.yorumlar', N'olumlu_yanlar') IS NULL
BEGIN
    ALTER TABLE [dbo].[yorumlar] ADD [olumlu_yanlar] nvarchar(max) NULL;
END
GO
IF COL_LENGTH(N'dbo.yorumlar', N'olumsuz_yanlar') IS NULL
BEGIN
    ALTER TABLE [dbo].[yorumlar] ADD [olumsuz_yanlar] nvarchar(max) NULL;
END
GO
IF COL_LENGTH(N'dbo.yorumlar', N'konaklama_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[yorumlar] ADD [konaklama_tarihi] date NULL;
END
GO
IF COL_LENGTH(N'dbo.yorumlar', N'konaklama_turu') IS NULL
BEGIN
    ALTER TABLE [dbo].[yorumlar] ADD [konaklama_turu] nvarchar(255) NULL;
END
GO
IF COL_LENGTH(N'dbo.yorumlar', N'kaldigi_oda_tipi') IS NULL
BEGIN
    ALTER TABLE [dbo].[yorumlar] ADD [kaldigi_oda_tipi] nvarchar(100) NULL;
END
GO
IF COL_LENGTH(N'dbo.yorumlar', N'gece_sayisi') IS NULL
BEGIN
    ALTER TABLE [dbo].[yorumlar] ADD [gece_sayisi] tinyint NULL;
END
GO
IF COL_LENGTH(N'dbo.yorumlar', N'dogrulanmis_konaklama') IS NULL
BEGIN
    ALTER TABLE [dbo].[yorumlar] ADD [dogrulanmis_konaklama] bit DEFAULT ((0)) NULL;
END
GO
IF COL_LENGTH(N'dbo.yorumlar', N'onay_durumu') IS NULL
BEGIN
    ALTER TABLE [dbo].[yorumlar] ADD [onay_durumu] nvarchar(255) NULL;
END
GO
IF COL_LENGTH(N'dbo.yorumlar', N'onaylayan_admin_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[yorumlar] ADD [onaylayan_admin_id] bigint NULL;
END
GO
IF COL_LENGTH(N'dbo.yorumlar', N'onay_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[yorumlar] ADD [onay_tarihi] datetime2(0) NULL;
END
GO
IF COL_LENGTH(N'dbo.yorumlar', N'red_nedeni') IS NULL
BEGIN
    ALTER TABLE [dbo].[yorumlar] ADD [red_nedeni] nvarchar(500) NULL;
END
GO
IF COL_LENGTH(N'dbo.yorumlar', N'faydali_oy_sayisi') IS NULL
BEGIN
    ALTER TABLE [dbo].[yorumlar] ADD [faydali_oy_sayisi] int DEFAULT ((0)) NULL;
END
GO
IF COL_LENGTH(N'dbo.yorumlar', N'faydasiz_oy_sayisi') IS NULL
BEGIN
    ALTER TABLE [dbo].[yorumlar] ADD [faydasiz_oy_sayisi] int DEFAULT ((0)) NULL;
END
GO
IF COL_LENGTH(N'dbo.yorumlar', N'rapor_sayisi') IS NULL
BEGIN
    ALTER TABLE [dbo].[yorumlar] ADD [rapor_sayisi] smallint DEFAULT ((0)) NULL;
END
GO
IF COL_LENGTH(N'dbo.yorumlar', N'otel_yaniti') IS NULL
BEGIN
    ALTER TABLE [dbo].[yorumlar] ADD [otel_yaniti] nvarchar(max) NULL;
END
GO
IF COL_LENGTH(N'dbo.yorumlar', N'otel_yaniti_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[yorumlar] ADD [otel_yaniti_tarihi] datetime2(0) NULL;
END
GO
IF COL_LENGTH(N'dbo.yorumlar', N'yanitlayan_kullanici_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[yorumlar] ADD [yanitlayan_kullanici_id] bigint NULL;
END
GO
IF COL_LENGTH(N'dbo.yorumlar', N'yorum_gorselleri') IS NULL
BEGIN
    ALTER TABLE [dbo].[yorumlar] ADD [yorum_gorselleri] nvarchar(max) NULL;
END
GO
IF COL_LENGTH(N'dbo.yorumlar', N'anonim_mi') IS NULL
BEGIN
    ALTER TABLE [dbo].[yorumlar] ADD [anonim_mi] bit DEFAULT ((0)) NULL;
END
GO
IF COL_LENGTH(N'dbo.yorumlar', N'olusturulma_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[yorumlar] ADD [olusturulma_tarihi] datetime2(0) DEFAULT (sysutcdatetime()) NULL;
END
GO
IF COL_LENGTH(N'dbo.yorumlar', N'guncellenme_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[yorumlar] ADD [guncellenme_tarihi] datetime2(0) NULL;
END
GO
IF COL_LENGTH(N'dbo.yorumlar', N'seyahat_profili') IS NULL
BEGIN
    ALTER TABLE [dbo].[yorumlar] ADD [seyahat_profili] nvarchar(40) NULL;
END
GO
IF COL_LENGTH(N'dbo.yorumlar', N'memnuniyet_seviyesi') IS NULL
BEGIN
    ALTER TABLE [dbo].[yorumlar] ADD [memnuniyet_seviyesi] tinyint NULL;
END
GO
IF COL_LENGTH(N'dbo.yorumlar', N'genel_puan_10') IS NULL
BEGIN
    ALTER TABLE [dbo].[yorumlar] ADD [genel_puan_10] tinyint NULL;
END
GO
IF COL_LENGTH(N'dbo.yorumlar', N'puan_oda_10') IS NULL
BEGIN
    ALTER TABLE [dbo].[yorumlar] ADD [puan_oda_10] tinyint NULL;
END
GO
IF COL_LENGTH(N'dbo.yorumlar', N'puan_konum_10') IS NULL
BEGIN
    ALTER TABLE [dbo].[yorumlar] ADD [puan_konum_10] tinyint NULL;
END
GO
IF COL_LENGTH(N'dbo.yorumlar', N'puan_fiyat_10') IS NULL
BEGIN
    ALTER TABLE [dbo].[yorumlar] ADD [puan_fiyat_10] tinyint NULL;
END
GO
IF COL_LENGTH(N'dbo.yorumlar', N'puan_personel_10') IS NULL
BEGIN
    ALTER TABLE [dbo].[yorumlar] ADD [puan_personel_10] tinyint NULL;
END
GO
IF COL_LENGTH(N'dbo.yorumlar', N'puan_temizlik_10') IS NULL
BEGIN
    ALTER TABLE [dbo].[yorumlar] ADD [puan_temizlik_10] tinyint NULL;
END
GO
IF COL_LENGTH(N'dbo.yorumlar', N'puan_sessizlik_10') IS NULL
BEGIN
    ALTER TABLE [dbo].[yorumlar] ADD [puan_sessizlik_10] tinyint NULL;
END
GO
IF COL_LENGTH(N'dbo.yorumlar', N'puan_ulasim_10') IS NULL
BEGIN
    ALTER TABLE [dbo].[yorumlar] ADD [puan_ulasim_10] tinyint NULL;
END
GO
