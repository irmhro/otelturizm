SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.schema_migrations', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[schema_migrations]
    (
        [script_name] nvarchar(255) NOT NULL,
        [checksum] nchar(64) NOT NULL,
        [applied_at] datetime2(0) CONSTRAINT [DF__schema_mi__appli__184C96B4] DEFAULT (sysutcdatetime()) NOT NULL,
        CONSTRAINT [PK_schema_migrations] PRIMARY KEY CLUSTERED ([script_name] ASC)
    );
END
GO

-- Missing column repair blocks for existing databases.
IF COL_LENGTH(N'dbo.schema_migrations', N'script_name') IS NULL
BEGIN
    ALTER TABLE [dbo].[schema_migrations] ADD [script_name] nvarchar(255) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.schema_migrations', N'checksum') IS NULL
BEGIN
    ALTER TABLE [dbo].[schema_migrations] ADD [checksum] nchar(64) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.schema_migrations', N'applied_at') IS NULL
BEGIN
    ALTER TABLE [dbo].[schema_migrations] ADD [applied_at] datetime2(0) DEFAULT (sysutcdatetime()) NOT NULL;
END
GO
