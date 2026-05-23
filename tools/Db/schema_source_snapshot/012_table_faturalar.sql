SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.faturalar', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[faturalar]
    (
        [id] bigint IDENTITY(1,1) NOT NULL,
        [fatura_no] nvarchar(30) NOT NULL,
        [fatura_tarihi] date NOT NULL,
        [fatura_turu] nvarchar(255) NOT NULL,
        [rezervasyon_id] bigint NULL,
        [otel_id] bigint NULL,
        [kullanici_id] bigint NULL,
        [partner_id] bigint NULL,
        [firma_id] bigint NULL,
        [odeme_islem_id] bigint NULL,
        [fatura_kesen] nvarchar(255) NOT NULL,
        [fatura_kesen_unvan] nvarchar(200) NOT NULL,
        [fatura_kesen_vergi_dairesi] nvarchar(100) NOT NULL,
        [fatura_kesen_vergi_no] nvarchar(20) NOT NULL,
        [fatura_kesen_adres] nvarchar(max) NOT NULL,
        [fatura_alici_unvan] nvarchar(200) NOT NULL,
        [fatura_alici_vergi_dairesi] nvarchar(100) NULL,
        [fatura_alici_vergi_no] nvarchar(20) NULL,
        [fatura_alici_tc_no] nvarchar(11) NULL,
        [fatura_alici_adres] nvarchar(max) NOT NULL,
        [fatura_alici_eposta] nvarchar(100) NULL,
        [ara_toplam] decimal(10,2) NOT NULL,
        [kdv_orani] decimal(5,2) CONSTRAINT [DF__faturalar__kdv_o__2BFE89A6] DEFAULT ((20.00)) NULL,
        [kdv_tutari] decimal(10,2) NOT NULL,
        [diger_vergiler] decimal(10,2) CONSTRAINT [DF__faturalar__diger__2CF2ADDF] DEFAULT ((0.00)) NULL,
        [konaklama_vergisi_orani] decimal(5,2) CONSTRAINT [DF__faturalar__konak__2DE6D218] DEFAULT ((2.00)) NULL,
        [konaklama_vergisi_tutari] decimal(10,2) CONSTRAINT [DF__faturalar__konak__2EDAF651] DEFAULT ((0.00)) NULL,
        [genel_toplam] decimal(10,2) NOT NULL,
        [para_birimi] nvarchar(3) NULL,
        [yalniz_yaziyla] nvarchar(500) NULL,
        [e_fatura_uuid] nvarchar(36) NULL,
        [e_fatura_durumu] nvarchar(255) NULL,
        [e_fatura_gonderim_tarihi] datetime2(0) NULL,
        [e_fatura_onay_tarihi] datetime2(0) NULL,
        [e_fatura_entegrasyon_turu] nvarchar(255) NULL,
        [entegrator_adi] nvarchar(50) NULL,
        [fatura_pdf_yolu] nvarchar(500) NULL,
        [fatura_html_yolu] nvarchar(500) NULL,
        [fatura_xml_yolu] nvarchar(500) NULL,
        [fatura_durumu] nvarchar(255) NULL,
        [iptal_nedeni] nvarchar(500) NULL,
        [iptal_tarihi] datetime2(0) NULL,
        [iptal_eden_admin_id] bigint NULL,
        [fatura_notu] nvarchar(max) NULL,
        [siparis_no] nvarchar(50) NULL,
        [olusturulma_tarihi] datetime2(0) CONSTRAINT [DF__faturalar__olust__2FCF1A8A] DEFAULT (sysutcdatetime()) NULL,
        [guncellenme_tarihi] datetime2(0) NULL,
        [vade_tarihi] date NULL,
        [odeme_tarihi] date NULL,
        CONSTRAINT [PK_faturalar] PRIMARY KEY CLUSTERED ([id] ASC)
    );
END
GO

