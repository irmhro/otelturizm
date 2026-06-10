-- Idempotent: Misafir faturasi yukleme e-posta sablonlari
-- Uygulama: sqlcmd -I -f 65001 -b -i "...\20260610_seed_bildirim_sablonlari_fatura_yuklendi.sql"
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
            (N'fatura_yuklendi', N'Fatura Yüklendi', N'E-posta', N'tr', N'Faturanız yüklendi', N'Faturanız yüklendi', N'Views/Email/tr/Fatura_Yuklendi.cshtml', N'user_first_name,hotel_name,booking_reference,invoice_download_link,invoices_panel_link'),
            (N'fatura_yuklendi', N'Invoice Uploaded', N'E-posta', N'en', N'Your invoice is ready', N'Invoice uploaded', N'Views/Email/en/Fatura_Yuklendi.cshtml', N'user_first_name,hotel_name,booking_reference,invoice_download_link,invoices_panel_link')
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

    PRINT CONCAT(N'fatura_yuklendi sablonlari tamamlandi. Etkilenen kayit: ', @@ROWCOUNT);

    COMMIT;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK;
    THROW;
END CATCH;
