# Platform siber güvenlik planı ve uygulanan önlemler

> **Not:** Hiçbir canlı sistem “sıfır zafiyet” garantisi veremez. Bu belge savunma katmanlarını, bilinen riskleri ve operasyonel önerileri özetler.

## Mevcut mimari özet

| Katman | Uygulama |
|--------|-----------|
| Oturum | Çerez tabanlı kimlik doğrulama (`Otelturizm.Auth`), HttpOnly, üretimde Secure |
| CSRF | Global `AutoValidateAntiforgeryToken`, ayrı antiforgery çerezi (`SameSite=Strict`), JSON API’lerde bilinçli `ValidateAntiForgeryToken` veya uç özel rate limit |
| CSP / başlıklar | `Content-Security-Policy` (rapor + isteğe bağlı enforce nonce), `X-Frame-Options`, `X-Content-Type-Options`, `Referrer-Policy`, `Permissions-Policy` |
| Oran sınırlama | `public-burst`, `quote-strict`, `growth-ingest`, `presence-beat`, `csp-ingest`, rezervasyon/konum vb. |
| Katalog API | Çoğu uç anonim + rate limit (kamuya açık fiyat/arama); webhook imza doğrulaması (`PhoneVerification`) |
| Sağlık uçları | `/health/live`, `/health/ready`, `/health/platform`; `health-probe` rate limit; platform raporu üretimde kısaltılmış JSON |
| İstemci hata telemetrisi | `/diagnostics/client-error` + `client-error-reporter.js` (örneklem + dedup); `SECURITY_EVENT kind=CLIENT_JS_ERROR` log satırı |
| Öznitelikli API rotaları | `MapControllers()` ile `/rum/vitals`, `/growth/events`, `/diagnostics/*` uçlarının yönlendirme keşfi |

## Tespit edilen riskler ve giderimler (bu çalışma)

| # | Risk | Giderim |
|---|------|---------|
| 1 | `/health/ready` yanıtında veritabanı hata metni (bilgi sızdırma) | Üretimde `error` alanı kaldırıldı; yalnızca geliştirme ortamında ayrıntı |
| 2 | `X-Correlation-Id` serbest metin (log/başlık enjeksiyonu) | Yalnızca güvenli karakter seti kabul edilir |
| 3 | Favori API 500 gövdesinde `ex.Message` | Sunucuda loglanır; istemciye genel mesaj |
| 4 | Fiyat toplu API: sınırsız `HotelIds` / tarih aralığı | En fazla 150 otel, en fazla 400 günlük aralık; `quote-strict` rate limit |
| 5 | Otel presence: sınırsız `tabId` anahtarı (bellek basıncı) | TabId uzunluk sınırı; otel başına ~400 aktif sekme tavanı + `presence-beat` |
| 6 | RUM/CSP rapor uçları spam | `growth-ingest` (RUM), `csp-ingest` (CSP rapor) politikaları |
| 7 | Header bildirim `MarkAsRead` null gövde | Null ve `ItemKeys` doğrulaması |
| 8 | Tüm sayfalara `X-Robots-Tag: noindex` (SEO / sayfa sağlığı) | Yalnızca `/panel`, `/admin`, giriş rotaları, `gelisim`, `secure-files`, `paneltema`, `development` vb. |
| 9 | `/health/platform` varsayılan raporda istisna / uzun açıklama sızıntısı | `HealthReportJsonWriter` + `Security:HealthChecksExposeDetails` (geliştirmede veya `true` iken ayrıntılı) |
| 10 | SQL health check sonucunda istisna nesnesi taşınması | `SqlConnectionHealthCheck` istisnayı yalnızca loglar; `Unhealthy` sabit metin |
| 11 | Sağlık URL’lerinin kötüye kullanımı | `health-probe` politikası (IP başına dakikalık tavan) |

## Yapılandırma (`appsettings`)

```json
"Security": {
  "CspEnforce": false,
  "CspReportEnabled": true,
  "HstsPreload": false,
  "HealthChecksExposeDetails": false
}
```

- **`HealthChecksExposeDetails: true`**: `/health/platform` yanıtında kontrol başına `description` ve süre görünür (üretimde yalnızca gerektiğinde kısa süreli açın).

- **`CspEnforce: true`**: Üretimde nonce ile uyum doğrulandıktan sonra açılmalı; aksi halde satır içi scriptler kırılabilir.
- **Bağlantı dizeleri / API anahtarları**: Depoda düz metin **olmamalı**. Ortam değişkeni (`ConnectionStrings__DefaultConnection`), Azure Key Vault veya User Secrets kullanın; depodaki parolaları **döndürün** (rotate).

## Operasyonel kontrol listesi

1. Üretimde HTTPS + `UseHsts` (gerekirse `Security:HstsPreload`).
2. CSP raporlarını izleyin; gürültü azalınca `CspEnforce` planlı açılış.
3. Rate limit eşiklerini trafik profiline göre gözden geçirin.
4. Düzenli bağımlılık ve framework güvenlik güncellemeleri.
5. Yönetim panelleri için RBAC ve denetim günlükleri (mevcut admin RBAC ile uyumlu).
6. Panellerde mobil: `ViewData["PageCssPath"]` ile sayfa CSS’i; aynı ada sahip `*.mobile.css` dosyası `max-width: 900px` ile yüklenir — eksik mobil dosyası olan sayfalar için `wwwroot/assets/css/paneller/...` altında eşleşen dosya ekleyin.

## Operasyon uçları (özet)

| URL | Amaç |
|-----|------|
| `GET /health/live` | Süreç ayakta (DB kontrolü yok) |
| `GET /health/ready` | DB ping; orchestrator hazırlık |
| `GET /health/platform` | Kayıtlı health check’lerin özeti |

## Doğrulama

`dotnet build "D:\otelturizm\otelturizmnew.csproj" --no-restore`
