SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.rezervasyonlar', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[rezervasyonlar]
    (
        [id] bigint IDENTITY(1,1) NOT NULL,
        [rezervasyon_no] nvarchar(20) NOT NULL,
        [otel_id] bigint NOT NULL,
        [oda_tip_id] bigint NOT NULL,
        [kullanici_id] bigint NOT NULL,
        [firma_id] bigint NULL,
        [firma_calisan_id] bigint NULL,
        [satis_temsilcisi_id] bigint NULL,
        [satis_musteri_id] bigint NULL,
        [rezervasyon_taslagi_id] bigint NULL,
        [misafir_ad_soyad] nvarchar(100) NOT NULL,
        [misafir_eposta] nvarchar(100) NOT NULL,
        [misafir_telefon] nvarchar(20) NOT NULL,
        [misafir_sehir] nvarchar(100) NULL,
        [misafir_ilce] nvarchar(100) NULL,
        [misafir_mahalle] nvarchar(120) NULL,
        [misafir_adres] nvarchar(max) NULL,
        [misafir_ulke] nvarchar(50) NULL,
        [misafir_notu] nvarchar(max) NULL,
        [giris_tarihi] date NOT NULL,
        [cikis_tarihi] date NOT NULL,
        [gece_sayisi] smallint NULL,
        [yetiskin_sayisi] tinyint NOT NULL,
        [cocuk_sayisi] tinyint CONSTRAINT [DF__rezervasy__cocuk__689D8392] DEFAULT ((0)) NULL,
        [bebek_sayisi] tinyint CONSTRAINT [DF__rezervasy__bebek__6991A7CB] DEFAULT ((0)) NULL,
        [cocuk_yaslari] nvarchar(max) NULL,
        [oda_sayisi] tinyint CONSTRAINT [DF__rezervasy__oda_s__6A85CC04] DEFAULT ((1)) NULL,
        [gecelik_fiyat] decimal(10,2) NOT NULL,
        [toplam_oda_tutari] decimal(10,2) NOT NULL,
        [ek_hizmet_tutari] decimal(10,2) CONSTRAINT [DF__rezervasy__ek_hi__6B79F03D] DEFAULT ((0.00)) NULL,
        [vergi_tutari] decimal(10,2) CONSTRAINT [DF__rezervasy__vergi__6C6E1476] DEFAULT ((0.00)) NULL,
        [indirim_tutari] decimal(10,2) CONSTRAINT [DF__rezervasy__indir__6D6238AF] DEFAULT ((0.00)) NULL,
        [toplam_tasarruf] decimal(10,2) CONSTRAINT [DF__rezervasy__topla__6E565CE8] DEFAULT ((0.00)) NOT NULL,
        [kupon_indirimi] decimal(10,2) CONSTRAINT [DF__rezervasy__kupon__6F4A8121] DEFAULT ((0.00)) NULL,
        [toplam_tutar] decimal(10,2) NOT NULL,
        [komisyon_orani] decimal(5,2) NOT NULL,
        [komisyon_tutari] decimal(10,2) NULL,
        [otele_odenecek_tutar] decimal(10,2) NULL,
        [para_birimi] nvarchar(3) NULL,
        [odeme_durumu] nvarchar(255) NULL,
        [odeme_yontemi] nvarchar(255) NULL,
        [odeme_tarihi] datetime2(0) NULL,
        [on_odeme_tutari] decimal(10,2) NULL,
        [kalan_odeme_tutari] decimal(10,2) NULL,
        [durum] nvarchar(255) NULL,
        [iptal_tarihi] datetime2(0) NULL,
        [iptal_nedeni] nvarchar(500) NULL,
        [iptal_eden] nvarchar(255) NULL,
        [iptal_kesintisi] decimal(10,2) NULL,
        [iade_tutari] decimal(10,2) NULL,
        [otel_onay_durumu] nvarchar(255) NULL,
        [firma_onay_durumu] nvarchar(255) NOT NULL,
        [firma_onaylayan_kullanici_id] bigint NULL,
        [satis_onaylayan_kullanici_id] bigint NULL,
        [satis_onay_tarihi] datetime2(0) NULL,
        [firma_onay_tarihi] datetime2(0) NULL,
        [otel_onay_tarihi] datetime2(0) NULL,
        [otel_red_nedeni] nvarchar(500) NULL,
        [erken_giris_talebi] bit CONSTRAINT [DF__rezervasy__erken__703EA55A] DEFAULT ((0)) NULL,
        [gec_cikis_talebi] bit CONSTRAINT [DF__rezervasy__gec_c__7132C993] DEFAULT ((0)) NULL,
        [transfer_talebi] bit CONSTRAINT [DF__rezervasy__trans__7226EDCC] DEFAULT ((0)) NULL,
        [ozel_istekler] nvarchar(max) NULL,
        [musteri_talep_notu] nvarchar(max) NULL,
        [kaynak] nvarchar(255) NULL,
        [rezervasyon_kanali] nvarchar(255) NULL,
        [kampanya_kodu] nvarchar(50) NULL,
        [referans_kodu] nvarchar(50) NULL,
        [olusturulma_tarihi] datetime2(0) CONSTRAINT [DF__rezervasy__olust__731B1205] DEFAULT (sysutcdatetime()) NULL,
        [guncellenme_tarihi] datetime2(0) NULL,
        [check_in_yapildi_mi] bit CONSTRAINT [DF__rezervasy__check__740F363E] DEFAULT ((0)) NULL,
        [check_in_tarihi] datetime2(0) NULL,
        [check_out_yapildi_mi] bit CONSTRAINT [DF__rezervasy__check__75035A77] DEFAULT ((0)) NULL,
        [check_out_tarihi] datetime2(0) NULL,
        [komisyon_vergi_kural_id] bigint NULL,
        [net_oda_tutari] decimal(18,2) NULL,
        [kdv_orani] decimal(5,2) NULL,
        [kdv_tutari] decimal(18,2) NULL,
        [konaklama_vergisi_orani] decimal(5,2) NULL,
        [konaklama_vergisi_tutari] decimal(18,2) NULL,
        [toplam_vergi_tutari] decimal(18,2) NULL,
        [komisyon_gelir_vergisi_orani] decimal(5,2) NULL,
        [komisyon_gelir_vergisi_tutari] decimal(18,2) NULL,
        [platform_net_komisyon_tutari] decimal(18,2) NULL,
        [kapida_odeme_tutari] decimal(18,2) NULL,
        [kapida_odeme_durumu] nvarchar(50) NULL,
        [online_odeme_tutari] decimal(18,2) NULL,
        [online_odeme_durumu] nvarchar(50) NULL,
        [tahsil_edilen_tutar] decimal(18,2) NULL,
        [kalan_tahsil_edilecek_tutar] decimal(18,2) NULL,
        [vergiler_dahil_toplam_tutar] decimal(18,2) NULL,
        [odeme_referans_no] nvarchar(100) NULL,
        [muhasebe_notu] nvarchar(500) NULL,
        [odeme_durumu_id] bigint NULL,
        [havale_eft_bekleyen_tutari] decimal(18,2) NULL,
        [rezervasyon_durumu_id] bigint NULL,
        [durum_ozel_veri] nvarchar(max) NULL,
        CONSTRAINT [PK_rezervasyonlar] PRIMARY KEY CLUSTERED ([id] ASC)
    );
