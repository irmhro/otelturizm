SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.sysdiagrams', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[sysdiagrams]
    (
        [name] sysname NOT NULL,
        [principal_id] int NOT NULL,
        [diagram_id] int IDENTITY(1,1) NOT NULL,
        [version] int NULL,
        [definition] varbinary(max) NULL,
        CONSTRAINT [PK__sysdiagr__C2B05B61294AD40F] PRIMARY KEY CLUSTERED ([diagram_id] ASC)
    );
END
GO

-- Missing column repair blocks for existing databases.
IF COL_LENGTH(N'dbo.sysdiagrams', N'name') IS NULL
BEGIN
    ALTER TABLE [dbo].[sysdiagrams] ADD [name] sysname NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.sysdiagrams', N'principal_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[sysdiagrams] ADD [principal_id] int NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.sysdiagrams', N'diagram_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[sysdiagrams] ADD [diagram_id] int NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.sysdiagrams', N'version') IS NULL
BEGIN
    ALTER TABLE [dbo].[sysdiagrams] ADD [version] int NULL;
END
GO
IF COL_LENGTH(N'dbo.sysdiagrams', N'definition') IS NULL
BEGIN
    ALTER TABLE [dbo].[sysdiagrams] ADD [definition] varbinary(max) NULL;
END
GO
