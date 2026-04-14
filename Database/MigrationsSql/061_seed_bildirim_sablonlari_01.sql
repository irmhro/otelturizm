INSERT INTO bildirim_sablonlari (sablon_kodu, sablon_adi, tur, dil, konu, icerik) VALUES
('rezervasyon_onay', 'Rezervasyon Onayı', 'E-posta', 'tr', '{otel_adi} - Rezervasyon Onayı #{rezervasyon_no}', 'Sayın {ad_soyad}, rezervasyonunuz onaylanmıştır...'),
('rezervasyon_hatirlatma', 'Rezervasyon Hatırlatma', 'Push Notification', 'tr', 'Yaklaşan Rezervasyon', '{otel_adi} için rezervasyonunuza 24 saat kaldı!'),
('odeme_basarili', 'Ödeme Başarılı', 'SMS', 'tr', NULL, '{tutar} TL tutarındaki ödemeniz alınmıştır. Rezervasyon No: {rezervasyon_no}'),
('yeni_mesaj', 'Yeni Mesaj', 'Sistem İçi', 'tr', 'Yeni Mesaj', '{gonderen_adi} size bir mesaj gönderdi.'),
('ozel_teklif', 'Özel Teklif', 'E-posta', 'tr', '{otel_adi} - Özel Teklif', 'Size özel {tutar} TL fiyat teklifi!');

