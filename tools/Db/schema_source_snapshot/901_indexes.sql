-- Indexes generated from current local MSSQL database.

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.bildirim_loglari') AND name = N'IX_bildirim_loglari_email_queue')
BEGIN
    CREATE INDEX [IX_bildirim_loglari_email_queue] ON [dbo].[bildirim_loglari] ([tur] ASC, [durum] ASC, [olusturulma_tarihi] ASC, [id] ASC) INCLUDE ([alici_eposta], [konu], [gonderilen_icerik], [gonderme_denemesi], [maksimum_deneme], [guncellenme_tarihi]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.firma_oda_fiyat_musaitlik') AND name = N'IX_firma_oda_fiyat_musaitlik_otel_room_date')
BEGIN
    CREATE INDEX [IX_firma_oda_fiyat_musaitlik_otel_room_date] ON [dbo].[firma_oda_fiyat_musaitlik] ([otel_id] ASC, [oda_tip_id] ASC, [tarih] ASC);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.firma_oda_fiyat_musaitlik') AND name = N'IX_firma_oda_fiyat_musaitlik_read')
BEGIN
    CREATE INDEX [IX_firma_oda_fiyat_musaitlik_read] ON [dbo].[firma_oda_fiyat_musaitlik] ([aktif_mi] ASC, [kapali_satis] ASC, [otel_id] ASC, [oda_tip_id] ASC, [tarih] ASC) INCLUDE ([firma_gecelik_fiyat], [minimum_geceleme], [maksimum_geceleme], [fiyat_notu], [guncellenme_tarihi]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.firma_oda_fiyat_musaitlik') AND name = N'IX_firma_ofm_otel_room_date')
BEGIN
    CREATE INDEX [IX_firma_ofm_otel_room_date] ON [dbo].[firma_oda_fiyat_musaitlik] ([otel_id] ASC, [oda_tip_id] ASC, [tarih] ASC);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.fiyat_indirimleri') AND name = N'IX_fiyat_indirimleri_aktif')
BEGIN
    CREATE INDEX [IX_fiyat_indirimleri_aktif] ON [dbo].[fiyat_indirimleri] ([aktif_mi] ASC, [siralama] ASC, [id] ASC);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.gelistirme_talepleri') AND name = N'IX_gelistirme_talepleri_ana_talep_id')
BEGIN
    CREATE INDEX [IX_gelistirme_talepleri_ana_talep_id] ON [dbo].[gelistirme_talepleri] ([ana_talep_id] ASC, [silindi_mi] ASC, [olusturulma_tarihi] DESC);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.gelistirme_talepleri') AND name = N'IX_gelistirme_talepleri_atanan')
BEGIN
    CREATE INDEX [IX_gelistirme_talepleri_atanan] ON [dbo].[gelistirme_talepleri] ([atanan_gelistirici_id] ASC, [silindi_mi] ASC, [son_hareket_tarihi] DESC);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.gelistirme_talepleri') AND name = N'IX_gelistirme_talepleri_durum')
BEGIN
    CREATE INDEX [IX_gelistirme_talepleri_durum] ON [dbo].[gelistirme_talepleri] ([durum] ASC, [oncelik] ASC, [son_hareket_tarihi] DESC);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.gelistirme_talepleri') AND name = N'IX_gelistirme_talepleri_olusturan')
BEGIN
    CREATE INDEX [IX_gelistirme_talepleri_olusturan] ON [dbo].[gelistirme_talepleri] ([olusturan_kullanici_id] ASC, [silindi_mi] ASC, [olusturulma_tarihi] DESC);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.komisyon_vergiler') AND name = N'IX_komisyon_vergiler_otel_tarih')
BEGIN
    CREATE INDEX [IX_komisyon_vergiler_otel_tarih] ON [dbo].[komisyon_vergiler] ([otel_id] ASC, [aktif_mi] ASC, [baslangic_tarihi] ASC, [bitis_tarihi] ASC);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.kullanici_giris_2fa_tokenlari') AND name = N'IX_kullanici_giris_2fa_phone')
BEGIN
    CREATE INDEX [IX_kullanici_giris_2fa_phone] ON [dbo].[kullanici_giris_2fa_tokenlari] ([telefon_e164] ASC, [olusturulma_tarihi] DESC);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.kullanici_giris_2fa_tokenlari') AND name = N'IX_kullanici_giris_2fa_user')
BEGIN
    CREATE INDEX [IX_kullanici_giris_2fa_user] ON [dbo].[kullanici_giris_2fa_tokenlari] ([kullanici_id] ASC, [olusturulma_tarihi] DESC);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.kullanici_giris_loglari') AND name = N'IX_kullanici_giris_loglari_user')
BEGIN
    CREATE INDEX [IX_kullanici_giris_loglari_user] ON [dbo].[kullanici_giris_loglari] ([kullanici_id] ASC, [giris_tarihi] DESC);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.kullanici_konum_loglari') AND name = N'IX_kullanici_konum_loglari_session_key_kayit_tarihi')
BEGIN
    CREATE INDEX [IX_kullanici_konum_loglari_session_key_kayit_tarihi] ON [dbo].[kullanici_konum_loglari] ([session_key] ASC, [kayit_tarihi] DESC);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.kullanici_konum_loglari') AND name = N'IX_kullanici_konum_loglari_user_id_kayit_tarihi')
BEGIN
    CREATE INDEX [IX_kullanici_konum_loglari_user_id_kayit_tarihi] ON [dbo].[kullanici_konum_loglari] ([user_id] ASC, [kayit_tarihi] DESC);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.kullanici_telefon_gecmisi') AND name = N'IX_kullanici_telefon_gecmisi_old_phone')
BEGIN
    CREATE INDEX [IX_kullanici_telefon_gecmisi_old_phone] ON [dbo].[kullanici_telefon_gecmisi] ([onceki_telefon_e164] ASC);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.kullanici_telefon_gecmisi') AND name = N'IX_kullanici_telefon_gecmisi_user')
BEGIN
    CREATE INDEX [IX_kullanici_telefon_gecmisi_user] ON [dbo].[kullanici_telefon_gecmisi] ([kullanici_id] ASC, [olusturulma_tarihi] DESC);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.oda_fiyat_musaitlik') AND name = N'IX_oda_fiyat_musaitlik_discount_only')
BEGIN
    CREATE INDEX [IX_oda_fiyat_musaitlik_discount_only] ON [dbo].[oda_fiyat_musaitlik] ([otel_id] ASC, [tarih] ASC, [oda_tip_id] ASC) INCLUDE ([indirimli_fiyat]) WHERE ([indirimli_fiyat] IS NOT NULL);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.oda_fiyat_musaitlik') AND name = N'IX_oda_fiyat_musaitlik_indirim_id')
BEGIN
    CREATE INDEX [IX_oda_fiyat_musaitlik_indirim_id] ON [dbo].[oda_fiyat_musaitlik] ([indirim_id] ASC) WHERE ([indirim_id] IS NOT NULL);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.oda_fiyat_musaitlik') AND name = N'IX_oda_fiyat_musaitlik_oda_otel_tarih_include')
BEGIN
    CREATE INDEX [IX_oda_fiyat_musaitlik_oda_otel_tarih_include] ON [dbo].[oda_fiyat_musaitlik] ([oda_tip_id] ASC, [otel_id] ASC, [tarih] ASC) INCLUDE ([gecelik_fiyat], [indirimli_fiyat], [kampanya_id], [toplam_oda_sayisi], [satilan_oda_sayisi], [bloke_oda_sayisi], [minimum_geceleme], [maksimum_geceleme], [kapali_satis]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.oda_fiyat_musaitlik') AND name = N'UX_oda_fiyat_musaitlik_otel_oda_tarih')
BEGIN
    CREATE UNIQUE INDEX [UX_oda_fiyat_musaitlik_otel_oda_tarih] ON [dbo].[oda_fiyat_musaitlik] ([otel_id] ASC, [oda_tip_id] ASC, [tarih] ASC);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.oda_ozellik_iliskileri') AND name = N'IX_oda_ozellik_iliskileri_oda_kategori')
BEGIN
    CREATE INDEX [IX_oda_ozellik_iliskileri_oda_kategori] ON [dbo].[oda_ozellik_iliskileri] ([oda_id] ASC, [kategori_id] ASC, [ozellik_id] ASC);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.oda_ozellik_iliskileri') AND name = N'IX_oda_ozellik_iliskileri_otel_kategori')
BEGIN
    CREATE INDEX [IX_oda_ozellik_iliskileri_otel_kategori] ON [dbo].[oda_ozellik_iliskileri] ([otel_id] ASC, [kategori_id] ASC, [oda_id] ASC);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.oda_ozellik_kategorileri') AND name = N'UX_oda_ozellik_kategorileri_ad')
BEGIN
    CREATE UNIQUE INDEX [UX_oda_ozellik_kategorileri_ad] ON [dbo].[oda_ozellik_kategorileri] ([kategori_adi] ASC);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.oda_ozellikleri') AND name = N'IX_oda_ozellikleri_kategori_sira')
BEGIN
    CREATE INDEX [IX_oda_ozellikleri_kategori_sira] ON [dbo].[oda_ozellikleri] ([kategori_id] ASC, [aktif_mi] ASC, [siralama] ASC, [ozellik_adi] ASC);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.oda_tipi_ozellikleri') AND name = N'IX_oda_tipi_ozellikleri_otel_kategori')
BEGIN
    CREATE INDEX [IX_oda_tipi_ozellikleri_otel_kategori] ON [dbo].[oda_tipi_ozellikleri] ([otel_id] ASC, [kategori_id] ASC, [oda_tip_id] ASC, [ozellik_id] ASC);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.odeme_durumu_tanimlari') AND name = N'UX_odeme_durumu_tanimlari_ad')
BEGIN
    CREATE UNIQUE INDEX [UX_odeme_durumu_tanimlari_ad] ON [dbo].[odeme_durumu_tanimlari] ([ad] ASC);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.odeme_durumu_tanimlari') AND name = N'UX_odeme_durumu_tanimlari_kod')
BEGIN
    CREATE UNIQUE INDEX [UX_odeme_durumu_tanimlari_kod] ON [dbo].[odeme_durumu_tanimlari] ([kod] ASC);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.odeme_yontemi_tanimlari') AND name = N'UX_odeme_yontemi_tanimlari_ad')
BEGIN
    CREATE UNIQUE INDEX [UX_odeme_yontemi_tanimlari_ad] ON [dbo].[odeme_yontemi_tanimlari] ([ad] ASC);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.odeme_yontemi_tanimlari') AND name = N'UX_odeme_yontemi_tanimlari_kod')
BEGIN
    CREATE UNIQUE INDEX [UX_odeme_yontemi_tanimlari_kod] ON [dbo].[odeme_yontemi_tanimlari] ([kod] ASC);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.otel_koordinat_degisim_loglari') AND name = N'IX_otel_koordinat_degisim_loglari_admin_id_kayit_tarihi')
BEGIN
    CREATE INDEX [IX_otel_koordinat_degisim_loglari_admin_id_kayit_tarihi] ON [dbo].[otel_koordinat_degisim_loglari] ([admin_kullanici_id] ASC, [kayit_tarihi] DESC);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.otel_koordinat_degisim_loglari') AND name = N'IX_otel_koordinat_degisim_loglari_otel_id_kayit_tarihi')
BEGIN
    CREATE INDEX [IX_otel_koordinat_degisim_loglari_otel_id_kayit_tarihi] ON [dbo].[otel_koordinat_degisim_loglari] ([otel_id] ASC, [kayit_tarihi] DESC);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.otel_liste_abonelikleri') AND name = N'IX_otel_liste_abonelikleri_kapsam')
BEGIN
    CREATE INDEX [IX_otel_liste_abonelikleri_kapsam] ON [dbo].[otel_liste_abonelikleri] ([kapsam_tipi] ASC, [kapsam_degeri_normalized] ASC, [hedef_sira] ASC, [durum] ASC, [baslangic_utc] ASC, [bitis_utc] ASC) INCLUDE ([otel_id]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.otel_liste_abonelikleri') AND name = N'IX_otel_liste_abonelikleri_otel')
BEGIN
    CREATE INDEX [IX_otel_liste_abonelikleri_otel] ON [dbo].[otel_liste_abonelikleri] ([otel_id] ASC, [durum] ASC, [baslangic_utc] ASC, [bitis_utc] ASC) INCLUDE ([kapsam_tipi], [kapsam_degeri], [hedef_sira]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.otel_ozellik_iliskileri') AND name = N'IX_otel_ozellik_iliskileri_kategori')
BEGIN
    CREATE INDEX [IX_otel_ozellik_iliskileri_kategori] ON [dbo].[otel_ozellik_iliskileri] ([otel_id] ASC, [kategori_id] ASC, [ozellik_id] ASC);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.otel_tipleri') AND name = N'IX_otel_tipleri_aktif_siralama')
BEGIN
    CREATE INDEX [IX_otel_tipleri_aktif_siralama] ON [dbo].[otel_tipleri] ([aktif_mi] ASC, [siralama] ASC, [tip_adi] ASC);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.otel_tipleri') AND name = N'UX_otel_tipleri_kod')
BEGIN
    CREATE UNIQUE INDEX [UX_otel_tipleri_kod] ON [dbo].[otel_tipleri] ([kod] ASC);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.oteller') AND name = N'IX_oteller_name_norm')
BEGIN
    CREATE INDEX [IX_oteller_name_norm] ON [dbo].[oteller] ([yayin_durumu] ASC, [onay_durumu] ASC, [otel_adi_normalized] ASC) INCLUDE ([sehir_normalized], [ilce_normalized], [mahalle_normalized], [populerlik_sirasi], [ortalama_puan], [toplam_yorum_sayisi]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.oteller') AND name = N'IX_oteller_otel_tipi_id')
BEGIN
    CREATE INDEX [IX_oteller_otel_tipi_id] ON [dbo].[oteller] ([otel_tipi_id] ASC);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.oteller') AND name = N'IX_oteller_search_norm')
BEGIN
    CREATE INDEX [IX_oteller_search_norm] ON [dbo].[oteller] ([yayin_durumu] ASC, [onay_durumu] ASC, [sehir_normalized] ASC, [ilce_normalized] ASC, [mahalle_normalized] ASC) INCLUDE ([otel_adi], [otel_adi_normalized], [konum_normalized], [kapak_fotografi], [yildiz_sayisi], [ortalama_puan], [toplam_yorum_sayisi], [populerlik_sirasi], [enlem], [boylam], [one_cikan_otel], [tavsiye_edilen_otel]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.oteller') AND name = N'IX_oteller_yayin_onay_mahalle')
BEGIN
    CREATE INDEX [IX_oteller_yayin_onay_mahalle] ON [dbo].[oteller] ([yayin_durumu] ASC, [onay_durumu] ASC, [mahalle] ASC) INCLUDE ([sehir], [ilce], [otel_adi], [populerlik_sirasi], [ortalama_puan], [toplam_yorum_sayisi]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.oteller') AND name = N'IX_oteller_yayin_onay_oteladi')
BEGIN
    CREATE INDEX [IX_oteller_yayin_onay_oteladi] ON [dbo].[oteller] ([yayin_durumu] ASC, [onay_durumu] ASC, [otel_adi] ASC) INCLUDE ([sehir], [ilce], [mahalle], [populerlik_sirasi], [ortalama_puan], [toplam_yorum_sayisi]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.oteller') AND name = N'IX_oteller_yayin_onay_sehir_ilce')
BEGIN
    CREATE INDEX [IX_oteller_yayin_onay_sehir_ilce] ON [dbo].[oteller] ([yayin_durumu] ASC, [onay_durumu] ASC, [sehir] ASC, [ilce] ASC) INCLUDE ([mahalle], [otel_adi], [kapak_fotografi], [yildiz_sayisi], [ortalama_puan], [toplam_yorum_sayisi], [populerlik_sirasi], [enlem], [boylam], [one_cikan_otel], [tavsiye_edilen_otel]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.partner_detaylari') AND name = N'IX_partner_detaylari_otel_tipi_id')
BEGIN
    CREATE INDEX [IX_partner_detaylari_otel_tipi_id] ON [dbo].[partner_detaylari] ([otel_tipi_id] ASC);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.platform_email_hesaplari') AND name = N'UX_platform_email_hesaplari_email_adresi')
BEGIN
    CREATE UNIQUE INDEX [UX_platform_email_hesaplari_email_adresi] ON [dbo].[platform_email_hesaplari] ([email_adresi] ASC);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.platform_email_hesaplari') AND name = N'UX_platform_email_hesaplari_hesap_kodu')
BEGIN
    CREATE UNIQUE INDEX [UX_platform_email_hesaplari_hesap_kodu] ON [dbo].[platform_email_hesaplari] ([hesap_kodu] ASC);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.platform_email_mesajlari') AND name = N'IX_platform_email_mesajlari_hesap_tarih')
BEGIN
    CREATE INDEX [IX_platform_email_mesajlari_hesap_tarih] ON [dbo].[platform_email_mesajlari] ([hesap_id] ASC, [tarih_utc] DESC);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.platform_email_mesajlari') AND name = N'UX_platform_email_mesajlari_hesap_uid')
BEGIN
    CREATE UNIQUE INDEX [UX_platform_email_mesajlari_hesap_uid] ON [dbo].[platform_email_mesajlari] ([hesap_id] ASC, [yon] ASC, [klasor] ASC, [uid_degeri] ASC) WHERE ([uid_degeri] IS NOT NULL);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.rezervasyon_durum_tanimlari') AND name = N'UX_rezervasyon_durum_tanimlari_ad')
BEGIN
    CREATE UNIQUE INDEX [UX_rezervasyon_durum_tanimlari_ad] ON [dbo].[rezervasyon_durum_tanimlari] ([ad] ASC);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.rezervasyon_durum_tanimlari') AND name = N'UX_rezervasyon_durum_tanimlari_kod')
BEGIN
    CREATE UNIQUE INDEX [UX_rezervasyon_durum_tanimlari_kod] ON [dbo].[rezervasyon_durum_tanimlari] ([kod] ASC);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.rezervasyon_odeme_kalemleri') AND name = N'IX_rezervasyon_odeme_kalemleri_rez')
BEGIN
    CREATE INDEX [IX_rezervasyon_odeme_kalemleri_rez] ON [dbo].[rezervasyon_odeme_kalemleri] ([rezervasyon_id] ASC);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.rezervasyonlar') AND name = N'IX_rezervasyonlar_odeme_durumu_id')
BEGIN
    CREATE INDEX [IX_rezervasyonlar_odeme_durumu_id] ON [dbo].[rezervasyonlar] ([odeme_durumu_id] ASC);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.rezervasyonlar') AND name = N'IX_rezervasyonlar_rezervasyon_durumu_id')
BEGIN
    CREATE INDEX [IX_rezervasyonlar_rezervasyon_durumu_id] ON [dbo].[rezervasyonlar] ([rezervasyon_durumu_id] ASC);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.sozlesme_dosyalari') AND name = N'IX_sozlesme_dosyalari_sozlesme_id')
BEGIN
    CREATE INDEX [IX_sozlesme_dosyalari_sozlesme_id] ON [dbo].[sozlesme_dosyalari] ([sozlesme_id] ASC);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.sozlesme_dosyalari') AND name = N'IX_sozlesme_dosyalari_tipi')
BEGIN
    CREATE INDEX [IX_sozlesme_dosyalari_tipi] ON [dbo].[sozlesme_dosyalari] ([dosya_tipi] ASC);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.sozlesme_gonderim_loglari') AND name = N'IX_sozlesme_gonderim_loglari_eposta')
BEGIN
    CREATE INDEX [IX_sozlesme_gonderim_loglari_eposta] ON [dbo].[sozlesme_gonderim_loglari] ([alici_eposta] ASC, [gonderim_tarihi] DESC);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.sozlesme_kabulleri') AND name = N'IX_sozlesme_kabulleri_firma')
BEGIN
    CREATE INDEX [IX_sozlesme_kabulleri_firma] ON [dbo].[sozlesme_kabulleri] ([firma_id] ASC);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.sozlesme_kabulleri') AND name = N'IX_sozlesme_kabulleri_partner')
BEGIN
    CREATE INDEX [IX_sozlesme_kabulleri_partner] ON [dbo].[sozlesme_kabulleri] ([partner_id] ASC);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.sozlesme_kabulleri') AND name = N'IX_sozlesme_kabulleri_user')
BEGIN
    CREATE INDEX [IX_sozlesme_kabulleri_user] ON [dbo].[sozlesme_kabulleri] ([kullanici_id] ASC, [sozlesme_id] ASC);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.sozlesmeler') AND name = N'IX_sozlesmeler_hedef')
BEGIN
    CREATE INDEX [IX_sozlesmeler_hedef] ON [dbo].[sozlesmeler] ([hedef_kitle] ASC, [sozlesme_tipi] ASC, [aktif_mi] ASC);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.sozlesmeler') AND name = N'UX_sozlesmeler_slug_versiyon')
BEGIN
    CREATE UNIQUE INDEX [UX_sozlesmeler_slug_versiyon] ON [dbo].[sozlesmeler] ([slug] ASC, [versiyon_no] ASC);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.telefon_dogrulama_tokenlari') AND name = N'IX_telefon_dogrulama_tokenlari_message')
BEGIN
    CREATE INDEX [IX_telefon_dogrulama_tokenlari_message] ON [dbo].[telefon_dogrulama_tokenlari] ([meta_mesaj_id] ASC);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.telefon_dogrulama_tokenlari') AND name = N'IX_telefon_dogrulama_tokenlari_user')
BEGIN
    CREATE INDEX [IX_telefon_dogrulama_tokenlari_user] ON [dbo].[telefon_dogrulama_tokenlari] ([kullanici_id] ASC, [olusturulma_tarihi] DESC);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.users') AND name = N'IX_users_profil_resim_url')
BEGIN
    CREATE INDEX [IX_users_profil_resim_url] ON [dbo].[users] ([profil_resim_url] ASC);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.users') AND name = N'IX_users_telefon_dogrulama_durumu')
BEGIN
    CREATE INDEX [IX_users_telefon_dogrulama_durumu] ON [dbo].[users] ([telefon_dogrulama_durumu] ASC, [telefon_dogrulama_tarihi] ASC);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.users') AND name = N'IX_users_telefon_e164')
BEGIN
    CREATE INDEX [IX_users_telefon_e164] ON [dbo].[users] ([telefon_e164] ASC);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.users') AND name = N'UX_users_eposta')
BEGIN
    CREATE UNIQUE INDEX [UX_users_eposta] ON [dbo].[users] ([eposta] ASC) WHERE ([eposta] IS NOT NULL AND [eposta]<>N'');
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.whatsapp_mesaj_loglari') AND name = N'IX_whatsapp_mesaj_loglari_meta')
BEGIN
    CREATE INDEX [IX_whatsapp_mesaj_loglari_meta] ON [dbo].[whatsapp_mesaj_loglari] ([meta_mesaj_id] ASC);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.whatsapp_mesaj_loglari') AND name = N'IX_whatsapp_mesaj_loglari_phone')
BEGIN
    CREATE INDEX [IX_whatsapp_mesaj_loglari_phone] ON [dbo].[whatsapp_mesaj_loglari] ([telefon_e164] ASC, [olusturulma_tarihi] DESC);
END
GO

