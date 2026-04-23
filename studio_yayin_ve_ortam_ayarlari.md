# Studio, Veritabanı ve Yayın Ayarları

Bu dosya, projeyi localde canlıya en yakın şekilde çalıştırıp ardından Visual Studio ile güvenli yayımlama yapmak için ana referans dokümandır.

## Temel Kural
- Geliştirme ortamı: `Development`
- Local geliştirme bağlantısı: `LocalDB`
- Local sunucu: `(localdb)\MSSQLLocalDB`
- Canlı sunucu: `185.111.244.246`
- Veritabanı adı: `otelturizm_2026db`
- Canlı veritabanı adı: `otelturizm_2026db`
- Amaç: localde güvenli geliştirme yapıp aynı şema adıyla canlıya kontrollü publish almaktır

## Zorunlu Operasyon Notu
- Kullanıcının isteği olmadan bile temel kural korunur: canlıya gönderilen her proje dosyası, kod değişikliği, migration, içerik dosyası veya önemli yapılandırma değişikliği GitHub reposuna da gönderilmiş olmalıdır.
- Canlıya atılıp GitHub'a gönderilmeyen değişiklik bırakılmaz.
- Kullanıcı `canlıya yükle` dediğinde bu komut sadece uygulama dosyalarını publish etmek anlamına gelmez.
- `canlıya yükle` komutu şu anlama gelir: local projedeki gerekli uygulama dosyaları, görseller, upload klasörleri, demo veriler, migration dosyaları ve gerekli veritabanı kayıtları eksiksiz şekilde canlıya senkronlanacaktır.
- `canlıya yükle` komutunda eksik veri bırakılmaz; özellikle oteller, odalar, görseller, kampanyalar ve bunlara bağlı veritabanı kayıtları local ile canlı arasında karşılaştırılarak eksikler tamamlanır.
- Kullanıcı `canlıya aktar` dediğinde zorunlu sıra şudur:
  1. Önce proje dosyaları ve veritabanı ile ilgili güncel içerikler GitHub reposuna gönderilir.
  2. Sonra aynı güncel durum canlı dosyalara ve canlı veritabanına aktarılır.
- `canlıya aktar` veya `canlıya yükle` komutlarında yalnızca migration uygulamak yeterli sayılmaz; localde bulunan ama canlıda eksik kalan içerik verileri de ayrıca taşınmalıdır.
- Operasyon tamamlandı kabul edilebilmesi için en az şu kontroller yapılır:
  - canlı dosyalar publish edilmiş olmalı
  - canlı veritabanı script/migration kayıtları güncel olmalı
  - localde bulunan önemli veri kayıtları canlıda da var olmalı
  - görsel veya upload dosyaları canlı URL üzerinden erişilebilir olmalı

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
"DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=otelturizm_2026db;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True;"
```

Kurallar:
- Local geliştirmede HeidiSQL, Laragon, MySQL kullanılmaz.
- Local testler `LocalDB` üzerinde yapılır.
- Tek bağlantı standardı vardır: `DefaultConnection`
- `dbbaglan` ve benzeri legacy connection string isimleri kullanılmaz.

## Canlı Ortam Ayarları

### Production Veritabanı
Dosya: `appsettings.json`

Kullanılan ana connection string:

```json
"DefaultConnection": "Data Source=185.111.244.246;Initial Catalog=otelturizm_2026db;User ID=sa;Password=Nusret.34.34.-;Encrypt=False;TrustServerCertificate=True;MultipleActiveResultSets=True;"
```

Ek bağlantılar:
- yok

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
- `SelfContained`: `false`
- `UseAppHost`: `true`
- `LastUsedBuildConfiguration`: `Release`
- `RuntimeIdentifier`: `win-x64`

Önemli düzeltme:
- Publish sırasında `otelturizmnew.dll` lock hatası alınmaması için doğru property adı kullanılmaktadır:
  - `EnableMSDeployAppOffline`
- `NETSDK1067` hatasını önlemek için publish profilinde `UseAppHost=true` açık tutulur.

## Locali Canlı ile Birebir Tutma Kuralları
- Veritabanı adı localde de canlıdakiyle aynı tutulur: `otelturizm_2026db`
- Development ve Production aynı DB adını kullanır ama farklı sunuculara bağlanır.
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
   - CLI eşdeğeri:

```powershell
dotnet publish -c Release /p:PublishProfile="Properties\PublishProfiles\IISProfile.pubxml"
```
6. Yayın sonrası canlı site kontrol edilir.

## Publish Öncesi Kontrol Listesi
- `appsettings.Development.json` local DB'yi gösteriyor mu
- `appsettings.json` canlı MSSQL'i gösteriyor mu
- `launchSettings.json` içinde `https://localhost:7223` aktif mi
- `IISProfile.pubxml` içinde `EnableMSDeployAppOffline=true` var mı
- `IISProfile.pubxml` içinde `UseAppHost=true` var mı
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
- Bundan sonra local düzeltme ve test `LocalDB` üzerinde yapılacaktır.
- Canlı deploy sırasında yalnızca publish ve kontrollü SQL aktarımı uygulanacaktır.
- “Önce localde düzelt, sonra yayımla” akışı bu dosya temel alınarak uygulanmalıdır.

## EF Geçişleri Hakkında
- Bu proje `Entity Framework DbContext` tabanlı değildir.
- Veri erişimi `Microsoft.Data.SqlClient` ile elle yazılmış SQL sorguları üzerinden yapılır.
- Bu nedenle Visual Studio içindeki:
  - `Bağlı Hizmetler > SQL Server Veritabanı > Entity Framework Geçişleri`
  ekranı bu projede çalışmaz.
- `Microsoft.EntityFrameworkCore.Design` eklemek tek başına çözüm değildir; çünkü projede `DbContext` yoktur.
- Veritabanı aktarımı için kullanılacak doğru araçlar:
  1. SSMS `Generate Scripts`
  2. `sqlcmd`
  3. `sqlpackage` / dacpac
