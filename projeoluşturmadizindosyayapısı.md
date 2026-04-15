# Proje Olusturma Dizin Dosya Yapisi Sozlesmesi

Bu dokuman, `otelturizmnew` projesinde zorunlu klasorleme, tasarim referansi ve kodlama kurallarini tanimlar.

## 1) Ana Kural (Zorunlu)
- UI gelistirmelerinde tek referans kaynak: `proje verileri` klasoru.
- Sayfa kurgusu, komponent yapisi ve mobil davranis bu klasordeki orijinal tasarimlara gore uygulanir.
- Rastgele/sifirdan farkli UI dili kullanilmaz.

## 2) Proje Verileri Klasor Sozlesmesi
`proje verileri` alan bazli klasorlenir:
- `00-referans/anasayfa-header-footer`
- `01-public/anasayfa`
- `01-public/kurumsal`
- `01-public/blog`
- `02-auth/kullanici`
- `02-auth/partner`
- `03-panel/admin`
- `04-otel-arama-ve-detay`
- `05-rezervasyon-ve-odeme`
- `06-destek`
- `07-eposta-sablonlari`
- `99-sql`

## 3) Alan Bazli Dizinleme Kurali (Zorunlu)
- Proje `Views`, `Controllers` ve `Models` altinda ayni alan hiyerarsisini izlemek zorundadir.
- Bir alan hangi isimle `Views` altinda acildiysa, ayni alan `Controllers` ve `Models` altinda da karsiligini bulmalidir.
- Sayfa ararken ya da guncellerken ilgili alan yalnizca kendi klasorunde gorulmelidir; baska alan dosyalari ayni yerde karismamalidir.

### 3.1) Views Hiyerarsisi
- Tum sayfalar `Views` altinda alan bazli klasorlenir.
- Zorunlu ana yapilar:
  - `Views/Anasayfa/`
  - `Views/Kurumsal/`
  - `Views/Destek/`
  - `Views/Firma/`
  - `Views/Login/`
  - `Views/Register/`
  - `Views/Oteller/`
  - `Views/Paneller/Admin/`
  - `Views/Paneller/Firma/`
  - `Views/Paneller/Partner/`
  - `Views/Paneller/Satis/`
  - `Views/Paneller/User/`
- Ornek zorunlu gorunumler:
  - `Views/Anasayfa/Anasayfa.cshtml`
  - `Views/Anasayfa/_AnasayfaHeader.cshtml`
  - `Views/Anasayfa/_AnasayfaContent.cshtml`
  - `Views/Anasayfa/_AnasayfaFooter.cshtml`
  - `Views/Kurumsal/Kurumsal.cshtml`
  - `Views/Kurumsal/_KurumsalHeader.cshtml`
  - `Views/Kurumsal/_KurumsalContent.cshtml`
  - `Views/Kurumsal/_KurumsalFooter.cshtml`
  - `Views/Destek/YardimMerkezi.cshtml`
  - `Views/Destek/Sss.cshtml`
  - `Views/Login/UserLogin.cshtml`
  - `Views/Login/PartnerLogin.cshtml`
  - `Views/Login/AdminLogin.cshtml`
  - `Views/Register/_UserRegisterForm.cshtml`
  - `Views/Register/_PartnerRegisterForm.cshtml`
  - `Views/Oteller/OtelListeleme.cshtml`
  - `Views/Oteller/OtelDetay.cshtml`
  - `Views/Paneller/User/Index.cshtml`
  - `Views/Paneller/Partner/Index.cshtml`
  - `Views/Paneller/Admin/Dashboard.cshtml`
- Login, register, panel ve kurumsal ekranlar `Views/Home`, `Areas` veya daginik teknik klasorler altinda tutulmaz.
- Kurumsal sayfa da anasayfa gibi kendi header/footer parcali yapi mantigini kullanir; kurumsal header ve footer `Views/Kurumsal/` altinda ayri partial dosyalar olarak tutulur.
- Panel ortak parcalari kendi panel alaninda kalir:
  - `Views/Paneller/User/_UserPanelLayout.cshtml`
  - `Views/Paneller/User/_UserSidebar.cshtml`
  - `Views/Paneller/User/_UserMobileNav.cshtml`
  - `Views/Paneller/Admin/_AdminPanelLayout.cshtml`
  - `Views/Paneller/Admin/_AdminSidebar.cshtml`
  - `Views/Paneller/Admin/_AdminMobileNav.cshtml`

### 3.2) Controllers Hiyerarsisi
- Controller yapisi gorunum hiyerarsisini aynalar:
  - `Controllers/Anasayfa/HomeController.cs`
  - `Controllers/Kurumsal/KurumsalController.cs`
  - `Controllers/Destek/DestekController.cs`
  - `Controllers/Firma/FirmaController.cs`
  - `Controllers/Login/AuthController.cs`
  - `Controllers/Register/RegisterController.cs`
  - `Controllers/Oteller/OtellerController.cs`
  - `Controllers/Paneller/Admin/AdminPanelController.cs`
  - `Controllers/Paneller/Firma/FirmaPanelController.cs`
  - `Controllers/Paneller/Partner/PartnerPanelController.cs`
  - `Controllers/Paneller/Satis/SalesPanelController.cs`
  - `Controllers/Paneller/User/UserPanelController.cs`
- Yeni bir alan icin yeni controller acildiginda ilgili `Views` klasoru ayni alan mantigiyla acilmak zorundadir.

### 3.3) Models Hiyerarsisi
- Model yapisi da ayni alan mantigini izler:
  - `Models/Anasayfa/`
  - `Models/Giris/`
  - `Models/Kurumsal/`
  - `Models/Firma/`
  - `Models/Destek/`
  - `Models/Register/`
  - `Models/Oteller/`
  - `Models/Paneller/Admin/`
  - `Models/Paneller/Firma/`
  - `Models/Paneller/Partner/`
  - `Models/Paneller/Satis/`
  - `Models/Paneller/User/`
- Ornekler:
  - `Models/Anasayfa/AnasayfaViewModel.cs`
  - `Models/Giris/UserSessionModel.cs`
  - `Models/Destek/SupportViewModels.cs`
  - `Models/Register/UserRegistrationModel.cs`
  - `Models/Register/PartnerRegistrationModel.cs`
  - `Models/Oteller/HotelListingViewModels.cs`
  - `Models/Paneller/Admin/AdminPanelViewModels.cs`
- Kimlik, kayit, panel ve otel verileri tek klasorde karisik tutulmaz; her alan yalnizca kendi model dizininde yasar.

