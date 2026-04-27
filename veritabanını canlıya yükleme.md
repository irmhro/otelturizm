# Veritabanını Canlıya Yükleme (SQL Server) - Eksiksiz Kılavuz

Bu doküman, `otelturizm_2026db` veritabanı migration/seed dosyalarını canlıya güvenli şekilde uygulama adımlarını içerir.

## 1) Ön Koşullar

- Sunucu: SQL Server erişimi (`Data Source`, `User`, `Password`)
- Yetki: `ALTER`, `CREATE`, `INSERT`, `UPDATE`
- Yerel makinede:
  - `sqlcmd` kurulu
  - migration dosyaları güncel

## 2) Güvenlik Kuralı (Zorunlu)

- **Onay olmadan canlıya uygulama yapılmaz.**
- Önce test/local ortamda denenir.
- Canlı uygulama öncesi yedek alınır.

## 3) Uygulama Sırası

Önerilen sıra:

1. Şema migration'ları (`create/alter/index`)
2. Veri düzeltme migration'ları (`fix/normalize`)
3. Seed içerikleri (`seed_*`)
4. Doğrulama sorguları

## 4) Komut Şablonu

```powershell
sqlcmd -S "185.111.244.246" -d "otelturizm_2026db" -U "sa" -P "PAROLA" -i "D:\otelturizm\Database\MigrationsSql\DOSYA.sql" -W -w 240
```

## 5) Örnek: Tek Migration Çalıştırma

```powershell
sqlcmd -S "185.111.244.246" -d "otelturizm_2026db" -U "sa" -P "PAROLA" -i "D:\otelturizm\Database\MigrationsSql\20260427_sqlserver_add_guncellenme_tarihi_bildirim_loglari.sql" -W -w 240
```

Beklenen:
- `kolon_var_mi = 1`

## 6) Çoklu Migration Uygulama (Manuel Güvenli Akış)

1. Çalıştırılacak dosyaları tarih sırasına göre belirle.
2. Her dosyayı tek tek çalıştır.
3. Her dosyadan sonra doğrulama sorgusu çalıştır.
4. Hata olursa bir sonraki dosyaya geçme.

## 7) Kritik Doğrulamalar

### 7.1 Kolon var mı?

```sql
SELECT COLUMN_NAME
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'bildirim_loglari'
  AND COLUMN_NAME = 'guncellenme_tarihi';
```

### 7.2 Seed sayısı kontrolü

```sql
SELECT COUNT(*) AS toplam
FROM dbo.destek_makaleleri;
```

### 7.3 Spesifik içerik kontrolü

```sql
SELECT TOP (10) id, baslik, seo_slug
FROM dbo.destek_makaleleri
WHERE seo_slug LIKE 'booking-%-makale-%'
   OR seo_slug LIKE 'airbnb-%-makale-%'
   OR seo_slug LIKE 'expedia-%-makale-%'
ORDER BY id DESC;
```

## 8) Rollback (Geri Dönüş) Prensibi

- Migration öncesi full backup al:
  - SQL Server Management Studio veya planlı yedek.
- Veri kaybı riski taşıyan scriptlerde:
  - Önce `SELECT` ile etki alanını doğrula.
  - Mümkünse transaction kullan.

## 9) Canlı İçin appsettings Notu

- `appsettings.Production.json` bağlantı dizesi canlı DB’yi göstermeli.
- `Database:RunMigrationsOnStartup` canlıda genelde `false` bırakılır.
- Migration otomatik değil, kontrollü şekilde `sqlcmd` ile uygulanır.

## 10) Operasyon Sonu Kontrol Listesi

- [ ] Tüm migration komutları hatasız tamamlandı
- [ ] Şema doğrulamaları geçti
- [ ] Seed verileri doğru sayıda oluştu
- [ ] Uygulama açılış logunda SQL hatası yok
- [ ] Kritik ekranlar (admin/partner/public) test edildi

