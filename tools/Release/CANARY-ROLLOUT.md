# Canary release (paket 224)

**Amaç:** Trafiğin küçük bir yüzdesini yeni sürüme verip metrik ve hata oranına göre durdur/genişlet.

Pratik tetikleyiciler:

- Reverse proxy veya CDN üzerinden %1–%10 ağırlık.
- **Geri alma koşulları:** 5xx oranı temel çizginin üstünde, `RUM_VITALS`/`GROWTH_EVENT` düşüşü, ödeme/rezervasyon başarı oranı düşüşü.

Bu repo içinde runtime bayrakları:

- `Growth:KillSwitchAll` ve Admin **Ticari içgörü** ekranındaki acil kill-switch ile growth ingest ve yüzdelik rollout’lar güvenli şekilde kapatılabilir.
