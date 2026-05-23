-- Tablo: dbo.ADMIN_ROLLER
IF OBJECT_ID(N'dbo.ADMIN_ROLLER', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ADMIN_ROLLER] (
        [ROL_CODE] nvarchar(64) NOT NULL,
        [ROL_NAME] nvarchar(128) NOT NULL,
        [DESCRIPTION] nvarchar(256) NULL,
        [ACTIVE] bit NOT NULL CONSTRAINT [DF_admin_roles_active] DEFAULT ((1)),
        [CREATED_UTC] datetime2(0) NOT NULL CONSTRAINT [DF_admin_roles_created] DEFAULT (sysutcdatetime()),
        CONSTRAINT [PK_ADMIN_ROLLER] PRIMARY KEY CLUSTERED ([ROL_CODE] ASC)
    );
END
