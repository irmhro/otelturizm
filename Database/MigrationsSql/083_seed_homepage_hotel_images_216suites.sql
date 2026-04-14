UPDATE oteller
SET
    kapak_fotografi = '/uploads/hotels/216suites/216-bosphorus-1.png',
    one_cikan_otel = 1,
    tavsiye_edilen_otel = 1,
    yayin_durumu = 'Yayında',
    onay_durumu = 'Onaylandı'
WHERE otel_adi LIKE '216 BOSPHORUS%';

UPDATE oteller
SET
    kapak_fotografi = '/uploads/hotels/216suites/216-comfort-inn-1.png',
    one_cikan_otel = 1,
    tavsiye_edilen_otel = 1,
    yayin_durumu = 'Yayında',
    onay_durumu = 'Onaylandı'
WHERE otel_adi LIKE 'COMFORT INN%';

UPDATE oteller
SET
    kapak_fotografi = '/uploads/hotels/216suites/216-macity-1.png',
    one_cikan_otel = 1,
    tavsiye_edilen_otel = 1,
    yayin_durumu = 'Yayında',
    onay_durumu = 'Onaylandı'
WHERE otel_adi LIKE 'MACITY%';

UPDATE oteller
SET
    kapak_fotografi = '/uploads/hotels/216suites/216-silver-1.jpg',
    one_cikan_otel = 1,
    tavsiye_edilen_otel = 1,
    yayin_durumu = 'Yayında',
    onay_durumu = 'Onaylandı'
WHERE otel_adi LIKE '216 SILVER%';

INSERT INTO otel_gorselleri (
    otel_id,
    gorsel_url,
    gorsel_turu,
    baslik,
    aciklama,
    kapak_fotografi_mi,
    one_cikan,
    siralama,
    onay_durumu
)
SELECT
    o.id,
    '/uploads/hotels/216suites/216-bosphorus-1.png',
    'Genel Alan',
    '216 Bosphorus Kapak Gorseli',
    '216suites.com sitesinden alinan lokal anasayfa gorseli.',
    1,
    1,
    1,
    'Onaylandı'
FROM oteller o
WHERE o.otel_adi LIKE '216 BOSPHORUS%'
  AND NOT EXISTS (
      SELECT 1
      FROM otel_gorselleri og
      WHERE og.otel_id = o.id
        AND og.gorsel_url = '/uploads/hotels/216suites/216-bosphorus-1.png'
  );

INSERT INTO otel_gorselleri (
    otel_id,
    gorsel_url,
    gorsel_turu,
    baslik,
    aciklama,
    kapak_fotografi_mi,
    one_cikan,
    siralama,
    onay_durumu
)
SELECT
    o.id,
    '/uploads/hotels/216suites/216-comfort-inn-1.png',
    'Genel Alan',
    '216 Comfort Inn Kapak Gorseli',
    '216suites.com sitesinden alinan lokal anasayfa gorseli.',
    1,
    1,
    1,
    'Onaylandı'
FROM oteller o
WHERE o.otel_adi LIKE 'COMFORT INN%'
  AND NOT EXISTS (
      SELECT 1
      FROM otel_gorselleri og
      WHERE og.otel_id = o.id
        AND og.gorsel_url = '/uploads/hotels/216suites/216-comfort-inn-1.png'
  );

INSERT INTO otel_gorselleri (
    otel_id,
    gorsel_url,
    gorsel_turu,
    baslik,
    aciklama,
    kapak_fotografi_mi,
    one_cikan,
    siralama,
    onay_durumu
)
SELECT
    o.id,
    '/uploads/hotels/216suites/216-macity-1.png',
    'Genel Alan',
    '216 Macity Kapak Gorseli',
    '216suites.com sitesinden alinan lokal anasayfa gorseli.',
    1,
    1,
    1,
    'Onaylandı'
FROM oteller o
WHERE o.otel_adi LIKE 'MACITY%'
  AND NOT EXISTS (
      SELECT 1
      FROM otel_gorselleri og
      WHERE og.otel_id = o.id
        AND og.gorsel_url = '/uploads/hotels/216suites/216-macity-1.png'
  );

INSERT INTO otel_gorselleri (
    otel_id,
    gorsel_url,
    gorsel_turu,
    baslik,
    aciklama,
    kapak_fotografi_mi,
    one_cikan,
    siralama,
    onay_durumu
)
SELECT
    o.id,
    '/uploads/hotels/216suites/216-silver-1.jpg',
    'Genel Alan',
    '216 Silver Kapak Gorseli',
    '216suites.com sitesinden alinan lokal anasayfa gorseli.',
    1,
    1,
    1,
    'Onaylandı'
FROM oteller o
WHERE o.otel_adi LIKE '216 SILVER%'
  AND NOT EXISTS (
      SELECT 1
      FROM otel_gorselleri og
      WHERE og.otel_id = o.id
        AND og.gorsel_url = '/uploads/hotels/216suites/216-silver-1.jpg'
  );
