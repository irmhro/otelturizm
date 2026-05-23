# Platform Çok Dilli UI Şablon Planı

**Orkestra:** `H13_i18n_ui` · **Wave:** `Wave-IX-i18n`  
**Kültürler (mevcut):** `tr-TR`, `en-US`, `en-GB`, `de-DE`, `fr-FR`, `es-ES`, `ru-RU`, `ar-SA` (`Program.cs`)

---

## Mevcut durum

| Alan | Durum |
|------|--------|
| `RequestLocalization` | ✅ ?lang=, cookie, Accept-Language |
| `.resx` / `IStringLocalizer` | ❌ yok — metinler CSHTML içinde sabit TR |
| Panel dilleri | ❌ paneller TR-only |
| Kamu layout | ✅ hreflang linkleri (`_Layout.cshtml`) |
| RTL | ✅ `ar-SA` dir=rtl layout’ta |

---

## Hedef mimari

```
Resources/
  SharedResources.resx          (tr default)
  SharedResources.en-US.resx
  SharedResources.de-DE.resx
  ...
Views/
  Shared/_UiStrings.cshtml      (fallback helper)
  Templates/I18n/               (opsiyonel partial şablonlar)
```

**Kullanım:** `@inject IStringLocalizer<SharedResources> L` → `@L["Nav.Hotels"]`

---

## Şablon katmanları (öncelik)

| Katman | Dosyalar | Task |
|--------|----------|------|
| **Kamu shell** | `_Layout`, `_AnasayfaHeader`, footer, arama | T440 |
| **Otel public** | liste, detay, harita filtre chip | T441 |
| **Auth** | login/register tüm paneller | T442 |
| **User panel** | 17 sayfa — nav + başlık | T443 |
| **Partner/Admin** | Faz 2 — shell nav only | T444 |

---

## UI şablon sözlüğü (örnek anahtarlar)

| Key | TR | EN | DE |
|-----|----|----|-----|
| Nav.Hotels | Oteller | Hotels | Hotels |
| Search.Placeholder | Nereye? | Where to? | Wohin? |
| Booking.CheckIn | Giriş | Check-in | Anreise |
| Filter.Breakfast | Kahvaltı dahil | Breakfast included | Frühstück inkl. |
| Btn.Search | Ara | Search | Suchen |

Min **120 anahtar** Faz 1 kamu + rezervasyon.

---

## Dil seçici UX

- Header: bayrak + dropdown (mevcut flag-icons)
- Cookie `Culture` 1 yıl
- Panel: kullanıcı profilinde `PreferredLanguage`

---

## Teknik

1. `otelturizm.csproj` — EmbeddedResource `.resx`
2. `Program.cs` — `AddLocalization(o => o.ResourcesPath = "Resources")`
3. `_ViewImports.cshtml` — `@inject IViewLocalizer<SharedResources> SharedLocalizer`

**Task T445:** scaffold + 30 kamu string wired

---

*5 dk döngü: ~20 key / tur.*
