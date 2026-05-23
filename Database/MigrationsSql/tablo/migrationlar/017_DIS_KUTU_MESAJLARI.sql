-- Tablo: dbo.DIS_KUTU_MESAJLARI
IF OBJECT_ID(N'dbo.DIS_KUTU_MESAJLARI', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[DIS_KUTU_MESAJLARI] (
        [ID] bigint IDENTITY(1,1) NOT NULL,
        [OLUSTURMA_UTC] datetime2(7) NOT NULL CONSTRAINT [DF_outbox_messages_created] DEFAULT (sysutcdatetime()),
        [OLAY_TURU] nvarchar(128) NOT NULL,
        [YUK] nvarchar(max) NOT NULL,
        [ISLENDI_MI] bit NOT NULL CONSTRAINT [DF_outbox_messages_done] DEFAULT ((0)),
        [ISLENDI_UTC] datetime2(7) NULL,
        [DENEME_SAYISI] int NOT NULL CONSTRAINT [DF_outbox_messages_attempts] DEFAULT ((0)),
        [SON_HATA] nvarchar(2000) NULL,
        CONSTRAINT [PK_DIS_KUTU_MESAJLARI] PRIMARY KEY CLUSTERED ([ID] ASC)
    );
END
