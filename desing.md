# OTELTURIZM TASARIM SISTEMI v4.1

Bu dokuman, OTELTURIZM projesinde ortak tasarim dilini merkezden yonetmek icin hazirlanmistir.

## KRITIK NOT (PROJE SOZLESMESI): Mobil CSS ve Masaustu CSS AYRI

- Mobil tasarim/override degisiklikleri **asla** ana masaustu CSS dosyasina yazilmaz.
- Her sayfa icin mobil override katmani `*.mobile.css` dosyasidir (ornegin: `otel-detay.css` + `otel-detay.mobile.css`).
- Mobil CSS dosyasi temel CSS'i `@import` ile ceker ve sadece mobilde gerekli override'lari icerir.
- Kural: **Masaustu bozulmasin diye** mobil gelistirme talebi geldiyse degisiklik hedefi varsayilan olarak `*.mobile.css` olmalidir.

## NOT: "Gecisli Kart" (Cok hafif yesil)

Kullanici "gecisli kart" istediginde uygulanacak standart kart dili:

- Kart arka plani: cok hafif yesil -> beyaz gecis
- Border: yesil ton (cok dusuk opaklik)
- Golge: yesil ton (cok dusuk opaklik)

Referans stil (masaustu):

```css
border-color: rgba(16, 185, 129, 0.22);
background: linear-gradient(180deg, rgba(236, 253, 245, 0.55) 0%, rgba(255, 255, 255, 0.98) 68%);
box-shadow: 0 10px 24px rgba(16, 185, 129, 0.06);
```

## 1) CSS Degiskenleri (Root)

Asagidaki degiskenleri ana CSS dosyanizin basina, `:root` blogu icine ekleyin.

| Degisken Adi | Varsayilan Deger | Ne ise yarar? | Nasil guncellenir? |
| --- | --- | --- | --- |
| `--font-main` | `'Satoshi', sans-serif` | Platformun ana yazi tipi | Font degistirmek icin `'Inter'` veya `'Poppins'` yazin |
| `--h1-size` | `clamp(1.8rem, 4vw, 2.4rem)` | Ana baslik boyutu | Min/max degerlerini degistirin |
| `--h2-size` | `clamp(1.4rem, 3vw, 1.8rem)` | Ikincil baslik boyutu | Min/max degerlerini ayarlayin |
| `--h3-size` | `clamp(1.1rem, 2.5vw, 1.4rem)` | Ucuncul baslik boyutu | Min/max degerlerini ayarlayin |
| `--body-size` | `0.95rem` | Govde metin boyutu | `1rem` yaparak buyutebilirsiniz |
| `--price-size` | `clamp(1.6rem, 3vw, 2rem)` | Fiyat yazisi boyutu | Vurguyu artirmak icin ust degeri buyutun |
| `--line-height-base` | `1.5` | Satir yuksekligi | `1.6` daha ferah, `1.4` daha sik |
| `--letter-spacing-tight` | `-0.01em` | Basliklarda harf araligi | `-0.02em` daha sik, `0` normal |
| `--primary` | `#003B95` | Ana kurumsal renk | Yeni HEX kodunu yazin |
| `--primary-dark` | `#002A6B` | Hover icin koyu ton | `--primary` ile uyumlu daha koyu ton secin |
| `--secondary` | `#FF385C` | Vurgu/indirim rengi | Yeni HEX kodunu yazin |
| `--success` | `#00A86B` | Basari/onay rengi | Yeni HEX kodunu yazin |
| `--gold` | `#FFD700` | Premium/altin vurgu | Yeni HEX kodunu yazin |
| `--bg-light` | `#F8FAFC` | Sayfa arka plani | Daha koyu arka plan icin degeri degistirin |
| `--card-bg` | `#FFFFFF` | Kart arka plani | Genelde beyaz kalir |
| `--text-dark` | `#1A1F26` | Ana metin rengi | Siyah yerine bu ton onerilir |
| `--text-gray` | `#4a4a4a` | Aciklama metinleri | Ihtiyaca gore daha soguk gri secilebilir |
| `--text-light` | `#94A3B8` | Ikincil metinler | Gerekirse bir ton koyulastirin |
| `--border-light` | `#E2E8F0` | Kart/input kenarlari | `#CBD5E1` daha belirgin gorunur |
| `--space-1 ... --space-6` | `4px ... 24px` | Standart bosluk sistemi | Degerleri buyuterek daha ferah yapi |
| `--radius-xl` | `20px` | Buyuk kart radius degeri | `24px` daha yumusak gorunum |
| `--radius-full` | `9999px` | Buton/rozet tam yuvarlaklik | Degistirilmesi onerilmez |
| `--shadow-lg` | `0 15px 30px -10px rgba(0,59,149,0.1)` | Kart hover golgesi | Opaklik degeri ile oynayin |

## 2) Bilesen Stilleri

Asagidaki siniflar, root degiskenlerine bagli calisir.

### 2.1 Butonlar (`.btn`, `.btn-outline`, `.btn-sm`)

```css
.btn {
  display: inline-flex; align-items: center; justify-content: center;
  gap: 8px; padding: 10px 18px; border-radius: var(--radius-full);
  font-weight: 600; font-size: 14px; text-decoration: none;
  transition: all 0.2s; border: none; cursor: pointer;
  font-family: var(--font-main); background: var(--primary); color: white;
}
.btn:hover { background: var(--primary-dark); transform: translateY(-1px); }

.btn-outline {
  background: transparent; border: 1.5px solid var(--border-light); color: var(--text-dark);
}
.btn-outline:hover { border-color: var(--primary); color: var(--primary); background: transparent; }

.btn-sm { padding: 6px 14px; font-size: 13px; }
```

