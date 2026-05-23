SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.user_favori_oteller', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[user_favori_oteller]
    (
        [id] bigint IDENTITY(1,1) NOT NULL,
        [user_id] bigint NOT NULL,
        [otel_id] bigint NOT NULL,
        [kaynak_sayfa] nvarchar(100) NULL,
        [kaynak_url] nvarchar(500) NULL,
        [cihaz_tipi] nvarchar(50) NULL,
        [ip_adresi] nvarchar(45) NULL,
        [aktif_mi] bit CONSTRAINT [DF__user_favo__aktif__3AA1AEB8] DEFAULT ((1)) NOT NULL,
        [son_islem_tarihi] datetime2(0) CONSTRAINT [DF__user_favo__son_i__3B95D2F1] DEFAULT (sysutcdatetime()) NULL,
        [kaldirilma_tarihi] datetime2(0) NULL,
        [olusturulma_tarihi] datetime2(0) CONSTRAINT [DF__user_favo__olust__3C89F72A] DEFAULT (sysutcdatetime()) NOT NULL,
        CONSTRAINT [PK_user_favori_oteller] PRIMARY KEY CLUSTERED ([id] ASC)
    );
END
GO

-- Missing column repair blocks for existing databases.
IF COL_LENGTH(N'dbo.user_favori_oteller', N'id') IS NULL
BEGIN
    ALTER TABLE [dbo].[user_favori_oteller] ADD [id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.user_favori_oteller', N'user_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[user_favori_oteller] ADD [user_id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.user_favori_oteller', N'otel_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[user_favori_oteller] ADD [otel_id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.user_favori_oteller', N'kaynak_sayfa') IS NULL
BEGIN
    ALTER TABLE [dbo].[user_favori_oteller] ADD [kaynak_sayfa] nvarchar(100) NULL;
END
GO
IF COL_LENGTH(N'dbo.user_favori_oteller', N'kaynak_url') IS NULL
BEGIN
    ALTER TABLE [dbo].[user_favori_oteller] ADD [kaynak_url] nvarchar(500) NULL;
END
GO
IF COL_LENGTH(N'dbo.user_favori_oteller', N'cihaz_tipi') IS NULL
BEGIN
    ALTER TABLE [dbo].[user_favori_oteller] ADD [cihaz_tipi] nvarchar(50) NULL;
END
GO
IF COL_LENGTH(N'dbo.user_favori_oteller', N'ip_adresi') IS NULL
BEGIN
    ALTER TABLE [dbo].[user_favori_oteller] ADD [ip_adresi] nvarchar(45) NULL;
END
GO
IF COL_LENGTH(N'dbo.user_favori_oteller', N'aktif_mi') IS NULL
BEGIN
    ALTER TABLE [dbo].[user_favori_oteller] ADD [aktif_mi] bit DEFAULT ((1)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.user_favori_oteller', N'son_islem_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[user_favori_oteller] ADD [son_islem_tarihi] datetime2(0) DEFAULT (sysutcdatetime()) NULL;
END
GO
IF COL_LENGTH(N'dbo.user_favori_oteller', N'kaldirilma_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[user_favori_oteller] ADD [kaldirilma_tarihi] datetime2(0) NULL;
END
GO
IF COL_LENGTH(N'dbo.user_favori_oteller', N'olusturulma_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[user_favori_oteller] ADD [olusturulma_tarihi] datetime2(0) DEFAULT (sysutcdatetime()) NOT NULL;
END
GO
