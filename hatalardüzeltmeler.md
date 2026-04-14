# Hata Düzeltmeleri

## 2026-04-11

### `/admin-giris` CSS yüklenme sorunu
- Sorun: `Views/Login/AdminLogin.cshtml` sayfası `ViewData["PageCss"] = "admin-login"` ile yalnızca `admin-login.css` dosyasını yüklüyordu.
- Kök neden: `admin-login.css` sadece admin ek stillerini içeriyordu; sayfanın kullandığı temel `.auth-shell`, `.auth-stage`, `.auth-form`, `.auth-sidebar` gibi ana stiller `user-login.css` dosyasındaydı.
- Sonuç: `/admin-giris` açıldığında tasarım bozuluyor, grid ve form bileşenleri ham HTML gibi görünüyordu.
- Nihai düzeltme: admin giriş ekranı referans şablona uygun tam sayfa bağımsız login ekranına çevrildi.
- Uygulama: `Views/Login/AdminLogin.cshtml` içinde `Layout = null` kullanıldı ve sayfa kendi `admin-login.css` dosyasını doğrudan yükleyecek şekilde düzenlendi.
- Ek iyileştirme: Admin giriş görünümündeki görünen metinler Türkçe karakterlerle düzeltildi ve logo alanı `~/uploads/logo/logo.png` kullanacak şekilde güncellendi.

### Tekrar yaşanmaması için kural
- Ortak bir görünüm altyapısını kullanan sayfalarda sadece “override” CSS dosyası bırakılmayacak.
- Sayfa, başka bir temel stile bağımlıysa:
  - ya temel CSS açıkça import edilecek,
  - ya da layout tarafında birlikte yüklenecek,
  - ya da bağımlı stil tek dosyada birleştirilecek.
- Yeni giriş/panel ekranı açarken önce kullanılan ana class aileleri (`auth-*`, `panel-*`, `dashboard-*`) kontrol edilmeden sayfa teslim edilmeyecek.
- Public site layout'u dışında tasarlanmış tam sayfa panel giriş ekranları `Layout = null` ile ayrı çalıştırılacak; header/footer ile zorla aynı kabuğa sokulmayacak.

### Partner panel bos durum ve runtime smoke test notu
- Sorun: Partner panelde veritabani kaydi olmayan otellerde liste ve grafik alanlari bos/kirgin hissediyordu; bu durum panelin eksik algilanmasina yol aciyordu.
- Kök neden: Dashboard, performans, finans, destek ve takvim ekranlarinda veri yoksa anlamli bos durum mesajlari yoktu; bazi ekranlar sadece temel iskelet gosteriyordu.
- Nihai düzeltme:
  - `Services/PartnerService.cs` icinde destek, finans, fiyat takvimi ve stok uyarisi sorgulari gercek tablolara baglandi.
  - `Views/Paneller/Partner/*` ekranlarinda bos durum kartlari eklendi.
  - Sidebar uzerine bekleyen rezervasyon, acik destek, stok uyarisi ve yanitsiz yorum badge'leri eklendi.
- Runtime dogrulama:
  - partner girisi calisti
  - `/panel/partner/dashboard` acildi
  - toplu fiyat guncelleme `oda_fiyat_musaitlik` tablosuna yazdi
  - destek talebi `partner_destek_talepleri` tablosuna yazdi
  - fotograf yukleme ve silme `otel_gorselleri` akisinda calisti

### Firma panel auth ve servis kaydi notu
- Sorun: `firma_admin`, `firma_manager` ve `firma_staff` rolleri icin public login sonrasi dogru panel yonlendirmesi yoktu.
- Kök neden:
  - `AuthService` firma rollerini ayri `account_type` olarak isaretlemiyordu.
  - `AuthController` icinde `firma` icin redirect yolu tanimli degildi.
  - `Program.cs` tarafinda `IFirmaService` dependency injection kaydi eksikti.
- Nihai düzeltme:
  - `Services/AuthService.cs` icinde `firma_*` rolleri `account_type = firma` olarak isaretlendi.
  - `Controllers/Login/AuthController.cs` icine `/panel/firma` yonlendirmesi eklendi.
  - `Program.cs` icinde `IFirmaService` -> `FirmaService` kaydi eklendi.
- Runtime dogrulama:
  - `/firma` acildi
  - `ahmet.yilmaz@abcteknoloji.com / 1585` ile login calisti
  - login sonrasi `/panel/firma` acildi
  - `firma-fiyatlari`, `rezervasyonlar`, `calisanlar`, `limitler-onaylar`, `faturalar`, `harcama-raporlari`, `otel-bazli-rapor` ekranlari `200` dondu

