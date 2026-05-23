-- Tablo: dbo.SADAKAT_ODULLERI
IF OBJECT_ID(N'dbo.SADAKAT_ODULLERI', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[SADAKAT_ODULLERI]
    (
        [ID] bigint IDENTITY(1,1) NOT NULL,
        [KOD] nvarchar(60) NOT NULL,
        [AD] nvarchar(120) NOT NULL,
        [ACIKLAMA] nvarchar(255) NULL,
        [GEREKLI_PUAN] int NOT NULL,
        [IKON] nvarchar(120) NULL,
        [TON] nvarchar(30) NULL,
        [AKTIF_MI] bit CONSTRAINT [DF__sadakat_o__aktif__0539C240] DEFAULT ((1)) NOT NULL,
        [OLUSTURULMA_TARIHI] datetime2(0) CONSTRAINT [DF__sadakat_o__olust__062DE679] DEFAULT (sysutcdatetime()) NOT NULL,
        CONSTRAINT [PK_SADAKAT_ODULLERI] PRIMARY KEY CLUSTERED ([ID] ASC)
    );
END
