SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.otel_kosullari', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[otel_kosullari]
    (
        [id] bigint IDENTITY(1,1) NOT NULL,
        [otel_id] bigint NOT NULL,
        [sigara_politikasi] nvarchar(255) NULL,
        [evcil_hayvan_politikasi] nvarchar(255) NULL,
        [evcil_hayvan_ucreti] decimal(10,2) NULL,
        [evcil_hayvan_depozitosu] decimal(10,2) NULL,
        [parti_etkinlik_izin] bit CONSTRAINT [DF__otel_kosu__parti__0B27A5C0] DEFAULT ((0)) NULL,
        [sessizlik_saatleri_baslangic] time(0) NULL,
        [sessizlik_saatleri_bitis] time(0) NULL,
        [minimum_yas_siniri] tinyint NULL,
        [sadece_yetiskinlere_mi] bit CONSTRAINT [DF__otel_kosu__sadec__0C1BC9F9] DEFAULT ((0)) NULL,
        [cocuk_kabul_yas_araligi] nvarchar(20) NULL,
        [bebek_karyolasi_var_mi] bit CONSTRAINT [DF__otel_kosu__bebek__0D0FEE32] DEFAULT ((0)) NULL,
        [bebek_karyolasi_ucreti] decimal(10,2) NULL,
        [ekstra_yatak_var_mi] bit CONSTRAINT [DF__otel_kosu__ekstr__0E04126B] DEFAULT ((0)) NULL,
        [ekstra_yatak_ucreti] decimal(10,2) NULL,
        [maksimum_cocuk_sayisi] tinyint NULL,
        [on_odeme_gerekli_mi] bit CONSTRAINT [DF__otel_kosu__on_od__0EF836A4] DEFAULT ((1)) NULL,
        [on_odeme_orani] decimal(5,2) CONSTRAINT [DF__otel_kosu__on_od__0FEC5ADD] DEFAULT ((30.00)) NULL,
        [kalan_odeme_zamani] nvarchar(255) NULL,
        [kredi_karti_ile_odeme_kabul] bit CONSTRAINT [DF__otel_kosu__kredi__10E07F16] DEFAULT ((1)) NULL,
        [nakit_odeme_kabul] bit CONSTRAINT [DF__otel_kosu__nakit__11D4A34F] DEFAULT ((0)) NULL,
        [kabul_edilen_kartlar] nvarchar(255) NULL,
        [iptal_politikasi_ozet] nvarchar(500) NULL,
        [detayli_iptal_kosullari] nvarchar(max) NULL,
        [ucretsiz_iptal_suresi] tinyint NULL,
        [gec_iptal_ceza_orani] decimal(5,2) NULL,
        [no_show_ceza_orani] decimal(5,2) CONSTRAINT [DF__otel_kosu__no_sh__12C8C788] DEFAULT ((100.00)) NULL,
        [hasar_depozitosu_tutari] decimal(10,2) NULL,
        [hasar_depozitosu_aciklamasi] nvarchar(255) NULL,
        [disaridan_yiyecek_icecek_serbest_mi] bit CONSTRAINT [DF__otel_kosu__disar__13BCEBC1] DEFAULT ((1)) NULL,
        [ziyaretci_kabul_edilir_mi] bit CONSTRAINT [DF__otel_kosu__ziyar__14B10FFA] DEFAULT ((0)) NULL,
        [ziyaretci_saati_baslangic] time(0) NULL,
        [ziyaretci_saati_bitis] time(0) NULL,
        [ozel_kosullar] nvarchar(max) NULL,
        [guncellenme_tarihi] datetime2(0) NULL,
        CONSTRAINT [PK_otel_kosullari] PRIMARY KEY CLUSTERED ([id] ASC)
    );
END
GO

