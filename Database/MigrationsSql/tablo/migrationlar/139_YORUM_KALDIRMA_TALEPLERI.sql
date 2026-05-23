-- Tablo: dbo.YORUM_KALDIRMA_TALEPLERI
IF OBJECT_ID(N'dbo.YORUM_KALDIRMA_TALEPLERI', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[YORUM_KALDIRMA_TALEPLERI] (
        [ID] bigint IDENTITY(1,1) NOT NULL,
        [YORUM_ID] bigint NOT NULL,
        [OTEL_ID] bigint NULL,
        [PARTNER_KULLANICI_ID] bigint NOT NULL,
        [SEBEP] nvarchar(800) NULL,
        [DURUM] nvarchar(40) NOT NULL CONSTRAINT [DF_yorum_kaldirma_talepleri_durum] DEFAULT (N'Beklemede'),
        [ADMIN_NOTU] nvarchar(800) NULL,
        [KARAR_VEREN_ADMIN_ID] bigint NULL,
        [KARAR_TARIHI] datetime2(0) NULL,
        [OLUSTURULMA_TARIHI] datetime2(0) NOT NULL CONSTRAINT [DF_yorum_kaldirma_talepleri_olustur] DEFAULT (sysutcdatetime()),
        [GUNCELLENME_TARIHI] datetime2(0) NULL,
        CONSTRAINT [PK_YORUM_KALDIRMA_TALEPLERI] PRIMARY KEY CLUSTERED ([ID] ASC)
    );
END
