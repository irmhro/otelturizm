SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.tema_panel', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tema_panel
    (
        id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_tema_panel PRIMARY KEY,
        hedef_tur NVARCHAR(32) NOT NULL,
        hedef_id BIGINT NOT NULL,
        bs_theme NVARCHAR(16) NOT NULL CONSTRAINT DF_tema_panel_bs_theme DEFAULT N'light',
        primary_hex NVARCHAR(16) NULL,
        accent_hex NVARCHAR(16) NULL,
        sidebar_bg_hex NVARCHAR(16) NULL,
        radius_scale DECIMAL(5,2) NULL,
        density NVARCHAR(24) NULL,
        font_family NVARCHAR(160) NULL,
        layout_mode NVARCHAR(32) NULL,
        rtl BIT NOT NULL CONSTRAINT DF_tema_panel_rtl DEFAULT 0,
        aktif_mi BIT NOT NULL CONSTRAINT DF_tema_panel_aktif DEFAULT 1,
        olusturulma_tarihi DATETIME2(0) NOT NULL CONSTRAINT DF_tema_panel_olusturma DEFAULT SYSUTCDATETIME(),
        guncellenme_tarihi DATETIME2(0) NULL
    );

END;
GO

IF COL_LENGTH('dbo.tema_panel', 'density') IS NULL
    ALTER TABLE dbo.tema_panel ADD density NVARCHAR(24) NULL;
GO

IF COL_LENGTH('dbo.tema_panel', 'layout_mode') IS NULL
    ALTER TABLE dbo.tema_panel ADD layout_mode NVARCHAR(32) NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_tema_panel_hedef_aktif' AND object_id = OBJECT_ID(N'dbo.tema_panel'))
    CREATE UNIQUE INDEX UX_tema_panel_hedef_aktif
        ON dbo.tema_panel(hedef_tur, hedef_id)
        WHERE aktif_mi = 1;
GO
