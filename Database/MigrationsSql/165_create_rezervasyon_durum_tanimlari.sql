/*
  Rezervasyon durumları: stabil kod + görünen ad (mevcut NVARCHAR durum ile uyumlu).
  rezervasyonlar.rezervasyon_durumu_id FK; durum_ozel_veri JSON (isteğe bağlı alanlar).
  Trigger: rezervasyon_durumu_id doluysa metin durumu eşitler; id boşsa metinden id doldurur.
*/

IF OBJECT_ID('dbo.rezervasyon_durum_tanimlari', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.rezervasyon_durum_tanimlari
    (
        id BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        kod NVARCHAR(40) NOT NULL,
        ad NVARCHAR(80) NOT NULL,
        aciklama NVARCHAR(500) NULL,
        sira_no INT NOT NULL CONSTRAINT DF_rezervasyon_durum_tanimlari_sira DEFAULT (0),
        aktif_mi BIT NOT NULL CONSTRAINT DF_rezervasyon_durum_tanimlari_aktif DEFAULT (1),
        sistem_satir_mi BIT NOT NULL CONSTRAINT DF_rezervasyon_durum_tanimlari_sistem DEFAULT (1),
        iptal_mi BIT NOT NULL CONSTRAINT DF_rezervasyon_durum_tanimlari_iptal DEFAULT (0),
        tamamlandi_mi BIT NOT NULL CONSTRAINT DF_rezervasyon_durum_tanimlari_tamamlandi DEFAULT (0),
        bekleyen_mi BIT NOT NULL CONSTRAINT DF_rezervasyon_durum_tanimlari_bekleyen DEFAULT (0),
        gelir_sayilir_mi BIT NOT NULL CONSTRAINT DF_rezervasyon_durum_tanimlari_gelir DEFAULT (0),
        ozellik_json_sablonu NVARCHAR(MAX) NULL,
        olusturulma_tarihi DATETIME2 NOT NULL CONSTRAINT DF_rezervasyon_durum_tanimlari_olustur DEFAULT (SYSUTCDATETIME())
    );

    CREATE UNIQUE INDEX UX_rezervasyon_durum_tanimlari_kod ON dbo.rezervasyon_durum_tanimlari(kod);
    CREATE UNIQUE INDEX UX_rezervasyon_durum_tanimlari_ad ON dbo.rezervasyon_durum_tanimlari(ad);
END;

/* Seed / güncelle (idempotent, kod anahtar) */
MERGE dbo.rezervasyon_durum_tanimlari AS t
USING (
    SELECT *
    FROM (VALUES
        (N'ONAY_BEKLIYOR',       N'Onay Bekliyor',       N'Varsayılan talep / onay bekleyen',        10, 1, 1, 0, 0, 1, 0, NULL),
        (N'ONAYLANDI',           N'Onaylandı',             N'Otel veya süreç onayı tamam',             20, 1, 1, 0, 0, 0, 1, NULL),
        (N'IPTAL_EDILDI',        N'İptal Edildi',          N'İptal',                                   30, 1, 1, 1, 0, 0, 0, NULL),
        (N'NO_SHOW',             N'No-Show',               N'Gelmedi',                                 40, 1, 1, 0, 0, 0, 0, NULL),
        (N'TAMAMLANDI',          N'Tamamlandı',            N'Konaklama tamamlandı',                    50, 1, 1, 0, 1, 1, 0, NULL),
        (N'DEGISIKLIK_BEKLIYOR', N'Değişiklik Bekliyor',   N'Değişiklik talebi',                       15, 1, 1, 0, 0, 1, 0, NULL)
    ) AS v(kod, ad, aciklama, sira_no, aktif_mi, sistem_satir_mi, iptal_mi, tamamlandi_mi, bekleyen_mi, gelir_sayilir_mi, ozellik_json_sablonu)
) AS s
ON t.kod = s.kod
WHEN MATCHED THEN
    UPDATE SET
        ad = s.ad,
        aciklama = s.aciklama,
        sira_no = s.sira_no,
        aktif_mi = s.aktif_mi,
        sistem_satir_mi = s.sistem_satir_mi,
        iptal_mi = s.iptal_mi,
        tamamlandi_mi = s.tamamlandi_mi,
        bekleyen_mi = s.bekleyen_mi,
        gelir_sayilir_mi = s.gelir_sayilir_mi,
        ozellik_json_sablonu = COALESCE(t.ozellik_json_sablonu, s.ozellik_json_sablonu)
WHEN NOT MATCHED THEN
    INSERT (kod, ad, aciklama, sira_no, aktif_mi, sistem_satir_mi, iptal_mi, tamamlandi_mi, bekleyen_mi, gelir_sayilir_mi, ozellik_json_sablonu)
    VALUES (s.kod, s.ad, s.aciklama, s.sira_no, s.aktif_mi, s.sistem_satir_mi, s.iptal_mi, s.tamamlandi_mi, s.bekleyen_mi, s.gelir_sayilir_mi, s.ozellik_json_sablonu);

IF COL_LENGTH('dbo.rezervasyonlar', 'rezervasyon_durumu_id') IS NULL
BEGIN
    ALTER TABLE dbo.rezervasyonlar ADD rezervasyon_durumu_id BIGINT NULL;
END;

IF COL_LENGTH('dbo.rezervasyonlar', 'durum_ozel_veri') IS NULL
BEGIN
    ALTER TABLE dbo.rezervasyonlar ADD durum_ozel_veri NVARCHAR(MAX) NULL;
END;

/* Mevcut metin durum -> id */
UPDATE r
SET r.rezervasyon_durumu_id = d.id
FROM dbo.rezervasyonlar r
INNER JOIN dbo.rezervasyon_durum_tanimlari d ON d.ad = r.durum
WHERE r.rezervasyon_durumu_id IS NULL;

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_rezervasyonlar_rezervasyon_durumu')
    AND OBJECT_ID('dbo.rezervasyon_durum_tanimlari', 'U') IS NOT NULL
BEGIN
    ALTER TABLE dbo.rezervasyonlar
        ADD CONSTRAINT FK_rezervasyonlar_rezervasyon_durumu
        FOREIGN KEY (rezervasyon_durumu_id) REFERENCES dbo.rezervasyon_durum_tanimlari(id);
END;

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.rezervasyonlar') AND name = N'IX_rezervasyonlar_rezervasyon_durumu_id'
)
BEGIN
    CREATE INDEX IX_rezervasyonlar_rezervasyon_durumu_id ON dbo.rezervasyonlar(rezervasyon_durumu_id);
END;

GO

IF OBJECT_ID(N'dbo.tr_rezervasyonlar_rezervasyon_durumu_sync', N'TR') IS NOT NULL
    DROP TRIGGER dbo.tr_rezervasyonlar_rezervasyon_durumu_sync;

GO

CREATE TRIGGER dbo.tr_rezervasyonlar_rezervasyon_durumu_sync
ON dbo.rezervasyonlar
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    /*
      İş kuralı: rezervasyonlar.durum (metin) ana kaynak; FK id denormalized önbellek.
      Böylece mevcut kod yalnızca durum metnini güncellerken id otomatik eşlenir.
    */
    UPDATE r
    SET rezervasyon_durumu_id = d.id
    FROM dbo.rezervasyonlar r
    INNER JOIN inserted i ON i.id = r.id
    INNER JOIN dbo.rezervasyon_durum_tanimlari d ON d.ad = r.durum;
END;

GO
