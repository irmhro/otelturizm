# Desing Panel Ana Sözleşmesi

Bu dosya, Otelturizm panel sayfalarını baştan kodlarken kullanılacak tekil tasarım uygulama sözleşmesidir. Amaç eski proje kalıntılarını taşımadan, `file:///D:/otelturizm/wwwroot/paneltematabler/` altındaki Tabler örneklerini referans alarak her panel sayfasını aynı kalite ve aynı iskelet diliyle yeniden üretmektir.

## 1) Kesin Kaynak

Panel tasarımlarında birincil kaynak:

`file:///D:/otelturizm/wwwroot/paneltematabler/`

Kullanılacak ana layout:

`file:///D:/otelturizm/wwwroot/paneltematabler/layout-fluid-vertical.html`

Projede kullanılacak asset yolu:

- CSS: `~/paneltematabler/dist/css/tabler.min.css`
- Vendor CSS: `~/paneltematabler/dist/css/tabler-vendors.min.css`
- JS: `~/paneltematabler/dist/js/tabler.min.js`

Eski özel panel iskeletleri, eski sidebar gridleri, eski dashboard kalıntıları ve kullanılmayan CSS dosyaları yeni sayfalara taşınmayacak.

## 2) Panel Kodlama Kuralı

Yeni bir panel sayfası istenirse önce Tabler örneği seçilir, sonra sayfa o örnekten uyarlanır.

Örnek:

- Kullanıcı “rezervasyon listesi tasarla” derse önce `tables.html`, `datatables.html`, `lists.html`, `dropdowns.html`, `modals.html` incelenir.
- Kullanıcı “fiyat takvimi” derse önce `form-layout.html`, `tables.html`, `fullcalendar.html`, `cards.html` incelenir.
- Kullanıcı “oda görselleri” derse önce `gallery.html`, `photogrid.html`, `dropzone.html`, `lightbox.html` incelenir.

Sayfalar doğrudan eski HTML/CSS parçalarından kopyalanmayacak. Tabler sınıfları ve Otelturizm iş kurallarıyla yeniden kodlanacak.

## 3) Ana İskelet

Tüm partner/admin/firma panelleri bu dizilimi korur:

```html
<body class="layout-fluid panel-v2">
  <div class="page">
    <aside class="navbar navbar-vertical navbar-expand-lg" data-bs-theme="dark"></aside>
    <div class="page-wrapper">
      <header class="navbar navbar-expand-md d-print-none"></header>
      <div class="page-body">
        <div class="container-fluid"></div>
      </div>
      <footer class="footer footer-transparent d-print-none"></footer>
    </div>
  </div>
</body>
```

Tabler dışı özel iskelet ancak gerçek işlev gerektiriyorsa eklenir. Tasarım için özel iskelet yazılmaz.

## 4) Sidebar Standardı

Kaynak:

- `layout-fluid-vertical.html`
- `navigation.html`
- `dropdowns.html`

Kurallar:

- Sidebar koyu tema olabilir, içerik alanı okunabilir açık kart yapısında kalabilir.
- Logo `~/uploads/logo/logo.png` üzerinden görünür ve okunur olmalıdır.
- Aktif tesis bilgisi kart gibi şişirilmez; kısa, okunur, normal bilgi satırı olarak gösterilir.
- Ana menü linkleri `nav-item > nav-link` standardında kalır.
- Alt sayfalar için küçük alt menü grupları kullanılabilir.
- Güvenlik ve çıkış aksiyonları sidebar altına sabitlenebilir.
- Başka firmaların ürün isimleri, marka adları veya `zpara` gibi dış sistem menüleri kullanılmaz.

Partner sidebar temel menüleri:

- Dashboard
- Rezervasyonlar
- Takvim & Fiyatlar
- Firma Fiyatları
- Odalar
- Fotoğraflar
- Tesis Bilgileri
- Kampanyalar
- Yorumlar
- Tesis Kullanıcıları
- Destek
- Güvenlik
- Çıkış

Partner alt menüleri:

- Oda Tipleri
- Oda Kontenjanları
- Firma Katkı Payı
- Süper Fiyat
- Ödeme Ayarları
- Kurallar & Kısıtlamalar
- Genel Tanımlar
- Konuk Değerlendirmeleri
- Ayarlar

## 5) Header Standardı

Kaynak:

- `layout-fluid-vertical.html`
- `buttons.html`
- `dropdowns.html`
- `badges.html`

Header’da olacaklar:

- Seçili tesis kısa bilgisi
- Birincil aksiyon butonu
- İkincil aksiyon butonu
- Tema ayar butonu
- Bildirim rozeti
- Kullanıcı menüsü

Header kalabalık olmayacak. Sayfa başlığı header içinde değil, sayfa gövdesindeki `page-header` veya hero alanında verilecek.

## 6) Dashboard Standardı

Kaynak:

- `index.html`
- `widgets.html`
- `charts.html`
- `cards.html`
- `lists.html`

Dashboard yapısı:

