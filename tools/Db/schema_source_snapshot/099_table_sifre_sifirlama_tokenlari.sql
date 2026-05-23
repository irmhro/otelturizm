SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.sifre_sifirlama_tokenlari', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[sifre_sifirlama_tokenlari]
    (
        [id] bigint IDENTITY(1,1) NOT NULL,
        [kullanici_id] bigint NOT NULL,
        [eposta] nvarchar(100) NOT NULL,
        [token] nvarchar(96) NOT NULL,
        [ip_adresi] nvarchar(45) NULL,
        [user_agent] nvarchar(500) NULL,
        [kullanildi_mi] bit CONSTRAINT [DF__sifre_sif__kulla__21D600EE] DEFAULT ((0)) NOT NULL,
        [gecerlilik_suresi] datetime2(0) NOT NULL,
        [kullanilma_tarihi] datetime2(0) NULL,
        [olusturulma_tarihi] datetime2(0) CONSTRAINT [DF__sifre_sif__olust__22CA2527] DEFAULT (sysutcdatetime()) NOT NULL,
        CONSTRAINT [PK_sifre_sifirlama_tokenlari] PRIMARY KEY CLUSTERED ([id] ASC)
    );
END
GO

-- Missing column repair blocks for existing databases.
IF COL_LENGTH(N'dbo.sifre_sifirlama_tokenlari', N'id') IS NULL
BEGIN
    ALTER TABLE [dbo].[sifre_sifirlama_tokenlari] ADD [id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.sifre_sifirlama_tokenlari', N'kullanici_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[sifre_sifirlama_tokenlari] ADD [kullanici_id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.sifre_sifirlama_tokenlari', N'eposta') IS NULL
BEGIN
    ALTER TABLE [dbo].[sifre_sifirlama_tokenlari] ADD [eposta] nvarchar(100) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.sifre_sifirlama_tokenlari', N'token') IS NULL
BEGIN
    ALTER TABLE [dbo].[sifre_sifirlama_tokenlari] ADD [token] nvarchar(96) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.sifre_sifirlama_tokenlari', N'ip_adresi') IS NULL
BEGIN
    ALTER TABLE [dbo].[sifre_sifirlama_tokenlari] ADD [ip_adresi] nvarchar(45) NULL;
END
GO
IF COL_LENGTH(N'dbo.sifre_sifirlama_tokenlari', N'user_agent') IS NULL
BEGIN
    ALTER TABLE [dbo].[sifre_sifirlama_tokenlari] ADD [user_agent] nvarchar(500) NULL;
END
GO
IF COL_LENGTH(N'dbo.sifre_sifirlama_tokenlari', N'kullanildi_mi') IS NULL
BEGIN
    ALTER TABLE [dbo].[sifre_sifirlama_tokenlari] ADD [kullanildi_mi] bit DEFAULT ((0)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.sifre_sifirlama_tokenlari', N'gecerlilik_suresi') IS NULL
BEGIN
    ALTER TABLE [dbo].[sifre_sifirlama_tokenlari] ADD [gecerlilik_suresi] datetime2(0) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.sifre_sifirlama_tokenlari', N'kullanilma_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[sifre_sifirlama_tokenlari] ADD [kullanilma_tarihi] datetime2(0) NULL;
END
GO
IF COL_LENGTH(N'dbo.sifre_sifirlama_tokenlari', N'olusturulma_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[sifre_sifirlama_tokenlari] ADD [olusturulma_tarihi] datetime2(0) DEFAULT (sysutcdatetime()) NOT NULL;
END
GO
