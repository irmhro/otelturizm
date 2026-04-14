-- 1) Otel turu enum alanini kurumsal/gelismis seceneklerle genislet
ALTER TABLE oteller
MODIFY COLUMN otel_turu ENUM(
    'Hotel',
    'Otel',
    'Butik Otel',
    'Apart Otel',
    'Villa',
    'Pansiyon',
    'Tatil Köyü',
    'Hostel',
    'Kamping',
    'Apartman Dairesi',
    'Resort Hotel',
    'City Hotel',
    'Business Hotel',
    'Airport Hotel',
    'Boutique Hotel',
    'Bungalow',
    'Glamping',
    'Residence',
    'Motel',
    'Thermal Hotel',
    'Ski Hotel',
    'Beach Hotel',
    'Luxury Hotel'
) NOT NULL;

-- 2) Tum mevcut kayitlari Hotel standardina cek
UPDATE oteller
SET otel_turu = 'Hotel';

-- 3) Kullanici talebine gore id=1 kaydini sil
DELETE FROM oteller
WHERE id = 1;
