INSERT INTO otel_ozellik_iliskileri (otel_id, ozellik_id)
SELECT o.id, z.id
FROM oteller o
JOIN otel_ozellikleri z ON z.ozellik_adi = 'Ücretsiz WiFi'
WHERE (
        o.otel_adi LIKE '216 BOSPHORUS%'
     OR o.otel_adi LIKE 'COMFORT INN%'
     OR o.otel_adi LIKE 'MACITY%'
     OR o.otel_adi LIKE '216 SILVER%'
)
AND NOT EXISTS (
    SELECT 1
    FROM otel_ozellik_iliskileri i
    WHERE i.otel_id = o.id
      AND i.ozellik_id = z.id
);

INSERT INTO otel_ozellik_iliskileri (otel_id, ozellik_id)
SELECT o.id, z.id
FROM oteller o
JOIN otel_ozellikleri z ON z.ozellik_adi = 'Açık Yüzme Havuzu'
WHERE (
        o.otel_adi LIKE '216 BOSPHORUS%'
     OR o.otel_adi LIKE 'COMFORT INN%'
     OR o.otel_adi LIKE 'MACITY%'
)
AND NOT EXISTS (
    SELECT 1
    FROM otel_ozellik_iliskileri i
    WHERE i.otel_id = o.id
      AND i.ozellik_id = z.id
);

INSERT INTO otel_ozellik_iliskileri (otel_id, ozellik_id)
SELECT o.id, z.id
FROM oteller o
JOIN otel_ozellikleri z ON z.ozellik_adi = 'SPA ve Sağlık Merkezi'
WHERE (
        o.otel_adi LIKE '216 BOSPHORUS%'
     OR o.otel_adi LIKE 'COMFORT INN%'
)
AND NOT EXISTS (
    SELECT 1
    FROM otel_ozellik_iliskileri i
    WHERE i.otel_id = o.id
      AND i.ozellik_id = z.id
);

INSERT INTO otel_ozellik_iliskileri (otel_id, ozellik_id)
SELECT o.id, z.id
FROM oteller o
JOIN otel_ozellikleri z ON z.ozellik_adi = 'Açık Büfe Kahvaltı'
WHERE (
        o.otel_adi LIKE 'MACITY%'
     OR o.otel_adi LIKE '216 SILVER%'
)
AND NOT EXISTS (
    SELECT 1
    FROM otel_ozellik_iliskileri i
    WHERE i.otel_id = o.id
      AND i.ozellik_id = z.id
);
