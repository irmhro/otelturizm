-- SQL Server (idempotent): sozlesme_dosyalari tablosuna guvenli_dosya_id ekle
IF OBJECT_ID(N'dbo.sozlesme_dosyalari', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.sozlesme_dosyalari', N'guvenli_dosya_id') IS NULL
    BEGIN
        ALTER TABLE dbo.sozlesme_dosyalari
        ADD guvenli_dosya_id BIGINT NULL;
    END

    IF COL_LENGTH(N'dbo.sozlesme_dosyalari', N'guvenli_dosya_id') IS NOT NULL
       AND OBJECT_ID(N'dbo.guvenli_dosya_varliklari', N'U') IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_sozlesme_dosyalari_guvenli_dosya')
    BEGIN
        ALTER TABLE dbo.sozlesme_dosyalari WITH CHECK
        ADD CONSTRAINT FK_sozlesme_dosyalari_guvenli_dosya
        FOREIGN KEY (guvenli_dosya_id) REFERENCES dbo.guvenli_dosya_varliklari(id);
    END

    IF NOT EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE name = N'IX_sozlesme_dosyalari_guvenli_dosya_id'
          AND object_id = OBJECT_ID(N'dbo.sozlesme_dosyalari')
    )
    BEGIN
        CREATE INDEX IX_sozlesme_dosyalari_guvenli_dosya_id
        ON dbo.sozlesme_dosyalari(guvenli_dosya_id);
    END
END

