# LocalDB — tam yerel veritabanı yedekleri (2026-06-06)

Bu klasör **LocalDB** üzerindeki tüm otelturizm veritabanlarının güncel tam yedeklerini içerir.

| Dosya | Veritabanı | Açıklama |
|-------|------------|----------|
| `otelturizm_2026db.bak` | otelturizm_2026db | Ana geliştirme DB (appsettings.Development.json) |
| `otelturizm_local.bak` | otelturizm_local | Eski/yerel ikinci DB |

## Geri yükleme (LocalDB)

SSMS veya `sqlcmd` ile. Örnek (`otelturizm_2026db`):

```powershell
sqlcmd -S "(localdb)\MSSQLLocalDB" -Q "RESTORE DATABASE [otelturizm_2026db] FROM DISK = N'C:\path\to\repo\Database\Backups\20260606_localdb_full\otelturizm_2026db.bak' WITH REPLACE"
```

> SSMS **Restore Database** sihirbazında MOVE adımları dosya yolu çakışması olursa otomatik önerilir.

## Not

- Yedekler **LocalDB Express** üzerinde alınmıştır (sıkıştırmasız `.bak`).
- Canlı sunucu (185.x) yedeği bu pakette yoktur; sadece yerel LocalDB.
