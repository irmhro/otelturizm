## Komisyon / Finans — Hesap Kesim Periyodu Modeli (Paket 157)

### Amaç

Partner ödemeleri ve komisyon hesaplarını “dönem” bazında standartlaştırmak.

### Önerilen dönem modeli

- **Weekly**: Pazartesi–Pazar
- **BiWeekly**: 15 günlük
- **Monthly**: ay bazlı

### Önerilen tablolar (ileride)

- `hesap_kesim_donemleri`:
  - `id`, `partner_id`, `period_type`, `start_utc`, `end_utc`, `status`
- `komisyon_hesaplari`:
  - `donem_id`, `rezervasyon_id`, `brut`, `komisyon_orani`, `komisyon`, `net`, `currency`

### Hesaplama prensipleri

- Kaynak: `rezervasyon_odeme_kalemleri` / rezervasyon toplamları
- İadeler/iptaller dönem dışına taşabilir → ayrı “adjustment” kalemi

