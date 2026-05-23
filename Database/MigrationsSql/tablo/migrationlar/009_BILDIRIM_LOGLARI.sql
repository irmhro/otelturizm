-- Tablo: dbo.BILDIRIM_LOGLARI
IF OBJECT_ID(N'dbo.BILDIRIM_LOGLARI', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[BILDIRIM_LOGLARI] (
        [ID] bigint IDENTITY(1,1) NOT NULL,
        [KULLANICI_ID] bigint NOT NULL,
        [BILDIRIM_SABLON_ID] smallint NULL,
        [TUR] nvarchar(255) NOT NULL,
        [ALICI_EPOSTA] nvarchar(100) NULL,
        [ALICI_TELEFON] nvarchar(20) NULL,
        [CIHAZ_TOKEN] nvarchar(255) NULL,
        [KONU] nvarchar(200) NULL,
        [ICERIK] nvarchar(max) NOT NULL,
        [GONDERILEN_ICERIK] nvarchar(max) NULL,
        [DURUM] nvarchar(255) NULL,
        [SAGLAYICI] nvarchar(255) NULL,
        [SAGLAYICI_MESAJ_ID] nvarchar(100) NULL,
        [HATA_KODU] nvarchar(20) NULL,
        [HATA_MESAJI] nvarchar(500) NULL,
        [GONDERME_DENEMESI] tinyint NULL CONSTRAINT [DF__bildirim___gonde__7A672E12] DEFAULT ((1)),
        [MAKSIMUM_DENEME] tinyint NULL CONSTRAINT [DF__bildirim___maksi__7B5B524B] DEFAULT ((3)),
        [GONDERIM_TARIHI] datetime2(0) NULL,
        [OKUNMA_TARIHI] datetime2(0) NULL,
        [OLUSTURULMA_TARIHI] datetime2(0) NULL CONSTRAINT [DF__bildirim___olust__7C4F7684] DEFAULT (sysutcdatetime()),
        [ILGILI_TABLO] nvarchar(50) NULL,
        [ILGILI_KAYIT_ID] bigint NULL,
        [GUNCELLENME_TARIHI] datetime2(7) NULL,
        [EKLER_JSON] nvarchar(max) NULL,
        [EPOSTA_SERVIS_KODU] nvarchar(80) NULL,
        [GONDEREN_EPOSTA_OVERRIDE] nvarchar(320) NULL,
        [SONRAKI_DENEME_UTC] datetime2(7) NULL,
        [DENEME_SAYISI] int NOT NULL CONSTRAINT [DF_bildirim_loglari_deneme_sayisi] DEFAULT ((0)),
        [MAKSIMUM_DENEME_SAYISI] int NOT NULL CONSTRAINT [DF_bildirim_loglari_maksimum_deneme_sayisi] DEFAULT ((5)),
        [EPOSTA_SERVIS_ID] smallint NULL,
        CONSTRAINT [PK_BILDIRIM_LOGLARI] PRIMARY KEY CLUSTERED ([ID] ASC)
    );
END
