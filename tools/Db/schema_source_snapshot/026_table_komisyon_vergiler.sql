SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.komisyon_vergiler', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[komisyon_vergiler]
    (
        [id] bigint IDENTITY(1,1) NOT NULL,
        [otel_id] bigint NOT NULL,
        [baslangic_tarihi] date NOT NULL,
        [bitis_tarihi] date NULL,
        [komisyon_orani] decimal(5,2) CONSTRAINT [DF_komisyon_vergiler_komisyon_orani] DEFAULT ((0)) NOT NULL,
        [komisyon_gelir_vergisi_orani] decimal(5,2) CONSTRAINT [DF_komisyon_vergiler_komisyon_gelir_vergisi_orani] DEFAULT ((0)) NOT NULL,
        [kdv_orani] decimal(5,2) CONSTRAINT [DF_komisyon_vergiler_kdv_orani] DEFAULT ((0)) NOT NULL,
        [konaklama_vergisi_orani] decimal(5,2) CONSTRAINT [DF_komisyon_vergiler_konaklama_vergisi_orani] DEFAULT ((0)) NOT NULL,
        [para_birimi] nvarchar(3) CONSTRAINT [DF_komisyon_vergiler_para_birimi] DEFAULT (N'TRY') NOT NULL,
        [aktif_mi] bit CONSTRAINT [DF_komisyon_vergiler_aktif_mi] DEFAULT ((1)) NOT NULL,
        [aciklama] nvarchar(500) NULL,
        [olusturan_kullanici_id] bigint NULL,
        [guncelleyen_kullanici_id] bigint NULL,
        [olusturulma_tarihi] datetime2(7) CONSTRAINT [DF_komisyon_vergiler_olusturulma_tarihi] DEFAULT (sysutcdatetime()) NOT NULL,
        [guncellenme_tarihi] datetime2(7) NULL,
        CONSTRAINT [PK__komisyon__3213E83F6E712D62] PRIMARY KEY CLUSTERED ([id] ASC)
    );
END
GO

-- Missing column repair blocks for existing databases.
IF COL_LENGTH(N'dbo.komisyon_vergiler', N'id') IS NULL
BEGIN
    ALTER TABLE [dbo].[komisyon_vergiler] ADD [id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.komisyon_vergiler', N'otel_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[komisyon_vergiler] ADD [otel_id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.komisyon_vergiler', N'baslangic_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[komisyon_vergiler] ADD [baslangic_tarihi] date NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.komisyon_vergiler', N'bitis_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[komisyon_vergiler] ADD [bitis_tarihi] date NULL;
END
GO
IF COL_LENGTH(N'dbo.komisyon_vergiler', N'komisyon_orani') IS NULL
BEGIN
    ALTER TABLE [dbo].[komisyon_vergiler] ADD [komisyon_orani] decimal(5,2) DEFAULT ((0)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.komisyon_vergiler', N'komisyon_gelir_vergisi_orani') IS NULL
BEGIN
    ALTER TABLE [dbo].[komisyon_vergiler] ADD [komisyon_gelir_vergisi_orani] decimal(5,2) DEFAULT ((0)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.komisyon_vergiler', N'kdv_orani') IS NULL
BEGIN
    ALTER TABLE [dbo].[komisyon_vergiler] ADD [kdv_orani] decimal(5,2) DEFAULT ((0)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.komisyon_vergiler', N'konaklama_vergisi_orani') IS NULL
BEGIN
    ALTER TABLE [dbo].[komisyon_vergiler] ADD [konaklama_vergisi_orani] decimal(5,2) DEFAULT ((0)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.komisyon_vergiler', N'para_birimi') IS NULL
BEGIN
    ALTER TABLE [dbo].[komisyon_vergiler] ADD [para_birimi] nvarchar(3) DEFAULT (N'TRY') NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.komisyon_vergiler', N'aktif_mi') IS NULL
BEGIN
    ALTER TABLE [dbo].[komisyon_vergiler] ADD [aktif_mi] bit DEFAULT ((1)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.komisyon_vergiler', N'aciklama') IS NULL
BEGIN
    ALTER TABLE [dbo].[komisyon_vergiler] ADD [aciklama] nvarchar(500) NULL;
END
GO
IF COL_LENGTH(N'dbo.komisyon_vergiler', N'olusturan_kullanici_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[komisyon_vergiler] ADD [olusturan_kullanici_id] bigint NULL;
END
GO
IF COL_LENGTH(N'dbo.komisyon_vergiler', N'guncelleyen_kullanici_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[komisyon_vergiler] ADD [guncelleyen_kullanici_id] bigint NULL;
END
GO
IF COL_LENGTH(N'dbo.komisyon_vergiler', N'olusturulma_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[komisyon_vergiler] ADD [olusturulma_tarihi] datetime2(7) DEFAULT (sysutcdatetime()) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.komisyon_vergiler', N'guncellenme_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[komisyon_vergiler] ADD [guncellenme_tarihi] datetime2(7) NULL;
END
GO
