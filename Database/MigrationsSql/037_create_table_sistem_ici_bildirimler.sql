CREATE TABLE sistem_ici_bildirimler (
    id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT PRIMARY KEY,
    kullanici_id BIGINT UNSIGNED NOT NULL,
    
    bildirim_turu ENUM('Rezervasyon', 'Mesaj', 'Ödeme', 'Kampanya', 'Sistem', 'Hatırlatma', 'Uyarı', 'Başarı') NOT NULL,
    
    baslik VARCHAR(100) NOT NULL,
    mesaj TEXT NOT NULL,
    
    ikon VARCHAR(50) NULL,
    renk VARCHAR(20) NULL DEFAULT '#007bff',
    
    -- Aksiyon Butonu
    aksiyon_url VARCHAR(500) NULL,
    aksiyon_metni VARCHAR(50) NULL,
    
    -- Okunma
    okundu_mu TINYINT(1) DEFAULT 0,
    okunma_tarihi TIMESTAMP NULL,
    arsivlendi_mi TINYINT(1) DEFAULT 0,
    
    -- Önem
    onem_derecesi ENUM('Düşük', 'Normal', 'Yüksek', 'Kritik') DEFAULT 'Normal',
    
    -- İlişki
    ilgili_tablo VARCHAR(50) NULL,
    ilgili_kayit_id BIGINT UNSIGNED NULL,
    
    -- Geçerlilik
    gecerlilik_baslangic TIMESTAMP NULL,
    gecerlilik_bitis TIMESTAMP NULL,
    
    olusturulma_tarihi TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    
    INDEX idx_kullanici_id (kullanici_id),
    INDEX idx_okundu (kullanici_id, okundu_mu),
    INDEX idx_tur (bildirim_turu),
    INDEX idx_olusturulma (olusturulma_tarihi DESC),
    INDEX idx_onem (onem_derecesi),
    INDEX idx_ilgili (ilgili_tablo, ilgili_kayit_id),
    
    FOREIGN KEY (kullanici_id) REFERENCES kullanicilar(id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Kullanıcıların sistem içi bildirimleri';

