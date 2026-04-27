/*
  2026-04-27
  SQL Server: fiyat_indirimleri tablosundaki bozuk Türkçe karakterleri normalize eder.
  Not: id 1-30 seed kayıtlarını canonical Türkçe metinlerle günceller.
*/

SET NOCOUNT ON;

;WITH canonical AS (
    SELECT *
    FROM (VALUES
        (1, N'Anneler Günü İndirimi', N'Anneler Günü haftasına özel fiyat avantajı.', N'<h3>Anneler Günü</h3><p>Bu indirim, Anneler Günü haftasında seçili günlerde geçerli olacak şekilde uygulanır.</p>'),
        (2, N'Sevgililer Günü İndirimi', N'Sevgililer Günü için romantik konaklama fırsatı.', N'<h3>Sevgililer Günü</h3><p>14 Şubat döneminde romantik kaçamak planları için özel indirim.</p>'),
        (3, N'Babalar Günü İndirimi', N'Babalar Günü dönemine özel.', N'<h3>Babalar Günü</h3><p>Babalar Günü döneminde seçili günlerde geçerli indirim.</p>'),
        (4, N'23 Nisan İndirimi', N'23 Nisan haftasında ailelere özel.', N'<h3>23 Nisan</h3><p>Ulusal Egemenlik ve Çocuk Bayramı haftasında aile konaklamalarına destek.</p>'),
        (5, N'19 Mayıs İndirimi', N'19 Mayıs dönemine özel indirim.', N'<h3>19 Mayıs</h3><p>Gençlik ve Spor Bayramı haftasında seçili günlerde geçerlidir.</p>'),
        (6, N'1 Mayıs İndirimi', N'1 Mayıs tatiline özel.', N'<h3>1 Mayıs</h3><p>Resmî tatil döneminde talebe göre uygulanan indirim.</p>'),
        (7, N'Ramazan Bayramı İndirimi', N'Bayram tatili dönemine özel fiyat.', N'<h3>Ramazan Bayramı</h3><p>Bayram döneminde seçili günlerde geçerli indirim.</p>'),
        (8, N'Kurban Bayramı İndirimi', N'Bayram tatiline özel fiyat.', N'<h3>Kurban Bayramı</h3><p>Bayram döneminde seçili günlerde geçerli indirim.</p>'),
        (9, N'Hafta Sonu İndirimi', N'Cuma-Cumartesi-Pazar için özel.', N'<h3>Hafta Sonu</h3><p>Hafta sonu yoğunluğuna göre belirlenen indirim.</p>'),
        (10, N'Hafta İçi İndirimi', N'Pazartesi-Perşembe için özel.', N'<h3>Hafta İçi</h3><p>Hafta içi konaklamalarını teşvik eden indirim.</p>'),
        (11, N'Erken Rezervasyon', N'Erken alım avantajı.', N'<h3>Erken Rezervasyon</h3><p>Belirli tarihten önce yapılan rezervasyonlarda uygulanır.</p>'),
        (12, N'Son Dakika Fırsatı', N'Kısa süreli fırsat indirimi.', N'<h3>Son Dakika</h3><p>Yakın tarihteki boşlukları değerlendirmek için hızlı indirim.</p>'),
        (13, N'Uzatılmış Konaklama', N'3+ gece konaklamalarda.', N'<h3>Uzatılmış Konaklama</h3><p>Uzun süreli kalışlarda gecelik fiyat avantajı.</p>'),
        (14, N'Aile Paketi', N'Aile konaklamalarına özel.', N'<h3>Aile Paketi</h3><p>Aileler için düzenlenmiş paket indirimi.</p>'),
        (15, N'Balayı Paketi', N'Balayı çiftlerine özel.', N'<h3>Balayı</h3><p>Balayı çiftlerine yönelik özel fiyat.</p>'),
        (16, N'Doğum Günü Sürprizi', N'Doğum günü haftasında.', N'<h3>Doğum Günü</h3><p>Doğum günü haftasına özel küçük bir fiyat avantajı.</p>'),
        (17, N'Yeni Üye İndirimi', N'İlk rezervasyona özel.', N'<h3>Yeni Üye</h3><p>İlk rezervasyonlarda uygulanabilen tanışma indirimi.</p>'),
        (18, N'Tek Gece Fırsatı', N'Günübirlik/tek gece için.', N'<h3>Tek Gece</h3><p>Tek gecelik kaçamaklarda uygulanır.</p>'),
        (19, N'Uzun Hafta Sonu', N'Resmî tatil + hafta sonu.', N'<h3>Uzun Hafta Sonu</h3><p>Resmî tatil birleşimlerinde uygulanır.</p>'),
        (20, N'Kış Sezonu', N'Kış dönemine özel.', N'<h3>Kış Sezonu</h3><p>Kış aylarında doluluğu destekleyen indirim.</p>'),
        (21, N'Yaz Sezonu', N'Yaz dönemi kampanyası.', N'<h3>Yaz Sezonu</h3><p>Yaz aylarında planlı kampanya indirimi.</p>'),
        (22, N'İlkbahar Fırsatı', N'Bahar dönemine özel.', N'<h3>İlkbahar</h3><p>Bahar döneminde seçili günlerde uygulanır.</p>'),
        (23, N'Sonbahar Fırsatı', N'Sonbahar dönemine özel.', N'<h3>Sonbahar</h3><p>Sonbahar döneminde seçili günlerde uygulanır.</p>'),
        (24, N'Kurumsal Anlaşma', N'Firma anlaşmalı fiyat.', N'<h3>Kurumsal</h3><p>Kurumsal anlaşmalara bağlı özel fiyat.</p>'),
        (25, N'Uzaktan Çalışma', N'Uzaktan çalışma paket indirimi.', N'<h3>Workation</h3><p>Uzaktan çalışma için uzun konaklama avantajı.</p>'),
        (26, N'Spa & Wellness', N'Spa odaklı paket.', N'<h3>Spa & Wellness</h3><p>Spa hizmetleriyle birlikte planlanan paket indirimi.</p>'),
        (27, N'Yemek Dahil', N'Yemek paketine özel fiyat.', N'<h3>Yemek Dahil</h3><p>Yemek seçenekleri dahil paketlerde geçerlidir.</p>'),
        (28, N'Sınırlı Stok', N'Sınırlı oda stoklarında.', N'<h3>Sınırlı Stok</h3><p>Sınırlı stok günlerinde hızlı aksiyon indirimi.</p>'),
        (29, N'Özel Gün', N'Partnerin belirlediği özel gün indirimi.', N'<h3>Özel Gün</h3><p>Partnerin tanımladığı özel dönem indirimi.</p>')
    ) v (id, indirim_adi, kisa_aciklama, detay_html)
)
UPDATE fi
SET
    fi.indirim_adi = c.indirim_adi,
    fi.kisa_aciklama = c.kisa_aciklama,
    fi.detay_html = c.detay_html,
    fi.guncellenme_tarihi = SYSUTCDATETIME()
FROM dbo.fiyat_indirimleri fi
INNER JOIN canonical c ON c.id = fi.id;

SELECT TOP (30)
    id,
    indirim_adi,
    kisa_aciklama
FROM dbo.fiyat_indirimleri
ORDER BY id;
