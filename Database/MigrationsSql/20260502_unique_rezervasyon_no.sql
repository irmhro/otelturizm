IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'UX_rezervasyonlar_rezervasyon_no'
      AND object_id = OBJECT_ID(N'dbo.rezervasyonlar')
)
AND NOT EXISTS (
    SELECT 1
    FROM dbo.rezervasyonlar
    WHERE rezervasyon_no IS NOT NULL
    GROUP BY rezervasyon_no
    HAVING COUNT_BIG(*) > 1
)
BEGIN
    CREATE UNIQUE INDEX UX_rezervasyonlar_rezervasyon_no
        ON dbo.rezervasyonlar (rezervasyon_no)
        WHERE rezervasyon_no IS NOT NULL;
END
