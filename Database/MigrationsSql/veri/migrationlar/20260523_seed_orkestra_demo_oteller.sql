-- Idempotent: yerel OtelDetay / liste SS için 3 yayında demo otel (+ partner + oda + 30 gün fiyat)
-- Uygulama: sqlcmd -S "(localdb)\MSSQLLocalDB" -d otelturizm_2026db -i bu dosya
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;

DECLARE @TrUlkeId bigint = (SELECT TOP (1) id FROM [dbo].[ULKELER] ORDER BY CASE WHEN [ISO2_KODU] = N'TR' THEN 0 ELSE 1 END, id);
DECLARE @IstanbulIlId bigint = (SELECT TOP (1) id FROM [dbo].[ILLER] WHERE [IL_ADI] LIKE N'%stanbul%' ORDER BY id);
DECLARE @KartalIlceId bigint = (
    SELECT TOP (1) id FROM [dbo].[ILCELER]
    WHERE [IL_ID] = @IstanbulIlId AND ([ILCE_ADI] LIKE N'Kartal%' OR [ILCE_ADI] LIKE N'kartal%')
    ORDER BY id);

IF @TrUlkeId IS NULL
BEGIN
    RAISERROR(N'ULKELER bos; once 20260522_seed_ulkeler_dunya_listesi.sql uygulayin.', 16, 1);
    RETURN;
END;

IF NOT EXISTS (SELECT 1 FROM [dbo].[OTEL_TIPLERI] WHERE [KOD] = N'HOTEL')
BEGIN
    INSERT INTO [dbo].[OTEL_TIPLERI] ([KOD], [TIP_ADI], [ACIKLAMA], [AKTIF_MI], [SIRALAMA])
    VALUES (N'HOTEL', N'OTEL', N'Orkestra demo seed tipi', 1, 1);
END;

DECLARE @HotelTypeId int = (SELECT TOP (1) id FROM [dbo].[OTEL_TIPLERI] WHERE [KOD] = N'HOTEL' ORDER BY id);
DECLARE @PartnerUserId bigint;
DECLARE @PartnerId bigint;
DECLARE @DemoPasswordHash nvarchar(64) = LOWER(CONVERT(VARCHAR(64), HASHBYTES('SHA2_256', CONVERT(nvarchar(max), N'Demo123!')), 2));

IF NOT EXISTS (SELECT 1 FROM [dbo].[KULLANICILAR] WHERE [EPOSTA] = N'ork-demo-partner@otelturizm.local')
BEGIN
    INSERT INTO [dbo].[KULLANICILAR]
        ([AD_SOYAD], [EPOSTA], [TELEFON], [SIFRE], [ROL], [HESAP_DURUMU], [KAYIT_KAYNAGI], [OLUSTURULMA_TARIHI])
    VALUES
        (N'Orkestra Demo Partner', N'ork-demo-partner@otelturizm.local', N'5000000099', @DemoPasswordHash, N'partner', 1, N'OrkestraSeed', SYSUTCDATETIME());
    SET @PartnerUserId = SCOPE_IDENTITY();
END
ELSE
    SELECT @PartnerUserId = id FROM [dbo].[KULLANICILAR] WHERE [EPOSTA] = N'ork-demo-partner@otelturizm.local';

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
        N'Kartal Demo Adres', N'Istanbul', N'Kartal', N'Orkestra Demo Partner', N'11111111111',
        N'5000000099', N'ork-demo-partner@otelturizm.local', N'Demo Bank', N'TR000000000000000000000001', N'Orkestra Demo Partner',
        N'Onaylandi', SYSUTCDATETIME(), 1, @HotelTypeId, SYSUTCDATETIME()
    );
    SET @PartnerId = SCOPE_IDENTITY();
END
ELSE
    SELECT @PartnerId = id FROM [dbo].[PARTNER_DETAYLARI] WHERE [VERGI_NUMARASI] = N'ORK-DEMO-0001';

