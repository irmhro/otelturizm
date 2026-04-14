ALTER TABLE users
    ADD COLUMN IF NOT EXISTS kvkk_onay_tarihi TIMESTAMP NULL AFTER telefon_dogrulama_tarihi,
    ADD COLUMN IF NOT EXISTS pazarlama_izni TINYINT(1) NOT NULL DEFAULT 0 AFTER kvkk_onay_tarihi,
    ADD COLUMN IF NOT EXISTS kayit_kaynagi VARCHAR(50) NULL AFTER pazarlama_izni;
