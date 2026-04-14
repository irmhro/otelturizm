CREATE TABLE mesaj_konusmalari (
    id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT PRIMARY KEY,
    konusma_kodu VARCHAR(20) NOT NULL UNIQUE COMMENT 'MSG-XXXXXXXX',
    
    -- İlişkiler
    rezervasyon_id BIGINT UNSIGNED NULL COMMENT 'Rezervasyon ile ilgiliyse',
    otel_id BIGINT UNSIGNED NOT NULL,
    
    -- Katılımcılar
    misafir_kullanici_id BIGINT UNSIGNED NOT NULL,
    otel_yetkilisi_kullanici_id BIGINT UNSIGNED NULL COMMENT 'Otel adına yanıt veren kişi',
    
    -- Konuşma Detayları
    konu_basligi VARCHAR(200) NOT NULL,
    konu_kategorisi ENUM('Rezervasyon', 'Özel İstek', 'İptal/Değişiklik', 'Ödeme', 'Giriş/Çıkış', 'Oda', 'Ulaşım', 'Diğer') DEFAULT 'Diğer',
    
    durum ENUM('Açık', 'Kapalı', 'Çözüldü', 'Spam', 'Arşivlendi') DEFAULT 'Açık',
    oncelik ENUM('Düşük', 'Normal', 'Yüksek', 'Acil') DEFAULT 'Normal',
    
    son_mesaj_tarihi TIMESTAMP NULL,
    son_mesaj_gonderen ENUM('Misafir', 'Otel', 'Sistem') NULL,
    son_mesaj_onizleme VARCHAR(100) NULL,
    
    -- Okunma Durumları
    misafir_okunmamis_sayisi INT UNSIGNED DEFAULT 0,
    otel_okunmamis_sayisi INT UNSIGNED DEFAULT 0,
    misafir_son_okuma_tarihi TIMESTAMP NULL,
    otel_son_okuma_tarihi TIMESTAMP NULL,
    
    -- Ek Bilgiler
    etiketler JSON NULL COMMENT '["önemli", "bekleyen"]',
    atanan_destek_ekibi_kullanici_id BIGINT UNSIGNED NULL COMMENT 'Platform desteği atandıysa',
    
    -- Zaman Damgaları
    olusturulma_tarihi TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    guncellenme_tarihi TIMESTAMP NULL ON UPDATE CURRENT_TIMESTAMP,
    kapatilma_tarihi TIMESTAMP NULL,
    kapatma_nedeni VARCHAR(255) NULL,
    
    -- İndeksler
    INDEX idx_konusma_kodu (konusma_kodu),
    INDEX idx_rezervasyon_id (rezervasyon_id),
    INDEX idx_otel_id (otel_id),
    INDEX idx_misafir_id (misafir_kullanici_id),
    INDEX idx_otel_yetkilisi (otel_yetkilisi_kullanici_id),
    INDEX idx_durum (durum),
    INDEX idx_oncelik (oncelik),
    INDEX idx_son_mesaj_tarihi (son_mesaj_tarihi DESC),
    INDEX idx_misafir_okunmamis (misafir_kullanici_id, misafir_okunmamis_sayisi),
    INDEX idx_otel_okunmamis (otel_id, otel_okunmamis_sayisi),
    
    FOREIGN KEY (rezervasyon_id) REFERENCES rezervasyonlar(id) ON DELETE SET NULL,
    FOREIGN KEY (otel_id) REFERENCES oteller(id) ON DELETE CASCADE,
    FOREIGN KEY (misafir_kullanici_id) REFERENCES kullanicilar(id) ON DELETE CASCADE,
    FOREIGN KEY (otel_yetkilisi_kullanici_id) REFERENCES kullanicilar(id) ON DELETE SET NULL,
    FOREIGN KEY (atanan_destek_ekibi_kullanici_id) REFERENCES kullanicilar(id) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Misafir-Otel arası mesajlaşma konuşmaları';

