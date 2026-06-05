SET NOCOUNT ON;

UPDATE dbo.SSS_KATEGORILERI
SET KATEGORI_ADI = N'Ödeme',
    GUNCELLENME_TARIHI = SYSUTCDATETIME()
WHERE SEO_SLUG = N'odeme';

UPDATE dbo.SSS_KATEGORILERI
SET KATEGORI_ADI = N'Hesap ve Güvenlik',
    GUNCELLENME_TARIHI = SYSUTCDATETIME()
WHERE SEO_SLUG = N'hesap-ve-guvenlik';

UPDATE dbo.SSS_KATEGORILERI
SET KATEGORI_ADI = N'Firma İşlemleri',
    GUNCELLENME_TARIHI = SYSUTCDATETIME()
WHERE SEO_SLUG = N'firma-islemleri';

UPDATE dbo.SSS_SORULARI
SET SORU = N'Rezervasyonumu nasıl iptal edebilirim?',
    CEVAP = N'Kullanıcı panelindeki rezervasyon detayından, check-in tarihi gelmeden iptal talebi oluşturabilirsiniz. Otel politikası ve ücretsiz iptal süresi rezervasyon kartında görünür.',
    GUNCELLENME_TARIHI = SYSUTCDATETIME()
WHERE id = 1;

UPDATE dbo.SSS_SORULARI
SET SORU = N'Rezervasyon güncellemesinde fiyat değişir mi?',
    CEVAP = N'Evet. Tarih, oda veya kişi sayısı değişirse geçerli güncel fiyatlar yeniden hesaplanır ve taraflara bildirilir.',
    GUNCELLENME_TARIHI = SYSUTCDATETIME()
WHERE id = 2;

UPDATE dbo.SSS_SORULARI
SET SORU = N'Kapıda ödeme ile online ödeme arasındaki fark nedir?',
    CEVAP = N'Kapıda ödeme tesis tarafından check-in sırasında tahsil edilir; online ödeme ise rezervasyon sırasında veya ön ödeme akışında tamamlanır.',
    GUNCELLENME_TARIHI = SYSUTCDATETIME()
WHERE id = 3;

UPDATE dbo.SSS_SORULARI
SET SORU = N'İade süreci ne kadar sürer?',
    CEVAP = N'İade süresi kullanılan ödeme yöntemine ve sağlayıcıya göre değişir; durum panel ve bildirim ekranlarında izlenir.',
    GUNCELLENME_TARIHI = SYSUTCDATETIME()
WHERE id = 4;

UPDATE dbo.SSS_SORULARI
SET SORU = N'E-posta doğrulaması neden zorunlu?',
    CEVAP = N'Hesap güvenliği, şifre sıfırlama ve rezervasyon bildirimlerinin doğru kişiye ulaşması için zorunludur.',
    GUNCELLENME_TARIHI = SYSUTCDATETIME()
WHERE id = 5;

UPDATE dbo.SSS_SORULARI
SET SORU = N'İki aşamalı doğrulama nasıl çalışır?',
    CEVAP = N'Giriş sırasında tek kullanımlık kod e-posta kanalıyla gönderilir; doğrulama tamamlanmadan yeni oturum açılmaz.',
    GUNCELLENME_TARIHI = SYSUTCDATETIME()
WHERE id = 6;

UPDATE dbo.SSS_SORULARI
SET SORU = N'Partner panelinde hangi işlemler yapılabilir?',
    CEVAP = N'Oda, fiyat, kampanya, görsel, rezervasyon ve finans süreçleri partner panelinden yönetilebilir.',
    GUNCELLENME_TARIHI = SYSUTCDATETIME()
WHERE id = 7;

UPDATE dbo.SSS_SORULARI
SET SORU = N'Belge onayı olmadan otel yayına açılır mı?',
    CEVAP = N'Hayır. Gerekli evraklar admin onayı almadan tesis yayınlanmaz.',
    GUNCELLENME_TARIHI = SYSUTCDATETIME()
WHERE id = 8;

UPDATE dbo.SSS_SORULARI
SET SORU = N'Firma rezervasyonunda personel atamak zorunlu mu?',
    CEVAP = N'Hayır; ancak atanırsa partner otel detayda kimin hangi odada kalacağını görebilir.',
    GUNCELLENME_TARIHI = SYSUTCDATETIME()
WHERE id = 9;

UPDATE dbo.SSS_SORULARI
SET SORU = N'Firma faturaları nerede görüntülenir?',
    CEVAP = N'Firma panelindeki faturalar alanında konaklama ve tahsilat belgeleri listelenir.',
    GUNCELLENME_TARIHI = SYSUTCDATETIME()
WHERE id = 10;

UPDATE dbo.SSS_SORULARI
SET SORU = N'Favori fiyat alarmı nasıl çalışır?',
    CEVAP = N'Kullanıcı hedef fiyat belirler; partner fiyatı bu seviyeye indiğinde uygun şablon ile e-posta bildirimi gönderilir.',
    GUNCELLENME_TARIHI = SYSUTCDATETIME()
WHERE id = 11;

UPDATE dbo.SSS_SORULARI
SET SORU = N'Kampanya bitince fiyat ne olur?',
    CEVAP = N'Kampanya bitiminde fiyat normal satış kuralına döner ve vitrin görünürlüğü otomatik sona erer.',
    GUNCELLENME_TARIHI = SYSUTCDATETIME()
WHERE id = 12;
