# Panel Tasarım Yapılandırması

Bu doküman, `D:\otelturizm\wwwroot\paneltematabler` içindeki Tabler tema dosyalarını Otelturizm panellerine uyarlamak için referans sözleşmedir. Partner paneli başta olmak üzere admin, firma ve kullanıcı panellerinde aynı iskelet ve bileşen dili kullanılacaktır.

## 1) Ana Tema Kaynağı

- Tema kökü: `wwwroot/paneltematabler`
- Ana iskelet referansı: `layout-fluid-vertical.html`
- CSS referansı: `wwwroot/paneltematabler/dist/css/tabler.min.css`
- Vendor CSS referansı: `wwwroot/paneltematabler/dist/css/tabler-vendors.min.css`
- JS referansı: `wwwroot/paneltematabler/dist/js/tabler.min.js`

Panel layout dosyaları `~/vendor/tabler` kullanmayacak. Projede çalışan ve indirilen tema dizini `~/paneltematabler/dist/...` yoludur.

## 2) Panel İskeleti

Her panel şu Tabler sırasını izler:

1. `<body class="layout-fluid">`
2. `<div class="page">`
3. `<aside class="navbar navbar-vertical navbar-expand-lg" data-bs-theme="dark">`
4. `<div class="page-wrapper">`
5. `<header class="navbar navbar-expand-md d-print-none">`
6. `<div class="page-body"><div class="container-fluid">`
7. `<footer class="footer footer-transparent d-print-none">`

Partner panelindeki ana dosyalar:

- `Views/Paneller/Partner/_PartnerPanelLayout.cshtml`
- `Views/Paneller/Partner/_PartnerSidebar.cshtml`
- `Views/Paneller/Partner/_PartnerPanelFooter.cshtml`
- `Views/Paneller/Partner/Dashboard.cshtml`
- `wwwroot/assets/css/paneller/partner/tabler-bridge.css`
- `wwwroot/assets/css/paneller/partner/shell.css`
- `wwwroot/assets/css/paneller/partner/dashboard.css`

## 3) Sidebar Standardı

`layout-fluid-vertical.html` yaklaşımı korunur:

- Koyu dikey sidebar kullanılır.
- Logo üstte `navbar-brand navbar-brand-autodark` içinde yer alır.
- Ana menüler `ul.navbar-nav pt-lg-3` altında tutulur.
- Ana linkler `nav-item > nav-link` standardındadır.
- Alt menüler Bootstrap collapse ile açılır.
- Rozetler `badge bg-*-lt text-*` sınıflarıyla sağa hizalanır.
- Aktif sayfa `nav-link active` ile belirtilir.

Partner menü grupları:

- Anasayfa
- Rezervasyonlar
- Takvim & Fiyatlar
- Firma Fiyatları
- Kampanyalar
- Oda Yönetimi
- Tesis Dokümanları
- Performans
- Destek ve Güvenlik

## 4) Header Standardı

Header alanı `navbar navbar-expand-md d-print-none` yapısını kullanır.

Zorunlu alanlar:

- Seçili otel adı
- Tema ayar butonu
- Bildirim rozeti
- Yayın durumu rozeti
- Kullanıcı avatar/dropdown
- Profil, güvenlik ve çıkış aksiyonları

Header yüksekliği kompakt kalmalı, ana içerik alanını aşağı itmemelidir. Mobilde gereksiz metinler gizlenir, sadece ikon ve menü aksiyonları kalır.

## 5) Footer Standardı

Footer `footer footer-transparent d-print-none` sınıfıyla kullanılır.

Zorunlu bilgiler:

- Seçili otel
- Partner ID
- Lokal tarih/saat
- Gizlilik, kullanım koşulları, KVKK linkleri

Footer operasyonel bilgi verir; satış/pazarlama metni veya uzun acenta bilgisi burada tutulmaz.

## 6) Dashboard Kart Standardı

Tabler dashboard kartlarında şu yapı kullanılacak:

```html
<div class="card card-sm">
  <div class="card-body">
    <div class="d-flex align-items-start justify-content-between">
      <div>
        <div class="text-muted text-uppercase fw-bold">Başlık</div>
        <div class="h1 m-0 mt-2">Değer</div>
        <div class="text-muted mt-1">Açıklama</div>
      </div>
      <span class="avatar bg-primary-lt text-primary">...</span>
    </div>
    <div class="progress progress-sm mt-3">
      <div class="progress-bar bg-primary" style="width:72%"></div>
    </div>
  </div>
</div>
```

Kart tipleri:

- KPI kartı: rezervasyon, gelir, doluluk, yorum, stok
- Aksiyon kartı: hızlı işlem butonları
- Tablo kartı: son rezervasyonlar, stok uyarıları
- Liste kartı: yorumlar, bildirimler, görevler
- Grafik kartı: gelir, trafik, dönüşüm, kanal performansı
- Durum kartı: yayın, ceza, doğrulama, doküman eksikleri

## 7) Home

Kaynak örnekler:

- `index.html`
- `activity.html`
- `gallery.html`
- `invoice.html`
- `profile.html`
- `settings.html`

Kullanım alanları:

- Dashboard ana sayfa
- Aktivite akışı
- Tesis görsel yönetimi
- Fatura/komisyon dokümanı
- Partner profil/tema ayarları

Kullanılacak parçalar:

- `page-header`
- `row row-cards`
- `card card-sm`
- `activity`
- `avatar`
- `table table-vcenter`
- `empty`

