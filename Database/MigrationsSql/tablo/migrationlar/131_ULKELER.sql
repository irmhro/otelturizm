-- Tablo: dbo.ULKELER
IF OBJECT_ID(N'dbo.ULKELER', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ULKELER]
    (
        [ID] bigint IDENTITY(1,1) NOT NULL,
        [ULKE_ADI] nvarchar(150) NOT NULL,
        [ISO2_KODU] nchar(2) NULL,
        [ISO3_KODU] nchar(3) NULL,
        [BAYRAK_IKON_KODU] nvarchar(16) NULL,
        [TELEFON_KODU] nvarchar(10) NULL,
        [PARA_BIRIMI_KODU] nvarchar(10) NULL,
        [VARSAYILAN_ULKE] bit CONSTRAINT [DF__ulkeler__varsayi__693CA210] DEFAULT ((0)) NOT NULL,
        [AKTIF_MI] bit CONSTRAINT [DF__ulkeler__aktif_m__6A30C649] DEFAULT ((1)) NOT NULL,
        [OLUSTURULMA_TARIHI] datetime2(0) CONSTRAINT [DF__ulkeler__olustur__6B24EA82] DEFAULT (sysutcdatetime()) NOT NULL,
        [GUNCELLENME_TARIHI] datetime2(0) NULL,
        CONSTRAINT [PK_ULKELER] PRIMARY KEY CLUSTERED ([ID] ASC)
    );
END
