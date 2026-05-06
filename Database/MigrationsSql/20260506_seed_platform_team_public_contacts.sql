SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;

IF OBJECT_ID(N'dbo.platform_ekip_uyeleri', N'U') IS NOT NULL
BEGIN
    DELETE FROM dbo.platform_ekip_uyeleri
    WHERE eposta IN (
        N'irmhro0@gmail.com',
        N'ik.yonetici@otelturizm.com',
        N'hukuk.musaviri@otelturizm.com',
        N'dba@otelturizm.com',
        N'yazilim.uzmani@otelturizm.com',
        N'teknik.lider@otelturizm.com',
        N'pazarlama.yonetici@otelturizm.com',
        N'destek.uzmani@otelturizm.com',
        N'destek.yonetici@otelturizm.com',
        N'operasyon.yonetici@otelturizm.com',
        N'muhasebe.uzmani@otelturizm.com',
        N'finans.yonetici@otelturizm.com',
        N'genelmudur@otelturizm.com'
    )
    OR ad_soyad IN (
        N'Demo Admin',
        N'İK Yöneticisi',
        N'Hukuk Müşaviri',
        N'Veritabanı Yöneticisi',
        N'Yazılım Uzmanı',
        N'Teknik Lider',
        N'Pazarlama Yöneticisi',
        N'Destek Uzmanı',
        N'Destek Yöneticisi',
        N'Operasyon Yöneticisi',
        N'Muhasebe Uzmanı',
        N'Finans Yöneticisi',
        N'Genel Müdür'
    );

    MERGE dbo.platform_ekip_uyeleri AS target
    USING (VALUES
        (1,  N'Deniz Aksoy',   N'Platform yönetimi',      N'info+admin@otelturizm.com',              N'Platform yönetimi',       N'https://ui-avatars.com/api/?name=Deniz+Aksoy&size=160&background=0b57d0&color=ffffff&bold=true&format=png'),
        (2,  N'İrem Yalçın',   N'İnsan kaynakları',       N'info+ik.yonetici@otelturizm.com',        N'İnsan kaynakları',        N'https://ui-avatars.com/api/?name=Irem+Yalcin&size=160&background=0b57d0&color=ffffff&bold=true&format=png'),
        (3,  N'Hande Mert',    N'Hukuk & uyum',           N'info+hukuk.musaviri@otelturizm.com',     N'Hukuk ve uyum',           N'https://ui-avatars.com/api/?name=Hande+Mert&size=160&background=0b57d0&color=ffffff&bold=true&format=png'),
        (4,  N'Volkan Yıldız', N'Veri & altyapı',         N'info+dba@otelturizm.com',                N'Veri ve altyapı',         N'https://ui-avatars.com/api/?name=Volkan+Yildiz&size=160&background=0b57d0&color=ffffff&bold=true&format=png'),
        (5,  N'Yiğit Uslu',    N'Uygulama geliştirme',    N'info+yazilim.uzmani@otelturizm.com',     N'Uygulama geliştirme',     N'https://ui-avatars.com/api/?name=Yigit+Uslu&size=160&background=0b57d0&color=ffffff&bold=true&format=png'),
        (6,  N'Tolga Levent',  N'Mimari & kod kalitesi',  N'info+teknik.lider@otelturizm.com',       N'Mimari ve kod kalitesi',  N'https://ui-avatars.com/api/?name=Tolga+Levent&size=160&background=0b57d0&color=ffffff&bold=true&format=png'),
        (7,  N'Pelin Yaman',   N'Büyüme & kampanya',      N'info+pazarlama.yonetici@otelturizm.com', N'Büyüme ve kampanya',      N'https://ui-avatars.com/api/?name=Pelin+Yaman&size=160&background=0b57d0&color=ffffff&bold=true&format=png'),
        (8,  N'Derya Uçar',    N'Müşteri destek',         N'info+destek.uzmani@otelturizm.com',      N'Müşteri destek',          N'https://ui-avatars.com/api/?name=Derya+Ucar&size=160&background=0b57d0&color=ffffff&bold=true&format=png'),
        (9,  N'Defne Yıldırım',N'Destek operasyonları',   N'info+destek.yonetici@otelturizm.com',    N'Destek operasyonları',    N'https://ui-avatars.com/api/?name=Defne+Yildirim&size=160&background=0b57d0&color=ffffff&bold=true&format=png'),
        (10, N'Okan Yalın',    N'Operasyon & SLA',        N'info+operasyon.yonetici@otelturizm.com', N'Operasyon ve SLA',        N'https://ui-avatars.com/api/?name=Okan+Yalin&size=160&background=0b57d0&color=ffffff&bold=true&format=png'),
        (11, N'Merve Uzun',    N'Muhasebe işlemleri',     N'info+muhasebe.uzmani@otelturizm.com',    N'Muhasebe işlemleri',      N'https://ui-avatars.com/api/?name=Merve+Uzun&size=160&background=0b57d0&color=ffffff&bold=true&format=png'),
        (12, N'Faruk Yılmaz',  N'Finans & tahsilat',      N'info+finans.yonetici@otelturizm.com',    N'Finans ve tahsilat',      N'https://ui-avatars.com/api/?name=Faruk+Yilmaz&size=160&background=0b57d0&color=ffffff&bold=true&format=png'),
        (13, N'Gökhan Mutlu',  N'Yönetim',                N'info+genelmudur@otelturizm.com',         N'Yönetim',                 N'https://ui-avatars.com/api/?name=Gokhan+Mutlu&size=160&background=0b57d0&color=ffffff&bold=true&format=png')
    ) AS source(siralama, ad_soyad, unvan, eposta, aciklama, avatar_url)
    ON target.eposta = source.eposta
    WHEN MATCHED THEN
        UPDATE SET
            ad_soyad = source.ad_soyad,
            unvan = source.unvan,
            aciklama = source.aciklama,
            avatar_url = source.avatar_url,
            siralama = source.siralama,
            aktif_mi = 1,
            guncellenme_tarihi = SYSUTCDATETIME()
    WHEN NOT MATCHED THEN
        INSERT (ad_soyad, unvan, eposta, aciklama, avatar_url, siralama, aktif_mi, olusturulma_tarihi)
        VALUES (source.ad_soyad, source.unvan, source.eposta, source.aciklama, source.avatar_url, source.siralama, 1, SYSUTCDATETIME());
END;

