SET NAMES utf8mb4;

ALTER TABLE oteller
ADD rezervasyon_telefonu VARCHAR(20) NULL,
ADD satis_kontak_adi VARCHAR(100) NULL,
ADD satis_kontak_telefonu VARCHAR(20) NULL,
ADD satis_kontak_eposta VARCHAR(100) NULL,
ADD satis_notlari NVARCHAR(MAX) NULL;
