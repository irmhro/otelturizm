IF COL_LENGTH('dbo.developer_bildirimleri', 'sayfa_basligi') IS NULL
BEGIN
    ALTER TABLE dbo.developer_bildirimleri
    ADD sayfa_basligi NVARCHAR(220) NULL;
END;
GO
