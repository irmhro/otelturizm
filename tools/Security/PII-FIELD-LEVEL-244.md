# PII alan düzeyi koruma — paket 244

## İlk savunma

- Aktarımda HTTPS, oturum çerezi `HttpOnly`, CSRF kritik formlarda zorunlu.
- Dosya ve kişisel veri: mevcut yükleme denetimi ve güvenli dosya URL’leri.

## DB’de şifreli kolon hedefi

1. Hassas kolonları belirle (Telefon, TCKN vb.).
2. ASP.NET **DataProtection** ile uygulama içi şifreleme veya SQL Always Encrypted (operasyon kararı).
3. Okuma/yazma tek repository katmanından; loglarda PII maskeleme.

Üretim öncesi DBA + KVKK birlikte onayı önerilir.
