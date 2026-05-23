# Kurumsal iş akışı — tam platform (orkestra ataması)

Tarih: **2026-05-22**  
Master CTO + `KALAN_ORKESTRA_PLANI.md`

---

## A. Yerel DB (öncelik 0)

| ID | Görev | Orkestratör | Durum |
|----|-------|-------------|-------|
| DB-01 | Tüm migration: tablo → constraints → veri + dated root | db-ork | 🔄 |
| DB-02 | `20260524_seed_koordinat_turkiye.sql` (il 5301, ilçe 973; mahalle seed yok) | db-ork | ✅ |
| DB-03 | `20260523_seed_orkestra_demo_oteller.sql` + `fix_orkestra_demo_yayin_onay` | db-ork | ✅ |
| DB-05 | `20260523_seed_istanbul_10_ilce_oteller.sql` (10 ilçe, filtre test) | db-ork | ✅ |
| DB-04 | `20260522_seed_admin_yetkiler.sql` + `20260523_seed_admin_demo_kullanici.sql` | db-ork / H3 T330 | ✅ |

---

## B. Kimlik (giriş / çıkış)

| Akış | Route / sayfa | Grup | CTO |
|------|---------------|------|-----|
| Kullanıcı giriş/çıkış | Auth, User panel | 07 auth | Security + FE |
| Partner giriş | PartnerLogin | 07, 09 | FE |
| Firma giriş | FirmaLogin | 07, 10 | FE |
| Admin giriş | Admin login | 07, 08 | FE |
| Satış giriş | Sales panel | 07, **D2 fe-satis** | FE |
| 2FA / telefon | PhoneVerification | 07 | Security |

---

## C. Rezervasyon yaşam döngüsü

| Adım | Aktör | Sayfa / servis | Orkestratör |
|------|-------|----------------|-------------|
| 1. Otel/oda/tarih seçimi | Kullanıcı / firma / satış | OtelDetay, CreateReservation | fe-otel, fe-firma, fe-satis |
| 2. Taslak oluştur | API | ReservationDraftService | 03 services |
| 3. Ödeme / onay bekliyor | Sistem | Rezervasyon durumları | 03 |
| 4. Partner onay / red | Partner | Reservations, CompanyReservations | fe-partner |
| 5. İptal (kullanıcı) | User | Reservations | fe-user |
| 6. İptal / no-show (partner) | Partner | CancellationNoShow | fe-partner |
| 7. Ücret / fiyat güncelleme | Partner | Pricing, SuperPrice, Discounts | fe-partner |
| 8. Komisyon hesaplama | Admin/Partner | Commissions, Finance | fe-admin, fe-partner ✅ (partner tablo + ödendi işaretle) |
| 9. Fatura (kullanıcı yükleme/görme) | User/Partner | Invoices, GuestInvoices | fe-user, fe-partner |
| 10. Bildirim | Tüm | HeaderBildiri, email queue | 07, 08 |

**Model bağlama:** `MISAFIR_*_ID`, `GuestUlkeId`… → ReservationDraft + PublicReservation INSERT + KULLANICILAR ULKE_ID (T109 ✅)

---

## D. Firma paneli (B2B)

| Sayfa | İş | Orkestratör |
|-------|-----|-------------|
| Hotels | Firma anlaşmalı oteller | fe-firma |
| Deals / DealsCompare | Firma fiyatları | fe-firma |
| CreateReservation | Personelli / personelsiz rezervasyon | fe-firma |
| Employees | Personel yönetimi | fe-firma |
| Limits | Limitler | fe-firma |
| Reservations | Liste | fe-firma |

---

## E. Partner paneli (eksiksiz)

Kampanyalar, indirimler, fiyat takvimi, stok kota, komisyon, oda özellikleri, meal services, marketing — **47 sayfa** → fe-partner + T200–T250

---

## F. Admin paneli (eksiksiz)

Komisyon ayarları, partner/otel onay, unified reservations, mail, yardım merkezi — **55 sayfa** → fe-admin + T140–T190

---

## G. SEO + güvenlik (paralel)

| Orkestratör | İş |
|-------------|-----|
| ork-seo-global | hreflang, çok dil meta, JSON-LD |
| ork-guvenlik | CSRF tüm POST, rate limit |
| ork-veri | PII, fatura dosya güvenliği |

---

## H. Türkçe dosya adı Faz 2

Panel controller rename (route korunur) → ork-turkce-faz2

---

## CTO onay

Her akış için: **Backend CTO** (DB+API) → **Frontend CTO** (mobil SS) → **Security CTO** → **Master CTO**

Hedef: `Canlıya hazır: EVET` yalnızca tüm tablolar ✅ ve FE-CTO = envanter.

*Arka plan orkestrası: `KURUMSAL_IS_AKISI` wave — agent ID 0303ab6a devam + yeni sprint.*
