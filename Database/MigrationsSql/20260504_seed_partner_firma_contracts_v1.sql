-- SQL Server (idempotent): partner + firma sözleşme ve KVKK metinleri (v1)
-- Sluglar:
--  - partner-basvuru-sozlesmesi
--  - partner-kvkk-aydinlatma
--  - firma-kurumsal-kullanim-kosullari
--  - firma-kvkk-aydinlatma
--
-- Not: Metinler ürün akışına uygun genel taslaktır; yayıma almadan önce hukuk birimi tarafından gözden geçirilmelidir.

SET NOCOUNT ON;

DECLARE @effective datetime2(7) = '2026-04-20T00:00:00';

-- =========================================================
-- PARTNER (Extranet / Tesis başvurusu)
-- =========================================================
DECLARE @partnerAgreementSlug nvarchar(200) = N'partner-basvuru-sozlesmesi';
DECLARE @partnerKvkkSlug nvarchar(200) = N'partner-kvkk-aydinlatma';

DECLARE @partnerAgreementTitle nvarchar(200) = N'Partner Başvuru Sözleşmesi ve Komisyon Politikası';
DECLARE @partnerAgreementSubtitle nvarchar(300) = N'Otelturizm extranet başvurusu, tesis kaydı, rezervasyon yönetimi ve komisyon kuralları.';

DECLARE @partnerKvkkTitle nvarchar(200) = N'Partner KVKK Aydınlatma Metni';
DECLARE @partnerKvkkSubtitle nvarchar(300) = N'6698 sayılı Kanun kapsamında partner başvurusu ve operasyon verilerinin işlenmesine dair aydınlatma.';

DECLARE @partnerAgreementSummary nvarchar(max) = N'
<p><strong>Kapsam:</strong> Partner başvurusu, tesis bilgileri, extranet erişimi, rezervasyon/operasyon yönetimi, komisyon ve ödeme süreçleri.</p>
<p><strong>Versiyon:</strong> v1 · <strong>20 Nisan 2026</strong> itibarıyla yürürlüktedir.</p>
<p><strong>Önemli:</strong> Başvuru kaydı, e‑posta doğrulaması tamamlandıktan sonra incelemeye alınır; panel erişimi, platform onayı ile etkinleşir.</p>
';

DECLARE @partnerAgreementContent nvarchar(max) = N'
<h2>1. Taraflar ve konu</h2>
<p>İşbu sözleşme (“Sözleşme”), otelturizm.com Platformu (“Platform”) üzerinden tesis/konaklama hizmeti sunmak üzere başvuru yapan işletme (“Partner/Tesis”) ile Platform işletmecisi (“Platform İşleticisi”) arasında, başvuru ve extranet kullanımına ilişkin kuralları düzenler.</p>

<h2>2. Başvuru, doğrulama ve panel erişimi</h2>
<ul>
  <li>Partner başvurusu, eksiksiz ve doğru bilgilerle yapılır. Yanıltıcı/yanlış beyan halinde başvuru reddedilebilir veya hesap kapatılabilir.</li>
  <li>E‑posta doğrulaması, başvuru iletişiminin güvenliği için zorunludur. E‑posta doğrulaması tamamlanmadan başvuru incelemeye alınmayabilir.</li>
  <li>Extranet/panel erişimi, Platform İşleticisi onayı ile etkinleşir. Onay verilene kadar erişim bekletilebilir.</li>
</ul>

<h2>3. Tesis bilgileri, içerikler ve doğruluk taahhüdü</h2>
<ul>
  <li>Partner; tesis adı, adres, yıldız, oda tipleri, fiyatlar, görseller, vergiler, iptal/konaklama kuralları gibi tüm içeriklerin doğru ve güncel olduğunu beyan eder.</li>
  <li>Görsel ve içeriklerin telif/marka haklarına uygunluğu Partner sorumluluğundadır.</li>
  <li>Platform, yanıltıcı içerikleri düzeltme, askıya alma veya yayından kaldırma hakkını saklı tutar.</li>
</ul>

<h2>4. Rezervasyon yönetimi</h2>
<ul>
  <li>Partner, rezervasyon durumlarını (onay/red/check‑in/check‑out/tamamlandı vb.) makul sürelerde güncellemeyi ve misafir iletişimini sürdürmeyi kabul eder.</li>
  <li>Rezervasyon reddi/iptali gibi işlemlerde gerekçe ve politika uyumu aranır. Aşırı red/iptal oranlarında Platform, görünürlüğü sınırlayabilir veya geçici kısıt uygulayabilir.</li>
  <li>Misafir notları ve talepleri, operasyon süreçlerine yardımcı olmak içindir; hukuka aykırı talepler yerine getirilmeyebilir.</li>
