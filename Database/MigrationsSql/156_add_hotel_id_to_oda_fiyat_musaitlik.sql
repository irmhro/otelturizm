SET @schema_name := DATABASE();

SET @add_hotel_id_column := IF(
    EXISTS(
        SELECT 1
        FROM information_schema.columns
        WHERE table_schema = @schema_name
          AND table_name = 'oda_fiyat_musaitlik'
          AND column_name = 'otel_id'
    ),
    'SELECT 1',
    'ALTER TABLE oda_fiyat_musaitlik ADD COLUMN otel_id BIGINT UNSIGNED NULL AFTER oda_tip_id'
);
PREPARE stmt FROM @add_hotel_id_column;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

UPDATE oda_fiyat_musaitlik ofm
INNER JOIN oda_tipleri ot ON ot.id = ofm.oda_tip_id
SET ofm.otel_id = ot.otel_id
WHERE ofm.otel_id IS NULL;

DELETE ofm
FROM oda_fiyat_musaitlik ofm
LEFT JOIN oda_tipleri ot ON ot.id = ofm.oda_tip_id
WHERE ofm.otel_id IS NULL
   OR ot.id IS NULL;

SET @make_hotel_not_null := IF(
    EXISTS(
        SELECT 1
        FROM information_schema.columns
        WHERE table_schema = @schema_name
          AND table_name = 'oda_fiyat_musaitlik'
          AND column_name = 'otel_id'
          AND is_nullable = 'YES'
    ),
    'ALTER TABLE oda_fiyat_musaitlik MODIFY COLUMN otel_id BIGINT UNSIGNED NOT NULL',
    'SELECT 1'
);
PREPARE stmt FROM @make_hotel_not_null;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @drop_old_unique := IF(
    EXISTS(
        SELECT 1
        FROM information_schema.statistics
        WHERE table_schema = @schema_name
          AND table_name = 'oda_fiyat_musaitlik'
          AND index_name = 'uk_oda_tip_tarih'
    ),
    'ALTER TABLE oda_fiyat_musaitlik DROP INDEX uk_oda_tip_tarih',
    'SELECT 1'
);
PREPARE stmt FROM @drop_old_unique;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @add_new_unique := IF(
    EXISTS(
        SELECT 1
        FROM information_schema.statistics
        WHERE table_schema = @schema_name
          AND table_name = 'oda_fiyat_musaitlik'
          AND index_name = 'uk_ofm_hotel_room_date'
    ),
    'SELECT 1',
    'ALTER TABLE oda_fiyat_musaitlik ADD UNIQUE INDEX uk_ofm_hotel_room_date (otel_id, oda_tip_id, tarih)'
);
PREPARE stmt FROM @add_new_unique;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @add_idx_hotel_date_room := IF(
    EXISTS(
        SELECT 1
        FROM information_schema.statistics
        WHERE table_schema = @schema_name
          AND table_name = 'oda_fiyat_musaitlik'
          AND index_name = 'idx_ofm_hotel_date_room'
    ),
    'SELECT 1',
    'ALTER TABLE oda_fiyat_musaitlik ADD INDEX idx_ofm_hotel_date_room (otel_id, tarih, oda_tip_id)'
);
PREPARE stmt FROM @add_idx_hotel_date_room;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @add_fk_hotel := IF(
    EXISTS(
        SELECT 1
        FROM information_schema.referential_constraints
        WHERE constraint_schema = @schema_name
          AND table_name = 'oda_fiyat_musaitlik'
          AND constraint_name = 'fk_ofm_hotel_id'
    ),
    'SELECT 1',
    'ALTER TABLE oda_fiyat_musaitlik ADD CONSTRAINT fk_ofm_hotel_id FOREIGN KEY (otel_id) REFERENCES oteller(id) ON DELETE CASCADE'
);
PREPARE stmt FROM @add_fk_hotel;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;
