-- SQL Server (idempotent): kullanıcı sözleşme + KVKK metinleri (v1)
-- Sluglar:
--  - kullanici-kullanim-kosullari
--  - kullanici-kvkk-aydinlatma

SET NOCOUNT ON;

DECLARE @effective datetime2(7) = '2026-04-20T00:00:00';

DECLARE @userAgreementSlug nvarchar(200) = N'kullanici-kullanim-kosullari';
DECLARE @userKvkkSlug nvarchar(200) = N'kullanici-kvkk-aydinlatma';

DECLARE @agreementTitle nvarchar(200) = N'Kullanıcı Kullanım Koşulları';
DECLARE @agreementSubtitle nvarchar(300) = N'Otelturizm bireysel kullanıcı üyeliği ve rezervasyon kullanım kuralları.';

DECLARE @kvkkTitle nvarchar(200) = N'Kullanıcı KVKK Aydınlatma Metni';
DECLARE @kvkkSubtitle nvarchar(300) = N'6698 sayılı Kanun kapsamında kullanıcı verilerinin işlenmesine dair aydınlatma metni.';

DECLARE @agreementSummary nvarchar(max) = N'
<p><strong>Platform:</strong> otelturizm.com</p>
<p><strong>Kapsam:</strong> Üyelik, rezervasyon, iptal/değişiklik, iletişim, içerik paylaşımı (yorumlar), hesap güvenliği ve bildirim süreçleri.</p>
<p><strong>Yürürlük:</strong> Versiyon 1 · 20 Nisan 2026 itibarıyla.</p>
';

DECLARE @agreementContent nvarchar(max) = N'
<h2>1. Taraflar ve konu</h2>
<p>İşbu <strong>Kullanıcı Kullanım Koşulları</strong> (“Sözleşme”), <strong>Otelturizm</strong> markasıyla işletilen <strong>otelturizm.com</strong> alan adlı platformu (“Platform”) kullanan bireysel kullanıcı (“Kullanıcı”) ile Platform işletmecisi (“Platform İşleticisi”) arasında, üyelik ve rezervasyon süreçlerine ilişkin temel kuralları düzenler.</p>

<h2>2. Tanımlar</h2>
<ul>
  <li><strong>Hizmet:</strong> Otel/tesis arama, listeleme, rezervasyon talebi oluşturma, iletişim, kullanıcı paneli ve ilgili dijital hizmetler.</li>
  <li><strong>Tesis/Partner:</strong> Platformda listelenen, konaklama hizmetini sunan gerçek/tüzel kişi işletme.</li>
  <li><strong>Rezervasyon:</strong> Platform üzerinden oluşturulan konaklama talebi ve/veya onaylanan konaklama kaydı.</li>
</ul>

<h2>3. Üyelik ve hesap oluşturma</h2>
<ul>
  <li>Kullanıcı, üyelik sırasında verdiği bilgilerin doğru ve güncel olduğunu; başkasına ait bilgileri izinsiz kullanmadığını kabul eder.</li>
  <li>Platform, güvenlik gerekçeleriyle ek doğrulama adımları isteyebilir ve hesabı şüpheli işlemlerde geçici olarak kısıtlayabilir.</li>
  <li>Kullanıcı, hesabına ilişkin işlemlerin sorumluluğunun kendisine ait olduğunu; oturum bilgilerini üçüncü kişilerle paylaşmayacağını kabul eder.</li>
</ul>

<h2>4. Rezervasyon süreci</h2>
<ul>
  <li>Platformda gösterilen uygunluk, fiyat ve koşullar; tesisin sağladığı envanter, kurallar ve dönemsel koşullara göre değişebilir.</li>
  <li>Rezervasyon talebinin oluşturulması, her zaman tesis tarafından <em>onay</em> anlamına gelmez. Nihai durum, rezervasyon kaydında ve bildirimlerde belirtilir.</li>
  <li>Kullanıcı, rezervasyonun tamamlanması için gerekli profil bilgilerini eksiksiz sağlamayı kabul eder.</li>
</ul>

