-- MSSQL: Review moderation helper tables (blocked words + takedown requests)
-- Safe/idempotent: creates tables if missing and adds missing columns.

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.blockyorumkelime', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.blockyorumkelime
    (
        id bigint IDENTITY(1,1) NOT NULL,
        kelime nvarchar(120) NOT NULL,
        aktif_mi bit NOT NULL CONSTRAINT DF_blockyorumkelime_aktif DEFAULT ((1)),
        aciklama nvarchar(250) NULL,
        ekleyen_admin_id bigint NULL,
        olusturulma_tarihi datetime2(0) NOT NULL CONSTRAINT DF_blockyorumkelime_olustur DEFAULT (sysutcdatetime()),
        guncellenme_tarihi datetime2(0) NULL,
        CONSTRAINT PK_blockyorumkelime PRIMARY KEY CLUSTERED (id ASC)
    );
END
GO

IF COL_LENGTH(N'dbo.blockyorumkelime', N'kelime') IS NULL
    ALTER TABLE dbo.blockyorumkelime ADD kelime nvarchar(120) NULL;
GO
IF COL_LENGTH(N'dbo.blockyorumkelime', N'aktif_mi') IS NULL
    ALTER TABLE dbo.blockyorumkelime ADD aktif_mi bit NOT NULL CONSTRAINT DF_blockyorumkelime_aktif DEFAULT ((1));
GO
IF COL_LENGTH(N'dbo.blockyorumkelime', N'aciklama') IS NULL
    ALTER TABLE dbo.blockyorumkelime ADD aciklama nvarchar(250) NULL;
GO
IF COL_LENGTH(N'dbo.blockyorumkelime', N'ekleyen_admin_id') IS NULL
    ALTER TABLE dbo.blockyorumkelime ADD ekleyen_admin_id bigint NULL;
GO
IF COL_LENGTH(N'dbo.blockyorumkelime', N'olusturulma_tarihi') IS NULL
    ALTER TABLE dbo.blockyorumkelime ADD olusturulma_tarihi datetime2(0) NULL;
GO
IF COL_LENGTH(N'dbo.blockyorumkelime', N'guncellenme_tarihi') IS NULL
    ALTER TABLE dbo.blockyorumkelime ADD guncellenme_tarihi datetime2(0) NULL;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'UX_blockyorumkelime_kelime'
      AND object_id = OBJECT_ID(N'dbo.blockyorumkelime')
)
BEGIN
    CREATE UNIQUE INDEX UX_blockyorumkelime_kelime
    ON dbo.blockyorumkelime(kelime)
    WHERE kelime IS NOT NULL;
END
GO

IF OBJECT_ID(N'dbo.yorum_kaldirma_talepleri', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.yorum_kaldirma_talepleri
    (
        id bigint IDENTITY(1,1) NOT NULL,
        yorum_id bigint NOT NULL,
        otel_id bigint NULL,
        partner_kullanici_id bigint NOT NULL,
        sebep nvarchar(800) NULL,
        durum nvarchar(40) NOT NULL CONSTRAINT DF_yorum_kaldirma_talepleri_durum DEFAULT (N'Beklemede'),
        admin_notu nvarchar(800) NULL,
        karar_veren_admin_id bigint NULL,
        karar_tarihi datetime2(0) NULL,
        olusturulma_tarihi datetime2(0) NOT NULL CONSTRAINT DF_yorum_kaldirma_talepleri_olustur DEFAULT (sysutcdatetime()),
        guncellenme_tarihi datetime2(0) NULL,
        CONSTRAINT PK_yorum_kaldirma_talepleri PRIMARY KEY CLUSTERED (id ASC)
    );
END
GO

IF COL_LENGTH(N'dbo.yorum_kaldirma_talepleri', N'yorum_id') IS NULL
    ALTER TABLE dbo.yorum_kaldirma_talepleri ADD yorum_id bigint NULL;
GO
IF COL_LENGTH(N'dbo.yorum_kaldirma_talepleri', N'otel_id') IS NULL
    ALTER TABLE dbo.yorum_kaldirma_talepleri ADD otel_id bigint NULL;
GO
IF COL_LENGTH(N'dbo.yorum_kaldirma_talepleri', N'partner_kullanici_id') IS NULL
    ALTER TABLE dbo.yorum_kaldirma_talepleri ADD partner_kullanici_id bigint NULL;
GO
IF COL_LENGTH(N'dbo.yorum_kaldirma_talepleri', N'sebep') IS NULL
    ALTER TABLE dbo.yorum_kaldirma_talepleri ADD sebep nvarchar(800) NULL;
GO
IF COL_LENGTH(N'dbo.yorum_kaldirma_talepleri', N'durum') IS NULL
    ALTER TABLE dbo.yorum_kaldirma_talepleri ADD durum nvarchar(40) NULL;
GO
IF COL_LENGTH(N'dbo.yorum_kaldirma_talepleri', N'admin_notu') IS NULL
    ALTER TABLE dbo.yorum_kaldirma_talepleri ADD admin_notu nvarchar(800) NULL;
GO
IF COL_LENGTH(N'dbo.yorum_kaldirma_talepleri', N'karar_veren_admin_id') IS NULL
    ALTER TABLE dbo.yorum_kaldirma_talepleri ADD karar_veren_admin_id bigint NULL;
GO
IF COL_LENGTH(N'dbo.yorum_kaldirma_talepleri', N'karar_tarihi') IS NULL
    ALTER TABLE dbo.yorum_kaldirma_talepleri ADD karar_tarihi datetime2(0) NULL;
GO
IF COL_LENGTH(N'dbo.yorum_kaldirma_talepleri', N'olusturulma_tarihi') IS NULL
    ALTER TABLE dbo.yorum_kaldirma_talepleri ADD olusturulma_tarihi datetime2(0) NULL;
GO
IF COL_LENGTH(N'dbo.yorum_kaldirma_talepleri', N'guncellenme_tarihi') IS NULL
    ALTER TABLE dbo.yorum_kaldirma_talepleri ADD guncellenme_tarihi datetime2(0) NULL;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_yorum_kaldirma_talepleri_yorum_id'
      AND object_id = OBJECT_ID(N'dbo.yorum_kaldirma_talepleri')
)
BEGIN
    CREATE INDEX IX_yorum_kaldirma_talepleri_yorum_id
    ON dbo.yorum_kaldirma_talepleri(yorum_id);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_yorum_kaldirma_talepleri_durum'
      AND object_id = OBJECT_ID(N'dbo.yorum_kaldirma_talepleri')
)
BEGIN
    CREATE INDEX IX_yorum_kaldirma_talepleri_durum
    ON dbo.yorum_kaldirma_talepleri(durum, olusturulma_tarihi DESC);
END
GO

