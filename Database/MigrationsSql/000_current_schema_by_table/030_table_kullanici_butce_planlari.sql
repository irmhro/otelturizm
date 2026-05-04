SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.kullanici_butce_planlari', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[kullanici_butce_planlari]
    (
        [id] bigint IDENTITY(1,1) NOT NULL,
        [kullanici_id] bigint NOT NULL,
        [hedef_sehir] nvarchar(120) NOT NULL,
        [hedef_butce] decimal(12,2) NOT NULL,
        [gece_sayisi] int CONSTRAINT [DF__kullanici__gece___7C1A6C5A] DEFAULT ((1)) NOT NULL,
        [kisi_sayisi] int CONSTRAINT [DF__kullanici__kisi___7D0E9093] DEFAULT ((1)) NOT NULL,
        [para_birimi] nvarchar(10) NOT NULL,
        [notlar] nvarchar(255) NULL,
        [olusturulma_tarihi] datetime2(0) CONSTRAINT [DF__kullanici__olust__7E02B4CC] DEFAULT (sysutcdatetime()) NOT NULL,
        [guncellenme_tarihi] datetime2(0) CONSTRAINT [DF__kullanici__gunce__7EF6D905] DEFAULT (sysutcdatetime()) NOT NULL,
        CONSTRAINT [PK_kullanici_butce_planlari] PRIMARY KEY CLUSTERED ([id] ASC)
    );
END
GO

-- Missing column repair blocks for existing databases.
IF COL_LENGTH(N'dbo.kullanici_butce_planlari', N'id') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_butce_planlari] ADD [id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.kullanici_butce_planlari', N'kullanici_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_butce_planlari] ADD [kullanici_id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.kullanici_butce_planlari', N'hedef_sehir') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_butce_planlari] ADD [hedef_sehir] nvarchar(120) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.kullanici_butce_planlari', N'hedef_butce') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_butce_planlari] ADD [hedef_butce] decimal(12,2) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.kullanici_butce_planlari', N'gece_sayisi') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_butce_planlari] ADD [gece_sayisi] int DEFAULT ((1)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.kullanici_butce_planlari', N'kisi_sayisi') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_butce_planlari] ADD [kisi_sayisi] int DEFAULT ((1)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.kullanici_butce_planlari', N'para_birimi') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_butce_planlari] ADD [para_birimi] nvarchar(10) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.kullanici_butce_planlari', N'notlar') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_butce_planlari] ADD [notlar] nvarchar(255) NULL;
END
GO
IF COL_LENGTH(N'dbo.kullanici_butce_planlari', N'olusturulma_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_butce_planlari] ADD [olusturulma_tarihi] datetime2(0) DEFAULT (sysutcdatetime()) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.kullanici_butce_planlari', N'guncellenme_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_butce_planlari] ADD [guncellenme_tarihi] datetime2(0) DEFAULT (sysutcdatetime()) NOT NULL;
END
GO
