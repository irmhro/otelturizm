SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;

UPDATE dbo.bildirim_loglari
SET durum = N'SMTP Kabul'
WHERE tur = N'E-posta'
  AND durum = N'Gönderildi'
  AND COALESCE(saglayici, N'') = N'SMTP';
