# PII logging — H8 (T107) notu

## İlke

- Loglarda **e-posta, telefon, TC, tam adres, şifre, token, dekont yolu** yazılmaz; gerekirse maskelenir.
- `RESERVATION_AUDIT` ve benzeri denetim logları yalnızca **kimlik (userId), otel/oda id, rezervasyon no, tutar** taşır; misafir PII taşımaz.

## Maskelenen / kısıtlanan noktalar (bu sprint)

| Kaynak | Alan | Davranış |
|--------|------|----------|
| `UploadAuditService` | `ip`, `ua`, `path` | IP son oktet maskeli; UA kısaltılmış; path dosya adı |
| `EmailDeliveryBackgroundService` | `Recipient` hata logu | `LogRedaction.MaskEmail` |
| `UserPanelService` / `AuthService` | 2FA yanıtları | Mevcut `MaskEmail` / `MaskPhone` |

## Bilinçli istisnalar

- `SqlTiming` yavaş SQL: parametre değerleri loglanmaz; yalnızca SQL metni (1200 karakter).
- `CspReportController` / `IstemciHataRaporController`: rapor gövdesi kısaltılmış; üretimde ek redaksiyon H7 ile gözden geçirilebilir.
- E-posta kuyruğu başarı logları alıcı içermez.

## API yanıtları

- Kullanıcı paneli profil/2FA: maskeli e-posta/telefon view model alanları (`MaskedEmailAddress`, `MaskedPhoneNumber`).
- Partner müşteri listesi: `MaskedEmail` (mevcut).

## Yapılmayan

- Tüm servislerde otomatik log enricher yok; yeni log eklerken bu tabloya uyulmalı.
