# Auth + Rezervasyon E2E Audit (H7 + H4)

**Tarih:** 2026-05-23  
**Kapsam:** Giriş/kayıt rotaları, panel demo hesapları, `OtelDetay` rezervasyon + adres API, kampanya fiyatları, T398–T401 backlog.  
**Kaynak kod (dar okuma):** `Controllers/Login/AuthController.cs`, `Controllers/Register/RegisterController.cs`, `Controllers/Oteller/OtellerController.cs`, `Controllers/Api/AdresAramaController.cs`, `Controllers/Kampanyalar/KampanyalarController.cs`, `Services/HotelService.cs`, `Services/PublicReservationService.cs`.  
**Demo dokümanlar:** `Docs/ADMIN_TEST_KULLANICI.md`, `Docs/ISTANBUL_ILCE_DEMO_KURULUM.md`.

---

## 1) Giriş / kayıt rota haritası

Tüm public auth rotaları `AuthController` ve `RegisterController` üzerinden attribute route ile tanımlı (ayrı `Login/*` controller yok).

### Kullanıcı (misafir)

| HTTP | Rota | Action / View | Not |
|------|------|---------------|-----|
| GET | `/kullanici-giris` | `UserLogin` → `Views/Login/UserLogin.cshtml` | Giriş + kayıt sekmesi (aynı sayfa) |
| POST | `/kullanici-giris` | `UserLogin` | `loginEmail`, `loginPassword`, `rememberMe` |
| GET | `/kullanici-giris-2fa` | `UserLoginTwoFactor` → `UserLogin2FA.cshtml` | 2FA yalnızca bu view |
| POST | `/kullanici-giris-2fa` | `UserLoginTwoFactorPost` | |
| POST | `/kullanici-giris-2fa/tekrar-gonder` | `ResendUserLoginTwoFactor` | |
| GET | `/kullanici-kayit` | `UserRegisterRedirect` → redirect `/kullanici-giris` + `OpenUserRegisterTab` | |
| POST | `/kullanici-kayit` | `UserKayit` | Başarı → `/eposta-dogrula?email=…` |
| GET | `/eposta-dogrula` | `VerifyEmail` → `VerifyEmail.cshtml` | |
| POST | `/eposta-dogrula` | `VerifyEmailPost` | |
| POST | `/eposta-dogrula/tekrar-gonder` | `ResendVerifyEmail` | |
| GET | `/sifremi-unuttum` | `ForgotPassword` | |
| POST | `/sifremi-unuttum` | `ForgotPassword` | |
| GET | `/sifre-sifirla` | `ResetPassword` | |
| POST | `/sifre-sifirla` | `ResetPassword` | |
| GET/POST | `/cikis-yap` | `Logout` / `LogoutPost` | Çıkış sonrası hesap tipine göre ilgili giriş sayfası |

**Başarılı yönlendirme:** `accountType` → `/panel/user` (`UserPanel`).

### Partner

| HTTP | Rota | Action / View |
|------|------|---------------|
| GET | `/partner-giris` | `PartnerLogin` → `PartnerLogin.cshtml` |
| POST | `/partner-giris` | `PartnerLogin` (`partnerIdentity`, `partnerPassword`) |
| GET | `/partner-kayit` | redirect `/partner-giris` + `OpenPartnerRegisterTab` |
| POST | `/partner-kayit` | `Partner` → e-posta doğrulama URL |

**Başarılı yönlendirme:** `/panel/partner` (`PartnerPanel`).

**2FA:** Partner/admin/firma 2FA açıksa yine `/kullanici-giris-2fa` (ortak view; bkz. §6).

### Firma

| HTTP | Rota | Action / View |
|------|------|---------------|
| GET | `/firma-giris` | `FirmaLogin` → `FirmaLogin.cshtml` |
| POST | `/firma-giris` | `FirmaLogin` (`firmaIdentity`, `firmaPassword`) |
| GET | `/firma-kayit` | redirect `/firma-giris` + `OpenFirmaRegisterTab` |
| POST | `/firma-kayit` | `Firma` → e-posta doğrulama URL |

