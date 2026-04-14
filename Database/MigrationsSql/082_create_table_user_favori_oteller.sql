CREATE TABLE user_favori_oteller (
    id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT PRIMARY KEY,
    user_id BIGINT UNSIGNED NOT NULL,
    otel_id BIGINT UNSIGNED NOT NULL,
    kaynak_sayfa VARCHAR(100) NULL COMMENT 'Anasayfa, Listeleme, Detay gibi favori kaynagi',
    olusturulma_tarihi TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,

    UNIQUE KEY uk_user_otel (user_id, otel_id),
    INDEX idx_user_id (user_id),
    INDEX idx_otel_id (otel_id),
    INDEX idx_created_at (olusturulma_tarihi),

    CONSTRAINT fk_user_favori_oteller_user
        FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE,
    CONSTRAINT fk_user_favori_oteller_otel
        FOREIGN KEY (otel_id) REFERENCES oteller(id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Kullanicilarin favorilerine ekledigi oteller';
