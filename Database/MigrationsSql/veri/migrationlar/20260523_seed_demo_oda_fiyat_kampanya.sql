-- Idempotent: ORK demo oteller — eksik oda, 90 gun fiyat, havuz/kahvalti/wifi, kampanya katilimi
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;

DECLARE @Today date = CAST(SYSUTCDATETIME() AS date);
DECLARE @KampanyaId bigint = (SELECT TOP (1) [ID] FROM [dbo].[KAMPANYALAR] WHERE [KAMPANYA_KODU] = N'KMP-2026-SEHIR' AND [AKTIF_MI] = 1 ORDER BY [ID]);
DECLARE @KampanyaBas datetime2(0) = COALESCE((SELECT [BASLANGIC_TARIHI] FROM [dbo].[KAMPANYALAR] WHERE [ID] = @KampanyaId), CAST(N'2026-01-01' AS datetime2(0)));
DECLARE @KampanyaBit datetime2(0) = COALESCE((SELECT [BITIS_TARIHI] FROM [dbo].[KAMPANYALAR] WHERE [ID] = @KampanyaId), CAST(N'2035-12-31 23:59:59' AS datetime2(0)));
DECLARE @Yayinda nvarchar(20) = N'Yay' + NCHAR(0x0131) + N'nda';
DECLARE @Onaylandi nvarchar(20) = N'Onayland' + NCHAR(0x0131);

IF OBJECT_ID(N'dbo.OTEL_OZELLIKLERI', N'U') IS NOT NULL
BEGIN
    DECLARE @OzKat bigint = (SELECT TOP (1) [ID] FROM [dbo].[OTEL_OZELLIK_KATEGORILERI] WHERE [AKTIF_MI] = 1 ORDER BY [SIRALAMA], [ID]);
    IF @OzKat IS NOT NULL
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM [dbo].[OTEL_OZELLIKLERI] WHERE [OZELLIK_KODU] = N'HAVUZ')
            INSERT INTO [dbo].[OTEL_OZELLIKLERI]([KATEGORI_ID],[OZELLIK_ADI],[OZELLIK_IKON],[OZELLIK_KODU],[AKTIF_MI],[SIRALAMA],[FILTRELENEBILIR_MI])
            VALUES (@OzKat, N'Havuz', N'fa-water-ladder', N'HAVUZ', 1, 12, 1);
    END;
END;

DECLARE @OzellikKodlari TABLE (Kod nvarchar(80) NOT NULL PRIMARY KEY);
INSERT INTO @OzellikKodlari (Kod) VALUES (N'HAVUZ'), (N'UCRETSIZ_WIFI'), (N'KAHVALTI');

DECLARE @HotelId bigint, @PartnerId bigint, @StdRoomId bigint, @DlxRoomId bigint;
DECLARE @StdFiyat decimal(10,2), @DlxFiyat decimal(10,2), @d int, @Tarih date, @IsWeekend bit;

DECLARE hotel_cursor CURSOR LOCAL FAST_FORWARD FOR
    SELECT o.[ID], o.[PARTNER_ID]
    FROM [dbo].[OTELLER] o
    WHERE o.[OTEL_KODU] LIKE N'ORK-IST-%' OR o.[OTEL_KODU] LIKE N'ORK-SEED-%';

OPEN hotel_cursor;
FETCH NEXT FROM hotel_cursor INTO @HotelId, @PartnerId;

