## Paket 193 — appsettings ve secret yönetimi (prod)

### İlkeler

- **Repo içinde** üretim parolası, API anahtarı veya tam SQL bağlantı dizisi tutulmaz.
- Prod’da değerler **ortam değişkenleri** veya güvenli mağaza (Azure Key Vault, IIS ortamı, Kubernetes Secret) ile verilir.

### ASP.NET Core isimlendirme

Örnek ortam değişkenleri:

| Ayar | Ortam değişkeni |
|------|------------------|
| `ConnectionStrings:DefaultConnection` | `ConnectionStrings__DefaultConnection` |

### Yerel geliştirme

- `appsettings.Development.json` → LocalDB (örnek).
- Uzak DB gerekiyorsa: `dotnet user-secrets set ConnectionStrings:DefaultConnection "..."` (User Secrets IDE ile bağlanır).

### Kontrol listesi

- [ ] Prod sunucuda IIS / systemd ortamında connection string tanımlı
- [ ] `web.config` repoda secret içermez; IIS “Application Settings” veya sunucu ortamında `ConnectionStrings__DefaultConnection` (ve gerekirse `ConnectionStrings__dbbaglan`) tanımlayın
- [ ] SMTP / WhatsApp / ödeme anahtarları yalnızca güvenli kanaldan
- [ ] `appsettings.Production.json` içinde düz metin secret yok
