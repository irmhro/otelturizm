# Otel detay (`/oteller/{slug}`) — 400 maddelik gelişim planı

Dosya sözleşmesi: `OtelDetay.cshtml`, `oteldetay_masaustu.css`, `oteldetay_mobil.css`, partial `_OtelDetay*`.

## D0 — Altyapı — route, service, cache

- [ ] **D0.01** Route `/oteller/{slug}` + i18n prefix
- [ ] **D0.02** Controller `OtellerController.OtelDetay`
- [ ] **D0.03** Service `GetHotelDetailPageAsync`
- [ ] **D0.04** VM `HotelDetailPageViewModel`
- [ ] **D0.05** Publish/onay SQL kapıları
- [ ] **D0.06** Slug çözümleme
- [ ] **D0.07** 2dk output cache
- [ ] **D0.08** CloneHotelDetail izolasyonu
- [x] **D0.09** `HotelDetailLoadOptions` deep link
- [ ] **D0.10** Connection string guard
- [ ] **D0.11** Tax display yüzdeleri
- [ ] **D0.12** Pricing read service entegrasyonu
- [ ] **D0.13** Blocked review words
- [ ] **D0.14** Discount meta map
- [ ] **D0.15** Room feature map
- [ ] **D0.16** Campaign join hazır
- [ ] **D0.17** Video URL alanı
- [ ] **D0.18** Growth signals hook
- [ ] **D0.19** Presence tracker
- [ ] **D0.20** Draft service entegrasyonu

## D1 — Hero & galeri

- [ ] **D1.01** Desktop gallery grid
- [ ] **D1.02** Mobil filmstrip
- [ ] **D1.03** Kapak preload LCP
- [ ] **D1.04** SlaytGorsel lightbox
- [ ] **D1.05** Star badge overlay
- [ ] **D1.06** Gallery thumb sync
- [ ] **D1.07** Ambient blur arka plan
- [ ] **D1.08** Onaylı görsel filtresi
- [ ] **D1.09** Logo upload hariç
- [ ] **D1.10** Fallback placeholder
- [ ] **D1.11** 4+ görsel grid tile
- [ ] **D1.12** Video embed slot
- [ ] **D1.13** Alt text otel adı
- [ ] **D1.14** fetchpriority first image
- [ ] **D1.15** lazy alt tiles
- [ ] **D1.16** Keyboard gallery nav
- [ ] **D1.17** Touch swipe mobil
- [ ] **D1.18** Gallery count badge
- [ ] **D1.19** Share image OG
- [ ] **D1.20** Gallery JSON-LD image array

## D2 — Breadcrumb & üst aksiyonlar

- [ ] **D2.01** Geri liste linki
- [ ] **D2.02** İlçe/il context link
- [ ] **D2.03** Paylaş menüsü WhatsApp
- [ ] **D2.04** Paylaş Facebook/X/Telegram
- [ ] **D2.05** Link kopyala
- [ ] **D2.06** Native share API
- [ ] **D2.07** Favori toggle üst
- [ ] **D2.08** Görüşme başlat CTA
- [ ] **D2.09** Auth gate mesajı
- [ ] **D2.10** Rezervasyon şartı rozeti
- [ ] **D2.11** Intent segment chip
- [ ] **D2.12** Live presence band
- [ ] **D2.13** Viewer band growth
- [ ] **D2.14** Breadcrumb schema
- [ ] **D2.15** Mobil breadcrumb wrap
- [ ] **D2.16** Quick action tone classes
- [ ] **D2.17** aria-label aksiyonlar
- [ ] **D2.18** Ctrl+share kapatma
- [ ] **D2.19** Focus trap share menu
- [ ] **D2.20** Sticky header polish

## D3 — Quick facts

