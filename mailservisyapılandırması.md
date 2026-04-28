# Mail Servis Yapılandırması

Bu doküman platform e-posta altyapısında kullanılan güncel sunucu/port bilgisini tutar.

## Aktif SMTP / IMAP Host

- Ana host: `umay.muvhost.com`
- SMTP `465` `SSL/TLS`
- SMTP `587` `STARTTLS`
- IMAP `993` `SSL/TLS`

## Platform Hesapları

- `bildiri@otelturizm.com`
- `bilgi@otelturizm.com`
- `destek@otelturizm.com`
- `guvenlik@otelturizm.com`
- `info@otelturizm.com`
- `odeme@otelturizm.com`
- `rezervasyon@otelturizm.com`

## Kullanım Notu

- Şablon bazlı yönlendirme `email_services` ve `platform_email_hesaplari` kayıtları üzerinden yapılır.
- Güvenlik / 2FA / e-posta doğrulama / şifre sıfırlama akışları `guvenlik@otelturizm.com` hattını kullanır.
- Rezervasyon akışları `rezervasyon@otelturizm.com` hattını kullanır.
- Bildirim ve fiyat alarmı akışları `bildiri@otelturizm.com` hattını kullanır.
- Genel fallback gönderen `info@otelturizm.com` hesabıdır.

## Operasyon Notu

- Repo içine gerçek parola yazılmaz.
- Canlı DB seed edilirken ilgili hesap parolaları operasyon sırasında ayrıca uygulanır.
