INSERT INTO kampanya_oteller
(kampanya_id, otel_id, partner_id, katilim_durumu, katilim_kaynagi, baslangic_tarihi, bitis_tarihi, ozel_indirim_orani, kampanya_etiketi, one_cikan, siralama)
SELECT k.id, o.id, o.partner_id, 'Aktif', 'Admin', GREATEST(k.baslangic_tarihi, '2026-04-14 00:00:00'), k.bitis_tarihi, CASE WHEN k.tur IN ('Yüzde İndirim', 'Erken Rezervasyon', 'Uzun Konaklama', 'Son Dakika', 'Mobil Özel') THEN COALESCE(k.indirim_orani, 10.00) ELSE NULL END, k.kampanya_etiketi, CASE WHEN k.one_cikan_kampanya = 1 THEN 1 ELSE 0 END, k.siralama FROM kampanyalar k JOIN oteller o ON o.id IN (20,29,14) WHERE k.kampanya_kodu = 'KMP-2026-YILBASI'
UNION ALL
SELECT k.id, o.id, o.partner_id, 'Aktif', 'Admin', GREATEST(k.baslangic_tarihi, '2026-04-14 00:00:00'), k.bitis_tarihi, CASE WHEN k.tur IN ('Yüzde İndirim', 'Erken Rezervasyon', 'Uzun Konaklama', 'Son Dakika', 'Mobil Özel') THEN COALESCE(k.indirim_orani, 10.00) ELSE NULL END, k.kampanya_etiketi, CASE WHEN k.one_cikan_kampanya = 1 THEN 1 ELSE 0 END, k.siralama FROM kampanyalar k JOIN oteller o ON o.id IN (2,3,20,29) WHERE k.kampanya_kodu = 'KMP-2026-ERKENREZ'
UNION ALL
SELECT k.id, o.id, o.partner_id, 'Aktif', 'Admin', GREATEST(k.baslangic_tarihi, '2026-04-14 00:00:00'), k.bitis_tarihi, CASE WHEN k.tur IN ('Yüzde İndirim', 'Erken Rezervasyon', 'Uzun Konaklama', 'Son Dakika', 'Mobil Özel') THEN COALESCE(k.indirim_orani, 10.00) ELSE NULL END, k.kampanya_etiketi, CASE WHEN k.one_cikan_kampanya = 1 THEN 1 ELSE 0 END, k.siralama FROM kampanyalar k JOIN oteller o ON o.id IN (14,17,20,29) WHERE k.kampanya_kodu = 'KMP-2026-HAFTASONU'
UNION ALL
SELECT k.id, o.id, o.partner_id, 'Aktif', 'Admin', GREATEST(k.baslangic_tarihi, '2026-04-14 00:00:00'), k.bitis_tarihi, CASE WHEN k.tur IN ('Yüzde İndirim', 'Erken Rezervasyon', 'Uzun Konaklama', 'Son Dakika', 'Mobil Özel') THEN COALESCE(k.indirim_orani, 10.00) ELSE NULL END, k.kampanya_etiketi, CASE WHEN k.one_cikan_kampanya = 1 THEN 1 ELSE 0 END, k.siralama FROM kampanyalar k JOIN oteller o ON o.id IN (3,4,20) WHERE k.kampanya_kodu = 'KMP-2026-FLASH'
UNION ALL
SELECT k.id, o.id, o.partner_id, 'Aktif', 'Admin', GREATEST(k.baslangic_tarihi, '2026-04-14 00:00:00'), k.bitis_tarihi, CASE WHEN k.tur IN ('Yüzde İndirim', 'Erken Rezervasyon', 'Uzun Konaklama', 'Son Dakika', 'Mobil Özel') THEN COALESCE(k.indirim_orani, 10.00) ELSE NULL END, k.kampanya_etiketi, CASE WHEN k.one_cikan_kampanya = 1 THEN 1 ELSE 0 END, k.siralama FROM kampanyalar k JOIN oteller o ON o.id IN (2,17,29) WHERE k.kampanya_kodu = 'KMP-2026-AYSONU'
UNION ALL
SELECT k.id, o.id, o.partner_id, 'Aktif', 'Admin', GREATEST(k.baslangic_tarihi, '2026-04-14 00:00:00'), k.bitis_tarihi, CASE WHEN k.tur IN ('Yüzde İndirim', 'Erken Rezervasyon', 'Uzun Konaklama', 'Son Dakika', 'Mobil Özel') THEN COALESCE(k.indirim_orani, 10.00) ELSE NULL END, k.kampanya_etiketi, CASE WHEN k.one_cikan_kampanya = 1 THEN 1 ELSE 0 END, k.siralama FROM kampanyalar k JOIN oteller o ON o.id IN (20,29) WHERE k.kampanya_kodu = 'KMP-2026-AKILLI'
UNION ALL
SELECT k.id, o.id, o.partner_id, 'Aktif', 'Admin', GREATEST(k.baslangic_tarihi, '2026-04-14 00:00:00'), k.bitis_tarihi, CASE WHEN k.tur IN ('Yüzde İndirim', 'Erken Rezervasyon', 'Uzun Konaklama', 'Son Dakika', 'Mobil Özel') THEN COALESCE(k.indirim_orani, 10.00) ELSE NULL END, k.kampanya_etiketi, CASE WHEN k.one_cikan_kampanya = 1 THEN 1 ELSE 0 END, k.siralama FROM kampanyalar k JOIN oteller o ON o.id IN (2,3,4,14,17,20,29) WHERE k.kampanya_kodu = 'KMP-2026-SEHIR'
ON DUPLICATE KEY UPDATE
    partner_id = VALUES(partner_id),
    katilim_durumu = VALUES(katilim_durumu),
    katilim_kaynagi = VALUES(katilim_kaynagi),
    baslangic_tarihi = VALUES(baslangic_tarihi),
    bitis_tarihi = VALUES(bitis_tarihi),
    ozel_indirim_orani = VALUES(ozel_indirim_orani),
    kampanya_etiketi = VALUES(kampanya_etiketi),
    one_cikan = VALUES(one_cikan),
    siralama = VALUES(siralama);
