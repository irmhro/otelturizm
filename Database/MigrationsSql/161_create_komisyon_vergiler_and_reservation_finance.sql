IF OBJECT_ID('dbo.komisyon_vergiler', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.komisyon_vergiler
    (
        id BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        otel_id BIGINT NOT NULL,
        baslangic_tarihi DATE NOT NULL,
        bitis_tarihi DATE NULL,
        komisyon_orani DECIMAL(5,2) NOT NULL CONSTRAINT DF_komisyon_vergiler_komisyon_orani DEFAULT (0),
        komisyon_gelir_vergisi_orani DECIMAL(5,2) NOT NULL CONSTRAINT DF_komisyon_vergiler_komisyon_gelir_vergisi_orani DEFAULT (0),
        kdv_orani DECIMAL(5,2) NOT NULL CONSTRAINT DF_komisyon_vergiler_kdv_orani DEFAULT (0),
        konaklama_vergisi_orani DECIMAL(5,2) NOT NULL CONSTRAINT DF_komisyon_vergiler_konaklama_vergisi_orani DEFAULT (0),
        para_birimi NVARCHAR(3) NOT NULL CONSTRAINT DF_komisyon_vergiler_para_birimi DEFAULT (N'TRY'),
        aktif_mi BIT NOT NULL CONSTRAINT DF_komisyon_vergiler_aktif_mi DEFAULT (1),
        aciklama NVARCHAR(500) NULL,
        olusturan_kullanici_id BIGINT NULL,
        guncelleyen_kullanici_id BIGINT NULL,
        olusturulma_tarihi DATETIME2 NOT NULL CONSTRAINT DF_komisyon_vergiler_olusturulma_tarihi DEFAULT (SYSUTCDATETIME()),
        guncellenme_tarihi DATETIME2 NULL
    );

    CREATE INDEX IX_komisyon_vergiler_otel_tarih
        ON dbo.komisyon_vergiler (otel_id, aktif_mi, baslangic_tarihi, bitis_tarihi);
END;

IF COL_LENGTH('dbo.rezervasyonlar', 'komisyon_vergi_kural_id') IS NULL
BEGIN
    ALTER TABLE dbo.rezervasyonlar ADD komisyon_vergi_kural_id BIGINT NULL;
END;

IF COL_LENGTH('dbo.rezervasyonlar', 'net_oda_tutari') IS NULL
BEGIN
    ALTER TABLE dbo.rezervasyonlar ADD net_oda_tutari DECIMAL(18,2) NULL;
END;

IF COL_LENGTH('dbo.rezervasyonlar', 'kdv_orani') IS NULL
BEGIN
    ALTER TABLE dbo.rezervasyonlar ADD kdv_orani DECIMAL(5,2) NULL;
END;

IF COL_LENGTH('dbo.rezervasyonlar', 'kdv_tutari') IS NULL
BEGIN
    ALTER TABLE dbo.rezervasyonlar ADD kdv_tutari DECIMAL(18,2) NULL;
END;

IF COL_LENGTH('dbo.rezervasyonlar', 'konaklama_vergisi_orani') IS NULL
BEGIN
    ALTER TABLE dbo.rezervasyonlar ADD konaklama_vergisi_orani DECIMAL(5,2) NULL;
END;

IF COL_LENGTH('dbo.rezervasyonlar', 'konaklama_vergisi_tutari') IS NULL
BEGIN
    ALTER TABLE dbo.rezervasyonlar ADD konaklama_vergisi_tutari DECIMAL(18,2) NULL;
END;

IF COL_LENGTH('dbo.rezervasyonlar', 'toplam_vergi_tutari') IS NULL
BEGIN
    ALTER TABLE dbo.rezervasyonlar ADD toplam_vergi_tutari DECIMAL(18,2) NULL;
END;

IF COL_LENGTH('dbo.rezervasyonlar', 'komisyon_gelir_vergisi_orani') IS NULL
BEGIN
    ALTER TABLE dbo.rezervasyonlar ADD komisyon_gelir_vergisi_orani DECIMAL(5,2) NULL;
END;

IF COL_LENGTH('dbo.rezervasyonlar', 'komisyon_gelir_vergisi_tutari') IS NULL
BEGIN
    ALTER TABLE dbo.rezervasyonlar ADD komisyon_gelir_vergisi_tutari DECIMAL(18,2) NULL;
END;

IF COL_LENGTH('dbo.rezervasyonlar', 'platform_net_komisyon_tutari') IS NULL
BEGIN
    ALTER TABLE dbo.rezervasyonlar ADD platform_net_komisyon_tutari DECIMAL(18,2) NULL;
END;

IF COL_LENGTH('dbo.rezervasyonlar', 'kapida_odeme_tutari') IS NULL
BEGIN
    ALTER TABLE dbo.rezervasyonlar ADD kapida_odeme_tutari DECIMAL(18,2) NULL;
END;

IF COL_LENGTH('dbo.rezervasyonlar', 'kapida_odeme_durumu') IS NULL
BEGIN
    ALTER TABLE dbo.rezervasyonlar ADD kapida_odeme_durumu NVARCHAR(50) NULL;
END;

IF COL_LENGTH('dbo.rezervasyonlar', 'online_odeme_tutari') IS NULL
BEGIN
    ALTER TABLE dbo.rezervasyonlar ADD online_odeme_tutari DECIMAL(18,2) NULL;
END;

IF COL_LENGTH('dbo.rezervasyonlar', 'online_odeme_durumu') IS NULL
BEGIN
    ALTER TABLE dbo.rezervasyonlar ADD online_odeme_durumu NVARCHAR(50) NULL;
END;

IF COL_LENGTH('dbo.rezervasyonlar', 'tahsil_edilen_tutar') IS NULL
BEGIN
    ALTER TABLE dbo.rezervasyonlar ADD tahsil_edilen_tutar DECIMAL(18,2) NULL;
END;

IF COL_LENGTH('dbo.rezervasyonlar', 'kalan_tahsil_edilecek_tutar') IS NULL
BEGIN
    ALTER TABLE dbo.rezervasyonlar ADD kalan_tahsil_edilecek_tutar DECIMAL(18,2) NULL;
END;

IF COL_LENGTH('dbo.rezervasyonlar', 'vergiler_dahil_toplam_tutar') IS NULL
BEGIN
    ALTER TABLE dbo.rezervasyonlar ADD vergiler_dahil_toplam_tutar DECIMAL(18,2) NULL;
END;

IF COL_LENGTH('dbo.rezervasyonlar', 'odeme_referans_no') IS NULL
BEGIN
    ALTER TABLE dbo.rezervasyonlar ADD odeme_referans_no NVARCHAR(100) NULL;
END;

IF COL_LENGTH('dbo.rezervasyonlar', 'muhasebe_notu') IS NULL
BEGIN
    ALTER TABLE dbo.rezervasyonlar ADD muhasebe_notu NVARCHAR(500) NULL;
END;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = 'FK_rezervasyonlar_komisyon_vergiler'
)
BEGIN
    ALTER TABLE dbo.rezervasyonlar
        ADD CONSTRAINT FK_rezervasyonlar_komisyon_vergiler
        FOREIGN KEY (komisyon_vergi_kural_id) REFERENCES dbo.komisyon_vergiler(id);
