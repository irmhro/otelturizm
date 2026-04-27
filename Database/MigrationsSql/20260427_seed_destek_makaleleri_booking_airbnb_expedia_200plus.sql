/*
  2026-04-27
  Destek makaleleri havuzunu Booking / Airbnb / Expedia odaklı 200+ içerikle genişletir.
  Hedef: partner, firma, satış, destek ve operasyon ekiplerinin soru havuzunu doldurmak.
*/

SET NOCOUNT ON;

DECLARE @CategorySeed TABLE
(
    kategori_adi VARCHAR(120) NOT NULL,
    seo_slug VARCHAR(150) NOT NULL,
    kategori_ikon VARCHAR(80) NOT NULL,
    kisa_aciklama VARCHAR(255) NULL,
    renk_kodu VARCHAR(20) NOT NULL,
    siralama INT NOT NULL
);

INSERT INTO @CategorySeed (kategori_adi, seo_slug, kategori_ikon, kisa_aciklama, renk_kodu, siralama)
VALUES
('Kanal Entegrasyonu', 'kanal-entegrasyonu', 'fa-plug', 'Kanal eşleşmeleri ve entegrasyon yönetimi.', '#2563EB', 11),
('Fiyat ve Komisyon', 'fiyat-ve-komisyon', 'fa-percent', 'Fiyat paritesi, komisyon ve marj yönetimi.', '#0F766E', 12),
('Rezervasyon Operasyonu', 'rezervasyon-operasyonu', 'fa-calendar-check', 'İptal, no-show, tarih değişikliği ve müsaitlik.', '#16A34A', 13),
('Ödeme ve Mutabakat', 'odeme-ve-mutabakat', 'fa-file-invoice-dollar', 'Ödeme akışı, fatura ve tahsilat mutabakatı.', '#B45309', 14),
('İçerik ve Görsel Kalite', 'icerik-ve-gorsel-kalite', 'fa-image', 'Listeleme metinleri, görseller ve içerik kalitesi.', '#7C3AED', 15),
('Puan ve Yorum Yönetimi', 'puan-ve-yorum-yonetimi', 'fa-star', 'Misafir puanı, yorum ve itibar yönetimi.', '#E11D48', 16),
('Kampanya ve Görünürlük', 'kampanya-ve-gorunurluk', 'fa-bullhorn', 'Promosyon, vitrin ve görünürlük optimizasyonu.', '#DB2777', 17),
('Güvenlik ve Sahtecilik', 'guvenlik-ve-sahtecilik', 'fa-shield-halved', 'Dolandırıcılık, sahte rezervasyon ve hesap güvenliği.', '#334155', 18),
('Firma Rezervasyonları', 'firma-rezervasyonlari', 'fa-building', 'Kurumsal rezervasyon, toplu konaklama ve faturalama.', '#1D4ED8', 19),
('Satış Operasyonu', 'satis-operasyonu', 'fa-chart-line', 'Satış ekibi teklif, dönüşüm ve takip akışları.', '#F59E0B', 20);

MERGE INTO dbo.destek_kategorileri AS target
USING @CategorySeed AS source
ON target.seo_slug = source.seo_slug
WHEN MATCHED THEN
    UPDATE SET
        target.kategori_adi = source.kategori_adi,
        target.kategori_ikon = source.kategori_ikon,
        target.kisa_aciklama = source.kisa_aciklama,
        target.renk_kodu = source.renk_kodu,
        target.siralama = source.siralama,
        target.durum = 1,
        target.guncellenme_tarihi = SYSUTCDATETIME()
WHEN NOT MATCHED THEN
    INSERT (kategori_adi, seo_slug, kategori_ikon, kisa_aciklama, renk_kodu, siralama, durum, olusturulma_tarihi, guncellenme_tarihi)
    VALUES (source.kategori_adi, source.seo_slug, source.kategori_ikon, source.kisa_aciklama, source.renk_kodu, source.siralama, 1, SYSUTCDATETIME(), SYSUTCDATETIME());

DECLARE @Platforms TABLE
(
    platform_key VARCHAR(20) NOT NULL,
    platform_name NVARCHAR(40) NOT NULL,
    base_order INT NOT NULL
);

INSERT INTO @Platforms (platform_key, platform_name, base_order)
VALUES
('booking', N'Kanal', 1),
('airbnb', N'Kanal', 2),
('expedia', N'Kanal', 3);

DECLARE @Audiences TABLE
(
    audience_key VARCHAR(20) NOT NULL,
    audience_name NVARCHAR(40) NOT NULL,
    base_order INT NOT NULL
);

INSERT INTO @Audiences (audience_key, audience_name, base_order)
VALUES
('partner', N'Partner Ekibi', 1),
('firma', N'Firma Ekibi', 2),
('satis', N'Satış Ekibi', 3),
('destek', N'Destek Ekibi', 4),
('operasyon', N'Operasyon Ekibi', 5);

