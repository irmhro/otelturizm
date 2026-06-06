# Firma (B2B kurumsal) paneli — tam gelişim planı

Partner paneliyle aynı orchestrator yapısı; mevcut `firmapanel-v6-shell` tasarımı korunur.

## Orchestrator özeti

| Grup | Kapsam | Öncelik |
|------|--------|---------|
| **F0** | Altyapı: route/CSS sözleşmesi, shell JS, geri bildirim, `otelId` yok (tek firma) | P0 |
| **F1** | Kokpit: KPI, hızlı aksiyonlar, son rezervasyonlar | P1 |
| **F2** | Organizasyon: çalışan CRUD, limitler, faturalar | P0 |
| **F3** | Seyahat: firma fiyatları, yeni rezervasyon, rezervasyon listesi, mesajlar | P0 |
| **F4** | Raporlar: harcama, otel bazlı, CSV export | P1 |
| **F5** | Hesap: hesap bilgileri, güvenlik/2FA, sözleşmeler | P1 |
| **F6** | Fiyat karşılaştırma & filtre polish | P2 |
| **F7** | Bildirim, destek, tema | P2 |
| **F8** | Passkey / WhatsApp 2FA (partner ile hizalı) | P3 |

## Menü ↔ route

- **Kokpit:** `/panel/firma/dashboard`
- **Organizasyon:** `calisanlar`, `limitler-onaylar`, `faturalar`
- **Seyahat:** `firma-fiyatlari`, `yeni-rezervasyon`, `rezervasyonlar`, `mesajlar`
- **Raporlar:** `harcama-raporlari`, `otel-bazli-rapor`
- **Hesap:** `hesap-bilgileri`, `guvenlik` (+ sözleşmeler anchor)

## F0 — Altyapı

- [x] Global arama (Ctrl+K, Enter → rezervasyonlar `?q=`)
- [x] Para birimi switcher görüntüleme tercihi (localStorage)
- [x] `ReturnUrl` rezervasyon onay redirect
- [ ] Dead route / controller temizliği taraması

## F2 — Organizasyon (P0 tamamlanan)

- [x] Çalışan düzenleme / pasifleştirme (`POST calisan-guncelle`)
- [x] Çalışan listesi arama + sayfalama (mevcut)
- [x] Fatura indirme (`GET faturalar/indir`)
- [x] Limitler sayfası şema eksikliğinde graceful mesaj (mevcut)

## F3 — Seyahat (P0 tamamlanan)

- [x] Rezervasyon filtreleri (durum, firma onayı, arama)
- [x] CSV dışa aktarma
- [x] Liste içi onay/red (bekleyen kayıtlar)
- [ ] Mesaj: yeni konuşma başlatma (P2)

## F4 — Raporlar

- [x] Harcama raporu metin temizliği
- [ ] Otel/harcama CSV export (P2)

## F5 — Hesap

- [x] Hesap bilgileri sayfası + sidebar
- [x] Güvenlik / 2FA e-posta (mevcut)
- [ ] Bildirim tercihleri, destek ticket (P2)

## Tasarım sözleşmesi

- Layout: `_FirmaPanelLayout.cshtml`, `_FirmaSidebar`, `_FirmaTopNav`
- Shell CSS: `firmapanel_masaustu.css`, `paneller/firma/shell.css`
- Sayfa CSS: `wwwroot/assets/css/paneller/firma/{sayfa}.css` (+ `.mobile.css`)
- Tablo: `firma-table firma-table--cards`, filtre: `firma-form-grid panel-form-ux`

## Canlı doğrulama checklist

- [ ] `/panel/firma/rezervasyonlar?q=` filtre
- [ ] `/panel/firma/rezervasyonlar/disa-aktar`
- [ ] `/panel/firma/faturalar/indir?invoiceId=`
- [ ] `/panel/firma/calisan-guncelle`
- [ ] `/panel/firma/hesap-bilgileri`
