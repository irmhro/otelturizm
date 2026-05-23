SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.users', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[users]
    (
        [id] bigint IDENTITY(1,1) NOT NULL,
        [ad_soyad] nvarchar(100) NOT NULL,
        [eposta] nvarchar(100) NOT NULL,
        [telefon] nvarchar(20) NULL,
        [tc_kimlik_no] nvarchar(11) NULL,
        [dogum_tarihi] date NULL,
        [cinsiyet] nvarchar(255) NULL,
        [uyruk] nvarchar(50) NULL,
        [adres] nvarchar(max) NULL,
        [sehir] nvarchar(100) NULL,
        [ilce] nvarchar(100) NULL,
        [mahalle] nvarchar(120) NULL,
        [posta_kodu] nvarchar(10) NULL,
        [tercih_edilen_oda_tipi] nvarchar(50) NULL,
        [yatak_tercihi] nvarchar(50) NULL,
        [konusulan_diller] nvarchar(200) NULL,
        [seyahat_amaci] nvarchar(255) NULL,
        [ozel_istekler] nvarchar(max) NULL,
        [iki_asamali_dogrulama_aktif_mi] bit CONSTRAINT [DF__users__iki_asama__4BCC3ABA] DEFAULT ((0)) NOT NULL,
        [profil_tamamlanma_tarihi] datetime2(0) NULL,
        [sifre] nvarchar(255) NOT NULL,
        [rol] nvarchar(64) CONSTRAINT [DF_users_rol] DEFAULT ('user') NOT NULL,
        [sahiplik_partner_id] bigint NULL,
        [firma_id] bigint NULL,
        [departman] nvarchar(100) NULL,
        [gorev_unvani] nvarchar(100) NULL,
        [satis_ekibi] nvarchar(100) NULL,
        [gunluk_satis_hedefi] decimal(12,2) NULL,
        [aylik_satis_hedefi] decimal(12,2) NULL,
        [dahili_numara] nvarchar(20) NULL,
        [harcama_limiti] decimal(10,2) NULL,
        [onay_gereksinimi] bit CONSTRAINT [DF__users__onay_gere__4CC05EF3] DEFAULT ((0)) NOT NULL,
        [personel_kodu] nvarchar(30) NULL,
        [firma_yonetici_mi] bit CONSTRAINT [DF__users__firma_yon__4DB4832C] DEFAULT ((0)) NOT NULL,
        [son_sirket_girisi_tarihi] datetime2(0) NULL,
        [profil_fotografi] nvarchar(255) NULL,
        [email_dogrulama_tarihi] datetime2(0) NULL,
        [basarisiz_giris_sayisi] smallint CONSTRAINT [DF__users__basarisiz__4EA8A765] DEFAULT ((0)) NOT NULL,
        [son_basarisiz_giris_tarihi] datetime2(0) NULL,
        [giris_kilit_bitis_tarihi] datetime2(0) NULL,
        [email_dogrulama_son_gonderim_tarihi] datetime2(0) NULL,
        [telefon_dogrulama_tarihi] datetime2(0) NULL,
        [kvkk_onay_tarihi] datetime2(0) NULL,
        [pazarlama_izni] bit CONSTRAINT [DF__users__pazarlama__4F9CCB9E] DEFAULT ((0)) NOT NULL,
        [kayit_kaynagi] nvarchar(50) NULL,
        [son_giris_tarihi] datetime2(0) NULL,
        [son_giris_ip] nvarchar(45) NULL,
        [hesap_durumu] tinyint CONSTRAINT [DF__users__hesap_dur__5090EFD7] DEFAULT ((1)) NOT NULL,
        [dil_tercihi] nvarchar(5) NULL,
        [para_birimi] nvarchar(3) NULL,
        [ulke] nvarchar(50) NULL,
        [olusturulma_tarihi] datetime2(0) CONSTRAINT [DF__users__olusturul__51851410] DEFAULT (sysutcdatetime()) NULL,
        [guncellenme_tarihi] datetime2(0) NULL,
        [telefon_e164] nvarchar(32) NULL,
        [telefon_dogrulama_kanali] nvarchar(30) NULL,
        [telefon_dogrulama_durumu] nvarchar(30) NULL,
        [telefon_son_dogrulama_istek_tarihi] datetime2(7) NULL,
        [telefon_son_sahiplik_teyit_tarihi] datetime2(7) NULL,
        [telefon_degistirilme_tarihi] datetime2(7) NULL,
        [iki_asamali_dogrulama_kanali] nvarchar(20) CONSTRAINT [DF_users_iki_asamali_dogrulama_kanali] DEFAULT (N'email') NULL,
        [profil_resim_url] nvarchar(255) NULL,
        [profil_resim_kaynak] nvarchar(30) NULL,
        [tercih_locale] nvarchar(16) NULL,
        [tercih_para_birimi] nvarchar(8) NULL,
        CONSTRAINT [PK_users] PRIMARY KEY CLUSTERED ([id] ASC)
    );