</ul>

<h2>5. Komisyon ve ödeme</h2>
<ul>
  <li>Komisyon oranları, sözleşmede veya extranet panelinde tanımlanan komisyon politikası çerçevesinde uygulanır.</li>
  <li>Vergiler, stopaj ve yasal kesintiler, mevzuat ve tarafların statüsüne göre değişebilir.</li>
  <li>Ödeme mutabakatları (tahsilat/ödeme yöntemi/ödeme zamanı), rezervasyon kaydı ve finans raporları üzerinden yürütülür.</li>
</ul>

<h2>6. Gizlilik ve veri güvenliği</h2>
<ul>
  <li>Partner, extranet erişim bilgilerini gizli tutar; yetkisiz erişimi engellemek için gerekli tedbirleri alır.</li>
  <li>Platform üzerinden erişilen misafir verileri, yalnızca rezervasyonun ifası ve yasal yükümlülükler için kullanılabilir.</li>
</ul>

<h2>7. Fesih ve askıya alma</h2>
<p>Taraflar, mevzuat ve sözleşme hükümleri çerçevesinde sözleşmeyi feshedebilir. Platform; ağır ihlal, sahtecilik, güvenlik riski, mevzuata aykırılık veya sistem suistimali halinde hesabı askıya alabilir.</p>

<h2>8. Uyuşmazlık</h2>
<p>Uyuşmazlıklarda Türkiye Cumhuriyeti mevzuatı uygulanır. Yetkili merci, ilişkinin niteliğine göre genel görevli mahkemeler/kurullar olabilir.</p>
';

DECLARE @partnerKvkkSummary nvarchar(max) = N'
<p>Bu metin, partner başvurusu ve extranet operasyon süreçlerinde işlenen kişisel veriler hakkında KVKK kapsamında aydınlatma sağlar.</p>
';

DECLARE @partnerKvkkContent nvarchar(max) = N'
<h2>1. Veri sorumlusu</h2>
<p>Partner başvurusu ve operasyon süreçlerinde işlenen veriler, <strong>Otelturizm</strong> (“Veri Sorumlusu”) tarafından KVKK kapsamında işlenebilir.</p>

<h2>2. İşlenen veri kategorileri</h2>
<ul>
  <li><strong>Yetkili kişi:</strong> Ad‑soyad, unvan, e‑posta, telefon, (varsa) T.C. kimlik bilgileri.</li>
  <li><strong>Firma/tesis:</strong> Unvan, vergi dairesi/numarası, adres, tesis bilgileri, operasyon kayıtları.</li>
  <li><strong>Güvenlik:</strong> Giriş/oturum logları, IP, cihaz bilgileri, işlem kayıtları.</li>
</ul>

<h2>3. Amaçlar ve hukuki sebepler</h2>
<p>Veriler; başvurunun değerlendirilmesi, sözleşmesel ilişkilerin kurulması/ifası, yasal yükümlülüklerin yerine getirilmesi, bilgi güvenliği ve operasyon yönetimi amaçlarıyla; KVKK madde 5/2 kapsamındaki hukuki sebeplerle işlenebilir.</p>

<h2>4. Aktarım</h2>
<p>Veriler; barındırma, e‑posta gönderimi, güvenlik ve benzeri altyapı sağlayıcılarıyla; yasal zorunluluk halinde yetkili kurumlarla; amaçla sınırlı şekilde paylaşılabilir.</p>

<h2>5. Haklar</h2>
<p>KVKK madde 11 kapsamındaki haklarınızı, Platform destek kanalları üzerinden kullanabilirsiniz.</p>
';

-- Upsert: Partner Agreement v1
IF EXISTS (SELECT 1 FROM dbo.sozlesmeler WHERE slug = @partnerAgreementSlug AND versiyon_no = 1)
BEGIN
    UPDATE dbo.sozlesmeler
    SET hedef_kitle = N'partner',
        sozlesme_tipi = N'agreement',
        baslik = @partnerAgreementTitle,
        alt_baslik = @partnerAgreementSubtitle,
        ozet_html = @partnerAgreementSummary,
        icerik_html = @partnerAgreementContent,
        gorsel_url = NULL,
        sozlesme_linki = NULL,
        baslangic_tarihi = @effective,
        bitis_tarihi = NULL,
        kabul_gerektirir_mi = 1,
        email_dogrulamada_gonder = 1,
        yenileme_gerekir_mi = 0,
        yenileme_periyodu_gun = NULL,
        aktif_mi = 1,
        notlar = N'Partner v1 seed (2026-05-04).',
        guncellenme_tarihi = SYSUTCDATETIME()
    WHERE slug = @partnerAgreementSlug AND versiyon_no = 1;
