# Medya Yükleme ve Dizinleme Standardı

Bu doküman, otel/oda görselleri ile dosya yüklemelerinin **wwwroot altında hangi dizinde** tutulduğunu ve uygulama içinde **hangi URL formatının** üretildiğini standartlaştırır.

## 1) Temel Kural

- Tüm medya varlıkları `wwwroot/uploads/` altında tutulur.
- Dizinleme ana anahtarları:
  - **otel id**
  - (oda görsellerinde ek olarak) **oda id**
- Veritabanında mümkün olduğunca **tam URL yerine göreli yol** saklanır. Örn: `/uploads/images/73/hotel/otel-73-1.webp`

## 2) Otel Görselleri

### Fiziksel dizin (disk)

`D:\otelturizm\wwwroot\uploads\images\{otelId}\hotel\`

Örnek:

- `D:\otelturizm\wwwroot\uploads\images\73\hotel\otel-73-1.webp`

### Public URL

`/uploads/images/{otelId}/hotel/{fileName}`

Örnek:

- `/uploads/images/73/hotel/otel-73-1.webp`

## 3) Oda Görselleri

### Fiziksel dizin (disk)

`D:\otelturizm\wwwroot\uploads\images\{otelId}\rooms\{roomId}\`

Örnek:

- `D:\otelturizm\wwwroot\uploads\images\73\rooms\182\oda-182-1.webp`

### Public URL

`/uploads/images/{otelId}/rooms/{roomId}/{fileName}`

Örnek:

- `/uploads/images/73/rooms/182/oda-182-1.webp`

## 4) Otel Dosyaları (PDF, sözleşme, ekler vb.)

### Fiziksel dizin (disk)

`D:\otelturizm\wwwroot\uploads\file\{otelId}\{kategori}\`

Örnek:

- `D:\otelturizm\wwwroot\uploads\file\73\sozlesme\sozlesme-73.pdf`

Kategori segmenti güvenli hale getirilir (harf/rakam dışı karakterler `-` olur).

## 5) Uygulama Tarafı (Kaynak)

Bu dizinleme standardının tek referans noktası:

- `Services/MediaStoragePaths.cs`

Partner/Admin yükleme akışları da görsel URL’leri bu standarda göre üretip DB’ye yazar.

## 6) Demo/Test Veri Uyumluluğu

Demo/test verilerinde bazı kayıtlarda görsel alanları sadece **dosya adı** olarak kalmış olabilir (örn. `otel-73-1.webp`).

Public sayfalarda bu durum için fallback kuralı:

- Eğer değer `/` içermiyorsa **dosya adı** kabul edilir ve URL otomatik olarak:
  - otel için: `/uploads/images/{otelId}/hotel/{fileName}`
  - oda için: `/uploads/images/{otelId}/rooms/{roomId}/{fileName}`
  formatına çevrilir.

## 7) Public Sayfalarda Görsel Çalışma Mantığı

### Otel kartları

Anasayfa ve otel listeleme kartlarında ana görsel şu sırayla seçilir:

1. `oteller.kapak_fotografi`
2. `otel_gorselleri.gorsel_url` içinden kapak/öne çıkan/sıralama önceliğine göre ilk kayıt
3. Hiçbiri yoksa placeholder/fallback alanı

Bu nedenle dosya diskte olsa bile DB’de `oteller.kapak_fotografi` boşsa ve `otel_gorselleri` kaydı yoksa public sayfada görsel gelmez.

### Otel detay galerisi

Otel detay sayfasında ana görsel ve galeri şu alanlardan beslenir:

- `oteller.kapak_fotografi` -> `HotelDetailPageViewModel.MainImageUrl`
- `otel_gorselleri.gorsel_url` -> `HotelDetailPageViewModel.GalleryImages`

`otel_gorselleri.onay_durumu` değeri `Onaylan%` filtresini karşılamalıdır. Türkçe karakter riskini azaltmak için demo seed/migration kayıtlarında `Onaylandi` kullanılır.

### Oda kartları

Oda kartı ve oda detay popup görselleri şu alanlardan beslenir:

- `oda_tipleri.kapak_fotografi`
- `oda_gorselleri.gorsel_url`
- `oda_tipleri.galeri` JSON/list alanı

Oda görseli diskte olsa bile `oda_tipleri.kapak_fotografi` veya `oda_gorselleri` kayıtları yoksa oda kartında görsel görünmez.

## 8) Kontrol Sırası

Görsel diskte ve DB’de var denmesine rağmen public sayfada görünmüyorsa şu sırayla kontrol edilir:

1. Uygulamanın çalıştığı gerçek DB bağlantısı kontrol edilir. Local DB dolu olup `appsettings.Development.json` canlı DB’ye bağlıysa sayfa canlı DB’deki boş kayıtları gösterir.
2. Statik dosya doğrudan açılır: `/uploads/images/{otelId}/hotel/{fileName}` veya `/uploads/images/{otelId}/rooms/{roomId}/{fileName}` HTTP 200 dönmelidir.
3. Otel için `oteller.kapak_fotografi` ve `otel_gorselleri.gorsel_url` kayıtları aynı public URL formatında olmalıdır.
4. Oda için `oda_tipleri.kapak_fotografi` ve `oda_gorselleri.gorsel_url` kayıtları aynı public URL formatında olmalıdır.
5. Sayfa HTML çıktısında `<img src="...">` gerçekten `/uploads/images/...` basıyor mu kontrol edilir. `src=""` veya placeholder varsa sorun servis/DB tarafındadır; 404 varsa sorun dosya yolu/statik sunum tarafındadır.

## 9) Demo Otel 73 Görsel Senkronu

Demo otel için referans migration:

- `Database/MigrationsSql/20260430_sqlserver_fix_demo_hotel_73_images.sql`

Bu script:

- `oteller.id=73` için kapak fotoğrafını `/uploads/images/73/hotel/demo-hotel-01.webp` yapar.
- `otel_gorselleri` tablosuna 20 otel görselini ekler.
- `oda_tipleri.id=182` için kapak fotoğrafını `/uploads/images/73/rooms/182/demo-room-01.webp` yapar.
- `oda_gorselleri` tablosuna 15 oda görselini ekler.

Son doğrulama sorguları:

```sql
SELECT id, kapak_fotografi
FROM dbo.oteller
WHERE id = 73;

SELECT COUNT(*) AS hotel_images
FROM dbo.otel_gorselleri
WHERE otel_id = 73;

SELECT id, kapak_fotografi
FROM dbo.oda_tipleri
WHERE id = 182;

SELECT COUNT(*) AS room_images
FROM dbo.oda_gorselleri
WHERE oda_tip_id = 182;
```
