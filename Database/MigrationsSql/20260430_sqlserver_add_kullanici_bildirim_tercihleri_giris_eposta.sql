SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF COL_LENGTH(N'dbo.kullanici_bildirim_tercihleri', N'giris_eposta') IS NULL
BEGIN
    ALTER TABLE [dbo].[kullanici_bildirim_tercihleri]
        ADD [giris_eposta] bit CONSTRAINT [DF_kullanici_bildirim_tercihleri_giris_eposta] DEFAULT ((0)) NOT NULL;
END
GO

