:setvar DatabaseName "otelturizm_2026db"

IF DB_ID(N'$(DatabaseName)') IS NULL
BEGIN
    RAISERROR(N'Database not found: $(DatabaseName)', 16, 1);
    RETURN;
END
GO

USE [$(DatabaseName)];
GO

SELECT
    DB_NAME() AS veritabani_adi,
    COUNT(*) AS toplam_tablo
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_TYPE = 'BASE TABLE';
GO

SELECT
    CASE WHEN OBJECT_ID(N'dbo.schema_migrations', N'U') IS NOT NULL THEN 1 ELSE 0 END AS schema_migrations_var_mi,
    CASE WHEN OBJECT_ID(N'dbo.users', N'U') IS NOT NULL THEN 1 ELSE 0 END AS users_var_mi,
    CASE WHEN OBJECT_ID(N'dbo.oteller', N'U') IS NOT NULL THEN 1 ELSE 0 END AS oteller_var_mi,
    CASE WHEN OBJECT_ID(N'dbo.oda_tipleri', N'U') IS NOT NULL THEN 1 ELSE 0 END AS oda_tipleri_var_mi,
    CASE WHEN OBJECT_ID(N'dbo.oda_fiyat_musaitlik', N'U') IS NOT NULL THEN 1 ELSE 0 END AS oda_fiyat_musaitlik_var_mi;
GO

IF OBJECT_ID(N'dbo.schema_migrations', N'U') IS NOT NULL
BEGIN
    SELECT TOP (20)
        dosya_adi,
        uygulanma_tarihi
    FROM dbo.schema_migrations
    ORDER BY id DESC;
END
GO
