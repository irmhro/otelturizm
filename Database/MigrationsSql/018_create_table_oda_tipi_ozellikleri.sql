CREATE TABLE oda_tipi_ozellikleri (
    oda_tip_id BIGINT  NOT NULL,
    ozellik_id SMALLINT  NOT NULL,
    miktar TINYINT  DEFAULT 1,
    
    PRIMARY KEY (oda_tip_id, ozellik_id),
    INDEX idx_oda_tip_id (oda_tip_id),
    INDEX idx_ozellik_id (ozellik_id),
    
    FOREIGN KEY (oda_tip_id) REFERENCES oda_tipleri(id) ON DELETE CASCADE,
    FOREIGN KEY (ozellik_id) REFERENCES oda_ozellikleri(id) ON DELETE CASCADE
);

