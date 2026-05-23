/* dbo.KAMPANYALAR only: 10 yillik kurumsal kampanya seti + eksik kampanyalar */
IF OBJECT_ID(N'dbo.KAMPANYALAR', N'U') IS NULL RETURN;

BEGIN TRAN;

IF OBJECT_ID('tempdb..#kampanya_seed') IS NOT NULL DROP TABLE #kampanya_seed;
CREATE TABLE #kampanya_seed
(
    KAMPANYA_KODU nvarchar(50) NOT NULL,
    KAMPANYA_ADI nvarchar(200) NOT NULL,
    SEO_SLUG nvarchar(255) NOT NULL,
    SAYFA_URL nvarchar(255) NOT NULL,
    KISA_ACIKLAMA nvarchar(255) NOT NULL,
    DETAY_ACIKLAMA nvarchar(max) NOT NULL,
    TUR nvarchar(255) NOT NULL,
    INDIRIM_ORANI decimal(5,2) NULL,
    INDIRIM_TUTARI decimal(18,2) NULL,
    KAMPANYA_ETIKETI nvarchar(100) NULL,
    PROMO_BADGE nvarchar(100) NULL,
    KAMPANYA_RENK_KODU nvarchar(20) NULL,
    SIRALAMA int NOT NULL
);

