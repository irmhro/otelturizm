SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.mesaj_konusmalari', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[mesaj_konusmalari]
    (
        [id] bigint IDENTITY(1,1) NOT NULL,
        [konusma_kodu] nvarchar(20) NOT NULL,
        [rezervasyon_id] bigint NULL,
        [otel_id] bigint NULL,
        [firma_id] bigint NULL,
        [firma_kullanici_id] bigint NULL,
        [misafir_kullanici_id] bigint NOT NULL,
        [otel_yetkilisi_kullanici_id] bigint NULL,
        [konu_basligi] nvarchar(200) NOT NULL,
        [konusma_turu] nvarchar(255) NOT NULL,
        [konu_kategorisi] nvarchar(255) NULL,
        [durum] nvarchar(255) NULL,
        [oncelik] nvarchar(255) NULL,
        [son_mesaj_tarihi] datetime2(0) NULL,
        [son_mesaj_gonderen] nvarchar(255) NULL,
        [son_mesaj_onizleme] nvarchar(100) NULL,
        [misafir_okunmamis_sayisi] int CONSTRAINT [DF__mesaj_kon__misaf__3BFFE745] DEFAULT ((0)) NULL,
        [otel_okunmamis_sayisi] int CONSTRAINT [DF__mesaj_kon__otel___3CF40B7E] DEFAULT ((0)) NULL,
        [firma_okunmamis_sayisi] int CONSTRAINT [DF__mesaj_kon__firma__3DE82FB7] DEFAULT ((0)) NOT NULL,
        [misafir_son_okuma_tarihi] datetime2(0) NULL,
        [otel_son_okuma_tarihi] datetime2(0) NULL,
        [firma_son_okuma_tarihi] datetime2(0) NULL,
        [etiketler] nvarchar(max) NULL,
        [atanan_destek_ekibi_kullanici_id] bigint NULL,
        [olusturulma_tarihi] datetime2(0) CONSTRAINT [DF__mesaj_kon__olust__3EDC53F0] DEFAULT (sysutcdatetime()) NULL,
        [guncellenme_tarihi] datetime2(0) NULL,
        [kapatilma_tarihi] datetime2(0) NULL,
        [kapatma_nedeni] nvarchar(255) NULL,
        CONSTRAINT [PK_mesaj_konusmalari] PRIMARY KEY CLUSTERED ([id] ASC)
    );
END
GO

