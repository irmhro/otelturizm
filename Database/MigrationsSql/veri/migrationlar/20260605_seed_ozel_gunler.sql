SET NOCOUNT ON;
SET XACT_ABORT ON;

IF OBJECT_ID(N'dbo.OZEL_GUNLER', N'U') IS NULL
BEGIN
    RAISERROR(N'OZEL_GUNLER tablosu bulunamadi. Once 20260605_sqlserver_ozel_gunler.sql calistirin.', 16, 1);
    RETURN;
END

CREATE TABLE #ozel_gun_seed
(
    [GUN_KODU] nvarchar(80) NOT NULL PRIMARY KEY,
    [GUN_ADI] nvarchar(200) NOT NULL,
    [AY] tinyint NOT NULL,
    [GUN] tinyint NULL,
    [KURAL_TIPI] nvarchar(30) NOT NULL,
    [KURAL_PARAM1] tinyint NULL,
    [KURAL_PARAM2] tinyint NULL,
    [EMOJI] nvarchar(16) NULL,
    [KUTLAMA_METNI] nvarchar(300) NULL,
    [KATEGORI] nvarchar(80) NULL,
    [SIRALAMA] int NOT NULL
);

INSERT INTO #ozel_gun_seed ([GUN_KODU], [GUN_ADI], [AY], [GUN], [KURAL_TIPI], [KURAL_PARAM1], [KURAL_PARAM2], [EMOJI], [KUTLAMA_METNI], [KATEGORI], [SIRALAMA]) VALUES
-- Türkiye / kutlama öncelikli
(N'yeni-yil', N'Yeni Yıl', 1, 1, N'SABIT', NULL, NULL, N'🎉', N'Yeni Yıl''ınız kutlu olsun', N'Kutlama', 1),
(N'cumhuriyet-bayrami', N'Cumhuriyet Bayramı', 10, 29, N'SABIT', NULL, NULL, N'🇹🇷', N'Cumhuriyet Bayramımız kutlu olsun', N'Ulusal', 2),
(N'zafer-bayrami', N'Zafer Bayramı', 8, 30, N'SABIT', NULL, NULL, N'🇹🇷', N'Zafer Bayramımız kutlu olsun', N'Ulusal', 3),
(N'cocuk-bayrami', N'Ulusal Egemenlik ve Çocuk Bayramı', 4, 23, N'SABIT', NULL, NULL, N'🎈', N'23 Nisan Ulusal Egemenlik ve Çocuk Bayramı kutlu olsun', N'Ulusal', 4),
(N'genclik-bayrami', N'Atatürk''ü Anma Gençlik ve Spor Bayramı', 5, 19, N'SABIT', NULL, NULL, N'🏃', N'19 Mayıs Atatürk''ü Anma Gençlik ve Spor Bayramı kutlu olsun', N'Ulusal', 5),
(N'demokrasi-bayrami', N'Demokrasi ve Milli Birlik Günü', 7, 15, N'SABIT', NULL, NULL, N'🇹🇷', N'Demokrasi ve Milli Birlik Günümüz kutlu olsun', N'Ulusal', 6),
(N'anneler-gunu', N'Anneler Günü', 5, NULL, N'NINCI_HAFTA_GUNU', 2, 0, N'💐', N'Anneler Günü''nü kutluyoruz', N'Kutlama', 7),
(N'babalar-gunu', N'Babalar Günü', 6, NULL, N'NINCI_HAFTA_GUNU', 3, 0, N'👔', N'Babalar Günü''nü kutluyoruz', N'Kutlama', 8),
(N'sevgililer-gunu', N'Sevgililer Günü', 2, 14, N'SABIT', NULL, NULL, N'❤️', N'Sevgililer Günü''nü kutluyoruz', N'Kutlama', 9),
(N'ogretmenler-gunu-tr', N'Öğretmenler Günü', 11, 24, N'SABIT', NULL, NULL, N'📚', N'Öğretmenler Günü''nü kutluyoruz', N'Ulusal', 10),
(N'kadinlar-gunu', N'Kadınlar Günü', 3, 8, N'SABIT', NULL, NULL, N'🌸', N'Kadınlar Günü''nü kutluyoruz', N'Kutlama', 11),
(N'emek-ve-dayanisma', N'Emek ve Dayanışma Günü', 5, 1, N'SABIT', NULL, NULL, N'✊', N'Emek ve Dayanışma Günü kutlu olsun', N'Ulusal', 12),

