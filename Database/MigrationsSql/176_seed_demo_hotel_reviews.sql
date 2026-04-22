SET NOCOUNT ON;

IF OBJECT_ID(N'dbo.oteller', N'U') IS NULL
    OR OBJECT_ID(N'dbo.yorumlar', N'U') IS NULL
BEGIN
    RETURN;
END;

DECLARE @approvedStatus nvarchar(32) = N'Onaylandı';
DECLARE @adminUserId bigint = 32;

DECLARE @reviewSeeds TABLE
(
    otel_id bigint NOT NULL,
    kullanici_id bigint NOT NULL,
    yorum_basligi nvarchar(200) NOT NULL,
    yorum_metni nvarchar(max) NOT NULL,
    olumlu_yanlar nvarchar(500) NULL,
    olumsuz_yanlar nvarchar(500) NULL,
    konaklama_tarihi date NOT NULL,
    konaklama_turu nvarchar(80) NOT NULL,
    kaldigi_oda_tipi nvarchar(120) NOT NULL,
    gece_sayisi tinyint NOT NULL,
    seyahat_profili nvarchar(80) NULL,
    genel_puan tinyint NOT NULL,
    temizlik_puani tinyint NOT NULL,
    konfor_puani tinyint NOT NULL,
    konum_puani tinyint NOT NULL,
    personel_puani tinyint NOT NULL,
    fiyat_performans_puani tinyint NOT NULL,
    memnuniyet_seviyesi tinyint NOT NULL,
    genel_puan_10 tinyint NOT NULL,
    puan_oda_10 tinyint NOT NULL,
    puan_konum_10 tinyint NOT NULL,
    puan_fiyat_10 tinyint NOT NULL,
    puan_personel_10 tinyint NOT NULL
);

INSERT INTO @reviewSeeds
(
    otel_id, kullanici_id, yorum_basligi, yorum_metni, olumlu_yanlar, olumsuz_yanlar,
    konaklama_tarihi, konaklama_turu, kaldigi_oda_tipi, gece_sayisi, seyahat_profili,
    genel_puan, temizlik_puani, konfor_puani, konum_puani, personel_puani, fiyat_performans_puani,
    memnuniyet_seviyesi, genel_puan_10, puan_oda_10, puan_konum_10, puan_fiyat_10, puan_personel_10
)
VALUES
(45, 103, N'Hafta sonu için dengeli seçim', N'Oda temizdi, giriş süreci hızlıydı ve personel ihtiyaç duyduğumuz her konuda yardımcı oldu. Kısa konaklama için rahat bir deneyimdi.', N'Temizlik, sessizlik, hızlı check-in', N'Kahvaltı çeşitliliği biraz artabilir', '2026-04-12', N'Tatil', N'Standart Oda', 2, N'Çift', 9, 9, 9, 8, 9, 9, 9, 9, 9, 8, 9, 9);

INSERT INTO @reviewSeeds
(
    otel_id, kullanici_id, yorum_basligi, yorum_metni, olumlu_yanlar, olumsuz_yanlar,
    konaklama_tarihi, konaklama_turu, kaldigi_oda_tipi, gece_sayisi, seyahat_profili,
    genel_puan, temizlik_puani, konfor_puani, konum_puani, personel_puani, fiyat_performans_puani,
    memnuniyet_seviyesi, genel_puan_10, puan_oda_10, puan_konum_10, puan_fiyat_10, puan_personel_10
)
VALUES
(45, 102, N'İş seyahati için pratik', N'Havaalanı ve şehir bağlantısı rahattı. Oda kompakt ama düzenliydi, internet hızı da toplantılar için yeterliydi.', N'Konum, internet, personel ilgisi', N'Oda biraz daha geniş olabilirdi', '2026-04-16', N'İş', N'Deluxe Oda', 1, N'İş', 8, 8, 8, 9, 9, 8, 8, 8, 8, 9, 8, 9);