### Firma panel CRUD ve demo veri tamamlama notu
- Sorun: Firma paneli ilk kurulumda sadece okunur listeler veriyordu; rezervasyonlar ve faturalar tablolari firma ozelinde bos oldugu icin referans ekran hissi eksik kalıyordu.
- Kök neden:
  - `firma_harcama_limitleri`, `firma_ozel_fiyatlar`, `rezervasyonlar` ve `faturalar` uzerinde firma paneli icin yeterli demo veri yoktu.
  - `Çalışan Ekle`, `Limit Kaydet`, `Onayla/Reddet` aksiyonlarinin form ve backend baglantisi bulunmuyordu.
- Nihai düzeltme:
  - `106_seed_firma_demo_reservations_and_invoices.sql` ile firma rezervasyon ve fatura seed verileri eklendi.
  - `Services/FirmaService.cs` icine calisan olusturma, limit upsert ve rezervasyon onay servisleri eklendi.
  - `Controllers/Paneller/Firma/FirmaPanelController.cs` icine post action akislari baglandi.
  - `Views/Paneller/Firma/Employees.cshtml`, `Limits.cshtml`, `Deals.cshtml`, `Reservations.cshtml` ekranlari form, filtre ve aksiyon butonlariyla zenginlestirildi.
- Runtime dogrulama:
  - yeni firma kullanicisi olusturuldu
  - limit kaydi `firma_harcama_limitleri` tablosuna yazildi
  - bekleyen rezervasyon firma tarafindan onaylandi
  - `faturalar`, `harcama-raporlari` ve `otel-bazli-rapor` ekranlari veri dolu state ile acildi

## 2026-04-11 - Satış Paneli
- `ISalesService` DI kaydı açılmış ama `SalesService.cs` eksik kaldığı için build kırılıyordu; servis, controller, panel shell ve sayfalar tamamlandı.
- Satış paneli için yeni DB alanları `satis_musterileri`, `satis_musteri_notlari`, `rezervasyonlar.satis_*`, `oteller.satis_*`, `users.sales_*` üzerinden bağlandı.
- Satış kullanıcıları normal kullanıcı girişinden giriş yapınca `/panel/satis` alanına yönlenir.
- Satış paneli rezervasyon oluşturduğunda müşteri ve partner için `bildirim_loglari` içine e-posta kuyruğu kaydı düşer.

## 2026-04-14 - Partner panel CRUD ve yönlendirme doğrulaması
- Sorun: Partner panelde bazı ekranlar sadece okunur seviyedeydi; `Oda Yönetimi`, `Fotoğraflar` ve `Performans` ekranlarında referans şablondaki düzenle/ekle/sil akışları tam karşılanmıyordu.
- Kök neden:
  - `Services/PartnerService.cs` içinde yardımcı metodlar eksik kaldığı için yeni CRUD akışları tamamlanmamıştı.
  - Sayfa bazlı partner CSS dosyaları boş placeholder durumundaydı.
  - Cookie auth çıkış sonrası korumalı partner sayfalarını genel kullanıcı girişine yönlendiriyordu.
- Nihai düzeltme:
  - `LoadRoomInventoryRowsAsync`, `LoadRoomFormAsync`, `LoadPhotoEditFormAsync`, `LoadCompetitorsAsync`, `BindCompetitorCommand`, `EscapeCsv`, `BuildRoomCode` yardımcı metodları tamamlandı.
  - `Views/Paneller/Partner/Rooms.cshtml`, `Photos.cshtml`, `Performance.cshtml` referans kullanım akışına yakın şekilde yeniden kuruldu.
  - `wwwroot/assets/css/paneller/partner/rooms.css`, `photos.css`, `performance.css` gerçek sayfa stilleriyle dolduruldu.
  - `Program.cs` içine partner/admin bazlı login redirect kuralı eklendi.
  - `Controllers/Login/AuthController.cs` içinde çıkış sonrası hesap tipine göre doğru login sayfasına dönüş sağlandı.
- Doğrulama:
  - `216silvertuzla@gmail.com / 1585` ile partner girişi çalıştı.
  - Tüm partner sidebar sayfaları `200` döndü ve `216 SILVER SUITE` verisi geldi.
  - Oda ekleme, oda güncelleme, oda pasife alma, toplu fiyat güncelleme, otel bilgi kaydetme, fotoğraf yükleme/güncelleme/kapak yapma/silme, tercih kaydetme, destek talebi oluşturma ve rakip analizi kaydetme akışları HTTP testinde başarılı döndü.
