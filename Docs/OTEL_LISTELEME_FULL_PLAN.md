# Otel listeleme (`/oteller`) — tam gelişim planı

Mevcut `otelliste-*` tasarımı korunarak DB-first liste, filtre, harita ve SEO orchestrator yapısı.

## Orchestrator özeti

| Grup | Kapsam | Öncelik |
|------|--------|---------|
| **L0** | Altyapı: route, CSS sözleşmesi, HotelService, yayın/onay kapıları | P0 |
| **L1** | Liste kartı: fiyat, indirim, galeri, rozet, favori | P0 |
| **L2** | Filtreler: konum, yıldız, fiyat, özellik, kampanya | P0 |
| **L3** | Sıralama, sayfalama, sponsor pin, boş durum | P0 |
| **L4** | Harita listesi, konum oturumu, mesafe | P1 |
| **L5** | Kampanya/etiket SEO, uluslararası route | P1 |
| **L6** | Sadakat dokunuşları, misafir favori senkronu | P1 |
| **L7** | Performans, cache, output cache invalidation | P2 |
| **L8** | Analytics, A/B, erişilebilirlik denetimi | P2 |

## Route ↔ dosya

- **Liste:** `/oteller`, `/oteller/istanbul`, `/en/hotels`, …
- **Controller:** `OtellerController.OtelListeleme`
- **Service:** `HotelService.GetHotelListingPageAsync`
- **View:** `Views/Oteller/OtelListeleme.cshtml`, `_OtelListelemeContent.cshtml`, `_OtelListelemeFilters.cshtml`
- **CSS:** `otelliste_masaustu.css`, `otelliste_mobil.css`
- **JS:** `otelliste-filters.js`, `favorites.js`

---

## L0 — Altyapı (P0)

- [x] L0.01 `YAYIN_DURUMU` + `ONAY_DURUMU` SQL kapıları (`PublishStatusSql`)
- [x] L0.02 `GetHotelListingPageAsync` tek giriş noktası
- [x] L0.03 Kart VM: `HotelListingCardViewModel` alan sözleşmesi
- [x] L0.04 Sayfa VM: şehir/ilçe/mahalle facet listeleri
- [x] L0.05 `otellistePage` + grid DOM id sözleşmesi
- [x] L0.06 Mobil filtre drawer + overlay
- [x] L0.07 Desktop/mobil çift filtre scope (`data-filter-scope`)
- [x] L0.08 Output cache `public-short` policy
- [x] L0.09 Rate limit public route
- [x] L0.10 Demo otel seed: `DEMO-MAIDAN-2026` tam paket
- [ ] L0.11 Check-in/out query → fiyat tarihi (P1)
- [ ] L0.12 Subscription sponsor SQL apply (mevcut hook doğrulama)

## L1 — Liste kartı (P0)

- [x] L1.01 Kapak görseli `OTEL_GORSELLERI` / `KAPAK_FOTOGRAFI`
- [x] L1.02 Galeri `STRING_AGG` top-3 (`gorsel_listesi`)
- [x] L1.03 Bugünkü en düşük gece fiyatı (`ODA_FIYAT_MUSAITLIK`)
- [x] L1.04 İndirimli fiyat + yüzde rozeti
- [x] L1.05 `FIYAT_INDIRIMLERI` ad/açıklama/görsel join
- [x] L1.06 Kampanya badge (`kampanya_badgetext`)
- [x] L1.07 Öne çıkan otel sıralama önceliği
- [x] L1.08 Yıldız sayısı satırı
- [x] L1.09 Özellik ikonları (top 3)
- [x] L1.10 Ücretsiz iptal rozeti (`OTEL_KOSULLARI.UCRETSIZ_IPTAL_SURESI`)
- [x] L1.11 Favori butonu + `ApplyFavoriteStatesAsync`
- [x] L1.12 Detay slug URL (`/oteller/{slug}`)
- [x] L1.13 Placeholder görsel fallback
- [x] L1.14 Vergiler dahil fiyat notu (KDV+konaklama)
- [x] L1.15 Kart hover galeri swipe (masaüstü hover, data-gallery)
- [x] L1.16 Lead oda adı alt satır (ListingLeadRoomName UI)

