/* Anasayfa otel vitrin yönetimi — RBAC yetkisi (UTF-8) */
SET NOCOUNT ON;
GO

IF OBJECT_ID(N'dbo.ADMIN_YETKILER', N'U') IS NULL RETURN;

IF NOT EXISTS (SELECT 1 FROM [dbo].[ADMIN_YETKILER] WHERE [YETKI_CODE] = N'admin.homepage_hotels')
BEGIN
    INSERT INTO [dbo].[ADMIN_YETKILER] ([YETKI_CODE], [YETKI_NAME], [GROUP_CODE], [DESCRIPTION], [ACTIVE])
    VALUES (N'admin.homepage_hotels', N'Anasayfa Aktif Oteller', N'content', N'Anasayfa vitrin bölümleri ve otel seçimi', 1);
END
GO

IF OBJECT_ID(N'dbo.ADMIN_ROL_YETKILER', N'U') IS NULL RETURN;

INSERT INTO [dbo].[ADMIN_ROL_YETKILER] ([ROL_CODE], [YETKI_CODE], [ACTIVE])
SELECT N'platform_admin_full', N'admin.homepage_hotels', 1
WHERE EXISTS (SELECT 1 FROM [dbo].[ADMIN_YETKILER] WHERE [YETKI_CODE] = N'admin.homepage_hotels' AND [ACTIVE] = 1)
  AND NOT EXISTS (
      SELECT 1 FROM [dbo].[ADMIN_ROL_YETKILER]
      WHERE [ROL_CODE] = N'platform_admin_full' AND [YETKI_CODE] = N'admin.homepage_hotels'
  );
GO
