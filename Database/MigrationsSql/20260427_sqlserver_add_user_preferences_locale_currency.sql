/*
  p171+p172: kullanıcı tercihleri (locale + currency)
  - Kolonlar yoksa ekler (idempotent).
*/

IF COL_LENGTH('dbo.users', 'tercih_locale') IS NULL
BEGIN
    ALTER TABLE dbo.users ADD tercih_locale NVARCHAR(16) NULL;
END

IF COL_LENGTH('dbo.users', 'tercih_para_birimi') IS NULL
BEGIN
    ALTER TABLE dbo.users ADD tercih_para_birimi NVARCHAR(8) NULL;
END