### 3.4) Dosya Guncelleme Mantigi
- `UserLogin` tasarimi guncellenecekse gelistirici `Views/Login/`, `Controllers/Login/` ve gerekiyorsa `Models/Giris` veya `Models/Register` alaninda calisir.
- `Kurumsal` sayfa guncellenecekse yalnizca `Views/Kurumsal/` ve ilgili controller/model alani kullanilir.
- `Destek` sayfalari guncellenecekse yalnizca `Views/Destek/`, `Controllers/Destek/` ve `Models/Destek/` alaninda calisilir.
- `Admin panel` guncellenecekse aranan dosyalar `Views/Paneller/Admin/`, `Controllers/Paneller/Admin/` ve `Models/Paneller/Admin/` altinda bulunur.
- `Partner panel` ve `User panel` de ayni mantikla yalnizca kendi alt alanlarinda gelistirilir.

## 4) CSS Sozlesmesi (Sayfa Bazli)
- Her sayfanin CSS dosyasi ayridir.
- Konum: `wwwroot/assets/css`
- Isim: sayfa dosya adina gore
  - `home-index.css`
  - `user-login.css`
  - `partner-login.css`
  - `otel-listeleme.css`
  - `otel-detay.css`
- Ortak stiller sadece `site-layout.css` dosyasinda tutulur.
- Panel ailelerinde ortak kabuk stili ayri tutulabilir:
  - `panel-user-shell.css`
- Alan bazli panel CSS klasorleme zorunludur:
  - `wwwroot/assets/css/paneller/admin/`
  - `wwwroot/assets/css/paneller/firma/`
  - `wwwroot/assets/css/paneller/partner/`
  - `wwwroot/assets/css/paneller/satis/`
  - `wwwroot/assets/css/paneller/user/`
- Panel sayfalarinin kendi CSS dosyalari ayri tutulur:
  - `panel-user-dashboard.css`
  - `panel-user-reservations.css`
  - `panel-user-favorites.css`
  - `panel-user-loyalty.css`
  - `panel-user-messages.css`
  - `panel-user-profile.css`
  - `panel-user-payment.css`
  - `panel-user-notifications.css`
  - `panel-user-security.css`

## 5) Header/Footer Sozlesmesi
- Header/Footer tasarimi `proje verileri/00-referans/anasayfa-header-footer` kaynaklarina gore gelistirilir.
- Mobil uyumluluk zorunlu (max-width 1024/900/768/560 kirilimlari).
- Tum public sayfalarda ortak sayfa genisligi kullanilir; header, footer ve icerik container olcusu tek merkezden tanimlanir.
- Farkli sayfalarda farkli `max-width` kullanilarak header/icerik hizasi bozulmaz.
- Ortak genislik standardi `site-layout.css` icindeki ortak container kurali uzerinden yonetilir; sayfa bazli CSS dosyalari kendi basina farkli ana genislik tanimlamaz.
- Public alanlarda text logo kullanilmaz; logo kullanimi yalnizca `wwwroot/uploads/logo/logo.png` gorseli uzerinden yapilir.
- Anasayfa, kurumsal, destek, login, register ve benzeri public ekranlarda logo alani yalnizca gorsel logo ile kurgulanir; `Otelturizm .com` gibi yazisal logo tekrar edilmez.

## 5.1) URL ve Sayfa Yolu Sozlesmesi
- Kullaniciya gorunen URL'lerde controller adi, teknik klasor adi veya `Auth/...` benzeri sistem yolu gosterilmez.
- Giris, kayit ve benzeri public erisim sayfalari domain uzerinde dogrudan sayfa adi slug'i ile acilir.
- Format: `domain/sayfa-adi`
- Ornek zorunlu yapilar:
  - `/kullanici-giris`
  - `/kullanici-kayit` (post endpoint)
  - `/partner-giris`
  - `/partner-kayit` (post endpoint)
  - `/admin-giris`
  - `/cikis-yap`
- Link, form post, cookie auth `LoginPath`, `AccessDeniedPath` ve redirect akislari ayni temiz URL yapisini kullanmak zorundadir.
- Eski teknik URL'ler gerekiyorsa sadece geriye donuk uyumluluk icin 301/302 yonlendirme veya alias olarak tutulur; ana kullanim yolu her zaman temiz slug olur.

## 6) Gelistirme Akisi
1. Once `proje verileri` icindeki ilgili tasarim dosyasi referans alinir.
2. Sonra MVC view + page css olusturulur.
3. Son adimda mobil kontrol yapilir.
4. Listeleme/detay gibi bagli ekranlarda route ve layout linkleri ayni turda baglanir.

## 6.1) Tasarim-Veritabani Uyum Zorunlulugu
- Tum tasarimlar gercek veritabani ile calisacak sekilde kurgulanir.
- Canliya cikacak hicbir ekran yalnizca statik/sablon veri ile birakilmaz.
- Referans tasarimda bulunan her alan, kart, rozet, filtre, istatistik, tablo sutunu veya aksiyon icin veritabani karsiligi kontrol edilir.
- Eger tasarimdaki bir ozellik icin veritabaninda ilgili tablo, kolon, iliski, enum veya baglanti yoksa eksik yapi eklenir.
- Eksik alanlar yalnizca view tarafinda gecici metinle gecistirilmez; migration, seed, servis ve veri baglantisi da kurulur.
- UI, backend ve veritabani birlikte ele alinir; tasarimda gorunen veri gercek kaynaktan beslenir.
- Gerekli oldugunda yeni migration dosyalari acilir, tablo/kolon iliskileri kurulur ve uygulama katmanina baglanir.
- Tasarimdaki her ozellik icin su zincir tamamlanmis olmalidir:
  1. Veritabani alani veya iliskisi
  2. Backend sorgusu veya servis baglantisi
  3. View model veya veri tasima yapisi
  4. Razor/View uzerinde gercek veri gosterimi
- Bu kural kullanici, partner, admin, otel listeleme, otel detay, rezervasyon, odeme ve kurumsal alanlarin tumu icin baglayicidir.
- Referans sayfada gozle gorulen bir kart, ikon, gorsel, rozet, favori kalbi, slider, galeri, ozellik listesi veya sayfa parcasi local projede birebir gorunmelidir.
- Referans tasarimda kullanilan gorseller lokal projede yoksa once gorseller indirilir, `wwwroot/uploads` altina yerlestirilir ve ilgili veritabani tablolarina (`oteller`, `otel_gorselleri`, gerekirse baska medya tablolari) baglanir.
- Referans kartlarda gorunen ikonlar yalnizca CSS ile taklit edilmez; mumkunse veritabani karsiligi (`otel_ozellikleri.ozellik_ikon` gibi) uzerinden okunur ve UI'ya baglanir.
- Referans tasarimda kullanici aksiyonu varsa (`favorilere ekle`, `begen`, `karsilastir`, `kaydet` gibi) bunun veritabani tablosu ve backend akisi da olusturulur.
- Referans destek sayfalarinda gorunen kategori, soru-cevap, yardim makalesi, destek kanali ve arama yapilari icin de veritabani tablolari ve admin tarafinda yonetilebilir veri karsiligi kurulur.
- Referans sayfada gorulen icerik local sitede de ayni veri mantigi ile gorulene kadar is tamamlanmis sayilmaz.
- Partner, firma, satis ve admin panellerindeki takvim, fiyat, stok ve musaitlik ekranlari yalnizca gercek veritabani tablolarina bagli calisir; statik gun kutulari, sabit fiyat kartlari veya sahte kampanya etiketleri kullanilmaz.
- `Takvim & Fiyatlar` benzeri ekranlarda her yeni UI ihtiyaci icin once veritabani semasi kontrol edilir; gerekiyorsa yeni kolon, index, foreign key ve migration ayni turda eklenir.
- Panel takvim ekranlarinda veri guncelleme akislari dogrudan ilgili tabloya yazmali ve sonrasinda ayni sayfada okunarak dogrulanabilir olmalidir.
- Fiyat, kampanya, satisa kapama, minimum-maksimum geceleme, stok ve not alanlari ayri ayri degil tek takvim kurgusu icinde tam uyumlu calismalidir.

