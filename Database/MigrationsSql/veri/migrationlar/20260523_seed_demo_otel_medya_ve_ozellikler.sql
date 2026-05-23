-- Demo oteller: otel/oda ozellikleri + gorsel kayitlari (dosyalar: tools/DemoImageSeed)
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;

DECLARE @Hotels TABLE (OtelKodu nvarchar(32) PRIMARY KEY, HotelId bigint NULL, RoomId bigint NULL);
INSERT INTO @Hotels (OtelKodu) VALUES
(N'ORK-SEED-001'),(N'ORK-SEED-002'),(N'ORK-SEED-003'),(N'ORK-SEED-004'),(N'ORK-SEED-005'),
(N'ORK-SEED-006'),(N'ORK-SEED-007'),(N'ORK-SEED-008'),(N'ORK-SEED-009'),(N'ORK-SEED-010');

UPDATE h SET
    h.HotelId = o.[ID],
    h.RoomId = r.[ID]
FROM @Hotels h
INNER JOIN [dbo].[OTELLER] o ON o.[OTEL_KODU] = h.OtelKodu
LEFT JOIN [dbo].[ODA_TIPLERI] r ON r.[OTEL_ID] = o.[ID] AND r.[ODA_TIP_KODU] = N'STD-DEMO';

-- Oda ozellikleri (sozluk)
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
END

DECLARE @OtelKodu nvarchar(32), @HotelId bigint, @RoomId bigint;
DECLARE @CoverUrl nvarchar(500), @Url nvarchar(500), @RoomCover nvarchar(500), @RoomUrl2 nvarchar(500), @i int;
DECLARE @OzellikKodlari TABLE (Kod nvarchar(80) NOT NULL);
INSERT INTO @OzellikKodlari VALUES
(N'UCRETSIZ_WIFI'),(N'RESEPSIYON_24_SAAT'),(N'KAHVALTI'),(N'OTOPARK'),(N'RESTORAN'),
(N'FITNESS'),(N'KLIMA'),(N'TV'),(N'ASANSOR');

