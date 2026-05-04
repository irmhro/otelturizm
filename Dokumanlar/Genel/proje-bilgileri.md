# Proje Bilgileri

## Veritabanı
- Sunucu/Ortam: `(localdb)\MSSQLLocalDB`
- Veritabanı adı: `otelturizm_2026db`
- Sağlayıcı: `Microsoft SQL Server / LocalDB`
- Not: Geliştirme ortamında HeidiSQL, Laragon ve MySQL kullanılmaz.

## Migration Mantığı (SQL Script Tabanlı)
- Script klasörü: `Database/MigrationsSql`
- Migration dosyaları tablo adı bazlı ayrıldı (`create_table_<tablo>`, `seed_<tablo>`).
- Uygulama başlangıcında `SqlMigrationRunner` otomatik kapalıdır (`RunMigrationsOnStartup = false`).
- Migration'lar kontrollü olarak SQL Server Management Studio veya Visual Studio SQL araçları ile uygulanır.
- Uygulanan scriptler MSSQL tarafında takip edilir; runtime başlangıcında otomatik MySQL-style script yürütme yoktur.
- Aynı script tekrar çalıştırılmadan önce MSSQL uyumu ve idempotent davranışı kontrol edilir.

## Split Migration Dosya Düzeni
- `001_bootstrap_set_names.sql`
- `002_bootstrap_fk_checks_off.sql`
- `003_create_table_roller.sql` ... `041_create_table_api_loglari.sql`
- `042_seed_roller_01.sql` ... `061_seed_bildirim_sablonlari_01.sql`
- `062_bootstrap_fk_checks_on.sql`
- Eski tek dosya arşivi: `Database/MigrationsSql/_archive/001_initial_schema.sql`

## Yenileme (Tablo bozulursa)
1. SSMS veya Visual Studio SQL araçlarında `otelturizm_2026db` veritabanını kontrol et.
2. Gerekirse LocalDB üzerinde aynı adıyla tekrar oluştur.
3. MSSQL uyumlu migration scriptlerini sırasıyla uygula.
4. Uygulamayı yeniden başlatıp bağlantıyı doğrula.

## Önemli Geçiş Notu
- Eğer eski tek dosyalı migration daha önce çalıştırıldıysa, split migration'a geçerken en güvenlisi veritabanını yeniden oluşturmaktır.
- Alternatif olarak yalnızca geliştirme ortamında `schema_migrations` tablosu temizlenip yeniden kurulum yapılabilir.

## Sonraki Migration Kuralı
- Yeni değişiklikleri mevcut scripti düzenleyerek değil, yeni dosya açarak ekle:
  - `063_...sql`
  - `064_...sql`

## Geliştirme Notu: Partner Rol ve Sahiplik Modeli
- `users` tablosunda artık her kullanıcının uygulama seviyesi rolü tutulur:
  - `user`
  - `admin`
  - `partner_owner`
  - `partner_manager`
  - `partner_staff`
- `users.sahiplik_partner_id` kolonu, kullanıcının ana partner hesabını belirtir.
- `oteller.user_id` kolonu, otelin birincil sorumlu kullanıcısını tutar.
- Bir otelin birden fazla kullanıcı tarafından yönetilmesi için `otel_kullanici_sahiplikleri` tablosu kullanılır.
- `otel_kullanici_sahiplikleri` içinde:
  - `otel_id`
  - `user_id`
  - `partner_id`
  - `rol` (`owner`, `manager`, `staff`)
  - `ana_sorumlu_mu`
  - `aktif_mi`
  tutulur.
- Partner paneli erişimi sadece `users_partner` ile değil, yeni rol ve otel sahiplik modeli ile birlikte değerlendirilir.
- Kullanıcı girişinde oturuma claim olarak şu bilgiler yazılır:
  - kullanıcı tipi
  - kullanıcı rolü
  - sahip olduğu partner id
  - yönetebildiği otel id listesi

## Geliştirme Notu: Migration Dosyaları
- `085_alter_users_add_role_and_partner_ownership.sql`
  - `users.rol`
  - `users.sahiplik_partner_id`
  - partner ve admin geri doldurma
- `086_alter_oteller_add_user_and_hotel_user_ownerships.sql`
  - `oteller.user_id`
  - `otel_kullanici_sahiplikleri`
  - mevcut partner-kullanıcı ilişkilerinden otel sahipliği geri doldurma

## MSSQL Geçiş Notu
- Repo artık tek veritabanı standardı olarak MSSQL/LocalDB kullanır.
- MySQL, MariaDB, HeidiSQL ve Laragon referansları yalnızca tarihsel not veya arşiv dosyalarında tutulabilir; aktif runtime için geçerli değildir.
- Yeni migration ve sorgular SQL Server söz dizimi ile yazılacaktır.

## Studio ve Yayın Referansı
- Local geliştirme ve yayın akışı için ana operasyon dosyası:
  - `yayin-ve-ortam-ayarlari.md`
- Local test adresi:
  - `https://localhost:7223`
- Local geliştirme veritabanı:
  - `185.111.244.246`
  - `otelturizm_2026db`
- `Development` ve `Production` connection stringleri canlı MSSQL hedefi ile eşitlenmiştir.
- Canlı yayımlama öncesi local doğrulama tamamlanmadan publish yapılmaz.