## 6.2) Yetki ve Sahiplik Sozlesmesi
- Kimlik dogrulama yalnizca `users` tablosu uzerinden yapilir.
- `users.rol` uygulama seviyesi rol bilgisini tutar:
  - `user`
  - `admin`
  - `partner_owner`
  - `partner_manager`
  - `partner_staff`
- `users.sahiplik_partner_id` kullanicinin ana partner hesabini tutar.
- `oteller.user_id` ilgili otelin birincil sorumlu kullanicisini tutar.
- Bir otelin birden fazla partner kullanicisi tarafindan yonetilebilmesi icin `otel_kullanici_sahiplikleri` tablosu zorunlu kullanilir.
- Partner panelinde kullanicinin hangi otellere erisecegi `otel_kullanici_sahiplikleri` uzerinden belirlenir; yalnizca `partner_id` ile genis yetki verilmez.
- Oturum claim'lerinde su alanlar tutulur:
  1. `account_type`
  2. `user_role`
  3. `partner_id`
  4. `ownership_partner_id`
  5. `managed_hotel_id`
- Yeni partner/admin/yetki gelistirmelerinde yalnizca UI degisimi yeterli sayilmaz; ilgili veritabani rolu, sahiplik kaydi ve oturum claim baglantisi birlikte kurulmak zorundadir.

## 6.3) Panel Guvenligi ve Oturum Sozlesmesi
- Tum panel alanlari (`admin`, `partner`, `firma`, `satis`, `user`) gorunur ve erisilebilir bir `Cikis Yap` butonuna sahip olmak zorundadir.
- Panel cikis aksiyonu link ile degil, CSRF korumali `POST /cikis-yap` formu ile calistirilir.
- Panel icindeki tum veri degistiren istekler antiforgery token ile korunur; panelde token'siz form birakilmaz.
- Cookie auth ayarlari tum hesap tiplerinde su taban kurallariyla calisir:
  1. `HttpOnly`
  2. `Secure`
  3. `SameSite`
  4. `SlidingExpiration`
  5. hesap tipine gore dogru login sayfasina redirect
- `beni hatirla` tercihi claim ve oturum istatistikleri seviyesinde izlenir.
- Oturum, cihaz ve ziyaret istatistikleri `kullanici_oturum_istatistikleri` tablosunda tutulur.
- Oturum istatistikleri yuksek trafikte asiri DB yazimi olusturmayacak sekilde aralikli/throttled mantikla yazilir.
- Panel korumali bir sayfaya cikis sonrasi tekrar girildiginde kullanici kendi hesap tipine uygun login ekranina donmelidir:
  - partner -> `/partner-giris`
  - admin -> `/admin-giris`
  - firma/user/satis -> `/kullanici-giris`

## 6.4) Yuksek Olcek ve Performans Sozlesmesi
- Proje gelistirmeleri minimum su olcek varsayimiyla yapilir:
  - `100000+` kullanici
  - `40000+` otel
  - `2000+` firma
  - `3000+` partner
- Her sayfa sanki ayni anda binlerce kullanici tarafindan goruluyormus gibi tasarlanir; sorgu, filtre ve listeleme mantigi buna gore kurulur.
- API ve servis sorgulari gercek veritabani ile calisir; sahte liste veya tum tabloyu memory'ye cekme yaklasimi kullanilmaz.
- Liste ekranlarinda indeks dostu sorgular, filtre bazli where yapisi, kontrollu join kullanimi ve gerekiyorsa sayfalama zorunludur.
- Oturum, panel ve dashboard servislerinde gereksiz tekrar sorgular, her istekte agir tam tablo taramalari ve plansiz N+1 sorgular kabul edilmez.
- UI tarafinda buton, kart, tablo ve istatistik kutulari dogrudan gercek backend baglantisina sahip olmali; sadece gorunen ama calismayan aksiyon teslim edilmis sayilmaz.
- Yeni migration hazirlanirken tablo anahtarlari, unique kisitlar, foreign key'ler ve sorgu ihtiyacina uygun index'ler birlikte dusunulur.

## 6.5) Gorsel Yukleme ve Medya Sozlesmesi
- Tum gorsel yukleme akislarinda ortak medya servisi kullanilir; controller veya service icinde ham dosya dogrudan diske kopyalanmaz.
- Standart servis: `IImageStorageService`
- Tum yeni gorsel yuklemeleri kalite korunarak `webp` formatinda kaydedilir.
- Yukleme sirasinda asgari kurallar:
  1. gecerli gorsel MIME tipi ve/veya izinli gorsel uzantisi kontrol edilir; son karar her zaman sunucu tarafinda gercek resim decode testi ile verilir
  2. boyut limiti uygulanir
  3. EXIF yon bilgisi duzeltilir
  4. asiri buyuk gorseller kontrollu sekilde optimize edilir
  5. veritabani yolu her zaman donusturulen son dosyayi isaret eder
  6. coklu yuklemede dosya sayisi ve toplam istek boyutu limiti tanimlanir
  7. coklu yukleme sirasinda kismi hata olursa diske yazilan dosyalar rollback ile temizlenir
  8. silinen gorsel hem veritabanindan hem fiziksel dizinden kaldirilir; yetim dosya birakilmaz
- `POST` upload endpoint'leri dogrudan adres cubugundan acilacak sayfa gibi kullanilmaz; gerekiyorsa ayni slug ailesinde guvenli `GET` yonlendirme tanimlanir.
- Bir alandaki gorsel yukleme duzeltmesi diger alanlarda tekrar yazilmaz; ayni ortak servis uzerinden genisletilir.

