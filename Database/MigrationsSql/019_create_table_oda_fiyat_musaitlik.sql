CREATE TABLE oda_fiyat_musaitlik (
    id BIGINT  NOT NULL IDENTITY(1,1),
    oda_tip_id BIGINT  NOT NULL,
    
    tarih DATE NOT NULL,
    
    -- Fiyatlandırma
    gecelik_fiyat DECIMAL(10,2) NOT NULL,
    indirimli_fiyat DECIMAL(10,2) NULL,
    kampanya_id BIGINT  NULL,
    
    -- Müsaitlik
    toplam_oda_sayisi SMALLINT  NOT NULL,
    satilan_oda_sayisi SMALLINT  DEFAULT 0,
    bloke_oda_sayisi SMALLINT  DEFAULT 0,
    minimum_geceleme TINYINT  DEFAULT 1,
    maksimum_geceleme SMALLINT  DEFAULT 30,
    
    -- Kısıtlamalar
    kapali_satis BIT DEFAULT 0,
    sadece_gunubirlik BIT DEFAULT 0,
    
    -- Kurallar
    iptal_politikasi_override JSON NULL,
    
    guncellenme_tarihi DATETIME2 DEFAULT GETDATE(),
    
    PRIMARY KEY (id),
    UNIQUE KEY uk_oda_tip_tarih (oda_tip_id, tarih),
    INDEX idx_tarih (tarih),
    INDEX idx_kampanya (kampanya_id),
    INDEX idx_musaitlik (oda_tip_id, tarih, toplam_oda_sayisi, satilan_oda_sayisi),
    
    FOREIGN KEY (oda_tip_id) REFERENCES oda_tipleri(id) ON DELETE CASCADE
);

