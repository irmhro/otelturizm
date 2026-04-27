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
61. Sorgu süreleri: en yavaş 20 SQL query’yi logla/raporla ⏳
62. SQL parametre sniffing riskli sorgular için `OPTION (RECOMPILE)` değerlendirmesi ⏳
63. `IMemoryCache` anahtar standardı + stampede önleme (tek uçtan) ⏳
64. OutputCache: tag eviction (otel/kampanya update) altyapısını uygula ⏳
65. OtelDetay için cache vary: `lang` ve kritik query param vary standardı ⏳
66. Static asset preload: Inter font + ana CSS için `<link rel="preload">` ⏳ (cihaza göre `PageCssMobile`/desktop ayrımını preload ile hızlandır, “critical CSS” üst katmanda kalsın)
67. Brotli/Gzip MIME kapsamı: svg/js/css için doğrulama ⏳
68. Görsel optimizasyon: hero görsellerde `loading="lazy"` audit ⏳
69. SQL index kullanım doğrulaması: plan cache üzerinden kontrol checklist’i ⏳
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
81. Email template: tr/en klasörleme standardı ⏳
82. Email subject localization (tr/en) ⏳
83. Email footer: şirket adresi/kvkk/iptal link standardı ⏳
84. Transactional email: “Rezervasyon alındı” şablonu tr/en ⏳
85. Transactional email: “Rezervasyon onaylandı” şablonu tr/en ⏳
86. Transactional email: “Rezervasyon iptal” şablonu tr/en ⏳
87. Password reset / verify email metinlerini tr/en hizala ⏳
88. Email linkleri: UTM param standardı ⏳
89. Email render testi: temel HTML doğrulama checklist’i ⏳
90. Email deliverability: SPF/DKIM/DMARC notları dokümana yaz ⏳

## Paket (91–100) — Panel Minimal UI Standardı (ot-* token yayılımı)
91. Panel buton standardı: `ot-btn` sınıfları (primary/secondary/ghost) ⏳
92. Panel kart standardı: `ot-card` ve spacing standardı ⏳
93. Panel rozet standardı: `ot-badge` (success/warn/danger/info) ⏳
94. Panel tablo standardı: tek “table look” + mobile overflow ⏳
95. Panel form standardı: input/select/textarea tek tip ⏳
96. Panel sayfa başlığı standardı: kicker + H1 + subtitle ⏳
97. Panel boş durumlar: empty state component standardı ⏳
98. Panel toast/flash mesaj standardı ⏳
99. Panel mobil nav: ikon/etiket standardı ⏳
100. Panel renk/kontrast audit (AA hedefi) ⏳

## Paket (101–110) — Rezervasyon Akışı Sağlamlaştırma
101. Rezervasyon form validation: server+client aynı kurallar ⏳
102. Rezervasyon idempotency key (double submit engeli) ⏳
103. Ödeme yöntemi ekranı: state management tek kaynak ⏳
104. Quote endpoint: input guard + log enrichment ⏳
105. Draft/resume akışı: edge case testleri ⏳
106. CSRF token kullanım audit (public + panel) ⏳
107. Rate limit: rezervasyon create için ayrı policy ⏳
108. Fail-safe: ödeme adımında 5xx durumunda kullanıcı yönlendirme ⏳
109. PDF üretimi: render timeout + retry ⏳
110. Rezervasyon audit: create/update/cancel tam zincir ⏳

## Paket (111–120) — SEO + İçerik (Global)
111. Canonical link standardı (public sayfalar) ⏳
112. Meta title/description standardı + otomatik fallback ⏳
113. OpenGraph/Twitter card standardı ⏳
114. `hreflang` site-wide: header link tag değerlendirmesi ⏳
115. Robots: crawl budget iyileştirme (query param disallow) ⏳
116. Sitemap: image title/alt standardı ⏳
117. 404 sayfası: arama kutusu + popüler linkler ⏳
118. Structured data: Hotel / Breadcrumb JSON-LD ⏳
119. Kampanya sayfalarında JSON-LD (Offer) ⏳
120. PageSpeed: LCP ana hedefler listesi ⏳

## Paket (121–130) — Public UX (Booking.com seviyesi)
121. Listing: sticky filter bar (mobile) ⏳
122. Listing: skeleton loading (first paint) ⏳
123. Detay: fiyat/uygunluk “trust” blokları standardı ⏳
124. Detay: oda kartı hiyerarşisi (fiyat/iptal/özellik) ⏳
125. Detay: galeri performans (prefetch/thumbnail) ⏳
126. Checkout: adım göstergesi + geri dönüş güveni ⏳
127. Checkout: form hatalarını alan bazlı gösterim ⏳
128. Favori: optimistic UI + fallback ⏳
129. Bildiri: okunmamış rozet tutarlılığı ⏳
130. Mobil: safe-area + sticky CTA standardı ⏳

