-- Tablo: dbo.DEPARTMANLAR
IF OBJECT_ID(N'dbo.DEPARTMANLAR', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[DEPARTMANLAR] (
        [ID] smallint IDENTITY(1,1) NOT NULL,
        [DEPARTMAN_KODU] nvarchar(30) NOT NULL,
        [DEPARTMAN_ADI] nvarchar(50) NOT NULL,
        [UST_DEPARTMAN_ID] smallint NULL,
        [YONETICI_ROL_ID] smallint NULL,
        [BINA_KAT] nvarchar(20) NULL,
        [DAHILI_TELEFON] nvarchar(10) NULL,
        [ACIKLAMA] nvarchar(255) NULL,
        [AKTIF_MI] bit NULL CONSTRAINT [DF__departman__aktif__02FC7413] DEFAULT ((1)),
        [OLUSTURULMA_TARIHI] datetime2(0) NULL CONSTRAINT [DF__departman__olust__03F0984C] DEFAULT (sysutcdatetime()),
        CONSTRAINT [PK_DEPARTMANLAR] PRIMARY KEY CLUSTERED ([ID] ASC)
    );
END
