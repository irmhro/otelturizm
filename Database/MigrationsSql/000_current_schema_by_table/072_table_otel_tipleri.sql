SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.otel_tipleri', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[otel_tipleri]
    (
        [id] int IDENTITY(1,1) NOT NULL,
        [kod] nvarchar(60) NOT NULL,
        [tip_adi] nvarchar(100) NOT NULL,
        [aciklama] nvarchar(300) NULL,
        [ikon_class] nvarchar(80) NULL,
        [siralama] smallint CONSTRAINT [DF_otel_tipleri_siralama] DEFAULT ((100)) NOT NULL,
        [aktif_mi] bit CONSTRAINT [DF_otel_tipleri_aktif_mi] DEFAULT ((1)) NOT NULL,
        [olusturulma_tarihi] datetime2(0) CONSTRAINT [DF_otel_tipleri_olusturulma] DEFAULT (sysutcdatetime()) NOT NULL,
        [guncellenme_tarihi] datetime2(0) NULL,
        CONSTRAINT [PK_otel_tipleri] PRIMARY KEY CLUSTERED ([id] ASC)
    );
END
GO

-- Missing column repair blocks for existing databases.
IF COL_LENGTH(N'dbo.otel_tipleri', N'id') IS NULL
BEGIN
    ALTER TABLE [dbo].[otel_tipleri] ADD [id] int NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.otel_tipleri', N'kod') IS NULL
BEGIN
    ALTER TABLE [dbo].[otel_tipleri] ADD [kod] nvarchar(60) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.otel_tipleri', N'tip_adi') IS NULL
BEGIN
    ALTER TABLE [dbo].[otel_tipleri] ADD [tip_adi] nvarchar(100) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.otel_tipleri', N'aciklama') IS NULL
BEGIN
    ALTER TABLE [dbo].[otel_tipleri] ADD [aciklama] nvarchar(300) NULL;
END
GO
IF COL_LENGTH(N'dbo.otel_tipleri', N'ikon_class') IS NULL
BEGIN
    ALTER TABLE [dbo].[otel_tipleri] ADD [ikon_class] nvarchar(80) NULL;
END
GO
IF COL_LENGTH(N'dbo.otel_tipleri', N'siralama') IS NULL
BEGIN
    ALTER TABLE [dbo].[otel_tipleri] ADD [siralama] smallint DEFAULT ((100)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.otel_tipleri', N'aktif_mi') IS NULL
BEGIN
    ALTER TABLE [dbo].[otel_tipleri] ADD [aktif_mi] bit DEFAULT ((1)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.otel_tipleri', N'olusturulma_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[otel_tipleri] ADD [olusturulma_tarihi] datetime2(0) DEFAULT (sysutcdatetime()) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.otel_tipleri', N'guncellenme_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[otel_tipleri] ADD [guncellenme_tarihi] datetime2(0) NULL;
END
GO
