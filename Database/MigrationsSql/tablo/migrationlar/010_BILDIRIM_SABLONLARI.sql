-- Tablo: dbo.BILDIRIM_SABLONLARI
IF OBJECT_ID(N'dbo.BILDIRIM_SABLONLARI', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[BILDIRIM_SABLONLARI] (
        [ID] smallint IDENTITY(1,1) NOT NULL,
        [SABLON_KODU] nvarchar(50) NOT NULL,
        [SABLON_ADI] nvarchar(100) NOT NULL,
        [TUR] nvarchar(255) NOT NULL,
        [DIL] nvarchar(5) NOT NULL,
        [KONU] nvarchar(200) NULL,
        [BASLIK] nvarchar(100) NULL,
        [ICERIK] nvarchar(max) NOT NULL,
        [DEGISKENLER] nvarchar(max) NULL,
        [AKTIF_MI] bit NULL CONSTRAINT [DF__bildirim___aktif__7F2BE32F] DEFAULT ((1)),
        [OLUSTURULMA_TARIHI] datetime2(0) NULL CONSTRAINT [DF__bildirim___olust__00200768] DEFAULT (sysutcdatetime()),
        CONSTRAINT [PK_BILDIRIM_SABLONLARI] PRIMARY KEY CLUSTERED ([ID] ASC)
    );
END