## 6.6) API Tabanli Guvenli Istek Sozlesmesi
- Panel ve kimlikli alan gelistirmelerinde veri okuma/yazma mantigi query-string odakli sayfa URL'leri uzerinden buyutulmez; arka planda yetki kontrollu servis ve API mantigi esas alinir.
- Kullaniciya gorunen sayfa URL'si sade kalabilir; fakat fiyat, kampanya, takvim, oturum, profil, rezervasyon ve panel aksiyonlari mumkun olan her yerde arka plan istekleri ile yonetilir.
- `otelId`, `roomId`, `partnerId`, `firmaId`, `userId` gibi hassas baglam parametreleri guvenlik kritik aksiyonlarda tek basina istemci guveni ile kullanilmaz; sunucu tarafinda oturum claim, sahiplik tablolari ve yetki kontrolleri ile yeniden dogrulanir.
- Veri degistiren tum API veya form istekleri su taban kurallara uyar:
  1. kimlik dogrulama zorunlu
  2. rol ve sahiplik kontrolu zorunlu
  3. CSRF / antiforgery korumasi zorunlu
  4. input validation zorunlu
  5. sadece gerekli alanlarin kabul edilmesi zorunlu
  6. audit/log kaydi zorunlu oldugu yerde tutulur
- Hassas panel verileri istemci tarafina “her seyi URL ile getir” mantigi ile acilmaz; veriler server-side filtreli sorgular veya guvenli API endpoint'leri ile saglanir.
- Kullanici, partner, firma ve admin verileri icin disaridan tahmin edilebilir parametrelerle toplu veri cekilebilecek endpoint tasarlanmaz.
- Yuz binlerce kayit olceginde calisacak alanlarda API endpoint'leri su ilkelerle kurulur:
  1. sayfalama
  2. filtreleme
  3. indeks dostu sorgu
  4. limitli kolon secimi
  5. N+1 sorgudan kacinma
  6. rate-limit / throttle dusuncesi
- Panel takvim, fiyat ve kampanya ekranlari asamali olarak API tabanli guvenli aksiyon modeline tasinacaktir; istemcide gorunen tarih veya oda secimi tek basina yetki kaniti sayilmayacaktir.
- Siber guvenlik adimlari “sonra eklenir” mantigi ile ertelenmez; yeni ekran yazilirken ayni turda guvenlik katmani uygulanir.

## 7) Gelistirme Ortami Sozlesmesi
- Uygulama derleme ve calistirma ana ortami `Visual Studio` olacaktir.
- Kod degisikligi sonrasi ana dogrulama `Visual Studio Build/Rebuild` ile yapilir.
- `MSB3026`, `MSB3027`, `EXE kilitli` benzeri hatalarda once calisan `otelturizmnew.exe` surecleri durdurulur, sonra yeniden derleme yapilir.
- Yerel veritabani ortami `Laragon` uzerinden yonetilir.
- Veritabani olusturma, tablo kontrolu, veri inceleme ve manuel SQL yonetimi `HeidiSQL` ile yapilir.
- Yerel veritabani adi `otelturizmnew` olarak esas alinir.
- Migration, seed, tablo kontrolu ve veri aktarimlarinda Laragon MySQL + HeidiSQL birlikte referans gelistirme ortami kabul edilir.

## 8) Canli Yayin Hazirlik Notu
- Canli sunucu, FTP, yayinlama ve production veritabani gecisi kurallari proje sozlesmesinde not olarak tutulur.
- Bu asamada canliya yukleme yapilmamis kabul edilir.
- Canliya gecis basladiginda FTP, veritabani baglanti, migration uygulama sirasi ve yedekleme adimlari ayrica netlestirilir.
- Production islemine gecmeden once yerel ortamda son dogrulama `Visual Studio`, `Laragon` ve `HeidiSQL` uzerinden tamamlanir.
- Migration uygulama sirasi production icin su mantikla korunur:
  1. Once tam yedek alinir.
  2. Sonra sirali migrationlar uygulanir.
  3. Ardindan seed/veri aktarimi kontrol edilir.
  4. Son olarak uygulama yayinlama ve canli test tamamlanir.
- FTP ve canli veritabani bilgileri yalnizca yayinlama asamasinda aktif kullanilir.

## 9) Yasaklar
- Sayfa bazli CSS yerine tum stilleri tek dosyada toplamak yasak.
- Referans tasarimi yok sayip farkli header/footer cikarmak yasak.
- Login sayfalarini `Areas` altinda tutmak yasak.
- Anasayfa header/footer parcalarini `Views/Shared` altinda tutmak yasak.

Bu sozlesme tum yeni ekranlar icin baglayicidir.

## 3.5) Partner Panel Zorunlu Dosyalari
- Partner panel ortak kabugu zorunlu olarak su dosyalarda tutulur:
  - `Views/Paneller/Partner/_PartnerPanelLayout.cshtml`
  - `Views/Paneller/Partner/_PartnerSidebar.cshtml`
  - `Views/Paneller/Partner/_PartnerPanelFooter.cshtml`
  - `Views/Paneller/Partner/_PartnerMobileNav.cshtml`
- Partner panel ekranlari kendi alaninda ayri dosyalarda tutulur:
  - `Views/Paneller/Partner/Dashboard.cshtml`
  - `Views/Paneller/Partner/Reservations.cshtml`
  - `Views/Paneller/Partner/Pricing.cshtml`
  - `Views/Paneller/Partner/Rooms.cshtml`
  - `Views/Paneller/Partner/HotelInfo.cshtml`
  - `Views/Paneller/Partner/Photos.cshtml`
  - `Views/Paneller/Partner/Performance.cshtml`
  - `Views/Paneller/Partner/Reviews.cshtml`
  - `Views/Paneller/Partner/Finance.cshtml`
  - `Views/Paneller/Partner/Preferences.cshtml`
  - `Views/Paneller/Partner/Support.cshtml`
- Partner panelde `ekle`, `düzenle`, `sil`, `kapak yap`, `yanıtla`, `kaydet`, `talep oluştur`, `rapor indir` gibi tüm butonlar gerçek controller action'larına bağlı olmak zorundadır.
- Partner panelde yalnızca görünür buton bırakmak yasaktır; sayfada görünen her aksiyon ya veritabanına yazmalı ya da açıkça pasif/taslak olarak işaretlenmelidir.
- Partner panel sayfaları otel sahiplik yetkisini `otel_kullanici_sahiplikleri` üzerinden kontrol eder ve yalnızca giriş yapan partner kullanıcısının erişebildiği otel kayıtlarıyla çalışır.
- Partner panel özel CSS dosyaları zorunlu olarak `wwwroot/assets/css/paneller/partner/` altında tutulur ve placeholder bırakılmaz.
  - `Views/Paneller/Partner/Rooms.cshtml`
  - `Views/Paneller/Partner/HotelInfo.cshtml`
  - `Views/Paneller/Partner/Photos.cshtml`
  - `Views/Paneller/Partner/Performance.cshtml`
  - `Views/Paneller/Partner/Reviews.cshtml`
  - `Views/Paneller/Partner/Finance.cshtml`
  - `Views/Paneller/Partner/Preferences.cshtml`
  - `Views/Paneller/Partner/Support.cshtml`

## 3.6) Satis Paneli Zorunlu Dosyalari
- Satis panel ortak kabugu zorunlu olarak su dosyalarda tutulur:
  - `Views/Paneller/Satis/_SalesPanelLayout.cshtml`
  - `Views/Paneller/Satis/_SalesSidebar.cshtml`
  - `Views/Paneller/Satis/_SalesPanelFooter.cshtml`
  - `Views/Paneller/Satis/_SalesMobileNav.cshtml`
