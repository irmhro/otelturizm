-- Tablo: dbo.SATIS_MUSTERILERI
IF OBJECT_ID(N'dbo.SATIS_MUSTERILERI', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[SATIS_MUSTERILERI]
    (
        [ID] bigint IDENTITY(1,1) NOT NULL,
        [MUSTERI_KODU] nvarchar(24) NOT NULL,
        [AD_SOYAD] nvarchar(120) NOT NULL,
        [EPOSTA] nvarchar(100) NULL,
        [TELEFON] nvarchar(20) NULL,
        [ULKE] nvarchar(60) NULL,
        [SEHIR] nvarchar(100) NULL,
        [ILCE] nvarchar(100) NULL,
        [MAHALLE] nvarchar(120) NULL,
        [ADRES] nvarchar(max) NULL,
        [UYELIK_SEVIYESI] nvarchar(255) NOT NULL,
        [TOPLAM_REZERVASYON_SAYISI] int CONSTRAINT [DF__satis_mus__topla__1293BD5E] DEFAULT ((0)) NOT NULL,
        [TOPLAM_HARCAMA] decimal(12,2) CONSTRAINT [DF__satis_mus__topla__1387E197] DEFAULT ((0.00)) NOT NULL,
        [SON_REZERVASYON_TARIHI] date NULL,
        [SON_TALEP_OZETI] nvarchar(255) NULL,
        [PAZARLAMA_IZNI] bit CONSTRAINT [DF__satis_mus__pazar__147C05D0] DEFAULT ((0)) NOT NULL,
        [NOTLAR] nvarchar(max) NULL,
        [OLUSTURAN_SATIS_KULLANICI_ID] bigint NULL,
        [OLUSTURULMA_TARIHI] datetime2(0) CONSTRAINT [DF__satis_mus__olust__15702A09] DEFAULT (sysutcdatetime()) NULL,
        [GUNCELLENME_TARIHI] datetime2(0) NULL,
        CONSTRAINT [PK_SATIS_MUSTERILERI] PRIMARY KEY CLUSTERED ([ID] ASC)
    );
END
