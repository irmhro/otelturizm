# Otelturizm — PC taşıma paketi (2026-06-05)

## İçerik

- `otelturizm_2026db.bak` — LocalDB `otelturizm_2026db` tam yedek (yaklaşık 23 MB)

## Yeni PC kurulumu

1. Repoyu klonlayın: `git clone https://github.com/irmhro/otelturizm.git`
2. .NET 8 SDK kurulu olsun.
3. `appsettings.Development.json` içindeki LocalDB bağlantısını kullanın (varsayılan).
4. Veritabanını geri yükleyin (LocalDB):

```powershell
sqlcmd -S "(localdb)\MSSQLLocalDB" -Q "RESTORE DATABASE [otelturizm_2026db] FROM DISK = N'C:\path\to\repo\Database\Backups\20260605_migration_package\otelturizm_2026db.bak' WITH REPLACE, MOVE N'otelturizm_2026db' TO N'C:\Users\<KULLANICI>\otelturizm_2026db.mdf', MOVE N'otelturizm_2026db_log' TO N'C:\Users\<KULLANICI>\otelturizm_2026db_log.ldf'"
```

> MOVE yolları ortamınıza göre değişir. SSMS üzerinden **Restore Database** ile de açılabilir.

5. Bağımlılıklar: `dotnet restore`
6. Çalıştırma: `dotnet run --project otelturizm.csproj`

## Canlı sunucu bağlantısı

Production SQL şifreleri güvenlik için repoda yoktur. Yeni PC'de `appsettings.Production.local.json` oluşturun veya User Secrets kullanın.

## Şema güncellemeleri

Ek migration scriptleri: `Database/MigrationsSql/` (idempotent SQL dosyaları).
