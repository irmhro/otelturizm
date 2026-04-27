/*
  2026-04-24
  Fiyat indirimleri sözlüğü (partnerların takvimde seçtiği indirim tanımı).

  Not: oda_fiyat_musaitlik.kampanya_id alanı bundan sonra "indirim_id" gibi kullanılır.
*/

IF OBJECT_ID('dbo.fiyat_indirimleri', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.fiyat_indirimleri
    (
        id BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        indirim_adi NVARCHAR(140) NOT NULL,
        kisa_aciklama NVARCHAR(220) NULL,
        detay_html NVARCHAR(MAX) NULL,
        gorsel_url NVARCHAR(500) NULL,
        ikon_class NVARCHAR(80) NULL,
        renk_kodu NVARCHAR(30) NULL,
        aktif_mi BIT NOT NULL CONSTRAINT DF_fiyat_indirimleri_aktif DEFAULT 1,
        siralama SMALLINT NOT NULL CONSTRAINT DF_fiyat_indirimleri_siralama DEFAULT 100,
        olusturulma_tarihi DATETIME2 NOT NULL CONSTRAINT DF_fiyat_indirimleri_olusturulma DEFAULT SYSUTCDATETIME(),
        guncellenme_tarihi DATETIME2 NOT NULL CONSTRAINT DF_fiyat_indirimleri_guncellenme DEFAULT SYSUTCDATETIME()
    );

    CREATE INDEX IX_fiyat_indirimleri_aktif ON dbo.fiyat_indirimleri(aktif_mi, siralama, id);
END
GO

/* Seed (>=30) */
IF NOT EXISTS (SELECT 1 FROM dbo.fiyat_indirimleri)
BEGIN
    INSERT INTO dbo.fiyat_indirimleri (indirim_adi, kisa_aciklama, detay_html, gorsel_url, ikon_class, renk_kodu, aktif_mi, siralama)
    VALUES
    (N'Anneler Günü İndirimi', N'Anneler Günü haftasına özel fiyat avantajı.', N'<h3>Anneler Günü</h3><p>Bu indirim, Anneler Günü haftasında seçili günlerde geçerli olacak şekilde uygulanır.</p>', NULL, N'fa-heart', N'#e11d48', 1, 10),
    (N'Sevgililer Günü İndirimi', N'Sevgililer Günü için romantik konaklama fırsatı.', N'<h3>Sevgililer Günü</h3><p>14 Şubat döneminde romantik kaçamak planları için özel indirim.</p>', NULL, N'fa-heart', N'#ef4444', 1, 11),
    (N'Babalar Günü İndirimi', N'Babalar Günü dönemine özel.', N'<h3>Babalar Günü</h3><p>Babalar Günü döneminde seçili günlerde geçerli indirim.</p>', NULL, N'fa-person', N'#0f172a', 1, 12),
    (N'23 Nisan İndirimi', N'23 Nisan haftasında ailelere özel.', N'<h3>23 Nisan</h3><p>Ulusal Egemenlik ve Çocuk Bayramı haftasında aile konaklamalarına destek.</p>', NULL, N'fa-children', N'#2563eb', 1, 13),
    (N'19 Mayıs İndirimi', N'19 Mayıs dönemine özel indirim.', N'<h3>19 Mayıs</h3><p>Gençlik ve Spor Bayramı haftasında seçili günlerde geçerlidir.</p>', NULL, N'fa-flag', N'#1d4ed8', 1, 14),
    (N'1 Mayıs İndirimi', N'1 Mayıs tatiline özel.', N'<h3>1 Mayıs</h3><p>Resmî tatil döneminde talebe göre uygulanan indirim.</p>', NULL, N'fa-calendar-day', N'#334155', 1, 15),
    (N'Ramazan Bayramı İndirimi', N'Bayram tatili dönemine özel fiyat.', N'<h3>Ramazan Bayramı</h3><p>Bayram döneminde seçili günlerde geçerli indirim.</p>', NULL, N'fa-moon', N'#0ea5e9', 1, 16),
    (N'Kurban Bayramı İndirimi', N'Bayram tatiline özel fiyat.', N'<h3>Kurban Bayramı</h3><p>Bayram döneminde seçili günlerde geçerli indirim.</p>', NULL, N'fa-moon', N'#0284c7', 1, 17),
    (N'Hafta Sonu İndirimi', N'Cuma-Cumartesi-Pazar için özel.', N'<h3>Hafta Sonu</h3><p>Hafta sonu yoğunluğuna göre belirlenen indirim.</p>', NULL, N'fa-umbrella-beach', N'#16a34a', 1, 20),
    (N'Hafta İçi İndirimi', N'Pazartesi-Perşembe için özel.', N'<h3>Hafta İçi</h3><p>Hafta içi konaklamalarını teşvik eden indirim.</p>', NULL, N'fa-briefcase', N'#0f766e', 1, 21),
    (N'Erken Rezervasyon', N'Erken alım avantajı.', N'<h3>Erken Rezervasyon</h3><p>Belirli tarihten önce yapılan rezervasyonlarda uygulanır.</p>', NULL, N'fa-bolt', N'#f59e0b', 1, 22),
    (N'Son Dakika Fırsatı', N'Kısa süreli fırsat indirimi.', N'<h3>Son Dakika</h3><p>Yakın tarihteki boşlukları değerlendirmek için hızlı indirim.</p>', NULL, N'fa-bolt', N'#f97316', 1, 23),
    (N'Uzatılmış Konaklama', N'3+ gece konaklamalarda.', N'<h3>Uzatılmış Konaklama</h3><p>Uzun süreli kalışlarda gecelik fiyat avantajı.</p>', NULL, N'fa-bed', N'#14b8a6', 1, 24),
    (N'Aile Paketi', N'Aile konaklamalarına özel.', N'<h3>Aile Paketi</h3><p>Aileler için düzenlenmiş paket indirimi.</p>', NULL, N'fa-people-roof', N'#22c55e', 1, 25),
    (N'Balayı Paketi', N'Balayı çiftlerine özel.', N'<h3>Balayı</h3><p>Balayı çiftlerine yönelik özel fiyat.</p>', NULL, N'fa-ring', N'#ec4899', 1, 26),
    (N'Doğum Günü Sürprizi', N'Doğum günü haftasında.', N'<h3>Doğum Günü</h3><p>Doğum günü haftasına özel küçük bir fiyat avantajı.</p>', NULL, N'fa-cake-candles', N'#a855f7', 1, 27),
    (N'Yeni Üye İndirimi', N'İlk rezervasyona özel.', N'<h3>Yeni Üye</h3><p>İlk rezervasyonlarda uygulanabilen tanışma indirimi.</p>', NULL, N'fa-user-plus', N'#1d4ed8', 1, 28),
    (N'Tek Gece Fırsatı', N'Günübirlik/tek gece için.', N'<h3>Tek Gece</h3><p>Tek gecelik kaçamaklarda uygulanır.</p>', NULL, N'fa-moon', N'#0f172a', 1, 29),
    (N'Uzun Hafta Sonu', N'Resmî tatil + hafta sonu.', N'<h3>Uzun Hafta Sonu</h3><p>Resmî tatil birleşimlerinde uygulanır.</p>', NULL, N'fa-calendar-week', N'#0ea5e9', 1, 30),
    (N'Kış Sezonu', N'Kış dönemine özel.', N'<h3>Kış Sezonu</h3><p>Kış aylarında doluluğu destekleyen indirim.</p>', NULL, N'fa-snowflake', N'#0284c7', 1, 31),
    (N'Yaz Sezonu', N'Yaz dönemi kampanyası.', N'<h3>Yaz Sezonu</h3><p>Yaz aylarında planlı kampanya indirimi.</p>', NULL, N'fa-sun', N'#f59e0b', 1, 32),
    (N'İlkbahar Fırsatı', N'Bahar dönemine özel.', N'<h3>İlkbahar</h3><p>Bahar döneminde seçili günlerde uygulanır.</p>', NULL, N'fa-seedling', N'#16a34a', 1, 33),
    (N'Sonbahar Fırsatı', N'Sonbahar dönemine özel.', N'<h3>Sonbahar</h3><p>Sonbahar döneminde seçili günlerde uygulanır.</p>', NULL, N'fa-leaf', N'#b45309', 1, 34),
    (N'Kurumsal Anlaşma', N'Firma anlaşmalı fiyat.', N'<h3>Kurumsal</h3><p>Kurumsal anlaşmalara bağlı özel fiyat.</p>', NULL, N'fa-building', N'#334155', 1, 35),
    (N'Uzaktan Çalışma', N'Uzaktan çalışma paket indirimi.', N'<h3>Workation</h3><p>Uzaktan çalışma için uzun konaklama avantajı.</p>', NULL, N'fa-laptop', N'#0f766e', 1, 36),
    (N'Spa & Wellness', N'Spa odaklı paket.', N'<h3>Spa & Wellness</h3><p>Spa hizmetleriyle birlikte planlanan paket indirimi.</p>', NULL, N'fa-spa', N'#22c55e', 1, 37),
    (N'Yemek Dahil', N'Yemek paketine özel fiyat.', N'<h3>Yemek Dahil</h3><p>Yemek seçenekleri dahil paketlerde geçerlidir.</p>', NULL, N'fa-utensils', N'#f97316', 1, 38),
    (N'Sınırlı Stok', N'Sınırlı oda stoklarında.', N'<h3>Sınırlı Stok</h3><p>Sınırlı stok günlerinde hızlı aksiyon indirimi.</p>', NULL, N'fa-hourglass-half', N'#ef4444', 1, 39),
    (N'Özel Gün', N'Partnerin belirlediği özel gün indirimi.', N'<h3>Özel Gün</h3><p>Partnerin tanımladığı özel dönem indirimi.</p>', NULL, N'fa-star', N'#2563eb', 1, 40);
END
GO

