-- Idempotent: 10 Istanbul ilce demo oteli (filtre/anasayfa test) + mevcut 3 otelin ILCE_ID duzeltmesi
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;

DECLARE @TrUlkeId bigint = (SELECT TOP (1) [ID] FROM [dbo].[ULKELER] WHERE [ISO2_KODU] = N'TR' ORDER BY [ID]);
DECLARE @IstanbulIlId bigint = (SELECT TOP (1) [ID] FROM [dbo].[ILLER] WHERE [IL_ADI] LIKE N'%stanbul%' ORDER BY [ID]);

IF @TrUlkeId IS NULL OR @IstanbulIlId IS NULL
BEGIN
    RAISERROR(N'ULKELER/ILLER eksik; once geo seed uygulayin.', 16, 1);
    RETURN;
END;

DECLARE @PartnerUserId bigint = (SELECT TOP (1) [ID] FROM [dbo].[KULLANICILAR] WHERE [EPOSTA] = N'ork-demo-partner@otelturizm.local');
DECLARE @PartnerId bigint = (SELECT TOP (1) [ID] FROM [dbo].[PARTNER_DETAYLARI] WHERE [VERGI_NUMARASI] = N'ORK-DEMO-0001');
DECLARE @HotelTypeId int = (SELECT TOP (1) [ID] FROM [dbo].[OTEL_TIPLERI] WHERE [KOD] = N'HOTEL' ORDER BY [ID]);
DECLARE @DemoPasswordHash nvarchar(64) = LOWER(CONVERT(VARCHAR(64), HASHBYTES('SHA2_256', CONVERT(nvarchar(max), N'Demo123!')), 2));

IF @PartnerUserId IS NULL
BEGIN
    INSERT INTO [dbo].[KULLANICILAR]([AD_SOYAD],[EPOSTA],[TELEFON],[SIFRE],[ROL],[HESAP_DURUMU],[KAYIT_KAYNAGI],[OLUSTURULMA_TARIHI])
    VALUES (N'Orkestra Demo Partner', N'ork-demo-partner@otelturizm.local', N'5000000099', @DemoPasswordHash, N'partner', 1, N'OrkestraSeed', SYSUTCDATETIME());
    SET @PartnerUserId = SCOPE_IDENTITY();
END

IF @PartnerId IS NULL
BEGIN
    INSERT INTO [dbo].[PARTNER_DETAYLARI]([KULLANICI_ID],[FIRMA_UNVANI],[FIRMA_TURU],[VERGI_DAIRESI],[VERGI_NUMARASI],[FATURA_ADRESI],[FATURA_IL],[FATURA_ILCE],[YETKILI_AD_SOYAD],[YETKILI_TC_NO],[YETKILI_TELEFON],[YETKILI_EPOSTA],[BANKA_ADI],[IBAN],[HESAP_SAHIBI_ADI],[ONAY_DURUMU],[ONAY_TARIHI],[AKTIF_MI],[OTEL_TIPI_ID],[OLUSTURULMA_TARIHI])
    VALUES (@PartnerUserId, N'Orkestra Demo Partner A.S.', N'Limited', N'Demo', N'ORK-DEMO-0001', N'Demo', N'Istanbul', N'Kadikoy', N'Demo Partner', N'11111111111', N'5000000099', N'ork-demo-partner@otelturizm.local', N'Demo Bank', N'TR000000000000000000000001', N'Demo', N'Onaylandi', SYSUTCDATETIME(), 1, @HotelTypeId, SYSUTCDATETIME());
    SET @PartnerId = SCOPE_IDENTITY();
END

DECLARE @Hotels TABLE (
    OtelKodu nvarchar(32) NOT NULL PRIMARY KEY,
    OtelAdi nvarchar(200) NOT NULL,
    SlugHint nvarchar(80) NOT NULL,
    IlceAdi nvarchar(50) NOT NULL,
    Mahalle nvarchar(80) NULL,
    Enlem decimal(10,8) NOT NULL,
    Boylam decimal(11,8) NOT NULL,
    Yildiz tinyint NOT NULL,
    Fiyat decimal(10,2) NOT NULL,
    OneCikan bit NOT NULL,
    Tavsiye bit NOT NULL,
    Puan decimal(3,1) NOT NULL,
    Havuz bit NOT NULL
);

