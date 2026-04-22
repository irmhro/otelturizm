/*
  Ödeme durumu ve yöntemi lookup tabloları; rezervasyon başına çoklu ödeme kalemi (kart + havale vb.).
  rezervasyonlar.odeme_durumu metni ana kaynak; odeme_durumu_id trigger ile eşlenir.
*/

IF OBJECT_ID('dbo.odeme_durumu_tanimlari', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.odeme_durumu_tanimlari
    (
        id BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        kod NVARCHAR(40) NOT NULL,
        ad NVARCHAR(80) NOT NULL,
        aciklama NVARCHAR(500) NULL,
        sira_no INT NOT NULL CONSTRAINT DF_odeme_durumu_tanimlari_sira DEFAULT (0),
        aktif_mi BIT NOT NULL CONSTRAINT DF_odeme_durumu_tanimlari_aktif DEFAULT (1),
        bekleyen_mi BIT NOT NULL CONSTRAINT DF_odeme_durumu_tanimlari_bekleyen DEFAULT (0),
        basari_mi BIT NOT NULL CONSTRAINT DF_odeme_durumu_tanimlari_basari DEFAULT (0),
        tam_mi BIT NOT NULL CONSTRAINT DF_odeme_durumu_tanimlari_tam DEFAULT (0),
        iade_mi BIT NOT NULL CONSTRAINT DF_odeme_durumu_tanimlari_iade DEFAULT (0),
        olusturulma_tarihi DATETIME2 NOT NULL CONSTRAINT DF_odeme_durumu_tanimlari_olustur DEFAULT (SYSUTCDATETIME())
    );
    CREATE UNIQUE INDEX UX_odeme_durumu_tanimlari_kod ON dbo.odeme_durumu_tanimlari(kod);
    CREATE UNIQUE INDEX UX_odeme_durumu_tanimlari_ad ON dbo.odeme_durumu_tanimlari(ad);
END;

IF OBJECT_ID('dbo.odeme_yontemi_tanimlari', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.odeme_yontemi_tanimlari
    (
        id BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        kod NVARCHAR(40) NOT NULL,
        ad NVARCHAR(80) NOT NULL,
        aciklama NVARCHAR(500) NULL,
        sira_no INT NOT NULL CONSTRAINT DF_odeme_yontemi_tanimlari_sira DEFAULT (0),
        aktif_mi BIT NOT NULL CONSTRAINT DF_odeme_yontemi_tanimlari_aktif DEFAULT (1),
        sistem_satir_mi BIT NOT NULL CONSTRAINT DF_odeme_yontemi_tanimlari_sistem DEFAULT (1),
        kapida_mi BIT NOT NULL CONSTRAINT DF_odeme_yontemi_tanimlari_kapida DEFAULT (0),
        kart_mi BIT NOT NULL CONSTRAINT DF_odeme_yontemi_tanimlari_kart DEFAULT (0),
        havale_mi BIT NOT NULL CONSTRAINT DF_odeme_yontemi_tanimlari_havale DEFAULT (0),
        online_mi BIT NOT NULL CONSTRAINT DF_odeme_yontemi_tanimlari_online DEFAULT (0),
        olusturulma_tarihi DATETIME2 NOT NULL CONSTRAINT DF_odeme_yontemi_tanimlari_olustur DEFAULT (SYSUTCDATETIME())
    );
    CREATE UNIQUE INDEX UX_odeme_yontemi_tanimlari_kod ON dbo.odeme_yontemi_tanimlari(kod);
    CREATE UNIQUE INDEX UX_odeme_yontemi_tanimlari_ad ON dbo.odeme_yontemi_tanimlari(ad);
END;

MERGE dbo.odeme_durumu_tanimlari AS t
USING (
    SELECT * FROM (VALUES
        (N'BEKLEMEDE',        N'Beklemede',        N'Ödeme bekleniyor veya havale onayı',                    10, 1, 1, 0, 0, 0),
        (N'ON_ODEME_ALINDI',  N'Ön Ödeme Alındı',  N'Ön ödeme tahsil edildi',                                20, 1, 0, 1, 0, 0),
        (N'KISMEN_ODENDI',    N'Kısmen Ödendi',    N'Bir kısım tahsil',                                       30, 1, 0, 1, 0, 0),
        (N'TAMAMLANDI',       N'Tamamlandı',       N'Tüm tutar tahsil',                                       40, 1, 0, 1, 1, 0),
        (N'IADE_EDILDI',      N'İade Edildi',      N'İade',                                                   50, 1, 0, 0, 0, 1),
        (N'KISMI_IADE',       N'Kısmi İade',       N'Kısmi iade',                                             55, 1, 0, 0, 0, 1),
        (N'BASARISIZ',        N'Başarısız',        N'Ödeme başarısız',                                       60, 1, 1, 0, 0, 0)
    ) AS v(kod, ad, aciklama, sira_no, aktif_mi, bekleyen_mi, basari_mi, tam_mi, iade_mi)
) AS s
ON t.kod = s.kod
WHEN MATCHED THEN
    UPDATE SET ad = s.ad, aciklama = s.aciklama, sira_no = s.sira_no, aktif_mi = s.aktif_mi,
               bekleyen_mi = s.bekleyen_mi, basari_mi = s.basari_mi, tam_mi = s.tam_mi, iade_mi = s.iade_mi
WHEN NOT MATCHED THEN
    INSERT (kod, ad, aciklama, sira_no, aktif_mi, bekleyen_mi, basari_mi, tam_mi, iade_mi)
    VALUES (s.kod, s.ad, s.aciklama, s.sira_no, s.aktif_mi, s.bekleyen_mi, s.basari_mi, s.tam_mi, s.iade_mi);

MERGE dbo.odeme_yontemi_tanimlari AS t
USING (
    SELECT * FROM (VALUES
        (N'KAPIDA_ODEME', N'Kapıda Ödeme', N'Otelde nakit/kart',              10, 1, 1, 1, 0, 0, 0),
        (N'NAKIT',       N'Nakit',         N'Kapıda nakit',                  11, 1, 1, 0, 0, 0, 0),
        (N'SANAL_POS',    N'Sanal POS',     N'Online kart / 3D',             20, 1, 1, 0, 1, 0, 1),
        (N'KREDI_KARTI',  N'Kredi Kartı',   N'Kart',                         21, 1, 1, 0, 1, 0, 1),
        (N'HAVALE_EFT',   N'Havale/EFT',    N'Banka havalesi',               30, 1, 1, 0, 0, 1, 0),
        (N'BANKA_HAVALESI', N'Banka Havalesi', N'Eski metin uyumu',          31, 1, 1, 0, 0, 1, 0),
        (N'DIJITAL_CUZDAN', N'Dijital Cüzdan', N'Pay / cüzdan',              40, 1, 1, 0, 0, 0, 1),
        (N'DIGER',        N'Diğer',         N'Diğer',                        99, 1, 1, 0, 0, 0, 0)
    ) AS v(kod, ad, aciklama, sira_no, aktif_mi, sistem_satir_mi, kapida_mi, kart_mi, havale_mi, online_mi)
) AS s
ON t.kod = s.kod
WHEN MATCHED THEN
    UPDATE SET ad = s.ad, aciklama = s.aciklama, sira_no = s.sira_no, aktif_mi = s.aktif_mi,
               sistem_satir_mi = s.sistem_satir_mi, kapida_mi = s.kapida_mi, kart_mi = s.kart_mi,
               havale_mi = s.havale_mi, online_mi = s.online_mi
WHEN NOT MATCHED THEN
    INSERT (kod, ad, aciklama, sira_no, aktif_mi, sistem_satir_mi, kapida_mi, kart_mi, havale_mi, online_mi)
    VALUES (s.kod, s.ad, s.aciklama, s.sira_no, s.aktif_mi, s.sistem_satir_mi, s.kapida_mi, s.kart_mi, s.havale_mi, s.online_mi);

IF OBJECT_ID('dbo.rezervasyon_odeme_kalemleri', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.rezervasyon_odeme_kalemleri
    (
        id BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        rezervasyon_id BIGINT NOT NULL,
        odeme_yontemi_id BIGINT NOT NULL,
        odeme_durumu_id BIGINT NOT NULL,
        tutar DECIMAL(18,2) NOT NULL CONSTRAINT DF_rezervasyon_odeme_kalem_tutar DEFAULT (0),
        tahsil_edilen_tutar DECIMAL(18,2) NULL,
        sira_no INT NOT NULL CONSTRAINT DF_rezervasyon_odeme_kalem_sira DEFAULT (1),
        havale_eft_referans NVARCHAR(120) NULL,
        dekont_guvenli_dosya_id BIGINT NULL,
        aciklama NVARCHAR(500) NULL,
        olusturulma_tarihi DATETIME2 NOT NULL CONSTRAINT DF_rezervasyon_odeme_kalem_olustur DEFAULT (SYSUTCDATETIME())
    );

    CREATE INDEX IX_rezervasyon_odeme_kalemleri_rez ON dbo.rezervasyon_odeme_kalemleri(rezervasyon_id);

    ALTER TABLE dbo.rezervasyon_odeme_kalemleri
        ADD CONSTRAINT FK_rezervasyon_odeme_kalem_rez
        FOREIGN KEY (rezervasyon_id) REFERENCES dbo.rezervasyonlar(id);

    ALTER TABLE dbo.rezervasyon_odeme_kalemleri
        ADD CONSTRAINT FK_rezervasyon_odeme_kalem_yontem
        FOREIGN KEY (odeme_yontemi_id) REFERENCES dbo.odeme_yontemi_tanimlari(id);

    ALTER TABLE dbo.rezervasyon_odeme_kalemleri
        ADD CONSTRAINT FK_rezervasyon_odeme_kalem_durum
        FOREIGN KEY (odeme_durumu_id) REFERENCES dbo.odeme_durumu_tanimlari(id);

    IF OBJECT_ID('dbo.guvenli_dosya_varliklari', 'U') IS NOT NULL
    BEGIN
        ALTER TABLE dbo.rezervasyon_odeme_kalemleri
            ADD CONSTRAINT FK_rezervasyon_odeme_kalem_dekont
            FOREIGN KEY (dekont_guvenli_dosya_id) REFERENCES dbo.guvenli_dosya_varliklari(id);
    END;
END;

IF COL_LENGTH('dbo.rezervasyonlar', 'odeme_durumu_id') IS NULL
BEGIN
    ALTER TABLE dbo.rezervasyonlar ADD odeme_durumu_id BIGINT NULL;
END;

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_rezervasyonlar_odeme_durumu')
    AND OBJECT_ID('dbo.odeme_durumu_tanimlari', 'U') IS NOT NULL
BEGIN
    ALTER TABLE dbo.rezervasyonlar
        ADD CONSTRAINT FK_rezervasyonlar_odeme_durumu
        FOREIGN KEY (odeme_durumu_id) REFERENCES dbo.odeme_durumu_tanimlari(id);
END;

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.rezervasyonlar') AND name = N'IX_rezervasyonlar_odeme_durumu_id'
)
BEGIN
    CREATE INDEX IX_rezervasyonlar_odeme_durumu_id ON dbo.rezervasyonlar(odeme_durumu_id);
END;

/* Mevcut ödeme metni -> id */
UPDATE r
SET r.odeme_durumu_id = d.id
FROM dbo.rezervasyonlar r
INNER JOIN dbo.odeme_durumu_tanimlari d ON d.ad = r.odeme_durumu
WHERE r.odeme_durumu_id IS NULL AND r.odeme_durumu IS NOT NULL;

GO

IF OBJECT_ID(N'dbo.tr_rezervasyonlar_odeme_durumu_sync', N'TR') IS NOT NULL
    DROP TRIGGER dbo.tr_rezervasyonlar_odeme_durumu_sync;

GO

CREATE TRIGGER dbo.tr_rezervasyonlar_odeme_durumu_sync
ON dbo.rezervasyonlar
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE r
    SET odeme_durumu_id = d.id
    FROM dbo.rezervasyonlar r
    INNER JOIN inserted i ON i.id = r.id
    INNER JOIN dbo.odeme_durumu_tanimlari d ON d.ad = r.odeme_durumu
    WHERE COALESCE(r.odeme_durumu, N'') <> N'';
END;

GO

IF COL_LENGTH('dbo.rezervasyonlar', 'havale_eft_bekleyen_tutari') IS NULL
BEGIN
    ALTER TABLE dbo.rezervasyonlar ADD havale_eft_bekleyen_tutari DECIMAL(18,2) NULL;
END;

GO

IF OBJECT_ID('dbo.rezervasyon_odeme_kalemleri', 'U') IS NOT NULL
    AND OBJECT_ID('dbo.guvenli_dosya_varliklari', 'U') IS NOT NULL
    AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_rezervasyon_odeme_kalem_dekont')
BEGIN
    ALTER TABLE dbo.rezervasyon_odeme_kalemleri
        ADD CONSTRAINT FK_rezervasyon_odeme_kalem_dekont
        FOREIGN KEY (dekont_guvenli_dosya_id) REFERENCES dbo.guvenli_dosya_varliklari(id);
END;

GO
