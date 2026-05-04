SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.kullanici_rolleri', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[kullanici_rolleri]
    (
        [kullanici_id] bigint NOT NULL,
        [rol_id] smallint NOT NULL,
        [atayan_kullanici_id] bigint NULL,
        [atama_tarihi] datetime2(0) CONSTRAINT [DF__kullanici__atama__1C873BEC] DEFAULT (sysutcdatetime()) NULL,
        [bitis_tarihi] datetime2(0) NULL,
        CONSTRAINT [PK_kullanici_rolleri] PRIMARY KEY CLUSTERED ([kullanici_id] ASC, [rol_id] ASC)
    );
END
GO

-- Missing column repair blocks for existing databases.
IF COL_LENGTH(N'dbo.kullanici_rolleri', N'kullanici_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_rolleri] ADD [kullanici_id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.kullanici_rolleri', N'rol_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_rolleri] ADD [rol_id] smallint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.kullanici_rolleri', N'atayan_kullanici_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_rolleri] ADD [atayan_kullanici_id] bigint NULL;
END
GO
IF COL_LENGTH(N'dbo.kullanici_rolleri', N'atama_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_rolleri] ADD [atama_tarihi] datetime2(0) DEFAULT (sysutcdatetime()) NULL;
END
GO
IF COL_LENGTH(N'dbo.kullanici_rolleri', N'bitis_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_rolleri] ADD [bitis_tarihi] datetime2(0) NULL;
END
GO
