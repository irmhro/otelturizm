UPDATE dbo.oda_ozellikleri
SET kategori = CASE
    WHEN kategori COLLATE Turkish_CI_AI IN (N'Yatak OdasÄ±', N'Yatak Odasi') THEN N'Yatak Odası'
    WHEN kategori COLLATE Turkish_CI_AI IN (N'Aile ve Ã‡ocuk', N'Aile ve Cocuk') THEN N'Aile ve Çocuk'
    WHEN kategori COLLATE Turkish_CI_AI IN (N'EriÅŸilebilirlik', N'Erisilebilirlik') THEN N'Erişilebilirlik'
    WHEN kategori COLLATE Turkish_CI_AI IN (N'GÃ¼venlik', N'Guvenlik') THEN N'Güvenlik'
    ELSE kategori
END
WHERE kategori COLLATE Turkish_CI_AI IN (
    N'Yatak OdasÄ±', N'Yatak Odasi',
    N'Aile ve Ã‡ocuk', N'Aile ve Cocuk',
    N'EriÅŸilebilirlik', N'Erisilebilirlik',
    N'GÃ¼venlik', N'Guvenlik'
);