-- Kullanıcı listesi + BM / dünya günleri (Ocak–Aralık)
(N'uluslararasi-egitim-gunu', N'Uluslararası Eğitim Günü', 1, 24, N'SABIT', NULL, NULL, N'🎓', N'Uluslararası Eğitim Günü''nü kutluyoruz', N'BM', 20),
(N'veri-gizliligi-gunu', N'Veri Gizliliği Günü', 1, 28, N'SABIT', NULL, NULL, N'🔒', N'Veri Gizliliği Günü''nü anıyoruz', N'BM', 21),
(N'dunya-kanser-gunu', N'Dünya Kanser Günü', 2, 4, N'SABIT', NULL, NULL, N'🎗️', N'Dünya Kanser Günü''nü anıyoruz', N'Sağlık', 22),
(N'bilim-gunu-subat', N'Bilim Günü', 2, 10, N'SABIT', NULL, NULL, N'🔬', N'Bilim Günü''nü kutluyoruz', N'BM', 23),
(N'dunya-radyo-gunu', N'Dünya Radyo Günü', 2, 13, N'SABIT', NULL, NULL, N'📻', N'Dünya Radyo Günü''nü kutluyoruz', N'BM', 24),
(N'sosyal-adalet-gunu', N'Sosyal Adalet Günü', 2, 20, N'SABIT', NULL, NULL, N'⚖️', N'Sosyal Adalet Günü''nü anıyoruz', N'BM', 25),
(N'ana-dil-gunu', N'Ana Dil Günü', 2, 21, N'SABIT', NULL, NULL, N'🗣️', N'Ana Dil Günü''nü kutluyoruz', N'BM', 26),
(N'yaban-hayati-gunu', N'Yaban Hayatı Günü', 3, 3, N'SABIT', NULL, NULL, N'🦁', N'Yaban Hayatı Günü''nü anıyoruz', N'Çevre', 27),
(N'insan-basarilari-gunu', N'İnsan Başarıları Günü', 7, 20, N'SABIT', NULL, NULL, N'🚀', N'İnsan Başarıları Günü''nü kutluyoruz', N'BM', 28),
(N'matematik-gunu', N'Matematik Günü', 3, 14, N'SABIT', NULL, NULL, N'🥧', N'Matematik Günü''nü kutluyoruz', N'BM', 29),
(N'dunya-mutluluk-gunu', N'Dünya Mutluluk Günü', 3, 20, N'SABIT', NULL, NULL, N'😊', N'Dünya Mutluluk Günü kutlu olsun', N'BM', 30),
(N'orman-gunu', N'Orman Günü', 3, 21, N'SABIT', NULL, NULL, N'🌳', N'Orman Günü''nü anıyoruz', N'Çevre', 31),
(N'dunya-su-gunu', N'Dünya Su Günü', 3, 22, N'SABIT', NULL, NULL, N'💧', N'Dünya Su Günü''nü anıyoruz', N'Çevre', 32),
(N'meteoroloji-gunu', N'Meteoroloji Günü', 3, 23, N'SABIT', NULL, NULL, N'🌦️', N'Meteoroloji Günü''nü kutluyoruz', N'BM', 33),
(N'dunya-saglik-gunu', N'Dünya Sağlık Günü', 4, 7, N'SABIT', NULL, NULL, N'🏥', N'Dünya Sağlık Günü''nü anıyoruz', N'Sağlık', 34),
(N'insanlik-uzay-ucusu-gunu', N'İnsanlık Uzay Uçuşu Günü', 4, 12, N'SABIT', NULL, NULL, N'🛰️', N'İnsanlık Uzay Uçuşu Günü''nü kutluyoruz', N'BM', 35),
(N'dunya-gunu', N'Dünya Günü', 4, 22, N'SABIT', NULL, NULL, N'🌍', N'Dünya Günü''nü anıyoruz', N'Çevre', 36),
(N'fikri-mulkiyet-gunu', N'Fikri Mülkiyet Günü', 4, 26, N'SABIT', NULL, NULL, N'💡', N'Fikri Mülkiyet Günü''nü anıyoruz', N'BM', 37),
(N'basin-ozgurlugu-gunu', N'Basın Özgürlüğü Günü', 5, 3, N'SABIT', NULL, NULL, N'📰', N'Basın Özgürlüğü Günü''nü anıyoruz', N'BM', 38),
(N'aile-gunu', N'Aile Günü', 5, 15, N'SABIT', NULL, NULL, N'👨‍👩‍👧', N'Aile Günü''nü kutluyoruz', N'BM', 39),
(N'internet-gunu', N'İnternet Günü', 5, 17, N'SABIT', NULL, NULL, N'🌐', N'İnternet Günü''nü kutluyoruz', N'BM', 40),
(N'ari-gunu', N'Arı Günü', 5, 20, N'SABIT', NULL, NULL, N'🐝', N'Arı Günü''nü anıyoruz', N'Çevre', 41),
(N'biyolojik-cesitlilik-gunu', N'Biyolojik Çeşitlilik Günü', 5, 22, N'SABIT', NULL, NULL, N'🦋', N'Biyolojik Çeşitlilik Günü''nü anıyoruz', N'Çevre', 42),
(N'dunya-cevre-gunu', N'Dünya Çevre Günü', 6, 5, N'SABIT', NULL, NULL, N'♻️', N'Dünya Çevre Günü''nü anıyoruz', N'Çevre', 43),
(N'okyanus-gunu', N'Okyanus Günü', 6, 8, N'SABIT', NULL, NULL, N'🌊', N'Okyanus Günü''nü anıyoruz', N'Çevre', 44),
(N'collesmeyle-mucadele-gunu', N'Çölleşmeyle Mücadele Günü', 6, 17, N'SABIT', NULL, NULL, N'🏜️', N'Çölleşmeyle Mücadele Günü''nü anıyoruz', N'Çevre', 45),
(N'yaz-gundonumu', N'Yaz Gündönümü', 6, 21, N'SABIT', NULL, NULL, N'☀️', N'Yaz Gündönümü''nü kutluyoruz', N'Doğa', 46),
(N'genclik-becerileri-gunu', N'Dünya Gençlik Becerileri Günü', 7, 15, N'SABIT', NULL, NULL, N'🛠️', N'Dünya Gençlik Becerileri Günü''nü kutluyoruz', N'BM', 47),
(N'hepatit-gunu', N'Hepatit Günü', 7, 28, N'SABIT', NULL, NULL, N'🩺', N'Hepatit Günü''nü anıyoruz', N'Sağlık', 48),
(N'dostluk-gunu', N'Dostluk Günü', 7, 30, N'SABIT', NULL, NULL, N'🤝', N'Dostluk Günü''nü kutluyoruz', N'BM', 49),
(N'genclik-gunu', N'Gençlik Günü', 8, 12, N'SABIT', NULL, NULL, N'🧑‍🤝‍🧑', N'Gençlik Günü''nü kutluyoruz', N'BM', 51),
(N'okuryazarlik-gunu', N'Okuryazarlık Günü', 9, 8, N'SABIT', NULL, NULL, N'📖', N'Okuryazarlık Günü''nü kutluyoruz', N'BM', 52),
(N'ozon-gunu', N'Ozon Günü', 9, 16, N'SABIT', NULL, NULL, N'🛡️', N'Ozon Günü''nü anıyoruz', N'Çevre', 53),
(N'baris-gunu', N'Barış Günü', 9, 21, N'SABIT', NULL, NULL, N'🕊️', N'Barış Günü''nü kutluyoruz', N'BM', 54),
(N'otomobilsiz-gun', N'Otomobilsiz Gün', 9, 22, N'SABIT', NULL, NULL, N'🚶', N'Otomobilsiz Gün''ü anıyoruz', N'Çevre', 55),
(N'dunya-turizm-gunu', N'Dünya Turizm Günü', 9, 27, N'SABIT', NULL, NULL, N'✈️', N'Dünya Turizm Günü''nü kutluyoruz', N'BM', 56),
(N'yaslilar-gunu', N'Yaşlılar Günü', 10, 1, N'SABIT', NULL, NULL, N'👵', N'Yaşlılar Günü''nü anıyoruz', N'BM', 57),
(N'hayvanlari-koruma-gunu', N'Hayvanları Koruma Günü', 10, 4, N'SABIT', NULL, NULL, N'🐾', N'Hayvanları Koruma Günü''nü anıyoruz', N'Çevre', 58),
(N'ogretmenler-gunu-dunya', N'Öğretmenler Günü', 10, 5, N'SABIT', NULL, NULL, N'👩‍🏫', N'Öğretmenler Günü''nü kutluyoruz', N'BM', 59),
(N'ruh-sagligi-gunu', N'Ruh Sağlığı Günü', 10, 10, N'SABIT', NULL, NULL, N'🧠', N'Ruh Sağlığı Günü''nü anıyoruz', N'Sağlık', 60),
(N'afet-risklerini-azaltma-gunu', N'Afet Risklerini Azaltma Günü', 10, 13, N'SABIT', NULL, NULL, N'🆘', N'Afet Risklerini Azaltma Günü''nü anıyoruz', N'BM', 61),
(N'dunya-gida-gunu', N'Dünya Gıda Günü', 10, 16, N'SABIT', NULL, NULL, N'🍽️', N'Dünya Gıda Günü''nü anıyoruz', N'BM', 62),
(N'bm-gunu', N'BM Günü', 10, 24, N'SABIT', NULL, NULL, N'🇺🇳', N'BM Günü''nü anıyoruz', N'BM', 63),
(N'dunya-bilim-gunu', N'Dünya Bilim Günü', 11, 10, N'SABIT', NULL, NULL, N'🧪', N'Dünya Bilim Günü''nü kutluyoruz', N'BM', 64),
(N'iyilik-gunu', N'İyilik Günü', 11, 13, N'SABIT', NULL, NULL, N'💛', N'İyilik Günü''nü kutluyoruz', N'BM', 65),
(N'diyabet-gunu', N'Diyabet Günü', 11, 14, N'SABIT', NULL, NULL, N'🩸', N'Diyabet Günü''nü anıyoruz', N'Sağlık', 66),
(N'cocuk-haklari-gunu', N'Çocuk Hakları Günü', 11, 20, N'SABIT', NULL, NULL, N'🧒', N'Çocuk Hakları Günü''nü anıyoruz', N'BM', 67),
(N'televizyon-gunu', N'Televizyon Günü', 11, 21, N'SABIT', NULL, NULL, N'📺', N'Televizyon Günü''nü anıyoruz', N'BM', 68),
(N'gonulluler-gunu', N'Gönüllüler Günü', 12, 5, N'SABIT', NULL, NULL, N'🤲', N'Gönüllüler Günü''nü kutluyoruz', N'BM', 70),
(N'sivil-havacilik-gunu', N'Sivil Havacılık Günü', 12, 7, N'SABIT', NULL, NULL, N'✈️', N'Sivil Havacılık Günü''nü kutluyoruz', N'BM', 71),
(N'daglar-gunu', N'Dağlar Günü', 12, 11, N'SABIT', NULL, NULL, N'⛰️', N'Dağlar Günü''nü anıyoruz', N'BM', 72),

