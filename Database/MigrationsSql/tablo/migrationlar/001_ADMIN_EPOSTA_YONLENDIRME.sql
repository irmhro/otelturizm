-- Tablo: dbo.ADMIN_EPOSTA_YONLENDIRME
IF OBJECT_ID(N'dbo.ADMIN_EPOSTA_YONLENDIRME', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ADMIN_EPOSTA_YONLENDIRME] (
        [ID] bigint IDENTITY(1,1) NOT NULL,
        [OLAY_KODU] nvarchar(64) NOT NULL,
        [BASLIK] nvarchar(200) NOT NULL,
        [ACIKLAMA] nvarchar(500) NULL,
        [HEDEF_EPOSTALAR] nvarchar(2000) NOT NULL CONSTRAINT [DF_admin_eposta_hedef] DEFAULT (N''),
        [AKTIF_MI] bit NOT NULL CONSTRAINT [DF_admin_eposta_aktif] DEFAULT ((1)),
        [GUNCELLENME_UTC] datetime2(0) NOT NULL CONSTRAINT [DF_admin_eposta_guncelle] DEFAULT (sysutcdatetime()),
        CONSTRAINT [PK_ADMIN_EPOSTA_YONLENDIRME] PRIMARY KEY CLUSTERED ([ID] ASC)
    );
END
