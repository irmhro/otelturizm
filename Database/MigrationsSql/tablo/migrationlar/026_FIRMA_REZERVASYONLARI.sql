-- Tablo: dbo.FIRMA_REZERVASYONLARI
IF OBJECT_ID(N'dbo.FIRMA_REZERVASYONLARI', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[FIRMA_REZERVASYONLARI] (
        [ID] bigint IDENTITY(1,1) NOT NULL,
        [FIRMA_ID] bigint NULL,
        [REZERVASYON_ID] bigint NULL,
        [OTEL_ID] bigint NULL,
        [ODA_TIPI_ID] bigint NULL,
        [ODA_ADEDI] int NOT NULL CONSTRAINT [DF_firma_rezervasyonlari_oda_adedi] DEFAULT ((1)),
        [GIRIS_TARIHI] date NULL,
        [CIKIS_TARIHI] date NULL,
        [DURUM] nvarchar(40) NOT NULL CONSTRAINT [DF_firma_rezervasyonlari_durum] DEFAULT (N'Bekliyor'),
        [TOPLAM_TUTAR] decimal(18,2) NOT NULL CONSTRAINT [DF_firma_rezervasyonlari_toplam_tutar] DEFAULT ((0)),
        [PERSONEL_ATAMA_ZORUNLU_MU] bit NOT NULL CONSTRAINT [DF_firma_rezervasyonlari_personel_zorunlu] DEFAULT ((0)),
        [OLUSTURULMA_TARIHI] datetime2(0) NOT NULL CONSTRAINT [DF_firma_rezervasyonlari_olusturulma_tarihi] DEFAULT (sysutcdatetime()),
        [GUNCELLENME_TARIHI] datetime2(0) NULL,
        CONSTRAINT [PK_FIRMA_REZERVASYONLARI] PRIMARY KEY CLUSTERED ([ID] ASC)
    );
END