DECLARE @Hotels TABLE
(
    OtelKodu nvarchar(32) NOT NULL PRIMARY KEY,
    OtelAdi nvarchar(255) NOT NULL,
    SlugHint nvarchar(80) NOT NULL,
    Ilce nvarchar(50) NOT NULL,
    Mahalle nvarchar(100) NULL,
    Enlem decimal(10,8) NOT NULL,
    Boylam decimal(11,8) NOT NULL,
    Yildiz tinyint NOT NULL,
    KisaAciklama nvarchar(500) NOT NULL
);

INSERT INTO @Hotels VALUES
(N'ORK-SEED-001', N'Orkestra Bogaz Otel', N'orkestra-bogaz-otel', N'Besiktas', N'Ortakoy', 41.05530000, 29.02740000, 5, N'Bogaz manzarali demo otel — FE-CTO OtelDetay SS.'),
(N'ORK-SEED-002', N'Orkestra Taksim Suites', N'orkestra-taksim-suites', N'Beyoglu', N'Taksim', 41.03700000, 28.98500000, 4, N'Sehir merkezi demo suite — liste/harita test.'),
(N'ORK-SEED-003', N'Orkestra Kartal Business', N'orkestra-kartal-business', N'Kartal', N'Atalar', 40.90610000, 29.28090000, 4, N'Is oteli demo — satis paneli rezervasyon test.');

DECLARE @OtelKodu nvarchar(32);
DECLARE @OtelAdi nvarchar(255);
DECLARE @Ilce nvarchar(50);
DECLARE @Mahalle nvarchar(100);
DECLARE @Enlem decimal(10,8);
DECLARE @Boylam decimal(11,8);
DECLARE @Yildiz tinyint;
DECLARE @KisaAciklama nvarchar(500);
DECLARE @HotelId bigint;
DECLARE @RoomTypeId bigint;
DECLARE @Today date = CONVERT(date, SYSUTCDATETIME());
DECLARE @d int;
DECLARE @Tarih date;

