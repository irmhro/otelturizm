IF COL_LENGTH(N'dbo.yorumlar', N'rezervasyon_no') IS NULL
BEGIN
    ALTER TABLE dbo.yorumlar ADD rezervasyon_no NVARCHAR(40) NULL;
END;
GO

UPDATE y
SET y.rezervasyon_no = r.rezervasyon_no
FROM dbo.yorumlar y
INNER JOIN dbo.rezervasyonlar r ON r.id = y.rezervasyon_id
WHERE (y.rezervasyon_no IS NULL OR y.rezervasyon_no = N'');
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_yorumlar_rezervasyon_no'
      AND object_id = OBJECT_ID(N'dbo.yorumlar')
)
BEGIN
    CREATE INDEX IX_yorumlar_rezervasyon_no ON dbo.yorumlar(rezervasyon_no);
END;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'UX_yorumlar_rezervasyon_id_kullanici_id'
      AND object_id = OBJECT_ID(N'dbo.yorumlar')
)
AND NOT EXISTS (
    SELECT 1
    FROM dbo.yorumlar
    WHERE rezervasyon_id IS NOT NULL
    GROUP BY rezervasyon_id, kullanici_id
    HAVING COUNT(*) > 1
)
BEGIN
    CREATE UNIQUE INDEX UX_yorumlar_rezervasyon_id_kullanici_id
    ON dbo.yorumlar(rezervasyon_id, kullanici_id)
    WHERE rezervasyon_id IS NOT NULL;
END;
