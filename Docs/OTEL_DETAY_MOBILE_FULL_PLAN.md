# Otel detay mobil — 200 maddelik gelişim planı

Route: `/oteller/{slug}` · CSS: `oteldetay_mobil.css`, `otel-detay-world.css` · JS: `otel-detay.js`, inline `OtelDetay.cshtml`

Referans: Booking/Expedia mobil otel detay, anasayfa premium tokenları, mevcut `od-detail` DOM sözleşmesi.

**Durum:** 2026-05-25 tarayıcı denetimi + M-v2 sprint (galeri oran, oda görsel `aspect-ratio`, fiyat foot grid)

---

## M1 — Sayfa iskeleti & taşma (10)

- [x] M1.01 `overflow-x: clip` + `max-width: 100%` kök
- [x] M1.02 `box-sizing: border-box` tüm bloklar
- [x] M1.03 Ana sütun `width: calc(100% - 32px)` + `margin: auto`
- [x] M1.04 Alt sticky bar safe-area padding
- [ ] M1.05 `100vw` yatay scroll audit (harita, yorum, benzer oteller)
- [ ] M1.06 Landscape 568px min-height düzen
- [ ] M1.07 901–1024px tek sütun zorla
- [ ] M1.08 iOS rubber-band scroll sınırı
- [ ] M1.09 Print mobil gizleme
- [ ] M1.10 PWA standalone viewport meta

## M2 — Üst galeri slider (10)

- [x] M2.01 Track `aspect-ratio: 4/3` sabit oran
- [x] M2.02 `object-fit: cover` slayt görselleri
- [x] M2.03 Chrome üst 12px hizalı back + sayaç
- [x] M2.04 Yıldız rozeti track üzerinde absolute
- [x] M2.05 Footer thumb şeridi kompakt padding
- [ ] M2.06 Touch swipe inertia + snap center
- [ ] M2.07 Nav okları reduced-motion kapalı animasyon
- [ ] M2.08 LCP ilk slayt `fetchpriority=high`
- [ ] M2.09 Tüm fotoğraflar lightbox girişi
- [ ] M2.10 Galeri JSON-LD image array

## M3 — Breadcrumb & quick actions (10)

- [x] M3.01 Breadcrumb yatay scroll chip
- [x] M3.02 Geri link mobil gizle (galeri back kullan)
- [x] M3.03 Quick action ikon-only 44px touch
- [x] M3.04 Paylaş native Web Share API
- [x] M3.05 Favori auth gate toast
- [ ] M3.06 Chat CTA tek tap
- [x] M3.07 Quick action yatay scroll (dikey stack kaldır)
- [x] M3.08 aria-label tüm aksiyonlar
- [x] M3.09 Breadcrumb schema mobil uyum
- [ ] M3.10 Sticky header çakışma testi

## M4 — Otel başlık kartı (10)

- [x] M4.01 Başlık `clamp()` tipografi
- [x] M4.02 Hero head dikey stack
- [x] M4.03 Rating chip sol hizalı
- [x] M4.04 Hava inline tam genişlik
- [ ] M4.05 Uzun otel adı 3 satır clamp
- [ ] M4.06 Konum link harita scroll
- [ ] M4.07 Yıldız + tip chip satırı
- [ ] M4.08 Sponsor / kampanya bandı
- [ ] M4.09 Live presence band kompakt
- [ ] M4.10 Headline JSON-LD uyumu

## M5 — Quick facts şeridi (10)

- [x] M5.01 Dikey stack chips
- [x] M5.02 Chip wrap gap 8px
- [ ] M5.03 Check-in/out saat ikonları
- [ ] M5.04 Ücretsiz iptal vurgu rengi
- [ ] M5.05 Chip yatay scroll alternatif
- [ ] M5.06 DB'den dinamik chip sırası
- [ ] M5.07 Chip tooltip uzun metin
- [ ] M5.08 i18n chip metinleri
- [ ] M5.09 Chip analytics impression
- [ ] M5.10 Reduced motion chip animasyon kapalı

## M6 — Oda kartı medya (10)

