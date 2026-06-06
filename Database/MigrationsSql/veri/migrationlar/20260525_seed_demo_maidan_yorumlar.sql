-- Idempotent: DEMO-MAIDAN-2026 onayli demo yorumlari (otel detay vitrini)
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

DECLARE @HotelId bigint = (SELECT TOP (1) [ID] FROM [dbo].[OTELLER] WHERE [OTEL_KODU] = N'DEMO-MAIDAN-2026' ORDER BY [ID]);
DECLARE @GuestUserId bigint = (SELECT TOP (1) [ID] FROM [dbo].[KULLANICILAR] WHERE [EPOSTA] = N'ork-demo-misafir@otelturizm.local' ORDER BY [ID]);

IF @HotelId IS NULL
BEGIN
    RAISERROR(N'DEMO-MAIDAN-2026 bulunamadi; once 20260525_seed_demo_maidan_otel_tam.sql uygulayin.', 16, 1);
    RETURN;
END;

IF @GuestUserId IS NULL
BEGIN
    INSERT INTO [dbo].[KULLANICILAR]([AD_SOYAD],[EPOSTA],[TELEFON],[SIFRE],[ROL],[HESAP_DURUMU],[KAYIT_KAYNAGI],[OLUSTURULMA_TARIHI])
    VALUES (N'Demo Misafir Maidan', N'ork-demo-misafir@otelturizm.local', N'5000000200',
        LOWER(CONVERT(VARCHAR(64), HASHBYTES('SHA2_256', N'Demo123!'), 2)), N'user', 1, N'MaidanReviewSeed', SYSUTCDATETIME());
    SET @GuestUserId = SCOPE_IDENTITY();
END;

IF OBJECT_ID(N'dbo.YORUMLAR', N'U') IS NULL
BEGIN
    PRINT N'YORUMLAR tablosu yok; atlandi.';
    RETURN;
END;

DECLARE @Reviews TABLE (
    Baslik nvarchar(200) NOT NULL,
    Metin nvarchar(max) NOT NULL,
    Genel10 tinyint NOT NULL,
    Konum10 tinyint NOT NULL,
    Temizlik10 tinyint NOT NULL,
    Personel10 tinyint NOT NULL,
    Fiyat10 tinyint NOT NULL,
    Profil nvarchar(40) NOT NULL
);

INSERT INTO @Reviews VALUES
(N'Mukemmel konum', N'Taksim''e yurume mesafesinde, superior odamiz sessiz ve ferahdi. Kahvalti zengin, personel cok ilgili.', 9, 10, 9, 9, 8, N'Cift'),
(N'Is seyahati icin ideal', N'Wi-Fi hizli, check-in hizli gecti. Deluxe odada calisma masasi genis, metro baglantisi yakin.', 9, 9, 9, 8, 8, N'Is'),
(N'Aile tatili', N'Cocuklu ailemiz icin guvenli ve temiz bir tesis. Oda genis, yatak konforu cok iyi.', 8, 8, 9, 9, 8, N'Aile'),
(N'Fiyat performans', N'Erken rezervasyon indirimi ile cok uygun kaldik. Otel fotograflari gercegi yansitiyor.', 8, 9, 8, 8, 9, N'Cift'),
(N'Sessiz oda', N'Caddeye bakan taraf yerine sessiz oda talep ettik, hemen cozduler. Uyku kalitemiz harikaydi.', 9, 8, 9, 9, 8, N'Cift');

DECLARE @Baslik nvarchar(200), @Metin nvarchar(max), @G10 tinyint, @K10 tinyint, @T10 tinyint, @P10 tinyint, @F10 tinyint, @Profil nvarchar(40);
DECLARE @DaysAgo int = 3;

