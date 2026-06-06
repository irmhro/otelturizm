-- Idempotent: DEMO-MAIDAN-2026 — tam demo otel (2 oda, fiyat, indirim, ozellik, gorsel, kosul)
-- Uygulama: sqlcmd -S "185.111.244.246" -d otelturizm_2026db -U sa -P "..." -I -f 65001 -b -i bu dosya
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET DATEFIRST 1;

DECLARE @OtelKodu nvarchar(32) = N'DEMO-MAIDAN-2026';
DECLARE @TrUlkeId bigint = (SELECT TOP (1) [ID] FROM [dbo].[ULKELER] WHERE [ISO2_KODU] = N'TR' ORDER BY [ID]);
DECLARE @IstanbulIlId bigint = (SELECT TOP (1) [ID] FROM [dbo].[ILLER] WHERE [IL_ADI] LIKE N'%stanbul%' ORDER BY [ID]);
DECLARE @BeyogluIlceId bigint = (
    SELECT TOP (1) [ID] FROM [dbo].[ILCELER]
    WHERE [IL_ID] = @IstanbulIlId AND ([ILCE_ADI] LIKE N'Beyo%' OR [SEO_SLUG] = N'beyoglu')
    ORDER BY [ID]);

IF @TrUlkeId IS NULL OR @IstanbulIlId IS NULL
BEGIN
    RAISERROR(N'ULKELER/ILLER eksik; once geo seed uygulayin.', 16, 1);
    RETURN;
END;

IF NOT EXISTS (SELECT 1 FROM [dbo].[OTEL_TIPLERI] WHERE [KOD] = N'HOTEL')
BEGIN
    INSERT INTO [dbo].[OTEL_TIPLERI] ([KOD], [TIP_ADI], [ACIKLAMA], [AKTIF_MI], [SIRALAMA])
    VALUES (N'HOTEL', N'OTEL', N'Demo otel tipi', 1, 1);
END;

DECLARE @HotelTypeId int = (SELECT TOP (1) [ID] FROM [dbo].[OTEL_TIPLERI] WHERE [KOD] = N'HOTEL' ORDER BY [ID]);
DECLARE @PartnerUserId bigint;
DECLARE @PartnerId bigint;
DECLARE @DemoPasswordHash nvarchar(64) = LOWER(CONVERT(VARCHAR(64), HASHBYTES('SHA2_256', CONVERT(nvarchar(max), N'Demo123!')), 2));
DECLARE @HotelId bigint;
DECLARE @SupRoomId bigint;
DECLARE @DlxRoomId bigint;
DECLARE @Today date = CAST(SYSUTCDATETIME() AS date);
DECLARE @StdFiyat decimal(10,2) = 5200.00;
DECLARE @DlxFiyat decimal(10,2) = 7020.00;
DECLARE @d int = 0;
DECLARE @Tarih date;
DECLARE @IsWeekend bit;
DECLARE @Indirimli decimal(10,2);
DECLARE @WeekendIndirimId bigint;
DECLARE @ErkenIndirimId bigint;
DECLARE @KampanyaId bigint = (SELECT TOP (1) [ID] FROM [dbo].[KAMPANYALAR] WHERE [KAMPANYA_KODU] = N'KMP-2026-SEHIR' ORDER BY [ID]);
DECLARE @KampanyaBas datetime2(0) = COALESCE((SELECT [BASLANGIC_TARIHI] FROM [dbo].[KAMPANYALAR] WHERE [ID] = @KampanyaId), CAST(N'2026-01-01' AS datetime2(0)));
DECLARE @KampanyaBit datetime2(0) = COALESCE((SELECT [BITIS_TARIHI] FROM [dbo].[KAMPANYALAR] WHERE [ID] = @KampanyaId), CAST(N'2035-12-31 23:59:59' AS datetime2(0)));