-- Missing column repair blocks for existing databases.
IF COL_LENGTH(N'dbo.mesaj_konusmalari', N'id') IS NULL
BEGIN
    ALTER TABLE [dbo].[mesaj_konusmalari] ADD [id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.mesaj_konusmalari', N'konusma_kodu') IS NULL
BEGIN
    ALTER TABLE [dbo].[mesaj_konusmalari] ADD [konusma_kodu] nvarchar(20) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.mesaj_konusmalari', N'rezervasyon_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[mesaj_konusmalari] ADD [rezervasyon_id] bigint NULL;
END
GO
IF COL_LENGTH(N'dbo.mesaj_konusmalari', N'otel_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[mesaj_konusmalari] ADD [otel_id] bigint NULL;
END
GO
IF COL_LENGTH(N'dbo.mesaj_konusmalari', N'firma_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[mesaj_konusmalari] ADD [firma_id] bigint NULL;
END
GO
IF COL_LENGTH(N'dbo.mesaj_konusmalari', N'firma_kullanici_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[mesaj_konusmalari] ADD [firma_kullanici_id] bigint NULL;
END
GO
IF COL_LENGTH(N'dbo.mesaj_konusmalari', N'misafir_kullanici_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[mesaj_konusmalari] ADD [misafir_kullanici_id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.mesaj_konusmalari', N'otel_yetkilisi_kullanici_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[mesaj_konusmalari] ADD [otel_yetkilisi_kullanici_id] bigint NULL;
END
GO
IF COL_LENGTH(N'dbo.mesaj_konusmalari', N'konu_basligi') IS NULL
BEGIN
    ALTER TABLE [dbo].[mesaj_konusmalari] ADD [konu_basligi] nvarchar(200) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.mesaj_konusmalari', N'konusma_turu') IS NULL
BEGIN
    ALTER TABLE [dbo].[mesaj_konusmalari] ADD [konusma_turu] nvarchar(255) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.mesaj_konusmalari', N'konu_kategorisi') IS NULL
BEGIN
    ALTER TABLE [dbo].[mesaj_konusmalari] ADD [konu_kategorisi] nvarchar(255) NULL;
END
GO
IF COL_LENGTH(N'dbo.mesaj_konusmalari', N'durum') IS NULL
BEGIN
    ALTER TABLE [dbo].[mesaj_konusmalari] ADD [durum] nvarchar(255) NULL;
END
GO
IF COL_LENGTH(N'dbo.mesaj_konusmalari', N'oncelik') IS NULL
BEGIN
    ALTER TABLE [dbo].[mesaj_konusmalari] ADD [oncelik] nvarchar(255) NULL;
END
GO
IF COL_LENGTH(N'dbo.mesaj_konusmalari', N'son_mesaj_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[mesaj_konusmalari] ADD [son_mesaj_tarihi] datetime2(0) NULL;
END
GO
IF COL_LENGTH(N'dbo.mesaj_konusmalari', N'son_mesaj_gonderen') IS NULL
BEGIN
    ALTER TABLE [dbo].[mesaj_konusmalari] ADD [son_mesaj_gonderen] nvarchar(255) NULL;
END
GO
IF COL_LENGTH(N'dbo.mesaj_konusmalari', N'son_mesaj_onizleme') IS NULL
BEGIN
    ALTER TABLE [dbo].[mesaj_konusmalari] ADD [son_mesaj_onizleme] nvarchar(100) NULL;
END
GO
IF COL_LENGTH(N'dbo.mesaj_konusmalari', N'misafir_okunmamis_sayisi') IS NULL
BEGIN
    ALTER TABLE [dbo].[mesaj_konusmalari] ADD [misafir_okunmamis_sayisi] int DEFAULT ((0)) NULL;
END
GO
IF COL_LENGTH(N'dbo.mesaj_konusmalari', N'otel_okunmamis_sayisi') IS NULL
BEGIN
    ALTER TABLE [dbo].[mesaj_konusmalari] ADD [otel_okunmamis_sayisi] int DEFAULT ((0)) NULL;
END
GO
IF COL_LENGTH(N'dbo.mesaj_konusmalari', N'firma_okunmamis_sayisi') IS NULL
BEGIN
    ALTER TABLE [dbo].[mesaj_konusmalari] ADD [firma_okunmamis_sayisi] int DEFAULT ((0)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.mesaj_konusmalari', N'misafir_son_okuma_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[mesaj_konusmalari] ADD [misafir_son_okuma_tarihi] datetime2(0) NULL;
END
GO
IF COL_LENGTH(N'dbo.mesaj_konusmalari', N'otel_son_okuma_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[mesaj_konusmalari] ADD [otel_son_okuma_tarihi] datetime2(0) NULL;
END
GO
IF COL_LENGTH(N'dbo.mesaj_konusmalari', N'firma_son_okuma_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[mesaj_konusmalari] ADD [firma_son_okuma_tarihi] datetime2(0) NULL;
END
GO
IF COL_LENGTH(N'dbo.mesaj_konusmalari', N'etiketler') IS NULL
BEGIN
    ALTER TABLE [dbo].[mesaj_konusmalari] ADD [etiketler] nvarchar(max) NULL;
END
GO
IF COL_LENGTH(N'dbo.mesaj_konusmalari', N'atanan_destek_ekibi_kullanici_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[mesaj_konusmalari] ADD [atanan_destek_ekibi_kullanici_id] bigint NULL;
END
GO
IF COL_LENGTH(N'dbo.mesaj_konusmalari', N'olusturulma_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[mesaj_konusmalari] ADD [olusturulma_tarihi] datetime2(0) DEFAULT (sysutcdatetime()) NULL;
END
GO
IF COL_LENGTH(N'dbo.mesaj_konusmalari', N'guncellenme_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[mesaj_konusmalari] ADD [guncellenme_tarihi] datetime2(0) NULL;
END
GO
IF COL_LENGTH(N'dbo.mesaj_konusmalari', N'kapatilma_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[mesaj_konusmalari] ADD [kapatilma_tarihi] datetime2(0) NULL;
END
GO
IF COL_LENGTH(N'dbo.mesaj_konusmalari', N'kapatma_nedeni') IS NULL
BEGIN
    ALTER TABLE [dbo].[mesaj_konusmalari] ADD [kapatma_nedeni] nvarchar(255) NULL;
END
GO
