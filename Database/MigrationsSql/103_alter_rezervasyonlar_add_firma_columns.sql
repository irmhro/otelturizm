ALTER TABLE [rezervasyonlar]
    ADD [firma_id] bigint  NULL AFTER [kullanici_id],
    ADD [firma_calisan_id] bigint  NULL AFTER [firma_id],
    ADD [firma_onay_durumu] enum('Beklemede','Onaylandı','Reddedildi','Onay Gerekmiyor') NOT NULL DEFAULT 'Onay Gerekmiyor' AFTER [otel_onay_durumu],
    ADD [firma_onaylayan_kullanici_id] bigint  NULL AFTER [firma_onay_durumu],
    ADD [firma_onay_tarihi] DATETIME2 NULL DEFAULT NULL AFTER [firma_onaylayan_kullanici_id],
    ADD [toplam_tasarruf] decimal(10,2) NOT NULL DEFAULT 0.00 AFTER [indirim_tutari];

ALTER TABLE [rezervasyonlar]
    ADD INDEX [idx_rezervasyonlar_firma_id] ([firma_id]),
    ADD INDEX [idx_rezervasyonlar_firma_calisan_id] ([firma_calisan_id]),
    ADD INDEX [idx_rezervasyonlar_firma_onay_durumu] ([firma_onay_durumu]);

ALTER TABLE [rezervasyonlar]
    ADD CONSTRAINT [fk_rezervasyonlar_firma_id]
    FOREIGN KEY ([firma_id]) REFERENCES [firmalar] ([id]) ON DELETE SET NULL,
    ADD CONSTRAINT [fk_rezervasyonlar_firma_calisan_id]
    FOREIGN KEY ([firma_calisan_id]) REFERENCES [users] ([id]) ON DELETE SET NULL,
    ADD CONSTRAINT [fk_rezervasyonlar_firma_onaylayan]
    FOREIGN KEY ([firma_onaylayan_kullanici_id]) REFERENCES [users] ([id]) ON DELETE SET NULL;
