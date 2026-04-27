/*
  2026-04-27 (SQL Server)
  - fiyat_indirimleri: "Test/Deneme İç kullanım / test indirimi." satırını kaldırır.
  - Sözlüğü 50+ aktif indirim tanımına genişletir (partner fiyat sunumu için).
*/

SET NOCOUNT ON;

-- 1) Test indirimi kaldır (id sabit kabul edilmez)
DELETE FROM dbo.fiyat_indirimleri
WHERE (indirim_adi LIKE N'Test/Deneme%' OR indirim_adi = N'Test/Deneme')
   OR (kisa_aciklama LIKE N'%İç kullanım%test%')
   OR (kisa_aciklama LIKE N'%ic kullanim%test%')
   OR (detay_html LIKE N'%iç test%' OR detay_html LIKE N'%ic test%');

-- 2) Yeni indirim tanımları (30 mevcut seed üzerine 20+ ek)
DECLARE @seed TABLE
(
    indirim_adi NVARCHAR(140) NOT NULL,
    kisa_aciklama NVARCHAR(220) NULL,
    detay_html NVARCHAR(MAX) NULL,
    ikon_class NVARCHAR(80) NULL,
    renk_kodu NVARCHAR(30) NULL,
    siralama SMALLINT NOT NULL
);

INSERT INTO @seed (indirim_adi, kisa_aciklama, detay_html, ikon_class, renk_kodu, siralama)
VALUES
 (N'Yılbaşı Fırsatı', N'Yılbaşı dönemine özel fiyat.', N'<h3>Yılbaşı</h3><p>Yılbaşı haftasında seçili günlerde geçerli fiyat avantajı.</p>', N'fa-champagne-glasses', N'#DB2777', 41),
 (N'Black Friday', N'Kasım ayı Black Friday kampanyası.', N'<h3>Black Friday</h3><p>Kasım indirim döneminde sınırlı süreli fiyat avantajı.</p>', N'fa-tags', N'#111827', 42),
 (N'Cyber Monday', N'Cyber Monday dönemine özel.', N'<h3>Cyber Monday</h3><p>Black Friday sonrası kısa süreli dijital kampanya indirimi.</p>', N'fa-tag', N'#0F766E', 43),
 (N'Sömestir Tatili', N'Sömestir tatil haftasına özel.', N'<h3>Sömestir</h3><p>Okul tatil döneminde aile planlarına özel fiyat.</p>', N'fa-school', N'#2563EB', 44),
 (N'Bayram Öncesi', N'Bayram öncesi tarihlerde teşvik indirimi.', N'<h3>Bayram Öncesi</h3><p>Bayram öncesi doluluğu desteklemek için.</p>', N'fa-calendar', N'#0284C7', 45),
 (N'Bayram Sonrası', N'Bayram sonrası tarihlerde teşvik indirimi.', N'<h3>Bayram Sonrası</h3><p>Bayram sonrası boşlukları değerlendirmek için.</p>', N'fa-calendar', N'#0EA5E9', 46),
 (N'Mobil Özel Fiyat', N'Mobil kullanıcılar için özel fiyat.', N'<h3>Mobil Özel</h3><p>Mobil kanaldan gelen rezervasyonları artırmak için.</p>', N'fa-mobile-screen', N'#16A34A', 47),
 (N'Üyeye Özel Fiyat', N'Üyelere özel ekstra avantaj.', N'<h3>Üye Özel</h3><p>Üyelik ile gelen kullanıcılar için ekstra indirim.</p>', N'fa-user-check', N'#334155', 48),
 (N'Sadakat (Loyalty)', N'Sadakat seviyesi olan misafirlere özel.', N'<h3>Sadakat</h3><p>Tekrarlı konaklamalara bağlı fiyat avantajı.</p>', N'fa-gem', N'#7C3AED', 49),
 (N'Çoklu Oda', N'2+ oda rezervasyonunda özel.', N'<h3>Çoklu Oda</h3><p>Birden fazla oda rezervasyonunda paket fiyat.</p>', N'fa-door-open', N'#1D4ED8', 50),
 (N'7+ Gece Avantajı', N'7 ve üzeri gece konaklamalarda.', N'<h3>7+ Gece</h3><p>Uzun konaklamalarda gecelik fiyatı optimize eder.</p>', N'fa-bed', N'#14B8A6', 51),
 (N'14+ Gece Avantajı', N'14 ve üzeri gece konaklamalarda.', N'<h3>14+ Gece</h3><p>Çok uzun konaklamalarda ek fiyat avantajı.</p>', N'fa-bed', N'#0F766E', 52),
 (N'Non-Refundable', N'İadesiz (iptalsiz) fiyat planı.', N'<h3>İadesiz</h3><p>İptal/iade olmayan fiyat planında daha iyi fiyat sunar.</p>', N'fa-lock', N'#111827', 53),
 (N'Ücretsiz İptal', N'Ücretsiz iptal penceresine özel.', N'<h3>Ücretsiz İptal</h3><p>Esnek iptal koşullarında görünürlüğü artırır.</p>', N'fa-rotate-left', N'#2563EB', 54),
 (N'Uzun Dönem Planlı', N'Sezon planı (haftalık/aylık) fiyatı.', N'<h3>Planlı</h3><p>Sezonluk planlı fiyat güncellemeleri için.</p>', N'fa-chart-line', N'#B45309', 55),
 (N'Açılış Fırsatı', N'Yeni açılan tesisler için.', N'<h3>Açılış</h3><p>Yeni tesislerin ilk dönem görünürlüğünü artırır.</p>', N'fa-bolt', N'#F59E0B', 56),
 (N'Renovasyon Sonrası', N'Renovasyon sonrası geri dönüş kampanyası.', N'<h3>Renovasyon</h3><p>Yenileme sonrası misafir kazanımı için.</p>', N'fa-wand-magic-sparkles', N'#EC4899', 57),
 (N'Erken Check-in / Geç Check-out', N'Ek ayrıcalıkla paket fiyat.', N'<h3>Ayrıcalık</h3><p>Erken giriş/geç çıkış avantajı ile sunulan paket.</p>', N'fa-clock', N'#334155', 58),
 (N'Toplu Konaklama', N'Toplu (grup) konaklamalarda.', N'<h3>Grup</h3><p>Grup rezervasyonlarında anlaşmalı fiyat.</p>', N'fa-people-group', N'#16A34A', 59),
 (N'Son Oda', N'Son kalan odalarda dinamik indirim.', N'<h3>Son Oda</h3><p>Son stoklarda hızlı aksiyon indirimi.</p>', N'fa-hourglass-end', N'#EF4444', 60),
 (N'Öğrenci Fırsatı', N'Öğrenci ve gençlik dönemine özel.', N'<h3>Öğrenci</h3><p>Uygunluk durumuna göre öğrenci/ gençlik döneminde avantajlı fiyat.</p>', N'fa-graduation-cap', N'#2563EB', 61);

INSERT INTO dbo.fiyat_indirimleri
(
    indirim_adi,
    kisa_aciklama,
    detay_html,
    ikon_class,
    renk_kodu,
    aktif_mi,
    siralama,
    olusturulma_tarihi,
    guncellenme_tarihi
)
SELECT
    s.indirim_adi,
    s.kisa_aciklama,
    s.detay_html,
    s.ikon_class,
    s.renk_kodu,
    1,
    s.siralama,
    SYSUTCDATETIME(),
    SYSUTCDATETIME()
FROM @seed s
WHERE NOT EXISTS
(
    SELECT 1
    FROM dbo.fiyat_indirimleri fi
    WHERE fi.indirim_adi = s.indirim_adi
);

-- 3) Hedef: en az 50 aktif kayıt (rapor)
SELECT
    SUM(CASE WHEN aktif_mi = 1 THEN 1 ELSE 0 END) AS aktif_adet,
    COUNT(*) AS toplam_adet
FROM dbo.fiyat_indirimleri;

