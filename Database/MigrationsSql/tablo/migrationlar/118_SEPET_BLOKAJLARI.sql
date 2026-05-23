-- Tablo: dbo.SEPET_BLOKAJLARI
IF OBJECT_ID(N'dbo.SEPET_BLOKAJLARI', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[SEPET_BLOKAJLARI]
    (
        [ID] bigint IDENTITY(1,1) NOT NULL,
        [BLOKAJ_KODU] nvarchar(30) NOT NULL,
        [OTEL_ID] bigint NOT NULL,
        [ODA_TIP_ID] bigint NOT NULL,
        [KULLANICI_ID] bigint NULL,
        [SESSION_ID] nvarchar(100) NOT NULL,
        [GIRIS_TARIHI] date NOT NULL,
        [CIKIS_TARIHI] date NOT NULL,
        [ODA_SAYISI] tinyint CONSTRAINT [DF__sepet_blo__oda_s__1B29035F] DEFAULT ((1)) NULL,
        [YETISKIN_SAYISI] tinyint NOT NULL,
        [COCUK_SAYISI] tinyint CONSTRAINT [DF__sepet_blo__cocuk__1C1D2798] DEFAULT ((0)) NULL,
        [GECELIK_FIYAT] decimal(10,2) NOT NULL,
        [TOPLAM_TUTAR] decimal(10,2) NOT NULL,
        [PARA_BIRIMI] nvarchar(3) NULL,
        [DURUM] nvarchar(255) NULL,
        [REZERVASYON_ID] bigint NULL,
        [BLOKAJ_BASLANGIC_TARIHI] datetime2(0) CONSTRAINT [DF__sepet_blo__bloka__1D114BD1] DEFAULT (sysutcdatetime()) NULL,
        [BLOKAJ_BITIS_TARIHI] datetime2(0) NULL,
        [SURE_DAKIKA] smallint CONSTRAINT [DF__sepet_blo__sure___1E05700A] DEFAULT ((15)) NULL,
        [HATIRLATMA_GONDERILDI_MI] bit CONSTRAINT [DF__sepet_blo__hatir__1EF99443] DEFAULT ((0)) NULL,
        [HATIRLATMA_GONDERILME_TARIHI] datetime2(0) NULL,
        [IP_ADRESI] nvarchar(45) NULL,
        CONSTRAINT [PK_SEPET_BLOKAJLARI] PRIMARY KEY CLUSTERED ([ID] ASC)
    );
END
