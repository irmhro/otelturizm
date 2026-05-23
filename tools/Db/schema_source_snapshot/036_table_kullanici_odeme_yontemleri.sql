SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.kullanici_odeme_yontemleri', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[kullanici_odeme_yontemleri]
    (
        [id] bigint IDENTITY(1,1) NOT NULL,
        [kullanici_id] bigint NOT NULL,
        [kart_etiketi] nvarchar(100) NOT NULL,
        [kart_sahibi] nvarchar(100) NOT NULL,
        [marka] nvarchar(30) NOT NULL,
        [son_dort_hane] nchar(4) NOT NULL,
        [son_kullanim_ay] tinyint NOT NULL,
        [son_kullanim_yil] smallint NOT NULL,
        [varsayilan_mi] bit CONSTRAINT [DF__kullanici__varsa__09746778] DEFAULT ((0)) NOT NULL,
        [aktif_mi] bit CONSTRAINT [DF__kullanici__aktif__0A688BB1] DEFAULT ((1)) NOT NULL,
        [olusturulma_tarihi] datetime2(0) CONSTRAINT [DF__kullanici__olust__0B5CAFEA] DEFAULT (sysutcdatetime()) NULL,
        [guncellenme_tarihi] datetime2(0) NULL,
        CONSTRAINT [PK_kullanici_odeme_yontemleri] PRIMARY KEY CLUSTERED ([id] ASC)
    );
END
GO

-- Missing column repair blocks for existing databases.
IF COL_LENGTH(N'dbo.kullanici_odeme_yontemleri', N'id') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_odeme_yontemleri] ADD [id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.kullanici_odeme_yontemleri', N'kullanici_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_odeme_yontemleri] ADD [kullanici_id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.kullanici_odeme_yontemleri', N'kart_etiketi') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_odeme_yontemleri] ADD [kart_etiketi] nvarchar(100) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.kullanici_odeme_yontemleri', N'kart_sahibi') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_odeme_yontemleri] ADD [kart_sahibi] nvarchar(100) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.kullanici_odeme_yontemleri', N'marka') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_odeme_yontemleri] ADD [marka] nvarchar(30) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.kullanici_odeme_yontemleri', N'son_dort_hane') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_odeme_yontemleri] ADD [son_dort_hane] nchar(4) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.kullanici_odeme_yontemleri', N'son_kullanim_ay') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_odeme_yontemleri] ADD [son_kullanim_ay] tinyint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.kullanici_odeme_yontemleri', N'son_kullanim_yil') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_odeme_yontemleri] ADD [son_kullanim_yil] smallint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.kullanici_odeme_yontemleri', N'varsayilan_mi') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_odeme_yontemleri] ADD [varsayilan_mi] bit DEFAULT ((0)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.kullanici_odeme_yontemleri', N'aktif_mi') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_odeme_yontemleri] ADD [aktif_mi] bit DEFAULT ((1)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.kullanici_odeme_yontemleri', N'olusturulma_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_odeme_yontemleri] ADD [olusturulma_tarihi] datetime2(0) DEFAULT (sysutcdatetime()) NULL;
END
GO
IF COL_LENGTH(N'dbo.kullanici_odeme_yontemleri', N'guncellenme_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_odeme_yontemleri] ADD [guncellenme_tarihi] datetime2(0) NULL;
END
GO
