# Grup 02 — Models

| Alan | Değer |
|------|-------|
| **Grup ID** | `02` |
| **Upstream** | `01` ✅ |
| **Durum** | ✅ |

### Master CTO atama
- **Assigned by:** Master CTO
- **Started:** 2026-05-22T14:10Z
- **Completed:** 2026-05-22T15:00Z

## Ajan listesi

| Ajan | Rol | Grup içi bağımlılık |
|------|-----|---------------------|
| `model-mapper` | ViewModel/DTO ↔ BÜYÜK HARF sütun | — |
| `column-auditor` | `schema_name_mapping.json` ile çapraz denetim | `model-mapper` |

## Dosya kapsamı

```text
Models/**
tools/Db/schema_name_mapping.json
```

## Giriş kriterleri

- [x] Grup **01** şema snapshot ✅
- [x] Adres ID sözleşmesi (`UlkeId`, `IlId`, `IlceId`, `MahalleId`)

## Çıkış kriterleri

- [x] `MODELS_GELISTIRME.md` tüm satırlar ✅
- [x] `dotnet build` (Models katmanı)
- [x] `rg "\[Column\]" Models/` — EF entity yok (proje kuralı)

## Paralelleştirme kuralı

**01 ✅** olmadan property/sütun ekleme yapılmaz.

## Atanan görevler (CTO kuyruk)

| Görev ID | Ajan | Durum |
|----------|------|-------|
| T010–T014 | model-ork, model-mapper | ⏳ Wave 2 |

**Şef:** `model-ork`

## İletişim

### Reservations / Payments ✅
### Giris / Register ✅
### Oteller / Paneller ✅
### Destek / Email / diğer ✅

**Kaynak:** [Models/MODELS_GELISTIRME.md](../../Models/MODELS_GELISTIRME.md)
