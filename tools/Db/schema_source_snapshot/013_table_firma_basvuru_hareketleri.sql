SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.firma_basvuru_hareketleri', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[firma_basvuru_hareketleri]
    (
        [id] bigint IDENTITY(1,1) NOT NULL,
        [firma_id] bigint NOT NULL,
        [onceki_durum] nvarchar(255) NULL,
        [yeni_durum] nvarchar(255) NOT NULL,
        [hareket_tipi] nvarchar(255) NOT NULL,
        [aciklama] nvarchar(max) NULL,
        [islem_yapan_kullanici_id] bigint NULL,
        [islem_kaynagi] nvarchar(50) NOT NULL,
        [ip_adresi] nvarchar(45) NULL,
        [olusturulma_tarihi] datetime2(0) CONSTRAINT [DF__firma_bas__olust__32AB8735] DEFAULT (sysutcdatetime()) NULL,
        CONSTRAINT [PK_firma_basvuru_hareketleri] PRIMARY KEY CLUSTERED ([id] ASC)
    );
END
GO

-- Missing column repair blocks for existing databases.
IF COL_LENGTH(N'dbo.firma_basvuru_hareketleri', N'id') IS NULL
BEGIN
    ALTER TABLE [dbo].[firma_basvuru_hareketleri] ADD [id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.firma_basvuru_hareketleri', N'firma_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[firma_basvuru_hareketleri] ADD [firma_id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.firma_basvuru_hareketleri', N'onceki_durum') IS NULL
BEGIN
    ALTER TABLE [dbo].[firma_basvuru_hareketleri] ADD [onceki_durum] nvarchar(255) NULL;
END
GO
IF COL_LENGTH(N'dbo.firma_basvuru_hareketleri', N'yeni_durum') IS NULL
BEGIN
    ALTER TABLE [dbo].[firma_basvuru_hareketleri] ADD [yeni_durum] nvarchar(255) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.firma_basvuru_hareketleri', N'hareket_tipi') IS NULL
BEGIN
    ALTER TABLE [dbo].[firma_basvuru_hareketleri] ADD [hareket_tipi] nvarchar(255) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.firma_basvuru_hareketleri', N'aciklama') IS NULL
BEGIN
    ALTER TABLE [dbo].[firma_basvuru_hareketleri] ADD [aciklama] nvarchar(max) NULL;
END
GO
IF COL_LENGTH(N'dbo.firma_basvuru_hareketleri', N'islem_yapan_kullanici_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[firma_basvuru_hareketleri] ADD [islem_yapan_kullanici_id] bigint NULL;
END
GO
IF COL_LENGTH(N'dbo.firma_basvuru_hareketleri', N'islem_kaynagi') IS NULL
BEGIN
    ALTER TABLE [dbo].[firma_basvuru_hareketleri] ADD [islem_kaynagi] nvarchar(50) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.firma_basvuru_hareketleri', N'ip_adresi') IS NULL
BEGIN
    ALTER TABLE [dbo].[firma_basvuru_hareketleri] ADD [ip_adresi] nvarchar(45) NULL;
END
GO
IF COL_LENGTH(N'dbo.firma_basvuru_hareketleri', N'olusturulma_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[firma_basvuru_hareketleri] ADD [olusturulma_tarihi] datetime2(0) DEFAULT (sysutcdatetime()) NULL;
END
GO