**Başarılı yönlendirme:** `/panel/firma` (`FirmaPanel`).

### Satış

| Durum | Detay |
|--------|--------|
| Ayrı `/satis-giris` | **Yok** — satış hesapları `/kullanici-giris` ile oturum açar |
| Panel | `[Route("panel/satis")]` → `SalesPanelController` |
| Cookie `OnRedirectToLogin` | `/panel/satis/*` → **`/kullanici-giris?ReturnUrl=…`** (ayrı `/satis-giris` yok; ortak kullanıcı girişi) |
| Başarılı auth redirect | `accountType=sales` veya `sales_*` rol → `/panel/satis` veya korunan `ReturnUrl` |
| Yetkilendirme | `[Authorize(Policy = "SalesPanel")]` — `accountType=sales` veya `userRole` `sales_*` |

**Demo satış hesabı:** `Database/MigrationsSql/veri/migrationlar/20260526_seed_satis_demo_kullanici.sql` — `satis@demo.otelturizm.local` / `Demo123!`

### Admin

| HTTP | Rota | Action / View |
|------|------|---------------|
| GET | `/admin-giris` | `AdminLogin` → `AdminLogin.cshtml` |
| POST | `/admin-giris` | `AdminLogin` (`adminEmail`, `adminPassword`) |
| GET | `/adminx` | `LegacyAdminLogin` → 302 `/admin-giris` |

**Başarılı yönlendirme:** `/admin/dashboard` (`AdminPanel.Dashboard`). Admin rolü yoksa `AdminLoginError`.

### Cookie auth (Program.cs)

- Varsayılan `LoginPath`: `/kullanici-giris`
- `/panel/partner` → `/partner-giris`
- `/panel/firma` → `/firma-giris`
- `/admin/*` → `/admin-giris`
- `/panel/satis/*` → `/kullanici-giris` + `ReturnUrl` (satış için ayrı giriş rotası yok; T398 ile `ReturnUrl` ve `SalesPanel` policy eklendi)

---

## 2) Panel E2E kontrol tabloları

**Durum:** `pending` = otomasyon/manuel smoke henüz işaretlenmedi; kod incelemesi tamamlandı.

### Kullanıcı (misafir)

| Rota | View | Demo kimlik | Beklenen adımlar | Durum |
|------|------|-------------|------------------|-------|
| `/kullanici-giris` | `UserLogin.cshtml` | `ork-demo-misafir@otelturizm.local` / `Demo123!` (seed: `20260526_seed_istanbul_ilce_oteller_tam.sql`) | Giriş → `/panel/user` veya ReturnUrl | pending |
| `/kullanici-kayit` | (redirect) | Yeni e-posta | Kayıt → `/eposta-dogrula` → doğrula → giriş | pending |
| `/oteller/{slug}` | `OtelDetay.cshtml` | Misafir hesabı | Tarih/oda seç → profil tamamla → rezervasyon POST | pending |

### Partner

| Rota | View | Demo kimlik | Beklenen adımlar | Durum |
|------|------|-------------|------------------|-------|
| `/partner-giris` | `PartnerLogin.cshtml` | `irmhro0+pendik@gmail.com` / `Demo123!` veya `ork-demo-partner@otelturizm.local` / `Demo123!` | Giriş → `/panel/partner` | pending |
| `/partner-kayit` | (redirect) | Yeni partner | Kayıt → e-posta doğrulama → onay sonrası giriş | pending |
| `/panel/partner/takvim-fiyatlar` | Partner panel | İlçe partner | Takvim + fiyat görünür | pending |

### Firma

