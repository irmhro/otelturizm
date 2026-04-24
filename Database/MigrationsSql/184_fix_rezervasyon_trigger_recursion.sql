IF OBJECT_ID(N'dbo.tr_rezervasyonlar_rezervasyon_durumu_sync', N'TR') IS NOT NULL
    DROP TRIGGER dbo.tr_rezervasyonlar_rezervasyon_durumu_sync;

GO

CREATE TRIGGER dbo.tr_rezervasyonlar_rezervasyon_durumu_sync
ON dbo.rezervasyonlar
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    IF TRIGGER_NESTLEVEL() > 1
        RETURN;

    UPDATE r
    SET rezervasyon_durumu_id = d.id
    FROM dbo.rezervasyonlar r
    INNER JOIN inserted i ON i.id = r.id
    INNER JOIN dbo.rezervasyon_durum_tanimlari d ON d.ad = r.durum
    WHERE ISNULL(r.rezervasyon_durumu_id, 0) <> d.id;
END;

GO

IF OBJECT_ID(N'dbo.tr_rezervasyonlar_odeme_durumu_sync', N'TR') IS NOT NULL
    DROP TRIGGER dbo.tr_rezervasyonlar_odeme_durumu_sync;

GO

CREATE TRIGGER dbo.tr_rezervasyonlar_odeme_durumu_sync
ON dbo.rezervasyonlar
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    IF TRIGGER_NESTLEVEL() > 1
        RETURN;

    UPDATE r
    SET odeme_durumu_id = d.id
    FROM dbo.rezervasyonlar r
    INNER JOIN inserted i ON i.id = r.id
    INNER JOIN dbo.odeme_durumu_tanimlari d ON d.ad = r.odeme_durumu
    WHERE COALESCE(r.odeme_durumu, N'') <> N''
      AND ISNULL(r.odeme_durumu_id, 0) <> d.id;
END;

GO
