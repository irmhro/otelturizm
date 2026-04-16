CREATE TABLE bildirim_sablonlari (
    id SMALLINT  NOT NULL IDENTITY(1,1) PRIMARY KEY,
    sablon_kodu VARCHAR(50) NOT NULL UNIQUE,
    sablon_adi VARCHAR(100) NOT NULL,
    
    tur ENUM('E-posta', 'SMS', 'Push Notification', 'Sistem İçi') NOT NULL,
    
    -- Çoklu Dil Desteği
    dil VARCHAR(5) NOT NULL DEFAULT 'tr',
    
    konu VARCHAR(200) NULL,
    baslik VARCHAR(100) NULL,
    icerik NVARCHAR(MAX) NOT NULL,
    
    -- Değişkenler
    degiskenler JSON NULL,
    
    aktif_mi BIT DEFAULT 1,
    olusturulma_tarihi DATETIME2 DEFAULT GETDATE(),
    
    UNIQUE KEY uk_kod_dil (sablon_kodu, dil),
    INDEX idx_tur (tur),
    INDEX idx_dil (dil),
    INDEX idx_aktif (aktif_mi)
);

