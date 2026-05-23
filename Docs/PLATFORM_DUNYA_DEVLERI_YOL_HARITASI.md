# Platform — Dünya Devleri Yol Haritası

**Mandate:** Booking · Expedia · Airbnb · Agoda · Hotels.com seviyesinde — tasarım, özellik, güvenlik, hız, SEO, dönüşüm.  
**Döngü:** Her 30 dk `PLATFORM_SUREKLI_GELISTIRME_DONGUSU.md` → PLAN → EXECUTE → VERIFY.  
**Onay:** Orchestra CTO ajanları `delegation_policy: kullanici_onaysiz_30dk_wave_assign` (commit/deploy hariç).

---

## 1. Altı sütun (sürekli gelişim)

| Sütun | Hedef | KPI | Owner |
|-------|--------|-----|-------|
| **Tasarım & mobil** | Tek ekran, tam alan, 44px+, safe-area, görsel hiyerarşi | FE-CTO 151/151, mobil LCP | H1–H6 |
| **Özellik & dönüşüm** | Arama → rezervasyon < 3 adım, şeffaf fiyat | Rezervasyon tamamlama % | H1,H4,H2 |
| **Komisyon & gelir** | Net komisyon, payout, kampanya ROI | Partner/admin komisyon görünürlüğü | H2,H3 |
| **Güvenlik & güven** | CSRF, fraud, audit, KVKK | K3 gate | H7 |
| **Hız & teknik** | TTFB, WebP, lazy, edge cache | Lighthouse ≥90 kamu | H1,H8 |
| **SEO & keşif** | Canonical, schema, ilçe/özellik URL | Organik landing | H9 |

---

## 2. Kullanıcı çeken özellikler (öncelikli backlog)

Rakiplerde standart, bizde eksik veya kısmi — **Wave-III+ uygulama**.

| ID | Özellik (çekim) | Rakip örnek | Bizde | Wave | Task |
|----|-----------------|-------------|-------|------|------|
| U1 | **Fiyat düşüşü bildirimi** | Booking Price Alert | yok | III | T371 |
| U2 | **Son dakika / flash deal vitrini** | Expedia Deals | kısmi kampanya | III | T372 |
| U3 | **Şeffaf toplam fiyat (vergi+dahil)** | Airbnb total before pay | kısmi checkout | III | T373 |
| U4 | **Anında rezervasyon (Instant Book)** | Airbnb IB | yok | IV | T374 |
| U5 | **Harita-öncelikli arama** (bbox, cluster) | Booking map | kısmi T329 | III | T375 |
| U6 | **Fotoğraf galeri tam ekran swipe** | Tüm devler | kısmi detay | III | T376 |
| U7 | **Misafir yorumu + foto kanıt moderasyonu** | Booking reviews | kısmi | IV | T377 |
| U8 | **Kayıtlı arama / alert** | Kayıtlı filtre e-posta | yok | IV | T378 |
| U9 | **Sadakat puanı checkout’ta kullan** | Expedia rewards | kısmi UI | IV | T379 |
| U10 | **Karşılaştırma (2–3 otel yan yana)** | Trivago compare | yok | V | T380 |
| U11 | **Ücretsiz iptal rozeti liste kartında** | Booking badge | kısmi | III | T381 |
| U12 | **“Bu fiyat X kişi bakıyor” sosyal kanıt** | Booking urgency | yok | IV | T382 |

---

## 3. Komisyon & rezervasyon taktikleri (eksikler)

| Alan | Eksik / fikir | Öncelik | Task |
|------|----------------|---------|------|
| **Partner komisyon** | Dönemsel trend grafik, tahmini payout tarihi, CSV + PDF özet | P0 | T383 |
| **Admin komisyon** | Platform GMV vs partner net, anomali (yüksek iptal) | P0 | T350 genişletme |
| **Rezervasyon** | Takvim doluluk heatmap (admin), no-show risk skoru | P1 | T384 |
| **Kampanya katılım** | ROI: katılım → rezervasyon attribution | P1 | T385 |
| **Firma B2B** | Toplu rezervasyon komisyon kırılımı, limit uyarısı | P1 | T386 |
| **Satış paneli** | Pipeline: lead → teklif → rezervasyon, komisyon projeksiyon | P2 | T387 |
| **Dinamik komisyon** | Sezon/ilçe bazlı kural motoru (stub) | P2 | T388 |

---

## 4. Mobil — tek ekran, tam alan kuralları

Tüm kamu + panellerde **MOBIL_TEK_EKRAN** standardı (`wwwroot/assets/css/shared/mobile-viewport-shell.css` hedef).

| Kural | Değer | Uygulama |
|-------|--------|----------|
| Dokunma hedefi | min 44×44px | FAB, CTA, tab, filtre chip |
| Alt sabit CTA | `100dvh` − safe-area, `position:fixed; bottom:0` | OtelDetay rezervasyon, liste filtre FAB |
| Görsel hero | min-height 40vh, `object-fit:cover` | Liste kartı, detay galeri |
| Tipografi | başlık clamp(1.1rem,4vw,1.5rem) | Kart başlık tek satır ellipsis |
| Tablo → kart | `<768px` `admin-table--cards` | Admin/partner listeler |
| Tek kolon form | input `width:100%`, gap 12px | Rezervasyon, profil |
| Boş durum | illüstrasyon + 2 CTA | Liste/harita/komisyon |
| Scroll kilidi | modal açıkken `body overflow:hidden` | Filtre drawer, galeri lightbox |

**Wave-III mobil P0 sayfalar:** OtelListeleme, OtelDetay, HaritaOteller, Partner Commissions, Partner Pricing, User Reservations.

---

## 5. Güvenlik & hız & SEO (devler seviyesi)

| P0 | İş | Task |
|----|-----|------|
| CSP enforce prod | T302 rollout tamam | H7 |
| Fraud inbox | T357 | H7+H3 |
| Core Web Vitals | preload, WebP, font-display swap | H1 T304+ |
| Structured data | Hotel, Offer, BreadcrumbList JSON-LD | H9 T389 |
| ilçe landing SEO | 39 ilçe `/{sehir}/{ilce}` meta unique | H9 T390 |

---

## 6. 30 dk dalga şablonu (Wave-III örnek)

**Wave-III-20260526-0200 — EXECUTE**

| Stream | Bu tur | Çıkış |
|--------|--------|-------|
| H1 | T375 harita bbox, T376 galeri fullscreen, T381 iptal rozeti | CSS+view |
| H2 | T383 komisyon trend, T311 SS 5 sayfa | partner |
| H3 | T350 revenue, T353–355 views, T356 bulk | admin |
| H4 | T373 şeffaf fiyat checkout copy | user |
| H7 | T357 fraud stub | security |
| H8 | T389 schema helper | backend |
| H9 | T390 ilçe meta | seo |
| H10 | K4 FE-CTO sayım, build gate | verify |

---

## 7. Canlıya hazır kapılar (değişmez)

K1 build · K2 migration · K3 güvenlik · K4 FE-CTO 151 · K5 Lighthouse · K6 SEO · K7 smoke · K8 E2E rezervasyon+komisyon.

**Güncel:** K1 ✅ · K4 ❌ (6/151) · K8 🔄

---

*Sonraki tur: `CTO_AJAN_ATAMA_KUYRUGU.md` → `wave_iii` · Plan şablonu: `PLATFORM_30DK_PLAN_SABLONU.md`*
