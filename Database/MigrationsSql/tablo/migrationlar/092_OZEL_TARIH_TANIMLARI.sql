-- Tablo: dbo.OZEL_TARIH_TANIMLARI
IF OBJECT_ID(N'dbo.OZEL_TARIH_TANIMLARI', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[OZEL_TARIH_TANIMLARI] (
        [ID] int IDENTITY(1,1) NOT NULL,
        [TUR] nvarchar(255) NOT NULL,
        [AD] nvarchar(100) NOT NULL,
        [BASLANGIC_TARIHI] date NOT NULL,
        [BITIS_TARIHI] date NOT NULL,
        [TEKRAR_EDER_MI] bit NULL CONSTRAINT [DF__ozel_tari__tekra__3BCADD1B] DEFAULT ((0)),
        [TEKRAR_KURALI] nvarchar(255) NULL,
        [ULKE] nvarchar(50) NULL,
        [SEHIR] nvarchar(50) NULL,
        [FIYAT_CARPANI] decimal(4,2) NULL CONSTRAINT [DF__ozel_tari__fiyat__3CBF0154] DEFAULT ((1.00)),
        [MINIMUM_GECELEME_KURALI] tinyint NULL,
        [ACIKLAMA] nvarchar(255) NULL,
        [AKTIF_MI] bit NULL CONSTRAINT [DF__ozel_tari__aktif__3DB3258D] DEFAULT ((1)),
        CONSTRAINT [PK_OZEL_TARIH_TANIMLARI] PRIMARY KEY CLUSTERED ([ID] ASC)
    );
END
