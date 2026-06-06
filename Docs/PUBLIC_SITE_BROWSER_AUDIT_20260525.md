# Public Site — Tarayıcı Denetim Raporu

**Tarih:** 2026-05-25  
**Ortam:** https://otelturizm.com (canlı, deploy öncesi/sonrası karşılaştırma)

## Yöntem

1. HTTP GET ile 18 public rota — layout sınıflandırması (`premium` vs `legacy-mix`).
2. Manuel kullanıcı akışı: ana sayfa → otel listesi → otel detay → yardım → giriş.

## Rota durumu (deploy öncesi)

| HTTP | Layout | URL |
|------|--------|-----|
| 200 | premium | `/` |
| 200 | premium | `/oteller` |
| 200 | legacy-mix | `/oteller/maidan-istanbul-boutique` |
| 200 | legacy-mix | `/oteller/harita` |
| 200 | premium | `/kampanyalar` |
| 200 | premium | `/kurumsal`, `/hakkimizda`, `/kariyer`, `/basin-odasi`, `/blog` |
| 200 | premium | `/firma` |
| 200 | legacy-mix | `/yardim-merkezi`, `/sss` |
| 200 | legacy-mix | `/seyahat-planlama` |
| 200 | premium | `/kullanici-giris`, `/partner-giris`, `/firma-giris` |
| 200 | legacy-mix | `/Home/Privacy` |

**Legacy işaretler:** `site-layout.css`, `bootstrap.min.css`, `yanbar`, `slaytgorsel` (layout seviyesinde).

## Migrasyon sonrası beklenen

Tüm `legacy-mix` rotalar → yalnızca `public-premium-pages`, `Plus Jakarta Sans`, `anasayfa-header` (Bootstrap yalnızca sayfa ihtiyacında opsiyonel).

## Eski tasarımda kalan (plan)

- `Shared/Error.cshtml`, `Shared/StatusCode.cshtml`
- `/gelisim` (development gate)

## Demo veri doğrulama

- Demo otel: `maidan-istanbul-boutique` (OTEL_ID=47)
- Ana sayfa vitrin: DB kaynaklı otel kartları
- Otel detay: 2 oda, fiyat, indirim, onaylı yorumlar

Detay plan: [PUBLIC_LEGACY_MIGRATION_PLAN.md](./PUBLIC_LEGACY_MIGRATION_PLAN.md)
