# Platform 30 Dakika Plan Şablonu



> Her döngü başında kopyala-doldur. Wave ID: `Wave-___-YYYYMMDD-HHMM`



---



## Meta



| Alan | Değer |

|------|--------|

| **Wave ID** | |

| **Döngü başlangıç** | YYYY-MM-DD HH:MM (+03) |

| **Döngü bitiş (hedef)** | +30 dk |

| **Sprint** | `sprint-continuous-infinite-20260523` |

| **Koordinatör** | Platform Coordinator |



---



## PLAN (0–10 dk)



### Önceki VERIFY özeti



| KPI | Değer |

|-----|--------|

| Build | pass / fail — not: |

| FE-CTO | /151 |

| K1–K8 | hangileri ✅ |



### Bu tur seçilen gap’ler (max 3)



| # | Gap (PLATFORM_OZELLIK_GAP_ANALIZI satırı) | T-ID | Owner stream |

|---|-------------------------------------------|------|--------------|

| 1 | | | |

| 2 | | | |

| 3 | | | |



### Orkestra atamaları



| Stream | T-ID listesi | Kabul kriteri (1 cümle) |

|--------|--------------|-------------------------|

| H1 | | |

| H2 | | |

| H3 | | |

| H4 | | |

| H5 | | |

| H6 | | |

| H7 | | |

| H8 | | |

| H9 | | |

| H10 | | |



### Blokajlar



- [ ] Build

- [ ] Auth test kullanıcı

- [ ] DB migration

- [ ] Diğer:



---



## EXECUTE (10–25 dk)



### Yapılan işler (kısa)



| T-ID | Durum | Kanıt (dosya/route) |

|------|--------|---------------------|

| | done / blocked | |



### DB değişikliği (varsa)



| Script | Yol |

|--------|-----|

| | `Database/MigrationsSql/...` |



---



## VERIFY (25–30 dk)



```text

dotnet build "D:\otelturizm\otelturizm.csproj" -o "D:\otelturizm\.coord-build" --no-restore

```



| Kontrol | Sonuç |

|---------|--------|

| Build 0 hata | |

| Smoke route | |

| SS üretildi | |

| FE-CTO delta | +0 / +N |



---



## Sonraki PLAN (30 dk)



| Alan | Değer |

|------|--------|

| **Sonraki Wave ID** | |

| **Öncelik P0** | |

| **Paralel kuyruk** | `queue_active_parallel` |



### Notlar (koordinatör → parent)



- 

- 



---



*Şablon — içerik her tick’te doldurulur; boş bırakılabilir alanlar kasıtlıdır.*



---



## Doldurulmuş örnek — Wave-II-20260523-0130



> Platform Coordinator · FIRST automated 30dk cycle (Wave-II)



### Meta



| Alan | Değer |

|------|--------|

| **Wave ID** | `Wave-II-20260523-0130` |

| **Döngü başlangıç** | 2026-05-23 01:30 (+03) |

| **Döngü bitiş (hedef)** | 2026-05-23 02:00 (+03) |

| **Sprint** | `sprint-continuous-infinite-20260523` |

| **Koordinatör** | Platform Coordinator |



### PLAN — P0/P1



| Öncelik | T-ID | Owner | Gap |

|---------|------|-------|-----|

| P0 | T349 | H8 | Build gate |

| P0 | T341/T342 | H8 | ORK-IST demo seed + images |

| P1 | T353 | H3 | SlowSql dedicated view |

| P1 | T350 | H3 | Revenue Command Center (sonraki) |

| P1 | T311 | H2 | Partner SS batch-1 |



### EXECUTE özeti



| T-ID | Durum | Kanıt |

|------|--------|-------|

| T349 | done | OtelListeleme Razor — build zaten 0 hata |

| T341/T342 | done | `Install-IstanbulIlceDemo.ps1` · 39× ORK-IST OK |

| T353 | done | `/admin/slow-sql` · `SlowSql.cshtml` · sidebar |



### VERIFY



| Kontrol | Sonuç |

|---------|--------|

| Build | `.coord-build` 0 hata (post-T353) |

| Sonraki wave | `Wave-III-20260523-0200` · P0: T350, T356, T357 |



---



## Doldurulmuş örnek — Wave-III-20260526-0200



