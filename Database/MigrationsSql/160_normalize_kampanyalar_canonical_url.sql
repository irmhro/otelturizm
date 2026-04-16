UPDATE kampanyalar
SET canonical_url = CONCAT('/kampanyalar/', seo_slug)
WHERE canonical_url IS NULL
   OR TRIM(canonical_url) = '';

UPDATE kampanyalar
SET canonical_url = SUBSTRING(
    TRIM(canonical_url),
    LOCATE('/', TRIM(canonical_url), LOCATE('://', TRIM(canonical_url)) + 3)
)
WHERE canonical_url IS NOT NULL
  AND (
      LOWER(TRIM(canonical_url)) LIKE 'http://%'
      OR LOWER(TRIM(canonical_url)) LIKE 'https://%'
  )
  AND LOCATE('/', TRIM(canonical_url), LOCATE('://', TRIM(canonical_url)) + 3) > 0;

UPDATE kampanyalar
SET canonical_url = CONCAT('/kampanyalar/', seo_slug)
WHERE canonical_url IS NOT NULL
  AND (
      LOWER(TRIM(canonical_url)) LIKE 'http://%'
      OR LOWER(TRIM(canonical_url)) LIKE 'https://%'
  )
  AND LOCATE('/', TRIM(canonical_url), LOCATE('://', TRIM(canonical_url)) + 3) = 0;

UPDATE kampanyalar
SET canonical_url = CONCAT('/', TRIM(BOTH '/' FROM canonical_url))
WHERE canonical_url IS NOT NULL
  AND TRIM(canonical_url) <> ''
  AND LEFT(TRIM(canonical_url), 1) <> '/';
