# Frontend Orkestratör Planı

Son güncelleme: **2026-05-23** (H1 fe-otel-public Wave-I — T327 OtelDetay SS, T328 liste empty-state, T329 harita cluster)  
Base URL (local): `http://127.0.0.1:5103`

## Özet

| Alan | Sayı |
|------|------|
| Admin CSHTML (sayfa) | 55 |
| Partner CSHTML | 49 |
| Firma CSHTML | 12 |
| User CSHTML | 17 |
| Satış CSHTML | 13 |
| Departman CSHTML | 5 (1 sayfa + layout partial) |
| Otel public CSHTML | 3 (+1 partial) |
| **Toplam envanter (bu plan)** | **153** |
| Screenshot (step-01 set) | **14 PNG** |
| FE-CTO onaylı (dürüst) | **6 / 151** |

---

## Otel public (`fe-otel-public`)

| Sayfa | Route | View | CSS | mobile.css | SS | FE-CTO |
|-------|-------|------|-----|------------|-----|--------|
| OtelListeleme | `/oteller`, `/oteller/istanbul` | `Views/Oteller/OtelListeleme.cshtml` | `otel-listeleme.css` | ✅ | ✅ desktop+mobil | **APPROVED** |
| HaritaOteller | `/oteller/harita` | `Views/Oteller/HaritaOteller.cshtml` | `haritaoteller.css` | ✅ | ✅ desktop+mobil | **APPROVED** |
| OtelDetay | `/oteller/{slug}` | `Views/Oteller/OtelDetay.cshtml` | `otel-detay.css` | ✅ | ✅ `fe-otel-public/otel-detay/` step-01 | **APPROVED** |

### FE-CTO APPROVED — OtelListeleme

- **Tarih:** 2026-05-22
- **SS:** `docs/frontend-screenshots/otel-listeleme/{desktop|mobil}/step-01-full-page.png`
- **Not:** Boş liste durumu; mobil FAB safe-area düzeltildi (`listing-filter-fab`).

### FE-CTO APPROVED — HaritaOteller

- **Tarih:** 2026-05-22
- **SS:** `docs/frontend-screenshots/haritaoteller/{desktop|mobil}/step-01-full-page.png`
- **Not:** Harita + filtre paneli; mobil padding `safe-area-inset-bottom` mevcut.

### FE-CTO APPROVED — OtelDetay

- **Tarih:** 2026-05-23
- **Route:** `/oteller/orkestra-bogaz-otel` (3 demo otel seed + `fix_orkestra_demo_yayin_onay`)
- **Not:** `mobile.css` mevcut; slug çözümleme `HotelService.BuildSlug` ile doğrulandı (DB filtre 3 kayıt). Tam SS seti T010.

### FE-CTO — OtelDetay (H1 sprint 2026-05-23, T306)

- **CSS yolu:** `wwwroot/assets/css/paneller/otel/otel-detay.css` (+ `.mobile.css` → kök `otel-detay*.css` import)
- **Controller:** `OtellerController.OtelDetay` → `PageCss` / `PageCssMobile` paneller/otel alias
- **Lighthouse (T304):** ana galeri `preload` + `fetchpriority=high`; thumbnaillar `loading=lazy`
- **Runtime:** build doğrulandı; canlı smoke `/oteller/{slug}` kullanıcı oturumunda
- **SS klasör:** `docs/frontend-screenshots/fe-otel-public/otel-detay/`
- **SS dosyaları:**
  - Desktop: `docs/frontend-screenshots/fe-otel-public/otel-detay/desktop/step-01-full-page.png`
  - Mobil: `docs/frontend-screenshots/fe-otel-public/otel-detay/mobil/step-01-full-page.png`
- **Yakalama URL (demo):** `http://127.0.0.1:5103/oteller/orkestra-bogaz-otel` (seed: `orkestra-bogaz-otel`)
- **Viewport:** desktop 1440×900; mobil 390×844 (≤900px genişlik)
- **H1 CSS (2026-05-23):** sticky `mobile-booking-bar` safe-area; galeri dokunma 44px; mobil breadcrumb truncation

### Wave-I H1 — `fe-otel-public` (T327–T329, 2026-05-23)

