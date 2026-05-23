# Grup 12 — Tools & Scripts

| Alan | Değer |
|------|-------|
| **Grup ID** | `12` |
| **Upstream** | `01` ✅ |
| **Durum** | ✅ |

## Ajan listesi

| Ajan | Rol | Grup içi bağımlılık |
|------|-----|---------------------|
| `mapping-json` | `schema_name_mapping.json` bakımı | — |
| `csharp-mapper` | `tools/Db` yardımcı scriptler | `mapping-json` |

## Dosya kapsamı

```text
tools/Db/**
!Database/MigrationsSql/**  (yalnızca 01 yazar; 12 PR önerir)
```

## Giriş kriterleri

- [x] **01** şema snapshot ✅

## Çıkış kriterleri

- [x] `schema_name_mapping.json` mevcut ve referanslı
- [x] `apply_local_database.ps1` dokümante
- [x] Migration README ile uyum

## Paralelleştirme kuralı

**01 ✅** olmadan mapping güncellenmez. Uygulama kodu (`Services/`) bu grupta değil.

## Atanan görevler (CTO kuyruk)

| Görev ID | Ajan | Durum |
|----------|------|-------|
| T320 | mapping-json | ✅ done |
| T321–T325 | csharp-mapper, tools-ork | ⏳ |

**Şef:** `tools-ork` | **Çıkış:** T325 Master CTO ✅

## İletişim

### mapping.json ✅
### apply script ✅
### schema snapshot ✅
