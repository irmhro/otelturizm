INSERT INTO bildirim_sablonlari
(sablon_kodu, sablon_adi, tur, dil, konu, baslik, icerik, degiskenler, aktif_mi)
SELECT
    'reservation_cancelled_partner',
    'Partner Rezervasyon Iptal Bildirimi',
    'E-posta',
    'tr',
    'Misafir rezervasyonu iptal etti',
    'Partner Rezervasyon Iptal',
    'Views/Email/Partner Rezervasyon Iptal.cshtml',
    JSON_ARRAY('{{hotel_manager_name}}','{{hotel_name}}','{{booking_reference}}','{{guest_full_name}}','{{check_in_date}}','{{check_out_date}}','{{room_type_name}}','{{total_price}}','{{cancel_reason}}'),
    1
WHERE NOT EXISTS (
    SELECT 1
    FROM bildirim_sablonlari
    WHERE sablon_kodu = 'reservation_cancelled_partner'
      AND tur = 'E-posta'
      AND dil = 'tr'
);
