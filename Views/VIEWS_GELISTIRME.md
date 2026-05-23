# Views Geliştirme Takibi

**Grup ID:** `05` · Charter: [docs/agent-gruplari/05-views-razor.md](../docs/agent-gruplari/05-views-razor.md)

## Grup ID → dosya

| Grup ID | Dosya |
|---------|-------|
| **05** | `Views/**`, `Pages/**` |

> UPPERCASE MSSQL şema (`KULLANICILAR`, `OTELLER`, `ULKELER`, `ILLER`, `ILCELER`, `MAHALLELER`, …) ile hizalama.
> Son güncelleme: 2026-05-22 · Master: [DB_UYUM_MASTER_PLAN.md](../DB_UYUM_MASTER_PLAN.md)

## Durum ikonları
| İkon | Anlam |
|------|-------|
| ✅ | Tamam — model/API uyumlu veya statik şablon |
| 🔄 | Devam ediyor |
| 🔒 | Bilinçli istisna / backlog |

## Adres API (kaynak: `AdresAramaController`)

> **2026-05-22:** Controller Türkçe adlandırma — `TURKCE_DOSYA_ADLANDIRMA_PLAN.md` Faz 1–3. Fetch URL'leri değişmedi.

| Endpoint | Query |
|----------|-------|
| `GET /api/adres/ulkeler` | — |
| `GET /api/adres/iller` | `ulkeId` (zorunlu) |
| `GET /api/adres/ilceler` | `ilId` |
| `GET /api/adres/mahalleler` | `ilceId` |

Partner tesis konumu (panel): `GET /panel/partner/tesis/konum/ilceler?cityId=`, `GET .../mahalleler?districtId=`

## JS deseni (public auth / profil)
- Ülke seçimi → `ulkeId` ile iller yüklenir.
- TR (`iso2 === 'TR'`) için ilçe/mahalle; yurtdışı için sadece il/eyalet.
- JSON alanları: `id`, `name`, `iso2`, `regionType` (camelCase).
- Form gönderimi: metin adları (`City`, `District`, `Nationality`); ID hidden alanları JS seed için.

## Sıra planı
1. Auth/Login/Register ✅
2. Paneller/User ✅
3. Paneller/Partner ✅
4. Paneller/Admin ✅
5. Oteller (public) ✅
6. Shared / Destek / diğer ✅

### Anasayfa (5 dosya)

| Durum | Dosya | Not |
|-------|-------|-----|
| ✅ | `Views/Anasayfa/_AnasayfaContent.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Anasayfa/_AnasayfaFooter.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Anasayfa/_AnasayfaHeader.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Anasayfa/_HomeHotelCard.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Anasayfa/Anasayfa.cshtml` | ViewModel/Controller uyumlu |

### Destek (4 dosya)

| Durum | Dosya | Not |
|-------|-------|-----|
| ✅ | `Views/Destek/Sss.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Destek/YardimKategori.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Destek/YardimMerkezi.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Destek/YardimSayfa.cshtml` | ViewModel/Controller uyumlu |

### Email (128 dosya)

