# LocalDB — tam yerel veritabanı yedekleri (2026-06-06)

Bu klasör **LocalDB** üzerindeki otelturizm veritabanlarının **tam yedeklerini** içerir (tablolar, veriler, **foreign key**, index, constraint dahil).

| Dosya | Veritabanı | Boyut (yaklaşık) |
|-------|------------|------------------|
| `otelturizm_2026db.bak` | otelturizm_2026db | ~23 MB — **ana geliştirme DB** |
| `otelturizm_local.bak` | otelturizm_local | ~8 MB — ikinci yerel DB |

**Doğrulama (kaynak DB):** 145 kullanıcı tablosu, **46 foreign key**. GitHub clone + restore testi ile birebir doğrulandı.

## Hızlı kurulum (PowerShell)

```powershell
git clone https://github.com/irmhro/otelturizm.git
cd otelturizm
.\Database\Backups\20260606_localdb_full\restore-otelturizm_2026db.ps1
```

## Manuel restore (sqlcmd)

```powershell
cd otelturizm
$bak = (Resolve-Path ".\Database\Backups\20260606_localdb_full\otelturizm_2026db.bak").Path
sqlcmd -S "(localdb)\MSSQLLocalDB" -Q "RESTORE DATABASE [otelturizm_2026db] FROM DISK = N'$bak' WITH REPLACE, STATS = 10"
```

> `$(pwd)` bash sözdizimidir; Windows PowerShell'de yukarıdaki `$bak` satırını kullanın.

## Not

- `.bak` = SQL Server **FULL BACKUP**; şema + veri + FK bütünlüğü tek dosyada gelir.
- Canlı sunucu (production) yedeği bu pakette yoktur.
