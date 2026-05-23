SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.odeme_islemleri', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[odeme_islemleri]
    (
        [id] bigint IDENTITY(1,1) NOT NULL,
        [islem_no] nvarchar(30) NOT NULL,
        [rezervasyon_id] bigint NOT NULL,
        [kullanici_id] bigint NOT NULL,
        [otel_id] bigint NOT NULL,
        [odeme_turu] nvarchar(255) NOT NULL,
        [odeme_yontemi] nvarchar(255) NOT NULL,
        [odeme_durumu] nvarchar(255) NULL,
        [tutar] decimal(10,2) NOT NULL,
        [komisyon_tutari] decimal(10,2) CONSTRAINT [DF__odeme_isl__komis__7167D3BD] DEFAULT ((0.00)) NULL,
        [vergi_tutari] decimal(10,2) CONSTRAINT [DF__odeme_isl__vergi__725BF7F6] DEFAULT ((0.00)) NULL,
        [toplam_tahsilat] decimal(10,2) NOT NULL,
        [para_birimi] nvarchar(3) NULL,
        [kur_orani] decimal(10,6) CONSTRAINT [DF__odeme_isl__kur_o__73501C2F] DEFAULT ((1.000000)) NULL,
        [orijinal_tutar] decimal(10,2) NULL,
        [orijinal_para_birimi] nvarchar(3) NULL,
        [taksit_sayisi] tinyint CONSTRAINT [DF__odeme_isl__taksi__74444068] DEFAULT ((1)) NULL,
        [taksit_sirasi] tinyint CONSTRAINT [DF__odeme_isl__taksi__753864A1] DEFAULT ((1)) NULL,
        [ana_odeme_id] bigint NULL,
        [kart_sahibi_adi] nvarchar(100) NULL,
        [kart_numarasi_masked] nvarchar(20) NULL,
        [kart_tipi] nvarchar(255) NULL,
        [kart_son_kullanma] nvarchar(5) NULL,
        [banka_adi] nvarchar(100) NULL,
        [iban_masked] nvarchar(30) NULL,
        [odeme_saglayici] nvarchar(255) NULL,
        [saglayici_islem_no] nvarchar(100) NULL,
        [saglayici_onay_kodu] nvarchar(50) NULL,
        [saglayici_hata_kodu] nvarchar(20) NULL,
        [saglayici_hata_mesaji] nvarchar(500) NULL,
        [uc_d_secure_kullanildi] bit CONSTRAINT [DF__odeme_isl__uc_d___762C88DA] DEFAULT ((0)) NULL,
        [uc_d_secure_durumu] nvarchar(255) NULL,
        [iade_edilebilir_tutar] decimal(10,2) NULL,
        [iade_edilen_tutar] decimal(10,2) CONSTRAINT [DF__odeme_isl__iade___7720AD13] DEFAULT ((0.00)) NULL,
        [iade_nedeni] nvarchar(255) NULL,
        [iade_aciklamasi] nvarchar(max) NULL,
        [iade_tarihi] datetime2(0) NULL,
        [iade_eden_admin_id] bigint NULL,
        [iptal_kesintisi_orani] decimal(5,2) NULL,
        [iptal_kesintisi_tutari] decimal(10,2) NULL,
        [fatura_id] bigint NULL,
        [odeme_ip_adresi] nvarchar(45) NULL,
        [odeme_cihaz_bilgisi] nvarchar(255) NULL,
        [odeme_konum] nvarchar(100) NULL,
        [risk_puani] tinyint CONSTRAINT [DF__odeme_isl__risk___7814D14C] DEFAULT ((0)) NULL,
        [risk_kontrolu_sonucu] nvarchar(255) NULL,
        [manuel_onay_gerektirir] bit CONSTRAINT [DF__odeme_isl__manue__7908F585] DEFAULT ((0)) NULL,
        [manuel_onaylayan_admin_id] bigint NULL,
        [manuel_onay_tarihi] datetime2(0) NULL,
        [odeme_baslangic_tarihi] datetime2(0) CONSTRAINT [DF__odeme_isl__odeme__79FD19BE] DEFAULT (sysutcdatetime()) NULL,
        [odeme_tamamlanma_tarihi] datetime2(0) NULL,
        [son_durum_degisikligi] datetime2(0) NULL,
        CONSTRAINT [PK_odeme_islemleri] PRIMARY KEY CLUSTERED ([id] ASC)
    );
