SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.partner_destek_talepleri', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[partner_destek_talepleri]
    (
        [id] bigint IDENTITY(1,1) NOT NULL,
        [partner_id] bigint NOT NULL,
        [kullanici_id] bigint NOT NULL,
        [otel_id] bigint NULL,
        [talep_no] nvarchar(32) NOT NULL,
        [konu] nvarchar(200) NOT NULL,
        [kategori] nvarchar(100) NOT NULL,
        [oncelik] nvarchar(255) NULL,
        [durum] nvarchar(255) NULL,
        [atanan_admin_id] bigint NULL,
        [son_mesaj_tarihi] datetime2(0) NULL,
        [olusturulma_tarihi] datetime2(0) CONSTRAINT [DF__partner_d__olust__4EDDB18F] DEFAULT (sysutcdatetime()) NULL,
        [guncellenme_tarihi] datetime2(0) NULL,
        CONSTRAINT [PK_partner_destek_talepleri] PRIMARY KEY CLUSTERED ([id] ASC)
    );
END
GO

-- Missing column repair blocks for existing databases.
IF COL_LENGTH(N'dbo.partner_destek_talepleri', N'id') IS NULL
BEGIN
    ALTER TABLE [dbo].[partner_destek_talepleri] ADD [id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.partner_destek_talepleri', N'partner_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[partner_destek_talepleri] ADD [partner_id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.partner_destek_talepleri', N'kullanici_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[partner_destek_talepleri] ADD [kullanici_id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.partner_destek_talepleri', N'otel_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[partner_destek_talepleri] ADD [otel_id] bigint NULL;
END
GO
IF COL_LENGTH(N'dbo.partner_destek_talepleri', N'talep_no') IS NULL
BEGIN
    ALTER TABLE [dbo].[partner_destek_talepleri] ADD [talep_no] nvarchar(32) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.partner_destek_talepleri', N'konu') IS NULL
BEGIN
    ALTER TABLE [dbo].[partner_destek_talepleri] ADD [konu] nvarchar(200) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.partner_destek_talepleri', N'kategori') IS NULL
BEGIN
    ALTER TABLE [dbo].[partner_destek_talepleri] ADD [kategori] nvarchar(100) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.partner_destek_talepleri', N'oncelik') IS NULL
BEGIN
    ALTER TABLE [dbo].[partner_destek_talepleri] ADD [oncelik] nvarchar(255) NULL;
END
GO
IF COL_LENGTH(N'dbo.partner_destek_talepleri', N'durum') IS NULL
BEGIN
    ALTER TABLE [dbo].[partner_destek_talepleri] ADD [durum] nvarchar(255) NULL;
END
GO
IF COL_LENGTH(N'dbo.partner_destek_talepleri', N'atanan_admin_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[partner_destek_talepleri] ADD [atanan_admin_id] bigint NULL;
END
GO
IF COL_LENGTH(N'dbo.partner_destek_talepleri', N'son_mesaj_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[partner_destek_talepleri] ADD [son_mesaj_tarihi] datetime2(0) NULL;
END
GO
IF COL_LENGTH(N'dbo.partner_destek_talepleri', N'olusturulma_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[partner_destek_talepleri] ADD [olusturulma_tarihi] datetime2(0) DEFAULT (sysutcdatetime()) NULL;
END
GO
IF COL_LENGTH(N'dbo.partner_destek_talepleri', N'guncellenme_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[partner_destek_talepleri] ADD [guncellenme_tarihi] datetime2(0) NULL;
END
GO
