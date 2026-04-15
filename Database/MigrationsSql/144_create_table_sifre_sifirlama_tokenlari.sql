CREATE TABLE IF NOT EXISTS sifre_sifirlama_tokenlari (
    id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
    kullanici_id BIGINT UNSIGNED NOT NULL,
    eposta VARCHAR(100) NOT NULL,
    token VARCHAR(96) NOT NULL,
    ip_adresi VARCHAR(45) NULL,
    user_agent VARCHAR(500) NULL,
    kullanildi_mi TINYINT(1) NOT NULL DEFAULT 0,
    gecerlilik_suresi TIMESTAMP NOT NULL,
    kullanilma_tarihi TIMESTAMP NULL DEFAULT NULL,
    olusturulma_tarihi TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    UNIQUE KEY uk_password_reset_token (token),
    KEY idx_password_reset_user_status (kullanici_id, kullanildi_mi, gecerlilik_suresi),
    CONSTRAINT fk_password_reset_user FOREIGN KEY (kullanici_id) REFERENCES users (id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
