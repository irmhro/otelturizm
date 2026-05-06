# Admin paneli (`/admin`) — eksiklik envanteri ve tamamlanan işler

## Kapsam

| Bileşen | Konum |
|--------|--------|
| Controller | `Controllers/Paneller/Admin/AdminPanelController.cs` (`[Route("admin")]`) |
| Layout | `Views/Paneller/Admin/_AdminPanelLayout.cshtml` |
| Menü | `Views/Paneller/Admin/_AdminSidebar.cshtml`, `_AdminTopNav.cshtml` |
| Mobil | `Views/Paneller/Admin/_AdminMobileNav.cshtml` |
| Stil | `wwwroot/assets/css/panel-admin-shell*.css`, `paneller/admin/*`, partner `shell*.css` |

## Planlanan eksiklikler ve durum

| # | Konu | Durum |
|---|------|--------|
| 1 | Kenar çubuğu akordeonları: alt sayfadayken doğru grubun açık kalması (`IsOpen`) — `UnifiedReservations`, `HotelDetail`, `EmailQueue`, `SettingsMonitor`, `SecurityEvents`, `UploadHistory`, `AdminActionLogs`, `RateLimitStats` vb. | Tamamlandı |
| 2 | Menü ile RBAC uyumu: endpoint izinleri olan ama menüde `Can()` olmayan öğeler (`Firma Rezervasyonları`, `Otel Liste Abonelikleri`) | Tamamlandı |
| 3 | Mobil alt gezinme: `AdminPanel` controller, Türkçe (Çıkış), RBAC ile görünürlük, otel/rezervasyon/kullanıcı bölümlerinde birleşik aktif durum, çok öğe için yatay kaydırma | Tamamlandı |
| 4 | Kullanılmayan/yanıltıcı görünüm: `Views/Paneller/Admin/Reviews.cshtml` (controller `ReviewsModeration.cshtml` kullanıyor) | Kaldırıldı |
| 5 | Küçük Türkçe ve kullanıcı metinleri (`HotelDetail` uyarısı, `GetFullName` varsayılanı, bölüm boş mesajı) | Tamamlandı |

## Teknik notlar

- **Yetki:** `admin.company_reservations`, `admin.listing_subscriptions` vb. tanımları `Database/MigrationsSql/*admin_rbac*.sql` ile uyumludur; menü `d-none` ile uçları gizler, gerçek kontrol controller’dadır.
- **Mobil alt çubuk:** `admin-mobile-nav--scroll` sınıfı `panel-admin-shell.mobile.css` içinde flex + yatay kaydırmayı etkinleştirir; rezervasyon sekmesi `admin.reservations` yoksa `admin.unified_reservations` ile `UnifiedReservations`’a gider.

## Doğrulama

`dotnet build "D:\otelturizm\otelturizmnew.csproj" --no-restore`

## 2026-05-06 Admin Sidebar Eksik Gelişim Checklist

Bu fazda partner paneli Cursor tarafında ilerlediği için admin panel önceliklidir. Admin menüsünde tanımlı her sayfa aynı iskelet, tablo, filtre, aksiyon ve doğrulama standardına alınacaktır.

| Faz | Alan | Durum | Gelişim Notu |
|---|---|---|---|
| 1 | Admin iskelet | Başladı | `_AdminPanelLayout`, `_AdminSidebar`, `_AdminTopNav`, `_AdminFooter`, mobil menü ve ortak section görünümü sabit Tabler standardına alınacak. |
| 2 | Placeholder sayfalar | Başladı | `RenderSectionAsync` kullanan kısa görünümler modern özet kart + tablo yapısına çekilecek; sonrasında sayfa sayfa özel servis/view ayrımı yapılacak. |
| 3 | Konaklama yönetimi | Sırada | Otel taslak/yayın/askı/onay, belge durumu, oda, fiyat, fotoğraf, koordinat ve komisyon bağlantıları tek admin akışında tamamlanacak. |
| 4 | Ticari operasyon | Sırada | Rezervasyonlar, firma rezervasyonları, ödemeler, faturalar, komisyonlar, mutabakat ve sözleşmeler tablo + aksiyon standardına alınacak. |
| 5 | Kullanıcı/yetki | Sırada | Kullanıcı silme/askıya alma, rol/departman atama, yönetici ve platform yetkilisi süreçleri loglu çalışacak. |
| 6 | Onay merkezi | Sırada | Partner/firma/otel/evrak/fatura/abonelik onayları tek görev kuyruğu mantığıyla bağlanacak. |
| 7 | İçerik ve iletişim | Sırada | Mail merkezi, e-posta kuyruğu, şablonlar, yönlendirmeler, destek makaleleri ve bildirimler üretim takibiyle güçlendirilecek. |
| 8 | Sistem ve güvenlik | Sırada | Checkup, sistem sağlığı, güvenlik olayları, upload geçmişi, loglar, konum logları ve yedekleme ekranları gerçek veriyle tamamlanacak. |

