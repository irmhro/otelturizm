SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET NUMERIC_ROUNDABORT OFF;

DECLARE @HotelId bigint;
DECLARE @AdminUserId bigint = 32;
DECLARE @Today date = CONVERT(date, GETDATE());

SELECT @HotelId = id
FROM dbo.oteller
WHERE otel_kodu = N'DEMO-OTEL-001'
   OR otel_adi = N'Demo Test Otel';

IF @HotelId IS NULL
BEGIN
    THROW 51000, 'Demo Test Otel bulunamadı.', 1;
END;

IF OBJECT_ID(N'dbo.CodexBackup_DemoTestOtel_20260502_oda_tipleri', N'U') IS NULL
    SELECT SYSUTCDATETIME() AS backup_utc, *
    INTO dbo.CodexBackup_DemoTestOtel_20260502_oda_tipleri
    FROM dbo.oda_tipleri
    WHERE otel_id = @HotelId;

IF OBJECT_ID(N'dbo.CodexBackup_DemoTestOtel_20260502_oda_fiyat_musaitlik', N'U') IS NULL
    SELECT SYSUTCDATETIME() AS backup_utc, fm.*
    INTO dbo.CodexBackup_DemoTestOtel_20260502_oda_fiyat_musaitlik
    FROM dbo.oda_fiyat_musaitlik fm
    JOIN dbo.oda_tipleri ot ON ot.id = fm.oda_tip_id
    WHERE ot.otel_id = @HotelId;

IF OBJECT_ID(N'dbo.CodexBackup_DemoTestOtel_20260502_oda_gorselleri', N'U') IS NULL
    SELECT SYSUTCDATETIME() AS backup_utc, og.*
    INTO dbo.CodexBackup_DemoTestOtel_20260502_oda_gorselleri
    FROM dbo.oda_gorselleri og
    JOIN dbo.oda_tipleri ot ON ot.id = og.oda_tip_id
    WHERE ot.otel_id = @HotelId;

IF OBJECT_ID(N'dbo.CodexBackup_DemoTestOtel_20260502_oda_tipi_ozellikleri', N'U') IS NULL
    SELECT SYSUTCDATETIME() AS backup_utc, oto.*
    INTO dbo.CodexBackup_DemoTestOtel_20260502_oda_tipi_ozellikleri
    FROM dbo.oda_tipi_ozellikleri oto
    JOIN dbo.oda_tipleri ot ON ot.id = oto.oda_tip_id
    WHERE ot.otel_id = @HotelId;

BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @Rooms TABLE
    (
        code nvarchar(50) NOT NULL,
        name nvarchar(150) NOT NULL,
        category nvarchar(50) NOT NULL,
        max_guest tinyint NOT NULL,
        max_adult tinyint NOT NULL,
        max_child tinyint NOT NULL,
        bed nvarchar(100) NOT NULL,
        bed_count tinyint NOT NULL,
        sqm smallint NOT NULL,
        balcony bit NOT NULL,
        view_type nvarchar(100) NOT NULL,
        bath nvarchar(100) NOT NULL,
        price decimal(18,2) NOT NULL,
        total_count smallint NOT NULL,
        sort_order smallint NOT NULL,
        img_prefix nvarchar(50) NOT NULL
    );

    INSERT INTO @Rooms
    VALUES
        (N'DEMO-DLX', N'Demo Deluxe Oda', N'Deluxe', 3, 2, 1, N'King Bed', 1, 36, 1, N'Deniz Manzaralı', N'Duş', 5600.00, 8, 2, N'demo-deluxe-room'),
        (N'DEMO-FAM', N'Demo Aile Suiti', N'Suit', 5, 4, 2, N'King Bed + İki Tek Yatak', 3, 52, 1, N'Şehir ve Deniz Manzaralı', N'Duş + Küvet', 7200.00, 6, 3, N'demo-family-suite');

    INSERT INTO dbo.oda_tipleri
    (
        otel_id, oda_tip_kodu, oda_adi, oda_kategorisi,
        maksimum_kisi_sayisi, maksimum_yetiskin_sayisi, maksimum_cocuk_sayisi,
        yatak_tipi, yatak_sayisi, ek_yatak_eklenebilir_mi, oda_metrekare,
        balkon_var_mi, manzara_tipi, ozel_banyo_var_mi, banyo_tipi,
        standart_gecelik_fiyat, toplam_oda_sayisi, aktif_mi, siralama,
        olusturulma_tarihi, guncellenme_tarihi
    )
    SELECT
        @HotelId, r.code, r.name, r.category,
        r.max_guest, r.max_adult, r.max_child,
        r.bed, r.bed_count, 1, r.sqm,
        r.balcony, r.view_type, 1, r.bath,
        r.price, r.total_count, 1, r.sort_order,
        SYSUTCDATETIME(), SYSUTCDATETIME()
    FROM @Rooms r
    WHERE NOT EXISTS (
        SELECT 1
        FROM dbo.oda_tipleri ot
        WHERE ot.otel_id = @HotelId
          AND ot.oda_tip_kodu = r.code
    );

    DECLARE @RoomIds TABLE
    (
        room_id bigint NOT NULL,
        code nvarchar(50) NOT NULL,
        name nvarchar(150) NOT NULL,
        price decimal(18,2) NOT NULL,
        total_count smallint NOT NULL,
        img_prefix nvarchar(50) NOT NULL
    );

    INSERT INTO @RoomIds
    SELECT ot.id, r.code, r.name, r.price, r.total_count, r.img_prefix
    FROM dbo.oda_tipleri ot
    JOIN @Rooms r ON r.code = ot.oda_tip_kodu
    WHERE ot.otel_id = @HotelId;

    ;WITH feature_seed AS
    (
        SELECT room_id, feature_name
        FROM @RoomIds
        CROSS APPLY (VALUES
            (N'Deniz Manzaralı'),
            (N'Şehir Manzaralı'),
            (N'Balkon'),
            (N'Oturma Alanı'),
            (N'Çalışma Masası'),
            (N'Bedelsiz Banyo Malzemeleri'),
            (N'Elektrikli Su Isıtıcısı'),
            (N'Yastık Menüsü')
        ) f(feature_name)
    ),
    matched AS
    (
        SELECT fs.room_id, ro.id AS feature_id, ro.kategori_id
        FROM feature_seed fs
        JOIN dbo.oda_ozellikleri ro
          ON ro.aktif_mi = 1
         AND ro.ozellik_adi = fs.feature_name
    )
    INSERT INTO dbo.oda_tipi_ozellikleri (oda_tip_id, ozellik_id, miktar, otel_id, kategori_id)
    SELECT m.room_id, m.feature_id, 1, @HotelId, m.kategori_id
    FROM matched m
    WHERE NOT EXISTS (
        SELECT 1
        FROM dbo.oda_tipi_ozellikleri x
        WHERE x.oda_tip_id = m.room_id
          AND x.ozellik_id = m.feature_id
    );

    ;WITH matched AS
    (
        SELECT oto.oda_tip_id, oto.ozellik_id, COALESCE(oto.kategori_id, ro.kategori_id) AS kategori_id
        FROM dbo.oda_tipi_ozellikleri oto
        JOIN @RoomIds r ON r.room_id = oto.oda_tip_id
        JOIN dbo.oda_ozellikleri ro ON ro.id = oto.ozellik_id
    )
    INSERT INTO dbo.oda_ozellik_iliskileri
    (otel_id, oda_id, kategori_id, ozellik_id, miktar, aktif_mi, olusturulma_tarihi, guncellenme_tarihi)
    SELECT @HotelId, m.oda_tip_id, COALESCE(m.kategori_id, 1), m.ozellik_id, 1, 1, SYSUTCDATETIME(), SYSUTCDATETIME()
    FROM matched m
    WHERE NOT EXISTS (
        SELECT 1
        FROM dbo.oda_ozellik_iliskileri x
        WHERE x.otel_id = @HotelId
          AND x.oda_id = m.oda_tip_id
          AND x.ozellik_id = m.ozellik_id
    );

    ;WITH n AS
    (
        SELECT TOP (30) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) - 1 AS i
        FROM sys.all_objects
    )
    INSERT INTO dbo.oda_fiyat_musaitlik
    (
        oda_tip_id, otel_id, tarih, gecelik_fiyat, indirimli_fiyat,
        toplam_oda_sayisi, satilan_oda_sayisi, bloke_oda_sayisi,
        minimum_geceleme, maksimum_geceleme, kapali_satis, fiyat_notu, guncellenme_tarihi
    )
    SELECT
        r.room_id, @HotelId, DATEADD(DAY, n.i, @Today),
        r.price,
        CASE
            WHEN r.code = N'DEMO-DLX' AND n.i % 7 IN (5, 6) THEN r.price - 450
            WHEN r.code = N'DEMO-FAM' AND n.i % 7 IN (5, 6) THEN r.price - 650
            ELSE NULL
        END,
        r.total_count, 0, 0, 1, 30, 0, N'Codex demo 1 aylık fiyat', SYSUTCDATETIME()
    FROM @RoomIds r
    CROSS JOIN n
    WHERE NOT EXISTS (
        SELECT 1
        FROM dbo.oda_fiyat_musaitlik fm
        WHERE fm.oda_tip_id = r.room_id
          AND fm.tarih = DATEADD(DAY, n.i, @Today)
    );

    ;WITH n AS
    (
        SELECT TOP (8) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS i
        FROM sys.all_objects
    )
    INSERT INTO dbo.oda_gorselleri
    (oda_tip_id, gorsel_url, baslik, aciklama, kapak_fotografi_mi, siralama, onay_durumu, onaylayan_admin_id, onay_tarihi, yukleyen_kullanici_id, olusturulma_tarihi)
    SELECT
        r.room_id,
        N'/uploads/images/' + CONVERT(nvarchar(20), @HotelId) + N'/rooms/' + CONVERT(nvarchar(20), r.room_id) + N'/' + r.img_prefix + N'-' + RIGHT(N'00' + CONVERT(nvarchar(2), n.i), 2) + N'.webp',
        r.name + N' görseli ' + CONVERT(nvarchar(2), n.i),
        N'Demo oda görseli.',
        CASE WHEN n.i = 1 THEN 1 ELSE 0 END,
        n.i,
        N'Onaylandı',
        @AdminUserId,
        SYSUTCDATETIME(),
        @AdminUserId,
        SYSUTCDATETIME()
    FROM @RoomIds r
    CROSS JOIN n
    WHERE NOT EXISTS (
        SELECT 1
        FROM dbo.oda_gorselleri og
        WHERE og.oda_tip_id = r.room_id
          AND og.siralama = n.i
          AND og.gorsel_url LIKE N'/uploads/images/' + CONVERT(nvarchar(20), @HotelId) + N'/rooms/' + CONVERT(nvarchar(20), r.room_id) + N'/%'
    );

    UPDATE ot
    SET kapak_fotografi = N'/uploads/images/' + CONVERT(nvarchar(20), @HotelId) + N'/rooms/' + CONVERT(nvarchar(20), ot.id) + N'/' + r.img_prefix + N'-01.webp',
        guncellenme_tarihi = SYSUTCDATETIME()
    FROM dbo.oda_tipleri ot
    JOIN @RoomIds r ON r.room_id = ot.id
    WHERE NULLIF(ot.kapak_fotografi, N'') IS NULL
       OR ot.kapak_fotografi NOT LIKE N'/uploads/images/' + CONVERT(nvarchar(20), @HotelId) + N'/rooms/' + CONVERT(nvarchar(20), ot.id) + N'/%';

    UPDATE h
    SET toplam_oda_sayisi = x.room_count,
        toplam_yatak_kapasitesi = x.bed_capacity,
        guncellenme_tarihi = SYSUTCDATETIME()
    FROM dbo.oteller h
    CROSS APPLY
    (
        SELECT
            SUM(CONVERT(int, toplam_oda_sayisi)) AS room_count,
            SUM(CONVERT(int, toplam_oda_sayisi) * CONVERT(int, maksimum_kisi_sayisi)) AS bed_capacity
        FROM dbo.oda_tipleri
        WHERE otel_id = @HotelId
          AND aktif_mi = 1
    ) x
    WHERE h.id = @HotelId;

    COMMIT TRANSACTION;

    SELECT
        r.room_id,
        r.code,
        r.name,
        (SELECT COUNT(*) FROM dbo.oda_gorselleri og WHERE og.oda_tip_id = r.room_id) AS gorsel_sayisi,
        (SELECT COUNT(*) FROM dbo.oda_tipi_ozellikleri oto WHERE oto.oda_tip_id = r.room_id) AS ozellik_sayisi,
        (SELECT COUNT(*) FROM dbo.oda_fiyat_musaitlik fm WHERE fm.oda_tip_id = r.room_id AND fm.tarih >= @Today AND fm.tarih < DATEADD(DAY, 30, @Today)) AS fiyat_gunu
    FROM @RoomIds r
    ORDER BY r.code;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
