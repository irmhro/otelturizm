SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.kullanici_bildirim_cihazlari', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[kullanici_bildirim_cihazlari]
    (
        [id] bigint IDENTITY(1,1) NOT NULL,
        [kullanici_id] bigint NOT NULL,
        [cihaz_turu] nvarchar(255) NOT NULL,
        [cihaz_token] nvarchar(255) NOT NULL,
        [cihaz_adi] nvarchar(100) NULL,
        [cihaz_modeli] nvarchar(50) NULL,
        [isletim_sistemi_surumu] nvarchar(20) NULL,
        [uygulama_surumu] nvarchar(10) NULL,
        [bildirim_izinleri] nvarchar(max) NULL,
        [son_kullanim_tarihi] datetime2(0) NULL,
        [aktif_mi] bit CONSTRAINT [DF__kullanici__aktif__6EC0713C] DEFAULT ((1)) NULL,
        [olusturulma_tarihi] datetime2(0) CONSTRAINT [DF__kullanici__olust__6FB49575] DEFAULT (sysutcdatetime()) NULL,
        [guncellenme_tarihi] datetime2(0) NULL,
        [son_bildirim_tarihi] datetime2(0) NULL,
        CONSTRAINT [PK_kullanici_bildirim_cihazlari] PRIMARY KEY CLUSTERED ([id] ASC)
    );
END
GO

-- Missing column repair blocks for existing databases.
IF COL_LENGTH(N'dbo.kullanici_bildirim_cihazlari', N'id') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_bildirim_cihazlari] ADD [id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.kullanici_bildirim_cihazlari', N'kullanici_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_bildirim_cihazlari] ADD [kullanici_id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.kullanici_bildirim_cihazlari', N'cihaz_turu') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_bildirim_cihazlari] ADD [cihaz_turu] nvarchar(255) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.kullanici_bildirim_cihazlari', N'cihaz_token') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_bildirim_cihazlari] ADD [cihaz_token] nvarchar(255) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.kullanici_bildirim_cihazlari', N'cihaz_adi') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_bildirim_cihazlari] ADD [cihaz_adi] nvarchar(100) NULL;
END
GO
IF COL_LENGTH(N'dbo.kullanici_bildirim_cihazlari', N'cihaz_modeli') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_bildirim_cihazlari] ADD [cihaz_modeli] nvarchar(50) NULL;
END
GO
IF COL_LENGTH(N'dbo.kullanici_bildirim_cihazlari', N'isletim_sistemi_surumu') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_bildirim_cihazlari] ADD [isletim_sistemi_surumu] nvarchar(20) NULL;
END
GO
IF COL_LENGTH(N'dbo.kullanici_bildirim_cihazlari', N'uygulama_surumu') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_bildirim_cihazlari] ADD [uygulama_surumu] nvarchar(10) NULL;
END
GO
IF COL_LENGTH(N'dbo.kullanici_bildirim_cihazlari', N'bildirim_izinleri') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_bildirim_cihazlari] ADD [bildirim_izinleri] nvarchar(max) NULL;
END
GO
IF COL_LENGTH(N'dbo.kullanici_bildirim_cihazlari', N'son_kullanim_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_bildirim_cihazlari] ADD [son_kullanim_tarihi] datetime2(0) NULL;
END
GO
IF COL_LENGTH(N'dbo.kullanici_bildirim_cihazlari', N'aktif_mi') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_bildirim_cihazlari] ADD [aktif_mi] bit DEFAULT ((1)) NULL;
END
GO
IF COL_LENGTH(N'dbo.kullanici_bildirim_cihazlari', N'olusturulma_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_bildirim_cihazlari] ADD [olusturulma_tarihi] datetime2(0) DEFAULT (sysutcdatetime()) NULL;
END
GO
IF COL_LENGTH(N'dbo.kullanici_bildirim_cihazlari', N'guncellenme_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_bildirim_cihazlari] ADD [guncellenme_tarihi] datetime2(0) NULL;
END
GO
IF COL_LENGTH(N'dbo.kullanici_bildirim_cihazlari', N'son_bildirim_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_bildirim_cihazlari] ADD [son_bildirim_tarihi] datetime2(0) NULL;
END
GO
