-- Tablo: dbo.OTEL_RAKIP_ANALIZI
IF OBJECT_ID(N'dbo.OTEL_RAKIP_ANALIZI', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[OTEL_RAKIP_ANALIZI] (
        [ID] bigint IDENTITY(1,1) NOT NULL,
        [OTEL_ID] bigint NOT NULL,
        [RAKIP_OTEL_ADI] nvarchar(200) NOT NULL,
        [RAKIP_SEHIR] nvarchar(100) NULL,
        [RAKIP_ILCE] nvarchar(100) NULL,
        [ANALIZ_TARIHI] date NOT NULL,
        [ORTALAMA_GECELIK_FIYAT] decimal(10,2) NULL,
        [TAHMINI_DOLULUK_ORANI] decimal(5,2) NULL,
        [KAYNAK_URL] nvarchar(500) NULL,
        [NOTLAR] nvarchar(max) NULL,
        CONSTRAINT [PK_OTEL_RAKIP_ANALIZI] PRIMARY KEY CLUSTERED ([ID] ASC)
    );
END