WHILE @@FETCH_STATUS = 0
BEGIN
    UPDATE [dbo].[OTELLER]
    SET [YAYIN_DURUMU] = @Yayinda, [ONAY_DURUMU] = @Onaylandi
    WHERE [ID] = @HotelId;

    IF OBJECT_ID(N'dbo.OTEL_OZELLIK_ILISKILERI', N'U') IS NOT NULL
    BEGIN
        INSERT INTO [dbo].[OTEL_OZELLIK_ILISKILERI]([OTEL_ID],[OZELLIK_ID],[AKTIF_MI])
        SELECT @HotelId, oz.[ID], 1
        FROM [dbo].[OTEL_OZELLIKLERI] oz
        INNER JOIN @OzellikKodlari k ON k.[Kod] = oz.[OZELLIK_KODU]
        WHERE NOT EXISTS (
            SELECT 1 FROM [dbo].[OTEL_OZELLIK_ILISKILERI] i
            WHERE i.[OTEL_ID] = @HotelId AND i.[OZELLIK_ID] = oz.[ID]
        );
    END;

    SET @StdFiyat = CAST(2500 + ((@HotelId % 15) * 200) AS decimal(10,2));
    SET @DlxFiyat = CAST(@StdFiyat * 1.35 AS decimal(10,2));

    SELECT @StdRoomId = [ID] FROM [dbo].[ODA_TIPLERI] WHERE [OTEL_ID] = @HotelId AND [ODA_TIP_KODU] = N'STD-DEMO';
    IF @StdRoomId IS NULL
    BEGIN
        INSERT INTO [dbo].[ODA_TIPLERI]([OTEL_ID],[ODA_TIP_KODU],[ODA_ADI],[ODA_KATEGORISI],[MAKSIMUM_KISI_SAYISI],[MAKSIMUM_YETISKIN_SAYISI],[MAKSIMUM_COCUK_SAYISI],[STANDART_GECELIK_FIYAT],[TOPLAM_ODA_SAYISI],[AKTIF_MI],[SIRALAMA])
        VALUES(@HotelId, N'STD-DEMO', N'Standart Demo', N'Standart Oda', 2, 2, 1, @StdFiyat, 10, 1, 1);
        SET @StdRoomId = SCOPE_IDENTITY();
    END;

    SELECT @DlxRoomId = [ID] FROM [dbo].[ODA_TIPLERI] WHERE [OTEL_ID] = @HotelId AND [ODA_TIP_KODU] = N'DLX-DEMO';
    IF @DlxRoomId IS NULL
    BEGIN
        INSERT INTO [dbo].[ODA_TIPLERI]([OTEL_ID],[ODA_TIP_KODU],[ODA_ADI],[ODA_KATEGORISI],[MAKSIMUM_KISI_SAYISI],[MAKSIMUM_YETISKIN_SAYISI],[MAKSIMUM_COCUK_SAYISI],[STANDART_GECELIK_FIYAT],[TOPLAM_ODA_SAYISI],[AKTIF_MI],[SIRALAMA])
        VALUES(@HotelId, N'DLX-DEMO', N'Deluxe Demo', N'Deluxe Oda', 3, 3, 1, @DlxFiyat, 6, 1, 2);
        SET @DlxRoomId = SCOPE_IDENTITY();
    END;

    SET @d = 0;
    WHILE @d < 90
    BEGIN
        SET @Tarih = DATEADD(DAY, @d, @Today);
        SET @IsWeekend = CASE WHEN DATEPART(WEEKDAY, @Tarih) IN (6, 7) THEN 1 ELSE 0 END;

        IF NOT EXISTS (SELECT 1 FROM [dbo].[ODA_FIYAT_MUSAITLIK] WHERE [OTEL_ID] = @HotelId AND [ODA_TIP_ID] = @StdRoomId AND [TARIH] = @Tarih)
            INSERT INTO [dbo].[ODA_FIYAT_MUSAITLIK]([ODA_TIP_ID],[OTEL_ID],[TARIH],[GECELIK_FIYAT],[INDIRIMLI_FIYAT],[KAMPANYA_ID],[TOPLAM_ODA_SAYISI],[KAPALI_SATIS],[KAMPANYA_ETIKETI])
            VALUES(@StdRoomId, @HotelId, @Tarih, @StdFiyat,
                CASE WHEN @IsWeekend = 1 THEN CAST(@StdFiyat * 0.85 AS decimal(10,2)) ELSE NULL END,
                @KampanyaId, 10, 0, N'SEHIR');

        IF NOT EXISTS (SELECT 1 FROM [dbo].[ODA_FIYAT_MUSAITLIK] WHERE [OTEL_ID] = @HotelId AND [ODA_TIP_ID] = @DlxRoomId AND [TARIH] = @Tarih)
            INSERT INTO [dbo].[ODA_FIYAT_MUSAITLIK]([ODA_TIP_ID],[OTEL_ID],[TARIH],[GECELIK_FIYAT],[INDIRIMLI_FIYAT],[KAMPANYA_ID],[TOPLAM_ODA_SAYISI],[KAPALI_SATIS],[KAMPANYA_ETIKETI])
            VALUES(@DlxRoomId, @HotelId, @Tarih, @DlxFiyat,
                CASE WHEN @IsWeekend = 1 THEN CAST(@DlxFiyat * 0.85 AS decimal(10,2)) ELSE NULL END,
                @KampanyaId, 6, 0, N'SEHIR');

        SET @d += 1;
    END;

    IF @KampanyaId IS NOT NULL AND OBJECT_ID(N'dbo.KAMPANYA_OTELLER', N'U') IS NOT NULL
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM [dbo].[KAMPANYA_OTELLER] WHERE [KAMPANYA_ID] = @KampanyaId AND [OTEL_ID] = @HotelId)
            INSERT INTO [dbo].[KAMPANYA_OTELLER](
                [KAMPANYA_ID],[OTEL_ID],[PARTNER_ID],[KATILIM_DURUMU],[KATILIM_KAYNAGI],
                [BASLANGIC_TARIHI],[BITIS_TARIHI],[ADMIN_ONAY_TARIHI],[PARTNER_ONAY_TARIHI],[OLUSTURULMA_TARIHI]
            )
            VALUES(@KampanyaId, @HotelId, @PartnerId, N'Aktif', N'DemoOdaFiyatSeed', @KampanyaBas, @KampanyaBit, SYSUTCDATETIME(), SYSUTCDATETIME(), SYSUTCDATETIME());
        ELSE
            UPDATE [dbo].[KAMPANYA_OTELLER]
            SET [KATILIM_DURUMU] = N'Aktif',
                [PARTNER_ID] = COALESCE(@PartnerId, [PARTNER_ID]),
                [BASLANGIC_TARIHI] = @KampanyaBas,
                [BITIS_TARIHI] = @KampanyaBit
            WHERE [KAMPANYA_ID] = @KampanyaId AND [OTEL_ID] = @HotelId;
    END;

    FETCH NEXT FROM hotel_cursor INTO @HotelId, @PartnerId;
