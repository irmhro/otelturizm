-- OtelPuan seviyeleri ve ödül kataloğu (idempotent seed)
SET NOCOUNT ON;

IF OBJECT_ID(N'dbo.SADAKAT_SEVIYELERI', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM [dbo].[SADAKAT_SEVIYELERI] WHERE [KOD] = N'BRONZE')
    BEGIN
        INSERT INTO [dbo].[SADAKAT_SEVIYELERI] ([KOD], [AD], [MINIMUM_PUAN], [MAXIMUM_PUAN], [RENK_KODU], [IKON], [AVANTAJLAR_METIN], [SIRA_NO], [AKTIF_MI])
        VALUES (N'BRONZE', N'Bronz', 0, 999, N'#CD7F32', N'fas fa-medal', N'Yuzde 5 indirim|Hos geldin puani', 1, 1);
    END;

    IF NOT EXISTS (SELECT 1 FROM [dbo].[SADAKAT_SEVIYELERI] WHERE [KOD] = N'SILVER')
    BEGIN
        INSERT INTO [dbo].[SADAKAT_SEVIYELERI] ([KOD], [AD], [MINIMUM_PUAN], [MAXIMUM_PUAN], [RENK_KODU], [IKON], [AVANTAJLAR_METIN], [SIRA_NO], [AKTIF_MI])
        VALUES (N'SILVER', N'Gumus', 1000, 4999, N'#C0C0C0', N'fas fa-star', N'Yuzde 8 indirim|Erken check-in|Oncelikli destek', 2, 1);
    END;

    IF NOT EXISTS (SELECT 1 FROM [dbo].[SADAKAT_SEVIYELERI] WHERE [KOD] = N'GOLD')
    BEGIN
        INSERT INTO [dbo].[SADAKAT_SEVIYELERI] ([KOD], [AD], [MINIMUM_PUAN], [MAXIMUM_PUAN], [RENK_KODU], [IKON], [AVANTAJLAR_METIN], [SIRA_NO], [AKTIF_MI])
        VALUES (N'GOLD', N'Altin', 5000, 14999, N'#FFD700', N'fas fa-crown', N'Yuzde 12 indirim|Oda yukseltme|Gec cikis', 3, 1);
    END;

    IF NOT EXISTS (SELECT 1 FROM [dbo].[SADAKAT_SEVIYELERI] WHERE [KOD] = N'PLATINUM')
    BEGIN
        INSERT INTO [dbo].[SADAKAT_SEVIYELERI] ([KOD], [AD], [MINIMUM_PUAN], [MAXIMUM_PUAN], [RENK_KODU], [IKON], [AVANTAJLAR_METIN], [SIRA_NO], [AKTIF_MI])
        VALUES (N'PLATINUM', N'Platin', 15000, NULL, N'#E5E4E2', N'fas fa-gem', N'Yuzde 15 indirim|VIP destek|Ozel kampanyalar', 4, 1);
    END;
END;

IF OBJECT_ID(N'dbo.SADAKAT_ODULLERI', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM [dbo].[SADAKAT_ODULLERI] WHERE [KOD] = N'FREE_BREAKFAST')
    BEGIN
        INSERT INTO [dbo].[SADAKAT_ODULLERI] ([KOD], [AD], [ACIKLAMA], [GEREKLI_PUAN], [IKON], [TON], [AKTIF_MI])
        VALUES (N'FREE_BREAKFAST', N'Ucretsiz Kahvalti', N'Bir sonraki konaklamada kahvalti dahil', 500, N'fas fa-mug-hot', N'success', 1);
    END;

    IF NOT EXISTS (SELECT 1 FROM [dbo].[SADAKAT_ODULLERI] WHERE [KOD] = N'LATE_CHECKOUT')
    BEGIN
        INSERT INTO [dbo].[SADAKAT_ODULLERI] ([KOD], [AD], [ACIKLAMA], [GEREKLI_PUAN], [IKON], [TON], [AKTIF_MI])
        VALUES (N'LATE_CHECKOUT', N'Gec Cikis', N'14:00''a kadar gec cikis ayricaligi', 750, N'fas fa-clock', N'info', 1);
    END;

    IF NOT EXISTS (SELECT 1 FROM [dbo].[SADAKAT_ODULLERI] WHERE [KOD] = N'ROOM_UPGRADE')
    BEGIN
        INSERT INTO [dbo].[SADAKAT_ODULLERI] ([KOD], [AD], [ACIKLAMA], [GEREKLI_PUAN], [IKON], [TON], [AKTIF_MI])
        VALUES (N'ROOM_UPGRADE', N'Oda Yukseltme', N'Musaitlik durumunda ucretsiz oda yukseltme', 1500, N'fas fa-bed', N'primary', 1);
    END;

    IF NOT EXISTS (SELECT 1 FROM [dbo].[SADAKAT_ODULLERI] WHERE [KOD] = N'SPA_VOUCHER')
    BEGIN
        INSERT INTO [dbo].[SADAKAT_ODULLERI] ([KOD], [AD], [ACIKLAMA], [GEREKLI_PUAN], [IKON], [TON], [AKTIF_MI])
        VALUES (N'SPA_VOUCHER', N'SPA Kuponu', N'Secili otellerde spa/wellness indirimi', 2000, N'fas fa-spa', N'warning', 1);
    END;
END;

PRINT N'Sadakat seviye ve odul seed tamamlandi.';
