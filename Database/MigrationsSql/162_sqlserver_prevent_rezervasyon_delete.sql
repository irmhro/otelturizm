IF OBJECT_ID('dbo.tr_rezervasyonlar_prevent_delete_sqlserver', 'TR') IS NOT NULL
BEGIN
    DROP TRIGGER dbo.tr_rezervasyonlar_prevent_delete_sqlserver;
END;
GO

CREATE TRIGGER dbo.tr_rezervasyonlar_prevent_delete_sqlserver
ON dbo.rezervasyonlar
INSTEAD OF DELETE
AS
BEGIN
    SET NOCOUNT ON;
    THROW 50001, N'Rezervasyon kayıtları silinemez. Yalnızca iptal süreci kullanılabilir.', 1;
END;
