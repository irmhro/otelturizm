# H1 Kamu UI — Şablon Referans Eşlemesi (Wave-XIII)

Kısa eşleme: `proje verileri` HTML → proje view/CSS. Tam HTML kopyalanmaz; token ve bileşen hizası hedeflenir.

| Şablon | Proje karşılığı | CSS |
|--------|-----------------|-----|
| `04-otel-arama-ve-detay/OTEL LİSTELEME SAYFASI.html` | `Views/Oteller/OtelListeleme.cshtml` | `otel-listeleme.css` + `.mobile.css` |
| `04-otel-arama-ve-detay/ULTRA LÜKS OTEL LİSTELEME SAYFASI .html` | `?etiket=ultra-luks` konsept + premium kart stilleri | `otel-listeleme.css` |
| `kodlaması tamamlanmış sayfalar/Kampanyalı Oteller.html` | `Views/Kampanyalar/Index.cshtml`, `Detail.cshtml` | `kampanyalar*.css`, `kampanya-detay*.css` |
| Harita (liste şablonu harita FAB) | `Views/Oteller/HaritaOteller.cshtml` | `haritaoteller.css` + `.mobile.css` |
| Otel detay (04 klasörü + mevcut v41) | `Views/Oteller/OtelDetay.cshtml` | `otel-detay.css` + `.mobile.css` |

## Konsept bar → `?etiket=`

| UI etiketi | Route |
|------------|--------|
| En İyi Fiyat | `?etiket=akilli-fiyat` |
| Hafta Sonu | `?etiket=hafta-sonu-firsatlari` |
| Bütçeme Uygun | `?etiket=butceme-uygun-oteller` |
| Yıldız Yağmuru | `?etiket=ultra-luks` |
| Evcil Hayvan | `?etiket=evcil-hayvan-dostu` |
| Havuzlu | `?etiket=havuzlu-oteller` |
| Kampanyalı | `?etiket=kampanyaya-dahil-oteller` |
| Tüm Kampanyalar | `/kampanyalar` |

Filtre mantığı: `HotelService` / `NormalizeCampaignTag` + liste sorgusu.

## İndirim kartı (köşe audit)

Şablon: `discount-badge` (sol üst) + `old-price` + kampanya fiyatı.  
Uygulama: `.listing-card-discount-corner` + `.old-price` + `.listing-discount-rate--pill` (mobilde gizlenmez).

## Tasarım tokenları

`--primary #003B95`, `--secondary #FF385C`, `--success #00A86B` — `otel-listeleme.css` `:root` / `--listing-*` ile hizalı.

## Wave-XIV ekleri

- Geçiş: `--transition-base` (`site-layout.css` ile aynı eğri) — liste kartları, kampanya hero/stat chip, harita geri linki.
- Sadakat: liste fiyat bloğunda `listing-loyalty-earn` (+N puan) veya `data-loyalty-hook` giriş placeholder.
- Panel form: `paneller/panel-form-ux.css` — sil/düzenle/yükle (pilot: partner misafir faturaları).
