SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.oda_tipleri', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[oda_tipleri]
    (
        [id] bigint IDENTITY(1,1) NOT NULL,
        [otel_id] bigint NOT NULL,
        [oda_tip_kodu] nvarchar(30) NOT NULL,
        [oda_adi] nvarchar(100) NOT NULL,
        [oda_kategorisi] nvarchar(255) NOT NULL,
        [maksimum_kisi_sayisi] tinyint NOT NULL,
        [maksimum_yetiskin_sayisi] tinyint NOT NULL,
        [maksimum_cocuk_sayisi] tinyint CONSTRAINT [DF__oda_tiple__maksi__6319B466] DEFAULT ((0)) NOT NULL,
        [yatak_tipi] nvarchar(255) NULL,
        [yatak_sayisi] tinyint NULL,
        [ek_yatak_eklenebilir_mi] bit CONSTRAINT [DF__oda_tiple__ek_ya__640DD89F] DEFAULT ((0)) NULL,
        [oda_metrekare] smallint NULL,
        [balkon_var_mi] bit CONSTRAINT [DF__oda_tiple__balko__6501FCD8] DEFAULT ((0)) NULL,
        [balkon_metrekare] smallint NULL,
        [manzara_tipi] nvarchar(255) NULL,
        [ozel_banyo_var_mi] bit CONSTRAINT [DF__oda_tiple__ozel___65F62111] DEFAULT ((1)) NULL,
        [banyo_tipi] nvarchar(255) NULL,
        [standart_gecelik_fiyat] decimal(10,2) NOT NULL,
        [haftasonu_fark_orani] decimal(5,2) CONSTRAINT [DF__oda_tiple__hafta__66EA454A] DEFAULT ((0.00)) NULL,
        [cocuk_indirim_orani] decimal(5,2) CONSTRAINT [DF__oda_tiple__cocuk__67DE6983] DEFAULT ((0.00)) NULL,
        [bebek_ucretsiz_mi] bit CONSTRAINT [DF__oda_tiple__bebek__68D28DBC] DEFAULT ((1)) NULL,
        [bebek_yas_siniri] tinyint CONSTRAINT [DF__oda_tiple__bebek__69C6B1F5] DEFAULT ((2)) NULL,
        [cocuk_yas_siniri] tinyint CONSTRAINT [DF__oda_tiple__cocuk__6ABAD62E] DEFAULT ((12)) NULL,
        [toplam_oda_sayisi] smallint NOT NULL,
        [overbooking_limit] tinyint CONSTRAINT [DF__oda_tiple__overb__6BAEFA67] DEFAULT ((0)) NULL,
        [kapak_fotografi] nvarchar(255) NULL,
        [galeri] nvarchar(max) NULL,
        [ozellikler] nvarchar(max) NULL,
        [aktif_mi] bit CONSTRAINT [DF__oda_tiple__aktif__6CA31EA0] DEFAULT ((1)) NULL,
        [siralama] smallint CONSTRAINT [DF__oda_tiple__siral__6D9742D9] DEFAULT ((0)) NULL,
        [olusturulma_tarihi] datetime2(0) CONSTRAINT [DF__oda_tiple__olust__6E8B6712] DEFAULT (sysutcdatetime()) NULL,
        [guncellenme_tarihi] datetime2(0) NULL,
        CONSTRAINT [PK_oda_tipleri] PRIMARY KEY CLUSTERED ([id] ASC)
    );
END
GO

