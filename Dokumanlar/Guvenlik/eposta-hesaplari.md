## Otelturizm E-posta Hesapları (ŞİFRESİZ KAYIT)

Bu doküman **bilinçli olarak şifre içermez**. Şifreler **repoya yazılmaz**, ticket/markdown/log içine konulmaz.

### Ortak Bağlantı Ayarları
- **Aktif Gelen Posta Sunucusu**: `umay.muvhost.com`
- **Aktif Giden Sunucu (SMTP)**: `umay.muvhost.com`
- **IMAP Port**: `993` (SSL/TLS)
- **POP3 Port**: `995` (SSL/TLS)
- **SMTP Port**: `465` (SSL/TLS)
- **SMTP Alternatif Port**: `587` (STARTTLS)
- **Auth**: IMAP/POP3/SMTP **authentication required**

### Hesaplar
- `bildiri@otelturizm.com` (1 GB)
- `bilgi@otelturizm.com` (1 GB)
- `guvenlik@otelturizm.com` (1 GB)
- `info@otelturizm.com` (1 GB)
- `odeme@otelturizm.com` (1 GB)
- `rezervasyon@otelturizm.com` (1 GB)  şifre

### Şifre Yönetimi Standardı
- **Üretim sunucu**: şifreler sadece sunucuda **Environment Variable** veya **Secret Manager** ile tutulur.
- **Yerel geliştirme**: şifreler `.env`/User Secrets gibi yerlerde tutulur ve git’e eklenmez.