INSERT INTO @Hotels VALUES
(N'ORK-SEED-001', N'Orkestra Bogaz Otel', N'orkestra-bogaz-otel', N'Besiktas', N'Ortakoy', 41.0553, 29.0274, 5, 6200, 1, 1, 9.1, 1),
(N'ORK-SEED-002', N'Orkestra Taksim Suites', N'orkestra-taksim-suites', N'Beyoglu', N'Taksim', 41.0370, 28.9850, 4, 4800, 1, 1, 8.8, 0),
(N'ORK-SEED-003', N'Orkestra Kartal Business', N'orkestra-kartal-business', N'Kartal', N'Atalar', 40.9061, 29.2809, 4, 2900, 0, 1, 8.2, 0),
(N'ORK-SEED-004', N'Orkestra Kadikoy Marina', N'orkestra-kadikoy-marina', N'Kadikoy', N'Moda', 40.9840, 29.0260, 4, 4100, 1, 0, 8.5, 1),
(N'ORK-SEED-005', N'Orkestra Sisli Park', N'orkestra-sisli-park', N'Sisli', N'Mecidiyekoy', 41.0720, 28.9940, 4, 3200, 0, 0, 8.0, 0),
(N'ORK-SEED-006', N'Orkestra Uskudar Konak', N'orkestra-uskudar-konak', N'Uskudar', N'Cengelkoy', 41.0500, 29.0600, 3, 3500, 0, 1, 8.3, 1),
(N'ORK-SEED-007', N'Orkestra Fatih Heritage', N'orkestra-fatih-heritage', N'Fatih', N'Sultanahmet', 41.0055, 28.9768, 4, 5200, 1, 0, 9.0, 0),
(N'ORK-SEED-008', N'Orkestra Bakirkoy Sahil', N'orkestra-bakirkoy-sahil', N'Bakirkoy', N'Atakoy', 40.9720, 28.8700, 4, 4400, 0, 1, 8.4, 1),
(N'ORK-SEED-009', N'Orkestra Maltepe City', N'orkestra-maltepe-city', N'Maltepe', N'Cevizli', 40.9350, 29.1550, 3, 3100, 0, 0, 7.9, 0),
(N'ORK-SEED-010', N'Orkestra Atasehir Tower', N'orkestra-atasehir-tower', N'Atasehir', N'Kucukbakkalkoy', 40.9920, 29.1240, 5, 5800, 1, 1, 8.9, 0);

