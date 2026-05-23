-- Idempotent: Istanbul — bir ilce bir otel (39 ilce), dedicated partner, oda, fiyat, kampanya, rezervasyon
-- Uygulama: sqlcmd -S "(localdb)\MSSQLLocalDB" -d otelturizm_2026db -i bu dosya
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET DATEFIRST 1;

DECLARE @TrUlkeId bigint = (SELECT TOP (1) [ID] FROM [dbo].[ULKELER] WHERE [ISO2_KODU] = N'TR' ORDER BY [ID]);
DECLARE @IstanbulIlId bigint = (SELECT TOP (1) [ID] FROM [dbo].[ILLER] WHERE [IL_ADI] LIKE N'%stanbul%' ORDER BY [ID]);

IF @TrUlkeId IS NULL OR @IstanbulIlId IS NULL
BEGIN
    RAISERROR(N'ULKELER/ILLER eksik; once geo seed uygulayin.', 16, 1);
    RETURN;
END;

IF NOT EXISTS (SELECT 1 FROM [dbo].[OTEL_TIPLERI] WHERE [KOD] = N'HOTEL')
BEGIN
    INSERT INTO [dbo].[OTEL_TIPLERI] ([KOD], [TIP_ADI], [ACIKLAMA], [AKTIF_MI], [SIRALAMA])
    VALUES (N'HOTEL', N'OTEL', N'Orkestra demo seed tipi', 1, 1);
END;

DECLARE @HotelTypeId int = (SELECT TOP (1) [ID] FROM [dbo].[OTEL_TIPLERI] WHERE [KOD] = N'HOTEL' ORDER BY [ID]);
DECLARE @KampanyaId bigint = (SELECT TOP (1) [ID] FROM [dbo].[KAMPANYALAR] WHERE [KAMPANYA_KODU] = N'KMP-2026-SEHIR' ORDER BY [ID]);
DECLARE @KampanyaBas datetime2(0) = COALESCE((SELECT [BASLANGIC_TARIHI] FROM [dbo].[KAMPANYALAR] WHERE [ID] = @KampanyaId), CAST(N'2026-01-01' AS datetime2(0)));
DECLARE @KampanyaBit datetime2(0) = COALESCE((SELECT [BITIS_TARIHI] FROM [dbo].[KAMPANYALAR] WHERE [ID] = @KampanyaId), CAST(N'2035-12-31 23:59:59' AS datetime2(0)));
DECLARE @DemoPasswordHash nvarchar(64) = LOWER(CONVERT(VARCHAR(64), HASHBYTES('SHA2_256', CONVERT(nvarchar(max), N'Demo123!')), 2));
DECLARE @GuestUserId bigint;
DECLARE @Today date = CAST(SYSUTCDATETIME() AS date);

IF NOT EXISTS (SELECT 1 FROM [dbo].[KULLANICILAR] WHERE [EPOSTA] = N'ork-demo-misafir@otelturizm.local')
BEGIN
    INSERT INTO [dbo].[KULLANICILAR]([AD_SOYAD],[EPOSTA],[TELEFON],[SIFRE],[ROL],[HESAP_DURUMU],[KAYIT_KAYNAGI],[OLUSTURULMA_TARIHI])
    VALUES (N'Orkestra Demo Misafir', N'ork-demo-misafir@otelturizm.local', N'5000000200', @DemoPasswordHash, N'user', 1, N'OrkestraSeed', SYSUTCDATETIME());
    SET @GuestUserId = SCOPE_IDENTITY();
END
ELSE
    SELECT @GuestUserId = [ID] FROM [dbo].[KULLANICILAR] WHERE [EPOSTA] = N'ork-demo-misafir@otelturizm.local';

