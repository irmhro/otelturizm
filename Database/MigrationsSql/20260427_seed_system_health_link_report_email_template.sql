IF NOT EXISTS (
    SELECT 1
    FROM dbo.bildirim_sablonlari
    WHERE sablon_kodu = N'system_health_link_report'
      AND tur = N'E-posta'
)
BEGIN
    INSERT INTO dbo.bildirim_sablonlari
    (sablon_kodu, sablon_adi, tur, dil, konu, baslik, icerik, degiskenler, aktif_mi, olusturulma_tarihi)
    VALUES
    (N'system_health_link_report', N'Sistem Sağlığı Link Kontrol Raporu', N'E-posta', N'tr',
     N'Sistem Sağlığı: Broken link raporu', N'Broken Link Raporu',
     N'Views/Email/Link Kontrol Raporu.cshtml',
     N'base_url,checked_at,ok_count,bad_count,total_count,bad_list',
     1, SYSUTCDATETIME());
END;

IF OBJECT_ID(N'schema_migrations', N'U') IS NOT NULL
AND NOT EXISTS (SELECT 1 FROM schema_migrations WHERE script_name = N'20260427_seed_system_health_link_report_email_template.sql')
BEGIN
    INSERT INTO schema_migrations (script_name, checksum, applied_at)
    VALUES (N'20260427_seed_system_health_link_report_email_template.sql', N'manual-update', SYSUTCDATETIME());
END

