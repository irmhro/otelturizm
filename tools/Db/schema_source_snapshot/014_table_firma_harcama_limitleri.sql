SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.firma_harcama_limitleri', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[firma_harcama_limitleri]
    (
        [id] bigint IDENTITY(1,1) NOT NULL,
        [firma_id] bigint NOT NULL,
        [departman] nvarchar(100) NULL,
        [kullanici_id] bigint NULL,
        [gecelik_limit] decimal(10,2) NULL,
        [rezervasyon_basi_limit] decimal(10,2) NULL,
        [aylik_limit] decimal(10,2) NULL,
        [onay_gereksinimi] bit CONSTRAINT [DF__firma_har__onay___3587F3E0] DEFAULT ((0)) NOT NULL,
        [otomatik_onay_limit] decimal(10,2) NULL,
        [aktif_mi] bit CONSTRAINT [DF__firma_har__aktif__367C1819] DEFAULT ((1)) NOT NULL,
        [olusturulma_tarihi] datetime2(0) CONSTRAINT [DF__firma_har__olust__37703C52] DEFAULT (sysutcdatetime()) NULL,
        [guncellenme_tarihi] datetime2(0) NULL,
        CONSTRAINT [PK_firma_harcama_limitleri] PRIMARY KEY CLUSTERED ([id] ASC)
    );
END
GO

-- Missing column repair blocks for existing databases.
IF COL_LENGTH(N'dbo.firma_harcama_limitleri', N'id') IS NULL
BEGIN
    ALTER TABLE [dbo].[firma_harcama_limitleri] ADD [id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.firma_harcama_limitleri', N'firma_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[firma_harcama_limitleri] ADD [firma_id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.firma_harcama_limitleri', N'departman') IS NULL
BEGIN
    ALTER TABLE [dbo].[firma_harcama_limitleri] ADD [departman] nvarchar(100) NULL;
END
GO
IF COL_LENGTH(N'dbo.firma_harcama_limitleri', N'kullanici_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[firma_harcama_limitleri] ADD [kullanici_id] bigint NULL;
END
GO
IF COL_LENGTH(N'dbo.firma_harcama_limitleri', N'gecelik_limit') IS NULL
BEGIN
    ALTER TABLE [dbo].[firma_harcama_limitleri] ADD [gecelik_limit] decimal(10,2) NULL;
END
GO
IF COL_LENGTH(N'dbo.firma_harcama_limitleri', N'rezervasyon_basi_limit') IS NULL
BEGIN
    ALTER TABLE [dbo].[firma_harcama_limitleri] ADD [rezervasyon_basi_limit] decimal(10,2) NULL;
END
GO
IF COL_LENGTH(N'dbo.firma_harcama_limitleri', N'aylik_limit') IS NULL
BEGIN
    ALTER TABLE [dbo].[firma_harcama_limitleri] ADD [aylik_limit] decimal(10,2) NULL;
END
GO
IF COL_LENGTH(N'dbo.firma_harcama_limitleri', N'onay_gereksinimi') IS NULL
BEGIN
    ALTER TABLE [dbo].[firma_harcama_limitleri] ADD [onay_gereksinimi] bit DEFAULT ((0)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.firma_harcama_limitleri', N'otomatik_onay_limit') IS NULL
BEGIN
    ALTER TABLE [dbo].[firma_harcama_limitleri] ADD [otomatik_onay_limit] decimal(10,2) NULL;
END
GO
IF COL_LENGTH(N'dbo.firma_harcama_limitleri', N'aktif_mi') IS NULL
BEGIN
    ALTER TABLE [dbo].[firma_harcama_limitleri] ADD [aktif_mi] bit DEFAULT ((1)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.firma_harcama_limitleri', N'olusturulma_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[firma_harcama_limitleri] ADD [olusturulma_tarihi] datetime2(0) DEFAULT (sysutcdatetime()) NULL;
END
GO
IF COL_LENGTH(N'dbo.firma_harcama_limitleri', N'guncellenme_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[firma_harcama_limitleri] ADD [guncellenme_tarihi] datetime2(0) NULL;
END
GO
