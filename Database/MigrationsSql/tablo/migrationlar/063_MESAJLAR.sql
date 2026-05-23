-- Tablo: dbo.MESAJLAR
IF OBJECT_ID(N'dbo.MESAJLAR', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[MESAJLAR] (
        [ID] bigint IDENTITY(1,1) NOT NULL,
        [KONUSMA_ID] bigint NOT NULL,
        [GONDEREN_TURU] nvarchar(255) NOT NULL,
        [GONDEREN_KULLANICI_ID] bigint NULL,
        [GONDEREN_OTEL_ID] bigint NULL,
        [GONDEREN_FIRMA_ID] bigint NULL,
        [GONDEREN_FIRMA_KULLANICI_ID] bigint NULL,
        [MESAJ_METNI] nvarchar(max) NOT NULL,
        [MESAJ_TIPI] nvarchar(255) NULL,
        [MEDYA_URLS] nvarchar(max) NULL,
        [MEDYA_TIPLERI] nvarchar(max) NULL,
        [OZEL_TEKLIF_VAR_MI] bit NULL CONSTRAINT [DF__mesajlar__ozel_t__467D75B8] DEFAULT ((0)),
        [TEKLIF_TUTARI] decimal(10,2) NULL,
        [TEKLIF_PARA_BIRIMI] nvarchar(3) NULL,
        [TEKLIF_GECERLILIK_SURESI] datetime2(0) NULL,
        [TEKLIF_DURUMU] nvarchar(255) NULL,
        [TEKLIF_KABUL_TARIHI] datetime2(0) NULL,
        [OKUNDU_MU] bit NULL CONSTRAINT [DF__mesajlar__okundu__477199F1] DEFAULT ((0)),
        [OKUNMA_TARIHI] datetime2(0) NULL,
        [DURUM] nvarchar(255) NULL,
        [IP_ADRESI] nvarchar(45) NULL,
        [CIHAZ_BILGISI] nvarchar(255) NULL,
        [GONDERIM_TARIHI] datetime2(0) NULL CONSTRAINT [DF__mesajlar__gonder__4865BE2A] DEFAULT (sysutcdatetime()),
        [DUZENLENME_TARIHI] datetime2(0) NULL,
        [DUZENLENDI_MI] bit NOT NULL CONSTRAINT [DF__mesajlar__duzenl__4959E263] DEFAULT ((0)),
        [DUZENLEYEN_KULLANICI_ID] bigint NULL,
        [SILINME_TARIHI] datetime2(0) NULL,
        [MISAFIR_GIZLENDI_MI] bit NOT NULL CONSTRAINT [DF__mesajlar__misafi__4A4E069C] DEFAULT ((0)),
        [FIRMA_GIZLENDI_MI] bit NOT NULL CONSTRAINT [DF__mesajlar__firma___4B422AD5] DEFAULT ((0)),
        [OTEL_GIZLENDI_MI] bit NOT NULL CONSTRAINT [DF__mesajlar__otel_g__4C364F0E] DEFAULT ((0)),
        [SILINME_NEDENI] nvarchar(255) NULL,
        [SILINME_GORUNUM_METNI] nvarchar(255) NULL,
        CONSTRAINT [PK_MESAJLAR] PRIMARY KEY CLUSTERED ([ID] ASC)
    );
END
