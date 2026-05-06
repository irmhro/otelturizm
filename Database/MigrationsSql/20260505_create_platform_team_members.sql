/*
  Yardım Merkezi -> Ekibimiz (platform takım kartları) yönetimi.
  Admin panelinden ekleme/düzenleme/silme/sıralama yapılır.
*/

IF OBJECT_ID(N'dbo.platform_ekip_uyeleri', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.platform_ekip_uyeleri
    (
        id BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        ad_soyad NVARCHAR(120) NOT NULL,
        unvan NVARCHAR(160) NOT NULL,
        eposta NVARCHAR(160) NOT NULL,
        aciklama NVARCHAR(260) NULL,
        avatar_url NVARCHAR(400) NULL,
        siralama INT NOT NULL CONSTRAINT DF_platform_ekip_uyeleri_siralama DEFAULT (0),
        aktif_mi BIT NOT NULL CONSTRAINT DF_platform_ekip_uyeleri_aktif DEFAULT (1),
        olusturulma_tarihi DATETIME2(0) NOT NULL CONSTRAINT DF_platform_ekip_uyeleri_olustur DEFAULT (SYSUTCDATETIME()),
        guncellenme_tarihi DATETIME2(0) NULL
    );

    CREATE INDEX IX_platform_ekip_uyeleri_siralama ON dbo.platform_ekip_uyeleri(siralama, id);
    CREATE INDEX IX_platform_ekip_uyeleri_aktif ON dbo.platform_ekip_uyeleri(aktif_mi, siralama, id);
END;

