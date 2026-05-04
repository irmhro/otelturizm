SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.bildirim_sablonlari', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[bildirim_sablonlari]
    (
        [id] smallint IDENTITY(1,1) NOT NULL,
        [sablon_kodu] nvarchar(50) NOT NULL,
        [sablon_adi] nvarchar(100) NOT NULL,
        [tur] nvarchar(255) NOT NULL,
        [dil] nvarchar(5) NOT NULL,
        [konu] nvarchar(200) NULL,
        [baslik] nvarchar(100) NULL,
        [icerik] nvarchar(max) NOT NULL,
        [degiskenler] nvarchar(max) NULL,
        [aktif_mi] bit CONSTRAINT [DF__bildirim___aktif__7F2BE32F] DEFAULT ((1)) NULL,
        [olusturulma_tarihi] datetime2(0) CONSTRAINT [DF__bildirim___olust__00200768] DEFAULT (sysutcdatetime()) NULL,
        CONSTRAINT [PK_bildirim_sablonlari] PRIMARY KEY CLUSTERED ([id] ASC)
    );
END
GO

-- Missing column repair blocks for existing databases.
IF COL_LENGTH(N'dbo.bildirim_sablonlari', N'id') IS NULL
BEGIN
    ALTER TABLE [dbo].[bildirim_sablonlari] ADD [id] smallint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.bildirim_sablonlari', N'sablon_kodu') IS NULL
BEGIN
    ALTER TABLE [dbo].[bildirim_sablonlari] ADD [sablon_kodu] nvarchar(50) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.bildirim_sablonlari', N'sablon_adi') IS NULL
BEGIN
    ALTER TABLE [dbo].[bildirim_sablonlari] ADD [sablon_adi] nvarchar(100) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.bildirim_sablonlari', N'tur') IS NULL
BEGIN
    ALTER TABLE [dbo].[bildirim_sablonlari] ADD [tur] nvarchar(255) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.bildirim_sablonlari', N'dil') IS NULL
BEGIN
    ALTER TABLE [dbo].[bildirim_sablonlari] ADD [dil] nvarchar(5) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.bildirim_sablonlari', N'konu') IS NULL
BEGIN
    ALTER TABLE [dbo].[bildirim_sablonlari] ADD [konu] nvarchar(200) NULL;
END
GO
IF COL_LENGTH(N'dbo.bildirim_sablonlari', N'baslik') IS NULL
BEGIN
    ALTER TABLE [dbo].[bildirim_sablonlari] ADD [baslik] nvarchar(100) NULL;
END
GO
IF COL_LENGTH(N'dbo.bildirim_sablonlari', N'icerik') IS NULL
BEGIN
    ALTER TABLE [dbo].[bildirim_sablonlari] ADD [icerik] nvarchar(max) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.bildirim_sablonlari', N'degiskenler') IS NULL
BEGIN
    ALTER TABLE [dbo].[bildirim_sablonlari] ADD [degiskenler] nvarchar(max) NULL;
END
GO
IF COL_LENGTH(N'dbo.bildirim_sablonlari', N'aktif_mi') IS NULL
BEGIN
    ALTER TABLE [dbo].[bildirim_sablonlari] ADD [aktif_mi] bit DEFAULT ((1)) NULL;
END
GO
IF COL_LENGTH(N'dbo.bildirim_sablonlari', N'olusturulma_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[bildirim_sablonlari] ADD [olusturulma_tarihi] datetime2(0) DEFAULT (sysutcdatetime()) NULL;
END
GO