<h2>5. İptal, değişiklik ve iade</h2>
<ul>
  <li>İptal/değişiklik koşulları, tesisin belirlediği kurallar ve rezervasyonun durumuna göre farklılık gösterebilir.</li>
  <li>Ödeme yöntemi ve tahsilat planı (kapıda/online/havale), rezervasyon kaydında belirtildiği şekilde uygulanır.</li>
  <li>Kullanıcı, iptal/değişiklik taleplerinin tesisin onay süreçlerine ve mevzuata tabi olduğunu kabul eder.</li>
</ul>

<h2>6. Kullanıcı notları, özel talepler ve iletişim</h2>
<ul>
  <li>Kullanıcı, rezervasyonla ilgili not/özel talep alanlarına hukuka uygun içerik gireceğini; üçüncü kişilerin haklarını ihlal etmeyeceğini kabul eder.</li>
  <li>Platform üzerinden gönderilen mesajlar kayıt altına alınabilir; kötüye kullanım tespitinde hesap kısıtlanabilir.</li>
</ul>

<h2>7. Yorumlar ve puanlama</h2>
<ul>
  <li>Yorumlar, mümkün olduğunda doğrulanmış konaklama kayıtlarına dayandırılır.</li>
  <li>Kullanıcı, yorumlarında hakaret, iftira, ayrımcılık, kişisel veri ifşası ve hukuka aykırı içerik paylaşmayacağını kabul eder.</li>
  <li>Platform, mevzuata ve topluluk kurallarına aykırı yorumları kaldırabilir veya görünürlüğünü sınırlandırabilir.</li>
</ul>

<h2>8. Fiyatlar, kampanyalar ve içerikler</h2>
<ul>
  <li>Kampanya, kupon ve indirimler belirli koşullara tabi olabilir; aynı rezervasyonda birden fazla avantaj birleştirilemeyebilir.</li>
  <li>Platformda yer alan metin, görsel ve marka unsurları ilgili hak sahiplerine aittir; izinsiz kullanım yasaktır.</li>
</ul>

<h2>9. Sorumluluğun sınırları</h2>
<ul>
  <li>Konaklama hizmeti, ilgili tesis tarafından sunulur. Platform, tesisin sunduğu hizmetin ifasından doğrudan sorumlu değildir; ancak mevzuat kapsamında aracı hizmet sağlayıcılara düşen yükümlülükleri yerine getirmeyi hedefler.</li>
  <li>Platform, teknik arızalar, mücbir sebepler veya üçüncü taraf hizmet sağlayıcı kaynaklı kesintiler nedeniyle oluşabilecek zararları, mevzuatın izin verdiği ölçüde sınırlar.</li>
</ul>

<h2>10. Bildirimler ve elektronik ileti</h2>
<p>Kullanıcı, hesap ve rezervasyon bildirimlerinin e‑posta/uygulama içi bildirimler yoluyla iletilebileceğini kabul eder. Ticari iletiler için ayrıca tercih ve onay mekanizmaları uygulanabilir.</p>

<h2>11. Değişiklikler</h2>
<p>Platform İşleticisi, mevzuat değişiklikleri veya hizmet güncellemeleri nedeniyle Sözleşme’yi güncelleyebilir. Güncel versiyon Platform üzerinden yayımlanır.</p>

<h2>12. Uyuşmazlıkların çözümü</h2>
<p>Uyuşmazlıkların çözümünde Türkiye Cumhuriyeti mevzuatı uygulanır. Yetkili merci, tüketici işleminin niteliğine göre tüketici hakem heyetleri/mahkemeleri ve genel görevli mahkemeler olabilir.</p>
';

DECLARE @kvkkSummary nvarchar(max) = N'
<p>Bu metin, <strong>6698 sayılı Kişisel Verilerin Korunması Kanunu</strong> (“KVKK”) kapsamında, otelturizm.com bireysel kullanıcı üyeliği ve rezervasyon süreçlerinde işlenen kişisel veriler hakkında aydınlatma sağlar.</p>
';