END
ELSE
BEGIN
    INSERT INTO dbo.sozlesmeler
    (hedef_kitle, sozlesme_tipi, baslik, alt_baslik, slug, ozet_html, icerik_html, gorsel_url, sozlesme_linki,
     versiyon_no, baslangic_tarihi, bitis_tarihi, kabul_gerektirir_mi, email_dogrulamada_gonder, yenileme_gerekir_mi, yenileme_periyodu_gun,
     aktif_mi, notlar, olusturulma_tarihi, guncellenme_tarihi)
    VALUES
    (N'partner', N'agreement', @partnerAgreementTitle, @partnerAgreementSubtitle, @partnerAgreementSlug, @partnerAgreementSummary, @partnerAgreementContent, NULL, NULL,
     1, @effective, NULL, 1, 1, 0, NULL,
     1, N'Partner v1 seed (2026-05-04).', SYSUTCDATETIME(), SYSUTCDATETIME());
END

-- Upsert: Partner KVKK v1
IF EXISTS (SELECT 1 FROM dbo.sozlesmeler WHERE slug = @partnerKvkkSlug AND versiyon_no = 1)
BEGIN
    UPDATE dbo.sozlesmeler
    SET hedef_kitle = N'partner',
        sozlesme_tipi = N'kvkk',
        baslik = @partnerKvkkTitle,
        alt_baslik = @partnerKvkkSubtitle,
        ozet_html = @partnerKvkkSummary,
        icerik_html = @partnerKvkkContent,
        gorsel_url = NULL,
        sozlesme_linki = NULL,
        baslangic_tarihi = @effective,
        bitis_tarihi = NULL,
        kabul_gerektirir_mi = 1,
        email_dogrulamada_gonder = 1,
        yenileme_gerekir_mi = 0,
        yenileme_periyodu_gun = NULL,
        aktif_mi = 1,
        notlar = N'Partner KVKK v1 seed (2026-05-04).',
        guncellenme_tarihi = SYSUTCDATETIME()
    WHERE slug = @partnerKvkkSlug AND versiyon_no = 1;
END
ELSE
BEGIN
    INSERT INTO dbo.sozlesmeler
    (hedef_kitle, sozlesme_tipi, baslik, alt_baslik, slug, ozet_html, icerik_html, gorsel_url, sozlesme_linki,
     versiyon_no, baslangic_tarihi, bitis_tarihi, kabul_gerektirir_mi, email_dogrulamada_gonder, yenileme_gerekir_mi, yenileme_periyodu_gun,
     aktif_mi, notlar, olusturulma_tarihi, guncellenme_tarihi)
    VALUES
    (N'partner', N'kvkk', @partnerKvkkTitle, @partnerKvkkSubtitle, @partnerKvkkSlug, @partnerKvkkSummary, @partnerKvkkContent, NULL, NULL,
     1, @effective, NULL, 1, 1, 0, NULL,
     1, N'Partner KVKK v1 seed (2026-05-04).', SYSUTCDATETIME(), SYSUTCDATETIME());
END

-- =========================================================
-- FİRMA (Kurumsal kullanıcı / şirket hesabı)
-- =========================================================
DECLARE @firmaAgreementSlug nvarchar(200) = N'firma-kurumsal-kullanim-kosullari';
DECLARE @firmaKvkkSlug nvarchar(200) = N'firma-kvkk-aydinlatma';

DECLARE @firmaAgreementTitle nvarchar(200) = N'Firma Kurumsal Kullanım Koşulları';
DECLARE @firmaAgreementSubtitle nvarchar(300) = N'Kurumsal hesap, çalışan yönetimi, kurumsal rezervasyon ve faturalama süreçleri kuralları.';

DECLARE @firmaKvkkTitle nvarchar(200) = N'Firma KVKK Aydınlatma Metni';
DECLARE @firmaKvkkSubtitle nvarchar(300) = N'6698 sayılı Kanun kapsamında kurumsal hesap verilerinin işlenmesine dair aydınlatma.';

