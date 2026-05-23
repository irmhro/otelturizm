SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.rezervasyon_odeme_kalemleri', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[rezervasyon_odeme_kalemleri]
    (
        [id] bigint IDENTITY(1,1) NOT NULL,
        [rezervasyon_id] bigint NOT NULL,
        [odeme_yontemi_id] bigint NOT NULL,
        [odeme_durumu_id] bigint NOT NULL,
        [tutar] decimal(18,2) CONSTRAINT [DF_rezervasyon_odeme_kalem_tutar] DEFAULT ((0)) NOT NULL,
        [tahsil_edilen_tutar] decimal(18,2) NULL,
        [sira_no] int CONSTRAINT [DF_rezervasyon_odeme_kalem_sira] DEFAULT ((1)) NOT NULL,
        [havale_eft_referans] nvarchar(120) NULL,
        [dekont_guvenli_dosya_id] bigint NULL,
        [aciklama] nvarchar(500) NULL,
        [olusturulma_tarihi] datetime2(7) CONSTRAINT [DF_rezervasyon_odeme_kalem_olustur] DEFAULT (sysutcdatetime()) NOT NULL,
        CONSTRAINT [PK__rezervas__3213E83FDA382B1A] PRIMARY KEY CLUSTERED ([id] ASC)
    );
END
GO

-- Missing column repair blocks for existing databases.
IF COL_LENGTH(N'dbo.rezervasyon_odeme_kalemleri', N'id') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyon_odeme_kalemleri] ADD [id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.rezervasyon_odeme_kalemleri', N'rezervasyon_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyon_odeme_kalemleri] ADD [rezervasyon_id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.rezervasyon_odeme_kalemleri', N'odeme_yontemi_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyon_odeme_kalemleri] ADD [odeme_yontemi_id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.rezervasyon_odeme_kalemleri', N'odeme_durumu_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyon_odeme_kalemleri] ADD [odeme_durumu_id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.rezervasyon_odeme_kalemleri', N'tutar') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyon_odeme_kalemleri] ADD [tutar] decimal(18,2) DEFAULT ((0)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.rezervasyon_odeme_kalemleri', N'tahsil_edilen_tutar') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyon_odeme_kalemleri] ADD [tahsil_edilen_tutar] decimal(18,2) NULL;
END
GO
IF COL_LENGTH(N'dbo.rezervasyon_odeme_kalemleri', N'sira_no') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyon_odeme_kalemleri] ADD [sira_no] int DEFAULT ((1)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.rezervasyon_odeme_kalemleri', N'havale_eft_referans') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyon_odeme_kalemleri] ADD [havale_eft_referans] nvarchar(120) NULL;
END
GO
IF COL_LENGTH(N'dbo.rezervasyon_odeme_kalemleri', N'dekont_guvenli_dosya_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyon_odeme_kalemleri] ADD [dekont_guvenli_dosya_id] bigint NULL;
END
GO
IF COL_LENGTH(N'dbo.rezervasyon_odeme_kalemleri', N'aciklama') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyon_odeme_kalemleri] ADD [aciklama] nvarchar(500) NULL;
END
GO
IF COL_LENGTH(N'dbo.rezervasyon_odeme_kalemleri', N'olusturulma_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[rezervasyon_odeme_kalemleri] ADD [olusturulma_tarihi] datetime2(7) DEFAULT (sysutcdatetime()) NOT NULL;
END
GO
