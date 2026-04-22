SET NOCOUNT ON;
DECLARE @userId bigint = 32;
DECLARE @email nvarchar(255) = N'irmhro0@gmail.com';
DECLARE @token nvarchar(200) = N'311bd1fa26d74863b9ac4d28bde618122ef583a8554b42a198709b649016f77a';
DECLARE @code nvarchar(20) = N'780943';
DECLARE @verificationLink nvarchar(max) = N'https://otelturizm.com/eposta-dogrula?email=irmhro0%40gmail.com&token=311bd1fa26d74863b9ac4d28bde618122ef583a8554b42a198709b649016f77a&code=780943';
DECLARE @templateId smallint = (SELECT TOP (1) id FROM bildirim_sablonlari WHERE sablon_kodu = 'email_verify' AND tur = N'E-posta' AND aktif_mi = 1 ORDER BY CASE WHEN dil='tr' THEN 1 ELSE 0 END DESC, id ASC);
DECLARE @body nvarchar(max) = N'<div style="font-family:Segoe UI,Arial,sans-serif;line-height:1.6"><h2>E-posta adresinizi doğrulayın</h2><p>Merhaba Admin,</p><p>Doğrulama kodunuz: <strong>' + @code + N'</strong></p><p><a href="' + @verificationLink + N'">E-posta adresimi doğrula</a></p><p>Bu bağlantı 24 saat geçerlidir.</p></div>';
INSERT INTO email_dogrulama_tokenlari
(kullanici_id, eposta, token, dogrulama_kodu, kullanildi_mi, deneme_sayisi, maksimum_deneme, ip_adresi, user_agent, gecerlilik_suresi, olusturulma_tarihi)
VALUES
(@userId, @email, @token, @code, 0, 0, 5, N'185.111.244.246', N'Codex pickup verification test', DATEADD(HOUR, 24, SYSUTCDATETIME()), SYSUTCDATETIME());
UPDATE users SET email_dogrulama_son_gonderim_tarihi = SYSUTCDATETIME() WHERE id = @userId;
INSERT INTO bildirim_loglari
(kullanici_id, bildirim_sablon_id, tur, alici_eposta, konu, icerik, gonderilen_icerik, durum, saglayici, ilgili_tablo, ilgili_kayit_id)
VALUES
(@userId, @templateId, N'E-posta', @email, N'E-posta adresinizi doğrulayın', @body, @body, N'Beklemede', N'SMTP', N'users', @userId);
SELECT TOP 1 id, eposta, token, dogrulama_kodu, olusturulma_tarihi FROM email_dogrulama_tokenlari WHERE kullanici_id=@userId ORDER BY id DESC;
SELECT TOP 1 id, alici_eposta, durum, gonderme_denemesi, hata_kodu, hata_mesaji, olusturulma_tarihi FROM bildirim_loglari WHERE kullanici_id=@userId AND tur=N'E-posta' ORDER BY id DESC;
