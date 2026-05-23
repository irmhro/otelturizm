-- Seed: platform paket kategorileri, 5651/5661 paketleri, admin yetkisi (idempotent)

-- Kategoriler
IF NOT EXISTS (SELECT 1 FROM [dbo].[PLATFORM_PAKET_KATEGORILERI] WHERE [KOD] = N'5651')
BEGIN
    INSERT INTO [dbo].[PLATFORM_PAKET_KATEGORILERI] ([KOD], [BASLIK], [ACIKLAMA], [SIRA])
    VALUES (N'5651', N'5651 İnternet Loglama', N'5651 sayılı kanun kapsamında internet trafik loglama ve saklama hizmetleri.', 10);
END

IF NOT EXISTS (SELECT 1 FROM [dbo].[PLATFORM_PAKET_KATEGORILERI] WHERE [KOD] = N'5661')
BEGIN
    INSERT INTO [dbo].[PLATFORM_PAKET_KATEGORILERI] ([KOD], [BASLIK], [ACIKLAMA], [SIRA])
    VALUES (N'5661', N'5661 Konaklama Loglama', N'Konaklama tesisleri için misafir/kimlik bildirim ve loglama uyumluluk paketleri.', 20);
END

IF NOT EXISTS (SELECT 1 FROM [dbo].[PLATFORM_PAKET_KATEGORILERI] WHERE [KOD] = N'PAKET')
BEGIN
    INSERT INTO [dbo].[PLATFORM_PAKET_KATEGORILERI] ([KOD], [BASLIK], [ACIKLAMA], [SIRA])
    VALUES (N'PAKET', N'Kombine Paketler', N'5651 + 5661 birlikte kurulum ve işletme paketleri.', 30);
END

DECLARE @kat5651 bigint = (SELECT TOP 1 [ID] FROM [dbo].[PLATFORM_PAKET_KATEGORILERI] WHERE [KOD] = N'5651');
DECLARE @kat5661 bigint = (SELECT TOP 1 [ID] FROM [dbo].[PLATFORM_PAKET_KATEGORILERI] WHERE [KOD] = N'5661');
DECLARE @katPaket bigint = (SELECT TOP 1 [ID] FROM [dbo].[PLATFORM_PAKET_KATEGORILERI] WHERE [KOD] = N'PAKET');

IF @kat5651 IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [dbo].[PLATFORM_PAKETLER] WHERE [PAKET_KODU] = N'log-5651-standart')
BEGIN
    INSERT INTO [dbo].[PLATFORM_PAKETLER] (
        [KATEGORI_ID], [PAKET_KODU], [BASLIK], [KISA_ACIKLAMA], [DETAY_METIN],
        [FIYAT_TUTAR], [FATURA_PERIYODU], [PLATFORM_KOMISYON_ORANI], [HEDEF_KURAL],
        [KAPAK_GORSEL_URL], [GALERI_JSON], [OZELLIKLER_JSON], [SOZLESME_URL], [DURUM], [SIRA]
    ) VALUES (
        @kat5651, N'log-5651-standart', N'5651 Standart Loglama', N'Aylık internet log saklama ve raporlama.',
        N'Otelinizde 5651 uyumlu log toplama, saklama ve denetim raporu. Kurulum rehberi ve 7/24 destek dahildir.',
        2490.00, N'Aylik', 15.00, N'HER_OTEL',
        N'/assets/img/platform-paketleri/5651-standart.svg',
        N'["/assets/img/platform-paketleri/5651-standart.svg","/assets/img/platform-paketleri/5651-dashboard.svg"]',
        N'["5651 uyumlu log arşivi","Aylık denetim özeti","Kurulum rehberi","E-posta destek"]',
        N'/sozlesmeler/platform-paket-5651', N'Yayinda', 10
    );
END

IF @kat5661 IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [dbo].[PLATFORM_PAKETLER] WHERE [PAKET_KODU] = N'log-5661-konaklama')
BEGIN
    INSERT INTO [dbo].[PLATFORM_PAKETLER] (
        [KATEGORI_ID], [PAKET_KODU], [BASLIK], [KISA_ACIKLAMA], [DETAY_METIN],
        [FIYAT_TUTAR], [FATURA_PERIYODU], [PLATFORM_KOMISYON_ORANI], [HEDEF_KURAL],
        [KAPAK_GORSEL_URL], [GALERI_JSON], [OZELLIKLER_JSON], [SOZLESME_URL], [DURUM], [SIRA]
    ) VALUES (
        @kat5661, N'log-5661-konaklama', N'5661 Konaklama Log Paketi', N'5661 sistemi kurulu olmayan tesisler için.',
        N'Misafir bildirim ve konaklama log süreçlerinin platform üzerinden kurulumu. Partner üzerinden başvuru ve admin onayı ile aktivasyon.',
        1890.00, N'Aylik', 12.00, N'OTEL_5661_YOK',
        N'/assets/img/platform-paketleri/5661-konaklama.svg',
        N'["/assets/img/platform-paketleri/5661-konaklama.svg"]',
        N'["5661 uyumluluk kontrol listesi","Otel PMS entegrasyon rehberi","Başvuru sonrası kurulum","Partner satış komisyonu"]',
        N'/sozlesmeler/platform-paket-5661', N'Yayinda', 20
    );
END

IF @katPaket IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [dbo].[PLATFORM_PAKETLER] WHERE [PAKET_KODU] = N'log-5651-5661-bundle')
BEGIN
    INSERT INTO [dbo].[PLATFORM_PAKETLER] (
        [KATEGORI_ID], [PAKET_KODU], [BASLIK], [KISA_ACIKLAMA], [DETAY_METIN],
        [FIYAT_TUTAR], [FATURA_PERIYODU], [PLATFORM_KOMISYON_ORANI], [HEDEF_KURAL],
        [KAPAK_GORSEL_URL], [GALERI_JSON], [OZELLIKLER_JSON], [SOZLESME_URL], [DURUM], [SIRA]
    ) VALUES (
        @katPaket, N'log-5651-5661-bundle', N'5651 + 5661 Tam Uyum Paketi', N'Tek sözleşme ile çift loglama altyapısı.',
        N'5651 internet loglama ve 5661 konaklama loglama birlikte. 5661 kurulu otellerde 5661 bileşeni atlanır; fiyatlandırma admin onayında netleştirilir.',
        3990.00, N'Aylik', 18.00, N'OTEL_5661_YOK',
        N'/assets/img/platform-paketleri/bundle-5651-5661.svg',
        N'["/assets/img/platform-paketleri/bundle-5651-5661.svg"]',
        N'["5651 + 5661 kurulum","Tek fatura","Öncelikli destek","Partner komisyon %18"]',
        N'/sozlesmeler/platform-paket-bundle', N'Yayinda', 30
    );
END

-- Admin RBAC
IF OBJECT_ID(N'dbo.ADMIN_YETKILER', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM [dbo].[ADMIN_YETKILER] WHERE [YETKI_CODE] = N'admin.platform_packages')
    BEGIN
        INSERT INTO [dbo].[ADMIN_YETKILER] ([YETKI_CODE], [YETKI_NAME], [GROUP_CODE], [DESCRIPTION], [ACTIVE])
        VALUES (N'admin.platform_packages', N'Platform Paket Satışı', N'commerce', N'5651/5661 paket kataloğu ve partner başvuruları', 1);
    END
END
