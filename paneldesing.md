# Panel Tasarım Notları

## Listeleme Tabloları Durum Renkleri

- Tüm panel listeleme tablolarında durum etiketleri yazılı ve anlaşılır olmalı; yalnız ikon kullanılmamalı.
- Rezervasyon durumlarında kullanıcıya gösterilen standart sözlük üçlü olmalı: `Tamamlandı`, `Bekliyor`, `Reddedildi`.
- DB veya operasyon içindeki `Onaylandı`, `Giriş Yaptı`, `Tamamlandı` değerleri liste ve kartlarda `Tamamlandı` olarak gösterilmeli.
- DB veya operasyon içindeki `İptal Edildi`, `Reddedildi`, otel onay durumu `Reddedildi` değerleri liste ve kartlarda `Reddedildi` olarak gösterilmeli. İptal/ret nedeni varsa detay popup içinde açıkça gösterilmeli.
- DB veya operasyon içindeki `Onay Bekliyor`, `Değişiklik Bekliyor`, otel onay durumu `Beklemede` değerleri liste ve kartlarda `Bekliyor` olarak gösterilmeli.
- Durum renkleri sabit kullanılmalı:
  - Tamamlandı: yeşil geçişli arka plan.
  - Reddedildi: kırmızı geçişli arka plan.
  - Bekliyor: sarı geçişli arka plan.
- Durum hücresinde farklı anlamdaki bilgi gösterilmemeli. Örneğin ödeme durumu, rezervasyon durumunun altında değil ayrı ödeme alanında yer almalı.
- Liste satırlarında durum rengine uygun hafif geçişli satır arka planı kullanılabilir; metin okunabilirliği korunmalı.

## Panel Tabler Standardı

- Partner, kullanıcı, firma, satış ve admin panelleri kademeli olarak Tabler şablon yapısına taşınacak.
- Panel layout yapısı `page`, `navbar-vertical`, `page-wrapper`, `navbar`, `page-body`, `footer` sırasını izlemeli.
- Her panelin shell dosyaları panel klasörüne göre ayrılmalı: örnek `assets/css/paneller/user/shell.css`.
- Sayfa özel CSS dosyaları sayfa adıyla tutulmalı: örnek `assets/css/paneller/user/dashboard.css`.
- Panel sayfası geliştirildiğinde route, view, code-behind/service metodu ve CSS dosyası aynı sayfa adı standardıyla ilerlemeli. Örnek: `/panel/user/favorilerim` için `Favorites.cshtml`, ilgili `Favorites` action/metodu ve `assets/css/paneller/user/favorites.css`; `/panel/user/rezervasyonlarim` için `Reservations.cshtml`, `Reservations` action/metodu ve `reservations.css`.
- Yeni sayfa özel CSS gerekiyorsa aynı ada sahip mobil dosya da hazırlanmalı: `dashboard.css` + `dashboard.mobile.css`, `favorites.css` + `favorites.mobile.css`.
- Yeni panel geliştirmelerinde içerik, buton, kart, tablo, filtre ve listeleme davranışları partner paneldeki Tabler yaklaşımıyla uyumlu olmalı.
- İkon tek başına aksiyon anlatmak için kullanılmamalı; kritik işlemlerde yazılı buton kullanılmalı.
- MCP/proje haritası mevcutsa önce dar kapsamlı MCP araçlarıyla ilgili dosya, metot veya satır okunmalı; MCP yoksa dosya sistemi aramaları yalnız ilgili panel alanıyla sınırlandırılmalı.