> Platform Coordinator · EXECUTE turu · `Docs/PLATFORM_DUNYA_DEVLERI_YOL_HARITASI.md` mandate



### Meta



| Alan | Değer |

|------|--------|

| **Wave ID** | `Wave-III-20260526-0200` |

| **Döngü başlangıç** | 2026-05-26 02:00 (+03) |

| **Döngü bitiş (hedef)** | 2026-05-26 02:30 (+03) |

| **Sprint** | `sprint-continuous-infinite-20260523` |

| **Koordinatör** | Platform Coordinator |



### PLAN (0–10 dk)



#### Önceki VERIFY özeti



| KPI | Değer |

|-----|--------|

| Build | pass — `dotnet restore` + `build -o .coord-build` 0 hata (2026-05-26) |

| FE-CTO | 6/151 |

| K1–K8 | K1 ✅ · K4 ❌ · K8 🔄 |



#### Bu tur seçilen gap’ler (max 3)



| # | Gap (PLATFORM_OZELLIK_GAP_ANALIZI satırı) | T-ID | Owner stream |

|---|-------------------------------------------|------|--------------|

| 1 | 40 — Harita bbox cluster | T375 | H1 |

| 2 | 38 — Şeffaf toplam fiyat checkout | T373 | H4 |

| 3 | 48 — Partner komisyon trend | T383 | H2 |



#### Orkestra atamaları



| Stream | T-ID listesi | Kabul kriteri (1 cümle) |

|--------|--------------|-------------------------|

| H1 | T375, T376, T381, T372 | Harita viewport filtre + detay galeri lightbox + iptal rozeti liste kartında görünür |

| H2 | T383, T311 | Komisyon sayfasında trend + payout ETA; partner SS batch-1 path tabloda |

| H3 | T350, T353–T356, T357 | Revenue Center iskelet; SlowSql/UploadHistory view; bulk stub; fraud inbox |

| H4 | T371, T373 | Checkout vergi dahil tek satır toplam; fiyat alert kayıt stub |

| H7 | T357 | FraudAlerts.cshtml route + boş durum |

| H9 | T389, T390 | JSON-LD helper; 3 ilçe pilot landing unique meta |

| H10 | T349 | `.coord-build` 0 hata kanıtı ORKESTRA |



#### Blokajlar



- [x] Build — restore + `.coord-build` 0 hata

- [ ] Auth test kullanıcı — admin SS T310

- [ ] DB migration — yok (bu tur view/CSS)

- [ ] Diğer: FE-CTO 145 sayfa bekliyor



### EXECUTE (10–25 dk)



#### Yapılan işler (kısa)



| T-ID | Durum | Kanıt (dosya/route) |

|------|--------|---------------------|

| T349 | done | `.coord-build\otelturizm.dll` |

| T375 | assigned | `HaritaOteller` bbox plan |

| T373 | assigned | checkout total copy spec |

| T383 | assigned | `Commissions` trend partial |

| T389 | assigned | schema helper spec |

| T350 | assigned | RevenueCommandCenter route stub |



#### DB değişikliği (varsa)



| Script | Yol |

|--------|-----|

| — | Bu tur şema yok |



### VERIFY (25–30 dk)



```text

dotnet restore "D:\otelturizm\otelturizm.csproj"

dotnet build "D:\otelturizm\otelturizm.csproj" -o "D:\otelturizm\.coord-build"

```



| Kontrol | Sonuç |

|---------|--------|

| Build 0 hata | pass (16 MailKit hatası `--no-restore` ile; restore sonrası 0) |

| Smoke route | `/`, `/otel-listeleme`, `/panel/partner/komisyonlar` |

| SS üretildi | H2 T311 path tablo |

| FE-CTO delta | +0 |



### Sonraki PLAN (30 dk)



| Alan | Değer |

|------|--------|

| **Sonraki Wave ID** | `Wave-III-20260526-0230` |

| **Öncelik P0** | T376 · T381 · T390 ×3 · T357 fraud view |

| **Paralel kuyruk** | `[H1, H2, H3, H7, H9]` |



#### Notlar (koordinatör → parent)



- Gap 36–50 (U1–U12 + T383/T389/T390) güncellendi.

- `wave_iii` aktif; T371–T390 registry'de.

- Commit/deploy yok.