END;

CLOSE hotel_cursor;
DEALLOCATE hotel_cursor;

UPDATE ko
SET [KATILIM_DURUMU] = N'Aktif'
FROM [dbo].[KAMPANYA_OTELLER] ko
INNER JOIN [dbo].[OTELLER] o ON o.[ID] = ko.[OTEL_ID]
WHERE (o.[OTEL_KODU] LIKE N'ORK-IST-%' OR o.[OTEL_KODU] LIKE N'ORK-SEED-%')
  AND LOWER(REPLACE(LTRIM(RTRIM(ko.[KATILIM_DURUMU])), NCHAR(0x0131), N'i')) IN (N'onaylandi', N'onaylanmis');

DECLARE @Oda int = (SELECT COUNT(*) FROM [dbo].[ODA_TIPLERI] ot JOIN [dbo].[OTELLER] h ON h.[ID]=ot.[OTEL_ID] WHERE h.[OTEL_KODU] LIKE N'ORK-%');
DECLARE @Fiyat int = (SELECT COUNT(*) FROM [dbo].[ODA_FIYAT_MUSAITLIK] ofm JOIN [dbo].[OTELLER] h ON h.[ID]=ofm.[OTEL_ID] WHERE h.[OTEL_KODU] LIKE N'ORK-%');
DECLARE @Ko int = (SELECT COUNT(*) FROM [dbo].[KAMPANYA_OTELLER] ko JOIN [dbo].[OTELLER] h ON h.[ID]=ko.[OTEL_ID] WHERE h.[OTEL_KODU] LIKE N'ORK-%' AND ko.[KATILIM_DURUMU]=N'Aktif');

PRINT N'Demo oda/fiyat/kampanya seed tamam.';
PRINT N'  ORK oda tipi: ' + CAST(@Oda AS nvarchar(12));
PRINT N'  ORK fiyat satiri: ' + CAST(@Fiyat AS nvarchar(12));
PRINT N'  ORK kampanya_otel (Aktif): ' + CAST(@Ko AS nvarchar(12));

-- Eksik DLX-DEMO (eski tek-oda seed kalintisi)
INSERT INTO [dbo].[ODA_TIPLERI]([OTEL_ID],[ODA_TIP_KODU],[ODA_ADI],[ODA_KATEGORISI],[MAKSIMUM_KISI_SAYISI],[MAKSIMUM_YETISKIN_SAYISI],[MAKSIMUM_COCUK_SAYISI],[STANDART_GECELIK_FIYAT],[TOPLAM_ODA_SAYISI],[AKTIF_MI],[SIRALAMA])
SELECT h.[ID], N'DLX-DEMO', N'Deluxe Demo', N'Deluxe Oda', 3, 3, 1, CAST(rs.[STANDART_GECELIK_FIYAT] * 1.35 AS decimal(10,2)), 6, 1, 2
FROM [dbo].[OTELLER] h
INNER JOIN [dbo].[ODA_TIPLERI] rs ON rs.[OTEL_ID] = h.[ID] AND rs.[ODA_TIP_KODU] = N'STD-DEMO'
WHERE (h.[OTEL_KODU] LIKE N'ORK-IST-%' OR h.[OTEL_KODU] LIKE N'ORK-SEED-%')
  AND NOT EXISTS (SELECT 1 FROM [dbo].[ODA_TIPLERI] d WHERE d.[OTEL_ID] = h.[ID] AND d.[ODA_TIP_KODU] = N'DLX-DEMO');
