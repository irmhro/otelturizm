-- Tablo: dbo.KULLANICI_AKTIVITE_LOGLARI
IF OBJECT_ID(N'dbo.KULLANICI_AKTIVITE_LOGLARI', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[KULLANICI_AKTIVITE_LOGLARI] (
        [ID] bigint IDENTITY(1,1) NOT NULL,
        [KULLANICI_ID] bigint NOT NULL,
        [AKTIVITE_TURU] nvarchar(255) NOT NULL,
        [AKTIVITE_DETAYI] nvarchar(max) NULL,
        [IP_ADRESI] nvarchar(45) NOT NULL,
        [KULLANICI_ARACISI] nvarchar(max) NULL,
        [CIHAZ_TURU] nvarchar(255) NULL,
        [ISLETIM_SISTEMI] nvarchar(50) NULL,
        [TARAYICI] nvarchar(50) NULL,
        [ULKE] nvarchar(50) NULL,
        [SEHIR] nvarchar(50) NULL,
        [SESSION_ID] nvarchar(100) NULL,
        [BASARILI_MI] bit NULL CONSTRAINT [DF__kullanici__basar__6AEFE058] DEFAULT ((1)),
        [HATA_NEDENI] nvarchar(255) NULL,
        [OLUSMA_TARIHI] datetime2(0) NULL CONSTRAINT [DF__kullanici__olusm__6BE40491] DEFAULT (sysutcdatetime()),
        CONSTRAINT [PK_KULLANICI_AKTIVITE_LOGLARI] PRIMARY KEY CLUSTERED ([ID] ASC)
    );
END
