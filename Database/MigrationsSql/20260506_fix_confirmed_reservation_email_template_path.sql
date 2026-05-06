SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;

UPDATE dbo.bildirim_sablonlari
SET icerik = N'Views/Email/tr/RezervasyonOnaylandi.cshtml'
WHERE sablon_kodu IN (N'reservation_confirmed_customer', N'rezervasyon_onay')
  AND tur = N'E-posta';

