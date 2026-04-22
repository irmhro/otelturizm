SET NOCOUNT ON;

;WITH canonical_room_features AS (
    SELECT
        id,
        kategori,
        ozellik_adi,
        ROW_NUMBER() OVER (PARTITION BY kategori, ozellik_adi ORDER BY id ASC) AS rn,
        MIN(id) OVER (PARTITION BY kategori, ozellik_adi) AS canonical_id
    FROM dbo.oda_ozellikleri
)
UPDATE rel
SET rel.ozellik_id = canonical.canonical_id
FROM dbo.oda_tipi_ozellikleri AS rel
JOIN canonical_room_features AS canonical
    ON canonical.id = rel.ozellik_id
WHERE canonical.rn > 1
  AND rel.ozellik_id <> canonical.canonical_id;

;WITH duplicate_relations AS (
    SELECT
        oda_tip_id,
        ozellik_id,
        ROW_NUMBER() OVER (PARTITION BY oda_tip_id, ozellik_id ORDER BY oda_tip_id) AS rn
    FROM dbo.oda_tipi_ozellikleri
)
DELETE FROM duplicate_relations
WHERE rn > 1;

;WITH duplicate_room_features AS (
    SELECT
        id,
        ROW_NUMBER() OVER (PARTITION BY kategori, ozellik_adi ORDER BY id ASC) AS rn
    FROM dbo.oda_ozellikleri
)
DELETE FROM dbo.oda_ozellikleri
WHERE id IN (
    SELECT id
    FROM duplicate_room_features
    WHERE rn > 1
);

DECLARE @HotelFeatures TABLE
(
    kategori_id SMALLINT,
    ozellik_adi NVARCHAR(200),
    ozellik_ikon NVARCHAR(100),
    one_cikan_ozellik BIT,
    siralama SMALLINT
);

INSERT INTO @HotelFeatures (kategori_id, ozellik_adi, ozellik_ikon, one_cikan_ozellik, siralama)
VALUES
-- Genel & Konaklama
(1, N'24 Saat Güvenlik', N'fa-shield-halved', 1, 9),
(1, N'Günlük Kat Hizmeti', N'fa-broom', 1, 10),
(1, N'Bağlantılı Aile Odaları', N'fa-people-roof', 1, 11),
(1, N'Self Check-in', N'fa-keyboard', 0, 12),
(1, N'Hızlı Check-in / Check-out', N'fa-bolt', 0, 13),
(1, N'Ortak Salon / TV Alanı', N'fa-couch', 0, 14),
(1, N'Bahçe', N'fa-seedling', 0, 15),
(1, N'Teras', N'fa-sun', 0, 16),
(1, N'Kütüphane', N'fa-book-open', 0, 17),
(1, N'Şömine Alanı', N'fa-fire', 0, 18),
(1, N'Çamaşırhane', N'fa-shirt', 0, 19),
(1, N'Kuru Temizleme', N'fa-soap', 0, 20),
(1, N'Ütü Hizmeti', N'fa-shirt', 0, 21),

-- İnternet
(4, N'Yüksek Hızlı WiFi', N'fa-wifi', 1, 4),
(4, N'Business Center İnternet Noktası', N'fa-network-wired', 0, 5),

-- Yeme İçme
(5, N'Sadece Oda', N'fa-bed', 0, 9),
(5, N'Yarım Pansiyon', N'fa-utensils', 1, 10),
(5, N'Tam Pansiyon', N'fa-plate-wheat', 1, 11),
(5, N'Her Şey Dahil', N'fa-champagne-glasses', 1, 12),
(5, N'Ultra Her Şey Dahil', N'fa-crown', 1, 13),
(5, N'Kahve Noktası', N'fa-mug-saucer', 0, 14),
(5, N'Vejetaryen Menü', N'fa-leaf', 0, 15),
(5, N'Vegan Menü', N'fa-seedling', 0, 16),
(5, N'Glutensiz Menü', N'fa-wheat-awn-circle-exclamation', 0, 17),
(5, N'Çocuk Büfesi', N'fa-child-reaching', 0, 18),
(5, N'Gece Çorbası', N'fa-bowl-food', 0, 19),
(5, N'Kafe', N'fa-mug-hot', 0, 20),
(5, N'Patisserie', N'fa-cake-candles', 0, 21),
(5, N'Çatı Restoranı', N'fa-building', 0, 22),

-- Havuz, Spa & Wellness
(6, N'Infinity Havuz', N'fa-water-ladder', 1, 16),
(6, N'Yetişkin Havuzu', N'fa-person-swimming', 0, 17),
(6, N'Aqua Park', N'fa-water-ladder', 1, 18),
(6, N'Spa Lounge', N'fa-spa', 0, 19),
(6, N'Tuz Odası', N'fa-gem', 0, 20),
(6, N'Wellness Programı', N'fa-heart-pulse', 0, 21),
(6, N'Yoga Stüdyosu', N'fa-person-praying', 0, 22),
(6, N'Pilates Alanı', N'fa-dumbbell', 0, 23),
(6, N'Kapalı Çocuk Oyun Havuzu', N'fa-child-reaching', 0, 24),