- `performans/rapor-indir` CSV çıktısı üretildi.
- Çıkış sonrası partner korumalı sayfa tekrar açıldığında `/partner-giris` yönlendirmesi doğrulandı.

## 2026-04-14 - Panel cikis, CSRF ve oturum guvenligi sertlestirmesi
- Sorun: Panel alanlarinda cikis linkleri daginikti; bazi ekranlarda mobil cikis yoktu, bazilarinda ise cikis `GET` link mantigiyla calisiyordu.
- Kök neden:
  - logout linkleri farkli panel parcalarinda farkli sekilde tanimlanmisti
  - `GET /cikis-yap` mantigi CSRF acisindan zayifti
  - yuksek trafik icin oturum istatistigi yazim araligi fazla sikti
- Nihai duzeltme:
  - tum panel sidebar ve mobil nav dosyalarinda logout alanlari CSRF korumali `POST /cikis-yap` formuna cevrildi
  - `Controllers/Login/AuthController.cs` icinde logout `POST` akisina alindi, `GET /cikis-yap` pasif yonlendirme davranisina cekildi
  - `Program.cs` icinde global `AutoValidateAntiforgeryTokenAttribute`, secure auth cookie, antiforgery cookie ve guvenlik header'lari korundu
  - `Services/SessionSecurityService.cs` icinde oturum istatistigi yazimi throttle edilerek yuksek trafikte gereksiz DB baskisi azaltildi
- Etki:
  - admin, partner, firma, satis ve user panellerinde gorunur cikis butonu standardize edildi
  - panel oturumlari daha guvenli ve olceklenebilir hale geldi
  - `107_alter_kullanici_oturum_istatistikleri_add_sales_type.sql` ile satis paneli oturum tipi de tablo semasina eklendi

## 2026-04-14 - Partner fotograf yukleme 400 hatasi ve ortak WebP servisi
- Sorun: `/panel/partner/fotograflar/yukle` adresi dogrudan acildiginda `HTTP 400` donuyordu ve yuklenen gorseller ham formatta saklaniyordu.
- Kök neden:
  - `fotograflar/yukle` yalnizca `POST` upload endpoint'i idi; kullanici bunu sayfa gibi acinca antiforgery akisi devreye girip 400 veriyordu.
  - `Services/PartnerService.cs` dosya yuklerken dosyayi oldugu gibi diske kopyaliyordu; ortak medya optimizasyonu yoktu.
- Nihai duzeltme:
  - `Controllers/Paneller/Partner/PartnerPanelController.cs` icine `GET /panel/partner/fotograflar/yukle` yonlendirmesi eklendi.
  - `Services/Abstractions/IImageStorageService.cs` ve `Services/ImageStorageService.cs` ile ortak gorsel kaydetme servisi eklendi.
  - Tum yuklenen gorseller kalite korunarak `webp` formatina donusturulmeye baslandi.
  - Partner fotograf yukleme akisi bu ortak servise baglandi.
- Canli dogrulama:
  - partner hesabiyla `/panel/partner/fotograflar/yukle?otelId=29` istegi artik `fotograflar` sayfasina duzgun yonlendi
  - test gorseli yuklendi
  - veritabani kaydi `.webp` uzantisi ile olustu
  - fiziksel dosya `wwwroot/uploads/hotels/partner/29/...webp` olarak dogrulandi

## 2026-04-14 - Partner Takvim ve Fiyatlar aylik takvim refaktoru
- Sorun: `Takvim & Fiyatlar` ekrani gercek partner operasyonunu karsilamiyordu; sayfa sadece 14 gunluk basit kartlar ve tek oda/tek fiyat mantigi ile calisiyordu.
- Kök neden:
  - `Views/Paneller/Partner/Pricing.cshtml` referans panel yerine placeholder seviyesinde kalmisti.
  - `Services/PartnerService.cs` icindeki `GetPricingAsync` ve `ApplyBulkPricingAsync` gercek takvim ayi, secili oda, kampanya etiketi, fiyat notu ve satisa kapama akisini desteklemiyordu.
  - `oda_fiyat_musaitlik` tablosunda partner takvimi icin gerekli `kampanya_etiketi`, `fiyat_notu`, `guncelleyen_kullanici_id` alanlari yoktu.