-- Ek dünya günleri (turizm / kutlama odaklı)
(N'dunya-hayvan-gunu', N'Dünya Hayvan Günü', 10, 4, N'SABIT', NULL, NULL, N'🐶', N'Dünya Hayvan Günü''nü anıyoruz', N'Çevre', 78),
(N'kisisel-verilerin-korunmasi', N'Kişisel Verilerin Korunması Günü', 1, 28, N'SABIT', NULL, NULL, N'🛡️', N'Kişisel Verilerin Korunması Günü''nü anıyoruz', N'BM', 77),
(N'fotograf-gunu', N'Dünya Fotoğraf Günü', 8, 19, N'SABIT', NULL, NULL, N'📷', N'Dünya Fotoğraf Günü''nü kutluyoruz', N'BM', 82),
(N'gastronomi-gunu', N'Dünya Gastronomi Günü', 10, 16, N'SABIT', NULL, NULL, N'🍷', N'Dünya Gastronomi Günü''nü kutluyoruz', N'BM', 83),
(N'kitap-gunu', N'Dünya Kitap Günü', 4, 23, N'SABIT', NULL, NULL, N'📚', N'Dünya Kitap Günü''nü kutluyoruz', N'BM', 84),
(N'muzik-gunu', N'Dünya Müzik Günü', 6, 21, N'SABIT', NULL, NULL, N'🎵', N'Dünya Müzik Günü''nü kutluyoruz', N'BM', 85),
(N'kahve-gunu', N'Uluslararası Kahve Günü', 10, 1, N'SABIT', NULL, NULL, N'☕', N'Uluslararası Kahve Günü''nü kutluyoruz', N'Kutlama', 86),
(N'cikolata-gunu', N'Dünya Çikolata Günü', 7, 7, N'SABIT', NULL, NULL, N'🍫', N'Dünya Çikolata Günü''nü kutluyoruz', N'Kutlama', 87),
(N'pizza-gunu', N'Dünya Pizza Günü', 2, 9, N'SABIT', NULL, NULL, N'🍕', N'Dünya Pizza Günü''nü kutluyoruz', N'Kutlama', 88),
(N'emoji-gunu', N'Dünya Emoji Günü', 7, 17, N'SABIT', NULL, NULL, N'😀', N'Dünya Emoji Günü''nü kutluyoruz', N'Kutlama', 89),
(N'kahkaha-gunu', N'Dünya Kahkaha Günü', 5, 3, N'SABIT', NULL, NULL, N'😂', N'Dünya Kahkaha Günü kutlu olsun', N'Kutlama', 90),
(N'kedi-gunu', N'Uluslararası Kedi Günü', 8, 8, N'SABIT', NULL, NULL, N'🐱', N'Uluslararası Kedi Günü''nü kutluyoruz', N'Kutlama', 91),
(N'kopek-gunu', N'Uluslararası Köpek Günü', 8, 26, N'SABIT', NULL, NULL, N'🐕', N'Uluslararası Köpek Günü''nü kutluyoruz', N'Kutlama', 92),
(N'gezegen-gunu', N'Dünya Gezegen Günü', 4, 22, N'SABIT', NULL, NULL, N'🪐', N'Dünya Gezegen Günü''nü anıyoruz', N'Çevre', 93),
(N'kis-gundonumu', N'Kış Gündönümü', 12, 21, N'SABIT', NULL, NULL, N'❄️', N'Kış Gündönümü''nü kutluyoruz', N'Doğa', 94),
(N'ilkbahar-gundonumu', N'İlkbahar Gündönümü', 3, 20, N'SABIT', NULL, NULL, N'🌷', N'İlkbahar Gündönümü''nü kutluyoruz', N'Doğa', 95),
(N'sonbahar-gundonumu', N'Sonbahar Gündönümü', 9, 22, N'SABIT', NULL, NULL, N'🍂', N'Sonbahar Gündönümü''nü kutluyoruz', N'Doğa', 96);

