SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.oteller', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[oteller]
    (
        [id] bigint IDENTITY(1,1) NOT NULL,
        [otel_kodu] nvarchar(32) NOT NULL,
        [partner_id] bigint NOT NULL,
        [user_id] bigint NULL,
        [otel_adi] nvarchar(255) NOT NULL,
        [otel_turu] nvarchar(255) NOT NULL,
        [yildiz_sayisi] tinyint NULL,
        [turizm_belge_no] nvarchar(50) NULL,
        [turizm_belge_turu] nvarchar(255) NULL,
        [ulke] nvarchar(50) NULL,
        [sehir] nvarchar(50) NOT NULL,
        [ilce] nvarchar(50) NOT NULL,
        [mahalle] nvarchar(100) NULL,
        [tam_adres] nvarchar(max) NOT NULL,
        [posta_kodu] nvarchar(10) NULL,
        [enlem] decimal(10,8) NULL,
        [boylam] decimal(11,8) NULL,
        [ulke_id] smallint NULL,
        [sehir_id] int NULL,
        [ilce_id] int NULL,
        [bolge_id] int NULL,
        [telefon_1] nvarchar(20) NOT NULL,
        [telefon_2] nvarchar(20) NULL,
        [faks] nvarchar(20) NULL,
        [eposta] nvarchar(100) NOT NULL,
        [web_sitesi] nvarchar(255) NULL,
        [rezervasyon_telefonu] nvarchar(20) NULL,
        [satis_kontak_adi] nvarchar(100) NULL,
        [satis_kontak_telefonu] nvarchar(20) NULL,
        [satis_kontak_eposta] nvarchar(100) NULL,
        [satis_notlari] nvarchar(max) NULL,
        [check_in_saati] time(0) NULL,
        [check_out_saati] time(0) NULL,
        [gec_check_out_mumkun_mu] bit CONSTRAINT [DF__oteller__gec_che__29AC2CE0] DEFAULT ((0)) NULL,
        [gec_check_out_ucreti] decimal(10,2) NULL,
        [erken_check_in_mumkun_mu] bit CONSTRAINT [DF__oteller__erken_c__2AA05119] DEFAULT ((0)) NULL,
        [erken_check_in_ucreti] decimal(10,2) NULL,
        [toplam_oda_sayisi] smallint NOT NULL,
        [toplam_yatak_kapasitesi] smallint NULL,
        [kat_sayisi] tinyint NULL,
        [asansor_var_mi] bit CONSTRAINT [DF__oteller__asansor__2B947552] DEFAULT ((0)) NULL,
        [asansor_sayisi] tinyint CONSTRAINT [DF__oteller__asansor__2C88998B] DEFAULT ((0)) NULL,
        [kisa_aciklama] nvarchar(500) NULL,
        [uzun_aciklama] nvarchar(max) NULL,
        [konum_aciklamasi] nvarchar(max) NULL,
        [komisyon_turu] nvarchar(255) NULL,
        [varsayilan_komisyon_orani] decimal(5,2) NOT NULL,
        [komisyon_hesaplama_tipi] nvarchar(255) NULL,
        [odeme_vadesi] nvarchar(255) NOT NULL,
        [odeme_yontemi] nvarchar(255) NOT NULL,
        [fatura_kesim_turu] nvarchar(255) NOT NULL,
        [depozito_tutari] decimal(10,2) NULL,
        [depozito_iade_suresi] tinyint NULL,
        [minimum_konaklama_gecesi] tinyint CONSTRAINT [DF__oteller__minimum__2D7CBDC4] DEFAULT ((1)) NULL,
        [maksimum_konaklama_gecesi] smallint CONSTRAINT [DF__oteller__maksimu__2E70E1FD] DEFAULT ((30)) NULL,
        [konusulan_diller] nvarchar(255) NULL,
        [ortalama_puan] decimal(3,2) CONSTRAINT [DF__oteller__ortalam__2F650636] DEFAULT ((0.00)) NULL,
        [toplam_yorum_sayisi] int CONSTRAINT [DF__oteller__toplam___30592A6F] DEFAULT ((0)) NULL,
        [temizlik_puani] decimal(3,2) CONSTRAINT [DF__oteller__temizli__314D4EA8] DEFAULT ((0.00)) NULL,
        [konfor_puani] decimal(3,2) CONSTRAINT [DF__oteller__konfor___324172E1] DEFAULT ((0.00)) NULL,
        [konum_puani] decimal(3,2) CONSTRAINT [DF__oteller__konum_p__3335971A] DEFAULT ((0.00)) NULL,
        [personel_puani] decimal(3,2) CONSTRAINT [DF__oteller__persone__3429BB53] DEFAULT ((0.00)) NULL,
        [fiyat_performans_puani] decimal(3,2) CONSTRAINT [DF__oteller__fiyat_p__351DDF8C] DEFAULT ((0.00)) NULL,
        [kapak_fotografi] nvarchar(255) NULL,
        [galeri] nvarchar(max) NULL,
        [video_url] nvarchar(255) NULL,
        [sanal_tur_url] nvarchar(255) NULL,
        [yayin_durumu] nvarchar(255) NULL,
        [onay_durumu] nvarchar(255) NULL,
        [onay_tarihi] datetime2(0) NULL,
        [onaylayan_admin_id] bigint NULL,
        [populerlik_sirasi] int CONSTRAINT [DF__oteller__populer__361203C5] DEFAULT ((0)) NULL,
        [one_cikan_otel] bit CONSTRAINT [DF__oteller__one_cik__370627FE] DEFAULT ((0)) NULL,
        [tavsiye_edilen_otel] bit CONSTRAINT [DF__oteller__tavsiye__37FA4C37] DEFAULT ((0)) NULL,
        [olusturulma_tarihi] datetime2(0) CONSTRAINT [DF__oteller__olustur__38EE7070] DEFAULT (sysutcdatetime()) NULL,
        [guncellenme_tarihi] datetime2(0) NULL,
        [partner_ceza_bitis_tarihi] datetime2(0) NULL,
        [sehir_normalized] AS (lower(translate(coalesce([sehir],N''),concat(nchar((304)),nchar((73)),nchar((305)),nchar((199)),nchar((231)),nchar((286)),nchar((287)),nchar((214)),nchar((246)),nchar((350)),nchar((351)),nchar((220)),nchar((252))),N'iiiccggoossuu'))) PERSISTED,
        [ilce_normalized] AS (lower(translate(coalesce([ilce],N''),concat(nchar((304)),nchar((73)),nchar((305)),nchar((199)),nchar((231)),nchar((286)),nchar((287)),nchar((214)),nchar((246)),nchar((350)),nchar((351)),nchar((220)),nchar((252))),N'iiiccggoossuu'))) PERSISTED,
        [mahalle_normalized] AS (lower(translate(coalesce([mahalle],N''),concat(nchar((304)),nchar((73)),nchar((305)),nchar((199)),nchar((231)),nchar((286)),nchar((287)),nchar((214)),nchar((246)),nchar((350)),nchar((351)),nchar((220)),nchar((252))),N'iiiccggoossuu'))) PERSISTED,
        [otel_adi_normalized] AS (lower(translate(coalesce([otel_adi],N''),concat(nchar((304)),nchar((73)),nchar((305)),nchar((199)),nchar((231)),nchar((286)),nchar((287)),nchar((214)),nchar((246)),nchar((350)),nchar((351)),nchar((220)),nchar((252))),N'iiiccggoossuu'))) PERSISTED,
        [konum_normalized] AS (lower(translate(concat(coalesce([mahalle],N''),N' ',coalesce([ilce],N''),N' ',coalesce([sehir],N'')),concat(nchar((304)),nchar((73)),nchar((305)),nchar((199)),nchar((231)),nchar((286)),nchar((287)),nchar((214)),nchar((246)),nchar((350)),nchar((351)),nchar((220)),nchar((252))),N'iiiccggoossuu'))) PERSISTED,
        [fts_search_text] AS (concat(coalesce([otel_adi],N''),N' ',coalesce([mahalle],N''),N' ',coalesce([ilce],N''),N' ',coalesce([sehir],N''))) PERSISTED,
        [otel_tipi_id] int NULL,
        CONSTRAINT [PK_oteller] PRIMARY KEY CLUSTERED ([id] ASC)
    );
