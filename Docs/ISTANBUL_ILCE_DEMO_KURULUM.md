# Istanbul ilce demo otelleri — kurulum

39 Istanbul ilcesi icin birer demo otel: liste, harita, detay, rezervasyon, admin ve partner ekran testleri.

## SQL uygulama sirasi

1. Geo / kampanya onkosullari (yoksa):
   - `20260522_seed_ulkeler_dunya_listesi.sql`
   - `20260522_seed_iller_turkiye.sql` / ilce seed
   - `20260522_seed_kampanyalar_10y_kurumsal.sql` (`KMP-2026-SEHIR`)
2. **`20260526_seed_istanbul_ilce_oteller_tam.sql`** — otel, partner, oda, 30 gun fiyat, kampanya katilimi, ilk 5 otelde demo rezervasyon
3. **`20260526_seed_istanbul_ilce_medya_ozellik.sql`** — gorsel kayitlari, oda ozellikleri, hafta sonu `FIYAT_INDIRIMLERI`
4. (Opsiyonel) Gorsel dosyalari: `powershell -File tools\Db\Install-DemoHotelMedia.ps1`

## sqlcmd ornegi

```powershell
$db = "otelturizm_2026db"
$srv = "(localdb)\MSSQLLocalDB"
$base = "D:\otelturizm\Database\MigrationsSql\veri\migrationlar"

sqlcmd -S $srv -d $db -b -i "$base\20260526_seed_istanbul_ilce_oteller_tam.sql"
sqlcmd -S $srv -d $db -b -i "$base\20260526_seed_istanbul_ilce_medya_ozellik.sql"
```

Dogrulama (Unicode-normalize — `HotelService` / `CampaignService` ile ayni):

```powershell
$q = @"
SET NOCOUNT ON;
SELECT 'yayinda' AS m, COUNT(*) c FROM OTELLER WHERE LOWER(REPLACE(LTRIM(RTRIM(yayin_durumu)), NCHAR(0x0131), N'i')) = N'yayinda';
SELECT 'oda_tipleri' AS m, COUNT(*) c FROM ODA_TIPLERI ot JOIN OTELLER h ON ot.otel_id=h.id WHERE LOWER(REPLACE(LTRIM(RTRIM(h.yayin_durumu)), NCHAR(0x0131), N'i')) = N'yayinda';
SELECT 'fiyat_satir' AS m, COUNT(*) c FROM ODA_FIYAT_MUSAITLIK ofm JOIN OTELLER h ON ofm.otel_id=h.id WHERE h.otel_kodu LIKE N'ORK-%';
SELECT 'kampanya_otel_aktif' AS m, COUNT(*) c FROM kampanya_oteller ko JOIN OTELLER h ON ko.otel_id=h.id WHERE ko.katilim_durumu=N'Aktif' AND h.otel_kodu LIKE N'ORK-%';
"@
sqlcmd -S $srv -d $db -E -W -h -1 -Q $q
```

Ek gap-fill (90 gun fiyat, eksik oda, havuz/wifi/kahvalti, `KATILIM_DURUMU=Aktif`):

`20260523_seed_demo_oda_fiyat_kampanya.sql`

## Partner giris bilgileri

Tum partner hesaplari sifre: **`Demo123!`**

Giris URL: **`http://127.0.0.1:5103/partner-giris`**

| Ilce (ornek) | E-posta | Otel kodu |
|--------------|---------|-----------|
| Adalar | irmhro0+adalar@gmail.com | ORK-IST-ADALAR |
| Besiktas | irmhro0+besiktas@gmail.com | ORK-SEED-001 |
| Beyoglu | irmhro0+beyoglu@gmail.com | ORK-SEED-002 |
| Kartal | irmhro0+kartal@gmail.com | ORK-SEED-003 |
| Kadikoy | irmhro0+kadikoy@gmail.com | ORK-SEED-004 |
| Pendik | irmhro0+pendik@gmail.com | ORK-IST-PENDIK |
| Sariyer | irmhro0+sariyer@gmail.com | ORK-IST-SARIYER |
| Sisli | irmhro0+sisli@gmail.com | ORK-SEED-005 |
| Uskudar | irmhro0+uskudar@gmail.com | ORK-SEED-006 |
| Fatih | irmhro0+fatih@gmail.com | ORK-SEED-007 |
| Bakirkoy | irmhro0+bakirkoy@gmail.com | ORK-SEED-008 |
| Maltepe | irmhro0+maltepe@gmail.com | ORK-SEED-009 |
| Atasehir | irmhro0+atasehir@gmail.com | ORK-SEED-010 |

Tam liste: `ILCELER.SEO_SLUG` → `irmhro0+{slug}@gmail.com`, vergi no `ORK-IST-{slug}` (max 20 karakter; uzun slug kisaltilir, ornek `ORK-IST-GAZIOSMANPA`).

## Test URL'leri

| Ekran | URL |
|-------|-----|
| Istanbul liste | `/oteller/istanbul` |
| Harita | `/oteller/harita` |
| Otel detay | `/oteller/{otel-slug}` (ornek: `/oteller/orkestra-pendik-hotel`) |
| Admin rezervasyonlar | `/admin/rezervasyonlar` |
| Partner takvim-fiyat | `/panel/partner/takvim-fiyatlar` |

Admin demo (ayri seed): `ork-demo-admin@otelturizm.local` / `Demo123!` — `/admin-giris`

Misafir demo rezervasyonlari: `ork-demo-misafir@otelturizm.local` — `ORK-ILCE-*` rezervasyon numaralari.

## Veri ozeti

- Otel basina: dedicated partner, `STD-DEMO` + `DLX-DEMO`, 30 gun `ODA_FIYAT_MUSAITLIK`
- Hafta sonu: `INDIRIMLI_FIYAT` = %85 gece fiyati; `KMP-2026-SEHIR` kampanya baglantisi
- Otel ozellikleri: WiFi, otopark, restoran, kahvalti, 24s resepsiyon, ATM
- Ilcede zaten `Yayında` otel varsa ilce atlanir (idempotent)
