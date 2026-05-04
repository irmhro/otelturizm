SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.otel_istatistikleri', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[otel_istatistikleri]
    (
        [id] bigint IDENTITY(1,1) NOT NULL,
        [otel_id] bigint NOT NULL,
        [istatistik_tarihi] date NOT NULL,
        [rezervasyon_sayisi] int CONSTRAINT [DF__otel_ista__rezer__02925FBF] DEFAULT ((0)) NULL,
        [iptal_sayisi] int CONSTRAINT [DF__otel_ista__iptal__038683F8] DEFAULT ((0)) NULL,
        [doluluk_orani] decimal(5,2) CONSTRAINT [DF__otel_ista__dolul__047AA831] DEFAULT ((0.00)) NULL,
        [brut_gelir] decimal(12,2) CONSTRAINT [DF__otel_ista__brut___056ECC6A] DEFAULT ((0.00)) NULL,
        [net_gelir] decimal(12,2) CONSTRAINT [DF__otel_ista__net_g__0662F0A3] DEFAULT ((0.00)) NULL,
        [ortalama_puan] decimal(3,2) CONSTRAINT [DF__otel_ista__ortal__075714DC] DEFAULT ((0.00)) NULL,
        [yorum_sayisi] int CONSTRAINT [DF__otel_ista__yorum__084B3915] DEFAULT ((0)) NULL,
        CONSTRAINT [PK_otel_istatistikleri] PRIMARY KEY CLUSTERED ([id] ASC)
    );
END
GO

-- Missing column repair blocks for existing databases.
IF COL_LENGTH(N'dbo.otel_istatistikleri', N'id') IS NULL
BEGIN
    ALTER TABLE [dbo].[otel_istatistikleri] ADD [id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.otel_istatistikleri', N'otel_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[otel_istatistikleri] ADD [otel_id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.otel_istatistikleri', N'istatistik_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[otel_istatistikleri] ADD [istatistik_tarihi] date NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.otel_istatistikleri', N'rezervasyon_sayisi') IS NULL
BEGIN
    ALTER TABLE [dbo].[otel_istatistikleri] ADD [rezervasyon_sayisi] int DEFAULT ((0)) NULL;
END
GO
IF COL_LENGTH(N'dbo.otel_istatistikleri', N'iptal_sayisi') IS NULL
BEGIN
    ALTER TABLE [dbo].[otel_istatistikleri] ADD [iptal_sayisi] int DEFAULT ((0)) NULL;
END
GO
IF COL_LENGTH(N'dbo.otel_istatistikleri', N'doluluk_orani') IS NULL
BEGIN
    ALTER TABLE [dbo].[otel_istatistikleri] ADD [doluluk_orani] decimal(5,2) DEFAULT ((0.00)) NULL;
END
GO
IF COL_LENGTH(N'dbo.otel_istatistikleri', N'brut_gelir') IS NULL
BEGIN
    ALTER TABLE [dbo].[otel_istatistikleri] ADD [brut_gelir] decimal(12,2) DEFAULT ((0.00)) NULL;
END
GO
IF COL_LENGTH(N'dbo.otel_istatistikleri', N'net_gelir') IS NULL
BEGIN
    ALTER TABLE [dbo].[otel_istatistikleri] ADD [net_gelir] decimal(12,2) DEFAULT ((0.00)) NULL;
END
GO
IF COL_LENGTH(N'dbo.otel_istatistikleri', N'ortalama_puan') IS NULL
BEGIN
    ALTER TABLE [dbo].[otel_istatistikleri] ADD [ortalama_puan] decimal(3,2) DEFAULT ((0.00)) NULL;
END
GO
IF COL_LENGTH(N'dbo.otel_istatistikleri', N'yorum_sayisi') IS NULL
BEGIN
    ALTER TABLE [dbo].[otel_istatistikleri] ADD [yorum_sayisi] int DEFAULT ((0)) NULL;
END
GO
