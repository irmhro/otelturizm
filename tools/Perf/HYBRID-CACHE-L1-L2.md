# Hybrid cache (L1 bellek + L2 Redis) — paket 241

## Mevcut durum

- **L1:** `IMemoryCache` yaygın kullanımda (ör. quote shield, otel detay önbelleği).
- **Stampede koruması:** `CacheSingleFlight` ile aynı anahtar için tek üretici (double-load önleme).

## Hedef (ileride Redis)

1. `IDistributedCache` (Redis) ile L2; anahtar formatı ortam önekli (`prod:otel:…`).
2. Okuma sırası: L1 hit → L2 hit → kaynak → L2 + L1 yaz.
3. Invalidation: OutputCache tag evict ile uyumlu event tetikleri (otel fiyat güncellemesi).

Tam Redis entegrasyonu için ortamda `ConnectionStrings:Redis` ve paket seçimi (StackExchange.Redis) ayrı iş paketidir.
