-- Tablo: dbo.KOMISYON_VERGILER
IF OBJECT_ID(N'dbo.KOMISYON_VERGILER', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[KOMISYON_VERGILER] (
        [ID] bigint IDENTITY(1,1) NOT NULL,
        [OTEL_ID] bigint NOT NULL,
        [BASLANGIC_TARIHI] date NOT NULL,
        [BITIS_TARIHI] date NULL,
        [KOMISYON_ORANI] decimal(5,2) NOT NULL CONSTRAINT [DF_komisyon_vergiler_komisyon_orani] DEFAULT ((0)),
        [KOMISYON_GELIR_VERGISI_ORANI] decimal(5,2) NOT NULL CONSTRAINT [DF_komisyon_vergiler_komisyon_gelir_vergisi_orani] DEFAULT ((0)),
        [KDV_ORANI] decimal(5,2) NOT NULL CONSTRAINT [DF_komisyon_vergiler_kdv_orani] DEFAULT ((0)),
        [KONAKLAMA_VERGISI_ORANI] decimal(5,2) NOT NULL CONSTRAINT [DF_komisyon_vergiler_konaklama_vergisi_orani] DEFAULT ((0)),
        [PARA_BIRIMI] nvarchar(3) NOT NULL CONSTRAINT [DF_komisyon_vergiler_para_birimi] DEFAULT (N'TRY'),
        [AKTIF_MI] bit NOT NULL CONSTRAINT [DF_komisyon_vergiler_aktif_mi] DEFAULT ((1)),
        [ACIKLAMA] nvarchar(500) NULL,
        [OLUSTURAN_KULLANICI_ID] bigint NULL,
        [GUNCELLEYEN_KULLANICI_ID] bigint NULL,
        [OLUSTURULMA_TARIHI] datetime2(7) NOT NULL CONSTRAINT [DF_komisyon_vergiler_olusturulma_tarihi] DEFAULT (sysutcdatetime()),
        [GUNCELLENME_TARIHI] datetime2(7) NULL,
        CONSTRAINT [PK_KOMISYON_VERGILER] PRIMARY KEY CLUSTERED ([ID] ASC)
    );
END
