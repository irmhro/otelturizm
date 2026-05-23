# Controllers — veritabanı uyum planı

**Tamamlanma:** 2026-05-22 — **100%** ✅

Yerel referans: `Database/MigrationsSql/tablo/migrationlar/` (BÜYÜK HARF tablo/sütun).  
Eşleme sözlüğü: `tools/Db/schema_name_mapping.json`.

---

## Özet

| Metrik | Değer |
|--------|-------|
| Toplam `.cs` | **37** |
| Doğrudan SQL içeren | **2** (`AdminPanelController`, `FavorilerApiController`) — güncellendi |
| Service üzerinden | **35** — Services katmanı uyumlu |
| Build | **0 hata** |

---

## Adımlar (tümü ✅)

| Adım | Kapsam | Durum |
|------|--------|-------|
| 0 | Hazırlık, migration, build | ✅ |
| 1 | `Controllers/Api` (9 dosya, Türkçe ad) | ✅ |
| 2 | Admin panel (inline SQL) | ✅ |
| 3–12 | User, Partner, Firma, Satis, Departman, Developer, Common, Oteller, Login, Register, diğer | ✅ |
| 13 | Services eşlemesi | ✅ |

---

## Api controller (final Türkçe dosya adları)

`AdresAramaController`, `BuyumeAnalitikController`, `FavorilerApiController`, `FiyatlandirmaController`, `IstemciHataRaporController`, `OtelAramaApiController`, `OtelVarlikController`, `RumVitalsController`, `TelefonDogrulamaWebhookController`

---

## Değişiklik günlüğü

| Tarih | Dosya | Not |
|-------|-------|-----|
| 2026-05-22 | `CONTROLLERS_GELISTIRME.md` | %100 tamamlandı |
| 2026-05-22 | `FavorilerApiController` | `KULLANICILAR` |
| 2026-05-22 | `AdminPanelController` | Yardım merkezi + sistem SQL BÜYÜK HARF |