IF OBJECT_ID(N'dbo.OTEL_OZELLIKLERI', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM [dbo].[OTEL_OZELLIKLERI] WHERE [OZELLIK_KODU] = N'ATM')
    BEGIN
        DECLARE @AtmKategoriId bigint = (SELECT TOP (1) [ID] FROM [dbo].[OTEL_OZELLIK_KATEGORILERI] WHERE [AKTIF_MI] = 1 ORDER BY [SIRALAMA], [ID]);
        IF @AtmKategoriId IS NOT NULL
            INSERT INTO [dbo].[OTEL_OZELLIKLERI]([KATEGORI_ID],[OZELLIK_ADI],[OZELLIK_IKON],[OZELLIK_KODU],[AKTIF_MI],[SIRALAMA],[FILTRELENEBILIR_MI])
            VALUES (@AtmKategoriId, N'ATM', N'fa-money-bill', N'ATM', 1, 95, 1);
    END
END;

DECLARE @OrkSeedMap TABLE (
    SeoSlug nvarchar(140) NOT NULL PRIMARY KEY,
    OtelKodu nvarchar(32) NOT NULL
);

INSERT INTO @OrkSeedMap (SeoSlug, OtelKodu) VALUES
(N'besiktas', N'ORK-SEED-001'),
(N'beyoglu', N'ORK-SEED-002'),
(N'kartal', N'ORK-SEED-003'),
(N'kadikoy', N'ORK-SEED-004'),
(N'sisli', N'ORK-SEED-005'),
(N'uskudar', N'ORK-SEED-006'),
(N'fatih', N'ORK-SEED-007'),
(N'bakirkoy', N'ORK-SEED-008'),
(N'maltepe', N'ORK-SEED-009'),
(N'atasehir', N'ORK-SEED-010');

DECLARE @OzellikKodlari TABLE (Kod nvarchar(80) NOT NULL PRIMARY KEY);
INSERT INTO @OzellikKodlari (Kod) VALUES
(N'UCRETSIZ_WIFI'),(N'OTOPARK'),(N'RESTORAN'),(N'KAHVALTI'),(N'RESEPSIYON_24_SAAT'),(N'ATM');

DECLARE @IlceId bigint, @IlceAdi nvarchar(100), @IlceSlug nvarchar(140);
DECLARE @IlceEnlem decimal(10,8), @IlceBoylam decimal(11,8);
DECLARE @OtelKodu nvarchar(32), @OtelAdi nvarchar(200), @PartnerEmail nvarchar(120), @VergiNo nvarchar(20);
DECLARE @PartnerUserId bigint, @PartnerId bigint, @HotelId bigint;
DECLARE @StdRoomId bigint, @DlxRoomId bigint, @StdFiyat decimal(10,2), @DlxFiyat decimal(10,2);
DECLARE @d int, @Tarih date, @Indirimli decimal(10,2), @IsWeekend bit;
DECLARE @Processed int = 0, @Skipped int = 0, @RezCounter int = 0;
DECLARE @RezNo1 nvarchar(20), @RezNo2 nvarchar(20), @Giris date, @Cikis date, @Gece smallint, @Toplam decimal(10,2);

DECLARE ilce_cursor CURSOR LOCAL FAST_FORWARD FOR
    SELECT c.[ID], c.[ILCE_ADI], c.[SEO_SLUG], c.[ENLEM], c.[BOYLAM]
    FROM [dbo].[ILCELER] c
    WHERE c.[IL_ID] = @IstanbulIlId AND c.[AKTIF_MI] = 1
    ORDER BY c.[ID];

OPEN ilce_cursor;
FETCH NEXT FROM ilce_cursor INTO @IlceId, @IlceAdi, @IlceSlug, @IlceEnlem, @IlceBoylam;

