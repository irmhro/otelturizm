-- Seed: partner platform + firma kurumsal sözleşme iskeletleri (idempotent)
-- UTF-8 BOM recommended when applied manually via sqlcmd.
-- Yasal metinler şablon/placeholder; canlı öncesi hukuk incelemesi zorunludur.

IF NOT EXISTS (SELECT 1 FROM [dbo].[SOZLESMELER] WHERE [SLUG] = N'partner-platform-sozlesmesi' AND [VERSIYON_NO] = 1)
BEGIN
    INSERT INTO [dbo].[SOZLESMELER] (
        [HEDEF_KITLE], [SOZLESME_TIPI], [BASLIK], [ALT_BASLIK], [SLUG],
        [OZET_HTML], [ICERIK_HTML], [GORSEL_URL], [VERSIYON_NO],
        [KABUL_GEREKTIRIR_MI], [EPOSTA_DOGRULAMADA_GONDER], [YENILEME_GEREKIR_MI], [AKTIF_MI]
    ) VALUES (
        N'partner', N'agreement', N'Partner Platform Sözleşmesi',
        N'Otelturizm aracılık platformunda tesis işletmecisi olarak listeleme ve rezervasyon hizmetleri.',
        N'partner-platform-sozlesmesi',
        N'<p>[PARTNER_UNVAN] ile Otelturizm arasında platform kullanımı, komisyon, KVKK ve uyuşmazlık hükümlerini içeren çerçeve sözleşmedir.</p>',
        N'<h2>1. TARAFLAR</h2>
<p><strong>Platform İşletmecisi:</strong> Otelturizm ([PLATFORM_UNVAN], MERSİS: [PLATFORM_MERSIS], adres: [PLATFORM_ADRES])</p>
<p><strong>Partner / Tesis İşletmecisi:</strong> [PARTNER_UNVAN], vergi dairesi/no: [PARTNER_VERGI], adres: [PARTNER_ADRES], yetkili: [PARTNER_YETKILI]</p>
<h2>2. KONU VE KAPSAM</h2>
<p>İşbu sözleşme; Partner''ın konaklama tesisini platformda listelemesi, fiyat/oda/müsaitlik yönetimi, rezervasyonların platform üzerinden alınması ve tahsilatın TBK hükümleri ile 6502 sayılı Tüketicinin Korunması Hakkında Kanun kapsamında şeffaf şekilde yürütülmesine ilişkin hak ve yükümlülükleri düzenler.</p>
<h2>3. PLATFORM HİZMETİ VE ARACILIK</h2>
<p>Otelturizm; elektronik ortamda aracılık hizmeti sunar. Rezervasyon sözleşmesi misafir ile Partner arasında kurulur; platform rezervasyon kaydı, bildirim ve raporlama sağlar.</p>
<h2>4. KOMİSYON VE ÜCRETLENDİRME</h2>
<p>Partner; platform üzerinden gerçekleşen ve tamamlanan rezervasyonlar için panelde beyan edilen oranlarda komisyon ödemeyi kabul eder. Güncel oranlar: <strong>[KOMISYON_ORANI]</strong>. Kampanya/indirim sonrası net komisyon, rezervasyon özetinde gösterilir.</p>
<h2>5. ÖDEME, TAHSİLAT VE MUTABAKAT</h2>
<p>Tahakkuk → tahsilat → dönemsel mutabakat akışı partner paneli ve admin komisyon modülünde izlenir. İtirazlar yazılı kanaldan [MUTABAKAT_EPOSTA] adresine [ITIRAZ_SURE_GUN] iş günü içinde iletilir.</p>
<h2>6. SÜRE VE YÜRÜRLÜK</h2>
<p>Sözleşme [BASLANGIC_TARIHI] tarihinde yürürlüğe girer; süresiz olup taraflar fesih bildirimi ile sona erdirebilir.</p>
<h2>7. FESİH</h2>
<p>Taraflar [FESIH_BILDIRIM_GUN] gün önceden yazılı bildirimle feshedebilir. Ağır ihlal, sahte belge, dolandırıcılık şüphesi veya mevzuata aykırılık hâlinde platform derhal askıya alma ve fesih hakkını saklı tutar.</p>
<h2>8. KVKK VE GİZLİLİK</h2>
<p>Taraflar 6698 sayılı KVKK''ya uygun hareket eder. Partner; misafir kişisel verilerini yalnızca rezervasyon ifası için işler. Aydınlatma metni: <a href="/sozlesmeler/partner-kvkk-aydinlatma">partner KVKK</a>.</p>
<h2>9. MESAFELİ SATIŞ VE TÜKETİCİ BİLGİLENDİRMESİ</h2>
<p>Kamu tüketicisine yönelik listelemelerde Partner; mesafeli satış yükümlülükleri, iptal/iade koşulları ve ön bilgilendirmeyi doğru sunmakla yükümlüdür.</p>
<h2>10. FİKRİ MÜLKİYET VE MARKA</h2>
<p>Platform markası ve arayüzü Otelturizm''e aittir. Partner yalnızca lisanslı görselleri yükler.</p>
<h2>11. SORUMLULUK SINIRI</h2>
<p>Tarafların kusuru hariç dolaylı zararlardan sorumluluğu, zorunlu tüketici mevzuatı saklı kalmak kaydıyla sınırlıdır.</p>
<h2>12. UYUŞMAZLIK</h2>
<p>İşbu sözleşmeden doğan uyuşmazlıklarda <strong>İstanbul (Çağlayan) Mahkemeleri ve İcra Daireleri</strong> yetkilidir.</p>
<h2>13. YÜRÜRLÜK VE KABUL</h2>
<p>Partner panelinde elektronik onay ile kabul edilir. Versiyon: 1 · [YURURLUK_TARIHI]</p>',
        NULL, 1, 1, 1, 1, 1
    );