-- Missing column repair blocks for existing databases.
IF COL_LENGTH(N'dbo.faturalar', N'id') IS NULL
BEGIN
    ALTER TABLE [dbo].[faturalar] ADD [id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.faturalar', N'fatura_no') IS NULL
BEGIN
    ALTER TABLE [dbo].[faturalar] ADD [fatura_no] nvarchar(30) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.faturalar', N'fatura_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[faturalar] ADD [fatura_tarihi] date NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.faturalar', N'fatura_turu') IS NULL
BEGIN
    ALTER TABLE [dbo].[faturalar] ADD [fatura_turu] nvarchar(255) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.faturalar', N'rezervasyon_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[faturalar] ADD [rezervasyon_id] bigint NULL;
END
GO
IF COL_LENGTH(N'dbo.faturalar', N'otel_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[faturalar] ADD [otel_id] bigint NULL;
END
GO
IF COL_LENGTH(N'dbo.faturalar', N'kullanici_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[faturalar] ADD [kullanici_id] bigint NULL;
END
GO
IF COL_LENGTH(N'dbo.faturalar', N'partner_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[faturalar] ADD [partner_id] bigint NULL;
END
GO
IF COL_LENGTH(N'dbo.faturalar', N'firma_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[faturalar] ADD [firma_id] bigint NULL;
END
GO
IF COL_LENGTH(N'dbo.faturalar', N'odeme_islem_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[faturalar] ADD [odeme_islem_id] bigint NULL;
END
GO
IF COL_LENGTH(N'dbo.faturalar', N'fatura_kesen') IS NULL
BEGIN
    ALTER TABLE [dbo].[faturalar] ADD [fatura_kesen] nvarchar(255) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.faturalar', N'fatura_kesen_unvan') IS NULL
BEGIN
    ALTER TABLE [dbo].[faturalar] ADD [fatura_kesen_unvan] nvarchar(200) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.faturalar', N'fatura_kesen_vergi_dairesi') IS NULL
BEGIN
    ALTER TABLE [dbo].[faturalar] ADD [fatura_kesen_vergi_dairesi] nvarchar(100) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.faturalar', N'fatura_kesen_vergi_no') IS NULL
BEGIN
    ALTER TABLE [dbo].[faturalar] ADD [fatura_kesen_vergi_no] nvarchar(20) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.faturalar', N'fatura_kesen_adres') IS NULL
BEGIN
    ALTER TABLE [dbo].[faturalar] ADD [fatura_kesen_adres] nvarchar(max) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.faturalar', N'fatura_alici_unvan') IS NULL
BEGIN
    ALTER TABLE [dbo].[faturalar] ADD [fatura_alici_unvan] nvarchar(200) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.faturalar', N'fatura_alici_vergi_dairesi') IS NULL
BEGIN
    ALTER TABLE [dbo].[faturalar] ADD [fatura_alici_vergi_dairesi] nvarchar(100) NULL;
END
GO
IF COL_LENGTH(N'dbo.faturalar', N'fatura_alici_vergi_no') IS NULL
BEGIN
    ALTER TABLE [dbo].[faturalar] ADD [fatura_alici_vergi_no] nvarchar(20) NULL;
END
GO
IF COL_LENGTH(N'dbo.faturalar', N'fatura_alici_tc_no') IS NULL
BEGIN
    ALTER TABLE [dbo].[faturalar] ADD [fatura_alici_tc_no] nvarchar(11) NULL;
END
GO
IF COL_LENGTH(N'dbo.faturalar', N'fatura_alici_adres') IS NULL
BEGIN
    ALTER TABLE [dbo].[faturalar] ADD [fatura_alici_adres] nvarchar(max) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.faturalar', N'fatura_alici_eposta') IS NULL
BEGIN
    ALTER TABLE [dbo].[faturalar] ADD [fatura_alici_eposta] nvarchar(100) NULL;
END
GO
IF COL_LENGTH(N'dbo.faturalar', N'ara_toplam') IS NULL
BEGIN
    ALTER TABLE [dbo].[faturalar] ADD [ara_toplam] decimal(10,2) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.faturalar', N'kdv_orani') IS NULL
BEGIN
    ALTER TABLE [dbo].[faturalar] ADD [kdv_orani] decimal(5,2) DEFAULT ((20.00)) NULL;
END
GO
IF COL_LENGTH(N'dbo.faturalar', N'kdv_tutari') IS NULL
BEGIN
    ALTER TABLE [dbo].[faturalar] ADD [kdv_tutari] decimal(10,2) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.faturalar', N'diger_vergiler') IS NULL
BEGIN
    ALTER TABLE [dbo].[faturalar] ADD [diger_vergiler] decimal(10,2) DEFAULT ((0.00)) NULL;
END
GO
IF COL_LENGTH(N'dbo.faturalar', N'konaklama_vergisi_orani') IS NULL
BEGIN
    ALTER TABLE [dbo].[faturalar] ADD [konaklama_vergisi_orani] decimal(5,2) DEFAULT ((2.00)) NULL;
END
GO
IF COL_LENGTH(N'dbo.faturalar', N'konaklama_vergisi_tutari') IS NULL
BEGIN
    ALTER TABLE [dbo].[faturalar] ADD [konaklama_vergisi_tutari] decimal(10,2) DEFAULT ((0.00)) NULL;
END
GO
IF COL_LENGTH(N'dbo.faturalar', N'genel_toplam') IS NULL
BEGIN
    ALTER TABLE [dbo].[faturalar] ADD [genel_toplam] decimal(10,2) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.faturalar', N'para_birimi') IS NULL
BEGIN
    ALTER TABLE [dbo].[faturalar] ADD [para_birimi] nvarchar(3) NULL;
END
GO
IF COL_LENGTH(N'dbo.faturalar', N'yalniz_yaziyla') IS NULL
BEGIN
    ALTER TABLE [dbo].[faturalar] ADD [yalniz_yaziyla] nvarchar(500) NULL;
END
GO
IF COL_LENGTH(N'dbo.faturalar', N'e_fatura_uuid') IS NULL
BEGIN
    ALTER TABLE [dbo].[faturalar] ADD [e_fatura_uuid] nvarchar(36) NULL;
END
GO
IF COL_LENGTH(N'dbo.faturalar', N'e_fatura_durumu') IS NULL
BEGIN
    ALTER TABLE [dbo].[faturalar] ADD [e_fatura_durumu] nvarchar(255) NULL;
END
GO
IF COL_LENGTH(N'dbo.faturalar', N'e_fatura_gonderim_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[faturalar] ADD [e_fatura_gonderim_tarihi] datetime2(0) NULL;
END
GO
IF COL_LENGTH(N'dbo.faturalar', N'e_fatura_onay_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[faturalar] ADD [e_fatura_onay_tarihi] datetime2(0) NULL;
END
GO
IF COL_LENGTH(N'dbo.faturalar', N'e_fatura_entegrasyon_turu') IS NULL
BEGIN
    ALTER TABLE [dbo].[faturalar] ADD [e_fatura_entegrasyon_turu] nvarchar(255) NULL;
END
GO
IF COL_LENGTH(N'dbo.faturalar', N'entegrator_adi') IS NULL
BEGIN
    ALTER TABLE [dbo].[faturalar] ADD [entegrator_adi] nvarchar(50) NULL;
END
GO
IF COL_LENGTH(N'dbo.faturalar', N'fatura_pdf_yolu') IS NULL
BEGIN
    ALTER TABLE [dbo].[faturalar] ADD [fatura_pdf_yolu] nvarchar(500) NULL;
END
GO
IF COL_LENGTH(N'dbo.faturalar', N'fatura_html_yolu') IS NULL
BEGIN
    ALTER TABLE [dbo].[faturalar] ADD [fatura_html_yolu] nvarchar(500) NULL;
END
GO
IF COL_LENGTH(N'dbo.faturalar', N'fatura_xml_yolu') IS NULL
BEGIN
    ALTER TABLE [dbo].[faturalar] ADD [fatura_xml_yolu] nvarchar(500) NULL;
END
GO
IF COL_LENGTH(N'dbo.faturalar', N'fatura_durumu') IS NULL
BEGIN
    ALTER TABLE [dbo].[faturalar] ADD [fatura_durumu] nvarchar(255) NULL;
END
GO
IF COL_LENGTH(N'dbo.faturalar', N'iptal_nedeni') IS NULL
BEGIN
    ALTER TABLE [dbo].[faturalar] ADD [iptal_nedeni] nvarchar(500) NULL;
END
GO
IF COL_LENGTH(N'dbo.faturalar', N'iptal_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[faturalar] ADD [iptal_tarihi] datetime2(0) NULL;
END
GO
IF COL_LENGTH(N'dbo.faturalar', N'iptal_eden_admin_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[faturalar] ADD [iptal_eden_admin_id] bigint NULL;
END
GO
IF COL_LENGTH(N'dbo.faturalar', N'fatura_notu') IS NULL
BEGIN
    ALTER TABLE [dbo].[faturalar] ADD [fatura_notu] nvarchar(max) NULL;
END
GO
IF COL_LENGTH(N'dbo.faturalar', N'siparis_no') IS NULL
BEGIN
    ALTER TABLE [dbo].[faturalar] ADD [siparis_no] nvarchar(50) NULL;
END
GO
IF COL_LENGTH(N'dbo.faturalar', N'olusturulma_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[faturalar] ADD [olusturulma_tarihi] datetime2(0) DEFAULT (sysutcdatetime()) NULL;
END
GO
IF COL_LENGTH(N'dbo.faturalar', N'guncellenme_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[faturalar] ADD [guncellenme_tarihi] datetime2(0) NULL;
END
GO
IF COL_LENGTH(N'dbo.faturalar', N'vade_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[faturalar] ADD [vade_tarihi] date NULL;
END
GO
IF COL_LENGTH(N'dbo.faturalar', N'odeme_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[faturalar] ADD [odeme_tarihi] date NULL;
END
GO