WHILE @@FETCH_STATUS = 0
BEGIN
    IF EXISTS (
        SELECT 1 FROM [dbo].[OTELLER] o
        WHERE o.[ILCE_ID] = @IlceId AND o.[YAYIN_DURUMU] LIKE N'Yay%'
    )
    BEGIN
        SET @Skipped += 1;
        FETCH NEXT FROM ilce_cursor INTO @IlceId, @IlceAdi, @IlceSlug, @IlceEnlem, @IlceBoylam;
        CONTINUE;
    END;

    SET @OtelKodu = COALESCE(
        (SELECT m.[OtelKodu] FROM @OrkSeedMap m WHERE m.[SeoSlug] = @IlceSlug),
        N'ORK-IST-' + UPPER(REPLACE(@IlceSlug, N'-', N''))
    );

    SET @OtelAdi = N'Orkestra ' + @IlceAdi + N' Hotel';
    SET @PartnerEmail = N'irmhro0+' + @IlceSlug + N'@gmail.com';
    SET @VergiNo = LEFT(N'ORK-IST-' + UPPER(REPLACE(@IlceSlug, N'-', N'')), 20);
    SET @StdFiyat = CAST(2500 + ((@IlceId % 15) * 200) AS decimal(10,2));
    SET @DlxFiyat = CAST(@StdFiyat * 1.35 AS decimal(10,2));

    IF @IlceEnlem IS NOT NULL AND @IlceBoylam IS NOT NULL
    BEGIN
        SET @IlceEnlem = @IlceEnlem;
        SET @IlceBoylam = @IlceBoylam;
    END
    ELSE
    BEGIN
        SET @IlceEnlem = CAST(41.01 + ((@IlceId % 20) * 0.008) AS decimal(10,8));
        SET @IlceBoylam = CAST(28.85 + ((@IlceId % 25) * 0.012) AS decimal(11,8));
    END;

    SET @PartnerUserId = NULL;
    SELECT @PartnerUserId = [ID] FROM [dbo].[KULLANICILAR] WHERE [EPOSTA] = @PartnerEmail;

    IF @PartnerUserId IS NULL
    BEGIN
        INSERT INTO [dbo].[KULLANICILAR]([AD_SOYAD],[EPOSTA],[TELEFON],[SIFRE],[ROL],[HESAP_DURUMU],[KAYIT_KAYNAGI],[OLUSTURULMA_TARIHI])
        VALUES (
            N'Orkestra ' + @IlceAdi + N' Partner',
            @PartnerEmail,
            N'5' + RIGHT(N'00000000' + CAST((500000000 + (@IlceId % 90000000)) AS nvarchar(20)), 9),
            @DemoPasswordHash,
            N'partner',
            1,
            N'OrkestraSeed',
            SYSUTCDATETIME()
        );
        SET @PartnerUserId = SCOPE_IDENTITY();
    END;

    SET @PartnerId = NULL;
    SELECT @PartnerId = [ID] FROM [dbo].[PARTNER_DETAYLARI] WHERE [VERGI_NUMARASI] = @VergiNo;

    IF @PartnerId IS NULL
    BEGIN
        INSERT INTO [dbo].[PARTNER_DETAYLARI](
            [KULLANICI_ID],[FIRMA_UNVANI],[FIRMA_TURU],[VERGI_DAIRESI],[VERGI_NUMARASI],
            [FATURA_ADRESI],[FATURA_IL],[FATURA_ILCE],[YETKILI_AD_SOYAD],[YETKILI_TC_NO],
            [YETKILI_TELEFON],[YETKILI_EPOSTA],[BANKA_ADI],[IBAN],[HESAP_SAHIBI_ADI],
            [ONAY_DURUMU],[ONAY_TARIHI],[AKTIF_MI],[OTEL_TIPI_ID],[OLUSTURULMA_TARIHI]
        )
        VALUES (
            @PartnerUserId,
            N'Orkestra ' + @IlceAdi + N' Turizm Ltd.',
            N'Limited',
            N'Istanbul',
            @VergiNo,
            N'Demo Mah. No:1',
            N'Istanbul',
            @IlceAdi,
            N'Orkestra ' + @IlceAdi + N' Yetkili',
            N'11111111111',
            N'5' + RIGHT(N'00000000' + CAST((500000000 + (@IlceId % 90000000)) AS nvarchar(20)), 9),
            @PartnerEmail,
            N'Demo Bank',
            N'TR000000000000000000000001',
            N'Orkestra ' + @IlceAdi + N' Partner',
            N'Onaylandi',
            SYSUTCDATETIME(),
            1,
            @HotelTypeId,
            SYSUTCDATETIME()
        );
        SET @PartnerId = SCOPE_IDENTITY();
    END
    ELSE
    BEGIN
        UPDATE [dbo].[PARTNER_DETAYLARI]
        SET [KULLANICI_ID] = @PartnerUserId,
            [FIRMA_UNVANI] = N'Orkestra ' + @IlceAdi + N' Turizm Ltd.',
            [FATURA_ILCE] = @IlceAdi,
            [YETKILI_EPOSTA] = @PartnerEmail,
            [AKTIF_MI] = 1,
            [ONAY_DURUMU] = N'Onaylandi'
        WHERE [ID] = @PartnerId;
    END;

    SET @HotelId = NULL;
    SELECT @HotelId = [ID] FROM [dbo].[OTELLER] WHERE [OTEL_KODU] = @OtelKodu;

    IF @HotelId IS NULL
    BEGIN
        INSERT INTO [dbo].[OTELLER](
            [OTEL_KODU],[PARTNER_ID],[KULLANICI_ID],[OTEL_ADI],[OTEL_TURU],[OTEL_TIPI_ID],[YILDIZ_SAYISI],
            [ULKE],[SEHIR],[ILCE],[MAHALLE],[TAM_ADRES],[ENLEM],[BOYLAM],[ULKE_ID],[SEHIR_ID],[ILCE_ID],
            [TELEFON_1],[EPOSTA],[REZERVASYON_TELEFONU],[SATIS_KONTAK_ADI],[SATIS_KONTAK_TELEFONU],[SATIS_KONTAK_EPOSTA],
            [CHECK_IN_SAATI],[CHECK_OUT_SAATI],[TOPLAM_ODA_SAYISI],[KISA_ACIKLAMA],[UZUN_ACIKLAMA],
            [VARSAYILAN_KOMISYON_ORANI],[ODEME_VADESI],[ODEME_YONTEMI],[FATURA_KESIM_TURU],
            [ORTALAMA_PUAN],[TOPLAM_YORUM_SAYISI],[YAYIN_DURUMU],[ONAY_DURUMU],[ONAY_TARIHI],[ONE_CIKAN_OTEL],[TAVSIYE_EDILEN_OTEL],[OLUSTURULMA_TARIHI]
        )
        VALUES(
            @OtelKodu, @PartnerId, @PartnerUserId, @OtelAdi, N'Hotel', @HotelTypeId, 4,
            N'Turkiye', N'Istanbul', @IlceAdi, @IlceAdi + N' Merkez', CONCAT(@IlceAdi, N' Merkez, Istanbul'),
            @IlceEnlem, @IlceBoylam, @TrUlkeId, @IstanbulIlId, @IlceId,
            N'2125550000', CONCAT(N'rez.', LOWER(REPLACE(@OtelKodu, N'-', N'')), N'@demo.otelturizm.local'),
            N'2125550001', N'Orkestra Demo', N'2125550002', @PartnerEmail,
            '14:00:00', '12:00:00', 24,
            N'Istanbul ' + @IlceAdi + N' demo oteli — liste, harita, rezervasyon test.',
            N'Istanbul ' + @IlceAdi + N' demo oteli — Orkestra ilce seed paketi.',
            15.00, N'Ayın 15''i', N'Online', N'Otel Keser',
            8.5, 48, N'Yayında', N'Onaylandı', SYSUTCDATETIME(), 0, 0, SYSUTCDATETIME()
        );
        SET @HotelId = SCOPE_IDENTITY();
    END
    ELSE
    BEGIN
        UPDATE [dbo].[OTELLER] SET
            [PARTNER_ID] = @PartnerId,
            [KULLANICI_ID] = @PartnerUserId,
            [OTEL_ADI] = @OtelAdi,
            [ILCE] = @IlceAdi,
            [MAHALLE] = @IlceAdi + N' Merkez',
            [TAM_ADRES] = CONCAT(@IlceAdi, N' Merkez, Istanbul'),
            [ILCE_ID] = @IlceId,
            [SEHIR_ID] = @IstanbulIlId,
            [ULKE_ID] = @TrUlkeId,
            [ENLEM] = @IlceEnlem,
            [BOYLAM] = @IlceBoylam,
            [SATIS_KONTAK_EPOSTA] = @PartnerEmail,
            [YAYIN_DURUMU] = N'Yayında',
            [ONAY_DURUMU] = N'Onaylandı',
            [ONAY_TARIHI] = COALESCE([ONAY_TARIHI], SYSUTCDATETIME())
        WHERE [ID] = @HotelId;
    END;

    IF NOT EXISTS (SELECT 1 FROM [dbo].[OTEL_KULLANICI_SAHIPLIKLERI] WHERE [OTEL_ID] = @HotelId AND [KULLANICI_ID] = @PartnerUserId)
        INSERT INTO [dbo].[OTEL_KULLANICI_SAHIPLIKLERI]([OTEL_ID],[KULLANICI_ID],[PARTNER_ID],[ROL],[ANA_SORUMLU_MU],[AKTIF_MI],[OLUSTURULMA_TARIHI])
        VALUES(@HotelId, @PartnerUserId, @PartnerId, N'owner', 1, 1, SYSUTCDATETIME());
    ELSE
        UPDATE [dbo].[OTEL_KULLANICI_SAHIPLIKLERI]
        SET [PARTNER_ID] = @PartnerId, [ANA_SORUMLU_MU] = 1, [AKTIF_MI] = 1
        WHERE [OTEL_ID] = @HotelId AND [KULLANICI_ID] = @PartnerUserId;

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

    SET @StdRoomId = NULL;
    SELECT @StdRoomId = [ID] FROM [dbo].[ODA_TIPLERI] WHERE [OTEL_ID] = @HotelId AND [ODA_TIP_KODU] = N'STD-DEMO';
    IF @StdRoomId IS NULL
    BEGIN
        INSERT INTO [dbo].[ODA_TIPLERI]([OTEL_ID],[ODA_TIP_KODU],[ODA_ADI],[ODA_KATEGORISI],[MAKSIMUM_KISI_SAYISI],[MAKSIMUM_YETISKIN_SAYISI],[MAKSIMUM_COCUK_SAYISI],[STANDART_GECELIK_FIYAT],[TOPLAM_ODA_SAYISI],[AKTIF_MI],[SIRALAMA])
        VALUES(@HotelId, N'STD-DEMO', N'Standart Demo', N'Standart Oda', 2, 2, 1, @StdFiyat, 10, 1, 1);
        SET @StdRoomId = SCOPE_IDENTITY();
    END
    ELSE
        UPDATE [dbo].[ODA_TIPLERI] SET [STANDART_GECELIK_FIYAT] = @StdFiyat, [AKTIF_MI] = 1 WHERE [ID] = @StdRoomId;

    SET @DlxRoomId = NULL;
    SELECT @DlxRoomId = [ID] FROM [dbo].[ODA_TIPLERI] WHERE [OTEL_ID] = @HotelId AND [ODA_TIP_KODU] = N'DLX-DEMO';
    IF @DlxRoomId IS NULL
    BEGIN
        INSERT INTO [dbo].[ODA_TIPLERI]([OTEL_ID],[ODA_TIP_KODU],[ODA_ADI],[ODA_KATEGORISI],[MAKSIMUM_KISI_SAYISI],[MAKSIMUM_YETISKIN_SAYISI],[MAKSIMUM_COCUK_SAYISI],[STANDART_GECELIK_FIYAT],[TOPLAM_ODA_SAYISI],[AKTIF_MI],[SIRALAMA])
        VALUES(@HotelId, N'DLX-DEMO', N'Deluxe Demo', N'Deluxe Oda', 3, 3, 1, @DlxFiyat, 6, 1, 2);
        SET @DlxRoomId = SCOPE_IDENTITY();
    END
    ELSE
        UPDATE [dbo].[ODA_TIPLERI] SET [STANDART_GECELIK_FIYAT] = @DlxFiyat, [AKTIF_MI] = 1 WHERE [ID] = @DlxRoomId;

    SET @d = 0;
    WHILE @d < 30
    BEGIN
        SET @Tarih = DATEADD(DAY, @d, @Today);
        SET @IsWeekend = CASE WHEN DATEPART(WEEKDAY, @Tarih) IN (6, 7) THEN 1 ELSE 0 END;

        IF NOT EXISTS (SELECT 1 FROM [dbo].[ODA_FIYAT_MUSAITLIK] WHERE [OTEL_ID] = @HotelId AND [ODA_TIP_ID] = @StdRoomId AND [TARIH] = @Tarih)
            INSERT INTO [dbo].[ODA_FIYAT_MUSAITLIK]([ODA_TIP_ID],[OTEL_ID],[TARIH],[GECELIK_FIYAT],[INDIRIMLI_FIYAT],[KAMPANYA_ID],[TOPLAM_ODA_SAYISI],[KAPALI_SATIS],[KAMPANYA_ETIKETI])
            VALUES(
                @StdRoomId, @HotelId, @Tarih, @StdFiyat,
                CASE WHEN @IsWeekend = 1 THEN CAST(@StdFiyat * 0.85 AS decimal(10,2)) ELSE NULL END,
                CASE WHEN @KampanyaId IS NOT NULL THEN @KampanyaId ELSE NULL END,
                10, 0, CASE WHEN @KampanyaId IS NOT NULL THEN N'SEHIR' ELSE NULL END
            );
        ELSE
            UPDATE [dbo].[ODA_FIYAT_MUSAITLIK]
            SET [GECELIK_FIYAT] = @StdFiyat,
                [INDIRIMLI_FIYAT] = CASE WHEN @IsWeekend = 1 THEN CAST(@StdFiyat * 0.85 AS decimal(10,2)) ELSE [INDIRIMLI_FIYAT] END,
                [KAMPANYA_ID] = COALESCE(@KampanyaId, [KAMPANYA_ID]),
                [KAMPANYA_ETIKETI] = COALESCE(N'SEHIR', [KAMPANYA_ETIKETI])
            WHERE [OTEL_ID] = @HotelId AND [ODA_TIP_ID] = @StdRoomId AND [TARIH] = @Tarih;

        IF NOT EXISTS (SELECT 1 FROM [dbo].[ODA_FIYAT_MUSAITLIK] WHERE [OTEL_ID] = @HotelId AND [ODA_TIP_ID] = @DlxRoomId AND [TARIH] = @Tarih)
            INSERT INTO [dbo].[ODA_FIYAT_MUSAITLIK]([ODA_TIP_ID],[OTEL_ID],[TARIH],[GECELIK_FIYAT],[INDIRIMLI_FIYAT],[KAMPANYA_ID],[TOPLAM_ODA_SAYISI],[KAPALI_SATIS],[KAMPANYA_ETIKETI])
            VALUES(
                @DlxRoomId, @HotelId, @Tarih, @DlxFiyat,
                CASE WHEN @IsWeekend = 1 THEN CAST(@DlxFiyat * 0.85 AS decimal(10,2)) ELSE NULL END,
                CASE WHEN @KampanyaId IS NOT NULL THEN @KampanyaId ELSE NULL END,
                6, 0, CASE WHEN @KampanyaId IS NOT NULL THEN N'SEHIR' ELSE NULL END
            );
        ELSE
            UPDATE [dbo].[ODA_FIYAT_MUSAITLIK]
            SET [GECELIK_FIYAT] = @DlxFiyat,
                [INDIRIMLI_FIYAT] = CASE WHEN @IsWeekend = 1 THEN CAST(@DlxFiyat * 0.85 AS decimal(10,2)) ELSE [INDIRIMLI_FIYAT] END,
                [KAMPANYA_ID] = COALESCE(@KampanyaId, [KAMPANYA_ID]),
                [KAMPANYA_ETIKETI] = COALESCE(N'SEHIR', [KAMPANYA_ETIKETI])
            WHERE [OTEL_ID] = @HotelId AND [ODA_TIP_ID] = @DlxRoomId AND [TARIH] = @Tarih;

        SET @d += 1;
    END;

    IF @KampanyaId IS NOT NULL AND OBJECT_ID(N'dbo.KAMPANYA_OTELLER', N'U') IS NOT NULL
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM [dbo].[KAMPANYA_OTELLER] WHERE [KAMPANYA_ID] = @KampanyaId AND [OTEL_ID] = @HotelId)
            INSERT INTO [dbo].[KAMPANYA_OTELLER](
                [KAMPANYA_ID],[OTEL_ID],[PARTNER_ID],[KATILIM_DURUMU],[KATILIM_KAYNAGI],
                [BASLANGIC_TARIHI],[BITIS_TARIHI],[ADMIN_ONAY_TARIHI],[PARTNER_ONAY_TARIHI],[OLUSTURULMA_TARIHI]
            )
            VALUES(
                @KampanyaId, @HotelId, @PartnerId, N'Onaylandi', N'OrkestraSeed',
                @KampanyaBas, @KampanyaBit, SYSUTCDATETIME(), SYSUTCDATETIME(), SYSUTCDATETIME()
            );
        ELSE
            UPDATE [dbo].[KAMPANYA_OTELLER]
            SET [KATILIM_DURUMU] = N'Onaylandi', [PARTNER_ID] = @PartnerId, [ADMIN_ONAY_TARIHI] = COALESCE([ADMIN_ONAY_TARIHI], SYSUTCDATETIME())
            WHERE [KAMPANYA_ID] = @KampanyaId AND [OTEL_ID] = @HotelId;
    END;

    IF @RezCounter < 5 AND OBJECT_ID(N'dbo.REZERVASYONLAR', N'U') IS NOT NULL AND @GuestUserId IS NOT NULL
    BEGIN
        SET @RezNo1 = N'ORK-ILCE-' + RIGHT(N'00' + CAST(@RezCounter + 1 AS nvarchar(2)), 2) + N'A';
        SET @RezNo2 = N'ORK-ILCE-' + RIGHT(N'00' + CAST(@RezCounter + 1 AS nvarchar(2)), 2) + N'B';
        SET @Giris = DATEADD(DAY, 14 + (@RezCounter * 3), @Today);
        SET @Cikis = DATEADD(DAY, 2, @Giris);
        SET @Gece = 2;
        SET @Toplam = @StdFiyat * @Gece;

        IF NOT EXISTS (SELECT 1 FROM [dbo].[REZERVASYONLAR] WHERE [REZERVASYON_NO] = @RezNo1)
            INSERT INTO [dbo].[REZERVASYONLAR](
                [REZERVASYON_NO],[OTEL_ID],[ODA_TIP_ID],[KULLANICI_ID],
                [MISAFIR_AD_SOYAD],[MISAFIR_EPOSTA],[MISAFIR_TELEFON],
                [GIRIS_TARIHI],[CIKIS_TARIHI],[GECE_SAYISI],[YETISKIN_SAYISI],
                [GECELIK_FIYAT],[TOPLAM_ODA_TUTARI],[TOPLAM_TASARRUF],[TOPLAM_TUTAR],[KOMISYON_ORANI],
                [DURUM],[OTEL_ONAY_DURUMU],[FIRMA_ONAY_DURUMU],[ODEME_DURUMU],[KAYNAK],[KAMPANYA_KODU]
            )
            VALUES(
                @RezNo1, @HotelId, @StdRoomId, @GuestUserId,
                N'Demo Misafir ' + CAST(@RezCounter + 1 AS nvarchar(2)), N'ork-demo-misafir@otelturizm.local', N'5000000200',
                @Giris, @Cikis, @Gece, 2,
                @StdFiyat, @Toplam, 0, @Toplam, 15.00,
                N'Onaylandi', N'Onaylandi', N'Onaylandi', N'Odendi', N'OrkestraSeed', N'KMP-2026-SEHIR'
            );

        IF @RezCounter < 5 AND NOT EXISTS (SELECT 1 FROM [dbo].[REZERVASYONLAR] WHERE [REZERVASYON_NO] = @RezNo2)
            INSERT INTO [dbo].[REZERVASYONLAR](
                [REZERVASYON_NO],[OTEL_ID],[ODA_TIP_ID],[KULLANICI_ID],
                [MISAFIR_AD_SOYAD],[MISAFIR_EPOSTA],[MISAFIR_TELEFON],
                [GIRIS_TARIHI],[CIKIS_TARIHI],[GECE_SAYISI],[YETISKIN_SAYISI],
                [GECELIK_FIYAT],[TOPLAM_ODA_TUTARI],[TOPLAM_TASARRUF],[TOPLAM_TUTAR],[KOMISYON_ORANI],
                [DURUM],[OTEL_ONAY_DURUMU],[FIRMA_ONAY_DURUMU],[ODEME_DURUMU],[KAYNAK],[KAMPANYA_KODU]
            )
            VALUES(
                @RezNo2, @HotelId, @DlxRoomId, @GuestUserId,
                N'Demo Misafir Deluxe ' + CAST(@RezCounter + 1 AS nvarchar(2)), N'ork-demo-misafir@otelturizm.local', N'5000000201',
                DATEADD(DAY, 1, @Giris), DATEADD(DAY, 3, @Giris), 2, 2,
                @DlxFiyat, @DlxFiyat * 2, 0, @DlxFiyat * 2, 15.00,
                N'Onaylandi', N'Onaylandi', N'Onaylandi', N'Odendi', N'OrkestraSeed', N'KMP-2026-SEHIR'
            );

        SET @RezCounter += 1;
    END;

    SET @Processed += 1;
    FETCH NEXT FROM ilce_cursor INTO @IlceId, @IlceAdi, @IlceSlug, @IlceEnlem, @IlceBoylam;
