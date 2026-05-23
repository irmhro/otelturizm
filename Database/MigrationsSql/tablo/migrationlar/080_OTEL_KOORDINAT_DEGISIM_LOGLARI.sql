-- Tablo: dbo.OTEL_KOORDINAT_DEGISIM_LOGLARI
IF OBJECT_ID(N'dbo.OTEL_KOORDINAT_DEGISIM_LOGLARI', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[OTEL_KOORDINAT_DEGISIM_LOGLARI] (
        [ID] bigint IDENTITY(1,1) NOT NULL,
        [ADMIN_KULLANICI_ID] bigint NOT NULL,
        [ADMIN_AD_SOYAD] nvarchar(160) NULL,
        [OTEL_ID] bigint NOT NULL,
        [OTEL_ADI] nvarchar(250) NULL,
        [ONCEKI_ENLEM] decimal(10,7) NULL,
        [ONCEKI_BOYLAM] decimal(10,7) NULL,
        [YENI_ENLEM] decimal(10,7) NULL,
        [YENI_BOYLAM] decimal(10,7) NULL,
        [IP_ADRESI] nvarchar(64) NULL,
        [NOTLAR] nvarchar(500) NULL,
        [KAYIT_TARIHI] datetime2(7) NOT NULL CONSTRAINT [DF_otel_koordinat_degisim_loglari_kayit_tarihi] DEFAULT (sysutcdatetime()),
        CONSTRAINT [PK_OTEL_KOORDINAT_DEGISIM_LOGLARI] PRIMARY KEY CLUSTERED ([ID] ASC)
    );
END
