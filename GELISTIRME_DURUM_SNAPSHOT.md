# Geliştirme Durum Snapshot — Visible Fix Wave

**Tarih:** 2026-05-25  
**Amaç:** Kullanıcının localhost’ta hemen görebileceği mobil otel detay, footer ve listing i18n düzeltmeleri.

---

## Terminal işleri

| Shell | Komut | Durum |
|-------|--------|--------|
| Build | `dotnet build D:\otelturizm\otelturizm.csproj -o .coord-build-visible` | **0 hata, 0 uyarı** |
| Dev server | `dotnet run --project D:\otelturizm\otelturizm.csproj --launch-profile https` | Yeniden başlatıldı (ilk deneme derlemede takıldı) |

**Portlar:** HTTPS `https://localhost:7223` · HTTP `http://localhost:5103`

---

## Canlıda görmek için

Bu dal değişiklikleri **yalnızca local dev** ortamında görünür. Production/canlı sitede görmek için **publish/deploy** gerekir; bu wave’de publish yapılmadı.

---

## 10 dosya — before / after

| # | Dosya | Önce | Sonra |
|---|--------|------|--------|
| 1 | `wwwroot/assets/css/otel-detay.mobile.css` | `#roomsCard` ~230px (2 sütunlu grid sıkışması); yatay oda kartları; outline CTA | Tek sütun full-width grid; oda kartı üst görsel + alt içerik; yatay chip olanaklar; mavi dolu CTA; galeri 4:3 + sıkı dots |
| 2 | `Views/Oteller/OtelDetay.cshtml` | Hardcoded TR metinler (oda bölümü, sticky bar) | `SharedLocalizer` ile oda/rezervasyon etiketleri |
| 3 | `Resources/SharedResources.resx` | Detail.Rooms.* anahtarları yok | 10 yeni TR Detail.* / Booking.* anahtarı |
| 4 | `Views/Anasayfa/_AnasayfaFooter.cshtml` | 5 sütun grid; İngilizce Footer.Description riski | TR path’te sabit Türkçe açıklama; mobil accordion (`details`) bölümler |
| 5 | `wwwroot/assets/css/site-footer.mobile.css` | *(yoktu)* | Yeni: #003B95 koyu footer, 16px+ tipografi, trust badge wrap, sticky bar safe-area |
| 6 | `Views/Shared/_Layout.cshtml` | Footer mobil CSS yok | `site-footer.mobile.css` media `(max-width:900px)` eklendi |
| 7 | `wwwroot/assets/css/site-layout.css` | Desktop’ta accordion summary tıklanabilir | ≥901px: summary chevron gizli, grid davranışı korunur |
| 8 | `Views/Oteller/OtelListeleme.cshtml` | `CurrentUICulture` → bazen "hotels found" | `/oteller` path’inde zorunlu `{N} otel bulundu` |
| 9 | `wwwroot/assets/css/paneller/otel/otel-detay.mobile.css` | Ana dosyayı import eder | Değişmedi (import zinciri aynı) |
| 10 | `GELISTIRME_DURUM_SNAPSHOT.md` | — | Bu dosya |

---

## Kök neden (otel detay 230px)

`otel-detay.css` dosyasının sonunda `.otel-detail-template-v41 .detail-grid { grid-template-columns: 1fr 360px }` kuralı, mobil `@media` tek sütun override’ını geçersiz kılıyordu. `otel-detay.mobile.css` sonuna `!important` ile tek sütun + `#roomsCard` full-width bloğu eklendi.

---

## Kullanıcı — değişiklikleri görmek için

1. Dev server çalışıyor olmalı (`Main terminalde `Now listening on https://localhost:7223`
2. Tarayıcıda **hard refresh** (Ctrl+Shift+R) veya gizli pencere
3. Test URL’leri:
   - Liste: `https://localhost:7223/oteller` → "12 otel bulundu" (TR)
   - Detay: herhangi bir otel slug → mobil görünüm (DevTools ≤900px)
4. Canlı sitede görmek için deploy/publish şart
