-- Tablo: dbo.API_LOGLARI
IF OBJECT_ID(N'dbo.API_LOGLARI', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[API_LOGLARI] (
        [ID] bigint IDENTITY(1,1) NOT NULL,
        [REQUEST_ID] nvarchar(36) NOT NULL,
        [API_VERSIYONU] nvarchar(10) NULL,
        [ENDPOINT] nvarchar(500) NOT NULL,
        [HTTP_METHOD] nvarchar(10) NOT NULL,
        [REQUEST_HEADERS] nvarchar(max) NULL,
        [REQUEST_BODY] nvarchar(max) NULL,
        [REQUEST_IP] nvarchar(45) NULL,
        [KULLANICI_ARACISI] nvarchar(max) NULL,
        [RESPONSE_STATUS] smallint NULL,
        [RESPONSE_HEADERS] nvarchar(max) NULL,
        [RESPONSE_BODY] nvarchar(max) NULL,
        [RESPONSE_SIZE] int NULL,
        [KULLANICI_ID] bigint NULL,
        [API_KEY_ID] int NULL,
        [PARTNER_ID] bigint NULL,
        [ISLEM_SURESI_MS] int NULL,
        [BELLEK_KULLANIMI_KB] int NULL,
        [BASARILI_MI] bit NULL CONSTRAINT [DF__api_logla__basar__72C60C4A] DEFAULT ((1)),
        [HATA_MESAJI] nvarchar(max) NULL,
        [HATA_KODU] nvarchar(20) NULL,
        [BASLANGIC_TARIHI] datetime2(0) NULL CONSTRAINT [DF__api_logla__basla__73BA3083] DEFAULT (sysutcdatetime()),
        [BITIS_TARIHI] datetime2(0) NULL,
        CONSTRAINT [PK_API_LOGLARI] PRIMARY KEY CLUSTERED ([ID] ASC)
    );
END
