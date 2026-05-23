/* dbo.OTEL_TIPLERI only: Turkce tip adi/aciklama guncelle + eksik tip ekle */
IF OBJECT_ID(N'dbo.OTEL_TIPLERI', N'U') IS NULL RETURN;

BEGIN TRAN;

IF OBJECT_ID('tempdb..#otel_tipi_tr_seed') IS NOT NULL DROP TABLE #otel_tipi_tr_seed;
CREATE TABLE #otel_tipi_tr_seed
(
    KOD nvarchar(60) NOT NULL,
    TIP_ADI nvarchar(100) NOT NULL,
    ACIKLAMA nvarchar(300) NULL,
    IKON_CLASS nvarchar(80) NULL
);

INSERT INTO #otel_tipi_tr_seed (KOD, TIP_ADI, ACIKLAMA, IKON_CLASS) VALUES
(N'APARTMENT',N'APART DAIRE',N'Kısa veya uzun konaklamaya uygun, tam donanımlı daire tipi tesis.',N'fa-building'),
(N'APART_HOTEL',N'APART OTEL',N'Otel hizmeti ile mutfaklı/apart ünite yapısını birleştiren konaklama türü.',N'fa-building-user'),
(N'BUNGALOW',N'BUNGALOV',N'Doğa içinde, bağımsız ve düşük katlı konaklama birimi.',N'fa-tree-city'),
(N'BOUTIQUE_HOTEL',N'BUTIK OTEL',N'Az odalı, özgün tasarımlı ve kişiselleştirilmiş hizmet sunan tesis.',N'fa-gem'),
(N'FARM_STAY',N'CIFTLIK KONAKLAMA',N'Kırsal deneyim odaklı, doğa ve yerel yaşam temalı tesis.',N'fa-tractor'),
(N'MOUNTAIN_HOTEL',N'DAG OTELI',N'Dağ ve yayla destinasyonlarına uygun konumlanan tesis.',N'fa-mountain'),
(N'SEASIDE_HOTEL',N'DENIZ OTELI',N'Sahil, plaj veya kıyı erişimi güçlü olan konaklama tesisi.',N'fa-umbrella-beach'),
(N'GLAMPING',N'GLAMPING',N'Lüks kamp deneyimi sunan, doğa ve konforu birleştiren tesis.',N'fa-campground'),
(N'AIRPORT_HOTEL',N'HAVAALANI OTELI',N'Havalimanına yakın veya bağlantılı konumdaki tesis.',N'fa-plane-departure'),
(N'HOSTEL',N'HOSTEL',N'Ekonomik, ortak alanlı veya kompakt odalı konaklama seçeneği.',N'fa-bed'),
(N'BUSINESS_HOTEL',N'IS OTELI',N'Kurumsal seyahat, toplantı ve iş amaçlı konaklama için optimize tesis.',N'fa-briefcase'),
(N'CAMP_SITE',N'KAMP ALANI',N'Çadır veya karavan konaklamasına uygun açık alan tesisi.',N'fa-tent'),
(N'SKI_HOTEL',N'KAYAK OTELI',N'Kış turizmi ve kayak aktivitelerine odaklı tesis.',N'fa-person-skiing'),
(N'GUESTHOUSE',N'KONUKEVI',N'Küçük ölçekli, samimi ve temel hizmet sunan konaklama türü.',N'fa-house-user'),
(N'CAVE_HOTEL',N'MAGARA OTEL',N'Kaya/mağara mimarisi ile özgün deneyim sunan tesis.',N'fa-landmark'),
(N'MOTEL',N'MOTEL',N'Yol üstü, kısa süreli ve pratik konaklama ihtiyacına uygun tesis.',N'fa-road'),
(N'HOTEL',N'OTEL',N'Standart veya kapsamlı hizmet sunan genel otel kategorisi.',N'fa-hotel'),
(N'PENSION',N'PANSIYON',N'Sade, ekonomik ve çoğunlukla aile işletmesi olan konaklama türü.',N'fa-house'),
(N'RESORT',N'RESORT',N'Tatil odaklı, geniş hizmet ve aktivite altyapısı sunan tesis.',N'fa-water-ladder'),
(N'RESIDENCE',N'REZIDANS',N'Servisli daire veya uzun konaklama odaklı rezidans tipi tesis.',N'fa-city'),
(N'CITY_HOTEL',N'SEHIR OTELI',N'Şehir merkezi veya iş bölgesinde konumlanan otel tipi.',N'fa-building'),
(N'SPA_HOTEL',N'SPA OTELI',N'SPA, wellness ve bakım hizmetleri güçlü konaklama tesisi.',N'fa-spa'),
(N'STONE_HOTEL',N'TAS OTEL',N'Taş mimari veya tarihi dokuya sahip konsept tesis.',N'fa-chess-rook'),
(N'HOLIDAY_VILLAGE',N'TATIL KOYU',N'Geniş alanda çoklu ünite ve aktivite sunan tatil kompleksi.',N'fa-person-shelter'),
(N'THERMAL_HOTEL',N'TERMAL OTEL',N'Termal su ve sağlık odaklı hizmet sunan tesis.',N'fa-hot-tub-person'),
(N'VILLA',N'VILLA',N'Aile veya grup konaklamasına uygun özel kullanım birimi.',N'fa-house-chimney'),
(N'URBAN_HOTEL',N'KENT OTELI',N'Şehir içi kısa veya iş konaklamaları için uygun tesis türü.',N'fa-city'),
(N'ECO_LODGE',N'EKOLOJIK LODGE',N'Sürdürülebilirlik ve çevre dostu işletme yaklaşımıyla çalışan tesis.',N'fa-leaf'),
(N'BED_AND_BREAKFAST',N'ODA KAHVALTI',N'Konaklama + kahvaltı paketine odaklı küçük ölçekli tesis.',N'fa-mug-hot'),
(N'LUXURY_HOTEL',N'LUKS OTEL',N'Üst segmentte premium hizmet ve deneyim sunan konaklama tesisi.',N'fa-crown'),
(N'DESIGN_HOTEL',N'TASARIM OTEL',N'Mimari ve iç mekân kimliğiyle öne çıkan konsept otel.',N'fa-pen-ruler'),
(N'CONVENTION_HOTEL',N'KONGRE OTELI',N'Toplantı, kongre ve etkinlik altyapısı güçlü kurumsal tesis.',N'fa-users-rectangle'),
(N'FAMILY_HOTEL',N'AILE OTELI',N'Aile seyahatine uygun, çocuk dostu hizmetler sunan tesis.',N'fa-people-roof'),
(N'ADULT_ONLY_HOTEL',N'YETISKIN OTELI',N'Yalnızca yetişkin misafir segmentine hizmet veren tesis.',N'fa-user-check'),
(N'ALL_INCLUSIVE_RESORT',N'HER SEY DAHIL RESORT',N'Her şey dahil paket modeli ile çalışan tatil tesisi.',N'fa-tags'),
(N'BUDGET_HOTEL',N'EKONOMIK OTEL',N'Temel konaklama ihtiyaçlarını uygun maliyetle sunan tesis.',N'fa-wallet'),
(N'BEACH_RESORT',N'PLAJ RESORT',N'Deniz-kum-güneş deneyimi odaklı resort tipi tesis.',N'fa-sun'),
(N'WELLNESS_RETREAT',N'WELLNESS TESISI',N'İyilik hali, dinlenme ve yenilenme programları sunan tesis.',N'fa-heart-pulse');

