-- Foreign keys generated from current local MSSQL database.

IF OBJECT_ID(N'dbo.FK_firma_oda_fiyat_musaitlik_oda_tip', N'F') IS NULL
BEGIN
    ALTER TABLE [dbo].[firma_oda_fiyat_musaitlik] WITH CHECK ADD CONSTRAINT [FK_firma_oda_fiyat_musaitlik_oda_tip] FOREIGN KEY ([oda_tip_id]) REFERENCES [dbo].[oda_tipleri] ([id]);
END
GO

IF OBJECT_ID(N'dbo.FK_firma_oda_fiyat_musaitlik_otel', N'F') IS NULL
BEGIN
    ALTER TABLE [dbo].[firma_oda_fiyat_musaitlik] WITH CHECK ADD CONSTRAINT [FK_firma_oda_fiyat_musaitlik_otel] FOREIGN KEY ([otel_id]) REFERENCES [dbo].[oteller] ([id]);
END
GO

IF OBJECT_ID(N'dbo.FK_firma_oda_fiyat_musaitlik_users_updated_by', N'F') IS NULL
BEGIN
    ALTER TABLE [dbo].[firma_oda_fiyat_musaitlik] WITH CHECK ADD CONSTRAINT [FK_firma_oda_fiyat_musaitlik_users_updated_by] FOREIGN KEY ([guncelleyen_kullanici_id]) REFERENCES [dbo].[users] ([id]);
END
GO

IF OBJECT_ID(N'dbo.FK_gelistirme_talepleri_ana', N'F') IS NULL
BEGIN
    ALTER TABLE [dbo].[gelistirme_talepleri] WITH CHECK ADD CONSTRAINT [FK_gelistirme_talepleri_ana] FOREIGN KEY ([ana_talep_id]) REFERENCES [dbo].[gelistirme_talepleri] ([id]);
END
GO

IF OBJECT_ID(N'dbo.FK_gelistirme_talepleri_atanan', N'F') IS NULL
BEGIN
    ALTER TABLE [dbo].[gelistirme_talepleri] WITH CHECK ADD CONSTRAINT [FK_gelistirme_talepleri_atanan] FOREIGN KEY ([atanan_gelistirici_id]) REFERENCES [dbo].[users] ([id]);
END
GO

IF OBJECT_ID(N'dbo.FK_gelistirme_talepleri_cevap', N'F') IS NULL
BEGIN
    ALTER TABLE [dbo].[gelistirme_talepleri] WITH CHECK ADD CONSTRAINT [FK_gelistirme_talepleri_cevap] FOREIGN KEY ([cevap_talep_id]) REFERENCES [dbo].[gelistirme_talepleri] ([id]);
END
GO

IF OBJECT_ID(N'dbo.FK_gelistirme_talepleri_olusturan', N'F') IS NULL
BEGIN
    ALTER TABLE [dbo].[gelistirme_talepleri] WITH CHECK ADD CONSTRAINT [FK_gelistirme_talepleri_olusturan] FOREIGN KEY ([olusturan_kullanici_id]) REFERENCES [dbo].[users] ([id]);
END
GO

IF OBJECT_ID(N'dbo.FK_kullanici_giris_2fa_tokenlari_users', N'F') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_giris_2fa_tokenlari] WITH CHECK ADD CONSTRAINT [FK_kullanici_giris_2fa_tokenlari_users] FOREIGN KEY ([kullanici_id]) REFERENCES [dbo].[users] ([id]) ON DELETE CASCADE;
END
GO

IF OBJECT_ID(N'dbo.FK_kullanici_giris_loglari_users', N'F') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_giris_loglari] WITH CHECK ADD CONSTRAINT [FK_kullanici_giris_loglari_users] FOREIGN KEY ([kullanici_id]) REFERENCES [dbo].[users] ([id]) ON DELETE CASCADE;
END
GO

IF OBJECT_ID(N'dbo.FK_kullanici_telefon_gecmisi_users', N'F') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_telefon_gecmisi] WITH CHECK ADD CONSTRAINT [FK_kullanici_telefon_gecmisi_users] FOREIGN KEY ([kullanici_id]) REFERENCES [dbo].[users] ([id]);
END
GO

