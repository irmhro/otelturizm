SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

IF COL_LENGTH('dbo.oteller', 'favori_sayisi') IS NULL
BEGIN
    ALTER TABLE dbo.oteller
        ADD favori_sayisi INT NOT NULL
            CONSTRAINT DF_oteller_favori_sayisi DEFAULT 0;
END;
GO

IF OBJECT_ID(N'dbo.user_favori_oteller', N'U') IS NOT NULL
BEGIN
    ;WITH FavoriSayim AS
    (
        SELECT otel_id, COUNT(DISTINCT user_id) AS toplam
        FROM dbo.user_favori_oteller
        WHERE COALESCE(aktif_mi, 1) = 1
          AND kaldirilma_tarihi IS NULL
        GROUP BY otel_id
    )
    UPDATE o
    SET favori_sayisi = COALESCE(f.toplam, 0)
    FROM dbo.oteller o
    LEFT JOIN FavoriSayim f ON f.otel_id = o.id;
END;
GO

IF OBJECT_ID(N'dbo.trg_user_favori_oteller_sync_oteller_favori_sayisi', N'TR') IS NOT NULL
    DROP TRIGGER dbo.trg_user_favori_oteller_sync_oteller_favori_sayisi;
GO

IF OBJECT_ID(N'dbo.user_favori_oteller', N'U') IS NOT NULL
BEGIN
    EXEC('
    CREATE TRIGGER dbo.trg_user_favori_oteller_sync_oteller_favori_sayisi
    ON dbo.user_favori_oteller
    AFTER INSERT, UPDATE, DELETE
    AS
    BEGIN
        SET NOCOUNT ON;

        ;WITH EtkilenenOteller AS
        (
            SELECT DISTINCT otel_id FROM inserted WHERE otel_id IS NOT NULL
            UNION
            SELECT DISTINCT otel_id FROM deleted WHERE otel_id IS NOT NULL
        ),
        FavoriSayim AS
        (
            SELECT uf.otel_id, COUNT(DISTINCT uf.user_id) AS toplam
            FROM dbo.user_favori_oteller uf
            INNER JOIN EtkilenenOteller eo ON eo.otel_id = uf.otel_id
            WHERE COALESCE(uf.aktif_mi, 1) = 1
              AND uf.kaldirilma_tarihi IS NULL
            GROUP BY uf.otel_id
        )
        UPDATE o
        SET favori_sayisi = COALESCE(fs.toplam, 0)
        FROM dbo.oteller o
        INNER JOIN EtkilenenOteller eo ON eo.otel_id = o.id
        LEFT JOIN FavoriSayim fs ON fs.otel_id = o.id;
    END
    ');
END;
GO
