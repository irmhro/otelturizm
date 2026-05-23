SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.sss_sorulari', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[sss_sorulari]
    (
        [id] bigint IDENTITY(1,1) NOT NULL,
        [sss_kategori_id] bigint NOT NULL,
        [soru] nvarchar(255) NOT NULL,
        [cevap] nvarchar(max) NOT NULL,
        [one_cikan_mi] bit CONSTRAINT [DF__sss_sorul__one_c__33F4B129] DEFAULT ((0)) NOT NULL,
        [siralama] int CONSTRAINT [DF__sss_sorul__siral__34E8D562] DEFAULT ((0)) NOT NULL,
        [aktif_mi] bit CONSTRAINT [DF__sss_sorul__aktif__35DCF99B] DEFAULT ((1)) NOT NULL,
        [olusturulma_tarihi] datetime2(0) CONSTRAINT [DF__sss_sorul__olust__36D11DD4] DEFAULT (sysutcdatetime()) NOT NULL,
        [guncellenme_tarihi] datetime2(0) CONSTRAINT [DF__sss_sorul__gunce__37C5420D] DEFAULT (sysutcdatetime()) NOT NULL,
        CONSTRAINT [PK_sss_sorulari] PRIMARY KEY CLUSTERED ([id] ASC)
    );
END
GO

-- Missing column repair blocks for existing databases.
IF COL_LENGTH(N'dbo.sss_sorulari', N'id') IS NULL
BEGIN
    ALTER TABLE [dbo].[sss_sorulari] ADD [id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.sss_sorulari', N'sss_kategori_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[sss_sorulari] ADD [sss_kategori_id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.sss_sorulari', N'soru') IS NULL
BEGIN
    ALTER TABLE [dbo].[sss_sorulari] ADD [soru] nvarchar(255) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.sss_sorulari', N'cevap') IS NULL
BEGIN
    ALTER TABLE [dbo].[sss_sorulari] ADD [cevap] nvarchar(max) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.sss_sorulari', N'one_cikan_mi') IS NULL
BEGIN
    ALTER TABLE [dbo].[sss_sorulari] ADD [one_cikan_mi] bit DEFAULT ((0)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.sss_sorulari', N'siralama') IS NULL
BEGIN
    ALTER TABLE [dbo].[sss_sorulari] ADD [siralama] int DEFAULT ((0)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.sss_sorulari', N'aktif_mi') IS NULL
BEGIN
    ALTER TABLE [dbo].[sss_sorulari] ADD [aktif_mi] bit DEFAULT ((1)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.sss_sorulari', N'olusturulma_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[sss_sorulari] ADD [olusturulma_tarihi] datetime2(0) DEFAULT (sysutcdatetime()) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.sss_sorulari', N'guncellenme_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[sss_sorulari] ADD [guncellenme_tarihi] datetime2(0) DEFAULT (sysutcdatetime()) NOT NULL;
END
GO
