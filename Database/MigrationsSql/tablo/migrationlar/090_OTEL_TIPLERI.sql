-- Tablo: dbo.OTEL_TIPLERI
IF OBJECT_ID(N'dbo.OTEL_TIPLERI', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[OTEL_TIPLERI] (
        [ID] int IDENTITY(1,1) NOT NULL,
        [KOD] nvarchar(60) NOT NULL,
        [TIP_ADI] nvarchar(100) NOT NULL,
        [ACIKLAMA] nvarchar(300) NULL,
        [IKON_CLASS] nvarchar(80) NULL,
        [SIRALAMA] smallint NOT NULL CONSTRAINT [DF_otel_tipleri_siralama] DEFAULT ((100)),
        [AKTIF_MI] bit NOT NULL CONSTRAINT [DF_otel_tipleri_aktif_mi] DEFAULT ((1)),
        [OLUSTURULMA_TARIHI] datetime2(0) NOT NULL CONSTRAINT [DF_otel_tipleri_olusturulma] DEFAULT (sysutcdatetime()),
        [GUNCELLENME_TARIHI] datetime2(0) NULL,
        CONSTRAINT [PK_OTEL_TIPLERI] PRIMARY KEY CLUSTERED ([ID] ASC)
    );
END
