SET NOCOUNT ON;

DECLARE @metadata NVARCHAR(MAX) = N'{
  "username": "info@otelturizm.com",
  "imap_host": "mail.otelturizm.com",
  "imap_port": 993,
  "pop3_host": "mail.otelturizm.com",
  "pop3_port": 995,
  "smtp_host": "umay.muvhost.com",
  "smtp_port": 587,
  "pickup_directory": "C:\\inetpub\\mailroot\\Pickup",
  "transport_mode": "smtp"
}';

UPDATE email_services
SET smtp_host = N'umay.muvhost.com',
    smtp_port = 587,
    guvenlik_tipi = N'STARTTLS',
    aktif_mi = 1,
    varsayilan_mi = 1,
    test_modu = 0,
    baglanti_zaman_asimi_saniye = 60,
    metadata = @metadata,
    guncellenme_tarihi = SYSUTCDATETIME()
WHERE servis_kodu = N'default_smtp';

IF OBJECT_ID(N'schema_migrations', N'U') IS NOT NULL
AND NOT EXISTS (SELECT 1 FROM schema_migrations WHERE script_name = N'180_switch_email_transport_to_real_smtp_host.sql')
BEGIN
    INSERT INTO schema_migrations (script_name, checksum, applied_at)
    VALUES (N'180_switch_email_transport_to_real_smtp_host.sql', N'manual-update', SYSUTCDATETIME());
END
