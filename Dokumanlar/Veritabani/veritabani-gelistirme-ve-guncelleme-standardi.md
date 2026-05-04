# Veritabanı Geliştirme ve Güncelleme Standardı

Bu doküman, `otelturizm_2026db` üzerinde yeni tablo, kolon, index, constraint, seed ve veri dönüşümü yapılırken izlenecek zorunlu standardı tanımlar. Amaç canlı veriyi koruyarak şema geliştirmektir.

## 1. Temel Kural

- Canlıda veri varsa tablo yeniden kurulmaz.
- `DROP TABLE`, `TRUNCATE TABLE`, koşulsuz `DELETE` ve kör toplu `UPDATE` yaklaşımı kullanılmaz.
- Önce şema evriltilir, sonra gerekiyorsa kontrollü veri taşıma yapılır.

Doğru yaklaşım:
- `ALTER TABLE`
- eksik kolon ekleme
- kontrollü `UPDATE`
- `IF NOT EXISTS` ile idempotent migration
- doğrulama sorguları

Yanlış yaklaşım:
- tabloyu silip yeniden oluşturmak
- mevcut satırları yedeksiz silmek
- duplicate veri temizlemeden unique index koymak
- uygulama kodu hazır olmadan şemayı tek başına ileri almak

## 2. Geliştirme Türleri

### 2.1 Sadece Şema Geliştirmesi

Örnek:
- yeni kolon ekleme
- yeni index ekleme
- yeni tablo ekleme
- yeni foreign key ekleme

Bu durumda:
1. migration yazılır
2. localde çalıştırılır
3. doğrulama yapılır
4. canlıya uygulanır

### 2.2 Şema + Veri Dönüşümü

Örnek:
- eski bir kolondan yeni kolona veri taşımak
- status metinlerini normalize etmek
- boş kanalları `email` ile doldurmak

Bu durumda:
1. şema hazırlanır
2. etkilenecek kayıtlar `SELECT` ile ölçülür
3. kontrollü `UPDATE` yazılır
4. sonuç sayısı doğrulanır

### 2.3 Seed / Başlangıç Verisi

Örnek:
- hazır `fiyat_indirimleri`
- platform email hesapları
- varsayılan ayarlar

Bu durumda:
- seed script tekrar çalışsa da duplicate oluşturmamalıdır
- `IF NOT EXISTS` veya doğal anahtar kontrolü kullanılmalıdır

## 3. Dosya Yapısı ve İsimlendirme

Migration dosyaları:
- `D:\otelturizm\Database\MigrationsSql`

İsim standardı:
- tarih ile başlar
- ne yaptığını açıkça söyler

Örnek:
- `20260428_create_platform_email_center_tables.sql`
- `20260428_normalize_email_delivery_status_tracking.sql`
- `20260428_enforce_default_email_2fa_channel.sql`

## 4. Migration Yazım Kuralları

### 4.1 Idempotent olmalı

Aynı script ikinci kez çalışırsa sistemi bozmamalıdır.

Örnek:

```sql
IF COL_LENGTH('dbo.users', 'iki_asamali_dogrulama_kanali') IS NULL
BEGIN
    ALTER TABLE dbo.users
    ADD iki_asamali_dogrulama_kanali NVARCHAR(50) NULL;
END
```

### 4.2 Veri koruyucu olmalı

Önce yeni alan ekle, sonra veri taşı:

```sql
IF COL_LENGTH('dbo.bildirim_loglari', 'email_servis_kodu') IS NULL
BEGIN
    ALTER TABLE dbo.bildirim_loglari
    ADD email_servis_kodu NVARCHAR(100) NULL;
END

UPDATE dbo.bildirim_loglari
SET email_servis_kodu = 'default_smtp'
WHERE email_servis_kodu IS NULL
  AND tur = 'email';
```

### 4.3 Transaction kullanımı

Büyük veri güncellemelerinde transaction tercih edilir, ancak uzun kilit riskine dikkat edilir.

```sql
BEGIN TRY
    BEGIN TRAN;

    -- kontrollü update

    COMMIT TRAN;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRAN;
    THROW;
END CATCH
```

### 4.4 Constraint eklemeden önce veri temizliği

Örnek:
- `users.eposta` unique yapılacaksa önce duplicate kayıtlar temizlenir
- sonra unique index eklenir

## 5. Canlı Veri Varken İzlenecek Sıra

1. Geliştirme ihtiyacını yaz
2. Etkilenen tabloları listele
3. Risk sınıfını belirle
4. Local veritabanında scripti çalıştır
5. Sonuç sorgularını doğrula
6. Geri dönüş planını hazırla
7. Canlıdan yedek al
8. Canlıya scripti uygula
9. Uygulama loglarını kontrol et
10. Kritik ekranları test et

