-- Tablo: dbo.GUVENLI_DOSYA_VARLIKLARI
IF OBJECT_ID(N'dbo.GUVENLI_DOSYA_VARLIKLARI', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[GUVENLI_DOSYA_VARLIKLARI] (
        [ID] bigint IDENTITY(1,1) NOT NULL,
        [BAGLAM_TABLO] nvarchar(100) NOT NULL,
        [BAGLAM_KAYIT_ID] bigint NOT NULL,
        [SAHIBI_KULLANICI_ID] bigint NULL,
        [SAHIBI_FIRMA_ID] bigint NULL,
        [KATEGORI] nvarchar(50) NOT NULL,
        [GORUNURLUK_KAPSAMI] nvarchar(255) NOT NULL,
        [ORIJINAL_DOSYA_ADI] nvarchar(255) NOT NULL,
        [DEPOLANAN_DOSYA_ADI] nvarchar(255) NOT NULL,
        [DEPOLAMA_YOLU] nvarchar(500) NOT NULL,
        [MIME_TIPI] nvarchar(150) NOT NULL,
        [DOSYA_UZANTISI] nvarchar(20) NULL,
        [DOSYA_BOYUTU] bigint NOT NULL CONSTRAINT [DF__guvenli_d__dosya__4B7734FF] DEFAULT ((0)),
        [SHA256_OZETI] nchar(64) NULL,
        [GORSEL_MI] bit NOT NULL CONSTRAINT [DF__guvenli_d__gorse__4C6B5938] DEFAULT ((0)),
        [AKTIF_MI] bit NOT NULL CONSTRAINT [DF__guvenli_d__aktif__4D5F7D71] DEFAULT ((1)),
        [SILINME_TARIHI] datetime2(0) NULL,
        [OLUSTURULMA_TARIHI] datetime2(0) NOT NULL CONSTRAINT [DF__guvenli_d__olust__4E53A1AA] DEFAULT (sysutcdatetime()),
        CONSTRAINT [PK_GUVENLI_DOSYA_VARLIKLARI] PRIMARY KEY CLUSTERED ([ID] ASC)
    );
END
