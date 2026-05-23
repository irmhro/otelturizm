# FE Dünya Standardı Orkestrası

**Mission:** 151 sayfa → world-class CSHTML + eşleşen `.css` + `.mobile.css` (Booking / Airbnb / Expedia / Otelz / Tatilbudur / Etstur seviyesi).

**Sprint:** `sprint-1ay-orkestra-20260523` · **Stream:** `H15_fe_world_standard`  
**Lead CTO:** `fe-world-ork` (Master FE World Standard)

---

## Orkestra yapısı

| ID | Sub-lead | Alan | Sayfa env. |
|----|----------|------|------------|
| H15 | **fe-world-ork** | Master koordinasyon, token’lar, kalite kapısı | 151 |
| H1 | Frontend-Ork-Kamu | Kamu otel, kampanya, konsept landing | 12 |
| H2 | Partner-FE-Ork | Partner panel | 47 |
| H3 | Admin-FE-Ork | Admin panel | 38 |
| H4 | User-FE-Ork | Kullanıcı paneli | 17 |
| H5 | Satis-FE-Ork | Satış paneli | 13 |
| H6 | Firma-FE-Ork | Firma + departman | 22 |

**Paralel akış:** `H15_fe_world_standard` — H1–H14 ile paralel; kuyruk: `CTO_AJAN_ATAMA_KUYRUGU.md`.

---

## i18n otomasyon

1. **SharedLocalizer kayıt defteri:** `Docs/I18N_KEY_REGISTRY.md` — her yeni dalga sonrası güncellenir.
2. **Ham anahtar yasağı:** Görünür metin `@SharedLocalizer["Key.Name"]` veya `T.Get("db_key")`; CSHTML’de Türkçe sabit metin bırakılmaz (Wave-XVIII+: header/footer/kamu P0).
3. **Path-locked culture (#053):** `InternationalSeoPaths` + `RoutePrefixRequestCultureProvider` — `/fr/hotels`, `/de/hotels` vb. prefix kültürü kilitler; Google Translate yalnızca prefix’siz TR rotalarında.
4. **Otomatik anahtar çıkarma checklist:**
   - [ ] Yeni CSHTML metni tarandı (`rg` / Wave diff)
   - [ ] 7 `.resx` dosyasına eklendi (`tr`, `en-US`, `de-DE`, `fr`, `es`, `ar`, `ru`)
   - [ ] `I18N_KEY_REGISTRY.md` Wave satırı
   - [ ] Build 0 hata
   - [ ] Path-locked route’larda kültür smoke

---

## Design tokens (`wwwroot/assets/css/fe-world-tokens.css`)

| Token | Değer | Kaynak |
|-------|-------|--------|
| `--font-main` | Inter | `proje verileri` şablonları |
| `--primary` | `#003B95` | Otel listeleme / kampanya HTML |
| `--touch-min` | `44px` | Mobil CTA / FAB |
| `--safe-area-bottom` | `env(safe-area-inset-bottom)` | iOS notch |
| `--transition-base` | `0.25s cubic-bezier(0.4, 0, 0.2, 1)` | Şablon `:root` |

Public layout: `ViewData["IncludeFeWorldTokens"] = true` veya varsayılan kamu `PageCss` sayfalarında `_Layout.cshtml` import.

---

## 30 günlük dalga planı

| Hafta | Odak | Sayfa hedefi |
|-------|------|--------------|
| **W1** | Kamu P0: liste, detay, kampanya, header/footer | 12 |
| **W2** | Paneller batch-1: partner + user | 40 |
| **W3** | Paneller batch-2: admin + satış + firma | 40 |
| **W4** | Polish, SS, Lighthouse, i18n tam kablolama | 59 + kapılar |

**Wave-XVIII (W1 kickoff):** `OtelListeleme`, `OtelDetay`, `Kampanyalar/Index`, `_AnasayfaHeader`, `_AnasayfaFooter`, `fe-world-tokens.css`.

---

## Sayfa kalite kapısı (FE-CTO onay)

Her sayfa merge öncesi:

- [ ] Desktop CSS (`PageCss`) — token uyumu
- [ ] Mobile CSS (`PageCssMobile`) — 44px CTA, safe-area
- [ ] i18n wired — SharedLocalizer, registry güncel
- [ ] Empty state + erişilebilirlik (`aria-*`, semantic HTML)
- [ ] Loading skeleton (opsiyonel, liste/kart sayfaları)
- [ ] SS path: `docs/frontend-screenshots/{panel}/{page}.png`

---

## Referans şablonlar

| Sayfa | Şablon |
|-------|--------|
| Otel listeleme | `proje verileri/04-otel-arama-ve-detay/OTEL LİSTELEME SAYFASI.html` |
| Kampanyalar | `proje verileri/kodlaması tamamlanmış sayfalar/Kampanyalı Oteller.html` |
| H1 eşleme | `Docs/H1_SABLON_REFERANS.md` |

---

*Güncelleme: 2026-05-24 · Wave-XVIII*
