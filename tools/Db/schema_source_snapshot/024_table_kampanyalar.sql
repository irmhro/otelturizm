SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.kampanyalar', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[kampanyalar]
    (
        [id] bigint IDENTITY(1,1) NOT NULL,
        [kampanya_kodu] nvarchar(50) NOT NULL,
        [kampanya_adi] nvarchar(200) NOT NULL,
        [seo_slug] nvarchar(255) NULL,
        [sayfa_url] nvarchar(255) NULL,
        [kampanya_aciklamasi] nvarchar(max) NULL,
        [kisa_aciklama] nvarchar(255) NULL,
        [detay_aciklama] nvarchar(max) NULL,
        [tur] nvarchar(255) NOT NULL,
        [indirim_orani] decimal(5,2) NULL,
        [indirim_tutari] decimal(10,2) NULL,
        [maksimum_indirim_tutari] decimal(10,2) NULL,
        [minimum_sepet_tutari] decimal(10,2) NULL,
        [hedef_otel_turu] nvarchar(255) NULL,
        [hedef_otel_idleri] nvarchar(max) NULL,
        [hedef_sehirler] nvarchar(max) NULL,
        [hedef_ilceler] nvarchar(max) NULL,
        [hedef_mahalleler] nvarchar(max) NULL,
        [hedef_kullanici_turu] nvarchar(255) NULL,
        [minimum_gecmis_rezervasyon] tinyint NULL,
        [baslangic_tarihi] datetime2(0) NOT NULL,
        [bitis_tarihi] datetime2(0) NOT NULL,
        [rezervasyon_tarih_araligi_baslangic] date NULL,
        [rezervasyon_tarih_araligi_bitis] date NULL,
        [konaklama_tarih_araligi_baslangic] date NULL,
        [konaklama_tarih_araligi_bitis] date NULL,
        [minimum_geceleme] tinyint CONSTRAINT [DF__kampanyal__minim__55F4C372] DEFAULT ((1)) NULL,
        [maksimum_geceleme] smallint NULL,
        [erken_rezervasyon_gun_sayisi] smallint NULL,
        [toplam_kullanim_limiti] int NULL,
        [kullanici_basina_limit] tinyint CONSTRAINT [DF__kampanyal__kulla__56E8E7AB] DEFAULT ((1)) NULL,
        [kullanilan_adet] int CONSTRAINT [DF__kampanyal__kulla__57DD0BE4] DEFAULT ((0)) NULL,
        [gosterim_adedi] int CONSTRAINT [DF__kampanyal__goste__58D1301D] DEFAULT ((0)) NOT NULL,
        [aktif_mi] bit CONSTRAINT [DF__kampanyal__aktif__59C55456] DEFAULT ((1)) NULL,
        [gorunurluk_durumu] nvarchar(255) NOT NULL,
        [partner_katilim_acik] bit CONSTRAINT [DF__kampanyal__partn__5AB9788F] DEFAULT ((1)) NOT NULL,
        [partner_katilim_baslangic] datetime2(0) NULL,
        [partner_katilim_bitis] datetime2(0) NULL,
        [otomatik_sona_ersin] bit CONSTRAINT [DF__kampanyal__otoma__5BAD9CC8] DEFAULT ((1)) NOT NULL,
        [one_cikan_kampanya] bit CONSTRAINT [DF__kampanyal__one_c__5CA1C101] DEFAULT ((0)) NULL,
        [siralama] int CONSTRAINT [DF__kampanyal__siral__5D95E53A] DEFAULT ((0)) NOT NULL,
        [aktif_sayfa_vitrini] bit CONSTRAINT [DF__kampanyal__aktif__5E8A0973] DEFAULT ((0)) NOT NULL,
        [banner_gorseli] nvarchar(255) NULL,
        [hero_gorseli] nvarchar(500) NULL,
        [kart_gorseli] nvarchar(500) NULL,
        [mobil_gorsel] nvarchar(500) NULL,
        [meta_title] nvarchar(255) NULL,
        [meta_description] nvarchar(500) NULL,
        [canonical_url] nvarchar(500) NULL,
        [kampanya_etiketi] nvarchar(100) NULL,
        [promo_badge] nvarchar(100) NULL,
        [kampanya_renk_kodu] nvarchar(20) NULL,
        [listeleme_basligi] nvarchar(255) NULL,
        [listeleme_aciklamasi] nvarchar(500) NULL,
        [kullanim_kosullari] nvarchar(max) NULL,
        [olusturan_admin_id] bigint NULL,
        [olusturulma_tarihi] datetime2(0) CONSTRAINT [DF__kampanyal__olust__5F7E2DAC] DEFAULT (sysutcdatetime()) NULL,
        [guncellenme_tarihi] datetime2(0) NULL,
        CONSTRAINT [PK_kampanyalar] PRIMARY KEY CLUSTERED ([id] ASC)
    );
