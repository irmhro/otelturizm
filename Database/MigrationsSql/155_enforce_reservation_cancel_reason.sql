DROP TRIGGER IF EXISTS tr_rezervasyonlar_require_cancel_reason;

DELIMITER $$
CREATE TRIGGER tr_rezervasyonlar_require_cancel_reason
BEFORE UPDATE ON rezervasyonlar
FOR EACH ROW
BEGIN
    IF NEW.durum = 'İptal Edildi'
       AND CHAR_LENGTH(TRIM(COALESCE(NEW.iptal_nedeni, ''))) < 10 THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'Iptal nedeni en az 10 karakter olmali ve bos birakilamaz.';
    END IF;
END$$
DELIMITER ;