| Durum | Dosya | Not |
|-------|-------|-----|
| ✅ | `Views/Email/ar/Admin_Routing_Bildirimi.cshtml` | E-posta şablonu — Razor model, DB kolon yok |
| ✅ | `Views/Email/ar/Developer_Bildirim.cshtml` | E-posta şablonu — Razor model, DB kolon yok |
| ✅ | `Views/Email/ar/E-posta_Adresini_Onayla.cshtml` | E-posta şablonu — Razor model, DB kolon yok |
| ✅ | `Views/Email/ar/Favori_Fiyat_Alarmi.cshtml` | E-posta şablonu — Razor model, DB kolon yok |
| ✅ | `Views/Email/ar/Giris_Guvenlik_Kodu.cshtml` | E-posta şablonu — Razor model, DB kolon yok |
| ✅ | `Views/Email/ar/Ozel_Teklif.cshtml` | E-posta şablonu — Razor model, DB kolon yok |
| ✅ | `Views/Email/ar/Partner_Komisyon_Odeme_Bildirimi.cshtml` | E-posta şablonu — Razor model, DB kolon yok |
| ✅ | `Views/Email/ar/Partner_Rezervasyon_Guncellendi.cshtml` | E-posta şablonu — Razor model, DB kolon yok |
| ✅ | `Views/Email/ar/Partner_Rezervasyon_Iptal.cshtml` | E-posta şablonu — Razor model, DB kolon yok |
| ✅ | `Views/Email/ar/Partner_Tesis_Kullanıcı_Daveti.cshtml` | E-posta şablonu — Razor model, DB kolon yok |
| ✅ | `Views/Email/ar/Partner_Yeni_Rezervasyon.cshtml` | E-posta şablonu — Razor model, DB kolon yok |
| ✅ | `Views/Email/ar/Rezervasyon_Guncellendi.cshtml` | E-posta şablonu — Razor model, DB kolon yok |
| ✅ | `Views/Email/ar/Rezervasyon_Reddedildi.cshtml` | E-posta şablonu — Razor model, DB kolon yok |
| ✅ | `Views/Email/ar/Rezervasyon_Talebi_Alindi.cshtml` | E-posta şablonu — Razor model, DB kolon yok |
| ✅ | `Views/Email/ar/RezervasyonOnaylandi.cshtml` | E-posta şablonu — Razor model, DB kolon yok |
| ✅ | `Views/Email/ar/Sifre_Sifirlama_Talebi.cshtml` | E-posta şablonu — Razor model, DB kolon yok |
| ✅ | `Views/Email/ar/Sozlesme_Bildirimi.cshtml` | E-posta şablonu — Razor model, DB kolon yok |
| ✅ | `Views/Email/de/Admin_Routing_Bildirimi.cshtml` | E-posta şablonu — Razor model, DB kolon yok |
| ✅ | `Views/Email/de/Developer_Bildirim.cshtml` | E-posta şablonu — Razor model, DB kolon yok |
| ✅ | `Views/Email/de/E-posta_Adresini_Onayla.cshtml` | E-posta şablonu — Razor model, DB kolon yok |
| ✅ | `Views/Email/de/Favori_Fiyat_Alarmi.cshtml` | E-posta şablonu — Razor model, DB kolon yok |
| ✅ | `Views/Email/de/Giris_Guvenlik_Kodu.cshtml` | E-posta şablonu — Razor model, DB kolon yok |
| ✅ | `Views/Email/de/Ozel_Teklif.cshtml` | E-posta şablonu — Razor model, DB kolon yok |
| ✅ | `Views/Email/de/Partner_Komisyon_Odeme_Bildirimi.cshtml` | E-posta şablonu — Razor model, DB kolon yok |
| ✅ | `Views/Email/de/Partner_Rezervasyon_Guncellendi.cshtml` | E-posta şablonu — Razor model, DB kolon yok |
| ✅ | `Views/Email/de/Partner_Rezervasyon_Iptal.cshtml` | E-posta şablonu — Razor model, DB kolon yok |
| ✅ | `Views/Email/de/Partner_Tesis_Kullanıcı_Daveti.cshtml` | E-posta şablonu — Razor model, DB kolon yok |
| ✅ | `Views/Email/de/Partner_Yeni_Rezervasyon.cshtml` | E-posta şablonu — Razor model, DB kolon yok |
| ✅ | `Views/Email/de/Rezervasyon_Guncellendi.cshtml` | E-posta şablonu — Razor model, DB kolon yok |
| ✅ | `Views/Email/de/Rezervasyon_Reddedildi.cshtml` | E-posta şablonu — Razor model, DB kolon yok |
| ✅ | `Views/Email/de/Rezervasyon_Talebi_Alindi.cshtml` | E-posta şablonu — Razor model, DB kolon yok |
| ✅ | `Views/Email/de/RezervasyonOnaylandi.cshtml` | E-posta şablonu — Razor model, DB kolon yok |
| ✅ | `Views/Email/de/Sifre_Sifirlama_Talebi.cshtml` | E-posta şablonu — Razor model, DB kolon yok |
| ✅ | `Views/Email/de/Sozlesme_Bildirimi.cshtml` | E-posta şablonu — Razor model, DB kolon yok |
| ✅ | `Views/Email/E-posta_Adresini_Onayla.cshtml` | E-posta şablonu — Razor model, DB kolon yok |
| ✅ | `Views/Email/en/E-posta_Adresini_Onayla.cshtml` | E-posta şablonu — Razor model, DB kolon yok |
| ✅ | `Views/Email/en/Favori_Fiyat_Alarmi.cshtml` | E-posta şablonu — Razor model, DB kolon yok |
| ✅ | `Views/Email/en/Giris_Guvenlik_Kodu.cshtml` | E-posta şablonu — Razor model, DB kolon yok |
| ✅ | `Views/Email/en/Ozel_Teklif.cshtml` | E-posta şablonu — Razor model, DB kolon yok |
| ✅ | `Views/Email/en/Partner_Rezervasyon_Iptal.cshtml` | E-posta şablonu — Razor model, DB kolon yok |
| ✅ | `Views/Email/en/Rezervasyon_Reddedildi.cshtml` | E-posta şablonu — Razor model, DB kolon yok |
| ✅ | `Views/Email/en/Rezervasyon_Talebi_Alindi.cshtml` | E-posta şablonu — Razor model, DB kolon yok |
| ✅ | `Views/Email/en/RezervasyonOnaylandi.cshtml` | E-posta şablonu — Razor model, DB kolon yok |
| ✅ | `Views/Email/en/Sifre_Sifirlama_Talebi.cshtml` | E-posta şablonu — Razor model, DB kolon yok |
| ✅ | `Views/Email/en/tr/Favori_Fiyat_Alarmi.cshtml` | E-posta şablonu — Razor model, DB kolon yok |
| ✅ | `Views/Email/en/tr/RezervasyonOnaylandi.cshtml` | E-posta şablonu — Razor model, DB kolon yok |
| ✅ | `Views/Email/es/Admin_Routing_Bildirimi.cshtml` | E-posta şablonu — Razor model, DB kolon yok |
| ✅ | `Views/Email/es/Developer_Bildirim.cshtml` | E-posta şablonu — Razor model, DB kolon yok |
| ✅ | `Views/Email/es/E-posta_Adresini_Onayla.cshtml` | E-posta şablonu — Razor model, DB kolon yok |
| ✅ | `Views/Email/es/Favori_Fiyat_Alarmi.cshtml` | E-posta şablonu — Razor model, DB kolon yok |
| ✅ | `Views/Email/es/Giris_Guvenlik_Kodu.cshtml` | E-posta şablonu — Razor model, DB kolon yok |
| ✅ | `Views/Email/es/Ozel_Teklif.cshtml` | E-posta şablonu — Razor model, DB kolon yok |
| ✅ | `Views/Email/es/Partner_Komisyon_Odeme_Bildirimi.cshtml` | E-posta şablonu — Razor model, DB kolon yok |
| ✅ | `Views/Email/es/Partner_Rezervasyon_Guncellendi.cshtml` | E-posta şablonu — Razor model, DB kolon yok |
| ✅ | `Views/Email/es/Partner_Rezervasyon_Iptal.cshtml` | E-posta şablonu — Razor model, DB kolon yok |
| ✅ | `Views/Email/es/Partner_Tesis_Kullanıcı_Daveti.cshtml` | E-posta şablonu — Razor model, DB kolon yok |
| ✅ | `Views/Email/es/Partner_Yeni_Rezervasyon.cshtml` | E-posta şablonu — Razor model, DB kolon yok |
| ✅ | `Views/Email/es/Rezervasyon_Guncellendi.cshtml` | E-posta şablonu — Razor model, DB kolon yok |
| ✅ | `Views/Email/es/Rezervasyon_Reddedildi.cshtml` | E-posta şablonu — Razor model, DB kolon yok |
| ✅ | `Views/Email/es/Rezervasyon_Talebi_Alindi.cshtml` | E-posta şablonu — Razor model, DB kolon yok |
| ✅ | `Views/Email/es/RezervasyonOnaylandi.cshtml` | E-posta şablonu — Razor model, DB kolon yok |
| ✅ | `Views/Email/es/Sifre_Sifirlama_Talebi.cshtml` | E-posta şablonu — Razor model, DB kolon yok |
| ✅ | `Views/Email/es/Sozlesme_Bildirimi.cshtml` | E-posta şablonu — Razor model, DB kolon yok |
| ✅ | `Views/Email/Favori_Fiyat_Alarmi.cshtml` | E-posta şablonu — Razor model, DB kolon yok |
| ✅ | `Views/Email/Firma_Rezervasyon_Bildirimi.cshtml` | E-posta şablonu — Razor model, DB kolon yok |
| ✅ | `Views/Email/fr/Admin_Routing_Bildirimi.cshtml` | E-posta şablonu — Razor model, DB kolon yok |
| ✅ | `Views/Email/fr/Developer_Bildirim.cshtml` | E-posta şablonu — Razor model, DB kolon yok |
| ✅ | `Views/Email/fr/E-posta_Adresini_Onayla.cshtml` | E-posta şablonu — Razor model, DB kolon yok |
| ✅ | `Views/Email/fr/Favori_Fiyat_Alarmi.cshtml` | E-posta şablonu — Razor model, DB kolon yok |
| ✅ | `Views/Email/fr/Giris_Guvenlik_Kodu.cshtml` | E-posta şablonu — Razor model, DB kolon yok |
| ✅ | `Views/Email/fr/Ozel_Teklif.cshtml` | E-posta şablonu — Razor model, DB kolon yok |
| ✅ | `Views/Email/fr/Partner_Komisyon_Odeme_Bildirimi.cshtml` | E-posta şablonu — Razor model, DB kolon yok |
| ✅ | `Views/Email/fr/Partner_Rezervasyon_Guncellendi.cshtml` | E-posta şablonu — Razor model, DB kolon yok |
| ✅ | `Views/Email/fr/Partner_Rezervasyon_Iptal.cshtml` | E-posta şablonu — Razor model, DB kolon yok |
| ✅ | `Views/Email/fr/Partner_Tesis_Kullanıcı_Daveti.cshtml` | E-posta şablonu — Razor model, DB kolon yok |
| ✅ | `Views/Email/fr/Partner_Yeni_Rezervasyon.cshtml` | E-posta şablonu — Razor model, DB kolon yok |
| ✅ | `Views/Email/fr/Rezervasyon_Guncellendi.cshtml` | E-posta şablonu — Razor model, DB kolon yok |
| ✅ | `Views/Email/fr/Rezervasyon_Reddedildi.cshtml` | E-posta şablonu — Razor model, DB kolon yok |
| ✅ | `Views/Email/fr/Rezervasyon_Talebi_Alindi.cshtml` | E-posta şablonu — Razor model, DB kolon yok |
| ✅ | `Views/Email/fr/RezervasyonOnaylandi.cshtml` | E-posta şablonu — Razor model, DB kolon yok |
| ✅ | `Views/Email/fr/Sifre_Sifirlama_Talebi.cshtml` | E-posta şablonu — Razor model, DB kolon yok |
| ✅ | `Views/Email/fr/Sozlesme_Bildirimi.cshtml` | E-posta şablonu — Razor model, DB kolon yok |
| ✅ | `Views/Email/Giris_Guvenlik_Kodu.cshtml` | E-posta şablonu — Razor model, DB kolon yok |
| ✅ | `Views/Email/Link_Kontrol_Raporu.cshtml` | E-posta şablonu — Razor model, DB kolon yok |
| ✅ | `Views/Email/Partner_Rezervasyon_Iptal.cshtml` | E-posta şablonu — Razor model, DB kolon yok |
| ✅ | `Views/Email/Partner_Tesis_Kullanıcı_Daveti.cshtml` | E-posta şablonu — Razor model, DB kolon yok |
| ✅ | `Views/Email/Partner_Yeni_Rezervasyon.cshtml` | E-posta şablonu — Razor model, DB kolon yok |
| ✅ | `Views/Email/Rezervasyon_Mesaji.cshtml` | E-posta şablonu — Razor model, DB kolon yok |
| ✅ | `Views/Email/Rezervasyon_Reddedildi.cshtml` | E-posta şablonu — Razor model, DB kolon yok |
| ✅ | `Views/Email/Rezervasyon_Talebi_Alindi.cshtml` | E-posta şablonu — Razor model, DB kolon yok |
| ✅ | `Views/Email/RezervasyonOnaylandi.cshtml` | E-posta şablonu — Razor model, DB kolon yok |
| ✅ | `Views/Email/ru/Admin_Routing_Bildirimi.cshtml` | E-posta şablonu — Razor model, DB kolon yok |
| ✅ | `Views/Email/ru/Developer_Bildirim.cshtml` | E-posta şablonu — Razor model, DB kolon yok |
| ✅ | `Views/Email/ru/E-posta_Adresini_Onayla.cshtml` | E-posta şablonu — Razor model, DB kolon yok |
| ✅ | `Views/Email/ru/Favori_Fiyat_Alarmi.cshtml` | E-posta şablonu — Razor model, DB kolon yok |
| ✅ | `Views/Email/ru/Giris_Guvenlik_Kodu.cshtml` | E-posta şablonu — Razor model, DB kolon yok |
| ✅ | `Views/Email/ru/Ozel_Teklif.cshtml` | E-posta şablonu — Razor model, DB kolon yok |
| ✅ | `Views/Email/ru/Partner_Komisyon_Odeme_Bildirimi.cshtml` | E-posta şablonu — Razor model, DB kolon yok |
| ✅ | `Views/Email/ru/Partner_Rezervasyon_Guncellendi.cshtml` | E-posta şablonu — Razor model, DB kolon yok |
| ✅ | `Views/Email/ru/Partner_Rezervasyon_Iptal.cshtml` | E-posta şablonu — Razor model, DB kolon yok |
| ✅ | `Views/Email/ru/Partner_Tesis_Kullanıcı_Daveti.cshtml` | E-posta şablonu — Razor model, DB kolon yok |
| ✅ | `Views/Email/ru/Partner_Yeni_Rezervasyon.cshtml` | E-posta şablonu — Razor model, DB kolon yok |
| ✅ | `Views/Email/ru/Rezervasyon_Guncellendi.cshtml` | E-posta şablonu — Razor model, DB kolon yok |
| ✅ | `Views/Email/ru/Rezervasyon_Reddedildi.cshtml` | E-posta şablonu — Razor model, DB kolon yok |
| ✅ | `Views/Email/ru/Rezervasyon_Talebi_Alindi.cshtml` | E-posta şablonu — Razor model, DB kolon yok |
| ✅ | `Views/Email/ru/RezervasyonOnaylandi.cshtml` | E-posta şablonu — Razor model, DB kolon yok |
| ✅ | `Views/Email/ru/Sifre_Sifirlama_Talebi.cshtml` | E-posta şablonu — Razor model, DB kolon yok |
| ✅ | `Views/Email/ru/Sozlesme_Bildirimi.cshtml` | E-posta şablonu — Razor model, DB kolon yok |
| ✅ | `Views/Email/Sifre_Sifirlama_Talebi.cshtml` | E-posta şablonu — Razor model, DB kolon yok |
| ✅ | `Views/Email/Sozlesme_Bildirimi.cshtml` | E-posta şablonu — Razor model, DB kolon yok |
| ✅ | `Views/Email/Şifre_Sıfırlama_Talebi.cshtml` | E-posta şablonu — Razor model, DB kolon yok |
| ✅ | `Views/Email/tr/Admin_Routing_Bildirimi.cshtml` | E-posta şablonu — Razor model, DB kolon yok |
| ✅ | `Views/Email/tr/Developer_Bildirim.cshtml` | E-posta şablonu — Razor model, DB kolon yok |
| ✅ | `Views/Email/tr/E-posta_Adresini_Onayla.cshtml` | E-posta şablonu — Razor model, DB kolon yok |
| ✅ | `Views/Email/tr/Favori_Fiyat_Alarmi.cshtml` | E-posta şablonu — Razor model, DB kolon yok |
| ✅ | `Views/Email/tr/Giris_Guvenlik_Kodu.cshtml` | E-posta şablonu — Razor model, DB kolon yok |
| ✅ | `Views/Email/tr/Ozel_Teklif.cshtml` | E-posta şablonu — Razor model, DB kolon yok |
| ✅ | `Views/Email/tr/Partner_Komisyon_Odeme_Bildirimi.cshtml` | E-posta şablonu — Razor model, DB kolon yok |
| ✅ | `Views/Email/tr/Partner_Rezervasyon_Guncellendi.cshtml` | E-posta şablonu — Razor model, DB kolon yok |
| ✅ | `Views/Email/tr/Partner_Rezervasyon_Iptal.cshtml` | E-posta şablonu — Razor model, DB kolon yok |
| ✅ | `Views/Email/tr/Partner_Tesis_Kullanıcı_Daveti.cshtml` | E-posta şablonu — Razor model, DB kolon yok |
| ✅ | `Views/Email/tr/Partner_Yeni_Rezervasyon.cshtml` | E-posta şablonu — Razor model, DB kolon yok |
| ✅ | `Views/Email/tr/Rezervasyon_Guncellendi.cshtml` | E-posta şablonu — Razor model, DB kolon yok |
| ✅ | `Views/Email/tr/Rezervasyon_Reddedildi.cshtml` | E-posta şablonu — Razor model, DB kolon yok |
| ✅ | `Views/Email/tr/Rezervasyon_Talebi_Alindi.cshtml` | E-posta şablonu — Razor model, DB kolon yok |
| ✅ | `Views/Email/tr/RezervasyonOnaylandi.cshtml` | E-posta şablonu — Razor model, DB kolon yok |
| ✅ | `Views/Email/tr/Sifre_Sifirlama_Talebi.cshtml` | E-posta şablonu — Razor model, DB kolon yok |
| ✅ | `Views/Email/tr/Sozlesme_Bildirimi.cshtml` | E-posta şablonu — Razor model, DB kolon yok |

