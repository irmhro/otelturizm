# Çok otelli rezervasyon platformu — panel envanteri ve güvenlik yüzeyi

Bu belge panellerin görevini, güvenli dosya/token kullanımını ve iş akışını özetler; tek tek sayfa listesi canlı koddan türetilir.

## Paneller (giriş ve rol)

| Panel | Örnek giriş | Kimlik |
|------|-------------|--------|
| Misafir / kullanıcı | `/kullanici-giris` → `/panel/user/*` | Çerez, `AccountType=user` |
| Otel ortağı | `/partner-giris` → `/panel/partner/*` | Çerez, partner kullanıcısı |
| Firma (kurumsal) | `/firma-giris` → `/panel/firma/*` | Çerez, firma kullanıcısı |
| Satış | `/kullanici-giris` → `/panel/satis/*` | Yetkiye bağlı |
| Yönetim | `/admin-giris` → `/admin/*` | Admin + DB RBAC izinleri |
| Departman | Ayrı layout | Kurumsal iç süreç |
| Geliştirici | Kısıtlı erişim | `DevelopmentAccess` vb. |

Global: **CSRF** (`AutoValidateAntiforgeryToken`), panel formlarında antiforgery çerezi; **CSP** ve güvenlik başlıkları `Program.cs` ara yazılımında.

## Güvenli dosya ve URL yüzeyi

| Öğe | Davranış |
|-----|----------|
| `/secure-files/{token}` | `[Authorize]` gerekli; token satırı kullanıcı + hesap tipine bağlı; süre ve kullanım sınırı (`SecureFileService`) |
| `/uploads/file/...` | Uygulama katmanında **404** — doğrudan tahmin edilebilir yol kapatılır |
| Mesaj / profil ekleri | Kayıt sonrası genelde `CreateAccessUrlAsync` ile zamanlı URL |

Özet: Dosya erişimi **token + oturum + DB doğrulaması** ile; ham yükleme URL’si ile herkese açık servis yok.

## Partner → Firma rezervasyonları (iş akışı)

- Liste: `/panel/partner/firmalar/rezervasyonlar`
- Veri: `rezervasyonlar.firma_id` dolu kayıtlar; firma adı `firmalar` ile.
- **Bu iyileştirmeden sonra:** Tarih filtresi **oluşturulma** veya **giriş–çıkış (konaklama penceresi)**; **“Yalnızca tamamlanan konaklama”** çıkış tarihi geçmiş ve iptal dışı kayıtları daraltır.
- Durum filtresi açılır liste ile ana rezervasyon durumlarına sabitlendi.

## Eksik sayfa / planlama notları

- Tüm panellerde menü–controller eşlemesi büyük ölçüde tamam; **PlannedModule** gibi “yakında” sayfalar bilinçli yer tutucudur.
- Firma panelinde rezervasyon oluşturma/listeler `FirmaService` ile ayrı akışta; partner tarafı firma **rezervasyonlarının görünürlüğü** ve **filtre zenginliği** bu iş paketinde güçlendirildi.
- İleride: Partner firma satırından detay sayfasına (`rezervasyonlar` odaklı) derin bağlantı istenirse tek rezervasyon route’u ile bağlanabilir.

## Loglama (PII)

- Ayrıntı: [PII_LOGGING_H8_NOTE.md](PII_LOGGING_H8_NOTE.md) — upload audit, e-posta hata logları ve rezervasyon denetim logları.

## Bakım

- Yeni panel API’si: POST’ta antiforgery veya açıkça sınırlandırılmış JSON + rate limit politikası.
- Yeni dosya türü: `SecureFileService` uzantısı + MIME/magic kontrolü.
