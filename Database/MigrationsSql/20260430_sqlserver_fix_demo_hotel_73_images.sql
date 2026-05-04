SET NOCOUNT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;

DECLARE @hotelId BIGINT = 73;
DECLARE @roomId BIGINT = 182;

IF OBJECT_ID(N'dbo.oteller', N'U') IS NULL
BEGIN
    PRINT N'dbo.oteller bulunamadi; migration atlandi.';
    RETURN;
END

IF NOT EXISTS (SELECT 1 FROM dbo.oteller WHERE id = @hotelId)
BEGIN
    PRINT N'Otel bulunamadi: 73; migration atlandi.';
    RETURN;
END

IF OBJECT_ID(N'dbo.otel_gorselleri', N'U') IS NULL
BEGIN
    PRINT N'dbo.otel_gorselleri bulunamadi; migration atlandi.';
    RETURN;
END

DECLARE @base NVARCHAR(200) = N'/uploads/images/73/hotel/';

-- Kapak foto: ilk demo görsel.
UPDATE dbo.oteller
SET kapak_fotografi = @base + N'demo-hotel-01.webp'
WHERE id = @hotelId
  AND (
      kapak_fotografi IS NULL
      OR LTRIM(RTRIM(kapak_fotografi)) = N''
      OR kapak_fotografi LIKE N'uploads/odalar/%'
      OR kapak_fotografi LIKE N'/uploads/odalar/%'
      OR kapak_fotografi NOT LIKE N'/uploads/images/%'
  );

;WITH files AS (
    SELECT @base + N'demo-hotel-01.webp' AS url, CAST(1 AS SMALLINT) AS sira UNION ALL
    SELECT @base + N'demo-hotel-02.webp', 2 UNION ALL
    SELECT @base + N'demo-hotel-03.webp', 3 UNION ALL
    SELECT @base + N'demo-hotel-04.webp', 4 UNION ALL
    SELECT @base + N'demo-hotel-05.webp', 5 UNION ALL
    SELECT @base + N'demo-hotel-06.webp', 6 UNION ALL
    SELECT @base + N'demo-hotel-07.webp', 7 UNION ALL
    SELECT @base + N'demo-hotel-08.webp', 8 UNION ALL
    SELECT @base + N'demo-hotel-09.webp', 9 UNION ALL
    SELECT @base + N'demo-hotel-10.webp', 10 UNION ALL
    SELECT @base + N'demo-hotel-11.webp', 11 UNION ALL
    SELECT @base + N'demo-hotel-12.webp', 12 UNION ALL
    SELECT @base + N'demo-hotel-13.webp', 13 UNION ALL
    SELECT @base + N'demo-hotel-14.webp', 14 UNION ALL
    SELECT @base + N'demo-hotel-15.webp', 15 UNION ALL
    SELECT @base + N'demo-hotel-16.webp', 16 UNION ALL
    SELECT @base + N'demo-hotel-17.webp', 17 UNION ALL
    SELECT @base + N'demo-hotel-18.webp', 18 UNION ALL
    SELECT @base + N'demo-hotel-19.webp', 19 UNION ALL
    SELECT @base + N'demo-hotel-20.webp', 20
)
INSERT INTO dbo.otel_gorselleri
(
    otel_id,
    gorsel_url,
    gorsel_turu,
    baslik,
    aciklama,
    kapak_fotografi_mi,
    one_cikan,
    siralama,
    onay_durumu
)
SELECT
    @hotelId,
    f.url,
    N'Genel Alan',
    N'Demo Otel Gorseli',
    N'Local demo gorsel seti (otel_id=73).',
    CASE WHEN f.sira = 1 THEN 1 ELSE 0 END,
    CASE WHEN f.sira = 1 THEN 1 ELSE 0 END,
    f.sira,
    N'Onaylandi'
FROM files f
WHERE NOT EXISTS (
    SELECT 1
    FROM dbo.otel_gorselleri og
    WHERE og.otel_id = @hotelId
      AND og.gorsel_url = f.url
);

IF OBJECT_ID(N'dbo.oda_tipleri', N'U') IS NOT NULL
   AND OBJECT_ID(N'dbo.oda_gorselleri', N'U') IS NOT NULL
   AND EXISTS (SELECT 1 FROM dbo.oda_tipleri WHERE id = @roomId AND otel_id = @hotelId)
BEGIN
    DECLARE @roomBase NVARCHAR(240) = N'/uploads/images/73/rooms/182/';

    UPDATE dbo.oda_tipleri
    SET kapak_fotografi = @roomBase + N'demo-room-01.webp'
    WHERE id = @roomId
      AND (
          kapak_fotografi IS NULL
          OR LTRIM(RTRIM(kapak_fotografi)) = N''
          OR kapak_fotografi NOT LIKE N'/uploads/images/%'
      );

    ;WITH room_files AS (
        SELECT @roomBase + N'demo-room-01.webp' AS url, CAST(1 AS SMALLINT) AS sira UNION ALL
        SELECT @roomBase + N'demo-room-02.webp', 2 UNION ALL
        SELECT @roomBase + N'demo-room-03.webp', 3 UNION ALL
        SELECT @roomBase + N'demo-room-04.webp', 4 UNION ALL
        SELECT @roomBase + N'demo-room-05.webp', 5 UNION ALL
        SELECT @roomBase + N'demo-room-06.webp', 6 UNION ALL
        SELECT @roomBase + N'demo-room-07.webp', 7 UNION ALL
        SELECT @roomBase + N'demo-room-08.webp', 8 UNION ALL
        SELECT @roomBase + N'demo-room-09.webp', 9 UNION ALL
        SELECT @roomBase + N'demo-room-10.webp', 10 UNION ALL
        SELECT @roomBase + N'demo-room-11.webp', 11 UNION ALL
        SELECT @roomBase + N'demo-room-12.webp', 12 UNION ALL
        SELECT @roomBase + N'demo-room-13.webp', 13 UNION ALL
        SELECT @roomBase + N'demo-room-14.webp', 14 UNION ALL
        SELECT @roomBase + N'demo-room-15.webp', 15
    )
    INSERT INTO dbo.oda_gorselleri
    (
        oda_tip_id,
        gorsel_url,
        baslik,
        aciklama,
        kapak_fotografi_mi,
        siralama,
        onay_durumu
    )
    SELECT
        @roomId,
        f.url,
        N'Demo Oda Gorseli',
        N'Local demo oda gorsel seti (oda_tip_id=182).',
        CASE WHEN f.sira = 1 THEN 1 ELSE 0 END,
        f.sira,
        N'Onaylandi'
    FROM room_files f
    WHERE NOT EXISTS (
        SELECT 1
        FROM dbo.oda_gorselleri og
        WHERE og.oda_tip_id = @roomId
          AND og.gorsel_url = f.url
    );
END

PRINT N'Demo otel 73 ve oda 182 gorselleri senkronlandi.';