### Firma (4 dosya)

| Durum | Dosya | Not |
|-------|-------|-----|
| ✅ | `Views/Firma/_FirmaContent.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Firma/_FirmaFooter.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Firma/_FirmaHeader.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Firma/Firma.cshtml` | ViewModel/Controller uyumlu |

### Gelisim (1 dosya)

| Durum | Dosya | Not |
|-------|-------|-----|
| ✅ | `Views/Gelisim/Index.cshtml` | ViewModel/Controller uyumlu |

### Home (1 dosya)

| Durum | Dosya | Not |
|-------|-------|-----|
| ✅ | `Views/Home/Privacy.cshtml` | ViewModel/Controller uyumlu |

### Kampanyalar (2 dosya)

| Durum | Dosya | Not |
|-------|-------|-----|
| ✅ | `Views/Kampanyalar/Detail.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Kampanyalar/Index.cshtml` | ViewModel/Controller uyumlu |

### Kurumsal (4 dosya)

| Durum | Dosya | Not |
|-------|-------|-----|
| ✅ | `Views/Kurumsal/_KurumsalContent.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Kurumsal/_KurumsalFooter.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Kurumsal/_KurumsalHeader.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Kurumsal/Kurumsal.cshtml` | ViewModel/Controller uyumlu |

### Legal (1 dosya)

