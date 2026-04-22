-- oteller tablosunda puan alanlari decimal(3,2) ile max 9.99; vitrin puani (genel_puan 1-5 * 2 = 2-10) icin genisletilir.
-- Canli ve yeni kurulumlarda 167 scriptinden once veya sonra calistirilabilir.

IF COL_LENGTH('oteller', 'ortalama_puan') IS NOT NULL
BEGIN
    ALTER TABLE oteller ALTER COLUMN ortalama_puan DECIMAL(5, 2) NULL;
END

IF COL_LENGTH('oteller', 'konum_puani') IS NOT NULL
BEGIN
    ALTER TABLE oteller ALTER COLUMN konum_puani DECIMAL(5, 2) NULL;
END

IF COL_LENGTH('oteller', 'konfor_puani') IS NOT NULL
BEGIN
    ALTER TABLE oteller ALTER COLUMN konfor_puani DECIMAL(5, 2) NULL;
END

IF COL_LENGTH('oteller', 'fiyat_performans_puani') IS NOT NULL
BEGIN
    ALTER TABLE oteller ALTER COLUMN fiyat_performans_puani DECIMAL(5, 2) NULL;
END
