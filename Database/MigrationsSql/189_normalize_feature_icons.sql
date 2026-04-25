IF OBJECT_ID(N'dbo.otel_ozellikleri', N'U') IS NOT NULL
BEGIN
    UPDATE dbo.otel_ozellikleri
    SET ozellik_adi = REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(ozellik_adi,
        N'Ãœ', N'Ü'),
        N'Ã¼', N'ü'),
        N'Ã–', N'Ö'),
        N'Ã¶', N'ö'),
        N'Ä°', N'İ'),
        N'Ä±', N'ı'),
        N'Ã‡', N'Ç'),
        N'Ã§', N'ç'),
        N'Äž', N'Ğ'),
        N'ÄŸ', N'ğ'),
        N'Åž', N'Ş'),
        N'ÅŸ', N'ş')
    WHERE ozellik_adi LIKE N'%Ã%' OR ozellik_adi LIKE N'%Ä%' OR ozellik_adi LIKE N'%Å%';

    UPDATE dbo.otel_ozellikleri
    SET ozellik_ikon =
        CASE
            WHEN ozellik_adi LIKE N'%WiFi%' OR ozellik_adi LIKE N'%Wifi%' OR ozellik_adi LIKE N'%İnternet%' THEN N'fa-wifi'
            WHEN ozellik_adi LIKE N'%Klima%' THEN N'fa-snowflake'
            WHEN ozellik_adi LIKE N'%Minibar%' THEN N'fa-wine-bottle'
            WHEN ozellik_adi LIKE N'%Fön%' OR ozellik_adi LIKE N'%Fon%' THEN N'fa-wind'
            WHEN ozellik_adi LIKE N'%Resepsiyon%' THEN N'fa-clock'
            WHEN ozellik_adi LIKE N'%Restoran%' OR ozellik_adi LIKE N'%Açık Büfe%' OR ozellik_adi LIKE N'%Kahvalt%' THEN N'fa-utensils'
            WHEN ozellik_adi LIKE N'%Fitness%' OR ozellik_adi LIKE N'%Spor%' THEN N'fa-dumbbell'
            WHEN ozellik_adi LIKE N'%Spa%' OR ozellik_adi LIKE N'%Masaj%' OR ozellik_adi LIKE N'%Sağlık%' OR ozellik_adi LIKE N'%Sauna%' OR ozellik_adi LIKE N'%Hamam%' THEN N'fa-spa'
            WHEN ozellik_adi LIKE N'%Yüzme Havuzu%' OR ozellik_adi LIKE N'%Çocuk Havuzu%' OR ozellik_adi LIKE N'%Havuz%' THEN N'fa-water-ladder'
            WHEN ozellik_adi LIKE N'%Otopark%' OR ozellik_adi LIKE N'%Vale%' THEN N'fa-square-parking'
            WHEN ozellik_adi LIKE N'%Transfer%' OR ozellik_adi LIKE N'%Shuttle%' THEN N'fa-bus'
            WHEN ozellik_adi LIKE N'%Güvenlik%' THEN N'fa-shield-halved'
            WHEN ozellik_adi LIKE N'%Bar%' OR ozellik_adi LIKE N'%Lounge%' THEN N'fa-martini-glass-citrus'
            WHEN ozellik_adi LIKE N'%Toplantı%' OR ozellik_adi LIKE N'%Business%' OR ozellik_adi LIKE N'%Çalışma%' THEN N'fa-briefcase'
            WHEN ozellik_adi LIKE N'%Plaj%' OR ozellik_adi LIKE N'%Deniz%' OR ozellik_adi LIKE N'%İskele%' THEN N'fa-umbrella-beach'
            WHEN ozellik_adi LIKE N'%Çocuk%' OR ozellik_adi LIKE N'%Bebek%' OR ozellik_adi LIKE N'%Aile%' THEN N'fa-children'
            WHEN ozellik_adi LIKE N'%Çamaşır%' OR ozellik_adi LIKE N'%Ütü%' THEN N'fa-shirt'
            WHEN ozellik_adi LIKE N'%Evcil%' THEN N'fa-paw'
            WHEN ozellik_adi LIKE N'%Mutfak%' THEN N'fa-kitchen-set'
            WHEN ozellik_adi LIKE N'%Bahçe%' THEN N'fa-seedling'
            WHEN ozellik_adi LIKE N'%Şömine%' THEN N'fa-fire'
            WHEN ozellik_adi LIKE N'%Asansör%' THEN N'fa-elevator'
            ELSE ozellik_ikon
        END
    WHERE aktif_mi = 1;
END