- Satis panel ekranlari kendi alaninda ayri dosyalarda tutulur:
  - `Views/Paneller/Satis/Dashboard.cshtml`
  - `Views/Paneller/Satis/CreateReservation.cshtml`
  - `Views/Paneller/Satis/Availability.cshtml`
  - `Views/Paneller/Satis/Reservations.cshtml`
  - `Views/Paneller/Satis/Customers.cshtml`
  - `Views/Paneller/Satis/Reports.cshtml`
  - `Views/Paneller/Satis/Hotels.cshtml`
- Satis panel mantigi:
  1. Cagri ile gelen talepler dogrudan rezervasyona donebilir.
  2. Yeni rezervasyon akisinda il, ilce, fiyat ve ozellik filtreleri gercek veritabani uzerinden calisir.
  3. Müşteri, rezervasyon ve otel rehberi alanlari sahte veriyle birakilmaz.
  4. Satis paneli uzerinden acilan rezervasyonlar `rezervasyonlar.satis_temsilcisi_id`, `rezervasyonlar.satis_musteri_id`, `rezervasyonlar.rezervasyon_kanali` alanlariyla takip edilir.
  5. Rezervasyon olustugunda musteri ve partner e-posta bilgilendirmesi queue/log kaydi veritabanina dusmelidir.

## 4.1) Partner Panel CSS Klasor Sozlesmesi
- Partner panel CSS dosyalari alan klasoru altinda tutulur:
  - `wwwroot/assets/css/paneller/partner/shell.css`
  - `wwwroot/assets/css/paneller/partner/dashboard.css`
  - `wwwroot/assets/css/paneller/partner/reservations.css`
  - `wwwroot/assets/css/paneller/partner/pricing.css`
  - `wwwroot/assets/css/paneller/partner/rooms.css`
  - `wwwroot/assets/css/paneller/partner/hotel-info.css`
  - `wwwroot/assets/css/paneller/partner/photos.css`
  - `wwwroot/assets/css/paneller/partner/performance.css`
  - `wwwroot/assets/css/paneller/partner/reviews.css`
  - `wwwroot/assets/css/paneller/partner/finance.css`
  - `wwwroot/assets/css/paneller/partner/preferences.css`
  - `wwwroot/assets/css/paneller/partner/support.css`

## 6.3) Olcek ve Oturum Sozlesmesi
- Partner panel sorgulari 20.000 partner ve 1.000.000 kullanici olcegine gore tasarlanir.
- Sahiplik sorgularinda once `otel_kullanici_sahiplikleri`, sonra otel bazli indeksli tablolar kullanilir; tam tablo taramasi ile panel kurulmaz.
- Partner panel icin eklenen `partner_panel_tercihleri`, `partner_destek_talepleri`, `partner_destek_mesajlari`, `otel_istatistikleri`, `otel_rakip_analizi` ve `kullanici_oturum_istatistikleri` tablolarinin her biri kullanici/partner/otel anahtarlarina gore indeksli kurulur.
- Oturum suresi, toplam ziyaret sayisi, cihaz anahtari ve beni hatirla tercihi `kullanici_oturum_istatistikleri` tablosunda tutulur.
- Ayrintili aktif session takibi `kullanici_oturumlari`, toplu davranis ve sure ozetleri ise `kullanici_oturum_istatistikleri` tablosu ile ele alinir.

## 3.6) Partner Panel Canli Dogrulama Kurali
- Partner panel teslim edilmeden once en az su akislarda runtime smoke test alinmis olmalidir:
  - partner girisi
  - dashboard erisimi
  - takvim ve fiyatlar ekrani
  - destek ekrani
  - fotograf galerisi ekrani
- Yazma yapan partner aksiyonlari sadece derlenmekle yeterli sayilmaz; mumkun olanlar local DB uzerinde dogrulanir:
  - toplu fiyat guncelleme `oda_fiyat_musaitlik`
  - destek talebi `partner_destek_talepleri` ve `partner_destek_mesajlari`
  - fotograf yukleme `otel_gorselleri`
- Panel ekranlari veri yoksa bos beyaz alan birakmaz; her liste, grafik ve tablo icin anlamli `empty state` zorunludur.
- Partner otel yonetimi yalnizca kullanicinin `otel_kullanici_sahiplikleri` ile bagli oldugu oteller uzerinden calisir.

## 3.7) Firma Alani Zorunlu Dosyalari
- Firma public alani ayri dizinde tutulur:
  - `Views/Firma/Firma.cshtml`
  - `Views/Firma/_FirmaHeader.cshtml`
  - `Views/Firma/_FirmaContent.cshtml`
  - `Views/Firma/_FirmaFooter.cshtml`
  - `Controllers/Firma/FirmaController.cs`
  - `Models/Firma/FirmaViewModels.cs`
- Firma paneli kendi panel alaninda tutulur:
  - `Views/Paneller/Firma/_FirmaPanelLayout.cshtml`
  - `Views/Paneller/Firma/_FirmaSidebar.cshtml`
  - `Views/Paneller/Firma/_FirmaPanelFooter.cshtml`
  - `Views/Paneller/Firma/_FirmaMobileNav.cshtml`
  - `Views/Paneller/Firma/Dashboard.cshtml`
  - `Views/Paneller/Firma/Deals.cshtml`
  - `Views/Paneller/Firma/Reservations.cshtml`
  - `Views/Paneller/Firma/Employees.cshtml`
  - `Views/Paneller/Firma/Limits.cshtml`
  - `Views/Paneller/Firma/Invoices.cshtml`
  - `Views/Paneller/Firma/Spending.cshtml`
  - `Views/Paneller/Firma/Hotels.cshtml`
  - `Controllers/Paneller/Firma/FirmaPanelController.cs`
  - `Models/Paneller/Firma/FirmaPanelViewModels.cs`
- Firma public sayfasi veya paneli `Views/Home`, `Views/Shared` ya da baska panel klasorlerine dagitilmaz.

## 4.2) Firma Panel CSS Klasor Sozlesmesi
- Firma panel CSS dosyalari alan klasoru altinda tutulur:
  - `wwwroot/assets/css/paneller/firma/shell.css`
  - `wwwroot/assets/css/paneller/firma/dashboard.css`
  - `wwwroot/assets/css/paneller/firma/deals.css`
  - `wwwroot/assets/css/paneller/firma/reservations.css`
  - `wwwroot/assets/css/paneller/firma/employees.css`
  - `wwwroot/assets/css/paneller/firma/limits.css`
  - `wwwroot/assets/css/paneller/firma/invoices.css`
  - `wwwroot/assets/css/paneller/firma/spending.css`
  - `wwwroot/assets/css/paneller/firma/hotels.css`
