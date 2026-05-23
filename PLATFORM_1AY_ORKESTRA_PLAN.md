# Platform 1 Ay Orkestra Planı

**Başlangıç:** 2026-05-23  
**Süre:** 30 gün · **1440 × 10 dk** döngü (haftalık gruplar)  
**Hedef:** Booking / Expedia seviyesine **dünya standardı yörüngesi** — 1 ayda %100 değil, **%75 olgunluk + sürekli kapı**  
**Koordinatör:** Platform Coordinator · Orkestra H1–H14

---

## Bağlantılar

| Doküman | Amaç |
|---------|------|
| [`geliştirme.md`](geliştirme.md) | Canlı KPI özeti |
| [`geliştrme-orkestra.md`](geliştrme-orkestra.md) | Sıralı dalga günlüğü (#001+) |
| [`Docs/PLATFORM_DUNYA_DEVLERI_YOL_HARITASI.md`](Docs/PLATFORM_DUNYA_DEVLERI_YOL_HARITASI.md) | Dünya devleri gap + sütunlar |
| [`PLATFORM_24SAAT_SPRINT.md`](PLATFORM_24SAAT_SPRINT.md) | 24 saat sprint (1 aya uzatıldı) |
| [`PLATFORM_SUREKLI_GELISTIRME_DONGUSU.md`](PLATFORM_SUREKLI_GELISTIRME_DONGUSU.md) | 10 dk sonsuz döngü |

---

## Olgunluk hedefleri (gerçekçi)

| Hafta | Bitiş % | Odak | Not |
|-------|---------|------|-----|
| **Hafta 1** | **45%** | Kamu UI polish | Liste/detay galeri slider, typography token, font stack |
| **Hafta 2** | **55%** | Panel SS + mobil CSS | 151 sayfa FE-CTO yolu, `panel-form-ux` tüm upload formları |
| **Hafta 3** | **65%** | SEO / i18n / e-posta / güvenlik E2E | 7 dil sitemap, auth smoke, e-posta Faz2 tamamlama |
| **Hafta 4** | **75%** | Performans, kampanya, rezervasyon E2E | Canlı kapıları K1–K8, output cache, checkout smoke |

> **Dürüst not:** 52 backend/infra teslimi ≠ 151 sayfa FE tamam. Yeni metrik: **sayfa olgunluk** tablosu (`geliştirme.md`).

---

## 30 günlük dalga takvimi (1440 × 10 dk)

Her gün **144 döngü** (24 saat × 6 tur/saat). Haftalık gruplar aşağıda **kavramsal** bloklar; koordinatör her tick’te gap analizinden bir P0 seçer.

### Hafta 1 — Kamu UI polish (D1–D7 · ~1008 döngü)

| Gün | Döngü aralığı | Tema | Çıktı |
|-----|---------------|------|-------|
| D1 | 1–144 | Otel detay galeri | Swipe, thumbnail strip, lightbox hook, Inter/clamp tipografi |
| D2 | 145–288 | Otel listeleme | Kart hover, font hiyerarşisi, skeleton/LCP |
| D3 | 289–432 | Konsept landing URL | `/havuzlu-oteller`, `/hafta-sonu-firsatlari`, `/evcil-hayvan-dostu-oteller` + SEO |
| D4 | 433–576 | Header / footer | 7 dil nav, safe-area, mobil drawer |
| D5 | 577–720 | Harita + kampanya kamu | Cluster, hero, stat chips |
| D6 | 721–864 | Rezervasyon sidebar UX | Fiyat şeffaflığı, sticky CTA |
| D7 | 865–1008 | Hafta 1 verify | Build gate, FE smoke, %45 milestone |

### Hafta 2 — Panel SS + mobil CSS (D8–D14 · ~1008 döngü)

| Gün | Döngü aralığı | Tema | Çıktı |
|-----|---------------|------|-------|
| D8–D9 | 1009–1296 | Partner panel | `panel-form-ux` foto/oda upload, tablo→kart |
| D10–D11 | 1297–1584 | Admin panel deep | Gelir merkezi, komisyon tahsilat/mutabakat, partner evrak kuyruğu, T356 toplu yayın, sözleşme seed (H16), `panel-form-ux` |
| D12 | 1585–1728 | User / satış / firma | FE-CTO PNG batch-1 (14 hedef SS) |
| D13 | 1729–1872 | Departman / otel paneli | Mobil master CSS tamamlama |
| D14 | 1873–2016 | Hafta 2 verify | 151 sayfa envanter güncelleme, %55 |

### Hafta 3 — SEO / i18n / e-posta / güvenlik E2E (D15–D21)

| Gün | Döngü aralığı | Tema | Çıktı |
|-----|---------------|------|-------|
| D15–D16 | 2017–2304 | SEO Faz2 | fr/es/ar/ru sitemap + hreflang doğrulama |
| D17 | 2305–2448 | i18n panel string | Kalan hardcoded TR → resx |
| D18–D19 | 2449–2736 | E-posta Faz2 | 7 dil × rezervasyon/fatura/güvenlik şablonları |
| D20 | 2737–2880 | Auth E2E | Tüm paneller smoke tablosu PASS |
| D21 | 2881–3024 | Hafta 3 verify | %65 milestone |

### Hafta 4 — Performans, kampanya, rezervasyon E2E, K1–K8 (D22–D30)

| Gün | Döngü aralığı | Tema | Çıktı |
|-----|---------------|------|-------|
| D22–D23 | 3025–3312 | Performans | Output cache, lazy load, WebP audit |
| D24–D25 | 3313–3600 | Kampanya E2E | Admin → partner → kamu vitrin |
| D26–D27 | 3601–3888 | Rezervasyon E2E | Arama → ödeme → e-posta |
| D28–D29 | 3889–4176 | Canlı kapıları K1–K8 | Yedek, migration sırası, smoke |
| D30 | 4177–4320 | Ay kapanış | %75 rapor, backlog Wave-II (Ay 2) |

---

## CTO atama özeti (1 ay)

| Orkestra | Hafta 1 | Hafta 2 | Hafta 3 | Hafta 4 |
|----------|---------|---------|---------|---------|
| H1 fe-otel-public | Galeri, liste, landing | Kamu verify | SEO entegrasyon | Perf |
| H2 fe-partner | — | SS + upload UX | Panel i18n | Kampanya |
| H3 fe-admin | — | Mobil admin + Wave-A1 (evrak, komisyon, onay) | — | Gelir E2E |
| H16 ork-hukuk | — | Sözleşme seed + şablon | Hukuk review checklist | Versiyonlama |
| H4 fe-user | — | User SS | — | Rezervasyon UX |
| H7 ork-guvenlik | — | — | Auth E2E | K3 gate |
| H8 ork-backend | Demo verify | Migration | — | K1–K2 |
| H9 ork-seo | Konsept URL | — | Sitemap 7 dil | Schema |
| H13 i18n | Header/footer | Panel string | Faz3 kapanış | — |
| H14 email | — | — | Faz2 tam | Rezervasyon mail E2E |
| H10 master-cto | Build gate | FE envanter | KPI | K8 sign-off |

---

## İzleme

- **Her 10 dk:** `AGENT_LOOP_TICK_platform_coord` → PLAN → EXECUTE → VERIFY  
- **Her 1 saat:** `AGENT_LOOP_HOURLY_git_sync` → commit + push  
- **Her hafta sonu:** `ORKESTRA_DURUM_KONTROL.md` + `%` milestone güncelle  
- **Build gate:** `dotnet build -o .coord-build-*` — 0 hata zorunlu

*Son güncelleme: 2026-05-24 · Wave-XVII*