- Nihai duzeltme:
  - Ekran aylik takvim gridine cevrildi.
  - Oda bazli goruntuleme ve ayni formdan coklu oda secerek toplu guncelleme akisi kuruldu.
  - Normal fiyat, kampanyali fiyat, stok, min/max geceleme, satisa kapama/acma, kampanya etiketi ve fiyat notu ayni update akisina baglandi.
  - `113_alter_oda_fiyat_musaitlik_add_partner_calendar_columns.sql` ile gerekli tablo gelistirmeleri eklendi ve local veritabanina uygulandi.
- Canli dogrulama:
  - partner hesabi ile `/panel/partner/takvim-fiyatlar` acildi
  - aylik takvim gridi render edildi
  - `otelId=29`, `roomId=25`, `2026-04-15..2026-04-17` araligina fiyat guncellemesi gonderildi
  - `oda_fiyat_musaitlik` icinde `gecelik_fiyat`, `indirimli_fiyat`, `toplam_oda_sayisi`, `minimum_geceleme`, `maksimum_geceleme`, `kapali_satis`, `kampanya_etiketi`, `fiyat_notu`, `guncelleyen_kullanici_id` alanlari dogrulandi

## 2026-04-14 - Panellerde gorunmeyen logout butonu
- Sorun: Admin panel basta olmak uzere kullanici, firma ve partner panellerinde gorunur cikis aksiyonu pratikte bulunamiyordu; kullanici oturumu aciyor fakat rahatca cikis yapamiyordu.
- Kök neden:
  - Guvenli logout `POST /cikis-yap` altyapisi vardi ancak panel shell gorunumlerinde yeterince gorunur degildi.
  - Ozellikle admin tarafinda cikis aksiyonu sadece sidebar/mobil yapisina bagli kaldigi icin kullaniciya yokmus gibi gorunuyordu.
- Nihai duzeltme:
  - `Views/Paneller/Admin/_AdminPanelLayout.cshtml`
  - `Views/Paneller/Partner/_PartnerPanelLayout.cshtml`
  - `Views/Paneller/Firma/_FirmaPanelLayout.cshtml`
  - `Views/Paneller/Satis/_SalesPanelLayout.cshtml`
  - `Views/Paneller/User/_UserPanelLayout.cshtml`
  dosyalarina header seviyesinde gorunur `Cikis Yap` formu eklendi.
  - Tum ilgili panel shell CSS dosyalarinda `panel-header-logout-btn` stilleri eklenerek cikis butonu belirgin ve mobil uyumlu hale getirildi.
- Canli dogrulama:
  - admin `root@otelturizm.com / 1585`
  - partner `216silvertuzla@gmail.com / 1585`
  - firma `ahmet.yilmaz@abcteknoloji.com / 1585`
  - kullanici `sales.test.175455@otelturizm.com / 1585`
  hesaplariyla giris yapildi
  - tum panellerde header logout formu gorundu
  - logout sonrasi korumali sayfalara tekrar gidildiginde dogru login ekranlarina yonlendirme dogrulandi

## 2026-04-14 - Panel sidebar marka alanlarinda yazi logo kullanimi
- Sorun: Panel sidebar'larinda marka alani panelden panele farkliydi; bazilarinda yazi tabanli logo, bazilarinda ikon ve metin, bazilarinda ise farkli olceklerde marka kullanimi vardi.
- Nihai duzeltme:
  - Admin, partner, firma, satis ve kullanici sidebar marka alanlari ortak mantiga cekildi.
  - Yazi tabanli marka alanlari kaldirildi.
  - Tum panellerde gercek logo gorseli `/uploads/logo/logo.png` kullanildi.
  - Sidebar stilleri daha minimalist hale getirildi; marka kutusu, profil kutusu ve menu araliklari sadeleştirildi.
- Teknik etki:
  - panel marka dili tutarli hale geldi
  - sidebar'lar daha temiz ve profesyonel gorunume kavustu
  - derleme sonrasi `0 hata`, `0 uyari` ile dogrulandi

## 2026-04-14 - Turkiye il ilce ve Istanbul mahalle seed genisletmesi
- Sorun: `iller`, `ilceler` ve `mahalleler` tabloları vardi ancak veri kismi durumdaydi; yalnizca Istanbul ve Kocaeli icin sinirli seed bulunuyordu.
- Kök neden:
  - Adres tablolarinin ilk migrationlari kurulmus olsa da ulke genelini ve Istanbul'un tam mahalle yapisini dolduran seed seti yoktu.
  - Formlar ileride kullanici / partner / firma akislarinda tam adres secimi icin yeterli veriye sahip degildi.
