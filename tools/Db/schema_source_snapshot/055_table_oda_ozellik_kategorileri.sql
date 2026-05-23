SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.oda_ozellik_kategorileri', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[oda_ozellik_kategorileri]
    (
        [id] smallint IDENTITY(1,1) NOT NULL,
        [kategori_adi] nvarchar(100) NOT NULL,
        [kategori_ikon] nvarchar(80) NULL,
        [siralama] smallint CONSTRAINT [DF_oda_ozellik_kategorileri_siralama] DEFAULT ((100)) NOT NULL,
        [aktif_mi] bit CONSTRAINT [DF_oda_ozellik_kategorileri_aktif] DEFAULT ((1)) NOT NULL,
        [olusturulma_tarihi] datetime2(0) CONSTRAINT [DF_oda_ozellik_kategorileri_olusturma] DEFAULT (sysutcdatetime()) NOT NULL,
        [guncellenme_tarihi] datetime2(0) NULL,
        CONSTRAINT [PK_oda_ozellik_kategorileri] PRIMARY KEY CLUSTERED ([id] ASC)
    );
END
GO

-- Missing column repair blocks for existing databases.
IF COL_LENGTH(N'dbo.oda_ozellik_kategorileri', N'id') IS NULL
BEGIN
    ALTER TABLE [dbo].[oda_ozellik_kategorileri] ADD [id] smallint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.oda_ozellik_kategorileri', N'kategori_adi') IS NULL
BEGIN
    ALTER TABLE [dbo].[oda_ozellik_kategorileri] ADD [kategori_adi] nvarchar(100) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.oda_ozellik_kategorileri', N'kategori_ikon') IS NULL
BEGIN
    ALTER TABLE [dbo].[oda_ozellik_kategorileri] ADD [kategori_ikon] nvarchar(80) NULL;
END
GO
IF COL_LENGTH(N'dbo.oda_ozellik_kategorileri', N'siralama') IS NULL
BEGIN
    ALTER TABLE [dbo].[oda_ozellik_kategorileri] ADD [siralama] smallint DEFAULT ((100)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.oda_ozellik_kategorileri', N'aktif_mi') IS NULL
BEGIN
    ALTER TABLE [dbo].[oda_ozellik_kategorileri] ADD [aktif_mi] bit DEFAULT ((1)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.oda_ozellik_kategorileri', N'olusturulma_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[oda_ozellik_kategorileri] ADD [olusturulma_tarihi] datetime2(0) DEFAULT (sysutcdatetime()) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.oda_ozellik_kategorileri', N'guncellenme_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[oda_ozellik_kategorileri] ADD [guncellenme_tarihi] datetime2(0) NULL;
END
GO
