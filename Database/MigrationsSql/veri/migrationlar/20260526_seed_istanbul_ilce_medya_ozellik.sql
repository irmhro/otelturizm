-- Istanbul ilce demo otelleri: medya, oda ozellikleri, hafta sonu indirim kaydi
-- Hedef: ORK-IST-* ve ORK-SEED-* (20260526_seed_istanbul_ilce_oteller_tam.sql sonrasi)
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET DATEFIRST 1;

DECLARE @WeekendIndirimId bigint;

IF OBJECT_ID(N'dbo.FIYAT_INDIRIMLERI', N'U') IS NOT NULL
BEGIN
    SELECT @WeekendIndirimId = [ID] FROM [dbo].[FIYAT_INDIRIMLERI] WHERE [INDIRIM_ADI] = N'Hafta Sonu Demo';

    IF @WeekendIndirimId IS NULL
    BEGIN
        INSERT INTO [dbo].[FIYAT_INDIRIMLERI]([INDIRIM_ADI],[KISA_ACIKLAMA],[IKON_CLASS],[RENK_KODU],[AKTIF_MI],[SIRALAMA])
        VALUES (N'Hafta Sonu Demo', N'Cumartesi-Pazar %15 indirim', N'fa-calendar-week', N'#0F766E', 1, 10);
        SET @WeekendIndirimId = SCOPE_IDENTITY();
    END;
END;

IF OBJECT_ID(N'dbo.ODA_OZELLIKLERI', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM [dbo].[ODA_OZELLIKLERI] WHERE [OZELLIK_ADI] = N'Klima')
        INSERT INTO [dbo].[ODA_OZELLIKLERI]([KATEGORI],[OZELLIK_ADI],[OZELLIK_IKON],[SIRALAMA],[AKTIF_MI]) VALUES (N'Konfor',N'Klima',N'fa-fan',10,1);
    IF NOT EXISTS (SELECT 1 FROM [dbo].[ODA_OZELLIKLERI] WHERE [OZELLIK_ADI] = N'Minibar')
        INSERT INTO [dbo].[ODA_OZELLIKLERI]([KATEGORI],[OZELLIK_ADI],[OZELLIK_IKON],[SIRALAMA],[AKTIF_MI]) VALUES (N'Konfor',N'Minibar',N'fa-wine-bottle',20,1);
    IF NOT EXISTS (SELECT 1 FROM [dbo].[ODA_OZELLIKLERI] WHERE [OZELLIK_ADI] = N'LED TV')
        INSERT INTO [dbo].[ODA_OZELLIKLERI]([KATEGORI],[OZELLIK_ADI],[OZELLIK_IKON],[SIRALAMA],[AKTIF_MI]) VALUES (N'Teknoloji',N'LED TV',N'fa-tv',30,1);
    IF NOT EXISTS (SELECT 1 FROM [dbo].[ODA_OZELLIKLERI] WHERE [OZELLIK_ADI] = N'Sac Kurutma Makinesi')
        INSERT INTO [dbo].[ODA_OZELLIKLERI]([KATEGORI],[OZELLIK_ADI],[OZELLIK_IKON],[SIRALAMA],[AKTIF_MI]) VALUES (N'Banyo',N'Sac Kurutma Makinesi',N'fa-wind',40,1);
    IF NOT EXISTS (SELECT 1 FROM [dbo].[ODA_OZELLIKLERI] WHERE [OZELLIK_ADI] = N'Kasa')
        INSERT INTO [dbo].[ODA_OZELLIKLERI]([KATEGORI],[OZELLIK_ADI],[OZELLIK_IKON],[SIRALAMA],[AKTIF_MI]) VALUES (N'Guvenlik',N'Kasa',N'fa-vault',50,1);
END;

DECLARE @Hotels TABLE (
    OtelKodu nvarchar(32) NOT NULL PRIMARY KEY,
    HotelId bigint NULL,
    StdRoomId bigint NULL,
    DlxRoomId bigint NULL
);

INSERT INTO @Hotels (OtelKodu)
SELECT o.[OTEL_KODU]
FROM [dbo].[OTELLER] o
WHERE o.[OTEL_KODU] LIKE N'ORK-IST-%' OR o.[OTEL_KODU] LIKE N'ORK-SEED-%';