INSERT INTO @reviewSeeds
(
    otel_id, kullanici_id, yorum_basligi, yorum_metni, olumlu_yanlar, olumsuz_yanlar,
    konaklama_tarihi, konaklama_turu, kaldigi_oda_tipi, gece_sayisi, seyahat_profili,
    genel_puan, temizlik_puani, konfor_puani, konum_puani, personel_puani, fiyat_performans_puani,
    memnuniyet_seviyesi, genel_puan_10, puan_oda_10, puan_konum_10, puan_fiyat_10, puan_personel_10
)
VALUES
(46, 103, N'Manzara ve atmosfer çok iyi', N'Kapadokya gezisi için çok keyifli bir konaklamaydı. Sabah manzara harikaydı, oda sıcak ve düzenliydi.', N'Manzara, oda atmosferi, kahvaltı', N'Otopark alanı biraz sınırlı', '2026-04-09', N'Tatil', N'Deluxe Oda', 2, N'Çift', 9, 9, 9, 10, 9, 8, 9, 9, 9, 10, 8, 9);

INSERT INTO @reviewSeeds
(
    otel_id, kullanici_id, yorum_basligi, yorum_metni, olumlu_yanlar, olumsuz_yanlar,
    konaklama_tarihi, konaklama_turu, kaldigi_oda_tipi, gece_sayisi, seyahat_profili,
    genel_puan, temizlik_puani, konfor_puani, konum_puani, personel_puani, fiyat_performans_puani,
    memnuniyet_seviyesi, genel_puan_10, puan_oda_10, puan_konum_10, puan_fiyat_10, puan_personel_10
)
VALUES
(46, 101, N'Balayı için sakin ve şık', N'Hizmet kalitesi iyi, oda dekorasyonu çok hoştu. Merkeze yakın olup yine de sakin kalabilen bir tesis.', N'Dekorasyon, sakinlik, personel', N'Akşam ikramları daha zengin olabilir', '2026-04-14', N'Balayı', N'Aile Suiti', 3, N'Çift', 9, 9, 10, 9, 9, 8, 9, 9, 10, 9, 8, 9);

INSERT INTO @reviewSeeds
(
    otel_id, kullanici_id, yorum_basligi, yorum_metni, olumlu_yanlar, olumsuz_yanlar,
    konaklama_tarihi, konaklama_turu, kaldigi_oda_tipi, gece_sayisi, seyahat_profili,
    genel_puan, temizlik_puani, konfor_puani, konum_puani, personel_puani, fiyat_performans_puani,
    memnuniyet_seviyesi, genel_puan_10, puan_oda_10, puan_konum_10, puan_fiyat_10, puan_personel_10
)
VALUES
(47, 103, N'Deniz tatili için başarılı', N'Plaj erişimi kolaydı ve oda ferah hissettirdi. Havuz alanı da gün içinde keyifliydi.', N'Plaj, havuz, ferah oda', N'Yoğun saatlerde restoranda sıra oluşabiliyor', '2026-04-07', N'Tatil', N'Standart Oda', 3, N'Aile', 9, 9, 9, 9, 8, 9, 9, 9, 9, 9, 9, 8);

INSERT INTO @reviewSeeds
(
    otel_id, kullanici_id, yorum_basligi, yorum_metni, olumlu_yanlar, olumsuz_yanlar,
    konaklama_tarihi, konaklama_turu, kaldigi_oda_tipi, gece_sayisi, seyahat_profili,
    genel_puan, temizlik_puani, konfor_puani, konum_puani, personel_puani, fiyat_performans_puani,
    memnuniyet_seviyesi, genel_puan_10, puan_oda_10, puan_konum_10, puan_fiyat_10, puan_personel_10
)
VALUES
(47, 102, N'Fiyat performans dengesi iyi', N'Bodrum için konumu iyi, personel çözüm odaklıydı. Oda temizliği günlük olarak düzenli yapıldı.', N'Konum, fiyat performans, temizlik', N'Akşam etkinlikleri biraz gürültülü olabiliyor', '2026-04-11', N'Tatil', N'Deluxe Oda', 2, N'Arkadaş', 8, 9, 8, 9, 8, 9, 8, 8, 8, 9, 9, 8);

