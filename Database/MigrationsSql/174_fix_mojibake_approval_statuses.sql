UPDATE dbo.oteller
SET onay_durumu = N'Onaylandı'
WHERE onay_durumu IN (N'Onaylandi', N'OnaylandÄ±', N'Onaylanmış', N'Onaylanmis', N'Onayli');

UPDATE dbo.otel_gorselleri
SET onay_durumu = N'Onaylandı'
WHERE onay_durumu IN (N'Onaylandi', N'OnaylandÄ±', N'Onaylanmış', N'Onaylanmis', N'Onayli');

UPDATE dbo.oda_gorselleri
SET onay_durumu = N'Onaylandı'
WHERE onay_durumu IN (N'Onaylandi', N'OnaylandÄ±', N'Onaylanmış', N'Onaylanmis', N'Onayli');
