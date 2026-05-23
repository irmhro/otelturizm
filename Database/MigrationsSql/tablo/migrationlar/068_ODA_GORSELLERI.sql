-- Tablo: dbo.ODA_GORSELLERI
IF OBJECT_ID(N'dbo.ODA_GORSELLERI', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ODA_GORSELLERI] (
        [ID] bigint IDENTITY(1,1) NOT NULL,
        [ODA_TIP_ID] bigint NOT NULL,
        [GORSEL_URL] nvarchar(500) NOT NULL,
        [THUMBNAIL_URL] nvarchar(500) NULL,
        [BASLIK] nvarchar(200) NULL,
        [ACIKLAMA] nvarchar(max) NULL,
        [KAPAK_FOTOGRAFI_MI] bit NULL CONSTRAINT [DF__oda_gorse__kapak__57A801BA] DEFAULT ((0)),
        [SIRALAMA] smallint NULL CONSTRAINT [DF__oda_gorse__siral__589C25F3] DEFAULT ((0)),
        [BOYUT_KB] int NULL,
        [ONAY_DURUMU] nvarchar(255) NULL,
        [ONAYLAYAN_ADMIN_ID] bigint NULL,
        [ONAY_TARIHI] datetime2(0) NULL,
        [YUKLEYEN_KULLANICI_ID] bigint NULL,
        [OLUSTURULMA_TARIHI] datetime2(0) NULL CONSTRAINT [DF__oda_gorse__olust__59904A2C] DEFAULT (sysutcdatetime()),
        CONSTRAINT [PK_ODA_GORSELLERI] PRIMARY KEY CLUSTERED ([ID] ASC)
    );
END
