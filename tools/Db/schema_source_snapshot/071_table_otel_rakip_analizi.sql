SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.otel_rakip_analizi', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[otel_rakip_analizi]
    (
        [id] bigint IDENTITY(1,1) NOT NULL,
        [otel_id] bigint NOT NULL,
        [rakip_otel_adi] nvarchar(200) NOT NULL,
        [rakip_sehir] nvarchar(100) NULL,
        [rakip_ilce] nvarchar(100) NULL,
        [analiz_tarihi] date NOT NULL,
        [ortalama_gecelik_fiyat] decimal(10,2) NULL,
        [tahmini_doluluk_orani] decimal(5,2) NULL,
        [kaynak_url] nvarchar(500) NULL,
        [notlar] nvarchar(max) NULL,
        CONSTRAINT [PK_otel_rakip_analizi] PRIMARY KEY CLUSTERED ([id] ASC)
    );
END
GO

-- Missing column repair blocks for existing databases.
IF COL_LENGTH(N'dbo.otel_rakip_analizi', N'id') IS NULL
BEGIN
    ALTER TABLE [dbo].[otel_rakip_analizi] ADD [id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.otel_rakip_analizi', N'otel_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[otel_rakip_analizi] ADD [otel_id] bigint NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.otel_rakip_analizi', N'rakip_otel_adi') IS NULL
BEGIN
    ALTER TABLE [dbo].[otel_rakip_analizi] ADD [rakip_otel_adi] nvarchar(200) NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.otel_rakip_analizi', N'rakip_sehir') IS NULL
BEGIN
    ALTER TABLE [dbo].[otel_rakip_analizi] ADD [rakip_sehir] nvarchar(100) NULL;
END
GO
IF COL_LENGTH(N'dbo.otel_rakip_analizi', N'rakip_ilce') IS NULL
BEGIN
    ALTER TABLE [dbo].[otel_rakip_analizi] ADD [rakip_ilce] nvarchar(100) NULL;
END
GO
IF COL_LENGTH(N'dbo.otel_rakip_analizi', N'analiz_tarihi') IS NULL
BEGIN
    ALTER TABLE [dbo].[otel_rakip_analizi] ADD [analiz_tarihi] date NULL -- originally NOT NULL; nullable repair avoids failing on populated tables;
END
GO
IF COL_LENGTH(N'dbo.otel_rakip_analizi', N'ortalama_gecelik_fiyat') IS NULL
BEGIN
    ALTER TABLE [dbo].[otel_rakip_analizi] ADD [ortalama_gecelik_fiyat] decimal(10,2) NULL;
END
GO
IF COL_LENGTH(N'dbo.otel_rakip_analizi', N'tahmini_doluluk_orani') IS NULL
BEGIN
    ALTER TABLE [dbo].[otel_rakip_analizi] ADD [tahmini_doluluk_orani] decimal(5,2) NULL;
END
GO
IF COL_LENGTH(N'dbo.otel_rakip_analizi', N'kaynak_url') IS NULL
BEGIN
    ALTER TABLE [dbo].[otel_rakip_analizi] ADD [kaynak_url] nvarchar(500) NULL;
END
GO
IF COL_LENGTH(N'dbo.otel_rakip_analizi', N'notlar') IS NULL
BEGIN
    ALTER TABLE [dbo].[otel_rakip_analizi] ADD [notlar] nvarchar(max) NULL;
END
GO
