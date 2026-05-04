# Web Sunucusuna Bağlanamama Sorunu (Visual Studio `'https' web sunucusuna bağlanılamıyor`)

Bu doküman, Visual Studio'da görülen `'https' web sunucusuna bağlanılamıyor` hatasının kök nedenlerini ve kesin çözüm akışını içerir.

## 1) Sorunun Belirtisi

- Visual Studio `https` profili ile debug başlatıldığında tarayıcı açılmaz veya açılıp hata verir.
- Uyarı penceresi: `'https' web sunucusuna bağlanılamıyor.`

## 2) Bu Projede Tespit Edilen Kök Neden

Bu projede temel neden, uygulama başlangıcında çalışan e-posta arka plan servisinin veritabanı şemasıyla uyumsuz olmasıydı:

- `Services/EmailDeliveryBackgroundService.cs` içinde `bildirim_loglari` tablosunda `guncellenme_tarihi` kolonu bekleniyordu.
- İlgili kolon bazı ortamlarda yoktu.
- Sonuç: başlangıçta SQL hatası üretiliyor, Visual Studio tarafında profil açılışı kararsız davranıyordu.

## 3) Uygulanan Kalıcı Çözüm

### 3.1 Kod tarafı güvence

`EmailDeliveryBackgroundService` güncellendi:

- `guncellenme_tarihi` kolonu yoksa SQL `UPDATE` bu kolonu kullanmadan çalışır.
- Böylece şema eksik olsa bile servis düşmez.

Dosya:
- `D:\otelturizm\Services\EmailDeliveryBackgroundService.cs`

### 3.2 Veritabanı migration

Kolonu eksik ortamlara ekleyen migration eklendi:

- `D:\otelturizm\Database\MigrationsSql\20260427_sqlserver_add_guncellenme_tarihi_bildirim_loglari.sql`

Yaptığı iş:
- `dbo.bildirim_loglari.guncellenme_tarihi` yoksa ekler.
- Eski kayıtları `olusturulma_tarihi` ile doldurur.

## 4) Hızlı Tanı Komutları (Windows / PowerShell)

### 4.1 Launch profile kontrolü

```powershell
Get-Content D:\otelturizm\Properties\launchSettings.json
```

Beklenen:
- `https` profilinde `https://localhost:7223;http://localhost:5103`

### 4.2 HTTPS sertifika kontrolü

```powershell
dotnet dev-certs https --check --trust
```

Beklenen:
- `A trusted certificate was found`

### 4.3 Port erişim testi

```powershell
Invoke-WebRequest -UseBasicParsing https://localhost:7223/ -TimeoutSec 10
```

Beklenen:
- `StatusCode : 200`

### 4.4 Şema kontrolü

```sql
SELECT COLUMN_NAME
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'bildirim_loglari'
ORDER BY ORDINAL_POSITION;
```

Beklenen:
- `guncellenme_tarihi` listede bulunmalı.

## 5) Sorun Tekrar Ederse Sıralı Çözüm

1. Visual Studio'yu kapat.
2. Terminalde sertifikayı doğrula:
   - `dotnet dev-certs https --check --trust`
3. Uygulama profiline bak:
   - `launchSettings.json` içinde `https` profil URL'si doğru mu?
4. Migration script'i çalıştır:
   - `20260427_sqlserver_add_guncellenme_tarihi_bildirim_loglari.sql`
5. `dotnet run --launch-profile https` ile terminalden başlat ve logu izle.
6. Tarayıcıda `https://localhost:7223` aç.

## 6) Not

- Bu hata sadece sertifika kaynaklı değildir; başlangıç SQL hataları da Visual Studio'da aynı kullanıcı hatasına dönüşebilir.
- Bu projede çözüm hem uygulama kodu hem DB şema tarafında tamamlanmıştır.
