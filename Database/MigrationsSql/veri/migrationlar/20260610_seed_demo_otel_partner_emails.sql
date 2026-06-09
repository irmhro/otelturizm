-- Idempotent: demo oteller (Maidan / Akdeniz / Goreme) icin ayri partner giris e-postalari
-- Ornek: irmhro0+maidan@gmail.com / irmhro0+akdeniz@gmail.com / irmhro0+goreme@gmail.com
-- Sifre: Demo123! (mevcut demo hash)
-- Uygulama: sqlcmd -I -f 65001 -b -i "...\20260610_seed_demo_otel_partner_emails.sql"
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

IF OBJECT_ID(N'dbo.OTELLER', N'U') IS NULL OR OBJECT_ID(N'dbo.KULLANICILAR', N'U') IS NULL
BEGIN
    RAISERROR(N'OTELLER/KULLANICILAR tablosu bulunamadi.', 16, 1);
    RETURN;
END;

DECLARE @DemoPasswordHash nvarchar(64) = LOWER(CONVERT(VARCHAR(64), HASHBYTES('SHA2_256', CONVERT(nvarchar(max), N'Demo123!')), 2));
DECLARE @HotelTypeId int = (SELECT TOP (1) [ID] FROM [dbo].[OTEL_TIPLERI] WHERE [KOD] = N'HOTEL' ORDER BY [ID]);

DECLARE @Map TABLE (
    OtelKodu nvarchar(32) NOT NULL PRIMARY KEY,
    EmailAlias nvarchar(40) NOT NULL,
    PartnerAd nvarchar(120) NOT NULL,
    FirmaUnvani nvarchar(200) NOT NULL,
    VergiNo nvarchar(20) NOT NULL,
    Telefon nvarchar(20) NOT NULL
);

INSERT INTO @Map (OtelKodu, EmailAlias, PartnerAd, FirmaUnvani, VergiNo, Telefon) VALUES
(N'DEMO-MAIDAN-2026', N'maidan', N'Maidan Demo Partner', N'Maidan Demo Partner A.S.', N'ORK-DEMO-MAIDAN', N'2125558800'),
(N'DEMO-ANTALYA-2026', N'akdeniz', N'Akdeniz Demo Partner', N'Akdeniz Demo Partner A.S.', N'ORK-DEMO-AKDENIZ', N'2425556600'),
(N'DEMO-KAPADOKYA-2026', N'goreme', N'Goreme Demo Partner', N'Goreme Demo Partner A.S.', N'ORK-DEMO-GOREME', N'3845557700');

DECLARE
    @OtelKodu nvarchar(32),
    @EmailAlias nvarchar(40),
    @PartnerAd nvarchar(120),
    @FirmaUnvani nvarchar(200),
    @VergiNo nvarchar(20),
    @Telefon nvarchar(20),
    @PartnerEmail nvarchar(120),
    @HotelId bigint,
    @PartnerUserId bigint,
    @PartnerId bigint,
    @OldPartnerUserId bigint,
    @Processed int = 0;

DECLARE hotel_cursor CURSOR LOCAL FAST_FORWARD FOR
    SELECT m.OtelKodu, m.EmailAlias, m.PartnerAd, m.FirmaUnvani, m.VergiNo, m.Telefon
    FROM @Map m
    ORDER BY m.OtelKodu;

OPEN hotel_cursor;
FETCH NEXT FROM hotel_cursor INTO @OtelKodu, @EmailAlias, @PartnerAd, @FirmaUnvani, @VergiNo, @Telefon;

