SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.telefon_dogrulama_tokenlari', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[telefon_dogrulama_tokenlari]
    (
        [id] bigint IDENTITY(1,1) NOT NULL,
        [kullanici_id] bigint NOT NULL,
        [telefon_raw] nvarchar(32) NULL,
        [telefon_e164] nvarchar(32) NOT NULL,
        [dogrulama_kodu_hash] nvarchar(128) NOT NULL,
        [dogrulama_kanali] nvarchar(30) CONSTRAINT [DF_telefon_dogrulama_tokenlari_kanal] DEFAULT ('whatsapp') NOT NULL,
        [meta_mesaj_id] nvarchar(120) NULL,
        [talep_durumu] nvarchar(40) CONSTRAINT [DF_telefon_dogrulama_tokenlari_durum] DEFAULT ('Hazirlaniyor') NOT NULL,
        [deneme_sayisi] smallint CONSTRAINT [DF_telefon_dogrulama_tokenlari_deneme] DEFAULT ((0)) NOT NULL,
        [maksimum_deneme] smallint CONSTRAINT [DF_telefon_dogrulama_tokenlari_max] DEFAULT ((5)) NOT NULL,
        [kullanildi_mi] bit CONSTRAINT [DF_telefon_dogrulama_tokenlari_kullanildi] DEFAULT ((0)) NOT NULL,
        [kullanilma_tarihi] datetime2(7) NULL,
        [gecerlilik_suresi] datetime2(7) NOT NULL,
        [son_hata_mesaji] nvarchar(500) NULL,
        [ip_adresi] nvarchar(80) NULL,
        [user_agent] nvarchar(500) NULL,
        [olusturulma_tarihi] datetime2(7) CONSTRAINT [DF_telefon_dogrulama_tokenlari_created] DEFAULT (sysutcdatetime()) NOT NULL,
        [guncellenme_tarihi] datetime2(7) CONSTRAINT [DF_telefon_dogrulama_tokenlari_updated] DEFAULT (sysutcdatetime()) NOT NULL,
        CONSTRAINT [PK__telefon___3213E83FC68C7A89] PRIMARY KEY CLUSTERED ([id] ASC)
    );
END
GO

-- Missing column repair blocks for existing databases.
IF COL_LENGTH(N'dbo.telefon_dogrulama_tokenlari', N'id') IS NULL
BEGIN
    ALTER TABLE [dbo].[telefon_dogrulama_tokenlari] ADD [id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.telefon_dogrulama_tokenlari', N'kullanici_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[telefon_dogrulama_tokenlari] ADD [kullanici_id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.telefon_dogrulama_tokenlari', N'telefon_raw') IS NULL
BEGIN
    ALTER TABLE [dbo].[telefon_dogrulama_tokenlari] ADD [telefon_raw] nvarchar(32) NULL;
END
GO
IF COL_LENGTH(N'dbo.telefon_dogrulama_tokenlari', N'telefon_e164') IS NULL
BEGIN
    ALTER TABLE [dbo].[telefon_dogrulama_tokenlari] ADD [telefon_e164] nvarchar(32) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.telefon_dogrulama_tokenlari', N'dogrulama_kodu_hash') IS NULL
BEGIN
    ALTER TABLE [dbo].[telefon_dogrulama_tokenlari] ADD [dogrulama_kodu_hash] nvarchar(128) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.telefon_dogrulama_tokenlari', N'dogrulama_kanali') IS NULL
BEGIN
    ALTER TABLE [dbo].[telefon_dogrulama_tokenlari] ADD [dogrulama_kanali] nvarchar(30) DEFAULT ('whatsapp') NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.telefon_dogrulama_tokenlari', N'meta_mesaj_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[telefon_dogrulama_tokenlari] ADD [meta_mesaj_id] nvarchar(120) NULL;
END
GO
IF COL_LENGTH(N'dbo.telefon_dogrulama_tokenlari', N'talep_durumu') IS NULL
BEGIN
    ALTER TABLE [dbo].[telefon_dogrulama_tokenlari] ADD [talep_durumu] nvarchar(40) DEFAULT ('Hazirlaniyor') NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.telefon_dogrulama_tokenlari', N'deneme_sayisi') IS NULL
BEGIN
    ALTER TABLE [dbo].[telefon_dogrulama_tokenlari] ADD [deneme_sayisi] smallint DEFAULT ((0)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.telefon_dogrulama_tokenlari', N'maksimum_deneme') IS NULL
BEGIN
    ALTER TABLE [dbo].[telefon_dogrulama_tokenlari] ADD [maksimum_deneme] smallint DEFAULT ((5)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.telefon_dogrulama_tokenlari', N'kullanildi_mi') IS NULL
BEGIN
    ALTER TABLE [dbo].[telefon_dogrulama_tokenlari] ADD [kullanildi_mi] bit DEFAULT ((0)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.telefon_dogrulama_tokenlari', N'kullanilma_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[telefon_dogrulama_tokenlari] ADD [kullanilma_tarihi] datetime2(7) NULL;
END
GO
IF COL_LENGTH(N'dbo.telefon_dogrulama_tokenlari', N'gecerlilik_suresi') IS NULL
BEGIN
    ALTER TABLE [dbo].[telefon_dogrulama_tokenlari] ADD [gecerlilik_suresi] datetime2(7) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.telefon_dogrulama_tokenlari', N'son_hata_mesaji') IS NULL
BEGIN
    ALTER TABLE [dbo].[telefon_dogrulama_tokenlari] ADD [son_hata_mesaji] nvarchar(500) NULL;
END
GO
IF COL_LENGTH(N'dbo.telefon_dogrulama_tokenlari', N'ip_adresi') IS NULL
BEGIN
    ALTER TABLE [dbo].[telefon_dogrulama_tokenlari] ADD [ip_adresi] nvarchar(80) NULL;
END
GO
IF COL_LENGTH(N'dbo.telefon_dogrulama_tokenlari', N'user_agent') IS NULL
BEGIN
    ALTER TABLE [dbo].[telefon_dogrulama_tokenlari] ADD [user_agent] nvarchar(500) NULL;
END
GO
IF COL_LENGTH(N'dbo.telefon_dogrulama_tokenlari', N'olusturulma_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[telefon_dogrulama_tokenlari] ADD [olusturulma_tarihi] datetime2(7) DEFAULT (sysutcdatetime()) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.telefon_dogrulama_tokenlari', N'guncellenme_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[telefon_dogrulama_tokenlari] ADD [guncellenme_tarihi] datetime2(7) DEFAULT (sysutcdatetime()) NOT NULL;
END
GO
