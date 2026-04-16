ALTER TABLE [users]
    MODIFY COLUMN [rol] varchar(64) NOT NULL DEFAULT 'user';

ALTER TABLE [users]
    ADD [firma_id] bigint  NULL AFTER [sahiplik_partner_id],
    ADD [departman] varchar(100) NULL AFTER [firma_id],
    ADD [gorev_unvani] varchar(100) NULL AFTER [departman],
    ADD [harcama_limiti] decimal(10,2) NULL AFTER [gorev_unvani],
    ADD [onay_gereksinimi] BIT NOT NULL DEFAULT 0 AFTER [harcama_limiti],
    ADD [personel_kodu] varchar(30) NULL AFTER [onay_gereksinimi],
    ADD [firma_yonetici_mi] BIT NOT NULL DEFAULT 0 AFTER [personel_kodu],
    ADD [son_sirket_girisi_tarihi] DATETIME2 NULL DEFAULT NULL AFTER [firma_yonetici_mi];

ALTER TABLE [users]
    ADD INDEX [idx_users_firma_id] ([firma_id]),
    ADD INDEX [idx_users_rol_firma] ([rol],[firma_id]),
    ADD INDEX [idx_users_departman] ([departman]);

ALTER TABLE [users]
    ADD CONSTRAINT [fk_users_firma_id]
    FOREIGN KEY ([firma_id]) REFERENCES [firmalar] ([id]) ON DELETE SET NULL;