DECLARE @kvkkContent nvarchar(max) = N'
<h2>1. Veri sorumlusu</h2>
<p>Bu aydınlatma metni kapsamında kişisel verileriniz, Platform üzerinden sunulan hizmetlerin yürütülmesi amacıyla <strong>Otelturizm</strong> (“Veri Sorumlusu”) tarafından işlenebilir.</p>

<h2>2. İşlenen kişisel veri kategorileri</h2>
<ul>
  <li><strong>Kimlik ve iletişim:</strong> Ad‑soyad, e‑posta, telefon, (varsa) T.C. kimlik no, doğum tarihi, cinsiyet, uyruk.</li>
  <li><strong>Rezervasyon bilgileri:</strong> Konaklama tarihleri, otel/oda seçimi, misafir sayıları, notlar/özel talepler.</li>
  <li><strong>Finans ve işlem:</strong> Ödeme yöntemi, ödeme durumları, tahsilat bilgileri (mevzuat ve zorunluluklar çerçevesinde).</li>
  <li><strong>Hesap ve güvenlik:</strong> Giriş/oturum kayıtları, IP, cihaz etiketi, 2FA tercihleri, güvenlik logları.</li>
  <li><strong>İletişim içerikleri:</strong> Platform içi mesajlaşma ve destek talepleri kapsamında paylaşılan içerikler.</li>
</ul>

<h2>3. Kişisel verilerin işlenme amaçları</h2>
<ul>
  <li>Üyelik oluşturma, hesap yönetimi ve kullanıcı doğrulama süreçlerinin yürütülmesi.</li>
  <li>Rezervasyon talebi oluşturma, rezervasyon yönetimi, iptal/değişiklik ve müşteri iletişimi süreçlerinin yürütülmesi.</li>
  <li>Finansal işlemlerin, faturalama ve muhasebe süreçlerinin yürütülmesi.</li>
  <li>Bilgi güvenliği, suistimal/üye güvenliği kontrolleri ve sistemsel risklerin yönetimi.</li>
  <li>Hizmet kalitesinin geliştirilmesi, müşteri memnuniyeti ve destek süreçlerinin iyileştirilmesi.</li>
</ul>

<h2>4. Hukuki sebepler</h2>
<p>Kişisel verileriniz; KVKK madde 5/2 kapsamında <em>sözleşmenin kurulması/ifası</em>, <em>hukuki yükümlülüğün yerine getirilmesi</em>, <em>bir hakkın tesisi/kullanılması/korunması</em> ve <em>meşru menfaat</em> gibi hukuki sebeplerle; gerekli hallerde açık rızanıza dayalı olarak işlenebilir.</p>

<h2>5. Kişisel verilerin aktarımı</h2>
<p>Kişisel verileriniz, rezervasyonun yürütülmesi için ilgili <strong>tesis/partner</strong> ile; yasal yükümlülüklerin yerine getirilmesi için yetkili kurum/kuruluşlarla; altyapı sağlayıcıları (barındırma, e‑posta gönderimi, güvenlik vb.) ile paylaşılabilir. Aktarımlar, amaçla sınırlı ve ölçülü şekilde gerçekleştirilir.</p>

<h2>6. Saklama süreleri</h2>
<p>Kişisel verileriniz; ilgili mevzuatta öngörülen süreler ve işleme amaçları için gerekli olan süre boyunca saklanır. Süre sonunda veri silme, yok etme veya anonimleştirme süreçleri uygulanır.</p>

<h2>7. KVKK kapsamındaki haklarınız</h2>
<p>KVKK madde 11 kapsamında; kişisel verilerinizin işlenip işlenmediğini öğrenme, işlenmişse bilgi talep etme, amacına uygun kullanılıp kullanılmadığını öğrenme, eksik/yanlış işlenmişse düzeltilmesini isteme, silinmesini/yok edilmesini isteme ve kanunda sayılan diğer haklara sahipsiniz.</p>

<h2>8. Başvuru yöntemi</h2>
<p>Haklarınıza ilişkin taleplerinizi, Platform üzerinden veya destek kanalları aracılığıyla iletebilirsiniz. Başvurular, kimlik doğrulama ve mevzuattaki süreler çerçevesinde değerlendirilir.</p>
';