### 2.2 Kartlar (`.card`)

```css
.card {
  background: var(--card-bg); border-radius: var(--radius-xl);
  padding: var(--space-5); box-shadow: var(--shadow-lg);
  border: 1px solid var(--border-light);
}
```

### 2.3 Form Elemanlari (`.form-group`, `.form-control`)

```css
.form-group { margin-bottom: var(--space-3); }
.form-group label {
  display: block; font-weight: 600; font-size: 13px;
  margin-bottom: 4px; color: var(--text-dark);
}
.form-control {
  width: 100%; padding: 10px 14px; border: 1.5px solid var(--border-light);
  border-radius: var(--radius-md); font-family: var(--font-main);
  font-size: 14px; transition: border 0.2s;
}
.form-control:focus { outline: none; border-color: var(--primary); }
```

### 2.4 Rozetler (`.badge`)

```css
.badge {
  display: inline-flex; align-items: center; gap: 4px;
  background: var(--success); color: white; padding: 4px 10px;
  border-radius: var(--radius-full); font-size: 12px; font-weight: 600;
}
.badge.secondary { background: var(--secondary); }
.badge.gold { background: var(--gold); color: var(--text-dark); }
```

### 2.5 Ozellik Listesi (`.feature-list`, `.feature-item`)

```css
.feature-list { display: flex; flex-wrap: wrap; gap: var(--space-3); }
.feature-item {
  display: flex; align-items: center; gap: 6px; background: var(--bg-light);
  padding: 4px 12px; border-radius: var(--radius-full); font-size: 13px;
  color: var(--text-dark);
}
.feature-item i { color: var(--success); font-size: 14px; }
```

### 2.6 Fiyat Blogu (`.price-block`, `.price-value`, `.price-note`)

```css
.price-block { display: flex; align-items: baseline; gap: 6px; }
.price-value { font-size: var(--price-size); font-weight: 700; color: var(--primary); }
.price-note { font-size: 13px; color: var(--text-gray); }
```

### 2.7 Mobil Kart ve Bosluk Standarti (Zorunlu)

Mobilde kartlarin ic ice gorunmesini engellemek ve tutarlilik saglamak icin asagidaki kural zorunludur:

```css
/* Mobilde kartli yapilar default olarak 2 sutun */
@media (max-width: 640px) {
  .steps-grid,
  .tiers-grid,
  .benefits-grid,
  .process-cards,
  .hero-points,
  .comparison-kpis,
  .hero-actions,
  .cta-actions {
    grid-template-columns: repeat(2, minmax(0, 1fr));
    gap: 12px; /* standart mobil kart araligi */
  }
}

/* Genel kartlar icin standart bosluk */
.section {
  padding: 28px 0; /* mobilde */
}
@media (min-width: 641px) {
  .section { padding: 44px 0; }
}
```

Not:
- Yogun metinli ozel bloklarda (ornek senaryo gibi) okunabilirlik icin tek sutun kullanilabilir.
- Kartlar arasinda minimum `12px` bosluk zorunludur; daha dusuk deger kullanilmaz.
- Kart grubu bittikten sonra gelen aksiyon butonlarinin (`.hero-actions`, `.cta-actions`) ust boslugu, kartlarin kendi `gap` degeri ile ayni olmalidir (ornek: `12px`).

## 3) Projeye Entegrasyon Adimlari

1. `assets/css/otelturizm-design-system.css` dosyasini olusturun.
2. Root degiskenlerini dosyanin en basina ekleyin.
3. Bilesen stillerini ayni dosyaya yapistirin.
4. Tum `.html` ve `.cshtml` dosyalarinin `<head>` alanina su satiri ekleyin:

```html
<link rel="stylesheet" href="/assets/css/otelturizm-design-system.css">
```

5. Satoshi fontu icin `<head>` alanina su satiri ekleyin:

```html
<link href="https://api.fontshare.com/v2/css?f[]=satoshi@400,500,700,900&display=swap" rel="stylesheet">
```

## 4) AI Context Update

Sistem talimati:

> Bundan sonra Otelturizm icin kod uretirken asagidaki tasarim sistemini kullan:
> - Ana Font: Satoshi (Fontshare CDN)
> - Renk Paleti: `#003B95` (Primary), `#FF385C` (Accent), `#00A86B` (Success), `#4a4a4a` (Text)
> - Tipografi: Basliklarda `clamp()` kullan, paragraf rengi `#4a4a4a`, harf araligi `-0.01em`
> - Bilesenler: Butonlar yuvarlak (`9999px`), kartlar `20px` border-radius, hover `translateY(-1px)`
> - Bosluk Sistemi: `--space-1` (`4px`) ile `--space-6` (`24px`) arasi degiskenleri kullan
> - Mobil: `640px` altinda grid'leri tek sutuna dusur, sayfa altina `padding-bottom: 80px` ekle

## 5) Hizli Guncelleme Ornegi

Senaryo: Platformun ana rengini kirmizi yapmak istiyorum.

Yapilacak degisiklik:

```css
--primary: #FF385C; /* Eskiden #003B95 idi */
```

Sonuc: Tum butonlar, vurgular ve fiyat yazilari aninda yeni ana renge gecer.
