SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.rezervasyon_taslaklari', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[rezervasyon_taslaklari]
    (
        [id] bigint IDENTITY(1,1) NOT NULL,
        [taslak_kodu] nvarchar(36) NOT NULL,
        [user_id] bigint NULL,
        [session_anahtari] nvarchar(100) NULL,
        [kaynak] nvarchar(255) NOT NULL,
        [durum] nvarchar(255) NOT NULL,
        [otel_id] bigint NOT NULL,
        [oda_tip_id] bigint NULL,
        [tamamlanan_rezervasyon_id] bigint NULL,
        [misafir_ad_soyad] nvarchar(120) NULL,
        [misafir_eposta] nvarchar(120) NULL,
        [misafir_telefon] nvarchar(20) NULL,
        [misafir_sehir] nvarchar(100) NULL,
        [misafir_ilce] nvarchar(100) NULL,
        [misafir_mahalle] nvarchar(120) NULL,
        [misafir_adres] nvarchar(max) NULL,
        [giris_tarihi] date NOT NULL,
        [cikis_tarihi] date NOT NULL,
        [yetiskin_sayisi] tinyint CONSTRAINT [DF__rezervasy__yetis__61F08603] DEFAULT ((2)) NOT NULL,
        [cocuk_sayisi] tinyint CONSTRAINT [DF__rezervasy__cocuk__62E4AA3C] DEFAULT ((0)) NOT NULL,
        [oda_sayisi] tinyint CONSTRAINT [DF__rezervasy__oda_s__63D8CE75] DEFAULT ((1)) NOT NULL,
        [gecelik_fiyat] decimal(10,2) NULL,
        [vergi_tutari] decimal(10,2) NULL,
        [toplam_tutar] decimal(10,2) NULL,
        [para_birimi] nvarchar(3) NOT NULL,
        [donus_url] nvarchar(500) NULL,
        [profil_tamamlanma_url] nvarchar(500) NULL,
        [notlar] nvarchar(max) NULL,
        [metadata] nvarchar(max) NULL,
        [son_aktivite_tarihi] datetime2(0) CONSTRAINT [DF__rezervasy__son_a__64CCF2AE] DEFAULT (sysutcdatetime()) NOT NULL,
        [son_bildirim_tarihi] datetime2(0) NULL,
        [gecerlilik_tarihi] datetime2(0) NULL,
        [olusturulma_tarihi] datetime2(0) CONSTRAINT [DF__rezervasy__olust__65C116E7] DEFAULT (sysutcdatetime()) NOT NULL,
        [guncellenme_tarihi] datetime2(0) NULL,
        [net_oda_tutari] decimal(18,2) NULL,
        [kdv_orani] decimal(9,4) NULL,
        [kdv_tutari] decimal(18,2) NULL,
        [konaklama_vergisi_orani] decimal(9,4) NULL,
        [konaklama_vergisi_tutari] decimal(18,2) NULL,
        CONSTRAINT [PK_rezervasyon_taslaklari] PRIMARY KEY CLUSTERED ([id] ASC)
    );
END
GO

