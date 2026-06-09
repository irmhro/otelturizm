-- Idempotent: Admin partner/firma basvuru bildirim sablonlari
-- Uygulama: sqlcmd -I -f 65001 -b -i "...\20260608_seed_bildirim_sablonlari_basvuru.sql"
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

IF OBJECT_ID(N'dbo.BILDIRIM_SABLONLARI', N'U') IS NULL
BEGIN
    PRINT N'BILDIRIM_SABLONLARI tablosu bulunamadi, atlandi.';
    RETURN;
END;

BEGIN TRY
    BEGIN TRAN;

    ;WITH src AS
    (
        SELECT * FROM (VALUES
            (N'admin_partner_basvuru', N'Admin Partner Başvuru Bildirimi', N'E-posta', N'tr', N'Yeni partner başvurusu', N'Partner Başvurusu', N'Views/Email/tr/Admin_Routing_Bildirimi.cshtml', N'email_subject,badge,title,intro,detail_html,primary_url,primary_label,event_code,occurred_at'),
            (N'admin_firma_basvuru', N'Admin Firma Başvuru Bildirimi', N'E-posta', N'tr', N'Yeni firma başvurusu', N'Firma Başvurusu', N'Views/Email/tr/Admin_Routing_Bildirimi.cshtml', N'email_subject,badge,title,intro,detail_html,primary_url,primary_label,event_code,occurred_at')
        ) x(SABLON_KODU, SABLON_ADI, TUR, DIL, KONU, BASLIK, ICERIK, DEGISKENLER)
    )
    MERGE [dbo].[BILDIRIM_SABLONLARI] AS t
    USING src AS s
       ON t.[SABLON_KODU] = s.[SABLON_KODU] AND t.[TUR] = s.[TUR] AND t.[DIL] = s.[DIL]
    WHEN MATCHED THEN UPDATE SET
        [SABLON_ADI] = s.[SABLON_ADI],
        [KONU] = s.[KONU],
        [BASLIK] = s.[BASLIK],
        [ICERIK] = s.[ICERIK],
        [DEGISKENLER] = s.[DEGISKENLER],
        [AKTIF_MI] = 1
    WHEN NOT MATCHED THEN
        INSERT ([SABLON_KODU], [SABLON_ADI], [TUR], [DIL], [KONU], [BASLIK], [ICERIK], [DEGISKENLER], [AKTIF_MI], [OLUSTURULMA_TARIHI])
        VALUES (s.[SABLON_KODU], s.[SABLON_ADI], s.[TUR], s.[DIL], s.[KONU], s.[BASLIK], s.[ICERIK], s.[DEGISKENLER], 1, SYSUTCDATETIME());

    PRINT CONCAT(N'Basvuru bildirim sablonlari tamamlandi. Etkilenen kayit: ', @@ROWCOUNT);

    COMMIT;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK;
    THROW;
END CATCH;
