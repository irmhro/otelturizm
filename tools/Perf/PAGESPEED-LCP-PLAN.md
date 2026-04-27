## Paket 111–120 (SEO + İçerik) — LCP hedef planı

Bu doküman, public sayfalarda özellikle **LCP (Largest Contentful Paint)** metriklerini iyileştirmek için hedefleri ve pratik adımları özetler.

### Hedefler

- **LCP (mobil)**: \(<= 2.5s\) iyi, \(2.5–4.0s\) iyileştirilebilir, \(> 4.0s\) kötü
- **CLS**: \(<= 0.1\)
- **INP**: \(<= 200ms\)

### Genel kurallar

- **Hero görseli**: LCP çoğunlukla hero görseli veya ilk büyük başlık olur.
- **Kritik CSS**: İlk render’ı geciktiren büyük CSS’leri azaltın; mümkünse sayfa bazlı CSS kullanın.
- **Fontlar**: `preload` + `font-display: swap` yaklaşımı, LCP/FOIT riskini azaltır.
- **Render-blocking JS**: Head içine büyük JS koymayın; mümkünse `defer` veya body sonu.

### Otel detay (`/oteller/{slug}`) önerileri

- **Hero görsel**:
  - `width/height` ekleyin (CLS azaltır)
  - mümkünse **WebP/AVIF** ve doğru boyut seti (`srcset`)
  - LCP görseli ise `fetchpriority="high"` ve `loading="eager"` (yalnızca tek hero için)
- **Harita/3rd party**: Harita ve ağır bileşenler “below-the-fold” olacak şekilde geciktirilmeli.

### Listeleme (`/oteller`) önerileri

- İlk 1–2 kart görselini optimize edin, diğerlerini `loading="lazy"`.
- Filtre UI’ı için gereksiz script/css yükünü azaltın.

### Kampanyalar (`/kampanyalar`) önerileri

- Kampanya hero görselini optimize edin, kart görsellerini `loading="lazy"` yapın.

