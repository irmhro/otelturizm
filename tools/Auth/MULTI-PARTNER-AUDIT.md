## Çoklu Partner İlişki Modeli — Audit (Paket 151)

### Amaç

Tek kullanıcı hesabının birden fazla **partner** organizasyonuna bağlanabildiği modeli (B2B) netleştirmek ve bug risklerini (yanlış partner’e veri sızması) azaltmak.

### Mevcut sinyaller (koddan)

- Auth claim’leri: `AuthClaimTypes.AccountType`, `AuthClaimTypes.UserRole`, `AuthClaimTypes.PartnerId`
- Oturum güvenliği: `SessionSecurityService` partner_id/accountType kaydı tutuyor.
- Admin/partner iş akışları: `AdminService`, `PartnerService` partner_id üzerinden çalışıyor.

### Risk alanları

- **Tekil `partner_id` claim’i**: kullanıcı birden çok partner’e bağlıysa “aktif partner” seçimi gerekir.
- **Yetki kontrolü**: controller/service seviyesinde partner_id doğrulaması yoksa yanlış partner verisi görülebilir.
- **Audit & log**: hangi partner bağlamında işlem yapıldı kayıt altına alınmalı.

### Önerilen hedef mimari

- **Aktif partner bağlamı**: cookie veya session key ile “aktif partnerId” seçimi.
- **İlişki tablosu**: `kullanici_partnerler` (kullanici_id, partner_id, rol, aktif_mi, baslangic/bitiş).
- **Guard**: her partner panel isteğinde `activePartnerId` → ilişki tablosundan doğrulanır.

### Minimum aksiyonlar (bu paket)

- Yetki matrisi dokümanı ile (Paket 153) “kim neyi görür” netleştirildi.
- RBAC guard standardı ile (Paket 154) controller/action kontrolünde tutarlılık hedefi yazıldı.

