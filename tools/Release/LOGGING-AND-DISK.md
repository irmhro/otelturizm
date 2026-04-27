## Paket 195 — Log rotasyonu ve disk izleme

### Uygulama (Serilog)

`Program.cs`: günlük dosya `App_Data/logs/app-*.json`, `rollingInterval: Day`, `retainedFileCountLimit: 14`.

### Operasyon

- Disk dolması: log klasörü için disk alarmı (ör. %85 üzeri).
- Uzun süreli arşiv: gerekiyorsa günlük dosyaları güvenli depolamaya (S3/Blob) taşıyan ayrı iş.

### Kontrol

- [ ] Prod’da `App_Data/logs` yazılabilir ve yeterli disk var.
- [ ] Retention ihtiyaçla uyumlu (uyumluluk için daha uzun süre gerekebilir).
