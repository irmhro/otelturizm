-- Tablo: dbo.SOZLESME_GONDERIM_LOGLARI
IF OBJECT_ID(N'dbo.SOZLESME_GONDERIM_LOGLARI', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[SOZLESME_GONDERIM_LOGLARI]
    (
        [ID] bigint IDENTITY(1,1) NOT NULL,
        [SOZLESME_ID] bigint NOT NULL,
        [KULLANICI_ID] bigint NOT NULL,
        [PARTNER_ID] bigint NULL,
        [FIRMA_ID] bigint NULL,
        [ALICI_EPOSTA] nvarchar(255) NOT NULL,
        [GONDERIM_NEDENI] nvarchar(80) NOT NULL,
        [BILDIRIM_LOG_ID] bigint NULL,
        [KONU_SNAPSHOT] nvarchar(255) NOT NULL,
        [ICERIK_SNAPSHOT] nvarchar(max) NULL,
        [DURUM] nvarchar(40) CONSTRAINT [DF_sozlesme_gonderim_durum] DEFAULT ('KuyrugaAlindi') NOT NULL,
        [GONDERIM_TARIHI] datetime2(7) CONSTRAINT [DF_sozlesme_gonderim_tarih] DEFAULT (sysutcdatetime()) NOT NULL,
        [IP_ADRESI] nvarchar(80) NULL,
        [KULLANICI_ARACISI] nvarchar(500) NULL,
        [OLUSTURAN_ADMIN_ID] bigint NULL,
        CONSTRAINT [PK_SOZLESME_GONDERIM_LOGLARI] PRIMARY KEY CLUSTERED ([ID] ASC)
    );
END