| Rota | View | Demo kimlik | Beklenen adımlar | Durum |
|------|------|-------------|------------------|-------|
| `/firma-giris` | `FirmaLogin.cshtml` | Seed’de hazır firma demo **yok** (manuel başvuru/onay) | Onaylı firma ile `/panel/firma` | pending |
| `/firma-kayit` | (redirect) | Yeni firma | Başvuru → admin onayı → giriş | pending |

### Satış

| Rota | View | Demo kimlik | Beklenen adımlar | Durum |
|------|------|-------------|------------------|-------|
| `/kullanici-giris` | `UserLogin.cshtml` | `satis@demo.otelturizm.local` / `Demo123!` (seed T398) | Giriş → `/panel/satis` veya `ReturnUrl` | **done** (kod; smoke pending) |
| `/panel/satis/yeni-rezervasyon` | `CreateReservation.cshtml` | Demo satış hesabı | Müşteri + otel + rezervasyon oluştur | pending |

### Admin

| Rota | View | Demo kimlik | Beklenen adımlar | Durum |
|------|------|-------------|------------------|-------|
| `/admin-giris` | `AdminLogin.cshtml` | `ork-demo-admin@otelturizm.local` / `Demo123!` (`Docs/ADMIN_TEST_KULLANICI.md`) | Giriş → `/admin/dashboard`, RBAC `platform_admin_full` | pending |
| `/admin/rezervasyonlar` | Admin panel | Admin demo | İlçe seed rezervasyonları (`ORK-ILCE-*`) listelenir | pending |

---

## 3) Rezervasyon akışı — `OtelDetay`

**Sayfa:** `GET /oteller/{slug}` → `OtellerController.OtelDetay` → `Views/Oteller/OtelDetay.cshtml`  
**Rezervasyon başlat:** `POST /oteller/{slug}/rezervasyon` (`StartReservation`)  
**Profil tamamlama (inline):** `POST /oteller/{slug}/profil-bilgilerini-tamamla` (JSON)

### Zorunlu profil alanları (UI + sunucu)

| Alan | `BuildProfilePrompt` / `ValidateReservationProfile` | `PublicReservationService.IsProfileComplete` |
|------|------------------------------------------------------|---------------------------------------------|
| Ad / Soyad | Evet | `FullName` (birleşik) |
| E-posta | Evet | Evet |
| Telefon | Evet | Evet |
| Doğum tarihi | Evet (+ 18 yaş, +1 gün kuralı) | `IsAgeEligible` |
| Cinsiyet | Evet | Evet |
| İl (`City`) | Evet | Evet |
| İlçe (`District`) | Evet | Evet |
| Mahalle (`Neighborhood`) | Evet | Evet |
| Açık adres | Formda var; **rezervasyon zorunlu setinde yok** | Zorunlu değil |

**Misafir (giriş yok):** Taslak `Giris Bekliyor` → mesaj + redirect `/kullanici-giris?ReturnUrl=…`  
**Girişli profil eksik:** Taslak `Profil Eksik` → `PublicReservationInfo` + `?continueDraft=1&openProfile=1`

### Adres API (`AdresAramaController`)

| Endpoint | Parametre | Validasyon |
|----------|-----------|------------|
| `GET /api/adres/ulkeler` | — | — |
| `GET /api/adres/iller` | `ulkeId` (>0) | Eksik → 400 `{ message: "ulkeId zorunludur." }` |
| `GET /api/adres/ilceler` | `ilId` | **Zorunluluk kontrolü yok** (0/eksik → boş liste) |
| `GET /api/adres/mahalleler` | `ilceId` | **Zorunluluk kontrolü yok** |

**OtelDetay JS:** `ulkeler` → `iller?ulkeId=` → `ilceler?ilId=` → `mahalleler?ilceId=` (satır ~4107+).

### TempData / bildirim anahtarları (rezervasyon)

| Anahtar | Kullanım | View |
|---------|----------|------|
| `PublicReservationSuccess` | Başarılı `StartReservation` | `OtelDetay` toast |
| `PublicReservationInfo` | Validasyon, hız limiti, iş kuralı, exception | `OtelDetay` bilgi |
| *(Error ayrı anahtar yok)* | Hatalar da `PublicReservationInfo` | Tek kanal |