- [ ] **D3.01** Check-in saati DB
- [ ] **D3.02** Check-out saati DB
- [ ] **D3.03** Yıldız chip
- [ ] **D3.04** Otel tipi chip
- [x] **D3.05** Ücretsiz iptal chip
- [ ] **D3.06** Mobil facts scroll
- [ ] **D3.07** Icon + label grid
- [ ] **D3.08** Localized labels
- [ ] **D3.09** Timezone TR
- [ ] **D3.10** Default saat fallback
- [ ] **D3.11** Facts aria-label
- [ ] **D3.12** Responsive wrap
- [ ] **D3.13** Chip tone variants
- [ ] **D3.14** Campaign chips
- [ ] **D3.15** Rating jump link
- [ ] **D3.16** Weather inline trigger
- [ ] **D3.17** Header metrics stack
- [ ] **D3.18** Star i18n
- [ ] **D3.19** Property type mapping
- [ ] **D3.20** Quick facts CSS token

## D4 — Hakkında & içerik

- [ ] **D4.01** Kısa açıklama vitrin
- [ ] **D4.02** Uzun açıklama expand
- [ ] **D4.03** Konum açıklaması
- [ ] **D4.04** Mojibake fix helper
- [ ] **D4.05** HTML sanitize
- [ ] **D4.06** Boş içerik fallback
- [ ] **D4.07** About section anchor
- [ ] **D4.08** Read more toggle
- [ ] **D4.09** SEO description kaynağı
- [ ] **D4.10** Çok dilli alan hazır
- [ ] **D4.11** Paragraph spacing
- [ ] **D4.12** Typography token uyumu
- [ ] **D4.13** Section head Tabler
- [ ] **D4.14** Anchor #about
- [ ] **D4.15** Print friendly text
- [ ] **D4.16** Admin içerik senkron
- [ ] **D4.17** Rich text future slot
- [ ] **D4.18** Location description map tie
- [ ] **D4.19** Intent-aware copy slot
- [ ] **D4.20** Content card od-section

## D5 — Tesis olanakları

- [ ] **D5.01** DB `OTEL_OZELLIK_ILISKILERI`
- [ ] **D5.02** Icon class map
- [ ] **D5.03** Featured top 4 grid
- [ ] **D5.04** Hidden amenities modal
- [ ] **D5.05** Expand all details
- [ ] **D5.06** Amenity subtext helper
- [ ] **D5.07** Modal focus trap
- [ ] **D5.08** Keyboard ESC close
- [ ] **D5.09** Fallback amenity set
- [ ] **D5.10** Filterlenebilir özellikler
- [ ] **D5.11** One cikan özellik sırası
- [ ] **D5.12** Mobil 2-col grid
- [ ] **D5.13** Amenity count badge
- [ ] **D5.14** i18n amenity names
- [ ] **D5.15** Duplicate label merge
- [ ] **D5.16** Accessibility list roles
- [ ] **D5.17** Amenity search in modal
- [ ] **D5.18** Partner sync doğrulama
- [ ] **D5.19** SVG icon fallback
- [ ] **D5.20** Section od-section--amenities

## D6 — Otel kuralları

- [x] **D6.01** DB `OTEL_KOSULLARI` load
- [x] **D6.02** İptal özeti gösterimi
- [x] **D6.03** Detaylı iptal metni
- [x] **D6.04** Ücretsiz iptal saati
- [x] **D6.05** Sigara politikası
- [x] **D6.06** Evcil hayvan politikası
- [x] **D6.07** Çocuk politikası
- [x] **D6.08** Ön ödeme rozeti
- [x] **D6.09** Kart kabul bilgisi
- [x] **D6.10** Partial `_OtelDetayPolicies`
- [x] **D6.11** od-policy-list CSS
- [x] **D6.12** Mobil policy stack
- [ ] **D6.13** Room cancellation fallback
- [ ] **D6.14** Policy anchor link
- [ ] **D6.15** Partner panel sync
- [x] **D6.16** Idempotent seed
- [x] **D6.17** Empty state hide
- [x] **D6.18** i18n policy labels
- [ ] **D6.19** Schema lodging policy
- [ ] **D6.20** Print policy block

