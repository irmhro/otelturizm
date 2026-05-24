# Seyahat Planlama — Özellik Planı (Tamamlandı)

**Dalga:** Wave-Travel-Plan  
**Route:** `/seyahat-planlama`  
**Durum:** Üretim hazır (Yakında placeholder kaldırıldı)

## Amaç

Kullanıcıların destinasyon, bütçe ve kampanya bağlamında otel keşfine yönlendirildiği, sadakat programıyla entegre kamu planlama sayfası.

## Kapsam (bitmiş)

| Modül | Davranış |
|--------|----------|
| Rota önerileri | İstanbul, Antalya, Kapadokya, Bodrum kartları → `/oteller?q=...` |
| Bütçe tahmini | GET form (`dest`, `gece`, `butce`, `tahmin=true`); tahmini TRY aralığı + liste CTA |
| Hafta sonu | 3 kart: şehir/sahil + `/hafta-sonu-firsatlari` |
| Kampanyalar | DB’den aktif ilk 3 (`ICampaignService`) → `/kampanyalar/{slug}` |
| OtelPuan | Girişli → `/panel/user/otelpuan-programi`; misafir → giriş/kayıt |

## Teknik

- **Controller:** `Controllers/SeyahatPlanlama/SeyahatPlanlamaController.cs`
- **Service:** `SeyahatPlanlamaService` + `ISeyahatPlanlamaService`
- **View:** `Views/SeyahatPlanlama/Index.cshtml` (`section.plan-page`)
- **CSS:** `wwwroot/assets/css/seyahat-planlama.css`, `seyahat-planlama.mobile.css`, `fe-world-tokens`
- **i18n:** `Plan.*` anahtarları `SharedResources*.resx` (sayfa metinleri Türkçe)
- **Cache:** `public-medium`, query vary (bütçe formu)

## Bütçe heuristiği

Destinasyon başına referans gecelik TRY (platform başlangıç fiyatlarına yakın):

| Destinasyon | Min gecelik TRY |
|-------------|-----------------|
| İstanbul | 1.850 |
| Antalya | 2.200 |
| Kapadokya | 2.600 |
| Bodrum | 3.200 |

- Tahmini toplam: `gece × minGecelik` – `×1,38` üst bant  
- Bütçe dar ise liste linki `etiket=butceme-uygun-oteller` eklenir

## Anasayfa

`_AnasayfaContent.cshtml` özellik kartı `/seyahat-planlama` tam sayfaya link verir (gömülü plan-page yok).

## Doğrulama

```powershell
dotnet build "D:\otelturizm\otelturizm.csproj" -o .coord-build-seyahat --no-restore
```

Manuel: `/seyahat-planlama`, bütçe formu gönder, kampanya kartları, mobil 44px dokunma hedefleri.

## DB

Yeni migration gerekmez (kampanya verisi mevcut `KAMPANYALAR` tablosundan).