UPDATE t
SET
    t.TIP_ADI = s.TIP_ADI,
    t.ACIKLAMA = s.ACIKLAMA,
    t.IKON_CLASS = COALESCE(NULLIF(t.IKON_CLASS,N''), s.IKON_CLASS),
    t.AKTIF_MI = 1,
    t.GUNCELLENME_TARIHI = SYSUTCDATETIME()
FROM dbo.OTEL_TIPLERI t
INNER JOIN #otel_tipi_tr_seed s ON s.KOD = t.KOD;

INSERT INTO dbo.OTEL_TIPLERI
(
    KOD, TIP_ADI, ACIKLAMA, IKON_CLASS, SIRALAMA, AKTIF_MI, OLUSTURULMA_TARIHI, GUNCELLENME_TARIHI
)
SELECT
    s.KOD, s.TIP_ADI, s.ACIKLAMA, s.IKON_CLASS,
    COALESCE((SELECT MAX(SIRALAMA) + 1 FROM dbo.OTEL_TIPLERI), 1),
    1, SYSUTCDATETIME(), SYSUTCDATETIME()
FROM #otel_tipi_tr_seed s
WHERE NOT EXISTS (SELECT 1 FROM dbo.OTEL_TIPLERI t WHERE t.KOD = s.KOD);

COMMIT TRAN;
