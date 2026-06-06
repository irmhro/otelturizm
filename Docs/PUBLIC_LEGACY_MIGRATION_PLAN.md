# Public Site — Legacy Tasarım Migrasyon Planı

**Tarih:** 2026-05-25  
**Referans şablon:** Anasayfa (`Views/Anasayfa/Anasayfa.cshtml`, `anasayfa_masaustu.css`, Plus Jakarta Sans, `--flag-crimson`, `--bg-warm`, `--radius-premium`)

## Durum özeti

| Katman | Layout | Durum |
|--------|--------|-------|
| Anasayfa | Standalone premium | ✅ Tamam |
| Otel listeleme, kampanya, kurumsal, firma, sözleşmeler | `_PublicPremiumPageLayout` | ✅ Tamam |
| Auth (giriş/kayıt) | `_AuthPremiumLayout` | ✅ Tamam |
| Otel detay, harita, destek, seyahat, gizlilik | `_PublicPremiumPageLayout` (2026-05-25) | ✅ Taşındı |
| Hata / status sayfaları | `_Layout` (legacy) | 🔄 Backlog |
| Gelişim takip (`/gelisim`) | `_Layout` (legacy) | 🔒 Bilinçli (gate) |
| Paneller | Panel shell | ✅ Ayrı sistem |

## Canlı denetim (2026-05-25 öncesi — legacy-mix)

| Rota | Eski layout işaretleri |
|------|------------------------|
| `/oteller/{slug}` | `site-layout.css`, bootstrap, yanbar |
| `/oteller/harita` | aynı |
| `/yardim-merkezi`, `/sss` | aynı |
| `/seyahat-planlama` | aynı |
| `/Home/Privacy` | aynı |

## Bu sprintte yapılanlar

1. **`_PublicPremiumPageLayout`** — SEO meta (canonical, OG, JSON-LD), opsiyonel Bootstrap/jQuery, SlaytGorsel, fe-world-tokens.
2. **`Views/Oteller/_ViewStart.cshtml`** — otel detay + harita premium layout.
3. **`Views/Destek/_ViewStart.cshtml`** — yardım + SSS premium + anasayfa token CSS.
4. **`Views/SeyahatPlanlama/_ViewStart.cshtml`** — seyahat planlama premium.
5. **`Home/Privacy.cshtml`** — premium layout.
6. **CSS tipografi** — `haritaoteller`, `yardim-merkezi`, `sss`, `seyahat-planlama`, `home-privacy` → Plus Jakarta Sans.

## Backlog (Faz 2)

| ID | Sayfa | İş |
|----|-------|-----|
| L1 | `Shared/Error.cshtml`, `StatusCode.cshtml` | Premium layout + minimal hata CSS |
| L2 | `Gelisim/Index.cshtml` | Premium shell (gate korunarak) |
| L3 | Orphan CSS | `otel-listeleme.css`, `otel-detay.css` sil veya arşivle |
| L4 | Orphan partials | `_AnasayfaHeader`, `_KurumsalHeader` vb. temizlik |
| L5 | Panel PDF | `ReservationPdf.cshtml` AdminLTE pdfmake bağımlılığı |
| L6 | i18n rotalar | `/en/hotels`, `/de/hotels` premium doğrulama |
| L7 | Kullanıcı paneli public rotalar | Rezervasyon akışı uçtan uca mobil/masaüstü |

## Test checklist

- [ ] `/` ana sayfa — header, footer, vitrin kartları
- [ ] `/oteller` — filtre, kart, mobil drawer
- [ ] `/oteller/maidan-istanbul-boutique` — galeri, rezervasyon, harita sekmesi
- [ ] `/oteller/harita` — leaflet pin, liste senkron
- [ ] `/yardim-merkezi`, `/sss` — arama, kategori
- [ ] `/seyahat-planlama` — rota/kampanya blokları
- [ ] `/Home/Privacy` — TOC, tipografi
- [ ] `/kullanici-giris`, `/partner-giris`, `/firma-giris` — auth premium

## Font / spacing referansı (anasayfa)

```css
--flag-crimson: #E30A17
--anatolian-sun: #FF9E00
--luxury-dark: #1A1919
--bg-warm: #FCFBF9
--border-subtle: #EBE9E4
--radius-premium: 16px
font-family: 'Plus Jakarta Sans', sans-serif;
```

Mobil kırılım: **900px** (`*_mobil.css` / `*.mobile.css`).
