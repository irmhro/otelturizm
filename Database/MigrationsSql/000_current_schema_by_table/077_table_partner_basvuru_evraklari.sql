SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.partner_basvuru_evraklari', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[partner_basvuru_evraklari]
    (
        [id] bigint IDENTITY(1,1) NOT NULL,
        [partner_id] bigint NOT NULL,
        [guvenli_dosya_id] bigint NOT NULL,
        [evrak_tipi] nvarchar(80) NOT NULL,
        [belge_basligi] nvarchar(150) NULL,
        [durum] nvarchar(255) NOT NULL,
        [red_nedeni] nvarchar(500) NULL,
        [yukleyen_kullanici_id] bigint NULL,
        [inceleyen_admin_id] bigint NULL,
        [incelenme_tarihi] datetime2(0) NULL,
        [olusturulma_tarihi] datetime2(0) CONSTRAINT [DF__partner_b__olust__45544755] DEFAULT (sysutcdatetime()) NOT NULL,
        [guncellenme_tarihi] datetime2(0) NULL,
        CONSTRAINT [PK_partner_basvuru_evraklari] PRIMARY KEY CLUSTERED ([id] ASC)
    );
END
GO

-- Missing column repair blocks for existing databases.
IF COL_LENGTH(N'dbo.partner_basvuru_evraklari', N'id') IS NULL
BEGIN
    ALTER TABLE [dbo].[partner_basvuru_evraklari] ADD [id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.partner_basvuru_evraklari', N'partner_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[partner_basvuru_evraklari] ADD [partner_id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.partner_basvuru_evraklari', N'guvenli_dosya_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[partner_basvuru_evraklari] ADD [guvenli_dosya_id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.partner_basvuru_evraklari', N'evrak_tipi') IS NULL
BEGIN
    ALTER TABLE [dbo].[partner_basvuru_evraklari] ADD [evrak_tipi] nvarchar(80) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.partner_basvuru_evraklari', N'belge_basligi') IS NULL
BEGIN
    ALTER TABLE [dbo].[partner_basvuru_evraklari] ADD [belge_basligi] nvarchar(150) NULL;
END
GO
IF COL_LENGTH(N'dbo.partner_basvuru_evraklari', N'durum') IS NULL
BEGIN
    ALTER TABLE [dbo].[partner_basvuru_evraklari] ADD [durum] nvarchar(255) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.partner_basvuru_evraklari', N'red_nedeni') IS NULL
BEGIN
    ALTER TABLE [dbo].[partner_basvuru_evraklari] ADD [red_nedeni] nvarchar(500) NULL;
END
GO
IF COL_LENGTH(N'dbo.partner_basvuru_evraklari', N'yukleyen_kullanici_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[partner_basvuru_evraklari] ADD [yukleyen_kullanici_id] bigint NULL;
END
GO
IF COL_LENGTH(N'dbo.partner_basvuru_evraklari', N'inceleyen_admin_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[partner_basvuru_evraklari] ADD [inceleyen_admin_id] bigint NULL;
END
GO
IF COL_LENGTH(N'dbo.partner_basvuru_evraklari', N'incelenme_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[partner_basvuru_evraklari] ADD [incelenme_tarihi] datetime2(0) NULL;
END
GO
IF COL_LENGTH(N'dbo.partner_basvuru_evraklari', N'olusturulma_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[partner_basvuru_evraklari] ADD [olusturulma_tarihi] datetime2(0) DEFAULT (sysutcdatetime()) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.partner_basvuru_evraklari', N'guncellenme_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[partner_basvuru_evraklari] ADD [guncellenme_tarihi] datetime2(0) NULL;
END
GO