IF OBJECT_ID(N'dbo.FK_oda_fiyat_musaitlik_fiyat_indirimleri', N'F') IS NULL
BEGIN
    ALTER TABLE [dbo].[oda_fiyat_musaitlik] WITH CHECK ADD CONSTRAINT [FK_oda_fiyat_musaitlik_fiyat_indirimleri] FOREIGN KEY ([indirim_id]) REFERENCES [dbo].[fiyat_indirimleri] ([id]);
END
GO

IF OBJECT_ID(N'dbo.FK_otel_liste_abonelikleri_oteller', N'F') IS NULL
BEGIN
    ALTER TABLE [dbo].[otel_liste_abonelikleri] WITH CHECK ADD CONSTRAINT [FK_otel_liste_abonelikleri_oteller] FOREIGN KEY ([otel_id]) REFERENCES [dbo].[oteller] ([id]);
END
GO

IF OBJECT_ID(N'dbo.FK_otel_liste_abonelikleri_users_approver', N'F') IS NULL
BEGIN
    ALTER TABLE [dbo].[otel_liste_abonelikleri] WITH CHECK ADD CONSTRAINT [FK_otel_liste_abonelikleri_users_approver] FOREIGN KEY ([onaylayan_admin_user_id]) REFERENCES [dbo].[users] ([id]);
END
GO

IF OBJECT_ID(N'dbo.FK_otel_liste_abonelikleri_users_requester', N'F') IS NULL
BEGIN
    ALTER TABLE [dbo].[otel_liste_abonelikleri] WITH CHECK ADD CONSTRAINT [FK_otel_liste_abonelikleri_users_requester] FOREIGN KEY ([talep_eden_user_id]) REFERENCES [dbo].[users] ([id]);
END
GO

IF OBJECT_ID(N'dbo.FK_oteller_otel_tipleri', N'F') IS NULL
BEGIN
    ALTER TABLE [dbo].[oteller] WITH CHECK ADD CONSTRAINT [FK_oteller_otel_tipleri] FOREIGN KEY ([otel_tipi_id]) REFERENCES [dbo].[otel_tipleri] ([id]);
END
GO

IF OBJECT_ID(N'dbo.FK_partner_detaylari_otel_tipleri', N'F') IS NULL
BEGIN
    ALTER TABLE [dbo].[partner_detaylari] WITH CHECK ADD CONSTRAINT [FK_partner_detaylari_otel_tipleri] FOREIGN KEY ([otel_tipi_id]) REFERENCES [dbo].[otel_tipleri] ([id]);
END
GO

IF OBJECT_ID(N'dbo.FK_platform_email_mesajlari_hesap', N'F') IS NULL
BEGIN
    ALTER TABLE [dbo].[platform_email_mesajlari] WITH CHECK ADD CONSTRAINT [FK_platform_email_mesajlari_hesap] FOREIGN KEY ([hesap_id]) REFERENCES [dbo].[platform_email_hesaplari] ([id]);
END
GO

IF OBJECT_ID(N'dbo.FK_rezervasyon_odeme_kalem_dekont', N'F') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyon_odeme_kalemleri] WITH CHECK ADD CONSTRAINT [FK_rezervasyon_odeme_kalem_dekont] FOREIGN KEY ([dekont_guvenli_dosya_id]) REFERENCES [dbo].[guvenli_dosya_varliklari] ([id]);
END
GO

IF OBJECT_ID(N'dbo.FK_rezervasyon_odeme_kalem_durum', N'F') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyon_odeme_kalemleri] WITH CHECK ADD CONSTRAINT [FK_rezervasyon_odeme_kalem_durum] FOREIGN KEY ([odeme_durumu_id]) REFERENCES [dbo].[odeme_durumu_tanimlari] ([id]);
END
GO

IF OBJECT_ID(N'dbo.FK_rezervasyon_odeme_kalem_rez', N'F') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyon_odeme_kalemleri] WITH CHECK ADD CONSTRAINT [FK_rezervasyon_odeme_kalem_rez] FOREIGN KEY ([rezervasyon_id]) REFERENCES [dbo].[rezervasyonlar] ([id]);
END
GO

