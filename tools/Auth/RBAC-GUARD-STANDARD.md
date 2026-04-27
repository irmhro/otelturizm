## RBAC Guard Standardı (Paket 154)

### Amaç

Controller/action bazlı “hesap tipi + rol” guard’larının tutarlı ve denetlenebilir olması.

### Standartlar

- **Route prefix**’i paneli belirler:
  - `/admin/*` → admin
  - `/panel/partner/*` → partner
  - `/panel/firma/*` → firma
  - `/panel/satis/*` → sales
  - `/panel/user/*` → user
- **Her panel controller**:
  - `[Authorize]` zorunlu
  - Giriş yönlendirmesi: accountType’e göre doğru login sayfası
  - İşlem yapan kullanıcının bağlam id’si (partnerId/firmaId) doğrulanmalı

### Checklist

- Action bir kayıt üzerinde işlem yapıyorsa: **kayıt bağlamı** (otel.partner_id, firma_id vb.) ile kullanıcı bağlamı eşleşiyor mu?
- Admin dışında “başka hesabın verisini” görme ihtimali var mı?
- Audit log: kritik aksiyonlarda (role değişimi, limit/onay) `admin_islem_loglari` veya sistem audit’e yazılıyor mu?