IF OBJECT_ID(N'dbo.FIYAT_INDIRIMLERI', N'U') IS NOT NULL
BEGIN
    SELECT @WeekendIndirimId = [ID] FROM [dbo].[FIYAT_INDIRIMLERI] WHERE [INDIRIM_ADI] = N'Hafta Sonu Demo';
    IF @WeekendIndirimId IS NULL
    BEGIN
        INSERT INTO [dbo].[FIYAT_INDIRIMLERI]([INDIRIM_ADI],[KISA_ACIKLAMA],[IKON_CLASS],[RENK_KODU],[AKTIF_MI],[SIRALAMA])
        VALUES (N'Hafta Sonu Demo', N'Cumartesi-Pazar %15 indirim', N'fa-calendar-week', N'#0F766E', 1, 10);
        SET @WeekendIndirimId = SCOPE_IDENTITY();
    END;

    SELECT @ErkenIndirimId = [ID] FROM [dbo].[FIYAT_INDIRIMLERI] WHERE [INDIRIM_ADI] = N'Maidan Erken Rezervasyon';
    IF @ErkenIndirimId IS NULL
    BEGIN
        INSERT INTO [dbo].[FIYAT_INDIRIMLERI]([INDIRIM_ADI],[KISA_ACIKLAMA],[IKON_CLASS],[RENK_KODU],[AKTIF_MI],[SIRALAMA],[GORSEL_URL])
        VALUES (N'Maidan Erken Rezervasyon', N'30 gun oncesi %20 indirim', N'fa-clock', N'#B45309', 1, 5, N'/assets/img/campaigns/early-bird.svg');
        SET @ErkenIndirimId = SCOPE_IDENTITY();
    END;
END;

IF NOT EXISTS (SELECT 1 FROM [dbo].[KULLANICILAR] WHERE [EPOSTA] = N'ork-demo-partner@otelturizm.local')
BEGIN
    INSERT INTO [dbo].[KULLANICILAR]
        ([AD_SOYAD], [EPOSTA], [TELEFON], [SIFRE], [ROL], [HESAP_DURUMU], [KAYIT_KAYNAGI], [OLUSTURULMA_TARIHI])
    VALUES
        (N'Orkestra Demo Partner', N'ork-demo-partner@otelturizm.local', N'5000000099', @DemoPasswordHash, N'partner', 1, N'MaidanSeed', SYSUTCDATETIME());
    SET @PartnerUserId = SCOPE_IDENTITY();
END
ELSE
    SELECT @PartnerUserId = [ID] FROM [dbo].[KULLANICILAR] WHERE [EPOSTA] = N'ork-demo-partner@otelturizm.local';

IF NOT EXISTS (SELECT 1 FROM [dbo].[PARTNER_DETAYLARI] WHERE [VERGI_NUMARASI] = N'ORK-DEMO-0001')
BEGIN
    INSERT INTO [dbo].[PARTNER_DETAYLARI]
    (
        [KULLANICI_ID], [FIRMA_UNVANI], [FIRMA_TURU], [VERGI_DAIRESI], [VERGI_NUMARASI],
        [FATURA_ADRESI], [FATURA_IL], [FATURA_ILCE], [YETKILI_AD_SOYAD], [YETKILI_TC_NO],
        [YETKILI_TELEFON], [YETKILI_EPOSTA], [BANKA_ADI], [IBAN], [HESAP_SAHIBI_ADI],
        [ONAY_DURUMU], [ONAY_TARIHI], [AKTIF_MI], [OTEL_TIPI_ID], [OLUSTURULMA_TARIHI]
    )
    VALUES
    (
        @PartnerUserId, N'Orkestra Demo Partner A.S.', N'Limited Sirketi', N'Demo VD', N'ORK-DEMO-0001',
        N'Taksim Demo Adres', N'Istanbul', N'Beyoglu', N'Orkestra Demo Partner', N'11111111111',
        N'5000000099', N'ork-demo-partner@otelturizm.local', N'Demo Bank', N'TR000000000000000000000001', N'Orkestra Demo Partner',
        N'Onaylandi', SYSUTCDATETIME(), 1, @HotelTypeId, SYSUTCDATETIME()
    );
    SET @PartnerId = SCOPE_IDENTITY();
