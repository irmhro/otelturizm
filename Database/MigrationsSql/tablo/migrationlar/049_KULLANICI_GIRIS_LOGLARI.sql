-- Tablo: dbo.KULLANICI_GIRIS_LOGLARI
IF OBJECT_ID(N'dbo.KULLANICI_GIRIS_LOGLARI', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[KULLANICI_GIRIS_LOGLARI] (
        [ID] bigint IDENTITY(1,1) NOT NULL,
        [KULLANICI_ID] bigint NOT NULL,
        [HESAP_TIPI] nvarchar(20) NOT NULL CONSTRAINT [DF_kullanici_giris_loglari_hesap] DEFAULT ('user'),
        [IP_ADRESI] nvarchar(80) NULL,
        [KULLANICI_ARACISI] nvarchar(500) NULL,
        [CIHAZ_ETIKETI] nvarchar(150) NULL,
        [GIRIS_TARIHI] datetime2(7) NOT NULL CONSTRAINT [DF_kullanici_giris_loglari_giris] DEFAULT (sysutcdatetime()),
        [OLUSTURULMA_TARIHI] datetime2(7) NOT NULL CONSTRAINT [DF_kullanici_giris_loglari_created] DEFAULT (sysutcdatetime()),
        CONSTRAINT [PK_KULLANICI_GIRIS_LOGLARI] PRIMARY KEY CLUSTERED ([ID] ASC)
    );
END
