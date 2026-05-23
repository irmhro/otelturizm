SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.mesaj_sablonlari', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[mesaj_sablonlari]
    (
        [id] int IDENTITY(1,1) NOT NULL,
        [sablon_kodu] nvarchar(30) NOT NULL,
        [sablon_adi] nvarchar(100) NOT NULL,
        [otel_id] bigint NULL,
        [sistem_geneli_mi] bit CONSTRAINT [DF__mesaj_sab__siste__41B8C09B] DEFAULT ((0)) NULL,
        [kategori] nvarchar(255) NOT NULL,
        [konu_basligi] nvarchar(200) NOT NULL,
        [mesaj_icerigi] nvarchar(max) NOT NULL,
        [kullanilabilir_degiskenler] nvarchar(max) NULL,
        [dil] nvarchar(5) NULL,
        [aktif_mi] bit CONSTRAINT [DF__mesaj_sab__aktif__42ACE4D4] DEFAULT ((1)) NULL,
        [olusturulma_tarihi] datetime2(0) CONSTRAINT [DF__mesaj_sab__olust__43A1090D] DEFAULT (sysutcdatetime()) NULL,
        CONSTRAINT [PK_mesaj_sablonlari] PRIMARY KEY CLUSTERED ([id] ASC)
    );
END
GO

-- Missing column repair blocks for existing databases.
IF COL_LENGTH(N'dbo.mesaj_sablonlari', N'id') IS NULL
BEGIN
    ALTER TABLE [dbo].[mesaj_sablonlari] ADD [id] int NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.mesaj_sablonlari', N'sablon_kodu') IS NULL
BEGIN
    ALTER TABLE [dbo].[mesaj_sablonlari] ADD [sablon_kodu] nvarchar(30) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.mesaj_sablonlari', N'sablon_adi') IS NULL
BEGIN
    ALTER TABLE [dbo].[mesaj_sablonlari] ADD [sablon_adi] nvarchar(100) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.mesaj_sablonlari', N'otel_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[mesaj_sablonlari] ADD [otel_id] bigint NULL;
END
GO
IF COL_LENGTH(N'dbo.mesaj_sablonlari', N'sistem_geneli_mi') IS NULL
BEGIN
    ALTER TABLE [dbo].[mesaj_sablonlari] ADD [sistem_geneli_mi] bit DEFAULT ((0)) NULL;
END
GO
IF COL_LENGTH(N'dbo.mesaj_sablonlari', N'kategori') IS NULL
BEGIN
    ALTER TABLE [dbo].[mesaj_sablonlari] ADD [kategori] nvarchar(255) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.mesaj_sablonlari', N'konu_basligi') IS NULL
BEGIN
    ALTER TABLE [dbo].[mesaj_sablonlari] ADD [konu_basligi] nvarchar(200) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.mesaj_sablonlari', N'mesaj_icerigi') IS NULL
BEGIN
    ALTER TABLE [dbo].[mesaj_sablonlari] ADD [mesaj_icerigi] nvarchar(max) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.mesaj_sablonlari', N'kullanilabilir_degiskenler') IS NULL
BEGIN
    ALTER TABLE [dbo].[mesaj_sablonlari] ADD [kullanilabilir_degiskenler] nvarchar(max) NULL;
END
GO
IF COL_LENGTH(N'dbo.mesaj_sablonlari', N'dil') IS NULL
BEGIN
    ALTER TABLE [dbo].[mesaj_sablonlari] ADD [dil] nvarchar(5) NULL;
END
GO
IF COL_LENGTH(N'dbo.mesaj_sablonlari', N'aktif_mi') IS NULL
BEGIN
    ALTER TABLE [dbo].[mesaj_sablonlari] ADD [aktif_mi] bit DEFAULT ((1)) NULL;
END
GO
IF COL_LENGTH(N'dbo.mesaj_sablonlari', N'olusturulma_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[mesaj_sablonlari] ADD [olusturulma_tarihi] datetime2(0) DEFAULT (sysutcdatetime()) NULL;
END
GO
