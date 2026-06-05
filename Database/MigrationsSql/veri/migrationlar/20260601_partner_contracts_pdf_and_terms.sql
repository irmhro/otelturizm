SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;

DECLARE @PartnerAgreementSummary nvarchar(max) = N'<p>Otelturizm partner ekosisteminde listeleme, rezervasyon, komisyon, ödeme, misafir önceliği, belge doğrulama ve platform karar süreçlerini düzenleyen başvuru ve platform kullanım sözleşmesidir.</p>';
DECLARE @PartnerAgreementHtml nvarchar(max) = N'
<h2>1. Taraflar ve başvuru niteliği</h2>
<p>İşbu Partner Başvuru ve Platform Sözleşmesi; Otelturizm platformu ile başvuru yapan tesis işletmecisi, konaklama sağlayıcısı veya yetkili temsilcisi arasında elektronik ortamda kurulacak ticari iş birliği çerçevesini düzenler. Başvuru sahibinin formda sunduğu tüm bilgi ve belgeler resmi kayıtlarla uyumlu olmalı, yetkisiz veya gerçeğe aykırı başvuru yapılmamalıdır.</p>
<h2>2. Platformun rolü ve hizmet kapsamı</h2>
<p>Otelturizm; otel, oda, fiyat, müsaitlik, kampanya, rezervasyon, bildirim, raporlama, komisyon ve destek süreçlerinin dijital ortamda yönetilmesini sağlayan aracı ve teknoloji platformudur. Konaklama hizmetinin fiili ifası Partner sorumluluğundadır.</p>
<h2>3. Başvuru incelemesi, belge doğrulama ve yayın kararı</h2>
<p>Partner hesabı, e-posta doğrulaması ve admin incelemesi tamamlanmadan tam panel kullanımına ve tesis yayınına açılmaz. Otelturizm; vergi levhası, ticaret sicili, imza sirküleri, turizm belgesi, yetki belgesi, IBAN ve gerekli görülen ek evrakları isteme, doğrulama, eksik evrakta başvuruyu bekletme, askıya alma veya reddetme hakkını saklı tutar.</p>
<h2>4. Listeleme, fiyat ve müsaitlik sorumluluğu</h2>
<p>Partner; tesis adı, adres, sınıf, fotoğraf, oda tipi, kontenjan, fiyat, vergi, dahil hizmet, iptal koşulu ve müsaitlik bilgilerinin doğru, güncel ve mevzuata uygun olmasından sorumludur. Yanlış fiyat, yanıltıcı görsel, eksik bilgi veya çift satış nedeniyle oluşabilecek misafir mağduriyetlerinde çözüm önceliği misafirdedir.</p>
<h2>5. Rezervasyon, misafir önceliği ve operasyon standardı</h2>
<p>Platform üzerinden oluşan rezervasyonlar Partner tarafından zamanında takip edilir. Partner, onay, giriş yaptı, iptal, no-show, tarih değişikliği, fatura ve ödeme aksiyonlarını panelden doğru işler. Misafir güvenliği, konaklama hakkı, tüketici bilgilendirmesi ve hızlı çözüm ilkesi platform kararlarında önceliklidir.</p>
<h2>6. Komisyon, tahakkuk ve ödeme</h2>
<p>Partner, platform üzerinden gerçekleşen tamamlanmış rezervasyonlar için panelde tanımlı oran ve kurallara göre komisyon ödemeyi kabul eder. Komisyon; rezervasyon tutarı, kampanya, indirim, vergi dahil/harç kalemleri ve mutabakat kuralları dikkate alınarak hesaplanır. Otelturizm, her dönem komisyon bildirimi ve mutabakat kaydı üretir.</p>
<h2>7. İptal, iade, no-show ve tüketici hakları</h2>
<p>İptal, iade, tarih değişikliği ve no-show süreçlerinde 6502 sayılı Tüketicinin Korunması Hakkında Kanun, Mesafeli Sözleşmeler Yönetmeliği ve ilgili mevzuat dikkate alınır. Partner, tüketiciyi yanıltan, iade hakkını haksız sınırlayan veya rezervasyon koşullarına aykırı işlem yapamaz.</p>
<h2>8. Fatura, vergi ve mali yükümlülükler</h2>
<p>Partner; kendi vergi, fatura, belge saklama, e-arşiv/e-fatura, konaklama vergisi ve yasal bildirim yükümlülüklerinden sorumludur. Konaklama tamamlandığında kullanıcı veya firma adına düzenlenen faturalar panelden güvenli şekilde yüklenir.</p>
<h2>9. KVKK, gizlilik ve veri güvenliği</h2>
<p>Partner, misafir ve firma çalışanı kişisel verilerini yalnızca rezervasyonun ifası, fatura, destek ve mevzuat yükümlülükleri için işler. Veriler yetkisiz kişilerle paylaşılamaz; ekran görüntüsü, dışa aktarım ve belge indirme işlemlerinde gizlilik korunur.</p>
<h2>10. Platform kuralları ve karar üstünlüğü</h2>
<p>Dolandırıcılık şüphesi, sahte belge, yanıltıcı fiyat, stok manipülasyonu, misafir mağduriyeti, güvenlik riski, ödeme ihtilafı veya mevzuata aykırılık hâlinde Otelturizm; tesisi geçici olarak askıya alma, sıralamayı düşürme, rezervasyon kabulünü kapatma, belge yenileme isteme ve nihai platform kararını verme hakkına sahiptir.</p>
<h2>11. Uyuşmazlık ve çözüm süreci</h2>
<p>Taraflar, uyuşmazlıklarda önce panel destek kayıtları, e-posta bildirimleri ve mutabakat kayıtları üzerinden iyi niyetli çözüm sürecini işletir. Tüketici başvuruları ve resmi mercilerden gelen talepler öncelikli ele alınır. Yetkili mahkeme ve icra daireleri, emredici tüketici hükümleri saklı kalmak kaydıyla İstanbul mahkemeleri ve icra daireleridir.</p>
<h2>12. Elektronik kabul ve yürürlük</h2>
<p>Başvuru formundaki onay kutularının işaretlenmesi, IP/kayıt zamanı, e-posta doğrulaması ve sözleşme gönderim logları elektronik kabul kaydıdır. Sözleşme, başvuru tarihinde elektronik ortamda kabul edilmiş sayılır; tesisin yayına alınması ayrıca admin onayına bağlıdır.</p>
<p><strong>Not:</strong> Bu metin platform çalışma standardını belirleyen sözleşme taslağıdır; mevzuat değişiklikleri ve şirket bilgilerinin kesinleşmesi halinde güncellenebilir.</p>';

