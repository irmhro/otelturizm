/*
  Admin olay bazlı e-posta yönlendirme + şablon (partner/firma kayıt bildirimi dahil).
*/

IF OBJECT_ID(N'dbo.admin_eposta_yonlendirme', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.admin_eposta_yonlendirme
    (
        id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_admin_eposta_yonlendirme PRIMARY KEY,
        olay_kodu NVARCHAR(64) NOT NULL CONSTRAINT UQ_admin_eposta_yonlendirme_kod UNIQUE,
        baslik NVARCHAR(200) NOT NULL,
        aciklama NVARCHAR(500) NULL,
        hedef_epostalar NVARCHAR(2000) NOT NULL CONSTRAINT DF_admin_eposta_hedef DEFAULT (N''),
        aktif_mi BIT NOT NULL CONSTRAINT DF_admin_eposta_aktif DEFAULT (1),
        guncellenme_utc DATETIME2(0) NOT NULL CONSTRAINT DF_admin_eposta_guncelle DEFAULT (SYSUTCDATETIME())
    );
    CREATE INDEX IX_admin_eposta_aktif ON dbo.admin_eposta_yonlendirme(aktif_mi, olay_kodu);
END;

DECLARE @def NVARCHAR(320) = N'irmhro0@gmail.com';

MERGE dbo.admin_eposta_yonlendirme AS t
USING (VALUES
    (N'partner_kayit',           N'Partner kaydı (web)',                    N'Yeni partner/taslak otel başvurusu tamamlandığında.', @def),
    (N'firma_kayit',            N'Firma başvurusu (web)',                  N'Yeni kurumsal firma başvurusu alındığında.', @def),
    (N'kullanici_kayit',        N'Üye kaydı (bireysel)',                   N'Yeni son kullanıcı hesabı oluşturulduğunda (isteğe bağlı).', @def),
    (N'rezervasyon_yonetim',   N'Rezervasyon — yönetim özeti',             N'Rezervasyon oluşturma / kritik durum (şablona bağlanır).', @def),
    (N'odeme_uyari',            N'Ödeme / tahsilat uyarıları',             N'Ödeme gecikmesi veya başarısız işlem bildirimi.', @def),
    (N'sikayet',                N'Şikayet / uyuşmazlık',                     N'Müşteri veya tesis şikayeti kaydı.', @def),
    (N'gelistirme_talebi',      N'Geliştirme / talep',                     N'Ürün geliştirme veya iş talebi (panel).', @def),
    (N'destek_talebi',          N'Destek talebi',                          N'Destek kanalından gelen talepler.', @def),
    (N'bildirim_kritik',        N'Kritik sistem bildirimi',                N'Sistem sağlığı, kuyruk veya güvenlik uyarıları.', @def),
    (N'firma_limit',            N'Firma limit / harcama',                  N'Kurumsal harcama limitine yaklaşım veya aşım.', @def),
    (N'partner_evrak',          N'Partner evrak / uyumluluk',              N'Partner evrak yükleme veya uyumluluk hatırlatması.', @def),
    (N'kvkk_basvuru',           N'KVKK başvurusu',                         N'Kişisel veri başvurusu veya silme talebi.', @def)
) AS s (olay_kodu, baslik, aciklama, hedef_epostalar)
ON t.olay_kodu = s.olay_kodu
WHEN MATCHED THEN UPDATE SET baslik = s.baslik, aciklama = s.aciklama
WHEN NOT MATCHED THEN INSERT (olay_kodu, baslik, aciklama, hedef_epostalar, aktif_mi, guncellenme_utc)
VALUES (s.olay_kodu, s.baslik, s.aciklama, s.hedef_epostalar, 1, SYSUTCDATETIME());

IF NOT EXISTS (SELECT 1 FROM dbo.bildirim_sablonlari WHERE sablon_kodu = N'admin_routing_notice' AND tur = N'E-posta' AND dil = N'tr')
BEGIN
    INSERT INTO dbo.bildirim_sablonlari
    (sablon_kodu, sablon_adi, tur, dil, konu, baslik, icerik, degiskenler, aktif_mi, olusturulma_tarihi)
    VALUES
    (
        N'admin_routing_notice',
        N'Admin olay bildirimi',
        N'E-posta',
        N'tr',
        N'{{email_subject}}',
        N'Admin bildirimi',
        N'Views/Email/tr/Admin Routing Bildirimi.cshtml',
        N'email_subject,badge,title,intro,detail_html,primary_url,primary_label,event_code,occurred_at',
        1,
        SYSUTCDATETIME()
    );
END
ELSE
BEGIN
    UPDATE dbo.bildirim_sablonlari
    SET konu = N'{{email_subject}}',
        baslik = N'Admin bildirimi',
        icerik = N'Views/Email/tr/Admin Routing Bildirimi.cshtml',
        aktif_mi = 1
    WHERE sablon_kodu = N'admin_routing_notice' AND tur = N'E-posta' AND dil = N'tr';
END
