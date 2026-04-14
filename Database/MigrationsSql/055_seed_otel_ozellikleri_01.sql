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

