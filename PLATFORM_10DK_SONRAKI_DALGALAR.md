# Platform — sonraki 12 × 10 dk dalga (tek sayfa)

**Tarih:** 2026-05-24  
**Bağlam:** #073 deploy-gap kapatıldıktan sonra orkestra öncelik sırası  
**Kural:** Her dalga bitince `geliştrme-orkestra.md` + `geliştirme.md` güncellenir; canlı için [`Docs/DEPLOY_ACIL_500_VE_GORUNUR_GELISTIRME.md`](Docs/DEPLOY_ACIL_500_VE_GORUNUR_GELISTIRME.md)

| # | Dalga | Alan | Çıktı |
|---|-------|------|--------|
| 074 | FE listing polish | H1 kamu | Liste/harita/detay SS + mobil CSS tutarlılığı |
| 075 | Partner evrak | H2 | Evrak yükleme UX, mobil form, SS batch |
| 076 | Admin komisyon | H3/H11 | Tahsilat merkezi Faz2, export smoke |
| 077 | Firma F2 | H6 | Rezervasyon E2E, deals deep link, SS |
| 078 | Panel SS wave | H2–H6 | `docs/frontend-screenshots` PNG seti (10+ sayfa) |
| 079 | i18n Faz2 | H13 | Panel string backlog, ar/ru eksikleri |
| 080 | SEO Faz2 | H9 | fr/es/ar/ru sitemap + hreflang doğrulama |
| 081 | Security smoke | H7 | Tüm panel giriş + ReturnUrl E2E |
| 082 | API sözleşme | Backend | Partner/public API dokümantasyon + rate limit |
| 083 | Muhasebe ödemeler | H11/H12 | Partner fatura + ödeme durumu kartları |
| 084 | Satış panel | H5 | Mobil master + rezervasyon SS |
| 085 | Performans | H1/H10 | Liste output cache, lazy load görseller |

**P0 blok:** Dalga 074–075 canlıda görünür olması için **her FE dalgasından önce veya sonra** tam Release publish (deploy doc).

**Metrik dürüstlüğü:** Repoda 60+ dalga teslimi ≠ canlıda 60+ özellik. Canlı = **publish + SQL + Production ortam**.
