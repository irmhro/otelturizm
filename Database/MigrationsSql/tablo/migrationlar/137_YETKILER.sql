-- Tablo: dbo.YETKILER
IF OBJECT_ID(N'dbo.YETKILER', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[YETKILER]
    (
        [ID] int IDENTITY(1,1) NOT NULL,
        [YETKI_KODU] nvarchar(100) NOT NULL,
        [MODUL] nvarchar(50) NOT NULL,
        [EYLEM] nvarchar(50) NOT NULL,
        [ACIKLAMA] nvarchar(255) NULL,
        [VARSAYILAN_IZIN] bit CONSTRAINT [DF__yetkiler__varsay__6497E884] DEFAULT ((0)) NULL,
        [OLUSTURULMA_TARIHI] datetime2(0) CONSTRAINT [DF__yetkiler__olustu__658C0CBD] DEFAULT (sysutcdatetime()) NULL,
        CONSTRAINT [PK_YETKILER] PRIMARY KEY CLUSTERED ([ID] ASC)
    );
END
