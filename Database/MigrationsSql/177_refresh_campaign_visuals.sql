SET NOCOUNT ON;

UPDATE k
SET
    hero_gorseli = '/uploads/demo/campaigns/' + k.seo_slug + '/campaign-hero.jpg',
    kart_gorseli = '/uploads/demo/campaigns/' + k.seo_slug + '/campaign-hero.jpg',
    banner_gorseli = '/uploads/demo/campaigns/' + k.seo_slug + '/campaign-hero.jpg',
    mobil_gorsel = '/uploads/demo/campaigns/' + k.seo_slug + '/campaign-hero.jpg'
FROM kampanyalar AS k
WHERE k.aktif_mi = 1
  AND k.seo_slug IS NOT NULL
  AND LTRIM(RTRIM(k.seo_slug)) <> '';

IF OBJECT_ID('dbo.schema_migrations', 'U') IS NOT NULL
AND NOT EXISTS (
    SELECT 1
    FROM dbo.schema_migrations
    WHERE script_name = '177_refresh_campaign_visuals.sql'
)
BEGIN
    INSERT INTO dbo.schema_migrations (script_name, checksum, applied_at)
    VALUES ('177_refresh_campaign_visuals.sql', 'df67c2a4e6ad96b120d9910a4a08394bf67978759245a268e62c69298e18ea13', SYSUTCDATETIME());
END;
