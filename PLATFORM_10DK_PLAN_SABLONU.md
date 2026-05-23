# Platform 10 Dakika Plan Şablonu

**Wave ID:** `Wave-___-YYYYMMDD-HHMM`  
**Tur başlangıç:** ____  
**Koordinatör:** Platform Coordinator (onaysız orchestra assign)

---

## PLAN (0–3 dk) — Tüm köşe taraması

| Köşe | Kontrol | Bulgu | P0? |
|------|---------|-------|-----|
| Kamu | anasayfa, `/oteller`, harita, detay, kampanya | | |
| Auth | user/partner/firma/admin/satis giriş+kayıt | | |
| Rezervasyon | adres API, profil, bildirim, checkout | | |
| Admin | dashboard, onay, rezervasyon, otel, güvenlik | | |
| Partner | takvim, komisyon, oda, özellik, foto | | |
| Firma / Satış | rezervasyon, fiyat, müşteri | | |
| DB | migration pending, seed, yayın durumu | | |
| Build | `.coord-build` 0 hata | | |
| FE-CTO | SS pending sayısı | | |

**Bu tur P0 (max 3):** T___ · T___ · T___

---

## EXECUTE (3–8 dk) — Orchestra atama

| Stream | Task | Ajan | Durum |
|--------|------|------|-------|
| H1 | | fe-otel-public | |
| H2 | | fe-partner | |
| H3 | | fe-admin | |
| H4 | | fe-user | |
| H7 | | security | |
| H8 | | db-services | |
| H9 | | seo | |
| H10 | | master-cto | |

---

## VERIFY (8–10 dk)

- [ ] `dotnet build -o .coord-build`
- [ ] `ORKESTRA_DURUM_KONTROL.md` snapshot
- [ ] `CTO_AJAN_ATAMA_KUYRUGU.md` wave kapatıldı

**Sonraki wave:** `Wave-___-____-____` (+10 dk)

---

## Örnek — Wave-V-20260526-1040 (doldurulmuş)

**Wave ID:** `Wave-V-20260526-1040`  
**Tur başlangıç:** 2026-05-26 10:30 +03  
**Sonraki wave:** `Wave-VI-20260526-1050` (+10 dk)

### PLAN (0–3 dk)

| Köşe | Kontrol | Bulgu | P0? |
|------|---------|-------|-----|
| Kamu | anasayfa, `/oteller`, harita, detay, kampanya | 39 seed + `PublishStatusSql` ✅; browser smoke bekliyor | — |
| Auth | user/partner/firma/admin/satis giriş+kayıt | E2E pending; satış `/kullanici-giris` tutarsız | **T398** |
| Rezervasyon | adres API, profil, bildirim, checkout | API wired; ilce/mahalle 400 eksik | T400 |
| Admin | dashboard, onay, rezervasyon, otel, güvenlik | T350 Revenue Center yok; T356 bulk yok | **T350**, **T356** |
| Partner | takvim, komisyon, oda, özellik, foto | Komisyon trend widget ✅ | — |
| Firma / Satış | rezervasyon, fiyat, müşteri | Satış demo seed yok | T398 |
| DB | migration pending, seed, yayın durumu | 39 otel localdb; T391 unicode fix uygulandı | — |
| Build | `.coord-build` 0 hata | ✅ pass (68s) | — |
| FE-CTO | SS pending sayısı | 6/151 | — |

**Bu tur P0 (max 3):** T350 · T356 · T398

### EXECUTE (3–8 dk)

| Stream | Task | Ajan | Durum |
|--------|------|------|-------|
| H3 | T350 Revenue Command Center | fe-admin | assigned |
| H3 | T356 bulk hotel publish | fe-admin | assigned |
| H7 | T398 satış OnRedirectToLogin + E2E | security | assigned |
| H4 | T400 adres API 400 | fe-user | queued |
| H10 | T349 build gate | master-cto | verify ✅ |

### VERIFY (8–10 dk)

- [x] `dotnet build -o .coord-build`
- [x] `ORKESTRA_DURUM_KONTROL.md` snapshot
- [x] `CTO_AJAN_ATAMA_KUYRUGU.md` wave_v active

**Özet:** Build pass · FE-CTO 6/151 · Köşe 2 PASS / 2 kısmi / 1 FAIL (auth)  
**Build:** pass · **FE-CTO:** 6/151 · **Oteller listed:** 39

---

## Örnek — Wave-X-20260526-integrasyon (doldurulmuş)

**Wave ID:** `Wave-X-20260526-integrasyon`  
**Tur başlangıç:** 2026-05-26 10:50 +03  
**Sonraki wave:** `Wave-XI-20260526-1100` (+10 dk)

### PLAN (0–3 dk)

| Köşe | Kontrol | Bulgu | P0? |
|------|---------|-------|-----|
| Kamu | anasayfa, `/oteller`, `en/de/fr/es/ru/ar` prefix, detay, kampanya | `InternationalSeoPaths` + hreflang wired; 39 seed ✅ | — |
| Auth | user/partner/firma/admin/satis giriş+kayıt | T398 SalesPanel + ReturnUrl kod ✅; E2E smoke pending | — |
| Rezervasyon | adres API, profil, bildirim, checkout | API wired; ilce/mahalle 400 eksik | T400 |
| Admin | dashboard, onay, rezervasyon, otel, güvenlik | T350 gelir merkezi ✅; T356 bulk ✅ | — |
| Partner | takvim, komisyon, oda, özellik, foto | H11 tahsilat merkezi ✅; T311 SS batch bekliyor | **T311** |
| Firma / Satış | rezervasyon, fiyat, müşteri | T398 demo seed ✅ | — |
| DB | migration pending, seed, yayın durumu | H11 `PLATFORM_TAHSILAT_*` migration — apply doc gerekli | **migration apply** |
| Build | `.coord-build` 0 hata | ✅ pass (`HotelListingSeo` + `OtellerController` SEO) | — |
| FE-CTO | SS pending sayısı | 6/151; user batch kuyruk | **FE-CTO user** |

**Bu tur P0 (kapalı):** T350 · T356 · T398 · H9 SEO · H11 komisyon · H4 mobile  
**Sonraki tur P0 (max 4):** i18n Faz2 (fr/es/resx) · FE-CTO user batch · T311 partner SS · komisyon migration apply doc

### EXECUTE (3–8 dk)

| Stream | Task | Ajan | Durum |
|--------|------|------|-------|
| H9 | InternationalSeoPaths + HotelListingSeo + Oteller locale routes | ork-seo | done ✅ |
| H3 | T350 Revenue · T356 bulk | fe-admin | done ✅ |
| H7 | T398 satış auth | security | done ✅ |
| H11 | Komisyon tahsilat T410–T415 | finans | done ✅ |
| H4 | User mobile safe-area pass | fe-user | done ✅ |
| H13 | i18n Faz1 scaffold | i18n | in_progress |
| H14 | Email master + 7 lang | email | assigned |
| H10 | T349 build gate | master-cto | verify ✅ |

### VERIFY (8–10 dk)

- [x] `dotnet build -o .coord-build`
- [x] `ORKESTRA_DURUM_KONTROL.md` Wave-X snapshot
- [x] `CTO_AJAN_ATAMA_KUYRUGU.md` active_wave Wave-X

**Özet:** Build 0 hata · InternationalSeo entegre · Tamamlanan: T350,T356,T398,H4,H11,H9  
**Sonraki:** i18n Faz2 · FE-CTO user · T311 · komisyon migration apply doc  
**Build:** pass · **FE-CTO:** 6/151 · **Oteller listed:** 39
