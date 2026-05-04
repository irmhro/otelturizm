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
- Canlıya yükleme ve GitHub push işlemleri varsayılan davranış değildir.
- Kullanıcı açıkça istemediği sürece:
  - canlıya publish yapılmaz
  - canlı veritabanına script çalıştırılmaz
  - GitHub'a commit/push yapılmaz
- Kullanıcı açıkça `canlıya yükle`, `canlıya aktar`, `gitupa yükle`, `gitupa gönder`, `pushla` gibi bir talep verdiğinde operasyon başlatılır.
- Veritabanı geliştirmesi yapıldıysa ve kullanıcı canlı çalışma için bu geliştirmeyi istediyse, uygulama dosya deploy'undan bağımsız olarak canlı veritabanı da mutlaka güncellenir.
- Kod deploy'unu kullanıcı kendisi yapıyorsa bile, canlıda hata oluşturmaması için gerekli veritabanı şema/veri güncellemesi ayrıca uygulanır ve doğrulanır.
- Canlıya gönderilen her proje dosyası, kod değişikliği, migration, içerik dosyası veya önemli yapılandırma değişikliği GitHub reposuna da gönderilmiş olmalıdır.
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

## Başka Ekip Üyesi İçin Uygulama Kuralı
- Bu projede başka bir yapay zeka, developer veya operatör çalışacaksa aynı kural geçerlidir:
  1. Önce localde geliştir ve test et
  2. Kullanıcı açıkça istemeden canlıya veya GitHub'a hiçbir şey gönderme
  3. Kullanıcı isterse önce istenen hedefi netleştir:
     - sadece local değişiklik
     - sadece GitHub
     - sadece canlı
     - GitHub + canlı
  4. Canlıya çıkan her şeyin GitHub karşılığı bulunmalı
  5. Canlıya yüklemede sadece kod değil veritabanı ve içerik dosyaları da kontrol edilmeli

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

## GitHub'a Gönderme Akışı
Kullanıcı açıkça isterse uygulanır.

1. Değişen dosyaları kontrol et:

```powershell
git status --short
```

2. Yalnızca ilgili dosyaları stage et:

```powershell
git add -- "Views\\Oteller\\OtelDetay.cshtml" "wwwroot\\assets\\css\\otel-detay.css"
```

3. Commit oluştur:

```powershell
git commit -m "Kısa ve açıklayıcı commit mesajı"
```

4. Push gönder:

```powershell
git push origin master
```

Kurallar:
- İlgisiz kirli dosyaları commit etme
- Kullanıcı istemeden push atma
- Canlıya çıkan değişiklikleri push'sız bırakma

## Dosyaları Canlıya Yükleme Akışı
Kullanıcı açıkça isterse uygulanır.

### Yöntem 1: Publish profil üzerinden

```powershell
dotnet publish "D:\otelturizm\otelturizmnew.csproj" -c Release /p:PublishProfile="Properties\PublishProfiles\IISProfile.pubxml"
```

### Yöntem 2: Ayrı publish klasörü + MSDeploy

1. Publish klasörü üret:

```powershell
dotnet publish "D:\otelturizm\otelturizmnew.csproj" -c Release -o "D:\otelturizm\artifacts\manual-publish"
```

2. MSDeploy ile gönder:

```powershell
& "C:\Program Files\IIS\Microsoft Web Deploy V3\msdeploy.exe" `
  -verb:sync `
  -source:contentPath="D:\otelturizm\artifacts\manual-publish" `
  -dest:contentPath="otelturizm.com",computerName="https://185.111.244.246:8172/msdeploy.axd?site=otelturizm.com",userName="administrator",password="***",authType="Basic" `
  -allowUntrusted `
  -enableRule:AppOffline `
  -enableRule:DoNotDeleteRule
```

Yükleme kapsamı:
- derlenmiş uygulama dosyaları
- `wwwroot` altındaki görseller ve assetler
- upload klasörleri
- gerekiyorsa demo içerikleri

Canlı dosya yükleme sonrası en az şu adresler kontrol edilir:
- `https://otelturizm.com/`
- `https://otelturizm.com/Oteller`
- ilgili detay sayfası
- gerekiyorsa yüklenen görsel URL'si

## Veritabanını Canlıya Aktarma Akışı
Kullanıcı açıkça isterse uygulanır.

Bu proje EF migration tabanlı değildir. SQL script veya tam yedek yaklaşımı kullanılır.

### Seçenek 1: Script çalıştırma

1. İlgili scripti hazırla:
- klasör: `D:\otelturizm\Database\MigrationsSql`

2. Localde test et

3. Canlıda çalıştır:

```powershell
sqlcmd -S 185.111.244.246 -U sa -P "Nusret.34.34.-" -d otelturizm_2026db -i "D:\otelturizm\Database\MigrationsSql\dosya_adi.sql"
```

4. Gerekirse migration kaydını doğrula:
- tablo: `schema_migrations`

### Seçenek 2: SSMS ile tam şema/veri taşıma
- SSMS aç
- local DB: `otelturizm_2026db`
- Tasks > Generate Scripts
- gerekli tablo/şema/veri seçeneklerini aç
- canlı DB üzerinde scripti çalıştır

### Seçenek 3: Tam yedek / restore
Sadece kullanıcı açıkça tam taşıma istediyse kullanılır.

Kurallar:
- canlı DB üzerine işlem yapmadan önce hedef tabloyu ve etkiyi kontrol et
- tüm DB restore işlemi üretim verisini ezebilir; açık kullanıcı onayı olmadan yapılmaz
- içerik verisi gerekiyorsa sadece şema değil veri de taşınır

## İçerik ve Görsel Senkronu
Kod publish edildi diye içerik senkron tamamlandı sayılmaz.

Kontrol edilecek alanlar:
- `wwwroot/uploads`
- `wwwroot/assets/img`
- demo otel / oda / kampanya görselleri
- public erişen görsellerin fiziksel dosyaları
- veritabanındaki görsel yollarının canlıda gerçekten dosyaya karşılık gelmesi

Görsel URL testi örneği:

```powershell
Invoke-WebRequest "https://otelturizm.com/uploads/demo/hotels/seyhli-grand-hotel/hotel-1.jpg" -UseBasicParsing
```

## Operasyon Sonrası Doğrulama
İşlem tamamlandı demeden önce şu doğrulamalar yapılır:

1. Build başarılı mı
2. Canlı site `200` dönüyor mu
3. İstenen sayfa gerçekten güncel mi
4. Veritabanı scripti gerçekten işlendi mi
5. Gerekli içerik kayıtları gerçekten var mı
6. Görsel URL'leri `200` dönüyor mu
7. Eğer kullanıcı istediyse GitHub commit ve push tamam mı

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


  <add name="dbbaglan" connectionString="Data Source=185.111.244.246;Initial Catalog=otelturizm_2026db;User ID=sa;Password=Nusret.34.34.-"


