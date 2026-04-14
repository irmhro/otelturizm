CREATE TABLE admin_islem_loglari (
    id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
    admin_kullanici_id BIGINT UNSIGNED NOT NULL,
    
    islem_turu ENUM(
        'Oluşturma', 'Güncelleme', 'Silme', 'Onaylama', 'Reddetme',
        'Yayına Alma', 'Yayından Kaldırma', 'Askıya Alma',
        'Rol Atama', 'Yetki Değiştirme',
        'Fiyat Güncelleme', 'Komisyon Değiştirme',
        'Ödeme Onaylama', 'İade Onaylama', 'İade Reddetme',
        'Yorum Onaylama', 'Yorum Silme', 'Yorum Düzenleme',
        'Kullanıcı Banlama', 'Kullanıcı Ban Kaldırma',
        'Sistem Ayarı Değiştirme', 'Bakım Modu Açma/Kapama',
        'Rapor İndirme', 'Veri Dışa Aktarma',
        'Toplu İşlem'
    ) NOT NULL,
    
    -- Etkilenen Kayıt
    hedef_tablo VARCHAR(50) NOT NULL,
    hedef_kayit_id BIGINT UNSIGNED NULL,
    
    -- Değişiklik Detayı
    onceki_deger JSON NULL,
    yeni_deger JSON NULL,
    degisiklik_ozeti TEXT NULL,
    
    -- Açıklama
    islem_nedeni VARCHAR(500) NULL,
    islem_notu TEXT NULL,
    
    -- IP ve Konum
    ip_adresi VARCHAR(45) NOT NULL,
    
    -- Zaman
    islem_tarihi TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    
    PRIMARY KEY (id),
    INDEX idx_admin_id (admin_kullanici_id),
    INDEX idx_islem_turu (islem_turu),
    INDEX idx_hedef (hedef_tablo, hedef_kayit_id),
    INDEX idx_islem_tarihi (islem_tarihi DESC),
    INDEX idx_ip (ip_adresi),
    
    FOREIGN KEY (admin_kullanici_id) REFERENCES kullanicilar(id) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Admin panelinde yapılan tüm işlemlerin kaydı - Aylık partition';