END
GO

-- Missing column repair blocks for existing databases.
IF COL_LENGTH(N'dbo.oteller', N'id') IS NULL
BEGIN
    ALTER TABLE [dbo].[oteller] ADD [id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.oteller', N'otel_kodu') IS NULL
BEGIN
    ALTER TABLE [dbo].[oteller] ADD [otel_kodu] nvarchar(32) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.oteller', N'partner_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[oteller] ADD [partner_id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.oteller', N'user_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[oteller] ADD [user_id] bigint NULL;
END
GO
IF COL_LENGTH(N'dbo.oteller', N'otel_adi') IS NULL
BEGIN
    ALTER TABLE [dbo].[oteller] ADD [otel_adi] nvarchar(255) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.oteller', N'otel_turu') IS NULL
BEGIN
    ALTER TABLE [dbo].[oteller] ADD [otel_turu] nvarchar(255) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.oteller', N'yildiz_sayisi') IS NULL
BEGIN
    ALTER TABLE [dbo].[oteller] ADD [yildiz_sayisi] tinyint NULL;
END
GO
IF COL_LENGTH(N'dbo.oteller', N'turizm_belge_no') IS NULL
BEGIN
    ALTER TABLE [dbo].[oteller] ADD [turizm_belge_no] nvarchar(50) NULL;
END
GO
IF COL_LENGTH(N'dbo.oteller', N'turizm_belge_turu') IS NULL
BEGIN
    ALTER TABLE [dbo].[oteller] ADD [turizm_belge_turu] nvarchar(255) NULL;
END
GO
IF COL_LENGTH(N'dbo.oteller', N'ulke') IS NULL
BEGIN
    ALTER TABLE [dbo].[oteller] ADD [ulke] nvarchar(50) NULL;
END
GO
IF COL_LENGTH(N'dbo.oteller', N'sehir') IS NULL
BEGIN
    ALTER TABLE [dbo].[oteller] ADD [sehir] nvarchar(50) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.oteller', N'ilce') IS NULL
BEGIN
    ALTER TABLE [dbo].[oteller] ADD [ilce] nvarchar(50) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.oteller', N'mahalle') IS NULL
BEGIN
    ALTER TABLE [dbo].[oteller] ADD [mahalle] nvarchar(100) NULL;
END
GO
IF COL_LENGTH(N'dbo.oteller', N'tam_adres') IS NULL
BEGIN
    ALTER TABLE [dbo].[oteller] ADD [tam_adres] nvarchar(max) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.oteller', N'posta_kodu') IS NULL
BEGIN
    ALTER TABLE [dbo].[oteller] ADD [posta_kodu] nvarchar(10) NULL;
END
GO
IF COL_LENGTH(N'dbo.oteller', N'enlem') IS NULL
BEGIN
    ALTER TABLE [dbo].[oteller] ADD [enlem] decimal(10,8) NULL;
END
GO
IF COL_LENGTH(N'dbo.oteller', N'boylam') IS NULL
BEGIN
    ALTER TABLE [dbo].[oteller] ADD [boylam] decimal(11,8) NULL;
END
GO
IF COL_LENGTH(N'dbo.oteller', N'ulke_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[oteller] ADD [ulke_id] smallint NULL;
END
GO
IF COL_LENGTH(N'dbo.oteller', N'sehir_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[oteller] ADD [sehir_id] int NULL;
END
GO
IF COL_LENGTH(N'dbo.oteller', N'ilce_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[oteller] ADD [ilce_id] int NULL;
END
GO
IF COL_LENGTH(N'dbo.oteller', N'bolge_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[oteller] ADD [bolge_id] int NULL;
END
GO
IF COL_LENGTH(N'dbo.oteller', N'telefon_1') IS NULL
BEGIN
    ALTER TABLE [dbo].[oteller] ADD [telefon_1] nvarchar(20) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.oteller', N'telefon_2') IS NULL
BEGIN
    ALTER TABLE [dbo].[oteller] ADD [telefon_2] nvarchar(20) NULL;
END
GO
IF COL_LENGTH(N'dbo.oteller', N'faks') IS NULL
BEGIN
    ALTER TABLE [dbo].[oteller] ADD [faks] nvarchar(20) NULL;
END
GO
IF COL_LENGTH(N'dbo.oteller', N'eposta') IS NULL
BEGIN
    ALTER TABLE [dbo].[oteller] ADD [eposta] nvarchar(100) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.oteller', N'web_sitesi') IS NULL
BEGIN
    ALTER TABLE [dbo].[oteller] ADD [web_sitesi] nvarchar(255) NULL;
END
GO
IF COL_LENGTH(N'dbo.oteller', N'rezervasyon_telefonu') IS NULL
BEGIN
    ALTER TABLE [dbo].[oteller] ADD [rezervasyon_telefonu] nvarchar(20) NULL;
END
GO
IF COL_LENGTH(N'dbo.oteller', N'satis_kontak_adi') IS NULL
BEGIN
    ALTER TABLE [dbo].[oteller] ADD [satis_kontak_adi] nvarchar(100) NULL;
END
GO
IF COL_LENGTH(N'dbo.oteller', N'satis_kontak_telefonu') IS NULL
BEGIN
    ALTER TABLE [dbo].[oteller] ADD [satis_kontak_telefonu] nvarchar(20) NULL;
END
GO
IF COL_LENGTH(N'dbo.oteller', N'satis_kontak_eposta') IS NULL
BEGIN
    ALTER TABLE [dbo].[oteller] ADD [satis_kontak_eposta] nvarchar(100) NULL;
END
GO
IF COL_LENGTH(N'dbo.oteller', N'satis_notlari') IS NULL
BEGIN
    ALTER TABLE [dbo].[oteller] ADD [satis_notlari] nvarchar(max) NULL;
END
GO
IF COL_LENGTH(N'dbo.oteller', N'check_in_saati') IS NULL
BEGIN
    ALTER TABLE [dbo].[oteller] ADD [check_in_saati] time(0) NULL;
END
GO
IF COL_LENGTH(N'dbo.oteller', N'check_out_saati') IS NULL
BEGIN
    ALTER TABLE [dbo].[oteller] ADD [check_out_saati] time(0) NULL;
END
GO
IF COL_LENGTH(N'dbo.oteller', N'gec_check_out_mumkun_mu') IS NULL
BEGIN
    ALTER TABLE [dbo].[oteller] ADD [gec_check_out_mumkun_mu] bit DEFAULT ((0)) NULL;
END
GO
IF COL_LENGTH(N'dbo.oteller', N'gec_check_out_ucreti') IS NULL
BEGIN
    ALTER TABLE [dbo].[oteller] ADD [gec_check_out_ucreti] decimal(10,2) NULL;
END
GO
IF COL_LENGTH(N'dbo.oteller', N'erken_check_in_mumkun_mu') IS NULL
BEGIN
    ALTER TABLE [dbo].[oteller] ADD [erken_check_in_mumkun_mu] bit DEFAULT ((0)) NULL;
END
GO
IF COL_LENGTH(N'dbo.oteller', N'erken_check_in_ucreti') IS NULL
BEGIN
    ALTER TABLE [dbo].[oteller] ADD [erken_check_in_ucreti] decimal(10,2) NULL;
END
GO
IF COL_LENGTH(N'dbo.oteller', N'toplam_oda_sayisi') IS NULL
BEGIN
    ALTER TABLE [dbo].[oteller] ADD [toplam_oda_sayisi] smallint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.oteller', N'toplam_yatak_kapasitesi') IS NULL
BEGIN
    ALTER TABLE [dbo].[oteller] ADD [toplam_yatak_kapasitesi] smallint NULL;
END
GO
IF COL_LENGTH(N'dbo.oteller', N'kat_sayisi') IS NULL
BEGIN
    ALTER TABLE [dbo].[oteller] ADD [kat_sayisi] tinyint NULL;
END
GO
IF COL_LENGTH(N'dbo.oteller', N'asansor_var_mi') IS NULL
BEGIN
    ALTER TABLE [dbo].[oteller] ADD [asansor_var_mi] bit DEFAULT ((0)) NULL;
END
GO
IF COL_LENGTH(N'dbo.oteller', N'asansor_sayisi') IS NULL
BEGIN
    ALTER TABLE [dbo].[oteller] ADD [asansor_sayisi] tinyint DEFAULT ((0)) NULL;
END
GO
IF COL_LENGTH(N'dbo.oteller', N'kisa_aciklama') IS NULL
BEGIN
    ALTER TABLE [dbo].[oteller] ADD [kisa_aciklama] nvarchar(500) NULL;
END
GO
IF COL_LENGTH(N'dbo.oteller', N'uzun_aciklama') IS NULL
BEGIN
    ALTER TABLE [dbo].[oteller] ADD [uzun_aciklama] nvarchar(max) NULL;
END
GO
IF COL_LENGTH(N'dbo.oteller', N'konum_aciklamasi') IS NULL
BEGIN
    ALTER TABLE [dbo].[oteller] ADD [konum_aciklamasi] nvarchar(max) NULL;
END
GO
IF COL_LENGTH(N'dbo.oteller', N'komisyon_turu') IS NULL
BEGIN
    ALTER TABLE [dbo].[oteller] ADD [komisyon_turu] nvarchar(255) NULL;
END
GO
IF COL_LENGTH(N'dbo.oteller', N'varsayilan_komisyon_orani') IS NULL
BEGIN
    ALTER TABLE [dbo].[oteller] ADD [varsayilan_komisyon_orani] decimal(5,2) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.oteller', N'komisyon_hesaplama_tipi') IS NULL
BEGIN
    ALTER TABLE [dbo].[oteller] ADD [komisyon_hesaplama_tipi] nvarchar(255) NULL;
END
GO
IF COL_LENGTH(N'dbo.oteller', N'odeme_vadesi') IS NULL
BEGIN
    ALTER TABLE [dbo].[oteller] ADD [odeme_vadesi] nvarchar(255) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.oteller', N'odeme_yontemi') IS NULL
BEGIN
    ALTER TABLE [dbo].[oteller] ADD [odeme_yontemi] nvarchar(255) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.oteller', N'fatura_kesim_turu') IS NULL
BEGIN
    ALTER TABLE [dbo].[oteller] ADD [fatura_kesim_turu] nvarchar(255) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.oteller', N'depozito_tutari') IS NULL
BEGIN
    ALTER TABLE [dbo].[oteller] ADD [depozito_tutari] decimal(10,2) NULL;
END
GO
IF COL_LENGTH(N'dbo.oteller', N'depozito_iade_suresi') IS NULL
BEGIN
    ALTER TABLE [dbo].[oteller] ADD [depozito_iade_suresi] tinyint NULL;
END
GO
IF COL_LENGTH(N'dbo.oteller', N'minimum_konaklama_gecesi') IS NULL
BEGIN
    ALTER TABLE [dbo].[oteller] ADD [minimum_konaklama_gecesi] tinyint DEFAULT ((1)) NULL;
END
GO
IF COL_LENGTH(N'dbo.oteller', N'maksimum_konaklama_gecesi') IS NULL
BEGIN
    ALTER TABLE [dbo].[oteller] ADD [maksimum_konaklama_gecesi] smallint DEFAULT ((30)) NULL;
END
GO
IF COL_LENGTH(N'dbo.oteller', N'konusulan_diller') IS NULL
BEGIN
    ALTER TABLE [dbo].[oteller] ADD [konusulan_diller] nvarchar(255) NULL;
END
GO
IF COL_LENGTH(N'dbo.oteller', N'ortalama_puan') IS NULL
BEGIN
    ALTER TABLE [dbo].[oteller] ADD [ortalama_puan] decimal(3,2) DEFAULT ((0.00)) NULL;
END
GO
IF COL_LENGTH(N'dbo.oteller', N'toplam_yorum_sayisi') IS NULL
BEGIN
    ALTER TABLE [dbo].[oteller] ADD [toplam_yorum_sayisi] int DEFAULT ((0)) NULL;
END
GO
IF COL_LENGTH(N'dbo.oteller', N'temizlik_puani') IS NULL
BEGIN
    ALTER TABLE [dbo].[oteller] ADD [temizlik_puani] decimal(3,2) DEFAULT ((0.00)) NULL;
END
GO
IF COL_LENGTH(N'dbo.oteller', N'konfor_puani') IS NULL
BEGIN
    ALTER TABLE [dbo].[oteller] ADD [konfor_puani] decimal(3,2) DEFAULT ((0.00)) NULL;
END
GO
IF COL_LENGTH(N'dbo.oteller', N'konum_puani') IS NULL
BEGIN
    ALTER TABLE [dbo].[oteller] ADD [konum_puani] decimal(3,2) DEFAULT ((0.00)) NULL;
END
GO
IF COL_LENGTH(N'dbo.oteller', N'personel_puani') IS NULL
BEGIN
    ALTER TABLE [dbo].[oteller] ADD [personel_puani] decimal(3,2) DEFAULT ((0.00)) NULL;
END
GO
IF COL_LENGTH(N'dbo.oteller', N'fiyat_performans_puani') IS NULL
BEGIN
    ALTER TABLE [dbo].[oteller] ADD [fiyat_performans_puani] decimal(3,2) DEFAULT ((0.00)) NULL;
END
GO
IF COL_LENGTH(N'dbo.oteller', N'kapak_fotografi') IS NULL
BEGIN
    ALTER TABLE [dbo].[oteller] ADD [kapak_fotografi] nvarchar(255) NULL;
END
GO
IF COL_LENGTH(N'dbo.oteller', N'galeri') IS NULL
BEGIN
    ALTER TABLE [dbo].[oteller] ADD [galeri] nvarchar(max) NULL;
END
GO
IF COL_LENGTH(N'dbo.oteller', N'video_url') IS NULL
BEGIN
    ALTER TABLE [dbo].[oteller] ADD [video_url] nvarchar(255) NULL;
END
GO
IF COL_LENGTH(N'dbo.oteller', N'sanal_tur_url') IS NULL
BEGIN
    ALTER TABLE [dbo].[oteller] ADD [sanal_tur_url] nvarchar(255) NULL;
END
GO
IF COL_LENGTH(N'dbo.oteller', N'yayin_durumu') IS NULL
BEGIN
    ALTER TABLE [dbo].[oteller] ADD [yayin_durumu] nvarchar(255) NULL;
END
GO
IF COL_LENGTH(N'dbo.oteller', N'onay_durumu') IS NULL
BEGIN
    ALTER TABLE [dbo].[oteller] ADD [onay_durumu] nvarchar(255) NULL;
END
GO
IF COL_LENGTH(N'dbo.oteller', N'onay_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[oteller] ADD [onay_tarihi] datetime2(0) NULL;
END
GO
IF COL_LENGTH(N'dbo.oteller', N'onaylayan_admin_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[oteller] ADD [onaylayan_admin_id] bigint NULL;
END
GO
IF COL_LENGTH(N'dbo.oteller', N'populerlik_sirasi') IS NULL
BEGIN
    ALTER TABLE [dbo].[oteller] ADD [populerlik_sirasi] int DEFAULT ((0)) NULL;
END
GO
IF COL_LENGTH(N'dbo.oteller', N'one_cikan_otel') IS NULL
BEGIN
    ALTER TABLE [dbo].[oteller] ADD [one_cikan_otel] bit DEFAULT ((0)) NULL;
END
GO
IF COL_LENGTH(N'dbo.oteller', N'tavsiye_edilen_otel') IS NULL
BEGIN
    ALTER TABLE [dbo].[oteller] ADD [tavsiye_edilen_otel] bit DEFAULT ((0)) NULL;
END
GO
IF COL_LENGTH(N'dbo.oteller', N'olusturulma_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[oteller] ADD [olusturulma_tarihi] datetime2(0) DEFAULT (sysutcdatetime()) NULL;
END
GO
IF COL_LENGTH(N'dbo.oteller', N'guncellenme_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[oteller] ADD [guncellenme_tarihi] datetime2(0) NULL;
END
GO
IF COL_LENGTH(N'dbo.oteller', N'partner_ceza_bitis_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[oteller] ADD [partner_ceza_bitis_tarihi] datetime2(0) NULL;
END
GO
IF COL_LENGTH(N'dbo.oteller', N'otel_tipi_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[oteller] ADD [otel_tipi_id] int NULL;
END
GO
