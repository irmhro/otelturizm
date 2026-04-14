SET NAMES utf8mb4;

ALTER TABLE oteller
ADD COLUMN rezervasyon_telefonu VARCHAR(20) NULL AFTER web_sitesi,
ADD COLUMN satis_kontak_adi VARCHAR(100) NULL AFTER rezervasyon_telefonu,
ADD COLUMN satis_kontak_telefonu VARCHAR(20) NULL AFTER satis_kontak_adi,
ADD COLUMN satis_kontak_eposta VARCHAR(100) NULL AFTER satis_kontak_telefonu,
ADD COLUMN satis_notlari TEXT NULL AFTER satis_kontak_eposta;
