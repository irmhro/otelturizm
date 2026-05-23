-- Tablo: dbo.SOZLESME_KABULLERI
IF OBJECT_ID(N'dbo.SOZLESME_KABULLERI', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[SOZLESME_KABULLERI]
    (
        [ID] bigint IDENTITY(1,1) NOT NULL,
        [SOZLESME_ID] bigint NOT NULL,
        [KABUL_EDEN_TIP] nvarchar(30) NOT NULL,
        [KULLANICI_ID] bigint NOT NULL,
        [PARTNER_ID] bigint NULL,
        [FIRMA_ID] bigint NULL,
        [ALICI_EPOSTA] nvarchar(255) NOT NULL,
        [SOZLESME_BASLIK_SNAPSHOT] nvarchar(200) NOT NULL,
        [SOZLESME_VERSIYON_SNAPSHOT] int NOT NULL,
        [KABUL_KAYNAGI] nvarchar(80) NOT NULL,
        [KABUL_IP] nvarchar(80) NULL,
        [KABUL_KULLANICI_ARACISI] nvarchar(500) NULL,
        [EPOSTA_DOGRULANDI_MI] bit CONSTRAINT [DF_sozlesme_kabulleri_eposta] DEFAULT ((0)) NOT NULL,
        [EPOSTA_DOGRULAMA_TARIHI] datetime2(7) NULL,
        [KABUL_TARIHI] datetime2(7) CONSTRAINT [DF_sozlesme_kabulleri_tarih] DEFAULT (sysutcdatetime()) NOT NULL,
        [SONA_ERME_TARIHI] datetime2(7) NULL,
        [DURUM] nvarchar(40) CONSTRAINT [DF_sozlesme_kabulleri_durum] DEFAULT ('KabulEdildi') NOT NULL,
        CONSTRAINT [PK_SOZLESME_KABULLERI] PRIMARY KEY CLUSTERED ([ID] ASC)
    );
END
