-- Tablo: dbo.PLATFORM_EPOSTA_MESAJLARI
IF OBJECT_ID(N'dbo.PLATFORM_EPOSTA_MESAJLARI', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[PLATFORM_EPOSTA_MESAJLARI] (
        [ID] bigint IDENTITY(1,1) NOT NULL,
        [HESAP_ID] bigint NOT NULL,
        [YON] nvarchar(20) NOT NULL CONSTRAINT [DF_platform_email_mesajlari_yon] DEFAULT (N'Gelen'),
        [KLASOR] nvarchar(120) NOT NULL CONSTRAINT [DF_platform_email_mesajlari_klasor] DEFAULT (N'INBOX'),
        [UID_DEGERI] nvarchar(120) NULL,
        [INTERNET_MESAJ_ID] nvarchar(500) NULL,
        [KONU] nvarchar(500) NULL,
        [GONDEREN] nvarchar(500) NULL,
        [ALICILAR] nvarchar(max) NULL,
        [CC] nvarchar(max) NULL,
        [TARIH_UTC] datetime2(7) NULL,
        [OZET] nvarchar(1200) NULL,
        [HTML_ICERIK] nvarchar(max) NULL,
        [METIN_ICERIK] nvarchar(max) NULL,
        [OKUNMUS_MU] bit NOT NULL CONSTRAINT [DF_platform_email_mesajlari_okunmus] DEFAULT ((0)),
        [SPAM_MI] bit NOT NULL CONSTRAINT [DF_platform_email_mesajlari_spam] DEFAULT ((0)),
        [ILGILI_BILDIRIM_LOG_ID] bigint NULL,
        [HAM_BASLIKLAR] nvarchar(max) NULL,
        [SENKRON_TARIHI] datetime2(7) NOT NULL CONSTRAINT [DF_platform_email_mesajlari_senkron] DEFAULT (sysutcdatetime()),
        [OLUSTURULMA_TARIHI] datetime2(7) NOT NULL CONSTRAINT [DF_platform_email_mesajlari_olusturulma] DEFAULT (sysutcdatetime()),
        [GUNCELLENME_TARIHI] datetime2(7) NOT NULL CONSTRAINT [DF_platform_email_mesajlari_guncellenme] DEFAULT (sysutcdatetime()),
        CONSTRAINT [PK_PLATFORM_EPOSTA_MESAJLARI] PRIMARY KEY CLUSTERED ([ID] ASC)
    );
END