IF OBJECT_ID(N'dbo.oda_ozellikleri', N'U') IS NOT NULL
BEGIN
    UPDATE dbo.oda_ozellikleri
    SET ozellik_adi = REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(ozellik_adi,
        N'Ãœ', N'Ü'),
        N'Ã¼', N'ü'),
        N'Ã–', N'Ö'),
        N'Ã¶', N'ö'),
        N'Ä°', N'İ'),
        N'Ä±', N'ı'),
        N'Ã‡', N'Ç'),
        N'Ã§', N'ç'),
        N'Äž', N'Ğ'),
        N'ÄŸ', N'ğ'),
        N'Åž', N'Ş'),
        N'ÅŸ', N'ş')
    WHERE ozellik_adi LIKE N'%Ã%' OR ozellik_adi LIKE N'%Ä%' OR ozellik_adi LIKE N'%Å%';

    UPDATE dbo.oda_ozellikleri
    SET ozellik_ikon =
        CASE
            WHEN ozellik_adi LIKE N'%WiFi%' OR ozellik_adi LIKE N'%Wifi%' OR ozellik_adi LIKE N'%İnternet%' THEN N'fa-wifi'
            WHEN ozellik_adi LIKE N'%Klima%' THEN N'fa-snowflake'
            WHEN ozellik_adi LIKE N'%Minibar%' THEN N'fa-wine-bottle'
            WHEN ozellik_adi LIKE N'%Fön%' OR ozellik_adi LIKE N'%Fon%' OR ozellik_adi LIKE N'%Saç Kurutma%' THEN N'fa-wind'
            WHEN ozellik_adi LIKE N'%TV%' THEN N'fa-tv'
            WHEN ozellik_adi LIKE N'%Balkon%' THEN N'fa-building'
            WHEN ozellik_adi LIKE N'%Teras%' THEN N'fa-sun'
            WHEN ozellik_adi LIKE N'%Banyo%' OR ozellik_adi LIKE N'%Duş%' OR ozellik_adi LIKE N'%Küvet%' OR ozellik_adi LIKE N'%Jakuzi%' OR ozellik_adi LIKE N'%havlu%' THEN N'fa-bath'
            WHEN ozellik_adi LIKE N'%Kasa%' OR ozellik_adi LIKE N'%Kilit%' OR ozellik_adi LIKE N'%Güvenlik%' THEN N'fa-lock'
            WHEN ozellik_adi LIKE N'%Mutfak%' OR ozellik_adi LIKE N'%Ocak%' OR ozellik_adi LIKE N'%Mikrodalga%' OR ozellik_adi LIKE N'%Fırın%' THEN N'fa-kitchen-set'
            WHEN ozellik_adi LIKE N'%Buzdolabı%' THEN N'fa-temperature-low'
            WHEN ozellik_adi LIKE N'%Kahve%' OR ozellik_adi LIKE N'%Çay%' OR ozellik_adi LIKE N'%Kettle%' THEN N'fa-mug-hot'
            WHEN ozellik_adi LIKE N'%Manzara%' THEN N'fa-binoculars'
            WHEN ozellik_adi LIKE N'%Çalışma%' OR ozellik_adi LIKE N'%Masa%' THEN N'fa-table'
            WHEN ozellik_adi LIKE N'%Yatak%' OR ozellik_adi LIKE N'%Sofa Bed%' OR ozellik_adi LIKE N'%Koltuk%' THEN N'fa-bed'
            WHEN ozellik_adi LIKE N'%Aile%' OR ozellik_adi LIKE N'%Çocuk%' OR ozellik_adi LIKE N'%Bebek%' THEN N'fa-children'
            WHEN ozellik_adi LIKE N'%Giriş%' THEN N'fa-door-open'
            WHEN ozellik_adi LIKE N'%Bahçe%' THEN N'fa-seedling'
            WHEN ozellik_adi LIKE N'%Ütü%' OR ozellik_adi LIKE N'%Çamaşır%' OR ozellik_adi LIKE N'%Kıyafet%' OR ozellik_adi LIKE N'%Gardırop%' OR ozellik_adi LIKE N'%gardırop%' THEN N'fa-shirt'
            WHEN ozellik_adi LIKE N'%saç kurutma%' OR ozellik_adi LIKE N'%fön%' OR ozellik_adi LIKE N'%fon%' THEN N'fa-wind'
            WHEN ozellik_adi LIKE N'%bakım seti%' OR ozellik_adi LIKE N'%kozmetik%' OR ozellik_adi LIKE N'%sabun%' THEN N'fa-pump-soap'
            ELSE ozellik_ikon
        END
    WHERE aktif_mi = 1;

    UPDATE dbo.oda_ozellikleri SET ozellik_ikon = N'fa-bath' WHERE aktif_mi = 1 AND ozellik_adi IN (N'Duş', N'duş', N'havlu seti', N'Havlu Seti');
    UPDATE dbo.oda_ozellikleri SET ozellik_ikon = N'fa-wind' WHERE aktif_mi = 1 AND ozellik_adi IN (N'saç kurutma makinası', N'Saç Kurutma Makinesi', N'Fön Makinası');
    UPDATE dbo.oda_ozellikleri SET ozellik_ikon = N'fa-pump-soap' WHERE aktif_mi = 1 AND ozellik_adi IN (N'kişiye özel bakım seti', N'Kişiye Özel Bakım Seti');
    UPDATE dbo.oda_ozellikleri SET ozellik_ikon = N'fa-shirt' WHERE aktif_mi = 1 AND ozellik_adi IN (N'Gardırop', N'gardırop');
END

IF OBJECT_ID(N'dbo.schema_migrations', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM dbo.schema_migrations WHERE script_name = N'189_normalize_feature_icons.sql')
BEGIN
    INSERT INTO dbo.schema_migrations (script_name, checksum, applied_at)
    VALUES (N'189_normalize_feature_icons.sql', N'1891891891891891891891891891891891891891891891891891891891891891', SYSUTCDATETIME());
END
