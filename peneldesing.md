# 🧠 OTELTURİZM ULTRA PROFESYONEL PANEL TASARIM SİSTEMİ · AI KODLAMA PROMPTU

# OTELTURİZM ULTRA PROFESYONEL PANEL TASARIM SİSTEMİ (v1.0)

Bu belge, Otelturizm platformu için geliştirilmiş **ultra profesyonel, modern ve mobil uyumlu** yönetim paneli tasarım dilini tanımlar. Aşağıdaki kurallara **harfiyen uyarak** kod üret.

---

## 1. TASARIM FELSEFESİ

- **Kurumsal & Güvenilir:** `#003B95` (Primary Blue) ana renktir.
- **Temiz & Ferah:** Açık arka plan (`#F8FAFC`), bol beyaz alan, yumuşak gölgeler.
- **Mobil Öncelikli:** Tüm bileşenler önce mobil için tasarlanır, masaüstünde grid ile genişler.
- **Tutarlı:** Tüm sayfalarda aynı CSS değişkenleri ve bileşen sınıfları kullanılır.

---

## 2. CSS DEĞİŞKENLERİ (KÖK TANIMLAR)

Aşağıdaki `:root` bloğunu **kesinlikle** kullan. Hiçbir rengi veya değeri sabit (hardcoded) yazma.

```css
:root {
  --font-main: 'Satoshi', sans-serif;
  
  /* Tipografi (clamp ile responsive) */
  --h1-size: clamp(1.8rem, 4vw, 2.4rem);
  --h2-size: clamp(1.4rem, 3vw, 1.8rem);
  --h3-size: clamp(1.1rem, 2.5vw, 1.4rem);
  --body-size: 0.95rem;
  --line-height-base: 1.5;
  --letter-spacing-tight: -0.01em;
  
  /* Renk Paleti */
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
  
  /* Aralıklar (Spacing Scale) */
  --space-1: 4px;
  --space-2: 8px;
  --space-3: 12px;
  --space-4: 16px;
  --space-5: 20px;
  --space-6: 24px;
  --space-8: 32px;
  
  /* Yarıçaplar */
  --radius-sm: 12px;
  --radius-md: 16px;
  --radius-lg: 20px;
  --radius-xl: 24px;
  --radius-full: 9999px;
  
  /* Gölgeler */
  --shadow-sm: 0 2px 8px rgba(0,0,0,0.04);
  --shadow-md: 0 8px 24px rgba(0,0,0,0.08);
  --shadow-lg: 0 20px 40px -10px rgba(0, 59, 149, 0.12);
  
  /* Layout */
  --sidebar-width: 280px;
}
```

## 3. GLOBAL STİLLER

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

## 4. LAYOUT YAPISI

### 4.1. Dashboard Konteyneri

```css
.dashboard {
  display: flex;
  min-height: 100vh;
}
```

### 4.2. Sidebar (Sabit Sol Menü)

- Genişlik: `var(--sidebar-width)`
- Arka plan: `var(--card-bg)`
- Sağ kenarlık: `1px solid var(--border-light)`
- Mobilde (`max-width: 1000px`) `display: none`.

İçerik:

- `.sidebar-header`: Logo (`.logo`).
- `.sidebar-nav`: `.nav-item` linkleri.
- Aktif sayfa: `background: var(--primary); color: white;`.

### 4.3. Ana İçerik (`.main-content`)

- `flex: 1; margin-left: var(--sidebar-width);` (Mobilde `margin-left: 0`).

### 4.4. Uygulama Header'ı (`.app-header`)

- Sabit (sticky) değil, sayfanın üstünde.
- Arka plan: `var(--card-bg)`, alt kenarlık: `1px solid var(--border-light)`.
- İçerik: Sol tarafta `.page-title` ve `.breadcrumb`, sağ tarafta `.search-mini`, `.header-actions` (ikon butonlar), `.user-dropdown`.
- İkon Buton (`.icon-btn`): Yuvarlak, hover'da `background: var(--primary); color: white;`.
- Bildirim Noktası (`.badge-dot`): Butonun sağ üstünde kırmızı daire.

## 5. BİLEŞEN KÜTÜPHANESİ

### 5.1. Butonlar (`.btn`)

| Sınıf | Açıklama | Stil |
| --- | --- | --- |
| `.btn` | Taban | `padding: 10px 18px; border-radius: var(--radius-full); font-weight: 600;` |
| `.btn-primary` | Ana aksiyon | `background: var(--primary); color: white;` |
| `.btn-outline` | İkincil | `background: transparent; border: 1.5px solid var(--border-light);` |
| `.btn-sm` | Küçük | `padding: 6px 14px; font-size: 13px;` |

Hover: `transform: translateY(-1px);` ve gölge/renk değişimi.

### 5.2. Kartlar (`.card`, `.stat-card`, `.activity-card` vb.)

- Temel Kart: `background: var(--card-bg); border-radius: var(--radius-lg); border: 1px solid var(--border-light); padding: var(--space-5); box-shadow: var(--shadow-sm);`.
- İstatistik Kartı (`.stat-card`): Hover'da `transform: translateY(-2px); box-shadow: var(--shadow-lg);`.