DECLARE @PartnerKvkkHtml nvarchar(max) = N'
<h2>1. Veri sorumlusu ve kapsam</h2>
<p>Bu aydınlatma metni, partner başvurusu ve tesis yönetimi süreçlerinde işlenen kişisel veriler hakkında 6698 sayılı Kişisel Verilerin Korunması Kanunu kapsamında bilgilendirme amacıyla hazırlanmıştır.</p>
<h2>2. İşlenen veri kategorileri</h2>
<p>Yetkili ad soyad, iletişim bilgileri, görev/unvan, kimlik doğrulama bilgileri, vergi ve ticaret kayıtları, tesis evrakları, banka/IBAN bilgileri, panel işlem kayıtları, IP ve cihaz kayıtları, destek yazışmaları, rezervasyon ve fatura süreçlerine ilişkin veriler işlenebilir.</p>
<h2>3. İşleme amaçları</h2>
<p>Başvurunun alınması, yetki ve belge doğrulaması, tesis yayın kararı, rezervasyon operasyonu, komisyon/mutabakat, fatura yönetimi, güvenlik, dolandırıcılık önleme, destek, mevzuat yükümlülükleri ve platform kalite denetimi amaçlarıyla veri işlenir.</p>
<h2>4. Aktarım</h2>
<p>Veriler; mevzuatın izin verdiği ölçüde yetkili kamu kurumları, ödeme ve e-posta hizmet sağlayıcıları, barındırma/altyapı hizmetleri, hukuk/muhasebe danışmanları ve rezervasyon sürecinin gerektirdiği taraflarla paylaşılabilir.</p>
<h2>5. Saklama ve güvenlik</h2>
<p>Veriler, işleme amacı ve yasal saklama süreleriyle sınırlı olarak tutulur. Evrak ve dosyalar güvenli dosya alanında, erişim tokenları ve yetki kontrolleriyle korunur.</p>
<h2>6. İlgili kişi hakları</h2>
<p>KVKK madde 11 kapsamındaki başvurularınızı Otelturizm destek kanalları veya kayıtlı iletişim adresleri üzerinden iletebilirsiniz.</p>';

