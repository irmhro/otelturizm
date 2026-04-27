-- SQL Server (idempotent)
-- Amaç: arama sorgularını SARGable yapmak için normalized computed (PERSISTED) kolonlar eklemek.
-- Not: Bu kolonlar, HotelService.BuildSearchNormalizationSql ile aynı dönüştürmeyi uygular.

IF OBJECT_ID(N'dbo.oteller', N'U') IS NOT NULL
BEGIN
    -- şehir
    IF COL_LENGTH(N'dbo.oteller', N'sehir_normalized') IS NULL
    BEGIN
        ALTER TABLE dbo.oteller
        ADD sehir_normalized AS
            LOWER(TRANSLATE(
                COALESCE(sehir, N''),
                CONCAT(NCHAR(304),NCHAR(73),NCHAR(305),NCHAR(199),NCHAR(231),NCHAR(286),NCHAR(287),NCHAR(214),NCHAR(246),NCHAR(350),NCHAR(351),NCHAR(220),NCHAR(252)),
                N'iiiccggoossuu'
            )) PERSISTED;
    END

    -- ilçe
    IF COL_LENGTH(N'dbo.oteller', N'ilce_normalized') IS NULL
    BEGIN
        ALTER TABLE dbo.oteller
        ADD ilce_normalized AS
            LOWER(TRANSLATE(
                COALESCE(ilce, N''),
                CONCAT(NCHAR(304),NCHAR(73),NCHAR(305),NCHAR(199),NCHAR(231),NCHAR(286),NCHAR(287),NCHAR(214),NCHAR(246),NCHAR(350),NCHAR(351),NCHAR(220),NCHAR(252)),
                N'iiiccggoossuu'
            )) PERSISTED;
    END

    -- mahalle
    IF COL_LENGTH(N'dbo.oteller', N'mahalle_normalized') IS NULL
    BEGIN
        ALTER TABLE dbo.oteller
        ADD mahalle_normalized AS
            LOWER(TRANSLATE(
                COALESCE(mahalle, N''),
                CONCAT(NCHAR(304),NCHAR(73),NCHAR(305),NCHAR(199),NCHAR(231),NCHAR(286),NCHAR(287),NCHAR(214),NCHAR(246),NCHAR(350),NCHAR(351),NCHAR(220),NCHAR(252)),
                N'iiiccggoossuu'
            )) PERSISTED;
    END

    -- otel adı
    IF COL_LENGTH(N'dbo.oteller', N'otel_adi_normalized') IS NULL
    BEGIN
        ALTER TABLE dbo.oteller
        ADD otel_adi_normalized AS
            LOWER(TRANSLATE(
                COALESCE(otel_adi, N''),
                CONCAT(NCHAR(304),NCHAR(73),NCHAR(305),NCHAR(199),NCHAR(231),NCHAR(286),NCHAR(287),NCHAR(214),NCHAR(246),NCHAR(350),NCHAR(351),NCHAR(220),NCHAR(252)),
                N'iiiccggoossuu'
            )) PERSISTED;
    END

    -- Konum birleşik (mahalle + ilçe + şehir)
    IF COL_LENGTH(N'dbo.oteller', N'konum_normalized') IS NULL
    BEGIN
        ALTER TABLE dbo.oteller
        ADD konum_normalized AS
            LOWER(TRANSLATE(
                CONCAT(COALESCE(mahalle, N''), N' ', COALESCE(ilce, N''), N' ', COALESCE(sehir, N'')),
                CONCAT(NCHAR(304),NCHAR(73),NCHAR(305),NCHAR(199),NCHAR(231),NCHAR(286),NCHAR(287),NCHAR(214),NCHAR(246),NCHAR(350),NCHAR(351),NCHAR(220),NCHAR(252)),
                N'iiiccggoossuu'
            )) PERSISTED;
    END

    -- İndeksler (idempotent)
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_oteller_search_norm' AND object_id = OBJECT_ID(N'dbo.oteller'))
    BEGIN
        CREATE NONCLUSTERED INDEX IX_oteller_search_norm
        ON dbo.oteller (yayin_durumu, onay_durumu, sehir_normalized, ilce_normalized, mahalle_normalized)
        INCLUDE (otel_adi, otel_adi_normalized, konum_normalized, kapak_fotografi, yildiz_sayisi, ortalama_puan, toplam_yorum_sayisi, populerlik_sirasi, enlem, boylam, one_cikan_otel, tavsiye_edilen_otel);
    END

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_oteller_name_norm' AND object_id = OBJECT_ID(N'dbo.oteller'))
    BEGIN
        CREATE NONCLUSTERED INDEX IX_oteller_name_norm
        ON dbo.oteller (yayin_durumu, onay_durumu, otel_adi_normalized)
        INCLUDE (sehir_normalized, ilce_normalized, mahalle_normalized, populerlik_sirasi, ortalama_puan, toplam_yorum_sayisi);
    END
END

