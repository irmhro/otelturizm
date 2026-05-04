SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.partner_destek_mesajlari', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[partner_destek_mesajlari]
    (
        [id] bigint IDENTITY(1,1) NOT NULL,
        [talep_id] bigint NOT NULL,
        [gonderen_kullanici_id] bigint NULL,
        [gonderen_tipi] nvarchar(255) NOT NULL,
        [mesaj] nvarchar(max) NOT NULL,
        [ek_dosya_yolu] nvarchar(255) NULL,
        [okundu_mu] bit CONSTRAINT [DF__partner_d__okund__4B0D20AB] DEFAULT ((0)) NULL,
        [olusturulma_tarihi] datetime2(0) CONSTRAINT [DF__partner_d__olust__4C0144E4] DEFAULT (sysutcdatetime()) NULL,
        CONSTRAINT [PK_partner_destek_mesajlari] PRIMARY KEY CLUSTERED ([id] ASC)
    );
END
GO

-- Missing column repair blocks for existing databases.
IF COL_LENGTH(N'dbo.partner_destek_mesajlari', N'id') IS NULL
BEGIN
    ALTER TABLE [dbo].[partner_destek_mesajlari] ADD [id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.partner_destek_mesajlari', N'talep_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[partner_destek_mesajlari] ADD [talep_id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.partner_destek_mesajlari', N'gonderen_kullanici_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[partner_destek_mesajlari] ADD [gonderen_kullanici_id] bigint NULL;
END
GO
IF COL_LENGTH(N'dbo.partner_destek_mesajlari', N'gonderen_tipi') IS NULL
BEGIN
    ALTER TABLE [dbo].[partner_destek_mesajlari] ADD [gonderen_tipi] nvarchar(255) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.partner_destek_mesajlari', N'mesaj') IS NULL
BEGIN
    ALTER TABLE [dbo].[partner_destek_mesajlari] ADD [mesaj] nvarchar(max) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.partner_destek_mesajlari', N'ek_dosya_yolu') IS NULL
BEGIN
    ALTER TABLE [dbo].[partner_destek_mesajlari] ADD [ek_dosya_yolu] nvarchar(255) NULL;
END
GO
IF COL_LENGTH(N'dbo.partner_destek_mesajlari', N'okundu_mu') IS NULL
BEGIN
    ALTER TABLE [dbo].[partner_destek_mesajlari] ADD [okundu_mu] bit DEFAULT ((0)) NULL;
END
GO
IF COL_LENGTH(N'dbo.partner_destek_mesajlari', N'olusturulma_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[partner_destek_mesajlari] ADD [olusturulma_tarihi] datetime2(0) DEFAULT (sysutcdatetime()) NULL;
END
GO
