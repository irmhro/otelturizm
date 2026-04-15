INSERT INTO bildirim_sablonlari
(sablon_kodu, sablon_adi, tur, dil, konu, baslik, icerik, degiskenler, aktif_mi)
SELECT
    'favorite_price_alert_match',
    'Favori Fiyat Alarmi Eslesmesi',
    'E-posta',
    'tr',
    'Favorinizde takip ettiginiz otelde fiyat dustu',
    'Favori Fiyat Alarmi',
    'Views/Email/Favori Fiyat Alarmi.cshtml',
    JSON_ARRAY('{{user_first_name}}','{{hotel_name}}','{{target_price}}','{{matched_price}}','{{matched_date}}','{{favorites_link}}','{{hotel_link}}'),
    1
WHERE NOT EXISTS (
    SELECT 1
    FROM bildirim_sablonlari
    WHERE sablon_kodu = 'favorite_price_alert_match'
      AND tur = 'E-posta'
      AND dil = 'tr'
);
