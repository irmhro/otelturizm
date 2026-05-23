# Türkçe Dosya Adlandırma Planı

**Güncelleme:** 2026-05-22

Route'lar korunur; yalnızca C# dosya ve sınıf adları değişir (`[Route]` / action attribute'ları aynı).

---

## `Controllers/Api` — Tamamlandı ✅

| Eski ad | Yeni ad | Route (değişmedi) |
|---------|---------|-------------------|
| `AddressLookupController` | `AdresAramaController` | `api/adres/*` |
| `FavoritesApiController` | `FavorilerApiController` | (mevcut api route'lar) |
| `HotelSearchApiController` | `OtelAramaApiController` | |
| `GrowthAnalyticsController` | `BuyumeAnalitikController` | |
| `ClientErrorReportController` | `IstemciHataRaporController` | |
| `HotelPresenceController` | `OtelVarlikController` | |
| `PricingController` | `FiyatlandirmaController` | |
| `PhoneVerificationWebhookController` | `TelefonDogrulamaWebhookController` | |
| `RumVitalsController` | `RumVitalsController` | İzleme; ad korundu |

**Final Api controller listesi:**  
`AdresAramaController`, `BuyumeAnalitikController`, `FavorilerApiController`, `FiyatlandirmaController`, `IstemciHataRaporController`, `OtelAramaApiController`, `OtelVarlikController`, `RumVitalsController`, `TelefonDogrulamaWebhookController`

---

## Faz 2 — Panel controller pilot (Wave F1, 2026-05-23)

**Karar:** Build yeşil tutmak için **rename uygulanmadı**; pilot plan kayıt altına alındı.

| Eski | Önerilen | Route (korunur) | Pilot |
|------|----------|-----------------|-------|
| `SalesPanelController` | `SatisPanelController` | `/panel/satis/*` | ✅ Sonraki sprint (T147) |
| `UserPanelController` | `KullaniciPanelController` | `/panel/user/*` | ⏳ |
| `AdminPanelController` | `YonetimPanelController` | `/admin/*` | ⏳ |

**T147 adımları (plan):** dosya/sınıf rename → `Program.cs` / view `@using` taraması → `rg SalesPanel` ref-patch → build 0 hata → route attribute doğrulama.

---

## Backlog (bilinçli erteleme)

| Klasör | Öneri | Durum |
|--------|-------|-------|
| `Services/` | İngilizce servis adları domain ile uyumlu; toplu rename riskli | ⏳ Backlog |
| `Models/` | ViewModel adları İngilizce kalabilir | ⏳ Backlog |
| `Views/` | Klasörler zaten Türkçe (`Paneller`, `Oteller`, …) | ✅ |
| `wwwroot/assets/` | Sayfa bazlı css (`otel-detay.css`) — mevcut sözleşme | ⏳ Backlog |

---

## Referanslar

- `Program.cs` — DI kayıtları güncel sınıf adlarıyla
- `DB_UYUM_MASTER_PLAN.md` — genel uyum durumu
