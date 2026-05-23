SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.otel_ozellikleri', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[otel_ozellikleri]
    (
        [id] int IDENTITY(1,1) NOT NULL,
        [kategori_id] smallint NOT NULL,
        [ozellik_adi] nvarchar(100) NOT NULL,
        [ozellik_ikon] nvarchar(50) NULL,
        [ucretli_mi] bit CONSTRAINT [DF__otel_ozel__ucret__220B0B18] DEFAULT ((0)) NULL,
        [one_cikan_ozellik] bit CONSTRAINT [DF__otel_ozel__one_c__22FF2F51] DEFAULT ((0)) NULL,
        [siralama] smallint CONSTRAINT [DF__otel_ozel__siral__23F3538A] DEFAULT ((0)) NULL,
        [aktif_mi] bit CONSTRAINT [DF__otel_ozel__aktif__24E777C3] DEFAULT ((1)) NULL,
        CONSTRAINT [PK_otel_ozellikleri] PRIMARY KEY CLUSTERED ([id] ASC)
    );
END
GO

-- Missing column repair blocks for existing databases.
IF COL_LENGTH(N'dbo.otel_ozellikleri', N'id') IS NULL
BEGIN
    ALTER TABLE [dbo].[otel_ozellikleri] ADD [id] int NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.otel_ozellikleri', N'kategori_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[otel_ozellikleri] ADD [kategori_id] smallint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.otel_ozellikleri', N'ozellik_adi') IS NULL
BEGIN
    ALTER TABLE [dbo].[otel_ozellikleri] ADD [ozellik_adi] nvarchar(100) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.otel_ozellikleri', N'ozellik_ikon') IS NULL
BEGIN
    ALTER TABLE [dbo].[otel_ozellikleri] ADD [ozellik_ikon] nvarchar(50) NULL;
END
GO
IF COL_LENGTH(N'dbo.otel_ozellikleri', N'ucretli_mi') IS NULL
BEGIN
    ALTER TABLE [dbo].[otel_ozellikleri] ADD [ucretli_mi] bit DEFAULT ((0)) NULL;
END
GO
IF COL_LENGTH(N'dbo.otel_ozellikleri', N'one_cikan_ozellik') IS NULL
BEGIN
    ALTER TABLE [dbo].[otel_ozellikleri] ADD [one_cikan_ozellik] bit DEFAULT ((0)) NULL;
END
GO
IF COL_LENGTH(N'dbo.otel_ozellikleri', N'siralama') IS NULL
BEGIN
    ALTER TABLE [dbo].[otel_ozellikleri] ADD [siralama] smallint DEFAULT ((0)) NULL;
END
GO
IF COL_LENGTH(N'dbo.otel_ozellikleri', N'aktif_mi') IS NULL
BEGIN
    ALTER TABLE [dbo].[otel_ozellikleri] ADD [aktif_mi] bit DEFAULT ((1)) NULL;
END
GO
