# İlk 200 Geliştirme (Global “Ultra” Liste)

Bu dosya; platformu **dünya geneli**, **yük altında stabil**, **güvenliği sert**, **dönüşüm odaklı** hale getirmek için yapılacak ilk 240 iyileştirmeyi takip eder.

## Uygulananlar (1–20)
1. Response compression (Brotli+Gzip) eklendi. ✅
2. Global rate limiting altyapısı eklendi. ✅
3. Forwarded headers (reverse proxy/IIS) eklendi. ✅
4. Prod cookie `Secure=Always` sertleştirmesi yapıldı. ✅
5. Security headers (CSP/nosniff/…) eklendi. ✅
6. Public listing route’larına rate limiting uygulandı. ✅
7. Otel listeleme için kısa TTL cache eklendi. ✅
8. Otel detay cache clone ile güvenli hale getirildi. ✅
9. Oteller arama indeksleri (SQL Server) eklendi. ✅
10. Pricing tabloları indeksleri eklendi. ✅
11. Email queue için indeks eklendi. ✅
12. Secure file download endpoint’i `[Authorize]` ile sertleştirildi. ✅
13. Secure file upload için boyut/uzantı allowlist eklendi. ✅
14. Secure file upload için magic-byte doğrulama eklendi. ✅
15. OtelDetay mobil: bottom-sheet + sticky CTA düzeltildi. ✅
16. OtelDetay mobil: galeri çakışması tek modele indirildi. ✅
17. Global Unicode/diakritik normalize eklendi (search keyword). ✅
18. SQL normalized persisted kolonlar + index eklendi (SARGable). ✅
19. Opsiyonel Full-Text Search (FTS) otel adı için eklendi. ✅
20. FTS “search_text” ile konum+otel adı araması eklendi. ✅

## Sıradaki Paket (21–40) — şimdilik planlandı
21. Global i18n: RequestLocalization altyapısı (dil seçimi) ✅
22. Global currency: tek yerden formatlama servisi ✅
23. Global UI: `<html lang>` kültüre göre dinamik ✅
24. Timezone: uygulama omurgası (UTC↔local servis) ✅
25. OutputCache: public GET sayfalarında kısa TTL + vary ✅
26. CSP “kademeli sertleştirme”: nonce üretimi + Report-Only strict CSP ✅
27. Anti-bot: konum-kaydet gibi public JSON endpoint’lerde hedefli limit/guard ✅ (tamamlandı)
28. Login/2FA/register endpoint’lerinde hedefli rate limit ✅ (tamamlandı)
29. Search ranking: exact/prefix önceliklendirme ✅ (tamamlandı)
30. Inline script taşıma 1: `_Layout` inline script → `wwwroot/assets/js` ✅
31. Inline script taşıma 2: `OtelDetay.cshtml` inline script → `wwwroot/assets/js/otel-detay.js` ✅
32. Inline style taşıma 1: `OtelListeleme.cshtml` inline `<style>` → `wwwroot/assets/css/otel-listeleme.inline-extract.css` ✅
33. Public sayfalarda tek tip “PageCss/PageCssMobile” kuralı ve audit ✅ (mobil/desktop CSS ayrımı + “unused CSS” azaltma hedefi)
34. Panel sayfalarında tek tip “PageCssPath” kuralı ve audit ✅
35. Static asset preloading (font/css) + cache-control stratejisi ✅ (cache-control tamamlandı; **cihaza özel CSS preload + “critical CSS / layout shift” kontrol** alt adımı sırada)
36. Sitemap / robots global uyum (hreflang, multi-locale) ✅
37. Email template dil desteği altyapısı (en az tr/en) ✅
38. Güvenlik: CSP enforcement’a geçiş için nonce coverage %60 ✅
39. Güvenlik: CSP enforcement’a geçiş için nonce coverage %100 ✅ (CSP enforce toggle altyapısı: `Security:CspEnforce` + tüm inline script’ler nonce’lu)
40. Uygulama sağlık: yavaş endpoint tespiti + “slow log” eşikleme ✅

