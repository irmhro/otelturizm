SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.departmanlar', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[departmanlar]
    (
        [id] smallint IDENTITY(1,1) NOT NULL,
        [departman_kodu] nvarchar(30) NOT NULL,
        [departman_adi] nvarchar(50) NOT NULL,
        [ust_departman_id] smallint NULL,
        [yonetici_rol_id] smallint NULL,
        [bina_kat] nvarchar(20) NULL,
        [dahili_telefon] nvarchar(10) NULL,
        [aciklama] nvarchar(255) NULL,
        [aktif_mi] bit CONSTRAINT [DF__departman__aktif__02FC7413] DEFAULT ((1)) NULL,
        [olusturulma_tarihi] datetime2(0) CONSTRAINT [DF__departman__olust__03F0984C] DEFAULT (sysutcdatetime()) NULL,
        CONSTRAINT [PK_departmanlar] PRIMARY KEY CLUSTERED ([id] ASC)
    );
END
GO

-- Missing column repair blocks for existing databases.
IF COL_LENGTH(N'dbo.departmanlar', N'id') IS NULL
BEGIN
    ALTER TABLE [dbo].[departmanlar] ADD [id] smallint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.departmanlar', N'departman_kodu') IS NULL
BEGIN
    ALTER TABLE [dbo].[departmanlar] ADD [departman_kodu] nvarchar(30) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.departmanlar', N'departman_adi') IS NULL
BEGIN
    ALTER TABLE [dbo].[departmanlar] ADD [departman_adi] nvarchar(50) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.departmanlar', N'ust_departman_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[departmanlar] ADD [ust_departman_id] smallint NULL;
END
GO
IF COL_LENGTH(N'dbo.departmanlar', N'yonetici_rol_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[departmanlar] ADD [yonetici_rol_id] smallint NULL;
END
GO
IF COL_LENGTH(N'dbo.departmanlar', N'bina_kat') IS NULL
BEGIN
    ALTER TABLE [dbo].[departmanlar] ADD [bina_kat] nvarchar(20) NULL;
END
GO
IF COL_LENGTH(N'dbo.departmanlar', N'dahili_telefon') IS NULL
BEGIN
    ALTER TABLE [dbo].[departmanlar] ADD [dahili_telefon] nvarchar(10) NULL;
END
GO
IF COL_LENGTH(N'dbo.departmanlar', N'aciklama') IS NULL
BEGIN
    ALTER TABLE [dbo].[departmanlar] ADD [aciklama] nvarchar(255) NULL;
END
GO
IF COL_LENGTH(N'dbo.departmanlar', N'aktif_mi') IS NULL
BEGIN
    ALTER TABLE [dbo].[departmanlar] ADD [aktif_mi] bit DEFAULT ((1)) NULL;
END
GO
IF COL_LENGTH(N'dbo.departmanlar', N'olusturulma_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[departmanlar] ADD [olusturulma_tarihi] datetime2(0) DEFAULT (sysutcdatetime()) NULL;
END
GO
