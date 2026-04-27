## Firma Çalışan Rolleri — Audit (Paket 152)

### Amaç

Firma panelinde çalışanların (personel) limit/onay akışında hangi rolün hangi aksiyonu yapabileceğini netleştirmek.

### Mevcut sinyaller

- Firma panelinde “çalışanlar / limitler / onay” sayfaları var (`Views/Paneller/Firma/*`).
- Firma kullanıcı tespiti: `FirmaPanelController.IsFirmaUser()` `accountType=firma` veya `firma_*` role prefix.

### Önerilen roller (minimum)

- **firma_admin**: tüm ayarlar, limitler, onaylar
- **firma_approver**: rezervasyon/limit onay
- **firma_viewer**: sadece görüntüleme

### Önerilen kontrol noktaları

- **Rezervasyon onayı**: `ReservationApproval` gibi aksiyonlarda approver/admin şartı
- **Limit upsert**: admin şartı
- **Çalışan yönetimi**: admin şartı

### Not

Bu paket “audit + standard” kapsamındadır; sonraki adımda RBAC guard (Paket 154) ile action bazlı enforce edilir.

