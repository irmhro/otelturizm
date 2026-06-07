-- Demo Maidan oteline tablodaki tum tesis ozelliklerini bagla (idempotent)
-- Oncelik: 20260525_seed_demo_maidan_otel_tam.sql

SET NOCOUNT ON;

DECLARE @HotelId bigint;
SELECT @HotelId = [ID] FROM [dbo].[OTELLER] WHERE [OTEL_KODU] = N'DEMO-MAIDAN-2026';

IF @HotelId IS NULL
BEGIN
    RAISERROR(N'DEMO-MAIDAN-2026 bulunamadi.', 16, 1);
    RETURN;
END;

DECLARE @OzellikKodlari TABLE (Kod nvarchar(80) NOT NULL PRIMARY KEY);
INSERT INTO @OzellikKodlari (Kod) VALUES
(N'RESEPSIYON_24_SAAT'),(N'UCRETSIZ_WIFI'),(N'ASANSOR'),(N'KLIMA'),(N'GUNLUK_TEMIZLIK'),
(N'RESTORAN'),(N'KAHVALTI'),(N'ACIK_BUFE'),(N'ODA_SERVISI'),(N'BAR'),
(N'MINIBAR'),(N'EMANET_KASASI'),(N'TV'),(N'KAHVE_CAY_SETI'),(N'BALKON'),
(N'SPA'),(N'SAUNA'),(N'HAMAM'),(N'FITNESS'),
(N'HAVUZ_KAPALI'),(N'COCUK_HAVUZU'),
(N'AILE_ODASI'),(N'COCUK_KULUBU'),(N'BEBEK_YATAGI'),
(N'ENGELLI_ERISIMI'),(N'ENGELLI_BANYO'),
(N'OTOPARK'),(N'HAVAALANI_SERVISI'),(N'VALE_HIZMETI'),(N'SEHIR_ICI_TRANSFER'),
(N'TOPLANTI_ODASI'),(N'IS_MERKEZI'),
(N'KARTLI_GIRIS'),(N'KAMERA_SISTEMI');

IF OBJECT_ID(N'dbo.OTEL_OZELLIK_ILISKILERI', N'U') IS NOT NULL AND OBJECT_ID(N'dbo.OTEL_OZELLIKLERI', N'U') IS NOT NULL
BEGIN
    INSERT INTO [dbo].[OTEL_OZELLIK_ILISKILERI]([OTEL_ID],[OZELLIK_ID],[AKTIF_MI])
    SELECT @HotelId, o.[ID], 1
    FROM [dbo].[OTEL_OZELLIKLERI] o
    INNER JOIN @OzellikKodlari k ON k.[Kod] = o.[OZELLIK_KODU]
    WHERE NOT EXISTS (
        SELECT 1 FROM [dbo].[OTEL_OZELLIK_ILISKILERI] i
        WHERE i.[OTEL_ID] = @HotelId AND i.[OZELLIK_ID] = o.[ID]
    );
END;

IF OBJECT_ID(N'dbo.OTEL_KOSULLARI', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM [dbo].[OTEL_KOSULLARI] WHERE [OTEL_ID] = @HotelId)
        INSERT INTO [dbo].[OTEL_KOSULLARI](
            [OTEL_ID],[IPTAL_POLITIKASI_OZET],[DETAYLI_IPTAL_KOSULLARI],[UCRETSIZ_IPTAL_SURESI],
            [SIGARA_POLITIKASI],[EVCIL_HAYVAN_POLITIKASI],[COCUK_KABUL_YAS_ARALIGI],
            [KREDI_KARTI_ILE_ODEME_KABUL],[GUNCELLENME_TARIHI]
        )
        VALUES(
            @HotelId,
            N'Giris tarihinden 24 saat onceye kadar ucretsiz iptal.',
            N'24 saat oncesine kadar ucretsiz iptal; gec iptallerde ilk gece ucreti tahsil edilir.',
            1,
            N'Tum kapali alanlarda sigara icilmez.',
            N'15 kg alti evcil hayvan kabul edilir (ucretli).',
            N'0-12 yas ucretsiz',
            1, SYSUTCDATETIME()
        );
    ELSE
        UPDATE [dbo].[OTEL_KOSULLARI]
        SET [SIGARA_POLITIKASI] = N'Tum kapali alanlarda sigara icilmez.',
            [EVCIL_HAYVAN_POLITIKASI] = N'15 kg alti evcil hayvan kabul edilir (ucretli).',
            [COCUK_KABUL_YAS_ARALIGI] = N'0-12 yas',
            [IPTAL_POLITIKASI_OZET] = N'Giris tarihinden 24 saat onceye kadar ucretsiz iptal.',
            [DETAYLI_IPTAL_KOSULLARI] = N'24 saat oncesine kadar ucretsiz iptal; gec iptallerde ilk gece ucreti tahsil edilir.',
            [UCRETSIZ_IPTAL_SURESI] = 1,
            [KREDI_KARTI_ILE_ODEME_KABUL] = 1,
            [GUNCELLENME_TARIHI] = SYSUTCDATETIME()
        WHERE [OTEL_ID] = @HotelId;
END;

PRINT N'Maidan demo ozellik genisletme tamam. OTEL_ID=' + CAST(@HotelId AS nvarchar(20));