**Tutarsızlık örnekleri (T401):**

- Servis mesajları ASCII Türkçe: `Lutfen`, `secilmeden`, `Devam etmek icin once giris yapiniz` — UI Türkçe karakterli.
- Başarısız rezervasyon ile profil uyarısı aynı `Info` anahtarında; ayrı `Error` yok (UX ayrımı zayıf).
- `CompleteProfileInline` JSON Türkçe; `StartReservation` redirect TempData farklı stil.

### `ValidateReservationStartForm` (POST öncesi)

Otel/oda/tarih/misafir sayısı/gece (1–60); **profil alanları burada kontrol edilmiyor** (taslak veya profil adımına bırakılıyor).

---

## 4) Kampanya sayfası ve liste filtresi

### Kampanya vitrini

| Rota | Controller | View |
|------|------------|------|
| `GET /kampanyalar` | `KampanyalarController.Index` | `Views/Kampanyalar/Index.cshtml` |
| `GET /kampanyalar/{slug}` | `KampanyalarController.Detail` | `Views/Kampanyalar/Detail.cshtml` |

Örnek slug (seed): `sehir-otelleri-kampanyasi` (`KMP-2026-SEHIR`).

### Otel listesinde kampanya

| Rota | Parametre | İşleme |
|------|-----------|--------|
| `GET /oteller` | `?kampanya={seo-slug}` | `SearchTextNormalizer.Normalize` → `HotelService.GetHotelListingPageAsync(..., campaignSlug)` |
| `GET /oteller/harita` | Aynı | Harita + liste SEO |

**Kampanya meta:** DB `KAMPANYALAR.SEO_SLUG`; kartlarda `kampanya_oteller` + `INDIRIMLI_FIYAT` / `ODA_FIYAT_MUSAITLIK`.

### İndirimli fiyat kuralları (`HotelService`)

1. **Kaynak fiyat:** `gecelik_fiyat` vs `indirimli_fiyat` (`ODA_FIYAT_MUSAITLIK`); indirimli geçerli yalnızca `0 < indirimli < gecelik`.
2. **Liste/kart:** `HasDiscount`, `OriginalPrice`, `DiscountedPrice`, `DiscountPercent` (1–95); misafir gösterimi `InclusiveNightlyPricing.StoredNetToGuestDisplay` ile KDV/konaklama vergisi dahil yuvarlama.
3. **Kampanya bağlantısı:** `kampanyalar` + `kampanya_oteller`; vitrin linki `/oteller?kampanya={slug}`.
4. **`ApplyCampaignFilter`:** `kampanya` query’si slug’dan **etiket** (`ActiveTag`) türetilir; bilinmeyen slug’larda filtre fallback (tüm liste) — kampanya DB filtresi ile etiket filtresi karışabilir (**T396**).

**Smoke URL:** `/oteller?kampanya=sehir-otelleri-kampanyasi` (Wave-IV doc ile uyumlu).

---

## 5) Top 10 bug / fix (T398–T401)