- Firma public sayfasi icin ayri public CSS kullanilir:
  - `wwwroot/assets/css/firma.css`

## 6.4) Firma Rol ve Veri Sozlesmesi
- Firma kullanicilari `users` tablosunda tutulur; rol bilgisi `users.rol` alanindan okunur.
- Firma icin gecerlilik roleri:
  - `firma_admin`
  - `firma_manager`
  - `firma_staff`
- Firma kullanicisinin hangi firmaya bagli oldugu `users.firma_id` uzerinden belirlenir.
- Firma panelindeki harcama limiti, onay akisi ve kurumsal fiyat ozellikleri yalnizca gercek tablolar uzerinden okunur:
  - `firmalar`
  - `firma_harcama_limitleri`
  - `firma_ozel_fiyatlar`
  - `rezervasyonlar.firma_id`
  - `rezervasyonlar.firma_calisan_id`
  - `rezervasyonlar.firma_onay_durumu`
  - `faturalar.firma_id`
- Firma panelinde gorunen ozet kartlar, fiyat listeleri, calisanlar, faturalar ve raporlar statik veri ile birakilmaz; her ekran gercek firma kaydi ve bagli kullanici verisi ile calisir.
- Firma panelindeki yonetim aksiyonlari yalnizca okunur liste seklinde birakilmaz; asgari su yazma akislarinin backend ve veritabani baglantisi kurulur:
  - calisan ekleme
  - limit kaydetme / guncelleme
  - firma onayli rezervasyon isleme alma
- Firma panelinde eklenen yazma akislarinin tamaminda local DB smoke test zorunludur.
- Public `firma` sayfasi ile `paneller/firma` ekranlari ayni veri mantigini kullanir; ozel fiyat, aktif firma ve rapor kartlari farkli sabit veri kaynagindan beslenmez.

## 3.8) Firma Panel Canli Dogrulama Kurali
- Firma panel teslim edilmeden once en az su akislarda runtime smoke test alinmis olmalidir:
  - firma kullanicisi ile giris
  - dashboard erisimi
  - firma fiyatlari ekrani
  - rezervasyonlar ekrani
  - calisanlar ekrani
  - limitler ve onaylar ekrani
  - faturalar ekrani
  - harcama raporlari
  - otel bazli rapor
- Yazma yapan firma aksiyonlari yalnizca derlenmekle yeterli sayilmaz; local DB uzerinde dogrulanir:
  - calisan olusturma `users`
  - limit kaydetme `firma_harcama_limitleri`
  - rezervasyon onayi `rezervasyonlar.firma_onay_durumu`

## 3.9) Tum Panellerde Guvenli Cikis Zorunlulugu
- Admin, partner, firma, kullanici ve satis panellerinin tamaminda cikis aksiyonu gorunur olmak zorundadir.
- Cikis yalnizca link olarak birakilmaz; CSRF korumali `POST /cikis-yap` formu ile calisir.
- Her panel turunde cikis aksiyonu en az iki noktada bulunur:
  - sidebar / mobil nav
  - panel header arac cubugu
- Cikis sonrasi kullanici kendi rolune uygun giris ekranina yonlenmelidir:
  - admin -> `/admin-giris`
  - partner -> `/partner-giris`
  - firma / kullanici / satis -> `/kullanici-giris`
- Panel teslimi icin sadece butonun gorunmesi yeterli sayilmaz; su smoke test zorunludur:
  - giris yap
  - panel ekrani acilsin
  - header logout gorunsun
  - logout sonrasi korumali sayfaya tekrar gidildiginde login ekranina dusulsun

## 3.10) Panel Sidebar Marka Kurali
- Admin, partner, firma, satis ve kullanici panellerinin sidebar marka alaninda yazi tabanli logo kullanilmaz.
- Sidebar marka alani yalnizca gercek marka gorseli ile calisir:
  - `/uploads/logo/logo.png`
- Logo alani tum panellerde daha sade ve minimalist bir kutu icinde sunulur; asiri buyuk, tasan veya farkli panelde farkli orantida marka kullanimi yapilmaz.
- Sidebar sade tutulur:
  - menu satirlari daha kompakt olur
  - gereksiz metin kalabaligi azaltilir
  - marka alani ile profil alani birbirinden net ayrilir

## 3.11) Admin Otel Yonetimi Sozlesmesi
- `admin/oteller` ekrani yalnizca okunur liste olmayacak; admin bir otelin tum ana baglamlarini tek akisla yonetebilmelidir.
- Admin otel yonetimi icin en az su iki ekran zorunludur:
  - `/admin/oteller`
  - `/admin/oteller/duzenle/{id}`
- `admin/oteller` listesindeki her kartta veya satirda gorunur bir `Oteli Duzenle` aksiyonu bulunur.
- `admin/oteller/duzenle/{id}` ekraninda ayni sayfa icinde en az su baglamlar yonetilir:
  - `oteller` tablosu ana kolonlari
  - `oda_tipleri`
  - `otel_gorselleri`
  - `oda_gorselleri`
- Gelistirme asamasinda hizli konfigurasyon amaciyla admin bir oteli tek yerden acip:
  - otel temel bilgilerini guncelleyebilmeli
  - oda tipi ekleyebilmeli / duzenleyebilmeli / pasife alabilmeli
  - otel gorseli yukleyebilmeli / duzenleyebilmeli / kapak yapabilmeli / silebilmeli
  - oda gorseli yukleyebilmeli / duzenleyebilmeli / kapak yapabilmeli / silebilmeli
- Bu ekranlar sahte partial veya placeholder ile birakilmaz; her aksiyonun backend servisi ve gercek DB baglantisi bulunur.
- Admin otel yonetimi icin CSS ayri tutulur:
  - `wwwroot/assets/css/panel-admin-hotels.css`
- Admin otel yonetimi teslim edilmeden once su runtime smoke test zorunludur:
  - admin login
  - `/admin/oteller` acilisi
  - `Oteli Duzenle` aksiyonu
  - `/admin/oteller/duzenle/{id}` acilisi
  - en az bir zararsiz POST aksiyonu (`kapak yap`, `duzenleme`, `kaydet` gibi)

## 8.6) Yuksek Trafik ve Gercek Veritabani Kuralı
- Tum panel, public sayfa, servis ve API akislarinda tasarimlar gercek veritabani ile calisacak sekilde kurulacaktir.
- Bir sayfada veya sablonda bulunan ozelligin veritabani karsiligi yoksa once migration / tablo / sutun / indeks / iliski eksigi tamamlanir, sonra ekran gelistirilir.
- Her sayfa; binlerce eszamanli kullanici, on binlerce otel ve yuksek panel trafigi varmis gibi dusunulerek gelistirilir.
- Sorgular performansli, indeks dostu, filtreli ve sayfalanabilir olacak sekilde yazilir.
- Sadece gorunur tasarim teslimi kabul edilmez; veri okuma, veri yazma, oturum, yetki, CSRF ve hata senaryolari birlikte dogrulanir.

