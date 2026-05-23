-- Platform paket satışı: 5651/5661 ve partner başvuru akışı (idempotent)

IF OBJECT_ID(N'dbo.PLATFORM_PAKET_KATEGORILERI', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[PLATFORM_PAKET_KATEGORILERI] (
        [ID] bigint IDENTITY(1,1) NOT NULL,
        [KOD] nvarchar(32) NOT NULL,
        [BASLIK] nvarchar(120) NOT NULL,
        [ACIKLAMA] nvarchar(500) NULL,
        [SIRA] int NOT NULL CONSTRAINT [DF_platform_paket_kategori_sira] DEFAULT (0),
        [AKTIF_MI] bit NOT NULL CONSTRAINT [DF_platform_paket_kategori_aktif] DEFAULT (1),
        [OLUSTURULMA_UTC] datetime2(7) NOT NULL CONSTRAINT [DF_platform_paket_kategori_olustur] DEFAULT (sysutcdatetime()),
        CONSTRAINT [PK_PLATFORM_PAKET_KATEGORILERI] PRIMARY KEY CLUSTERED ([ID] ASC),
        CONSTRAINT [UQ_PLATFORM_PAKET_KATEGORILERI_KOD] UNIQUE ([KOD])
    );
END

IF OBJECT_ID(N'dbo.PLATFORM_PAKETLER', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[PLATFORM_PAKETLER] (
        [ID] bigint IDENTITY(1,1) NOT NULL,
        [KATEGORI_ID] bigint NOT NULL,
        [PAKET_KODU] nvarchar(64) NOT NULL,
        [BASLIK] nvarchar(200) NOT NULL,
        [KISA_ACIKLAMA] nvarchar(500) NULL,
        [DETAY_METIN] nvarchar(max) NULL,
        [FIYAT_TUTAR] decimal(18,2) NOT NULL CONSTRAINT [DF_platform_paket_fiyat] DEFAULT (0),
        [PARA_BIRIMI] nvarchar(8) NOT NULL CONSTRAINT [DF_platform_paket_para] DEFAULT (N'TRY'),
        [FATURA_PERIYODU] nvarchar(24) NOT NULL CONSTRAINT [DF_platform_paket_periyot] DEFAULT (N'Aylik'),
        [PLATFORM_KOMISYON_ORANI] decimal(5,2) NULL,
        [HEDEF_KURAL] nvarchar(32) NOT NULL CONSTRAINT [DF_platform_paket_hedef] DEFAULT (N'HER_OTEL'),
        [KAPAK_GORSEL_URL] nvarchar(500) NULL,
        [GALERI_JSON] nvarchar(max) NULL,
        [OZELLIKLER_JSON] nvarchar(max) NULL,
        [SOZLESME_URL] nvarchar(500) NULL,
        [DURUM] nvarchar(20) NOT NULL CONSTRAINT [DF_platform_paket_durum] DEFAULT (N'Yayinda'),
        [SIRA] int NOT NULL CONSTRAINT [DF_platform_paket_sira] DEFAULT (0),
        [OLUSTURULMA_UTC] datetime2(7) NOT NULL CONSTRAINT [DF_platform_paket_olustur] DEFAULT (sysutcdatetime()),
        [GUNCELLEME_UTC] datetime2(7) NULL,
        CONSTRAINT [PK_PLATFORM_PAKETLER] PRIMARY KEY CLUSTERED ([ID] ASC),
        CONSTRAINT [UQ_PLATFORM_PAKETLER_KOD] UNIQUE ([PAKET_KODU]),
        CONSTRAINT [FK_PLATFORM_PAKETLER_KATEGORI] FOREIGN KEY ([KATEGORI_ID]) REFERENCES [dbo].[PLATFORM_PAKET_KATEGORILERI]([ID])
    );
END

IF OBJECT_ID(N'dbo.OTEL_UYUM_DURUMLARI', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[OTEL_UYUM_DURUMLARI] (
        [OTEL_ID] bigint NOT NULL,
        [LOG_5651_KURULU] bit NOT NULL CONSTRAINT [DF_otel_uyum_5651] DEFAULT (0),
        [LOG_5661_KURULU] bit NOT NULL CONSTRAINT [DF_otel_uyum_5661] DEFAULT (0),
        [NOT_METNI] nvarchar(500) NULL,
        [GUNCELLEYEN_TIP] nvarchar(24) NULL,
        [GUNCELLEME_UTC] datetime2(7) NOT NULL CONSTRAINT [DF_otel_uyum_guncelleme] DEFAULT (sysutcdatetime()),
        CONSTRAINT [PK_OTEL_UYUM_DURUMLARI] PRIMARY KEY CLUSTERED ([OTEL_ID] ASC)
    );
END

IF OBJECT_ID(N'dbo.PARTNER_PAKET_BASVURULARI', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[PARTNER_PAKET_BASVURULARI] (
        [ID] bigint IDENTITY(1,1) NOT NULL,
        [PARTNER_KULLANICI_ID] bigint NOT NULL,
        [OTEL_ID] bigint NOT NULL,
        [PAKET_ID] bigint NOT NULL,
        [DURUM] nvarchar(24) NOT NULL CONSTRAINT [DF_partner_paket_basvuru_durum] DEFAULT (N'Beklemede'),
        [OTEL_5661_KURULU_BEYAN] bit NOT NULL CONSTRAINT [DF_partner_paket_5661_beyan] DEFAULT (0),
        [ILETISIM_AD] nvarchar(120) NULL,
        [ILETISIM_TELEFON] nvarchar(32) NULL,
        [ILETISIM_EPOSTA] nvarchar(160) NULL,
        [PARTNER_NOTU] nvarchar(1000) NULL,
        [ADMIN_NOTU] nvarchar(1000) NULL,
        [TEKLIF_TUTAR] decimal(18,2) NOT NULL,
        [TEKLIF_PARA_BIRIMI] nvarchar(8) NOT NULL CONSTRAINT [DF_partner_paket_teklif_para] DEFAULT (N'TRY'),
        [ONAYLAYAN_ADMIN_KULLANICI_ID] bigint NULL,
        [OLUSTURULMA_UTC] datetime2(7) NOT NULL CONSTRAINT [DF_partner_paket_basvuru_olustur] DEFAULT (sysutcdatetime()),
        [ONAY_TARIHI_UTC] datetime2(7) NULL,
        [AKTIF_BASLANGIC_UTC] datetime2(7) NULL,
        [AKTIF_BITIS_UTC] datetime2(7) NULL,
        CONSTRAINT [PK_PARTNER_PAKET_BASVURULARI] PRIMARY KEY CLUSTERED ([ID] ASC),
        CONSTRAINT [FK_PARTNER_PAKET_BASVURU_PAKET] FOREIGN KEY ([PAKET_ID]) REFERENCES [dbo].[PLATFORM_PAKETLER]([ID])
    );
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.PARTNER_PAKET_BASVURULARI') AND name = N'IX_partner_paket_basvuru_otel')
BEGIN
    CREATE INDEX [IX_partner_paket_basvuru_otel] ON [dbo].[PARTNER_PAKET_BASVURULARI] ([OTEL_ID] ASC, [DURUM] ASC, [OLUSTURULMA_UTC] DESC);
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.PARTNER_PAKET_BASVURULARI') AND name = N'IX_partner_paket_basvuru_partner')
BEGIN
    CREATE INDEX [IX_partner_paket_basvuru_partner] ON [dbo].[PARTNER_PAKET_BASVURULARI] ([PARTNER_KULLANICI_ID] ASC, [OLUSTURULMA_UTC] DESC);
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.PLATFORM_PAKETLER') AND name = N'IX_platform_paketler_kategori')
BEGIN
    CREATE INDEX [IX_platform_paketler_kategori] ON [dbo].[PLATFORM_PAKETLER] ([KATEGORI_ID] ASC, [DURUM] ASC, [SIRA] ASC);
END
