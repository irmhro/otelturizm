## Paket 198 — Performans regresyon checklist’i

### Yayın öncesi

- [ ] `dotnet build -c Release`
- [ ] View compile publish doğrulaması (`Verify-Publish-ViewCompilation.ps1`)
- [ ] Ana sayfa / liste / detay: LCP ve kritik CSS/JS yükü (manuel veya Lighthouse örneği)

### Yayın sonrası

- [ ] `/health/ready` süresi makul (< birkaç saniye).
- [ ] Yavaş istek loglarında ani artış yok.
- [ ] OutputCache kritik içerik güncellemelerinde invalidate edildi mi?