-- Spor & Eğlence
(7, N'Çocuk Kulübü', N'fa-children', 1, 8),
(7, N'Oyun Salonu', N'fa-gamepad', 0, 9),
(7, N'Canlı Müzik', N'fa-music', 0, 10),
(7, N'Sinema Gecesi', N'fa-film', 0, 11),
(7, N'Bisiklet Kiralama', N'fa-bicycle', 0, 12),
(7, N'Kayak Ekipmanı Deposu', N'fa-person-skiing', 0, 13),
(7, N'Dalış Merkezi', N'fa-person-swimming', 0, 14),
(7, N'Basketbol Sahası', N'fa-basketball', 0, 15),
(7, N'Futbol Sahası', N'fa-futbol', 0, 16),
(7, N'Mini Golf', N'fa-golf-ball-tee', 0, 17),

-- Otopark & Ulaşım
(9, N'Havaalanı Transferi', N'fa-shuttle-van', 1, 6),
(9, N'Şehir Merkezi Shuttle', N'fa-bus', 0, 7),
(9, N'Araç Kiralama', N'fa-car-side', 0, 8),
(9, N'Bisiklet Parkı', N'fa-bicycle', 0, 9),
(9, N'Motosiklet Parkı', N'fa-motorcycle', 0, 10),

-- Deniz & Plaj
(10, N'Özel Plaj', N'fa-umbrella-beach', 1, 1),
(10, N'Sahil Servisi', N'fa-umbrella-beach', 0, 2),
(10, N'Plaj Havlusu', N'fa-person-shelter', 0, 3),
(10, N'İskele', N'fa-anchor', 0, 4),
(10, N'Mavi Bayraklı Plaj', N'fa-flag', 1, 5),
(10, N'Beach Club', N'fa-umbrella-beach', 0, 6),

-- Aile & Çocuk
(11, N'Bebek Yatağı', N'fa-baby', 1, 1),
(11, N'Bebek Sandalyesi', N'fa-chair', 0, 2),
(11, N'Çocuk Bakıcısı', N'fa-baby', 0, 3),
(11, N'Çocuk Oyun Alanı', N'fa-puzzle-piece', 0, 4),
(11, N'Aile Süitleri', N'fa-people-roof', 1, 5),
(11, N'Çocuk Menüsü', N'fa-bowl-rice', 0, 6),

-- İş & Toplantı
(12, N'Toplantı Odası', N'fa-people-group', 1, 1),
(12, N'BalO Salonu', N'fa-people-roof', 0, 2),
(12, N'Business Center', N'fa-briefcase', 0, 3),
(12, N'Yazıcı / Fotokopi', N'fa-print', 0, 4),
(12, N'Konferans Salonu', N'fa-microphone-lines', 0, 5),

-- Erişilebilirlik
(13, N'Asansörle Erişim', N'fa-elevator', 0, 5),
(13, N'Sesli Uyarı Sistemi', N'fa-volume-high', 0, 6),
(13, N'Rampa Erişimi', N'fa-road-barrier', 0, 7),

-- Evcil Hayvan
(14, N'Kedi Dostu', N'fa-cat', 0, 5),
(14, N'Köpek Dostu', N'fa-dog', 0, 6),
(14, N'Evcil Hayvan Yıkama Alanı', N'fa-shower', 0, 7),

-- Sürdürülebilirlik
(15, N'Geri Dönüşüm Politikası', N'fa-recycle', 0, 1),
(15, N'Güneş Enerjisi', N'fa-solar-panel', 0, 2),
(15, N'Su Tasarrufu Uygulaması', N'fa-droplet', 0, 3),
(15, N'Elektrikli Araç Dostu', N'fa-charging-station', 0, 4),

-- Konsept / Tesis Tipi
(16, N'Pansiyon Konsepti', N'fa-house', 1, 1),
(16, N'Apart Konaklama', N'fa-building-user', 1, 2),
(16, N'Butik Otel Konsepti', N'fa-gem', 1, 3),
(16, N'Resort Konsepti', N'fa-umbrella-beach', 1, 4),
(16, N'Ski Lodge Konsepti', N'fa-person-skiing', 0, 5),
(16, N'Spa Resort Konsepti', N'fa-spa', 0, 6);

INSERT INTO dbo.otel_ozellikleri (kategori_id, ozellik_adi, ozellik_ikon, one_cikan_ozellik, siralama, aktif_mi)
SELECT hf.kategori_id, hf.ozellik_adi, hf.ozellik_ikon, hf.one_cikan_ozellik, hf.siralama, 1
FROM @HotelFeatures AS hf
WHERE NOT EXISTS (
    SELECT 1
    FROM dbo.otel_ozellikleri AS existing
    WHERE existing.ozellik_adi = hf.ozellik_adi
);

DECLARE @RoomFeatures TABLE
(
    kategori NVARCHAR(100),
    ozellik_adi NVARCHAR(200),
    ozellik_ikon NVARCHAR(100)
);

