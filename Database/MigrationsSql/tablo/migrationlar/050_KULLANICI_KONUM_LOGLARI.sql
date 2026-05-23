-- Tablo: dbo.KULLANICI_KONUM_LOGLARI
IF OBJECT_ID(N'dbo.KULLANICI_KONUM_LOGLARI', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[KULLANICI_KONUM_LOGLARI] (
        [ID] bigint IDENTITY(1,1) NOT NULL,
        [KULLANICI_ID] bigint NULL,
        [ENLEM] decimal(10,7) NOT NULL,
        [BOYLAM] decimal(10,7) NOT NULL,
        [KAYNAK] nvarchar(50) NULL,
        [KULLANICI_AJAN] nvarchar(500) NULL,
        [IP_ADRESI] nvarchar(64) NULL,
        [KAYIT_TARIHI] datetime2(0) NOT NULL CONSTRAINT [DF_kullanici_konum_loglari_kayit_tarihi] DEFAULT (sysutcdatetime()),
        [SESSION_KEY] nvarchar(120) NULL,
        [YARICAP_KM] int NULL,
        [GORUNEN_OTEL_SAYISI] int NULL,
        [ARAMA_METNI] nvarchar(250) NULL,
        [ARAMA_BOLGESI] nvarchar(200) NULL,
        [CIHAZ_TIPI] nvarchar(50) NULL,
        [CIHAZ_MODELI] nvarchar(120) NULL,
        [PLATFORM] nvarchar(80) NULL,
        [TARAYICI] nvarchar(80) NULL,
        [TELEFON_IPUCU] nvarchar(80) NULL,
        [SAYFA_URL] nvarchar(500) NULL,
        [LISTELENEN_OTEL_IDLERI] nvarchar(max) NULL,
        CONSTRAINT [PK_KULLANICI_KONUM_LOGLARI] PRIMARY KEY CLUSTERED ([ID] ASC)
    );
END
