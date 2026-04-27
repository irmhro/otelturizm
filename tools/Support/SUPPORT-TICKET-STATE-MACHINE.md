## Destek Ticket — SLA + Durum Makinesi (Paket 156)

### Durumlar

- **New** → yeni açıldı
- **Triaged** → etiketlendi/önceliklendirildi
- **WaitingCustomer** → müşteriden bilgi bekleniyor
- **InProgress** → işlemde
- **Resolved** → çözüldü
- **Closed** → kapandı

### SLA alanları (öneri)

- `priority`: Low / Normal / High / Critical
- `first_response_due_utc`
- `resolution_due_utc`
- `first_response_at_utc`
- `resolved_at_utc`

### Kurallar

- Priority yükseldikçe first response ve çözüm süresi kısalır.
- WaitingCustomer durumunda SLA “pause” opsiyonu düşünülür.

