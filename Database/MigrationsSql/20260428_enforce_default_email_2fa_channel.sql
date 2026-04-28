SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;

IF COL_LENGTH('dbo.users', 'iki_asamali_dogrulama_kanali') IS NOT NULL
BEGIN
    UPDATE dbo.users
    SET iki_asamali_dogrulama_kanali = N'email'
    WHERE NULLIF(LTRIM(RTRIM(COALESCE(iki_asamali_dogrulama_kanali, N''))), N'') IS NULL;

    IF NOT EXISTS (
        SELECT 1
        FROM sys.default_constraints dc
        INNER JOIN sys.columns c
            ON c.default_object_id = dc.object_id
        INNER JOIN sys.tables t
            ON t.object_id = c.object_id
        WHERE t.name = N'users'
          AND c.name = N'iki_asamali_dogrulama_kanali'
    )
    BEGIN
        ALTER TABLE dbo.users
        ADD CONSTRAINT DF_users_iki_asamali_dogrulama_kanali
            DEFAULT N'email' FOR iki_asamali_dogrulama_kanali;
    END
END
