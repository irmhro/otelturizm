SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.kullanici_ozel_teklifleri', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[kullanici_ozel_teklifleri]
    (
        [id] bigint IDENTITY(1,1) NOT NULL,
        [kullanici_id] bigint NULL,
        [teklif_tipi] nvarchar(60) NOT NULL,
        [baslik] nvarchar(180) NOT NULL,
        [aciklama] nvarchar(500) NULL,
        [kampanya_kodu] nvarchar(80) NOT NULL,
        [indirim_orani] decimal(5,2) NULL,
        [minimum_sepet_tutari] decimal(12,2) NULL,
        [gecerlilik_baslangic] date NOT NULL,
        [gecerlilik_bitis] date NOT NULL,
        [buton_url] nvarchar(255) NULL,
        [aktif_mi] bit CONSTRAINT [DF__kullanici__aktif__13F1F5EB] DEFAULT ((1)) NOT NULL,
        [olusturulma_tarihi] datetime2(0) CONSTRAINT [DF__kullanici__olust__14E61A24] DEFAULT (sysutcdatetime()) NOT NULL,
        [guncellenme_tarihi] datetime2(0) CONSTRAINT [DF__kullanici__gunce__15DA3E5D] DEFAULT (sysutcdatetime()) NOT NULL,
        CONSTRAINT [PK_kullanici_ozel_teklifleri] PRIMARY KEY CLUSTERED ([id] ASC)
    );
END
GO

-- Missing column repair blocks for existing databases.
IF COL_LENGTH(N'dbo.kullanici_ozel_teklifleri', N'id') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_ozel_teklifleri] ADD [id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.kullanici_ozel_teklifleri', N'kullanici_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_ozel_teklifleri] ADD [kullanici_id] bigint NULL;
END
GO
IF COL_LENGTH(N'dbo.kullanici_ozel_teklifleri', N'teklif_tipi') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_ozel_teklifleri] ADD [teklif_tipi] nvarchar(60) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.kullanici_ozel_teklifleri', N'baslik') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_ozel_teklifleri] ADD [baslik] nvarchar(180) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.kullanici_ozel_teklifleri', N'aciklama') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_ozel_teklifleri] ADD [aciklama] nvarchar(500) NULL;
END
GO
IF COL_LENGTH(N'dbo.kullanici_ozel_teklifleri', N'kampanya_kodu') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_ozel_teklifleri] ADD [kampanya_kodu] nvarchar(80) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.kullanici_ozel_teklifleri', N'indirim_orani') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_ozel_teklifleri] ADD [indirim_orani] decimal(5,2) NULL;
END
GO
IF COL_LENGTH(N'dbo.kullanici_ozel_teklifleri', N'minimum_sepet_tutari') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_ozel_teklifleri] ADD [minimum_sepet_tutari] decimal(12,2) NULL;
END
GO
IF COL_LENGTH(N'dbo.kullanici_ozel_teklifleri', N'gecerlilik_baslangic') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_ozel_teklifleri] ADD [gecerlilik_baslangic] date NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.kullanici_ozel_teklifleri', N'gecerlilik_bitis') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_ozel_teklifleri] ADD [gecerlilik_bitis] date NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.kullanici_ozel_teklifleri', N'buton_url') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_ozel_teklifleri] ADD [buton_url] nvarchar(255) NULL;
END
GO
IF COL_LENGTH(N'dbo.kullanici_ozel_teklifleri', N'aktif_mi') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_ozel_teklifleri] ADD [aktif_mi] bit DEFAULT ((1)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.kullanici_ozel_teklifleri', N'olusturulma_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_ozel_teklifleri] ADD [olusturulma_tarihi] datetime2(0) DEFAULT (sysutcdatetime()) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.kullanici_ozel_teklifleri', N'guncellenme_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_ozel_teklifleri] ADD [guncellenme_tarihi] datetime2(0) DEFAULT (sysutcdatetime()) NOT NULL;
END
GO