IF OBJECT_ID(N'dbo.FK_rezervasyon_odeme_kalem_yontem', N'F') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyon_odeme_kalemleri] WITH CHECK ADD CONSTRAINT [FK_rezervasyon_odeme_kalem_yontem] FOREIGN KEY ([odeme_yontemi_id]) REFERENCES [dbo].[odeme_yontemi_tanimlari] ([id]);
END
GO

IF OBJECT_ID(N'dbo.FK_rezervasyonlar_komisyon_vergiler', N'F') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyonlar] WITH CHECK ADD CONSTRAINT [FK_rezervasyonlar_komisyon_vergiler] FOREIGN KEY ([komisyon_vergi_kural_id]) REFERENCES [dbo].[komisyon_vergiler] ([id]);
END
GO

IF OBJECT_ID(N'dbo.FK_rezervasyonlar_odeme_durumu', N'F') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyonlar] WITH CHECK ADD CONSTRAINT [FK_rezervasyonlar_odeme_durumu] FOREIGN KEY ([odeme_durumu_id]) REFERENCES [dbo].[odeme_durumu_tanimlari] ([id]);
END
GO

IF OBJECT_ID(N'dbo.FK_rezervasyonlar_rezervasyon_durumu', N'F') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyonlar] WITH CHECK ADD CONSTRAINT [FK_rezervasyonlar_rezervasyon_durumu] FOREIGN KEY ([rezervasyon_durumu_id]) REFERENCES [dbo].[rezervasyon_durum_tanimlari] ([id]);
END
GO

IF OBJECT_ID(N'dbo.FK_sozlesme_gonderim_sozlesmeler', N'F') IS NULL
BEGIN
    ALTER TABLE [dbo].[sozlesme_gonderim_loglari] WITH CHECK ADD CONSTRAINT [FK_sozlesme_gonderim_sozlesmeler] FOREIGN KEY ([sozlesme_id]) REFERENCES [dbo].[sozlesmeler] ([id]);
END
GO

IF OBJECT_ID(N'dbo.FK_sozlesme_gonderim_users', N'F') IS NULL
BEGIN
    ALTER TABLE [dbo].[sozlesme_gonderim_loglari] WITH CHECK ADD CONSTRAINT [FK_sozlesme_gonderim_users] FOREIGN KEY ([kullanici_id]) REFERENCES [dbo].[users] ([id]);
END
GO

IF OBJECT_ID(N'dbo.FK_sozlesme_kabulleri_sozlesmeler', N'F') IS NULL
BEGIN
    ALTER TABLE [dbo].[sozlesme_kabulleri] WITH CHECK ADD CONSTRAINT [FK_sozlesme_kabulleri_sozlesmeler] FOREIGN KEY ([sozlesme_id]) REFERENCES [dbo].[sozlesmeler] ([id]);
END
GO

IF OBJECT_ID(N'dbo.FK_sozlesme_kabulleri_users', N'F') IS NULL
BEGIN
    ALTER TABLE [dbo].[sozlesme_kabulleri] WITH CHECK ADD CONSTRAINT [FK_sozlesme_kabulleri_users] FOREIGN KEY ([kullanici_id]) REFERENCES [dbo].[users] ([id]);
END
GO

IF OBJECT_ID(N'dbo.FK_telefon_dogrulama_tokenlari_users', N'F') IS NULL
BEGIN
    ALTER TABLE [dbo].[telefon_dogrulama_tokenlari] WITH CHECK ADD CONSTRAINT [FK_telefon_dogrulama_tokenlari_users] FOREIGN KEY ([kullanici_id]) REFERENCES [dbo].[users] ([id]);
END
GO

IF OBJECT_ID(N'dbo.FK_whatsapp_mesaj_loglari_users', N'F') IS NULL
BEGIN
    ALTER TABLE [dbo].[whatsapp_mesaj_loglari] WITH CHECK ADD CONSTRAINT [FK_whatsapp_mesaj_loglari_users] FOREIGN KEY ([kullanici_id]) REFERENCES [dbo].[users] ([id]);
END
GO