END
ELSE
    SELECT @PartnerId = [ID] FROM [dbo].[PARTNER_DETAYLARI] WHERE [VERGI_NUMARASI] = N'ORK-DEMO-0001';

SET @HotelId = NULL;
SELECT @HotelId = [ID] FROM [dbo].[OTELLER] WHERE [OTEL_KODU] = @OtelKodu;

IF @HotelId IS NULL
BEGIN
    INSERT INTO [dbo].[OTELLER]
    (
        [OTEL_KODU], [PARTNER_ID], [KULLANICI_ID], [OTEL_ADI], [OTEL_TURU], [OTEL_TIPI_ID], [YILDIZ_SAYISI],
        [ULKE], [SEHIR], [ILCE], [MAHALLE], [TAM_ADRES], [ENLEM], [BOYLAM], [ULKE_ID], [SEHIR_ID], [ILCE_ID],
        [TELEFON_1], [EPOSTA], [REZERVASYON_TELEFONU], [SATIS_KONTAK_ADI], [SATIS_KONTAK_TELEFONU], [SATIS_KONTAK_EPOSTA],
        [CHECK_IN_SAATI], [CHECK_OUT_SAATI], [TOPLAM_ODA_SAYISI], [KISA_ACIKLAMA], [UZUN_ACIKLAMA], [KONUM_ACIKLAMASI],
        [VARSAYILAN_KOMISYON_ORANI], [ODEME_VADESI], [ODEME_YONTEMI], [FATURA_KESIM_TURU],
        [ORTALAMA_PUAN], [TOPLAM_YORUM_SAYISI], [YAYIN_DURUMU], [ONAY_DURUMU], [ONAY_TARIHI],
        [ONE_CIKAN_OTEL], [TAVSIYE_EDILEN_OTEL], [POPULERLIK_SIRASI], [OLUSTURULMA_TARIHI]
    )
    VALUES
    (
        @OtelKodu, @PartnerId, @PartnerUserId, N'Maidan Istanbul Boutique', N'Hotel', @HotelTypeId, 5,
        N'Turkiye', N'Istanbul', N'Beyoglu', N'Taksim', N'Istiklal Cad. Maidan Sok. No:12 Beyoglu / Istanbul',
        41.03690000, 28.98500000, @TrUlkeId, @IstanbulIlId, @BeyogluIlceId,
        N'2125558800', N'rez.maidan@demo.otelturizm.local', N'2125558801', N'Maidan Demo', N'2125558802', N'ork-demo-partner@otelturizm.local',
        '15:00:00', '12:00:00', 48,
        N'Taksim''in kalbinde butik konfor — ana sayfa ve liste vitrini demo oteli.',
        N'Maidan Istanbul Boutique, Istiklal Caddesi''ne yurume mesafesinde konumlanan 5 yildizli butik tesisimiz; superior ve deluxe odalar, spa, kapali havuz ve sehir manzarali restoraniyla misafirlerine premium bir Istanbul deneyimi sunar. Tum odalarda ucretsiz yuksek hiz Wi-Fi, minibar ve 24 saat oda servisi mevcuttur.',
        N'Taksim Meydani''na 3 dakika yuruyus, metro ve havalimani servisi kapi onu.',
        15.00, N'Cikis Gunu', N'Online', N'Otel Keser',
        9.2, 126, N'Yayında', N'Onaylandı', SYSUTCDATETIME(),
        1, 1, 100, SYSUTCDATETIME()
    );
    SET @HotelId = SCOPE_IDENTITY();
