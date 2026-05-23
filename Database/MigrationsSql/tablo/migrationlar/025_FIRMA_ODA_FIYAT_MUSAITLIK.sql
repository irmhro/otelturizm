-- Tablo: dbo.FIRMA_ODA_FIYAT_MUSAITLIK
IF OBJECT_ID(N'dbo.FIRMA_ODA_FIYAT_MUSAITLIK', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[FIRMA_ODA_FIYAT_MUSAITLIK] (
        [ID] bigint IDENTITY(1,1) NOT NULL,
        [OTEL_ID] bigint NOT NULL,
        [ODA_TIP_ID] bigint NOT NULL,
        [TARIH] date NOT NULL,
        [FIRMA_GECELIK_FIYAT] decimal(10,2) NOT NULL,
        [MINIMUM_GECELEME] tinyint NULL,
        [MAKSIMUM_GECELEME] smallint NULL,
        [KAPALI_SATIS] bit NOT NULL CONSTRAINT [DF_firma_ofm_kapali] DEFAULT ((0)),
        [FIYAT_NOTU] nvarchar(255) NULL,
        [GUNCELLEYEN_KULLANICI_ID] bigint NULL,
        [GUNCELLENME_TARIHI] datetime2(7) NOT NULL CONSTRAINT [DF_firma_ofm_guncellenme] DEFAULT (sysutcdatetime()),
        [AKTIF_MI] bit NOT NULL CONSTRAINT [DF_firma_ofm_aktif] DEFAULT ((1)),
        [FIRMA_ID] bigint NOT NULL CONSTRAINT [DF_firma_ofm_firma_id] DEFAULT ((0)),
        CONSTRAINT [PK_FIRMA_ODA_FIYAT_MUSAITLIK] PRIMARY KEY CLUSTERED ([ID] ASC)
    );
END
