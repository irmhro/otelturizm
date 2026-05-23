-- Tablo: dbo.ADMIN_ROL_YETKILER
IF OBJECT_ID(N'dbo.ADMIN_ROL_YETKILER', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ADMIN_ROL_YETKILER] (
        [ROL_CODE] nvarchar(64) NOT NULL,
        [YETKI_CODE] nvarchar(64) NOT NULL,
        [ACTIVE] bit NOT NULL CONSTRAINT [DF_admin_role_permissions_active] DEFAULT ((1)),
        [CREATED_UTC] datetime2(0) NOT NULL CONSTRAINT [DF_admin_role_permissions_created] DEFAULT (sysutcdatetime()),
        CONSTRAINT [PK_ADMIN_ROL_YETKILER] PRIMARY KEY CLUSTERED ([ROL_CODE], [YETKI_CODE] ASC)
    );
END
