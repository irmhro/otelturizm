# Panel Geliştirme Takibi

Canlı checklist. Tasarım kuralları: `panel-tasarim-sistemi.md`. Platform sırası: `Docs/PLATFORM_MASTER_EXECUTION_ORDER.md`.

## Genel Hedef

Tüm paneller Tabler şablon yapısına taşınacak. Her aşamada önce shell/layout, sonra dashboard, sonra işlem sayfaları geliştirilecek.

## Çalışma Kuralı

- Önce ilgili panelin layout/sidebar/header/footer dosyaları güncellenecek.
- Sonra dashboard Tabler kart, filtre, tablo ve aksiyon standardına taşınacak.
- Daha sonra işleyiş sayfaları tek tek ele alınacak.
- CSS dosyaları panel ve sayfa adına göre ayrılacak.
- Sayfa geliştirmelerinde dosya/route ad standardı korunacak: route hangi sayfayı temsil ediyorsa `.cs`, `.cshtml`, `.css` ve varsa `.mobile.css` dosya adları aynı sayfa adıyla geliştirilecek.
- MCP server varsa dar kapsamlı MCP okuma kullanılacak; MCP yoksa sadece ilgili dosyalar okunacak.

## Kullanıcı Paneli

- [x] Kullanıcı panel shell yapısı Tabler düzenine geçirildi.
- [x] Kullanıcı sidebar Tabler `navbar-vertical` yapısına geçirildi.
- [x] Kullanıcı header/topbar Tabler yapısına geçirildi.
- [x] Kullanıcı footer eklendi.
- [x] Kullanıcı dashboard Tabler kart, filtre, tablo ve aksiyon düzenine geçirildi.
- [x] Kullanıcı rezervasyonlarım sayfası Tabler listeleme standardına taşındı; gelişmiş filtre, sayfa başı listeleme ve mobil kart görünümü eklendi.
- [x] Kullanıcı favorilerim sayfası Tabler kart standardına taşındı; responsive kartlar ve fiyat alarmı modalı güncellendi.
- [x] Kullanıcı yorumlarım sayfası eklendi; sayfa başı 5 listeleme, otele/şehre/rezervasyon no'ya göre arama ve yorum yazma/düzenleme/silme akışı eklendi.
- [x] Kullanıcı rezervasyon yorum formu Tabler sayfa düzenine taşındı; kategori puanları yıldız seçiciye çevrildi ve üst bilgi kartı sıkılaştırıldı.
- [x] Kullanıcı puanlarım sayfası kendi `loyalty.css` ve `loyalty.mobile.css` dosyalarına ayrıldı.
- [ ] Kullanıcı mesajlarım sayfası Tabler sohbet/listeme standardına taşınacak.
- [ ] Kullanıcı profil bilgilerim sayfası `profile.cshtml/profile.css/profile.mobile.css` standardında son görsel kontrolle tamamlanacak.
- [ ] Kullanıcı ödeme yöntemleri sayfası `payment-methods.cshtml/payment-methods.css/payment-methods.mobile.css` standardına taşınacak.
- [ ] Kullanıcı bildirim tercihleri sayfası `notifications.cshtml/notifications.css/notifications.mobile.css` standardına taşınacak.
- [x] Kullanıcı güvenlik ve giriş sayfasında hero alanı sıkılaştırıldı; 2FA e-posta anahtarı ve kanal seçimi çalışır hale getirildi.
- [x] `/UserPanel` ve `/UserPanel/Index` eski giriş linkleri `/panel/user/dashboard` aksiyonuna bağlandı.
- [x] Canlı ortam e-posta teslimi için production ortamında boş connection string override kaldırıldı; e-posta hosted service sürekli kayıtlı kalacak şekilde doğrulandı.
- [ ] Canlı DB'de `email_services`, `bildirim_sablonlari`, `bildirim_loglari` şeması ve SMTP şifresi doğrulanacak; işlemden önce canlı DB yedeği alınacak.

## Partner Paneli

- [x] Partner panel Tabler shell yapısı aktif.
- [x] Partner dashboard rezervasyon tablosu ve sohbet alanı güncellendi.
- [x] Partner rezervasyon durum renkleri sabitlendi.

## Firma Paneli

- [x] Firma shell/layout Tabler varlıkları ve panel köprüsüyle uyumlu hale getirildi.
- [x] Firma sidebar koyudan açığa kurumsal geçişli renge taşındı.
- [x] Firma header ve kart radius/gölge standardı partner-kullanıcı panel çizgisine yaklaştırıldı.
- [ ] Firma dashboard işlem kartları ve tablo durum renkleri partner panel standardına göre tek tek geliştirilecek.

## Admin Paneli

- [x] Admin layout Tabler varlıkları ve panel köprüsüyle birlikte çalışacak şekilde güncellendi.
- [x] Admin sidebar/header yüzeyi koyudan açığa geçişli shell standardına yaklaştırıldı.
- [x] Admin card/small-box radius ve gölge standardı güncellendi.
- [x] Admin master geçiş planı `Dokumanlar/Planlar/admin-panel-gecis-plani.md` dosyasına route, cshtml, css, db ve e-posta canlı servis başlıklarıyla işlendi.
- [x] Admin dashboard AdminLTE `small-box` kalıntısından çıkarıldı; `paneller/admin/dashboard.css` ve `dashboard.mobile.css` sayfa standardına taşındı.
- [x] Admin komisyonlar sayfası `paneller/admin/commissions.css` standardına taşındı.
- [x] Admin partner başvuruları sayfası `paneller/admin/partner-applications.css` standardına taşındı.
- [x] Admin rezervasyonlar tek liste sayfası `paneller/admin/unified-reservations.css` standardına taşındı.
- [x] Platform checkup raporu `Dokumanlar/Planlar/platform-checkup-gelisim-raporu.md` dosyasına işlendi.
- [x] Admin `platform-checkup` sayfası eklendi.
- [x] Eksik gelişim planı `Dokumanlar/Planlar/admin-firma-partner-satis-eksik-gelisim-plani.md` dosyasına işlendi.
- [x] Admin `onay-merkezi` sayfası eklendi.
- [ ] Admin roller-yetkiler ve konumlar sayfaları eklenecek.

## Departman Panelleri

- [x] Departman panel temel route yapısı `/panel/departman/dashboard` ve `/panel/departman/{departman}` olarak oluşturuldu.
- [x] Departman hesap tipi `department/departman` auth yönlendirmesine bağlandı.
- [x] Departman dashboard iskeletleri eklendi.
- [x] Departman shell/sidebar/header/footer ayrı CSS dosyalarına alındı.
- [x] Departman rol hazırlığı `Database/MigrationsSql/20260504_department_panel_roles.sql` dosyasına alındı.
- [ ] Kalıcı departman kullanıcıları açık işlem onayı sonrası güvenli seed edilecek.
