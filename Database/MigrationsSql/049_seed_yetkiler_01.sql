INSERT INTO yetkiler (yetki_kodu, modul, eylem, aciklama, varsayilan_izin) VALUES
-- Otel Modülü
('otel.listele', 'Otel', 'listele', 'Otelleri listeleme', 1),
('otel.goruntule', 'Otel', 'goruntule', 'Otel detay sayfası görüntüleme', 1),
('otel.ekle', 'Otel', 'ekle', 'Yeni otel ekleme', 0),
('otel.duzenle', 'Otel', 'duzenle', 'Otel bilgilerini düzenleme', 0),
('otel.sil', 'Otel', 'sil', 'Otel silme (soft delete)', 0),
('otel.onayla', 'Otel', 'onayla', 'Otel yayına alma onayı', 0),
('otel.yorum.sil', 'Otel', 'sil', 'Otel yorumu silme', 0),

-- Finans Modülü
('finans.komisyon.gor', 'Finans', 'goruntule', 'Komisyon oranlarını görme', 0),
('finans.komisyon.duzenle', 'Finans', 'duzenle', 'Komisyon oranı değiştirme', 0),
('finans.odeme.onayla', 'Finans', 'onayla', 'Partnere ödeme onayı verme', 0),
('finans.fatura.kes', 'Finans', 'ekle', 'Fatura kesme yetkisi', 0),
('finans.rapor.gor', 'Finans', 'goruntule', 'Finansal raporları görüntüleme', 0),
('finans.iade.onayla', 'Finans', 'onayla', 'İade taleplerini onaylama', 0),

-- Rezervasyon Modülü
('rezervasyon.listele', 'Rezervasyon', 'listele', 'Tüm rezervasyonları listeleme', 0),
('rezervasyon.iptal.et', 'Rezervasyon', 'sil', 'Rezervasyon iptal etme', 0),
('rezervasyon.tarih.degistir', 'Rezervasyon', 'duzenle', 'Rezervasyon tarihi değiştirme', 0),

-- Sistem Modülü
('sistem.ayarlar.gor', 'Sistem', 'goruntule', 'Sistem ayarlarını görme', 0),
('sistem.ayarlar.duzenle', 'Sistem', 'duzenle', 'Sistem ayarlarını değiştirme', 0),
('sistem.log.gor', 'Sistem', 'goruntule', 'Sistem loglarını görüntüleme', 0),
('sistem.kullanici.rol.ata', 'Sistem', 'duzenle', 'Kullanıcıya rol atama', 0);

