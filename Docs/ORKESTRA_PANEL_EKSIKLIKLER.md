# Orkestra Panel Eksiklikleri

**Kaynak:** `FRONTEND_ORKESTRATOR_PLAN.md` (ilk 200 satır + panel tabloları) · dar kod taraması 2026-05-23  
**Amaç:** Panel bazında eksik sayfa, `mobile.css`, screenshot (SS), API

---

## Admin (`/admin`) — H3

| Kategori | Durum | Detay |
|----------|--------|--------|
| **Sayfa** | 55 envanter | Eksik **view**: `SlowSql`, `SecurityEvents`, `UploadHistory` (controller action var). Roadmap: `RevenueCommandCenter`, `FraudAlerts`, `DataExportCenter`, `ChannelManagerHub`, `ApiKeys` (T350–T362) |
| **mobile.css** | kısmi | Dedicated dosya ~14; kalan sayfalar layout `admin-tabler-overrides.mobile` — table-cards sadece 5 sayfada (T111–T115) |
| **SS** | ❌ düşük | Auth test kullanıcı yok → panel içi SS blokaj (T101/T310) |
| **API** | kısmi | CSV export bazı action’larda; unified export/API keys/webhooks **yok** |

**Öncelik:** T349 build fix → T353–T355 view’lar → T350 Revenue Center

---

## Partner (`/panel/partner`) — H2

| Kategori | Durum | Detay |
|----------|--------|--------|
| **Sayfa** | 49 CSHTML | Tam set mevcut; `PlatformPackages` + detay eklendi (T321) |
| **mobile.css** | ✅ envanter | Plan: tüm sayfalarda ✅ — kalite SS ile doğrulanacak |
| **SS** | ⏳ | Dashboard APPROVED; 47 sayfa SS döngüsü (T311) pending |
| **API** | kısmi | Komisyon ödendi POST (T309); channel sync **yok** |

**Öncelik:** T311 SS batch · Commissions KPI kanıtı koru

---

## Firma (`/panel/firma`) — H6

| Kategori | Durum | Detay |
|----------|--------|--------|
| **Sayfa** | 12 | Tam |
| **mobile.css** | kısmi | Dashboard/CreateReservation ✅; 9 sayfa `kısmi` (plan satır 211–220) |
| **SS** | ⏳ | Dashboard giriş SS var; CreateReservation E2E kanıt (T220) |
| **API** | kısmi | Rezervasyon oluşturma E2E; deals compare API sınırlı |

**Öncelik:** Reservations + Deals mobile.css + SS

---

## Kullanıcı (`/panel/user`) — H4

| Kategori | Durum | Detay |
|----------|--------|--------|
| **Sayfa** | 17 | Tam (layout partial dahil) |
| **mobile.css** | ✅ shell | Profil/rezervasyon safe-area (T103); faturalar PageCssMobile (T230) |
| **SS** | ⏳ | 6 path atandı (T312); PNG üretimi bekliyor |
| **API** | kısmi | Favoriler local; **cross-device sync yok** (T342) |

**Öncelik:** T312 PNG · T342 wishlist sync

---

## Satış (`/panel/satis`) — H5

| Kategori | Durum | Detay |
|----------|--------|--------|
| **Sayfa** | 13 | Tam |
| **mobile.css** | ✅ | Shell safe-area + dashboard (T104/T130–T132) |
| **SS** | ⏳ | T104 path; PNG pending |
| **API** | kısmi | Rezervasyon/müşteri CRUD; rapor export sınırlı |

**Öncelik:** Dashboard + CreateReservation SS

---

## Departman (`/panel/departman`) — H6/D3

| Kategori | Durum | Detay |
|----------|--------|--------|
| **Sayfa** | 5 (1 sayfa + layout) | En küçük panel |
| **mobile.css** | kısmi | Dashboard + layout `kısmi` |
| **SS** | ❌ | Atanmadı |
| **API** | yok | Departman-specific backend minimal |

**Öncelik:** T120 benzeri — dashboard mobile tam + SS

---

## Özet matris

| Panel | Sayfa eksik | mobile.css eksik | SS eksik | API eksik |
|-------|-------------|------------------|----------|-----------|
| Admin | 5+ roadmap view | ~40 sayfa dedicated yok | 54/55 | export/keys/webhooks |
| Partner | 0 | 0 (kalite) | ~47 | channel CM |
| Firma | 0 | ~9 kısmi | ~11 | deals API |
| User | 0 | 0 (kalite) | ~16 | wishlist sync |
| Satış | 0 | 0 (kalite) | ~12 | rapor export |
| Departman | 0 | layout kısmi | 5 | çoğu |

---

*Güncelleme: her Wave VERIFY sonrası `ORKESTRA_DURUM_KONTROL.md` ile senkron.*
