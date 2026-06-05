-- UTF-8 BOM recommended
-- Firma sözleşme slug'ları: UI rotaları ile eşleme (idempotent)

IF NOT EXISTS (SELECT 1 FROM [dbo].[SOZLESMELER] WHERE [SLUG] = N'firma-kurumsal-kullanim-kosullari' AND [VERSIYON_NO] = 1)
BEGIN
    INSERT INTO [dbo].[SOZLESMELER] (
        [HEDEF_KITLE], [SOZLESME_TIPI], [BASLIK], [ALT_BASLIK], [SLUG],
        [OZET_HTML], [ICERIK_HTML], [GORSEL_URL], [VERSIYON_NO],
        [KABUL_GEREKTIRIR_MI], [EPOSTA_DOGRULAMADA_GONDER], [YENILEME_GEREKIR_MI], [AKTIF_MI]
    )
    SELECT TOP (1)
        N'company', N'agreement', N'Firma Kurumsal Kullanım Koşulları',
        N'Kurumsal firma hesabı kullanım koşulları ve platform kuralları.',
        N'firma-kurumsal-kullanim-kosullari',
        [OZET_HTML], [ICERIK_HTML], [GORSEL_URL], 1,
        1, 1, 1, 1
    FROM [dbo].[SOZLESMELER]
    WHERE [SLUG] = N'firma-kurumsal-platform-sozlesmesi' AND [VERSIYON_NO] = 1;
END

IF NOT EXISTS (SELECT 1 FROM [dbo].[SOZLESMELER] WHERE [SLUG] = N'firma-kvkk-aydinlatma' AND [VERSIYON_NO] = 1)
BEGIN
    INSERT INTO [dbo].[SOZLESMELER] (
        [HEDEF_KITLE], [SOZLESME_TIPI], [BASLIK], [ALT_BASLIK], [SLUG],
        [OZET_HTML], [ICERIK_HTML], [GORSEL_URL], [VERSIYON_NO],
        [KABUL_GEREKTIRIR_MI], [EPOSTA_DOGRULAMADA_GONDER], [YENILEME_GEREKIR_MI], [AKTIF_MI]
    ) VALUES (
        N'company', N'kvkk', N'Firma KVKK Aydınlatma Metni',
        N'6698 sayılı Kanun kapsamında kurumsal firma veri işleme bilgilendirmesi.',
        N'firma-kvkk-aydinlatma',
        N'<p>[FIRMA_UNVAN] adına işlenen kişisel veriler hakkında bilgilendirme.</p>',
        N'<h2>1. Veri sorumlusu</h2><p>Otelturizm ve [FIRMA_UNVAN] iş birliği kapsamında veri sorumlusu/işleyen rolleri sözleşmede tanımlanır.</p><h2>2. İşlenen veriler</h2><ul><li>Yetkili ve çalışan iletişim bilgileri</li><li>Rezervasyon ve fatura kayıtları</li><li>Kurumsal sözleşme ve başvuru evrakları</li></ul><h2>3. Haklar</h2><p>KVKK md. 11 başvuruları destek@otelturizm.com üzerinden yapılabilir.</p>',
        NULL, 1, 1, 1, 0, 1
    );
END