-- Missing column repair blocks for existing databases.
IF COL_LENGTH(N'dbo.oda_tipleri', N'id') IS NULL
BEGIN
    ALTER TABLE [dbo].[oda_tipleri] ADD [id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.oda_tipleri', N'otel_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[oda_tipleri] ADD [otel_id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.oda_tipleri', N'oda_tip_kodu') IS NULL
BEGIN
    ALTER TABLE [dbo].[oda_tipleri] ADD [oda_tip_kodu] nvarchar(30) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.oda_tipleri', N'oda_adi') IS NULL
BEGIN
    ALTER TABLE [dbo].[oda_tipleri] ADD [oda_adi] nvarchar(100) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.oda_tipleri', N'oda_kategorisi') IS NULL
BEGIN
    ALTER TABLE [dbo].[oda_tipleri] ADD [oda_kategorisi] nvarchar(255) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.oda_tipleri', N'maksimum_kisi_sayisi') IS NULL
BEGIN
    ALTER TABLE [dbo].[oda_tipleri] ADD [maksimum_kisi_sayisi] tinyint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.oda_tipleri', N'maksimum_yetiskin_sayisi') IS NULL
BEGIN
    ALTER TABLE [dbo].[oda_tipleri] ADD [maksimum_yetiskin_sayisi] tinyint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.oda_tipleri', N'maksimum_cocuk_sayisi') IS NULL
BEGIN
    ALTER TABLE [dbo].[oda_tipleri] ADD [maksimum_cocuk_sayisi] tinyint DEFAULT ((0)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.oda_tipleri', N'yatak_tipi') IS NULL
BEGIN
    ALTER TABLE [dbo].[oda_tipleri] ADD [yatak_tipi] nvarchar(255) NULL;
END
GO
IF COL_LENGTH(N'dbo.oda_tipleri', N'yatak_sayisi') IS NULL
BEGIN
    ALTER TABLE [dbo].[oda_tipleri] ADD [yatak_sayisi] tinyint NULL;
END
GO
IF COL_LENGTH(N'dbo.oda_tipleri', N'ek_yatak_eklenebilir_mi') IS NULL
BEGIN
    ALTER TABLE [dbo].[oda_tipleri] ADD [ek_yatak_eklenebilir_mi] bit DEFAULT ((0)) NULL;
END
GO
IF COL_LENGTH(N'dbo.oda_tipleri', N'oda_metrekare') IS NULL
BEGIN
    ALTER TABLE [dbo].[oda_tipleri] ADD [oda_metrekare] smallint NULL;
END
GO
IF COL_LENGTH(N'dbo.oda_tipleri', N'balkon_var_mi') IS NULL
BEGIN
    ALTER TABLE [dbo].[oda_tipleri] ADD [balkon_var_mi] bit DEFAULT ((0)) NULL;
END
GO
IF COL_LENGTH(N'dbo.oda_tipleri', N'balkon_metrekare') IS NULL
BEGIN
    ALTER TABLE [dbo].[oda_tipleri] ADD [balkon_metrekare] smallint NULL;
END
GO
IF COL_LENGTH(N'dbo.oda_tipleri', N'manzara_tipi') IS NULL
BEGIN
    ALTER TABLE [dbo].[oda_tipleri] ADD [manzara_tipi] nvarchar(255) NULL;
END
GO
IF COL_LENGTH(N'dbo.oda_tipleri', N'ozel_banyo_var_mi') IS NULL
BEGIN
    ALTER TABLE [dbo].[oda_tipleri] ADD [ozel_banyo_var_mi] bit DEFAULT ((1)) NULL;
END
GO
IF COL_LENGTH(N'dbo.oda_tipleri', N'banyo_tipi') IS NULL
BEGIN
    ALTER TABLE [dbo].[oda_tipleri] ADD [banyo_tipi] nvarchar(255) NULL;
END
GO
IF COL_LENGTH(N'dbo.oda_tipleri', N'standart_gecelik_fiyat') IS NULL
BEGIN
    ALTER TABLE [dbo].[oda_tipleri] ADD [standart_gecelik_fiyat] decimal(10,2) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.oda_tipleri', N'haftasonu_fark_orani') IS NULL
BEGIN
    ALTER TABLE [dbo].[oda_tipleri] ADD [haftasonu_fark_orani] decimal(5,2) DEFAULT ((0.00)) NULL;
END
GO
IF COL_LENGTH(N'dbo.oda_tipleri', N'cocuk_indirim_orani') IS NULL
BEGIN
    ALTER TABLE [dbo].[oda_tipleri] ADD [cocuk_indirim_orani] decimal(5,2) DEFAULT ((0.00)) NULL;
END
GO
IF COL_LENGTH(N'dbo.oda_tipleri', N'bebek_ucretsiz_mi') IS NULL
BEGIN
    ALTER TABLE [dbo].[oda_tipleri] ADD [bebek_ucretsiz_mi] bit DEFAULT ((1)) NULL;
END
GO
IF COL_LENGTH(N'dbo.oda_tipleri', N'bebek_yas_siniri') IS NULL
BEGIN
    ALTER TABLE [dbo].[oda_tipleri] ADD [bebek_yas_siniri] tinyint DEFAULT ((2)) NULL;
END
GO
IF COL_LENGTH(N'dbo.oda_tipleri', N'cocuk_yas_siniri') IS NULL
BEGIN
    ALTER TABLE [dbo].[oda_tipleri] ADD [cocuk_yas_siniri] tinyint DEFAULT ((12)) NULL;
END
GO
IF COL_LENGTH(N'dbo.oda_tipleri', N'toplam_oda_sayisi') IS NULL
BEGIN
    ALTER TABLE [dbo].[oda_tipleri] ADD [toplam_oda_sayisi] smallint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.oda_tipleri', N'overbooking_limit') IS NULL
BEGIN
    ALTER TABLE [dbo].[oda_tipleri] ADD [overbooking_limit] tinyint DEFAULT ((0)) NULL;
END
GO
IF COL_LENGTH(N'dbo.oda_tipleri', N'kapak_fotografi') IS NULL
BEGIN
    ALTER TABLE [dbo].[oda_tipleri] ADD [kapak_fotografi] nvarchar(255) NULL;
END
GO
IF COL_LENGTH(N'dbo.oda_tipleri', N'galeri') IS NULL
BEGIN
    ALTER TABLE [dbo].[oda_tipleri] ADD [galeri] nvarchar(max) NULL;
END
GO
IF COL_LENGTH(N'dbo.oda_tipleri', N'ozellikler') IS NULL
BEGIN
    ALTER TABLE [dbo].[oda_tipleri] ADD [ozellikler] nvarchar(max) NULL;
END
GO
IF COL_LENGTH(N'dbo.oda_tipleri', N'aktif_mi') IS NULL
BEGIN
    ALTER TABLE [dbo].[oda_tipleri] ADD [aktif_mi] bit DEFAULT ((1)) NULL;
END
GO
IF COL_LENGTH(N'dbo.oda_tipleri', N'siralama') IS NULL
BEGIN
    ALTER TABLE [dbo].[oda_tipleri] ADD [siralama] smallint DEFAULT ((0)) NULL;
END
GO
IF COL_LENGTH(N'dbo.oda_tipleri', N'olusturulma_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[oda_tipleri] ADD [olusturulma_tarihi] datetime2(0) DEFAULT (sysutcdatetime()) NULL;
END
GO
IF COL_LENGTH(N'dbo.oda_tipleri', N'guncellenme_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[oda_tipleri] ADD [guncellenme_tarihi] datetime2(0) NULL;
END
GO
