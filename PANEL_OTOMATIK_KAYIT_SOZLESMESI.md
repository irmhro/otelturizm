# Panel Otomatik Kayıt Sözleşmesi

Bu sözleşme, Otelturizm **misafir / partner / firma / admin** panellerinde form ve seçim alanlarının nasıl kaydedileceğini tanımlar. Yeni panel sayfaları ve mevcut sayfa refaktörleri bu kurallara uymalıdır.

## Amaç

Kullanıcı bir alanı değiştirdiğinde veya bir seçim yaptığında veri **anında** sunucuya kaydedilir. Ayrı **Kaydet** butonu zorunlu değildir. İşlem sonucu ekranın üstünde kısa bir **toast bildirimi** gösterilir.

## Zorunlu UX Kuralları

1. **Seçim / toggle / select** → `change` olayında otomatik kayıt.
2. **Metin / textarea / sayı** → `blur` olayında otomatik kayıt (debounce ile).
3. **Tarih alanı** → `change` olayında otomatik kayıt.
4. **Dosya yükleme** → geçerli dosya seçildiğinde otomatik yükleme.
5. Form geçersizse (`checkValidity()` başarısız) kayıt **tetiklenmez**; kullanıcı zorunlu alanları tamamlayana kadar beklenir.
6. Başarılı kayıtta toast başlığı: **Kaydedildi**; metin alan/section bağlamına göre kısa Türkçe mesaj (ör. `Seçiminiz kaydedildi.`, `Profil bilgileriniz kaydedildi.`).
7. Hata durumunda toast başlığı: **Hata**; sunucu mesajı gösterilir.
8. Kayıt sırasında isteğe bağlı **Kaydediliyor…** bilgi toast’ı gösterilebilir; tamamlanınca başarı/hata toast’ı ile değiştirilir.
9. Silme gibi geri alınamaz işlemlerde onay (`confirm`) korunur; onay sonrası AJAX + toast + gerekirse sayfa yenileme.

## Teknik Standart

### HTML işaretleyicileri

| Öznitelik | Anlam |
|-----------|--------|
| `data-panel-auto-save` | Otomatik kayıt formu (debounce + fetch POST) |
| `data-panel-autosave="change"` | Alan değişince kaydet (select, checkbox, radio, date) |
| `data-panel-autosave="blur"` | Odak kaybında kaydet (input, textarea) |
| `data-panel-auto-save-success` | Başarı mesajı override (opsiyonel) |
| `data-panel-auto-action` | Tek aksiyon formu (seç / sil); submit engellenir, fetch POST |
| `data-panel-auto-upload` | Dosya seçilince otomatik yükleme formu |

### Ortak JS

- `wwwroot/assets/js/panel-auto-save.js` — form bağlama, debounce, fetch POST.
- `wwwroot/assets/js/panel-toasts.js` — `window.OtPanelToast.show(text, tone, title?)` programatik toast API.

Panel layout’larında her iki script yüklenmelidir.

### Sunucu (Controller)

- AJAX istekleri `X-Requested-With: XMLHttpRequest` header’ı ile gelir.
- Yanıt: `Json(new { success = true/false, message = "..." })`.
- Klasik form POST (JS devre dışı) için mevcut redirect + `TempData["UserSuccess"]` / `UserError` (veya panel tipine göre eşdeğeri) korunur.
- `[ValidateAntiForgeryToken]` korunur; fetch `FormData` ile token gönderilir.

### Toast altyapısı

- Partial: `Views/Paneller/Common/_PanelToasts.cshtml`
- Stil: `wwwroot/assets/css/paneller/panel-toasts.css`
- TempData anahtarları: `UserSuccess`, `UserError`, `UserProfileSuccess`, `UserProfileError`, `PartnerSuccess`, …

## Uygulama Örnekleri

| Sayfa | Durum |
|-------|--------|
| `/panel/user/profil-bilgilerim` | Otomatik kayıt (referans uygulama) |
| `/panel/partner/fotograflar` | Mevcut sayfa-içi status; yeni geliştirmelerde toast standardına geçilir |

## Yapılmaması Gerekenler

- Her alan için ayrı **Kaydet** butonu eklemek (istisna: çok adımlı doğrulama akışları — e-posta/telefon kodu gibi).
- Sayfa yenilemesi gerektiren tam form POST (otomatik kayıt akışında).
- Toast yerine sayfa altında kalıcı alert blokları (TempData redirect senaryosu hariç).

## İlgili Dokümanlar

- `Dokumanlar/Genel/kodlama-sozlesmesi.md` — genel UI ilkesi (madde 16)
- `Dokumanlar/Tasarim/panel-tasarim-sozlesmesi.md` — panel shell ve bileşen dili