- Nihai duzeltme:
  - `114_alter_address_tables_add_api_columns.sql`
  - `115_seed_turkiye_iller_ve_ilceler.sql`
  - `116_seed_istanbul_ilce_mahalleleri.sql`
  migrationlari eklendi ve local veritabanina uygulandi.
  - `iller` tablosuna `nufus`
  - `ilceler` tablosuna `api_kodu`, `nufus`
  - `mahalleler` tablosuna `api_kodu`, `nufus`
  alanlari eklendi.
- Canli dogrulama:
  - `iller = 81`
  - `ilceler = 973`
  - `Istanbul ilce = 39`
  - `Istanbul mahalle = 962`
  - `Tuzla mahalle = 17`
  degerleri local DB uzerinde dogrulandi.

## 2026-04-14 - Panel guvenliginde query-string bagimliligini azaltma notu
- Tespit: Panel ekranlarinda bazi filtre ve baglamlar hala `otelId`, `roomId`, `month` gibi query-string parametreleriyle ilerliyor; bu yapi tek basina bir acik degil ancak yuksek guvenlik beklentisinde istemci tarafinda fazla baglam tasinmasina neden oluyor.
- Risk: Yetki kontrolleri sadece URL parametresi degil sunucu tarafindaki sahiplik sorgulari ile tekrar edilmezse ileride IDOR benzeri guvenlik zafiyetleri olusabilir.
- Alinan karar:
  - Kullaniciya gorunen temiz sayfa URL yapisi korunacak.
  - Veri degistiren ve kritik veri okuyan panel aksiyonlari kademeli olarak API/arka plan istegi modeline tasinacak.
  - Tanimli tum endpoint'lerde rol, sahiplik, CSRF, input validation ve audit mantigi standart olacak.
- Uygulama kurali:
  - Yeni panel gelistirmelerinde “once ekran, sonra guvenlik” yaklasimi kabul edilmeyecek.
  - Yetki dogrulama her zaman server-side yapilacak.

## 2026-04-14 - Partner panel toplu goruntu yukleme 400 ve dosya temizligi
- Sorun: Partner panelinde tekli fotograf yukleme calisiyor ancak coklu secimde bazi istemciler `HTTP 400` aliyordu; kullaniciya yukleme ilerlemesi gosterilmiyor ve silinen dosyanin diskten mutlaka temizlenmesi acik kurala bagli degildi.
- Kök neden:
  - Coklu multipart isteklerinde bazi dosyalar `image/*` yerine farkli content-type ile geliyordu; yalnizca MIME tipi bazli kontrol bu durumda gecersiz dosya gibi davranabiliyordu.
  - Upload action'inda yuksek dosya sayisi icin form/request limiti tanimli degildi.
  - Partner fotograf ekraninda AJAX JSON donusu, canli ilerleme cubugu ve kismi hata rollback temizligi yoktu.
- Nihai duzeltme:
  - `Services/ImageStorageService.cs` ortak medya servisi izinli uzanti + resim decode dogrulamasi ile genisletildi.
  - Coklu yukleme akisinda kayit sirasinda hata olursa diske yazilan kismi dosyalar rollback ile silinir hale getirildi.
  - Fotograf silme akisi ortak medya servisine tasinarak hem veritabani hem fiziksel dosya temizligi garanti altina alindi.
  - `Program.cs` icinde multipart/form ve Kestrel request limitleri yuksek hacimli yuklemeye gore tanimlandi.
  - `Views/Paneller/Partner/Photos.cshtml` icine dosya sayisi, toplam boyut, ilerleme cubugu ve kullanici dostu durum mesajlari eklendi.
- Canli dogrulama:
  - Partner oturumunda `8` dosyali toplu yukleme basarili oldu.
  - Ayni akis `50` dosyali stres testinde de basarili oldu.
  - Yuklenen dosyalar `.webp` olarak dizinde dogrulandi.
  - Silinen test fotografi hem `otel_gorselleri` tablosundan hem fiziksel klasorden kaldirildi.

