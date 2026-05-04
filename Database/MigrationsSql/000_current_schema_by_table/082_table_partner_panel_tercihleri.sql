SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.partner_panel_tercihleri', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[partner_panel_tercihleri]
    (
        [id] bigint IDENTITY(1,1) NOT NULL,
        [kullanici_id] bigint NOT NULL,
        [partner_id] bigint NOT NULL,
        [varsayilan_otel_id] bigint NULL,
        [dil] nvarchar(10) NULL,
        [para_birimi] nvarchar(3) NULL,
        [zaman_dilimi] nvarchar(64) NULL,
        [takvim_gorunumu] nvarchar(255) NULL,
        [email_bildirimleri] bit CONSTRAINT [DF__partner_p__email__54968AE5] DEFAULT ((1)) NULL,
        [sms_bildirimleri] bit CONSTRAINT [DF__partner_p__sms_b__558AAF1E] DEFAULT ((0)) NULL,
        [push_bildirimleri] bit CONSTRAINT [DF__partner_p__push___567ED357] DEFAULT ((1)) NULL,
        [masaustu_bildirimleri] bit CONSTRAINT [DF__partner_p__masau__5772F790] DEFAULT ((1)) NULL,
        [yeni_rezervasyon_bildirimi] bit CONSTRAINT [DF__partner_p__yeni___58671BC9] DEFAULT ((1)) NULL,
        [iptal_bildirimi] bit CONSTRAINT [DF__partner_p__iptal__595B4002] DEFAULT ((1)) NULL,
        [odeme_bildirimi] bit CONSTRAINT [DF__partner_p__odeme__5A4F643B] DEFAULT ((1)) NULL,
        [yorum_bildirimi] bit CONSTRAINT [DF__partner_p__yorum__5B438874] DEFAULT ((1)) NULL,
        [otomatik_fiyat_onerileri] bit CONSTRAINT [DF__partner_p__otoma__5C37ACAD] DEFAULT ((1)) NULL,
        [otomatik_kapali_gun_uyarisi] bit CONSTRAINT [DF__partner_p__otoma__5D2BD0E6] DEFAULT ((1)) NULL,
        [cihazi_hatirla] bit CONSTRAINT [DF__partner_p__cihaz__5E1FF51F] DEFAULT ((1)) NULL,
        [olusturulma_tarihi] datetime2(0) CONSTRAINT [DF__partner_p__olust__5F141958] DEFAULT (sysutcdatetime()) NULL,
        [guncellenme_tarihi] datetime2(0) NULL,
        CONSTRAINT [PK_partner_panel_tercihleri] PRIMARY KEY CLUSTERED ([id] ASC)
    );
END
GO

