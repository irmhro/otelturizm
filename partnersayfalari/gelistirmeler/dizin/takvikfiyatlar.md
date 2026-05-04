# Takvim & Fiyatlar Geliştirme Notları

Bu doküman, `panel/partner/takvim-fiyatlar` sayfasında indirim + kampanya birlikte çalışacak yapının geliştirme adımlarını içerir.

## Hedef

- Oda bazlı fiyat yönetiminde normal fiyat, indirimli fiyat, indirim türü ve kampanya ilişkisini birlikte yönetmek.
- Partner kullanıcısının indirimli fiyat girerken `fiyat_indirimleri` tablosundan seçim yapabilmesini sağlamak.
- Gerekli olduğunda kampanya seçimini de aynı ekranda sunmak.

## Uygulanan Adımlar

1. `PartnerPricingPageViewModel` içine `AvailableCampaigns` alanı eklendi.
2. `PartnerBulkPricingUpdateRequest` ve `PartnerDailyPricingUpdateRequest` içine `CampaignId` alanı eklendi.
3. `PartnerService.GetPricingAsync(...)` içinde aktif kampanyalar yüklenip modelde taşındı.
4. `PartnerService.ApplyBulkPricingAsync(...)` içinde:
   - İndirimli fiyat girildiğinde sadece indirim değil kampanya seçimi de kabul edilir hale getirildi.
   - `CampaignId` doğrulaması eklendi.
   - `kampanya_id` yazımı kampanya seçimini önceleyecek şekilde güncellendi.
   - Kampanya seçildiyse `kampanya_oteller` ilişkisi otomatik güncellenir hale getirildi.
5. `Pricing.cshtml` içinde:
   - Toplu güncelleme formuna `Kampanya seçimi` alanı eklendi.
   - Günlük düzenleme modalına `Kampanya seçimi` alanı eklendi.
   - JS doğrulaması, indirimli fiyat için “indirim veya kampanya” seçimini zorunlu kılacak şekilde güncellendi.

## Veritabanı Güncelleme Dosyaları

- `Database/MigrationsSql/20260429_sqlserver_alter_oda_fiyat_musaitlik_add_discount_and_campaign_columns.sql`
  - `indirim_id` ve `kampanya_fiyati` alanlarını ekler.
  - Eski kayıtlar için geçiş amaçlı doldurma yapar.
- `Database/MigrationsSql/20260429_sqlserver_fix_fiyat_indirimleri_turkish_chars_all_rows.sql`
  - `fiyat_indirimleri` tablosunda bozuk Türkçe karakterleri normalize eder.

## Sonraki Aşama

- `panel/partner/takvim-fiyatlar` sayfasının ekran tasarımı, paylaştığınız referans görseldeki yoğun tablo düzenine daha yakın bir görünüme geçirilecek.
- Gün bazlı satır düzeninde (yetişkin/çocuk, iadesiz vb.) hızlı hücre düzenleme deneyimi eklenecek.