DECLARE @Templates TABLE
(
    template_order INT NOT NULL,
    category_slug VARCHAR(150) NOT NULL,
    title_pattern NVARCHAR(180) NOT NULL,
    summary_pattern NVARCHAR(300) NOT NULL,
    content_pattern NVARCHAR(MAX) NOT NULL,
    ikon VARCHAR(80) NOT NULL,
    one_cikan_mi BIT NOT NULL
);

INSERT INTO @Templates (template_order, category_slug, title_pattern, summary_pattern, content_pattern, ikon, one_cikan_mi)
VALUES
(1, 'fiyat-ve-komisyon', N'{platform} fiyat paritesi: {audience} kontrol listesi', N'Fiyat farkı, parite ihlali ve gelir kaybını önleme adımları.', N'{platform} üzerinde fiyat paritesi yönetimi için {audience} şu kontrol adımlarını uygular: 1) Platform fiyatı ile panel fiyatını günlük karşılaştırın. 2) Vergi, kahvaltı ve hizmet dahil-hariç farklarını aynı standarda çekin. 3) Oda tipi bazında minimum marj eşiklerini tanımlayın. 4) Parite ihlali tespitinde düzeltme SLA süresini 15 dakika altında tutun.', 'fa-scale-balanced', 1),
(2, 'fiyat-ve-komisyon', N'{platform} komisyon hesabı: {audience} mutabakat rehberi', N'Komisyon oranı, net gelir ve kesinti kalemlerini doğrulama akışı.', N'{audience}, {platform} komisyon mutabakatında rezervasyon bazlı brüt tutar, komisyon oranı, iade etkisi ve vergi kalemlerini karşılaştırır. Ay sonu kapanışta uyuşmayan satırlar için rezervasyon numarası, konaklama tarihi ve ödeme statüsü ile destek kaydı açılır.', 'fa-percent', 1),
(3, 'rezervasyon-operasyonu', N'{platform} iptal ve no-show yönetimi: {audience}', N'İptal politikası, no-show cezası ve misafir iletişim adımları.', N'{platform} rezervasyonlarında {audience}, iptal politikası eşleşmesini kontrol eder, no-show olayını 24 saat içinde raporlar ve misafir iletişim logunu kaydeder. Haksız kesinti itirazlarında panel kayıtları ve zaman damgası ile kanıt dosyası oluşturulur.', 'fa-ban', 1),
(4, 'rezervasyon-operasyonu', N'{platform} oda envanteri senkronu: {audience}', N'Overbooking riskini azaltan stok senkron adımları.', N'{audience}, {platform} kanalındaki stok yönetiminde satışı kapalı günleri, minimum gece kuralını ve kanal bazlı allotment limitini günde en az 3 kez doğrular. Senkron gecikmesinde manuel stop-sell kuralı devreye alınır.', 'fa-bed', 1),
(5, 'rezervasyon-operasyonu', N'{platform} minimum kalış ve stop-sell ayarı: {audience}', N'Tarih bazlı minimum gece ve satışa kapama kuralları.', N'{audience}, talep yoğunluğu olan tarihlerde {platform} için minimum kalış, check-in/check-out kısıtı ve stop-sell parametrelerini sezon takvimine göre günceller. Kural değişikliği sonrası 30 dakika içinde ilk rezervasyon etkisi izlenir.', 'fa-calendar-days', 0),
(6, 'odeme-ve-mutabakat', N'{platform} ödeme akışı ve vade takibi: {audience}', N'Ödeme tipleri, tahsilat tarihi ve vade kontrol şablonu.', N'{platform} ödeme akışında {audience}, ödeme modelini (ön ödemeli / tesiste ödeme), vade tarihini ve kesinti kodunu rezervasyon satırı ile eşleştirir. Vade aşımı durumunda otomatik hatırlatma ve eskalasyon akışı uygulanır.', 'fa-credit-card', 0),
(7, 'odeme-ve-mutabakat', N'{platform} fatura ve vergi mutabakatı: {audience}', N'Fatura kesimi, KDV-konaklama vergisi ve para birimi kontrolü.', N'{audience}, {platform} kaynaklı rezervasyonlarda faturayı para birimi, vergi oranı ve geceleme sayısı ile doğrular. Kurumsal rezervasyonlarda firma vergi numarası ve e-fatura alanları zorunlu tutulur.', 'fa-file-invoice', 0),
(8, 'icerik-ve-gorsel-kalite', N'{platform} listeleme içerik kalitesi: {audience}', N'Başlık, açıklama, olanak ve görsel kalite standardı.', N'{platform} listeleme performansı için {audience}, oda başlıklarının netliğini, açıklama metinlerinin tutarlılığını, görsel çözünürlüğünü ve zorunlu olanak alanlarını haftalık olarak kontrol eder. Eksik içerik tespitinde düzeltme görevi açılır.', 'fa-image', 0),
(9, 'puan-ve-yorum-yonetimi', N'{platform} puan ve yorum yönetimi: {audience}', N'Yorum yanıt SLA, puan düşüş analizi ve aksiyon planı.', N'{audience}, {platform} değerlendirmelerinde puan düşüşünü konu etiketlerine ayırır (temizlik, iletişim, konum, fiyat/performans). Kritik yorumlara 12 saat içinde yanıt verilir ve aksiyon sonucu izlenir.', 'fa-star', 0),
(10, 'kampanya-ve-gorunurluk', N'{platform} kampanya görünürlüğü: {audience}', N'Promosyon kuralları ve görünürlük metrikleri.', N'{audience}, {platform} kampanyalarında indirim etiketi, tarih kapsamı ve uygun oda tipini doğrular. Kampanya sonrası dönüşüm oranı, ADR etkisi ve doluluk artışı haftalık raporlanır.', 'fa-bullhorn', 0),
(11, 'firma-rezervasyonlari', N'{platform} kurumsal rezervasyon süreci: {audience}', N'Firma sözleşmeli rezervasyonlarda operasyon adımları.', N'{audience}, {platform} kanalından gelen firma rezervasyonlarında şirket bilgisi doğrulama, toplu oda dağılımı ve ödeme/fatura eşleşmesini tamamlar. Rezervasyon notları satış ve operasyon ekipleriyle paylaşılır.', 'fa-building', 0),
(12, 'satis-operasyonu', N'{platform} satış ekibi teklif süreci: {audience}', N'Tekliften rezervasyona dönüşüm adımları ve izleme.', N'{audience}, {platform} kaynaklı taleplerde teklif yanıt süresi, fiyat revizyon adedi ve dönüşüm oranını takip eder. Düşük dönüşümlü segmentler için haftalık fiyat optimizasyon önerisi hazırlanır.', 'fa-chart-line', 0),
(13, 'kanal-entegrasyonu', N'{platform} API / channel manager eşleşmesi: {audience}', N'Kanal yöneticisi bağlantısı, hata kodu ve retry akışı.', N'{audience}, {platform} entegrasyonunda oda kodu eşleşmesi, fiyat planı eşleşmesi ve webhook teslimat loglarını izler. Hata kodlarında retry politikası uygulanır, başarısız kayıtlar manuel kuyruğa alınır.', 'fa-plug', 0),
(14, 'guvenlik-ve-sahtecilik', N'{platform} sahte rezervasyon ve güvenlik kontrolü: {audience}', N'Fraud sinyalleri, hesap güvenliği ve doğrulama adımları.', N'{audience}, {platform} rezervasyonlarında şüpheli işlemleri (aynı kart ile seri rezervasyon, tutarsız IP/ülke, sahte iletişim bilgisi) işaretler. Riskli kayıtlar için manuel doğrulama ve güvenlik notu zorunludur.', 'fa-shield-halved', 0);

