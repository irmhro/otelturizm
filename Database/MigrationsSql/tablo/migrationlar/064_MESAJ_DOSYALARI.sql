-- Tablo: dbo.MESAJ_DOSYALARI
IF OBJECT_ID(N'dbo.MESAJ_DOSYALARI', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[MESAJ_DOSYALARI] (
        [ID] bigint IDENTITY(1,1) NOT NULL,
        [MESAJ_ID] bigint NOT NULL,
        [GUVENLI_DOSYA_ID] bigint NOT NULL,
        [GOSTERIM_ADI] nvarchar(255) NULL,
        [SIRALAMA] int NOT NULL CONSTRAINT [DF__mesaj_dos__siral__373B3228] DEFAULT ((1)),
        [AKTIF_MI] bit NOT NULL CONSTRAINT [DF__mesaj_dos__aktif__382F5661] DEFAULT ((1)),
        [OLUSTURULMA_TARIHI] datetime2(0) NOT NULL CONSTRAINT [DF__mesaj_dos__olust__39237A9A] DEFAULT (sysutcdatetime()),
        CONSTRAINT [PK_MESAJ_DOSYALARI] PRIMARY KEY CLUSTERED ([ID] ASC)
    );
END
