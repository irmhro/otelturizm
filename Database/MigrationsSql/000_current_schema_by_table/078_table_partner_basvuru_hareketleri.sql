SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.partner_basvuru_hareketleri', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[partner_basvuru_hareketleri]
    (
        [id] bigint IDENTITY(1,1) NOT NULL,
        [partner_id] bigint NOT NULL,
        [onceki_durum] nvarchar(40) NULL,
        [yeni_durum] nvarchar(40) NOT NULL,
        [islem_tipi] nvarchar(60) NOT NULL,
        [aciklama] nvarchar(500) NULL,
        [islem_yapan_kullanici_id] bigint NULL,
        [olusturulma_tarihi] datetime2(0) CONSTRAINT [DF__partner_b__olust__4830B400] DEFAULT (sysutcdatetime()) NOT NULL,
        CONSTRAINT [PK_partner_basvuru_hareketleri] PRIMARY KEY CLUSTERED ([id] ASC)
    );
END
GO

-- Missing column repair blocks for existing databases.
IF COL_LENGTH(N'dbo.partner_basvuru_hareketleri', N'id') IS NULL
BEGIN
    ALTER TABLE [dbo].[partner_basvuru_hareketleri] ADD [id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.partner_basvuru_hareketleri', N'partner_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[partner_basvuru_hareketleri] ADD [partner_id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.partner_basvuru_hareketleri', N'onceki_durum') IS NULL
BEGIN
    ALTER TABLE [dbo].[partner_basvuru_hareketleri] ADD [onceki_durum] nvarchar(40) NULL;
END
GO
IF COL_LENGTH(N'dbo.partner_basvuru_hareketleri', N'yeni_durum') IS NULL
BEGIN
    ALTER TABLE [dbo].[partner_basvuru_hareketleri] ADD [yeni_durum] nvarchar(40) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.partner_basvuru_hareketleri', N'islem_tipi') IS NULL
BEGIN
    ALTER TABLE [dbo].[partner_basvuru_hareketleri] ADD [islem_tipi] nvarchar(60) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.partner_basvuru_hareketleri', N'aciklama') IS NULL
BEGIN
    ALTER TABLE [dbo].[partner_basvuru_hareketleri] ADD [aciklama] nvarchar(500) NULL;
END
GO
IF COL_LENGTH(N'dbo.partner_basvuru_hareketleri', N'islem_yapan_kullanici_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[partner_basvuru_hareketleri] ADD [islem_yapan_kullanici_id] bigint NULL;
END
GO
IF COL_LENGTH(N'dbo.partner_basvuru_hareketleri', N'olusturulma_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[partner_basvuru_hareketleri] ADD [olusturulma_tarihi] datetime2(0) DEFAULT (sysutcdatetime()) NOT NULL;
END
GO
