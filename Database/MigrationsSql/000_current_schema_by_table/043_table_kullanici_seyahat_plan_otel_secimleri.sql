SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.kullanici_seyahat_plan_otel_secimleri', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[kullanici_seyahat_plan_otel_secimleri]
    (
        [id] bigint IDENTITY(1,1) NOT NULL,
        [plan_id] bigint NOT NULL,
        [otel_id] bigint NOT NULL,
        [ekleyen_kullanici_id] bigint NOT NULL,
        [oy_puani] int CONSTRAINT [DF__kullanici__oy_pu__2BC97F7C] DEFAULT ((0)) NOT NULL,
        [notlar] nvarchar(255) NULL,
        [olusturulma_tarihi] datetime2(0) CONSTRAINT [DF__kullanici__olust__2CBDA3B5] DEFAULT (sysutcdatetime()) NOT NULL,
        CONSTRAINT [PK_kullanici_seyahat_plan_otel_secimleri] PRIMARY KEY CLUSTERED ([id] ASC)
    );
END
GO

-- Missing column repair blocks for existing databases.
IF COL_LENGTH(N'dbo.kullanici_seyahat_plan_otel_secimleri', N'id') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_seyahat_plan_otel_secimleri] ADD [id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.kullanici_seyahat_plan_otel_secimleri', N'plan_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_seyahat_plan_otel_secimleri] ADD [plan_id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.kullanici_seyahat_plan_otel_secimleri', N'otel_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_seyahat_plan_otel_secimleri] ADD [otel_id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.kullanici_seyahat_plan_otel_secimleri', N'ekleyen_kullanici_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_seyahat_plan_otel_secimleri] ADD [ekleyen_kullanici_id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.kullanici_seyahat_plan_otel_secimleri', N'oy_puani') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_seyahat_plan_otel_secimleri] ADD [oy_puani] int DEFAULT ((0)) NOT NULL;
END
GO
IF COL_LENGTH(N'dbo.kullanici_seyahat_plan_otel_secimleri', N'notlar') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_seyahat_plan_otel_secimleri] ADD [notlar] nvarchar(255) NULL;
END
GO
IF COL_LENGTH(N'dbo.kullanici_seyahat_plan_otel_secimleri', N'olusturulma_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_seyahat_plan_otel_secimleri] ADD [olusturulma_tarihi] datetime2(0) DEFAULT (sysutcdatetime()) NOT NULL;
END
GO
