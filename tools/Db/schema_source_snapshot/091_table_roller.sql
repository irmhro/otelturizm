SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.roller', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[roller]
    (
        [id] smallint IDENTITY(1,1) NOT NULL,
        [rol_kodu] nvarchar(30) NOT NULL,
        [rol_adi] nvarchar(50) NOT NULL,
        [departman] nvarchar(50) NOT NULL,
        [seviye] tinyint NOT NULL,
        [ust_rol_id] smallint NULL,
        [varsayilan_mi] bit CONSTRAINT [DF__roller__varsayil__7BB05806] DEFAULT ((0)) NULL,
        [aciklama] nvarchar(255) NULL,
        [olusturulma_tarihi] datetime2(0) CONSTRAINT [DF__roller__olusturu__7CA47C3F] DEFAULT (sysutcdatetime()) NULL,
        CONSTRAINT [PK_roller] PRIMARY KEY CLUSTERED ([id] ASC)
    );
END
GO

-- Missing column repair blocks for existing databases.
IF COL_LENGTH(N'dbo.roller', N'id') IS NULL
BEGIN
    ALTER TABLE [dbo].[roller] ADD [id] smallint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.roller', N'rol_kodu') IS NULL
BEGIN
    ALTER TABLE [dbo].[roller] ADD [rol_kodu] nvarchar(30) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.roller', N'rol_adi') IS NULL
BEGIN
    ALTER TABLE [dbo].[roller] ADD [rol_adi] nvarchar(50) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.roller', N'departman') IS NULL
BEGIN
    ALTER TABLE [dbo].[roller] ADD [departman] nvarchar(50) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.roller', N'seviye') IS NULL
BEGIN
    ALTER TABLE [dbo].[roller] ADD [seviye] tinyint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.roller', N'ust_rol_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[roller] ADD [ust_rol_id] smallint NULL;
END
GO
IF COL_LENGTH(N'dbo.roller', N'varsayilan_mi') IS NULL
BEGIN
    ALTER TABLE [dbo].[roller] ADD [varsayilan_mi] bit DEFAULT ((0)) NULL;
END
GO
IF COL_LENGTH(N'dbo.roller', N'aciklama') IS NULL
BEGIN
    ALTER TABLE [dbo].[roller] ADD [aciklama] nvarchar(255) NULL;
END
GO
IF COL_LENGTH(N'dbo.roller', N'olusturulma_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[roller] ADD [olusturulma_tarihi] datetime2(0) DEFAULT (sysutcdatetime()) NULL;
END
GO
