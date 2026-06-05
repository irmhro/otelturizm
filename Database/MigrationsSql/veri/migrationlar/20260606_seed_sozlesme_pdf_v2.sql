-- Idempotent: kullanici + partner sozlesme PDF v2 kayitlari (wwwroot/uploads/contracts)
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

IF OBJECT_ID(N'dbo.SOZLESMELER', N'U') IS NULL
BEGIN
    RAISERROR(N'SOZLESMELER tablosu bulunamadi.', 16, 1);
    RETURN;
END;

UPDATE [dbo].[SOZLESMELER]
SET [VERSIYON_NO] = CASE WHEN [VERSIYON_NO] < 2 THEN 2 ELSE [VERSIYON_NO] END,
    [GUNCELLENME_TARIHI] = SYSUTCDATETIME()
WHERE [SLUG] IN (
    N'kullanici-kullanim-kosullari',
    N'kullanici-kvkk-aydinlatma',
    N'partner-basvuru-sozlesmesi'
)
  AND [AKTIF_MI] = 1;

IF OBJECT_ID(N'dbo.SOZLESME_DOSYALARI', N'U') IS NOT NULL
BEGIN
    DECLARE @PartnerAgreementId bigint = (
        SELECT TOP (1) [ID] FROM [dbo].[SOZLESMELER]
        WHERE [SLUG] = N'partner-basvuru-sozlesmesi' AND [AKTIF_MI] = 1
        ORDER BY [VERSIYON_NO] DESC, [ID] DESC
    );
    DECLARE @UserAgreementId bigint = (
        SELECT TOP (1) [ID] FROM [dbo].[SOZLESMELER]
        WHERE [SLUG] = N'kullanici-kullanim-kosullari' AND [AKTIF_MI] = 1
        ORDER BY [VERSIYON_NO] DESC, [ID] DESC
    );
    DECLARE @UserKvkkId bigint = (
        SELECT TOP (1) [ID] FROM [dbo].[SOZLESMELER]
        WHERE [SLUG] = N'kullanici-kvkk-aydinlatma' AND [AKTIF_MI] = 1
        ORDER BY [VERSIYON_NO] DESC, [ID] DESC
    );

    IF @PartnerAgreementId IS NOT NULL
       AND NOT EXISTS (
           SELECT 1 FROM [dbo].[SOZLESME_DOSYALARI]
           WHERE [SOZLESME_ID] = @PartnerAgreementId
             AND [DOSYA_TIPI] = N'pdf'
             AND [DOSYA_YOLU] = N'/uploads/contracts/partner-basvuru-sozlesmesi-v2.pdf'
       )
    BEGIN
        INSERT INTO [dbo].[SOZLESME_DOSYALARI] ([SOZLESME_ID], [DOSYA_TIPI], [DOSYA_ADI], [DOSYA_YOLU], [MIME_TIPI], [OLUSTURULMA_TARIHI])
        VALUES (@PartnerAgreementId, N'pdf', N'Partner Kullanim Sozlesmesi v2.pdf', N'/uploads/contracts/partner-basvuru-sozlesmesi-v2.pdf', N'application/pdf', SYSUTCDATETIME());
    END;

    IF @UserAgreementId IS NOT NULL
       AND NOT EXISTS (
           SELECT 1 FROM [dbo].[SOZLESME_DOSYALARI]
           WHERE [SOZLESME_ID] = @UserAgreementId
             AND [DOSYA_TIPI] = N'pdf'
             AND [DOSYA_YOLU] = N'/uploads/contracts/kullanici-kullanim-kosullari-v2.pdf'
       )
    BEGIN
        INSERT INTO [dbo].[SOZLESME_DOSYALARI] ([SOZLESME_ID], [DOSYA_TIPI], [DOSYA_ADI], [DOSYA_YOLU], [MIME_TIPI], [OLUSTURULMA_TARIHI])
        VALUES (@UserAgreementId, N'pdf', N'Kullanici Kullanim Sozlesmesi v2.pdf', N'/uploads/contracts/kullanici-kullanim-kosullari-v2.pdf', N'application/pdf', SYSUTCDATETIME());
    END;

    IF @UserKvkkId IS NOT NULL
       AND NOT EXISTS (
           SELECT 1 FROM [dbo].[SOZLESME_DOSYALARI]
           WHERE [SOZLESME_ID] = @UserKvkkId
             AND [DOSYA_TIPI] = N'pdf'
             AND [DOSYA_YOLU] = N'/uploads/contracts/kullanici-kvkk-aydinlatma-v2.pdf'
       )
    BEGIN
        INSERT INTO [dbo].[SOZLESME_DOSYALARI] ([SOZLESME_ID], [DOSYA_TIPI], [DOSYA_ADI], [DOSYA_YOLU], [MIME_TIPI], [OLUSTURULMA_TARIHI])
        VALUES (@UserKvkkId, N'pdf', N'KVKK Aydinlatma Metni v2.pdf', N'/uploads/contracts/kullanici-kvkk-aydinlatma-v2.pdf', N'application/pdf', SYSUTCDATETIME());
    END;
END;

SELECT s.[SLUG], s.[VERSIYON_NO], d.[DOSYA_YOLU], d.[DOSYA_ADI]
FROM [dbo].[SOZLESMELER] s
LEFT JOIN [dbo].[SOZLESME_DOSYALARI] d ON d.[SOZLESME_ID] = s.[ID] AND d.[DOSYA_TIPI] = N'pdf'
WHERE s.[SLUG] IN (N'partner-basvuru-sozlesmesi', N'kullanici-kullanim-kosullari', N'kullanici-kvkk-aydinlatma')
ORDER BY s.[SLUG], d.[ID] DESC;
