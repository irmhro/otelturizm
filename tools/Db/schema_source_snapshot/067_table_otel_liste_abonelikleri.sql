SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.otel_liste_abonelikleri', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[otel_liste_abonelikleri]
    (
        [id] bigint IDENTITY(1,1) NOT NULL,
        [otel_id] bigint NOT NULL,
        [kapsam_tipi] nvarchar(16) NOT NULL,
        [kapsam_degeri] nvarchar(160) NOT NULL,
        [kapsam_degeri_normalized] nvarchar(160) NOT NULL,
        [hedef_sira] int NOT NULL,
        [baslangic_utc] datetime2(7) NOT NULL,
        [bitis_utc] datetime2(7) NOT NULL,
        [durum] nvarchar(20) CONSTRAINT [DF__otel_list__durum__5F691F13] DEFAULT (N'Beklemede') NOT NULL,
        [talep_eden_user_id] bigint NULL,
        [onaylayan_admin_user_id] bigint NULL,
        [admin_notu] nvarchar(500) NULL,
        [partner_notu] nvarchar(500) NULL,
        [olusturulma_tarihi] datetime2(7) CONSTRAINT [DF__otel_list__olust__605D434C] DEFAULT (sysutcdatetime()) NOT NULL,
        [onay_tarihi] datetime2(7) NULL,
        CONSTRAINT [PK__otel_lis__3213E83F47B22F40] PRIMARY KEY CLUSTERED ([id] ASC)
    );
END
GO

-- Missing column repair blocks for existing databases.
IF COL_LENGTH(N'dbo.otel_liste_abonelikleri', N'id') IS NULL
BEGIN
    ALTER TABLE [dbo].[otel_liste_abonelikleri] ADD [id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.otel_liste_abonelikleri', N'otel_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[otel_liste_abonelikleri] ADD [otel_id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.otel_liste_abonelikleri', N'kapsam_tipi') IS NULL
BEGIN
    ALTER TABLE [dbo].[otel_liste_abonelikleri] ADD [kapsam_tipi] nvarchar(16) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.otel_liste_abonelikleri', N'kapsam_degeri') IS NULL
BEGIN
    ALTER TABLE [dbo].[otel_liste_abonelikleri] ADD [kapsam_degeri] nvarchar(160) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.otel_liste_abonelikleri', N'kapsam_degeri_normalized') IS NULL
BEGIN
    ALTER TABLE [dbo].[otel_liste_abonelikleri] ADD [kapsam_degeri_normalized] nvarchar(160) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.otel_liste_abonelikleri', N'hedef_sira') IS NULL
BEGIN
    ALTER TABLE [dbo].[otel_liste_abonelikleri] ADD [hedef_sira] int NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.otel_liste_abonelikleri', N'baslangic_utc') IS NULL
BEGIN
    ALTER TABLE [dbo].[otel_liste_abonelikleri] ADD [baslangic_utc] datetime2(7) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.otel_liste_abonelikleri', N'bitis_utc') IS NULL
BEGIN
    ALTER TABLE [dbo].[otel_liste_abonelikleri] ADD [bitis_utc] datetime2(7) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.otel_liste_abonelikleri', N'durum') IS NULL
BEGIN
    ALTER TABLE [dbo].[otel_liste_abonelikleri] ADD [durum] nvarchar(20) DEFAULT (N'Beklemede') NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.otel_liste_abonelikleri', N'talep_eden_user_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[otel_liste_abonelikleri] ADD [talep_eden_user_id] bigint NULL;
END
GO
IF COL_LENGTH(N'dbo.otel_liste_abonelikleri', N'onaylayan_admin_user_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[otel_liste_abonelikleri] ADD [onaylayan_admin_user_id] bigint NULL;
END
GO
IF COL_LENGTH(N'dbo.otel_liste_abonelikleri', N'admin_notu') IS NULL
BEGIN
    ALTER TABLE [dbo].[otel_liste_abonelikleri] ADD [admin_notu] nvarchar(500) NULL;
END
GO
IF COL_LENGTH(N'dbo.otel_liste_abonelikleri', N'partner_notu') IS NULL
BEGIN
    ALTER TABLE [dbo].[otel_liste_abonelikleri] ADD [partner_notu] nvarchar(500) NULL;
END
GO
IF COL_LENGTH(N'dbo.otel_liste_abonelikleri', N'olusturulma_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[otel_liste_abonelikleri] ADD [olusturulma_tarihi] datetime2(7) DEFAULT (sysutcdatetime()) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.otel_liste_abonelikleri', N'onay_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[otel_liste_abonelikleri] ADD [onay_tarihi] datetime2(7) NULL;
END
GO
