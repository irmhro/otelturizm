# Platform Checkup ve Eksiksiz Gelişim Raporu

Son güncelleme: 03.05.2026

## Çalışma Standardı

- MCP varsa `project_map`, `find_files`, `search_code`, `read_range`, `read_method` ile dar bağlam okunur.
- MCP yoksa sadece ilgili controller, service, view, css ve migration dosyaları hedefli okunur.
- Her sayfa kendi route/action, `.cs`, `.cshtml`, `.css`, `.mobile.css` adıyla geliştirilir.
- Canlı veritabanında yıkıcı işlem yoktur; tablo değişiklikleri veri koruyan `ALTER` yaklaşımıyla yapılır.
- Canlı e-posta için `appsettings.Production.json` bağlantı override engeli kaldırılmış olmalı, SMTP hesapları `email_services` üzerinden aktif kalmalıdır.

## Admin Panel Eksikleri

| Alan | Durum | Tamamlama |
| --- | --- | --- |
| Dashboard | Başladı | Tabler KPI, operasyon ve ticari özet tamamlandı. |
| Platform Checkup | Başladı | Canlı şema ve servis kontrollerini gösteren sayfa eklendi. |
| Yetki ve Rol Yönetimi | Eksik | Kullanıcı, yönetici, rol, yetki matrisi, oturum ve admin işlem logları tek yönetim ekranlarına ayrılacak. |
| Partner Başvuruları | Başladı | Evrak, e-posta login onayı ve karar süreci Tabler yapısına alındı; evrak detayları geliştirilecek. |
| Firma Başvuruları | Kısmi | Firma onayı, evrak, kurumsal yetkili, limit ve çalışan ilk kurulum akışı geliştirilecek. |
| Satış Ekibi Ciro | Eksik | Satışçı bazlı rezervasyon, ciro, komisyon ve müşteri havuzu raporu eklenecek. |
| Rezervasyon Tek Liste | Başladı | Kaynak, durum, ciro ve komisyon alanları eklendi; detay popup ve aksiyon standardı genişletilecek. |
| Komisyonlar | Başladı | Otel bazlı komisyon, KDV ve stopaj kuralı ekranı eklendi; mutabakat fazı eklenecek. |
| Otel/Oda/Fiyat/Görsel | Kısmi | Otel detay, oda tipleri, oda özellikleri, fiyat takvimi ve WEBP/token görsel yönetimi admin standardına alınacak. |
| E-posta ve Kuyruk | Kısmi | Mail merkezi var; canlı retry, servis sağlık takibi ve şablon testi tek akışta güçlendirilecek. |
| Loglama ve Sistem Sağlığı | Kısmi | Hata, audit, API log, rate limit ve backup izleme sayfaları Tabler standardına taşınacak. |
| İl/İlçe/Mahalle | Eksik | Konum yönetimi, mahalle kontrolü, geo log ve otel koordinat onay akışı eklenecek. |

## Partner Panel Eksikleri

- Dashboard, rezervasyonlar, takvim-fiyat, odalar, görseller, tesis bilgisi, finans ve yorumlar ana işlev olarak mevcut.
- Mutabakat, komisyon hesap detayı, evrak durum merkezi, tesis çalışan yetkileri, e-posta bildirim ayarları ve destek süreçleri tekil sayfa standardıyla tamamlanacak.
- Partner rezervasyon durum standardı tüm tablolarda aynı kalacak: bekliyor sarı, tamamlandı/onaylandı yeşil, reddedildi/iptal kırmızı.

## Firma Panel Eksikleri

- Dashboard, rezervasyon, çalışan, limit, fatura ve harcama raporları mevcut.
- Firma başvuru/onay sonrası ilk kurulum, çalışan yetkisi, rezervasyon onay hiyerarşisi, bütçe/limit matrisi, firma ciro raporu ve mesajlaşma sayfaları Tabler düzeyine taşınacak.

## Kullanıcı Panel Eksikleri

- Dashboard, rezervasyonlarım, favorilerim, yorumlarım, puanlarım, mesajlarım, profil, ödeme, bildirim, güvenlik sayfaları ayrı adlandırma standardına alındı.
- Kalan iş: tüm formlarda mobil kalite kontrol, yorum düzenleme/silme 7 gün kuralı, profil görsel geçmişi ve tokenlı görsel temizleme işlerinin ortak servis standardına bağlanması.

## Veritabanı Checkup Başlıkları

- Yetki: `kullanicilar`, `yoneticiler`, `yetkiler`, `rol_yetkileri`, `admin_islem_loglari`
- Ticaret: `rezervasyonlar`, `odeme_islemleri`, `komisyon_muhasebe_kayitlari`, `firma_rezervasyonlari`, `satis_musterileri`
- Otel: `oteller`, `oda_tipleri`, `oda_ozellikleri`, `oda_fiyat_musaitlik`, `otel_fotograflari`, `oda_fotograflari`
- Başvuru: `partner_detaylari`, `partner_application_assets`, `partner_basvuru_hareketleri`, `firmalar`, `firma_calisanlari`
- E-posta/log: `email_services`, `bildirim_loglari`, `bildirim_sablonlari`, `sistem_hata_loglari`, `api_loglari`
- Konum: `iller`, `ilceler`, `mahalleler`, `geo_search_logs`, `otel_koordinat_degisiklikleri`

## Otomatik Tamamlama Sırası

1. Admin platform checkup ekranını canlı veri ile çalışır hale getir.
2. Admin yetki/rol, satış ciro, kullanıcı rezervasyon adedi ve log yönetimi sayfalarını ekle.
3. Admin otel/oda/fiyat/görsel yönetimini partner standardıyla eşitle.
4. E-posta servisleri, kuyruk, şablon, canlı SMTP ve health ekranlarını tek akışta güçlendir.
5. Firma panel başvuru, onay, limit, çalışan ve ciro sayfalarını tamamla.
6. Partner panel mutabakat, evrak, çalışan yetkisi ve bildirim ayarlarını tamamla.
7. Mobil ve responsive kalite kontrolünü sayfa sayfa tamamla.