## 8) Interface

Kaynak örnekler:

- `accordion.html`
- `alerts.html`
- `avatars.html`
- `badges.html`
- `buttons.html`
- `cards.html`
- `card-actions.html`
- `datagrid.html`
- `dropdowns.html`
- `empty.html`
- `lists.html`
- `modals.html`
- `pagination.html`
- `progress.html`
- `steps.html`
- `tabs.html`
- `tables.html`

Kullanım alanları:

- Rezervasyon onay/red kartları
- Kampanya durum rozetleri
- Firma fiyat tabloları
- Oda/fiyat takvimi
- Bildirim listeleri
- Modal ve dialog akışları

Kurallar:

- Ana aksiyonlar `btn btn-primary`
- İkincil aksiyonlar `btn btn-outline-primary`
- Tehlikeli aksiyonlar `btn btn-outline-danger` veya `btn btn-danger`
- Durumlar `badge bg-*-lt text-*`
- Tablolar `table table-vcenter`
- Boş durumlar `empty` bileşeni

## 9) Forms

Kaynak örnekler:

- `form-elements.html`
- `form-layout.html`
- `form-validation.html`
- `form-wizard.html`
- `colorpicker.html`
- `dropzone.html`

Kullanım alanları:

- Otel bilgileri
- Oda yönetimi
- Fiyat/müsaitlik güncelleme
- Firma fiyatları
- Fotoğraf yükleme
- Tema ayarları

Kurallar:

- Etiketler `form-label`
- Inputlar `form-control`
- Selectler `form-select`
- Yardım metni `form-hint`
- Hatalar `invalid-feedback`
- Çoklu seçimler `form-selectgroup`
- Dosya yükleme `dropzone` yaklaşımıyla tasarlanır, fakat dosya yolu standardı proje servisleriyle korunur.

## 10) Extra

Kaynak örnekler:

- `charts.html`
- `chat.html`
- `emails.html`
- `faq.html`
- `job-listing.html`
- `logs.html`
- `markdown.html`
- `pricing.html`
- `tasks.html`
- `users.html`

Kullanım alanları:

- Gelir grafikleri
- Partner destek mesajları
- E-posta bildirim geçmişi
- Yardım merkezi
- İş/görev takibi
- Kullanıcı yetkilendirme

Grafiklerde ApexCharts kullanılabilir. Grafik JS’i ilgili sayfada lazy yüklenmeli, global layout şişirilmemelidir.

## 11) Layout

Kaynak örnekler:

- `layout-fluid-vertical.html`
- `layout-vertical.html`
- `layout-fluid.html`
- `layout-horizontal.html`
- `layout-boxed.html`
- `layout-condensed.html`
- `layout-combo.html`
- `layout-navbar-sticky.html`
- `layout-navbar-dark.html`
- `layout-vertical-transparent.html`
- `layout-vertical-right.html`
- `layout-rtl.html`

Otelturizm panel varsayılanı:

- Partner/Admin/Firma: `layout-fluid-vertical.html`
- İçerik genişliği: `container-fluid`
- Sidebar: koyu, dikey, collapse destekli
- Header: sticky, cam efekti, kompakt
- Footer: transparent

## 12) Plugins

Kaynak örnekler:

- `datatables.html`
- `fullcalendar.html`
- `lightbox.html`
- `maps.html`
- `maps-vector.html`
- `map-fullsize.html`
- `inline-player.html`

Kullanım alanları:

- Rezervasyon listeleri için DataTables
- Fiyat takvimi için calendar/grid
- Fotoğraflar için lightbox
- Otel konumu için harita
- Eğitim/video içerikleri için inline player

Plugin JS/CSS sadece ihtiyaç duyulan sayfada yüklenmelidir.

## 13) Addons

Kaynak örnekler:

- `icons.html`
- `flags.html`
- `illustrations.html`
- `colors.html`
- `cookie-banner.html`

Kullanım alanları:

- Menü ikonları
- Dil/bölge rozetleri
- Boş durum illüstrasyonları
- Tema renk paleti
- Çerez/onay bannerları

İkon standardı mevcut projede FontAwesome’dur. Tabler Icons kullanılacaksa sayfa bazlı ve tutarlı geçirilmelidir.

## 14) Help

Kaynak örnekler:

- `changelog.html`
- `license.html`
- `error-404.html`
- `error-500.html`
- `error-maintenance.html`

Kullanım alanları:

- Gelişim notları
- Lisans ve sözleşme ekranları
- Panel hata sayfaları
- Bakım modu

## 15) Partner Panel Dönüşüm Notu

Yapılan ilk dönüşüm:

- Partner layout Tabler asset yolunu `~/paneltematabler/dist/...` olarak kullanır.
- Dashboard sayfası `ViewData["PageCssPath"] = "paneller/partner/dashboard"` ile kendi CSS’ini yükler.
- Dashboard KPI kartları `card card-sm`, avatar ve progress standardına yaklaştırılmıştır.
- Hızlı menü butonları Tabler kart diliyle uyumlu özel `partner-quick-link` bileşenine taşınmıştır.

Sonraki dönüşüm sırası:

1. Rezervasyonlar
2. Takvim & Fiyatlar
3. Firma Fiyatları
4. Oda Yönetimi
5. Fotoğraflar
6. Otel Bilgileri
7. Güvenlik ve Tercihler
