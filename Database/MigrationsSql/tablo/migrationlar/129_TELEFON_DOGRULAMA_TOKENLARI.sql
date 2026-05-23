-- Tablo: dbo.TELEFON_DOGRULAMA_TOKENLARI
IF OBJECT_ID(N'dbo.TELEFON_DOGRULAMA_TOKENLARI', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[TELEFON_DOGRULAMA_TOKENLARI]
    (
        [ID] bigint IDENTITY(1,1) NOT NULL,
        [KULLANICI_ID] bigint NOT NULL,
        [TELEFON_RAW] nvarchar(32) NULL,
        [TELEFON_E164] nvarchar(32) NOT NULL,
        [DOGRULAMA_KODU_HASH] nvarchar(128) NOT NULL,
        [DOGRULAMA_KANALI] nvarchar(30) CONSTRAINT [DF_telefon_dogrulama_tokenlari_kanal] DEFAULT ('whatsapp') NOT NULL,
        [META_MESAJ_ID] nvarchar(120) NULL,
        [TALEP_DURUMU] nvarchar(40) CONSTRAINT [DF_telefon_dogrulama_tokenlari_durum] DEFAULT ('Hazirlaniyor') NOT NULL,
        [DENEME_SAYISI] smallint CONSTRAINT [DF_telefon_dogrulama_tokenlari_deneme] DEFAULT ((0)) NOT NULL,
        [MAKSIMUM_DENEME] smallint CONSTRAINT [DF_telefon_dogrulama_tokenlari_max] DEFAULT ((5)) NOT NULL,
        [KULLANILDI_MI] bit CONSTRAINT [DF_telefon_dogrulama_tokenlari_kullanildi] DEFAULT ((0)) NOT NULL,
        [KULLANILMA_TARIHI] datetime2(7) NULL,
        [GECERLILIK_SURESI] datetime2(7) NOT NULL,
        [SON_HATA_MESAJI] nvarchar(500) NULL,
        [IP_ADRESI] nvarchar(80) NULL,
        [KULLANICI_ARACISI] nvarchar(500) NULL,
        [OLUSTURULMA_TARIHI] datetime2(7) CONSTRAINT [DF_telefon_dogrulama_tokenlari_created] DEFAULT (sysutcdatetime()) NOT NULL,
        [GUNCELLENME_TARIHI] datetime2(7) CONSTRAINT [DF_telefon_dogrulama_tokenlari_updated] DEFAULT (sysutcdatetime()) NOT NULL,
        CONSTRAINT [PK_TELEFON_DOGRULAMA_TOKENLARI] PRIMARY KEY CLUSTERED ([ID] ASC)
    );
END
