## Muhasebe Export Standardı (Paket 159)

### Çıktı formatları

- **CSV (UTF-8 BOM)**: Excel uyumlu
- **Excel**: (ileride) ayrı servis/kitaplık ile

### Dosya adlandırma

- `accounting-export_{scope}_{start}_{end}_{generatedAt}.csv`

### Kolonlar (minimum)

- `reservation_id`
- `reservation_no`
- `hotel_id`
- `hotel_name`
- `company_id` / `firma_id` (varsa)
- `check_in`, `check_out`
- `gross_total`
- `tax_total`
- `commission_total`
- `net_total`
- `currency`
- `status`
- `created_at_utc`

### Veri kaynağı

- `rezervasyonlar`
- `rezervasyon_odeme_kalemleri` (kalem bazlı)

