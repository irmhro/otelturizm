-- Tablo: dbo.BASARISIZ_ODEME_DENEMELERI
IF OBJECT_ID(N'dbo.BASARISIZ_ODEME_DENEMELERI', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[BASARISIZ_ODEME_DENEMELERI] (
        [ID] bigint IDENTITY(1,1) NOT NULL,
        [REZERVASYON_ID] bigint NOT NULL,
        [KULLANICI_ID] bigint NOT NULL,
        [DENEME_TARIHI] datetime2(0) NULL CONSTRAINT [DF__basarisiz__denem__76969D2E] DEFAULT (sysutcdatetime()),
        [TUTAR] decimal(10,2) NOT NULL,
        [PARA_BIRIMI] nvarchar(3) NULL,
        [ODEME_YONTEMI] nvarchar(255) NOT NULL,
        [KART_TIPI] nvarchar(255) NULL,
        [KART_NUMARASI_MASKED] nvarchar(20) NULL,
        [ODEME_SAGLAYICI] nvarchar(255) NULL,
        [HATA_KODU] nvarchar(20) NULL,
        [HATA_MESAJI] nvarchar(500) NOT NULL,
        [HATA_DETAYI] nvarchar(max) NULL,
        [UC_D_SECURE_DURUMU] nvarchar(255) NULL,
        [IP_ADRESI] nvarchar(45) NULL,
        [CIHAZ_BILGISI] nvarchar(255) NULL,
        [COZULDU_MU] bit NULL CONSTRAINT [DF__basarisiz__cozul__778AC167] DEFAULT ((0)),
        [COZULME_TARIHI] datetime2(0) NULL,
        CONSTRAINT [PK_BASARISIZ_ODEME_DENEMELERI] PRIMARY KEY CLUSTERED ([ID] ASC)
    );
END
