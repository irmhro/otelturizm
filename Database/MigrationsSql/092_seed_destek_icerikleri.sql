INSERT INTO [destek_kategorileri] ([kategori_adi], [seo_slug], [kategori_ikon], [kisa_aciklama], [renk_kodu], [siralama], [durum])
SELECT * FROM (
    SELECT 'Rezervasyonlar' AS kategori_adi, 'rezervasyonlar' AS seo_slug, 'fa-calendar-check' AS kategori_ikon, 'Değişiklik, iptal, onay' AS kisa_aciklama, '#00A86B' AS renk_kodu, 1 AS siralama, 1 AS durum
    UNION ALL SELECT 'Ödemeler', 'odemeler', 'fa-credit-card', 'İade, fatura, ödeme yöntemleri', '#003B95', 2, 1
    UNION ALL SELECT 'Hesabım', 'hesabim', 'fa-user-circle', 'Giriş, profil, güvenlik', '#FFB800', 3, 1
    UNION ALL SELECT 'Otel Bilgileri', 'otel-bilgileri', 'fa-hotel', 'Olanaklar, konum, iletişim', '#FF385C', 4, 1
) AS kaynak
WHERE NOT EXISTS (
    SELECT 1 FROM [destek_kategorileri] dk WHERE dk.[seo_slug] = kaynak.[seo_slug]
);

INSERT INTO [destek_makaleleri] ([destek_kategori_id], [baslik], [seo_slug], [ozet], [icerik], [ikon], [one_cikan_mi], [yardim_merkezinde_goster], [siralama], [durum])
SELECT dk.id, kaynak.baslik, kaynak.seo_slug, kaynak.ozet, kaynak.icerik, kaynak.ikon, kaynak.one_cikan_mi, 1, kaynak.siralama, 1
FROM (
    SELECT 'rezervasyonlar' AS kategori_slug, 'Rezervasyonumu nasıl iptal ederim?' AS baslik, 'rezervasyonumu-nasil-iptal-ederim' AS seo_slug, 'Ücretsiz iptal koşulları ve adım adım rehber.' AS ozet, 'Rezervasyonlarım ekranından rezervasyon detayına girerek iptal talebi oluşturabilirsiniz. Ücretsiz iptal hakkı, otelin iptal politikasına göre değişir.' AS icerik, 'fa-ban' AS ikon, 1 AS one_cikan_mi, 1 AS siralama
    UNION ALL SELECT 'odemeler', 'İade ücretim ne zaman hesabıma yatar?', 'iade-ucretim-ne-zaman-hesabima-yatar', 'İade süreci ve banka işlem süreleri.', 'İade onaylandıktan sonra ödemeniz bankanıza bağlı olarak genellikle 3-5 iş günü içinde hesabınıza yansır.', 'fa-rotate-left', 1, 2
    UNION ALL SELECT 'rezervasyonlar', 'Rezervasyon tarihlerimi değiştirebilir miyim?', 'rezervasyon-tarihlerimi-degistirebilir-miyim', 'Tarih değişikliği ve fiyat farkı hakkında.', 'Rezervasyon detay sayfası üzerinden tarih değişikliği talep edebilirsiniz. Yeni tarihe göre fiyat farkı oluşabilir.', 'fa-pen', 1, 3
    UNION ALL SELECT 'otel-bilgileri', 'Çocuk politikası nedir?', 'cocuk-politikasi-nedir', 'Yaş sınırları ve ek yatak ücretleri.', 'Her otelin çocuk ve ek yatak politikası farklıdır. Bu bilgi otel detayında ve rezervasyon özetinde gösterilir.', 'fa-child', 1, 4
    UNION ALL SELECT 'otel-bilgileri', 'Evcil hayvan kabul ediliyor mu?', 'evcil-hayvan-kabul-ediliyor-mu', 'Evcil hayvan dostu oteller ve kurallar.', 'Evcil hayvan kabul politikası otelden otele değişir. Filtrelerden evcil hayvan dostu otelleri seçebilirsiniz.', 'fa-paw', 1, 5
    UNION ALL SELECT 'hesabim', 'Rezervasyonum güvende mi?', 'rezervasyonum-guvende-mi', 'TÜRSAB ve PCI DSS güvencesi.', 'Otelturizm güvenli ödeme altyapısı, kayıt doğrulama ve işlem logları ile rezervasyon sürecini korur.', 'fa-shield-alt', 1, 6
) AS kaynak
INNER JOIN [destek_kategorileri] dk ON dk.[seo_slug] = kaynak.[kategori_slug]
WHERE NOT EXISTS (
    SELECT 1 FROM [destek_makaleleri] dm WHERE dm.[seo_slug] = kaynak.[seo_slug]
);

