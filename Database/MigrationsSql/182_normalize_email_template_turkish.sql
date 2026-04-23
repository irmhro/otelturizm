UPDATE bildirim_sablonlari
SET
    sablon_adi = N'S' + NCHAR(246) + N'zle' + NCHAR(351) + N'me Bildirimi',
    baslik = N'S' + NCHAR(246) + N'zle' + NCHAR(351) + N'me Paketi'
WHERE sablon_kodu = N'contract_delivery';

UPDATE bildirim_sablonlari
SET
    konu = N'E-posta adresinizi onaylay' + NCHAR(305) + N'n'
WHERE sablon_kodu = N'email_verify';

UPDATE bildirim_sablonlari
SET
    sablon_adi = N'Favori Fiyat Alarm' + NCHAR(305) + N' E' + NCHAR(351) + N'le' + NCHAR(351) + N'mesi',
    konu = N'Favorinizde takip etti' + NCHAR(287) + N'iniz otelde fiyat d' + NCHAR(252) + N'' + NCHAR(351) + N't' + NCHAR(252),
    baslik = N'Favori Fiyat Alarm' + NCHAR(305)
WHERE sablon_kodu = N'favorite_price_alert_match';

UPDATE bildirim_sablonlari
SET
    sablon_adi = N'Giri' + NCHAR(351) + N' ' + NCHAR(304) + N'ki A' + NCHAR(351) + N'amal' + NCHAR(305) + N' Do' + NCHAR(287) + N'rulama E-postas' + NCHAR(305),
    konu = N'Giri' + NCHAR(351) + N' g' + NCHAR(252) + N'venlik kodunuz',
    baslik = N'G' + NCHAR(252) + N'venlik Kodunuz'
WHERE sablon_kodu = N'login_2fa_email';

UPDATE bildirim_sablonlari
SET
    sablon_adi = N'Partner Rezervasyon ' + NCHAR(304) + N'ptal Bildirimi',
    baslik = N'Partner Rezervasyon ' + NCHAR(304) + N'ptal'
WHERE sablon_kodu = N'reservation_cancelled_partner';

UPDATE bildirim_sablonlari
SET
    konu = N'Rezervasyonunuz onayland' + NCHAR(305)
WHERE sablon_kodu = N'reservation_confirmed_customer';

UPDATE bildirim_sablonlari
SET
    sablon_adi = N'Partner Mesaj' + NCHAR(305) + N' - Rezervasyon',
    konu = N'Rezervasyonunuz i' + NCHAR(231) + N'in yeni mesaj var',
    baslik = N'Rezervasyon Mesaj' + NCHAR(305)
WHERE sablon_kodu = N'reservation_guest_message';

UPDATE bildirim_sablonlari
SET
    konu = N'Yeni rezervasyon onay' + NCHAR(305)
WHERE sablon_kodu = N'reservation_new_partner';

UPDATE bildirim_sablonlari
SET
    konu = N'Rezervasyon talebiniz al' + NCHAR(305) + N'nd' + NCHAR(305)
WHERE sablon_kodu = N'reservation_received_customer';

UPDATE bildirim_sablonlari
SET
    konu = N'Rezervasyon talebinizde g' + NCHAR(252) + N'ncelleme var'
WHERE sablon_kodu = N'reservation_rejected_customer';