INSERT INTO @reviewSeeds
(
    otel_id, kullanici_id, yorum_basligi, yorum_metni, olumlu_yanlar, olumsuz_yanlar,
    konaklama_tarihi, konaklama_turu, kaldigi_oda_tipi, gece_sayisi, seyahat_profili,
    genel_puan, temizlik_puani, konfor_puani, konum_puani, personel_puani, fiyat_performans_puani,
    memnuniyet_seviyesi, genel_puan_10, puan_oda_10, puan_konum_10, puan_fiyat_10, puan_personel_10
)
VALUES
(48, 101, N'Kış kaçamağı için çok uygun', N'Orman manzarası ve sessizlik çok iyiydi. Oda sıcaklığı dengeliydi, kahvaltı ürünleri tazeydi.', N'Doğa, sessizlik, kahvaltı', N'Spa alanı biraz daha büyük olabilir', '2026-04-05', N'Tatil', N'Deluxe Oda', 2, N'Çift', 9, 9, 9, 8, 9, 8, 9, 9, 9, 8, 8, 9);

INSERT INTO @reviewSeeds
(
    otel_id, kullanici_id, yorum_basligi, yorum_metni, olumlu_yanlar, olumsuz_yanlar,
    konaklama_tarihi, konaklama_turu, kaldigi_oda_tipi, gece_sayisi, seyahat_profili,
    genel_puan, temizlik_puani, konfor_puani, konum_puani, personel_puani, fiyat_performans_puani,
    memnuniyet_seviyesi, genel_puan_10, puan_oda_10, puan_konum_10, puan_fiyat_10, puan_personel_10
)
VALUES
(48, 103, N'Dinlenmek için sakin bir tesis', N'Kartepe tarafında kafa dinlemek isteyenler için iyi bir seçenek. Personel ilgili ve oda konforu yeterliydi.', N'İlgili ekip, sakin ortam, temiz oda', N'Ulaşım aracı olmayanlar için transfer eklenebilir', '2026-04-10', N'Tatil', N'Standart Oda', 1, N'Tek', 8, 8, 8, 8, 9, 8, 8, 8, 8, 8, 8, 9);

INSERT INTO @reviewSeeds
(
    otel_id, kullanici_id, yorum_basligi, yorum_metni, olumlu_yanlar, olumsuz_yanlar,
    konaklama_tarihi, konaklama_turu, kaldigi_oda_tipi, gece_sayisi, seyahat_profili,
    genel_puan, temizlik_puani, konfor_puani, konum_puani, personel_puani, fiyat_performans_puani,
    memnuniyet_seviyesi, genel_puan_10, puan_oda_10, puan_konum_10, puan_fiyat_10, puan_personel_10
)
VALUES
(49, 102, N'Alaçatı ruhunu iyi yansıtıyor', N'Tesisin dekorasyonu ve konumu çok keyifliydi. Çarşıya yürüyerek ulaşmak büyük avantaj sağladı.', N'Dekorasyon, konum, huzurlu ortam', N'Oda içi aydınlatma biraz güçlendirilebilir', '2026-04-08', N'Tatil', N'Deluxe Oda', 2, N'Çift', 9, 9, 9, 10, 8, 8, 9, 9, 9, 10, 8, 8);

INSERT INTO @reviewSeeds
(
    otel_id, kullanici_id, yorum_basligi, yorum_metni, olumlu_yanlar, olumsuz_yanlar,
    konaklama_tarihi, konaklama_turu, kaldigi_oda_tipi, gece_sayisi, seyahat_profili,
    genel_puan, temizlik_puani, konfor_puani, konum_puani, personel_puani, fiyat_performans_puani,
    memnuniyet_seviyesi, genel_puan_10, puan_oda_10, puan_konum_10, puan_fiyat_10, puan_personel_10
)
VALUES
(49, 103, N'Hafta sonu için çok keyifliydi', N'Butik hissi güçlü, personel samimi ve kahvaltı lezzetliydi. Kısa konaklama için tekrar tercih edilebilir.', N'Kahvaltı, personel, merkezi konum', N'Otopark kapasitesi sınırlı', '2026-04-13', N'Tatil', N'Aile Suiti', 2, N'Arkadaş', 8, 8, 8, 9, 9, 8, 8, 8, 8, 9, 8, 9);