END
GO

-- Missing column repair blocks for existing databases.
IF COL_LENGTH(N'dbo.kampanyalar', N'id') IS NULL
BEGIN
    ALTER TABLE [dbo].[kampanyalar] ADD [id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.kampanyalar', N'kampanya_kodu') IS NULL
BEGIN
    ALTER TABLE [dbo].[kampanyalar] ADD [kampanya_kodu] nvarchar(50) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.kampanyalar', N'kampanya_adi') IS NULL
BEGIN
    ALTER TABLE [dbo].[kampanyalar] ADD [kampanya_adi] nvarchar(200) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.kampanyalar', N'seo_slug') IS NULL
BEGIN
    ALTER TABLE [dbo].[kampanyalar] ADD [seo_slug] nvarchar(255) NULL;
END
GO
IF COL_LENGTH(N'dbo.kampanyalar', N'sayfa_url') IS NULL
BEGIN
    ALTER TABLE [dbo].[kampanyalar] ADD [sayfa_url] nvarchar(255) NULL;
END
GO
IF COL_LENGTH(N'dbo.kampanyalar', N'kampanya_aciklamasi') IS NULL
BEGIN
    ALTER TABLE [dbo].[kampanyalar] ADD [kampanya_aciklamasi] nvarchar(max) NULL;
END
GO
IF COL_LENGTH(N'dbo.kampanyalar', N'kisa_aciklama') IS NULL
BEGIN
    ALTER TABLE [dbo].[kampanyalar] ADD [kisa_aciklama] nvarchar(255) NULL;
END
GO
IF COL_LENGTH(N'dbo.kampanyalar', N'detay_aciklama') IS NULL
BEGIN
    ALTER TABLE [dbo].[kampanyalar] ADD [detay_aciklama] nvarchar(max) NULL;
END
GO
IF COL_LENGTH(N'dbo.kampanyalar', N'tur') IS NULL
BEGIN
    ALTER TABLE [dbo].[kampanyalar] ADD [tur] nvarchar(255) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.kampanyalar', N'indirim_orani') IS NULL
BEGIN
    ALTER TABLE [dbo].[kampanyalar] ADD [indirim_orani] decimal(5,2) NULL;
END
GO
IF COL_LENGTH(N'dbo.kampanyalar', N'indirim_tutari') IS NULL
BEGIN
    ALTER TABLE [dbo].[kampanyalar] ADD [indirim_tutari] decimal(10,2) NULL;
END
GO
IF COL_LENGTH(N'dbo.kampanyalar', N'maksimum_indirim_tutari') IS NULL
BEGIN
    ALTER TABLE [dbo].[kampanyalar] ADD [maksimum_indirim_tutari] decimal(10,2) NULL;
END
GO
IF COL_LENGTH(N'dbo.kampanyalar', N'minimum_sepet_tutari') IS NULL
BEGIN
    ALTER TABLE [dbo].[kampanyalar] ADD [minimum_sepet_tutari] decimal(10,2) NULL;
END
GO
IF COL_LENGTH(N'dbo.kampanyalar', N'hedef_otel_turu') IS NULL
BEGIN
    ALTER TABLE [dbo].[kampanyalar] ADD [hedef_otel_turu] nvarchar(255) NULL;
END
GO
IF COL_LENGTH(N'dbo.kampanyalar', N'hedef_otel_idleri') IS NULL
BEGIN
    ALTER TABLE [dbo].[kampanyalar] ADD [hedef_otel_idleri] nvarchar(max) NULL;
END
GO
IF COL_LENGTH(N'dbo.kampanyalar', N'hedef_sehirler') IS NULL
BEGIN
    ALTER TABLE [dbo].[kampanyalar] ADD [hedef_sehirler] nvarchar(max) NULL;
END
GO
IF COL_LENGTH(N'dbo.kampanyalar', N'hedef_ilceler') IS NULL
BEGIN
    ALTER TABLE [dbo].[kampanyalar] ADD [hedef_ilceler] nvarchar(max) NULL;
END
GO
IF COL_LENGTH(N'dbo.kampanyalar', N'hedef_mahalleler') IS NULL
BEGIN
    ALTER TABLE [dbo].[kampanyalar] ADD [hedef_mahalleler] nvarchar(max) NULL;
END
GO
IF COL_LENGTH(N'dbo.kampanyalar', N'hedef_kullanici_turu') IS NULL
BEGIN
    ALTER TABLE [dbo].[kampanyalar] ADD [hedef_kullanici_turu] nvarchar(255) NULL;
END
GO
IF COL_LENGTH(N'dbo.kampanyalar', N'minimum_gecmis_rezervasyon') IS NULL
BEGIN
    ALTER TABLE [dbo].[kampanyalar] ADD [minimum_gecmis_rezervasyon] tinyint NULL;
END
GO
IF COL_LENGTH(N'dbo.kampanyalar', N'baslangic_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[kampanyalar] ADD [baslangic_tarihi] datetime2(0) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.kampanyalar', N'bitis_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[kampanyalar] ADD [bitis_tarihi] datetime2(0) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.kampanyalar', N'rezervasyon_tarih_araligi_baslangic') IS NULL
BEGIN
    ALTER TABLE [dbo].[kampanyalar] ADD [rezervasyon_tarih_araligi_baslangic] date NULL;
END
GO
IF COL_LENGTH(N'dbo.kampanyalar', N'rezervasyon_tarih_araligi_bitis') IS NULL
BEGIN
    ALTER TABLE [dbo].[kampanyalar] ADD [rezervasyon_tarih_araligi_bitis] date NULL;
END
GO
IF COL_LENGTH(N'dbo.kampanyalar', N'konaklama_tarih_araligi_baslangic') IS NULL
BEGIN
    ALTER TABLE [dbo].[kampanyalar] ADD [konaklama_tarih_araligi_baslangic] date NULL;
END
GO
IF COL_LENGTH(N'dbo.kampanyalar', N'konaklama_tarih_araligi_bitis') IS NULL
BEGIN
    ALTER TABLE [dbo].[kampanyalar] ADD [konaklama_tarih_araligi_bitis] date NULL;
END
GO
IF COL_LENGTH(N'dbo.kampanyalar', N'minimum_geceleme') IS NULL
BEGIN
    ALTER TABLE [dbo].[kampanyalar] ADD [minimum_geceleme] tinyint DEFAULT ((1)) NULL;
END
GO
IF COL_LENGTH(N'dbo.kampanyalar', N'maksimum_geceleme') IS NULL
BEGIN
    ALTER TABLE [dbo].[kampanyalar] ADD [maksimum_geceleme] smallint NULL;
END
GO
IF COL_LENGTH(N'dbo.kampanyalar', N'erken_rezervasyon_gun_sayisi') IS NULL
BEGIN
    ALTER TABLE [dbo].[kampanyalar] ADD [erken_rezervasyon_gun_sayisi] smallint NULL;
END
GO
IF COL_LENGTH(N'dbo.kampanyalar', N'toplam_kullanim_limiti') IS NULL
BEGIN
    ALTER TABLE [dbo].[kampanyalar] ADD [toplam_kullanim_limiti] int NULL;
END
GO
IF COL_LENGTH(N'dbo.kampanyalar', N'kullanici_basina_limit') IS NULL
BEGIN
    ALTER TABLE [dbo].[kampanyalar] ADD [kullanici_basina_limit] tinyint DEFAULT ((1)) NULL;
END
GO
IF COL_LENGTH(N'dbo.kampanyalar', N'kullanilan_adet') IS NULL
BEGIN
    ALTER TABLE [dbo].[kampanyalar] ADD [kullanilan_adet] int DEFAULT ((0)) NULL;
END
GO
IF COL_LENGTH(N'dbo.kampanyalar', N'gosterim_adedi') IS NULL
BEGIN
    ALTER TABLE [dbo].[kampanyalar] ADD [gosterim_adedi] int DEFAULT ((0)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.kampanyalar', N'aktif_mi') IS NULL
BEGIN
    ALTER TABLE [dbo].[kampanyalar] ADD [aktif_mi] bit DEFAULT ((1)) NULL;
END
GO
IF COL_LENGTH(N'dbo.kampanyalar', N'gorunurluk_durumu') IS NULL
BEGIN
    ALTER TABLE [dbo].[kampanyalar] ADD [gorunurluk_durumu] nvarchar(255) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.kampanyalar', N'partner_katilim_acik') IS NULL
BEGIN
    ALTER TABLE [dbo].[kampanyalar] ADD [partner_katilim_acik] bit DEFAULT ((1)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.kampanyalar', N'partner_katilim_baslangic') IS NULL
BEGIN
    ALTER TABLE [dbo].[kampanyalar] ADD [partner_katilim_baslangic] datetime2(0) NULL;
END
GO
IF COL_LENGTH(N'dbo.kampanyalar', N'partner_katilim_bitis') IS NULL
BEGIN
    ALTER TABLE [dbo].[kampanyalar] ADD [partner_katilim_bitis] datetime2(0) NULL;
END
GO
IF COL_LENGTH(N'dbo.kampanyalar', N'otomatik_sona_ersin') IS NULL
BEGIN
    ALTER TABLE [dbo].[kampanyalar] ADD [otomatik_sona_ersin] bit DEFAULT ((1)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.kampanyalar', N'one_cikan_kampanya') IS NULL
BEGIN
    ALTER TABLE [dbo].[kampanyalar] ADD [one_cikan_kampanya] bit DEFAULT ((0)) NULL;
END
GO
IF COL_LENGTH(N'dbo.kampanyalar', N'siralama') IS NULL
BEGIN
    ALTER TABLE [dbo].[kampanyalar] ADD [siralama] int DEFAULT ((0)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.kampanyalar', N'aktif_sayfa_vitrini') IS NULL
BEGIN
    ALTER TABLE [dbo].[kampanyalar] ADD [aktif_sayfa_vitrini] bit DEFAULT ((0)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.kampanyalar', N'banner_gorseli') IS NULL
BEGIN
    ALTER TABLE [dbo].[kampanyalar] ADD [banner_gorseli] nvarchar(255) NULL;
END
GO
IF COL_LENGTH(N'dbo.kampanyalar', N'hero_gorseli') IS NULL
BEGIN
    ALTER TABLE [dbo].[kampanyalar] ADD [hero_gorseli] nvarchar(500) NULL;
END
GO
IF COL_LENGTH(N'dbo.kampanyalar', N'kart_gorseli') IS NULL
BEGIN
    ALTER TABLE [dbo].[kampanyalar] ADD [kart_gorseli] nvarchar(500) NULL;
END
GO
IF COL_LENGTH(N'dbo.kampanyalar', N'mobil_gorsel') IS NULL
BEGIN
    ALTER TABLE [dbo].[kampanyalar] ADD [mobil_gorsel] nvarchar(500) NULL;
END
GO
IF COL_LENGTH(N'dbo.kampanyalar', N'meta_title') IS NULL
BEGIN
    ALTER TABLE [dbo].[kampanyalar] ADD [meta_title] nvarchar(255) NULL;
END
GO
IF COL_LENGTH(N'dbo.kampanyalar', N'meta_description') IS NULL
BEGIN
    ALTER TABLE [dbo].[kampanyalar] ADD [meta_description] nvarchar(500) NULL;
END
GO
IF COL_LENGTH(N'dbo.kampanyalar', N'canonical_url') IS NULL
BEGIN
    ALTER TABLE [dbo].[kampanyalar] ADD [canonical_url] nvarchar(500) NULL;
END
GO
IF COL_LENGTH(N'dbo.kampanyalar', N'kampanya_etiketi') IS NULL
BEGIN
    ALTER TABLE [dbo].[kampanyalar] ADD [kampanya_etiketi] nvarchar(100) NULL;
END
GO
IF COL_LENGTH(N'dbo.kampanyalar', N'promo_badge') IS NULL
BEGIN
    ALTER TABLE [dbo].[kampanyalar] ADD [promo_badge] nvarchar(100) NULL;
END
GO
IF COL_LENGTH(N'dbo.kampanyalar', N'kampanya_renk_kodu') IS NULL
BEGIN
    ALTER TABLE [dbo].[kampanyalar] ADD [kampanya_renk_kodu] nvarchar(20) NULL;
END
GO
IF COL_LENGTH(N'dbo.kampanyalar', N'listeleme_basligi') IS NULL
BEGIN
    ALTER TABLE [dbo].[kampanyalar] ADD [listeleme_basligi] nvarchar(255) NULL;
END
GO
IF COL_LENGTH(N'dbo.kampanyalar', N'listeleme_aciklamasi') IS NULL
BEGIN
    ALTER TABLE [dbo].[kampanyalar] ADD [listeleme_aciklamasi] nvarchar(500) NULL;
END
GO
IF COL_LENGTH(N'dbo.kampanyalar', N'kullanim_kosullari') IS NULL
BEGIN
    ALTER TABLE [dbo].[kampanyalar] ADD [kullanim_kosullari] nvarchar(max) NULL;
END
GO
IF COL_LENGTH(N'dbo.kampanyalar', N'olusturan_admin_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[kampanyalar] ADD [olusturan_admin_id] bigint NULL;
END
GO
IF COL_LENGTH(N'dbo.kampanyalar', N'olusturulma_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[kampanyalar] ADD [olusturulma_tarihi] datetime2(0) DEFAULT (sysutcdatetime()) NULL;
END
GO
IF COL_LENGTH(N'dbo.kampanyalar', N'guncellenme_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[kampanyalar] ADD [guncellenme_tarihi] datetime2(0) NULL;
END
GO
