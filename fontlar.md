# fontlar.md — Otelturizm tipografi sözleşmesi

Tüm yeni sayfa ve panel geliştirmelerinde bu dosyadaki boyutlar referans alınır. Tekil `font-size` dağıtmayın; ilgili CSS token / sınıfını kullanın.

**Kaynak dosyalar**

| Alan | Masaüstü CSS | Mobil CSS |
|------|--------------|-----------|
| Anasayfa içerik | `wwwroot/assets/css/anasayfa_masaustu.css` | `wwwroot/assets/css/anasayfa_mobil.css` |
| Anasayfa header / logo | `wwwroot/assets/css/anasayfa-header.css` | `wwwroot/assets/css/anasayfa-header.mobile.css` |
| Footer | `wwwroot/assets/css/footer_masaustu.css` | `wwwroot/assets/css/footer_mobil.css` |
| Admin panel shell | `wwwroot/assets/css/panel-admin-shell.css` | `wwwroot/assets/css/panel-admin-shell.mobile.css` |
| Kullanıcı panel shell | `wwwroot/assets/css/panel-user-shell.css` | `wwwroot/assets/css/panel-user-shell.mobile.css` |
| Ortak panel form UX | `wwwroot/assets/css/paneller/panel-form-ux.css` | — |
| Panel kanon | `Dokumanlar/Tasarim/panel-tasarim-sistemi.md` | aynı |
| Kamu / otel listeleme | `wwwroot/assets/css/otel-listeleme.css` | `wwwroot/assets/css/otel-listeleme.mobile.css` |

**Kural:** Mobil override yalnızca `*.mobile.css` dosyalarına yazılır.

---

## 1) Font aileleri

| Alan | Font |
|------|------|
| Anasayfa | Plus Jakarta Sans |
| Anasayfa footer | Inter, Segoe UI, sans-serif |
| Paneller (kanon) | Satoshi, sans-serif |
| Admin legacy shell | Inter, sans-serif |
| Google yük (anasayfa) | Plus Jakarta Sans 400–800 |

---

## 2) Anasayfa — içerik token’ları

### Masaüstü (≥901px)

| Rol | Boyut | CSS token |
|-----|-------|-----------|
| Form / promo başlık | 15px | `--home-type-promo-title` |
| Form alt etiket | 12px | `--home-type-field-sub` |
| Input / select değer | 14px | `--home-type-input-value` |
| Bölüm başlığı | 22px | `--home-type-section` |
| Bölüm yan link | 13px | `--home-type-section-link` |
| Kart adı | 14.5px | `--home-type-card-title` |
| Konum | 12.5px | `--home-type-card-location` |
| Fiyat alt notu | 12px | `--home-type-card-meta` |
| Kart aksiyon linki | 13px | `--home-type-card-action` |
| Buton metni | 13px | `--home-type-btn` |

### Mobil (≤900px)

| Rol | Boyut |
|-----|-------|
| Form / promo başlık | 13px |
| Bölüm başlığı | 20px |
| Kart adı | 14px |
| Konum | 12px |
| Form alt etiket | 11px |
| Input değer | 14px |

### Kart görsel yüksekliği (font değil)

| Breakpoint | Yükseklik |
|------------|-----------|
| Masaüstü | 210px (`--home-card-media-h`) |
| ≤900px | 180px |
| ≤520px | 168px |

---

## 3) Anasayfa — logo (wordmark only, ikon yok)

| Öğe | Masaüstü (≥901px) | Mobil |
|-----|-------------------|-------|
| OTEL (`--home-logo-size`) | 17px | 16px |
| TURİZM (`--home-logo-sub-size`) | 16px | 15px |
| OTEL letter-spacing | 0.22em | 0.18em |
| TURİZM letter-spacing | 0.15em | 0.12em |
| Ayırıcı (`.logo-sep`) | 1px gradient | 1px gradient |
| Font weight | 800 | 800 |

Markup: `Views/Anasayfa/_AnasayfaLogo.cshtml` · CSS: `anasayfa-header.css`

---

## 4) Anasayfa — header

| Rol | Masaüstü | Mobil |
|-----|----------|-------|
| Utility bar | 11px | gizli |
| TÜRSAB rozeti | 9.5px | — |
| Menü linkleri | 14px | drawer 14px |
| Dil / para seçici | 12px | — |
| Otelini Kaydet | 12.5px | — |
| Giriş Yap | 12.5px | 12px |
| Mobil menü ikon | — | 17px |
| Drawer logo kelime | — | 15px |

---

## 5) Anasayfa — hero

