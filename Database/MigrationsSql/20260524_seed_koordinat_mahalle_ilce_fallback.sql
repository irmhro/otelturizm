/* Mahalle koordinati: posta kodu guncellemesi oncesi ilce merkezine bagla (gecici / yedek) */
SET NOCOUNT ON;
GO

UPDATE m
SET
    m.[ENLEM] = c.[ENLEM],
    m.[BOYLAM] = c.[BOYLAM],
    m.[GUNCELLENME_TARIHI] = sysutcdatetime()
FROM [dbo].[MAHALLELER] AS m
INNER JOIN [dbo].[ILCELER] AS c ON c.[ID] = m.[ILCE_ID]
WHERE (m.[ENLEM] IS NULL OR m.[BOYLAM] IS NULL)
  AND c.[ENLEM] IS NOT NULL
  AND c.[BOYLAM] IS NOT NULL;
GO
