# Canlı Deploy — Dosya ve Veritabanı Güncelleme

Ana operasyon rehberi. Eski dağınık kopyalar: `Dokumanlar/Guvenlik/canli-baglanti-bilgileri.md`, `Dokumanlar/Yayin/yayin-ve-ortam-ayarlari.md`, `Dokumanlar/Yayin/canli-dosya-yayin-kilavuzu.md`, `Dokumanlar/Veritabani/canli-veritabani-yukleme-kilavuzu.md`.

---

## Zorunlu kural

- Kullanıcı **açıkça istemedikçe** canlı publish, canlı SQL ve GitHub push **yapılmaz**.
- Canlıya çıkan her değişiklik GitHub’a da gitmelidir.
- Canlı DB işleminden **önce yedek** alınır.
- Bu proje **EF migration kullanmaz**; SQL scriptler `Database/MigrationsSql` altından `sqlcmd` ile uygulanır.

---

## Ortam ve adresler

| Ortam | URL |
|--------|-----|
| Local HTTPS | `https://localhost:7223` |
| Local HTTP | `http://localhost:5103` |
| Canlı site | `https://otelturizm.com` |

| Ortam | Sunucu | Veritabanı | Kimlik |
|--------|--------|------------|--------|
| **Local** | `(localdb)\MSSQLLocalDB` | `otelturizm_2026db` | Windows Auth |
| **Canlı** | `185.111.244.246` | `otelturizm_2026db` | `sa` |

### Local connection string

```text
Server=(localdb)\MSSQLLocalDB;Database=otelturizm_2026db;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True;
```

Dosya: `appsettings.Development.json` → `DefaultConnection`

### Canlı connection string

```text
Data Source=185.111.244.246;Initial Catalog=otelturizm_2026db;User ID=sa;Password=Nusret.34.34.-;Encrypt=False;TrustServerCertificate=True;MultipleActiveResultSets=True;
```

Dosyalar: `appsettings.json`, `appsettings.Production.json` → `DefaultConnection`

---

## Canlı IIS

| Alan | Değer |
|------|--------|
| Sunucu IP | `185.111.244.246` |
| IIS site adı | `otelturizm.com` |
| Fiziksel yol | `C:\inetpub\wwwroot\otelturizm.com` |
| Web Deploy (WMSVC) | `185.111.244.246:8172` |
| Publish profili | `Properties/PublishProfiles/IISProfile.pubxml` |
| MSDeploy kullanıcı | `administrator` (parola VS publish profilinde) |
| AppOffline | aktif (`EnableMSDeployAppOffline=true`) |
| Build | `Release`, `win-x64`, framework-dependent |

Korunan klasörler (publish sırasında silinmez):
- `wwwroot/uploads`
- `App_Data/logs`

---

## 1) Local test

```powershell
dotnet build "D:\otelturizm\otelturizm.csproj" --no-restore
```

Tarayıcı: `https://localhost:7223`

Local DB migration (tüm scriptler):

```powershell
powershell -ExecutionPolicy Bypass -File "D:\otelturizm\tools\Db\apply_local_database.ps1"
```

Tek script:

```powershell
sqlcmd -S "(localdb)\MSSQLLocalDB" -d "otelturizm_2026db" -E -I -f 65001 -b -i "D:\otelturizm\Database\MigrationsSql\...\dosya.sql"
```

---

## 2) Canlı dosya yayını

### Yöntem A — Visual Studio / Publish profili (önerilen)

```powershell
dotnet publish "D:\otelturizm\otelturizm.csproj" -c Release /p:PublishProfile="Properties\PublishProfiles\IISProfile.pubxml"
```

### Yöntem B — Publish klasörü + MSDeploy

