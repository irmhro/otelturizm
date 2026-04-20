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

## Publish
- Publish profil dosyası: `Properties/PublishProfiles/IISProfile.pubxml`
- Hedef IIS uygulaması: `otelturizm.com`
- Web Deploy sunucusu: `185.111.244.246`
- AppOffline: `aktif`

## Not
- Operasyon ve yayın adımları için ana rehber:
  - `studio_yayin_ve_ortam_ayarlari.md`

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
