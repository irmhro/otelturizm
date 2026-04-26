-- SQL Server indeks paketi (idempotent)
-- Hedef: oda_fiyat_musaitlik + firma_oda_fiyat_musaitlik yoğun sorguları

-- 1) oda_fiyat_musaitlik: (otel_id, oda_tip_id, tarih) - benzersiz/lookup
IF OBJECT_ID(N'dbo.oda_fiyat_musaitlik', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE name = N'UX_oda_fiyat_musaitlik_otel_oda_tarih'
          AND object_id = OBJECT_ID(N'dbo.oda_fiyat_musaitlik')
    )
    BEGIN
        -- Unique tercih edilir; mevcut veride çakışma varsa non-unique'e düşürün.
        CREATE UNIQUE NONCLUSTERED INDEX UX_oda_fiyat_musaitlik_otel_oda_tarih
        ON dbo.oda_fiyat_musaitlik(otel_id, oda_tip_id, tarih);
    END

    IF NOT EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE name = N'IX_oda_fiyat_musaitlik_oda_otel_tarih_include'
          AND object_id = OBJECT_ID(N'dbo.oda_fiyat_musaitlik')
    )
    BEGIN
        CREATE NONCLUSTERED INDEX IX_oda_fiyat_musaitlik_oda_otel_tarih_include
        ON dbo.oda_fiyat_musaitlik(oda_tip_id, otel_id, tarih)
        INCLUDE (gecelik_fiyat, indirimli_fiyat, kampanya_id, toplam_oda_sayisi, satilan_oda_sayisi, bloke_oda_sayisi, minimum_geceleme, maksimum_geceleme, kapali_satis);
    END

    IF NOT EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE name = N'IX_oda_fiyat_musaitlik_discount_only'
          AND object_id = OBJECT_ID(N'dbo.oda_fiyat_musaitlik')
    )
    BEGIN
        CREATE NONCLUSTERED INDEX IX_oda_fiyat_musaitlik_discount_only
        ON dbo.oda_fiyat_musaitlik(otel_id, tarih, oda_tip_id)
        INCLUDE (indirimli_fiyat)
        WHERE indirimli_fiyat IS NOT NULL;
    END
END

-- 2) firma_oda_fiyat_musaitlik: sorgular otel_id ile de filtreliyor
IF OBJECT_ID(N'dbo.firma_oda_fiyat_musaitlik', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE name = N'UX_firma_oda_fiyat_musaitlik_firma_otel_oda_tarih'
          AND object_id = OBJECT_ID(N'dbo.firma_oda_fiyat_musaitlik')
    )
    BEGIN
        CREATE UNIQUE NONCLUSTERED INDEX UX_firma_oda_fiyat_musaitlik_firma_otel_oda_tarih
        ON dbo.firma_oda_fiyat_musaitlik(firma_id, otel_id, oda_tip_id, tarih);
    END

    IF NOT EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE name = N'IX_firma_oda_fiyat_musaitlik_read'
          AND object_id = OBJECT_ID(N'dbo.firma_oda_fiyat_musaitlik')
    )
    BEGIN
        CREATE NONCLUSTERED INDEX IX_firma_oda_fiyat_musaitlik_read
        ON dbo.firma_oda_fiyat_musaitlik(firma_id, otel_id, oda_tip_id, tarih)
        INCLUDE (firma_gecelik_fiyat, kapali_satis, aktif_mi, guncellenme_tarihi);
    END
END

