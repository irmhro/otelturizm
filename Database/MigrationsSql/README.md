# MigrationsSql

MSSQL şema ve veri betikleri ayrı klasörlerde tutulur.

## Klasör yapısı

| Klasör | İçerik |
|--------|--------|
| `tablo/migrationlar/` | Tablo `CREATE` snapshot (`000_SEMA_MIGRASYONLARI.sql`, `001_…` …) ve şema migration (`YYYYMMDD_*_sqlserver_*.sql`, `ALTER` vb.) |
| `constraints/` | `900_foreign_keys.sql`, `901_indexes.sql`, `902_triggers.sql` |
| `veri/migrationlar/` | Seed / içerik (`YYYYMMDD_seed_*.sql`, UTF-8 BOM) |

## Uygulama sırası

1. `tablo/migrationlar/*.sql` (dosya adına göre)
2. `constraints/900` → `901` → `902`
3. `veri/migrationlar/*.sql` (dosya adına göre)

Uygulama: uygulama açılışında `SqlMigrationRunner` veya `tools/Db/apply_local_database.ps1` (yerel LocalDB).

## Geçmiş (SEMA_MIGRASYONLARI)

Kayıt **yalnızca dosya adı** (`BETIK_ADI`) + içerik hash ile tutulur. Dosyayı başka klasöre taşımak, runner’ın dosyayı yeniden “görmemesi” anlamına gelmez; ancak eski konumdaki ad zaten uygulandıysa ve içerik değişmediyse yeniden çalıştırılmaz. Klasör taşıması sonrası aynı dosya adıyla yeni ortamda ilk kez çalışıyorsa idempotent scriptler güvenlidir.

## Yeni dosya adlandırma

- Şema: `YYYYMMDD_aciklama_sqlserver_*.sql` → `tablo/migrationlar/`
- Seed: `YYYYMMDD_seed_*.sql` → `veri/migrationlar/`
