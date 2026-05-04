# Admin/Firma/Partner/Satış Eksik Gelişim Planı

Son güncelleme: 04.05.2026

## Ana Kural

- Admin onayı almayan partner, firma ve otel yayına veya operasyona alınmaz.
- Oteller listeleme ekranlarında kart değil tablo mantığı kullanılacak.
- Tüm kritik süreçlerde aksiyon butonları yazılı ve anlaşılır olacak: `İncele`, `Onayla`, `Reddet`, `Askıya Al`, `Yayına Aç`, `Komisyon`, `Fatura`.
- Evrak, komisyon, fatura, rezervasyon ve onay hareketleri loglanacak.
- Her sayfa kendi route/action, `.cs`, `.cshtml`, `.css`, `.mobile.css` adıyla geliştirilecek.

## Admin Panel

| Alan | Durum | Gelişim |
| --- | --- | --- |
| Onay Merkezi | Başladı | Partner, firma, otel, fatura ve komisyon özetleri tek tablo akışında eklendi. |
| Partner Başvuruları | Kısmi | Evrak türleri, eksik evrak takibi, admin notu, giriş onayı ve otel yayın bağlantısı güçlendirilecek. |
| Firma Başvuruları | Kısmi | Firma evrakları, vergi/sicil/mersis kontrolleri ve onay geçmişi eklenecek. |
| Otel Yönetimi | Kısmi | Otel onay/yayın/askı butonları, belge şartları ve gelişmiş filtreli tablo yapısı tamamlanacak. |
| Komisyon Yönetimi | Başladı | Otel bazlı komisyon/vergi kuralı var; aylık ciro, tahakkuk, fatura ve mutabakat detayları genişletilecek. |
| Fatura Yönetimi | Eksik | Partnerin kullanıcı/firma konaklama faturası yüklemesi admin ve ilgili panelde izlenecek. |
| Yetki/Rol | Eksik | Departman, rol, kullanıcı, panel yetkisi ve işlem logları tek yönetim sayfasına alınacak. |
| Departman Panelleri | Planlandı | Kullanıcı, partner, firma, satış, muhasebe, destek panelleri için iskelet ve sidebar standardı kurulacak. |

## Partner Panel

- Firma rezervasyonları için oda tipi, oda adedi, konaklama tarihi ve opsiyonel personel bilgileri detayda gösterilecek.
- Konaklama tamamlandıktan sonra partner fatura yükleyecek.
- Kullanıcı rezervasyonu için kullanıcı panelinde, firma rezervasyonu için firma panelinde fatura görünecek.
- Partner evrak alanı; ruhsat, sürdürülebilirlik belgesi, turizm belge no, ticaret sicil no, vergi levhası ve sözleşme türleriyle genişletilecek.

## Firma Panel

- Firma, otel/oda/oda adedi/tarih seçerek rezervasyon oluşturabilecek.
- Personel atama opsiyonel olacak; seçilirse partner detay ekranında görünecek.
- Firma faturalarım alanında partnerin yüklediği konaklama faturaları listelenecek.
- Firma onayı olmadan firma kaynaklı operasyon açılmayacak.

## Satış Paneli

- Satış temsilcisi bazlı rezervasyon, ciro, komisyon ve müşteri havuzu raporu admin tarafından izlenecek.
- Satış paneli kendi departman iskeletinde ayrı yetkilendirilecek.

## Veritabanı Başlıkları

- Evrak: partner/firma/otel belge türleri, belge no, son geçerlilik tarihi, güvenli dosya id, onay durumu, admin notu.
- Fatura: rezervasyon id, firma id, kullanıcı id, partner id, güvenli dosya id, fatura türü, yükleyen rol, onay durumu.
- Onay logları: hedef tablo, hedef id, eski durum, yeni durum, admin id, not, tarih, IP.
- Departman kullanıcıları: rol, departman, panel erişimi, aktiflik, son giriş.

## Hesap Seed Notu

Departman kullanıcıları için istenen e-posta ve parola seti kalıcı hesap oluşturduğu için DB’ye uygulanmadan önce ayrıca son teyit alınacak. Parola düz metin olarak repoya yazılmayacak; uygulamadaki parola hash standardı kullanılacak.
