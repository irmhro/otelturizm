# Studio, Veritabanı ve Yayın Ayarları

Bu dosya, projeyi localde canlıya en yakın şekilde çalıştırıp ardından Visual Studio ile güvenli yayımlama yapmak için ana referans dokümandır.

## Temel Kural
- Geliştirme ortamı: `Development`
- Local geliştirme bağlantısı: canlı MSSQL ile aynı
- Sunucu: `185.111.244.246`
- Veritabanı adı: `otelturizm_2026db`
- Canlı veritabanı adı: `otelturizm_2026db`
- Amaç: local ve canlıda aynı veritabanı adı, aynı bağlantı hedefi ve aynı MSSQL yapısı ile ilerlemek

## Local Geliştirme Ayarları

### Uygulama URL'leri
Dosya: `Properties/launchSettings.json`

- HTTPS: `https://localhost:7223`
- HTTP: `http://localhost:5103`

Not:
- Asıl test adresi `https://localhost:7223` kullanılmalıdır.
- Antiforgery ve güvenlik cookie ayarları nedeniyle bazı akışlar HTTP'de doğru davranmayabilir.

### Development Veritabanı
Dosya: `appsettings.Development.json`

Kullanılan connection string:

```json
"DefaultConnection": "Data Source=185.111.244.246;Initial Catalog=otelturizm_2026db;User ID=sa;Password=Nusret.34.34.-;Encrypt=False;TrustServerCertificate=True;MultipleActiveResultSets=True;"
```

Kurallar:
- Local geliştirmede HeidiSQL, Laragon, MySQL kullanılmaz.
- Local testler canlıyla aynı MSSQL bağlantısı üzerinden yapılır.
- `DefaultConnection`, `MssqlLocalConnection` ve `dbbaglan` aynı hedefi göstermelidir.

## Canlı Ortam Ayarları

### Production Veritabanı
Dosya: `appsettings.json`

Kullanılan ana connection string:

```json
"DefaultConnection": "Data Source=185.111.244.246;Initial Catalog=otelturizm_2026db;User ID=sa;Password=Nusret.34.34.-;Encrypt=False;TrustServerCertificate=True;MultipleActiveResultSets=True;"
```

Ek bağlantılar:
- `MssqlLocalConnection`
- `dbbaglan`

Not:
- Bu dosya canlı MSSQL bağlantısını temsil eder.
- Local geliştirme sırasında ana referans `appsettings.Development.json` olmalıdır.

## Visual Studio Yayın Profili
Dosya: `Properties/PublishProfiles/IISProfile.pubxml`

Aktif önemli ayarlar:
- `WebPublishMethod`: `MSDeploy`
- `MSDeployServiceURL`: `185.111.244.246`
- `DeployIisAppPath`: `otelturizm.com`
- `MSDeployPublishMethod`: `WMSVC`
- `EnableMSDeployBackup`: `true`
- `EnableMSDeployAppOffline`: `true`

Önemli düzeltme:
- Publish sırasında `otelturizmnew.dll` lock hatası alınmaması için doğru property adı kullanılmaktadır:
  - `EnableMSDeployAppOffline`

## Locali Canlı ile Birebir Tutma Kuralları
- Veritabanı adı localde de canlıdakiyle aynı tutulur: `otelturizm_2026db`
- Development ve Production connection stringleri aynı canlı MSSQL hedefini kullanır.
- Sağlayıcı tek standarttır: `SqlServer`
- Yeni sorgular MSSQL söz dizimi ile yazılır.
- MySQL, MariaDB, HeidiSQL, Laragon aktif runtime standardı değildir.
- Migration başlangıçta otomatik çalıştırılmaz:
  - `RunMigrationsOnStartup = false`
- Şema değişiklikleri kontrollü uygulanır.

## Geliştirme -> Test -> Yayın Akışı

1. LocalDB üzerinde değişiklik yapılır.
2. Uygulama `https://localhost:7223` üzerinde test edilir.
3. Build alınır:

```powershell
dotnet build --no-restore
```

4. Eğer çalışan local process varsa kapatılır.
5. Visual Studio üzerinden publish başlatılır.
6. Yayın sonrası canlı site kontrol edilir.

## Publish Öncesi Kontrol Listesi
- `appsettings.Development.json` local DB'yi gösteriyor mu
- `appsettings.Development.json` içindeki `dbbaglan` dahil tüm connection stringler canlı MSSQL ile birebir aynı mı
- `appsettings.json` canlı MSSQL'i gösteriyor mu
- `launchSettings.json` içinde `https://localhost:7223` aktif mi
- `IISProfile.pubxml` içinde `EnableMSDeployAppOffline=true` var mı
- Localde çalışan `otelturizmnew.exe` veya `dotnet` process'i kapalı mı
- Son build temiz mi

## Yayın Sırasında DLL Lock Hatası Alınırsa
Hata örneği:
- `Web Dağıtımı hedefteki 'otelturizmnew.dll' dosyasını değiştiremiyor`

Yapılacaklar:
1. Canlı IIS tarafında çalışan uygulama AppOffline ile kısa süreli kapatılmalı
2. Gerekirse App Pool recycle edilmeli
3. Publish tekrar denenmeli

Not:
- Bu proje için publish profilinde AppOffline ayarı doğru property adıyla zaten tanımlandı.

## Operasyon Notu
- Bundan sonra local düzeltme, test ve yayın tek MSSQL hattı üzerinden yapılacaktır.
- “Önce localde düzelt, sonra yayımla” akışı bu dosya temel alınarak uygulanmalıdır.
