SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.whatsapp_mesaj_loglari', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[whatsapp_mesaj_loglari]
    (
        [id] bigint IDENTITY(1,1) NOT NULL,
        [kullanici_id] bigint NULL,
        [telefon_e164] nvarchar(32) NOT NULL,
        [template_name] nvarchar(120) NOT NULL,
        [meta_mesaj_id] nvarchar(120) NULL,
        [delivery_status] nvarchar(40) NULL,
        [request_payload] nvarchar(max) NULL,
        [response_payload] nvarchar(max) NULL,
        [error_code] nvarchar(50) NULL,
        [error_message] nvarchar(500) NULL,
        [olusturulma_tarihi] datetime2(7) CONSTRAINT [DF_whatsapp_mesaj_loglari_created] DEFAULT (sysutcdatetime()) NOT NULL,
        [guncellenme_tarihi] datetime2(7) CONSTRAINT [DF_whatsapp_mesaj_loglari_updated] DEFAULT (sysutcdatetime()) NOT NULL,
        CONSTRAINT [PK__whatsapp__3213E83FC64386F7] PRIMARY KEY CLUSTERED ([id] ASC)
    );
END
GO

-- Missing column repair blocks for existing databases.
IF COL_LENGTH(N'dbo.whatsapp_mesaj_loglari', N'id') IS NULL
BEGIN
    ALTER TABLE [dbo].[whatsapp_mesaj_loglari] ADD [id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.whatsapp_mesaj_loglari', N'kullanici_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[whatsapp_mesaj_loglari] ADD [kullanici_id] bigint NULL;
END
GO
IF COL_LENGTH(N'dbo.whatsapp_mesaj_loglari', N'telefon_e164') IS NULL
BEGIN
    ALTER TABLE [dbo].[whatsapp_mesaj_loglari] ADD [telefon_e164] nvarchar(32) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.whatsapp_mesaj_loglari', N'template_name') IS NULL
BEGIN
    ALTER TABLE [dbo].[whatsapp_mesaj_loglari] ADD [template_name] nvarchar(120) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.whatsapp_mesaj_loglari', N'meta_mesaj_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[whatsapp_mesaj_loglari] ADD [meta_mesaj_id] nvarchar(120) NULL;
END
GO
IF COL_LENGTH(N'dbo.whatsapp_mesaj_loglari', N'delivery_status') IS NULL
BEGIN
    ALTER TABLE [dbo].[whatsapp_mesaj_loglari] ADD [delivery_status] nvarchar(40) NULL;
END
GO
IF COL_LENGTH(N'dbo.whatsapp_mesaj_loglari', N'request_payload') IS NULL
BEGIN
    ALTER TABLE [dbo].[whatsapp_mesaj_loglari] ADD [request_payload] nvarchar(max) NULL;
END
GO
IF COL_LENGTH(N'dbo.whatsapp_mesaj_loglari', N'response_payload') IS NULL
BEGIN
    ALTER TABLE [dbo].[whatsapp_mesaj_loglari] ADD [response_payload] nvarchar(max) NULL;
END
GO
IF COL_LENGTH(N'dbo.whatsapp_mesaj_loglari', N'error_code') IS NULL
BEGIN
    ALTER TABLE [dbo].[whatsapp_mesaj_loglari] ADD [error_code] nvarchar(50) NULL;
END
GO
IF COL_LENGTH(N'dbo.whatsapp_mesaj_loglari', N'error_message') IS NULL
BEGIN
    ALTER TABLE [dbo].[whatsapp_mesaj_loglari] ADD [error_message] nvarchar(500) NULL;
END
GO
IF COL_LENGTH(N'dbo.whatsapp_mesaj_loglari', N'olusturulma_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[whatsapp_mesaj_loglari] ADD [olusturulma_tarihi] datetime2(7) DEFAULT (sysutcdatetime()) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.whatsapp_mesaj_loglari', N'guncellenme_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[whatsapp_mesaj_loglari] ADD [guncellenme_tarihi] datetime2(7) DEFAULT (sysutcdatetime()) NOT NULL;
END
GO
