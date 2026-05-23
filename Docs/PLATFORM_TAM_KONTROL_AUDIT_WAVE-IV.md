# Platform Tam Kontrol — Wave-IV Audit

**Tarih:** 2026-05-26 · **Mandate:** Giriş/kayıt, rezervasyon adresi, otel görünürlük, filtreler, kampanya fiyatları.

---

## Özet bulgular

| Alan | Durum | Sorun | Task |
|------|--------|-------|------|
| Oteller sitede | 🔴 kritik | DB’de 39 `Yayında` otel var; tam eşleşme `N'Yayında'` sqlcmd’de encoding farkı — kodda tolerans gerek | T391 |
| Anasayfa / liste / harita | 🔄 | `HotelService` yayın filtresi; Razor build kırığı liste sayfasını etkileyebilir | T392, T393 |
| Kahvaltı / öğle / akşam filtre | ✅ | `20260526_seed_otel_yemek_ozellikleri.sql` (KAHVALTI_DAHIL/OGLE/AKSAM); `ApplyCampaignFilter` + `OtelListeleme` sidebar + quick-filter; `?etiket=kahvalti-dahil` | T394 ✅, T395 ✅ |
| Kampanya indirimli fiyat | kısmi | `INDIRIMLI_FIYAT` + `kampanya_oteller` SQL var; kartta görünürlük doğrulanacak | T396 |
| Oda kapasitesi | kısmi | `TOPLAM_ODA_SAYISI` / müsaitlik SQL var; detay rezervasyon test | T397 |
| Giriş partner/firma/satış/admin | 🔄 test | Demo hesaplar: `irmhro0+{ilce}@gmail.com`, `ork-demo-admin@`, `ork-demo-partner@` | T398 |
| Kayıt aşamaları | 🔄 test | `AuthController` — E2E checklist gerekli | T399 |
| Rezervasyon ülke/il/ilçe/mahalle | kısmi | `/api/adres/*` + `OtelDetay` profil tamamlama zorunlu | T400 |
| Bildirimler | 🔄 | Doğrulama: toast/email şablonları rezervasyon sonrası | T401 |

---

## Oteller neden görünmeyebilir?

1. **Yanlış veritabanı** — Uygulama `otelturizm_2026db` dışında boş DB’ye bağlı.
2. **Migration uygulanmamış** — `RunMigrationsOnStartup: false`; seed manuel.
3. **Derleme / Razor** — `OtelListeleme.cshtml` hataları tam solution build’i kırıyor olabilir.
4. **Yayın durumu encoding** — Seed’de doğru `ı` (U+0131); kod literal’i farklıysa 0 sonuç → **T391 migration + SQL tolerans**.

**Doğrulama (localdb):** 39 otel, `LIKE N'Yay%'` + `ONAY LIKE N'Onay%'` → 39; koordinat dolu; 156 `OTEL_GORSELLERI`.

---

## Test URL’leri (smoke)

| Senaryo | URL |
|---------|-----|
| Anasayfa oteller | `/` |
| Liste | `/oteller`, `/oteller/istanbul` |
| Harita | `/oteller/harita` |
| Detay | `/oteller/orkestra-bogaz-otel` |
| Kampanya | `/oteller?kampanya=sehir-otelleri-kampanyasi` |
| Kahvaltı (hedef) | `/oteller?etiket=kahvalti-dahil` |
| Partner giriş | `/partner-giris` → `irmhro0+pendik@gmail.com` / `Demo123!` |
| Admin | `/admin-giris` → `ork-demo-admin@otelturizm.local` / `Demo123!` |
| Rezervasyon | Detay sayfası → tarih + oda + profil alanları |

---

## Wave-IV EXECUTE (30 dk)

| Stream | Task | İş |
|--------|------|-----|
| H8 | T391 | `20260526_fix_yayin_onay_unicode.sql` |
| H1 | T392–T393 | Liste Razor fix + görünürlük smoke doc |
| H1 | T394–T395 | ✅ Kahvaltı/öğle/akşam filtre UI + seed (`20260526_seed_otel_yemek_ozellikleri.sql`, `HotelService`, `OtelListeleme`) |
| H8 | T396–T397 | İndirimli fiyat + kapasite seed doğrulama |
| H7 | T398–T399 | Auth/kayıt E2E checklist + düzeltme |
| H4 | T400–T401 | Adres API + bildirim metinleri |

---

## Smoke sonuc

| Kontrol | Sonuç |
|---------|--------|
| sqlcmd `20260526_fix_yayin_onay_unicode.sql` | Uygulandı (`-I`); UPDATE satırları çalıştı, satır 18 `PRINT` alt sorgu hatası (Msg 1046) — kritik değil |
| `otelturizm_2026db` otel sayısı | `SELECT COUNT(*) FROM oteller o WHERE o.yayin_durumu LIKE N'Yay%'` → **39** |
| Tolerans SQL (`PublishStatusSql` deseni) | Mojibake/encoding nedeniyle localdb’de **0** eşleşme; `HotelService` artık `NCHAR(0x0131)` ile filtreliyor |
| `HotelService` | `PublishStatusSql` + `ApprovalStatusSql`; liste/arama/homepage ilk 5 `yayin` bloğu + `GetHotelListingPage` WHERE |
| `OtelListeleme.cshtml` | İç içe `@if` → `if` (RZ1010) |
| `dotnet build -o .coord-build` | **Başarılı** (0 hata, 0 uyarı) |

---

*Koordinatör: her 30 dk `PLATFORM_30DK_PLAN_SABLONU` + bu dosya güncellenir.*
