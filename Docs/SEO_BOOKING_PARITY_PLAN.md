# Otelturizm — SEO ve keşfedilebilirlik planı (Booking / Airbnb / Expedia / Agoda referansı)

Bu belge, OTA standartlarına yaklaşmak için teknik SEO, içerik ve operasyonel kontrolleri özetler. Uygulama öncelikleri aşağıdaki fazlara ayrılmıştır.

## 1. Teknik temel (tamam / sürekli)

| Alan | Hedef | Not |
|------|--------|-----|
| **HTTPS** | Zorunlu, HSTS | Üretimde geçerli sertifika |
| **Canonical** | Her önemli URL için tek canonical | Ana sayfa ve otel detaylarında `App:PublicBaseUrl` ile tutarlılık |
| **Robots** | Public içerik indeks; panel/API kapalı | `wwwroot/robots.txt` — `Sitemap` üretim domain ile |
| **Sitemap** | `/sitemap.xml` güncel | `SitemapService` + admin yenileme |
| **JSON-LD** | WebSite + SearchAction (ana sayfa); otel sayfalarında Hotel / LodgingBusiness | Sayfa bazında genişletme |
| **Performans** | LCP, CLS; görsel boyutları | Hero ve liste görselleri için uygun boyut/webp |
| **Hreflang** | Tek dil (tr) ise `html lang="tr"` yeterli | Çok dil planlanırsa x-default + dil kodları |

## 2. Sayfa bazlı meta ve yapı

- **Ana sayfa:** Title, meta description, OG/Twitter, JSON-LD (mevcut katman).
- **Otel listesi (`/oteller`):** Filtre parametreleri için canonical kuralları (fazla sayfa indeksi önleme: ana liste canonical veya `noindex` stratejisi).
- **Otel detay:** Benzersiz title/description; fiyat aralığı veya yıldız bilgisi meta’ya özet; mümkünse `Hotel` + `Offer` şeması.
- **Kampanya / içerik sayfaları:** Tek amaçlı URL; duplicate içerikten kaçınma.
- **404/410:** Kalıcı kapanan oteller için uygun HTTP kodu ve yönlendirme politikası (`DeadLinkRedirect` ile uyum).

## 3. İçerik ve E-E-A-T

- Şeffaf **iptal ve ödeme** metinleri (sayfa ve footer linkleri).
- **KVKK / gizlilik / kullanım koşulları** footer ve indekslenebilir statik sayfalar.
- Otel ve destinasyon için benzersiz metin (şablon + veri alanları).

## 4. Ölçüm ve izlenebilirlik

- Arama konsolu (Google / Bing) doğrulama ve sitemap gönderimi.
- Önemli dönüşüm URL’lerinde tutarlı UTM / kampanya takibi (marketing ile hizalı).

## 5. Uygulama öncelik sırası (kısa)

1. Üretim `App:PublicBaseUrl` ve robots/sitemap tutarlılığı.
2. Otel detay JSON-LD ve dinamik meta.
3. Liste ve sayfalama canonical/`robots` kuralları.
4. İçerik ve hukuki sayfaların iç link grafiği.

Bu plan, kod değişiklikleriyle birlikte iteratif güncellenmelidir; her sprintte “teknik + içerik” maddelerinden en az biri kapatılmalıdır.
