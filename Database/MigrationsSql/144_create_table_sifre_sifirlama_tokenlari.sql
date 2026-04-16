CREATE TABLE IF NOT EXISTS sifre_sifirlama_tokenlari (
    id BIGINT  NOT NULL IDENTITY(1,1),
    kullanici_id BIGINT  NOT NULL,
    eposta VARCHAR(100) NOT NULL,
    token VARCHAR(96) NOT NULL,
    ip_adresi VARCHAR(45) NULL,
    user_agent VARCHAR(500) NULL,
    kullanildi_mi BIT NOT NULL DEFAULT 0,
    gecerlilik_suresi DATETIME2 NOT NULL,
    kullanilma_tarihi DATETIME2 NULL DEFAULT NULL,
    olusturulma_tarihi DATETIME2 NOT NULL DEFAULT GETDATE(),
    PRIMARY KEY (id),
    UNIQUE KEY uk_password_reset_token (token),
    KEY idx_password_reset_user_status (kullanici_id, kullanildi_mi, gecerlilik_suresi),
    CONSTRAINT fk_password_reset_user FOREIGN KEY (kullanici_id) REFERENCES users (id) ON DELETE CASCADE
);
