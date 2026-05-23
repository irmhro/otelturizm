SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.satis_musteri_notlari', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[satis_musteri_notlari]
    (
        [id] bigint IDENTITY(1,1) NOT NULL,
        [satis_musteri_id] bigint NOT NULL,
        [sales_user_id] bigint NULL,
        [not_turu] nvarchar(255) NOT NULL,
        [not_metni] nvarchar(max) NOT NULL,
        [planlanan_geri_donus_tarihi] datetime2(0) NULL,
        [olusturulma_tarihi] datetime2(0) CONSTRAINT [DF__satis_mus__olust__0FB750B3] DEFAULT (sysutcdatetime()) NULL,
        CONSTRAINT [PK_satis_musteri_notlari] PRIMARY KEY CLUSTERED ([id] ASC)
    );
END
GO

-- Missing column repair blocks for existing databases.
IF COL_LENGTH(N'dbo.satis_musteri_notlari', N'id') IS NULL
BEGIN
    ALTER TABLE [dbo].[satis_musteri_notlari] ADD [id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.satis_musteri_notlari', N'satis_musteri_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[satis_musteri_notlari] ADD [satis_musteri_id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.satis_musteri_notlari', N'sales_user_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[satis_musteri_notlari] ADD [sales_user_id] bigint NULL;
END
GO
IF COL_LENGTH(N'dbo.satis_musteri_notlari', N'not_turu') IS NULL
BEGIN
    ALTER TABLE [dbo].[satis_musteri_notlari] ADD [not_turu] nvarchar(255) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.satis_musteri_notlari', N'not_metni') IS NULL
BEGIN
    ALTER TABLE [dbo].[satis_musteri_notlari] ADD [not_metni] nvarchar(max) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.satis_musteri_notlari', N'planlanan_geri_donus_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[satis_musteri_notlari] ADD [planlanan_geri_donus_tarihi] datetime2(0) NULL;
END
GO
IF COL_LENGTH(N'dbo.satis_musteri_notlari', N'olusturulma_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[satis_musteri_notlari] ADD [olusturulma_tarihi] datetime2(0) DEFAULT (sysutcdatetime()) NULL;
END
GO
