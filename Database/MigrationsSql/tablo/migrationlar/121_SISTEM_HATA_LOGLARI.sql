-- Tablo: dbo.SISTEM_HATA_LOGLARI
IF OBJECT_ID(N'dbo.SISTEM_HATA_LOGLARI', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[SISTEM_HATA_LOGLARI]
    (
        [ID] bigint IDENTITY(1,1) NOT NULL,
        [HATA_SEVIYESI] nvarchar(255) NOT NULL,
        [HATA_KODU] nvarchar(20) NULL,
        [HATA_MESAJI] nvarchar(max) NOT NULL,
        [HATA_DETAYI] nvarchar(max) NULL,
        [DOSYA_YOLU] nvarchar(500) NULL,
        [SATIR_NO] int NULL,
        [FONKSIYON_ADI] nvarchar(100) NULL,
        [SINIF_ADI] nvarchar(100) NULL,
        [URL] nvarchar(2000) NULL,
        [HTTP_METHOD] nvarchar(10) NULL,
        [IP_ADRESI] nvarchar(45) NULL,
        [KULLANICI_ARACISI] nvarchar(max) NULL,
        [REFERER] nvarchar(2000) NULL,
        [KULLANICI_ID] bigint NULL,
        [SESSION_ID] nvarchar(100) NULL,
        [REQUEST_ID] nvarchar(36) NULL,
        [REQUEST_VERISI] nvarchar(max) NULL,
        [YANIT_VERISI] nvarchar(max) NULL,
        [EK_BILGILER] nvarchar(max) NULL,
        [OLUSMA_TARIHI] datetime2(0) CONSTRAINT [DF__sistem_ha__olusm__25A691D2] DEFAULT (sysutcdatetime()) NULL,
        [COZULDU_MU] bit CONSTRAINT [DF__sistem_ha__cozul__269AB60B] DEFAULT ((0)) NULL,
        [COZULME_TARIHI] datetime2(0) NULL,
        [COZEN_ADMIN_ID] bigint NULL,
        [COZUM_NOTU] nvarchar(max) NULL,
        CONSTRAINT [PK_SISTEM_HATA_LOGLARI] PRIMARY KEY CLUSTERED ([ID] ASC)
    );
END