;WITH existing_counts AS
(
    SELECT
        otel_id,
        COUNT(*) AS yorum_sayisi
    FROM dbo.yorumlar
    WHERE onay_durumu = @approvedStatus
    GROUP BY otel_id
),
seed_ranked AS
(
    SELECT
        rs.*,
        ROW_NUMBER() OVER (PARTITION BY rs.otel_id ORDER BY rs.konaklama_tarihi, rs.kullanici_id) AS seed_order
    FROM @reviewSeeds rs
),
seed_to_insert AS
(
    SELECT sr.*
    FROM seed_ranked sr
    LEFT JOIN existing_counts ec ON ec.otel_id = sr.otel_id
    WHERE sr.seed_order <= (2 - ISNULL(ec.yorum_sayisi, 0))
      AND ISNULL(ec.yorum_sayisi, 0) < 2
)
INSERT INTO dbo.yorumlar
(
    otel_id, kullanici_id, rezervasyon_id, genel_puan, temizlik_puani, konfor_puani, konum_puani, personel_puani,
    fiyat_performans_puani, yorum_basligi, yorum_metni, olumlu_yanlar, olumsuz_yanlar, konaklama_tarihi, konaklama_turu,
    kaldigi_oda_tipi, gece_sayisi, dogrulanmis_konaklama, onay_durumu, onaylayan_admin_id, onay_tarihi, red_nedeni,
    faydali_oy_sayisi, faydasiz_oy_sayisi, rapor_sayisi, otel_yaniti, otel_yaniti_tarihi, yanitlayan_kullanici_id,
    yorum_gorselleri, anonim_mi, olusturulma_tarihi, guncellenme_tarihi, seyahat_profili, memnuniyet_seviyesi,
    genel_puan_10, puan_oda_10, puan_konum_10, puan_fiyat_10, puan_personel_10
)
SELECT
    sti.otel_id,
    sti.kullanici_id,
    NULL,
    sti.genel_puan,
    sti.temizlik_puani,
    sti.konfor_puani,
    sti.konum_puani,
    sti.personel_puani,
    sti.fiyat_performans_puani,
    sti.yorum_basligi,
    sti.yorum_metni,
    sti.olumlu_yanlar,
    sti.olumsuz_yanlar,
    sti.konaklama_tarihi,
    sti.konaklama_turu,
    sti.kaldigi_oda_tipi,
    sti.gece_sayisi,
    1,
    @approvedStatus,
    @adminUserId,
    SYSUTCDATETIME(),
    NULL,
    0,
    0,
    0,
    N'Geri bildiriminiz için teşekkür ederiz.',
    SYSUTCDATETIME(),
    @adminUserId,
    NULL,
    0,
    DATEADD(MINUTE, -5 * sti.seed_order, SYSUTCDATETIME()),
    SYSUTCDATETIME(),
    sti.seyahat_profili,
    sti.memnuniyet_seviyesi,
    sti.genel_puan_10,
    sti.puan_oda_10,
    sti.puan_konum_10,
    sti.puan_fiyat_10,
    sti.puan_personel_10
FROM seed_to_insert sti;

;WITH approved_review_stats AS
(
    SELECT
        y.otel_id,
        CAST(AVG(CAST(y.genel_puan_10 AS decimal(10,2))) / 1.0 AS decimal(10,2)) AS ortalama_puan_10luk,
        COUNT(*) AS yorum_sayisi,
        CAST(AVG(CAST(y.temizlik_puani AS decimal(10,2))) AS decimal(10,2)) AS ortalama_temizlik,
        CAST(AVG(CAST(y.konfor_puani AS decimal(10,2))) AS decimal(10,2)) AS ortalama_konfor,
        CAST(AVG(CAST(y.konum_puani AS decimal(10,2))) AS decimal(10,2)) AS ortalama_konum,
        CAST(AVG(CAST(y.personel_puani AS decimal(10,2))) AS decimal(10,2)) AS ortalama_personel,
        CAST(AVG(CAST(y.fiyat_performans_puani AS decimal(10,2))) AS decimal(10,2)) AS ortalama_fiyat
    FROM dbo.yorumlar y
    WHERE y.onay_durumu = @approvedStatus
    GROUP BY y.otel_id
)
UPDATE o
SET
    o.ortalama_puan = ars.ortalama_puan_10luk,
    o.toplam_yorum_sayisi = ars.yorum_sayisi,
    o.temizlik_puani = ars.ortalama_temizlik,
    o.konfor_puani = ars.ortalama_konfor,
    o.konum_puani = ars.ortalama_konum,
    o.personel_puani = ars.ortalama_personel,
    o.fiyat_performans_puani = ars.ortalama_fiyat
FROM dbo.oteller o
INNER JOIN approved_review_stats ars ON ars.otel_id = o.id
WHERE o.id BETWEEN 45 AND 49;