## 2026-04-14 - Admin oteller ekranini merkezi yonetime cevirme
- Sorun: `/admin/oteller` ekrani sadece temel liste seviyesindeydi; otel kayitlari icin merkezi duzenleme akisi, oda baglantilari ve medya yonetimi eksikti.
- Kök neden:
  - `Views/Paneller/Admin/Hotels.cshtml` ve `HotelDetail.cshtml` placeholder/generic section mantiginda kalmisti.
  - Admin controller tarafinda otel, oda, otel gorseli ve oda gorseli icin ayri post akislarini yoneten bir yapi yoktu.
  - Admin tarafinda bu alan icin ayrik bir service ve view model katmani bulunmuyordu.
- Nihai duzeltme:
  - `Models/Paneller/Admin/AdminHotelManagementViewModels.cs` ile admin otel yonetimi modelleri eklendi.
  - `Services/Abstractions/IAdminHotelManagementService.cs` ve `Services/AdminHotelManagementService.cs` ile otel, oda, otel fotografi ve oda fotografi yonetim servisi kuruldu.
  - `Controllers/Paneller/Admin/AdminPanelController.cs` icine `/admin/oteller`, `/admin/oteller/duzenle/{id}` ve ilgili tum kaydet/yukle/sil/kapak yap post endpoint'leri eklendi.
  - `Views/Paneller/Admin/Hotels.cshtml` listesinde gorunur `Oteli Duzenle` aksiyonu eklendi.
  - `Views/Paneller/Admin/HotelDetail.cshtml` tek ekranda:
    - `oteller`
    - `oda_tipleri`
    - `otel_gorselleri`
    - `oda_gorselleri`
    yonetimini saglayacak sekilde yeniden kuruldu.
  - `wwwroot/assets/css/panel-admin-hotels.css` ile bu alan icin ayri stil dosyasi eklendi.
- Canli dogrulama:
  - `root@otelturizm.com / 1585` ile admin login calisti
  - `/admin/oteller` acildi
  - liste ekrani `Oteli Duzenle` aksiyonunu gosterdi
  - `/admin/oteller/duzenle/29` acildi
  - detay ekraninda otel fotografi, oda fotografi, oda kaydetme ve otel kaydetme aksiyonlari render edildi
  - zararsiz `otel-fotograf-kapak-yap` POST aksiyonu `302` redirect ile basarili dogrulandi

## 2026-04-14 - Kampanya semasi ve favori akisinda runtime tamamlama
- Sorun: `kampanyalar` ve `user_favori_oteller` icin hazirlanan migrationlar `ADD COLUMN IF NOT EXISTS` / `CREATE INDEX IF NOT EXISTS` kullandigi icin mevcut MySQL surumunde syntax hatasi veriyordu; favori butonlari kodda hazir olsa da DB alanlari eksik kalirsa runtime'da patlayacakti.
- Kök neden:
  - Local MySQL sunucusu `ALTER TABLE ... ADD COLUMN IF NOT EXISTS` ve `CREATE INDEX IF NOT EXISTS` kullanimini bu scriptlerde kabul etmedi.
  - Favori servisi `kaynak_url`, `cihaz_tipi`, `aktif_mi`, `son_islem_tarihi`, `kaldirilma_tarihi` alanlarini bekliyordu.
  - Kampanya tarafinda da landing/SEO/vitrin alanlari olmadan partner-katilim ve public kampanya vitrini eksik kalacakti.
- Nihai duzeltme:
  - `117_alter_kampanyalar_expand_marketing_columns.sql` ve `121_alter_user_favori_oteller_expand_tracking.sql` dosyalari `information_schema` kontrollu, tekrar calistirilabilir dinamik SQL mantigina cevrildi.
  - `118_create_table_kampanya_oteller.sql`, `119_seed_active_campaigns.sql`, `120_seed_kampanya_oteller_demo.sql` ile birlikte local veritabanina uygulandi.
  - Son durumda:
    - `kampanyalar = 22`
    - `kampanya_oteller = 26`
    - `user_favori_oteller` genisletilmis takip kolonlari ile dogrulandi.
- Canli dogrulama:
  - `sales.test.175455@otelturizm.com / 1585` kullanicisi ile HTTPS uzerinden login alindi.
  - `/oteller/216-silver-suite` detay ekranindan favori API akisi test edildi.
  - `POST /api/favoriler/toggle` sonucu kayit DB'ye yazildi ve `Favorilerim` ekraninda `216 SILVER SUITE` gorundu.
  - Ayni akisla favoriden cikarma sonrasi kayit `aktif_mi = 0` olacak sekilde guncellendi.