| Rol | Masaüstü | Mobil |
|-----|----------|-------|
| Kicker | 11px | 10px |
| Ana başlık | clamp(38px, 3.15vw, 54px) | clamp(28px, 7.4vw, 36px) |
| ≤520px başlık | — | clamp(26px, 6.8vw, 32px) |

---

## 6) Anasayfa — diğer bölümler (masaüstü)

| Rol | Boyut |
|-----|-------|
| Sürpriz kutu alt metin | 12.5px |
| Kategori chip | 13.5px |
| Fiyat (kart içi strong) | 14.5px |
| Kart rozeti | 10px |
| Fırsat kart başlık | 16px |
| Fırsat etiket | 10px |
| Fırsat gövde | 13px |
| Fırsat alt satır | 12.5px |
| Fiyat kalkanı başlık | 15.5px |
| Fiyat kalkanı metin | 13px |
| Fiyat kalkanı buton | 12.5px |
| Rozet / görev başlık | 13.5px |
| Rozet / görev alt metin | 11.5px |
| Destinasyon başlık | 14px |
| Destinasyon alt metin | 12px |
| Koleksiyon overlay başlık | 16px |
| Koleksiyon overlay alt | 12px |
| Deneyim başlık | 13.5px |
| Deneyim metin | 11.5px |
| Deneyim alt link | 12px |
| Club başlık | 20px |
| Club metin | 13.5px |
| Club buton | 13px |
| Radar kart başlık | 14px |
| Radar kart alt | 11.5px |
| Radar süre rozeti | 11px |
| Canlı izleyici | 10.5px |
| Radar fiyat | 14.5px |
| Finans kart başlık | 13.5px |
| Finans kart metin | 12px |
| AI pill (arama) | 9–10px |
| Typewriter ipucu | 14px |
| Route marquee chip | 11px |

---

## 7) Anasayfa — buton ölçüleri

| Özellik | Değer |
|---------|-------|
| Metin | 13px |
| Min yükseklik | 40px |
| Padding | 10px 16px |
| Köşe radius | 8px |

Geçerli: Otel Bul, Kalan Süre, Kutuyu Aç ve Uygula, mystery / arama CTA.

---

## 8) Paneller — kanonik token’lar (tüm paneller)

`panel-tasarim-sistemi.md` ile uyumlu. Taban rem = **16px**.

### Tipografi

| Rol | Masaüstü | Mobil | CSS token |
|-----|----------|-------|-----------|
| Sayfa H1 | clamp(29px, 4vw, 38px) | clamp(29px, 4vw, 34px) | `--h1-size` |
| Sayfa H2 | clamp(22px, 3vw, 29px) | clamp(22px, 3vw, 26px) | `--h2-size` |
| Sayfa H3 | clamp(18px, 2.5vw, 22px) | clamp(18px, 2.5vw, 20px) | `--h3-size` |
| Gövde metni | 15px (0.95rem) | 15px | `--body-size` |
| Küçük gövde | 14px (0.875rem) | 14px | — |
| İkincil / muted | 12px (0.75rem) | 12px | `--text-light` tonu |

### Bileşenler (ortak)

| Rol | Masaüstü | Mobil |
|-----|----------|-------|
| Buton (`.btn`) | 14px | 14px |
| Buton küçük (`.btn-sm`) | 13px | 13px |
| Form label | 13px | 13px |
| Form input / select | 14px | 16px |
| Form yardım metni | 12px | 12px |
| Badge / rozet | 12px | 11px |
| Tablo başlık | 13.5px | 13px |
| Tablo hücre | 14.5px | 14px |
| Toast / alert başlık | 14px | 14px |
| Toast / alert gövde | 13px | 13px |
| Filtre chip | 12px | 12px |

### Buton ölçüleri (panel)

| Özellik | Değer |
|---------|-------|
| Min yükseklik (form UX) | 40px |
| Radius (form UX) | 12px |
| Radius (genel pill) | 9999px (`--radius-full`) |
| Font weight (CTA) | 700 |

Kaynak: `paneller/panel-form-ux.css`

---

## 9) Admin panel shell

### Masaüstü (≥992px)

| Rol | Boyut |
|-----|-------|
| Sayfa H1 (`.admin-page-heading h1`) | clamp(29px, 2.4vw, 38px) |
| Sayfa alt açıklama | 15px (0.96rem) |
| Sidebar profil alt satır | 13px (0.82rem) |
| Sidebar nav link | 13px (0.82rem) |
| Sidebar grup etiketi | 11.5px (0.72rem) |
| Topbar / nav link | 14.5px (0.92rem) |
| Tablo / kart gövde | 14.5px (0.92rem) |
| KPI büyük değer | 32px (2rem) |
| KPI küçük etiket | 11px (0.68rem) |
| Footer / meta | 12px (0.76rem) |

