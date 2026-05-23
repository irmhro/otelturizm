-- Idempotent (T450): EN ilçe meta — yalnızca ILCE_SEO_META tablosu varsa uygulanır.
-- Kaynak: Docs/seo/en-istanbul-districts-meta.json
SET NOCOUNT ON;

IF OBJECT_ID(N'dbo.ILCE_SEO_META', N'U') IS NULL
BEGIN
    PRINT N'ILCE_SEO_META yok; seed atlandi (JSON kaynak gecerli).';
    RETURN;
END;

MERGE [dbo].[ILCE_SEO_META] AS t
USING (
    VALUES
        (N'adalar', N'en', N'Compare hotels in Adalar, Istanbul. Island stays, ferry access and secure booking on Otelturizm.'),
        (N'besiktas', N'en', N'Bosphorus-side hotels in Beşiktaş, Istanbul. Compare boutique and city hotels on Otelturizm.'),
        (N'kadikoy', N'en', N'Asian-side nightlife and ferry hotels in Kadıköy, Istanbul on Otelturizm.'),
        (N'fatih', N'en', N'Sultanahmet and Old City hotels in Fatih, Istanbul. Heritage district booking on Otelturizm.')
) AS s ([SEO_SLUG], [LANG], [META_DESCRIPTION])
ON t.[SEO_SLUG] = s.[SEO_SLUG] AND t.[LANG] = s.[LANG]
WHEN MATCHED THEN
    UPDATE SET t.[META_DESCRIPTION] = s.[META_DESCRIPTION]
WHEN NOT MATCHED THEN
    INSERT ([SEO_SLUG], [LANG], [META_DESCRIPTION]) VALUES (s.[SEO_SLUG], s.[LANG], s.[META_DESCRIPTION]);