END;

INSERT INTO dbo.komisyon_vergiler
(
    otel_id,
    baslangic_tarihi,
    bitis_tarihi,
    komisyon_orani,
    komisyon_gelir_vergisi_orani,
    kdv_orani,
    konaklama_vergisi_orani,
    para_birimi,
    aktif_mi,
    aciklama,
    olusturulma_tarihi
)
SELECT
    o.id,
    CAST('2020-01-01' AS date),
    NULL,
    COALESCE(o.varsayilan_komisyon_orani, 0),
    20.00,
    10.00,
    2.00,
    N'TRY',
    1,
    N'Varsayılan MSSQL komisyon ve vergi kuralı',
    SYSUTCDATETIME()
FROM dbo.oteller o
WHERE NOT EXISTS
(
    SELECT 1
    FROM dbo.komisyon_vergiler kv
    WHERE kv.otel_id = o.id
);

UPDATE r
SET
    komisyon_vergi_kural_id = COALESCE(r.komisyon_vergi_kural_id, kv.id),
    net_oda_tutari = COALESCE(r.net_oda_tutari, r.toplam_oda_tutari, 0),
    kdv_orani = COALESCE(r.kdv_orani, kv.kdv_orani, 0),
    konaklama_vergisi_orani = COALESCE(r.konaklama_vergisi_orani, kv.konaklama_vergisi_orani, 0),
    kdv_tutari = COALESCE(r.kdv_tutari, ROUND(COALESCE(r.toplam_oda_tutari, 0) * COALESCE(kv.kdv_orani, 0) / 100.0, 2), 0),
    konaklama_vergisi_tutari = COALESCE(r.konaklama_vergisi_tutari, ROUND(COALESCE(r.toplam_oda_tutari, 0) * COALESCE(kv.konaklama_vergisi_orani, 0) / 100.0, 2), 0),
    toplam_vergi_tutari = COALESCE(r.toplam_vergi_tutari, r.vergi_tutari, 0),
    komisyon_gelir_vergisi_orani = COALESCE(r.komisyon_gelir_vergisi_orani, kv.komisyon_gelir_vergisi_orani, 0),
    komisyon_gelir_vergisi_tutari = COALESCE(r.komisyon_gelir_vergisi_tutari, ROUND(COALESCE(r.komisyon_tutari, 0) * COALESCE(kv.komisyon_gelir_vergisi_orani, 0) / 100.0, 2), 0),
    platform_net_komisyon_tutari = COALESCE(r.platform_net_komisyon_tutari, COALESCE(r.komisyon_tutari, 0) - ROUND(COALESCE(r.komisyon_tutari, 0) * COALESCE(kv.komisyon_gelir_vergisi_orani, 0) / 100.0, 2)),
    kapida_odeme_tutari = COALESCE(r.kapida_odeme_tutari, CASE WHEN COALESCE(r.odeme_yontemi, N'') IN (N'Kapıda Ödeme', N'Nakit') THEN COALESCE(r.toplam_tutar, 0) ELSE 0 END),
    kapida_odeme_durumu = COALESCE(r.kapida_odeme_durumu, CASE WHEN COALESCE(r.odeme_yontemi, N'') IN (N'Kapıda Ödeme', N'Nakit') THEN COALESCE(r.odeme_durumu, N'Beklemede') ELSE N'Uygulanmıyor' END),
    online_odeme_tutari = COALESCE(r.online_odeme_tutari, CASE WHEN COALESCE(r.odeme_yontemi, N'') IN (N'Sanal POS', N'Online Ödeme', N'Kredi Kartı') THEN COALESCE(r.toplam_tutar, 0) ELSE 0 END),
    online_odeme_durumu = COALESCE(r.online_odeme_durumu, CASE WHEN COALESCE(r.odeme_yontemi, N'') IN (N'Sanal POS', N'Online Ödeme', N'Kredi Kartı') THEN COALESCE(r.odeme_durumu, N'Beklemede') ELSE N'Uygulanmıyor' END),
    tahsil_edilen_tutar = COALESCE(r.tahsil_edilen_tutar, CASE WHEN COALESCE(r.odeme_durumu, N'') IN (N'Tamamlandı', N'Ön Ödeme Alındı') THEN COALESCE(r.toplam_tutar, 0) ELSE 0 END),
    kalan_tahsil_edilecek_tutar = COALESCE(r.kalan_tahsil_edilecek_tutar, CASE WHEN COALESCE(r.odeme_durumu, N'') IN (N'Tamamlandı') THEN 0 ELSE COALESCE(r.toplam_tutar, 0) - COALESCE(r.on_odeme_tutari, 0) END),
    vergiler_dahil_toplam_tutar = COALESCE(r.vergiler_dahil_toplam_tutar, r.toplam_tutar, 0)
FROM dbo.rezervasyonlar r
OUTER APPLY
(
    SELECT TOP (1) kv.*
    FROM dbo.komisyon_vergiler kv
    WHERE kv.otel_id = r.otel_id
      AND kv.aktif_mi = 1
      AND kv.baslangic_tarihi <= CAST(COALESCE(r.giris_tarihi, GETDATE()) AS date)
      AND (kv.bitis_tarihi IS NULL OR kv.bitis_tarihi >= CAST(COALESCE(r.giris_tarihi, GETDATE()) AS date))
    ORDER BY kv.baslangic_tarihi DESC, kv.id DESC
) kv;
