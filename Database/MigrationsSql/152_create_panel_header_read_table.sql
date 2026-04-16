CREATE TABLE IF NOT EXISTS panel_header_bildiri_okumalari (
    id BIGINT IDENTITY(1,1) NOT NULL,
    panel_kodu VARCHAR(24) NOT NULL,
    kullanici_id BIGINT  NOT NULL,
    bildiri_anahtari VARCHAR(190) NOT NULL,
    okundu_mi BIT NOT NULL DEFAULT 1,
    okundu_tarihi DATETIME2 NULL,
    olusturulma_tarihi DATETIME2 NOT NULL DEFAULT GETDATE(),
    guncellenme_tarihi DATETIME2 NOT NULL DEFAULT GETDATE(),
    PRIMARY KEY (id),
    UNIQUE KEY uq_panel_user_item (panel_kodu, kullanici_id, bildiri_anahtari),
    KEY idx_panel_user_unread (panel_kodu, kullanici_id, okundu_mi),
    CONSTRAINT fk_panel_header_read_user FOREIGN KEY (kullanici_id) REFERENCES users(id) ON DELETE CASCADE
);