## Paket (131–140) — Upload Pipeline Standardizasyonu (46’nın tamamı)
131. Görsel upload noktalarını envanterle ⏳
132. ImageStorage: WebP dönüştürme + kalite profilleri ⏳
133. Thumbnail üretimi (size set) ⏳
134. Upload boyut limiti: endpoint bazlı farklılaştırma ⏳
135. MIME + magic-byte doğrulama tüm upload’lara yay ⏳
136. Dosya adı normalizasyonu + path traversal kilidi audit ⏳
137. CDN-ready path standardı ⏳
138. Secure download: content-disposition standardı ⏳
139. Upload audit log: kim/hangi kategori/boyut ⏳
140. Temizleme: orphan dosyalar için job ⏳

## Paket (141–150) — “200 Health” Otomasyon (41–44 genişletme)
141. Route map çıkar: public + panel tüm GET sayfalar ⏳
142. Smoke test script: temel 200/302 kontrolü ⏳
143. 404 link checker: header/footer/nav ⏳
144. View compile errors: publish aşamasında doğrulama ⏳
145. Broken static asset referans taraması ⏳
146. CSP report toplama: en sık ihlaller listesi ⏳
147. HTML doğrulama: temel semantik/hata kontrol ⏳
148. Mobil viewport audit (kritik sayfalar) ⏳
149. Accessibility: fokus halkası + klavye nav ⏳
150. “Release checklist” dokümanı ⏳

## Paket (151–160) — Çoklu Partner / Çoklu Firma Yönetimi (Platform Omurga)
151. Kullanıcı → birden çok partner ilişki modeli audit ⏳
152. Firma çalışan rolleri (limit/onay akışı) audit ⏳
153. Yetki matrisi dokümanı (admin/partner/firma/satış/user) ⏳
154. RBAC guard: controller/action bazlı tutarlılık ⏳
155. Audit log: role değişimi ve yetki güncellemeleri ⏳
156. Destek ticket: SLA alanları + durum makinesi ⏳
157. Komisyon/finans: hesap kesim periyodu modelleme ⏳
158. Fatura yükleme: doğrulama + saklama standardı ⏳
159. Muhasebe export: CSV/Excel çıktı standardı ⏳
160. Satış paneli: müşteri akışı standardı ⏳

## Paket (161–170) — Dayanıklılık (Queue/Retry/Timeout)
161. Email gönderim retry politikası (exponential backoff) ⏳
162. HTTP client timeouts standardı (Weather/WhatsApp) ⏳
163. Circuit breaker (harici servisler) ⏳
164. BackgroundService: graceful shutdown standartları ⏳
165. **Soft Delete & Archive**: DB’den silme yerine pasife çekme + eski rezervasyonları “cold storage/arşiv” tablolarına taşıma stratejisi ⏳
166. PDF üretiminde timebox + fallback ⏳
167. Upload işleminde atomic write standardı ⏳
168. Cache stampede koruması “hot keys” ⏳
169. Sitemap refresh job: kilit/çakışma güvenliği ⏳
170. RateLimit bypass audit (trusted proxies) ⏳

## Paket (171–180) — Uluslararasılaştırma (Global Readiness)
171. Currency seçimi: cookie + kullanıcı tercihi + fallback ⏳
172. Locale seçimi: cookie + UI toggle + fallback ⏳
173. Tarih/saat gösterimi: timezone servisinin UI’ya yayılması ⏳
174. Çok dillilik: public metinlerin kaynaklaştırılması ⏳
175. Dil bazlı sitemap genişletme (daha fazla locale) ⏳
176. Hreflang: otel/kampanya parametreli sayfalarda doğrulama ⏳
177. Unicode normalize: tüm arama girişlerinde tek standart ⏳
178. Çoklu para birimi: fiyat hesaplama katmanı audit ⏳
179. “Accept-Language” vary: cache vary doğrulama ⏳
180. Locale-aware URL stratejisi değerlendirmesi ⏳

## Paket (181–190) — Admin “Tam Onay Tam Yetki” + Denetim
181. Admin aksiyonları: zorunlu gerekçe alanı (kritik işlemler) ⏳
182. Admin log ekranı: filtre/sıralama/CSV export ⏳
183. Admin: partner/firma rezervasyonlarını tek listede görüntüleme ⏳
184. Admin: kuyruk (email) yönetimi ekranı ⏳
185. Admin: cache tag evict manuel tetik ⏳
186. Admin: sitemap refresh manuel tetik ⏳
187. Admin: rate limit istatistikleri görüntüleme ⏳
188. Admin: güvenlik olayları ekranı ⏳
189. Admin: dosya upload geçmişi + indir ⏳
190. Admin: kritik ayarları “read-only” izleme paneli ⏳

## Paket (191–200) — Release / Deploy / Operasyon
191. `dotnet publish` profil standardı (prod) ⏳
192. DB migration runner: canlıda güvenli çalışma prosedürü ⏳
193. appsettings: secrets ayrıştırma (prod) ⏳
194. IIS/Kestrel reverse proxy header doğrulama checklist’i ⏳
195. Log rotasyonu + disk kullanım alarmı ⏳
196. Backup/restore prosedürü (SQL) dokümantasyonu ⏳
197. Incident runbook: temel senaryolar ⏳
198. Performans regresyon checklist’i ⏳
199. Güvenlik regresyon checklist’i (CSP/CSRF/AuthZ) ⏳
200. “Go-live” checklist: smoke test + SEO + mail + ödeme ⏳

