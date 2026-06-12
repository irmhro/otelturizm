Kodlama Sözleşmesi — UI & Yorum Kılavuzu

Amaç
- Kullanıcı-odaklı, yalın ve sürdürülebilir kod. Arayüzler gerçek kullanıcıların (mobil ve masaüstü) beklisini karşılayacak şekilde tasarlanır.

Genel İlkeler
- "Kullanıcı gibi düşün": Her arayüz değişikliğinde önce basit bir kullanıcı senaryosu düşünün (ne görmek isterim?, en önemli bilgi nedir?).
- Yorumlar kısa ve hedefe odaklı olsun: maksimum 1–2 satır; neden (neden bu yapıldı), altta yatan karmaşıklık önemliyse dış belgede detaylandır.
- Kodda büyük açıklama bloklarından kaçının; bunlar sürüm kontrolündeki commit mesajlarına veya proje dokümanına taşınsın.

UI / Tasarım Kuralları
- Bilgi hiyerarşisi: başlık, kısa açıklama/konum, etiketler/öncelikler, görsel (thumbnail), metrikler (puan/yorum), fiyat, CTA.
- Kartlar kompakt olmalı: gereksiz büyük boşluk, aşırı gölge veya büyük tipografik sapmalar olmasın.
- Mobil öncelikli: mobil görünümü düşünerek grid/stack tasarımları kullanın.
- Erişilebilirlik: semantic HTML, alt metin, label, kontrast ve klavye erişilebilirliği zorunlu.
- Panel içi özellik seçimleri, toggle/checkbox/pill butonları ve kart üzerindeki düzenlenebilir alanlar mümkün olduğunca otomatik kayıt mantığıyla çalışır. Kullanıcı seçim yaptığında veya alanı değiştirdiğinde kayıt/sil/değiştir işlemi anlık yapılır; ayrıca ayrı "Kaydet" butonu zorunlu tutulmaz. İşlem sonucunda ekranda kısa ve anlaşılır güncelleme bildirimi gösterilir. **Ayrıntılı teknik sözleşme:** [PANEL_OTOMATIK_KAYIT_SOZLESMESI.md](../../PANEL_OTOMATIK_KAYIT_SOZLESMESI.md) (proje kök dizini).

Admin Panel (AdminLTE v3) Zorunlu Standardı
- Tüm admin panel sayfaları AdminLTE v3 bileşen diliyle geliştirilmelidir.
- Header, sidebar, kart, tablo, form, badge, buton ve icon yapıları AdminLTE sınıfları üzerinden kullanılmalıdır.
- Dashboard ve yönetim sayfaları aşağıdaki AdminLTE bilgi mimarisine göre ilerletilecektir: Dashboard, Widgets, Layout Options, Charts, UI Elements, Forms, Tables, Calendar, Gallery, Kanban, Mailbox, Pages, Extras, Documentation.
- Sidebar’da platform logosu tek başına görünür olmalıdır; logo yanında ekstra marka metni kullanılmaz.
- Admin panelinde harici CDN bağımlılığı kullanılmaz; tüm tema dosyaları proje içindeki `wwwroot/vendor/AdminLTE-3.2.0` dizininden yüklenir.
- Yeni sayfa/özellik geliştirirken AdminLTE başlık hiyerarşisi, spacing, font boyutu ve responsive davranışları korunur.

Bileşenler ve CSS
- Tek sorumluluk: bileşenler küçük ve yeniden kullanılabilir olsun.
- Tasarım token'ları: renk, radius, spacing gibi sabitler `:root` değişkenlerinde tutulsun.
- CSS yorumları kısa ve bölüm başlıkları şeklinde olsun (// veya /* Section */). Büyük açıklamalar kodun içine yazılmasın.

İnceleme & Rollere Göre Onay
- Değişiklikler küçük PR'larda sunulsun.
- PR incelemesi için en az 2 onay: frontend (UI/uygulama), UX veya erişilebilirlik reviewer.
- Kritik/özgün tasarım değişimlerinde Product Owner, UX uzmanı ve 1 backend reviewer dahil edilmelidir.

Commit Mesajları
- Kısa özet (max 50 karakter) + boş satır + detay (opsiyonel) + issue id.

Test & Ölçüm
- Görsel değişiklikler için gözlem (A/B veya user feedback) önerilir.
- UI hatalarında responsive test (320–1440px) yapılmalı.

Alan Mantığı — İndirim ve Kampanya Ayrımı (Zorunlu)
- `kampanya` ve `indirim` aynı şey değildir; kodda ve UI metinlerinde birbirinin yerine kullanılmaz.
- `indirim`: Otelin fiyatına uygulanan doğrudan fiyat düşüşüdür (normal fiyat -> indirimli fiyat).
- `kampanya`: Platform seviyesinde koşullu teklif/ödül kurgusudur (ör. "3 rezervasyondan sonra 4. rezervasyona %40").
- Partner takvim/fiyat ekranında indirimli fiyat girilirken kullanıcı, `fiyat_indirimleri` tablosundan `indirim_adi` seçer; sistem kaydı `indirim_id` ile yapar.
- Fiyat kayıtlarında alan adları açık ve ayrık olmalıdır: `normal_fiyat`, `indirimli_fiyat`, `indirim_id`. Eski `kampanya_id` kullanımı yeni geliştirmede `indirim_id` olarak ele alınır.
- Oda kartları ve fiyatı gösteren tüm alanlarda "neden indirim var" bilgisi kullanıcıya gösterilir (seçilen `indirim_adi` üzerinden).
- Kampanyaya katılan oteller ayrı görünürlük/öne çıkarma alanında listelenir (ör. "Kahvaltı Hediyeli Oteller"). Bu alan indirim alanından bağımsız yönetilir.

Veri Kalitesi — Türkçe Karakter Standardı
- `fiyat_indirimleri` ve partner takvim/fiyat tablolarında Türkçe karakterler (`ç, ğ, ı, İ, ö, ş, ü`) bozulmadan saklanmalı ve gösterilmelidir.
- SQL tarafında metin kolonlarında Unicode (`NVARCHAR` + `N'...'`) standardı korunur.
- "Test/Deneme" gibi geçici metinler canlı iş akışını bozmayacak şekilde normalize edilir veya anlamlı içerikle güncellenir.

SEO ve XML Üretim Kuralları
- Geliştirme aşamasında robots dosyası, arama motoru taramasını engelleyecek şekilde yönetilir.
- `sitemap.xml` ve il/ilçe bazlı otel XML dosyaları sistem tarafından otomatik üretilir.
- İl/ilçe XML çıktıları `Views/xml` altında tutulur; mahalle seviyesi bu fazda kapsam dışıdır.
- Sitemap yenileme kontrol periyodu 3 gündür; güncel içerik otomatik yeniden üretilir.

Yayın Güvenliği
- Kullanıcı açık onay vermeden canlıya yükleme/deploy yapılmaz.

Uygulama Örneği — Otel Kartı (özet)
- thumbnail | badge (küçük) | favori ikonu
- başlık (1 satır), konum (metin)
- etiketler (küçük pill'ler)
- özellikler (ikon+etiket satırı)
- rating | fiyat | CTA

Bu sözleşmeye uymayan büyük değişiklikler yapılmasın; değişiklik teklif edilecekse önce PR açıklamasında kullanıcı senaryosu ve hangi rollerin inceleyeceği belirtilsin.