-- Upsert: Kullanıcı Kullanım Koşulları (v1)
IF EXISTS (SELECT 1 FROM dbo.sozlesmeler WHERE slug = @userAgreementSlug AND versiyon_no = 1)
BEGIN
    UPDATE dbo.sozlesmeler
    SET hedef_kitle = N'user',
        sozlesme_tipi = N'agreement',
        baslik = @agreementTitle,
        alt_baslik = @agreementSubtitle,
        ozet_html = @agreementSummary,
        icerik_html = @agreementContent,
        gorsel_url = NULL,
        sozlesme_linki = NULL,
        baslangic_tarihi = @effective,
        bitis_tarihi = NULL,
        kabul_gerektirir_mi = 1,
        email_dogrulamada_gonder = 1,
        yenileme_gerekir_mi = 0,
        yenileme_periyodu_gun = NULL,
        aktif_mi = 1,
        notlar = N'Versiyon 1 seed (2026-05-04).',
        guncellenme_tarihi = SYSUTCDATETIME()
    WHERE slug = @userAgreementSlug AND versiyon_no = 1;
END
ELSE
BEGIN
    INSERT INTO dbo.sozlesmeler
    (hedef_kitle, sozlesme_tipi, baslik, alt_baslik, slug, ozet_html, icerik_html, gorsel_url, sozlesme_linki,
     versiyon_no, baslangic_tarihi, bitis_tarihi, kabul_gerektirir_mi, email_dogrulamada_gonder, yenileme_gerekir_mi, yenileme_periyodu_gun,
     aktif_mi, notlar, olusturulma_tarihi, guncellenme_tarihi)
    VALUES
    (N'user', N'agreement', @agreementTitle, @agreementSubtitle, @userAgreementSlug, @agreementSummary, @agreementContent, NULL, NULL,
     1, @effective, NULL, 1, 1, 0, NULL,
     1, N'Versiyon 1 seed (2026-05-04).', SYSUTCDATETIME(), SYSUTCDATETIME());
END

-- Upsert: Kullanıcı KVKK Aydınlatma (v1)
IF EXISTS (SELECT 1 FROM dbo.sozlesmeler WHERE slug = @userKvkkSlug AND versiyon_no = 1)
BEGIN
    UPDATE dbo.sozlesmeler
    SET hedef_kitle = N'user',
        sozlesme_tipi = N'kvkk',
        baslik = @kvkkTitle,
        alt_baslik = @kvkkSubtitle,
        ozet_html = @kvkkSummary,
        icerik_html = @kvkkContent,
        gorsel_url = NULL,
        sozlesme_linki = NULL,
        baslangic_tarihi = @effective,
        bitis_tarihi = NULL,
        kabul_gerektirir_mi = 1,
        email_dogrulamada_gonder = 1,
        yenileme_gerekir_mi = 0,
        yenileme_periyodu_gun = NULL,
        aktif_mi = 1,
        notlar = N'Versiyon 1 seed (2026-05-04).',
        guncellenme_tarihi = SYSUTCDATETIME()
    WHERE slug = @userKvkkSlug AND versiyon_no = 1;
END
ELSE
BEGIN
    INSERT INTO dbo.sozlesmeler
    (hedef_kitle, sozlesme_tipi, baslik, alt_baslik, slug, ozet_html, icerik_html, gorsel_url, sozlesme_linki,
     versiyon_no, baslangic_tarihi, bitis_tarihi, kabul_gerektirir_mi, email_dogrulamada_gonder, yenileme_gerekir_mi, yenileme_periyodu_gun,
     aktif_mi, notlar, olusturulma_tarihi, guncellenme_tarihi)
    VALUES
    (N'user', N'kvkk', @kvkkTitle, @kvkkSubtitle, @userKvkkSlug, @kvkkSummary, @kvkkContent, NULL, NULL,
     1, @effective, NULL, 1, 1, 0, NULL,
     1, N'Versiyon 1 seed (2026-05-04).', SYSUTCDATETIME(), SYSUTCDATETIME());
END

