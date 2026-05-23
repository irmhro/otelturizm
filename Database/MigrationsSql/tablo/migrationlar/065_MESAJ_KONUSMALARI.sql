-- Tablo: dbo.MESAJ_KONUSMALARI
IF OBJECT_ID(N'dbo.MESAJ_KONUSMALARI', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[MESAJ_KONUSMALARI] (
        [ID] bigint IDENTITY(1,1) NOT NULL,
        [KONUSMA_KODU] nvarchar(20) NOT NULL,
        [REZERVASYON_ID] bigint NULL,
        [OTEL_ID] bigint NULL,
        [FIRMA_ID] bigint NULL,
        [FIRMA_KULLANICI_ID] bigint NULL,
        [MISAFIR_KULLANICI_ID] bigint NOT NULL,
        [OTEL_YETKILISI_KULLANICI_ID] bigint NULL,
        [KONU_BASLIGI] nvarchar(200) NOT NULL,
        [KONUSMA_TURU] nvarchar(255) NOT NULL,
        [KONU_KATEGORISI] nvarchar(255) NULL,
        [DURUM] nvarchar(255) NULL,
        [ONCELIK] nvarchar(255) NULL,
        [SON_MESAJ_TARIHI] datetime2(0) NULL,
        [SON_MESAJ_GONDEREN] nvarchar(255) NULL,
        [SON_MESAJ_ONIZLEME] nvarchar(100) NULL,
        [MISAFIR_OKUNMAMIS_SAYISI] int NULL CONSTRAINT [DF__mesaj_kon__misaf__3BFFE745] DEFAULT ((0)),
        [OTEL_OKUNMAMIS_SAYISI] int NULL CONSTRAINT [DF__mesaj_kon__otel___3CF40B7E] DEFAULT ((0)),
        [FIRMA_OKUNMAMIS_SAYISI] int NOT NULL CONSTRAINT [DF__mesaj_kon__firma__3DE82FB7] DEFAULT ((0)),
        [MISAFIR_SON_OKUMA_TARIHI] datetime2(0) NULL,
        [OTEL_SON_OKUMA_TARIHI] datetime2(0) NULL,
        [FIRMA_SON_OKUMA_TARIHI] datetime2(0) NULL,
        [ETIKETLER] nvarchar(max) NULL,
        [ATANAN_DESTEK_EKIBI_KULLANICI_ID] bigint NULL,
        [OLUSTURULMA_TARIHI] datetime2(0) NULL CONSTRAINT [DF__mesaj_kon__olust__3EDC53F0] DEFAULT (sysutcdatetime()),
        [GUNCELLENME_TARIHI] datetime2(0) NULL,
        [KAPATILMA_TARIHI] datetime2(0) NULL,
        [KAPATMA_NEDENI] nvarchar(255) NULL,
        CONSTRAINT [PK_MESAJ_KONUSMALARI] PRIMARY KEY CLUSTERED ([ID] ASC)
    );
END
