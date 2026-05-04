SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.rozet_tanimlari', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[rozet_tanimlari]
    (
        [id] bigint IDENTITY(1,1) NOT NULL,
        [kod] nvarchar(60) NOT NULL,
        [ad] nvarchar(120) NOT NULL,
        [aciklama] nvarchar(255) NULL,
        [ikon] nvarchar(120) NULL,
        [kategori] nvarchar(80) NULL,
        [rozet_rengi] nvarchar(20) NULL,
        [hedef_deger] int CONSTRAINT [DF__rozet_tan__hedef__7F80E8EA] DEFAULT ((1)) NOT NULL,
        [siralama] int CONSTRAINT [DF__rozet_tan__siral__00750D23] DEFAULT ((0)) NOT NULL,
        [aktif_mi] bit CONSTRAINT [DF__rozet_tan__aktif__0169315C] DEFAULT ((1)) NOT NULL,
        [olusturulma_tarihi] datetime2(0) CONSTRAINT [DF__rozet_tan__olust__025D5595] DEFAULT (sysutcdatetime()) NOT NULL,
        CONSTRAINT [PK_rozet_tanimlari] PRIMARY KEY CLUSTERED ([id] ASC)
    );
END
GO

-- Missing column repair blocks for existing databases.
IF COL_LENGTH(N'dbo.rozet_tanimlari', N'id') IS NULL
BEGIN
    ALTER TABLE [dbo].[rozet_tanimlari] ADD [id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.rozet_tanimlari', N'kod') IS NULL
BEGIN
    ALTER TABLE [dbo].[rozet_tanimlari] ADD [kod] nvarchar(60) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.rozet_tanimlari', N'ad') IS NULL
BEGIN
    ALTER TABLE [dbo].[rozet_tanimlari] ADD [ad] nvarchar(120) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.rozet_tanimlari', N'aciklama') IS NULL
BEGIN
    ALTER TABLE [dbo].[rozet_tanimlari] ADD [aciklama] nvarchar(255) NULL;
END
GO
IF COL_LENGTH(N'dbo.rozet_tanimlari', N'ikon') IS NULL
BEGIN
    ALTER TABLE [dbo].[rozet_tanimlari] ADD [ikon] nvarchar(120) NULL;
END
GO
IF COL_LENGTH(N'dbo.rozet_tanimlari', N'kategori') IS NULL
BEGIN
    ALTER TABLE [dbo].[rozet_tanimlari] ADD [kategori] nvarchar(80) NULL;
END
GO
IF COL_LENGTH(N'dbo.rozet_tanimlari', N'rozet_rengi') IS NULL
BEGIN
    ALTER TABLE [dbo].[rozet_tanimlari] ADD [rozet_rengi] nvarchar(20) NULL;
END
GO
IF COL_LENGTH(N'dbo.rozet_tanimlari', N'hedef_deger') IS NULL
BEGIN
    ALTER TABLE [dbo].[rozet_tanimlari] ADD [hedef_deger] int DEFAULT ((1)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.rozet_tanimlari', N'siralama') IS NULL
BEGIN
    ALTER TABLE [dbo].[rozet_tanimlari] ADD [siralama] int DEFAULT ((0)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.rozet_tanimlari', N'aktif_mi') IS NULL
BEGIN
    ALTER TABLE [dbo].[rozet_tanimlari] ADD [aktif_mi] bit DEFAULT ((1)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.rozet_tanimlari', N'olusturulma_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[rozet_tanimlari] ADD [olusturulma_tarihi] datetime2(0) DEFAULT (sysutcdatetime()) NOT NULL;
END
GO
