## Release Checklist (Paket 150)

### 0) Go-live (Paket 200)

- Ayrıntılı: `tools/Release/GO-LIVE-CHECKLIST.md`

### 0.1) Deploy / operasyon (Paket 191–199)

- Publish standardı: `tools/Release/DOTNET-PUBLISH-PROD.md`
- Canlı migration: `tools/Release/DB-MIGRATIONS-PROD.md`
- Secret’lar: `tools/Release/SECRETS-AND-CONFIG-PROD.md`
- Reverse proxy: `tools/Release/REVERSE-PROXY-HEADERS.md`
- Log / disk: `tools/Release/LOGGING-AND-DISK.md`
- SQL yedek: `tools/Release/SQL-BACKUP-RESTORE.md`
- Olay müdahale: `tools/Release/INCIDENT-RUNBOOK.md`
- Performans regresyon: `tools/Release/PERF-REGRESSION-CHECKLIST.md`
- Güvenlik regresyon: `tools/Release/SECURITY-REGRESSION-CHECKLIST.md`

### 1) Build / Publish

- `dotnet build -c Release`
- `tools/Health/Verify-Publish-ViewCompilation.ps1`
- Üretim klasör publish: `dotnet publish -c Release /p:PublishProfile=FolderProfile-Release-Prod` (bkz. `DOTNET-PUBLISH-PROD.md`)

### 2) Sağlık / Smoke

- `tools/Health/Extract-InternalRoutesFromViews.ps1`
- `tools/Health/Generate-SmokeRoutesList.ps1`
- Uygulama ayakta iken:
  - `tools/Health/Run-SmokeRoutes.ps1 -BaseUrl https://localhost:7223`
- Header/Footer linkleri:
  - `tools/Health/Check-HeaderFooter-Links.ps1 -BaseUrl https://localhost:7223`

### 3) Static asset doğrulama

- `tools/Health/Scan-Broken-AssetReferences.ps1`

### 4) HTML semantik hızlı audit

- `tools/Health/Html-Semantic-Audit.ps1`

### 5) Security / CSP

- CSP rapor özeti:
  - `tools/Security/Summarize-CspReports.ps1 -LogRoot d:\otelturizm\App_Data`

### 5.1) Global Readiness (Locale/Currency)

- Locale switch testi: `?lang=` + `/locale/set` + cookie akışı (tr-TR/en-US/en-GB/de-DE/fr-FR/es-ES)
- Currency switch testi: `/currency/set` cookie yazıyor mu (TRY/USD/EUR/GBP)
- Hreflang doğrulama: sayfada tüm locale linkleri var mı (parametreli sayfalarda path korunuyor mu)
- Sitemap locale alternates: sitemap xml içinde `xhtml:link` hreflang’ler var mı

### 6) Mobil

- `tools/Ui/MOBILE-VIEWPORT-AUDIT.md` checklist’ini kritik sayfalarda uygula.

### 7) Operasyon

- `Uploads:OrphanCleanupEnabled` prod’da **kapalı** başlat (ilk deploy), sonra kontrollü aç.

