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
| Web Deploy (WMSVC) | `https://185.111.244.246:8172/msdeploy.axd?site=otelturizm.com` |
| Publish profili | `Properties/PublishProfiles/IISProfile.pubxml` |
| MSDeploy kullanıcı | `administrator` |
| MSDeploy parola | `uaD1pBe0Bp9q` |
| AppOffline | aktif (`EnableMSDeployAppOffline=true`) |
| Build | `Release`, framework-dependent |

Korunan klasörler (publish sırasında **silinmez / atlanır**):
- `wwwroot/uploads`
- `App_Data/logs`

### Son başarılı dosya deploy (2026-06-05)

| Alan | Değer |
|------|--------|
| Publish çıktısı | `D:\otelturizm\artifacts\live-deploy-20260606\` |
| Dosya sayısı | 9.620 |
| MSDeploy kuralı | `DoNotDeleteRule` (sunucudaki mevcut dosyalar silinmedi: **0 silinen**) |
| Sonuç | Başarılı — 767 ekleme, 164 güncelleme |
| Doğrulama | `https://otelturizm.com` ve `/oteller` → 200, yeni CSS/layout aktif |

**Not:** Publish paketine proje kökündeki `_publish-verify` klasörü yanlışlıkla dahil olmuştu; sonraki publish öncesi bu klasör hariç tutulmalı veya silinmeli.

---

## 0) Yeni bilgisayar — repoyu çekme ve local kurulum

Repo **public**: `https://github.com/irmhro/otelturizm.git`

```powershell
git clone https://github.com/irmhro/otelturizm.git D:\otelturizm
cd D:\otelturizm
dotnet restore "D:\otelturizm\otelturizm.csproj"
```

Gereksinimler: .NET SDK (proje sürümü), SQL Server LocalDB, Web Deploy V3 (canlı publish için), `sqlcmd`.

### Local veritabanı (ilk kurulum)

**Seçenek A — yedekten restore (önerilen, hızlı):**

```powershell
powershell -ExecutionPolicy Bypass -File "D:\otelturizm\Database\Backups\20260606_localdb_full\restore-otelturizm_2026db.ps1"
```

Yedek klasörü: `Database/Backups/20260606_localdb_full/` (`otelturizm_2026db.bak`)

**Seçenek B — boş DB + tüm migration scriptleri:**

```powershell
powershell -ExecutionPolicy Bypass -File "D:\otelturizm\tools\Db\apply_local_database.ps1"
```

### Local demo hesap şifreleri

Kaynak: `şifreleri.txt` — reset-demo-db sonrası tüm demo hesaplar **`908155`**:

| Rol | E-posta |
|-----|---------|
| Admin | `irmhro0@gmail.com` |
| Satış | `irmhro0+satis@gmail.com` |
| Kullanıcı | `irmhro0+user@gmail.com` |
| Firma | `irmhro0+firma@gmail.com` |
| Partner/Kurumsal | `irmhro0+kurumsal@gmail.com` |

Giriş: `https://localhost:7223/kullanici-giris` (kullanıcı/firma/satis), `https://localhost:7223/partner-giris`, `https://localhost:7223/admin-giris`

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

### Yöntem B — Publish klasörü + MSDeploy (canlıda mevcut dosyalara dokunmadan)

```powershell
$out = "D:\otelturizm\artifacts\live-deploy-$(Get-Date -Format 'yyyyMMdd')"
if (Test-Path $out) { Remove-Item -Recurse -Force $out }
dotnet publish "D:\otelturizm\otelturizm.csproj" -c Release -o $out
```

```powershell
$out = "D:\otelturizm\artifacts\live-deploy-20260606"
$pass = "uaD1pBe0Bp9q"

& "C:\Program Files\IIS\Microsoft Web Deploy V3\msdeploy.exe" `
  -verb:sync `
  -source:contentPath="$out" `
  -dest:contentPath="otelturizm.com",computerName="https://185.111.244.246:8172/msdeploy.axd?site=otelturizm.com",userName="administrator",password="$pass",authType="Basic" `
  -allowUntrusted `
  -enableRule:AppOffline `
  -enableRule:DoNotDeleteRule `
  -skip:objectName=dirPath,absolutePath="wwwroot\\uploads" `
  -skip:objectName=dirPath,absolutePath="App_Data\\logs"
```

**Önemli:** `DoNotDeleteRule` sunucuda pakette olmayan dosyaları silmez. Sunucuda başka dosyalar varsa güvenli yöntem budur. Tam senkron (fazla dosyaları silme) için `IISProfile.pubxml` kullanılır (`RemoveAdditionalFilesFromDestination=true`).

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

### Son oturumda eklenen scriptler (2026-06-06)

Önce yedek, sonra sırayla:

```powershell
# E-posta servisleri aktif (SMTP sifresi ayri adimda)
sqlcmd -S "185.111.244.246" -d "otelturizm_2026db" -U "sa" -P "Nusret.34.34.-" -I -f 65001 -b -i "D:\otelturizm\Database\MigrationsSql\veri\migrationlar\20260606_seed_enable_eposta_servisleri.sql"

# Canli test kullanicisi (irmhro0+user@gmail.com) e-posta/telefon dogrulama + aktif
sqlcmd -S "185.111.244.246" -d "otelturizm_2026db" -U "sa" -P "Nusret.34.34.-" -I -f 65001 -b -i "D:\otelturizm\Database\MigrationsSql\veri\migrationlar\20260606_seed_activate_irmhro0_user.sql"
```

Önceki oturum (örnek):