END
GO

-- Missing column repair blocks for existing databases.
IF COL_LENGTH(N'dbo.rezervasyonlar', N'id') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyonlar] ADD [id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.rezervasyonlar', N'rezervasyon_no') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyonlar] ADD [rezervasyon_no] nvarchar(20) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.rezervasyonlar', N'otel_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyonlar] ADD [otel_id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.rezervasyonlar', N'oda_tip_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyonlar] ADD [oda_tip_id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.rezervasyonlar', N'kullanici_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyonlar] ADD [kullanici_id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.rezervasyonlar', N'firma_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyonlar] ADD [firma_id] bigint NULL;
END
GO
IF COL_LENGTH(N'dbo.rezervasyonlar', N'firma_calisan_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyonlar] ADD [firma_calisan_id] bigint NULL;
END
GO
IF COL_LENGTH(N'dbo.rezervasyonlar', N'satis_temsilcisi_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyonlar] ADD [satis_temsilcisi_id] bigint NULL;
END
GO
IF COL_LENGTH(N'dbo.rezervasyonlar', N'satis_musteri_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyonlar] ADD [satis_musteri_id] bigint NULL;
END
GO
IF COL_LENGTH(N'dbo.rezervasyonlar', N'rezervasyon_taslagi_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyonlar] ADD [rezervasyon_taslagi_id] bigint NULL;
END
GO
IF COL_LENGTH(N'dbo.rezervasyonlar', N'misafir_ad_soyad') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyonlar] ADD [misafir_ad_soyad] nvarchar(100) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.rezervasyonlar', N'misafir_eposta') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyonlar] ADD [misafir_eposta] nvarchar(100) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.rezervasyonlar', N'misafir_telefon') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyonlar] ADD [misafir_telefon] nvarchar(20) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.rezervasyonlar', N'misafir_sehir') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyonlar] ADD [misafir_sehir] nvarchar(100) NULL;
END
GO
IF COL_LENGTH(N'dbo.rezervasyonlar', N'misafir_ilce') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyonlar] ADD [misafir_ilce] nvarchar(100) NULL;
END
GO
IF COL_LENGTH(N'dbo.rezervasyonlar', N'misafir_mahalle') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyonlar] ADD [misafir_mahalle] nvarchar(120) NULL;
END
GO
IF COL_LENGTH(N'dbo.rezervasyonlar', N'misafir_adres') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyonlar] ADD [misafir_adres] nvarchar(max) NULL;
END
GO
IF COL_LENGTH(N'dbo.rezervasyonlar', N'misafir_ulke') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyonlar] ADD [misafir_ulke] nvarchar(50) NULL;
END
GO
IF COL_LENGTH(N'dbo.rezervasyonlar', N'misafir_notu') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyonlar] ADD [misafir_notu] nvarchar(max) NULL;
END
GO
IF COL_LENGTH(N'dbo.rezervasyonlar', N'giris_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyonlar] ADD [giris_tarihi] date NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.rezervasyonlar', N'cikis_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyonlar] ADD [cikis_tarihi] date NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.rezervasyonlar', N'gece_sayisi') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyonlar] ADD [gece_sayisi] smallint NULL;
END
GO
IF COL_LENGTH(N'dbo.rezervasyonlar', N'yetiskin_sayisi') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyonlar] ADD [yetiskin_sayisi] tinyint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.rezervasyonlar', N'cocuk_sayisi') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyonlar] ADD [cocuk_sayisi] tinyint DEFAULT ((0)) NULL;
END
GO
IF COL_LENGTH(N'dbo.rezervasyonlar', N'bebek_sayisi') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyonlar] ADD [bebek_sayisi] tinyint DEFAULT ((0)) NULL;
END
GO
IF COL_LENGTH(N'dbo.rezervasyonlar', N'cocuk_yaslari') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyonlar] ADD [cocuk_yaslari] nvarchar(max) NULL;
END
GO
IF COL_LENGTH(N'dbo.rezervasyonlar', N'oda_sayisi') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyonlar] ADD [oda_sayisi] tinyint DEFAULT ((1)) NULL;
END
GO
IF COL_LENGTH(N'dbo.rezervasyonlar', N'gecelik_fiyat') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyonlar] ADD [gecelik_fiyat] decimal(10,2) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.rezervasyonlar', N'toplam_oda_tutari') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyonlar] ADD [toplam_oda_tutari] decimal(10,2) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.rezervasyonlar', N'ek_hizmet_tutari') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyonlar] ADD [ek_hizmet_tutari] decimal(10,2) DEFAULT ((0.00)) NULL;
END
GO
IF COL_LENGTH(N'dbo.rezervasyonlar', N'vergi_tutari') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyonlar] ADD [vergi_tutari] decimal(10,2) DEFAULT ((0.00)) NULL;
END
GO
IF COL_LENGTH(N'dbo.rezervasyonlar', N'indirim_tutari') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyonlar] ADD [indirim_tutari] decimal(10,2) DEFAULT ((0.00)) NULL;
END
GO
IF COL_LENGTH(N'dbo.rezervasyonlar', N'toplam_tasarruf') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyonlar] ADD [toplam_tasarruf] decimal(10,2) DEFAULT ((0.00)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.rezervasyonlar', N'kupon_indirimi') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyonlar] ADD [kupon_indirimi] decimal(10,2) DEFAULT ((0.00)) NULL;
END
GO
IF COL_LENGTH(N'dbo.rezervasyonlar', N'toplam_tutar') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyonlar] ADD [toplam_tutar] decimal(10,2) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.rezervasyonlar', N'komisyon_orani') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyonlar] ADD [komisyon_orani] decimal(5,2) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.rezervasyonlar', N'komisyon_tutari') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyonlar] ADD [komisyon_tutari] decimal(10,2) NULL;
END
GO
IF COL_LENGTH(N'dbo.rezervasyonlar', N'otele_odenecek_tutar') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyonlar] ADD [otele_odenecek_tutar] decimal(10,2) NULL;
END
GO
IF COL_LENGTH(N'dbo.rezervasyonlar', N'para_birimi') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyonlar] ADD [para_birimi] nvarchar(3) NULL;
END
GO
IF COL_LENGTH(N'dbo.rezervasyonlar', N'odeme_durumu') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyonlar] ADD [odeme_durumu] nvarchar(255) NULL;
END
GO
IF COL_LENGTH(N'dbo.rezervasyonlar', N'odeme_yontemi') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyonlar] ADD [odeme_yontemi] nvarchar(255) NULL;
END
GO
IF COL_LENGTH(N'dbo.rezervasyonlar', N'odeme_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyonlar] ADD [odeme_tarihi] datetime2(0) NULL;
END
GO
IF COL_LENGTH(N'dbo.rezervasyonlar', N'on_odeme_tutari') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyonlar] ADD [on_odeme_tutari] decimal(10,2) NULL;
END
GO
IF COL_LENGTH(N'dbo.rezervasyonlar', N'kalan_odeme_tutari') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyonlar] ADD [kalan_odeme_tutari] decimal(10,2) NULL;
END
GO
IF COL_LENGTH(N'dbo.rezervasyonlar', N'durum') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyonlar] ADD [durum] nvarchar(255) NULL;
END
GO
IF COL_LENGTH(N'dbo.rezervasyonlar', N'iptal_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyonlar] ADD [iptal_tarihi] datetime2(0) NULL;
END
GO
IF COL_LENGTH(N'dbo.rezervasyonlar', N'iptal_nedeni') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyonlar] ADD [iptal_nedeni] nvarchar(500) NULL;
END
GO
IF COL_LENGTH(N'dbo.rezervasyonlar', N'iptal_eden') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyonlar] ADD [iptal_eden] nvarchar(255) NULL;
END
GO
IF COL_LENGTH(N'dbo.rezervasyonlar', N'iptal_kesintisi') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyonlar] ADD [iptal_kesintisi] decimal(10,2) NULL;
END
GO
IF COL_LENGTH(N'dbo.rezervasyonlar', N'iade_tutari') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyonlar] ADD [iade_tutari] decimal(10,2) NULL;
END
GO
IF COL_LENGTH(N'dbo.rezervasyonlar', N'otel_onay_durumu') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyonlar] ADD [otel_onay_durumu] nvarchar(255) NULL;
END
GO
IF COL_LENGTH(N'dbo.rezervasyonlar', N'firma_onay_durumu') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyonlar] ADD [firma_onay_durumu] nvarchar(255) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.rezervasyonlar', N'firma_onaylayan_kullanici_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyonlar] ADD [firma_onaylayan_kullanici_id] bigint NULL;
END
GO
IF COL_LENGTH(N'dbo.rezervasyonlar', N'satis_onaylayan_kullanici_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyonlar] ADD [satis_onaylayan_kullanici_id] bigint NULL;
END
GO
IF COL_LENGTH(N'dbo.rezervasyonlar', N'satis_onay_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyonlar] ADD [satis_onay_tarihi] datetime2(0) NULL;
END
GO
IF COL_LENGTH(N'dbo.rezervasyonlar', N'firma_onay_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyonlar] ADD [firma_onay_tarihi] datetime2(0) NULL;
END
GO
IF COL_LENGTH(N'dbo.rezervasyonlar', N'otel_onay_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyonlar] ADD [otel_onay_tarihi] datetime2(0) NULL;
END
GO
IF COL_LENGTH(N'dbo.rezervasyonlar', N'otel_red_nedeni') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyonlar] ADD [otel_red_nedeni] nvarchar(500) NULL;
END
GO
IF COL_LENGTH(N'dbo.rezervasyonlar', N'erken_giris_talebi') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyonlar] ADD [erken_giris_talebi] bit DEFAULT ((0)) NULL;
END
GO
IF COL_LENGTH(N'dbo.rezervasyonlar', N'gec_cikis_talebi') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyonlar] ADD [gec_cikis_talebi] bit DEFAULT ((0)) NULL;
END
GO
IF COL_LENGTH(N'dbo.rezervasyonlar', N'transfer_talebi') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyonlar] ADD [transfer_talebi] bit DEFAULT ((0)) NULL;
END
GO
IF COL_LENGTH(N'dbo.rezervasyonlar', N'ozel_istekler') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyonlar] ADD [ozel_istekler] nvarchar(max) NULL;
END
GO
IF COL_LENGTH(N'dbo.rezervasyonlar', N'musteri_talep_notu') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyonlar] ADD [musteri_talep_notu] nvarchar(max) NULL;
END
GO
IF COL_LENGTH(N'dbo.rezervasyonlar', N'kaynak') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyonlar] ADD [kaynak] nvarchar(255) NULL;
END
GO
IF COL_LENGTH(N'dbo.rezervasyonlar', N'rezervasyon_kanali') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyonlar] ADD [rezervasyon_kanali] nvarchar(255) NULL;
END
GO
IF COL_LENGTH(N'dbo.rezervasyonlar', N'kampanya_kodu') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyonlar] ADD [kampanya_kodu] nvarchar(50) NULL;
END
GO
IF COL_LENGTH(N'dbo.rezervasyonlar', N'referans_kodu') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyonlar] ADD [referans_kodu] nvarchar(50) NULL;
END
GO
IF COL_LENGTH(N'dbo.rezervasyonlar', N'olusturulma_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyonlar] ADD [olusturulma_tarihi] datetime2(0) DEFAULT (sysutcdatetime()) NULL;
END
GO
IF COL_LENGTH(N'dbo.rezervasyonlar', N'guncellenme_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyonlar] ADD [guncellenme_tarihi] datetime2(0) NULL;
END
GO
IF COL_LENGTH(N'dbo.rezervasyonlar', N'check_in_yapildi_mi') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyonlar] ADD [check_in_yapildi_mi] bit DEFAULT ((0)) NULL;
END
GO
IF COL_LENGTH(N'dbo.rezervasyonlar', N'check_in_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyonlar] ADD [check_in_tarihi] datetime2(0) NULL;
END
GO
IF COL_LENGTH(N'dbo.rezervasyonlar', N'check_out_yapildi_mi') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyonlar] ADD [check_out_yapildi_mi] bit DEFAULT ((0)) NULL;
END
GO
IF COL_LENGTH(N'dbo.rezervasyonlar', N'check_out_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyonlar] ADD [check_out_tarihi] datetime2(0) NULL;
END
GO
IF COL_LENGTH(N'dbo.rezervasyonlar', N'komisyon_vergi_kural_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyonlar] ADD [komisyon_vergi_kural_id] bigint NULL;
END
GO
IF COL_LENGTH(N'dbo.rezervasyonlar', N'net_oda_tutari') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyonlar] ADD [net_oda_tutari] decimal(18,2) NULL;
END
GO
IF COL_LENGTH(N'dbo.rezervasyonlar', N'kdv_orani') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyonlar] ADD [kdv_orani] decimal(5,2) NULL;
END
GO
IF COL_LENGTH(N'dbo.rezervasyonlar', N'kdv_tutari') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyonlar] ADD [kdv_tutari] decimal(18,2) NULL;
END
GO
IF COL_LENGTH(N'dbo.rezervasyonlar', N'konaklama_vergisi_orani') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyonlar] ADD [konaklama_vergisi_orani] decimal(5,2) NULL;
END
GO
IF COL_LENGTH(N'dbo.rezervasyonlar', N'konaklama_vergisi_tutari') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyonlar] ADD [konaklama_vergisi_tutari] decimal(18,2) NULL;
END
GO
IF COL_LENGTH(N'dbo.rezervasyonlar', N'toplam_vergi_tutari') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyonlar] ADD [toplam_vergi_tutari] decimal(18,2) NULL;
END
GO
IF COL_LENGTH(N'dbo.rezervasyonlar', N'komisyon_gelir_vergisi_orani') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyonlar] ADD [komisyon_gelir_vergisi_orani] decimal(5,2) NULL;
END
GO
IF COL_LENGTH(N'dbo.rezervasyonlar', N'komisyon_gelir_vergisi_tutari') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyonlar] ADD [komisyon_gelir_vergisi_tutari] decimal(18,2) NULL;
END
GO
IF COL_LENGTH(N'dbo.rezervasyonlar', N'platform_net_komisyon_tutari') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyonlar] ADD [platform_net_komisyon_tutari] decimal(18,2) NULL;
END
GO
IF COL_LENGTH(N'dbo.rezervasyonlar', N'kapida_odeme_tutari') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyonlar] ADD [kapida_odeme_tutari] decimal(18,2) NULL;
END
GO
IF COL_LENGTH(N'dbo.rezervasyonlar', N'kapida_odeme_durumu') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyonlar] ADD [kapida_odeme_durumu] nvarchar(50) NULL;
END
GO
IF COL_LENGTH(N'dbo.rezervasyonlar', N'online_odeme_tutari') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyonlar] ADD [online_odeme_tutari] decimal(18,2) NULL;
END
GO
IF COL_LENGTH(N'dbo.rezervasyonlar', N'online_odeme_durumu') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyonlar] ADD [online_odeme_durumu] nvarchar(50) NULL;
END
GO
IF COL_LENGTH(N'dbo.rezervasyonlar', N'tahsil_edilen_tutar') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyonlar] ADD [tahsil_edilen_tutar] decimal(18,2) NULL;
END
GO
IF COL_LENGTH(N'dbo.rezervasyonlar', N'kalan_tahsil_edilecek_tutar') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyonlar] ADD [kalan_tahsil_edilecek_tutar] decimal(18,2) NULL;
END
GO
IF COL_LENGTH(N'dbo.rezervasyonlar', N'vergiler_dahil_toplam_tutar') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyonlar] ADD [vergiler_dahil_toplam_tutar] decimal(18,2) NULL;
END
GO
IF COL_LENGTH(N'dbo.rezervasyonlar', N'odeme_referans_no') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyonlar] ADD [odeme_referans_no] nvarchar(100) NULL;
END
GO
IF COL_LENGTH(N'dbo.rezervasyonlar', N'muhasebe_notu') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyonlar] ADD [muhasebe_notu] nvarchar(500) NULL;
END
GO
IF COL_LENGTH(N'dbo.rezervasyonlar', N'odeme_durumu_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyonlar] ADD [odeme_durumu_id] bigint NULL;
END
GO
IF COL_LENGTH(N'dbo.rezervasyonlar', N'havale_eft_bekleyen_tutari') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyonlar] ADD [havale_eft_bekleyen_tutari] decimal(18,2) NULL;
END
GO
IF COL_LENGTH(N'dbo.rezervasyonlar', N'rezervasyon_durumu_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyonlar] ADD [rezervasyon_durumu_id] bigint NULL;
END
GO
IF COL_LENGTH(N'dbo.rezervasyonlar', N'durum_ozel_veri') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyonlar] ADD [durum_ozel_veri] nvarchar(max) NULL;
END
GO
