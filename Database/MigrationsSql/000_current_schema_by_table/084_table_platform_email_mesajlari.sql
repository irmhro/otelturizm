SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.platform_email_mesajlari', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[platform_email_mesajlari]
    (
        [id] bigint IDENTITY(1,1) NOT NULL,
        [hesap_id] bigint NOT NULL,
        [yon] nvarchar(20) CONSTRAINT [DF_platform_email_mesajlari_yon] DEFAULT (N'Gelen') NOT NULL,
        [klasor] nvarchar(120) CONSTRAINT [DF_platform_email_mesajlari_klasor] DEFAULT (N'INBOX') NOT NULL,
        [uid_degeri] nvarchar(120) NULL,
        [internet_message_id] nvarchar(500) NULL,
        [konu] nvarchar(500) NULL,
        [gonderen] nvarchar(500) NULL,
        [alicilar] nvarchar(max) NULL,
        [cc] nvarchar(max) NULL,
        [tarih_utc] datetime2(7) NULL,
        [ozet] nvarchar(1200) NULL,
        [html_icerik] nvarchar(max) NULL,
        [text_icerik] nvarchar(max) NULL,
        [okunmus_mu] bit CONSTRAINT [DF_platform_email_mesajlari_okunmus] DEFAULT ((0)) NOT NULL,
        [spam_mi] bit CONSTRAINT [DF_platform_email_mesajlari_spam] DEFAULT ((0)) NOT NULL,
        [ilgili_bildirim_log_id] bigint NULL,
        [ham_basliklar] nvarchar(max) NULL,
        [senkron_tarihi] datetime2(7) CONSTRAINT [DF_platform_email_mesajlari_senkron] DEFAULT (sysutcdatetime()) NOT NULL,
        [olusturulma_tarihi] datetime2(7) CONSTRAINT [DF_platform_email_mesajlari_olusturulma] DEFAULT (sysutcdatetime()) NOT NULL,
        [guncellenme_tarihi] datetime2(7) CONSTRAINT [DF_platform_email_mesajlari_guncellenme] DEFAULT (sysutcdatetime()) NOT NULL,
        CONSTRAINT [PK__platform__3213E83F8235AEC2] PRIMARY KEY CLUSTERED ([id] ASC)
    );
END
GO

-- Missing column repair blocks for existing databases.
IF COL_LENGTH(N'dbo.platform_email_mesajlari', N'id') IS NULL
BEGIN
    ALTER TABLE [dbo].[platform_email_mesajlari] ADD [id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.platform_email_mesajlari', N'hesap_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[platform_email_mesajlari] ADD [hesap_id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.platform_email_mesajlari', N'yon') IS NULL
BEGIN
    ALTER TABLE [dbo].[platform_email_mesajlari] ADD [yon] nvarchar(20) DEFAULT (N'Gelen') NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.platform_email_mesajlari', N'klasor') IS NULL
BEGIN
    ALTER TABLE [dbo].[platform_email_mesajlari] ADD [klasor] nvarchar(120) DEFAULT (N'INBOX') NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.platform_email_mesajlari', N'uid_degeri') IS NULL
BEGIN
    ALTER TABLE [dbo].[platform_email_mesajlari] ADD [uid_degeri] nvarchar(120) NULL;
END
GO
IF COL_LENGTH(N'dbo.platform_email_mesajlari', N'internet_message_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[platform_email_mesajlari] ADD [internet_message_id] nvarchar(500) NULL;
END
GO
IF COL_LENGTH(N'dbo.platform_email_mesajlari', N'konu') IS NULL
BEGIN
    ALTER TABLE [dbo].[platform_email_mesajlari] ADD [konu] nvarchar(500) NULL;
END
GO
IF COL_LENGTH(N'dbo.platform_email_mesajlari', N'gonderen') IS NULL
BEGIN
    ALTER TABLE [dbo].[platform_email_mesajlari] ADD [gonderen] nvarchar(500) NULL;
END
GO
IF COL_LENGTH(N'dbo.platform_email_mesajlari', N'alicilar') IS NULL
BEGIN
    ALTER TABLE [dbo].[platform_email_mesajlari] ADD [alicilar] nvarchar(max) NULL;
END
GO
IF COL_LENGTH(N'dbo.platform_email_mesajlari', N'cc') IS NULL
BEGIN
    ALTER TABLE [dbo].[platform_email_mesajlari] ADD [cc] nvarchar(max) NULL;
END
GO
IF COL_LENGTH(N'dbo.platform_email_mesajlari', N'tarih_utc') IS NULL
BEGIN
    ALTER TABLE [dbo].[platform_email_mesajlari] ADD [tarih_utc] datetime2(7) NULL;
END
GO
IF COL_LENGTH(N'dbo.platform_email_mesajlari', N'ozet') IS NULL
BEGIN
    ALTER TABLE [dbo].[platform_email_mesajlari] ADD [ozet] nvarchar(1200) NULL;
END
GO
IF COL_LENGTH(N'dbo.platform_email_mesajlari', N'html_icerik') IS NULL
BEGIN
    ALTER TABLE [dbo].[platform_email_mesajlari] ADD [html_icerik] nvarchar(max) NULL;
END
GO
IF COL_LENGTH(N'dbo.platform_email_mesajlari', N'text_icerik') IS NULL
BEGIN
    ALTER TABLE [dbo].[platform_email_mesajlari] ADD [text_icerik] nvarchar(max) NULL;
END
GO
IF COL_LENGTH(N'dbo.platform_email_mesajlari', N'okunmus_mu') IS NULL
BEGIN
    ALTER TABLE [dbo].[platform_email_mesajlari] ADD [okunmus_mu] bit DEFAULT ((0)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.platform_email_mesajlari', N'spam_mi') IS NULL
BEGIN
    ALTER TABLE [dbo].[platform_email_mesajlari] ADD [spam_mi] bit DEFAULT ((0)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.platform_email_mesajlari', N'ilgili_bildirim_log_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[platform_email_mesajlari] ADD [ilgili_bildirim_log_id] bigint NULL;
END
GO
IF COL_LENGTH(N'dbo.platform_email_mesajlari', N'ham_basliklar') IS NULL
BEGIN
    ALTER TABLE [dbo].[platform_email_mesajlari] ADD [ham_basliklar] nvarchar(max) NULL;
END
GO
IF COL_LENGTH(N'dbo.platform_email_mesajlari', N'senkron_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[platform_email_mesajlari] ADD [senkron_tarihi] datetime2(7) DEFAULT (sysutcdatetime()) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.platform_email_mesajlari', N'olusturulma_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[platform_email_mesajlari] ADD [olusturulma_tarihi] datetime2(7) DEFAULT (sysutcdatetime()) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.platform_email_mesajlari', N'guncellenme_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[platform_email_mesajlari] ADD [guncellenme_tarihi] datetime2(7) DEFAULT (sysutcdatetime()) NOT NULL;
END
GO
