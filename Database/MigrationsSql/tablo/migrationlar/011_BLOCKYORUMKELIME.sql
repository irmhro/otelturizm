-- Tablo: dbo.BLOCKYORUMKELIME
IF OBJECT_ID(N'dbo.BLOCKYORUMKELIME', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[BLOCKYORUMKELIME] (
        [ID] bigint IDENTITY(1,1) NOT NULL,
        [KELIME] nvarchar(120) NOT NULL,
        [AKTIF_MI] bit NOT NULL CONSTRAINT [DF_blockyorumkelime_aktif] DEFAULT ((1)),
        [ACIKLAMA] nvarchar(250) NULL,
        [EKLEYEN_ADMIN_ID] bigint NULL,
        [OLUSTURULMA_TARIHI] datetime2(0) NOT NULL CONSTRAINT [DF_blockyorumkelime_olustur] DEFAULT (sysutcdatetime()),
        [GUNCELLENME_TARIHI] datetime2(0) NULL,
        CONSTRAINT [PK_BLOCKYORUMKELIME] PRIMARY KEY CLUSTERED ([ID] ASC)
    );
END
