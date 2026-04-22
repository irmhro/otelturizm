SET NOCOUNT ON;

DECLARE @PublishedHotelId BIGINT = (
    SELECT TOP (1) id
    FROM oteller
    WHERE yayin_durumu LIKE N'Yay%'
      AND onay_durumu LIKE N'Onaylan%'
    ORDER BY COALESCE(one_cikan_otel, 0) DESC, id DESC
);

IF OBJECT_ID('tempdb..#campaign_assets') IS NOT NULL
    DROP TABLE #campaign_assets;

CREATE TABLE #campaign_assets
(
    seo_slug NVARCHAR(200) NOT NULL PRIMARY KEY,
    image_url NVARCHAR(400) NOT NULL,
    short_description NVARCHAR(280) NOT NULL,
    listing_title NVARCHAR(200) NOT NULL,
    listing_description NVARCHAR(280) NOT NULL
);

INSERT INTO #campaign_assets (seo_slug, image_url, short_description, listing_title, listing_description)
VALUES
    (N'yilbasi-ozel', N'/uploads/demo/campaigns/yilbasi-ozel/campaign-hero.png', N'Yeni yila secili otellerde canli kampanya vitrini ile girin.', N'Yilbasi Ozel Oteller', N'Yilbasi temasina dahil secili otelleri il, ilce ve fiyat bazinda listeleyin.'),
    (N'sevgililer-gunu-romantik-kacis', N'/uploads/demo/campaigns/sevgililer-gunu-romantik-kacis/campaign-hero.png', N'Romantik kacislar ve ozel suit secimleri tek listede.', N'Sevgililer Gunu Seckisi', N'Sevgililer gunu kampanyasina dahil secili otelleri bolge bazinda karsilastirin.'),
    (N'erken-rezervasyon-avantaji', N'/uploads/demo/campaigns/erken-rezervasyon-avantaji/campaign-hero.png', N'Planini erkenden yapan misafirler icin avantajli secenekler.', N'Erken Rezervasyon Otelleri', N'Erken rezervasyon kampanyasina katilan secili otelleri hizli filtreleyin.'),
    (N'akilli-fiyat-seckisi', N'/uploads/demo/campaigns/akilli-fiyat-seckisi/campaign-hero.png', N'Fiyat-performans dengesi yuksek secili tesisler vitrinde.', N'Akilli Fiyat Seckisi', N'Akilli fiyat vitrinine dahil otelleri fiyat ve bolge bazinda filtreleyin.'),
    (N'bayram-tatili-seckisi', N'/uploads/demo/campaigns/bayram-tatili-seckisi/campaign-hero.png', N'Bayram donemine uygun secili oteller burada.', N'Bayram Tatili Seckisi', N'Bayram kampanyasina katilan otelleri il, ilce ve fiyat araligina gore bulun.'),
    (N'flash-indirim', N'/uploads/demo/campaigns/flash-indirim/campaign-hero.png', N'Kisa sureli fiyat dususlerini tek ekranda takip edin.', N'Flash Indirim Otelleri', N'Flash indirim kampanyasina dahil secili otelleri hizlica listeleyin.'),
    (N'ay-sonu-ozel', N'/uploads/demo/campaigns/ay-sonu-ozel/campaign-hero.png', N'Ay sonu planlari icin secili tesislerde hizli kampanya secimi.', N'Ay Sonu Ozel Oteller', N'Ay sonu kampanyasina dahil otelleri konuma ve fiyata gore filtreleyin.'),
    (N'ultra-luks-secki', N'/uploads/demo/campaigns/ultra-luks-secki/campaign-hero.png', N'Premium deneyim isteyen misafirler icin secili oteller.', N'Ultra Luks Seckisi', N'Ultra luks vitrinine dahil secili otelleri tek ekranda inceleyin.'),
    (N'anneler-gunu-kacamagi', N'/uploads/demo/campaigns/anneler-gunu-kacamagi/campaign-hero.png', N'Wellness ve rahatlama odakli secili oteller burada.', N'Anneler Gunu Kacamagi', N'Anneler gunu kampanyasindaki secili otelleri il ve ilce bazinda bulun.'),
    (N'gece-yarisi-flas-fiyat', N'/uploads/demo/campaigns/gece-yarisi-flas-fiyat/campaign-hero.png', N'Gece acilan ozel fiyatlari secili tesislerde yakalayin.', N'Gece Yarisi Fiyatlari', N'Gece yarisi fiyat kampanyasina dahil otelleri hizla karsilastirin.'),
    (N'hafta-sonu-firsatlari', N'/uploads/demo/campaigns/hafta-sonu-firsatlari/campaign-hero.png', N'Kisa kacamaklar icin secili tesisler burada.', N'Hafta Sonu Firsatlari', N'Hafta sonu kampanyasindaki otelleri il, ilce ve mahalle bazinda bulun.'),
    (N'havuz-keyfi-kampanyasi', N'/uploads/demo/campaigns/havuz-keyfi-kampanyasi/campaign-hero.png', N'Havuz keyfini one cikaran secili oteller vitrinde.', N'Havuz Keyfi Kampanyasi', N'Havuz keyfi kampanyasina katilan tesisleri fiyat ve puan bazinda filtreleyin.'),
    (N'butceye-uygun-oteller', N'/uploads/demo/campaigns/butceye-uygun-oteller/campaign-hero.png', N'Ekonomik fiyat bandindaki secili oteller burada.', N'Butceye Uygun Oteller', N'Butce odakli kampanyaya dahil otelleri tek ekranda listeleyin.'),
    (N'spa-ve-wellness-gunleri', N'/uploads/demo/campaigns/spa-ve-wellness-gunleri/campaign-hero.png', N'Spa ve wellness odakli secili tesis secimi.', N'Spa ve Wellness Gunleri', N'Spa ve wellness kampanyasindaki secili otelleri lokasyon bazinda bulun.'),
    (N'sehir-kacamagi', N'/uploads/demo/campaigns/sehir-kacamagi/campaign-hero.png', N'Merkezi lokasyonlu secili sehir otelleri vitrinde.', N'Sehir Kacamagi Otelleri', N'Sehir kacamagi kampanyasina dahil tesisleri mahalle seviyesine kadar filtreleyin.'),
    (N'mobil-uygulama-ozel', N'/uploads/demo/campaigns/mobil-uygulama-ozel/campaign-hero.png', N'Mobil odakli secili kampanya gorunumu hazir.', N'Mobil Ozel Kampanyalar', N'Mobil odakli kampanyaya katilan otelleri fiyat ve bolge bazinda inceleyin.'),
    (N'uzun-konaklama-avantaji', N'/uploads/demo/campaigns/uzun-konaklama-avantaji/campaign-hero.png', N'Birden fazla gece planlayanlar icin secili tesisler.', N'Uzun Konaklama Avantaji', N'Uzun konaklama kampanyasina dahil otelleri secili bolgelere gore listeleyin.'),
    (N'sadik-misafir-avantaji', N'/uploads/demo/campaigns/sadik-misafir-avantaji/campaign-hero.png', N'Sadik misafirler icin secili konaklama vitrinine goz atin.', N'Sadik Misafir Avantaji', N'Sadik misafir kampanyasina dahil secili tesisleri tek ekranda inceleyin.'),
    (N'aile-kacamagi', N'/uploads/demo/campaigns/aile-kacamagi/campaign-hero.png', N'Aile odakli secili tesisler tek listede.', N'Aile Kacamagi Otelleri', N'Aile kampanyasina katilan secili otelleri il, ilce ve fiyat bazinda filtreleyin.'),
    (N'seyahat-planlama-asistani-seckisi', N'/uploads/demo/campaigns/seyahat-planlama-asistani-seckisi/campaign-hero.png', N'Seyahat planlamayi kolaylastiran secili tesis listesi.', N'Seyahat Planlama Seckisi', N'Seyahat planlama vitrini icindeki secili otelleri lokasyon bazinda bulun.');