- Minimal hero alanı
- KPI kartları
- Hızlı aksiyon listesi
- Yaklaşan rezervasyon tablosu
- Stok/müsaitlik uyarıları
- Gelir grafiği
- Son yorumlar

Dashboard’da büyük koyu bloklar kullanılmayacak. Grafikler sade, beyaz kart içinde, mavi vurgulu ve okunabilir olacak.

## 7) Rezervasyon Listesi Tasarım Kuralı

Kaynak:

- `tables.html`
- `datatables.html`
- `lists.html`
- `badges.html`
- `dropdowns.html`
- `modals.html`

Kullanılacak yapı:

- Üstte `page-header`
- Filtre kartı
- Durum özet KPI kartları
- `table table-vcenter table-hover`
- Durum için `badge bg-*-lt text-*`
- Satır aksiyonları için `btn-list`
- Red/onay gibi kritik işlemler için modal/dialog

Rezervasyon listesinde tablo kolonları:

- Rezervasyon no
- Misafir
- Oda
- Giriş/çıkış
- Gece
- Durum
- Ödeme
- Tutar
- Aksiyon

## 7.1) Panel Listeleme Standardı

Partner, firma, kullanıcı, satış ve admin panellerinde veri listeleyen tüm sayfalar öncelikle tablo mantığıyla tasarlanır. Kart grid yalnızca özet KPI, medya galerisi veya tekrar eden küçük aksiyon kartları için kullanılır. Oda, otel, rezervasyon, evrak, kampanya, abonelik, finans, kullanıcı ve fiyat stok listelerinde standart: filtre barı, sayfa başı seçimi, tablo başlığı, durum rozeti, yazılı aksiyon butonları ve mobil uyumlu satır/kart kırılımıdır.

## 8) Takvim & Fiyatlar Tasarım Kuralı

Kaynak:

- `form-layout.html`
- `form-elements.html`
- `tables.html`
- `fullcalendar.html`
- `cards.html`

Kullanılacak yapı:

- Ay seçimi üst toolbar içinde
- Oda tipi seçimi net ve tek satır
- Çoklu fiyat güncelleme yatay kart olarak
- Takvim görünümü geniş ekranı kullanır
- Normal fiyat, indirimli fiyat, stok, kapalı satış aynı hiyerarşide gösterilir

Tablo/veri yoğunluğu yüksek olduğu için görsel efekt azaltılır, okunabilirlik önceliklidir.

## 9) Firma Fiyatları Tasarım Kuralı

Kaynak:

- `pricing.html`
- `pricing-table.html`
- `form-layout.html`
- `tables.html`

Kullanılacak yapı:

- Firma fiyatları açıklama metni panelden kaldırılır; bilgi gerekiyorsa kısa `form-hint`
- Toplu güncelleme yatay ve takvim üstünde olur
- Ay takvimi fiyat hücreleriyle birlikte gösterilir
- Kurumsal fiyat net şekilde normal fiyatlardan ayrılır

## 10) Oda Yönetimi Tasarım Kuralı

Kaynak:

- `cards.html`
- `gallery.html`
- `form-elements.html`
- `dropzone.html`
- `modals.html`

Kullanılacak yapı:

- Oda kartları görsel + oda adı + kapasite + fiyat + durum
- Oda düzenleme formu Tabler form grid ile yazılır
- Oda özellikleri ikonlu chip veya küçük checkbox gruplarıyla gösterilir
- Görsel yükleme `dropzone` yaklaşımına benzer, fakat proje dosya yolu standardı korunur

## 11) Fotoğraflar Tasarım Kuralı

Kaynak:

- `gallery.html`
- `photogrid.html`
- `dropzone.html`
- `lightbox.html`
- `cards-masonry.html`

Kullanılacak yapı:

- Otel görselleri ve oda görselleri ayrı sekmeler
- Kapak görseli açık şekilde işaretlenir
- Sıralama, silme, kapak yapma aksiyonları görsel kart üstünden yapılır
- Dosya yolu standardı: `wwwroot/uploads/images/{otelId}/hotel/` ve `wwwroot/uploads/images/{otelId}/rooms/{roomId}/`

## 12) Tesis Bilgileri Tasarım Kuralı

Kaynak:

- `settings.html`
- `form-layout.html`
- `steps.html`
- `tabs.html`

Kullanılacak yapı:

- Genel bilgiler
- Adres/konum
- İletişim
- Tesis kuralları
- Vergi/fatura bilgileri
- Yayın durumu

Uzun formlar sekmeli veya adımlı yapı ile bölünecek.

## 13) Kampanyalar Tasarım Kuralı

Kaynak:

- `cards.html`
- `badges.html`
- `pricing.html`
- `lists.html`

Kullanılacak yapı:

- Kampanya kartları
- Katılım durumu rozeti
- Başlangıç/bitiş tarihleri
- İndirim tipi
- Performans kısa metrikleri

Kampanya adı kısa olmalı; UI’da 3 kelimeyi aşan başlıklar daraltılır.

## 14) Yorumlar Tasarım Kuralı

