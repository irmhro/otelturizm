# Kalan DB Uyum — Data, Middleware, BackgroundServices, Filters

**Tamamlanma:** 2026-05-22 ✅

---

## Kapsam

| Alan | Dosya sayısı (yaklaşık) | SQL | Durum |
|------|-------------------------|-----|-------|
| `Data/` | `SqlMigrationRunner.cs` + yardımcılar | Migration yolu | ✅ |
| `Middleware/` | Oturum/güvenlik | Service / yok | ✅ |
| `Filters/` | Yetkilendirme | Service / yok | ✅ |
| `Services/*BackgroundService*.cs` | Arşiv, e-posta, fiyat retention | Literal / SP | ✅ |

---

## Kontroller

- [x] `Data/SqlMigrationRunner` sırası: `tablo/migrationlar` → `constraints` → `veri/migrationlar`
- [x] Background servislerde `users` / `hotels` / küçük harf `FROM oteller` yok
- [x] `TableExistsAsync` / `ColumnExistsAsync` çağrıları çözümlenen tablo adıyla (`dbo.OTELLER`, `dbo.KULLANICILAR`, …)
- [x] Build: 0 hata

---

## Bilinen backlog (kod dışı / düşük risk)

| Madde | Not |
|-------|-----|
| `ReservationsArchiveBackgroundService` | `rezervasyonlar_archive` — DBA ile planlı; log metni |
| `PricingRetentionBackgroundService` | `dbo.usp_fiyat_musaitlik_retention_cleanup` — SP adı olduğu gibi |
| Mahalle `ENLEM`/`BOYLAM` seed | Dev ortamında migration seed ile doğrulanmalı |

---

## Değişiklik günlüğü

| Tarih | Not |
|-------|-----|
| 2026-05-22 | Tüm kalemler tamamlandı |
