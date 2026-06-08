-- Idempotent: DEMO-ANTALYA-2026 + DEMO-KAPADOKYA-2026 demo yorumlari
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

DECLARE @GuestUserId bigint = (SELECT TOP (1) [ID] FROM [dbo].[KULLANICILAR] WHERE [EPOSTA]=N'ork-demo-misafir@otelturizm.local');
IF @GuestUserId IS NULL BEGIN
    INSERT INTO [dbo].[KULLANICILAR]([AD_SOYAD],[EPOSTA],[TELEFON],[SIFRE],[ROL],[HESAP_DURUMU],[KAYIT_KAYNAGI],[OLUSTURULMA_TARIHI])
    VALUES(N'Demo Misafir',N'ork-demo-misafir@otelturizm.local',N'5000000200',LOWER(CONVERT(VARCHAR(64),HASHBYTES('SHA2_256',N'Demo123!'),2)),N'user',1,N'DemoReviewSeed',SYSUTCDATETIME());
    SET @GuestUserId=SCOPE_IDENTITY();
END;

IF OBJECT_ID(N'dbo.YORUMLAR',N'U') IS NULL BEGIN PRINT N'YORUMLAR yok; atlandi.'; RETURN; END;

DECLARE @Hotels TABLE (HotelId bigint NOT NULL, HotelKodu nvarchar(32) NOT NULL);
INSERT INTO @Hotels(HotelId,HotelKodu)
SELECT [ID],[OTEL_KODU] FROM [dbo].[OTELLER] WHERE [OTEL_KODU] IN (N'DEMO-ANTALYA-2026',N'DEMO-KAPADOKYA-2026');

DECLARE @HotelId bigint, @HotelKodu nvarchar(32);
DECLARE @Reviews TABLE (Baslik nvarchar(200), Metin nvarchar(max), G10 tinyint, K10 tinyint, T10 tinyint, P10 tinyint, F10 tinyint, Profil nvarchar(40));

