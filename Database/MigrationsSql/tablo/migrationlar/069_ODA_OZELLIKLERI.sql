-- Tablo: dbo.ODA_OZELLIKLERI
IF OBJECT_ID(N'dbo.ODA_OZELLIKLERI', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ODA_OZELLIKLERI] (
        [ID] smallint IDENTITY(1,1) NOT NULL,
        [KATEGORI] nvarchar(50) NOT NULL,
        [OZELLIK_ADI] nvarchar(100) NOT NULL,
        [OZELLIK_IKON] nvarchar(50) NULL,
        [SIRALAMA] smallint NULL CONSTRAINT [DF__oda_ozell__siral__5C6CB6D7] DEFAULT ((0)),
        [AKTIF_MI] bit NULL CONSTRAINT [DF__oda_ozell__aktif__5D60DB10] DEFAULT ((1)),
        [KATEGORI_ID] smallint NULL,
        CONSTRAINT [PK_ODA_OZELLIKLERI] PRIMARY KEY CLUSTERED ([ID] ASC)
    );
END
