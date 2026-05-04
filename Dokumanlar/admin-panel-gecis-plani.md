# Admin Panel Tabler Geçiş Master Planı

## Amaç

Admin paneli partner panel standardına uygun Tabler tabanlı shell, dashboard, tablo, kart, filtre, form, modal, durum rozeti ve mobil davranış standardına taşınacak. Her sayfa kendi route adıyla izlenebilir dosyalara ayrılacak.

## Dizin Sözleşmesi

- Controller: `Controllers/Paneller/Admin/AdminPanelController.cs`
- View model: `Models/Paneller/Admin/AdminPanelViewModels.cs` veya sayfa adına özel model dosyası.
- Service arayüzü: `Services/Abstractions/IAdminService.cs` veya özel servis arayüzü.
- Service: `Services/AdminService.cs` veya sayfa adına özel servis.
- View: `Views/Paneller/Admin/{PageName}.cshtml`
- CSS: `wwwroot/assets/css/paneller/admin/{page-name}.css`
- Mobil CSS: `wwwroot/assets/css/paneller/admin/{page-name}.mobile.css`
- Ortak shell CSS: `wwwroot/assets/css/paneller/admin/shell.css` ve `shell.mobile.css`
- Geçiş süresinde mevcut dosyalar korunacak, yeni sayfalarda `paneller/admin/...` yolu kullanılacak.

## Ortak UI Standardı

- Shell: Tabler `navbar-vertical`, sticky topbar, footer, mobile off-canvas menu.
- Kart: 8-12px radius, hafif gölge, gereksiz nested card yok.
- Tablo: filtre üstte, sayfa başı seçim, durum rengi sabit, aksiyonlar yazılı buton.
- Durum renkleri:
  - `Onaylandı`, `Tamamlandı`, `Giriş Yaptı`, `Aktif`: yeşil geçişli.
  - `Bekliyor`, `Onay Bekliyor`, `İşleniyor`: sarı geçişli.
  - `Reddedildi`, `İptal Edildi`, `Başarısız`, `Pasif`: kırmızı geçişli.
  - `Taslak`, `Bilgi`, `SMTP Kabul`: mavi/nötr geçişli.
- Filtreler: tarih aralığı, durum, kaynak, otel, partner, şehir/ilçe/mahalle, arama, sayfa başı.
- Aksiyon güvenliği: kritik işlemlerde gerekçe zorunlu ve admin işlem logu.

## Öncelik Sırası

1. Shell/layout/sidebar/topbar/footer ortak admin tema geçişi.
2. Dashboard KPI, ciro, komisyon, rezervasyon, partner evrak, e-posta kuyruk özetleri.
3. Ticari operasyon: rezervasyon, ödeme, fatura, komisyon, sözleşme.
4. Partner ve otel yönetimi: başvuru, evrak, otel, oda, görsel, koordinat, kampanya.
5. Kullanıcı/yetki yönetimi: kullanıcı, yönetici, roller, rol-yetki matrisi.
6. Konum yönetimi: il, ilçe, mahalle, koordinat kalite ve arama logları.
7. İletişim: e-posta hesapları, şablonlar, kuyruk, mail merkezi, WhatsApp Cloud API.
8. Sistem: sağlık, ayarlar, rate limit, sitemap, yedek, loglar, audit.

## Route ve Sayfa Planı

