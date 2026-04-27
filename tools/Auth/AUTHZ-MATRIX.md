## Yetki Matrisi (Paket 153)

### Hesap tipleri

- **admin**: sistem yönetimi, partner/firma/user yönetimi, finans ayarları
- **partner**: kendi otelleri/odaları/fiyatları/kampanya katılımı
- **firma**: firma çalışanları, limit/onay, firma rezervasyonları
- **sales**: satış rezervasyonu, müşteri yönetimi, satış raporları
- **developer**: geliştirme ve sistem araçları (kısıtlı)
- **user**: public rezervasyon, profil, favoriler, mesajlar

### Ekranlar ve erişim

- **Public (`/`, `/oteller`, `/kampanyalar`)**: herkes
- **User Panel (`/panel/user/*`)**: `accountType=user` (veya user role)
- **Firma Panel (`/panel/firma/*`)**: `accountType=firma` veya `userRole` `firma_*`
- **Partner Panel (`/panel/partner/*`)**: `accountType=partner` veya partner role
- **Sales Panel (`/panel/satis/*`)**: `accountType=sales` veya sales role
- **Admin (`/admin/*`)**: `accountType=admin` veya admin role
- **Secure files (`/secure-files/{token}`)**: auth + token doğrulaması (zaten kullanıcıya bağlı)

### Kritik prensipler

- **Bağlam doğrulaması**: partner/firma verileri “aktif bağlam” ile doğrulanmalı.
- **Least privilege**: sadece gerekli aksiyonlar rol bazında açılır.
- **Audit**: role değişimi, yetki güncellemesi, limit/onay kararları loglanır.