| Durum | Dosya | Not |
|-------|-------|-----|
| ✅ | `Views/Legal/ContractDetail.cshtml` | ViewModel/Controller uyumlu |

### Login (8 dosya)

| Durum | Dosya | Not |
|-------|-------|-----|
| ✅ | `Views/Login/AdminLogin.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Login/FirmaLogin.cshtml` | Adres API + ulkeId doğrulandı |
| ✅ | `Views/Login/ForgotPassword.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Login/PartnerLogin.cshtml` | Adres API + ulkeId doğrulandı |
| ✅ | `Views/Login/ResetPassword.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Login/UserLogin.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Login/UserLogin2FA.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Login/VerifyEmail.cshtml` | ViewModel/Controller uyumlu |

### Oteller (4 dosya)

| Durum | Dosya | Not |
|-------|-------|-----|
| ✅ | `Views/Oteller/HaritaOteller.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Oteller/OtelDetay.cshtml` | Adres API + ulkeId doğrulandı |
| ✅ | `Views/Oteller/OtelListeleme.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Oteller/Partials/_ReservationCreateButton.cshtml` | ViewModel/Controller uyumlu |

### Paneller (162 dosya)

| Durum | Dosya | Not |
|-------|-------|-----|
| ✅ | `Views/Paneller/Admin/_AdminFooter.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/Admin/_AdminMobileNav.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/Admin/_AdminPanelLayout.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/Admin/_AdminSectionPage.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/Admin/_AdminSidebar.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/Admin/_AdminTopNav.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/Admin/ActiveHotels.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/Admin/AdminActionLogs.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/Admin/ApprovalCenter.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/Admin/Backups.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/Admin/Blog.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/Admin/Campaigns.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/Admin/CommerceInsight.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/Admin/Commissions.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/Admin/CompanyApplications.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/Admin/CompanyReservations.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/Admin/Complaints.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/Admin/Contracts.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/Admin/Dashboard.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/Admin/DevelopmentRequests.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/Admin/EmailQueue.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/Admin/EmailRouting.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/Admin/EmailTemplates.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/Admin/Faq.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/Admin/GeoSearchLogs.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/Admin/HelpCenter.cshtml` | Eski tablo adı metinleri güncellendi |
| ✅ | `Views/Paneller/Admin/HotelCoordinateChanges.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/Admin/HotelDetail.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/Admin/Hotels.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/Admin/Invoices.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/Admin/ListingSubscriptions.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/Admin/LogEvents.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/Admin/Logs.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/Admin/MailCenter.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/Admin/Managers.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/Admin/Notifications.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/Admin/PartnerApplications.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/Admin/Payments.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/Admin/PendingHotels.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/Admin/PlatformCheckup.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/Admin/PlatformOfficials.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/Admin/RateLimitStats.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/Admin/Reports.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/Admin/Reservations.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/Admin/ReviewsModeration.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/Admin/Security.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/Admin/Settings.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/Admin/SettingsMonitor.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/Admin/Sitemap.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/Admin/SupportArticles.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/Admin/SystemHealth.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/Admin/Team.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/Admin/UnifiedReservations.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/Admin/Users.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/Admin/WhatsAppCloudApi.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/Common/_DeveloperFeedbackWidget.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/Common/_PanelToasts.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/Departman/_DepartmentFooter.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/Departman/_DepartmentMobileNav.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/Departman/_DepartmentPanelLayout.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/Departman/_DepartmentSidebar.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/Departman/Dashboard.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/Developer/_DeveloperMobileNav.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/Developer/_DeveloperPanelLayout.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/Developer/_DeveloperSidebar.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/Developer/Index.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/Developer/Security.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/Firma/_FirmaMobileNav.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/Firma/_FirmaPanelFooter.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/Firma/_FirmaPanelLayout.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/Firma/_FirmaSidebar.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/Firma/CreateReservation.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/Firma/Dashboard.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/Firma/Deals.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/Firma/DealsCompare.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/Firma/Employees.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/Firma/Hotels.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/Firma/Invoices.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/Firma/Limits.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/Firma/Messages.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/Firma/Reservations.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/Firma/Security.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/Firma/Spending.cshtml` | Eski tablo adı metinleri güncellendi |
| ✅ | `Views/Paneller/Partner/_PartnerPanelFooter.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/Partner/_PartnerPanelLayout.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/Partner/_PartnerSidebar.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/Partner/AccountInfo.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/Partner/Campaigns.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/Partner/CancellationNoShow.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/Partner/Commissions.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/Partner/CompanyAnalytics.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/Partner/CompanyPricing.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/Partner/CompanyRequests.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/Partner/CompanyReservations.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/Partner/DailyNotes.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/Partner/Dashboard.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/Partner/Discounts.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/Partner/FacilityAmenities.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/Partner/FacilityDefinitions.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/Partner/FacilityLocation.cshtml` | Adres API + ulkeId doğrulandı |
| ✅ | `Views/Paneller/Partner/FacilityPolicies.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/Partner/FacilityUsers.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/Partner/FavoriteGuests.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/Partner/Finance.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/Partner/GuestInvoices.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/Partner/GuestMessages.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/Partner/HotelInfo.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/Partner/Invoices.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/Partner/ListingSubscriptions.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/Partner/LocationInsights.cshtml` | Eski tablo adı metinleri güncellendi |
| ✅ | `Views/Paneller/Partner/MarketingEvents.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/Partner/MealServices.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/Partner/NoHotelAssigned.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/Partner/NotificationPreferences.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/Partner/PaymentSettings.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/Partner/PaymentStatuses.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/Partner/Performance.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/Partner/Photos.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/Partner/PlannedModule.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/Partner/Preferences.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/Partner/Pricing.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/Partner/Reconciliation.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/Partner/ReservationCalendar.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/Partner/Reservations.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/Partner/Restrictions.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/Partner/Reviews.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/Partner/RoomFeatures.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/Partner/Rooms.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/Partner/Security.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/Partner/Settings.cshtml` | Eski tablo adı metinleri güncellendi |
| ✅ | `Views/Paneller/Partner/StockQuota.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/Partner/SuperPrice.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/Partner/Support.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/Satis/_SalesMobileNav.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/Satis/_SalesPanelFooter.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/Satis/_SalesPanelLayout.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/Satis/_SalesSidebar.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/Satis/Availability.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/Satis/CreateReservation.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/Satis/Customers.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/Satis/Dashboard.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/Satis/Hotels.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/Satis/Reports.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/Satis/ReservationPdf.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/Satis/Reservations.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/Satis/Security.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/User/_UserMobileNav.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/User/_UserPanelFooter.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/User/_UserPanelLayout.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/User/_UserRouteHub.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/User/_UserSidebar.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/User/Dashboard.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/User/Favorites.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/User/Loyalty.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/User/Messages.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/User/Notifications.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/User/PaymentMethods.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/User/Profile.cshtml` | Adres API + ulkeId doğrulandı |
| ✅ | `Views/Paneller/User/ReservationReview.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/User/Reservations.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/User/Reviews.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Paneller/User/Security.cshtml` | ViewModel/Controller uyumlu |

