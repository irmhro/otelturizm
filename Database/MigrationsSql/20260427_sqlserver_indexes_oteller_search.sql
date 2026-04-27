-- SQL Server indeks paketi (idempotent)
-- Hedef: otel listeleme aramaları (sehir/ilce/mahalle/otel_adi) ve yayin/onay filtreleri

IF OBJECT_ID(N'dbo.oteller', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_oteller_yayin_onay_sehir_ilce' AND object_id = OBJECT_ID(N'dbo.oteller'))
    BEGIN
        CREATE NONCLUSTERED INDEX IX_oteller_yayin_onay_sehir_ilce
        ON dbo.oteller (yayin_durumu, onay_durumu, sehir, ilce)
        INCLUDE (mahalle, otel_adi, kapak_fotografi, yildiz_sayisi, ortalama_puan, toplam_yorum_sayisi, populerlik_sirasi, enlem, boylam, one_cikan_otel, tavsiye_edilen_otel);
    END

    IF COL_LENGTH(N'dbo.oteller', N'mahalle') IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_oteller_yayin_onay_mahalle' AND object_id = OBJECT_ID(N'dbo.oteller'))
    BEGIN
        CREATE NONCLUSTERED INDEX IX_oteller_yayin_onay_mahalle
        ON dbo.oteller (yayin_durumu, onay_durumu, mahalle)
        INCLUDE (sehir, ilce, otel_adi, populerlik_sirasi, ortalama_puan, toplam_yorum_sayisi);
    END

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_oteller_yayin_onay_oteladi' AND object_id = OBJECT_ID(N'dbo.oteller'))
    BEGIN
        CREATE NONCLUSTERED INDEX IX_oteller_yayin_onay_oteladi
        ON dbo.oteller (yayin_durumu, onay_durumu, otel_adi)
        INCLUDE (sehir, ilce, mahalle, populerlik_sirasi, ortalama_puan, toplam_yorum_sayisi);
    END
END

-- Not: normalize/LIKE/fonksiyonlu aramalar için ideal çözüm computed/persisted normalized kolon + index'tir.
-- Bu dosya, mevcut şemaya minimum müdahale ile ilk hız kazancını hedefler.