WHILE EXISTS (SELECT 1 FROM @Hotels WHERE HotelId IS NOT NULL)
BEGIN
    SELECT TOP (1) @OtelKodu = OtelKodu, @HotelId = HotelId, @RoomId = RoomId FROM @Hotels WHERE HotelId IS NOT NULL ORDER BY OtelKodu;

    -- Otel ozellik iliskileri
    IF OBJECT_ID(N'dbo.OTEL_OZELLIK_ILISKILERI', N'U') IS NOT NULL AND OBJECT_ID(N'dbo.OTEL_OZELLIKLERI', N'U') IS NOT NULL
    BEGIN
        INSERT INTO [dbo].[OTEL_OZELLIK_ILISKILERI]([OTEL_ID],[OZELLIK_ID])
        SELECT @HotelId, o.[ID]
        FROM [dbo].[OTEL_OZELLIKLERI] o
        INNER JOIN @OzellikKodlari k ON k.Kod = o.[OZELLIK_KODU]
        WHERE NOT EXISTS (SELECT 1 FROM [dbo].[OTEL_OZELLIK_ILISKILERI] i WHERE i.[OTEL_ID]=@HotelId AND i.[OZELLIK_ID]=o.[ID]);

        IF @OtelKodu IN (N'ORK-SEED-001',N'ORK-SEED-004',N'ORK-SEED-008') -- havuzlu
        BEGIN
            INSERT INTO [dbo].[OTEL_OZELLIK_ILISKILERI]([OTEL_ID],[OZELLIK_ID])
            SELECT @HotelId, o.[ID] FROM [dbo].[OTEL_OZELLIKLERI] o WHERE o.[OZELLIK_KODU]=N'HAVUZ_ACIK'
              AND NOT EXISTS (SELECT 1 FROM [dbo].[OTEL_OZELLIK_ILISKILERI] i WHERE i.[OTEL_ID]=@HotelId AND i.[OZELLIK_ID]=o.[ID]);
        END
        IF @OtelKodu IN (N'ORK-SEED-001',N'ORK-SEED-007',N'ORK-SEED-010')
        BEGIN
            INSERT INTO [dbo].[OTEL_OZELLIK_ILISKILERI]([OTEL_ID],[OZELLIK_ID])
            SELECT @HotelId, o.[ID] FROM [dbo].[OTEL_OZELLIKLERI] o WHERE o.[OZELLIK_KODU]=N'SPA'
              AND NOT EXISTS (SELECT 1 FROM [dbo].[OTEL_OZELLIK_ILISKILERI] i WHERE i.[OTEL_ID]=@HotelId AND i.[OZELLIK_ID]=o.[ID]);
        END
    END

    -- Oda tipi ozellikleri
    IF @RoomId IS NOT NULL AND OBJECT_ID(N'dbo.ODA_TIPI_OZELLIKLERI', N'U') IS NOT NULL
    BEGIN
        INSERT INTO [dbo].[ODA_TIPI_OZELLIKLERI]([ODA_TIP_ID],[OZELLIK_ID],[MIKTAR])
        SELECT @RoomId, oo.[ID], 1
        FROM [dbo].[ODA_OZELLIKLERI] oo
        WHERE oo.[AKTIF_MI]=1 AND oo.[OZELLIK_ADI] IN (N'Klima',N'Minibar',N'LED TV',N'Sac Kurutma Makinesi',N'Kasa')
          AND NOT EXISTS (SELECT 1 FROM [dbo].[ODA_TIPI_OZELLIKLERI] x WHERE x.[ODA_TIP_ID]=@RoomId AND x.[OZELLIK_ID]=oo.[ID]);
    END

    -- Otel gorselleri (demo-cover.webp + demo-01..03)
    IF OBJECT_ID(N'dbo.OTEL_GORSELLERI', N'U') IS NOT NULL
    BEGIN
        SET @CoverUrl = N'/uploads/images/' + CAST(@HotelId AS nvarchar(20)) + N'/hotel/demo-cover.webp';
        IF NOT EXISTS (SELECT 1 FROM [dbo].[OTEL_GORSELLERI] WHERE [OTEL_ID]=@HotelId AND [GORSEL_URL]=@CoverUrl)
        BEGIN
            INSERT INTO [dbo].[OTEL_GORSELLERI]([OTEL_ID],[GORSEL_URL],[GORSEL_TURU],[BASLIK],[KAPAK_FOTOGRAFI_MI],[ONE_CIKAN],[SIRALAMA],[ONAY_DURUMU])
            VALUES(@HotelId,@CoverUrl,N'Genel Alan',N'Kapak',1,1,0,N'Onaylandı');
            UPDATE [dbo].[OTELLER] SET [KAPAK_FOTOGRAFI]=@CoverUrl WHERE [ID]=@HotelId;
        END

        SET @i = 1;
        WHILE @i <= 3
        BEGIN
            SET @Url = N'/uploads/images/' + CAST(@HotelId AS nvarchar(20)) + N'/hotel/demo-' + RIGHT(N'0'+CAST(@i AS nvarchar(2)),2) + N'.webp';
            IF NOT EXISTS (SELECT 1 FROM [dbo].[OTEL_GORSELLERI] WHERE [OTEL_ID]=@HotelId AND [GORSEL_URL]=@Url)
                INSERT INTO [dbo].[OTEL_GORSELLERI]([OTEL_ID],[GORSEL_URL],[GORSEL_TURU],[BASLIK],[KAPAK_FOTOGRAFI_MI],[SIRALAMA],[ONAY_DURUMU])
                VALUES(@HotelId,@Url,N'Genel Alan',CONCAT(N'Galeri ',@i),0,@i,N'Onaylandı');
            SET @i += 1;
        END
    END

    -- Oda gorselleri
    IF @RoomId IS NOT NULL AND OBJECT_ID(N'dbo.ODA_GORSELLERI', N'U') IS NOT NULL
    BEGIN
        SET @RoomCover = N'/uploads/images/' + CAST(@HotelId AS nvarchar(20)) + N'/rooms/' + CAST(@RoomId AS nvarchar(20)) + N'/demo-room-cover.webp';
        IF NOT EXISTS (SELECT 1 FROM [dbo].[ODA_GORSELLERI] WHERE [ODA_TIP_ID]=@RoomId AND [GORSEL_URL]=@RoomCover)
            INSERT INTO [dbo].[ODA_GORSELLERI]([ODA_TIP_ID],[GORSEL_URL],[BASLIK],[KAPAK_FOTOGRAFI_MI],[SIRALAMA],[ONAY_DURUMU])
            VALUES(@RoomId,@RoomCover,N'Oda kapak',1,0,N'Onaylandı');

        SET @RoomUrl2 = N'/uploads/images/' + CAST(@HotelId AS nvarchar(20)) + N'/rooms/' + CAST(@RoomId AS nvarchar(20)) + N'/demo-room-02.webp';
        IF NOT EXISTS (SELECT 1 FROM [dbo].[ODA_GORSELLERI] WHERE [ODA_TIP_ID]=@RoomId AND [GORSEL_URL]=@RoomUrl2)
            INSERT INTO [dbo].[ODA_GORSELLERI]([ODA_TIP_ID],[GORSEL_URL],[BASLIK],[KAPAK_FOTOGRAFI_MI],[SIRALAMA],[ONAY_DURUMU])
            VALUES(@RoomId,@RoomUrl2,N'Oda detay',0,1,N'Onaylandı');
    END

    DELETE FROM @Hotels WHERE OtelKodu = @OtelKodu;
END

PRINT N'Demo otel medya ve ozellik seed tamam.';
