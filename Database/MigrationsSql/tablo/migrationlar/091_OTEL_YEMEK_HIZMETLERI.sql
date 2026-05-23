-- Tablo: dbo.OTEL_YEMEK_HIZMETLERI
IF OBJECT_ID(N'dbo.OTEL_YEMEK_HIZMETLERI', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[OTEL_YEMEK_HIZMETLERI] (
        [ID] bigint IDENTITY(1,1) NOT NULL,
        [OTEL_ID] bigint NOT NULL,
        [HIZMET_TIPI] nvarchar(40) NOT NULL,
        [AKTIF_MI] bit NOT NULL CONSTRAINT [DF_otel_yemek_hizmetleri_aktif] DEFAULT ((0)),
        [ODA_FIYATINA_DAHIL_MI] bit NOT NULL CONSTRAINT [DF_otel_yemek_hizmetleri_dahil] DEFAULT ((0)),
        [KISI_BASI_FIYAT] decimal(10,2) NULL,
        [IKI_KISI_FIYAT] decimal(10,2) NULL,
        [ACIKLAMA] nvarchar(500) NULL,
        [BASLANGIC_SAATI] time(0) NULL,
        [BITIS_SAATI] time(0) NULL,
        [OLUSTURULMA_TARIHI] datetime2(0) NOT NULL CONSTRAINT [DF_otel_yemek_hizmetleri_olusturma] DEFAULT (sysutcdatetime()),
        [GUNCELLENME_TARIHI] datetime2(0) NULL,
        CONSTRAINT [PK_OTEL_YEMEK_HIZMETLERI] PRIMARY KEY CLUSTERED ([ID] ASC)
    );
END
