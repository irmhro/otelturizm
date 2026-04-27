-- SQL Server indeks paketi (idempotent)
-- Hedef: EmailDeliveryBackgroundService ClaimPendingEmailsAsync kuyruğu hızlı seçebilsin.

IF OBJECT_ID(N'dbo.bildirim_loglari', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_bildirim_loglari_email_queue' AND object_id = OBJECT_ID(N'dbo.bildirim_loglari'))
    BEGIN
        CREATE NONCLUSTERED INDEX IX_bildirim_loglari_email_queue
        ON dbo.bildirim_loglari (tur, durum, olusturulma_tarihi, id)
        INCLUDE (alici_eposta, konu, gonderilen_icerik, gonderme_denemesi, maksimum_deneme, guncellenme_tarihi);
    END
END