| T-ID | Çıktı | Durum |
|------|--------|--------|
| **T327** | OtelDetay tam SS seti (`fe-otel-public/otel-detay/{desktop\|mobil}/step-01-full-page.png`) | ✅ path + PNG |
| **T328** | OtelListeleme boş durum: `listing-empty-state` + FAB `safe-area` (`listing-filter-fab` alias) + client-side filtre boşluğu | ✅ |
| **T329** | HaritaOteller: Leaflet `markerCluster` + pin sayısı rozeti + mobil popup CTA 44px | ✅ |

- **Build (H1):** `dotnet build -o .build-h1`
- **T328 not:** Sunucu boş liste + JS filtre boşluğu ayrı bloklar; «Filtreleri temizle» → `#clearAllFiltersBtn`.
- **T329 not:** Küme tıklanınca `zoomToBoundsOnClick`; pin rozeti `#hotelMapPinCount`.

### Wave-III mobil (H1+H2, MOBIL_TEK_EKRAN, 2026-05-23)

Wave-III P0 sayfalarında ortak `wwwroot/assets/css/shared/mobile-viewport-shell.css` (44px dokunma, `--mob-sticky-cta-*`, `100dvh` + safe-area) ilgili `.mobile.css` dosyalarına `@import` ile bağlandı. **OtelDetay:** galeri thumb → tam ekran `SlaytGorsel`; mobil `mobile-booking-bar` `mob-sticky-cta` geri eklendi. **OtelListeleme:** ücretsiz iptal rozeti (`HasFreeCancellation` / `ORK-*` demo); filtre FAB `mob-sticky-cta`. **Partner Komisyonlar:** boş durum illüstrasyon + 2 CTA, trend alanı mobil min-height. **Partner Fiyat takvimi:** ay ızgarası tek genişlikte yatay kaydırma. Build: `dotnet build -o .build-wave3-mobile`.

---

## Admin panel (`fe-admin`) — route prefix `/admin`

