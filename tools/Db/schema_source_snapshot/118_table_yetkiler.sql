SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.yetkiler', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[yetkiler]
    (
        [id] int IDENTITY(1,1) NOT NULL,
        [yetki_kodu] nvarchar(100) NOT NULL,
        [modul] nvarchar(50) NOT NULL,
        [eylem] nvarchar(50) NOT NULL,
        [aciklama] nvarchar(255) NULL,
        [varsayilan_izin] bit CONSTRAINT [DF__yetkiler__varsay__6497E884] DEFAULT ((0)) NULL,
        [olusturulma_tarihi] datetime2(0) CONSTRAINT [DF__yetkiler__olustu__658C0CBD] DEFAULT (sysutcdatetime()) NULL,
        CONSTRAINT [PK_yetkiler] PRIMARY KEY CLUSTERED ([id] ASC)
    );
END
GO

-- Missing column repair blocks for existing databases.
IF COL_LENGTH(N'dbo.yetkiler', N'id') IS NULL
BEGIN
    ALTER TABLE [dbo].[yetkiler] ADD [id] int NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.yetkiler', N'yetki_kodu') IS NULL
BEGIN
    ALTER TABLE [dbo].[yetkiler] ADD [yetki_kodu] nvarchar(100) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.yetkiler', N'modul') IS NULL
BEGIN
    ALTER TABLE [dbo].[yetkiler] ADD [modul] nvarchar(50) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.yetkiler', N'eylem') IS NULL
BEGIN
    ALTER TABLE [dbo].[yetkiler] ADD [eylem] nvarchar(50) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.yetkiler', N'aciklama') IS NULL
BEGIN
    ALTER TABLE [dbo].[yetkiler] ADD [aciklama] nvarchar(255) NULL;
END
GO
IF COL_LENGTH(N'dbo.yetkiler', N'varsayilan_izin') IS NULL
BEGIN
    ALTER TABLE [dbo].[yetkiler] ADD [varsayilan_izin] bit DEFAULT ((0)) NULL;
END
GO
IF COL_LENGTH(N'dbo.yetkiler', N'olusturulma_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[yetkiler] ADD [olusturulma_tarihi] datetime2(0) DEFAULT (sysutcdatetime()) NULL;
END
GO
