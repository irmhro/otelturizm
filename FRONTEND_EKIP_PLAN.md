# Frontend Ekip Planı — Otelturizm

**Son güncelleme:** 2026-05-22  
**Envanter:** 216 `.cshtml` (Email hariç) | **Tamamlanan (CTO ship YES):** 0 | **Kritik yol (🔄):** 2

---

## Ekip rolleri (simüle)

| Rol | Görev | Bu oturum çıktısı |
|-----|--------|-------------------|
| **UI Scout** | 1440px desktop + 390px mobil PNG | Klasör + manifest; **0 PNG** (sunucu 500 → migration sonrası yeniden) |
| **CSS Engineer** | `*.css` / `*.mobile.css` | `otel-listeleme.mobile.css`, `otel-detay.mobile.css` güncellendi |
| **Razor Integrator** | `.cshtml`, lazy img, DB alanları | `OtelListeleme`, `OtelDetay` lazy load |
| **CTO Reviewer** | Batch onay | Aşağıdaki `## CTO Review` blokları |

---

## Screenshot protokolü

- **URL:** `http://localhost:5103` (`Properties/launchSettings.json`)
- **Yol:** `docs/frontend-screenshots/{sayfa}/{desktop|mobil}/step-NN-{bolum}.png`
- **Adımlar (≥20/sayfa):** [`docs/frontend-screenshots/README.md`](docs/frontend-screenshots/README.md)

```powershell
dotnet build "D:\otelturizm\otelturizm.csproj"
dotnet run --project "D:\otelturizm\otelturizm.csproj" --launch-profile http
```

---

## CSS kuralları

- Public: `ViewData["PageCss"]` + `PageCssMobile` → `wwwroot/assets/css/{ad}.css`
- Panel: `ViewData["PageCssPath"]` → `wwwroot/assets/css/paneller/...`
- Mobil breakpoint: **900px** (_Layout ile uyumlu)
- Dokunma **≥44px**, iOS **safe-area**, yatay scroll yok

---

## Faz 0 — Envanter (özet)

| path | route | CSS | mobile.css | DB-heavy | status |
|------|-------|-----|------------|----------|--------|
| `Views/Oteller/OtelListeleme.cshtml` | `/oteller` | otel-listeleme | ✅ | ✅ | 🔄 |
| `Views/Oteller/OtelDetay.cshtml` | `/oteller/{slug}` | otel-detay | ✅ | ✅ | 🔄 |
| `Views/Oteller/HaritaOteller.cshtml` | `/oteller/harita` | haritaoteller | ✅ | ✅ | ⏳ |
| `Views/Anasayfa/*` (5) | `/` | site-layout | ✅ | kısmi | ⏳ |
| `Views/Login/*` (8) | giriş | auth | ✅ | ulkeId | ⏳ |
| `Views/Register/*` (4) | kayıt | register | ✅ | ✅ | ⏳ |
| `Views/Paneller/*` (162) | panel/admin | paneller/* | çoğu | ✅ | ⏳ |
| `Views/Destek/*` (4) | yardım | — | kısmi | ⏳ | ⏳ |
| Diğer (Kurumsal, Kampanya, …) | çeşitli | parçalı | ⏳ | ⏳ |

**216 sayfa** — panel sayfaları `Controllers/**` içindeki `PageCssPath` ile eşlenir.

---

## Faz 1 — Kritik yol (öncelik)

### Otel listeleme

- Route: `GET /oteller` — `OtellerController.OtelListeleme`
- CSS: `otel-listeleme.css`, `otel-listeleme.mobile.css`, `otel-listeleme.inline-extract.css`
- DB kart: `City`, `District`, `Neighborhood`, `Latitude`/`Longitude`, fiyat, `Slug`
- **Bu oturum:** lazy kart görseli; 44px CTA/filtre/chip; DB `KAPSAM_DEGERI_NORMALIZE` migration + `HotelService` kolon düzeltmesi

### Otel detay

- Route: `GET /oteller/{slug}`
- CSS: `otel-detay.css`, `otel-detay.mobile.css`, `otel-detay-reservation.css`
- DB: `District`, `City`, geo JSON-LD, odalar, fiyat
- **Bu oturum:** galeri lazy; sticky CTA 48px; galeri `scroll-snap`

---

## CTO Review — OtelListeleme

- **Screenshot refs:** `docs/frontend-screenshots/otel-listeleme/` (0/20+)
- **Desktop parity:** 🔧
- **Mobile parity:** 🔧
- **DB field coverage:** Model ↔ view eşleşiyor; runtime için `20260522_otel_liste_abonelikleri_kapsam_normalize_sqlserver.sql` uygulandı
- **Blockers:** App eski DLL ile 500 verdi; rebuild + restart sonrası UI Scout
- **Approved to ship:** **NO**

---

## CTO Review — OtelDetay

- **Screenshot refs:** `docs/frontend-screenshots/otel-detay/` (0/20+)
- **Desktop parity:** 🔧
- **Mobile parity:** 🔧
- **DB field coverage:** ✅ City/District/geo/oda/fiyat
- **Blockers:** Screenshot + slug ile canlı doğrulama
- **Approved to ship:** **NO**

---

## Faz 2 — Auth & kullanıcı paneli ⏳

Login, register, profile, reservations, favorites — ülke→il cascade.

## Faz 3 — Partner & Admin ⏳

Tabler mobil sidebar; tablo→kart.

## Faz 4 — Kalan public ⏳

Anasayfa, statik, yardım, sözleşmeler.

## Faz 5 — Global polish ⏳

`_Layout`, header/footer mobil, minimal utilities.

---

## Değiştirilen dosyalar (2026-05-22)

| Dosya | Amaç |
|-------|------|
| `wwwroot/assets/css/otel-listeleme.mobile.css` | 44px touch, toolbar |
| `wwwroot/assets/css/otel-detay.mobile.css` | CTA, galeri swipe |
| `Views/Oteller/OtelListeleme.cshtml` | `loading="lazy"` |
| `Views/Oteller/OtelDetay.cshtml` | Galeri lazy |
| `Services/HotelService.cs` | `[KAPSAM_DEGERI_NORMALIZE]`, `x.Slug` fix |
| `Database/MigrationsSql/tablo/migrationlar/20260522_otel_liste_abonelikleri_kapsam_normalize_sqlserver.sql` | DB kolon |
| `appsettings.Development.json` | Duplicate key kaldırıldı |
| `docs/frontend-screenshots/README.md` | Screenshot manifest |

---

## Mobil düzeltmeler (özet)

- Liste: 44px İncele / filtre / chip; lazy kart görselleri; safe-area footer
- Detay: 48px sticky CTA; galeri scroll-snap; lazy thumb; footer nefes payı
- Config/DB: local `dotnet run` engelleri giderildi

---

## Build

- İlk build: başarılı
- `HotelService.cs` `x.[SLUG]` → `x.Slug` (CS1001)
- Commit/deploy: **yapılmadı**

---

## İlerleme

| Metrik | Değer |
|--------|--------|
| Sayfa ✅ / toplam | **0 / 216** |
| Kritik 🔄 | **2** |
| Screenshot PNG | **0** (`docs/frontend-screenshots/`) |

**Sıradaki:** `dotnet run` → README adımlarına göre 40+ PNG → CTO ✅ → ship YES
