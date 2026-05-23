-- Kalan demo oteller (006-010) — fiyat dongusu yok (hizli)
SET NOCOUNT ON; SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON; SET ANSI_NULLS ON; SET ANSI_PADDING ON; SET ANSI_WARNINGS ON; SET ARITHABORT ON; SET CONCAT_NULL_YIELDS_NULL ON;

DECLARE @PartnerUserId bigint = (SELECT TOP 1 [ID] FROM [dbo].[KULLANICILAR] WHERE [EPOSTA]=N'ork-demo-partner@otelturizm.local');
DECLARE @PartnerId bigint = (SELECT TOP 1 [ID] FROM [dbo].[PARTNER_DETAYLARI] WHERE [VERGI_NUMARASI]=N'ORK-DEMO-0001');
DECLARE @HotelTypeId int = (SELECT TOP 1 [ID] FROM [dbo].[OTEL_TIPLERI] WHERE [KOD]=N'HOTEL');
DECLARE @TrUlkeId bigint = (SELECT TOP 1 [ID] FROM [dbo].[ULKELER] WHERE [ISO2_KODU]=N'TR');
DECLARE @IstanbulIlId bigint = (SELECT TOP 1 [ID] FROM [dbo].[ILLER] WHERE [IL_ADI] LIKE N'%stanbul%');

DECLARE @Rows TABLE (Kod nvarchar(32), Ad nvarchar(200), Ilce nvarchar(50), Mahalle nvarchar(80), Fiyat decimal(10,2));
INSERT INTO @Rows VALUES
(N'ORK-SEED-006',N'Orkestra Uskudar Konak',N'Uskudar',N'Cengelkoy',3500),
(N'ORK-SEED-007',N'Orkestra Fatih Heritage',N'Fatih',N'Sultanahmet',5200),
(N'ORK-SEED-008',N'Orkestra Bakirkoy Sahil',N'Bakirkoy',N'Atakoy',4400),
(N'ORK-SEED-009',N'Orkestra Maltepe City',N'Maltepe',N'Cevizli',3100),
(N'ORK-SEED-010',N'Orkestra Atasehir Tower',N'Atasehir',N'Kucukbakkalkoy',5800);

DECLARE @Kod nvarchar(32), @Ad nvarchar(200), @Ilce nvarchar(50), @Mahalle nvarchar(80), @Fiyat decimal(10,2);
DECLARE @IlceId bigint, @HotelId bigint, @RoomId bigint;

