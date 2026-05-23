-- Tablo: dbo.PARTNER_TESIS_KULLANICILARI
IF OBJECT_ID(N'dbo.PARTNER_TESIS_KULLANICILARI', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[PARTNER_TESIS_KULLANICILARI] (
        [ID] bigint IDENTITY(1,1) NOT NULL,
        [OTEL_ID] bigint NOT NULL,
        [KULLANICI_ID] bigint NOT NULL,
        [DURUM] nvarchar(50) NULL,
        [BASLANGIC_TARIHI] datetime2(0) NULL,
        [BITIS_TARIHI] datetime2(0) NULL,
        [DAVET_TOKEN] nvarchar(200) NULL,
        [DAVET_GONDERIM_TARIHI] datetime2(0) NULL,
        [DAVET_SON_GECERLILIK] datetime2(0) NULL,
        [ONAY_TARIHI] datetime2(0) NULL,
        [EKLEYEN_KULLANICI_ID] bigint NULL,
        [IPTAL_EDEN_KULLANICI_ID] bigint NULL,
        [IPTAL_TARIHI] datetime2(0) NULL,
        [IPTAL_NEDENI] nvarchar(500) NULL,
        [AKTIF_MI] bit NOT NULL CONSTRAINT [DF_partner_tesis_kullanicilari_aktif_mi] DEFAULT ((1)),
        [OLUSTURULMA_TARIHI] datetime2(0) NOT NULL CONSTRAINT [DF_partner_tesis_kullanicilari_olusturulma] DEFAULT (sysutcdatetime()),
        [GUNCELLENME_TARIHI] datetime2(0) NULL,
        CONSTRAINT [PK_PARTNER_TESIS_KULLANICILARI] PRIMARY KEY CLUSTERED ([ID] ASC)
    );
END
