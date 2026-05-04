SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.sadakat_odulleri', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[sadakat_odulleri]
    (
        [id] bigint IDENTITY(1,1) NOT NULL,
        [kod] nvarchar(60) NOT NULL,
        [ad] nvarchar(120) NOT NULL,
        [aciklama] nvarchar(255) NULL,
        [gerekli_puan] int NOT NULL,
        [ikon] nvarchar(120) NULL,
        [ton] nvarchar(30) NULL,
        [aktif_mi] bit CONSTRAINT [DF__sadakat_o__aktif__0539C240] DEFAULT ((1)) NOT NULL,
        [olusturulma_tarihi] datetime2(0) CONSTRAINT [DF__sadakat_o__olust__062DE679] DEFAULT (sysutcdatetime()) NOT NULL,
        CONSTRAINT [PK_sadakat_odulleri] PRIMARY KEY CLUSTERED ([id] ASC)
    );
END
GO

-- Missing column repair blocks for existing databases.
IF COL_LENGTH(N'dbo.sadakat_odulleri', N'id') IS NULL
BEGIN
    ALTER TABLE [dbo].[sadakat_odulleri] ADD [id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.sadakat_odulleri', N'kod') IS NULL
BEGIN
    ALTER TABLE [dbo].[sadakat_odulleri] ADD [kod] nvarchar(60) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.sadakat_odulleri', N'ad') IS NULL
BEGIN
    ALTER TABLE [dbo].[sadakat_odulleri] ADD [ad] nvarchar(120) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.sadakat_odulleri', N'aciklama') IS NULL
BEGIN
    ALTER TABLE [dbo].[sadakat_odulleri] ADD [aciklama] nvarchar(255) NULL;
END
GO
IF COL_LENGTH(N'dbo.sadakat_odulleri', N'gerekli_puan') IS NULL
BEGIN
    ALTER TABLE [dbo].[sadakat_odulleri] ADD [gerekli_puan] int NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.sadakat_odulleri', N'ikon') IS NULL
BEGIN
    ALTER TABLE [dbo].[sadakat_odulleri] ADD [ikon] nvarchar(120) NULL;
END
GO
IF COL_LENGTH(N'dbo.sadakat_odulleri', N'ton') IS NULL
BEGIN
    ALTER TABLE [dbo].[sadakat_odulleri] ADD [ton] nvarchar(30) NULL;
END
GO
IF COL_LENGTH(N'dbo.sadakat_odulleri', N'aktif_mi') IS NULL
BEGIN
    ALTER TABLE [dbo].[sadakat_odulleri] ADD [aktif_mi] bit DEFAULT ((1)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.sadakat_odulleri', N'olusturulma_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[sadakat_odulleri] ADD [olusturulma_tarihi] datetime2(0) DEFAULT (sysutcdatetime()) NOT NULL;
END
GO
