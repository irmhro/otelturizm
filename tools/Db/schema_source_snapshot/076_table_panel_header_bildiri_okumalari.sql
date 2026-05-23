SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.panel_header_bildiri_okumalari', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[panel_header_bildiri_okumalari]
    (
        [id] bigint IDENTITY(1,1) NOT NULL,
        [panel_kodu] nvarchar(24) NOT NULL,
        [kullanici_id] bigint NOT NULL,
        [bildiri_anahtari] nvarchar(190) NOT NULL,
        [okundu_mi] bit CONSTRAINT [DF__panel_hea__okund__408F9238] DEFAULT ((1)) NOT NULL,
        [okundu_tarihi] datetime2(0) NULL,
        [olusturulma_tarihi] datetime2(0) CONSTRAINT [DF__panel_hea__olust__4183B671] DEFAULT (sysutcdatetime()) NOT NULL,
        [guncellenme_tarihi] datetime2(0) CONSTRAINT [DF__panel_hea__gunce__4277DAAA] DEFAULT (sysutcdatetime()) NOT NULL,
        CONSTRAINT [PK_panel_header_bildiri_okumalari] PRIMARY KEY CLUSTERED ([id] ASC)
    );
END
GO

-- Missing column repair blocks for existing databases.
IF COL_LENGTH(N'dbo.panel_header_bildiri_okumalari', N'id') IS NULL
BEGIN
    ALTER TABLE [dbo].[panel_header_bildiri_okumalari] ADD [id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.panel_header_bildiri_okumalari', N'panel_kodu') IS NULL
BEGIN
    ALTER TABLE [dbo].[panel_header_bildiri_okumalari] ADD [panel_kodu] nvarchar(24) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.panel_header_bildiri_okumalari', N'kullanici_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[panel_header_bildiri_okumalari] ADD [kullanici_id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.panel_header_bildiri_okumalari', N'bildiri_anahtari') IS NULL
BEGIN
    ALTER TABLE [dbo].[panel_header_bildiri_okumalari] ADD [bildiri_anahtari] nvarchar(190) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.panel_header_bildiri_okumalari', N'okundu_mi') IS NULL
BEGIN
    ALTER TABLE [dbo].[panel_header_bildiri_okumalari] ADD [okundu_mi] bit DEFAULT ((1)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.panel_header_bildiri_okumalari', N'okundu_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[panel_header_bildiri_okumalari] ADD [okundu_tarihi] datetime2(0) NULL;
END
GO
IF COL_LENGTH(N'dbo.panel_header_bildiri_okumalari', N'olusturulma_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[panel_header_bildiri_okumalari] ADD [olusturulma_tarihi] datetime2(0) DEFAULT (sysutcdatetime()) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.panel_header_bildiri_okumalari', N'guncellenme_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[panel_header_bildiri_okumalari] ADD [guncellenme_tarihi] datetime2(0) DEFAULT (sysutcdatetime()) NOT NULL;
END
GO
