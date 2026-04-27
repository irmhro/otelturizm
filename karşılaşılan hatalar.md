# Karşılaşılan Hatalar ve Çözüm Yönergeleri

Bu dosya, geliştirme sırasında karşılaşılan kritik hataları ve **kalıcı çözüm adımlarını** kayıt altına almak için tutulur.

## 2026-04-27 — Anasayfa/CSS yüklenmiyor (Chrome) → `net::ERR_HTTP2_PROTOCOL_ERROR`

### Belirti
- `https://localhost:7223/` anasayfa “bozuk” görünür.
- Network sekmesinde CSS/JS istekleri `(failed) net::ERR_HTTP2_PROTOCOL_ERROR` ile düşer.

### Kök neden (tipik)
- Dev ortamında HTTPS üzerinde **HTTP/2 bağlantı pazarlığı** bazı Windows/Chrome kombinasyonlarında reset olabiliyor ve statik asset istekleri düşüyor.

### Kalıcı çözüm (projeye işlendi)
- Development ortamında HTTPS için **HTTP/1.1’e sabitle**:
  - `Program.cs`: Development’ta endpoint default protokolünü `Http1` yap.
  - `appsettings.Development.json`: `Kestrel:EndpointDefaults:Protocols = "Http1"` ekle.

### Doğrulama
- Chrome DevTools → Network → “Protocol” sütununda istekler **`http/1.1`** görünmeli.

---

## 2026-04-27 — Otel listeleme sayfaları 500 → `SqlException: Incorrect syntax near the keyword 'CASE'.`

### Belirti
- `https://localhost:7223/Oteller`
- `https://localhost:7223/oteller/istanbul?filter=campaign`
- `https://localhost:7223/oteller/istanbul?filter=budget`
  istekleri 500 verir.
- Hata: `SqlException: Incorrect syntax near the keyword 'CASE'.`

### Kök neden
- SQL Server’da `CONTAINS()` fonksiyonu içinde `CASE WHEN ... THEN ... ELSE ... END` ifadesi kullanılamaz.
- Örn. `CONTAINS(CASE WHEN ... THEN col1 ELSE col2 END, @q)` **geçersiz sentaks** üretir.

### Kalıcı çözüm (projeye işlendi)
- `CONTAINS(CASE WHEN ...)` yerine bayrağa göre iki ayrı `CONTAINS` çağrısını `OR` ile bağla:
  - `(@hasFtsSearchText=1 AND CONTAINS(o.fts_search_text, @ftsQuery))`
  - `(@hasFtsSearchText=0 AND CONTAINS(o.otel_adi, @ftsQuery))`

### Doğrulama
- İlgili URL’ler 200 dönmeli ve listeleme çalışmalı.
- `dotnet build` başarılı olmalı.