UPDATE h SET
    h.HotelId = o.[ID],
    h.StdRoomId = rs.[ID],
    h.DlxRoomId = rd.[ID]
FROM @Hotels h
INNER JOIN [dbo].[OTELLER] o ON o.[OTEL_KODU] = h.OtelKodu
LEFT JOIN [dbo].[ODA_TIPLERI] rs ON rs.[OTEL_ID] = o.[ID] AND rs.[ODA_TIP_KODU] = N'STD-DEMO'
LEFT JOIN [dbo].[ODA_TIPLERI] rd ON rd.[OTEL_ID] = o.[ID] AND rd.[ODA_TIP_KODU] = N'DLX-DEMO';

DECLARE @OtelKodu nvarchar(32), @HotelId bigint, @StdRoomId bigint, @DlxRoomId bigint, @RoomId bigint;
DECLARE @CoverUrl nvarchar(500), @Url nvarchar(500), @RoomCover nvarchar(500), @RoomUrl2 nvarchar(500), @i int;
DECLARE @RoomIds TABLE (RoomId bigint NOT NULL PRIMARY KEY);

WHILE EXISTS (SELECT 1 FROM @Hotels WHERE HotelId IS NOT NULL)
BEGIN
    SELECT TOP (1)
        @OtelKodu = OtelKodu,
        @HotelId = HotelId,
        @StdRoomId = StdRoomId,
        @DlxRoomId = DlxRoomId
    FROM @Hotels
    WHERE HotelId IS NOT NULL
    ORDER BY OtelKodu;

    IF @WeekendIndirimId IS NOT NULL AND OBJECT_ID(N'dbo.ODA_FIYAT_MUSAITLIK', N'U') IS NOT NULL
    BEGIN
        UPDATE m
        SET m.[INDIRIM_ID] = @WeekendIndirimId
        FROM [dbo].[ODA_FIYAT_MUSAITLIK] m
        WHERE m.[OTEL_ID] = @HotelId
          AND m.[INDIRIMLI_FIYAT] IS NOT NULL
          AND (m.[INDIRIM_ID] IS NULL OR m.[INDIRIM_ID] <> @WeekendIndirimId);
    END;

    DELETE FROM @RoomIds;
    IF @StdRoomId IS NOT NULL INSERT INTO @RoomIds VALUES (@StdRoomId);
    IF @DlxRoomId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM @RoomIds WHERE RoomId = @DlxRoomId) INSERT INTO @RoomIds VALUES (@DlxRoomId);

    WHILE EXISTS (SELECT 1 FROM @RoomIds)
    BEGIN
        SELECT TOP (1) @RoomId = RoomId FROM @RoomIds ORDER BY RoomId;

        IF OBJECT_ID(N'dbo.ODA_TIPI_OZELLIKLERI', N'U') IS NOT NULL AND OBJECT_ID(N'dbo.ODA_OZELLIKLERI', N'U') IS NOT NULL
        BEGIN
            INSERT INTO [dbo].[ODA_TIPI_OZELLIKLERI]([ODA_TIP_ID],[OZELLIK_ID],[MIKTAR])
            SELECT @RoomId, oo.[ID], 1
            FROM [dbo].[ODA_OZELLIKLERI] oo
            WHERE oo.[AKTIF_MI] = 1 AND oo.[OZELLIK_ADI] IN (N'Klima', N'Minibar', N'LED TV', N'Sac Kurutma Makinesi', N'Kasa')
              AND NOT EXISTS (SELECT 1 FROM [dbo].[ODA_TIPI_OZELLIKLERI] x WHERE x.[ODA_TIP_ID] = @RoomId AND x.[OZELLIK_ID] = oo.[ID]);
        END;

        IF OBJECT_ID(N'dbo.ODA_GORSELLERI', N'U') IS NOT NULL
        BEGIN
            SET @RoomCover = N'/uploads/images/' + CAST(@HotelId AS nvarchar(20)) + N'/rooms/' + CAST(@RoomId AS nvarchar(20)) + N'/demo-room-cover.webp';
            IF NOT EXISTS (SELECT 1 FROM [dbo].[ODA_GORSELLERI] WHERE [ODA_TIP_ID] = @RoomId AND [GORSEL_URL] = @RoomCover)
                INSERT INTO [dbo].[ODA_GORSELLERI]([ODA_TIP_ID],[GORSEL_URL],[BASLIK],[KAPAK_FOTOGRAFI_MI],[SIRALAMA],[ONAY_DURUMU])
                VALUES(@RoomId, @RoomCover, N'Oda kapak', 1, 0, N'Onaylandı');

            SET @RoomUrl2 = N'/uploads/images/' + CAST(@HotelId AS nvarchar(20)) + N'/rooms/' + CAST(@RoomId AS nvarchar(20)) + N'/demo-room-02.webp';
            IF NOT EXISTS (SELECT 1 FROM [dbo].[ODA_GORSELLERI] WHERE [ODA_TIP_ID] = @RoomId AND [GORSEL_URL] = @RoomUrl2)
                INSERT INTO [dbo].[ODA_GORSELLERI]([ODA_TIP_ID],[GORSEL_URL],[BASLIK],[KAPAK_FOTOGRAFI_MI],[SIRALAMA],[ONAY_DURUMU])
                VALUES(@RoomId, @RoomUrl2, N'Oda detay', 0, 1, N'Onaylandı');
        END;

        DELETE FROM @RoomIds WHERE RoomId = @RoomId;
    END;

    IF OBJECT_ID(N'dbo.OTEL_GORSELLERI', N'U') IS NOT NULL
    BEGIN
        SET @CoverUrl = N'/uploads/images/' + CAST(@HotelId AS nvarchar(20)) + N'/hotel/demo-cover.webp';
        IF NOT EXISTS (SELECT 1 FROM [dbo].[OTEL_GORSELLERI] WHERE [OTEL_ID] = @HotelId AND [GORSEL_URL] = @CoverUrl)
        BEGIN
            INSERT INTO [dbo].[OTEL_GORSELLERI]([OTEL_ID],[GORSEL_URL],[GORSEL_TURU],[BASLIK],[KAPAK_FOTOGRAFI_MI],[ONE_CIKAN],[SIRALAMA],[ONAY_DURUMU])
            VALUES(@HotelId, @CoverUrl, N'Genel Alan', N'Kapak', 1, 1, 0, N'Onaylandı');
            UPDATE [dbo].[OTELLER] SET [KAPAK_FOTOGRAFI] = @CoverUrl WHERE [ID] = @HotelId;
        END;

        SET @i = 1;
        WHILE @i <= 3
        BEGIN
            SET @Url = N'/uploads/images/' + CAST(@HotelId AS nvarchar(20)) + N'/hotel/demo-' + RIGHT(N'0' + CAST(@i AS nvarchar(2)), 2) + N'.webp';
            IF NOT EXISTS (SELECT 1 FROM [dbo].[OTEL_GORSELLERI] WHERE [OTEL_ID] = @HotelId AND [GORSEL_URL] = @Url)
                INSERT INTO [dbo].[OTEL_GORSELLERI]([OTEL_ID],[GORSEL_URL],[GORSEL_TURU],[BASLIK],[KAPAK_FOTOGRAFI_MI],[SIRALAMA],[ONAY_DURUMU])
                VALUES(@HotelId, @Url, N'Genel Alan', CONCAT(N'Galeri ', @i), 0, @i, N'Onaylandı');
            SET @i += 1;
        END;
    END;

    DELETE FROM @Hotels WHERE OtelKodu = @OtelKodu;
END;

DECLARE @GorselCount int = (
    SELECT COUNT(*)
    FROM [dbo].[OTEL_GORSELLERI] g
    INNER JOIN [dbo].[OTELLER] o ON o.[ID] = g.[OTEL_ID]
    WHERE o.[OTEL_KODU] LIKE N'ORK-IST-%' OR o.[OTEL_KODU] LIKE N'ORK-SEED-%'
);

PRINT N'Istanbul ilce medya/ozellik seed tamam. OTEL_GORSELLERI (ORK-IST/SEED): ' + CAST(@GorselCount AS nvarchar(12));