-- Missing column repair blocks for existing databases.
IF COL_LENGTH(N'dbo.otel_kosullari', N'id') IS NULL
BEGIN
    ALTER TABLE [dbo].[otel_kosullari] ADD [id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.otel_kosullari', N'otel_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[otel_kosullari] ADD [otel_id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.otel_kosullari', N'sigara_politikasi') IS NULL
BEGIN
    ALTER TABLE [dbo].[otel_kosullari] ADD [sigara_politikasi] nvarchar(255) NULL;
END
GO
IF COL_LENGTH(N'dbo.otel_kosullari', N'evcil_hayvan_politikasi') IS NULL
BEGIN
    ALTER TABLE [dbo].[otel_kosullari] ADD [evcil_hayvan_politikasi] nvarchar(255) NULL;
END
GO
IF COL_LENGTH(N'dbo.otel_kosullari', N'evcil_hayvan_ucreti') IS NULL
BEGIN
    ALTER TABLE [dbo].[otel_kosullari] ADD [evcil_hayvan_ucreti] decimal(10,2) NULL;
END
GO
IF COL_LENGTH(N'dbo.otel_kosullari', N'evcil_hayvan_depozitosu') IS NULL
BEGIN
    ALTER TABLE [dbo].[otel_kosullari] ADD [evcil_hayvan_depozitosu] decimal(10,2) NULL;
END
GO
IF COL_LENGTH(N'dbo.otel_kosullari', N'parti_etkinlik_izin') IS NULL
BEGIN
    ALTER TABLE [dbo].[otel_kosullari] ADD [parti_etkinlik_izin] bit DEFAULT ((0)) NULL;
END
GO
IF COL_LENGTH(N'dbo.otel_kosullari', N'sessizlik_saatleri_baslangic') IS NULL
BEGIN
    ALTER TABLE [dbo].[otel_kosullari] ADD [sessizlik_saatleri_baslangic] time(0) NULL;
END
GO
IF COL_LENGTH(N'dbo.otel_kosullari', N'sessizlik_saatleri_bitis') IS NULL
BEGIN
    ALTER TABLE [dbo].[otel_kosullari] ADD [sessizlik_saatleri_bitis] time(0) NULL;
END
GO
IF COL_LENGTH(N'dbo.otel_kosullari', N'minimum_yas_siniri') IS NULL
BEGIN
    ALTER TABLE [dbo].[otel_kosullari] ADD [minimum_yas_siniri] tinyint NULL;
END
GO
IF COL_LENGTH(N'dbo.otel_kosullari', N'sadece_yetiskinlere_mi') IS NULL
BEGIN
    ALTER TABLE [dbo].[otel_kosullari] ADD [sadece_yetiskinlere_mi] bit DEFAULT ((0)) NULL;
END
GO
IF COL_LENGTH(N'dbo.otel_kosullari', N'cocuk_kabul_yas_araligi') IS NULL
BEGIN
    ALTER TABLE [dbo].[otel_kosullari] ADD [cocuk_kabul_yas_araligi] nvarchar(20) NULL;
END
GO
IF COL_LENGTH(N'dbo.otel_kosullari', N'bebek_karyolasi_var_mi') IS NULL
BEGIN
    ALTER TABLE [dbo].[otel_kosullari] ADD [bebek_karyolasi_var_mi] bit DEFAULT ((0)) NULL;
END
GO
IF COL_LENGTH(N'dbo.otel_kosullari', N'bebek_karyolasi_ucreti') IS NULL
BEGIN
    ALTER TABLE [dbo].[otel_kosullari] ADD [bebek_karyolasi_ucreti] decimal(10,2) NULL;
END
GO
IF COL_LENGTH(N'dbo.otel_kosullari', N'ekstra_yatak_var_mi') IS NULL
BEGIN
    ALTER TABLE [dbo].[otel_kosullari] ADD [ekstra_yatak_var_mi] bit DEFAULT ((0)) NULL;
END
GO
IF COL_LENGTH(N'dbo.otel_kosullari', N'ekstra_yatak_ucreti') IS NULL
BEGIN
    ALTER TABLE [dbo].[otel_kosullari] ADD [ekstra_yatak_ucreti] decimal(10,2) NULL;
END
GO
IF COL_LENGTH(N'dbo.otel_kosullari', N'maksimum_cocuk_sayisi') IS NULL
BEGIN
    ALTER TABLE [dbo].[otel_kosullari] ADD [maksimum_cocuk_sayisi] tinyint NULL;
END
GO
IF COL_LENGTH(N'dbo.otel_kosullari', N'on_odeme_gerekli_mi') IS NULL
BEGIN
    ALTER TABLE [dbo].[otel_kosullari] ADD [on_odeme_gerekli_mi] bit DEFAULT ((1)) NULL;
END
GO
IF COL_LENGTH(N'dbo.otel_kosullari', N'on_odeme_orani') IS NULL
BEGIN
    ALTER TABLE [dbo].[otel_kosullari] ADD [on_odeme_orani] decimal(5,2) DEFAULT ((30.00)) NULL;
END
GO
IF COL_LENGTH(N'dbo.otel_kosullari', N'kalan_odeme_zamani') IS NULL
BEGIN
    ALTER TABLE [dbo].[otel_kosullari] ADD [kalan_odeme_zamani] nvarchar(255) NULL;
END
GO
IF COL_LENGTH(N'dbo.otel_kosullari', N'kredi_karti_ile_odeme_kabul') IS NULL
BEGIN
    ALTER TABLE [dbo].[otel_kosullari] ADD [kredi_karti_ile_odeme_kabul] bit DEFAULT ((1)) NULL;
END
GO
IF COL_LENGTH(N'dbo.otel_kosullari', N'nakit_odeme_kabul') IS NULL
BEGIN
    ALTER TABLE [dbo].[otel_kosullari] ADD [nakit_odeme_kabul] bit DEFAULT ((0)) NULL;
END
GO
IF COL_LENGTH(N'dbo.otel_kosullari', N'kabul_edilen_kartlar') IS NULL
BEGIN
    ALTER TABLE [dbo].[otel_kosullari] ADD [kabul_edilen_kartlar] nvarchar(255) NULL;
END
GO
IF COL_LENGTH(N'dbo.otel_kosullari', N'iptal_politikasi_ozet') IS NULL
BEGIN
    ALTER TABLE [dbo].[otel_kosullari] ADD [iptal_politikasi_ozet] nvarchar(500) NULL;
END
GO
IF COL_LENGTH(N'dbo.otel_kosullari', N'detayli_iptal_kosullari') IS NULL
BEGIN
    ALTER TABLE [dbo].[otel_kosullari] ADD [detayli_iptal_kosullari] nvarchar(max) NULL;
END
GO
IF COL_LENGTH(N'dbo.otel_kosullari', N'ucretsiz_iptal_suresi') IS NULL
BEGIN
    ALTER TABLE [dbo].[otel_kosullari] ADD [ucretsiz_iptal_suresi] tinyint NULL;
END
GO
IF COL_LENGTH(N'dbo.otel_kosullari', N'gec_iptal_ceza_orani') IS NULL
BEGIN
    ALTER TABLE [dbo].[otel_kosullari] ADD [gec_iptal_ceza_orani] decimal(5,2) NULL;
END
GO
IF COL_LENGTH(N'dbo.otel_kosullari', N'no_show_ceza_orani') IS NULL
BEGIN
    ALTER TABLE [dbo].[otel_kosullari] ADD [no_show_ceza_orani] decimal(5,2) DEFAULT ((100.00)) NULL;
END
GO
IF COL_LENGTH(N'dbo.otel_kosullari', N'hasar_depozitosu_tutari') IS NULL
BEGIN
    ALTER TABLE [dbo].[otel_kosullari] ADD [hasar_depozitosu_tutari] decimal(10,2) NULL;
END
GO
IF COL_LENGTH(N'dbo.otel_kosullari', N'hasar_depozitosu_aciklamasi') IS NULL
BEGIN
    ALTER TABLE [dbo].[otel_kosullari] ADD [hasar_depozitosu_aciklamasi] nvarchar(255) NULL;
END
GO
IF COL_LENGTH(N'dbo.otel_kosullari', N'disaridan_yiyecek_icecek_serbest_mi') IS NULL
BEGIN
    ALTER TABLE [dbo].[otel_kosullari] ADD [disaridan_yiyecek_icecek_serbest_mi] bit DEFAULT ((1)) NULL;
END
GO
IF COL_LENGTH(N'dbo.otel_kosullari', N'ziyaretci_kabul_edilir_mi') IS NULL
BEGIN
    ALTER TABLE [dbo].[otel_kosullari] ADD [ziyaretci_kabul_edilir_mi] bit DEFAULT ((0)) NULL;
END
GO
IF COL_LENGTH(N'dbo.otel_kosullari', N'ziyaretci_saati_baslangic') IS NULL
BEGIN
    ALTER TABLE [dbo].[otel_kosullari] ADD [ziyaretci_saati_baslangic] time(0) NULL;
END
GO
IF COL_LENGTH(N'dbo.otel_kosullari', N'ziyaretci_saati_bitis') IS NULL
BEGIN
    ALTER TABLE [dbo].[otel_kosullari] ADD [ziyaretci_saati_bitis] time(0) NULL;
END
GO
IF COL_LENGTH(N'dbo.otel_kosullari', N'ozel_kosullar') IS NULL
BEGIN
    ALTER TABLE [dbo].[otel_kosullari] ADD [ozel_kosullar] nvarchar(max) NULL;
END
GO
IF COL_LENGTH(N'dbo.otel_kosullari', N'guncellenme_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[otel_kosullari] ADD [guncellenme_tarihi] datetime2(0) NULL;
END
GO
