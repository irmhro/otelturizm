SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.sozlesme_dosyalari', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[sozlesme_dosyalari]
    (
        [id] bigint IDENTITY(1,1) NOT NULL,
        [sozlesme_id] bigint NOT NULL,
        [dosya_tipi] nvarchar(40) NOT NULL,
        [dosya_adi] nvarchar(250) NULL,
        [dosya_yolu] nvarchar(500) NOT NULL,
        [mime_tipi] nvarchar(120) NULL,
        [olusturan_kullanici_id] bigint NULL,
        [olusturulma_tarihi] datetime2(7) CONSTRAINT [DF_sozlesme_dosyalari_olusturulma] DEFAULT (sysutcdatetime()) NOT NULL,
        CONSTRAINT [PK__sozlesme__3213E83FED25222E] PRIMARY KEY CLUSTERED ([id] ASC)
    );
END
GO

-- Missing column repair blocks for existing databases.
IF COL_LENGTH(N'dbo.sozlesme_dosyalari', N'id') IS NULL
BEGIN
    ALTER TABLE [dbo].[sozlesme_dosyalari] ADD [id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.sozlesme_dosyalari', N'sozlesme_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[sozlesme_dosyalari] ADD [sozlesme_id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.sozlesme_dosyalari', N'dosya_tipi') IS NULL
BEGIN
    ALTER TABLE [dbo].[sozlesme_dosyalari] ADD [dosya_tipi] nvarchar(40) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.sozlesme_dosyalari', N'dosya_adi') IS NULL
BEGIN
    ALTER TABLE [dbo].[sozlesme_dosyalari] ADD [dosya_adi] nvarchar(250) NULL;
END
GO
IF COL_LENGTH(N'dbo.sozlesme_dosyalari', N'dosya_yolu') IS NULL
BEGIN
    ALTER TABLE [dbo].[sozlesme_dosyalari] ADD [dosya_yolu] nvarchar(500) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.sozlesme_dosyalari', N'mime_tipi') IS NULL
BEGIN
    ALTER TABLE [dbo].[sozlesme_dosyalari] ADD [mime_tipi] nvarchar(120) NULL;
END
GO
IF COL_LENGTH(N'dbo.sozlesme_dosyalari', N'olusturan_kullanici_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[sozlesme_dosyalari] ADD [olusturan_kullanici_id] bigint NULL;
END
GO
IF COL_LENGTH(N'dbo.sozlesme_dosyalari', N'olusturulma_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[sozlesme_dosyalari] ADD [olusturulma_tarihi] datetime2(7) DEFAULT (sysutcdatetime()) NOT NULL;
END
GO
