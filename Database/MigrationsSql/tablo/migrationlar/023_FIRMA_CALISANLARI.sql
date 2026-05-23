-- Tablo: dbo.FIRMA_CALISANLARI
IF OBJECT_ID(N'dbo.FIRMA_CALISANLARI', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[FIRMA_CALISANLARI] (
        [ID] bigint IDENTITY(1,1) NOT NULL,
        [FIRMA_ID] bigint NULL,
        [AD_SOYAD] nvarchar(160) NOT NULL,
        [EPOSTA] nvarchar(180) NULL,
        [TELEFON] nvarchar(40) NULL,
        [DEPARTMAN] nvarchar(120) NULL,
        [GOREV] nvarchar(120) NULL,
        [AKTIF_MI] bit NOT NULL CONSTRAINT [DF_firma_calisanlari_aktif_mi] DEFAULT ((1)),
        [OLUSTURULMA_TARIHI] datetime2(0) NOT NULL CONSTRAINT [DF_firma_calisanlari_olusturulma_tarihi] DEFAULT (sysutcdatetime()),
        [GUNCELLENME_TARIHI] datetime2(0) NULL,
        CONSTRAINT [PK_FIRMA_CALISANLARI] PRIMARY KEY CLUSTERED ([ID] ASC)
    );
END
