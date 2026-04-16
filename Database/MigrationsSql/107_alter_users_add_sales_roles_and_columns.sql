SET NAMES utf8mb4;

ALTER TABLE users
MODIFY COLUMN rol VARCHAR(64) NOT NULL DEFAULT 'user';

ALTER TABLE users
ADD satis_ekibi VARCHAR(100) NULL,
ADD gunluk_satis_hedefi DECIMAL(12,2) NULL,
ADD aylik_satis_hedefi DECIMAL(12,2) NULL,
ADD dahili_numara VARCHAR(20) NULL;

ALTER TABLE users
ADD INDEX idx_users_sales_team (satis_ekibi),
ADD INDEX idx_users_sales_role (rol, satis_ekibi);
