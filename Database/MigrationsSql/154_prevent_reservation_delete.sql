DROP TRIGGER IF EXISTS tr_rezervasyonlar_prevent_delete;

CREATE TRIGGER tr_rezervasyonlar_prevent_delete
BEFORE DELETE ON rezervasyonlar
FOR EACH ROW
SIGNAL SQLSTATE '45000'
SET MESSAGE_TEXT = 'Rezervasyon kayitlari silinemez. Sadece iptal sureci kullanilabilir.';
