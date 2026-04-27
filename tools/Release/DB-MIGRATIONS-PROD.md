## Paket 192 — DB migration (canlı) güvenli çalışma prosedürü

### Araç

`tools/DbMigrations/Apply-SqlServerMigrations.ps1`

- `Database/MigrationsSql` altında dosya adında `sqlserver` geçen `.sql` dosyalarını sırayla uygular.
- `dbo.schema_migrations` ile idempotent takip yapar.

### Canlı öncesi

1. **Tam yedek**: Veritabanının tam yedeği (bkup veya Azure snapshot).
2. Bakım penceresi veya düşük trafik zamanı.
3. Script’i staging’de veya prod kopyasında bir kez çalıştırın.
4. Rollback planı: migration geri alma script’i veya restore.

### Çalıştırma

```powershell
.\tools\DbMigrations\Apply-SqlServerMigrations.ps1 -ConnectionString "Server=...;Database=...;User Id=...;Password=...;Encrypt=True;"
```

Bağlantı dizgisini ortam değişkeninden okuyun; PowerShell geçmişine yazmayın.

### Başarısızlık

- Hata mesajını ve hangi dosyada kaldığını kaydedin.
- `schema_migrations` kaydı yapılmadıysa script tekrar güvenli şekilde denenebilir (idempotent tasarım).
- Şema kısmen değiştiyse manuel müdahale + runbook’a işleyin.
