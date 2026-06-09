-- Idempotent: anasayfa otel vitrin bölümleri + varsayılan otel atamaları (UTF-8)
SET NOCOUNT ON;
SET XACT_ABORT ON;

IF OBJECT_ID(N'dbo.ANASAYFA_OTEL_BOLUMLERI', N'U') IS NULL RETURN;
IF OBJECT_ID(N'dbo.ANASAYFA_OTEL_KAYITLARI', N'U') IS NULL RETURN;
IF OBJECT_ID(N'dbo.OTELLER', N'U') IS NULL RETURN;

DECLARE @Sections TABLE
(
    BolumKodu nvarchar(50) NOT NULL PRIMARY KEY,
    Baslik nvarchar(200) NOT NULL,
    Siralama int NOT NULL,
    HotelLimit int NOT NULL
);

INSERT INTO @Sections (BolumKodu, Baslik, Siralama, HotelLimit) VALUES
(N'ozel-rotalar', N'Seçilen Özel Rotalar', 10, 4),
(N'hafta-sonu-firsatlari', N'Hafta Sonu Fırsatları', 20, 8),
(N'butceme-uygun-oteller', N'Bütçene Uygun', 30, 8),
(N'evcil-hayvan-dostu', N'Evcil Hayvan Dostu', 40, 8),
(N'kampanyaya-dahil-oteller', N'Kampanyalı Oteller', 50, 8),
(N'ultra-luks', N'Yıldız Yağmuru', 60, 8);

DECLARE @BolumKodu nvarchar(50);
DECLARE @Baslik nvarchar(200);
DECLARE @Siralama int;
DECLARE @HotelLimit int;
DECLARE @SectionId bigint;

DECLARE section_cursor CURSOR LOCAL FAST_FORWARD FOR
    SELECT BolumKodu, Baslik, Siralama, HotelLimit
    FROM @Sections
    ORDER BY Siralama;

OPEN section_cursor;
FETCH NEXT FROM section_cursor INTO @BolumKodu, @Baslik, @Siralama, @HotelLimit;

WHILE @@FETCH_STATUS = 0
BEGIN
    IF NOT EXISTS (SELECT 1 FROM [dbo].[ANASAYFA_OTEL_BOLUMLERI] WHERE [BOLUM_KODU] = @BolumKodu)
    BEGIN
        INSERT INTO [dbo].[ANASAYFA_OTEL_BOLUMLERI] ([BOLUM_KODU], [BASLIK], [ALT_BASLIK], [SIRALAMA], [AKTIF_MI])
        VALUES (@BolumKodu, @Baslik, NULL, @Siralama, 1);
    END;

    SELECT @SectionId = [ID]
    FROM [dbo].[ANASAYFA_OTEL_BOLUMLERI]
    WHERE [BOLUM_KODU] = @BolumKodu;

    IF @SectionId IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM [dbo].[ANASAYFA_OTEL_KAYITLARI] WHERE [BOLUM_ID] = @SectionId)
    BEGIN
        INSERT INTO [dbo].[ANASAYFA_OTEL_KAYITLARI] ([BOLUM_ID], [OTEL_ID], [SIRALAMA], [AKTIF_MI])
        SELECT TOP (@HotelLimit)
            @SectionId,
            ranked.[ID],
            ranked.[RowNum] * 10,
            1
        FROM (
            SELECT
                o.[ID],
                ROW_NUMBER() OVER (ORDER BY COALESCE(o.[POPULERLIK_SIRASI], 9999), o.[ID]) AS [RowNum]
            FROM [dbo].[OTELLER] o
            WHERE o.[YAYIN_DURUMU] = N'Yayında'
              AND o.[ONAY_DURUMU] IN (N'Onaylandı', N'Onaylandi', N'OnaylandÄ±', N'Onaylanmış', N'Onaylanmis', N'Onayli')
        ) ranked
        ORDER BY ranked.[RowNum];
    END;

    SET @SectionId = NULL;
    FETCH NEXT FROM section_cursor INTO @BolumKodu, @Baslik, @Siralama, @HotelLimit;
END;

CLOSE section_cursor;
DEALLOCATE section_cursor;
GO