WHILE EXISTS (SELECT 1 FROM @Rows)
BEGIN
    SELECT TOP (1) @Kod=Kod, @Ad=Ad, @Ilce=Ilce, @Mahalle=Mahalle, @Fiyat=Fiyat FROM @Rows ORDER BY Kod;
    SELECT @HotelId = [ID] FROM [dbo].[OTELLER] WHERE [OTEL_KODU]=@Kod;
    IF @HotelId IS NOT NULL
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM [dbo].[ODA_TIPLERI] WHERE [OTEL_ID]=@HotelId AND [ODA_TIP_KODU]=N'STD-DEMO')
            INSERT INTO [dbo].[ODA_TIPLERI]([OTEL_ID],[ODA_TIP_KODU],[ODA_ADI],[ODA_KATEGORISI],[MAKSIMUM_KISI_SAYISI],[MAKSIMUM_YETISKIN_SAYISI],[MAKSIMUM_COCUK_SAYISI],[YATAK_TIPI],[YATAK_SAYISI],[STANDART_GECELIK_FIYAT],[TOPLAM_ODA_SAYISI],[AKTIF_MI],[SIRALAMA])
            VALUES(@HotelId,N'STD-DEMO',N'Standart',N'Standart',2,2,1,N'Queen',1,@Fiyat,10,1,1);
        DELETE FROM @Rows WHERE Kod=@Kod;
        CONTINUE;
    END

    SET @IlceId = (SELECT TOP 1 [ID] FROM [dbo].[ILCELER] WHERE [IL_ID]=@IstanbulIlId AND ([ILCE_ADI]=@Ilce OR [ILCE_ADI] LIKE @Ilce+N'%') ORDER BY [ID]);
    IF @IlceId IS NULL SET @IlceId = (SELECT TOP 1 [ID] FROM [dbo].[ILCELER] WHERE [IL_ID]=@IstanbulIlId ORDER BY [ID]);

    INSERT INTO [dbo].[OTELLER](
        [OTEL_KODU],[PARTNER_ID],[KULLANICI_ID],[OTEL_ADI],[OTEL_TURU],[OTEL_TIPI_ID],[YILDIZ_SAYISI],
        [ULKE],[SEHIR],[ILCE],[MAHALLE],[TAM_ADRES],[ULKE_ID],[SEHIR_ID],[ILCE_ID],
        [TELEFON_1],[EPOSTA],[REZERVASYON_TELEFONU],[SATIS_KONTAK_ADI],[SATIS_KONTAK_TELEFONU],[SATIS_KONTAK_EPOSTA],
        [CHECK_IN_SAATI],[CHECK_OUT_SAATI],[TOPLAM_ODA_SAYISI],[KISA_ACIKLAMA],[UZUN_ACIKLAMA],
        [VARSAYILAN_KOMISYON_ORANI],[ODEME_VADESI],[ODEME_YONTEMI],[FATURA_KESIM_TURU],
        [YAYIN_DURUMU],[ONAY_DURUMU],[ONAY_TARIHI],[OLUSTURULMA_TARIHI])
    VALUES(@Kod,@PartnerId,@PartnerUserId,@Ad,N'Hotel',@HotelTypeId,4,N'Turkiye',N'Istanbul',@Ilce,@Mahalle,CONCAT(@Mahalle,N', Istanbul'),
        @TrUlkeId,@IstanbulIlId,@IlceId,N'2125550000',CONCAT(N'rez.',LOWER(REPLACE(@Kod,N'-',N'')),N'@demo.otelturizm.local'),
        N'2125550001',N'Demo',N'2125550002',N'ork-demo-partner@otelturizm.local','14:00:00','12:00:00',20,N'Demo otel',N'Demo otel',
        15.00,N'Ayın 15''i',N'Online',N'Otel Keser',N'Yayında',N'Onaylandı',SYSUTCDATETIME(),SYSUTCDATETIME());
    SET @HotelId = SCOPE_IDENTITY();

    IF NOT EXISTS (SELECT 1 FROM [dbo].[OTEL_KULLANICI_SAHIPLIKLERI] WHERE [OTEL_ID]=@HotelId AND [KULLANICI_ID]=@PartnerUserId)
        INSERT INTO [dbo].[OTEL_KULLANICI_SAHIPLIKLERI]([OTEL_ID],[KULLANICI_ID],[PARTNER_ID],[ROL],[ANA_SORUMLU_MU],[AKTIF_MI],[OLUSTURULMA_TARIHI])
        VALUES(@HotelId,@PartnerUserId,@PartnerId,N'owner',1,1,SYSUTCDATETIME());

    IF NOT EXISTS (SELECT 1 FROM [dbo].[ODA_TIPLERI] WHERE [OTEL_ID]=@HotelId AND [ODA_TIP_KODU]=N'STD-DEMO')
        INSERT INTO [dbo].[ODA_TIPLERI]([OTEL_ID],[ODA_TIP_KODU],[ODA_ADI],[ODA_KATEGORISI],[MAKSIMUM_KISI_SAYISI],[MAKSIMUM_YETISKIN_SAYISI],[MAKSIMUM_COCUK_SAYISI],[YATAK_TIPI],[YATAK_SAYISI],[STANDART_GECELIK_FIYAT],[TOPLAM_ODA_SAYISI],[AKTIF_MI],[SIRALAMA])
        VALUES(@HotelId,N'STD-DEMO',N'Standart',N'Standart',2,2,1,N'Queen',1,@Fiyat,10,1,1);

    DELETE FROM @Rows WHERE Kod=@Kod;
END
DECLARE @Cnt int = (SELECT COUNT(*) FROM [dbo].[OTELLER]);
PRINT N'006-010 quick seed OK. OTELLER: ' + CAST(@Cnt AS nvarchar(12));
