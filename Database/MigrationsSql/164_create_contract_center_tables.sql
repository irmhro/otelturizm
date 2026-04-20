IF OBJECT_ID('dbo.sozlesmeler', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.sozlesmeler
    (
        id BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        hedef_kitle NVARCHAR(30) NOT NULL,
        sozlesme_tipi NVARCHAR(30) NOT NULL,
        baslik NVARCHAR(200) NOT NULL,
        alt_baslik NVARCHAR(300) NULL,
        slug NVARCHAR(200) NOT NULL,
        ozet_html NVARCHAR(MAX) NULL,
        icerik_html NVARCHAR(MAX) NOT NULL,
        gorsel_url NVARCHAR(500) NULL,
        sozlesme_linki NVARCHAR(500) NULL,
        versiyon_no INT NOT NULL CONSTRAINT DF_sozlesmeler_versiyon DEFAULT 1,
        baslangic_tarihi DATETIME2 NOT NULL CONSTRAINT DF_sozlesmeler_baslangic DEFAULT SYSUTCDATETIME(),
        bitis_tarihi DATETIME2 NULL,
        kabul_gerektirir_mi BIT NOT NULL CONSTRAINT DF_sozlesmeler_kabul DEFAULT 1,
        email_dogrulamada_gonder BIT NOT NULL CONSTRAINT DF_sozlesmeler_email DEFAULT 1,
        yenileme_gerekir_mi BIT NOT NULL CONSTRAINT DF_sozlesmeler_yenileme DEFAULT 0,
        yenileme_periyodu_gun INT NULL,
        aktif_mi BIT NOT NULL CONSTRAINT DF_sozlesmeler_aktif DEFAULT 1,
        notlar NVARCHAR(1000) NULL,
        olusturan_kullanici_id BIGINT NULL,
        guncelleyen_kullanici_id BIGINT NULL,
        olusturulma_tarihi DATETIME2 NOT NULL CONSTRAINT DF_sozlesmeler_olustur DEFAULT SYSUTCDATETIME(),
        guncellenme_tarihi DATETIME2 NOT NULL CONSTRAINT DF_sozlesmeler_guncel DEFAULT SYSUTCDATETIME()
    );
    CREATE UNIQUE INDEX UX_sozlesmeler_slug_versiyon ON dbo.sozlesmeler(slug, versiyon_no);
    CREATE INDEX IX_sozlesmeler_hedef ON dbo.sozlesmeler(hedef_kitle, sozlesme_tipi, aktif_mi);
END;

IF OBJECT_ID('dbo.sozlesme_kabulleri', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.sozlesme_kabulleri
    (
        id BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        sozlesme_id BIGINT NOT NULL,
        kabul_eden_tip NVARCHAR(30) NOT NULL,
        kullanici_id BIGINT NOT NULL,
        partner_id BIGINT NULL,
        firma_id BIGINT NULL,
        alici_eposta NVARCHAR(255) NOT NULL,
        sozlesme_baslik_snapshot NVARCHAR(200) NOT NULL,
        sozlesme_versiyon_snapshot INT NOT NULL,
        kabul_kaynagi NVARCHAR(80) NOT NULL,
        kabul_ip NVARCHAR(80) NULL,
        kabul_user_agent NVARCHAR(500) NULL,
        eposta_dogrulandi_mi BIT NOT NULL CONSTRAINT DF_sozlesme_kabulleri_eposta DEFAULT 0,
        eposta_dogrulama_tarihi DATETIME2 NULL,
        kabul_tarihi DATETIME2 NOT NULL CONSTRAINT DF_sozlesme_kabulleri_tarih DEFAULT SYSUTCDATETIME(),
        sona_erme_tarihi DATETIME2 NULL,
        durum NVARCHAR(40) NOT NULL CONSTRAINT DF_sozlesme_kabulleri_durum DEFAULT 'KabulEdildi'
    );
    ALTER TABLE dbo.sozlesme_kabulleri ADD CONSTRAINT FK_sozlesme_kabulleri_sozlesmeler FOREIGN KEY (sozlesme_id) REFERENCES dbo.sozlesmeler(id);
    ALTER TABLE dbo.sozlesme_kabulleri ADD CONSTRAINT FK_sozlesme_kabulleri_users FOREIGN KEY (kullanici_id) REFERENCES dbo.users(id);
    CREATE INDEX IX_sozlesme_kabulleri_user ON dbo.sozlesme_kabulleri(kullanici_id, sozlesme_id);
    CREATE INDEX IX_sozlesme_kabulleri_partner ON dbo.sozlesme_kabulleri(partner_id);
    CREATE INDEX IX_sozlesme_kabulleri_firma ON dbo.sozlesme_kabulleri(firma_id);
END;

IF OBJECT_ID('dbo.sozlesme_gonderim_loglari', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.sozlesme_gonderim_loglari
    (
        id BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        sozlesme_id BIGINT NOT NULL,
        kullanici_id BIGINT NOT NULL,
        partner_id BIGINT NULL,
        firma_id BIGINT NULL,
        alici_eposta NVARCHAR(255) NOT NULL,
        gonderim_nedeni NVARCHAR(80) NOT NULL,
        bildirim_log_id BIGINT NULL,
        konu_snapshot NVARCHAR(255) NOT NULL,
        icerik_snapshot NVARCHAR(MAX) NULL,
        durum NVARCHAR(40) NOT NULL CONSTRAINT DF_sozlesme_gonderim_durum DEFAULT 'KuyrugaAlindi',
        gonderim_tarihi DATETIME2 NOT NULL CONSTRAINT DF_sozlesme_gonderim_tarih DEFAULT SYSUTCDATETIME(),
        ip_adresi NVARCHAR(80) NULL,
        user_agent NVARCHAR(500) NULL,
        olusturan_admin_id BIGINT NULL
    );
    ALTER TABLE dbo.sozlesme_gonderim_loglari ADD CONSTRAINT FK_sozlesme_gonderim_sozlesmeler FOREIGN KEY (sozlesme_id) REFERENCES dbo.sozlesmeler(id);
    ALTER TABLE dbo.sozlesme_gonderim_loglari ADD CONSTRAINT FK_sozlesme_gonderim_users FOREIGN KEY (kullanici_id) REFERENCES dbo.users(id);
    CREATE INDEX IX_sozlesme_gonderim_loglari_eposta ON dbo.sozlesme_gonderim_loglari(alici_eposta, gonderim_tarihi DESC);
END;

IF NOT EXISTS (SELECT 1 FROM dbo.bildirim_sablonlari WHERE sablon_kodu = 'contract_delivery')
BEGIN
    INSERT INTO dbo.bildirim_sablonlari
    (sablon_kodu, sablon_adi, tur, dil, konu, baslik, icerik, degiskenler, aktif_mi, olusturulma_tarihi)
    VALUES
    (
        'contract_delivery',
        'Sözleşme Bildirimi',
        'E-posta',
        'tr',
        '{{contract_bundle_title}}',
        'Sözleşme Paketi',
        'Views/Email/Sozlesme Bildirimi.cshtml',
        '{{recipient_name}},{{module_label}},{{contract_bundle_title}},{{contract_sections_html}},{{primary_contract_url}}',
        1,
        SYSUTCDATETIME()
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.sozlesmeler WHERE slug = 'kullanici-kullanim-kosullari' AND versiyon_no = 1)
BEGIN
    INSERT INTO dbo.sozlesmeler
    (hedef_kitle, sozlesme_tipi, baslik, alt_baslik, slug, ozet_html, icerik_html, versiyon_no, baslangic_tarihi, kabul_gerektirir_mi, email_dogrulamada_gonder, aktif_mi, notlar, olusturulma_tarihi, guncellenme_tarihi)
    VALUES
    ('user','agreement','Kullanıcı Kullanım Koşulları','Otelturizm bireysel kullanıcı üyeliği ve rezervasyon kullanım kuralları.','kullanici-kullanim-kosullari',
     '<p>Platform üyeliği, rezervasyon, iptal, iletişim ve hesap güvenliği süreçlerinde geçerli ana kullanıcı sözleşmesidir.</p>',
     '<h2>1. Taraflar ve konu</h2><p>Bu sözleşme, Otelturizm platformunu kullanan bireysel kullanıcılar ile platform işletmecisi arasındaki dijital hizmet ilişkisinin esaslarını düzenler.</p><h2>2. Üyelik ve hesap güvenliği</h2><p>Kullanıcı, kayıt sırasında verdiği bilgilerin doğru olduğunu ve hesabını üçüncü kişilerle paylaşmayacağını kabul eder.</p><h2>3. Rezervasyon ve ödeme</h2><p>Rezervasyon sırasında gösterilen fiyat, vergi ve ek hizmetler kullanıcıya açık şekilde sunulur. Platform, ilgili otel kurallarını kullanıcıya görünür biçimde iletir.</p><h2>4. İptal ve değişiklik</h2><p>İptal koşulları rezervasyon detayında ayrıca gösterilir. Kullanıcı, bu koşulları rezervasyon anında kabul etmiş sayılır.</p><h2>5. Uyuşmazlık</h2><p>Türk hukuku uygulanır; yetkili merciler Türkiye Cumhuriyeti mevzuatına göre belirlenir.</p>',
     1, SYSUTCDATETIME(), 1, 1, 1, 'İlk kullanıcı sözleşmesi', SYSUTCDATETIME(), SYSUTCDATETIME());
END;

IF NOT EXISTS (SELECT 1 FROM dbo.sozlesmeler WHERE slug = 'kullanici-kvkk-aydinlatma' AND versiyon_no = 1)
BEGIN
    INSERT INTO dbo.sozlesmeler
    (hedef_kitle, sozlesme_tipi, baslik, alt_baslik, slug, ozet_html, icerik_html, versiyon_no, baslangic_tarihi, kabul_gerektirir_mi, email_dogrulamada_gonder, aktif_mi, notlar, olusturulma_tarihi, guncellenme_tarihi)
    VALUES
    ('user','kvkk','Kullanıcı KVKK Aydınlatma Metni','6698 sayılı Kanun kapsamında kullanıcı verilerinin işlenmesine dair aydınlatma metni.','kullanici-kvkk-aydinlatma',
     '<p>Kişisel verileriniz rezervasyon, destek, güvenlik ve yasal yükümlülüklerin yerine getirilmesi amacıyla işlenir.</p>',
     '<h2>1. Veri sorumlusu</h2><p>Otelturizm, 6698 sayılı Kişisel Verilerin Korunması Kanunu kapsamında veri sorumlusudur.</p><h2>2. İşlenen veriler</h2><p>Kimlik, iletişim, rezervasyon, ödeme ve kullanım güvenliği verileri işlenebilir.</p><h2>3. Amaç</h2><p>Rezervasyon süreçlerinin yürütülmesi, müşteri destek operasyonları, güvenlik ve yasal uyum amaçlarıyla kullanılır.</p><h2>4. Haklarınız</h2><p>KVKK m.11 kapsamındaki taleplerinizi platform destek kanallarından iletebilirsiniz.</p>',
     1, SYSUTCDATETIME(), 1, 1, 1, 'İlk kullanıcı KVKK metni', SYSUTCDATETIME(), SYSUTCDATETIME());
END;

IF NOT EXISTS (SELECT 1 FROM dbo.sozlesmeler WHERE slug = 'partner-basvuru-sozlesmesi' AND versiyon_no = 1)
BEGIN
    INSERT INTO dbo.sozlesmeler
    (hedef_kitle, sozlesme_tipi, baslik, alt_baslik, slug, ozet_html, icerik_html, versiyon_no, baslangic_tarihi, kabul_gerektirir_mi, email_dogrulamada_gonder, aktif_mi, notlar, olusturulma_tarihi, guncellenme_tarihi)
    VALUES
    ('partner','agreement','Partner Başvuru ve Yayın Sözleşmesi','Tesis sahipleri ve partnerler için panel, içerik ve komisyon kullanım esasları.','partner-basvuru-sozlesmesi',
     '<p>Partner hesabı açan tesisler; içerik doğruluğu, komisyon, tahsilat, yayın ve hizmet seviyelerine ilişkin kuralları kabul eder.</p>',
     '<h2>1. Başvuru süreci</h2><p>Partner, başvuru sırasında ilettiği tesis ve yetkili bilgilerinin resmi kayıtlarla uyumlu olduğunu kabul eder.</p><h2>2. Panel kullanımı</h2><p>Partner panelinde yapılan fiyat, stok, görsel ve içerik güncellemeleri kayıt altına alınır.</p><h2>3. Yayın ve görünürlük</h2><p>Admin onayı tamamlanmadan tesis kamuya açık yayına alınmaz.</p><h2>4. Komisyon ve tahsilat</h2><p>Komisyon oranları ve vergisel yükümlülükler sistemde tanımlanan güncel kurallara göre uygulanır.</p>',
     1, SYSUTCDATETIME(), 1, 1, 1, 'İlk partner sözleşmesi', SYSUTCDATETIME(), SYSUTCDATETIME());
END;

IF NOT EXISTS (SELECT 1 FROM dbo.sozlesmeler WHERE slug = 'partner-kvkk-aydinlatma' AND versiyon_no = 1)
BEGIN
    INSERT INTO dbo.sozlesmeler
    (hedef_kitle, sozlesme_tipi, baslik, alt_baslik, slug, ozet_html, icerik_html, versiyon_no, baslangic_tarihi, kabul_gerektirir_mi, email_dogrulamada_gonder, aktif_mi, notlar, olusturulma_tarihi, guncellenme_tarihi)
    VALUES
    ('partner','kvkk','Partner KVKK Aydınlatma Metni','Partner başvuru ve panel kullanım verilerinin işlenmesine dair aydınlatma metni.','partner-kvkk-aydinlatma',
     '<p>Başvuru belgeleri, yetkili kişi bilgileri ve operasyonel kayıtlar KVKK çerçevesinde işlenir.</p>',
     '<h2>1. Kapsam</h2><p>Başvuru sırasında yüklenen evraklar, banka bilgileri ve iletişim kayıtları bu metin kapsamında değerlendirilir.</p><h2>2. İşleme amacı</h2><p>Doğrulama, onay, operasyon, muhasebe ve yasal yükümlülüklerin yerine getirilmesi amaçlanır.</p><h2>3. Saklama süresi</h2><p>Mevzuata uygun süreler boyunca saklanır ve gerekli olduğunda güncellenir.</p>',
     1, SYSUTCDATETIME(), 1, 1, 1, 'İlk partner KVKK metni', SYSUTCDATETIME(), SYSUTCDATETIME());
END;

IF NOT EXISTS (SELECT 1 FROM dbo.sozlesmeler WHERE slug = 'firma-kurumsal-kullanim-kosullari' AND versiyon_no = 1)
BEGIN
    INSERT INTO dbo.sozlesmeler
    (hedef_kitle, sozlesme_tipi, baslik, alt_baslik, slug, ozet_html, icerik_html, versiyon_no, baslangic_tarihi, kabul_gerektirir_mi, email_dogrulamada_gonder, aktif_mi, notlar, olusturulma_tarihi, guncellenme_tarihi)
    VALUES
    ('firma','agreement','Firma Sözleşmesi ve Kurumsal Kullanım Koşulları','Kurumsal hesap, çalışan yönetimi, limit ve rezervasyon süreçlerine ilişkin ana sözleşme.','firma-kurumsal-kullanim-kosullari',
     '<p>Kurumsal firmalar; çalışan rezervasyonları, limit yönetimi, onay akışları ve faturalama süreçlerinde bu sözleşme hükümlerine tabi olur.</p>',
     '<h2>1. Kurumsal hesap</h2><p>Firma hesabı, şirket adına yetkili kişi tarafından oluşturulur ve yönetilir.</p><h2>2. Çalışan rezervasyonları</h2><p>Firma paneli üzerinden yapılan rezervasyonlar iç onay limitlerine tabi olabilir.</p><h2>3. Faturalama ve ödeme</h2><p>Kurumsal fiyatlar, kampanyalar ve tahsilat akışları sistem kayıtlarına göre yürütülür.</p><h2>4. Sözleşme güncellemeleri</h2><p>Güncel sürümler admin paneli ve e-posta bildirimi ile firmalara iletilir.</p>',
     1, SYSUTCDATETIME(), 1, 1, 1, 'İlk firma sözleşmesi', SYSUTCDATETIME(), SYSUTCDATETIME());
END;

IF NOT EXISTS (SELECT 1 FROM dbo.sozlesmeler WHERE slug = 'firma-kvkk-aydinlatma' AND versiyon_no = 1)
BEGIN
    INSERT INTO dbo.sozlesmeler
    (hedef_kitle, sozlesme_tipi, baslik, alt_baslik, slug, ozet_html, icerik_html, versiyon_no, baslangic_tarihi, kabul_gerektirir_mi, email_dogrulamada_gonder, aktif_mi, notlar, olusturulma_tarihi, guncellenme_tarihi)
    VALUES
    ('firma','kvkk','Firma KVKK Aydınlatma Metni','Kurumsal başvuru ve çalışan yönetim verilerinin işlenmesine ilişkin aydınlatma metni.','firma-kvkk-aydinlatma',
     '<p>Firma ve yetkili kişi verileri, kurumsal rezervasyon operasyonlarının yürütülmesi amacıyla işlenir.</p>',
     '<h2>1. Veri kategorileri</h2><p>Firma unvanı, vergi bilgileri, yetkili ve çalışan iletişim verileri işlenebilir.</p><h2>2. İşleme amaçları</h2><p>Kurumsal fiyatlama, rezervasyon, limit ve muhasebe süreçlerinin yürütülmesi amaçlanır.</p><h2>3. Haklar</h2><p>KVKK kapsamındaki erişim, düzeltme ve silme talepleri mevzuat sınırları içinde değerlendirilir.</p>',
     1, SYSUTCDATETIME(), 1, 1, 1, 'İlk firma KVKK metni', SYSUTCDATETIME(), SYSUTCDATETIME());
END;
