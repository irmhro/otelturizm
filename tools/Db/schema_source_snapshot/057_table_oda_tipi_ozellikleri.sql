SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.oda_tipi_ozellikleri', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[oda_tipi_ozellikleri]
    (
        [oda_tip_id] bigint NOT NULL,
        [ozellik_id] smallint NOT NULL,
        [miktar] tinyint CONSTRAINT [DF__oda_tipi___mikta__603D47BB] DEFAULT ((1)) NULL,
        [otel_id] bigint NULL,
        [kategori_id] smallint NULL,
        CONSTRAINT [PK_oda_tipi_ozellikleri] PRIMARY KEY CLUSTERED ([oda_tip_id] ASC, [ozellik_id] ASC)
    );
END
GO

-- Missing column repair blocks for existing databases.
IF COL_LENGTH(N'dbo.oda_tipi_ozellikleri', N'oda_tip_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[oda_tipi_ozellikleri] ADD [oda_tip_id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.oda_tipi_ozellikleri', N'ozellik_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[oda_tipi_ozellikleri] ADD [ozellik_id] smallint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.oda_tipi_ozellikleri', N'miktar') IS NULL
BEGIN
    ALTER TABLE [dbo].[oda_tipi_ozellikleri] ADD [miktar] tinyint DEFAULT ((1)) NULL;
END
GO
IF COL_LENGTH(N'dbo.oda_tipi_ozellikleri', N'otel_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[oda_tipi_ozellikleri] ADD [otel_id] bigint NULL;
END
GO
IF COL_LENGTH(N'dbo.oda_tipi_ozellikleri', N'kategori_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[oda_tipi_ozellikleri] ADD [kategori_id] smallint NULL;
END
GO
