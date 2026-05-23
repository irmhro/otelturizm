-- Tablo: dbo.FIRMA_BASVURU_HAREKETLERI
IF OBJECT_ID(N'dbo.FIRMA_BASVURU_HAREKETLERI', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[FIRMA_BASVURU_HAREKETLERI] (
        [ID] bigint IDENTITY(1,1) NOT NULL,
        [FIRMA_ID] bigint NOT NULL,
        [ONCEKI_DURUM] nvarchar(255) NULL,
        [YENI_DURUM] nvarchar(255) NOT NULL,
        [HAREKET_TIPI] nvarchar(255) NOT NULL,
        [ACIKLAMA] nvarchar(max) NULL,
        [ISLEM_YAPAN_KULLANICI_ID] bigint NULL,
        [ISLEM_KAYNAGI] nvarchar(50) NOT NULL,
        [IP_ADRESI] nvarchar(45) NULL,
        [OLUSTURULMA_TARIHI] datetime2(0) NULL CONSTRAINT [DF__firma_bas__olust__32AB8735] DEFAULT (sysutcdatetime()),
        CONSTRAINT [PK_FIRMA_BASVURU_HAREKETLERI] PRIMARY KEY CLUSTERED ([ID] ASC)
    );
END