UPDATE dbo.SOZLESMELER
SET BASLIK = N'Partner Başvuru Sözleşmesi',
    ALT_BASLIK = N'Partner başvurusu, tesis yayını, komisyon, ödeme, belge doğrulama ve misafir öncelikli platform kuralları.',
    SLUG = N'partner-basvuru-sozlesmesi',
    OZET_HTML = @PartnerAgreementSummary,
    ICERIK_HTML = @PartnerAgreementHtml,
    VERSIYON_NO = CASE WHEN VERSIYON_NO < 2 THEN 2 ELSE VERSIYON_NO END,
    KABUL_GEREKTIRIR_MI = 1,
    EPOSTA_DOGRULAMADA_GONDER = 1,
    AKTIF_MI = 1
WHERE HEDEF_KITLE = N'partner'
  AND SOZLESME_TIPI = N'agreement';

UPDATE dbo.SOZLESMELER
SET BASLIK = N'Partner KVKK Aydınlatma Metni',
    ALT_BASLIK = N'6698 sayılı Kanun kapsamında partner başvuru, tesis yönetimi ve operasyon verilerinin işlenmesine ilişkin bilgilendirme.',
    SLUG = N'partner-kvkk-aydinlatma',
    OZET_HTML = N'<p>Partner başvurusu ve tesis yönetimi sırasında işlenen kişisel verilere ilişkin KVKK bilgilendirmesidir.</p>',
    ICERIK_HTML = @PartnerKvkkHtml,
    VERSIYON_NO = CASE WHEN VERSIYON_NO < 2 THEN 2 ELSE VERSIYON_NO END,
    KABUL_GEREKTIRIR_MI = 1,
    EPOSTA_DOGRULAMADA_GONDER = 1,
    AKTIF_MI = 1
WHERE HEDEF_KITLE = N'partner'
  AND SOZLESME_TIPI = N'kvkk';

IF OBJECT_ID(N'dbo.SOZLESME_DOSYALARI', N'U') IS NOT NULL
BEGIN
    DECLARE @AgreementId bigint = (SELECT TOP (1) id FROM dbo.SOZLESMELER WHERE SLUG = N'partner-basvuru-sozlesmesi' ORDER BY VERSIYON_NO DESC, id DESC);
    DECLARE @KvkkId bigint = (SELECT TOP (1) id FROM dbo.SOZLESMELER WHERE SLUG = N'partner-kvkk-aydinlatma' ORDER BY VERSIYON_NO DESC, id DESC);

    IF @AgreementId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.SOZLESME_DOSYALARI WHERE SOZLESME_ID = @AgreementId AND DOSYA_TIPI = N'pdf' AND DOSYA_YOLU = N'/uploads/contracts/partner-basvuru-sozlesmesi-v2.pdf')
    BEGIN
        INSERT INTO dbo.SOZLESME_DOSYALARI (SOZLESME_ID, DOSYA_TIPI, DOSYA_ADI, DOSYA_YOLU, MIME_TIPI, OLUSTURULMA_TARIHI)
        VALUES (@AgreementId, N'pdf', N'Partner Başvuru Sözleşmesi v2.pdf', N'/uploads/contracts/partner-basvuru-sozlesmesi-v2.pdf', N'application/pdf', SYSUTCDATETIME());
    END;

    IF @KvkkId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.SOZLESME_DOSYALARI WHERE SOZLESME_ID = @KvkkId AND DOSYA_TIPI = N'pdf' AND DOSYA_YOLU = N'/uploads/contracts/partner-kvkk-aydinlatma-v2.pdf')
    BEGIN
        INSERT INTO dbo.SOZLESME_DOSYALARI (SOZLESME_ID, DOSYA_TIPI, DOSYA_ADI, DOSYA_YOLU, MIME_TIPI, OLUSTURULMA_TARIHI)
        VALUES (@KvkkId, N'pdf', N'Partner KVKK Aydınlatma Metni v2.pdf', N'/uploads/contracts/partner-kvkk-aydinlatma-v2.pdf', N'application/pdf', SYSUTCDATETIME());
    END;
END;
