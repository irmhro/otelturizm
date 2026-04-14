CREATE TABLE otel_ozellik_iliskileri (
    otel_id BIGINT UNSIGNED NOT NULL,
    ozellik_id INT UNSIGNED NOT NULL,
    ek_ucret DECIMAL(10,2) NULL COMMENT 'Bu özellik ücretliyse tutar',
    aciklama VARCHAR(255) NULL COMMENT 'Özel not (örn: Sadece yaz sezonu açık)',
    
    PRIMARY KEY (otel_id, ozellik_id),
    INDEX idx_otel_id (otel_id),
    INDEX idx_ozellik_id (ozellik_id),
    
    FOREIGN KEY (otel_id) REFERENCES oteller(id) ON DELETE CASCADE,
    FOREIGN KEY (ozellik_id) REFERENCES otel_ozellikleri(id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Otellerin sahip olduğu özellikler';