- Ek guvenlik notu:
  - Antiforgery `SecurePolicy = Always` oldugu icin bu akis HTTP debug adresinde bilerek token uretmiyor; bu nedenle login ve favori testleri HTTPS uzerinden yapildi.
## 2026-04-14 - Favori modalı ve kampanya CRUD

- Public favori butonları giriş yapmamış kullanıcıda sessiz hata vermemeli; API `401/403` dönse bile sayfa içinde login/register modalı açılmalı.
- Razor inline CSS içinde `@media` kullanırken `@@media` kaçışı uygulanmalı; aksi halde derleme `CS0103` ile kırılır.
- Header badge gibi kullanıcıya özel alanlarda Razor `out var` kapsamına güvenilmemeli; değişken önce tanımlanıp sonra `TryParse` ile doldurulmalı.
- Partner kampanya ayrıl akışında kayıt hard delete yapılmadı; durum `Pasif`e çekilerek audit izi korundu. Public listeleme zaten sadece `Aktif` ve tarih aralığı uygun kayıtları gösterir.

## 2026-04-14 - Firma landing ve onayli firma giris akisi
- Sorun: `/firma` sayfasindaki karsilastirma bloklari gercek otel adina dayaniyordu; bu alan mantik anlatimi yerine canli otel verisi gibi algilaniyordu. Ayrica firma hesaplari icin ayri basvuru/onay/giris yasam dongusu eksikti.
- Kök neden:
  - `Views/Firma/_FirmaContent.cshtml` icinde `FeaturedDeals` uzerinden gercek otel verisi kullaniliyordu.
  - Firma girisi genel kullanici girisiyle ayni akista eriyor, ayri `firma-giris / firma-kayit` deneyimi bulunmuyordu.
  - `firmalar` tablosunda onay yasam dongusunun detay alanlari ve giris izni kontrolu eksikti.
- Nihai duzeltme:
  - `Views/Firma/_FirmaHeader.cshtml` ve `_FirmaContent.cshtml` firma odakli ayri rota mantigina cekildi.
  - Hero ve `Ornek Senaryo` alanlari gercek otel adindan arindirilarak kurumsal avantaj mantigini anlatan sabit ornek yapisina cevrildi.
  - `Models/Register/FirmaRegistrationModel.cs`, `Views/Login/FirmaLogin.cshtml`, `Views/Register/_FirmaRegisterForm.cshtml`, `Controllers/Login/AuthController.cs`, `Controllers/Register/RegisterController.cs` ve `Services/AuthService.cs` ile ayri firma giris/kayit akisi kuruldu.
  - `122_alter_firmalar_add_application_workflow.sql` ile `firmalar` tablosuna basvuru, onay ve giris izni kolonlari eklendi.
  - Firma login artik `firmalar.onay_durumu = 'Onaylandı'` ve `giris_izni_aktif_mi = 1` olmadan basarili olmuyor.
- Canli/servis dogrulama:
  - `dotnet build` temiz gecti.
  - Servis seviyesinde olusturulan test firma basvurusu DB'ye `Beklemede` olarak yazildi.
  - Ayni hesapla `AuthenticateFirmaAsync` sonucu bilincli olarak `null` dondu; yani onaysiz firma girisi engellendi.
  - HTTP debug ortaminda antiforgery `SecurePolicy = Always` oldugu icin firma login sayfasi sadece HTTPS ortaminda normal render olur; bu davranis guvenlik geregi korunmustur.

## 2026-04-14 - CDN varliklarin locale alinmasi
- Sorun: Birden fazla public/panel sayfa `cdnjs`, `jsdelivr` ve `google fonts` uzerinden dis varlik yuku yapiyordu. Bu durum CSS sorunlarinda mudahaleyi zorlastiriyor, bagimlilik kiran/ag kesintisi yasatan bir risk olusturuyordu.
- Nihai duzeltme:
  - `Font Awesome`, `Flag Icon`, `Swiper` ve `Inter` font dosyalari `wwwroot/assets/vendor` altina indirildi.
  - View dosyalarindaki tum aktif CDN referanslari local vendor yollarina cekildi.
  - `fonts.googleapis.com` ve `fonts.gstatic.com` baglantilari kaldirildi; `inter-local.css` ile fontlar lokal servis ediliyor.
- Kural: Projede aktif ekranda kullanilan CSS/JS/font/icon kutuphaneleri mumkun oldugunca vendor/local tutulur; yeni bir CDN baglantisi eklenirse kalici local karsiligi ayni turda hazirlanir.