INSERT INTO #kampanya_seed
(KAMPANYA_KODU,KAMPANYA_ADI,SEO_SLUG,SAYFA_URL,KISA_ACIKLAMA,DETAY_ACIKLAMA,TUR,INDIRIM_ORANI,INDIRIM_TUTARI,KAMPANYA_ETIKETI,PROMO_BADGE,KAMPANYA_RENK_KODU,SIRALAMA)
VALUES
(N'KMP-2026-YILBASI',N'YILBASI DONEMI KAMPANYASI',N'yilbasi-donemi-kampanyasi',N'/kampanyalar/yilbasi-donemi-kampanyasi',N'Yilbasi donemine ozel secili otellerde fiyat avantaji.',N'Yilbasi donemi icin platform genelinde kurumsal kampanya vitrini.',N'Yüzde İndirim',18,NULL,N'YILBASI',N'ONE CIKAN',N'#DC2626',10),
(N'KMP-2026-ANNELER',N'ANNELER GUNU KONAKLAMA KAMPANYASI',N'anneler-gunu-konaklama-kampanyasi',N'/kampanyalar/anneler-gunu-konaklama-kampanyasi',N'Anneler gunu doneminde secili tesislerde konaklama avantaji.',N'Anneler Gunu odakli, wellness ve sehir otellerini kapsayan kampanya seti.',N'Yüzde İndirim',15,NULL,N'ANNELER GUNU',N'DONEMSEL',N'#BE185D',20),
(N'KMP-2026-SEVGILILER',N'SEVGILILER GUNU KONAKLAMA KAMPANYASI',N'sevgililer-gunu-konaklama-kampanyasi',N'/kampanyalar/sevgililer-gunu-konaklama-kampanyasi',N'Cift konaklamalarina ozel secili tesis avantajlari.',N'Sevgililer Gunu donemi icin suit ve resort segmentli kampanya kurgusu.',N'Sabit İndirim',NULL,1250,N'SEVGILILER GUNU',N'DONEMSEL',N'#DB2777',30),
(N'KMP-2026-GECEYARISI',N'GECE SAATLERI FIYAT AVANTAJI',N'gece-saatleri-fiyat-avantaji',N'/kampanyalar/gece-saatleri-fiyat-avantaji',N'Belirli saat araliklarinda acilan sinirli sureli fiyatlar.',N'Son dakika satin alma davranisina uygun fiyat avantaji kampanyasi.',N'Son Dakika',12,NULL,N'GECE FIYATI',N'HIZLI SATIS',N'#0EA5E9',40),
(N'KMP-2026-ERKENREZ',N'ERKEN REZERVASYON PROGRAMI',N'erken-rezervasyon-programi',N'/kampanyalar/erken-rezervasyon-programi',N'Planli konaklamalar icin erken donem fiyat avantajlari.',N'Yuksek donemler oncesinde rezervasyon davranisini guclendiren kampanya.',N'Erken Rezervasyon',22,NULL,N'ERKEN REZERVASYON',N'PLANLI ALISVERIS',N'#2563EB',50),
(N'KMP-2026-HAFTASONU',N'HAFTA SONU KONAKLAMA FIRSATLARI',N'hafta-sonu-konaklama-firsatlari',N'/kampanyalar/hafta-sonu-konaklama-firsatlari',N'Kisa sureli hafta sonu konaklamalari icin secili oteller.',N'Hafta sonu doluluklarini destekleyen platform genel kampanya kurgusu.',N'Yüzde İndirim',14,NULL,N'HAFTA SONU',N'DONEMSEL',N'#0F766E',60),
(N'KMP-2026-HAVUZLU',N'HAVUZ VE REKREASYON OTELLERI KAMPANYASI',N'havuz-ve-rekreasyon-otelleri-kampanyasi',N'/kampanyalar/havuz-ve-rekreasyon-otelleri-kampanyasi',N'Havuz olanaklari guclu tesisler icin secili fiyatlar.',N'Resort ve yazlik tesis segmenti icin olusturulan surekli kampanya vitrini.',N'Yüzde İndirim',13,NULL,N'RESORT',N'POPULER',N'#0284C7',70),
(N'KMP-2026-BUTCE',N'EKONOMIK FIYAT SEGMENTI KAMPANYASI',N'ekonomik-fiyat-segmenti-kampanyasi',N'/kampanyalar/ekonomik-fiyat-segmenti-kampanyasi',N'Butce odakli konaklamalar icin secili tesisler.',N'Fiyat duyarliligi yuksek kullanicilar icin optimize edilmis kampanya yapisi.',N'Yüzde İndirim',10,NULL,N'EKONOMIK',N'SUREKLI',N'#334155',80),
(N'KMP-2026-AKILLI',N'FIYAT PERFORMANS OTELLERI KAMPANYASI',N'fiyat-performans-otelleri-kampanyasi',N'/kampanyalar/fiyat-performans-otelleri-kampanyasi',N'Fiyat ve hizmet dengesinde one cikan secili oteller.',N'Fiyat performans sinifinda listelenen tesislerin donemsel vitrini.',N'Yüzde İndirim',11,NULL,N'FIYAT PERFORMANS',N'SECKI',N'#1D4ED8',90),
(N'KMP-2026-SPA',N'SPA VE WELLNESS DONEMI',N'spa-ve-wellness-donemi',N'/kampanyalar/spa-ve-wellness-donemi',N'SPA ve wellness olanaklari guclu tesislerde secili fiyatlar.',N'Wellness odakli seyahat segmenti icin surekli gorunurluk kampanyasi.',N'Yüzde İndirim',16,NULL,N'WELLNESS',N'DONEMSEL',N'#7C3AED',100),
(N'KMP-2026-SEHIR',N'SEHIR OTELLERI KAMPANYASI',N'sehir-otelleri-kampanyasi',N'/kampanyalar/sehir-otelleri-kampanyasi',N'Merkezi konumdaki sehir otelleri icin secili teklifler.',N'Is ve kisa sureli konaklama segmentini hedefleyen sehir kampanyasi.',N'Yüzde İndirim',9,NULL,N'SEHIR',N'SUREKLI',N'#475569',110),
(N'KMP-2026-MOBIL',N'MOBIL REZERVASYON AVANTAJI',N'mobil-rezervasyon-avantaji',N'/kampanyalar/mobil-rezervasyon-avantaji',N'Mobil kanal uzerinden rezervasyonlara ozel secili avantajlar.',N'Mobil kullanici donusumunu arttirmaya yonelik platform kampanyasi.',N'Yüzde İndirim',8,NULL,N'MOBIL',N'DIJITAL',N'#0EA5E9',120),
(N'KMP-2026-UZUNKONAKLAMA',N'UZUN KONAKLAMA PROGRAMI',N'uzun-konaklama-programi',N'/kampanyalar/uzun-konaklama-programi',N'Uzun sureli konaklamalarda kademeli fiyat avantaji.',N'7 gece ve uzeri planlamalara odakli uzun konaklama kampanyasi.',N'Yüzde İndirim',17,NULL,N'UZUN KONAKLAMA',N'SUREKLI',N'#0F766E',130),
(N'KMP-2026-BAYRAM',N'BAYRAM DONEMI KONAKLAMA KAMPANYASI',N'bayram-donemi-konaklama-kampanyasi',N'/kampanyalar/bayram-donemi-konaklama-kampanyasi',N'Bayram tatili donemlerinde secili tesislerde fiyat avantaji.',N'Ramazan ve Kurban donemlerinde aktiflestirilen genel bayram kampanyasi.',N'Yüzde İndirim',15,NULL,N'BAYRAM',N'DONEMSEL',N'#B45309',140),
(N'KMP-2026-FLASH',N'ANLIK FIYAT AVANTAJI',N'anlik-fiyat-avantaji',N'/kampanyalar/anlik-fiyat-avantaji',N'Kisa sureli fiyat guncellemelerini one cikarir.',N'Dinamik fiyatlama kaynakli anlik satis firsatlarini vitrine tasir.',N'Son Dakika',11,NULL,N'ANLIK',N'HIZLI SATIS',N'#DC2626',150),
(N'KMP-2026-AYSONU',N'AY SONU KONAKLAMA AVANTAJI',N'ay-sonu-konaklama-avantaji',N'/kampanyalar/ay-sonu-konaklama-avantaji',N'Ay sonu donemlerinde secili tesislerde fiyat avantaji.',N'Aylik hedef donemlerine uygun ay sonu destek kampanyasi.',N'Yüzde İndirim',10,NULL,N'AY SONU',N'DONEMSEL',N'#1D4ED8',160),
(N'KMP-2026-SADIK',N'SADIK MISAFIR PROGRAMI',N'sadik-misafir-programi',N'/kampanyalar/sadik-misafir-programi',N'Tekrar rezervasyon yapan kullanicilar icin avantajlar.',N'Sadakat odakli musteri segmentinde tekrar satin alma kampanyasi.',N'Yüzde İndirim',12,NULL,N'SADAKAT',N'UYELIK',N'#7C3AED',170),
(N'KMP-2026-AILE',N'AILE KONAKLAMA KAMPANYASI',N'aile-konaklama-kampanyasi',N'/kampanyalar/aile-konaklama-kampanyasi',N'Aile odasi ve cocuk dostu tesislerde secili avantajlar.',N'Aile segmentli rezervasyonlar icin yil boyu kullanilan kampanya.',N'Yüzde İndirim',13,NULL,N'AILE',N'SUREKLI',N'#0891B2',180),
(N'KMP-2026-LUKS',N'LUKS SEGMENT KONAKLAMA KAMPANYASI',N'luks-segment-konaklama-kampanyasi',N'/kampanyalar/luks-segment-konaklama-kampanyasi',N'Premium segment tesislerde secili fiyat avantajlari.',N'Ust segmentte konumlanan tesislerin performans vitrini.',N'Yüzde İndirim',9,NULL,N'LUKS',N'PREMIUM',N'#9333EA',190),
(N'KMP-2026-SEYAHATPLAN',N'SEYAHAT PLANLAMA DESTEK KAMPANYASI',N'seyahat-planlama-destek-kampanyasi',N'/kampanyalar/seyahat-planlama-destek-kampanyasi',N'Tarih ve bolge bazli planlama yapan kullanicilar icin secili teklifler.',N'Planli rezervasyon akisini destekleyen genel kampanya kurgusu.',N'Yüzde İndirim',10,NULL,N'PLANLAMA',N'DESTEK',N'#2563EB',200),
(N'KMP-2026-KURUMSAL',N'KURUMSAL KONAKLAMA PROGRAMI',N'kurumsal-konaklama-programi',N'/kampanyalar/kurumsal-konaklama-programi',N'Firma kullanicilarina ozel konaklama fiyat avantajlari.',N'B2B rezervasyon akislarini destekleyen kurumsal kampanya seti.',N'Yüzde İndirim',14,NULL,N'KURUMSAL',N'B2B',N'#0F766E',210),
(N'KMP-2026-SONDAKIKA',N'SON DAKIKA REZERVASYON PROGRAMI',N'son-dakika-rezervasyon-programi',N'/kampanyalar/son-dakika-rezervasyon-programi',N'Yaklasan tarihlerde acilan secili fiyat avantajlari.',N'Son dakika doluluk yonetimi icin aktiflestirilen kampanya yapisi.',N'Son Dakika',13,NULL,N'SON DAKIKA',N'DINAMIK',N'#DC2626',220),

