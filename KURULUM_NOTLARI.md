# Otelturizmnew Kurulum Notlari

Bu dosya, projeyi baska bir bilgisayarda eksiksiz calistirmak icin gerekli tum programlari, ayarlari ve veritabani geri yukleme adimlarini icerir.

## 1) Gerekli Programlar

- Git (son surum)
- Visual Studio 2022 (17.10+ onerilir)
- .NET SDK 10.0.x
- SQL Server (Express veya Developer) + LocalDB (opsiyonel)
- SQL Server Management Studio (SSMS)
- (Opsiyonel) Laragon / IIS Express / Kestrel

## 2) Visual Studio Kurulumunda Secilecek Workload ve Bilesenler

### Workload
- ASP.NET and web development

### Individual Components (onerilen)
- .NET 10 SDK
- .NET 10 Runtime
- IIS Express
- SQL Server Data Tools (varsa)

## 3) Projedeki NuGet Paketleri

`otelturizmnew.csproj` icerigine gore:

- `MailKit` `4.15.1`
- `Microsoft.Data.SqlClient` `7.0.0`
- `SixLabors.ImageSharp` `3.1.12`

Not: Proje acildiginda `dotnet restore` veya Visual Studio restore ile otomatik yuklenir.

## 4) Veritabani Tam Yedek Dosyasi

Bu projeye alinan tam yedek:

- `Database/Backups/otelturizm_2026db_full_20260416_184910.bak`

## 5) SSMS ile Veritabani Geri Yukleme

1. SSMS acin.
2. `Databases` uzerine sag tik -> `Restore Database...`
3. `Device` secin -> `.bak` dosyasini gosterin:
   - `Database/Backups/otelturizm_2026db_full_20260416_184910.bak`
4. Hedef veritabani adini girin (onerilen): `otelturizm_2026db`
5. `Options` sekmesinde gerekirse:
   - `Overwrite the existing database (WITH REPLACE)` isaretleyin.
6. `OK` ile restore edin.

## 6) appsettings Baglanti Ayari

`appsettings.json` / `appsettings.Development.json` icinde:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=otelturizm_2026db;Trusted_Connection=True;TrustServerCertificate=True;"
}
```

Eger LocalDB yerine SQL Express kullanilacaksa:

```json
"DefaultConnection": "Server=.\\SQLEXPRESS;Database=otelturizm_2026db;Trusted_Connection=True;TrustServerCertificate=True;"
```

## 7) Projeyi Calistirma

Terminal:

```powershell
dotnet restore
dotnet build
dotnet run
```

Visual Studio:

1. Cozumu ac.
2. Startup project `otelturizmnew` sec.
3. `https` profili ile F5.

## 8) E-posta Ayarlari (Onemli)

E-posta gonderimi `email_services` tablosundaki aktif SMTP kaydina baglidir.
Yeni bilgisayarda SMTP erisimi icin:

- Host/port firewall acik olmali (`465` veya `587`)
- DNS cozumleme calismali
- Gerekirse SMTP saglayicisinda IP allowlist yapilmali

## 9) Hizli Kontrol Listesi

- [ ] Visual Studio + .NET 10 kuruldu
- [ ] SSMS kuruldu
- [ ] Veritabani `.bak` dosyasindan restore edildi
- [ ] `appsettings` baglanti stringi dogrulandi
- [ ] `dotnet restore/build/run` hatasiz
- [ ] Login / Register / Forgot Password / Verify Email sayfalari aciliyor

