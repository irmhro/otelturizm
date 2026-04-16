ALTER TABLE kullanici_oturum_istatistikleri
    MODIFY COLUMN hesap_tipi ENUM('user','partner','admin','firma','sales')
    COLLATE utf8mb4_unicode_ci
    NOT NULL;