## D7 — Oda kartları

- [ ] **D7.01** Aktif oda tipleri SQL
- [ ] **D7.02** Kapak + galeri merge
- [ ] **D7.03** Oda özellik listesi
- [ ] **D7.04** Feature expand panel
- [ ] **D7.05** Oda detay modal
- [ ] **D7.06** SlaytGorsel oda galeri
- [ ] **D7.07** Specs metin builder
- [ ] **D7.08** Metrekare/yatak
- [ ] **D7.09** Max misafir/adult/child
- [ ] **D7.10** Select room CTA
- [ ] **D7.11** Discount stack UI
- [ ] **D7.12** Discount info modal
- [ ] **D7.13** Cancellation badge
- [ ] **D7.14** Closed room filter
- [ ] **D7.15** No price state
- [ ] **D7.16** Lead room highlight
- [x] **D7.17** Query `room` preselect
- [ ] **D7.18** Scroll to booking
- [ ] **D7.19** Room card od-room-card
- [ ] **D7.20** Mobil room stack

## D8 — Fiyat & indirim

- [ ] **D8.01** Bugün effective price
- [ ] **D8.02** Date-range quote API
- [ ] **D8.03** `FIYAT_INDIRIMLERI` meta
- [ ] **D8.04** Kampanya etiketi
- [ ] **D8.05** VAT+konaklama inclusive
- [ ] **D8.06** Nightly breakdown details
- [ ] **D8.07** Multi-night total line
- [ ] **D8.08** Price refresh on date change
- [ ] **D8.09** Closed sales guard
- [ ] **D8.10** Stock zero guard
- [ ] **D8.11** Currency TRY format
- [ ] **D8.12** Discount percent pill
- [ ] **D8.13** Base vs discount display
- [ ] **D8.14** Tax note vergiler dahil
- [ ] **D8.15** Quote error toast
- [ ] **D8.16** Loading skeleton price
- [ ] **D8.17** Adult/child price rules
- [ ] **D8.18** Corporate price exclude
- [ ] **D8.19** Rate plan future hook
- [ ] **D8.20** Price cache bust on date

## D9 — Rezervasyon paneli

- [ ] **D9.01** Sticky desktop sidebar
- [ ] **D9.02** Mobil bottom sheet
- [ ] **D9.03** Multi-room JSON builder
- [ ] **D9.04** Room add/remove
- [ ] **D9.05** Date inputs min today
- [ ] **D9.06** Check-out > check-in validation
- [ ] **D9.07** Payment method select
- [ ] **D9.08** Bank transfer upload
- [ ] **D9.09** Profile completion gate
- [ ] **D9.10** Anti-forgery token
- [ ] **D9.11** Rate limit create
- [ ] **D9.12** Velocity guard
- [ ] **D9.13** Idempotency key
- [ ] **D9.14** Draft resume banner
- [ ] **D9.15** Draft cancel form
- [ ] **D9.16** Confirm modal
- [ ] **D9.17** Guest vs logged-in flow
- [x] **D9.18** Query checkIn/out hydrate
- [x] **D9.19** ContinueDraft query
- [ ] **D9.20** Reservation POST slug route

## D10 — Yorumlar

- [ ] **D10.01** Onaylı yorum SQL
- [ ] **D10.02** Aggregate score bars
- [ ] **D10.03** Topic keyword chips
- [ ] **D10.04** Review sort new/old/best
- [ ] **D10.05** Review pagination query
- [ ] **D10.06** Verified stay badge
- [ ] **D10.07** Mask guest name
- [ ] **D10.08** Blocked word filter
- [ ] **D10.09** Empty review state
- [ ] **D10.10** Eligible review stays
- [ ] **D10.11** Review form POST
- [ ] **D10.12** Score 1-10 normalize
- [ ] **D10.13** Travel profile label
- [ ] **D10.14** Memnuniyet etiketi
- [ ] **D10.15** Review card expand
- [ ] **D10.16** Devamını oku
- [ ] **D10.17** Schema review aggregate
- [x] **D10.18** Demo seed yorumlar
- [ ] **D10.19** Admin onay durumu
- [ ] **D10.20** Review section anchor

