/* Departman panel rol hazırlığı.
   Kalıcı kullanıcı oluşturma bu scriptte yoktur; kullanıcı seed işlemi işlem anında açık onayla çalıştırılmalıdır. */

IF OBJECT_ID('dbo.roles', 'U') IS NOT NULL
BEGIN
    INSERT INTO dbo.roles (rol_kodu, rol_adi, aciklama, aktif_mi, olusturulma_tarihi)
    SELECT v.rol_kodu, v.rol_adi, v.aciklama, 1, SYSUTCDATETIME()
    FROM (VALUES
        ('departman_kullanici', 'Kullanıcı Departmanı', 'Kullanıcı rezervasyon, yorum, profil ve bildirim operasyonları'),
        ('departman_partner', 'Partner Departmanı', 'Partner başvuru, otel, evrak ve yayın operasyonları'),
        ('departman_firma', 'Firma Departmanı', 'Firma başvuru, kurumsal rezervasyon ve fatura operasyonları'),
        ('departman_satis', 'Satış Departmanı', 'Satış müşteri, ciro ve rezervasyon operasyonları'),
        ('departman_muhasebe', 'Muhasebe Departmanı', 'Komisyon, mutabakat, vergi ve fatura operasyonları'),
        ('departman_destek', 'Destek Departmanı', 'Destek talebi, mesaj ve sistem sağlığı operasyonları')
    ) v(rol_kodu, rol_adi, aciklama)
    WHERE NOT EXISTS (SELECT 1 FROM dbo.roles r WHERE r.rol_kodu = v.rol_kodu);
END
