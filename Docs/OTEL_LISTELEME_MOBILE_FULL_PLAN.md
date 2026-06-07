# Otel listeleme mobil — 200 maddelik gelişim planı

Route: `/oteller` · CSS: `otelliste_mobil.css` · JS: `otelliste-filters.js`, `otelliste-list-mobile.js`

Referans: anasayfa premium tokenları, Booking/Expedia mobil liste UX, mevcut `otelliste-*` DOM sözleşmesi.

**Durum:** 2026-05-25 tarayıcı denetimi + M-v2 sprint (yatay kart, amenity scroll, foot CTA)

---

## M1 — Sayfa iskeleti & sütun düzeni (10)

- [x] M1.01 `_mobil` CSS dosya adı layout resolver fix
- [x] M1.02 Tek sütun grid `grid-template-columns: 1fr`
- [x] M1.03 Yatay kompakt kart: görsel sol 118px, içerik sağ
- [x] M1.04 `min-width: 0` ile flex/grid taşma önleme
- [x] M1.05 Mobil bar negatif margin kaldır — padding hizası
- [ ] M1.06 `100vw` yatay scroll sıfır (overflow-x audit tüm bloklar)
- [x] M1.07 Safe-area alt padding footer + sticky bar
- [ ] M1.08 901–1024px ara breakpoint tek sütun zorla
- [ ] M1.09 Landscape 568px kart yükseklik min/max
- [ ] M1.10 Print stylesheet mobil gizleme

## M2 — Üst arama şeridi (10)

- [x] M2.01 Tek satır arama: ikon + input + Ara (stack yerine)
- [x] M2.02 Yuvarlatılmış search capsule `border-radius: 14px`
- [ ] M2.03 Voice search hook (placeholder genişletme)
- [ ] M2.04 Son aramalar chips (localStorage)
- [ ] M2.05 Submit loading state
- [ ] M2.06 Autocomplete dropdown mobil tam genişlik
- [ ] M2.07 Klavye açılınca sticky bar gizle/göster
- [x] M2.08 Aktif kriter chip wrap + scroll
- [ ] M2.09 Temizle touch target 44px
- [ ] M2.10 i18n placeholder `SharedLocalizer`

## M3 — Sticky filtre/sıralama bar (10)

- [x] M3.01 Sticky top `header-sticky-h` offset
- [x] M3.02 Filtrele | Sırala | Harita 3'lü düzen
- [ ] M3.03 Scroll-down bar compact mod (mini)
- [ ] M3.04 Aktif filtre badge animasyon
- [x] M3.05 Blur arka plan + border-bottom
- [ ] M3.06 Harita ikon tooltip
- [ ] M3.07 Sıralama native picker iOS stil
- [ ] M3.08 Bar z-index header alt/üst çakışma testi
- [ ] M3.09 Reduced motion sticky transition kapalı
- [ ] M3.10 Bar aria `role=toolbar`

## M4 — Sonuç başlığı & meta (10)

- [x] M4.01 Başlık + otel sayısı dikey stack
- [x] M4.02 Masaüstü sort/map gizle
- [ ] M4.03 Uzun arama metni 2 satır clamp + expand
- [ ] M4.04 Breadcrumb mobil gizle
- [ ] M4.05 Sadakat chip (giriş yapmış)
- [ ] M4.06 Sponsor pin banner
- [ ] M4.07 Sonuç sayısı skeleton
- [ ] M4.08 Pull-to-refresh hook
- [ ] M4.09 Headline JSON-LD uyumu
- [ ] M4.10 Screen reader sonuç güncelleme `aria-live`

## M5 — Konsept pill şeridi (10)

- [x] M5.01 Yatay scroll `scroll-snap-type: x mandatory`
- [ ] M5.02 Snap center pill
- [x] M5.03 Aktif pill crimson vurgu
- [ ] M5.04 Pill ikon + metin hizası
- [ ] M5.05 Fade edge gradient scroll hint
- [ ] M5.06 Kampanya pill server-driven genişletme
- [ ] M5.07 Pill touch min-height 36px
- [ ] M5.08 RTL locale pill scroll
- [ ] M5.09 Pill analytics click
- [ ] M5.10 Pill keyboard focus ring

## M6 — Kart medya (10)

- [x] M6.01 Yatay layout sol sütun görsel
- [x] M6.02 Görsel `object-fit: cover` sabit min-height
- [ ] M6.03 Touch swipe galeri (data-gallery)
- [ ] M6.04 Galeri dot indicator
- [ ] M6.05 Lazy load + blur-up LQIP
- [ ] M6.06 WebP/srcset responsive
- [ ] M6.07 Video kapak desteği hook
- [x] M6.08 Badge + rating konum mobil küçült
- [ ] M6.09 Sponsor “Öne çıkan” ribbon
- [ ] M6.10 Medya aspect 4:5 alternatif toggle