END
ELSE
BEGIN
    UPDATE [dbo].[OTELLER] SET
        [PARTNER_ID] = @PartnerId,
        [KULLANICI_ID] = @PartnerUserId,
        [OTEL_ADI] = N'Maidan Istanbul Boutique',
        [ILCE] = N'Beyoglu',
        [MAHALLE] = N'Taksim',
        [ILCE_ID] = @BeyogluIlceId,
        [KISA_ACIKLAMA] = N'Taksim''in kalbinde butik konfor — ana sayfa ve liste vitrini demo oteli.',
        [UZUN_ACIKLAMA] = N'Maidan Istanbul Boutique, Istiklal Caddesi''ne yurume mesafesinde konumlanan 5 yildizli butik tesisimiz; superior ve deluxe odalar, spa, kapali havuz ve sehir manzarali restoraniyla misafirlerine premium bir Istanbul deneyimi sunar.',
        [YAYIN_DURUMU] = N'Yayında',
        [ONAY_DURUMU] = N'Onaylandı',
        [ONAY_TARIHI] = COALESCE([ONAY_TARIHI], SYSUTCDATETIME()),
        [ONE_CIKAN_OTEL] = 1,
        [TAVSIYE_EDILEN_OTEL] = 1,
        [ORTALAMA_PUAN] = 9.2,
        [TOPLAM_YORUM_SAYISI] = 126
    WHERE [ID] = @HotelId;
END;

IF NOT EXISTS (SELECT 1 FROM [dbo].[OTEL_KULLANICI_SAHIPLIKLERI] WHERE [OTEL_ID] = @HotelId AND [KULLANICI_ID] = @PartnerUserId)
    INSERT INTO [dbo].[OTEL_KULLANICI_SAHIPLIKLERI]([OTEL_ID],[KULLANICI_ID],[PARTNER_ID],[ROL],[ANA_SORUMLU_MU],[AKTIF_MI],[OLUSTURULMA_TARIHI])
    VALUES(@HotelId, @PartnerUserId, @PartnerId, N'owner', 1, 1, SYSUTCDATETIME());

IF OBJECT_ID(N'dbo.OTEL_KOSULLARI', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM [dbo].[OTEL_KOSULLARI] WHERE [OTEL_ID] = @HotelId)
        INSERT INTO [dbo].[OTEL_KOSULLARI](
            [OTEL_ID],[IPTAL_POLITIKASI_OZET],[DETAYLI_IPTAL_KOSULLARI],[UCRETSIZ_IPTAL_SURESI],
            [ON_ODEME_GEREKLI_MI],[ON_ODEME_ORANI],[KREDI_KARTI_ILE_ODEME_KABUL],[GUNCELLENME_TARIHI]
        )
        VALUES(
            @HotelId,
            N'Giris tarihinden 24 saat onceye kadar ucretsiz iptal.',
            N'24 saat oncesine kadar ucretsiz iptal; gec iptallerde ilk gece ucreti tahsil edilir.',
            1, 0, 0.00, 1, SYSUTCDATETIME()
        );
    ELSE
        UPDATE [dbo].[OTEL_KOSULLARI]
        SET [IPTAL_POLITIKASI_OZET] = N'Giris tarihinden 24 saat onceye kadar ucretsiz iptal.',
            [UCRETSIZ_IPTAL_SURESI] = 1,
            [GUNCELLENME_TARIHI] = SYSUTCDATETIME()
        WHERE [OTEL_ID] = @HotelId;
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
    IF NOT EXISTS (SELECT 1 FROM [dbo].[ODA_OZELLIKLERI] WHERE [OZELLIK_ADI] = N'Nespresso')
        INSERT INTO [dbo].[ODA_OZELLIKLERI]([KATEGORI],[OZELLIK_ADI],[OZELLIK_IKON],[SIRALAMA],[AKTIF_MI]) VALUES (N'Konfor',N'Nespresso',N'fa-mug-hot',55,1);
END;

DECLARE @OzellikKodlari TABLE (Kod nvarchar(80) NOT NULL PRIMARY KEY);
INSERT INTO @OzellikKodlari (Kod) VALUES
(N'UCRETSIZ_WIFI'),(N'RESEPSIYON_24_SAAT'),(N'KAHVALTI'),(N'OTOPARK'),(N'RESTORAN'),
(N'FITNESS'),(N'KLIMA'),(N'SPA'),(N'HAVUZ_KAPALI'),(N'ASANSOR');

