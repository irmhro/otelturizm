SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.basarisiz_odeme_denemeleri', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[basarisiz_odeme_denemeleri]
    (
        [id] bigint IDENTITY(1,1) NOT NULL,
        [rezervasyon_id] bigint NOT NULL,
        [kullanici_id] bigint NOT NULL,
        [deneme_tarihi] datetime2(0) CONSTRAINT [DF__basarisiz__denem__76969D2E] DEFAULT (sysutcdatetime()) NULL,
        [tutar] decimal(10,2) NOT NULL,
        [para_birimi] nvarchar(3) NULL,
        [odeme_yontemi] nvarchar(255) NOT NULL,
        [kart_tipi] nvarchar(255) NULL,
        [kart_numarasi_masked] nvarchar(20) NULL,
        [odeme_saglayici] nvarchar(255) NULL,
        [hata_kodu] nvarchar(20) NULL,
        [hata_mesaji] nvarchar(500) NOT NULL,
        [hata_detayi] nvarchar(max) NULL,
        [uc_d_secure_durumu] nvarchar(255) NULL,
        [ip_adresi] nvarchar(45) NULL,
        [cihaz_bilgisi] nvarchar(255) NULL,
        [cozuldu_mu] bit CONSTRAINT [DF__basarisiz__cozul__778AC167] DEFAULT ((0)) NULL,
        [cozulme_tarihi] datetime2(0) NULL,
        CONSTRAINT [PK_basarisiz_odeme_denemeleri] PRIMARY KEY CLUSTERED ([id] ASC)
    );
END
GO

-- Missing column repair blocks for existing databases.
IF COL_LENGTH(N'dbo.basarisiz_odeme_denemeleri', N'id') IS NULL
BEGIN
    ALTER TABLE [dbo].[basarisiz_odeme_denemeleri] ADD [id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.basarisiz_odeme_denemeleri', N'rezervasyon_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[basarisiz_odeme_denemeleri] ADD [rezervasyon_id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.basarisiz_odeme_denemeleri', N'kullanici_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[basarisiz_odeme_denemeleri] ADD [kullanici_id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.basarisiz_odeme_denemeleri', N'deneme_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[basarisiz_odeme_denemeleri] ADD [deneme_tarihi] datetime2(0) DEFAULT (sysutcdatetime()) NULL;
END
GO
IF COL_LENGTH(N'dbo.basarisiz_odeme_denemeleri', N'tutar') IS NULL
BEGIN
    ALTER TABLE [dbo].[basarisiz_odeme_denemeleri] ADD [tutar] decimal(10,2) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.basarisiz_odeme_denemeleri', N'para_birimi') IS NULL
BEGIN
    ALTER TABLE [dbo].[basarisiz_odeme_denemeleri] ADD [para_birimi] nvarchar(3) NULL;
END
GO
IF COL_LENGTH(N'dbo.basarisiz_odeme_denemeleri', N'odeme_yontemi') IS NULL
BEGIN
    ALTER TABLE [dbo].[basarisiz_odeme_denemeleri] ADD [odeme_yontemi] nvarchar(255) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.basarisiz_odeme_denemeleri', N'kart_tipi') IS NULL
BEGIN
    ALTER TABLE [dbo].[basarisiz_odeme_denemeleri] ADD [kart_tipi] nvarchar(255) NULL;
END
GO
IF COL_LENGTH(N'dbo.basarisiz_odeme_denemeleri', N'kart_numarasi_masked') IS NULL
BEGIN
    ALTER TABLE [dbo].[basarisiz_odeme_denemeleri] ADD [kart_numarasi_masked] nvarchar(20) NULL;
END
GO
IF COL_LENGTH(N'dbo.basarisiz_odeme_denemeleri', N'odeme_saglayici') IS NULL
BEGIN
    ALTER TABLE [dbo].[basarisiz_odeme_denemeleri] ADD [odeme_saglayici] nvarchar(255) NULL;
END
GO
IF COL_LENGTH(N'dbo.basarisiz_odeme_denemeleri', N'hata_kodu') IS NULL
BEGIN
    ALTER TABLE [dbo].[basarisiz_odeme_denemeleri] ADD [hata_kodu] nvarchar(20) NULL;
END
GO
IF COL_LENGTH(N'dbo.basarisiz_odeme_denemeleri', N'hata_mesaji') IS NULL
BEGIN
    ALTER TABLE [dbo].[basarisiz_odeme_denemeleri] ADD [hata_mesaji] nvarchar(500) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.basarisiz_odeme_denemeleri', N'hata_detayi') IS NULL
BEGIN
    ALTER TABLE [dbo].[basarisiz_odeme_denemeleri] ADD [hata_detayi] nvarchar(max) NULL;
END
GO
IF COL_LENGTH(N'dbo.basarisiz_odeme_denemeleri', N'uc_d_secure_durumu') IS NULL
BEGIN
    ALTER TABLE [dbo].[basarisiz_odeme_denemeleri] ADD [uc_d_secure_durumu] nvarchar(255) NULL;
END
GO
IF COL_LENGTH(N'dbo.basarisiz_odeme_denemeleri', N'ip_adresi') IS NULL
BEGIN
    ALTER TABLE [dbo].[basarisiz_odeme_denemeleri] ADD [ip_adresi] nvarchar(45) NULL;
END
GO
IF COL_LENGTH(N'dbo.basarisiz_odeme_denemeleri', N'cihaz_bilgisi') IS NULL
BEGIN
    ALTER TABLE [dbo].[basarisiz_odeme_denemeleri] ADD [cihaz_bilgisi] nvarchar(255) NULL;
END
GO
IF COL_LENGTH(N'dbo.basarisiz_odeme_denemeleri', N'cozuldu_mu') IS NULL
BEGIN
    ALTER TABLE [dbo].[basarisiz_odeme_denemeleri] ADD [cozuldu_mu] bit DEFAULT ((0)) NULL;
END
GO
IF COL_LENGTH(N'dbo.basarisiz_odeme_denemeleri', N'cozulme_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[basarisiz_odeme_denemeleri] ADD [cozulme_tarihi] datetime2(0) NULL;
END
GO