Kaynak:

- `lists.html`
- `cards.html`
- `stars-rating.html`
- `modals.html`

Kullanılacak yapı:

- Son yorumlar listesi
- Puan kırılımları
- Filtreler: en yeni, en iyi, en kötü
- Konu butonları: genel, konum, temizlik, personel, fiyat, sessizlik, ulaşım

Yorum metinlerinde Türkçe karakter bozulması varsa önce DB/veri normalize edilir, CSS ile gizlenmez.

## 15) Destek ve Mesajlaşma Tasarım Kuralı

Kaynak:

- `chat.html`
- `emails.html`
- `lists.html`
- `badges.html`

Kullanılacak yapı:

- Sol konuşma listesi
- Sağ mesaj alanı
- Mesaj durumu
- Ekler
- Cevap formu

Mesaj ekranında aksiyon butonları üstte değil, konuşma bağlamında olmalıdır.

## 16) Güvenlik ve Hesap Tasarım Kuralı

Kaynak:

- `settings.html`
- `2-step-verification.html`
- `2-step-verification-code.html`
- `form-elements.html`

Kullanılacak yapı:

- Şifre değiştir
- 2FA durumu
- Oturumlar
- Güvenlik kayıtları
- Çıkış butonu

Güvenlik ekranında kırmızı aksiyonlar sadece gerçekten tehlikeli işlemlerde kullanılır.

## 17) CSS Dosya Kuralı

Panel ortak iskelet:

- `wwwroot/assets/css/paneller/partner/tabler-bridge.css`
- `wwwroot/assets/css/paneller/partner/shell.css`
- `wwwroot/assets/css/paneller/partner/shell.mobile.css`

Sayfa özel CSS:

- `wwwroot/assets/css/paneller/partner/dashboard.css`
- `wwwroot/assets/css/paneller/partner/reservations.css`
- `wwwroot/assets/css/paneller/partner/pricing.css`
- `wwwroot/assets/css/paneller/partner/company-pricing.css`
- `wwwroot/assets/css/paneller/partner/rooms.css`
- `wwwroot/assets/css/paneller/partner/photos.css`

Kural:

- Ortak layout stilleri sayfa CSS’ine yazılmaz.
- Sayfa CSS’i başka sayfayı etkilemez.
- Eski class isimleri yeni sayfalara taşınmaz.
- Kullanılmayan CSS dosyaları referanssızsa kaldırılır.

## 18) Razor Dosya Kuralı

Partner panel ana dosyaları:

- `_PartnerPanelLayout.cshtml`
- `_PartnerSidebar.cshtml`
- `_PartnerPanelFooter.cshtml`
- `Dashboard.cshtml`

Her sayfada:

```cshtml
@{
    Layout = "~/Views/Paneller/Partner/_PartnerPanelLayout.cshtml";
    ViewData["PartnerShell"] = Model.Shell;
    ViewData["PageCssPath"] = "paneller/partner/sayfa-adi";
}
```

URL üretiminde `@hotelQuery` path’e bitişik yazılmaz.

Doğru:

```cshtml
@{
    string PartnerUrl(string path) => $"{path}{hotelQuery}";
}
<a href="@PartnerUrl("/panel/partner/takvim-fiyatlar")">...</a>
```

Yanlış:

```cshtml
<a href="/panel/partner/takvim-fiyatlar@hotelQuery">...</a>
```

## 19) Dark Mode Kuralı

Panel dark mode’da:

- Sidebar koyu kalabilir.
- İçerik kartları okunabilir olmalıdır.
- Tablo yazıları kontrast kaybetmemelidir.
- Form inputları görünür kalmalıdır.
- Dashboard koyu bloklarla boğulmaz.

Eğer tam dark mode yapılacaksa tüm kart, tablo, dropdown, modal, input, badge ve text-muted renkleri birlikte güncellenir. Yarım dark mode bırakılmaz.

## 20) Sayfa Baştan Kodlama Sırası

Partner panel dönüşümü şu sırayla yapılacak:

1. Dashboard
2. Rezervasyonlar
3. Takvim & Fiyatlar
4. Firma Fiyatları
5. Oda Yönetimi
6. Fotoğraflar
7. Tesis Bilgileri
8. Kampanyalar
9. Yorumlar
10. Tesis Kullanıcıları
11. Destek
12. Güvenlik

Her sayfa tamamlandıktan sonra:

- `dotnet build` alınır.
- Sayfa localhost’ta açılır.
- Mobil görünüm kontrol edilir.
- Eski CSS/CSHTML referansı varsa kaldırılır.

## 21) Kabul Kriteri

Bir panel sayfası tamamlanmış sayılırsa:

- Tabler referans sayfası bellidir.
- Sayfa responsive çalışır.
- Sidebar/header/footer bozulmaz.
- URL’ler doğru query üretir.
- Dark mode veya hibrit mode okunabilir kalır.
- Eski proje kalıntısı class veya kullanılmayan partial taşımaz.
- Build hatasızdır.