WHILE EXISTS(SELECT 1 FROM @Hotels)
BEGIN
    SELECT TOP(1) @HotelId=HotelId, @HotelKodu=HotelKodu FROM @Hotels ORDER BY HotelKodu;
    DELETE FROM @Reviews;

    IF @HotelKodu = N'DEMO-ANTALYA-2026'
        INSERT INTO @Reviews VALUES
        (N'Sahil keyfi',N'Konyaalti plajina yurume mesafesinde, deniz manzarali odamiz harikaydi.',9,10,9,9,8,N'Aile'),
        (N'Aile suit mukemmel',N'Panoramik suitte cocuklar icin ek yatak duzeni cok iyiydi.',9,9,9,8,8,N'Aile'),
        (N'Erken rezervasyon avantaji',N'14 gun sonrasi fiyatlar dusuyor, indirim net gorunuyor.',8,8,9,8,9,N'Cift'),
        (N'Temiz ve genis',N'Standart oda bekledigimizden ferah, kahvalti zengin.',8,9,9,9,8,N'Cift'),
        (N'Transfer kolayligi',N'Havalimani transferi dakik, resepsiyon hizli check-in yapti.',9,9,8,9,8,N'Is');

    IF @HotelKodu = N'DEMO-KAPADOKYA-2026'
        INSERT INTO @Reviews VALUES
        (N'Balon manzarasi',N'Sabah terasindan balonlari izlemek inanilmazdi.',10,10,9,9,8,N'Cift'),
        (N'Cave oda atmosferi',N'Tas oda sicak ve otantik, jakuzili suitte gece cok keyifli.',9,9,10,9,7,N'Cift'),
        (N'Aile icin guvenli',N'Cocuklu ailemiz icin sessiz oda ayarladilar, cok memnun kaldik.',9,8,9,9,8,N'Aile'),
        (N'Konum super',N'Goreme merkeze yakin, yuruyus rotalari otelin onunden basliyor.',9,10,9,8,8,N'Cift'),
        (N'Fiyat performans',N'Kapadokya icin makul fiyat, oda fotograflari gercegi yansitiyor.',8,9,9,8,9,N'Cift');

    DECLARE @Baslik nvarchar(200), @Metin nvarchar(max), @G10 tinyint, @K10 tinyint, @T10 tinyint, @P10 tinyint, @F10 tinyint, @Profil nvarchar(40), @DaysAgo int = 2;
    WHILE EXISTS(SELECT 1 FROM @Reviews)
    BEGIN
        SELECT TOP(1) @Baslik=Baslik,@Metin=Metin,@G10=G10,@K10=K10,@T10=T10,@P10=P10,@F10=F10,@Profil=Profil FROM @Reviews ORDER BY Baslik;
        IF NOT EXISTS(SELECT 1 FROM [dbo].[YORUMLAR] WHERE [OTEL_ID]=@HotelId AND [YORUM_BASLIGI]=@Baslik)
            INSERT INTO [dbo].[YORUMLAR]([OTEL_ID],[KULLANICI_ID],[GENEL_PUAN],[TEMIZLIK_PUANI],[KONFOR_PUANI],[KONUM_PUANI],[PERSONEL_PUANI],[FIYAT_PERFORMANS_PUANI],[YORUM_BASLIGI],[YORUM_METNI],[KONAKLAMA_TARIHI],[KONAKLAMA_TURU],[DOGRULANMIS_KONAKLAMA],[ONAY_DURUMU],[ONAY_TARIHI],[SEYAHAT_PROFILI],[MEMNUNIYET_SEVIYESI],[GENEL_PUAN_10],[PUAN_KONUM_10],[PUAN_TEMIZLIK_10],[PUAN_PERSONEL_10],[PUAN_FIYAT_10],[OLUSTURULMA_TARIHI])
            VALUES(@HotelId,@GuestUserId,CASE WHEN @G10>=9 THEN 5 WHEN @G10>=7 THEN 4 ELSE 3 END,CASE WHEN @T10>=9 THEN 5 WHEN @T10>=7 THEN 4 ELSE 3 END,CASE WHEN @T10>=9 THEN 5 WHEN @T10>=7 THEN 4 ELSE 3 END,CASE WHEN @K10>=9 THEN 5 WHEN @K10>=7 THEN 4 ELSE 3 END,CASE WHEN @P10>=9 THEN 5 WHEN @P10>=7 THEN 4 ELSE 3 END,CASE WHEN @F10>=9 THEN 5 WHEN @F10>=7 THEN 4 ELSE 3 END,@Baslik,@Metin,DATEADD(DAY,-@DaysAgo,CAST(SYSUTCDATETIME() AS date)),N'Demo',1,N'Onaylandı',SYSUTCDATETIME(),@Profil,CASE WHEN @G10>=9 THEN 5 WHEN @G10>=7 THEN 4 ELSE 3 END,@G10,@K10,@T10,@P10,@F10,DATEADD(DAY,-@DaysAgo,SYSUTCDATETIME()));
        SET @DaysAgo+=3;
        DELETE FROM @Reviews WHERE Baslik=@Baslik;
    END;

    UPDATE [dbo].[OTELLER] SET [TOPLAM_YORUM_SAYISI]=(SELECT COUNT(*) FROM [dbo].[YORUMLAR] WHERE [OTEL_ID]=@HotelId AND [ONAY_DURUMU] LIKE N'Onaylan%'),
        [ORTALAMA_PUAN]=COALESCE((SELECT AVG(CAST(COALESCE([GENEL_PUAN_10],[GENEL_PUAN]*2) AS decimal(9,2))) FROM [dbo].[YORUMLAR] WHERE [OTEL_ID]=@HotelId AND [ONAY_DURUMU] LIKE N'Onaylan%'),[ORTALAMA_PUAN])
    WHERE [ID]=@HotelId;

    DELETE FROM @Hotels WHERE HotelId=@HotelId;
END;

PRINT N'Antalya + Kapadokya demo yorum seed tamam.';
