SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.kullanici_seyahat_planlari', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[kullanici_seyahat_planlari]
    (
        [id] bigint IDENTITY(1,1) NOT NULL,
        [olusturan_kullanici_id] bigint NOT NULL,
        [plan_kodu] nvarchar(80) NOT NULL,
        [plan_adi] nvarchar(180) NOT NULL,
        [hedef_sehir] nvarchar(120) NOT NULL,
        [baslangic_tarihi] date NULL,
        [bitis_tarihi] date NULL,
        [butce_tutari] decimal(12,2) NULL,
        [para_birimi] nvarchar(10) NOT NULL,
        [davet_kodu] nvarchar(40) NULL,
        [durum] nvarchar(30) NOT NULL,
        [olusturulma_tarihi] datetime2(0) CONSTRAINT [DF__kullanici__olust__2F9A1060] DEFAULT (sysutcdatetime()) NOT NULL,
        [guncellenme_tarihi] datetime2(0) CONSTRAINT [DF__kullanici__gunce__308E3499] DEFAULT (sysutcdatetime()) NOT NULL,
        CONSTRAINT [PK_kullanici_seyahat_planlari] PRIMARY KEY CLUSTERED ([id] ASC)
    );
END
GO

-- Missing column repair blocks for existing databases.
IF COL_LENGTH(N'dbo.kullanici_seyahat_planlari', N'id') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_seyahat_planlari] ADD [id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.kullanici_seyahat_planlari', N'olusturan_kullanici_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_seyahat_planlari] ADD [olusturan_kullanici_id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.kullanici_seyahat_planlari', N'plan_kodu') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_seyahat_planlari] ADD [plan_kodu] nvarchar(80) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.kullanici_seyahat_planlari', N'plan_adi') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_seyahat_planlari] ADD [plan_adi] nvarchar(180) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.kullanici_seyahat_planlari', N'hedef_sehir') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_seyahat_planlari] ADD [hedef_sehir] nvarchar(120) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.kullanici_seyahat_planlari', N'baslangic_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_seyahat_planlari] ADD [baslangic_tarihi] date NULL;
END
GO
IF COL_LENGTH(N'dbo.kullanici_seyahat_planlari', N'bitis_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_seyahat_planlari] ADD [bitis_tarihi] date NULL;
END
GO
IF COL_LENGTH(N'dbo.kullanici_seyahat_planlari', N'butce_tutari') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_seyahat_planlari] ADD [butce_tutari] decimal(12,2) NULL;
END
GO
IF COL_LENGTH(N'dbo.kullanici_seyahat_planlari', N'para_birimi') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_seyahat_planlari] ADD [para_birimi] nvarchar(10) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.kullanici_seyahat_planlari', N'davet_kodu') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_seyahat_planlari] ADD [davet_kodu] nvarchar(40) NULL;
END
GO
IF COL_LENGTH(N'dbo.kullanici_seyahat_planlari', N'durum') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_seyahat_planlari] ADD [durum] nvarchar(30) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.kullanici_seyahat_planlari', N'olusturulma_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_seyahat_planlari] ADD [olusturulma_tarihi] datetime2(0) DEFAULT (sysutcdatetime()) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.kullanici_seyahat_planlari', N'guncellenme_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_seyahat_planlari] ADD [guncellenme_tarihi] datetime2(0) DEFAULT (sysutcdatetime()) NOT NULL;
END
GO
