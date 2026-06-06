# Kullanıcı (misafir) paneli — tam gelişim planı

Mevcut `kullanici-panel-shell` tasarımı korunarak partner/firma paneliyle hizalı orchestrator yapısı.

## Orchestrator özeti

| Grup | Kapsam | Öncelik |
|------|--------|---------|
| **U0** | Altyapı: shell JS, global arama, tema, badge/rozet | P0 |
| **U1** | Kokpit: dashboard widget, hızlı aksiyonlar | P1 |
| **U2** | Rezervasyonlar: filtre, iptal, not, yorum, export | P0 |
| **U3** | Favoriler & fiyat alarmları | P1 |
| **U4** | OtelPuan: seviye, ödül, pasaport, planlayıcı | P0 |
| **U5** | Cüzdan: faturalar, ödeme yöntemleri, fatura bilgisi | P0 |
| **U6** | Mesajlar & destek | P1 |
| **U7** | Profil, bildirim, güvenlik | P1 |
| **U8** | Passkey / WhatsApp 2FA, oturum sonlandırma | P2 |

## Menü ↔ route

- **Kokpit:** `/panel/user/dashboard`
- **Rezervasyon:** `rezervasyonlarim`, `yorumlarim`, `rezervasyonlarim/yorum/{id}`
- **Favoriler:** `favorilerim`
- **OtelPuan:** `puanlarim` (alias `otelpuan-programi`)
- **Cüzdan:** `faturalarim`, `odeme-yontemleri`
- **Mesajlar:** `mesajlarim`
- **Profil:** `profil-bilgilerim`, `bildirim-tercihleri`, `guvenlik-ve-giris`

## U0 — Altyapı (P0 tamamlanan)

- [x] Global arama Enter → `rezervasyonlarim?searchTerm=`
- [x] Ctrl+K odak
- [x] Dinamik OtelPuan seviye chip (topbar)
- [ ] Mobil alt nav: faturalar/puan linkleri (P2)

## U2 — Rezervasyonlar (P0 tamamlanan)

- [x] Durum sekmeleri, tarih, arama, sıralama, sayfalama (mevcut)
- [x] CSV dışa aktarma (`rezervasyonlarim/disa-aktar`)
- [x] İptal + misafir notu + yorum (mevcut)

## U4 — OtelPuan (P0 tamamlanan)

- [x] Ödül kataloğu kullanımı (`puanlarim/odul-kullan`)
- [x] DB seed: `SADAKAT_SEVIYELERI`, `SADAKAT_ODULLERI`
- [x] Bütçe / seyahat planı formları (mevcut)

## U5 — Cüzdan (P0 tamamlanan)

- [x] Fatura indirme (secure URL, mevcut)
- [x] Ödeme yöntemi ekle/sil (mevcut)
- [x] Fatura bilgisi düzenleme (`odeme-yontemleri/fatura-kaydet`)

## Tasarım sözleşmesi

- Layout: `_UserPanelLayout`, `_UserSidebar`, `_UserTopNav`
- Shell CSS: `kullanici_panel_masaustu.css` / `_mobil.css`
- Sayfa CSS: `kullanici_panel_{sayfa}_masaustu.css` (+ `_mobil`)
- Shell JS: `kullanici_panel-shell.js`

## Canlı doğrulama checklist

- [ ] `/panel/user/rezervasyonlarim?searchTerm=`
- [ ] `/panel/user/rezervasyonlarim/disa-aktar`
- [ ] `/panel/user/puanlarim/odul-kullan` (POST)
- [ ] `/panel/user/odeme-yontemleri/fatura-kaydet`
- [ ] Sadakat seed canlı DB'de
