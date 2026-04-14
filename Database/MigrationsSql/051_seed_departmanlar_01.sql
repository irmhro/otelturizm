INSERT INTO departmanlar (departman_kodu, departman_adi, yonetici_rol_id, aciklama) VALUES
('YK', 'Yönetim Kurulu', (SELECT id FROM roller WHERE rol_kodu = 'super_admin'), 'En üst karar organı'),
('GM', 'Genel Müdürlük', (SELECT id FROM roller WHERE rol_kodu = 'genel_mudur'), 'İcra kurulu başkanlığı'),
('FIN', 'Finans ve Muhasebe', (SELECT id FROM roller WHERE rol_kodu = 'finans_direktoru'), 'Tüm mali işlemler'),
('SAT', 'Satış ve İş Geliştirme', (SELECT id FROM roller WHERE rol_kodu = 'satis_direktoru'), 'Otel kazanımı ve satış'),
('OPS', 'Operasyon', (SELECT id FROM roller WHERE rol_kodu = 'operasyon_direktoru'), 'Günlük operasyon yönetimi'),
('DESTEK', 'Müşteri Hizmetleri', (SELECT id FROM roller WHERE rol_kodu = 'destek_direktoru'), '7/24 destek hizmetleri'),
('PAZ', 'Pazarlama', (SELECT id FROM roller WHERE rol_kodu = 'pazarlama_direktoru'), 'Marka ve dijital pazarlama'),
('IT', 'Bilgi Teknolojileri', (SELECT id FROM roller WHERE rol_kodu = 'cto'), 'Yazılım ve altyapı'),
('HUK', 'Hukuk ve Uyumluluk', (SELECT id FROM roller WHERE rol_kodu = 'hukuk_direktoru'), 'Hukuki işler ve KVKK'),
('IK', 'İnsan Kaynakları', (SELECT id FROM roller WHERE rol_kodu = 'ik_direktoru'), 'Personel yönetimi'),
('DIS', 'Dış Paydaşlar', NULL, 'Oteller, acenteler, misafirler');

