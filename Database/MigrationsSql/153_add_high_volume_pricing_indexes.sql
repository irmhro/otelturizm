SET @schema_name := DATABASE();

SET @create_idx_room_date_status := IF(
    EXISTS(
        SELECT 1
        FROM information_schema.statistics
        WHERE table_schema = @schema_name
          AND table_name = 'oda_fiyat_musaitlik'
          AND index_name = 'idx_ofm_room_date_status'
    ),
    'SELECT 1',
    'ALTER TABLE oda_fiyat_musaitlik ADD INDEX idx_ofm_room_date_status (oda_tip_id, tarih, kapali_satis, toplam_oda_sayisi, satilan_oda_sayisi, bloke_oda_sayisi)'
);
PREPARE stmt FROM @create_idx_room_date_status;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @create_idx_date_room_price := IF(
    EXISTS(
        SELECT 1
        FROM information_schema.statistics
        WHERE table_schema = @schema_name
          AND table_name = 'oda_fiyat_musaitlik'
          AND index_name = 'idx_ofm_date_room_price'
    ),
    'SELECT 1',
    'ALTER TABLE oda_fiyat_musaitlik ADD INDEX idx_ofm_date_room_price (tarih, oda_tip_id, indirimli_fiyat, gecelik_fiyat)'
);
PREPARE stmt FROM @create_idx_date_room_price;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;
