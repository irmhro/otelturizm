CREATE TABLE IF NOT EXISTS panel_header_bildiri_okumalari (
    id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
    panel_kodu VARCHAR(24) NOT NULL,
    kullanici_id BIGINT UNSIGNED NOT NULL,
    bildiri_anahtari VARCHAR(190) NOT NULL,
    okundu_mi TINYINT(1) NOT NULL DEFAULT 1,
    okundu_tarihi DATETIME NULL,
    olusturulma_tarihi TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    guncellenme_tarihi TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    UNIQUE KEY uq_panel_user_item (panel_kodu, kullanici_id, bildiri_anahtari),
    KEY idx_panel_user_unread (panel_kodu, kullanici_id, okundu_mi),
    CONSTRAINT fk_panel_header_read_user FOREIGN KEY (kullanici_id) REFERENCES users(id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
