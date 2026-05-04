SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.komisyon_muhasebe_kayitlari', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[komisyon_muhasebe_kayitlari]
    (
        [id] bigint IDENTITY(1,1) NOT NULL,
        [kayit_no] nvarchar(30) NOT NULL,
        [kayit_tarihi] date NOT NULL,
        [donem] nvarchar(7) NOT NULL,
        [rezervasyon_id] bigint NOT NULL,
        [otel_id] bigint NOT NULL,
        [partner_id] bigint NOT NULL,
        [fatura_id] bigint NULL,
        [toplam_rezervasyon_tutari] decimal(10,2) NOT NULL,
        [komisyon_orani] decimal(5,2) NOT NULL,
        [komisyon_tutari] decimal(10,2) NOT NULL,
        [ek_kesintiler] decimal(10,2) CONSTRAINT [DF__komisyon___ek_ke__625A9A57] DEFAULT ((0.00)) NULL,
        [net_otele_odenecek] decimal(10,2) NOT NULL,
        [otele_odeme_durumu] nvarchar(255) NULL,
        [otele_odeme_tarihi] date NULL,
        [otele_odeme_referansi] nvarchar(50) NULL,
        [odeme_emri_no] nvarchar(30) NULL,
        [muhasebe_hesap_kodu] nvarchar(20) NULL,
        [karsi_hesap_kodu] nvarchar(20) NULL,
        [yevmiye_no] nvarchar(20) NULL,
        [fis_no] nvarchar(20) NULL,
        [mutabakat_durumu] nvarchar(255) NULL,
        [mutabakat_gonderim_tarihi] datetime2(0) NULL,
        [mutabakat_onay_tarihi] datetime2(0) NULL,
        [mutabakat_notu] nvarchar(max) NULL,
        [itiraz_var_mi] bit CONSTRAINT [DF__komisyon___itira__634EBE90] DEFAULT ((0)) NULL,
        [itiraz_nedeni] nvarchar(500) NULL,
        [itiraz_tarihi] datetime2(0) NULL,
        [itiraz_cozum_tarihi] datetime2(0) NULL,
        [itiraz_cozum_aciklamasi] nvarchar(max) NULL,
        [duzeltme_tutari] decimal(10,2) NULL,
        [stopaj_orani] decimal(5,2) CONSTRAINT [DF__komisyon___stopa__6442E2C9] DEFAULT ((0.00)) NULL,
        [stopaj_tutari] decimal(10,2) CONSTRAINT [DF__komisyon___stopa__65370702] DEFAULT ((0.00)) NULL,
        [kdv_orani] decimal(5,2) CONSTRAINT [DF__komisyon___kdv_o__662B2B3B] DEFAULT ((20.00)) NULL,
        [kdv_tutari] decimal(10,2) CONSTRAINT [DF__komisyon___kdv_t__671F4F74] DEFAULT ((0.00)) NULL,
        [olusturulma_tarihi] datetime2(0) CONSTRAINT [DF__komisyon___olust__681373AD] DEFAULT (sysutcdatetime()) NULL,
        [guncellenme_tarihi] datetime2(0) NULL,
        [olusturan_admin_id] bigint NULL,
        [onaylayan_finans_admin_id] bigint NULL,
        CONSTRAINT [PK_komisyon_muhasebe_kayitlari] PRIMARY KEY CLUSTERED ([id] ASC)
    );
END
GO

