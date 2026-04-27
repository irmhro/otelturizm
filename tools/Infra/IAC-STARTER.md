# IaC başlangıç planı (paket 222)

Üretim ortamları için hedef durum:

- Konfigürasyon tek gerçek kaynak değildir; **Kaynak kod + ortam değişkenleri + gizli kasa** birlikte IaC oluşturur.
- ASP.NET için `appsettings.{Environment}.json` veya ortam önekli değişkenler (`Growth__KillSwitchAll` vb.) kullanın; repo içinde düz metin secret bırakmayın.
- SQL şema değişiklikleri `Database/MigrationsSql/*.sql` ile takip edilir; uygulama `Archive:Reservations` gibi özellik bayrakları ile işleri açıp kapatabilir.

Önerilen sıra: dev/staging/prod parametreleri → ARM/Bicep/Terraform ile kaynak grubu tanımı (isteğe bağlı) → pipeline’da migration + smoke.