(N'KMP-SABIT-19MAYIS',N'19 MAYIS GENCLIK VE SPOR BAYRAMI KAMPANYASI',N'19-mayis-genclik-ve-spor-bayrami-kampanyasi',N'/kampanyalar/19-mayis-genclik-ve-spor-bayrami-kampanyasi',N'19 Mayis doneminde secili konaklamalarda kampanya avantaji.',N'19 Mayis haftasinda aktiflestirilen resmi tatil odakli kampanya.',N'Yüzde İndirim',19,NULL,N'19 MAYIS',N'RESMI TATIL',N'#1D4ED8',230),
(N'KMP-SABIT-BABALAR',N'BABALAR GUNU KONAKLAMA KAMPANYASI',N'babalar-gunu-konaklama-kampanyasi',N'/kampanyalar/babalar-gunu-konaklama-kampanyasi',N'Babalar Gunu doneminde secili tesislerde fiyat avantaji.',N'Babalar Gunu temali donemsel kampanya, sehir ve resort segmentini kapsar.',N'Yüzde İndirim',15,NULL,N'BABALAR GUNU',N'DONEMSEL',N'#0369A1',240),
(N'KMP-SABIT-23NISAN',N'23 NISAN DONEMI AILE KAMPANYASI',N'23-nisan-donemi-aile-kampanyasi',N'/kampanyalar/23-nisan-donemi-aile-kampanyasi',N'23 Nisan doneminde aile odakli secili tesis avantajlari.',N'23 Nisan doneminde aile segmentini destekleyen kampanya.',N'Yüzde İndirim',14,NULL,N'23 NISAN',N'RESMI TATIL',N'#0EA5E9',250),
(N'KMP-SABIT-30AGUSTOS',N'30 AGUSTOS DONEMI KAMPANYASI',N'30-agustos-donemi-kampanyasi',N'/kampanyalar/30-agustos-donemi-kampanyasi',N'30 Agustos doneminde secili otellerde fiyat avantaji.',N'Yaz sonu resmi tatil talebini destekleyen kampanya.',N'Yüzde İndirim',16,NULL,N'30 AGUSTOS',N'RESMI TATIL',N'#1E40AF',260),
(N'KMP-SABIT-29EKIM',N'29 EKIM CUMHURIYET BAYRAMI KAMPANYASI',N'29-ekim-cumhuriyet-bayrami-kampanyasi',N'/kampanyalar/29-ekim-cumhuriyet-bayrami-kampanyasi',N'29 Ekim doneminde secili tesislerde konaklama avantaji.',N'Cumhuriyet Bayrami doneminde ic pazar talebini destekleyen kampanya.',N'Yüzde İndirim',17,NULL,N'29 EKIM',N'RESMI TATIL',N'#B91C1C',270),
(N'KMP-SABIT-BLACKFRIDAY',N'KASIM GLOBAL INDIRIM HAFTASI',N'kasim-global-indirim-haftasi',N'/kampanyalar/kasim-global-indirim-haftasi',N'Kasim ayi global indirim donemine ozel secili teklifler.',N'Coklu otel platformlarinda yaygin kullanilan global indirim temasi.',N'Yüzde İndirim',20,NULL,N'GLOBAL INDIRIM',N'BLACK FRIDAY',N'#111827',280),
(N'KMP-SABIT-CYBERMONDAY',N'DIJITAL SATIS PAZARTESI KAMPANYASI',N'dijital-satis-pazartesi-kampanyasi',N'/kampanyalar/dijital-satis-pazartesi-kampanyasi',N'Dijital kanal odakli haftalik indirim kampanyasi.',N'Online rezervasyon kanalini guclendiren global kampanya turu.',N'Yüzde İndirim',18,NULL,N'DIJITAL SATIS',N'CYBER MONDAY',N'#1F2937',290),
(N'KMP-SABIT-SOMESTR',N'SOMESTR DONEMI KAMPANYASI',N'somestr-donemi-kampanyasi',N'/kampanyalar/somestr-donemi-kampanyasi',N'Somestr tatili doneminde secili tesislerde fiyat avantaji.',N'Okul tatili donemindeki aile ve sehir segmentini hedefleyen kampanya.',N'Yüzde İndirim',16,NULL,N'SOMESTR',N'DONEMSEL',N'#2563EB',300),
(N'KMP-SABIT-YAZSEZON',N'YAZ SEZONU KONAKLAMA KAMPANYASI',N'yaz-sezonu-konaklama-kampanyasi',N'/kampanyalar/yaz-sezonu-konaklama-kampanyasi',N'Yaz sezonu boyunca secili resort tesislerde avantajli fiyatlar.',N'Yaz talebini dengeleyen uzun sureli sezon kampanyasi.',N'Yüzde İndirim',15,NULL,N'YAZ SEZONU',N'SEZONLUK',N'#0EA5E9',310),
(N'KMP-SABIT-RAMAZAN',N'RAMAZAN BAYRAMI KONAKLAMA KAMPANYASI',N'ramazan-bayrami-konaklama-kampanyasi',N'/kampanyalar/ramazan-bayrami-konaklama-kampanyasi',N'Ramazan Bayrami doneminde secili tesislerde kampanya avantajlari.',N'Bayram doneminde talep yogunlugunu yonetmek icin tanimli kampanya.',N'Yüzde İndirim',15,NULL,N'RAMAZAN BAYRAMI',N'BAYRAM',N'#B45309',320),
(N'KMP-SABIT-KURBAN',N'KURBAN BAYRAMI KONAKLAMA KAMPANYASI',N'kurban-bayrami-konaklama-kampanyasi',N'/kampanyalar/kurban-bayrami-konaklama-kampanyasi',N'Kurban Bayrami doneminde secili tesislerde kampanya avantajlari.',N'Yuksek sezon bayram donemi icin kapsayici kampanya yapisi.',N'Yüzde İndirim',16,NULL,N'KURBAN BAYRAMI',N'BAYRAM',N'#92400E',330);