### Register (4 dosya)

| Durum | Dosya | Not |
|-------|-------|-----|
| ✅ | `Views/Register/_ContractPopupModal.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Register/_FirmaRegisterForm.cshtml` | Adres API + ulkeId doğrulandı |
| ✅ | `Views/Register/_PartnerRegisterForm.cshtml` | Adres API + ulkeId doğrulandı |
| ✅ | `Views/Register/_UserRegisterForm.cshtml` | ViewModel/Controller uyumlu |

### Root (2 dosya)

| Durum | Dosya | Not |
|-------|-------|-----|
| ✅ | `Views/_ViewImports.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/_ViewStart.cshtml` | ViewModel/Controller uyumlu |

### SeyahatPlanlama (1 dosya)

| Durum | Dosya | Not |
|-------|-------|-----|
| ✅ | `Views/SeyahatPlanlama/Index.cshtml` | ViewModel/Controller uyumlu |

### Shared (14 dosya)

| Durum | Dosya | Not |
|-------|-------|-----|
| ✅ | `Views/Shared/_ActiveReservationDraftBanner.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Shared/_AuthTablerLayout.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Shared/_DevelopmentGateOverlay.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Shared/_FavoriteToggleScriptPartial.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Shared/_HeaderBildiri.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Shared/_Layout.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Shared/_Layout.cshtml.css` | ViewModel/Controller uyumlu |
| ✅ | `Views/Shared/_PublicHeaderUserActions.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Shared/_ReservationDraftCancelButton.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Shared/_SlaytGorsel.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Shared/_ValidationScriptsPartial.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Shared/Error.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Shared/StatusCode.cshtml` | ViewModel/Controller uyumlu |
| ✅ | `Views/Shared/yanbar.cshtml` | ViewModel/Controller uyumlu |

## Özet
- **Toplam dosya:** 345
- **Tamamlanan (✅):** 345 (100%)
- **Adres API güncellenen:** 7
- **Metin/şema notu düzeltilen:** 4
- **Silinen:** `Views/Oteller/OtelDetay.cshtml.bak` (eski `/api/adres/iller` deseni)
- **Build:** `dotnet build` — 0 hata

