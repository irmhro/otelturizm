/*
  2026-04-27 (SQL Server)
  destek_makaleleri: başlık/özet/içerik + SEO slug içinde kalan marka/acente adlarını temizler.

  - seo_slug güncellemesi collision riski taşır; bu yüzden prefix dönüşümünde benzersizliği korur:
    booking-*  -> kanal1-*
    airbnb-*   -> kanal2-*
    expedia-*  -> kanal3-*

  Not: Bu script yalnızca mevcut kalıntıları temizler; yeni seed'lerde de marka metinleri kaldırıldı.
*/

SET NOCOUNT ON;

-- 1) Metin alanları: marka/acente kelimelerini temizle
UPDATE dm
SET
    dm.baslik = REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(
        dm.baslik,
        N'Booking.com', N'Kanal'),
        N'Booking', N'Kanal'),
        N'Airbnb', N'Kanal'),
        N'Expedia', N'Kanal'),
        N'OTA', N'Kanal'),
    dm.ozet = REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(
        dm.ozet,
        N'Booking.com', N'Kanal'),
        N'Booking', N'Kanal'),
        N'Airbnb', N'Kanal'),
        N'Expedia', N'Kanal'),
        N'OTA', N'Kanal'),
    dm.icerik = REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(
        dm.icerik,
        N'Booking.com', N'Kanal'),
        N'Booking', N'Kanal'),
        N'Airbnb', N'Kanal'),
        N'Expedia', N'Kanal'),
        N'OTA', N'Kanal'),
    dm.guncellenme_tarihi = SYSUTCDATETIME()
FROM dbo.destek_makaleleri dm
WHERE dm.baslik LIKE N'%Booking%'
   OR dm.baslik LIKE N'%Airbnb%'
   OR dm.baslik LIKE N'%Expedia%'
   OR dm.baslik LIKE N'%OTA%'
   OR dm.ozet LIKE N'%Booking%'
   OR dm.ozet LIKE N'%Airbnb%'
   OR dm.ozet LIKE N'%Expedia%'
   OR dm.ozet LIKE N'%OTA%'
   OR dm.icerik LIKE N'%Booking%'
   OR dm.icerik LIKE N'%Airbnb%'
   OR dm.icerik LIKE N'%Expedia%'
   OR dm.icerik LIKE N'%OTA%';

-- 2) seo_slug: marka prefixlerini benzersiz biçimde temizle
UPDATE dm
SET
    dm.seo_slug = CONCAT('kanal1-', SUBSTRING(dm.seo_slug, LEN('booking-') + 1, 180)),
    dm.guncellenme_tarihi = SYSUTCDATETIME()
FROM dbo.destek_makaleleri dm
WHERE dm.seo_slug LIKE 'booking-%';

UPDATE dm
SET
    dm.seo_slug = CONCAT('kanal2-', SUBSTRING(dm.seo_slug, LEN('airbnb-') + 1, 180)),
    dm.guncellenme_tarihi = SYSUTCDATETIME()
FROM dbo.destek_makaleleri dm
WHERE dm.seo_slug LIKE 'airbnb-%';

UPDATE dm
SET
    dm.seo_slug = CONCAT('kanal3-', SUBSTRING(dm.seo_slug, LEN('expedia-') + 1, 180)),
    dm.guncellenme_tarihi = SYSUTCDATETIME()
FROM dbo.destek_makaleleri dm
WHERE dm.seo_slug LIKE 'expedia-%';

-- 3) Rapor: kalan kalıntılar
SELECT
    SUM(CASE WHEN seo_slug LIKE 'booking-%' OR seo_slug LIKE 'airbnb-%' OR seo_slug LIKE 'expedia-%' THEN 1 ELSE 0 END) AS kalan_slug,
    SUM(CASE WHEN baslik LIKE N'%Booking%' OR baslik LIKE N'%Airbnb%' OR baslik LIKE N'%Expedia%' THEN 1 ELSE 0 END) AS kalan_baslik,
    SUM(CASE WHEN ozet LIKE N'%Booking%' OR ozet LIKE N'%Airbnb%' OR ozet LIKE N'%Expedia%' THEN 1 ELSE 0 END) AS kalan_ozet,
    SUM(CASE WHEN icerik LIKE N'%Booking%' OR icerik LIKE N'%Airbnb%' OR icerik LIKE N'%Expedia%' THEN 1 ELSE 0 END) AS kalan_icerik
FROM dbo.destek_makaleleri;