## 2026-04-14 - Firma basvuru sureci migration ve test verisi dogrulamasi
- `122_alter_firmalar_add_application_workflow.sql` local DB'ye calistirildi ve `schema_migrations` tablosuna checksum ile kaydedildi.
- Ek olarak `123_create_table_firma_basvuru_hareketleri.sql` ile firma basvuru audit tablosu acildi.
- `124_seed_firma_basvuru_sureci_test_verileri.sql` ile 4 farkli senaryo olusturuldu:
  - `Onaylandı`
  - `Beklemede`
  - `Reddedildi`
  - `Askıda`
- Servis seviyesi auth testi sonucu:
  - `onayli.firma.test@otelturizm.com / 1585` => giris basarili
  - `bekleyen.firma.test@otelturizm.com / 1585` => engellendi
  - `reddedilen.firma.test@otelturizm.com / 1585` => engellendi
  - `askida.firma.test@otelturizm.com / 1585` => engellendi
## 2026-04-14 - Sayfa ve build kurtarma
- Partner panel 500 hatasının kökü `PartnerService.GetManagedHotelsAsync` içindeki kolon map uyumsuzluğuydu; bool alan string gibi okunuyordu, düzeltildi.
- `HotelService` içinde yinelenen `ultra-luks` switch kolu build'i bozuyordu; kaldırıldı.
- Public footer ve detay sayfalarında kullanıcıyı boşa düşüren `href="#"` placeholder'lar gerçek route veya güvenli pasif buton yapısına çevrildi.
- Partner ve firma kayıt formları DB tabanlı `iller / ilceler / mahalleler` API akışına bağlandı.

## 2026-04-14 - User Panel ve Migration Kurtarma
- Kullanıcı panelinde dashboard ve rezervasyonlarım ekranları 500 veriyordu.
- Kök nedenler: UserPanelService içinde canlı şemada olmayan oteller.seo_slug, otel_gorselleri.dosya_yolu ve oteller.puan kolonlarının kullanılmasıydı.
- Çözüm: kullanıcı panel sorguları canlı veritabanı şemasına uyarlandı; slug üretimi sunucu tarafında otel_adi + otel_kodu üzerinden yeniden kuruldu; görsel alanı gorsel_url, puan alanı ortalama_puan olarak güncellendi.
- 125_alter_users_add_user_profile_security_columns.sql migrationı MariaDB uyumsuzluğu nedeniyle ADD COLUMN IF NOT EXISTS yerine INFORMATION_SCHEMA kontrollü idempotent yapıya çevrildi ve başarıyla uygulandı.
- Kullanıcı paneli ekranları Index, Reservations, Messages, Profile, Notifications, Security, PaymentMethods gerçek view model + DB bağlamına geçirildi.
- Doğrulama: partner panel ana sayfalarının tamamı 200; kullanıcı panel ana sayfalarının tamamı 200; profil, bildirim tercihleri ve ödeme yöntemi kayıt akışları DB yazımı ile test edildi.

## 2026-04-14 - Anasayfa arama ve /Oteller favori akışı düzeltildi
- Anasayfa hero araması varsayılan Istanbul değeri olmadan çalışacak şekilde güncellendi.
- Arama placeholder metni Otel adı, il, ilçe, mahalle veya bölge ile ara olarak değiştirildi.
- Arama alanı sonundaki dönen otel ikonu kaldırıldı.
- /api/oteller/arama-onerileri ve otel listeleme sorguları il, ilçe, mahalle ve otel adı için DB destekli hale getirildi.
- Türkçe karakter toleranslı arama normalizasyonu eklendi; Postane gibi mahalle aramaları öneri ve listeleme tarafında doğrulandı.
- /oteller sayfasındaki favori butonları kullanıcı oturumuyla tekrar test edildi; /api/favoriler/toggle başarılı çalıştı.
- Arama sonrası kategori hızlı linklerinde ?/& birleşim hatası giderildi.

## 2026-04-14 - Public header session görünürlüğü ve tam mahalle importu
- Anasayfa, kurumsal ve firma header alanlarında oturum açmış kullanıcı/partner/firma hesapları için panel ve çıkış aksiyonları görünür hale getirildi.
- Anasayfa üst header araması gerçek /oteller?q akışına bağlandı.
- ücretsiz API kaynağı ile tüm Türkiye mahalle verisi idempotent şekilde import edildi.
- ilceler tablosunda mahalle kaydı olmayan ilçe kalmadı.

