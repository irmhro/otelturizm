-- SQL Server Full-Text Search (opsiyonel, idempotent)
-- Amaç: otel_adi üzerinden global aramada LIKE maliyetini düşürmek.
-- Not: Sunucuda Full-Text bileşeni yoksa bu script hiçbir şey yapmadan çıkar.

IF OBJECT_ID(N'dbo.oteller', N'U') IS NOT NULL
BEGIN
    DECLARE @ftsInstalled INT = 0;
    BEGIN TRY
        SET @ftsInstalled = CAST(FULLTEXTSERVICEPROPERTY('IsFullTextInstalled') AS int);
    END TRY
    BEGIN CATCH
        SET @ftsInstalled = 0;
    END CATCH;

    IF (@ftsInstalled = 1)
    BEGIN
        BEGIN TRY
            IF NOT EXISTS (SELECT 1 FROM sys.fulltext_catalogs WHERE name = N'ftc_otelturizm')
            BEGIN
                EXEC (N'CREATE FULLTEXT CATALOG ftc_otelturizm AS DEFAULT;');
            END

            -- Full-text index bir unique key ister. PK ismini dinamik bulalım.
            DECLARE @pkName sysname;
            SELECT TOP (1) @pkName = kc.name
            FROM sys.key_constraints kc
            WHERE kc.parent_object_id = OBJECT_ID(N'dbo.oteller')
              AND kc.[type] = N'PK'
            ORDER BY kc.name;

            IF (@pkName IS NOT NULL)
            BEGIN
                IF NOT EXISTS (SELECT 1 FROM sys.fulltext_indexes WHERE object_id = OBJECT_ID(N'dbo.oteller'))
                BEGIN
                    DECLARE @sql NVARCHAR(MAX) = N'
                        CREATE FULLTEXT INDEX ON dbo.oteller
                        (
                            otel_adi LANGUAGE 1055
                        )
                        KEY INDEX ' + QUOTENAME(@pkName) + N'
                        WITH CHANGE_TRACKING AUTO;';
                    EXEC (@sql);
                END
            END
        END TRY
        BEGIN CATCH
            -- FTS bileseni kurulu gorunse bile yuklenemeyebilir; bu script opsiyoneldir.
        END CATCH
    END
END

