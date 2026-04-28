/* Partner e-posta giriş onayı (admin) - idempotent */

IF COL_LENGTH('dbo.partner_detaylari', 'eposta_giris_onayi_verildi_mi') IS NULL
BEGIN
    ALTER TABLE dbo.partner_detaylari
        ADD eposta_giris_onayi_verildi_mi bit NOT NULL CONSTRAINT DF_partner_detaylari_eposta_giris_onayi_verildi_mi DEFAULT (0) WITH VALUES;
END;

IF COL_LENGTH('dbo.partner_detaylari', 'eposta_giris_onay_tarihi') IS NULL
BEGIN
    ALTER TABLE dbo.partner_detaylari
        ADD eposta_giris_onay_tarihi datetime2 NULL;
END;

IF COL_LENGTH('dbo.partner_detaylari', 'eposta_giris_onaylayan_admin_id') IS NULL
BEGIN
    ALTER TABLE dbo.partner_detaylari
        ADD eposta_giris_onaylayan_admin_id bigint NULL;
END;

