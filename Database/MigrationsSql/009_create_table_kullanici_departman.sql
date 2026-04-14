CREATE TABLE kullanici_departman (
    kullanici_id BIGINT UNSIGNED NOT NULL,
    departman_id SMALLINT UNSIGNED NOT NULL,
    unvan VARCHAR(100) NULL COMMENT 'Kıdemli Yazılım Uzmanı vb.',
    ise_baslama_tarihi DATE NULL,
    yonetici_mi TINYINT(1) DEFAULT 0,
    
    PRIMARY KEY (kullanici_id, departman_id),
    FOREIGN KEY (kullanici_id) REFERENCES kullanicilar(id) ON DELETE CASCADE,
    FOREIGN KEY (departman_id) REFERENCES departmanlar(id) ON DELETE CASCADE,
    
    INDEX idx_departman (departman_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Şirket çalışanlarının departman bilgileri';

