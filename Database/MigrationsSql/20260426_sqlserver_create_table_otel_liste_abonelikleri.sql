-- Otel liste abonelikleri (SQL Server, idempotent)
IF OBJECT_ID(N'dbo.otel_liste_abonelikleri', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.otel_liste_abonelikleri (
        id BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,

        otel_id BIGINT NOT NULL,

        -- IL / ILCE / MAHALLE
        kapsam_tipi NVARCHAR(16) NOT NULL,
        kapsam_degeri NVARCHAR(160) NOT NULL,
        kapsam_degeri_normalized NVARCHAR(160) NOT NULL,

        -- 1 / 2 / 3 (sabit pin)
        hedef_sira INT NOT NULL,

        baslangic_utc DATETIME2 NOT NULL,
        bitis_utc DATETIME2 NOT NULL,

        durum NVARCHAR(20) NOT NULL DEFAULT N'Beklemede', -- Beklemede / Onaylandı / Reddedildi / Askıda / İptal

        talep_eden_user_id BIGINT NULL,
        onaylayan_admin_user_id BIGINT NULL,

        admin_notu NVARCHAR(500) NULL,
        partner_notu NVARCHAR(500) NULL,

        olusturulma_tarihi DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        onay_tarihi DATETIME2 NULL
    );
END
GO

IF OBJECT_ID(N'dbo.otel_liste_abonelikleri', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_otel_liste_abonelikleri_kapsam' AND object_id = OBJECT_ID(N'dbo.otel_liste_abonelikleri'))
    BEGIN
        CREATE NONCLUSTERED INDEX IX_otel_liste_abonelikleri_kapsam
        ON dbo.otel_liste_abonelikleri (kapsam_tipi, kapsam_degeri_normalized, hedef_sira, durum, baslangic_utc, bitis_utc)
        INCLUDE (otel_id);
    END

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_otel_liste_abonelikleri_otel' AND object_id = OBJECT_ID(N'dbo.otel_liste_abonelikleri'))
    BEGIN
        CREATE NONCLUSTERED INDEX IX_otel_liste_abonelikleri_otel
        ON dbo.otel_liste_abonelikleri (otel_id, durum, baslangic_utc, bitis_utc)
        INCLUDE (kapsam_tipi, kapsam_degeri, hedef_sira);
    END
END
GO

-- FK (idempotent)
IF OBJECT_ID(N'dbo.otel_liste_abonelikleri', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_otel_liste_abonelikleri_oteller')
    BEGIN
        ALTER TABLE dbo.otel_liste_abonelikleri WITH CHECK
        ADD CONSTRAINT FK_otel_liste_abonelikleri_oteller
        FOREIGN KEY (otel_id) REFERENCES dbo.oteller(id);
    END

    IF OBJECT_ID(N'dbo.users', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_otel_liste_abonelikleri_users_requester')
    BEGIN
        ALTER TABLE dbo.otel_liste_abonelikleri WITH CHECK
        ADD CONSTRAINT FK_otel_liste_abonelikleri_users_requester
        FOREIGN KEY (talep_eden_user_id) REFERENCES dbo.users(id);
    END

    IF OBJECT_ID(N'dbo.users', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_otel_liste_abonelikleri_users_approver')
    BEGIN
        ALTER TABLE dbo.otel_liste_abonelikleri WITH CHECK
        ADD CONSTRAINT FK_otel_liste_abonelikleri_users_approver
        FOREIGN KEY (onaylayan_admin_user_id) REFERENCES dbo.users(id);
    END
END
GO

