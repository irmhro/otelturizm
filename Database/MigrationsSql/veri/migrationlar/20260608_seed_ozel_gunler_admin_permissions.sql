-- Özel günler admin sayfası RBAC
IF NOT EXISTS (SELECT 1 FROM [dbo].[ADMIN_YETKILER] WHERE [YETKI_CODE] = N'admin.ozel_gunler')
BEGIN
    INSERT INTO [dbo].[ADMIN_YETKILER] ([YETKI_CODE], [YETKI_NAME], [GROUP_CODE], [DESCRIPTION], [ACTIVE])
    VALUES (N'admin.ozel_gunler', N'Özel Günler Yönetimi', N'content', N'OZEL_GUNLER tablosu CRUD', 1);
END
GO

INSERT INTO [dbo].[ADMIN_ROL_YETKILER] ([ROL_CODE], [YETKI_CODE], [ACTIVE])
SELECT N'platform_admin_full', N'admin.ozel_gunler', 1
WHERE EXISTS (SELECT 1 FROM [dbo].[ADMIN_YETKILER] WHERE [YETKI_CODE] = N'admin.ozel_gunler' AND [ACTIVE] = 1)
  AND NOT EXISTS (
      SELECT 1 FROM [dbo].[ADMIN_ROL_YETKILER]
      WHERE [ROL_CODE] = N'platform_admin_full' AND [YETKI_CODE] = N'admin.ozel_gunler'
  );
GO

PRINT N'admin.ozel_gunler yetkisi seed tamamlandi.';
