# Panel Tasarım Standardı (Otelturizm) · (Eski)

Bu dosya artık **kanonik değildir**.

- Yeni tek kaynak doküman: `panel-tasarim-sistemi.md`

Bu doküman; **Admin / Partner / Firma / Satış / Kullanıcı paneli** dahil tüm panellerde ortak kullanılacak **UI/UX, bileşen, renk ve ikon** standartlarını tanımlar. Hedef: **minimalist, hızlı, tutarlı, premium** bir görünüm.

> Not: Bu doküman “framework bağımsızdır”; Bootstrap 5, Tailwind benzeri utility yaklaşımı veya mevcut CSS yapısı ile uygulanabilir.

---

## 1) Temel prensipler

- **Tutarlılık**: Aynı iş aynı görsel dil ile temsil edilir.
- **Hiyerarşi**: Başlık → açıklama → aksiyon sırası net olmalı.
- **Okunabilirlik**: Panel içerikleri “metin yoğun” olabilir; satır aralığı ve kontrast yüksek tutulmalı.
- **Hız algısı**: Skeleton/placeholder yerine mümkün olduğunca stabil layout ve anında geri bildirim.
- **Erişilebilirlik**:
  - Butonlar ve menüler klavye ile gezilebilir olmalı.
  - Renk tek başına anlam taşımamalı; **ikon + metin** ile desteklenmeli.

---

## 2) Renk rolleri (Durum renkleri)

Bu renkler her panelde aynı anlamı taşır.

### Durum rolleri

- **Kırmızı (Danger / Red)**: *Reddedildi / Onaylanmadı / Hata / Silme*
  - Örnek metin: `Reddedildi`, `İptal edildi`, `Sil`, `İşlem başarısız`
  - İkon: `fa-circle-xmark`, `fa-triangle-exclamation`, `fa-trash`

- **Yeşil (Success / Green)**: *Onaylandı / Kabul / Aktif*
  - Örnek metin: `Onayla`, `Kabul et`, `Aktif`
  - İkon: `fa-circle-check`, `fa-check`, `fa-badge-check`

- **Sarı (Warning / Amber)**: *Bekliyor / İncelemede / Uyarı*
  - Örnek metin: `Bekliyor`, `İncelemede`, `Uyarı`
  - İkon: `fa-clock`, `fa-hourglass-half`, `fa-circle-exclamation`

- **Mavi (Info / Blue)**: *Bilgi / Detay / Görüntüle / Tanımlar*
  - Örnek metin: `Detay`, `Görüntüle`, `Bilgi`
  - İkon: `fa-circle-info`, `fa-eye`, `fa-arrow-right`

### Nötr rolleri
- **Nötr (Gray)**: *İptal / Vazgeç / İkincil aksiyon*
  - İkon: `fa-xmark`, `fa-arrow-left`

---

## 3) Buton standardı (ikon + metin zorunlu)

### Boyutlar
- **sm**: tablolar satır içi aksiyonlar
- **md (default)**: sayfa üstü ana aksiyonlar
- **lg**: kritik tek CTA (örn. “Kaydet ve Devam Et”)

### Buton tipleri
- **Primary**: sayfanın ana aksiyonu (mavi)
- **Success**: onay/kabul (yeşil)
- **Danger**: red/sil (kırmızı)
- **Warning**: beklet/uyarı (sarı)
- **Ghost/Outline**: ikincil aksiyonlar

### Kural seti
- **Her butonda ikon** olmalı. (Istisna: çok dar alanlarda sadece ikon butonu)
- **Disabled**: görsel olarak belirgin olmalı; hover yok.
- **Loading**: buton metni sabit kalmalı, sağda spinner/mini loader olmalı.
- **Confirm gerektiren** aksiyonlar: silme/red/iptal → modal onayı.

Örnek metin standardı:
- `Onayla` (Success)
- `Reddet` (Danger)
- `Beklet` (Warning)
- `Detay` (Info)

---

## 4) Panel sayfa iskeleti (Header / Sidebar / Content / Footer)

### Header
- Sol: Logo + panel adı
- Orta: sayfa başlığı (opsiyonel)
- Sağ: kullanıcı menüsü + bildirim + hızlı aksiyonlar

Header kısa olmalı. İçerik “görsel” değil “iş” odaklı.

### Sidebar (Akordiyon yapı)
Kural: **Tüm panellerde aynı sırayla** gider. Yetkiye göre gizlenebilir ama sıra bozulmaz.

