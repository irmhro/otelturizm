# Orkestra — Panel SS batch (H2 Partner · H4 User · H5 Satış · H6 Firma)

Son güncelleme: **2026-05-23** (Wave-XI scope B — H4 user + H2 partner FE-CTO)  
Kaynak plan: `FRONTEND_ORKESTRATOR_PLAN.md` (`fe-partner`, `fe-firma`, `fe-user`, `fe-satis`) · görev: **T311** (H2), **T312** (H4)  
Build doğrulama: `dotnet build otelturizm.csproj -o .build-panels-wave --no-restore`

### Wave-XI FE-CTO özet

| Stream | Batch | Sayfa | SS klasör kökü |
|--------|-------|-------|----------------|
| **H4** user | FE-CTO batch | 4 | `docs/frontend-screenshots/fe-user/{dashboard,favorilerim,rezervasyonlarim,faturalarim}/` |
| **H2** partner | T311 batch-1 | 10 | `docs/frontend-screenshots/partner/{sayfa-slug}/` (tablo § Partner) |

*Onay sayacı:* `6/151` (mevcut APPROVED kapılar) — bu batch **hedef path** ataması; PNG üretimi oturum + demo veri ile sprint.

## Mobil CSS doğrulama (kod — 2026-05-23)

| Panel | Sayfa | mobile.css | Not |
|-------|-------|------------|-----|
| Partner | Commissions | `finance.mobile.css` | KPI grid, filtre 1fr, tablo kaydırma, **44px** dokunma hedefi |
| Partner | PlatformPackages | `platform-packages.mobile.css` | Filtre/kart **44px**, başvuru tablosu yatay kaydırma |
| Firma / User / Satış | Shell | `shell.mobile.css` | `viewport-fit=cover`, alt nav safe-area |

---

## Partner — T311 batch-1 (10 sayfa)

Öncelik: giriş kapısı sonrası yüksek kullanım. PNG → **pending** (oturum + `otelId`).

| # | Sayfa | Route | View | CSS (desktop) | mobile.css | SS |
|---|-------|-------|------|---------------|------------|-----|
| 1 | Dashboard | `/panel/partner` · `/panel/partner/dashboard` | `Views/Paneller/Partner/Dashboard.cshtml` | `paneller/partner/dashboard.css` | ✅ | done (giriş kapısı 2026-05-22) |
| 2 | Tesis konumu | `/panel/partner/tesis/konum` | `FacilityLocation.cshtml` | `hotel-info.css` | ✅ `hotel-info.mobile.css` | pending |
| 3 | Rezervasyonlar | `/panel/partner/rezervasyonlar` | `Reservations.cshtml` | `reservations.css` | ✅ (toolbar + 44px, 2026-05-23) | pending |
| 4 | Takvim fiyatlar | `/panel/partner/takvim-fiyatlar` | `Pricing.cshtml` | `pricing.css` | ✅ (hero/toolbar mobil, 2026-05-23) | pending |
| 5 | Firma fiyatları | `/panel/partner/firma-fiyatlari` | `CompanyPricing.cshtml` | `company-pricing.css` | ✅ | pending |
| 6 | Oda yönetimi | `/panel/partner/oda-yonetimi` | `Rooms.cshtml` | `rooms.css` | ✅ | pending |
| 7 | Oda özellikleri | `/panel/partner/oda/ozellikler` | `RoomFeatures.cshtml` | `room-features.css` | ✅ | pending |
| 8 | Otel bilgileri | `/panel/partner/otel-bilgileri` | `HotelInfo.cshtml` | `hotel-info.css` | ✅ | pending |
| 9 | Fotoğraflar | `/panel/partner/fotograflar` | `Photos.cshtml` | `photos.css` | ✅ | pending |
| 10 | Performans | `/panel/partner/performans` | `Performance.cshtml` | `performance.css` | ✅ (hero grid + 44px, 2026-05-23) | pending |

**Ek (kod hazır):** Platform Paketleri — `/panel/partner/platform-paketleri` · `PlatformPackages.cshtml` · `platform-packages.css` / `.mobile.css` · SS: `docs/frontend-screenshots/partner/platform-paketleri/`.

Partner giriş: `/partner-giris`. Demo otel: `20260523_seed_10_istanbul_demo_oteller.sql`.

---

## Firma panel — batch (5 sayfa)

| # | Sayfa | Route | View | CSS | mobile.css | SS |
|---|-------|-------|------|-----|------------|-----|
| 1 | Dashboard | `/panel/firma` · `/panel/firma/dashboard` | `Dashboard.cshtml` | `firma/dashboard.css` | ✅ | pending |
| 2 | Yeni rezervasyon | `/panel/firma/yeni-rezervasyon` | `CreateReservation.cshtml` | `create-reservation.css` | ✅ (`PageCssMobile` view) | pending |
| 3 | Rezervasyonlar | `/panel/firma/rezervasyonlar` | `Reservations.cshtml` | `reservations.css` | ✅ (tablo kaydırma + 44px, 2026-05-23) | pending |
| 4 | Firma fiyatları | `/panel/firma/firma-fiyatlari` | `Deals.cshtml` | `deals.css` | ✅ (filtre/kart + 44px, 2026-05-23) | pending |
| 5 | Güvenlik | `/panel/firma/guvenlik` | `Security.cshtml` | `security.css` | ✅ | pending |

