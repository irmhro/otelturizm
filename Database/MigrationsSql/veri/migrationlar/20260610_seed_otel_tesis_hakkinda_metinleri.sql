-- Idempotent: Tesis Hakkında metinlerini zenginleştir (demo oteller + kısa açıklamalı kayıtlar)
-- Uygulama: sqlcmd -I -f 65001 -b -i "...\20260610_seed_otel_tesis_hakkinda_metinleri.sql"
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

IF OBJECT_ID(N'dbo.OTELLER', N'U') IS NULL
BEGIN
    PRINT N'OTELLER tablosu bulunamadi, atlandi.';
    RETURN;
END;

BEGIN TRY
    BEGIN TRAN;

    UPDATE [dbo].[OTELLER]
    SET [UZUN_ACIKLAMA] = N'Göreme Vadi Cave Suites, restore edilmiş taş odaları, gün doğumu terası ve yerel şarap tadımı ile Kapadokya''nın kalbinde butik bir konaklama sunar. Vadinin balon manzarasına hakim konumu sayesinde sabah erken saatlerde sıcak hava balonlarını yakından izleyebilirsiniz.',
        [KONUM_ACIKLAMASI] = N'Balon kalkış alanına 5 dk yürüme mesafesinde; Göreme açık hava müzesi, yeraltı şehirleri ve ATV rotalarına kolay ulaşım.'
    WHERE [OTEL_KODU] = N'DEMO-KAPADOKYA-2026';

    UPDATE [dbo].[OTELLER]
    SET [UZUN_ACIKLAMA] = N'Antalya sahil şeridinde konumlanan demo otelimiz, denize yakın odaları, açık yüzme havuzu ve aile dostu tesis olanaklarıyla hem iş hem tatil konaklamalarına uygundur.',
        [KONUM_ACIKLAMASI] = N'Konyaaltı sahil yoluna ve toplu taşıma duraklarına birkaç dakika mesafede; havalimanı transferi için pratik konum.'
    WHERE [OTEL_KODU] = N'DEMO-ANTALYA-2026';

    UPDATE [dbo].[OTELLER]
    SET [UZUN_ACIKLAMA] = N'Maidan Suites, şehir merkezine yakın konumuyla iş ve kısa konaklama ihtiyaçlarına uygun modern odalar sunar. Geniş lobi, hızlı check-in ve 7/24 resepsiyon hizmeti misafir deneyimini kolaylaştırır.',
        [KONUM_ACIKLAMASI] = N'Ana caddelere, alışveriş ve yeme-içme noktalarına yürüme mesafesinde merkezi konum.'
    WHERE [OTEL_KODU] = N'DEMO-MAIDAN-2026';

    UPDATE [dbo].[OTELLER]
    SET [KONUM_ACIKLAMASI] = LEFT(LTRIM(RTRIM([UZUN_ACIKLAMA])), 180)
    WHERE ( [KONUM_ACIKLAMASI] IS NULL OR LTRIM(RTRIM([KONUM_ACIKLAMASI])) = N'' OR LEN(LTRIM(RTRIM([KONUM_ACIKLAMASI]))) < 40 )
      AND [UZUN_ACIKLAMA] IS NOT NULL
      AND LTRIM(RTRIM([UZUN_ACIKLAMA])) <> N'';

    PRINT CONCAT(N'Tesis hakkinda metin seed tamam. Guncellenen kayit: ', @@ROWCOUNT);

    COMMIT;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK;
    THROW;
END CATCH;
