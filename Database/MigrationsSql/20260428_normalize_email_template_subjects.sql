UPDATE dbo.bildirim_sablonlari
SET konu = N'Kurumsal rezervasyon kaydınız oluşturuldu'
WHERE sablon_kodu = N'firma_reservation_created_company';

UPDATE dbo.bildirim_sablonlari
SET konu = N'Yeni kurumsal rezervasyon kaydı'
WHERE sablon_kodu = N'firma_reservation_created_partner';

UPDATE dbo.bildirim_sablonlari
SET sablon_adi = N'Sistem Sağlığı Link Kontrol Raporu',
    konu = N'Sistem Sağlığı: Broken link raporu'
WHERE sablon_kodu = N'system_health_link_report';