Önerilen ana gruplar:
1. **Genel**
   - Dashboard
   - Bildirimler / Loglar
2. **İşlemler**
   - Rezervasyonlar
   - Talepler / Başvurular
3. **İçerik**
   - Oteller / Odalar / Fiyatlar
   - Kampanyalar
4. **Raporlar**
   - Satış / Gelir / Performans
5. **Ayarlar**
   - Profil / Şirket
   - Güvenlik / 2FA
   - Entegrasyonlar

Sidebar akordiyon kuralları:
- Aynı anda **tek grup açık** (varsayılan).
- Aktif sayfa **vurgulu** (sol çizgi + arkaplan).
- Menü öğeleri: **ikon + metin**.

### Footer
Panelde minimal: telif + saat + sürüm (opsiyonel). Link kalabalığı yapılmaz.

---

## 5) Form standardı

### Grid
- Desktop: 2 kolon (min 320px)
- Mobil: 1 kolon

### Alanlar
- Label üstte, input altta.
- Hata mesajı input altında.
- Zorunlu alanlar: `*` veya “Zorunlu” rozet.

### Aksiyonlar
- Form altı: sol “İptal” (ghost), sağ “Kaydet” (primary/success).

---

## 6) Tablo standardı

### Tablo üst bar
- Sol: başlık + açıklama
- Sağ: arama + filtre + dışa aktar (opsiyonel)

### Satır aksiyonları
- En fazla 3 aksiyon direkt göster (Detay / Onayla / Reddet)
- Diğerleri: “…” overflow menü

### Durum kolonları
Durumlar **renk + ikon + metin**:
- `✅ Onaylandı` (green)
- `⏳ Bekliyor` (yellow)
- `⛔ Reddedildi` (red)
- `ℹ️ Bilgi` (blue)

---

## 7) Kart standardı

Kart içi hiyerarşi:
- Üst: Başlık + sağda durum rozeti
- Orta: kısa metrikler / açıklama
- Alt: aksiyon barı (sağda)

Kart gölgesi “hafif”, border “net” olmalı.

---

## 8) Bildiriler (Toast / Alert / Inline)

Türler:
- **Success**: işlem başarılı
- **Error**: işlem hatası
- **Warning**: dikkat/eksik
- **Info**: bilgilendirme

Kurallar:
- 1 ekranda aynı anda en fazla 1 kritik toast.
- Inline uyarılar (form üstü) mümkünse kısa ve aksiyon odaklı.

---

## 9) Grafik / KPI alanları

Panel grafik alanı için standart:
- Üstte KPI kartları (3–6 adet)
- Altta grafik (1 ana grafik + 1 küçük kırılım)
- Filtre: tarih aralığı, segment (opsiyonel)

Grafik renkleri:
- Ana seri: mavi
- İkinci seri: yeşil
- Uyarı/negatif: kırmızı

---

## 10) İkon seti

FontAwesome ile önerilen minimum ikon sözlüğü:
- Onayla: `fa-circle-check`
- Reddet: `fa-circle-xmark`
- Bekliyor: `fa-hourglass-half` / `fa-clock`
- Detay: `fa-eye`
- Düzenle: `fa-pen-to-square`
- Sil: `fa-trash`
- Filtre: `fa-sliders`
- Ara: `fa-magnifying-glass`
- İndir: `fa-download`
- Uyarı: `fa-triangle-exclamation`
- Bilgi: `fa-circle-info`

---

## 11) Uygulama notları (Bootstrap5 / Tailwind benzeri)

Bu dokümanı panellere uygularken:
- Panel kökünde tek bir “theme” sınıfı kullanın (örn. `.panel-theme`).
- Durum renklerini her yerde aynı sınıfla verin (örn. `.is-success/.is-danger/.is-warning/.is-info`).
- Sidebar için aynı DOM yapısı + aynı class isimleri kullanın.

---

## 12) Kabul kriterleri (QA)

- Aynı aksiyon (Onayla/Reddet/Beklet/Detay) tüm panellerde **aynı renk + ikon** ile görünür.
- Sidebar sırası her panelde aynı; yetkiye göre sadece öğeler gizlenebilir.
- Mobilde: sidebar drawer, içerik tek kolon, tablolar yatay scroll veya kartlaşma.
- Kontrast: metin/arka plan WCAG AA seviyesinde okunur.