IF OBJECT_ID(N'dbo.OTEL_OZELLIK_ILISKILERI', N'U') IS NOT NULL AND OBJECT_ID(N'dbo.OTEL_OZELLIKLERI', N'U') IS NOT NULL
BEGIN
    INSERT INTO [dbo].[OTEL_OZELLIK_ILISKILERI]([OTEL_ID],[OZELLIK_ID],[AKTIF_MI])
    SELECT @HotelId, o.[ID], 1
    FROM [dbo].[OTEL_OZELLIKLERI] o
    INNER JOIN @OzellikKodlari k ON k.[Kod] = o.[OZELLIK_KODU]
    WHERE NOT EXISTS (SELECT 1 FROM [dbo].[OTEL_OZELLIK_ILISKILERI] i WHERE i.[OTEL_ID]=@HotelId AND i.[OZELLIK_ID]=o.[ID]);
END;

SET @SupRoomId = NULL;
SELECT @SupRoomId = [ID] FROM [dbo].[ODA_TIPLERI] WHERE [OTEL_ID] = @HotelId AND [ODA_TIP_KODU] = N'SUP-DEMO';
IF @SupRoomId IS NULL
BEGIN
    INSERT INTO [dbo].[ODA_TIPLERI](
        [OTEL_ID],[ODA_TIP_KODU],[ODA_ADI],[ODA_KATEGORISI],
        [MAKSIMUM_KISI_SAYISI],[MAKSIMUM_YETISKIN_SAYISI],[MAKSIMUM_COCUK_SAYISI],
        [YATAK_TIPI],[YATAK_SAYISI],[ODA_METREKARE],[MANZARA_TIPI],[OZELLIKLER],
        [STANDART_GECELIK_FIYAT],[TOPLAM_ODA_SAYISI],[AKTIF_MI],[SIRALAMA]
    )
    VALUES(
        @HotelId, N'SUP-DEMO', N'Superior Sehir Manzarali', N'Superior Oda',
        2, 2, 1, N'King', 1, 28, N'Sehir', N'Klima, Minibar, LED TV, Nespresso, ucretsiz Wi-Fi',
        @StdFiyat, 24, 1, 1
    );
    SET @SupRoomId = SCOPE_IDENTITY();
END
ELSE
    UPDATE [dbo].[ODA_TIPLERI] SET [STANDART_GECELIK_FIYAT]=@StdFiyat, [AKTIF_MI]=1, [ODA_ADI]=N'Superior Sehir Manzarali' WHERE [ID]=@SupRoomId;

SET @DlxRoomId = NULL;
SELECT @DlxRoomId = [ID] FROM [dbo].[ODA_TIPLERI] WHERE [OTEL_ID] = @HotelId AND [ODA_TIP_KODU] = N'DLX-DEMO';
IF @DlxRoomId IS NULL
BEGIN
    INSERT INTO [dbo].[ODA_TIPLERI](
        [OTEL_ID],[ODA_TIP_KODU],[ODA_ADI],[ODA_KATEGORISI],
        [MAKSIMUM_KISI_SAYISI],[MAKSIMUM_YETISKIN_SAYISI],[MAKSIMUM_COCUK_SAYISI],
        [YATAK_TIPI],[YATAK_SAYISI],[ODA_METREKARE],[BALKON_VAR_MI],[MANZARA_TIPI],[OZELLIKLER],
        [STANDART_GECELIK_FIYAT],[TOPLAM_ODA_SAYISI],[AKTIF_MI],[SIRALAMA]
    )
    VALUES(
        @HotelId, N'DLX-DEMO', N'Deluxe Bogaz Manzarali', N'Deluxe Oda',
        3, 3, 1, N'King + Sofa', 2, 38, 1, N'Bogaz', N'Jakuzi, Minibar, LED TV, balkon, Nespresso',
        @DlxFiyat, 12, 1, 2
    );
    SET @DlxRoomId = SCOPE_IDENTITY();
END
ELSE
    UPDATE [dbo].[ODA_TIPLERI] SET [STANDART_GECELIK_FIYAT]=@DlxFiyat, [AKTIF_MI]=1, [ODA_ADI]=N'Deluxe Bogaz Manzarali' WHERE [ID]=@DlxRoomId;

