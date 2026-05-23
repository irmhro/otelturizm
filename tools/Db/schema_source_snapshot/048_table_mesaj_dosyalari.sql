SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.mesaj_dosyalari', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[mesaj_dosyalari]
    (
        [id] bigint IDENTITY(1,1) NOT NULL,
        [mesaj_id] bigint NOT NULL,
        [guvenli_dosya_id] bigint NOT NULL,
        [gosterim_adi] nvarchar(255) NULL,
        [siralama] int CONSTRAINT [DF__mesaj_dos__siral__373B3228] DEFAULT ((1)) NOT NULL,
        [aktif_mi] bit CONSTRAINT [DF__mesaj_dos__aktif__382F5661] DEFAULT ((1)) NOT NULL,
        [olusturulma_tarihi] datetime2(0) CONSTRAINT [DF__mesaj_dos__olust__39237A9A] DEFAULT (sysutcdatetime()) NOT NULL,
        CONSTRAINT [PK_mesaj_dosyalari] PRIMARY KEY CLUSTERED ([id] ASC)
    );
END
GO

-- Missing column repair blocks for existing databases.
IF COL_LENGTH(N'dbo.mesaj_dosyalari', N'id') IS NULL
BEGIN
    ALTER TABLE [dbo].[mesaj_dosyalari] ADD [id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.mesaj_dosyalari', N'mesaj_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[mesaj_dosyalari] ADD [mesaj_id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.mesaj_dosyalari', N'guvenli_dosya_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[mesaj_dosyalari] ADD [guvenli_dosya_id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.mesaj_dosyalari', N'gosterim_adi') IS NULL
BEGIN
    ALTER TABLE [dbo].[mesaj_dosyalari] ADD [gosterim_adi] nvarchar(255) NULL;
END
GO
IF COL_LENGTH(N'dbo.mesaj_dosyalari', N'siralama') IS NULL
BEGIN
    ALTER TABLE [dbo].[mesaj_dosyalari] ADD [siralama] int DEFAULT ((1)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.mesaj_dosyalari', N'aktif_mi') IS NULL
BEGIN
    ALTER TABLE [dbo].[mesaj_dosyalari] ADD [aktif_mi] bit DEFAULT ((1)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.mesaj_dosyalari', N'olusturulma_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[mesaj_dosyalari] ADD [olusturulma_tarihi] datetime2(0) DEFAULT (sysutcdatetime()) NOT NULL;
END
GO
