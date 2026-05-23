-- Tablo: dbo.TEMA_PANEL
IF OBJECT_ID(N'dbo.TEMA_PANEL', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[TEMA_PANEL] (
        [ID] bigint IDENTITY(1,1) NOT NULL,
        [HEDEF_TUR] nvarchar(32) NOT NULL,
        [HEDEF_ID] bigint NOT NULL,
        [BS_THEME] nvarchar(16) NOT NULL CONSTRAINT [DF_tema_panel_bs_theme] DEFAULT (N'light'),
        [PRIMARY_HEX] nvarchar(16) NULL,
        [ACCENT_HEX] nvarchar(16) NULL,
        [SIDEBAR_BG_HEX] nvarchar(16) NULL,
        [RADIUS_SCALE] decimal(5,2) NULL,
        [DENSITY] nvarchar(24) NULL,
        [FONT_FAMILY] nvarchar(160) NULL,
        [LAYOUT_MODE] nvarchar(32) NULL,
        [RTL] bit NOT NULL CONSTRAINT [DF_tema_panel_rtl] DEFAULT ((0)),
        [AKTIF_MI] bit NOT NULL,
        [OLUSTURULMA_TARIHI] datetime2(0) NOT NULL CONSTRAINT [DF_tema_panel_olusturma] DEFAULT (sysutcdatetime()),
        [GUNCELLENME_TARIHI] datetime2(0) NULL,
        CONSTRAINT [PK_TEMA_PANEL] PRIMARY KEY CLUSTERED ([ID] ASC)
    );
END