## L2 — Filtreler (P0)

- [x] L2.01 Anahtar kelime client filter (`data-keywords`)
- [x] L2.02 İl / ilçe / mahalle select facet
- [x] L2.03 Konum haritası JSON (`otellisteLocationMap`)
- [x] L2.04 Yıldız chip multi-select
- [x] L2.05 Min/max fiyat aralığı
- [x] L2.06 Özellik checkbox (dinamik top-10)
- [x] L2.07 Kampanya slug checkbox
- [x] L2.08 Aktif filtre sayacı
- [x] L2.09 Filtre sıfırla
- [x] L2.10 URL `?q=` / `?city=` hydrate (client)
- [ ] L2.11 Server-side yıldız filtresi query param (P1)
- [ ] L2.12 Server-side fiyat min/max (P1)
- [ ] L2.13 Tarih aralığına göre fiyat yeniden hesap (P1)
- [ ] L2.14 Misafir sayısı filtresi (P2)

## L3 — Sıralama & sayfalama (P0)

- [x] L3.01 Önerilen sıra (featured → rating)
- [x] L3.02 Fiyat artan/azalan client sort
- [x] L3.03 Puan azalan client sort
- [x] L3.04 Server sayfalama (`page`, `TotalPages`)
- [x] L3.05 Sponsor pinning (`ApplySponsorPinning`)
- [x] L3.06 Kampanya etiket filtresi (`etiket`, `filter`)
- [x] L3.07 Kampanya slug filtresi (`kampanya`)
- [x] L3.08 Boş liste sunucu mesajı
- [x] L3.09 Client-side boş filtre mesajı (`otellisteClientEmpty`)
- [x] L3.10 Sonuç sayacı (`otellisteResultCount`)
- [ ] L3.11 Infinite scroll alternatif (P2)
- [x] L3.12 URL'de sort persist (P1)

## L4 — Harita (P1)

- [x] L4.01 `/oteller/harita` route
- [x] L4.02 Leaflet/cluster marker
- [ ] L4.03 Liste ↔ harita senkron scroll
- [ ] L4.04 Konum oturumu cookie
- [ ] L4.05 Mesafe sıralama
- [ ] L4.06 Mobil harita tam ekran

## L5 — SEO & i18n (P1)

- [x] L5.01 `InternationalSeoService.BuildListingMeta`
- [x] L5.02 Çoklu dil route prefix (`/en/hotels`, …)
- [x] L5.03 Canonical + meta description ViewData
- [x] L5.04 Schema.org ItemList JSON-LD
- [ ] L5.05 Şehir landing slug sayfaları genişletme
- [x] L5.06 hreflang alternates (premium layout)

## L6 — Sadakat & büyüme (P1)

- [x] L6.01 `ApplyListingLoyaltyTouchpoints`
- [x] L6.02 Tahmini sadakat puanı VM alanı
- [x] L6.03 Giriş yapmış kullanıcıya puan chip UI
- [ ] L6.04 Growth signals impression log

## L7 — Performans (P2)

- [x] L7.01 SQL tek sorgu kart yükleme
- [x] L7.02 Lazy image loading (ilk kart eager)
- [ ] L7.03 Facet cache 60s memory
- [ ] L7.04 CDN görsel prefix
- [ ] L7.05 DB index `IX_oteller_yayin_onay`

## L8 — Kalite & erişilebilirlik (P2)

- [x] L8.01 Kart `aria-label` detay link
- [x] L8.02 Favori `aria-pressed`
- [x] L8.03 Drawer `aria-hidden` toggle
- [x] L8.04 Klavye ile yıldız filtresi
- [ ] L8.05 E2E: filtre + detay navigasyon
- [ ] L8.06 Lighthouse LCP < 2.5s hedef