### İlk Envanter

Özel sayfa olarak gelişmiş olanlar: `Dashboard`, `SystemHealth`, `PlatformCheckup`, `ApprovalCenter`, `Hotels`, `HotelDetail`, `Commissions`, `Contracts`, `PartnerApplications`, `CompanyApplications`, `ListingSubscriptions`, `DevelopmentRequests`, `ReviewsModeration`, `EmailQueue`, `EmailRouting`, `MailCenter`, `EmailTemplates`, `SupportArticles`, `Users`, `WhatsAppCloudApi`.

Ortak section iskeletiyle çalışan ve sırayla özel sayfaya dönüştürülecekler: `ActiveHotels`, `PendingHotels`, `Reservations`, `Payments`, `Invoices`, `Reports`, `Campaigns`, `Notifications`, `Settings`, `Security`, `Blog`, `Faq`, `Complaints`, `Logs`, `GeoSearchLogs`, `HotelCoordinateChanges`, `CompanyReservations`, `Backups`, `Managers`, `PlatformOfficials`.

### Sidebar Sayfa Aksiyon Envanteri

| Menü | Sayfa | Eksik/Çalışacak Aksiyon | Durum |
|---|---|---|---|
| Konaklama | Açık Oteller | Otel detay, yayını kapat, komisyon/fotoğraf/oda bağlantısı, gelişmiş il/ilçe/mahalle filtreleri | Geliştirildi: gelişmiş otel tablosuna bağlandı |
| Konaklama | Bekleyen Oteller | Evrak/bilgi/komisyon kontrolü, yayına al, reddet/askıya al, detay bağlantısı | Geliştirildi: gelişmiş otel tablosuna bağlandı |
| Ticari Operasyon | Rezervasyonlar | Durum, ödeme, otel, kullanıcı ve tarih filtreleri; detay ve müdahale aksiyonları | Geliştirildi: tek liste altyapısına bağlandı, detay aksiyonları sırada |
| Ticari Operasyon | Ödemeler | Tahsilat, iade, başarısız ödeme, provider referans ve mutabakat takibi | Geliştirildi: filtreli/sayfalı özel tabloya bağlandı; iade/onay aksiyonları sırada |
| Ticari Operasyon | Faturalar | Partner/kullanıcı/firma faturası, indirme, onay, iptal ve eksik belge takibi | Geliştirildi: filtreli/sayfalı özel tabloya bağlandı; onay/iptal/yükleme aksiyonları sırada |
| Ticari Operasyon | Raporlar | Otel bazlı ciro/komisyon CSV, dönem filtresi ve aylık kırılım | Geliştirildi: dönem/otel filtreli özel rapor sayfasına bağlandı |
| Konaklama | Kampanyalar | Kampanya oluşturma, otel katılımı, aktif/pasif, kullanım raporu | Sırada |
| İletişim | Bildirimler | Sistem bildirimi oluşturma, hedef kitle, okundu/arşiv aksiyonları | Sırada |
| Sistem | Ayarlar/Güvenlik | Canlı servis ayarları, 2FA, IP, rate limit ve audit kayıtları | Sırada |
| Sistem | Log/Konum/Yedekleme | Log filtreleme, konum logları, koordinat değişimleri, yedek kayıtları | Sırada |
