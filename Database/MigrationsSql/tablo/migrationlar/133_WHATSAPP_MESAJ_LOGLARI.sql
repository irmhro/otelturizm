-- Tablo: dbo.WHATSAPP_MESAJ_LOGLARI
IF OBJECT_ID(N'dbo.WHATSAPP_MESAJ_LOGLARI', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[WHATSAPP_MESAJ_LOGLARI]
    (
        [ID] bigint IDENTITY(1,1) NOT NULL,
        [KULLANICI_ID] bigint NULL,
        [TELEFON_E164] nvarchar(32) NOT NULL,
        [SABLON_ADI] nvarchar(120) NOT NULL,
        [META_MESAJ_ID] nvarchar(120) NULL,
        [TESLIMAT_DURUMU] nvarchar(40) NULL,
        [REQUEST_PAYLOAD] nvarchar(max) NULL,
        [YANIT_PAYLOAD] nvarchar(max) NULL,
        [HATA_KODU] nvarchar(50) NULL,
        [HATA_MESAJI] nvarchar(500) NULL,
        [OLUSTURULMA_TARIHI] datetime2(7) CONSTRAINT [DF_whatsapp_mesaj_loglari_created] DEFAULT (sysutcdatetime()) NOT NULL,
        [GUNCELLENME_TARIHI] datetime2(7) CONSTRAINT [DF_whatsapp_mesaj_loglari_updated] DEFAULT (sysutcdatetime()) NOT NULL,
        CONSTRAINT [PK_WHATSAPP_MESAJ_LOGLARI] PRIMARY KEY CLUSTERED ([ID] ASC)
    );
END
