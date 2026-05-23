SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.otel_ozellik_kategorileri', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[otel_ozellik_kategorileri]
    (
        [id] smallint IDENTITY(1,1) NOT NULL,
        [kategori_adi] nvarchar(50) NOT NULL,
        [kategori_ikon] nvarchar(50) NULL,
        [siralama] tinyint CONSTRAINT [DF__otel_ozel__siral__1E3A7A34] DEFAULT ((0)) NULL,
        [aktif_mi] bit CONSTRAINT [DF__otel_ozel__aktif__1F2E9E6D] DEFAULT ((1)) NULL,
        CONSTRAINT [PK_otel_ozellik_kategorileri] PRIMARY KEY CLUSTERED ([id] ASC)
    );
END
GO

-- Missing column repair blocks for existing databases.
IF COL_LENGTH(N'dbo.otel_ozellik_kategorileri', N'id') IS NULL
BEGIN
    ALTER TABLE [dbo].[otel_ozellik_kategorileri] ADD [id] smallint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.otel_ozellik_kategorileri', N'kategori_adi') IS NULL
BEGIN
    ALTER TABLE [dbo].[otel_ozellik_kategorileri] ADD [kategori_adi] nvarchar(50) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.otel_ozellik_kategorileri', N'kategori_ikon') IS NULL
BEGIN
    ALTER TABLE [dbo].[otel_ozellik_kategorileri] ADD [kategori_ikon] nvarchar(50) NULL;
END
GO
IF COL_LENGTH(N'dbo.otel_ozellik_kategorileri', N'siralama') IS NULL
BEGIN
    ALTER TABLE [dbo].[otel_ozellik_kategorileri] ADD [siralama] tinyint DEFAULT ((0)) NULL;
END
GO
IF COL_LENGTH(N'dbo.otel_ozellik_kategorileri', N'aktif_mi') IS NULL
BEGIN
    ALTER TABLE [dbo].[otel_ozellik_kategorileri] ADD [aktif_mi] bit DEFAULT ((1)) NULL;
END
GO