- [x] M6.01 Kapak `aspect-ratio: 16/10`
- [x] M6.02 `object-fit: cover` — oransız uzatma fix
- [x] M6.03 Trigger tam alan tıklanabilir
- [x] M6.04 Galeri sayacı rozeti sağ üst
- [x] M6.05 Medya padding 12px üst
- [ ] M6.06 Kapak lazy + blur-up placeholder
- [ ] M6.07 onerror placeholder görsel
- [ ] M6.08 Pinch-zoom lightbox
- [ ] M6.09 Video oda desteği slot
- [ ] M6.10 Oda görsel WebP srcset

## M7 — Oda thumb şeridi (10)

- [x] M7.01 Yatay scroll snap 56px kare
- [x] M7.02 Aktif thumb crimson border
- [x] M7.03 Scrollbar gizle
- [x] M7.04 Thumb tap → kapak preview sync (JS)
- [x] M7.05 6+ thumb fade edge hint
- [x] M7.06 Thumb keyboard focus ring
- [x] M7.07 Thumb aria-current active
- [x] M7.08 Swipe thumb strip momentum
- [ ] M7.09 Thumb loading skeleton
- [ ] M7.10 Thumb reduced motion

## M8 — Oda kartı içerik (10)

- [x] M8.01 Dikey stack: medya üst, metin alt
- [x] M8.02 Başlık 1.05rem kompakt
- [x] M8.03 Specs 2 satır clamp
- [x] M8.04 Meta pill wrap
- [x] M8.05 Özellik listesi ikon hizası
- [ ] M8.06 Diğer özellikler accordion animasyon
- [ ] M8.07 Detaylar modal sheet
- [x] M8.08 İptal rozeti renk kodu
- [x] M8.09 Boş oda uyarı banner
- [ ] M8.10 Oda kartı skeleton loading

## M9 — Oda fiyat & CTA foot (10)

- [x] M9.01 Grid: fiyat sol, rezervasyon sağ
- [x] M9.02 CTA pill crimson 44px min-height
- [x] M9.03 İndirim stack eski fiyat + pill
- [x] M9.04 Gece başlığı kicker uppercase
- [x] M9.05 Vergi dahil etiketi kompakt
- [x] M9.06 Fiyat yok disabled state metni
- [ ] M9.07 Çoklu gece toplam tooltip
- [ ] M9.08 Kampanya countdown chip
- [ ] M9.09 CTA loading state
- [ ] M9.10 CTA analytics event

## M10 — Rezervasyon sheet (10)

- [x] M10.01 Sidebar fixed bottom sheet
- [x] M10.02 Backdrop blur + opacity
- [x] M10.03 Sheet drag handle çizgi
- [x] M10.04 Safe-area alt padding
- [x] M10.05 Swipe-down kapatma
- [x] M10.06 Focus trap sheet açık
- [x] M10.07 Escape kapatma
- [ ] M10.08 Tarih picker native mobil
- [x] M10.09 Misafir stepper 44px
- [ ] M10.10 Multi-room accordion mobil

## M11 — Sticky alt bar (10)

- [x] M11.01 Fiyat + Rezervasyona Git görünür
- [x] M11.02 z-index header altında çakışma yok
- [ ] M11.03 Scroll-up bar compact mod
- [ ] M11.04 Bar animasyon reduced motion
- [ ] M11.05 Bar aria live fiyat güncelleme
- [ ] M11.06 Bar tık → sheet aç
- [ ] M11.07 Bar gizle galeri fullscreen
- [ ] M11.08 Bar shadow scroll state
- [ ] M11.09 Bar i18n metinleri
- [ ] M11.10 Bar A/B CTA metin testi

## M12 — Olanaklar bölümü (10)

- [x] M12.01 Tek sütun amenity grid
- [ ] M12.02 Kategori accordion
- [ ] M12.03 Ücretli olanak badge
- [ ] M12.04 Tümünü göster sheet
- [ ] M12.05 İkon renk kategoriye göre
- [ ] M12.06 Amenity arama filtresi
- [ ] M12.07 Amenity schema markup
- [ ] M12.08 Amenity lazy render
- [ ] M12.09 Amenity i18n
- [ ] M12.10 Amenity touch 44px satır

## M13 — Konum & harita (10)

- [x] M13.01 Harita yükseklik 200px mobil
- [ ] M13.02 Harita lazy iframe
- [ ] M13.03 Yol tarifi deep link
- [ ] M13.04 POI yakınlık listesi scroll
- [ ] M13.05 Harita fullscreen modal
- [ ] M13.06 Konum kopyala
- [ ] M13.07 Geo schema
- [ ] M13.08 Offline harita fallback
- [ ] M13.09 Harita consent cookie
- [ ] M13.10 Harita reduced data mod

