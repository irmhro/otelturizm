CREATE TABLE rol_yetkileri (
    rol_id SMALLINT UNSIGNED NOT NULL,
    yetki_id INT UNSIGNED NOT NULL,
    izin_var TINYINT(1) DEFAULT 1 COMMENT '1: İzin var, 0: Özel olarak engellenmiş',
    atayan_kullanici_id BIGINT UNSIGNED NULL,
    atama_tarihi TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    
    PRIMARY KEY (rol_id, yetki_id),
    FOREIGN KEY (rol_id) REFERENCES roller(id) ON DELETE CASCADE,
    FOREIGN KEY (yetki_id) REFERENCES yetkiler(id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Rollere atanmış yetkiler';

