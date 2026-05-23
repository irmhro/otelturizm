SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.email_dogrulama_tokenlari', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[email_dogrulama_tokenlari]
    (
        [id] bigint IDENTITY(1,1) NOT NULL,
        [kullanici_id] bigint NOT NULL,
        [eposta] nvarchar(100) NOT NULL,
        [token] nvarchar(96) NOT NULL,
        [dogrulama_kodu] nvarchar(8) NOT NULL,
        [kullanildi_mi] bit CONSTRAINT [DF__email_dog__kulla__19DFD96B] DEFAULT ((0)) NOT NULL,
        [deneme_sayisi] smallint CONSTRAINT [DF__email_dog__denem__1AD3FDA4] DEFAULT ((0)) NOT NULL,
        [maksimum_deneme] smallint CONSTRAINT [DF__email_dog__maksi__1BC821DD] DEFAULT ((5)) NOT NULL,
        [ip_adresi] nvarchar(45) NULL,
        [user_agent] nvarchar(500) NULL,
        [gecerlilik_suresi] datetime2(0) NOT NULL,
        [kullanilma_tarihi] datetime2(0) NULL,
        [olusturulma_tarihi] datetime2(0) CONSTRAINT [DF__email_dog__olust__1CBC4616] DEFAULT (sysutcdatetime()) NOT NULL,
        CONSTRAINT [PK_email_dogrulama_tokenlari] PRIMARY KEY CLUSTERED ([id] ASC)
    );
END
GO

-- Missing column repair blocks for existing databases.
IF COL_LENGTH(N'dbo.email_dogrulama_tokenlari', N'id') IS NULL
BEGIN
    ALTER TABLE [dbo].[email_dogrulama_tokenlari] ADD [id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.email_dogrulama_tokenlari', N'kullanici_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[email_dogrulama_tokenlari] ADD [kullanici_id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.email_dogrulama_tokenlari', N'eposta') IS NULL
BEGIN
    ALTER TABLE [dbo].[email_dogrulama_tokenlari] ADD [eposta] nvarchar(100) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.email_dogrulama_tokenlari', N'token') IS NULL
BEGIN
    ALTER TABLE [dbo].[email_dogrulama_tokenlari] ADD [token] nvarchar(96) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.email_dogrulama_tokenlari', N'dogrulama_kodu') IS NULL
BEGIN
    ALTER TABLE [dbo].[email_dogrulama_tokenlari] ADD [dogrulama_kodu] nvarchar(8) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.email_dogrulama_tokenlari', N'kullanildi_mi') IS NULL
BEGIN
    ALTER TABLE [dbo].[email_dogrulama_tokenlari] ADD [kullanildi_mi] bit DEFAULT ((0)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.email_dogrulama_tokenlari', N'deneme_sayisi') IS NULL
BEGIN
    ALTER TABLE [dbo].[email_dogrulama_tokenlari] ADD [deneme_sayisi] smallint DEFAULT ((0)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.email_dogrulama_tokenlari', N'maksimum_deneme') IS NULL
BEGIN
    ALTER TABLE [dbo].[email_dogrulama_tokenlari] ADD [maksimum_deneme] smallint DEFAULT ((5)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.email_dogrulama_tokenlari', N'ip_adresi') IS NULL
BEGIN
    ALTER TABLE [dbo].[email_dogrulama_tokenlari] ADD [ip_adresi] nvarchar(45) NULL;
END
GO
IF COL_LENGTH(N'dbo.email_dogrulama_tokenlari', N'user_agent') IS NULL
BEGIN
    ALTER TABLE [dbo].[email_dogrulama_tokenlari] ADD [user_agent] nvarchar(500) NULL;
END
GO
IF COL_LENGTH(N'dbo.email_dogrulama_tokenlari', N'gecerlilik_suresi') IS NULL
BEGIN
    ALTER TABLE [dbo].[email_dogrulama_tokenlari] ADD [gecerlilik_suresi] datetime2(0) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.email_dogrulama_tokenlari', N'kullanilma_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[email_dogrulama_tokenlari] ADD [kullanilma_tarihi] datetime2(0) NULL;
END
GO
IF COL_LENGTH(N'dbo.email_dogrulama_tokenlari', N'olusturulma_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[email_dogrulama_tokenlari] ADD [olusturulma_tarihi] datetime2(0) DEFAULT (sysutcdatetime()) NOT NULL;
END
GO
