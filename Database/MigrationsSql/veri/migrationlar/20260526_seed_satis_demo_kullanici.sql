-- Idempotent (T398): yerel satış paneli demo kullanıcısı
-- Giriş: http://127.0.0.1:5103/kullanici-giris  |  ReturnUrl=/panel/satis
-- E-posta: satis@demo.otelturizm.local  |  Şifre: Demo123!
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

IF OBJECT_ID(N'dbo.KULLANICILAR', N'U') IS NULL RETURN;

DECLARE @DemoPasswordHash nvarchar(64) = LOWER(CONVERT(VARCHAR(64), HASHBYTES('SHA2_256', CONVERT(nvarchar(max), N'Demo123!')), 2));
DECLARE @SalesUserId bigint;

IF NOT EXISTS (SELECT 1 FROM [dbo].[KULLANICILAR] WHERE [EPOSTA] = N'satis@demo.otelturizm.local')
BEGIN
    INSERT INTO [dbo].[KULLANICILAR]
        ([AD_SOYAD], [EPOSTA], [TELEFON], [SIFRE], [ROL], [HESAP_DURUMU], [KAYIT_KAYNAGI], [OLUSTURULMA_TARIHI])
    VALUES
        (N'Orkestra Demo Satış', N'satis@demo.otelturizm.local', N'5000000300', @DemoPasswordHash, N'sales_admin', 1, N'OrkestraSeed', SYSUTCDATETIME());
    SET @SalesUserId = SCOPE_IDENTITY();
END
ELSE
    SELECT @SalesUserId = [ID] FROM [dbo].[KULLANICILAR] WHERE [EPOSTA] = N'satis@demo.otelturizm.local';

IF @SalesUserId IS NULL RETURN;

UPDATE [dbo].[KULLANICILAR]
SET [ROL] = N'sales_admin',
    [HESAP_DURUMU] = 1,
    [SIFRE] = @DemoPasswordHash
WHERE [ID] = @SalesUserId;

IF COL_LENGTH(N'dbo.KULLANICILAR', N'EPOSTA_DOGRULAMA_TARIHI') IS NOT NULL
BEGIN
    UPDATE [dbo].[KULLANICILAR]
    SET [EPOSTA_DOGRULAMA_TARIHI] = COALESCE([EPOSTA_DOGRULAMA_TARIHI], SYSUTCDATETIME())
    WHERE [ID] = @SalesUserId AND [EPOSTA_DOGRULAMA_TARIHI] IS NULL;
END;

IF COL_LENGTH(N'dbo.KULLANICILAR', N'SATIS_EKIBI') IS NOT NULL
BEGIN
    UPDATE [dbo].[KULLANICILAR]
    SET [SATIS_EKIBI] = COALESCE(NULLIF(LTRIM(RTRIM([SATIS_EKIBI])), N''), N'Demo')
    WHERE [ID] = @SalesUserId;
END
ELSE IF COL_LENGTH(N'dbo.KULLANICILAR', N'satis_ekibi') IS NOT NULL
BEGIN
    UPDATE [dbo].[KULLANICILAR]
    SET [satis_ekibi] = COALESCE(NULLIF(LTRIM(RTRIM([satis_ekibi])), N''), N'Demo')
    WHERE [ID] = @SalesUserId;
END;
GO