DECLARE @dBas datetime2 = '2026-01-01T00:00:00';
DECLARE @dBit datetime2 = '2035-12-31T23:59:59';

UPDATE t
SET
    t.KAMPANYA_ADI = s.KAMPANYA_ADI,
    t.SEO_SLUG = s.SEO_SLUG,
    t.SAYFA_URL = s.SAYFA_URL,
    t.KAMPANYA_ACIKLAMASI = s.KAMPANYA_ADI + N' icin kurumsal kampanya gorunurlugu.',
    t.KISA_ACIKLAMA = s.KISA_ACIKLAMA,
    t.DETAY_ACIKLAMA = s.DETAY_ACIKLAMA,
    t.TUR = s.TUR,
    t.INDIRIM_ORANI = s.INDIRIM_ORANI,
    t.INDIRIM_TUTARI = s.INDIRIM_TUTARI,
    t.MAKSIMUM_INDIRIM_TUTARI = COALESCE(t.MAKSIMUM_INDIRIM_TUTARI, 5000),
    t.MINIMUM_SEPET_TUTARI = COALESCE(t.MINIMUM_SEPET_TUTARI, 0),
    t.HEDEF_OTEL_TURU = COALESCE(NULLIF(t.HEDEF_OTEL_TURU,N''), N'Tümü'),
    t.HEDEF_KULLANICI_TURU = COALESCE(NULLIF(t.HEDEF_KULLANICI_TURU,N''), N'Tümü'),
    t.BASLANGIC_TARIHI = @dBas,
    t.BITIS_TARIHI = @dBit,
    t.MINIMUM_GECELEME = COALESCE(t.MINIMUM_GECELEME, 1),
    t.MAKSIMUM_GECELEME = COALESCE(t.MAKSIMUM_GECELEME, 14),
    t.KULLANICI_BASINA_LIMIT = COALESCE(t.KULLANICI_BASINA_LIMIT, 1),
    t.KULLANILAN_ADET = COALESCE(t.KULLANILAN_ADET, 0),
    t.GOSTERIM_ADEDI = COALESCE(t.GOSTERIM_ADEDI, 0),
    t.AKTIF_MI = 1,
    t.GORUNURLUK_DURUMU = N'Yayında',
    t.PARTNER_KATILIM_ACIK = 1,
    t.PARTNER_KATILIM_BASLANGIC = @dBas,
    t.PARTNER_KATILIM_BITIS = @dBit,
    t.OTOMATIK_SONA_ERSIN = 1,
    t.ONE_CIKAN_KAMPANYA = CASE WHEN s.SIRALAMA <= 80 THEN 1 ELSE 0 END,
    t.SIRALAMA = s.SIRALAMA,
    t.AKTIF_SAYFA_VITRINI = 1,
    t.META_TITLE = s.KAMPANYA_ADI + N' | Otelturizm',
    t.META_DESCRIPTION = s.KISA_ACIKLAMA,
    t.CANONICAL_URL = N'https://otelturizm.com' + s.SAYFA_URL,
    t.KAMPANYA_ETIKETI = s.KAMPANYA_ETIKETI,
    t.PROMO_BADGE = s.PROMO_BADGE,
    t.KAMPANYA_RENK_KODU = s.KAMPANYA_RENK_KODU,
    t.LISTELEME_BASLIGI = s.KAMPANYA_ADI,
    t.LISTELEME_ACIKLAMASI = s.KISA_ACIKLAMA,
    t.GUNCELLENME_TARIHI = SYSUTCDATETIME()
