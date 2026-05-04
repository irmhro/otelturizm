SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.kullanici_departman', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[kullanici_departman]
    (
        [kullanici_id] bigint NOT NULL,
        [departman_id] smallint NOT NULL,
        [unvan] nvarchar(100) NULL,
        [ise_baslama_tarihi] date NULL,
        [yonetici_mi] bit CONSTRAINT [DF__kullanici__yonet__01D345B0] DEFAULT ((0)) NULL,
        CONSTRAINT [PK_kullanici_departman] PRIMARY KEY CLUSTERED ([kullanici_id] ASC, [departman_id] ASC)
    );
END
GO

-- Missing column repair blocks for existing databases.
IF COL_LENGTH(N'dbo.kullanici_departman', N'kullanici_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_departman] ADD [kullanici_id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.kullanici_departman', N'departman_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_departman] ADD [departman_id] smallint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.kullanici_departman', N'unvan') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_departman] ADD [unvan] nvarchar(100) NULL;
END
GO
IF COL_LENGTH(N'dbo.kullanici_departman', N'ise_baslama_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_departman] ADD [ise_baslama_tarihi] date NULL;
END
GO
IF COL_LENGTH(N'dbo.kullanici_departman', N'yonetici_mi') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_departman] ADD [yonetici_mi] bit DEFAULT ((0)) NULL;
END
GO
