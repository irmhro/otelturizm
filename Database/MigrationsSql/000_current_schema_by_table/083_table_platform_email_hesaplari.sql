SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.platform_email_hesaplari', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[platform_email_hesaplari]
    (
        [id] bigint IDENTITY(1,1) NOT NULL,
        [hesap_kodu] nvarchar(80) NOT NULL,
        [hesap_adi] nvarchar(180) NOT NULL,
        [email_adresi] nvarchar(320) NOT NULL,
        [gelen_protokol] nvarchar(20) CONSTRAINT [DF_platform_email_hesaplari_gelen_protokol] DEFAULT (N'IMAP') NOT NULL,
        [gelen_sunucu] nvarchar(255) NOT NULL,
        [gelen_port] int NOT NULL,
        [gelen_ssl] bit CONSTRAINT [DF_platform_email_hesaplari_gelen_ssl] DEFAULT ((1)) NOT NULL,
        [giden_sunucu] nvarchar(255) NOT NULL,
        [giden_port] int NOT NULL,
        [giden_guvenlik_tipi] nvarchar(40) CONSTRAINT [DF_platform_email_hesaplari_giden_guvenlik] DEFAULT (N'SSL/TLS') NOT NULL,
        [kullanici_adi] nvarchar(320) NOT NULL,
        [sifre_sifreli] nvarchar(max) NOT NULL,
        [aktif_mi] bit CONSTRAINT [DF_platform_email_hesaplari_aktif] DEFAULT ((1)) NOT NULL,
        [varsayilan_gonderen_mi] bit CONSTRAINT [DF_platform_email_hesaplari_varsayilan] DEFAULT ((0)) NOT NULL,
        [son_senkron_tarihi] datetime2(7) NULL,
        [son_hata_mesaji] nvarchar(1000) NULL,
        [olusturulma_tarihi] datetime2(7) CONSTRAINT [DF_platform_email_hesaplari_olusturulma] DEFAULT (sysutcdatetime()) NOT NULL,
        [guncellenme_tarihi] datetime2(7) CONSTRAINT [DF_platform_email_hesaplari_guncellenme] DEFAULT (sysutcdatetime()) NOT NULL,
        CONSTRAINT [PK__platform__3213E83F6A272B74] PRIMARY KEY CLUSTERED ([id] ASC)
    );
END
GO

-- Missing column repair blocks for existing databases.
IF COL_LENGTH(N'dbo.platform_email_hesaplari', N'id') IS NULL
BEGIN
    ALTER TABLE [dbo].[platform_email_hesaplari] ADD [id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.platform_email_hesaplari', N'hesap_kodu') IS NULL
BEGIN
    ALTER TABLE [dbo].[platform_email_hesaplari] ADD [hesap_kodu] nvarchar(80) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.platform_email_hesaplari', N'hesap_adi') IS NULL
BEGIN
    ALTER TABLE [dbo].[platform_email_hesaplari] ADD [hesap_adi] nvarchar(180) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.platform_email_hesaplari', N'email_adresi') IS NULL
BEGIN
    ALTER TABLE [dbo].[platform_email_hesaplari] ADD [email_adresi] nvarchar(320) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.platform_email_hesaplari', N'gelen_protokol') IS NULL
BEGIN
    ALTER TABLE [dbo].[platform_email_hesaplari] ADD [gelen_protokol] nvarchar(20) DEFAULT (N'IMAP') NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.platform_email_hesaplari', N'gelen_sunucu') IS NULL
BEGIN
    ALTER TABLE [dbo].[platform_email_hesaplari] ADD [gelen_sunucu] nvarchar(255) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.platform_email_hesaplari', N'gelen_port') IS NULL
BEGIN
    ALTER TABLE [dbo].[platform_email_hesaplari] ADD [gelen_port] int NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.platform_email_hesaplari', N'gelen_ssl') IS NULL
BEGIN
    ALTER TABLE [dbo].[platform_email_hesaplari] ADD [gelen_ssl] bit DEFAULT ((1)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.platform_email_hesaplari', N'giden_sunucu') IS NULL
BEGIN
    ALTER TABLE [dbo].[platform_email_hesaplari] ADD [giden_sunucu] nvarchar(255) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.platform_email_hesaplari', N'giden_port') IS NULL
BEGIN
    ALTER TABLE [dbo].[platform_email_hesaplari] ADD [giden_port] int NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.platform_email_hesaplari', N'giden_guvenlik_tipi') IS NULL
BEGIN
    ALTER TABLE [dbo].[platform_email_hesaplari] ADD [giden_guvenlik_tipi] nvarchar(40) DEFAULT (N'SSL/TLS') NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.platform_email_hesaplari', N'kullanici_adi') IS NULL
BEGIN
    ALTER TABLE [dbo].[platform_email_hesaplari] ADD [kullanici_adi] nvarchar(320) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.platform_email_hesaplari', N'sifre_sifreli') IS NULL
BEGIN
    ALTER TABLE [dbo].[platform_email_hesaplari] ADD [sifre_sifreli] nvarchar(max) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.platform_email_hesaplari', N'aktif_mi') IS NULL
BEGIN
    ALTER TABLE [dbo].[platform_email_hesaplari] ADD [aktif_mi] bit DEFAULT ((1)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.platform_email_hesaplari', N'varsayilan_gonderen_mi') IS NULL
BEGIN
    ALTER TABLE [dbo].[platform_email_hesaplari] ADD [varsayilan_gonderen_mi] bit DEFAULT ((0)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.platform_email_hesaplari', N'son_senkron_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[platform_email_hesaplari] ADD [son_senkron_tarihi] datetime2(7) NULL;
END
GO
IF COL_LENGTH(N'dbo.platform_email_hesaplari', N'son_hata_mesaji') IS NULL
BEGIN
    ALTER TABLE [dbo].[platform_email_hesaplari] ADD [son_hata_mesaji] nvarchar(1000) NULL;
END
GO
IF COL_LENGTH(N'dbo.platform_email_hesaplari', N'olusturulma_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[platform_email_hesaplari] ADD [olusturulma_tarihi] datetime2(7) DEFAULT (sysutcdatetime()) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.platform_email_hesaplari', N'guncellenme_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[platform_email_hesaplari] ADD [guncellenme_tarihi] datetime2(7) DEFAULT (sysutcdatetime()) NOT NULL;
END
GO
