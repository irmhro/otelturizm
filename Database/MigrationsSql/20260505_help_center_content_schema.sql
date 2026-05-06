/*
  Yardım Merkezi içerik şeması (Airbnb kalite bilgi merkezi hedefi)
  - Kategori detayları: hero görsel, başlık/altbaşlık, tam açıklama (HTML/Markdown)
  - Kategori SSS: soru/cevap
  - İçerik sayfaları: Hakkımızda, Kariyer, Basın Odası, Blog (tek tablo)
*/

IF OBJECT_ID(N'dbo.yardim_merkezi_kategori_detaylari', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.yardim_merkezi_kategori_detaylari
    (
        id BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        destek_kategori_id BIGINT NOT NULL,
        hero_baslik NVARCHAR(160) NULL,
        hero_alt_baslik NVARCHAR(260) NULL,
        hero_gorsel_url NVARCHAR(400) NULL,
        tam_aciklama NVARCHAR(MAX) NULL,
        aktif_mi BIT NOT NULL CONSTRAINT DF_ym_kat_detay_aktif DEFAULT (1),
        guncellenme_tarihi DATETIME2(0) NOT NULL CONSTRAINT DF_ym_kat_detay_guncel DEFAULT (SYSUTCDATETIME())
    );

    CREATE UNIQUE INDEX UX_ym_kat_detay_kategori ON dbo.yardim_merkezi_kategori_detaylari(destek_kategori_id);
END;

IF OBJECT_ID(N'dbo.yardim_merkezi_kategori_sss', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.yardim_merkezi_kategori_sss
    (
        id BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        destek_kategori_id BIGINT NOT NULL,
        soru NVARCHAR(220) NOT NULL,
        cevap NVARCHAR(MAX) NOT NULL,
        siralama INT NOT NULL CONSTRAINT DF_ym_kat_sss_sira DEFAULT (0),
        aktif_mi BIT NOT NULL CONSTRAINT DF_ym_kat_sss_aktif DEFAULT (1),
        olusturulma_tarihi DATETIME2(0) NOT NULL CONSTRAINT DF_ym_kat_sss_olustur DEFAULT (SYSUTCDATETIME())
    );
    CREATE INDEX IX_ym_kat_sss_kategori ON dbo.yardim_merkezi_kategori_sss(destek_kategori_id, siralama, id);
END;

IF OBJECT_ID(N'dbo.yardim_merkezi_icerikler', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.yardim_merkezi_icerikler
    (
        id BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        icerik_turu NVARCHAR(40) NOT NULL, -- about / career / press / blog / doc
        baslik NVARCHAR(180) NOT NULL,
        seo_slug NVARCHAR(180) NOT NULL,
        ozet NVARCHAR(320) NULL,
        hero_baslik NVARCHAR(160) NULL,
        hero_alt_baslik NVARCHAR(260) NULL,
        hero_gorsel_url NVARCHAR(400) NULL,
        icerik NVARCHAR(MAX) NOT NULL,
        ikon NVARCHAR(80) NULL,
        siralama INT NOT NULL CONSTRAINT DF_ym_icerik_sira DEFAULT (0),
        one_cikan_mi BIT NOT NULL CONSTRAINT DF_ym_icerik_onecikan DEFAULT (0),
        aktif_mi BIT NOT NULL CONSTRAINT DF_ym_icerik_aktif DEFAULT (1),
        olusturulma_tarihi DATETIME2(0) NOT NULL CONSTRAINT DF_ym_icerik_olustur DEFAULT (SYSUTCDATETIME()),
        guncellenme_tarihi DATETIME2(0) NOT NULL CONSTRAINT DF_ym_icerik_guncel DEFAULT (SYSUTCDATETIME())
    );
    CREATE UNIQUE INDEX UX_ym_icerik_slug ON dbo.yardim_merkezi_icerikler(icerik_turu, seo_slug);
    CREATE INDEX IX_ym_icerik_liste ON dbo.yardim_merkezi_icerikler(icerik_turu, aktif_mi, one_cikan_mi, siralama, id);
END;

