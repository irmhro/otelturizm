SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.kullanici_oturum_istatistikleri', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[kullanici_oturum_istatistikleri]
    (
        [id] bigint IDENTITY(1,1) NOT NULL,
        [kullanici_id] bigint NOT NULL,
        [hesap_tipi] nvarchar(255) NOT NULL,
        [partner_id] bigint NULL,
        [cihaz_anahtari] nvarchar(128) NOT NULL,
        [cihaz_etiketi] nvarchar(150) NULL,
        [beni_hatirla_tercihi] bit CONSTRAINT [DF__kullanici__beni___0E391C95] DEFAULT ((0)) NULL,
        [toplam_ziyaret_sayisi] int CONSTRAINT [DF__kullanici__topla__0F2D40CE] DEFAULT ((0)) NULL,
        [toplam_oturum_suresi_saniye] bigint CONSTRAINT [DF__kullanici__topla__10216507] DEFAULT ((0)) NULL,
        [son_oturum_baslangici] datetime2(0) NULL,
        [son_oturum_bitisi] datetime2(0) NULL,
        [son_aktivite_tarihi] datetime2(0) NULL,
        [son_ip_hash] nchar(64) NULL,
        [son_user_agent_hash] nchar(64) NULL,
        [olusturulma_tarihi] datetime2(0) CONSTRAINT [DF__kullanici__olust__11158940] DEFAULT (sysutcdatetime()) NULL,
        [guncellenme_tarihi] datetime2(0) NULL,
        CONSTRAINT [PK_kullanici_oturum_istatistikleri] PRIMARY KEY CLUSTERED ([id] ASC)
    );
END
GO

-- Missing column repair blocks for existing databases.
IF COL_LENGTH(N'dbo.kullanici_oturum_istatistikleri', N'id') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_oturum_istatistikleri] ADD [id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.kullanici_oturum_istatistikleri', N'kullanici_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_oturum_istatistikleri] ADD [kullanici_id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.kullanici_oturum_istatistikleri', N'hesap_tipi') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_oturum_istatistikleri] ADD [hesap_tipi] nvarchar(255) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.kullanici_oturum_istatistikleri', N'partner_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_oturum_istatistikleri] ADD [partner_id] bigint NULL;
END
GO
IF COL_LENGTH(N'dbo.kullanici_oturum_istatistikleri', N'cihaz_anahtari') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_oturum_istatistikleri] ADD [cihaz_anahtari] nvarchar(128) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.kullanici_oturum_istatistikleri', N'cihaz_etiketi') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_oturum_istatistikleri] ADD [cihaz_etiketi] nvarchar(150) NULL;
END
GO
IF COL_LENGTH(N'dbo.kullanici_oturum_istatistikleri', N'beni_hatirla_tercihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_oturum_istatistikleri] ADD [beni_hatirla_tercihi] bit DEFAULT ((0)) NULL;
END
GO
IF COL_LENGTH(N'dbo.kullanici_oturum_istatistikleri', N'toplam_ziyaret_sayisi') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_oturum_istatistikleri] ADD [toplam_ziyaret_sayisi] int DEFAULT ((0)) NULL;
END
GO
IF COL_LENGTH(N'dbo.kullanici_oturum_istatistikleri', N'toplam_oturum_suresi_saniye') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_oturum_istatistikleri] ADD [toplam_oturum_suresi_saniye] bigint DEFAULT ((0)) NULL;
END
GO
IF COL_LENGTH(N'dbo.kullanici_oturum_istatistikleri', N'son_oturum_baslangici') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_oturum_istatistikleri] ADD [son_oturum_baslangici] datetime2(0) NULL;
END
GO
IF COL_LENGTH(N'dbo.kullanici_oturum_istatistikleri', N'son_oturum_bitisi') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_oturum_istatistikleri] ADD [son_oturum_bitisi] datetime2(0) NULL;
END
GO
IF COL_LENGTH(N'dbo.kullanici_oturum_istatistikleri', N'son_aktivite_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_oturum_istatistikleri] ADD [son_aktivite_tarihi] datetime2(0) NULL;
END
GO
IF COL_LENGTH(N'dbo.kullanici_oturum_istatistikleri', N'son_ip_hash') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_oturum_istatistikleri] ADD [son_ip_hash] nchar(64) NULL;
END
GO
IF COL_LENGTH(N'dbo.kullanici_oturum_istatistikleri', N'son_user_agent_hash') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_oturum_istatistikleri] ADD [son_user_agent_hash] nchar(64) NULL;
END
GO
IF COL_LENGTH(N'dbo.kullanici_oturum_istatistikleri', N'olusturulma_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_oturum_istatistikleri] ADD [olusturulma_tarihi] datetime2(0) DEFAULT (sysutcdatetime()) NULL;
END
GO
IF COL_LENGTH(N'dbo.kullanici_oturum_istatistikleri', N'guncellenme_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_oturum_istatistikleri] ADD [guncellenme_tarihi] datetime2(0) NULL;
END
GO