## D11 — Harita & konum

- [ ] **D11.01** Leaflet embed
- [ ] **D11.02** Lat/lon data attrs
- [ ] **D11.03** Google Maps external link
- [ ] **D11.04** Fallback no coords
- [ ] **D11.05** Address display
- [ ] **D11.06** Geo JSON-LD
- [ ] **D11.07** Mahalle/ilce ids
- [ ] **D11.08** Location description
- [ ] **D11.09** Map height responsive
- [ ] **D11.10** Marker popup otel adı
- [ ] **D11.11** Map lazy init
- [ ] **D11.12** Mobil map 200px
- [ ] **D11.13** Konum section anchor
- [ ] **D11.14** Transport score tie-in
- [ ] **D11.15** Nearby POI future
- [ ] **D11.16** Static map fallback
- [ ] **D11.17** Cookie location session
- [ ] **D11.18** Harita erişilebilirlik
- [ ] **D11.19** Map CSP nonce scripts
- [ ] **D11.20** od-section--map

## D12 — Benzer oteller

- [x] **D12.01** Same city SQL top 6
- [ ] **D12.02** Exclude current hotel
- [ ] **D12.03** Price from pricing service
- [ ] **D12.04** Rating text builder
- [x] **D12.05** Slug link generate
- [x] **D12.06** Cover image normalize
- [x] **D12.07** Partial `_OtelDetaySimilarHotels`
- [x] **D12.08** od-similar-grid CSS
- [x] **D12.09** Mobil 1-col cards
- [x] **D12.10** Empty hide section
- [x] **D12.11** Listing cross-link
- [x] **D12.12** Cache similar block
- [x] **D12.13** Sponsor exclude
- [ ] **D12.14** Featured boost sort
- [ ] **D12.15** District proximity future
- [ ] **D12.16** Card hover motion
- [ ] **D12.17** Placeholder image
- [ ] **D12.18** Similar section anchor
- [ ] **D12.19** Analytics impression
- [ ] **D12.20** A/B similar count

## D13 — Growth, hava, sosyal kanıt

- [ ] **D13.01** Weather widget inject
- [ ] **D13.02** 3-day forecast popup
- [ ] **D13.03** Weather theme classes
- [ ] **D13.04** District forecast label
- [ ] **D13.05** Active viewer band
- [ ] **D13.06** Live presence count
- [ ] **D13.07** Intent cookie trip=
- [ ] **D13.08** SearchCtx cookie city
- [ ] **D13.09** Favori count sync
- [ ] **D13.10** Campaign JSON script
- [ ] **D13.11** Promo badge header
- [ ] **D13.12** Loyalty touchpoint slot
- [ ] **D13.13** Referrer capture
- [ ] **D13.14** Social proof microcopy
- [ ] **D13.15** Urgency ethical guard
- [ ] **D13.16** Band TTL cache
- [ ] **D13.17** Weather error fallback
- [ ] **D13.18** Growth signals log
- [ ] **D13.19** A/B band copy
- [ ] **D13.20** Cookie consent aware

## D14 — SEO & uluslararası

