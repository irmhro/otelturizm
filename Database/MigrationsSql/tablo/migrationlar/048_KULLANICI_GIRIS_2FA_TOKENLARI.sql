-- Tablo: dbo.KULLANICI_GIRIS_2FA_TOKENLARI
IF OBJECT_ID(N'dbo.KULLANICI_GIRIS_2FA_TOKENLARI', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[KULLANICI_GIRIS_2FA_TOKENLARI] (
        [ID] bigint IDENTITY(1,1) NOT NULL,
        [KULLANICI_ID] bigint NOT NULL,
        [TELEFON_E164] nvarchar(32) NOT NULL,
        [DOGRULAMA_KODU_HASH] nvarchar(128) NOT NULL,
        [DENEME_SAYISI] smallint NOT NULL CONSTRAINT [DF_kullanici_giris_2fa_deneme] DEFAULT ((0)),
        [MAKSIMUM_DENEME] smallint NOT NULL CONSTRAINT [DF_kullanici_giris_2fa_max] DEFAULT ((5)),
        [KULLANILDI_MI] bit NOT NULL CONSTRAINT [DF_kullanici_giris_2fa_kullanildi] DEFAULT ((0)),
        [KULLANILMA_TARIHI] datetime2(7) NULL,
        [GECERLILIK_SURESI] datetime2(7) NOT NULL,
        [IP_ADRESI] nvarchar(80) NULL,
        [KULLANICI_ARACISI] nvarchar(500) NULL,
        [OLUSTURULMA_TARIHI] datetime2(7) NOT NULL CONSTRAINT [DF_kullanici_giris_2fa_created] DEFAULT (sysutcdatetime()),
        [GUNCELLENME_TARIHI] datetime2(7) NOT NULL CONSTRAINT [DF_kullanici_giris_2fa_updated] DEFAULT (sysutcdatetime()),
        [KANAL] nvarchar(20) NOT NULL CONSTRAINT [DF_kullanici_giris_2fa_kanal] DEFAULT (N'whatsapp'),
        [EPOSTA] nvarchar(255) NULL,
        CONSTRAINT [PK_KULLANICI_GIRIS_2FA_TOKENLARI] PRIMARY KEY CLUSTERED ([ID] ASC)
    );
END