```powershell
dotnet publish "D:\otelturizm\otelturizm.csproj" -c Release -o "D:\otelturizm\artifacts\manual-publish"
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

### Yöntem C — Paket klasörü (RDP kopyalama)

```powershell
powershell -ExecutionPolicy Bypass -File "D:\otelturizm\tools\Release\Publish-To-Yayinla.ps1"
```

Çıktı: `D:\otelturizm\yayinla\publish-<tarih>/` → IIS fiziksel yola **tamamını** kopyala.

### Yayın kapsamı (sadece DLL yetmez)

- Derlenmiş uygulama (`bin`, `Views`, `wwwroot`)
- `wwwroot/assets/**` (CSS/JS)
- `wwwroot/uploads/**` (görseller — stratejiye göre)
- Demo otel / oda / kampanya görselleri

### DLL kilit hatası

Hata: `Web Dağıtımı hedefteki 'otelturizmnew.dll' dosyasını değiştiremiyor`

1. AppOffline ile site kapanır (profilde açık)
2. Gerekirse IIS App Pool recycle
3. Publish tekrar

---

## 3) Canlı veritabanı güncelleme

### Uygulama sırası

1. **Yedek** (full backup)
2. Tablo şema: `Database/MigrationsSql/tablo/migrationlar/`
3. Constraint/index: `Database/MigrationsSql/constraints/` (900–902)
4. Veri/seed: `Database/MigrationsSql/veri/migrationlar/`
5. Doğrulama sorguları

### Komut şablonu

```powershell
sqlcmd -S "185.111.244.246" -d "otelturizm_2026db" -U "sa" -P "Nusret.34.34.-" -I -f 65001 -b -i "D:\otelturizm\Database\MigrationsSql\...\dosya.sql" -W -w 240
```

Her script **tek tek** çalıştırılır; hata varsa sonrakine geçilmez.

### Son oturumda eklenen scriptler (örnek)

```powershell
sqlcmd -S "185.111.244.246" -d "otelturizm_2026db" -U "sa" -P "Nusret.34.34.-" -I -f 65001 -b -i "D:\otelturizm\Database\MigrationsSql\tablo\migrationlar\20260605_sqlserver_ozel_gunler.sql"
sqlcmd -S "185.111.244.246" -d "otelturizm_2026db" -U "sa" -P "Nusret.34.34.-" -I -f 65001 -b -i "D:\otelturizm\Database\MigrationsSql\veri\migrationlar\20260605_seed_ozel_gunler.sql"
sqlcmd -S "185.111.244.246" -d "otelturizm_2026db" -U "sa" -P "Nusret.34.34.-" -I -f 65001 -b -i "D:\otelturizm\Database\MigrationsSql\veri\migrationlar\20260605_seed_ozel_gunler_cleanup_sensitive.sql"
```

Doğrulama:

```sql
SELECT COUNT(*) FROM dbo.OZEL_GUNLER;
SELECT TOP 5 GUN_KODU, GUN_ADI, AKTIF_MI FROM dbo.OZEL_GUNLER ORDER BY SIRALAMA;
```

### appsettings (canlı)

- `Database:RunMigrationsOnStartup` → **`false`** (otomatik migration kapalı)
- `DevelopmentGate:Enabled` → **`false`**
- `ASPNETCORE_ENVIRONMENT` → **`Production`**

---

## 4) Canlı sonrası kontrol

```powershell
Invoke-WebRequest "https://otelturizm.com" -UseBasicParsing
Invoke-WebRequest "https://otelturizm.com/oteller" -UseBasicParsing
Invoke-WebRequest "https://otelturizm.com/kampanyalar" -UseBasicParsing
```

Kontrol listesi:

- [ ] Ana sayfa 200
- [ ] Hero kampanya slider / header özel gün
- [ ] `/kampanyalar` 6’lı grid
- [ ] Login / panel açılıyor
- [ ] SQL hatası yok (App_Data/logs veya IIS log)
- [ ] Görsel URL’leri 200
- [ ] GitHub commit + push (kullanıcı istediyse)

---

## 5) GitHub (kullanıcı istediğinde)

```powershell
git status --short
git add -- "ilgili-dosyalar"
git commit -m "Açıklayıcı mesaj"
git push origin master
```

---

## Demo hesaplar (test)

| Panel | E-posta | Şifre |
|-------|---------|-------|
| Partner | `demo.partner@otelturizm.com` | `Demo1585A!` |
| Firma | `demo.firma@otelturizm.com` | `Demo1585A!` |
| Kullanıcı | `demo.user@otelturizm.com` | `Demo1585A!` |

Developer panel: `/panel/developer/index`

---

## İlgili dosyalar

| Dosya | Açıklama |
|-------|----------|
| `Properties/PublishProfiles/IISProfile.pubxml` | MSDeploy profili |
| `tools/Db/apply_local_database.ps1` | Local DB tüm migration |
| `tools/Release/Publish-To-Yayinla.ps1` | Release paket üret |
| `Docs/DEPLOY_ACIL_500_VE_GORUNUR_GELISTIRME.md` | 500 / eksik publish sorunları |
| `tools/Release/BLUE-GREEN-DEPLOYMENT.md` | İleri seviye slot deploy |

---

## Operasyon özeti

| Komut | Anlam |
|-------|--------|
| **canlıya yükle** | Dosya + görsel + gerekli DB kayıtları eksiksiz senkron |
| **canlıya aktar** | Önce GitHub, sonra canlı dosya + DB |

**Son güncelleme:** 2026-06-05 — `canlıdeploy.md` ana dizine taşındı / birleştirildi.