## 8.7) Adres Verisi Kuralı
- Kullanici, partner ve firma formlarinda kullanilan il / ilce / mahalle verisi statik dizi veya elde yazilmis front-end listesi ile yonetilmez.
- Adres secim altyapisi yalnizca veritabani tablolari uzerinden calisir:
  - `iller`
  - `ilceler`
  - `mahalleler`
- Adres alanlarinda sablonda gerekli olan veri veritabaninda eksikse once migration ile tablo / sutun / indeks eksigi tamamlanir, sonra ekran baglanir.
- Turkiye geneli il ve ilce verisi eksiksiz tutulur; sehir bazli ihtiyac varsa ilgili ilin mahalle verisi de seed edilir.
- Adres verisi guncellenirken mumkunse ucretsiz ve guvenilir veri kaynagi kullanilir; veriler idempotent seed mantigiyla tekrar calistirilabilir sekilde eklenir.
- Dis kaynaklardan gelen kimlik ve baglam verileri icin su alanlar korunur:
  - `iller.plaka_kodu`
  - `iller.nufus`
  - `ilceler.api_kodu`
  - `ilceler.nufus`
  - `mahalleler.api_kodu`
  - `mahalleler.nufus`

## 8.8) Favoriler ve Kaydet Akisi Kurali
- Anasayfa, otel listeleme ve otel detay sayfalarindaki `Kaydet / Favori` butonlari front-end sahte toggle ile birakilmaz; gercek DB kaydina bagli calisir.
- Favori yapisi yalnizca `user` tipindeki oturumlar icin calisir; partner, admin, firma ve satis kullanicilari kullanici favori akisina yazilmaz.
- Favori aksiyonu API tabanli ve CSRF korumali olur:
  - `POST /api/favoriler/toggle`
- Favori baglami veritabaninda en az su alanlari tasir:
  - `user_id`
  - `otel_id`
  - `kaynak_sayfa`
  - `kaynak_url`
  - `cihaz_tipi`
  - `ip_adresi`
  - `aktif_mi`
  - `son_islem_tarihi`
  - `kaldirilma_tarihi`
  - `olusturulma_tarihi`
- Favori silme fiziksel kaydi hard delete yapmak zorunda degildir; audit ve tekrar-aktiflestirme icin `aktif_mi` mantigi kabul edilir.
- `Favorilerim` panel ekrani yalnizca gercek `user_favori_oteller` verisiyle dolar; sabit kart, sahte sayi ve localStorage tabanli simulasyon kabul edilmez.
- Teslim icin zorunlu runtime smoke test:
  - gercek kullanici login
  - anasayfa veya otel detaydan favori ekleme
  - `user_favori_oteller` kaydinin DB'de gorulmesi
  - `/panel/user/favorilerim` ekraninda kartin gorunmesi
  - ayni aksiyonla favoriden cikarip `aktif_mi = 0` durumunun dogrulanmasi

## 8.9) Kampanya ve Kampanya-Otel Mimarisi Kurali
- Kampanyalar yalnizca basit baslik ve indirim orani tablosu ile sinirli tutulmaz; landing, SEO, vitrin ve partner katilim senaryolarini karsilayacak sekilde modellenir.
- `kampanyalar` tablosunda en az su alanlar bulunur:
  - `kampanya_kodu`
  - `kampanya_adi`
  - `seo_slug`
  - `sayfa_url`
  - `kampanya_aciklamasi`
  - `kisa_aciklama`
  - `detay_aciklama`
  - `banner_gorseli`
  - `hero_gorseli`
  - `kart_gorseli`
  - `mobil_gorsel`
  - `meta_title`
  - `meta_description`
  - `canonical_url`
  - `kampanya_etiketi`
  - `promo_badge`
  - `kampanya_renk_kodu`
  - `listeleme_basligi`
  - `listeleme_aciklamasi`
  - `kullanim_kosullari`
  - `gorunurluk_durumu`
  - `partner_katilim_acik`
  - `partner_katilim_baslangic`
  - `partner_katilim_bitis`
  - `otomatik_sona_ersin`
  - `siralama`
  - `aktif_sayfa_vitrini`
  - `gosterim_adedi`
- Partner veya admin tarafindan kampanyaya katilan oteller icin ayri iliski tablosu zorunludur:
  - `kampanya_oteller`
- `kampanya_oteller` tablosu en az su alanlari tasir:
  - `kampanya_id`
  - `otel_id`
  - `partner_id`
  - `katilim_durumu`
  - `katilim_kaynagi`
  - `baslangic_tarihi`
  - `bitis_tarihi`
  - `ozel_indirim_orani`
  - `ozel_indirim_tutari`
  - `ozel_kampanyali_fiyat`
  - `kampanya_etiketi`
  - `landing_url`
  - `partner_notu`
  - `one_cikan`
  - `siralama`
  - `admin_onay_tarihi`
  - `partner_onay_tarihi`
  - `olusturan_kullanici_id`
  - `guncelleyen_kullanici_id`
- Kampanya vitrini ve public kampanya sayfalari yalnizca su durumda gosterilir:
  - `kampanyalar.gorunurluk_durumu = 'Yayında'`
  - `kampanyalar.aktif_mi = 1`
  - tarih bugun ile `baslangic_tarihi / bitis_tarihi` araligindadir
  - iliski gerekiyorsa `kampanya_oteller.katilim_durumu = 'Aktif'`
  - iliski tarihi de bugun icin gecerli olmalidir
- Kampanya bittiginde veya tarih araligi gectiginde ilgili kampanya ve ona bagli oteller public vitrinde gosterilmez.
- Kampanya migrasyonlari MySQL uyumlu, tekrar calistirilabilir ve idempotent sekilde yazilir; `IF NOT EXISTS` destegi belirsiz ise `information_schema` kontrollu dinamik SQL tercih edilir.
## 2026-04-14 Kampanya ve Favori Kuralları

- Public `favori` aksiyonları sadece gerçek `user` oturumu ile çalışır.
- Giriş yapmamış kullanıcı favori butonuna bastığında doğrudan sessiz redirect yapılmaz; sayfa içinde `Lütfen giriş yapınız` uyarısı ve `Giriş Yap` / `Kayıt Ol` aksiyonları gösterilir.
- Public header kullanıcı alanında favori sayısı gerçek veritabanındaki aktif favori sayısına göre badge olarak gösterilir.
- Kampanya ekranları yalnızca `aktif_mi = 1`, görünürlük durumu yayında olan ve tarih aralığı aktif olan kampanyaları gösterir.
- `kampanya_oteller` tablosu kampanya-otel ilişkisinin tek kaynak tablosudur; partner katılımı, vitrinde öne çıkarma, sıralama, partner notu, landing URL, özel indirim ve kampanyalı fiyat bu tablodan yönetilir.
- Kampanya süresi bittiğinde public kampanya sayfaları ve kampanyalı oteller listeleri otomatik olarak ilgili kampanyayı göstermemelidir.
- Partner panelde kampanyaya katıl / ayrıl akışları yalnızca partnerin yetkili olduğu oteller için çalışmalıdır; istemciden gelen `otelId` sunucuda tekrar doğrulanmadan işleme alınmaz.
- Public sayfalar, paneller ve API uçları aynı veritabanı doğruluk kuralları ile çalışır; şablonda görünen kampanya/favori özelliği için karşılık tablosu ve bağlamı kurulmadan görünüm tarafı tamamlanmış kabul edilmez.

