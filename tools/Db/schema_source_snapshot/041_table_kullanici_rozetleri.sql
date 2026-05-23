SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.kullanici_rozetleri', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[kullanici_rozetleri]
    (
        [id] bigint IDENTITY(1,1) NOT NULL,
        [kullanici_id] bigint NOT NULL,
        [rozet_id] bigint NOT NULL,
        [durum] nvarchar(30) NOT NULL,
        [ilerleme_degeri] int CONSTRAINT [DF__kullanici__ilerl__1F63A897] DEFAULT ((0)) NOT NULL,
        [kazanilma_tarihi] datetime2(0) NULL,
        [olusturulma_tarihi] datetime2(0) CONSTRAINT [DF__kullanici__olust__2057CCD0] DEFAULT (sysutcdatetime()) NOT NULL,
        [guncellenme_tarihi] datetime2(0) CONSTRAINT [DF__kullanici__gunce__214BF109] DEFAULT (sysutcdatetime()) NOT NULL,
        CONSTRAINT [PK_kullanici_rozetleri] PRIMARY KEY CLUSTERED ([id] ASC)
    );
END
GO

-- Missing column repair blocks for existing databases.
IF COL_LENGTH(N'dbo.kullanici_rozetleri', N'id') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_rozetleri] ADD [id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.kullanici_rozetleri', N'kullanici_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_rozetleri] ADD [kullanici_id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.kullanici_rozetleri', N'rozet_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_rozetleri] ADD [rozet_id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.kullanici_rozetleri', N'durum') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_rozetleri] ADD [durum] nvarchar(30) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.kullanici_rozetleri', N'ilerleme_degeri') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_rozetleri] ADD [ilerleme_degeri] int DEFAULT ((0)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.kullanici_rozetleri', N'kazanilma_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_rozetleri] ADD [kazanilma_tarihi] datetime2(0) NULL;
END
GO
IF COL_LENGTH(N'dbo.kullanici_rozetleri', N'olusturulma_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_rozetleri] ADD [olusturulma_tarihi] datetime2(0) DEFAULT (sysutcdatetime()) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.kullanici_rozetleri', N'guncellenme_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_rozetleri] ADD [guncellenme_tarihi] datetime2(0) DEFAULT (sysutcdatetime()) NOT NULL;
END
GO
