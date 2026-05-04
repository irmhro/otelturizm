SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.partner_detaylari', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[partner_detaylari]
    (
        [id] bigint IDENTITY(1,1) NOT NULL,
        [kullanici_id] bigint NOT NULL,
        [firma_unvani] nvarchar(200) NOT NULL,
        [firma_turu] nvarchar(255) NOT NULL,
        [ticaret_sicil_no] nvarchar(50) NULL,
        [ticaret_odasi] nvarchar(100) NULL,
        [kurulus_yili] nvarchar(max) NULL,
        [vergi_dairesi] nvarchar(100) NOT NULL,
        [vergi_numarasi] nvarchar(20) NOT NULL,
        [tc_kimlik_no] nvarchar(11) NULL,
        [fatura_adresi] nvarchar(max) NOT NULL,
        [fatura_il] nvarchar(50) NOT NULL,
        [fatura_ilce] nvarchar(50) NOT NULL,
        [fatura_posta_kodu] nvarchar(10) NULL,
        [yetkili_ad_soyad] nvarchar(100) NOT NULL,
        [yetkili_tc_no] nvarchar(11) NOT NULL,
        [yetkili_telefon] nvarchar(20) NOT NULL,
        [yetkili_eposta] nvarchar(100) NOT NULL,
        [yetkili_gorev] nvarchar(100) NULL,
        [banka_adi] nvarchar(100) NOT NULL,
        [banka_subesi] nvarchar(100) NULL,
        [iban] nvarchar(26) NOT NULL,
        [hesap_sahibi_adi] nvarchar(150) NOT NULL,
        [hesap_para_birimi] nvarchar(255) NULL,
        [sozlesme_no] nvarchar(50) NULL,
        [sozlesme_baslangic_tarihi] date NULL,
        [sozlesme_bitis_tarihi] date NULL,
        [sozlesme_pdf_yolu] nvarchar(255) NULL,
        [onay_durumu] nvarchar(255) NULL,
        [onay_tarihi] datetime2(0) NULL,
        [onaylayan_admin_id] bigint NULL,
        [red_nedeni] nvarchar(500) NULL,
        [web_sitesi] nvarchar(255) NULL,
        [logo_yolu] nvarchar(255) NULL,
        [aciklama] nvarchar(max) NULL,
        [olusturulma_tarihi] datetime2(0) CONSTRAINT [DF__partner_d__olust__51BA1E3A] DEFAULT (sysutcdatetime()) NULL,
        [guncellenme_tarihi] datetime2(0) NULL,
        [eposta_giris_onayi_verildi_mi] bit CONSTRAINT [DF_partner_detaylari_eposta_giris_onayi_verildi_mi] DEFAULT ((0)) NOT NULL,
        [eposta_giris_onay_tarihi] datetime2(7) NULL,
        [eposta_giris_onaylayan_admin_id] bigint NULL,
        [aktif_mi] bit CONSTRAINT [DF_partner_detaylari_aktif_mi] DEFAULT ((1)) NOT NULL,
        [otel_tipi_id] int NULL,
        CONSTRAINT [PK_partner_detaylari] PRIMARY KEY CLUSTERED ([id] ASC)
    );
END
GO

