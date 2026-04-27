## Paket 199 — Güvenlik regresyon checklist’i (CSP / CSRF / AuthZ)

### CSP

- [ ] CSP ihlal raporu özeti (`tools/Security/Summarize-CspReports.ps1`)
- [ ] Inline script nonce kullanımı bozulmadı

### CSRF

- [ ] Form POST’larında antiforgery token (admin kritik aksiyonlar dahil)

### AuthZ

- [ ] Panel route’ları `[Authorize]` ve rol kontrolleri
- [ ] Admin kritik aksiyonlar: gerekçe + audit log

### Oturum / çerez

- [ ] HTTPS prod’da zorunlu; `Secure` cookie davranışı beklenen

### Dosya yükleme

- [ ] Boyut limitleri ve MIME/magic kontrolleri kritik endpoint’lerde aktif
