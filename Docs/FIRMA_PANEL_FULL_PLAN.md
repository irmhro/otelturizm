# Firma paneli — tam kapsam planı

## Kapsam

- **Rotalar:** `FirmaPanelController` (`/panel/firma/*`)
- **Görünümler:** `Views/Paneller/Firma/*.cshtml`
- **Stil:** `wwwroot/assets/css/paneller/firma/*` + mesajlar için `paneller/firma/messages` (kullanıcı mesaj stillerini import eder)
- **Servis:** `FirmaService` + `IAuthService` (güvenlik/2FA)

## Sayfa envanteri (controller ↔ view)

| Rota / Action | View | CSS |
|---------------|------|-----|
| `Index` / `dashboard` | Dashboard | `paneller/firma/dashboard` |
| `Security` | Security | `paneller/firma/security` |
| `Deals` | Deals | `paneller/firma/deals` |
| `CompareDeals` | DealsCompare | `paneller/firma/deals` |
| `Reservations` | Reservations | `paneller/firma/reservations` |
| `CreateReservation` | CreateReservation | `paneller/firma/create-reservation` |
| `Messages` | Messages | `paneller/firma/messages` → user mesaj temeli import |
| `Employees` | Employees | `paneller/firma/employees` |
| `Limits` | Limits | `paneller/firma/limits` |
| `Invoices` | Invoices | `paneller/firma/invoices` |
| `Spending` | Spending | `paneller/firma/spending` |
| `Hotels` | Hotels | `paneller/firma/hotels` |

## Kapatılan eksikler (bu sprint)

1. **Kenar çubuğu:** `CompareDeals` iken “Firma Fiyatları” aktif görünsün (karşılaştırma akışı).
2. **Mobil alt menü:** Yalnızca 6 kısayol yerine tüm kritik sayfalara yatay kaydırmalı erişim (sidebar ile uyumlu).
3. **Mesajlar CSS:** `panel-user-messages` yolu yerine `paneller/firma/messages` — sayfa adı standardına uygun import dosyaları.
4. **Shell mobil CSS:** Alt bar çok öğeli düzende taşmayı önlemek için scroll düzeni.

## Test checklist

- [ ] Desktop: tüm sidebar linkleri açılır.
- [ ] Mobil (<900px): alt menüden Dashboard, Fiyat, Yeni rezervasyon, Rezervasyonlar, Mesajlar, Çalışanlar, Limit/Onay, Fatura, Harcama, Otel raporu, Güvenlik erişilebilir.
- [ ] Fiyat karşılaştır sayfasında sidebar’da “Firma Fiyatları” vurgulu.