Giriş: `/firma-giris` veya kurumsal oturum.

---

## Kullanıcı paneli — H4 FE-CTO batch (4 sayfa, Wave-XI)

Öncelik: T312 · route prefix `/panel/user` · giriş: `/kullanici-giris` (veya `Auth` user login).

| # | Sayfa | Route | View | CSS (desktop) | mobile.css | SS hedef |
|---|-------|-------|------|---------------|------------|----------|
| 1 | Dashboard | `/panel/user` · `/panel/user/dashboard` | `Views/Paneller/User/Dashboard.cshtml` | `paneller/user/dashboard.css` | ✅ `dashboard.mobile.css` (hero CTA 44px) | `fe-user/dashboard/{desktop\|mobil}/step-01-*.png` |
| 2 | Favorilerim | `/panel/user/favorilerim` | `Favorites.cshtml` | `favorites.css` | ✅ `favorites.mobile.css` (kart + 44px) | `fe-user/favorilerim/` |
| 3 | Rezervasyonlarım | `/panel/user/rezervasyonlarim` | `Reservations.cshtml` | `reservations.css` | ✅ `reservations.mobile.css` (safe-area) | `fe-user/rezervasyonlarim/` |
| 4 | Faturalarım | `/panel/user/faturalarim` | `Invoices.cshtml` | `invoices.css` | ✅ `invoices.mobile.css` (tablo→kart listesi, 44px) | `fe-user/faturalarim/` (T230) |

**Faz-2 (SS kuyruk, bu batch dışı):** `profil-bilgilerim`, `guvenlik-ve-giris`, `yorumlarim`, `bildirimler`, `odeme-yontemleri` — `FRONTEND_ORKESTRATOR_PLAN.md` fe-user tablosu.

---

## Satış paneli — batch (5 sayfa)

| # | Sayfa | Route | View | CSS | mobile.css | SS |
|---|-------|-------|------|-----|------------|-----|
| 1 | Dashboard | `/panel/satis/dashboard` | `Dashboard.cshtml` | `satis/dashboard.css` | ✅ (`PageCssMobile` layout) | pending (T104) |
| 2 | Yeni rezervasyon | `/panel/satis/yeni-rezervasyon` | `CreateReservation.cshtml` | `create-reservation.css` | ✅ (form grid + 44px, 2026-05-23) | pending |
| 3 | Rezervasyonlarım | `/panel/satis/rezervasyonlarim` | `Reservations.cshtml` | `reservations.css` | ✅ (filtre + tablo, 2026-05-23) | pending |
| 4 | Müşteriler | `/panel/satis/musteri-yonetimi` | `Customers.cshtml` | `customers.css` | ✅ (kart grid + 44px, 2026-05-23) | pending |
| 5 | Güvenlik | `/panel/satis/guvenlik` | `Security.cshtml` | `panel-user-security.css` | ✅ (`PageCssMobile` controller, 2026-05-23) | pending |

Shell: `shell.mobile.css` + `viewport-fit=cover` (H5 sprint notu).

---

## Görüntü protokolü

- Klasör: `docs/frontend-screenshots/{panel}/{sayfa-slug}/{desktop|mobil}/step-NN-*.png`
- Desktop 1440×900 · Mobil 390×844 (3× DPR)
- Detay: `docs/frontend-screenshots/README.md`

---

## Kalan mobil gap özeti (dosya düzeyi)

| Panel | Toplam sayfa CSS | Sadece `@import` stub mobile | Kalan gap |
|-------|------------------|------------------------------|-----------|
| Partner | 31 | 13 | **13** |
| Firma | 11 | 6 | **6** |
| User | 12 | 0 | **0** |
| Satış | 8 | 3 | **3** |

*Stub:* `.mobile.css` yalnızca `@import url("./…");` — sayfa kuralları ana `.css` içindeki `@media` ile geliyor; tam mobil katman sprint’te genişletilecek.

---

## Blokerler

| Bloker | Etkilenen |
|--------|-----------|
| Partner/Firma oturum + `otelId` | Partner T311, Firma SS |
| PNG repo’da yok | Tüm `pending` SS satırları |

CTO kuyruk: `CTO_AJAN_ATAMA_KUYRUGU.md` → **T311** (Partner 10), **T312** (H4 user 4 path), **T332** (Firma kalan). Durum özeti: `ORKESTRA_DURUM_KONTROL.md` § FE-CTO.
