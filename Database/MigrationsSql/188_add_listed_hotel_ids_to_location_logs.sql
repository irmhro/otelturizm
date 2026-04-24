IF COL_LENGTH(N'dbo.kullanici_konum_loglari', N'listelenen_otel_idleri') IS NULL
BEGIN
    ALTER TABLE dbo.kullanici_konum_loglari
    ADD listelenen_otel_idleri NVARCHAR(MAX) NULL;
END
GO

IF OBJECT_ID(N'dbo.schema_migrations', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.schema_migrations', N'script_name') IS NOT NULL
    BEGIN
        EXEC(N'
            IF NOT EXISTS (
                SELECT 1
                FROM dbo.schema_migrations
                WHERE script_name = N''188_add_listed_hotel_ids_to_location_logs.sql''
            )
            BEGIN
                INSERT INTO dbo.schema_migrations (script_name, checksum, applied_at)
                VALUES (N''188_add_listed_hotel_ids_to_location_logs.sql'', N''1881881881881881881881881881881881881881881881881881881881881881'', SYSUTCDATETIME());
            END
        ');
    END
    ELSE IF COL_LENGTH(N'dbo.schema_migrations', N'file_name') IS NOT NULL
    BEGIN
        EXEC(N'
            IF NOT EXISTS (
                SELECT 1
                FROM dbo.schema_migrations
                WHERE file_name = N''188_add_listed_hotel_ids_to_location_logs.sql''
            )
            BEGIN
                INSERT INTO dbo.schema_migrations (file_name, applied_at)
                VALUES (N''188_add_listed_hotel_ids_to_location_logs.sql'', SYSUTCDATETIME());
            END
        ');
    END
END
GO