## Paket (201–220) — Product Analytics & Growth (Devlerin Üstü)
201. Images-on-the-fly: cihaz ekranına göre anlık resize/format (imageproxy/cloudinary benzeri) ⏳
202. Bot fingerprinting: IP değil davranış bazlı bot engelleme (rate limit + fingerprint) ⏳
203. Post-booking automation: rezervasyon sonrası “hoş geldiniz / hava durumu / hatırlatma” tetikleri ⏳
204. Funnel tracking: Arama → Listeleme → Detay → Ödeme adımlarına event kancaları (drop-off analizi) ⏳
205. Rage click & dead click takibi: UI sorunlarını event olarak topla ⏳
206. Dinamik search ranking: (Conversion * Karlılık * Puan) gibi ağırlıklı sıralama katmanı ⏳
207. Null-results analizi: sonuçsuz aramaları logla/raporla (satış aksiyonu üret) ⏳
208. Form field abandonment: hangi input’ta kaçış oluyor (focus/blur ile) ⏳
209. A/B testing framework: feature flag altyapısı (yüzde rollout) ⏳
210. Social proof & urgency: “şu an bakan kişi / son oda” gibi FOMO mekanizması (cache destekli) ⏳
211. Cross-sell / up-sell motoru: sepet/rezervasyon sonrası öneri katmanı ⏳
212. User intent classification: segment (balayı/iş/aile) çıkarımı + kişiselleştirme ⏳
213. NPS & review loop: konaklama sonrası otomatik geri bildirim tetikleri ⏳
214. Smart price cache: event-driven invalidation (otel panel fiyat değişince ilgili aralık purge) ⏳
215. Price elasticity log: fiyat ↔ tıklama/rezervasyon dönüşümü anonimize analiz ⏳
216. RUM-driven optimization: cihaz/ağ durumuna göre “Adaptive Mode” (daha az JS/daha düşük görsel) ⏳
217. Priority hints: hero görseli için yüksek öncelik pipeline’ı (`fetchpriority`, preconnect) ⏳
218. Contextual search: geçmiş davranışa göre filtre taşımadan “üst sıralama” ⏳
219. Payment & fraud orchestration: 3D-secure smart retry + gateway fallback ⏳
220. Currency shield: 15–30 dk kur sabitleme (quote/checkout) ⏳

## Paket (221–240) — Self-Healing + Global Commerce
221. Automated dead-link redirector: 404 yakala → en yakın sayfaya 301 öner/uygula ⏳
222. IaC başlangıcı: ortamların kodla tanımı (appsettings/infra planı) ⏳
223. Blue-green deployment: hızlı rollback stratejisi ⏳
224. Canary release: %1/%10 rollout + otomatik geri alma koşulları ⏳
225. Checkout payment error classifier: kullanıcıya spesifik yönlendirme metinleri ⏳
226. Fraud sinyalleri: velocity checks (çok deneme/çok kart) ⏳
227. Risk skoru: rezervasyon/ödeme için basit risk puanı ⏳
228. Geo/locale-specific layout: RTL + bölge bazlı görsel/metin öncelikleri ⏳
229. Personalization privacy: KVKK/GDPR uyumlu anonim event storage ⏳
230. Feature flag governance: audit + kill-switch (admin panelden) ⏳
231. Price history UI: admin panelde fiyat geçmişi görüntüleme ⏳
232. Pricing audit trail: partner panelde fiyat değişiklik logu + açıklama ⏳
233. Price cache purge UI: admin/panel manuel purge tetikleme ⏳
234. Real-time view counter: otel detayda “aktif görüntüleyen” göstergesi ⏳
235. Live inventory monitor: müsaitlik düşüşlerini izleme ⏳
236. Cold storage job: eski rezervasyonları arşiv tablosuna taşıma ⏳
237. Archive read-path: arşivden rapor/ekran okuma katmanı ⏳
238. Web-vitals dashboard: LCP/INP/CLS sayfa bazlı rapor ⏳
239. Alerting: vitals/5xx artışı için alarm kuralları ⏳
240. Growth dashboard: funnel + conversion + AOV + churn temel KPI ⏳

## Paket (241–246) — .NET 10 “Enterprise Core” eklentileri
241. HybridCache migration: L1 (memory) + L2 (Redis) cache tutarlılığı + stampede protection ⏳
242. Transactional Outbox Pattern: ödeme/rezervasyon sonrası email/sms/index güncellemelerini atomik hale getir ⏳
243. Tax & Fee Engine: ülke/şehir bazlı vergi (VAT/GST/City Tax) hesaplama + gösterim ⏳
244. PII Protection: KVKK/GDPR için field-level encryption (DB çalınsa bile PII okunamasın) ⏳
245. Native AOT audit: kritik servislerin AOT uyumluluğu + cold-start/RAM optimizasyonu ⏳
246. Health Check UI+: sistem sağlığı verilerini admin panelde görsel dashboard’a dönüştür ⏳

> Not: 200 sonrası vizyon paketleri (201+) “devlerin üstü” büyüme/analitik/commerce katmanı içindir.