İçerik:

- `.stat-header`: Başlık ve ikon.
- `.stat-value`: Büyük sayı (`font-size: 34px; font-weight: 700;`).
- `.stat-footer`: Trend (`--success` veya `--error`) ve sparkline (mini grafik).

Sparkline Yapısı:

```html
<div class="sparkline">
  <div class="sparkline-bar" style="height: 12px;"></div>
  <div class="sparkline-bar" style="height: 16px;"></div>
  <!-- 5 adet bar -->
</div>
```

```css
.sparkline-bar { width: 4px; background: var(--primary); border-radius: 2px; opacity: 0.7; }
```

### 5.3. Tablolar (`.data-table`)

- Konteyner: `.table-container` (kenarlık, yuvarlak köşe, `overflow: hidden`).
- Tablo: `width: 100%; border-collapse: collapse;`.
- Başlıklar: `background: var(--bg-light); font-weight: 600; color: var(--text-gray);`.
- Hücreler: `padding: var(--space-4) var(--space-5); border-bottom: 1px solid var(--border-light);`.
- Mobil: `.table-container`'a `overflow-x: auto` ekle, tabloya `min-width: 800px` ver.

### 5.4. Rozetler (`.badge`)

- `display: inline-flex; padding: 4px 10px; border-radius: var(--radius-full); font-size: 12px; font-weight: 600;`.

Renk varyasyonları:

- `.badge-success`: `background: rgba(0, 168, 107, 0.1); color: var(--success);`
- `.badge-warning`: `background: rgba(255, 184, 0, 0.1); color: var(--warning);`
- `.badge-error`: `background: rgba(239, 68, 68, 0.1); color: var(--error);`

### 5.5. Form Elemanları

Input/Select: `.form-control`

- `width: 100%; padding: 12px 16px; border: 1.5px solid var(--border-light); border-radius: var(--radius-md);`
- Focus: `border-color: var(--primary); outline: none;`

Form Grid (`.form-grid`): `display: grid; grid-template-columns: repeat(2, 1fr); gap: var(--space-5);` (Mobilde 1 sütun).

### 5.6. Filtre Alanı (`.filter-section`)

- Arka plan beyaz, kart gibi.
- `.filter-row`: `display: flex; flex-wrap: wrap; gap: var(--space-4);`.
- `.filter-group`: İçinde `<label>` ve `<select>`.

### 5.7. Progres Barları

```css
.progress-bar { height: 6px; background: var(--border-light); border-radius: var(--radius-full); }
.progress-fill { height: 100%; background: var(--primary); border-radius: var(--radius-full); }
```

### 5.8. Aktivite Feed'i (`.activity-feed`)

- `.activity-item`: `display: flex; gap: var(--space-3); border-bottom: 1px solid var(--border-light);`.
- `.activity-icon`: Yuvarlak, açık mavi arka plan, ikon.

## 6. RESPONSIVE DAVRANIŞ KURALLARI

| Ekran Genişliği | Davranış |
| --- | --- |
| ≤ 1000px | Sidebar gizlenir. `.main-content margin-left: 0` olur. |
| ≤ 640px | `.stats-grid` 1 sütun. `.two-column` 1 sütun. `.header-right` alt satıra geçer. `.filter-row` sütun yönünde. Tablo yatay kaydırılır. |

## 7. ÖRNEK KULLANIM (AI'ye Verilecek Prompt)

Komut:

> "Yukarıdaki Otelturizm Ultra Profesyonel Panel Tasarım Sistemi kurallarına %100 uyarak, aşağıdaki özelliklere sahip bir Kullanıcı Yönetimi sayfası kodla:
> 
> Başlık: Kullanıcılar
> 
> 4 adet istatistik kartı (Toplam Kullanıcı, Aktif, Pasif, Yeni Kayıt)
> 
> Kullanıcı listesi tablosu (Ad, E-posta, Kayıt Tarihi, Durum, İşlemler)
> 
> Filtre alanı (Durum, Kayıt Tarihi)
> 
> Yeni Kullanıcı Ekle butonu (sağ üst)
> 
> Mobil uyumlu olacak.
> Sadece HTML ve CSS kodu üret."

---

## 🎯 BU PROMPT İLE NELER ELDE EDERSİNİZ?

1. **Tutarlılık:** Hangi AI aracını kullanırsanız kullanın, üretilen tüm sayfalar **aynı tasarım dilini** konuşur.
2. **Hız:** Yeni bir sayfa ihtiyacınız olduğunda, bu promptu yapıştırıp sadece sayfanın içeriğini tarif etmeniz yeterlidir.
3. **Bakım Kolaylığı:** Renk paletini veya yazı tipini değiştirmek isterseniz, tüm sayfalarda tek tek değişiklik yapmak yerine sadece `:root` bloğunu güncellemeniz yeterli olacaktır.
4. **Profesyonellik:** Bu doküman, bir yazılım ekibine verebileceğiniz **resmi bir tasarım sistemi kılavuzu** niteliğindedir.

Bu prompt, Otelturizm platformunun **tüm yönetim panellerinin (Admin, Partner, Firma) temelini** oluşturacak güçtedir. 🚀
