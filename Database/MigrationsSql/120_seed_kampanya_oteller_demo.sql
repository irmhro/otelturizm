MERGE kampanya_oteller AS target
USING (
SELECT k.id, o.id, o.partner_id, 'Aktif', 'Admin', CASE WHEN k.baslangic_tarihi > '2026-04-14 00:00:00' THEN k.baslangic_tarihi ELSE '2026-04-14 00:00:00' END, k.bitis_tarihi, CASE WHEN k.tur IN ('Yüzde İndirim', 'Erken Rezervasyon', 'Uzun Konaklama', 'Son Dakika', 'Mobil Özel') THEN COALESCE(k.indirim_orani, 10.00) ELSE NULL END, k.kampanya_etiketi, CASE WHEN k.one_cikan_kampanya = 1 THEN 1 ELSE 0 END, k.siralama FROM kampanyalar k JOIN oteller o ON o.id IN (20,29,14) WHERE k.kampanya_kodu = 'KMP-2026-YILBASI'
UNION ALL
SELECT k.id, o.id, o.partner_id, 'Aktif', 'Admin', CASE WHEN k.baslangic_tarihi > '2026-04-14 00:00:00' THEN k.baslangic_tarihi ELSE '2026-04-14 00:00:00' END, k.bitis_tarihi, CASE WHEN k.tur IN ('Yüzde İndirim', 'Erken Rezervasyon', 'Uzun Konaklama', 'Son Dakika', 'Mobil Özel') THEN COALESCE(k.indirim_orani, 10.00) ELSE NULL END, k.kampanya_etiketi, CASE WHEN k.one_cikan_kampanya = 1 THEN 1 ELSE 0 END, k.siralama FROM kampanyalar k JOIN oteller o ON o.id IN (2,3,20,29) WHERE k.kampanya_kodu = 'KMP-2026-ERKENREZ'
UNION ALL
SELECT k.id, o.id, o.partner_id, 'Aktif', 'Admin', CASE WHEN k.baslangic_tarihi > '2026-04-14 00:00:00' THEN k.baslangic_tarihi ELSE '2026-04-14 00:00:00' END, k.bitis_tarihi, CASE WHEN k.tur IN ('Yüzde İndirim', 'Erken Rezervasyon', 'Uzun Konaklama', 'Son Dakika', 'Mobil Özel') THEN COALESCE(k.indirim_orani, 10.00) ELSE NULL END, k.kampanya_etiketi, CASE WHEN k.one_cikan_kampanya = 1 THEN 1 ELSE 0 END, k.siralama FROM kampanyalar k JOIN oteller o ON o.id IN (14,17,20,29) WHERE k.kampanya_kodu = 'KMP-2026-HAFTASONU'
UNION ALL
SELECT k.id, o.id, o.partner_id, 'Aktif', 'Admin', CASE WHEN k.baslangic_tarihi > '2026-04-14 00:00:00' THEN k.baslangic_tarihi ELSE '2026-04-14 00:00:00' END, k.bitis_tarihi, CASE WHEN k.tur IN ('Yüzde İndirim', 'Erken Rezervasyon', 'Uzun Konaklama', 'Son Dakika', 'Mobil Özel') THEN COALESCE(k.indirim_orani, 10.00) ELSE NULL END, k.kampanya_etiketi, CASE WHEN k.one_cikan_kampanya = 1 THEN 1 ELSE 0 END, k.siralama FROM kampanyalar k JOIN oteller o ON o.id IN (3,4,20) WHERE k.kampanya_kodu = 'KMP-2026-FLASH'
UNION ALL
SELECT k.id, o.id, o.partner_id, 'Aktif', 'Admin', CASE WHEN k.baslangic_tarihi > '2026-04-14 00:00:00' THEN k.baslangic_tarihi ELSE '2026-04-14 00:00:00' END, k.bitis_tarihi, CASE WHEN k.tur IN ('Yüzde İndirim', 'Erken Rezervasyon', 'Uzun Konaklama', 'Son Dakika', 'Mobil Özel') THEN COALESCE(k.indirim_orani, 10.00) ELSE NULL END, k.kampanya_etiketi, CASE WHEN k.one_cikan_kampanya = 1 THEN 1 ELSE 0 END, k.siralama FROM kampanyalar k JOIN oteller o ON o.id IN (2,17,29) WHERE k.kampanya_kodu = 'KMP-2026-AYSONU'
UNION ALL
SELECT k.id, o.id, o.partner_id, 'Aktif', 'Admin', CASE WHEN k.baslangic_tarihi > '2026-04-14 00:00:00' THEN k.baslangic_tarihi ELSE '2026-04-14 00:00:00' END, k.bitis_tarihi, CASE WHEN k.tur IN ('Yüzde İndirim', 'Erken Rezervasyon', 'Uzun Konaklama', 'Son Dakika', 'Mobil Özel') THEN COALESCE(k.indirim_orani, 10.00) ELSE NULL END, k.kampanya_etiketi, CASE WHEN k.one_cikan_kampanya = 1 THEN 1 ELSE 0 END, k.siralama FROM kampanyalar k JOIN oteller o ON o.id IN (20,29) WHERE k.kampanya_kodu = 'KMP-2026-AKILLI'
UNION ALL
SELECT k.id, o.id, o.partner_id, 'Aktif', 'Admin', CASE WHEN k.baslangic_tarihi > '2026-04-14 00:00:00' THEN k.baslangic_tarihi ELSE '2026-04-14 00:00:00' END, k.bitis_tarihi, CASE WHEN k.tur IN ('Yüzde İndirim', 'Erken Rezervasyon', 'Uzun Konaklama', 'Son Dakika', 'Mobil Özel') THEN COALESCE(k.indirim_orani, 10.00) ELSE NULL END, k.kampanya_etiketi, CASE WHEN k.one_cikan_kampanya = 1 THEN 1 ELSE 0 END, k.siralama FROM kampanyalar k JOIN oteller o ON o.id IN (2,3,4,14,17,20,29) WHERE k.kampanya_kodu = 'KMP-2026-SEHIR'
) AS source (kampanya_id, otel_id, partner_id, katilim_durumu, katilim_kaynagi, baslangic_tarihi, bitis_tarihi, ozel_indirim_orani, kampanya_etiketi, one_cikan, siralama)
ON target.kampanya_id = source.kampanya_id AND target.otel_id = source.otel_id
WHEN MATCHED THEN
    UPDATE SET
        partner_id = source.partner_id,
        katilim_durumu = source.katilim_durumu,
        katilim_kaynagi = source.katilim_kaynagi,
        baslangic_tarihi = source.baslangic_tarihi,
        bitis_tarihi = source.bitis_tarihi,
        ozel_indirim_orani = source.ozel_indirim_orani,
        kampanya_etiketi = source.kampanya_etiketi,
        one_cikan = source.one_cikan,
        siralama = source.siralama
WHEN NOT MATCHED THEN
    INSERT (kampanya_id, otel_id, partner_id, katilim_durumu, katilim_kaynagi, baslangic_tarihi, bitis_tarihi, ozel_indirim_orani, kampanya_etiketi, one_cikan, siralama)
    VALUES (source.kampanya_id, source.otel_id, source.partner_id, source.katilim_durumu, source.katilim_kaynagi, source.baslangic_tarihi, source.bitis_tarihi, source.ozel_indirim_orani, source.kampanya_etiketi, source.one_cikan, source.siralama);