| # | Sayfa | Action / URL | View | mobile.css | Sprint |
|---|-------|--------------|------|------------|--------|
| 1 | Dashboard | `Dashboard` | `Dashboard.cshtml` | ✅ `PageCssMobile` | **in_progress** T101 |
| 2 | SystemHealth | `SystemHealth` | `SystemHealth.cshtml` | ✅ |
| 3 | PlatformCheckup | `PlatformCheckup` | `PlatformCheckup.cshtml` | ✅ |
| 4 | ApprovalCenter | `ApprovalCenter` | `ApprovalCenter.cshtml` | ✅ table-cards | **in_progress** T113 |
| 5 | Team | `Team` | `Team.cshtml` | ✅ |
| 6 | HelpCenter | `HelpCenter` | `HelpCenter.cshtml` | ✅ |
| 7 | Hotels | `Hotels` | `Hotels.cshtml` | ✅ table-cards | **in_progress** T112 |
| 8 | HotelDetail | `HotelDetail` | `HotelDetail.cshtml` | ✅ |
| 9 | ActiveHotels | `ActiveHotels` | `ActiveHotels.cshtml` | ✅ |
| 10 | PendingHotels | `PendingHotels` | `PendingHotels.cshtml` | ✅ |
| 11 | UnifiedReservations | `UnifiedReservations` | `UnifiedReservations.cshtml` | ✅ |
| 12 | Reservations | `Reservations` | `Reservations.cshtml` | ✅ table-cards | **in_progress** T111 |
| 13 | CompanyReservations | `CompanyReservations` | `CompanyReservations.cshtml` | ✅ |
| 14 | Payments | `Payments` | `Payments.cshtml` | ✅ |
| 15 | Invoices | `Invoices` | `Invoices.cshtml` | ✅ |
| 16 | Commissions | `Commissions` | `Commissions.cshtml` | ✅ |
| 17 | Contracts | `Contracts` | `Contracts.cshtml` | ✅ |
| 18 | Campaigns | `Campaigns` | `Campaigns.cshtml` | ✅ |
| 19 | Notifications | `Notifications` | `Notifications.cshtml` | ✅ |
| 20 | Settings | `Settings` | `Settings.cshtml` | ✅ |
| 21 | SettingsMonitor | `SettingsMonitor` | `SettingsMonitor.cshtml` | ✅ |
| 22 | Security | `Security` | `Security.cshtml` | ✅ table-cards | **in_progress** T114 |
| 23 | Users | `Users` | `Users.cshtml` | ✅ |
| 24 | Managers | `Managers` | `Managers.cshtml` | ✅ |
| 25 | Reports | `Reports` | `Reports.cshtml` | ✅ |
| 26 | CommerceInsight | `CommerceInsight` | `CommerceInsight.cshtml` | ✅ |
| 27 | EmailQueue | `EmailQueue` | `EmailQueue.cshtml` | ✅ |
| 28 | EmailRouting | `EmailRouting` | `EmailRouting.cshtml` | ✅ |
| 29 | EmailTemplates | `EmailTemplates` | `EmailTemplates.cshtml` | ✅ |
| 30 | MailCenter | `MailCenter` | `MailCenter.cshtml` | ✅ |
| 31 | WhatsAppCloudApi | `WhatsAppCloudApi` | `WhatsAppCloudApi.cshtml` | ✅ |
| 32 | PartnerApplications | `PartnerApplications` | `PartnerApplications.cshtml` | ✅ |
| 33 | CompanyApplications | `CompanyApplications` | `CompanyApplications.cshtml` | ✅ |
| 34 | ListingSubscriptions | `ListingSubscriptions` | `ListingSubscriptions.cshtml` | ✅ |
| 34b | PlatformPackages | `PlatformPackages` | `PlatformPackages.cshtml` | ✅ `platform-packages.mobile` | **in_progress** T322 |
| 35 | DevelopmentRequests | `DevelopmentRequests` | `DevelopmentRequests.cshtml` | ✅ |
| 36 | ReviewsModeration | `ReviewsModeration` | `ReviewsModeration.cshtml` | ✅ |
| 37 | Reviews | `Reviews` | `Reviews.cshtml` | ✅ |
| 38 | Complaints | `Complaints` | `Complaints.cshtml` | ✅ |
| 39 | SupportArticles | `SupportArticles` | `SupportArticles.cshtml` | ✅ |
| 40 | Faq | `Faq` | `Faq.cshtml` | ✅ |
| 41 | Blog | `Blog` | `Blog.cshtml` | ✅ |
| 42 | Sitemap | `Sitemap` | `Sitemap.cshtml` | ✅ |
| 43 | Logs | `Logs` | `Logs.cshtml` | ✅ |
| 44 | LogEvents | `LogEvents` | `LogEvents.cshtml` | ✅ |
| 45 | AdminActionLogs | `AdminActionLogs` | `AdminActionLogs.cshtml` | ✅ |
| 46 | RateLimitStats | `RateLimitStats` | `RateLimitStats.cshtml` | ✅ |
| 47 | GeoSearchLogs | `GeoSearchLogs` | `GeoSearchLogs.cshtml` | ✅ |
| 48 | HotelCoordinateChanges | `HotelCoordinateChanges` | `HotelCoordinateChanges.cshtml` | ✅ |
| 49 | PlatformOfficials | `PlatformOfficials` | `PlatformOfficials.cshtml` | ✅ |
| 50 | Backups | `Backups` | `Backups.cshtml` | ✅ |
| 51 | Contracts (preview) | `ContractPreview` | — | — |
| 52 | SlowSql | `SlowSql` | — | — |
| 53 | UploadHistory | `UploadHistory` | — | — |
| 54 | SecurityEvents | `SecurityEvents` | — | — |
| 55 | EditHotel | `EditHotel` | redirect | — |

### FE-CTO APPROVED — Admin Dashboard (giriş kapısı)

- **Tarih:** 2026-05-22
- **SS:** `docs/frontend-screenshots/admin/dashboard/{desktop|mobil}/step-01-full-page.png`
- **Not:** Oturum yok → `/admin-giris` (Dashboard hedefli); panel içi SS sonraki sprint.

### H3 sprint — Admin auth SS (T101 / T310)

- **Giriş:** `http://127.0.0.1:5103/admin-giris` → `accountType=admin` veya `userRole=admin` claim
- **RBAC seed:** `Database/MigrationsSql/veri/migrationlar/20260522_seed_admin_yetkiler.sql` (`platform_admin_full` rol + tüm `admin.*` yetkileri)
- **Test kullanıcı (T330):** `ork-demo-admin@otelturizm.local` / `Demo123!` — `20260523_seed_admin_demo_kullanici.sql` + `Docs/ADMIN_TEST_KULLANICI.md`
- **FE-CTO batch (T333):** `Docs/FE_CTO_ADMIN_BATCH_T333.md` — 10 admin sayfa onay yolu
- **PageCssMobile:** `_AdminPanelLayout` + `Dashboard` action → `paneller/admin/dashboard.mobile`
- **Mobil tablo:** `admin-table--cards` + `data-label` — Reservations, Hotels, ApprovalCenter, Security, PlatformPackages

