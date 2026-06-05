SET NOCOUNT ON;

-- AIDS, din, ırk/etnisite ve hassas içerikler — header vitrininden kaldır
UPDATE dbo.OZEL_GUNLER
SET [AKTIF_MI] = 0
WHERE [GUN_KODU] IN (
    N'aids-gunu',
    N'noel',
    N'yerli-halklar-gunu',
    N'koeliligin-kaldirilmasi-gunu',
    N'kadin-haklari-gunu',
    N'engelliler-gunu',
    N'insan-haklari-gunu'
);

DELETE FROM dbo.OZEL_GUNLER
WHERE [GUN_KODU] IN (
    N'aids-gunu',
    N'noel',
    N'yerli-halklar-gunu',
    N'koeliligin-kaldirilmasi-gunu',
    N'kadin-haklari-gunu',
    N'engelliler-gunu',
    N'insan-haklari-gunu'
);

PRINT N'OZEL_GUNLER hassas kayitlar kaldirildi.';
GO
