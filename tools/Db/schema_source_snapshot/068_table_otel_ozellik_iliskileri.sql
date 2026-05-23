SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.otel_ozellik_iliskileri', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[otel_ozellik_iliskileri]
    (
        [otel_id] bigint NOT NULL,
        [ozellik_id] int NOT NULL,
        [ek_ucret] decimal(10,2) NULL,
        [aciklama] nvarchar(255) NULL,
        [kategori_id] int NULL,
        CONSTRAINT [PK_otel_ozellik_iliskileri] PRIMARY KEY CLUSTERED ([otel_id] ASC, [ozellik_id] ASC)
    );
END
GO

-- Missing column repair blocks for existing databases.
IF COL_LENGTH(N'dbo.otel_ozellik_iliskileri', N'otel_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[otel_ozellik_iliskileri] ADD [otel_id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.otel_ozellik_iliskileri', N'ozellik_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[otel_ozellik_iliskileri] ADD [ozellik_id] int NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.otel_ozellik_iliskileri', N'ek_ucret') IS NULL
BEGIN
    ALTER TABLE [dbo].[otel_ozellik_iliskileri] ADD [ek_ucret] decimal(10,2) NULL;
END
GO
IF COL_LENGTH(N'dbo.otel_ozellik_iliskileri', N'aciklama') IS NULL
BEGIN
    ALTER TABLE [dbo].[otel_ozellik_iliskileri] ADD [aciklama] nvarchar(255) NULL;
END
GO
IF COL_LENGTH(N'dbo.otel_ozellik_iliskileri', N'kategori_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[otel_ozellik_iliskileri] ADD [kategori_id] int NULL;
END
GO
