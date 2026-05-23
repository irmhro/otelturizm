# Grup 03 — Services

| Alan | Değer |
|------|-------|
| **Grup ID** | `03` |
| **Upstream** | `01` ✅, `02` ✅ |
| **Durum** | ✅ |

### Master CTO atama
- **Assigned by:** Master CTO
- **Started:** 2026-05-22T14:15Z
- **Completed:** 2026-05-22T15:10Z

## Ajan listesi

| Ajan | Rol | Grup içi bağımlılık |
|------|-----|---------------------|
| `sql-fixer` | Inline SQL → BÜYÜK HARF tablo/sütun | — |
| `service-integrator` | Abstractions, DI, controller sözleşmesi | `sql-fixer` |

## Dosya kapsamı

```text
Services/**
Services/Abstractions/**
```

**Yasak:** `wwwroot/**/*.css`, `wwwroot/**/*.js` (Grup **06**).

## Giriş kriterleri

- [x] **01** + **02** ✅
- [x] `tools/Db/schema_name_mapping.json` güncel

## Çıkış kriterleri

- [x] `SERVICES_GELISTIRME.md` — kritik servisler ✅
- [x] `rg -i "FROM (users|hotels)" Services/` — eşleşme yok (2026-05-22)
- [x] `dotnet build`
- [ ] CTO: uçtan uca smoke (rezervasyon, adres lookup) — opsiyonel backlog

## Paralelleştirme kuralı

**01+02 ✅** olmadan SQL düzeltmesi yapılmaz. CSS/JS bu grupta düzenlenmez.

## Atanan görevler (CTO kuyruk)

| Görev ID | Ajan | Durum |
|----------|------|-------|
| T020–T030 | svc-ork, sql-upper | 🔄 Wave 2 (`T020` in_progress) |

**Şef:** `svc-ork`

## İletişim

### Adres / lookup servisleri ✅
### Rezervasyon / ödeme ✅
### Panel servisleri (Admin/Partner/User) ✅
### Auth servisleri 🔄 (Grup **07** ile paylaşımlı izleme)

**Kaynak:** [Services/SERVICES_GELISTIRME.md](../../Services/SERVICES_GELISTIRME.md)
