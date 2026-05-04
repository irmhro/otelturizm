SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.rol_yetkileri', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[rol_yetkileri]
    (
        [rol_id] smallint NOT NULL,
        [yetki_id] int NOT NULL,
        [izin_var] bit CONSTRAINT [DF__rol_yetki__izin___77DFC722] DEFAULT ((1)) NULL,
        [atayan_kullanici_id] bigint NULL,
        [atama_tarihi] datetime2(0) CONSTRAINT [DF__rol_yetki__atama__78D3EB5B] DEFAULT (sysutcdatetime()) NULL,
        CONSTRAINT [PK_rol_yetkileri] PRIMARY KEY CLUSTERED ([rol_id] ASC, [yetki_id] ASC)
    );
END
GO

-- Missing column repair blocks for existing databases.
IF COL_LENGTH(N'dbo.rol_yetkileri', N'rol_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[rol_yetkileri] ADD [rol_id] smallint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.rol_yetkileri', N'yetki_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[rol_yetkileri] ADD [yetki_id] int NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.rol_yetkileri', N'izin_var') IS NULL
BEGIN
    ALTER TABLE [dbo].[rol_yetkileri] ADD [izin_var] bit DEFAULT ((1)) NULL;
END
GO
IF COL_LENGTH(N'dbo.rol_yetkileri', N'atayan_kullanici_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[rol_yetkileri] ADD [atayan_kullanici_id] bigint NULL;
END
GO
IF COL_LENGTH(N'dbo.rol_yetkileri', N'atama_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[rol_yetkileri] ADD [atama_tarihi] datetime2(0) DEFAULT (sysutcdatetime()) NULL;
END
GO
