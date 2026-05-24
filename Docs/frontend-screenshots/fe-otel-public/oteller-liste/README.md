# Otel listesi — ekran görüntüsü (#073)

**Sayfa:** `/oteller` (`OtelListeleme.cshtml`)

## Hedef dosyalar

| Ortam | Dosya |
|-------|--------|
| Desktop 1440×900 | `desktop/step-01.png` |
| Mobil 390×844 | `mobil/step-01.png` |

## Ne görünmeli (deploy sonrası)

- Üst başlıkta **“X tesis”** sonuç rozeti (`listing-result-badge`)
- `fe-world-tokens.css` yüklü (geliştirici araçları → Network)
- Boş aramada **“Tüm otelleri göster”** birincil buton

## Yakalama (deploy sonrası önerilir)

1. Tam Release publish: `Docs/DEPLOY_ACIL_500_VE_GORUNUR_GELISTIRME.md`
2. Uygulamayı çalıştırın: `dotnet run --project D:\otelturizm\otelturizm.csproj` veya IIS
3. Tarayıcıda `/oteller` → **Ctrl+F5**
4. Playwright / UI Scout / manuel screenshot:
   - Desktop: 1440×900 full page
   - Mobil: 390×844, device toolbar
5. PNG’leri bu klasöre kaydedin (`desktop/`, `mobil/` alt klasörleri oluşturun)

**Not:** Bu oturumda PNG üretilmediyse klasör yapısı + bu README yeterli; SS deploy sonrası alınır.
