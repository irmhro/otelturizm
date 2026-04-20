IF COL_LENGTH('dbo.rezervasyon_taslaklari', 'net_oda_tutari') IS NULL
BEGIN
    ALTER TABLE dbo.rezervasyon_taslaklari
    ADD net_oda_tutari DECIMAL(18,2) NULL;
END
GO

IF COL_LENGTH('dbo.rezervasyon_taslaklari', 'kdv_orani') IS NULL
BEGIN
    ALTER TABLE dbo.rezervasyon_taslaklari
    ADD kdv_orani DECIMAL(9,4) NULL;
END
GO

IF COL_LENGTH('dbo.rezervasyon_taslaklari', 'kdv_tutari') IS NULL
BEGIN
    ALTER TABLE dbo.rezervasyon_taslaklari
    ADD kdv_tutari DECIMAL(18,2) NULL;
END
GO

IF COL_LENGTH('dbo.rezervasyon_taslaklari', 'konaklama_vergisi_orani') IS NULL
BEGIN
    ALTER TABLE dbo.rezervasyon_taslaklari
    ADD konaklama_vergisi_orani DECIMAL(9,4) NULL;
END
GO

IF COL_LENGTH('dbo.rezervasyon_taslaklari', 'konaklama_vergisi_tutari') IS NULL
BEGIN
    ALTER TABLE dbo.rezervasyon_taslaklari
    ADD konaklama_vergisi_tutari DECIMAL(18,2) NULL;
END
GO

UPDATE rt
SET
    rt.net_oda_tutari = COALESCE(rt.net_oda_tutari, rt.toplam_tutar - COALESCE(rt.vergi_tutari, 0), rt.toplam_tutar, 0),
    rt.kdv_orani = COALESCE(rt.kdv_orani, kgv.kdv_orani, 10),
    rt.konaklama_vergisi_orani = COALESCE(rt.konaklama_vergisi_orani, kgv.konaklama_vergisi_orani, 2),
    rt.kdv_tutari = COALESCE(rt.kdv_tutari, ROUND((COALESCE(rt.net_oda_tutari, rt.toplam_tutar - COALESCE(rt.vergi_tutari, 0), rt.toplam_tutar, 0)) * (COALESCE(kgv.kdv_orani, 10) / 100.0), 2)),
    rt.konaklama_vergisi_tutari = COALESCE(rt.konaklama_vergisi_tutari, ROUND((COALESCE(rt.net_oda_tutari, rt.toplam_tutar - COALESCE(rt.vergi_tutari, 0), rt.toplam_tutar, 0)) * (COALESCE(kgv.konaklama_vergisi_orani, 2) / 100.0), 2))
FROM dbo.rezervasyon_taslaklari rt
OUTER APPLY (
    SELECT TOP (1)
        kv.kdv_orani,
        kv.konaklama_vergisi_orani
    FROM dbo.komisyon_vergiler kv
    WHERE kv.otel_id = rt.otel_id
      AND kv.aktif_mi = 1
    ORDER BY kv.baslangic_tarihi DESC, kv.id DESC
) kgv;
GO