---

## Ana sayfa otel vitrini (cross-cutting)

- [x] H1.01 `HomeController` → `GetHomepageAsync`
- [x] H1.02 `PopularHotels` / `WeekendHotels` DB kaynaklı
- [x] H1.03 `_HomeHotelCard` 4'lü galeri grid
- [x] H1.04 `GalleryImageUrls` batch yükleme (`PopulateHomeHotelGalleriesAsync`)
- [x] H1.05 Fallback kartlar yalnızca DB boşken
- [x] H1.06 Demo Maidan otel `ONE_CIKAN_OTEL=1`

## Demo veri paketi (`DEMO-MAIDAN-2026`)

- [x] D1 Partner: `ork-demo-partner@otelturizm.local`
- [x] D2 Otel: onaylı + yayında + öne çıkan
- [x] D3 Oda: `SUP-DEMO`, `DLX-DEMO`
- [x] D4 90 gün fiyat + hafta sonu / erken rez indirim
- [x] D5 `FIYAT_INDIRIMLERI` kayıtları
- [x] D6 `OTEL_KOSULLARI` ücretsiz iptal
- [x] D7 Otel/oda özellik + görsel DB kayıtları
- [x] D8 `tools/DemoImageSeed --codes=DEMO-MAIDAN-2026`

## Canlı doğrulama checklist

- [ ] `https://otelturizm.com` — Maidan kartı ana sayfada
- [ ] `https://otelturizm.com/oteller` — liste + filtre
- [ ] `https://otelturizm.com/oteller?q=maidan` — arama
- [ ] Otel detay 2 oda + fiyat takvimi
- [ ] Görseller `/uploads/images/{otelId}/` 200

## P0 tamamlanan (bu oturum)

1. Demo otel tam seed SQL
2. Ana sayfa galeri batch
3. Liste ücretsiz iptal rozeti + SQL join
4. Filtre URL hydrate (`?q=`)
5. DemoImageSeed `DEMO-%` kod desteği

## UI parity sprint (2026-05-25) — anasayfa referans

- [x] U1 Kart `--radius-premium` + gölge (`otelliste_masaustu.css`)
- [x] U2 Grid gap 24px, amenity pill token font
- [x] U3 Mobil touch 44px (CTA, fav, pagination, drawer close)
- [x] U4 Mobil drawer duplicate sıfırla/uygula gizleme
- [x] U5 Konsept pill `border-radius: 999px`
- [x] U6 Arama input token font, search row radius
- [x] U7 Kart hover galeri swipe (L1.15)

## Tarayıcı denetimi (2026-05-25) — mobil/masaüstü

| # | Bulgu | Düzeltme |
|---|--------|----------|
| B1 | `otelliste_mobil.css` yüklenmiyordu (`otelliste_mobil.mobile.css` 404) | `_PublicPremiumPageLayout` `ResolveMobileStylesheet` `_mobil` suffix tanıdı |
| B2 | Sayfa açılışında “Filtrelere uygun otel yok” + kart birlikte | Fiyat min/max varsayılan değerleri kaldırıldı; filtre yalnızca kullanıcı girince aktif |
| B3 | Mobilde filtre barı görünmüyordu | `otelliste_mobil.css` yükleme + `display:flex !important` |
| B4 | Masaüstü: sidebar filtre + grid OK | — |
| B5 | Mobil: drawer filtre + sticky bar + tek sütun kart | CSS dosyası fix sonrası doğrulandı |

**Mobil 200 maddelik plan:** [OTEL_LISTELEME_MOBILE_FULL_PLAN.md](OTEL_LISTELEME_MOBILE_FULL_PLAN.md) — M-v2 yatay kart, amenity scroll, CTA foot (2026-05-25)
- [x] U8 Lead oda adı alt satır (L1.16)
