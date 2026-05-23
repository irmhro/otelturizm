SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.api_loglari', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[api_loglari]
    (
        [id] bigint IDENTITY(1,1) NOT NULL,
        [request_id] nvarchar(36) NOT NULL,
        [api_versiyonu] nvarchar(10) NULL,
        [endpoint] nvarchar(500) NOT NULL,
        [http_method] nvarchar(10) NOT NULL,
        [request_headers] nvarchar(max) NULL,
        [request_body] nvarchar(max) NULL,
        [request_ip] nvarchar(45) NULL,
        [user_agent] nvarchar(max) NULL,
        [response_status] smallint NULL,
        [response_headers] nvarchar(max) NULL,
        [response_body] nvarchar(max) NULL,
        [response_size] int NULL,
        [kullanici_id] bigint NULL,
        [api_key_id] int NULL,
        [partner_id] bigint NULL,
        [islem_suresi_ms] int NULL,
        [bellek_kullanimi_kb] int NULL,
        [basarili_mi] bit CONSTRAINT [DF__api_logla__basar__72C60C4A] DEFAULT ((1)) NULL,
        [hata_mesaji] nvarchar(max) NULL,
        [hata_kodu] nvarchar(20) NULL,
        [baslangic_tarihi] datetime2(0) CONSTRAINT [DF__api_logla__basla__73BA3083] DEFAULT (sysutcdatetime()) NULL,
        [bitis_tarihi] datetime2(0) NULL,
        CONSTRAINT [PK_api_loglari] PRIMARY KEY CLUSTERED ([id] ASC)
    );
END
GO

-- Missing column repair blocks for existing databases.
IF COL_LENGTH(N'dbo.api_loglari', N'id') IS NULL
BEGIN
    ALTER TABLE [dbo].[api_loglari] ADD [id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.api_loglari', N'request_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[api_loglari] ADD [request_id] nvarchar(36) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.api_loglari', N'api_versiyonu') IS NULL
BEGIN
    ALTER TABLE [dbo].[api_loglari] ADD [api_versiyonu] nvarchar(10) NULL;
END
GO
IF COL_LENGTH(N'dbo.api_loglari', N'endpoint') IS NULL
BEGIN
    ALTER TABLE [dbo].[api_loglari] ADD [endpoint] nvarchar(500) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.api_loglari', N'http_method') IS NULL
BEGIN
    ALTER TABLE [dbo].[api_loglari] ADD [http_method] nvarchar(10) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.api_loglari', N'request_headers') IS NULL
BEGIN
    ALTER TABLE [dbo].[api_loglari] ADD [request_headers] nvarchar(max) NULL;
END
GO
IF COL_LENGTH(N'dbo.api_loglari', N'request_body') IS NULL
BEGIN
    ALTER TABLE [dbo].[api_loglari] ADD [request_body] nvarchar(max) NULL;
END
GO
IF COL_LENGTH(N'dbo.api_loglari', N'request_ip') IS NULL
BEGIN
    ALTER TABLE [dbo].[api_loglari] ADD [request_ip] nvarchar(45) NULL;
END
GO
IF COL_LENGTH(N'dbo.api_loglari', N'user_agent') IS NULL
BEGIN
    ALTER TABLE [dbo].[api_loglari] ADD [user_agent] nvarchar(max) NULL;
END
GO
IF COL_LENGTH(N'dbo.api_loglari', N'response_status') IS NULL
BEGIN
    ALTER TABLE [dbo].[api_loglari] ADD [response_status] smallint NULL;
END
GO
IF COL_LENGTH(N'dbo.api_loglari', N'response_headers') IS NULL
BEGIN
    ALTER TABLE [dbo].[api_loglari] ADD [response_headers] nvarchar(max) NULL;
END
GO
IF COL_LENGTH(N'dbo.api_loglari', N'response_body') IS NULL
BEGIN
    ALTER TABLE [dbo].[api_loglari] ADD [response_body] nvarchar(max) NULL;
END
GO
IF COL_LENGTH(N'dbo.api_loglari', N'response_size') IS NULL
BEGIN
    ALTER TABLE [dbo].[api_loglari] ADD [response_size] int NULL;
END
GO
IF COL_LENGTH(N'dbo.api_loglari', N'kullanici_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[api_loglari] ADD [kullanici_id] bigint NULL;
END
GO
IF COL_LENGTH(N'dbo.api_loglari', N'api_key_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[api_loglari] ADD [api_key_id] int NULL;
END
GO
IF COL_LENGTH(N'dbo.api_loglari', N'partner_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[api_loglari] ADD [partner_id] bigint NULL;
END
GO
IF COL_LENGTH(N'dbo.api_loglari', N'islem_suresi_ms') IS NULL
BEGIN
    ALTER TABLE [dbo].[api_loglari] ADD [islem_suresi_ms] int NULL;
END
GO
IF COL_LENGTH(N'dbo.api_loglari', N'bellek_kullanimi_kb') IS NULL
BEGIN
    ALTER TABLE [dbo].[api_loglari] ADD [bellek_kullanimi_kb] int NULL;
END
GO
IF COL_LENGTH(N'dbo.api_loglari', N'basarili_mi') IS NULL
BEGIN
    ALTER TABLE [dbo].[api_loglari] ADD [basarili_mi] bit DEFAULT ((1)) NULL;
END
GO
IF COL_LENGTH(N'dbo.api_loglari', N'hata_mesaji') IS NULL
BEGIN
    ALTER TABLE [dbo].[api_loglari] ADD [hata_mesaji] nvarchar(max) NULL;
END
GO
IF COL_LENGTH(N'dbo.api_loglari', N'hata_kodu') IS NULL
BEGIN
    ALTER TABLE [dbo].[api_loglari] ADD [hata_kodu] nvarchar(20) NULL;
END
GO
IF COL_LENGTH(N'dbo.api_loglari', N'baslangic_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[api_loglari] ADD [baslangic_tarihi] datetime2(0) DEFAULT (sysutcdatetime()) NULL;
END
GO
IF COL_LENGTH(N'dbo.api_loglari', N'bitis_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[api_loglari] ADD [bitis_tarihi] datetime2(0) NULL;
END
GO
