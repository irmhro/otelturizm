# Komisyon & Tahsilat Merkezi — Geliştirme Planı

**Orkestra:** `H11_finans_komisyon` (Admin H3 + Partner H2)  
**Wave:** `Wave-VII-20260526` · **Öncelik:** P0  
**Hedef:** Binlerce otel için aylık komisyonları tek listede takip; il/ilçe/mahalle filtre; sıralama; Excel; tahsilat (platformun partner/otelden alacağı) durumu.

---

## Mevcut durum (gap)

| Alan | Admin `/admin/komisyonlar` | Partner `/panel/partner/finans/komisyonlar` |
|------|---------------------------|-----------------------------------------------|
| Aylık otel özeti | Var ama **TOP 15**, geo filtre view’da UI var **servise bağlı değil** | 30 gün KPI + liste (pageSize 50) |
| İl/ilçe/mahalle | Form alanları var, **SQL filtre yok** | Yok |
| Sıralama | Yok (sabit ciro DESC) | Sınırlı |
| Excel/CSV | Sadece `/admin/raporlar/aylik-ciro-komisyon.csv` (aylık özet) | Yok |
| Tahsilat takibi | `OTELE_ODEME_DURUMU` (otele ödeme); **platform tahsilat** ayrı değil | `POST odendi` tek otel |
| Sayfalama | View `pageSize` var, **controller geçirmiyor** | Var |

**Kaynak tablo:** `KOMISYON_MUHASEBE_KAYITLARI` (`DONEM` yyyy-MM, `KOMISYON_TUTARI`, `OTELE_ODEME_DURUMU`, `MUTABAKAT_DURUMU`).

---

## Hedef UX

### Admin — Komisyon Tahsilat Merkezi
- **Route:** `/admin/komisyon-tahsilat` (veya mevcut `/admin/komisyonlar` genişletme)
- **Görünüm:** Tablo (varsayılan) + isteğe bağlı “özet kart” modu
- **Dönem:** Ay seçici (`DONEM` = `2026-05`) + tarih aralığı
- **Filtreler:** İl, ilçe (dropdown `ILLER`/`ILCELER`), mahalle (metin veya API), partner, otel kodu, ödeme durumu, **tahsilat durumu** (Bekliyor / Tahsil edildi / Kısmi / İtiraz)
- **Sıralama:** Ciro, komisyon, bekleyen tahsilat, otel adı, ilçe (tıklanabilir başlık)
- **Sayfalama:** 50 / 100 / 200 / 500 (server-side `OFFSET/FETCH`)
- **Toplamlar:** Seçili filtre için alt bilgi şeridi (toplam komisyon, tahsil edilen, bekleyen)
- **Excel:** `GET /admin/komisyon-tahsilat/export.xlsx` (aynı filtre parametreleri)
- **Aksiyon:** Toplu “tahsil edildi işaretle”, tek satır not, audit log

### Partner — Aylık komisyon özeti
- Mevcut sayfa genişletilir: dönem ayı, indir Excel, sadece kendi otelleri
- Tahsilat durumu salt okunur (platform günceller)

---

## Veri modeli (Faz 1 — migration)

**Dosya:** `Database/MigrationsSql/tablo/migrationlar/20260526_komisyon_tahsilat_alanlari.sql`

| Kolon | Tip | Açıklama |
|-------|-----|----------|
| `PLATFORM_TAHSILAT_DURUMU` | nvarchar(50) | Bekliyor, TahsilEdildi, Kismi, Itiraz |
| `PLATFORM_TAHSILAT_TARIHI` | date NULL | Tahsilat tarihi |
| `PLATFORM_TAHSILAT_REFERANSI` | nvarchar(80) NULL | Dekont / ref |
| `PLATFORM_TAHSILAT_NOTU` | nvarchar(500) NULL | Finans notu |

Idempotent `IF COL_LENGTH` + index `IX_komisyon_donem_otel`.

---

## Teknik görevler (CTO kuyruk)

| ID | Owner | Görev | Çıkış |
|----|-------|-------|-------|
| **T410** | H3 | `GetCommissionCollectionLedgerAsync` — paginated SQL, geo filter, sort | AdminService |
| **T411** | H3 | Admin view `CommissionCollection.cshtml` + CSS/mobile + sidebar | UI |
| **T412** | H3 | Excel export ClosedXML veya CSV UTF-8 BOM | Controller |
| **T413** | H3 | POST tahsilat güncelle (tek + toplu) + audit | API |
| **T414** | H2 | Partner komisyon: dönem filtresi + Excel + mobil tablo | Partner |
| **T415** | H8 | Migration + seed demo tahsilat durumları (ORK oteller) | SQL |

---

## SQL taslağı (otel-ay özet)

```sql
SELECT o.ID, o.OTEL_KODU, o.OTEL_ADI, o.SEHIR, o.ILCE, o.MAHALLE, o.ILCE_ID,
       k.DONEM,
       COUNT(*) rez_adet,
       SUM(k.KOMISYON_TUTARI) brut_komisyon,
       SUM(CASE WHEN k.PLATFORM_TAHSILAT_DURUMU = N'TahsilEdildi' THEN k.KOMISYON_TUTARI ELSE 0 END) tahsil,
       ...
FROM KOMISYON_MUHASEBE_KAYITLARI k
JOIN OTELLER o ON o.ID = k.OTEL_ID
WHERE (@donem = '' OR k.DONEM = @donem)
  AND (@ilceId IS NULL OR o.ILCE_ID = @ilceId)
GROUP BY o.ID, ..., k.DONEM
ORDER BY ... OFFSET @skip ROWS FETCH NEXT @take ROWS ONLY
```