MERGE [dbo].[OZEL_GUNLER] AS target
USING #ozel_gun_seed AS source
    ON target.[GUN_KODU] = source.[GUN_KODU]
WHEN MATCHED THEN
    UPDATE SET
        [GUN_ADI] = source.[GUN_ADI],
        [AY] = source.[AY],
        [GUN] = source.[GUN],
        [KURAL_TIPI] = source.[KURAL_TIPI],
        [KURAL_PARAM1] = source.[KURAL_PARAM1],
        [KURAL_PARAM2] = source.[KURAL_PARAM2],
        [EMOJI] = source.[EMOJI],
        [KUTLAMA_METNI] = source.[KUTLAMA_METNI],
        [KATEGORI] = source.[KATEGORI],
        [SIRALAMA] = source.[SIRALAMA],
        [AKTIF_MI] = 1
WHEN NOT MATCHED BY TARGET THEN
    INSERT ([GUN_KODU], [GUN_ADI], [AY], [GUN], [KURAL_TIPI], [KURAL_PARAM1], [KURAL_PARAM2], [EMOJI], [KUTLAMA_METNI], [KATEGORI], [SIRALAMA], [AKTIF_MI])
    VALUES (source.[GUN_KODU], source.[GUN_ADI], source.[AY], source.[GUN], source.[KURAL_TIPI], source.[KURAL_PARAM1], source.[KURAL_PARAM2], source.[EMOJI], source.[KUTLAMA_METNI], source.[KATEGORI], source.[SIRALAMA], 1);

DROP TABLE #ozel_gun_seed;

DECLARE @ozelGunCount int;
SELECT @ozelGunCount = COUNT(*) FROM dbo.OZEL_GUNLER;
PRINT N'OZEL_GUNLER seed tamamlandi. Kayit: ' + CAST(@ozelGunCount AS nvarchar(20));
GO
