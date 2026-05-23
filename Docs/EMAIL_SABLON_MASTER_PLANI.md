# E-posta Şablon Master Planı

**Orkestra:** `H14_email_ork` · **Wave:** `Wave-IX-i18n`

---

## Mevcut

| Bileşen | Durum |
|---------|--------|
| `Views/Email/{tr,en,de,fr,es,ru,ar}/*.cshtml` | ✅ çok sayıda şablon |
| `EmailTemplateService` | ⚠️ `SupportedLanguages = [tr, en]` only |
| `EmailQueueService` | ✅ token replace + UTM |
| Admin `EmailTemplates.cshtml` | ✅ binding UI |
| Master layout | ❌ her şablon tekrar HTML |

---

## Hedef

1. **`Views/Email/_EmailMaster.cshtml`** — responsive, marka, footer yasal, `dir=rtl` ar
2. **Tüm diller** EmailTemplateService: tr, en, de, fr, es, ru, ar
3. **Şablon envanteri** — 15 core × 7 dil = 105 (eksikler üretilir)
4. **DB seed** — `EMAIL_SABLONLARI` binding idempotent (varsa tablo)

---

## Core şablonlar (15)

| Kod | Kullanım |
|-----|---------|
| `rezervasyon_onaylandi` | Misafir onay |
| `rezervasyon_talebi_alindi` | Beklemede |
| `rezervasyon_reddedildi` | Red |
| `giris_guvenlik_kodu` | 2FA |
| `eposta_onay` | Kayıt |
| `sifre_sifirlama` | Reset |
| `partner_yeni_rezervasyon` | Partner |
| `partner_rezervasyon_iptal` | Partner |
| `partner_komisyon_odeme` | Finans |
| `favori_fiyat_alarmi` | U1 özellik |
| `fatura_yuklendi` | Fatura T435 |
| `sozlesme_bildirimi` | Yasal |
| `rezervasyon_guncellendi` | Değişiklik |
| `partner_rezervasyon_guncellendi` | Partner |
| `kampanya_duyuru` | Pazarlama |

---

## Şablon tasarım standardı

- Max width 600px, inline CSS
- Primary CTA button 44px height
- `{{GuestName}}`, `{{HotelName}}`, `{{CheckIn}}`, `{{TotalPrice}}`, `{{CtaUrl}}`
- Preheader text
- Plain-text alternatif (Faz 2)

---

## Görevler

| ID | Görev |
|----|--------|
| T452 | `_EmailMaster.cshtml` + partial |
| T453 | `EmailTemplateService` 7 dil + fallback chain |
| T454 | Eksik şablonları tamamla (grep diff tr vs de/fr/es/ru/ar) |
| T455 | `tools/Email/Render-EmailTemplates.ps1` tüm diller |
| T456 | Admin preview by language |
| T457 | Seed + queue test rezervasyon_onaylandi |

---

## Örnek path

`Views/Email/de/RezervasyonOnaylandi.cshtml` → layout → tokens

---

*UTM: mevcut EmailQueueService korunur.*