DECLARE @RoomIds TABLE (RoomId bigint NOT NULL PRIMARY KEY);
INSERT INTO @RoomIds VALUES (@SupRoomId), (@DlxRoomId);

DECLARE @RoomId bigint;
WHILE EXISTS (SELECT 1 FROM @RoomIds)
BEGIN
    SELECT TOP (1) @RoomId = RoomId FROM @RoomIds ORDER BY RoomId;

    IF OBJECT_ID(N'dbo.ODA_TIPI_OZELLIKLERI', N'U') IS NOT NULL
    BEGIN
        INSERT INTO [dbo].[ODA_TIPI_OZELLIKLERI]([ODA_TIP_ID],[OZELLIK_ID],[MIKTAR])
        SELECT @RoomId, oo.[ID], 1
        FROM [dbo].[ODA_OZELLIKLERI] oo
        WHERE oo.[AKTIF_MI]=1 AND oo.[OZELLIK_ADI] IN (N'Klima',N'Minibar',N'LED TV',N'Sac Kurutma Makinesi',N'Kasa',N'Nespresso')
          AND NOT EXISTS (SELECT 1 FROM [dbo].[ODA_TIPI_OZELLIKLERI] x WHERE x.[ODA_TIP_ID]=@RoomId AND x.[OZELLIK_ID]=oo.[ID]);
    END;

    DELETE FROM @RoomIds WHERE RoomId = @RoomId;
END;

SET @d = 0;
WHILE @d < 90
BEGIN
    SET @Tarih = DATEADD(DAY, @d, @Today);
    SET @IsWeekend = CASE WHEN DATEPART(WEEKDAY, @Tarih) IN (6, 7) THEN 1 ELSE 0 END;

    SET @Indirimli = CASE
        WHEN @IsWeekend = 1 THEN CAST(@StdFiyat * 0.85 AS decimal(10,2))
        WHEN @d >= 14 THEN CAST(@StdFiyat * 0.80 AS decimal(10,2))
        ELSE NULL
    END;

    IF NOT EXISTS (SELECT 1 FROM [dbo].[ODA_FIYAT_MUSAITLIK] WHERE [OTEL_ID]=@HotelId AND [ODA_TIP_ID]=@SupRoomId AND [TARIH]=@Tarih)
        INSERT INTO [dbo].[ODA_FIYAT_MUSAITLIK]([ODA_TIP_ID],[OTEL_ID],[TARIH],[GECELIK_FIYAT],[INDIRIMLI_FIYAT],[INDIRIM_ID],[KAMPANYA_ID],[TOPLAM_ODA_SAYISI],[KAPALI_SATIS],[KAMPANYA_ETIKETI])
        VALUES(@SupRoomId, @HotelId, @Tarih, @StdFiyat, @Indirimli,
            CASE WHEN @Indirimli IS NOT NULL AND @IsWeekend=1 THEN @WeekendIndirimId WHEN @Indirimli IS NOT NULL AND @d>=14 THEN @ErkenIndirimId ELSE NULL END,
            @KampanyaId, 24, 0, CASE WHEN @KampanyaId IS NOT NULL THEN N'SEHIR' ELSE NULL END);
    ELSE
        UPDATE [dbo].[ODA_FIYAT_MUSAITLIK]
        SET [GECELIK_FIYAT]=@StdFiyat,
            [INDIRIMLI_FIYAT]=@Indirimli,
            [INDIRIM_ID]=CASE WHEN @Indirimli IS NOT NULL AND @IsWeekend=1 THEN @WeekendIndirimId WHEN @Indirimli IS NOT NULL AND @d>=14 THEN @ErkenIndirimId ELSE NULL END,
            [KAMPANYA_ID]=COALESCE(@KampanyaId,[KAMPANYA_ID]),
            [KAPALI_SATIS]=0
        WHERE [OTEL_ID]=@HotelId AND [ODA_TIP_ID]=@SupRoomId AND [TARIH]=@Tarih;

    SET @Indirimli = CASE
        WHEN @IsWeekend = 1 THEN CAST(@DlxFiyat * 0.85 AS decimal(10,2))
        WHEN @d >= 14 THEN CAST(@DlxFiyat * 0.80 AS decimal(10,2))
        ELSE NULL
    END;

    IF NOT EXISTS (SELECT 1 FROM [dbo].[ODA_FIYAT_MUSAITLIK] WHERE [OTEL_ID]=@HotelId AND [ODA_TIP_ID]=@DlxRoomId AND [TARIH]=@Tarih)
        INSERT INTO [dbo].[ODA_FIYAT_MUSAITLIK]([ODA_TIP_ID],[OTEL_ID],[TARIH],[GECELIK_FIYAT],[INDIRIMLI_FIYAT],[INDIRIM_ID],[KAMPANYA_ID],[TOPLAM_ODA_SAYISI],[KAPALI_SATIS],[KAMPANYA_ETIKETI])
        VALUES(@DlxRoomId, @HotelId, @Tarih, @DlxFiyat, @Indirimli,
            CASE WHEN @Indirimli IS NOT NULL AND @IsWeekend=1 THEN @WeekendIndirimId WHEN @Indirimli IS NOT NULL AND @d>=14 THEN @ErkenIndirimId ELSE NULL END,
            @KampanyaId, 12, 0, CASE WHEN @KampanyaId IS NOT NULL THEN N'SEHIR' ELSE NULL END);
    ELSE
        UPDATE [dbo].[ODA_FIYAT_MUSAITLIK]
        SET [GECELIK_FIYAT]=@DlxFiyat,
            [INDIRIMLI_FIYAT]=@Indirimli,
            [INDIRIM_ID]=CASE WHEN @Indirimli IS NOT NULL AND @IsWeekend=1 THEN @WeekendIndirimId WHEN @Indirimli IS NOT NULL AND @d>=14 THEN @ErkenIndirimId ELSE NULL END,
            [KAMPANYA_ID]=COALESCE(@KampanyaId,[KAMPANYA_ID]),
            [KAPALI_SATIS]=0
        WHERE [OTEL_ID]=@HotelId AND [ODA_TIP_ID]=@DlxRoomId AND [TARIH]=@Tarih;

    SET @d += 1;
