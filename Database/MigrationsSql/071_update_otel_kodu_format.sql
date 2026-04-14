-- Domain tabanli standart kod formati:
-- OTLTRZM_000001

UPDATE oteller
SET otel_kodu = CONCAT('OTLTRZM_', LPAD(id, 6, '0'));
