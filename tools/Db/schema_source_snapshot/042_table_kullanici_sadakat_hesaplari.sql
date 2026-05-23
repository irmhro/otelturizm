SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.kullanici_sadakat_hesaplari', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[kullanici_sadakat_hesaplari]
    (
        [id] bigint IDENTITY(1,1) NOT NULL,
        [kullanici_id] bigint NOT NULL,
        [toplam_puan] int CONSTRAINT [DF__kullanici__topla__24285DB4] DEFAULT ((0)) NOT NULL,
        [kullanilabilir_puan] int CONSTRAINT [DF__kullanici__kulla__251C81ED] DEFAULT ((0)) NOT NULL,
        [bu_yil_kazanilan_puan] int CONSTRAINT [DF__kullanici__bu_yi__2610A626] DEFAULT ((0)) NOT NULL,
        [bu_yil_kullanilan_puan] int CONSTRAINT [DF__kullanici__bu_yi__2704CA5F] DEFAULT ((0)) NOT NULL,
        [mevcut_seviye_id] bigint NULL,
        [sonraki_seviye_id] bigint NULL,
        [son_seviye_guncelleme_tarihi] datetime2(0) NULL,
        [puan_gecerlilik_tarihi] date NULL,
        [olusturulma_tarihi] datetime2(0) CONSTRAINT [DF__kullanici__olust__27F8EE98] DEFAULT (sysutcdatetime()) NOT NULL,
        [guncellenme_tarihi] datetime2(0) CONSTRAINT [DF__kullanici__gunce__28ED12D1] DEFAULT (sysutcdatetime()) NOT NULL,
        CONSTRAINT [PK_kullanici_sadakat_hesaplari] PRIMARY KEY CLUSTERED ([id] ASC)
    );
END
GO

-- Missing column repair blocks for existing databases.
IF COL_LENGTH(N'dbo.kullanici_sadakat_hesaplari', N'id') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_sadakat_hesaplari] ADD [id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.kullanici_sadakat_hesaplari', N'kullanici_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_sadakat_hesaplari] ADD [kullanici_id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.kullanici_sadakat_hesaplari', N'toplam_puan') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_sadakat_hesaplari] ADD [toplam_puan] int DEFAULT ((0)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.kullanici_sadakat_hesaplari', N'kullanilabilir_puan') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_sadakat_hesaplari] ADD [kullanilabilir_puan] int DEFAULT ((0)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.kullanici_sadakat_hesaplari', N'bu_yil_kazanilan_puan') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_sadakat_hesaplari] ADD [bu_yil_kazanilan_puan] int DEFAULT ((0)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.kullanici_sadakat_hesaplari', N'bu_yil_kullanilan_puan') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_sadakat_hesaplari] ADD [bu_yil_kullanilan_puan] int DEFAULT ((0)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.kullanici_sadakat_hesaplari', N'mevcut_seviye_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_sadakat_hesaplari] ADD [mevcut_seviye_id] bigint NULL;
END
GO
IF COL_LENGTH(N'dbo.kullanici_sadakat_hesaplari', N'sonraki_seviye_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_sadakat_hesaplari] ADD [sonraki_seviye_id] bigint NULL;
END
GO
IF COL_LENGTH(N'dbo.kullanici_sadakat_hesaplari', N'son_seviye_guncelleme_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_sadakat_hesaplari] ADD [son_seviye_guncelleme_tarihi] datetime2(0) NULL;
END
GO
IF COL_LENGTH(N'dbo.kullanici_sadakat_hesaplari', N'puan_gecerlilik_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_sadakat_hesaplari] ADD [puan_gecerlilik_tarihi] date NULL;
END
GO
IF COL_LENGTH(N'dbo.kullanici_sadakat_hesaplari', N'olusturulma_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_sadakat_hesaplari] ADD [olusturulma_tarihi] datetime2(0) DEFAULT (sysutcdatetime()) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.kullanici_sadakat_hesaplari', N'guncellenme_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_sadakat_hesaplari] ADD [guncellenme_tarihi] datetime2(0) DEFAULT (sysutcdatetime()) NOT NULL;
END
GO
