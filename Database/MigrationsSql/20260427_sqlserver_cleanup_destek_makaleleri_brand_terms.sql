/*
  2026-04-27 (SQL Server)
  Destek içeriklerinde marka isimleri (Booking/Airbnb/Expedia) yer almayacak şekilde temizlik.
  Not: SEO slug'lar kırılmasın diye seo_slug alanlarına dokunulmaz.
*/

SET NOCOUNT ON;

-- Kategori kısa açıklamalarını normalize et
UPDATE dk
SET
    dk.kisa_aciklama = REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(
        dk.kisa_aciklama,
        'Booking.com', 'Kanal'),
        'Booking', 'Kanal'),
        'Airbnb', 'Kanal'),
        'Expedia', 'Kanal'),
        'OTA', 'Kanal'),
    dk.guncellenme_tarihi = SYSUTCDATETIME()
FROM dbo.destek_kategorileri dk
WHERE dk.kisa_aciklama LIKE '%Booking%'
   OR dk.kisa_aciklama LIKE '%Airbnb%'
   OR dk.kisa_aciklama LIKE '%Expedia%'
   OR dk.kisa_aciklama LIKE '%OTA%';

-- Makale başlık/özet/içerik içinde geçen marka kelimelerini temizle
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

-- Rapor
SELECT
    SUM(CASE WHEN baslik LIKE N'%Booking%' OR baslik LIKE N'%Airbnb%' OR baslik LIKE N'%Expedia%' THEN 1 ELSE 0 END) AS kalan_baslik,
    SUM(CASE WHEN ozet LIKE N'%Booking%' OR ozet LIKE N'%Airbnb%' OR ozet LIKE N'%Expedia%' THEN 1 ELSE 0 END) AS kalan_ozet,
    SUM(CASE WHEN icerik LIKE N'%Booking%' OR icerik LIKE N'%Airbnb%' OR icerik LIKE N'%Expedia%' THEN 1 ELSE 0 END) AS kalan_icerik
FROM dbo.destek_makaleleri;