WHILE @@FETCH_STATUS = 0
BEGIN
    SET @PartnerEmail = N'irmhro0+' + @EmailAlias + N'@gmail.com';
    SET @HotelId = NULL;
    SET @PartnerUserId = NULL;
    SET @PartnerId = NULL;
    SET @OldPartnerUserId = NULL;

    SELECT @HotelId = [ID], @OldPartnerUserId = [KULLANICI_ID], @PartnerId = [PARTNER_ID]
    FROM [dbo].[OTELLER]
    WHERE [OTEL_KODU] = @OtelKodu;

    IF @HotelId IS NULL
    BEGIN
        PRINT N'Atlandi (otel yok): ' + @OtelKodu;
        FETCH NEXT FROM hotel_cursor INTO @OtelKodu, @EmailAlias, @PartnerAd, @FirmaUnvani, @VergiNo, @Telefon;
        CONTINUE;
    END;

    SELECT @PartnerUserId = [ID]
    FROM [dbo].[KULLANICILAR]
    WHERE [EPOSTA] = @PartnerEmail;

    IF @PartnerUserId IS NULL
    BEGIN
        INSERT INTO [dbo].[KULLANICILAR](
            [AD_SOYAD], [EPOSTA], [TELEFON], [SIFRE], [ROL], [HESAP_DURUMU],
            [EPOSTA_DOGRULAMA_TARIHI], [KAYIT_KAYNAGI], [OLUSTURULMA_TARIHI], [GUNCELLENME_TARIHI]
        )
        VALUES (
            @PartnerAd, @PartnerEmail, @Telefon, @DemoPasswordHash, N'partner', 1,
            SYSUTCDATETIME(), N'DemoPartnerEmailSeed', SYSUTCDATETIME(), SYSUTCDATETIME()
        );
        SET @PartnerUserId = SCOPE_IDENTITY();
    END
    ELSE
    BEGIN
        UPDATE [dbo].[KULLANICILAR]
        SET
            [AD_SOYAD] = @PartnerAd,
            [TELEFON] = COALESCE(NULLIF(LTRIM(RTRIM([TELEFON])), N''), @Telefon),
            [SIFRE] = @DemoPasswordHash,
            [ROL] = N'partner',
            [HESAP_DURUMU] = 1,
            [EPOSTA_DOGRULAMA_TARIHI] = COALESCE([EPOSTA_DOGRULAMA_TARIHI], SYSUTCDATETIME()),
            [BASARISIZ_GIRIS_SAYISI] = 0,
            [GIRIS_KILIT_BITIS_TARIHI] = NULL,
            [GUNCELLENME_TARIHI] = SYSUTCDATETIME()
        WHERE [ID] = @PartnerUserId;
    END;

    IF OBJECT_ID(N'dbo.PARTNER_DETAYLARI', N'U') IS NOT NULL
    BEGIN
        SET @PartnerId = NULL;
        SELECT @PartnerId = [ID]
        FROM [dbo].[PARTNER_DETAYLARI]
        WHERE [VERGI_NUMARASI] = @VergiNo;

        IF @PartnerId IS NULL
        BEGIN
            SELECT @PartnerId = [ID]
            FROM [dbo].[PARTNER_DETAYLARI]
            WHERE [KULLANICI_ID] = @PartnerUserId;
        END;

        IF @PartnerId IS NULL
        BEGIN
            INSERT INTO [dbo].[PARTNER_DETAYLARI](
                [KULLANICI_ID], [FIRMA_UNVANI], [FIRMA_TURU], [VERGI_DAIRESI], [VERGI_NUMARASI],
                [FATURA_ADRESI], [FATURA_IL], [FATURA_ILCE], [YETKILI_AD_SOYAD], [YETKILI_TC_NO],
                [YETKILI_TELEFON], [YETKILI_EPOSTA], [BANKA_ADI], [IBAN], [HESAP_SAHIBI_ADI],
                [ONAY_DURUMU], [OLUSTURULMA_TARIHI], [AKTIF_MI], [OTEL_TIPI_ID],
                [EPOSTA_GIRIS_ONAYI_VERILDI_MI], [EPOSTA_GIRIS_ONAY_TARIHI]
            )
            VALUES (
                @PartnerUserId, @FirmaUnvani, N'Limited Sirketi', N'Demo VD', @VergiNo,
                N'Demo Adres', N'Istanbul', N'Beyoglu', @PartnerAd, N'11111111111',
                @Telefon, @PartnerEmail, N'Demo Bank', N'TR000000000000000000000001', @FirmaUnvani,
                N'Onaylandı', SYSUTCDATETIME(), 1, @HotelTypeId,
                1, SYSUTCDATETIME()
            );
            SET @PartnerId = SCOPE_IDENTITY();
        END
        ELSE
        BEGIN
            UPDATE [dbo].[PARTNER_DETAYLARI]
            SET
                [KULLANICI_ID] = @PartnerUserId,
                [FIRMA_UNVANI] = @FirmaUnvani,
                [YETKILI_AD_SOYAD] = @PartnerAd,
                [YETKILI_TELEFON] = @Telefon,
                [YETKILI_EPOSTA] = @PartnerEmail,
                [ONAY_DURUMU] = N'Onaylandı',
                [AKTIF_MI] = 1,
                [EPOSTA_GIRIS_ONAYI_VERILDI_MI] = 1,
                [EPOSTA_GIRIS_ONAY_TARIHI] = COALESCE([EPOSTA_GIRIS_ONAY_TARIHI], SYSUTCDATETIME()),
                [GUNCELLENME_TARIHI] = SYSUTCDATETIME()
            WHERE [ID] = @PartnerId;
        END;
    END;

    UPDATE [dbo].[OTELLER]
    SET
        [KULLANICI_ID] = @PartnerUserId,
        [PARTNER_ID] = @PartnerId,
        [EPOSTA] = @PartnerEmail,
        [SATIS_KONTAK_EPOSTA] = @PartnerEmail,
        [GUNCELLENME_TARIHI] = COALESCE([GUNCELLENME_TARIHI], SYSUTCDATETIME())
    WHERE [ID] = @HotelId;

    IF OBJECT_ID(N'dbo.OTEL_KULLANICI_SAHIPLIKLERI', N'U') IS NOT NULL
    BEGIN
        IF EXISTS (SELECT 1 FROM [dbo].[OTEL_KULLANICI_SAHIPLIKLERI] WHERE [OTEL_ID] = @HotelId)
        BEGIN
            UPDATE [dbo].[OTEL_KULLANICI_SAHIPLIKLERI]
            SET
                [KULLANICI_ID] = @PartnerUserId,
                [PARTNER_ID] = @PartnerId,
                [AKTIF_MI] = 1,
                [GUNCELLENME_TARIHI] = SYSUTCDATETIME()
            WHERE [OTEL_ID] = @HotelId;
        END
        ELSE
        BEGIN
            INSERT INTO [dbo].[OTEL_KULLANICI_SAHIPLIKLERI](
                [OTEL_ID], [KULLANICI_ID], [PARTNER_ID], [ROL], [ANA_SORUMLU_MU], [AKTIF_MI], [OLUSTURULMA_TARIHI]
            )
            VALUES (@HotelId, @PartnerUserId, @PartnerId, N'owner', 1, 1, SYSUTCDATETIME());
        END;
    END;

    IF OBJECT_ID(N'dbo.PARTNER_TESIS_KULLANICILARI', N'U') IS NOT NULL
    BEGIN
        IF NOT EXISTS (
            SELECT 1 FROM [dbo].[PARTNER_TESIS_KULLANICILARI]
            WHERE [OTEL_ID] = @HotelId AND [KULLANICI_ID] = @PartnerUserId
        )
        BEGIN
            INSERT INTO [dbo].[PARTNER_TESIS_KULLANICILARI](
                [OTEL_ID], [KULLANICI_ID], [DURUM], [BASLANGIC_TARIHI], [ONAY_TARIHI], [AKTIF_MI], [OLUSTURULMA_TARIHI]
            )
            VALUES (@HotelId, @PartnerUserId, N'Onaylandi', SYSUTCDATETIME(), SYSUTCDATETIME(), 1, SYSUTCDATETIME());
        END
        ELSE
        BEGIN
            UPDATE [dbo].[PARTNER_TESIS_KULLANICILARI]
            SET [AKTIF_MI] = 1, [DURUM] = N'Onaylandi', [GUNCELLENME_TARIHI] = SYSUTCDATETIME()
            WHERE [OTEL_ID] = @HotelId AND [KULLANICI_ID] = @PartnerUserId;
        END;
    END;

    SET @Processed += 1;
    PRINT CONCAT(N'OK ', @OtelKodu, N' => ', @PartnerEmail, N' | OTEL_ID=', @HotelId, N' | USER_ID=', @PartnerUserId);

    FETCH NEXT FROM hotel_cursor INTO @OtelKodu, @EmailAlias, @PartnerAd, @FirmaUnvani, @VergiNo, @Telefon;
END;

CLOSE hotel_cursor;
DEALLOCATE hotel_cursor;

PRINT CONCAT(N'Demo otel partner e-posta seed tamam. Islenen otel: ', @Processed);