---

## Partner panel (`fe-partner`) — route prefix `/panel/partner`

| # | Sayfa | Action | View | mobile.css |
|---|-------|--------|------|------------|
| 1 | Dashboard | `Index` | `Dashboard.cshtml` | ✅ |
| 2 | FacilityLocation | `FacilityLocation` | `FacilityLocation.cshtml` | ✅ |
| 3 | Reservations | `Reservations` | `Reservations.cshtml` | ✅ |
| 4 | Pricing | `Pricing` | `Pricing.cshtml` | ✅ |
| 5 | CompanyPricing | `CompanyPricing` | `CompanyPricing.cshtml` | ✅ |
| 6 | Rooms | `Rooms` | `Rooms.cshtml` | ✅ |
| 7 | RoomFeatures | `RoomFeatures` | `RoomFeatures.cshtml` | ✅ |
| 8 | HotelInfo | `HotelInfo` | `HotelInfo.cshtml` | ✅ |
| 9 | Photos | `Photos` | `Photos.cshtml` | ✅ |
| 10 | Performance | `Performance` | `Performance.cshtml` | ✅ |
| 11 | Reviews | `Reviews` | `Reviews.cshtml` | ✅ |
| 12 | Finance | `Finance` | `Finance.cshtml` | ✅ |
| 13 | Settings | `Settings` | `Settings.cshtml` | ✅ |
| 14 | Preferences | `Preferences` | `Preferences.cshtml` | ✅ |
| 15 | Security | `Security` | `Security.cshtml` | ✅ |
| 16 | Support | `Support` | `Support.cshtml` | ✅ |
| 17 | Campaigns | `Campaigns` | `Campaigns.cshtml` | ✅ |
| 18 | Commissions | `Commissions` | `Commissions.cshtml` | ✅ (T309 KPI+tablo+POST ödendi) |
| 19 | Invoices | `Invoices` | `Invoices.cshtml` | ✅ |
| 20 | GuestInvoices | `GuestInvoices` | `GuestInvoices.cshtml` | ✅ |
| 21 | CompanyReservations | `CompanyReservations` | `CompanyReservations.cshtml` | ✅ |
| 22 | CompanyAnalytics | `CompanyAnalytics` | `CompanyAnalytics.cshtml` | ✅ |
| 23 | CompanyRequests | `CompanyRequests` | `CompanyRequests.cshtml` | ✅ |
| 24 | GuestMessages | `GuestMessages` | `GuestMessages.cshtml` | ✅ |
| 25 | ReservationCalendar | `ReservationCalendar` | `ReservationCalendar.cshtml` | ✅ |
| 26 | CancellationNoShow | `CancellationNoShow` | `CancellationNoShow.cshtml` | ✅ |
| 27 | PaymentStatuses | `PaymentStatuses` | `PaymentStatuses.cshtml` | ✅ |
| 28 | FacilityAmenities | `FacilityAmenities` | `FacilityAmenities.cshtml` | ✅ |
| 29 | FacilityPolicies | `FacilityPolicies` | `FacilityPolicies.cshtml` | ✅ |
| 30 | FacilityDefinitions | `FacilityDefinitions` | `FacilityDefinitions.cshtml` | ✅ |
| 31 | FacilityUsers | `FacilityUsers` | `FacilityUsers.cshtml` | ✅ |
| 32 | MealServices | `MealServices` | `MealServices.cshtml` | ✅ |
| 33 | SuperPrice | `SuperPrice` | `SuperPrice.cshtml` | ✅ |
| 34 | Discounts | `Discounts` | `Discounts.cshtml` | ✅ |
| 35 | Restrictions | `Restrictions` | `Restrictions.cshtml` | ✅ |
| 36 | DailyNotes | `DailyNotes` | `DailyNotes.cshtml` | ✅ |
| 37 | StockQuota | `StockQuota` | `StockQuota.cshtml` | ✅ |
| 38 | PaymentSettings | `PaymentSettings` | `PaymentSettings.cshtml` | ✅ |
| 39 | Reconciliation | `Reconciliation` | `Reconciliation.cshtml` | ✅ |
| 40 | ListingSubscriptions | `ListingSubscriptions` | `ListingSubscriptions.cshtml` | ✅ |
| 41 | LocationInsights | `LocationInsights` | `LocationInsights.cshtml` | ✅ |
| 42 | FavoriteGuests | `FavoriteGuests` | `FavoriteGuests.cshtml` | ✅ |
| 43 | MarketingEvents | `MarketingEvents` | `MarketingEvents.cshtml` | ✅ |
| 44 | AccountInfo | `AccountInfo` | `AccountInfo.cshtml` | ✅ |
| 45 | NotificationPreferences | `NotificationPreferences` | `NotificationPreferences.cshtml` | ✅ |
| 46 | PlannedModule | `PlannedModule` | `PlannedModule.cshtml` | ✅ |
| 47 | NoHotelAssigned | `NoHotelAssigned` | `NoHotelAssigned.cshtml` | ✅ |
| 48 | PlatformPackages | `PlatformPackages` | `PlatformPackages.cshtml` | ✅ |
| 49 | PlatformPackageDetail | `PlatformPackageDetail` | `PlatformPackageDetail.cshtml` | ✅ |

