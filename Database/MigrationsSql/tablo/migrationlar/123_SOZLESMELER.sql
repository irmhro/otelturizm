-- Tablo: dbo.SOZLESMELER
IF OBJECT_ID(N'dbo.SOZLESMELER', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[SOZLESMELER]
    (
        [ID] bigint IDENTITY(1,1) NOT NULL,
        [HEDEF_KITLE] nvarchar(30) NOT NULL,
        [SOZLESME_TIPI] nvarchar(30) NOT NULL,
        [BASLIK] nvarchar(200) NOT NULL,
        [ALT_BASLIK] nvarchar(300) NULL,
        [SLUG] nvarchar(200) NOT NULL,
        [OZET_HTML] nvarchar(max) NULL,
        [ICERIK_HTML] nvarchar(max) NOT NULL,
        [GORSEL_URL] nvarchar(500) NULL,
        [SOZLESME_LINKI] nvarchar(500) NULL,
        [VERSIYON_NO] int CONSTRAINT [DF_sozlesmeler_versiyon] DEFAULT ((1)) NOT NULL,
        [BASLANGIC_TARIHI] datetime2(7) CONSTRAINT [DF_sozlesmeler_baslangic] DEFAULT (sysutcdatetime()) NOT NULL,
        [BITIS_TARIHI] datetime2(7) NULL,
        [KABUL_GEREKTIRIR_MI] bit CONSTRAINT [DF_sozlesmeler_kabul] DEFAULT ((1)) NOT NULL,
        [EPOSTA_DOGRULAMADA_GONDER] bit CONSTRAINT [DF_sozlesmeler_email] DEFAULT ((1)) NOT NULL,
        [YENILEME_GEREKIR_MI] bit CONSTRAINT [DF_sozlesmeler_yenileme] DEFAULT ((0)) NOT NULL,
        [YENILEME_PERIYODU_GUN] int NULL,
        [AKTIF_MI] bit CONSTRAINT [DF_sozlesmeler_aktif] DEFAULT ((1)) NOT NULL,
        [NOTLAR] nvarchar(1000) NULL,
        [OLUSTURAN_KULLANICI_ID] bigint NULL,
        [GUNCELLEYEN_KULLANICI_ID] bigint NULL,
        [OLUSTURULMA_TARIHI] datetime2(7) CONSTRAINT [DF_sozlesmeler_olustur] DEFAULT (sysutcdatetime()) NOT NULL,
        [GUNCELLENME_TARIHI] datetime2(7) CONSTRAINT [DF_sozlesmeler_guncel] DEFAULT (sysutcdatetime()) NOT NULL,
        CONSTRAINT [PK_SOZLESMELER] PRIMARY KEY CLUSTERED ([ID] ASC)
    );
END
