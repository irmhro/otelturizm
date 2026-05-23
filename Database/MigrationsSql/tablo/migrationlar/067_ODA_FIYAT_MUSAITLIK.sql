-- Tablo: dbo.ODA_FIYAT_MUSAITLIK
IF OBJECT_ID(N'dbo.ODA_FIYAT_MUSAITLIK', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ODA_FIYAT_MUSAITLIK] (
        [ID] bigint IDENTITY(1,1) NOT NULL,
        [ODA_TIP_ID] bigint NOT NULL,
        [OTEL_ID] bigint NOT NULL,
        [TARIH] date NOT NULL,
        [GECELIK_FIYAT] decimal(10,2) NOT NULL,
        [INDIRIMLI_FIYAT] decimal(10,2) NULL,
        [KAMPANYA_ID] bigint NULL,
        [TOPLAM_ODA_SAYISI] smallint NOT NULL,
        [SATILAN_ODA_SAYISI] smallint NULL CONSTRAINT [DF__oda_fiyat__satil__4F12BBB9] DEFAULT ((0)),
        [BLOKE_ODA_SAYISI] smallint NULL CONSTRAINT [DF__oda_fiyat__bloke__5006DFF2] DEFAULT ((0)),
        [MINIMUM_GECELEME] tinyint NULL CONSTRAINT [DF__oda_fiyat__minim__50FB042B] DEFAULT ((1)),
        [MAKSIMUM_GECELEME] smallint NULL CONSTRAINT [DF__oda_fiyat__maksi__51EF2864] DEFAULT ((30)),
        [KAPALI_SATIS] bit NULL CONSTRAINT [DF__oda_fiyat__kapal__52E34C9D] DEFAULT ((0)),
        [KAMPANYA_ETIKETI] nvarchar(120) NULL,
        [FIYAT_NOTU] nvarchar(255) NULL,
        [GUNCELLEYEN_KULLANICI_ID] bigint NULL,
        [SADECE_GUNUBIRLIK] bit NULL CONSTRAINT [DF__oda_fiyat__sadec__53D770D6] DEFAULT ((0)),
        [IPTAL_POLITIKASI_OVERRIDE] nvarchar(max) NULL,
        [GUNCELLENME_TARIHI] datetime2(0) NULL CONSTRAINT [DF__oda_fiyat__gunce__54CB950F] DEFAULT (sysutcdatetime()),
        [INDIRIM_ID] bigint NULL,
        [KAMPANYA_FIYATI] decimal(18,2) NULL,
        CONSTRAINT [PK_ODA_FIYAT_MUSAITLIK] PRIMARY KEY CLUSTERED ([ID] ASC)
    );
END
