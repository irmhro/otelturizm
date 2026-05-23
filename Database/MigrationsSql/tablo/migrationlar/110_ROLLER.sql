-- Tablo: dbo.ROLLER
IF OBJECT_ID(N'dbo.ROLLER', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ROLLER]
    (
        [ID] smallint IDENTITY(1,1) NOT NULL,
        [ROL_KODU] nvarchar(30) NOT NULL,
        [ROL_ADI] nvarchar(50) NOT NULL,
        [DEPARTMAN] nvarchar(50) NOT NULL,
        [SEVIYE] tinyint NOT NULL,
        [UST_ROL_ID] smallint NULL,
        [VARSAYILAN_MI] bit CONSTRAINT [DF__roller__varsayil__7BB05806] DEFAULT ((0)) NULL,
        [ACIKLAMA] nvarchar(255) NULL,
        [OLUSTURULMA_TARIHI] datetime2(0) CONSTRAINT [DF__roller__olusturu__7CA47C3F] DEFAULT (sysutcdatetime()) NULL,
        CONSTRAINT [PK_ROLLER] PRIMARY KEY CLUSTERED ([ID] ASC)
    );
END
