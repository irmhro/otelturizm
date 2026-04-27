## Çoklu para birimi – fiyat hesaplama katmanı audit (p178)

### Mevcut durum (kod sinyalleri)

- UI / servis katmanında para formatlama yer yer **hardcode `tr-TR`** (`"{0:C0}"`) ve TRY sembolüne bağlı.
- `CurrencyFormatter` mevcut ama uygulama içinde yaygın kullanılmıyor.
- Currency seçimi artık `ot_currency` cookie ile tutuluyor; kullanıcı tercih kolonları opsiyonel olarak eklendi:
  - `users.tercih_para_birimi` (kolon varsa)

### Riskler

- **Gerçek FX dönüşümü yok**: USD/EUR seçilse bile tutarlar TRY olarak kalabilir (sadece gösterim değişirse UX tutarsızlığı doğar).
- **Yuvarlama ve vergi**: farklı para birimlerinde round stratejisi ve vergisel raporlama ayrılmalı.
- **Cache key**: fiyatı etkileyen tüm yerlerde currency vary gerekir (listing/detail/quote).

### Hedef mimari (öneri)

1) **Money value object**
   - `amount` + `currencyCode`
   - DB’de baz para birimi (örn. TRY) + anlık FX tablosu (günlük) ile görüntü dönüşümü.

2) **FX rate provider**
   - kaynak: manual admin panel / provider API (ileride)
   - cache + singleflight + circuit breaker

3) **Pricing pipeline**
   - `basePriceTry` → `priceInSelectedCurrency`
   - `CurrencyPreferenceService` üzerinden seçili currency alınır.

4) **UI format standardı**
   - tüm paneller/public için tek formatter (culture + currency symbol).

### Aksiyon listesi (sonraki iterasyon)

- `PartnerService/FirmaService` içindeki `FormatMoney` hardcode’larını merkezi formatlayıcıya taşı.
- `GetPriceQuote` ve listing/detail viewmodel’lerine `CurrencyCode` ekle (cache vary dahil).
- FX rate storage tablosu tasarla (`fx_rates` – date, base, quote, rate).