-- Missing column repair blocks for existing databases.
IF COL_LENGTH(N'dbo.komisyon_muhasebe_kayitlari', N'id') IS NULL
BEGIN
    ALTER TABLE [dbo].[komisyon_muhasebe_kayitlari] ADD [id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.komisyon_muhasebe_kayitlari', N'kayit_no') IS NULL
BEGIN
    ALTER TABLE [dbo].[komisyon_muhasebe_kayitlari] ADD [kayit_no] nvarchar(30) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.komisyon_muhasebe_kayitlari', N'kayit_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[komisyon_muhasebe_kayitlari] ADD [kayit_tarihi] date NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.komisyon_muhasebe_kayitlari', N'donem') IS NULL
BEGIN
    ALTER TABLE [dbo].[komisyon_muhasebe_kayitlari] ADD [donem] nvarchar(7) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.komisyon_muhasebe_kayitlari', N'rezervasyon_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[komisyon_muhasebe_kayitlari] ADD [rezervasyon_id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.komisyon_muhasebe_kayitlari', N'otel_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[komisyon_muhasebe_kayitlari] ADD [otel_id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.komisyon_muhasebe_kayitlari', N'partner_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[komisyon_muhasebe_kayitlari] ADD [partner_id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.komisyon_muhasebe_kayitlari', N'fatura_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[komisyon_muhasebe_kayitlari] ADD [fatura_id] bigint NULL;
END
GO
IF COL_LENGTH(N'dbo.komisyon_muhasebe_kayitlari', N'toplam_rezervasyon_tutari') IS NULL
BEGIN
    ALTER TABLE [dbo].[komisyon_muhasebe_kayitlari] ADD [toplam_rezervasyon_tutari] decimal(10,2) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.komisyon_muhasebe_kayitlari', N'komisyon_orani') IS NULL
BEGIN
    ALTER TABLE [dbo].[komisyon_muhasebe_kayitlari] ADD [komisyon_orani] decimal(5,2) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.komisyon_muhasebe_kayitlari', N'komisyon_tutari') IS NULL
BEGIN
    ALTER TABLE [dbo].[komisyon_muhasebe_kayitlari] ADD [komisyon_tutari] decimal(10,2) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.komisyon_muhasebe_kayitlari', N'ek_kesintiler') IS NULL
BEGIN
    ALTER TABLE [dbo].[komisyon_muhasebe_kayitlari] ADD [ek_kesintiler] decimal(10,2) DEFAULT ((0.00)) NULL;
END
GO
IF COL_LENGTH(N'dbo.komisyon_muhasebe_kayitlari', N'net_otele_odenecek') IS NULL
BEGIN
    ALTER TABLE [dbo].[komisyon_muhasebe_kayitlari] ADD [net_otele_odenecek] decimal(10,2) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.komisyon_muhasebe_kayitlari', N'otele_odeme_durumu') IS NULL
BEGIN
    ALTER TABLE [dbo].[komisyon_muhasebe_kayitlari] ADD [otele_odeme_durumu] nvarchar(255) NULL;
END
GO
IF COL_LENGTH(N'dbo.komisyon_muhasebe_kayitlari', N'otele_odeme_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[komisyon_muhasebe_kayitlari] ADD [otele_odeme_tarihi] date NULL;
END
GO
IF COL_LENGTH(N'dbo.komisyon_muhasebe_kayitlari', N'otele_odeme_referansi') IS NULL
BEGIN
    ALTER TABLE [dbo].[komisyon_muhasebe_kayitlari] ADD [otele_odeme_referansi] nvarchar(50) NULL;
END
GO
IF COL_LENGTH(N'dbo.komisyon_muhasebe_kayitlari', N'odeme_emri_no') IS NULL
BEGIN
    ALTER TABLE [dbo].[komisyon_muhasebe_kayitlari] ADD [odeme_emri_no] nvarchar(30) NULL;
END
GO
IF COL_LENGTH(N'dbo.komisyon_muhasebe_kayitlari', N'muhasebe_hesap_kodu') IS NULL
BEGIN
    ALTER TABLE [dbo].[komisyon_muhasebe_kayitlari] ADD [muhasebe_hesap_kodu] nvarchar(20) NULL;
END
GO
IF COL_LENGTH(N'dbo.komisyon_muhasebe_kayitlari', N'karsi_hesap_kodu') IS NULL
BEGIN
    ALTER TABLE [dbo].[komisyon_muhasebe_kayitlari] ADD [karsi_hesap_kodu] nvarchar(20) NULL;
END
GO
IF COL_LENGTH(N'dbo.komisyon_muhasebe_kayitlari', N'yevmiye_no') IS NULL
BEGIN
    ALTER TABLE [dbo].[komisyon_muhasebe_kayitlari] ADD [yevmiye_no] nvarchar(20) NULL;
END
GO
IF COL_LENGTH(N'dbo.komisyon_muhasebe_kayitlari', N'fis_no') IS NULL
BEGIN
    ALTER TABLE [dbo].[komisyon_muhasebe_kayitlari] ADD [fis_no] nvarchar(20) NULL;
END
GO
IF COL_LENGTH(N'dbo.komisyon_muhasebe_kayitlari', N'mutabakat_durumu') IS NULL
BEGIN
    ALTER TABLE [dbo].[komisyon_muhasebe_kayitlari] ADD [mutabakat_durumu] nvarchar(255) NULL;
END
GO
IF COL_LENGTH(N'dbo.komisyon_muhasebe_kayitlari', N'mutabakat_gonderim_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[komisyon_muhasebe_kayitlari] ADD [mutabakat_gonderim_tarihi] datetime2(0) NULL;
END
GO
IF COL_LENGTH(N'dbo.komisyon_muhasebe_kayitlari', N'mutabakat_onay_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[komisyon_muhasebe_kayitlari] ADD [mutabakat_onay_tarihi] datetime2(0) NULL;
END
GO
IF COL_LENGTH(N'dbo.komisyon_muhasebe_kayitlari', N'mutabakat_notu') IS NULL
BEGIN
    ALTER TABLE [dbo].[komisyon_muhasebe_kayitlari] ADD [mutabakat_notu] nvarchar(max) NULL;
END
GO
IF COL_LENGTH(N'dbo.komisyon_muhasebe_kayitlari', N'itiraz_var_mi') IS NULL
BEGIN
    ALTER TABLE [dbo].[komisyon_muhasebe_kayitlari] ADD [itiraz_var_mi] bit DEFAULT ((0)) NULL;
END
GO
IF COL_LENGTH(N'dbo.komisyon_muhasebe_kayitlari', N'itiraz_nedeni') IS NULL
BEGIN
    ALTER TABLE [dbo].[komisyon_muhasebe_kayitlari] ADD [itiraz_nedeni] nvarchar(500) NULL;
END
GO
IF COL_LENGTH(N'dbo.komisyon_muhasebe_kayitlari', N'itiraz_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[komisyon_muhasebe_kayitlari] ADD [itiraz_tarihi] datetime2(0) NULL;
END
GO
IF COL_LENGTH(N'dbo.komisyon_muhasebe_kayitlari', N'itiraz_cozum_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[komisyon_muhasebe_kayitlari] ADD [itiraz_cozum_tarihi] datetime2(0) NULL;
END
GO
IF COL_LENGTH(N'dbo.komisyon_muhasebe_kayitlari', N'itiraz_cozum_aciklamasi') IS NULL
BEGIN
    ALTER TABLE [dbo].[komisyon_muhasebe_kayitlari] ADD [itiraz_cozum_aciklamasi] nvarchar(max) NULL;
END
GO
IF COL_LENGTH(N'dbo.komisyon_muhasebe_kayitlari', N'duzeltme_tutari') IS NULL
BEGIN
    ALTER TABLE [dbo].[komisyon_muhasebe_kayitlari] ADD [duzeltme_tutari] decimal(10,2) NULL;
END
GO
IF COL_LENGTH(N'dbo.komisyon_muhasebe_kayitlari', N'stopaj_orani') IS NULL
BEGIN
    ALTER TABLE [dbo].[komisyon_muhasebe_kayitlari] ADD [stopaj_orani] decimal(5,2) DEFAULT ((0.00)) NULL;
END
GO
IF COL_LENGTH(N'dbo.komisyon_muhasebe_kayitlari', N'stopaj_tutari') IS NULL
BEGIN
    ALTER TABLE [dbo].[komisyon_muhasebe_kayitlari] ADD [stopaj_tutari] decimal(10,2) DEFAULT ((0.00)) NULL;
END
GO
IF COL_LENGTH(N'dbo.komisyon_muhasebe_kayitlari', N'kdv_orani') IS NULL
BEGIN
    ALTER TABLE [dbo].[komisyon_muhasebe_kayitlari] ADD [kdv_orani] decimal(5,2) DEFAULT ((20.00)) NULL;
END
GO
IF COL_LENGTH(N'dbo.komisyon_muhasebe_kayitlari', N'kdv_tutari') IS NULL
BEGIN
    ALTER TABLE [dbo].[komisyon_muhasebe_kayitlari] ADD [kdv_tutari] decimal(10,2) DEFAULT ((0.00)) NULL;
END
GO
IF COL_LENGTH(N'dbo.komisyon_muhasebe_kayitlari', N'olusturulma_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[komisyon_muhasebe_kayitlari] ADD [olusturulma_tarihi] datetime2(0) DEFAULT (sysutcdatetime()) NULL;
END
GO
IF COL_LENGTH(N'dbo.komisyon_muhasebe_kayitlari', N'guncellenme_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[komisyon_muhasebe_kayitlari] ADD [guncellenme_tarihi] datetime2(0) NULL;
END
GO
IF COL_LENGTH(N'dbo.komisyon_muhasebe_kayitlari', N'olusturan_admin_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[komisyon_muhasebe_kayitlari] ADD [olusturan_admin_id] bigint NULL;
END
GO
IF COL_LENGTH(N'dbo.komisyon_muhasebe_kayitlari', N'onaylayan_finans_admin_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[komisyon_muhasebe_kayitlari] ADD [onaylayan_finans_admin_id] bigint NULL;
END
GO
