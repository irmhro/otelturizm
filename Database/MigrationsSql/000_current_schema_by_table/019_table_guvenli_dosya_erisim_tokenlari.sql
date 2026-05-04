SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.guvenli_dosya_erisim_tokenlari', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[guvenli_dosya_erisim_tokenlari]
    (
        [id] bigint IDENTITY(1,1) NOT NULL,
        [guvenli_dosya_id] bigint NOT NULL,
        [erisim_tokeni] nvarchar(64) NOT NULL,
        [kullanici_id] bigint NOT NULL,
        [hesap_tipi] nvarchar(30) NOT NULL,
        [kullanim_sayisi] int CONSTRAINT [DF__guvenli_d__kulla__47A6A41B] DEFAULT ((0)) NOT NULL,
        [maksimum_kullanim_sayisi] int NULL,
        [gecerlilik_tarihi] datetime2(0) NOT NULL,
        [son_erisim_tarihi] datetime2(0) NULL,
        [iptal_tarihi] datetime2(0) NULL,
        [olusturulma_tarihi] datetime2(0) CONSTRAINT [DF__guvenli_d__olust__489AC854] DEFAULT (sysutcdatetime()) NOT NULL,
        CONSTRAINT [PK_guvenli_dosya_erisim_tokenlari] PRIMARY KEY CLUSTERED ([id] ASC)
    );
END
GO

-- Missing column repair blocks for existing databases.
IF COL_LENGTH(N'dbo.guvenli_dosya_erisim_tokenlari', N'id') IS NULL
BEGIN
    ALTER TABLE [dbo].[guvenli_dosya_erisim_tokenlari] ADD [id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.guvenli_dosya_erisim_tokenlari', N'guvenli_dosya_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[guvenli_dosya_erisim_tokenlari] ADD [guvenli_dosya_id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.guvenli_dosya_erisim_tokenlari', N'erisim_tokeni') IS NULL
BEGIN
    ALTER TABLE [dbo].[guvenli_dosya_erisim_tokenlari] ADD [erisim_tokeni] nvarchar(64) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.guvenli_dosya_erisim_tokenlari', N'kullanici_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[guvenli_dosya_erisim_tokenlari] ADD [kullanici_id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.guvenli_dosya_erisim_tokenlari', N'hesap_tipi') IS NULL
BEGIN
    ALTER TABLE [dbo].[guvenli_dosya_erisim_tokenlari] ADD [hesap_tipi] nvarchar(30) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.guvenli_dosya_erisim_tokenlari', N'kullanim_sayisi') IS NULL
BEGIN
    ALTER TABLE [dbo].[guvenli_dosya_erisim_tokenlari] ADD [kullanim_sayisi] int DEFAULT ((0)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.guvenli_dosya_erisim_tokenlari', N'maksimum_kullanim_sayisi') IS NULL
BEGIN
    ALTER TABLE [dbo].[guvenli_dosya_erisim_tokenlari] ADD [maksimum_kullanim_sayisi] int NULL;
END
GO
IF COL_LENGTH(N'dbo.guvenli_dosya_erisim_tokenlari', N'gecerlilik_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[guvenli_dosya_erisim_tokenlari] ADD [gecerlilik_tarihi] datetime2(0) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.guvenli_dosya_erisim_tokenlari', N'son_erisim_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[guvenli_dosya_erisim_tokenlari] ADD [son_erisim_tarihi] datetime2(0) NULL;
END
GO
IF COL_LENGTH(N'dbo.guvenli_dosya_erisim_tokenlari', N'iptal_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[guvenli_dosya_erisim_tokenlari] ADD [iptal_tarihi] datetime2(0) NULL;
END
GO
IF COL_LENGTH(N'dbo.guvenli_dosya_erisim_tokenlari', N'olusturulma_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[guvenli_dosya_erisim_tokenlari] ADD [olusturulma_tarihi] datetime2(0) DEFAULT (sysutcdatetime()) NOT NULL;
END
GO