WHILE EXISTS (SELECT 1 FROM @Reviews)
BEGIN
    SELECT TOP (1)
        @Baslik = Baslik, @Metin = Metin, @G10 = Genel10, @K10 = Konum10, @T10 = Temizlik10,
        @P10 = Personel10, @F10 = Fiyat10, @Profil = Profil
    FROM @Reviews ORDER BY Baslik;

    IF NOT EXISTS (SELECT 1 FROM [dbo].[YORUMLAR] WHERE [OTEL_ID] = @HotelId AND [YORUM_BASLIGI] = @Baslik)
    BEGIN
        INSERT INTO [dbo].[YORUMLAR](
            [OTEL_ID],[KULLANICI_ID],[GENEL_PUAN],[TEMIZLIK_PUANI],[KONFOR_PUANI],[KONUM_PUANI],[PERSONEL_PUANI],[FIYAT_PERFORMANS_PUANI],
            [YORUM_BASLIGI],[YORUM_METNI],[KONAKLAMA_TARIHI],[KONAKLAMA_TURU],[DOGRULANMIS_KONAKLAMA],[ONAY_DURUMU],[ONAY_TARIHI],
            [SEYAHAT_PROFILI],[MEMNUNIYET_SEVIYESI],[GENEL_PUAN_10],[PUAN_KONUM_10],[PUAN_TEMIZLIK_10],[PUAN_PERSONEL_10],[PUAN_FIYAT_10],
            [OLUSTURULMA_TARIHI]
        )
        VALUES(
            @HotelId, @GuestUserId,
            CASE WHEN @G10 >= 9 THEN 5 WHEN @G10 >= 7 THEN 4 ELSE 3 END,
            CASE WHEN @T10 >= 9 THEN 5 WHEN @T10 >= 7 THEN 4 ELSE 3 END,
            CASE WHEN @T10 >= 9 THEN 5 WHEN @T10 >= 7 THEN 4 ELSE 3 END,
            CASE WHEN @K10 >= 9 THEN 5 WHEN @K10 >= 7 THEN 4 ELSE 3 END,
            CASE WHEN @P10 >= 9 THEN 5 WHEN @P10 >= 7 THEN 4 ELSE 3 END,
            CASE WHEN @F10 >= 9 THEN 5 WHEN @F10 >= 7 THEN 4 ELSE 3 END,
            @Baslik, @Metin, DATEADD(DAY, -@DaysAgo, CAST(SYSUTCDATETIME() AS date)), N'Demo', 1, N'Onaylandı', SYSUTCDATETIME(),
            @Profil, CASE WHEN @G10 >= 9 THEN 5 WHEN @G10 >= 7 THEN 4 ELSE 3 END,
            @G10, @K10, @T10, @P10, @F10,
            DATEADD(DAY, -@DaysAgo, SYSUTCDATETIME())
        );
    END;

    SET @DaysAgo += 4;
    DELETE FROM @Reviews WHERE Baslik = @Baslik;
END;

UPDATE [dbo].[OTELLER]
SET [TOPLAM_YORUM_SAYISI] = (SELECT COUNT(*) FROM [dbo].[YORUMLAR] WHERE [OTEL_ID] = @HotelId AND [ONAY_DURUMU] LIKE N'Onaylan%'),
    [ORTALAMA_PUAN] = COALESCE((
        SELECT AVG(CAST(COALESCE([GENEL_PUAN_10], [GENEL_PUAN] * 2) AS decimal(9,2)))
        FROM [dbo].[YORUMLAR] WHERE [OTEL_ID] = @HotelId AND [ONAY_DURUMU] LIKE N'Onaylan%'
    ), [ORTALAMA_PUAN])
WHERE [ID] = @HotelId;

IF OBJECT_ID(N'dbo.OTEL_KOSULLARI', N'U') IS NOT NULL
BEGIN
    UPDATE [dbo].[OTEL_KOSULLARI]
    SET [SIGARA_POLITIKASI] = N'Tum kapali alanlarda sigara icilmez.',
        [EVCIL_HAYVAN_POLITIKASI] = N'15 kg alti evcil hayvan kabul edilir (ucretli).',
        [COCUK_KABUL_YAS_ARALIGI] = N'0-12 yas'
    WHERE [OTEL_ID] = @HotelId;
END;

PRINT N'Maidan demo yorum seed tamam. OTEL_ID=' + CAST(@HotelId AS nvarchar(20));
