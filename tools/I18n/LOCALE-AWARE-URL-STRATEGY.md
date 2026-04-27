## Locale-aware URL stratejisi (p180)

### Mevcut durum

- Uygulama `RequestLocalization` ile locale seçimini destekliyor.
- Public sayfalarda `hreflang` ve sitemap alternates için `?lang=xx-YY` kullanıyoruz.
- Kullanıcı etkileşimiyle `/locale/set?lang=...` cookie yazıyor (PRG pattern).

### Alternatifler

1) **Query param**: `?lang=tr-TR`
   - **Artıları**: hızlı; route kırmaz; SEO için `hreflang` ile uyumlu; cache vary yönetimi kolay.
   - **Eksileri**: URL’ler daha “kirli”; paylaşımda dil paramı taşır.

2) **Path prefix**: `/tr/oteller`, `/en/hotels`
   - **Artıları**: SEO’da dil sayfaları ayrışır; canonical/hreflang daha net.
   - **Eksileri**: tüm route’lar değişir; redirect/canonical kuralları gerekir; link checker/testleri genişler.

3) **Subdomain**: `tr.otelturizm.com`, `en.otelturizm.com`
   - **Artıları**: dil ayrımı çok net; CDN/cache ayrımı doğal.
   - **Eksileri**: DNS/SSL operasyonu; cookie scope/SSO karmaşıklığı; local dev zorluğu.

### Önerilen hedef (kademeli geçiş)

- Kısa vadede: **Query param + cookie** (şu anki yaklaşım) + `Accept-Language` vary cache devam.
- Orta vadede: `/tr/` prefix’e geçiş için bir “routing shim” planı:
  - önce read-only redirect (301) ile `/tr/*` → `/*?lang=tr-TR`
  - sonra gerçek route map’i iki dillilikle üretme.

### Dikkat edilmesi gerekenler

- Canonical URL: tracking/query temizliği (UTM vb.) korunmalı.
- `hreflang`: parametreli sayfalarda path korunmalı, yalnızca `lang` değişmeli.
- Cache: `Accept-Language` + `lang` query vary; auth’lu kullanıcıda cache kapalı.

