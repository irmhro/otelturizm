-- Tablo: dbo.WHATSAPP_CLOUD_API_AYARLARI
IF OBJECT_ID(N'dbo.WHATSAPP_CLOUD_API_AYARLARI', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[WHATSAPP_CLOUD_API_AYARLARI]
    (
        [ID] bigint IDENTITY(1,1) NOT NULL,
        [APP_ID] nvarchar(100) NULL,
        [APP_SECRET_ENCRYPTED] nvarchar(max) NULL,
        [BUSINESS_ACCOUNT_ID] nvarchar(100) NULL,
        [TELEFON_NUMARASI_ID] nvarchar(100) NULL,
        [PERMANENT_ACCESS_TOKEN_ENCRYPTED] nvarchar(max) NULL,
        [WEBHOOK_VERIFY_TOKEN_ENCRYPTED] nvarchar(max) NULL,
        [DOGRULAMA_SABLON_ADI] nvarchar(120) NULL,
        [VARSAYILAN_DIL_KODU] nvarchar(20) CONSTRAINT [DF_whatsapp_cloud_api_ayarlari_lang] DEFAULT ('tr') NOT NULL,
        [OTP_KOD_LENGTH] tinyint CONSTRAINT [DF_whatsapp_cloud_api_ayarlari_code] DEFAULT ((6)) NOT NULL,
        [OTP_TTL_SECONDS] int CONSTRAINT [DF_whatsapp_cloud_api_ayarlari_ttl] DEFAULT ((300)) NOT NULL,
        [RESEND_COOLDOWN_SECONDS] int CONSTRAINT [DF_whatsapp_cloud_api_ayarlari_cooldown] DEFAULT ((60)) NOT NULL,
        [MAX_ATTEMPT_COUNT] tinyint CONSTRAINT [DF_whatsapp_cloud_api_ayarlari_attempt] DEFAULT ((5)) NOT NULL,
        [TELEFON_REVERIFY_AFTER_DAYS] int CONSTRAINT [DF_whatsapp_cloud_api_ayarlari_reverify] DEFAULT ((180)) NOT NULL,
        [RESERVATION_TELEFON_VERIFICATION_REQUIRED] bit CONSTRAINT [DF_whatsapp_cloud_api_ayarlari_reservation] DEFAULT ((0)) NOT NULL,
        [AKTIF_MI] bit CONSTRAINT [DF_whatsapp_cloud_api_ayarlari_active] DEFAULT ((0)) NOT NULL,
        [TEST_RECIPIENT_TELEFON_E164] nvarchar(32) NULL,
        [SON_TEST_MESAJ_TARIHI] datetime2(7) NULL,
        [OLUSTURAN_KULLANICI_ID] bigint NULL,
        [GUNCELLEYEN_KULLANICI_ID] bigint NULL,
        [OLUSTURULMA_TARIHI] datetime2(7) CONSTRAINT [DF_whatsapp_cloud_api_ayarlari_created] DEFAULT (sysutcdatetime()) NOT NULL,
        [GUNCELLENME_TARIHI] datetime2(7) CONSTRAINT [DF_whatsapp_cloud_api_ayarlari_updated] DEFAULT (sysutcdatetime()) NOT NULL,
        CONSTRAINT [PK_WHATSAPP_CLOUD_API_AYARLARI] PRIMARY KEY CLUSTERED ([ID] ASC)
    );
END
