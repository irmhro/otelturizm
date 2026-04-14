-- Legacy aktarim icin acilmis gecici partner kullanicilarini temizler.
-- Yalnizca artik aktif partner/otel iliskisi olmayan kayitlar silinir.

DROP TEMPORARY TABLE IF EXISTS tmp_legacy_partner_cleanup_users;

CREATE TEMPORARY TABLE tmp_legacy_partner_cleanup_users AS
SELECT u.id
FROM users u
LEFT JOIN users_partner up
    ON up.user_id = u.id
LEFT JOIN partner_detaylari pd
    ON pd.kullanici_id = u.id
WHERE (
        u.eposta LIKE 'legacy-partner-%@otelturizm.com'
        OR u.ad_soyad LIKE 'Legacy Partner %'
    )
  AND up.user_id IS NULL
  AND pd.id IS NULL;

DELETE u
FROM users u
JOIN tmp_legacy_partner_cleanup_users x
    ON x.id = u.id;

DROP TEMPORARY TABLE IF EXISTS tmp_legacy_partner_cleanup_users;
