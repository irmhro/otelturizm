ALTER TABLE `users`
    MODIFY COLUMN `rol` varchar(64) NOT NULL DEFAULT 'user';

ALTER TABLE `users`
    ADD COLUMN `firma_id` bigint unsigned NULL AFTER `sahiplik_partner_id`,
    ADD COLUMN `departman` varchar(100) NULL AFTER `firma_id`,
    ADD COLUMN `gorev_unvani` varchar(100) NULL AFTER `departman`,
    ADD COLUMN `harcama_limiti` decimal(10,2) NULL AFTER `gorev_unvani`,
    ADD COLUMN `onay_gereksinimi` tinyint(1) NOT NULL DEFAULT 0 AFTER `harcama_limiti`,
    ADD COLUMN `personel_kodu` varchar(30) NULL AFTER `onay_gereksinimi`,
    ADD COLUMN `firma_yonetici_mi` tinyint(1) NOT NULL DEFAULT 0 AFTER `personel_kodu`,
    ADD COLUMN `son_sirket_girisi_tarihi` timestamp NULL DEFAULT NULL AFTER `firma_yonetici_mi`;

ALTER TABLE `users`
    ADD INDEX `idx_users_firma_id` (`firma_id`),
    ADD INDEX `idx_users_rol_firma` (`rol`,`firma_id`),
    ADD INDEX `idx_users_departman` (`departman`);

ALTER TABLE `users`
    ADD CONSTRAINT `fk_users_firma_id`
    FOREIGN KEY (`firma_id`) REFERENCES `firmalar` (`id`) ON DELETE SET NULL;
