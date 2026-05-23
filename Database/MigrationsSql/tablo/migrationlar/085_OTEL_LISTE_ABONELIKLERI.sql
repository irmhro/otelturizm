-- Tablo: dbo.OTEL_LISTE_ABONELIKLERI
IF OBJECT_ID(N'dbo.OTEL_LISTE_ABONELIKLERI', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[OTEL_LISTE_ABONELIKLERI] (
        [ID] bigint IDENTITY(1,1) NOT NULL,
        [OTEL_ID] bigint NOT NULL,
        [KAPSAM_TIPI] nvarchar(16) NOT NULL,
        [KAPSAM_DEGERI] nvarchar(160) NOT NULL,
        [KAPSAM_DEGERI_NORMALIZE] nvarchar(160) NOT NULL,
        [HEDEF_SIRA] int NOT NULL,
        [BASLANGIC_UTC] datetime2(7) NOT NULL,
        [BITIS_UTC] datetime2(7) NOT NULL,
        [DURUM] nvarchar(20) NOT NULL CONSTRAINT [DF__otel_list__durum__7DEDA633] DEFAULT (N'Beklemede'),
        [TALEP_EDEN_KULLANICI_ID] bigint NULL,
        [ONAYLAYAN_ADMIN_KULLANICI_ID] bigint NULL,
        [ADMIN_NOTU] nvarchar(500) NULL,
        [PARTNER_NOTU] nvarchar(500) NULL,
        [OLUSTURULMA_TARIHI] datetime2(7) NOT NULL CONSTRAINT [DF__otel_list__olust__7EE1CA6C] DEFAULT (sysutcdatetime()),
        [ONAY_TARIHI] datetime2(7) NULL,
        CONSTRAINT [PK_OTEL_LISTE_ABONELIKLERI] PRIMARY KEY CLUSTERED ([ID] ASC)
    );
END
