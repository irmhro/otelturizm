CREATE TABLE otel_gorselleri (
    id BIGINT  NOT NULL IDENTITY(1,1) PRIMARY KEY,
    otel_id BIGINT  NOT NULL,
    
    gorsel_url VARCHAR(500) NOT NULL,
    thumbnail_url VARCHAR(500) NULL,
    gorsel_turu ENUM('Dış Cephe', 'Lobi', 'Restoran', 'Havuz', 'Plaj', 'Oda', 'Banyo', 'Spor Salonu', 'SPA', 'Toplantı Odası', 'Genel Alan', 'Yemek', 'Manzara') NOT NULL,
    
    baslik VARCHAR(200) NULL,
    aciklama NVARCHAR(MAX) NULL,
    
    kapak_fotografi_mi BIT DEFAULT 0,
    one_cikan BIT DEFAULT 0,
    siralama SMALLINT  DEFAULT 0,
    
    boyut_kb INT  NULL,
    genislik SMALLINT  NULL,
    yukseklik SMALLINT  NULL,
    
    onay_durumu ENUM('Beklemede', 'Onaylandı', 'Reddedildi') DEFAULT 'Beklemede',
    onaylayan_admin_id BIGINT  NULL,
    onay_tarihi DATETIME2 NULL,
    
    yukleyen_kullanici_id BIGINT  NULL,
    olusturulma_tarihi DATETIME2 DEFAULT GETDATE(),
    
    INDEX idx_otel_id (otel_id),
    INDEX idx_gorsel_turu (gorsel_turu),
    INDEX idx_kapak (otel_id, kapak_fotografi_mi),
    INDEX idx_onay (onay_durumu),
    
    FOREIGN KEY (otel_id) REFERENCES oteller(id) ON DELETE CASCADE,
    FOREIGN KEY (onaylayan_admin_id) REFERENCES kullanicilar(id) ON DELETE SET NULL,
    FOREIGN KEY (yukleyen_kullanici_id) REFERENCES kullanicilar(id) ON DELETE SET NULL
);

