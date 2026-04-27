## Paket 200 — Go-live checklist (smoke + SEO + mail + ödeme)

### Smoke

- [ ] `Generate-SmokeRoutesList.ps1` + `Run-SmokeRoutes.ps1 -BaseUrl https://PROD`
- [ ] Header/footer link kontrolü (`Check-HeaderFooter-Links.ps1`)
- [ ] Kırık statik asset taraması

### SEO

- [ ] Canonical / meta / hreflang kritik sayfalar
- [ ] `robots.txt` ve sitemap erişimi
- [ ] 404 sayfası ve status code handling

### E-posta

- [ ] Test modu kapalı (prod)
- [ ] Kuyruk işleniyor (`/admin/email-kuyruk`)
- [ ] Şablonlar TR/EN ve örnek bir transactional mail uçtan uca

### Ödeme / finans

- [ ] Ödeme sağlayıcısı prod anahtarları yalnızca güvenli kanalda
- [ ] Test kartı / sandbox kapalı (prod’da)
- [ ] Komisyon ve fatura akışı için kritik tablolar migration tamam

### Son adım

- [ ] `RELEASE-CHECKLIST.md` + bu dosya tamamlandı
- [ ] Rollback: önceki artifact + DB yedeği hazır
