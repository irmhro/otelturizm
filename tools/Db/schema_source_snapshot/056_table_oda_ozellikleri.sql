SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.oda_ozellikleri', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[oda_ozellikleri]
    (
        [id] smallint IDENTITY(1,1) NOT NULL,
        [kategori] nvarchar(50) NOT NULL,
        [ozellik_adi] nvarchar(100) NOT NULL,
        [ozellik_ikon] nvarchar(50) NULL,
        [siralama] smallint CONSTRAINT [DF__oda_ozell__siral__5C6CB6D7] DEFAULT ((0)) NULL,
        [aktif_mi] bit CONSTRAINT [DF__oda_ozell__aktif__5D60DB10] DEFAULT ((1)) NULL,
        [kategori_id] smallint NULL,
        CONSTRAINT [PK_oda_ozellikleri] PRIMARY KEY CLUSTERED ([id] ASC)
    );
END
GO

-- Missing column repair blocks for existing databases.
IF COL_LENGTH(N'dbo.oda_ozellikleri', N'id') IS NULL
BEGIN
    ALTER TABLE [dbo].[oda_ozellikleri] ADD [id] smallint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.oda_ozellikleri', N'kategori') IS NULL
BEGIN
    ALTER TABLE [dbo].[oda_ozellikleri] ADD [kategori] nvarchar(50) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.oda_ozellikleri', N'ozellik_adi') IS NULL
BEGIN
    ALTER TABLE [dbo].[oda_ozellikleri] ADD [ozellik_adi] nvarchar(100) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.oda_ozellikleri', N'ozellik_ikon') IS NULL
BEGIN
    ALTER TABLE [dbo].[oda_ozellikleri] ADD [ozellik_ikon] nvarchar(50) NULL;
END
GO
IF COL_LENGTH(N'dbo.oda_ozellikleri', N'siralama') IS NULL
BEGIN
    ALTER TABLE [dbo].[oda_ozellikleri] ADD [siralama] smallint DEFAULT ((0)) NULL;
END
GO
IF COL_LENGTH(N'dbo.oda_ozellikleri', N'aktif_mi') IS NULL
BEGIN
    ALTER TABLE [dbo].[oda_ozellikleri] ADD [aktif_mi] bit DEFAULT ((1)) NULL;
END
GO
IF COL_LENGTH(N'dbo.oda_ozellikleri', N'kategori_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[oda_ozellikleri] ADD [kategori_id] smallint NULL;
END
GO
