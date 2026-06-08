-- Idempotent: kullanici-kvkk-aydinlatma ozet (cift encode & kesik metin) ve icerik HTML duzeltmesi
-- UTF-8 BOM onerilir: sqlcmd -f 65001
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

DECLARE @ozet nvarchar(max) = N'
<p><strong>Veri sorumlusu:</strong> Otelturizm.com işletmecisi (&ldquo;Şirket&rdquo;).</p>
<p><strong>Kapsam:</strong> Platformu kullanan müşteriler, ziyaretçiler ve partner otel çalışanları ile yetkilileri.</p>
<p>6698 sayılı Kişisel Verilerin Korunması Kanunu (&ldquo;KVKK&rdquo;) kapsamında kişisel verileriniz; veri kategorileri, işleme amaçları, aktarım alıcıları ve yasal saklama süreleri çerçevesinde aşağıda açıklanmaktadır.</p>';

DECLARE @icerik nvarchar(max) = N'
<h2>Kişisel Verilerin Korunması (KVKK) Aydınlatma Metni</h2>
<div class="sozlesme-kvkk-lead">
<p><strong>Veri sorumlusu:</strong> Otelturizm.com işletmecisi (&ldquo;Şirket&rdquo;)</p>
<p><strong>Kapsam:</strong> Bu metin; Platformu kullanan müşterileri, ziyaretçileri ve partner firma (otel) çalışanları ile yetkililerini kapsar.</p>
</div>
<p>6698 sayılı Kişisel Verilerin Korunması Kanunu (&ldquo;KVKK&rdquo;) uyarınca Şirket, veri sorumlusu sıfatıyla kişisel verilerinizi; aşağıda açıklanan amaçlar kapsamında, hukuka ve dürüstlük kurallarına uygun olarak, yasal saklama süreleri boyunca işleyebilir, kaydedebilir ve aktarabilir.</p>
<h2>1. İşlenen kişisel veri kategorileri ve işlenme amaçları</h2>
<h3>A. Müşteriler için</h3>
<ul>
<li><strong>Kimlik ve iletişim verileri</strong> (ad, soyad, T.C. kimlik numarası, pasaport, telefon, e-posta): Rezervasyonun oluşturulması ve teyidi, fatura kesimi, konaklama kaydının yapılması, değişiklik/iptal süreçlerinde iletişim kurulması. <em>Hukuki sebep:</em> Sözleşmenin ifası ve yasal yükümlülük.</li>
<li><strong>Finansal veriler</strong> (maskelenmiş kart bilgisi, ödeme tutarı): Ödemenin güvenli alınması ve muhasebe kayıtlarının tutulması.</li>
<li><strong>İşlem güvenliği verileri</strong> (IP adresi, log kayıtları): 5651 sayılı Kanun kapsamında sistem güvenliğinin sağlanması.</li>
</ul>
<h3>B. Partner firmalar (oteller) için</h3>
<p>Otel yetkililerine ait kimlik, iletişim ve imza verileri; ticari sözleşmenin kurulması, komisyon ödemeleri ve mutabakatların yapılması, yasal fatura süreçlerinin yönetilmesi amacıyla işlenir.</p>
<h2>2. Kişisel verilerin aktarımı</h2>
<p>Toplanan kişisel verileriniz; işleme amaçlarıyla sınırlı olmak üzere aşağıdaki alıcılara aktarılabilir:</p>
<ul>
<li><strong>Partner oteller:</strong> Rezervasyon yapan müşteriye ait ad, soyad ve iletişim bilgileri; hizmetin sunulması ve Kimlik Bildirme Kanunu kapsamındaki yasal giriş işlemleri için yalnızca ilgili otelle paylaşılır.</li>
<li><strong>Ödeme ve altyapı kuruluşları:</strong> Tahsilat ve komisyon transferleri için BDDK/TCMB lisanslı ödeme kuruluşlarına (sanal POS hizmet sağlayıcıları) aktarılır.</li>
<li><strong>Yetkili kamu kurumları:</strong> Emniyet Genel Müdürlüğü (KBS), maliye ve adli mercilerden gelen yasal talepler doğrultusunda yetkili makamlara aktarılabilir.</li>
</ul>
<h2>3. İlgili kişinin hakları ve başvuru (KVKK md. 11)</h2>
<p>KVKK''nın 11. maddesi kapsamında;</p>
<ul>
<li>Kişisel verilerinizin işlenip işlenmediğini öğrenme,</li>
<li>İşlenme amacına uygun kullanılıp kullanılmadığını bilme,</li>
<li>Yurt içinde veya yurt dışında aktarıldığı üçüncü kişileri bilme,</li>
<li>Eksik veya yanlış işlenmişse düzeltilmesini isteme,</li>
<li>Kanunda öngörülen şartlar çerçevesinde silinmesini veya yok edilmesini isteme</li>
</ul>
<p>haklarına sahipsiniz.</p>
<p>Bu haklarınıza ilişkin taleplerinizi &ldquo;Veri Sorumlusuna Başvuru Usul ve Esasları Hakkında Tebliğ&rdquo; uyarınca <a href="mailto:kvkk@otelturizm.com">kvkk@otelturizm.com</a> adresine e-posta göndererek veya kayıtlı şirket adresimize ıslak imzalı dilekçe ile iletebilirsiniz.</p>
<p>Detaylı bilgi için <a href="/Home/Privacy">Gizlilik Politikası</a> sayfasını inceleyebilirsiniz.</p>';

UPDATE [dbo].[SOZLESMELER]
SET
    [OZET_HTML] = @ozet,
    [ICERIK_HTML] = @icerik,
    [GUNCELLENME_TARIHI] = SYSUTCDATETIME()
WHERE [SLUG] = N'kullanici-kvkk-aydinlatma'
  AND [AKTIF_MI] = 1;

IF @@ROWCOUNT = 0
BEGIN
    RAISERROR(N'kullanici-kvkk-aydinlatma aktif kaydi bulunamadi.', 16, 1);
END

SELECT
    s.[SLUG],
    s.[VERSIYON_NO],
    LEN(s.[OZET_HTML]) AS ozet_len,
    LEFT(REPLACE(REPLACE(s.[OZET_HTML], CHAR(13), N''), CHAR(10), N' '), 120) AS ozet_preview
FROM [dbo].[SOZLESMELER] s
WHERE s.[SLUG] = N'kullanici-kvkk-aydinlatma'
  AND s.[AKTIF_MI] = 1;
