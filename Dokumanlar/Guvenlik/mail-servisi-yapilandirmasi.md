# Mail Servis Yapılandırması

Bu doküman platform e-posta altyapısında kullanılan güncel sunucu/port bilgisini tutar.

## Aktif SMTP / IMAP Host

- Ana host: `umay.muvhost.com`
- SMTP `465` `SSL/TLS`
- SMTP `587` `STARTTLS`
- IMAP `993` `SSL/TLS`
- SMTP kullanıcı doğrulaması zorunlu: gönderen adresi ile aynı hesap kullanılmalı

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
- Antispam için uygulama tarafında şu kurallar korunur:
  - `HTML + plain text` birlikte gönderilir
  - `From`, `Reply-To` ve SMTP login aynı hesap hattında kalır
  - bozuk Türkçe başlık/gönderen adı kullanılmaz
  - test/rezervasyon/2FA maillerine "virüs tarandı" gibi yapay güven cümleleri eklenmez

## DNS / Teslimat Kontrolü

- SPF kaydı aktif olmalı ve `mail.otelturizm.com` / `umay.muvhost.com` gönderim yetkisini kapsamalı.
- DKIM imzası hosting panelinde açık olmalı.
- DMARC kaydı en azından raporlama modunda bulunmalı; üretimde tercihen `quarantine` veya kontrollü `reject` seviyesine yükseltilir.
- Gmail spam düşmesini azaltmak için:
  - aynı şablondan kısa sürede çok yüksek hacimli test atılmaz
  - konu satırı ve gönderen adı Türkçe bozuk karakter içermez
  - 2FA / doğrulama / şifre sıfırlama mailleri yalnızca ilgili güvenlik hesabından çıkar
