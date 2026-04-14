INSERT INTO oda_tipleri (
    otel_id, oda_tip_kodu, oda_adi, oda_kategorisi,
    maksimum_kisi_sayisi, maksimum_yetiskin_sayisi, maksimum_cocuk_sayisi,
    yatak_tipi, yatak_sayisi, oda_metrekare, manzara_tipi,
    standart_gecelik_fiyat, toplam_oda_sayisi
) VALUES
(1, 'STD-01', 'Standart Oda', 'Standart', 2, 2, 0, 'Çift Kişilik', 1, 28, 'Bahçe', 2500.00, 100),
(1, 'DLX-01', 'Deluxe Oda Deniz Manzaralı', 'Deluxe', 3, 2, 1, 'Queen Size', 1, 35, 'Deniz', 4500.00, 80),
(1, 'SUI-01', 'Junior Suite', 'Junior Suite', 4, 2, 2, 'King Size', 2, 55, 'Deniz', 7500.00, 40),
(1, 'FAM-01', 'Aile Odası', 'Aile Odası', 5, 2, 3, 'Queen Size', 2, 65, 'Havuz', 6000.00, 30);


