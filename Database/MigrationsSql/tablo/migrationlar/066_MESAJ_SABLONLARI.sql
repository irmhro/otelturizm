-- Tablo: dbo.MESAJ_SABLONLARI
IF OBJECT_ID(N'dbo.MESAJ_SABLONLARI', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[MESAJ_SABLONLARI] (
        [ID] int IDENTITY(1,1) NOT NULL,
        [SABLON_KODU] nvarchar(30) NOT NULL,
        [SABLON_ADI] nvarchar(100) NOT NULL,
        [OTEL_ID] bigint NULL,
        [SISTEM_GENELI_MI] bit NULL CONSTRAINT [DF__mesaj_sab__siste__41B8C09B] DEFAULT ((0)),
        [KATEGORI] nvarchar(255) NOT NULL,
        [KONU_BASLIGI] nvarchar(200) NOT NULL,
        [MESAJ_ICERIGI] nvarchar(max) NOT NULL,
        [KULLANILABILIR_DEGISKENLER] nvarchar(max) NULL,
        [DIL] nvarchar(5) NULL,
        [AKTIF_MI] bit NULL CONSTRAINT [DF__mesaj_sab__aktif__42ACE4D4] DEFAULT ((1)),
        [OLUSTURULMA_TARIHI] datetime2(0) NULL CONSTRAINT [DF__mesaj_sab__olust__43A1090D] DEFAULT (sysutcdatetime()),
        CONSTRAINT [PK_MESAJ_SABLONLARI] PRIMARY KEY CLUSTERED ([ID] ASC)
    );
END
