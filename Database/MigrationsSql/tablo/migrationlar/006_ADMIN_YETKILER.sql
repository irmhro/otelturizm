-- Tablo: dbo.ADMIN_YETKILER
IF OBJECT_ID(N'dbo.ADMIN_YETKILER', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ADMIN_YETKILER] (
        [YETKI_CODE] nvarchar(64) NOT NULL,
        [YETKI_NAME] nvarchar(128) NOT NULL,
        [GROUP_CODE] nvarchar(64) NOT NULL CONSTRAINT [DF_admin_permissions_group] DEFAULT (N'general'),
        [DESCRIPTION] nvarchar(256) NULL,
        [ACTIVE] bit NOT NULL CONSTRAINT [DF_admin_permissions_active] DEFAULT ((1)),
        [CREATED_UTC] datetime2(0) NOT NULL CONSTRAINT [DF_admin_permissions_created] DEFAULT (sysutcdatetime()),
        CONSTRAINT [PK_ADMIN_YETKILER] PRIMARY KEY CLUSTERED ([YETKI_CODE] ASC)
    );
END
