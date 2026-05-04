SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.users_partner', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[users_partner]
    (
        [id] bigint IDENTITY(1,1) NOT NULL,
        [user_id] bigint NOT NULL,
        [partner_id] bigint NOT NULL,
        [rol] nvarchar(255) NOT NULL,
        [aktif_mi] bit CONSTRAINT [DF__users_par__aktif__5FD33367] DEFAULT ((1)) NOT NULL,
        [ana_hesap_mi] bit CONSTRAINT [DF__users_par__ana_h__60C757A0] DEFAULT ((1)) NOT NULL,
        [olusturulma_tarihi] datetime2(0) CONSTRAINT [DF__users_par__olust__61BB7BD9] DEFAULT (sysutcdatetime()) NULL,
        [guncellenme_tarihi] datetime2(0) NULL,
        CONSTRAINT [PK_users_partner] PRIMARY KEY CLUSTERED ([id] ASC)
    );
END
GO

-- Missing column repair blocks for existing databases.
IF COL_LENGTH(N'dbo.users_partner', N'id') IS NULL
BEGIN
    ALTER TABLE [dbo].[users_partner] ADD [id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.users_partner', N'user_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[users_partner] ADD [user_id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.users_partner', N'partner_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[users_partner] ADD [partner_id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.users_partner', N'rol') IS NULL
BEGIN
    ALTER TABLE [dbo].[users_partner] ADD [rol] nvarchar(255) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.users_partner', N'aktif_mi') IS NULL
BEGIN
    ALTER TABLE [dbo].[users_partner] ADD [aktif_mi] bit DEFAULT ((1)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.users_partner', N'ana_hesap_mi') IS NULL
BEGIN
    ALTER TABLE [dbo].[users_partner] ADD [ana_hesap_mi] bit DEFAULT ((1)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.users_partner', N'olusturulma_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[users_partner] ADD [olusturulma_tarihi] datetime2(0) DEFAULT (sysutcdatetime()) NULL;
END
GO
IF COL_LENGTH(N'dbo.users_partner', N'guncellenme_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[users_partner] ADD [guncellenme_tarihi] datetime2(0) NULL;
END
GO
