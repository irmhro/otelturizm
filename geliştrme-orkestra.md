# Geliştirme Orkestra Günlüğü

**Sprint:** `sprint-1ay-orkestra-20260523` (+1 ay · 1440×10dk)  
**Koordinatör:** Platform Coordinator · **CTO orkestralar:** H1–H14  
**Döngü:** 10 dk plan dalgası (Cursor sohbet/subagent) — **Politika:** onaysız atama · commit/deploy yok · **Yasak:** terminal `AGENT_LOOP_TICK_*` yalnızca `Write-Output` döngüsü (kod yazmaz)  

> Bu dosya her dalga sonrası **sıralı** güncellenir. Özet dashboard: [`geliştirme.md`](geliştirme.md)

---

## Sıralı log (#001–#043)

| # | Tarih | Wave | Owner | Geliştirme | Durum |
|---|--------|------|-------|------------|--------|
| 001 | 2026-05-23 | Wave-I | H10 | Sürekli geliştirme döngüsü + koordinatör operasyon planı | ✅ |
| 002 | 2026-05-23 | Wave-I | H10 | `Docs/PLATFORM_OZELLIK_GAP_ANALIZI.md` (50+ gap satırı) | ✅ |
| 003 | 2026-05-23 | Wave-I | H3 | `Docs/ADMIN_PANEL_MASTER_ROADMAP.md` master yol haritası | ✅ |
| 004 | 2026-05-23 | Wave-II | H8 | `20260526_seed_istanbul_ilce_oteller_tam.sql` — 39 ilçe demo otel | ✅ |
| 005 | 2026-05-23 | Wave-II | H8 | `20260526_seed_istanbul_ilce_medya_ozellikleri.sql` | ✅ |
| 006 | 2026-05-23 | Wave-II | H8 | `tools/Db/Install-IstanbulIlceDemo.ps1` + DemoImageSeed genişletme | ✅ |
| 007 | 2026-05-23 | Wave-II | H8 | `20260526_fix_yayin_onay_unicode.sql` + HotelService PublishStatusSql | ✅ |
| 008 | 2026-05-23 | Wave-III | H1 | Otel listeleme RZ1010, harita cluster, boş durumlar | ✅ |
| 009 | 2026-05-23 | Wave-III | H1 | OtelDetay mobil galeri + `mobile-viewport-shell.css` | ✅ |
| 010 | 2026-05-23 | Wave-III | H1 | Yemek filtreleri `?etiket=kahvalti-dahil` + özellik seed | ✅ |
| 011 | 2026-05-24 | Wave-IV | H3 | T350 Admin Gelir Merkezi `/admin/gelir-merkezi` | ✅ |
| 012 | 2026-05-24 | Wave-IV | H3 | T356 Toplu otel yayın `/admin/oteller/toplu-yayin` | ✅ |
| 013 | 2026-05-24 | Wave-IV | H3 | T353 Yavaş SQL monitör + dashboard GMV widget | ✅ |
| 014 | 2026-05-24 | Wave-IV | H8 | `20260523_seed_admin_demo_kullanici.sql` + admin test doc | ✅ |
| 015 | 2026-05-24 | Wave-V | H7 | T398 SalesPanel policy + ReturnUrl + satış demo seed | ✅ |
| 016 | 2026-05-24 | Wave-V | H7 | Partner/firma kayıt TempData düzeltmesi | ✅ |
| 017 | 2026-05-25 | Wave-V | H11 | Komisyon tahsilat migration `20260526_komisyon_tahsilat_alanlari.sql` | ✅ |
| 018 | 2026-05-25 | Wave-V | H11 | Admin `/admin/komisyon-tahsilat` + CSV + bulk ödendi | ✅ |
| 019 | 2026-05-25 | Wave-V | H11 | Partner komisyon ay filtresi + CSV export | ✅ |
| 020 | 2026-05-25 | Wave-VI | H4 | User panel mobil master — `user-mobile-bundle.css` + `.mobile.css` | ✅ |
| 021 | 2026-05-25 | Wave-VI | H4 | Tablo → kart `data-label`, safe-area, 44px touch | ✅ |
| 022 | 2026-05-25 | Wave-VII | H12 | User `/panel/user/faturalarim` mobil + PDF önizleme | ✅ |
| 023 | 2026-05-25 | Wave-VII | H12 | Partner misafir faturaları upload UX + mobile CSS | ✅ |
| 024 | 2026-05-26 | Wave-VIII | H13 | SharedResources tr/en/de — 49 key + dil değiştirici | ✅ |
| 025 | 2026-05-26 | Wave-VIII | H9 | InternationalSeoService + `/en/hotels` `/de/hotels` hreflang | ✅ |
| 026 | 2026-05-26 | Wave-VIII | H14 | `_EmailMaster.cshtml` + EmailTemplateService 7 dil | ✅ |
| 027 | 2026-05-26 | Wave-IX | H9 | `wwwroot/sitemap-en.xml` + layout hreflang path | ✅ |
| 028 | 2026-05-26 | Wave-X | H9 | `InternationalSeoPaths` + `HotelListingSeo` canonical/robots | ✅ |
| 029 | 2026-05-26 | Wave-X | H9 | OtellerController 7 locale route (tr/en/de/fr/es/ru/ar) | ✅ |
| 030 | 2026-05-23 | Wave-XI-A | H13 | `SharedResources.fr/es.resx` + 44 layout SharedLocalizer | ✅ |
| 031 | 2026-05-23 | Wave-XI-B | H4+H2 | FE-CTO batch: 4 user + 10 partner route/SS planı | ✅ |
| 032 | 2026-05-23 | Wave-XI-B | H4 | `Docs/ORKESTRA_PANEL_SS_BATCH.md` Wave-XI güncelleme | ✅ |
| 033 | 2026-05-23 | Wave-XII | H11/H8 | `Docs/KOMISYON_TAHSILAT_MERKEZI_PLANI.md` § apply runbook (yedek, sıra, doğrulama, rollback) | ✅ |
| 034 | 2026-05-23 | Wave-XII | H13 | `SharedResources.ar.resx` + `SharedResources.ru.resx` (49 key) · layout SharedLocalizer doğrulandı | ✅ |
| 035 | 2026-05-23 | Wave-XIII | H1 | Otel listeleme konsept bar + `?etiket=` wiring (7 konsept + kampanyalar linki) | ✅ |
| 036 | 2026-05-23 | Wave-XIII | H1 | Liste kartı indirim köşe rozeti + mobil fiyat strikethrough (köşe audit) | ✅ |
| 037 | 2026-05-23 | Wave-XIII | H1 | Harita/detay/kampanya mobil+desktop CSS · `PageCss` wiring · `Docs/H1_SABLON_REFERANS.md` | ✅ |
| 038 | 2026-05-23 | Wave-XIII | H1 | Mobil harita FAB, filtre drawer safe-area, kampanya index hero/stat chips | ✅ |
| 039 | 2026-05-23 | Wave-XIV | H1 | Kamu kart geçişleri `--transition-base` (liste, kampanya hero, harita geri, detay token) | ✅ |
| 040 | 2026-05-23 | Wave-XIV | H4 | `panel-form-ux.css` — sil/düzenle/yükle ortak desen; partner misafir faturaları pilot | ✅ |
| 041 | 2026-05-23 | Wave-XIV | H1 | Liste sadakat rozeti (`EstimatedLoyaltyPoints`) + `data-loyalty-hook` placeholder | ✅ |
| 042 | 2026-05-23 | Wave-XIV | H1 | `LoyaltyPointsEstimator` + `ApplyListingLoyaltyTouchpoints` controller wiring | ✅ |
| 043 | 2026-05-23 | Wave-XIV | H10 | `dotnet build -o .coord-build-xiv` — 0 hata gate | ✅ |
| 044 | 2026-05-23 | Wave-XVI | H4 | `panel-form-ux` — Admin Kampanyalar aktif/pasif toggle deseni | ✅ |
| 045 | 2026-05-23 | Wave-XVI | H4 | `panel-form-ux` — Admin Oteller toplu yayın + satır düzenle/yayın aksiyonları | ✅ |
| 046 | 2026-05-23 | Wave-XVI | H1 | `/havuzlu-oteller` → `?etiket=havuzlu-oteller` (`KonseptOtelLandingController`) | ✅ |
| 047 | 2026-05-23 | Wave-XVI | H14 | E-posta Faz2: `tr/Rezervasyon_Talebi_Alindi` → `_EmailMaster` | ✅ |
| 048 | 2026-05-23 | Wave-XVI | H14 | E-posta Faz2: `tr/Sifre_Sifirlama_Talebi` → `_EmailMaster` | ✅ |
| 049 | 2026-05-23 | Wave-XVI | H10 | `dotnet build -o .coord-build-xvi` — 0 hata gate | ✅ |
| 050 | 2026-05-23 | Wave-XVI | H10 | Orkestra dokümanları (#050+) + köşe audit satırı güncellendi | ✅ |
| 051 | 2026-05-23 | Wave-XVII | H8 | Yerel DB tam migration (sqlcmd): 179 script OK · 2 idempotent fail · 39 yayında otel | ✅ |
| 053 | 2026-05-23 | P0 | H9+H13+H1 | **urgent-fix:** path-locked culture (no cookie/Accept-Language ar drift); `AddLocalization()` ResourcesPath düzeltmesi (`Nav.Hotels` → Oteller); dil değiştirici path prefix; liste/harita kart URL path-based; firma/kurumsal header menü | ✅ |
| 054 | 2026-05-24 | P0 | H10 | **Geçici erişim perdesi kaldırıldı:** `DevelopmentGate:Enabled=false` (appsettings); overlay/CSS/JS yalnızca flag açıkken yüklenir; `/gelisim` dev sayfası korundu | ✅ |
| 055 | 2026-05-24 | Wave-XVII | H1 | OtelDetay galeri: ok gezinme, klavye ←/→, `data-slayt-lightbox`, mobil swipe hint, Inter/clamp tipografi | ✅ |
| 056 | 2026-05-24 | Wave-XVII | H1 | Otel listeleme: kart görsel hover zoom + `clamp()` font hiyerarşisi (`otel-listeleme.css`) | ✅ |
| 057 | 2026-05-24 | Wave-XVII | H1 | `/hafta-sonu-firsatlari` + `/evcil-hayvan-dostu-oteller` kalıcı yönlendirme (`KonseptOtelLandingController`) | ✅ |
| 058 | 2026-05-24 | Wave-XVII | H4+H10 | `panel-form-ux` partner Photos upload zone pilot + `dotnet build -o .coord-build-xvii` gate | ✅ |
| 065 | 2026-05-24 | P0 | H10+H13 | **Canlı HTTP 500:** Razor `_ViewImports`/`_Layout`/`Anasayfa` eksik using + SharedLocalizer tip; footer yasal seed + `gizlilik`/`sozlesme` CSS · `Docs/CANLI_500_KOK_NEDEN.md` | ✅ |
| 066 | 2026-05-24 | Wave-XVIII | H1 | Şirket footer sayfaları: `/hakkimizda`, `/kariyer`, `/basin-odasi`, `/blog` + CSS çiftleri + i18n + yardım-merkezi 301 | ✅ |
| 067 | 2026-05-24 | Wave-A1 | H3 | `Docs/ADMIN_PANEL_TAM_YAPILANDIRMA.md` — admin route/gap matris + komisyon/otel/evrak akışları | ✅ |
| 068 | 2026-05-24 | Wave-A1 | H16 | `Docs/PLATFORM_SOZLESME_HUKUK_ORKESTRA.md` + `20260524_seed_platform_sozlesmeler.sql` | ✅ |
| 069 | 2026-05-24 | Wave-A1 | H3 | Admin `/admin/partner-evraklari` — evrak kuyruğu, checklist, onay/red, mobil CSS | ✅ |
| 070 | 2026-05-24 | Wave-A1 | H3 | Onay Merkezi T356 CTA + evrak link + mobil aksiyon UX | ✅ |
| 071 | 2026-05-24 | Wave-A1 | H3 | Komisyon tahsilat yaşam döngüsü bar + mutabakat rapor linki | ✅ |
| 072 | 2026-05-24 | Wave-A1 | H10 | `dotnet build -o .coord-build-admin` gate | ⏳ |
| 075 | 2026-05-25 | P0 | H9+H13 | **ar-remove-i18n-fix:** `AddLocalization()` ResourcesPath kaldırıldı (raw key); Arapça route/UI/RTL kaldırıldı; `/ar/*`→TR 301; varsayılan tr-TR LTR | ✅ |
| 076 | 2026-05-25 | Wave-XIX | H1 | Anasayfa kategori swiper mobil tam alan kart (`home-index.mobile.css`) | ✅ |
| 079 | 2026-05-25 | Wave-Travel-Plan | H1 | `/seyahat-planlama` tam sayfa: rota, bütçe, hafta sonu, kampanya DB, OtelPuan CTA · `Docs/SEYAHAT_PLANLAMA_OZELLIK_PLANI.md` | ✅ |
| 077 | 2026-05-25 | P0 | H1+H13 | **mobile-drawer-fix:** drawer i18n `.Value`, TR `/oteller` linkleri, Arapça dil kaldırıldı, tek sütun mobil CSS + safe-area · `dotnet build -o .coord-build-drawer` | ✅ |
| 081 | 2026-05-25 | P0 | H1+H13 | **listing-i18n-ux:** `/oteller` path-locked TR UI; tek satır sonuç (`Tüm bölgeler · N otel bulundu`); Listing.* resx 7 dil; mobil header/konsept/kart CTA · `dotnet build -o .coord-build-listing-i18n` | ✅ |
| 082 | 2026-05-25 | P0 | Koord | **AGENT_LOOP_TICK_platform_coord KAPALI:** terminal 703446 ~35h sonra durdu (exit 4294967295); kullanıcı talebi — yeniden başlatılmayacak; `AGENT_LOOP_HOURLY_git_sync` / `Invoke-HourlyGitSync.ps1` aynı | ✓ |
| 084 | 2026-05-25 | P0 | H1 | **otel-detay-roomsCard:** Detail.* i18n `.Value` + 7 resx; oda görseli fallback/onerror; `#roomsCard` mobil+desktop CSS; `mobileBookingBar` CTA; JS seçili oda i18n · `dotnet build -o .coord-build-rooms-card` | ✅ |
| 087 | 2026-05-25 | P0 | H1 | **otel-detay-benchmark:** 5 OTA kıyas (galeri/chip/oda/yorum/harita/sticky); full-bleed mobil galeri; `detail-hero-head` puan chip; sidebar inline; 10 Detail.* i18n · `dotnet build -o .coord-build-otel-detay-benchmark` | ✅ |
| 083 | 2026-05-25 | Wave-XX | H1 | **confirm-modal-premium:** `#reservationConfirmModal` bottom sheet mobil, ikonlu kartlar, toplam, i18n · `otel-detay.css` / `.mobile.css` | ✅ |

**Toplam tamamlanan teslimat:** **70** (Wave-I → Wave-A1 + #053–#058, #065–#071, #075–#077, #076, #079, #083–#084; #072 build bekliyor)

---

## Dalga detayları (son 5)

### Wave-XVII — Galeri polish + konsept landing + panel upload (2026-05-24)

- **H1:** OtelDetay galeri ok/klavye/swipe/lightbox hook; liste kart hover + tipografi  
- **H1:** `/hafta-sonu-firsatlari`, `/evcil-hayvan-dostu-oteller` SEO URL  
- **H4:** Partner Photos `panel-form-ux-upload*` pilot  
- **H10:** `.coord-build-xvii` build gate  
- **Plan:** [`PLATFORM_1AY_ORKESTRA_PLAN.md`](PLATFORM_1AY_ORKESTRA_PLAN.md) oluşturuldu  

### Wave-XVI — Panel form + konsept landing + e-posta Faz2 (2026-05-23)

- **H4:** `panel-form-ux` admin Kampanyalar + Oteller (bulk bar, düzenle/sil toggle)  
- **H1:** `/havuzlu-oteller` kalıcı yönlendirme  
- **H14:** 2 tr şablon master layout (`Rezervasyon_Talebi_Alindi`, `Sifre_Sifirlama_Talebi`)  
- **H10:** `.coord-build-xvi` build gate  

### Wave-XIV — Geçişler + sadakat + panel form UX (2026-05-23)

- **H1:** `--transition-base` liste/kampanya/harita/detay; liste `+N puan` / giriş hook  
- **H4:** `panel-form-ux.css` partner misafir faturaları pilot + admin/partner layout link  
- **H10:** `.coord-build-xiv` build gate  

### Wave-XII — Komisyon runbook + i18n Faz3 (2026-05-23)

- **H11/H8:** Komisyon tahsilat migration apply runbook (idempotent script sırası)  
- **H13:** ar/ru resx 49 anahtar; `_AnasayfaHeader` / `_Layout` nav-footer zaten `SharedLocalizer`  
- **Build:** `.coord-build` gate  

### Wave-XI — FE-CTO + i18n Faz2 (2026-05-23)

- **H13:** fr/es resx, header/footer/search i18n, locale-aware otel linkleri  
- **H4/H2:** 14 hedef SS path dokümante; PNG henüz yok  
- **Build:** 0 hata  

### Wave-X — SEO entegrasyon (2026-05-26)

- `Utils/HotelListingSeo.cs` ↔ `InternationalSeoPaths`  
- OtellerController meta + hreflang  
- `ORKESTRA_DURUM_KONTROL.md` + `PLATFORM_10DK_PLAN_SABLONU.md` güncellendi  

### Wave-VIII / IX — i18n + SEO + Email Faz1

- 49 key × 3 dil (Faz1), e-posta master, sitemap-en  

### Wave-V / VI — Komisyon + User mobil

- Tahsilat merkezi uçtan uca Faz1  
- User panel mobil standart  

### Wave-II / III — Demo + Kamu UI

- 39 İstanbul ilçe otel  
- Liste/harita/detay mobil polish  

---

## Sırada (Wave-XIII+ kuyruk)

| # | Planlanan | Owner | Hedef |
|---|-----------|-------|--------|
| 039 | E-posta Faz2 kalan şablonlar | H14 | Master layout’a taşıma |
| 040 | FE-CTO PNG batch-1 | H4 | 4 user desktop/mobil SS |
| 041 | Partner SS batch-1 PNG | H2 | T311 10 sayfa |
| 042 | Auth E2E smoke tablosu | H7 | Tüm paneller PASS |
| 043 | Eksik konsept landing HTML | H1 | `eksik sayfalar kodlanacak` — havuzlu ✅; hafta sonu/evcil sırada |
| 051 | E-posta Faz2 kalan şablonlar (7 dil) | H14 | `Rezervasyon_Reddedildi`, `Giris_Guvenlik_Kodu`, … |
| 052 | `panel-form-ux` partner upload sayfaları | H4/H8 | fotograflar, oda medya |
| 053 | SEO sitemap fr/es/ar/ru | H9 | `sitemap-*.xml` + hreflang doğrulama |

---

## Orkestra CTO haritası

| ID | Orkestra | Son durum |
|----|----------|-----------|
| H1 | fe-otel-public | verify — SEO path entegre |
| H2 | fe-partner | assigned — T311 SS |
| H3 | fe-admin | done — T350/T356 |
| H4 | fe-user | assigned — FE-CTO PNG |
| H5 | fe-satis | done |
| H6 | fe-firma | in_progress — Wave-F1 P0 (`FIRMA_PANEL_MASTER_PLAN`) |
| H7 | ork-guvenlik | done — E2E pending |
| H8 | ork-backend | verify — runbook doc ✅ |
| H9 | ork-seo | done — Faz2 route |
| H10 | master-cto | verify — build gate |
| H11 | finans | done — apply runbook ✅ |
| H12 | fatura | done Faz1 |
| H13 | i18n | Faz3 ar/ru done — panel string backlog |
| H14 | email | verify — Faz2: 6/∞ master (tr rezervasyon×3 + fatura×2 + şifre) |
| H15 | fe-world-standard | active — Wave-XVIII kamu P0 ✅ |

---

## Güncelleme kuralı

Her 10 dk dalga bitince:

1. Bu dosyaya yeni satır (#NNN) ekle  
2. [`geliştirme.md`](geliştirme.md) KPI tablosunu güncelle  
3. `ORKESTRA_DURUM_KONTROL.md` snapshot  

*Son otomatik dalga: #076 home-card-mobile-full · Sonraki: #075 partner evrak SS*

### #083 — OtelDetay rezervasyon onay modalı premium (2026-05-25)

- **FE:** `#reservationConfirmModal` — mobil bottom sheet, desktop ~520px dialog, ikonlu özet kartları, toplam satırı, 44px CTA
- **i18n:** `Detail.ConfirmModal.*` SharedLocalizer + `reservationConfirmI18nJson` JS köprüsü
- **CSS:** `otel-detay.css`, `otel-detay.mobile.css` — fe-world-tokens, blur backdrop, açılış animasyonu
- **Build:** `dotnet build -o .coord-build-confirm-modal` — 0 hata hedef

### #076 — home-card-mobile-full (2026-05-25)

- **Görünür FE:** Anasayfa kategori swiper — `.card-content` tam genişlik (safe-area), fiyat/indirim hiyerarşisi, puan rozeti sağ alt
- **CSS:** `home-index.mobile.css` — `@media (max-width: 768px)` yalnızca `.home-category-section`
- **JS:** `home-category-swiper` mobil `spaceBetween: 12`; desktop 16 korundu (640+ breakpoint)
- **Build:** `dotnet build` — 0 hata hedef

### #074 — Wave-XIX partner fatura mobil (2026-05-24)

- **Görünür FE:** `Invoices.cshtml` — `partner-invoices-table--cards`, misafir fatura banner, `invoices.css` + `invoices.mobile.css`
- **Upload UX:** `GuestInvoices.cshtml` — `panel-form-ux-upload` sürükle-bırak (`is-dragover`)
- **SS path:** `Docs/frontend-screenshots/fe-partner-panel/faturalar/README.md` (PNG bekleniyor)
- **Plan:** `PLATFORM_10DK_SONRAKI_DALGALAR.md` — döngü 13–24 eklendi
- **Build:** `dotnet build -o .coord-build-xix` — 0 hata hedef
- **Chat:** İlerleme `geliştrme-orkestra.md` + `geliştirme.md` başlığında; canlı için tam publish şart

### #073 — deploy-gap + görünür FE (2026-05-24)

- **Deploy doc:** `Docs/DEPLOY_ACIL_500_VE_GORUNUR_GELISTIRME.md` — neden canlıda görünmüyor, PowerShell publish, SQL sırası, smoke URL, Ctrl+F5
- **Görünür FE:** `OtelListeleme.cshtml` — `listing-result-badge` (toplam tesis), boş liste CTA → `/oteller`, `fe-world-tokens` (`IncludeFeWorldTokens`)
- **Plan:** `PLATFORM_10DK_SONRAKI_DALGALAR.md` — sonraki 12 × 10 dk dalga
- **Build/publish:** `dotnet build -c Release -o .coord-build-deploy-ready` · `dotnet publish -c Release -o publish/deploy-ready`
- **Dürüst:** Repoda #053–#072 çoğu dalga **deploy edilmeden** canlıda yok; 500 kök nedeni #063/#065 (Razor i18n) — tam DLL publish şart

### #066 — H6 firma panel Wave-F1 (2026-05-24)

- **Doc:** `Docs/FIRMA_PANEL_MASTER_PLAN.md` — route envanteri, rezervasyon E2E, F1–F5
- **Kuyruk:** `H6_fe_firma` → `in_progress`, priority P0
- **FE:** `firma-table--cards` + `data-label`; `panel-form-ux` (layout + çalışan/rezervasyon formları); CreateReservation personel + misafir kapasitesi + kurumsal fiyat rozeti; Deals `roomTypeId` deep link; tema offcanvas
- **Build:** `dotnet build -o .coord-build-firma` — 0 hata

### #063 — prod-500-fix (2026-05-24)

- **Kök neden:** `_Layout` + Razor runtime i18n (`SharedLocalizer`); anasayfa `Layout=null` olduğu için 200, `/oteller` ve giriş sayfaları 500
- **Fix:** Production’da runtime compile kapalı; `ResourcesPath`; culture provider fail-safe; `/Oteller`→`/oteller` 301; layout draft SQL try/catch
- **Doc:** `Docs/CANLI_500_KOK_NEDEN.md` deploy checklist
- **Build:** `dotnet build -o .coord-build-prodfix` 0 hata hedef

### #059 — fe-world-orchestra charter (2026-05-24)

- **Doc:** `ORKESTRA_FE_DUNYA_STANDARDI.md` — 151 sayfa mission, H15 stream, i18n checklist, 30-day waves, quality gate
- **Kuyruk:** `CTO_AJAN_ATAMA_KUYRUGU.md` — `H15_fe_world_standard` active, parallel H1–H14
- **KPI:** `geliştirme.md` — FE Dünya Standardı Orkestrası bölümü, Wave-XVIII aktif dalga

### #060 — Wave-XVIII kamu P0 CSHTML (2026-05-24)

- **Liste:** `OtelListeleme.cshtml` — hero search summary bar, 16/10 kart görseli, rating row, loyalty badge, price-actions-row
- **Detay:** `OtelDetay.cshtml` — review teaser block, sticky booking bar safe-area + i18n
- **Kampanya:** `Kampanyalar/Index.cshtml` — template hero + countdown timer + stats bar
- **CSS:** `fe-world-tokens.css` + `_Layout.cshtml` import; `kampanyalar.css` hero timer

### #061 — Wave-XVIII i18n (2026-05-24)

- **Header/Footer:** `_AnasayfaHeader`, `_AnasayfaFooter` — SharedLocalizer tüm görünür nav metinleri
- **Resx:** 42 yeni anahtar × 7 kültür (`SharedResources*.resx`)
- **Registry:** `Docs/I18N_KEY_REGISTRY.md` Wave-XVIII tablosu

### #062 — Wave-XVIII build gate (2026-05-24)

- **Build:** `dotnet build -o .coord-build-xviii` — 0 hata hedef
- **Olgunluk:** Kamu otel 58%→62%, kampanya 52%→56% (`geliştirme.md`)

### #051 — demo-full-stack (2026-05-23)

- **DB:** `20260526_fix_yayin_onay_unicode.sql` (`Yayında` = `Yay`+U+0131+`nda`), `20260523_ensure_demo_hotels_published.sql`, `20260523_seed_demo_oda_fiyat_kampanya.sql` (90 gun fiyat, 2 oda, havuz/wifi/kahvalti, kampanya `Aktif`)
- **Kod:** `CampaignService` — `PublishStatusSql` / `ApprovalStatusSql` / `KATILIM_DURUMU` (`Aktif`|`Onaylandi`) HotelService ile hizalandi
- **Dogrulama:** 39 yayinda ORK otel, kampanya `/kampanyalar` otel sayaci > 0 (`KMP-2026-SEHIR`)
- **Doc:** `Docs/ISTANBUL_ILCE_DEMO_KURULUM.md` verify sorgulari guncellendi

### #052 - publish+images local (2026-05-23)

- **SQL:** 20260523_ensure_demo_hotels_published.sql, 20260526_fix_yayin_onay_unicode.sql, 20260523_fix_orkestra_demo_yayin_onay.sql (sqlcmd -I); ek UPDATE ILCE/ORK-SEED/irmhro0 demo -> Yayinda+Onaylandi (10 satir)
- **Gorsel:** Install-IstanbulIlceDemo.ps1, Install-DemoHotelMedia.ps1, DemoImageSeed (--root); 39 otel, 312 gorsel indir/guncelle; wwwroot ~366 dosya
- **Dogrulama (LocalDB otelturizm_2026db):** 39 yayinda otel, 39 otel_gorsel; ornek https://localhost:7223/uploads/images/13/hotel/demo-cover.webp
- **Not:** --run-sql-migrations bir scriptte ILCE/ILLER FK DELETE hatasi (mevcut otel verisi); publish scriptleri uygulandi

### #077 - AGENT_LOOP_TICK devre disi (2026-05-25)

- **Sorun:** Cursor terminalinde ad-hoc pwsh: while ($true) { Start-Sleep -Seconds 600; Write-Output 'AGENT_LOOP_TICK_platform_coord {...}' } — yalnizca plan/prompt metni; Cursor ajan cagrisi veya kaynak dosya degisikligi yok.
- **Komut ornegi:** Write-Output + Start-Sleep -Seconds 600 (10 dk); ayri 5 dk dongu Start-Sleep -Seconds 300 (pid 10144) ayni anti-pattern.
- **Repo:** 	ools/ altinda platform_coord 10 dk scripti yok; saatlik 	ools/Git/Invoke-HourlyGitSync.ps1 farkli is (git snapshot).
- **Aksiyon:** Calisan AGENT_LOOP_TICK_platform_coord surecleri durduruldu (orn. pid 24456).
- **Bundan sonra:** Dalga gelistirme **Cursor sohbet + subagent** ile; PLATFORM_10DK_PLAN_SABLONU / PLATFORM_10DK_SONRAKI_DALGALAR guncellemesi ve kod degisikligi terminal tick ile otomatiklenmez.




### #083 — Otel Detay mobil rebuild + i18n (2026-05-25)

- **i18n:** `Detail.BackToHotels`, `Detail.Booking.Total`, `Detail.Booking.GoToReserve` ve oda/rezervasyon anahtarları 7× `SharedResources*.resx`; `OtelDetay.cshtml` → `.Value`
- **CSS:** `otel-detay.mobile.css` sıfırdan (≤900px): tam genişlik grid, full-bleed galeri, sticky CTA safe-area, `#roomsCard` tek sütun
- **Desktop fix:** `otel-detay.css` — iki sütunlu `.detail-grid` yalnız `@media (min-width: 901px)` (mobilde ~230px sıkışma kök nedeni)
- **Build:** `dotnet build -o .coord-build-otel-detay-rebuild` — 0 hata hedef
- **Kullanıcı:** `dotnet run --launch-profile https` yeniden başlat + mobil viewport Ctrl+F5

### #084 — Otel Detay mobil CTA i18n hotfix (2026-05-25)

- **Kök neden:** `#mobileBookingBar` ve breadcrumb `SharedLocalizer["Detail.*"]` kullanıyordu; anahtarlar `SharedResources.resx` + uydu `.resx` dosyalarında eksik/henüz derlenmemişti → kaynak bulunamayınca ham key (`Detail.Booking.Total` vb.) render edildi.
- **Düzeltme:** Tüm `Detail.*` / `Detail.ConfirmModal.*` anahtarları 7× `SharedResources*.resx` (TR: Otellere Dön, Toplam, Rezervasyona Git); `OtelDetay.cshtml` tüm `SharedLocalizer` → `.Value`; `OtellerController.OtelDetay` → `ApplyRouteListingCulture()` action başında (listeleme ile aynı).
- **Build:** `dotnet build -o .coord-build-detail-i18n` — 0 hata
- **Kullanıcı:** `dotnet run --launch-profile https` yeniden başlat + otel detay sayfasında mobil viewport Ctrl+F5


- **Durum:** `AGENT_LOOP_TICK_platform_coord` (terminal 703446) ~35 saat sonra durdu; exit_code **4294967295**; döngü yalnızca `Start-Sleep 600` + `Write-Output` idi (ajan/kod yok).
- **Kullanıcı talebi:** Bu tick döngüsü **yeniden başlatılmayacak**.
- **Devam eden:** `AGENT_LOOP_HOURLY_git_sync` (3600 sn) — `tools/Git/Invoke-HourlyGitSync.ps1` değişmedi.
- **Referans:** #077 aynı anti-pattern; bu kayıt kullanıcı onayı ile kalıcı kapatma.

### #087 — Otel Detay global benchmark mobil+desktop (2026-05-25)

- **Kıyas:** Booking.com, Expedia, Hotels.com, Agoda, Trip.com/Trivago — galeri swipe/dots, başlık+puan chip, sticky CTA, oda kartı (görsel üst + pill + CTA), yorum barları, harita z-index, tam viewport genişlik
- **CSHTML:** `detail-hero-head` + `detail-rating-chip`; mobil galeri tek görsel; `SharedLocalizer` → `.Value` (About/Location/Reviews/Gallery)
- **CSS:** `otel-detay.mobile.css` full-bleed galeri, sidebar grid gizle + inline, `#roomsCard`/`#reviewsSection` mobil; `otel-detay.css` hero chip + ≤900 tabler grid reset
- **i18n:** `Detail.About.Title`, `Detail.Location.*`, `Detail.Reviews.*`, `Detail.Gallery.SwipeHint`, `Detail.Rating.*` → 7× resx
- **Build:** `dotnet build -o .coord-build-otel-detay-benchmark`

### #085 — Buton i18n taraması (2026-05-25)

- **Tarama:** 362 `Views/**/*.cshtml`; 14 dosyada **51 buton/link/aria-label** düzeltmesi
- **Kök neden:** `@SharedLocalizer["Key"]` `.Value` olmadan render → ham anahtar veya `LocalizedString` nesnesi
- **Yeni anahtarlar (16):** `Detail.ConfirmModal.*` (11), `Listing.ClearAllFilters`, `Listing.ShowAllHotels`, `Listing.SearchOnMap`, `Btn.Filter`, `Btn.Clear` → 7× `SharedResources*.resx`
- **Dosyalar:** `_AnasayfaHeader/Footer`, `OtelListeleme`, `SeyahatPlanlama/Index`, Kurumsal (BasinOdasi/Blog/BlogDetay/Hakkimizda/Kariyer), `Kampanyalar/Index+Detail`, `_Layout`, `yanbar`
- **Build:** `dotnet build -o .coord-build-btn-fix` — 0 hata
- **Ertelenen:** Panel drawer hardcoded TR (`_KurumsalHeader`, `_FirmaHeader`); auth sayfaları kasıtlı TR metin

