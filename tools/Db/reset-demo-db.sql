SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET ARITHABORT ON;
SET NUMERIC_ROUNDABORT OFF;

-- Lokal/canlı demo reset (YIKICI).
-- Sadece katalog tabloları kalır; otel, oda, fiyat/müsaitlik ve rezervasyon verileri temizlenir.
-- Demo hesap şifresi: 908155

DECLARE @Confirm nvarchar(10) = N'YES';
IF @Confirm <> N'YES'
BEGIN
    RAISERROR(N'Bu script yıkıcıdır. Devam için @Confirm = YES olmalı.', 16, 1);
    RETURN;
END;

IF OBJECT_ID(N'dbo.users', N'U') IS NULL OR OBJECT_ID(N'dbo.oteller', N'U') IS NULL
BEGIN
    RAISERROR(N'Beklenen ana tablolar bulunamadı: dbo.users / dbo.oteller.', 16, 1);
    RETURN;
END;

BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @PasswordPlain nvarchar(100) = N'908155';
    DECLARE @PasswordHash nvarchar(64) =
        LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONVERT(nvarchar(max), @PasswordPlain)), 2));

    -------------------------------------------------------------------------
    -- 1) Otel / oda / rezervasyon / işlem verileri temizliği
    -------------------------------------------------------------------------
    IF OBJECT_ID(N'dbo.rezervasyonlar', N'U') IS NOT NULL DISABLE TRIGGER ALL ON dbo.rezervasyonlar;

    IF OBJECT_ID(N'dbo.rezervasyon_odeme_kalemleri', N'U') IS NOT NULL DELETE FROM dbo.rezervasyon_odeme_kalemleri;
    IF OBJECT_ID(N'dbo.odeme_islemleri', N'U') IS NOT NULL DELETE FROM dbo.odeme_islemleri;
    IF OBJECT_ID(N'dbo.basarisiz_odeme_denemeleri', N'U') IS NOT NULL DELETE FROM dbo.basarisiz_odeme_denemeleri;
    IF OBJECT_ID(N'dbo.faturalar', N'U') IS NOT NULL DELETE FROM dbo.faturalar;
    IF OBJECT_ID(N'dbo.komisyon_muhasebe_kayitlari', N'U') IS NOT NULL DELETE FROM dbo.komisyon_muhasebe_kayitlari;
    IF OBJECT_ID(N'dbo.rezervasyon_taslaklari', N'U') IS NOT NULL DELETE FROM dbo.rezervasyon_taslaklari;
    IF OBJECT_ID(N'dbo.sepet_blokajlari', N'U') IS NOT NULL DELETE FROM dbo.sepet_blokajlari;
    IF OBJECT_ID(N'dbo.rezervasyonlar', N'U') IS NOT NULL DELETE FROM dbo.rezervasyonlar;
    IF OBJECT_ID(N'dbo.rezervasyonlar_archive', N'U') IS NOT NULL DELETE FROM dbo.rezervasyonlar_archive;
    IF OBJECT_ID(N'dbo.yorumlar', N'U') IS NOT NULL DELETE FROM dbo.yorumlar;

    IF OBJECT_ID(N'dbo.user_favori_oteller', N'U') IS NOT NULL DELETE FROM dbo.user_favori_oteller;
    IF OBJECT_ID(N'dbo.user_favorite_price_alerts', N'U') IS NOT NULL DELETE FROM dbo.user_favorite_price_alerts;
    IF OBJECT_ID(N'dbo.user_favorite_price_alert_jobs', N'U') IS NOT NULL DELETE FROM dbo.user_favorite_price_alert_jobs;
    IF OBJECT_ID(N'dbo.kullanici_ozel_teklifleri', N'U') IS NOT NULL DELETE FROM dbo.kullanici_ozel_teklifleri;
    IF OBJECT_ID(N'dbo.kullanici_seyahat_plan_otel_secimleri', N'U') IS NOT NULL DELETE FROM dbo.kullanici_seyahat_plan_otel_secimleri;
    IF OBJECT_ID(N'dbo.kullanici_seyahat_planlari', N'U') IS NOT NULL DELETE FROM dbo.kullanici_seyahat_planlari;
    IF OBJECT_ID(N'dbo.kullanici_konum_loglari', N'U') IS NOT NULL DELETE FROM dbo.kullanici_konum_loglari;

    IF OBJECT_ID(N'dbo.kampanya_oteller', N'U') IS NOT NULL DELETE FROM dbo.kampanya_oteller;
    IF OBJECT_ID(N'dbo.otel_istatistikleri', N'U') IS NOT NULL DELETE FROM dbo.otel_istatistikleri;
    IF OBJECT_ID(N'dbo.otel_koordinat_degisim_loglari', N'U') IS NOT NULL DELETE FROM dbo.otel_koordinat_degisim_loglari;
    IF OBJECT_ID(N'dbo.otel_kosullari', N'U') IS NOT NULL DELETE FROM dbo.otel_kosullari;
    IF OBJECT_ID(N'dbo.otel_liste_abonelikleri', N'U') IS NOT NULL DELETE FROM dbo.otel_liste_abonelikleri;
    IF OBJECT_ID(N'dbo.otel_rakip_analizi', N'U') IS NOT NULL DELETE FROM dbo.otel_rakip_analizi;
    IF OBJECT_ID(N'dbo.otel_gorselleri', N'U') IS NOT NULL DELETE FROM dbo.otel_gorselleri;
    IF OBJECT_ID(N'dbo.otel_ozellik_iliskileri', N'U') IS NOT NULL DELETE FROM dbo.otel_ozellik_iliskileri;
    IF OBJECT_ID(N'dbo.otel_kullanici_sahiplikleri', N'U') IS NOT NULL DELETE FROM dbo.otel_kullanici_sahiplikleri;

    IF OBJECT_ID(N'dbo.oda_fiyat_musaitlik', N'U') IS NOT NULL DELETE FROM dbo.oda_fiyat_musaitlik;
    IF OBJECT_ID(N'dbo.firma_oda_fiyat_musaitlik', N'U') IS NOT NULL DELETE FROM dbo.firma_oda_fiyat_musaitlik;
    IF OBJECT_ID(N'dbo.oda_gorselleri', N'U') IS NOT NULL DELETE FROM dbo.oda_gorselleri;
    IF OBJECT_ID(N'dbo.oda_ozellik_iliskileri', N'U') IS NOT NULL DELETE FROM dbo.oda_ozellik_iliskileri;
    IF OBJECT_ID(N'dbo.oda_tipi_ozellikleri', N'U') IS NOT NULL DELETE FROM dbo.oda_tipi_ozellikleri;
    IF OBJECT_ID(N'dbo.oda_tipleri', N'U') IS NOT NULL DELETE FROM dbo.oda_tipleri;

    IF OBJECT_ID(N'dbo.partner_basvuru_evraklari', N'U') IS NOT NULL DELETE FROM dbo.partner_basvuru_evraklari;
    IF OBJECT_ID(N'dbo.partner_basvuru_hareketleri', N'U') IS NOT NULL DELETE FROM dbo.partner_basvuru_hareketleri;
    IF OBJECT_ID(N'dbo.partner_destek_mesajlari', N'U') IS NOT NULL DELETE FROM dbo.partner_destek_mesajlari;
    IF OBJECT_ID(N'dbo.partner_destek_talepleri', N'U') IS NOT NULL DELETE FROM dbo.partner_destek_talepleri;
    IF OBJECT_ID(N'dbo.partner_panel_tercihleri', N'U') IS NOT NULL DELETE FROM dbo.partner_panel_tercihleri;
    IF OBJECT_ID(N'dbo.users_partner', N'U') IS NOT NULL DELETE FROM dbo.users_partner;
    IF OBJECT_ID(N'dbo.partner_detaylari', N'U') IS NOT NULL DELETE FROM dbo.partner_detaylari;

    IF OBJECT_ID(N'dbo.firma_harcama_limitleri', N'U') IS NOT NULL DELETE FROM dbo.firma_harcama_limitleri;
    IF OBJECT_ID(N'dbo.firma_basvuru_hareketleri', N'U') IS NOT NULL DELETE FROM dbo.firma_basvuru_hareketleri;
    IF OBJECT_ID(N'dbo.firmalar', N'U') IS NOT NULL DELETE FROM dbo.firmalar;

    DELETE FROM dbo.oteller;

    DBCC CHECKIDENT (N'dbo.oteller', RESEED, 72) WITH NO_INFOMSGS;
    IF OBJECT_ID(N'dbo.oda_tipleri', N'U') IS NOT NULL DBCC CHECKIDENT (N'dbo.oda_tipleri', RESEED, 181) WITH NO_INFOMSGS;

    -------------------------------------------------------------------------
    -- 2) Demo kullanıcılar
    -------------------------------------------------------------------------
    DECLARE @DemoUsers TABLE(email nvarchar(200) PRIMARY KEY, ad nvarchar(120), rol nvarchar(50), telefon nvarchar(30));
    INSERT INTO @DemoUsers VALUES
    (N'irmhro0@gmail.com', N'Demo Admin', N'admin', N'5000000001'),
    (N'irmhro0+satis@gmail.com', N'Demo Satış', N'satis', N'5000000002'),
    (N'irmhro0+user@gmail.com', N'Demo Kullanıcı', N'user', N'5000000003'),
    (N'irmhro0+firma@gmail.com', N'Demo Firma', N'firma', N'5000000004'),
    (N'irmhro0+kurumsal@gmail.com', N'Demo Kurumsal Partner', N'partner_owner', N'5000000005');

    UPDATE u
    SET eposta = CONCAT(N'arsiv+', u.id, N'@demo.local'),
        guncellenme_tarihi = SYSUTCDATETIME()
    FROM dbo.users u
    JOIN @DemoUsers d ON d.email = u.eposta
    WHERE u.id NOT IN (SELECT MIN(id) FROM dbo.users WHERE eposta IN (SELECT email FROM @DemoUsers) GROUP BY eposta);

    INSERT INTO dbo.users
    (
        ad_soyad, eposta, telefon, sifre, rol,
        iki_asamali_dogrulama_aktif_mi, onay_gereksinimi, firma_yonetici_mi,
        basarisiz_giris_sayisi, pazarlama_izni, hesap_durumu,
        email_dogrulama_tarihi, telefon_dogrulama_tarihi,
        kayit_kaynagi, olusturulma_tarihi, guncellenme_tarihi
    )
    SELECT d.ad, d.email, d.telefon, @PasswordHash, d.rol,
           0, 0, CASE WHEN d.rol = N'firma' THEN 1 ELSE 0 END,
           0, 0, 1,
           SYSUTCDATETIME(), SYSUTCDATETIME(),
           N'DemoReset', SYSUTCDATETIME(), SYSUTCDATETIME()
    FROM @DemoUsers d
    WHERE NOT EXISTS (SELECT 1 FROM dbo.users u WHERE u.eposta = d.email);

    UPDATE u
    SET ad_soyad = d.ad,
        telefon = d.telefon,
        sifre = @PasswordHash,
        rol = d.rol,
        hesap_durumu = 1,
        iki_asamali_dogrulama_aktif_mi = 0,
        email_dogrulama_tarihi = COALESCE(u.email_dogrulama_tarihi, SYSUTCDATETIME()),
        telefon_dogrulama_tarihi = COALESCE(u.telefon_dogrulama_tarihi, SYSUTCDATETIME()),
        guncellenme_tarihi = SYSUTCDATETIME()
    FROM dbo.users u
    JOIN @DemoUsers d ON d.email = u.eposta;

    DECLARE @AdminUserId bigint = (SELECT TOP (1) id FROM dbo.users WHERE eposta = N'irmhro0@gmail.com');
    DECLARE @SatisUserId bigint = (SELECT TOP (1) id FROM dbo.users WHERE eposta = N'irmhro0+satis@gmail.com');
    DECLARE @UserId bigint = (SELECT TOP (1) id FROM dbo.users WHERE eposta = N'irmhro0+user@gmail.com');
    DECLARE @FirmaUserId bigint = (SELECT TOP (1) id FROM dbo.users WHERE eposta = N'irmhro0+firma@gmail.com');
    DECLARE @PartnerUserId bigint = (SELECT TOP (1) id FROM dbo.users WHERE eposta = N'irmhro0+kurumsal@gmail.com');

    -------------------------------------------------------------------------
    -- 3) Demo firma ve demo partner
    -------------------------------------------------------------------------
    INSERT INTO dbo.firmalar
    (
        firma_kodu, firma_adi, firma_turu, vergi_no, vergi_dairesi,
        yetkili_ad_soyad, yetkili_eposta, yetkili_telefon,
        acik_adres, sehir, ilce,
        varsayilan_para_birimi, onay_durumu, aktif_mi, giris_izni_aktif_mi,
        planlanan_onay_suresi_saat, kayit_kaynagi, olusturulma_tarihi, guncellenme_tarihi
    )
    VALUES
    (
        N'DEMOFIRMA', N'Demo Firma A.Ş.', N'Anonim Şirketi', N'1111111111', N'Demo Vergi Dairesi',
        N'Demo Firma Yetkilisi', N'irmhro0+firma@gmail.com', N'5000000004',
        N'Demo Firma Adresi', N'İstanbul', N'Kartal',
        N'TRY', N'Onaylandı', 1, 1,
        24, N'DemoReset', SYSUTCDATETIME(), SYSUTCDATETIME()
    );
    DECLARE @FirmaId bigint = SCOPE_IDENTITY();

    UPDATE dbo.users
    SET firma_id = @FirmaId, firma_yonetici_mi = 1, guncellenme_tarihi = SYSUTCDATETIME()
    WHERE id = @FirmaUserId;

    DECLARE @HotelTypeId int = COALESCE(
        (SELECT TOP (1) id FROM dbo.otel_tipleri WHERE kod = N'hotel-sehir-ici'),
        (SELECT TOP (1) id FROM dbo.otel_tipleri WHERE kod = N'otel')
    );

    INSERT INTO dbo.partner_detaylari
    (
        kullanici_id, firma_unvani, firma_turu,
        vergi_dairesi, vergi_numarasi,
        fatura_adresi, fatura_il, fatura_ilce,
        yetkili_ad_soyad, yetkili_tc_no, yetkili_telefon, yetkili_eposta, yetkili_gorev,
        banka_adi, iban, hesap_sahibi_adi, hesap_para_birimi,
        onay_durumu, onay_tarihi, onaylayan_admin_id,
        web_sitesi, aciklama,
        eposta_giris_onayi_verildi_mi, eposta_giris_onay_tarihi, eposta_giris_onaylayan_admin_id,
        aktif_mi, otel_tipi_id, olusturulma_tarihi, guncellenme_tarihi
    )
    VALUES
    (
        @PartnerUserId, N'Demo Kurumsal Partner', N'Limited Şirketi',
        N'Demo Vergi Dairesi', N'2222222222',
        N'Demo Partner Adresi', N'İstanbul', N'Kartal',
        N'Demo Kurumsal Partner', N'11111111110', N'5000000005', N'irmhro0+kurumsal@gmail.com', N'Genel Müdür',
        N'Demo Bank', N'TR000000000000000000000000', N'Demo Kurumsal Partner', N'TRY',
        N'Onaylandi', SYSUTCDATETIME(), @AdminUserId,
        N'https://otelturizm.com', N'Demo partner hesabı.',
        1, SYSUTCDATETIME(), @AdminUserId,
        1, @HotelTypeId, SYSUTCDATETIME(), SYSUTCDATETIME()
    );
    DECLARE @PartnerId bigint = SCOPE_IDENTITY();

    -------------------------------------------------------------------------
    -- 4) Tek demo otel + oda + fiyat/müsaitlik
    -------------------------------------------------------------------------
    INSERT INTO dbo.oteller
    (
        otel_kodu, partner_id, user_id, otel_adi, otel_turu, otel_tipi_id,
        yildiz_sayisi, ulke, sehir, ilce, mahalle, tam_adres, posta_kodu,
        enlem, boylam, telefon_1, eposta, web_sitesi, rezervasyon_telefonu,
        satis_kontak_adi, satis_kontak_telefonu, satis_kontak_eposta,
        check_in_saati, check_out_saati,
        toplam_oda_sayisi, toplam_yatak_kapasitesi,
        kisa_aciklama, uzun_aciklama, konum_aciklamasi,
        varsayilan_komisyon_orani, odeme_vadesi, odeme_yontemi, fatura_kesim_turu,
        ortalama_puan, toplam_yorum_sayisi, yayin_durumu, onay_durumu, onay_tarihi, onaylayan_admin_id,
        one_cikan_otel, tavsiye_edilen_otel,
        olusturulma_tarihi, guncellenme_tarihi
    )
    VALUES
    (
        N'DEMO-OTEL-001', @PartnerId, @PartnerUserId, N'Demo Test Otel', N'Hotel Şehir İçi', @HotelTypeId,
        4, N'Türkiye', N'İstanbul', N'Kartal', N'Atalar', N'Atalar Mahallesi Demo Sokak No:1 Kartal / İstanbul', N'34862',
        40.9060787, 29.280922, N'5000000005', N'irmhro0+kurumsal@gmail.com', N'https://otelturizm.com', N'5000000005',
        N'Demo Kurumsal Partner', N'5000000005', N'irmhro0+kurumsal@gmail.com',
        '14:00:00', '12:00:00',
        10, 20,
        N'Tek demo otel kaydı.', N'Lokal ve canlı test süreçleri için oluşturulan tek demo otel kaydıdır.', N'Kartal, İstanbul merkezli demo konum.',
        15.00, N'Çıkış Günü', N'Havale/EFT', N'Otel Keser',
        9.0, 0, N'Yayında', N'Onaylandı', SYSUTCDATETIME(), @AdminUserId,
        1, 1,
        SYSUTCDATETIME(), SYSUTCDATETIME()
    );
    DECLARE @HotelId bigint = SCOPE_IDENTITY();

    INSERT INTO dbo.otel_kullanici_sahiplikleri
    (otel_id, user_id, partner_id, rol, ana_sorumlu_mu, aktif_mi, olusturulma_tarihi)
    VALUES (@HotelId, @PartnerUserId, @PartnerId, N'owner', 1, 1, SYSUTCDATETIME());

    IF OBJECT_ID(N'dbo.users_partner', N'U') IS NOT NULL
        INSERT INTO dbo.users_partner (user_id, partner_id, rol, aktif_mi, ana_hesap_mi, olusturulma_tarihi)
        VALUES (@PartnerUserId, @PartnerId, N'owner', 1, 1, SYSUTCDATETIME());

    INSERT INTO dbo.oda_tipleri
    (
        otel_id, oda_tip_kodu, oda_adi, oda_kategorisi,
        maksimum_kisi_sayisi, maksimum_yetiskin_sayisi, maksimum_cocuk_sayisi,
        yatak_tipi, yatak_sayisi, ek_yatak_eklenebilir_mi, oda_metrekare,
        balkon_var_mi, manzara_tipi, ozel_banyo_var_mi, banyo_tipi,
        standart_gecelik_fiyat, toplam_oda_sayisi, aktif_mi, siralama,
        olusturulma_tarihi, guncellenme_tarihi
    )
    VALUES
    (
        @HotelId, N'DEMO-STD', N'Demo Standart Oda', N'Standart',
        2, 2, 1,
        N'Queen Bed', 1, 1, 28,
        1, N'Şehir Manzaralı', 1, N'Duş',
        4200.00, 10, 1, 1,
        SYSUTCDATETIME(), SYSUTCDATETIME()
    );
    DECLARE @RoomId bigint = SCOPE_IDENTITY();

    IF OBJECT_ID(N'dbo.otel_ozellik_iliskileri', N'U') IS NOT NULL
        INSERT INTO dbo.otel_ozellik_iliskileri (otel_id, ozellik_id, kategori_id)
        SELECT TOP (6) @HotelId, o.id, o.kategori_id
        FROM dbo.otel_ozellikleri o
        WHERE o.aktif_mi = 1
        ORDER BY o.one_cikan_ozellik DESC, o.siralama, o.id;

    IF OBJECT_ID(N'dbo.oda_tipi_ozellikleri', N'U') IS NOT NULL
        INSERT INTO dbo.oda_tipi_ozellikleri (oda_tip_id, ozellik_id, miktar, otel_id, kategori_id)
        SELECT TOP (8) @RoomId, o.id, 1, @HotelId, o.kategori_id
        FROM dbo.oda_ozellikleri o
        WHERE o.aktif_mi = 1
        ORDER BY o.siralama, o.id;

    IF OBJECT_ID(N'dbo.oda_ozellik_iliskileri', N'U') IS NOT NULL
        INSERT INTO dbo.oda_ozellik_iliskileri (otel_id, oda_id, kategori_id, ozellik_id, miktar, aktif_mi)
        SELECT @HotelId, @RoomId, o.kategori_id, o.id, 1, 1
        FROM dbo.oda_ozellikleri o
        WHERE o.id IN (SELECT ozellik_id FROM dbo.oda_tipi_ozellikleri WHERE oda_tip_id = @RoomId);

    ;WITH n AS
    (
        SELECT TOP (90) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) - 1 AS i
        FROM sys.all_objects
    )
    INSERT INTO dbo.oda_fiyat_musaitlik
    (
        oda_tip_id, otel_id, tarih, gecelik_fiyat, indirimli_fiyat,
        toplam_oda_sayisi, satilan_oda_sayisi, bloke_oda_sayisi,
        minimum_geceleme, maksimum_geceleme, kapali_satis, guncellenme_tarihi
    )
    SELECT @RoomId, @HotelId, DATEADD(DAY, i, CONVERT(date, GETDATE())),
           4200.00, CASE WHEN i % 7 IN (5,6) THEN 3900.00 ELSE NULL END,
           10, 0, 0, 1, 30, 0, SYSUTCDATETIME()
    FROM n;

    ;WITH n AS
    (
        SELECT TOP (20) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS i
        FROM sys.all_objects
    )
    INSERT INTO dbo.otel_gorselleri
    (otel_id, gorsel_url, gorsel_turu, baslik, aciklama, kapak_fotografi_mi, one_cikan, siralama, onay_durumu, onaylayan_admin_id, onay_tarihi, yukleyen_kullanici_id, olusturulma_tarihi)
    SELECT @HotelId,
           N'/uploads/images/' + CONVERT(nvarchar(20), @HotelId) + N'/hotel/demo-hotel-' + RIGHT(N'00' + CONVERT(nvarchar(2), i), 2) + N'.webp',
           N'Genel Alan', N'Demo otel görseli ' + CONVERT(nvarchar(2), i), N'Telifsiz demo otel görseli.',
           CASE WHEN i = 1 THEN 1 ELSE 0 END, CASE WHEN i <= 6 THEN 1 ELSE 0 END, i,
           N'Onaylandı', @AdminUserId, SYSUTCDATETIME(), @AdminUserId, SYSUTCDATETIME()
    FROM n;

    ;WITH n AS
    (
        SELECT TOP (15) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS i
        FROM sys.all_objects
    )
    INSERT INTO dbo.oda_gorselleri
    (oda_tip_id, gorsel_url, baslik, aciklama, kapak_fotografi_mi, siralama, onay_durumu, onaylayan_admin_id, onay_tarihi, yukleyen_kullanici_id, olusturulma_tarihi)
    SELECT @RoomId,
           N'/uploads/images/' + CONVERT(nvarchar(20), @HotelId) + N'/rooms/' + CONVERT(nvarchar(20), @RoomId) + N'/demo-room-' + RIGHT(N'00' + CONVERT(nvarchar(2), i), 2) + N'.webp',
           N'Demo oda görseli ' + CONVERT(nvarchar(2), i), N'Telifsiz demo oda görseli.',
           CASE WHEN i = 1 THEN 1 ELSE 0 END, i,
           N'Onaylandı', @AdminUserId, SYSUTCDATETIME(), @AdminUserId, SYSUTCDATETIME()
    FROM n;

    UPDATE dbo.oteller
    SET kapak_fotografi = N'/uploads/images/' + CONVERT(nvarchar(20), @HotelId) + N'/hotel/demo-hotel-01.webp'
    WHERE id = @HotelId;

    UPDATE dbo.oda_tipleri
    SET kapak_fotografi = N'/uploads/images/' + CONVERT(nvarchar(20), @HotelId) + N'/rooms/' + CONVERT(nvarchar(20), @RoomId) + N'/demo-room-01.webp'
    WHERE id = @RoomId;

    UPDATE dbo.users
    SET sahiplik_partner_id = CASE WHEN id = @PartnerUserId THEN @PartnerId ELSE sahiplik_partner_id END,
        firma_id = CASE WHEN id = @FirmaUserId THEN @FirmaId ELSE firma_id END,
        guncellenme_tarihi = SYSUTCDATETIME()
    WHERE id IN (@PartnerUserId, @FirmaUserId);

    IF OBJECT_ID(N'dbo.rezervasyonlar', N'U') IS NOT NULL ENABLE TRIGGER ALL ON dbo.rezervasyonlar;

    COMMIT TRANSACTION;

    SELECT
        @HotelId AS demo_otel_id,
        @RoomId AS demo_oda_id,
        @PartnerId AS demo_partner_id,
        @FirmaId AS demo_firma_id,
        @PasswordPlain AS demo_sifre,
        N'Demo reset tamamlandı. Sadece tek demo otel ve demo hesaplar hazır.' AS mesaj;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    IF OBJECT_ID(N'dbo.rezervasyonlar', N'U') IS NOT NULL ENABLE TRIGGER ALL ON dbo.rezervasyonlar;
    DECLARE @err nvarchar(2048) = ERROR_MESSAGE();
    RAISERROR(@err, 16, 1);
END CATCH;