## M7 — Kart gövde metin (10)

- [x] M7.01 Başlık 2 satır clamp
- [x] M7.02 Konum tek satır ellipsis
- [x] M7.03 Lead oda satırı kompakt
- [ ] M7.04 Yıldız inline rating chip
- [ ] M7.05 İndirim yüzde pill
- [ ] M7.06 Ücretsiz iptal yeşil micro-badge
- [ ] M7.07 Mesafe km (konum API)
- [ ] M7.08 Tesis tipi chip (Butik/Resort)
- [ ] M7.09 Kart tıklama ripple/haptic hook
- [ ] M7.10 Long press favori preview

## M8 — Kart fiyat & CTA (10)

- [x] M8.01 Foot her zaman görünür — column stack
- [x] M8.02 Tam genişlik “İncele” CTA 44px
- [x] M8.03 Fiyat + “/ gece” + vergi notu
- [ ] M8.04 Çizili eski fiyat indirimli kart
- [ ] M8.05 “X gece toplam” tarih query ile
- [ ] M8.06 Fiyat yok “Fiyat sor” CTA secondary
- [ ] M8.07 Sticky mini CTA scroll (opsiyonel)
- [ ] M8.08 CTA loading navigate state
- [ ] M8.09 Deep link detay + oda param
- [ ] M8.10 A/B CTA metin (“Rezervasyon” vs “İncele”)

## M9 — Olanak pill şeridi (10)

- [x] M9.01 Yatay scroll taşma düzeltmesi
- [x] M9.02 `flex-shrink: 0` pill
- [ ] M9.03 Max 3 görünür + “+N” expand
- [ ] M9.04 İkon tutarlılık audit
- [ ] M9.05 Özellik filtresi ile pill sync highlight
- [ ] M9.06 UPPERCASE → title case CSS
- [ ] M9.07 Scroll momentum iOS
- [ ] M9.08 Amenities keyboard scroll
- [ ] M9.09 High contrast mode border
- [ ] M9.10 Screen reader amenity list hidden full

## M10 — Favori butonu (10)

- [x] M10.01 44×44 touch target
- [ ] M10.02 Giriş yönlendirme toast
- [ ] M10.03 Animasyon heart pop
- [ ] M10.04 Offline favori queue
- [ ] M10.05 aria-pressed sync
- [ ] M10.06 Favori sayısı badge (otel)
- [ ] M10.07 Konum: medya sağ alt vs header
- [ ] M10.08 Double-tap engelle
- [ ] M10.09 Analytics favori event
- [ ] M10.10 Panel favoriler sync

## M11 — Filtre drawer kabuk (10)

- [x] M11.01 Tam ekran slide-in
- [x] M11.02 Overlay blur
- [x] M11.03 Footer Sıfırla | Uygula grid
- [ ] M11.04 Swipe-right kapat gesture
- [ ] M11.05 Focus trap a11y
- [ ] M11.06 Body scroll lock iOS fix
- [ ] M11.07 Drawer açık URL hash `#filters`
- [ ] M11.08 Escape kapat
- [ ] M11.09 Drawer anim reduced motion
- [ ] M11.10 Z-index header üstü

## M12 — Filtre grupları (10)

- [ ] M12.01 Accordion collapse gruplar
- [ ] M12.02 Fiyat slider dual-handle
- [ ] M12.03 Yıldız chip touch 44px
- [ ] M12.04 Checkbox list virtualize (>20)
- [ ] M12.05 İl→ilçe→mahalle cascade loading
- [ ] M12.06 Kampanya görsel checkbox
- [ ] M12.07 Aktif filtre özeti drawer üst
- [ ] M12.08 Server-side filtre query sync
- [ ] M12.09 Tarih aralığı date picker mobil
- [ ] M12.10 Misafir sayısı stepper

## M13 — Sıralama (10)

- [x] M13.01 Mobil select compact
- [x] M13.02 URL `?sort=` persist
- [ ] M13.03 Bottom sheet sort alternatif UI
- [ ] M13.04 Sıralama açıklama tooltip
- [ ] M13.05 Önerilen algoritma badge
- [ ] M13.06 Sort + filter kombine state
- [ ] M13.07 Analytics sort change
- [ ] M13.08 Disabled sort empty list
- [ ] M13.09 i18n sort labels
- [ ] M13.10 Sort restore back navigation

