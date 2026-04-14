-- Otel kodunu algoritmik/profesyonel formata gecir:
-- OTLTRZM-<SEHIR3>-<HASH8>  (ornek: OTLTRZM-IST-A1B2C3D4)

ALTER TABLE oteller
MODIFY COLUMN otel_kodu VARCHAR(32) NOT NULL;

-- Mevcut kayitlari yeni algoritmik formata cevir
UPDATE oteller
SET otel_kodu = CONCAT(
    'OTLTRZM-',
    UPPER(
        SUBSTRING(
            REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(IFNULL(sehir, 'GEN'), 'İ', 'I'), 'I', 'I'), 'Ş', 'S'), 'Ç', 'C'), 'Ğ', 'G'), 'Ü', 'U'),
            1,
            3
        )
    ),
    '-',
    UPPER(SUBSTRING(SHA2(CONCAT(id, '|', IFNULL(otel_adi, ''), '|', IFNULL(tam_adres, '')), 256), 1, 8))
);

-- Kodu bos gelen yeni kayitlarda otomatik uretim
DROP TRIGGER IF EXISTS trg_oteller_generate_kod_bi;
DELIMITER $$
CREATE TRIGGER trg_oteller_generate_kod_bi
BEFORE INSERT ON oteller
FOR EACH ROW
BEGIN
    IF NEW.otel_kodu IS NULL OR NEW.otel_kodu = '' THEN
        SET NEW.otel_kodu = CONCAT(
            'OTLTRZM-',
            UPPER(
                SUBSTRING(
                    REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(IFNULL(NEW.sehir, 'GEN'), 'İ', 'I'), 'I', 'I'), 'Ş', 'S'), 'Ç', 'C'), 'Ğ', 'G'), 'Ü', 'U'),
                    1,
                    3
                )
            ),
            '-',
            UPPER(SUBSTRING(SHA2(CONCAT(UUID(), '|', IFNULL(NEW.otel_adi, ''), '|', IFNULL(NEW.partner_id, 0)), 256), 1, 8))
        );
    END IF;
END$$
DELIMITER ;
