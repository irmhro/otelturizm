-- Tablo: dbo.KULLANICI_BUTCE_PLANLARI
IF OBJECT_ID(N'dbo.KULLANICI_BUTCE_PLANLARI', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[KULLANICI_BUTCE_PLANLARI] (
        [ID] bigint IDENTITY(1,1) NOT NULL,
        [KULLANICI_ID] bigint NOT NULL,
        [HEDEF_SEHIR] nvarchar(120) NOT NULL,
        [HEDEF_BUTCE] decimal(12,2) NOT NULL,
        [GECE_SAYISI] int NOT NULL CONSTRAINT [DF__kullanici__gece___7C1A6C5A] DEFAULT ((1)),
        [KISI_SAYISI] int NOT NULL CONSTRAINT [DF__kullanici__kisi___7D0E9093] DEFAULT ((1)),
        [PARA_BIRIMI] nvarchar(10) NOT NULL,
        [NOTLAR] nvarchar(255) NULL,
        [OLUSTURULMA_TARIHI] datetime2(0) NOT NULL CONSTRAINT [DF__kullanici__olust__7E02B4CC] DEFAULT (sysutcdatetime()),
        [GUNCELLENME_TARIHI] datetime2(0) NOT NULL CONSTRAINT [DF__kullanici__gunce__7EF6D905] DEFAULT (sysutcdatetime()),
        CONSTRAINT [PK_KULLANICI_BUTCE_PLANLARI] PRIMARY KEY CLUSTERED ([ID] ASC)
    );
END
