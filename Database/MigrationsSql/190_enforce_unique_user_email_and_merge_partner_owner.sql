SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;

IF OBJECT_ID(N'dbo.users', N'U') IS NULL
BEGIN
    RETURN;
END;

BEGIN TRY
    BEGIN TRANSACTION;

    -- Eagle Palace partner hesabını tek kullanıcıya indir.
    IF EXISTS (SELECT 1 FROM dbo.users WHERE id = 101 AND eposta = N'kurumsal@otelturizm.com')
       AND EXISTS (SELECT 1 FROM dbo.users WHERE id = 117 AND eposta = N'kurumsal@otelturizm.com')
    BEGIN
        -- Ana hesap için istenen şifre: 1585
        UPDATE dbo.users
        SET sifre = LOWER(CONVERT(VARCHAR(64), HASHBYTES('SHA2_256', CONVERT(nvarchar(max), N'1585')), 2)),
            rol = COALESCE(NULLIF(rol, N''), N'partner_owner'),
            guncellenme_tarihi = SYSUTCDATETIME()
        WHERE id = 101;

        IF OBJECT_ID(N'dbo.oteller', N'U') IS NOT NULL
        BEGIN
            UPDATE dbo.oteller
            SET user_id = 101,
                eposta = N'kurumsal@otelturizm.com',
                guncellenme_tarihi = SYSUTCDATETIME()
            WHERE user_id = 117
               OR id = 59;
        END;

        IF OBJECT_ID(N'dbo.otel_kullanici_sahiplikleri', N'U') IS NOT NULL
        BEGIN
            UPDATE dbo.otel_kullanici_sahiplikleri
            SET user_id = 101,
                guncellenme_tarihi = SYSUTCDATETIME()
            WHERE user_id = 117;

            ;WITH duplicates AS (
                SELECT id,
                       ROW_NUMBER() OVER (
                           PARTITION BY otel_id, user_id, partner_id, rol, ana_sorumlu_mu, aktif_mi
                           ORDER BY id
                       ) AS rn
                FROM dbo.otel_kullanici_sahiplikleri
            )
            DELETE FROM duplicates WHERE rn > 1;
        END;

        IF OBJECT_ID(N'dbo.partner_detaylari', N'U') IS NOT NULL
        BEGIN
            UPDATE dbo.partner_detaylari
            SET kullanici_id = 101,
                guncellenme_tarihi = SYSUTCDATETIME(),
                yetkili_eposta = N'kurumsal@otelturizm.com',
                yetkili_ad_soyad = COALESCE(NULLIF(yetkili_ad_soyad, N''), N'Kurumsal Kullanici')
            WHERE kullanici_id = 117;
        END;

        IF OBJECT_ID(N'dbo.users_partner', N'U') IS NOT NULL
        BEGIN
            UPDATE dbo.users_partner
            SET user_id = 101
            WHERE user_id = 117;
        END;

        DELETE FROM dbo.users WHERE id = 117;
    END;

    -- E-posta tekrarını DB seviyesinde engelle.
    UPDATE dbo.users
    SET eposta = LOWER(LTRIM(RTRIM(eposta)))
    WHERE eposta IS NOT NULL;

    IF EXISTS (
        SELECT 1
        FROM sys.indexes
        WHERE name = N'UX_users_eposta_normalize'
          AND object_id = OBJECT_ID(N'dbo.users')
    )
    BEGIN
        DROP INDEX UX_users_eposta_normalize ON dbo.users;
    END;

    IF NOT EXISTS (
        SELECT 1
        FROM sys.indexes
        WHERE name = N'UX_users_eposta'
          AND object_id = OBJECT_ID(N'dbo.users')
    )
    BEGIN
        CREATE UNIQUE INDEX UX_users_eposta
            ON dbo.users(eposta)
            WHERE eposta IS NOT NULL AND eposta <> N'';
    END;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    THROW;
END CATCH;
