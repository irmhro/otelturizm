:setvar DatabaseName "otelturizm_2026db"

IF DB_ID(N'$(DatabaseName)') IS NULL
BEGIN
    PRINT N'Creating database: $(DatabaseName)';
    EXEC(N'CREATE DATABASE [' + REPLACE('$(DatabaseName)', ']', ']]') + N']');
END
ELSE
BEGIN
    PRINT N'Database already exists: $(DatabaseName)';
END
GO

USE [$(DatabaseName)];
GO

IF OBJECT_ID(N'dbo.schema_migrations', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.schema_migrations
    (
        id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        dosya_adi NVARCHAR(255) NOT NULL,
        uygulanma_tarihi DATETIME2(0) NOT NULL CONSTRAINT DF_schema_migrations_uygulanma_tarihi DEFAULT SYSUTCDATETIME()
    );

    CREATE UNIQUE INDEX UX_schema_migrations_dosya_adi
        ON dbo.schema_migrations(dosya_adi);
END
GO