## 8.10) Firma Başvuru ve Onay Kurali
- `firma` public alanı, `kurumsal`dan ayrı bağımsız header/footer ve kendi CSS dosyası ile yönetilir; dosyalar `Views/Firma` ve `wwwroot/assets/css/firma*.css` altında kalır.
- `Firma Girişi` ve `Firma Hesabı Açın` akışları genel kullanıcı kaydı altında gizlenmez; ayrı rota ile açılır:
  - `/firma-giris`
  - `/firma-kayit`
- Firma hesabı açma akışı için gerekli alanlar tek merkezde `firmalar` tablosunda tutulur; başvuru verisini taşımak için geçici/sahte tablo açılıp asıl kayıt sonradan başka yere taşınmaz.
- `firmalar` tablosunda en az şu yaşam döngüsü alanları bulunur:
  - `onay_durumu`
  - `basvuru_tarihi`
  - `onay_sureci_baslama_tarihi`
  - `onay_tarihi`
  - `reddedilme_tarihi`
  - `onaylayan_kullanici_id`
  - `onay_notu`
  - `giris_izni_aktif_mi`
  - `planlanan_onay_suresi_saat`
  - `kayit_kaynagi`
  - `sozlesme_onay_tarihi`
  - `kvkk_onay_tarihi`
  - `yetkili_unvani`
- Firma basvurusu olustugunda yonetici kullanicisi `users` tablosunda acilabilir; ancak firma `Onaylandı` ve `giris_izni_aktif_mi = 1` olmadan panel girisi alamaz.
- Firma login dogrulamasi yalnizca sifre dogrulama ile bitmez; `users -> firmalar` baglaminda onay, aktiflik ve giris izni server-side yeniden kontrol edilir.
- `/firma` landing sayfasindaki fiyat karsilastirma bloklari gercek otel adi kullanmadan, mantigi anlatan ornek senaryo olarak gosterilir. Gercek otel verisi ile karistirilabilecek demo kart kabul edilmez.

## 8.11) Vendor / Local Asset Kurali
- Aktif public ve panel ekranlarinda CDN uzerinden kritik CSS, JS, font ve icon kutuphaneleri calistirilmaz; bunlar `wwwroot/assets/vendor` altinda lokal olarak tutulur.
- Yeni bir UI kutuphanesi eklenirse yalniz link etiketi ile gecilmez; ayni turda local vendor kopyasi da projeye eklenir.
- `fonts.googleapis.com`, `fonts.gstatic.com`, `cdnjs`, `jsdelivr` gibi dis baglantilar gelistirme sirasinda kabul edilse bile final kullanimda local karsilikla degistirilir.
- View/partial dosyalari vendor yolunu dogrudan kullanir; ornek: `~/assets/vendor/...`

## 8.12) Firma Basvuru Audit ve Seed Kurali
- `firmalar` tablosu yalniz son durumu tutar; durum gecmisi gerekiyorsa `firma_basvuru_hareketleri` tablosu ile audit izi korunur.
- Firma onboarding gelistirmelerinde en az su test senaryolari DB seed ile hazir tutulur:
  - `Onaylandı`
  - `Beklemede`
  - `Reddedildi`
  - `Askıda`
- Basvuru workflow migration'lari manuel calistirilsa bile `schema_migrations` tablosuna checksum ile kaydedilir.
- Firma login kabul testi, en az bir onayli ve en az bir onaysiz hesapla servis veya HTTP seviyesinde dogrulanmadan tamamlanmis sayilmaz.
- Public veya panel sayfalarında `href="#"` / `action="#"` placeholder bırakılmaz. Gerçek route bağlanır veya erişime kapalı bir aksiyon ise `button type="button" disabled` yaklaşımı kullanılır.

## Panel DB Uyum Kuralı
- Panel sayfaları yalnızca açılıyor olmakla kabul edilmiş sayılmaz; ekran içeriği mümkün olan her yerde gerçek veritabanı sorguları ile beslenmeli, statik örnek içerik geçici ise notlanmalıdır.
- Geliştirme sırasında migration dosyaları hedef veritabanı motoru ile uyumlu hazırlanmalıdır. MariaDB/MySQL sürüm farkı olan projelerde IF NOT EXISTS gibi sözdizimleri körlemesine kullanılmayacak; gerekiyorsa INFORMATION_SCHEMA kontrollü idempotent migration yazılacaktır.
- Kullanıcı, partner, firma ve admin panelindeki her POST aksiyonu build ve DB yazma testi ile doğrulanmadan tamamlanmış kabul edilmez.

## 8.13) Guvenli Mesaj ve Belge Kurali
- `panel/user/mesajlarim` ve firma mesaj alanlari ortak bir mesaj merkezi mantigi ile calisir; ayni mesajlasma davranisi iki ayri sahte sorgu seti ile kopyalanmaz.
- Mesajlasma akisi icin `mesaj_konusmalari` ve `mesajlar` tablolari firma baglami, okunmamis sayilari, soft delete, duzenleme ve dosya iliskileri ile genisletilmelidir.
- Guvenli ek dosyalar `wwwroot` altina yazilmaz; `App_Data/secure-storage/...` gibi public olmayan dizinde tutulur.
- Otel gorselleri disindaki kullanici/firma belgeleri dogrudan fiziksel URL ile yayinlanmaz; yalniz tokenli ve kullaniciya bagli `secure-files/{token}` rotasi ile servis edilir.
- Tokenli belge erisimi su kurallarla calisir:
  - token bir kullaniciya ve hesap tipine baglidir
  - suresi vardir
  - kullanim sayisi siniri destekler
  - token gecersiz, suresi dolmus veya hesap uyumsuz ise dosya yaniti verilmez
- Mesaj silme hard delete degildir; mesaj icerigi audit icin tabloda kalir, gorunumde `Bu mesaj silindi.` gibi bir placeholder ile blur/gri durum gosterilir.
- Mesaj ekleri icin baglama tablolari ayri tutulur:
  - `guvenli_dosya_varliklari`
  - `guvenli_dosya_erisim_tokenlari`
  - `mesaj_dosyalari`
- Kullanici ve firma mesajlasma akislari HTTP seviyesinde dogrulanmadan tamamlanmis sayilmaz:
  - sayfa acilisi
  - mesaj gonderme
  - ek dosya yukleme
  - tokenli dosya indirme
  - hesaplar arasi yetkisiz erisim engeli
  - soft delete placeholder gorunumu