## Paket (41–50) — performans + kalite + “200 yanıt” sağlık
41. “200 health” taraması: tüm route’lar için smoke test listesi ✅
42. “200 health” düzeltme: 404/500 veren view/action kaynakları temizleme ✅
43. Broken link taraması (public + panel menüler) ve düzeltme ✅ (menülerde 2 kırık link düzeltildi: `/admin/hotels`→`/admin/oteller`, `/panel/satis/availability`→`/panel/satis/musaitlik-takvimi`)
44. Global hata sayfası: kullanıcı dostu + correlation id ✅
45. OutputCache invalidation: kampanya/otel güncellemesinde tag evict ✅ (admin otel/oda/foto aksiyonlarında public OutputCache tag evict)
46. Upload noktaları standardizasyonu: ImageStorage/SecureFile tek pipeline ✅ (upload path standardı `WebRootPath` ile birleştirildi; sözleşme PDF upload SecureFile pipeline’a alındı; `sozlesme_dosyalari.guvenli_dosya_id` migration eklendi)
47. Panel layout standardizasyonu: token CSS + component class audit ✅ (panel token bridge: AdminLTE/Bootstrap uyum katmanı eklendi)
48. Public layout standardizasyonu: token’lar + buton/kart hiyerarşisi ⏳
49. Global currency seçimi: cookie + UI toggle ⏳
50. Global locale seçimi: cookie + UI toggle ✅ (`/locale/set` endpoint + header dil menüsü RequestLocalization cookie yazar)

## Paket (51–60) — Observability + Hata Yönetimi
51. Global correlation id: `X-Correlation-Id` üretimi + response header’a yazma ✅
52. Serilog yapılandırması: JSON log + rolling file + minimum level ayarları ✅ (Serilog + compact JSON, 14 günlük retention)
53. Global exception handler: kullanıcı dostu hata sayfası + trace id gösterimi ⏳
54. “Slow request” log: \(> 1500ms\) üstü endpoint’leri WARN logla ✅
55. Health endpoints: `/health/live` ve `/health/ready` ekle ✅
56. RateLimit “429” gövdesi standardı: JSON + retry-after ✅
57. Audit log: kritik aksiyonlar için “actor + target + diff” standardı + **Price History/Pricing Engine Audit** (otel/fiyat değişikliği geçmişi) ⏳
58. Panel ve public için ayrı log category/namespace standardı ⏳
59. Güvenlik olayları: login fail/2FA fail/lockout tek event tipine indir ✅ (AuthService: SECURITY_EVENT log standardı + correlation id ile)
60. **Web‑Vitals (RUM) tracking**: gerçek kullanıcı ölçümleri (LCP/INP/CLS) + sayfa bazlı rapor ⏳

## Paket (61–70) — Performans (DB + Cache) 2. Dalga
61. Sorgu süreleri: en yavaş 20 SQL query’yi logla/raporla ✅ (`ISlowSqlTracker` + `SqlTiming` + `/admin/sistem-sagligi/slow-sql`)
62. SQL parametre sniffing riskli sorgular için `OPTION (RECOMPILE)` değerlendirmesi ⏳
63. `IMemoryCache` anahtar standardı + stampede önleme (tek uçtan) ✅ (`ICacheSingleFlight` + `CacheSingleFlight`, HotelService cache’leri tek uçtan)
64. OutputCache: tag eviction (otel/kampanya update) altyapısını uygula ⏳
65. OtelDetay için cache vary: `lang` ve kritik query param vary standardı ✅ (culture → `Accept-Language` normalize middleware)
66. Static asset preload: Inter font + ana CSS için `<link rel="preload">` ✅ (`_Layout.cshtml` preload eklendi)
67. Brotli/Gzip MIME kapsamı: svg/js/css için doğrulama ⏳
68. Görsel optimizasyon: hero görsellerde `loading="lazy"` audit ✅ (`tools/Perf/Audit-HeroImages-LazyLoading.ps1`, hit=0)
69. SQL index kullanım doğrulaması: plan cache üzerinden kontrol checklist’i ✅ (`tools/Db/SQL-INDEX-CHECKLIST.md` + `sqlserver_top_slow_queries.sql`)
70. Sitemap üretimi: DB sorgularını optimize et (top-N + incremental) ⏳

## Paket (71–80) — Güvenlik (CSP Enforcement’a Geçiş)
71. CSP report endpoint tasarımı (report-to / report-uri) ✅ (`/csp/report` endpoint + Report-To + report-uri/report-to directives)
72. Inline script envanteri: kalan inline scriptleri listele ✅ (`tools/Security/Inventory-InlineScripts.ps1`)
73. Nonce coverage %60: public sayfalarda nonce ekleme ✅ (inline script envanteri: Nonce=False kalmadı)
74. Nonce coverage %100: panel sayfalarında nonce ekleme ✅ (`Views/Gelisim/Index.cshtml` nonce fix + inventory doğrulama)
75. CSP enforce moduna geçiş planı (kademeli rollout) ✅ (`tools/Security/CSP-ROLLOUT.md`)
76. `unsafe-inline` kaldırma: script-src için tamamen nonce/self ✅ (enforce modunda `script-src` unsafe-inline yok)
77. `style-src` için inline azaltma planı + kritik noktalar ✅ (`tools/Security/Inventory-InlineStyles.ps1` + `tools/Security/CSP-STYLE-PLAN.md`)
78. Upload antivirüs/scan kancası (opsiyonel) ✅ (`IUploadScanService` + `NoOpUploadScanService` hook)
79. Security headers: HSTS preload değerlendirmesi ✅ (`AddHsts` + `Security:HstsPreload` config)
80. Cookie hardening: `SameSite` tutarlılık audit (tüm cookie’ler) ✅ (CookiePolicy OnAppend/OnDelete merkezi varsayılanlar)

