INSERT INTO otel_ozellik_iliskileri (otel_id, ozellik_id, ek_ucret, aciklama)
SELECT
    o.id,
    oz.id,
    NULL,
    'Varsayilan temel ozellik baglantisi'
FROM oteller o
JOIN otel_ozellikleri oz
    ON oz.ozellik_adi IN ('24 Saat Resepsiyon', 'Ücretsiz WiFi', 'Restoran')
LEFT JOIN otel_ozellik_iliskileri i
    ON i.otel_id = o.id
   AND i.ozellik_id = oz.id
WHERE i.otel_id IS NULL;
