SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.oda_ozellik_iliskileri', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[oda_ozellik_iliskileri]
    (
        [otel_id] bigint NOT NULL,
        [oda_id] bigint NOT NULL,
        [kategori_id] smallint NOT NULL,
        [ozellik_id] smallint NOT NULL,
        [miktar] tinyint CONSTRAINT [DF_oda_ozellik_iliskileri_miktar] DEFAULT ((1)) NULL,
        [aktif_mi] bit CONSTRAINT [DF_oda_ozellik_iliskileri_aktif] DEFAULT ((1)) NOT NULL,
        [olusturulma_tarihi] datetime2(0) CONSTRAINT [DF_oda_ozellik_iliskileri_olusturma] DEFAULT (sysutcdatetime()) NOT NULL,
        [guncellenme_tarihi] datetime2(0) NULL,
        CONSTRAINT [PK_oda_ozellik_iliskileri] PRIMARY KEY CLUSTERED ([otel_id] ASC, [oda_id] ASC, [ozellik_id] ASC)
    );
END
GO

-- Missing column repair blocks for existing databases.
IF COL_LENGTH(N'dbo.oda_ozellik_iliskileri', N'otel_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[oda_ozellik_iliskileri] ADD [otel_id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.oda_ozellik_iliskileri', N'oda_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[oda_ozellik_iliskileri] ADD [oda_id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.oda_ozellik_iliskileri', N'kategori_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[oda_ozellik_iliskileri] ADD [kategori_id] smallint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.oda_ozellik_iliskileri', N'ozellik_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[oda_ozellik_iliskileri] ADD [ozellik_id] smallint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.oda_ozellik_iliskileri', N'miktar') IS NULL
BEGIN
    ALTER TABLE [dbo].[oda_ozellik_iliskileri] ADD [miktar] tinyint DEFAULT ((1)) NULL;
END
GO
IF COL_LENGTH(N'dbo.oda_ozellik_iliskileri', N'aktif_mi') IS NULL
BEGIN
    ALTER TABLE [dbo].[oda_ozellik_iliskileri] ADD [aktif_mi] bit DEFAULT ((1)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.oda_ozellik_iliskileri', N'olusturulma_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[oda_ozellik_iliskileri] ADD [olusturulma_tarihi] datetime2(0) DEFAULT (sysutcdatetime()) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.oda_ozellik_iliskileri', N'guncellenme_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[oda_ozellik_iliskileri] ADD [guncellenme_tarihi] datetime2(0) NULL;
END
GO
