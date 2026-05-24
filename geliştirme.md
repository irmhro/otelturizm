# Platform Geliştirme — Canlı Özet

**Son güncelleme:** 2026-05-24 (#074 Wave-XIX partner fatura mobil)  
**Sprint:** `sprint-1ay-orkestra-20260523` (24h → **1 ay** uzatma)  
**Kullanıcı tam onay:** sonsuz orkestra aktif — chat’te görünmez → [`geliştrme-orkestra.md`](geliştrme-orkestra.md) canlı günlük  
**Detaylı sıralı log:** [`geliştrme-orkestra.md`](geliştrme-orkestra.md)  
**1 ay plan:** [`PLATFORM_1AY_ORKESTRA_PLAN.md`](PLATFORM_1AY_ORKESTRA_PLAN.md)  
**24h plan (referans):** [`PLATFORM_24SAAT_SPRINT.md`](PLATFORM_24SAAT_SPRINT.md)

> **CANLI UYARI:** Canlıda hiçbir şey görünmüyorsa → **tam Release publish zorunlu** (sadece cshtml yetmez). Adımlar: [`Docs/DEPLOY_ACIL_500_VE_GORUNUR_GELISTIRME.md`](Docs/DEPLOY_ACIL_500_VE_GORUNUR_GELISTIRME.md)

---

## Nasıl gidiyor?

| Gösterge | Durum |
|----------|--------|
| **Döngü** | **10 dk** geliştirme · **1 saat** GitHub push |
| **Job (10dk)** | `AGENT_LOOP_TICK_platform_coord` (600 sn) — **çalışıyor** |
| **Job (1sa)** | `AGENT_LOOP_HOURLY_git_sync` (3600 sn) — **aktif** |
| **Politika** | Onaysız orkestra CTO · **saatlik commit+push açık** · canlı deploy yok |
| **Build** | ✅ `dotnet build -o .coord-build-xix` — **0 hata** hedef (#074) |
| **Deploy** | 🔴 **Canlı gap** — repoda 60+ dalga; sunucuda eski build → 500 / görünür FE yok |
| **Platform olgunluk** | ~**42%** → hedef W1 **45%** |
| **Canlıya hazır** | **Kod P0 fix hazır** — tam publish + SQL + Production ortam şart |
| **FE-CTO** | **6/151** onaylı · **14** hedef SS path (PNG bekliyor) |

**Mandate:** Kullanıcı emanet — **10 dk dalgalar** + **1 ay sürekli orkestra** ile Booking/Expedia yörüngesi. Multigörev modunda arka plan ajanları durmadan devam eder.

> **Dürüst not:** ~52 backend/infra teslimi (Wave-I→XVII) **151 sayfa FE tamamlanmış** anlamına gelmez. Yeni izleme metriği: **sayfa olgunluk** tablosu (aşağıda).

---

## Sayfa olgunluk (151 FE-CTO envanter)

| Alan | Sayfa | Olgunluk | Not |
|------|-------|----------|-----|
| Kamu otel | 12 | **62%** | Wave-XVIII: liste hero/kart, detay review teaser, tokens |
| Kampanya / konsept | 8 | **56%** | Index hero timer Wave-XVIII; 3 SEO landing ✅ |
| User panel | 18 | **48%** | Mobil master ✅; SS PNG yok |
| Partner panel | 42 | **45%** | Wave-XIX: fatura mobil kart + misafir yükleme drag 🟡 |
| Admin panel | 38 | **50%** | Gelir/komisyon ✅; form UX kısmi |
| Satış / firma / departman | 22 | **52%** | **P0:** H6 firma panel Wave-F1 — mobil kart tablolar, rezervasyon personel+misafir, `Docs/FIRMA_PANEL_MASTER_PLAN.md` |
| E-posta / auth / SEO | 11 | **55%** | 7 dil Faz1 ✅; Faz2 şablonlar 🟡 |
| **Toplam ağırlıklı** | **151** | **~42%** | W4 hedef **75%** (canlı kapıları hariç %100 değil) |

---

## Aktif dalga

| Alan | Wave | Odak |
|------|------|------|
| **Şu an** | **#074** | Partner fatura mobil kart + `panel-form-ux` upload drag · SS path |
| **Önceki** | Wave-XVIII | fe-world-tokens · liste/detay/kampanya i18n |
| **Az önce** | Wave-XVII | Otel detay galeri/slider · liste kart hover · konsept landing ×2 |

### Sonraki 10 dalga (10 dk orkestra)

| # | Odak |
|---|------|
| 074 | FE listing + SS (`oteller-liste`) |
| 075 | Partner evrak upload |
| 076 | Admin komisyon Faz2 |
| 077 | Firma panel F2 |
| 078 | Panel screenshot batch |
| 079 | i18n panel backlog |
| 080 | SEO sitemap 7 dil |
| 081 | Security E2E smoke |
| 082 | API sözleşme |
| 083 | Muhasebe / partner fatura |

Detay: [`PLATFORM_10DK_SONRAKI_DALGALAR.md`](PLATFORM_10DK_SONRAKI_DALGALAR.md)

Kaynak: `ORKESTRA_DURUM_KONTROL.md` · `CTO_AJAN_ATAMA_KUYRUGU.md`

---

## 1 ay milestone tablosu

| Hafta | Bitiş % | Odak |
|-------|---------|------|
| W1 (D1–7) | **45%** | Kamu UI: galeri, typography, font, konsept URL |
| W2 (D8–14) | **55%** | Panel SS + mobil CSS 151 sayfa yolu |
| W3 (D15–21) | **65%** | SEO/i18n/e-posta/güvenlik E2E |
| W4 (D22–30) | **75%** | Performans, kampanya, rezervasyon E2E, K1–K8 |

---

## Tamamlanan ana başlıklar (özet)

1. **Kamu otel UI (H1)** — Wave-XVII: detay galeri nav/swipe/lightbox hook; Wave-XIV liste sadakat; Wave-XIII konsept CSS  
2. **Admin (H3)** — gelir merkezi, toplu yayın, yavaş SQL, dashboard widget’ları  
3. **Auth / satış (H7)** — SalesPanel, ReturnUrl, demo kullanıcı  
4. **Komisyon (H11)** — tahsilat merkezi Faz 1 (admin + partner + export)  
5. **User panel (H4)** — mobil master CSS, faturalar/favoriler/rezervasyonlar  
6. **Fatura (H12)** — kullanıcı + partner Faz 1  
7. **i18n (H13)** — Faz 1–3 (tr/en/de/fr/es/ar/ru), 49 anahtar, layout wiring  
8. **SEO (H9)** — 7 dil path, hreflang, `HotelListingSeo`, sitemap-en  
9. **E-posta (H14)** — master layout, 7 dil şablon servisi  
10. **Demo veri (H8)** — İstanbul 39 ilçe otel + medya seed  
11. **Orkestra altyapı** — sürekli döngü, 1 ay plan, gap analizi  

---

## Sıradaki P0

| Alan | P0 | Durum |
|------|-----|--------|
| **Panels** | `panel-form-ux` partner oda medya upload | 🟡 foto pilot ✅ |
| **Landing (H1)** | kalan konsept şablon HTML | 🟡 3/7 SEO URL ✅ |
| **SEO (H9)** | fr/es/ar/ru sitemap + hreflang | 🔴 açık |
| **Security (H7)** | Auth E2E smoke tüm paneller | 🟡 kısmi |
| **Speed** | liste output cache / lazy load | 🟡 backlog |
| **Email (H14)** | Faz2 kalan şablonlar (7 dil) | 🟡 devam |

---

## FE Dünya Standardı Orkestrası

**Charter:** [`ORKESTRA_FE_DUNYA_STANDARDI.md`](ORKESTRA_FE_DUNYA_STANDARDI.md)  
**Stream:** `H15_fe_world_standard` · Lead: `fe-world-ork`  
**Hedef:** 151 sayfa — ultra-prof CSHTML + `.css` + `.mobile.css` + i18n registry

| Hafta | Dalga | Kamu / panel odak |
|-------|-------|-------------------|
| W1 | Wave-XVIII+ | Liste, detay, kampanya, header/footer, tokens |
| W2 | Wave-XIX | Partner + user 40 sayfa |
| W3 | Wave-XX | Admin + satış + firma 40 sayfa |
| W4 | Wave-XXI | Polish, SS, Lighthouse |

**Wave-XVIII teslim:** `fe-world-tokens.css`, `OtelListeleme` hero/kart, `OtelDetay` review teaser, `Kampanyalar/Index` timer hero, header/footer i18n, `I18N_KEY_REGISTRY` güncelleme.

---

## Hızlı linkler

| Dosya | Amaç |
|-------|------|
| [`geliştrme-orkestra.md`](geliştrme-orkestra.md) | Sıralı geliştirme günlüğü (her dalga) |
| [`PLATFORM_1AY_ORKESTRA_PLAN.md`](PLATFORM_1AY_ORKESTRA_PLAN.md) | 30 gün × 1440 döngü takvimi |
| `ORKESTRA_DURUM_KONTROL.md` | KPI + köşe audit |
| `CTO_AJAN_ATAMA_KUYRUGU.md` | Ajan kuyruğu |
| `PLATFORM_SUREKLI_GELISTIRME_DONGUSU.md` | 10 dk sonsuz döngü |
| `Docs/PLATFORM_OZELLIK_GAP_ANALIZI.md` | Özellik gap tablosu |
| `Docs/PLATFORM_DUNYA_DEVLERI_YOL_HARITASI.md` | Dünya devleri yol haritası |
| `proje verileri/` | HTML şablon referansları |
| [`ORKESTRA_FE_DUNYA_STANDARDI.md`](ORKESTRA_FE_DUNYA_STANDARDI.md) | FE world-class orkestra charter |
