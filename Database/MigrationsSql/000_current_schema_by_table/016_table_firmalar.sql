SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.firmalar', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[firmalar]
    (
        [id] bigint IDENTITY(1,1) NOT NULL,
        [firma_kodu] nvarchar(32) NOT NULL,
        [firma_adi] nvarchar(200) NOT NULL,
        [firma_turu] nvarchar(255) NOT NULL,
        [vergi_no] nvarchar(20) NOT NULL,
        [vergi_dairesi] nvarchar(100) NULL,
        [ticaret_sicil_no] nvarchar(50) NULL,
        [mersis_no] nvarchar(32) NULL,
        [firma_eposta] nvarchar(100) NULL,
        [firma_telefon] nvarchar(20) NULL,
        [web_sitesi] nvarchar(255) NULL,
        [logo_yolu] nvarchar(255) NULL,
        [sektor] nvarchar(100) NULL,
        [calisan_sayisi] int CONSTRAINT [DF__firmalar__calisa__40058253] DEFAULT ((0)) NULL,
        [aylik_seyahat_butcesi] decimal(12,2) NULL,
        [varsayilan_para_birimi] nvarchar(3) NOT NULL,
        [acik_adres] nvarchar(max) NULL,
        [sehir] nvarchar(100) NULL,
        [ilce] nvarchar(100) NULL,
        [posta_kodu] nvarchar(10) NULL,
        [yetkili_ad_soyad] nvarchar(100) NULL,
        [yetkili_unvani] nvarchar(100) NULL,
        [yetkili_eposta] nvarchar(100) NULL,
        [yetkili_telefon] nvarchar(20) NULL,
        [onay_durumu] nvarchar(255) NOT NULL,
        [basvuru_tarihi] datetime2(0) CONSTRAINT [DF__firmalar__basvur__40F9A68C] DEFAULT (sysutcdatetime()) NULL,
        [onay_sureci_baslama_tarihi] datetime2(0) NULL,
        [onay_tarihi] datetime2(0) NULL,
        [reddedilme_tarihi] datetime2(0) NULL,
        [onaylayan_kullanici_id] bigint NULL,
        [onay_notu] nvarchar(max) NULL,
        [aktif_mi] bit CONSTRAINT [DF__firmalar__aktif___41EDCAC5] DEFAULT ((1)) NOT NULL,
        [giris_izni_aktif_mi] bit CONSTRAINT [DF__firmalar__giris___42E1EEFE] DEFAULT ((0)) NOT NULL,
        [planlanan_onay_suresi_saat] smallint CONSTRAINT [DF__firmalar__planla__43D61337] DEFAULT ((24)) NOT NULL,
        [kayit_kaynagi] nvarchar(50) NOT NULL,
        [sozlesme_onay_tarihi] datetime2(0) NULL,
        [kvkk_onay_tarihi] datetime2(0) NULL,
        [notlar] nvarchar(max) NULL,
        [olusturulma_tarihi] datetime2(0) CONSTRAINT [DF__firmalar__olustu__44CA3770] DEFAULT (sysutcdatetime()) NULL,
        [guncellenme_tarihi] datetime2(0) NULL,
        CONSTRAINT [PK_firmalar] PRIMARY KEY CLUSTERED ([id] ASC)
    );
END
GO

