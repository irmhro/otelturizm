-- Tablo: dbo.REZERVASYON_ODEME_KALEMLERI
IF OBJECT_ID(N'dbo.REZERVASYON_ODEME_KALEMLERI', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[REZERVASYON_ODEME_KALEMLERI]
    (
        [ID] bigint IDENTITY(1,1) NOT NULL,
        [REZERVASYON_ID] bigint NOT NULL,
        [ODEME_YONTEMI_ID] bigint NOT NULL,
        [ODEME_DURUMU_ID] bigint NOT NULL,
        [TUTAR] decimal(18,2) CONSTRAINT [DF_rezervasyon_odeme_kalem_tutar] DEFAULT ((0)) NOT NULL,
        [TAHSIL_EDILEN_TUTAR] decimal(18,2) NULL,
        [SIRA_NO] int CONSTRAINT [DF_rezervasyon_odeme_kalem_sira] DEFAULT ((1)) NOT NULL,
        [HAVALE_EFT_REFERANS] nvarchar(120) NULL,
        [DEKONT_GUVENLI_DOSYA_ID] bigint NULL,
        [ACIKLAMA] nvarchar(500) NULL,
        [OLUSTURULMA_TARIHI] datetime2(7) CONSTRAINT [DF_rezervasyon_odeme_kalem_olustur] DEFAULT (sysutcdatetime()) NOT NULL,
        CONSTRAINT [PK_REZERVASYON_ODEME_KALEMLERI] PRIMARY KEY CLUSTERED ([ID] ASC)
    );
END
