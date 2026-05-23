-- Seed: kullanıcı KVKK aydınlatma + kullanım koşulları (footer yasal linkleri, idempotent)
-- UTF-8 BOM recommended when applied manually via sqlcmd.

IF NOT EXISTS (SELECT 1 FROM [dbo].[SOZLESMELER] WHERE [SLUG] = N'kullanici-kullanim-kosullari' AND [VERSIYON_NO] = 1)
BEGIN
    INSERT INTO [dbo].[SOZLESMELER] (
        [HEDEF_KITLE], [SOZLESME_TIPI], [BASLIK], [ALT_BASLIK], [SLUG],
        [OZET_HTML], [ICERIK_HTML], [GORSEL_URL], [VERSIYON_NO],
        [KABUL_GEREKTIRIR_MI], [EPOSTA_DOGRULAMADA_GONDER], [YENILEME_GEREKIR_MI], [AKTIF_MI]
    ) VALUES (
        N'user', N'agreement', N'Kullanım Koşulları',
        N'Otelturizm platformunu kullanırken geçerli kurallar ve sorumluluklar.',
        N'kullanici-kullanim-kosullari',
        N'<p>Bu metin Otelturizm web sitesi ve mobil deneyiminde sunulan hizmetlerin kullanımına ilişkin genel şartları açıklar.</p>',
        N'<h2>1. Taraflar ve kapsam</h2><p>Otelturizm platformu üzerinden otel arama, karşılaştırma ve rezervasyon oluşturma hizmetlerinden yararlanırken bu koşullar geçerlidir.</p><h2>2. Hesap ve güvenlik</h2><p>Hesap bilgilerinizin gizliliğinden siz sorumlusunuz. Şüpheli oturumları destek kanallarımıza bildirmenizi öneririz.</p><h2>3. Rezervasyon</h2><p>Rezervasyon özeti, iptal ve iade koşulları ilgili otel/kampanya kuralları ve hesabınızdaki rezervasyon detayında gösterilir.</p><h2>4. Fikri mülkiyet</h2><p>Platform içeriği, marka ve arayüz unsurları izinsiz kopyalanamaz.</p><h2>5. Sorumluluk sınırı</h2><p>Hizmet &quot;olduğu gibi&quot; sunulur; zorunlu tüketici mevzuatı saklıdır.</p><h2>6. Değişiklikler</h2><p>Koşullar güncellenebilir; yürürlük tarihi sayfa üzerinde belirtilir.</p><h2>7. İletişim</h2><p>Sorularınız için <a href="/yardim-merkezi">Yardım Merkezi</a> veya destek@otelturizm.com.</p>',
        NULL, 1, 1, 1, 0, 1
    );
END

IF NOT EXISTS (SELECT 1 FROM [dbo].[SOZLESMELER] WHERE [SLUG] = N'kullanici-kvkk-aydinlatma' AND [VERSIYON_NO] = 1)
BEGIN
    INSERT INTO [dbo].[SOZLESMELER] (
        [HEDEF_KITLE], [SOZLESME_TIPI], [BASLIK], [ALT_BASLIK], [SLUG],
        [OZET_HTML], [ICERIK_HTML], [GORSEL_URL], [VERSIYON_NO],
        [KABUL_GEREKTIRIR_MI], [EPOSTA_DOGRULAMADA_GONDER], [YENILEME_GEREKIR_MI], [AKTIF_MI]
    ) VALUES (
        N'user', N'kvkk', N'KVKK Aydınlatma Metni',
        N'6698 sayılı Kişisel Verilerin Korunması Kanunu kapsamında bilgilendirme.',
        N'kullanici-kvkk-aydinlatma',
        N'<p>Veri sorumlusu sıfatıyla Otelturizm; kişisel verilerinizi mevzuata uygun şekilde işler.</p>',
        N'<h2>1. Veri sorumlusu</h2><p>Otelturizm platformu kapsamında kişisel verileriniz veri sorumlusu olarak işlenmektedir.</p><h2>2. İşlenen veri kategorileri</h2><ul><li>Kimlik ve iletişim (ad, e-posta, telefon)</li><li>Rezervasyon ve işlem bilgileri</li><li>İşlem güvenliği (IP, oturum, log kayıtları)</li></ul><h2>3. Amaçlar</h2><p>Hizmet sunumu, sözleşmenin ifası, güvenlik, müşteri desteği ve yasal yükümlülükler.</p><h2>4. Aktarım</h2><p>Rezervasyonun yürütülmesi için konaklama tesisleri, ödeme kuruluşları ve zorunlu resmi merciler ile paylaşım yapılabilir.</p><h2>5. Haklarınız</h2><p>KVKK md. 11 kapsamındaki taleplerinizi destek@otelturizm.com üzerinden iletebilirsiniz.</p><h2>6. Saklama</h2><p>Veriler işleme amacının gerektirdiği süre ve mevzuattaki saklama süreleri boyunca tutulur.</p><p>Detaylı bilgi için <a href="/Home/Privacy">Gizlilik Politikası</a> sayfasını inceleyebilirsiniz.</p>',
        NULL, 1, 1, 1, 0, 1
    );
END
