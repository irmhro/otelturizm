CREATE TABLE oda_tipi_ozellikleri (
    oda_tip_id BIGINT UNSIGNED NOT NULL,
    ozellik_id SMALLINT UNSIGNED NOT NULL,
    miktar TINYINT UNSIGNED DEFAULT 1 COMMENT 'Örn: 2 adet TV varsa',
    
    PRIMARY KEY (oda_tip_id, ozellik_id),
    INDEX idx_oda_tip_id (oda_tip_id),
    INDEX idx_ozellik_id (ozellik_id),
    
    FOREIGN KEY (oda_tip_id) REFERENCES oda_tipleri(id) ON DELETE CASCADE,
    FOREIGN KEY (ozellik_id) REFERENCES oda_ozellikleri(id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Oda tiplerine ait özellikler';

