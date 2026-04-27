/*
  2026-04-27
  SQL Server: bildirim_loglari tablosuna guncellenme_tarihi kolonu eksikse ekler.
*/

IF COL_LENGTH('dbo.bildirim_loglari', 'guncellenme_tarihi') IS NULL
BEGIN
    ALTER TABLE dbo.bildirim_loglari
    ADD guncellenme_tarihi DATETIME2 NULL;
END;

IF COL_LENGTH('dbo.bildirim_loglari', 'guncellenme_tarihi') IS NOT NULL
BEGIN
    EXEC sp_executesql N'
        UPDATE dbo.bildirim_loglari
        SET guncellenme_tarihi = olusturulma_tarihi
        WHERE guncellenme_tarihi IS NULL;';
END;

SELECT
    CASE WHEN COL_LENGTH('dbo.bildirim_loglari', 'guncellenme_tarihi') IS NULL THEN 0 ELSE 1 END AS kolon_var_mi;