-- Missing column repair blocks for existing databases.
IF COL_LENGTH(N'dbo.partner_panel_tercihleri', N'id') IS NULL
BEGIN
    ALTER TABLE [dbo].[partner_panel_tercihleri] ADD [id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.partner_panel_tercihleri', N'kullanici_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[partner_panel_tercihleri] ADD [kullanici_id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.partner_panel_tercihleri', N'partner_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[partner_panel_tercihleri] ADD [partner_id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.partner_panel_tercihleri', N'varsayilan_otel_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[partner_panel_tercihleri] ADD [varsayilan_otel_id] bigint NULL;
END
GO
IF COL_LENGTH(N'dbo.partner_panel_tercihleri', N'dil') IS NULL
BEGIN
    ALTER TABLE [dbo].[partner_panel_tercihleri] ADD [dil] nvarchar(10) NULL;
END
GO
IF COL_LENGTH(N'dbo.partner_panel_tercihleri', N'para_birimi') IS NULL
BEGIN
    ALTER TABLE [dbo].[partner_panel_tercihleri] ADD [para_birimi] nvarchar(3) NULL;
END
GO
IF COL_LENGTH(N'dbo.partner_panel_tercihleri', N'zaman_dilimi') IS NULL
BEGIN
    ALTER TABLE [dbo].[partner_panel_tercihleri] ADD [zaman_dilimi] nvarchar(64) NULL;
END
GO
IF COL_LENGTH(N'dbo.partner_panel_tercihleri', N'takvim_gorunumu') IS NULL
BEGIN
    ALTER TABLE [dbo].[partner_panel_tercihleri] ADD [takvim_gorunumu] nvarchar(255) NULL;
END
GO
IF COL_LENGTH(N'dbo.partner_panel_tercihleri', N'email_bildirimleri') IS NULL
BEGIN
    ALTER TABLE [dbo].[partner_panel_tercihleri] ADD [email_bildirimleri] bit DEFAULT ((1)) NULL;
END
GO
IF COL_LENGTH(N'dbo.partner_panel_tercihleri', N'sms_bildirimleri') IS NULL
BEGIN
    ALTER TABLE [dbo].[partner_panel_tercihleri] ADD [sms_bildirimleri] bit DEFAULT ((0)) NULL;
END
GO
IF COL_LENGTH(N'dbo.partner_panel_tercihleri', N'push_bildirimleri') IS NULL
BEGIN
    ALTER TABLE [dbo].[partner_panel_tercihleri] ADD [push_bildirimleri] bit DEFAULT ((1)) NULL;
END
GO
IF COL_LENGTH(N'dbo.partner_panel_tercihleri', N'masaustu_bildirimleri') IS NULL
BEGIN
    ALTER TABLE [dbo].[partner_panel_tercihleri] ADD [masaustu_bildirimleri] bit DEFAULT ((1)) NULL;
END
GO
IF COL_LENGTH(N'dbo.partner_panel_tercihleri', N'yeni_rezervasyon_bildirimi') IS NULL
BEGIN
    ALTER TABLE [dbo].[partner_panel_tercihleri] ADD [yeni_rezervasyon_bildirimi] bit DEFAULT ((1)) NULL;
END
GO
IF COL_LENGTH(N'dbo.partner_panel_tercihleri', N'iptal_bildirimi') IS NULL
BEGIN
    ALTER TABLE [dbo].[partner_panel_tercihleri] ADD [iptal_bildirimi] bit DEFAULT ((1)) NULL;
END
GO
IF COL_LENGTH(N'dbo.partner_panel_tercihleri', N'odeme_bildirimi') IS NULL
BEGIN
    ALTER TABLE [dbo].[partner_panel_tercihleri] ADD [odeme_bildirimi] bit DEFAULT ((1)) NULL;
END
GO
IF COL_LENGTH(N'dbo.partner_panel_tercihleri', N'yorum_bildirimi') IS NULL
BEGIN
    ALTER TABLE [dbo].[partner_panel_tercihleri] ADD [yorum_bildirimi] bit DEFAULT ((1)) NULL;
END
GO
IF COL_LENGTH(N'dbo.partner_panel_tercihleri', N'otomatik_fiyat_onerileri') IS NULL
BEGIN
    ALTER TABLE [dbo].[partner_panel_tercihleri] ADD [otomatik_fiyat_onerileri] bit DEFAULT ((1)) NULL;
END
GO
IF COL_LENGTH(N'dbo.partner_panel_tercihleri', N'otomatik_kapali_gun_uyarisi') IS NULL
BEGIN
    ALTER TABLE [dbo].[partner_panel_tercihleri] ADD [otomatik_kapali_gun_uyarisi] bit DEFAULT ((1)) NULL;
END
GO
IF COL_LENGTH(N'dbo.partner_panel_tercihleri', N'cihazi_hatirla') IS NULL
BEGIN
    ALTER TABLE [dbo].[partner_panel_tercihleri] ADD [cihazi_hatirla] bit DEFAULT ((1)) NULL;
END
GO
IF COL_LENGTH(N'dbo.partner_panel_tercihleri', N'olusturulma_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[partner_panel_tercihleri] ADD [olusturulma_tarihi] datetime2(0) DEFAULT (sysutcdatetime()) NULL;
END
GO
IF COL_LENGTH(N'dbo.partner_panel_tercihleri', N'guncellenme_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[partner_panel_tercihleri] ADD [guncellenme_tarihi] datetime2(0) NULL;
END
GO
