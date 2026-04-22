IF COL_LENGTH('dbo.users', 'profil_resim_url') IS NULL
BEGIN
    ALTER TABLE dbo.users ADD profil_resim_url NVARCHAR(255) NULL;
END;

IF COL_LENGTH('dbo.users', 'profil_resim_kaynak') IS NULL
BEGIN
    ALTER TABLE dbo.users ADD profil_resim_kaynak NVARCHAR(30) NULL;
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_users_profil_resim_url' AND object_id = OBJECT_ID('dbo.users'))
BEGIN
    EXEC sys.sp_executesql N'CREATE INDEX IX_users_profil_resim_url ON dbo.users(profil_resim_url);';
END;

