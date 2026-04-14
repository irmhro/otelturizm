CREATE TABLE otel_kosullari (
    id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT PRIMARY KEY,
    otel_id BIGINT UNSIGNED NOT NULL,
    
    -- Genel Kurallar
    sigara_politikasi ENUM('Tüm alanlarda yasak', 'Sadece belirli alanlarda serbest', 'Tamamen serbest') DEFAULT 'Sadece belirli alanlarda serbest',
    evcil_hayvan_politikasi ENUM('Kabul edilmez', 'Ücretsiz kabul edilir', 'Ücretli kabul edilir', 'Sadece küçük evcil hayvanlar') DEFAULT 'Kabul edilmez',
    evcil_hayvan_ucreti DECIMAL(10,2) NULL COMMENT 'Gecelik veya konaklama başına',
    evcil_hayvan_depozitosu DECIMAL(10,2) NULL,
    
    -- Parti / Etkinlik
    parti_etkinlik_izin TINYINT(1) DEFAULT 0,
    sessizlik_saatleri_baslangic TIME NULL,
    sessizlik_saatleri_bitis TIME NULL,
    
    -- Yaş Sınırlamaları
    minimum_yas_siniri TINYINT UNSIGNED NULL COMMENT 'Tek başına check-in yapabilecek minimum yaş',
    sadece_yetiskinlere_mi TINYINT(1) DEFAULT 0,
    
    -- Çocuk Politikası (JSON yerine ayrı tablo veya sütunlar)
    cocuk_kabul_yas_araligi VARCHAR(20) NULL COMMENT '0-2, 2-12 gibi',
    bebek_karyolasi_var_mi TINYINT(1) DEFAULT 0,
    bebek_karyolasi_ucreti DECIMAL(10,2) NULL,
    ekstra_yatak_var_mi TINYINT(1) DEFAULT 0,
    ekstra_yatak_ucreti DECIMAL(10,2) NULL,
    maksimum_cocuk_sayisi TINYINT UNSIGNED NULL,
    
    -- Rezervasyon Kuralları
    on_odeme_gerekli_mi TINYINT(1) DEFAULT 1,
    on_odeme_orani DECIMAL(5,2) DEFAULT 30.00 COMMENT 'Yüzde',
    kalan_odeme_zamani ENUM('Girişte', 'Çıkışta', 'Online') DEFAULT 'Girişte',
    kredi_karti_ile_odeme_kabul TINYINT(1) DEFAULT 1,
    nakit_odeme_kabul TINYINT(1) DEFAULT 0,
    
    -- Kabul Edilen Kart Tipleri
    kabul_edilen_kartlar SET('Visa', 'Mastercard', 'American Express', 'Troy', 'UnionPay') NULL,
    
    -- İptal ve Değişiklik Politikası
    iptal_politikasi_ozet VARCHAR(500) NULL,
    detayli_iptal_kosullari JSON NULL COMMENT '{"gun_kala": 7, "kesinti_orani": 0}, {"gun_kala": 3, "kesinti_orani": 50}, {"gun_kala": 0, "kesinti_orani": 100}',
    ucretsiz_iptal_suresi TINYINT UNSIGNED NULL COMMENT 'Kaç gün kala ücretsiz iptal',
    gec_iptal_ceza_orani DECIMAL(5,2) NULL,
    no_show_ceza_orani DECIMAL(5,2) DEFAULT 100.00,
    
    -- Hasar Depozitosu
    hasar_depozitosu_tutari DECIMAL(10,2) NULL,
    hasar_depozitosu_aciklamasi VARCHAR(255) NULL,
    
    -- Diğer
    disaridan_yiyecek_icecek_serbest_mi TINYINT(1) DEFAULT 1,
    ziyaretci_kabul_edilir_mi TINYINT(1) DEFAULT 0,
    ziyaretci_saati_baslangic TIME NULL,
    ziyaretci_saati_bitis TIME NULL,
    
    ozel_kosullar TEXT NULL,
    
    -- Zaman Damgası
    guncellenme_tarihi TIMESTAMP NULL ON UPDATE CURRENT_TIMESTAMP,
    
    UNIQUE KEY uk_otel_id (otel_id),
    INDEX idx_sigara (sigara_politikasi),
    INDEX idx_evcil_hayvan (evcil_hayvan_politikasi),
    INDEX idx_minimum_yas (minimum_yas_siniri),
    
    FOREIGN KEY (otel_id) REFERENCES oteller(id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Otellerin konaklama koşulları ve kuralları';

