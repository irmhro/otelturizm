SET NOCOUNT ON;

IF COL_LENGTH('dbo.users', 'iki_asamali_dogrulama_kanali') IS NULL
BEGIN
    ALTER TABLE dbo.users
    ADD iki_asamali_dogrulama_kanali NVARCHAR(20) NULL;
END;

IF COL_LENGTH('dbo.users', 'iki_asamali_dogrulama_kanali') IS NOT NULL
BEGIN
    EXEC(N'
        UPDATE dbo.users
        SET iki_asamali_dogrulama_kanali = CASE
                WHEN COALESCE(LTRIM(RTRIM(iki_asamali_dogrulama_kanali)), '''') = '''' THEN N''email''
                ELSE LOWER(LTRIM(RTRIM(iki_asamali_dogrulama_kanali)))
            END
        WHERE COALESCE(LTRIM(RTRIM(iki_asamali_dogrulama_kanali)), '''') = ''''
           OR iki_asamali_dogrulama_kanali NOT IN (N''email'', N''whatsapp'');
    ');
END;

IF COL_LENGTH('dbo.kullanici_giris_2fa_tokenlari', 'kanal') IS NULL
BEGIN
    ALTER TABLE dbo.kullanici_giris_2fa_tokenlari
    ADD kanal NVARCHAR(20) NOT NULL CONSTRAINT DF_kullanici_giris_2fa_kanal DEFAULT N'whatsapp';
END;

IF COL_LENGTH('dbo.kullanici_giris_2fa_tokenlari', 'eposta') IS NULL
BEGIN
    ALTER TABLE dbo.kullanici_giris_2fa_tokenlari
    ADD eposta NVARCHAR(255) NULL;
END;

IF COL_LENGTH('dbo.kullanici_giris_2fa_tokenlari', 'kanal') IS NOT NULL
BEGIN
    EXEC(N'
        UPDATE dbo.kullanici_giris_2fa_tokenlari
        SET kanal = CASE
                WHEN COALESCE(NULLIF(LTRIM(RTRIM(eposta)), N''''), N'''') <> N'''' THEN N''email''
                WHEN COALESCE(NULLIF(LTRIM(RTRIM(telefon_e164)), N''''), N'''') <> N'''' THEN N''whatsapp''
                ELSE N''email''
            END
        WHERE kanal NOT IN (N''email'', N''whatsapp'');
    ');
END;

IF NOT EXISTS (
    SELECT 1
    FROM dbo.bildirim_sablonlari
    WHERE sablon_kodu = N'login_2fa_email'
      AND tur = N'E-posta')
BEGIN
    INSERT INTO dbo.bildirim_sablonlari
    (sablon_kodu, sablon_adi, tur, dil, konu, baslik, icerik, degiskenler, aktif_mi, olusturulma_tarihi)
    VALUES
    (N'login_2fa_email', N'Giriş İki Aşamalı Doğrulama E-postası', N'E-posta', N'tr', N'Giriş güvenlik kodunuz', N'Güvenlik Kodunuz', N'Views/Email/Giris Guvenlik Kodu.cshtml', N'verification_code,user_first_name,verification_channel,login_time', 1, SYSUTCDATETIME());
END;

IF OBJECT_ID(N'schema_migrations', N'U') IS NOT NULL
AND NOT EXISTS (SELECT 1 FROM schema_migrations WHERE script_name = N'181_add_email_channel_to_login_2fa.sql')
BEGIN
    INSERT INTO schema_migrations (script_name, checksum, applied_at)
    VALUES (N'181_add_email_channel_to_login_2fa.sql', N'manual-update', SYSUTCDATETIME());
END
