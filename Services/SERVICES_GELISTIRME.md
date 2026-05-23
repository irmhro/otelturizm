# Services Geliştirme Takibi

**Grup ID:** `03`  
**Charter:** [docs/agent-gruplari/03-services.md](../docs/agent-gruplari/03-services.md)  
**Upstream:** Grup `01` ✅, `02` ✅

## Grup ID → dosya

| Grup ID | Kapsam |
|---------|--------|
| **03** | `Services/**`, `Services/Abstractions/**` |

## Kurallar

- SQL tabloları BÜYÜK HARF (`KULLANICILAR`, `OTELLER`, …)
- CSS/JS düzenlenmez (Grup **06**)
- Eşleme: `tools/Db/schema_name_mapping.json`

## Durum özeti

| Kontrol | Sonuç |
|---------|-------|
| `rg -i "FROM (users|hotels)" Services/` | ✅ eşleşme yok (2026-05-22) |
| Adres lookup servisi | ✅ |
| Panel servisleri | ✅ (smoke ⏳) |
| `dotnet build` | Grup **14** ile doğrulanır |

## Kalan (smoke, Grup 04/14)

- [ ] Rezervasyon uçtan uca
- [ ] Mahalle koordinat seed (Grup **01** `coord-fetcher`)

## İlgili

- [DB_UYUM_MASTER_PLAN.md](../DB_UYUM_MASTER_PLAN.md)
- [Controllers/CONTROLLERS_GELISTIRME.md](../Controllers/CONTROLLERS_GELISTIRME.md)
