SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.sistem_ici_bildirimler', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[sistem_ici_bildirimler]
    (
        [id] bigint IDENTITY(1,1) NOT NULL,
        [kullanici_id] bigint NOT NULL,
        [bildirim_turu] nvarchar(255) NOT NULL,
        [baslik] nvarchar(100) NOT NULL,
        [mesaj] nvarchar(max) NOT NULL,
        [ikon] nvarchar(50) NULL,
        [renk] nvarchar(20) NULL,
        [aksiyon_url] nvarchar(500) NULL,
        [aksiyon_metni] nvarchar(50) NULL,
        [okundu_mu] bit CONSTRAINT [DF__sistem_ic__okund__297722B6] DEFAULT ((0)) NULL,
        [okunma_tarihi] datetime2(0) NULL,
        [arsivlendi_mi] bit CONSTRAINT [DF__sistem_ic__arsiv__2A6B46EF] DEFAULT ((0)) NULL,
        [onem_derecesi] nvarchar(255) NULL,
        [ilgili_tablo] nvarchar(50) NULL,
        [ilgili_kayit_id] bigint NULL,
        [gecerlilik_baslangic] datetime2(0) NULL,
        [gecerlilik_bitis] datetime2(0) NULL,
        [olusturulma_tarihi] datetime2(0) CONSTRAINT [DF__sistem_ic__olust__2B5F6B28] DEFAULT (sysutcdatetime()) NULL,
        CONSTRAINT [PK_sistem_ici_bildirimler] PRIMARY KEY CLUSTERED ([id] ASC)
    );
END
GO

-- Missing column repair blocks for existing databases.
IF COL_LENGTH(N'dbo.sistem_ici_bildirimler', N'id') IS NULL
BEGIN
    ALTER TABLE [dbo].[sistem_ici_bildirimler] ADD [id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.sistem_ici_bildirimler', N'kullanici_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[sistem_ici_bildirimler] ADD [kullanici_id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.sistem_ici_bildirimler', N'bildirim_turu') IS NULL
BEGIN
    ALTER TABLE [dbo].[sistem_ici_bildirimler] ADD [bildirim_turu] nvarchar(255) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.sistem_ici_bildirimler', N'baslik') IS NULL
BEGIN
    ALTER TABLE [dbo].[sistem_ici_bildirimler] ADD [baslik] nvarchar(100) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.sistem_ici_bildirimler', N'mesaj') IS NULL
BEGIN
    ALTER TABLE [dbo].[sistem_ici_bildirimler] ADD [mesaj] nvarchar(max) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.sistem_ici_bildirimler', N'ikon') IS NULL
BEGIN
    ALTER TABLE [dbo].[sistem_ici_bildirimler] ADD [ikon] nvarchar(50) NULL;
END
GO
IF COL_LENGTH(N'dbo.sistem_ici_bildirimler', N'renk') IS NULL
BEGIN
    ALTER TABLE [dbo].[sistem_ici_bildirimler] ADD [renk] nvarchar(20) NULL;
END
GO
IF COL_LENGTH(N'dbo.sistem_ici_bildirimler', N'aksiyon_url') IS NULL
BEGIN
    ALTER TABLE [dbo].[sistem_ici_bildirimler] ADD [aksiyon_url] nvarchar(500) NULL;
END
GO
IF COL_LENGTH(N'dbo.sistem_ici_bildirimler', N'aksiyon_metni') IS NULL
BEGIN
    ALTER TABLE [dbo].[sistem_ici_bildirimler] ADD [aksiyon_metni] nvarchar(50) NULL;
END
GO
IF COL_LENGTH(N'dbo.sistem_ici_bildirimler', N'okundu_mu') IS NULL
BEGIN
    ALTER TABLE [dbo].[sistem_ici_bildirimler] ADD [okundu_mu] bit DEFAULT ((0)) NULL;
END
GO
IF COL_LENGTH(N'dbo.sistem_ici_bildirimler', N'okunma_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[sistem_ici_bildirimler] ADD [okunma_tarihi] datetime2(0) NULL;
END
GO
IF COL_LENGTH(N'dbo.sistem_ici_bildirimler', N'arsivlendi_mi') IS NULL
BEGIN
    ALTER TABLE [dbo].[sistem_ici_bildirimler] ADD [arsivlendi_mi] bit DEFAULT ((0)) NULL;
END
GO
IF COL_LENGTH(N'dbo.sistem_ici_bildirimler', N'onem_derecesi') IS NULL
BEGIN
    ALTER TABLE [dbo].[sistem_ici_bildirimler] ADD [onem_derecesi] nvarchar(255) NULL;
END
GO
IF COL_LENGTH(N'dbo.sistem_ici_bildirimler', N'ilgili_tablo') IS NULL
BEGIN
    ALTER TABLE [dbo].[sistem_ici_bildirimler] ADD [ilgili_tablo] nvarchar(50) NULL;
END
GO
IF COL_LENGTH(N'dbo.sistem_ici_bildirimler', N'ilgili_kayit_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[sistem_ici_bildirimler] ADD [ilgili_kayit_id] bigint NULL;
END
GO
IF COL_LENGTH(N'dbo.sistem_ici_bildirimler', N'gecerlilik_baslangic') IS NULL
BEGIN
    ALTER TABLE [dbo].[sistem_ici_bildirimler] ADD [gecerlilik_baslangic] datetime2(0) NULL;
END
GO
IF COL_LENGTH(N'dbo.sistem_ici_bildirimler', N'gecerlilik_bitis') IS NULL
BEGIN
    ALTER TABLE [dbo].[sistem_ici_bildirimler] ADD [gecerlilik_bitis] datetime2(0) NULL;
END
GO
IF COL_LENGTH(N'dbo.sistem_ici_bildirimler', N'olusturulma_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[sistem_ici_bildirimler] ADD [olusturulma_tarihi] datetime2(0) DEFAULT (sysutcdatetime()) NULL;
END
GO