-- Missing column repair blocks for existing databases.
IF COL_LENGTH(N'dbo.partner_detaylari', N'id') IS NULL
BEGIN
    ALTER TABLE [dbo].[partner_detaylari] ADD [id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.partner_detaylari', N'kullanici_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[partner_detaylari] ADD [kullanici_id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.partner_detaylari', N'firma_unvani') IS NULL
BEGIN
    ALTER TABLE [dbo].[partner_detaylari] ADD [firma_unvani] nvarchar(200) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.partner_detaylari', N'firma_turu') IS NULL
BEGIN
    ALTER TABLE [dbo].[partner_detaylari] ADD [firma_turu] nvarchar(255) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.partner_detaylari', N'ticaret_sicil_no') IS NULL
BEGIN
    ALTER TABLE [dbo].[partner_detaylari] ADD [ticaret_sicil_no] nvarchar(50) NULL;
END
GO
IF COL_LENGTH(N'dbo.partner_detaylari', N'ticaret_odasi') IS NULL
BEGIN
    ALTER TABLE [dbo].[partner_detaylari] ADD [ticaret_odasi] nvarchar(100) NULL;
END
GO
IF COL_LENGTH(N'dbo.partner_detaylari', N'kurulus_yili') IS NULL
BEGIN
    ALTER TABLE [dbo].[partner_detaylari] ADD [kurulus_yili] nvarchar(max) NULL;
END
GO
IF COL_LENGTH(N'dbo.partner_detaylari', N'vergi_dairesi') IS NULL
BEGIN
    ALTER TABLE [dbo].[partner_detaylari] ADD [vergi_dairesi] nvarchar(100) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.partner_detaylari', N'vergi_numarasi') IS NULL
BEGIN
    ALTER TABLE [dbo].[partner_detaylari] ADD [vergi_numarasi] nvarchar(20) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.partner_detaylari', N'tc_kimlik_no') IS NULL
BEGIN
    ALTER TABLE [dbo].[partner_detaylari] ADD [tc_kimlik_no] nvarchar(11) NULL;
END
GO
IF COL_LENGTH(N'dbo.partner_detaylari', N'fatura_adresi') IS NULL
BEGIN
    ALTER TABLE [dbo].[partner_detaylari] ADD [fatura_adresi] nvarchar(max) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.partner_detaylari', N'fatura_il') IS NULL
BEGIN
    ALTER TABLE [dbo].[partner_detaylari] ADD [fatura_il] nvarchar(50) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.partner_detaylari', N'fatura_ilce') IS NULL
BEGIN
    ALTER TABLE [dbo].[partner_detaylari] ADD [fatura_ilce] nvarchar(50) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.partner_detaylari', N'fatura_posta_kodu') IS NULL
BEGIN
    ALTER TABLE [dbo].[partner_detaylari] ADD [fatura_posta_kodu] nvarchar(10) NULL;
END
GO
IF COL_LENGTH(N'dbo.partner_detaylari', N'yetkili_ad_soyad') IS NULL
BEGIN
    ALTER TABLE [dbo].[partner_detaylari] ADD [yetkili_ad_soyad] nvarchar(100) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.partner_detaylari', N'yetkili_tc_no') IS NULL
BEGIN
    ALTER TABLE [dbo].[partner_detaylari] ADD [yetkili_tc_no] nvarchar(11) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.partner_detaylari', N'yetkili_telefon') IS NULL
BEGIN
    ALTER TABLE [dbo].[partner_detaylari] ADD [yetkili_telefon] nvarchar(20) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.partner_detaylari', N'yetkili_eposta') IS NULL
BEGIN
    ALTER TABLE [dbo].[partner_detaylari] ADD [yetkili_eposta] nvarchar(100) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.partner_detaylari', N'yetkili_gorev') IS NULL
BEGIN
    ALTER TABLE [dbo].[partner_detaylari] ADD [yetkili_gorev] nvarchar(100) NULL;
END
GO
IF COL_LENGTH(N'dbo.partner_detaylari', N'banka_adi') IS NULL
BEGIN
    ALTER TABLE [dbo].[partner_detaylari] ADD [banka_adi] nvarchar(100) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.partner_detaylari', N'banka_subesi') IS NULL
BEGIN
    ALTER TABLE [dbo].[partner_detaylari] ADD [banka_subesi] nvarchar(100) NULL;
END
GO
IF COL_LENGTH(N'dbo.partner_detaylari', N'iban') IS NULL
BEGIN
    ALTER TABLE [dbo].[partner_detaylari] ADD [iban] nvarchar(26) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.partner_detaylari', N'hesap_sahibi_adi') IS NULL
BEGIN
    ALTER TABLE [dbo].[partner_detaylari] ADD [hesap_sahibi_adi] nvarchar(150) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.partner_detaylari', N'hesap_para_birimi') IS NULL
BEGIN
    ALTER TABLE [dbo].[partner_detaylari] ADD [hesap_para_birimi] nvarchar(255) NULL;
END
GO
IF COL_LENGTH(N'dbo.partner_detaylari', N'sozlesme_no') IS NULL
BEGIN
    ALTER TABLE [dbo].[partner_detaylari] ADD [sozlesme_no] nvarchar(50) NULL;
END
GO
IF COL_LENGTH(N'dbo.partner_detaylari', N'sozlesme_baslangic_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[partner_detaylari] ADD [sozlesme_baslangic_tarihi] date NULL;
END
GO
IF COL_LENGTH(N'dbo.partner_detaylari', N'sozlesme_bitis_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[partner_detaylari] ADD [sozlesme_bitis_tarihi] date NULL;
END
GO
IF COL_LENGTH(N'dbo.partner_detaylari', N'sozlesme_pdf_yolu') IS NULL
BEGIN
    ALTER TABLE [dbo].[partner_detaylari] ADD [sozlesme_pdf_yolu] nvarchar(255) NULL;
END
GO
IF COL_LENGTH(N'dbo.partner_detaylari', N'onay_durumu') IS NULL
BEGIN
    ALTER TABLE [dbo].[partner_detaylari] ADD [onay_durumu] nvarchar(255) NULL;
END
GO
IF COL_LENGTH(N'dbo.partner_detaylari', N'onay_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[partner_detaylari] ADD [onay_tarihi] datetime2(0) NULL;
END
GO
IF COL_LENGTH(N'dbo.partner_detaylari', N'onaylayan_admin_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[partner_detaylari] ADD [onaylayan_admin_id] bigint NULL;
END
GO
IF COL_LENGTH(N'dbo.partner_detaylari', N'red_nedeni') IS NULL
BEGIN
    ALTER TABLE [dbo].[partner_detaylari] ADD [red_nedeni] nvarchar(500) NULL;
END
GO
IF COL_LENGTH(N'dbo.partner_detaylari', N'web_sitesi') IS NULL
BEGIN
    ALTER TABLE [dbo].[partner_detaylari] ADD [web_sitesi] nvarchar(255) NULL;
END
GO
IF COL_LENGTH(N'dbo.partner_detaylari', N'logo_yolu') IS NULL
BEGIN
    ALTER TABLE [dbo].[partner_detaylari] ADD [logo_yolu] nvarchar(255) NULL;
END
GO
IF COL_LENGTH(N'dbo.partner_detaylari', N'aciklama') IS NULL
BEGIN
    ALTER TABLE [dbo].[partner_detaylari] ADD [aciklama] nvarchar(max) NULL;
END
GO
IF COL_LENGTH(N'dbo.partner_detaylari', N'olusturulma_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[partner_detaylari] ADD [olusturulma_tarihi] datetime2(0) DEFAULT (sysutcdatetime()) NULL;
END
GO
IF COL_LENGTH(N'dbo.partner_detaylari', N'guncellenme_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[partner_detaylari] ADD [guncellenme_tarihi] datetime2(0) NULL;
END
GO
IF COL_LENGTH(N'dbo.partner_detaylari', N'eposta_giris_onayi_verildi_mi') IS NULL
BEGIN
    ALTER TABLE [dbo].[partner_detaylari] ADD [eposta_giris_onayi_verildi_mi] bit DEFAULT ((0)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.partner_detaylari', N'eposta_giris_onay_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[partner_detaylari] ADD [eposta_giris_onay_tarihi] datetime2(7) NULL;
END
GO
IF COL_LENGTH(N'dbo.partner_detaylari', N'eposta_giris_onaylayan_admin_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[partner_detaylari] ADD [eposta_giris_onaylayan_admin_id] bigint NULL;
END
GO
IF COL_LENGTH(N'dbo.partner_detaylari', N'aktif_mi') IS NULL
BEGIN
    ALTER TABLE [dbo].[partner_detaylari] ADD [aktif_mi] bit DEFAULT ((1)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.partner_detaylari', N'otel_tipi_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[partner_detaylari] ADD [otel_tipi_id] int NULL;
END
GO
