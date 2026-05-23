-- Tablo: dbo.OTEL_KOSUL_SOZLUGU
IF OBJECT_ID(N'dbo.OTEL_KOSUL_SOZLUGU', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[OTEL_KOSUL_SOZLUGU] (
        [ID] bigint IDENTITY(1,1) NOT NULL,
        [OTEL_TIPI_ID] int NULL,
        [KATEGORI] nvarchar(80) NOT NULL,
        [KOSUL_ADI] nvarchar(200) NOT NULL,
        [ACIKLAMA] nvarchar(500) NULL,
        [SIRALAMA] smallint NOT NULL CONSTRAINT [DF_otel_kosul_sozlugu_siralama] DEFAULT ((100)),
        [AKTIF_MI] bit NOT NULL CONSTRAINT [DF_otel_kosul_sozlugu_aktif] DEFAULT ((1)),
        [OLUSTURULMA_TARIHI] datetime2(0) NOT NULL CONSTRAINT [DF_otel_kosul_sozlugu_olusturma] DEFAULT (sysutcdatetime()),
        [GUNCELLENME_TARIHI] datetime2(0) NULL,
        CONSTRAINT [PK_OTEL_KOSUL_SOZLUGU] PRIMARY KEY CLUSTERED ([ID] ASC)
    );
END
