ALTER TABLE [faturalar]
    ADD [firma_id] bigint  NULL AFTER [partner_id];

ALTER TABLE [faturalar]
    ADD INDEX [idx_faturalar_firma_id] ([firma_id]);

ALTER TABLE [faturalar]
    ADD CONSTRAINT [fk_faturalar_firma_id]
    FOREIGN KEY ([firma_id]) REFERENCES [firmalar] ([id]) ON DELETE SET NULL;
