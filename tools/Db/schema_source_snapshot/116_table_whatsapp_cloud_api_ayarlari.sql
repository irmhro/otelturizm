SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.whatsapp_cloud_api_ayarlari', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[whatsapp_cloud_api_ayarlari]
    (
        [id] bigint IDENTITY(1,1) NOT NULL,
        [app_id] nvarchar(100) NULL,
        [app_secret_encrypted] nvarchar(max) NULL,
        [business_account_id] nvarchar(100) NULL,
        [phone_number_id] nvarchar(100) NULL,
        [permanent_access_token_encrypted] nvarchar(max) NULL,
        [webhook_verify_token_encrypted] nvarchar(max) NULL,
        [verification_template_name] nvarchar(120) NULL,
        [default_language_code] nvarchar(20) CONSTRAINT [DF_whatsapp_cloud_api_ayarlari_lang] DEFAULT ('tr') NOT NULL,
        [otp_code_length] tinyint CONSTRAINT [DF_whatsapp_cloud_api_ayarlari_code] DEFAULT ((6)) NOT NULL,
        [otp_ttl_seconds] int CONSTRAINT [DF_whatsapp_cloud_api_ayarlari_ttl] DEFAULT ((300)) NOT NULL,
        [resend_cooldown_seconds] int CONSTRAINT [DF_whatsapp_cloud_api_ayarlari_cooldown] DEFAULT ((60)) NOT NULL,
        [max_attempt_count] tinyint CONSTRAINT [DF_whatsapp_cloud_api_ayarlari_attempt] DEFAULT ((5)) NOT NULL,
        [phone_reverify_after_days] int CONSTRAINT [DF_whatsapp_cloud_api_ayarlari_reverify] DEFAULT ((180)) NOT NULL,
        [reservation_phone_verification_required] bit CONSTRAINT [DF_whatsapp_cloud_api_ayarlari_reservation] DEFAULT ((0)) NOT NULL,
        [is_active] bit CONSTRAINT [DF_whatsapp_cloud_api_ayarlari_active] DEFAULT ((0)) NOT NULL,
        [test_recipient_phone_e164] nvarchar(32) NULL,
        [last_test_message_at] datetime2(7) NULL,
        [created_by_user_id] bigint NULL,
        [updated_by_user_id] bigint NULL,
        [olusturulma_tarihi] datetime2(7) CONSTRAINT [DF_whatsapp_cloud_api_ayarlari_created] DEFAULT (sysutcdatetime()) NOT NULL,
        [guncellenme_tarihi] datetime2(7) CONSTRAINT [DF_whatsapp_cloud_api_ayarlari_updated] DEFAULT (sysutcdatetime()) NOT NULL,
        CONSTRAINT [PK__whatsapp__3213E83F481B5B59] PRIMARY KEY CLUSTERED ([id] ASC)
    );
END
GO

