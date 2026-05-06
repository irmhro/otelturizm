/*
  admin.listing_subscriptions izni — Otel Liste Abonelikleri menü/endpoint ile uyum.
  Idempotent MERGE.
*/
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.admin_permissions', N'U') IS NOT NULL
BEGIN
    MERGE dbo.admin_permissions AS t
    USING (VALUES
        (N'admin.listing_subscriptions', N'Otel Liste Abonelikleri', N'ops', N'Liste abonelik onay ve görüntüleme')
    ) AS s(permission_code, permission_name, group_code, description)
    ON t.permission_code = s.permission_code
    WHEN MATCHED THEN UPDATE SET permission_name = s.permission_name, group_code = s.group_code, description = s.description, active = 1
    WHEN NOT MATCHED THEN INSERT(permission_code, permission_name, group_code, description, active)
        VALUES(s.permission_code, s.permission_name, s.group_code, s.description, 1);
END
GO

-- superadmin zaten tüm izinlerde; finance ve ops için ticari operasyonla uyumlu atama
IF OBJECT_ID(N'dbo.admin_role_permissions', N'U') IS NOT NULL
BEGIN
    INSERT INTO dbo.admin_role_permissions(role_code, permission_code, active)
    SELECT r.role_code, N'admin.listing_subscriptions', 1
    FROM (VALUES (N'superadmin'), (N'finance'), (N'ops')) AS r(role_code)
    WHERE NOT EXISTS (
        SELECT 1 FROM dbo.admin_role_permissions rp
        WHERE rp.role_code = r.role_code AND rp.permission_code = N'admin.listing_subscriptions'
    );
END
GO
