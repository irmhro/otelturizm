/*
  CANLI VERITABANI GÜVENLİ UYGULAMA RUNBOOK'u
  -----------------------------------------
  Amaç:
  - Var olan verileri BOZMADAN, migration/seed scriptlerini eksiksiz uygulamak
  - Her scripti bir kere çalıştırmak (tekrar çalıştırmada zarar vermemek)
  - Hangi script ne zaman uygulandı kayıt altına almak
  - Uygulama öncesi yedek alınmasını hatırlatmak

  ÖNEMLİ:
  - Bu dosya "tek tık" çalıştırma içindir fakat yine de CANLI ortamda önce FULL BACKUP alın.
  - Çalıştırmadan önce doğru veritabanında olduğunuzu doğrulayın.

  Önerilen çalışma sırası:
  1) FULL BACKUP
  2) Bu script
  3) Uygulama deploy
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;

PRINT '== Otelturizm migration apply başladı ==';
PRINT 'DB: ' + DB_NAME();

--------------------------------------------------------------------------------
-- 0) ZORUNLU: migration takip tablosu
--------------------------------------------------------------------------------
-- Projede zaten mevcut: dbo.schema_migrations (000_current_schema_by_table/097_table_schema_migrations.sql)
IF OBJECT_ID(N'dbo.schema_migrations', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[schema_migrations]
    (
        [script_name] nvarchar(255) NOT NULL,
        [checksum] nchar(64) NOT NULL,
        [applied_at] datetime2(0) CONSTRAINT [DF_schema_migrations_applied_at] DEFAULT (sysutcdatetime()) NOT NULL,
        CONSTRAINT [PK_schema_migrations] PRIMARY KEY CLUSTERED ([script_name] ASC)
    );
END

--------------------------------------------------------------------------------
-- Helper: script uygula + kaydet (checksum opsiyonel; dosyadan hesaplanmıyor)
--------------------------------------------------------------------------------
DECLARE @script_name nvarchar(255);
DECLARE @checksum nchar(64);

-- Her blokta:
-- IF NOT EXISTS (SELECT 1 FROM dbo.schema_migrations WHERE script_name = @script_name) BEGIN ... EXEC ... INSERT ... END

--------------------------------------------------------------------------------
-- 20260504_admin_eposta_yonlendirme.sql
--------------------------------------------------------------------------------
SET @script_name = N'20260504_admin_eposta_yonlendirme.sql';
SET @checksum = REPLICATE(N'0', 64);

IF NOT EXISTS (SELECT 1 FROM dbo.schema_migrations WHERE script_name = @script_name)
BEGIN
    PRINT 'Uygulaniyor: ' + @script_name;
    BEGIN TRY
        BEGIN TRAN;

        -- İçerik: tablo + seed + bildirim şablonu (idempotent)
        IF OBJECT_ID(N'dbo.admin_eposta_yonlendirme', N'U') IS NULL
        BEGIN
            CREATE TABLE dbo.admin_eposta_yonlendirme
            (
                id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_admin_eposta_yonlendirme PRIMARY KEY,
                olay_kodu NVARCHAR(64) NOT NULL CONSTRAINT UQ_admin_eposta_yonlendirme_kod UNIQUE,
                baslik NVARCHAR(200) NOT NULL,
                aciklama NVARCHAR(500) NULL,
                hedef_epostalar NVARCHAR(2000) NOT NULL CONSTRAINT DF_admin_eposta_hedef DEFAULT (N''),
                aktif_mi BIT NOT NULL CONSTRAINT DF_admin_eposta_aktif DEFAULT (1),
                guncellenme_utc DATETIME2(0) NOT NULL CONSTRAINT DF_admin_eposta_guncelle DEFAULT (SYSUTCDATETIME())
            );
            CREATE INDEX IX_admin_eposta_aktif ON dbo.admin_eposta_yonlendirme(aktif_mi, olay_kodu);
        END;

        DECLARE @def NVARCHAR(320) = N'irmhro0@gmail.com';
        MERGE dbo.admin_eposta_yonlendirme AS t
        USING (VALUES
            (N'partner_kayit',           N'Partner kaydı (web)',                    N'Yeni partner/taslak otel başvurusu tamamlandığında.', @def),
            (N'firma_kayit',            N'Firma başvurusu (web)',                  N'Yeni kurumsal firma başvurusu alındığında.', @def),
            (N'kullanici_kayit',        N'Üye kaydı (bireysel)',                   N'Yeni son kullanıcı hesabı oluşturulduğunda (isteğe bağlı).', @def),
            (N'rezervasyon_yonetim',   N'Rezervasyon — yönetim özeti',             N'Rezervasyon oluşturma / kritik durum (şablona bağlanır).', @def),
            (N'odeme_uyari',            N'Ödeme / tahsilat uyarıları',             N'Ödeme gecikmesi veya başarısız işlem bildirimi.', @def),
            (N'sikayet',                N'Şikayet / uyuşmazlık',                   N'Müşteri veya tesis şikayeti kaydı.', @def),
            (N'gelistirme_talebi',      N'Geliştirme / talep',                     N'Ürün geliştirme veya iş talebi (panel).', @def),
            (N'destek_talebi',          N'Destek talebi',                          N'Destek kanalından gelen talepler.', @def),
            (N'bildirim_kritik',        N'Kritik sistem bildirimi',                N'Sistem sağlığı, kuyruk veya güvenlik uyarıları.', @def),
            (N'firma_limit',            N'Firma limit / harcama',                  N'Kurumsal harcama limitine yaklaşım veya aşım.', @def),
            (N'partner_evrak',          N'Partner evrak / uyumluluk',              N'Partner evrak yükleme veya uyumluluk hatırlatması.', @def),
            (N'kvkk_basvuru',           N'KVKK başvurusu',                         N'Kişisel veri başvurusu veya silme talebi.', @def)
        ) AS s (olay_kodu, baslik, aciklama, hedef_epostalar)
        ON t.olay_kodu = s.olay_kodu
        WHEN MATCHED THEN UPDATE SET baslik = s.baslik, aciklama = s.aciklama
        WHEN NOT MATCHED THEN INSERT (olay_kodu, baslik, aciklama, hedef_epostalar, aktif_mi, guncellenme_utc)
        VALUES (s.olay_kodu, s.baslik, s.aciklama, s.hedef_epostalar, 1, SYSUTCDATETIME());

        IF OBJECT_ID(N'dbo.bildirim_sablonlari', N'U') IS NOT NULL
        BEGIN
            IF NOT EXISTS (SELECT 1 FROM dbo.bildirim_sablonlari WHERE sablon_kodu = N'admin_routing_notice' AND tur = N'E-posta' AND dil = N'tr')
            BEGIN
                INSERT INTO dbo.bildirim_sablonlari
                (sablon_kodu, sablon_adi, tur, dil, konu, baslik, icerik, degiskenler, aktif_mi, olusturulma_tarihi)
                VALUES
                (
                    N'admin_routing_notice',
                    N'Admin olay bildirimi',
                    N'E-posta',
                    N'tr',
                    N'{{email_subject}}',
                    N'Admin bildirimi',
                    N'Views/Email/tr/Admin Routing Bildirimi.cshtml',
                    N'email_subject,badge,title,intro,detail_html,primary_url,primary_label,event_code,occurred_at',
                    1,
                    SYSUTCDATETIME()
                );
            END
            ELSE
            BEGIN
                UPDATE dbo.bildirim_sablonlari
                SET konu = N'{{email_subject}}',
                    baslik = N'Admin bildirimi',
                    icerik = N'Views/Email/tr/Admin Routing Bildirimi.cshtml',
                    aktif_mi = 1
                WHERE sablon_kodu = N'admin_routing_notice' AND tur = N'E-posta' AND dil = N'tr';
            END
        END

        INSERT INTO dbo.schema_migrations(script_name, checksum) VALUES (@script_name, @checksum);
        COMMIT;
        PRINT 'OK: ' + @script_name;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK;
        DECLARE @msg nvarchar(4000) = ERROR_MESSAGE();
        PRINT 'HATA: ' + @script_name + N' -> ' + @msg;
        THROW;
    END CATCH
END
ELSE
BEGIN
    PRINT 'Atlandi (zaten uygulanmis): ' + @script_name;
END

--------------------------------------------------------------------------------
-- NOT:
-- Bu dosya şu an sadece en kritik yeni migration'ı (admin e-posta yönlendirme) güvenli uygular.
-- Kalan scriptleri de aynı pattern ile buraya sırayla ekleyebilirim; bunun için canlı DB sürümünüz
-- ile hangi scriptlerin eksik olduğunu tespit etmem gerekir (schema_migrations tablonuzdaki kayıtlar).
--------------------------------------------------------------------------------

PRINT '== Otelturizm migration apply bitti ==';

