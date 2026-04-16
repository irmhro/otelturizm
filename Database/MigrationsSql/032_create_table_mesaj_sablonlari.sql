CREATE TABLE mesaj_sablonlari (
    id INT  NOT NULL IDENTITY(1,1) PRIMARY KEY,
    sablon_kodu VARCHAR(30) NOT NULL UNIQUE,
    sablon_adi VARCHAR(100) NOT NULL,
    
    -- Sahiplik
    otel_id BIGINT  NULL,
    sistem_geneli_mi BIT DEFAULT 0,
    
    -- Şablon İçeriği
    kategori ENUM('Hoş Geldin', 'Rezervasyon Onayı', 'Ödeme Hatırlatma', 'Giriş Bilgileri', 'Teşekkür', 'Özel Teklif', 'İptal', 'Diğer') NOT NULL,
    konu_basligi VARCHAR(200) NOT NULL,
    mesaj_icerigi NVARCHAR(MAX) NOT NULL,
    
    -- Değişkenler
    kullanilabilir_degiskenler JSON NULL,
    
    -- Dil
    dil VARCHAR(5) DEFAULT 'tr',
    
    aktif_mi BIT DEFAULT 1,
    olusturulma_tarihi DATETIME2 DEFAULT GETDATE(),
    
    INDEX idx_otel_id (otel_id),
    INDEX idx_kategori (kategori),
    INDEX idx_dil (dil),
    INDEX idx_aktif (aktif_mi),
    
    FOREIGN KEY (otel_id) REFERENCES oteller(id) ON DELETE CASCADE
);

