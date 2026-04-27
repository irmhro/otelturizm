# Native AOT uyumluluk özeti — paket 245

Bu proje şu an **JIT** (`net10.0` web) olarak yayınlanır.

## AOT’a geçişte riskli alanlar

| Alan | Not |
|------|-----|
| Razor view derlemesi | Publish-time compile zaten kullanılıyor; runtime reflection azaltılmalı |
| EF/Dapper özel mapper | Reflection-heavy mapper’lar elenmeli |
| ImageSharp | Native AOT için Paket dokümantasyonunu doğrula |

## Öneri

Önce **trim analysis** ile uyarıları sıfırlayın; sonra `PublishAot` deneme profili yalnızca staging’de.
