-- SQL Server seed (idempotent): departman hesaplari (irmhro0+*)
-- Not: Var olan kullanicilara dokunmaz; sadece yoksa ekler.
-- Parola politikasi: min 6, harf + rakam zorunlu.

SET NOCOUNT ON;

DECLARE @defaultPassword nvarchar(128) = N'Otelturizm2026';
DECLARE @hash nvarchar(64) = LOWER(CONVERT(VARCHAR(64), HASHBYTES('SHA2_256', CONVERT(nvarchar(max), @defaultPassword)), 2));

DECLARE @now datetime2(0) = SYSUTCDATETIME();

DECLARE @items TABLE(email nvarchar(100) NOT NULL, role nvarchar(64) NOT NULL, department nvarchar(100) NOT NULL, full_name nvarchar(100) NOT NULL);
INSERT INTO @items(email, role, department, full_name)
VALUES
 (N'irmhro0+kullanici@gmail.com',  N'departman_kullanici',  N'Kullanıcı',  N'Departman Kullanıcı'),
 (N'irmhro0+partner@gmail.com',    N'departman_partner',    N'Partner',    N'Departman Partner'),
 (N'irmhro0+firma@gmail.com',      N'departman_firma',      N'Firma',      N'Departman Firma'),
 (N'irmhro0+satis@gmail.com',      N'departman_satis',      N'Satış',      N'Departman Satış'),
 (N'irmhro0+muhasebe@gmail.com',   N'departman_muhasebe',   N'Muhasebe',   N'Departman Muhasebe'),
 (N'irmhro0+destek@gmail.com',     N'departman_destek',     N'Destek',     N'Departman Destek');

INSERT INTO dbo.users
(
    ad_soyad,
    eposta,
    sifre,
    rol,
    departman,
    hesap_durumu,
    email_dogrulama_tarihi,
    kayit_kaynagi,
    olusturulma_tarihi,
    guncellenme_tarihi
)
SELECT
    i.full_name,
    i.email,
    @hash,
    i.role,
    i.department,
    1,
    @now,
    N'seed_department_accounts',
    @now,
    @now
FROM @items i
WHERE NOT EXISTS (SELECT 1 FROM dbo.users u WHERE u.eposta = i.email);

