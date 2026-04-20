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

## Local Geliştirme MSSQL
- Sunucu: `185.111.244.246`
- Veritabanı: `otelturizm_2026db`
- Kullanıcı: `sa`
- Güvenli bağlantı: `Encrypt=False;TrustServerCertificate=True;MultipleActiveResultSets=True`

Local connection string:

```text
Data Source=185.111.244.246;Initial Catalog=otelturizm_2026db;User ID=sa;Password=Nusret.34.34.-;Encrypt=False;TrustServerCertificate=True;MultipleActiveResultSets=True;
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
