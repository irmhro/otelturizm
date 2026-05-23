-- Tablo: dbo.OTEL_GORSELLERI
IF OBJECT_ID(N'dbo.OTEL_GORSELLERI', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[OTEL_GORSELLERI] (
        [ID] bigint IDENTITY(1,1) NOT NULL,
        [OTEL_ID] bigint NOT NULL,
        [GORSEL_URL] nvarchar(500) NOT NULL,
        [THUMBNAIL_URL] nvarchar(500) NULL,
        [GORSEL_TURU] nvarchar(255) NOT NULL,
        [BASLIK] nvarchar(200) NULL,
        [ACIKLAMA] nvarchar(max) NULL,
        [KAPAK_FOTOGRAFI_MI] bit NULL CONSTRAINT [DF__otel_gors__kapak__7CD98669] DEFAULT ((0)),
        [ONE_CIKAN] bit NULL CONSTRAINT [DF__otel_gors__one_c__7DCDAAA2] DEFAULT ((0)),
        [SIRALAMA] smallint NULL CONSTRAINT [DF__otel_gors__siral__7EC1CEDB] DEFAULT ((0)),
        [BOYUT_KB] int NULL,
        [GENISLIK] smallint NULL,
        [YUKSEKLIK] smallint NULL,
        [ONAY_DURUMU] nvarchar(255) NULL,
        [ONAYLAYAN_ADMIN_ID] bigint NULL,
        [ONAY_TARIHI] datetime2(0) NULL,
        [YUKLEYEN_KULLANICI_ID] bigint NULL,
        [OLUSTURULMA_TARIHI] datetime2(0) NULL CONSTRAINT [DF__otel_gors__olust__7FB5F314] DEFAULT (sysutcdatetime()),
        CONSTRAINT [PK_OTEL_GORSELLERI] PRIMARY KEY CLUSTERED ([ID] ASC)
    );
END
