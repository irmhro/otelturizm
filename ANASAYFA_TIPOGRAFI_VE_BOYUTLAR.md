# Anasayfa tipografi ve bileşen boyutları

> **Ana referans:** [fontlar.md](fontlar.md) — tüm anasayfa + panel + kamu font sözleşmesi.

Kaynak CSS:
- `wwwroot/assets/css/anasayfa-header.css` — logo, header
- `wwwroot/assets/css/anasayfa_masaustu.css` — içerik token’ları
- `wwwroot/assets/css/anasayfa_mobil.css` — mobil override

Markup: `Views/Anasayfa/_AnasayfaLogo.cshtml`

---

## Font ailesi

| Alan | Değer |
|------|-------|
| Anasayfa gövde | Plus Jakarta Sans |
| Footer | Inter, Segoe UI, sans-serif |
| Google Fonts ağırlıkları | 400, 500, 600, 700, 800 |

---

## Logo (wordmark only — ikon yok)

| Token / öğe | Masaüstü | Mobil |
|-------------|----------|-------|
| `--home-logo-size` (OTEL) | 17px (≥901px) / 18px (taban) | 16px |
| `--home-logo-sub-size` (TURİZM) | 16px / 17px | 15px |
| `.logo-otel` letter-spacing | 0.22em | 0.18em |
| `.logo-turizm` letter-spacing | 0.15em | 0.12em |
| `.logo-sep` | 1px dikey gradient | 1px |
| Font weight | 800 | 800 |

**Yapı:** `OTEL` koyu + ince ayraç + `TURİZM` gradient. İkon/mark kullanılmaz.

**Selector:** `.logo-area`, `.logo-wordmark`, `.logo-otel`, `.logo-sep`, `.logo-turizm`

**Footer koyu tema:** `.footer .logo-otel` beyaz, `.footer .logo-sep` açık gradient, `.footer .logo-turizm` pembe→turuncu gradient

---

## İçerik token’ları — masaüstü

| Token | Boyut |
|-------|-------|
| `--home-type-promo-title` | 15px |
| `--home-type-section` | 22px |
| `--home-type-section-link` | 13px |
| `--home-type-card-title` | 14.5px |
| `--home-type-card-location` | 12.5px |
| `--home-type-card-meta` | 12px |
| `--home-type-card-action` | 13px |
| `--home-type-field-sub` | 12px |
| `--home-type-input-value` | 14px |
| `--home-type-btn` | 13px |

---

## İçerik token’ları — mobil (≤900px)

| Token | Boyut |
|-------|-------|
| `--home-type-promo-title` | 13px |
| `--home-type-section` | 20px |
| `--home-type-card-title` | 14px |
| `--home-type-card-location` | 12px |
| `--home-type-field-sub` | 11px |

---

## Header metinleri

| Rol | Masaüstü |
|-----|----------|
| Utility bar | 11px |
| TÜRSAB rozeti | 9.5px |
| Menü linkleri | 14px |
| Dil / para seçici | 12px |
| Otelini Kaydet | 12.5px |
| Giriş Yap | 12.5px |

| Rol | Mobil |
|-----|-------|
| Giriş Yap (header) | 12px |
| Menü ikon | 17px |
| Drawer logo | 15px |

---

## Hero

| Rol | Masaüstü | Mobil |
|-----|----------|-------|
| Kicker | 11px | 10px |
| Ana başlık | clamp(38px, 3.15vw, 54px) | clamp(28px, 7.4vw, 36px) |
| ≤520px başlık | — | clamp(26px, 6.8vw, 32px) |

---

## Buton sözleşmesi

| Özellik | Değer |
|---------|-------|
| Metin | 13px (`--home-type-btn`) |
| Min yükseklik | 40px |
| Padding | 10px 16px |
| Radius | 8px |

---

## Kart boyutları

| Öğe | Masaüstü | Mobil ≤900 | Mobil ≤520 |
|-----|----------|------------|------------|
| Görsel yüksekliği | 210px | 180px | 168px |
| Otel grid gap | 24px | — | — |
| Premium radius | 16px | — | — |

---

## Rol → selector eşlemesi

| Rol | Selector |
|-----|----------|
| Form / promo başlık | `.search-pro-field label`, `.mystery-details h4` |
| Form alt etiket | `.search-pro-mini .mini-label` |
| Input değer | `.search-engine-pro .search-pro-input input/select` |
| Bölüm başlığı | `.section-headline`, `h2.section-headline` |
| Bölüm yan link | `.headline-link` |
| Kart adı | `.hotel-title` |
| Konum | `.hotel-desc` |
| Fiyat notu | `.price-tax-note` |
| Kart aksiyon | `.card-link` |
| Sürpriz kutu butonları | `.mystery-countdown`, `.btn-open-box` |
| Arama gönder | `.btn-search-pro` |

---

## Değişiklik kuralı

1. Boyut değişikliği önce bu dosyada ve ilgili CSS token’ında yapılır.
2. Logo yalnızca `_AnasayfaLogo.cshtml` + `anasayfa-header.css` üzerinden güncellenir.
3. Mobil farkları `anasayfa_mobil.css` ve `anasayfa-header.mobile.css` ile verilir.
