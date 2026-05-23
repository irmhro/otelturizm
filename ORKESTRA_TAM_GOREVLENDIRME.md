# Orkestra tam görevlendirme — Baş mühendis onayı

**Tarih:** 2026-05-23  
**Yetki:** Kullanıcı onayı olmadan tüm `pending` görevler ekiplere atandı.  
**Kural:** Deploy/commit yok · `AGENTS.md` dar tarama · Build doğrula.

---

## Aktif sprint

`sprint_id: sprint-total-20260523-chief-engineer`  
**Paralel hatlar:** 8 orkestra · **Durum:** `assigned` → çalışıyor

| Hat | Orkestra ID | Lead ajan | Atanan T-ID | Öncelik |
|-----|-------------|-----------|-------------|---------|
| H1 | `fe-otel-public` | Frontend Ork (kamu) | T005,T006,T007,T306,T307,T304 | P0 |
| H2 | `fe-partner` | Partner FE Ork | T102,T202,T200,T201,T309,T311,T321 | P0 |
| H3 | `fe-admin` | Admin FE Ork | T101,T108,T111-T119,T210,T310,T322 | P1 |
| H4 | `fe-user` | User FE Ork | T103,T120-T129,T230 | P1 |
| H5 | `fe-satis` | Satış FE Ork | T104,T130-T139 | P2 |
| H6 | `fe-firma` | Firma FE Ork | T220,T140-T146 | P2 |
| H7 | `ork-guvenlik` | Security Ork | T004,T301,T302,T149,T320 | P0 |
| H8 | `ork-backend` | DB+Services Ork | T308,T313,T107,T303,T305,T148 | P1 |
| H9 | `ork-seo-perf` | SEO Ork | T148,T305 (paylaşımlı H8) | P1 |
| H10 | `master-cto` | Master CTO | T150,T250,T314,T325 | Son kapı |

**Wave B/C:** `grup-02/03/05` — H8 ile birlikte smoke; upstream DB ✅.

---

## T-ID → ekip (tam liste)

| T-ID | Ekip | Durum | Çıktı tanımı |
|------|------|-------|--------------|
| T004 | H7 | done | Login CSRF + panel POST (User 13 düzeltme) |
| T005-T007 | H1 | assigned | Liste/harita/detay SS + FE-CTO notu |
| T101 | H3 | assigned | Admin dashboard auth SS |
| T102 | H2 | assigned | Partner dashboard SS |
| T103 | H4 | assigned | User profil/rezervasyon mobil |
| T104 | H5 | assigned | Satış shell mobil |
| T107 | H8 | assigned | PII maskeleme grep + fix |
| T108 | H3 | assigned | Admin iç sayfa batch 1 |
| T111-T119 | H3 | assigned | Admin SS döngüsü (10 sayfa/sprint) |
| T120-T129 | H4 | assigned | User panel SS |
| T130-T139 | H5 | assigned | Satış panel SS |
| T140-T146 | H6 | assigned | Departman SS |
| T148 | H9 | assigned | Liste canonical/noindex |
| T149 | H7 | done | CSP prod path + OTELTURIZM_CSP_ENFORCE |
| T150 | H10 | assigned | FE-CTO 151 kapısı (koşullu) |
| T200-T202 | H2 | assigned | Partner kampanya/fiyat/komisyon |
| T210 | H3 | assigned | Admin komisyon SS |
| T220 | H6 | assigned | Firma rezervasyon E2E |
| T230 | H4 | assigned | User fatura SS |
| T250 | H10 | assigned | Tamamlanma % güncelle |
| T301-T302 | H7 | done | CSRF audit + CSP dokümantasyon |
| T303 | H8 | assigned | Upload WebP policy |
| T304 | H1 | assigned | Lighthouse kamu 3lü |
| T305 | H9 | assigned | Anasayfa etiket URL |
| T306-T307 | H1 | assigned | OtelDetay + anasayfa pill |
| T308 | H8 | assigned | 10 İstanbul demo otel seed |
| T309 | H2 | assigned | Partner komisyon tablo+KPI |
| T310-T312 | H3/H2/H4 | assigned | Panel SS envanter |
| T313 | H8 | assigned | Adres ID E2E |
| T314 | H10 | assigned | Canlıya hazır kapıları |
| T320 | H7 | done | Paket başvuru audit (mevcut) |
| T321-T322 | H2/H3 | assigned | Paket SS admin/partner |
| T323-T325 | H8/H10 | assigned | Medya Faz2 / onay merkezi |

---

## Admin auth test kullanıcı (T101 / T310 — H3)

| Gereksinim | Kaynak |
|------------|--------|
| Giriş URL | `/admin-giris` |
| Hesap tipi | `AuthClaimTypes.AccountType=admin` veya `UserRole=admin` |
| RBAC | `20260522_seed_admin_yetkiler.sql` → rol `platform_admin_full` |
| Kullanıcı–rol | `20260523_seed_admin_demo_kullanici.sql` → `ork-demo-admin@otelturizm.local` + `platform_admin_full` (`Docs/ADMIN_TEST_KULLANICI.md`) |
| SS hedefi | Dashboard + SystemHealth, ardından T111–T115 batch |

**Not:** Partner demo (`ork-demo-partner@otelturizm.local`) admin için geçersiz; admin demo: `ork-demo-admin@otelturizm.local` / `Demo123!`.

---

## Doğrulama (her hat bitince)

1. `dotnet build` 0 hata  
2. İlgili MD satırı `done` + kısa kanıt  
3. `CTO_AJAN_ATAMA_KUYRUGU.md` güncelle  

---

*Bu dosya `CTO_AJAN_ATAMA_KUYRUGU.md` ile senkron tutulur.*