END
GO

-- Missing column repair blocks for existing databases.
IF COL_LENGTH(N'dbo.users', N'id') IS NULL
BEGIN
    ALTER TABLE [dbo].[users] ADD [id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.users', N'ad_soyad') IS NULL
BEGIN
    ALTER TABLE [dbo].[users] ADD [ad_soyad] nvarchar(100) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.users', N'eposta') IS NULL
BEGIN
    ALTER TABLE [dbo].[users] ADD [eposta] nvarchar(100) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.users', N'telefon') IS NULL
BEGIN
    ALTER TABLE [dbo].[users] ADD [telefon] nvarchar(20) NULL;
END
GO
IF COL_LENGTH(N'dbo.users', N'tc_kimlik_no') IS NULL
BEGIN
    ALTER TABLE [dbo].[users] ADD [tc_kimlik_no] nvarchar(11) NULL;
END
GO
IF COL_LENGTH(N'dbo.users', N'dogum_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[users] ADD [dogum_tarihi] date NULL;
END
GO
IF COL_LENGTH(N'dbo.users', N'cinsiyet') IS NULL
BEGIN
    ALTER TABLE [dbo].[users] ADD [cinsiyet] nvarchar(255) NULL;
END
GO
IF COL_LENGTH(N'dbo.users', N'uyruk') IS NULL
BEGIN
    ALTER TABLE [dbo].[users] ADD [uyruk] nvarchar(50) NULL;
END
GO
IF COL_LENGTH(N'dbo.users', N'adres') IS NULL
BEGIN
    ALTER TABLE [dbo].[users] ADD [adres] nvarchar(max) NULL;
END
GO
IF COL_LENGTH(N'dbo.users', N'sehir') IS NULL
BEGIN
    ALTER TABLE [dbo].[users] ADD [sehir] nvarchar(100) NULL;
END
GO
IF COL_LENGTH(N'dbo.users', N'ilce') IS NULL
BEGIN
    ALTER TABLE [dbo].[users] ADD [ilce] nvarchar(100) NULL;
END
GO
IF COL_LENGTH(N'dbo.users', N'mahalle') IS NULL
BEGIN
    ALTER TABLE [dbo].[users] ADD [mahalle] nvarchar(120) NULL;
END
GO
IF COL_LENGTH(N'dbo.users', N'posta_kodu') IS NULL
BEGIN
    ALTER TABLE [dbo].[users] ADD [posta_kodu] nvarchar(10) NULL;
END
GO
IF COL_LENGTH(N'dbo.users', N'tercih_edilen_oda_tipi') IS NULL
BEGIN
    ALTER TABLE [dbo].[users] ADD [tercih_edilen_oda_tipi] nvarchar(50) NULL;
END
GO
IF COL_LENGTH(N'dbo.users', N'yatak_tercihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[users] ADD [yatak_tercihi] nvarchar(50) NULL;
END
GO
IF COL_LENGTH(N'dbo.users', N'konusulan_diller') IS NULL
BEGIN
    ALTER TABLE [dbo].[users] ADD [konusulan_diller] nvarchar(200) NULL;
END
GO
IF COL_LENGTH(N'dbo.users', N'seyahat_amaci') IS NULL
BEGIN
    ALTER TABLE [dbo].[users] ADD [seyahat_amaci] nvarchar(255) NULL;
END
GO
IF COL_LENGTH(N'dbo.users', N'ozel_istekler') IS NULL
BEGIN
    ALTER TABLE [dbo].[users] ADD [ozel_istekler] nvarchar(max) NULL;
END
GO
IF COL_LENGTH(N'dbo.users', N'iki_asamali_dogrulama_aktif_mi') IS NULL
BEGIN
    ALTER TABLE [dbo].[users] ADD [iki_asamali_dogrulama_aktif_mi] bit DEFAULT ((0)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.users', N'profil_tamamlanma_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[users] ADD [profil_tamamlanma_tarihi] datetime2(0) NULL;
END
GO
IF COL_LENGTH(N'dbo.users', N'sifre') IS NULL
BEGIN
    ALTER TABLE [dbo].[users] ADD [sifre] nvarchar(255) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.users', N'rol') IS NULL
BEGIN
    ALTER TABLE [dbo].[users] ADD [rol] nvarchar(64) DEFAULT ('user') NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.users', N'sahiplik_partner_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[users] ADD [sahiplik_partner_id] bigint NULL;
END
GO
IF COL_LENGTH(N'dbo.users', N'firma_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[users] ADD [firma_id] bigint NULL;
END
GO
IF COL_LENGTH(N'dbo.users', N'departman') IS NULL
BEGIN
    ALTER TABLE [dbo].[users] ADD [departman] nvarchar(100) NULL;
END
GO
IF COL_LENGTH(N'dbo.users', N'gorev_unvani') IS NULL
BEGIN
    ALTER TABLE [dbo].[users] ADD [gorev_unvani] nvarchar(100) NULL;
END
GO
IF COL_LENGTH(N'dbo.users', N'satis_ekibi') IS NULL
BEGIN
    ALTER TABLE [dbo].[users] ADD [satis_ekibi] nvarchar(100) NULL;
END
GO
IF COL_LENGTH(N'dbo.users', N'gunluk_satis_hedefi') IS NULL
BEGIN
    ALTER TABLE [dbo].[users] ADD [gunluk_satis_hedefi] decimal(12,2) NULL;
END
GO
IF COL_LENGTH(N'dbo.users', N'aylik_satis_hedefi') IS NULL
BEGIN
    ALTER TABLE [dbo].[users] ADD [aylik_satis_hedefi] decimal(12,2) NULL;
END
GO
IF COL_LENGTH(N'dbo.users', N'dahili_numara') IS NULL
BEGIN
    ALTER TABLE [dbo].[users] ADD [dahili_numara] nvarchar(20) NULL;
END
GO
IF COL_LENGTH(N'dbo.users', N'harcama_limiti') IS NULL
BEGIN
    ALTER TABLE [dbo].[users] ADD [harcama_limiti] decimal(10,2) NULL;
END
GO
IF COL_LENGTH(N'dbo.users', N'onay_gereksinimi') IS NULL
BEGIN
    ALTER TABLE [dbo].[users] ADD [onay_gereksinimi] bit DEFAULT ((0)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.users', N'personel_kodu') IS NULL
BEGIN
    ALTER TABLE [dbo].[users] ADD [personel_kodu] nvarchar(30) NULL;
END
GO
IF COL_LENGTH(N'dbo.users', N'firma_yonetici_mi') IS NULL
BEGIN
    ALTER TABLE [dbo].[users] ADD [firma_yonetici_mi] bit DEFAULT ((0)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.users', N'son_sirket_girisi_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[users] ADD [son_sirket_girisi_tarihi] datetime2(0) NULL;
END
GO
IF COL_LENGTH(N'dbo.users', N'profil_fotografi') IS NULL
BEGIN
    ALTER TABLE [dbo].[users] ADD [profil_fotografi] nvarchar(255) NULL;
END
GO
IF COL_LENGTH(N'dbo.users', N'email_dogrulama_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[users] ADD [email_dogrulama_tarihi] datetime2(0) NULL;
END
GO
IF COL_LENGTH(N'dbo.users', N'basarisiz_giris_sayisi') IS NULL
BEGIN
    ALTER TABLE [dbo].[users] ADD [basarisiz_giris_sayisi] smallint DEFAULT ((0)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.users', N'son_basarisiz_giris_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[users] ADD [son_basarisiz_giris_tarihi] datetime2(0) NULL;
END
GO
IF COL_LENGTH(N'dbo.users', N'giris_kilit_bitis_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[users] ADD [giris_kilit_bitis_tarihi] datetime2(0) NULL;
END
GO
IF COL_LENGTH(N'dbo.users', N'email_dogrulama_son_gonderim_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[users] ADD [email_dogrulama_son_gonderim_tarihi] datetime2(0) NULL;
END
GO
IF COL_LENGTH(N'dbo.users', N'telefon_dogrulama_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[users] ADD [telefon_dogrulama_tarihi] datetime2(0) NULL;
END
GO
IF COL_LENGTH(N'dbo.users', N'kvkk_onay_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[users] ADD [kvkk_onay_tarihi] datetime2(0) NULL;
END
GO
IF COL_LENGTH(N'dbo.users', N'pazarlama_izni') IS NULL
BEGIN
    ALTER TABLE [dbo].[users] ADD [pazarlama_izni] bit DEFAULT ((0)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.users', N'kayit_kaynagi') IS NULL
BEGIN
    ALTER TABLE [dbo].[users] ADD [kayit_kaynagi] nvarchar(50) NULL;
END
GO
IF COL_LENGTH(N'dbo.users', N'son_giris_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[users] ADD [son_giris_tarihi] datetime2(0) NULL;
END
GO
IF COL_LENGTH(N'dbo.users', N'son_giris_ip') IS NULL
BEGIN
    ALTER TABLE [dbo].[users] ADD [son_giris_ip] nvarchar(45) NULL;
END
GO
IF COL_LENGTH(N'dbo.users', N'hesap_durumu') IS NULL
BEGIN
    ALTER TABLE [dbo].[users] ADD [hesap_durumu] tinyint DEFAULT ((1)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.users', N'dil_tercihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[users] ADD [dil_tercihi] nvarchar(5) NULL;
END
GO
IF COL_LENGTH(N'dbo.users', N'para_birimi') IS NULL
BEGIN
    ALTER TABLE [dbo].[users] ADD [para_birimi] nvarchar(3) NULL;
END
GO
IF COL_LENGTH(N'dbo.users', N'ulke') IS NULL
BEGIN
    ALTER TABLE [dbo].[users] ADD [ulke] nvarchar(50) NULL;
END
GO
IF COL_LENGTH(N'dbo.users', N'olusturulma_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[users] ADD [olusturulma_tarihi] datetime2(0) DEFAULT (sysutcdatetime()) NULL;
END
GO
IF COL_LENGTH(N'dbo.users', N'guncellenme_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[users] ADD [guncellenme_tarihi] datetime2(0) NULL;
END
GO
IF COL_LENGTH(N'dbo.users', N'telefon_e164') IS NULL
BEGIN
    ALTER TABLE [dbo].[users] ADD [telefon_e164] nvarchar(32) NULL;
END
GO
IF COL_LENGTH(N'dbo.users', N'telefon_dogrulama_kanali') IS NULL
BEGIN
    ALTER TABLE [dbo].[users] ADD [telefon_dogrulama_kanali] nvarchar(30) NULL;
END
GO
IF COL_LENGTH(N'dbo.users', N'telefon_dogrulama_durumu') IS NULL
BEGIN
    ALTER TABLE [dbo].[users] ADD [telefon_dogrulama_durumu] nvarchar(30) NULL;
END
GO
IF COL_LENGTH(N'dbo.users', N'telefon_son_dogrulama_istek_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[users] ADD [telefon_son_dogrulama_istek_tarihi] datetime2(7) NULL;
END
GO
IF COL_LENGTH(N'dbo.users', N'telefon_son_sahiplik_teyit_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[users] ADD [telefon_son_sahiplik_teyit_tarihi] datetime2(7) NULL;
END
GO
IF COL_LENGTH(N'dbo.users', N'telefon_degistirilme_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[users] ADD [telefon_degistirilme_tarihi] datetime2(7) NULL;
END
GO
IF COL_LENGTH(N'dbo.users', N'iki_asamali_dogrulama_kanali') IS NULL
BEGIN
    ALTER TABLE [dbo].[users] ADD [iki_asamali_dogrulama_kanali] nvarchar(20) DEFAULT (N'email') NULL;
END
GO
IF COL_LENGTH(N'dbo.users', N'profil_resim_url') IS NULL
BEGIN
    ALTER TABLE [dbo].[users] ADD [profil_resim_url] nvarchar(255) NULL;
END
GO
IF COL_LENGTH(N'dbo.users', N'profil_resim_kaynak') IS NULL
BEGIN
    ALTER TABLE [dbo].[users] ADD [profil_resim_kaynak] nvarchar(30) NULL;
END
GO
IF COL_LENGTH(N'dbo.users', N'tercih_locale') IS NULL
BEGIN
    ALTER TABLE [dbo].[users] ADD [tercih_locale] nvarchar(16) NULL;
END
GO
IF COL_LENGTH(N'dbo.users', N'tercih_para_birimi') IS NULL
BEGIN
    ALTER TABLE [dbo].[users] ADD [tercih_para_birimi] nvarchar(8) NULL;
END
GO