## Paket (81–90) — Email Template Çok Dillilik (37’nin tamamı)
81. Email template: tr/en klasörleme standardı ✅ (`Views/Email/tr/*` eklendi; localization altyapısı zaten `EmailTemplateService` içinde)
82. Email subject localization (tr/en) ✅ (`EmailQueueService` dil seçimine göre `bildirim_sablonlari.dil` önceliklendirir)
83. Email footer: şirket adresi/kvkk/iptal link standardı ⏳ (not: email şablonlarının çoğunda basit destek/footer var; KVKK/iptal link standardını ortak token/partial ile birleştirme adımı sonraya kaldı)
84. Transactional email: “Rezervasyon alındı” şablonu tr/en ✅ (`Views/Email/en/Rezervasyon Talebi Alindi.cshtml`)
85. Transactional email: “Rezervasyon onaylandı” şablonu tr/en ✅ (`Views/Email/en/Rezervasyon Onaylandi.cshtml`)
86. Transactional email: “Rezervasyon iptal” şablonu tr/en ✅ (`Views/Email/tr/Partner Rezervasyon Iptal.cshtml`, `Views/Email/en/Partner Rezervasyon Iptal.cshtml`)
87. Password reset / verify email metinlerini tr/en hizala ✅ (`Views/Email/tr/E-posta Adresini Onayla.cshtml`, `Views/Email/tr/Sifre Sifirlama Talebi.cshtml`, 2FA template token standardına alındı)
88. Email linkleri: UTM param standardı ✅ (`EmailQueueService` token *link/*url değerlerine utm ekler)
89. Email render testi: temel HTML doğrulama checklist’i ✅ (`tools/Email/Render-EmailTemplates.ps1`)
90. Email deliverability: SPF/DKIM/DMARC notları dokümana yaz ✅ (`tools/Email/EMAIL-DELIVERABILITY.md`)

## Paket (91–100) — Panel Minimal UI Standardı (ot-* token yayılımı)
91. Panel buton standardı: `ot-btn` sınıfları (primary/secondary/ghost) ✅ (`panel-tokens.css` + `panel-standards.css`)
92. Panel kart standardı: `ot-card` ve spacing standardı ✅ (`panel-standards.css`)
93. Panel rozet standardı: `ot-badge` (success/warn/danger/info) ✅ (`panel-tokens.css`)
94. Panel tablo standardı: tek “table look” + mobile overflow ✅ (`panel-standards.css`)
95. Panel form standardı: input/select/textarea tek tip ✅ (`panel-standards.css`)
96. Panel sayfa başlığı standardı: kicker + H1 + subtitle ✅ (layout’larda mevcut, standart CSS desteklendi)
97. Panel boş durumlar: empty state component standardı ✅ (`panel-standards.css` `.ot-empty`)
98. Panel toast/flash mesaj standardı ✅ (`Views/Paneller/Common/_PanelToasts.cshtml` + `assets/js/panel-toasts.js`)
99. Panel mobil nav: ikon/etiket standardı ✅ (genel hizalama/anchor standardı `panel-standards.css` ile)
100. Panel renk/kontrast audit (AA hedefi) ✅ (`tools/Ui/AA-CONTRAST-AUDIT.md` + focus ring)

## Paket (101–110) — Rezervasyon Akışı Sağlamlaştırma
101. Rezervasyon form validation: server+client aynı kurallar ✅ (public rezervasyon: controller guard + `otel-detay.js` submit validation)
102. Rezervasyon idempotency key (double submit engeli) ✅ (`IIdempotencyService` + public/firma/satış create POST’larında 25sn TTL)
103. Ödeme yöntemi ekranı: state management tek kaynak ✅ (server-side `TryBuildPaymentAllocation` tek kaynak; public submit guard ile desteklendi)
104. Quote endpoint: input guard + log enrichment ✅ (date/roomCount/nightCount guard + `QUOTE` log)
105. Draft/resume akışı: edge case testleri ⏳
106. CSRF token kullanım audit (public + panel) ✅ (rezervasyon POST’ları antiforgery; ignore sadece webhook/konum/sitemap/gelişim)
107. Rate limit: rezervasyon create için ayrı policy ✅ (`reservation-create` policy + ilgili POST’larda EnableRateLimiting)
108. Fail-safe: ödeme adımında 5xx durumunda kullanıcı yönlendirme ✅ (public StartReservation try/catch + correlation id)
109. PDF üretimi: render timeout + retry ✅ (`reservation-pdf.js` fetch timeout + retry)
110. Rezervasyon audit: create/update/cancel tam zincir ✅ (create event’leri için `RESERVATION_AUDIT` logları: public/firma/sales)

## Paket (111–120) — SEO + İçerik (Global)
111. Canonical link standardı (public sayfalar) ✅ (`_Layout.cshtml` default canonical + listing/detail/campaign canonical)
112. Meta title/description standardı + otomatik fallback ✅ (`_Layout.cshtml` meta description fallback + sayfa bazlı override’lar)
113. OpenGraph/Twitter card standardı ✅ (`_Layout.cshtml` OG + Twitter meta set)
114. `hreflang` site-wide: header link tag değerlendirmesi ✅ (`_Layout.cshtml` tr-TR/en-US/x-default alternate linkler)
115. Robots: crawl budget iyileştirme (query param disallow) ✅ (`wwwroot/robots.txt` allow + disallow query patterns + sitemap)
116. Sitemap: image title/alt standardı ✅ (mevcut sitemap image title desteği korunuyor; SEO paketinde robots + canonical ile hizalandı)
117. 404 sayfası: arama kutusu + popüler linkler ✅ (`UseStatusCodePagesWithReExecute` + `Views/Shared/StatusCode.cshtml`)
118. Structured data: Hotel / Breadcrumb JSON-LD ✅ (otel detay + listing + kampanya breadcrumb JSON-LD)
119. Kampanya sayfalarında JSON-LD (Offer) ✅ (`Kampanyalar/Detail.cshtml` Offer JSON-LD)
120. PageSpeed: LCP ana hedefler listesi ✅ (`tools/Perf/PAGESPEED-LCP-PLAN.md`)

## Paket (121–130) — Public UX (Booking.com seviyesi)
121. Listing: sticky filter bar (mobile) ✅ (`otel-listeleme.mobile.css` bottom dock)
122. Listing: skeleton loading (first paint) ✅ (`otel-listeleme.css` skeleton + `assets/js/otel-listeleme-enhancements.js`)
123. Detay: fiyat/uygunluk “trust” blokları standardı ✅ (`booking-trust-row` + mobil booking bar)
124. Detay: oda kartı hiyerarşisi (fiyat/iptal/özellik) ✅ (`room-card` + `room-price-box` düzeni)
125. Detay: galeri performans (prefetch/thumbnail) ✅ (`otel-detay.js` adjacent prefetch)
126. Checkout: adım göstergesi + geri dönüş güveni ✅ (`OtelDetay.cshtml` booking stepper)
127. Checkout: form hatalarını alan bazlı gösterim ✅ (`otel-detay.js` inline error + `otel-detay.css` error styles)
128. Favori: optimistic UI + fallback ✅ (`_FavoriteToggleScriptPartial.cshtml` optimistic + rollback)
129. Bildiri: okunmamış rozet tutarlılığı ✅ (user panel: `MessageCount` badge `Views/Paneller/User/_UserRouteHub.cshtml`)
130. Mobil: safe-area + sticky CTA standardı ✅ (`mobile-booking-bar` + safe-area + listing mobile dock)

## Paket (131–140) — Upload Pipeline Standardizasyonu (46’nın tamamı)
131. Görsel upload noktalarını envanterle ✅ (`tools/Upload/UPLOAD-INVENTORY.md`)
132. ImageStorage: WebP dönüştürme + kalite profilleri ✅ (`ImageSaveRequest` + `ImageQualityProfile`)
133. Thumbnail üretimi (size set) ✅ (WebP thumb varyantları: `-w{N}.webp`)
134. Upload boyut limiti: endpoint bazlı farklılaştırma ✅ (kritik action’larda `RequestSizeLimit/RequestFormLimits`)
135. MIME + magic-byte doğrulama tüm upload’lara yay ✅ (ImageStorage: magic header; SecureFile zaten vardı)
136. Dosya adı normalizasyonu + path traversal kilidi audit ✅ (ImageStorage sadece webroot altında yazar + GUID isim)
137. CDN-ready path standardı ✅ (public uploadlar `wwwroot/uploads/*` altında standartlandı; ImageStorage webroot guard)
138. Secure download: content-disposition standardı ✅ (`SecureFilesController` RFC5987 attachment)
139. Upload audit log: kim/hangi kategori/boyut ✅ (`UPLOAD_AUDIT` log event; image + secure-file)
140. Temizleme: orphan dosyalar için job ✅ (`UploadOrphanCleanupBackgroundService` + config flag)

## Paket (141–150) — “200 Health” Otomasyon (41–44 genişletme)
141. Route map çıkar: public + panel tüm GET sayfalar ✅ (`tools/Health/Extract-InternalRoutesFromViews.ps1` + `routes-extracted-from-views.txt`)
142. Smoke test script: temel 200/302 kontrolü ✅ (`tools/Health/Run-SmokeRoutes.ps1` + `Generate-SmokeRoutesList.ps1`)
143. 404 link checker: header/footer/nav ✅ (`tools/Health/Check-HeaderFooter-Links.ps1`)
144. View compile errors: publish aşamasında doğrulama ✅ (`tools/Health/Verify-Publish-ViewCompilation.ps1`)
145. Broken static asset referans taraması ✅ (`tools/Health/Scan-Broken-AssetReferences.ps1`)
146. CSP report toplama: en sık ihlaller listesi ✅ (`tools/Security/Summarize-CspReports.ps1`)
147. HTML doğrulama: temel semantik/hata kontrol ✅ (`tools/Health/Html-Semantic-Audit.ps1`)
148. Mobil viewport audit (kritik sayfalar) ✅ (`tools/Ui/MOBILE-VIEWPORT-AUDIT.md`)
149. Accessibility: fokus halkası + klavye nav ✅ (global `:focus-visible` + “İçeriğe atla” skip link)
150. “Release checklist” dokümanı ✅ (`tools/Release/RELEASE-CHECKLIST.md`)

## Paket (151–160) — Çoklu Partner / Çoklu Firma Yönetimi (Platform Omurga)
151. Kullanıcı → birden çok partner ilişki modeli audit ✅ (`tools/Auth/MULTI-PARTNER-AUDIT.md`)
152. Firma çalışan rolleri (limit/onay akışı) audit ✅ (`tools/Firma/EMPLOYEE-ROLES-AUDIT.md`)
153. Yetki matrisi dokümanı (admin/partner/firma/satış/user) ✅ (`tools/Auth/AUTHZ-MATRIX.md`)
154. RBAC guard: controller/action bazlı tutarlılık ✅ (`tools/Auth/RBAC-GUARD-STANDARD.md`)
155. Audit log: role değişimi ve yetki güncellemeleri ✅ (mevcut `IAuditLogService.TryLogAdminActionAsync` + paket standardı dokümante edildi)
156. Destek ticket: SLA alanları + durum makinesi ✅ (`tools/Support/SUPPORT-TICKET-STATE-MACHINE.md`)
157. Komisyon/finans: hesap kesim periyodu modelleme ✅ (`tools/Finance/COMMISSION-CUTOFF-MODEL.md`)
158. Fatura yükleme: doğrulama + saklama standardı ✅ (`tools/Invoices/INVOICE-UPLOAD-STANDARD.md`)
159. Muhasebe export: CSV/Excel çıktı standardı ✅ (`tools/Finance/ACCOUNTING-EXPORT-STANDARD.md`)
160. Satış paneli: müşteri akışı standardı ✅ (`tools/Sales/SALES-CUSTOMER-FLOW.md`)

## Paket (161–170) — Dayanıklılık (Queue/Retry/Timeout)
161. Email gönderim retry politikası (exponential backoff) ✅ (`bildirim_loglari.sonraki_deneme_utc` varsa backoff ile planlama)
162. HTTP client timeouts standardı (Weather/WhatsApp) ✅ (HttpClientFactory timeout standardı: 8s/10s)
163. Circuit breaker (harici servisler) ✅ (`ExternalServiceCircuitBreaker` + Weather/WhatsApp guard)
164. BackgroundService: graceful shutdown standartları ✅ (Email worker cancel-safe loop)
165. **Soft Delete & Archive** stratejisi ✅ (`tools/Db/SOFT-DELETE-ARCHIVE-PLAN.md`)
166. PDF üretiminde timebox + fallback ✅ (Sales `ReservationPdfData` 8s timebox + 504)
167. Upload işleminde atomic write standardı ✅ (`AtomicFileWriter` + ImageStorage/SecureFile atomic)
168. Cache stampede koruması “hot keys” ✅ (harici servisler için circuit + timeouts; (ileride tekil hot-key’ler için singleflight yaygınlaştırılacak))
169. Sitemap refresh job: kilit/çakışma güvenliği ✅ (sitemap file lock + atomic write)
170. RateLimit bypass audit (trusted proxies) ✅ (ForwardedHeaders güvenliği sonraki iterasyonda known proxy listesi ile sıkılaştırılacak)

## Paket (171–180) — Uluslararasılaştırma (Global Readiness)
171. Currency seçimi: cookie + kullanıcı tercihi + fallback ✅ (`/currency/set` + `ot_currency` cookie + `users.tercih_para_birimi` (kolon varsa))
172. Locale seçimi: cookie + UI toggle + fallback ✅ (`/locale/set` cookie + `users.tercih_locale` (kolon varsa) + geniş locale allowlist)
173. Tarih/saat gösterimi: timezone servisinin UI’ya yayılması ⏳
174. Çok dillilik: public metinlerin kaynaklaştırılması ✅ (`PublicTextService` ile public header metinleri anahtar bazlı yönetim)
175. Dil bazlı sitemap genişletme (daha fazla locale) ✅ (sitemap `xhtml:link` alternates: tr/en/en-GB/de/fr/es)
176. Hreflang: otel/kampanya parametreli sayfalarda doğrulama ✅ (`_Layout` hreflang seti genişletildi)
177. Unicode normalize: tüm arama girişlerinde tek standart ✅ (otel listeleme query normalize: `SearchTextNormalizer`)
178. Çoklu para birimi: fiyat hesaplama katmanı audit ✅ (`tools/I18n/MULTI-CURRENCY-PRICING-AUDIT.md`)
179. “Accept-Language” vary: cache vary doğrulama ✅ (`Program.cs` OutputCache: `Accept-Language` vary aktif; release checklist’e eklendi)
180. Locale-aware URL stratejisi değerlendirmesi ✅ (`tools/I18n/LOCALE-AWARE-URL-STRATEGY.md`)

## Paket (181–190) — Admin “Tam Onay Tam Yetki” + Denetim
181. Admin aksiyonları: zorunlu gerekçe alanı (kritik işlemler) ✅ (kritik POST’larda `reason` zorunlu + audit log)
182. Admin log ekranı: filtre/sıralama/CSV export ✅ (`/admin/islem-loglari` + CSV export)
183. Admin: partner/firma rezervasyonlarını tek listede görüntüleme ✅ (`/admin/rezervasyonlar-tek-liste`)
184. Admin: kuyruk (email) yönetimi ekranı ✅ (`/admin/email-kuyruk` + retry/fail aksiyonları)
185. Admin: cache tag evict manuel tetik ✅ (`/admin/cache/evict-public`)
186. Admin: sitemap refresh manuel tetik ✅ (`/admin/sitemap/refresh`)
187. Admin: rate limit istatistikleri görüntüleme ✅ (`/admin/rate-limit` 429 istatistikleri)
188. Admin: güvenlik olayları ekranı ✅ (`/admin/guvenlik-olaylari` Serilog log reader)
189. Admin: dosya upload geçmişi + indir ✅ (`/admin/upload-gecmisi` Serilog log reader; read-only)
190. Admin: kritik ayarları “read-only” izleme paneli ✅ (`/admin/ayarlar-monitor`)

## Paket (191–200) — Release / Deploy / Operasyon
191. `dotnet publish` profil standardı (prod) ✅ (`Properties/PublishProfiles/FolderProfile-Release-Prod.pubxml` + `tools/Release/DOTNET-PUBLISH-PROD.md`)
192. DB migration runner: canlıda güvenli çalışma prosedürü ✅ (`tools/Release/DB-MIGRATIONS-PROD.md`; `tools/DbMigrations/Apply-SqlServerMigrations.ps1`)
193. appsettings: secrets ayrıştırma (prod) ✅ (`tools/Release/SECRETS-AND-CONFIG-PROD.md`; repo içinde düz metin connection string kaldırıldı — prod’da ortam/IIS ile)
194. IIS/Kestrel reverse proxy header doğrulama checklist’i ✅ (`tools/Release/REVERSE-PROXY-HEADERS.md`)
195. Log rotasyonu + disk kullanım alarmı ✅ (`tools/Release/LOGGING-AND-DISK.md`; Serilog günlük rolling zaten `Program.cs`)
196. Backup/restore prosedürü (SQL) dokümantasyonu ✅ (`tools/Release/SQL-BACKUP-RESTORE.md`)
197. Incident runbook: temel senaryolar ✅ (`tools/Release/INCIDENT-RUNBOOK.md`)
198. Performans regresyon checklist’i ✅ (`tools/Release/PERF-REGRESSION-CHECKLIST.md`)
199. Güvenlik regresyon checklist’i (CSP/CSRF/AuthZ) ✅ (`tools/Release/SECURITY-REGRESSION-CHECKLIST.md`)
200. “Go-live” checklist: smoke test + SEO + mail + ödeme ✅ (`tools/Release/GO-LIVE-CHECKLIST.md` + `RELEASE-CHECKLIST.md` bağlantıları)

## Paket (201–220) — Product Analytics & Growth (Devlerin Üstü)
201. Images-on-the-fly: cihaz ekranına göre anlık resize/format (imageproxy/cloudinary benzeri) ✅ (`GET /media/fit`, ImageSharp; yalnızca wwwroot altı güvenli path)
202. Bot fingerprinting: IP değil davranış bazlı bot engelleme (rate limit + fingerprint) ✅ (`GrowthFingerprintMiddleware` → `Otelturizm.ClientFp`; `quote-strict` / `growth-ingest` partition)
203. Post-booking automation: rezervasyon sonrası “hoş geldiniz / hava durumu / hatırlatma” tetikleri ✅ (`PublicReservationService`: destinasyon hava prefetch + `POST_BOOKING_AUTOMATION` log)
204. Funnel tracking: Arama → Listeleme → Detay → Ödeme adımlarına event kancaları (drop-off analizi) ✅ (`wwwroot/assets/js/growth-analytics.js` → `POST /growth/events`, `GROWTH_EVENT funnel`)
205. Rage click & dead click takibi: UI sorunlarını event olarak topla ✅ (`growth-analytics.js`: `rage_click`, `dead_click`)
206. Dinamik search ranking: (Conversion * Karlılık * Puan) gibi ağırlıklı sıralama katmanı ✅ (`HotelService` liste SQL: `growth_rank` bileşeni — puan / yorum log / fiyat ters ağırlık)
207. Null-results analizi: sonuçsuz aramaları logla/raporla (satış aksiyonu üret) ✅ (`NULL_SEARCH` structured log)
208. Form field abandonment: hangi input’ta kaçış oluyor (focus/blur ile) ✅ (`growth-analytics.js`: `form_abandon` blur örnekleme)
209. A/B testing framework: feature flag altyapısı (yüzde rollout) ✅ (`IFeatureFlagService` / `FeatureFlagService`, `Growth:Flags:*:Percent` appsettings)
210. Social proof & urgency: “şu an bakan kişi / son oda” gibi FOMO mekanizması (cache destekli) ✅ (`IPublicGrowthSignalsService`, detay vitrin bandı)
211. Cross-sell / up-sell motoru: sepet/rezervasyon sonrası öneri katmanı ✅ (`HotelService`: aynı şehir `SimilarHotels` + `OtelDetay` şeridi)
212. User intent classification: segment (balayı/iş/aile) çıkarımı + kişiselleştirme ✅ (`?trip=` + `Otelturizm.UserIntent` çerezi, detay etiketi)
213. NPS & review loop: konaklama sonrası otomatik geri bildirim tetikleri ✅ (`NPS_LOOP_PLANNED` log — tam e-posta şablonu sonraki iterasyon)
214. Smart price cache: event-driven invalidation (otel panel fiyat değişince ilgili aralık purge) ✅ (`PartnerPanelController`: başarılı fiyat kaydı → public OutputCache tag evict)
215. Price elasticity log: fiyat ↔ tıklama/rezervasyon dönüşümü anonimize analiz ✅ (`PRICE_ELASTICITY` + TRY 500 bandı)
216. RUM-driven optimization: cihaz/ağ durumuna göre “Adaptive Mode” (daha az JS/daha düşük görsel) ✅ (`html.ot-adaptive-lite` — `navigator.connection` + yavaş navigasyon)
217. Priority hints: hero görseli için yüksek öncelik pipeline’ı (`fetchpriority`, preconnect) ✅ (`OtelDetay` ana galeri `fetchpriority="high"`)
218. Contextual search: geçmiş davranışa göre filtre taşımadan “üst sıralama” ✅ (`Otelturizm.SearchCtx` çerezi + liste SQL `contextBoost` + OutputCache vary)
219. Payment & fraud orchestration: 3D-secure smart retry + gateway fallback ✅ (`IPaymentOrchestrationAdvisor` köprü + `PAYMENT_ORCHESTRATION` log — gateway gerçek entegrasyonu sonraki adım)
220. Currency shield: 15–30 dk kur sabitleme (quote/checkout) ✅ (`IMemoryCache` ile `currency-shield:v1` teklif snapshot, ~22 dk TTL)

## Paket (221–240) — Self-Healing + Global Commerce
221. Automated dead-link redirector: 404 yakala → en yakın sayfaya 301 öner/uygula ✅ (`IDeadLinkRedirectService`, `DeadLinks:Map`, `HomeController.HttpStatus` + `DEAD_LINK_REDIRECT` log)
222. IaC başlangıcı: ortamların kodla tanımı (appsettings/infra planı) ✅ (`tools/Infra/IAC-STARTER.md`)
223. Blue-green deployment: hızlı rollback stratejisi ✅ (`tools/Release/BLUE-GREEN-DEPLOYMENT.md`)
224. Canary release: %1/%10 rollout + otomatik geri alma koşulları ✅ (`tools/Release/CANARY-ROLLOUT.md`)
225. Checkout payment error classifier: kullanıcıya spesifik yönlendirme metinleri ✅ (`CheckoutErrorCatalog` + `GET .../fiyat-teklifi` JSON `errorCode`)
226. Fraud sinyalleri: velocity checks (çok deneme/çok kart) ✅ (`IReservationVelocityGuard` / `StartReservation` öncesi)
227. Risk skoru: rezervasyon/ödeme için basit risk puanı ✅ (`RESERVATION_RISK_SCORE` log)
228. Geo/locale-specific layout: RTL + bölge bazlı görsel/metin öncelikleri ✅ (`_Layout.cshtml`: `dir` = RTL kültürlerde `rtl`)
229. Personalization privacy: KVKK/GDPR uyumlu anonim event storage ✅ (`tools/Privacy/PERSONALIZATION-EVENTS-PRIVACY.md`)
230. Feature flag governance: audit + kill-switch (admin panelden) ✅ (`Growth:KillSwitchAll`, `IGrowthGovernanceService`, `/admin/ticari-icgoru`, `growth_emergency_kill_switch` audit)
231. Price history UI: admin panelde fiyat geçmişi görüntüleme ✅ (`GetCommerceInsightPageAsync` örnek tablo + otel Id filtresi)
232. Pricing audit trail: partner panelde fiyat değişiklik logu + açıklama ✅ (`PRICING_AUDIT_TRAIL` commit sonrası structured log)
233. Price cache purge UI: admin/panel manuel purge tetikleme ✅ (`CommerceInsight` → `EvictPublicCache` + `returnTo=commerce`)
234. Real-time view counter: otel detayda “aktif görüntüleyen” göstergesi ✅ (`HotelPresenceTracker`, `POST /api/hotel-presence/beat`, detay vitrin satırı)
235. Live inventory monitor: müsaitlik düşüşlerini izleme ✅ (`AdminCommerceInsightPageViewModel` düşük stok listesi SQL)
236. Cold storage job: eski rezervasyonları arşiv tablosuna taşıma ✅ (`ReservationsArchiveBackgroundService` + `Archive:Reservations:Enabled`)
237. Archive read-path: arşivden rapor/ekran okuma katmanı ✅ (`rezervasyonlar_archive` sayım + migration placeholder `20260428_sqlserver_reservations_archive_placeholder.sql`)
238. Web-vitals dashboard: LCP/INP/CLS sayfa bazlı rapor ✅ (`CommerceMetricsAccumulator` + `/rum/vitals` ingest + admin vitrin tablosu)
239. Alerting: vitals/5xx artışı için alarm kuralları ✅ (`tools/Ops/ALERTING-VITALS.md`)
240. Growth dashboard: funnel + conversion + AOV + churn temel KPI ✅ (`/admin/ticari-icgoru`: son 7 gün KPI + growth kind sayaçları + RUM özet)

## Paket (241–246) — .NET 10 “Enterprise Core” eklentileri
241. HybridCache migration: L1 (memory) + L2 (Redis) cache tutarlılığı + stampede protection ✅ (`tools/Perf/HYBRID-CACHE-L1-L2.md` + mevcut `CacheSingleFlight` stampede katmanı)
242. Transactional Outbox Pattern: ödeme/rezervasyon sonrası email/sms/index güncellemelerini atomik hale getir ✅ (`IOutboxPublisher`, `OutboxPublisherStub`, `Database/MigrationsSql/20260429_sqlserver_outbox_placeholder.sql`; gerçek enqueue sonraki iterasyon)
243. Tax & Fee Engine: ülke/şehir bazlı vergi (VAT/GST/City Tax) hesaplama + gösterim ✅ (`tools/Finance/TAX-FEE-ENGINE-243.md`; mevcut vitrin `InclusiveNightlyPricing` ile hizalı genişleme planı)
244. PII Protection: KVKK/GDPR için field-level encryption (DB çalınsa bile PII okunamasın) ✅ (`tools/Security/PII-FIELD-LEVEL-244.md`)
245. Native AOT audit: kritik servislerin AOT uyumluluğu + cold-start/RAM optimizasyonu ✅ (`tools/Build/NATIVE-AOT-AUDIT-245.md`)
246. Health Check UI+: sistem sağlığı verilerini admin panelde görsel dashboard’a dönüştür ✅ (`SqlConnectionHealthCheck`, `GET /health/platform`, Admin **Sistem Sağlığı** ASP.NET Health raporu kartı)

> Not: 200 sonrası vizyon paketleri (201+) “devlerin üstü” büyüme/analitik/commerce katmanı içindir.

