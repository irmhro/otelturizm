# Vergi ve ek ücret motoru — paket 243

## Bugün

Misafir tarafı fiyat gösterimi ve partner girişleri **`InclusiveNightlyPricing`** ve otel bağlı **`komisyon_vergiler`** / sabit KDV-konaklama oranları ile uyumludur.

## Ülke / şehir genişlemesi

- Konfigürasyon katmanı: il/ülke koduna göre KDV, şehir vergisi, servis ücreti katsayıları.
- Tek doğruluk kaynağı: rezervasyon satır hesaplaması ile vitrin çizgisinin aynı yardımcı sınıftan geçmesi.

Bu dosya mimari kararı sabitler; kodda yeni ülke eklemek için önce şema + admin tanımı gerekir.