-- Missing column repair blocks for existing databases.
IF COL_LENGTH(N'dbo.whatsapp_cloud_api_ayarlari', N'id') IS NULL
BEGIN
    ALTER TABLE [dbo].[whatsapp_cloud_api_ayarlari] ADD [id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.whatsapp_cloud_api_ayarlari', N'app_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[whatsapp_cloud_api_ayarlari] ADD [app_id] nvarchar(100) NULL;
END
GO
IF COL_LENGTH(N'dbo.whatsapp_cloud_api_ayarlari', N'app_secret_encrypted') IS NULL
BEGIN
    ALTER TABLE [dbo].[whatsapp_cloud_api_ayarlari] ADD [app_secret_encrypted] nvarchar(max) NULL;
END
GO
IF COL_LENGTH(N'dbo.whatsapp_cloud_api_ayarlari', N'business_account_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[whatsapp_cloud_api_ayarlari] ADD [business_account_id] nvarchar(100) NULL;
END
GO
IF COL_LENGTH(N'dbo.whatsapp_cloud_api_ayarlari', N'phone_number_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[whatsapp_cloud_api_ayarlari] ADD [phone_number_id] nvarchar(100) NULL;
END
GO
IF COL_LENGTH(N'dbo.whatsapp_cloud_api_ayarlari', N'permanent_access_token_encrypted') IS NULL
BEGIN
    ALTER TABLE [dbo].[whatsapp_cloud_api_ayarlari] ADD [permanent_access_token_encrypted] nvarchar(max) NULL;
END
GO
IF COL_LENGTH(N'dbo.whatsapp_cloud_api_ayarlari', N'webhook_verify_token_encrypted') IS NULL
BEGIN
    ALTER TABLE [dbo].[whatsapp_cloud_api_ayarlari] ADD [webhook_verify_token_encrypted] nvarchar(max) NULL;
END
GO
IF COL_LENGTH(N'dbo.whatsapp_cloud_api_ayarlari', N'verification_template_name') IS NULL
BEGIN
    ALTER TABLE [dbo].[whatsapp_cloud_api_ayarlari] ADD [verification_template_name] nvarchar(120) NULL;
END
GO
IF COL_LENGTH(N'dbo.whatsapp_cloud_api_ayarlari', N'default_language_code') IS NULL
BEGIN
    ALTER TABLE [dbo].[whatsapp_cloud_api_ayarlari] ADD [default_language_code] nvarchar(20) DEFAULT ('tr') NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.whatsapp_cloud_api_ayarlari', N'otp_code_length') IS NULL
BEGIN
    ALTER TABLE [dbo].[whatsapp_cloud_api_ayarlari] ADD [otp_code_length] tinyint DEFAULT ((6)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.whatsapp_cloud_api_ayarlari', N'otp_ttl_seconds') IS NULL
BEGIN
    ALTER TABLE [dbo].[whatsapp_cloud_api_ayarlari] ADD [otp_ttl_seconds] int DEFAULT ((300)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.whatsapp_cloud_api_ayarlari', N'resend_cooldown_seconds') IS NULL
BEGIN
    ALTER TABLE [dbo].[whatsapp_cloud_api_ayarlari] ADD [resend_cooldown_seconds] int DEFAULT ((60)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.whatsapp_cloud_api_ayarlari', N'max_attempt_count') IS NULL
BEGIN
    ALTER TABLE [dbo].[whatsapp_cloud_api_ayarlari] ADD [max_attempt_count] tinyint DEFAULT ((5)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.whatsapp_cloud_api_ayarlari', N'phone_reverify_after_days') IS NULL
BEGIN
    ALTER TABLE [dbo].[whatsapp_cloud_api_ayarlari] ADD [phone_reverify_after_days] int DEFAULT ((180)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.whatsapp_cloud_api_ayarlari', N'reservation_phone_verification_required') IS NULL
BEGIN
    ALTER TABLE [dbo].[whatsapp_cloud_api_ayarlari] ADD [reservation_phone_verification_required] bit DEFAULT ((0)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.whatsapp_cloud_api_ayarlari', N'is_active') IS NULL
BEGIN
    ALTER TABLE [dbo].[whatsapp_cloud_api_ayarlari] ADD [is_active] bit DEFAULT ((0)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.whatsapp_cloud_api_ayarlari', N'test_recipient_phone_e164') IS NULL
BEGIN
    ALTER TABLE [dbo].[whatsapp_cloud_api_ayarlari] ADD [test_recipient_phone_e164] nvarchar(32) NULL;
END
GO
IF COL_LENGTH(N'dbo.whatsapp_cloud_api_ayarlari', N'last_test_message_at') IS NULL
BEGIN
    ALTER TABLE [dbo].[whatsapp_cloud_api_ayarlari] ADD [last_test_message_at] datetime2(7) NULL;
END
GO
IF COL_LENGTH(N'dbo.whatsapp_cloud_api_ayarlari', N'created_by_user_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[whatsapp_cloud_api_ayarlari] ADD [created_by_user_id] bigint NULL;
END
GO
IF COL_LENGTH(N'dbo.whatsapp_cloud_api_ayarlari', N'updated_by_user_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[whatsapp_cloud_api_ayarlari] ADD [updated_by_user_id] bigint NULL;
END
GO
IF COL_LENGTH(N'dbo.whatsapp_cloud_api_ayarlari', N'olusturulma_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[whatsapp_cloud_api_ayarlari] ADD [olusturulma_tarihi] datetime2(7) DEFAULT (sysutcdatetime()) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.whatsapp_cloud_api_ayarlari', N'guncellenme_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[whatsapp_cloud_api_ayarlari] ADD [guncellenme_tarihi] datetime2(7) DEFAULT (sysutcdatetime()) NOT NULL;
END
GO
