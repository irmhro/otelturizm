# Otelturizm Panel Tasarım Sistemi (Canon) · Linkli Standart

Kanonik panel UI standardı. Tüm paneller (Admin/Partner/Firma/Satış/Kullanıcı) için **ortak, minimalist, premium** görünüm kurallarını tanımlar.

## İçindekiler

- [1) Tasarım felsefesi](#1-tasarım-felsefesi)
- [2) Tasarım token’ları (CSS değişkenleri)](#2-tasarım-tokenları-css-değişkenleri)
- [3) Global stiller](#3-global-stiller)
- [4) Layout iskeleti (Header/Sidebar/Content/Footer)](#4-layout-iskeleti-headersidebarcontentfooter)
- [5) Durum rolleri (kırmızı/sarı/yeşil/mavi) + ikon sözlüğü](#5-durum-rolleri-kırmızısarıyeşilmavi--ikon-sözlüğü)
- [6) Bileşen kütüphanesi](#6-bileşen-kütüphanesi)
  - [6.1 Butonlar](#61-butonlar)
  - [6.2 Rozetler (badge)](#62-rozetler-badge)
  - [6.3 Kartlar (stat/activity/content)](#63-kartlar-statactivitycontent)
  - [6.4 Tablolar](#64-tablolar)
  - [6.5 Formlar](#65-formlar)
  - [6.6 Bildirimler (toast/alert/inline)](#66-bildirimler-toastalertinline)
  - [6.7 Filtre alanı](#67-filtre-alanı)
  - [6.8 Progres barları](#68-progres-barları)
  - [6.9 Grafik/KPI alanları](#69-grafikkpi-alanları)
- [7) Sidebar akordiyon sırası (tüm paneller için sabit)](#7-sidebar-akordiyon-sırası-tüm-paneller-için-sabit)
- [8) Responsive kurallar](#8-responsive-kurallar)
- [9) Uygulama notları (Bootstrap5 / Tailwind benzeri)](#9-uygulama-notları-bootstrap5--tailwind-benzeri)
- [10) QA / Kabul kriterleri](#10-qa--kabul-kriterleri)
- [11) AI prompt şablonu (iç sayfa üretimi)](#11-ai-prompt-şablonu-iç-sayfa-üretimi)

---

## 1) Tasarım felsefesi

- **Kurumsal & güvenilir**: `--primary` (Otelturizm mavi) ana renktir.
- **Temiz & ferah**: açık arka plan, bol beyaz alan, yumuşak gölge.
- **Mobil öncelikli**: bileşenler önce mobilde doğru çalışır, masaüstünde grid ile genişler.
- **Tutarlı**: aynı sınıf isimleri + aynı token’lar her panelde kullanılır.
- **Erişilebilir**: renk tek başına anlam taşımaz → **ikon + metin** zorunlu.
- **Tabler zorunlu**: 2026-04 itibarıyla panel ekranlarında temel şablon **Tabler**’dır. Panel içi sayfa geliştirmelerinde Tabler bileşenleri (Home / Interface / Forms / Extra / Layout / Plugins / Addons / Help) görünüm uyumu korunur; yeni UI işlerinde Tabler dışına çıkılmaz.
- **Yerel Tabler önizleme**: Geliştirme sırasında Tabler bileşenlerini birebir referans almak için sadece localhost’ta çalışan `/paneltema` sayfası kullanılır. Panel geliştirmesi bittiğinde bu preview alanı kaldırılacaktır.

---

## 2) Tasarım token’ları (CSS değişkenleri)

> Kural: Üretimde “hardcoded” renk/spacing yazma. Aşağıdaki token’lar referans alınır.

```css
:root {
  --font-main: 'Satoshi', sans-serif;

  /* Tipografi (responsive) */
  --h1-size: clamp(1.8rem, 4vw, 2.4rem);
  --h2-size: clamp(1.4rem, 3vw, 1.8rem);
  --h3-size: clamp(1.1rem, 2.5vw, 1.4rem);
  --body-size: 0.95rem;
  --line-height-base: 1.5;
  --letter-spacing-tight: -0.01em;

  /* Renk paleti */
  --primary: #003B95;
  --primary-dark: #002A6B;
  --secondary: #FF385C;
  --success: #00A86B;
  --warning: #FFB800;
  --error: #EF4444;
  --gold: #FFD700;
  --bg-light: #F8FAFC;
  --card-bg: #FFFFFF;
  --text-dark: #1A1F26;
  --text-gray: #4a4a4a;
  --text-light: #94A3B8;
  --border-light: #E2E8F0;

  /* Spacing */
  --space-1: 4px;
  --space-2: 8px;
  --space-3: 12px;
  --space-4: 16px;
  --space-5: 20px;
  --space-6: 24px;
  --space-8: 32px;

  /* Radius */
  --radius-sm: 12px;
  --radius-md: 16px;
  --radius-lg: 20px;
  --radius-xl: 24px;
  --radius-full: 9999px;

  /* Shadow */
  --shadow-sm: 0 2px 8px rgba(0,0,0,0.04);
  --shadow-md: 0 8px 24px rgba(0,0,0,0.08);
  --shadow-lg: 0 20px 40px -10px rgba(0, 59, 149, 0.12);

  /* Layout */
  --sidebar-width: 280px;
}
```

---

## 3) Global stiller

```css
* { margin: 0; padding: 0; box-sizing: border-box; }
body {
  font-family: var(--font-main);
  line-height: var(--line-height-base);
  -webkit-font-smoothing: antialiased;
  background: var(--bg-light);
  color: var(--text-dark);
  font-size: var(--body-size);
}
h1, h2, h3 {
  letter-spacing: var(--letter-spacing-tight);
  font-weight: 700;
  margin-bottom: 0.35em;
  line-height: 1.3;
}
h1 { font-size: var(--h1-size); }
h2 { font-size: var(--h2-size); }
h3 { font-size: var(--h3-size); }
```

---

## 4) Layout iskeleti (Header/Sidebar/Content/Footer)

### Panel konteyneri

```css
.dashboard {
  display: flex;
  min-height: 100vh;
}
```

### Sidebar (akordiyon)
- Genişlik: `var(--sidebar-width)`
- Arka plan: `var(--card-bg)`
- Sağ border: `1px solid var(--border-light)`
- Mobil (`≤1000px`): drawer veya gizli

### Main content
- Desktop: `margin-left: var(--sidebar-width)`
- Mobil: `margin-left: 0`

### Header
- Sol: logo + panel adı
- Orta: sayfa başlığı (opsiyonel)
- Sağ: arama mini + bildirim + kullanıcı menüsü + hızlı aksiyon

### Footer
- Minimal: telif + saat + sürüm (opsiyonel)

---

## 5) Durum rolleri (kırmızı/sarı/yeşil/mavi) + ikon sözlüğü

> Kural: Durumlar **renk + ikon + metin** ile gösterilir.

- **Danger / Red**: Reddedildi / Onaylanmadı / Silme / Hata  
  - Metin: `Reddedildi`, `Sil`, `İşlem başarısız`  
  - İkon: `fa-circle-xmark`, `fa-trash`, `fa-triangle-exclamation`

- **Success / Green**: Onaylandı / Kabul / Aktif  
  - Metin: `Onayla`, `Kabul et`, `Aktif`  
  - İkon: `fa-circle-check`, `fa-check`, `fa-badge-check`

- **Warning / Amber**: Bekliyor / İncelemede / Uyarı  
  - Metin: `Bekliyor`, `İncelemede`  
  - İkon: `fa-hourglass-half`, `fa-clock`, `fa-circle-exclamation`

- **Info / Blue**: Bilgi / Detay / Görüntüle / Tanımlar  
  - Metin: `Detay`, `Görüntüle`, `Bilgi`  
  - İkon: `fa-circle-info`, `fa-eye`, `fa-arrow-right`

Ek önerilen ikon sözlüğü:
- Düzenle: `fa-pen-to-square`
- Filtre: `fa-sliders`
- Ara: `fa-magnifying-glass`
- İndir: `fa-download`

---

## 6) Bileşen kütüphanesi

### 6.1 Butonlar

Kural seti:
- **Her butonda ikon** (dar alan istisna: ikon-only).
- **Loading**: metin sabit, sağda loader.
- **Confirm**: sil/red/iptal → modal onayı.

Önerilen sınıflar:
- `.btn` (taban)
- `.btn-primary` `.btn-success` `.btn-danger` `.btn-warning` `.btn-outline`
- `.btn-sm` `.btn-lg`

### 6.2 Rozetler (badge)

- `.badge` taban: inline-flex, radius-full
- `.badge-success` `.badge-warning` `.badge-error` `.badge-info`

### 6.3 Kartlar (stat/activity/content)

Temel kart:
- border + radius + hafif gölge
- hover: küçük translate + shadow-lg (istatistik kartlarında)

Sparkline (opsiyonel):

```html
<div class="sparkline">
  <div class="sparkline-bar" style="height: 12px;"></div>
  <div class="sparkline-bar" style="height: 16px;"></div>
</div>
```

### 6.4 Tablolar

- `.table-container`: border + radius + overflow hidden
- Mobil: `overflow-x: auto` + tabloya `min-width`
- Satır aksiyonu: en fazla 3 direkt (Detay/Onayla/Reddet), fazlası “…” menü

### 6.5 Formlar

- Desktop: 2 kolon grid, Mobil: 1 kolon
- Label üstte, hata input altında
- Aksiyonlar: sol “İptal” (outline/ghost), sağ “Kaydet” (primary/success)

### 6.6 Bildirimler (toast/alert/inline)

- `success/error/warning/info` tonları
- Aynı anda 1 kritik toast
- Form üstü inline uyarılar kısa ve aksiyon odaklı

### 6.7 Filtre alanı

- Kart görünümünde, `.filter-row` flex wrap + gap
- Filtreler “üst bar”da (tablo üstü) konumlanır

### 6.8 Progres barları

```css
.progress-bar { height: 6px; background: var(--border-light); border-radius: var(--radius-full); }
.progress-fill { height: 100%; background: var(--primary); border-radius: var(--radius-full); }
```

### 6.9 Grafik/KPI alanları

- Üstte KPI kartları (3–6)
- Altta 1 ana grafik + 1 küçük kırılım
- Renk: ana mavi, ikinci seri yeşil, negatif kırmızı

---

## 7) Sidebar akordiyon sırası (tüm paneller için sabit)

Sıra bozulmaz; yetki yoksa sadece ilgili item gizlenir.

1. **Genel** (Dashboard, Bildirim/Log)
2. **İşlemler** (Rezervasyonlar, Talepler/Başvurular)
3. **İçerik** (Oteller/Odalar/Fiyatlar, Kampanyalar)
4. **Raporlar** (Satış/Gelir/Performans)
5. **Ayarlar** (Profil/Şirket, Güvenlik/2FA, Entegrasyonlar)

---

## 8) Responsive kurallar

| Genişlik | Davranış |
| --- | --- |
| ≤ 1000px | Sidebar drawer/gizli, main margin 0 |
| ≤ 640px | KPI 1 kolon, iki kolon formlar 1 kolon, tablolar yatay kaydırılır |

---

## 9) Uygulama notları (Bootstrap5 / Tailwind benzeri)

- Panel kökünde tek theme sınıfı önerilir: `.panel-theme`
- Durumlar: `.is-success/.is-danger/.is-warning/.is-info` gibi tek tip sınıflar
- Sidebar/Buton/Bildiri sınıfları paneller arasında aynı DOM yapıda tutulur

---

## 10) QA / Kabul kriterleri

- “Onayla/Reddet/Bekliyor/Detay” her panelde aynı renk+ikon+metin
- Sidebar sırası sabit
- Mobilde drawer + tek kolon, tablo yatay scroll/kartlaşma
- Kontrast ve okunabilirlik yüksek (AA seviyesinde)

---

## 11) AI prompt şablonu (iç sayfa üretimi)

Kopyala-yapıştır prompt:

> “Bu projede `panel-tasarim-sistemi.md` kurallarına %100 uyarak bir panel sayfası üret.  
> Başlık: …  
> KPI kartları: …  
> Tablo kolonları: …  
> Durumlar: (bekliyor/onaylandı/reddedildi)  
> Aksiyonlar: (detay/onayla/reddet) ikonlu  
> Mobil uyumlu olacak.  
> Framework: (Bootstrap5/Tailwind/Mevcut CSS) …”