;WITH prepared AS
(
    SELECT
        p.platform_key,
        p.platform_name,
        a.audience_key,
        a.audience_name,
        t.template_order,
        t.category_slug,
        LEFT(REPLACE(REPLACE(t.title_pattern, N'{platform}', p.platform_name), N'{audience}', a.audience_name), 180) AS baslik,
        LEFT(REPLACE(REPLACE(t.summary_pattern, N'{platform}', p.platform_name), N'{audience}', a.audience_name), 300) AS ozet,
        REPLACE(REPLACE(t.content_pattern, N'{platform}', p.platform_name), N'{audience}', a.audience_name) AS icerik,
        t.ikon,
        t.one_cikan_mi,
        ((p.base_order * 1000) + (a.base_order * 100) + t.template_order) AS siralama
    FROM @Platforms p
    CROSS JOIN @Audiences a
    CROSS JOIN @Templates t
),
final_rows AS
(
    SELECT
        dk.id AS destek_kategori_id,
        pr.baslik,
        CONCAT(pr.platform_key, '-', pr.audience_key, '-makale-', RIGHT(CONCAT('000', pr.template_order), 3)) AS seo_slug,
        pr.ozet,
        pr.icerik,
        pr.ikon,
        pr.one_cikan_mi,
        pr.siralama
    FROM prepared pr
    INNER JOIN dbo.destek_kategorileri dk ON dk.seo_slug = pr.category_slug
)
INSERT INTO dbo.destek_makaleleri
(
    destek_kategori_id,
    baslik,
    seo_slug,
    ozet,
    icerik,
    ikon,
    one_cikan_mi,
    yardim_merkezinde_goster,
    siralama,
    durum,
    olusturulma_tarihi,
    guncellenme_tarihi
)
SELECT
    fr.destek_kategori_id,
    fr.baslik,
    fr.seo_slug,
    fr.ozet,
    fr.icerik,
    fr.ikon,
    fr.one_cikan_mi,
    1,
    fr.siralama,
    1,
    SYSUTCDATETIME(),
    SYSUTCDATETIME()
FROM final_rows fr
WHERE NOT EXISTS
(
    SELECT 1
    FROM dbo.destek_makaleleri dm
    WHERE dm.seo_slug = fr.seo_slug
);

SELECT
    COUNT(*) AS toplam_ota_makale
FROM dbo.destek_makaleleri
WHERE seo_slug LIKE 'booking-%-makale-%'
   OR seo_slug LIKE 'airbnb-%-makale-%'
   OR seo_slug LIKE 'expedia-%-makale-%';