FROM dbo.KAMPANYALAR t
INNER JOIN #kampanya_seed s ON s.KAMPANYA_KODU = t.KAMPANYA_KODU;

INSERT INTO dbo.KAMPANYALAR
(
    KAMPANYA_KODU,KAMPANYA_ADI,SEO_SLUG,SAYFA_URL,KAMPANYA_ACIKLAMASI,KISA_ACIKLAMA,DETAY_ACIKLAMA,
    TUR,INDIRIM_ORANI,INDIRIM_TUTARI,MAKSIMUM_INDIRIM_TUTARI,MINIMUM_SEPET_TUTARI,
    HEDEF_OTEL_TURU,HEDEF_KULLANICI_TURU,
    BASLANGIC_TARIHI,BITIS_TARIHI,MINIMUM_GECELEME,MAKSIMUM_GECELEME,KULLANICI_BASINA_LIMIT,KULLANILAN_ADET,GOSTERIM_ADEDI,
    AKTIF_MI,GORUNURLUK_DURUMU,PARTNER_KATILIM_ACIK,PARTNER_KATILIM_BASLANGIC,PARTNER_KATILIM_BITIS,OTOMATIK_SONA_ERSIN,
    ONE_CIKAN_KAMPANYA,SIRALAMA,AKTIF_SAYFA_VITRINI,BANNER_GORSELI,HERO_GORSELI,KART_GORSELI,MOBIL_GORSEL,
    META_TITLE,META_DESCRIPTION,CANONICAL_URL,KAMPANYA_ETIKETI,PROMO_BADGE,KAMPANYA_RENK_KODU,LISTELEME_BASLIGI,LISTELEME_ACIKLAMASI,
    OLUSTURULMA_TARIHI,GUNCELLENME_TARIHI
)
SELECT
    s.KAMPANYA_KODU,s.KAMPANYA_ADI,s.SEO_SLUG,s.SAYFA_URL,s.KAMPANYA_ADI + N' icin kurumsal kampanya gorunurlugu.',s.KISA_ACIKLAMA,s.DETAY_ACIKLAMA,
    s.TUR,s.INDIRIM_ORANI,s.INDIRIM_TUTARI,5000,0,
    N'Tümü',N'Tümü',
    @dBas,@dBit,1,14,1,0,0,
    1,N'Yayında',1,@dBas,@dBit,1,
    CASE WHEN s.SIRALAMA <= 80 THEN 1 ELSE 0 END,s.SIRALAMA,1,
    N'/uploads/demo/campaigns/yilbasi-ozel/campaign-hero.jpg',N'/uploads/demo/campaigns/yilbasi-ozel/campaign-hero.jpg',N'/uploads/demo/campaigns/yilbasi-ozel/campaign-hero.jpg',N'/uploads/demo/campaigns/yilbasi-ozel/campaign-hero.jpg',
    s.KAMPANYA_ADI + N' | Otelturizm',s.KISA_ACIKLAMA,N'https://otelturizm.com' + s.SAYFA_URL,s.KAMPANYA_ETIKETI,s.PROMO_BADGE,s.KAMPANYA_RENK_KODU,s.KAMPANYA_ADI,s.KISA_ACIKLAMA,
    SYSUTCDATETIME(),SYSUTCDATETIME()
FROM #kampanya_seed s
WHERE NOT EXISTS (SELECT 1 FROM dbo.KAMPANYALAR t WHERE t.KAMPANYA_KODU = s.KAMPANYA_KODU);

COMMIT TRAN;
