# Canlı Bağlantı Bilgileri

## Canlı MSSQL
- Sunucu: `185.111.244.246`
- Veritabanı: `otelturizm_2026db`
- Kullanıcı: `sa`
- Şifre: `Nusret.34.34.-`
- Şifreleme: `Encrypt=False`
- TrustServerCertificate: `True`
- MARS: `True`

Ana connection string:

```text
Data Source=185.111.244.246;Initial Catalog=otelturizm_2026db;User ID=sa;Password=Nusret.34.34.-;Encrypt=False;TrustServerCertificate=True;MultipleActiveResultSets=True;
```

Legacy referans olarak verdiğiniz satır:

```xml
<add name="dbbaglan" connectionString="Data Source=185.111.244.246;Initial Catalog=otelturizm_2026db;User ID=sa;Password=Nusret.34.34.-" />
```

Not:
- Bu satır bilgi amaçlı kaydedildi.
- Uygulama içinde aktif standart bağlantı adı `DefaultConnection` olarak kullanılmaktadır.

## Local Geliştirme MSSQL
- Sunucu: `(localdb)\MSSQLLocalDB`
- Veritabanı: `otelturizm_2026db`
- Kullanıcı: `Windows Authentication`
- Güvenli bağlantı: `Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True`

Local connection string:

```text
Server=(localdb)\MSSQLLocalDB;Database=otelturizm_2026db;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True;
```

## Uygulama Adresleri
- Local HTTPS: `https://localhost:7223`
- Local HTTP: `http://localhost:5103`
- Canlı site: `https://otelturizm.com`
- Developer login sonrası canlı yönlendirme: `/panel/developer/index`

## Canlı IIS
- Canlı sunucu: `185.111.244.246`
- IIS uygulama adı: `otelturizm.com`
- Fiziksel site yolu: `C:\inetpub\wwwroot\otelturizm.com`

## Publish
- Publish profil dosyası: `Properties/PublishProfiles/IISProfile.pubxml`
- Hedef IIS uygulaması: `otelturizm.com`
- Web Deploy sunucusu: `185.111.244.246`
- AppOffline: `aktif`
- Build yapılandırması: `Release`
- Runtime: `win-x64`

## Zorunlu Operasyon Kuralı
- Kullanıcı açıkça istemediği sürece:
  - canlıya publish yapılmaz
  - canlı veritabanına script çalıştırılmaz
  - GitHub'a push yapılmaz
- Kullanıcı isterse hedef açıkça ayrılır:
  - sadece local
  - sadece GitHub
  - sadece canlı
  - GitHub + canlı
- Canlıya çıkan her değişiklik GitHub'a da gitmelidir.

## Not
- Operasyon ve yayın adımları için ana rehber: `studio_yayin_ve_ortam_ayarlari.md`

## Canlıya nasıl yükleniyor (Operasyon Akışı)
1. Localde test et: `https://localhost:7223`
2. Build al:

```bash
dotnet build --no-restore
```

3. Publish al (Release):

```bash
dotnet publish "D:\otelturizm\otelturizmnew.csproj" -c Release
```

4. MSDeploy ile canlıya gönder:
- Profil: `D:\otelturizm\Properties\PublishProfiles\IISProfile.pubxml`
- Yöntem: `MSDeploy / WMSVC`
- Hedef app: `otelturizm.com`

Alternatif manuel publish:

```powershell
dotnet publish "D:\otelturizm\otelturizmnew.csproj" -c Release -o "D:\otelturizm\artifacts\manual-publish"
```

```powershell
& "C:\Program Files\IIS\Microsoft Web Deploy V3\msdeploy.exe" `
  -verb:sync `
  -source:contentPath="D:\otelturizm\artifacts\manual-publish" `
  -dest:contentPath="otelturizm.com",computerName="https://185.111.244.246:8172/msdeploy.axd?site=otelturizm.com",userName="administrator",password="***",authType="Basic" `
  -allowUntrusted `
  -enableRule:AppOffline `
  -enableRule:DoNotDeleteRule
```

## Veritabanı nasıl güncelleniyor
- Bu proje EF Migration kullanmaz.
- SQL scriptler `D:\otelturizm\Database\MigrationsSql` içinden `sqlcmd` ile uygulanır.

Örnek:

```bash
sqlcmd -S 185.111.244.246 -U sa -P "********" -d otelturizm_2026db -i "D:\otelturizm\Database\MigrationsSql\183_create_development_requests_module.sql"
```

Kontrol listesi:
- script localde test edildi mi
- canlıda doğru DB seçildi mi
- gerekiyorsa `schema_migrations` kaydı eklendi mi
- veri gerekiyorsa sadece şema değil veri de taşındı mı

## Dosya ve İçerik Gönderme Yönergesi
Canlıya yükleme sadece DLL publish değildir. Aşağıdakiler ayrıca kontrol edilir:

- `wwwroot/uploads`
- `wwwroot/assets/img`
- demo otel görselleri
- oda görselleri
- kampanya görselleri
- PDF, doküman, statik içerik

Doğrulama örneği:

```powershell
Invoke-WebRequest "https://otelturizm.com/uploads/demo/hotels/seyhli-grand-hotel/hotel-1.jpg" -UseBasicParsing
```

## GitHub Yükleme Yönergesi
Kullanıcı isterse uygulanır.

```powershell
git status --short
git add -- "ilgili-dosyalar"
git commit -m "Açıklayıcı mesaj"
git push origin master
```

Kural:
- ilgisiz kirli dosyaları commit etme
- canlıya çıktıysa GitHub karşılığını bırak
- kullanıcı istemeden push yapma

## Önemli operasyon kuralı (Zorunlu)
- **canlıya yükle** = dosyalar + görseller + gerekli DB kayıtları eksiksiz senkron.
- **canlıya aktar** = önce GitHub, sonra canlı.
- Canlıya çıkan her değişiklik GitHub’a da gitmelidir (sürüm kaydı/izlenebilirlik için).

## Demo Hesaplar
- Partner:
  - E-posta: `demo.partner@otelturizm.com`
  - Şifre: `Demo1585A!`
- Firma:
  - E-posta: `demo.firma@otelturizm.com`
  - Şifre: `Demo1585A!`
- Kullanıcı:
  - E-posta: `demo.user@otelturizm.com`
  - Şifre: `Demo1585A!`

## Veritabanı Aktarım Notu
- Bu proje `Entity Framework` kullanmaz.
- `DbContext` olmadığı için Visual Studio içindeki `EF Geçişleri` penceresi bu projede kullanılmaz.
- Veritabanı şema/veri aktarımı için:
  - SSMS `Generate Scripts`
  - `sqlcmd`
  - `sqlpackage`
  kullanılmalıdır.
