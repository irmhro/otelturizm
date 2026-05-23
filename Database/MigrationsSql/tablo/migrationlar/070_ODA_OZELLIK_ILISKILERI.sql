-- Tablo: dbo.ODA_OZELLIK_ILISKILERI
IF OBJECT_ID(N'dbo.ODA_OZELLIK_ILISKILERI', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ODA_OZELLIK_ILISKILERI] (
        [OTEL_ID] bigint NOT NULL,
        [ODA_ID] bigint NOT NULL,
        [KATEGORI_ID] smallint NOT NULL,
        [OZELLIK_ID] smallint NOT NULL,
        [MIKTAR] tinyint NULL CONSTRAINT [DF_oda_ozellik_iliskileri_miktar] DEFAULT ((1)),
        [AKTIF_MI] bit NOT NULL CONSTRAINT [DF_oda_ozellik_iliskileri_aktif] DEFAULT ((1)),
        [OLUSTURULMA_TARIHI] datetime2(0) NOT NULL CONSTRAINT [DF_oda_ozellik_iliskileri_olusturma] DEFAULT (sysutcdatetime()),
        [GUNCELLENME_TARIHI] datetime2(0) NULL,
        CONSTRAINT [PK_ODA_OZELLIK_ILISKILERI] PRIMARY KEY CLUSTERED ([OTEL_ID], [ODA_ID], [OZELLIK_ID] ASC)
    );
END
