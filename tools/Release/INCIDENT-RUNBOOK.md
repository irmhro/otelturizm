## Paket 197 — Incident runbook (temel senaryolar)

### 1. Site erişilemiyor (502/503)

1. IIS / app pool çalışıyor mu?
2. `/health/live` → uygulama ayakta mı?
3. `/health/ready` → DB bağlantısı?
4. Son deploy / config değişimi?

### 2. Veritabanı hatası artışı

1. SQL Server erişilebilirlik, disk, deadlock.
2. Son migration?
3. Yavaş sorgu logları (`SlowSql` izleme varsa).

### 3. E-posta gönderilmiyor

1. Admin e-posta kuyruk ekranı (`/admin/email-kuyruk`).
2. `email_services` aktif kayıt ve test modu.
3. Arka plan worker logları.

### 4. Güvenlik olayı şüphesi

1. `/admin/guvenlik-olaylari` ve Serilog dosyaları.
2. Şüpheli IP / hesap kilidi.
3. Gerekirse CSP rapor özeti.

### İletişim

- İç eskalasyon ve müşteri iletişim kanallarını ekleyin.
