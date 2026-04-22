UPDATE dbo.oteller
SET onay_durumu = CASE
    WHEN onay_durumu IS NULL OR LTRIM(RTRIM(onay_durumu)) = N'' THEN N'Bekliyor'
    WHEN onay_durumu COLLATE Turkish_CI_AI IN (N'Onaylandı', N'Onaylandi', N'Onaylanmış', N'Onaylanmis', N'Onayli') THEN N'Onaylandı'
    WHEN onay_durumu COLLATE Turkish_CI_AI IN (N'Beklemede', N'Bekliyor', N'Onay Bekliyor', N'Incelemede', N'Kontrol Bekliyor') THEN N'Bekliyor'
    WHEN onay_durumu COLLATE Turkish_CI_AI IN (N'Reddedildi', N'Red', N'Pasif') THEN N'Reddedildi'
    WHEN onay_durumu COLLATE Turkish_CI_AI IN (N'Taslak', N'Draft') THEN N'Taslak'
    ELSE onay_durumu
END
WHERE onay_durumu IS NULL
   OR onay_durumu COLLATE Turkish_CI_AI IN (
        N'Onaylandi', N'Onaylanmış', N'Onaylanmis', N'Onayli',
        N'Beklemede', N'Onay Bekliyor', N'Incelemede', N'Kontrol Bekliyor',
        N'Red', N'Pasif', N'Draft'
   );
