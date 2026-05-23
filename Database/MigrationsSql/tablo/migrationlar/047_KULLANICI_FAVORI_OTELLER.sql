-- Tablo: dbo.KULLANICI_FAVORI_OTELLER
IF OBJECT_ID(N'dbo.KULLANICI_FAVORI_OTELLER', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[KULLANICI_FAVORI_OTELLER] (
        [ID] bigint IDENTITY(1,1) NOT NULL,
        [KULLANICI_ID] bigint NOT NULL,
        [OTEL_ID] bigint NOT NULL,
        [KAYNAK_SAYFA] nvarchar(100) NULL,
        [KAYNAK_URL] nvarchar(500) NULL,
        [CIHAZ_TIPI] nvarchar(50) NULL,
        [IP_ADRESI] nvarchar(45) NULL,
        [AKTIF_MI] bit NOT NULL CONSTRAINT [DF__user_favo__aktif__3AA1AEB8] DEFAULT ((1)),
        [SON_ISLEM_TARIHI] datetime2(0) NULL CONSTRAINT [DF__user_favo__son_i__3B95D2F1] DEFAULT (sysutcdatetime()),
        [KALDIRILMA_TARIHI] datetime2(0) NULL,
        [OLUSTURULMA_TARIHI] datetime2(0) NOT NULL CONSTRAINT [DF__user_favo__olust__3C89F72A] DEFAULT (sysutcdatetime()),
        CONSTRAINT [PK_KULLANICI_FAVORI_OTELLER] PRIMARY KEY CLUSTERED ([ID] ASC)
    );
END