DECLARE @firmaAgreementSummary nvarchar(max) = N'
<p><strong>Kapsam:</strong> Kurumsal üyelik, yetkili/çalışan erişimleri, kurumsal rezervasyon, faturalama ve ödeme süreçleri.</p>
<p><strong>Versiyon:</strong> v1 · <strong>20 Nisan 2026</strong> itibarıyla yürürlüktedir.</p>
';

DECLARE @firmaAgreementContent nvarchar(max) = N'
<h2>1. Taraflar ve konu</h2>
<p>İşbu sözleşme (“Sözleşme”), kurumsal hesap açan firma/kurum (“Firma”) ile Platform İşleticisi arasında, otelturizm.com üzerinden kurumsal rezervasyon ve kullanıcı yönetimi süreçlerine ilişkin kuralları düzenler.</p>

<h2>2. Kurumsal hesap ve yetkiler</h2>
<ul>
  <li>Firma, hesabı temsil etmeye yetkili kişi(ler) aracılığıyla açar ve yönetir.</li>
  <li>Yetkilendirme ve kullanıcı ekleme/çıkarma işlemlerinden Firma sorumludur.</li>
  <li>Kurumsal hesap üzerinden yapılan işlemler, Firma adına yapılmış sayılabilir.</li>
</ul>

<h2>3. Rezervasyon ve faturalama</h2>
<ul>
  <li>Kurumsal rezervasyonlarda misafir bilgileri, tarih ve oda seçimleri ile ödeme/fatura bilgileri doğru girilmelidir.</li>
  <li>İptal/değişiklik kuralları tesis politikaları ve rezervasyon statüsüne göre uygulanır.</li>
  <li>Faturalama süreçleri mevzuata uygun yürütülür; gerekli hallerde ek bilgi/belge talep edilebilir.</li>
</ul>

<h2>4. Ödeme ve tahsilat</h2>
<p>Ödeme yöntemi (online, kapıda, havale vb.) ve tahsilat planı, rezervasyon kaydında belirtilen koşullara göre yürütülür.</p>

<h2>5. Veri güvenliği ve gizlilik</h2>
<ul>
  <li>Firma, kurumsal hesap erişim bilgilerini yetkisiz kullanıma karşı korur.</li>
  <li>Firma, çalışan/misafir bilgilerini hukuka uygun şekilde işlemek ve paylaşmakla sorumludur.</li>
</ul>

<h2>6. Sorumluluk sınırları</h2>
<p>Konaklama hizmeti ilgili tesis tarafından sunulur. Platform, aracı hizmet sağlayıcı olarak mevzuat kapsamındaki yükümlülüklerini yerine getirmeyi hedefler.</p>

<h2>7. Fesih</h2>
<p>Taraflar, mevzuat ve sözleşme hükümleri çerçevesinde kurumsal hesabı kapatabilir veya sözleşmeyi feshedebilir.</p>
';

DECLARE @firmaKvkkSummary nvarchar(max) = N'
<p>Bu metin, kurumsal hesap ve kurumsal rezervasyon süreçlerinde işlenen kişisel veriler hakkında KVKK kapsamında aydınlatma sağlar.</p>
';

DECLARE @firmaKvkkContent nvarchar(max) = N'
<h2>1. Veri sorumlusu</h2>
<p>Kurumsal hesap süreçlerinde işlenen veriler, <strong>Otelturizm</strong> (“Veri Sorumlusu”) tarafından KVKK kapsamında işlenebilir.</p>

<h2>2. İşlenen veri kategorileri</h2>
<ul>
  <li><strong>Yetkili ve kullanıcılar:</strong> Ad‑soyad, e‑posta, telefon, rol/yetki, oturum kayıtları.</li>
  <li><strong>Firma bilgileri:</strong> Unvan, vergi bilgileri, adres, fatura bilgileri.</li>
  <li><strong>Rezervasyon ve finans:</strong> Rezervasyon detayları, ödeme yöntemi/durumu, işlem kayıtları.</li>
</ul>

<h2>3. Amaçlar ve hukuki sebepler</h2>
<p>Veriler; sözleşmenin kurulması/ifası, yasal yükümlülükler, bilgi güvenliği ve operasyon yönetimi amaçlarıyla KVKK madde 5/2 kapsamında işlenebilir.</p>

