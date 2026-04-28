/* Unicode konu alanlari: Türkçe karakter bozulmasini onler (SQL Server) */

IF OBJECT_ID(N'dbo.bildirim_sablonlari', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.bildirim_sablonlari', 'konu') IS NOT NULL
    BEGIN
        DECLARE @konuTypeSablon NVARCHAR(128);
        SELECT @konuTypeSablon = TYPE_NAME(c.user_type_id)
        FROM sys.columns c
        WHERE c.object_id = OBJECT_ID(N'dbo.bildirim_sablonlari')
          AND c.name = N'konu';

        IF @konuTypeSablon IS NOT NULL AND @konuTypeSablon <> N'nvarchar'
        BEGIN
            ALTER TABLE dbo.bildirim_sablonlari ALTER COLUMN konu NVARCHAR(300) NULL;
        END
    END
END

IF OBJECT_ID(N'dbo.bildirim_loglari', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.bildirim_loglari', 'konu') IS NOT NULL
    BEGIN
        DECLARE @konuTypeLog NVARCHAR(128);
        SELECT @konuTypeLog = TYPE_NAME(c.user_type_id)
        FROM sys.columns c
        WHERE c.object_id = OBJECT_ID(N'dbo.bildirim_loglari')
          AND c.name = N'konu';

        IF @konuTypeLog IS NOT NULL AND @konuTypeLog <> N'nvarchar'
        BEGIN
            ALTER TABLE dbo.bildirim_loglari ALTER COLUMN konu NVARCHAR(300) NULL;
        END
    END
END

