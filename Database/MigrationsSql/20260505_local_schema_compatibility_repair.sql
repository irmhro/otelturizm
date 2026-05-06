SET NOCOUNT ON;

IF COL_LENGTH(N'dbo.users', N'profil_gorsel_guvenli_dosya_id') IS NULL
BEGIN
    ALTER TABLE dbo.users ADD profil_gorsel_guvenli_dosya_id BIGINT NULL;
END;

IF COL_LENGTH(N'dbo.users', N'iki_asamali_eposta_aktif') IS NULL
BEGIN
    ALTER TABLE dbo.users ADD iki_asamali_eposta_aktif BIT NOT NULL
        CONSTRAINT DF_users_iki_asamali_eposta_aktif DEFAULT (0);
END;

IF COL_LENGTH(N'dbo.oda_tipleri', N'max_yetiskin') IS NULL
BEGIN
    ALTER TABLE dbo.oda_tipleri ADD max_yetiskin TINYINT NULL;
    EXEC(N'UPDATE dbo.oda_tipleri SET max_yetiskin = maksimum_yetiskin_sayisi WHERE max_yetiskin IS NULL;');
END;

IF COL_LENGTH(N'dbo.oda_tipleri', N'max_cocuk') IS NULL
BEGIN
    ALTER TABLE dbo.oda_tipleri ADD max_cocuk TINYINT NULL;
    EXEC(N'UPDATE dbo.oda_tipleri SET max_cocuk = maksimum_cocuk_sayisi WHERE max_cocuk IS NULL;');
END;

IF COL_LENGTH(N'dbo.bildirim_loglari', N'email_service_id') IS NULL
BEGIN
    ALTER TABLE dbo.bildirim_loglari ADD email_service_id SMALLINT NULL;
END;

IF OBJECT_ID(N'dbo.guvenli_dosya_varliklari', N'U') IS NOT NULL
   AND NOT EXISTS (
        SELECT 1
        FROM sys.foreign_keys
        WHERE name = N'FK_users_profil_gorsel_guvenli_dosya'
   )
BEGIN
    EXEC(N'ALTER TABLE dbo.users WITH NOCHECK
        ADD CONSTRAINT FK_users_profil_gorsel_guvenli_dosya
        FOREIGN KEY (profil_gorsel_guvenli_dosya_id) REFERENCES dbo.guvenli_dosya_varliklari(id);');
END;

IF OBJECT_ID(N'dbo.email_services', N'U') IS NOT NULL
   AND NOT EXISTS (
        SELECT 1
        FROM sys.foreign_keys
        WHERE name = N'FK_bildirim_loglari_email_service'
   )
BEGIN
    EXEC(N'ALTER TABLE dbo.bildirim_loglari WITH NOCHECK
        ADD CONSTRAINT FK_bildirim_loglari_email_service
        FOREIGN KEY (email_service_id) REFERENCES dbo.email_services(id);');
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_bildirim_loglari_email_service_id'
      AND object_id = OBJECT_ID(N'dbo.bildirim_loglari')
)
BEGIN
    EXEC(N'CREATE INDEX IX_bildirim_loglari_email_service_id
        ON dbo.bildirim_loglari(email_service_id)
        WHERE email_service_id IS NOT NULL;');
END;
