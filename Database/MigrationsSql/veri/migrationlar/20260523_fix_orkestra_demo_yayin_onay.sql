-- UTF-8 BOM: demo otellerin yayin/onay metinlerini HotelService slug sorgusu ile uyumlu hale getirir
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;

UPDATE [dbo].[OTELLER]
SET [YAYIN_DURUMU] = N'Yayında',
    [ONAY_DURUMU] = N'Onaylandı'
WHERE [OTEL_KODU] LIKE N'ORK-SEED-%';

GO