DECLARE @OtelKodu nvarchar(32), @OtelAdi nvarchar(200), @IlceAdi nvarchar(50), @Mahalle nvarchar(80);
DECLARE @Enlem decimal(10,8), @Boylam decimal(11,8), @Yildiz tinyint, @Fiyat decimal(10,2);
DECLARE @OneCikan bit, @Tavsiye bit, @Puan decimal(3,1), @Havuz bit;
DECLARE @IlceId bigint, @HotelId bigint, @RoomTypeId bigint, @Today date = CAST(SYSUTCDATETIME() AS date), @d int, @Tarih date;
WHILE EXISTS (SELECT 1 FROM @Hotels)
BEGIN
    SELECT TOP (1) @OtelKodu=OtelKodu, @OtelAdi=OtelAdi, @IlceAdi=IlceAdi, @Mahalle=Mahalle,
        @Enlem=Enlem, @Boylam=Boylam, @Yildiz=Yildiz, @Fiyat=Fiyat,
        @OneCikan=OneCikan, @Tavsiye=Tavsiye, @Puan=Puan, @Havuz=Havuz
    FROM @Hotels ORDER BY OtelKodu;

    SET @IlceId = (
        SELECT TOP (1) c.[ID] FROM [dbo].[ILCELER] c
        WHERE c.[IL_ID]=@IstanbulIlId AND (
            c.[ILCE_ADI]=@IlceAdi OR c.[ILCE_ADI] LIKE @IlceAdi + N'%'
            OR REPLACE(c.[ILCE_ADI], N'ğ', N'g') LIKE REPLACE(@IlceAdi, N'ğ', N'g') + N'%')
        ORDER BY c.[ID]);

    IF @IlceId IS NULL SET @IlceId = (SELECT TOP (1) [ID] FROM [dbo].[ILCELER] WHERE [IL_ID]=@IstanbulIlId ORDER BY [ID]);

    SELECT @HotelId = [ID] FROM [dbo].[OTELLER] WHERE [OTEL_KODU]=@OtelKodu;

    IF @HotelId IS NULL
    BEGIN
        INSERT INTO [dbo].[OTELLER](
            [OTEL_KODU],[PARTNER_ID],[KULLANICI_ID],[OTEL_ADI],[OTEL_TURU],[OTEL_TIPI_ID],[YILDIZ_SAYISI],
            [ULKE],[SEHIR],[ILCE],[MAHALLE],[TAM_ADRES],[ENLEM],[BOYLAM],[ULKE_ID],[SEHIR_ID],[ILCE_ID],
            [TELEFON_1],[EPOSTA],[REZERVASYON_TELEFONU],[SATIS_KONTAK_ADI],[SATIS_KONTAK_TELEFONU],[SATIS_KONTAK_EPOSTA],
            [CHECK_IN_SAATI],[CHECK_OUT_SAATI],[TOPLAM_ODA_SAYISI],[KISA_ACIKLAMA],[UZUN_ACIKLAMA],
            [VARSAYILAN_KOMISYON_ORANI],[ODEME_VADESI],[ODEME_YONTEMI],[FATURA_KESIM_TURU],
            [ORTALAMA_PUAN],[TOPLAM_YORUM_SAYISI],[YAYIN_DURUMU],[ONAY_DURUMU],[ONAY_TARIHI],[ONE_CIKAN_OTEL],[TAVSIYE_EDILEN_OTEL],[OLUSTURULMA_TARIHI])
        VALUES(
            @OtelKodu,@PartnerId,@PartnerUserId,@OtelAdi,N'Hotel',@HotelTypeId,@Yildiz,
            N'Turkiye',N'Istanbul',@IlceAdi,@Mahalle,CONCAT(@Mahalle,N', ',@IlceAdi,N', Istanbul'),
            @Enlem,@Boylam,@TrUlkeId,@IstanbulIlId,@IlceId,
            N'2125550000',CONCAT(N'rez.',LOWER(REPLACE(@OtelKodu,N'-',N'')),N'@demo.otelturizm.local'),
            N'2125550001',N'Orkestra Demo',N'2125550002',N'ork-demo-partner@otelturizm.local',
            '14:00:00','12:00:00',20,N'Anasayfa filtre demo oteli.',N'Anasayfa filtre demo oteli.',
            15.00,N'Ayın 15''i',N'Online',N'Otel Keser',
            @Puan,120,N'Yayında',N'Onaylandı',SYSUTCDATETIME(),@OneCikan,@Tavsiye,SYSUTCDATETIME());
        SET @HotelId = SCOPE_IDENTITY();
    END
    ELSE
    BEGIN
        UPDATE [dbo].[OTELLER] SET
            [ILCE]=@IlceAdi,[MAHALLE]=@Mahalle,[ILCE_ID]=@IlceId,[SEHIR_ID]=@IstanbulIlId,[ULKE_ID]=@TrUlkeId,
            [ENLEM]=@Enlem,[BOYLAM]=@Boylam,[ONE_CIKAN_OTEL]=@OneCikan,[TAVSIYE_EDILEN_OTEL]=@Tavsiye,
            [ORTALAMA_PUAN]=@Puan,[ODEME_VADESI]=N'Ayın 15''i',[YAYIN_DURUMU]=N'Yayında',[ONAY_DURUMU]=N'Onaylandı'
        WHERE [ID]=@HotelId;
    END

    IF NOT EXISTS (SELECT 1 FROM [dbo].[OTEL_KULLANICI_SAHIPLIKLERI] WHERE [OTEL_ID]=@HotelId AND [KULLANICI_ID]=@PartnerUserId)
        INSERT INTO [dbo].[OTEL_KULLANICI_SAHIPLIKLERI]([OTEL_ID],[KULLANICI_ID],[PARTNER_ID],[ROL],[ANA_SORUMLU_MU],[AKTIF_MI],[OLUSTURULMA_TARIHI])
        VALUES(@HotelId,@PartnerUserId,@PartnerId,N'owner',1,1,SYSUTCDATETIME());

    -- Otel ozellikleri: 20260523_seed_demo_otel_medya_ve_ozellikler.sql (Havuz vb.)

    SELECT @RoomTypeId = [ID] FROM [dbo].[ODA_TIPLERI] WHERE [OTEL_ID]=@HotelId AND [ODA_TIP_KODU]=N'STD-DEMO';
    IF @RoomTypeId IS NULL
    BEGIN
        INSERT INTO [dbo].[ODA_TIPLERI]([OTEL_ID],[ODA_TIP_KODU],[ODA_ADI],[ODA_KATEGORISI],[MAKSIMUM_KISI_SAYISI],[STANDART_GECELIK_FIYAT],[TOPLAM_ODA_SAYISI],[AKTIF_MI],[SIRALAMA])
        VALUES(@HotelId,N'STD-DEMO',N'Standart',N'Standart Oda',2,@Fiyat,10,1,1);
        SET @RoomTypeId = SCOPE_IDENTITY();
    END

    SET @d=0;
    WHILE @d < 45
    BEGIN
        SET @Tarih = DATEADD(DAY,@d,@Today);
        IF NOT EXISTS (SELECT 1 FROM [dbo].[ODA_FIYAT_MUSAITLIK] WHERE [OTEL_ID]=@HotelId AND [ODA_TIP_ID]=@RoomTypeId AND [TARIH]=@Tarih)
            INSERT INTO [dbo].[ODA_FIYAT_MUSAITLIK]([ODA_TIP_ID],[OTEL_ID],[TARIH],[GECELIK_FIYAT],[TOPLAM_ODA_SAYISI],[KAPALI_SATIS])
            VALUES(@RoomTypeId,@HotelId,@Tarih,@Fiyat,10,0);
        SET @d += 1;
    END

    DELETE FROM @Hotels WHERE OtelKodu=@OtelKodu;
END

DECLARE @OtelCountAfter int = (SELECT COUNT(*) FROM [dbo].[OTELLER]);
PRINT N'Istanbul 10 ilce seed tamam. OTELLER: ' + CAST(@OtelCountAfter AS nvarchar(12));
