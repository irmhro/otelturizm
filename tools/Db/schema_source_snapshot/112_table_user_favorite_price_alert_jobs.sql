SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.user_favorite_price_alert_jobs', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[user_favorite_price_alert_jobs]
    (
        [id] bigint IDENTITY(1,1) NOT NULL,
        [otel_id] bigint NOT NULL,
        [tarih_baslangic] date NOT NULL,
        [tarih_bitis] date NOT NULL,
        [tetikleyen_kullanici_id] bigint NULL,
        [durum] nvarchar(24) NOT NULL,
        [son_islenen_alert_id] bigint CONSTRAINT [DF__user_favo__son_i__3F6663D5] DEFAULT ((0)) NOT NULL,
        [islenen_kayit_sayisi] int CONSTRAINT [DF__user_favo__islen__405A880E] DEFAULT ((0)) NOT NULL,
        [deneme_sayisi] int CONSTRAINT [DF__user_favo__denem__414EAC47] DEFAULT ((0)) NOT NULL,
        [hata_mesaji] nvarchar(500) NULL,
        [planli_calisma_tarihi] datetime2(0) CONSTRAINT [DF__user_favo__planl__4242D080] DEFAULT (sysutcdatetime()) NOT NULL,
        [olusturulma_tarihi] datetime2(0) CONSTRAINT [DF__user_favo__olust__4336F4B9] DEFAULT (sysutcdatetime()) NOT NULL,
        [guncellenme_tarihi] datetime2(0) CONSTRAINT [DF__user_favo__gunce__442B18F2] DEFAULT (sysutcdatetime()) NOT NULL,
        CONSTRAINT [PK_user_favorite_price_alert_jobs] PRIMARY KEY CLUSTERED ([id] ASC)
    );
END
GO

-- Missing column repair blocks for existing databases.
IF COL_LENGTH(N'dbo.user_favorite_price_alert_jobs', N'id') IS NULL
BEGIN
    ALTER TABLE [dbo].[user_favorite_price_alert_jobs] ADD [id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.user_favorite_price_alert_jobs', N'otel_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[user_favorite_price_alert_jobs] ADD [otel_id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.user_favorite_price_alert_jobs', N'tarih_baslangic') IS NULL
BEGIN
    ALTER TABLE [dbo].[user_favorite_price_alert_jobs] ADD [tarih_baslangic] date NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.user_favorite_price_alert_jobs', N'tarih_bitis') IS NULL
BEGIN
    ALTER TABLE [dbo].[user_favorite_price_alert_jobs] ADD [tarih_bitis] date NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.user_favorite_price_alert_jobs', N'tetikleyen_kullanici_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[user_favorite_price_alert_jobs] ADD [tetikleyen_kullanici_id] bigint NULL;
END
GO
IF COL_LENGTH(N'dbo.user_favorite_price_alert_jobs', N'durum') IS NULL
BEGIN
    ALTER TABLE [dbo].[user_favorite_price_alert_jobs] ADD [durum] nvarchar(24) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.user_favorite_price_alert_jobs', N'son_islenen_alert_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[user_favorite_price_alert_jobs] ADD [son_islenen_alert_id] bigint DEFAULT ((0)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.user_favorite_price_alert_jobs', N'islenen_kayit_sayisi') IS NULL
BEGIN
    ALTER TABLE [dbo].[user_favorite_price_alert_jobs] ADD [islenen_kayit_sayisi] int DEFAULT ((0)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.user_favorite_price_alert_jobs', N'deneme_sayisi') IS NULL
BEGIN
    ALTER TABLE [dbo].[user_favorite_price_alert_jobs] ADD [deneme_sayisi] int DEFAULT ((0)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.user_favorite_price_alert_jobs', N'hata_mesaji') IS NULL
BEGIN
    ALTER TABLE [dbo].[user_favorite_price_alert_jobs] ADD [hata_mesaji] nvarchar(500) NULL;
END
GO
IF COL_LENGTH(N'dbo.user_favorite_price_alert_jobs', N'planli_calisma_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[user_favorite_price_alert_jobs] ADD [planli_calisma_tarihi] datetime2(0) DEFAULT (sysutcdatetime()) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.user_favorite_price_alert_jobs', N'olusturulma_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[user_favorite_price_alert_jobs] ADD [olusturulma_tarihi] datetime2(0) DEFAULT (sysutcdatetime()) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.user_favorite_price_alert_jobs', N'guncellenme_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[user_favorite_price_alert_jobs] ADD [guncellenme_tarihi] datetime2(0) DEFAULT (sysutcdatetime()) NOT NULL;
END
GO
