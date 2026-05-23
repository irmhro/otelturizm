# Dosyaları Canlıya Yükleme - Eksiksiz Yayın Kılavuzu

Bu doküman, uygulama dosyalarını canlı sunucuya güvenli biçimde taşıma adımlarını içerir.

## 1) Yayın Öncesi Hazırlık

1. Kod doğrulama:
   - `dotnet build` başarılı olmalı.
2. Ortam dosyaları:
   - `appsettings.Production.json` doğru olmalı.
3. Veritabanı:
   - Gerekli migration/seed scriptleri uygulanmış olmalı.
4. Onay:
   - Canlı yükleme için yazılı/onaylı izin alınmalı.

## 2) Publish Alma

```powershell
dotnet publish D:\otelturizm\otelturizm.csproj -c Release -o D:\otelturizm\.codex-publish\release
```

Oluşan klasör:
- `D:\otelturizm\.codex-publish\release`

## 3) Sunucuya Dosya Transferi

Transfer yöntemleri:
- RDP + kopyalama
- SFTP/WinSCP
- CI/CD artifact deploy

Kopyalanacak ana içerik:
- publish klasörü içindeki tüm dosyalar
- gerekli `uploads` içerikleri (stratejiye göre)

## 4) IIS ile Canlı Dağıtım (Önerilen)

1. IIS uygulama havuzunu durdur:
   - `Stop-WebAppPool -Name "UYGULAMA_HAVUZU"`
2. Mevcut canlı klasörü yedekle:
   - ör. `C:\inetpub\otelturizm_backup_yyyyMMdd_HHmm`
3. Yeni publish dosyalarını canlı klasöre kopyala.
4. Uygulama havuzunu başlat:
   - `Start-WebAppPool -Name "UYGULAMA_HAVUZU"`

## 5) Kestrel + Reverse Proxy Kullanımı

Eğer IIS yerine servis kullanılıyorsa:

1. Servisi durdur.
2. Yeni publish dosyalarını hedef dizine kopyala.
3. Servisi başlat.
4. Nginx/IIS reverse proxy route ve HTTPS doğrula.

## 6) appsettings.Production.json Kontrolü

Kontrol edilmesi gereken alanlar:

- `ConnectionStrings:DefaultConnection`
- `App:PublicBaseUrl`
- `Database:RunMigrationsOnStartup` (genelde `false`)
- SMTP ve dış servis ayarları

## 7) Canlı Sonrası Zorunlu Testler

1. Ana sayfa açılıyor mu?
2. `https` sertifikası geçerli mi?
3. Login / admin panel açılıyor mu?
4. Partner fiyat/takvim ekranları açılıyor mu?
5. Hata loglarında SQL veya 500 var mı?

## 8) Hızlı Sağlık Kontrolü

```powershell
Invoke-WebRequest https://otelturizm.com -UseBasicParsing
```

Beklenen:
- HTTP 200 veya yönlendirme senaryosunda 301/302 + hedef URL’de 200

## 9) Rollback Planı

- Eski sürüm klasörünü sakla.
- Sorun çıkarsa:
  1. Yeni sürümü durdur
  2. Eski sürümü geri kopyala
  3. Servisi tekrar başlat

## 10) Operasyon Disiplini

- Canlı ortamda doğrudan elle dosya silme yerine yedekli değişim yap.
- Her yayın için:
  - yayın zamanı,
  - commit/hash,
  - uygulanan migration listesi
  kayıt altına alınmalı.

