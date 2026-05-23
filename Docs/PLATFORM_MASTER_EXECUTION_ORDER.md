# Platform tamamlama — sıralı yürütme sırası (tek doğruluk kaynağı)

**Agent grupları:** [AGENT_GRUPLARI_MASTER.md](../AGENT_GRUPLARI_MASTER.md) · **Grup 14:** [PROJECT_COMPLETION_SUMMARY.md](../PROJECT_COMPLETION_SUMMARY.md)

**Tek pencere özet ve boşluk tablosu:** `PLATFORM_SINGLE_WINDOW_AUDIT_AND_BACKLOG.md` — mimari, riskler ve iş sırası (S1–S10).

**Kurumsal B2B + satış + SEO birlikte:** `CROSS_PANEL_B2B_AND_SEO_ROADMAP.md` — firma/partner/satış/admin akışı ve konum bazlı SEO sprint sırası.

Bu belge, depodaki diğer plan dosyalarını **tek bir sıraya** oturtur. Her fazın sonunda **doğrulama** maddeleri işaretlenmeden sonraki faza geçilmez.

> **Kapsam gerçeği:** “Tam sürüm yayın”, OTA (Booking/Agoda vb.) paritesi ve tam güvenlik olgunluğu **çok fazlasıyla sprint ve insan kontrollü QA** gerektirir; burada süreç sırası ve çıktı tanımı sabittir.

## Faz sırası (özet)

| Sıra | Faz | Ana kaynak belge | Çıktı / doğrulama |
|------|-----|------------------|-------------------|
| 1 | Güvenlik & sağlık uçları | `SECURITY_PLATFORM_PLAN.md` | `/health/*` yanıtları, CSRF/rate limit, üretimde sızıntı yok |
| 2 | SEO & kamusal indeks | `SEO_BOOKING_PARITY_PLAN.md` | robots/sitemap/canonical/otel JSON-LD kontrol listesi |
| 3 | Paneller operasyon | `PANELS_OPERATIONS_AND_SECURITY_MAP.md` | RBAC, audit, kritik mail akışları |
| 4 | Admin tam özellik seti | `ADMIN_PANEL_FULL_PLAN.md`, `ADMIN_FULL_RELEASE_PLAN.md` | Özellik başına kabul kriteri |
| 5 | Kullanıcı / Partner / Firma panelleri | `USER_PANEL_FULL_PLAN.md`, `PARTNER_PANEL_FULL_PLAN.md`, `FIRMA_PANEL_FULL_PLAN.md` | Panel bazlı smoke test |
| 6 | Mobil & tasarım drift | `wwwroot/assets/css/**/*.mobile.css` + sayfa `PageCssPath` | Dar görünümde kırık layout raporu |
| 7 | Veri & migration | `Database/MigrationsSql/` | Staging’de güvenli migrate + rollback notu |
| 8 | Yayın öncesi smoke | Aşağıdaki checklist | Üretim checklist tamam |

## Faz 8 — Yayın smoke (minimum)

- [ ] Ana sayfa, otel liste, otel detay, rezervasyon başlatma (mutlu yol)
- [ ] Girişler: kullanıcı / partner / firma / admin (yetkisiz → login)
- [ ] `/health/live`, `/health/ready`, `/health/platform` (beklenen HTTP kodları)
- [ ] Admin: Sistem sağlığı, Platform checkup, Güvenlik (yetki ile)
- [ ] Log klasörü yazımı (Sunucu): `App_Data/logs` izinleri

## Grup ID → faz eşlemesi

| Faz | Grup ID |
|-----|---------|
| Güvenlik | 07 |
| SEO / public | 11, 06 |
| Paneller | 08, 09, 10 |
| Mobil CSS | 06 |
| DB migration | 01, 12 |
| Yayın smoke | 14 |

## Paralel “iş gücü” modeli (insan takımı için)

Aynı fazda paralelleştirilebilir hatlar (birbirini bloke etmez):

- **A:** Güvenlik middleware + API rate limit + audit  
- **B:** SEO + içerik şeması  
- **C:** Panel iş kuralları + komisyon  
- **D:** Mobil CSS drift  
- **E:** DB migration + yedek  

*(Bu yapı geliştirici takımı içindir; tek yapay zekâ oturumu tüm kalemleri_bitiremez.)*

## İlgili dosyalar

- `Docs/SECURITY_PLATFORM_PLAN.md`
- `Docs/SEO_BOOKING_PARITY_PLAN.md`
- `Docs/PANELS_OPERATIONS_AND_SECURITY_MAP.md`
- `Docs/ADMIN_PANEL_FULL_PLAN.md`, `Docs/ADMIN_FULL_RELEASE_PLAN.md`
- `Docs/USER_PANEL_FULL_PLAN.md`, `Docs/PARTNER_PANEL_FULL_PLAN.md`, `Docs/FIRMA_PANEL_FULL_PLAN.md`

## Sürekli iyileştirme

Her sprint sonunda: bu belgenin tablosuna **tamamlanan satır → tarih → sorumlu** notu düşün.
