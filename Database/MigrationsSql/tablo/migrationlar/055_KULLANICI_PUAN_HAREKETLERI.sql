-- Tablo: dbo.KULLANICI_PUAN_HAREKETLERI
IF OBJECT_ID(N'dbo.KULLANICI_PUAN_HAREKETLERI', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[KULLANICI_PUAN_HAREKETLERI] (
        [ID] bigint IDENTITY(1,1) NOT NULL,
        [KULLANICI_ID] bigint NOT NULL,
        [SADAKAT_HESAP_ID] bigint NULL,
        [REZERVASYON_ID] bigint NULL,
        [HAREKET_TIPI] nvarchar(60) NOT NULL,
        [BASLIK] nvarchar(180) NOT NULL,
        [ACIKLAMA] nvarchar(500) NULL,
        [PUAN_DEGISIM] int NOT NULL,
        [PUAN_BAKIYE_SONRASI] int NULL,
        [DURUM] nvarchar(30) NOT NULL,
        [ISLEM_TARIHI] datetime2(0) NOT NULL CONSTRAINT [DF__kullanici__islem__18B6AB08] DEFAULT (sysutcdatetime()),
        [GECERLILIK_TARIHI] datetime2(0) NULL,
        [OLUSTURULMA_TARIHI] datetime2(0) NOT NULL CONSTRAINT [DF__kullanici__olust__19AACF41] DEFAULT (sysutcdatetime()),
        CONSTRAINT [PK_KULLANICI_PUAN_HAREKETLERI] PRIMARY KEY CLUSTERED ([ID] ASC)
    );
END
