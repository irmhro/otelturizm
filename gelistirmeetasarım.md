# Otelturizm — Panel geliştirme & tasarım analizi (ultra hedef)

> **Belge amacı:** Mevcut panel kodlarını (layout, sidebar, header/footer, formlar, tablolar, bildirimler) envanterlemek; sayfalar arası bağlantıları ve “backend sözleşmesini” netleştirmek; **panel–sayfa bazında** eksiklikleri ve ultra seviye (işlev + görünüm) yapılacaklar listesini çıkarmak.  
> **Kaynak:** `Views/Paneller/**`, `Controllers/Paneller/**`, `wwwroot/assets/css/paneller/**`, `Views/Paneller/Common/_PanelToasts.cshtml`, `panel-standards.css`, `panel-tokens.css`.

---

## İçindekiler

1. [Yöntem ve varsayımlar](#1-yöntem-ve-varsayımlar)  
2. [Ortak kabuk mimarisi](#2-ortak-kabuk-mimarisi)  
3. [Bildirimler (toast) ve TempData uyumu](#3-bildirimler-toast-ve-tempdata-uyumu)  
4. [API / istek yapısı](#4-api--istek-yapısı)  
5. [Admin paneli (`/admin`)](#5-admin-paneli-admin)  
6. [Partner paneli (`/panel/partner`)](#6-partner-paneli-panelpartner)  
7. [Firma paneli (`/panel/firma`)](#7-firma-paneli-panelfirma)  
8. [Satış paneli (`/panel/satis`)](#8-satış-paneli-panelsatis)  
9. [Kullanıcı paneli (`/panel/user`)](#9-kullanıcı-paneli-paneluser)  
10. [Developer paneli (`/panel/developer`)](#10-developer-paneli-paneldeveloper)  
11. [Çapraz panel bağlantıları](#11-çapraz-panel-bağlantıları)  
12. [Global eksiklikler (kod taraması)](#12-global-eksiklikler-kod-taraması)  
13. [Ultra seviye master backlog](#13-ultra-seviye-master-backlog)

---

## 1. Yöntem ve varsayımlar

- **Sunum katmanı:** Klasik ASP.NET Core MVC (Razor) + çoğu işlem **form POST** + `ValidateAntiForgeryToken`.  
- **JSON:** Bazı yardımcı uçlar (ör. satış otel asistanı) `Json` döner; panel ana akışı MVC.  
- **Stil:** AdminLTE 3 tabanlı shell + `panel-tokens.css` (tasarım token’ları) + `panel-standards.css` (`.ot-card`, `.ot-table`, `.ot-form-grid`, `.ot-btn`, toast).  
- **Sayfa CSS:** `ViewData["PageCssPath"]` → `~/assets/css/{path}.css` + çoğu panelde `.mobile.css` eşi.

---

## 2. Ortak kabuk mimarisi

| Bileşen | Konum | Görev |
|--------|--------|--------|
| **Layout** | Her panel `_*PanelLayout.cshtml` | AdminLTE `wrapper`, `content-wrapper`, CSP nonce, toast partial |
| **Üst nav** | Admin: `_AdminTopNav.cshtml` | Profil / çıkış / başlık |
| **Sidebar** | `_AdminSidebar`, `_PartnerSidebar`, `_FirmaSidebar`, `_SalesSidebar`, `_UserSidebar`, `_DeveloperSidebar` | Accordion gruplar, `asp-action` ile sayfa bağlantıları |
| **Footer** | Partner/Firma/Satis `_…PanelFooter.cshtml` | İkincil linkler |
| **Mobil alt nav** | `_AdminMobileNav`, `_PartnerMobileNav`, vb. | Küçük ekran gezinti |
| **Toast** | `Common/_PanelToasts.cshtml` | Başarı / uyarı / hata (TempData anahtarlarına bağlı) |
| **Ortak JS** | `panel-toasts.js` | Toast zaman aşımı |

**Tasarım standardı (mevcut):** `panel-standards.css` içinde kart başlığı, tablo wrap, form grid, odak halkası (`--ot-focus`).

**Ultra hedef:** Tüm panellerde aynı **bilgi kartı** yoğunluğu (kicker + başlık + alt metin), tablo üstü **filtre çubuğu**, form alanlarında **yardım metni + hata satırı**, boş durum **illustration + CTA**.

---

## 3. Bildirimler (toast) ve TempData uyumu

`_PanelToasts.cshtml` şu anahtarları okur:

| Tür | Anahtarlar |
|-----|------------|
| Başarı | `AdminSuccess`, `PartnerSuccess`, `FirmaSuccess`, `SalesSuccess`, `UserSuccess` |
| Hata | `AdminError`, `PartnerError`, … |
| Uyarı | `AdminWarning`, … |

**Tespit edilen uyumsuzluklar (düzeltme önerisi):**

| Sorun | Detay |
|-------|--------|
| **Admin başarı mesajı** | Birçok aksiyon `TempData["AdminMessage"]` kullanıyor; partial **`AdminSuccess` bekliyor** → başarı toast’u görünmeyebilir. |
| **Developer paneli** | Controller `DeveloperSuccess` / `DeveloperError` yazar; partial’da **Developer** anahtarı yok → toast çalışmaz. |
| **Tutarlılık** | Ya tüm controller’lar `*Success` kullanmalı ya da partial’a `AdminMessage` / `DeveloperSuccess` eklenmeli. |

---

## 4. API / istek yapısı

| Kalıp | Örnek | Not |
|--------|--------|-----|
| GET liste / form | `/admin/oteller`, `/panel/partner/takvim-fiyatlar` | Sayfa render |
| POST işlem | `ValidateAntiForgeryToken`, `[FromForm]` | Durum güncelleme, yükleme |
| GET JSON (yardımcı) | Satış: `SearchHotelsAssistant`, `ReservationPdfData` | Asistan / PDF veri |
| Health (genel) | `/health/live`, `/health/ready`, `/health/platform` | Operasyon; panel dışı |

**Ultra hedef:** Kritik formlarda **optimistic UI** yerine net yükleme durumu; hata mesajlarını alan bazlı gösterme (model state); uzun işlemlerde **progress** (CSV export, rapor).

---

## 5. Admin paneli (`/admin`)

**Controller:** `AdminPanelController`  
**Layout:** `_AdminPanelLayout.cshtml`  
**Shell CSS:** `panel-admin-shell.css` (+ sayfa bazlı `ViewData["PageCssPath"]`).

### 5.1 Özel sayfalar (özel view + servis)

| Sayfa (Action) | Türkçe rota / not | View | Önerilen ultra geliştirme |
|----------------|-------------------|------|---------------------------|
| `Dashboard` | `/admin`, `/admin/dashboard` | `Dashboard.cshtml` | KPI kartlarına trend sparkline; drill-down linkler |
| `SystemHealth` | `/admin/sistem-sagligi` | `SystemHealth.cshtml` | Health API özeti mevcut; zaman serisi grafiği (log tabanlı) |
| `CommerceInsight` | `/admin/ticari-icgoru` | `CommerceInsight.cshtml` | Growth/RUM için Chart.js / küçük çubuk grafik |
| `SettingsMonitor` | `/admin/ayarlar-monitor` | `SettingsMonitor.cshtml` | Değişiklik diff / son kontrol zamanı |
| `UnifiedReservations` | `/admin/rezervasyonlar-tek-liste` | `UnifiedReservations.cshtml` | Gelişmiş filtre, sütun seçimi, dışa aktar |
| `EmailQueue` | `/admin/email-kuyruk` | `EmailQueue.cshtml` | Kuyruk derinliği grafiği, SLA renk kodu |
| `AdminActionLogs` | `/admin/islem-loglari` | `AdminActionLogs.cshtml` | Zaman çizelgesi, aktör grafiği |
| `RateLimitStats` | `/admin/rate-limit` | `RateLimitStats.cshtml` | Endpoint heatmap |
| `LogEvents` | `/admin/guvenlik-olaylari`, `/admin/upload-gecmisi` | `LogEvents.cshtml` | JSON satır expand, filtre |
| `Hotels` | `/admin/oteller` | `Hotels.cshtml` | Toplu seçim, durum rozetleri |
| `HotelDetail` | `/admin/otel-detay/{id}` | `HotelDetail.cshtml` | Sekmeli düzen, fotoğraf grid önizleme |
| `Commissions` | `/admin/komisyonlar` | `Commissions.cshtml` | Oran görselleştirme, çakışma uyarısı |
| `Contracts` | `/admin/sozlesmeler` | `Contracts.cshtml` | PDF önizleme modal, imza durumu |
| `SupportArticles` | `/admin/destek-makaleleri` | `SupportArticles.cshtml` | Markdown önizleme, kategori ağacı |
| `Sitemap` | `/admin/sitemap` | `Sitemap.cshtml` | Son üretim zamanı, boyut kartı |
| `WhatsAppCloudApi` | `/admin/whatsapp-cloud-api` | `WhatsAppCloudApi.cshtml` | Test gönderimi sonucu kartı |
| `DevelopmentRequests` | Geliştirme talepleri | `DevelopmentRequests.cshtml` | Kanban veya öncelik matrisi |
| `ListingSubscriptions` | Liste abonelikleri | `ListingSubscriptions.cshtml` | Yenileme tarihi takvimi |
| `PartnerApplications` | Partner başvuru | `PartnerApplications.cshtml` | Karar geçmişi timeline |
| `CompanyApplications` | Firma başvuru | `CompanyApplications.cshtml` | Aynı |
| `CompanyReservations` | `/admin/firma-rezervasyonlari` | `CompanyReservations.cshtml` | Firma bazlı pivot |
| `GeoSearchLogs` | Konum arama log | `GeoSearchLogs.cshtml` | Harita üstü yoğunluk (anonim) |
| `HotelCoordinateChanges` | Koordinat değişimi | `HotelCoordinateChanges.cshtml` | Harita diff |

### 5.2 Bölüm şablonu (`RenderSectionAsync`)

`GetSectionPageAsync` + `~/Views/Paneller/Admin/{ViewName}.cshtml` — `panel-admin-section` veya `panel-admin-users`.

| Action → View | Türkçe rota örneği | Not |
|---------------|-------------------|-----|
| Users | `/admin/kullanicilar` | `panel-admin-users` |
| Managers, PlatformOfficials | yöneticiler / platform | Genel section |
| Reservations, Payments, Invoices | rezervasyon / ödeme / fatura | Tablo sayfaları |
| ActiveHotels, PendingHotels | açık / bekleyen oteller | Liste |
| Reviews, Reports, Campaigns | değerlendirme / rapor / kampanya | |
| Notifications, Settings | bildirim / ayar | |
| Security, Blog, EmailTemplates, Faq | güvenlik / blog / e-posta / SSS | |
| Complaints, Logs | şikayet / log | |
| **Backups** | `/admin/yedekleme` | `Backups.cshtml` eklendi (`_AdminSectionPage` ile uyumlu); içerik/operasyon SQL’i sonraki iterasyon |

### 5.3 POST / kritik aksiyonlar (özet)

Önbellek silme, sitemap, e-posta retry, growth kill-switch, otel/oda foto, komisyon, sözleşme — hepsi **gerekçe + audit** desenine uygun tasarlanmalı (UI’da reason alanı net).

---

## 6. Partner paneli (`/panel/partner`)

**Layout:** `_PartnerPanelLayout.cshtml`  
**Sidebar:** `_PartnerSidebar.cshtml`  
**CSS kökü:** `paneller/partner/shell.css` + sayfa dosyaları (`dashboard.css`, `pricing.css`, …).

| URL (GET) | Action | View | Ultra geliştirme odağı |
|-----------|--------|------|-------------------------|
| ``, `dashboard` | Index | `Dashboard.cshtml` | Özet kartlar, hızlı kısayollar, uyarı şeridi |
| `guvenlik` | Security | `Security.cshtml` | 2FA adım göstergesi |
| `rezervasyonlar` | Reservations | `Reservations.cshtml` | Takvim görünümü, durum filtre chips |
| `rezervasyonlar/disa-aktar` | ExportReservations | (dosya) | İlerleme bildirimi |
| `takvim-fiyatlar` | Pricing | `Pricing.cshtml` | **Takvim heatmap**, toplu işlem özeti |
| `firma-fiyatlari` | CompanyPricing | `CompanyPricing.cshtml` | Firma karşılaştırma grafiği |
| `aboneliklerim` | ListingSubscriptions | `ListingSubscriptions.cshtml` | Yenileme timeline |
| `kampanyalar` | Campaigns | `Campaigns.cshtml` | Kampanya kartları, katılım oranı |
| `oda-yonetimi`, `rooms` | Rooms | `Rooms.cshtml` | Kapasite uyarıları |
| `otel-bilgileri` | HotelInfo | `HotelInfo.cshtml` | SEO skoru kartı |
| `fotograflar`, `fotograflar/yukle` | Photos / UploadPhotosPage | `Photos.cshtml` | Sürükle-bırak, sıralama |
| `performans` | Performance | `Performance.cshtml` | **Rakip / fiyat grafikleri** |
| `degerlendirmeler` | Reviews | `Reviews.cshtml` | Yanıt SLA göstergesi |
| `finans` | Finance | `Finance.cshtml` | Gelir çizgisi, ödeme durumu |
| `tercihler`, `basvuru-ve-evraklar` | Preferences | `Preferences.cshtml` | Evrak durumu progress |
| `724-destek` | Support | `Support.cshtml` | Bilet durumu pipeline |

**Özel:** `NoHotelAssigned.cshtml` — otel atanmamış kullanıcı boş durumu.

---

## 7. Firma paneli (`/panel/firma`)

**Layout:** `_FirmaPanelLayout.cshtml`  
**Sidebar:** `_FirmaSidebar.cshtml`

| URL | View | Ultra odak |
|-----|------|------------|
| ``, `dashboard` | `Dashboard.cshtml` | Harcama / limit özeti göstergeleri |
| `guvenlik` | `Security.cshtml` | Ortak güvenlik şablonu |
| `firma-fiyatlari` | `Deals.cshtml` | Filtre şeridi, karşılaştırma CTA |
| `firma-fiyatlari/karsilastir` | `DealsCompare.cshtml` | Yan yana tablo + mini grafik |
| `rezervasyonlar` | `Reservations.cshtml` | Onay akışı görünürlüğü |
| `yeni-rezervasyon` | `CreateReservation.cshtml` | Adım sihirbazı, özet kartı |
| `mesajlar` | `Messages.cshtml` | Okunmamış rozet |
| `calisanlar` | `Employees.cshtml` | Rol matrisi |
| `limitler-onaylar` | `Limits.cshtml` | Limit progress bar |
| `faturalar` | `Invoices.cshtml` | Ödeme durumu renk kodu |
| `harcama-raporlari` | `Spending.cshtml` | **Zaman serisi grafik** |
| `otel-bazli-rapor` | `Hotels.cshtml` | Otel bazlı drill-down |

---

## 8. Satış paneli (`/panel/satis`)

**Layout:** `_SalesPanelLayout.cshtml`  
**Sidebar:** `_SalesSidebar.cshtml`

| URL | View | Ultra odak |
|-----|------|------------|
| ``, `dashboard` | `Dashboard.cshtml` | Satış hunisi özeti |
| `guvenlik` | `Security.cshtml` | — |
| `yeni-rezervasyon` | `CreateReservation.cshtml` | Otel asistanı + müşteri seçimi birleşik UX |
| `yeni-rezervasyon/otel-asistani` | (JSON asistan) | Sonuç kartları, harita pin |
| `rezervasyon-pdf/{id}` | `ReservationPdf.cshtml` | Yazdır önizleme |
| `musaitlik-takvimi` | `Availability.cshtml` | Müsaitlik heatmap |
| `rezervasyonlarim` | `Reservations.cshtml` | Hızlı filtre |
| `musteri-yonetimi` | `Customers.cshtml` | CRM mini profil |
| `raporlar` | `Reports.cshtml` | Grafik + dışa aktar |
| `otel-rehberi` | `Hotels.cshtml` | Arama facet |

---

## 9. Kullanıcı paneli (`/panel/user`)

**Layout:** `_UserPanelLayout.cshtml`  
**Sidebar:** `_UserSidebar.cshtml`  
**Ek:** `_UserRouteHub.cshtml` (hub / yönlendirme yardımı).

| URL | View | Ultra odak |
|-----|------|------------|
| ``, `index` | `Index.cshtml` | Kişiselleştirilmiş özet |
| `rezervasyonlarim` | `Reservations.cshtml` | Durum timeline |
| `rezervasyonlarim/yorum/{id}` | `ReservationReview.cshtml` | Yıldız + etiket seçimi |
| `favorilerim` | `Favorites.cshtml` | Fiyat alarmları birlikte |
| `otelpuan-programi`, `puanlarim` | `Loyalty.cshtml` | Tier progress, ödül kartları |
| `mesajlarim` | `Messages.cshtml` | Okundu sync |
| `profil-bilgilerim` | `Profile.cshtml` | Tamamlanma yüzdesi |
| `odeme-yontemleri` | `PaymentMethods.cshtml` | Kart maskeli liste |
| `bildirim-tercihleri` | `Notifications.cshtml` | Kanal bazlı toggle |
| `guvenlik-ve-giris` | `Security.cshtml` | Oturum listesi (ileride) |

---

## 10. Developer paneli (`/panel/developer`)

**Layout:** `_DeveloperPanelLayout.cshtml`

| URL | View | Ultra odak |
|-----|------|------------|
| ``, `index` | `Index.cshtml` | Talep kartları, durum sütunu, SLA |
| `guvenlik` | `Security.cshtml` | Admin ile hizalı güvenlik |

**Not:** `DeveloperSuccess` toast partial’da yok → bildirim düzeltmesi gerekli.

---

## 11. Çapraz panel bağlantıları

| Akış | Bağlantı |
|------|----------|
| Admin ↔ Operasyon | Sistem sağlığı, e-posta kuyruğu, ticari içgörü |
| Partner ↔ Public | Fiyat güncelleme → OutputCache evict |
| Firma ↔ Rezervasyon | Limit / onay ↔ admin firma rezervasyonları |
| Satış ↔ Müşteri | PDF ve müşteri kaydı |
| Kullanıcı ↔ Mesaj | Mesaj merkezi ortak servis |

**Ultra hedef:** Her panel dashboard’unda **“son kritik olaylar”** tek satır (audit’ten veya bildirimden).

---

## 12. Global eksiklikler (kod taraması)

| ID | Eksiklik | Önem |
|----|-----------|------|
| G1 | `AdminMessage` vs `AdminSuccess` toast uyumsuzluğu | Yüksek |
| G2 | `DeveloperSuccess` / `DeveloperError` toast’ta yok | Orta |
| G3 | ~~`Backups.cshtml` eksikti~~ → **giderildi** (minimal section şablonu) | — |
| G4 | Grafik / chart kütüphanesi standardize değil (çoğu sayfa tablo ağırlıklı) | Orta |
| G5 | Bazı panellerde `panel-standards` bileşenleri tam homojen değil | Orta |

---

## 13. Ultra seviye master backlog

### A. Görünüm & bileşen (tüm paneller)

- [ ] **Bilgi kartı** şablonu: `ot-card` + kicker + aksiyon alanı (filtre / dışa aktar).  
- [ ] **Tablo**: yapışkan başlık, sütun gizleme, satır detay genişletme, CSV/XLSX.  
- [ ] **Form**: alan bazlı hata, `aria-describedby`, loading state on submit.  
- [ ] **Grafik**: Chart.js veya benzeri tek seçim; Partner finans/performans, Admin ticari içgörü, Firma harcama, Satış rapor.  
- [ ] **Boş durum**: ikon + kısa metin + birincil CTA.  
- [ ] **Bildirim**: TempData anahtarlarını **tekilleştir** + Developer dahil.

### B. Sayfa özel (özet öncelik)

1. **Admin `Backups`** — içerik: yedek job durumu, son çalışma, disk/KB (şu an boş section; veri bağlama).  
2. **Admin `CommerceInsight` / `SystemHealth`** — küçük grafikler + tarih seçici.  
3. **Partner `Pricing` / `Finance`** — takvim + çizgi grafik birleşimi.  
4. **Firma `Spending` / `DealsCompare`** — karşılaştırma görselleştirme.  
5. **Satış `CreateReservation` + asistan** — tek akış sihirbazı.  
6. **User `Loyalty`** — gamification görsel tier.  

### C. Teknik borç

- [ ] Toast anahtarları için küçük helper (`PanelTempData.SuccessKey(panel)`) veya partial güncellemesi.  
- [ ] Admin section sayfalarında ortak **filtre çubuğu** partial’ı.  
- [ ] Mobil: her sayfa `.mobile.css` ile kritik tabloların kart görünümü.

---

**Sonuç:** Panel ailesi güçlü bir shell ve operasyon ekranlarına sahip; **ultra seviye** için birleşik grafik/tablo/form katmanı, toast tutarlılığı ve **`Backups` view** gibi kritik eksiklerin kapatılması önceliklidir.

---

*Bu belge kod tabanı taramasıyla üretilmiştir; canlı ortamda sayfa sayfa tıklama testi önerilir.*