### FE-CTO — Partner Platform Paketleri (T321, kod hazır)

- **Tarih:** 2026-05-23
- **Route:** `/panel/partner/platform-paketleri`, `/panel/partner/platform-paketleri/detay/{id}`
- **CSS:** `platform-packages.css` + `platform-packages.mobile.css` (`_PartnerPanelLayout` `PageCssPath` ile otomatik)
- **Not:** Katalog kart grid + başvuru tablosu mobil kaydırma; tam SS seti T321 sprint. Oturum yok → `/partner-giris`.

### FE-CTO APPROVED — Partner Dashboard (giriş kapısı)

- **Tarih:** 2026-05-22
- **SS:** `docs/frontend-screenshots/partner/dashboard/{desktop|mobil}/step-01-full-page.png`
- **Not:** `/panel/partner` → `/partner-giris`; FacilityLocation SS ayrı sprint. `dashboard.mobile.css` layout `PageCssPath` ile yüklenir (T102).

---

## Firma panel (`fe-firma`) — route prefix `/panel/firma`

| # | Sayfa | Action | View | mobile.css |
|---|-------|--------|------|------------|
| 1 | Dashboard | `Index` | `Dashboard.cshtml` | ✅ |
| 2 | CreateReservation | `CreateReservation` | `CreateReservation.cshtml` | ✅ |
| 3 | Reservations | `Reservations` | `Reservations.cshtml` | kısmi |
| 4 | Deals | `Deals` | `Deals.cshtml` | kısmi |
| 5 | DealsCompare | `DealsCompare` | `DealsCompare.cshtml` | kısmi |
| 6 | Employees | `Employees` | `Employees.cshtml` | kısmi |
| 7 | Limits | `Limits` | `Limits.cshtml` | kısmi |
| 8 | Invoices | `Invoices` | `Invoices.cshtml` | kısmi |
| 9 | Spending | `Spending` | `Spending.cshtml` | kısmi |
| 10 | Hotels | `Hotels` | `Hotels.cshtml` | kısmi |
| 11 | Messages | `Messages` | `Messages.cshtml` | kısmi |
| 12 | Security | `Security` | `Security.cshtml` | kısmi |

### FE-CTO APPROVED — Firma Dashboard (giriş kapısı)

- **Tarih:** 2026-05-22
- **SS:** `docs/frontend-screenshots/firma/dashboard/{desktop|mobil}/step-01-full-page.png`
- **Not:** `/panel/firma` → `/firma-giris`; CreateReservation SS T005 sonraki oturum.

---

## Kullanıcı paneli (`fe-user`) — route prefix `/panel/user`