## M14 — Boş durum (10)

- [x] M14.01 Client/server empty ayrımı
- [x] M14.02 `[hidden]` grid layout fix
- [ ] M14.03 Illustrasyon SVG empty
- [ ] M14.04 Önerilen alternatif şehirler
- [ ] M14.05 Haritaya yönlendir CTA
- [ ] M14.06 Filtre sıfırla one-tap
- [ ] M14.07 Empty SEO noindex koşul
- [ ] M14.08 Skeleton → empty transition
- [ ] M14.09 Error state network
- [ ] M14.10 Empty analytics

## M15 — Sayfalama (10)

- [x] M15.01 Touch target 44px sayfa link
- [ ] M15.02 Infinite scroll alternatif
- [ ] M15.03 “Daha fazla yükle” butonu
- [ ] M15.04 Sayfa scroll top on change
- [ ] M15.05 Prev/next sticky footer
- [ ] M15.06 Page input jump
- [ ] M15.07 Total count göster
- [ ] M15.08 SEO rel prev/next
- [ ] M15.09 Cache page state
- [ ] M15.10 Pagination loading

## M16 — Performans (10)

- [ ] M16.01 İlk kart LCP preload
- [ ] M16.02 Below-fold lazy images
- [ ] M16.03 CSS critical inline hero
- [ ] M16.04 JS defer filters
- [ ] M16.05 Content-visibility kart list
- [ ] M16.06 Passive scroll listeners
- [ ] M16.07 Image dimensions width/height attr
- [ ] M16.08 Service worker cache CSS
- [ ] M16.09 3G throttling test <3s FCP
- [ ] M16.10 Bundle otelliste JS split

## M17 — Erişilebilirlik (10)

- [ ] M17.01 Skip to results link
- [ ] M17.02 Filter drawer focus return
- [ ] M17.03 Star filter keyboard toggle
- [ ] M17.04 Color contrast WCAG AA audit
- [ ] M17.05 Touch target spacing 8px min
- [ ] M17.06 aria-label favori/harita
- [ ] M17.07 Live region filter count
- [ ] M17.08 Reduced motion tüm anim
- [ ] M17.09 High contrast theme
- [ ] M17.10 Screen reader kart okuma sırası

## M18 — Dokunmatik jestler (10)

- [ ] M18.01 Kart swipe actions (favori/harita)
- [x] M18.02 Amenity strip scroll snap (plan)
- [ ] M18.03 Pull refresh liste
- [ ] M18.04 Haptic feedback CTA (PWA)
- [ ] M18.05 Pinch zoom engelle görsel
- [ ] M18.06 Long press context menu
- [ ] M18.07 Drawer swipe dismiss
- [ ] M18.08 Double-tap zoom önleme
- [ ] M18.09 Touch drag sort (favori)
- [ ] M18.10 Gesture conflict harita link

## M19 — i18n & locale (10)

- [ ] M19.01 `/en/hotels` mobil CSS parity
- [ ] M19.02 RTL layout mirror test
- [ ] M19.03 Currency format locale
- [ ] M19.04 Date format TR/EN
- [ ] M19.05 Sort/filter label resource
- [ ] M19.06 Concept pill çeviri
- [ ] M19.07 hreflang mobil canonical
- [ ] M19.08 Number input locale
- [ ] M19.09 Typography CJK fallback
- [ ] M19.10 Geo default city locale

## M20 — QA, analytics, release (10)

- [x] M20.01 Canlı mobil screenshot checklist
- [ ] M20.02 BrowserStack cihaz matrisi
- [ ] M20.03 Visual regression Percy
- [ ] M20.04 GA4 listing filter events
- [ ] M20.05 Hotjar scroll heatmap
- [ ] M20.06 Sentry layout error boundary
- [ ] M20.07 Lighthouse mobile ≥90
- [ ] M20.08 MSDeploy CSS cache bust verify
- [ ] M20.09 Rollback plan dokümantasyon
- [ ] M20.10 Playwright mobil E2E smoke

---

## M-v2 sprint tamamlanan (2026-05-25)

1. Yatay kompakt kart düzeni (görsel sol, metin sağ)
2. Amenity yatay scroll — taşma giderildi
3. Foot column: fiyat + tam genişlik CTA
4. Arama tek satır capsule
5. Sticky bar padding hizası
6. Konsept pill scroll-snap
7. Sonuç başlığı stack
8. `otelliste-list-mobile.js` — touch galeri swipe hook

**İlerleme:** 38/200 tamamlandı · Sonraki P0: M12 accordion filtre, M6.03 touch galeri, M1.08 ara breakpoint