INSERT INTO @RoomFeatures (kategori, ozellik_adi, ozellik_ikon)
VALUES
-- Genel
(N'Genel', N'Balkon', N'fa-building'),
(N'Genel', N'Teras', N'fa-sun'),
(N'Genel', N'Bahçe Erişimi', N'fa-seedling'),
(N'Genel', N'Dağ Manzaralı', N'fa-mountain'),
(N'Genel', N'Orman Manzaralı', N'fa-tree'),
(N'Genel', N'Göl Manzaralı', N'fa-water'),
(N'Genel', N'Avlu Manzaralı', N'fa-archway'),
(N'Genel', N'Bağlantılı Oda', N'fa-link'),
(N'Genel', N'Özel Giriş', N'fa-door-open'),
(N'Genel', N'Sigara İçilmeyen Oda', N'fa-smoking-ban'),

-- Yatak Odası
(N'Yatak Odası', N'King Yatak', N'fa-bed'),
(N'Yatak Odası', N'Twin Yatak', N'fa-bed'),
(N'Yatak Odası', N'Sofa Bed', N'fa-couch'),
(N'Yatak Odası', N'Ortopedik Yatak', N'fa-bed'),
(N'Yatak Odası', N'Blackout Perde', N'fa-square'),
(N'Yatak Odası', N'Kasalı Dolap', N'fa-vault'),

-- Banyo
(N'Banyo', N'Özel Banyo', N'fa-bath'),
(N'Banyo', N'Duşakabin', N'fa-shower'),
(N'Banyo', N'Küvet', N'fa-bath'),
(N'Banyo', N'Jakuzi Küvet', N'fa-hot-tub-person'),
(N'Banyo', N'Çift Lavabo', N'fa-faucet'),
(N'Banyo', N'Yağmur Duşu', N'fa-cloud-showers-heavy'),
(N'Banyo', N'Bidet', N'fa-faucet-drip'),

-- Teknoloji
(N'Teknoloji', N'USB Şarj Noktası', N'fa-plug'),
(N'Teknoloji', N'Bluetooth Hoparlör', N'fa-volume-high'),
(N'Teknoloji', N'Laptop Kasası', N'fa-laptop'),
(N'Teknoloji', N'Fiber İnternet', N'fa-network-wired'),
(N'Teknoloji', N'Kablosuz Şarj', N'fa-charging-station'),

-- Mutfak
(N'Mutfak', N'Çay / Kahve Seti', N'fa-mug-hot'),
(N'Mutfak', N'Espresso Makinesi', N'fa-mug-hot'),
(N'Mutfak', N'Amerikan Mutfak', N'fa-kitchen-set'),
(N'Mutfak', N'Yemek Pişirme Seti', N'fa-pan-frying'),
(N'Mutfak', N'Su Şişesi', N'fa-bottle-water'),

-- Konfor
(N'Konfor', N'Oturma Grubu', N'fa-couch'),
(N'Konfor', N'Yemek Bölümü', N'fa-chair'),
(N'Konfor', N'Separate Living Room', N'fa-people-roof'),
(N'Konfor', N'Halılı Zemin', N'fa-rug'),
(N'Konfor', N'Parke Zemin', N'fa-grip-lines'),
(N'Konfor', N'Isı Kontrollü Klima', N'fa-fan'),
(N'Konfor', N'Okuma Lambası', N'fa-lightbulb'),

-- Erişilebilirlik
(N'Erişilebilirlik', N'Tekerlekli Sandalyeye Uygun', N'fa-wheelchair'),
(N'Erişilebilirlik', N'Tutunma Barları', N'fa-grip-lines'),
(N'Erişilebilirlik', N'Alçak Lavabo', N'fa-faucet'),
(N'Erişilebilirlik', N'Geniş Kapı', N'fa-door-open'),

-- Güvenlik
(N'Güvenlik', N'Elektronik Kasa', N'fa-vault'),
(N'Güvenlik', N'Akıllı Kilit', N'fa-key'),
(N'Güvenlik', N'Duman Dedektörü', N'fa-fire-extinguisher'),
(N'Güvenlik', N'Kartlı Giriş', N'fa-id-card'),

-- Hizmet
(N'Hizmet', N'Günlük Oda Temizliği', N'fa-broom'),
(N'Hizmet', N'Yastık Menüsü', N'fa-bed'),
(N'Hizmet', N'Gece Servisi', N'fa-bell-concierge'),
(N'Hizmet', N'Uyandırma Servisi', N'fa-clock'),

-- Aile & Çocuk
(N'Aile ve Çocuk', N'Bebek Yatağı Uygun', N'fa-baby'),
(N'Aile ve Çocuk', N'Çocuk Güvenlik Kilidi', N'fa-child'),
(N'Aile ve Çocuk', N'Aile Kullanımına Uygun', N'fa-people-roof');

INSERT INTO dbo.oda_ozellikleri (kategori, ozellik_adi, ozellik_ikon, aktif_mi)
SELECT rf.kategori, rf.ozellik_adi, rf.ozellik_ikon, 1
FROM @RoomFeatures AS rf
WHERE NOT EXISTS (
    SELECT 1
    FROM dbo.oda_ozellikleri AS existing
    WHERE existing.kategori = rf.kategori
      AND existing.ozellik_adi = rf.ozellik_adi
);
