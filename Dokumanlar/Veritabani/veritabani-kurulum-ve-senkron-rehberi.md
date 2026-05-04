# Veritabanı Kurulum ve Senkron Rehberi

Bu doküman, `otelturizm_2026db` veritabanını hem local hem canlı ortamda nasıl oluşturacağımızı, migrationları nasıl uygulayacağımızı ve local içeriği canlıya nasıl senkronlayacağımızı tek yerde toplar.

## 1. Amaç

Bu rehber üç işi kapsar:
- sıfırdan veritabanı oluşturma
- migration ve seed uygulama
- local içeriği canlıya senkronlama

## 2. Standart Veritabanı Adı

- Local: `otelturizm_2026db`
- Canlı: `otelturizm_2026db`

İki ortamda da aynı veritabanı adı kullanılır. Fark sadece sunucudur.

## 3. Local ve Canlı Bağlantı

### Local

```txt
Server=(localdb)\MSSQLLocalDB;Database=otelturizm_2026db;Trusted_Connection=True;TrustServerCertificate=True;
```

### Canlı

```txt
Data Source=185.111.244.246;Initial Catalog=otelturizm_2026db;User ID=sa;Password=Nusret.34.34.-;Encrypt=False;TrustServerCertificate=True;MultipleActiveResultSets=True;
```

## 4. Sıfırdan DB Oluşturma Scriptleri

Hazır bootstrap dosyaları:
- `D:\otelturizm\Database\Bootstrap\001_create_otelturizm_database.sql`
- `D:\otelturizm\Database\Bootstrap\002_verify_otelturizm_database.sql`

### Localde oluşturma

```powershell
sqlcmd -S "(localdb)\MSSQLLocalDB" -d "master" -i "D:\otelturizm\Database\Bootstrap\001_create_otelturizm_database.sql"
```

### Canlıda oluşturma

```powershell
sqlcmd -S "185.111.244.246" -U "sa" -P "Nusret.34.34.-" -d "master" -i "D:\otelturizm\Database\Bootstrap\001_create_otelturizm_database.sql"
```

### Doğrulama

```powershell
sqlcmd -S "(localdb)\MSSQLLocalDB" -d "master" -i "D:\otelturizm\Database\Bootstrap\002_verify_otelturizm_database.sql"
sqlcmd -S "185.111.244.246" -U "sa" -P "Nusret.34.34.-" -d "master" -i "D:\otelturizm\Database\Bootstrap\002_verify_otelturizm_database.sql"
```

## 5. Migration Uygulama

Tüm migrationlar:
- `D:\otelturizm\Database\MigrationsSql`

Temel yaklaşım:
1. önce local
2. sonra canlı
3. her script sonrası doğrulama

Örnek:

```powershell
sqlcmd -S "(localdb)\MSSQLLocalDB" -d "otelturizm_2026db" -i "D:\otelturizm\Database\MigrationsSql\20260428_create_platform_email_center_tables.sql"
sqlcmd -S "185.111.244.246" -U "sa" -P "Nusret.34.34.-" -d "otelturizm_2026db" -i "D:\otelturizm\Database\MigrationsSql\20260428_create_platform_email_center_tables.sql"
```

### 5.1 Local’de tablo tablo migration (önerilen)

Not: `Database\MigrationsSql` klasöründe eski MySQL döneminden kalan scriptler de olabildiği için,
local MSSQL’de her `.sql` dosyası çalıştırılamaz. Aşağıdaki betik:

- MSSQL uyumlu olan scriptleri seçer (MySQL kokulu olanları pas geçer)
- dosya adına göre sıralı uygular
- istenirse sadece belirli tabloları hedefler
- uygulananları `dbo.schema_migrations` tablosuna kaydeder

Sadece planı görmek (uygulama yapmadan):

```powershell
pwsh D:\otelturizm\tools\DbMigrations\Apply-LocalDb-TableWise.ps1 -WhatIf
```

Belirli tablolar için çalıştırmak:

```powershell
pwsh D:\otelturizm\tools\DbMigrations\Apply-LocalDb-TableWise.ps1 -Tables oteller, rezervasyonlar, fiyat_indirimleri
```

Hepsini çalıştırmak:

```powershell
pwsh D:\otelturizm\tools\DbMigrations\Apply-LocalDb-TableWise.ps1
```

## 6. Local İçeriği Canlıya Senkronlama

Hazır script:
- `D:\otelturizm\tools\DatabaseSync\sync_local_to_live.py`

Bu script:
- local tüm tabloları okur
- canlıdaki aynı tabloları temizler
- local veriyi canlıya yeniden yazar
- identity değerlerini korur

Çalıştırma:

```powershell
python D:\otelturizm\tools\DatabaseSync\sync_local_to_live.py
```

## 7. Senkron Öncesi Zorunlu Kural

Canlıya tam senkron atmadan önce:
- canlı yedek alınmalı
- kullanıcı onayı olmalı
- local verinin gerçekten doğru olduğu doğrulanmalı

Özellikle dikkat:
- bu script canlıdaki mevcut içerikleri local ile değiştirir
- yani içerik senkronu için uygundur
- ama “canlıdaki veriyi koru, sadece yeni kolon ekle” senaryosunda kullanılmaz

O tip işlerde şu doküman esas alınır:
- `veritabani-gelistirme-ve-guncelleme-standardi.md`

## 8. Hangi Senaryoda Hangisi Kullanılır

### Sıfırdan yeni makine / yeni DB
- bootstrap script
- migrationlar
- gerekiyorsa backup restore veya seed

### Sadece şema geliştirme
- migration script
- canlı veriyi koruyan `ALTER` yaklaşımı

### Local içeriği canlıya tamamen eşitleme
- `sync_local_to_live.py`

## 9. Canlıda Şu Anki Gerçek Durum

Bu proje için canlı ve local ana veritabanı adı aynıdır:
- `otelturizm_2026db`

Kural:
- kod deploy ayrı iştir
- DB senkron ayrı iştir
- biri yapıldı diye diğeri yapılmış sayılmaz

## 10. İlgili Diğer Dokümanlar

- `veritabani-gelistirme-ve-guncelleme-standardi.md`
- `canli-veritabani-yukleme-kilavuzu.md`
- `canli-baglanti-bilgileri.md`
- `yayin-ve-ortam-ayarlari.md`
