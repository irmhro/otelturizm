-- Tablo: dbo.KULLANICI_ROZETLERI
IF OBJECT_ID(N'dbo.KULLANICI_ROZETLERI', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[KULLANICI_ROZETLERI] (
        [ID] bigint IDENTITY(1,1) NOT NULL,
        [KULLANICI_ID] bigint NOT NULL,
        [ROZET_ID] bigint NOT NULL,
        [DURUM] nvarchar(30) NOT NULL,
        [ILERLEME_DEGERI] int NOT NULL CONSTRAINT [DF__kullanici__ilerl__1F63A897] DEFAULT ((0)),
        [KAZANILMA_TARIHI] datetime2(0) NULL,
        [OLUSTURULMA_TARIHI] datetime2(0) NOT NULL CONSTRAINT [DF__kullanici__olust__2057CCD0] DEFAULT (sysutcdatetime()),
        [GUNCELLENME_TARIHI] datetime2(0) NOT NULL CONSTRAINT [DF__kullanici__gunce__214BF109] DEFAULT (sysutcdatetime()),
        CONSTRAINT [PK_KULLANICI_ROZETLERI] PRIMARY KEY CLUSTERED ([ID] ASC)
    );
END