| # | Sayfa | Action / URL | View | mobile.css | SS | FE-CTO |
|---|-------|--------------|------|------------|-----|--------|
| 1 | Dashboard | `dashboard` | `Dashboard.cshtml` | ✅ | ⏳ T120 `fe-user/dashboard/` | ⏳ |
| 2 | Rezervasyonlarım | `rezervasyonlarim` | `Reservations.cshtml` | ✅ safe-area | ⏳ `docs/frontend-screenshots/fe-user/rezervasyonlarim/` | ⏳ |
| 3 | Favorilerim | `favorilerim` | `Favorites.cshtml` | ✅ | ⏳ T121 `fe-user/favorilerim/` | ⏳ |
| 4 | Profil | `profil-bilgilerim` | `Profile.cshtml` | ✅ safe-area | ⏳ T103 `fe-user/profil/` | ⏳ |
| 5 | Güvenlik | `guvenlik-ve-giris` | `Security.cshtml` | ✅ | ⏳ T122 `fe-user/guvenlik/` | ⏳ |
| 6 | Yorumlarım | `yorumlarim` | `Reviews.cshtml` | ✅ | ⏳ | ⏳ |
| 7 | Rezervasyon yorumu | `rezervasyon-yorumu` | `ReservationReview.cshtml` | ✅ | ⏳ | ⏳ |
| 8 | Bildirimler | `bildirimler` | `Notifications.cshtml` | ✅ | ⏳ | ⏳ |
| 9 | Faturalarım | `faturalarim` | `Invoices.cshtml` | ✅ `PageCssMobile` | ⏳ T230 `fe-user/faturalarim/` | ⏳ |
| 10 | Ödeme yöntemleri | `odeme-yontemleri` | `PaymentMethods.cshtml` | ✅ | ⏳ | ⏳ |
| 11 | Sadakat | `otelpuan-programi` | `Loyalty.cshtml` | ✅ | ⏳ | ⏳ |
| 12 | Mesajlar | `mesajlarim` | `Messages.cshtml` | ✅ | ⏳ | ⏳ |
| 13–17 | Layout partial | `_UserPanelLayout`, sidebar, mobil nav, footer, route hub | partial | ✅ shell + `PageCssMobile` | — | — |

**T312 (kısmi):** fe-user SS path atandı — dashboard, favoriler, güvenlik, profil, rezervasyonlar, faturalar (`docs/frontend-screenshots/fe-user/…`); PNG üretimi sprint.

**Orkestratör:** D1 (`fe-user`) — Frontend CTO

---

## Satış paneli (`fe-satis`) — route prefix `/panel/satis`

| # | Sayfa | Action / URL | View | mobile.css | SS | FE-CTO |
|---|-------|--------------|------|------------|-----|--------|
| 1 | Dashboard | `dashboard` | `Dashboard.cshtml` | ✅ | ⏳ T104 | ⏳ |
| 2 | Yeni rezervasyon | `yeni-rezervasyon` | `CreateReservation.cshtml` | ✅ | ⏳ T104 | ⏳ |
| 3 | Rezervasyon PDF | `rezervasyon-pdf/{id}` | `ReservationPdf.cshtml` | ✅ | ⏳ | ⏳ |
| 4 | Müşteriler | `musteri-yonetimi` | `Customers.cshtml` | ✅ | ⏳ | ⏳ |
| 5 | Güvenlik | `guvenlik` | `Security.cshtml` | ✅ | ⏳ | ⏳ |
| 6 | Raporlar | `raporlar` | `Reports.cshtml` | ✅ | ⏳ | ⏳ |
| 7 | Rezervasyonlarım | `rezervasyonlarim` | `Reservations.cshtml` | ✅ | ⏳ | ⏳ |
| 8 | Müsaitlik | `musaitlik-takvimi` | `Availability.cshtml` | ✅ | ⏳ | ⏳ |
| 9 | Otel rehberi | `otel-rehberi` | `Hotels.cshtml` | ✅ | ⏳ | ⏳ |
| 10–13 | Layout partial | `_SalesPanelLayout`, sidebar, mobil nav, footer | partial | ✅ shell (safe-area T104) | — | — |

### H5 sprint notu (2026-05-23)

- Satış `shell.mobile.css` + `viewport-fit=cover`; dashboard `PageCssMobile` layout’ta
- `dashboard.mobile.css`: KPI grid + liste kart mobil düzeni

**Orkestratör:** D2 (`fe-satis`) — Frontend CTO

---

## Departman paneli (`fe-departman`) — route prefix `/panel/departman`

| # | Sayfa | Action / URL | View | mobile.css | SS | FE-CTO |
|---|-------|--------------|------|------------|-----|--------|
| 1 | Dashboard | `dashboard` | `Dashboard.cshtml` | kısmi | ⏳ | ⏳ |
| 2–5 | Layout partial | `_DepartmentPanelLayout`, sidebar, mobil nav, footer | partial | kısmi | — | — |

**Orkestratör:** D3 (`fe-departman`) — Frontend CTO

---

## Screenshot protokolü

- Klasör: `docs/frontend-screenshots/{sayfa}/{desktop|mobil}/step-NN-*.png`
- Desktop: 1440×900 — Mobil: 390×844 (3x DPR)
- Detaylı adım listesi: `docs/frontend-screenshots/README.md`

---

*Frontend Orkestratör — Master CTO ofisi ile senkron.*
