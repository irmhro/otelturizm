ALTER TABLE kullanici_oturum_istatistikleri
    MODIFY COLUMN hesap_tipi ENUM('user','partner','admin','firma','sales')
    CHARACTER SET utf8mb4
    COLLATE utf8mb4_unicode_ci
    NOT NULL;