END;

CLOSE ilce_cursor;
DEALLOCATE ilce_cursor;

IF @KampanyaId IS NOT NULL AND OBJECT_ID(N'dbo.KAMPANYA_OTELLER', N'U') IS NOT NULL
BEGIN
    INSERT INTO [dbo].[KAMPANYA_OTELLER](
        [KAMPANYA_ID],[OTEL_ID],[PARTNER_ID],[KATILIM_DURUMU],[KATILIM_KAYNAGI],
        [BASLANGIC_TARIHI],[BITIS_TARIHI],[ADMIN_ONAY_TARIHI],[PARTNER_ONAY_TARIHI],[OLUSTURULMA_TARIHI]
    )
    SELECT
        @KampanyaId, o.[ID], o.[PARTNER_ID], N'Onaylandi', N'OrkestraSeed',
        @KampanyaBas, @KampanyaBit, SYSUTCDATETIME(), SYSUTCDATETIME(), SYSUTCDATETIME()
    FROM [dbo].[OTELLER] o
    INNER JOIN [dbo].[ILCELER] c ON c.[ID] = o.[ILCE_ID]
    INNER JOIN [dbo].[ILLER] i ON i.[ID] = c.[IL_ID]
    WHERE c.[IL_ID] = @IstanbulIlId
      AND o.[YAYIN_DURUMU] LIKE N'Yay%'
      AND (o.[OTEL_KODU] LIKE N'ORK-IST-%' OR o.[OTEL_KODU] LIKE N'ORK-SEED-%')
      AND NOT EXISTS (
          SELECT 1 FROM [dbo].[KAMPANYA_OTELLER] ko
          WHERE ko.[KAMPANYA_ID] = @KampanyaId AND ko.[OTEL_ID] = o.[ID]
      );