END

IF NOT EXISTS (SELECT 1 FROM [dbo].[SOZLESMELER] WHERE [SLUG] = N'partner-kvkk-aydinlatma' AND [VERSIYON_NO] = 1)
BEGIN
    INSERT INTO [dbo].[SOZLESMELER] (
        [HEDEF_KITLE], [SOZLESME_TIPI], [BASLIK], [ALT_BASLIK], [SLUG],
        [OZET_HTML], [ICERIK_HTML], [GORSEL_URL], [VERSIYON_NO],
        [KABUL_GEREKTIRIR_MI], [EPOSTA_DOGRULAMADA_GONDER], [YENILEME_GEREKIR_MI], [AKTIF_MI]
    ) VALUES (
        N'partner', N'kvkk', N'Partner KVKK Aydınlatma Metni',
        N'6698 sayılı Kanun kapsamında partner veri işleme bilgilendirmesi.',
        N'partner-kvkk-aydinlatma',
        N'<p>[PARTNER_UNVAN] adına işlenen kişisel veriler hakkında bilgilendirme.</p>',
        N'<h2>1. Veri sorumlusu</h2><p>Otelturizm ve [PARTNER_UNVAN] iş birliği kapsamında veri sorumlusu/işleyen rolleri sözleşmede tanımlanır.</p><h2>2. İşlenen veriler</h2><ul><li>Yetkili ve iletişim bilgileri</li><li>Tesis ve evrak verileri</li><li>Rezervasyon ve finans kayıtları</li></ul><h2>3. Haklar</h2><p>KVKK md. 11 başvuruları destek@otelturizm.com üzerinden yapılabilir.</p>',
        NULL, 1, 1, 1, 0, 1
    );
END

IF NOT EXISTS (SELECT 1 FROM [dbo].[SOZLESMELER] WHERE [SLUG] = N'firma-kurumsal-platform-sozlesmesi' AND [VERSIYON_NO] = 1)
BEGIN
    INSERT INTO [dbo].[SOZLESMELER] (
        [HEDEF_KITLE], [SOZLESME_TIPI], [BASLIK], [ALT_BASLIK], [SLUG],
        [OZET_HTML], [ICERIK_HTML], [GORSEL_URL], [VERSIYON_NO],
        [KABUL_GEREKTIRIR_MI], [EPOSTA_DOGRULAMADA_GONDER], [YENILEME_GEREKIR_MI], [AKTIF_MI]
    ) VALUES (
        N'company', N'agreement', N'Kurumsal Firma Platform Sözleşmesi',
        N'[FIRMA_UNVAN] kurumsal rezervasyon ve faturalama hizmetleri.',
        N'firma-kurumsal-platform-sozlesmesi',
        N'<p>Kurumsal firma hesabı kullanım koşulları ve KVKK çerçevesi.</p>',
        N'<h2>1. Taraflar</h2><p><strong>Firma:</strong> [FIRMA_UNVAN] · <strong>Platform:</strong> Otelturizm</p><h2>2. Konu</h2><p>Kurumsal rezervasyon, onay akışı ve faturalama.</p><h2>3. KVKK</h2><p>Çalışan/yolcu verileri yalnızca hizmet ifası için işlenir.</p><h2>4. Uyuşmazlık</h2><p>İstanbul mahkemeleri yetkilidir.</p>',
        NULL, 1, 1, 1, 1, 1
    );
END

IF NOT EXISTS (SELECT 1 FROM [dbo].[SOZLESMELER] WHERE [SLUG] = N'komisyon-mesafeli-satis-ek' AND [VERSIYON_NO] = 1)
BEGIN
    INSERT INTO [dbo].[SOZLESMELER] (
        [HEDEF_KITLE], [SOZLESME_TIPI], [BASLIK], [ALT_BASLIK], [SLUG],
        [OZET_HTML], [ICERIK_HTML], [GORSEL_URL], [VERSIYON_NO],
        [KABUL_GEREKTIRIR_MI], [EPOSTA_DOGRULAMADA_GONDER], [YENILEME_GEREKIR_MI], [AKTIF_MI]
    ) VALUES (
        N'partner', N'addendum', N'Komisyon ve Mesafeli Satış Ek Protokolü',
        N'Komisyon tahakkuku, iptal/iade ve mesafeli satış bilgilendirme ekleri.',
        N'komisyon-mesafeli-satis-ek',
        N'<p>Partner platform sözleşmesine ek komisyon ve tüketici bilgilendirme hükümleri.</p>',
        N'<h2>1. Komisyon tahakkuku</h2><p>Rezervasyon check-out veya no-show politikasına göre tahakkuk eder.</p><h2>2. Mesafeli satış</h2><p>6502 sayılı Kanun ve Mesafeli Sözleşmeler Yönetmeliği kapsamında ön bilgilendirme yükümlülükleri Partner''a aittir.</p><h2>3. İptal ve iade</h2><p>Koşullar rezervasyon özeti ve otel politikasında gösterilir.</p>',
        NULL, 1, 0, 0, 1, 1
    );
END
