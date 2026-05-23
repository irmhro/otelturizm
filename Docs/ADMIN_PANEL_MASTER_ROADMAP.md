# Admin Panel Master Roadmap

**Vizyon:** *En gelişmiş admin* — rakip CM/OTA admin konsollarıyla eşdeğer veya üstü: tek cam revenue command center, tam RBAC şeffaflığı, bulk+workflow, entegrasyon hub, proaktif risk.

**Kod envanteri (2026-05-23):** `Views/Paneller/Admin` ~51 sayfa view; `AdminPanelController` tek controller; dedicated `*.mobile.css` ~14 sayfa + layout override. `SlowSql`, `SecurityEvents`, `UploadHistory` → action var, **view yok**.

**Registry:** T350–T370 · Owner: **H3_admin_master** + **H8** backend

---

## Faz özeti

| Faz | Hedef | Çıkış kapısı |
|-----|-------|--------------|
| **P0** | Gelir + risk + build stabil | Revenue Center + fraud + bulk v1 + build 0 hata |
| **P1** | Operasyon + entegrasyon | Channel hook, API keys, export center, SlowSql/Security views |
| **P2** | Ölçek + marka | Multi-property, white-label, scheduled reports |
| **P3** | Fark yaratan | A/B admin, AI search config, parity monitor |

---

## P0 — Bu döngü ve +2 tur (T350–T357)

| Task | Özellik (kodda yok / eksik) | Deliverable | Bağımlılık |
|------|----------------------------|-------------|------------|
| **T350** | Revenue Command Center | `RevenueCommandCenter.cshtml` + service aggregate GMV/komisyon/5651 | H8 metrics API |
| **T351** | Rate parity monitor (v0) | Admin sayfa: manuel competitor URL + alert listesi | H8 |
| **T352** | Real-time ops notification feed | Dashboard widget + SSE stub | H8 |
| **T353** | Slow SQL dedicated view | `SlowSql.cshtml` + mobile.css (action mevcut) | — |
| **T354** | Security events console | `SecurityEvents.cshtml` + filtre + export | H7 |
| **T355** | Upload history UI | `UploadHistory.cshtml` + `SecureFileService` tie-in | H7 |
| **T356** | Bulk hotel publish/unpublish | `Hotels` çoklu seçim POST + audit | T328 |
| **T357** | Fraud alert inbox | `FraudAlerts.cshtml` + rule stub tablo | H7 migration |

---

## P1 — Entegrasyon ve workflow (T358–T364)

| Task | Özellik | Deliverable |
|------|---------|-------------|
| **T358** | Multi-stage approval designer | `ApprovalCenter` stage config + SLA badge |
| **T359** | Unified export center | `/admin/veri-aktarim` — CSV şablonları |
| **T360** | Channel manager hub | `/admin/kanal-yoneticisi` — webhook URL, sync log |
| **T361** | API keys + scopes | CRUD + `admin.api_keys` migration |
| **T362** | Outbound webhooks registry | Event type, secret rotate, delivery log |
| **T363** | Dynamic pricing rules (admin read) | Kurallar listesi, partner rule mirror |
| **T364** | Guest messaging oversight | Cross-hotel thread list, mute/ban |

---

## P2 — Ölçek ve uyumluluk (T365–T368)

| Task | Özellik | Deliverable |
|------|---------|-------------|
| **T365** | Multi-property portfolio | Zincir rollup dashboard |
| **T366** | White-label tenant config | Domain/logo/email şablon |
| **T367** | Scheduled reports | Cron UI + email queue entegrasyonu |
| **T368** | e-Fatura monitor | GIB durum, hata yeniden dene |

---

## P3 — Fark yaratan (T369–T370)

| Task | Özellik | Deliverable |
|------|---------|-------------|
| **T369** | Homepage A/B admin | Varyant CRUD + trafik yüzdesi |
| **T370** | AI search assist config | Placeholder toggle + prompt policy (no LLM prod) |

---

## Mevcut güçlü yanlar (korunacak)

- `PlatformPackages` + 5651/5661 paket satışı (T322/T345)
- `AdminActionLogs` + CSV export
- `CommerceInsight` + growth kill switch
- `ReviewsModeration` + blocked words
- `GeoSearchLogs`, `RateLimitStats`, `PlatformCheckup`
- RBAC seed (`platform_admin_full`)

---

## FE-CTO hedefi (admin)

| Metrik | Şimdi | P0 bitiş | P3 bitiş |
|--------|-------|----------|----------|
| Sayfa SS onayı | ~1/55 | 20/55 | 55/55 |
| Dedicated mobile.css | ~14 | 30 | 55 |
| Yeni sayfa (roadmap) | 0 | +4 (T353–355, T357) | +10 |

---

*Senkron: `PLATFORM_SUREKLI_GELISTIRME_DONGUSU.md` · `CTO_AJAN_ATAMA_KUYRUGU.md` wave_i*
