# Models Geliştirme Takibi

Yeni MSSQL şeması (BÜYÜK HARF tablo/sütun, adres hiyerarşisi `ULKELER` → `ILLER` → `ILCELER` → `MAHALLELER`) ile `Models/` altındaki C# tiplerinin hizalanması.

**Tamamlanma:** 2026-05-22 — **100%** ✅

## Referanslar

| Kaynak | Açıklama |
|--------|----------|
| [tools/Db/schema_name_mapping.json](../tools/Db/schema_name_mapping.json) | Eski → yeni tablo/sütun eşlemesi |
| [Database/MigrationsSql/tablo/migrationlar/](../Database/MigrationsSql/tablo/migrationlar/) | Tablo CREATE/ALTER gerçeği |
| [Services/Abstractions/IAddressLookupService.cs](../Services/Abstractions/IAddressLookupService.cs) | Adres lookup DTO'ları |

## Kurallar

- **EF entity yok** — `Models` çoğunlukla ViewModel/DTO; `[Column]` yalnızca doğrudan satır yansıyan tiplerde.
- **Adres ID:** `UlkeId`, `IlId`, `IlceId`, `MahalleId`; misafir: `MisafirUlkeId`, `MisafirIlId`, … (`MISAFIR_*_ID`).
- Metin alanları (`City`, `District`, …) UI uyumluluğu için korunur.
- Saf API/UI DTO'lar: şema değişikliği gerekmez → ✅.

## Özet

| Metrik | Değer |
|--------|-------|
| Dosya (plan listesi) | 51 |
| Tamamlanan | **51** ✅ |
| Adres ID eklenen / doğrulanan | Reservations, Register, User/Partner/Admin/Satis panelleri, Oteller |

## Dosya durumu (kısa)

Tüm satırlar **✅** — rezervasyon/ödeme kod sabitleri `dbo.ODEME_*_TANIMLARI` ile uyumlu; `ReservationDraftModels` `GuestUlkeId` … `GuestMahalleId`; panel kayıt modellerinde `UlkeId` zinciri.

_Son güncelleme: 2026-05-22 — tamamlandı._
