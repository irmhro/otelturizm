CREATE TABLE sistem_ici_bildirimler (
    id BIGINT  NOT NULL IDENTITY(1,1) PRIMARY KEY,
    kullanici_id BIGINT  NOT NULL,
    
    bildirim_turu ENUM('Rezervasyon', 'Mesaj', 'Ödeme', 'Kampanya', 'Sistem', 'Hatırlatma', 'Uyarı', 'Başarı') NOT NULL,
    
    baslik VARCHAR(100) NOT NULL,
    mesaj NVARCHAR(MAX) NOT NULL,
    
    ikon VARCHAR(50) NULL,
    renk VARCHAR(20) NULL DEFAULT '#007bff',
    
    -- Aksiyon Butonu
    aksiyon_url VARCHAR(500) NULL,
    aksiyon_metni VARCHAR(50) NULL,
    
    -- Okunma
    okundu_mu BIT DEFAULT 0,
    okunma_tarihi DATETIME2 NULL,
    arsivlendi_mi BIT DEFAULT 0,
    
    -- Önem
    onem_derecesi ENUM('Düşük', 'Normal', 'Yüksek', 'Kritik') DEFAULT 'Normal',
    
    -- İlişki
    ilgili_tablo VARCHAR(50) NULL,
    ilgili_kayit_id BIGINT  NULL,
    
    -- Geçerlilik
    gecerlilik_baslangic DATETIME2 NULL,
    gecerlilik_bitis DATETIME2 NULL,
    
    olusturulma_tarihi DATETIME2 DEFAULT GETDATE(),
    
    INDEX idx_kullanici_id (kullanici_id),
    INDEX idx_okundu (kullanici_id, okundu_mu),
    INDEX idx_tur (bildirim_turu),
    INDEX idx_olusturulma (olusturulma_tarihi DESC),
    INDEX idx_onem (onem_derecesi),
    INDEX idx_ilgili (ilgili_tablo, ilgili_kayit_id),
    
    FOREIGN KEY (kullanici_id) REFERENCES kullanicilar(id) ON DELETE CASCADE
);

