-- Tablo: dbo.ODA_OZELLIK_KATEGORILERI
IF OBJECT_ID(N'dbo.ODA_OZELLIK_KATEGORILERI', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ODA_OZELLIK_KATEGORILERI] (
        [ID] smallint IDENTITY(1,1) NOT NULL,
        [KATEGORI_ADI] nvarchar(100) NOT NULL,
        [KATEGORI_IKON] nvarchar(80) NULL,
        [SIRALAMA] smallint NOT NULL CONSTRAINT [DF_oda_ozellik_kategorileri_siralama] DEFAULT ((100)),
        [AKTIF_MI] bit NOT NULL CONSTRAINT [DF_oda_ozellik_kategorileri_aktif] DEFAULT ((1)),
        [OLUSTURULMA_TARIHI] datetime2(0) NOT NULL CONSTRAINT [DF_oda_ozellik_kategorileri_olusturma] DEFAULT (sysutcdatetime()),
        [GUNCELLENME_TARIHI] datetime2(0) NULL,
        CONSTRAINT [PK_ODA_OZELLIK_KATEGORILERI] PRIMARY KEY CLUSTERED ([ID] ASC)
    );
END
