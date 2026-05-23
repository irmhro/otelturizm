-- Tablo: dbo.PARTNER_DESTEK_TALEPLERI
IF OBJECT_ID(N'dbo.PARTNER_DESTEK_TALEPLERI', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[PARTNER_DESTEK_TALEPLERI] (
        [ID] bigint IDENTITY(1,1) NOT NULL,
        [PARTNER_ID] bigint NOT NULL,
        [KULLANICI_ID] bigint NOT NULL,
        [OTEL_ID] bigint NULL,
        [TALEP_NO] nvarchar(32) NOT NULL,
        [KONU] nvarchar(200) NOT NULL,
        [KATEGORI] nvarchar(100) NOT NULL,
        [ONCELIK] nvarchar(255) NULL,
        [DURUM] nvarchar(255) NULL,
        [ATANAN_ADMIN_ID] bigint NULL,
        [SON_MESAJ_TARIHI] datetime2(0) NULL,
        [OLUSTURULMA_TARIHI] datetime2(0) NULL CONSTRAINT [DF__partner_d__olust__4EDDB18F] DEFAULT (sysutcdatetime()),
        [GUNCELLENME_TARIHI] datetime2(0) NULL,
        CONSTRAINT [PK_PARTNER_DESTEK_TALEPLERI] PRIMARY KEY CLUSTERED ([ID] ASC)
    );
END
