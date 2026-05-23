-- Tablo: dbo.REZERVASYONLAR_ARSIV
IF OBJECT_ID(N'dbo.REZERVASYONLAR_ARSIV', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[REZERVASYONLAR_ARSIV]
    (
        [ID] bigint NOT NULL,
        [OLUSTURULMA_TARIHI] datetime2(7) NULL,
        [DURUM] nvarchar(64) NULL,
        [ARSIV_TARIHI_UTC] datetime2(7) CONSTRAINT [DF_rezervasyonlar_archive_arsiv] DEFAULT (sysutcdatetime()) NOT NULL,
        CONSTRAINT [PK_REZERVASYONLAR_ARSIV] PRIMARY KEY CLUSTERED ([ID] ASC)
    );
END
