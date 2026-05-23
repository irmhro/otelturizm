# Grup 01 — Migrasyon & DB

| Alan | Değer |
|------|-------|
| **Grup ID** | `01` |
| **Charter** | `docs/agent-gruplari/01-migrasyon-db.md` |
| **Upstream** | Yok |
| **Durum** | ✅ |

## Ajan listesi

| Ajan | Rol | Grup içi bağımlılık |
|------|-----|---------------------|
| `schema-migrator` | Tablo/ALTER script üretimi ve sıra | — |
| `seed-runner` | `veri/migrationlar` idempotent seed | `schema-migrator` ✅ |
| `coord-fetcher` | Mahalle/koordinat seed ve adres verisi | `seed-runner` (kısmi) |

## Dosya kapsamı

```text
Database/MigrationsSql/**
Data/SqlMigrationRunner.cs
tools/Db/apply_local_database.ps1
tools/Db/schema_source_snapshot/**
```

**Dokunulmaz:** `Database/Backups/**`, canlı DB doğrudan düzenleme.

## Giriş kriterleri

- [x] Şema kaynağı tanımlı (`tablo/migrationlar`, `constraints`, `veri`)
- [x] `SqlMigrationRunner` uygulama açılışında kayıtlı

## Çıkış kriterleri

- [x] Migration sırası dokümante (`Database/MigrationsSql/README.md`)
- [x] Yeni değişiklikler idempotent `.sql` ile
- [x] `rg` — eski tablo adı migration dışı kritik yok (spot)
- [x] CTO: downstream 02/03/12 başlayabilir

## Paralelleştirme kuralı

**Bu grup upstream gerektirmez.** Diğer gruplar **01 ✅** olmadan şema/seed kodu yazmaz (audit hariç).

## İletişim — dosya alt bölümleri

### `tablo/migrationlar/` ✅
### `constraints/` ✅
### `veri/migrationlar/` ✅
### `SqlMigrationRunner` ✅

## Atanan görevler (CTO kuyruk)

| Görev ID | Ajan | Durum |
|----------|------|-------|
| T001–T005 | db-ork, schema-migrator, seed-runner | 🔄 Wave 1 |

**Şef:** `db-ork` | **Çıkış:** T005 Master CTO ✅ → Grup 02/03/12

## Bağımsız geçiş notu (2026-05-22)

MigrationsSql yapısı ve runner mevcut; mahalle koordinat seed backlog `coord-fetcher` için 🔄 (veri, bloklayıcı değil).

### Master CTO atama
- **Assigned by:** Master CTO
- **Started:** 2026-05-22T14:00Z
- **Completed:** 2026-05-22T15:00Z
