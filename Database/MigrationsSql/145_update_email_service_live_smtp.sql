MERGE email_services AS target
USING (
    SELECT
        'default_smtp' AS servis_kodu,
        'Otelturizm Canli SMTP' AS servis_adi,
        'SMTP' AS saglayici,
        1 AS varsayilan_mi,
        1 AS aktif_mi,
        'Otelturizm' AS gonderen_ad,
        'info@otelturizm.com' AS gonderen_eposta,
        'info@otelturizm.com' AS yanitla_eposta,
        'mail.otelturizm.com' AS smtp_host,
        465 AS smtp_port,
        'info@otelturizm.com' AS smtp_kullanici_adi,
        'cYUJ*6yozW$gFm)G' AS smtp_sifre,
        0 AS sifre_sifrelenmis_mi,
        'SSL' AS guvenlik_tipi,
        0 AS test_modu,
        '{"imap_host":"mail.otelturizm.com","imap_port":993,"pop3_host":"mail.otelturizm.com","pop3_port":995,"smtp_host":"mail.otelturizm.com","smtp_port":465,"username":"info@otelturizm.com"}' AS metadata
) AS source
ON target.servis_kodu = source.servis_kodu
WHEN MATCHED THEN
    UPDATE SET
        servis_adi = source.servis_adi,
        saglayici = source.saglayici,
        varsayilan_mi = source.varsayilan_mi,
        aktif_mi = source.aktif_mi,
        gonderen_ad = source.gonderen_ad,
        gonderen_eposta = source.gonderen_eposta,
        yanitla_eposta = source.yanitla_eposta,
        smtp_host = source.smtp_host,
        smtp_port = source.smtp_port,
        smtp_kullanici_adi = source.smtp_kullanici_adi,
        smtp_sifre = source.smtp_sifre,
        sifre_sifrelenmis_mi = source.sifre_sifrelenmis_mi,
        guvenlik_tipi = source.guvenlik_tipi,
        test_modu = source.test_modu,
        metadata = source.metadata
WHEN NOT MATCHED THEN
    INSERT (
        servis_kodu, servis_adi, saglayici, varsayilan_mi, aktif_mi, gonderen_ad, gonderen_eposta, yanitla_eposta,
        smtp_host, smtp_port, smtp_kullanici_adi, smtp_sifre, sifre_sifrelenmis_mi, guvenlik_tipi, test_modu, metadata
    )
    VALUES (
        source.servis_kodu, source.servis_adi, source.saglayici, source.varsayilan_mi, source.aktif_mi, source.gonderen_ad, source.gonderen_eposta, source.yanitla_eposta,
        source.smtp_host, source.smtp_port, source.smtp_kullanici_adi, source.smtp_sifre, source.sifre_sifrelenmis_mi, source.guvenlik_tipi, source.test_modu, source.metadata
    );
