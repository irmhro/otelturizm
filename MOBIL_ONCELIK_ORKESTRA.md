# Mobil öncelik orkestrası

**Hedef kitle:** ~%99 mobil kullanıcı · masaüstü ikincil ama kırıksız.  
**Koordinatör:** `PLATFORM_KOORDINATOR_OPERASYON_PLANI.md`  
**Sayfa envanteri:** `FRONTEND_ORKESTRATOR_PLAN.md` (151 sayfa)

---

## Standartlar (tüm paneller + kamu)

| Kural | Değer |
|-------|--------|
| Dokunma alanı | min 44×44px |
| Safe area | `env(safe-area-inset-*)` — `shell.mobile.css`, alt nav |
| Tablo → kart | `<768px` yatay scroll yerine kart satırı |
| Tipografi | Mobil gövde ≥16px (zoom engeli) |
| Form | Tek sütun; label üstte |
| Modal / sheet | Tam genişlik alt sheet veya full-screen mobil |
| `PageCssMobile` | Her sayfada layout’ta tanımlı; eksik dosya = 🔴 |

---

## Orkestra ataması

| Orkestra | Sayfa | Öncelik |
|----------|-------|---------|
| `fe-otel-public` | Anasayfa, Oteller, Harita, OtelDetay, Rezervasyon | P0 — trafik |
| `fe-user` | Profil, rezervasyonlar, faturalar | P0 |
| `fe-partner` | Dashboard, tesis, fiyat, komisyon, rezervasyon | P0 |
| `fe-admin` | Dashboard, rezervasyon birleşik, güvenlik | P1 |
| `fe-firma` | Rezervasyon oluştur, fiyat karşılaştır | P1 |
| `fe-satis` | Dashboard, teklifler | P2 |

---

## Doğrulama (FE-CTO)

Her sayfa için:

1. Chrome DevTools — iPhone 14 / 390×844  
2. SS: `docs/frontend-screenshots/{panel}/{sayfa}-mobil.png`  
3. Kontrol: taşma yok, CTA görünür, tablo kart, formlar tek parmak  
4. `FRONTEND_ORKESTRATOR_PLAN.md` satırı → **APPROVED** veya red notu  

**Durum:** CSS envanter çoğu ✅ · FE-CTO onay **6/151** — SS + auth döngüsü eksik.

---

## Teknik borç (mobil)

- Panel offcanvas menü: `T016` (`panel-admin-shell`)  
- Uzun admin tabloları: sticky filtre + kart modu  
- Harita: filter bottom sheet mobil  
- OtelDetay: galeri swipe, sticky “Rezervasyon” bar  

*Detay işler `CTO_AJAN_ATAMA_KUYRUGU.md` wave D + G.*
