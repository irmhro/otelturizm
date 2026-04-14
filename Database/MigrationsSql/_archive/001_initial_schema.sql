-- Auto-generated initial schema from proje verileri/sq*.txt (cleaned)

-- Generated at: 2026-04-10 15:58:21

SET NAMES utf8mb4;

SET FOREIGN_KEY_CHECKS = 0;



CREATE TABLE roller (
    id SMALLINT UNSIGNED NOT NULL AUTO_INCREMENT PRIMARY KEY,
    rol_kodu VARCHAR(30) NOT NULL UNIQUE COMMENT 'Sistemde kullanılacak sabit kod',
    rol_adi VARCHAR(50) NOT NULL COMMENT 'Görünen ad',
    departman VARCHAR(50) NOT NULL COMMENT 'Yönetim, Operasyon, Finans, Satış, Destek, IT, Hukuk',
    seviye TINYINT UNSIGNED NOT NULL COMMENT '1-99 arası yetki seviyesi',
    ust_rol_id SMALLINT UNSIGNED NULL COMMENT 'Hiyerarşi için üst rol',
    varsayilan_mi TINYINT(1) DEFAULT 0 COMMENT 'Yeni kullanıcıya otomatik atanır mı?',
    aciklama VARCHAR(255),
    olusturulma_tarihi TIMESTAMP DEFAULT CURRENT_TIMESTAMP,

    INDEX idx_departman (departman),
    INDEX idx_seviye (seviye),
    INDEX idx_rol_kodu (rol_kodu),
    FOREIGN KEY (ust_rol_id) REFERENCES roller(id) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Kurumsal rol ve departman tanımları';

CREATE TABLE yetkiler (
    id INT UNSIGNED NOT NULL AUTO_INCREMENT PRIMARY KEY,
    yetki_kodu VARCHAR(100) NOT NULL UNIQUE COMMENT 'örn: otel.sil, rezervasyon.iptal, finans.rapor.gor',
    modul VARCHAR(50) NOT NULL COMMENT 'Otel, Rezervasyon, Finans, Kullanici, Rapor, Sistem',
    eylem VARCHAR(50) NOT NULL COMMENT 'listele, goruntule, ekle, duzenle, sil, onayla, reddet, indir',
    aciklama VARCHAR(255),
    varsayilan_izin TINYINT(1) DEFAULT 0 COMMENT 'Tüm roller için varsayılan izin',
    olusturulma_tarihi TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    
    INDEX idx_modul (modul),
    INDEX idx_eylem (eylem)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Sistemdeki tüm yetki tanımları';

CREATE TABLE rol_yetkileri (
    rol_id SMALLINT UNSIGNED NOT NULL,
    yetki_id INT UNSIGNED NOT NULL,
    izin_var TINYINT(1) DEFAULT 1 COMMENT '1: İzin var, 0: Özel olarak engellenmiş',
    atayan_kullanici_id BIGINT UNSIGNED NULL,
    atama_tarihi TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    
    PRIMARY KEY (rol_id, yetki_id),
    FOREIGN KEY (rol_id) REFERENCES roller(id) ON DELETE CASCADE,
    FOREIGN KEY (yetki_id) REFERENCES yetkiler(id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Rollere atanmış yetkiler';

CREATE TABLE kullanicilar (
    id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT PRIMARY KEY,
    ad_soyad VARCHAR(100) NOT NULL,
    eposta VARCHAR(100) NOT NULL UNIQUE,
    telefon VARCHAR(20) NULL UNIQUE,
    sifre VARCHAR(255) NOT NULL,
    profil_fotografi VARCHAR(255) NULL,
    email_dogrulama_tarihi TIMESTAMP NULL,
    telefon_dogrulama_tarihi TIMESTAMP NULL,
    son_giris_tarihi TIMESTAMP NULL,
    son_giris_ip VARCHAR(45) NULL,
    hesap_durumu TINYINT NOT NULL DEFAULT 1 COMMENT '1:Aktif, 0:Pasif, 2:Askıda, 3:Banlı',
    dil_tercihi VARCHAR(5) DEFAULT 'tr',
    para_birimi VARCHAR(3) DEFAULT 'TRY',
    ulke VARCHAR(50) DEFAULT 'Türkiye',
    olusturulma_tarihi TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    guncellenme_tarihi TIMESTAMP NULL ON UPDATE CURRENT_TIMESTAMP,
    
    INDEX idx_eposta (eposta),
    INDEX idx_telefon (telefon),
    INDEX idx_hesap_durumu (hesap_durumu)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Tüm kullanıcıların ana tablosu';

CREATE TABLE kullanici_rolleri (
    kullanici_id BIGINT UNSIGNED NOT NULL,
    rol_id SMALLINT UNSIGNED NOT NULL,
    atayan_kullanici_id BIGINT UNSIGNED NULL,
    atama_tarihi TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    bitis_tarihi TIMESTAMP NULL COMMENT 'Geçici rol atamaları için',
    
    PRIMARY KEY (kullanici_id, rol_id),
    FOREIGN KEY (kullanici_id) REFERENCES kullanicilar(id) ON DELETE CASCADE,
    FOREIGN KEY (rol_id) REFERENCES roller(id) ON DELETE CASCADE,
    FOREIGN KEY (atayan_kullanici_id) REFERENCES kullanicilar(id) ON DELETE SET NULL,
    
    INDEX idx_rol (rol_id),
    INDEX idx_bitis (bitis_tarihi)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Kullanıcıların sahip olduğu roller';

CREATE TABLE departmanlar (
    id SMALLINT UNSIGNED NOT NULL AUTO_INCREMENT PRIMARY KEY,
    departman_kodu VARCHAR(30) NOT NULL UNIQUE,
    departman_adi VARCHAR(50) NOT NULL,
    ust_departman_id SMALLINT UNSIGNED NULL,
    yonetici_rol_id SMALLINT UNSIGNED NULL COMMENT 'Bu departmanın varsayılan yönetici rolü',
    bina_kat VARCHAR(20) NULL,
    dahili_telefon VARCHAR(10) NULL,
    aciklama VARCHAR(255),
    aktif_mi TINYINT(1) DEFAULT 1,
    olusturulma_tarihi TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    
    FOREIGN KEY (ust_departman_id) REFERENCES departmanlar(id) ON DELETE SET NULL,
    FOREIGN KEY (yonetici_rol_id) REFERENCES roller(id) ON DELETE SET NULL,
    
    INDEX idx_departman_kodu (departman_kodu),
    INDEX idx_aktif (aktif_mi)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Şirket departman hiyerarşisi';

CREATE TABLE kullanici_departman (
    kullanici_id BIGINT UNSIGNED NOT NULL,
    departman_id SMALLINT UNSIGNED NOT NULL,
    unvan VARCHAR(100) NULL COMMENT 'Kıdemli Yazılım Uzmanı vb.',
    ise_baslama_tarihi DATE NULL,
    yonetici_mi TINYINT(1) DEFAULT 0,
    
    PRIMARY KEY (kullanici_id, departman_id),
    FOREIGN KEY (kullanici_id) REFERENCES kullanicilar(id) ON DELETE CASCADE,
    FOREIGN KEY (departman_id) REFERENCES departmanlar(id) ON DELETE CASCADE,
    
    INDEX idx_departman (departman_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Şirket çalışanlarının departman bilgileri';

CREATE TABLE partner_detaylari (
    id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT PRIMARY KEY,
    kullanici_id BIGINT UNSIGNED NOT NULL UNIQUE COMMENT 'Her partnerin bir kullanıcı hesabı olmalı',
    
    -- Firma Bilgileri
    firma_unvani VARCHAR(200) NOT NULL COMMENT 'Ticaret sicilindeki tam unvan',
    firma_turu ENUM('Anonim Şirketi', 'Limited Şirketi', 'Şahıs Firması', 'Adi Ortaklık', 'Vakıf', 'Dernek') NOT NULL,
    ticaret_sicil_no VARCHAR(50) NULL,
    ticaret_odasi VARCHAR(100) NULL,
    kurulus_yili YEAR NULL,
    
    -- Vergi Bilgileri
    vergi_dairesi VARCHAR(100) NOT NULL,
    vergi_numarasi VARCHAR(20) NOT NULL UNIQUE,
    tc_kimlik_no VARCHAR(11) NULL COMMENT 'Şahıs firması için zorunlu',
    
    -- İletişim ve Adres
    fatura_adresi TEXT NOT NULL,
    fatura_il VARCHAR(50) NOT NULL,
    fatura_ilce VARCHAR(50) NOT NULL,
    fatura_posta_kodu VARCHAR(10) NULL,
    
    -- Yetkili Kişi Bilgileri
    yetkili_ad_soyad VARCHAR(100) NOT NULL,
    yetkili_tc_no VARCHAR(11) NOT NULL,
    yetkili_telefon VARCHAR(20) NOT NULL,
    yetkili_eposta VARCHAR(100) NOT NULL,
    yetkili_gorev VARCHAR(100) NULL COMMENT 'Genel Müdür, Sahibi, Satış Müdürü vb.',
    
    -- Banka Bilgileri (Komisyon Ödemeleri İçin)
    banka_adi VARCHAR(100) NOT NULL,
    banka_subesi VARCHAR(100) NULL,
    iban VARCHAR(26) NOT NULL UNIQUE,
    hesap_sahibi_adi VARCHAR(150) NOT NULL,
    hesap_para_birimi ENUM('TRY', 'USD', 'EUR') DEFAULT 'TRY',
    
    -- Sözleşme Bilgileri
    sozlesme_no VARCHAR(50) NULL UNIQUE,
    sozlesme_baslangic_tarihi DATE NULL,
    sozlesme_bitis_tarihi DATE NULL,
    sozlesme_pdf_yolu VARCHAR(255) NULL,
    
    -- Onay ve Durum
    onay_durumu ENUM('Beklemede', 'Onaylandi', 'Reddedildi', 'Askida', 'Kara Liste') DEFAULT 'Beklemede',
    onay_tarihi TIMESTAMP NULL,
    onaylayan_admin_id BIGINT UNSIGNED NULL,
    red_nedeni VARCHAR(500) NULL,
    
    -- Ek Bilgiler
    web_sitesi VARCHAR(255) NULL,
    logo_yolu VARCHAR(255) NULL,
    aciklama TEXT NULL,
    
    -- Zaman Damgaları
    olusturulma_tarihi TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    guncellenme_tarihi TIMESTAMP NULL ON UPDATE CURRENT_TIMESTAMP,
    
    -- İndeksler (10M+ veri için kritik)
    INDEX idx_kullanici_id (kullanici_id),
    INDEX idx_vergi_numarasi (vergi_numarasi),
    INDEX idx_onay_durumu (onay_durumu),
    INDEX idx_iban (iban),
    INDEX idx_olusturulma (olusturulma_tarihi),
    
    -- Foreign Key
    FOREIGN KEY (kullanici_id) REFERENCES kullanicilar(id) ON DELETE RESTRICT,
    FOREIGN KEY (onaylayan_admin_id) REFERENCES kullanicilar(id) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Platforma otel tanımlayan işletme sahipleri/yetkilileri';

CREATE TABLE oteller (
    id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT PRIMARY KEY,
    otel_kodu VARCHAR(20) NOT NULL UNIQUE COMMENT 'Platform içi benzersiz kod (örn: HIL-IST-001)',
    partner_id BIGINT UNSIGNED NOT NULL COMMENT 'Bu otelin sahibi/yetkilisi',
    
    -- Temel Bilgiler
    otel_adi VARCHAR(255) NOT NULL,
    otel_turu ENUM('Otel', 'Butik Otel', 'Apart Otel', 'Villa', 'Pansiyon', 'Tatil Köyü', 'Hostel', 'Kamping', 'Apartman Dairesi') NOT NULL,
    yildiz_sayisi TINYINT UNSIGNED NULL COMMENT '1-5 arası veya NULL (belgesiz)',
    turizm_belge_no VARCHAR(50) NULL,
    turizm_belge_turu ENUM('Turizm İşletme Belgeli', 'Turizm Yatırım Belgeli', 'Belediye Belgeli', 'Basit Konaklama Belgeli', 'Belgesiz') NULL,
    
    -- Konum Bilgileri (Arama optimizasyonu için ayrı sütunlar)
    ulke VARCHAR(50) DEFAULT 'Türkiye',
    sehir VARCHAR(50) NOT NULL,
    ilce VARCHAR(50) NOT NULL,
    mahalle VARCHAR(100) NULL,
    tam_adres TEXT NOT NULL,
    posta_kodu VARCHAR(10) NULL,
    enlem DECIMAL(10, 8) NULL COMMENT 'GPS koordinatı - harita için',
    boylam DECIMAL(11, 8) NULL COMMENT 'GPS koordinatı - harita için',
    
    -- Coğrafi Hiyerarşi ID'leri (Ayrı bir lokasyon tablosundan)
    ulke_id SMALLINT UNSIGNED NULL,
    sehir_id INT UNSIGNED NULL,
    ilce_id INT UNSIGNED NULL,
    bolge_id INT UNSIGNED NULL COMMENT 'Tatil bölgesi: Belek, Lara, Çeşme vb.',
    
    -- İletişim Bilgileri
    telefon_1 VARCHAR(20) NOT NULL COMMENT 'Rezervasyon/resepsiyon',
    telefon_2 VARCHAR(20) NULL,
    faks VARCHAR(20) NULL,
    eposta VARCHAR(100) NOT NULL,
    web_sitesi VARCHAR(255) NULL,
    
    -- Operasyonel Bilgiler
    check_in_saati TIME DEFAULT '14:00:00',
    check_out_saati TIME DEFAULT '12:00:00',
    gec_check_out_mumkun_mu TINYINT(1) DEFAULT 0,
    gec_check_out_ucreti DECIMAL(10,2) NULL,
    erken_check_in_mumkun_mu TINYINT(1) DEFAULT 0,
    erken_check_in_ucreti DECIMAL(10,2) NULL,
    
    toplam_oda_sayisi SMALLINT UNSIGNED NOT NULL,
    toplam_yatak_kapasitesi SMALLINT UNSIGNED NULL,
    kat_sayisi TINYINT UNSIGNED NULL,
    asansor_var_mi TINYINT(1) DEFAULT 0,
    asansor_sayisi TINYINT UNSIGNED DEFAULT 0,
    
    -- Açıklamalar
    kisa_aciklama VARCHAR(500) NULL COMMENT 'Listeleme sayfaları için',
    uzun_aciklama TEXT NULL COMMENT 'Detay sayfası için',
    konum_aciklamasi TEXT NULL COMMENT 'Yakın çevre, ulaşım vb.',
    
    -- Finansal ve Komisyon Ayarları
    komisyon_turu ENUM('sabit_oran', 'oda_bazli', 'sezon_bazli', 'karma') DEFAULT 'sabit_oran',
    varsayilan_komisyon_orani DECIMAL(5,2) NOT NULL COMMENT 'Yüzde olarak (örn: 15.50)',
    komisyon_hesaplama_tipi ENUM('gecelik_fiyat_uzerinden', 'toplam_tutar_uzerinden', 'kira_bedeli_uzerinden') DEFAULT 'toplam_tutar_uzerinden',
    
    odeme_vadesi ENUM('Rezervasyon Anında', 'Giriş Günü', 'Çıkış Günü', 'Haftalık', 'Aylık', '15 Günde Bir') NOT NULL DEFAULT 'Çıkış Günü',
    odeme_yontemi ENUM('Havale/EFT', 'Sanal POS (Platform Tahsilat)', 'Otel Tahsilatı') NOT NULL DEFAULT 'Havale/EFT',
    fatura_kesim_turu ENUM('Platform Keser', 'Otel Keser') NOT NULL DEFAULT 'Otel Keser',
    
    depozito_tutari DECIMAL(10,2) NULL COMMENT 'Girişte alınacak depozito',
    depozito_iade_suresi TINYINT UNSIGNED NULL COMMENT 'Çıkıştan kaç gün sonra iade edilir',
    
    minimum_konaklama_gecesi TINYINT UNSIGNED DEFAULT 1,
    maksimum_konaklama_gecesi SMALLINT UNSIGNED DEFAULT 30,
    
    -- Dil Seçenekleri
    konusulan_diller SET('Türkçe', 'İngilizce', 'Almanca', 'Rusça', 'Arapça', 'Fransızca', 'İspanyolca', 'İtalyanca') DEFAULT 'Türkçe',
    
    -- Puanlamalar (Cache - performans için)
    ortalama_puan DECIMAL(3,2) DEFAULT 0.00,
    toplam_yorum_sayisi INT UNSIGNED DEFAULT 0,
    temizlik_puani DECIMAL(3,2) DEFAULT 0.00,
    konfor_puani DECIMAL(3,2) DEFAULT 0.00,
    konum_puani DECIMAL(3,2) DEFAULT 0.00,
    personel_puani DECIMAL(3,2) DEFAULT 0.00,
    fiyat_performans_puani DECIMAL(3,2) DEFAULT 0.00,
    
    -- Görseller
    kapak_fotografi VARCHAR(255) NULL,
    galeri JSON NULL COMMENT '["url1", "url2", ...]',
    video_url VARCHAR(255) NULL,
    sanal_tur_url VARCHAR(255) NULL,
    
    -- Durum Bilgileri
    yayin_durumu ENUM('Taslak', 'Yayında', 'Bakımda', 'Sezon Dışı', 'Kapatıldı', 'Askıda') DEFAULT 'Taslak',
    onay_durumu ENUM('Beklemede', 'İçerik Eksik', 'Onaylandı', 'Reddedildi') DEFAULT 'Beklemede',
    onay_tarihi TIMESTAMP NULL,
    onaylayan_admin_id BIGINT UNSIGNED NULL,
    
    populerlik_sirasi INT UNSIGNED DEFAULT 0 COMMENT 'Arama sonuçlarında sıralama için',
    one_cikan_otel TINYINT(1) DEFAULT 0 COMMENT 'Ana sayfada gösterim',
    tavsiye_edilen_otel TINYINT(1) DEFAULT 0,
    
    -- Zaman Damgaları
    olusturulma_tarihi TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    guncellenme_tarihi TIMESTAMP NULL ON UPDATE CURRENT_TIMESTAMP,
    
    -- İndeksler (10M+ otel verisi için performans kritik)
    INDEX idx_otel_kodu (otel_kodu),
    INDEX idx_partner_id (partner_id),
    INDEX idx_sehir (sehir),
    INDEX idx_ilce (ilce),
    INDEX idx_sehir_ilce (sehir, ilce) COMMENT 'En çok kullanılan arama kombinasyonu',
    INDEX idx_bolge_id (bolge_id),
    INDEX idx_yildiz_sayisi (yildiz_sayisi),
    INDEX idx_otel_turu (otel_turu),
    INDEX idx_yayin_durumu (yayin_durumu),
    INDEX idx_onay_durumu (onay_durumu),
    INDEX idx_populerlik (populerlik_sirasi DESC),
    INDEX idx_one_cikan (one_cikan_otel),
    INDEX idx_enlem_boylam (enlem, boylam) COMMENT 'Yakındaki oteller sorguları için',
    INDEX idx_ortalama_puan (ortalama_puan DESC),
    
    -- Foreign Keys
    FOREIGN KEY (partner_id) REFERENCES partner_detaylari(id) ON DELETE RESTRICT,
    FOREIGN KEY (onaylayan_admin_id) REFERENCES kullanicilar(id) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Platformdaki tüm konaklama tesisleri - 10M+ veri optimizasyonlu';

CREATE TABLE otel_ozellik_kategorileri (
    id SMALLINT UNSIGNED NOT NULL AUTO_INCREMENT PRIMARY KEY,
    kategori_adi VARCHAR(50) NOT NULL,
    kategori_ikon VARCHAR(50) NULL COMMENT 'Font Awesome ikon adı',
    siralama TINYINT UNSIGNED DEFAULT 0,
    aktif_mi TINYINT(1) DEFAULT 1,
    
    INDEX idx_siralama (siralama),
    INDEX idx_aktif (aktif_mi)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Otel özellikleri kategorileri';

CREATE TABLE otel_ozellikleri (
    id INT UNSIGNED NOT NULL AUTO_INCREMENT PRIMARY KEY,
    kategori_id SMALLINT UNSIGNED NOT NULL,
    ozellik_adi VARCHAR(100) NOT NULL,
    ozellik_ikon VARCHAR(50) NULL,
    ucretli_mi TINYINT(1) DEFAULT 0 COMMENT 'Bu özellik ek ücrete tabi olabilir mi?',
    one_cikan_ozellik TINYINT(1) DEFAULT 0 COMMENT 'Filtrelerde öne çıksın mı?',
    siralama SMALLINT UNSIGNED DEFAULT 0,
    aktif_mi TINYINT(1) DEFAULT 1,
    
    UNIQUE KEY uk_kategori_ozellik (kategori_id, ozellik_adi),
    INDEX idx_kategori_id (kategori_id),
    INDEX idx_one_cikan (one_cikan_ozellik),
    INDEX idx_aktif (aktif_mi),
    
    FOREIGN KEY (kategori_id) REFERENCES otel_ozellik_kategorileri(id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Tüm otel özellikleri havuzu';

CREATE TABLE otel_ozellik_iliskileri (
    otel_id BIGINT UNSIGNED NOT NULL,
    ozellik_id INT UNSIGNED NOT NULL,
    ek_ucret DECIMAL(10,2) NULL COMMENT 'Bu özellik ücretliyse tutar',
    aciklama VARCHAR(255) NULL COMMENT 'Özel not (örn: Sadece yaz sezonu açık)',
    
    PRIMARY KEY (otel_id, ozellik_id),
    INDEX idx_otel_id (otel_id),
    INDEX idx_ozellik_id (ozellik_id),
    
    FOREIGN KEY (otel_id) REFERENCES oteller(id) ON DELETE CASCADE,
    FOREIGN KEY (ozellik_id) REFERENCES otel_ozellikleri(id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Otellerin sahip olduğu özellikler';

CREATE TABLE otel_kosullari (
    id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT PRIMARY KEY,
    otel_id BIGINT UNSIGNED NOT NULL,
    
    -- Genel Kurallar
    sigara_politikasi ENUM('Tüm alanlarda yasak', 'Sadece belirli alanlarda serbest', 'Tamamen serbest') DEFAULT 'Sadece belirli alanlarda serbest',
    evcil_hayvan_politikasi ENUM('Kabul edilmez', 'Ücretsiz kabul edilir', 'Ücretli kabul edilir', 'Sadece küçük evcil hayvanlar') DEFAULT 'Kabul edilmez',
    evcil_hayvan_ucreti DECIMAL(10,2) NULL COMMENT 'Gecelik veya konaklama başına',
    evcil_hayvan_depozitosu DECIMAL(10,2) NULL,
    
    -- Parti / Etkinlik
    parti_etkinlik_izin TINYINT(1) DEFAULT 0,
    sessizlik_saatleri_baslangic TIME NULL,
    sessizlik_saatleri_bitis TIME NULL,
    
    -- Yaş Sınırlamaları
    minimum_yas_siniri TINYINT UNSIGNED NULL COMMENT 'Tek başına check-in yapabilecek minimum yaş',
    sadece_yetiskinlere_mi TINYINT(1) DEFAULT 0,
    
    -- Çocuk Politikası (JSON yerine ayrı tablo veya sütunlar)
    cocuk_kabul_yas_araligi VARCHAR(20) NULL COMMENT '0-2, 2-12 gibi',
    bebek_karyolasi_var_mi TINYINT(1) DEFAULT 0,
    bebek_karyolasi_ucreti DECIMAL(10,2) NULL,
    ekstra_yatak_var_mi TINYINT(1) DEFAULT 0,
    ekstra_yatak_ucreti DECIMAL(10,2) NULL,
    maksimum_cocuk_sayisi TINYINT UNSIGNED NULL,
    
    -- Rezervasyon Kuralları
    on_odeme_gerekli_mi TINYINT(1) DEFAULT 1,
    on_odeme_orani DECIMAL(5,2) DEFAULT 30.00 COMMENT 'Yüzde',
    kalan_odeme_zamani ENUM('Girişte', 'Çıkışta', 'Online') DEFAULT 'Girişte',
    kredi_karti_ile_odeme_kabul TINYINT(1) DEFAULT 1,
    nakit_odeme_kabul TINYINT(1) DEFAULT 0,
    
    -- Kabul Edilen Kart Tipleri
    kabul_edilen_kartlar SET('Visa', 'Mastercard', 'American Express', 'Troy', 'UnionPay') NULL,
    
    -- İptal ve Değişiklik Politikası
    iptal_politikasi_ozet VARCHAR(500) NULL,
    detayli_iptal_kosullari JSON NULL COMMENT '{"gun_kala": 7, "kesinti_orani": 0}, {"gun_kala": 3, "kesinti_orani": 50}, {"gun_kala": 0, "kesinti_orani": 100}',
    ucretsiz_iptal_suresi TINYINT UNSIGNED NULL COMMENT 'Kaç gün kala ücretsiz iptal',
    gec_iptal_ceza_orani DECIMAL(5,2) NULL,
    no_show_ceza_orani DECIMAL(5,2) DEFAULT 100.00,
    
    -- Hasar Depozitosu
    hasar_depozitosu_tutari DECIMAL(10,2) NULL,
    hasar_depozitosu_aciklamasi VARCHAR(255) NULL,
    
    -- Diğer
    disaridan_yiyecek_icecek_serbest_mi TINYINT(1) DEFAULT 1,
    ziyaretci_kabul_edilir_mi TINYINT(1) DEFAULT 0,
    ziyaretci_saati_baslangic TIME NULL,
    ziyaretci_saati_bitis TIME NULL,
    
    ozel_kosullar TEXT NULL,
    
    -- Zaman Damgası
    guncellenme_tarihi TIMESTAMP NULL ON UPDATE CURRENT_TIMESTAMP,
    
    UNIQUE KEY uk_otel_id (otel_id),
    INDEX idx_sigara (sigara_politikasi),
    INDEX idx_evcil_hayvan (evcil_hayvan_politikasi),
    INDEX idx_minimum_yas (minimum_yas_siniri),
    
    FOREIGN KEY (otel_id) REFERENCES oteller(id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Otellerin konaklama koşulları ve kuralları';

CREATE TABLE oda_tipleri (
    id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT PRIMARY KEY,
    otel_id BIGINT UNSIGNED NOT NULL,
    
    oda_tip_kodu VARCHAR(30) NOT NULL COMMENT 'Otel içi benzersiz kod',
    oda_adi VARCHAR(100) NOT NULL COMMENT 'Standart Oda, Deluxe Oda, Aile Odası vb.',
    oda_kategorisi ENUM('Standart', 'Superior', 'Deluxe', 'Junior Suite', 'Suite', 'Executive Suite', 'Presidential Suite', 'Aile Odası', 'Engelli Odası', 'Villa') NOT NULL,
    
    -- Kapasite
    maksimum_kisi_sayisi TINYINT UNSIGNED NOT NULL,
    maksimum_yetiskin_sayisi TINYINT UNSIGNED NOT NULL,
    maksimum_cocuk_sayisi TINYINT UNSIGNED DEFAULT 0,
    
    -- Yatak Düzeni
    yatak_tipi ENUM('Tek Kişilik', 'Çift Kişilik', 'Queen Size', 'King Size', 'Super King Size', 'Ranza', 'Çekyat') NULL,
    yatak_sayisi TINYINT UNSIGNED NULL,
    ek_yatak_eklenebilir_mi TINYINT(1) DEFAULT 0,
    
    -- Oda Ölçüleri
    oda_metrekare SMALLINT UNSIGNED NULL,
    balkon_var_mi TINYINT(1) DEFAULT 0,
    balkon_metrekare SMALLINT UNSIGNED NULL,
    manzara_tipi ENUM('Yok', 'Deniz', 'Havuz', 'Bahçe', 'Dağ', 'Şehir', 'Göl', 'İç Avlu') DEFAULT 'Yok',
    
    -- Banyo
    ozel_banyo_var_mi TINYINT(1) DEFAULT 1,
    banyo_tipi ENUM('Duş', 'Küvet', 'Jakuzi', 'Duş ve Küvet') DEFAULT 'Duş',
    
    -- Fiyatlandırma
    standart_gecelik_fiyat DECIMAL(10,2) NOT NULL COMMENT 'Baz fiyat - takvimde değişebilir',
    haftasonu_fark_orani DECIMAL(5,2) DEFAULT 0.00 COMMENT 'Yüzde',
    cocuk_indirim_orani DECIMAL(5,2) DEFAULT 0.00 COMMENT 'Yüzde',
    bebek_ucretsiz_mi TINYINT(1) DEFAULT 1,
    bebek_yas_siniri TINYINT UNSIGNED DEFAULT 2,
    cocuk_yas_siniri TINYINT UNSIGNED DEFAULT 12,
    
    -- Stok Yönetimi
    toplam_oda_sayisi SMALLINT UNSIGNED NOT NULL COMMENT 'Bu tipten kaç oda var',
    overbooking_limit TINYINT UNSIGNED DEFAULT 0 COMMENT 'Kaç oda fazla satış yapılabilir',
    
    -- Görseller
    kapak_fotografi VARCHAR(255) NULL,
    galeri JSON NULL,
    
    -- Özellikler
    ozellikler JSON NULL COMMENT '{"klima": true, "minibar": true, "tv": "LCD"}',
    
    -- Durum
    aktif_mi TINYINT(1) DEFAULT 1,
    siralama SMALLINT UNSIGNED DEFAULT 0,
    
    olusturulma_tarihi TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    guncellenme_tarihi TIMESTAMP NULL ON UPDATE CURRENT_TIMESTAMP,
    
    UNIQUE KEY uk_otel_oda_kodu (otel_id, oda_tip_kodu),
    INDEX idx_otel_id (otel_id),
    INDEX idx_kategori (oda_kategorisi),
    INDEX idx_kapasite (maksimum_kisi_sayisi),
    INDEX idx_aktif (aktif_mi),
    INDEX idx_fiyat (standart_gecelik_fiyat),
    
    FOREIGN KEY (otel_id) REFERENCES oteller(id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Otellerin oda tipi tanımları - 10M+ veri için partition uygun';

CREATE TABLE oda_ozellikleri (
    id SMALLINT UNSIGNED NOT NULL AUTO_INCREMENT PRIMARY KEY,
    kategori VARCHAR(50) NOT NULL COMMENT 'Genel, Yatak Odası, Banyo, Teknoloji, Mutfak',
    ozellik_adi VARCHAR(100) NOT NULL,
    ozellik_ikon VARCHAR(50) NULL,
    siralama SMALLINT UNSIGNED DEFAULT 0,
    aktif_mi TINYINT(1) DEFAULT 1,
    
    INDEX idx_kategori (kategori),
    INDEX idx_aktif (aktif_mi)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Oda içi özellikler havuzu';

CREATE TABLE oda_tipi_ozellikleri (
    oda_tip_id BIGINT UNSIGNED NOT NULL,
    ozellik_id SMALLINT UNSIGNED NOT NULL,
    miktar TINYINT UNSIGNED DEFAULT 1 COMMENT 'Örn: 2 adet TV varsa',
    
    PRIMARY KEY (oda_tip_id, ozellik_id),
    INDEX idx_oda_tip_id (oda_tip_id),
    INDEX idx_ozellik_id (ozellik_id),
    
    FOREIGN KEY (oda_tip_id) REFERENCES oda_tipleri(id) ON DELETE CASCADE,
    FOREIGN KEY (ozellik_id) REFERENCES oda_ozellikleri(id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Oda tiplerine ait özellikler';

CREATE TABLE oda_fiyat_musaitlik (
    id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
    oda_tip_id BIGINT UNSIGNED NOT NULL,
    
    tarih DATE NOT NULL,
    
    -- Fiyatlandırma
    gecelik_fiyat DECIMAL(10,2) NOT NULL,
    indirimli_fiyat DECIMAL(10,2) NULL,
    kampanya_id BIGINT UNSIGNED NULL COMMENT 'Uygulanan kampanya varsa',
    
    -- Müsaitlik
    toplam_oda_sayisi SMALLINT UNSIGNED NOT NULL COMMENT 'O gün satılabilir oda sayısı',
    satilan_oda_sayisi SMALLINT UNSIGNED DEFAULT 0 COMMENT 'Rezerve edilmiş oda sayısı',
    bloke_oda_sayisi SMALLINT UNSIGNED DEFAULT 0 COMMENT 'Bakım/arıza nedeniyle bloke',
    minimum_geceleme TINYINT UNSIGNED DEFAULT 1,
    maksimum_geceleme SMALLINT UNSIGNED DEFAULT 30,
    
    -- Kısıtlamalar
    kapali_satis TINYINT(1) DEFAULT 0 COMMENT 'Satışa kapalı gün',
    sadece_gunubirlik TINYINT(1) DEFAULT 0,
    
    -- Kurallar
    iptal_politikasi_override JSON NULL COMMENT 'Bu güne özel iptal koşulları',
    
    guncellenme_tarihi TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    
    PRIMARY KEY (id, tarih),
    UNIQUE KEY uk_oda_tip_tarih (oda_tip_id, tarih),
    INDEX idx_tarih (tarih),
    INDEX idx_kampanya (kampanya_id),
    INDEX idx_musaitlik (oda_tip_id, tarih, toplam_oda_sayisi, satilan_oda_sayisi),
    
    FOREIGN KEY (oda_tip_id) REFERENCES oda_tipleri(id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Oda bazlı günlük fiyat ve müsaitlik - Aylık partition önerilir'
PARTITION BY RANGE (TO_DAYS(tarih)) (
    PARTITION p_2024_01 VALUES LESS THAN (TO_DAYS('2024-02-01')),
    PARTITION p_2024_02 VALUES LESS THAN (TO_DAYS('2024-03-01')),
    PARTITION p_2024_03 VALUES LESS THAN (TO_DAYS('2024-04-01')),
    PARTITION p_2024_04 VALUES LESS THAN (TO_DAYS('2024-05-01')),
    PARTITION p_2024_05 VALUES LESS THAN (TO_DAYS('2024-06-01')),
    PARTITION p_2024_06 VALUES LESS THAN (TO_DAYS('2024-07-01')),
    PARTITION p_2024_07 VALUES LESS THAN (TO_DAYS('2024-08-01')),
    PARTITION p_2024_08 VALUES LESS THAN (TO_DAYS('2024-09-01')),
    PARTITION p_2024_09 VALUES LESS THAN (TO_DAYS('2024-10-01')),
    PARTITION p_2024_10 VALUES LESS THAN (TO_DAYS('2024-11-01')),
    PARTITION p_2024_11 VALUES LESS THAN (TO_DAYS('2024-12-01')),
    PARTITION p_2024_12 VALUES LESS THAN (TO_DAYS('2025-01-01')),
    PARTITION p_future VALUES LESS THAN MAXVALUE
);

CREATE TABLE ozel_tarih_tanimlari (
    id INT UNSIGNED NOT NULL AUTO_INCREMENT PRIMARY KEY,
    
    tur ENUM('Resmi Tatil', 'Dini Bayram', 'Özel Gün', 'Sezon Başlangıcı', 'Sezon Bitişi', 'Yılbaşı', 'Festival', 'Fuar') NOT NULL,
    ad VARCHAR(100) NOT NULL,
    
    baslangic_tarihi DATE NOT NULL,
    bitis_tarihi DATE NOT NULL,
    
    tekrar_eder_mi TINYINT(1) DEFAULT 0 COMMENT 'Her yıl aynı tarihte tekrarlar mı?',
    tekrar_kurali ENUM('Sabit Tarih', 'Ayın X. Günü', 'Hicri Takvim', 'Her Yıl Aynı Gün') NULL,
    
    ulke VARCHAR(50) DEFAULT 'Türkiye',
    sehir VARCHAR(50) NULL COMMENT 'Sadece belirli bir şehir için geçerliyse',
    
    fiyat_carpani DECIMAL(4,2) DEFAULT 1.00 COMMENT 'Bu dönemde fiyatlar kaç katına çıkar?',
    minimum_geceleme_kurali TINYINT UNSIGNED NULL,
    
    aciklama VARCHAR(255) NULL,
    aktif_mi TINYINT(1) DEFAULT 1,
    
    INDEX idx_tarih_aralik (baslangic_tarihi, bitis_tarihi),
    INDEX idx_tur (tur),
    INDEX idx_ulke (ulke),
    INDEX idx_sehir (sehir),
    INDEX idx_aktif (aktif_mi)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Özel gün, tatil ve sezon tanımları';

CREATE TABLE otel_gorselleri (
    id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT PRIMARY KEY,
    otel_id BIGINT UNSIGNED NOT NULL,
    
    gorsel_url VARCHAR(500) NOT NULL,
    thumbnail_url VARCHAR(500) NULL,
    gorsel_turu ENUM('Dış Cephe', 'Lobi', 'Restoran', 'Havuz', 'Plaj', 'Oda', 'Banyo', 'Spor Salonu', 'SPA', 'Toplantı Odası', 'Genel Alan', 'Yemek', 'Manzara') NOT NULL,
    
    baslik VARCHAR(200) NULL,
    aciklama TEXT NULL,
    
    kapak_fotografi_mi TINYINT(1) DEFAULT 0,
    one_cikan TINYINT(1) DEFAULT 0,
    siralama SMALLINT UNSIGNED DEFAULT 0,
    
    boyut_kb INT UNSIGNED NULL,
    genislik SMALLINT UNSIGNED NULL,
    yukseklik SMALLINT UNSIGNED NULL,
    
    onay_durumu ENUM('Beklemede', 'Onaylandı', 'Reddedildi') DEFAULT 'Beklemede',
    onaylayan_admin_id BIGINT UNSIGNED NULL,
    onay_tarihi TIMESTAMP NULL,
    
    yukleyen_kullanici_id BIGINT UNSIGNED NULL,
    olusturulma_tarihi TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    
    INDEX idx_otel_id (otel_id),
    INDEX idx_gorsel_turu (gorsel_turu),
    INDEX idx_kapak (otel_id, kapak_fotografi_mi),
    INDEX idx_onay (onay_durumu),
    
    FOREIGN KEY (otel_id) REFERENCES oteller(id) ON DELETE CASCADE,
    FOREIGN KEY (onaylayan_admin_id) REFERENCES kullanicilar(id) ON DELETE SET NULL,
    FOREIGN KEY (yukleyen_kullanici_id) REFERENCES kullanicilar(id) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Otellere ait detaylı görsel galerisi';

CREATE TABLE oda_gorselleri (
    id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT PRIMARY KEY,
    oda_tip_id BIGINT UNSIGNED NOT NULL,
    
    gorsel_url VARCHAR(500) NOT NULL,
    thumbnail_url VARCHAR(500) NULL,
    
    baslik VARCHAR(200) NULL,
    aciklama TEXT NULL,
    
    kapak_fotografi_mi TINYINT(1) DEFAULT 0,
    siralama SMALLINT UNSIGNED DEFAULT 0,
    
    boyut_kb INT UNSIGNED NULL,
    
    onay_durumu ENUM('Beklemede', 'Onaylandı', 'Reddedildi') DEFAULT 'Beklemede',
    onaylayan_admin_id BIGINT UNSIGNED NULL,
    onay_tarihi TIMESTAMP NULL,
    
    yukleyen_kullanici_id BIGINT UNSIGNED NULL,
    olusturulma_tarihi TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    
    INDEX idx_oda_tip_id (oda_tip_id),
    INDEX idx_kapak (oda_tip_id, kapak_fotografi_mi),
    INDEX idx_onay (onay_durumu),
    
    FOREIGN KEY (oda_tip_id) REFERENCES oda_tipleri(id) ON DELETE CASCADE,
    FOREIGN KEY (onaylayan_admin_id) REFERENCES kullanicilar(id) ON DELETE SET NULL,
    FOREIGN KEY (yukleyen_kullanici_id) REFERENCES kullanicilar(id) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Oda tiplerine ait görseller';

CREATE TABLE yorumlar (
    id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT PRIMARY KEY,
    otel_id BIGINT UNSIGNED NOT NULL,
    kullanici_id BIGINT UNSIGNED NOT NULL,
    rezervasyon_id BIGINT UNSIGNED NULL COMMENT 'Yorumun hangi rezervasyona ait olduğu (doğrulama için)',
    
    -- Puanlamalar (1-5 arası)
    genel_puan TINYINT UNSIGNED NOT NULL CHECK (genel_puan BETWEEN 1 AND 5),
    temizlik_puani TINYINT UNSIGNED NOT NULL CHECK (temizlik_puani BETWEEN 1 AND 5),
    konfor_puani TINYINT UNSIGNED NOT NULL CHECK (konfor_puani BETWEEN 1 AND 5),
    konum_puani TINYINT UNSIGNED NOT NULL CHECK (konum_puani BETWEEN 1 AND 5),
    personel_puani TINYINT UNSIGNED NOT NULL CHECK (personel_puani BETWEEN 1 AND 5),
    fiyat_performans_puani TINYINT UNSIGNED NOT NULL CHECK (fiyat_performans_puani BETWEEN 1 AND 5),
    
    -- Yorum İçeriği
    yorum_basligi VARCHAR(200) NULL,
    yorum_metni TEXT NOT NULL,
    olumlu_yanlar TEXT NULL,
    olumsuz_yanlar TEXT NULL,
    
    -- Konaklama Detayları
    konaklama_tarihi DATE NULL,
    konaklama_turu ENUM('İş', 'Çift', 'Aile', 'Arkadaş Grubu', 'Yalnız') NULL,
    kaldigi_oda_tipi VARCHAR(100) NULL,
    gece_sayisi TINYINT UNSIGNED NULL,
    
    -- Doğrulama ve Onay
    dogrulanmis_konaklama TINYINT(1) DEFAULT 0 COMMENT 'Rezervasyon ile eşleşti mi?',
    onay_durumu ENUM('Beklemede', 'Onaylandı', 'Reddedildi', 'İnceleniyor') DEFAULT 'Beklemede',
    onaylayan_admin_id BIGINT UNSIGNED NULL,
    onay_tarihi TIMESTAMP NULL,
    red_nedeni VARCHAR(500) NULL,
    
    -- Etkileşim
    faydali_oy_sayisi INT UNSIGNED DEFAULT 0,
    faydasiz_oy_sayisi INT UNSIGNED DEFAULT 0,
    rapor_sayisi SMALLINT UNSIGNED DEFAULT 0,
    
    -- Otel Yanıtı
    otel_yaniti TEXT NULL,
    otel_yaniti_tarihi TIMESTAMP NULL,
    yanitlayan_kullanici_id BIGINT UNSIGNED NULL,
    
    -- Görseller
    yorum_gorselleri JSON NULL COMMENT '["url1", "url2"]',
    
    -- Anonim
    anonim_mi TINYINT(1) DEFAULT 0,
    
    -- Zaman Damgaları
    olusturulma_tarihi TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    guncellenme_tarihi TIMESTAMP NULL ON UPDATE CURRENT_TIMESTAMP,
    
    -- İndeksler
    UNIQUE KEY uk_kullanici_otel_rezervasyon (kullanici_id, otel_id, rezervasyon_id) COMMENT 'Aynı rezervasyona bir kez yorum',
    INDEX idx_otel_id (otel_id),
    INDEX idx_kullanici_id (kullanici_id),
    INDEX idx_genel_puan (genel_puan DESC),
    INDEX idx_onay_durumu (onay_durumu),
    INDEX idx_olusturulma (olusturulma_tarihi DESC),
    INDEX idx_dogrulanmis (dogrulanmis_konaklama),
    INDEX idx_otel_puan (otel_id, genel_puan),
    
    -- Foreign Keys
    FOREIGN KEY (otel_id) REFERENCES oteller(id) ON DELETE CASCADE,
    FOREIGN KEY (kullanici_id) REFERENCES kullanicilar(id) ON DELETE CASCADE,
    FOREIGN KEY (onaylayan_admin_id) REFERENCES kullanicilar(id) ON DELETE SET NULL,
    FOREIGN KEY (yanitlayan_kullanici_id) REFERENCES kullanicilar(id) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Otellere yapılan kullanıcı değerlendirmeleri - 10M+ yorum için partition uygun';

CREATE TABLE rezervasyonlar (
    id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
    rezervasyon_no VARCHAR(20) NOT NULL UNIQUE COMMENT 'Platform geneli benzersiz no',
    
    -- İlişkiler
    otel_id BIGINT UNSIGNED NOT NULL,
    oda_tip_id BIGINT UNSIGNED NOT NULL,
    kullanici_id BIGINT UNSIGNED NOT NULL,
    
    -- Misafir Bilgileri
    misafir_ad_soyad VARCHAR(100) NOT NULL,
    misafir_eposta VARCHAR(100) NOT NULL,
    misafir_telefon VARCHAR(20) NOT NULL,
    misafir_ulke VARCHAR(50) NULL,
    misafir_notu TEXT NULL,
    
    -- Konaklama Detayları
    giris_tarihi DATE NOT NULL,
    cikis_tarihi DATE NOT NULL,
    gece_sayisi SMALLINT UNSIGNED GENERATED ALWAYS AS (DATEDIFF(cikis_tarihi, giris_tarihi)) STORED,
    
    yetiskin_sayisi TINYINT UNSIGNED NOT NULL,
    cocuk_sayisi TINYINT UNSIGNED DEFAULT 0,
    bebek_sayisi TINYINT UNSIGNED DEFAULT 0,
    cocuk_yaslari JSON NULL COMMENT '[5, 8, 12]',
    
    oda_sayisi TINYINT UNSIGNED DEFAULT 1,
    
    -- Fiyatlandırma (Finansal Kayıt)
    gecelik_fiyat DECIMAL(10,2) NOT NULL COMMENT 'Rezervasyon anındaki gecelik fiyat',
    toplam_oda_tutari DECIMAL(10,2) NOT NULL COMMENT 'gecelik_fiyat * gece_sayisi * oda_sayisi',
    
    ek_hizmet_tutari DECIMAL(10,2) DEFAULT 0.00,
    vergi_tutari DECIMAL(10,2) DEFAULT 0.00,
    indirim_tutari DECIMAL(10,2) DEFAULT 0.00,
    kupon_indirimi DECIMAL(10,2) DEFAULT 0.00,
    
    toplam_tutar DECIMAL(10,2) NOT NULL COMMENT 'Müşteriden tahsil edilecek net tutar',
    
    komisyon_orani DECIMAL(5,2) NOT NULL COMMENT 'Rezervasyon anındaki geçerli komisyon oranı',
    komisyon_tutari DECIMAL(10,2) GENERATED ALWAYS AS (toplam_tutar * komisyon_orani / 100) STORED,
    otele_odenecek_tutar DECIMAL(10,2) GENERATED ALWAYS AS (toplam_tutar - (toplam_tutar * komisyon_orani / 100)) STORED,
    
    para_birimi VARCHAR(3) DEFAULT 'TRY',
    
    -- Ödeme Bilgileri
    odeme_durumu ENUM('Beklemede', 'Ön Ödeme Alındı', 'Tamamlandı', 'İade Edildi', 'Kısmi İade', 'Başarısız') DEFAULT 'Beklemede',
    odeme_yontemi ENUM('Kredi Kartı', 'Banka Havalesi', 'Kapıda Ödeme', 'Sanal POS') NULL,
    odeme_tarihi TIMESTAMP NULL,
    on_odeme_tutari DECIMAL(10,2) NULL,
    kalan_odeme_tutari DECIMAL(10,2) NULL,
    
    -- Rezervasyon Durumu
    durum ENUM('Onay Bekliyor', 'Onaylandı', 'İptal Edildi', 'No-Show', 'Tamamlandı', 'Değişiklik Bekliyor') DEFAULT 'Onay Bekliyor',
    iptal_tarihi TIMESTAMP NULL,
    iptal_nedeni VARCHAR(500) NULL,
    iptal_eden ENUM('Misafir', 'Otel', 'Platform') NULL,
    iptal_kesintisi DECIMAL(10,2) NULL,
    iade_tutari DECIMAL(10,2) NULL,
    
    -- Otel Onayı
    otel_onay_durumu ENUM('Beklemede', 'Onaylandı', 'Reddedildi') DEFAULT 'Beklemede',
    otel_onay_tarihi TIMESTAMP NULL,
    otel_red_nedeni VARCHAR(500) NULL,
    
    -- Özel İstekler
    erken_giris_talebi TINYINT(1) DEFAULT 0,
    gec_cikis_talebi TINYINT(1) DEFAULT 0,
    transfer_talebi TINYINT(1) DEFAULT 0,
    ozel_istekler TEXT NULL,
    
    -- Kaynak / Kanal
    kaynak ENUM('Web', 'Mobil App', 'Telefon', 'Acente', 'Kurumsal') DEFAULT 'Web',
    kampanya_kodu VARCHAR(50) NULL,
    referans_kodu VARCHAR(50) NULL,
    
    -- Zaman Damgaları
    olusturulma_tarihi TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    guncellenme_tarihi TIMESTAMP NULL ON UPDATE CURRENT_TIMESTAMP,
    check_in_yapildi_mi TINYINT(1) DEFAULT 0,
    check_in_tarihi TIMESTAMP NULL,
    check_out_yapildi_mi TINYINT(1) DEFAULT 0,
    check_out_tarihi TIMESTAMP NULL,
    
    -- İndeksler ve Partition
    PRIMARY KEY (id, giris_tarihi),
    UNIQUE KEY uk_rezervasyon_no (rezervasyon_no, giris_tarihi),
    INDEX idx_otel_id (otel_id),
    INDEX idx_kullanici_id (kullanici_id),
    INDEX idx_giris_tarihi (giris_tarihi),
    INDEX idx_durum (durum),
    INDEX idx_odeme_durumu (odeme_durumu),
    INDEX idx_otel_tarih (otel_id, giris_tarihi),
    INDEX idx_olusturulma (olusturulma_tarihi DESC),
    
    FOREIGN KEY (otel_id) REFERENCES oteller(id) ON DELETE RESTRICT,
    FOREIGN KEY (oda_tip_id) REFERENCES oda_tipleri(id) ON DELETE RESTRICT,
    FOREIGN KEY (kullanici_id) REFERENCES kullanicilar(id) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Tüm rezervasyonlar - Aylık partition zorunlu'
PARTITION BY RANGE (TO_DAYS(giris_tarihi)) (
    PARTITION p_2024_01 VALUES LESS THAN (TO_DAYS('2024-02-01')),
    PARTITION p_2024_02 VALUES LESS THAN (TO_DAYS('2024-03-01')),
    PARTITION p_2024_03 VALUES LESS THAN (TO_DAYS('2024-04-01')),
    PARTITION p_2024_04 VALUES LESS THAN (TO_DAYS('2024-05-01')),
    PARTITION p_2024_05 VALUES LESS THAN (TO_DAYS('2024-06-01')),
    PARTITION p_2024_06 VALUES LESS THAN (TO_DAYS('2024-07-01')),
    PARTITION p_2024_07 VALUES LESS THAN (TO_DAYS('2024-08-01')),
    PARTITION p_2024_08 VALUES LESS THAN (TO_DAYS('2024-09-01')),
    PARTITION p_2024_09 VALUES LESS THAN (TO_DAYS('2024-10-01')),
    PARTITION p_2024_10 VALUES LESS THAN (TO_DAYS('2024-11-01')),
    PARTITION p_2024_11 VALUES LESS THAN (TO_DAYS('2024-12-01')),
    PARTITION p_2024_12 VALUES LESS THAN (TO_DAYS('2025-01-01')),
    PARTITION p_future VALUES LESS THAN MAXVALUE
);

CREATE TABLE kampanyalar (
    id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT PRIMARY KEY,
    kampanya_kodu VARCHAR(50) NOT NULL UNIQUE,
    kampanya_adi VARCHAR(200) NOT NULL,
    kampanya_aciklamasi TEXT NULL,
    
    tur ENUM('Yüzde İndirim', 'Sabit İndirim', 'Erken Rezervasyon', 'Uzun Konaklama', 'Son Dakika', 'Mobil Özel', 'Kupon Kodu') NOT NULL,
    
    indirim_orani DECIMAL(5,2) NULL COMMENT 'Yüzde indirim için',
    indirim_tutari DECIMAL(10,2) NULL COMMENT 'Sabit indirim için',
    maksimum_indirim_tutari DECIMAL(10,2) NULL,
    minimum_sepet_tutari DECIMAL(10,2) NULL,
    
    -- Hedefleme
    hedef_otel_turu ENUM('Tümü', 'Belirli Oteller', 'Belirli Şehirler', 'Belirli Bölgeler', 'Zincir Oteller') DEFAULT 'Tümü',
    hedef_otel_idleri JSON NULL COMMENT '[1, 5, 10, 15]',
    hedef_sehirler JSON NULL COMMENT '["Antalya", "Muğla"]',
    
    hedef_kullanici_turu ENUM('Tümü', 'Yeni Üye', 'Sadık Müşteri', 'Belirli Ülkeler') DEFAULT 'Tümü',
    minimum_gecmis_rezervasyon TINYINT UNSIGNED NULL COMMENT 'En az X rezervasyon yapmış olanlar',
    
    -- Tarih Aralığı
    baslangic_tarihi DATETIME NOT NULL,
    bitis_tarihi DATETIME NOT NULL,
    rezervasyon_tarih_araligi_baslangic DATE NULL COMMENT 'Sadece bu tarihler arası yapılan rezervasyonlar',
    rezervasyon_tarih_araligi_bitis DATE NULL,
    konaklama_tarih_araligi_baslangic DATE NULL COMMENT 'Sadece bu tarihler arası konaklamalar',
    konaklama_tarih_araligi_bitis DATE NULL,
    
    -- Konaklama Şartları
    minimum_geceleme TINYINT UNSIGNED DEFAULT 1,
    maksimum_geceleme SMALLINT UNSIGNED NULL,
    erken_rezervasyon_gun_sayisi SMALLINT UNSIGNED NULL COMMENT 'En az X gün önce rezervasyon',
    
    -- Kullanım Limitleri
    toplam_kullanim_limiti INT UNSIGNED NULL,
    kullanici_basina_limit TINYINT UNSIGNED DEFAULT 1,
    kullanilan_adet INT UNSIGNED DEFAULT 0,
    
    -- Durum
    aktif_mi TINYINT(1) DEFAULT 1,
    one_cikan_kampanya TINYINT(1) DEFAULT 0,
    banner_gorseli VARCHAR(255) NULL,
    
    olusturan_admin_id BIGINT UNSIGNED NULL,
    olusturulma_tarihi TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    guncellenme_tarihi TIMESTAMP NULL ON UPDATE CURRENT_TIMESTAMP,
    
    INDEX idx_kod (kampanya_kodu),
    INDEX idx_tarih (baslangic_tarihi, bitis_tarihi),
    INDEX idx_aktif (aktif_mi),
    INDEX idx_tur (tur),
    
    FOREIGN KEY (olusturan_admin_id) REFERENCES kullanicilar(id) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Platform geneli kampanya ve indirim tanımları';

CREATE TABLE odeme_islemleri (
    id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
    islem_no VARCHAR(30) NOT NULL UNIQUE COMMENT 'Platform geneli benzersiz işlem numarası',
    
    -- İlişkiler
    rezervasyon_id BIGINT UNSIGNED NOT NULL,
    kullanici_id BIGINT UNSIGNED NOT NULL COMMENT 'Ödemeyi yapan kullanıcı',
    otel_id BIGINT UNSIGNED NOT NULL,
    
    -- Ödeme Detayları
    odeme_turu ENUM('Ön Ödeme', 'Kalan Ödeme', 'Tam Ödeme', 'İade', 'Kısmi İade', 'Komisyon Kesintisi', 'Taksit') NOT NULL,
    odeme_yontemi ENUM('Kredi Kartı', 'Banka Havalesi/EFT', 'Sanal POS', 'Kapıda Ödeme', 'Hediye Kartı', 'Puan Kullanımı', 'Hızlı Havale', 'Dijital Cüzdan') NOT NULL,
    odeme_durumu ENUM('Beklemede', 'İşleniyor', 'Başarılı', 'Başarısız', 'İptal Edildi', 'Geri Ödendi', 'Kısmi Geri Ödendi', 'Askıda') DEFAULT 'Beklemede',
    
    -- Tutar Bilgileri
    tutar DECIMAL(10,2) NOT NULL COMMENT 'İşlem tutarı',
    komisyon_tutari DECIMAL(10,2) DEFAULT 0.00 COMMENT 'Platform komisyonu',
    vergi_tutari DECIMAL(10,2) DEFAULT 0.00 COMMENT 'KDV vb.',
    toplam_tahsilat DECIMAL(10,2) NOT NULL COMMENT 'Müşteriden çekilen toplam tutar',
    
    para_birimi VARCHAR(3) DEFAULT 'TRY',
    kur_orani DECIMAL(10,6) DEFAULT 1.000000 COMMENT 'Yabancı para biriminden çevrim için',
    orijinal_tutar DECIMAL(10,2) NULL COMMENT 'Yabancı para birimindeki tutar',
    orijinal_para_birimi VARCHAR(3) NULL,
    
    -- Taksit Bilgileri (Taksitli ödeme için)
    taksit_sayisi TINYINT UNSIGNED DEFAULT 1,
    taksit_sirasi TINYINT UNSIGNED DEFAULT 1,
    ana_odeme_id BIGINT UNSIGNED NULL COMMENT 'Taksitli ödemenin ana kaydı',
    
    -- Kart / Banka Bilgileri (Maskelenmiş)
    kart_sahibi_adi VARCHAR(100) NULL,
    kart_numarasi_masked VARCHAR(20) NULL COMMENT '**** **** **** 1234',
    kart_tipi ENUM('Visa', 'Mastercard', 'American Express', 'Troy', 'UnionPay', 'Diğer') NULL,
    kart_son_kullanma VARCHAR(5) NULL COMMENT 'MM/YY',
    banka_adi VARCHAR(100) NULL,
    iban_masked VARCHAR(30) NULL COMMENT 'TR*****1234',
    
    -- Sanal POS / Ödeme Sağlayıcı
    odeme_saglayici ENUM('İyzico', 'PayTR', 'Stripe', 'PayPal', 'Garanti POS', 'Yapı Kredi POS', 'İş Bankası POS', 'Akbank POS', 'Halkbank POS', 'Vakıfbank POS') NULL,
    saglayici_islem_no VARCHAR(100) NULL,
    saglayici_onay_kodu VARCHAR(50) NULL,
    saglayici_hata_kodu VARCHAR(20) NULL,
    saglayici_hata_mesaji VARCHAR(500) NULL,
    
    -- 3D Secure
    uc_d_secure_kullanildi TINYINT(1) DEFAULT 0,
    uc_d_secure_durumu ENUM('Başarılı', 'Başarısız', 'Kullanılmadı') DEFAULT 'Kullanılmadı',
    
    -- İade Bilgileri
    iade_edilebilir_tutar DECIMAL(10,2) GENERATED ALWAYS AS (tutar - COALESCE(iade_edilen_tutar, 0)) STORED,
    iade_edilen_tutar DECIMAL(10,2) DEFAULT 0.00,
    iade_nedeni ENUM('İptal', 'Değişiklik', 'Mükerrer Ödeme', 'Anlaşmazlık', 'Diğer') NULL,
    iade_aciklamasi TEXT NULL,
    iade_tarihi TIMESTAMP NULL,
    iade_eden_admin_id BIGINT UNSIGNED NULL,
    
    -- Kesinti / Ceza
    iptal_kesintisi_orani DECIMAL(5,2) NULL,
    iptal_kesintisi_tutari DECIMAL(10,2) NULL,
    
    -- Fatura İlişkisi
    fatura_id BIGINT UNSIGNED NULL,
    
    -- IP ve Cihaz Bilgileri (Güvenlik)
    odeme_ip_adresi VARCHAR(45) NULL,
    odeme_cihaz_bilgisi VARCHAR(255) NULL,
    odeme_konum VARCHAR(100) NULL,
    
    -- Risk Değerlendirmesi
    risk_puani TINYINT UNSIGNED DEFAULT 0 COMMENT '0-100 arası',
    risk_kontrolu_sonucu ENUM('Düşük', 'Orta', 'Yüksek', 'İnceleniyor') DEFAULT 'İnceleniyor',
    manuel_onay_gerektirir TINYINT(1) DEFAULT 0,
    manuel_onaylayan_admin_id BIGINT UNSIGNED NULL,
    manuel_onay_tarihi TIMESTAMP NULL,
    
    -- Zaman Damgaları
    odeme_baslangic_tarihi TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    odeme_tamamlanma_tarihi TIMESTAMP NULL,
    son_durum_degisikligi TIMESTAMP NULL ON UPDATE CURRENT_TIMESTAMP,
    
    -- İndeksler
    PRIMARY KEY (id, odeme_baslangic_tarihi),
    UNIQUE KEY uk_islem_no (islem_no, odeme_baslangic_tarihi),
    INDEX idx_rezervasyon_id (rezervasyon_id),
    INDEX idx_kullanici_id (kullanici_id),
    INDEX idx_otel_id (otel_id),
    INDEX idx_odeme_durumu (odeme_durumu),
    INDEX idx_odeme_turu (odeme_turu),
    INDEX idx_tarih (odeme_baslangic_tarihi DESC),
    INDEX idx_saglayici_islem (saglayici_islem_no),
    INDEX idx_risk (risk_puani, manuel_onay_gerektirir),
    
    FOREIGN KEY (rezervasyon_id) REFERENCES rezervasyonlar(id) ON DELETE RESTRICT,
    FOREIGN KEY (kullanici_id) REFERENCES kullanicilar(id) ON DELETE RESTRICT,
    FOREIGN KEY (otel_id) REFERENCES oteller(id) ON DELETE RESTRICT,
    FOREIGN KEY (ana_odeme_id) REFERENCES odeme_islemleri(id) ON DELETE SET NULL,
    FOREIGN KEY (iade_eden_admin_id) REFERENCES kullanicilar(id) ON DELETE SET NULL,
    FOREIGN KEY (manuel_onaylayan_admin_id) REFERENCES kullanicilar(id) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Tüm ödeme işlemleri - Aylık partition zorunlu'
PARTITION BY RANGE (TO_DAYS(odeme_baslangic_tarihi)) (
    PARTITION p_2024_01 VALUES LESS THAN (TO_DAYS('2024-02-01')),
    PARTITION p_2024_02 VALUES LESS THAN (TO_DAYS('2024-03-01')),
    PARTITION p_2024_03 VALUES LESS THAN (TO_DAYS('2024-04-01')),
    PARTITION p_2024_04 VALUES LESS THAN (TO_DAYS('2024-05-01')),
    PARTITION p_2024_05 VALUES LESS THAN (TO_DAYS('2024-06-01')),
    PARTITION p_2024_06 VALUES LESS THAN (TO_DAYS('2024-07-01')),
    PARTITION p_2024_07 VALUES LESS THAN (TO_DAYS('2024-08-01')),
    PARTITION p_2024_08 VALUES LESS THAN (TO_DAYS('2024-09-01')),
    PARTITION p_2024_09 VALUES LESS THAN (TO_DAYS('2024-10-01')),
    PARTITION p_2024_10 VALUES LESS THAN (TO_DAYS('2024-11-01')),
    PARTITION p_2024_11 VALUES LESS THAN (TO_DAYS('2024-12-01')),
    PARTITION p_2024_12 VALUES LESS THAN (TO_DAYS('2025-01-01')),
    PARTITION p_future VALUES LESS THAN MAXVALUE
);

CREATE TABLE basarisiz_odeme_denemeleri (
    id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
    rezervasyon_id BIGINT UNSIGNED NOT NULL,
    kullanici_id BIGINT UNSIGNED NOT NULL,
    
    deneme_tarihi TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    
    tutar DECIMAL(10,2) NOT NULL,
    para_birimi VARCHAR(3) DEFAULT 'TRY',
    
    odeme_yontemi ENUM('Kredi Kartı', 'Banka Havalesi/EFT', 'Sanal POS', 'Dijital Cüzdan') NOT NULL,
    kart_tipi ENUM('Visa', 'Mastercard', 'American Express', 'Troy', 'UnionPay', 'Diğer') NULL,
    kart_numarasi_masked VARCHAR(20) NULL,
    
    odeme_saglayici ENUM('İyzico', 'PayTR', 'Stripe', 'Garanti POS', 'Yapı Kredi POS', 'İş Bankası POS') NULL,
    hata_kodu VARCHAR(20) NULL,
    hata_mesaji VARCHAR(500) NOT NULL,
    hata_detayi TEXT NULL,
    
    uc_d_secure_durumu ENUM('Başarılı', 'Başarısız', 'Kullanılmadı') DEFAULT 'Kullanılmadı',
    
    ip_adresi VARCHAR(45) NULL,
    cihaz_bilgisi VARCHAR(255) NULL,
    
    cozuldu_mu TINYINT(1) DEFAULT 0 COMMENT 'Sonraki denemede başarılı oldu mu?',
    cozulme_tarihi TIMESTAMP NULL,
    
    PRIMARY KEY (id, deneme_tarihi),
    INDEX idx_rezervasyon_id (rezervasyon_id),
    INDEX idx_kullanici_id (kullanici_id),
    INDEX idx_hata_kodu (hata_kodu),
    INDEX idx_tarih (deneme_tarihi DESC),
    INDEX idx_cozuldu (cozuldu_mu),
    
    FOREIGN KEY (rezervasyon_id) REFERENCES rezervasyonlar(id) ON DELETE CASCADE,
    FOREIGN KEY (kullanici_id) REFERENCES kullanicilar(id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Başarısız ödeme denemelerinin kaydı - Aylık partition önerilir'
PARTITION BY RANGE (TO_DAYS(deneme_tarihi)) (
    PARTITION p_2024_01 VALUES LESS THAN (TO_DAYS('2024-02-01')),
    PARTITION p_2024_02 VALUES LESS THAN (TO_DAYS('2024-03-01')),
    PARTITION p_2024_03 VALUES LESS THAN (TO_DAYS('2024-04-01')),
    PARTITION p_2024_04 VALUES LESS THAN (TO_DAYS('2024-05-01')),
    PARTITION p_2024_05 VALUES LESS THAN (TO_DAYS('2024-06-01')),
    PARTITION p_2024_06 VALUES LESS THAN (TO_DAYS('2024-07-01')),
    PARTITION p_future VALUES LESS THAN MAXVALUE
);

CREATE TABLE faturalar (
    id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT PRIMARY KEY,
    fatura_no VARCHAR(30) NOT NULL UNIQUE,
    fatura_tarihi DATE NOT NULL,
    fatura_turu ENUM('Satış Faturası', 'İade Faturası', 'Komisyon Faturası', 'Proforma', 'e-Fatura', 'e-Arşiv') NOT NULL,
    
    -- İlişkiler
    rezervasyon_id BIGINT UNSIGNED NULL,
    otel_id BIGINT UNSIGNED NULL COMMENT 'Fatura otele kesiliyorsa',
    kullanici_id BIGINT UNSIGNED NULL COMMENT 'Fatura misafire kesiliyorsa',
    partner_id BIGINT UNSIGNED NULL COMMENT 'Fatura partnere kesiliyorsa',
    odeme_islem_id BIGINT UNSIGNED NULL,
    
    -- Fatura Bilgileri
    fatura_kesen ENUM('Platform', 'Otel') NOT NULL,
    fatura_kesen_unvan VARCHAR(200) NOT NULL,
    fatura_kesen_vergi_dairesi VARCHAR(100) NOT NULL,
    fatura_kesen_vergi_no VARCHAR(20) NOT NULL,
    fatura_kesen_adres TEXT NOT NULL,
    
    fatura_alici_unvan VARCHAR(200) NOT NULL,
    fatura_alici_vergi_dairesi VARCHAR(100) NULL,
    fatura_alici_vergi_no VARCHAR(20) NULL,
    fatura_alici_tc_no VARCHAR(11) NULL,
    fatura_alici_adres TEXT NOT NULL,
    fatura_alici_eposta VARCHAR(100) NULL,
    
    -- Tutar Bilgileri
    ara_toplam DECIMAL(10,2) NOT NULL,
    kdv_orani DECIMAL(5,2) DEFAULT 20.00,
    kdv_tutari DECIMAL(10,2) NOT NULL,
    diger_vergiler DECIMAL(10,2) DEFAULT 0.00,
    konaklama_vergisi_orani DECIMAL(5,2) DEFAULT 2.00 COMMENT 'Türkiye konaklama vergisi',
    konaklama_vergisi_tutari DECIMAL(10,2) DEFAULT 0.00,
    genel_toplam DECIMAL(10,2) NOT NULL,
    
    para_birimi VARCHAR(3) DEFAULT 'TRY',
    yalniz_yaziyla VARCHAR(500) NULL COMMENT 'Tutarın yazı ile ifadesi',
    
    -- e-Fatura / e-Arşiv Bilgileri
    e_fatura_uuid VARCHAR(36) NULL UNIQUE,
    e_fatura_durumu ENUM('Taslak', 'Oluşturuldu', 'Gönderildi', 'Onaylandı', 'Reddedildi', 'İptal Edildi') NULL,
    e_fatura_gonderim_tarihi TIMESTAMP NULL,
    e_fatura_onay_tarihi TIMESTAMP NULL,
    e_fatura_entegrasyon_turu ENUM('GİB Portal', 'Özel Entegratör', 'Doğrudan Entegrasyon') NULL,
    entegrator_adi VARCHAR(50) NULL,
    
    -- PDF / Görüntü
    fatura_pdf_yolu VARCHAR(500) NULL,
    fatura_html_yolu VARCHAR(500) NULL,
    fatura_xml_yolu VARCHAR(500) NULL,
    
    -- Durum
    fatura_durumu ENUM('Taslak', 'Kesildi', 'İptal Edildi', 'İade Faturası Kesildi') DEFAULT 'Kesildi',
    iptal_nedeni VARCHAR(500) NULL,
    iptal_tarihi TIMESTAMP NULL,
    iptal_eden_admin_id BIGINT UNSIGNED NULL,
    
    -- Notlar
    fatura_notu TEXT NULL,
    siparis_no VARCHAR(50) NULL,
    
    -- Zaman Damgaları
    olusturulma_tarihi TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    guncellenme_tarihi TIMESTAMP NULL ON UPDATE CURRENT_TIMESTAMP,
    vade_tarihi DATE NULL,
    odeme_tarihi DATE NULL,
    
    -- İndeksler
    INDEX idx_fatura_no (fatura_no),
    INDEX idx_fatura_tarihi (fatura_tarihi DESC),
    INDEX idx_rezervasyon_id (rezervasyon_id),
    INDEX idx_otel_id (otel_id),
    INDEX idx_kullanici_id (kullanici_id),
    INDEX idx_partner_id (partner_id),
    INDEX idx_fatura_turu (fatura_turu),
    INDEX idx_e_fatura_uuid (e_fatura_uuid),
    INDEX idx_durum (fatura_durumu),
    
    FOREIGN KEY (rezervasyon_id) REFERENCES rezervasyonlar(id) ON DELETE SET NULL,
    FOREIGN KEY (otel_id) REFERENCES oteller(id) ON DELETE SET NULL,
    FOREIGN KEY (kullanici_id) REFERENCES kullanicilar(id) ON DELETE SET NULL,
    FOREIGN KEY (partner_id) REFERENCES partner_detaylari(id) ON DELETE SET NULL,
    FOREIGN KEY (odeme_islem_id) REFERENCES odeme_islemleri(id) ON DELETE SET NULL,
    FOREIGN KEY (iptal_eden_admin_id) REFERENCES kullanicilar(id) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Tüm faturaların merkezi yönetimi';

CREATE TABLE komisyon_muhasebe_kayitlari (
    id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT PRIMARY KEY,
    kayit_no VARCHAR(30) NOT NULL UNIQUE,
    kayit_tarihi DATE NOT NULL,
    donem VARCHAR(7) NOT NULL COMMENT 'YYYY-MM formatında',
    
    -- İlişkiler
    rezervasyon_id BIGINT UNSIGNED NOT NULL,
    otel_id BIGINT UNSIGNED NOT NULL,
    partner_id BIGINT UNSIGNED NOT NULL,
    fatura_id BIGINT UNSIGNED NULL,
    
    -- Finansal Bilgiler
    toplam_rezervasyon_tutari DECIMAL(10,2) NOT NULL COMMENT 'Müşteriden tahsil edilen',
    komisyon_orani DECIMAL(5,2) NOT NULL,
    komisyon_tutari DECIMAL(10,2) NOT NULL,
    ek_kesintiler DECIMAL(10,2) DEFAULT 0.00 COMMENT 'Reklam, görünürlük programı vb.',
    net_otele_odenecek DECIMAL(10,2) NOT NULL,
    
    -- Ödeme Durumu
    otele_odeme_durumu ENUM('Beklemede', 'Ödeme Emri Oluşturuldu', 'Ödendi', 'Mahsuplaşıldı', 'Askıda') DEFAULT 'Beklemede',
    otele_odeme_tarihi DATE NULL,
    otele_odeme_referansi VARCHAR(50) NULL,
    odeme_emri_no VARCHAR(30) NULL,
    
    -- Muhasebe Hesapları
    muhasebe_hesap_kodu VARCHAR(20) NULL COMMENT '600.01.001 vb.',
    karsi_hesap_kodu VARCHAR(20) NULL,
    yevmiye_no VARCHAR(20) NULL,
    fis_no VARCHAR(20) NULL,
    
    -- Mutabakat
    mutabakat_durumu ENUM('Beklemede', 'Otele Gönderildi', 'Otel Onayladı', 'İtiraz Var', 'Çözüldü') DEFAULT 'Beklemede',
    mutabakat_gonderim_tarihi TIMESTAMP NULL,
    mutabakat_onay_tarihi TIMESTAMP NULL,
    mutabakat_notu TEXT NULL,
    
    -- İtiraz ve Düzeltme
    itiraz_var_mi TINYINT(1) DEFAULT 0,
    itiraz_nedeni VARCHAR(500) NULL,
    itiraz_tarihi TIMESTAMP NULL,
    itiraz_cozum_tarihi TIMESTAMP NULL,
    itiraz_cozum_aciklamasi TEXT NULL,
    duzeltme_tutari DECIMAL(10,2) NULL,
    
    -- Vergi
    stopaj_orani DECIMAL(5,2) DEFAULT 0.00,
    stopaj_tutari DECIMAL(10,2) DEFAULT 0.00,
    kdv_orani DECIMAL(5,2) DEFAULT 20.00,
    kdv_tutari DECIMAL(10,2) DEFAULT 0.00,
    
    -- Zaman Damgaları
    olusturulma_tarihi TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    guncellenme_tarihi TIMESTAMP NULL ON UPDATE CURRENT_TIMESTAMP,
    olusturan_admin_id BIGINT UNSIGNED NULL,
    onaylayan_finans_admin_id BIGINT UNSIGNED NULL,
    
    -- İndeksler
    INDEX idx_kayit_no (kayit_no),
    INDEX idx_donem (donem),
    INDEX idx_rezervasyon_id (rezervasyon_id),
    INDEX idx_otel_id (otel_id),
    INDEX idx_partner_id (partner_id),
    INDEX idx_otele_odeme_durumu (otele_odeme_durumu),
    INDEX idx_mutabakat_durumu (mutabakat_durumu),
    INDEX idx_kayit_tarihi (kayit_tarihi DESC),
    
    FOREIGN KEY (rezervasyon_id) REFERENCES rezervasyonlar(id) ON DELETE RESTRICT,
    FOREIGN KEY (otel_id) REFERENCES oteller(id) ON DELETE RESTRICT,
    FOREIGN KEY (partner_id) REFERENCES partner_detaylari(id) ON DELETE RESTRICT,
    FOREIGN KEY (fatura_id) REFERENCES faturalar(id) ON DELETE SET NULL,
    FOREIGN KEY (olusturan_admin_id) REFERENCES kullanicilar(id) ON DELETE SET NULL,
    FOREIGN KEY (onaylayan_finans_admin_id) REFERENCES kullanicilar(id) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Komisyon hesaplamaları ve muhasebe kayıtları';

CREATE TABLE mesaj_konusmalari (
    id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT PRIMARY KEY,
    konusma_kodu VARCHAR(20) NOT NULL UNIQUE COMMENT 'MSG-XXXXXXXX',
    
    -- İlişkiler
    rezervasyon_id BIGINT UNSIGNED NULL COMMENT 'Rezervasyon ile ilgiliyse',
    otel_id BIGINT UNSIGNED NOT NULL,
    
    -- Katılımcılar
    misafir_kullanici_id BIGINT UNSIGNED NOT NULL,
    otel_yetkilisi_kullanici_id BIGINT UNSIGNED NULL COMMENT 'Otel adına yanıt veren kişi',
    
    -- Konuşma Detayları
    konu_basligi VARCHAR(200) NOT NULL,
    konu_kategorisi ENUM('Rezervasyon', 'Özel İstek', 'İptal/Değişiklik', 'Ödeme', 'Giriş/Çıkış', 'Oda', 'Ulaşım', 'Diğer') DEFAULT 'Diğer',
    
    durum ENUM('Açık', 'Kapalı', 'Çözüldü', 'Spam', 'Arşivlendi') DEFAULT 'Açık',
    oncelik ENUM('Düşük', 'Normal', 'Yüksek', 'Acil') DEFAULT 'Normal',
    
    son_mesaj_tarihi TIMESTAMP NULL,
    son_mesaj_gonderen ENUM('Misafir', 'Otel', 'Sistem') NULL,
    son_mesaj_onizleme VARCHAR(100) NULL,
    
    -- Okunma Durumları
    misafir_okunmamis_sayisi INT UNSIGNED DEFAULT 0,
    otel_okunmamis_sayisi INT UNSIGNED DEFAULT 0,
    misafir_son_okuma_tarihi TIMESTAMP NULL,
    otel_son_okuma_tarihi TIMESTAMP NULL,
    
    -- Ek Bilgiler
    etiketler JSON NULL COMMENT '["önemli", "bekleyen"]',
    atanan_destek_ekibi_kullanici_id BIGINT UNSIGNED NULL COMMENT 'Platform desteği atandıysa',
    
    -- Zaman Damgaları
    olusturulma_tarihi TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    guncellenme_tarihi TIMESTAMP NULL ON UPDATE CURRENT_TIMESTAMP,
    kapatilma_tarihi TIMESTAMP NULL,
    kapatma_nedeni VARCHAR(255) NULL,
    
    -- İndeksler
    INDEX idx_konusma_kodu (konusma_kodu),
    INDEX idx_rezervasyon_id (rezervasyon_id),
    INDEX idx_otel_id (otel_id),
    INDEX idx_misafir_id (misafir_kullanici_id),
    INDEX idx_otel_yetkilisi (otel_yetkilisi_kullanici_id),
    INDEX idx_durum (durum),
    INDEX idx_oncelik (oncelik),
    INDEX idx_son_mesaj_tarihi (son_mesaj_tarihi DESC),
    INDEX idx_misafir_okunmamis (misafir_kullanici_id, misafir_okunmamis_sayisi),
    INDEX idx_otel_okunmamis (otel_id, otel_okunmamis_sayisi),
    
    FOREIGN KEY (rezervasyon_id) REFERENCES rezervasyonlar(id) ON DELETE SET NULL,
    FOREIGN KEY (otel_id) REFERENCES oteller(id) ON DELETE CASCADE,
    FOREIGN KEY (misafir_kullanici_id) REFERENCES kullanicilar(id) ON DELETE CASCADE,
    FOREIGN KEY (otel_yetkilisi_kullanici_id) REFERENCES kullanicilar(id) ON DELETE SET NULL,
    FOREIGN KEY (atanan_destek_ekibi_kullanici_id) REFERENCES kullanicilar(id) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Misafir-Otel arası mesajlaşma konuşmaları';

CREATE TABLE mesajlar (
    id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
    konusma_id BIGINT UNSIGNED NOT NULL,
    
    -- Gönderici Bilgisi
    gonderen_turu ENUM('Misafir', 'Otel', 'Sistem', 'Destek') NOT NULL,
    gonderen_kullanici_id BIGINT UNSIGNED NULL,
    gonderen_otel_id BIGINT UNSIGNED NULL,
    
    -- Mesaj İçeriği
    mesaj_metni TEXT NOT NULL,
    mesaj_tipi ENUM('Metin', 'Resim', 'Dosya', 'Konum', 'Teklif', 'Sistem Bildirimi') DEFAULT 'Metin',
    
    -- Medya İçerikleri
    medya_urls JSON NULL COMMENT '["url1", "url2"]',
    medya_tipleri JSON NULL COMMENT '["image/jpeg", "application/pdf"]',
    
    -- Özel Teklif (Otelden misafire fiyat teklifi)
    ozel_teklif_var_mi TINYINT(1) DEFAULT 0,
    teklif_tutari DECIMAL(10,2) NULL,
    teklif_para_birimi VARCHAR(3) NULL,
    teklif_gecerlilik_suresi DATETIME NULL,
    teklif_durumu ENUM('Beklemede', 'Kabul Edildi', 'Reddedildi', 'Süresi Doldu') NULL,
    teklif_kabul_tarihi TIMESTAMP NULL,
    
    -- Okunma Bilgileri
    okundu_mu TINYINT(1) DEFAULT 0,
    okunma_tarihi TIMESTAMP NULL,
    
    -- Durum
    durum ENUM('Gönderildi', 'İletildi', 'Okundu', 'Silindi', 'Spam') DEFAULT 'Gönderildi',
    
    -- IP ve Cihaz
    ip_adresi VARCHAR(45) NULL,
    cihaz_bilgisi VARCHAR(255) NULL,
    
    -- Zaman Damgası
    gonderim_tarihi TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    duzenlenme_tarihi TIMESTAMP NULL,
    silinme_tarihi TIMESTAMP NULL,
    
    -- İndeksler
    PRIMARY KEY (id, gonderim_tarihi),
    INDEX idx_konusma_id (konusma_id),
    INDEX idx_gonderen_kullanici (gonderen_kullanici_id),
    INDEX idx_gonderen_otel (gonderen_otel_id),
    INDEX idx_gonderim_tarihi (gonderim_tarihi DESC),
    INDEX idx_okundu (okundu_mu),
    INDEX idx_teklif_durumu (teklif_durumu),
    
    FOREIGN KEY (konusma_id) REFERENCES mesaj_konusmalari(id) ON DELETE CASCADE,
    FOREIGN KEY (gonderen_kullanici_id) REFERENCES kullanicilar(id) ON DELETE SET NULL,
    FOREIGN KEY (gonderen_otel_id) REFERENCES oteller(id) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Tüm mesajlar - Aylık partition zorunlu'
PARTITION BY RANGE (TO_DAYS(gonderim_tarihi)) (
    PARTITION p_2024_01 VALUES LESS THAN (TO_DAYS('2024-02-01')),
    PARTITION p_2024_02 VALUES LESS THAN (TO_DAYS('2024-03-01')),
    PARTITION p_2024_03 VALUES LESS THAN (TO_DAYS('2024-04-01')),
    PARTITION p_2024_04 VALUES LESS THAN (TO_DAYS('2024-05-01')),
    PARTITION p_2024_05 VALUES LESS THAN (TO_DAYS('2024-06-01')),
    PARTITION p_2024_06 VALUES LESS THAN (TO_DAYS('2024-07-01')),
    PARTITION p_future VALUES LESS THAN MAXVALUE
);

CREATE TABLE mesaj_sablonlari (
    id INT UNSIGNED NOT NULL AUTO_INCREMENT PRIMARY KEY,
    sablon_kodu VARCHAR(30) NOT NULL UNIQUE,
    sablon_adi VARCHAR(100) NOT NULL,
    
    -- Sahiplik
    otel_id BIGINT UNSIGNED NULL COMMENT 'Otele özel şablon',
    sistem_geneli_mi TINYINT(1) DEFAULT 0,
    
    -- Şablon İçeriği
    kategori ENUM('Hoş Geldin', 'Rezervasyon Onayı', 'Ödeme Hatırlatma', 'Giriş Bilgileri', 'Teşekkür', 'Özel Teklif', 'İptal', 'Diğer') NOT NULL,
    konu_basligi VARCHAR(200) NOT NULL,
    mesaj_icerigi TEXT NOT NULL,
    
    -- Değişkenler
    kullanilabilir_degiskenler JSON NULL COMMENT '["{misafir_adi}", "{giris_tarihi}", "{otel_adi}"]',
    
    -- Dil
    dil VARCHAR(5) DEFAULT 'tr',
    
    aktif_mi TINYINT(1) DEFAULT 1,
    olusturulma_tarihi TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    
    INDEX idx_otel_id (otel_id),
    INDEX idx_kategori (kategori),
    INDEX idx_dil (dil),
    INDEX idx_aktif (aktif_mi),
    
    FOREIGN KEY (otel_id) REFERENCES oteller(id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Hızlı mesaj şablonları';

CREATE TABLE sepet_blokajlari (
    id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
    blokaj_kodu VARCHAR(30) NOT NULL UNIQUE,
    
    -- İlişkiler
    otel_id BIGINT UNSIGNED NOT NULL,
    oda_tip_id BIGINT UNSIGNED NOT NULL,
    kullanici_id BIGINT UNSIGNED NULL COMMENT 'Giriş yapmamış kullanıcı için NULL olabilir',
    session_id VARCHAR(100) NOT NULL COMMENT 'PHP/Laravel session ID',
    
    -- Blokaj Detayları
    giris_tarihi DATE NOT NULL,
    cikis_tarihi DATE NOT NULL,
    oda_sayisi TINYINT UNSIGNED DEFAULT 1,
    yetiskin_sayisi TINYINT UNSIGNED NOT NULL,
    cocuk_sayisi TINYINT UNSIGNED DEFAULT 0,
    
    -- Fiyat Bilgisi (Blokaj anındaki)
    gecelik_fiyat DECIMAL(10,2) NOT NULL,
    toplam_tutar DECIMAL(10,2) NOT NULL,
    para_birimi VARCHAR(3) DEFAULT 'TRY',
    
    -- Durum
    durum ENUM('Aktif', 'Ödemeye Geçildi', 'Süresi Doldu', 'İptal Edildi', 'Rezervasyona Dönüştü') DEFAULT 'Aktif',
    rezervasyon_id BIGINT UNSIGNED NULL COMMENT 'Dönüşen rezervasyon ID',
    
    -- Süre
    blokaj_baslangic_tarihi TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    blokaj_bitis_tarihi TIMESTAMP NULL COMMENT 'Otomatik serbest kalma zamanı',
    sure_dakika SMALLINT UNSIGNED DEFAULT 15 COMMENT 'Blokaj süresi',
    
    -- Hatırlatma
    hatirlatma_gonderildi_mi TINYINT(1) DEFAULT 0,
    hatirlatma_gonderilme_tarihi TIMESTAMP NULL,
    
    -- IP
    ip_adresi VARCHAR(45) NULL,
    
    -- İndeksler
    PRIMARY KEY (id, blokaj_baslangic_tarihi),
    INDEX idx_blokaj_kodu (blokaj_kodu),
    INDEX idx_session_id (session_id),
    INDEX idx_otel_oda_tarih (otel_id, oda_tip_id, giris_tarihi),
    INDEX idx_kullanici_id (kullanici_id),
    INDEX idx_durum (durum),
    INDEX idx_bitis_tarihi (blokaj_bitis_tarihi),
    
    FOREIGN KEY (otel_id) REFERENCES oteller(id) ON DELETE CASCADE,
    FOREIGN KEY (oda_tip_id) REFERENCES oda_tipleri(id) ON DELETE CASCADE,
    FOREIGN KEY (kullanici_id) REFERENCES kullanicilar(id) ON DELETE SET NULL,
    FOREIGN KEY (rezervasyon_id) REFERENCES rezervasyonlar(id) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Ödeme öncesi geçici oda blokajları - Günlük partition önerilir'
PARTITION BY RANGE (TO_DAYS(blokaj_baslangic_tarihi)) (
    PARTITION p_2024_01 VALUES LESS THAN (TO_DAYS('2024-02-01')),
    PARTITION p_2024_02 VALUES LESS THAN (TO_DAYS('2024-03-01')),
    PARTITION p_2024_03 VALUES LESS THAN (TO_DAYS('2024-04-01')),
    PARTITION p_future VALUES LESS THAN MAXVALUE
);

CREATE TABLE bildirim_sablonlari (
    id SMALLINT UNSIGNED NOT NULL AUTO_INCREMENT PRIMARY KEY,
    sablon_kodu VARCHAR(50) NOT NULL UNIQUE,
    sablon_adi VARCHAR(100) NOT NULL,
    
    tur ENUM('E-posta', 'SMS', 'Push Notification', 'Sistem İçi') NOT NULL,
    
    -- Çoklu Dil Desteği
    dil VARCHAR(5) NOT NULL DEFAULT 'tr',
    
    konu VARCHAR(200) NULL COMMENT 'E-posta konusu',
    baslik VARCHAR(100) NULL COMMENT 'Push bildirim başlığı',
    icerik TEXT NOT NULL,
    
    -- Değişkenler
    degiskenler JSON NULL COMMENT '["{ad_soyad}", "{rezervasyon_no}", "{otel_adi}"]',
    
    aktif_mi TINYINT(1) DEFAULT 1,
    olusturulma_tarihi TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    
    UNIQUE KEY uk_kod_dil (sablon_kodu, dil),
    INDEX idx_tur (tur),
    INDEX idx_dil (dil),
    INDEX idx_aktif (aktif_mi)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Tüm bildirim şablonları';

CREATE TABLE kullanici_bildirim_cihazlari (
    id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT PRIMARY KEY,
    kullanici_id BIGINT UNSIGNED NOT NULL,
    
    cihaz_turu ENUM('iOS', 'Android', 'Web', 'Huawei') NOT NULL,
    cihaz_token VARCHAR(255) NOT NULL,
    cihaz_adi VARCHAR(100) NULL,
    cihaz_modeli VARCHAR(50) NULL,
    isletim_sistemi_surumu VARCHAR(20) NULL,
    uygulama_surumu VARCHAR(10) NULL,
    
    bildirim_izinleri JSON NULL COMMENT '{"rezervasyon": true, "kampanya": false, "mesaj": true}',
    
    son_kullanim_tarihi TIMESTAMP NULL,
    aktif_mi TINYINT(1) DEFAULT 1,
    
    olusturulma_tarihi TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    guncellenme_tarihi TIMESTAMP NULL ON UPDATE CURRENT_TIMESTAMP,
    son_bildirim_tarihi TIMESTAMP NULL,
    
    UNIQUE KEY uk_kullanici_token (kullanici_id, cihaz_token),
    INDEX idx_token (cihaz_token),
    INDEX idx_cihaz_turu (cihaz_turu),
    INDEX idx_aktif (aktif_mi),
    
    FOREIGN KEY (kullanici_id) REFERENCES kullanicilar(id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Push notification tokenları';

CREATE TABLE bildirim_loglari (
    id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
    kullanici_id BIGINT UNSIGNED NOT NULL,
    bildirim_sablon_id SMALLINT UNSIGNED NULL,
    
    tur ENUM('E-posta', 'SMS', 'Push Notification', 'Sistem İçi') NOT NULL,
    
    alici_eposta VARCHAR(100) NULL,
    alici_telefon VARCHAR(20) NULL,
    cihaz_token VARCHAR(255) NULL,
    
    konu VARCHAR(200) NULL,
    icerik TEXT NOT NULL,
    gonderilen_icerik TEXT NULL COMMENT 'Değişkenler işlenmiş son hali',
    
    durum ENUM('Beklemede', 'Gönderildi', 'İletildi', 'Okundu', 'Başarısız', 'İptal Edildi') DEFAULT 'Beklemede',
    
    saglayici ENUM('SMTP', 'SendGrid', 'Amazon SES', 'Netgsm', 'Telsam', 'Firebase', 'APNS', 'OneSignal') NULL,
    saglayici_mesaj_id VARCHAR(100) NULL,
    hata_kodu VARCHAR(20) NULL,
    hata_mesaji VARCHAR(500) NULL,
    
    gonderme_denemesi TINYINT UNSIGNED DEFAULT 1,
    maksimum_deneme TINYINT UNSIGNED DEFAULT 3,
    
    gonderim_tarihi TIMESTAMP NULL,
    okunma_tarihi TIMESTAMP NULL,
    olusturulma_tarihi TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    
    -- İlişkili Kayıt
    ilgili_tablo VARCHAR(50) NULL COMMENT 'rezervasyonlar, mesajlar',
    ilgili_kayit_id BIGINT UNSIGNED NULL,
    
    PRIMARY KEY (id, olusturulma_tarihi),
    INDEX idx_kullanici_id (kullanici_id),
    INDEX idx_tur (tur),
    INDEX idx_durum (durum),
    INDEX idx_gonderim_tarihi (gonderim_tarihi DESC),
    INDEX idx_ilgili (ilgili_tablo, ilgili_kayit_id),
    
    FOREIGN KEY (kullanici_id) REFERENCES kullanicilar(id) ON DELETE CASCADE,
    FOREIGN KEY (bildirim_sablon_id) REFERENCES bildirim_sablonlari(id) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Tüm bildirim gönderim logları - Aylık partition zorunlu'
PARTITION BY RANGE (TO_DAYS(olusturulma_tarihi)) (
    PARTITION p_2024_01 VALUES LESS THAN (TO_DAYS('2024-02-01')),
    PARTITION p_2024_02 VALUES LESS THAN (TO_DAYS('2024-03-01')),
    PARTITION p_2024_03 VALUES LESS THAN (TO_DAYS('2024-04-01')),
    PARTITION p_future VALUES LESS THAN MAXVALUE
);

CREATE TABLE sistem_ici_bildirimler (
    id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT PRIMARY KEY,
    kullanici_id BIGINT UNSIGNED NOT NULL,
    
    bildirim_turu ENUM('Rezervasyon', 'Mesaj', 'Ödeme', 'Kampanya', 'Sistem', 'Hatırlatma', 'Uyarı', 'Başarı') NOT NULL,
    
    baslik VARCHAR(100) NOT NULL,
    mesaj TEXT NOT NULL,
    
    ikon VARCHAR(50) NULL,
    renk VARCHAR(20) NULL DEFAULT '#007bff',
    
    -- Aksiyon Butonu
    aksiyon_url VARCHAR(500) NULL,
    aksiyon_metni VARCHAR(50) NULL,
    
    -- Okunma
    okundu_mu TINYINT(1) DEFAULT 0,
    okunma_tarihi TIMESTAMP NULL,
    arsivlendi_mi TINYINT(1) DEFAULT 0,
    
    -- Önem
    onem_derecesi ENUM('Düşük', 'Normal', 'Yüksek', 'Kritik') DEFAULT 'Normal',
    
    -- İlişki
    ilgili_tablo VARCHAR(50) NULL,
    ilgili_kayit_id BIGINT UNSIGNED NULL,
    
    -- Geçerlilik
    gecerlilik_baslangic TIMESTAMP NULL,
    gecerlilik_bitis TIMESTAMP NULL,
    
    olusturulma_tarihi TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    
    INDEX idx_kullanici_id (kullanici_id),
    INDEX idx_okundu (kullanici_id, okundu_mu),
    INDEX idx_tur (bildirim_turu),
    INDEX idx_olusturulma (olusturulma_tarihi DESC),
    INDEX idx_onem (onem_derecesi),
    INDEX idx_ilgili (ilgili_tablo, ilgili_kayit_id),
    
    FOREIGN KEY (kullanici_id) REFERENCES kullanicilar(id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Kullanıcıların sistem içi bildirimleri';

CREATE TABLE sistem_hata_loglari (
    id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
    
    hata_seviyesi ENUM('DEBUG', 'INFO', 'NOTICE', 'WARNING', 'ERROR', 'CRITICAL', 'ALERT', 'EMERGENCY') NOT NULL,
    hata_kodu VARCHAR(20) NULL,
    hata_mesaji TEXT NOT NULL,
    hata_detayi LONGTEXT NULL COMMENT 'Stack trace, context',
    
    -- Kaynak
    dosya_yolu VARCHAR(500) NULL,
    satir_no INT UNSIGNED NULL,
    fonksiyon_adi VARCHAR(100) NULL,
    sinif_adi VARCHAR(100) NULL,
    
    -- İstek Bilgileri
    url VARCHAR(2000) NULL,
    http_method VARCHAR(10) NULL,
    ip_adresi VARCHAR(45) NULL,
    user_agent TEXT NULL,
    referer VARCHAR(2000) NULL,
    
    -- Kullanıcı
    kullanici_id BIGINT UNSIGNED NULL,
    session_id VARCHAR(100) NULL,
    request_id VARCHAR(36) NULL COMMENT 'İstek takibi için UUID',
    
    -- Ek Veri
    request_verisi JSON NULL COMMENT 'POST/GET parametreleri (hassas veriler maskelenmiş)',
    response_verisi JSON NULL,
    ek_bilgiler JSON NULL,
    
    -- Zaman
    olusma_tarihi TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    
    -- Durum
    cozuldu_mu TINYINT(1) DEFAULT 0,
    cozulme_tarihi TIMESTAMP NULL,
    cozen_admin_id BIGINT UNSIGNED NULL,
    cozum_notu TEXT NULL,
    
    PRIMARY KEY (id, olusma_tarihi),
    INDEX idx_hata_seviyesi (hata_seviyesi),
    INDEX idx_hata_kodu (hata_kodu),
    INDEX idx_url (url(255)),
    INDEX idx_kullanici_id (kullanici_id),
    INDEX idx_ip (ip_adresi),
    INDEX idx_olusma_tarihi (olusma_tarihi DESC),
    INDEX idx_request_id (request_id),
    
    FOREIGN KEY (kullanici_id) REFERENCES kullanicilar(id) ON DELETE SET NULL,
    FOREIGN KEY (cozen_admin_id) REFERENCES kullanicilar(id) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Sistem hataları ve exception logları - Günlük partition önerilir'
PARTITION BY RANGE (TO_DAYS(olusma_tarihi)) (
    PARTITION p_2024_01_01 VALUES LESS THAN (TO_DAYS('2024-01-02')),
    PARTITION p_2024_01_02 VALUES LESS THAN (TO_DAYS('2024-01-03')),
    PARTITION p_2024_01_03 VALUES LESS THAN (TO_DAYS('2024-01-04')),
    PARTITION p_future VALUES LESS THAN MAXVALUE
);

CREATE TABLE kullanici_aktivite_loglari (
    id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
    kullanici_id BIGINT UNSIGNED NOT NULL,
    
    aktivite_turu ENUM(
        'Giriş', 'Çıkış', 'Başarısız Giriş', 'Şifre Değiştirme', 'Şifre Sıfırlama',
        'Profil Güncelleme', 'E-posta Doğrulama', 'Telefon Doğrulama',
        'Rezervasyon Oluşturma', 'Rezervasyon İptal', 'Rezervasyon Görüntüleme',
        'Ödeme Başlatma', 'Ödeme Tamamlama', 'Ödeme Başarısız',
        'Mesaj Gönderme', 'Mesaj Okuma',
        'Yorum Yazma', 'Yorum Düzenleme', 'Yorum Silme',
        'Otel Favorilere Ekleme', 'Otel Favorilerden Çıkarma',
        'Arama Yapma', 'Filtreleme',
        'Dosya Yükleme', 'Dosya Silme',
        'Hesap Silme Talebi', 'KVKK Onayı', 'KVKK Reddi'
    ) NOT NULL,
    
    aktivite_detayi JSON NULL COMMENT '{"otel_id": 123, "arama_kriterleri": {...}}',
    
    -- Cihaz ve Konum
    ip_adresi VARCHAR(45) NOT NULL,
    user_agent TEXT NULL,
    cihaz_turu ENUM('Mobil', 'Tablet', 'Masaüstü', 'Bot', 'Bilinmiyor') DEFAULT 'Bilinmiyor',
    isletim_sistemi VARCHAR(50) NULL,
    tarayici VARCHAR(50) NULL,
    ulke VARCHAR(50) NULL,
    sehir VARCHAR(50) NULL,
    
    -- Oturum
    session_id VARCHAR(100) NULL,
    
    -- Durum
    basarili_mi TINYINT(1) DEFAULT 1,
    hata_nedeni VARCHAR(255) NULL,
    
    olusma_tarihi TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    
    PRIMARY KEY (id, olusma_tarihi),
    INDEX idx_kullanici_id (kullanici_id),
    INDEX idx_aktivite_turu (aktivite_turu),
    INDEX idx_ip (ip_adresi),
    INDEX idx_olusma_tarihi (olusma_tarihi DESC),
    INDEX idx_basarili (basarili_mi),
    
    FOREIGN KEY (kullanici_id) REFERENCES kullanicilar(id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Kullanıcıların tüm aktiviteleri - Aylık partition zorunlu'
PARTITION BY RANGE (TO_DAYS(olusma_tarihi)) (
    PARTITION p_2024_01 VALUES LESS THAN (TO_DAYS('2024-02-01')),
    PARTITION p_2024_02 VALUES LESS THAN (TO_DAYS('2024-03-01')),
    PARTITION p_2024_03 VALUES LESS THAN (TO_DAYS('2024-04-01')),
    PARTITION p_future VALUES LESS THAN MAXVALUE
);

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
    
    PRIMARY KEY (id, islem_tarihi),
    INDEX idx_admin_id (admin_kullanici_id),
    INDEX idx_islem_turu (islem_turu),
    INDEX idx_hedef (hedef_tablo, hedef_kayit_id),
    INDEX idx_islem_tarihi (islem_tarihi DESC),
    INDEX idx_ip (ip_adresi),
    
    FOREIGN KEY (admin_kullanici_id) REFERENCES kullanicilar(id) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Admin panelinde yapılan tüm işlemlerin kaydı - Aylık partition'
PARTITION BY RANGE (TO_DAYS(islem_tarihi)) (
    PARTITION p_2024_01 VALUES LESS THAN (TO_DAYS('2024-02-01')),
    PARTITION p_2024_02 VALUES LESS THAN (TO_DAYS('2024-03-01')),
    PARTITION p_2024_03 VALUES LESS THAN (TO_DAYS('2024-04-01')),
    PARTITION p_future VALUES LESS THAN MAXVALUE
);

CREATE TABLE api_loglari (
    id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
    request_id VARCHAR(36) NOT NULL,
    
    -- API Bilgileri
    api_versiyonu VARCHAR(10) NULL,
    endpoint VARCHAR(500) NOT NULL,
    http_method VARCHAR(10) NOT NULL,
    
    -- İstek
    request_headers JSON NULL,
    request_body JSON NULL,
    request_ip VARCHAR(45) NULL,
    user_agent TEXT NULL,
    
    -- Yanıt
    response_status SMALLINT UNSIGNED NULL,
    response_headers JSON NULL,
    response_body JSON NULL,
    response_size INT UNSIGNED NULL,
    
    -- Kimlik
    kullanici_id BIGINT UNSIGNED NULL,
    api_key_id INT UNSIGNED NULL,
    partner_id BIGINT UNSIGNED NULL,
    
    -- Performans
    islem_suresi_ms INT UNSIGNED NULL COMMENT 'Milisaniye',
    bellek_kullanimi_kb INT UNSIGNED NULL,
    
    -- Durum
    basarili_mi TINYINT(1) DEFAULT 1,
    hata_mesaji TEXT NULL,
    hata_kodu VARCHAR(20) NULL,
    
    baslangic_tarihi TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    bitis_tarihi TIMESTAMP NULL,
    
    PRIMARY KEY (id, baslangic_tarihi),
    UNIQUE KEY uk_request_id (request_id, baslangic_tarihi),
    INDEX idx_endpoint (endpoint(255)),
    INDEX idx_method (http_method),
    INDEX idx_kullanici_id (kullanici_id),
    INDEX idx_partner_id (partner_id),
    INDEX idx_response_status (response_status),
    INDEX idx_basarili (basarili_mi),
    INDEX idx_sure (islem_suresi_ms),
    INDEX idx_tarih (baslangic_tarihi DESC),
    
    FOREIGN KEY (kullanici_id) REFERENCES kullanicilar(id) ON DELETE SET NULL,
    FOREIGN KEY (partner_id) REFERENCES partner_detaylari(id) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Tüm API çağrılarının loglanması - Günlük partition zorunlu'
PARTITION BY RANGE (TO_DAYS(baslangic_tarihi)) (
    PARTITION p_2024_01_01 VALUES LESS THAN (TO_DAYS('2024-01-02')),
    PARTITION p_2024_01_02 VALUES LESS THAN (TO_DAYS('2024-01-03')),
    PARTITION p_2024_01_03 VALUES LESS THAN (TO_DAYS('2024-01-04')),
    PARTITION p_future VALUES LESS THAN MAXVALUE
);



INSERT INTO roller (rol_kodu, rol_adi, departman, seviye, varsayilan_mi, aciklama) VALUES
('super_admin', 'Süper Yönetici', 'Yönetim', 99, 0, 'Sunucu ayarları dahil her şeye erişim'),
('genel_mudur', 'Genel Müdür', 'Yönetim', 95, 0, 'Tüm raporlara ve finansal özetlere erişim'),
('genel_mudur_yardimcisi', 'Genel Müdür Yardımcısı', 'Yönetim', 90, 0, 'Operasyonel genel yetkili');

INSERT INTO roller (rol_kodu, rol_adi, departman, seviye, varsayilan_mi, aciklama) VALUES
('finans_direktoru', 'Finans Direktörü', 'Finans', 85, 0, 'Tüm mali işlemler ve raporlar'),
('finans_yoneticisi', 'Finans Yöneticisi', 'Finans', 80, 0, 'Ödeme onayları, iade işlemleri'),
('muhasebe_sefi', 'Muhasebe Şefi', 'Finans', 75, 0, 'Cari hesap mutabakatları'),
('muhasebe_uzmani', 'Muhasebe Uzmanı', 'Finans', 70, 0, 'Fatura kesme, ödeme takibi'),
('muhasebe_yardimcisi', 'Muhasebe Yardımcısı', 'Finans', 65, 0, 'Veri girişi, dekont eşleştirme'),
('finansal_raporlama_uzmani', 'Finansal Raporlama Uzmanı', 'Finans', 72, 0, 'İleri seviye raporlama ve analiz');

INSERT INTO roller (rol_kodu, rol_adi, departman, seviye, varsayilan_mi, aciklama) VALUES
('operasyon_direktoru', 'Operasyon Direktörü', 'Operasyon', 80, 0, 'Tüm otel operasyonlarından sorumlu'),
('operasyon_yoneticisi', 'Operasyon Yöneticisi', 'Operasyon', 75, 0, 'Otel entegrasyonları ve sorun çözme'),
('otel_iliskileri_yoneticisi', 'Otel İlişkileri Yöneticisi', 'Operasyon', 72, 0, 'Partner otellerle günlük iletişim'),
('otel_iliskileri_uzmani', 'Otel İlişkileri Uzmanı', 'Operasyon', 68, 0, 'Otel talepleri ve sorun takibi'),
('icerik_yoneticisi', 'İçerik Yöneticisi', 'Operasyon', 65, 0, 'Otel sayfaları içerik ve görsel onay'),
('icerik_editoru', 'İçerik Editörü', 'Operasyon', 60, 0, 'Metin düzenleme, çeviri kontrol'),
('fotograf_onay_sorumlusu', 'Fotoğraf Onay Sorumlusu', 'Operasyon', 60, 0, 'Otel görsellerinin kalite kontrolü'),
('extranet_destek_uzmani', 'Extranet Destek Uzmanı', 'Operasyon', 62, 0, 'Partner paneli teknik desteği');

INSERT INTO roller (rol_kodu, rol_adi, departman, seviye, varsayilan_mi, aciklama) VALUES
('destek_direktoru', 'Destek Direktörü', 'Destek', 75, 0, 'Müşteri hizmetleri yönetimi'),
('destek_yoneticisi', 'Destek Yöneticisi', 'Destek', 70, 0, 'Vardiya ve ekip yönetimi'),
('kdemli_destek_uzmani', 'Kıdemli Destek Uzmanı', 'Destek', 60, 0, 'Zorlu vakalar ve iade onayları'),
('destek_uzmani', 'Destek Uzmanı', 'Destek', 50, 0, 'Canlı destek, e-posta, çağrı'),
('destek_asistani', 'Destek Asistanı', 'Destek', 45, 0, 'Stajyer veya yeni başlayan'),
('sosyal_medya_destek', 'Sosyal Medya Destek', 'Destek', 48, 0, 'Sosyal medya kanallarından gelen talepler'),
('iade_ve_anlasmazlik_uzmani', 'İade ve Anlaşmazlık Uzmanı', 'Destek', 65, 0, 'İptal/iade talepleri değerlendirme');

INSERT INTO roller (rol_kodu, rol_adi, departman, seviye, varsayilan_mi, aciklama) VALUES
('cto', 'Teknoloji Direktörü', 'IT', 95, 0, 'Tüm teknoloji altyapısından sorumlu'),
('teknik_lider', 'Teknik Lider', 'IT', 85, 0, 'Yazılım ekibi teknik yönetimi'),
('kidemli_yazilimci', 'Kıdemli Yazılım Uzmanı', 'IT', 80, 0, 'Tam yetkili geliştirici'),
('yazilim_uzmani', 'Yazılım Uzmanı', 'IT', 70, 0, 'Backend/Frontend geliştirici'),
('stajyer_yazilimci', 'Stajyer Yazılımcı', 'IT', 40, 0, 'Sınırlı erişimli geliştirici'),
('sistem_yoneticisi', 'Sistem Yöneticisi', 'IT', 85, 0, 'Sunucu ve altyapı yönetimi'),
('veritabani_yoneticisi', 'Veritabanı Yöneticisi', 'IT', 88, 0, 'Veritabanı optimizasyon ve bakım'),
('devops_muhendisi', 'DevOps Mühendisi', 'IT', 82, 0, 'CI/CD ve deployment yönetimi'),
('guvenlik_uzmani', 'Siber Güvenlik Uzmanı', 'IT', 90, 0, 'Güvenlik açıkları ve sızma testleri'),
('test_muhendisi', 'Test Mühendisi', 'IT', 65, 0, 'QA ve otomasyon testleri');

INSERT INTO roller (rol_kodu, rol_adi, departman, seviye, varsayilan_mi, aciklama) VALUES
('hukuk_direktoru', 'Hukuk Direktörü', 'Hukuk', 90, 0, 'Tüm hukuki süreçler'),
('hukuk_musaviri', 'Hukuk Müşaviri', 'Hukuk', 85, 0, 'Sözleşme ve uyuşmazlıklar'),
('veri_koruma_gorevlisi', 'KVKK / GDPR Uzmanı', 'Hukuk', 88, 0, 'Kişisel verilerin korunması'),
('uyumluluk_uzmani', 'Uyumluluk Uzmanı', 'Hukuk', 80, 0, 'Mevzuat ve politika uyumluluğu');

INSERT INTO roller (rol_kodu, rol_adi, departman, seviye, varsayilan_mi, aciklama) VALUES
('ik_direktoru', 'İK Direktörü', 'İnsan Kaynakları', 80, 0, 'İnsan kaynakları yönetimi'),
('ik_yoneticisi', 'İK Yöneticisi', 'İnsan Kaynakları', 75, 0, 'İşe alım ve performans'),
('ik_uzmani', 'İK Uzmanı', 'İnsan Kaynakları', 70, 0, 'Özlük işleri ve bordro'),
('egitim_koordinatoru', 'Eğitim Koordinatörü', 'İnsan Kaynakları', 68, 0, 'Çalışan eğitimleri organizasyonu');

INSERT INTO yetkiler (yetki_kodu, modul, eylem, aciklama, varsayilan_izin) VALUES
-- Otel Modülü
('otel.listele', 'Otel', 'listele', 'Otelleri listeleme', 1),
('otel.goruntule', 'Otel', 'goruntule', 'Otel detay sayfası görüntüleme', 1),
('otel.ekle', 'Otel', 'ekle', 'Yeni otel ekleme', 0),
('otel.duzenle', 'Otel', 'duzenle', 'Otel bilgilerini düzenleme', 0),
('otel.sil', 'Otel', 'sil', 'Otel silme (soft delete)', 0),
('otel.onayla', 'Otel', 'onayla', 'Otel yayına alma onayı', 0),
('otel.yorum.sil', 'Otel', 'sil', 'Otel yorumu silme', 0),

-- Finans Modülü
('finans.komisyon.gor', 'Finans', 'goruntule', 'Komisyon oranlarını görme', 0),
('finans.komisyon.duzenle', 'Finans', 'duzenle', 'Komisyon oranı değiştirme', 0),
('finans.odeme.onayla', 'Finans', 'onayla', 'Partnere ödeme onayı verme', 0),
('finans.fatura.kes', 'Finans', 'ekle', 'Fatura kesme yetkisi', 0),
('finans.rapor.gor', 'Finans', 'goruntule', 'Finansal raporları görüntüleme', 0),
('finans.iade.onayla', 'Finans', 'onayla', 'İade taleplerini onaylama', 0),

-- Rezervasyon Modülü
('rezervasyon.listele', 'Rezervasyon', 'listele', 'Tüm rezervasyonları listeleme', 0),
('rezervasyon.iptal.et', 'Rezervasyon', 'sil', 'Rezervasyon iptal etme', 0),
('rezervasyon.tarih.degistir', 'Rezervasyon', 'duzenle', 'Rezervasyon tarihi değiştirme', 0),

-- Sistem Modülü
('sistem.ayarlar.gor', 'Sistem', 'goruntule', 'Sistem ayarlarını görme', 0),
('sistem.ayarlar.duzenle', 'Sistem', 'duzenle', 'Sistem ayarlarını değiştirme', 0),
('sistem.log.gor', 'Sistem', 'goruntule', 'Sistem loglarını görüntüleme', 0),
('sistem.kullanici.rol.ata', 'Sistem', 'duzenle', 'Kullanıcıya rol atama', 0);

INSERT INTO rol_yetkileri (rol_id, yetki_id, izin_var)
SELECT (SELECT id FROM roller WHERE rol_kodu = 'super_admin'), id, 1 FROM yetkiler;

INSERT INTO departmanlar (departman_kodu, departman_adi, yonetici_rol_id, aciklama) VALUES
('YK', 'Yönetim Kurulu', (SELECT id FROM roller WHERE rol_kodu = 'super_admin'), 'En üst karar organı'),
('GM', 'Genel Müdürlük', (SELECT id FROM roller WHERE rol_kodu = 'genel_mudur'), 'İcra kurulu başkanlığı'),
('FIN', 'Finans ve Muhasebe', (SELECT id FROM roller WHERE rol_kodu = 'finans_direktoru'), 'Tüm mali işlemler'),
('SAT', 'Satış ve İş Geliştirme', (SELECT id FROM roller WHERE rol_kodu = 'satis_direktoru'), 'Otel kazanımı ve satış'),
('OPS', 'Operasyon', (SELECT id FROM roller WHERE rol_kodu = 'operasyon_direktoru'), 'Günlük operasyon yönetimi'),
('DESTEK', 'Müşteri Hizmetleri', (SELECT id FROM roller WHERE rol_kodu = 'destek_direktoru'), '7/24 destek hizmetleri'),
('PAZ', 'Pazarlama', (SELECT id FROM roller WHERE rol_kodu = 'pazarlama_direktoru'), 'Marka ve dijital pazarlama'),
('IT', 'Bilgi Teknolojileri', (SELECT id FROM roller WHERE rol_kodu = 'cto'), 'Yazılım ve altyapı'),
('HUK', 'Hukuk ve Uyumluluk', (SELECT id FROM roller WHERE rol_kodu = 'hukuk_direktoru'), 'Hukuki işler ve KVKK'),
('IK', 'İnsan Kaynakları', (SELECT id FROM roller WHERE rol_kodu = 'ik_direktoru'), 'Personel yönetimi'),
('DIS', 'Dış Paydaşlar', NULL, 'Oteller, acenteler, misafirler');

INSERT INTO partner_detaylari (
    kullanici_id, firma_unvani, firma_turu, vergi_dairesi, vergi_numarasi,
    fatura_adresi, fatura_il, fatura_ilce, yetkili_ad_soyad, yetkili_tc_no,
    yetkili_telefon, yetkili_eposta, banka_adi, iban, hesap_sahibi_adi,
    onay_durumu, onay_tarihi
) VALUES (
    1, 'Lüks Otelcilik A.Ş.', 'Anonim Şirketi', 'Turizm Vergi Dairesi', '1234567890',
    'Lara Cad. No:123 Muratpaşa', 'Antalya', 'Muratpaşa', 'Ahmet Yılmaz', '12345678901',
    '+905301234567', 'ahmet@luksotel.com', 'İş Bankası', 'TR320006200123456789000123', 'Lüks Otelcilik A.Ş.',
    'Onaylandi', NOW()
);

INSERT INTO oteller (
    otel_kodu, partner_id, otel_adi, otel_turu, yildiz_sayisi,
    sehir, ilce, tam_adres, enlem, boylam,
    telefon_1, eposta, toplam_oda_sayisi,
    varsayilan_komisyon_orani, komisyon_turu, odeme_vadesi,
    yayin_durumu, onay_durumu, kisa_aciklama
) VALUES (
    'ANT-LUX-001', 1, 'Lüks Resort & Spa', 'Otel', 5,
    'Antalya', 'Muratpaşa', 'Lara Cad. No:123 Muratpaşa/Antalya', 36.8512, 30.7689,
    '+902421234567', 'info@luksresort.com', 250,
    15.00, 'sabit_oran', 'Çıkış Günü',
    'Yayında', 'Onaylandı', 'Lara sahiline 100 metre mesafede, her şey dahil konseptli lüks resort otel.'
);

INSERT INTO otel_ozellik_kategorileri (kategori_adi, kategori_ikon, siralama) VALUES
('Genel', 'fa-building', 1),
('Oda Özellikleri', 'fa-bed', 2),
('Banyo', 'fa-bath', 3),
('İnternet / Teknoloji', 'fa-wifi', 4),
('Yeme & İçme', 'fa-utensils', 5),
('Havuz & SPA', 'fa-swimming-pool', 6),
('Spor & Eğlence', 'fa-futbol', 7),
('Aile & Çocuk', 'fa-child', 8),
('Ulaşım & Otopark', 'fa-car', 9),
('Resepsiyon Hizmetleri', 'fa-concierge-bell', 10),
('Temizlik Hizmetleri', 'fa-broom', 11),
('İş İmkanları', 'fa-briefcase', 12),
('Engelli Dostu', 'fa-wheelchair', 13),
('Evcil Hayvan', 'fa-dog', 14),
('Güvenlik', 'fa-shield-alt', 15),
('Sağlık Önlemleri', 'fa-thermometer-half', 16);

INSERT INTO otel_ozellikleri (kategori_id, ozellik_adi, ozellik_ikon, one_cikan_ozellik, siralama) VALUES
-- Genel
(1, '24 Saat Resepsiyon', 'fa-clock', 1, 1),
(1, 'Klima', 'fa-wind', 1, 2),
(1, 'Isıtma', 'fa-temperature-high', 1, 3),
(1, 'Ses Yalıtımlı Odalar', 'fa-volume-mute', 0, 4),
(1, 'Sigara İçilmeyen Odalar', 'fa-smoking-ban', 1, 5),
(1, 'Aile Odaları', 'fa-users', 1, 6),
(1, 'Asansör', 'fa-elevator', 1, 7),
(1, 'Valiz Odası', 'fa-suitcase', 0, 8),

-- Havuz & SPA
(6, 'Açık Yüzme Havuzu', 'fa-swimming-pool', 1, 1),
(6, 'Kapalı Yüzme Havuzu', 'fa-water', 1, 2),
(6, 'Çocuk Havuzu', 'fa-child', 1, 3),
(6, 'Isıtmalı Havuz', 'fa-temperature-high', 0, 4),
(6, 'Tuzlu Su Havuzu', 'fa-water', 0, 5),
(6, 'SPA ve Sağlık Merkezi', 'fa-spa', 1, 10),
(6, 'Sauna', 'fa-hot-tub', 1, 11),
(6, 'Hamam', 'fa-mosque', 1, 12),
(6, 'Buhar Odası', 'fa-wind', 0, 13),
(6, 'Jakuzi', 'fa-hot-tub', 0, 14),
(6, 'Masaj Hizmetleri', 'fa-hands', 1, 15),

-- İnternet / Teknoloji
(4, 'Ücretsiz WiFi', 'fa-wifi', 1, 1),
(4, 'Odalarda WiFi', 'fa-wifi', 1, 2),
(4, 'Ortak Alanlarda WiFi', 'fa-wifi', 0, 3),

-- Yeme & İçme
(5, 'Restoran', 'fa-utensils', 1, 1),
(5, 'Bar', 'fa-glass-cheers', 1, 2),
(5, 'Açık Büfe Kahvaltı', 'fa-coffee', 1, 3),
(5, 'Kontinental Kahvaltı', 'fa-bread-slice', 0, 4),
(5, 'Oda Kahvaltısı', 'fa-coffee', 0, 5),
(5, 'Snack Bar', 'fa-hamburger', 0, 6),
(5, 'Havuz Başı Bar', 'fa-cocktail', 0, 7),
(5, 'Özel Diyet Menüleri', 'fa-carrot', 0, 8),

-- Spor & Eğlence
(7, 'Fitness Merkezi', 'fa-dumbbell', 1, 1),
(7, 'Tenis Kortu', 'fa-table-tennis', 0, 2),
(7, 'Bilardo', 'fa-dice', 0, 3),
(7, 'Masa Tenisi', 'fa-table-tennis', 0, 4),
(7, 'Animasyon Ekibi', 'fa-music', 1, 5),
(7, 'Gece Eğlencesi / DJ', 'fa-music', 0, 6),
(7, 'Su Sporları', 'fa-water', 0, 7),

-- Otopark
(9, 'Ücretsiz Otopark', 'fa-parking', 1, 1),
(9, 'Ücretli Otopark', 'fa-parking', 0, 2),
(9, 'Vale Hizmeti', 'fa-car', 0, 3),
(9, 'Elektrikli Araç Şarj İstasyonu', 'fa-charging-station', 1, 4),
(9, 'Engelli Otoparkı', 'fa-wheelchair', 0, 5),

-- Engelli Dostu
(13, 'Tekerlekli Sandalye Erişimi', 'fa-wheelchair', 1, 1),
(13, 'Engelli Odası', 'fa-wheelchair', 1, 2),
(13, 'Engelli Banyosu', 'fa-wheelchair', 0, 3),
(13, 'Görme Engelliler İçin İşaretler', 'fa-eye-slash', 0, 4),

-- Evcil Hayvan
(14, 'Evcil Hayvan Kabul Edilir', 'fa-dog', 1, 1),
(14, 'Evcil Hayvan Mama Kabı', 'fa-bone', 0, 2),
(14, 'Evcil Hayvan Yatağı', 'fa-bed', 0, 3),
(14, 'Evcil Hayvan Ücretlidir', 'fa-coins', 0, 4);

INSERT INTO otel_ozellik_iliskileri (otel_id, ozellik_id, ek_ucret) VALUES
(1, 1, NULL),   -- 24 Saat Resepsiyon
(1, 2, NULL),   -- Klima
(1, 8, NULL),   -- Açık Yüzme Havuzu
(1, 13, NULL),  -- SPA ve Sağlık Merkezi
(1, 14, NULL),  -- Sauna
(1, 15, 500.00), -- Hamam (ücretli)
(1, 16, NULL),  -- Masaj Hizmetleri
(1, 18, NULL),  -- Ücretsiz WiFi
(1, 21, NULL),  -- Restoran
(1, 32, NULL);

INSERT INTO otel_kosullari (
    otel_id, sigara_politikasi, evcil_hayvan_politikasi, evcil_hayvan_ucreti,
    minimum_yas_siniri, cocuk_kabul_yas_araligi, bebek_karyolasi_var_mi,
    ekstra_yatak_var_mi, ekstra_yatak_ucreti, on_odeme_orani,
    ucretsiz_iptal_suresi, hasar_depozitosu_tutari
) VALUES (
    1, 'Sadece belirli alanlarda serbest', 'Ücretli kabul edilir', 250.00,
    18, '0-12', 1,
    1, 750.00, 30.00,
    7, 1000.00
);

INSERT INTO oda_tipleri (
    otel_id, oda_tip_kodu, oda_adi, oda_kategorisi,
    maksimum_kisi_sayisi, maksimum_yetiskin_sayisi, maksimum_cocuk_sayisi,
    yatak_tipi, yatak_sayisi, oda_metrekare, manzara_tipi,
    standart_gecelik_fiyat, toplam_oda_sayisi
) VALUES
(1, 'STD-01', 'Standart Oda', 'Standart', 2, 2, 0, 'Çift Kişilik', 1, 28, 'Bahçe', 2500.00, 100),
(1, 'DLX-01', 'Deluxe Oda Deniz Manzaralı', 'Deluxe', 3, 2, 1, 'Queen Size', 1, 35, 'Deniz', 4500.00, 80),
(1, 'SUI-01', 'Junior Suite', 'Junior Suite', 4, 2, 2, 'King Size', 2, 55, 'Deniz', 7500.00, 40),
(1, 'FAM-01', 'Aile Odası', 'Aile Odası', 5, 2, 3, 'Queen Size + Ranza', 2, 65, 'Havuz', 6000.00, 30);

INSERT INTO oda_ozellikleri (kategori, ozellik_adi, ozellik_ikon) VALUES
-- Genel
('Genel', 'Klima', 'fa-wind'),
('Genel', 'Isıtma', 'fa-temperature-high'),
('Genel', 'Ses Yalıtımı', 'fa-volume-mute'),
('Genel', 'Giriş Katı', 'fa-door-open'),
('Genel', 'Üst Katlar', 'fa-arrow-up'),
('Genel', 'Şehir Manzaralı', 'fa-city'),
('Genel', 'Deniz Manzaralı', 'fa-water'),
('Genel', 'Havuz Manzaralı', 'fa-swimming-pool'),

-- Yatak Odası
('Yatak Odası', 'Gardırop / Dolap', 'fa-tshirt'),
('Yatak Odası', 'Kıyafet Askısı', 'fa-hanger'),
('Yatak Odası', 'Çalışma Masası', 'fa-desk'),
('Yatak Odası', 'Oturma Alanı', 'fa-couch'),
('Yatak Odası', 'Kanepe', 'fa-couch'),
('Yatak Odası', 'Çamaşır Kurutma Askısı', 'fa-tshirt'),

-- Banyo
('Banyo', 'Saç Kurutma Makinesi', 'fa-wind'),
('Banyo', 'Bedelsiz Banyo Malzemeleri', 'fa-pump-soap'),
('Banyo', 'Bornoz', 'fa-bath'),
('Banyo', 'Terlik', 'fa-shoe-prints'),
('Banyo', 'Makyaj Aynası', 'fa-mirror'),

-- Teknoloji
('Teknoloji', 'Düz Ekran TV', 'fa-tv'),
('Teknoloji', 'Uydu Kanalları', 'fa-satellite'),
('Teknoloji', 'Netflix / Akıllı TV', 'fa-tv'),
('Teknoloji', 'Telefon', 'fa-phone'),
('Teknoloji', 'Elektrikli Su Isıtıcısı', 'fa-mug-hot'),

-- Mutfak / Minibar
('Mutfak', 'Minibar', 'fa-glass'),
('Mutfak', 'Buzdolabı', 'fa-refrigerator'),
('Mutfak', 'Mikrodalga Fırın', 'fa-microwave'),
('Mutfak', 'Mutfak Gereçleri', 'fa-utensils'),
('Mutfak', 'Yemek Masası', 'fa-chair'),
('Mutfak', 'Bulaşık Makinesi', 'fa-pump-soap'),
('Mutfak', 'Fırın / Ocak', 'fa-fire'),
('Mutfak', 'Çamaşır Makinesi', 'fa-soap');

INSERT INTO ozel_tarih_tanimlari (tur, ad, baslangic_tarihi, bitis_tarihi, tekrar_eder_mi, fiyat_carpani, minimum_geceleme_kurali) VALUES
('Resmi Tatil', '23 Nisan Ulusal Egemenlik ve Çocuk Bayramı', '2024-04-23', '2024-04-23', 1, 1.30, NULL),
('Resmi Tatil', '1 Mayıs Emek ve Dayanışma Günü', '2024-05-01', '2024-05-01', 1, 1.30, NULL),
('Resmi Tatil', '19 Mayıs Atatürk''ü Anma, Gençlik ve Spor Bayramı', '2024-05-19', '2024-05-19', 1, 1.20, NULL),
('Dini Bayram', 'Ramazan Bayramı', '2024-04-10', '2024-04-12', 0, 2.00, 3),
('Dini Bayram', 'Kurban Bayramı', '2024-06-16', '2024-06-19', 0, 2.50, 4),
('Resmi Tatil', '15 Temmuz Demokrasi ve Milli Birlik Günü', '2024-07-15', '2024-07-15', 1, 1.20, NULL),
('Resmi Tatil', '30 Ağustos Zafer Bayramı', '2024-08-30', '2024-08-30', 1, 1.50, NULL),
('Resmi Tatil', '29 Ekim Cumhuriyet Bayramı', '2024-10-29', '2024-10-29', 1, 1.40, NULL),
('Özel Gün', 'Yılbaşı', '2024-12-31', '2025-01-01', 1, 2.50, 2),
('Özel Gün', 'Sevgililer Günü', '2025-02-14', '2025-02-14', 1, 1.50, NULL),
('Sezon Başlangıcı', 'Yaz Sezonu Başlangıcı (Antalya)', '2024-05-01', '2024-05-01', 1, 1.00, NULL),
('Sezon Bitişi', 'Yaz Sezonu Bitişi (Antalya)', '2024-10-31', '2024-10-31', 1, 0.70, NULL);

INSERT INTO bildirim_sablonlari (sablon_kodu, sablon_adi, tur, dil, konu, icerik) VALUES
('rezervasyon_onay', 'Rezervasyon Onayı', 'E-posta', 'tr', '{otel_adi} - Rezervasyon Onayı #{rezervasyon_no}', 'Sayın {ad_soyad}, rezervasyonunuz onaylanmıştır...'),
('rezervasyon_hatirlatma', 'Rezervasyon Hatırlatma', 'Push Notification', 'tr', 'Yaklaşan Rezervasyon', '{otel_adi} için rezervasyonunuza 24 saat kaldı!'),
('odeme_basarili', 'Ödeme Başarılı', 'SMS', 'tr', NULL, '{tutar} TL tutarındaki ödemeniz alınmıştır. Rezervasyon No: {rezervasyon_no}'),
('yeni_mesaj', 'Yeni Mesaj', 'Sistem İçi', 'tr', 'Yeni Mesaj', '{gonderen_adi} size bir mesaj gönderdi.'),
('ozel_teklif', 'Özel Teklif', 'E-posta', 'tr', '{otel_adi} - Özel Teklif', 'Size özel {tutar} TL fiyat teklifi!');



SET FOREIGN_KEY_CHECKS = 1;


