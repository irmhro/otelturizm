SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.firma_calisanlari', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[firma_calisanlari]
    (
        [id] bigint IDENTITY(1,1) NOT NULL,
        [firma_id] bigint NULL,
        [ad_soyad] nvarchar(160) NOT NULL,
        [eposta] nvarchar(180) NULL,
        [telefon] nvarchar(40) NULL,
        [departman] nvarchar(120) NULL,
        [gorev] nvarchar(120) NULL,
        [aktif_mi] bit CONSTRAINT [DF_firma_calisanlari_aktif_mi] DEFAULT ((1)) NOT NULL,
        [olusturulma_tarihi] datetime2(0) CONSTRAINT [DF_firma_calisanlari_olusturulma_tarihi] DEFAULT (sysutcdatetime()) NOT NULL,
        [guncellenme_tarihi] datetime2(0) NULL,
        CONSTRAINT [PK_firma_calisanlari] PRIMARY KEY CLUSTERED ([id] ASC)
    );
END
GO

IF OBJECT_ID(N'dbo.firma_rezervasyonlari', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[firma_rezervasyonlari]
    (
        [id] bigint IDENTITY(1,1) NOT NULL,
        [firma_id] bigint NULL,
        [rezervasyon_id] bigint NULL,
        [otel_id] bigint NULL,
        [oda_tipi_id] bigint NULL,
        [oda_adedi] int CONSTRAINT [DF_firma_rezervasyonlari_oda_adedi] DEFAULT ((1)) NOT NULL,
        [giris_tarihi] date NULL,
        [cikis_tarihi] date NULL,
        [durum] nvarchar(40) CONSTRAINT [DF_firma_rezervasyonlari_durum] DEFAULT (N'Bekliyor') NOT NULL,
        [toplam_tutar] decimal(18,2) CONSTRAINT [DF_firma_rezervasyonlari_toplam_tutar] DEFAULT ((0)) NOT NULL,
        [personel_atama_zorunlu_mu] bit CONSTRAINT [DF_firma_rezervasyonlari_personel_zorunlu] DEFAULT ((0)) NOT NULL,
        [olusturulma_tarihi] datetime2(0) CONSTRAINT [DF_firma_rezervasyonlari_olusturulma_tarihi] DEFAULT (sysutcdatetime()) NOT NULL,
        [guncellenme_tarihi] datetime2(0) NULL,
        CONSTRAINT [PK_firma_rezervasyonlari] PRIMARY KEY CLUSTERED ([id] ASC)
    );
END
GO

IF COL_LENGTH(N'dbo.firma_calisanlari', N'aktif_mi') IS NULL
BEGIN
    ALTER TABLE [dbo].[firma_calisanlari] ADD [aktif_mi] bit NULL CONSTRAINT [DF_firma_calisanlari_aktif_mi_repair] DEFAULT ((1));
END
GO

IF COL_LENGTH(N'dbo.firma_rezervasyonlari', N'personel_atama_zorunlu_mu') IS NULL
BEGIN
    ALTER TABLE [dbo].[firma_rezervasyonlari] ADD [personel_atama_zorunlu_mu] bit NULL CONSTRAINT [DF_firma_rezervasyonlari_personel_zorunlu_repair] DEFAULT ((0));
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_firma_calisanlari_firma_aktif' AND object_id = OBJECT_ID(N'dbo.firma_calisanlari'))
BEGIN
    CREATE INDEX [IX_firma_calisanlari_firma_aktif] ON [dbo].[firma_calisanlari] ([firma_id], [aktif_mi]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_firma_rezervasyonlari_firma_durum' AND object_id = OBJECT_ID(N'dbo.firma_rezervasyonlari'))
BEGIN
    CREATE INDEX [IX_firma_rezervasyonlari_firma_durum] ON [dbo].[firma_rezervasyonlari] ([firma_id], [durum], [olusturulma_tarihi] DESC);
END
GO
