-- Idempotent (T330): yerel admin panel oturumu + RBAC test kullanıcısı
-- Önkoşul: 20260522_seed_admin_yetkiler.sql (platform_admin_full)
-- Giriş: http://127.0.0.1:5103/admin-giris
-- E-posta: ork-demo-admin@otelturizm.local  |  Şifre: Demo123!
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

IF OBJECT_ID(N'dbo.KULLANICILAR', N'U') IS NULL RETURN;
IF OBJECT_ID(N'dbo.ADMIN_KULLANICI_ROLLER', N'U') IS NULL RETURN;

IF NOT EXISTS (SELECT 1 FROM [dbo].[ADMIN_ROLLER] WHERE [ROL_CODE] = N'platform_admin_full')
BEGIN
    RAISERROR(N'platform_admin_full rol yok; once 20260522_seed_admin_yetkiler.sql uygulayin.', 16, 1);
    RETURN;
END;

DECLARE @DemoPasswordHash nvarchar(64) = LOWER(CONVERT(VARCHAR(64), HASHBYTES('SHA2_256', CONVERT(nvarchar(max), N'Demo123!')), 2));
DECLARE @AdminUserId bigint;

IF NOT EXISTS (SELECT 1 FROM [dbo].[KULLANICILAR] WHERE [EPOSTA] = N'ork-demo-admin@otelturizm.local')
BEGIN
    INSERT INTO [dbo].[KULLANICILAR]
        ([AD_SOYAD], [EPOSTA], [TELEFON], [SIFRE], [ROL], [HESAP_DURUMU], [KAYIT_KAYNAGI], [OLUSTURULMA_TARIHI])
    VALUES
        (N'Orkestra Demo Admin', N'ork-demo-admin@otelturizm.local', N'5000000100', @DemoPasswordHash, N'admin', 1, N'OrkestraSeed', SYSUTCDATETIME());
    SET @AdminUserId = SCOPE_IDENTITY();
END
ELSE
    SELECT @AdminUserId = [ID] FROM [dbo].[KULLANICILAR] WHERE [EPOSTA] = N'ork-demo-admin@otelturizm.local';

IF @AdminUserId IS NULL RETURN;

IF NOT EXISTS (
    SELECT 1 FROM [dbo].[ADMIN_KULLANICI_ROLLER]
    WHERE [ADMIN_KULLANICI_ID] = @AdminUserId AND [ROL_CODE] = N'platform_admin_full'
)
BEGIN
    INSERT INTO [dbo].[ADMIN_KULLANICI_ROLLER] ([ADMIN_KULLANICI_ID], [ROL_CODE], [ACTIVE])
    VALUES (@AdminUserId, N'platform_admin_full', 1);
END;
GO