<h2>4. Aktarım</h2>
<p>Rezervasyonun yürütülmesi için ilgili tesis/partner ile; altyapı sağlayıcılarla ve yasal zorunluluk halinde yetkili kurumlarla amaçla sınırlı paylaşım yapılabilir.</p>

<h2>5. Haklar</h2>
<p>KVKK madde 11 kapsamındaki haklarınızı, Platform destek kanalları üzerinden kullanabilirsiniz.</p>
';

-- Upsert: Firma Agreement v1
IF EXISTS (SELECT 1 FROM dbo.sozlesmeler WHERE slug = @firmaAgreementSlug AND versiyon_no = 1)
BEGIN
    UPDATE dbo.sozlesmeler
    SET hedef_kitle = N'firma',
        sozlesme_tipi = N'agreement',
        baslik = @firmaAgreementTitle,
        alt_baslik = @firmaAgreementSubtitle,
        ozet_html = @firmaAgreementSummary,
        icerik_html = @firmaAgreementContent,
        gorsel_url = NULL,
        sozlesme_linki = NULL,
        baslangic_tarihi = @effective,
        bitis_tarihi = NULL,
        kabul_gerektirir_mi = 1,
        email_dogrulamada_gonder = 1,
        yenileme_gerekir_mi = 0,
        yenileme_periyodu_gun = NULL,
        aktif_mi = 1,
        notlar = N'Firma v1 seed (2026-05-04).',
        guncellenme_tarihi = SYSUTCDATETIME()
    WHERE slug = @firmaAgreementSlug AND versiyon_no = 1;
END
ELSE
BEGIN
    INSERT INTO dbo.sozlesmeler
    (hedef_kitle, sozlesme_tipi, baslik, alt_baslik, slug, ozet_html, icerik_html, gorsel_url, sozlesme_linki,
     versiyon_no, baslangic_tarihi, bitis_tarihi, kabul_gerektirir_mi, email_dogrulamada_gonder, yenileme_gerekir_mi, yenileme_periyodu_gun,
     aktif_mi, notlar, olusturulma_tarihi, guncellenme_tarihi)
    VALUES
    (N'firma', N'agreement', @firmaAgreementTitle, @firmaAgreementSubtitle, @firmaAgreementSlug, @firmaAgreementSummary, @firmaAgreementContent, NULL, NULL,
     1, @effective, NULL, 1, 1, 0, NULL,
     1, N'Firma v1 seed (2026-05-04).', SYSUTCDATETIME(), SYSUTCDATETIME());
END

-- Upsert: Firma KVKK v1
IF EXISTS (SELECT 1 FROM dbo.sozlesmeler WHERE slug = @firmaKvkkSlug AND versiyon_no = 1)
BEGIN
    UPDATE dbo.sozlesmeler
    SET hedef_kitle = N'firma',
        sozlesme_tipi = N'kvkk',
        baslik = @firmaKvkkTitle,
        alt_baslik = @firmaKvkkSubtitle,
        ozet_html = @firmaKvkkSummary,
        icerik_html = @firmaKvkkContent,
        gorsel_url = NULL,
        sozlesme_linki = NULL,
        baslangic_tarihi = @effective,
        bitis_tarihi = NULL,
        kabul_gerektirir_mi = 1,
        email_dogrulamada_gonder = 1,
        yenileme_gerekir_mi = 0,
        yenileme_periyodu_gun = NULL,
        aktif_mi = 1,
        notlar = N'Firma KVKK v1 seed (2026-05-04).',
        guncellenme_tarihi = SYSUTCDATETIME()
    WHERE slug = @firmaKvkkSlug AND versiyon_no = 1;
END
ELSE
BEGIN
    INSERT INTO dbo.sozlesmeler
    (hedef_kitle, sozlesme_tipi, baslik, alt_baslik, slug, ozet_html, icerik_html, gorsel_url, sozlesme_linki,
     versiyon_no, baslangic_tarihi, bitis_tarihi, kabul_gerektirir_mi, email_dogrulamada_gonder, yenileme_gerekir_mi, yenileme_periyodu_gun,
     aktif_mi, notlar, olusturulma_tarihi, guncellenme_tarihi)
    VALUES
    (N'firma', N'kvkk', @firmaKvkkTitle, @firmaKvkkSubtitle, @firmaKvkkSlug, @firmaKvkkSummary, @firmaKvkkContent, NULL, NULL,
     1, @effective, NULL, 1, 1, 0, NULL,
     1, N'Firma KVKK v1 seed (2026-05-04).', SYSUTCDATETIME(), SYSUTCDATETIME());
END

