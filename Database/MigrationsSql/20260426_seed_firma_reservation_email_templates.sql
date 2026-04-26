MERGE INTO bildirim_sablonlari AS target
USING (VALUES
    ('firma_reservation_created_company', 'Firma Rezervasyon Bildirimi (Firma)', 'E-posta', 'tr',
     'Kurumsal rezervasyon kaydınız oluşturuldu',
     'Firma Rezervasyon Bildirimi',
     'Views/Email/Firma Rezervasyon Bildirimi.cshtml',
     '["{{booking_reference}}","{{hotel_name}}","{{check_in_date}}","{{check_out_date}}","{{total_price}}","{{company_name}}"]',
     1),
    ('firma_reservation_created_partner', 'Firma Rezervasyon Bildirimi (Partner)', 'E-posta', 'tr',
     'Yeni kurumsal rezervasyon kaydı',
     'Firma Rezervasyon Bildirimi',
     'Views/Email/Firma Rezervasyon Bildirimi.cshtml',
     '["{{booking_reference}}","{{hotel_name}}","{{check_in_date}}","{{check_out_date}}","{{total_price}}","{{company_name}}","{{hotel_manager_name}}"]',
     1)
) AS source (sablon_kodu, sablon_adi, tur, dil, konu, baslik, icerik, degiskenler, aktif_mi)
ON target.sablon_kodu = source.sablon_kodu AND target.dil = source.dil
WHEN MATCHED THEN
    UPDATE SET sablon_adi = source.sablon_adi,
               tur = source.tur,
               konu = source.konu,
               baslik = source.baslik,
               icerik = source.icerik,
               degiskenler = source.degiskenler,
               aktif_mi = source.aktif_mi
WHEN NOT MATCHED THEN
    INSERT (sablon_kodu, sablon_adi, tur, dil, konu, baslik, icerik, degiskenler, aktif_mi)
    VALUES (source.sablon_kodu, source.sablon_adi, source.tur, source.dil, source.konu, source.baslik, source.icerik, source.degiskenler, source.aktif_mi);

