-- Indexes
-- Indexes generated from current local MSSQL database.

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.BILDIRIM_LOGLARI') AND name = N'IX_bildirim_loglari_email_queue')
BEGIN
    CREATE INDEX [IX_bildirim_loglari_email_queue] ON [dbo].[BILDIRIM_LOGLARI] ([TUR] ASC, [DURUM] ASC, [OLUSTURULMA_TARIHI] ASC, [ID] ASC) INCLUDE ([ALICI_EPOSTA], [KONU], [GONDERILEN_ICERIK], [GONDERME_DENEMESI], [MAKSIMUM_DENEME], [GUNCELLENME_TARIHI]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.FIRMA_ODA_FIYAT_MUSAITLIK') AND name = N'IX_firma_oda_fiyat_musaitlik_otel_room_date')
BEGIN
    CREATE INDEX [IX_firma_oda_fiyat_musaitlik_otel_room_date] ON [dbo].[FIRMA_ODA_FIYAT_MUSAITLIK] ([OTEL_ID] ASC, [ODA_TIP_ID] ASC, [TARIH] ASC);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.FIRMA_ODA_FIYAT_MUSAITLIK') AND name = N'IX_firma_oda_fiyat_musaitlik_read')
BEGIN
    CREATE INDEX [IX_firma_oda_fiyat_musaitlik_read] ON [dbo].[FIRMA_ODA_FIYAT_MUSAITLIK] ([AKTIF_MI] ASC, [KAPALI_SATIS] ASC, [OTEL_ID] ASC, [ODA_TIP_ID] ASC, [TARIH] ASC) INCLUDE ([FIRMA_GECELIK_FIYAT], [MINIMUM_GECELEME], [MAKSIMUM_GECELEME], [FIYAT_NOTU], [GUNCELLENME_TARIHI]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.FIRMA_ODA_FIYAT_MUSAITLIK') AND name = N'IX_firma_ofm_otel_room_date')
BEGIN
    CREATE INDEX [IX_firma_ofm_otel_room_date] ON [dbo].[FIRMA_ODA_FIYAT_MUSAITLIK] ([OTEL_ID] ASC, [ODA_TIP_ID] ASC, [TARIH] ASC);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.FIYAT_INDIRIMLERI') AND name = N'IX_fiyat_indirimleri_aktif')
BEGIN
    CREATE INDEX [IX_fiyat_indirimleri_aktif] ON [dbo].[FIYAT_INDIRIMLERI] ([AKTIF_MI] ASC, [SIRALAMA] ASC, [ID] ASC);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.GELISTIRME_TALEPLERI') AND name = N'IX_gelistirme_talepleri_ana_talep_id')
BEGIN
    CREATE INDEX [IX_gelistirme_talepleri_ana_talep_id] ON [dbo].[GELISTIRME_TALEPLERI] ([ANA_TALEP_ID] ASC, [SILINDI_MI] ASC, [OLUSTURULMA_TARIHI] DESC);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.GELISTIRME_TALEPLERI') AND name = N'IX_gelistirme_talepleri_atanan')
BEGIN
    CREATE INDEX [IX_gelistirme_talepleri_atanan] ON [dbo].[GELISTIRME_TALEPLERI] ([ATANAN_GELISTIRICI_ID] ASC, [SILINDI_MI] ASC, [SON_HAREKET_TARIHI] DESC);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.GELISTIRME_TALEPLERI') AND name = N'IX_gelistirme_talepleri_durum')
BEGIN
    CREATE INDEX [IX_gelistirme_talepleri_durum] ON [dbo].[GELISTIRME_TALEPLERI] ([DURUM] ASC, [ONCELIK] ASC, [SON_HAREKET_TARIHI] DESC);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.GELISTIRME_TALEPLERI') AND name = N'IX_gelistirme_talepleri_olusturan')
BEGIN
    CREATE INDEX [IX_gelistirme_talepleri_olusturan] ON [dbo].[GELISTIRME_TALEPLERI] ([OLUSTURAN_KULLANICI_ID] ASC, [SILINDI_MI] ASC, [OLUSTURULMA_TARIHI] DESC);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.KOMISYON_VERGILER') AND name = N'IX_komisyon_vergiler_otel_tarih')
BEGIN
    CREATE INDEX [IX_komisyon_vergiler_otel_tarih] ON [dbo].[KOMISYON_VERGILER] ([OTEL_ID] ASC, [AKTIF_MI] ASC, [BASLANGIC_TARIHI] ASC, [BITIS_TARIHI] ASC);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.KULLANICI_GIRIS_2FA_TOKENLARI') AND name = N'IX_kullanici_giris_2fa_phone')
BEGIN
    CREATE INDEX [IX_kullanici_giris_2fa_phone] ON [dbo].[KULLANICI_GIRIS_2FA_TOKENLARI] ([TELEFON_E164] ASC, [OLUSTURULMA_TARIHI] DESC);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.KULLANICI_GIRIS_2FA_TOKENLARI') AND name = N'IX_kullanici_giris_2fa_user')
BEGIN
    CREATE INDEX [IX_kullanici_giris_2fa_user] ON [dbo].[KULLANICI_GIRIS_2FA_TOKENLARI] ([KULLANICI_ID] ASC, [OLUSTURULMA_TARIHI] DESC);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.KULLANICI_GIRIS_LOGLARI') AND name = N'IX_kullanici_giris_loglari_user')
BEGIN
    CREATE INDEX [IX_kullanici_giris_loglari_user] ON [dbo].[KULLANICI_GIRIS_LOGLARI] ([KULLANICI_ID] ASC, [GIRIS_TARIHI] DESC);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.KULLANICI_KONUM_LOGLARI') AND name = N'IX_kullanici_konum_loglari_session_key_kayit_tarihi')
BEGIN
    CREATE INDEX [IX_kullanici_konum_loglari_session_key_kayit_tarihi] ON [dbo].[KULLANICI_KONUM_LOGLARI] ([SESSION_KEY] ASC, [KAYIT_TARIHI] DESC);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.KULLANICI_KONUM_LOGLARI') AND name = N'IX_kullanici_konum_loglari_user_id_kayit_tarihi')
BEGIN
    CREATE INDEX [IX_kullanici_konum_loglari_user_id_kayit_tarihi] ON [dbo].[KULLANICI_KONUM_LOGLARI] ([KULLANICI_ID] ASC, [KAYIT_TARIHI] DESC);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.KULLANICI_TELEFON_GECMISI') AND name = N'IX_kullanici_telefon_gecmisi_old_phone')
BEGIN
    CREATE INDEX [IX_kullanici_telefon_gecmisi_old_phone] ON [dbo].[KULLANICI_TELEFON_GECMISI] ([ONCEKI_TELEFON_E164] ASC);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.KULLANICI_TELEFON_GECMISI') AND name = N'IX_kullanici_telefon_gecmisi_user')
BEGIN
    CREATE INDEX [IX_kullanici_telefon_gecmisi_user] ON [dbo].[KULLANICI_TELEFON_GECMISI] ([KULLANICI_ID] ASC, [OLUSTURULMA_TARIHI] DESC);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.ODA_FIYAT_MUSAITLIK') AND name = N'IX_oda_fiyat_musaitlik_discount_only')
BEGIN
    CREATE INDEX [IX_oda_fiyat_musaitlik_discount_only] ON [dbo].[ODA_FIYAT_MUSAITLIK] ([OTEL_ID] ASC, [TARIH] ASC, [ODA_TIP_ID] ASC) INCLUDE ([INDIRIMLI_FIYAT]) WHERE ([INDIRIMLI_FIYAT] IS NOT NULL);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.ODA_FIYAT_MUSAITLIK') AND name = N'IX_oda_fiyat_musaitlik_indirim_id')
BEGIN
    CREATE INDEX [IX_oda_fiyat_musaitlik_indirim_id] ON [dbo].[ODA_FIYAT_MUSAITLIK] ([INDIRIM_ID] ASC) WHERE ([INDIRIM_ID] IS NOT NULL);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.ODA_FIYAT_MUSAITLIK') AND name = N'IX_oda_fiyat_musaitlik_oda_otel_tarih_include')
BEGIN
    CREATE INDEX [IX_oda_fiyat_musaitlik_oda_otel_tarih_include] ON [dbo].[ODA_FIYAT_MUSAITLIK] ([ODA_TIP_ID] ASC, [OTEL_ID] ASC, [TARIH] ASC) INCLUDE ([GECELIK_FIYAT], [INDIRIMLI_FIYAT], [KAMPANYA_ID], [TOPLAM_ODA_SAYISI], [SATILAN_ODA_SAYISI], [BLOKE_ODA_SAYISI], [MINIMUM_GECELEME], [MAKSIMUM_GECELEME], [KAPALI_SATIS]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.ODA_FIYAT_MUSAITLIK') AND name = N'UX_oda_fiyat_musaitlik_otel_oda_tarih')
BEGIN
    CREATE UNIQUE INDEX [UX_oda_fiyat_musaitlik_otel_oda_tarih] ON [dbo].[ODA_FIYAT_MUSAITLIK] ([OTEL_ID] ASC, [ODA_TIP_ID] ASC, [TARIH] ASC);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.ODA_OZELLIK_ILISKILERI') AND name = N'IX_oda_ozellik_iliskileri_oda_kategori')
BEGIN
    CREATE INDEX [IX_oda_ozellik_iliskileri_oda_kategori] ON [dbo].[ODA_OZELLIK_ILISKILERI] ([ODA_ID] ASC, [KATEGORI_ID] ASC, [OZELLIK_ID] ASC);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.ODA_OZELLIK_ILISKILERI') AND name = N'IX_oda_ozellik_iliskileri_otel_kategori')
BEGIN
    CREATE INDEX [IX_oda_ozellik_iliskileri_otel_kategori] ON [dbo].[ODA_OZELLIK_ILISKILERI] ([OTEL_ID] ASC, [KATEGORI_ID] ASC, [ODA_ID] ASC);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.ODA_OZELLIK_KATEGORILERI') AND name = N'UX_oda_ozellik_kategorileri_ad')
BEGIN
    CREATE UNIQUE INDEX [UX_oda_ozellik_kategorileri_ad] ON [dbo].[ODA_OZELLIK_KATEGORILERI] ([KATEGORI_ADI] ASC);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.ODA_OZELLIKLERI') AND name = N'IX_oda_ozellikleri_kategori_sira')
BEGIN
    CREATE INDEX [IX_oda_ozellikleri_kategori_sira] ON [dbo].[ODA_OZELLIKLERI] ([KATEGORI_ID] ASC, [AKTIF_MI] ASC, [SIRALAMA] ASC, [OZELLIK_ADI] ASC);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.ODA_TIPI_OZELLIKLERI') AND name = N'IX_oda_tipi_ozellikleri_otel_kategori')
BEGIN
    CREATE INDEX [IX_oda_tipi_ozellikleri_otel_kategori] ON [dbo].[ODA_TIPI_OZELLIKLERI] ([OTEL_ID] ASC, [KATEGORI_ID] ASC, [ODA_TIP_ID] ASC, [OZELLIK_ID] ASC);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.ODEME_DURUMU_TANIMLARI') AND name = N'UX_odeme_durumu_tanimlari_ad')
BEGIN
    CREATE UNIQUE INDEX [UX_odeme_durumu_tanimlari_ad] ON [dbo].[ODEME_DURUMU_TANIMLARI] ([AD] ASC);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.ODEME_DURUMU_TANIMLARI') AND name = N'UX_odeme_durumu_tanimlari_kod')
BEGIN
    CREATE UNIQUE INDEX [UX_odeme_durumu_tanimlari_kod] ON [dbo].[ODEME_DURUMU_TANIMLARI] ([KOD] ASC);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.ODEME_YONTEMI_TANIMLARI') AND name = N'UX_odeme_yontemi_tanimlari_ad')
BEGIN
    CREATE UNIQUE INDEX [UX_odeme_yontemi_tanimlari_ad] ON [dbo].[ODEME_YONTEMI_TANIMLARI] ([AD] ASC);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.ODEME_YONTEMI_TANIMLARI') AND name = N'UX_odeme_yontemi_tanimlari_kod')
BEGIN
    CREATE UNIQUE INDEX [UX_odeme_yontemi_tanimlari_kod] ON [dbo].[ODEME_YONTEMI_TANIMLARI] ([KOD] ASC);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.OTEL_KOORDINAT_DEGISIM_LOGLARI') AND name = N'IX_otel_koordinat_degisim_loglari_admin_id_kayit_tarihi')
BEGIN
    CREATE INDEX [IX_otel_koordinat_degisim_loglari_admin_id_kayit_tarihi] ON [dbo].[OTEL_KOORDINAT_DEGISIM_LOGLARI] ([ADMIN_KULLANICI_ID] ASC, [KAYIT_TARIHI] DESC);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.OTEL_KOORDINAT_DEGISIM_LOGLARI') AND name = N'IX_otel_koordinat_degisim_loglari_otel_id_kayit_tarihi')
BEGIN
    CREATE INDEX [IX_otel_koordinat_degisim_loglari_otel_id_kayit_tarihi] ON [dbo].[OTEL_KOORDINAT_DEGISIM_LOGLARI] ([OTEL_ID] ASC, [KAYIT_TARIHI] DESC);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.OTEL_LISTE_ABONELIKLERI') AND name = N'IX_otel_liste_abonelikleri_kapsam')
BEGIN
    CREATE INDEX [IX_otel_liste_abonelikleri_kapsam] ON [dbo].[OTEL_LISTE_ABONELIKLERI] ([KAPSAM_TIPI] ASC, [KAPSAM_DEGERI_NORMALIZE] ASC, [HEDEF_SIRA] ASC, [DURUM] ASC, [BASLANGIC_UTC] ASC, [BITIS_UTC] ASC) INCLUDE ([OTEL_ID]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.OTEL_LISTE_ABONELIKLERI') AND name = N'IX_otel_liste_abonelikleri_otel')
BEGIN
    CREATE INDEX [IX_otel_liste_abonelikleri_otel] ON [dbo].[OTEL_LISTE_ABONELIKLERI] ([OTEL_ID] ASC, [DURUM] ASC, [BASLANGIC_UTC] ASC, [BITIS_UTC] ASC) INCLUDE ([KAPSAM_TIPI], [KAPSAM_DEGERI], [HEDEF_SIRA]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.OTEL_OZELLIK_ILISKILERI') AND name = N'IX_otel_ozellik_iliskileri_kategori')
BEGIN
    CREATE INDEX [IX_otel_ozellik_iliskileri_kategori] ON [dbo].[OTEL_OZELLIK_ILISKILERI] ([OTEL_ID] ASC, [KATEGORI_ID] ASC, [OZELLIK_ID] ASC);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.OTEL_TIPLERI') AND name = N'IX_otel_tipleri_aktif_siralama')
BEGIN
    CREATE INDEX [IX_otel_tipleri_aktif_siralama] ON [dbo].[OTEL_TIPLERI] ([AKTIF_MI] ASC, [SIRALAMA] ASC, [TIP_ADI] ASC);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.OTEL_TIPLERI') AND name = N'UX_otel_tipleri_kod')
BEGIN
    CREATE UNIQUE INDEX [UX_otel_tipleri_kod] ON [dbo].[OTEL_TIPLERI] ([KOD] ASC);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.OTELLER') AND name = N'IX_oteller_name_norm')
BEGIN
    CREATE INDEX [IX_oteller_name_norm] ON [dbo].[OTELLER] ([YAYIN_DURUMU] ASC, [ONAY_DURUMU] ASC, [otel_adi_normalized] ASC) INCLUDE ([sehir_normalized], [ilce_normalized], [mahalle_normalized], [POPULERLIK_SIRASI], [ORTALAMA_PUAN], [TOPLAM_YORUM_SAYISI]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.OTELLER') AND name = N'IX_oteller_otel_tipi_id')
BEGIN
    CREATE INDEX [IX_oteller_otel_tipi_id] ON [dbo].[OTELLER] ([OTEL_TIPI_ID] ASC);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.OTELLER') AND name = N'IX_oteller_search_norm')
BEGIN
    CREATE INDEX [IX_oteller_search_norm] ON [dbo].[OTELLER] ([YAYIN_DURUMU] ASC, [ONAY_DURUMU] ASC, [sehir_normalized] ASC, [ilce_normalized] ASC, [mahalle_normalized] ASC) INCLUDE ([OTEL_ADI], [otel_adi_normalized], [konum_normalized], [KAPAK_FOTOGRAFI], [YILDIZ_SAYISI], [ORTALAMA_PUAN], [TOPLAM_YORUM_SAYISI], [POPULERLIK_SIRASI], [ENLEM], [BOYLAM], [ONE_CIKAN_OTEL], [TAVSIYE_EDILEN_OTEL]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.OTELLER') AND name = N'IX_oteller_yayin_onay_mahalle')
BEGIN
    CREATE INDEX [IX_oteller_yayin_onay_mahalle] ON [dbo].[OTELLER] ([YAYIN_DURUMU] ASC, [ONAY_DURUMU] ASC, [MAHALLE] ASC) INCLUDE ([SEHIR], [ILCE], [OTEL_ADI], [POPULERLIK_SIRASI], [ORTALAMA_PUAN], [TOPLAM_YORUM_SAYISI]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.OTELLER') AND name = N'IX_oteller_yayin_onay_oteladi')
BEGIN
    CREATE INDEX [IX_oteller_yayin_onay_oteladi] ON [dbo].[OTELLER] ([YAYIN_DURUMU] ASC, [ONAY_DURUMU] ASC, [OTEL_ADI] ASC) INCLUDE ([SEHIR], [ILCE], [MAHALLE], [POPULERLIK_SIRASI], [ORTALAMA_PUAN], [TOPLAM_YORUM_SAYISI]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.OTELLER') AND name = N'IX_oteller_yayin_onay_sehir_ilce')
BEGIN
    CREATE INDEX [IX_oteller_yayin_onay_sehir_ilce] ON [dbo].[OTELLER] ([YAYIN_DURUMU] ASC, [ONAY_DURUMU] ASC, [SEHIR] ASC, [ILCE] ASC) INCLUDE ([MAHALLE], [OTEL_ADI], [KAPAK_FOTOGRAFI], [YILDIZ_SAYISI], [ORTALAMA_PUAN], [TOPLAM_YORUM_SAYISI], [POPULERLIK_SIRASI], [ENLEM], [BOYLAM], [ONE_CIKAN_OTEL], [TAVSIYE_EDILEN_OTEL]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.PARTNER_DETAYLARI') AND name = N'IX_partner_detaylari_otel_tipi_id')
BEGIN
    CREATE INDEX [IX_partner_detaylari_otel_tipi_id] ON [dbo].[PARTNER_DETAYLARI] ([OTEL_TIPI_ID] ASC);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.PLATFORM_EPOSTA_HESAPLARI') AND name = N'UX_platform_email_hesaplari_email_adresi')
BEGIN
    CREATE UNIQUE INDEX [UX_platform_email_hesaplari_email_adresi] ON [dbo].[PLATFORM_EPOSTA_HESAPLARI] ([EPOSTA_ADRESI] ASC);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.PLATFORM_EPOSTA_HESAPLARI') AND name = N'UX_platform_email_hesaplari_hesap_kodu')
BEGIN
    CREATE UNIQUE INDEX [UX_platform_email_hesaplari_hesap_kodu] ON [dbo].[PLATFORM_EPOSTA_HESAPLARI] ([HESAP_KODU] ASC);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.PLATFORM_EPOSTA_MESAJLARI') AND name = N'IX_platform_email_mesajlari_hesap_tarih')
BEGIN
    CREATE INDEX [IX_platform_email_mesajlari_hesap_tarih] ON [dbo].[PLATFORM_EPOSTA_MESAJLARI] ([HESAP_ID] ASC, [TARIH_UTC] DESC);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.PLATFORM_EPOSTA_MESAJLARI') AND name = N'UX_platform_email_mesajlari_hesap_uid')
BEGIN
    CREATE UNIQUE INDEX [UX_platform_email_mesajlari_hesap_uid] ON [dbo].[PLATFORM_EPOSTA_MESAJLARI] ([HESAP_ID] ASC, [YON] ASC, [KLASOR] ASC, [UID_DEGERI] ASC) WHERE ([UID_DEGERI] IS NOT NULL);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.REZERVASYON_DURUM_TANIMLARI') AND name = N'UX_rezervasyon_durum_tanimlari_ad')
BEGIN
    CREATE UNIQUE INDEX [UX_rezervasyon_durum_tanimlari_ad] ON [dbo].[REZERVASYON_DURUM_TANIMLARI] ([AD] ASC);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.REZERVASYON_DURUM_TANIMLARI') AND name = N'UX_rezervasyon_durum_tanimlari_kod')
BEGIN
    CREATE UNIQUE INDEX [UX_rezervasyon_durum_tanimlari_kod] ON [dbo].[REZERVASYON_DURUM_TANIMLARI] ([KOD] ASC);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.REZERVASYON_ODEME_KALEMLERI') AND name = N'IX_rezervasyon_odeme_kalemleri_rez')
BEGIN
    CREATE INDEX [IX_rezervasyon_odeme_kalemleri_rez] ON [dbo].[REZERVASYON_ODEME_KALEMLERI] ([REZERVASYON_ID] ASC);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.REZERVASYONLAR') AND name = N'IX_rezervasyonlar_odeme_durumu_id')
BEGIN
    CREATE INDEX [IX_rezervasyonlar_odeme_durumu_id] ON [dbo].[REZERVASYONLAR] ([ODEME_DURUMU_ID] ASC);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.REZERVASYONLAR') AND name = N'IX_rezervasyonlar_rezervasyon_durumu_id')
BEGIN
    CREATE INDEX [IX_rezervasyonlar_rezervasyon_durumu_id] ON [dbo].[REZERVASYONLAR] ([REZERVASYON_DURUMU_ID] ASC);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.SOZLESME_DOSYALARI') AND name = N'IX_sozlesme_dosyalari_sozlesme_id')
BEGIN
    CREATE INDEX [IX_sozlesme_dosyalari_sozlesme_id] ON [dbo].[SOZLESME_DOSYALARI] ([SOZLESME_ID] ASC);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.SOZLESME_DOSYALARI') AND name = N'IX_sozlesme_dosyalari_tipi')
BEGIN
    CREATE INDEX [IX_sozlesme_dosyalari_tipi] ON [dbo].[SOZLESME_DOSYALARI] ([DOSYA_TIPI] ASC);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.SOZLESME_GONDERIM_LOGLARI') AND name = N'IX_sozlesme_gonderim_loglari_eposta')
BEGIN
    CREATE INDEX [IX_sozlesme_gonderim_loglari_eposta] ON [dbo].[SOZLESME_GONDERIM_LOGLARI] ([ALICI_EPOSTA] ASC, [GONDERIM_TARIHI] DESC);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.SOZLESME_KABULLERI') AND name = N'IX_sozlesme_kabulleri_firma')
BEGIN
    CREATE INDEX [IX_sozlesme_kabulleri_firma] ON [dbo].[SOZLESME_KABULLERI] ([FIRMA_ID] ASC);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.SOZLESME_KABULLERI') AND name = N'IX_sozlesme_kabulleri_partner')
BEGIN
    CREATE INDEX [IX_sozlesme_kabulleri_partner] ON [dbo].[SOZLESME_KABULLERI] ([PARTNER_ID] ASC);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.SOZLESME_KABULLERI') AND name = N'IX_sozlesme_kabulleri_user')
BEGIN
    CREATE INDEX [IX_sozlesme_kabulleri_user] ON [dbo].[SOZLESME_KABULLERI] ([KULLANICI_ID] ASC, [SOZLESME_ID] ASC);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.SOZLESMELER') AND name = N'IX_sozlesmeler_hedef')
BEGIN
    CREATE INDEX [IX_sozlesmeler_hedef] ON [dbo].[SOZLESMELER] ([HEDEF_KITLE] ASC, [SOZLESME_TIPI] ASC, [AKTIF_MI] ASC);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.SOZLESMELER') AND name = N'UX_sozlesmeler_slug_versiyon')
BEGIN
    CREATE UNIQUE INDEX [UX_sozlesmeler_slug_versiyon] ON [dbo].[SOZLESMELER] ([SLUG] ASC, [VERSIYON_NO] ASC);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.TELEFON_DOGRULAMA_TOKENLARI') AND name = N'IX_telefon_dogrulama_tokenlari_message')
BEGIN
    CREATE INDEX [IX_telefon_dogrulama_tokenlari_message] ON [dbo].[TELEFON_DOGRULAMA_TOKENLARI] ([META_MESAJ_ID] ASC);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.TELEFON_DOGRULAMA_TOKENLARI') AND name = N'IX_telefon_dogrulama_tokenlari_user')
BEGIN
    CREATE INDEX [IX_telefon_dogrulama_tokenlari_user] ON [dbo].[TELEFON_DOGRULAMA_TOKENLARI] ([KULLANICI_ID] ASC, [OLUSTURULMA_TARIHI] DESC);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.KULLANICILAR') AND name = N'IX_users_profil_resim_url')
BEGIN
    CREATE INDEX [IX_users_profil_resim_url] ON [dbo].[KULLANICILAR] ([PROFIL_RESIM_URL] ASC);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.KULLANICILAR') AND name = N'IX_users_telefon_dogrulama_durumu')
BEGIN
    CREATE INDEX [IX_users_telefon_dogrulama_durumu] ON [dbo].[KULLANICILAR] ([TELEFON_DOGRULAMA_DURUMU] ASC, [TELEFON_DOGRULAMA_TARIHI] ASC);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.KULLANICILAR') AND name = N'IX_users_telefon_e164')
BEGIN
    CREATE INDEX [IX_users_telefon_e164] ON [dbo].[KULLANICILAR] ([TELEFON_E164] ASC);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.KULLANICILAR') AND name = N'UX_users_eposta')
BEGIN
    CREATE UNIQUE INDEX [UX_users_eposta] ON [dbo].[KULLANICILAR] ([EPOSTA] ASC) WHERE ([EPOSTA] IS NOT NULL AND [EPOSTA]<>N'');
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.WHATSAPP_MESAJ_LOGLARI') AND name = N'IX_whatsapp_mesaj_loglari_meta')
BEGIN
    CREATE INDEX [IX_whatsapp_mesaj_loglari_meta] ON [dbo].[WHATSAPP_MESAJ_LOGLARI] ([META_MESAJ_ID] ASC);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.WHATSAPP_MESAJ_LOGLARI') AND name = N'IX_whatsapp_mesaj_loglari_phone')
BEGIN
    CREATE INDEX [IX_whatsapp_mesaj_loglari_phone] ON [dbo].[WHATSAPP_MESAJ_LOGLARI] ([TELEFON_E164] ASC, [OLUSTURULMA_TARIHI] DESC);
END
GO