Ek operasyon kuralı:
- Kullanıcı kod deploy'unu kendisi yapacak olsa bile, canlı hata nedeni veritabanı şema/veri farkı ise ilgili DB güncellemesi ayrıca uygulanır.
- Özellikle login, panel erişimi, rezervasyon ve e-posta gibi kritik akışlarda DB geliştirmesi localde bırakılmaz; canlı şema/veri de aynı seviyeye getirilir.

## 6. Risk Sınıfları

### Düşük Risk
- yeni tablo
- nullable yeni kolon
- yeni log tablosu
- yeni index

### Orta Risk
- mevcut kolonu backfill etmek
- default değer atamak
- null olmayan kolona dönüşüm hazırlığı

### Yüksek Risk
- unique index
- foreign key ekleme
- kolon tipi değiştirme
- mevcut veriyi toplu dönüştürme
- büyük seed merge işlemleri

Yüksek riskli işlerde:
- yedek zorunlu
- önce local doğrulama zorunlu
- mümkünse canlı bakım penceresinde uygulanmalı

## 7. Canlıya Uygulama Öncesi Zorunlu Kontroller

- [ ] Script localde hatasız çalıştı
- [ ] Etkilenen kayıt sayısı ölçüldü
- [ ] Doğrulama sorguları hazır
- [ ] Geri dönüş planı yazıldı
- [ ] Canlı yedek alındı
- [ ] Uygulama kodu yeni şemaya uyumlu

## 8. Yedek Alma Zorunluluğu

Kritik işlemlerden önce:
- full backup
- ya da en azından etkilenen tabloların export’u alınır

Özellikle zorunlu:
- `UPDATE`
- `DELETE`
- unique index ekleme
- constraint ekleme
- kolon tipi değiştirme

## 9. Canlıya Uygulama Komut Şablonu

```powershell
sqlcmd -S "185.111.244.246" -d "otelturizm_2026db" -U "sa" -P "PAROLA" -i "D:\otelturizm\Database\MigrationsSql\DOSYA.sql" -W -w 240
```

Birden fazla dosya varsa:
- tarih sırasına göre
- tek tek
- her biri sonrası doğrulama ile

## 10. Doğrulama Sorguları

### 10.1 Kolon kontrolü

```sql
SELECT COLUMN_NAME
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'users'
  AND COLUMN_NAME = 'iki_asamali_dogrulama_kanali';
```

### 10.2 Index kontrolü

```sql
SELECT name
FROM sys.indexes
WHERE object_id = OBJECT_ID('dbo.users')
  AND name = 'UX_users_eposta';
```

### 10.3 Veri güncelleme sonucu

```sql
SELECT COUNT(*) AS toplam
FROM dbo.users
WHERE iki_asamali_dogrulama_kanali = 'email';
```

## 11. Uygulama Kodu ile Senkron Kuralı

Veritabanı şeması tek başına ileri alınmaz. Kodla birlikte düşünülür.

Örnek doğru akış:
1. yeni kolon migration
2. uygulama kodu yeni kolonu okuyacak hale gelir
3. eski alan kullanım dışı bırakılır

Örnek yanlış akış:
1. canlıda kolon tipi değiştirilir
2. kod hâlâ eski tipe göre çalışır
3. sayfalar `500` verir

## 12. Seed ve Konfigürasyon Verisi İçin Kural

Konfigürasyon tablolarında:
- aynı doğal anahtar ikinci kez açılmamalı
- seed kayıtları tekrar çalıştırılabilir olmalı

Örnek:
- `email_services.servis_kodu`
- `platform_email_hesaplari.eposta`
- `fiyat_indirimleri.indirim_adi`

## 13. Yasaklı İşlemler

Aşağıdaki işlemler onaysız yapılmaz:
- `DROP TABLE`
- `DROP COLUMN`
- `TRUNCATE TABLE`
- `DELETE FROM tablo` koşulsuz
- tüm kullanıcıları etkileyen toplu `UPDATE` koşulsuz

Bu işlemler gerekiyorsa:
- ayrı risk notu yazılır
- yedek alınır
- geri dönüş planı hazırlanır

## 14. Operasyon Sonrası Kontrol Listesi

- [ ] Script hatasız tamamlandı
- [ ] Doğrulama sorguları geçti
- [ ] Uygulama açılış logunda SQL hatası yok
- [ ] Admin ekranları açılıyor
- [ ] Partner ekranları açılıyor
- [ ] Public kritik rotalar çalışıyor
- [ ] Etkilenen özellik manuel test edildi

## 15. Bu Proje İçin Sabit Kurallar

- Veritabanı standardı: SQL Server / MSSQL
- Migration yöntemi: SQL script + `sqlcmd`
- EF migration kullanılmıyor
- Canlı veri varsa önce koruma, sonra geliştirme
- `1 email = 1 kullanıcı` gibi veri bütünlüğü kuralları DB seviyesinde desteklenmeli

## 16. İlgili Diğer Dokümanlar

- `canli-veritabani-yukleme-kilavuzu.md`
- `yayin-ve-ortam-ayarlari.md`
- `canli-baglanti-bilgileri.md`
- `mssql-gecis-kontrol-raporu.md`
