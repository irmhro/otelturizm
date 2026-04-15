INSERT INTO email_services
(servis_kodu, servis_adi, saglayici, varsayilan_mi, aktif_mi, gonderen_ad, gonderen_eposta, yanitla_eposta, smtp_port, guvenlik_tipi, test_modu, metadata)
VALUES
('default_smtp', 'Varsayilan SMTP Servisi', 'SMTP', 1, 1, 'Otelturizm', 'no-reply@otelturizm.com', 'destek@otelturizm.com', 587, 'TLS', 1, JSON_OBJECT('note', 'SMTP bilgileri sonra admin panelinden guncellenecek.'))
ON DUPLICATE KEY UPDATE
servis_adi = VALUES(servis_adi),
aktif_mi = VALUES(aktif_mi),
varsayilan_mi = VALUES(varsayilan_mi),
gonderen_ad = VALUES(gonderen_ad),
gonderen_eposta = VALUES(gonderen_eposta),
yanitla_eposta = VALUES(yanitla_eposta),
test_modu = VALUES(test_modu),
metadata = VALUES(metadata);

INSERT INTO bildirim_sablonlari
(sablon_kodu, sablon_adi, tur, dil, konu, baslik, icerik, degiskenler, aktif_mi)
VALUES
('email_verify', 'E-posta Adresini Onayla', 'E-posta', 'tr', 'E-posta adresinizi onaylayin', 'E-posta Adresini Onayla', 'Views/Email/E-posta Adresini Onayla.cshtml', JSON_ARRAY('{{user_first_name}}','{{user_email}}','{{registration_date}}','{{verification_link}}'), 1),
('password_reset', 'Şifre Sıfırlama Talebi', 'E-posta', 'tr', 'Şifre sıfırlama talebiniz', 'Şifre Sıfırlama Talebi', 'Views/Email/Şifre Sıfırlama Talebi.cshtml', JSON_ARRAY('{{user_first_name}}','{{user_email}}','{{reset_link}}','{{request_ip}}'), 1),
('reservation_received_customer', 'Rezervasyon Talebi Alındı', 'E-posta', 'tr', 'Rezervasyon talebiniz alindi', 'Rezervasyon Talebi Alındı', 'Views/Email/Rezervasyon Talebi Alındı.cshtml', JSON_ARRAY('{{user_first_name}}','{{booking_reference}}','{{hotel_name}}','{{check_in_date}}','{{check_out_date}}','{{total_price}}'), 1),
('reservation_confirmed_customer', 'Rezervasyon Onaylandı', 'E-posta', 'tr', 'Rezervasyonunuz onaylandi', 'Rezervasyon Onaylandı', 'Views/Email/Rezervasyon Onaylandı.cshtml', JSON_ARRAY('{{user_first_name}}','{{booking_reference}}','{{hotel_name}}','{{check_in_date}}','{{check_out_date}}','{{total_price}}'), 1),
('reservation_new_partner', 'Partner Yeni Rezervasyon Bildirimi', 'E-posta', 'tr', 'Yeni rezervasyon onayi', 'Partner Yeni Rezervasyon', 'Views/Email/Partner Yeni Rezervasyon.cshtml', JSON_ARRAY('{{hotel_manager_name}}','{{hotel_name}}','{{booking_reference}}','{{guest_full_name}}','{{total_price}}'), 1)
ON DUPLICATE KEY UPDATE
sablon_adi = VALUES(sablon_adi),
konu = VALUES(konu),
baslik = VALUES(baslik),
icerik = VALUES(icerik),
degiskenler = VALUES(degiskenler),
aktif_mi = VALUES(aktif_mi);
