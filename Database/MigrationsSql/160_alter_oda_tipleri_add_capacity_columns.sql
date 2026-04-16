IF COL_LENGTH('oda_tipleri', 'maksimum_yetiskin_sayisi') IS NULL
BEGIN
    ALTER TABLE oda_tipleri
    ADD maksimum_yetiskin_sayisi TINYINT NULL;
END;

IF COL_LENGTH('oda_tipleri', 'maksimum_cocuk_sayisi') IS NULL
BEGIN
    ALTER TABLE oda_tipleri
    ADD maksimum_cocuk_sayisi TINYINT NULL;
END;

UPDATE oda_tipleri
SET maksimum_yetiskin_sayisi = COALESCE(maksimum_yetiskin_sayisi, NULLIF(maksimum_kisi_sayisi, 0), 1),
    maksimum_cocuk_sayisi = COALESCE(maksimum_cocuk_sayisi, 0);

ALTER TABLE oda_tipleri
ALTER COLUMN maksimum_yetiskin_sayisi TINYINT NOT NULL;

ALTER TABLE oda_tipleri
ALTER COLUMN maksimum_cocuk_sayisi TINYINT NOT NULL;