END;

IF @KampanyaId IS NOT NULL AND OBJECT_ID(N'dbo.KAMPANYA_OTELLER', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM [dbo].[KAMPANYA_OTELLER] WHERE [KAMPANYA_ID]=@KampanyaId AND [OTEL_ID]=@HotelId)
        INSERT INTO [dbo].[KAMPANYA_OTELLER]([KAMPANYA_ID],[OTEL_ID],[PARTNER_ID],[KATILIM_DURUMU],[KATILIM_KAYNAGI],[BASLANGIC_TARIHI],[BITIS_TARIHI],[ADMIN_ONAY_TARIHI],[PARTNER_ONAY_TARIHI],[OLUSTURULMA_TARIHI])
        VALUES(@KampanyaId, @HotelId, @PartnerId, N'Aktif', N'MaidanSeed', @KampanyaBas, @KampanyaBit, SYSUTCDATETIME(), SYSUTCDATETIME(), SYSUTCDATETIME());
    ELSE
        UPDATE [dbo].[KAMPANYA_OTELLER] SET [KATILIM_DURUMU]=N'Aktif', [PARTNER_ID]=@PartnerId WHERE [KAMPANYA_ID]=@KampanyaId AND [OTEL_ID]=@HotelId;
END;

DECLARE @CoverUrl nvarchar(500), @Url nvarchar(500), @RoomCover nvarchar(500), @RoomUrl2 nvarchar(500), @i int;

