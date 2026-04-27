# SQL Index Kullanım Doğrulama Checklist’i (p69)

## Hedef

Kritik sayfalarda (otel listeleme, otel detay, firma deals, partner fiyat ekranları) planların **index seek** kullandığını doğrulamak ve gereksiz scan’leri azaltmak.

## Pratik adımlar (SQL Server)

- `SET STATISTICS IO, TIME ON;` ile kritik sorguları çalıştır.
- `Actual Execution Plan` aç ve şunları kontrol et:
  - Index **Seek** oranı (özellikle tarih aralığı ve otel_id/oda_tip_id filtrelerinde)
  - “Key Lookup” yoğunluğu (gerekirse include column)
  - “Spill to tempdb” / sort uyarıları
- Parametre sniffing şüphesi varsa aynı sorguyu farklı parametrelerle tekrar çalıştır ve planı karşılaştır.

## Yardımcı rapor

- `tools/Db/sqlserver_top_slow_queries.sql`
  - Not: `VIEW SERVER STATE` izni gerektirebilir.

