## Paket 196 — SQL Server yedekleme ve geri yükleme

### Tam yedek (örnek)

```sql
BACKUP DATABASE [otelturizm_2026db]
TO DISK = N'D:\Backup\otelturizm_full.bak'
WITH FORMAT, INIT, COMPRESSION, STATS = 10;
```

### Geri yükleme

- Önce aktif bağlantıları kısıtlayın veya tek kullanıcı modunu kullanın.
- `RESTORE DATABASE ... FROM DISK = ... WITH REPLACE` (ortamınıza göre).

### Prod kuralları

- Migration öncesi mutlaka yedek.
- Yedek dosyası erişim izinleri sıkı; şifreli ortamda saklayın.
