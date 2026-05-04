# Admin/Firma/Partner/Satış/Departman Panel Gelişim Planı

## Zorunlu Standart
- Her sayfa kendi route, `.cs`, `.cshtml`, `.css` ve gerekiyorsa `.mobile.css` dosya adıyla geliştirilir.
- Listeleme olan alanlar kart değil tablo mantığıyla, gelişmiş filtre ve yazılı aksiyon butonlarıyla tasarlanır.
- Durum renkleri platform genelinde standarttır: tamamlandı/onaylandı yeşil, bekliyor sarı, reddedildi/iptal kırmızı geçişli.
- Admin onayı olmayan partner, firma ve oteller platformda yayınlanamaz.

## Admin Panel
| Alan | Durum | Gelişim |
| --- | --- | --- |
| Onay Merkezi | Başladı | Partner, firma, otel, fatura ve komisyon özetleri tek tablo akışında eklendi. |
| Partner Başvuruları | Kısmi | Evrak türleri, eksik evrak takibi, admin notu, giriş onayı ve otel yayın bağlantısı güçlendirilecek. |
| Firma Başvuruları | Kısmi | Firma evrakları, vergi/sicil/MERSİS kontrolleri ve onay geçmişi eklenecek. |
| Otel Yönetimi | Kısmi | Otel onay/yayın/askı butonları, belge şartları ve gelişmiş filtreli tablo tamamlanacak. |
| Komisyon Yönetimi | Başladı | Otel bazlı komisyon/vergi kuralı, aylık ciro, tahakkuk, fatura ve mutabakat genişletilecek. |
| Fatura Yönetimi | Eksik | Partnerin kullanıcı/firma konaklama faturası yüklemesi admin ve ilgili panellerde izlenecek. |
| Yetki/Rol | Eksik | Departman, rol, kullanıcı, panel yetkisi ve işlem logları tek yönetim sayfasına alınacak. |
| Departman Panelleri | Başladı | Kullanıcı, partner, firma, satış, muhasebe ve destek departman panel iskeleti kuruldu. |

## Departman Panel Kullanıcıları
- E-posta standardı: `irmhro0+departmanadi@gmail.com`.
- Rol standardı: `departman_kullanici`, `departman_partner`, `departman_firma`, `departman_satis`, `departman_muhasebe`, `departman_destek`.
- Kullanıcı seed işlemi kalıcı erişim oluşturduğu için işlem anında ayrıca onaylanır; plaintext parola repoya yazılmaz.

## Sıradaki Kodlama Sırası
1. Admin `partner-basvurulari`, `firma-basvurulari`, `oteller`, `komisyonlar`, `faturalar`, `yetkiler` sayfaları tablo standardına yükseltilecek.
2. Firma `dashboard`, `firma-fiyatlari`, `yeni-rezervasyon`, `rezervasyonlar`, `faturalar`, `calisanlar` veri tabanı uyumuyla tamamlanacak.
3. Partner firma rezervasyon detayında oda adedi, personel ataması ve fatura yükleme süreci geliştirilecek.
4. Satış panelinde müşteri, rezervasyon, ciro ve temsilci raporları departman KPI'larıyla bağlanacak.
