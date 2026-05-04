SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.rezervasyonlar_archive', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[rezervasyonlar_archive]
    (
        [id] bigint NOT NULL,
        [olusturulma_tarihi] datetime2(7) NULL,
        [durum] nvarchar(64) NULL,
        [arsiv_tarihi_utc] datetime2(7) CONSTRAINT [DF_rezervasyonlar_archive_arsiv] DEFAULT (sysutcdatetime()) NOT NULL,
        CONSTRAINT [PK__rezervas__3213E83FB099BEF4] PRIMARY KEY CLUSTERED ([id] ASC)
    );
END
GO

-- Missing column repair blocks for existing databases.
IF COL_LENGTH(N'dbo.rezervasyonlar_archive', N'id') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyonlar_archive] ADD [id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.rezervasyonlar_archive', N'olusturulma_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyonlar_archive] ADD [olusturulma_tarihi] datetime2(7) NULL;
END
GO
IF COL_LENGTH(N'dbo.rezervasyonlar_archive', N'durum') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyonlar_archive] ADD [durum] nvarchar(64) NULL;
END
GO
IF COL_LENGTH(N'dbo.rezervasyonlar_archive', N'arsiv_tarihi_utc') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyonlar_archive] ADD [arsiv_tarihi_utc] datetime2(7) DEFAULT (sysutcdatetime()) NOT NULL;
END
GO
