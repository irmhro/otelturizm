INSERT INTO email_services
(
    servis_kodu,
    servis_adi,
    saglayici,
    varsayilan_mi,
    aktif_mi,
    gonderen_ad,
    gonderen_eposta,
    yanitla_eposta,
    smtp_host,
    smtp_port,
    smtp_kullanici_adi,
    smtp_sifre,
    sifre_sifrelenmis_mi,
    guvenlik_tipi,
    test_modu,
    metadata
)
VALUES
(
    'default_smtp',
    'Otelturizm Canli SMTP',
    'SMTP',
    1,
    1,
    'Otelturizm',
    'info@otelturizm.com',
    'info@otelturizm.com',
    'mail.otelturizm.com',
    465,
    'info@otelturizm.com',
    'cYUJ*6yozW$gFm)G',
    0,
    'SSL',
    0,
    JSON_OBJECT(
        'imap_host', 'mail.otelturizm.com',
        'imap_port', 993,
        'pop3_host', 'mail.otelturizm.com',
        'pop3_port', 995,
        'smtp_host', 'mail.otelturizm.com',
        'smtp_port', 465,
        'username', 'info@otelturizm.com'
    )
)
ON DUPLICATE KEY UPDATE
    servis_adi = VALUES(servis_adi),
    saglayici = VALUES(saglayici),
    varsayilan_mi = VALUES(varsayilan_mi),
    aktif_mi = VALUES(aktif_mi),
    gonderen_ad = VALUES(gonderen_ad),
    gonderen_eposta = VALUES(gonderen_eposta),
    yanitla_eposta = VALUES(yanitla_eposta),
    smtp_host = VALUES(smtp_host),
    smtp_port = VALUES(smtp_port),
    smtp_kullanici_adi = VALUES(smtp_kullanici_adi),
    smtp_sifre = VALUES(smtp_sifre),
    sifre_sifrelenmis_mi = VALUES(sifre_sifrelenmis_mi),
    guvenlik_tipi = VALUES(guvenlik_tipi),
    test_modu = VALUES(test_modu),
    metadata = VALUES(metadata);
