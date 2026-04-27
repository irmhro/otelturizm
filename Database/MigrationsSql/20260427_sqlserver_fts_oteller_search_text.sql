-- SQL Server Full-Text Search genişletme (opsiyonel, idempotent)
-- Amaç: otel adı + şehir/ilçe/mahalle birleşik aramada LIKE maliyetini düşürmek.
-- Gereksinim: SQL Server Full-Text bileşeni yüklü olmalı.

IF OBJECT_ID(N'dbo.oteller', N'U') IS NOT NULL
BEGIN
    -- 1) Birleşik arama metni (persisted computed)
    IF COL_LENGTH(N'dbo.oteller', N'fts_search_text') IS NULL
    BEGIN
        ALTER TABLE dbo.oteller
        ADD fts_search_text AS
            CONCAT(
                COALESCE(otel_adi, N''), N' ',
                COALESCE(mahalle, N''), N' ',
                COALESCE(ilce, N''), N' ',
                COALESCE(sehir, N'')
            ) PERSISTED;
    END

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
            -- Full-text catalog yoksa oluştur.
            IF NOT EXISTS (SELECT 1 FROM sys.fulltext_catalogs WHERE name = N'ftc_otelturizm')
            BEGIN
                EXEC (N'CREATE FULLTEXT CATALOG ftc_otelturizm AS DEFAULT;');
            END

            DECLARE @pkName sysname;
            SELECT TOP (1) @pkName = kc.name
            FROM sys.key_constraints kc
            WHERE kc.parent_object_id = OBJECT_ID(N'dbo.oteller')
              AND kc.[type] = N'PK'
            ORDER BY kc.name;

            IF (@pkName IS NOT NULL)
            BEGIN
                IF EXISTS (SELECT 1 FROM sys.fulltext_indexes WHERE object_id = OBJECT_ID(N'dbo.oteller'))
                BEGIN
                    -- Mevcut fulltext indexe kolon ekle (varsa tekrar etme).
                    IF NOT EXISTS (
                        SELECT 1
                        FROM sys.fulltext_index_columns fic
                        WHERE fic.object_id = OBJECT_ID(N'dbo.oteller')
                          AND fic.column_id = COLUMNPROPERTY(OBJECT_ID(N'dbo.oteller'), N'fts_search_text', 'ColumnId')
                    )
                    BEGIN
                        EXEC (N'ALTER FULLTEXT INDEX ON dbo.oteller ADD (fts_search_text LANGUAGE 1055);');
                    END
                END
                ELSE
                BEGIN
                    DECLARE @sql NVARCHAR(MAX) = N'
                        CREATE FULLTEXT INDEX ON dbo.oteller
                        (
                            otel_adi LANGUAGE 1055,
                            fts_search_text LANGUAGE 1055
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

