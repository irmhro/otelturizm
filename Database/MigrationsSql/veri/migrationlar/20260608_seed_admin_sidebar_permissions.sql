/* Admin sidebar v2 — RBAC yetkileri (UTF-8) */
SET NOCOUNT ON;
GO

IF OBJECT_ID(N'dbo.ADMIN_YETKILER', N'U') IS NULL RETURN;

;WITH yetkiler AS (
    SELECT * FROM (VALUES
        (N'admin.locations', N'Konum Yönetimi', N'locations'),
        (N'admin.roles', N'Rol Yönetimi', N'system'),
        (N'admin.companies', N'Firma Yönetimi', N'users'),
        (N'admin.platform_stats', N'Veritabanı İstatistikleri', N'system'),
        (N'admin.help_center', N'Yardım Merkezi Tabloları', N'content'),
        (N'admin.sss_tables', N'SSS Tablo Yönetimi', N'content')
    ) AS v([YETKI_CODE], [YETKI_NAME], [GROUP_CODE])
)
INSERT INTO [dbo].[ADMIN_YETKILER] ([YETKI_CODE], [YETKI_NAME], [GROUP_CODE], [DESCRIPTION], [ACTIVE])
SELECT y.[YETKI_CODE], y.[YETKI_NAME], y.[GROUP_CODE], NULL, 1
FROM yetkiler y
WHERE NOT EXISTS (SELECT 1 FROM [dbo].[ADMIN_YETKILER] p WHERE p.[YETKI_CODE] = y.[YETKI_CODE]);
GO

IF OBJECT_ID(N'dbo.ADMIN_ROL_YETKILER', N'U') IS NULL RETURN;

INSERT INTO [dbo].[ADMIN_ROL_YETKILER] ([ROL_CODE], [YETKI_CODE], [ACTIVE])
SELECT N'platform_admin_full', p.[YETKI_CODE], 1
FROM [dbo].[ADMIN_YETKILER] p
WHERE p.[YETKI_CODE] IN (
    N'admin.locations',
    N'admin.roles',
    N'admin.companies',
    N'admin.platform_stats',
    N'admin.help_center',
    N'admin.sss_tables'
)
  AND p.[ACTIVE] = 1
  AND NOT EXISTS (
      SELECT 1 FROM [dbo].[ADMIN_ROL_YETKILER] rp
      WHERE rp.[ROL_CODE] = N'platform_admin_full' AND rp.[YETKI_CODE] = p.[YETKI_CODE]
  );
GO
