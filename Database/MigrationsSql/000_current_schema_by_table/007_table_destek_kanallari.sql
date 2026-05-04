SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.destek_kanallari', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[destek_kanallari]
    (
        [id] bigint IDENTITY(1,1) NOT NULL,
        [kanal_adi] nvarchar(120) NOT NULL,
        [kanal_turu] nvarchar(40) NOT NULL,
        [ikon] nvarchar(80) NOT NULL,
        [aciklama] nvarchar(255) NOT NULL,
        [buton_metin] nvarchar(120) NOT NULL,
        [baglanti_url] nvarchar(255) NOT NULL,
        [ek_bilgi] nvarchar(180) NULL,
        [renk_tonu] nvarchar(30) NOT NULL,
        [siralama] int CONSTRAINT [DF__destek_ka__siral__06CD04F7] DEFAULT ((0)) NOT NULL,
        [aktif_mi] bit CONSTRAINT [DF__destek_ka__aktif__07C12930] DEFAULT ((1)) NOT NULL,
        [olusturulma_tarihi] datetime2(0) CONSTRAINT [DF__destek_ka__olust__08B54D69] DEFAULT (sysutcdatetime()) NOT NULL,
        [guncellenme_tarihi] datetime2(0) CONSTRAINT [DF__destek_ka__gunce__09A971A2] DEFAULT (sysutcdatetime()) NOT NULL,
        CONSTRAINT [PK_destek_kanallari] PRIMARY KEY CLUSTERED ([id] ASC)
    );
END
GO

-- Missing column repair blocks for existing databases.
IF COL_LENGTH(N'dbo.destek_kanallari', N'id') IS NULL
BEGIN
    ALTER TABLE [dbo].[destek_kanallari] ADD [id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.destek_kanallari', N'kanal_adi') IS NULL
BEGIN
    ALTER TABLE [dbo].[destek_kanallari] ADD [kanal_adi] nvarchar(120) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.destek_kanallari', N'kanal_turu') IS NULL
BEGIN
    ALTER TABLE [dbo].[destek_kanallari] ADD [kanal_turu] nvarchar(40) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.destek_kanallari', N'ikon') IS NULL
BEGIN
    ALTER TABLE [dbo].[destek_kanallari] ADD [ikon] nvarchar(80) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.destek_kanallari', N'aciklama') IS NULL
BEGIN
    ALTER TABLE [dbo].[destek_kanallari] ADD [aciklama] nvarchar(255) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.destek_kanallari', N'buton_metin') IS NULL
BEGIN
    ALTER TABLE [dbo].[destek_kanallari] ADD [buton_metin] nvarchar(120) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.destek_kanallari', N'baglanti_url') IS NULL
BEGIN
    ALTER TABLE [dbo].[destek_kanallari] ADD [baglanti_url] nvarchar(255) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.destek_kanallari', N'ek_bilgi') IS NULL
BEGIN
    ALTER TABLE [dbo].[destek_kanallari] ADD [ek_bilgi] nvarchar(180) NULL;
END
GO
IF COL_LENGTH(N'dbo.destek_kanallari', N'renk_tonu') IS NULL
BEGIN
    ALTER TABLE [dbo].[destek_kanallari] ADD [renk_tonu] nvarchar(30) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.destek_kanallari', N'siralama') IS NULL
BEGIN
    ALTER TABLE [dbo].[destek_kanallari] ADD [siralama] int DEFAULT ((0)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.destek_kanallari', N'aktif_mi') IS NULL
BEGIN
    ALTER TABLE [dbo].[destek_kanallari] ADD [aktif_mi] bit DEFAULT ((1)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.destek_kanallari', N'olusturulma_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[destek_kanallari] ADD [olusturulma_tarihi] datetime2(0) DEFAULT (sysutcdatetime()) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.destek_kanallari', N'guncellenme_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[destek_kanallari] ADD [guncellenme_tarihi] datetime2(0) DEFAULT (sysutcdatetime()) NOT NULL;
END
GO