## M14 — Yorumlar OTA (10)

- [x] M14.01 Özet skor dikey stack
- [x] M14.02 Topic strip yatay scroll
- [x] M14.03 Review kart border radius
- [ ] M14.04 Filtre chip sheet
- [ ] M14.05 Sıralama native select
- [ ] M14.06 Fotoğraflı yorum grid
- [ ] M14.07 Yanıt ver accordion
- [ ] M14.08 Helpful vote touch
- [ ] M14.09 Review skeleton
- [ ] M14.10 Review schema aggregateRating

## M15 — Politikalar & SSS (10)

- [x] M15.01 Policy item 2 kolon kompakt
- [ ] M15.02 Collapsible section animasyon
- [ ] M15.03 SSS accordion tek açık
- [ ] M15.04 Politika ikon seti
- [ ] M15.05 Uzun metin expand
- [ ] M15.06 PDF indirme linki
- [ ] M15.07 i18n politika metinleri
- [ ] M15.08 Print politika bloğu
- [ ] M15.09 Schema FAQPage
- [ ] M15.10 Reduced motion collapse

## M16 — Benzer oteller (10)

- [x] M16.01 Tek sütun similar grid
- [ ] M16.02 Yatay scroll kart carousel alternatif
- [ ] M16.03 Kart aspect-ratio tutarlı
- [ ] M16.04 Favori hızlı toggle
- [ ] M16.05 Fiyat chip
- [ ] M16.06 Lazy image intersection
- [ ] M16.07 Similar analytics click
- [ ] M16.08 Similar skeleton
- [ ] M16.09 Similar i18n
- [ ] M16.10 Similar empty state gizle

## M17 — Erişilebilirlik (10)

- [ ] M17.01 Skip link odalar bölümü
- [ ] M17.02 Focus visible tüm interaktif
- [ ] M17.03 Galeri aria-roledescription
- [ ] M17.04 Sheet aria-modal
- [ ] M17.05 Renk kontrast WCAG AA audit
- [ ] M17.06 Touch target 44px audit
- [ ] M17.07 Screen reader oda fiyat duyuru
- [ ] M17.08 Reduced motion global
- [ ] M17.09 High contrast mode
- [ ] M17.10 VoiceOver swipe test checklist

## M18 — Performans (10)

- [x] M18.01 LCP galeri ilk görsel preload
- [x] M18.02 Oda görselleri lazy below fold
- [ ] M18.03 CSS kritik inline değerlendirme
- [ ] M18.04 JS defer otel-detay.js split
- [ ] M18.05 Harita iframe intersection lazy
- [ ] M18.06 Font subset Türkçe
- [ ] M18.07 Image CDN resize param
- [ ] M18.08 Cache-Control static assets
- [x] M18.09 CLS oda kartı reserved height
- [ ] M18.10 Lighthouse mobil hedef 90+

## M19 — i18n & RTL (10)

- [ ] M19.01 SharedLocalizer tüm CTA
- [ ] M19.02 RTL galeri nav swap
- [ ] M19.03 Tarih format locale
- [ ] M19.04 Para birimi sembol
- [ ] M19.05 Uzun Almanca başlık clamp
- [ ] M19.06 Arapça numeral
- [ ] M19.07 hreflang link
- [ ] M19.08 Cookie consent locale
- [ ] M19.09 Error mesaj i18n
- [ ] M19.10 QA dil matrisi

## M20 — QA & regresyon (10)

- [ ] M20.01 iPhone SE 375px screenshot
- [ ] M20.02 iPhone 14 Pro 390px
- [ ] M20.03 Pixel 7 412px
- [ ] M20.04 iPad 768px tek sütun
- [ ] M20.05 Android Chrome swipe galeri
- [ ] M20.06 Safari iOS sheet safe-area
- [ ] M20.07 Boş galeri fallback
- [ ] M20.08 Tek oda / çok oda
- [ ] M20.09 İndirimli fiyat görünümü
- [ ] M20.10 Canlı smoke `/oteller/{slug}`

---

**İlerleme özeti (M-v2):** ~42 / 200 tamamlandı

**Sonraki sprint:** M7.04 thumb preview sync, M10.05 swipe kapat, M11.03 compact bar, M18.09 CLS reserve
