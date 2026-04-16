CREATE TABLE user_favori_oteller (
    id BIGINT  NOT NULL IDENTITY(1,1) PRIMARY KEY,
    user_id BIGINT  NOT NULL,
    otel_id BIGINT  NOT NULL,
    kaynak_sayfa VARCHAR(100) NULL,
    olusturulma_tarihi DATETIME2 NOT NULL DEFAULT GETDATE(),

    UNIQUE KEY uk_user_otel (user_id, otel_id),
    INDEX idx_user_id (user_id),
    INDEX idx_otel_id (otel_id),
    INDEX idx_created_at (olusturulma_tarihi),

    CONSTRAINT fk_user_favori_oteller_user
        FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE,
    CONSTRAINT fk_user_favori_oteller_otel
        FOREIGN KEY (otel_id) REFERENCES oteller(id) ON DELETE CASCADE
);