---

## Canlı / stage apply runbook (#033)

**Amaç:** `PLATFORM_TAHSILAT_*` kolonları ve `IX_komisyon_donem_otel` indeksini güvenli ve **idempotent** uygulamak. Scriptler tekrar çalıştırılabilir (`IF COL_LENGTH`, `IF NOT EXISTS`).

### 1. Yedek (zorunlu)

| Adım | Komut / not |
|------|-------------|
| Full backup | `BACKUP DATABASE [otelturizm] TO DISK = N'...\\pre_komisyon_tahsilat_YYYYMMDD.bak' WITH INIT, COMPRESSION, CHECKSUM;` |
| Doğrulama | `RESTORE VERIFYONLY FROM DISK = N'...'` |
| Bakım penceresi | Admin/partner komisyon ekranları kısa süre eski şema ile çalışabilir; apply sırasında tahsilat POST’larını durdurun |

### 2. Önkoşul

- Tablo `KOMISYON_MUHASEBE_KAYITLARI` mevcut olmalı (kaynak: `035_KOMISYON_MUHASEBE_KAYITLARI.sql` veya mevcut canlı şema).
- Uygulama kodu Wave-V dalgasıyla uyumlu (admin `/admin/komisyon-tahsilat`, partner komisyon CSV).

### 3. Script sırası

| Sıra | Dosya | Tür | Not |
|------|--------|-----|-----|
| 1 | `Database/MigrationsSql/tablo/migrationlar/20260526_komisyon_tahsilat_alanlari.sql` | Şema | 4 kolon + `Bekliyor` backfill + `IX_komisyon_donem_otel` |
| 2 | `Database/MigrationsSql/veri/migrationlar/20260526_seed_komisyon_tahsilat_demo.sql` | Seed (opsiyonel) | **Yalnızca demo/stage** — `ORK-%` otellerinde karışık tahsilat durumu |

*Üretimde sıra 2 atlanır.* Genel migration runner (`--run-sql-migrations`) kullanılıyorsa bu dosyalar idempotent oldukları için tekrar güvenlidir.

### 4. Uygulama (örnek)

```powershell
# SSMS / sqlcmd — sırayla
sqlcmd -S <server> -d <database> -i "Database\MigrationsSql\tablo\migrationlar\20260526_komisyon_tahsilat_alanlari.sql"
# Stage/demo:
sqlcmd -S <server> -d <database> -i "Database\MigrationsSql\veri\migrationlar\20260526_seed_komisyon_tahsilat_demo.sql"
```

### 5. Doğrulama sorguları

```sql
-- Kolonlar
SELECT COL_LENGTH('dbo.KOMISYON_MUHASEBE_KAYITLARI', 'PLATFORM_TAHSILAT_DURUMU') AS durum_len,
       COL_LENGTH('dbo.KOMISYON_MUHASEBE_KAYITLARI', 'PLATFORM_TAHSILAT_TARIHI') AS tarih_len;

-- NULL kalmamalı (backfill)
SELECT COUNT(*) AS null_durum
FROM dbo.KOMISYON_MUHASEBE_KAYITLARI
WHERE PLATFORM_TAHSILAT_DURUMU IS NULL;

-- İndeks
SELECT name FROM sys.indexes
WHERE object_id = OBJECT_ID(N'dbo.KOMISYON_MUHASEBE_KAYITLARI')
  AND name = N'IX_komisyon_donem_otel';

-- Dağılım (stage demo sonrası)
SELECT PLATFORM_TAHSILAT_DURUMU, COUNT(*) AS adet, SUM(KOMISYON_TUTARI) AS toplam
FROM dbo.KOMISYON_MUHASEBE_KAYITLARI
GROUP BY PLATFORM_TAHSILAT_DURUMU
ORDER BY adet DESC;
```

**Beklenen:** `null_durum = 0`; indeks satırı 1; admin tahsilat listesi filtre/sıralama hatasız.

### 6. Rollback notu

| Senaryo | Önerilen aksiyon |
|---------|------------------|
| Apply öncesi | Full backup’tan `RESTORE DATABASE` (tercih edilen) |
| Yalnızca şema geri alma | `DROP INDEX IX_komisyon_donem_otel ON dbo.KOMISYON_MUHASEBE_KAYITLARI` ardından kolon `DROP` — **tahsilat verisi kaybolur**; üretimde kullanmayın |
| Demo seed geri alma | `UPDATE ... SET PLATFORM_TAHSILAT_DURUMU = N'Bekliyor' WHERE PLATFORM_TAHSILAT_REFERANSI = N'DEMO-TAHSIL-ORK'` (idempotent temizlik) |

*Koordinatör:* Wave-XII #033 — runbook bu dosyada; canlı apply kullanıcı onayı + yedek sonrası.

---

## Faz 2 (sonraki 5 dk dalgalar)

- Dashboard widget: “Bu ay tahsil edilecek”
- Partner push bildirim tahsilat hatırlatma
- e-Fatura / mutabakat PDF
- API webhook muhasebe

---

## Smoke test

1. Admin → 39 ilçe oteli, dönem bu ay, filtre İstanbul → liste
2. Excel indir → satır sayısı = UI toplam
3. Partner `irmhro0+pendik@gmail.com` → sadece Pendik oteli satırları
4. Tahsilat işaretle → audit + satır güncellenir

*Koordinatör: 5 dk döngüde T410→T415 sırası.*
