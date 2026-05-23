# Demo otel görselleri ve özellikler

## Klasör yapısı (canlı ile aynı)

```
wwwroot/uploads/images/{otelId}/hotel/demo-cover.webp
wwwroot/uploads/images/{otelId}/hotel/demo-01.webp … demo-03.webp
wwwroot/uploads/images/{otelId}/rooms/{odaTipId}/demo-room-cover.webp
wwwroot/uploads/images/{otelId}/rooms/{odaTipId}/demo-room-02.webp
```

Kaynak: [Picsum Photos](https://picsum.photos) (ücretsiz placeholder), WebP dönüşümü `tools/DemoImageSeed`.

`DemoImageSeed` DB'den `ORK-IST-%` ve `ORK-SEED-%` otellerini okur; otel başına ilk **2** `ODA_TIPLERI` için oda görselleri indirir.

```powershell
# Tek otel / kod listesi
dotnet run --project tools\DemoImageSeed\DemoImageSeed.csproj -c Release -- --root=D:\otelturizm --codes=ORK-IST-PENDIK,ORK-SEED-001
```

## Tek komut — 10 ilçe (eski seed)

```powershell
powershell -File tools\Db\Install-DemoHotelMedia.ps1
```

## Tek komut — İstanbul 39 ilçe demo

Sıra: görsel (mevcut DB) → istanbul tam otel seed → (isteğe bağlı eski 10 ilçe) → medya/özellik SQL → görsel tekrar (tüm `ORK-IST` + `ORK-SEED`).

```powershell
powershell -File tools\Db\Install-IstanbulIlceDemo.ps1
# veya
powershell -File tools\Db\Install-IstanbulIlceDemo.ps1 -Server "(localdb)\MSSQLLocalDB" -Database otelturizm_2026db -IncludeLegacy10Ilce
```

### SQL seed sırası (39 ilçe)

1. `20260526_seed_istanbul_ilce_tam_oteller.sql` (veya eşdeğer `20260526_seed_istanbul_39_ilce_*.sql`) — 39 ilçe `ORK-IST-*` otelleri
2. `20260526_seed_istanbul_ilce_medya_ozellik.sql` — `OTEL_OZELLIK_ILISKILERI`, `ODA_TIPI_OZELLIKLERI`, `OTEL_GORSELLERI`, `ODA_GORSELLERI` (yol: `/uploads/images/{otelId}/...`)
3. İsteğe bağlı: `20260523_seed_istanbul_10_ilce_oteller.sql` (`-IncludeLegacy10Ilce`) — `ORK-SEED-001`…`010`

Eski 10 ilçe / 3 otel hızlı patch:

- `20260523_seed_istanbul_006_010_quick.sql` / `20260523_seed_istanbul_007_010_inserts.sql`
- `20260523_seed_demo_otel_medya_ve_ozellikler.sql` — yalnızca `ORK-SEED-*`

## Yayın

```powershell
powershell -File tools\Release\Publish-To-23052026.ps1
```

Çıktı: `D:\otelturizm\23.05.2026\`
