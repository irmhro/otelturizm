## Mobil Viewport Audit (Paket 148)

Amaç: Kritik public sayfalarda mobilde “kırılma / taşma / tıklanamaz alan / sticky çakışması” sorunlarını hızlıca yakalamak.

### Test cihazları

- 360×800 (Android Chrome)
- 390×844 (iPhone 13/14)
- 412×915 (Android büyük ekran)

### Kritik sayfalar

- `/` (anasayfa)
- `/oteller` (listeleme)
- `/oteller/{slug}` (detay + mobil booking bar)
- `/kampanyalar`
- `/kampanyalar/{slug}`
- `/kullanici-giris` (login)
- `/admin-giris` (admin login)

### Kontrol listesi

- **Header**
  - Menü butonu tıklanabilir mi?
  - Dil seçimi dropdown taşmıyor mu?
  - Arama input’u klavye açılınca ekrandan kaçıyor mu?
- **Sticky/Fixed çakışmaları**
  - Listing “mobile filter dock” ile footer/CTA çakışıyor mu?
  - Detay “mobile booking bar” form modal/backdrop ile çakışıyor mu?
  - Safe-area: `env(safe-area-inset-bottom)` var mı?
- **Görseller**
  - Hero görsel oranı bozuluyor mu?
  - Kart görselleri yüklenmeden layout zıplaması (CLS) var mı?
- **Formlar**
  - Date input, select, number input: zoom yapıyor mu?
  - Hata mesajları alan altında okunaklı mı?
- **Erişilebilirlik**
  - Klavye ile (Tab) odak sırası mantıklı mı?
  - Skip link çalışıyor mu?