- [ ] **D14.01** Title/meta ViewData
- [ ] **D14.02** Canonical `/oteller/{slug}`
- [ ] **D14.03** OG image main photo
- [ ] **D14.04** OG locale alternates
- [ ] **D14.05** Hreflang routes
- [ ] **D14.06** JSON-LD Hotel
- [ ] **D14.07** JSON-LD Breadcrumb
- [ ] **D14.08** InternationalSeo meta builder
- [ ] **D14.09** Slug unicode normalize
- [ ] **D14.10** Robots index follow
- [ ] **D14.11** Noindex draft test
- [ ] **D14.12** Structured address
- [ ] **D14.13** AggregateRating schema
- [ ] **D14.14** Offer schema future
- [ ] **D14.15** Sitemap detail URL
- [ ] **D14.16** Slug redirect 301
- [ ] **D14.17** Duplicate slug guard
- [ ] **D14.18** Meta description truncate
- [ ] **D14.19** Twitter card tags
- [ ] **D14.20** Detail culture route prefix

## D15 — JavaScript davranış

- [ ] **D15.01** `otel-detay.js` config
- [ ] **D15.02** Inline v41 script guard
- [ ] **D15.03** Gallery SlaytGorsel
- [ ] **D15.04** Share menu toggle
- [ ] **D15.05** Favorite sync API
- [ ] **D15.06** Booking price recalc
- [ ] **D15.07** Multi-room template
- [ ] **D15.08** Room detail modal
- [ ] **D15.09** Amenities modal
- [ ] **D15.10** Weather popup
- [ ] **D15.11** Map Leaflet init
- [ ] **D15.12** Review read more
- [ ] **D15.13** Scroll booking CTA
- [ ] **D15.14** Select room scroll
- [ ] **D15.15** Profile modal
- [ ] **D15.16** Confirm reservation modal
- [ ] **D15.17** Draft prompt logic
- [ ] **D15.18** Payment mix calculator
- [ ] **D15.19** Nightly breakdown fetch
- [ ] **D15.20** Reduced motion respect

## D16 — CSS masaüstü — `oteldetay_masaustu.css`

- [ ] **D16.01** Shell `--od-*` tokens
- [ ] **D16.02** Page max width 1240
- [ ] **D16.03** Breadcrumb layout
- [ ] **D16.04** Gallery grid desktop
- [ ] **D16.05** Detail grid 2-col
- [ ] **D16.06** Sticky booking sidebar
- [ ] **D16.07** Room card hover
- [ ] **D16.08** Amenities grid 4
- [ ] **D16.09** Review bars styling
- [ ] **D16.10** Map card radius
- [ ] **D16.11** Modal overlay z-index
- [ ] **D16.12** Chip components
- [x] **D16.13** Policy list grid
- [x] **D16.14** Similar 3-col grid
- [ ] **D16.15** Tabler section head
- [ ] **D16.16** Button crimson theme
- [ ] **D16.17** Typography Plus Jakarta
- [ ] **D16.18** Shadow premium
- [ ] **D16.19** Focus visible states
- [ ] **D16.20** Print stylesheet hook

## D17 — CSS mobil — `oteldetay_mobil.css`

- [ ] **D17.01** Max-width 900 media
- [ ] **D17.02** Mobile gallery filmstrip
- [ ] **D17.03** Bottom booking bar
- [ ] **D17.04** Booking sheet full screen
- [ ] **D17.05** Touch target 40px
- [ ] **D17.06** Facts horizontal scroll
- [ ] **D17.07** Room card stack
- [ ] **D17.08** Review pager compact
- [x] **D17.09** Map height 200px
- [x] **D17.10** Similar 1-col
- [x] **D17.11** Policy compact icons
- [ ] **D17.12** Modal full bleed
- [ ] **D17.13** Safe area padding
- [ ] **D17.14** Sticky CTA shadow
- [ ] **D17.15** Reduced motion mobile
- [ ] **D17.16** Font size scale down
- [ ] **D17.17** Hide desktop gallery dup
- [ ] **D17.18** Swipe gallery support
- [ ] **D17.19** Mobile share sheet
- [ ] **D17.20** Performance containment

## D18 — Performans & erişilebilirlik