INSERT INTO [destek_kanallari] ([kanal_adi], [kanal_turu], [ikon], [aciklama], [buton_metin], [baglanti_url], [ek_bilgi], [renk_tonu], [siralama], [aktif_mi])
SELECT * FROM (
    SELECT 'Canlı Destek' AS kanal_adi, 'canli_destek' AS kanal_turu, 'fa-comment-dots' AS ikon, '7/24 canlı destek ekibimizle anında yazışın.' AS aciklama, 'Sohbet Başlat' AS buton_metin, '/panel/user/mesajlarim' AS baglanti_url, 'Ortalama yanıt süresi: 2 dk' AS ek_bilgi, 'primary' AS renk_tonu, 1 AS siralama, 1 AS aktif_mi
    UNION ALL SELECT 'E-posta Desteği', 'eposta', 'fa-envelope', 'Detaylı sorularınız için e-posta gönderin.', 'Mesaj Gönder', 'mailto:destek@otelturizm.com', '24 saat içinde dönüş', 'outline', 2, 1
    UNION ALL SELECT 'Telefon Desteği', 'telefon', 'fa-phone-alt', 'Acil durumlar için 7/24 telefon hattı.', '0850 123 45 67', 'tel:08501234567', 'Ücretsiz hat', 'outline', 3, 1
) AS kaynak
WHERE NOT EXISTS (
    SELECT 1 FROM [destek_kanallari] dk WHERE dk.[kanal_adi] = kaynak.[kanal_adi]
);

INSERT INTO [sss_kategorileri] ([kategori_adi], [seo_slug], [ikon], [siralama], [aktif_mi])
SELECT * FROM (
    SELECT 'Rezervasyon' AS kategori_adi, 'rezervasyon' AS seo_slug, 'fa-calendar-check' AS ikon, 1 AS siralama, 1 AS aktif_mi
    UNION ALL SELECT 'Ödeme', 'odeme', 'fa-credit-card', 2, 1
    UNION ALL SELECT 'İptal & İade', 'iptal-iade', 'fa-ban', 3, 1
    UNION ALL SELECT 'Hesap', 'hesap', 'fa-user-circle', 4, 1
) AS kaynak
WHERE NOT EXISTS (
    SELECT 1 FROM [sss_kategorileri] sk WHERE sk.[seo_slug] = kaynak.[seo_slug]
);

INSERT INTO [sss_sorulari] ([sss_kategori_id], [soru], [cevap], [one_cikan_mi], [siralama], [aktif_mi])
SELECT sk.id, kaynak.soru, kaynak.cevap, kaynak.one_cikan_mi, kaynak.siralama, 1
FROM (
    SELECT 'rezervasyon' AS kategori_slug, 'Rezervasyonumu nasıl yapabilirim?' AS soru, 'Otel sayfasında tarih ve misafir bilgilerini seçip rezervasyon adımlarını tamamlayarak kolayca rezervasyon yapabilirsiniz.' AS cevap, 1 AS one_cikan_mi, 1 AS siralama
    UNION ALL SELECT 'rezervasyon', 'Rezervasyon onayımı nasıl alırım?', 'Rezervasyon tamamlandığında e-posta adresinize onay mesajı ve rezervasyon numaranız gönderilir.', 1, 2
    UNION ALL SELECT 'rezervasyon', 'Rezervasyonumu değiştirebilir miyim?', 'Rezervasyonlarım sayfasından ilgili rezervasyonu açarak değişiklik talebinde bulunabilirsiniz.', 1, 3
    UNION ALL SELECT 'odeme', 'Hangi ödeme yöntemlerini kabul ediyorsunuz?', 'Visa, Mastercard, American Express ve Troy kartları desteklenir. Güvenli ödeme için 3D Secure kullanılabilir.', 1, 4
    UNION ALL SELECT 'odeme', 'Ödeme ne zaman alınır?', 'Ödeme koşulu otele göre değişir; bazı tesisler anında tahsilat yaparken bazıları girişte ödeme alır.', 0, 5
    UNION ALL SELECT 'iptal-iade', 'Rezervasyonumu ücretsiz iptal edebilir miyim?', 'Ücretsiz iptal hakkı otelin rezervasyon politikalarına göre değişir. Rezervasyon detay sayfasında gösterilir.', 1, 6
    UNION ALL SELECT 'iptal-iade', 'İade ücretim ne zaman hesabıma yatar?', 'İade işlemi onaylandıktan sonra 3-5 iş günü içinde bankanıza bağlı olarak hesabınıza yansır.', 1, 7
    UNION ALL SELECT 'hesap', 'Hesabıma giriş yapamıyorum, ne yapmalıyım?', 'Şifre sıfırlama bağlantısını kullanabilir veya destek ekibimize ulaşabilirsiniz.', 0, 8
) AS kaynak
INNER JOIN [sss_kategorileri] sk ON sk.[seo_slug] = kaynak.[kategori_slug]
WHERE NOT EXISTS (
    SELECT 1 FROM [sss_sorulari] ss
    WHERE ss.[sss_kategori_id] = sk.[id]
      AND ss.[soru] = kaynak.[soru]
);
