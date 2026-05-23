-- Tablo: dbo.GELISTIRME_TALEPLERI
IF OBJECT_ID(N'dbo.GELISTIRME_TALEPLERI', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[GELISTIRME_TALEPLERI] (
        [ID] bigint IDENTITY(1,1) NOT NULL,
        [ANA_TALEP_ID] bigint NULL,
        [CEVAP_TALEP_ID] bigint NULL,
        [KAYIT_TIPI] nvarchar(40) NOT NULL CONSTRAINT [DF_gelistirme_talepleri_kayit_tipi] DEFAULT (N'talep'),
        [KAYNAK_ROL] nvarchar(40) NOT NULL CONSTRAINT [DF_gelistirme_talepleri_kaynak_rol] DEFAULT (N'developer'),
        [OLUSTURAN_KULLANICI_ID] bigint NOT NULL,
        [ATANAN_GELISTIRICI_ID] bigint NULL,
        [BASLIK] nvarchar(220) NULL,
        [ACIKLAMA] nvarchar(max) NULL,
        [ONCELIK] nvarchar(30) NOT NULL CONSTRAINT [DF_gelistirme_talepleri_oncelik] DEFAULT (N'Orta'),
        [DURUM] nvarchar(40) NOT NULL CONSTRAINT [DF_gelistirme_talepleri_durum] DEFAULT (N'Yeni'),
        [PLANLANAN_BASLANGIC_TARIHI] date NULL,
        [HEDEF_BITIS_TARIHI] date NULL,
        [TAMAMLANMA_TARIHI] datetime2(7) NULL,
        [GORSEL_URL] nvarchar(500) NULL,
        [SILINDI_MI] bit NOT NULL CONSTRAINT [DF_gelistirme_talepleri_silindi] DEFAULT ((0)),
        [SON_HAREKET_TARIHI] datetime2(7) NOT NULL CONSTRAINT [DF_gelistirme_talepleri_son_hareket] DEFAULT (sysutcdatetime()),
        [OLUSTURULMA_TARIHI] datetime2(7) NOT NULL CONSTRAINT [DF_gelistirme_talepleri_olusturulma] DEFAULT (sysutcdatetime()),
        [GUNCELLENME_TARIHI] datetime2(7) NOT NULL CONSTRAINT [DF_gelistirme_talepleri_guncellenme] DEFAULT (sysutcdatetime()),
        CONSTRAINT [PK_GELISTIRME_TALEPLERI] PRIMARY KEY CLUSTERED ([ID] ASC)
    );
END
