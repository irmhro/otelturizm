-- Puan yönetimi admin sayfası RBAC
IF NOT EXISTS (SELECT 1 FROM [dbo].[ADMIN_YETKILER] WHERE [YETKI_CODE] = N'admin.puan_yonetimi')
BEGIN
    INSERT INTO [dbo].[ADMIN_YETKILER] ([YETKI_CODE], [YETKI_ADI], [MODUL], [ACIKLAMA], [ACTIVE])
    VALUES (N'admin.puan_yonetimi', N'Puan Yönetimi', N'crm', N'PUAN_AYAR / PUAN_KULLANICI yönetimi', 1);
END
GO

INSERT INTO [dbo].[ADMIN_ROL_YETKILER] ([ROL_CODE], [YETKI_CODE], [ACTIVE])
SELECT N'platform_admin_full', N'admin.puan_yonetimi', 1
WHERE EXISTS (SELECT 1 FROM [dbo].[ADMIN_YETKILER] WHERE [YETKI_CODE] = N'admin.puan_yonetimi' AND [ACTIVE] = 1)
  AND NOT EXISTS (
      SELECT 1 FROM [dbo].[ADMIN_ROL_YETKILER]
      WHERE [ROL_CODE] = N'platform_admin_full' AND [YETKI_CODE] = N'admin.puan_yonetimi'
  );
GO

PRINT N'admin.puan_yonetimi yetkisi seed tamamlandı.';
GO
