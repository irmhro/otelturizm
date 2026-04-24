UPDATE oda_fiyat_musaitlik
SET iptal_politikasi_override = N'İptal koşulu girişten 24 saat öncesine kadar ücretsizdir.'
WHERE iptal_politikasi_override IS NOT NULL
  AND (
        iptal_politikasi_override LIKE N'%24 saat%'
        OR iptal_politikasi_override LIKE N'%Iptal kosulu%'
        OR iptal_politikasi_override LIKE N'%giriÅŸten%'
        OR iptal_politikasi_override LIKE N'%giristen%'
        OR iptal_politikasi_override LIKE N'%İptal koşulu%'
      );

UPDATE oda_fiyat_musaitlik
SET fiyat_notu = N'Demo takvim fiyat kaydı'
WHERE fiyat_notu IS NOT NULL
  AND (
        fiyat_notu LIKE N'Demo takvim fiyat%'
        OR fiyat_notu LIKE N'%takvim fiyat kaydi%'
        OR fiyat_notu LIKE N'%takvim fiyat kaydı%'
      );

UPDATE oda_fiyat_musaitlik
SET kampanya_etiketi = N'Akıllı Fiyat'
WHERE kampanya_etiketi IS NOT NULL
  AND (
        kampanya_etiketi LIKE N'Ak%'
        OR kampanya_etiketi LIKE N'%AkÄ±llÄ± Fiyat%'
        OR kampanya_etiketi LIKE N'%Akıllı Fiyat%'
      );