END;

DECLARE @OtelYayinda int = (SELECT COUNT(*) FROM [dbo].[OTELLER] WHERE [YAYIN_DURUMU] LIKE N'Yay%' AND [ILCE_ID] IN (SELECT [ID] FROM [dbo].[ILCELER] WHERE [IL_ID] = @IstanbulIlId));
DECLARE @PartnerCount int = (SELECT COUNT(*) FROM [dbo].[PARTNER_DETAYLARI] WHERE [VERGI_NUMARASI] LIKE N'ORK-IST-%');
DECLARE @RezCount int = (SELECT COUNT(*) FROM [dbo].[REZERVASYONLAR] WHERE [REZERVASYON_NO] LIKE N'ORK-ILCE-%');

PRINT N'Istanbul ilce otel seed tamam.';
PRINT N'  Islenen ilce: ' + CAST(@Processed AS nvarchar(12));
PRINT N'  Atlanan ilce (yayinda otel vardi): ' + CAST(@Skipped AS nvarchar(12));
PRINT N'  Istanbul yayinda otel: ' + CAST(@OtelYayinda AS nvarchar(12));
PRINT N'  ORK-IST partner: ' + CAST(@PartnerCount AS nvarchar(12));
PRINT N'  Demo rezervasyon (ORK-ILCE-*): ' + CAST(@RezCount AS nvarchar(12));