END
GO

-- Missing column repair blocks for existing databases.
IF COL_LENGTH(N'dbo.odeme_islemleri', N'id') IS NULL
BEGIN
    ALTER TABLE [dbo].[odeme_islemleri] ADD [id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.odeme_islemleri', N'islem_no') IS NULL
BEGIN
    ALTER TABLE [dbo].[odeme_islemleri] ADD [islem_no] nvarchar(30) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.odeme_islemleri', N'rezervasyon_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[odeme_islemleri] ADD [rezervasyon_id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.odeme_islemleri', N'kullanici_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[odeme_islemleri] ADD [kullanici_id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.odeme_islemleri', N'otel_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[odeme_islemleri] ADD [otel_id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.odeme_islemleri', N'odeme_turu') IS NULL
BEGIN
    ALTER TABLE [dbo].[odeme_islemleri] ADD [odeme_turu] nvarchar(255) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.odeme_islemleri', N'odeme_yontemi') IS NULL
BEGIN
    ALTER TABLE [dbo].[odeme_islemleri] ADD [odeme_yontemi] nvarchar(255) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.odeme_islemleri', N'odeme_durumu') IS NULL
BEGIN
    ALTER TABLE [dbo].[odeme_islemleri] ADD [odeme_durumu] nvarchar(255) NULL;
END
GO
IF COL_LENGTH(N'dbo.odeme_islemleri', N'tutar') IS NULL
BEGIN
    ALTER TABLE [dbo].[odeme_islemleri] ADD [tutar] decimal(10,2) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.odeme_islemleri', N'komisyon_tutari') IS NULL
BEGIN
    ALTER TABLE [dbo].[odeme_islemleri] ADD [komisyon_tutari] decimal(10,2) DEFAULT ((0.00)) NULL;
END
GO
IF COL_LENGTH(N'dbo.odeme_islemleri', N'vergi_tutari') IS NULL
BEGIN
    ALTER TABLE [dbo].[odeme_islemleri] ADD [vergi_tutari] decimal(10,2) DEFAULT ((0.00)) NULL;
END
GO
IF COL_LENGTH(N'dbo.odeme_islemleri', N'toplam_tahsilat') IS NULL
BEGIN
    ALTER TABLE [dbo].[odeme_islemleri] ADD [toplam_tahsilat] decimal(10,2) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.odeme_islemleri', N'para_birimi') IS NULL
BEGIN
    ALTER TABLE [dbo].[odeme_islemleri] ADD [para_birimi] nvarchar(3) NULL;
END
GO
IF COL_LENGTH(N'dbo.odeme_islemleri', N'kur_orani') IS NULL
BEGIN
    ALTER TABLE [dbo].[odeme_islemleri] ADD [kur_orani] decimal(10,6) DEFAULT ((1.000000)) NULL;
END
GO
IF COL_LENGTH(N'dbo.odeme_islemleri', N'orijinal_tutar') IS NULL
BEGIN
    ALTER TABLE [dbo].[odeme_islemleri] ADD [orijinal_tutar] decimal(10,2) NULL;
END
GO
IF COL_LENGTH(N'dbo.odeme_islemleri', N'orijinal_para_birimi') IS NULL
BEGIN
    ALTER TABLE [dbo].[odeme_islemleri] ADD [orijinal_para_birimi] nvarchar(3) NULL;
END
GO
IF COL_LENGTH(N'dbo.odeme_islemleri', N'taksit_sayisi') IS NULL
BEGIN
    ALTER TABLE [dbo].[odeme_islemleri] ADD [taksit_sayisi] tinyint DEFAULT ((1)) NULL;
END
GO
IF COL_LENGTH(N'dbo.odeme_islemleri', N'taksit_sirasi') IS NULL
BEGIN
    ALTER TABLE [dbo].[odeme_islemleri] ADD [taksit_sirasi] tinyint DEFAULT ((1)) NULL;
END
GO
IF COL_LENGTH(N'dbo.odeme_islemleri', N'ana_odeme_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[odeme_islemleri] ADD [ana_odeme_id] bigint NULL;
END
GO
IF COL_LENGTH(N'dbo.odeme_islemleri', N'kart_sahibi_adi') IS NULL
BEGIN
    ALTER TABLE [dbo].[odeme_islemleri] ADD [kart_sahibi_adi] nvarchar(100) NULL;
END
GO
IF COL_LENGTH(N'dbo.odeme_islemleri', N'kart_numarasi_masked') IS NULL
BEGIN
    ALTER TABLE [dbo].[odeme_islemleri] ADD [kart_numarasi_masked] nvarchar(20) NULL;
END
GO
IF COL_LENGTH(N'dbo.odeme_islemleri', N'kart_tipi') IS NULL
BEGIN
    ALTER TABLE [dbo].[odeme_islemleri] ADD [kart_tipi] nvarchar(255) NULL;
END
GO
IF COL_LENGTH(N'dbo.odeme_islemleri', N'kart_son_kullanma') IS NULL
BEGIN
    ALTER TABLE [dbo].[odeme_islemleri] ADD [kart_son_kullanma] nvarchar(5) NULL;
END
GO
IF COL_LENGTH(N'dbo.odeme_islemleri', N'banka_adi') IS NULL
BEGIN
    ALTER TABLE [dbo].[odeme_islemleri] ADD [banka_adi] nvarchar(100) NULL;
END
GO
IF COL_LENGTH(N'dbo.odeme_islemleri', N'iban_masked') IS NULL
BEGIN
    ALTER TABLE [dbo].[odeme_islemleri] ADD [iban_masked] nvarchar(30) NULL;
END
GO
IF COL_LENGTH(N'dbo.odeme_islemleri', N'odeme_saglayici') IS NULL
BEGIN
    ALTER TABLE [dbo].[odeme_islemleri] ADD [odeme_saglayici] nvarchar(255) NULL;
END
GO
IF COL_LENGTH(N'dbo.odeme_islemleri', N'saglayici_islem_no') IS NULL
BEGIN
    ALTER TABLE [dbo].[odeme_islemleri] ADD [saglayici_islem_no] nvarchar(100) NULL;
END
GO
IF COL_LENGTH(N'dbo.odeme_islemleri', N'saglayici_onay_kodu') IS NULL
BEGIN
    ALTER TABLE [dbo].[odeme_islemleri] ADD [saglayici_onay_kodu] nvarchar(50) NULL;
END
GO
IF COL_LENGTH(N'dbo.odeme_islemleri', N'saglayici_hata_kodu') IS NULL
BEGIN
    ALTER TABLE [dbo].[odeme_islemleri] ADD [saglayici_hata_kodu] nvarchar(20) NULL;
END
GO
IF COL_LENGTH(N'dbo.odeme_islemleri', N'saglayici_hata_mesaji') IS NULL
BEGIN
    ALTER TABLE [dbo].[odeme_islemleri] ADD [saglayici_hata_mesaji] nvarchar(500) NULL;
END
GO
IF COL_LENGTH(N'dbo.odeme_islemleri', N'uc_d_secure_kullanildi') IS NULL
BEGIN
    ALTER TABLE [dbo].[odeme_islemleri] ADD [uc_d_secure_kullanildi] bit DEFAULT ((0)) NULL;
END
GO
IF COL_LENGTH(N'dbo.odeme_islemleri', N'uc_d_secure_durumu') IS NULL
BEGIN
    ALTER TABLE [dbo].[odeme_islemleri] ADD [uc_d_secure_durumu] nvarchar(255) NULL;
END
GO
IF COL_LENGTH(N'dbo.odeme_islemleri', N'iade_edilebilir_tutar') IS NULL
BEGIN
    ALTER TABLE [dbo].[odeme_islemleri] ADD [iade_edilebilir_tutar] decimal(10,2) NULL;
END
GO
IF COL_LENGTH(N'dbo.odeme_islemleri', N'iade_edilen_tutar') IS NULL
BEGIN
    ALTER TABLE [dbo].[odeme_islemleri] ADD [iade_edilen_tutar] decimal(10,2) DEFAULT ((0.00)) NULL;
END
GO
IF COL_LENGTH(N'dbo.odeme_islemleri', N'iade_nedeni') IS NULL
BEGIN
    ALTER TABLE [dbo].[odeme_islemleri] ADD [iade_nedeni] nvarchar(255) NULL;
END
GO
IF COL_LENGTH(N'dbo.odeme_islemleri', N'iade_aciklamasi') IS NULL
BEGIN
    ALTER TABLE [dbo].[odeme_islemleri] ADD [iade_aciklamasi] nvarchar(max) NULL;
END
GO
IF COL_LENGTH(N'dbo.odeme_islemleri', N'iade_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[odeme_islemleri] ADD [iade_tarihi] datetime2(0) NULL;
END
GO
IF COL_LENGTH(N'dbo.odeme_islemleri', N'iade_eden_admin_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[odeme_islemleri] ADD [iade_eden_admin_id] bigint NULL;
END
GO
IF COL_LENGTH(N'dbo.odeme_islemleri', N'iptal_kesintisi_orani') IS NULL
BEGIN
    ALTER TABLE [dbo].[odeme_islemleri] ADD [iptal_kesintisi_orani] decimal(5,2) NULL;
END
GO
IF COL_LENGTH(N'dbo.odeme_islemleri', N'iptal_kesintisi_tutari') IS NULL
BEGIN
    ALTER TABLE [dbo].[odeme_islemleri] ADD [iptal_kesintisi_tutari] decimal(10,2) NULL;
END
GO
IF COL_LENGTH(N'dbo.odeme_islemleri', N'fatura_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[odeme_islemleri] ADD [fatura_id] bigint NULL;
END
GO
IF COL_LENGTH(N'dbo.odeme_islemleri', N'odeme_ip_adresi') IS NULL
BEGIN
    ALTER TABLE [dbo].[odeme_islemleri] ADD [odeme_ip_adresi] nvarchar(45) NULL;
END
GO
IF COL_LENGTH(N'dbo.odeme_islemleri', N'odeme_cihaz_bilgisi') IS NULL
BEGIN
    ALTER TABLE [dbo].[odeme_islemleri] ADD [odeme_cihaz_bilgisi] nvarchar(255) NULL;
END
GO
IF COL_LENGTH(N'dbo.odeme_islemleri', N'odeme_konum') IS NULL
BEGIN
    ALTER TABLE [dbo].[odeme_islemleri] ADD [odeme_konum] nvarchar(100) NULL;
END
GO
IF COL_LENGTH(N'dbo.odeme_islemleri', N'risk_puani') IS NULL
BEGIN
    ALTER TABLE [dbo].[odeme_islemleri] ADD [risk_puani] tinyint DEFAULT ((0)) NULL;
END
GO
IF COL_LENGTH(N'dbo.odeme_islemleri', N'risk_kontrolu_sonucu') IS NULL
BEGIN
    ALTER TABLE [dbo].[odeme_islemleri] ADD [risk_kontrolu_sonucu] nvarchar(255) NULL;
END
GO
IF COL_LENGTH(N'dbo.odeme_islemleri', N'manuel_onay_gerektirir') IS NULL
BEGIN
    ALTER TABLE [dbo].[odeme_islemleri] ADD [manuel_onay_gerektirir] bit DEFAULT ((0)) NULL;
END
GO
IF COL_LENGTH(N'dbo.odeme_islemleri', N'manuel_onaylayan_admin_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[odeme_islemleri] ADD [manuel_onaylayan_admin_id] bigint NULL;
END
GO
IF COL_LENGTH(N'dbo.odeme_islemleri', N'manuel_onay_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[odeme_islemleri] ADD [manuel_onay_tarihi] datetime2(0) NULL;
END
GO
IF COL_LENGTH(N'dbo.odeme_islemleri', N'odeme_baslangic_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[odeme_islemleri] ADD [odeme_baslangic_tarihi] datetime2(0) DEFAULT (sysutcdatetime()) NULL;
END
GO
IF COL_LENGTH(N'dbo.odeme_islemleri', N'odeme_tamamlanma_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[odeme_islemleri] ADD [odeme_tamamlanma_tarihi] datetime2(0) NULL;
END
GO
IF COL_LENGTH(N'dbo.odeme_islemleri', N'son_durum_degisikligi') IS NULL
BEGIN
    ALTER TABLE [dbo].[odeme_islemleri] ADD [son_durum_degisikligi] datetime2(0) NULL;
END
GO
