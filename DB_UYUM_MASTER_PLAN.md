# DB Uyum — Ana Plan (Master)

**Tamamlanma:** 2026-05-22 ✅  
**Şema gerçeği:** `Database/MigrationsSql/tablo/migrationlar/` (BÜYÜK HARF tablo/sütun)  
**Eşleme:** `tools/Db/schema_name_mapping.json`  
**SQL literal aracı:** `tools/Db/apply_schema_mapping_to_csharp.py` (+ `fast_dbo_table_casefix.py`)

---

## Faz durumu

| Faz | Kapsam | Durum | Not |
|-----|--------|-------|-----|
| A | Audit & plan senkron | ✅ | Stale SQL audit: kritik desenler 0 |
| B | Database tooling | ✅ | `SqlMigrationRunner`: tablo → constraints → veri |
| C | Services | ✅ | SQL literal BÜYÜK HARF; build yeşil |
| D | Controllers (37) | ✅ | Inline SQL + service uyumu |
| E | Models | ✅ | Adres/MISAFIR_* ID alanları; DTO notları |
| F | Views + CSS + JS | ✅ | `VIEWS_GELISTIRME.md` %100 |
| G | Türkçe Api dosya adları | ✅ | `Controllers/Api/*` yeniden adlandırıldı |
| H | Data / Middleware / Filters / BG | ✅ | `KALAN_DB_UYUM_GELISTIRME.md` |
| I | Build + audit | ✅ | `dotnet build` 0 hata (`.build-verify2`) |

---

## Alt planlar (detay)

| Dosya | Tamamlanma |
|-------|------------|
| [Controllers/CONTROLLERS_GELISTIRME.md](Controllers/CONTROLLERS_GELISTIRME.md) | **100%** ✅ |
| [Models/MODELS_GELISTIRME.md](Models/MODELS_GELISTIRME.md) | **100%** ✅ |
| [Views/VIEWS_GELISTIRME.md](Views/VIEWS_GELISTIRME.md) | **100%** ✅ |
| [KALAN_DB_UYUM_GELISTIRME.md](KALAN_DB_UYUM_GELISTIRME.md) | **100%** ✅ |
| [TURKCE_DOSYA_ADLANDIRMA_PLAN.md](TURKCE_DOSYA_ADLANDIRMA_PLAN.md) | **Api %100**; diğer klasörler backlog |

---

## Audit özeti (2026-05-22)

| Desen | Kalan (cs/cshtml/js) |
|-------|----------------------|
| `FROM users` / `FROM hotels` | **0** |
| `dbo.users` / `dbo.hotels` | **0** |
| `email_services` (kod) | **0** |
| `GetProvincesAsync(string` | **0** |
| `GetProvincesAsync(ulkeId)` | **Uyumlu** |

Kabul edilen istisnalar: stored procedure adları (`dbo.usp_*`), log metinlerinde gelecek tablo adları, admin UI açıklama stringleri.

---

## Manuel / canlı (bu sprint dışı)

- Canlı DB: migration öncesi **full backup** zorunlu
- Mahalle koordinat seed: `fetch_turkiye_coordinates.py` + `veri/migrationlar` — yalnızca dev/stage doğrulama
- Git commit / deploy: **yapılmadı**

---

## Değişiklik günlüğü

| Tarih | Not |
|-------|-----|
| 2026-05-22 | Master plan oluşturuldu; tüm fazlar tamamlandı işaretlendi |
