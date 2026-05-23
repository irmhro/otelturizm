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

Dogrulama:

```powershell
sqlcmd -S $srv -d $db -Q "SELECT COUNT(*) AS IstanbulYayinda FROM OTELLER o INNER JOIN ILCELER c ON c.ID=o.ILCE_ID INNER JOIN ILLER i ON i.ID=c.IL_ID WHERE i.IL_ADI LIKE N'%stanbul%' AND o.YAYIN_DURUMU=N'Yayında'"
```

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
