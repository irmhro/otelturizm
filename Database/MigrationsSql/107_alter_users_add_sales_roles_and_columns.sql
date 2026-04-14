SET NAMES utf8mb4;

ALTER TABLE users
MODIFY COLUMN rol ENUM(
    'user',
    'admin',
    'partner_owner',
    'partner_manager',
    'partner_staff',
    'firma_admin',
    'firma_manager',
    'firma_staff',
    'sales_admin',
    'sales_agent'
) NOT NULL DEFAULT 'user';

ALTER TABLE users
ADD COLUMN satis_ekibi VARCHAR(100) NULL AFTER gorev_unvani,
ADD COLUMN gunluk_satis_hedefi DECIMAL(12,2) NULL AFTER satis_ekibi,
ADD COLUMN aylik_satis_hedefi DECIMAL(12,2) NULL AFTER gunluk_satis_hedefi,
ADD COLUMN dahili_numara VARCHAR(20) NULL AFTER aylik_satis_hedefi;

ALTER TABLE users
ADD INDEX idx_users_sales_team (satis_ekibi),
ADD INDEX idx_users_sales_role (rol, satis_ekibi);