-- Missing column repair blocks for existing databases.
IF COL_LENGTH(N'dbo.rezervasyon_taslaklari', N'id') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyon_taslaklari] ADD [id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.rezervasyon_taslaklari', N'taslak_kodu') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyon_taslaklari] ADD [taslak_kodu] nvarchar(36) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.rezervasyon_taslaklari', N'user_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyon_taslaklari] ADD [user_id] bigint NULL;
END
GO
IF COL_LENGTH(N'dbo.rezervasyon_taslaklari', N'session_anahtari') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyon_taslaklari] ADD [session_anahtari] nvarchar(100) NULL;
END
GO
IF COL_LENGTH(N'dbo.rezervasyon_taslaklari', N'kaynak') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyon_taslaklari] ADD [kaynak] nvarchar(255) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.rezervasyon_taslaklari', N'durum') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyon_taslaklari] ADD [durum] nvarchar(255) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.rezervasyon_taslaklari', N'otel_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyon_taslaklari] ADD [otel_id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.rezervasyon_taslaklari', N'oda_tip_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyon_taslaklari] ADD [oda_tip_id] bigint NULL;
END
GO
IF COL_LENGTH(N'dbo.rezervasyon_taslaklari', N'tamamlanan_rezervasyon_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyon_taslaklari] ADD [tamamlanan_rezervasyon_id] bigint NULL;
END
GO
IF COL_LENGTH(N'dbo.rezervasyon_taslaklari', N'misafir_ad_soyad') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyon_taslaklari] ADD [misafir_ad_soyad] nvarchar(120) NULL;
END
GO
IF COL_LENGTH(N'dbo.rezervasyon_taslaklari', N'misafir_eposta') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyon_taslaklari] ADD [misafir_eposta] nvarchar(120) NULL;
END
GO
IF COL_LENGTH(N'dbo.rezervasyon_taslaklari', N'misafir_telefon') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyon_taslaklari] ADD [misafir_telefon] nvarchar(20) NULL;
END
GO
IF COL_LENGTH(N'dbo.rezervasyon_taslaklari', N'misafir_sehir') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyon_taslaklari] ADD [misafir_sehir] nvarchar(100) NULL;
END
GO
IF COL_LENGTH(N'dbo.rezervasyon_taslaklari', N'misafir_ilce') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyon_taslaklari] ADD [misafir_ilce] nvarchar(100) NULL;
END
GO
IF COL_LENGTH(N'dbo.rezervasyon_taslaklari', N'misafir_mahalle') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyon_taslaklari] ADD [misafir_mahalle] nvarchar(120) NULL;
END
GO
IF COL_LENGTH(N'dbo.rezervasyon_taslaklari', N'misafir_adres') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyon_taslaklari] ADD [misafir_adres] nvarchar(max) NULL;
END
GO
IF COL_LENGTH(N'dbo.rezervasyon_taslaklari', N'giris_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyon_taslaklari] ADD [giris_tarihi] date NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.rezervasyon_taslaklari', N'cikis_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyon_taslaklari] ADD [cikis_tarihi] date NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.rezervasyon_taslaklari', N'yetiskin_sayisi') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyon_taslaklari] ADD [yetiskin_sayisi] tinyint DEFAULT ((2)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.rezervasyon_taslaklari', N'cocuk_sayisi') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyon_taslaklari] ADD [cocuk_sayisi] tinyint DEFAULT ((0)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.rezervasyon_taslaklari', N'oda_sayisi') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyon_taslaklari] ADD [oda_sayisi] tinyint DEFAULT ((1)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.rezervasyon_taslaklari', N'gecelik_fiyat') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyon_taslaklari] ADD [gecelik_fiyat] decimal(10,2) NULL;
END
GO
IF COL_LENGTH(N'dbo.rezervasyon_taslaklari', N'vergi_tutari') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyon_taslaklari] ADD [vergi_tutari] decimal(10,2) NULL;
END
GO
IF COL_LENGTH(N'dbo.rezervasyon_taslaklari', N'toplam_tutar') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyon_taslaklari] ADD [toplam_tutar] decimal(10,2) NULL;
END
GO
IF COL_LENGTH(N'dbo.rezervasyon_taslaklari', N'para_birimi') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyon_taslaklari] ADD [para_birimi] nvarchar(3) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.rezervasyon_taslaklari', N'donus_url') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyon_taslaklari] ADD [donus_url] nvarchar(500) NULL;
END
GO
IF COL_LENGTH(N'dbo.rezervasyon_taslaklari', N'profil_tamamlanma_url') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyon_taslaklari] ADD [profil_tamamlanma_url] nvarchar(500) NULL;
END
GO
IF COL_LENGTH(N'dbo.rezervasyon_taslaklari', N'notlar') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyon_taslaklari] ADD [notlar] nvarchar(max) NULL;
END
GO
IF COL_LENGTH(N'dbo.rezervasyon_taslaklari', N'metadata') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyon_taslaklari] ADD [metadata] nvarchar(max) NULL;
END
GO
IF COL_LENGTH(N'dbo.rezervasyon_taslaklari', N'son_aktivite_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyon_taslaklari] ADD [son_aktivite_tarihi] datetime2(0) DEFAULT (sysutcdatetime()) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.rezervasyon_taslaklari', N'son_bildirim_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyon_taslaklari] ADD [son_bildirim_tarihi] datetime2(0) NULL;
END
GO
IF COL_LENGTH(N'dbo.rezervasyon_taslaklari', N'gecerlilik_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyon_taslaklari] ADD [gecerlilik_tarihi] datetime2(0) NULL;
END
GO
IF COL_LENGTH(N'dbo.rezervasyon_taslaklari', N'olusturulma_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyon_taslaklari] ADD [olusturulma_tarihi] datetime2(0) DEFAULT (sysutcdatetime()) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.rezervasyon_taslaklari', N'guncellenme_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyon_taslaklari] ADD [guncellenme_tarihi] datetime2(0) NULL;
END
GO
IF COL_LENGTH(N'dbo.rezervasyon_taslaklari', N'net_oda_tutari') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyon_taslaklari] ADD [net_oda_tutari] decimal(18,2) NULL;
END
GO
IF COL_LENGTH(N'dbo.rezervasyon_taslaklari', N'kdv_orani') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyon_taslaklari] ADD [kdv_orani] decimal(9,4) NULL;
END
GO
IF COL_LENGTH(N'dbo.rezervasyon_taslaklari', N'kdv_tutari') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyon_taslaklari] ADD [kdv_tutari] decimal(18,2) NULL;
END
GO
IF COL_LENGTH(N'dbo.rezervasyon_taslaklari', N'konaklama_vergisi_orani') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyon_taslaklari] ADD [konaklama_vergisi_orani] decimal(9,4) NULL;
END
GO
IF COL_LENGTH(N'dbo.rezervasyon_taslaklari', N'konaklama_vergisi_tutari') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyon_taslaklari] ADD [konaklama_vergisi_tutari] decimal(18,2) NULL;
END
GO
