-- Tablo: dbo.KULLANICILAR_ARSIV_YEDEK
IF OBJECT_ID(N'dbo.KULLANICILAR_ARSIV_YEDEK', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[KULLANICILAR_ARSIV_YEDEK] (
        [ID] bigint NOT NULL CONSTRAINT [DF__kullanicilar__id__32767D0B] DEFAULT ((0)),
        [AD_SOYAD] nvarchar(100) NOT NULL,
        [EPOSTA] nvarchar(100) NOT NULL,
        [TELEFON] nvarchar(20) NULL,
        [SIFRE] nvarchar(255) NOT NULL,
        [PROFIL_FOTOGRAFI] nvarchar(255) NULL,
        [EPOSTA_DOGRULAMA_TARIHI] datetime2(0) NULL,
        [TELEFON_DOGRULAMA_TARIHI] datetime2(0) NULL,
        [SON_GIRIS_TARIHI] datetime2(0) NULL,
        [SON_GIRIS_IP] nvarchar(45) NULL,
        [HESAP_DURUMU] tinyint NOT NULL CONSTRAINT [DF__kullanici__hesap__336AA144] DEFAULT ((1)),
        [DIL_TERCIHI] nvarchar(5) NULL,
        [PARA_BIRIMI] nvarchar(3) NULL,
        [ULKE] nvarchar(50) NULL,
        [OLUSTURULMA_TARIHI] datetime2(0) NULL CONSTRAINT [DF__kullanici__olust__345EC57D] DEFAULT (sysutcdatetime()),
        [GUNCELLENME_TARIHI] datetime2(0) NULL,
        CONSTRAINT [PK_KULLANICILAR_ARSIV_YEDEK] PRIMARY KEY CLUSTERED ([ID] ASC)
    );
END