| Route | View | CSS | Veri / Service |
|---|---|---|---|
| `/admin/dashboard` | `Dashboard.cshtml` | `paneller/admin/dashboard.css` | KPI, rezervasyon, ciro, komisyon, son hareketler |
| `/admin/sistem-sagligi` | `SystemHealth.cshtml` | `paneller/admin/system-health.css` | DB, email, queue, servis kontrolleri |
| `/admin/rezervasyonlar-tek-liste` | `UnifiedReservations.cshtml` | `paneller/admin/unified-reservations.css` | tüm rezervasyonlar, kaynak, durum, tutar |
| `/admin/rezervasyonlar` | `Reservations.cshtml` | `paneller/admin/reservations.css` | bireysel rezervasyonlar |
| `/admin/firma-rezervasyonlari` | `CompanyReservations.cshtml` | `paneller/admin/company-reservations.css` | firma rezervasyonları |
| `/admin/odemeler` | `Payments.cshtml` | `paneller/admin/payments.css` | ödeme hareketleri |
| `/admin/faturalar` | `Invoices.cshtml` | `paneller/admin/invoices.css` | fatura durumları |
| `/admin/komisyonlar` | `Commissions.cshtml` | `paneller/admin/commissions.css` | partner otel komisyon oranları, vergi, net komisyon |
| `/admin/sozlesmeler` | `Contracts.cshtml` | `paneller/admin/contracts.css` | partner sözleşme ve PDF evrak |
| `/admin/partner-basvurulari` | `PartnerApplications.cshtml` | `paneller/admin/partner-applications.css` | partner evrak, e-posta giriş onayı |
| `/admin/firma-basvurulari` | `CompanyApplications.cshtml` | `paneller/admin/company-applications.css` | firma başvuru onay/red |
| `/admin/oteller` | `Hotels.cshtml` | `paneller/admin/hotels.css` | otel listeleme, il/ilçe/mahalle, yayın durumu |
| `/admin/otel-detay/{id}` | `HotelDetail.cshtml` | `paneller/admin/hotel-detail.css` | otel, oda, görsel, fiyat, koordinat |
| `/admin/acik-oteller` | `ActiveHotels.cshtml` | `paneller/admin/active-hotels.css` | açık tesisler |
| `/admin/bekleyen-oteller` | `PendingHotels.cshtml` | `paneller/admin/pending-hotels.css` | onay bekleyen tesisler |
| `/admin/degerlendirmeler` | `Reviews.cshtml` | `paneller/admin/reviews.css` | yorum onay/red, rezervasyon doğrulama |
| `/admin/kampanyalar` | `Campaigns.cshtml` | `paneller/admin/campaigns.css` | kampanya ve indirim yönetimi |
| `/admin/kullanicilar` | `Users.cshtml` | `paneller/admin/users.css` | kullanıcı hesabı, rol, doğrulama |
| `/admin/yoneticiler` | `Managers.cshtml` | `paneller/admin/managers.css` | admin kullanıcıları |
| `/admin/platform-yetkilileri` | `PlatformOfficials.cshtml` | `paneller/admin/platform-officials.css` | yetkili tanımı |
| `/admin/roller-yetkiler` | yeni | `paneller/admin/roles-permissions.css` | roller, yetkiler, rol_yetkileri |
| `/admin/konumlar` | yeni | `paneller/admin/locations.css` | iller, ilçeler, mahalleler, koordinat kalite |
| `/admin/eposta-sablonlari` | `EmailTemplates.cshtml` | `paneller/admin/email-templates.css` | şablon, gönderici eşleşmesi |
| `/admin/email-kuyruk` | `EmailQueue.cshtml` | `paneller/admin/email-queue.css` | kuyruk, retry, başarısız kayıt |
| `/admin/mail-merkezi` | `MailCenter.cshtml` | `paneller/admin/mail-center.css` | platform e-posta hesapları |
| `/admin/whatsapp-cloud-api` | `WhatsAppCloudApi.cshtml` | `paneller/admin/whatsapp-cloud-api.css` | WhatsApp ayar ve test |
| `/admin/bildirimler` | `Notifications.cshtml` | `paneller/admin/notifications.css` | panel bildirimleri |
| `/admin/ticari-icgoru` | `CommerceInsight.cshtml` | `paneller/admin/commerce-insight.css` | vitals, growth, fiyat örnekleri |
| `/admin/islem-loglari` | `AdminActionLogs.cshtml` | `paneller/admin/action-logs.css` | admin audit |
| `/admin/rate-limit` | `RateLimitStats.cshtml` | `paneller/admin/rate-limit.css` | 429 ve endpoint yoğunluğu |
| `/admin/ayarlar-monitor` | `SettingsMonitor.cshtml` | `paneller/admin/settings-monitor.css` | kritik ayar read-only |
| `/admin/sitemap` | `Sitemap.cshtml` | `paneller/admin/sitemap.css` | sitemap yenileme |
| `/admin/guvenlik` | `Security.cshtml` | `paneller/admin/security.css` | güvenlik olayları |
| `/admin/yedekleme` | `Backups.cshtml` | `paneller/admin/backups.css` | DB yedek durumu |
| `/admin/log-kayitlari` | `Logs.cshtml` | `paneller/admin/logs.css` | uygulama logları |

## Veri Tabanı Kontrol Listesi

- Komisyon:
  - `komisyon_muhasebe_kayitlari`
  - komisyon kural tablosu mevcut değilse `admin_komisyon_kurallari` veya mevcut eşdeğer tablo standardize edilecek.
  - `rezervasyonlar.komisyon_tutari`, ödeme durumu ve partner net hakediş eşleştirilecek.
- Partner evrak:
  - `partner_detaylari`, `partner_application_assets`, sözleşme PDF güvenli dosya alanları.
  - Evrak durumu: bekliyor/onaylandı/reddedildi/eksik.
- Rezervasyon:
  - `rezervasyonlar`, `rezervasyon_durum_tanimlari`, finans kırılımları.
  - Giriş yaptı/tamamlandı durumu komisyon hesap tetikleyicisi olacak.
- Kullanıcı/rol/yetki:
  - `users`, `yetkiler`, `rol_yetkileri`.
  - Rol matrisi admin panelden yönetilebilir olacak.
- Konum:
  - `iller`, `ilceler`, `mahalleler`.
  - Eksik koordinat, hatalı slug, mükerrer mahalle kontrolü.
- E-posta:
  - `email_services`, `bildirim_sablonlari`, `bildirim_loglari`, `platform_email_hesaplari`, `platform_email_mesajlari`.
  - Production’da `test_modu=0`, aktif SMTP, şifre dolu, `transport_mode=smtp`.
  - `appsettings.Production.json` boş connection string ile canlı bağlantıyı ezmeyecek.
- Audit:
  - kritik admin post işlemleri `admin_islem_loglari` içine gerekçe ile yazılacak.

## E-posta Canlı Çalışma Standardı

- `EmailDeliveryBackgroundService` her ortamda hosted service olarak kayıtlı kalacak.
- Canlıda pickup fallback değil SMTP kullanılacak.
- Test modu yalnızca development ortamında pickup directory için kullanılacak.
- Kuyrukta `Beklemede`, eski `İşleniyor`, `Başarısız` durumları admin panelden retry edilecek.
- Gönderici adı kullanıcıya `otelturizm.com` olarak gidecek.
- Şablon başlıkları tarih/rezervasyon no içerecek; Gmail thread çakışması azaltılacak.

## Uygulama Aşamaları

- Aşama 1: Admin dashboard ve shell Tabler standardı.
- Aşama 2: Komisyonlar + partner evrak + sözleşmeler.
- Aşama 3: Rezervasyon/ciro/komisyon tek liste.
- Aşama 4: Otel/oda/görsel/konum yönetimi.
- Aşama 5: Kullanıcı/rol/yetki/mahalle kontrol panelleri.
- Aşama 6: E-posta merkezi ve canlı teslimat monitörü.
- Aşama 7: Sistem sağlığı, log, rate limit, yedek ve audit ekranları.