| # | Task | Önem | Bulgu | Önerilen fix |
|---|------|------|-------|----------------|
| 1 | **T398** | Yüksek | `/panel/satis` yetkisiz erişimde giriş + `ReturnUrl` | **done:** `SalesPanel` policy, `ReturnUrl` POST/2FA, `OnRedirectToAccessDenied` satış için `ReturnUrl`; demo seed |
| 2 | **T398** | Orta | Partner/admin/firma 2FA sonrası hep `/kullanici-giris-2fa` (misafir UI) | **partial** — satış akışı `ReturnUrl` korunur; panel bazlı 2FA view açık |
| 3 | **T399** | Orta | Partner kayıt başarısı `UserLoginSuccess` kullanıyordu; partner view okumuyordu | **Düzeltildi:** `PartnerRegisterSuccess` + `PartnerLogin.cshtml` |
| 4 | **T399** | Orta | Firma kayıt başarısı `UserLoginSuccess`; view `FirmaRegisterSuccess` bekliyordu | **Düzeltildi:** `FirmaRegisterSuccess` |
| 5 | **T399** | Düşük | Kayıt sonrası partner/firma da `UserLoginSuccess` ile verify sayfasına gidiyor | Verify sonrası `ResolveLoginPathByEmailAsync` ile doğru giriş URL (mevcut) — smoke doğrula |
| 6 | **T400** | Orta | `ilceler` / `mahalleler` API’de `ilId`/`ilceId` ≤0 için 400 yok | `iller` ile aynı BadRequest sözleşmesi |
| 7 | **T400** | Orta | Profil tamamlama metin (`City`/`District`) ile DB id (`UlkeId`/`IlId`) ayrı; kayıtta eşleşme hatası riski | `SaveProfileAsync` id + metin tutarlılık testi |
| 8 | **T401** | Orta | Rezervasyon hata/başarı tek TempData tipi (`PublicReservationInfo` vs `Success`) | Kritik hatalar için `PublicReservationError` + view ayrımı |
| 9 | **T401** | Düşük | `PublicReservationService` kullanıcı mesajlarında ASCII Türkçe | Mesajları UI ile aynı karakter setine çek |
| 10 | **T396** | Orta | `?kampanya=` slug → `ActiveTag` heuristic; DB kampanya otel seti ile etiket filtresi örtüşmeyebilir | `campaignSlug` için doğrudan `kampanya_oteller` SQL filtresi |

---

## 6) Bu oturumda uygulanan hızlı düzeltme

| Dosya | Değişiklik |
|-------|------------|
| `Controllers/Register/RegisterController.cs` | Partner → `PartnerRegisterSuccess`; Firma → `FirmaRegisterSuccess` |
| `Views/Login/PartnerLogin.cshtml` | Kayıt başarı banner’ı eklendi |

| `Program.cs` | `SalesPanel` authorization policy; satış `OnRedirectToAccessDenied` + `ReturnUrl` |
| `Controllers/Login/AuthController.cs` | `ReturnUrl` koruma (giriş + 2FA cookie) |
| `Controllers/Paneller/Satis/SalesPanelController.cs` | `[Authorize(Policy = "SalesPanel")]` |
| `Views/Login/UserLogin.cshtml` | Gizli `ReturnUrl` alanı |
| `Database/.../20260526_seed_satis_demo_kullanici.sql` | Demo satış kullanıcısı |

**Build:** `.build-t398`; commit yapılmadı.

---

## 7) Önerilen E2E smoke sırası (yerel `http://127.0.0.1:5103`)

1. Migration: `ADMIN_TEST_KULLANICI.md` + `ISTANBUL_ILCE_DEMO_KURULUM.md` sırası.
2. Admin: `/admin-giris` → dashboard.
3. Partner: `/partner-giris` (`irmhro0+pendik@gmail.com`).
4. Misafir: `/kullanici-giris` (`ork-demo-misafir@otelturizm.local`) → `/oteller/orkestra-pendik-hotel` (veya ilçe slug).
5. Profil + rezervasyon POST; `PublicReservationSuccess` / profil modal.
6. `/oteller?kampanya=sehir-otelleri-kampanyasi` — indirimli fiyat kartta.
7. `/kampanyalar` ve `/kampanyalar/sehir-otelleri-kampanyasi`.

---

## İlgili görevler

| Stream | Task | Bu doküman bölümü |
|--------|------|-------------------|
| H7 | T398–T399 | §1–2, §5 #1–5 |
| H4 | T400–T401 | §3, §5 #6–9 |
| H8 | T396 | §4, §5 #10 |

*Wave-IV özet:* `Docs/PLATFORM_TAM_KONTROL_AUDIT_WAVE-IV.md`
