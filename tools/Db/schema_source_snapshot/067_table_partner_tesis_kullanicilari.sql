SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.partner_tesis_kullanicilari', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[partner_tesis_kullanicilari]
    (
        [id] bigint IDENTITY(1,1) NOT NULL,
        [otel_id] bigint NOT NULL,
        [kullanici_id] bigint NOT NULL,
        [durum] nvarchar(50) NULL,
        [baslangic_tarihi] datetime2(0) NULL,
        [bitis_tarihi] datetime2(0) NULL,
        [davet_token] nvarchar(200) NULL,
        [davet_gonderim_tarihi] datetime2(0) NULL,
        [davet_son_gecerlilik] datetime2(0) NULL,
        [onay_tarihi] datetime2(0) NULL,
        [ekleyen_kullanici_id] bigint NULL,
        [iptal_eden_kullanici_id] bigint NULL,
        [iptal_tarihi] datetime2(0) NULL,
        [iptal_nedeni] nvarchar(500) NULL,
        [aktif_mi] bit CONSTRAINT [DF_partner_tesis_kullanicilari_aktif_mi] DEFAULT ((1)) NOT NULL,
        [olusturulma_tarihi] datetime2(0) CONSTRAINT [DF_partner_tesis_kullanicilari_olusturulma] DEFAULT (sysutcdatetime()) NOT NULL,
        [guncellenme_tarihi] datetime2(0) NULL,
        CONSTRAINT [PK_partner_tesis_kullanicilari] PRIMARY KEY CLUSTERED ([id] ASC)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_partner_tesis_kullanicilari_otel_aktif' AND object_id = OBJECT_ID(N'dbo.partner_tesis_kullanicilari'))
BEGIN
    CREATE INDEX [IX_partner_tesis_kullanicilari_otel_aktif]
        ON [dbo].[partner_tesis_kullanicilari] ([otel_id], [aktif_mi], [olusturulma_tarihi] DESC);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_partner_tesis_kullanicilari_token' AND object_id = OBJECT_ID(N'dbo.partner_tesis_kullanicilari'))
BEGIN
    CREATE INDEX [IX_partner_tesis_kullanicilari_token]
        ON [dbo].[partner_tesis_kullanicilari] ([davet_token])
        WHERE [davet_token] IS NOT NULL;
END
GO

