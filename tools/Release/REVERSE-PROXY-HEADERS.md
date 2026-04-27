## Paket 194 — IIS / reverse proxy forwarded headers checklist

### Uygulama tarafı

`Program.cs` içinde `ForwardedHeaders` (X-Forwarded-For, X-Forwarded-Proto) kullanılıyor; sıra: `UseForwardedHeaders()` genelde erken pipeline’da olmalıdır.

### IIS / ARR / NGINX

- Gerçek istemci IP’si için proxy `X-Forwarded-For` eklemeli.
- HTTPS sonlandırma için `X-Forwarded-Proto: https` doğru iletilmeli.
- Bilinen proxy ağı dışında gelen spoof başlıklarına güvenilmez: mümkünse **KnownNetworks / KnownProxies** sıkılaştırılır.

### Doğrulama

- [ ] `Request.Scheme` ve `Request.Host` prod’da doğru (HTTPS linkleri, cookie Secure).
- [ ] Rate limit / log IP’leri beklenen aralıkta.
- [ ] Admin health ve gerçek kullanıcı isteğinde `X-Forwarded-*` ile uyumlu test.
