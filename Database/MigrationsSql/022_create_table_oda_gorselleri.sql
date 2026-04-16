CREATE TABLE oda_gorselleri (
    id BIGINT  NOT NULL IDENTITY(1,1) PRIMARY KEY,
    oda_tip_id BIGINT  NOT NULL,
    
    gorsel_url VARCHAR(500) NOT NULL,
    thumbnail_url VARCHAR(500) NULL,
    
    baslik VARCHAR(200) NULL,
    aciklama NVARCHAR(MAX) NULL,
    
    kapak_fotografi_mi BIT DEFAULT 0,
    siralama SMALLINT  DEFAULT 0,
    
    boyut_kb INT  NULL,
    
    onay_durumu ENUM('Beklemede', 'Onaylandı', 'Reddedildi') DEFAULT 'Beklemede',
    onaylayan_admin_id BIGINT  NULL,
    onay_tarihi DATETIME2 NULL,
    
    yukleyen_kullanici_id BIGINT  NULL,
    olusturulma_tarihi DATETIME2 DEFAULT GETDATE(),
    
    INDEX idx_oda_tip_id (oda_tip_id),
    INDEX idx_kapak (oda_tip_id, kapak_fotografi_mi),
    INDEX idx_onay (onay_durumu),
    
    FOREIGN KEY (oda_tip_id) REFERENCES oda_tipleri(id) ON DELETE CASCADE,
    FOREIGN KEY (onaylayan_admin_id) REFERENCES kullanicilar(id) ON DELETE SET NULL,
    FOREIGN KEY (yukleyen_kullanici_id) REFERENCES kullanicilar(id) ON DELETE SET NULL
);

