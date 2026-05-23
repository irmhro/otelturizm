# Platform Özellik Gap Analizi

**Tarih:** 2026-05-26 · **Wave:** `Wave-III-20260526-0200`  
**Rakip referansları:** Booking.com, Expedia, Otelz, Jolly, HotelRunner, SiteMinder (admin/CM yetenekleri)

**Durum kodları:** `var` · `kısmi` · `yok`

| # | Özellik | Sektör standardı | Otelturizm | Öncelik | Owner | Task ID |
|---|---------|------------------|------------|---------|-------|---------|
| 1 | Admin analytics (unified KPI) | Tek ekranda GMV, doluluk, kanal kırılımı | kısmi — `Dashboard`, `CommerceInsight` ayrık | P0 | H3 | T350 |
| 2 | RBAC audit log (kim, ne, önce/sonra) | Immutable audit + diff | kısmi — `AdminActionLogs` + CSV | P0 | H7 | T328 |
| 3 | Bulk operations (otel/rez/onay) | Çoklu seçim + toplu durum | yok | P0 | H3 | T329 |
| 4 | Approval workflows (çok aşamalı) | SLA, delegasyon, eskalasyon | kısmi — `ApprovalCenter` tek seviye | P0 | H3 | T330 |
| 5 | Revenue dashboard | Net/brüt, komisyon, trend, forecast | kısmi — `Commissions`, export CSV | P0 | H3 | T327 |
| 6 | Channel manager hooks | OTA bağlantı, rate/availability push | yok | P1 | H8 | T331 |
| 7 | Dynamic pricing rules | Kurallar, min stay, competitor floor | kısmi — partner `Pricing` | P1 | H3 | T332 |
| 8 | Guest messaging (platform oversight) | Merkezi inbox, şablon, SLA | kısmi — partner `GuestMessages` | P1 | H3 | T347 |
| 9 | Review moderation queue | SLA, otomatik flag, appeal | kısmi — `ReviewsModeration` | P1 | H3 | T348 |
| 10 | Fraud / risk alerts | Anomali rezervasyon, kart, velocity | yok | P0 | H7 | T333 |
| 11 | API keys management | Scope, rotate, revoke | yok | P1 | H8 | T334 |
| 12 | Webhooks (outbound) | Event subscription, retry, secret | yok | P1 | H8 | T335 |
| 13 | Export CSV (unified) | Tüm tablolar için standart export | kısmi — bazı admin export action | P1 | H3 | T336 |
| 14 | Scheduled reports | Email/cron, PDF/CSV | yok | P2 | H3 | T337 |
| 15 | Multi-property portfolio | Zincir otel, rollup KPI | yok | P1 | H3 | T338 |
| 16 | White-label / tenant branding | Logo, domain, email FROM | kısmi — `PanelThemeService` | P2 | H3 | T339 |
| 17 | A/B homepage experiments | Varyant, trafik %, conversion | yok | P2 | H9 | T340 |
| 18 | Map clustering (harita) | Marker cluster, bbox lazy load | kısmi — `HaritaOteller` | P1 | H1 | T341 |
| 19 | Wishlist sync (cihazlar arası) | Girişli merge, offline queue | kısmi — user `Favorites` | P1 | H4 | T342 |
| 20 | Loyalty points ledger | Kazanma/harcama, tier | kısmi — `Loyalty.cshtml` UI | P1 | H4 | T343 |
| 21 | Invoice e-Fatura | GIB durum, iptal, PDF arşiv | kısmi — `Invoices` listeler | P1 | H8 | T344 |
| 22 | 5651/5661 uyumluluk paketi | Satış, provisioning, audit | kısmi — migration+`PlatformPackages` | P0 | H3 | T345 |
| 23 | AI search assist (placeholder) | Admin toggle, prompt guardrails | yok | P2 | H9 | T346 |
| 24 | Rate parity / competitor monitor | OTA fiyat uyarısı | yok | P2 | H3 | T351 |
| 25 | Real-time notifications hub | WebSocket/SSE admin feed | kısmi — `Notifications` sayfa | P1 | H3 | T352 |
| 26 | Slow SQL observability | Top N, trend | kısmi — action var, view eksik | P1 | H3 | T353 |
| 27 | Security events console | Login fail, lockout, IP | kısmi — action, dedicated view yok | P1 | H7 | T354 |
| 28 | Upload history / malware scan | Dosya lineage, boyut | kısmi — `UploadHistory` action | P1 | H3 | T355 |
| 29 | Geo search analytics | Heatmap, sıfır sonuç | var — `GeoSearchLogs` | P2 | H3 | — |
| 30 | Platform health SLO | Uptime, dependency | var — `SystemHealth` | P2 | H8 | — |
| 31 | Partner commission payout | Ödendi işaretle, KPI | var — T309 partner | P1 | H2 | — |
| 32 | CSRF / rate limit governance | Platform-wide POST audit | var — T301/T302 | P0 | H7 | — |
| 33 | SEO canonical + etiket URL | Liste/kampanya tutarlılığı | var — T305/T307 | P1 | H9 | — |
| 34 | Demo content seed | İstanbul demo oteller | var — T308 seed | P1 | H8 | — |
| 35 | PII-safe logging | Log redaction policy | var — T107 | P1 | H8 | — |
| 36 | Fiyat düşüşü bildirimi | Kayıtlı otel/fiyat için e-posta/push alert | yok | P0 | H4 | T371 |
| 37 | Son dakika / flash deal vitrini | Ana sayfa + liste “deal” bandı, geri sayım | kısmi — kampanya modülü | P0 | H1 | T372 |
| 38 | Şeffaf toplam fiyat (vergi+dahil) | Checkout öncesi tek satır “ödeyeceğiniz” | kısmi — checkout copy | P0 | H4 | T373 |
| 39 | Anında rezervasyon (Instant Book) | Onay beklemeden kesin rezervasyon | yok | P1 | H2 | T374 |
| 40 | Harita-öncelikli arama (bbox, cluster) | Harita viewport ile filtre, cluster lazy | kısmi — `HaritaOteller` | P0 | H1 | T375 |
| 41 | Fotoğraf galeri tam ekran swipe | Detay lightbox, pinch/swipe, keyboard | kısmi — detay galeri | P0 | H1 | T376 |
| 42 | Misafir yorumu + foto kanıt moderasyonu | Yorumda foto, admin/partner SLA kuyruk | kısmi — `ReviewsModeration` | P1 | H3 | T377 |
| 43 | Kayıtlı arama / alert | Filtre kaydet, e-posta tetik | yok | P1 | H4 | T378 |
| 44 | Sadakat puanı checkout’ta kullan | Ödeme adımında puan düşümü | kısmi — `Loyalty` UI | P1 | H4 | T379 |
| 45 | Karşılaştırma (2–3 otel yan yana) | Özellik/fiyat tablo karşılaştırma | yok | P2 | H1 | T380 |
| 46 | Ücretsiz iptal rozeti liste kartında | Liste/kart badge, politika linki | kısmi — detay metin | P0 | H1 | T381 |
| 47 | “Bu fiyat X kişi bakıyor” sosyal kanıt | Liste/detay urgency (etik kurallı) | yok | P1 | H1 | T382 |
| 48 | Partner komisyon trend + payout tahmini | Dönem grafik, tahmini ödeme tarihi | kısmi — `Commissions` tablo | P0 | H2 | T383 |
| 49 | Structured data (Hotel/Offer/Breadcrumb) | JSON-LD kamu otel/liste/detay | yok | P0 | H9 | T389 |
| 50 | ilçe landing SEO (39 ilçe) | `/{sehir}/{ilce}` unique meta + içerik | kısmi — T307 canonical | P0 | H9 | T390 |

*Kaynak U1–U12 + Wave-III P0: `Docs/PLATFORM_DUNYA_DEVLERI_YOL_HARITASI.md`*

---

## Özet (Wave-III)

| Durum | Adet (tablo 1–50) |
|-------|-------------------|
| var | 7 |
| kısmi | 24 |
| yok | 19 |

**P0 kod önceliği (bu döngü):** T349 (build) → T375 → T376 → T381 → T373 → T383 → T389 → T390 → T350 → T357

*Canlı deploy yok; şema değişiklikleri yalnızca `Database/MigrationsSql` idempotent script.*
