-- Idempotent: REZERVASYON_FATURALARI foreign keys and indexes
SET NOCOUNT ON;

IF OBJECT_ID(N'dbo.REZERVASYON_FATURALARI', N'U') IS NULL
BEGIN
    PRINT N'REZERVASYON_FATURALARI tablosu yok, atlandi.';
    RETURN;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.REZERVASYON_FATURALARI') AND name = N'UX_rezervasyon_faturalari_rezervasyon')
BEGIN
    CREATE UNIQUE INDEX [UX_rezervasyon_faturalari_rezervasyon] ON [dbo].[REZERVASYON_FATURALARI] ([REZERVASYON_ID] ASC);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.REZERVASYON_FATURALARI') AND name = N'IX_rezervasyon_faturalari_otel')
BEGIN
    CREATE INDEX [IX_rezervasyon_faturalari_otel] ON [dbo].[REZERVASYON_FATURALARI] ([OTEL_ID] ASC, [OLUSTURULMA_TARIHI] DESC);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_rezervasyon_faturalari_rezervasyon')
BEGIN
    ALTER TABLE [dbo].[REZERVASYON_FATURALARI] WITH CHECK ADD CONSTRAINT [FK_rezervasyon_faturalari_rezervasyon]
        FOREIGN KEY ([REZERVASYON_ID]) REFERENCES [dbo].[REZERVASYONLAR] ([ID]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_rezervasyon_faturalari_otel')
BEGIN
    ALTER TABLE [dbo].[REZERVASYON_FATURALARI] WITH CHECK ADD CONSTRAINT [FK_rezervasyon_faturalari_otel]
        FOREIGN KEY ([OTEL_ID]) REFERENCES [dbo].[OTELLER] ([ID]);
END
GO

IF OBJECT_ID(N'dbo.GUVENLI_DOSYA_VARLIKLARI', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_rezervasyon_faturalari_guvenli_dosya')
BEGIN
    ALTER TABLE [dbo].[REZERVASYON_FATURALARI] WITH CHECK ADD CONSTRAINT [FK_rezervasyon_faturalari_guvenli_dosya]
        FOREIGN KEY ([GUVENLI_DOSYA_ID]) REFERENCES [dbo].[GUVENLI_DOSYA_VARLIKLARI] ([ID]);
END
GO

IF OBJECT_ID(N'dbo.KULLANICILAR', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_rezervasyon_faturalari_yukleyen')
BEGIN
    ALTER TABLE [dbo].[REZERVASYON_FATURALARI] WITH CHECK ADD CONSTRAINT [FK_rezervasyon_faturalari_yukleyen]
        FOREIGN KEY ([YUKLEYEN_KULLANICI_ID]) REFERENCES [dbo].[KULLANICILAR] ([ID]);
END
GO

PRINT N'REZERVASYON_FATURALARI constraint/index migration tamamlandi.';
GO
