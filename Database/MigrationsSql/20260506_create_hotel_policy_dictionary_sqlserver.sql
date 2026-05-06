IF OBJECT_ID(N'dbo.otel_kosul_sozlugu', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[otel_kosul_sozlugu]
    (
        [id] bigint IDENTITY(1,1) NOT NULL,
        [otel_tipi_id] int NULL,
        [kategori] nvarchar(80) NOT NULL,
        [kosul_adi] nvarchar(200) NOT NULL,
        [aciklama] nvarchar(500) NULL,
        [siralama] smallint NOT NULL CONSTRAINT [DF_otel_kosul_sozlugu_siralama] DEFAULT ((100)),
        [aktif_mi] bit NOT NULL CONSTRAINT [DF_otel_kosul_sozlugu_aktif] DEFAULT ((1)),
        [olusturulma_tarihi] datetime2(0) NOT NULL CONSTRAINT [DF_otel_kosul_sozlugu_olusturma] DEFAULT (sysutcdatetime()),
        [guncellenme_tarihi] datetime2(0) NULL,
        CONSTRAINT [PK_otel_kosul_sozlugu] PRIMARY KEY CLUSTERED ([id] ASC)
    );

    CREATE INDEX [IX_otel_kosul_sozlugu_aktif] ON [dbo].[otel_kosul_sozlugu] ([aktif_mi], [kategori], [siralama]);
END

IF OBJECT_ID(N'dbo.otel_kosul_secimleri', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[otel_kosul_secimleri]
    (
        [id] bigint IDENTITY(1,1) NOT NULL,
        [otel_id] bigint NOT NULL,
        [kosul_id] bigint NOT NULL,
        [olusturulma_tarihi] datetime2(0) NOT NULL CONSTRAINT [DF_otel_kosul_secimleri_olusturma] DEFAULT (sysutcdatetime()),
        CONSTRAINT [PK_otel_kosul_secimleri] PRIMARY KEY CLUSTERED ([id] ASC)
    );

    CREATE UNIQUE INDEX [UX_otel_kosul_secimleri_hotel_policy] ON [dbo].[otel_kosul_secimleri] ([otel_id], [kosul_id]);
    CREATE INDEX [IX_otel_kosul_secimleri_hotel] ON [dbo].[otel_kosul_secimleri] ([otel_id]);
END

-- Minimal seed (only if empty)
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = N'otel_kosul_sozlugu' AND schema_id = SCHEMA_ID(N'dbo'))
AND NOT EXISTS (SELECT 1 FROM dbo.otel_kosul_sozlugu)
BEGIN
    INSERT INTO dbo.otel_kosul_sozlugu (otel_tipi_id, kategori, kosul_adi, aciklama, siralama, aktif_mi)
    VALUES
    (NULL, N'Check-in / Check-out', N'Kimlik zorunludur', N'Check-in sırasında tüm misafirlerin kimlik/pasaport ibrazı zorunludur.', 10, 1),
    (NULL, N'Check-in / Check-out', N'Erken giriş müsaitliğe bağlıdır', N'Erken giriş talepleri doluluğa göre değerlendirilir.', 20, 1),
    (NULL, N'Güvenlik', N'Girişte güvenlik kontrolü yapılabilir', N'Ortak alan güvenliği için girişte kontrol uygulanabilir.', 30, 1),
    (NULL, N'Ödeme', N'Ekstra harcamalar çıkışta tahsil edilir', N'Mini bar vb. ek harcamalar check-out sırasında ödenir.', 40, 1),
    (NULL, N'Genel', N'Ortak alanlarda sessizlik hassasiyeti', N'22:00 sonrası gürültü yapılmaması rica edilir.', 50, 1);
END