```powershell
sqlcmd -S "185.111.244.246" -d "otelturizm_2026db" -U "sa" -P "Nusret.34.34.-" -I -f 65001 -b -i "D:\otelturizm\Database\MigrationsSql\tablo\migrationlar\20260605_sqlserver_ozel_gunler.sql"
sqlcmd -S "185.111.244.246" -d "otelturizm_2026db" -U "sa" -P "Nusret.34.34.-" -I -f 65001 -b -i "D:\otelturizm\Database\MigrationsSql\veri\migrationlar\20260605_seed_ozel_gunler.sql"
sqlcmd -S "185.111.244.246" -d "otelturizm_2026db" -U "sa" -P "Nusret.34.34.-" -I -f 65001 -b -i "D:\otelturizm\Database\MigrationsSql\veri\migrationlar\20260605_seed_ozel_gunler_cleanup_sensitive.sql"
```

Doğrulama:

```sql
SELECT COUNT(*) FROM dbo.OZEL_GUNLER;
SELECT TOP 5 GUN_KODU, GUN_ADI, AKTIF_MI FROM dbo.OZEL_GUNLER ORDER BY SIRALAMA;
SELECT ID, SERVIS_KODU, AKTIF_MI, GONDEREN_EPOSTA FROM dbo.EPOSTA_SERVISLERI ORDER BY ID;
```

### appsettings (canlı)

- `Database:RunMigrationsOnStartup` → **`false`** (otomatik migration kapalı)
- `DevelopmentGate:Enabled` → **`false`**
- `ASPNETCORE_ENVIRONMENT` → **`Production`**

---

## 3b) E-posta / SMTP (canlı gönderim)

Sunucu: `umay.muvhost.com` — port **465** (SSL/TLS) veya **587** (STARTTLS).

Posta kutuları (`EPOSTA_SERVISLERI` → `platform_*` kodları): `bildiri@`, `bilgi@`, `guvenlik@`, `info@`, `odeme@`, `rezervasyon@` @otelturizm.com

**Sorun giderme:** Servisler `AKTIF_MI=0` veya `SMTP_SIFRE` boşsa kuyruk işlenmez (`BILDIRIM_LOGLARI` → Beklemede).

### SMTP şifresini canlıya yazma

**Yöntem 1 — PowerShell (tüm platform_* servisleri):**

```powershell
powershell -ExecutionPolicy Bypass -File "D:\otelturizm\tools\Email\apply-platform-smtp-password.ps1" -SmtpPassword "MUVHOST_POSTA_KUTUSU_SIFRESI"
```

**Yöntem 2 — appsettings (sunucuda veya publish paketinde):**

`appsettings.Production.json` → `Email:SharedMailboxPassword` (tüm platform kutuları ortak şifre kullanıyorsa)

**Yöntem 3 — Admin panel:** `/admin/mail-merkezi` → servis başına SMTP şifresi

Local geliştirmede pickup / test modu: `appsettings.Development.json` → `Email` bölümü; `EmailDeliveryBackgroundService` önce DB şifresine, sonra `Email:SmtpPasswords:{kod}` ve `SharedMailboxPassword` değerlerine bakar.

Detay: `Dokumanlar/Guvenlik/eposta-hesaplari.md`, `Dokumanlar/Guvenlik/mail-servisi-yapilandirmasi.md`

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
- [ ] GitHub commit + push (canlıya çıkmadan önce veya hemen sonra)
- [ ] E-posta kuyruğu (Admin → Mail Merkezi veya `SELECT TOP 5 * FROM BILDIRIM_LOGLARI ORDER BY ID DESC`)

---

## 5) GitHub — commit ve push

Canlı deploy **öncesi veya sonrası** değişiklikler `master` branch'e push edilir; yeni bilgisayarda `git pull` ile alınır.

```powershell
cd D:\otelturizm
git status --short
git add -- Services/ Views/ wwwroot/ Database/MigrationsSql/ tools/ appsettings*.json canlıdeploy.md
git commit -m "feat: sozlesme/eposta-dogrula UI, e-posta servisleri ve canli deploy rehberi"
git push origin master
```

Yeni bilgisayarda güncelleme:

```powershell
cd D:\otelturizm
git pull origin master
dotnet build "D:\otelturizm\otelturizm.csproj" --no-restore
```

---

## Tam operasyon sırası (canlıya aktar)

1. Local build + test (`dotnet build`, tarayıcı)
2. Gerekli SQL scriptleri localde dene (`apply_local_database.ps1` veya tek script)
3. **Git commit + push** → GitHub
4. `dotnet publish` → MSDeploy (uploads/logs korunur)
5. Canlı SQL: yedek → migration scriptleri (tek tek)
6. SMTP şifresi eksikse `apply-platform-smtp-password.ps1` veya Mail Merkezi
7. HTTPS smoke test + log kontrolü

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
| `tools/Email/apply-platform-smtp-password.ps1` | Canlı SMTP şifresi (platform_* servisleri) |
| `Database/Backups/20260606_localdb_full/` | LocalDB tam yedek + restore script |
| `şifreleri.txt` | Local demo hesap şifreleri |
| `Docs/DEPLOY_ACIL_500_VE_GORUNUR_GELISTIRME.md` | 500 / eksik publish sorunları |
| `tools/Release/BLUE-GREEN-DEPLOYMENT.md` | İleri seviye slot deploy |

---

## Operasyon özeti

| Komut | Anlam |
|-------|--------|
| **canlıya yükle** | Dosya + görsel + gerekli DB kayıtları eksiksiz senkron |
| **canlıya aktar** | Önce GitHub, sonra canlı dosya + DB |

**Son güncelleme:** 2026-06-06 — yeni bilgisayar kurulumu, e-posta/SMTP adımları, 20260606 DB seed scriptleri, tam operasyon sırası ve GitHub akışı eklendi.