-- Missing column repair blocks for existing databases.
IF COL_LENGTH(N'dbo.firmalar', N'id') IS NULL
BEGIN
    ALTER TABLE [dbo].[firmalar] ADD [id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.firmalar', N'firma_kodu') IS NULL
BEGIN
    ALTER TABLE [dbo].[firmalar] ADD [firma_kodu] nvarchar(32) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.firmalar', N'firma_adi') IS NULL
BEGIN
    ALTER TABLE [dbo].[firmalar] ADD [firma_adi] nvarchar(200) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.firmalar', N'firma_turu') IS NULL
BEGIN
    ALTER TABLE [dbo].[firmalar] ADD [firma_turu] nvarchar(255) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.firmalar', N'vergi_no') IS NULL
BEGIN
    ALTER TABLE [dbo].[firmalar] ADD [vergi_no] nvarchar(20) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.firmalar', N'vergi_dairesi') IS NULL
BEGIN
    ALTER TABLE [dbo].[firmalar] ADD [vergi_dairesi] nvarchar(100) NULL;
END
GO
IF COL_LENGTH(N'dbo.firmalar', N'ticaret_sicil_no') IS NULL
BEGIN
    ALTER TABLE [dbo].[firmalar] ADD [ticaret_sicil_no] nvarchar(50) NULL;
END
GO
IF COL_LENGTH(N'dbo.firmalar', N'mersis_no') IS NULL
BEGIN
    ALTER TABLE [dbo].[firmalar] ADD [mersis_no] nvarchar(32) NULL;
END
GO
IF COL_LENGTH(N'dbo.firmalar', N'firma_eposta') IS NULL
BEGIN
    ALTER TABLE [dbo].[firmalar] ADD [firma_eposta] nvarchar(100) NULL;
END
GO
IF COL_LENGTH(N'dbo.firmalar', N'firma_telefon') IS NULL
BEGIN
    ALTER TABLE [dbo].[firmalar] ADD [firma_telefon] nvarchar(20) NULL;
END
GO
IF COL_LENGTH(N'dbo.firmalar', N'web_sitesi') IS NULL
BEGIN
    ALTER TABLE [dbo].[firmalar] ADD [web_sitesi] nvarchar(255) NULL;
END
GO
IF COL_LENGTH(N'dbo.firmalar', N'logo_yolu') IS NULL
BEGIN
    ALTER TABLE [dbo].[firmalar] ADD [logo_yolu] nvarchar(255) NULL;
END
GO
IF COL_LENGTH(N'dbo.firmalar', N'sektor') IS NULL
BEGIN
    ALTER TABLE [dbo].[firmalar] ADD [sektor] nvarchar(100) NULL;
END
GO
IF COL_LENGTH(N'dbo.firmalar', N'calisan_sayisi') IS NULL
BEGIN
    ALTER TABLE [dbo].[firmalar] ADD [calisan_sayisi] int DEFAULT ((0)) NULL;
END
GO
IF COL_LENGTH(N'dbo.firmalar', N'aylik_seyahat_butcesi') IS NULL
BEGIN
    ALTER TABLE [dbo].[firmalar] ADD [aylik_seyahat_butcesi] decimal(12,2) NULL;
END
GO
IF COL_LENGTH(N'dbo.firmalar', N'varsayilan_para_birimi') IS NULL
BEGIN
    ALTER TABLE [dbo].[firmalar] ADD [varsayilan_para_birimi] nvarchar(3) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.firmalar', N'acik_adres') IS NULL
BEGIN
    ALTER TABLE [dbo].[firmalar] ADD [acik_adres] nvarchar(max) NULL;
END
GO
IF COL_LENGTH(N'dbo.firmalar', N'sehir') IS NULL
BEGIN
    ALTER TABLE [dbo].[firmalar] ADD [sehir] nvarchar(100) NULL;
END
GO
IF COL_LENGTH(N'dbo.firmalar', N'ilce') IS NULL
BEGIN
    ALTER TABLE [dbo].[firmalar] ADD [ilce] nvarchar(100) NULL;
END
GO
IF COL_LENGTH(N'dbo.firmalar', N'posta_kodu') IS NULL
BEGIN
    ALTER TABLE [dbo].[firmalar] ADD [posta_kodu] nvarchar(10) NULL;
END
GO
IF COL_LENGTH(N'dbo.firmalar', N'yetkili_ad_soyad') IS NULL
BEGIN
    ALTER TABLE [dbo].[firmalar] ADD [yetkili_ad_soyad] nvarchar(100) NULL;
END
GO
IF COL_LENGTH(N'dbo.firmalar', N'yetkili_unvani') IS NULL
BEGIN
    ALTER TABLE [dbo].[firmalar] ADD [yetkili_unvani] nvarchar(100) NULL;
END
GO
IF COL_LENGTH(N'dbo.firmalar', N'yetkili_eposta') IS NULL
BEGIN
    ALTER TABLE [dbo].[firmalar] ADD [yetkili_eposta] nvarchar(100) NULL;
END
GO
IF COL_LENGTH(N'dbo.firmalar', N'yetkili_telefon') IS NULL
BEGIN
    ALTER TABLE [dbo].[firmalar] ADD [yetkili_telefon] nvarchar(20) NULL;
END
GO
IF COL_LENGTH(N'dbo.firmalar', N'onay_durumu') IS NULL
BEGIN
    ALTER TABLE [dbo].[firmalar] ADD [onay_durumu] nvarchar(255) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.firmalar', N'basvuru_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[firmalar] ADD [basvuru_tarihi] datetime2(0) DEFAULT (sysutcdatetime()) NULL;
END
GO
IF COL_LENGTH(N'dbo.firmalar', N'onay_sureci_baslama_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[firmalar] ADD [onay_sureci_baslama_tarihi] datetime2(0) NULL;
END
GO
IF COL_LENGTH(N'dbo.firmalar', N'onay_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[firmalar] ADD [onay_tarihi] datetime2(0) NULL;
END
GO
IF COL_LENGTH(N'dbo.firmalar', N'reddedilme_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[firmalar] ADD [reddedilme_tarihi] datetime2(0) NULL;
END
GO
IF COL_LENGTH(N'dbo.firmalar', N'onaylayan_kullanici_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[firmalar] ADD [onaylayan_kullanici_id] bigint NULL;
END
GO
IF COL_LENGTH(N'dbo.firmalar', N'onay_notu') IS NULL
BEGIN
    ALTER TABLE [dbo].[firmalar] ADD [onay_notu] nvarchar(max) NULL;
END
GO
IF COL_LENGTH(N'dbo.firmalar', N'aktif_mi') IS NULL
BEGIN
    ALTER TABLE [dbo].[firmalar] ADD [aktif_mi] bit DEFAULT ((1)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.firmalar', N'giris_izni_aktif_mi') IS NULL
BEGIN
    ALTER TABLE [dbo].[firmalar] ADD [giris_izni_aktif_mi] bit DEFAULT ((0)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.firmalar', N'planlanan_onay_suresi_saat') IS NULL
BEGIN
    ALTER TABLE [dbo].[firmalar] ADD [planlanan_onay_suresi_saat] smallint DEFAULT ((24)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.firmalar', N'kayit_kaynagi') IS NULL
BEGIN
    ALTER TABLE [dbo].[firmalar] ADD [kayit_kaynagi] nvarchar(50) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.firmalar', N'sozlesme_onay_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[firmalar] ADD [sozlesme_onay_tarihi] datetime2(0) NULL;
END
GO
IF COL_LENGTH(N'dbo.firmalar', N'kvkk_onay_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[firmalar] ADD [kvkk_onay_tarihi] datetime2(0) NULL;
END
GO
IF COL_LENGTH(N'dbo.firmalar', N'notlar') IS NULL
BEGIN
    ALTER TABLE [dbo].[firmalar] ADD [notlar] nvarchar(max) NULL;
END
GO
IF COL_LENGTH(N'dbo.firmalar', N'olusturulma_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[firmalar] ADD [olusturulma_tarihi] datetime2(0) DEFAULT (sysutcdatetime()) NULL;
END
GO
IF COL_LENGTH(N'dbo.firmalar', N'guncellenme_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[firmalar] ADD [guncellenme_tarihi] datetime2(0) NULL;
END
GO
