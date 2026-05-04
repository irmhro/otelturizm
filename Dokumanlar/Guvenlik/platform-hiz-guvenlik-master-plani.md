# Platform Hız + Güvenlik Master Planı (Faz 0→2)

Bu doküman; **yük altında (onbinlerce eşzamanlı)** performansın stabil kalması, dönüşüm/ikna akışlarının hızlanması ve güvenlik risklerinin “0’a yakın” seviyeye indirilmesi için uygulanacak adımları toplar.

## Faz 0 (hemen – temel sertleştirme / hızlı kazanımlar)

### Uygulama katmanı (Web)
- **Response compression** (Brotli+Gzip): HTML/CSS/JS/JSON sıkıştırma ile bant genişliği ve TTFB düşürme.
- **Rate limiting**: IP bazlı sabit pencere limitleri (genel trafik + auth + fiyat teklifi gibi yoğun endpoint’ler).
- **Forwarded headers**: Reverse proxy/IIS arkasında doğru protokol/IP okuma.
- **Cookie sertleştirme**: Prod’da `Secure=Always`, CSRF cookie secure policy, auth cookie secure policy.
- **Security headers**: CSP + nosniff + frame-ancestors + referrer-policy + permissions-policy.

### Veri katmanı (SQL Server)
- **Otel listeleme hız indeksleri**: `oteller` üzerinde (yayin_durumu, onay_durumu, sehir/ilce/mahalle/otel_adi) odaklı index paketleri.
- **Email queue indeksleri**: `bildirim_loglari` üzerinde (tur, durum, olusturulma_tarihi) claim sorgusu için index.
- **Pricing tabloları indeksleri**: `oda_fiyat_musaitlik` + `firma_oda_fiyat_musaitlik` yoğun okuma/yazma desenlerine uygun unique+include indexler.

### Cache (servis katmanı)
- **Listing cache (kısa TTL)**: Kullanıcıya özel olmayan “ham liste” 45sn cache; favori durumu controller’da kullanıcıya göre uygulanır.
- **Detail cache (kısa TTL)**: 2dk absolute + 30sn sliding (zaten mevcut) ve clone ile “paylaşımlı nesne mutasyonu” önleme.

## Faz 1 (yük altında ölçek – 1–2 hafta)

### SQL “SARGable arama” (kritik)
- `BuildSearchNormalizationSql(...)` ile kolon üzerinde fonksiyon uygulanan aramalar **index kullanamaz**.
- Çözüm: `oteller` tablosuna **persisted computed** veya fiziksel `*_normalized` kolonlar eklenmesi, bunlara index.
  - Örn: `sehir_normalized`, `ilce_normalized`, `mahalle_normalized`, `otel_adi_normalized`
  - Arama sorguları `WHERE sehir_normalized = @term` gibi sargable hale gelir.

### Output caching (public GET)
- `/oteller`, `/kampanyalar` gibi public GET sayfalarında; user-cookie vary’si doğru yönetilerek output cache uygulanabilir.
- Favori/oturum gibi kullanıcı bağımlı kısımlar “edge”de değil, client-side fetch veya ayrı endpoint ile çözülebilir.

### Kuyruklar / background işler
- E-posta kuyruğu claim mekanizması (UPDLOCK/READPAST) mevcut; indeks + backoff + maksimum deneme politikası netleştirilecek.
- “Aynı rezervasyon için tekrar mail” deduplikasyon (idempotency key) eklenebilir.

## Faz 2 (kurumsal güvenlik + observability – 2–4 hafta)

### Güvenlik
- Upload güvenliği: içerik sniffing, MIME doğrulama, boyut limit, zararlı dosya bloklama, antivirus hook (opsiyon).
- AuthZ/RBAC: admin/panel endpoint’lerinde policy bazlı yetki (tam audit log ile).
- CSP “unsafe-inline” azaltma: inline script/style kademeli kaldırma (nonce/hash).
- WAF/Reverse proxy: IP reputation + bot mitigation + geo throttling (altyapı).

### Observability
- İstek bazlı latency histogram, slow query log, background job sağlık metriği.
- “System Health” sayfasını (admin) canlı metriklerle genişletme.

## Bu dokümandaki son değişiklikler
- 2026-04-27: Faz‑0 uygulama sertleştirmeleri + ilk indeks paketleri + listing cache eklendi.

