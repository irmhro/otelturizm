SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.user_favorite_price_alerts', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[user_favorite_price_alerts]
    (
        [id] bigint IDENTITY(1,1) NOT NULL,
        [user_id] bigint NOT NULL,
        [otel_id] bigint NOT NULL,
        [hedef_maksimum_fiyat] decimal(12,2) NOT NULL,
        [baslangic_tarihi] date NOT NULL,
        [bitis_tarihi] date NOT NULL,
        [aktif_mi] bit CONSTRAINT [DF__user_favo__aktif__4707859D] DEFAULT ((1)) NOT NULL,
        [son_tetiklenen_tarih] datetime2(0) NULL,
        [son_tetiklenen_fiyat] decimal(12,2) NULL,
        [olusturulma_tarihi] datetime2(0) CONSTRAINT [DF__user_favo__olust__47FBA9D6] DEFAULT (sysutcdatetime()) NOT NULL,
        [guncellenme_tarihi] datetime2(0) CONSTRAINT [DF__user_favo__gunce__48EFCE0F] DEFAULT (sysutcdatetime()) NOT NULL,
        CONSTRAINT [PK_user_favorite_price_alerts] PRIMARY KEY CLUSTERED ([id] ASC)
    );
END
GO

-- Missing column repair blocks for existing databases.
IF COL_LENGTH(N'dbo.user_favorite_price_alerts', N'id') IS NULL
BEGIN
    ALTER TABLE [dbo].[user_favorite_price_alerts] ADD [id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.user_favorite_price_alerts', N'user_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[user_favorite_price_alerts] ADD [user_id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.user_favorite_price_alerts', N'otel_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[user_favorite_price_alerts] ADD [otel_id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.user_favorite_price_alerts', N'hedef_maksimum_fiyat') IS NULL
BEGIN
    ALTER TABLE [dbo].[user_favorite_price_alerts] ADD [hedef_maksimum_fiyat] decimal(12,2) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.user_favorite_price_alerts', N'baslangic_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[user_favorite_price_alerts] ADD [baslangic_tarihi] date NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.user_favorite_price_alerts', N'bitis_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[user_favorite_price_alerts] ADD [bitis_tarihi] date NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.user_favorite_price_alerts', N'aktif_mi') IS NULL
BEGIN
    ALTER TABLE [dbo].[user_favorite_price_alerts] ADD [aktif_mi] bit DEFAULT ((1)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.user_favorite_price_alerts', N'son_tetiklenen_tarih') IS NULL
BEGIN
    ALTER TABLE [dbo].[user_favorite_price_alerts] ADD [son_tetiklenen_tarih] datetime2(0) NULL;
END
GO
IF COL_LENGTH(N'dbo.user_favorite_price_alerts', N'son_tetiklenen_fiyat') IS NULL
BEGIN
    ALTER TABLE [dbo].[user_favorite_price_alerts] ADD [son_tetiklenen_fiyat] decimal(12,2) NULL;
END
GO
IF COL_LENGTH(N'dbo.user_favorite_price_alerts', N'olusturulma_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[user_favorite_price_alerts] ADD [olusturulma_tarihi] datetime2(0) DEFAULT (sysutcdatetime()) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.user_favorite_price_alerts', N'guncellenme_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[user_favorite_price_alerts] ADD [guncellenme_tarihi] datetime2(0) DEFAULT (sysutcdatetime()) NOT NULL;
END
GO