### Mobil (≤991px)

| Rol | Boyut |
|-----|-------|
| Topbar min yükseklik | 62px (layout) |
| Sayfa başlık (`.page-title`) | 18px (1.12rem) |
| Alt mobil nav etiket | 11px (0.7rem) |
| Alt mobil nav ikon | 16px (1rem) |
| ≤576px nav etiket | 10px (0.64rem) |

Kaynak: `panel-admin-shell.css`, `panel-admin-shell.mobile.css`

---

## 10) Kullanıcı panel shell

### Masaüstü

| Rol | Boyut |
|-----|-------|
| Sidebar nav | 13px (0.83rem) |
| Sidebar bölüm etiketi | 11.5px (0.72rem) |
| Kart başlık | 16px (1rem) |
| Kart alt metin | 14px (0.88rem) |
| KPI değer | 32px (2rem) |
| KPI etiket | 12px (0.74rem) |
| Tablo / liste | 14.5px (0.92rem) |
| Mini label | 12px (0.74rem) |
| Badge | 11px (0.68rem) |

### Mobil

| Rol | Boyut |
|-----|-------|
| Shell gövde | 15px (1rem) taban korunur |
| Drawer / kompakt nav | 13px (0.85rem) |
| Alt aksiyon etiket | 11px (0.68rem) |

Kaynak: `panel-user-shell.css`, `paneller/user/shell.mobile.css`

---

## 11) Partner / Firma / Satış panelleri

Partner iç sayfaları kanon + admin/user shell ile hizalanır. Sık kullanılan değerler:

| Rol | Masaüstü | Mobil |
|-----|----------|-------|
| Dashboard sayfa başlık | clamp(20px, 2vw, 28px) | clamp(20px, 5vw, 24px) |
| Dashboard kicker / etiket | 11px | 10px |
| Stat kart değer | 28px | 24px |
| Stat kart etiket | 12px | 11px |
| Bildirim satır başlık | 14px | 14px |
| Bildirim satır alt | 12px | 12px |
| Fiyat vurgu (paket) | 22px (1.35rem) | 20px |

Kaynak: `paneller/partner/*.css` — yeni sayfada yukarıdaki kanon tercih edilir.

---

## 12) Kamu otel listeleme / detay (özet)

Listeleme şablonu (`listing-template-v41`) panel kanonuna yakın:

| Rol | Masaüstü | Mobil |
|-----|----------|-------|
| Sayfa başlık | clamp(26px, 2.3vw, 35px) | clamp(22px, 6vw, 28px) |
| Filtre / meta | 15px | 14px |
| Kart otel adı | clamp(21px, 1.7vw, 26px) | 18px |
| Konum / meta | 13px | 12px |
| Fiyat strong | 22px (1.35rem) | 20px |
| Vergi notu | 12px | 12px |
| Chip / tag | 13px | 12px |

Detay sayfası için aynı hiyerarşi: H1 > kart başlık > gövde > meta.

Kaynak: `otel-listeleme.css`, `otel-listeleme.mobile.css`, `otel-detay.css`

---

## 13) Selector hızlı indeks (anasayfa)

| Rol | Selector |
|-----|----------|
| Form başlık | `.search-pro-field label`, `.mystery-details h4` |
| Form alt etiket | `.search-pro-mini .mini-label` |
| Bölüm başlık | `.section-headline`, `h2.section-headline` |
| Kart adı | `.hotel-title` |
| Konum | `.hotel-desc` |
| Fiyat notu | `.price-tax-note` |
| Aksiyon | `.card-link` |
| Logo | `.logo-otel`, `.logo-turizm`, `.logo-sep` |

---

## 14) Değişiklik protokolü

1. Boyut değişecekse önce **bu dosya** güncellenir.
2. Anasayfa → `anasayfa_masaustu.css` / `anasayfa_mobil.css` token’ları.
3. Panel → `panel-tasarim-sistemi.md` + ilgili `panel-*-shell*.css`.
4. Yeni rol ekleniyorsa tabloya satır eklenir; dağınık piksel yazılmaz.

---

## 15) İlgili dosyalar

- `ANASAYFA_TIPOGRAFI_VE_BOYUTLAR.md` — anasayfa detay + logo (fontlar.md ile senkron tutulur)
- `Dokumanlar/Tasarim/tasarim-sistemi.md` — genel platform token’ları
- `Dokumanlar/Tasarim/panel-tasarim-sistemi.md` — panel kanon
