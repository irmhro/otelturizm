-- Tablo: dbo.PLATFORM_EKIP_UYELERI
IF OBJECT_ID(N'dbo.PLATFORM_EKIP_UYELERI', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[PLATFORM_EKIP_UYELERI] (
        [ID] bigint IDENTITY(1,1) NOT NULL,
        [AD_SOYAD] nvarchar(120) NOT NULL,
        [UNVAN] nvarchar(160) NOT NULL,
        [EPOSTA] nvarchar(160) NOT NULL,
        [ACIKLAMA] nvarchar(260) NULL,
        [AVATAR_URL] nvarchar(400) NULL,
        [SIRALAMA] int NOT NULL CONSTRAINT [DF_platform_ekip_uyeleri_siralama] DEFAULT ((0)),
        [AKTIF_MI] bit NOT NULL CONSTRAINT [DF_platform_ekip_uyeleri_aktif] DEFAULT ((1)),
        [OLUSTURULMA_TARIHI] datetime2(0) NOT NULL CONSTRAINT [DF_platform_ekip_uyeleri_olustur] DEFAULT (sysutcdatetime()),
        [GUNCELLENME_TARIHI] datetime2(0) NULL,
        CONSTRAINT [PK_PLATFORM_EKIP_UYELERI] PRIMARY KEY CLUSTERED ([ID] ASC)
    );
END
