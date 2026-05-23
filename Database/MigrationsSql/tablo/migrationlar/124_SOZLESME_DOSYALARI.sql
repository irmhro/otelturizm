-- Tablo: dbo.SOZLESME_DOSYALARI
IF OBJECT_ID(N'dbo.SOZLESME_DOSYALARI', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[SOZLESME_DOSYALARI]
    (
        [ID] bigint IDENTITY(1,1) NOT NULL,
        [SOZLESME_ID] bigint NOT NULL,
        [DOSYA_TIPI] nvarchar(40) NOT NULL,
        [DOSYA_ADI] nvarchar(250) NULL,
        [DOSYA_YOLU] nvarchar(500) NOT NULL,
        [MIME_TIPI] nvarchar(120) NULL,
        [OLUSTURAN_KULLANICI_ID] bigint NULL,
        [OLUSTURULMA_TARIHI] datetime2(7) CONSTRAINT [DF_sozlesme_dosyalari_olusturulma] DEFAULT (sysutcdatetime()) NOT NULL,
        CONSTRAINT [PK_SOZLESME_DOSYALARI] PRIMARY KEY CLUSTERED ([ID] ASC)
    );
END
