SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRAN;

    IF OBJECT_ID(N'dbo.DEPARTMANLAR', N'U') IS NOT NULL
    BEGIN
        ;WITH src AS
        (
            SELECT * FROM (VALUES
                (N'GENEL_YONETIM', N'Genel Yönetim', N'Platform seviyesi strateji, yayın ve onay akışları'),
                (N'OPERASYON', N'Operasyon', N'Rezervasyon, tesis ve süreç operasyonları'),
                (N'DESTEK', N'Destek', N'Kullanıcı, firma ve partner destek süreçleri'),
                (N'SATIS', N'Satış', N'Teklif, dönüşüm ve satış operasyonu'),
                (N'MUHASEBE', N'Muhasebe', N'Fatura, mutabakat ve muhasebe işlemleri'),
                (N'FINANS', N'Finans', N'Tahsilat, komisyon ve gelir yönetimi'),
                (N'TEKNOLOJI', N'Teknoloji', N'Yazılım, altyapı ve güvenlik geliştirmeleri'),
                (N'HUKUK_UYUM', N'Hukuk ve Uyum', N'Sözleşme, KVKK ve uyum süreçleri'),
                (N'INSAN_KAYNAKLARI', N'İnsan Kaynakları', N'İşe alım, ekip ve organizasyon süreçleri')
            ) x(DEPARTMAN_KODU, DEPARTMAN_ADI, ACIKLAMA)
        )
        MERGE [dbo].[DEPARTMANLAR] AS t
        USING src AS s
           ON t.[DEPARTMAN_KODU] = s.[DEPARTMAN_KODU]
        WHEN MATCHED THEN UPDATE SET
            [DEPARTMAN_ADI] = s.[DEPARTMAN_ADI],
            [ACIKLAMA] = s.[ACIKLAMA],
            [AKTIF_MI] = 1
        WHEN NOT MATCHED THEN
            INSERT ([DEPARTMAN_KODU], [DEPARTMAN_ADI], [ACIKLAMA], [AKTIF_MI], [OLUSTURULMA_TARIHI])
            VALUES (s.[DEPARTMAN_KODU], s.[DEPARTMAN_ADI], s.[ACIKLAMA], 1, SYSUTCDATETIME());
    END;

    IF OBJECT_ID(N'dbo.ROLLER', N'U') IS NOT NULL
    BEGIN
        ;WITH src AS
        (
            SELECT * FROM (VALUES
                (N'SISTEM_YONETICISI', N'Sistem Yöneticisi', N'GENEL_YONETIM', CAST(90 AS tinyint), CAST(0 AS bit), NULL, N'Tüm platform yönetim yetkileri'),
                (N'PLATFORM_OPERASYON_YONETICISI', N'Platform Operasyon Yöneticisi', N'OPERASYON', CAST(80 AS tinyint), CAST(0 AS bit), N'SISTEM_YONETICISI', N'Operasyon ve onay süreçlerini yönetir'),
                (N'TEKNOLOJI_YONETICISI', N'Teknoloji Yöneticisi', N'TEKNOLOJI', CAST(80 AS tinyint), CAST(0 AS bit), N'SISTEM_YONETICISI', N'Sistem, güvenlik ve içerik altyapısını yönetir'),
                (N'HUKUK_UYUM_UZMANI', N'Hukuk ve Uyum Uzmanı', N'HUKUK_UYUM', CAST(70 AS tinyint), CAST(0 AS bit), N'PLATFORM_OPERASYON_YONETICISI', N'Sözleşme ve uyum kayıtlarını yönetir'),
                (N'IK_YONETICISI', N'İK Yöneticisi', N'INSAN_KAYNAKLARI', CAST(70 AS tinyint), CAST(0 AS bit), N'SISTEM_YONETICISI', N'Organizasyon ve ekip kayıtlarını yönetir'),
                (N'DESTEK_UZMANI', N'Destek Uzmanı', N'DESTEK', CAST(60 AS tinyint), CAST(0 AS bit), N'PLATFORM_OPERASYON_YONETICISI', N'Destek, yorum ve rezervasyon müdahale yetkileri'),
                (N'SATIS_UZMANI', N'Satış Uzmanı', N'SATIS', CAST(60 AS tinyint), CAST(0 AS bit), N'PLATFORM_OPERASYON_YONETICISI', N'Satış talepleri ve rezervasyon dönüşüm süreçleri'),
                (N'MUHASEBE_UZMANI', N'Muhasebe Uzmanı', N'MUHASEBE', CAST(60 AS tinyint), CAST(0 AS bit), N'PLATFORM_OPERASYON_YONETICISI', N'Fatura ve mutabakat yönetimi'),
                (N'FINANS_UZMANI', N'Finans Uzmanı', N'FINANS', CAST(60 AS tinyint), CAST(0 AS bit), N'PLATFORM_OPERASYON_YONETICISI', N'Komisyon ve tahsilat yönetimi'),
                (N'TESIS_YONETICISI', N'Tesis Yöneticisi', N'OPERASYON', CAST(50 AS tinyint), CAST(0 AS bit), N'PLATFORM_OPERASYON_YONETICISI', N'Partner otel ve tesis işlemleri'),
                (N'KURUM_YONETICISI', N'Kurum Yöneticisi', N'SATIS', CAST(50 AS tinyint), CAST(0 AS bit), N'SATIS_UZMANI', N'Kurumsal firma rezervasyon süreçleri'),
                (N'SON_KULLANICI', N'Son Kullanıcı', N'DESTEK', CAST(10 AS tinyint), CAST(1 AS bit), NULL, N'Son kullanıcı paneli ve rezervasyon görünümü')
            ) x(ROL_KODU, ROL_ADI, DEPARTMAN, SEVIYE, VARSAYILAN_MI, UST_ROL_KODU, ACIKLAMA)
        )
        MERGE [dbo].[ROLLER] AS t
        USING src AS s
           ON t.[ROL_KODU] = s.[ROL_KODU]
        WHEN MATCHED THEN UPDATE SET
            [ROL_ADI] = s.[ROL_ADI],
            [DEPARTMAN] = s.[DEPARTMAN],
            [SEVIYE] = s.[SEVIYE],
            [VARSAYILAN_MI] = s.[VARSAYILAN_MI],
            [ACIKLAMA] = s.[ACIKLAMA]
        WHEN NOT MATCHED THEN
            INSERT ([ROL_KODU], [ROL_ADI], [DEPARTMAN], [SEVIYE], [VARSAYILAN_MI], [ACIKLAMA], [OLUSTURULMA_TARIHI])
            VALUES (s.[ROL_KODU], s.[ROL_ADI], s.[DEPARTMAN], s.[SEVIYE], s.[VARSAYILAN_MI], s.[ACIKLAMA], SYSUTCDATETIME());

        ;WITH parent_map AS
        (
            SELECT c.[ID] AS child_id, p.[ID] AS parent_id
            FROM [dbo].[ROLLER] c
            INNER JOIN
            (
                SELECT ROL_KODU, UST_ROL_KODU
                FROM (VALUES
                    (N'PLATFORM_OPERASYON_YONETICISI', N'SISTEM_YONETICISI'),
                    (N'TEKNOLOJI_YONETICISI', N'SISTEM_YONETICISI'),
                    (N'HUKUK_UYUM_UZMANI', N'PLATFORM_OPERASYON_YONETICISI'),
                    (N'IK_YONETICISI', N'SISTEM_YONETICISI'),
                    (N'DESTEK_UZMANI', N'PLATFORM_OPERASYON_YONETICISI'),
                    (N'SATIS_UZMANI', N'PLATFORM_OPERASYON_YONETICISI'),
                    (N'MUHASEBE_UZMANI', N'PLATFORM_OPERASYON_YONETICISI'),
                    (N'FINANS_UZMANI', N'PLATFORM_OPERASYON_YONETICISI'),
                    (N'TESIS_YONETICISI', N'PLATFORM_OPERASYON_YONETICISI'),
                    (N'KURUM_YONETICISI', N'SATIS_UZMANI')
                ) m(ROL_KODU, UST_ROL_KODU)
            ) pm ON pm.[ROL_KODU] = c.[ROL_KODU]
            INNER JOIN [dbo].[ROLLER] p ON p.[ROL_KODU] = pm.[UST_ROL_KODU]
        )
        UPDATE r
        SET [UST_ROL_ID] = pm.[parent_id]
        FROM [dbo].[ROLLER] r
        INNER JOIN parent_map pm ON pm.[child_id] = r.[ID]
        WHERE COALESCE(r.[UST_ROL_ID], 0) <> pm.[parent_id];
    END;

    IF OBJECT_ID(N'dbo.DEPARTMANLAR', N'U') IS NOT NULL AND OBJECT_ID(N'dbo.ROLLER', N'U') IS NOT NULL
    BEGIN
        UPDATE d
        SET [YONETICI_ROL_ID] = r.[ID]
        FROM [dbo].[DEPARTMANLAR] d
        INNER JOIN [dbo].[ROLLER] r ON r.[ROL_KODU] =
            CASE d.[DEPARTMAN_KODU]
                WHEN N'GENEL_YONETIM' THEN N'SISTEM_YONETICISI'
                WHEN N'OPERASYON' THEN N'PLATFORM_OPERASYON_YONETICISI'
                WHEN N'DESTEK' THEN N'DESTEK_UZMANI'
                WHEN N'SATIS' THEN N'SATIS_UZMANI'
                WHEN N'MUHASEBE' THEN N'MUHASEBE_UZMANI'
                WHEN N'FINANS' THEN N'FINANS_UZMANI'
                WHEN N'TEKNOLOJI' THEN N'TEKNOLOJI_YONETICISI'
                WHEN N'HUKUK_UYUM' THEN N'HUKUK_UYUM_UZMANI'
                WHEN N'INSAN_KAYNAKLARI' THEN N'IK_YONETICISI'
            END
        WHERE COALESCE(d.[YONETICI_ROL_ID], 0) <> r.[ID];
    END;

    IF OBJECT_ID(N'dbo.YETKILER', N'U') IS NOT NULL
    BEGIN
        ;WITH src AS
        (
            SELECT * FROM (VALUES
                (N'DASHBOARD_GORUNTULE', N'Dashboard', N'Görüntüle', N'Panel özet ekranlarını görüntüler', 1),
                (N'REZERVASYON_GORUNTULE', N'Rezervasyon', N'Görüntüle', N'Rezervasyon kayıtlarını listeler', 1),
                (N'REZERVASYON_YONET', N'Rezervasyon', N'Yönet', N'Rezervasyon statüsü ve müdahale işlemleri', 0),
                (N'OTEL_GORUNTULE', N'Otel', N'Görüntüle', N'Otel ve tesis kayıtlarını listeler', 1),
                (N'OTEL_YONET', N'Otel', N'Yönet', N'Otel yayın, askı ve detay yönetimi', 0),
                (N'ODA_YONET', N'Oda', N'Yönet', N'Oda ve oda özelliklerini yönetir', 0),
                (N'FIYAT_YONET', N'Fiyat', N'Yönet', N'Fiyat, stok ve kampanya kayıtlarını yönetir', 0),
                (N'KAMPANYA_YONET', N'Kampanya', N'Yönet', N'Kampanya tanım ve katılım kayıtlarını yönetir', 0),
                (N'FIRMA_GORUNTULE', N'Firma', N'Görüntüle', N'Firma başvuru ve rezervasyon kayıtlarını görüntüler', 0),
                (N'FIRMA_YONET', N'Firma', N'Yönet', N'Firma hesap ve onay süreçlerini yönetir', 0),
                (N'PARTNER_GORUNTULE', N'Partner', N'Görüntüle', N'Partner başvuru ve tesis kayıtlarını görüntüler', 0),
                (N'PARTNER_YONET', N'Partner', N'Yönet', N'Partner onay, evrak ve tesis süreçlerini yönetir', 0),
                (N'KULLANICI_GORUNTULE', N'Kullanıcı', N'Görüntüle', N'Kullanıcı kayıtlarını görüntüler', 0),
                (N'KULLANICI_YONET', N'Kullanıcı', N'Yönet', N'Kullanıcı, rol ve departman atamalarını yönetir', 0),
                (N'ROL_YONET', N'Yetki', N'Yönet', N'Rol ve yetki matrisi yönetimi', 0),
                (N'FINANS_GORUNTULE', N'Finans', N'Görüntüle', N'Fatura, komisyon ve ödeme kayıtlarını görüntüler', 0),
                (N'FINANS_YONET', N'Finans', N'Yönet', N'Ödeme, mutabakat ve finans yönetimi', 0),
                (N'RAPOR_GORUNTULE', N'Rapor', N'Görüntüle', N'Operasyon ve gelir raporlarını görüntüler', 0),
                (N'ICERIK_YONET', N'İçerik', N'Yönet', N'Yardım merkezi, blog, SSS ve içerik yönetimi', 0),
                (N'MAIL_YONET', N'E-posta', N'Yönet', N'E-posta servisleri, kuyruk ve şablon yönetimi', 0),
                (N'DESTEK_YONET', N'Destek', N'Yönet', N'Destek talepleri ve cevap süreçlerini yönetir', 0),
                (N'YORUM_YONET', N'Yorum', N'Yönet', N'Yorum moderasyonu ve ihlal bildirimleri', 0),
                (N'GUVENLIK_GORUNTULE', N'Güvenlik', N'Görüntüle', N'Güvenlik olaylarını görüntüler', 0),
                (N'GUVENLIK_YONET', N'Güvenlik', N'Yönet', N'Güvenlik, oturum ve erişim ayarlarını yönetir', 0),
                (N'SISTEM_GORUNTULE', N'Sistem', N'Görüntüle', N'Sistem sağlığı ve checkup ekranlarını görüntüler', 0),
                (N'SISTEM_YONET', N'Sistem', N'Yönet', N'Platform ayarları ve servis yapılandırması', 0),
                (N'SATIS_GORUNTULE', N'Satış', N'Görüntüle', N'Satış talepleri ve müşteri kayıtlarını görüntüler', 0),
                (N'SATIS_YONET', N'Satış', N'Yönet', N'Satış akışı ve teklif süreçlerini yönetir', 0),
                (N'SOZLESME_YONET', N'Sözleşme', N'Yönet', N'Sözleşme ve KVKK içerik süreçlerini yönetir', 0)
            ) x(YETKI_KODU, MODUL, EYLEM, ACIKLAMA, VARSAYILAN_IZIN)
        )
        MERGE [dbo].[YETKILER] AS t
        USING src AS s
           ON t.[YETKI_KODU] = s.[YETKI_KODU]
        WHEN MATCHED THEN UPDATE SET
            [MODUL] = s.[MODUL],
            [EYLEM] = s.[EYLEM],
            [ACIKLAMA] = s.[ACIKLAMA],
            [VARSAYILAN_IZIN] = s.[VARSAYILAN_IZIN]
        WHEN NOT MATCHED THEN
            INSERT ([YETKI_KODU], [MODUL], [EYLEM], [ACIKLAMA], [VARSAYILAN_IZIN], [OLUSTURULMA_TARIHI])
            VALUES (s.[YETKI_KODU], s.[MODUL], s.[EYLEM], s.[ACIKLAMA], s.[VARSAYILAN_IZIN], SYSUTCDATETIME());
    END;

    IF OBJECT_ID(N'dbo.ROL_YETKILERI', N'U') IS NOT NULL AND OBJECT_ID(N'dbo.ROLLER', N'U') IS NOT NULL AND OBJECT_ID(N'dbo.YETKILER', N'U') IS NOT NULL
    BEGIN
        ;WITH src AS
        (
            SELECT * FROM (VALUES
                (N'SISTEM_YONETICISI', N'*'),
                (N'PLATFORM_OPERASYON_YONETICISI', N'DASHBOARD_GORUNTULE'),
                (N'PLATFORM_OPERASYON_YONETICISI', N'REZERVASYON_GORUNTULE'),
                (N'PLATFORM_OPERASYON_YONETICISI', N'REZERVASYON_YONET'),
                (N'PLATFORM_OPERASYON_YONETICISI', N'OTEL_GORUNTULE'),
                (N'PLATFORM_OPERASYON_YONETICISI', N'OTEL_YONET'),
                (N'PLATFORM_OPERASYON_YONETICISI', N'PARTNER_GORUNTULE'),
                (N'PLATFORM_OPERASYON_YONETICISI', N'PARTNER_YONET'),
                (N'PLATFORM_OPERASYON_YONETICISI', N'FIRMA_GORUNTULE'),
                (N'PLATFORM_OPERASYON_YONETICISI', N'FIRMA_YONET'),
                (N'PLATFORM_OPERASYON_YONETICISI', N'RAPOR_GORUNTULE'),
                (N'PLATFORM_OPERASYON_YONETICISI', N'YORUM_YONET'),
                (N'TEKNOLOJI_YONETICISI', N'DASHBOARD_GORUNTULE'),
                (N'TEKNOLOJI_YONETICISI', N'SISTEM_GORUNTULE'),
                (N'TEKNOLOJI_YONETICISI', N'SISTEM_YONET'),
                (N'TEKNOLOJI_YONETICISI', N'GUVENLIK_GORUNTULE'),
                (N'TEKNOLOJI_YONETICISI', N'GUVENLIK_YONET'),
                (N'TEKNOLOJI_YONETICISI', N'ICERIK_YONET'),
                (N'TEKNOLOJI_YONETICISI', N'MAIL_YONET'),
                (N'DESTEK_UZMANI', N'DASHBOARD_GORUNTULE'),
                (N'DESTEK_UZMANI', N'REZERVASYON_GORUNTULE'),
                (N'DESTEK_UZMANI', N'REZERVASYON_YONET'),
                (N'DESTEK_UZMANI', N'DESTEK_YONET'),
                (N'DESTEK_UZMANI', N'YORUM_YONET'),
                (N'SATIS_UZMANI', N'DASHBOARD_GORUNTULE'),
                (N'SATIS_UZMANI', N'SATIS_GORUNTULE'),
                (N'SATIS_UZMANI', N'SATIS_YONET'),
                (N'SATIS_UZMANI', N'REZERVASYON_GORUNTULE'),
                (N'SATIS_UZMANI', N'FIRMA_GORUNTULE'),
                (N'MUHASEBE_UZMANI', N'DASHBOARD_GORUNTULE'),
                (N'MUHASEBE_UZMANI', N'FINANS_GORUNTULE'),
                (N'MUHASEBE_UZMANI', N'FINANS_YONET'),
                (N'MUHASEBE_UZMANI', N'RAPOR_GORUNTULE'),
                (N'FINANS_UZMANI', N'DASHBOARD_GORUNTULE'),
                (N'FINANS_UZMANI', N'FINANS_GORUNTULE'),
                (N'FINANS_UZMANI', N'FINANS_YONET'),
                (N'FINANS_UZMANI', N'RAPOR_GORUNTULE'),
                (N'TESIS_YONETICISI', N'DASHBOARD_GORUNTULE'),
                (N'TESIS_YONETICISI', N'OTEL_GORUNTULE'),
                (N'TESIS_YONETICISI', N'OTEL_YONET'),
                (N'TESIS_YONETICISI', N'ODA_YONET'),
                (N'TESIS_YONETICISI', N'FIYAT_YONET'),
                (N'TESIS_YONETICISI', N'KAMPANYA_YONET'),
                (N'TESIS_YONETICISI', N'REZERVASYON_GORUNTULE'),
                (N'KURUM_YONETICISI', N'DASHBOARD_GORUNTULE'),
                (N'KURUM_YONETICISI', N'REZERVASYON_GORUNTULE'),
                (N'KURUM_YONETICISI', N'FIRMA_GORUNTULE'),
                (N'SON_KULLANICI', N'DASHBOARD_GORUNTULE'),
                (N'SON_KULLANICI', N'REZERVASYON_GORUNTULE')
            ) x(ROL_KODU, YETKI_KODU)
        ),
        expanded AS
        (
            SELECT r.[ID] AS ROL_ID, y.[ID] AS YETKI_ID
            FROM src s
            INNER JOIN [dbo].[ROLLER] r ON r.[ROL_KODU] = s.[ROL_KODU]
            INNER JOIN [dbo].[YETKILER] y ON s.[YETKI_KODU] = N'*' OR y.[YETKI_KODU] = s.[YETKI_KODU]
        )
        MERGE [dbo].[ROL_YETKILERI] AS t
        USING expanded AS s
           ON t.[ROL_ID] = s.[ROL_ID] AND t.[YETKI_ID] = s.[YETKI_ID]
        WHEN MATCHED THEN UPDATE SET
            [IZIN_VAR] = 1,
            [ATAMA_TARIHI] = COALESCE(t.[ATAMA_TARIHI], SYSUTCDATETIME())
        WHEN NOT MATCHED THEN
            INSERT ([ROL_ID], [YETKI_ID], [IZIN_VAR], [ATAMA_TARIHI])
            VALUES (s.[ROL_ID], s.[YETKI_ID], 1, SYSUTCDATETIME());
    END;

    IF OBJECT_ID(N'dbo.KULLANICI_ROLLERI', N'U') IS NOT NULL AND OBJECT_ID(N'dbo.ROLLER', N'U') IS NOT NULL AND OBJECT_ID(N'dbo.KULLANICILAR', N'U') IS NOT NULL
    BEGIN
        ;WITH src AS
        (
            SELECT u.id AS KULLANICI_ID,
                   r.[ID] AS ROL_ID
            FROM [dbo].[KULLANICILAR] u
            INNER JOIN [dbo].[ROLLER] r
                ON r.[ROL_KODU] =
                    CASE LOWER(COALESCE(u.[ROL], N'user'))
                        WHEN N'admin' THEN N'SISTEM_YONETICISI'
                        WHEN N'partner' THEN N'TESIS_YONETICISI'
                        WHEN N'firma' THEN N'KURUM_YONETICISI'
                        WHEN N'satis' THEN N'SATIS_UZMANI'
                        WHEN N'muhasebe' THEN N'MUHASEBE_UZMANI'
                        WHEN N'destek' THEN N'DESTEK_UZMANI'
                        ELSE N'SON_KULLANICI'
                    END
        )
        MERGE [dbo].[KULLANICI_ROLLERI] AS t
        USING src AS s
           ON t.[KULLANICI_ID] = s.[KULLANICI_ID] AND t.[ROL_ID] = s.[ROL_ID]
        WHEN NOT MATCHED THEN
            INSERT ([KULLANICI_ID], [ROL_ID], [ATAMA_TARIHI])
            VALUES (s.[KULLANICI_ID], s.[ROL_ID], SYSUTCDATETIME());
    END;

    IF OBJECT_ID(N'dbo.KULLANICI_DEPARTMAN', N'U') IS NOT NULL AND OBJECT_ID(N'dbo.DEPARTMANLAR', N'U') IS NOT NULL AND OBJECT_ID(N'dbo.KULLANICILAR', N'U') IS NOT NULL
    BEGIN
        ;WITH src AS
        (
            SELECT u.id AS KULLANICI_ID,
                   d.[ID] AS DEPARTMAN_ID,
                   CAST(CASE WHEN LOWER(COALESCE(u.[ROL], N'user')) = N'admin' THEN 1 ELSE 0 END AS bit) AS YONETICI_MI
            FROM [dbo].[KULLANICILAR] u
            INNER JOIN [dbo].[DEPARTMANLAR] d
                ON d.[DEPARTMAN_KODU] =
                    CASE LOWER(COALESCE(u.[ROL], N'user'))
                        WHEN N'admin' THEN N'GENEL_YONETIM'
                        WHEN N'partner' THEN N'OPERASYON'
                        WHEN N'firma' THEN N'SATIS'
                        WHEN N'satis' THEN N'SATIS'
                        WHEN N'muhasebe' THEN N'MUHASEBE'
                        WHEN N'destek' THEN N'DESTEK'
                        ELSE N'DESTEK'
                    END
        )
        MERGE [dbo].[KULLANICI_DEPARTMAN] AS t
        USING src AS s
           ON t.[KULLANICI_ID] = s.[KULLANICI_ID] AND t.[DEPARTMAN_ID] = s.[DEPARTMAN_ID]
        WHEN MATCHED THEN UPDATE SET
            [YONETICI_MI] = s.[YONETICI_MI]
        WHEN NOT MATCHED THEN
            INSERT ([KULLANICI_ID], [DEPARTMAN_ID], [YONETICI_MI], [ISE_BASLAMA_TARIHI])
            VALUES (s.[KULLANICI_ID], s.[DEPARTMAN_ID], s.[YONETICI_MI], CAST(SYSUTCDATETIME() AS date));
    END;

    IF OBJECT_ID(N'dbo.REZERVASYON_DURUM_TANIMLARI', N'U') IS NOT NULL
    BEGIN
        ;WITH src AS
        (
            SELECT * FROM (VALUES
                (N'ONAY_BEKLIYOR', N'Onay Bekliyor', N'Rezervasyon partner/onay akışında bekliyor', 10, 1, 1, 0, 1, 0),
                (N'ONAYLANDI', N'Onaylandı', N'Rezervasyon onaylandı ve aktif plan olarak ilerliyor', 20, 1, 0, 0, 0, 1),
                (N'DEGISIKLIK_BEKLIYOR', N'Değişiklik Bekliyor', N'Tarih, oda veya fiyat değişikliği onay bekliyor', 30, 1, 0, 0, 1, 0),
                (N'TAMAMLANDI', N'Tamamlandı', N'Konaklama başarıyla tamamlandı', 40, 1, 0, 0, 0, 1),
                (N'IPTAL_EDILDI', N'İptal Edildi', N'Rezervasyon iptal edildi', 50, 1, 0, 1, 0, 0),
                (N'NO_SHOW', N'No-Show', N'Misafir giriş yapmadı', 60, 1, 0, 1, 0, 0)
            ) x(KOD, AD, ACIKLAMA, SIRA_NO, AKTIF_MI, SISTEM_SATIR_MI, IPTAL_MI, BEKLEYEN_MI, GELIR_SAYILIR_MI)
        )
        MERGE [dbo].[REZERVASYON_DURUM_TANIMLARI] AS t
        USING src AS s
           ON t.[KOD] = s.[KOD]
        WHEN MATCHED THEN UPDATE SET
            [AD] = s.[AD], [ACIKLAMA] = s.[ACIKLAMA], [SIRA_NO] = s.[SIRA_NO], [AKTIF_MI] = s.[AKTIF_MI],
            [SISTEM_SATIR_MI] = s.[SISTEM_SATIR_MI], [IPTAL_MI] = s.[IPTAL_MI], [TAMAMLANDI_MI] = CASE WHEN s.[KOD]=N'TAMAMLANDI' THEN 1 ELSE 0 END,
            [BEKLEYEN_MI] = s.[BEKLEYEN_MI], [GELIR_SAYILIR_MI] = s.[GELIR_SAYILIR_MI]
        WHEN NOT MATCHED THEN
            INSERT ([KOD], [AD], [ACIKLAMA], [SIRA_NO], [AKTIF_MI], [SISTEM_SATIR_MI], [IPTAL_MI], [TAMAMLANDI_MI], [BEKLEYEN_MI], [GELIR_SAYILIR_MI], [OLUSTURULMA_TARIHI])
            VALUES (s.[KOD], s.[AD], s.[ACIKLAMA], s.[SIRA_NO], s.[AKTIF_MI], s.[SISTEM_SATIR_MI], s.[IPTAL_MI], CASE WHEN s.[KOD]=N'TAMAMLANDI' THEN 1 ELSE 0 END, s.[BEKLEYEN_MI], s.[GELIR_SAYILIR_MI], SYSUTCDATETIME());
    END;

    IF OBJECT_ID(N'dbo.ODEME_DURUMU_TANIMLARI', N'U') IS NOT NULL
    BEGIN
        ;WITH src AS
        (
            SELECT * FROM (VALUES
                (N'BEKLEMEDE', N'Beklemede', N'Ödeme kaydı oluşturuldu ancak tahsilat tamamlanmadı', 10, 1, 1, 0, 0),
                (N'ON_ODEME_ALINDI', N'Ön Ödeme Alındı', N'Kısmi ön ödeme başarılı şekilde alındı', 20, 1, 0, 0, 0),
                (N'KISMEN_ODENDI', N'Kısmen Ödendi', N'Ödemenin bir bölümü tahsil edildi', 30, 1, 0, 0, 0),
                (N'TAMAMLANDI', N'Tamamlandı', N'Ödeme tamamen tahsil edildi', 40, 1, 0, 1, 0),
                (N'IADE_EDILDI', N'İade Edildi', N'Tutar tamamen iade edildi', 50, 1, 0, 0, 1),
                (N'KISMI_IADE', N'Kısmi İade', N'Tutarın bir bölümü iade edildi', 60, 1, 0, 0, 1),
                (N'BASARISIZ', N'Başarısız', N'Ödeme girişimi başarısız oldu', 70, 1, 0, 0, 0)
            ) x(KOD, AD, ACIKLAMA, SIRA_NO, AKTIF_MI, BEKLEYEN_MI, BASARI_MI, IADE_MI)
        )
        MERGE [dbo].[ODEME_DURUMU_TANIMLARI] AS t
        USING src AS s
           ON t.[KOD] = s.[KOD]
        WHEN MATCHED THEN UPDATE SET
            [AD] = s.[AD], [ACIKLAMA] = s.[ACIKLAMA], [SIRA_NO] = s.[SIRA_NO], [AKTIF_MI] = s.[AKTIF_MI],
            [BEKLEYEN_MI] = s.[BEKLEYEN_MI], [BASARI_MI] = s.[BASARI_MI], [TAM_MI] = CASE WHEN s.[KOD]=N'TAMAMLANDI' THEN 1 ELSE 0 END,
            [IADE_MI] = s.[IADE_MI]
        WHEN NOT MATCHED THEN
            INSERT ([KOD], [AD], [ACIKLAMA], [SIRA_NO], [AKTIF_MI], [BEKLEYEN_MI], [BASARI_MI], [TAM_MI], [IADE_MI], [OLUSTURULMA_TARIHI])
            VALUES (s.[KOD], s.[AD], s.[ACIKLAMA], s.[SIRA_NO], s.[AKTIF_MI], s.[BEKLEYEN_MI], s.[BASARI_MI], CASE WHEN s.[KOD]=N'TAMAMLANDI' THEN 1 ELSE 0 END, s.[IADE_MI], SYSUTCDATETIME());
    END;

    IF OBJECT_ID(N'dbo.ODEME_YONTEMI_TANIMLARI', N'U') IS NOT NULL
    BEGIN
        ;WITH src AS
        (
            SELECT * FROM (VALUES
                (N'KAPIDA_ODEME', N'Kapıda Ödeme', N'Tesis sırasında tahsil edilen ödeme', 10, 1, 1, 0, 0, 0),
                (N'NAKIT', N'Nakit', N'Nakit tahsilat yöntemi', 20, 1, 0, 0, 0, 0),
                (N'SANAL_POS', N'Sanal POS', N'Online kart ile tahsilat', 30, 1, 0, 1, 0, 1),
                (N'KREDI_KARTI', N'Kredi Kartı', N'Kredi kartı ile tahsilat', 40, 1, 0, 1, 0, 1),
                (N'HAVALE_EFT', N'Havale / EFT', N'Banka havalesi veya EFT ile tahsilat', 50, 1, 0, 0, 1, 0),
                (N'BANKA_HAVALESI', N'Banka Havalesi', N'Banka havalesi ile tahsilat', 60, 1, 0, 0, 1, 0),
                (N'DIJITAL_CUZDAN', N'Dijital Cüzdan', N'Dijital cüzdan veya QR ödeme', 70, 1, 0, 0, 0, 1),
                (N'DIGER', N'Diğer', N'Sistem dışı veya özel ödeme yöntemi', 80, 1, 0, 0, 0, 0)
            ) x(KOD, AD, ACIKLAMA, SIRA_NO, AKTIF_MI, KAPIDA_MI, KART_MI, HAVALE_MI, ONLINE_MI)
        )
        MERGE [dbo].[ODEME_YONTEMI_TANIMLARI] AS t
        USING src AS s
           ON t.[KOD] = s.[KOD]
        WHEN MATCHED THEN UPDATE SET
            [AD] = s.[AD], [ACIKLAMA] = s.[ACIKLAMA], [SIRA_NO] = s.[SIRA_NO], [AKTIF_MI] = s.[AKTIF_MI],
            [SISTEM_SATIR_MI] = 1, [KAPIDA_MI] = s.[KAPIDA_MI], [KART_MI] = s.[KART_MI], [HAVALE_MI] = s.[HAVALE_MI], [ONLINE_MI] = s.[ONLINE_MI]
        WHEN NOT MATCHED THEN
            INSERT ([KOD], [AD], [ACIKLAMA], [SIRA_NO], [AKTIF_MI], [SISTEM_SATIR_MI], [KAPIDA_MI], [KART_MI], [HAVALE_MI], [ONLINE_MI], [OLUSTURULMA_TARIHI])
            VALUES (s.[KOD], s.[AD], s.[ACIKLAMA], s.[SIRA_NO], s.[AKTIF_MI], 1, s.[KAPIDA_MI], s.[KART_MI], s.[HAVALE_MI], s.[ONLINE_MI], SYSUTCDATETIME());
    END;

    IF OBJECT_ID(N'dbo.ODA_OZELLIK_KATEGORILERI', N'U') IS NOT NULL
    BEGIN
        ;WITH src AS
        (
            SELECT * FROM (VALUES
                (N'Konfor', N'fa-bed', 10),
                (N'Banyo', N'fa-bath', 20),
                (N'Medya ve Teknoloji', N'fa-tv', 30),
                (N'Mutfak ve İkram', N'fa-mug-hot', 40),
                (N'Erişilebilirlik', N'fa-universal-access', 50),
                (N'Dış Alan ve Manzara', N'fa-mountain-sun', 60)
            ) x(KATEGORI_ADI, KATEGORI_IKON, SIRALAMA)
        )
        MERGE [dbo].[ODA_OZELLIK_KATEGORILERI] AS t
        USING src AS s
           ON t.[KATEGORI_ADI] = s.[KATEGORI_ADI]
        WHEN MATCHED THEN UPDATE SET
            [KATEGORI_IKON] = s.[KATEGORI_IKON], [SIRALAMA] = s.[SIRALAMA], [AKTIF_MI] = 1
        WHEN NOT MATCHED THEN
            INSERT ([KATEGORI_ADI], [KATEGORI_IKON], [SIRALAMA], [AKTIF_MI], [OLUSTURULMA_TARIHI])
            VALUES (s.[KATEGORI_ADI], s.[KATEGORI_IKON], s.[SIRALAMA], 1, SYSUTCDATETIME());
    END;

    IF OBJECT_ID(N'dbo.OTEL_KOSUL_SOZLUGU', N'U') IS NOT NULL
    BEGIN
        ;WITH src AS
        (
            SELECT * FROM (VALUES
                (NULL, N'Giriş ve Çıkış', N'Erken giriş imkanı', N'Müsaitlik durumuna göre erken giriş sunulabilir.', 10),
                (NULL, N'Giriş ve Çıkış', N'Geç çıkış imkanı', N'Müsaitlik durumuna göre geç çıkış sunulabilir.', 20),
                (NULL, N'Çocuk ve Ek Yatak', N'Bebek karyolası', N'Bebek karyolası ücretsiz veya ücretli sağlanabilir.', 30),
                (NULL, N'Çocuk ve Ek Yatak', N'Ekstra yatak', N'Ekstra yatak talebi oda tipine göre değerlendirilebilir.', 40),
                (NULL, N'Ödeme', N'Ön ödeme zorunluluğu', N'Bazı dönemlerde rezervasyon için ön ödeme gerekebilir.', 50),
                (NULL, N'Ödeme', N'Hasar depozitosu', N'Girişte hasar depozitosu uygulanabilir.', 60),
                (NULL, N'İptal ve İade', N'Ücretsiz iptal süresi', N'Belirli süreye kadar ücretsiz iptal uygulanabilir.', 70),
                (NULL, N'İptal ve İade', N'No-show cezası', N'Giriş yapılmayan rezervasyonlarda ceza uygulanabilir.', 80),
                (NULL, N'Evcil Hayvan', N'Evcil hayvan kabulü', N'Evcil hayvan kabulü otel politikasına göre belirlenir.', 90),
                (NULL, N'Sessizlik ve Etkinlik', N'Sessizlik saatleri', N'Gece sessizlik saatleri uygulanabilir.', 100),
                (NULL, N'Ziyaretçi Politikası', N'Ziyaretçi kabulü', N'Dış ziyaretçi kabulü otel kurallarına bağlıdır.', 110),
                (NULL, N'Yiyecek ve İçecek', N'Dışarıdan yiyecek içecek', N'Dışarıdan yiyecek veya içecek getirimi sınırlandırılabilir.', 120)
            ) x(OTEL_TIPI_ID, KATEGORI, KOSUL_ADI, ACIKLAMA, SIRALAMA)
        )
        MERGE [dbo].[OTEL_KOSUL_SOZLUGU] AS t
        USING src AS s
           ON COALESCE(t.[OTEL_TIPI_ID], 0) = COALESCE(s.[OTEL_TIPI_ID], 0)
          AND t.[KATEGORI] = s.[KATEGORI]
          AND t.[KOSUL_ADI] = s.[KOSUL_ADI]
        WHEN MATCHED THEN UPDATE SET
            [ACIKLAMA] = s.[ACIKLAMA], [SIRALAMA] = s.[SIRALAMA], [AKTIF_MI] = 1
        WHEN NOT MATCHED THEN
            INSERT ([OTEL_TIPI_ID], [KATEGORI], [KOSUL_ADI], [ACIKLAMA], [SIRALAMA], [AKTIF_MI], [OLUSTURULMA_TARIHI])
            VALUES (s.[OTEL_TIPI_ID], s.[KATEGORI], s.[KOSUL_ADI], s.[ACIKLAMA], s.[SIRALAMA], 1, SYSUTCDATETIME());
    END;

    IF OBJECT_ID(N'dbo.DESTEK_KATEGORILERI', N'U') IS NOT NULL
    BEGIN
        ;WITH src AS
        (
            SELECT * FROM (VALUES
                (N'Rezervasyonlar', N'rezervasyonlar', N'fa-calendar-check', N'Değişiklik, iptal ve onay süreçleri', N'#0b57d0', 10),
                (N'Ödemeler', N'odemeler', N'fa-credit-card', N'İade, fatura ve ödeme yöntemleri', N'#116466', 20),
                (N'Hesabım', N'hesabim', N'fa-user-shield', N'Giriş, profil ve güvenlik adımları', N'#7c3aed', 30),
                (N'Otel Bilgileri', N'otel-bilgileri', N'fa-hotel', N'Olanaklar, konum ve tesis içerikleri', N'#c05621', 40),
                (N'Kanal Entegrasyonu', N'kanal-entegrasyonu', N'fa-plug', N'Kanal eşleşmeleri ve entegrasyon yönetimi', N'#1d4ed8', 50),
                (N'Fiyat ve Komisyon', N'fiyat-ve-komisyon', N'fa-percent', N'Fiyat paritesi, komisyon ve marj yönetimi', N'#0f766e', 60),
                (N'Rezervasyon Operasyonu', N'rezervasyon-operasyonu', N'fa-bell-concierge', N'No-show, tarih değişikliği ve müsaitlik yönetimi', N'#9a3412', 70),
                (N'Ödeme ve Mutabakat', N'odeme-ve-mutabakat', N'fa-file-invoice-dollar', N'Ödeme akışı, fatura ve tahsilat mutabakatı', N'#1e40af', 80),
                (N'İçerik ve Görsel Kalite', N'icerik-ve-gorsel-kalite', N'fa-images', N'Listeleme metinleri, görseller ve içerik kalitesi', N'#6d28d9', 90),
                (N'Puan ve Yorum Yönetimi', N'puan-ve-yorum-yonetimi', N'fa-star-half-stroke', N'Misafir puanı, yorum ve itibar yönetimi', N'#b45309', 100),
                (N'Kampanya ve Görünürlük', N'kampanya-ve-gorunurluk', N'fa-bullhorn', N'Promosyon, vitrin ve görünürlük optimizasyonu', N'#be185d', 110),
                (N'Güvenlik ve Sahtecilik', N'guvenlik-ve-sahtecilik', N'fa-shield-halved', N'Dolandırıcılık, sahte rezervasyon ve hesap güvenliği', N'#991b1b', 120),
                (N'Firma Rezervasyonları', N'firma-rezervasyonlari', N'fa-building', N'Kurumsal rezervasyon, toplu konaklama ve faturalama', N'#1f2937', 130),
                (N'Satış Operasyonu', N'satis-operasyonu', N'fa-chart-line', N'Satış ekibi teklif, dönüşüm ve takip akışları', N'#065f46', 140)
            ) x(KATEGORI_ADI, SEO_SLUG, KATEGORI_IKON, KISA_ACIKLAMA, RENK_KODU, SIRALAMA)
        )
        MERGE [dbo].[DESTEK_KATEGORILERI] AS t
        USING src AS s
           ON t.[SEO_SLUG] = s.[SEO_SLUG]
        WHEN MATCHED THEN UPDATE SET
            [KATEGORI_ADI] = s.[KATEGORI_ADI], [KATEGORI_IKON] = s.[KATEGORI_IKON], [KISA_ACIKLAMA] = s.[KISA_ACIKLAMA],
            [RENK_KODU] = s.[RENK_KODU], [SIRALAMA] = s.[SIRALAMA], [DURUM] = 1, [GUNCELLENME_TARIHI] = SYSUTCDATETIME()
        WHEN NOT MATCHED THEN
            INSERT ([KATEGORI_ADI], [SEO_SLUG], [KATEGORI_IKON], [KISA_ACIKLAMA], [RENK_KODU], [SIRALAMA], [DURUM], [OLUSTURULMA_TARIHI], [GUNCELLENME_TARIHI])
            VALUES (s.[KATEGORI_ADI], s.[SEO_SLUG], s.[KATEGORI_IKON], s.[KISA_ACIKLAMA], s.[RENK_KODU], s.[SIRALAMA], 1, SYSUTCDATETIME(), SYSUTCDATETIME());
    END;

    IF OBJECT_ID(N'dbo.YARDIM_MERKEZI_KATEGORI_DETAYLARI', N'U') IS NOT NULL AND OBJECT_ID(N'dbo.DESTEK_KATEGORILERI', N'U') IS NOT NULL
    BEGIN
        ;WITH src AS
        (
            SELECT * FROM (VALUES
                (N'rezervasyonlar', N'Rezervasyon akışını uçtan uca yönetin', N'İptal, onay, tarih değişikliği ve misafir bilgilendirmesi için temel rehber.', N'/assets/img/support/reservations.jpg', N'<p>Rezervasyon yönetiminde tarih değişikliği, iptal, no-show ve misafir bilgilendirmesi aynı operasyon akışında ele alınmalıdır. Otel ve kullanıcı panelinde görünen durum isimleri birbiriyle tutarlı kalmalıdır.</p>'),
                (N'odemeler', N'Ödeme akışını netleştirin', N'Ödeme durumu, iade ve tahsilat adımlarını tek merkezden yönetin.', N'/assets/img/support/payments.jpg', N'<p>Ödeme süreçlerinde yöntem, durum, iade ve mutabakat adımlarının birlikte izlenmesi gerekir. Şablon ve fatura bağlantıları ödeme kayıtlarıyla senkron tutulmalıdır.</p>'),
                (N'hesabim', N'Hesap ve erişim güvenliği', N'Profil, doğrulama ve giriş güvenliği için temel adımlar.', N'/assets/img/support/account.jpg', N'<p>Hesap güvenliği; e-posta doğrulama, 2FA, oturum izleme ve profil güncellemelerini kapsar. Şüpheli oturumlar mutlaka kayıt altına alınmalıdır.</p>'),
                (N'otel-bilgileri', N'Tesis içeriğini profesyonel tutun', N'Otel özellikleri, konum ve iletişim alanlarının görünürlük etkisi.', N'/assets/img/support/hotel-content.jpg', N'<p>Tesis adı, adres, koordinat, olanaklar ve iletişim bilgileri rezervasyon dönüşümünü doğrudan etkiler. Görseller, oda açıklamaları ve konum verisi birlikte güncel tutulmalıdır.</p>'),
                (N'kanal-entegrasyonu', N'Kanal bağlantılarını eşleyin', N'Entegrasyon ve eşleşme hatalarını azaltan temel ilkeler.', N'/assets/img/support/integration.jpg', N'<p>Kanal entegrasyonunda oda eşleşmesi, fiyat/stok takvimi ve iptal politikalarının senkron olması gerekir. Hatalar için izleme ve geri dönüş planı tanımlanmalıdır.</p>'),
                (N'fiyat-ve-komisyon', N'Fiyat ve komisyon yönetimi', N'Parite, marj ve dönemsel komisyon görünürlüğü.', N'/assets/img/support/pricing.jpg', N'<p>Fiyat, komisyon ve indirim kayıtları tek kaynaktan beslenmelidir. Kullanıcı panelindeki görünen fiyat ile partner panelindeki satış fiyatı tutarlı kalmalıdır.</p>'),
                (N'rezervasyon-operasyonu', N'Operasyonel rezervasyon kontrolü', N'No-show, müsaitlik ve değişiklik yönetimi rehberi.', N'/assets/img/support/ops.jpg', N'<p>Rezervasyon operasyonunda check-in/check-out tarihi, oda adedi ve durum notları net olmalıdır. Her işlem e-posta ve audit izi ile takip edilmelidir.</p>'),
                (N'odeme-ve-mutabakat', N'Mutabakat sürecini şeffaflaştırın', N'Fatura, tahsilat ve dönem sonu kapanış adımları.', N'/assets/img/support/reconciliation.jpg', N'<p>Mutabakat ekranlarında komisyon, vergi ve tahsilat durumları birlikte izlenmelidir. Fatura yükleme ve onay adımları tarih bazında kayıt altına alınmalıdır.</p>'),
                (N'icerik-ve-gorsel-kalite', N'İçerik kalitesini yükseltin', N'Listeleme metinleri ve görseller için kalite standardı.', N'/assets/img/support/media.jpg', N'<p>Başlık, açıklama, fotoğraf ve oda özellikleri okunabilir ve güven veren bir standartta olmalıdır. Düşük kaliteli medya dönüşümü olumsuz etkiler.</p>'),
                (N'puan-ve-yorum-yonetimi', N'Yorum itibarını yönetin', N'Onaylı misafir yorumu, moderasyon ve yanıt ilkeleri.', N'/assets/img/support/reviews.jpg', N'<p>Yorum yönetiminde yalnızca onaylı konaklama sonrası içerik kabul edilmeli, itiraz ve moderasyon kayıtları açık tutulmalıdır.</p>'),
                (N'kampanya-ve-gorunurluk', N'Görünürlüğü kontrollü artırın', N'Kampanya, vitrin ve sıralama etkilerini izleyin.', N'/assets/img/support/campaigns.jpg', N'<p>Kampanyalar tarih, fiyat ve stok kurallarıyla birlikte yayınlanmalıdır. Görünürlük satın alımları için başlangıç ve bitiş tarihleri net olmalıdır.</p>'),
                (N'guvenlik-ve-sahtecilik', N'Güvenlik risklerini azaltın', N'Sahte rezervasyon, dolandırıcılık ve erişim güvenliği rehberi.', N'/assets/img/support/security.jpg', N'<p>IP, cihaz ve işlem logları üzerinden anomali tespiti yapılmalı; şüpheli kayıtlar güvenlik olayları akışına düşmelidir.</p>'),
                (N'firma-rezervasyonlari', N'Kurumsal konaklamayı yönetin', N'Personel atama, toplu oda ve faturalama akışı.', N'/assets/img/support/company.jpg', N'<p>Firma rezervasyonlarında oda adedi, tarih aralığı ve personel ataması birlikte ele alınmalıdır. Partner tarafı konaklayacak personeli detayda görebilmelidir.</p>'),
                (N'satis-operasyonu', N'Satış dönüşümünü hızlandırın', N'Satış ekibi teklif, rezervasyon ve takip akışları.', N'/assets/img/support/sales.jpg', N'<p>Satış paneli üzerinden oluşturulan talepler kullanıcı ve partner akışlarına eksiksiz bağlanmalıdır. Tekliften rezervasyona dönüşüm loglu ilerlemelidir.</p>')
            ) x(SEO_SLUG, HERO_BASLIK, HERO_ALT_BASLIK, HERO_GORSEL_URL, TAM_ACIKLAMA)
        ),
        normalized AS
        (
            SELECT k.[ID] AS DESTEK_KATEGORI_ID, s.[HERO_BASLIK], s.[HERO_ALT_BASLIK], s.[HERO_GORSEL_URL], s.[TAM_ACIKLAMA]
            FROM src s
            INNER JOIN [dbo].[DESTEK_KATEGORILERI] k ON k.[SEO_SLUG] = s.[SEO_SLUG]
        )
        MERGE [dbo].[YARDIM_MERKEZI_KATEGORI_DETAYLARI] AS t
        USING normalized AS s
           ON t.[DESTEK_KATEGORI_ID] = s.[DESTEK_KATEGORI_ID]
        WHEN MATCHED THEN UPDATE SET
            [HERO_BASLIK] = s.[HERO_BASLIK], [HERO_ALT_BASLIK] = s.[HERO_ALT_BASLIK], [HERO_GORSEL_URL] = s.[HERO_GORSEL_URL],
            [TAM_ACIKLAMA] = s.[TAM_ACIKLAMA], [AKTIF_MI] = 1, [GUNCELLENME_TARIHI] = SYSUTCDATETIME()
        WHEN NOT MATCHED THEN
            INSERT ([DESTEK_KATEGORI_ID], [HERO_BASLIK], [HERO_ALT_BASLIK], [HERO_GORSEL_URL], [TAM_ACIKLAMA], [AKTIF_MI], [GUNCELLENME_TARIHI])
            VALUES (s.[DESTEK_KATEGORI_ID], s.[HERO_BASLIK], s.[HERO_ALT_BASLIK], s.[HERO_GORSEL_URL], s.[TAM_ACIKLAMA], 1, SYSUTCDATETIME());
    END;

    IF OBJECT_ID(N'dbo.YARDIM_MERKEZI_KATEGORI_SSS', N'U') IS NOT NULL AND OBJECT_ID(N'dbo.DESTEK_KATEGORILERI', N'U') IS NOT NULL
    BEGIN
        ;WITH src AS
        (
            SELECT * FROM (VALUES
                (N'rezervasyonlar', N'Rezervasyon iptalinde hangi bilgiler görünmeli?', N'İptal tarihi, iptal eden taraf, neden ve varsa ceza koşulu aynı kayıt üzerinde görünmelidir.', 10),
                (N'odemeler', N'İade süreci nasıl izlenmeli?', N'İade edilen tutar, iade tarihi, yöntem ve kalan bakiye aynı ödeme kaydında tutulmalıdır.', 20),
                (N'hesabim', N'E-posta doğrulama neden zorunlu?', N'Hesap güvenliği ve işlem teyidi için e-posta doğrulama tamamlanmadan kritik panel akışları açılmamalıdır.', 30),
                (N'otel-bilgileri', N'Koordinat bilgisi neden önemlidir?', N'Harita görünürlüğü, rota servisleri ve konum filtreleri için doğru koordinat zorunludur.', 40),
                (N'kanal-entegrasyonu', N'Kanal eşleşme hatası nasıl anlaşılır?', N'Oda/stok/fiyat verisi senkron değilse kanal eşleşmesi incelenmeli ve mapping kayıtları gözden geçirilmelidir.', 50),
                (N'fiyat-ve-komisyon', N'Komisyon oranı hangi ekranda yönetilmeli?', N'Komisyon oranı tek merkezden yönetilmeli, partner ve admin ekranlarında aynı değeri göstermelidir.', 60),
                (N'rezervasyon-operasyonu', N'No-show kaydı ne zaman işlenmeli?', N'Giriş günü sonrası teyit alındığında no-show statüsü ve varsa ceza oranı sisteme işlenmelidir.', 70),
                (N'odeme-ve-mutabakat', N'Mutabakatta hangi alanlar zorunlu?', N'Dönem, rezervasyon no, brüt tutar, komisyon, vergi, tahsilat durumu ve fatura bağlantısı zorunludur.', 80),
                (N'icerik-ve-gorsel-kalite', N'Kapak görseli nasıl seçilmeli?', N'Tesisi doğru temsil eden, yüksek çözünürlüklü ve ilk bakışta güven veren görsel kapak olarak seçilmelidir.', 90),
                (N'puan-ve-yorum-yonetimi', N'Yorumlar ne zaman yayınlanmalı?', N'Sadece onaylı konaklama sonrası oluşturulan yorumlar moderasyon kurallarına göre yayınlanmalıdır.', 100),
                (N'kampanya-ve-gorunurluk', N'Kampanya bitince ne olur?', N'Kampanya bitiş tarihinde görünürlük ve indirim etkisi otomatik sona erer; fiyat normal satış kuralına döner.', 110),
                (N'guvenlik-ve-sahtecilik', N'Sahte rezervasyon nasıl işaretlenir?', N'Anormal IP, cihaz veya ödeme paterni görüldüğünde kayıt güvenlik incelemesine alınmalıdır.', 120),
                (N'firma-rezervasyonlari', N'Firma personel ataması zorunlu mu?', N'Hayır; ancak atanırsa partner panelinde oda bazında personel bilgisi görünmelidir.', 130),
                (N'satis-operasyonu', N'Satış kaydı kime bildirilir?', N'Satış üzerinden oluşan rezervasyon kullanıcıya ve partnere e-posta ile bildirilmelidir.', 140)
            ) x(SEO_SLUG, SORU, CEVAP, SIRALAMA)
        ),
        normalized AS
        (
            SELECT k.[ID] AS DESTEK_KATEGORI_ID, s.[SORU], s.[CEVAP], s.[SIRALAMA]
            FROM src s
            INNER JOIN [dbo].[DESTEK_KATEGORILERI] k ON k.[SEO_SLUG] = s.[SEO_SLUG]
        )
        MERGE [dbo].[YARDIM_MERKEZI_KATEGORI_SSS] AS t
        USING normalized AS s
           ON t.[DESTEK_KATEGORI_ID] = s.[DESTEK_KATEGORI_ID] AND t.[SORU] = s.[SORU]
        WHEN MATCHED THEN UPDATE SET
            [CEVAP] = s.[CEVAP], [SIRALAMA] = s.[SIRALAMA], [AKTIF_MI] = 1
        WHEN NOT MATCHED THEN
            INSERT ([DESTEK_KATEGORI_ID], [SORU], [CEVAP], [SIRALAMA], [AKTIF_MI], [OLUSTURULMA_TARIHI])
            VALUES (s.[DESTEK_KATEGORI_ID], s.[SORU], s.[CEVAP], s.[SIRALAMA], 1, SYSUTCDATETIME());
    END;

    IF OBJECT_ID(N'dbo.DESTEK_MAKALELERI', N'U') IS NOT NULL AND OBJECT_ID(N'dbo.DESTEK_KATEGORILERI', N'U') IS NOT NULL
    BEGIN
        ;WITH src AS
        (
            SELECT * FROM (VALUES
                (N'rezervasyonlar', N'Rezervasyon değişikliği ve iptal akışı', N'rezervasyon-degisikligi-ve-iptal-akisi', N'Rezervasyon tarih, oda ve iptal yönetimi için temel akış.', N'<p>Rezervasyon değişikliği, iptal ve onay adımları kullanıcı, partner ve admin tarafında aynı durum tanımlarıyla yürütülmelidir.</p>', N'fa-calendar-check', 1, 1, 10),
                (N'odemeler', N'Ödeme ve iade durumları nasıl izlenir?', N'odeme-ve-iade-durumlari-nasil-izlenir', N'Ödeme durumu, iade ve tahsilat alanlarının standardı.', N'<p>Ödeme kayıtlarında yöntem, durum, tahsilat ve iade alanları açık şekilde ayrılmalıdır.</p>', N'fa-credit-card', 1, 1, 20),
                (N'hesabim', N'Hesap güvenliği ve oturum takibi', N'hesap-guvenligi-ve-oturum-takibi', N'2FA, e-posta doğrulama ve oturum izleme notları.', N'<p>Şüpheli oturumları izlemek için cihaz, IP ve kullanıcı aracısı kayıtları takip edilmelidir.</p>', N'fa-user-shield', 1, 1, 30),
                (N'otel-bilgileri', N'Tesis içeriği nasıl güncel tutulur?', N'tesis-icerigi-nasil-guncel-tutulur', N'Görsel, olanak ve konum bilgilerinin standardı.', N'<p>Tesis içerikleri düzenli kontrol edilmeli, kapak görseli ve koordinat bilgileri doğrulanmalıdır.</p>', N'fa-hotel', 0, 1, 40),
                (N'fiyat-ve-komisyon', N'Fiyat paritesi ve komisyon takibi', N'fiyat-paritesi-ve-komisyon-takibi', N'Fiyat ve komisyon ekranlarında uyumlu veri akışı.', N'<p>Partner fiyatları ile kullanıcıya görünen fiyatlar aynı kaynak üzerinden hesaplanmalıdır.</p>', N'fa-percent', 1, 1, 50),
                (N'guvenlik-ve-sahtecilik', N'Sahte rezervasyon riskini azaltma', N'sahte-rezervasyon-riskini-azaltma', N'Riskli akışları izlemek için temel kontrol listesi.', N'<p>Sahte rezervasyon riski, IP ve cihaz izleriyle birlikte ödeme ve davranış örüntüleri üzerinden izlenmelidir.</p>', N'fa-shield-halved', 1, 1, 60)
            ) x(SEO_SLUG, BASLIK, MAK_SLUG, OZET, ICERIK, IKON, ONE_CIKAN_MI, YARDIM_MERKEZINDE_GOSTER, SIRALAMA)
        ),
        normalized AS
        (
            SELECT k.[ID] AS DESTEK_KATEGORI_ID, s.[BASLIK], s.[MAK_SLUG], s.[OZET], s.[ICERIK], s.[IKON], s.[ONE_CIKAN_MI], s.[YARDIM_MERKEZINDE_GOSTER], s.[SIRALAMA]
            FROM src s
            INNER JOIN [dbo].[DESTEK_KATEGORILERI] k ON k.[SEO_SLUG] = s.[SEO_SLUG]
        )
        MERGE [dbo].[DESTEK_MAKALELERI] AS t
        USING normalized AS s
           ON t.[SEO_SLUG] = s.[MAK_SLUG]
        WHEN MATCHED THEN UPDATE SET
            [DESTEK_KATEGORI_ID] = s.[DESTEK_KATEGORI_ID], [BASLIK] = s.[BASLIK], [OZET] = s.[OZET], [ICERIK] = s.[ICERIK],
            [IKON] = s.[IKON], [ONE_CIKAN_MI] = s.[ONE_CIKAN_MI], [YARDIM_MERKEZINDE_GOSTER] = s.[YARDIM_MERKEZINDE_GOSTER],
            [SIRALAMA] = s.[SIRALAMA], [DURUM] = 1, [GUNCELLENME_TARIHI] = SYSUTCDATETIME()
        WHEN NOT MATCHED THEN
            INSERT ([DESTEK_KATEGORI_ID], [BASLIK], [SEO_SLUG], [OZET], [ICERIK], [IKON], [ONE_CIKAN_MI], [YARDIM_MERKEZINDE_GOSTER], [SIRALAMA], [DURUM], [OLUSTURULMA_TARIHI], [GUNCELLENME_TARIHI])
            VALUES (s.[DESTEK_KATEGORI_ID], s.[BASLIK], s.[MAK_SLUG], s.[OZET], s.[ICERIK], s.[IKON], s.[ONE_CIKAN_MI], s.[YARDIM_MERKEZINDE_GOSTER], s.[SIRALAMA], 1, SYSUTCDATETIME(), SYSUTCDATETIME());
    END;

    IF OBJECT_ID(N'dbo.DESTEK_KANALLARI', N'U') IS NOT NULL
    BEGIN
        ;WITH src AS
        (
            SELECT * FROM (VALUES
                (N'Canlı Destek', N'canli_destek', N'fa-headset', N'Mesai saatlerinde anlık destek alın.', N'Görüşmeyi başlat', N'/yardim-merkezi#destek-kanallari', N'Operasyon saatlerinde öncelikli yanıt.', N'primary', 10),
                (N'E-posta Desteği', N'eposta', N'fa-envelope', N'Teknik ve operasyonel taleplerinizi e-posta ile iletin.', N'E-posta gönder', N'mailto:destek@otelturizm.com', N'destek@otelturizm.com', N'info', 20),
                (N'Kurumsal Satış Hattı', N'kurumsal_satis', N'fa-building', N'Firma ve toplu konaklama talepleri için satış ekibine ulaşın.', N'Satış ekibine yaz', N'mailto:info+satis@gmail.com', N'Kurumsal rezervasyon ve teklif akışı', N'success', 30),
                (N'Yardım Merkezi', N'yardim_merkezi', N'fa-circle-question', N'Kategori bazlı makale ve SSS içeriklerine ulaşın.', N'Yardım merkezini aç', N'/yardim-merkezi', N'24/7 self servis içerik', N'warning', 40),
                (N'Güvenlik Bildirimi', N'guvenlik', N'fa-shield-halved', N'Güvenlik ve hesap ihlali bildirimleri için özel kanal.', N'Güvenliğe yaz', N'mailto:guvenlik@otelturizm.com', N'Hesap güvenliği ve şüpheli işlem bildirimleri', N'danger', 50)
            ) x(KANAL_ADI, KANAL_TURU, IKON, ACIKLAMA, BUTON_METIN, BAGLANTI_URL, EK_BILGI, RENK_TONU, SIRALAMA)
        )
        MERGE [dbo].[DESTEK_KANALLARI] AS t
        USING src AS s
           ON t.[KANAL_TURU] = s.[KANAL_TURU]
        WHEN MATCHED THEN UPDATE SET
            [KANAL_ADI] = s.[KANAL_ADI], [IKON] = s.[IKON], [ACIKLAMA] = s.[ACIKLAMA], [BUTON_METIN] = s.[BUTON_METIN],
            [BAGLANTI_URL] = s.[BAGLANTI_URL], [EK_BILGI] = s.[EK_BILGI], [RENK_TONU] = s.[RENK_TONU], [SIRALAMA] = s.[SIRALAMA],
            [AKTIF_MI] = 1, [GUNCELLENME_TARIHI] = SYSUTCDATETIME()
        WHEN NOT MATCHED THEN
            INSERT ([KANAL_ADI], [KANAL_TURU], [IKON], [ACIKLAMA], [BUTON_METIN], [BAGLANTI_URL], [EK_BILGI], [RENK_TONU], [SIRALAMA], [AKTIF_MI], [OLUSTURULMA_TARIHI], [GUNCELLENME_TARIHI])
            VALUES (s.[KANAL_ADI], s.[KANAL_TURU], s.[IKON], s.[ACIKLAMA], s.[BUTON_METIN], s.[BAGLANTI_URL], s.[EK_BILGI], s.[RENK_TONU], s.[SIRALAMA], 1, SYSUTCDATETIME(), SYSUTCDATETIME());
    END;

    IF OBJECT_ID(N'dbo.PLATFORM_EKIP_UYELERI', N'U') IS NOT NULL
    BEGIN
        ;WITH src AS
        (
            SELECT * FROM (VALUES
                (N'Deniz Aksoy', N'Platform yönetimi', N'info+admin@otelturizm.com', N'Platform genel yönetimi ve yayın kararları', N'', 10),
                (N'İrem Yalçın', N'İnsan kaynakları', N'info+ik.yonetici@otelturizm.com', N'İnsan kaynakları ve ekip süreçleri', N'', 20),
                (N'Hande Mert', N'Hukuk & uyum', N'info+hukuk.musaviri@otelturizm.com', N'Hukuki süreçler ve uyum akışları', N'', 30),
                (N'Volkan Yıldız', N'Veri & altyapı', N'info+dba@otelturizm.com', N'Veri tabanı ve altyapı yönetimi', N'', 40),
                (N'Yiğit Uslu', N'Uygulama geliştirme', N'info+yazilim.uzmani@otelturizm.com', N'Uygulama geliştirme ve teslim süreçleri', N'', 50),
                (N'Tolga Levent', N'Mimari & kod kalitesi', N'info+teknik.lider@otelturizm.com', N'Mimari kararlar ve kod standardı', N'', 60),
                (N'Pelin Yaman', N'Büyüme & kampanya', N'info+pazarlama.yonetici@otelturizm.com', N'Kampanya ve görünürlük yönetimi', N'', 70),
                (N'Derya Uçar', N'Müşteri destek', N'info+destek.uzmani@otelturizm.com', N'Kullanıcı destek süreçleri', N'', 80),
                (N'Defne Yıldırım', N'Destek operasyonları', N'info+destek.yonetici@otelturizm.com', N'Destek SLA ve operasyon takibi', N'', 90),
                (N'Okan Yalın', N'Operasyon & SLA', N'info+operasyon.yonetici@otelturizm.com', N'Operasyonel kalite ve SLA yönetimi', N'', 100),
                (N'Merve Uzun', N'Muhasebe işlemleri', N'info+muhasebe.uzmani@otelturizm.com', N'Muhasebe ve belge akışı', N'', 110),
                (N'Faruk Yılmaz', N'Finans & tahsilat', N'info+finans.yonetici@otelturizm.com', N'Komisyon ve tahsilat yönetimi', N'', 120),
                (N'Gökhan Mutlu', N'Yönetim', N'info+genelmudur@otelturizm.com', N'Üst yönetim ve stratejik onay akışları', N'', 130)
            ) x(AD_SOYAD, UNVAN, EPOSTA, ACIKLAMA, AVATAR_URL, SIRALAMA)
        )
        MERGE [dbo].[PLATFORM_EKIP_UYELERI] AS t
        USING src AS s
           ON t.[EPOSTA] = s.[EPOSTA]
        WHEN MATCHED THEN UPDATE SET
            [AD_SOYAD] = s.[AD_SOYAD], [UNVAN] = s.[UNVAN], [ACIKLAMA] = s.[ACIKLAMA], [AVATAR_URL] = NULLIF(s.[AVATAR_URL], N''),
            [SIRALAMA] = s.[SIRALAMA], [AKTIF_MI] = 1, [GUNCELLENME_TARIHI] = SYSUTCDATETIME()
        WHEN NOT MATCHED THEN
            INSERT ([AD_SOYAD], [UNVAN], [EPOSTA], [ACIKLAMA], [AVATAR_URL], [SIRALAMA], [AKTIF_MI], [OLUSTURULMA_TARIHI])
            VALUES (s.[AD_SOYAD], s.[UNVAN], s.[EPOSTA], s.[ACIKLAMA], NULLIF(s.[AVATAR_URL], N''), s.[SIRALAMA], 1, SYSUTCDATETIME());
    END;

    IF OBJECT_ID(N'dbo.SSS_KATEGORILERI', N'U') IS NOT NULL
    BEGIN
        ;WITH src AS
        (
            SELECT * FROM (VALUES
                (N'Rezervasyon', N'rezervasyon', N'fa-calendar-check', 10),
                (N'Ödeme', N'odeme', N'fa-credit-card', 20),
                (N'Hesap ve Güvenlik', N'hesap-ve-guvenlik', N'fa-user-lock', 30),
                (N'Partner Operasyonu', N'partner-operasyonu', N'fa-hotel', 40),
                (N'Firma İşlemleri', N'firma-islemleri', N'fa-building', 50),
                (N'Kampanya ve Fiyat', N'kampanya-ve-fiyat', N'fa-tags', 60)
            ) x(KATEGORI_ADI, SEO_SLUG, IKON, SIRALAMA)
        )
        MERGE [dbo].[SSS_KATEGORILERI] AS t
        USING src AS s
           ON t.[SEO_SLUG] = s.[SEO_SLUG]
        WHEN MATCHED THEN UPDATE SET
            [KATEGORI_ADI] = s.[KATEGORI_ADI], [IKON] = s.[IKON], [SIRALAMA] = s.[SIRALAMA], [AKTIF_MI] = 1, [GUNCELLENME_TARIHI] = SYSUTCDATETIME()
        WHEN NOT MATCHED THEN
            INSERT ([KATEGORI_ADI], [SEO_SLUG], [IKON], [SIRALAMA], [AKTIF_MI], [OLUSTURULMA_TARIHI], [GUNCELLENME_TARIHI])
            VALUES (s.[KATEGORI_ADI], s.[SEO_SLUG], s.[IKON], s.[SIRALAMA], 1, SYSUTCDATETIME(), SYSUTCDATETIME());
    END;

    IF OBJECT_ID(N'dbo.SSS_SORULARI', N'U') IS NOT NULL AND OBJECT_ID(N'dbo.SSS_KATEGORILERI', N'U') IS NOT NULL
    BEGIN
        ;WITH src AS
        (
            SELECT * FROM (VALUES
                (N'rezervasyon', N'Rezervasyonumu nasıl iptal edebilirim?', N'Kullanıcı panelindeki rezervasyon detayından, check-in tarihi gelmeden iptal talebi oluşturabilirsiniz. Otel politikası ve ücretsiz iptal süresi rezervasyon kartında görünür.', 1, 10),
                (N'rezervasyon', N'Rezervasyon güncellemesinde fiyat değişir mi?', N'Evet. Tarih, oda veya kişi sayısı değişirse geçerli güncel fiyatlar yeniden hesaplanır ve taraflara bildirilir.', 1, 20),
                (N'odeme', N'Kapıda ödeme ile online ödeme arasındaki fark nedir?', N'Kapıda ödeme tesis tarafından check-in sırasında tahsil edilir; online ödeme ise rezervasyon sırasında veya ön ödeme akışında tamamlanır.', 1, 10),
                (N'odeme', N'İade süreci ne kadar sürer?', N'İade süresi kullanılan ödeme yöntemine ve sağlayıcıya göre değişir; durum panel ve bildirim ekranlarında izlenir.', 0, 20),
                (N'hesap-ve-guvenlik', N'E-posta doğrulaması neden zorunlu?', N'Hesap güvenliği, şifre sıfırlama ve rezervasyon bildirimlerinin doğru kişiye ulaşması için zorunludur.', 1, 10),
                (N'hesap-ve-guvenlik', N'İki aşamalı doğrulama nasıl çalışır?', N'Giriş sırasında tek kullanımlık kod e-posta kanalıyla gönderilir; doğrulama tamamlanmadan yeni oturum açılmaz.', 0, 20),
                (N'partner-operasyonu', N'Partner panelinde hangi işlemler yapılabilir?', N'Oda, fiyat, kampanya, görsel, rezervasyon ve finans süreçleri partner panelinden yönetilebilir.', 1, 10),
                (N'partner-operasyonu', N'Belge onayı olmadan otel yayına açılır mı?', N'Hayır. Gerekli evraklar admin onayı almadan tesis yayınlanmaz.', 1, 20),
                (N'firma-islemleri', N'Firma rezervasyonunda personel atamak zorunlu mu?', N'Hayır; ancak atanırsa partner otel detayda kimin hangi odada kalacağını görebilir.', 1, 10),
                (N'firma-islemleri', N'Firma faturaları nerede görüntülenir?', N'Firma panelindeki faturalar alanında konaklama ve tahsilat belgeleri listelenir.', 0, 20),
                (N'kampanya-ve-fiyat', N'Favori fiyat alarmı nasıl çalışır?', N'Kullanıcı hedef fiyat belirler; partner fiyatı bu seviyeye indiğinde uygun şablon ile e-posta bildirimi gönderilir.', 1, 10),
                (N'kampanya-ve-fiyat', N'Kampanya bitince fiyat ne olur?', N'Kampanya bitiminde fiyat normal satış kuralına döner ve vitrin görünürlüğü otomatik sona erer.', 0, 20)
            ) x(SEO_SLUG, SORU, CEVAP, ONE_CIKAN_MI, SIRALAMA)
        ),
        normalized AS
        (
            SELECT k.[ID] AS SSS_KATEGORI_ID, s.[SORU], s.[CEVAP], s.[ONE_CIKAN_MI], s.[SIRALAMA]
            FROM src s
            INNER JOIN [dbo].[SSS_KATEGORILERI] k ON k.[SEO_SLUG] = s.[SEO_SLUG]
        )
        MERGE [dbo].[SSS_SORULARI] AS t
        USING normalized AS s
           ON t.[SSS_KATEGORI_ID] = s.[SSS_KATEGORI_ID] AND t.[SORU] = s.[SORU]
        WHEN MATCHED THEN UPDATE SET
            [CEVAP] = s.[CEVAP], [ONE_CIKAN_MI] = s.[ONE_CIKAN_MI], [SIRALAMA] = s.[SIRALAMA], [AKTIF_MI] = 1, [GUNCELLENME_TARIHI] = SYSUTCDATETIME()
        WHEN NOT MATCHED THEN
            INSERT ([SSS_KATEGORI_ID], [SORU], [CEVAP], [ONE_CIKAN_MI], [SIRALAMA], [AKTIF_MI], [OLUSTURULMA_TARIHI], [GUNCELLENME_TARIHI])
            VALUES (s.[SSS_KATEGORI_ID], s.[SORU], s.[CEVAP], s.[ONE_CIKAN_MI], s.[SIRALAMA], 1, SYSUTCDATETIME(), SYSUTCDATETIME());
    END;

    IF OBJECT_ID(N'dbo.YARDIM_MERKEZI_ICERIKLER', N'U') IS NOT NULL
    BEGIN
        ;WITH src AS
        (
            SELECT * FROM (VALUES
                (N'rehber', N'Rezervasyon Politikası', N'rezervasyon-politikasi', N'Rezervasyon akışları için temel operasyon notları', N'Rezervasyon sürecini kontrol altında tutun', N'İptal, değişiklik ve onay adımlarını tek politikada toplayın.', N'/assets/img/support/reservations.jpg', N'<p>Rezervasyon politikası; onay, iptal, no-show, güncelleme ve misafir iletişim adımlarını kapsar. Kullanıcıya gösterilen durum ile partner ve admin tarafındaki durumların aynı terminolojiye sahip olması gerekir.</p>', N'fa-book-open', 10, 1),
                (N'rehber', N'Ödeme Güvenliği', N'odeme-guvenligi', N'Ödeme ve tahsilat akışı için güvenlik esasları', N'Ödeme akışını güvenli yapılandırın', N'Yöntem, durum ve iade kayıtlarını standardize edin.', N'/assets/img/support/payments.jpg', N'<p>Ödeme güvenliği için yöntem/durum sözlükleri boş kalmamalı, iade ve tahsilat durumları ayrı kolonlarda izlenmelidir. E-posta ve fatura bildirimleri ödeme kayıtlarıyla eşleştirilmelidir.</p>', N'fa-lock', 20, 1),
                (N'rehber', N'Partner Operasyon Rehberi', N'partner-operasyon-rehberi', N'Partner tesis yönetimi için temel çalışma standardı', N'Partner operasyon standardı', N'Tesis, oda, fiyat ve rezervasyon akışlarını tek yerden yönetin.', N'/assets/img/support/ops.jpg', N'<p>Partner operasyon rehberi; evrak, yayın, fotoğraf, fiyat/stok, oda özellikleri ve rezervasyon yönetimi için tek akış standardını tanımlar.</p>', N'fa-clipboard-list', 30, 1)
            ) x(ICERIK_TURU, BASLIK, SEO_SLUG, OZET, HERO_BASLIK, HERO_ALT_BASLIK, HERO_GORSEL_URL, ICERIK, IKON, SIRALAMA, ONE_CIKAN_MI)
        )
        MERGE [dbo].[YARDIM_MERKEZI_ICERIKLER] AS t
        USING src AS s
           ON t.[SEO_SLUG] = s.[SEO_SLUG]
        WHEN MATCHED THEN UPDATE SET
            [ICERIK_TURU] = s.[ICERIK_TURU], [BASLIK] = s.[BASLIK], [OZET] = s.[OZET], [HERO_BASLIK] = s.[HERO_BASLIK],
            [HERO_ALT_BASLIK] = s.[HERO_ALT_BASLIK], [HERO_GORSEL_URL] = s.[HERO_GORSEL_URL], [ICERIK] = s.[ICERIK], [IKON] = s.[IKON],
            [SIRALAMA] = s.[SIRALAMA], [ONE_CIKAN_MI] = s.[ONE_CIKAN_MI], [AKTIF_MI] = 1, [GUNCELLENME_TARIHI] = SYSUTCDATETIME()
        WHEN NOT MATCHED THEN
            INSERT ([ICERIK_TURU], [BASLIK], [SEO_SLUG], [OZET], [HERO_BASLIK], [HERO_ALT_BASLIK], [HERO_GORSEL_URL], [ICERIK], [IKON], [SIRALAMA], [ONE_CIKAN_MI], [AKTIF_MI], [OLUSTURULMA_TARIHI], [GUNCELLENME_TARIHI])
            VALUES (s.[ICERIK_TURU], s.[BASLIK], s.[SEO_SLUG], s.[OZET], s.[HERO_BASLIK], s.[HERO_ALT_BASLIK], s.[HERO_GORSEL_URL], s.[ICERIK], s.[IKON], s.[SIRALAMA], s.[ONE_CIKAN_MI], 1, SYSUTCDATETIME(), SYSUTCDATETIME());
    END;

    IF OBJECT_ID(N'dbo.BILDIRIM_SABLONLARI', N'U') IS NOT NULL
    BEGIN
        ;WITH src AS
        (
            SELECT * FROM (VALUES
                (N'login_2fa_email', N'Giriş Güvenlik Kodu', N'E-posta', N'tr', N'Giriş güvenlik kodunuz', N'Giriş Güvenlik Kodu', N'Views/Email/Giris_Guvenlik_Kodu.cshtml', N'verification_code,user_first_name,login_time'),
                (N'email_verify', N'E-posta Doğrulama', N'E-posta', N'tr', N'E-posta adresinizi onaylayın', N'E-posta Doğrulama', N'Views/Email/E-posta_Adresini_Onayla.cshtml', N'user_first_name,user_email,registration_date,verification_link,verification_code'),
                (N'password_reset', N'Şifre Sıfırlama', N'E-posta', N'tr', N'Şifre sıfırlama talebi', N'Şifre Sıfırlama', N'Views/Email/Sifre_Sifirlama_Talebi.cshtml', N'user_first_name,user_email,reset_link,request_ip'),
                (N'reservation_received_customer', N'Rezervasyon Talebi Alındı', N'E-posta', N'tr', N'Rezervasyon talebiniz alındı', N'Rezervasyon Talebi Alındı', N'Views/Email/Rezervasyon_Talebi_Alindi.cshtml', N'reservation_no,hotel_name,guest_full_name,check_in_date,check_out_date,room_type_name,total_price'),
                (N'reservation_confirmed_customer', N'Rezervasyon Onaylandı', N'E-posta', N'tr', N'Rezervasyonunuz onaylandı', N'Rezervasyon Onaylandı', N'Views/Email/RezervasyonOnaylandi.cshtml', N'reservation_no,hotel_name,guest_full_name,check_in_date,check_out_date,room_type_name,total_price'),
                (N'reservation_new_partner', N'Partner Yeni Rezervasyon', N'E-posta', N'tr', N'Yeni rezervasyon onayı', N'Partner Yeni Rezervasyon', N'Views/Email/Partner_Yeni_Rezervasyon.cshtml', N'reservation_no,hotel_name,guest_full_name,check_in_date,check_out_date,room_type_name,total_price'),
                (N'reservation_rejected_customer', N'Rezervasyon Reddedildi', N'E-posta', N'tr', N'Rezervasyon talebiniz reddedildi', N'Rezervasyon Reddedildi', N'Views/Email/Rezervasyon_Reddedildi.cshtml', N'reservation_no,hotel_name,guest_full_name,cancel_reason'),
                (N'reservation_guest_message', N'Rezervasyon Mesajı', N'E-posta', N'tr', N'Rezervasyon mesajınız var', N'Rezervasyon Mesajı', N'Views/Email/Rezervasyon_Mesaji.cshtml', N'reservation_no,hotel_name,message_body,guest_full_name'),
                (N'reservation_cancelled_partner', N'Partner Rezervasyon İptal', N'E-posta', N'tr', N'Rezervasyon iptal edildi', N'Partner Rezervasyon İptal', N'Views/Email/Partner_Rezervasyon_Iptal.cshtml', N'hotel_manager_name,hotel_name,booking_reference,guest_full_name,check_in_date,check_out_date,room_type_name,total_price,cancel_reason'),
                (N'favorite_price_alert_match', N'Favori Fiyat Alarmı', N'E-posta', N'tr', N'Fiyat alarmınız tetiklendi', N'Favori Fiyat Alarmı', N'Views/Email/Favori_Fiyat_Alarmi.cshtml', N'hotel_name,room_name,old_price,new_price,check_in_date,check_out_date'),
                (N'contract_delivery', N'Sözleşme Bildirimi', N'E-posta', N'tr', N'Sözleşme ve KVKK paketi', N'Sözleşme Bildirimi', N'Views/Email/Sozlesme_Bildirimi.cshtml', N'recipient_name,module_label,contract_bundle_title,contract_sections_html,primary_contract_url'),
                (N'firma_reservation_created_company', N'Kurumsal Rezervasyon Firma Bildirimi', N'E-posta', N'tr', N'Kurumsal rezervasyon talebiniz alındı', N'Kurumsal Rezervasyon', N'Views/Email/Firma_Rezervasyon_Bildirimi.cshtml', N'reservation_no,hotel_name,company_name,check_in_date,check_out_date,room_count,total_price'),
                (N'firma_reservation_created_partner', N'Kurumsal Rezervasyon Partner Bildirimi', N'E-posta', N'tr', N'Yeni kurumsal rezervasyon talebi', N'Kurumsal Rezervasyon', N'Views/Email/Firma_Rezervasyon_Bildirimi.cshtml', N'reservation_no,hotel_name,company_name,check_in_date,check_out_date,room_count,total_price'),
                (N'system_health_link_report', N'Sistem Sağlığı Link Raporu', N'E-posta', N'tr', N'Sistem link kontrol raporu', N'Link Kontrol Raporu', N'Views/Email/Link_Kontrol_Raporu.cshtml', N'report_title,report_summary,report_items,generated_at'),
                (N'admin_routing_notice', N'Admin Yönlendirme Bildirimi', N'E-posta', N'tr', N'Admin bildirim yönlendirmesi', N'Admin Routing', N'Views/Email/tr/Admin_Routing_Bildirimi.cshtml', N'email_subject,badge,title,intro,detail_html,primary_url,primary_label,event_code,occurred_at'),
                (N'partner_facility_user_invite', N'Tesis Kullanıcı Daveti', N'E-posta', N'tr', N'Tesis kullanıcı daveti', N'Tesis Kullanıcı Daveti', N'Views/Email/Partner_Tesis_Kullanıcı_Daveti.cshtml', N'partner_name,hotel_name,invite_link,recipient_name'),
                (N'developer_feedback', N'Beta Geri Bildirim', N'E-posta', N'tr', N'[BETA BİLDİRİM] {{title}}', N'Beta Geri Bildirim', N'Views/Email/tr/Developer_Bildirim.cshtml', N'feedback_id,panel_key,feedback_type,title,content,page_url,page_title,user_full_name,user_email,account_type,ip_address,user_agent,viewport,device_info,image_url,created_at')
            ) x(SABLON_KODU, SABLON_ADI, TUR, DIL, KONU, BASLIK, ICERIK, DEGISKENLER)
        )
        MERGE [dbo].[BILDIRIM_SABLONLARI] AS t
        USING src AS s
           ON t.[SABLON_KODU] = s.[SABLON_KODU] AND t.[TUR] = s.[TUR] AND t.[DIL] = s.[DIL]
        WHEN MATCHED THEN UPDATE SET
            [SABLON_ADI] = s.[SABLON_ADI], [KONU] = s.[KONU], [BASLIK] = s.[BASLIK], [ICERIK] = s.[ICERIK], [DEGISKENLER] = s.[DEGISKENLER], [AKTIF_MI] = 1
        WHEN NOT MATCHED THEN
            INSERT ([SABLON_KODU], [SABLON_ADI], [TUR], [DIL], [KONU], [BASLIK], [ICERIK], [DEGISKENLER], [AKTIF_MI], [OLUSTURULMA_TARIHI])
            VALUES (s.[SABLON_KODU], s.[SABLON_ADI], s.[TUR], s.[DIL], s.[KONU], s.[BASLIK], s.[ICERIK], s.[DEGISKENLER], 1, SYSUTCDATETIME());
    END;

    IF OBJECT_ID(N'dbo.ADMIN_EPOSTA_YONLENDIRME', N'U') IS NOT NULL
    BEGIN
        ;WITH src AS
        (
            SELECT * FROM (VALUES
                (N'partner_kayit', N'Partner başvurusu', N'Yeni partner ve tesis başvuruları için yönetim bildirimi', N'irmhro0@gmail.com', 1),
                (N'firma_kayit', N'Firma başvurusu', N'Yeni kurumsal firma başvuruları için yönetim bildirimi', N'irmhro0@gmail.com', 1)
            ) x(OLAY_KODU, BASLIK, ACIKLAMA, HEDEF_EPOSTALAR, AKTIF_MI)
        )
        MERGE [dbo].[ADMIN_EPOSTA_YONLENDIRME] AS t
        USING src AS s
           ON t.[OLAY_KODU] = s.[OLAY_KODU]
        WHEN MATCHED THEN UPDATE SET
            [BASLIK] = s.[BASLIK], [ACIKLAMA] = s.[ACIKLAMA], [HEDEF_EPOSTALAR] = s.[HEDEF_EPOSTALAR], [AKTIF_MI] = s.[AKTIF_MI], [GUNCELLENME_UTC] = SYSUTCDATETIME()
        WHEN NOT MATCHED THEN
            INSERT ([OLAY_KODU], [BASLIK], [ACIKLAMA], [HEDEF_EPOSTALAR], [AKTIF_MI], [GUNCELLENME_UTC])
            VALUES (s.[OLAY_KODU], s.[BASLIK], s.[ACIKLAMA], s.[HEDEF_EPOSTALAR], s.[AKTIF_MI], SYSUTCDATETIME());
    END;

    IF OBJECT_ID(N'dbo.EPOSTA_SERVISLERI', N'U') IS NOT NULL
    BEGIN
        ;WITH src AS
        (
            SELECT * FROM (VALUES
                (N'platform_info', N'Platform Genel Gönderici', N'info@otelturizm.com'),
                (N'platform_bildiri', N'Platform Bildirim Gönderici', N'bildiri@otelturizm.com'),
                (N'platform_bilgi', N'Platform Bilgi Gönderici', N'bilgi@otelturizm.com'),
                (N'platform_destek', N'Platform Destek Gönderici', N'destek@otelturizm.com'),
                (N'platform_guvenlik', N'Platform Güvenlik Gönderici', N'guvenlik@otelturizm.com'),
                (N'platform_odeme', N'Platform Ödeme Gönderici', N'odeme@otelturizm.com'),
                (N'platform_rezervasyon', N'Platform Rezervasyon Gönderici', N'rezervasyon@otelturizm.com')
            ) x(SERVIS_KODU, SERVIS_ADI, GONDEREN_EPOSTA)
        )
        MERGE [dbo].[EPOSTA_SERVISLERI] AS t
        USING src AS s
           ON t.[SERVIS_KODU] = s.[SERVIS_KODU]
        WHEN MATCHED THEN UPDATE SET
            [SERVIS_ADI] = s.[SERVIS_ADI], [SAGLAYICI] = N'SMTP', [VARSAYILAN_MI] = CASE WHEN s.[SERVIS_KODU]=N'platform_info' THEN 1 ELSE 0 END,
            [AKTIF_MI] = COALESCE(t.[AKTIF_MI], 0), [GONDEREN_AD] = N'otelturizm.com', [GONDEREN_EPOSTA] = s.[GONDEREN_EPOSTA],
            [YANITLA_EPOSTA] = s.[GONDEREN_EPOSTA], [SMTP_HOST] = COALESCE(NULLIF(t.[SMTP_HOST], N''), N'umay.muvhost.com'),
            [SMTP_PORT] = COALESCE(NULLIF(t.[SMTP_PORT], 0), 465), [SMTP_KULLANICI_ADI] = COALESCE(NULLIF(t.[SMTP_KULLANICI_ADI], N''), s.[GONDEREN_EPOSTA]),
            [GUVENLIK_TIPI] = COALESCE(NULLIF(t.[GUVENLIK_TIPI], N''), N'SSL/TLS'), [BAGLANTI_ZAMAN_ASIMI_SANIYE] = COALESCE(NULLIF(t.[BAGLANTI_ZAMAN_ASIMI_SANIYE], 0), 45),
            [GONDERIM_LIMITI_DAKIKA] = COALESCE(NULLIF(t.[GONDERIM_LIMITI_DAKIKA], 0), 60), [GONDERIM_LIMITI_SAAT] = COALESCE(NULLIF(t.[GONDERIM_LIMITI_SAAT], 0), 600),
            [GONDERIM_LIMITI_GUN] = COALESCE(NULLIF(t.[GONDERIM_LIMITI_GUN], 0), 5000), [TEST_MODU] = COALESCE(t.[TEST_MODU], 0),
            [HATA_ESIGI] = COALESCE(NULLIF(t.[HATA_ESIGI], 0), 5), [METADATA] = COALESCE(t.[METADATA], N'{"requires_secret":true,"seed":"20260525"}'),
            [SON_HATA_MESAJI] = CASE WHEN NULLIF(COALESCE(t.[SMTP_SIFRE], N''), N'') IS NULL THEN N'SMTP şifresi operasyon sırasında tanımlanmalı.' ELSE t.[SON_HATA_MESAJI] END,
            [GUNCELLENME_TARIHI] = SYSUTCDATETIME()
        WHEN NOT MATCHED THEN
            INSERT ([SERVIS_KODU], [SERVIS_ADI], [SAGLAYICI], [VARSAYILAN_MI], [AKTIF_MI], [GONDEREN_AD], [GONDEREN_EPOSTA], [YANITLA_EPOSTA], [SMTP_HOST], [SMTP_PORT], [SMTP_KULLANICI_ADI], [SMTP_SIFRE], [SIFRE_SIFRELENMIS_MI], [GUVENLIK_TIPI], [BAGLANTI_ZAMAN_ASIMI_SANIYE], [GONDERIM_LIMITI_DAKIKA], [GONDERIM_LIMITI_SAAT], [GONDERIM_LIMITI_GUN], [TEST_MODU], [HATA_ESIGI], [SON_HATA_MESAJI], [METADATA], [OLUSTURULMA_TARIHI], [GUNCELLENME_TARIHI])
            VALUES (s.[SERVIS_KODU], s.[SERVIS_ADI], N'SMTP', CASE WHEN s.[SERVIS_KODU]=N'platform_info' THEN 1 ELSE 0 END, 0, N'otelturizm.com', s.[GONDEREN_EPOSTA], s.[GONDEREN_EPOSTA], N'umay.muvhost.com', 465, s.[GONDEREN_EPOSTA], NULL, 0, N'SSL/TLS', 45, 60, 600, 5000, 0, 5, N'SMTP şifresi operasyon sırasında tanımlanmalı.', N'{"requires_secret":true,"seed":"20260525"}', SYSUTCDATETIME(), SYSUTCDATETIME());
    END;

    COMMIT;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK;
    THROW;
END CATCH;
