# Uluslararası SEO Planı — Yabancı Ülke Aramaları

**Orkestra:** `H9_ork_seo` · **Wave:** `Wave-IX-i18n`

---

## Hedef pazarlar & arama niyeti

| Pazar | Dil | Örnek sorgu | Landing hedefi |
|-------|-----|-------------|----------------|
| Almanya | de | hotels istanbul | `/de/oteller/istanbul` |
| UK/US | en | istanbul hotels booking | `/en/hotels/istanbul` |
| Fransa | fr | hotel istanbul | `/fr/hotels/istanbul` |
| Rusya | ru | отели стамбул | `/ru/oteli/stambul` |
| Arap ülkeleri | ar | فنادق اسطنبول | `/ar/oteller/istanbul` |
| İspanya | es | hoteles estambul | `/es/hoteles/estambul` |
| Türkiye | tr | istanbul oteller | `/oteller/istanbul` (mevcut) |

---

## URL stratejisi (Faz 1)

**Seçenek uygulanacak:** Kültür prefix + mevcut slug

| TR (default) | EN | DE |
|--------------|----|----|
| `/oteller` | `/en/hotels` | `/de/hotels` |
| `/oteller/istanbul` | `/en/hotels/istanbul` | `/de/hotels/istanbul` |
| `/oteller/{slug}` | `/en/hotels/{slug}` | `/de/hotels/{slug}` |

`Program.cs` — route convention `LocalizedPublicRoutes` middleware veya duplicate attribute routes.

---

## SEO meta şablonları (ViewData)

| Sayfa | TR title | EN title |
|-------|----------|----------|
| Liste şehir | `{City} Otelleri \| Otelturizm` | `{City} Hotels \| Otelturizm` |
| Otel detay | `{Hotel} — {City}` | `{Hotel} — {City} — Book now` |
| Harita | `Haritada {City} Otelleri` | `{City} hotels on map` |

**Sınıf:** `Services/InternationalSeoService.cs` — `BuildListingMeta(culture, city, hotelCount)`

---

## hreflang & canonical

- Her sayfa: `link rel=alternate` 8 dil + `x-default`
- Canonical = kültürsüz TR veya kullanıcının dil versiyonu (tek duplicate önleme)
- `sitemap.xml` — dil segmentleri (`/sitemap-en.xml`, …)

**Mevcut gap:** `BuildLangUrl` sadece `?lang=` — **path-based hreflang** gerek (T446)

---

## Schema.org çok dilli

- `Hotel` JSON-LD: `name`, `description` dil alanı (`OTEL_CEEVIRILERI` tablosu Faz 2)
- Faz 1: `inLanguage` + TR description

---

## İçerik / seed

- 39 İstanbul ilçe — EN meta description seed (`Docs/seo/en-istanbul-districts.json` → migration)
- Ülke sayfaları: `/en/destinations/turkey/istanbul`

---

## Görevler

| ID | Görev | Faz 1 |
|----|--------|-------|
| T446 | `InternationalSeoService` + OtellerController meta | ✅ |
| T447 | Localized route map en/de/fr/es/ru/ar | ✅ en/de route; path map 7 dil |
| T448 | Sitemap dil segmentleri | ✅ stub `wwwroot/sitemap-en.xml` + `SitemapService` path hreflang |
| T449 | hreflang path-based `_Layout` | ✅ |
| T450 | Seed EN meta 39 ilçe | ✅ `Docs/seo/en-istanbul-districts-meta.json` (+ opsiyonel SQL) |
| T451 | `noindex` kuralları filtre/query (mevcut HotelListingSeo genişlet) | ✅ culture-aware canonical |

### Faz 1 uygulama notları (2026-05-23, H9)

- `Services/InternationalSeoService.cs`, `Services/InternationalSeoPaths.cs` — listing/detail meta, hreflang alternates.
- `OtellerController`: `[Route("en/hotels")]`, `[Route("de/hotels")]`; TR `/oteller` değişmedi.
- `Infrastructure/RoutePrefixRequestCultureProvider` — path prefix → UI kültürü.
- `_Layout.cshtml` — `BuildLangUrl` path tabanlı; `ViewData["HreflangAlternates"]` öncelikli.
- `HotelListingSeo` — kültür prefix canonical.
- Build doğrulama: `dotnet build -o .build-h9-seo`.

---

## KPI

- Google Search Console — uluslararası impression (manuel)
- Lighthouse SEO ≥ 90 kamu sayfaları

---

*Booking/Expedia referans: path-prefix + x-default TR.*
