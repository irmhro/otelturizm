/* ABD askeri posta eyaletleri (APO) - harita aramasi icin yaklasik merkez noktalari */
SET NOCOUNT ON;
GO

UPDATE [dbo].[ILLER]
SET
    [ENLEM] = v.[ENLEM],
    [BOYLAM] = v.[BOYLAM],
    [GUNCELLENME_TARIHI] = sysutcdatetime()
FROM [dbo].[ILLER] AS i
INNER JOIN (
    VALUES
        (N'Armed Forces Europe', CAST(50.110924 AS decimal(10,8)), CAST(8.682127 AS decimal(11,8))),
        (N'Armed Forces of the Americas', CAST(38.907192 AS decimal(10,8)), CAST(-77.036871 AS decimal(11,8))),
        (N'Armed Forces Pacific', CAST(21.306944 AS decimal(10,8)), CAST(-157.858337 AS decimal(11,8)))
) AS v([IL_ADI], [ENLEM], [BOYLAM])
    ON i.[IL_ADI] = v.[IL_ADI]
   AND i.[BOLGE_TIPI] = N'EYALET'
WHERE i.[ENLEM] IS NULL OR i.[BOYLAM] IS NULL;
GO