- [ ] **D18.01** LCP image preload
- [ ] **D18.02** Lazy below-fold images
- [ ] **D18.03** Output cache 2min
- [x] **D18.04** Skip cache on date query
- [ ] **D18.05** SQL single round trips
- [ ] **D18.06** Gallery batch load
- [ ] **D18.07** Defer Leaflet CSS
- [ ] **D18.08** Script nonce CSP
- [ ] **D18.09** aria-modal dialogs
- [ ] **D18.10** Focus return on close
- [ ] **D18.11** Keyboard room select
- [ ] **D18.12** Color contrast AA
- [ ] **D18.13** Skip link target
- [ ] **D18.14** Live region toasts
- [ ] **D18.15** Form label association
- [ ] **D18.16** Error summary aria
- [ ] **D18.17** prefers-reduced-motion
- [ ] **D18.18** Image width/height attrs
- [ ] **D18.19** CDN upload path
- [ ] **D18.20** Core Web Vitals budget

## D19 — QA & canlı doğrulama

- [x] **D19.01** Build Release verify
- [ ] **D19.02** Maidan detay 200
- [ ] **D19.03** Galeri görseller 200
- [ ] **D19.04** 2 oda fiyat görünür
- [ ] **D19.05** Policies section DB
- [ ] **D19.06** Similar hotels render
- [ ] **D19.07** Reviews seed 5+
- [ ] **D19.08** Free cancel chip
- [ ] **D19.09** ?checkIn=&checkOut= deep link
- [ ] **D19.10** ?room= preselect
- [ ] **D19.11** Favori toggle
- [ ] **D19.12** Share copy link
- [ ] **D19.13** Map marker
- [ ] **D19.14** Mobile sheet booking
- [ ] **D19.15** Draft resume flow
- [ ] **D19.16** Guest checkout path
- [ ] **D19.17** Logged-in profile gate
- [ ] **D19.18** sqlcmd seed sırası
- [ ] **D19.19** MSDeploy CSS/Views
- [ ] **D19.20** Cache purge after deploy

## P0 bu oturumda tamamlanan
- OTEL_KOSULLARI → detay policies partial
- SimilarHotels → `_OtelDetaySimilarHotels` partial
- `?checkIn` / `?checkOut` / `?room` deep link + cache bypass
- Ücretsiz iptal quick fact chip
- Demo Maidan yorum seed SQL

## UI parity sprint (2026-05-25) — mobil + masaüstü

- [x] **U-D1** Mobil galeri kart kabuğu (rounded shell, thumb strip, nav 44px)
- [x] **U-D2** Breadcrumb geri link gizle (galeri chrome back)
- [x] **U-D3** Yorumlar OTA layout CSS (mobil stack + masaüstü grid)
- [x] **U-D4** Section headrow mobil stack + full-width aksiyon
- [x] **U-D5** Sticky booking bar z-index 1020 + sheet grabber
- [x] **U-D6** Benzer oteller 2-col @1100px
- [x] **U-D7** Legacy `otel-detay-world.css` token birleştirme
- [x] **U-D8** Desktop gallery overlap margin düzeltmesi

## Tarayıcı denetimi (2026-05-25) — mobil/masaüstü + rezervasyon

| # | Bulgu | Düzeltme |
|---|--------|----------|
| B1 | `oteldetay_mobil.css` yüklenmiyordu | Layout `_mobil` suffix fix |
| B2 | Mobilde masaüstü galeri + sheet aynı anda | Mobil CSS yükleme; desktop galeri `display:none` |
| B3 | Alt sticky bar (`mobile-booking-bar`) mobilde gizli kalıyordu | `display:flex !important` |
| B4 | Rezervasyon sheet kapalıyken layout boşluğu | `visibility:hidden` when not `.is-open` |
| B5 | Rezervasyon akışı: tarih/oda/ödeme/plan formu mobil sheet içinde | Mevcut JS `openBookingSheet` — sheet açılışı doğrulandı |
| B6 | Masaüstü: sidebar booking sticky + oda kartları 2-col | OK |