UPDATE k
SET
    hero_gorseli = asset.image_url,
    kart_gorseli = asset.image_url,
    banner_gorseli = asset.image_url,
    mobil_gorsel = asset.image_url,
    kisa_aciklama = asset.short_description,
    listeleme_basligi = asset.listing_title,
    listeleme_aciklamasi = asset.listing_description,
    guncellenme_tarihi = SYSUTCDATETIME()
FROM kampanyalar k
JOIN #campaign_assets asset ON asset.seo_slug = k.seo_slug
WHERE k.aktif_mi = 1
  AND k.gorunurluk_durumu LIKE N'Yay%';

IF @PublishedHotelId IS NOT NULL
BEGIN
    UPDATE ko
    SET
        katilim_durumu = N'Aktif',
        baslangic_tarihi = COALESCE(k.baslangic_tarihi, SYSUTCDATETIME()),
        bitis_tarihi = COALESCE(k.bitis_tarihi, DATEADD(DAY, 180, SYSUTCDATETIME())),
        kampanya_etiketi = COALESCE(NULLIF(k.kampanya_etiketi, N''), NULLIF(k.promo_badge, N''), k.kampanya_adi),
        one_cikan = CASE WHEN COALESCE(k.one_cikan_kampanya, 0) = 1 THEN 1 ELSE 0 END,
        siralama = CASE WHEN COALESCE(k.siralama, 0) <= 0 THEN 10 ELSE k.siralama END,
        ozel_kampanyali_fiyat = CASE
            WHEN k.seo_slug = N'flash-indirim' THEN 3590
            WHEN k.seo_slug = N'butceye-uygun-oteller' THEN 3490
            WHEN k.seo_slug = N'akilli-fiyat-seckisi' THEN 3790
            WHEN k.seo_slug = N'ultra-luks-secki' THEN 5190
            ELSE 3990
        END,
        guncellenme_tarihi = SYSUTCDATETIME()
    FROM kampanya_oteller ko
    JOIN kampanyalar k ON k.id = ko.kampanya_id
    WHERE ko.otel_id = @PublishedHotelId;

    INSERT INTO kampanya_oteller
    (
        kampanya_id,
        otel_id,
        partner_id,
        katilim_durumu,
        katilim_kaynagi,
        baslangic_tarihi,
        bitis_tarihi,
        ozel_kampanyali_fiyat,
        kampanya_etiketi,
        landing_url,
        partner_notu,
        one_cikan,
        siralama,
        olusturulma_tarihi,
        guncellenme_tarihi
    )
    SELECT
        k.id,
        @PublishedHotelId,
        NULL,
        N'Aktif',
        N'admin_seed',
        COALESCE(k.baslangic_tarihi, SYSUTCDATETIME()),
        COALESCE(k.bitis_tarihi, DATEADD(DAY, 180, SYSUTCDATETIME())),
        CASE
            WHEN k.seo_slug = N'flash-indirim' THEN 3590
            WHEN k.seo_slug = N'butceye-uygun-oteller' THEN 3490
            WHEN k.seo_slug = N'akilli-fiyat-seckisi' THEN 3790
            WHEN k.seo_slug = N'ultra-luks-secki' THEN 5190
            ELSE 3990
        END,
        COALESCE(NULLIF(k.kampanya_etiketi, N''), NULLIF(k.promo_badge, N''), k.kampanya_adi),
        CONCAT(N'/oteller?kampanya=', k.seo_slug),
        N'Otomatik kampanya vitrini baglantisi.',
        CASE WHEN COALESCE(k.one_cikan_kampanya, 0) = 1 THEN 1 ELSE 0 END,
        CASE WHEN COALESCE(k.siralama, 0) <= 0 THEN 10 ELSE k.siralama END,
        SYSUTCDATETIME(),
        SYSUTCDATETIME()
    FROM kampanyalar k
    WHERE k.aktif_mi = 1
      AND k.gorunurluk_durumu LIKE N'Yay%'
      AND NOT EXISTS (
            SELECT 1
            FROM kampanya_oteller existing
            WHERE existing.kampanya_id = k.id
              AND existing.otel_id = @PublishedHotelId
      );
END;

DROP TABLE #campaign_assets;
