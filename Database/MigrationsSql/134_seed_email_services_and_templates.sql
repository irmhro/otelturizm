MERGE email_services AS target
USING (
    SELECT
        'default_smtp' AS servis_kodu,
        'Varsayilan SMTP Servisi' AS servis_adi,
        'SMTP' AS saglayici,
        1 AS varsayilan_mi,
        1 AS aktif_mi,
        'Otelturizm' AS gonderen_ad,
        'no-reply@otelturizm.com' AS gonderen_eposta,
        'destek@otelturizm.com' AS yanitla_eposta,
        587 AS smtp_port,
        'TLS' AS guvenlik_tipi,
        1 AS test_modu,
        '{"note":"SMTP bilgileri sonra admin panelinden guncellenecek."}' AS metadata
) AS source
ON target.servis_kodu = source.servis_kodu
WHEN MATCHED THEN
    UPDATE SET
        servis_adi = source.servis_adi,
        aktif_mi = source.aktif_mi,
        varsayilan_mi = source.varsayilan_mi,
        gonderen_ad = source.gonderen_ad,
        gonderen_eposta = source.gonderen_eposta,
        yanitla_eposta = source.yanitla_eposta,
        smtp_port = source.smtp_port,
        guvenlik_tipi = source.guvenlik_tipi,
        test_modu = source.test_modu,
        metadata = source.metadata
WHEN NOT MATCHED THEN
    INSERT (servis_kodu, servis_adi, saglayici, varsayilan_mi, aktif_mi, gonderen_ad, gonderen_eposta, yanitla_eposta, smtp_port, guvenlik_tipi, test_modu, metadata)
    VALUES (source.servis_kodu, source.servis_adi, source.saglayici, source.varsayilan_mi, source.aktif_mi, source.gonderen_ad, source.gonderen_eposta, source.yanitla_eposta, source.smtp_port, source.guvenlik_tipi, source.test_modu, source.metadata);

MERGE bildirim_sablonlari AS target
USING (
    VALUES
    ('email_verify', 'E-posta Adresini Onayla', 'E-posta', 'tr', 'E-posta adresinizi onaylayin', 'E-posta Adresini Onayla', 'Views/Email/E-posta Adresini Onayla.cshtml', '["{{user_first_name}}","{{user_email}}","{{registration_date}}","{{verification_link}}"]', 1),
    ('password_reset', 'Şifre Sıfırlama Talebi', 'E-posta', 'tr', 'Şifre sıfırlama talebiniz', 'Şifre Sıfırlama Talebi', 'Views/Email/Şifre Sıfırlama Talebi.cshtml', '["{{user_first_name}}","{{user_email}}","{{reset_link}}","{{request_ip}}"]', 1),
    ('reservation_received_customer', 'Rezervasyon Talebi Alındı', 'E-posta', 'tr', 'Rezervasyon talebiniz alindi', 'Rezervasyon Talebi Alındı', 'Views/Email/Rezervasyon Talebi Alındı.cshtml', '["{{user_first_name}}","{{booking_reference}}","{{hotel_name}}","{{check_in_date}}","{{check_out_date}}","{{total_price}}"]', 1),
    ('reservation_confirmed_customer', 'Rezervasyon Onaylandı', 'E-posta', 'tr', 'Rezervasyonunuz onaylandi', 'Rezervasyon Onaylandı', 'Views/Email/Rezervasyon Onaylandı.cshtml', '["{{user_first_name}}","{{booking_reference}}","{{hotel_name}}","{{check_in_date}}","{{check_out_date}}","{{total_price}}"]', 1),
    ('reservation_new_partner', 'Partner Yeni Rezervasyon Bildirimi', 'E-posta', 'tr', 'Yeni rezervasyon onayi', 'Partner Yeni Rezervasyon', 'Views/Email/Partner Yeni Rezervasyon.cshtml', '["{{hotel_manager_name}}","{{hotel_name}}","{{booking_reference}}","{{guest_full_name}}","{{total_price}}"]', 1)
) AS source (sablon_kodu, sablon_adi, tur, dil, konu, baslik, icerik, degiskenler, aktif_mi)
ON target.sablon_kodu = source.sablon_kodu
WHEN MATCHED THEN
    UPDATE SET
        sablon_adi = source.sablon_adi,
        konu = source.konu,
        baslik = source.baslik,
        icerik = source.icerik,
        degiskenler = source.degiskenler,
        aktif_mi = source.aktif_mi
WHEN NOT MATCHED THEN
    INSERT (sablon_kodu, sablon_adi, tur, dil, konu, baslik, icerik, degiskenler, aktif_mi)
    VALUES (source.sablon_kodu, source.sablon_adi, source.tur, source.dil, source.konu, source.baslik, source.icerik, source.degiskenler, source.aktif_mi);