WHILE EXISTS (SELECT 1 FROM @Hotels)
BEGIN
    SELECT TOP (1)
        @OtelKodu = OtelKodu,
        @OtelAdi = OtelAdi,
        @Ilce = Ilce,
        @Mahalle = Mahalle,
        @Enlem = Enlem,
        @Boylam = Boylam,
        @Yildiz = Yildiz,
        @KisaAciklama = KisaAciklama
    FROM @Hotels
    ORDER BY OtelKodu;

    SET @HotelId = NULL;
    SELECT @HotelId = id FROM [dbo].[OTELLER] WHERE [OTEL_KODU] = @OtelKodu;

    IF @HotelId IS NULL
    BEGIN
        INSERT INTO [dbo].[OTELLER]
        (
            [OTEL_KODU], [PARTNER_ID], [KULLANICI_ID], [OTEL_ADI], [OTEL_TURU], [OTEL_TIPI_ID], [YILDIZ_SAYISI],
            [ULKE], [SEHIR], [ILCE], [MAHALLE], [TAM_ADRES], [ENLEM], [BOYLAM], [ULKE_ID], [SEHIR_ID], [ILCE_ID],
            [TELEFON_1], [EPOSTA], [REZERVASYON_TELEFONU], [SATIS_KONTAK_ADI], [SATIS_KONTAK_TELEFONU], [SATIS_KONTAK_EPOSTA],
            [CHECK_IN_SAATI], [CHECK_OUT_SAATI], [TOPLAM_ODA_SAYISI], [KISA_ACIKLAMA], [UZUN_ACIKLAMA],
            [VARSAYILAN_KOMISYON_ORANI], [ODEME_VADESI], [ODEME_YONTEMI], [FATURA_KESIM_TURU],
            [ORTALAMA_PUAN], [YAYIN_DURUMU], [ONAY_DURUMU], [ONAY_TARIHI], [ONE_CIKAN_OTEL], [OLUSTURULMA_TARIHI]
        )
        VALUES
        (
            @OtelKodu, @PartnerId, @PartnerUserId, @OtelAdi, N'Hotel', @HotelTypeId, @Yildiz,
            N'Turkiye', N'Istanbul', @Ilce, @Mahalle, CONCAT(@Mahalle, N' Mah. Demo Sok. No:1 ', @Ilce, N' / Istanbul'),
            @Enlem, @Boylam, @TrUlkeId, @IstanbulIlId, @KartalIlceId,
            N'2125550100', CONCAT(N'rez.', LOWER(REPLACE(@OtelKodu, N'-', N'')), N'@demo.otelturizm.local'),
            N'2125550101', N'Orkestra Demo', N'2125550102', N'ork-demo-partner@otelturizm.local',
            '14:00:00', '12:00:00', 24, @KisaAciklama, @KisaAciklama,
            15.00, N'Cikis Gunu', N'Havale/EFT', N'Otel Keser',
            8.7, N'Yayında', N'Onaylandı', SYSUTCDATETIME(), 1, SYSUTCDATETIME()
        );
        SET @HotelId = SCOPE_IDENTITY();

        IF NOT EXISTS (SELECT 1 FROM [dbo].[OTEL_KULLANICI_SAHIPLIKLERI] WHERE [OTEL_ID] = @HotelId AND [KULLANICI_ID] = @PartnerUserId)
        BEGIN
            INSERT INTO [dbo].[OTEL_KULLANICI_SAHIPLIKLERI]
                ([OTEL_ID], [KULLANICI_ID], [PARTNER_ID], [ROL], [ANA_SORUMLU_MU], [AKTIF_MI], [OLUSTURULMA_TARIHI])
            VALUES (@HotelId, @PartnerUserId, @PartnerId, N'owner', 1, 1, SYSUTCDATETIME());
        END
    END

    SET @RoomTypeId = NULL;
    SELECT @RoomTypeId = id FROM [dbo].[ODA_TIPLERI] WHERE [OTEL_ID] = @HotelId AND [ODA_TIP_KODU] = N'STD-DEMO';

    IF @RoomTypeId IS NULL
    BEGIN
        INSERT INTO [dbo].[ODA_TIPLERI]
        (
            [OTEL_ID], [ODA_TIP_KODU], [ODA_ADI], [ODA_KATEGORISI],
            [MAKSIMUM_KISI_SAYISI], [MAKSIMUM_YETISKIN_SAYISI], [MAKSIMUM_COCUK_SAYISI],
            [YATAK_TIPI], [YATAK_SAYISI], [STANDART_GECELIK_FIYAT], [TOPLAM_ODA_SAYISI], [AKTIF_MI], [SIRALAMA]
        )
        VALUES
        (
            @HotelId, N'STD-DEMO', N'Demo Standart Oda', N'Standart',
            2, 2, 1, N'Queen', 1, 4500.00, 12, 1, 1
        );
        SET @RoomTypeId = SCOPE_IDENTITY();
    END

    SET @d = 0;
    WHILE @d < 30
    BEGIN
        SET @Tarih = DATEADD(DAY, @d, @Today);
        IF NOT EXISTS (
            SELECT 1 FROM [dbo].[ODA_FIYAT_MUSAITLIK]
            WHERE [OTEL_ID] = @HotelId AND [ODA_TIP_ID] = @RoomTypeId AND [TARIH] = @Tarih)
        BEGIN
            INSERT INTO [dbo].[ODA_FIYAT_MUSAITLIK]
                ([ODA_TIP_ID], [OTEL_ID], [TARIH], [GECELIK_FIYAT], [TOPLAM_ODA_SAYISI], [KAPALI_SATIS])
            VALUES (@RoomTypeId, @HotelId, @Tarih, 4500.00, 12, 0);
        END
        SET @d += 1;
    END

    DELETE FROM @Hotels WHERE OtelKodu = @OtelKodu;
END

DECLARE @OtelCount int = (SELECT COUNT(*) FROM [dbo].[OTELLER]);
PRINT N'Orkestra demo otel seed tamam. OTELLER sayisi: ' + CAST(@OtelCount AS nvarchar(20));
