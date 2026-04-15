CREATE TABLE IF NOT EXISTS sadakat_seviyeleri (
    id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
    kod VARCHAR(40) NOT NULL,
    ad VARCHAR(100) NOT NULL,
    minimum_puan INT NOT NULL DEFAULT 0,
    maximum_puan INT NULL,
    renk_kodu VARCHAR(20) NULL,
    ikon VARCHAR(120) NULL,
    avantajlar_metin TEXT NULL,
    sira_no INT NOT NULL DEFAULT 0,
    aktif_mi TINYINT(1) NOT NULL DEFAULT 1,
    olusturulma_tarihi TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    guncellenme_tarihi TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    UNIQUE KEY uq_sadakat_seviyeleri_kod (kod),
    KEY idx_sadakat_seviyeleri_aktif_sira (aktif_mi, sira_no)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS kullanici_sadakat_hesaplari (
    id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
    kullanici_id BIGINT UNSIGNED NOT NULL,
    toplam_puan INT NOT NULL DEFAULT 0,
    kullanilabilir_puan INT NOT NULL DEFAULT 0,
    bu_yil_kazanilan_puan INT NOT NULL DEFAULT 0,
    bu_yil_kullanilan_puan INT NOT NULL DEFAULT 0,
    mevcut_seviye_id BIGINT UNSIGNED NULL,
    sonraki_seviye_id BIGINT UNSIGNED NULL,
    son_seviye_guncelleme_tarihi DATETIME NULL,
    puan_gecerlilik_tarihi DATE NULL,
    olusturulma_tarihi TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    guncellenme_tarihi TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    UNIQUE KEY uq_kullanici_sadakat_hesaplari_kullanici (kullanici_id),
    KEY idx_kullanici_sadakat_seviye (mevcut_seviye_id),
    CONSTRAINT fk_kullanici_sadakat_hesaplari_user FOREIGN KEY (kullanici_id) REFERENCES users(id) ON DELETE CASCADE,
    CONSTRAINT fk_kullanici_sadakat_hesaplari_mevcut_seviye FOREIGN KEY (mevcut_seviye_id) REFERENCES sadakat_seviyeleri(id) ON DELETE SET NULL,
    CONSTRAINT fk_kullanici_sadakat_hesaplari_sonraki_seviye FOREIGN KEY (sonraki_seviye_id) REFERENCES sadakat_seviyeleri(id) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS kullanici_puan_hareketleri (
    id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
    kullanici_id BIGINT UNSIGNED NOT NULL,
    sadakat_hesap_id BIGINT UNSIGNED NULL,
    rezervasyon_id BIGINT UNSIGNED NULL,
    hareket_tipi VARCHAR(60) NOT NULL,
    baslik VARCHAR(180) NOT NULL,
    aciklama VARCHAR(500) NULL,
    puan_degisim INT NOT NULL,
    puan_bakiye_sonrasi INT NULL,
    durum VARCHAR(30) NOT NULL DEFAULT 'Tamamlandi',
    islem_tarihi DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    gecerlilik_tarihi DATETIME NULL,
    olusturulma_tarihi TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    KEY idx_kullanici_puan_hareketleri_user_date (kullanici_id, islem_tarihi DESC),
    CONSTRAINT fk_kullanici_puan_hareketleri_user FOREIGN KEY (kullanici_id) REFERENCES users(id) ON DELETE CASCADE,
    CONSTRAINT fk_kullanici_puan_hareketleri_hesap FOREIGN KEY (sadakat_hesap_id) REFERENCES kullanici_sadakat_hesaplari(id) ON DELETE SET NULL,
    CONSTRAINT fk_kullanici_puan_hareketleri_rez FOREIGN KEY (rezervasyon_id) REFERENCES rezervasyonlar(id) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS rozet_tanimlari (
    id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
    kod VARCHAR(60) NOT NULL,
    ad VARCHAR(120) NOT NULL,
    aciklama VARCHAR(255) NULL,
    ikon VARCHAR(120) NULL,
    kategori VARCHAR(80) NULL,
    rozet_rengi VARCHAR(20) NULL,
    hedef_deger INT NOT NULL DEFAULT 1,
    siralama INT NOT NULL DEFAULT 0,
    aktif_mi TINYINT(1) NOT NULL DEFAULT 1,
    olusturulma_tarihi TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    UNIQUE KEY uq_rozet_tanimlari_kod (kod)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS kullanici_rozetleri (
    id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
    kullanici_id BIGINT UNSIGNED NOT NULL,
    rozet_id BIGINT UNSIGNED NOT NULL,
    durum VARCHAR(30) NOT NULL DEFAULT 'Kilitli',
    ilerleme_degeri INT NOT NULL DEFAULT 0,
    kazanilma_tarihi DATETIME NULL,
    olusturulma_tarihi TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    guncellenme_tarihi TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    UNIQUE KEY uq_kullanici_rozetleri_user_rozet (kullanici_id, rozet_id),
    CONSTRAINT fk_kullanici_rozetleri_user FOREIGN KEY (kullanici_id) REFERENCES users(id) ON DELETE CASCADE,
    CONSTRAINT fk_kullanici_rozetleri_rozet FOREIGN KEY (rozet_id) REFERENCES rozet_tanimlari(id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS kullanici_dijital_pasaportlari (
    id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
    kullanici_id BIGINT UNSIGNED NOT NULL,
    sehir VARCHAR(120) NOT NULL,
    ulke VARCHAR(120) NULL,
    ilk_konaklama_tarihi DATE NULL,
    son_konaklama_tarihi DATE NULL,
    toplam_konaklama_sayisi INT NOT NULL DEFAULT 0,
    damga_kodu VARCHAR(80) NULL,
    olusturulma_tarihi TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    guncellenme_tarihi TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    UNIQUE KEY uq_kullanici_pasaport_sehir (kullanici_id, sehir),
    CONSTRAINT fk_kullanici_pasaport_user FOREIGN KEY (kullanici_id) REFERENCES users(id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS kullanici_seyahat_planlari (
    id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
    olusturan_kullanici_id BIGINT UNSIGNED NOT NULL,
    plan_kodu VARCHAR(80) NOT NULL,
    plan_adi VARCHAR(180) NOT NULL,
    hedef_sehir VARCHAR(120) NOT NULL,
    baslangic_tarihi DATE NULL,
    bitis_tarihi DATE NULL,
    butce_tutari DECIMAL(12,2) NULL,
    para_birimi VARCHAR(10) NOT NULL DEFAULT 'TRY',
    davet_kodu VARCHAR(40) NULL,
    durum VARCHAR(30) NOT NULL DEFAULT 'Taslak',
    olusturulma_tarihi TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    guncellenme_tarihi TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    UNIQUE KEY uq_kullanici_seyahat_planlari_plan_kodu (plan_kodu),
    KEY idx_kullanici_seyahat_planlari_user (olusturan_kullanici_id, durum),
    CONSTRAINT fk_kullanici_seyahat_planlari_user FOREIGN KEY (olusturan_kullanici_id) REFERENCES users(id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS kullanici_seyahat_plan_otel_secimleri (
    id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
    plan_id BIGINT UNSIGNED NOT NULL,
    otel_id BIGINT UNSIGNED NOT NULL,
    ekleyen_kullanici_id BIGINT UNSIGNED NOT NULL,
    oy_puani INT NOT NULL DEFAULT 0,
    notlar VARCHAR(255) NULL,
    olusturulma_tarihi TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    UNIQUE KEY uq_seyahat_plan_otel (plan_id, otel_id),
    CONSTRAINT fk_seyahat_plan_otel_plan FOREIGN KEY (plan_id) REFERENCES kullanici_seyahat_planlari(id) ON DELETE CASCADE,
    CONSTRAINT fk_seyahat_plan_otel_otel FOREIGN KEY (otel_id) REFERENCES oteller(id) ON DELETE CASCADE,
    CONSTRAINT fk_seyahat_plan_otel_user FOREIGN KEY (ekleyen_kullanici_id) REFERENCES users(id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS kullanici_ozel_teklifleri (
    id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
    kullanici_id BIGINT UNSIGNED NULL,
    teklif_tipi VARCHAR(60) NOT NULL DEFAULT 'Kampanya',
    baslik VARCHAR(180) NOT NULL,
    aciklama VARCHAR(500) NULL,
    kampanya_kodu VARCHAR(80) NOT NULL,
    indirim_orani DECIMAL(5,2) NULL,
    minimum_sepet_tutari DECIMAL(12,2) NULL,
    gecerlilik_baslangic DATE NOT NULL,
    gecerlilik_bitis DATE NOT NULL,
    buton_url VARCHAR(255) NULL,
    aktif_mi TINYINT(1) NOT NULL DEFAULT 1,
    olusturulma_tarihi TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    guncellenme_tarihi TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    KEY idx_kullanici_ozel_teklifleri_user_dates (kullanici_id, aktif_mi, gecerlilik_baslangic, gecerlilik_bitis),
    CONSTRAINT fk_kullanici_ozel_teklifleri_user FOREIGN KEY (kullanici_id) REFERENCES users(id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS kullanici_butce_planlari (
    id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
    kullanici_id BIGINT UNSIGNED NOT NULL,
    hedef_sehir VARCHAR(120) NOT NULL,
    hedef_butce DECIMAL(12,2) NOT NULL,
    gece_sayisi INT NOT NULL DEFAULT 1,
    kisi_sayisi INT NOT NULL DEFAULT 1,
    para_birimi VARCHAR(10) NOT NULL DEFAULT 'TRY',
    notlar VARCHAR(255) NULL,
    olusturulma_tarihi TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    guncellenme_tarihi TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    KEY idx_kullanici_butce_planlari_user (kullanici_id, guncellenme_tarihi DESC),
    CONSTRAINT fk_kullanici_butce_planlari_user FOREIGN KEY (kullanici_id) REFERENCES users(id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS sadakat_odulleri (
    id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
    kod VARCHAR(60) NOT NULL,
    ad VARCHAR(120) NOT NULL,
    aciklama VARCHAR(255) NULL,
    gerekli_puan INT NOT NULL,
    ikon VARCHAR(120) NULL,
    ton VARCHAR(30) NULL,
    aktif_mi TINYINT(1) NOT NULL DEFAULT 1,
    olusturulma_tarihi TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    UNIQUE KEY uq_sadakat_odulleri_kod (kod)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

INSERT IGNORE INTO sadakat_seviyeleri (kod, ad, minimum_puan, maximum_puan, renk_kodu, ikon, avantajlar_metin, sira_no, aktif_mi)
VALUES
('BRONZE', 'Bronz', 0, 999, '#CD7F32', 'fas fa-star', 'Yuzde 5 indirim|Hos geldin puani', 1, 1),
('SILVER', 'Silver', 1000, 2499, '#C0C0C0', 'fas fa-medal', 'Yuzde 10 indirim|Erken check-in|Dogum gunu bonusu', 2, 1),
('GOLD', 'Gold', 2500, 4999, '#FFD700', 'fas fa-crown', 'Yuzde 15 indirim|Erken check-in|Hos geldin hediyesi|Oncelikli destek', 3, 1),
('PLATINUM', 'Platinum', 5000, NULL, '#E5E4E2', 'fas fa-gem', 'Yuzde 30 indirim|Lounge erisimi|Ucretsiz oda yukseltme|VIP transfer', 4, 1);

INSERT IGNORE INTO rozet_tanimlari (kod, ad, aciklama, ikon, kategori, rozet_rengi, hedef_deger, siralama, aktif_mi)
VALUES
('FIRST_BOOKING', 'Ilk Rezervasyon', 'Ilk konaklamanizi tamamlayin.', 'fas fa-check', 'Rezervasyon', '#22C55E', 1, 1, 1),
('FIVE_STAR', '5 Yildizli Deneyim', '5 yildizli bir otelde konaklayin.', 'fas fa-star', 'Deneyim', '#F59E0B', 1, 2, 1),
('BEACH_EXPLORER', 'Sahil Kasifi', 'Sahil otellerinden rezervasyon yapin.', 'fas fa-umbrella-beach', 'Tema', '#0EA5E9', 1, 3, 1),
('TEN_BOOKINGS', '10 Rezervasyon', '10 farkli rezervasyon yapin.', 'fas fa-crown', 'Rezervasyon', '#8B5CF6', 10, 4, 1);

INSERT IGNORE INTO sadakat_odulleri (kod, ad, aciklama, gerekli_puan, ikon, ton, aktif_mi)
VALUES
('DISCOUNT_250', 'Indirim Kuponu', '250 TL degerinde konaklama indirimi', 500, 'fas fa-ticket-alt', 'primary', 1),
('UPGRADE_1_NIGHT', 'Oda Yukseltme', '1 gecelik ucretsiz upgrade hakkı', 1000, 'fas fa-bed', 'warning', 1),
('FREE_BREAKFAST', 'Ucretsiz Kahvalti', '2 kisilik kahvalti paketi', 300, 'fas fa-utensils', 'success', 1),
('SPA_ACCESS', 'Spa Kullanimi', '1 saatlik spa kullanimi', 800, 'fas fa-spa', 'secondary', 1),
('AIRPORT_TRANSFER', 'Havalimani Transfer', 'Tek yon VIP transfer', 1500, 'fas fa-car', 'info', 1),
('PLATINUM_TRIAL', 'Platinum Deneme', '30 gun Platinum deneme paketi', 2000, 'fas fa-gem', 'platinum', 1);

INSERT IGNORE INTO kullanici_ozel_teklifleri (kullanici_id, teklif_tipi, baslik, aciklama, kampanya_kodu, indirim_orani, minimum_sepet_tutari, gecerlilik_baslangic, gecerlilik_bitis, buton_url, aktif_mi)
VALUES
(NULL, 'Kampanya', 'Istanbul City Break', 'Sehir otellerinde hafta ici indirim.', 'CITYWEEK15', 15.00, 2500.00, CURDATE(), DATE_ADD(CURDATE(), INTERVAL 45 DAY), '/oteller?q=istanbul', 1),
(NULL, 'Kampanya', 'Sahil Kasifi Firsati', 'Yaz rotalari icin sahil konseptli secili oteller.', 'SAHIL10', 10.00, 3000.00, CURDATE(), DATE_ADD(CURDATE(), INTERVAL 60 DAY), '/oteller?q=antalya', 1),
(NULL, 'Sadakat', 'Gold Uye Bonus Kodu', 'Gold seviyesine ulasan kullanicilar icin ek kupon.', 'GOLDPLUS', 12.50, 2000.00, CURDATE(), DATE_ADD(CURDATE(), INTERVAL 30 DAY), '/oteller', 1);
