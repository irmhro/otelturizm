SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.sepet_blokajlari', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[sepet_blokajlari]
    (
        [id] bigint IDENTITY(1,1) NOT NULL,
        [blokaj_kodu] nvarchar(30) NOT NULL,
        [otel_id] bigint NOT NULL,
        [oda_tip_id] bigint NOT NULL,
        [kullanici_id] bigint NULL,
        [session_id] nvarchar(100) NOT NULL,
        [giris_tarihi] date NOT NULL,
        [cikis_tarihi] date NOT NULL,
        [oda_sayisi] tinyint CONSTRAINT [DF__sepet_blo__oda_s__1B29035F] DEFAULT ((1)) NULL,
        [yetiskin_sayisi] tinyint NOT NULL,
        [cocuk_sayisi] tinyint CONSTRAINT [DF__sepet_blo__cocuk__1C1D2798] DEFAULT ((0)) NULL,
        [gecelik_fiyat] decimal(10,2) NOT NULL,
        [toplam_tutar] decimal(10,2) NOT NULL,
        [para_birimi] nvarchar(3) NULL,
        [durum] nvarchar(255) NULL,
        [rezervasyon_id] bigint NULL,
        [blokaj_baslangic_tarihi] datetime2(0) CONSTRAINT [DF__sepet_blo__bloka__1D114BD1] DEFAULT (sysutcdatetime()) NULL,
        [blokaj_bitis_tarihi] datetime2(0) NULL,
        [sure_dakika] smallint CONSTRAINT [DF__sepet_blo__sure___1E05700A] DEFAULT ((15)) NULL,
        [hatirlatma_gonderildi_mi] bit CONSTRAINT [DF__sepet_blo__hatir__1EF99443] DEFAULT ((0)) NULL,
        [hatirlatma_gonderilme_tarihi] datetime2(0) NULL,
        [ip_adresi] nvarchar(45) NULL,
        CONSTRAINT [PK_sepet_blokajlari] PRIMARY KEY CLUSTERED ([id] ASC)
    );
END
GO

-- Missing column repair blocks for existing databases.
IF COL_LENGTH(N'dbo.sepet_blokajlari', N'id') IS NULL
BEGIN
    ALTER TABLE [dbo].[sepet_blokajlari] ADD [id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.sepet_blokajlari', N'blokaj_kodu') IS NULL
BEGIN
    ALTER TABLE [dbo].[sepet_blokajlari] ADD [blokaj_kodu] nvarchar(30) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.sepet_blokajlari', N'otel_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[sepet_blokajlari] ADD [otel_id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.sepet_blokajlari', N'oda_tip_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[sepet_blokajlari] ADD [oda_tip_id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.sepet_blokajlari', N'kullanici_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[sepet_blokajlari] ADD [kullanici_id] bigint NULL;
END
GO
IF COL_LENGTH(N'dbo.sepet_blokajlari', N'session_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[sepet_blokajlari] ADD [session_id] nvarchar(100) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.sepet_blokajlari', N'giris_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[sepet_blokajlari] ADD [giris_tarihi] date NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.sepet_blokajlari', N'cikis_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[sepet_blokajlari] ADD [cikis_tarihi] date NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.sepet_blokajlari', N'oda_sayisi') IS NULL
BEGIN
    ALTER TABLE [dbo].[sepet_blokajlari] ADD [oda_sayisi] tinyint DEFAULT ((1)) NULL;
END
GO
IF COL_LENGTH(N'dbo.sepet_blokajlari', N'yetiskin_sayisi') IS NULL
BEGIN
    ALTER TABLE [dbo].[sepet_blokajlari] ADD [yetiskin_sayisi] tinyint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.sepet_blokajlari', N'cocuk_sayisi') IS NULL
BEGIN
    ALTER TABLE [dbo].[sepet_blokajlari] ADD [cocuk_sayisi] tinyint DEFAULT ((0)) NULL;
END
GO
IF COL_LENGTH(N'dbo.sepet_blokajlari', N'gecelik_fiyat') IS NULL
BEGIN
    ALTER TABLE [dbo].[sepet_blokajlari] ADD [gecelik_fiyat] decimal(10,2) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.sepet_blokajlari', N'toplam_tutar') IS NULL
BEGIN
    ALTER TABLE [dbo].[sepet_blokajlari] ADD [toplam_tutar] decimal(10,2) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.sepet_blokajlari', N'para_birimi') IS NULL
BEGIN
    ALTER TABLE [dbo].[sepet_blokajlari] ADD [para_birimi] nvarchar(3) NULL;
END
GO
IF COL_LENGTH(N'dbo.sepet_blokajlari', N'durum') IS NULL
BEGIN
    ALTER TABLE [dbo].[sepet_blokajlari] ADD [durum] nvarchar(255) NULL;
END
GO
IF COL_LENGTH(N'dbo.sepet_blokajlari', N'rezervasyon_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[sepet_blokajlari] ADD [rezervasyon_id] bigint NULL;
END
GO
IF COL_LENGTH(N'dbo.sepet_blokajlari', N'blokaj_baslangic_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[sepet_blokajlari] ADD [blokaj_baslangic_tarihi] datetime2(0) DEFAULT (sysutcdatetime()) NULL;
END
GO
IF COL_LENGTH(N'dbo.sepet_blokajlari', N'blokaj_bitis_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[sepet_blokajlari] ADD [blokaj_bitis_tarihi] datetime2(0) NULL;
END
GO
IF COL_LENGTH(N'dbo.sepet_blokajlari', N'sure_dakika') IS NULL
BEGIN
    ALTER TABLE [dbo].[sepet_blokajlari] ADD [sure_dakika] smallint DEFAULT ((15)) NULL;
END
GO
IF COL_LENGTH(N'dbo.sepet_blokajlari', N'hatirlatma_gonderildi_mi') IS NULL
BEGIN
    ALTER TABLE [dbo].[sepet_blokajlari] ADD [hatirlatma_gonderildi_mi] bit DEFAULT ((0)) NULL;
END
GO
IF COL_LENGTH(N'dbo.sepet_blokajlari', N'hatirlatma_gonderilme_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[sepet_blokajlari] ADD [hatirlatma_gonderilme_tarihi] datetime2(0) NULL;
END
GO
IF COL_LENGTH(N'dbo.sepet_blokajlari', N'ip_adresi') IS NULL
BEGIN
    ALTER TABLE [dbo].[sepet_blokajlari] ADD [ip_adresi] nvarchar(45) NULL;
END
GO
