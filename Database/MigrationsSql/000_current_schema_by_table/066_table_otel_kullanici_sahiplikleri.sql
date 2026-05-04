SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.otel_kullanici_sahiplikleri', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[otel_kullanici_sahiplikleri]
    (
        [id] bigint IDENTITY(1,1) NOT NULL,
        [otel_id] bigint NOT NULL,
        [user_id] bigint NOT NULL,
        [partner_id] bigint NOT NULL,
        [rol] nvarchar(255) NOT NULL,
        [ana_sorumlu_mu] bit CONSTRAINT [DF__otel_kull__ana_s__178D7CA5] DEFAULT ((0)) NOT NULL,
        [aktif_mi] bit CONSTRAINT [DF__otel_kull__aktif__1881A0DE] DEFAULT ((1)) NOT NULL,
        [olusturulma_tarihi] datetime2(0) CONSTRAINT [DF__otel_kull__olust__1975C517] DEFAULT (sysutcdatetime()) NOT NULL,
        [guncellenme_tarihi] datetime2(0) NULL,
        CONSTRAINT [PK_otel_kullanici_sahiplikleri] PRIMARY KEY CLUSTERED ([id] ASC)
    );
END
GO

-- Missing column repair blocks for existing databases.
IF COL_LENGTH(N'dbo.otel_kullanici_sahiplikleri', N'id') IS NULL
BEGIN
    ALTER TABLE [dbo].[otel_kullanici_sahiplikleri] ADD [id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.otel_kullanici_sahiplikleri', N'otel_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[otel_kullanici_sahiplikleri] ADD [otel_id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.otel_kullanici_sahiplikleri', N'user_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[otel_kullanici_sahiplikleri] ADD [user_id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.otel_kullanici_sahiplikleri', N'partner_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[otel_kullanici_sahiplikleri] ADD [partner_id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.otel_kullanici_sahiplikleri', N'rol') IS NULL
BEGIN
    ALTER TABLE [dbo].[otel_kullanici_sahiplikleri] ADD [rol] nvarchar(255) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.otel_kullanici_sahiplikleri', N'ana_sorumlu_mu') IS NULL
BEGIN
    ALTER TABLE [dbo].[otel_kullanici_sahiplikleri] ADD [ana_sorumlu_mu] bit DEFAULT ((0)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.otel_kullanici_sahiplikleri', N'aktif_mi') IS NULL
BEGIN
    ALTER TABLE [dbo].[otel_kullanici_sahiplikleri] ADD [aktif_mi] bit DEFAULT ((1)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.otel_kullanici_sahiplikleri', N'olusturulma_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[otel_kullanici_sahiplikleri] ADD [olusturulma_tarihi] datetime2(0) DEFAULT (sysutcdatetime()) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.otel_kullanici_sahiplikleri', N'guncellenme_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[otel_kullanici_sahiplikleri] ADD [guncellenme_tarihi] datetime2(0) NULL;
END
GO
