-- Tablo: dbo.REZERVASYON_DURUM_TANIMLARI
IF OBJECT_ID(N'dbo.REZERVASYON_DURUM_TANIMLARI', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[REZERVASYON_DURUM_TANIMLARI]
    (
        [ID] bigint IDENTITY(1,1) NOT NULL,
        [KOD] nvarchar(40) NOT NULL,
        [AD] nvarchar(80) NOT NULL,
        [ACIKLAMA] nvarchar(500) NULL,
        [SIRA_NO] int CONSTRAINT [DF_rezervasyon_durum_tanimlari_sira] DEFAULT ((0)) NOT NULL,
        [AKTIF_MI] bit CONSTRAINT [DF_rezervasyon_durum_tanimlari_aktif] DEFAULT ((1)) NOT NULL,
        [SISTEM_SATIR_MI] bit CONSTRAINT [DF_rezervasyon_durum_tanimlari_sistem] DEFAULT ((1)) NOT NULL,
        [IPTAL_MI] bit CONSTRAINT [DF_rezervasyon_durum_tanimlari_iptal] DEFAULT ((0)) NOT NULL,
        [TAMAMLANDI_MI] bit CONSTRAINT [DF_rezervasyon_durum_tanimlari_tamamlandi] DEFAULT ((0)) NOT NULL,
        [BEKLEYEN_MI] bit CONSTRAINT [DF_rezervasyon_durum_tanimlari_bekleyen] DEFAULT ((0)) NOT NULL,
        [GELIR_SAYILIR_MI] bit CONSTRAINT [DF_rezervasyon_durum_tanimlari_gelir] DEFAULT ((0)) NOT NULL,
        [OZELLIK_JSON_SABLONU] nvarchar(max) NULL,
        [OLUSTURULMA_TARIHI] datetime2(7) CONSTRAINT [DF_rezervasyon_durum_tanimlari_olustur] DEFAULT (sysutcdatetime()) NOT NULL,
        CONSTRAINT [PK_REZERVASYON_DURUM_TANIMLARI] PRIMARY KEY CLUSTERED ([ID] ASC)
    );
END
