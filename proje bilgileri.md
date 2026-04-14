# Proje Bilgileri

## Veritabanı
- Sunucu/Ortam: Laragon (HeidiSQL)
- Veritabanı adı: `otelturizmnew`
- Collation: `utf8mb4_turkish_ci`
- Not: Veritabanı başarıyla oluşturuldu.

## Migration Mantığı (SQL Script Tabanlı)
- Script klasörü: `Database/MigrationsSql`
- Migration dosyaları tablo adı bazlı ayrıldı (`create_table_<tablo>`, `seed_<tablo>`).
- Uygulama başlangıcında `SqlMigrationRunner` otomatik kapalıdır (`RunMigrationsOnStartup = false`).
- Migration'lar kontrollü olarak HeidiSQL veya local MySQL komutu ile uygulanır.
- Uygulanan scriptler `schema_migrations` tablosunda tutulur.
- Aynı script tekrar çalıştırılmaz; içeriği değişirse güvenlik için hata verir.

## Split Migration Dosya Düzeni
- `001_bootstrap_set_names.sql`
- `002_bootstrap_fk_checks_off.sql`
- `003_create_table_roller.sql` ... `041_create_table_api_loglari.sql`
- `042_seed_roller_01.sql` ... `061_seed_bildirim_sablonlari_01.sql`
- `062_bootstrap_fk_checks_on.sql`
- Eski tek dosya arşivi: `Database/MigrationsSql/_archive/001_initial_schema.sql`

## Yenileme (Tablo bozulursa)
1. HeidiSQL'de `otelturizmnew` veritabanını sil.
2. Aynı adla tekrar oluştur (`utf8mb4_turkish_ci`).
3. Uygulamayı yeniden başlat.
4. Migration scriptleri sırasıyla yeniden yüklenir.

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

## MySQL 8.4 Uyum Notu
- Partition + Foreign Key birlikte hata verdiği için migration dosyalarındaki PARTITION BY RANGE blokları kaldırıldı.
- Partition zorunluluğu olursa ileride FK yapısı yeniden tasarlanarak ayrı migration ile eklenecek.

