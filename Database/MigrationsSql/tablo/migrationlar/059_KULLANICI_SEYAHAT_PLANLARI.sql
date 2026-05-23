-- Tablo: dbo.KULLANICI_SEYAHAT_PLANLARI
IF OBJECT_ID(N'dbo.KULLANICI_SEYAHAT_PLANLARI', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[KULLANICI_SEYAHAT_PLANLARI] (
        [ID] bigint IDENTITY(1,1) NOT NULL,
        [OLUSTURAN_KULLANICI_ID] bigint NOT NULL,
        [PLAN_KODU] nvarchar(80) NOT NULL,
        [PLAN_ADI] nvarchar(180) NOT NULL,
        [HEDEF_SEHIR] nvarchar(120) NOT NULL,
        [BASLANGIC_TARIHI] date NULL,
        [BITIS_TARIHI] date NULL,
        [BUTCE_TUTARI] decimal(12,2) NULL,
        [PARA_BIRIMI] nvarchar(10) NOT NULL,
        [DAVET_KODU] nvarchar(40) NULL,
        [DURUM] nvarchar(30) NOT NULL,
        [OLUSTURULMA_TARIHI] datetime2(0) NOT NULL CONSTRAINT [DF__kullanici__olust__2F9A1060] DEFAULT (sysutcdatetime()),
        [GUNCELLENME_TARIHI] datetime2(0) NOT NULL CONSTRAINT [DF__kullanici__gunce__308E3499] DEFAULT (sysutcdatetime()),
        CONSTRAINT [PK_KULLANICI_SEYAHAT_PLANLARI] PRIMARY KEY CLUSTERED ([ID] ASC)
    );
END
