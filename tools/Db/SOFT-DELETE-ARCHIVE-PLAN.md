## Soft Delete & Archive Stratejisi (Paket 165)

### Amaç

Üretimde veri silmeyi (hard delete) minimize etmek, log/audit gereksinimlerini karşılamak ve performansı korumak.

### Soft delete standardı

- Tablolarda mümkünse:
  - `aktif_mi` (bit)
  - `silinme_tarihi_utc` (datetime2, null)
  - `silinme_nedeni` (nvarchar, null)
  - `silinen_kullanici_id` (bigint, null)
- Uygulama sorguları: default olarak `aktif_mi = 1` filtreler.

### Archive (cold storage)

- Büyük/historik tablolar:
  - `rezervasyonlar`, `rezervasyon_odeme_kalemleri`, `admin_islem_loglari`, `api_loglari`
- Arşiv yaklaşımı:
  - `*_arsiv` tablolarına (aynı şema) dönemsel taşıma
  - “aktif” tabloda yalnızca son \(N\) ay bırakma (örn. 12–24 ay)

### Taşıma prensipleri

- Taşıma “idempotent” olmalı.
- Taşıma öncesi “foreign key” bağımlılıkları planlanmalı.
- Taşıma job’ı: düşük trafikte çalışır, batch ve timebox ile.

