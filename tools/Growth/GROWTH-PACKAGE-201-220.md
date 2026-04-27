# Growth paketi (201–220) — teknik özeti

Bu dosya `improvements-first-200.md` içindeki **201–220** maddelerinin depoda hangi bileşenlere karşılık geldiğini özetler.

| # | Başlık | Kod / konfig |
|---|--------|----------------|
| 201 | Image resize | `Controllers/MediaFitController.cs` → `GET /media/fit?path=&w=` |
| 202 | Fingerprint + RL | `Middleware/GrowthFingerprintMiddleware.cs`, `Program.cs` (`quote-strict`, `growth-ingest`) |
| 203 | Post-booking | `PublicReservationService` (hava prefetch, `POST_BOOKING_AUTOMATION`) |
| 204–205 | Funnel / rage / dead | `wwwroot/assets/js/growth-analytics.js`, `Controllers/Api/GrowthAnalyticsController.cs` |
| 206 | Dinamik sıralama | `HotelService` liste sorgusu `ORDER BY` sonuna skor |
| 207 | Null search | `NULL_SEARCH` log (`HotelService`) |
| 208 | Form abandonment | `growth-analytics.js` blur → `form_abandon` |
| 209 | Feature flag | `Services/FeatureFlagService.cs`, `appsettings.json` → `Growth:Flags` |
| 210 | Sosyal kanıt | `PublicGrowthSignalsService`, `OtelDetay` bandı |
| 211 | Cross-sell | `HotelService` `SimilarHotels`, detay şeridi |
| 212 | Niyet | `?trip=`, `Otelturizm.UserIntent` |
| 213 | NPS | `NPS_LOOP_PLANNED` log |
| 214 | Fiyat cache purge | `PartnerPanelController` → `IOutputCacheStore.EvictByTagAsync` |
| 215 | Elasticity | `PRICE_ELASTICITY` (`OtellerController` `GetPriceQuote`) |
| 216 | Adaptive | `html.ot-adaptive-lite` (connection + slow navigation) |
| 217 | Hero öncelik | `OtelDetay.cshtml` `fetchpriority="high"` |
| 218 | Bağlamsal arama | `Otelturizm.SearchCtx`, `HotelService` `contextBoost`, OutputCache `SetVaryByValue` |
| 219 | Ödeme orkestrasyon | `IPaymentOrchestrationAdvisor` (genişletme noktası) |
| 220 | Currency shield | `currency-shield:v1:*` `IMemoryCache` (`OtellerController`) |

Loglar Serilog JSON dosyasında `GROWTH_EVENT`, `NULL_SEARCH`, `PRICE_ELASTICITY`, `QUOTE_SHIELD_HIT` vb. anahtarlarla aranabilir.
