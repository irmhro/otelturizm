INSERT INTO bildirim_sablonlari
(sablon_kodu, sablon_adi, tur, dil, konu, baslik, icerik, degiskenler, aktif_mi)
SELECT
    'reservation_rejected_customer',
    'Rezervasyon Reddedildi',
    'E-posta',
    'tr',
    'Rezervasyon talebinizde guncelleme var',
    'Rezervasyon Reddedildi',
    'Views/Email/Rezervasyon Reddedildi.cshtml',
    JSON_ARRAY('{{user_first_name}}','{{booking_reference}}','{{hotel_name}}','{{check_in_date}}','{{check_out_date}}','{{room_type_name}}','{{total_price}}','{{rejection_reason}}'),
    1
WHERE NOT EXISTS (
    SELECT 1
    FROM bildirim_sablonlari
    WHERE sablon_kodu = 'reservation_rejected_customer'
      AND tur = 'E-posta'
      AND dil = 'tr'
);

INSERT INTO bildirim_sablonlari
(sablon_kodu, sablon_adi, tur, dil, konu, baslik, icerik, degiskenler, aktif_mi)
SELECT
    'reservation_guest_message',
    'Partner Mesaji - Rezervasyon',
    'E-posta',
    'tr',
    'Rezervasyonunuz icin yeni mesaj var',
    'Rezervasyon Mesaji',
    'Views/Email/Rezervasyon Mesaji.cshtml',
    JSON_ARRAY('{{user_first_name}}','{{booking_reference}}','{{message_subject}}','{{message_text}}'),
    1
WHERE NOT EXISTS (
    SELECT 1
    FROM bildirim_sablonlari
    WHERE sablon_kodu = 'reservation_guest_message'
      AND tur = 'E-posta'
      AND dil = 'tr'
);
