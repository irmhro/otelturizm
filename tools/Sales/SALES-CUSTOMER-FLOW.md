## Satış Paneli — Müşteri Akışı Standardı (Paket 160)

### Amaç

Satış ekibinin müşteri oluşturma → teklif/rezervasyon → ödeme takibi akışını tek standartta tutmak.

### Önerilen akış

1. **Müşteri seç/oluştur**
2. **Otel/oda/tarih seçimi**
3. **Fiyat & ödeme planı**
4. **Rezervasyon oluştur**
5. **Dokümanlar / PDF / e-posta**

### Guard ve audit

- Her satış işlemi `RESERVATION_AUDIT` (create/update/cancel) zincirine bağlı olmalı.
- Müşteri verisi sadece sales/admin erişimine açık olmalı.

