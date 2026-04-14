CREATE TABLE kullanici_rolleri (
    kullanici_id BIGINT UNSIGNED NOT NULL,
    rol_id SMALLINT UNSIGNED NOT NULL,
    atayan_kullanici_id BIGINT UNSIGNED NULL,
    atama_tarihi TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    bitis_tarihi TIMESTAMP NULL COMMENT 'Geçici rol atamaları için',
    
    PRIMARY KEY (kullanici_id, rol_id),
    FOREIGN KEY (kullanici_id) REFERENCES kullanicilar(id) ON DELETE CASCADE,
    FOREIGN KEY (rol_id) REFERENCES roller(id) ON DELETE CASCADE,
    FOREIGN KEY (atayan_kullanici_id) REFERENCES kullanicilar(id) ON DELETE SET NULL,
    
    INDEX idx_rol (rol_id),
    INDEX idx_bitis (bitis_tarihi)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Kullanıcıların sahip olduğu roller';

