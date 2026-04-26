-- SQL Server FK paketi (idempotent) - firma_oda_fiyat_musaitlik
IF OBJECT_ID(N'dbo.firma_oda_fiyat_musaitlik', N'U') IS NOT NULL
BEGIN
    -- firmalar FK
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_firma_oda_fiyat_musaitlik_firma')
    BEGIN
        ALTER TABLE dbo.firma_oda_fiyat_musaitlik WITH CHECK
        ADD CONSTRAINT FK_firma_oda_fiyat_musaitlik_firma
        FOREIGN KEY (firma_id) REFERENCES dbo.firmalar(id);
    END

    -- oteller FK
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_firma_oda_fiyat_musaitlik_otel')
    BEGIN
        ALTER TABLE dbo.firma_oda_fiyat_musaitlik WITH CHECK
        ADD CONSTRAINT FK_firma_oda_fiyat_musaitlik_otel
        FOREIGN KEY (otel_id) REFERENCES dbo.oteller(id);
    END

    -- oda_tipleri FK
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_firma_oda_fiyat_musaitlik_oda_tip')
    BEGIN
        ALTER TABLE dbo.firma_oda_fiyat_musaitlik WITH CHECK
        ADD CONSTRAINT FK_firma_oda_fiyat_musaitlik_oda_tip
        FOREIGN KEY (oda_tip_id) REFERENCES dbo.oda_tipleri(id);
    END

    -- users FK (guncelleyen)
    IF COL_LENGTH('dbo.firma_oda_fiyat_musaitlik', 'guncelleyen_kullanici_id') IS NOT NULL
       AND OBJECT_ID(N'dbo.users', N'U') IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_firma_oda_fiyat_musaitlik_users_updated_by')
    BEGIN
        ALTER TABLE dbo.firma_oda_fiyat_musaitlik WITH CHECK
        ADD CONSTRAINT FK_firma_oda_fiyat_musaitlik_users_updated_by
        FOREIGN KEY (guncelleyen_kullanici_id) REFERENCES dbo.users(id);
    END
END

