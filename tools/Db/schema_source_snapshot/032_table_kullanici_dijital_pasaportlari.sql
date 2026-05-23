SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.kullanici_dijital_pasaportlari', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[kullanici_dijital_pasaportlari]
    (
        [id] bigint IDENTITY(1,1) NOT NULL,
        [kullanici_id] bigint NOT NULL,
        [sehir] nvarchar(120) NOT NULL,
        [ulke] nvarchar(120) NULL,
        [ilk_konaklama_tarihi] date NULL,
        [son_konaklama_tarihi] date NULL,
        [toplam_konaklama_sayisi] int CONSTRAINT [DF__kullanici__topla__04AFB25B] DEFAULT ((0)) NOT NULL,
        [damga_kodu] nvarchar(80) NULL,
        [olusturulma_tarihi] datetime2(0) CONSTRAINT [DF__kullanici__olust__05A3D694] DEFAULT (sysutcdatetime()) NOT NULL,
        [guncellenme_tarihi] datetime2(0) CONSTRAINT [DF__kullanici__gunce__0697FACD] DEFAULT (sysutcdatetime()) NOT NULL,
        CONSTRAINT [PK_kullanici_dijital_pasaportlari] PRIMARY KEY CLUSTERED ([id] ASC)
    );
END
GO

-- Missing column repair blocks for existing databases.
IF COL_LENGTH(N'dbo.kullanici_dijital_pasaportlari', N'id') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_dijital_pasaportlari] ADD [id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.kullanici_dijital_pasaportlari', N'kullanici_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_dijital_pasaportlari] ADD [kullanici_id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.kullanici_dijital_pasaportlari', N'sehir') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_dijital_pasaportlari] ADD [sehir] nvarchar(120) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.kullanici_dijital_pasaportlari', N'ulke') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_dijital_pasaportlari] ADD [ulke] nvarchar(120) NULL;
END
GO
IF COL_LENGTH(N'dbo.kullanici_dijital_pasaportlari', N'ilk_konaklama_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_dijital_pasaportlari] ADD [ilk_konaklama_tarihi] date NULL;
END
GO
IF COL_LENGTH(N'dbo.kullanici_dijital_pasaportlari', N'son_konaklama_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_dijital_pasaportlari] ADD [son_konaklama_tarihi] date NULL;
END
GO
IF COL_LENGTH(N'dbo.kullanici_dijital_pasaportlari', N'toplam_konaklama_sayisi') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_dijital_pasaportlari] ADD [toplam_konaklama_sayisi] int DEFAULT ((0)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.kullanici_dijital_pasaportlari', N'damga_kodu') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_dijital_pasaportlari] ADD [damga_kodu] nvarchar(80) NULL;
END
GO
IF COL_LENGTH(N'dbo.kullanici_dijital_pasaportlari', N'olusturulma_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_dijital_pasaportlari] ADD [olusturulma_tarihi] datetime2(0) DEFAULT (sysutcdatetime()) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.kullanici_dijital_pasaportlari', N'guncellenme_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_dijital_pasaportlari] ADD [guncellenme_tarihi] datetime2(0) DEFAULT (sysutcdatetime()) NOT NULL;
END
GO