IF OBJECT_ID(N'dbo.OTEL_GORSELLERI', N'U') IS NOT NULL
BEGIN
    SET @CoverUrl = N'/uploads/images/' + CAST(@HotelId AS nvarchar(20)) + N'/hotel/demo-cover.webp';
    IF NOT EXISTS (SELECT 1 FROM [dbo].[OTEL_GORSELLERI] WHERE [OTEL_ID]=@HotelId AND [GORSEL_URL]=@CoverUrl)
    BEGIN
        INSERT INTO [dbo].[OTEL_GORSELLERI]([OTEL_ID],[GORSEL_URL],[GORSEL_TURU],[BASLIK],[KAPAK_FOTOGRAFI_MI],[ONE_CIKAN],[SIRALAMA],[ONAY_DURUMU])
        VALUES(@HotelId,@CoverUrl,N'Genel Alan',N'Kapak',1,1,0,N'Onaylandı');
        UPDATE [dbo].[OTELLER] SET [KAPAK_FOTOGRAFI]=@CoverUrl WHERE [ID]=@HotelId;
    END;

    SET @i = 1;
    WHILE @i <= 3
    BEGIN
        SET @Url = N'/uploads/images/' + CAST(@HotelId AS nvarchar(20)) + N'/hotel/demo-' + RIGHT(N'0'+CAST(@i AS nvarchar(2)),2) + N'.webp';
        IF NOT EXISTS (SELECT 1 FROM [dbo].[OTEL_GORSELLERI] WHERE [OTEL_ID]=@HotelId AND [GORSEL_URL]=@Url)
            INSERT INTO [dbo].[OTEL_GORSELLERI]([OTEL_ID],[GORSEL_URL],[GORSEL_TURU],[BASLIK],[KAPAK_FOTOGRAFI_MI],[SIRALAMA],[ONAY_DURUMU])
            VALUES(@HotelId,@Url,N'Genel Alan',CONCAT(N'Galeri ',@i),0,@i,N'Onaylandı');
        SET @i += 1;
    END;
END;

DECLARE @RoomLoop TABLE (RoomId bigint NOT NULL PRIMARY KEY);
INSERT INTO @RoomLoop VALUES (@SupRoomId), (@DlxRoomId);
WHILE EXISTS (SELECT 1 FROM @RoomLoop)
BEGIN
    SELECT TOP (1) @RoomId = RoomId FROM @RoomLoop ORDER BY RoomId;

    IF OBJECT_ID(N'dbo.ODA_GORSELLERI', N'U') IS NOT NULL
    BEGIN
        SET @RoomCover = N'/uploads/images/' + CAST(@HotelId AS nvarchar(20)) + N'/rooms/' + CAST(@RoomId AS nvarchar(20)) + N'/demo-room-cover.webp';
        IF NOT EXISTS (SELECT 1 FROM [dbo].[ODA_GORSELLERI] WHERE [ODA_TIP_ID]=@RoomId AND [GORSEL_URL]=@RoomCover)
            INSERT INTO [dbo].[ODA_GORSELLERI]([ODA_TIP_ID],[GORSEL_URL],[BASLIK],[KAPAK_FOTOGRAFI_MI],[SIRALAMA],[ONAY_DURUMU])
            VALUES(@RoomId,@RoomCover,N'Oda kapak',1,0,N'Onaylandı');

        SET @RoomUrl2 = N'/uploads/images/' + CAST(@HotelId AS nvarchar(20)) + N'/rooms/' + CAST(@RoomId AS nvarchar(20)) + N'/demo-room-02.webp';
        IF NOT EXISTS (SELECT 1 FROM [dbo].[ODA_GORSELLERI] WHERE [ODA_TIP_ID]=@RoomId AND [GORSEL_URL]=@RoomUrl2)
            INSERT INTO [dbo].[ODA_GORSELLERI]([ODA_TIP_ID],[GORSEL_URL],[BASLIK],[KAPAK_FOTOGRAFI_MI],[SIRALAMA],[ONAY_DURUMU])
            VALUES(@RoomId,@RoomUrl2,N'Oda detay',0,1,N'Onaylandı');
    END;

    DELETE FROM @RoomLoop WHERE RoomId = @RoomId;
END;

PRINT N'Maidan demo otel seed tamam. OTEL_ID=' + CAST(@HotelId AS nvarchar(20)) + N' KOD=' + @OtelKodu;
